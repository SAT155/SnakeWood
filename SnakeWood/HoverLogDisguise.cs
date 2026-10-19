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
	// Vanilla tree logs are modelled standing up (they spawn upright and topple), so the synced root
	// is turned by this to lay one along the log's heading. HoverLog's frame undoes it for the model.
	public static readonly Quaternion Offset = Quaternion.Euler(90f, 0f, 0f);

	public const string VariantKey = "SnakeWood_variant";
	public static readonly int VariantHash = VariantKey.GetStableHashCode();

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
	private static readonly Dictionary<string, int> s_disguises = new();

	// Run by the owner when a log loads.
	public static void Apply(HoverLog log, ZDO zdo)
	{
		if (zdo.GetString(VariantHash).Length == 0)
		{
			// Newly built (or saved before the disguise existed): the root still holds the log's own
			// heading, so add the offset now.
			log.transform.rotation *= Offset;
			zdo.Set(VariantKey, log.m_variant);
		}
		if (!SnakeWoodPlugin.Disguise.Value)
		{
			return;
		}
		int disguise = DisguiseFor(log.m_variant);
		if (disguise == 0)
		{
			return;
		}
		zdo.SetPrefab(disguise);
		// Keeps vanilla players from chopping a parked log to bits if they end up owning it.
		zdo.Set(ZDOVars.s_health, 1e9f);
	}

	private static int DisguiseFor(string variant)
	{
		if (s_disguises.TryGetValue(variant, out int cached))
		{
			return cached;
		}
		int hash = 0;
		if (ZNetScene.instance != null)
		{
			List<GameObject> logs = ZNetScene.instance.m_prefabs
				.Where(p => p != null && p.GetComponent<TreeLog>() != null && p.GetComponent<ZSyncTransform>() != null)
				.OrderBy(p => p.name.ToLowerInvariant().Contains("half") ? 1 : 0)
				.ToList();
			string[] hints = s_hints.TryGetValue(variant, out string[] h) ? h : new string[0];
			GameObject? pick = logs.FirstOrDefault(p => hints.Any(hint => p.name.ToLowerInvariant().Contains(hint)))
				?? logs.FirstOrDefault();
			if (pick != null)
			{
				hash = pick.name.GetStableHashCode();
				SnakeWoodPlugin.Log.LogInfo($"{variant} logs show as {pick.name} to players without the mod.");
			}
			else
			{
				SnakeWoodPlugin.Log.LogWarning("No vanilla tree log found to disguise hover logs as; players without the mod won't see them.");
			}
		}
		s_disguises[variant] = hash;
		return hash;
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
