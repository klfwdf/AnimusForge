# Source extraction slices

The first independent GCCZ code slice is intentionally small and dependency-free:

- `src/AnimusForge.SiegeAftermathIntervention/SiegeInterventionOutcome.cs`
- `src/AnimusForge.SiegeAftermathIntervention/SiegeInterventionActionKind.cs`
- `src/AnimusForge.SiegeAftermathIntervention/SiegeActionTagCatalog.cs`

Why this slice first:

- It is copied from live fused behavior vocabulary, not invented from scratch.
- It gives future AF bridge code a stable vocabulary without depending on Bannerlord runtime types.
- It can be compiled independently before touching the fused `new-` runtime.

Next safe extraction candidates:

1. Postprocess tag allow/filter rules.
2. Outcome transition guard (`搜掠` reversible, `血洗/殖民` irreversible).
3. Shared relief-pool accounting model without Bannerlord item references.

## Second slice: outcome-rule core

Added:

- `SiegeInterventionActionRuleDecision.cs`
- `SiegeInterventionActionRules.cs`

Preserved invariants from the current fused implementation:

- 宽恕/救济/宣抚/盟誓 are the mercy track.
- 搜掠 is destructive but reversible before escalation.
- 血洗 cannot downgrade back to 搜掠/宽恕/救济; it can still upgrade to 殖民.
- 殖民 can be triggered directly at the start or as an upgrade after 血洗.
- Mercy-track choices can stop/rewrite reversible 搜掠.
- Mercy-track choices are blocked after 血洗/殖民 or pending devastate aftermath.
- Destructive choices are no longer blocked by same-culture policy; same-culture only affects soldier tone.
- 殖民 requires allied-soldier context.

`SiegeMercyTrackTransitionProfile` now owns mercy-track transition UI wording for blocked post-destruction actions and reversible-plunder stop notices. Fused AF still owns live destructive-lock checks, plunder state clearing, logging, and display side effects.

## Standalone tests

Added `tests/AnimusForge.SiegeAftermathIntervention.Tests`, a no-third-party console test project that verifies:

- English/Chinese action tag parsing.
- Canonical Chinese action tag normalization.
- Mercy-track / destructive / irreversible classification.
- Reversible 搜掠 override by mercy-track actions.
- 血洗/殖民 downgrade blocking.
- Same-culture destructive blocking removal.
- 殖民 allied-soldier context requirement.

## ModuleData rule snippet

Added `ModuleData/siege_intervention_aftermath.rule.json` extracted from the current fused `new-` runtime `RuleBehaviorPrompts.json` entry.

The standalone tests now validate:

- snippet container is `RulePrompts`;
- rule id is `siege_intervention_aftermath`;
- `TriggerKeywords` stays empty;
- instruction keeps the runtime-injection guard;
- all nine canonical postprocess tags are present.


## Tag order and aliases

`SiegeActionTagCatalog` now owns the canonical tag order and alias table, so AF bridge code does not need duplicate switch statements for action tag normalization.

The same catalog now also owns dependency-free ACTION tag regex patterns for each GCCZ action and the any-action matcher. Fused AF still compiles `Regex` instances and performs replacements, but no longer hard-codes the tag vocabulary in the bridge file.


## Postprocess-rule filtering

`SiegePostprocessRuleFilter` owns dependency-free filtering for active scene postprocess tags: destructive policy gate, irreversible outcome downgrade gate, and pending/completed soldier appeasement visibility. AF adapters pass runtime booleans and keep side effects outside the core.


## Fallback postprocess rule catalog

`SiegePostprocessRuleCatalog` owns the dependency-free fallback postprocess rule definitions. Fused AF maps these definitions to `PostprocessRuleEntry` instead of keeping rule wording inside `SiegeAiInterventionBehavior`.

The same catalog now also owns the GCCZ passive rule id and injected-rule marker. Fused AF still injects/reads rule blocks through current AF prompt and postprocess plumbing, but no longer hard-codes `siege_intervention_aftermath` in bridge source files.


## Postprocess context builder

`SiegePostprocessContextBuilder` owns dependency-free formatting for postprocess runtime facts plus speaker identity labels for allied soldiers, civilians, and other scene NPCs. The AF adapter now only gathers live objects and passes `SiegePostprocessContextFacts`.


## Postprocess tag normalizer

`SiegePostprocessTagNormalizer` owns dependency-free AI output tag normalization: allowed-tag gating, English/Chinese alias matching, fixed canonical output order, duplicate removal, and last mood tag preservation. The fused AF adapter now only supplies the runtime-allowed postprocess rule tags and keeps logging/exception handling at the bridge.


## Postprocess action effects

`SiegePostprocessActionEffectProfile` owns dependency-free trigger source/detail text used after postprocess action tags mutate GCCZ aftermath state. Fused AF still owns regex matching, live target checks, and the actual Bannerlord state mutations.


## Shared civilian relief pool facts

`SiegeSharedReliefPoolFacts` and `SiegeSharedReliefPoolFormatter` own dependency-free material-pool checks and context wording for AF give-item/give-gold civilian relief. Fused AF still records Bannerlord `ItemObject` references and inventory/UI side effects, but the large behavior file no longer duplicates pool description logic.

Negative-outcome shared-pool refund message, memory text, and returned-gold source construction now also live in `SiegeSharedReliefPoolFormatter`; fused AF still mutates player gold/items and only passes the collected return summary/source reason into GCCZ core wording.

Shared-pool applied-effect UI text now also lives in `SiegeSharedReliefPoolFormatter`; fused AF still resolves the live pool description and displays the resulting Bannerlord message.

Shared-pool capture UI text now also lives in `SiegeSharedReliefPoolFormatter`; fused AF still supplies live Bannerlord item names/ids, but GCCZ core now formats gold/item amount lines, joins transfer/refund summaries, owns the unavailable-stats fallback, and builds the user-facing "计入全城平民共享安抚物资" message.

`SiegeSharedReliefPoolEffectCalculator` and `SiegeSharedReliefPoolEffectDeltas` now own the gold/food/material-value formula for newly applied shared relief material. Fused AF still mutates Bannerlord town food stocks and applies the resulting settlement deltas.


## Settlement effect profile

`SiegeSettlementEffectProfile` now owns dependency-free reason codes for GCCZ settlement-effect mutations. Fused AF still performs Bannerlord settlement, town, and reward-system side effects, but no longer hard-codes the positive public-trust reason in the large behavior file.


## Outcome message de-duplication

`SiegeOutcomeMessageDeduplicator` owns dependency-free per-outcome message-key state. Fused AF still displays Bannerlord `InformationMessage` UI, but reset/show-once decisions now route through GCCZ core instead of keeping duplicate state fields in `SiegeAiInterventionBehavior`.


## Postprocess outcome text

`SiegePostprocessOutcomeFacts` and `SiegePostprocessOutcomeTextBuilder` own dependency-free current-outcome wording for the postprocess context. Fused AF now passes only live outcome flags and pending aftermath name before `SiegePostprocessContextBuilder` formats the full context.


## Civilian gather context

`SiegeCivilianGatherContextFacts` and `SiegeCivilianGatherContextBuilder` own dependency-free 民众召集状态 wording for runtime prompt/postprocess context. Fused AF still counts live mission agents and tracks gather/formation flags.

`SiegeCivilianGatherUiProfile` now owns dependency-free civilian-gather UI and memory wording for prepared-civilian count, messenger propagation, messenger additions, formation-control queueing, and formation-ready completion. It also owns the immediate messenger speech fact prompt and fallback messenger/target names. Fused AF still owns mission agents, timing, formation control, `ShoutBehavior` triggering, and display/recording side effects.

`SiegeCivilianGatherInteractionProfile` now owns dependency-free runtime parameters and source codes for 民众召集 messenger speech count, talk duration, approach/follow timing, fallback timing, soldier messenger ratio/source codes, messenger speed, formation-control batching, gather-mark/seed/fallback/messenger-return/formation-queue source construction, target waiting, messenger movement, follower preparation, interaction release, fake-talk follower completion, fallback follower marking, and formation-control reasons/order readiness. Fused AF still owns live mission-agent selection, `ShoutBehavior` triggering, movement, formation control, and side effects.

`SiegeCivilianAssemblyProfile` now owns dependency-free runtime parameters and source codes for civilian assembly target counts, scene caps, native-civilian-only assembly, forward offset, grid spacing, columns, mission-start assembly, and control-tick assembly. Fused AF still owns scene capacity checks, formation slot projection, and mission side effects.

`SiegeSceneAgentSuppressionProfile` now owns dependency-free reason codes for suppressing unsafe vanilla scene agents, protected agents, player companion scene spawns, and guard leftovers before/inside the GCCZ scene. Fused AF still owns live agent classification, `ShoutBehavior` cancellation, fade-out, and slot cleanup side effects.


## Soldier cordon profile

`SiegeSoldierCordonProfile` now owns dependency-free runtime parameters and source codes for soldier cordon radius/padding, teleport threshold, move/settle tolerances, order/look refresh timing, allied control tick, default infantry follow, spawn friendly-state restore, spawn follow, and spawn-batch order-controller priming. Fused AF still owns live soldier selection, target-slot projection, movement orders, and look-at side effects.


## Intervention memory context

`SiegeInterventionMemoryContextBuilder` owns dependency-free formatting for the remembered GCCZ event list used in runtime prompts plus the max retained memory-event count. Fused AF still records, de-duplicates, and applies trimming to memory events, but the large behavior file no longer owns the prompt context wording or count literal.


## Intervention memory event formatter

`SiegeInterventionMemoryEventFormatter` owns dependency-free single-event formatting for GCCZ memory entries, including action-tag stripping and whitespace normalization. Fused AF still owns sequencing, duplicate checks, trim application, and logging.


## Completed intervention summary

`SiegeCompletedInterventionSummaryFacts` and `SiegeCompletedInterventionSummaryBuilder` own dependency-free wording for the completed GCCZ summary shown after AF aftermath resolution. Fused AF still gathers live settlement/culture/loot facts and performs menu transitions.


## Runtime prompt profile

`SiegeRuntimePromptFacts` and `SiegeRuntimePromptProfile` own dependency-free runtime prompt wording for the active GCCZ post-siege intervention scene. Fused AF now resolves live Bannerlord agents, guard/civilian/allied-soldier flags, gather context, memory context, and outcome state, then passes those facts into the standalone prompt builder.


## Loot accounting profile

`SiegeLootAccountingProfile` owns dependency-free loot-accounting UI wording for civilian exit settlement and per-target civilian gold messages. Fused AF still owns Bannerlord gold transfer, target eligibility, random amount calculation, and display side effects.

The same profile now also owns market gold, market inventory, and civilian-spoils loot messages. Fused AF still owns Bannerlord town gold/inventory mutation, pending loot roster construction, random stack selection, and display side effects.

Direct aftermath loot status text now also lives in `SiegeLootAccountingProfile`, including direct devastate/plunder settlement notices and the credited loot summary. Fused AF still owns direct loot-screen timing and state flags.

Market-loot settlement reasons and capture ratios now also live in `SiegeLootAccountingProfile`. Fused AF still mutates Bannerlord town gold/inventory and guards one-time application, but no longer hard-codes the GCCZ plunder/massacre market-loot labels or percentages.

Civilian/hero gold amount constants and award source codes for per-target/market 搜掠/血洗 accounting now also live in `SiegeLootAccountingProfile`. Fused AF still chooses valid targets, samples random non-hero plunder amounts, applies Bannerlord gold transfer, and displays the resulting messages.

`SiegePlunderInteractionProfile` now owns dependency-free runtime parameters and source codes for 搜掠 soldier assignment, max concurrent interactions, approach distance, talk duration, allied assignment restore, and target follow operations. Fused AF still owns live mission-agent selection, movement, conversation timing application, and side effects.


## Intervention entry profile

`SiegeInterventionEntryProfile` owns dependency-free GCCZ scene-entry tooltip text and missing-scene UI text. Fused AF keeps settlement/location/menu checks while delegating the entry wording to GCCZ core.

The same profile now also owns troop-selection entry instructions, decision-policy UI, entry-failure UI, and troop-selection completion/fallback wording. Fused AF still owns Bannerlord menu callbacks and selected-roster storage; the large behavior file only calls GCCZ-owned message builders before displaying `InformationMessage`.

It now also owns the mission-entry battle-equipment notice plus auto-summoned allied troop/no-healthy-troop UI text. Fused AF still owns Bannerlord equipment mutation, troop picking, and agent spawning; only the entry/summon wording and colors are delegated.

The scene-entry campaign menu option text now also lives in `SiegeInterventionEntryProfile`; fused AF still owns the actual `AddGameMenuOption` registration and callback wiring.

Default entry troop counts plus troop-selection mission-entry, scene-cleanup, auto-enter summon, and ensure-allied-troops summon source codes now also live in `SiegeInterventionEntryProfile` as `DefaultAutoSummonCount`, `MaxSummonPerAction`, `SelectionUnavailableMissionSource`, `TroopSelectionDoneMissionSource`, `SceneEntryCleanupSource`, `AutoEnterSummonSource`, and `EnsureAlliedTroopsSummonSource`. Fused AF keeps the actual roster selection, mission opening, agent spawning, formation placement, and encounter-record side effects, but no longer hard-codes the GCCZ entry summon limits/source strings inside the large behavior file.


## Native aftermath selection policy

`SiegeAftermathResolutionKind` and `SiegeAftermathSelectionPolicy` own dependency-free severity, shared-relief return, and pending-aftermath replacement rules. Fused AF maps Bannerlord's native aftermath enum into the standalone kind, then delegates reversible plunder downgrade and devastate-lock checks to GCCZ core.


## Mission-exit outcome profile

`SiegeMissionExitOutcomeProfile` owns the dependency-free fallback decision used when the GCCZ mission exits before or after an explicit scene outcome. Fused AF now passes live plunder/massacre/repopulation/pending/policy flags into the profile, then either starts plunder or marks a native aftermath through the existing thin adapter.


## Action-tag routing policy

`SiegeActionRoutingFacts`, `SiegeActionRoutingDecision`, and `SiegeActionRoutingPolicy` own dependency-free routing for postprocess action batches. Fused AF now passes raw action text plus live target/material/lock facts, then delegates destructive detection, mercy-track availability, soldier relief downgrade, and soldier positive-action capping to GCCZ core.


## Relief choice profile

`SiegeReliefChoiceProfile` owns dependency-free relief/appeasement deltas, message text, memory text, soldier appeasement reason, shared-pool effect reason, stop-reversible-plunder reason, and destructive-lock display action name. Fused AF now delegates relief-profile selection to GCCZ core, then applies Bannerlord settlement, inventory, UI, and memory side effects through a thin adapter.

Relief validation messages for wrong soldier targets and missing AF shared material now also live in `SiegeReliefChoiceProfile`, replacing the remaining hard-coded validation text in the fused AF adapter.


## Civic choice profile

`SiegeCivicChoiceProfile` owns dependency-free deltas, notable effects, message text, memory text, gather source, soldier appeasement reason, shared-pool effect reason, stop-reversible-plunder reasons, and destructive-lock display action names for 安民宣抚 and 归心盟誓. Fused AF now delegates civic-profile selection to GCCZ core, then applies Bannerlord settlement, notable, gather, UI, and memory side effects.


## Mercy choice profile

`SiegeMercyChoiceProfile` owns dependency-free stop-plunder reason, soldier appeasement reason, shared-pool effect reason, message text, memory text, loyalty bonus, and destructive-lock display action name for the simple 宽恕 choice. Fused AF now delegates those values to GCCZ core while preserving AF-side aftermath, UI, memory, and settlement mutation side effects.


## Destructive choice profile

`SiegeDestructiveChoiceProfile` owns dependency-free aftermath kind, assembly source, message text, memory text, public-trust deltas, and trigger-source wording for 搜掠 and 血洗. Fused AF now maps the standalone aftermath kind back to TaleWorlds' native aftermath enum and keeps only mission, settlement, UI, and combat side effects in the AF adapter; the 搜掠 finalized trust penalty also comes from this profile.

Same-culture/policy block messages for 搜掠 and 血洗 were retired after the 2026-06-13 soldier-thinking update. Do not reintroduce a same-culture gate in `StartPlunder`, `StartMassacre`, scene entry, or postprocess action-batch routing.

Direct player-attack bloodbath trigger UI, pending-aftermath detail wording, attack-release damage source, and agent/score/non-enemy-hit bridge source codes now also live in `SiegeDestructiveChoiceProfile`. Fused AF still owns input/damage detection and combat state, but no longer formats these direct-attack messages or hard-codes the damage/hit source strings inside the large behavior file.

`SiegeMassacreInteractionProfile` now owns dependency-free runtime parameters and source codes for 血洗 civilian hide distance, hide refresh timing, soldier follow refresh, soldier target refresh, occupation follow, combat preparation, allied combat drive operations, and all-targets-down victory completion. Fused AF still owns live mission-agent routing, order timing application, hide-point projection, and combat side effects.


## Cultural repopulation profile

`SiegeCulturalRepopulationProfile` owns dependency-free 屠民迁殖 request wording, pending/completion message text, notable-result wording, and the standalone devastate aftermath kind. Fused AF keeps culture resolution and settlement/notable mutation, while routing the request and completion bridge through this profile.

Policy and target-validation messages for 屠民迁殖 now also live in `SiegeCulturalRepopulationProfile`, replacing the remaining hard-coded request validation text in the fused AF adapter.

Target-culture source labels, display formatting, and apply source codes now also live in `SiegeCulturalRepopulationProfile`. Fused AF still resolves Bannerlord `CultureObject` instances and invokes the settlement mutation hook, but the user-facing source labels, fallback text, and repopulation apply source strings are owned by the standalone GCCZ core.


## Soldier appeasement profile

`SiegeSoldierAppeasementProfile` owns dependency-free 安兵 need-warning text, success text, and the fallback morale-penalty amount/text. Fused AF still validates the target soldier and mutates Bannerlord party morale, but the large behavior file no longer owns the profile wording.

The 安兵 target-validation message now also lives in `SiegeSoldierAppeasementProfile`, replacing another hard-coded GCCZ UI string in the fused AF adapter.


## Completion UI profile

`SiegeInterventionCompletionUiProfile` owns dependency-free final completion/encounter-exit UI wording, including completed-menu fallbacks, the continue menu option text, massacre-victory message/quick text, final completed-aftermath labels/message, loot-settlement summary, and leave-encounter quick text. Fused AF still maps native aftermath enums, gathers loot totals, and performs Bannerlord menu/display side effects.

## Direct aftermath campaign source profile

`SiegeDirectAftermathSourceProfile` now owns the campaign-tick, native-menu intercept, external-pump, direct-script phase, and direct loot-screen defer source codes used by direct AF massacre/plunder aftermath scripts. Fused AF still owns campaign tick scheduling, loot-screen state, and encounter transitions, but no longer hard-codes those source strings in the large behavior file.

## Aftermath transition source profile

`SiegeAftermathTransitionSourceProfile` now owns the mission-end finalization, session-load runtime guard reset, post-mission encounter finish retry, done-menu continue finish, native menu initialization, campaign-tick native menu detection, and native devastate summary continuation source codes. Fused AF still owns mission lifecycle, menu switching, loot-screen timing, and encounter transitions, but no longer hard-codes those transition source strings in the large behavior file.

## Native bridge source profile

`SiegeNativeBridgeSourceProfile` now owns source codes used by native flee suppression, order UI readiness, order-team resolution, commandable-agent probing, control-tick order-controller priming, order-controller binding, and native order-view injection. Fused AF still owns Harmony patches, mission views, native order-controller binding, and live agent side effects.

## Aftermath menu profile

`SiegeAftermathMenuProfile` now owns menu identifiers and the contextual-summary source marker for GCCZ aftermath entry, native settlement-taken routing, and contextual summary routing. Fused AF still owns Bannerlord menu registration, switching, and live menu side effects.
