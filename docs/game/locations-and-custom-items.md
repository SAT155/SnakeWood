# Location prefabs and custom items

Read from the game's code with the Decompile workflow on 2026-11-04 (Valheim 0.221.x, the
dedicated server's `assembly_valheim.dll`), for SnakewaySurfer 1.10. *(verified)* means it was
seen in the decompiled code. Nothing here has been tried in-game yet.

## Location prefabs are soft references

- `ZoneSystem.instance.m_locations` is a `List<ZoneSystem.ZoneLocation>` (a public nested class).
  Each has `m_prefabName` and `m_prefab`, a `SoftReference<GameObject>` from
  `SoftReferenceableAssets.dll` (ilspy can't decompile it from `assembly_valheim.dll`).
  *(verified)*
- How the game uses it *(verified)*: `m_prefab.IsValid`, `m_prefab.Name`, `m_prefab.Load()`
  (synchronous: `SpawnLocation` reads `m_prefab.Asset` on the next line), `m_prefab.LoadAsync(cb)`,
  `m_prefab.Asset` and `m_prefab.Release()`. Loads are counted, so pair each `Load` with a
  `Release`. Copies keep using the bundle's meshes, so release only when you're done with them.
- `SpawnLocation` moves the asset itself to the origin while it spawns. Copy it, don't edit it.
- Boss location prefab names (asset search) *(verified)*: `Eikthyrnir`, `GDKing`, `Bonemass`,
  `Dragonqueen`, `GoblinKing`, `Mistlands_DvergrBossEntrance1`, `StartTemple`,
  `Vendor_BlackForest`, `BogWitch_Camp`, `Hildir_camp`, `Hildir_cave`, `Hildir_crypt`,
  `Hildir_plainsfortress`.
- A picture of one: `Instantiate` it under an inactive parent, so `Awake` never runs and no
  `ZNetView` registers, then strip the components (as SnakewaySurfer's `Pictures.MakePicture`
  does). A location holds its pieces as child objects, so the copy shows them.

## Altar summoning effects (SnakewaySurfer 1.12)

`OfferingBowl` on a boss location (private fields, use reflection): `m_spawnBossStartEffects`
(sparkles and sound as the items are given), `m_spawnBossDoneffects` (the game's spelling; as
the boss appears) and `m_spawnBossDelay` (float, seconds between them). Load the location's
soft reference first (above).

## Adding an item (no Jotunn)

- `ObjectDB.Awake` calls the private `UpdateRegisters()`, which rebuilds `m_itemByHash` (by
  prefab name hash) and `m_itemByData`. `CopyOtherDB(other)` copies `m_items`, `m_StatusEffects`
  and `m_terrainOps`, then calls `UpdateRegisters` again. *(verified)*
- To add one: patch both with a postfix, add your prefab to `m_items`, and call
  `UpdateRegisters` by reflection. Make the prefab by instantiating a vanilla item under an
  inactive `DontDestroyOnLoad` holder, then give it a `MemberwiseClone` of
  `m_itemData.m_shared`. The shared data is shared by reference with the original, so a plain
  edit would change the vanilla item too.
- For dropping it in the world, also add it to `ZNetScene.m_prefabs` and to the private
  `m_namedPrefabs` dictionary (key `name.GetStableHashCode()`). Players without the mod can't
  create it.
- Saves store the prefab name. The item loads only while it's registered, and vanilla drops
  unknown items from the bag.

## Trinkets and adrenaline

- Slot: `Humanoid.m_trinketItem` (protected). `ItemType.Trinket = 24`. The visual is set with
  `VisEquipment.SetTrinketItem(m_dropPrefab.name)`. *(verified)*
- Equipment modifiers, including the trinket's, come from `s_equipmentModifierSourceFields`
  read off `m_shared`. Movement speed is `m_movementModifier`, and adrenaline is
  `m_maxAdrenaline`. *(verified)*
- When adrenaline fills, `Player.AddAdrenaline` checks each equipped item's
  `m_shared.m_fullAdrenalineSE`. If it's already active it calls `ResetTime()`, otherwise
  `GetSEMan().AddStatusEffect(se, resetTime: false, 0, 0f, -1)` with the object itself. So a
  status effect made at runtime works without registering it in `ObjectDB.m_StatusEffects`.
  *(verified)*
- `SE_Stats` *(verified)*:
  - `m_speedModifier` adds `baseSpeed * value` (half while swimming).
  - `m_staminaRegenMultiplier` above 1 is added as `value - 1`, below 1 it multiplies.
  - Also `m_adrenalineModifier` and `m_adrenalineUpFront`.
- Vanilla trinket prefabs (asset search): TrinketBronzeStamina, TrinketBronzeHealth,
  TrinketIronStamina, TrinketIronHealth, TrinketBlackStamina, TrinketBlackDamageHealth,
  TrinketSilverDamage, TrinketSilverResist, TrinketBloodGoldStamina, TrinketBloodGoldHealth,
  TrinketScaleStaminaDamage, TrinketChitinSwim, TrinketCarapaceEitr, TrinketFlametalEitr,
  TrinketFlametalStaminaHealth, TrinketDNGold, TrinketDNNornThread.
- Item icons sit in an atlas that scripts can't read. To make a tinted icon, `Graphics.Blit` the
  texture into a `RenderTexture`, then `ReadPixels` the sprite's `textureRect`.
