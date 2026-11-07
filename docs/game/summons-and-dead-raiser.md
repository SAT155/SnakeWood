# Summons: the Dead Raiser and friendly skeletons

Read 2026-11-07 from Valheim with Deep North (Unity 6000.0.75f1) by the SnakeSkills Dead Raiser
session: Decompile of `SpawnAbility`, `Attack` (eitr lines), `Humanoid` (default items),
`Tameable` (unsummon), plus the **prefab dump** (Decompile workflow `prefabs` input, main notes §3)
of `StaffSkeleton`, `staff_skeleton_spawn`, `Skeleton_Friendly` and the skeleton weapons.
*(verified)* = read in the decompile or the dump; not yet confirmed in-game.

## The cast *(verified)*

- `StaffSkeleton` (Dead Raiser, `$item_staffskeleton`, skill `BloodMagic` = 10, max quality 4):
  `m_attack` is `m_attackType` Projectile (2), animation `staff_summon`, **`m_attackEitr` 100**,
  `m_attackHealthPercentage` 40 (40% of current health), `m_attackProjectile` =
  `staff_skeleton_spawn`. **No secondary attack** (`m_secondaryAttack.m_attackAnimation` empty), so
  middle click is free. `m_blockPower` 48 (right click blocks).
- Eitr: `Attack.GetAttackEitr(Character, ItemData)` = `weapon.m_shared.m_attack.m_attackEitr ×
  (1 − 0.33 × skill factor)`; it always reads the **primary** attack's eitr, also used by
  `Attack.Start` (`TryUseEitr`) and the draw/hold `Update`. A postfix on that overload changes the
  real cost. The health cost is `GetAttackHealth` (same 0.33 discount).
- `staff_skeleton_spawn` is a `SpawnAbility` (implements `IProjectile`): `Setup(owner, ...)` starts
  the `Spawn` coroutine. Values: `m_spawnPrefab` [`Skeleton_Friendly`], min/max to spawn 1,
  `m_maxSpawned` 10 (per prefab, via `SpawnSystem.GetNrOfInstances`), `m_preSpawnDelay` 2.5 s,
  `m_targetType` RandomPathfindablePosition, `m_commandOnSpawn` (follows the caster),
  `m_wakeUpAnimation`, `m_copySkill` BloodMagic with `m_copySkillToRandomFactor` 0.02 (ZDO
  `s_randomSkillFactor` = 1 + level × 0.02: the skeleton hits up to 3× harder at 100),
  `m_setMaxInstancesFromWeaponLevel` (ZDO `s_maxInstances` = staff quality; `Tameable`
  unsummons the oldest beyond it), level-up settings all `m_setLevel` 1.
- In `Spawn`, in one frame: `Instantiate(prefab)`, then (owner has skills) ZDO random skill
  factor, then `Character.SetLevel(...)` from `m_levelUpSettings`, then `Tameable.Command`.
  The skeleton's `Start` runs later, so **`SetLevel` is a hook to mark a fresh summon** before it
  gets its weapons. The spawner is the caster's client, so the caster owns the new ZDO.

## Skeleton weapons *(verified)*

- `Humanoid.Start` calls `GiveDefaultItems()` for every non-player, **on every client, every time
  the creature loads**; not saved. `GiveDefaultItems` gives `m_defaultItems`, then seeds
  `Random.InitState(m_seed)` (ZDO `s_seed`, from the ZDO uid on first `Awake`) and picks one
  `m_randomShield`, one `m_randomWeapon`, one `m_randomSets` entry, then `m_randomItems`.
  Null entries mean "none". The arrays are per instance, so a `GiveDefaultItems` prefix can narrow
  them for one creature; store the choice on its ZDO so every client and every reload agrees.
- `Skeleton_Friendly` (`$enemy_skeleton_summoned`): `m_randomWeapon` = 4 × `skeleton_sword2`,
  1 × `skeleton_bow2`; `m_randomShield` = `ShieldWood`, `ShieldBronzeBuckler`, null, null.
- Other skeletons, same rig (`$enemy_skeleton`, can use each other's weapons): `Skeleton_NoArcher`
  (`skeleton_sword`), `Skeleton_Meadows` (sword/bow `_meadows`), `Skeleton_Mountains`
  (`_mountains`), `Skeleton_Swamps`, `Skeleton_DeepNorth` (`skeleton_mace_DeepNorth` + wood or
  bronze shield). `Skeleton_Poison` (rancid remains) has `skeleton_mace`; `Skeleton_Hildir`
  (Brenna) `skeleton_sword_hildir` + `skeleton_hildir_firenova`.
- Weapon damage (prefab values): `skeleton_sword` 25 slash, `skeleton_sword_meadows` 15,
  `skeleton_sword2` 50, `_swamps` 48 + 10 chop, `_mountains` 60 + 15 chop,
  `skeleton_sword_hildir` 60 slash + 20 fire; bows (`attack_bow`, `draugr_bow_projectile`, range 20)
  `skeleton_bow` 20 pierce, `_swamps` 55, `_mountains` 60; maces (`attack_mace`, range 2.2, force 80)
  `skeleton_mace` 20 blunt + 30 poison, `skeleton_mace_DeepNorth` 80 blunt + 10 frost. Swords use
  `attack`, range 1.6. Item names are placeholders ("Dragur axe", "Bow"); they may have no icon.

## Other names

- Effects: `fx_summon_skeleton`, `fx_summon_skeleton_spawn`, `sfx_skull_summon_skeleton`,
  `fx_skeleton_pet`. Message `$hud_maxsummonsreached`. Trophy `TrophySkeleton`.
- `Tameable.m_unsummonDistance` / `UnSummon()` → `RPC_UnSummon` (effect, owner destroys).
