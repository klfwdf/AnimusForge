# AF 0.9 siege aftermath bridge runtime compatibility

Date: 2026-06-20

## Scope

This note records a fused-tree bridge fix for `G:\AFMOD\NEW-0.9`. GCCZ core rules did not change; the failure was in AF/Bannerlord runtime adaptation around native post-siege menus.

## Fixed bridge problems

- `MenuContext.OpenTroopSelection` is version-sensitive:
  - Bannerlord 1.3.x runtime exposes the 6-parameter overload.
  - Bannerlord 1.4.5/Naval-aware references expose a ship-aware overload with extra parameters.
- The fused AF bridge must not hard-call the ship-aware signature when running on 1.3.15. It now resolves `OpenTroopSelection` at runtime and falls back to the 6-parameter shape when that is what the loaded game DLL provides.
- If GCCZ entry throws before the intervention mission opens, the bridge must reset AF/GCCZ aftermath runtime guards. Otherwise native post-siege summary menus can be suppressed after ordinary vanilla choices.
- If a stale `WaitingDecision` guard is encountered before a native siege aftermath menu and there is no active mission, pending GCCZ outcome, or direct loot script, the bridge releases the stale guard and lets vanilla continue.

## Isolation contract

- Keep this as AF-side bridge behavior in fused trees such as `G:\AFMOD\NEW-0.9\SiegeAiInterventionBehavior.cs`.
- GCCZ core owns only source/reason strings in `SiegeAftermathTransitionSourceProfile`:
  - `ResetInterventionEntryFailedSource`
  - `BuildResetStaleEntryGuardSource(...)`
- Do not move Bannerlord reflection, `MenuContext`, `GameMenu`, or `PlayerEncounter` side effects into standalone GCCZ core.

## AF 0.9.1 prompt/postprocess bridge addendum

Date: 2026-06-21

The AF 0.9.1 fused tree must also keep the GCCZ shout bridge connected during the active post-siege intervention scene:

- `MyBehavior.BuildShoutPromptContextForExternalInternal(...)` appends `SiegeAiInterventionBehavior.BuildRuntimePromptForPromptContext(...)` as passive rule block `siege_intervention_aftermath`.
  - This preserves normal AF culture/settlement/person/history memory first.
  - GCCZ runtime prompt then overrides only current post-siege facts and action semantics.
- `ShoutBehavior.RunCourierActionPostprocessForExternal(...)` and deferred scene postprocess both include `SiegeAiInterventionBehavior.BuildRuntimePostprocessRulesForExternal()` plus `BuildRuntimePostprocessContextForExternal(...)`.
- GCCZ tags produced by the unified postprocessor must be executed through `SiegeAiInterventionBehavior.TryProcessAiActionTags(...)`, not re-enqueued as normal NPC speech.
- `SiegeAiInterventionBehavior` records one entry memory when the intervention mission starts so civilians know the settlement has already fallen and the player is the occupying decision-maker.
- The “亲自进城决定” entry is town-only. Castle scenes usually have no civilian population to handle, so castles should fall through to vanilla aftermath choices.
