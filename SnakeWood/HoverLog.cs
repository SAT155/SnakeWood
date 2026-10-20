using System.Collections.Generic;
using UnityEngine;

namespace SnakeWood;

// A rideable hover log. Control follows the Lox saddle (Sadle): the rider asks the ZDO owner for
// control, the owner records them as the user and hands them ownership, and from then on the
// rider's own game runs the physics. Everyone else just sees the transform ZSyncTransform syncs.
public class HoverLog : MonoBehaviour, Interactable, Hoverable, IDoodadController, IRemoved
{
	public string m_variant = "";
	public Transform m_attachPoint = null!;
	// Holds the model, collider and seat; the root carries the vanilla-disguise rotation on top.
	public Transform m_frame = null!;

	public Transform Frame => m_frame;

	// How the synced root is turned relative to the log itself, so the vanilla log it disguises as
	// lies along the heading for players without the mod. The frame undoes it for the real model.
	private Quaternion m_disguiseOffset = Quaternion.identity;
	private float m_offsetCheckTimer;

	internal void SetDisguiseOffset(Quaternion offset)
	{
		m_disguiseOffset = offset;
		m_frame.localRotation = Quaternion.Inverse(offset);
		MatchDisguiseSize();
	}

	// Vanilla logs can't be shrunk for players without the mod, so the real log grows instead: to
	// about 90% of the vanilla log it's disguised as. Scaling the frame scales the model, collider,
	// deck and seat position together; the rider keeps their own size.
	private int m_sizedFor;

	private void MatchDisguiseSize()
	{
		if (m_v == null || !m_nview.IsValid())
		{
			return;
		}
		int prefab = m_nview.GetZDO().GetPrefab();
		if (prefab == m_sizedFor)
		{
			return;
		}
		m_sizedFor = prefab;
		(float length, float girth)? size = HoverLogDisguise.SizeOf(prefab);
		if (size == null)
		{
			m_frame.localScale = Vector3.one;
			return;
		}
		float lengthScale = Mathf.Max(1f, size.Value.length * 0.9f / m_v.Length);
		float girthScale = Mathf.Max(1f, size.Value.girth * 0.9f / (0.45f * m_v.Thickness));
		m_frame.localScale = new Vector3(girthScale, girthScale, lengthScale);
		// What it bumps into stays the log's own size (a little shorter, even), so the bigger look
		// doesn't wedge it in doorways and beside portals. The deck keeps its width and height so
		// passengers still stand on top of what they see, but not its length.
		if (m_frame.GetComponent<CapsuleCollider>() is CapsuleCollider capsule)
		{
			capsule.radius = 0.225f * m_v.Thickness / girthScale;
			capsule.height = 0.9f * m_v.Length / lengthScale;
		}
		if (m_frame.Find("deck")?.GetComponent<BoxCollider>() is BoxCollider deck)
		{
			Vector3 deckSize = deck.size;
			deckSize.z = 0.8f * m_v.Length / lengthScale;
			deck.size = deckSize;
		}
	}
	public Vector3 m_detachOffset = new(0f, 0.6f, -1f);
	public float m_maxUseRange = 4f;

	private const float FreeFallMargin = 1.5f;
	private const float RamCooldown = 0.6f;

	private static readonly RaycastHit[] s_hits = new RaycastHit[16];
	private static int s_groundMask;
	private static int s_terrainMask;

	private ZNetView m_nview = null!;
	private Rigidbody m_body = null!;
	private ZSyncTransform? m_sync;
	private HoverLogVariant m_v = null!;

	// Rider input, written by ApplyControlls on the rider's game.
	private Vector3 m_moveInput;
	private Vector3 m_lookDir;
	private bool m_boost;
	private bool m_brake;
	private bool m_crouchHeld;
	private bool m_hopQueued;

	private float m_yaw;
	private float m_roll;
	private float m_pitch;
	private bool m_airborne;
	// The velocity the log was driven at this physics step, before any collision changed it.
	private Vector3 m_driveVelocity;
	private int m_airHopsUsed;
	private readonly List<(Vector3 pos, float time)> m_cloud = new();
	private readonly Dictionary<int, float> m_lastCloudHit = new();
	private float m_cloudTimer;
	private static readonly List<Character> s_inCloud = new();
	private float m_validUserTimer;
	private bool m_haveValidUser;
	private readonly Dictionary<int, float> m_lastRamTime = new();

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_sync = GetComponent<ZSyncTransform>();
		HoverLogVariant? variant = HoverLogVariant.Get(m_variant);
		if (m_nview.GetZDO() == null || variant == null)
		{
			// Placement ghost (or a broken prefab): just a picture, no physics. The ghost is turned by
			// the placement rotation alone, so the frame drops the disguise offset to lie flat.
			m_frame.localRotation = Quaternion.identity;
			m_body.isKinematic = true;
			enabled = false;
			return;
		}
		m_v = variant;
		m_attachPoint.localPosition = new Vector3(0f, SnakeWoodPlugin.SeatHeight.Value, -0.2f * m_v.Length);
		if (m_nview.IsOwner())
		{
			HoverLogDisguise.Apply(this, m_nview.GetZDO());
		}
		else
		{
			SetDisguiseOffset(HoverLogDisguise.StoredOffset(m_nview.GetZDO()));
		}
		m_yaw = FrameYaw();

		if (s_groundMask == 0)
		{
			s_groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
		}

		HoverLogFx.Attach(this, m_v);

		m_nview.Register<long>("SnakeWood_RequestControl", RPC_RequestControl);
		m_nview.Register<long>("SnakeWood_ReleaseControl", RPC_ReleaseControl);
		m_nview.Register<bool>("SnakeWood_RequestResponse", RPC_RequestResponse);
		m_nview.Register<int>("SnakeWood_AddFuel", RPC_AddFuel);
		m_nview.Register<float>("SnakeWood_Repair", RPC_Repair);
		m_nview.Register<Vector3, bool>("SnakeWood_Hit", RPC_Hit);
	}

	public HoverLogVariant Variant => m_v;

	// ---- Fuel (resin by default) ----------------------------------------------------------------

	private static readonly int s_fuelKey = "SnakeWood_fuel".GetStableHashCode();

	private static bool FuelEnabled => SnakeWoodPlugin.FuelPerMinute.Value > 0f;

	private float GetFuel() => m_nview.IsValid() ? m_nview.GetZDO().GetFloat(s_fuelKey, 0f) : 0f;

	private static ItemDrop.ItemData? FuelItemData() =>
		ObjectDB.instance?.GetItemPrefab(SnakeWoodPlugin.FuelItem.Value)?.GetComponent<ItemDrop>()?.m_itemData;

	private static string FuelName() => FuelItemData()?.m_shared.m_name ?? SnakeWoodPlugin.FuelItem.Value;

	// Moves as much fuel as fits from the player's inventory into the log.
	private bool AddFuelFrom(Player player, int max)
	{
		string fuelName = FuelName();
		int room = SnakeWoodPlugin.FuelCapacity.Value - Mathf.CeilToInt(GetFuel());
		int amount = Mathf.Min(max, room, player.GetInventory().CountItems(fuelName));
		if (room <= 0)
		{
			player.Message(MessageHud.MessageType.Center, "It's full");
			return false;
		}
		if (amount <= 0)
		{
			player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("You have no " + fuelName));
			return false;
		}
		player.GetInventory().RemoveItem(fuelName, amount);
		m_nview.InvokeRPC("SnakeWood_AddFuel", amount);
		player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"Added {amount} {fuelName}"));
		return true;
	}

	private void RPC_AddFuel(long sender, int amount)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(s_fuelKey, Mathf.Clamp(GetFuel() + amount, 0f, SnakeWoodPlugin.FuelCapacity.Value));
		}
	}

	// Takes the whole resin left in the log back out: into the player's inventory if there's room,
	// otherwise onto the ground at their feet.
	private void TakeFuelOut(Player player)
	{
		int amount = Mathf.FloorToInt(GetFuel());
		ItemDrop.ItemData? fuel = FuelItemData();
		if (amount <= 0 || fuel == null)
		{
			player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"There's no whole {FuelName()} left in it"));
			return;
		}
		m_nview.InvokeRPC("SnakeWood_AddFuel", -amount);
		ItemDrop.ItemData item = fuel.Clone();
		item.m_stack = amount;
		if (!player.GetInventory().AddItem(item))
		{
			ItemDrop.DropItem(fuel, amount, player.transform.position + player.transform.forward * 0.5f + Vector3.up * 0.5f, Quaternion.identity);
		}
		player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"Took out {amount} {FuelName()}"));
	}

	// Deconstructing a log gives back the fuel still in it.
	public void OnRemoved()
	{
		if (m_nview != null && m_nview.IsValid())
		{
			DropFuel();
		}
	}

	private void DropFuel()
	{
		int amount = Mathf.FloorToInt(GetFuel());
		ItemDrop.ItemData? fuel = FuelItemData();
		if (amount > 0 && fuel != null)
		{
			m_nview.GetZDO().Set(s_fuelKey, 0f);
			ItemDrop.DropItem(fuel, amount, transform.position + Vector3.up * 0.5f, Quaternion.identity);
		}
	}

	// ---- Wear -----------------------------------------------------------------------------------

	// Stored as wear taken, so a new log starts in perfect condition.
	private static readonly int s_wearKey = "SnakeWood_wear".GetStableHashCode();

	private static bool WearEnabled => SnakeWoodPlugin.WearEnabled.Value;

	private float GetWear() => m_nview.IsValid() ? m_nview.GetZDO().GetFloat(s_wearKey, 0f) : 0f;

	private float Condition() => Mathf.Clamp01(1f - GetWear() / Mathf.Max(1f, m_v.DurabilityCfg.Value));

	// Each log is mended with the raw material it's made of (its Repair Item).
	private string RepairName() =>
		ObjectDB.instance?.GetItemPrefab(m_v.RepairItemCfg.Value)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name
		?? m_v.RepairItemCfg.Value;

	// Owner only (whoever is riding owns the log).
	private void AddWear(float amount, Vector3? point = null)
	{
		if (!WearEnabled || amount <= 0f || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (IsWrecked())
		{
			return;
		}
		float wear = Mathf.Min(m_v.DurabilityCfg.Value, GetWear() + amount);
		m_nview.GetZDO().Set(s_wearKey, wear);
		bool wrecked = wear >= m_v.DurabilityCfg.Value;
		if (amount >= 1f || wrecked)
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "SnakeWood_Hit", point ?? transform.position, wrecked);
		}
		if (wrecked)
		{
			m_nview.GetZDO().Set(s_wreckedKey, true);
			ThrowRider($"Your log is wrecked! Bring {RepairsNeeded()} {RepairName()} to repair it");
		}
	}

	// Uses repair items from the player's inventory to fix as much wear as they can.
	private bool RepairFrom(Player player)
	{
		string name = RepairName();
		int needed = Mathf.CeilToInt(GetWear() / SnakeWoodPlugin.RepairPerItem.Value);
		int amount = Mathf.Min(needed, player.GetInventory().CountItems(name));
		if (amount <= 0)
		{
			player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("You have no " + name + " to repair it"));
			return false;
		}
		player.GetInventory().RemoveItem(name, amount);
		m_nview.InvokeRPC("SnakeWood_Repair", amount * SnakeWoodPlugin.RepairPerItem.Value);
		int left = needed - amount;
		player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(left > 0 && IsWrecked()
			? $"Repaired with {amount} {name}. {left} more to get it flying"
			: $"Repaired with {amount} {name}"));
		return true;
	}

	private void RPC_Repair(long sender, float amount)
	{
		if (m_nview.IsOwner())
		{
			float wear = Mathf.Max(0f, GetWear() - amount);
			m_nview.GetZDO().Set(s_wearKey, wear);
			if (wear <= 0f)
			{
				// Fully mended: the wreck is a working log again.
				m_nview.GetZDO().Set(s_wreckedKey, false);
			}
		}
	}

	// ---- Wreckage ------------------------------------------------------------------------------

	// A log worn right out is wrecked: it drops where it is, dark and lifeless, and stays put (no
	// one can push or ride it) until it's repaired all the way back up.
	private static readonly int s_wreckedKey = "SnakeWood_wrecked".GetStableHashCode();

	private bool IsWrecked() => WearEnabled && m_nview.IsValid() && m_nview.GetZDO().GetBool(s_wreckedKey, false);

	private int RepairsNeeded() => Mathf.CeilToInt(GetWear() / SnakeWoodPlugin.RepairPerItem.Value);

	private bool m_wreckSettled;

	// Owner: lays the wreck on whatever's under it, tipped on its side, and pins it there.
	private void HoldWreck()
	{
		if (m_wreckSettled)
		{
			return;
		}
		m_wreckSettled = true;
		Vector3 pos = m_body.position;
		GetSurface(pos, out float surfaceY, out _, out _, out _);
		if (!float.IsNegativeInfinity(surfaceY))
		{
			pos.y = surfaceY + 0.2f * m_v.Thickness * m_frame.localScale.x;
		}
		m_body.linearVelocity = Vector3.zero;
		m_body.angularVelocity = Vector3.zero;
		m_body.isKinematic = true;
		Quaternion rotation = Quaternion.Euler(0f, m_yaw, 0f) * Quaternion.Euler(-6f, 0f, 28f) * m_disguiseOffset;
		m_body.position = pos;
		m_body.rotation = rotation;
		transform.SetPositionAndRotation(pos, rotation);
	}

	// Everyone: a wreck loses its glow and trail and goes dark.
	private bool m_showingWreck;
	private float m_wreckCheckTimer;
	private static MaterialPropertyBlock? s_block;

	private void UpdateWreckLook()
	{
		if ((m_wreckCheckTimer -= Time.deltaTime) > 0f)
		{
			return;
		}
		m_wreckCheckTimer = 0.5f;
		bool wrecked = IsWrecked();
		if (wrecked == m_showingWreck)
		{
			return;
		}
		m_showingWreck = wrecked;
		foreach (Light light in m_frame.GetComponentsInChildren<Light>(true))
		{
			light.enabled = !wrecked;
		}
		foreach (ParticleSystem particles in m_frame.GetComponentsInChildren<ParticleSystem>(true))
		{
			if (wrecked)
			{
				particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			}
			else
			{
				particles.Play();
			}
		}
		s_block ??= new MaterialPropertyBlock();
		foreach (Renderer renderer in m_frame.GetComponentsInChildren<Renderer>(true))
		{
			if (renderer is ParticleSystemRenderer)
			{
				continue;
			}
			if (!wrecked)
			{
				renderer.SetPropertyBlock(null);
				continue;
			}
			Material material = renderer.sharedMaterial;
			if (material == null || !material.HasProperty("_Color"))
			{
				continue;
			}
			renderer.GetPropertyBlock(s_block);
			s_block.SetColor("_Color", material.color * new Color(0.35f, 0.32f, 0.3f, 1f));
			renderer.SetPropertyBlock(s_block);
		}
	}

	// ---- Load (carry weight) and cargo ----------------------------------------------------------

	// Weight works like a cart's: no hard limit, just a log that flies worse the more you carry.
	// Load is carry weight over Load Capacity. Up to 1, performance eases down in a straight line to
	// Performance At Load Capacity; past it, it falls Overload Falloff faster, to a floor of 8%.
	private float m_load;
	private float m_loadTimer;

	private float LoadOf(Player player) =>
		player.GetInventory().GetTotalWeight() / Mathf.Max(1f, m_v.LoadCapacityCfg.Value);

	private static float Performance(float load)
	{
		float atCapacity = SnakeWoodPlugin.LoadAtCapacity.Value;
		float performance = load <= 1f
			? 1f - (1f - atCapacity) * load
			: atCapacity - SnakeWoodPlugin.OverloadFalloff.Value * (load - 1f);
		return Mathf.Clamp(performance, 0.08f, 1f);
	}

	private static bool CarriesMetal(Player player) =>
		SnakeWoodPlugin.NoMetal.Value && !player.GetInventory().IsTeleportable(false);

	// Checked twice a second while riding. False when the rider just got thrown off for cargo.
	private bool UpdateLoad(float dt)
	{
		Player player = Player.m_localPlayer;
		if (player == null || (m_loadTimer -= dt) > 0f)
		{
			return true;
		}
		m_loadTimer = 0.5f;
		if (CarriesMetal(player))
		{
			ThrowRider("The log won't carry ore or metal");
			return false;
		}
		bool wasOverloaded = m_load > 1f;
		m_load = LoadOf(player);
		if (m_load > 1f && !wasOverloaded)
		{
			player.Message(MessageHud.MessageType.Center, "Overloaded! The log is sinking and sluggish");
		}
		return true;
	}

	// Burns fuel while ridden; false once the tank is dry.
	private bool BurnFuel(float dt)
	{
		if (!FuelEnabled)
		{
			return true;
		}
		float fuel = GetFuel();
		if (fuel <= 0f)
		{
			return false;
		}
		fuel = Mathf.Max(0f, fuel - SnakeWoodPlugin.FuelPerMinute.Value / 60f * dt);
		m_nview.GetZDO().Set(s_fuelKey, fuel);
		if (fuel <= 0f)
		{
			Player.m_localPlayer?.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"Out of {FuelName()}! The log sputters"));
		}
		return fuel > 0f;
	}

	public float Speed => m_body.linearVelocity.magnitude;

	// ---- Letting the rider's own attacks pass through the log ----------------------------------

	private static int s_passThroughLayer = -1;
	private int m_solidLayer = -1;

	// Attack and projectile raycasts include the "vehicle" layer the log sits on, so a rider's
	// swings and arrows hit their own log. For the length of those checks the log moves to
	// Ignore Raycast, which those masks leave out.
	internal static HoverLog? BeginRiderPassThrough()
	{
		Player player = Player.m_localPlayer;
		if (player == null || player.GetDoodadController() is not HoverLog log)
		{
			return null;
		}
		if (s_passThroughLayer < 0)
		{
			s_passThroughLayer = LayerMask.NameToLayer("Ignore Raycast");
		}
		log.m_solidLayer = log.gameObject.layer;
		log.SetColliderLayer(s_passThroughLayer);
		return log;
	}

	// ---- Riding through portals (see MountedPortal) ---------------------------------------------

	internal ZDOID Id => m_nview.IsValid() ? m_nview.GetZDO().m_uid : ZDOID.None;

	// Once the rider has arrived: puts the log under them, facing the way they face, and seats
	// them again if the log had to be unloaded on the way.
	internal void ArriveUnder(Player rider)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		m_nview.ClaimOwnership();
		m_nview.GetZDO().Set(ZDOVars.s_user, rider.GetZDOID().UserID);
		float yaw = rider.transform.eulerAngles.y;
		Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
		// The seat sits behind the middle, so the middle goes a little ahead of the rider.
		Vector3 pos = rider.transform.position + heading * new Vector3(0f, 0f, 0.2f * m_v.Length * m_frame.localScale.z)
			- Vector3.up * SnakeWoodPlugin.SeatHeight.Value;
		Quaternion rotation = heading * m_disguiseOffset;
		m_body.linearVelocity = Vector3.zero;
		m_body.angularVelocity = Vector3.zero;
		m_body.position = pos;
		m_body.rotation = rotation;
		transform.SetPositionAndRotation(pos, rotation);
		m_nview.GetZDO().SetPosition(pos);
		m_nview.GetZDO().SetRotation(rotation);
		m_yaw = yaw;
		m_pitch = 0f;
		m_roll = 0f;
		m_unstickUntil = Time.time + 3f;
		if (!ReferenceEquals(rider.GetDoodadController(), this))
		{
			// The old log object was unloaded during the trip: seat the rider on this one.
			rider.m_doodadController = null;
			rider.AttachStop();
			ResetControls();
			RidingControlsPatch.ResetStance();
			rider.StartDoodadControl(this);
			rider.AttachStart(m_attachPoint, gameObject, hideWeapons: false, isBed: false, onShip: false,
				SnakeWoodPlugin.AttachAnimation, m_detachOffset);
		}
	}

	private void Start()
	{
		if (m_nview != null && m_nview.IsValid() && Player.m_localPlayer != null)
		{
			MountedPortal.OnLogLoaded(this, Player.m_localPlayer);
		}
	}

	internal void EndRiderPassThrough()
	{
		if (m_solidLayer >= 0)
		{
			SetColliderLayer(m_solidLayer);
		}
	}

	private void SetColliderLayer(int layer)
	{
		foreach (Collider collider in GetComponentsInChildren<Collider>())
		{
			collider.gameObject.layer = layer;
		}
	}

	// ---- Hover text and mounting -------------------------------------------------------------

	public string GetHoverName() => m_v?.Name ?? "";

	public string GetHoverText()
	{
		if (!m_nview.IsValid() || m_v == null || Player.m_localPlayer == null)
		{
			return "";
		}
		string fuel = "";
		if (WearEnabled)
		{
			fuel += IsWrecked()
				? $"\n<color=red>Wrecked</color>: needs {RepairsNeeded()} {RepairName()} to fly again"
				: $"\nCondition: {Condition() * 100f:0}%";
		}
		if (FuelEnabled)
		{
			fuel += $"\n{FuelName()}: {Mathf.CeilToInt(GetFuel())}/{SnakeWoodPlugin.FuelCapacity.Value}";
		}
		if (WearEnabled && GetWear() > 0f)
		{
			fuel += $"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Repair ({RepairName()})";
		}
		else if (FuelEnabled)
		{
			fuel += $"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Add {FuelName()}";
		}
		if (FuelEnabled && GetFuel() >= 1f)
		{
			fuel += $"\n[<color=yellow><b>{SnakeWoodPlugin.TakeFuelKey.Value}</b></color>] Take out {FuelName()}";
		}
		if (IsLocalUser())
		{
			return Localization.instance.Localize(m_v.Name + fuel + "\n[<color=yellow><b>$KEY_Use</b></color>] Get off");
		}
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(m_v.Name + fuel + "\n[<color=yellow><b>$KEY_Use</b></color>] Ride");
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold || !m_nview.IsValid() || character is not Player player)
		{
			return false;
		}
		if (alt && WearEnabled && GetWear() > 0f)
		{
			RepairFrom(player);
			return false;
		}
		if (alt && FuelEnabled)
		{
			AddFuelFrom(player, int.MaxValue);
			return false;
		}
		// Looking at your own log while riding it and pressing Use gets you off, like looking away does.
		if (ReferenceEquals(player.GetDoodadController(), this))
		{
			player.StopDoodadControl();
			return false;
		}
		if (!InUseDistance(player) || player.IsAttached() || player.GetDoodadController() != null)
		{
			return false;
		}
		if (FuelEnabled && GetFuel() <= 0f)
		{
			player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"It needs {FuelName()} to fly (Shift+E to add)"));
			return false;
		}
		if (IsWrecked())
		{
			player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"It's wrecked. Repair it with {RepairsNeeded()} {RepairName()} (Shift+E)"));
			return false;
		}
		if (CarriesMetal(player))
		{
			player.Message(MessageHud.MessageType.Center, "It won't carry ore or metal");
			return false;
		}
		if (LoadOf(player) > 1f)
		{
			player.Message(MessageHud.MessageType.Center, $"Overloaded: this log struggles past {m_v.LoadCapacityCfg.Value:0} weight");
		}
		m_nview.InvokeRPC("SnakeWood_RequestControl", player.GetZDOID().UserID);
		m_controlRequestedAt = Time.time;
		return false;
	}

	// A log can end up owned by a player without the mod, who never answers a request. If no answer
	// comes, take ownership and grant the request here.
	private float m_controlRequestedAt;

	private void CheckUnansweredRequest()
	{
		if (m_controlRequestedAt <= 0f || Time.time - m_controlRequestedAt < 1.5f)
		{
			return;
		}
		m_controlRequestedAt = 0f;
		Player player = Player.m_localPlayer;
		if (player == null || player.GetDoodadController() != null || !m_nview.IsValid())
		{
			return;
		}
		m_nview.ClaimOwnership();
		RPC_RequestControl(ZNet.GetUID(), player.GetZDOID().UserID);
	}

	// Using the fuel item on the log from the hotbar adds one.
	public bool UseItem(Humanoid user, ItemDrop.ItemData item) =>
		FuelEnabled && user is Player player && item.m_shared.m_name == FuelName() && AddFuelFrom(player, 1);

	public float GetHoverOffset() => 0f;

	private bool InUseDistance(Humanoid human) =>
		Vector3.Distance(human.transform.position, transform.position) < m_maxUseRange;

	private void RPC_RequestControl(long sender, long userID)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		CalculateHaveValidUser();
		if (GetUser() == userID || !m_haveValidUser)
		{
			m_nview.GetZDO().Set(ZDOVars.s_user, userID);
			m_nview.InvokeRPC(sender, "SnakeWood_RequestResponse", true);
			m_nview.GetZDO().SetOwner(sender);
		}
		else
		{
			m_nview.InvokeRPC(sender, "SnakeWood_RequestResponse", false);
		}
	}

	private void RPC_ReleaseControl(long sender, long userID)
	{
		if (m_nview.IsOwner() && GetUser() == userID)
		{
			m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
		}
	}

	private void RPC_RequestResponse(long sender, bool granted)
	{
		m_controlRequestedAt = 0f;
		Player player = Player.m_localPlayer;
		if (player == null)
		{
			return;
		}
		if (!granted)
		{
			player.Message(MessageHud.MessageType.Center, "$msg_inuse");
			return;
		}
		ResetControls();
		RidingControlsPatch.ResetStance();
		m_yaw = FrameYaw();
		m_load = LoadOf(player);
		m_loadTimer = 0.5f;
		player.StartDoodadControl(this);
		player.AttachStart(m_attachPoint, gameObject, hideWeapons: false, isBed: false, onShip: false,
			SnakeWoodPlugin.AttachAnimation, m_detachOffset);
	}

	// ---- IDoodadController: the rider's game feeds us its input every frame ---------------------

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		m_moveInput = moveDir;
		m_lookDir = lookDir;
		m_boost = run;
		// With Mounted Combat, block raises your shield; Crouch lowers or brakes instead (see Update).
		m_brake = block && !SnakeWoodPlugin.MountedCombat.Value;
	}

	private void Update()
	{
		CheckUnansweredRequest();
		// Watchers follow whatever offset the owner has applied.
		if (m_nview.IsValid() && !m_nview.IsOwner() && (m_offsetCheckTimer -= Time.deltaTime) <= 0f)
		{
			m_offsetCheckTimer = 1f;
			SetDisguiseOffset(HoverLogDisguise.StoredOffset(m_nview.GetZDO()));
		}
		m_crouchHeld = IsLocallyDriven() && (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"));
		CheckTakeFuelKey();
		UpdateWreckLook();
		UpdateShake();
		LogSync();
	}

	// ---- Getting unstuck --------------------------------------------------------------------------

	// A log wedged inside something (a wall beside a portal, a rock it was shoved into) lets go of
	// it: once it has been overlapping solid things for a moment, its colliders go soft until it's
	// clear. Hover doesn't need them (it's raycast), and merely pushing against a wall isn't an
	// overlap, so this can't be used to drive through walls.
	private float m_unstickUntil;
	private float m_unstickTimer;
	private int m_overlapChecks;
	private bool m_ghost;
	private static readonly Collider[] s_overlaps = new Collider[16];

	private void UpdateUnstick(float dt)
	{
		if ((m_unstickTimer -= dt) > 0f)
		{
			return;
		}
		m_unstickTimer = 0.25f;
		bool overlapping = Embedded();
		m_overlapChecks = overlapping ? m_overlapChecks + 1 : 0;
		if (!overlapping)
		{
			SetGhost(false);
		}
		else if (m_overlapChecks >= 3 || Time.time < m_unstickUntil)
		{
			SetGhost(true);
		}
	}

	private bool Embedded()
	{
		if (m_frame.GetComponent<CapsuleCollider>() is not CapsuleCollider capsule)
		{
			return false;
		}
		Vector3 axis = m_frame.forward;
		float radius = capsule.radius * m_frame.lossyScale.x * 0.8f;
		float half = Mathf.Max(0f, capsule.height * m_frame.lossyScale.z * 0.5f - radius);
		Vector3 centre = m_frame.TransformPoint(capsule.center);
		int count = Physics.OverlapCapsuleNonAlloc(centre - axis * half, centre + axis * half, radius, s_overlaps,
			s_groundMask & ~LayerMask.GetMask("terrain"), QueryTriggerInteraction.Ignore);
		for (int i = 0; i < count; i++)
		{
			Collider other = s_overlaps[i];
			if (other.attachedRigidbody != m_body && other.GetComponentInParent<Character>() == null)
			{
				return true;
			}
		}
		return false;
	}

	private void SetGhost(bool ghost)
	{
		if (ghost == m_ghost)
		{
			return;
		}
		m_ghost = ghost;
		foreach (Collider collider in m_frame.GetComponentsInChildren<Collider>())
		{
			collider.isTrigger = ghost;
		}
	}

	// ---- Network sync ------------------------------------------------------------------------------

	// On everyone else's game the log is kinematic and simply eases towards the latest position and
	// rotation the rider's game sent. Left as a free body, ZSyncTransform would both push it along
	// with the synced velocity and aim it ahead along that velocity (for up to two seconds when
	// updates are late), so a laggy rider's sharp turn sent it sailing off the way it had been
	// going.
	private void FollowOwner()
	{
		if (!m_body.isKinematic)
		{
			m_body.linearVelocity = Vector3.zero;
			m_body.angularVelocity = Vector3.zero;
			m_body.isKinematic = true;
		}
		if (m_sync != null)
		{
			m_sync.m_isKinematicBody = true;
		}
	}

	// Sync Debug: every two seconds, how often updates for this log arrive and how far what you see
	// is from the latest update, to tell lag from bad smoothing.
	private long m_lastRevision = -1;
	private float m_lastRevisionTime;
	private float m_syncLogTimer;
	private int m_updates;
	private float m_longestGap;
	private float m_worstError;

	private void LogSync()
	{
		if (!SnakeWoodPlugin.SyncDebug.Value || !m_nview.IsValid())
		{
			return;
		}
		ZDO zdo = m_nview.GetZDO();
		if ((long)zdo.DataRevision != m_lastRevision)
		{
			if (m_lastRevisionTime > 0f)
			{
				m_longestGap = Mathf.Max(m_longestGap, Time.time - m_lastRevisionTime);
			}
			m_lastRevision = (long)zdo.DataRevision;
			m_lastRevisionTime = Time.time;
			m_updates++;
		}
		m_worstError = Mathf.Max(m_worstError, Vector3.Distance(transform.position, zdo.GetPosition()));
		if ((m_syncLogTimer += Time.deltaTime) < 2f)
		{
			return;
		}
		SnakeWoodPlugin.Log.LogInfo($"Sync {m_v.Name} {zdo.m_uid.ID}: {(m_nview.IsOwner() ? "owner" : "watching")}, " +
			$"{m_updates / m_syncLogTimer:0.#} updates/s, longest gap {m_longestGap * 1000f:0} ms, " +
			$"worst offset from latest update {m_worstError:0.00} m, speed {m_body.linearVelocity.magnitude:0.#} m/s, kinematic {m_body.isKinematic}");
		m_syncLogTimer = 0f;
		m_updates = 0;
		m_longestGap = 0f;
		m_worstError = 0f;
	}

	// ---- Damage cues ------------------------------------------------------------------------------

	// Taking wear plays the cart's hit effect (wood splinters and a knock) and shakes the log, like
	// a cart taking damage; wrecking it plays the cart's break effect. Everyone nearby sees it.
	private static EffectList? s_hitEffect;
	private static EffectList? s_breakEffect;
	private float m_shakeUntil;

	private static void FindCartEffects()
	{
		if (s_hitEffect == null && ZNetScene.instance?.GetPrefab("Cart")?.GetComponent<WearNTear>() is WearNTear cart)
		{
			s_hitEffect = cart.m_hitEffect;
			s_breakEffect = cart.m_destroyedEffect;
		}
	}

	private void RPC_Hit(long sender, Vector3 point, bool wrecked)
	{
		FindCartEffects();
		(wrecked ? s_breakEffect : s_hitEffect)?.Create(point, Quaternion.identity);
		m_shakeUntil = Time.time + (wrecked ? 0.6f : 0.3f);
	}

	private void UpdateShake()
	{
		if (Time.time < m_shakeUntil)
		{
			m_frame.localPosition = Random.insideUnitSphere * 0.05f;
		}
		else if (m_frame.localPosition != Vector3.zero)
		{
			m_frame.localPosition = Vector3.zero;
		}
	}

	private void CheckTakeFuelKey()
	{
		Player player = Player.m_localPlayer;
		if (player == null || !FuelEnabled || !m_nview.IsValid() || !SnakeWoodPlugin.TakeFuelKey.Value.IsDown()
			|| !player.TakeInput() || player.GetHoverObject() is not GameObject hovered
			|| hovered.GetComponentInParent<HoverLog>() != this || !InUseDistance(player))
		{
			return;
		}
		TakeFuelOut(player);
	}

	public void OnUseStop(Player player)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		m_nview.InvokeRPC("SnakeWood_ReleaseControl", player.GetZDOID().UserID);
		RidingControlsPatch.ResetStance();
		player.AttachStop();
		ResetControls();
	}

	// Vanilla turns the rider to face this component's forward, so it's the frame, not the root.
	public Component GetControlledComponent() => m_frame;

	private float FrameYaw()
	{
		Vector3 forward = m_frame.forward;
		forward.y = 0f;
		return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward).eulerAngles.y : transform.eulerAngles.y;
	}

	public Vector3 GetPosition() => m_attachPoint.position;

	public bool IsValid() => this;

	// Jump is a hop while riding (see JumpHopsPatch); vanilla would treat it as getting off.
	public void QueueHop() => m_hopQueued = true;

	private void ResetControls()
	{
		m_moveInput = Vector3.zero;
		m_lookDir = m_frame.forward;
		m_boost = false;
		m_brake = false;
		m_crouchHeld = false;
		m_hopQueued = false;
	}

	// ---- Users --------------------------------------------------------------------------------

	private long GetUser() => m_nview.IsValid() ? m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L) : 0L;

	private bool IsLocalUser()
	{
		Player player = Player.m_localPlayer;
		if (player == null)
		{
			return false;
		}
		long user = GetUser();
		return user != 0L && user == player.GetZDOID().UserID;
	}

	private bool IsLocallyDriven()
	{
		Player player = Player.m_localPlayer;
		return player != null && ReferenceEquals(player.GetDoodadController(), this) && IsLocalUser();
	}

	// A recorded user only counts while their character is still nearby (same rule as Sadle).
	private void CalculateHaveValidUser()
	{
		m_haveValidUser = false;
		long user = GetUser();
		if (user == 0L)
		{
			return;
		}
		foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
		{
			if (zdo.m_uid.UserID == user)
			{
				m_haveValidUser = Vector3.Distance(zdo.GetPosition(), transform.position) < 10f;
				break;
			}
		}
	}

	// ---- Physics (owner only) -----------------------------------------------------------------

	// Character carries anyone standing on a moving body along in a straight line, but not around
	// its turns, so passengers would slide off whenever the log turns. On each player's own game, a
	// passenger standing on this log is held to their spot on the deck while they stand still (grip
	// through speeding up, braking and turns), and swung round with the log while they walk.
	private float m_lastSeenYaw = float.NaN;
	private bool m_holdingPassenger;
	private Vector3 m_passengerSpot;

	private void CarryPassenger()
	{
		float yaw = FrameYaw();
		float turned = float.IsNaN(m_lastSeenYaw) ? 0f : Mathf.DeltaAngle(m_lastSeenYaw, yaw);
		m_lastSeenYaw = yaw;
		Player player = Player.m_localPlayer;
		if (player == null || player.IsAttached() || player.IsTeleporting() || player.m_lastGroundBody != m_body || !player.IsOnGround())
		{
			m_holdingPassenger = false;
			return;
		}
		Quaternion spin = Quaternion.Euler(0f, turned, 0f);
		player.transform.rotation = spin * player.transform.rotation;
		Vector3 position;
		if (player.m_moveDir.sqrMagnitude > 0.01f)
		{
			// Walking: let them move, just carry them round the turn.
			m_holdingPassenger = false;
			Vector3 pivot = m_body.position;
			position = pivot + spin * (player.m_body.position - pivot);
		}
		else
		{
			if (!m_holdingPassenger)
			{
				m_holdingPassenger = true;
				m_passengerSpot = m_frame.InverseTransformPoint(player.m_body.position);
			}
			position = m_frame.TransformPoint(m_passengerSpot);
			player.m_body.linearVelocity = m_body.GetPointVelocity(position);
		}
		player.m_body.position = position;
		player.transform.position = position;
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		CarryPassenger();
		if (!m_nview.IsOwner())
		{
			FollowOwner();
			return;
		}
		if (m_sync != null)
		{
			m_sync.m_isKinematicBody = false;
		}
		float dt = Time.fixedDeltaTime;
		m_validUserTimer -= dt;
		if (m_validUserTimer <= 0f)
		{
			m_validUserTimer = 1f;
			CalculateHaveValidUser();
		}
		bool driving = IsLocallyDriven();
		if (MountedPortal.Holds(this))
		{
			// In a portal: no flying, no stamina or fuel, until the rider arrives.
			m_body.linearVelocity = Vector3.zero;
			return;
		}
		if (!driving)
		{
			ResetControls();
		}

		if (IsWrecked())
		{
			HoldWreck();
			return;
		}
		m_wreckSettled = false;
		m_body.isKinematic = false;
		m_body.useGravity = false;
		if (driving || Time.time < m_unstickUntil)
		{
			UpdateUnstick(dt);
		}
		else
		{
			SetGhost(false);
		}

		Vector3 pos = m_body.position;
		Vector3 vel = m_body.linearVelocity;
		GetSurface(pos, out float surfaceY, out Vector3 surfaceNormal, out bool overWater, out float waterDepth);
		bool deepWater = overWater && waterDepth > SnakeWoodPlugin.DeepWaterDepth.Value;

		if (driving && !UpdateRiderStamina(dt))
		{
			driving = false;
		}
		if (driving && !UpdateLoad(dt))
		{
			driving = false;
		}
		float performance = driving ? Performance(m_load) : 1f;
		// Out of fuel or overloaded, the log still carries you, but only just: low, slow,
		// no boost or hops.
		bool outOfFuel = driving && !BurnFuel(dt);
		bool sputtering = driving && outOfFuel;
		if (sputtering)
		{
			m_boost = false;
			m_hopQueued = false;
		}

		// Going under the water's surface knocks you off; the log bobs back up on its own.
		if (driving && overWater && m_attachPoint.position.y < surfaceY - 0.3f)
		{
			ThrowRider("You went under and lost your grip");
			driving = false;
		}

		// Vertical. An empty log rests on water like a real one; over deep water a ridden log sinks
		// to skim the surface. Otherwise a damped spring holds the hover height. Well above that
		// (after a hop or off a cliff) it's in the air and falls under its own gravity instead.
		// Hover sinks more gently than the rest, until overloading drags it right down.
		float heightFactor = Mathf.Sqrt(performance);
		float handling = performance;
		bool lowering = m_crouchHeld && SnakeWoodPlugin.Crouch.Value == CrouchAction.Lower;
		float climb = m_v.MaxClimbCfg.Value;
		float hoverHeight = overWater && (!driving || deepWater) ? 0.15f
			: sputtering || lowering ? 0.25f
			: Mathf.Max(0.25f, m_v.HoverHeightCfg.Value * heightFactor);

		// Nose pitch follows where the rider looks (up or down). Nose up rides higher the faster you
		// go, to clear rising ground and bumps; nose down sits lower and dives faster off ledges.
		float pitchInput = 0f;
		if (driving && SnakeWoodPlugin.PitchControl.Value && !lowering)
		{
			Vector3 look = m_lookDir.sqrMagnitude > 0.001f ? m_lookDir.normalized : Vector3.zero;
			// A dead zone around the camera's usual slight downward look keeps normal riding level.
			const float deadZone = 0.15f;
			float y = look.y;
			pitchInput = Mathf.Abs(y) <= deadZone ? 0f : Mathf.Clamp((y - Mathf.Sign(y) * deadZone) / 0.45f, -1f, 1f);
		}
		float speedAhead = Mathf.Max(0f, Vector3.Dot(vel, Quaternion.Euler(0f, m_yaw, 0f) * Vector3.forward));
		if (pitchInput > 0f)
		{
			hoverHeight += Mathf.Min(3f, pitchInput * speedAhead * 0.25f);
		}
		else if (pitchInput < 0f)
		{
			hoverHeight = Mathf.Max(0.3f, hoverHeight * (1f + pitchInput * 0.4f));
		}
		float error = surfaceY + hoverHeight - pos.y;
		float airGravity = Physics.gravity.y * m_v.FallGravityCfg.Value * (1f + Mathf.Max(0f, -pitchInput) * 1.5f);
		if (m_airborne && (vel.y <= 0f && error > -0.2f))
		{
			m_airborne = false;
		}
		bool inAir = m_airborne || error < -FreeFallMargin;
		if (!inAir)
		{
			m_airHopsUsed = 0;
		}
		vel.y += (inAir ? airGravity : Mathf.Clamp(error * 14f - vel.y * 6f, -40f, 40f)) * dt;
		if (m_hopQueued)
		{
			m_hopQueued = false;
			TryHop(ref vel, inAir, airGravity, performance);
		}

		// Steering.
		float throttle = Mathf.Clamp(m_moveInput.z, -1f, 1f);
		float side = Mathf.Clamp(m_moveInput.x, -1f, 1f);
		float turnSpeed = m_v.TurnSpeedCfg.Value * handling;
		float turnInput;
		float strafe = 0f;
		if (SnakeWoodPlugin.Steering.Value == SteeringMode.Keys || !driving)
		{
			turnInput = side;
		}
		else
		{
			Vector3 look = m_lookDir;
			look.y = 0f;
			float delta = look.sqrMagnitude > 0.001f
				? Mathf.DeltaAngle(m_yaw, Quaternion.LookRotation(look).eulerAngles.y)
				: 0f;
			turnInput = Mathf.Clamp(delta / 30f, -1f, 1f);
			strafe = side;
		}
		m_yaw += turnInput * turnSpeed * dt;

		// Horizontal: drive along the log's heading, with grip bleeding off sideways slide.
		Quaternion yawRot = Quaternion.Euler(0f, m_yaw, 0f);
		Vector3 forward = yawRot * Vector3.forward;
		Vector3 right = yawRot * Vector3.right;
		float baseSpeed = m_v.MaxSpeedCfg.Value * (deepWater ? m_v.DeepWaterSpeedCfg.Value : 1f) * (sputtering ? 0.3f : 1f)
			* performance * (lowering ? 0.35f : 1f);
		bool boosting = m_boost && throttle > 0f && driving && UseBoostStamina(dt);
		float topSpeed = baseSpeed * (boosting ? m_v.BoostCfg.Value : 1f);
		float targetForward = throttle >= 0f ? throttle * topSpeed : throttle * baseSpeed * 0.4f;
		float targetSide = strafe * baseSpeed * 0.5f;
		bool braking = m_brake || (m_crouchHeld && SnakeWoodPlugin.Crouch.Value == CrouchAction.Brake);
		if (braking)
		{
			targetForward = 0f;
			targetSide = 0f;
		}
		float accel = m_v.AccelerationCfg.Value * handling;
		float brake = accel * (braking ? 3f : 1.5f) * (deepWater ? 2f : 1f);
		float forwardSpeed = Vector3.Dot(vel, forward);
		float sideSpeed = Vector3.Dot(vel, right);
		forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetForward,
			(Mathf.Abs(targetForward) > Mathf.Abs(forwardSpeed) && Mathf.Sign(targetForward) == Mathf.Sign(forwardSpeed) ? accel : brake) * dt);
		sideSpeed = Mathf.MoveTowards(sideSpeed, targetSide, (targetSide != 0f ? accel : m_v.Grip) * dt);
		if (!inAir && TooSteepAhead(pos, surfaceY, forward * Mathf.Sign(forwardSpeed), Mathf.Abs(forwardSpeed), climb))
		{
			// Too steep to climb: stall, with a little slide back down.
			forwardSpeed = -Mathf.Sign(forwardSpeed) * 0.5f;
		}
		Vector3 horizontal = forward * forwardSpeed + right * sideSpeed;
		m_body.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
		m_driveVelocity = m_body.linearVelocity;

		// Rotation: heading, a lean into turns, and a partial tilt to match the ground.
		float targetRoll = -turnInput * Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(1f, m_v.MaxSpeedCfg.Value)) * 18f;
		m_roll = Mathf.Lerp(m_roll, targetRoll, 4f * dt);
		// The nose never tips further than the log can climb, from look pitch or from the ground.
		m_pitch = Mathf.Lerp(m_pitch, pitchInput * Mathf.Min(SnakeWoodPlugin.MaxPitch.Value, climb), 4f * dt);
		float tilt = inAir ? 0f : 0.6f;
		float groundAngle = Vector3.Angle(Vector3.up, surfaceNormal);
		if (groundAngle * tilt > climb)
		{
			tilt = climb / groundAngle;
		}
		Vector3 up = Vector3.Slerp(Vector3.up, surfaceNormal, tilt);
		Quaternion target = Quaternion.FromToRotation(Vector3.up, up) * yawRot * Quaternion.Euler(-m_pitch, 0f, m_roll);
		m_body.MoveRotation(Quaternion.Slerp(m_body.rotation, target * m_disguiseOffset, 8f * dt));
		m_body.angularVelocity = Vector3.zero;

		if (driving)
		{
			UpdatePoisonCloud(dt);
			MakeNoise(Mathf.Abs(forwardSpeed));
		}
	}

	// Creatures hear a log coming: a little idling, a lot at full tilt.
	private void MakeNoise(float speed)
	{
		Player player = Player.m_localPlayer;
		float scale = m_v.NoiseCfg.Value;
		if (player == null || scale <= 0f)
		{
			return;
		}
		float t = Mathf.Clamp01(speed / Mathf.Max(1f, m_v.MaxSpeedCfg.Value));
		float range = Mathf.Lerp(SnakeWoodPlugin.NoiseAtRest.Value, SnakeWoodPlugin.NoiseAtSpeed.Value, t) * scale;
		if (range > 0f)
		{
			player.AddNoise(range);
		}
	}

	// Whether the ground just ahead (in the direction of travel) rises more steeply than the log
	// can climb. Only terrain counts: trees, rocks and walls are for crashing into (or ramming), and
	// water ahead is flat.
	private bool TooSteepAhead(Vector3 pos, float surfaceY, Vector3 direction, float speed, float climb)
	{
		if (speed < 0.2f || float.IsNegativeInfinity(surfaceY))
		{
			return false;
		}
		float distance = 1.2f + 0.5f * m_v.Length + speed * 0.1f;
		Vector3 ahead = pos + direction * distance;
		ahead.y = surfaceY + distance * 4f + 1f;
		if (s_terrainMask == 0)
		{
			s_terrainMask = LayerMask.GetMask("terrain");
		}
		float height = Physics.Raycast(ahead, Vector3.down, out RaycastHit hit, distance * 8f + 2f, s_terrainMask, QueryTriggerInteraction.Ignore)
			? hit.point.y
			: float.NegativeInfinity;
		if (float.IsNegativeInfinity(height) || (ZoneSystem.instance != null && ZoneSystem.instance.m_waterLevel >= height))
		{
			return false;
		}
		return Mathf.Atan2(height - surfaceY, distance) * Mathf.Rad2Deg > climb;
	}

	// A hop launches the log to Hop Height above where it is, under the same gravity it falls with,
	// so the hover spring can't drag it straight back down. Air hops work the same way in mid-air.
	private void TryHop(ref Vector3 vel, bool inAir, float airGravity, float heightFactor)
	{
		float height = m_v.HopHeightCfg.Value * heightFactor;
		if (height <= 0f || (inAir && m_airHopsUsed >= m_v.AirHopsCfg.Value))
		{
			return;
		}
		Player player = Player.m_localPlayer;
		float cost = SnakeWoodPlugin.HopStamina.Value;
		if (player != null && cost > 0f)
		{
			if (!player.HaveStamina(cost))
			{
				Hud.instance?.StaminaBarEmptyFlash();
				return;
			}
			player.UseStamina(cost);
		}
		if (inAir)
		{
			m_airHopsUsed++;
		}
		vel.y = Mathf.Max(vel.y, Mathf.Sqrt(2f * -airGravity * height));
		m_airborne = true;
	}

	// Riding takes a steady trickle of stamina to keep your balance; run dry and you fall off.
	private bool UpdateRiderStamina(float dt)
	{
		Player player = Player.m_localPlayer;
		float drain = m_v.RideStaminaCfg.Value;
		if (player == null || drain <= 0f)
		{
			return true;
		}
		if (player.GetStamina() <= 0f)
		{
			ThrowRider("You're too tired to hold on");
			return false;
		}
		// Eitr, if you have any, steadies the log: a slow trickle of it takes most of the strain.
		float eitr = SnakeWoodPlugin.EitrPerSecond.Value * dt;
		if (eitr > 0f && player.GetMaxEitr() > 0f && player.HaveEitr(eitr))
		{
			player.UseEitr(eitr);
			drain *= SnakeWoodPlugin.EitrStaminaFactor.Value;
		}
		player.UseStamina(drain * dt);
		return true;
	}

	private bool UseBoostStamina(float dt)
	{
		Player player = Player.m_localPlayer;
		float drain = SnakeWoodPlugin.BoostStamina.Value;
		if (player == null || drain <= 0f)
		{
			return true;
		}
		if (player.GetStamina() <= drain * dt)
		{
			return false;
		}
		player.UseStamina(drain * dt);
		return true;
	}

	// Knocks the local rider off, keeping some of the log's momentum and a little bounce.
	private void ThrowRider(string message)
	{
		Player player = Player.m_localPlayer;
		if (player == null || !ReferenceEquals(player.GetDoodadController(), this))
		{
			return;
		}
		Vector3 velocity = m_body.linearVelocity;
		player.StopDoodadControl();
		player.AttachStop();
		player.m_body.linearVelocity = velocity * 0.7f + Vector3.up * 3f;
		player.Message(MessageHud.MessageType.Center, message);
	}

	// Height and normal of whatever's under the log (ground, build pieces or the water surface),
	// whether that's water, and how deep the water is down to the sea bed.
	private void GetSurface(Vector3 pos, out float height, out Vector3 normal, out bool overWater, out float waterDepth)
	{
		height = float.NegativeInfinity;
		normal = Vector3.up;
		int count = Physics.RaycastNonAlloc(pos + Vector3.up * 1f, Vector3.down, s_hits, 60f, s_groundMask, QueryTriggerInteraction.Ignore);
		float best = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			RaycastHit hit = s_hits[i];
			if (hit.collider.attachedRigidbody == m_body || hit.distance >= best)
			{
				continue;
			}
			best = hit.distance;
			height = hit.point.y;
			normal = hit.normal;
		}
		overWater = false;
		waterDepth = 0f;
		ZoneSystem zones = ZoneSystem.instance;
		if (zones == null)
		{
			return;
		}
		float seabed = zones.GetGroundHeight(pos);
		if (float.IsNegativeInfinity(height))
		{
			height = seabed;
		}
		if (zones.m_waterLevel > height)
		{
			overWater = true;
			waterDepth = zones.m_waterLevel - Mathf.Min(seabed, height);
			height = zones.m_waterLevel;
			normal = Vector3.up;
		}
	}

	// ---- Ancient Bark's poison cloud --------------------------------------------------------------

	private void UpdatePoisonCloud(float dt)
	{
		if (m_v.Fx != TrailFx.Poison || m_v.PoisonCloudCfg == null || m_v.PoisonCloudCfg.Value <= 0f)
		{
			return;
		}
		Vector3 pos = transform.position;
		if (m_cloud.Count == 0 || Vector3.Distance(m_cloud[m_cloud.Count - 1].pos, pos) > 1.5f)
		{
			m_cloud.Add((pos, Time.time));
		}
		m_cloud.RemoveAll(p => Time.time - p.time > PoisonCloud.Lifetime);
		m_cloudTimer -= dt;
		if (m_cloudTimer > 0f)
		{
			return;
		}
		m_cloudTimer = 0.5f;
		Player rider = Player.m_localPlayer;
		foreach ((Vector3 point, float _) in m_cloud)
		{
			s_inCloud.Clear();
			Character.GetCharactersInRange(point, PoisonCloud.Radius, s_inCloud);
			foreach (Character character in s_inCloud)
			{
				if (character == rider || character.IsDead() || !CanHarm(character))
				{
					continue;
				}
				int id = character.GetInstanceID();
				if (m_lastCloudHit.TryGetValue(id, out float last) && Time.time - last < 1f)
				{
					continue;
				}
				m_lastCloudHit[id] = Time.time;
				HitData hit = new();
				hit.m_damage.m_poison = m_v.PoisonCloudCfg.Value;
				hit.m_point = character.GetCenterPoint();
				hit.m_dir = Vector3.up;
				hit.SetAttacker(rider);
				character.Damage(hit);
			}
		}
	}

	private static bool CanHarm(Character character) =>
		SnakeWoodPlugin.RamFriendlies.Value || !(character.IsPlayer() || character.IsTamed());

	// ---- Ramming --------------------------------------------------------------------------------

	private void OnCollisionEnter(Collision collision)
	{
		if (m_v == null || !m_nview.IsValid() || !m_nview.IsOwner() || !IsLocallyDriven())
		{
			return;
		}
		Ram(collision);
		if (m_nview.IsValid())
		{
			CheckCrash(collision);
		}
	}

	// Slamming into something solid (not a creature) hard enough throws the rider off. Only the
	// sideways part of the impact counts, so landings don't.
	private void CheckCrash(Collision collision)
	{
		if (collision.collider.GetComponentInParent<Character>() != null)
		{
			return;
		}
		Vector3 impulse = collision.impulse;
		impulse.y = 0f;
		float hit = impulse.magnitude / m_body.mass;
		// Bumps wear a little; real crashes a lot.
		AddWear(Mathf.Max(0f, hit - 3f) * 3f * SnakeWoodPlugin.CrashWear.Value,
			collision.contactCount > 0 ? collision.GetContact(0).point : null);
		float crashSpeed = m_v.CrashSpeedCfg.Value;
		if (crashSpeed > 0f && hit >= crashSpeed)
		{
			ThrowRider("You crashed!");
		}
	}

	private void Ram(Collision collision)
	{
		float damage = m_v.RamDamageCfg.Value;
		// Only the log's own speed into the target counts, so things running into a parked or
		// stopped log don't get hurt by it.
		Vector3 contact = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.bounds.center;
		Vector3 toTarget = contact - m_body.position;
		toTarget.y = 0f;
		float speed = toTarget.sqrMagnitude > 0.0001f ? Vector3.Dot(m_driveVelocity, toTarget.normalized) : 0f;
		float minSpeed = Mathf.Max(3f, m_v.MaxSpeedCfg.Value * 0.35f);
		if (damage <= 0f || speed < minSpeed)
		{
			return;
		}
		IDestructible? destructible = collision.collider.GetComponentInParent<IDestructible>();
		if (destructible is not Component target || target.transform.IsChildOf(transform))
		{
			return;
		}
		Player rider = Player.m_localPlayer;
		if (target.gameObject == rider.gameObject)
		{
			return;
		}
		if (target is Character character && !CanHarm(character))
		{
			return;
		}
		if (target is WearNTear && !SnakeWoodPlugin.RamBuildings.Value)
		{
			return;
		}
		int id = target.GetInstanceID();
		if (m_lastRamTime.TryGetValue(id, out float last) && Time.time - last < RamCooldown)
		{
			return;
		}
		m_lastRamTime[id] = Time.time;

		float topSpeed = m_v.MaxSpeedCfg.Value * m_v.BoostCfg.Value;
		float factor = Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(minSpeed, topSpeed, speed));
		float amount = damage * factor;
		HitData hit = new();
		hit.m_damage.m_blunt = amount;
		hit.m_damage.m_chop = amount * m_v.RamChop;
		hit.m_damage.m_pickaxe = amount * m_v.RamPickaxe;
		hit.m_damage.m_fire = m_v.RamFire * factor;
		hit.m_damage.m_frost = m_v.RamFrost * factor;
		hit.m_damage.m_poison = m_v.RamPoison * factor;
		hit.m_toolTier = (short)m_v.RamToolTier;
		hit.m_pushForce = m_v.RamPush * factor;
		hit.m_point = collision.contactCount > 0 ? collision.GetContact(0).point : target.transform.position;
		Vector3 dir = m_body.linearVelocity;
		dir.y = 0f;
		hit.m_dir = dir.sqrMagnitude > 0.01f ? dir.normalized : m_frame.forward;
		hit.SetAttacker(rider);
		destructible.Damage(hit);
		if (m_v.RamKeepSpeed > 0f)
		{
			// A heavy ram ploughs on instead of stopping dead against what it hit.
			Vector3 keep = m_driveVelocity * m_v.RamKeepSpeed;
			m_body.linearVelocity = new Vector3(keep.x, m_body.linearVelocity.y, keep.z);
		}
		// Tool-like rams (felling, mining) are built for it and wear slower.
		float wearShare = m_v.RamChop > 0f || m_v.RamPickaxe > 0f ? 0.02f : 0.05f;
		AddWear(amount * wearShare * SnakeWoodPlugin.RamWear.Value, hit.m_point);
	}

	private void OnDestroy()
	{
		Player player = Player.m_localPlayer;
		if (player != null && ReferenceEquals(player.GetDoodadController(), this) && !MountedPortal.Holds(this))
		{
			player.StopDoodadControl();
			player.AttachStop();
		}
	}
}
