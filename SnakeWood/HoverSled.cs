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

	public float TopSpeed()
	{
		List<float> speeds = Team().Where(a => a.Pulling)
			.Select(a => (Species(a.Prefab)?.Speed ?? 6f) * (1f + SnakeWoodPlugin.StarSpeedBonus.Value * a.Stars)).ToList();
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

	public void HitchOrUnhitch(Player player)
	{
		if (m_log.HasRider)
		{
			player.Message(MessageHud.MessageType.Center, "Get everyone off the sled first");
			return;
		}
		m_nview.ClaimOwnership();
		int free = -1, last = -1;
		for (int i = 0; i < Slots; i++)
		{
			if (Get(i) == null)
			{
				free = free < 0 ? i : free;
			}
			else
			{
				last = i;
			}
		}
		if (free >= 0 && FindCandidate() is Character creature)
		{
			Hitch(free, creature, player);
			return;
		}
		if (last >= 0)
		{
			Unhitch(last, player);
			return;
		}
		player.Message(MessageHud.MessageType.Center,
			"Bring a tamed wolf close, or harpoon a hare and drag it right up to the sled");
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
		return text + $"\n[<color=yellow><b>{SnakeWoodPlugin.HitchKey.Value}</b></color>] Hitch / unhitch";
	}

	// ---- The team you see --------------------------------------------------------------------------

	// Each hitched animal is shown as a copy of its own model, stripped to a picture and set running
	// in harness. Rebuilt whenever the team changes; the run animation follows the sled's speed.
	private int m_shownRev = -1;
	private readonly Animator?[] m_animators = new Animator?[Slots];
	private Vector3 m_lastPos;
	private static GameObject? s_scratch;

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
			if (m_animators[i] is Animator animator && animator)
			{
				bool pulling = Get(i) is Animal a && a.Pulling;
				animator.SetFloat("forward_speed", pulling ? Mathf.Min(speed, 12f) : 0f);
				animator.SetBool("onGround", true);
			}
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
			GameObject visual = Instantiate(prefab, s_scratch.transform, false);
			HoverLogPrefabs.StripToVisual(visual);
			visual.transform.SetParent(m_slots[i], false);
			visual.transform.localPosition = Vector3.zero;
			visual.transform.localRotation = Quaternion.identity;
			visual.transform.localScale = Vector3.one * (1f + 0.06f * animal.Stars);
			foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.layer = gameObject.layer;
			}
			if (visual.GetComponentInChildren<Animator>(true) is Animator animator)
			{
				// The real creature's scripts are gone, so its animation events would have no receiver.
				animator.fireEvents = false;
				m_animators[i] = animator;
			}
		}
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
				if (parts.Length == 3 && float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float speed)
					&& float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float strength))
				{
					s_species[parts[0].Trim()] = new SledSpecies { Prefab = parts[0].Trim(), Speed = speed, Strength = strength };
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
