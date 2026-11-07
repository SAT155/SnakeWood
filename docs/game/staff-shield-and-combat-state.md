# Staff of Protection bubble and "in combat"

Read from: Valheim with Deep North (Nov 2026), decompiled 2026-11-07 by the SnakeSkills bubble
shield session. All *(verified)* by decompile or asset search.

## The bubble (`SE_Shield : StatusEffect`)

- Item prefab `StaffShield` ("Staff of Protection"; description: "a slight blood offering", so the
  cast also costs health). Cast effect prefab `staff_shield_aoe`; visuals `vfx_StaffShield`,
  `fx_StaffShield_Hit`, `fx_StaffShield_Break`. Localization: `se_shield` "Magic barrier",
  `se_shield_ttl`, `se_shield_damage`.
- Fields: `m_absorbDamage` (100), `m_absorbDamageWorldLevel`, `m_absorbDamagePerSkillLevel`,
  `m_ttlPerItemLevel`, `m_levelUpSkillOnBreak` + `m_levelUpSkillFactor`, private `m_totalAbsorbDamage`
  and `m_damage`.
- `SetLevel(itemLevel, skillLevel)`: `m_ttl = m_ttlPerItemLevel * itemLevel` when > 0 (duration grows
  with the staff's quality), absorb = base + per-skill × skill (+ world level bonus).
- `OnDamaged` adds the hit's total to `m_damage` and zeroes the hit (`ApplyModifier(0)`). `IsDone()`
  ends the effect early once `m_damage > m_totalAbsorbDamage` (break effect, raises the skill), else
  the base timeout.
- Time left: `StatusEffect.GetRemaningTime()` (sic) = `m_ttl - m_time`. Find the bubble with
  `player.GetSEMan().GetStatusEffects()` and `se is SE_Shield` (no name hash needed).
- Casting it from code: with the staff as `GetCurrentWeapon()`, `Humanoid.StartAttack(null, false)`
  runs the normal cast (`Attack.Start` checks stamina, `TryUseEitr`, then animation); returns false
  when it can't start.

## Is the player "in combat"?

There is no public player in-combat flag *(verified: no `InCombat` on Player/Character/Humanoid)*.
What exists:

- `Humanoid.m_lastCombatTimer` (protected, starts 999): set to 0 in `StartAttack` when an attack
  starts (any attack, incl. tool swings), += dt in `Humanoid.CustomFixedUpdate`. `Player.CanSwitchPVP()`
  is `m_lastCombatTimer > 10f`.
- `MusicMan` has a private `m_combatTimer` (`ResetCombatTimer()` sets `m_combatMusicTimeout` 4 s;
  private `InCombat()`); Player calls `ResetCombatTimer` in some hit path.
- `BaseAI.IsAlerted()` (`m_alerted`); `Character.GetBaseAI()`; BaseAI has a static helper that
  returns true when any instance within `range` of a point is alerted (name not captured; line ~1500).
- Hits on the player: patch `Player.OnDamaged(HitData)` (protected override) to time-stamp them.

SnakeSkills 1.27 uses: own attack < 10 s ago, hit < 10 s ago, or an alerted `BaseAI.IsEnemy`
character within 30 m (`Character.GetAllCharacters()`).
