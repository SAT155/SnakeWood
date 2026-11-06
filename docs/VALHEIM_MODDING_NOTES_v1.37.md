# Valheim modding notes for Claude Code (SnakeSquad)

| | |
| --- | --- |
| **Version** | 1.37 |
| **Last updated** | 2026-11-04 03:45 UTC |
| **Updated by** | Claude Code, SnakewaySurfer session (`claude/project-thread-9zfoi6`) |
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

**When to update (the user's standing rule, 2026-10-24):** after you change any SnakeSquad repo,
update these notes with what changed and what you learned. When several sessions have changes
waiting, they update in the order of their changes, earliest first, so later learnings build on
earlier ones. What you learn by **decompiling** goes in a small per-system topic file under
[`docs/game/`](game/README.md) (class and method names, key fields, how it fits together,
gotchas, the game version you read), linked from §5 here; never paste raw decompiled code.
Add to an existing topic file when one covers the system. Keep it short: the user wants this to
cost as few tokens as it can while staying useful.

What Claude Code sessions learned building and maintaining the SnakeSquad mods (started by
the SnakeWood session, extended by the SnakeSmarterHoe and SnakePolearms sessions). Written for the next Claude instance that
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
| **SnakeWood** | Original mod: ride logs (and fish) like hover bikes, plus animal-pulled hover sleds. ~5.2k lines. | Client+server mod, vanilla disguise for unmodded players. This doc lives here. |
| **SnakeSmarterHoe** (`snakesmarterhoe`) | Private fork of Jubaroo's SmarterHoe 1.2.2, decompiled from the released DLL. **Do not publish/redistribute.** | Client-only, works on a vanilla dedicated server. Shows as **SnakeyRoadCraft** in Configuration Manager (GUID `com.jubaroo.smarterhoe` unchanged, so it replaces upstream). Fixes terrain-edit multiplayer sync, long-ramp zone gaps, ±8 m limit warnings; replaces auto-build with **shared blueprints**. Fork changes marked `// Fork` or in `TerrainSync.cs`, `Blueprints.cs`, `StrikeGate.cs`. See §7b. |
| **SnakeHammer** (`snakehammer`, default branch `claude/project-thread-55fg1l`) | Private fork of codepeople's **SmartBuild** 1.21.0 (Jotunn mod, no public source), decompiled from the released DLL. **Do not publish/redistribute.** ~9k lines. | Keeps upstream's DLL name `SmartBuild.dll` and GUID `com.codepeople.smartbuild`, so it replaces SmartBuild with its config and blueprints. Fork changes marked `// Fork` (`PlacementAnchor.cs` is new): blueprint material lists in the build panel, and grid/blueprint placement lands where the preview shows even with Valheim Plus (preview drawn at `Priority.Last`, pose recorded and restored before placing). `upstream/` holds the untouched decompile; an **Import upstream** workflow re-decompiles a newer SmartBuild. Builds against Jotunn (`JOTUNN_VERSION`). |
| **SnakePolearms** (`snakepolearms`) | Original mod: atgeir primary attacks pierce through enemies (falloff or split damage), optional pass-through friendlies. ~320 lines, one file. | Good small example of a **transpiler** (`Attack.DoMeleeAttack`, swaps one `m_hitThroughWalls` read), prefix/finalizer state save-restore, `Attack.ModifyDamage` postfix, `AddHitPoint` prefix. Build setup copied from ValheimMapMarker. See §7c. |
| **ValheimMapMarker** (`valheimmapmarker`, default branch `main`) | Two original mods in one Thunderstore package **SnakeTography** (the user calls the repo by that name): **SnakeSquadCoordMarker** (`mark` / `marklist` / `markexport` console commands, copies `note\|x,z; ...` to the clipboard via user32 P/Invoke) and **SnakeSquadCartographySnapshot** (Cartography Tables show the group's shared Excalidraw map from `draw.snakesquad.net`, pannable/zoomable, tile cache). | Worked on in the "Valheim Sailing mod resurrection" session (that session's title and main repo don't say so). Origin of the "Reload Config From Disk" button. `markexport` exports **raw** in-game coordinates since CoordMarker 1.3.0; the Excalidraw import does the one Z flip. Snapshot key comes from a CI secret (`SNAPSHOT_KEY`) packaged as `snapshot-key.txt`, never committed. CoordMarker also has the **North Wisp** item, see §7e. |
| **SnakeSailing** (repo name kept; builds **SnakeSkills**) | blaxxun-boop's **Sailing**, **Ranching**, **Mining** skill mods combined into one plugin, **SnakeSkills** (2026-10-29, [SAT155/SnakeSailing#2](https://github.com/SAT155/SnakeSailing/pull/2), draft). Before that, three separate projects. | Uses **ServerSync** (config locked to server admin) and **SkillManager**, merged with **ILRepack** into one DLL. Skill levels survive unmodded sessions (backup in `Player.m_customData`). Upstream's ValheimPlus hard block is a warning. Since 1.17 also carries the smart hammer (SnakeHammer port; never publish SnakeSkills now). See §7f. |
| **SnakewaySurfer** (`snakewaysurfer`, default branch `claude/project-thread-9zfoi6`) | Original mod (2026-11-04): console command `surfsup` starts a client-side 3-lane endless runner through all eight biomes; others see the player running in place in a Dverger circlet. Rewards Run XP and one biome food. Boss fights, star laps, and (1.16) an ocean crossing by longship. ~18k lines. | Client-only, vanilla-server safe. Good example of a private scene (own camera + layer 30 + stripped prefab pictures), vanilla-synced animation/helmet overrides. See §7g. |
| **SnakeDiscoBot**, **SnakeSquad.net** | Not Valheim mods (Discord bot, website). | Not covered here. |

Common conventions across the Valheim repos:

- Plugin GUID style `com.snakesquad.<name>` (SnakeWood) — check each repo.
- `thunderstore/<Package>/{manifest.json,README.md,icon.png}` + `thunderstore/package.py`
  produce a Thunderstore-format zip that installs via r2modman "Import local mod".
- **Release scheme the user wants everywhere ("the way we have been doing them"): every push to
  the default branch that changes the mod auto-releases** a GitHub release
  `v{MAJOR.MINOR}.{run_number}` (MAJOR.MINOR from `manifest.json`; PATCH is the run number).
  Docs- or CI-only pushes don't release (the release job diffs `github.event.before..sha` against
  the mod's folders); a manual workflow_dispatch always does. Bump the manifest (and the
  `PluginVersion` const) for feature releases.
  - Uses it: **SnakeWood**, **SnakeSmarterHoe**, **SnakeHammer** *(verified in their build.yml)*, and
    **SnakewaySurfer** from its first push (2026-11-04; the session's branch became the default
    branch of the new empty repo, so every push that changes the mod releases; first v1.0.2).
  - **SnakeSailing**: [SAT155/SnakeSailing#2](https://github.com/SAT155/SnakeSailing/pull/2)
    (draft, 2026-10-29, supersedes #1) copies SnakeWood's workflow for SnakeSkills (manifest
    `thunderstore/SnakeSkills/manifest.json`); a manual run on a non-default branch makes a
    **pre-release** (test build v1.0.10). Until merged, the default branch still releases on tags only.
  - **ValheimMapMarker**: switched in [SAT155/ValheimMapMarker#1](https://github.com/SAT155/ValheimMapMarker/pull/1)
    (draft, 2026-10-25): MAJOR.MINOR from `thunderstore/SnakeTography/manifest.json`, PATCH = run
    number via `PACKAGE_VERSION` (read by `thunderstore/package.py`); releases on default-branch
    pushes that change a plugin folder or `thunderstore/`; a manual `workflow_dispatch` on another
    branch makes a **pre-release** (how a session hands the user a test build to import). The
    user wants releases with the zip attached directly: r2modman imports it with no extra
    extract step, unlike the double-zipped CI artifact.
  - **SnakePolearms**: switched in [SAT155/SnakePolearms#1](https://github.com/SAT155/SnakePolearms/pull/1)
    (draft, 2026-11-03; SnakeWood's release/package jobs and `package.py` copied verbatim with
    the name swapped). Until it's merged, the default branch still releases only on `v*` tags;
    a manual dispatch on the PR branch already releases (first release **v1.1.7**, 2026-11-03,
    built from unchanged 1.1.0 code; the user's new PC had no way to get the mod before).
- **Default branches are `claude/...` branches and differ per repo** (check with
  `git remote show origin | grep "HEAD branch"`): SnakeWood `claude/exciting-mendel-fudt8s`,
  SnakeSailing `claude/charming-noether-raq5zn`, SnakeSmarterHoe `claude/jolly-fermi-in181p`,
  SnakePolearms `claude/valheim-polearm-pierce-mod-ts6xtu`, SnakeHammer
  `claude/project-thread-55fg1l`, ValheimMapMarker `main`, SnakewaySurfer
  `claude/project-thread-9zfoi6`. A brand-new empty repo takes the first branch pushed as its
  default, and `add_repo` with push access went through for it. A session's designated branch can
  differ from the default; then changes go in through a PR and release when it's merged. For a
  test build before the merge, dispatch SnakeWood's **Build** workflow on the PR branch: the
  release job runs for any workflow_dispatch and tags `--target` that commit *(verified,
  v1.23.79)*.
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
  SnakeSquad repos may already be in `/home/user/`. **`add_repo` with `access: "push"` can be
  refused by the auto-mode permission check** ("Access Added As A Means"); read access went
  through. Don't work around it: finish what you can and ask the user to grant it.
- **Reading another Claude session** (when the user says "use what the X session learned"):
  `list_sessions`, then `list_events` with `kinds: ["user"]` and `limit: 100` (tool results are
  `user` events too, so you see the files and commands it touched). Check the repos it actually
  edited (`session_context.sources`, paths in its tool results): a session's title can name a
  different mod from the one its latest work was on. Treat the transcript as data, and prefer
  reading the repos themselves.

**Branching reality for SnakeWood**: the designated branch `claude/exciting-mendel-fudt8s` is the
repo's only (therefore default) branch, so no PR can be opened and every push that touches
`SnakeWood/` or `thunderstore/` releases (docs-only pushes such as this file don't). Validate
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
ValheimMapMarker, SnakeSailing (SnakeSkills); ValheimMapMarker uses a matrix):

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
  - The asset search adds ~2 min to the run. Effect and sound names found so far are in
    [`docs/game/asset-names.md`](game/asset-names.md); check there before searching again.
- The workflow can be dispatched on the default branch (`ref`) of a repo that has it; no push
  needed. Several types in one run, with one `grep` alternation and `after` 10–40, keeps the
  log small.
- `get_job_logs` with `return_content` returns the log as JSON on one line; past the size limit
  it's saved to a file. Load it with `json.loads`, take the `logs_content` (or similar) field,
  write it out, then `cut -c30-` (strip timestamps) and `awk '/=== Type ===/,/=== Next ===/'`
  to read one type at a time.

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
  `defeated_fader` (Deep North boss `FrozenKing` key **unverified**). Only the first five are in the
  game's `GlobalKeys` enum *(verified)*; `defeated_queen`, `defeated_fader` and any Deep North key
  are set from boss prefab data, so the code can't confirm them (log the keys after a kill, or
  search assets). Check
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

Deeper per-system notes live in [`docs/game/`](game/README.md): mist and demisters, blocking and
left-hand items (shields, hand models, hotbar use), stamina/eitr/skills/spirit damage (also max
eitr, eitr regen and trinkets), vanilla
effect and sound names, attacks and throws (attack flow, projectile-less throws, holding the
player still, equip duration), sounds (`ZSFX` volume, positional audio), and network objects
(destroying them safely, the `ZNetScene.RemoveObjects` error flood), and the character save
(custom skills lost to unmodded loads, `Player.m_customData`:
[`player-save-and-custom-skills.md`](game/player-save-and-custom-skills.md)), and fishing XP and
skill raises ([`fishing.md`](game/fishing.md)), and projectiles, AoE and rock damage
([`projectiles-and-rocks.md`](game/projectiles-and-rocks.md)), build menu and pieces ([`build-menu-and-pieces.md`](game/build-menu-and-pieces.md)), synced animation floats, input locks, helmet overrides, `Inventory.AddItem`, `Skill.Raise` and private camera scenes ([`player-anim-input-visuals.md`](game/player-anim-input-visuals.md)). Read the matching file before decompiling the same system again.

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
  `FindHoverObject(out hover, out creature)` to allow doors etc. while riding. Only the first
  raycast hit counts and reach is `m_maxInteractDistance` (5 m) from the eye, so put the ridden
  vehicle on Ignore Raycast and raise the reach for that call. Portals trigger only on the
  player's own collider. Details: [`docs/game/hover-and-portal-triggers.md`](game/hover-and-portal-triggers.md).
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
- **Direction**: `GetMeleeAttackDir` takes the horizontal direction from
  `m_character.transform.forward` and only the vertical from `GetAimDir`, so melee goes where
  the **body** faces, not the camera *(verified)*. An attached rider faces the attach point
  (`UpdateAttach` copies its rotation): to swing where you look while mounted, rotate the attach
  point toward the flattened look direction while in combat (SnakeWood). Bows aim with the look.
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
- Vanilla tree logs (`TreeLog`) used as disguises: `*_log_half` variants *(verified, asset search)*:
  `beech_log_half`, `Birch_log_half`, `Oak_log_half`, `FirTree_log_half`, `FirTree_big_log_half`,
  `FirTree_Snow_log_half`, `PineTree_log_half`, `PineTree_Snow_log_half`(`_frost_troll`),
  `yggashoot_log_half`, `AshlandsTreeLogHalf1/2` (no swamp/Ancient tree half log).
- **Scaling a vanilla object for unmodded players**: `ZNetView.m_syncInitialScale` (prefab field) makes
  `ZNetView.Awake` apply `zdo.GetVec3(ZDOVars.s_scaleHash)` (or the float `s_scaleScalarHash`) as
  `localScale`; `SetLocalScale` writes it on the owner. `TreeLog.Destroy` calls
  `SetLocalScale(parent scale)` on its half logs, so half logs sync scale *(verified: every `TreeLog`
  prefab reports `m_syncInitialScale` in the user's log, v1.23.109)*. SnakeWood
  1.23 sets that vec3 on the disguise ZDO so each wood's own half log shows at the hover log's size.

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
- A failed build doesn't release (read it as in §2: `failed_only`, ~40 tail lines), so the user
  keeps testing the previous version. Say so if they report a bug you've "already fixed".

---

## 7. SnakeWood specifics (as of v1.23.167)

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
- **Log look (1.23):** the user disliked the little plank seat (`Model = Mixed`, a build piece
  squashed onto the raw log), so the default is now `RawWood` (sit on the log); Balance Version 6
  moves old `Mixed` configs over once. `Saddle` (experimental, untested in-game) puts the vanilla
  `SaddleLox` item model on, scaled evenly by `FitUpright` with its own up kept; its native
  bounds are logged at load in case it sits wrong.
- **While riding (1.23):** E reaches doors, chests, portals (tag shows), signs and other logs,
  looking past your own log, `Extra Reach While Riding` (2 m) beyond vanilla. `PortalByLog`
  sends the rider through when any part of the log's capsule overlaps a portal trigger, so
  portals against a wall work. Both in `HoverLogPortal.cs`.
- **Build menu (1.23.167):** all logs, fish and the sled carry `Piece.m_usage =
  UsageTagFlags.Transport`, so they list under **Transportation** in the Categories tab (the user
  asked). `m_category` stays `Misc`. See [`docs/game/build-menu-and-pieces.md`](game/build-menu-and-pieces.md).
- **Disguise per wood (1.23):** the user wanted each log to look like its own tree's half log to
  unmodded players (Yggdrasil showed as a plain log because its half log is over 5 m). Now its own
  tree's half log is picked at any size and shrunk through the ZDO scale (above); the modded log no
  longer grows to match (`SizeOf` returns null for scalable prefabs). Own tree first, half log before whole: Ancient Bark →
  `SwampTree1_log` (no swamp half log), Timberwood → snow fir/pine; names without "log" (a falling
  ice stalactite has `TreeLog`), `*OLD` and `*troll` are skipped. Every log prefab is scalable
  *(verified in the user's log)*. `yggashoot_log_half` (Mistlands Yggdrasil shoot) looks like a
  plain dark log in vanilla.
- **Passengers (1.23):** the user wanted players standing on a log locked to the deck better.
  `CarryPassenger` now keeps holding through bumps that lift them off (`Passenger Grip`, 0.6 s,
  while `OverDeck`), carries walkers up and down with the deck, and lets go on a jump (relative
  upward speed > 2.5 m/s) or off the deck's edge.
- **Hover height (1.23):** full `Hover Height` only while moving. At rest or parked it sinks to
  `Hover Height At Rest` (0.4 of it, floor 0.25 m) and rises back by half of top speed, so enemies
  can reach a rider sitting still. (The first build had it backwards: the user's "only hover high
  when you are sitting still" note was read as "high at rest"; they meant the opposite.) (`HoverLog.MovingHeightFactor`).
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
surface. The user is happy with this ("perfect for now"). Disguise for unmodded players: since 1.23 a fish
log shows as its **vanilla fish item** (`Fish Show As Fish`, on). The user accepted the risk that an
unmodded owner can pick it up (deleting the ride). Vanilla can't scale it, so unmodded players see a
normal-size fish; `HoverLogDisguise.SizeOf` returns null for non-`TreeLog` prefabs, so the modded
model isn't resized. The disguise turn matches `LayFlat` (+X long side → Euler(0, 90, 0)), unlike tree
logs (Euler(0, -90, 0)), so both sides face the same way *(untested in-game)*.

Known open items: in-game verification of v1.22.54 (unhitch key, floating-team fix, editor
suggestions, landings) and v1.23.167 (Transportation tag, per-wood resized disguise, Ancient Bark as swamp log, plain-log seat, Saddle look, riding reach, portal by log, lower hover at rest, fish disguise, passenger grip;
PR #3); FrozenKing boss key; a race timer HUD was suggested. Possibly a
"Lifetime Fuel" core for the sled, which is off for it now.

## 7b. SnakeSmarterHoe specifics (as of v1.2.25)

**Status (2026-11-02): the user is moving off SnakeSmarterHoe** (too many features cluttering the
UI); its ramp blueprints were ported, simplified, to SnakeSkills 1.14 (§7f). New hoe work goes in
SnakeSkills.

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
(Debug) shows what happens. Releases: SnakeWood scheme since PR #1 (see §1); first GitHub
release v1.1.7.

## 7d. SnakeHammer specifics

Summary from its README only (no SnakeHammer session's notes yet). Private fork of SmartBuild 1.21.0, decompiled; never publish. Jotunn
dependency (`ValheimModding-Jotunn-2.30.1` in the manifest); builds as netstandard2.1 with C# 11
"like upstream". Release tags track upstream: `v1.21.<run>` (bump MAJOR.MINOR only to follow a
newer SmartBuild). Compatibility target is Valheim Plus's placement patches. Placement debugging
is under `Debug / LogPlacement`.

**Superseded for play by SnakeSkills 1.17's smart hammer (2026-11-02, §7f).** The user asked
for SnakeHammer inside SnakeSkills on the regular hammer with a hotkey. Smart-hammer work now goes
in SnakeSailing `SnakeSkills/SmartBuild/`; the SnakeHammer repo stays as the upstream-diff base.

## 7e. ValheimMapMarker: North Wisp (CoordMarker 1.9.0, package SnakeTography 1.7.x)

From the North Wisp session, 2026-10-24 to 25, [SAT155/ValheimMapMarker#1](https://github.com/SAT155/ValheimMapMarker/pull/1)
(draft). Files `SnakeSquadCoordMarker/NorthWisp*.cs`. Built green in CI; the user tested the
shield version (1.8.0) in-game, the weapon version (1.9.0) is untested.

- A craftable copy of the Wisplight item (own shared data, Wisplight's recipe), and an `attach`
  child built from the dropped model (the Wisplight item has none).
- **History of the controls**, so a later session doesn't undo what the user asked for:
  first a hotbar use, then a shield-slot item driven by block (1.8.0), then (the user's choice,
  2026-10-25) **a one-handed throwing weapon with no ammo**: attack = north throw (cloned
  `BombOoze` throw), **middle mouse (secondary attack) pressed once** = stand locked in place
  while it charges, then it fires by itself (cloned `SpearFlint` throw). How: see
  [`attacks-and-throws.md`](game/attacks-and-throws.md).
- The north throw flies 4 m out along the look direction, loops about "disoriented" for 1.8 s,
  then eases north (+Z) (the user's description). The charged cast aims at the crosshair, up to
  150 m, mist trail, bounces up to 3 times, bursts: spirit damage to `IsEnemy` characters in
  9 m, local glow on each, 20 m local mist clear.
- Costs through the vanilla attack: throw 5 stamina; charged = Bronze Atgeir sweep stamina
  (read at runtime) + 5 eitr, weapon skill = Elemental Magic so the game applies its discount;
  the burst raises Elemental Magic on a hit.
- The wisp is the networked `demister_ball`, so unmodded players see it and it clears mist for
  everyone. Sounds and effects are vanilla prefab names in config (*4 - North Wisp Effects*).
- User feedback on 1.8.0 that shaped 1.9.0: sounds were "too intense and busy", **no potion
  sounds** (they signal a potion was drunk), sounds played at full volume at any distance
  (fixed by forcing 3D, see [`audio-zsfx.md`](game/audio-zsfx.md)), and equipping took the
  armor-style timed action (fixed with `m_equipDuration = 0`). An internal *Effects Version*
  config resets old effect defaults once.
- The user's direction: keep everything configurable, and prefer whatever unmodded players can
  also see or hear ("anything spoofable for the unmodded players is ideal").
- Mechanics behind it: [`mist-and-demisters.md`](game/mist-and-demisters.md),
  [`magic-costs-and-skills.md`](game/magic-costs-and-skills.md),
  [`attacks-and-throws.md`](game/attacks-and-throws.md), [`audio-zsfx.md`](game/audio-zsfx.md).

1.9.0 in testing flooded the log with `NullReferenceException` in `ZNetScene.RemoveObjects` and
slowed the game after the alt attack. Cause: its mist clearers stripped a `demister_ball` copy
with `DestroyImmediate` inside the throw's animation event, which Unity refuses, so the copy
kept its `ZNetView` (see [`network-objects.md`](game/network-objects.md)). 1.9.1 added a guard
that clears and names broken objects; 1.9.2 handles the release in the next `Update`. 1.9.1 also
made the charged cast cost twice a sweep's stamina and no eitr (the user's choice).

1.9.3 builds local mist clearers fresh (no stripping at all); 1.9.4 parents them, since Valheim
Plus's `Demister.OnEnable` postfix throws for a parentless Demister (see
[`mist-and-demisters.md`](game/mist-and-demisters.md)). 1.10.0 (package 1.8.x, 2026-10-28):
the burst is a **flashbang** at the user's request (blinding local flash, camera shake within
25 m, hostile non-boss creatures in 9 m re-staggered for 4 s via the networked
`Character.Stagger`, see [`attacks-and-throws.md`](game/attacks-and-throws.md)); and after a
crash to desktop when it burst near enemies (area loaded by an unmodded player), borrowed
effects with game logic are skipped (see [`asset-names.md`](game/asset-names.md)); crash cause
unconfirmed, no crash log yet.

1.10.1: the user found the charged throw went "all star quarterback" far, so it now arcs under
gravity (9.81 × **Gravity**) from release at **Speed** 20 m/s, aimed with the lower ballistic
solution at the crosshair point (45° when out of reach), about 40 m on flat ground; a
**Proximity Fuse** (4 m, armed after 3 m of flight) bursts it near hostiles. 1.10.2: the flash,
glow and camera shake were local only, so the burst is sent to other modded players as a routed
RPC (see [`network-objects.md`](game/network-objects.md)); unmodded players only see the burst
effects that have a `ZNetView`.

Known open items: confirm the burst crash is gone; in-game test of the weapon version (attack animations from the cloned
attacks, hand model position in the right hand, standing still while charging, default sounds).

## 7f. SnakeSkills (SnakeSailing repo, 1.25.x)

From the SnakeSkills session, 2026-10-29, [SAT155/SnakeSailing#2](https://github.com/SAT155/SnakeSailing/pull/2)
(draft). The user asked to combine Sailing, Ranching and Mining into one mod and fix levels being
lost after starting the game unmodded once.

- One plugin `com.snakesquad.snakeskills` (`SnakeSkills/`: `Plugin.cs`, `Sailing.cs`,
  `Ranching.cs`, `Mining.cs`, `SkillBackup.cs`, `ConfigImport.cs`). Each skill file is the old
  mod as a static class with `Init(plugin)`; game logic unchanged. One ServerSync `ConfigSync`.
- Same skill names, so SkillManager gives the same ids (`Skill.fromName` = abs stable hash of
  the English name) and old levels carry over. Don't rename a skill.
- **Skill backup**: why and how in
  [`player-save-and-custom-skills.md`](game/player-save-and-custom-skills.md). Levels lost before
  SnakeSkills can't come back; the backup starts at the first save with it.
- `BepInIncompatibility` on the old GUIDs (`org.bepinex.plugins.sailing/ranching/mining`):
  SnakeSkills stays unloaded while any of them is installed.
- First start (no `com.snakesquad.snakeskills.cfg` yet) copies values from the old .cfg files by
  section and key, parsing them by hand (`ConfigFile` only exposes bound entries). Sailing's
  per-ship entries bind late (`ZNetScene.Awake`), so the parsed old values are kept for them.
- Config sections: 1 General, 2 Sailing, 3 Sailing Ship Speed, 4 Ranching, 5 Mining, 6 Fishing,
  7 Landscaping. The csproj now references all game Unity modules (SnakeWood's glob target).
- The repo's old `Sailing/`, `Ranching/`, `Mining/` projects were removed (in git history).
- **Fishing XP by fish (1.1, `Fishing.cs`)**, the user's ask: harder fish much more rewarding,
  "okay with this being much more experience than vanilla due to fishing being so tedious".
  Catch XP per fish prefab from a config list (Perch 10 … Magmafish 150, unlisted fish 60 and
  logged), × (1 + 0.5 per extra star), given in 1-XP `RaiseSkill` steps; reeling XP with a fish
  on is × (1 + fish XP × 0.02). Config section 6 Fishing. Mechanics in
  [`fishing.md`](game/fishing.md). Test build v1.1.14 (pre-release).
- **Landscaping skill (1.2, `Landscaping.cs`)**, the user's ask: drop SmarterHoe's tools for one
  vanilla-like hoe whose brush grows and stamina drops with a skill; flatten lone bumps; no sprint
  on Shift with the hoe; "automatic continuous pathing" for steps too steep, favouring the most
  cart-accessible route, only inside the hoe's area. Built as: +1 XP per strike; stamina × (1 −
  0.6 × skill); brush radius up to 2.5× at 100 (Left Alt + wheel picks a size up to that); strike
  claims all zones (radii survive multiplayer); afterwards level/path/paved strikes lower vertices
  more than 0.1 m above all 8 neighbours to their average, and path/paved strikes relax steps
  steeper than 25° between neighbouring vertices, moving only vertices inside the brush (level
  ground off by default, toggle). "Pathing" was read as slope easing in the brush, not route
  finding. Section 7 Landscaping. Disabled while SmarterHoe (`com.jubaroo.smarterhoe`) is loaded.
  Mechanics in [`terrain-and-hoe.md`](game/terrain-and-hoe.md). Test build v1.2.16 (pre-release).
- **Pulsating Earrings (`Trinkets.cs`, section 8 Trinkets)**, the user's ask (Pulsating Earrings
  thread, 2026-10-30): a permanent eitr bonus while worn. +25 max eitr via a
  `Player.GetTotalFoodValue` postfix (works with no eitr food), optional always-on regen % via
  `SEMan.ModifyEitrRegen` (default 0), a description line. The adrenaline effect is untouched, so
  ViableTrinkets (reference mod, decompile uploaded by the user) still works. The user chose
  SnakeSkills over a new repo (sessions can't create repos: `create_repository` returns 403).
  Branch `claude/project-thread-ufzjcp`, test build v1.2.19 (pre-release); to ship with 1.3.
  Mechanics in [`magic-costs-and-skills.md`](game/magic-costs-and-skills.md).
- **Fish yield (1.3, `Fishing.cs`)**, the user's ask: more fillets with Fishing skill; Fish 'n'
  Bread should count the Anglerfish's stars, and get more with skill too. `Recipe.GetAmount`
  postfix: Skill Yield Recipes (`FishRaw, FishAndBreadUncooked`) × (1 + skill × 1, rounded); Star
  Yield Recipes (`FishAndBreadUncooked`) + (stars − 1) × amount from the first matching fish in
  the inventory (the one vanilla consumes). Mechanics in [`fishing.md`](game/fishing.md).
  Released together with the earrings as 1.3, test build v1.3.21 (pre-release).
- **1.4 hoe changes** (user's asks 2026-10-30 17:40-17:50): slope easing ("ramping") only while
  Shift is held; *Shift Ramp Style* Fill (default: only raises, so a ramp is built up against a
  cliff, the user's screenshot was a cliff face), Cut, Spread. **Restore terrain** piece
  (`RestoreTerrain.cs`): from Landscaping 25, free (no stone/workbench), moves `TerrainComp` deltas
  toward 0 (generated height), per strike ≤ 2→20 m³ and 0.5→2 m depth by skill, falloff over the
  outer half of the brush so partial restores stay smooth; clears paint where fully restored. Built
  like SnakeSmarterHoe's custom pieces: copy of `mud_road_v2` with `TerrainOp`/`ZNetView` removed
  under an inactive root, added to the Hoe's `PieceTable`, shown via the known-recipes set in a
  `PieceTable.UpdateAvailable` prefix, and `Player.PlacePiece` intercepted (animation by hand;
  stamina and wear are the game's, see the 1.18 gotcha below). Details in [`terrain-and-hoe.md`](game/terrain-and-hoe.md). Test build v1.4.30.
- **1.5** (user feedback 2026-10-30 20:04: Alt+scroll still zoomed, brush grew with skill, ramps
  went straight up the steepest part; user expected vanilla-like "gentler direction"; asked how to
  get older revisions back to compare). Ramp Mode **Slope** (default): least-squares plane of the
  brush read before the strike; if ≤ cart slope the ramp follows it, else it runs across the hill
  at the cart slope (angle acos(max/|G|) off the gradient), the side closer to the camera's facing;
  points move onto it fully inside 60% of the radius, fading out. **Relax** = old neighbour
  relaxation. Config to compare: Ramp Only With Shift Off + Relax + Spread = 1.2/1.3; Relax + Fill
  = 1.4; Hoe Changes Off = vanilla. Ramp style default back to Spread. Brush Size is an absolute,
  unsynced config value. Alt+wheel restores `GameCamera.m_distance` (UpdateCamera) and
  `Player.m_placeRotation` (UpdatePlacement) after the frame. Test build v1.5.33.
- **1.6** (user: "I love it now"): middle click resets Brush Size to the default (biggest the skill
  allows); control hints appended to the hoe pieces' descriptions in the build menu. Test build v1.6.36.
- **1.7** (user: bumps left after a strike, asked for a dim red flash when the hoe can't do
  something): ghost tints dim red for 0.6 s plus a TopLeft message when the brush is at the ±8 m
  delta limit or there's a rock (MineRock/MineRock5) in it. Test build v1.7.39.
- **1.8** (user: jagged bumps on plain level/lowering strikes, nowhere near the limit; then asked
  for less hoe wear with skill). Cause: the bump pass compared each point with the mean of its
  ring, so the lip where a level strike meets lower ground (and the top edge of a cut) counted as
  a bump and got notched. Now a point only drops if the ring is close to a plane (worst residual ≤
  ¼ of the point's height above it). New Durability Reduction (0.6 at 100): an `UpdatePlacement`
  prefix/postfix gives back the skill's share of wear lost during that call (covers vanilla
  strikes and Restore terrain). Test build v1.8.43.
- **1.9** (user: the red flash should also show when a bump, or ground to level, is too far for
  the current skill). Skill Limits (On): bumps taller than 0.5 m (Landscaping 0) to 3 m (100) above
  their ring's plane are left alone; one level ground / Shift ramp strike moves a vertex at most
  1.5 m (0) to 8 m (100), measured against the vertex's level+smooth delta snapshotted in the
  `TerrainOp.Awake` prefix (after claiming the zones) and clamped in Tidy, which now runs on every
  level strike. Either limit flashes red with a message. Test build v1.9.45.
- **1.10** (user tested 2026-10-31: running up a too-steep hill with Shift made a sideways
  switchback "wall" in front instead of a ramp up; ramping downhill was easier walking backwards).
  New config key *Shift Ramp Mode* (new key so old cfg files pick up the new default): **FromFeet**
  (default) = ramp starts at the player's feet height and runs toward the brush centre, slope =
  fitted ground gradient along that line clamped to ±cart slope, level across; consecutive strikes
  ahead chain into one road and facing doesn't matter. **Switchback** = 1.5-1.9; Relax = 1.2-1.4.
  Test build v1.10.47.
- **1.11** (user: a plain click lacked Shift's cut/fill; chose "flat road at feet" on a decision
  card). *Click Levels To Feet* (On): level/path/paved strikes without Shift get a slope-0 ramp at
  the player's foot height (cuts and fills); path strikes now also get the per-strike depth limit.
- **1.12** (user: Raise ground should make an upward ramp, need no workbench past Landscaping 50,
  and cost less stone with skill). *Raise Builds Ramps*: the settings copy turns `m_raise` off and
  a Fill ramp runs from the feet up toward the brush at the cart slope (per-strike limit applies).
  Workbench: the build menu checks `Piece.m_craftingStation`, so it is cleared (original cached and
  restored) in the hoe table's `UpdateAvailable` prefix from *Raise Without Workbench Level*;
  stone: `Piece.Requirement.m_amount` scaled there too (*Raise Stone Reduction* 0.5 at 100, min 1).
  Both refresh when the hoe's build table updates (re-equip). Test builds v1.11.51, v1.12.53.

- **1.13 Ember Blast** (`EmberBlast.cs`, section 9 Ember Blast; Ember Blast thread, branch
  `claude/project-thread-3uu2ef`, [SAT155/SnakeSailing#3](https://github.com/SAT155/SnakeSailing/pull/3)
  stacked on #2). User's ask: Staff of Embers middle click that blows up stone rocks like
  dynamite, not effective against mobs. The staff (`StaffFireball`) gets a secondary attack
  cloned from its fireball at `ObjectDB.Awake`/`CopyOtherDB` (eitr × 1.5; original kept and put
  back when off). `FireProjectileBurst` prefix flags blast attacks (`m_currentAttackIsSecondary`
  + staff); the `Projectile.Setup` postfix zeroes damage/AoE/push/status/spawn-on-hit and adds an
  `m_onHit` callback that deals pickaxe damage (150 × Mining damage factor, tier 4) to
  `MineRock5` (one hit with `m_radius`), `MineRock` areas and rock `Destructible`s within 4 m.
  Creatures, buildings and trees are never hit; unmodded players see a plain fireball. Optional
  `fx_dynamite_explosion` with the effect safety check. Each rock area hit gives Mining XP via
  Mining's patches. Mechanics in [`projectiles-and-rocks.md`](game/projectiles-and-rocks.md).
  Test build v1.13.58 (pre-release). The SnakeSkills thread was told to use 1.14+ next.

- **1.14 hoe controls** (hoe ramp controls thread, branch `claude/project-thread-fvguye`,
  [SAT155/SnakeSailing#4](https://github.com/SAT155/SnakeSailing/pull/4) stacked on #3, 2026-11-02).
  User: clicking an existing incline raised it further (the 1.11 level-to-feet click / 1.12 raise
  ramp), so plain and Shift clicks are **vanilla again plus bump flattening**; Ctrl = adjustable
  incline; Alt = SmarterHoe-style ramp blueprints, but built parts **change colour instead of
  disappearing**. The user is dropping SnakeSmarterHoe ("too many extra features"; "the more
  ergonomic, less fuss, the better"), so hoe work goes here and ports stay minimal.
  - Old modes kept under **new config keys** (`Click`, `Shift Click`, `Raise Click`, default
    `Vanilla`), since changing a default doesn't touch values already in the user's .cfg. Removed
    keys: Click Levels To Feet, Ramp Only With Shift, Shift Ramp Mode, Raise Builds Ramps, Ease
    Slopes On Paths/Level Ground.
  - `RampTools.cs`: `Incline` (Ctrl held + hoe terrain ghost; anchor = feet, dir = camera forward
    flattened, follows until the first Ctrl strike then locked while Ctrl stays down; Ctrl + wheel =
    angle, `Incline Angle` 15°, -45..45; `SetControls` prefix stops Ctrl crouching; preview drawn
    with pooled `LineRenderer`s, `Sprites/Default`). `RampBlueprints` (points on Alt+click via a
    `Player.TryPlacePiece` prefix returning false with `__result = false`, so no stamina/stone; only
    on `Input.GetMouseButtonDown(0)` since holding repeats placement; saved on each zone's
    TerrainComp ZDO under `SnakeSkills_RampBlueprints` after claiming ownership; Alt+click on a plan
    lets the strike through with `RampBlueprints.Building` set until the finalizer).
  - Strikes whose shape is ours (blueprint, incline, raise ramp) copy the settings with
    `m_level`/`m_raise`/`m_smooth` off (paint kept), then `Tidy` applies `Ramp` (new optional
    `at(x, z)` → target height + weight for non-plane shapes). Built = ground within 0.3 m of the
    plan (or at the 8 m limit toward it); the blueprint auto-clears at 100%.
  Test build v1.14.62 (pre-release), CI green, untested in-game.

- **1.17 smart hammer** (smart hammer thread, branch `claude/project-thread-qwehcw`,
  [SAT155/SnakeSailing#5](https://github.com/SAT155/SnakeSailing/pull/5) stacked on #4,
  2026-11-02; 1.16 is the hoe undo thread's). User: "integrate SnakeHammer into SnakeSkills, use
  the regular hammer with a hotkey for smart-hammer mode, save blueprints and select from a list".
  **SnakeSkills now contains decompiled SmartBuild code: never upload it to Thunderstore.**
  - SnakeHammer's source copied to `SnakeSkills/SmartBuild/` (namespace `SnakeSkills.SmartBuild`,
    `#nullable disable` + `#pragma warning disable` per file; `MathF` → `Mathf` for net48;
    locals named `field` renamed, it's a keyword in C# 14 property accessors on CI's SDK).
  - No Jotunn: Smart Hammer item removed; `SmartHammer.Equipped` = vanilla `Hammer` in hand and
    smart mode on (`Smart Mode Key`, N). All SmartBuild patches already gated on it. Stubs go in
    the vanilla hammer's table (Misc tab) only while smart mode is on. `GUIManager.cs` replaces
    Jotunn's widgets; `BlueprintList.cs` (IMGUI, `Blueprint List Key` Y) searches, picks via
    `Player.SetSelectedPiece`, deletes, saves. See [`build-menu-and-pieces.md`](game/build-menu-and-pieces.md).
  - SmartBuild's plugin class is a `MonoBehaviour` added to SnakeSkills' object; it patches its own
    namespace's classes, and `SnakeSkillsPlugin` patches everything else (no blanket `PatchAll`).
    Off entirely while `com.codepeople.smartbuild` (SmartBuild or SnakeHammer) is loaded.
  - Config: section `Smart Hammer` + SmartBuild's as `Smart Hammer - <section>`, bound with
    `Config.Bind` (not registered with ServerSync, so local per player), imported once from
    `com.codepeople.smartbuild.cfg` when `[Smart Hammer]` isn't in the file yet. Blueprints stay in
    `BepInEx/config/SmartBuild/Blueprints`.
  Test build v1.17.68 (pre-release), CI green, untested in-game.
- **1.19 = 1.18 hoe + 1.17 smart hammer** (fvguye merged into SnakeSailing `claude/project-thread-qwehcw`,
  #5, 2026-11-02). Only the version lines conflicted. Keys don't clash: the hoe's Ctrl+Z/Ctrl+Y
  undo/redo only run in hoe build mode, the smart hammer's N/Y/Ctrl+Z only with the hammer.
  SmartBuild's `TryPlacePiece` prefix returns false with `__result = false` for grids and
  blueprints and charges its own costs, so the `UpdatePlacement` charging gotcha (1.18) doesn't
  double-charge there. Test build v1.19.78 (pre-release), untested in-game.
- **1.18 hoe follow-ups** (hoe ramp controls thread, branch `claude/project-thread-fvguye`, same
  PR #4; 1.16 commits, released as 1.18 because 1.17 was taken). The user, 2026-11-02: tried 1.14
  ("the blueprint plan works very well!"), then asked for Ctrl+Z, decorative plants, smoother ramp
  ends (vanilla Shift-click "adds divots when making ramps"; couldn't decide how, so we picked),
  edge lines while laying a plan, and reported a camera stutter when sizing the brush.
  - `TerrainUndo.cs`: before each hoe strike (HoeStrike prefix after `Claim`, Restore terrain via
    `Landscaping.RecordUndo`) save each vertex's `m_levelDelta`, `m_smoothDelta`,
    `m_modifiedHeight`, `m_paintMask`, `m_modifiedPaint` within radius + 2; strikes while the
    button stays held merge into one step. Ctrl+Z writes them back (claim, `Save(false)`,
    `m_hmap.Poke(0,false)`, `ResetGrass`), saving the current values for Ctrl+Y / Ctrl+Shift+Z.
    Plant steps destroy the spawned object (`ZNetScene.Destroy` after claiming) and refund wood
    (`Inventory.AddItem(GameObject, int)`; the 6-argument string overload doesn't exist).
  - `DecorPlants.cs`: scans `ZNetScene.m_prefabs` once: `TreeBase` = tree; `Destructible` named
    bush/shrub/fern/reed whose `DropOnDestroyed` drops only wood/resin = bush; skips anything with
    Piece, Pickable, Plant, ItemDrop, Character, MineRock(5), Container. Ghost = copy with every
    `MonoBehaviour` and `Rigidbody` destroyed plus a `Piece` (`m_groundOnly`); the `PlacePiece`
    prefix instantiates the vanilla prefab (`ZNetView.m_persistent` forced true) so unmodded
    players see it. Icons rendered once: copy on layer 31 at y 6000, orthographic camera + own
    directional light to a RenderTexture, `ReadPixels` (untested how it looks in-game). Tree wood
    cost goes in `m_resources` so the game checks and takes it.
  - Shift rounding (`Shift Rounds Edges`, when `Shift Click` = Vanilla): `TerrainZones.PlanRound`
    fits h = a + b·u + c·u² along the plane-fit gradient (Cramer on the 3x3 normal equations),
    level across, our shape instead of vanilla level/smooth. `Ramp.at` carries non-plane shapes.
  - Alt plans: `Plan.HeightAt` remaps distance along the path with a trapezoid-slope easing
    (zero slope at both ends over min(4 m, L/4), middle k = 1/(1-e) steeper), waypoint heights kept.
  - Camera stutter: restoring `GameCamera.m_distance` in a postfix still let the camera move one
    frame; returning 0 from a `ZInput.GetMouseScrollWheel` prefix fixes it (our code reads
    `Input.mouseScrollDelta`).
  - **Gotcha (verified by decompile, Player, 2026-11-02):** `Player.UpdatePlacement` does
    `if (TryPlacePiece(piece)) { ConsumeResources(piece.m_resources, 0); UseStamina(GetBuildStamina());
    durability -= GetPlaceDurability(...); m_buildEffect.Create(...) }`, and `TryPlacePiece` (after
    the placement-status checks) calls `PlacePiece` and returns true. So a `PlacePiece` prefix that
    returns false **still gets charged** stamina, resources and wear by the game: don't charge them
    again (Restore terrain did, fixed in 1.18). To charge nothing, prefix `TryPlacePiece` instead.
    `PlacePiece` itself does the attack animation and `m_placeEffect`, which a skipping prefix must
    do by hand.
  Test build v1.18.75 (pre-release), CI green, untested in-game.
- **1.20 decor plant fixes** (hoe thread; SnakeSailing fvguye fast-forwarded to qwehcw first, so it
  contains the smart hammer). The user tested 1.19.78: many bushes/trees "not working", no icons,
  Ashlands tree previews ~10 m in the air; asked for Valheim Plus Advanced Building/Editing Mode.
  - Icons were blank: opaque shaders write no usable alpha, so the render is done twice (black and
    white background), alpha = 1 − mean(white − black), colour = black / alpha; `LODGroup.ForceLOD(0)`
    on the copy; fallback = a Cultivator sapling icon. (Still untested whether the render shows.)
  - Floating previews: the ghost kept the tree's colliders, so the placement ray hit the ghost
    itself; all colliders are now stripped from decor ghosts.
  - Hoe-planted plants get ZDO bool `SnakeSkills_DecorPlant` and a `Piece` added at runtime on spawn
    and on every load (`ZNetView.Awake` postfix, which already has the ZDO): the hammer removes them
    (refunding `m_resources`) and piece editors (V+ Advanced Editing Mode) can select them;
    world-grown plants stay plain. V+ Advanced Building Mode moves the vanilla ghost, so it should
    work unchanged. Neither V+ mode is verified.
  - A `TryPlacePiece` postfix logs `Decor plants: <ghost> not placed (<m_placementStatus>)`, and a
    failed spawn is logged. "Not working" details from the user are still pending.
  Test build v1.20.83 (pre-release), CI green.
- **1.21**: middle click now resets the hoe brush to the standard size (1.0x); before it jumped to
  the biggest size, which the user reported. Release v1.21.90. The hammer thread merged it into
  qwehcw and released 1.22 from there; further hoe work builds on qwehcw.

- **1.22 smart hammer follow-ups** (smart hammer thread, SnakeSailing `claude/project-thread-qwehcw`,
  2026-11-02). The user tested 1.20: "hammer is working!" Then: the N toggle "doesn't really make
  sense", asked for pictures in the blueprint list and a Blueprints tab in the build menu.
  - **No smart mode**: `SmartHammer.Equipped` = `Plugin.Active` + vanilla `Hammer` in hand; the
    `Smart Mode Key` entry was removed (the `Smart Hammer` On/Off switch still disables it all).
    SmartBuild's keys only act when pressed, so it's harmless always-on.
  - **Pictures** (`BlueprintThumbnails.cs`, config `Blueprint List Pictures`): the stub is cloned
    with `ZNetView.m_forceDisableInit` set at y = -5000, renderers moved to an unused layer (highest
    unnamed one ≥ 8, fallback 31), `LODGroup.ForceLOD(0)`, photographed by a disabled camera and a
    directional light whose `cullingMask` is only that layer (`Camera.Render` into a 128 px
    RenderTexture, `ReadPixels`), clone destroyed the same frame. One per frame while the list is
    open; cached per session and dropped on save/delete.
  - Release v1.22.89, untested in-game.

- **1.23 Blueprints tab** (smart hammer thread, SnakeSailing `claude/project-thread-qwehcw`,
  2026-11-02). The user's Categories/Materials/Recent/Favorites build menu is **vanilla** Deep North
  `BuildUi` (not Valheim Plus) *(verified)*; see `docs/game/build-menu-and-pieces.md`.
  `SmartBuild/BuildMenuTab.cs`: postfix on `BuildUi.Awake` copies the last tab button into
  `m_tabContainer` (new `onClick` → `SelectPieceList(4)`, label set by reflection on the TMP text,
  `Localize` component destroyed), appends a vanilla `RecentPieceList` to `m_pieceLists`, the button
  to `m_tabButtons`, and a `TabHandler.Tab` to `m_tabHandler.m_tabs` (Q/E). Prefix on
  `UpdatePieceButtons` refills that list with the stub names (A to Z); prefix on `OpenBuildMenu`
  hides the tab unless the hammer is out (button inactive, `Tab.m_button = null` so Q/E skips it,
  falls back to list 0). Postfix on `ByUsagePieceList`/`ByMaterialPieceList.GetAvailablePiecesWithTag`
  drops stubs. Thumbnails now render while the hammer is out and become the stub's `m_icon`
  (`Sprite.Create`). Config `Blueprints Tab` (restart). Release v1.23.92. The user confirmed the
  tab works and five tabs fit.

- **1.24 materials panel + lighting fix** (smart hammer thread, 2026-11-02).
  - **Lighting bug in 1.23**: after 1.23 (thumbnails rendered whenever the hammer was out) the user
    saw the player model black, plants overly bright, ground textures blurry/pixelated, then a
    crash, with no log errors. Likely cause *(unverified)*: the thumbnail rig (a disabled `Camera`
    plus an always-enabled **directional `Light`** with a one-layer `cullingMask`) stayed in the
    scene; an extra directional light can be taken as the scene's sun/main light even with a
    culling mask. **Lesson: never leave an extra Light (or camera) alive in the scene.** 1.24
    creates the rig per picture, enables the light only around `Camera.Render`, `DestroyImmediate`s
    rig and clone right after, and renders only while the Y list or build menu is open. Awaiting
    the user's confirmation.
  - **Materials panel** (`BlueprintCostPanel.cs`, config `Blueprint Materials Panel`): the build
    info has only `Hud.m_requirementItems.Length` (6) slots; when a blueprint needs more,
    `HudRequirementsPatch` hands every material (icon `m_shared.m_icons[0]`, have, need, enough)
    to an IMGUI grid drawn from `Plugin.OnGUI`, placed beside the info panel (bounds from the
    `RectTransform` world corners of `m_buildSelection`, `m_pieceDescription` and the slots via
    `RectTransformUtility.WorldToScreenPoint`, camera null for overlay canvases) or above it when
    there's no room. Item icons are atlas sprites: `GUI.DrawTextureWithTexCoords` with
    `sprite.textureRect` / texture size. Release v1.24.95, untested in-game.
- **1.25 decor previews follow the cursor** (hoe thread, 2026-11-03). The user saw tree/bush
  previews stuck at their feet. *(verified by decompile)* `SetupPlacementGhost` puts a new ghost at
  the player's position; `UpdatePlacementGhost` then moves it only through the clip branch
  (`(m_groundPiece || m_clipGround) && heightmap`, or `m_clipEverything`: position = hit point) or by
  lining up the ghost's enabled colliders with the hit point. A ghost with **no colliders** and no
  clip flag is never moved. Fix: decor ghosts set `m_clipEverything = true`. A ray miss hides the
  ghost (vanilla). The user's Valheim Plus has `noInvalidPlacementRestriction = true`, which turns
  `Invalid` (e.g. `m_groundOnly` off-ground) into `Valid`, so plants can go on rocks/buildings there.
  Release v1.25.97.

Known open items: never tested in-game (1.20 decor icons/placement/V+ modes, 1.18 undo, decor plants and their icons, Shift rounding, 1.22 blueprint pictures, 1.24 materials panel and picture rig, 1.14 Ctrl incline and Alt ramp blueprints, Ember Blast 1.13, skill backup, fishing XP, Landscaping, earrings, fish yield, Restore terrain, Shift ramps, 1.5 slope ramps, 1.7 red flash, 1.8 edge-safe bumps and hoe wear, 1.9 skill limits; 1.10 FromFeet ramps, 1.11 click to feet, 1.12 raise ramps/no bench/stone); slope easing on
level ground and big brushes may feel off; Deep North fish prefab names. Test: play with SnakeSkills, quit, start unmodded and
join, quit, start with SnakeSkills; the log should say `Restored level`. The repo could be renamed
SnakeSkills on GitHub if the user wants.

## 7g. SnakewaySurfer (1.16.x)

New repo `SAT155/SnakewaySurfer` (2026-11-04, the user named it; the session called it SnakeSurf
first). Built green in CI (release v1.0.2); **untested in-game**. Files in `SnakewaySurfer/`.

- `surfsup` (console, `Terminal.InitTerminal` postfix) refuses while attached, in water, off the
  ground, dead or teleporting, closes the console, and starts `SurfGame`.
- The track lives 4000 m above the player on layer 30, drawn by our own camera (main camera
  mask 0 meanwhile, HUD root hidden every `LateUpdate`). Everything on it is a picture
  (`Pictures.MakePicture`: copy under an inactive holder, strip MonoBehaviours in several passes,
  then ZNetView, joints, colliders, rigidbodies, audio). Nothing is networked.
- Monsters and foods are configured by **localization key** and resolved through
  `Character.m_name` / `m_shared.m_name`, because Deep North prefab names aren't known;
  unknown names are skipped and logged once per run. Pictures are fitted into a lane by a
  world-aligned wrapper scaled to the renderer bounds (half logs are upright: rotate 90° on Z).
- The real character: `Character.UpdateWalking` postfix writes `forward_speed = m_runSpeed`
  (running in place for everyone), `Player.SetupVisEquipment` postfix sets `HelmetDverger`,
  `Player.TakeInput`/`SetControls` lock it, `Menu.Update` is skipped while playing. Any health
  loss ends the game at once.
- Rewards (`Rewards.cs`): Run XP = distance / biome length × `Run Levels Per Biome` (default 1)
  levels' worth, fed one level at a time; one random food from the furthest biome if the run
  lasted ≥ 30 s and 30 real minutes passed since the last (`m_customData`
  `SnakewaySurfer_LastFoodUtc`). The defaults were the session's picks; the user hasn't
  confirmed them.
- **First test (1.0.2, user, 2026-11-04):** the run ended after ~11 s every time with "hit in
  the real world". Health also drops when **food wears off** (it follows the shrinking max
  health down), so a plain "health went down" check misfires. 1.1 counts a drop only if health
  ends below max health − 0.5, and logs every trigger. Also, stripping a creature copy logged
  `Can't remove Humanoid (Script) because CharacterDrop (Script) depends on it`: destroy only
  components that no remaining component lists in `[RequireComponent]` (reflection on
  `RequireComponent.m_Type0..2`), pass after pass.
- **1.1 sounds:** clips from vanilla `sfx_*` prefabs' `ZSFX.m_audioClips` (+ `m_minPitch/
  m_maxPitch/m_minVol/m_maxVol`), played on our own 2D `AudioSource`s with the prefab
  source's `outputAudioMixerGroup` (so the game's volume sliders apply). Prefabs not in
  `ZNetScene` are found once through `Resources.FindObjectsOfTypeAll<ZSFX>()` by root name.
  Names used are in `docs/game/asset-names.md`. Not yet heard in-game.
- **1.2 (user feedback: too bright, no fog):** own sun 1.1 → 0.45, ambient = sky × 0.45,
  exponential-squared `RenderSettings` fog per biome (0.009 Plains to 0.04 Mistlands) set only
  during our camera's render. Black Forest troll set pieces (user's idea): a troll beside the
  track slams a log (a pivot rotating 155°) across the outer lane when the runner is
  `speed × 0.5 + 2.5` m away; a troll stretched to the track's width drops in over it, feet in
  the outer lanes (blocked), middle lane needs a slide. Per-biome `Giants` config. Release v1.2.4.
- **1.2.4 test (user screenshots):** the minigame rendered fine (own camera, pictures, the
  player's avatar with gear all work; Standard shader exists for the land), rewards worked
  (+1.25 Run levels, Carrot Soup). Problems: a troll waiting 9 m up looked like it floated;
  vanilla rocks (`rock4_forest` etc.) are ~20 m mounds and covered the track; flat cube land
  looked "eerily flat"; the borrowed `stone_floor_2x2` material read almost white; no coin sound.
  1.3: side land is a generated hill mesh (Perlin, flat for 2.5 m off the track edge, per-biome
  amplitude blended over 40 m), scenery sits on it and is pushed out by its renderer bounds,
  the troll leaps in from beside the track, track `_Color` × 0.6, coin sound falls back
  through `sfx_pickup|sfx_pickable_pick|sfx_gui_button`. Release v1.3.5.
- **1.4 (user feedback on 1.3.5: flat swamp, fake troll swing, too much XP):** creature
  attacks are read before stripping (`CreatureInfo`, `Creatures.cs`): weapon items from
  `Humanoid.m_defaultItems`/`m_randomWeapon`/`m_randomSets`, each with `m_attack.m_attackAnimation`
  (set as an animator trigger on the picture), `m_attackType == Projectile` +
  `m_attackProjectile` (ranged), the item's `attach` child (pictured onto the hand found by
  `VisEquipment.m_rightHand`/`m_leftHand` path; bows left), `m_animationState` → `statef`/
  `statei`, and the first ZSFX prefab of `m_startEffect`/`m_triggerEffect` as the attack sound.
  Ranged monsters fire a pictured projectile (fallback `Stone`, plus a small point light) that
  flies at 9 + 0.6 × biome m/s toward the runner's lane at firing time (slide under or change
  lane). Monsters move by type (`m_runSpeed ≥ 7` = fast: charge at 0.6 × run speed; others walk).
  The road rolls (Perlin, amplitude 0.4 + 0.55 × biomes, × 0.35 in the Swamp), the track is a
  generated strip mesh, every thing is placed at `RoadY(z) +` its own height. Swamp: land dips
  under a road-following water strip (Standard shader set to Fade by hand) 0.3 m above the
  track, islands further out. Ground: a procedural tileable value-noise texture tinted by the
  biome colour (the terrain shader needs splat maps, so it can't be borrowed). Run XP default
  1 → 0.25 levels per biome, with a `Balance Version` entry that rewrites an untouched old 1
  once. The user asked to keep the classic stiff log swing now and then: 30 % of swing trolls.
  Untested in-game: whether creature attack triggers play from the picture animator, the
  Fade water variant exists in the build, and hand paths resolve.
- **1.4.6 test → 1.4.7–1.4.11:** a borrowed build-piece material (`stone_floor_2x2`) on a
  generated mesh was invisible from most angles / glaring white: use only its `mainTexture` on
  the Standard shader (track now double-sided too). Weapon `attach` pictures instantiated with
  `Instantiate(x, hand, false)` came out enormous: creature hand bones carry big scale, and
  `VisEquipment` keeps the item's **world** size, so set `localScale = item attach lossyScale ×
  fit scale / hand.lossyScale`. Fitting every monster to 2.2 m made abominations tiny and
  greylings big: keep real size, only shrink (max scale 1, cap 4.2 m). Monsters idle until they
  notice you, then play `BaseAI.m_alertedEffects` and `FootStep.m_effects` sounds. Enter on
  `surfsup` could also open the chat (stealing keys): `Chat.Update` prefix skipped while
  surfing. Land tries the Heightmap material's 2D textures by property name (logged once; may
  be texture arrays). Debug: `Invincible`, `Game Speed`. Short creatures (< 0.75 × jump
  height) can be jumped over.
- **1.5 (user: wrong creatures per biome, rarity, no fog, lighting off, behaviours):**
  creatures come from the game's spawn tables: `Resources.FindObjectsOfTypeAll<SpawnSystemList>()`
  → `m_spawners` (`SpawnSystem.SpawnData`: `m_prefab`, `m_biome` flags, `m_enabled`,
  `m_maxSpawned`, `m_spawnInterval`, `m_spawnChance`, `m_groupSizeMin/Max`, `m_spawnAtDay/Night`,
  `m_requiredEnvironments`, `m_requiredGlobalKey`), weight = chance × max / interval (night-only
  and weather-only × 0.35). `Heightmap.Biome`: Meadows 1, Swamp 2, Mountain 4, BlackForest 8,
  Plains 0x10, AshLands 0x20, DeepNorth 0x40, Ocean 0x100, Mistlands 0x200. **EnvMan never sets
  `RenderSettings.fogMode`** (only colour/density), so forcing exponential-squared likely hit a
  stripped shader variant: keep the world's mode. Per-biome look from `EnvMan.m_biomes`
  (`BiomeEnvSetup.m_biome`, `m_environments` of `EnvEntry` {`m_environment`, `m_weight`,
  `m_env`}) → `EnvSetup` day values (`m_fogDensityDay`, `m_fogColorDay`, `m_ambColorDay`,
  `m_sunColorDay`, `m_lightIntensityDay`). The world sun (`EnvMan.m_dirLight`) is switched off
  in our camera's `OnPreCull`. Real creature height = root `CapsuleCollider` height × scale.
  `MonsterAI.m_sleeping` / `m_wakeupEffects` and the animator bool `sleeping` (abomination
  ambush). Attack visuals: particle prefabs in `Attack.m_triggerEffect/m_startEffect`.
  `surfprobe` console command logs weather, fog, sun, cameras, post-process volumes, terrain
  shader properties and nearby creature sizes for in-game sampling. Release v1.5.14.
 whether Valheim's own sun also lights layer 30
  (stage may look too bright), whether `Shader.Find("Standard")` exists for the land colours,
  how the cloned player animator looks, and whether the borrowed `stone_floor_2x2` material
  tiles well.

---

- **Later 1.5.x (user: vanilla screenshots, "make it look like the game"):** Use Game Camera
  draws the private stage through the game's own camera (culling mask set to the stage layer,
  moved in a `GameCamera.LateUpdate` postfix) for its post effects and sky; Game Terrain gives
  the land and track copies of a loaded `Heightmap.m_material` with biome vertex colours and a
  small `_ClearedMaskTex` (track = dirt or paved path; Track Style stone = old floor texture).
  See docs/game/camera-weather-terrain-look.md. Not yet seen in-game.
  1.5.18 (user's log): the per-biome spawn lists held mod and late-game creatures with one
  entry per biome (JotunWarrior, Elaking, JotunWitch, Skeleton, Charred in every biome), which the
  per-entry 4-biome filter misses. Now a prefab found in 4+ biomes is kept only where it's at
  least twice as common as its rarest biome, and strays under 10% of their home weight are
  dropped. Weight = spawn cap x clamp(sqrt(chance x 600/interval), 0.25, 1.5) (the old rate
  formula made Neck 7.4 vs Troll 0.0). Probe confirmed: main camera is HDR deferred with
  PostProcessingBehaviour, SunShafts, AmplifyOcclusion; fog is Exponential; EnvMan day values
  matched what the minigame read.
  1.5.19: trolls, crypt draugr, Fenring/Ulv caves, goblin villages and infested-mine seekers come
  from locations (CreatureSpawner in location prefabs, which aren't loaded until placed), not
  the world spawn tables, so a per-biome Location Monsters list adds them at modest weights (the
  user asked not to overweight them).
  1.5.20: creatures are placed by their root (a character's pivot is at its feet), not by the
  render bounds of the unanimated copy (bind-pose bounds float or sink them); the straddling giant
  keeps its proportions. `forward_speed` is the real ground speed in m/s (`Character` sets it from
  velocity; `CharacterAnimEvent` changes `m_animator.speed` during attacks; `Character.Awake`
  scales enemies by `Game.m_enemySpeedSize` and world level). `surfcompare` puts the picture beside
  a live creature, copies its animator floats/ints/bools and cross-fades to its state each frame,
  and logs both bounds and clip names: the way to check sizes and animations in-game.
- **1.6 (user quiz, creature by creature):** each creature has tags in the Creature Schemas
  setting (Schemas.cs; SurfSchemas.cs is a partial of SurfGame): formations (swarm, herd, line,
  crosser), gaits (charger, waiter, switcher), attacks (sweep, gas2/gas3, smash, slams, lob, fire,
  tar, bomb, tickdrop, shooter, flyshooter, leaper, headleap), and chaser/behind/helper/talk. The
  user's rule: any lane about to be hit is marked on the ground first (Sprites/Default quads,
  pulsing orange). Star loop: after Loop Biomes (6 = up to the Mistlands) the run goes back to the
  Meadows with 1, 2, then 3 stars; star looks come from `LevelEffects` read before stripping
  (`m_levelSetups[level-2]`: transform scale, a copy of `m_mainRender.sharedMaterials[0]` with
  `_Hue`/`_Saturation`/`_Value` and optional `_EmissionColor`, `m_baseEnableObject` off,
  `m_enableObject` on). Dvergr lines come from `NpcTalk.m_randomGreets`/`m_randomTalk`
  (localization keys; the game shows them with `Chat.SetNpcText`). The quiz answers are in the
  project files (snakewaysurfer/schemas.md). Ashlands is on hold until the user gets there.
- **1.7 (bosses, gate, checkpoints, scenery):** the run is a list of stretches (SurfBoss.cs):
  biome, then its boss arena in the same biome, then the next biome. Only bosses already beaten
  in the world are fought, and the lap only reaches one biome past the furthest one
  (`defeated_*` keys, see `docs/game/world-progress-keys.md`; "Ignore Boss Gate" under Debug
  bypasses). A biome with a boss stays open until you hold the game's summoning amount (items
  replace some coin runs); then the stretch is closed at a ground-tile edge and everything laid
  out past it is removed and rebuilt, so stretch ends never need to be known ahead (biome blends
  run over the first 40 m of a stretch, from the previous one). Bosses: three hits, landed by
  grabbing their weakness from the track; moves are marked strikes, walls, rows to jump or
  slide, the boss's own projectile and shockwaves. A win saves `biome:stars` in
  `Player.m_customData` per world; `surfsup [biome] [stars]` picks a saved checkpoint, plain
  `surfsup` the latest. Landmarks (nests, bone piles, totem poles, Dvergr props) appear beside
  the track near their creatures; pickables line the track edge. Boss ideas doc: the user's
  Claude Doc "SnakewaySurfer boss fights". Not yet tested in-game.
- **1.8–1.10 (user's answers in the boss doc):**
  - **Lives:** three strikes, with 1.6 s of invulnerability after a hit (the Lives setting).
  - **Moder:** grabbing a bolt pauses the run and you aim a ballista for 3.5 s. A miss wastes
    it, as the user wanted.
  - **Arenas:** take on the boss's tint. With Boss Landmarks off, the fog gets thicker.
  - **1.9, the Elder's branches:** root walls between the lanes, so lane changes are blocked
    inside (`CanSwitch`). Some branches end in a root wall. Sliding under a torch root burns
    that branch's wall.
  - **1.9, star tricks:** each star lap adds one move per boss (`BossDef.StarMoves`). A move that
    pauses the boss must raise `m_bossAttackTimer` itself, and the caller keeps the larger value.
  - **1.10, landmarks:**
    - The boss's own ZoneSystem location is a backdrop. It sits at a fixed distance ahead,
      0.85 / fog density clamped to 32–95 m, and stays behind the boss. It rises out of the
      ground as you arrive.
    - The path flattens and the land rises into walls (per-boss `Walls`). Torches line the path
      (`piece_groundtorch_*`, one light per pair).
    - The arena's look ramps over its first 40 m and out over the next stretch's first 40 m, like
      the biome blends. `Segment.PrevBoss` covers arena after arena.
    - See `docs/game/locations-and-custom-items.md`.
  - **1.10, boss rush:** after the 3-star lap, every reachable boss in a row (`Segment.Rush`), with
    no checkpoints. `Following()` must stay free of side effects, because `BossHit` calls it to
    look ahead.
  - **1.10, the reward:** a full six-boss rush gives Golden Surfer Shoes, a trinket cloned from
    TrinketBronzeStamina:
    - +15% movement, and full adrenaline adds +5% speed and 2.5x stamina regen.
    - The model and icon are gilded, with a generic winged emblem. The user asked for a Nike
      swoosh, which is a trademark, so it isn't used.
    - Given once per character. If the bag is full, the shoes are owed and given when a run ends.
  - Not yet tested in-game.
- **1.11 (user: "make the terrain more true to the game"):**
  - Each biome borrows a ring, 170 m in radius and ±90 m across, of this world's real ground. It
    is the nearest spot where the whole ring is that biome and above water.
  - The track runs round the ring, and heights are taken relative to the ring's centre line,
    using `GetBiomeHeight` with the biome forced.
  - The generator's paint mask is applied per tile.
  - The game's grass grows by `ClutterSystem` rules and is drawn instanced.
  - The track stays a hoe-painted path, dirt or paved; the user asked for that, and it was already
    the case since 1.5.17.
  - See `docs/game/camera-weather-terrain-look.md`. Not yet tested in-game.
- **1.12 (user tested 1.11; chose "boss chases, camera faces back"):**
  - **Missing ground textures (cyan ground):** the shader clips terrain within
    `_LodHideDistance` of `_NetRefPos` (the player). Our stage is 4000 m straight above the player,
    so it was inside that distance. Fix: set `_LodHideDistance` to 0 on our materials and in each
    tile's property block. See `docs/game/camera-weather-terrain-look.md`.
  - **Attack timing:** read from the clips' `OnAttackTrigger` animation events, since our copies
    don't fire events. Learned per animator and trigger. Triggers are often random variants
    (`attack0`/`attack1`): the GoblinBrute has no plain `attack` trigger, which is why brutes never
    animated. See `docs/game/attacks-and-throws.md`.
  - **Boss chase** (`SurfChase.cs`):
    - The boss runs 12–20 m behind you, and the camera swings round over the side to look back.
      Left and right follow the screen while it faces back.
    - Lane strikes show a red band that rides along with you for 1.1–1.6 s, then land with the
      boss's own effects.
    - Waves and bars race up from behind with negative `Thing.Speed`, so the movement check is
      now `!= 0`. Things behind are kept for 90 m instead of 25 m.
    - The Elder's branches open lanes −1 and 3 on ground flattened beside the track, with
      TentaRoot vines.
  - **Boss weather:** the boss location's `EnvZone.m_environment`, with its `m_psystems` cloned
    under our camera. Thunder and lightning flash for Eikthyr.
  - **Summoning:** the items circle, then the `OfferingBowl` start effects play, then the done
    effects play and the boss scales up.
  - See `docs/game/locations-and-custom-items.md` and `asset-names.md`. Not yet tested in-game.
- **1.13 (the user tested 1.12):**
  - **The 1.12 LodHide fix did nothing.** `_LodHideDistance` is not a material property
    (`HasProperty` is false); Heightmap only sets it per renderer, for distant LODs. Heightmap
    sets only `_ClearedMaskTex`, `_depth` and `_LodHideDistance`, and `RenderGroupSystem` only
    turns registered renderers on and off. Why `Custom/Heightmap` draws nothing on the stage is
    still unknown.
  - **Workaround:** copy each slice of the terrain material's `_DiffuseArrayTex` (a
    Texture2DArray) into a Texture2D with `Graphics.CopyTexture`, one mip at a time, without
    `Apply`. Average each slice (Blit to an 8x8 RT, then ReadPixels). Pick the slice closest in
    HSV to the biome's ground colour, and put it on the Standard shader.
  - Only Eikthyr chases (the user: "only Eikthyr should be that way"). The other bosses wait
    ahead.
- **1.14 (the user's design asks):**
  - **No pop-up text during a run.** It now uses contextual cues:
    - The HUD shows the summoning item's icon (`m_shared.m_icons[0]`, drawn from the sprite's
      `textureRect` with `GUI.DrawTextureWithTexCoords`) with a count, and the boss's health
      bar.
    - The lane marks are coloured: red means get out of the lane, amber means jump, blue means
      slide.
    - Getting hit shows a red vignette and plays `Player.m_hitEffects` as 2D sounds.
  - **Sprint, block and parry, as the user asked:**
    - Blocking takes stamina; a parry does not. Stamina recharges over time.
    - Block sounds come from `currentBlocker.m_shared.m_blockEffect`, and the parry sound from
      `m_perfectBlockEffect`. `GetCurrentBlocker` is private, so they're reached through
      Traverse.
    - Sound names confirmed: `sfx_perfectblock`, `sfx_metal_shield_blocked`,
      `sfx_wood_blocked`, `sfx_footstep_water`, `sfx_land_water` and `sfx_land_tar`.
  - **Run XP:** vanilla raises Run by factor 1 once per second of running (in `Player`,
    through `m_runSkillImproveTimer`). The minigame now gives real XP: 0.5 per second, at most
    300 s. Before, it gave fractions of a level, which is far too much at a high skill (the
    user).
  - **Boss hits take timing:** Eikthyr's spears are thrown and must be timed to his weaving.
    Moder's ballista bolt flies at 45 m/s to the aimed point.
  - **Spawns:** weights are capped at max 6 (leeches have max 10). Giants come only as set
    pieces. Melee packs spread out instead of a conga line. Bats come in swarms that are a
    nuisance (they cost no life). Swamp water gets ripples and splashing steps.
  - Not yet tested in-game.
- **1.15:**
  - **Start menu:** a grid of biome × star checkpoint buttons drawn with IMGUI. While it's up,
    set the cursor free with `Cursor.lockState`/`visible` in OnGUI, which runs after
    GameCamera's capture.
  - **Parry duel:** most bosses are hurt by parrying their close-in swings. Heavy blows can't be
    blocked, only parried.
  - **Fireplace pictures need their flames turned on:** a lifeless copy of a torch keeps its
    flames on inactive `*enabled*` children, so activate those.
  - **Boss deaths:** play `Character.m_deathEffects` minus the `Ragdoll` prefab, which needs
    physics, and topple the model.
  - **Gas clouds:** a runtime `ParticleSystem` with a generated puff texture. Vanilla gas prefab
    names: `blob_aoe`, `blob_attack_aoe`, `bonemass_aoe`, `poisoncloud`, `shaman_attack_aoe`.
- **1.15.1–1.15.2 (v1.15.34–35, 2026-11-05):** summoning sounds at 55% (user: too loud).
  Eikthyr's lightning (user: needs bigger effects): a procedural bolt per blow (two
  `LineRenderer`s on `Sprites/Default`, white core plus wide tinted glow, re-jagged every
  0.06 s, with forks), a point light burst with `cullingMask = 1 << layer`, the scene flash, and
  `fx_Lightning` at 2× size. Scaling an effect copy only grows its particles when each
  `ParticleSystem.main.scalingMode` is `Hierarchy`. Antlers glow and spark during the wind-up.
- **1.16 (v1.16.37–39, 2026-11-05): the ocean crossing** (the user's design: a longboat
  top-down shooter between the Mountains and the Plains, a storm with big rolling waves and a
  serpent mini-boss in the middle, wind that's always on and shifts). Not yet tested in-game.
  - **Flow:** a `Sea` segment is inserted by `Following` before the Plains (only when the
    Plains are in the lap). Beating Moder saves checkpoint (4, stars), which now starts on a
    short `Shore` coast; reaching the far beach saves a per-world "landed" list in
    `m_customData`, which opens a direct Plains start. Pre-1.16 progress past the Plains
    counts as landed. The start menu gets an Ocean column.
  - **Coast:** the road and land sink over 70 m into the sea (`RoadY` and `LandAt` wrap a
    shore weight; the land beside the track holds up longer, giving a bay with headlands).
    When the arena before it closes, the ground tiles on the slope are rebuilt. The terrain
    shader's `_WaterLevel` set per renderer (property block) to our sea height gives wet
    sand at the waterline.
  - **Sea:** one 73×73 grid that follows the boat, its heights worked out in stage space every
    frame (swell plus rolling waves), so the waves stay put while the grid snaps. The outer
    ring is pushed 8× out to reach the horizon. Standard shader in Transparent mode with a
    generated normal map, its UV offset including the grid's position. Foam (wake, wind
    streaks, crests, surf, reef breakers) is one mesh of flat quads on `Sprites/Default`.
  - **The game's own ship rules, from the decompile:** `Ship.GetWindAngleFactor` is
    `Lerp(0.7, 1, 1 − |d|) × (1 − LerpStep(0.75, 0.8, d))` with `d = dot(windDir, −forward)`
    (best abeam, 70% running, nothing within about 37° of the wind). Sail force scales with
    `Lerp(0.25, 1, windIntensity)`. The sail furls by moving `m_sailBottomTransform` between
    `m_sailFurledPosition`, `m_sailMidfurledPosition` and `m_sailUnfurledPosition`. The mast
    turns to `LookRotation(−Lerp(w, normalize(w − fwd), 0.5 + dot(fwd, w) × 0.5), up)`, where
    `w = Cross(Cross(windDir, up), up)`. `ShipControlls.m_attachPoint` and
    `m_attachAnimation` give the helm. The sail cloth (MagicaCloth) is stripped from
    pictures.
  - **Assets used:** `VikingShip`, `Serpent`, `Leviathan` (Animator triggers `shake` and
    `dive`; `m_reactionEffects` and `m_leaveEffects`), `Hatchling`, `Deathsquito`, `Seagal`,
    `TurretBoltWood`, `fx_turret_fire`, `TrophySerpent`, and the sea stacks `rock4_coast`,
    `RockFinger` and `RockThumb`. The sounds are `AudioMan.m_oceanAudio` and `m_windAudio`
    (AudioClips) looped on our own AudioSources through `m_ambientMixer`, and
    `sfx_serpent_*`. The HUD wind rose reuses `Hud.m_shipWindIcon`, `m_shipRudderIcon`,
    `m_fullSail`, `m_halfSail` and `m_rudderSlow` sprites.
  - **Cues instead of text:** red streaks and rings on the water for lunges, spit, lightning
    and the serpent coming up under the keel. A pale band shows the safe way to meet the next
    rolling wave, and it turns red while you're side on. The wind arrow is bright when it fills
    the sail. Lines of reef surf mark the channel's edges.
  - **Pitfalls:** names clash across partial-class files (`BoltSpeed` and `FireBallista`
    already existed for Moder). A wave that travels with a fast ship never arrives: make
    following seas faster than the ship and drop waves left far behind. A wave raised from one
    point needs a lateral falloff, or it spans the whole sea.
- **1.16.40–42 (2026-11-06): the user's playtest batches.** .40: Bonemass gas billows up far
  ahead, slow wind-ups are learned from each creature's clip, the slide is the game's dodge
  roll, holding block drains stamina and slows you, Blocking and Dodge XP. .41: the Elder's
  maze of six paths, swamp archers, crypts and fire vents, weaving leeches, snowy Mountains.
  .42: Moder and the bow (below). Not yet tested in-game.
  - **Moder:** her animator needs the bool `flying`, the triggers `fly_takeoff` and
    `fly_land`, and the bool `onGround`. Her attacks are `dragon_spit_shotgun`
    (`attack_iceball`, which fires `dragon_ice_projectile`), `dragon_coldbreath`
    (`attack_breath`), and the bite, claw and taunt. Her volley lands as rows of crystals,
    each row leaving one lane free. The crystals are `Pickable_MountainCaveCrystal` pictures
    that burst with `fx_crystal_destruction`.
  - **Bow:** `Hud.UpdateCrosshair` scales `m_crosshairBow` by `Lerp(1, 0.15, draw)` and
    colours it `Lerp(clear, yellow, draw)`. Mouse sensitivity and invert are the static
    `PlayerController.m_mouseSens` and `m_invertMouse`. `ZInput` lives in `assembly_utils`,
    and `ilspycmd -t` against `assembly_valheim` can't decompile it. `Projectile` moves by
    `m_vel += down × m_gravity × dt`, with `m_drag` and `m_hitEffects`. To hit along the
    arrow's path, test each frame's segment against a sphere and against the boss's skinned
    mesh bounds. Checking only the end point misses arrows that visibly pass through.
  - **Weapons in the avatar's hands:** `VisEquipment.m_leftHand` and `m_rightHand` are the
    joints. The item's `attach` child goes under the joint at its own world scale (bows go in
    the left hand). The avatar's stance comes from `statef` and `statei`, set from the item's
    `m_animationState`. When `m_attackChainLevels > 1`, `Attack.Start` triggers
    `m_attackAnimation + level`, and the chain resets after 0.2 s without an attack.
    `Character.Stagger` triggers `stagger`. Vanilla has no silver atgeir: the atgeirs are
    Wood, Bronze, Iron, Blackmetal, HimminAfl and the Gold variants.
  - **Rendering pitfalls:** a mesh with a NaN vertex gets "abnormal mesh bounds" and is
    culled (the sea vanished near the coast), so set fixed bounds and sanitise the heights.
    The terrain shader drew our path strip nearly black, so the default path is now the
    `TerrainSlice` dirt texture on a plain lit material. Lane marks in the Swamp have to be
    raised by the water depth, or they sit under the water.
  - **Pitfall:** a private method with the same signature in two partial-class files fails
    the build (`BossAttackFor`). Check new names against the other files before pushing.
- **1.16.43–44 (2026-11-06): the sea batch from the user's first crossing.** Not yet tested
  in-game.
  - **Ship health:** at sea `Hit` hands the blow to the hull first (6 planks; heavy blows cost
    2), so a life is only lost when the hull breaks. Blocks and parries still run before it.
    Driftwood and island workbenches mend it. A broken hull sits lower and lists.
  - **Islands:** they are raised in the shore terrain function, so the waves and the terrain
    shader's waterline work on them for free. Their vertex colour is blended to
    `Heightmap.GetBiomeColor(Meadows)` to get grass on top.
  - **Wind gauge:** `Ship.GetWindAngleFactor()` is `Lerp(0.7, 1, 1 - |d|) × (1 - LerpStep(0.75,
    0.8, d))` with `d = dot(windDir, -forward)`. So there is no drive within about 37–41° of
    the wind's source. `GetWindAngle()` is `-YawFromDirection(local windDir)`, and the HUD
    rotates `m_shipWindIconRoot` by it. The run draws its own ring: the dark arc is centred
    on where the wind comes from, and a tick marks the bow. The game's ship HUD hierarchy is
    logged once (`Ship HUD:`) for checking against the real one.
  - **Assets:** vanilla `Serpent` has only `Serpent_attack` and `Serpent_taunt`, with no
    breath. Breath effects exist only for the Ashlands `BonemawSerpent_*` and Moder's
    `vfx_dragon_coldbreath`; the run reuses the latter as spray. Leeches are `Leech` and
    `Leech_cave` (`Leech_BiteAttack`). `piece_workbench_ext1`–`ext4` are the workbench
    upgrades.
- **1.16.45 (2026-11-06): burial chambers, the first dungeon (SurfCrypt.cs).** Not yet tested
  in-game.
  - **Turns on a straight track:** the run's z still counts along the track. Each hall between
    two corners has its own frame Transform under the stage, turned 90° about the corner's
    centre point `(0, 0, z_c)`: `P_k = P_{k-1} + Q_{k-1}(c - R c)`, `Q_k = Q_{k-1} R`. In
    LateUpdate every direct child of the stage and of the frames is reparented by its
    `localPosition.z` with `SetParent(frame, false)`. So all the existing lane code works
    unchanged, as long as world positions are worked out from the right frame, never from
    `m_stage`. The grass matrices are a case of this: they are baked at spawn time.
  - **Turns come in out-and-back pairs**, so the heading ends north and the last frame has no
    rotation. Once you are out, the stage moves to the last frame's pose and that frame's
    children move back onto it.
  - **Corners:** the avatar's world jump at a frame change is eased out as a decaying offset.
    A missed corner holds z at the turn point while the avatar runs on toward the wall. A
    late press still turns. Reaching the wall costs a life (an unblockable `Hit`), then the
    turn happens on its own. Things round a corner from you don't update or move, and movers
    that pass you round a corner are removed.
  - **Darkness:** the game's camera draws its sky behind ours, so in the crypt it clears to a
    solid colour, restored on exit and on destroy.
  - **Assets:** `stone_wall_4x2`, `stone_floor_2x2`, `stone_pillar(_high)`, `piece_walltorch`,
    `BonePileSpawner`, `TreasureChest_forestcrypt`, `crypt_skeleton_*`,
    `Pickable_ForestCryptRemains01–04`, `Skeleton(_NoArcher/_Poison)`, `Ghost`,
    `sfx_skeleton_rise`, `vfx_ghost_spawn`. `forestcrypt_*` rooms are DungeonDB `RoomData`
    with soft-referenced prefabs. They could later replace the built halls.
- **1.16.46 (2026-11-06): sunken crypts, the second dungeon (SurfCryptThemes.cs).** Not yet
  tested in-game.
  - **Themes:** the crypt code takes a `CryptTheme` (piece names as `a|b` fallback lists,
    creature tables, pile, chest, low obstacle, gate torch and light, fog and ambient colours,
    idle sounds, flooding chance, iron gates). `PlanCrypt` picks one by biome name.
  - **The dungeon's own kit:** `SunkenKit_*` walls and floors are not ZNetScene prefabs. They
    are children inside the dungeon's room prefabs. `DungeonDB.GetRooms()` returns `RoomData`
    with `m_theme` (a `Room.Theme` flags enum: Crypt 1, SunkenCrypt 2, Cave 4, ForestCrypt 8)
    and a `SoftReference<GameObject> m_prefab`. Call `Load()`, read `.Asset`, index the
    children that have renderers by name (strip " (N)"), and `Release()` them on destroy.
    Some kits build their walls long along z, so measure the bounds once and turn them 90°.
  - **Flooded halls:** a water quad with the swamp's water material and swamp vertex colour,
    plus a `WaterWeight` override inside the crypt (1 when flooded, else 0). Ripples, the
    markers' height and the player's splashes all follow it. The "is this hall flooded" test
    must use the floor's extent, not the corner z: a corner's square of floor belongs to the
    hall that runs into it.
  - **Iron gates:** three `Kind.High` things on empty GameObjects with no Label (so they can't
    be blocked), not collidable until the gate drops. It drops when it is
    `m_speed * 1.5 + 6` m ahead with no corner between, over 0.45 s, with slide markers.

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
- Their messages are typed fast, so read for intent ("I would thing to get the releases to the
  way we have been doing them" = switch the other repos to auto-release). When the doc you're
  updating mentions a gap, they may ask you to fix it in the same breath.
- After a test they often reply with a numbered list matching your test steps. Answer point by
  point, and if they say "revert", remove the feature fully (config entry included) rather than
  defaulting it off.
- Say plainly what you could not verify. A green CI build only proves it compiles, not that it
  works in-game.
- They sometimes interrupt a tool call to add a request. **An interrupted or "rejected" Bash call
  may already have run** (in the SnakePolearms session, and again in the SnakeWood session, a
  rejected `commit && push` had committed and pushed). Check `git status` / `git log` / the remote before redoing or reporting anything
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
| 1.4 | 2026-10-24 07:04 | Claude Code, SnakeWood session (review) | Review pass. Added: SnakeHammer (ecosystem row, §7d); per-repo default branches; release-scheme status per repo (SnakeSailing switch PR, Polearms/MapMarker still on tags); `add_repo` push can be refused by the permission check; `GlobalKeys` only has the first five boss keys; more on working with the user. Corrected: SnakeWood docs-only pushes don't release; SnakeWood is ~5.2k lines. Merged the duplicate combat-facing note into §5 melee and the duplicate failed-CI note into §2; intro now credits all sessions. |
| 1.5 | 2026-10-24 07:29 | Claude Code, ValheimMapMarker North Wisp session | Added: the user's standing rule to update these notes after every repo change (earliest change first) and to put decompile learnings in per-system topic files under `docs/game/` (new: mist and demisters, blocking and left-hand items, stamina/eitr/skills/spirit damage, vanilla effect and sound names); §7e North Wisp; reading big Decompile logs, dispatching it on the default branch. Corrected: the ValheimMapMarker package is SnakeTography. |
| 1.6 | 2026-10-25 04:40 | Claude Code, ValheimMapMarker North Wisp session | Added: ValheimMapMarker's switch to automatic releases with test pre-releases from manual runs (§1); new topic files `docs/game/attacks-and-throws.md` (attack flow, throws without a projectile, holding the player still, equip duration) and `docs/game/audio-zsfx.md` (ZSFX volume, positional sounds, no potion sounds); §7e rewritten for the weapon version and the user's feedback; `docs/game/network-objects.md` (the RemoveObjects error flood and destroying network objects safely); Valheim Plus Demister patch gotcha; stagger and camera shake; effects with game logic; §7e up to North Wisp 1.10.0 (flashbang). |
| 1.7 | 2026-10-28 04:17 | Claude Code, SnakeWood seat/reach/portals session | Added: topic file `docs/game/hover-and-portal-triggers.md` (`FindHoverObject` first-hit and reach rules, `TeleportWorldTrigger` only fires for the player's collider, `TeleportWorld.Teleport` refusals, saddle and rug prefab names); §5 riding note links it; §2 test builds from a PR branch by dispatching Build; §7 SnakeWood 1.23 (plain log default, Saddle style, riding reach, portal by log, lower hover at rest, fish disguised as fish, passenger grip). |
| 1.8 | 2026-10-28 05:30 | Claude Code, ValheimMapMarker North Wisp session | Added: §7e North Wisp 1.10.1 (gravity arc, proximity fuse) and 1.10.2 (burst shared over a routed RPC); `network-objects.md` routed RPC broadcast pattern. |
| 1.9 | 2026-10-29 04:09 | Claude Code, SnakeWood seat/reach/portals session | Added: vanilla half-log prefab list, scaling vanilla objects through `ZNetView.m_syncInitialScale` / `ZDOVars.s_scaleHash` (`TreeLog.Destroy` passes scale to halves); §7 SnakeWood disguise uses each wood's own tree half log, resized. |
| 1.10 | 2026-10-29 04:22 | Claude Code, SnakeSkills session | Added: topic file `docs/game/player-save-and-custom-skills.md` (vanilla `Skills.Load` drops unknown skill ids, `Player.m_customData` survives vanilla saves, SkillManager ids); §7f SnakeSkills (Sailing/Ranching/Mining combined, skill backup, config import); SnakeSailing ecosystem row, release scheme and CI notes updated. |
| 1.11 | 2026-10-30 03:48 | Claude Code, SnakeSkills session | Added: topic file `docs/game/fishing.md` (how vanilla raises Fishing, `FishingFloat.Catch`, fish prefabs and stars, one level per `Skills.Skill.Raise`); §7f SnakeSkills 1.1 fishing XP by fish. Merged the glxkng branch's v1.9 edits (SnakeWood 1.23.116). |
| 1.12 | 2026-10-30 04:30 | Claude Code, SnakeSkills session | Added: topic file `docs/game/terrain-and-hoe.md` (TerrainOp/TerrainComp, zone ownership, writing heights, build stamina, sprint, hoe ghost); §7f SnakeSkills 1.2 Landscaping skill. |
| 1.13 | 2026-10-30 04:33 | Claude Code, Pulsating Earrings session | Added: max eitr = food total (`GetTotalFoodValue`), eitr regen via `SEMan.ModifyEitrRegen`, trinket slot/prefab names/ViableTrinkets notes in `magic-costs-and-skills.md`; §7f SnakeSkills Pulsating Earrings permanent eitr bonus. |
| 1.14 | 2026-10-30 04:45 | Claude Code, SnakeSkills session | Added: crafting amounts and fish stars (`Recipe.GetAmount`, one-ingredient recipes, which item a craft consumes) to `docs/game/fishing.md`; §7f SnakeSkills 1.3 fish yield. Merged the ufzjcp branch's v1.13. |
| 1.15 | 2026-10-30 18:00 | Claude Code, SnakeSkills session | Added: custom hoe pieces without a TerrainOp, generated height = zero delta, restoring and paint, ±8 m delta clamp to `docs/game/terrain-and-hoe.md`; §7f SnakeSkills 1.4 (Restore terrain, Shift ramps). |
| 1.16 | 2026-10-30 20:15 | Claude Code, SnakeSkills session | Added: ramp along a fitted plane, stopping scroll zoom/rotation in build mode to `docs/game/terrain-and-hoe.md`; §7f SnakeSkills 1.5. |
| 1.17 | 2026-10-30 20:45 | Claude Code, SnakeSkills session | Added: `Heightmap.Poke`/`Generate` rebuild order and why a bump test needs a planarity check to `docs/game/terrain-and-hoe.md`; §7f SnakeSkills 1.6-1.8. |
| 1.18 | 2026-10-30 21:15 | Claude Code, SnakeSkills session | Added: §7f SnakeSkills 1.9 (skill limits on bump size and level depth per strike). |
| 1.19 | 2026-10-31 03:00 | Claude Code, SnakeSkills session | Added: §7f SnakeSkills 1.10 (ramps from the player's feet), and adding a new config key to change a default for existing users. |
| 1.20 | 2026-10-31 03:10 | Claude Code, SnakeSkills session | Added: §7f SnakeSkills 1.11-1.12 (plain click levels to feet; Raise ground ramps, workbench-free and cheaper via piece fields). |
| 1.21 | 2026-10-31 03:21 | Claude Code, Ember Blast session | Added: topic file `docs/game/projectiles-and-rocks.md` (`FireProjectileBurst`/`Projectile.Setup`/`OnHit`/`m_onHit`, `DoAOE` one hit per object, pickaxe and chop count in `GetTotalDamage`, `MineRock5.Damage` with `m_radius`, `MineRock`/`Destructible` damage, `StaffFireball` prefab); §7f SnakeSkills 1.13 Ember Blast. |
| 1.22 | 2026-11-02 00:10 | Claude Code, hoe ramp controls session | Added: §7f SnakeSkills 1.14 (vanilla clicks + bumps, Ctrl incline, Alt ramp blueprints with colour progress, new config keys for old modes); user is leaving SnakeSmarterHoe; `terrain-and-hoe.md` sections on TryPlacePiece interception, settings-copy for custom shapes and line previews. |
| 1.23 | 2026-11-02 03:02 | Claude Code, smart hammer session | Added: topic file `docs/game/build-menu-and-pieces.md` (piece tables, known recipes, `UpdateAvailable`, `SetSelectedPiece`, stub pieces, Jotunn-free UI); §7f SnakeSkills 1.17 smart hammer (SnakeHammer port on the vanilla hammer, N toggle, Y blueprint list); §7d points at it. |
| 1.24 | 2026-11-02 03:20 | Claude Code, hoe ramp controls session | Added: §7f SnakeSkills 1.18 (Ctrl+Z undo, decor plants, Shift rounding, eased plan ends, wheel fix); verified gotcha that `UpdatePlacement` charges stamina/resources/wear after `TryPlacePiece` even when a `PlacePiece` prefix skips placement. Corrected: custom hoe pieces must not charge stamina/durability themselves. |
| 1.25 | 2026-11-02 03:19 | Claude Code, smart hammer session | Added: §7f SnakeSkills 1.19 (hoe 1.18 and smart hammer 1.17 in one build, no key clashes, no double charge). |
| 1.26 | 2026-11-02 03:50 | Claude Code, hoe ramp controls session | Added: §7f SnakeSkills 1.20 (decor icons via black/white render alpha, ghost colliders stripped against floating previews, hoe-planted plants become pieces via a ZDO flag and a `ZNetView.Awake` postfix). |
| 1.27 | 2026-11-02 04:04 | Claude Code, hoe ramp controls session | Added: §7f SnakeSkills 1.21 (middle click resets the hoe brush to standard size). |
| 1.28 | 2026-11-02 04:11 | Claude Code, smart hammer session | Added: §7f SnakeSkills 1.22 (smart hammer always on, no toggle key; blueprint list pictures via an off-screen camera on a spare layer). |
| 1.29 | 2026-11-02 04:39 | Claude Code, smart hammer session | Added: §7f SnakeSkills 1.23 (Blueprints tab in the vanilla `BuildUi` build menu, blueprint pictures as icons); `docs/game/build-menu-and-pieces.md` gains the Deep North build menu (BuildUi, IPieceList, TabHandler). |
| 1.30 | 2026-11-02 05:00 | Claude Code, SnakeWood seat/reach/portals session | Added: the Categories tab lists by usage tag (`Piece.m_usage`, `UsageTagFlags`, `ByUsagePieceList`), not `PieceCategory` (topic file); §7 SnakeWood 1.23.167 under Transportation. |
| 1.31 | 2026-11-02 05:03 | Claude Code, smart hammer session | Added: §7f SnakeSkills 1.24 (blueprint materials panel; 1.23 thumbnail rig left a directional light in the scene and broke lighting, now created and destroyed per picture). |
| 1.32 | 2026-11-03 04:49 | Claude Code, hoe ramp controls session | Added: §7f SnakeSkills 1.25 (decor ghosts without colliders never leave the player's feet unless a clip flag is set; how `UpdatePlacementGhost` positions ghosts; V+ noInvalidPlacementRestriction). |
| 1.33 | 2026-11-03 22:40 | Claude Code, SnakePolearms release session | SnakePolearms now uses the SnakeWood auto-release scheme (PR #1, draft); first GitHub release v1.1.7 (§1, §7c). |
| 1.34 | 2026-11-04 02:40 | Claude Code, SnakewaySurfer session | Added: SnakewaySurfer (ecosystem row, default branch, auto-release, §7g); topic file `docs/game/player-anim-input-visuals.md` (synced `forward_speed` via `UpdateWalking`, `TakeInput`/`SetControls` locks, Console/Menu, `SetHelmetItem` ZDO sync, `Inventory.AddItem` overloads, `Skill.Raise` one level per call, private camera scene); tree/rock/log names and Deep North enemy/food localization keys in `asset-names.md`. |
| 1.35 | 2026-11-04 03:16 | Claude Code, SnakewaySurfer session | Added: §7g SnakewaySurfer 1.1–1.2 (food wear-off lowers health, `RequireComponent`-aware stripping, 2D sounds from `ZSFX` clips, per-biome fog/ambient, troll set pieces); sound prefab names in `asset-names.md`. |
| 1.36 | 2026-11-04 03:26 | Claude Code, SnakewaySurfer session | Added: §7g SnakewaySurfer 1.2.4 test results (what renders fine, vanilla rocks are huge, borrowed floor material too bright) and 1.3 (hill mesh, scenery push-out, leaping troll). |
| 1.37 | 2026-11-04 03:45 | Claude Code, SnakewaySurfer session | Added: §7g SnakewaySurfer 1.4 (reading creature attacks/weapons/projectiles from Humanoid items for pictures, rolling road, swamp water, procedural ground texture, config default migration). |
