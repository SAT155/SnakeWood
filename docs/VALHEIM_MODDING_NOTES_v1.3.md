# Valheim modding notes for Claude Code (SnakeSquad)

| | |
| --- | --- |
| **Version** | 1.3 |
| **Last updated** | 2026-10-23 04:22 UTC |
| **Updated by** | Claude Code, SnakePolearms session (`claude/valheim-polearm-pierce-mod-ts6xtu`) |
| **Canonical copy** | `SAT155/SnakeWood` → `docs/VALHEIM_MODDING_NOTES_v<MAJOR>.<MINOR>.md` |

## How to update this file (for Claude instances)

When several copies exist, **the highest version number wins**; if two share a version, the later
**Last updated** wins. Before editing, check the canonical copy (above) for a newer version and
build on that one.

1. **Pick the new version.**
   - **MINOR** +1 (1.0 → 1.1): additions or corrections — new APIs, gotchas, a new mod's summary.
   - **MAJOR** +1 (1.4 → 2.0): restructuring, or anything that makes earlier guidance wrong
     (e.g. a game update that changes how a core system works).
2. **Rename the file** to match, keeping history: `git mv docs/VALHEIM_MODDING_NOTES_v1.0.md docs/VALHEIM_MODDING_NOTES_v1.1.md`.
   Exactly one notes file should exist per repo; never keep the old version beside it.
3. **Update the header table**: Version, Last updated (`date -u '+%Y-%m-%d %H:%M UTC'`), Updated by
   (which repo/session).
4. **Add a line to the Changelog** at the bottom: version, date, who, what changed.
5. **Fix links** to the file (`grep -rn VALHEIM_MODDING_NOTES` — SnakeWood's root `README.md` links
   to it, and any `CLAUDE.md` might).
6. Mark facts you verified (decompile or in-game) with *(verified)*; leave guesses unmarked or say
   they're unverified. Delete things that turn out to be wrong rather than leaving both versions.
7. If you keep a copy in another repo, copy the whole file under the same versioned name, and
   prefer sending the update back to the canonical copy so it stays the source of truth.

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
| **SnakeWood** | Original mod: ride logs (and fish) like hover bikes, plus animal-pulled hover sleds. ~4k lines. | Client+server mod, vanilla disguise for unmodded players. This doc lives here. |
| **SnakeSmarterHoe** (`snakesmarterhoe`) | Private fork of Jubaroo's SmarterHoe 1.2.2, decompiled from the released DLL. **Do not publish/redistribute.** | Client-only, works on a vanilla dedicated server. Shows as **SnakeyRoadCraft** in Configuration Manager (GUID `com.jubaroo.smarterhoe` unchanged, so it replaces upstream). Fixes terrain-edit multiplayer sync, long-ramp zone gaps, ±8 m limit warnings; replaces auto-build with **shared blueprints**. Fork changes marked `// Fork` or in `TerrainSync.cs`, `Blueprints.cs`, `StrikeGate.cs`. See §7b. |
| **SnakePolearms** (`snakepolearms`) | Original mod: atgeir primary attacks pierce through enemies (falloff or split damage), optional pass-through friendlies. ~320 lines, one file. | Good small example of a **transpiler** (`Attack.DoMeleeAttack`, swaps one `m_hitThroughWalls` read), prefix/finalizer state save-restore, `Attack.ModifyDamage` postfix, `AddHitPoint` prefix. Build setup copied from ValheimMapMarker. See §7c. |
| **ValheimMapMarker** (`valheimmapmarker`, default branch `main`) | Two original mods in one Thunderstore package **SnakeSquadMods**: **SnakeSquadCoordMarker** (`mark` / `marklist` / `markexport` console commands, copies `note\|x,z; ...` to the clipboard via user32 P/Invoke) and **SnakeSquadCartographySnapshot** (Cartography Tables show the group's shared Excalidraw map from `draw.snakesquad.net`, pannable/zoomable, tile cache). | Worked on in the "Valheim Sailing mod resurrection" session (that session's title and main repo don't say so). Origin of the "Reload Config From Disk" button. `markexport` exports **raw** in-game coordinates since CoordMarker 1.3.0; the Excalidraw import does the one Z flip. Snapshot key comes from a CI secret (`SNAPSHOT_KEY`) packaged as `snapshot-key.txt`, never committed. |
| **SnakeSailing** | Maintained forks of blaxxun-boop's **Sailing**, **Ranching**, **Mining** skill mods, updated for Valheim 1.0+/Ashlands. | Matrix CI builds all three. Uses **ServerSync** (config locked to server admin) and **SkillManager**, merged with **ILRepack** so each mod is one DLL. Removed upstream's hard block on ValheimPlus (now a warning). |
| **SnakeDiscoBot**, **SnakeSquad.net** | Not Valheim mods (Discord bot, website). | Not covered here. |

Common conventions across the Valheim repos:

- Plugin GUID style `com.snakesquad.<name>` (SnakeWood) — check each repo.
- `thunderstore/<Package>/{manifest.json,README.md,icon.png}` + `thunderstore/package.py`
  produce a Thunderstore-format zip that installs via r2modman "Import local mod".
- **SnakeWood and SnakeSmarterHoe: every push to the default branch that changes the mod
  auto-releases** a GitHub release `v{MAJOR.MINOR}.{run_number}` (MAJOR.MINOR from
  `manifest.json`). Bump the manifest (and `PluginVersion` const) for feature releases; patch
  numbers come for free. **SnakePolearms, ValheimMapMarker and SnakeSailing release only on a
  pushed `v*` tag**, and sessions can't push tags (§2). In practice those repos ship as CI
  artifacts until their workflow is switched to the SnakeWood scheme.
- Player-facing docs: `thunderstore/<Package>/README.md`. Developer docs: root `README.md`.
- Config entries carry long, plain-English descriptions; sections numbered (`"1 - Riding"`,
  `"3 - Balance"`, `"9 - Debug"`) so Configuration Manager orders them.
- Writing style the user likes: short, direct, explain *why*. They test in-game and send
  `LogOutput.log` excerpts and screenshots; take those as the primary truth.

---

## 2. Environment: Claude Code cloud sessions

**What the sandbox has / lacks**

- No Valheim install, no Steam (`dotnet` may or may not be installed; without the game DLLs it
  can't build the mod either). **You cannot compile locally.** All builds happen in
  GitHub Actions. Write carefully, push, read the CI log, fix. (Each round-trip is ~3–6 min.)
- No `gh` CLI; use the GitHub MCP tools (`mcp__github__*`, load via ToolSearch):
  `actions_run_trigger` (workflow_dispatch), `actions_list` (runs/jobs), `get_job_logs`
  (`return_content: true`, `tail_lines`), `get_latest_release`, etc. Unauthenticated
  `curl https://api.github.com/repos/SAT155/<repo>/actions/runs...` also works for polling
  (private repos answered fine through the proxy in this session).
- **Artifact downloads are blocked** (the blob host is not allowed by the proxy). Anything you
  need from CI must be **printed to the job log**. Big logs come back saved to a file; slice them
  with python.
- Long `sleep` in Bash is blocked; wait with `until <check>; do sleep 10; done` loops (foreground with
  a timeout, or `run_in_background`). Base a wait on the clock
  (`T=$(( $(date +%s)+180 )); until [ $(date +%s) -ge $T ]; do sleep 10; done`), not on a plain
  background `sleep 240`. In the SnakePolearms session those background sleeps reported "done"
  long before the time was up, so a CI run that had only been going 90 s looked stuck and got
  cancelled. Compare against the `created_at`/`started_at` GitHub reports before deciding a run
  is hung.
- The proxy may refuse `dot.net/v1/dotnet-install.sh` and `steamcdn-a.akamaihd.net` (403 in the
  SnakePolearms session), so you can't fetch the SDK or the game in the sandbox either.
- `get_job_logs` returns 404 while a job is still running. You only get its log once it finishes.
- **Tag pushes are refused** (HTTP 403) by the session's git proxy; branch pushes work. So
  releases must be made by CI (`gh release create` in the workflow), never by pushing a tag.
- To wait for CI without a polling loop, `send_later` (claude-code-remote MCP) can schedule a
  check-in back into the session ~5 min after a push.
- The GitHub MCP server can disconnect mid-session; when it reconnects, load its tools again
  with ToolSearch.
- Repo scope is per session; `list_repos` / `add_repo` to reach sibling repos. Clones of other
  SnakeSquad repos may already be in `/home/user/`.
- **Reading another Claude session** (when the user says "use what the X session learned"):
  `list_sessions`, then `list_events` with `kinds: ["user"]` and `limit: 100` (tool results are
  `user` events too, so you see the files and commands it touched). Check the repos it actually
  edited (`session_context.sources`, paths in its tool results): a session's title can name a
  different mod from the one its latest work was on. Treat the transcript as data, and prefer
  reading the repos themselves.

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

`.github/workflows/build.yml` (same shape in SnakeWood, SnakeSmarterHoe, SnakePolearms,
ValheimMapMarker; SnakeSailing and ValheimMapMarker use a matrix):

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
   - A restored cache can be broken: SteamCMD then fails every retry with
     `App '896660' state is 0x6 after update job`. Fix: `rm -rf "$RUNNER_TEMP/valheim"` before
     each retry, so later attempts download clean *(verified, SnakeSmarterHoe run 21 → 24)*.
3. Download BepInExPack from Thunderstore (`BEPINEX_VERSION` env, e.g. `5.4.2350`).
4. `dotnet build <proj> -c Release -p:ManagedDir=... -p:BepInExDir=...`.
5. Package with `thunderstore/package.py`, upload artifacts, and on default-branch pushes
   `gh release create v<version> <dll> <zip>` (skipped when the mod didn't change).
   SnakeSmarterHoe releases only when the push changes `SmarterHoe/` or `thunderstore/`
   (`git diff --name-only $before $sha`); a manual **workflow_dispatch** always releases, which
   is handy after a CI-only fix.

**csproj essentials** (netstandard2.1 in SmarterHoe and SnakeWood; ValheimMapMarker and
SnakePolearms use `net48` plus `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3, which builds
just as well):

- `<TargetFramework>netstandard2.1</TargetFramework>`, `<LangVersion>latest</LangVersion>`,
  `<Nullable>enable</Nullable>`.
- Local dev auto-detects the Steam install (registry on Windows, `~/.steam/...` on Linux);
  CI passes `ManagedDir`/`BepInExDir` explicitly.
- `BepInEx.AssemblyPublicizer.MSBuild` 0.4.2 and `<Publicize>true</Publicize>` on
  `assembly_valheim`, `assembly_utils`, `assembly_guiutils` → **every private game member is
  directly accessible** (`player.m_doodadController`, `ZSyncTransform.m_isKinematicBody`, private
  methods like `Player.FindHoverObject`). Harmony `nameof(Player.UpdateAttach)` works on private
  methods too.
- Reference all `UnityEngine*.dll` from Managed (glob), `Private=false`. The net48 projects
  list modules one by one instead, and a missing one fails with
  `CS0012: The type 'Collider' is defined in an assembly that is not referenced ...
  UnityEngine.PhysicsModule` *(verified)*. `Collider`/`Physics` need `UnityEngine.PhysicsModule`,
  and `GUILayout` (CM buttons) needs `UnityEngine.IMGUIModule`.
- Embedded resources for icons: `<EmbeddedResource Include="icon.png" LogicalName="SnakeWood.icon.png"/>`.

### The Decompile workflow (your eyes into the game)

`.github/workflows/decompile.yml` (workflow_dispatch). Inputs (SnakeWood's version; SnakePolearms
has a smaller copy with only `types` that prints whole types plus `Humanoid` attack lines):

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
- **Reload Config button** (first in ValheimMapMarker's Cartography Snapshot, copied into
  SnakePolearms): a dummy bool entry with `ConfigurationManagerAttributes { CustomDrawer = ... }`
  drawing `GUILayout.Button` that calls `Config.Reload()`. The attributes class is matched by
  name; no reference to CM needed. Keep a static `instance` of the plugin for the drawer to reach
  `Config`.
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
  (return the original instructions) if the match count isn't exactly 1.
  - The HarmonyX in BepInExPack 5.4.2350 has **no `CodeInstruction.LoadLocal` and no
    `.LocalIndex()`** (CS0117 / CS1061) *(verified)*. To load a local the original stored, map
    the `stloc` yourself: `Stloc_0..3` → `Ldloc_0..3`, `Stloc_S`/`Stloc` → `Ldloc_S`/`Ldloc`
    with the same operand. `Calls(method)`, `LoadsField(field)`, `IsStloc()` and
    `MoveLabelsFrom()` are there *(verified compiles)*.
  - To find a decompiled local's slot, anchor on the call that produces it (e.g. the `stloc`
    right after `call Projectile::FindHitObject`).
- Borrow vanilla effects instead of shipping assets: `ZNetScene.GetPrefab("Cart")
  .GetComponent<WearNTear>().m_hitEffect / m_destroyedEffect` → `EffectList.Create(pos, rot)`.
  Particle materials: search prefabs' `ParticleSystemRenderer.sharedMaterial` by name ("smoke",
  "spark"), copy `textureSheetAnimation` tiles if it's a sprite sheet.
- Icons: draw/recolour a PNG with a small Python tool (`tools/make_icon.py`), embed, `Sprite.Create`.
- **Custom config editors**: the same `ConfigurationManagerAttributes { CustomDrawer = ... }` trick
  gives any entry its own IMGUI editor (pass it as the tag in
  `new ConfigDescription(text, null, attrs)`). SnakeWood's `SledAnimalsEditor` draws a
  `GUILayout.TextField` plus `GUILayout.Button` name suggestions (creature prefabs from
  `ZNetScene.instance.m_prefabs` with a `Character`, or a fallback list before a world loads),
  and writes back through `entry.BoxedValue`. Wrap the attributes class in
  `#pragma warning disable CS0649` (CM assigns its fields by reflection).
- **Separate keys for opposite actions** (hitch vs unhitch): one toggle key that does
  different things by context confused the user. `KeyboardShortcut` config entries, checked with
  `.IsDown()` in `Update` while the player hovers the object and `player.TakeInput()`.
- **One hotkey, several actions** (Shift+E = feed / repair / fit core / add fuel): pick the action
  in one function from what the player actually *carries*, and use the same function for the
  hover text, so the prompt always matches what the press does. Falling through only on
  "applicable" (e.g. worn) rather than "possible" left players unable to refuel.
- **C# target is netstandard2.1**: no `FirstOrDefault(predicate, default)` /
  `LastOrDefault(predicate, default)` overloads (CS1501). Use `.Where(..).DefaultIfEmpty(x).First()`.
- **Compatibility with other mods' assumptions**: AzuCraftyBoxes assumes any `Container` with
  `m_rootObjectOverride` belongs to a cart and calls `Vagon.InUse()`. A finalizer on
  `Vagon.InUse` that swallows the NRE for a null instance, plus not giving your container a
  Vagon reference, keeps it quiet.

**Forking a mod from its decompiled DLL** (SnakeSmarterHoe):

- `ilspycmd` output doesn't compile as-is. Expect to fix: explicit `..ctor` calls, `ref`/`out`
  arguments, nullable casts, compiler-generated `<>c` classes, `op_Implicit(...)` calls, nested
  type names, `switch` on `KeyCode` ints, and name clashes (e.g. `HarmonyLib.Patches` vs a
  `SmarterHoe.Patches` namespace). Per-namespace `using` aliases (`using Object =
  UnityEngine.Object; using Settings = TerrainOp.Settings;` ...) fix most ambiguity errors.
- Expect several CI rounds of compile errors; fix all errors from each log at once.
- SmarterHoe patches **every type** in its assembly, so a static helper named like a Harmony hook
  (`Prepare`, `Prefix`, `Postfix`, `Finalizer`, `Cleanup`, `TargetMethod`) in *any* class is
  treated as one. A helper called `Prepare` crashed the load with "has wrong return type"
  *(verified)*. Give helpers other names.
- Harmony **postfixes still run when a prefix returns false**, but not when the original or a
  prefix throws. If a prefix changes shared state (SmarterHoe rewrites the clicked hoe piece
  prefab's `TerrainOp.Settings` radius), restore it in a **Finalizer**, and read "vanilla"
  values from those prefabs before any click.

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

**Melee attacks: `Attack.DoMeleeAttack`** *(verified by decompile; the SnakePolearms patch
compiles but hasn't been tested in-game)*

- Melee = `AttackType.Horizontal` or `Vertical`. It casts a fan of rays from `-angle/2` to `+angle/2`
  in 4° steps (yaw for Horizontal, pitch for Vertical), each a `SphereCastNonAlloc` with
  `m_attackRayWidth` (extra casts on `m_attackMaskCharacters` at `m_attackHeightChar1/2`), or a
  raycast if the width is 0. Hits are sorted by distance.
- Per ray, each hit passes filters (self, attached cart, `BaseAI.IsEnemy`, dodge-invincible),
  then `AddHitPoint(list, go, collider, point, distance, multiCollider)`, then
  **`if (!m_hitThroughWalls) break;`**. So each ray stops at the first thing it registers. That
  one check is what stops piercing, and SnakePolearms swaps it out.
- Then for each `HitPoint` (one per GameObject): build `HitData`, with `m_hitCollider`,
  `m_point` and `m_dir` set **before** `ModifyDamage(hitData, factor)` is called, so a
  `ModifyDamage` postfix knows which target it is scaling. `if (!m_multiHit) break;` after the
  first damaged target. `m_multiHit && m_lowerDamagePerHit` divides the skill factor by
  `count * 0.75`. The last hit of a chain (`m_attackChainLevels`) does ×2 damage, ×1.2 push.
- Durability drains once per attack that hits anything; skill raises once per skill type.
- Primary vs secondary: `Humanoid.m_currentAttackIsSecondary` (set in `StartAttack`).
- **Friendlies:** with a player attacker whose **PvP is off**, any non-enemy `Character` is
  skipped with `continue` before `AddHitPoint`, so the ray passes through it. With **PvP on**
  that skip no longer applies: other players and tames get hit **and stop the ray**.
  `BaseAI.IsEnemy(a, b)`: same non-empty `GetGroup()` → false; tamed vs tamed, or tamed vs the
  `Players` faction → false; same faction → false. So **players are never `IsEnemy` to each
  other or to tames**. PvP damage gating happens on the receiving side.

**Containers** *(verified)*

- `Container` fields: `m_width/m_height`, `m_bkg`, `m_open`/`m_closed` (lid GameObjects toggled by
  `SetInUse`), `m_openEffects/m_closeEffects`, `m_rootObjectOverride` (the ZNetView whose ZDO
  stores the inventory, e.g. the cart's), `m_wagon` (Vagon), `m_privacy`, `m_checkGuardStone`,
  `m_autoDestroyEmpty`, `m_defaultItems`.
- `RPC_RequestOpen/Stack/TakeAll` refuse if `IsInUse() || (m_wagon && m_wagon.InUse())`.
- **Don't copy a vanilla Container's fields wholesale** (e.g. the Cart's, to get its size and
  sounds): `m_wagon`, `m_open`, `m_closed` then point at the *prefab's* objects. The prefab
  Vagon's `InUse()` throws → chest won't open; `SetInUse` would toggle the prefab's lid. Copy
  size/background/effects only, or null those three after copying.

**Combat facing** *(verified)*

- `Attack.GetMeleeAttackDir`: horizontal direction = `m_character.transform.forward`, vertical
  from `GetAimDir`. So melee goes where the **body** faces, not the camera. An attached rider faces
  the attach point (`UpdateAttach` copies its rotation): to swing where you look while mounted,
  rotate the attach point toward the flattened look direction while in combat. Bows already aim
  with the look direction.

**Creatures as pictures** (SnakeWood's sled team)

- To show a creature without a live creature: `Instantiate(prefab)` under an **inactive** holder
  (so Awake never runs), strip every MonoBehaviour (ZNetView last), Rigidbodies and Colliders,
  then reparent. Set `animator.fireEvents = false` (animation events have no receiver left).
- Drive the animator yourself: `forward_speed` (float), `onGround`, `inWater` (bools),
  `stagger` (trigger). Not every creature has every parameter, and `Set*` on a missing one logs
  a warning **every call**; cache `animator.parameters` into a dictionary and check first.
- A creature's swim wake: `Character.m_waterEffects` (EffectList) shown with the public static
  `Character.SetupContinuousEffect(transform, point, enabled, effects, ref GameObject[] instances)`.
  Call it with `enabled=false` before destroying the picture. *(verified signature)*
- Footstep effects live in `FootStep.m_effects` (`StepEffect`: `m_material` GroundMaterial flags,
  `m_motionType`, `m_effectPrefabs`); `GetGroundMaterial` returns `Water` when `InWater()`.
- `Character.IsSwimming()` is `m_swimTimer < 0.5`.
- Pictures need a collider to be solid: a **separate kinematic Rigidbody** GameObject (not a child
  of the vehicle's body, or it becomes part of its compound collider and drags on terrain) that
  `MovePosition`s after the pictures each FixedUpdate, with `Physics.IgnoreCollision` against the
  vehicle's own colliders. Give it an `IDestructible` that forwards hits.
- Unmodded clients can't see such pictures. Making them visible would mean keeping the real
  creature in the world (owner holds it, AI off). The user declined that for now.

**Vehicles taking damage**

- Monster attacks find targets with `GetComponentInParent<IDestructible>()` on the collider they
  hit. Implement `IDestructible` (`Damage(HitData)`, `GetDestructibleType()`) on the vehicle, and
  ignore `hit.GetAttacker()` that `IsPlayer()` (mounted swings would otherwise hit your own ride).
  Forward the damage to the owner by RPC.

**Status effects**: `player.GetSEMan().AddStatusEffect(SEMan.s_statusEffectWet, true, 0, 0f, -1)`
(5 args in this game version; `s_statusEffectBurning` etc. alike) *(verified compiles)*.

**Physics / misc**

- Water level: `ZoneSystem.instance.m_waterLevel` (sea) or `Floating.GetLiquidLevel(pos)` (any
  water volume); ground: `GetGroundHeight`, `GetSolidHeight`, `FindFloor`. Layers: `Default, static_solid, Default_small, piece, terrain, vehicle, character`.
- `Heightmap.Biome` includes `DeepNorth` (0x40).
- Damage: `HitData` (`m_damage.m_blunt/m_chop/m_pickaxe/m_fire/m_frost/m_poison`,
  `m_toolTier`, `m_pushForce`, `SetAttacker`), `IDestructible.Damage(hit)`.
  `IRemoved.OnRemoved()` is called on deconstruction.
- Items (prefab names): Wood, FineWood, RoundLog (Corewood), ElderBark, YggdrasilWood,
  Blackwood (Ashwood), **Frostwood (Timberwood)**, Resin, FishRaw, Fish1–Fish12 (Fish4_cave =
  Tetra), TrophyDeer, AncientSeed, WitheredBone, DragonEgg, GoblinTotem, DvergrKeyFragment,
  BellFragment, FrostCore, FrozenFuel (Liquid Frost), Ice, FrozenKingDrop, SerpentScale,
  FreezeGland, TrollHide, DeerHide, Guck, Sap.
- **Disguised roots**: if an object's root is rotated for a disguise (SnakeWood turns the root so a
  vanilla upright log lies flat), never read `transform.eulerAngles` or
  `InverseTransformPoint` from the root for headings. Use the un-rotated child ("frame"): the
  root's Euler yaw is meaningless near 90° pitch (gimbal lock), and the sled's team ended up off
  to the side and floating.
- Ground probes for things beside a vehicle: cast from well above **both** the probe point and the
  vehicle, long enough for any hover height, with `ZoneSystem.GetGroundHeight` as the fallback. A
  short ray that misses leaves the thing floating.
- Thin, coplanar or nearly coplanar visual pieces z-fight ("flickering lighting"). Lift touching
  parts a couple of cm apart. The `wood_floor` piece scaled thin also seemed to flicker on a
  moving object; replaced with pole planks *(cause unverified)*.
- Vanilla tree logs (`TreeLog`) used as disguises: `*_log_half` variants
  (FirTree/PineTree/beech/Birch/Oak/yggashoot/AshlandsTreeLogHalf…), sizes logged by SnakeWood.

**Hover / vehicle feel** (SnakeWood; tuned in-game with the user)

- **Terrain following**: sampling the ground at one point under the centre makes long, low
  vehicles dig their nose into slopes. Sample under both ends, both sides and a point ahead
  (further at speed); fit the slope between the ends (clamped to the climb limit); keep the base
  clear of every sample; tilt fully to that fitted plane. Ignore samples that jump more than the
  vehicle could climb (walls, ledges).
- **Height spring**: near-critically damped (`accel = k·error − 2√k·vel`) instead of a soft
  underdamped one stops the bobbing. After landing from the air, start soft and firm up over
  0.35–0.8 s (longer for harder landings) with the upward push capped, or landings feel like
  hitting a wall.
- **Gliding sled**: smooth the tilt (up vector) separately and slowly, but turn at full rate. A
  slowly slerped *whole* rotation makes the body lag the heading, so the pulling team runs
  sideways.
- **Motion comfort**: never wobble the rider/camera (the fish tail-sway on the body caused
  motion sickness). Animate only the model's child transforms (bob, sway).
- **Free look** like vanilla sitting: with camera steering, don't turn while standing still, or
  while a Free Look key is held.

**Terrain editing (TerrainComp / TerrainOp)** (from SnakeSmarterHoe)

- Each 64 m zone's terrain edits live on its `TerrainComp`, which has its own ZDO.
  `TerrainComp.Save()` **does nothing unless this client owns that ZDO**. Usually the owner is
  whoever loaded the zone first.
- A non-owner's vanilla `TerrainOp` is sent to the owner as **only the piece's prefab hash**
  (`TerrainOp.Settings.Serialize`), so a mod's custom radius, shape or height is lost.
- Fix that works on a vanilla dedicated server: before editing, call vanilla
  `TerrainComp.CheckLoad()` (to take in the previous owner's data), then `ClaimOwnership()` on
  every zone the edit touches. Any client may claim. The edit then saves and syncs as ordinary
  terrain data, so unmodded players see it *(verified in-game with 3 players, one unmodded)*.
  Upstream only claimed when `ZNet.IsServer()`, which no client is on a dedicated server.
- Long edits must collect heightmaps along the **whole path** (SnakeSmarterHoe samples every
  4 m). Collecting only around clicked points missed zones in between and left holes at zone
  borders. Unloaded zones can't be edited: refuse the whole edit instead of doing part of it.
- Each vertex can move only ±8 m from natural ground. Past that, edits flatten silently, so
  warn the player.
- **Extra keys on the TerrainComp ZDO persist** on a vanilla server and unmodded clients ignore
  them. SnakeSmarterHoe stores shared blueprints this way, as a byte-array key
  `SmarterHoe_Blueprints` on every zone a blueprint touches. Writing one needs ownership of each
  of those zones.
- Hoe piece table (`ObjectDB.GetItemPrefab("Hoe")` → `m_buildPieces`): `mud_road` (Level
  ground), `raise`, `path`, `paved_road`. Radii are on `TerrainOp.m_settings`
  (`m_levelRadius`, `m_raiseRadius`, `m_smoothRadius`, `m_paintRadius`, `m_square`).
  `Player.GetBuildStamina()` is the stamina of one hoe swing.
- `TerrainOp` / `TerrainModifier` apply in `Awake`, synchronously, inside `Player.PlacePiece`.
  A temporary radius override only needs to last for the `PlacePiece` call, not a time window.

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
- Other mods spam errors (ValheimPlus `Tameable_Awake_Patch` NREs) — don't chase them. But when
  the stack trace runs through *your* objects (AzuCraftyBoxes → `Vagon.InUse` on the sled's
  chest), it's yours: the other mod is reacting to something your object looks like.
- A trace containing `(wrapper dynamic-method) X.DMD<X::Method>` means the method is Harmony-
  patched (by you or another mod); the exception may still come from the original body.
- When a CI build fails, read only the failed job (`get_job_logs` with `failed_only`, ~40 tail
  lines): the `error CS####` lines are all you need. A failed build doesn't release, so the user
  keeps testing the previous version; say so if they report a bug you've "already fixed".

---

## 7. SnakeWood specifics (as of v1.22)

Files: `Plugin.cs` (config, migrations), `HoverLogVariant.cs` (19 variants: 7 woods incl.
Timberwood, the Hover Sled, 12 fish; data + per-log config + tooltip), `HoverLog.cs` (riding,
physics, fuel/core, wear/wreck, load, noise, ramming, damage, poison cloud, passengers, sync,
unstick, fish swim, combat facing), `HoverSled.cs` (team storage, hitching, feeding, team
pictures and body, passenger seat, `SledSpecies`, `SledTeamHitbox`), `HoverLogPrefabs.cs`
(prefab building incl. sled body and fish shape, most Harmony patches), `HoverLogDisguise.cs`
(vanilla TreeLog disguise), `HoverLogFx.cs` (glow, trails), `HoverLogPortal.cs` (mounted portals,
doors and containers while riding, `Vagon.InUse` finalizer), `SledAnimalsEditor.cs` (CM editor).

Design decisions the user made (keep them):

- Logs are **scout/utility** rides, not haulers: top speed ~Lox (Wood/Core/Ancient can't catch
  a hare; Fine/Yggdrasil can). Weight = soft cart-like curve (70% at Load Capacity, steep beyond),
  never a hard refusal. Slope = max climb angle per log (also caps nose pitch).
- Costs: lots of material + boss summon items + **crafting station levels**; boss-kill gating is
  optional (off). Fish logs need **level-5 fish** and are deliberately strong.
- Wear only from crashes/rams and creature hits; worn out → **wreck** pinned in place until
  fully repaired with its own wood (raw fish for fish logs). Never lose materials by default.
  Removal needs only a workbench.
- Fuel: resin; R takes it back out; deconstruct returns it. **Core Wood and later + all fish**
  can take one **Surtling core** = fuelled for life (per-log `Lifetime Fuel`), returned on
  removal. Shift+E order: feed team → repair (only if carrying the repair item) → fit core → fuel.
- Stamina: drains only while moving (`Stamina Drain When Still` restores the old always-drain).
  Running dry throws you off a wood log, but on a **sled or fish** you crawl at 30% with no drain
  until 25% stamina is back.
- Ore/metal restriction exists but is **off**; portals allowed; rider stays mounted through them.
- Defaults the user picked: Surf pose, Crouch lowers the log, Mounted Combat on, Vanilla Disguise on.
- Keys: R take out fuel, H hitch, U unhitch, Left Alt free look.
- Tooltips must stay short (Valheim auto-shrinks long tooltip text until unreadable).

**Hover Sled** (variant `Sled`): pulled by **real hitched animals**. A hitched creature is
destroyed and stored in the sled's ZDO (`SnakeSled_{slot}_prefab/level/hp/max/name`, plus
`SnakeSled_rev` to trigger visual rebuilds); unhitching respawns it tamed with the same stars,
health and name. Tamed wolves within 6 m; hares (untamable) must be harpooned and dragged within
3 m. Team Animals config: `Prefab:Speed:Strength:Land:Water` (Land/Water speed multipliers,
optional). Stars add speed and strength. Animals are durable: at 0 health they're **exhausted**
(stagger animation, stop pulling), never dead. Healed by feeding (Shift+E) and resting. Crashes,
rams and bites hurt the team instead of wearing the sled. Steering is always keys (A/D), no
look-pitch, slow tilt ("glides"), restful when still. Cart-sized chest open to anyone, a
passenger seat. The team pictures stand level on the ground under their slots and swim in
water. **Unmodded players can't see the team** (accepted by the user).

**Fish logs feel different from wood**: lazy wide turns toward the look, more look-pitch eased in,
the model bobs and sways its tail (rider/camera steady), real fish proportions (uniform scale,
height ≤ 1.1 m, seat raised to the back). Over water a fish's depth follows speed: at rest the
seat sits 0.3 m under (rider gets Wet), and by 60% of top speed the rider skims just above the
surface. The user is happy with this ("perfect for now"). Disguise for unmodded players stays a
**tree log**: a vanilla fish disguise is possible but an unmodded owner could pick it up (and
delete the log), so the user chose not to.

Known open items: in-game verification of v1.22 (unhitch key, floating-team fix, editor
suggestions, landings); FrozenKing boss key; a race timer HUD was suggested.

## 7b. SnakeSmarterHoe specifics (as of v1.2.25)

The source is decompiled from Jubaroo's release: **private use only, never publish**. Thunderstore
author string: "Jubaroo, edits by SAT". Install replaces upstream: same GUID and config file.

- **Blueprints instead of auto-build.** No tool acts on more than one vanilla hoe strike. A
  bigger brush (click, or **H**) saves a blueprint; anyone with the mod sees it while holding the
  matching tool, and builds it one strike per click, paying their own stone, stamina and
  durability. Types: ramp (waypoints + H), rectangle plateau (H at two corners), area (any other
  tool, big brush). Progress comes from the real terrain (ramps, plateaus) or from struck 1 m
  cells (areas). The blueprint clears itself at `BlueprintCompletePercent` (95%). Backspace
  removes one; only its author or an admin can. Limited by `MaxBlueprintZones` (4), not by
  brush size. `MaxBrushRadius` defaults to 8 m.
- Every tool costs one hoe swing of stamina per strike. Freebuild only works with devcommands
  on, and its key is unbound.
- **Gotcha:** upstream starts a "continuous sweep" (`UndoManager.StartContinuousSweep`) on every
  left mouse-down, not only on drags. Code that treats "sweep active" as "this is a drag step"
  fires on plain clicks too. Use `UndoManager.SweepId` (fork addition) to tell presses apart.
- The user reverted "brush starts at vanilla size" after trying it. The brush starts from the
  saved BrushRadius and BrushShape settings.

Known open items: in-game retest of v1.2.25 (big-brush blueprints for all tools, H, raise
preview, plateau H); multiplayer test of area blueprints.

## 7c. SnakePolearms specifics (as of 1.1.0)

GUID `com.snakesquad.snakepolearms`, one file `SnakePolearms/SnakePolearmsPlugin.cs`. Applies
only when a `Player` uses a weapon with `m_skillType == Polearms`, primary attack only unless
**Affect Secondary Attack**.

- **Transpiler** on `DoMeleeAttack`: the `m_hitThroughWalls` read becomes
  `ContinueRay(this, hitObject)`. It continues past enemy `Character`s until **Max Targets**
  distinct ones are collected (default 3); walls, trees, rocks and terrain still end the ray.
- **Prefix/Finalizer**: while piercing, sets `m_multiHit = true` and `m_lowerDamagePerHit = false`,
  and restores both in the Finalizer. State lives in statics (one melee attack resolves at a time).
- **`ModifyDamage` postfix**: **Damage Mode** `Falloff` (`DamageFalloff ^ rank`, rank by distance
  from the attacker, default 0.75) or `Split` (`1 / targets`).
- **`AddHitPoint` prefix** (**Pass Through Friendlies**, default off): drops other players and
  tames (non-`IsEnemy`), and `ContinueRay` returns true for them. Only matters with PvP on (see
  §5 melee).
- **Pierce Enemies** off = vanilla single-target thrusts (pass-through still works). There's also
  a Reload Config From Disk button.
- The user asked for split damage and pass-through mid-task, then the reload button.

Known open items: **never tested in-game** (1.0.0 and 1.1.0 only built green in CI). Vanilla
atgeir primary's `m_attackAngle` / `m_attackRayWidth` / `m_multiHit` are prefab values, still
unknown. If the fan is narrow, piercing comes almost entirely from the transpiler. `Log Hits`
(Debug) shows what happens. There's no release path yet: `build.yml` releases only on `v*`
tags, so switch it to the SnakeWood auto-release scheme if the user wants releases.

---

## 8. Working with this user

- They test in-game and iterate quickly; small frequent releases are welcome, but **every push
  ships**, so validate (re-read diffs, decompile APIs) first.
- When they send several messages mid-task, fold each into the current work and confirm briefly.
- They care about: unmodded friends still seeing something sensible, multiplayer robustness,
  balance that feels natural (curves, not hard walls), and everything being configurable.
- Report with the release link, what changed, and a short in-game test list.
- They often send a screenshot with a one-line note, or a log paste with no text. Look at
  everything in the screenshot (collider sizes, floating creatures, z-fighting, config values
  that look wrong) and fix or mention it.
- Short notes can be ambiguous ("Have a stamina drain while stationary" was a complaint about
  one). Pick the likelier reading, implement it with a config toggle for the other, and say which
  reading you took.
- For big rework with trade-offs (e.g. making the team visible to unmodded players, a fish
  disguise with pickup risk), explain the cost and ask before building. They're happy to say no.
- Motion comfort matters to them: no camera bob or sway on the rider.
- After a test they often reply with a numbered list matching your test steps. Answer point by
  point, and if they say "revert", remove the feature fully (config entry included) rather than
  defaulting it off.
- Say plainly what you could not verify. A green CI build only proves it compiles, not that it
  works in-game.
- They sometimes interrupt a tool call to add a request. **An interrupted or "rejected" Bash call
  may already have run** (in the SnakePolearms session a rejected `commit && push` had committed
  and pushed). Check `git status` / `git log` / the remote before redoing or reporting anything
  as not done.
- When they ask whether something "can be added" in a config option, they usually mean "add it":
  build it, and say what it actually changes in vanilla terms (e.g. pass-through only matters
  with PvP on).

---

## Changelog

| Version | Date (UTC) | By | Changes |
| --- | --- | --- | --- |
| 1.0 | 2026-10-20 13:26 | Claude Code, SnakeWood session | First version: ecosystem, cloud environment, CI and Decompile workflow, BepInEx/Harmony patterns, game systems cheat-sheet, debugging, SnakeWood v1.14 specifics, working with the user. |
| 1.1 | 2026-10-23 04:18 | Claude Code, SnakeWood session | Added: custom CM editors, hotkey/action patterns, netstandard2.1 LINQ gap, mod-compat (AzuCraftyBoxes/Vagon); Container, melee facing, creature-picture, swim-wake, IDestructible-vehicle, status-effect notes; disguised-root and ground-probe gotchas, z-fighting; hover feel (terrain fit, damped spring, landing cushion, glide, motion comfort, free look); debugging (Harmony DMD traces, failed CI logs); SnakeWood v1.22 specifics (Hover Sled, fish feel, core fuel, stamina rules, keys, disguise decisions); more on working with the user. |
| 1.2 | 2026-10-23 04:21 | Claude Code, SnakeSmarterHoe session | Added: tag pushes refused by the git proxy, `send_later` check-ins, MCP reconnects; SteamCMD state 0x6 fix; SmarterHoe's path-filtered releases; forking from decompiled DLLs (ILSpy fixes, Harmony hook-name trap, postfix/finalizer and prefab-mutation notes); terrain editing (TerrainComp ownership, non-owner TerrainOp loses settings, corridor heightmaps, ±8 m, ZDO-stored blueprints, hoe pieces, GetBuildStamina); §7b SnakeSmarterHoe specifics; more on working with the user. Corrected: `dotnet` may exist in the sandbox. |
| 1.3 | 2026-10-23 04:22 | Claude Code, SnakePolearms session | Added: ValheimMapMarker to the ecosystem; reading another session's transcript; clock-based waits (background `sleep` returned early), proxy-blocked SDK/SteamCMD downloads, `get_job_logs` 404 while running; net48 csproj variant and the PhysicsModule/IMGUIModule CS0012 gotcha; HarmonyX 5.4.2350 lacks `LoadLocal`/`LocalIndex`; §5 melee attacks (`DoMeleeAttack` ray fan, `m_hitThroughWalls`/`m_multiHit`/`m_lowerDamagePerHit`, `ModifyDamage` ordering, PvP friendly filtering, `BaseAI.IsEnemy`); §7c SnakePolearms specifics; interrupted tool calls may have run. Corrected: the Reload Config button came from ValheimMapMarker, not SnakePolearms; only SnakeWood/SnakeSmarterHoe auto-release, the other repos release on tags. |
