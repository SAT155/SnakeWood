# Blocking, shields and left-hand items

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-10-24 by the ValheimMapMarker
North Wisp session. All *(verified)* by decompile unless noted.

## Block input and state

- `Player.SetControls(..., bool block, bool blockHold, ...)` sets `Character.m_blocking`
  (protected; the publicizer exposes it) to `blockHold`, or toggles it on `block` in the
  gamepad toggle layout. Moving with auto-run/blockHold also steers; `m_blocking` is cleared in
  some movement cases. Jump or dodge while blocking dodges.
- `Humanoid.IsBlocking()`: on the owner, `m_blocking` and not attacking, dodging, in place mode,
  encumbered, in a minor action or staggering. On other clients it reads the ZDO bool
  `ZDOVars.s_isBlockingHash`.
- So for "is the block button held" read `m_blocking`; for "is the block actually up" read
  `IsBlocking()`.
- `Humanoid.UpdateBlock` (FixedUpdate) sets the ZDO bool and `m_zanim.SetBool("blocking")` when
  the state changes. The raised-shield pose is therefore **synced to every client**, including
  unmodded ones.
- `GetCurrentBlocker()` = the left item if any, else the current weapon. `BlockAttack` uses its
  `GetBlockPower(skillFactor)`, `m_timedBlockBonus` (parry within 0.25 s, `m_blockTimer`),
  `m_damageModifiers`, `m_blockEffect`, and raises Blocking skill.
- Blocking slows stamina and eitr regen (×0.8).

## Equipping (`Humanoid.EquipItem`)

- `Shield`: goes to `m_leftItem`; unequips the right item unless it's a one-handed weapon or a
  torch.
- `Bow`: left hand, unequips both.
- `Torch`: right hand, or left hand when a one-handed weapon is already in the right and the left
  is empty.
- `GetCurrentWeapon()` returns the left item only if it `IsWeapon()` and isn't a torch, so a
  shield never becomes the weapon.
- Animation state comes from the left item's `m_shared.m_animationState` (`LeftTorch` for torches).
  `ItemDrop.ItemData.AnimationState` includes `Shield`, `Staves`, `MagicItem`.
- `Player` treats a left item with `m_shared.m_attack.m_attackEitr > 0` as a magic weapon for
  gamepad trigger feedback, so left-hand items can carry attacks.

## Hand models (`VisEquipment`)

- `SetLeftItem(hash, variant, quality)` writes the item hash to the ZDO (`ZDOVars.s_leftItem`);
  each client looks it up in its own `ObjectDB`. A client without the item logs
  `Missing attach item: <hash>` and shows nothing. Harmless, but unmodded players can't see a
  modded hand item.
- `AttachItem` instantiates the item prefab's first child named `attach` (or `attach_skin`;
  `attach_back` for back slots), runs `CleanupInstance`, enables equip effects, and parents it to
  the joint (`m_leftHand`, `m_rightHand`, `m_backShield`, ...). An item with no such child shows
  nothing in hand. To make a non-hand item holdable, add an `attach` child to its prefab.

## Hotbar and item use

- `Player` hotbar: `ZInput.GetButtonDown("Hotbar{i}")` or `"Hotbar{i}Alt"` (i = 1..8) calls
  `UseHotbarItem(i)` → `m_inventory.GetItemAt(i - 1, 0)` → `UseItem(null, item, fromInventoryGui:
  false)`. Only the press is seen; to detect a hold, poll `ZInput.GetButton` yourself.
- `Humanoid.UseItem(inventory, item, fromInventoryGui)` is the one path for hotbar keys and
  inventory right-click; it may interact with a hovered object first when not from the GUI.
