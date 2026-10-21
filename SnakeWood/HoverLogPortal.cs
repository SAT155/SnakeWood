using HarmonyLib;
using UnityEngine;

namespace SnakeWood;

// Riding through a portal. Vanilla pins an attached rider to their seat every physics step, which
// fights the teleport (it never finishes: a black screen). So for the trip the seat stops pinning
// the rider and the log stops flying; once the rider has arrived, the log is brought in under them
// and they're seated again. If the log was unloaded on the way, its saved copy is moved to the
// rider, and they're seated as soon as it loads there.
internal static class MountedPortal
{
	private static bool s_active;
	private static HoverLog? s_log;
	private static ZDOID s_logId = ZDOID.None;
	private static bool s_moved;
	private static float s_arrivedAt;

	public static bool Active => s_active;

	public static void Begin(HoverLog log)
	{
		s_active = true;
		s_log = log;
		s_logId = log.Id;
		s_moved = false;
		s_arrivedAt = 0f;
	}

	// The log being carried through a portal right now (it mustn't drop its rider when unloaded).
	public static bool Holds(HoverLog log) => s_active && ReferenceEquals(log, s_log);

	public static void Update(Player player)
	{
		if (!s_active || player.IsTeleporting())
		{
			return;
		}
		if (s_arrivedAt <= 0f)
		{
			s_arrivedAt = Time.time;
		}
		if (s_log != null)
		{
			// Still loaded (a short hop): just bring it over.
			Finish(s_log, player);
			return;
		}
		if (!s_moved)
		{
			s_moved = true;
			ZDO? zdo = ZDOMan.instance.GetZDO(s_logId);
			if (zdo == null)
			{
				GiveUp(player);
				return;
			}
			zdo.SetOwner(ZDOMan.GetSessionID());
			zdo.SetPosition(player.transform.position);
			if (ZNetScene.instance.FindInstance(zdo) is ZNetView view && view && view.GetComponent<HoverLog>() is HoverLog loaded)
			{
				Finish(loaded, player);
				return;
			}
		}
		if (Time.time - s_arrivedAt > 10f)
		{
			GiveUp(player);
		}
	}

	// A log just loaded: if it's the one we're waiting for, seat the rider on it.
	public static void OnLogLoaded(HoverLog log, Player player)
	{
		if (s_active && s_moved && !player.IsTeleporting() && log.Id == s_logId)
		{
			Finish(log, player);
		}
	}

	private static void Finish(HoverLog log, Player player)
	{
		s_active = false;
		s_log = null;
		log.ArriveUnder(player);
	}

	private static void GiveUp(Player player)
	{
		s_active = false;
		s_log = null;
		player.m_doodadController = null;
		player.AttachStop();
		SnakeWoodPlugin.Log.LogWarning("The hover log didn't make it through the portal; it's where you left it.");
	}
}

[HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
internal static class RideThroughPortalPatch
{
	private static void Postfix(Player __instance, bool __result)
	{
		if (__result && __instance == Player.m_localPlayer && __instance.GetDoodadController() is HoverLog log)
		{
			MountedPortal.Begin(log);
		}
	}
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
internal static class ArriveOnLogPatch
{
	private static void Postfix(Player __instance)
	{
		if (__instance == Player.m_localPlayer)
		{
			MountedPortal.Update(__instance);
		}
	}
}

// While the rider is in transit the seat doesn't pin them (that's what stalled teleports).
[HarmonyPatch(typeof(Player), nameof(Player.UpdateAttach))]
internal static class FreeRiderInPortalPatch
{
	private static bool Prefix(Player __instance) => !(MountedPortal.Active && __instance == Player.m_localPlayer);
}

// Vanilla stops looking for things to use while you control a doodad. On a log, doors (and the
// log itself, to get off) can still be used.
[HarmonyPatch(typeof(Player), nameof(Player.UpdateHover))]
internal static class DoorsWhileRidingPatch
{
	private static void Postfix(Player __instance)
	{
		if (__instance != Player.m_localPlayer || __instance.GetDoodadController() is not HoverLog)
		{
			return;
		}
		__instance.FindHoverObject(out GameObject hover, out Character _);
		if (hover != null && (hover.GetComponentInParent<Door>() != null || hover.GetComponentInParent<HoverLog>() != null
			|| hover.GetComponentInParent<Container>() != null))
		{
			__instance.m_hovering = hover;
		}
	}
}
