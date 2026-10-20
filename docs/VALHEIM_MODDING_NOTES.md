# Valheim modding notes for Claude Code (SnakeSquad)

What one Claude Code session learned while building **SnakeWood** (hover logs) from scratch,
plus what it picked up from the other SnakeSquad repos. Written for the next Claude instance that
works on a Valheim mod here. Treat it as field notes, not gospel: anything marked *(verified)* was
checked against the decompiled game or confirmed in-game; the rest is best understanding.

Game version at time of writing: Valheim with **Deep North** released (Oct 2026), Unity 6
(`Rigidbody.linearVelocity`, not `velocity`), BepInEx 5.4.2350.

---

## 1. The SnakeSquad ecosystem

All repos are private, owner `SAT155`. Play happens on a small private **vanilla-ish dedicated
server** with a handful of friends; some players run mods, some don't.

| Repo | What it is | Notes |
| --- | --- | --- |
| **SnakeWood** | Original mod: ride logs (and fish) like hover bikes. ~2.5k lines. | Client+server mod, vanilla disguise for unmodded players. This doc lives here. |
| **SnakeSmarterHoe** (`snakesmarterhoe`) | Private fork of Jubaroo's SmarterHoe 1.2.2, decompiled from the released DLL. **Do not publish/redistribute.** | Client-only. Fixes terrain-edit multiplayer sync (claims `TerrainComp` ZDO ownership before editing), long-ramp zone gaps, ±8 m limit warnings, blueprints. Fork changes marked `// Fork` or in `TerrainSync.cs`. Has ServerSync folder, icon tool. |
| **SnakePolearms** (`snakepolearms`) | Original mod: atgeir primary attacks pierce through enemies (falloff or split damage), optional pass-through friendlies. ~320 lines. | Good small example of a **transpiler** (`Attack.DoMeleeAttack`, swaps one `m_hitThroughWalls` read), prefix/finalizer state save-restore, `Attack.ModifyDamage` postfix. Origin of the "Reload Config From Disk" button. |
| **SnakeSailing** | Maintained forks of blaxxun-boop's **Sailing**, **Ranching**, **Mining** skill mods, updated for Valheim 1.0+/Ashlands. | Matrix CI builds all three. Uses **ServerSync** (config locked to server admin) and **SkillManager**, merged with **ILRepack** so each mod is one DLL. Removed upstream's hard block on ValheimPlus (now a warning). |
| **SnakeDiscoBot**, **SnakeSquad.net** | Not Valheim mods (Discord bot, website). | Not covered here. |

Common conventions across the Valheim repos:

- Plugin GUID style `com.snakesquad.<name>` (SnakeWood) — check each repo.
- `thunderstore/<Package>/{manifest.json,README.md,icon.png}` + `thunderstore/package.py`
  produce a Thunderstore-format zip that installs via r2modman "Import local mod".
- **Every push to the default branch auto-releases** a GitHub release `v{MAJOR.MINOR}.{run_number}`
  (MAJOR.MINOR from `manifest.json`). Bump the manifest (and `PluginVersion` const) for feature
  releases; patch numbers come for free.
- Player-facing docs: `thunderstore/<Package>/README.md`. Developer docs: root `README.md`.
- Config entries carry long, plain-English descriptions; sections numbered (`"1 - Riding"`,
  `"3 - Balance"`, `"9 - Debug"`) so Configuration Manager orders them.
- Writing style the user likes: short, direct, explain *why*. They test in-game and send
  `LogOutput.log` excerpts and screenshots; take those as the primary truth.

---

## 2. Environment: Claude Code cloud sessions

**What the sandbox has / lacks**

- No `dotnet`, no Valheim install, no Steam. **You cannot compile locally.** All builds happen in
  GitHub Actions. Write carefully, push, read the CI log, fix. (Each round-trip is ~3–6 min.)
- No `gh` CLI; use the GitHub MCP tools (`mcp__github__*`, load via ToolSearch):
  `actions_run_trigger` (workflow_dispatch), `actions_list` (runs/jobs), `get_job_logs`
  (`return_content: true`, `tail_lines`), `get_latest_release`, etc. Unauthenticated
  `curl https://api.github.com/repos/SAT155/<repo>/actions/runs...` also works for polling
  (private repos answered fine through the proxy in this session).
- **Artifact downloads are blocked** (the blob host is not allowed by the proxy). Anything you
  need from CI must be **printed to the job log**. Big logs come back saved to a file; slice them
  with python.
- `sleep` in Bash is blocked; wait with `until <check>; do sleep 10; done` loops (foreground with
  a timeout, or `run_in_background`).
- Repo scope is per session; `list_repos` / `add_repo` to reach sibling repos. Clones of other
  SnakeSquad repos may already be in `/home/user/`.

**Branching reality for SnakeWood**: the designated branch `claude/exciting-mendel-fudt8s` is the
repo's only (therefore default) branch, so no PR can be opened and every push releases. Validate
before pushing; a broken push = a failed release run (no bad release is published, but it costs a
cycle).

**Polling recipe** (find the Build run for a commit and wait):

```bash
sha=$(git rev-parse --short=7 HEAD)
for i in $(seq 1 60); do
  r=$(curl -s "https://api.github.com/repos/SAT155/SnakeWood/actions/runs?branch=<branch>&per_page=4" \
      | python3 -c "import json,sys;d=json.load(sys.stdin);[print(x['id'],x['name'],x['status'],x['conclusion']) for x in d['workflow_runs'] if x['head_sha'].startswith('$sha') and x['name']=='Build']")
  echo "$r" | grep -q completed && { echo "$r"; break; }; sleep 20
done
```

On failure: `get_job_logs` with `run_id` + `failed_only: true`; compiler errors are also echoed
as `::error::` lines by the build step.

---

## 3. CI: building without the game

`.github/workflows/build.yml` (same shape in SnakeWood, SnakeSmarterHoe, SnakePolearms;
SnakeSailing uses a matrix):

1. `actions/setup-dotnet@v4` with `8.0.x`.
2. Install SteamCMD (`lib32gcc-s1`, tarball from steamcdn), then
   `steamcmd +force_install_dir $RUNNER_TEMP/valheim +login anonymous +app_update 896660 validate +quit`.
   **App 896660 = Valheim Dedicated Server**, free and anonymous, and it ships the same managed
   assemblies (`valheim_server_Data/Managed/assembly_valheim.dll`, `assembly_utils`,
   `assembly_guiutils`, `UnityEngine*.dll`) a client mod compiles against.
   - Wrap in `timeout 300` and retry 3× — SteamCMD sometimes hangs. First attempt often prints
     `ERROR! Failed to install app '896660' (Missing configuration)`; the retry succeeds.
   - Cache `$RUNNER_TEMP/valheim` with `actions/cache` (save immediately after download so a
     failing compile still warms it).
3. Download BepInExPack from Thunderstore (`BEPINEX_VERSION` env, e.g. `5.4.2350`).
4. `dotnet build <proj> -c Release -p:ManagedDir=... -p:BepInExDir=...`.
5. Package with `thunderstore/package.py`, upload artifacts, and on default-branch pushes
   `gh release create v<version> <dll> <zip>` (skipped when the mod didn't change).

**csproj essentials** (netstandard2.1, works for SmarterHoe and SnakeWood):

- `<TargetFramework>netstandard2.1</TargetFramework>`, `<LangVersion>latest</LangVersion>`,
  `<Nullable>enable</Nullable>`.
- Local dev auto-detects the Steam install (registry on Windows, `~/.steam/...` on Linux);
  CI passes `ManagedDir`/`BepInExDir` explicitly.
- `BepInEx.AssemblyPublicizer.MSBuild` 0.4.2 and `<Publicize>true</Publicize>` on
  `assembly_valheim`, `assembly_utils`, `assembly_guiutils` → **every private game member is
  directly accessible** (`player.m_doodadController`, `ZSyncTransform.m_isKinematicBody`, private
  methods like `Player.FindHoverObject`). Harmony `nameof(Player.UpdateAttach)` works on private
  methods too.
- Reference all `UnityEngine*.dll` from Managed (glob), `Private=false`.
- Embedded resources for icons: `<EmbeddedResource Include="icon.png" LogicalName="SnakeWood.icon.png"/>`.

### The Decompile workflow (your eyes into the game)

`.github/workflows/decompile.yml` (workflow_dispatch). Inputs:

- `types`: space-separated type names → `ilspycmd -t <Type> assembly_valheim.dll` (ilspycmd
  8.2.0.7535, needs `DOTNET_ROLL_FORWARD=Major`).
- `grep` + `after`: print only matching lines with N lines after (the full source is too long for
  the log). Use precise regexes like `public override bool TeleportTo\(|private void UpdateTeleport\(`.
  Empty `grep` prints the whole type (fine for small types like `ZSyncTransform`).
- `assets`: regex; uses **UnityPy** to list matching **GameObject names** and **TextAsset lines**
  across all asset bundles (`StreamingAssets/SoftRef/Bundles/*` — plain `strings` can't see
  inside, they're compressed). Localization rows print as `key,English`. This is how to find
  **prefab/item names** the code never mentions.
  - Item list: `assets = ^"item_[a-z0-9_]+","` → every `item_*` key with its English name.
  - Then confirm prefab names: e.g. `^([Ff]rost|[Ff]rozen|Ice$|[Tt]imber)…` → `Frostwood`,
    `FrostCore`, `FrozenFuel`, `Ice`, `FrozenKing`, `FrozenKingDrop`.
  - Gotcha: localization key ≠ prefab name (`item_frostwood` "Timberwood" → prefab `Frostwood`;
    `item_roundlog` "Corewood" → `RoundLog`).
  - Values on prefabs (speeds, damage, HP) live in serialized assets, not code. For those, log
    them at runtime instead (see §6 debug tricks).

Always verify an API by decompiling before relying on it. Signatures drift between versions
(e.g. `ZNetScene.FindInstance(ZDO)` returns a **`ZNetView`**, not a `GameObject` — a guess
cost a CI round-trip).

---

## 4. BepInEx / Harmony patterns that worked

- `[BepInPlugin(GUID, Name, Version)]`, `new Harmony(GUID).PatchAll(Assembly.GetExecutingAssembly())`.
- **Config**: bind everything in `Awake`, numbered sections, `AcceptableValueRange`, enums show as
  drop-downs in Configuration Manager (use enums instead of magic strings — the user asked for this).
  `Config.SettingChanged += ...` to refresh live data (piece descriptions/recipes).
- **Reload Config button** (from SnakePolearms): a dummy bool entry with
  `ConfigurationManagerAttributes { CustomDrawer = ... }` drawing `GUILayout.Button` that calls
  `Config.Reload()`. The attributes class is matched by name; no reference to CM needed.
- **Changing defaults for existing users**: BepInEx keeps old values in the `.cfg`. Use an
  `"Internal" / "Balance Version"` int: on load, if it's below N, overwrite specific entries with
  new defaults once, then store N. (SnakeWood does this for speeds, recipes, load capacities,
  pose.) Mention it in the README so users know.
- **Pieces**: build prefabs at `ObjectDB.Awake` (postfix), parent them under an **inactive holder
  GameObject** so nothing wakes, add to the Hammer's `PieceTable` (`ObjectDB.GetItemPrefab("Hammer")
  .GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces`), and register with `ZNetScene`
  (`m_prefabs` + `m_namedPrefabs[hash]`) in whichever of `ObjectDB.Awake`/`ZNetScene.Awake` runs
  second. Requirements must point at *this* ObjectDB's `ItemDrop`s (they're recreated per scene).
- **Crafting stations**: find the `CraftingStation` among hammer pieces by prefab name:
  `piece_workbench`, `forge`, `piece_stonecutter`, `piece_artisanstation`, `piece_magetable`
  (Galdr), `blackforge`. Vanilla build checks only that the station is **in range, not its level**
  — enforce levels yourself in a `Player.HaveRequirements(Piece, RequirementMode)` postfix with
  `CraftingStation.HaveBuildStationInRange(name, pos).GetLevel()`. Removing a piece needs its
  station in range (`Player.CheckCanRemovePiece`) — swap `piece.m_craftingStation` in a
  prefix/postfix to relax that.
- **Item quality gates** (5-star fish): `Inventory.CountItems(name, quality)` /
  `RemoveItem(name, amount, itemQuality)` exist; vanilla piece costs ignore quality, so patch
  `HaveRequirements` (postfix) and `Player.ConsumeResources` (prefix, do the removal yourself).
  Fish stars = `m_itemData.m_quality` (1–5).
- **Hiding pieces until unlocked**: return false from `HaveRequirements` for every mode (including
  `IsKnown`) → not shown in the build menu. Boss keys: `defeated_eikthyr`, `defeated_gdking`,
  `defeated_bonemass`, `defeated_dragon`, `defeated_goblinking`, `defeated_queen`,
  `defeated_fader` (Deep North boss `FrozenKing` key **unverified**); check
  `ZoneSystem.instance.GetGlobalKey(k) || player.HaveUniqueKey(k)`.
- **Prefix/Postfix state** via `out T __state`; restore in Postfix or a **Finalizer** if the
  original can throw (SnakePolearms).
- Transpilers: see SnakePolearms — find one instruction, replace with a helper call, log and bail
  if not found.
- Borrow vanilla effects instead of shipping assets: `ZNetScene.GetPrefab("Cart")
  .GetComponent<WearNTear>().m_hitEffect / m_destroyedEffect` → `EffectList.Create(pos, rot)`.
  Particle materials: search prefabs' `ParticleSystemRenderer.sharedMaterial` by name ("smoke",
  "spark"), copy `textureSheetAnimation` tiles if it's a sprite sheet.
- Icons: draw/recolour a PNG with a small Python tool (`tools/make_icon.py`), embed, `Sprite.Create`.

---

## 5. Game systems cheat-sheet (verified by decompile unless noted)

**Networking (ZDO/ZNetView)**

- Every networked object: `ZNetView` + `ZDO`. Only the **owner** simulates; others read synced
  data. `m_nview.IsOwner()`, `ClaimOwnership()`, `zdo.SetOwner(ZDOMan.GetSessionID())`.
- Custom state: `zdo.Set/GetFloat/GetBool/GetString/GetVec3(hash)` with
  `"Mod_key".GetStableHashCode()`. Change it on the owner (send an RPC to the owner from others).
- RPCs: `m_nview.Register<T>("Name", handler)` in Awake; `InvokeRPC("Name", args)` goes to the
  owner, `InvokeRPC(ZNetView.Everybody, ...)` to all (use for cosmetic effects). Up to several
  typed args (`Register<Vector3, bool>` works).
- `ZNetScene` destroys instances whose ZDO is outside the active area around the reference
  position; the **ZDO survives** (if persistent) and is recreated when in range.
  `ZNetScene.instance.FindInstance(zdo)` → `ZNetView` or null. `ZDOMan.instance.GetZDO(id)`.
- Prefab swapping for unmodded clients: prefix `ZNetScene.CreateObject(ZDO)`; `zdo.SetPrefab(hash)`
  changes what everyone instantiates. SnakeWood saves logs as vanilla `TreeLog` half-log prefabs
  (unmodded players see a floating log), stores its real variant in a ZDO string, and swaps back
  to its own prefab on modded clients. Set health huge so vanilla players can't chop it.

**ZSyncTransform** (decompiled in full):

- Owner (`OwnerSync`, LateUpdate): writes position, `s_velHash` velocity, rotation, and with
  `m_syncBodyVelocity` the body velocities. Characters on a parent use relative-position sync
  (`GetRelativePosition`, attach joints).
- Watchers (`ClientSync`, FixedUpdate): if `m_isKinematicBody`: `MovePosition(Lerp(pos, zdoPos, 0.2))`,
  `MoveRotation(zdoRot)` — **no extrapolation**. Otherwise `SyncPosition` lerps toward
  `zdoPos + vel * timeSinceUpdate` (extrapolates up to **2 s**) **and** sets the body velocity, so
  fast/turning objects overshoot badly under lag. For player-driven vehicles, make the body
  kinematic on non-owners and set `sync.m_isKinematicBody = true` (it's cached at Awake).

**Player / riding**

- `IDoodadController` (`ApplyControlls(moveDir, lookDir, run, autoRun, block)`, `OnUseStop`,
  `GetControlledComponent`, `GetPosition`, `IsValid`) + `Player.StartDoodadControl` /
  `StopDoodadControl` + `Player.AttachStart(attachPoint, colliderRoot, hideWeapons, isBed, onShip,
  animation, detachOffset)` / `AttachStop()`. Ownership handshake copied from `Sadle` (Lox):
  RPC request → owner sets `ZDOVars.s_user` and `SetOwner(sender)` → response → attach. If the
  owner is an unmodded client it never answers: time out after ~1.5 s and claim ownership.
- `Player.UpdateAttach` sets the player's transform to the attach point **every FixedUpdate**
  (no parenting). Attach animations: `attach_lox`, `attach_chair`, `attach_bed`; toggling
  `m_zanim.SetBool(m_attachAnimation, false)` makes an attached player **stand** (used for "Surf"
  and for a combat stance).
- `Character.CanMove` refuses while the animator tag is "sitting" → attacks fail while seated.
- `Player.SetControls` treats Jump/attack as "get off" for any doodad → prefix to reinterpret.
- `UpdateAttackBowDraw` cancels bow draws while `IsAttached()`.
- Attack raycasts include the "vehicle" layer → the rider hits their own vehicle; move its
  colliders to "Ignore Raycast" during the rider's attacks.
- `UpdateHover` clears `m_hovering` while controlling a doodad; `Use` then either interacts with
  `m_hovering` or calls `StopDoodadControl()`. Postfix `UpdateHover` +
  `FindHoverObject(out hover, out creature)` to allow doors etc. while riding.
- `Character` carries players standing on a moving rigidbody only by its point velocity, not
  around turns — carry passengers through yaw changes yourself.
- Stamina/eitr: `UseStamina`, `HaveStamina`, `GetStamina`, `UseEitr`, `HaveEitr`, `GetMaxEitr`;
  `Hud.instance.StaminaBarEmptyFlash()`. Noise: `Character.AddNoise(range)`.
- Inventory: `GetTotalWeight()`, `IsTeleportable(bool allowAllItems)`, `CountItems(name, quality)`,
  `RemoveItem(name, amount, quality)`, `AddItem(ItemData)`, `ItemDrop.DropItem(data, amount, pos, rot)`.
  Names are `m_shared.m_name` (localization tokens like `$item_resin`).

**Teleporting** (`Player.TeleportTo` / `UpdateTeleport`, `TeleportWorld.Teleport`)

- `TeleportTo` refuses during an active teleport or within a **2 s cooldown**; sets
  `m_teleporting`, target pos/rot. `UpdateTeleport` waits 2 s, then sets the transform to the
  target every step until `ZNetScene.IsAreaReady(target)` and `FindFloor` succeed (distant
  teleports wait ≥ 8 s or 15 s fallback). Portal exit = linked portal pos + forward * `m_exitDistance` + up.
- Trigger is `TeleportWorldTrigger.OnTriggerEnter` on the local player's collider.
- **An attached player can never finish a teleport** (UpdateAttach drags them back to the seat
  each step) → infinite black screen. Skip `UpdateAttach` while teleporting.
- Moving the player (or something that moves the player) into an **unloaded** area makes
  `ZNetScene` unload the local player ("Local player destroyed" in the log → black screen).

**Physics / misc**

- Water level: `ZoneSystem.instance.m_waterLevel`; ground: `GetGroundHeight`, `GetSolidHeight`,
  `FindFloor`. Layers: `Default, static_solid, Default_small, piece, terrain, vehicle, character`.
- `Heightmap.Biome` includes `DeepNorth` (0x40).
- Damage: `HitData` (`m_damage.m_blunt/m_chop/m_pickaxe/m_fire/m_frost/m_poison`,
  `m_toolTier`, `m_pushForce`, `SetAttacker`), `IDestructible.Damage(hit)`.
  `IRemoved.OnRemoved()` is called on deconstruction.
- Items (prefab names): Wood, FineWood, RoundLog (Corewood), ElderBark, YggdrasilWood,
  Blackwood (Ashwood), **Frostwood (Timberwood)**, Resin, FishRaw, Fish1–Fish12 (Fish4_cave =
  Tetra), TrophyDeer, AncientSeed, WitheredBone, DragonEgg, GoblinTotem, DvergrKeyFragment,
  BellFragment, FrostCore, FrozenFuel (Liquid Frost), Ice, FrozenKingDrop, SerpentScale,
  FreezeGland, TrollHide, DeerHide, Guck, Sap.
- Vanilla tree logs (`TreeLog`) used as disguises: `*_log_half` variants
  (FirTree/PineTree/beech/Birch/Oak/yggashoot/AshlandsTreeLogHalf…), sizes logged by SnakeWood.

---

## 6. Debugging tricks

- Ask the user for `BepInEx/LogOutput.log`. Log *decisions* (which disguise was chosen and why,
  which material was borrowed) at Info level once — it made remote debugging possible.
- Log runtime prefab values the decompiler can't show (e.g. creature `m_runSpeed` at
  `ZNetScene.Awake`).
- Opt-in debug config (`"9 - Debug"`): e.g. SnakeWood's **Sync Debug** logs per-log update rate,
  longest gap and visual offset every 2 s — have rider and watcher both enable it and compare.
- Read vanilla log lines in their logs as breadcrumbs: `Teleportation TRIGGER`, `Teleporting X`,
  `Stop doodad controlls`, `Local player destroyed`, `Ignoring N colliders` (AttachStart).
- Other mods spam errors (ValheimPlus `Tameable_Awake_Patch` NREs) — don't chase them.

---

## 7. SnakeWood specifics (as of v1.14)

Files: `Plugin.cs` (config, migrations), `HoverLogVariant.cs` (18 variants: 7 woods incl.
Timberwood, 12 fish — data + per-log config + tooltip), `HoverLog.cs` (riding, physics, fuel,
wear/wreck, load, noise, ramming, poison cloud, passengers, sync, unstick),
`HoverLogPrefabs.cs` (prefab building, most Harmony patches), `HoverLogDisguise.cs` (vanilla
TreeLog disguise), `HoverLogFx.cs` (glow, trails), `HoverLogPortal.cs` (mounted portals, doors).

Design decisions the user made (keep them):

- Logs are **scout/utility** rides, not haulers: top speed ~Lox (Wood/Core/Ancient can't catch
  a hare; Fine/Yggdrasil can). Weight = soft cart-like curve (70% at Load Capacity, steep beyond),
  never a hard refusal. Slope = max climb angle per log (also caps nose pitch).
- Costs: lots of material + boss summon items + **crafting station levels**; boss-kill gating is
  optional (off). Fish logs need **level-5 fish** and are deliberately strong.
- Wear only from crashes/rams; worn out → **wreck** pinned in place until fully repaired with
  its own wood (raw fish for fish logs). Never lose materials by default. Removal needs only a
  workbench.
- Fuel: resin; R takes it back out; deconstruct returns it.
- Ore/metal restriction exists but is **off**; portals allowed; rider stays mounted through them.
- Defaults the user picked: Surf pose, Crouch lowers the log, Mounted Combat on, Vanilla Disguise on.
- Tooltips must stay short (Valheim auto-shrinks long tooltip text until unreadable).

Known open items: in-game verification of v1.14 (mounted portals, unstick, doors, Core mining
balance); FrozenKing boss key; possible Deep North fish; a race timer HUD was suggested (no
placed assets needed).

---

## 8. Working with this user

- They test in-game and iterate quickly; small frequent releases are welcome, but **every push
  ships**, so validate (re-read diffs, decompile APIs) first.
- When they send several messages mid-task, fold each into the current work and confirm briefly.
- They care about: unmodded friends still seeing something sensible, multiplayer robustness,
  balance that feels natural (curves, not hard walls), and everything being configurable.
- Report with the release link, what changed, and a short in-game test list.
