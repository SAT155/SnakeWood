# Valheim modding notes for Claude Code (SnakeSquad)

| | |
| --- | --- |
| **Version** | 1.19 |
| **Last updated** | 2026-10-31 03:00 UTC |
| **Updated by** | Claude Code, SnakeSkills session (`claude/project-thread-1pnygx`) |
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
| **SnakeSailing** (repo name kept; builds **SnakeSkills**) | blaxxun-boop's **Sailing**, **Ranching**, **Mining** skill mods combined into one plugin, **SnakeSkills** (2026-10-29, [SAT155/SnakeSailing#2](https://github.com/SAT155/SnakeSailing/pull/2), draft). Before that, three separate projects. | Uses **ServerSync** (config locked to server admin) and **SkillManager**, merged with **ILRepack** into one DLL. Skill levels survive unmodded sessions (backup in `Player.m_customData`). Upstream's ValheimPlus hard block is a warning. See §7f. |
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
  - Uses it: **SnakeWood**, **SnakeSmarterHoe**, **SnakeHammer** *(verified in their build.yml)*.
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
  - **SnakePolearms**: still releases only on a pushed `v*` tag, which sessions can't push (§2),
    so it ships as CI artifacts. Switching them needs a session with push access
    to those repos (the SnakeWood session's `add_repo` with push was refused by the permission
    check; ask the user).
- **Default branches are `claude/...` branches and differ per repo** (check with
  `git remote show origin | grep "HEAD branch"`): SnakeWood `claude/exciting-mendel-fudt8s`,
  SnakeSailing `claude/charming-noether-raq5zn`, SnakeSmarterHoe `claude/jolly-fermi-in181p`,
  SnakePolearms `claude/valheim-polearm-pierce-mod-ts6xtu`, SnakeHammer
  `claude/project-thread-55fg1l`, ValheimMapMarker `main`. A session's designated branch can
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
skill raises ([`fishing.md`](game/fishing.md)). Read the matching file before decompiling the same system again.

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

## 7. SnakeWood specifics (as of v1.23.116)

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
suggestions, landings) and v1.23.116 (per-wood resized disguise, Ancient Bark as swamp log, plain-log seat, Saddle look, riding reach, portal by log, lower hover at rest, fish disguise, passenger grip;
PR #3); FrozenKing boss key; a race timer HUD was suggested. Possibly a
"Lifetime Fuel" core for the sled, which is off for it now.

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

## 7d. SnakeHammer specifics

Summary from its README only (no SnakeHammer session's notes yet; the next session there should
add its own learnings). Private fork of SmartBuild 1.21.0, decompiled; never publish. Jotunn
dependency (`ValheimModding-Jotunn-2.30.1` in the manifest); builds as netstandard2.1 with C# 11
"like upstream". Release tags track upstream: `v1.21.<run>` (bump MAJOR.MINOR only to follow a
newer SmartBuild). Compatibility target is Valheim Plus's placement patches. Placement debugging
is under `Debug / LogPlacement`.

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

## 7f. SnakeSkills (SnakeSailing repo, 1.10.x)

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
  `PieceTable.UpdateAvailable` prefix, and `Player.PlacePiece` intercepted (stamina, durability,
  animation by hand). Details in [`terrain-and-hoe.md`](game/terrain-and-hoe.md). Test build v1.4.30.
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
  Test build v1.10.<run>.

Known open items: never tested in-game (skill backup, fishing XP, Landscaping, earrings, fish yield, Restore terrain, Shift ramps, 1.5 slope ramps, 1.7 red flash, 1.8 edge-safe bumps and hoe wear, 1.9 skill limits; 1.10 FromFeet ramps); slope easing on
level ground and big brushes may feel off; Deep North fish prefab names. Test: play with SnakeSkills, quit, start unmodded and
join, quit, start with SnakeSkills; the log should say `Restored level`. The repo could be renamed
SnakeSkills on GitHub if the user wants.

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
