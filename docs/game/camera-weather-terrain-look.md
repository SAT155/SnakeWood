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
