# AF bridge surface for GCCZ / 攻城处置

This file records the current AF-facing bridge surface so GCCZ can be isolated without losing the working integration.

## Registration points in fused `new-`

- `SubModule.OnBeforeInitialModuleScreenSetAsRoot()` calls `SiegeAftermathPatchBootstrap.Apply(harmony)` to register supplemental native aftermath guards for `SwitchToMenu`, native menu init, contextual-summary continue, and `GameStateManager.OnTick` loot pumps.
- `SubModule.OnGameStart()` / campaign starter path registers `new SiegeAiInterventionBehavior()`.
- `Patch_GameMenu_ActivateGameMenu.Prefix()` first lets GCCZ intercept direct massacre/plunder/native aftermath menus, then falls through to normal AF encounter redirect logic.
- Current AF086 bridge ships a small `Patch_SiegeAftermath_AFIntervention.cs` AF adapter file. Native aftermath routing is handled by this file, `Patch_GameMenu_ActivateGameMenu.cs`, and the guarded menu/encounter helpers inside `SiegeAiInterventionBehavior.cs`; GCCZ policy/rules remain outside the AF patch file.
- Fused agent-spawn adapters must rely on `Mission.SpawnAgent(...)` to dispatch `MissionBehavior.OnAgentBuild(...)`; do not manually invoke `BattleAgentLogic.OnAgentBuild` after spawning, because Bannerlord already performs that lifecycle notification.

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

### Scene / mission / menu guards

- `IsOccupationSceneActiveForExternal()`
- `IsInterventionAlliedSoldierForExternal(...)`
- `ShouldRedirectResolvedAftermathMenuForExternal(string menuId)`
- `TryHandleNativeAftermathMenuInitForExternal(string source)`
- `TryHandleNativeAftermathSummaryContinueForExternal(string source)`
- `TryHandleDirectMassacreAftermathMenuForExternal(string menuId, string source)`
- `TryHandleDirectPlunderAftermathMenuForExternal(string menuId, string source)`
- `TryPumpDirectMassacreAftermathScriptForExternal(string source)`
- `TryPumpDirectPlunderAftermathScriptForExternal(string source)`

### AF give / relief capture

- Fused `AfGcczShoutBridge.ShouldCaptureSharedReliefTransfer(int targetAgentIndex)` gates scene-only capture.
- Fused `AfGcczShoutBridge.CaptureSharedReliefGoldTransfer(...)` and `CaptureSharedReliefItemTransfer(...)` route successful AF give transfers into `RecordSharedCivilianReliefTransferForExternal(...)`.
- During active GCCZ scenes, AF give gold/items to in-scene allied soldiers, civilians, merchants, artisans, headmen, or notables are centralized into the all-town shared civilian relief pool rather than becoming private receiver property or normal AF prepaid debt.
- Fused `ShoutBehavior.ApplyShoutGiveTransfer()` removes the player gold/item first, skips per-receiver storage/prepaid recording when capture succeeds, and relies on GCCZ positive relief/civic choices to apply settlement food/trust effects or negative outcomes to refund the logical pool to the player.

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


Follow-up isolation: postprocess tag normalization now lives in `SiegePostprocessTagNormalizer`; `SiegeAiInterventionBehavior.NormalizeSiegeInterventionPostprocessTagsForExternal(...)` is a thin bridge that passes the runtime-allowed rule tags and delegates legacy alias mapping, numeric canonical output, action ordering, de-duplication, and mood preservation to GCCZ core.


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


Follow-up isolation: postprocess action effect triggers now use `SiegePostprocessActionEffectProfile`; AF keeps live Bannerlord target checks and applies one validated `SiegeInterventionActionKind`, while GCCZ core owns action selection, compatibility aliases, routing policy, and source/detail wording. The retired dialogue-keyword mercy-to-relief upgrader and duplicated per-action town regex path must not be restored.


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

## 2026-07-29 town action routing repair

- The core normalizer must preserve the explicit bloodbath-plus-colonization pair as `[ACTION:殖民]`; reducing `[ACTION:9] [ACTION:10]` to bloodbath silently loses the documented action-10 upgrade.
- Fused AF town-action handlers may bridge 安兵、士兵救济、搜掠、血洗、殖民 targets into the historical allied index set only when the live Agent has explicit player provenance: already registered/commandable origin, SETS selected follower, main-party origin, or an active main-party hero. A matching troop template or a shared peaceful-scene team is not sufficient.
- Fused diagnostics should record the normalized town action kinds, direct-response state, runtime allied identity, legacy registration state, and final handled result without changing ordinary AF dialogue outside active GCCZ.

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

Follow-up outcome tuning: finalized GCCZ destructive settlement effects now use `SiegeSettlementOutcomeProfile`. 搜掠 keeps native Pillage effects, then applies current settlement public trust -30, bound village public trust -20, settlement/bound-village notable relation -30, and notable personal trust -30. 血洗 keeps native Devastate effects, adds the same prosperity loss once more so the final prosperity penalty is twice the native value, then applies current settlement public trust -50, bound village public trust -50, notable relation -70, and notable personal trust -70. Both 血洗 and 殖民 apply a one-campaign-year recruitment slowdown to the affected settlement and its bound villages: native empty-slot volunteer production probability is multiplied by 0.20, while existing volunteers are preserved. 殖民 also keeps the doubled native Devastate prosperity loss, applies bound village public trust -80, resets current town/castle loyalty to 100, and removes 70% of positive daily prosperity growth for one campaign year. The fused adapter stores the slowdown under a marked v3 save key, migrates old v1/v2 suppression timers without deleting them, and restores any active cultural-repopulation timer from the prosperity-debuff expiry.

## 2026-06-12 direct destructive tag gate

- Fused ShoutBehavior now passes a reply-is-direct-player-response flag into SiegeAiInterventionBehavior.TryProcessAiActionTags and the auxiliary action-postprocess context.
- GCCZ core policy treats [ACTION:搜掠]/[ACTION:血洗]/[ACTION:殖民] as soldier-mediated destructive labels: they execute only when the target is a player-allied siege soldier directly responding to the player's current command.
- Invalid soldier-mediated destructive tags from NPC-to-NPC chatter are stripped and may trigger a nearby allied soldier inquiry instead of applying settlement consequences.

## 2026-08-22 town role reaction guidance

- `TownPromptComposer` injects the localized six-role reaction matrix from `GcczTownPrompt.zh-CN.json` into the active town prompt.
- AF remains responsible for live personality, relationship, identity, culture, knowledge-library, and memory facts. GCCZ does not create a parallel persona store.
- Runtime scene facts and witnessed events have the highest priority, followed by AF personality and relationship. Role and cultural background refine expression without replacing current authority or causality.
- Shared culture changes tone only. It cannot block or authorize `Plunder`, `Massacre`, `CulturalRepopulation`, or any other semantic action.
- The same accepted action may sound different across roles and personalities, but eligibility, requirements, completion, rewards, penalties, and state transitions remain unchanged.

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

## 2026-07-08 resolved aftermath deferred finish guard

- Resolved native aftermath menu guards (`GameMenu.ActivateGameMenu`, `GameMenu.SwitchToMenu`, and native aftermath menu init prefixes) must only suppress the stale vanilla menu and queue encounter finish; they must not call `PlayerEncounter.Finish(true)` synchronously inside the menu activation/switch/init prefix stack.
- The queued finish is pumped from `CampaignTick` / `GameStateManager.OnTick` through `TryPumpPendingEncounterFinishForExternal(...)`, after the menu prefix returns. This avoids reentrant crash reports after town GCCZ positive outcomes such as `[ACTION:宽恕]`.
- Direct destructive loot flows may keep their own guarded loot/finish timing; the deferred-finish rule is for already-resolved non-direct fallback aftermath menus.

## 2026-08-22 on-demand hidden residents in the normal town-center scene

- Fused `G:\AFMOD\NEW-10\SiegeAiInterventionBehavior.cs` opens GCCZ personal-entry through `PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(center, ...)`, matching a normal non-siege town-center entry.
- The earlier prosperity-weighted 100-200 civilian auto-fill path is retired. `InterventionHiddenResidentSpawnMissionBehavior` has no mission-start or tick-driven spawning; it runs only after the semantic gather action is accepted.
- The AF bridge may still summon selected allied troops as GCCZ escorts. Hidden residents remain vanilla-location-based: the bridge creates adult townsfolk through `CommonTownsfolkCampaignBehavior`, uses `MissionAgentHandler.SpawnDefaultLocationCharacter(...)`, and never injects raw `SimpleAgentOrigin` civilians.
- Each accepted request can bring out at most six residents, the scene can add at most twelve, no residents are added once twenty-four civilians are already visible, and the shared scene agent soft cap still applies. The mission-owned ledger disappears on scene exit.
- Candidate corners are behind or to the side of the player, at least eighteen meters away, on a valid navmesh, and clear of active agents. Failure returns localized scene feedback instead of falling back to a visible or unsafe spawn.
- Only `npc_common` and `npc_common_limited` points may seed the vanilla spawn before the agent is moved to the safe corner. Merchant, notable, workshop, and market tags are never reused, so the vanilla market population and services are not replaced.
- A sealed plunder or massacre target snapshot blocks new arrivals. Residents spawned before an operation snapshot are registered through the existing civilian tracker and therefore participate in later scene actions without changing already-fixed targets.

Follow-up isolation: GCCZ runtime prompt commander-identity wording now uses `SiegeRuntimePromptProfile.BuildPlayerCommanderContext`; fused AF only supplies the live player name and soldier/civilian booleans. Fused allied-soldier prompt detection also falls back to player main-party / selected-entry roster membership for guard-named troops when direct `AgentIndex` tracking is unavailable, so selected troops such as palace guards still recognize the player as their commander.

## 2026-06-25 AF094 immediate/ambient identity prompt bridge

Fused AF short scene-reaction generators must keep GCCZ identity rules at the top of the prompt while the active siege-aftermath scene is running.

- `GenerateCompactSceneReactionLineAsync(...)` and `GenerateImmediateSceneBehaviorReactionAsync(...)` now split `ctx.Extras` and, only when `【附加规则:siege_intervention_aftermath】` is present, lift the GCCZ rule block into the system prompt instead of dropping it from auxiliary short replies.
- The bridge also injects a compact highest-priority identity override from `SiegeRuntimePromptProfile.BuildImmediateReactionIdentityOverride(...)`: civilians address the player as 大人/领主/攻城者/胜利方首领; allied soldiers address the player as 统帅/大人/长官; short replies must not call the player 库赛特人/陌生人/路人/本地人 or claim the player's army is outside while GCCZ is active.
- Ordinary AF scenes remain unchanged because the bridge is gated by active GCCZ state plus the injected `siege_intervention_aftermath` rule marker.

## 2026-06-25 AF094 ceremonial banner-bearer bridge

- `SiegeBannerBearerProfile` owns dependency-free constants for the GCCZ ceremonial entry escort: two enabled banner bearers, the native second-command formation index (`FormationClass.Ranged` in the AF bridge), initial spawn offsets, and AF bridge source strings. `SiegeCastleRosterSelectionProfile` separately fixes all player-selected castle escort troops to the native first command formation (`FormationClass.Infantry`).
- Fused `G:\AFMOD\NEW-10\SiegeAiInterventionBehavior.cs` owns live Bannerlord side effects only: resolving the player's clan banner/banner item, picking non-hero main-party troops, spawning two banner-bearer agents with `AgentBuildData.BannerItem(...)`, assigning them to the native second formation, and giving that formation the same follow/order-controller priming as other GCCZ troops. `TroopInspectionBehavior.cs` reassigns both reused and newly spawned selected escorts to the first formation.
- The old custom banner-bearer follow/stop/teleport loop is intentionally removed. After initial spawn, banner bearers are ordinary player-team formation troops, so vanilla formation commands, including mount/dismount for mounted troops, can control them.
- Banner bearers are added to the allied-agent set so GCCZ cleanup, prompt identity, and friendly-state restoration treat them as player soldiers, but the AF bridge excludes them from plunder allocation and massacre hunter selection. They remain visual/command-formation escorts, not part of the 70% plunder/attack executor pool.
- The bridge is gated by the active GCCZ mission and runs after normal allied troop summon succeeds; ordinary AF scenes and vanilla town entries are unaffected.

Mounted-player follow-up: TownCenter normally calls `SandBoxHelpers.MissionHelper.SpawnPlayer(..., noHorses: true)`, which forces the player on foot even when `Hero.MainHero.BattleEquipment` has a horse. During the active GCCZ mission, or the immediately pending GCCZ mission whose live settlement still matches `_activeSettlementId`, the fused AF bridge patches the `SpawnPlayer` overloads before the original body runs and rewrites the spawn parameters to `civilianEquipment=false` and `noHorses=false`. This makes the player enter GCCZ directly with battle equipment and the equipped mount when one exists, without spawning a separate ceremony horse after mission start. The pending-stage prefix is live-settlement gated and falls through unchanged for ordinary AF scenes and vanilla town entry. Banner bearers spawn mounted only when the player is already mounted or the player's battle equipment has a horse, and only if the selected non-hero troop can spawn mounted; otherwise they enter on foot in the same second formation.

Castle scene input follow-up: fused `G:\AFMOD\NEW-10\AnimusForge\GUI\Prefabs\ShoutTextInputPopup.xml` keeps Enter-to-submit but also exposes explicit `发送` and `取消` buttons. This is an AF UI bridge fix for battle-hosted castle scenes; no GCCZ action is triggered until the normal shout submit callback receives non-empty player text.

Banner-bearer source refinement: banner bearers are never heroes. The fused bridge now selects the banner-bearer troop type from the active selected entry roster first: among non-hero selected troops, the largest stack supplies both flag bearers; if multiple stacks have the same count, one stack is chosen randomly. Only when the selected entry roster has no non-hero troops, such as a pure companion/family/wanderer entry, does the bridge fall back to the player's hero culture and pick a highest-tier/highest-level non-hero soldier from that culture. Main-party majority is only a final safety fallback if the culture lookup cannot produce a soldier. Normal summoned heroes and soldiers remain forced on foot via the regular allied-spawn `.NoHorses(true)` path; only banner bearers may spawn mounted under the player-mount rule above.

Local civilian violence reaction bridge: local player attacks against GCCZ civilians are treated as区域性街巷冲突, not automatic massacre. The reusable policy lives in `SiegeLocalCivilianReactionProfile`: 24m witness radius, 18 witness cap, 4 short-line speakers, 3 local resisters, and 18s per-witness repeat cooldown. The fused adapter only listens while the GCCZ mission is active, then uses existing local flee/hide and hostile-civilian helpers so nearby civilians run, speak, or have a small capped chance to resist. It also handles player-caused down/kill removal events so one-hit knockdowns still propagate local panic. This bridge must not enable vanilla `FleeBehavior` globally and must not affect normal AF scenes.

Local soldier witness inquiry refinement: when the player attacks or downs a civilian during active GCCZ, the fused adapter now checks allied soldiers within the same 24m `SiegeLocalCivilianReactionProfile.WitnessRadius`. If at least one allied soldier is in range, one nearby soldier must immediately ask the player what to do. Ordinary hit incidents should sound like local control/cordon requests instead of a hard three-choice menu. Downed civilians may let the soldier remind the player that full-city plunder or massacre requires an explicit command. Bloodthirsty/cruel escalation is driven by the existing AF persona chain: the fused adapter reads hero persona via `MyBehavior.GetNpcPersonaForExternal(...)` or unnamed troop persona via `ShoutUtils.TryGetUnnamedNpcPersona(...)`, then GCCZ core classifies that text through `SiegeLocalCivilianReactionProfile.ResolveSoldierWitnessBloodthirstyFromPersona(...)`. The prompt/fallback may ask whether to order massacre/slaughter only when that persona text indicates cruelty, but it still cannot execute the outcome by itself. This forced soldier inquiry bypasses the generic destructive-inquiry cooldown but is deduplicated per civilian victim, remains GCCZ active-stage gated, and falls back to a visible soldier inquiry message if the immediate AI short-line bridge cannot start.

Cultural-repopulation tag audit: `[ACTION:殖民]` remains a soldier-mediated destructive action only. The fused postprocess bridge applies it only when the active target is a player-allied soldier and the reply is a direct response to the player's current command; civilian replies, ambient chatter, soldier-to-soldier talk, and soldier inquiry echoes are routed to destructive inquiry instead of executing repopulation. GCCZ standalone tests now cover direct civilian, indirect soldier, and valid direct allied-soldier repopulation routing so future prompt/rule edits do not reopen the bug.

## GCCZ regional civilian panic bridge

- Fused AF adapters must keep the global GCCZ civilian `FleeBehavior` suppression, but explicitly allow agents recorded as local regional-conflict fleeing civilians. Those agents should activate Bannerlord's native `AlarmedBehaviorGroup` + `FleeBehavior` so town conflict panic can choose passages/guards when a local fight is active.
- Local regional resistance is now a non-bloodbath defiant/standoff reaction: a few bold or armed civilians may shout, stare down, or briefly hold position, but the adapter must not start `MissionFightHandler`, must not move them into the GCCZ massacre enemy team, and must not make allied soldiers valid local-conflict targets. Full-city combat remains the separate GCCZ massacre path.
- Local regional fleeing civilians must use the AF-style panic movement order: clear daily/scripted usable targets, play frightened civilian action once, try direct navmesh retreat away from the player first, and only fall back to `AlarmedBehaviorGroup` + `FleeBehavior` when a direct retreat target cannot be issued. This prevents regional-conflict bystanders from standing idle when they are not part of the narrow native fight.
- Each regional player-on-civilian conflict debt uses `SiegeRegionalConflictProfile`: the first local hit/down creates one 24m-diameter street-area debt, applies settlement public trust -1 inside the current town, and adds one backend conflict-debt stack. Later hits/downs inside that area do not add more debt; separate areas can add separate backend debt stacks. The player-facing prompt only says regional conflict/panic and must not expose trust deltas, debt counts, or later positive-effect penalties. The fused adapter applies the debt to later relief/inspiration/rally-oath positive public-trust, loyalty, notable-relation, and notable-trust gains; simple mercy remains a unilateral no-kill/no-plunder choice and is not blocked by civilian agreement or regional-conflict debt.
- When regional conflict escalates to massacre, end the narrow native local fight before handing control to GCCZ massacre combat driving.

Follow-up isolation: GCCZ postprocess frequency policy now lives in `SiegePostprocessFrequencyProfile`; fused AF keeps MCM settings and the active-scene runtime throttle inside `DuelSettings`, `AfGcczShoutBridge`, and `ShoutBehavior`. Default is unlimited/current behavior. When unlimited is disabled, low-priority GCCZ postprocess calls are limited to N per 10; direct player destructive/mercy/gather-looking text may bypass only to force an AI postprocess review, but the fixed text check never emits ACTION tags or applies outcomes. The fused bridge resets the throttle bucket on GCCZ mission start/end to avoid cross-mission carryover.

## 2026-06-29 GCCZ native navigation bridge note

- 血洗/区域冲突的“谁逃跑、谁反抗、多少士兵追击”仍由 GCCZ 独立策略决定，不接管 AF 普通城镇冲突结算、犯罪、通缉或 SceneTaunt 状态机。
- 融合树 `SiegeAiInterventionBehavior.cs` 只复用 AF 原版城镇冲突中更稳的移动方式：`AgentNavigator.SetTargetFrame(WorldPosition)`、navmesh 采样、以及远离威胁的 direct retreat 采样。
- 已标记为 GCCZ 逃跑的平民在血洗阶段也必须允许 `FleeBehavior` tick/availability；这是路径行为桥，不改变平民逃跑/反抗判定。
- 独立参数位于 `SiegeAgentWallRescueProfile`，融合树 live adapter 负责 Bannerlord `Mission`/`AgentNavigator` 调用。若 agent 已触发卡住救援、离目标仍超过穿墙阈值且冷却允许，adapter 可以把非玩家 agent 瞬移到已校验的 navmesh 目标点，作为 GCCZ 专用 wall-pass 兜底；普通 AF/原版城镇进入不调用这条路径。

### 2026-06-29 massacre/repopulation fleeing-civilian bridge

- 血洗和屠民迁殖共用 GCCZ massacre combat driver；殖民只是在触发/结算层升级为最高级毁坏与改文化，不另开一套平民 AI。
- 融合树不得在 `PrepareCivilianForMassacreCombat(...)` 开头把所有目标平民统一 `SetTeam(enemyTeam)`。只有 GCCZ 策略判定会反抗的少数平民、携械者、要人/头人、守卫/守军会立即切敌队并接 `FightBehavior`。
- 非反抗平民先进入 `LocalFleeingCivilianAgentIndexes`，清目标/队形/日常 usable，播放受惊动作，并优先用 direct retreat/navmesh 藏身点逃散；这条路径同时服务血洗和殖民，避免触发后全城平民瞬间红名冲向玩家/士兵。
- 70% 追击士兵仍会追猎全部未结算目标。逃跑平民只有在追击士兵已经逼近目标时，才通过 `SiegeMassacreInteractionProfile.CivilianHunterContactSource` 被桥接为可攻击敌对目标；该目标仍维持恐慌/逃跑行为，不被强行改成成建制反击者。

## 2026-06-29 mercy and GCCZ NPC response limit contract

- `[ACTION:宽恕]` is a unilateral player mercy choice: the player clearly declaring no killing, no plunder, no pursuit, or protection of civilians is enough. Civilian fear, refusal, silence, or inability to represent the whole town must not block simple mercy.
- Regional conflict debt still makes higher positive routes (`[ACTION:救济]`, `[ACTION:宣抚]`, `[ACTION:盟誓]`) harder to justify in prompt wording and continues to reduce their later positive settlement/notable effects; it does not block simple mercy.
- Fused AF must expose the GCCZ NPC response limit under MCM group `13. GCCZ攻城后处置`. The limit controls how many NPCs may speak after a GCCZ action-tag/event reaction and how many NPCs may answer one player group shout during the active GCCZ scene. It must not throttle or skip ACTION postprocess itself; ACTION tags still come only from AI postprocess.
- The same MCM group should expose a one-click `GCCZ_Debug.log` export button. The fused AF bridge owns file-system export/open-folder side effects; GCCZ core owns only the diagnostic filename constant.
- Positive civilian morale body reactions use `SiegeCivilianMoraleReactionProfile`: while the active GCCZ mission is running, mercy-track actions clear frightened/panic civilian state, and higher civic-positive actions (`宣抚` / `盟誓`) additionally trigger one-shot civilian cheer animations. The fused AF bridge owns live `Agent` animation/movement cleanup and must not run this in ordinary AF town scenes.
