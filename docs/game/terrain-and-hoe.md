# Terrain edits and the hoe

How a hoe strike changes terrain and how a mod can change it further. From the decompile
(SnakeSkills 1.2 session, 2026-10-30) and SnakeSmarterHoe's `TerrainSync.cs`. Not yet checked
in-game.

## A strike, step by step

- Hoe pieces (`mud_road_v2` level ground, `path_v2`, `paved_road_v2`, `raise_v2`, ...) are
  prefabs with a `TerrainOp` on the root. Placing one instantiates it.
- `TerrainOp.Awake` (skipped while `TerrainOp.m_forceDisableTerrainOps`):
  `Heightmap.FindHeightmap(pos, GetRadius(), list)`, then for each heightmap
  `GetAndCreateTerrainCompiler().ApplyOperation(this)`, then `OnPlaced`, then destroys itself.
- `TerrainOp.Settings`: `m_level`/`m_levelRadius`/`m_levelOffset`/`m_square`,
  `m_raise`/`m_raiseRadius`/`m_raiseDelta`/`m_raisePower`, `m_smooth`/`m_smoothRadius`/`m_smoothPower`,
  `m_paintCleared`/`m_paintRadius`/`m_paintType`/..., `m_rotation`. `GetRadius()` is the largest
  enabled radius. Changing radii: give the instance a **copy** of the settings in an `Awake`
  prefix (SnakeSmarterHoe `CloneSettingsWithRadius` lists every field).
- `TerrainComp.ApplyOperation` sends `RPC_ApplyOperation` to the zone's owner; only the owner runs
  `DoOperation`: `InternalDoOperation` (LevelTerrain, RaiseTerrain, SmoothTerrain, PaintCleared),
  `Save(paintOnly)`, `m_hmap.Poke(1, paintOnly)` (delayed rebuild), `ClutterSystem.ResetGrass`.
- **A non-owner's strike reaches the owner as the piece's prefab hash only**, so changed radii
  are lost. Fix: claim every zone first (`comp.m_nview.ClaimOwnership()`, after `CheckLoad()` if
  `m_initialized`); any client may. Then the strike runs locally and synchronously inside `Awake`.

## Heights

- `TerrainComp` keeps per-vertex `m_levelDelta`, `m_smoothDelta`, `m_modifiedHeight`, index
  `y * (m_width + 1) + x`. `LevelTerrain` per vertex: `delta = target - GetHeight(x, y) +
  m_smoothDelta[i]`, `m_smoothDelta[i] = 0`, `m_levelDelta[i] += delta` clamped to ±8,
  `m_modifiedHeight[i] = true`. Writing a height the same way works for any custom edit.
- Zone edges are shared: a vertex on an edge lives in two or four heightmaps; write all of them.
- `Heightmap.GetHeight(x, y)` = `m_heights[y * (m_width + 1) + x]`, local to the heightmap.
  Vertex world position: `transform.position + ((x - m_width/2) * m_scale, h, (y - m_width/2) * m_scale)`
  (`WorldToVertex` rounds the other way).
- After `DoOperation` the heights aren't rebuilt yet (delayed `Poke`). `Heightmap.Poke(0, false)`
  calls `Regenerate()` at once, so a postfix on `TerrainOp.Awake` can read the result.
- Saving: `TerrainComp.Save(false)` (only the owner's save reaches other players), then `Poke`.

## Restoring and custom hoe pieces

- `m_levelDelta + m_smoothDelta` is the height above the generated terrain, so 0 is untouched
  ("restore" = move it toward 0; also clear `m_modifiedHeight`). Deltas are clamped to ±8 m, so
  a mod can't raise or dig more than 8 m from the generated height.
- Paint: `TerrainComp.m_paintMask[i]` (Color) and `m_modifiedPaint[i]`; reset with
  `Heightmap.m_paintMaskNothing`, and set the heightmap's `m_paintMask` texture pixel (x, y) to
  clear for an instant visual. Then `Save(false)`, `Poke(0, false)`, `ClutterSystem.ResetGrass`.
- A custom hoe piece without network or terrain side effects (SnakeSmarterHoe's way, reused by
  SnakeSkills): copy a vanilla hoe piece under an inactive `DontDestroyOnLoad` root (no `Awake`),
  remove `TerrainOp` and `ZNetView`, empty `m_resources`, add it to the Hoe's `PieceTable.m_pieces`.
  It only shows if its `Piece.m_name` is in the known-recipes set passed to
  `PieceTable.UpdateAvailable(knownRecipies, player, ...)` (prefix adds it). Intercept
  `Player.PlacePiece(piece, pos, rot, doAttack)` with a prefix returning false and do stamina
  (`GetBuildStamina`), durability, the attack animation (`m_zanim.SetTrigger`) and the effect
  yourself.

## Player side

- Stamina per strike: `Player.GetBuildStamina()` = attack stamina × home-item modifiers and
  status effects, minus 0.5 × skill factor of the build table's skill (none for the hoe). Called
  on each placement from `UpdatePlacement`.
- Sprint: `Player.CheckRun(Vector3 moveDir, float dt)` is a protected override; a prefix
  returning false with `__result = false` stops sprinting.
- Ghost: `Player.UpdatePlacementGhost(bool)`; the hoe ghost shows its area through a
  `_GhostOnly` child, `Projector.orthographicSize` and `CircleProjector.m_radius`. Projector
  needs the full Unity module references (SnakeWood's `AddGameReferences` csproj target).

## Used by

- SnakeSkills `Landscaping.cs` (1.2): bigger brush by skill, less stamina, bump flattening, cart
  slope easing, no sprint. See notes §7f.
- SnakeSmarterHoe (notes §7b): its own brush tools; SnakeSkills leaves the hoe alone when it is
  loaded.
