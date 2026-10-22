using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnakeWood;

// A hover sled: a HoverLog with a team of real animals in harness, a cart-sized chest and a
// passenger seat. Tamed wolves are hitched by bringing them near; hares, which can't be tamed,
// are harpooned and dragged in. A hitched animal leaves the world and lives in the sled's ZDO
// (prefab, level, health, name) until it's unhitched, so its stars and wounds carry over. The team
// sets the sled's speed and pulling strength, takes the knocks the sled takes, and is healed by
// feeding. HoverLog asks this component for all of that.
public class HoverSled : MonoBehaviour
{
	public const int Slots = 2;

	public Transform[] m_slots = new Transform[Slots];
	public Transform m_passengerSeat = null!;
	public Container? m_storage;

	private ZNetView m_nview = null!;
	private HoverLog m_log = null!;

	private static readonly int s_revKey = "SnakeSled_rev".GetStableHashCode();
	private static int Key(int slot, string field) => $"SnakeSled_{slot}_{field}".GetStableHashCode();

	public struct Animal
	{
		public string Prefab;
		public int Level;
		public float Health;
		public float MaxHealth;
		public string Name;
		public int Stars => Mathf.Max(0, Level - 1);
		public bool Pulling => Health > 0f;
	}

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_log = GetComponent<HoverLog>();
		if (m_nview.GetZDO() == null)
		{
			enabled = false;
			return;
		}
		m_nview.Register<int, float>("SnakeSled_Heal", RPC_Heal);
	}

	// ---- Team data (in the ZDO) ----------------------------------------------------------------

	public Animal? Get(int slot)
	{
		if (!m_nview.IsValid())
		{
			return null;
		}
		ZDO zdo = m_nview.GetZDO();
		string prefab = zdo.GetString(Key(slot, "prefab"));
		if (prefab.Length == 0)
		{
			return null;
		}
		return new Animal
		{
			Prefab = prefab,
			Level = zdo.GetInt(Key(slot, "level"), 1),
			Health = zdo.GetFloat(Key(slot, "hp"), 1f),
			MaxHealth = zdo.GetFloat(Key(slot, "max"), 1f),
			Name = zdo.GetString(Key(slot, "name")),
		};
	}

	public IEnumerable<Animal> Team()
	{
		for (int i = 0; i < Slots; i++)
		{
			if (Get(i) is Animal animal)
			{
				yield return animal;
			}
		}
	}

	// Owner only.
	private void Set(int slot, Animal? animal)
	{
		ZDO zdo = m_nview.GetZDO();
		zdo.Set(Key(slot, "prefab"), animal?.Prefab ?? "");
		zdo.Set(Key(slot, "level"), animal?.Level ?? 0);
		zdo.Set(Key(slot, "hp"), animal?.Health ?? 0f);
		zdo.Set(Key(slot, "max"), animal?.MaxHealth ?? 0f);
		zdo.Set(Key(slot, "name"), animal?.Name ?? "");
		zdo.Set(s_revKey, zdo.GetInt(s_revKey) + 1);
	}

	private void SetHealth(int slot, float health)
	{
		if (Get(slot) is Animal animal)
		{
			m_nview.GetZDO().Set(Key(slot, "hp"), Mathf.Clamp(health, 0f, animal.MaxHealth));
		}
	}

	// ---- What the team gives the sled ----------------------------------------------------------

	private static SledSpecies? Species(string prefab) => SledSpecies.Get(prefab);

	public bool CanPull => Team().Any(a => a.Pulling);

	public float TopSpeed(bool onWater = false)
	{
		List<float> speeds = Team().Where(a => a.Pulling)
			.Select(a => Species(a.Prefab) is SledSpecies sp
				? sp.Speed * (onWater ? sp.Water : sp.Land) * (1f + SnakeWoodPlugin.StarSpeedBonus.Value * a.Stars)
				: 6f).ToList();
		if (speeds.Count == 0)
		{
			return 0f;
		}
		// One animal can't hold the pace of a pair.
		return speeds.Average() * (speeds.Count < Slots ? 0.8f : 1f);
	}

	public float Strength() => Team().Where(a => a.Pulling)
		.Sum(a => (Species(a.Prefab)?.Strength ?? 50f) * (1f + SnakeWoodPlugin.StarStrengthBonus.Value * a.Stars));

	public float Acceleration() => 3f + Strength() / 30f;

	public float CargoWeight() => m_storage != null && m_storage.GetInventory() != null ? m_storage.GetInventory().GetTotalWeight() : 0f;

	// Crashes and rams hurt the team instead of wearing the sled. Owner only. Exhausted animals
	// stop pulling until they're fed; they never die in harness.
	public void Hurt(float amount)
	{
		amount *= SnakeWoodPlugin.TeamDamage.Value;
		List<int> pulling = Enumerable.Range(0, Slots).Where(i => Get(i) is Animal a && a.Pulling).ToList();
		if (amount <= 0f || pulling.Count == 0)
		{
			return;
		}
		foreach (int slot in pulling)
		{
			Animal animal = Get(slot)!.Value;
			float health = animal.Health - amount / pulling.Count;
			SetHealth(slot, health);
			if (health <= 0f)
			{
				Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
					Localization.instance.Localize($"{DisplayName(animal)} is exhausted! Feed it to get it pulling again"));
			}
		}
	}

	// A slow recovery while the sled stands still. Owner only.
	private float m_regenTimer;

	private void FixedUpdate()
	{
		MoveTeamBody();
		if (!m_nview.IsValid() || !m_nview.IsOwner() || (m_regenTimer += Time.fixedDeltaTime) < 2f)
		{
			return;
		}
		float seconds = m_regenTimer;
		m_regenTimer = 0f;
		if (m_log.Speed > 0.5f || SnakeWoodPlugin.TeamRegen.Value <= 0f)
		{
			return;
		}
		for (int i = 0; i < Slots; i++)
		{
			if (Get(i) is Animal a && a.Health < a.MaxHealth)
			{
				SetHealth(i, a.Health + a.MaxHealth * SnakeWoodPlugin.TeamRegen.Value / 100f * seconds / 60f);
			}
		}
	}

	// ---- Feeding ----------------------------------------------------------------------------------

	// Shift+E with food: each hurt animal that eats something you're carrying gets one item.
	public bool TryFeed(Player player)
	{
		bool fed = false;
		for (int i = 0; i < Slots; i++)
		{
			if (Get(i) is not Animal animal || animal.Health >= animal.MaxHealth || Species(animal.Prefab) is not SledSpecies species)
			{
				continue;
			}
			foreach (ItemDrop food in species.Food())
			{
				string name = food.m_itemData.m_shared.m_name;
				if (player.GetInventory().CountItems(name) <= 0)
				{
					continue;
				}
				player.GetInventory().RemoveItem(name, 1);
				m_nview.InvokeRPC("SnakeSled_Heal", i, animal.MaxHealth * SnakeWoodPlugin.FeedHeal.Value);
				player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"Fed {DisplayName(animal)} {name}"));
				fed = true;
				break;
			}
		}
		return fed;
	}

	public bool AnyHungry(Player player) => Team().Any(a => a.Health < a.MaxHealth && Species(a.Prefab) is SledSpecies s
		&& s.Food().Any(f => player.GetInventory().CountItems(f.m_itemData.m_shared.m_name) > 0));

	private void RPC_Heal(long sender, int slot, float amount)
	{
		if (m_nview.IsOwner() && Get(slot) is Animal animal)
		{
			SetHealth(slot, animal.Health + amount);
		}
	}

	// ---- Hitching ---------------------------------------------------------------------------------

	public void Hitch(Player player)
	{
		if (m_log.HasRider)
		{
			player.Message(MessageHud.MessageType.Center, "Get everyone off the sled first");
			return;
		}
		int free = Enumerable.Range(0, Slots).Where(i => Get(i) == null).DefaultIfEmpty(-1).First();
		if (free < 0)
		{
			player.Message(MessageHud.MessageType.Center, $"The team is full ([{SnakeWoodPlugin.UnhitchKey.Value}] to unhitch one)");
			return;
		}
		if (FindCandidate() is not Character creature)
		{
			player.Message(MessageHud.MessageType.Center,
				"Bring a tamed wolf close, or harpoon a hare and drag it right up to the sled");
			return;
		}
		m_nview.ClaimOwnership();
		Hitch(free, creature, player);
	}

	// Lets the last animal hitched go.
	public void Unhitch(Player player)
	{
		if (m_log.HasRider)
		{
			player.Message(MessageHud.MessageType.Center, "Get everyone off the sled first");
			return;
		}
		int last = Enumerable.Range(0, Slots).Where(i => Get(i) != null).DefaultIfEmpty(-1).Last();
		if (last < 0)
		{
			player.Message(MessageHud.MessageType.Center, "Nothing is hitched");
			return;
		}
		m_nview.ClaimOwnership();
		Unhitch(last, player);
	}

	// The nearest creature that can be put in harness: tamable ones only once tamed (within 6 m),
	// untamable ones (hares) only when dragged right up to the sled (within 3 m).
	private Character? FindCandidate()
	{
		List<Character> nearby = new();
		Character.GetCharactersInRange(transform.position, 6f, nearby);
		return nearby
			.Where(c => !c.IsPlayer() && !c.IsDead() && Species(Utils.GetPrefabName(c.gameObject)) != null)
			.Where(c => c.GetComponent<Tameable>() != null
				? c.IsTamed()
				: Vector3.Distance(c.transform.position, transform.position) <= 3f)
			.OrderBy(c => Vector3.Distance(c.transform.position, transform.position))
			.FirstOrDefault();
	}

	private void Hitch(int slot, Character creature, Player player)
	{
		Animal animal = new()
		{
			Prefab = Utils.GetPrefabName(creature.gameObject),
			Level = creature.GetLevel(),
			Health = creature.GetHealth(),
			MaxHealth = creature.GetMaxHealth(),
			Name = creature.GetComponent<Tameable>() is Tameable tameable ? tameable.GetText() : "",
		};
		Set(slot, animal);
		if (creature.GetComponent<ZNetView>() is ZNetView view && view.IsValid())
		{
			view.ClaimOwnership();
			ZNetScene.instance.Destroy(creature.gameObject);
		}
		player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"{DisplayName(animal)} is hitched to the sled"));
	}

	private void Unhitch(int slot, Player player)
	{
		Animal animal = Get(slot)!.Value;
		Set(slot, null);
		GameObject? prefab = ZNetScene.instance.GetPrefab(animal.Prefab);
		if (prefab == null)
		{
			return;
		}
		Vector3 pos = m_slots[slot].position + Vector3.up * 0.3f;
		GameObject spawned = Instantiate(prefab, pos, Quaternion.LookRotation(transform.forward));
		if (spawned.GetComponent<Character>() is Character character)
		{
			character.SetLevel(animal.Level);
			character.SetMaxHealth(animal.MaxHealth);
			character.SetHealth(Mathf.Max(1f, animal.Health));
			if (spawned.GetComponent<Tameable>() is Tameable tameable)
			{
				character.SetTamed(true);
				if (animal.Name.Length > 0)
				{
					tameable.SetText(animal.Name);
				}
			}
		}
		player.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"{DisplayName(animal)} is unhitched"));
	}

	public static string DisplayName(Animal animal)
	{
		if (animal.Name.Length > 0)
		{
			return animal.Name;
		}
		GameObject? prefab = ZNetScene.instance?.GetPrefab(animal.Prefab);
		return prefab?.GetComponent<Character>()?.m_name ?? animal.Prefab;
	}

	// ---- Hover text -----------------------------------------------------------------------------

	public string TeamText()
	{
		string text = "";
		foreach (Animal animal in Team())
		{
			string stars = animal.Stars > 0 ? " " + new string('★', animal.Stars) : "";
			string state = animal.Pulling ? $"{animal.Health:0}/{animal.MaxHealth:0}" : "<color=red>exhausted</color>";
			text += $"\n{DisplayName(animal)}{stars}: {state}";
		}
		if (text.Length == 0)
		{
			text = "\n<color=#888888>No team: hitch tamed wolves or harpooned hares</color>";
		}
		return text + $"\n[<color=yellow><b>{SnakeWoodPlugin.HitchKey.Value}</b></color>] Hitch  [<color=yellow><b>{SnakeWoodPlugin.UnhitchKey.Value}</b></color>] Unhitch";
	}

	// ---- The team you see --------------------------------------------------------------------------

	// Each hitched animal is shown as a copy of its own model, stripped to a picture and set running
	// in harness. Rebuilt whenever the team changes; the run animation follows the sled's speed.
	// The pictures don't tilt with the sled: they stand level on the ground under their harness
	// slot (or swim, in water), and an exhausted animal staggers along instead of running.
	private int m_shownRev = -1;
	private readonly Animator?[] m_animators = new Animator?[Slots];
	private readonly Transform?[] m_visuals = new Transform?[Slots];
	private readonly float[] m_staggerTimer = new float[Slots];
	// Each creature's own swimming wake, from its Character, and the live copies of it.
	private readonly EffectList?[] m_wake = new EffectList?[Slots];
	private readonly GameObject[]?[] m_wakeInstances = new GameObject[]?[Slots];
	private Vector3 m_lastPos;
	private static GameObject? s_scratch;
	private static int s_footMask;

	private void Update()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		int rev = m_nview.GetZDO().GetInt(s_revKey);
		if (rev != m_shownRev)
		{
			m_shownRev = rev;
			RebuildTeam();
		}
		float speed = Time.deltaTime > 0f ? Vector3.Distance(transform.position, m_lastPos) / Time.deltaTime : 0f;
		m_lastPos = transform.position;
		for (int i = 0; i < Slots; i++)
		{
			if (m_animators[i] is not Animator animator || !animator || Get(i) is not Animal animal)
			{
				continue;
			}
			bool swimming = m_visuals[i] is Transform visual && InWater(visual.position + Vector3.up * 0.3f);
			SetParam(i, "forward_speed", animal.Pulling ? Mathf.Min(speed, 12f) : Mathf.Min(speed, 1.5f));
			SetParam(i, "onGround", !swimming);
			SetParam(i, "inWater", swimming);
			if (!animal.Pulling && (m_staggerTimer[i] -= Time.deltaTime) <= 0f)
			{
				m_staggerTimer[i] = 2.5f;
				SetParam(i, "stagger", null);
			}
		}
	}

	// Stand each animal level on whatever is under its harness slot.
	private void LateUpdate()
	{
		if (s_footMask == 0)
		{
			s_footMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
		}
		Quaternion level = Heading();
		for (int i = 0; i < Slots; i++)
		{
			if (m_visuals[i] is not Transform visual || !visual)
			{
				continue;
			}
			Vector3 slot = SlotPosition(i);
			float y = Physics.Raycast(slot + Vector3.up * 2.5f, Vector3.down, out RaycastHit hit, 6f, s_footMask, QueryTriggerInteraction.Ignore)
				? hit.point.y : slot.y;
			float water = Floating.GetLiquidLevel(slot);
			if (water > y + 0.5f)
			{
				y = water - 0.5f;
			}
			Vector3 pos = visual.position;
			pos.x = slot.x;
			pos.z = slot.z;
			pos.y = Mathf.Abs(pos.y - y) > 2f ? y : Mathf.Lerp(pos.y, y, Mathf.Min(1f, 12f * Time.deltaTime));
			visual.SetPositionAndRotation(pos, level);
			if (m_wake[i] is EffectList wake)
			{
				Character.SetupContinuousEffect(visual, new Vector3(pos.x, water, pos.z), water > y, wake, ref m_wakeInstances[i]);
			}
		}
	}

	// Where a harness slot is with the sled's lean taken out: the team stays square to the heading
	// instead of swinging sideways as the sled tilts over the ground.
	private Vector3 SlotPosition(int slot)
	{
		Transform frame = m_log.m_frame;
		Vector3 local = Vector3.Scale(frame.InverseTransformPoint(m_slots[slot].position), frame.lossyScale);
		return frame.position + Heading() * local;
	}

	// The sled's heading, level. Read from the frame: the root is turned for the vanilla-log
	// disguise, so its own angles say nothing useful about where the sled points.
	private Quaternion Heading() => Quaternion.Euler(0f, m_log.FrameYaw(), 0f);

	private static bool InWater(Vector3 point) => Floating.GetLiquidLevel(point) > point.y;

	// Only touch parameters the creature's animator has (a missing one logs a warning every call).
	private readonly Dictionary<string, AnimatorControllerParameterType>?[] m_params = new Dictionary<string, AnimatorControllerParameterType>?[Slots];

	private void SetParam(int slot, string name, object? value)
	{
		if (m_animators[slot] is not Animator animator || m_params[slot] is not { } known || !known.TryGetValue(name, out AnimatorControllerParameterType type))
		{
			return;
		}
		switch (value)
		{
			case float f when type == AnimatorControllerParameterType.Float: animator.SetFloat(name, f); break;
			case bool b when type == AnimatorControllerParameterType.Bool: animator.SetBool(name, b); break;
			case null when type == AnimatorControllerParameterType.Trigger: animator.SetTrigger(name); break;
		}
	}

	// ---- The team's body ----------------------------------------------------------------------------

	// The pictures have no colliders, so the team gets one box of its own: people walking up bump
	// into it and creatures can bite it (the bites hurt the team). It's a separate kinematic body
	// that follows the harness, so it never drags on the ground or pushes the sled itself.
	private GameObject? m_teamBody;
	private Rigidbody? m_teamRigidbody;

	private void UpdateTeamBody()
	{
		bool any = m_visuals.Any(v => v != null);
		if (any && m_teamBody == null)
		{
			m_teamBody = new GameObject("SnakeSled_TeamBody") { layer = gameObject.layer };
			m_teamBody.transform.SetPositionAndRotation(transform.position, transform.rotation);
			m_teamRigidbody = m_teamBody.AddComponent<Rigidbody>();
			m_teamRigidbody.isKinematic = true;
			m_teamRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			BoxCollider box = m_teamBody.AddComponent<BoxCollider>();
			box.size = new Vector3(1.9f, 1f, 1.8f);
			box.center = new Vector3(0f, 0.6f, 0f);
			foreach (Collider own in GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(box, own);
			}
			m_teamBody.AddComponent<SledTeamHitbox>().m_log = m_log;
		}
		if (m_teamBody != null)
		{
			m_teamBody.SetActive(any);
		}
	}

	private void MoveTeamBody()
	{
		if (m_teamRigidbody == null || !m_teamBody!.activeSelf)
		{
			return;
		}
		Vector3 sum = Vector3.zero;
		for (int i = 0; i < Slots; i++)
		{
			Vector3 slot = SlotPosition(i);
			if (m_visuals[i] is Transform visual && visual)
			{
				slot.y = visual.position.y;
			}
			sum += slot;
		}
		m_teamRigidbody.MovePosition(sum / Slots);
		m_teamRigidbody.MoveRotation(Heading());
	}

	private void OnDestroy()
	{
		foreach (GameObject[]? instances in m_wakeInstances)
		{
			foreach (GameObject wake in instances ?? System.Array.Empty<GameObject>())
			{
				if (wake != null)
				{
					Destroy(wake);
				}
			}
		}
		if (m_teamBody != null)
		{
			Destroy(m_teamBody);
		}
	}

	private void RebuildTeam()
	{
		for (int i = 0; i < Slots; i++)
		{
			foreach (Transform child in m_slots[i].Cast<Transform>().ToList())
			{
				Destroy(child.gameObject);
			}
			m_animators[i] = null;
			if (m_wake[i] is EffectList oldWake && m_visuals[i] is Transform oldVisual && oldVisual)
			{
				Character.SetupContinuousEffect(oldVisual, oldVisual.position, false, oldWake, ref m_wakeInstances[i]);
			}
			m_wake[i] = null;
			m_wakeInstances[i] = null;
			m_visuals[i] = null;
			m_params[i] = null;
			if (Get(i) is not Animal animal || ZNetScene.instance.GetPrefab(animal.Prefab) is not GameObject prefab)
			{
				continue;
			}
			if (s_scratch == null)
			{
				// Inactive, so the copy's Character, AI and ZNetView never wake up before they're stripped.
				s_scratch = new GameObject("SnakeSled_Scratch");
				s_scratch.SetActive(false);
				DontDestroyOnLoad(s_scratch);
			}
			m_wake[i] = prefab.GetComponent<Character>()?.m_waterEffects is EffectList wake && wake.HasEffects() ? wake : null;
			GameObject visual = Instantiate(prefab, s_scratch.transform, false);
			HoverLogPrefabs.StripToVisual(visual);
			visual.transform.SetParent(m_slots[i], false);
			visual.transform.localPosition = Vector3.zero;
			visual.transform.localRotation = Quaternion.identity;
			visual.transform.localScale = Vector3.one * (1f + 0.06f * animal.Stars);
			m_visuals[i] = visual.transform;
			foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.layer = gameObject.layer;
			}
			if (visual.GetComponentInChildren<Animator>(true) is Animator animator)
			{
				// The real creature's scripts are gone, so its animation events would have no receiver.
				animator.fireEvents = false;
				m_animators[i] = animator;
				m_params[i] = animator.parameters.ToDictionary(p => p.name, p => p.type);
			}
		}
		UpdateTeamBody();
	}

	// Taking the sled down: its cargo spills out and its team is let go, so nothing is lost.
	public void OnRemoved()
	{
		if (m_storage != null && m_storage.GetInventory() is Inventory inventory)
		{
			foreach (ItemDrop.ItemData item in inventory.GetAllItems().ToList())
			{
				ItemDrop.DropItem(item, item.m_stack, transform.position + Vector3.up * 0.8f, Quaternion.identity);
			}
			inventory.RemoveAll();
		}
		Player? player = Player.m_localPlayer;
		for (int i = 0; i < Slots; i++)
		{
			if (Get(i) != null && player != null)
			{
				Unhitch(i, player);
			}
		}
	}

	// ---- Passenger ----------------------------------------------------------------------------------

	public void SeatPassenger(Player player)
	{
		player.AttachStart(m_passengerSeat, gameObject, hideWeapons: false, isBed: false, onShip: true,
			"attach_chair", new Vector3(0.9f, 0.5f, 0f));
	}
}

// What each hitchable creature brings: "Prefab:Speed:Strength" entries in the Sled Team config.
// Strength is the load it can pull; Speed its share of the sled's top speed. Diet comes from the
// creature's own AI (what tamed ones eat), or the Hare Food setting for animals without one.
public class SledSpecies
{
	public string Prefab = "";
	public float Speed;
	public float Strength;
	public float Land = 1f;
	public float Water = 1f;

	private static string s_parsed = "";
	private static readonly Dictionary<string, SledSpecies> s_species = new();

	public static SledSpecies? Get(string prefab)
	{
		string config = SnakeWoodPlugin.SledAnimals.Value;
		if (config != s_parsed)
		{
			s_parsed = config;
			s_species.Clear();
			foreach (string entry in config.Split(','))
			{
				string[] parts = entry.Split(':');
				float Num(int i, float fallback) => parts.Length > i
					&& float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback;
				if (parts.Length >= 3 && parts[0].Trim().Length > 0)
				{
					s_species[parts[0].Trim()] = new SledSpecies
					{
						Prefab = parts[0].Trim(), Speed = Num(1, 6f), Strength = Num(2, 50f), Land = Num(3, 1f), Water = Num(4, 1f),
					};
				}
			}
		}
		return s_species.TryGetValue(prefab, out SledSpecies species) ? species : null;
	}

	public IEnumerable<ItemDrop> Food()
	{
		GameObject? prefab = ZNetScene.instance?.GetPrefab(Prefab);
		if (prefab?.GetComponent<MonsterAI>() is MonsterAI ai && ai.m_consumeItems != null && ai.m_consumeItems.Count > 0)
		{
			return ai.m_consumeItems.Where(i => i != null);
		}
		return SnakeWoodPlugin.HareFood.Value.Split(',')
			.Select(n => ObjectDB.instance?.GetItemPrefab(n.Trim())?.GetComponent<ItemDrop>())
			.Where(i => i != null).Select(i => i!);
	}
}

// The team's box forwards bites to the sled, which hurts the team (see HoverLog.Damage).
public class SledTeamHitbox : MonoBehaviour, IDestructible
{
	public HoverLog? m_log;

	public DestructibleType GetDestructibleType() => DestructibleType.Default;

	public void Damage(HitData hit)
	{
		if (m_log != null)
		{
			m_log.Damage(hit);
		}
	}
}
