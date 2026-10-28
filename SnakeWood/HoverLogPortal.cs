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

// Vanilla stops looking for things to use while you control a doodad. On a log, doors, portals,
// signs, chests (and other logs) can still be used. The search looks past the log you ride (it
// would block the view down and ahead) and reaches further than on foot, since a rider sits higher
// and further back than someone walking up to a door.
[HarmonyPatch(typeof(Player), nameof(Player.UpdateHover))]
internal static class DoorsWhileRidingPatch
{
	private static void Postfix(Player __instance)
	{
		if (__instance != Player.m_localPlayer || __instance.GetDoodadController() is not HoverLog log)
		{
			return;
		}
		GameObject hover;
		HoverLog? own = HoverLog.BeginRiderPassThrough();
		float reach = __instance.m_maxInteractDistance;
		__instance.m_maxInteractDistance = reach + SnakeWoodPlugin.RidingReach.Value;
		try
		{
			__instance.FindHoverObject(out hover, out Character _);
		}
		finally
		{
			__instance.m_maxInteractDistance = reach;
			own?.EndRiderPassThrough();
		}
		if (hover != null && (hover.GetComponentInParent<Door>() != null || hover.GetComponentInParent<HoverLog>() != null
			|| hover.GetComponentInParent<Container>() != null || hover.GetComponentInParent<TeleportWorld>() != null
			|| hover.GetComponentInParent<Sign>() != null))
		{
			__instance.m_hovering = hover;
		}
		PortalByLog.Check(__instance, log);
	}
}

// A portal only takes you when your own body walks into its trigger. Riding, the log's nose gets
// there first, and against a wall your body never does. So the log's whole length counts: as it
// enters a portal's trigger, the rider goes through (MountedPortal brings the log along).
internal static class PortalByLog
{
	private static readonly Collider[] s_hits = new Collider[32];
	private static TeleportWorld? s_inside;

	public static void Check(Player player, HoverLog log)
	{
		if (MountedPortal.Active || player.IsTeleporting() || log.Frame.GetComponent<CapsuleCollider>() is not CapsuleCollider capsule)
		{
			s_inside = null;
			return;
		}
		Transform t = capsule.transform;
		Vector3 center = t.TransformPoint(capsule.center);
		Vector3 half = t.TransformVector(Vector3.forward * Mathf.Max(0f, capsule.height / 2f - capsule.radius));
		float radius = capsule.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.y) + 0.2f;
		int count = Physics.OverlapCapsuleNonAlloc(center - half, center + half, radius, s_hits, ~0, QueryTriggerInteraction.Collide);
		TeleportWorld? portal = null;
		for (int i = 0; i < count && portal == null; i++)
		{
			if (s_hits[i].isTrigger && s_hits[i].GetComponentInParent<TeleportWorldTrigger>() is TeleportWorldTrigger trigger)
			{
				portal = trigger.m_teleportWorld;
			}
		}
		// Only on the way in, so standing in a portal that refuses you (ore, no link) doesn't repeat its message.
		if (portal != null && portal != s_inside)
		{
			ZLog.Log("Teleportation TRIGGER (hover log)");
			portal.Teleport(player);
		}
		s_inside = portal;
	}
}

// Some container mods (AzuCraftyBoxes) assume any container with m_rootObjectOverride belongs to a
// cart and call Vagon.InUse() on GetComponent<Vagon>() without a null check. A sled's chest uses an
// override but has no Vagon, so they'd throw every frame. A null "cart" simply isn't in use.
[HarmonyPatch(typeof(Vagon), nameof(Vagon.InUse))]
internal static class NullCartNotInUsePatch
{
	private static System.Exception? Finalizer(System.Exception? __exception, Vagon? __instance, ref bool __result)
	{
		if (__exception is System.NullReferenceException && __instance == null)
		{
			__result = false;
			return null;
		}
		return __exception;
	}
}
