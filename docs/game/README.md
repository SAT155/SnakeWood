# Game-system topic notes

Small notes on how parts of Valheim work, written by Claude sessions from what they decompiled
(the Decompile workflow, see the main notes §3). One file per system. They record class and
method names, key fields, how the pieces fit, and gotchas, not raw decompiled code: re-run the
Decompile workflow when you need the code itself. Linked from the main notes
(`docs/VALHEIM_MODDING_NOTES_v*.md`, §5).

Each note says which game version it was read from. A fact marked *(verified)* was read in the
decompile or seen in-game; anything else is a best guess. If a game update changes something,
fix the note and say so in its changelog line.

| File | System |
| --- | --- |
| [mist-and-demisters.md](mist-and-demisters.md) | Mistlands mist (`ParticleMist`), `Demister`, the Wisplight (`SE_Demister`, `demister_ball`) |
| [blocking-and-left-hand.md](blocking-and-left-hand.md) | Blocking, shields and other left-hand items, `VisEquipment` hand models, hotbar use |
| [magic-costs-and-skills.md](magic-costs-and-skills.md) | Stamina and eitr costs, skill discounts, skill raises, spirit damage, `HitData` |
| [asset-names.md](asset-names.md) | Vanilla effect and sound prefab names found by asset search, for borrowing |
| [building-and-placement.md](building-and-placement.md) | Click-to-place flow (`UpdatePlacement`, `TryPlacePiece`, `UpdatePlacementGhost`), build panel requirement slots, Valheim Plus placement patches |
