# Projectiles, AoE and rock damage

Read 2026-10-31 from the Decompile workflow (Valheim with Deep North), for SnakeSkills' Ember
Blast. *(verified)* = read in the decompile; nothing here has been tried in-game yet.

## Firing a projectile *(verified)*

- `Attack.FireProjectileBurst()` (private): per projectile, `Instantiate(attackProjectile, ...)`,
  builds a `HitData` (`m_toolTier = m_weapon.m_shared.m_toolTier`, `m_damage.Add(m_weapon
  .GetDamage())`, push force, status effect hash, skill, attacker...), then `ModifyDamage`, then
  **`m_character.GetSEMan().ModifyAttack(skillType, ref hitData)`** (so Mining's pickaxe bonus
  applies to projectiles too), then `IProjectile.Setup(...)`. A prefix/finalizer on it plus a
  `Projectile.Setup` postfix is a clean way to mark "projectiles from this attack".
- `Projectile.Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData,
  ItemDrop.ItemData item, ItemDrop.ItemData ammo)` copies `m_damage`, `m_attackForce`,
  `m_statusEffectHash`, `m_skill`, `m_raiseSkillAmount`, etc. from the HitData. Overwrite them in
  a postfix to change one projectile.
- `ItemData.HaveSecondaryAttack()` = `m_shared.m_secondaryAttack.m_attackAnimation` not empty.
  The Staff of Embers is prefab **`StaffFireball`**, projectile `staff_fireball_projectile`
  (asset search); it has no secondary attack in vanilla *(assumed from gameplay; logged at runtime
  by SnakeSkills if not)*. `Attack.Clone()` is `MemberwiseClone`.

## Projectile flight and hits *(verified)*

- `Projectile.FixedUpdate`: only the **owner** (the shooter) moves it and tests hits (ray or
  sphere cast along the path), calling `public void OnHit(Collider collider, Vector3 hitPoint,
  bool water, Vector3 normal)`.
- `OnHit`: bounce, then `m_aoe > 0` → `DoAOE`, else damage the one `IDestructible` hit; hit
  effects; `SpawnOnHit` (`m_spawnOnHit`, `m_randomSpawnOnHit`, `m_spawnItem`); then
  **`m_onHit?.Invoke(collider, hitPoint, water)`** (`public OnProjectileHit m_onHit`, global
  delegate `void OnProjectileHit(Collider, Vector3, bool)`), skill raise if it hurt a character,
  `RPC_OnHit` to everyone, destroy. `m_onHit` is a free per-instance hook for custom impact logic.
- `DoAOE(hitPoint, ...)`: `OverlapSphere(m_aoe)`, then **one hit per IDestructible object**
  (`s_hitSet` by `FindHitObject`), so a multi-area `MineRock5` loses only one piece per AoE.
- `FindHitObject(collider)` = the `IDestructible` in parents, else the rigidbody, else the collider.

## Damage totals *(verified)*

- `DamageTypes.GetTotalDamage()` = damage + blunt + slash + pierce + **chop + pickaxe** + fire +
  frost + lightning + poison + spirit + nonPlayer. So chop/pickaxe are not "harmless" to
  creatures by themselves; it depends on the creature's modifiers. To be sure a hit can't hurt
  mobs, don't send it to a `Character` at all.
- `GetTotalPhysicalDamage` = blunt + slash + pierce; `GetTotalStaggerDamage` adds lightning.
- `HitData.m_toolTier` is a `short`; rocks check `hit.CheckToolTier(m_minToolTier)` and show
  "too hard" otherwise.

## Rocks *(verified)*

- `MineRock5.Damage(HitData)`: if `hit.m_hitCollider == null || hit.m_radius > 0`, it
  overlap-spheres `hit.m_point` with `m_radius` (or 0.05) and sends `RPC_Damage(hit, areaIndex)`
  for **every one of its area colliders in the sphere**. One call with `m_radius` = a blast.
  Otherwise one area from `m_hitCollider`. `RPC_Damage` (owner) → `DamageArea`: resistance,
  tool tier, `m_health` per area, hit effect, noise, drops when an area dies.
- `MineRock.Damage(HitData)`: needs `m_hitCollider` that is one of `m_hitAreas` (else logs
  "Invalid hit area"); `RPC_Hit` keeps health in ZDO `"Health" + index`.
- `Destructible.Damage` → `RPC_Damage` (owner): one health pool (`ZDOVars.s_health`), world
  level adds HP. Rock-like test used by Mining: pickaxe not Immune, chop Immune, not a Tree.
- SnakeSkills' Mining patches `MineRock5.RPC_Damage`, `MineRock.RPC_Hit`, `Destructible
  .RPC_Damage` (rock-like only): any player hit with pickaxe damage and enough tier gives +1
  Mining XP per call and the yield bonus, whatever the source.
