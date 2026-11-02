# Mist and demisters

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-10-24 by the ValheimMapMarker
North Wisp session. All *(verified)* by decompile unless noted.

## How mist is drawn

- `ParticleMist` (one instance, `ParticleMist.instance`) is a **local** particle system around the
  local player. Every 0.1 s it emits mist particles near the player and around "misters"
  (`Mister`, the mist volumes), and skips any point inside a demister. So mist is per client:
  clearing it is something each client does for itself.
- It only works inside `m_maxDistance` (50) of the player for demister edges, and
  `m_distantMaxRange` (100) for distant mister volumes.
- `ParticleMist.IsInMist(p)` and `IsMistBlocked(p0, p1)` (line of sight through mist, used by
  AI) also check demisters.

## Demister

- `Demister` (MonoBehaviour) keeps a static list of every **enabled** one
  (`Demister.GetDemisters()`, added in `OnEnable`, removed in `OnDisable`).
- Its clearing radius is `m_forceField.endRange`, a `ParticleSystemForceField` fetched with
  `GetComponent` in `Awake` (same GameObject). The force field also pushes existing particles out.
- `ParticleMist` reads `m_forceField.endRange` on **every** demister without a null check, so a
  `Demister` whose force field is missing breaks mist for that client. Destroy such a copy at once.
- `m_disableForcefieldDelay > 0` turns the field off after that many seconds.
- `GetMovedDistance()` makes a moving demister re-emit mist behind it (so a moving clear spot
  closes again behind itself).
- Changing `endRange` at runtime changes the clearing radius. Needs a reference to
  `UnityEngine.ParticleSystemModule` to compile.

## The Wisplight

- Item prefab `Demister` (utility item). Its equip status effect is `SE_Demister`.
- `SE_Demister.UpdateStatusEffect` spawns `m_ballPrefab` (`demister_ball`) with a plain
  `Object.Instantiate`, which makes it a **network object** (it has a `ZNetView`), then moves it
  every frame toward an offset above the player with noise. Removal: `ClaimOwnership()` +
  `ZNetView.Destroy()`.
- `demister_ball` has a `Demister`, so it clears mist for every client that has it loaded.
  Others see it move through its `ZSyncTransform`.
- The item prefab has no `attach` child (it's not a hand item).

## Recipes for mods

- **Wisp everyone sees and that clears mist for everyone**: `Instantiate(demister_ball)` and move
  it yourself (ValheimMapMarker's North Wisp).
- **Invisible, local mist clearer** (ValheimMapMarker 1.9.3): a new inactive GameObject, add a
  `ParticleSystemForceField` and copy `demister_ball`'s field settings onto it, then add a
  `Demister`, copy its public fields from the prefab's and set `m_forceField`, then activate.
  Set `endRange` for the size. (Stripping a `demister_ball` copy with `DestroyImmediate` worked
  but broke when done inside an animation event; see `network-objects.md`.)

## Valheim Plus

The user's server runs Valheim Plus. Its `Demister_OnEnable_Patch` postfix (read from
Grantapher/ValheimPlus, `ValheimPlus/GameClasses/Demister.cs`, 2026-10-27) checks
`transform.root.name` against `demister_ball` / `piece_groundtorch_mist` and then
`transform.parent.name` against `Mistwalker` **without a null check**, so any `Demister` on a
root object throws a `NullReferenceException` on enable (seen in-game with the North Wisp's mist
clearers). Put your own demisters under a parent object. A `demister_ball` copy gets V+'s
Wisplight range applied when its range setting is on.

