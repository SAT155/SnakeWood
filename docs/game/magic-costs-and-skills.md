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
- **Max eitr is food only** (decompiled 2026-10-30, Pulsating Earrings session): `Player.UpdateFood`
  (about once a second, or forced when eating) calls private
  `GetTotalFoodValue(out float hp, out float stamina, out float eitr)` (hp starts at `m_baseHP`,
  stamina at `m_baseStamina`, eitr at **0**, then adds each `Food.m_eitr`) and passes the totals to
  `SetMaxHealth`/`SetMaxStamina`/`SetMaxEitr(eitr, flashBar)`. A postfix with `ref float eitr`
  raises max eitr like food does (bar flashes up) and drops it within a second when the condition
  ends. `SetMaxEitr` clamps current `m_eitr`.
- **Eitr regen** in `Player.UpdateStats`: `(m_eiterRegen + (1 − eitr/max) × m_eiterRegen) ×
  blocking factor`, × a multiplier that starts at 1 and goes through
  `SEMan.ModifyEitrRegen(ref float eitrMultiplier)` (each SE's `ModifyEitrRegen`; `SE_Stats` adds
  `m_eitrRegenMultiplier − 1` when > 1, else multiplies). Waits `m_eitrRegenDelay` (1 s) after use.
- `Player.GetEquipmentEitrRegenModifier()` sums `m_shared.m_eitrRegenModifier` over chest, legs,
  helmet, shoulder, both hands and utility: **not the trinket slot**, so setting that field on a
  trinket does nothing.

## Trinkets (Deep North)

- Slot: `Humanoid.m_trinketItem` (protected; publicized), item type
  `ItemDrop.ItemData.ItemType.Trinket`. Its `m_equipStatusEffect` is applied like other gear
  (`UpdateEquipmentStatusEffects`); the trinket's own effect is `m_shared.m_fullAdrenalineSE`,
  fired when adrenaline reaches `m_shared.m_maxAdrenaline` (`Player.GetMaxAdrenaline` adds it).
- Prefab names (from the ViableTrinkets mod, not asset-searched): `TrinketBronzeHealth`,
  `TrinketBronzeStamina`, `TrinketIronHealth`, `TrinketIronStamina`, `TrinketChitinSwim`,
  `TrinketSilverDamage`, `TrinketSilverResist`, `TrinketBlackDamageHealth`, `TrinketBlackStamina`,
  **`TrinketCarapaceEitr` (Pulsating Earrings; adrenaline SE = eitr regen)**,
  `TrinketScaleStaminaDamage`, `TrinketFlametalEitr` (Jormundling), `TrinketFlametalStaminaHealth`
  (Brimstone), `TrinketBloodGoldStamina` (Witch Crown), `TrinketBloodGoldHealth` (Neckstabber).
- Test "is X worn": `player.m_trinketItem?.m_dropPrefab?.name == "<prefab>"`.
- ViableTrinkets (CoachPotatoes, GUID `devon752.ViableTrinkets`) edits each trinket's
  `m_fullAdrenalineSE` (an `SE_Stats`) and `m_maxAdrenaline` in `ObjectDB.Awake`/`CopyOtherDB`
  postfixes; it never adds a permanent bonus, so permanent effects patched elsewhere stack with it.
- Item descriptions: prefab `m_shared` is shared by reference with items in inventories, so
  appending to `m_shared.m_description` at `ObjectDB.Awake` changes every tooltip. Keep the
  original per `SharedData` so config changes don't stack lines.

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
