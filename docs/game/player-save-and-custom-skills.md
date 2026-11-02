# Character save and custom skills

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-10-29 by the SnakeSkills session.
All *(verified)* by decompile unless noted. Also see
[magic-costs-and-skills.md](magic-costs-and-skills.md) for skill factors and raises.

## Why unmodded play wipes custom skills

- `Skills.Load(ZPackage)`: version int, `m_skillData.Clear()`, then per entry `(SkillType)int`,
  level, accumulator (version >= 2). It keeps an entry only `if (IsSkillValid(skillType))`, and
  `private static bool IsSkillValid(SkillType)` is `Enum.IsDefined(typeof(SkillType), type)`.
- `Skills.Save` writes `m_skillData` as it is. So a load without the mod drops the custom ids and
  the next save writes the list without them: gone for good.
- blaxxun-boop's **SkillManager** (`Libs/SkillManager.dll`) postfixes `IsSkillValid` to accept its
  skills, gives each `SkillType = Math.Abs(englishName.GetStableHashCode())` (`Skill.fromName`),
  and also patches `GetSkillDef`, `CheatRaiseSkill`/`CheatResetSkill`, `OnDeath`. It does not
  persist anything itself (upstream source checked 2026-10-29).
- `Skills.m_player` is the owning Player; `GetSkill(type)` creates the entry if missing.

## Player.m_customData survives vanilla

- `public Dictionary<string, string> m_customData` on `Player`. `Player.Save(ZPackage)` (data
  version 34) writes its count and pairs; `Player.Load(ZPackage)` reads them back into it. Vanilla
  never looks at the keys, so mod data stored there survives unmodded sessions.
- Where `m_skills.Load` sits inside `Player.Load` relative to `m_customData` wasn't printed.
  SnakeSkills doesn't depend on it: it notes missing skills in a `Skills.Load` postfix, restores
  there if the backup is already loaded, and again in a `Player.Load` postfix.
- Pattern (SnakeSkills `SkillBackup.cs`): `Player.Save` prefix writes
  `m_customData["SnakeSkills_<Name>"] = "level;accumulator"` (invariant culture) for skills present
  in `m_skillData`; on load, a skill missing from `m_skillData` is set from the backup, a present
  one wins. *(Untested in-game.)*
