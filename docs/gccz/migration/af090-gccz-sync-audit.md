# AF 0.9.0 GCCZ sync audit

Date: 2026-06-20

## Scope

- Read-only upstream source checked: `G:\AFMOD\YM0.9.0\Mount-Blade-Bannerlord-AnimusForge-mod-main`.
- Standalone GCCZ source checked: `G:\AFMOD\GCCZ`.
- New fused worktree seeded from upstream: `G:\AFMOD\NEW-0.9`.

## Result

AF 0.9.0 already contains the same GCCZ reusable source, docs, and passive rule content as standalone `G:\AFMOD\GCCZ`.

No GCCZ core code sync was required.

## Evidence

- Core C# file set:
  - `G:\AFMOD\YM0.9.0\Mount-Blade-Bannerlord-AnimusForge-mod-main\AnimusForge.SiegeAftermathIntervention`: 65 `*.cs` files.
  - `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention`: 65 `*.cs` files.
  - File-name-only differences: 0.
  - Normalized content differences: 0.
- Raw SHA differences: 18 files, caused by line-ending/BOM-level formatting only:
  - `SiegeActionTagCatalog.cs`
  - `SiegeAgentWallRescueProfile.cs`
  - `SiegeAmbientReactionProfile.cs`
  - `SiegeCivicChoiceProfile.cs`
  - `SiegeCivilianGatherInteractionProfile.cs`
  - `SiegeDestructiveChoiceProfile.cs`
  - `SiegeInterventionActionRuleDecision.cs`
  - `SiegeInterventionActionRules.cs`
  - `SiegeInterventionOutcome.cs`
  - `SiegeNotableSceneDeathProfile.cs`
  - `SiegePostprocessContextBuilder.cs`
  - `SiegePostprocessContextFacts.cs`
  - `SiegePostprocessRuleCatalog.cs`
  - `SiegePostprocessRuleDefinition.cs`
  - `SiegePostprocessRuleFilter.cs`
  - `SiegeReliefChoiceProfile.cs`
  - `SiegeSettlementEffectProfile.cs`
  - `SiegeSettlementOutcomeProfile.cs`
- GCCZ docs:
  - `G:\AFMOD\YM0.9.0\Mount-Blade-Bannerlord-AnimusForge-mod-main\docs\gccz`
  - `G:\AFMOD\GCCZ\docs`
  - File-name-only differences: 0.
  - Normalized content differences: 0.
- Passive rule `siege_intervention_aftermath`:
  - `G:\AFMOD\GCCZ\ModuleData\siege_intervention_aftermath.rule.json`
  - `G:\AFMOD\YM0.9.0\Mount-Blade-Bannerlord-AnimusForge-mod-main\RuleBehaviorPrompts.json`
  - `G:\AFMOD\YM0.9.0\Mount-Blade-Bannerlord-AnimusForge-mod-main\AnimusForge\ModuleData\RuleBehaviorPrompts.json`
  - Normalized rule SHA: `4fb1b35758649ff0a5bf00aecb11eb9a1de30d51f3eb159898e39d1c622a5481`.
  - `TriggerKeywords` remains empty by design.

## Sync action

- Created `G:\AFMOD\NEW-0.9` from AF 0.9.0 upstream source.
- Kept `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention` unchanged because there were no normalized source differences to import.
- Recorded this audit in standalone GCCZ and mirrored it into the fused `NEW-0.9` docs tree.

## Boundary note

For AF 0.9.0, the expected boundary remains unchanged:

- GCCZ reusable rules/profiles stay in `AnimusForge.SiegeAftermathIntervention`.
- AF host files only keep active-stage guards, bridge calls, runtime object lookup, and side effects.
- `YM0.9.0` remains read-only.
