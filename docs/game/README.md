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
