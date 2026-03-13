# Vanilla Issue Audit

## Scope
- Covers `39` primary `*IssueBehavior.cs` files and `4` quest-only `*IssueQuestBehavior.cs` files under `原版游戏本体代码/TaleWorlds.CampaignSystem/Issues` and `原版游戏本体代码/SandBox/Issues`.
- Raw machine-generated inventory lives in `docs/vanilla_issue_audit.csv`.
- Regenerate the CSV after upstream code changes with `powershell -ExecutionPolicy Bypass -File tools/Export-VanillaIssueAudit.ps1`.

## Snapshot
- Total task-bearing source files: `43`
- Risk split: `29 High`, `14 Medium`, `0 Low`
- Shared success hook alone is insufficient: many quests call `GiveGoldAction.ApplyBetweenCharacters(...)`, `ChangeRelationAction.ApplyPlayerRelation(...)`, or set `RelationshipChangeWithQuestGiver` before `base.CompleteQuestWithSuccess()`.
- `QuestBase.CompleteQuestWithSuccess()` at `原版游戏本体代码/TaleWorlds.CampaignSystem/QuestBase.cs:210` is still the shared completion choke point, but it must be paired with reward/relation interception to protect the AnimusForge turn-in flow.

## Risk Rubric
- `High`: no shared `base.CompleteQuestWithSuccess()` call, wraps completion in a custom helper, emits more than one gold transfer, or emits at least five direct relation calls.
- `Medium`: still uses the shared success path but already changes gold, relation, or `RelationshipChangeWithQuestGiver` on the way there.
- `Low`: would have no reward or relation side effects before shared completion. None of the current vanilla issue files land here.

## High-Risk Files First
- `原版游戏本体代码\SandBox\Issues\SnareTheWealthyIssueBehavior.cs`
- `原版游戏本体代码\SandBox\Issues\RivalGangMovingInIssueBehavior.cs`
- `原版游戏本体代码\SandBox\Issues\ProdigalSonIssueBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\ArmyNeedsSuppliesIssueBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\GangLeaderNeedsToOffloadStolenGoodsIssueBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\LandLordTheArtOfTheTradeIssueBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\RevenueFarmingIssueBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\VillageNeedsToolsIssueBehavior.cs`

## Quest-Only Files
- `原版游戏本体代码\SandBox\Issues\TheSpyPartyIssueQuestBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\GangLeaderNeedsWeaponsIssueQuestBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\LordNeedsGarrisonTroopsIssueQuestBehavior.cs`
- `原版游戏本体代码\TaleWorlds.CampaignSystem\Issues\MerchantNeedsHelpWithOutlawsIssueQuestBehavior.cs`

## Reverse-Engineering Workflow
- Extract prompt-facing fields first: `IssueBase.Title`, `IssueBase.Description`, `IssueBase.IssueBriefByIssueGiver`, `IssueBase.IssueQuestSolutionExplanationByIssueGiver`, and quest journal logs.
- Mark where each quest flips from “objective complete” to “quest finalized”.
- Record every pre-finalize side effect: gold transfer, inventory grant, direct relation change, trait/renown/influence change, prisoner transfer, and party/item exchange.
- Distinguish between “objective complete” and “NPC turn-in complete”; the mod needs to intercept the first and defer the second.
- Capture whether vanilla already waits for a follow-up conversation before success. Those files are the best design references for the AnimusForge turn-in flow.

## Suggested Audit Fields Per Quest
- `Quest start trigger`
- `Objective completion trigger`
- `Vanilla success call site`
- `Gold/item transfer call sites`
- `Relation call sites`
- `Needs NPC turn-in conversation`
- `Best AnimusForge interception point`

## Immediate Engineering Implication
- A future interception layer should not rely on `QuestBase.CompleteQuestWithSuccess()` alone.
- The mod will need:
- A `pending_turn_in` state keyed by quest or issue owner
- A reward suppression layer for gold/item payout before shared completion
- A relation deferral or replay layer for quest-side `ChangeRelationAction` calls
- A final “authorized finalize” path once the LLM-driven turn-in conversation has actually paid the player from NPC inventory
