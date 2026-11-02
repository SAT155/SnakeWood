# Fishing and skill raises

Read from: Valheim with Deep North (Oct 2026), decompiled 2026-10-30 by the SnakeSkills session.
All *(verified)* by decompile unless noted.

## How Fishing XP is earned

- Only `FishingFloat.FixedUpdate` (private, runs on the float's owner = the fisher) raises it:
  while blocking (pulling) with stamina and the line still longer than the distance,
  `m_fishingSkillImproveTimer += dt * (fish == null ? 1 : 2)` and at > 1 it calls
  `owner.RaiseSkill(Skills.SkillType.Fishing)` (value 1). So 1 per second of reeling, 2 with a
  fish on, the same for every fish. Nothing extra for the catch.
- Catch: when `m_lineLength <= 0.5` with a fish, `public static string Catch(Fish fish,
  Character owner)` picks it up (`ItemDrop.Pickup` or `Fish.Pickup`) and returns the message.
  A postfix there is the clean "fish caught" hook; it runs inside `FixedUpdate`.
- Lost fish: stamina out (`$msg_fishing_lost`), line too long (`$msg_fishing_linebroke`),
  attacking or drawing a bow.

## Fish (component `Fish`)

- Prefab names: Fish1 Perch, Fish2 Pike, Fish3 Tuna, Fish4_cave Tetra, Fish5 Trollfish, Fish6
  Giant herring, Fish7 Grouper, Fish8 Coral cod, Fish9 Anglerfish, Fish10 Northern salmon, Fish11
  Magmafish, Fish12 Pufferfish (Deep North fish names unverified). `Utils.GetPrefabName(go)`.
- Stars = `GetComponent<ItemDrop>().m_itemData.m_quality`. Pull stamina per second is
  `fish.GetStaminaUse()` (`m_staminaUse`, or `m_escapeStaminaUse` while escaping) × quality.
- Other fields: `m_baseHookChance`, `m_escapeMin/Max`, `m_escapeMaxPerLevel`, `m_baits`
  (`BaitSetting { m_bait, m_chance }`), `m_extraDrops`. Values live on the prefabs.
- `FishingFloat.GetCatch()` is the hooked fish; `FishingFloat.FindFloat(fish)`.

## Skill raises

- `Player.RaiseSkill(SkillType, float value = 1)` overrides `Character.RaiseSkill` (which only
  levels a tame's owner skill): multiplies by `SEMan.ModifyRaiseSkill`, then `Skills.RaiseSkill`.
- `Skills.RaiseSkill`: `skill.Raise(factor)`; on a level-up, skill-cap rebalance, the level-up
  effect and the "$msg_skillup" message.
- `Skills.Skill.Raise` can gain **at most one level per call**: past `GetNextLevelRequirement()`
  (`floor(level+1)^1.5 × 0.5 + 0.5`) it adds 1 level and sets the accumulator to 0, dropping the
  rest. To give a big amount, call it in small steps (SnakeSkills uses steps of 1).

## Crafting with fish (fillets, Fish 'n' Bread)

- Recipes are `Recipe` ScriptableObjects in `ObjectDB.m_recipes` (not GameObjects, so the asset
  search doesn't list them); find them by `recipe.m_item.gameObject.name`. Items: `FishRaw` (Raw
  fish, the fillets), `FishAndBreadUncooked` (needs Anglerfish `Fish9` plus other ingredients).
- `Recipe.GetAmount(quality, out need, out singleReqItem, craftMultiplier)` is the amount
  `InventoryGui.DoCrafting` adds (and checks inventory space for). Only for
  `m_requireOnlyOneIngredient` recipes (fillets) it adds the ingredient's stars:
  `+ ceil((quality - 1) × m_amount × m_qualityResultAmountMultiplier) + m_extraAmountOnlyOneIngredient`,
  using `Player.GetFirstRequiredItem` (lowest quality with enough count). Other recipes ignore
  ingredient stars. The recipe list shows `m_amount × multiplier`, not `GetAmount`.
- Normal recipes: `HaveRequirementItems` needs enough of one quality (max count over qualities);
  `ConsumeResources(..., itemQuality -1)` → `Inventory.RemoveItem(name, n, -1)` takes from the
  first matching stacks in inventory order, any quality.

