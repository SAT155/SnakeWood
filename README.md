# SnakeWood

A BepInEx mod for Valheim: build a log with the hammer and ride it like a hover bike. There's
one log per wood type, each with its own handling, and the heavy ones ram. The player-facing
details, controls and config are in
[`thunderstore/SnakeWood/README.md`](thunderstore/SnakeWood/README.md).

## How it works

- **Prefabs (`HoverLogPrefabs.cs`).** On `ObjectDB.Awake`, one prefab is built per wood type
  under an inactive holder, so nothing on it wakes up. Its look is copied from the hammer's
  longest pole or beam made from that wood (falling back to named pieces, then `wood_pole2`),
  stripped of game logic and worn/broken variants, turned to lie along +Z and resized. It gets
  a capsule collider, a `ZNetView`, a `Rigidbody`, a `ZSyncTransform`, a `Piece` with the recipe
  and a `HoverLog`. Prefabs go into the hammer's piece table and into `ZNetScene`, whichever
  wakes second.
- **Riding (`HoverLog.cs`).** This is the Lox saddle's (`Sadle`) handshake. Pressing Use sends
  `RequestControl` to the log's ZDO owner. If nobody valid is riding, the owner records the
  rider and hands them ZDO ownership. The rider then attaches with `AttachStart` and takes
  control through `IDoodadController`, so vanilla feeds it move, look, run and block input.
- **Physics.** Only the ZDO owner, which is the rider while someone rides, runs the
  simulation. Everyone else gets the synced transform and velocity. Height is a damped spring
  toward ground (raycast against terrain and pieces), or sea level when that's higher. Well
  above that height it falls under scaled gravity instead. Heading and lean go through
  `MoveRotation` on a rotation-frozen body.
- **Jump hops (`JumpHopsPatch`).** Vanilla's `Player.SetControls` treats Jump as "get off" for
  any doodad. A prefix turns it into a hop while the doodad is a hover log.
- **Balance.** The rider's game reads their inventory weight (`Inventory.GetTotalWeight`)
  twice a second and scales top speed, hover/hop height and handling by it, probes the terrain
  ahead for slopes steeper than the log's climb angle, and calls `Character.AddNoise` each physics
  step. Fuel and wear live in the log's ZDO (`SnakeWood_fuel`, `SnakeWood_wear`); changes go
  through RPCs to the owner.
- **Ramming.** `OnCollisionEnter` on the rider's game sends a `HitData` (blunt, plus
  chop/pickaxe/fire/poison per wood) to the `IDestructible` it hit, scaled by impact speed,
  with a short cooldown per target.

## Building

Same setup as SnakeSmarterHoe and SnakePolearms:

- **CI** (`.github/workflows/build.yml`) builds on every push. It downloads the free Valheim
  dedicated server through SteamCMD for the game assemblies, plus BepInExPack. It then builds
  `SnakeWood.dll` and packages `SnakeWood-<version>.zip` for r2modman's "Import local mod".
  Pushes to the default branch publish a GitHub release automatically. The version is the
  manifest's MAJOR.MINOR plus the run number.
- **Locally:** `dotnet build SnakeWood/SnakeWood.csproj -c Release` with Valheim and BepInEx
  installed. Steam installs are auto-detected. You can also pass `-p:GamePath=...`, or
  `-p:ManagedDir=... -p:BepInExDir=...`.

The **Decompile** workflow (manual) prints decompiled Valheim types to the job log. The
`grep` and `after` inputs let you print just one method. Use it to check the APIs this mod
relies on (`Player`, `Sadle`, `IDoodadController`, `ZSyncTransform`) after a game update.

`tools/make_icon.py` redraws the icon (`SnakeWood/icon.png`, also the build-menu icon, and
`thunderstore/SnakeWood/icon.png`).

Notes for future Claude Code sessions (environment, CI, game APIs, the SnakeSquad repos): [`docs/VALHEIM_MODDING_NOTES_v1.2.md`](docs/VALHEIM_MODDING_NOTES_v1.2.md) (the version is in the filename; the newest version is the current one).
