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
- Rebuild order (`Heightmap.Generate`, synchronous): `m_heights` = `m_buildData.m_baseHeights`
  (generated), then `ApplyModifiers`: every enabled `TerrainModifier` (piece-attached level/smooth),
  then `TerrainComp.ApplyToHeightmap`: each vertex `+= m_levelDelta + m_smoothDelta`, clamped to
  ±8 m of the post-modifier height. `GetWorldBaseHeight` reads `m_baseHeights`.
- Vanilla `SmoothTerrain` adds `lerp(h, target, t) - h` to `m_smoothDelta` (clamped ±1).
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
  `Player.PlacePiece(piece, pos, rot, doAttack)` with a prefix returning false and do the attack
  animation (`m_zanim.SetTrigger`) and the effect yourself. **Not stamina or durability:** the game
  takes those (and `m_resources`) in `UpdatePlacement` once `TryPlacePiece` returns true, which it
  does even when the `PlacePiece` prefix skipped (verified by decompile 2026-11-02; earlier notes
  said to charge them yourself, which double-charged).

## Shaping a ramp (SnakeSkills 1.5)

- Read the brush's heights before the strike (TerrainOp.Awake prefix), fit a plane by least
  squares (gradient G). A cart-safe ramp of max slope m follows G if |G| ≤ m; otherwise it runs at
  angle acos(m/|G|) to G (rises exactly m along it, level across it): two mirror choices, pick by
  the camera's facing. Blend target heights in with a smoothstep falloff at the rim.

## Knocking down bumps (SnakeSkills 1.8)

- Comparing a point with the mean of its neighbours (the plane's height for a symmetric ring) is
  not enough: the lip where a level strike meets lower ground, or the top of a cut, sits well above
  that mean and gets notched, which looks like jagged bumps. Also fit the ring's slope
  (`sum(h*dx)/sum(dx*dx)` per axis) and only lower the point if every ring point is within about a
  quarter of the point's excess from that plane.

## Custom shapes and tools on modifier keys (SnakeSkills 1.14)

- To use a hoe click for something that isn't a strike (laying blueprint points), prefix
  `Player.TryPlacePiece(Piece piece)` (bool) and return false with `__result = false`: nothing is
  placed or paid. `PlacePiece` (and so `TerrainOp.Awake`) runs inside `TryPlacePiece`, so a static
  set in its prefix and cleared in a finalizer is visible to the strike. Holding the button repeats
  placement; gate one-off actions on `Input.GetMouseButtonDown(0)`.
- For a strike with your own shape, copy the settings in the `TerrainOp.Awake` prefix with
  `m_level`/`m_raise`/`m_smooth` off (paint and stone/stamina stay vanilla), claim the zones
  yourself, and write heights in the postfix (SnakeSkills `TerrainZones.Tidy`).
- The hoe ghost keeps its `TerrainOp` (`GetComponentInChildren<TerrainOp>(true)` on
  `m_placementGhost`), handy for telling which tool is selected and its radius (`GetRadius()`).
- Previews: pooled `LineRenderer`s (`useWorldSpace`, `Sprites/Default` material) redrawn per frame.
  Ctrl is Crouch by default: a `Player.SetControls` prefix with `ref bool crouch` stops it.

## Undo and decor (SnakeSkills 1.18)

- Undo = save `m_levelDelta`, `m_smoothDelta`, `m_modifiedHeight`, `m_paintMask`, `m_modifiedPaint`
  per vertex before the strike, write back later (claim, `Save(false)`, `Poke(0,false)`).
  `TerrainComp.Initialize()` first if `!m_initialized`.
- Mouse-wheel modes: return 0 from a `ZInput.GetMouseScrollWheel` prefix (camera zoom and piece
  rotation read it) and read `Input.mouseScrollDelta` yourself.
- World plants as hoe pieces: ghost copy without scripts **or colliders** (the placement ray hits a
  ghost's colliders, so the preview climbs on top of itself), `PlacePiece` prefix instantiates the
  vanilla prefab (see notes §7f 1.18/1.20). Runtime build-menu icons: render on black and on white
  and derive alpha from the difference (opaque shaders don't write alpha); force LOD 0.
- Ghost positioning *(verified)*: a new ghost starts at the player's feet; `UpdatePlacementGhost`
  puts it on the hit point only for clip pieces (`m_groundPiece`/`m_clipGround` on terrain, or
  `m_clipEverything`), otherwise it lines up enabled colliders with the hit point. A collider-less
  ghost needs `m_clipEverything = true` or it never moves.

## Player side

- Stamina per strike: `Player.GetBuildStamina()` = attack stamina × home-item modifiers and
  status effects, minus 0.5 × skill factor of the build table's skill (none for the hoe). Called
  on each placement from `UpdatePlacement`.
- Sprint: `Player.CheckRun(Vector3 moveDir, float dt)` is a protected override; a prefix
  returning false with `__result = false` stops sprinting.
- Scroll in build mode: `GameCamera.UpdateCamera` changes `m_distance` (zoom) and
  `Player.UpdatePlacement` changes `m_placeRotation`. To use the wheel for something else, save
  them in a prefix and put them back in a postfix while your key is held.
- Ghost: `Player.UpdatePlacementGhost(bool)`; the hoe ghost shows its area through a
  `_GhostOnly` child, `Projector.orthographicSize` and `CircleProjector.m_radius`. Projector
  needs the full Unity module references (SnakeWood's `AddGameReferences` csproj target).

## Used by

- SnakeSkills `Landscaping.cs` (1.2): bigger brush by skill, less stamina, bump flattening, cart
  slope easing, no sprint; `RampTools.cs` (1.14): Ctrl incline, Alt ramp blueprints. See notes §7f.
- SnakeSmarterHoe (notes §7b): its own brush tools; SnakeSkills leaves the hoe alone when it is
  loaded.
