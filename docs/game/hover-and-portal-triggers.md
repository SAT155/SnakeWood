# Hover/interact search and portal triggers

Decompiled from Valheim (Deep North, Oct 2026) in the SnakeWood seat/reach thread
(SnakeWood 1.23.79). Classes: `Player`, `TeleportWorld`, `TeleportWorldTrigger`.

## Player hover search

- `public float m_maxInteractDistance = 5f;` (field on `Player`).
- `private bool FindHoverObject(out GameObject hover, out Character hoverCreature)`: casts from the
  camera, takes the first hit and keeps it only if
  `Vector3.Distance(m_eye.position, hit.point) < m_maxInteractDistance + (Hoverable?.GetHoverOffset() ?? 0)`.
  Hover target = the collider with a `Hoverable`, else its `attachedRigidbody`'s object, else the
  collider's object. Then `break`: **only the first hit counts**, so a vehicle under the camera
  ray blocks everything behind it.
- Raising reach: set `m_maxInteractDistance` around a `FindHoverObject` call and restore it in
  `finally`. The same field is used by the doodad drop check
  (`Vector3.Distance(m_doodadController.GetPosition(), transform.position) > m_maxInteractDistance`
  → `StopDoodadControl()`).
- Gotcha: `UpdateHover` clears `m_hovering` while controlling a doodad (see main notes §5); SnakeWood
  re-runs `FindHoverObject` in a postfix with its own log on Ignore Raycast
  (`HoverLog.BeginRiderPassThrough` / `EndRiderPassThrough`) and +`Extra Reach While Riding`.

## Portals

- `TeleportWorldTrigger.OnTriggerEnter(Collider colliderIn)`:
  `colliderIn.GetComponent<Player>()` must be `Player.m_localPlayer`, then
  `m_teleportWorld.Teleport(player)` (`m_teleportWorld` = `GetComponentInParent<TeleportWorld>()` in
  Awake). **Only the player's own collider triggers it**: a vehicle's nose entering does nothing,
  so a mounted rider can't reach a portal set against a wall.
- `TeleportWorld.Teleport(Player)`: returns if `!TargetFound()`; messages and returns for
  `GlobalKeys.NoPortals`, `NoBossPortals` during a boss, or `!player.IsTeleportable(m_allowAllItems)`
  (so calling it every frame spams the message; call on entry only). Then `TeleportTo` to the
  linked portal.
- `TeleportWorld.Interact` = rename tag (`TextInput.RequestText(this, "$piece_portal_tag", 10)`),
  after `PrivateArea.CheckAccess`. Hover text shows the tag and connected state.
- SnakeWood's fix (`PortalByLog` in `HoverLogPortal.cs`): `Physics.OverlapCapsuleNonAlloc` over the
  log's capsule with `QueryTriggerInteraction.Collide`, all layers, find
  `GetComponentInParent<TeleportWorldTrigger>()` on trigger colliders, call `Teleport` once per
  entry. *(compiles; not yet tested in-game)*

## Saddle and rug prefab names *(verified, asset search)*

`SaddleLox`, `SaddleAsksvin`, `SaddleMoose`, `Saddle`; rugs `rug_deer`, `rug_wolf`, `rug_fur`,
`rug_hare`, `rug_moose`, `rug_seal`, `rug_straw`, `rug_asksvin`, `rug_Bjorn`.
