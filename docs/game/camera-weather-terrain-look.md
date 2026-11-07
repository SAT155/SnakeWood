# Camera, weather and terrain look

What makes the world look like Valheim, for mods that draw their own scenes (SnakewaySurfer
1.5.x, 2026-11-04). Read from the decompile of the current game; nothing here is verified
in-game yet unless marked.

## Weather and fog

- `EnvMan.m_biomes` is a list of `BiomeEnvSetup` (`m_biome`, `m_environments` of `EnvEntry`
  {`m_environment` name, `m_weight`, `m_env` → `EnvSetup`}). An `EnvSetup` has day/night pairs:
  `m_fogDensityDay`, `m_fogColorDay`, `m_ambColorDay`, `m_sunColorDay`, `m_lightIntensityDay`
  and the rest. The heaviest-weight entry is the biome's usual weather.
- `EnvMan` sets only `RenderSettings.fogColor` and `fogDensity`, never `fogMode`. Forcing
  ExponentialSquared on a custom camera gave no fog at all (probably a stripped shader variant):
  keep the world's fog mode.
- `EnvMan.m_dirLight` is the world sun. A private scene with its own light should turn it off in
  `OnPreCull` and back on after rendering.

## Game camera

- `GameCamera` (private `LateUpdate` → `UpdateCamera`) sets `m_camera.fieldOfView` and
  `m_skyCamera.fieldOfView`. `m_skyCamera` is a separate camera that draws the sky behind.
- The game's haze, bloom and colour grading are post effects on the main camera, so a separate
  camera doesn't get them. To draw a private scene through the game's camera: set its
  `cullingMask` to the scene's layer (and a 0 entry in `layerCullDistances` for it), then move it
  in a `GameCamera.LateUpdate` postfix (position, rotation, FOV, and the sky camera's FOV and
  rotation). Restore the mask, cull distances and background when done.

## Terrain shader

- Every `Heightmap` has `m_material`; `RebuildRenderMesh` makes `m_materialInstance = new
  Material(m_material)` and sets `_ClearedMaskTex` to its `m_paintMask` texture, plus a
  property block with `_depth` and `_LodHideDistance`. The mesh carries vertex colours
  (`SetColors`) and uv0 (`SetUVs(0)`) that address the paint mask.
- **The shader hides terrain near the player.** `_LodHideDistance` is set per renderer
  (property block). For distant LODs it is `GetLodHideDistance()`, NearSimulationDistance × the
  zone diagonal. Ground within that distance of the global `_NetRefPos` (the player) is clipped.
  A private scene straight above the player (SnakewaySurfer's stage, 4000 m up) falls inside it
  and draws cyan or nothing. Set `_LodHideDistance` to 0 on the material and in the block. Also
  give `_depth` a float array of 4 (`SetFloatArray`).
- The vertex colour picks the biome's ground textures: `public static Color32
  Heightmap.GetBiomeColor(Biome)`. Swamp (255,0,0,0), Mountain and DeepNorth (0,255,0,0),
  BlackForest (0,0,255,0), Plains (0,0,0,255), AshLands (255,0,0,255), Mistlands
  (0,0,255,255), Meadows and anything else 0. Blend two biome colours at a border.
- Paint mask channels: red dirt, green cultivated, blue paved (what the hoe paints;
  `m_paintMaskNothing` is untouched). A custom mesh can draw a path by giving its material a
  small mask texture and uvs across the path.
- Copying a build piece's shader onto a generated mesh drew nothing (or white); the Standard
  shader with the piece's main texture works. Generated meshes: don't add back faces that share
  vertices with the front (the normals cancel and it goes black).

## Real terrain shape, paint mask and grass (SnakewaySurfer 1.11)

These were read from the decompiled game code on 2026-11-04. *(verified)* means it was seen in
that code. None of it has been checked in-game yet.

### Terrain shape

- `WorldGenerator.instance.GetBiomeHeight(biome, wx, wy, out Color mask)` *(verified)*:
  - Returns the height in metres: the biome formula's value times `GetHeightMultiplier()`, which
    is 200.
  - Sea level is 30 m. River beds come out at 0.128–0.14, so about 26–28 m.
  - Each biome has its own private formula: `GetMeadowsHeight`, `GetForestHeight`,
    `GetMarshHeight`, `GetSnowMountainHeight`, `GetPlainsHeight`, `GetMistlandsHeight`, and the
    Ashlands and Deep North ones. Each builds on `GetBaseHeight` and `AddRivers`.
  - `mask` is `Color.black` (alpha 1) except in the Mistlands (alpha from the hill factor),
    Ashlands (alpha is lava) and Deep North.
- `GetHeight(wx, wy)` is `GetBiomeHeight(GetBiome(wx, wy), …)`. Calling `GetBiomeHeight` with a
  fixed biome forces that biome's look, even at a border. *(verified)*

### Paint mask

- The Heightmap paint mask starts as `HeightmapBuilder`'s `m_baseMask`, which holds the
  generator's mask, before terrain mods. *(verified)*
- Its alpha is the vegetation mask: `GetVegetationMask`, with `m_paintMaskClearVegetation` =
  (0,0,0,0) and `m_paintMaskNothing` = (0,0,0,1). In the Ashlands the alpha is lava, and in the
  Mistlands it's the cliff and rock look. *(verified)*
- So a custom mesh with a constant mask alpha of 1 loses the Mistlands textures. SnakewaySurfer
  gives each tile its own small mask texture through a `MaterialPropertyBlock`, and sets
  `_WaterLevel` per tile.

### Grass

- `ClutterSystem.m_clutter` holds the `Clutter` entries *(verified)*. The fields:
  - `m_biome` (a flags field), `m_instanced`, `m_prefab`, `m_amount` per 8 m patch.
  - Quality scales the amount: Low is a quarter, Med a half, High all of it, then times
    `m_amountScale`.
  - `m_onUncleared` / `m_onCleared`, `m_min/maxVegetation`, `m_scaleMin/Max`,
    `m_min/maxTilt`, `m_min/maxAlt` (measured above `m_waterLevel`), `m_snapToWater`,
    `m_terrainTilt`, `m_randomOffset`, `m_min/maxOceanDepth`.
  - `m_inForest` with `m_forestTresholdMin/Max`, using `WorldGenerator.GetForestFactor`.
  - Fractal placement: `Utils.Fbm(pos * 0.01 * m_fractalScale + (m_fractalOffset, 0, 0), 3,
    1.6, 0.7)` checked against `m_fractalTresholdMin/Max`.
- `InstanceRenderer` keeps `m_mesh`, `m_material` and `m_scale`, plus up to 1023 matrices. It
  draws with `Graphics.DrawMeshInstanced(…, layer "InstanceRenderer")` for every camera, so a
  private scene has to draw the mesh itself on its own layer. *(verified)*

## Weather from an environment (SnakewaySurfer 1.12)

- `EnvMan.instance.GetEnv(name)` gives an `EnvSetup`. Day values: `m_fogDensityDay`,
  `m_fogColorDay`, `m_ambColorDay`, `m_sunColorDay`, `m_lightIntensityDay`. Rain, snow and ash
  are `m_psystems` (GameObject[]); `m_psystemsOutsideOnly`; `m_envObject` holds extras such as
  the `Thunder` component.
- `Thunder`: `m_strikeIntervalMin/Max`, `m_thunderDelayMin/Max`, `m_flashEffect` and
  `m_thunderEffect` (EffectLists). There is no `LightningFlash` class.
- A boss location's weather: `EnvZone.m_environment` on the location prefab.
- Copies of `m_psystems` parented to your own camera work as private weather. Turn their
  particle collision off, and put them on your layer.
