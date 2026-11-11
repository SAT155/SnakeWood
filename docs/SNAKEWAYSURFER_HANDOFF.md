# SnakewaySurfer handoff (2026-11-07)

**State:** v1.16.171 (2026-11-11, branch `claude/project-thread-8ph0gw`, draft PR #6: self-test tour and raven picture, stacked on) v1.16.163 (2026-11-10, branch `claude/project-thread-jzg0yv`, draft PR #5 stacked on #1: dungeon junctions, chase cam in dungeons, health regen, star mixes, boss fixes; see notes §7g). Before that v1.16.149 (2026-11-10; a dodge roll passes labeled Block attacks and creatures, except Wall roots and vines; includes u27urh v1.16.132 Strange Portal fix; camp rest menu opens on sitting) is on branch `claude/project-thread-2zimly`, which is draft PR #1 into the default branch `claude/project-thread-9zfoi6`. A branch that isn't the default releases only when Build is dispatched on it with `workflow_dispatch`. Code is in `SnakewaySurfer/*.cs`, about 29k lines. Never publish to Thunderstore.

## Build and release
- CI is the only compiler. Each push that builds auto-releases as v1.16.N.
- Wait for CI:
  ```
  sha=$(git rev-parse HEAD); sleep 20; for i in $(seq 1 60); do r=$(gh api "repos/SAT155/SnakewaySurfer/actions/runs?head_sha=$sha" --jq '[.workflow_runs[]|select(.name=="Build")][0]|"\(.id) \(.status) \(.conclusion)"'); case "$r" in *completed*) echo $r; break;; esac; sleep 15; done
  ```
- Errors on failure:
  ```
  gh api repos/SAT155/SnakewaySurfer/actions/jobs/<jobid>/logs | grep "error CS"
  ```
  The job id comes from `actions/runs/<id>/jobs`.
- Common build errors:
  - CS0136: a local name reused in a nested scope, often `half`, `key` or `name`.
  - CS0535: a missing interface member. Hoverable needs `GetHoverOffset`.
- After each release:
  - Add a changelog line to `thunderstore/SnakewaySurfer/README.md` under "Changelog (spoilers)".
  - Add a notes bullet to `docs/VALHEIM_MODDING_NOTES_v1.40.md` (section 7g, before "## 8.").
  - Update memory in `snakewaysurfer.md` (keep it under 4 KB) and MEMORY.md.
  - Rename the thread to "SnakewaySurfer vX".
- Pushing prints "repository moved". This is harmless.

## Testing without SAT's eyes (2026-11-11)
- `surftest` in the console plays the run by itself and saves screenshots, `log.txt` and `report.txt` to `BepInEx/SnakewaySurferTest/<time>/`. Ask SAT for that folder (zipped) after each release instead of single bug reports. Details: notes section 7g, "SnakewaySurfer self-test and review".
- Every hit is in the BepInEx log as `Hit: ...`.
- Review and plan: https://claude.ai/artifact/Sa48CY9QvnaGMexzBYf9tn. Work lives on branch `claude/project-thread-8ph0gw` (PR #6, stacked on #5).

## Open bugs and owed items
- **Crash to desktop:** cause unknown. It happened on a retry in the Plains on v1.16.88 or older, and the log has no exception. SAT still owes the newest folder from `%userprofile%\AppData\LocalLow\IronGate\Valheim\Crashes`. v1.16.93 moved the ripple's `Camera.Render` out of OnGUI on a hunch.
- **Untested in game:**
  - Ravens:
    - The flying clip that `SurfStory.FlyClip` chooses. It's logged as "Raven flight: ...". If the log says "none", the ravens perch.
    - Whether the head-bone facing works.
    - Whether the text below Hugin is in view.
  - The dream ripple on portals (story portal, run entry, dream end).
  - Strange Portal behaviour.
  - Camp emotes and the harpoon animation.
  - The serpent tow.
- **Next feature:** coin bank and trader ability shop. Doc: https://claude.ai/artifact/PJ5gsWZaYt83pmpUDoCWHK. SAT's answers are in its comments:
  - Coins only go to the bank, and score resets each run.
  - The shops replace the life-refill stalls.
  - "Dreamify" the game's own store UI.
  - One vendor per biome on 1-star runs, two from 2 stars up.
  - Abilities last until that biome's boss.

## Where things live (open these; skip broad greps)
- **Run core:** `SurfGame.cs`. It holds state, Update and LateUpdate, `Begin()` (around line 304), ResetRun, and the GUI styles m_big, m_mid and m_small (around line 3790). It's 4k lines, so grep for the method name rather than reading it.
- **Menu, dream text, loading and ripple:** `SurfDream.cs` and `SurfMenu.cs`.
  - In SurfDream: BeginLoad, CoverNow, `CaptureWave`, `DrawWaveFrom`, `DrawHaze`, FirstRun.
  - `Fill` is in SurfMenu.
- **Ravens:**
  - World story (portal, nagging, sleep detection, `surfstory` command): `SurfStory.cs`. Line text is in `StoryLines.cs`.
  - In-run Hugin: `SurfRaven.cs`. It reuses SurfStory's static FlyClip, HoldFlight, HeadYaw and FitLive.
- **Checkpoints:** `SurfCheckpoint.cs`, with save data in `Progress.cs` (SaveResume, Resume, ClearResume).
- **Strange Portal hammer piece:** `SurfPortalPiece.cs`. It uses Harmony on ZNetScene.Awake and on ObjectDB Awake and CopyOtherDB.
- **Camp and harpoon:** `SurfCamp.cs`.
- **Rewards and timers:** `Rewards.cs`.
- **Sea:** `SurfSea.cs`, `SurfSeaFoes.cs` and `SurfHull.cs` (islands).
- **Bosses:** `SurfBoss.cs`, `SurfArena.cs` and `SurfChase.cs`.
- **Dungeons:** `SurfCrypt.cs`, `SurfCryptThemes.cs`, and `SurfCryptPlus.cs` (junctions, mud banks).
- **Configs and console commands:** `Plugin.cs`. The commands are surfsup, surfstory, surfportal and surfprogress.
- **Player data:** everything per character is in `player.m_customData`, with keys prefixed "SnakewaySurfer.". `surfprogress reset` wipes them all, including the story stage.

## Gotchas
- **Don't touch RenderSettings** around an overlay camera. Changing them makes the world's terrain lighting flicker.
- **Raven skinned bounds are bogus** (about 35 m) unless `updateWhenOffscreen` is set.
- **SAT's preferences:**
  - No popup text. Hugin speaks the messages.
  - Use Valheim's own assets.
  - Don't copy other games.
  - SAT runs Valheim Plus.
- **SAT's log:** it may not include our Info lines if it was taken at a lower log level. Ask for LogOutput.log from BepInEx.
