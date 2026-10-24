# Building and placement

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-10-23 by the SnakeHammer session
(SmartBuild fork). All *(verified)* by decompile unless noted. Valheim Plus facts are from its
source (`Grantapher/ValheimPlus`, the maintained fork, Oct 2026).

## Click to placed piece

- `Player.UpdatePlacement(takeInput, dt)` runs each frame in place mode. On a click (within
  0.2 s, past `m_placeDelay`), for an ordinary piece: `if (m_noPlacementCost ||
  HaveRequirements(piece, CanBuild))` then `TryPlacePiece(piece)`; when that returns true it
  consumes resources (unless the piece's `FreeBuildKey()` global key is set), stamina
  (`GetBuildStamina()`), durability, and raises the build skill.
- **`TryPlacePiece` first calls `UpdatePlacementGhost(flashGuardStone: true)`**, which re-raycasts
  and re-snaps the ghost, then switches on `m_placementStatus` (message per failure) and calls
  `PlacePiece(piece, ghost.position, ghost.rotation, doAttack: true, cheated)`.
  A `TryPlacePiece` **prefix** that returns false skips that refresh, so it sees the ghost as last
  frame left it (what the player saw).
- `PlacePiece` instantiates the prefab at exactly the given pose.

## UpdatePlacementGhost

- `PieceRayTest` → rotation `Quaternion.Euler(0, m_placeRotationDegrees * m_placeRotation, 0)`
  (yaw only) → validity checks that set `m_placementStatus` (station extension, block radius,
  `m_mustConnectTo`, water/no-water, ground/cultivated/dirt, `m_notOnWood`, tilting surface,
  dungeon, deep snow) → ghost moved so its closest collider point (or the manual snap point,
  Tab) sits on the hit point → snapping (`FindClosestSnapPoints`, `Piece.GetSnapPoints(pos, 10f, ...)`).
- Postfixes from other mods can still move the ghost after all of that (see Valheim Plus).

## Build panel (Hud)

- `Hud.SetupPieceInfo(piece)` sets `m_buildSelection` (name), `m_pieceDescription` (TMP text),
  icon, then one `m_requirementItems[j]` slot per `piece.m_resources[j]` via
  `InventoryGui.SetupRequirement(slot, req, player, craft, quality, multiplier)`; hides the rest;
  puts the crafting station in slot `[m_resources.Length]`.
- **The slot count is fixed by the HUD prefab.** A piece (or a mod's blueprint) with more
  materials than slots silently loses the extras. Put the full list in the description instead.

## Valheim Plus (GUID `org.bepinex.plugins.valheim_plus`)

- **GridAlignment**: in V+'s `UpdatePlacementGhost` postfix, snaps the ghost to a grid while
  **Left Alt** is held or after **F7** toggles it (F6 cycles 0.5/1/2/4 m). The held state comes
  from `Input.GetKeyDown/GetKeyUp`, so **Alt+Tab can leave it stuck on** (key-up missed).
- **Advanced Building Mode** (F1 enter, F3 exit) moves the ghost from a `Player.Update` postfix;
  its `UpdatePlacementGhost` prefix/postfix pin the ghost so vanilla doesn't move it.
- **FreePlacementRotation** transpiles the rotation `Quaternion.Euler` (can add pitch/roll) and
  reads the wheel with `Input.GetAxis` (bypasses `ZInput` patches).
- Build-from-chest makes `HaveRequirements` count containers in range.
- So a mod drawing a preview from the ghost in its own `UpdatePlacementGhost` postfix must run
  after V+ (`[HarmonyPriority(Priority.Last)]`, `[HarmonyAfter(...)]`), or it previews a pose the
  piece won't be placed at.

## Changelog

- 2026-10-23: first version (SnakeHammer session).
