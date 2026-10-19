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
		["ElderBark"] = new[] { "swamp" },
		["YggdrasilWood"] = new[] { "ygga", "yggdrasil" },
		["Blackwood"] = new[] { "ash" },
		["Fish2"] = new[] { "beech" },
	};
	private static readonly Dictionary<string, (int hash, Vector3 offset)> s_disguises = new();
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
		log.SetDisguiseOffset(wanted);
		GameObject? own = HoverLogPrefabs.PrefabFor(log.m_variant);
		zdo.SetPrefab(disguise != 0 ? disguise : own != null ? own.name.GetStableHashCode() : zdo.GetPrefab());
		if (disguise != 0)
		{
			// Keeps vanilla players from chopping a parked log to bits if they end up owning it.
			zdo.Set(ZDOVars.s_health, 1e9f);
		}
	}

	// The vanilla log to disguise as, and the turn that lays its longest side along +Z (forward).
	private static (int hash, Vector3 offset) DisguiseFor(string variant)
	{
		if (s_disguises.TryGetValue(variant, out (int, Vector3) cached))
		{
			return cached;
		}
		int hash = 0;
		Vector3 offset = Vector3.zero;
		if (ZNetScene.instance != null)
		{
			HoverLogVariant? v = HoverLogVariant.Get(variant);
			float wantLength = v?.Length ?? 2.6f;
			float wantGirth = 0.45f * (v?.Thickness ?? 1f);
			string[] hints = s_hints.TryGetValue(variant, out string[] h) ? h : new string[0];
			// Vanilla tree logs can't be scaled for players without the mod, so pick the one whose size
			// is closest to this log's; its own tree's log wins only when it's nearly as close.
			var logs = ZNetScene.instance.m_prefabs
				.Where(p => p != null && p.GetComponent<TreeLog>() != null && p.GetComponent<ZSyncTransform>() != null)
				.Select(p =>
				{
					Vector3 size = HoverLogPrefabs.Bounds(p.transform).size;
					float length = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
					float girth = size.x + size.y + size.z - length - Mathf.Min(size.x, Mathf.Min(size.y, size.z));
					float misfit = Mathf.Abs(Mathf.Log(length / wantLength)) + Mathf.Abs(Mathf.Log(Mathf.Max(0.05f, girth) / wantGirth));
					bool ownTree = hints.Any(hint => p.name.ToLowerInvariant().Contains(hint));
					return (prefab: p, size, misfit: misfit * (ownTree ? 0.8f : 1f));
				})
				.OrderBy(c => c.misfit)
				.ToList();
			if (!s_listed)
			{
				s_listed = true;
				SnakeWoodPlugin.Log.LogInfo("Vanilla tree logs available as disguises: " +
					string.Join(", ", logs.Select(c => $"{c.prefab.name} {c.size}")));
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
				SnakeWoodPlugin.Log.LogInfo($"{variant} logs show as {pick.name} to players without the mod (long side {axis}, size {size}).");
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
