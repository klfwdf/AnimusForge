# AnimusForge.SiegeAftermathIntervention

Standalone GCCZ source area.

Current first extraction slice:

- `SiegeInterventionOutcome` mirrors the existing fused outcome state names.
- `SiegeInterventionActionKind` mirrors the existing postprocess action vocabulary.
- `SiegeActionTagCatalog` preserves current English/Chinese action tag parsing and normalizes to the Chinese canonical tags already used by `SiegeAiInterventionBehavior`.

This slice has no Bannerlord, Harmony, or AF dependencies. It is safe to build independently and later bridge from the fused `AnimusForge` namespace.

Second extraction slice:

- `SiegeInterventionActionRules` preserves the current outcome-routing invariants: mercy/relief/inspire/oath can override reversible plunder, but cannot downgrade massacre or cultural repopulation; destructive actions are no longer blocked solely by same-culture policy; cultural repopulation requires allied-soldier context.
- `SiegeInterventionActionRuleDecision` returns a dependency-free decision for future AF adapters to translate into UI messages, memory records, and Bannerlord effects.


## Tag order and aliases

`SiegeActionTagCatalog` now owns the canonical tag order and alias table, so AF bridge code does not need duplicate switch statements for action tag normalization.


## Postprocess-rule filtering

`SiegePostprocessRuleFilter` owns dependency-free filtering for active scene postprocess tags: irreversible outcome downgrade gate and pending/completed soldier appeasement visibility. AF adapters pass runtime booleans and keep side effects outside the core; same-culture does not suppress destructive tags.


## Fallback postprocess rule catalog

`SiegePostprocessRuleCatalog` owns the dependency-free fallback postprocess rule definitions. Fused AF maps these definitions to `PostprocessRuleEntry` instead of keeping rule wording inside `SiegeAiInterventionBehavior`.


## Postprocess context builder

`SiegePostprocessContextBuilder` owns dependency-free formatting for postprocess runtime facts plus speaker identity labels for allied soldiers, civilians, and other scene NPCs. The AF adapter now only gathers live objects and passes `SiegePostprocessContextFacts`.


## Postprocess tag normalizer

`SiegePostprocessTagNormalizer` owns dependency-free AI output tag normalization for the active GCCZ scene. AF adapters pass the runtime-allowed postprocess tags; the core preserves English/Chinese alias matching, conservative single-action ordering, duplicate removal, and last mood tag preservation. The explicit `[ACTION:9]` + `[ACTION:10]` pair is normalized to colonization because action 10 is the documented bloodbath-to-colonization upgrade; unrelated ambiguous multi-action batches retain conservative priority. Allowed tags are matched exactly, so `[ACTION:1]` cannot pass an `[ACTION:10]`-only allowlist.


## Shared civilian relief pool

`SiegeSharedReliefPoolFacts` and `SiegeSharedReliefPoolFormatter` own dependency-free checks and context wording for the AF give-item/give-gold pool reserved for civilian relief in town GCCZ scenes. `SiegeSharedReliefBridgeProfile` keeps castle aftermath gifts private to the receiver. Bannerlord item objects and inventory side effects stay in the AF adapter.

Negative-outcome refund UI, memory wording, and returned-gold source construction for shared relief material also live in `SiegeSharedReliefPoolFormatter`; AF keeps only inventory/gold mutation and summary collection.

Shared-pool applied-effect UI wording also lives in `SiegeSharedReliefPoolFormatter`; AF keeps only the live pool description and Bannerlord `InformationMessage` display call.

Town actions are parsed once by `SiegeActionTagCatalog` and routed as `SiegeInterventionActionKind` values. Dialogue wording never upgrades mercy into relief; only validated scene state may deterministically downgrade an unavailable relief action to mercy or redirect a soldier morale action to an available relief pool.

`SiegeSharedReliefPoolEffectCalculator` and `SiegeSharedReliefPoolEffectDeltas` own dependency-free settlement-effect calculations for newly applied shared relief material. AF keeps the Bannerlord town food-stock mutation and settlement delta application.


## Settlement effect profile

`SiegeSettlementEffectProfile` owns dependency-free reason codes for positive GCCZ settlement-effect mutations. `SiegeSettlementOutcomeProfile` owns finalized 搜掠/血洗/殖民 settlement, bound-village, notable-relation, prosperity, loyalty, and one-year prosperity-growth debuff policy. AF adapters still own Bannerlord settlement, town, village, notable, reward-system, and save-data side effects.


## Outcome message de-duplication

`SiegeOutcomeMessageDeduplicator` owns dependency-free per-outcome message-key de-duplication. The AF adapter still displays `InformationMessage` entries, but the repeated-message state is no longer embedded directly in the large behavior file.


## Postprocess outcome text

`SiegePostprocessOutcomeFacts` and `SiegePostprocessOutcomeTextBuilder` own dependency-free current-outcome wording for the postprocess context. The AF adapter now supplies only live state flags and pending aftermath name.


## Civilian gather context

`SiegeCivilianGatherContextFacts` and `SiegeCivilianGatherContextBuilder` own dependency-free wording for runtime 民众召集状态 context. AF adapters still count Bannerlord agents and track gathering/formation flags.


## Civilian gather interaction profile

`SiegeCivilianGatherInteractionProfile` owns dependency-free runtime parameters and source codes for GCCZ 民众召集 messenger speech, follow refresh, fallback timing, approach distance, soldier messenger ratio/source codes, messenger speed, formation-control batching, gather-mark/seed/fallback/messenger-return/formation-queue source construction, target waiting, messenger movement, follower preparation, interaction release, fake-talk follower completion, fallback follower marking, and formation-control reasons/order readiness. AF adapters still own live mission-agent selection, `ShoutBehavior` triggering, movement, formation control, and side effects.


## Civilian assembly profile

`SiegeCivilianAssemblyProfile` owns dependency-free runtime parameters and source codes for GCCZ civilian assembly target counts, scene caps, native-civilian-only assembly, forward offset, grid spacing, columns, mission-start assembly, and control-tick assembly. AF adapters still own scene capacity checks, formation slot projection, and mission side effects.

`SiegeSceneAgentSuppressionProfile` owns dependency-free reason codes for suppressing unsafe vanilla scene agents, protected agents, player companion scene spawns, and guard leftovers. AF adapters still own live agent classification, `ShoutBehavior` cancellation, fade-out, and slot cleanup side effects.


## Soldier cordon profile

`SiegeSoldierCordonProfile` owns dependency-free runtime parameters and source codes for GCCZ soldier cordon radius, padding, teleport threshold, movement tolerance, settle tolerance, order/look refresh timing, allied control tick, default infantry follow, spawn friendly-state restore, spawn follow, and spawn-batch order-controller priming. AF adapters still own live soldier selection, target-slot projection, movement orders, and look-at side effects.


## Intervention memory context

`SiegeInterventionMemoryContextBuilder` owns dependency-free formatting for the per-scene GCCZ memory context appended to AF prompts plus the max retained memory-event count. AF adapters still own event collection, de-duplication, trim application, and logging.


## Intervention memory event formatter

`SiegeInterventionMemoryEventFormatter` owns dependency-free formatting for one GCCZ memory event: kind fallback, detail fallback, action-tag stripping, and whitespace normalization. AF adapters still own sequencing, duplicate checks, trim application, and logging.


## Completed intervention summary

`SiegeCompletedInterventionSummaryFacts` and `SiegeCompletedInterventionSummaryBuilder` own dependency-free wording for the post-intervention completion summary. AF adapters still resolve Bannerlord settlement/culture/live loot facts and perform menu/encounter transitions.


## Loot accounting profile

`SiegeLootAccountingProfile` owns dependency-free loot UI wording, market-loot ratios, civilian/hero gold amount constants, and award source codes for GCCZ 搜掠/血洗 accounting. AF adapters still own Bannerlord gold/item mutation, target eligibility, random sampling, and display side effects.


## Plunder interaction profile

`SiegePlunderInteractionProfile` owns dependency-free runtime parameters and source codes for GCCZ 搜掠 soldier assignment, approach distance, concurrent interactions, talk duration, allied assignment restore, and target follow operations. AF adapters still own live mission-agent selection, movement, timing application, and side effects.


## Intervention entry profile

`SiegeInterventionEntryProfile` owns dependency-free scene-entry tooltip, missing-scene UI wording, troop-selection mission-entry, scene-cleanup, auto-enter summon, and ensure-allied-troops summon source codes for the GCCZ intervention menu. AF adapters still resolve Bannerlord settlements, locations, menu args, and display side effects.


## Native aftermath selection policy

`SiegeAftermathResolutionKind` and `SiegeAftermathSelectionPolicy` own dependency-free severity and replacement rules for pending native aftermath choices. AF adapters map Bannerlord's `SiegeAftermath` enum into this core and keep inventory/UI side effects outside the policy.


## Action-tag routing policy

`SiegeActionRoutingFacts`, `SiegeActionRoutingDecision`, and `SiegeActionRoutingPolicy` own dependency-free routing for postprocess action batches: destructive detection, soldier-mediated direct-player-response gating, mercy-track availability, soldier relief downgrade without shared material, and soldier positive-action capping to relief when a shared pool exists.


## Relief choice profile

`SiegeReliefChoiceProfile` owns dependency-free deltas, messages, memory text, shared-pool effect reasons, stop-reversible-plunder reason, and the destructive-lock display action name for the current relief/appeasement choice. AF adapters still apply Bannerlord settlement, inventory, UI, and memory side effects.

Relief validation UI text for invalid targets or missing shared material also lives on this profile, so the AF adapter does not keep hard-coded GCCZ wording in `SiegeAiInterventionBehavior`.


## Civic choice profile

`SiegeCivicChoiceProfile` owns dependency-free deltas, notable effects, messages, memory text, gather source, shared-pool effect reasons, stop-reversible-plunder reasons, and destructive-lock display action names for 安民宣抚 and 归心盟誓. AF adapters still apply Bannerlord settlement, notable, gathering, UI, and memory side effects.


## Mercy choice profile

`SiegeMercyChoiceProfile` owns dependency-free stop-plunder reason, soldier appeasement reason, shared-pool effect reason, message, memory text, loyalty bonus, and destructive-lock display action name for the simple 宽恕 choice. AF adapters still apply Bannerlord aftermath, shared-pool, UI, memory, and settlement side effects.


## Destructive choice profile

`SiegeDestructiveChoiceProfile` owns dependency-free aftermath kind, assembly source, message text, memory text, and massacre source classification for GCCZ plunder and massacre choices. Finalized settlement/notable penalties live in `SiegeSettlementOutcomeProfile`. AF adapters still apply Bannerlord aftermath, troop, mission, UI, settlement, damage, and memory side effects.

Same-culture destructive blocking has been removed from this profile. AF adapters must not block GCCZ entry, 搜掠, 血洗, or 屠民迁殖 solely because player/soldier/settlement culture matches; same-culture should only affect soldier tone.


## Local player attack profile

`SiegeLocalAttackProfile` owns dependency-free bridge source codes, UI wording, and memory wording for player strikes against one NPC during an active GCCZ scene. AF adapters must treat these hits as local flee/resist conflict unless massacre or cultural repopulation has already been explicitly triggered by soldier-mediated action tags.

`SiegeRegionalConflictProfile` owns dependency-free town trust-delta, 24m-diameter area de-duplication, generic player-facing notice text, and positive-effect-debt policy for regional civilian conflict. The fused AF adapter records the live first-hit/down center for each debt area, applies -1 settlement public trust once per area, shows only a generic regional-conflict panic prompt without backend values, then accumulates backend conflict debt that reduces later positive public-trust, loyalty, relation, and trust gains.


## Massacre interaction profile

`SiegeMassacreInteractionProfile` owns dependency-free runtime parameters and source codes for GCCZ 血洗 civilian hide distance, hide refresh timing, soldier follow refresh, soldier target refresh, rare/limited civilian resistance selection, panic rout, occupation follow, combat preparation, allied combat drive, and all-targets-down victory completion. AF adapters still own live mission-agent routing, order timing application, hide-point projection, flee/fight behavior assignment, and combat side effects.

`SetsOwnedSettlementMassacreProfile` separately owns the SETS-only 100-follower shout policy and the AI-decided start/stop tags for player-owned or ruler-attached town scenes. It intentionally does not make ordinary GCCZ massacre reversible: only the SETS entry-scene combat can be halted, and stopping does not undo casualties or the incident state.


`SiegeAgentWallRescueProfile` owns dependency-free thresholds for temporary native movement rescue when a GCCZ scene NPC appears pinned against collision while still far from its target. The fused AF adapter may reissue the same target with Bannerlord scripted movement flags for a short window, but ordinary AF scenes must not use this rescue.

## Cultural repopulation profile

`SiegeCulturalRepopulationProfile` owns dependency-free aftermath kind, massacre trigger wording, request memory text, pending UI message, completion UI message, and apply source codes for 屠民迁殖. AF adapters still resolve Bannerlord cultures, mutate settlements/villages/notables, and run mission/combat side effects.

屠民迁殖 target validation UI text also lives on this profile, keeping the fused AF handler limited to allied-soldier checks. Same-culture is not a policy block.

## Constructive town culture administration

`TownConstructiveCultureChangePolicy` owns the dependency-free eligibility matrix for ordinary town culture administration. It requires an active GCCZ town stage, a different player-governance target culture, a direct response from an authorized role, and no active massacre or colonization state.

`TownConstructiveCultureChangeTextProfile` formats localized prompt context, completion UI, and scene memory. The AF adapter may mutate only the current town culture and refresh settlement rule memory; it must not modify bound villages, notables, operation ledgers, pending aftermath, rewards, penalties, or completion requirements. Destructive cultural repopulation remains a separate action and state machine.

## Town role reaction guidance

`TownPromptComposer` injects the active six-role reaction matrix from `GcczTownPrompt.zh-CN.json`. AF continues to supply live named or unnamed personality, relationship, identity, knowledge, and memory facts. The GCCZ resource sets the reaction priority, keeps shared culture secondary, and varies dialogue expression without changing action eligibility or settlement outcomes.


## Soldier appeasement profile

`SiegeSoldierAppeasementProfile` owns dependency-free need-warning text, memory text, message text, colors, and morale penalty amount for 安兵 and the fallback military morale penalty. AF adapters still validate the target, mutate Bannerlord party morale, and display UI/memory side effects.

安兵 target-validation UI text also lives on this profile, keeping the fused AF adapter limited to the live allied-soldier check and display side effect.

## Direct aftermath source profile

`SiegeDirectAftermathSourceProfile` owns dependency-free source codes for direct AF aftermath campaign tick scripts, native-menu intercepts, external pump fallbacks, direct-script phase transitions, and direct loot-screen defer reasons. AF adapters still own campaign tick timing, loot-screen state, and encounter transitions.

## Aftermath transition source profile

`SiegeAftermathTransitionSourceProfile` owns dependency-free source codes for mission-end aftermath finalization, session-load runtime guard resets, post-mission encounter finish retries, done-menu continue finish, native menu initialization, campaign-tick native menu detection, and native devastate summary continuation. AF adapters still own mission lifecycle, menu switching, loot-screen timing, and encounter side effects.

## Native bridge source profile

`SiegeNativeBridgeSourceProfile` owns dependency-free source codes for native flee suppression, order UI readiness, order-team resolution, commandable-agent probing, control-tick order-controller priming, order-controller binding, and injected native order views. AF adapters still own Harmony patches, mission views, and live agent/order side effects.

## Aftermath menu profile

`SiegeAftermathMenuProfile` owns dependency-free menu identifiers and contextual-summary source marker for GCCZ aftermath entry, native settlement-taken routing, and contextual summary routing. AF adapters still own Bannerlord menu registration, switching, and live menu side effects.
