using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnakeWood;

// Players without the mod can't create a hover log, so each log's ZDO is saved under a vanilla
// fallen-tree log's prefab instead, with the real wood type in a ZDO string. Modded games spot that
// string and build the hover log (see CreateObjectPatch); vanilla games build the tree log, which
// moves with the synced position. If the mod is ever removed, saved logs become ordinary tree logs.
internal static class HoverLogDisguise
{
	public const string VariantKey = "SnakeWood_variant";
	public static readonly int VariantHash = VariantKey.GetStableHashCode();
	// The offset (Euler angles) applied to this log's synced rotation; see HoverLog.SetDisguiseOffset.
	private static readonly int OffsetHash = "SnakeWood_offset".GetStableHashCode();
	// Logs disguised by 1.2.11-1.2.12 got a fixed 90 degree turn and no stored offset.
	private static readonly Vector3 LegacyOffset = new(90f, 0f, 0f);

	// Which vanilla tree's log each wood hides as.
	private static readonly Dictionary<string, string[]> s_hints = new()
	{
		["Wood"] = new[] { "beech", "fir" },
		["FineWood"] = new[] { "birch", "oak" },
		["RoundLog"] = new[] { "pine" },
		["ElderBark"] = new[] { "oak" },
		["YggdrasilWood"] = new[] { "ygga", "yggdrasil" },
		["Blackwood"] = new[] { "ash" },
		["Frostwood"] = new[] { "snow" },
		["Sled"] = new[] { "beech", "birch" },
		["Fish2"] = new[] { "beech" },
	};
	private static readonly Dictionary<string, (int hash, Vector3 offset)> s_disguises = new();
	// The scale each disguise is saved at so vanilla games show it at the hover log's own size
	// (zero when its prefab can't be scaled that way).
	private static readonly Dictionary<string, Vector3> s_scales = new();
	private static bool s_listed;

	// The offset already applied to a log's synced rotation (none for a log never seen by this code).
	public static Quaternion StoredOffset(ZDO zdo) =>
		zdo.GetString(VariantHash).Length == 0 ? Quaternion.identity : Quaternion.Euler(zdo.GetVec3(OffsetHash, LegacyOffset));

	// Run by the owner when a log loads: picks the disguise, and re-turns the root if the offset it
	// needs differs from the one already applied (new logs have none; older ones may have a wrong one).
	public static void Apply(HoverLog log, ZDO zdo)
	{
		(int disguise, Vector3 offset) = SnakeWoodPlugin.Disguise.Value ? DisguiseFor(log.m_variant) : (0, Vector3.zero);
		if (disguise == 0)
		{
			offset = Vector3.zero;
		}
		Quaternion stored = StoredOffset(zdo);
		Quaternion wanted = Quaternion.Euler(offset);
		if (Quaternion.Angle(stored, wanted) > 0.5f)
		{
			log.transform.rotation = log.transform.rotation * Quaternion.Inverse(stored) * wanted;
		}
		zdo.Set(VariantKey, log.m_variant);
		zdo.Set(OffsetHash, offset);
		// Vanilla half logs take their scale from the ZDO (a felled tree passes its own size on to
		// its halves), so the disguise is shrunk or stretched to the hover log instead.
		zdo.Set(ZDOVars.s_scaleHash, disguise != 0 && s_scales.TryGetValue(log.m_variant, out Vector3 scale) ? scale : Vector3.zero);
		GameObject? own = HoverLogPrefabs.PrefabFor(log.m_variant);
		zdo.SetPrefab(disguise != 0 ? disguise : own != null ? own.name.GetStableHashCode() : zdo.GetPrefab());
		log.SetDisguiseOffset(wanted);
		if (disguise != 0)
		{
			// Keeps vanilla players from chopping a parked log to bits if they end up owning it.
			zdo.Set(ZDOVars.s_health, 1e9f);
		}
	}

	private static readonly Dictionary<int, (float length, float girth)> s_sizes = new();

	private static float LongSide(Vector3 size) => Mathf.Max(size.x, Mathf.Max(size.y, size.z));

	// Length and thickness of a vanilla log prefab (by hash), or null for anything else, including
	// the hover log's own prefab. The real model is scaled to about this so both sides match.
	public static (float length, float girth)? SizeOf(int prefabHash)
	{
		if (s_sizes.TryGetValue(prefabHash, out (float, float) known))
		{
			return known;
		}
		GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
		if (prefab == null || prefab.GetComponent<TreeLog>() == null || SyncsScale(prefab))
		{
			return null;
		}
		Vector3 size = HoverLogPrefabs.Bounds(prefab.transform).size;
		float length = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
		float girth = size.x + size.y + size.z - length - Mathf.Min(size.x, Mathf.Min(size.y, size.z));
		s_sizes[prefabHash] = (length, girth);
		return (length, girth);
	}

	// A fish log can show as its own vanilla fish (the user accepted the risk: a player without the
	// mod who ends up owning it can pick it up, which deletes the ride). Turned the way LayFlat turns
	// the modded model, so both face the same way. Vanilla can't be told to scale it, so it shows at
	// the fish item's own size.
	private static (int hash, Vector3 offset)? FishDisguise(string variant)
	{
		if (!SnakeWoodPlugin.FishDisguise.Value || ZNetScene.instance == null
			|| HoverLogVariant.Get(variant) is not HoverLogVariant v || !v.IsFish)
		{
			return null;
		}
		GameObject? fish = ZNetScene.instance.GetPrefab(v.WoodItem);
		if (fish == null || fish.GetComponent<ZSyncTransform>() == null)
		{
			SnakeWoodPlugin.Log.LogWarning($"{variant}: the vanilla {v.WoodItem} can't follow a moving log, so it shows as a tree log to players without the mod.");
			return null;
		}
		Vector3 size = HoverLogPrefabs.Bounds(fish.transform).size;
		Vector3 offset = size.y >= size.x && size.y >= size.z ? new Vector3(90f, 0f, 0f)
			: size.x >= size.z ? new Vector3(0f, 90f, 0f)
			: Vector3.zero;
		SnakeWoodPlugin.Log.LogInfo($"{variant} logs show as the vanilla {fish.name} to players without the mod (size {size}, turn {offset}).");
		return (fish.name.GetStableHashCode(), offset);
	}

	private static bool SyncsScale(GameObject prefab) => prefab.GetComponent<ZNetView>() is ZNetView view && view.m_syncInitialScale;

	// The vanilla log to disguise as, and the turn that lays its longest side along +Z (forward).
	private static (int hash, Vector3 offset) DisguiseFor(string variant)
	{
		if (s_disguises.TryGetValue(variant, out (int, Vector3) cached))
		{
			return cached;
		}
		int hash = 0;
		Vector3 offset = Vector3.zero;
		if (FishDisguise(variant) is (int, Vector3) fish)
		{
			s_disguises[variant] = fish;
			return fish;
		}
		if (ZNetScene.instance != null)
		{
			string[] hints = s_hints.TryGetValue(variant, out string[] h) ? h : variant.StartsWith("Fish") ? s_hints["Fish2"] : new string[0];
			// Half logs (what a felled trunk splits into) are the right sort of size; whole trunks are
			// the entire tree. The real hover log is then sized to match (SizeOf).
			var logs = ZNetScene.instance.m_prefabs
				.Where(p => p != null && p.GetComponent<TreeLog>() != null && p.GetComponent<ZSyncTransform>() != null
					&& !p.name.ToLowerInvariant().Contains("old") && !p.name.ToLowerInvariant().Contains("troll"))
				.Select(p =>
				{
					Vector3 size = HoverLogPrefabs.Bounds(p.transform).size;
					string lower = p.name.ToLowerInvariant();
					return (prefab: p, size, half: lower.Contains("half"), own: hints.Any(lower.Contains), scales: SyncsScale(p));
				})
				// Its own tree's half log: at any size if vanilla can be told to resize it, otherwise
				// only if it's no longer than 5 m (the hover log would grow to match). Else the
				// smallest half log.
				.OrderBy(c => c.half ? 0 : 1)
				.ThenBy(c => c.own && (c.scales || LongSide(c.size) <= 5f) ? 0 : 1)
				.ThenBy(c => c.size.x * c.size.y * c.size.z)
				.ToList();
			if (!s_listed)
			{
				s_listed = true;
				SnakeWoodPlugin.Log.LogInfo("Vanilla tree logs available as disguises: " +
					string.Join(", ", logs.Select(c => $"{c.prefab.name} {c.size}{(c.scales ? " scalable" : "")}")));
			}
			GameObject? pick = logs.Count > 0 ? logs[0].prefab : null;
			if (pick != null)
			{
				hash = pick.name.GetStableHashCode();
				Vector3 size = HoverLogPrefabs.Bounds(pick.transform).size;
				string axis;
				if (size.y >= size.x && size.y >= size.z)
				{
					offset = new Vector3(90f, 0f, 0f); // Euler(90,0,0) turns +Y onto +Z
					axis = "Y";
				}
				else if (size.x >= size.z)
				{
					offset = new Vector3(0f, -90f, 0f); // Euler(0,-90,0) turns +X onto +Z
					axis = "X";
				}
				else
				{
					axis = "Z";
				}
				Vector3 scale = Vector3.zero;
				if (SyncsScale(pick) && HoverLogVariant.Get(variant) is HoverLogVariant v)
				{
					// Long side to the hover log's length, the other two to its thickness (keeping
					// the log's own cross-section).
					Vector3 longAxis = axis == "Y" ? Vector3.up : axis == "X" ? Vector3.right : Vector3.forward;
					Vector3 cross = Vector3.Scale(size, Vector3.one - longAxis);
					float girth = Mathf.Max(cross.x, Mathf.Max(cross.y, cross.z));
					float along = v.Length / Mathf.Max(0.01f, LongSide(size));
					float across = 0.45f * v.Thickness / Mathf.Max(0.01f, girth);
					scale = Vector3.Scale(pick.transform.localScale, longAxis * along + (Vector3.one - longAxis) * across);
				}
				s_scales[variant] = scale;
				SnakeWoodPlugin.Log.LogInfo($"{variant} logs show as {pick.name} to players without the mod (long side {axis}, size {size}" +
					(scale != Vector3.zero ? $", scaled {scale} to match)." : ", not scalable: the hover log grows to match)."));
			}
			else
			{
				SnakeWoodPlugin.Log.LogWarning("No vanilla tree log found to disguise hover logs as; players without the mod won't see them.");
			}
		}
		s_disguises[variant] = (hash, offset);
		return (hash, offset);
	}
}

// Builds the hover log instead of the vanilla prefab for any ZDO carrying a hover log's wood type.
// Mirrors ZNetScene.CreateObject, just with our prefab.
[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObject))]
internal static class CreateObjectPatch
{
	private static bool Prefix(ZDO zdo, ref GameObject __result)
	{
		string variant = zdo.GetString(HoverLogDisguise.VariantHash);
		GameObject? prefab = variant.Length > 0 ? HoverLogPrefabs.PrefabFor(variant) : null;
		if (prefab == null)
		{
			return true;
		}
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		__result = Object.Instantiate(prefab, zdo.GetPosition(), zdo.GetRotation());
		ZNetView.m_initZDO = null;
		ZNetView.m_useInitZDO = false;
		return false;
	}
}
