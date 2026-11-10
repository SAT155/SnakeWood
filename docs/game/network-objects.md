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
- **Don't strip or `DestroyImmediate` inside an animation event** (or physics trigger/contact,
  rendering callbacks, `OnValidate`): Unity refuses with "Destroying components immediately is
  not permitted during ... animation event callbacks" *(seen in-game, 2026-10-25)*, the
  components stay, and a stripped network-prefab copy keeps its `ZNetView`. `Attack.OnAttackTrigger`
  (and so `ProjectileAttackTriggered`) runs from an animation event: queue the work and do it in
  the next `Update`. Also check a stripped copy has no `ZNetView` left before waking it.
- This was the North Wisp's bug: the charged cast's mist clearers were built at the throw's
  release point. The guard's log line named the object only by its hash, since the copy had
  been renamed; a hash not in `ZNetScene` points at a renamed copy like that.

## Showing a local effect to other modded players (routed RPC)

Lights, camera shake and other local-only things never reach other players. ValheimMapMarker's
North Wisp 1.10.2 sends them as a routed RPC, built from memory of the API and not yet confirmed
in-game:
- Register on each new `ZRoutedRpc.instance` (it's made anew for each world joined; checking every
  frame is cheap): `ZRoutedRpc.instance.Register<Vector3>("Name", (long sender, Vector3 pos) => …)`.
- Send with `InvokeRoutedRPC(ZRoutedRpc.Everybody, "Name", pos)`. Skip the handler when
  `sender == ZRoutedRpc.instance.m_id`, in case the sender gets its own call (unverified).
- Leave out of the remote copy any spawned prefab with a `ZNetView`: it is already a network
  object that everyone sees.
- Unmodded clients have no handler for it. That is assumed harmless (at most a log line) but not
  verified.

## Custom pieces seen by unmodded players

An unmodded client or server that loads a ZDO whose prefab hash it doesn't know logs
`Missing prefab hash: <hash>` from ZNetScene **every frame** while that area is loaded. The ZDO is
kept, so nothing is lost, but the log floods. To find the mod: hash the prefab names with
`GetStableHashCode` (SnakewaySurfer's Strange Portal was -1982128). Fix used in SnakewaySurfer
1.16.132: save the piece under the vanilla prefab it was copied from (`zdo.SetPrefab(hash)` in a
component Awake that runs after ZNetView's) plus a ZDO flag. Modded clients convert flagged
instances in a postfix on that prefab's own Awake. Unverified in-game as of 2026-11-10.
