# Network objects: ZNetView and ZNetScene

Read 2026-10-25 from the Decompile workflow (Valheim with Deep North), after the ValheimMapMarker
North Wisp flooded a tester's log. *(verified)* = read in the decompile.

- `ZNetView.Awake` *(verified)*: if `ZNetView.m_forceDisableInit` (static) or no `ZDOMan`, it
  `Destroy(this)` and stops; otherwise it gets or creates a ZDO and registers in
  `ZNetScene.m_instances` (ZDO → ZNetView). A plain `Object.Instantiate` of a prefab with a
  `ZNetView` is therefore a network object.
- `ZNetScene.Destroy(GameObject)` *(verified)*: `ResetZDO`, remove from `m_instances`, destroy
  the ZDO if owner, then `Object.Destroy`. `ZNetView.Destroy()` goes through it. Always remove
  network objects this way (claim ownership first if needed).
- `ZNetScene.RemoveObjects` *(verified)* runs every frame and reads
  `value.GetZDO().TempRemoveEarmark` for **every** registered view, then
  `Object.Destroy(zNetView.gameObject)` for out-of-range ones. A view destroyed with a plain
  `Object.Destroy` (or by its parent being destroyed, or a particle `stopAction`) stays
  registered, and this throws a `NullReferenceException` every frame: a flood of
  "ZNetScene.RemoveObjects" errors and a very slow game. The loop never gets to remove it.
- To make a local, unregistered copy of a network prefab: instantiate under an inactive parent
  with `m_forceDisableInit = true`, strip what you don't want, and keep the flag set while you
  call `SetActive(true)` too, so a `ZNetView` that survived stripping removes itself.
- Safety net used in ValheimMapMarker (`NetworkObjectGuard.cs`): a prefix on
  `ZNetScene.RemoveObjects` that drops entries whose view is null or has no ZDO (destroying a
  non-persistent ZDO we own, as vanilla would) and logs the prefab name once.
