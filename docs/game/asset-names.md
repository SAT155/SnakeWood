# Vanilla effect and sound prefab names

Found 2026-10-24 by the Decompile workflow's asset search (Valheim with Deep North). Names only:
none of these has been seen or heard in-game by a Claude session unless marked. Spawn with
`Object.Instantiate(prefab, pos, rot)`; one that carries a `ZNetView` becomes a network object
(everyone, modded or not, sees or hears it at that spot). Find them with
`ZNetScene.instance.GetPrefab(name)`, or among loaded root objects
(`Resources.FindObjectsOfTypeAll<GameObject>()` with no parent and no valid scene) for effects
that aren't registered in `ZNetScene`.

Search regexes used: `^(fx_|vfx_)…(spirit|wisp|explo|demist|spark|light)` and
`^(sfx_|fx_|vfx_)…(charge|staff|warp|distort|portal|magic|hum|wisp|demist|eitr|spirit|seeker|cast|ready|glow)`.
Re-run with your own regex for other needs.

## Charging, casting, magic

- Charge: `fx_lightningstaff_charge`, `fx_JotunWitch_MagicBlast_Charge`,
  `fx_JotunWitch_LightningBolt_Charge`, `fx_charred_firestaff_chargeup`, `fx_ShieldCharge_1`…`_5`,
  `sfx_staff_lightning_charge`, `sfx_StaffLightning_charge`, `sfx_charred_mage_attack_charge`,
  `sfx_catapult_charge`, `sfx_writhan_death_charge`, `sfx_frozenking_charge_start`.
- Cast / fire: `vfx_lightningstaff_fire`, `sfx_staff_lightning_fire`, `sfx_firestaff_launch`,
  `sfx_staffspiritcaller_cast`, `sfx_staffspiritcaller_summon`, `sfx_stafffrostorbs_cast`,
  `sfx_stafforbofahri_cast`, `sfx_staff_elder_cast`, `sfx_staff_grenade_cast`,
  `sfx_staff_trollstav_cast`, `sfx_staffthunderblood_cast`, `sfx_icestaff_start`.
- Loops (hums): `sfx_stafffrostorbs_shield_loop`, `sfx_stafforbofahri_projectile_loop`,
  `sfx_glowworm_idle_loop`.
- Staff shield: `vfx_StaffShield`, `fx_StaffShield_Hit`, `fx_StaffShield_Break`.
- Eitr potion: `sfx_Potion_eitr_minor`, `vfx_Potion_eitr_minor`.
- Wisp / demister: `sfx_demister_start`, `vfx_pick_wisp`.
- Distortion: `vfx_Ashlands_HeatDistortion` (size unknown; probably meant for a large area).
- Portals: `fx_portal_connected`, `fx_portal_stone_connected`, `sfx_OpenPortal`, `vfx_Place_portal`.

## Hits and explosions

- `fx_summon_spirit_spawn`, `fx_chainlightning_hit`, `fx_chainlightning_spread`,
  `fx_lightningstaffprojectile_hit`, `fx_lightningweapon_hit`, `fx_redlightning_burst`,
  `vfx_HitSparks`, `fx_fireball_staff_explosion`, `fx_clusterbombstaff_hit`,
  `fx_dynamite_explosion`, `fx_siegebomb_explosion`, `fx_lavabomb_explosion`,
  `fx_Fader_CorpseExplosion`, `fx_FallenValkyrie_projectile_explosion`,
  `fx_JotunWitch_MagicBlast`, `fx_JotunWitch_LightningBolt_Explosion`, `vfx_GodExplosion`,
  `vfx_BombBlob_explode_{frost,lava,morkhalla,poison,poisonelite,tar}`, `fx_writhan_explosion`.
- Sounds: `sfx_stafforbofahri_explosion`, `sfx_staff_grenade_impact_big` / `_small`,
  `sfx_staff_trollstav_meteorite`, `sfx_staffthunderblood_thunder`.
- Lightning: `fx_Lightning`, `fx_Lightning_red`.

## Misc

- `fx_ItemSparkles`, `vfx_Firework_Sparkler`, `vfx_LightFoot`, `fx_charred_chestglow`,
  `fx_charred_eyeglow`, seeker sounds and effects (`sfx_seeker_*`, `fx_seeker_*`).

## Caution: effects with game logic

Some `fx_*` names are more than visuals. The North Wisp's first burst defaults
(`fx_summon_spirit_spawn`, `fx_chainlightning_hit`) are the main suspect for a crash to desktop
when it burst near enemies (2026-10-28, not confirmed). Before spawning a borrowed effect, check
its components: ValheimMapMarker skips any prefab with a MonoBehaviour other than `ZNetView`,
`ZSyncTransform`, `ZSFX`, `TimedDestruction`, `LightFlicker`, `LightLod`, `CamShaker`,
`EffectFade`, `Billboard`, `LineConnect`, `RandomAnimation`, or with a collider, and logs it.


## Trees, rocks, logs, creatures and foods (2026-11-04, SnakewaySurfer session)

GameObject names found by asset search (not all are `ZNetScene` prefabs; resolve with
`GetPrefab` and skip misses): trees `Beech1`, `Beech_small1/2`, `Oak1`, `Bush01`, `FirTree`,
`FirTree_small`, `FirTree_oldLog`, `FirTree_oldLog_deepnorth`, `Pinetree_01`, `Pinetree_Snow`,
`Pinetree_Snow_dead`, `SnowFirTree`, `SwampTree1`, `SwampTree2`, `SwampTree2_darkland`,
`Birch1/2`, `Birch1_aut/2_aut`, `YggaShoot1/2/3`, `YggaShoot_small1`, `AshlandsTree1/3/4/5/6`,
`AshlandsTreeStump1/2/3`, `stubbe`; rocks `Rock_3/4/7`, `rock1_mountain`, `rock2_mountain`,
`rock3_mountain`, `rock1_mistlands`, `rock2_heath`, `rock4_heath`, `rock4_forest`,
`rock4_coast`, `rock3_ice`; logs `SwampTree1_log`, `SwampTree2_log`, `PineTree_Snow_log_XL_half`
plus the `*_log_half` list in the main notes; `HelmetDverger`, `Coins`, `piece_sharpstakes`.

Deep North creature and food prefab names are still unknown, but their **localization keys**
are known, and `Character.m_name` / `ItemDrop.m_itemData.m_shared.m_name` hold them, so look
prefabs up by key: enemies `$enemy_frysling`, `jotun_warrior` (Krigen), `jotun_witch` (Hexen),
`kvastur`, `moose`, `seal`, `trollfrost` (Gammeltroll), `blobfrost`, `writhan`, `bjorn` (Bear),
`unbjorn` (Vile), `dvergr_deepnorth`, `goblin_deepnorth`, `elaking`, `barka`, `mistile`; foods
`$item_sealsoup`, `smokedmoosemeat`, `moose_meat_cooked`, `meatballsmashedpoteitr`,
`oatmeallingonberryjam` (Oatmeal), `blubber_cooked`, `feastdeepnorth`. Full lists: search
`^"(enemy_|item_)` in the Decompile workflow's `assets` input.

## Player and UI sounds (2026-11-04, SnakewaySurfer session)

Asset search `^sfx_…(jump|land|step|hit|pickup|ui|gui|dodge|levelup|swing|…)$` found, among
others: `sfx_jump`, `sfx_land`, `sfx_dodge`, `sfx_perfect_dodge`, `sfx_footstep`,
`sfx_footstep_run`, `sfx_footstep_mud_run`, `sfx_footstep_snow_run`, `sfx_footstep_ice_run`,
`sfx_pickup`, `sfx_pickable_pick`, `sfx_gui_button`, `sfx_levelup`, `sfx_eat`, `sfx_hit`,
`sfx_wood_hit`, `sfx_rock_hit`, `sfx_tree_fall`, `sfx_unarmed_swing`, `sfx_club_swing`,
`sfx_troll_attack_hit`, `sfx_troll_footstep`, plus `sfx_<creature>_death/_hit/_footstep` for
most creatures (`greyling`, `greydwarf`, `troll`, `draugr`, `wolf`, `goblin`, `seeker_brute`,
`charred`, `moose`, `jotunwarrior`, `kvastur`, `gameltroll`, ...).

## Landmarks, pickables, boss items (2026-11-04, SnakewaySurfer session)

Asset search for `Pickable_`, `Spawner_`, totems, nests, ruins and boss names found (GameObject
names; most are ZNetScene prefabs, `Spawner_*` are mostly invisible creature spawners):
creature homes `Spawner_GreydwarfNest`, `BonePileSpawner`, `BonePileSpawner_swamp`,
`Spawner_DraugrPile`, `VoltureNest`; fuling village pieces `goblin_totempole`, `goblin_banner`,
`goblin_fence`, `goblin_pole`, `goblin_trashpile`, `goblin_strawpile`, `goblin_bed`; Dvergr props
`dvergrprops_crate`, `_crate_long`, `_barrel`, `_lantern`, `_lantern_standing`, `_banner`,
`_table`, `_chair`, `_shelf`, `_pickaxe`, `_wood_*`; pickables `Pickable_Dandelion`,
`_Mushroom`, `_Mushroom_yellow`, `_Mushroom_blue`, `_Mushroom_Magecap`, `_Mushroom_JotunPuffs`,
`_Thistle`, `_Flint`, `_Stone`, `_Branch`, `_Branch_Snow`, `_SeedCarrot`, `_SeedTurnip`,
`_SeedOnion`, `_Barley_Wild`, `_Flax_Wild`, `_Obsidian`, `_DragonEgg`, `_RoyalJelly`,
`_DvergrLantern`, `_Tar`, `_Tin`, `_SurtlingCoreStand`, `_Charredskull`, `_Ashstone`,
`_SmokePuff`, `_Snowball`, `_Fiddlehead`, `_VoltureEgg`; statues `StatueDeer`, `StatueSeed`,
`Statue_Thor`, `Statue_Freya` (with `_broken` variants); ruins `Ruin1`–`3`, `SwampRuin1`–`2`,
`StoneTowerRuins03`–`10`, `CharredRuins*`. Weapons/items: `Thunderstone`, `SurtlingCore`,
`MaceIron`, `BoltIron`, `BoltBone`, `BoltBlackmetal`, `BoltCarapace`, `ArrowFrost`, `BombLava`,
`BombBile`. See [world-progress-keys.md](world-progress-keys.md) for boss and summoning names.


## Boss effects (2026-11-05, SnakewaySurfer session)

- Lightning and storm: `fx_Lightning`, `lightningAOE`, `sfx_thunder`, `sfx_distant thunder`.
- Eikthyr: `fx_eikthyr_stomp`, `fx_eikthyr_forwardshockwave`, `sfx_eikthyr_attack`.
- The Elder: `TentaRoot` (the vine creature), `fx_gdking_rootspawn`, `vfx_gdking_stomp`,
  `sfx_tentaroot_attack`, `sfx_gdking_stomp`, `sfx_gdking_shoot_trigger`, `gd_king_rootspawn`,
  `gdking_root_projectile`.
- Bonemass: `fx_Bonemass_aoe_start`, `sfx_Bonemass_throw_trigger`, `sfx_Bonemass_punch_start`;
  attacks punch, aoe, spawn.
- Yagluth (GoblinKing) attacks: nova, taunt, beam (`projectile_beam`), cast1 (`spawn_meteors`).
- Moder (Dragon) attacks: `attack_breath`, `attack_iceball`.
- Environments: `Eikthyr`, `ThunderStorm`, `GDKing`, `DeepForest Mist`, `Bonemass`,
  `SwampRain`, `Moder`, `SnowStorm`, `GoblinKing`, `Heath clear`, `Queen`,
  `Mistlands_thunder`.
