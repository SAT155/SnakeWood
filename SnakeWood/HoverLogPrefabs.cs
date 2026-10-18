using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnakeWood;

// Builds one networked prefab per wood type at startup and adds it to the hammer. The look is
// borrowed from a vanilla pole or beam built from the same wood, laid flat and resized; the
// physics, sync and riding parts are added fresh. No asset bundle needed.
public static class HoverLogPrefabs
{
	private static GameObject? s_holder;
	private static readonly Dictionary<string, GameObject> s_prefabs = new();
	private static Texture2D? s_iconTexture;

	public static IEnumerable<GameObject> Prefabs => s_prefabs.Values;

	// Called from ObjectDB.Awake: the hammer's piece table and the item prefabs both live there.
	public static void Build(ObjectDB db)
	{
		PieceTable? hammer = db.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
		if (hammer == null)
		{
			return;
		}
		CraftingStation? workbench = hammer.m_pieces
			.Select(p => p ? p.GetComponent<CraftingStation>() : null)
			.FirstOrDefault(s => s != null && s.gameObject.name == "piece_workbench");

		foreach (HoverLogVariant variant in HoverLogVariant.All)
		{
			if (!s_prefabs.TryGetValue(variant.Key, out GameObject prefab))
			{
				GameObject? created = Create(variant, db, hammer, workbench);
				if (created == null)
				{
					continue;
				}
				s_prefabs[variant.Key] = prefab = created;
			}
			// Requirements point at this ObjectDB's items, which are recreated with each scene.
			prefab.GetComponent<Piece>().m_resources = Requirements(variant, db);
			if (variant.Enabled.Value && !hammer.m_pieces.Contains(prefab))
			{
				hammer.m_pieces.Add(prefab);
			}
		}
		RegisterWithZNetScene();
	}

	public static void RefreshPieces()
	{
		foreach (HoverLogVariant variant in HoverLogVariant.All)
		{
			if (s_prefabs.TryGetValue(variant.Key, out GameObject prefab))
			{
				Piece piece = prefab.GetComponent<Piece>();
				piece.m_description = variant.Describe();
				if (ObjectDB.instance != null)
				{
					piece.m_resources = Requirements(variant, ObjectDB.instance);
				}
			}
		}
	}

	// ZNetScene can wake before or after ObjectDB; whichever runs second registers the prefabs.
	public static void RegisterWithZNetScene()
	{
		ZNetScene scene = ZNetScene.instance;
		if (scene == null)
		{
			return;
		}
		foreach (GameObject prefab in s_prefabs.Values)
		{
			int hash = prefab.name.GetStableHashCode();
			if (!scene.m_prefabs.Contains(prefab))
			{
				scene.m_prefabs.Add(prefab);
			}
			scene.m_namedPrefabs[hash] = prefab;
		}
	}

	private static GameObject? Create(HoverLogVariant variant, ObjectDB db, PieceTable hammer, CraftingStation? workbench)
	{
		if (db.GetItemPrefab(variant.WoodItem) == null)
		{
			SnakeWoodPlugin.Log.LogWarning($"{variant.Name}: item {variant.WoodItem} isn't in this game version, so this log is skipped.");
			return null;
		}
		GameObject? template = FindTemplate(variant, hammer);
		GameObject? rawWood = db.GetItemPrefab(variant.WoodItem);
		ModelStyle style = SnakeWoodPlugin.Model.Value;
		if (style != ModelStyle.BuildPiece && !HasMesh(rawWood))
		{
			style = ModelStyle.BuildPiece;
		}
		if (style != ModelStyle.RawWood && template == null)
		{
			style = HasMesh(rawWood) ? ModelStyle.RawWood : style;
		}
		if (style == ModelStyle.BuildPiece && template == null)
		{
			SnakeWoodPlugin.Log.LogWarning($"{variant.Name}: no model to borrow, so this log is skipped.");
			return null;
		}
		SnakeWoodPlugin.Log.LogInfo($"{variant.Name}: {style} model from {(style == ModelStyle.BuildPiece ? template!.name : rawWood!.name)}" +
			(style == ModelStyle.Mixed ? $" with a {template!.name} saddle." : "."));

		if (s_holder == null)
		{
			// Inactive, so nothing parented under it runs Awake: these are templates, not live objects.
			s_holder = new GameObject("SnakeWood_Prefabs");
			s_holder.SetActive(false);
			Object.DontDestroyOnLoad(s_holder);
		}

		GameObject prefab = new(variant.PrefabName);
		prefab.transform.SetParent(s_holder.transform, false);
		int vehicleLayer = LayerMask.NameToLayer("vehicle");
		prefab.layer = vehicleLayer;

		GameObject model = Object.Instantiate(style == ModelStyle.BuildPiece ? template! : rawWood!, prefab.transform, false);
		model.name = "visual";
		StripToVisual(model);
		LayFlat(model, prefab.transform, variant);
		if (variant.Tint != Color.white && style == ModelStyle.BuildPiece)
		{
			Tint(model, variant.Tint);
		}
		if (style == ModelStyle.Mixed)
		{
			// The build piece shrunk into a little saddle on top of the raw log.
			GameObject saddle = Object.Instantiate(template!, prefab.transform, false);
			saddle.name = "saddle";
			StripToVisual(saddle);
			float radius = 0.225f * variant.Thickness;
			FitToBox(saddle, prefab.transform,
				new Vector3(2.4f * radius, 0.12f, 0.35f * variant.Length),
				new Vector3(0f, radius + 0.04f, -0.12f * variant.Length));
		}
		foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
		{
			t.gameObject.layer = vehicleLayer;
		}

		// One smooth capsule instead of the template's colliders: mesh colliders can't go on a
		// moving rigidbody, and a capsule glides over bumps instead of catching on them.
		CapsuleCollider capsule = prefab.AddComponent<CapsuleCollider>();
		capsule.direction = 2;
		capsule.radius = 0.225f * variant.Thickness;
		capsule.height = variant.Length;

		ZNetView nview = prefab.AddComponent<ZNetView>();
		nview.m_persistent = true;

		Rigidbody body = prefab.AddComponent<Rigidbody>();
		body.mass = variant.Mass;
		body.useGravity = false;
		body.freezeRotation = true;
		body.interpolation = RigidbodyInterpolation.Interpolate;
		body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		body.maxDepenetrationVelocity = 2f;

		ZSyncTransform sync = prefab.AddComponent<ZSyncTransform>();
		sync.m_syncPosition = true;
		sync.m_syncRotation = true;
		sync.m_syncBodyVelocity = true;

		Piece? templatePiece = template != null ? template.GetComponent<Piece>() : null;
		Piece piece = prefab.AddComponent<Piece>();
		piece.m_name = variant.Name;
		piece.m_description = variant.Describe();
		piece.m_icon = Icon(variant) ?? templatePiece?.m_icon;
		piece.m_category = Piece.PieceCategory.Misc;
		piece.m_craftingStation = workbench;
		if (templatePiece != null)
		{
			piece.m_placeEffect = templatePiece.m_placeEffect;
		}
		piece.m_resources = Requirements(variant, db);

		GameObject seat = new("attach");
		seat.transform.SetParent(prefab.transform, false);
		HoverLog log = prefab.AddComponent<HoverLog>();
		log.m_variant = variant.Key;
		log.m_attachPoint = seat.transform;

		return prefab;
	}

	// The vanilla pole/beam built from the same wood (the longest one), so each log looks like its
	// wood. Falls back to the variant's named hints, then to any plain wooden pole.
	private static GameObject? FindTemplate(HoverLogVariant variant, PieceTable hammer)
	{
		List<GameObject> pieces = hammer.m_pieces.Where(p => p != null && p.GetComponent<Piece>() != null).ToList();
		bool LooksLikeLog(GameObject p)
		{
			string n = p.name.ToLowerInvariant();
			return n.Contains("pole") || n.Contains("beam") || n.Contains("log");
		}
		bool MadeOf(GameObject p, string item) => p.GetComponent<Piece>().m_resources
			.Any(r => r.m_resItem != null && r.m_resItem.gameObject.name == item);

		GameObject? byWood = pieces
			.Where(p => LooksLikeLog(p) && MadeOf(p, variant.WoodItem) && HasMesh(p))
			.OrderByDescending(p => LongestAxis(Bounds(p.transform)))
			.FirstOrDefault();
		if (byWood != null)
		{
			return byWood;
		}
		foreach (string hint in variant.TemplateHints)
		{
			GameObject? named = pieces.FirstOrDefault(p => p.name == hint);
			if (named != null)
			{
				return named;
			}
		}
		return pieces.FirstOrDefault(p => p.name == "wood_pole2") ?? pieces.FirstOrDefault(p => LooksLikeLog(p) && MadeOf(p, "Wood"));
	}

	// Keeps just the meshes; drops the piece's game logic and its worn/broken alternates.
	private static void StripToVisual(GameObject visual)
	{
		WearNTear? wear = visual.GetComponent<WearNTear>();
		if (wear != null)
		{
			foreach (GameObject? alternate in new[] { wear.m_worn, wear.m_broken, wear.m_wet })
			{
				if (alternate != null && alternate != wear.m_new && alternate != visual)
				{
					Object.DestroyImmediate(alternate);
				}
			}
		}
		foreach (Transform t in visual.GetComponentsInChildren<Transform>(true).ToArray())
		{
			if (t != null && t != visual.transform && t.CompareTag("snappoint"))
			{
				Object.DestroyImmediate(t.gameObject);
			}
		}
		// Unity won't remove a component another one [RequireComponent]s, so go newest-first (the
		// dependent ones are usually added later) and repeat; ZNetView goes last.
		for (int pass = 0; pass < 4; pass++)
		{
			MonoBehaviour[] behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
			if (behaviours.Length == 0)
			{
				break;
			}
			foreach (MonoBehaviour behaviour in behaviours.Reverse())
			{
				if (behaviour is not ZNetView || behaviours.All(b => b is ZNetView))
				{
					Object.DestroyImmediate(behaviour);
				}
			}
		}
		foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true))
		{
			Object.DestroyImmediate(rb);
		}
		foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
		{
			Object.DestroyImmediate(collider);
		}
	}

	// Turns the template so its longest side runs along the log's forward axis, then scales it to
	// the variant's length and thickness and centres it on the log's origin.
	private static void LayFlat(GameObject visual, Transform root, HoverLogVariant variant)
	{
		Bounds bounds = Bounds(visual.transform, root);
		Vector3 size = bounds.size;
		if (size.y >= size.x && size.y >= size.z)
		{
			visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		}
		else if (size.x >= size.z)
		{
			visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
		}
		bounds = Bounds(visual.transform, root);
		float length = Mathf.Max(0.01f, bounds.size.z);
		float girth = Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.y));
		// A log reads as a log at about 0.45 m across; Thickness scales from there.
		float lengthScale = variant.Length / length;
		float girthScale = 0.45f * variant.Thickness / girth;
		Vector3 local = Quaternion.Inverse(visual.transform.localRotation) * new Vector3(girthScale, girthScale, lengthScale);
		visual.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)));
		bounds = Bounds(visual.transform, root);
		visual.transform.localPosition -= bounds.center;
	}

	// Squashes and stretches a model to fill a box: longest side along the log, flattest side up.
	private static void FitToBox(GameObject visual, Transform root, Vector3 size, Vector3 center)
	{
		Bounds bounds = Bounds(visual.transform, root);
		Vector3 s = bounds.size;
		Quaternion alongZ = s.y >= s.x && s.y >= s.z ? Quaternion.Euler(90f, 0f, 0f)
			: s.x >= s.z ? Quaternion.Euler(0f, 90f, 0f)
			: Quaternion.identity;
		visual.transform.localRotation = alongZ;
		bounds = Bounds(visual.transform, root);
		if (bounds.size.x < bounds.size.y)
		{
			visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f) * alongZ;
			bounds = Bounds(visual.transform, root);
		}
		Vector3 factor = new(size.x / Mathf.Max(0.01f, bounds.size.x), size.y / Mathf.Max(0.01f, bounds.size.y), size.z / Mathf.Max(0.01f, bounds.size.z));
		Vector3 local = Quaternion.Inverse(visual.transform.localRotation) * factor;
		visual.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)));
		bounds = Bounds(visual.transform, root);
		visual.transform.localPosition += center - bounds.center;
	}

	private static bool HasMesh(GameObject? go) => go != null &&
		(go.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh != null)
		 || go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => r.sharedMesh != null));

	private static Bounds Bounds(Transform t) => Bounds(t, t);

	// Mesh bounds in `space`'s local coordinates. Works on inactive objects, where Renderer.bounds doesn't.
	private static Bounds Bounds(Transform t, Transform space)
	{
		bool any = false;
		Bounds result = default;
		List<(Mesh mesh, Transform transform)> meshes = t.GetComponentsInChildren<MeshFilter>(true)
			.Where(f => f.sharedMesh != null).Select(f => (f.sharedMesh, f.transform))
			.Concat(t.GetComponentsInChildren<SkinnedMeshRenderer>(true)
				.Where(r => r.sharedMesh != null).Select(r => (r.sharedMesh, r.transform)))
			.ToList();
		foreach ((Mesh sharedMesh, Transform meshTransform) in meshes)
		{
			Matrix4x4 toSpace = space.worldToLocalMatrix * meshTransform.localToWorldMatrix;
			Bounds mesh = sharedMesh.bounds;
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				Vector3 p = toSpace.MultiplyPoint3x4(corner);
				if (!any)
				{
					result = new Bounds(p, Vector3.zero);
					any = true;
				}
				else
				{
					result.Encapsulate(p);
				}
			}
		}
		return result;
	}

	private static float LongestAxis(Bounds b) => Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));

	private static void Tint(GameObject visual, Color tint)
	{
		foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
		{
			Material[] materials = renderer.sharedMaterials;
			for (int i = 0; i < materials.Length; i++)
			{
				if (materials[i] != null && materials[i].HasProperty("_Color"))
				{
					materials[i] = new Material(materials[i]) { color = materials[i].color * tint };
				}
			}
			renderer.sharedMaterials = materials;
		}
	}

	// Parses the variant's Recipe config ("Item:Amount,Item:Amount") into build requirements.
	private static Piece.Requirement[] Requirements(HoverLogVariant variant, ObjectDB db)
	{
		List<Piece.Requirement> requirements = new();
		foreach (string part in variant.RecipeCfg.Value.Split(','))
		{
			string[] pair = part.Split(':');
			string item = pair[0].Trim();
			if (item.Length == 0)
			{
				continue;
			}
			int amount = 1;
			if (pair.Length > 1 && !int.TryParse(pair[1].Trim(), out amount))
			{
				SnakeWoodPlugin.Log.LogWarning($"{variant.Name} recipe: '{part.Trim()}' needs a whole number after the colon.");
				continue;
			}
			ItemDrop? drop = db.GetItemPrefab(item)?.GetComponent<ItemDrop>();
			if (drop == null)
			{
				SnakeWoodPlugin.Log.LogWarning($"{variant.Name} recipe: there's no item called '{item}', so it's left out.");
				continue;
			}
			if (amount > 0)
			{
				requirements.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
			}
		}
		return requirements.ToArray();
	}

	// The embedded icon, with the bark recoloured to roughly match each wood.
	private static Sprite? Icon(HoverLogVariant variant)
	{
		if (s_iconTexture == null)
		{
			using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SnakeWood.icon.png");
			if (stream == null)
			{
				return null;
			}
			byte[] bytes = new byte[stream.Length];
			stream.Read(bytes, 0, bytes.Length);
			s_iconTexture = new Texture2D(2, 2);
			s_iconTexture.LoadImage(bytes);
		}
		Texture2D texture = s_iconTexture;
		if (variant.Tint != Color.white)
		{
			texture = new Texture2D(s_iconTexture.width, s_iconTexture.height, TextureFormat.RGBA32, false);
			Color[] pixels = s_iconTexture.GetPixels();
			for (int i = 0; i < pixels.Length; i++)
			{
				Color c = pixels[i];
				// Leave the blue hover glow alone.
				if (!(c.b > c.r + 0.15f))
				{
					pixels[i] = new Color(c.r * variant.Tint.r, c.g * variant.Tint.g, c.b * variant.Tint.b, c.a);
				}
			}
			texture.SetPixels(pixels);
			texture.Apply();
		}
		return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
	}
}

[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
internal static class ObjectDBAwakePatch
{
	private static void Postfix(ObjectDB __instance) => HoverLogPrefabs.Build(__instance);
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
internal static class ZNetSceneAwakePatch
{
	private static void Postfix() => HoverLogPrefabs.RegisterWithZNetScene();
}

// Controls while on a log. Vanilla's Player.SetControls drops you off any mount on Jump or attack.
// Here Jump hops instead, Crouch is held to brake (so it mustn't toggle crouching), and with Mounted
// Combat on, attacks are kept away from that check and restored afterwards so they still happen.
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class RidingControlsPatch
{
	internal struct HeldAttacks
	{
		public bool Active, Attack, AttackHold, Secondary, SecondaryHold;
	}

	private static void Prefix(Player __instance, ref bool jump, ref bool crouch, ref bool attack, ref bool attackHold,
		ref bool secondaryAttack, ref bool secondaryAttackHold, out HeldAttacks __state)
	{
		__state = default;
		if (__instance.GetDoodadController() is not HoverLog log)
		{
			return;
		}
		if (jump)
		{
			log.QueueHop();
			jump = false;
		}
		crouch = false;
		if (SnakeWoodPlugin.MountedCombat.Value)
		{
			__state = new HeldAttacks
			{
				Active = true, Attack = attack, AttackHold = attackHold, Secondary = secondaryAttack, SecondaryHold = secondaryAttackHold,
			};
			attack = attackHold = secondaryAttack = secondaryAttackHold = false;
		}
	}

	private static void Postfix(Player __instance, HeldAttacks __state)
	{
		if (!__state.Active)
		{
			return;
		}
		__instance.m_attack = __state.Attack;
		__instance.m_attackHold = __state.AttackHold;
		__instance.m_secondaryAttack = __state.Secondary;
		__instance.m_secondaryAttackHold = __state.SecondaryHold;
	}
}

// Player.UpdateAttackBowDraw cancels any bow draw while the player IsAttached(). For a rider on a
// log with Mounted Combat, that one check is told "not attached" so bows can be drawn.
[HarmonyPatch(typeof(Player), nameof(Player.UpdateAttackBowDraw))]
internal static class BowDrawWhileRidingPatch
{
	internal static bool InBowDraw;

	private static void Prefix(Player __instance) =>
		InBowDraw = SnakeWoodPlugin.MountedCombat.Value && __instance.GetDoodadController() is HoverLog;

	private static void Finalizer() => InBowDraw = false;
}

[HarmonyPatch(typeof(Player), nameof(Player.IsAttached))]
internal static class IsAttachedDuringBowDrawPatch
{
	private static bool Prefix(ref bool __result)
	{
		if (!BowDrawWhileRidingPatch.InBowDraw)
		{
			return true;
		}
		__result = false;
		return false;
	}
}

// Projectiles fired from a log spread more: each log's flat Aim Penalty, plus more with speed.
// Attack.FireProjectileBurst reads m_projectileAccuracy(Min) as its spread in degrees.
[HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
internal static class AimWhileRidingPatch
{
	private static void Prefix(Attack __instance, out (bool applied, float accuracy, float accuracyMin) __state)
	{
		__state = default;
		if (__instance.m_character is not Player player || player.GetDoodadController() is not HoverLog log)
		{
			return;
		}
		float penalty = log.Variant.AimPenaltyCfg.Value + log.Speed * SnakeWoodPlugin.AimPenaltyPerSpeed.Value;
		__state = (true, __instance.m_projectileAccuracy, __instance.m_projectileAccuracyMin);
		__instance.m_projectileAccuracy += penalty;
		__instance.m_projectileAccuracyMin += penalty;
	}

	private static void Finalizer(Attack __instance, (bool applied, float accuracy, float accuracyMin) __state)
	{
		if (__state.applied)
		{
			__instance.m_projectileAccuracy = __state.accuracy;
			__instance.m_projectileAccuracyMin = __state.accuracyMin;
		}
	}
}
