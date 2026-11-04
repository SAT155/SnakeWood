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
