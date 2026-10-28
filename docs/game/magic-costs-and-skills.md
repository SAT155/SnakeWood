# Stamina, eitr, skills and damage

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-10-24 by the ValheimMapMarker
North Wisp session. All *(verified)* by decompile unless noted.

## Stamina

- `Player.UseStamina(v)` multiplies by `Game.m_staminaRate` and resets the regen delay
  (`m_staminaRegenDelay`, 1 s). `HaveStamina(amount)` is `m_stamina > amount` (no rate applied).
- `Hud.instance.StaminaBarEmptyFlash()` is the "not enough stamina" flash.
- `Attack.m_attackStamina` defaults to 20 in code; real values are on item prefabs (read them at
  runtime, e.g. `ObjectDB.GetItemPrefab("AtgeirBronze")` → `m_shared.m_secondaryAttack
  .m_attackStamina` for the atgeir sweep).
- `Attack.GetAttackStamina()`: base × (1 + equipment attack-stamina modifier), status-effect
  modifiers, then **− 33 % × skill factor** of the weapon's skill, minus any per-missing-HP return.

## Eitr

- `Character.TryUseEitr(cost)`: if `GetMaxEitr() == 0` shows `$hud_eitrrequired` (centre
  message) and fails; if `!HaveEitr(cost + 0.1f)` flashes `Hud.instance.EitrBarEmptyFlash()` and
  fails; otherwise uses it.
- `Attack.GetAttackEitr(character, weapon)` = `m_attackEitr − m_attackEitr × 0.33 × skill factor`
  (a third off at skill 100), the same discount as stamina.
- Player overrides `UseEitr`, `HaveEitr`, `GetMaxEitr` (Character's defaults are no-ops / 0).

## Skills

- `Skills.SkillType`: `ElementalMagic = 9`, `BloodMagic = 10` (plus the weapon skills).
- `Player.RaiseSkill(skill, value = 1)` applies status-effect multipliers
  (`SEMan.ModifyRaiseSkill`) and raises it.
- Attacks raise `m_weapon.m_shared.m_skillType` by `Attack.m_raiseSkillAmount` (default 1):
  melee and area attacks when they hit something of `m_skillHitType` (characters by default);
  projectiles carry `HitData.m_skillRaiseAmount` and raise when they hit. Staff magic therefore
  trains on hits, not on casts.
- `GetSkillFactor(skill)` is 0..1 (level / 100).

## Damage and spirit

- `Character.Damage(hit)` sets the weak spot from `hit.m_hitCollider` and sends `RPC_Damage` to
  the creature's owner, so any client can damage any creature.
- In `RPC_Damage`: a blockable hit on a blocking character goes through `BlockAttack`; then
  modifiers apply; fire, spirit and poison are taken out of the instant damage and applied as
  damage-over-time (`AddFireDamage`, `AddSpiritDamage`, `AddPoisonDamage`), frost and lightning
  likewise as status effects. Spirit only hurts creatures whose spirit modifier isn't Immune
  (in practice mostly undead; prefab values, unverified per creature).
- `HitData`: `m_damage` (`DamageTypes`: `m_blunt`, `m_slash`, `m_pierce`, `m_chop`, `m_pickaxe`,
  `m_fire`, `m_frost`, `m_lightning`, `m_poison`, `m_spirit`, `m_nonPlayer`), `m_point`, `m_dir`,
  `m_hitCollider`, `m_ranged`, `m_pushForce`, `m_skillRaiseAmount`, `SetAttacker(character)`.
- `Character.GetAllCharacters()` is the static list of all loaded characters (copy it before
  damaging in a loop; a death removes from it). `Character.m_collider` is the capsule.
- `BaseAI.IsEnemy(a, b)` for "should this hurt them" (players are never enemies of each other or
  of tames).
