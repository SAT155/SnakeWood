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

## The Deep North build menu: `BuildUi` *(verified)*

Read 2026-11-02 (Decompile of `Hud`, `BuildUi`, `IPieceList`, `RecentPieceList`,
`BuildUiPieceButton`, `TabHandler`).

- The menu with tabs **Categories / Materials / Recent / Favorites**, a left list (Show All, Misc.,
  Crafting, Building, ...), F to search and stars for favorites is vanilla: `Hud.m_buildUi`
  (`BuildUi : MonoBehaviour`), opened by `BuildUi.OpenBuildMenu()`, closed by `Hud.CloseBuildUi()`.
  A tool's `PieceTable.m_hideAdvancedMenu` gives the simple menu without tabs/tags.
- Tabs: `BuildUi.Awake` fills `m_pieceLists` (`List<IPieceList>`) with `ByUsagePieceList("$hud_byusage")`,
  `ByMaterialPieceList`, `RecentPieceList` (`m_maxRecentPieces` 50), `FavoritePieceList`, and wires
  each child of `m_tabContainer` (a `Button`) to `SelectPieceList(childIndex)`. Tab index = list
  index. Q/E (`TabLeft`/`TabRight`) come from `m_tabHandler` (`TabHandler.m_tabs`, a list of
  `Tab { m_button, m_page, m_default, m_onClick }`); cycling invokes the tab's `m_button.onClick`
  and skips tabs whose `m_button` is null. `SelectPieceList` calls
  `m_tabHandler.SetActiveTabWithoutInvokingOnClick(i)`, which indexes `m_tabs[i]` (so a list
  index without a `Tab` throws). A tab's `Selected` child is shown for the active tab.
- `IPieceList`: `DisplayName`, `ShowTags`, `CanCustomizeTags`, `TagCount`, `TagSeparatorIndex`,
  `GetTagDisplayName(i)`, `GetTagIdByIndex(i)`, `GetAvailablePiecesWithTag(tagId, table, resultOut)`,
  `UpdateAvailableTags(table)`. `RecentPieceList` has no tags and returns pieces of
  `table.m_availablePieces` whose **`gameObject.name`** is in its name list, in list order;
  `AddPiece(name)` inserts at the front, `Reset()` clears. Only `m_recentPieceList` and
  `m_favoritePieceList` are saved (`SaveToBinary`), so an extra list is session-only.
- `UpdatePieceButtons()` (private) asks the current list for pieces and sets up pooled
  `BuildUiPieceButton`s (`Setup(piece, ui)`: icon = `piece.m_icon` into private `m_icon` Image,
  favorite star, search terms from `m_name` and resource names). It reruns when
  `m_currentBuildTool.m_availablePieces.Count` changes (`Update`) and on tab/tag change. Clicking
  calls `Player.SetSelectedPiece(piece)` then closes the menu.
- A placed piece goes to Recent through `BuildUi.AddRecentPiece`.

## Categories tab = usage tags *(verified)*

Read 2026-11-02 (Decompile of `Piece`, `ByUsagePieceList`) by the SnakeWood session.

- The Categories tab's left list (Show All, Misc., Crafting, Building, Transportation, ...) is
  `ByUsagePieceList`, built from **`Piece.m_usage`** (`Piece.UsageTagFlags`, a `[Flags]` enum with
  `[DisplayName("$tag_...")]` per value, e.g. `Transport = 0x800`, `Food = 0x1000`, `Meads = 0x2000`,
  `Feasts = 0x4000`, `Defense = 0x8000`, `Stacks = 0x10000`, `Stairs = 0x20000`, `Doors = 0x40000`),
  **not** `Piece.m_category`. `UpdateAvailableTags` ORs the flags of `table.m_availablePieces` and
  shows only tags some piece has; `GetAvailablePiecesWithTag` returns pieces with that flag (plus
  repair/remove pieces; tag -1 = Show All).
- `Piece.PieceCategory` (old tab system, still on pieces): `Misc 0, Crafting 1, BuildingWorkbench 2,
  BuildingStonecutter 3, Furniture 4, DeepNorth 5, Feasts 6, Food 7, Meads 8, Max 9, All 100`.
- A mod piece with `m_usage = 0` shows only under Show All. SnakeWood sets `Transport`.
