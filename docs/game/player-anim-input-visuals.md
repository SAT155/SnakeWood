# Player animation sync, input locks, helmet visuals, rewards

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-11-04 by the SnakewaySurfer
session. *(verified)* = read in the decompile; nothing here is tested in-game yet.

## Animation floats (what others see)

- `Character` statics *(verified)*: `s_forwardSpeed = ZSyncAnimation.GetHash("forward_speed")`,
  `s_sidewaySpeed` ("sideway_speed"), `s_turnSpeed`, `s_inWater`, `s_onGround`, `s_encumbered`,
  `s_flying`, `s_slipping`, `s_skating`; `Player.s_crouching` ("crouching").
- `private void Character.UpdateWalking(float dt)` ends by writing them through `m_zanim`
  (`ZSyncAnimation.SetFloat/SetBool`), owner only. `ZSyncAnimation` syncs them, so a postfix on
  `UpdateWalking` that overwrites `forward_speed` with `m_runSpeed` makes a standing player run
  in place for everyone, modded or not. `ZSyncAnimation.SetFloat` damps forward/sideway speed
  (0.2 s) when `m_smoothCharacterSpeeds`.
- Triggers: `Character.Jump` → `m_zanim.SetTrigger("jump")`; stagger `"stagger"`; dodge
  `"dodge"`. Creatures use the same `forward_speed` / `onGround` names.

## Locking input

- `protected override bool Player.TakeInput()`: false while chat focused, `Console.IsVisible()`,
  `TextInput`, `StoreGui`, `InventoryGui`, `Menu.IsVisible()`, minimap open, free-fly, dead,
  cutscene, teleporting. A postfix forcing false stops hotkeys.
- `PlayerController` has its **own** private `TakeInput(bool look)` (doesn't call the player's).
  Movement goes through `Player.SetControls(Vector3 movedir, bool attack, bool attackHold, bool
  secondaryAttack, bool secondaryAttackHold, bool block, bool blockHold, bool jump, bool crouch,
  bool run, bool autoRun, bool dodge = false)`: a prefix with `ref` params zeroing them freezes
  the character.
- `Console`: `public static bool IsVisible()` = `m_chatWindow.gameObject.activeInHierarchy`;
  close it with `Console.instance.m_chatWindow.gameObject.SetActive(false)`. Its `Update`
  toggles on the "Console" button or Escape while visible.
- `Menu.Update` (private) opens the pause menu on Escape; skipping it (prefix returning false)
  while a minigame owns Escape stops the menu popping up. `Menu.IsVisible()` is static.
- `Hud.instance.m_rootObject` is the HUD root; `Hud.IsUserHidden()` is Ctrl+F3's state.

## Helmet override that vanilla clients see

- `VisEquipment.SetHelmetItem(int itemHash)`: stores it and, on the owner, writes
  `ZDOVars.s_helmetItem` to the ZDO, so every client draws that helmet *(verified)*.
- `Humanoid.SetupVisEquipment(VisEquipment, bool isRagdoll)` (protected virtual, Player
  overrides it adding model/skin/hair) sets all slots from what's equipped. Postfix
  `Player.SetupVisEquipment` to swap the hash, and call `player.SetupVisEquipment(
  player.m_visEquipment, false)` to apply or undo. Dverger circlet prefab: `HelmetDverger`.

## Giving items and XP

- `Inventory.AddItem(GameObject prefab, int amount)` (bool, clones the ItemDrop data),
  `CanAddItem(GameObject prefab, int stack = -1)`, `AddItem(ItemData)`, and
  `AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName,
  bool cheated, bool pickedUp = false)` (returns ItemData) *(verified)*.
- `Skills.RaiseSkill(SkillType, float factor = 1)` → `Skill.Raise(factor)`: adds
  `m_info.m_increseStep * factor * Game.m_skillGainRate` to `m_accumulator`; if it reaches
  `GetNextLevelRequirement()`, level +1 and accumulator **reset to 0** (one level per call, the
  extra is lost). Stops at 100. Vanilla running calls `RaiseSkill(Run)` once per second of
  running with stamina. To give "N levels' worth", feed it one level at a time.
  `Skills.RaiseSkill` itself applies no status-effect bonus (that's in the Character wrapper).

## Rendering a private scene

- A second `Camera` with `cullingMask = 1 << layer` and a higher `depth` than `Camera.main`,
  `clearFlags = SolidColor`, draws only pictures on that layer; set the main camera's mask to 0
  meanwhile and restore it on destroy. SnakeSkills uses layer 31 for thumbnails, SnakewaySurfer
  layer 30. Fog/ambient are global `RenderSettings` driven by `EnvMan`; swap them in
  `OnPreRender`/`OnPostRender` of a script on your camera. *(Approach untested in-game.)*
