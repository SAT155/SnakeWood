# Build menu, piece tables and selecting a piece

Read from Valheim with Deep North (Unity 6), 2026-11-02, by the SnakeSkills smart hammer session
(Decompile of `Player`, `PieceTable`). *(verified)* = read in the decompile.

## How a piece gets into the build menu *(verified)*

- A tool's `ItemData.m_shared.m_buildPieces` is a `PieceTable`. `m_shared` is shared by every
  copy of the item, so adding a prefab to `table.m_pieces` (the vanilla Hammer's, from
  `ObjectDB.GetItemPrefab("Hammer")` or the equipped item) shows it for everyone's hammer on this
  client. Remove it again to hide it.
- `Player.UpdateKnownRecipesList()` (private): for every piece table in the inventory
  (`m_inventory.GetAllPieceTables`), a piece whose `m_enabled` is true and whose `m_name` isn't in
  `m_knownRecipes` gets `AddKnownPiece` (the "new piece" popup, saved in the profile) when
  `HaveRequirements(piece, RequirementMode.IsKnown)`.
- `HaveRequirements(piece, IsKnown)`: false if `m_craftingStation` is set and the station isn't in
  `m_knownStations`, or a DLC is missing; then it checks each requirement's material is known. **A
  piece with no `m_resources` and no station is known at once.**
- `PieceTable.UpdateAvailable(knownRecipies, player, hideUnavailable, noPlacementCost)` rebuilds
  `m_availablePiecesByCategory` (9 lists, one per tab) from `m_pieces`: skipped unless `m_enabled`
  (or in season) and `knownRecipies.Contains(m_name)`. The set passed in is the player's own
  `m_knownRecipes`, so adding to it in a prefix persists (RestoreTerrain does that on purpose).
- `Player.UpdateAvailablePiecesList()` (private) calls `UpdateAvailable` and `SetupPlacementGhost`.
  Invoking `player.GetInventory().m_onChanged` runs `OnInventoryChanged`, which refreshes both.

## Selecting a piece from code *(verified)*

- `Player.SetSelectedPiece(Piece p)`: `m_buildPieces.GetPieceIndex(p, out index, out category)`,
  then `SetBuildCategory(category)` + `SetSelectedPiece(index)` (which calls `SetupPlacementGhost`).
  Returns false if the piece isn't in the current table's available lists, so add it, then call
  `UpdateKnownRecipesList()` + `UpdateAvailablePiecesList()` and retry.
- `PieceTable.SetCategory(Piece.PieceCategory)` sets `m_selectedCategory`. Piece category is
  `Piece.m_category`; `PieceCategory.Misc` = 0. Vanilla has no way to add a tab (Jotunn patches
  the HUD for custom categories), so mods without Jotunn reuse an existing tab.

## Custom "stub" pieces

SmartBuild's blueprint stubs: an empty `GameObject` under an inactive root (so it never runs in
the world; a ghost instantiated from it is active), a `Piece` with no resources, visual-only
child clones (components and colliders stripped, built with `ZNetView.m_forceDisableInit` and
`TerrainOp.m_forceDisableTerrainOps` set), a box collider on layer `piece_nonsolid` and
`snappoint`-tagged children. `Player.TryPlacePiece` is intercepted so the stub itself is never
placed. The ghost's `name` is the stub's prefab name.

## Jotunn-free UI

Jotunn's GUIManager widgets are Unity `UnityEngine.UI.DefaultControls` (panel, button, input
field, toggle) with game sprites (`woodpanel_trophys`, `button`, `text_field`, `checkbox`,
`checkbox_marker`; from the `UIAtlas` `SpriteAtlas` or `Resources.FindObjectsOfTypeAll<Sprite>`)
and fonts `AveriaSerifLibre-Regular`/`-Bold` (`Resources.FindObjectsOfTypeAll<Font>`), on a
ScreenSpaceOverlay canvas (sortingOrder 2000) under the root object `GuiRoot`'s child `GUI`.
SnakeSkills' `SmartBuild/GUIManager.cs` is a drop-in copy of the calls SmartBuild used.
