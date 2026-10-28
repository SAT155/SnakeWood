# Attacks, throws and holding the player still

Read 2026-10-25 from the Decompile workflow (Valheim with Deep North), for the ValheimMapMarker
North Wisp. *(verified)* = read in the decompile; nothing here has been tried in-game yet.

## How an attack runs *(verified)*

1. `Player.PlayerAttackInput` (FixedUpdate): a press of the secondary attack sets
   `m_queuedSecondAttackTimer = 0.5f`; then `if ((m_queuedSecondAttackTimer > 0f ||
   m_secondaryAttackHold) && StartAttack(null, secondaryAttack: true))`. So `StartAttack` is
   **called every fixed frame while the button is held**; a press is `m_queuedSecondAttackTimer > 0`.
   The primary attack works the same way with `m_queuedAttackTimer` / `m_attackHold`.
2. `Humanoid.StartAttack(Character target, bool secondaryAttack)` (public override): refuses
   if already attacking (unless a chain is queued), dodging, can't move, knocked back,
   staggering or in a minor action. Takes `GetCurrentWeapon()` (the right-hand item), needs
   `HaveSecondaryAttack()` / `HavePrimaryAttack()`, **clones** `m_shared.m_secondaryAttack` or
   `m_attack`, and calls `attack.Start(...)`. On success sets `m_currentAttack` and
   `m_currentAttackIsSecondary`.
3. `Attack.Start(...)`: `m_attackAnimation == ""` → false. Checks `m_requiresReload`,
   `m_cantUseInDungeon`, stamina (`GetAttackStamina()`, needs +0.1 spare, else
   `Hud.StaminaBarEmptyFlash`) and eitr (`character.TryUseEitr(GetAttackEitr())`), then plays the
   animation (synced to everyone by `ZSyncAnimation`).
4. The animation's event calls `Attack.OnAttackTrigger()`: `UseAmmo(out ...)` (an empty
   `m_ammoType` needs none) and not staggering, then by `m_attackType`: Horizontal/Vertical →
   `DoMeleeAttack`, Area → `DoAreaAttack`, **Projectile → `ProjectileAttackTriggered()`**
   (private), None → `DoNonAttack`.
5. `ProjectileAttackTriggered`: `GetProjectileSpawnPoint(out spawnPoint, out aimDir)`, trigger
   effects, durability, then `FireProjectileBurst()` (or bursts over time). With
   `m_perBurstResourceUsage` the stamina/eitr is paid per burst there instead.

`Attack.Stop()` ends it (triggers `attack_abort` for looping attacks, `detach` if attached;
`m_attackKillsSelf` kills the attacker). `CanStartChainAttack()` needs `m_nextAttackChainLevel > 0`.

## Custom thrown things without a projectile

Clone a vanilla item's `Attack` (`attack.Clone()`; e.g. `BombOoze`'s primary for a bomb throw,
`SpearFlint`'s secondary for a spear throw, both exist as item prefabs), set
`m_attackType = Projectile`, `m_consumeItem = false`, `m_attackProjectile = null`,
`m_projectileBursts = 1`, then **prefix `Attack.ProjectileAttackTriggered`** for your weapon
and spawn your own thing there, returning false (on every client, since the projectile is
null). You keep the vanilla animation, stamina/eitr payment, skill discount (from the weapon's
`m_skillType`) and what unmodded players see. To start an attack yourself (e.g. after a charge),
call `player.StartAttack(null, true)` with a flag your own `StartAttack` prefix lets through.

## Holding the player still *(verified signature)*

`Player.SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool
secondaryAttackHold, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun,
bool dodge = false)` is called by `PlayerController.FixedUpdate` with the input each frame. A
Harmony prefix with `ref` parameters (`ref Vector3 movedir, ref bool jump, ...`) can zero them;
camera turning still works. `PlayerController` reads `ZInput.GetButton("SecondaryAttack")` (middle
mouse by default) and `"JoySecondaryAttack"`.

## Equipping *(verified)*

`Player.EquipItem`: `m_shared.m_equipDuration <= 0` equips (or unequips) at once; otherwise
`QueueEquipAction` runs a timed minor action ("$hud_equipping", `equipping` animation), which is
what armor and utility items do. `SharedData.m_equipDuration` defaults to 1, so a cloned utility
item (e.g. the Wisplight) turned weapon needs `m_equipDuration = 0`.

## Stagger and camera shake *(verified 2026-10-28)*

- `Character.Stagger(Vector3 forceDirection)` (public): runs `RPC_Stagger` if we own the
  creature, else `InvokeRPC("RPC_Stagger", ...)` to its owner, so it works on creatures other
  players own and everyone sees it. `RPC_Stagger` does nothing while `IsStaggering()`; otherwise
  it turns the creature to face against the force and triggers the `stagger` animation. A
  longer "stun" = call it again as each stagger ends (ValheimMapMarker's `FlashStun`, every 1.2 s).
- Normal stagger from hits: `AddStaggerDamage` accumulates toward `GetMaxHealth() ×
  m_staggerDamageFactor` (0 = never staggers from damage) and decays at a fifth of that per
  second.
- `GameCamera.instance.AddShake(Vector3 point, float range, float strength, bool continous)`:
  local camera shake, scaled by distance, ignored if weaker than the current shake.
- Check `character.m_nview.IsValid()` before acting on a creature from `GetAllCharacters()`.

