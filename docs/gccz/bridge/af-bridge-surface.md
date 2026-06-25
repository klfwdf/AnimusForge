# AF bridge surface for GCCZ / 攻城处置

This file records the current AF-facing bridge surface so GCCZ can be isolated without losing the working integration.

## Registration points in fused `new-`

- `SubModule.OnBeforeInitialModuleScreenSetAsRoot()` calls `SiegeAftermathPatchBootstrap.Apply(harmony)` to register supplemental native aftermath guards for `SwitchToMenu`, native menu init, contextual-summary continue, and `GameStateManager.OnTick` loot pumps.
- `SubModule.OnGameStart()` / campaign starter path registers `new SiegeAiInterventionBehavior()`.
- `Patch_GameMenu_ActivateGameMenu.Prefix()` first lets GCCZ intercept direct massacre/plunder/native aftermath menus, then falls through to normal AF encounter redirect logic.
- Current AF086 bridge ships a small `Patch_SiegeAftermath_AFIntervention.cs` AF adapter file. Native aftermath routing is handled by this file, `Patch_GameMenu_ActivateGameMenu.cs`, and the guarded menu/encounter helpers inside `SiegeAiInterventionBehavior.cs`; GCCZ policy/rules remain outside the AF patch file.

## Current public/internal bridge methods called by AF-side code

Keep these as the first adapter seam when splitting source. Their names may remain as compatibility facades in `new-` while implementations move into a separate GCCZ namespace.

### Prompt / postprocess

- `BuildRuntimePromptForAgent(...)`
- `BuildRuntimePromptForPromptContext(...)`
- `ShouldRunSiegeInterventionPostprocessForExternal()`
- `BuildRuntimePostprocessRulesForExternal()`
- `BuildRuntimePostprocessContextForExternal(int targetAgentIndex)`
- `NormalizeSiegeInterventionPostprocessTagsForExternal(string raw, List<PostprocessRuleEntry> rules)`
- `TryProcessAiActionTags(...)`
- `TryProcessPlayerInstruction(...)`

### Scene / mission / menu guards

- `IsOccupationSceneActiveForExternal()`
- `IsInterventionAlliedSoldierForExternal(...)`
- `ShouldForceAllowInterventionMissionExitForExternal()`
- `ShouldRedirectResolvedAftermathMenuForExternal(string menuId)`
- `TryHandleNativeAftermathMenuInitForExternal(string source)`
- `TryHandleNativeAftermathSummaryContinueForExternal(string source)`
- `TryHandleDirectMassacreAftermathMenuForExternal(string menuId, string source)`
- `TryHandleDirectPlunderAftermathMenuForExternal(string menuId, string source)`
- `TryPumpDirectMassacreAftermathScriptForExternal(string source)`
- `TryPumpDirectPlunderAftermathScriptForExternal(string source)`

### AF give / relief capture

- `ShouldCapturePlayerGiveForSharedCivilianReliefForExternal()`
- `RecordSharedCivilianReliefTransferForExternal(...)`

### Order UI / command adapters

- `FilterInterventionNativeVisualOrdersForExternal(...)`
- `ResolveInterventionPlayerCommandTeamForExternal(...)`
- `EnsureInterventionCommandUiReadyForExternal(...)`
- `InterventionPlayerHasCommandableAgentsForExternal(...)`
- `ShouldInjectInterventionOrderViewsForExternal(...)`
- `IsNativeOrderControllerReadyForExternal(...)`
- `TryResolveNativeOrderControllerForExternal(...)`
- `TryBindNativeOrderControllerForExternal(...)`
- `NativeOrderControllerHasSelectedFormationsForExternal(...)`

## Isolation target

Preferred future shape:

```text
AnimusForge.SiegeAftermathIntervention          # GCCZ core namespace / standalone code
AnimusForge.SiegeAftermathIntervention.Adapter  # thin AF/Bannerlord bridge
AnimusForge                                     # compatibility facades only inside fused AF tree
```

Do not move all code at once. First extract stable data/rules/state helpers, then route the existing facade methods to the extracted code one slice at a time.

## 2026-06-08 fused runtime bridge seed

The fused AF test tree now has a real isolated GCCZ source area:

- `G:\AFMOD\new-\AnimusForge.SiegeAftermathIntervention\`

First live bridge in `G:\AFMOD\new-\SiegeAiInterventionBehavior.cs`:

- action-tag classification in runtime postprocess-rule filtering uses `SiegeActionTagCatalog` and `SiegeInterventionActionRules`;
- postprocess tag normalization uses the standalone tag catalog while preserving the previous fixed canonical output order;
- destructive/irreversible outcome locking uses `SiegeInterventionActionRules.HasDestructiveOutcomeLocked` through a small AF-state adapter;
- destructive-tag routing and mercy-track downgrade detection now use the standalone action classifier; the old same-culture destructive blocker is retired and must not be reintroduced for GCCZ.

This is intentionally not a wholesale rewrite: AF/Bannerlord side effects, mission state, settlement mutation, and UI messages stay in the AF adapter until the next extraction slices are verified.


Follow-up isolation: canonical tag order and alias table now live in `SiegeActionTagCatalog`, removing duplicate switch helpers from the AF adapter.


Follow-up isolation: ACTION tag regex patterns now also live in `SiegeActionTagCatalog`; AF keeps only the compiled `Regex` instances and replacement side effects while GCCZ core owns the tag vocabulary/pattern strings.


Follow-up isolation: postprocess-rule filtering now lives in `SiegePostprocessRuleFilter`; `SiegeAiInterventionBehavior` passes only runtime booleans and no longer duplicates destructive/mercy/安兵 tag classification.


Follow-up isolation: fallback postprocess rules now live in `SiegePostprocessRuleCatalog`; fused AF maps them to `PostprocessRuleEntry` and no longer stores rule wording in `SiegeAiInterventionBehavior`.


Follow-up isolation: GCCZ passive rule id and injected-rule marker now also live in `SiegePostprocessRuleCatalog`; AF keeps prompt-rule injection, preprocess-hit checks, and postprocess routing while GCCZ core owns the rule id/marker strings.


Follow-up isolation: postprocess context text and speaker identity labels now live in `SiegePostprocessContextBuilder`; fused AF gathers live facts into `SiegePostprocessContextFacts` and delegates formatting plus identity-label selection to GCCZ core.


Follow-up isolation: postprocess tag normalization now lives in `SiegePostprocessTagNormalizer`; `SiegeAiInterventionBehavior.NormalizeSiegeInterventionPostprocessTagsForExternal(...)` is a thin bridge that passes the runtime-allowed rule tags and delegates alias matching, canonical ordering, de-duplication, and mood preservation to GCCZ core.


Follow-up isolation: shared civilian relief-pool context now uses `SiegeSharedReliefPoolFacts` and `SiegeSharedReliefPoolFormatter`; `SiegeAiInterventionBehavior` still owns Bannerlord inventory, UI, and settlement effects while delegating dependency-free material checks and context text.


Follow-up isolation: negative-outcome shared-pool refund UI, memory wording, and returned-gold source construction now also use `SiegeSharedReliefPoolFormatter`; AF keeps inventory/gold mutation and summary collection.


Follow-up isolation: shared-pool applied-effect UI now also uses `SiegeSharedReliefPoolFormatter`; AF keeps live pool description and display side effects.


Follow-up isolation: shared-pool capture/refund summaries now also use `SiegeSharedReliefPoolFormatter`; AF keeps Bannerlord gold/item mutation, live item name lookup, and display side effects while GCCZ core owns gold/item amount lines, summary joining, unavailable-stats fallback, and captured-transfer wording.


Follow-up isolation: newly applied shared-pool settlement-effect deltas now use `SiegeSharedReliefPoolEffectCalculator`; AF keeps Bannerlord town food-stock mutation and settlement application.


Follow-up isolation: positive settlement public-trust reason codes now use `SiegeSettlementEffectProfile`; AF keeps Bannerlord settlement, town, and reward-system mutation calls.


Follow-up isolation: outcome message de-duplication now uses `SiegeOutcomeMessageDeduplicator`; AF remains responsible for `InformationMessage` display while GCCZ core tracks per-outcome show-once keys.


Follow-up isolation: postprocess current-outcome wording now uses `SiegePostprocessOutcomeFacts` and `SiegePostprocessOutcomeTextBuilder`; AF supplies live flags and pending aftermath name while GCCZ core owns the context text decision.


Follow-up isolation: civilian gather runtime context now uses `SiegeCivilianGatherContextFacts` and `SiegeCivilianGatherContextBuilder`; AF keeps live agent counting and gather/formation flags while GCCZ core owns the 民众召集状态 wording.


Follow-up isolation: civilian gather UI/memory now uses `SiegeCivilianGatherUiProfile`; AF keeps mission-agent tracking, messenger/formation state, `ShoutBehavior` triggering, and side effects while GCCZ core owns prepared-count, messenger, queue, ready wording, immediate messenger speech prompt, and fallback names.


Follow-up isolation: civilian gather interaction timing, formation-control parameters, gather-mark/seed/fallback/messenger-return/formation-queue source construction, soldier messenger source codes, interaction release/fake-talk source codes, fallback follower source, and formation-control reason/order-readiness strings now use `SiegeCivilianGatherInteractionProfile`; AF keeps live mission-agent selection, `ShoutBehavior` triggering, movement, formation control, and side effects while GCCZ core owns the runtime constants and source-code strings.


Follow-up isolation: civilian assembly target counts, scene caps, native-civilian-only assembly, grid layout, mission-start assembly source, and control-tick assembly source now use `SiegeCivilianAssemblyProfile`; AF keeps scene capacity checks, formation slot projection, and mission side effects while GCCZ core owns the runtime/source constants.

Follow-up isolation: scene-agent suppression reasons now use `SiegeSceneAgentSuppressionProfile`; AF keeps live agent classification, `ShoutBehavior` cancellation, fade-out, and slot cleanup side effects while GCCZ core owns unsafe/criminal/protected/player-companion/guard removal reason codes.


Follow-up isolation: soldier cordon positioning, refresh parameters, allied/default-follow source codes, spawn friendly-state restore source, spawn-follow source codes, and spawn-batch order-controller source now use `SiegeSoldierCordonProfile`; AF keeps live soldier selection, target-slot projection, movement orders, and look-at side effects while GCCZ core owns the runtime/source constants.


Follow-up isolation: intervention memory context formatting and the max retained memory-event count now use `SiegeInterventionMemoryContextBuilder`; AF keeps event collection, de-duplication, trim application, and logging while GCCZ core owns the prompt context wording and count constant.


Follow-up isolation: single memory-event formatting now uses `SiegeInterventionMemoryEventFormatter`; AF keeps sequencing, duplicate checks, trim application, and logging while GCCZ core owns kind/detail fallback, tag stripping, and whitespace normalization.


Follow-up isolation: completed intervention summary now uses `SiegeCompletedInterventionSummaryFacts` and `SiegeCompletedInterventionSummaryBuilder`; AF keeps live fact collection and menu transitions while GCCZ core owns completion-summary wording.


Follow-up isolation: civilian loot-accounting UI now uses `SiegeLootAccountingProfile`; AF keeps Bannerlord gold transfer, target eligibility, random amount calculation, and `InformationMessage` display while GCCZ core owns exit-settlement and per-target loot wording.


Follow-up isolation: market/civilian-spoils loot UI now also uses `SiegeLootAccountingProfile`; AF keeps town gold/inventory mutation, pending loot roster construction, random stack selection, and display side effects while GCCZ core owns market gold, market inventory, and civilian-spoils wording.


Follow-up isolation: direct aftermath loot status UI now also uses `SiegeLootAccountingProfile`; AF keeps direct loot-screen timing/state flags and display side effects while GCCZ core owns direct devastate/plunder settlement notices and credited loot summary wording.


Follow-up isolation: market-loot settlement reasons and capture ratios now also use `SiegeLootAccountingProfile`; AF keeps town gold/inventory mutation plus one-time guards while GCCZ core owns the plunder/massacre labels and percentage constants.


Follow-up isolation: civilian/hero gold amount constants and award source codes for 搜掠/血洗 now also use `SiegeLootAccountingProfile`; AF keeps target validation, random sampling, Bannerlord gold transfer, and display side effects while GCCZ core owns the amount constants.


Follow-up isolation: 搜掠 soldier-assignment, interaction timing parameters, and movement/follow source codes now use `SiegePlunderInteractionProfile`; AF keeps live mission-agent selection, movement, timing application, and side effects while GCCZ core owns the runtime constants and source-code strings.


Follow-up isolation: GCCZ scene-entry tooltip and missing-scene UI now use `SiegeInterventionEntryProfile`; AF keeps settlement/location/menu checks and display side effects while GCCZ core owns entry wording.


Follow-up isolation: GCCZ scene-entry troop-selection instructions and selection-result UI now also use `SiegeInterventionEntryProfile`; AF keeps Bannerlord troop-selection callbacks, selected-roster storage, and `InformationMessage` display while GCCZ core owns the wording and colors.


Follow-up isolation: GCCZ mission-entry battle-equipment and allied-summon UI now also use `SiegeInterventionEntryProfile`; AF keeps equipment mutation, troop picking, agent spawning, and formation side effects while GCCZ core owns the player-facing wording and colors.


Follow-up isolation: GCCZ scene-entry menu option text now also uses `SiegeInterventionEntryProfile`; AF keeps only the menu registration IDs, callbacks, and live condition/consequence checks.


Follow-up isolation: GCCZ entry auto-summon/default selection limits plus troop-selection, scene-cleanup, auto-enter summon, and ensure-allied-troops summon source codes now also use `SiegeInterventionEntryProfile`; AF keeps taunt-state cleanup calls, roster selection, soldier spawning, formation placement, mission opening, and encounter-summary side effects while GCCZ core owns the count/source constants.


Follow-up isolation: pending native aftermath selection now uses `SiegeAftermathResolutionKind` and `SiegeAftermathSelectionPolicy`; AF maps TaleWorlds aftermath enum values and keeps relief-pool/UI side effects while GCCZ core owns severity and replacement rules.


Follow-up isolation: action-tag routing now uses `SiegeActionRoutingFacts`, `SiegeActionRoutingDecision`, and `SiegeActionRoutingPolicy`; AF keeps regex replacement and side effects while GCCZ core owns destructive/mercy-track detection plus soldier relief routing decisions.


Follow-up isolation: postprocess action effect triggers now use `SiegePostprocessActionEffectProfile`; AF keeps regex matches and live target checks while GCCZ core owns normalized mercy replacement plus the source/detail wording passed into aftermath mutations.


Follow-up isolation: mercy-track transition UI now uses `SiegeMercyTrackTransitionProfile`; AF keeps destructive-lock checks, plunder-state clearing, logging, and `InformationMessage` display while GCCZ core owns blocked-action and reversible-plunder-stop wording.


Follow-up isolation: relief/appeasement profile selection now uses `SiegeReliefChoiceProfile`; AF still applies Bannerlord settlement, inventory, UI, and memory side effects while GCCZ core owns the deltas, messages, memory wording, shared-pool effect reason, stop-reversible-plunder reason, and destructive-lock display action name.


Follow-up isolation: relief validation UI for invalid soldier targets and missing shared material now also uses `SiegeReliefChoiceProfile`; AF keeps only the live target/pool checks and `InformationMessage` display call.


Follow-up isolation: civic profile selection now uses `SiegeCivicChoiceProfile`; AF still applies Bannerlord settlement, notable, gather, UI, and memory side effects while GCCZ core owns 安民宣抚/归心盟誓 deltas, messages, memory wording, shared-pool effect reason, stop-reversible-plunder reasons, and destructive-lock display action names.


Follow-up isolation: mercy profile selection now uses `SiegeMercyChoiceProfile`; AF still applies Bannerlord aftermath, shared-pool, UI, memory, and settlement side effects while GCCZ core owns the stop-plunder reason, soldier appeasement reason, message, memory wording, loyalty bonus, and destructive-lock display action name.


Follow-up isolation: destructive profile selection now uses `SiegeDestructiveChoiceProfile`; AF keeps mission side effects, settlement trust adjustment, and massacre combat drive, while GCCZ core owns 搜掠/血洗 aftermath kind, assembly source, UI message text, memory wording, and trigger-source classification. The old same-culture guard is no longer part of GCCZ.


Follow-up isolation: plunder finalized trust penalty now also routes through `SiegeDestructiveChoiceProfile`; AF keeps the Bannerlord settlement mutation call while GCCZ core owns the delta and reason string.


Follow-up isolation update: destructive same-culture validation UI for 搜掠 and 血洗 has been removed. AF bridge should not block GCCZ entry, 搜掠, 血洗, or 屠民迁殖 solely because settlement/player/soldier culture matches.


Follow-up isolation update: scene entry and postprocess destructive batches must rely on active GCCZ stage plus allied-soldier direct-player-command gates, not on same-culture policy wording.


Follow-up isolation: direct player-attack bloodbath trigger wording, attack-release damage source, and agent/score/non-enemy-hit bridge source codes now also use `SiegeDestructiveChoiceProfile`; AF keeps attack/damage detection, pending-aftermath mutation, and combat side effects while GCCZ core owns the UI text, trigger sources, trigger details, damage source string, and hit bridge source strings, including non-enemy friendly-hit restore.


Follow-up isolation: 血洗 civilian-hide parameters, soldier-order refresh parameters, occupation/combat/allied-drive source codes, and all-targets-down victory source now use `SiegeMassacreInteractionProfile`; AF keeps live mission-agent routing, order timing application, hide-point projection, and combat side effects while GCCZ core owns the runtime constants and source-code strings.


Follow-up isolation: cultural repopulation request handling now uses `SiegeCulturalRepopulationProfile`; AF keeps target validation, culture resolution, massacre start call, pending aftermath mutation, and later settlement/notable mutation, while GCCZ core owns the 屠民迁殖 request wording and devastate aftermath kind.


Follow-up isolation: cultural repopulation completion UI now also routes through `SiegeCulturalRepopulationProfile`; AF keeps the actual settlement/village/notable mutations and passes only settlement/culture/count facts to GCCZ-owned wording.


Follow-up isolation: cultural repopulation policy/target validation UI now also routes through `SiegeCulturalRepopulationProfile`; AF keeps only live policy checks, allied-soldier validation, and display side effects.


Follow-up isolation: cultural repopulation target-culture labels and apply source codes now also route through `SiegeCulturalRepopulationProfile`; AF keeps Bannerlord culture resolution and settlement mutation calls while GCCZ core owns player/kingdom/clan culture source labels, fallback wording, display formatting, and repopulation apply source strings.


Follow-up isolation: runtime prompt wording now routes through `SiegeRuntimePromptProfile`; AF keeps live agent lookup, allied/guard/civilian classification, gather/memory context collection, and outcome state flags while GCCZ core owns the long post-siege scene prompt text.


Follow-up isolation: soldier appeasement now uses `SiegeSoldierAppeasementProfile`; AF keeps target validation, party morale mutation, UI display, and memory recording, while GCCZ core owns 安兵/军心 wording, colors, and the morale penalty amount.


Follow-up isolation: soldier appeasement need-warning now also routes through `SiegeSoldierAppeasementProfile`, so the AF adapter keeps only the random requirement gate and state flips before displaying GCCZ-owned wording.


Follow-up isolation: soldier appeasement target validation now also uses `SiegeSoldierAppeasementProfile`; AF keeps only the allied-soldier check and `InformationMessage` display side effect.


Follow-up isolation: final completion and encounter-exit UI now uses `SiegeInterventionCompletionUiProfile`; AF keeps native-aftermath mapping, loot total checks, `InformationMessage`/`MBInformationManager` display, menu registration/text variable assignment, and mission-exit state while GCCZ core owns the completion labels/fallback, continue option text, massacre-victory, and loot-summary wording.


Follow-up isolation: mission-exit fallback aftermath selection now uses `SiegeMissionExitOutcomeProfile`; AF keeps live state flags, native enum mapping, plunder start side effects, and pending-aftermath mutation while GCCZ core owns the exit priority order plus trigger source/detail wording.

Follow-up isolation: direct AF aftermath campaign tick, native-menu intercept, external-pump, script-phase, and direct loot-screen defer source codes now use `SiegeDirectAftermathSourceProfile`; AF keeps the campaign tick callbacks, loot-screen timing, pending-script state, and encounter transition side effects while GCCZ core owns those source-code strings.

Follow-up isolation: mission-end, session-load runtime guard reset, post-mission encounter finish, done-menu continue finish, native menu init/detection, and native devastate summary transition source codes now use `SiegeAftermathTransitionSourceProfile`; AF keeps mission lifecycle, native menu handling, loot-screen timing, and encounter transition side effects while GCCZ core owns the source-code strings.

Follow-up isolation: native flee/order bridge, commandable-agent probing, control-tick order-controller priming, and order-controller source codes now use `SiegeNativeBridgeSourceProfile`; AF keeps Harmony patch registration, mission-view construction, order-controller binding, and live agent side effects while GCCZ core owns the source strings.

Follow-up isolation: GCCZ aftermath menu IDs and contextual-summary source marker now use `SiegeAftermathMenuProfile`; AF keeps Bannerlord menu registration, switching, and live menu side effects while GCCZ core owns the menu identifier strings, source marker, and matching helpers.

Handoff/tooling note: fused `G:\AFMOD\new-\一键编译覆盖推送` scripts now default to Bannerlord 1.3.x for build/overwrite/package/push workflows and require explicit `--dual` for 1.4.5 output; this keeps the GCCZ+AF test path aligned with the current 1.3.x game install and prevents optional 1.4.5 dependency gaps from blocking 1.3.x handoff work.

Handoff/tooling note: fused deploy now restores module-local runtime dependencies (`0Harmony.dll`, `Microsoft.ML.OnnxRuntime.dll`, `System.Memory.dll`, `System.Buffers.dll`, and `System.Runtime.CompilerServices.Unsafe.dll`) from the local build output after module mirroring, and build scripts pass `AnimusForgeBinDir` to the local output folder so Steam target cleanup cannot break the next 1.3.x build.

Handoff/tooling note: AF v0.8.3 zip fusion completed from `F:\YLQxz\Mount-Blade-Bannerlord-AnimusForge-mod-main (2).zip` into fused `G:\AFMOD\new-` and deployed to the Bannerlord 1.3.x module. The 0.8.3 upstream added `WorldMapPartyCommandBehavior`; the fused tree now carries that file and registers it in `SubModule.cs`, while GCCZ remains isolated under `AnimusForge.SiegeAftermathIntervention` with AF-side hooks limited to guarded prompt injection, shared relief capture, and postprocess tag dispatch. Build/deploy verification used 1.3.x Debug output with DLL SHA256 `3F2D7A33919341A307718D2AE2BD1104462A97D8A6302325F7A5288655671751`.

Handoff/bridge fix: fused `ShoutBehavior.QueueDeferredScenePostprocessActions(...)` must include `siegeInterventionRuleInjected` in its early-return guard. Otherwise pure GCCZ scene turns log `queueDeferred=True` but return before the auxiliary AI ActionPostprocess call, so no AI-selected GCCZ action labels are produced.

Follow-up bridge fix: when an AI-selected `[ACTION:召集]` comes from an allied soldier after civilian gather has already entered command-control, the fused AF adapter must not restart civilian gathering or silently ignore the tag. It should call the GCCZ-owned `SiegeCivilianGatherInteractionProfile.ShouldReleaseSoldiersForCommandControlRepeat(...)` policy and, only when that policy accepts the repeat soldier gather, run the minimal live-agent soldier return/unlock side effect in `SiegeAiInterventionBehavior`.

Handoff/tooling note: AF v0.8.4 zip fusion completed from `F:\YLQxz\Mount-Blade-Bannerlord-AnimusForge-mod-main (3).zip` into exact-zip fused worktree `G:\AFMOD\new-084-auto` and deployed to the Bannerlord 1.3.x module `AnimusForge_1_3_x`. The guarded GCCZ contract remains: standalone source lives under `AnimusForge.SiegeAftermathIntervention`, AF-side edits are limited to `SubModule.cs`, `Patch_GameMenu_ActivateGameMenu.cs`, `SceneTauntBehavior.cs`, `MyBehavior.cs`, `ShoutBehavior.cs`, and `SiegeAiInterventionBehavior.cs`, and `siege_intervention_aftermath` remains a passive rule only injected during the active GCCZ scene. Verification: GCCZ standalone tests passed, AF 1.3.x Debug build passed with 0 warnings/0 errors, deployed DLL SHA256 `56E3D215099E5C026E5848480C9830B044AC4379677502D767531479AB517781`.

Follow-up outcome tuning: finalized GCCZ destructive settlement effects now use `SiegeSettlementOutcomeProfile`. 搜掠 keeps native Pillage effects, then applies current settlement public trust -30, bound village public trust -20, and settlement/bound-village notable relation -30. 血洗 keeps native Devastate effects, then applies current settlement public trust -50, bound village public trust -50, and notable relation -70; the older extra x1.8 devastate/loyalty/notable-power penalty was removed. 殖民 keeps native Devastate effects, then applies bound village public trust -80, resets current town/castle loyalty to 100, doubles the native devastate prosperity loss, and records a one-campaign-year prosperity-growth debuff that removes 70% of positive daily prosperity growth while active.

## 2026-06-12 direct destructive tag gate

- Fused ShoutBehavior now passes a reply-is-direct-player-response flag into SiegeAiInterventionBehavior.TryProcessAiActionTags and the auxiliary action-postprocess context.
- GCCZ core policy treats [ACTION:搜掠]/[ACTION:血洗]/[ACTION:殖民] as soldier-mediated destructive labels: they execute only when the target is a player-allied siege soldier directly responding to the player's current command.
- Invalid soldier-mediated destructive tags from NPC-to-NPC chatter are stripped and may trigger a nearby allied soldier inquiry instead of applying settlement consequences.

## 2026-06-13 soldier thinking and same-culture cleanup

- `SiegeSoldierThinkingProfile` owns the allied-soldier visible behavior chain: scene fact → player command authority → current outcome → original culture/troop voice → emotion/personality variation → natural reply.
- Soldier identity override does not erase AF culture/troop knowledge. A Khuzait soldier brought into GCCZ should still be able to use Khuzait knowledge-library material if AF passes the original `CultureId`, troop/identity id, and `CharacterObject` through the prompt bridge.
- Same-culture destructive blocking is removed from GCCZ core. Same-culture may make soldiers more tense, ashamed, quiet, or uncomfortable, but it must not block GCCZ entry, 搜掠, 血洗, or 屠民迁殖.
- 血洗 cannot be downgraded back to 搜掠/宽恕/救济, but it may still be upgraded into 屠民迁殖. 屠民迁殖 can also be triggered directly at the start by a clear player command to allied soldiers.

## 2026-06-14 AF086 prompt/postprocess bridge

- Fused `G:\AFMOD\new-086\ShoutBehavior.cs` now appends `SiegeAiInterventionBehavior.BuildRuntimePromptForPromptContext(...)` into the scene `extraFact` / `fullExtra` immediately before AF builds the shout prompt context.
- This is a thin bridge only: AF still passes the original `Hero`, `CharacterObject`, `CultureId`, troop/identity, prefetched lore, and preprocess exclusions into `MyBehavior.BuildShoutPromptContextForExternal(...)`, so AF knowledge-library lookup remains upstream of GCCZ thinking rules.
- The scene unified action postprocess now merges `SiegeAiInterventionBehavior.BuildRuntimePostprocessRulesForExternal()` while the GCCZ stage is active, adds `BuildRuntimePostprocessContextForExternal(targetAgentIndex, replyIsDirectPlayerResponse)`, normalizes GCCZ action tags, and dispatches them through `TryProcessAiActionTags(...)`.
- `replyIsDirectPlayerResponse` is propagated from the first/direct speaker turn into deferred action dispatch and speech-tag cleanup. This preserves the rule that soldier-mediated destructive tags such as 搜掠/血洗/殖民 only execute from a player-allied siege soldier directly answering the player's current command; NPC-to-NPC chatter is stripped or rerouted by GCCZ policy.

## 2026-06-14 active-speech tag hardening

- GCCZ core routing now also treats `[ACTION:抢钱]` as a direct-player-response action: it can apply local civilian robbery only when the current non-soldier speaker is directly responding to the player's current demand.
- If `[ACTION:抢钱]` appears in NPC-to-NPC chatter, a soldier reply, or an immediate/indirect echo, the fused AF adapter strips the tag and may trigger a nearby allied-soldier inquiry instead of applying robbery settlement effects.
- Fused `ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(...)` now returns whether the immediate speech was queued. `SiegeAiInterventionBehavior.TryPromptSoldierDestructiveInquiry(...)` tries nearby allied soldiers in distance order and consumes its cooldown only after a soldier reaction is actually queued.
- Compact/immediate scene reactions now append the active GCCZ runtime prompt block when the intervention scene is active, so soldier inquiries can see the same scene authority, memory, same-culture discomfort, and no-tag constraints as normal GCCZ dialogue while still bypassing destructive action postprocess execution.

## 2026-06-14 ambient label reactions

- GCCZ core now owns `SiegeAmbientReactionProfile`: dependency-free prompt facts plus the shared RPM budget constants `WindowSeconds = 30` and `MaxSpeakersPerAudience = 3`.
- Ambient reactions are for NPC units that are **not** directly talking to the player. When a tag is successfully triggered or a persistent tag is being executed, the fused AF adapter may ask nearby non-direct civilians/soldiers to produce short scene speech.
- Fused `SiegeAiInterventionBehavior` remains a thin bridge: it gates the active GCCZ mission, selects live nearby agents, excludes the current direct/focus agent, checks same-culture discomfort, then calls `ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(...)` with the GCCZ-owned fact text.
- Each side has its own batch throttle: at most 3 civilian speakers per 30 seconds and at most 3 allied-soldier speakers per 30 seconds. Existing civilian-gather messenger speeches share the same side throttle so they cannot bypass the RPM cap.
- Ongoing execution currently refreshes ambient reactions for 召集、搜掠、血洗、屠民迁殖 under the same throttle; instant positive/robbery labels only fire ambient reactions after the side effect actually succeeds.

## 2026-06-14 fused build nested obj/bin exclusion

- After installing .NET 8 SDK and running the fused build, `AnimusForge.csproj` compiled generated files under `AnimusForge.SiegeAftermathIntervention\obj\...`, causing duplicate `AssemblyVersion` and `TargetFrameworkAttribute` errors.
- Fused `G:\AFMOD\new-086\AnimusForge.csproj` now explicitly excludes `AnimusForge.SiegeAftermathIntervention\bin\**` and `AnimusForge.SiegeAftermathIntervention\obj\**` from `Compile`, `EmbeddedResource`, and `None` items.
- This is a fused build hygiene bridge only; GCCZ core source remains under `AnimusForge.SiegeAftermathIntervention` and should not ship generated `bin/obj` artifacts into the AF host compile.

## 2026-06-14 AF086 compile bridge fix

- After the SDK install exposed real fused-build errors, `SceneTauntBehavior` needed external clear wrappers for pending forced player execution and pending main-hero battle-death state. `SiegeAiInterventionBehavior` calls these during GCCZ scene entry cleanup so old scene-taunt defeat/execution carryover cannot leak into the post-siege intervention scene.
- `MyBehavior.RecordAnimusForgeSiegeInterventionForExternal(...)` is now present in the fused tree. It records the finalized GCCZ aftermath into AF's NPC action memory for relevant lords/owners while GCCZ still owns the settlement outcome logic and summary facts.
- These are AF adapter/host compile fixes only; they do not move GCCZ outcome rules into `MyBehavior` or `SceneTauntBehavior`.

## 2026-06-14 dual deploy runtime dependency restore

- `G:\AFMOD\new-086\一键编译覆盖推送\deploy_module.ps1` now restores module-local runtime dependencies after the `/MIR` module copy and DLL/PDB update.
- The deployed `AnimusForge_1_3_x` and `AnimusForge_1_4_5` bins receive `0Harmony.dll`, `Microsoft.ML.OnnxRuntime.dll`, `System.Memory.dll`, `System.Buffers.dll`, and `System.Runtime.CompilerServices.Unsafe.dll` from the current build output or source module bin.
- This prevents the dual overwrite script from deleting runtime dependencies while mirroring the source module into Bannerlord `Modules`.
- The same deploy script now uses a local SHA-256 helper with a `.NET` fallback when the host PowerShell does not expose `Get-FileHash`, so post-copy verification works on the older shell launched by the batch file.

## 2026-06-14 dual native siege aftermath entry menus

- `SiegeAftermathMenuProfile.EntryMenuIds` now lists both `menu_settlement_taken_player_leader` and `menu_settlement_taken`.
- The fused AF bridge should register the same `亲自进城决定` entry option on every ID in that list, because Bannerlord 1.3.15 can show the native `毁坏 / 掠夺 / 宽恕` menu through `menu_settlement_taken` instead of the player-leader ID.
- `SiegeAftermathMenuProfile.EntryMenuInsertionIndex` is `0`, and each native menu receives a unique option ID via `BuildEntryMenuOptionId(...)`, so the GCCZ entry is inserted above the vanilla three aftermath choices instead of being appended below the visible list.

## 2026-06-15 AF086 behavior registration guard

- Fused `G:\AFMOD\new-086\SubModule.cs` must explicitly call `campaignGameStarter.AddBehavior(new SiegeAiInterventionBehavior())`.
- If the class is compiled into `AnimusForge.dll` but this Campaign behavior is not registered, `RegisterEvents()` never runs, `OnSessionLaunched` never calls the GCCZ menu-registration bridge, and the native aftermath menu will only show vanilla `毁坏 / 掠夺 / 宽恕`.
- Runtime check: after loading a campaign, `Mod_Logic.txt` should include `[SiegeAiIntervention] Reset AF siege aftermath runtime guards` and, when a campaign session launches, `Registered entry option` lines for the menu IDs owned by `SiegeAftermathMenuProfile.EntryMenuIds`.

## 2026-06-15 AF086 native aftermath activation guard

- Fused `G:\AFMOD\new-086\Patch_GameMenu_ActivateGameMenu.cs` must let GCCZ inspect native siege aftermath menu activation before the original `GameMenu.ActivateGameMenu` body runs.
- The bridge calls `SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuActivationForExternal(menuId)` for `menu_settlement_taken`, `menu_settlement_taken_player_leader`, and contextual summary menus. If GCCZ has already finalized or is still in mission-end/loot/encounter-finish transition, the native menu activation is suppressed so the player does not return to the `亲自进城决定 / 毁坏 / 掠夺 / 宽恕` entry screen after leaving the GCCZ scene.
- This guard must not run for unrelated future settlements; the fused bridge checks the completed settlement before suppressing stale native aftermath menus.

## 2026-06-15 AF086 resolved menu bridge parity

- `G:\AFMOD\new-084-auto` handled post-GCCZ return by checking `ShouldRedirectResolvedAftermathMenuForExternal(menuId)` inside `Patch_GameMenu_ActivateGameMenu` before the native menu body ran.
- `G:\AFMOD\new-086` must preserve that old resolved-menu branch in addition to the newer transition activation guard. Bannerlord 1.3.15 may restore the already-open `menu_settlement_taken_player_leader` / `menu_settlement_taken` context after the GCCZ mission ends, so the bridge has to finish the encounter instead of letting the native three-option menu draw again.
- The fused entry condition should also hide `亲自进城决定` once GCCZ has finalized or queued encounter finish for the current settlement. This prevents re-entering the GCCZ scene after native `ApplyAftermath` plus GCCZ extra effects have already been applied.

## 2026-06-15 AF086 supplemental aftermath bridge restored

- `Patch_GameMenu_ActivateGameMenu.Prefix()` and `Patch_GameMenu_SwitchToMenu_AFResolvedSiegeAftermath.Prefix()` must check direct massacre/plunder scripts before the generic resolved-menu redirect. Direct destructive scripts own loot-screen timing and encounter finish; resolved redirect is only the fallback after direct scripts are not pending.
- `Patch_SiegeAftermath_*_OnInit_AFRedirect` keeps native aftermath menu init from drawing stale vanilla menus after GCCZ resolution.
- `Patch_SiegeAftermath_Continue_AFMassacreLoot` is the bridge for native Devastate contextual-summary continue, so GCCZ can open pending loot or finish the encounter after the native summary.
- `Patch_GameStateManager_OnTick_AFMassacreLoot` is a guarded fallback pump. It calls the same direct script pump facades and is safe because the scripts return when no direct aftermath is pending, a mission is still active, or the loot screen already opened.

## 2026-06-24 AF094 normal town-center entry with raised civilian population

- Fused `G:\AFMOD\new-0.9.4\SiegeAiInterventionBehavior.cs` opens GCCZ personal-entry through `PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(center, ...)`, matching a normal non-siege town-center entry.
- The previous test path that called `SandBoxMissions.OpenTownCenterMission(...)` with `GetUpgradeLevelTag(wallLevel) + " siege"` scene levels is retired for now, because the siege scene layer produced too few active civilians and blocked the raised-population test.
- The visual-only wall-damage overlay is also retired for this normal-town test path. GCCZ must not apply `damage_decal`, `WallSegment.OnChooseUsedWallSegment(...)`, siege deployment, siege AI, siege spawning, or `PreDestroy()` paths while evaluating normal civilian population.
- The AF bridge may still summon the selected allied player troops as GCCZ escorts. Civilian population must remain vanilla-location-based: no `SpawnAssemblyCivilian`, no raw synthetic `SimpleAgentOrigin` agent injection, and no siege deployment refill.
- AF094 keeps `InterventionNativeTownCivilianPopulationMissionBehavior` before the GCCZ mission behavior. It raises the active town-center civilian population to a prosperity-weighted random target of roughly **100-200** civilians, capped by `SiegeCivilianAssemblyProfile`, by creating vanilla `LocationCharacter` entries through `CommonTownsfolkCampaignBehavior` creators and spawning them through `MissionAgentHandler.SpawnDefaultLocationCharacter(...)`.
- The population bridge skips protected child/teenager creators, because GCCZ separately suppresses protected child scene agents. Adult civilians, beggars, cleaners, dancers, and carrying townsfolk still use the settlement culture and normal `npc_common` / `npc_common_limited` / special town spawn tags. When common points are exhausted, the bridge may reuse unused civilian-safe town tags such as merchants, armorers, weaponsmiths, blacksmiths, barber, and gambler points; it still does not raw-spawn synthetic agents.

Follow-up isolation: GCCZ runtime prompt commander-identity wording now uses `SiegeRuntimePromptProfile.BuildPlayerCommanderContext`; fused AF only supplies the live player name and soldier/civilian booleans. Fused allied-soldier prompt detection also falls back to player main-party / selected-entry roster membership for guard-named troops when direct `AgentIndex` tracking is unavailable, so selected troops such as palace guards still recognize the player as their commander.

## 2026-06-25 AF094 immediate/ambient identity prompt bridge

Fused AF short scene-reaction generators must keep GCCZ identity rules at the top of the prompt while the active siege-aftermath scene is running.

- `GenerateCompactSceneReactionLineAsync(...)` and `GenerateImmediateSceneBehaviorReactionAsync(...)` now split `ctx.Extras` and, only when `【附加规则:siege_intervention_aftermath】` is present, lift the GCCZ rule block into the system prompt instead of dropping it from auxiliary short replies.
- The bridge also injects a compact highest-priority identity override from `SiegeRuntimePromptProfile.BuildImmediateReactionIdentityOverride(...)`: civilians address the player as 大人/领主/攻城者/胜利方首领; allied soldiers address the player as 统帅/大人/长官; short replies must not call the player 库赛特人/陌生人/路人/本地人 or claim the player's army is outside while GCCZ is active.
- Ordinary AF scenes remain unchanged because the bridge is gated by active GCCZ state plus the injected `siege_intervention_aftermath` rule marker.

## 2026-06-25 AF094 ceremonial banner-bearer bridge

- `SiegeBannerBearerProfile` owns dependency-free constants for the GCCZ ceremonial entry escort: exactly two banner bearers, left/right player offsets, follow refresh, teleport catch-up distance, and AF bridge source strings.
- Fused `G:\AFMOD\new-0.9.4\SiegeAiInterventionBehavior.cs` owns live Bannerlord side effects only: resolving the player's clan banner/banner item, picking non-hero main-party/selected troops, spawning the two agents with `AgentBuildData.BannerItem(...)`, and maintaining their left/right positions near the player.
- Banner bearers are added to the allied-agent set so GCCZ cleanup, prompt identity, and friendly-state restoration treat them as player soldiers, but the AF bridge excludes them from commandable plunder allocation and massacre hunter selection. They should remain visual escorts, not reduce the 70% plunder/attack pool.
- The bridge is gated by the active GCCZ mission and runs after normal allied troop summon succeeds; ordinary AF scenes and vanilla town entries are unaffected.

Mounted-player follow-up: when the player is riding during GCCZ entry, the fused AF bridge uses `Agent.MountAgent` to switch to the mounted offsets and tighter follow refresh in `SiegeBannerBearerProfile`. Banner-bearer troop selection preserves normal roster order but promotes troops that actually have a mount (`HasMount()`); mounted banner bearers spawn with their own mount key and use cavalry formation, while non-mounted fallback bearers stay on foot with the wider mounted-player spacing. This remains a GCCZ-only bridge and still excludes banner bearers from plunder/massacre command allocation.

Banner-bearer source refinement: banner bearers are never heroes. The fused bridge now builds non-hero troop stacks from `PartyBase.MainParty.MemberRoster` first, ignoring companion/family/wanderer-only selected entry rosters for banner duty. It sorts stacks by mounted-capable troop first and then by available unwounded count, so if the player enters with only hero companions, the two flag bearers still come from the largest eligible soldier stack in the party. Normal summoned heroes and soldiers remain forced on foot via the regular allied-spawn `.NoHorses(true)` path; only banner bearers may spawn mounted, and only when the player is actually mounted in the GCCZ mission.

Local civilian violence reaction bridge: local player attacks against GCCZ civilians are treated as区域性街巷冲突, not automatic massacre. The reusable policy lives in `SiegeLocalCivilianReactionProfile`: 24m witness radius, 18 witness cap, 4 short-line speakers, 3 local resisters, and 18s per-witness repeat cooldown. The fused adapter only listens while the GCCZ mission is active, then uses existing local flee/hide and hostile-civilian helpers so nearby civilians run, speak, or have a small capped chance to resist. It also handles player-caused down/kill removal events so one-hit knockdowns still propagate local panic. This bridge must not enable vanilla `FleeBehavior` globally and must not affect normal AF scenes.

Local soldier witness inquiry refinement: when the player attacks or downs a civilian during active GCCZ, the fused adapter now checks allied soldiers within the same 24m `SiegeLocalCivilianReactionProfile.WitnessRadius`. If at least one allied soldier is in range, one nearby soldier must immediately ask the player whether to keep the incident local, expand to full-city plunder, or escalate to massacre. This forced soldier inquiry bypasses the generic destructive-inquiry cooldown but is deduplicated per civilian victim, remains GCCZ active-stage gated, and falls back to a visible soldier inquiry message if the immediate AI short-line bridge cannot start. The inquiry itself never executes plunder/massacre; it only requests player confirmation.

Cultural-repopulation tag audit: `[ACTION:殖民]` remains a soldier-mediated destructive action only. The fused postprocess bridge applies it only when the active target is a player-allied soldier and the reply is a direct response to the player's current command; civilian replies, ambient chatter, soldier-to-soldier talk, and soldier inquiry echoes are routed to destructive inquiry instead of executing repopulation. GCCZ standalone tests now cover direct civilian, indirect soldier, and valid direct allied-soldier repopulation routing so future prompt/rule edits do not reopen the bug.

## GCCZ regional civilian panic bridge

- Fused AF adapters must keep the global GCCZ civilian `FleeBehavior` suppression, but explicitly allow agents recorded as local regional-conflict fleeing civilians. Those agents should activate Bannerlord's native `AlarmedBehaviorGroup` + `FleeBehavior` so town conflict panic can choose passages/guards when a local fight is active.
- Local regional resistance may start or join a narrow `MissionFightHandler` fight with the player and the few resisting civilians only. Do not add all civilians or all allied soldiers to that native fight; full-city combat remains the separate GCCZ massacre path.
- When regional conflict escalates to massacre, end the narrow native local fight before handing control to GCCZ massacre combat driving.
