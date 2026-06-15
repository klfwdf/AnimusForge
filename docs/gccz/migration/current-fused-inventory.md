# Current fused GCCZ implementation inventory

Captured from `G:\AFMOD\new-` on 2026-06-08.

## Runtime source of truth

- Main existing behavior: `G:\AFMOD\new-\SiegeAiInterventionBehavior.cs`
- AF aftermath bridge: current AF086 uses `G:\AFMOD\new-086\Patch_GameMenu_ActivateGameMenu.cs` plus guarded helper methods in `G:\AFMOD\new-086\SiegeAiInterventionBehavior.cs`; there is no separate `Patch_SiegeAftermath_AFIntervention.cs` file in AF086.
- AF game menu bridge: `G:\AFMOD\new-086\Patch_GameMenu_ActivateGameMenu.cs`
- AF registration: `G:\AFMOD\new-\SubModule.cs`
- Passive rule config: `G:\AFMOD\new-\AnimusForge\ModuleData\RuleBehaviorPrompts.json`, entry `siege_intervention_aftermath`

## Current behavior slices to refactor from the existing implementation

Refactor in small slices; do not replace the old behavior with a newly invented GCCZ.

1. **Entry and aftermath routing**
   - menu option / troop selection / scene entry
   - native siege aftermath redirect and completion handling
2. **Runtime prompt and postprocess bridge**
   - in-scene prompt facts
   - passive postprocess rules
   - action tag normalization and processing
3. **Outcome state machine**
   - none / waiting decision / mercy-relief / plunder / massacre
   - reversible plunder vs irreversible massacre / repopulation
4. **Shared civilian relief pool**
   - AF give-gold / give-item capture
   - settlement-wide relief settlement or refund on negative outcome
5. **Civilian gathering / speech rally**
   - messenger assignment
   - civilian follow / formation handoff
6. **Allied soldier command and safety**
   - commandable agent marking
   - visual order filtering
   - friendly-hit and command UI guards
7. **Plunder script**
   - target selection
   - soldier interaction assignment
   - loot/gold award
8. **Massacre / destructive outcome script**
   - player attack escalation
   - civilian hostile conversion
   - direct native aftermath pump
9. **Settlement consequence adapters**
   - loyalty / morale / notable / culture repopulation effects
   - final native aftermath choice mapping

## Current action tags

The current implementation recognizes both English and Chinese action tags:

- `[ACTION:SIEGE_MERCY]` / `[ACTION:宽恕]`
- `[ACTION:SIEGE_RELIEF]` / `[ACTION:救济]`
- `[ACTION:SIEGE_INSPIRE]` / `[ACTION:宣抚]`
- `[ACTION:SIEGE_RALLY_OATH]` / `[ACTION:盟誓]`
- `[ACTION:SIEGE_APPEASE_SOLDIERS]` / `[ACTION:安兵]`
- `[ACTION:SIEGE_GATHER_CIVILIANS]` / `[ACTION:召集]`
- `[ACTION:SIEGE_PLUNDER]` / `[ACTION:搜掠]`
- `[ACTION:SIEGE_MASSACRE]` / `[ACTION:血洗]`
- `[ACTION:SIEGE_CULTURAL_REPOPULATION]`, `[ACTION:SIEGE_PURGE_REPOPULATION]` / `[ACTION:殖民]`

## Build-error relevance

`G:\AFMOD\101011.txt` reports an external failed merge with conflict markers in `Patch_GameMenu_ActivateGameMenu.cs`, `ShoutBehavior.cs`, and `MyBehavior.cs`. Local `G:\AFMOD\new-` was checked with ripgrep and currently has no `<<<<<<<`, `=======`, or `>>>>>>>` conflict markers in active `.cs/.json/.xml` files outside `bin/obj` and reference dumps.
