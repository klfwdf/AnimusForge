# GCCZ Town Runtime Inventory

This inventory freezes the active town implementation surface before final cleanup. It records live callers and compatibility seams rather than treating old comments or inactive reference dumps as runtime truth.

## Scope

- Reusable source of truth: `GCCZ/src/AnimusForge.SiegeAftermathIntervention`.
- Compile-ready mirror: `NEW-10/AnimusForge.SiegeAftermathIntervention`.
- AF adapter files: `NEW-10/AfGcczShoutBridge.cs`, `NEW-10/SiegeAiInterventionBehavior.cs`, and the narrow call sites in `NEW-10/ShoutBehavior.cs`.
- Active scope: the captured settlement aftermath mission accepted by `SiegeAiInterventionBehavior`. Ordinary settlement visits, villages, castles, world-map dialogue, player exports, and unrelated AF actions are outside the town refactor unless an explicit bridge guard is required.

## Live Entry And Exit Path

1. `SubModule` registers one `SiegeAiInterventionBehavior` campaign behavior.
2. `SiegeAiInterventionBehavior.AddGameMenus` adds the aftermath entry option to the compatible native aftermath menus.
3. `EnterIntervention` captures the settlement, owner, besieger, and selected troop context before mission creation.
4. `OnMissionStarted` rejects a mismatched live settlement, begins `TownSceneMemorySession`, and attaches the town mission behaviors only after the pending intervention matches.
5. `OnMissionTick` exits immediately unless `IsActiveInCurrentMission()` succeeds.
6. `OnMissionEnded` finalizes the selected outcome and always calls `EndInterventionSceneScope` from `finally`.
7. `EndInterventionSceneScope` clears scene-local memory, ambient response events, response budgets, pending ambient requests, agent-index caches, movement throttles, and temporary combat references.
8. Post-mission summary and loot routing retain only the state required to finish the already committed aftermath, then `ClearActiveState` resets the remaining operation session.

## Semantic Action Path

1. `AfGcczShoutBridge` asks the active runtime for the current `TownAfDialoguePhase`.
2. `TownAfRuleRoutingPolicy` filters the enabled AF rules for normal occupation or atrocity combat.
3. `SiegePostprocessRuleFilter` supplies only currently eligible GCCZ candidates.
4. `TownPromptComposer` appends the low-attention decision contract after the short context sections.
5. `SiegePostprocessTagNormalizer` parses numeric tags and explicit legacy machine-tag aliases.
6. `TownPostprocessDecisionValidator` keeps at most one primary town action.
7. `TryProcessAiActionTags` rechecks the live settlement, scene, role, and action eligibility before dispatching a side effect.

The active GCCZ runtime contains no `Contains`, `IndexOf`, prefix, suffix, or regex decision over `playerText`. Historical tag names remain accepted only inside explicit machine tags through `LegacyTownTagAdapter`. `SetsSettlementCivilianGatherProfile.ShouldHandleExplicitPlayerCommand` is an older ordinary SETS town/village command path outside an active GCCZ aftermath; it is not a GCCZ action-tag selector and is deliberately not changed by this town-only refactor.

The abandoned `SiegePostprocessFrequencyProfile` keyword list and its AF no-op frequency facade were removed. They no longer throttled any request, but retaining them would have left a misleading second interpretation path beside the semantic candidate-tag contract.

## Roles And Memory

- `TownDialogueRoleClassifier` classifies accompanying allied nobles, noble prisoners, player companions, settlement notables/headmen, ordinary soldiers, and ordinary civilians.
- Named heroes continue through AF persistent personal memory.
- `TownSceneMemorySession` owns scene-local ordinary soldier and civilian memory and clears it on mission exit.
- `SettlementRuleMemoryStore` owns the last three generated ruler memories for towns only; `GcczTownRuleMemoryRuntimeBridge` owns Bannerlord save and encyclopedia integration.
- Existing AF player biography remains the sole player-history implementation.

## Operation And Outcome State

- `TownOperationLedger` is the source of target claims, acquired value, unique deaths, committed progress, and duplicate prevention for plunder, massacre, and colonization.
- `TownColonizationStateMachine` owns pending, stopped, ready, and committed colonization transitions.
- `TownPlunderConsequenceDelta` and `TownMassacreConsequenceDelta` convert ledger progress into incremental consequence deltas.
- `TownOutcomeCompatibilityProfile` snapshots the legacy full positive and negative anchors.
- `TownSettlementEffectPlan` converts plunder, massacre, final outcome, relief, civic, and local-conflict decisions into immutable settlement-effect batches without referencing Bannerlord types.
- `SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs` is the single Bannerlord mutation boundary for immediate town public trust, bound-village public trust, notable relation, notable trust, loyalty, security, and relief-food effects. It also owns the final colonization loyalty reset and finalized prosperity/debuff bridge.
- Operation controllers still decide when a ledger delta may commit, but they no longer repeat or directly apply the settlement/notable mutations. SETS owner-specific incident penalties remain a separate pre-existing SETS path because they target a specific clan leader rather than the GCCZ town-effect batch.

## Save Surface

- Town ruler memory uses versioned serialized records through `SettlementRuleMemorySaveCodec` and `GcczTownRuleMemoryRuntimeBridge`.
- Recruitment slowdown uses `TownRecruitmentSlowdownSaveMigration`.
- In-progress colonization uses `TownColonizationSnapshotCodec` plus guarded load recovery in `SiegeAiInterventionBehavior.TownColonizationLoadRecovery.cs`.
- Missing, older, and malformed individual records fail closed or migrate to defaults; completed operation markers prevent repeated settlement commits.

## Global Registration And Isolation Points

- Campaign event listeners are registered on the campaign behavior, but mission handlers return unless a matching pending or active intervention exists.
- Harmony patches remain installed for the campaign lifetime. Every town-sensitive prefix or postfix checks `IsOccupationSceneActiveForExternal` or a stricter matching-settlement guard and otherwise falls through to original behavior.
- `AfGcczShoutBridge` does not activate town prompt or response policy outside a live town dialogue phase.
- `ShoutBehavior` clears speech, immediate-reaction, postprocess, movement, and scene-history queues on mission removal and mission end.
- The preserved AD1259 player-export directory is outside this inventory and must remain untracked.

## Automated Boundary Evidence

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify_gccz_town_refactor.ps1
```

The verifier fails when:

- a reusable C# file is missing, added only to the fused copy, or differs from the standalone source after line-ending normalization;
- a mirrored GCCZ player resource or handoff document differs;
- a reusable core filename or namespace is duplicated in the AF root;
- an active GCCZ adapter starts classifying `playerText` with fixed dialogue keywords;
- immediate GCCZ public-trust or notable-trust mutation escapes the dedicated settlement-effect adapter;
- the required active-stage and mission-exit cleanup seams disappear.

This verifier is an architecture regression guard. It does not replace the standalone behavior tests, compatibility snapshots, dual Bannerlord builds, or in-game acceptance sequence.
