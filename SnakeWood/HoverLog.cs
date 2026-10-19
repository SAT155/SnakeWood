using System.Collections.Generic;
using UnityEngine;

namespace SnakeWood;

// A rideable hover log. Control follows the Lox saddle (Sadle): the rider asks the ZDO owner for
// control, the owner records them as the user and hands them ownership, and from then on the
// rider's own game runs the physics. Everyone else just sees the transform ZSyncTransform syncs.
public class HoverLog : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
	public string m_variant = "";
	public Transform m_attachPoint = null!;
	// Holds the model, collider and seat; the root carries the vanilla-disguise rotation on top.
	public Transform m_frame = null!;

	public Transform Frame => m_frame;
	public Vector3 m_detachOffset = new(0f, 0.6f, -1f);
	public float m_maxUseRange = 4f;

	private const float FreeFallMargin = 1.5f;
	private const float RamCooldown = 0.6f;

	private static readonly RaycastHit[] s_hits = new RaycastHit[16];
	private static int s_groundMask;

	private ZNetView m_nview = null!;
	private Rigidbody m_body = null!;
	private HoverLogVariant m_v = null!;

	// Rider input, written by ApplyControlls on the rider's game.
	private Vector3 m_moveInput;
	private Vector3 m_lookDir;
	private bool m_boost;
	private bool m_brake;
	private bool m_crouchBrake;
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
			m_nview.GetZDO().Set(s_fuelKey, Mathf.Min(SnakeWoodPlugin.FuelCapacity.Value, GetFuel() + amount));
		}
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
		string fuel = FuelEnabled
			? $"\n{FuelName()}: {Mathf.CeilToInt(GetFuel())}/{SnakeWoodPlugin.FuelCapacity.Value}" +
			  $"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Add {FuelName()}"
			: "";
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
		player.StartDoodadControl(this);
		player.AttachStart(m_attachPoint, gameObject, hideWeapons: false, isBed: false, onShip: false,
			SnakeWoodPlugin.AttachAnimation.Value, m_detachOffset);
	}

	// ---- IDoodadController: the rider's game feeds us its input every frame ---------------------

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		m_moveInput = moveDir;
		m_lookDir = lookDir;
		m_boost = run;
		// With Mounted Combat, block raises your shield and Crouch brakes instead (see Update).
		m_brake = block && !SnakeWoodPlugin.MountedCombat.Value;
	}

	private void Update()
	{
		CheckUnansweredRequest();
		m_crouchBrake = IsLocallyDriven() && (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"));
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
		m_crouchBrake = false;
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

	private void FixedUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		float dt = Time.fixedDeltaTime;
		m_validUserTimer -= dt;
		if (m_validUserTimer <= 0f)
		{
			m_validUserTimer = 1f;
			CalculateHaveValidUser();
		}
		bool driving = IsLocallyDriven();
		if (!driving)
		{
			ResetControls();
		}

		m_body.isKinematic = false;
		m_body.useGravity = false;

		Vector3 pos = m_body.position;
		Vector3 vel = m_body.linearVelocity;
		GetSurface(pos, out float surfaceY, out Vector3 surfaceNormal, out bool overWater, out float waterDepth);
		bool deepWater = overWater && waterDepth > SnakeWoodPlugin.DeepWaterDepth.Value;

		if (driving && !UpdateRiderStamina(dt))
		{
			driving = false;
		}
		// Out of fuel the log still carries you, but only just: low, slow, no boost or hops.
		bool sputtering = driving && !BurnFuel(dt);
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
		float hoverHeight = overWater && (!driving || deepWater) ? 0.15f : sputtering ? 0.3f : m_v.HoverHeightCfg.Value;

		// Nose pitch follows where the rider looks (up or down). Nose up rides higher the faster you
		// go, to clear rising ground and bumps; nose down sits lower and dives faster off ledges.
		float pitchInput = 0f;
		if (driving && SnakeWoodPlugin.PitchControl.Value)
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
			TryHop(ref vel, inAir, airGravity);
		}

		// Steering.
		float throttle = Mathf.Clamp(m_moveInput.z, -1f, 1f);
		float side = Mathf.Clamp(m_moveInput.x, -1f, 1f);
		float turnSpeed = m_v.TurnSpeedCfg.Value;
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
		float baseSpeed = m_v.MaxSpeedCfg.Value * (deepWater ? m_v.DeepWaterSpeedCfg.Value : 1f) * (sputtering ? 0.3f : 1f);
		bool boosting = m_boost && throttle > 0f && driving && UseBoostStamina(dt);
		float topSpeed = baseSpeed * (boosting ? m_v.BoostCfg.Value : 1f);
		float targetForward = throttle >= 0f ? throttle * topSpeed : throttle * baseSpeed * 0.4f;
		float targetSide = strafe * baseSpeed * 0.5f;
		bool braking = m_brake || m_crouchBrake;
		if (braking)
		{
			targetForward = 0f;
			targetSide = 0f;
		}
		float accel = m_v.AccelerationCfg.Value;
		float brake = accel * (braking ? 3f : 1.5f) * (deepWater ? 2f : 1f);
		float forwardSpeed = Vector3.Dot(vel, forward);
		float sideSpeed = Vector3.Dot(vel, right);
		forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetForward,
			(Mathf.Abs(targetForward) > Mathf.Abs(forwardSpeed) && Mathf.Sign(targetForward) == Mathf.Sign(forwardSpeed) ? accel : brake) * dt);
		sideSpeed = Mathf.MoveTowards(sideSpeed, targetSide, (targetSide != 0f ? accel : m_v.Grip) * dt);
		Vector3 horizontal = forward * forwardSpeed + right * sideSpeed;
		m_body.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
		m_driveVelocity = m_body.linearVelocity;

		// Rotation: heading, a lean into turns, and a partial tilt to match the ground.
		float targetRoll = -turnInput * Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(1f, m_v.MaxSpeedCfg.Value)) * 18f;
		m_roll = Mathf.Lerp(m_roll, targetRoll, 4f * dt);
		m_pitch = Mathf.Lerp(m_pitch, pitchInput * SnakeWoodPlugin.MaxPitch.Value, 4f * dt);
		Vector3 up = Vector3.Slerp(Vector3.up, surfaceNormal, inAir ? 0f : 0.6f);
		Quaternion target = Quaternion.FromToRotation(Vector3.up, up) * yawRot * Quaternion.Euler(-m_pitch, 0f, m_roll);
		m_body.MoveRotation(Quaternion.Slerp(m_body.rotation, target * HoverLogDisguise.Offset, 8f * dt));
		m_body.angularVelocity = Vector3.zero;

		if (driving)
		{
			UpdatePoisonCloud(dt);
		}
	}

	// A hop launches the log to Hop Height above where it is, under the same gravity it falls with,
	// so the hover spring can't drag it straight back down. Air hops work the same way in mid-air.
	private void TryHop(ref Vector3 vel, bool inAir, float airGravity)
	{
		float height = m_v.HopHeightCfg.Value;
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
		CheckCrash(collision);
	}

	// Slamming into something solid (not a creature) hard enough throws the rider off. Only the
	// sideways part of the impact counts, so landings don't.
	private void CheckCrash(Collision collision)
	{
		float crashSpeed = m_v.CrashSpeedCfg.Value;
		if (crashSpeed <= 0f || collision.collider.GetComponentInParent<Character>() != null)
		{
			return;
		}
		Vector3 impulse = collision.impulse;
		impulse.y = 0f;
		if (impulse.magnitude / m_body.mass >= crashSpeed)
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
		hit.m_damage.m_poison = m_v.RamPoison * factor;
		hit.m_toolTier = (short)m_v.RamToolTier;
		hit.m_pushForce = m_v.RamPush * factor;
		hit.m_point = collision.contactCount > 0 ? collision.GetContact(0).point : target.transform.position;
		Vector3 dir = m_body.linearVelocity;
		dir.y = 0f;
		hit.m_dir = dir.sqrMagnitude > 0.01f ? dir.normalized : m_frame.forward;
		hit.SetAttacker(rider);
		destructible.Damage(hit);
	}

	private void OnDestroy()
	{
		Player player = Player.m_localPlayer;
		if (player != null && ReferenceEquals(player.GetDoodadController(), this))
		{
			player.StopDoodadControl();
			player.AttachStop();
		}
	}
}
