# Game-system topic notes

Small notes on how parts of Valheim work, written by Claude sessions from what they decompiled
(the Decompile workflow, see the main notes §3). One file per system. They record class and
method names, key fields, how the pieces fit, and gotchas, not raw decompiled code: re-run the
Decompile workflow when you need the code itself. Linked from the main notes
(`docs/VALHEIM_MODDING_NOTES_v*.md`, §5).

Each note says which game version it was read from. A fact marked *(verified)* was read in the
decompile or seen in-game; anything else is a best guess. If a game update changes something,
fix the note and say so in its changelog line.

| File | System |
| --- | --- |
| [mist-and-demisters.md](mist-and-demisters.md) | Mistlands mist (`ParticleMist`), `Demister`, the Wisplight (`SE_Demister`, `demister_ball`) |
| [blocking-and-left-hand.md](blocking-and-left-hand.md) | Blocking, shields and other left-hand items, `VisEquipment` hand models, hotbar use |
| [magic-costs-and-skills.md](magic-costs-and-skills.md) | Stamina and eitr costs, max eitr (food total) and eitr regen, trinkets (slot, prefab names, adrenaline SE), skill discounts, skill raises, spirit damage, `HitData` |
| [asset-names.md](asset-names.md) | Vanilla effect and sound prefab names found by asset search, for borrowing |
| [attacks-and-throws.md](attacks-and-throws.md) | How an attack runs (`StartAttack` → `Attack.Start` → `OnAttackTrigger`), thrown items without a projectile, holding the player still (`SetControls`), equip duration |
| [audio-zsfx.md](audio-zsfx.md) | `ZSFX` volume, making sounds positional, which sounds not to borrow |
| [network-objects.md](network-objects.md) | `ZNetView` registration, destroying network objects safely, the `ZNetScene.RemoveObjects` error flood |
| [hover-and-portal-triggers.md](hover-and-portal-triggers.md) | `Player.FindHoverObject` reach and first-hit rule, portal triggers (`TeleportWorldTrigger`, `TeleportWorld.Teleport`), saddle and rug prefab names |
| [player-save-and-custom-skills.md](player-save-and-custom-skills.md) | Why custom skills vanish after an unmodded load (`Skills.Load`/`IsSkillValid`), `Player.m_customData` as storage vanilla keeps, SkillManager skill ids |
| [fishing.md](fishing.md) | How vanilla raises Fishing (`FishingFloat.FixedUpdate`, `Catch`), fish prefabs and stars, `RaiseSkill` gains at most one level per call |
| [terrain-and-hoe.md](terrain-and-hoe.md) | Hoe strikes (`TerrainOp`, `TerrainComp`), zone ownership for edits, writing terrain heights, build stamina, sprint, hoe ghost |
| [projectiles-and-rocks.md](projectiles-and-rocks.md) | Firing projectiles (`FireProjectileBurst`, `Projectile.Setup`), hits and the `m_onHit` hook, `DoAOE` one hit per object, damage totals, `MineRock5`/`MineRock`/`Destructible` damage, Staff of Embers prefab |
| [build-menu-and-pieces.md](build-menu-and-pieces.md) | Piece tables and the build menu (`UpdateKnownRecipesList`, `UpdateAvailable`, known recipes), selecting a piece from code (`SetSelectedPiece`), stub pieces, Jotunn-free UI widgets |
| [player-anim-input-visuals.md](player-anim-input-visuals.md) | Synced animation floats (`UpdateWalking`, running in place), locking input (`TakeInput`, `SetControls`, Console/Menu), helmet override via `SetHelmetItem`, `Inventory.AddItem` overloads, `Skill.Raise` one level per call, a private camera scene |
| [camera-weather-terrain-look.md](camera-weather-terrain-look.md) | Biome weather (`EnvMan.m_biomes`, fog mode), drawing a private scene through the game camera, the terrain shader's biome vertex colours and paint mask |
| [world-progress-keys.md](world-progress-keys.md) | Boss kill keys (`defeated_*`, `GetGlobalKey`, player-based keys), summoning items and amounts, world name/UID, per-world data in `m_customData` |
| [locations-and-custom-items.md](locations-and-custom-items.md) | Location prefabs as soft references (`ZoneLocation.m_prefab.Load/Asset/Release`), boss location names, adding an item without Jotunn (`ObjectDB.UpdateRegisters`, `ZNetScene.m_namedPrefabs`), trinket full-adrenaline status effects |
| [summons-and-dead-raiser.md](summons-and-dead-raiser.md) | Dead Raiser cast and eitr, `SpawnAbility` (summon flow, `SetLevel` hook), `GiveDefaultItems` on every load, friendly/hostile skeleton prefabs and their weapons with damage |
| [staff-shield-and-combat-state.md](staff-shield-and-combat-state.md) | Staff of Protection bubble (`SE_Shield`: duration, absorb, time left, casting from code), what "in combat" can mean (`m_lastCombatTimer`, MusicMan, `BaseAI.IsAlerted`) |
