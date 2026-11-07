# World progress: boss keys, world id, per-world saves

Checked against the dedicated-server assemblies by the Decompile workflow on 2026-11-04
(SnakewaySurfer session). Not yet confirmed in-game.

## Boss kills

- A boss kill sets a global key: `defeated_eikthyr`, `defeated_gdking` (the Elder),
  `defeated_bonemass`, `defeated_dragon` (Moder), `defeated_goblinking` (Yagluth),
  `defeated_queen` (the Queen). Boss prefabs: `Eikthyr`, `gd_king`, `Bonemass`, `Dragon`,
  `GoblinKing`, `SeekerQueen`.
- Read them on a client with `ZoneSystem.instance.GetGlobalKey(string name)` (there are also
  `GetGlobalKey(GlobalKeys)`, `GetGlobalKey(string, out string)` and `GetGlobalKeyExact`).
- Worlds with player-based keys keep them on the character instead: `ZoneSystem.CheckKey`
  falls back to `Player.m_localPlayer.HaveUniqueKey(key)`. Check both if you don't want to
  call `CheckKey` itself.
- Summoning items (vanilla amounts at the altars): `TrophyDeer` ×2, `AncientSeed` ×3,
  `WitheredBone` ×10, `DragonEgg` ×3, `GoblinTotem` ×5, `DvergrKeyFragment` ×3. Altar stones:
  `BossStone_Eikthyr`, `BossStone_TheElder`, `BossStone_Bonemass`, `BossStone_DragonQueen`,
  `BossStone_Yagluth`, `BossStone_TheQueen`, `BossStone_Fader`.

## Which world

- `ZNet.instance.GetWorldName()` and `ZNet.instance.GetWorldUID()` (long) both exist. Which of
  them a client connected to a dedicated server gets filled in is not checked yet; SnakewaySurfer
  uses the name and falls back to the UID.
- Per-world data on the character: `Player.m_customData` (saved with the character, kept by
  vanilla) under a key that includes the world name, e.g. `SnakewaySurfer.Checkpoints.<world>`.
  See [player-save-and-custom-skills.md](player-save-and-custom-skills.md).
