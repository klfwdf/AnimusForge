# AF 1.1.3 fixed-keyword postprocess bridge

- GCCZ fixed-keyword handling is active-stage gated by `SiegeAiInterventionBehavior.IsActiveInCurrentMission()` and applies the matched action immediately.
- When that immediate path reports `actionHandled=true`, fused `ShoutBehavior` must disable only the GCCZ postprocess selection for the same reply. This prevents the same action from being emitted and applied a second time by the deferred AI postprocess while leaving unrelated AF postprocess rules enabled.
- SETS selected-follower speech filtering remains mission-identity gated by `SettlementEntryTroopSelectionBehavior.IsSetsSelectedFollowerAgentForExternal(...)`; it returns false outside the active SETS entry mission and therefore does not suppress ordinary AF NPC replies.
- The root and packaged `RuleBehaviorPrompts.json` copies must contain one canonically identical `siege_intervention_aftermath` entry for source/tooling and runtime packaging parity.
