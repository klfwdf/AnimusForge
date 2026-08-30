using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

static void AssertTrue(bool value, string message)
{
    if (!value)
    {
        throw new InvalidOperationException(message);
    }
}

static GameInteractionSnapshot BuildSnapshot(List<InteractionCandidate> candidates, Dictionary<string, string> facts)
{
    return new GameInteractionSnapshot(
        new InteractionIdentity("session-1", InteractionChannel.SceneShout, "hero-1"),
        new TraceContext("trace-1", 4, 9, "single-player", "1.4"),
        "hello",
        "town-1",
        12,
        8,
        candidates,
        new[] { "hero-1", "hero-2" },
        facts);
}

var candidates = new List<InteractionCandidate>
{
    new InteractionCandidate("hero-2", "NPC", 3, true)
};
var facts = new Dictionary<string, string> { ["mood"] = "calm" };
GameInteractionSnapshot snapshot = BuildSnapshot(candidates, facts);
candidates.Clear();
facts["mood"] = "mutated-after-capture";
AssertTrue(snapshot.Candidates.Count == 1, "snapshot retained a live mutable candidate list");
AssertTrue(snapshot.DetachedFacts["mood"] == "calm", "snapshot retained a live mutable fact map");

bool detachedLookupCalled = false;
var detachedRuleSelector = new LegacyDetachedRuleSelector((userText, secondaryText, runtimeContext, topN, excludedRuleIds) =>
{
    detachedLookupCalled = userText == "hello" && runtimeContext == "town-context" && excludedRuleIds.Contains("scene_only");
    return new DetachedRuleLookupResult(new[] { "rule.dialogue", "rule.dialogue", "rule.trade" });
});
GameInteractionSnapshot detachedRuleSnapshot = BuildSnapshot(
    new List<InteractionCandidate>(),
    new Dictionary<string, string>
    {
        ["rule_runtime_context"] = "town-context",
        ["excluded_rule_ids"] = "scene_only"
    });
RuleSelection detachedSelection = detachedRuleSelector.Select(detachedRuleSnapshot);
AssertTrue(detachedLookupCalled, "detached rule selector did not pass snapshot-only inputs");
AssertTrue(detachedSelection.RuleIds.Count == 2 && detachedSelection.RuleIds[0] == "rule.dialogue", "detached rule selector did not deduplicate rule ids");

var legacyMessages = new List<object>
{
    new Dictionary<string, object> { ["role"] = "system", ["content"] = "system block" },
    new Dictionary<string, object> { ["role"] = "assistant", ["content"] = "assistant block" },
    new Dictionary<string, object> { ["role"] = "unknown", ["content"] = "user fallback" },
    new Dictionary<string, object> { ["role"] = "user", ["content"] = "" }
};
PromptPackage detachedPrompt = LegacyPromptPackageAdapter.FromLegacyMessages(legacyMessages, 0, "model-detached");
legacyMessages[0] = new Dictionary<string, object> { ["role"] = "user", ["content"] = "mutated" };
AssertTrue(detachedPrompt.Messages.Count == 3 && detachedPrompt.Messages[0].Role == "system" && detachedPrompt.Messages[2].Role == "user", "prompt adapter did not normalize/copy legacy messages");
IReadOnlyList<object> roundTripMessages = LegacyPromptPackageAdapter.ToLegacyMessages(detachedPrompt);
AssertTrue(roundTripMessages.Count == 3, "prompt adapter did not produce gateway messages");

DetachedPromptSections detachedSections = new DetachedPromptSections(
    new[] { "stable Persona", "task rules without action tags" },
    new[] { "runtime context", "knowledge/RAG" },
    new[] { "AFEF facts", "rule block" });
InteractionEnvelope composedEnvelope = new InteractionEnvelope(
    snapshot,
    new[] { new PromptMessage("assistant", "prior NPC reply") },
    detachedSections);
PromptPackage composedPrompt = new LegacyDetachedPromptComposer(128, "model-composed").Compose(
    composedEnvelope,
    new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>()),
    new CapabilitySet(new[] { "prompt.compose" }));
AssertTrue(composedPrompt.Messages.Count == 7, "detached composer did not preserve all prompt sections and current input");
AssertTrue(composedPrompt.Messages[0].Role == "system" && composedPrompt.Messages[1].Role == "user" && composedPrompt.Messages[2].Role == "user" && composedPrompt.Messages[3].Role == "assistant" && composedPrompt.Messages[4].Content == "AFEF facts" && composedPrompt.Messages[5].Content == "rule block" && composedPrompt.Messages[6].Content == "hello", "detached composer changed canonical message order");
detachedSections = new DetachedPromptSections(new[] { "system" }, Array.Empty<string>(), Array.Empty<string>(), appendCurrentPlayerInput: false);
PromptPackage noDuplicateInput = new LegacyDetachedPromptComposer().Compose(
    new InteractionEnvelope(snapshot, new[] { new PromptMessage("user", "hello already recorded") }, detachedSections),
    new RuleSelection(Array.Empty<string>(), Array.Empty<string>()),
    new CapabilitySet(Array.Empty<string>()));
AssertTrue(noDuplicateInput.Messages.Count == 2, "detached composer duplicated a channel-owned current input");
DetachedPostprocessPromptSections postprocessSections = new DetachedPostprocessPromptSections(
    new[] { "postprocess system with tag rules" },
    new[] { "history and AFEF facts" },
    new[] { "runtime target facts" });
PromptPackage postprocessPrompt = new LegacyDetachedPostprocessPromptComposer(256, "model-postprocess").Compose(
    new InteractionEnvelope(snapshot, new[] { new PromptMessage("assistant", "prior reply") }, detachedSections, postprocessSections),
    new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>()),
    "visible reply",
    "raw reply with internal tags",
    new PostprocessContext(new[] { "rule.dialogue" }, new[] { "ACTION:DUEL" }, new CapabilitySet(new[] { "action.parse" })));
AssertTrue(postprocessPrompt.Messages.Count == 5 && postprocessPrompt.Messages[0].Role == "system" && postprocessPrompt.Messages[1].Content == "history and AFEF facts" && postprocessPrompt.Messages[2].Role == "assistant" && postprocessPrompt.Messages[3].Content == "runtime target facts" && postprocessPrompt.Messages[4].Content == "[latest_reply]\nvisible reply", "detached postprocess composer changed canonical postprocess order");
DetachedInteractionPromptSections atomicSections = new DetachedInteractionPromptSections(
    new DetachedPromptSections(new[] { "main" }, Array.Empty<string>(), Array.Empty<string>()),
    new DetachedPostprocessPromptSections(new[] { "post" }, Array.Empty<string>(), Array.Empty<string>()));
AssertTrue(atomicSections.Main.SystemSections.Count == 1 && atomicSections.Postprocess.SystemSections.Count == 1, "atomic detached sections bundle lost one of the LLM stages");

var nativeLegacyMessages = new List<object>
{
    new Dictionary<string, object> { ["role"] = "system", ["content"] = "native system" },
    new Dictionary<string, object> { ["role"] = "user", ["content"] = "native prefix" },
    new Dictionary<string, object> { ["role"] = "assistant", ["content"] = "prior native reply" },
    new Dictionary<string, object> { ["role"] = "user", ["content"] = "native suffix" }
};
LegacyNativePromptParity.LegacyNativePromptParityResult nativeMainParity = LegacyNativePromptParity.CompareMainMessages(
    nativeLegacyMessages,
    new[] { "native prefix" },
    new[] { "native suffix" },
    "already recorded input");
AssertTrue(nativeMainParity.Matches && nativeMainParity.DetachedPackage.Messages.Count == 4, "Native old-vs-detached main prompt parity failed");
LegacyNativePromptParity.LegacyNativePromptParityResult nativePostParity = LegacyNativePromptParity.ComparePostprocessBlocks(
    "native post system with tag rules",
    "native post user with history and visible reply");
AssertTrue(nativePostParity.Matches && nativePostParity.DetachedPackage.Messages.Count == 2, "Native old-vs-detached postprocess parity failed");
DetachedInteractionPromptSections nativeAtomicBundle = LegacyNativePromptParity.BuildAtomicBundle(
    nativeMainParity.MainSections,
    nativePostParity.PostprocessSections);
AssertTrue(nativeAtomicBundle.Main.SystemSections[0] == "native system"
    && nativeAtomicBundle.Postprocess.SystemSections[0] == "native post system with tag rules"
    && !nativeAtomicBundle.Main.AppendCurrentPlayerInput
    && !nativeAtomicBundle.Postprocess.AppendLatestVisibleReply,
    "Native parity did not produce an atomic main/postprocess bundle");

var actionParser = new LegacyActionTagParser();
ActionPlan parsedActions = actionParser.Parse(
    "自然文本 [ACTION:GIVE:npc-2:gold=10:note=offer] [ACTION:SECRET:hidden]",
    new PostprocessContext(new[] { "trade" }, new[] { "ACTION:GIVE" }, new CapabilitySet(new[] { "action.parse" })));
AssertTrue(parsedActions.Actions.Count == 1 && parsedActions.Actions[0].Tag == "ACTION:GIVE" && parsedActions.Actions[0].TargetId == "npc-2" && parsedActions.Actions[0].Parameters["gold"] == "10", "action parser did not enforce detached allowlist/parameters");
AssertTrue(parsedActions.RawPostprocessId.Contains("ACTION:SECRET", StringComparison.Ordinal), "action parser did not retain raw postprocess trace");
ActionPlan protocolActions = actionParser.Parse(
    "[A:H_J_P_P_C] [AD:debt-7:30:future payment] [ADP:debt-8] [RELAY:2] [FOL] [ACTION:DUEL] [AFEF NPC行为补充]",
    new PostprocessContext(
        new[] { "trade" },
        new[] { "A:H_J_P_P_C&L", "AD", "ADP", "RELAY", "FOL", "ACTION:DUEL" },
        new CapabilitySet(new[] { "action.parse" })));
AssertTrue(protocolActions.Actions.Count == 6, "detached action parser did not cover the legacy protocol families");
AssertTrue(protocolActions.Actions[0].Tag == "A:H_J_P_P_C" && protocolActions.Actions[0].TargetId == "", "A protocol target parsing changed the short hero-join form");
AssertTrue(protocolActions.Actions[1].Tag == "AD" && protocolActions.Actions[1].TargetId == "debt-7" && protocolActions.Actions[1].Parameters["arg0"] == "30", "AD protocol parameters were not detached correctly");
AssertTrue(protocolActions.Actions[2].Tag == "ADP" && protocolActions.Actions[2].TargetId == "debt-8" && protocolActions.Actions[3].Tag == "RELAY" && protocolActions.Actions[4].Tag == "FOL", "detached non-ACTION protocol tags were not preserved");
ActionPlan rejectedLegacy = actionParser.Parse(
    "[A:H_J_P_P] [ACTION:SECRET]",
    new PostprocessContext(new[] { "trade" }, new[] { "A:H_J_P_P_C&L", "ACTION:GIVE" }, new CapabilitySet(Array.Empty<string>())));
AssertTrue(rejectedLegacy.Actions.Count == 0, "detached action parser accepted a legacy or unauthorized tag");
ActionPlan nestedAsset = actionParser.Parse(
    "[ACTION:GIVE_ASSET:[ROT]佛雷甲:1][ACTION:GIVE_ASSET:火焰:北境版:2]",
    new PostprocessContext(
        new[] { "reward" },
        new[] { "ACTION:GIVE_ASSET" },
        new CapabilitySet(new[] { "action.parse" })));
AssertTrue(
    nestedAsset.Actions.Count == 2
        && nestedAsset.Actions[0].TargetId == "[ROT]佛雷甲"
        && nestedAsset.Actions[0].Parameters["quantity"] == "1"
        && nestedAsset.Actions[1].TargetId == "火焰:北境版"
        && nestedAsset.Actions[1].Parameters["quantity"] == "2",
    "detached action parser truncated nested RichText asset tags");
ActionPlan catalogActions = actionParser.Parse(
    "[ACTION:DUEL][ACTION:DUEL_STAKE_GOLD:100][ACTION:SECRET:hidden][ACTION:8]",
    new PostprocessContext(
        new[] { "all-existing-domains" },
        LegacyActionTagCatalog.DefaultAllowedTagFamilies,
        new CapabilitySet(new[] { "action.parse" })));
AssertTrue(
    catalogActions.Actions.Count == 3
        && catalogActions.Actions[0].Tag == "ACTION:DUEL"
        && catalogActions.Actions[1].Tag == "ACTION:DUEL_STAKE_GOLD"
        && catalogActions.Actions[2].Tag == "ACTION:8",
    "finite action catalog did not preserve approved tags while rejecting an unknown family");

var economyAdapter = new LegacyEconomyRewardDebtAdapter();
ActionPlan economyActionPlan = actionParser.Parse(
    "[ACTION:GIVE_ASSET:[ROT]佛雷甲:2] [ACTION:GIVE_GOLD:npc-2:75] [AD:120:30:P:late payment] [ADP:debt-7] [ACTION:SETTLEMENT_TRANSFER:TO_NPC:settlement-3] [ACTION:DUEL]",
    new PostprocessContext(
        new[] { "reward", "debt", "settlement" },
        new[] { "ACTION:GIVE_ASSET", "ACTION:GIVE_GOLD", "AD", "ADP", "ACTION:SETTLEMENT_TRANSFER", "ACTION:DUEL" },
        new CapabilitySet(new[]
        {
            EconomyRewardDebtCapabilityIds.GiveAsset,
            EconomyRewardDebtCapabilityIds.GiveGold,
            EconomyRewardDebtCapabilityIds.DebtCreate,
            EconomyRewardDebtCapabilityIds.DebtResolve,
            EconomyRewardDebtCapabilityIds.SettlementTransfer
        })));
EconomyRewardDebtReplayPlan economyReplay = economyAdapter.Plan(
    economyActionPlan,
    new CapabilitySet(new[]
    {
        EconomyRewardDebtCapabilityIds.GiveAsset,
        EconomyRewardDebtCapabilityIds.GiveGold,
        EconomyRewardDebtCapabilityIds.DebtCreate,
        EconomyRewardDebtCapabilityIds.DebtResolve,
        EconomyRewardDebtCapabilityIds.SettlementTransfer
    }));
AssertTrue(economyReplay.Actions.Count == 5 && economyReplay.HasExcludedActions, "economy adapter did not separate economy actions from non-applicable actions");
AssertTrue(
    economyReplay.Actions[0].Kind == EconomyRewardDebtActionKind.GiveAsset
        && economyReplay.Actions[0].AssetToken == "[ROT]佛雷甲"
        && economyReplay.Actions[0].QuantityToken == "2"
        && economyReplay.Actions[1].Kind == EconomyRewardDebtActionKind.GiveGold
        && economyReplay.Actions[1].AmountToken == "75"
        && economyReplay.Actions[2].Kind == EconomyRewardDebtActionKind.DebtCreate
        && economyReplay.Actions[2].AmountToken == "120"
        && economyReplay.Actions[2].DirectionToken == "P"
        && economyReplay.Actions[3].DebtId == "debt-7"
        && economyReplay.Actions[4].DirectionToken == "TO_NPC"
        && economyReplay.Actions[4].SettlementToken == "settlement-3",
    "economy adapter lost reward/debt/settlement stable tokens");
EconomyRewardDebtReplayPlan missingCapabilityReplay = economyAdapter.Plan(
    economyActionPlan,
    new CapabilitySet(new[] { EconomyRewardDebtCapabilityIds.GiveAsset }));
AssertTrue(
    missingCapabilityReplay.Actions.Count == 1
        && missingCapabilityReplay.ExclusionReasons.Any(reason => reason.Contains("capability_missing", StringComparison.OrdinalIgnoreCase)),
    "economy adapter did not return an explicit capability exclusion reason");
ActionPlan invalidDebtPlan = actionParser.Parse(
    "[AD:0:days:P:invalid]",
    new PostprocessContext(new[] { "debt" }, new[] { "AD" }, new CapabilitySet(Array.Empty<string>())));
EconomyRewardDebtReplayPlan invalidDebtReplay = economyAdapter.Plan(
    invalidDebtPlan,
    new CapabilitySet(new[] { EconomyRewardDebtCapabilityIds.DebtCreate }));
AssertTrue(
    invalidDebtReplay.Actions.Count == 0
        && invalidDebtReplay.ExclusionReasons.Any(reason => reason.Contains("invalid_amount", StringComparison.OrdinalIgnoreCase)),
    "economy adapter accepted malformed debt syntax");

int nativeActionCallbackCount = 0;
var nativeActionExecutor = new LegacyNativeActionPlanExecutor((plan, current) =>
{
    nativeActionCallbackCount++;
    return InteractionStatus.Executed;
});
ActionPlan nativeActionPlan = actionParser.Parse(
    "NPC 已同意 [ACTION:DUEL] [ACTION:MOOD:NEUTRAL]",
    new PostprocessContext(
        new[] { "duel" },
        new[] { "ACTION:DUEL", "ACTION:MOOD" },
        new CapabilitySet(new[] { "action.parse" })));
AssertTrue(
    nativeActionExecutor.ValidateAndExecute(nativeActionPlan, snapshot) == InteractionStatus.Executed
        && nativeActionCallbackCount == 1,
    "Native ActionPlan executor did not execute an exact authorized raw plan");
ActionPlan tamperedNativeActionPlan = new ActionPlan(
    nativeActionPlan.Actions,
    nativeActionPlan.RawPostprocessId + " [ACTION:SECRET]");
AssertTrue(
    nativeActionExecutor.ValidateAndExecute(tamperedNativeActionPlan, snapshot) == InteractionStatus.RejectedByValidation
        && nativeActionCallbackCount == 1,
    "Native ActionPlan executor allowed an extra raw action tag");
var throwingNativeActionExecutor = new LegacyNativeActionPlanExecutor((plan, current) => throw new InvalidOperationException("simulated"));
AssertTrue(
    throwingNativeActionExecutor.ValidateAndExecute(nativeActionPlan, snapshot) == InteractionStatus.RejectedByValidation,
    "Native ActionPlan executor did not isolate a host action exception");

var successGateway = new FakeGateway(new LlmGenerateResult(LlmResultStatus.Succeeded, "raw-with-action", 2, 3, ""));
var pipeline = BuildPipeline(new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>()), successGateway);
InteractionResult success = pipeline.GenerateAsync(
    new InteractionEnvelope(snapshot, new[] { new PromptMessage("user", "hello") }),
    new LlmProviderSnapshot("main", "https://example.invalid", "model-a", 1000, 128),
    CancellationToken.None).GetAwaiter().GetResult();
AssertTrue(success.Status == InteractionStatus.Succeeded, "successful pipeline did not succeed");
AssertTrue(success.VisibleReply == "visible-reply", "visible reply normalization was not applied");
AssertTrue(success.ActionPlan.Actions.Count == 1, "ActionPlan was not produced");
AssertTrue(successGateway.CallCount == 1, "gateway call count mismatch");
LlmGenerateMetadata metadataFixture = new LlmGenerateMetadata(
    statusCode: 200,
    finishReason: "stop",
    resolvedRoute: "shared",
    isOutputTruncated: false,
    promptCacheHitTokens: 4);
LlmGenerateResult metadataResult = new LlmGenerateResult(
    LlmResultStatus.Succeeded,
    "ok",
    12,
    3,
    "",
    metadataFixture);
AssertTrue(
    metadataResult.Metadata.StatusCode == 200
        && metadataResult.Metadata.PromptCacheHitTokens == 4
        && metadataResult.Metadata.FinishReason == "stop",
    "shared LLM result did not preserve non-secret domain metadata");
byte[] audioSource = { 1, 2, 3 };
TtsSynthesisResult audioResult = new TtsSynthesisResult(true, audioSource, 200, "");
audioSource[0] = 9;
AssertTrue(
    audioResult.Success && audioResult.AudioBytes[0] == 1 && audioResult.AudioBytes.Length == 3,
    "TTS result did not detach audio bytes from the caller buffer");

var skippedGateway = new FakeGateway(new LlmGenerateResult(LlmResultStatus.Succeeded, "should-not-run", 0, 0, ""));
var skippedPipeline = BuildPipeline(new RuleSelection(Array.Empty<string>(), Array.Empty<string>()), skippedGateway);
InteractionResult skipped = skippedPipeline.GenerateAsync(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    new LlmProviderSnapshot("main", "https://example.invalid", "model-a", 1000, 128),
    CancellationToken.None).GetAwaiter().GetResult();
AssertTrue(skipped.Status == InteractionStatus.SkippedByEligibility, "ineligible interaction was not skipped");
AssertTrue(skippedGateway.CallCount == 0, "ineligible interaction called the gateway");

var staleGateway = new FakeGateway(new LlmGenerateResult(LlmResultStatus.Cancelled, "", 0, 0, "cancelled"));
var stalePipeline = BuildPipeline(new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>()), staleGateway);
InteractionResult stale = stalePipeline.GenerateAsync(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    new LlmProviderSnapshot("main", "https://example.invalid", "model-a", 1000, 128),
    CancellationToken.None).GetAwaiter().GetResult();
AssertTrue(stale.Status == InteractionStatus.CancelledAsStale, "cancelled LLM result was not mapped to stale");

var enabledModules = new Dictionary<string, bool> { ["conversation"] = true };
var providers = new Dictionary<string, LlmProviderSnapshot>
{
    ["main"] = new LlmProviderSnapshot("main", "https://example.invalid", "model-a", 1000, 128)
};
RuntimeConfigSnapshot config = new RuntimeConfigSnapshot("single-player", 1, enabledModules, providers);
enabledModules["conversation"] = false;
AssertTrue(config.IsModuleEnabled("conversation"), "config snapshot changed after source reload");
LlmProviderSnapshot provider;
AssertTrue(config.TryGetProvider("main", out provider) && provider.Model == "model-a", "provider snapshot missing");

bool publishedModuleEnabled = true;
long publishedGeneration = 10;
bool failSnapshotFactory = false;
int snapshotFactoryCalls = 0;
var snapshotStore = new RuntimeConfigSnapshotStore(() =>
{
    snapshotFactoryCalls++;
    if (failSnapshotFactory)
    {
        return null;
    }
    return new RuntimeConfigSnapshot(
        "store-profile",
        publishedGeneration,
        new Dictionary<string, bool> { ["conversation"] = publishedModuleEnabled },
        providers);
});
RuntimeConfigSnapshot inFlightSnapshot = snapshotStore.Capture();
publishedModuleEnabled = false;
publishedGeneration = 11;
AssertTrue(
    inFlightSnapshot.IsModuleEnabled("conversation")
        && snapshotStore.Capture().ConfigurationGeneration == 10,
    "runtime config store exposed mutable source changes to an in-flight snapshot");
RuntimeConfigSnapshot reloadedSnapshot;
AssertTrue(
    snapshotStore.TryReload(out reloadedSnapshot)
        && reloadedSnapshot != null
        && !reloadedSnapshot.IsModuleEnabled("conversation")
        && reloadedSnapshot.ConfigurationGeneration == 11
        && snapshotFactoryCalls == 2,
    "runtime config store did not atomically publish the replacement snapshot");
failSnapshotFactory = true;
RuntimeConfigSnapshot failedReloadSnapshot;
AssertTrue(
    !snapshotStore.TryReload(out failedReloadSnapshot)
        && failedReloadSnapshot == null
        && ReferenceEquals(snapshotStore.Capture(), reloadedSnapshot),
    "failed runtime config reload discarded the last known-good snapshot");

var stagedGateway = new StagedGateway();
var fullPipeline = new FullInteractionPipeline(
    new FakeRuleSelector(new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>())),
    new FakePromptComposer(),
    new FakePostprocessContextBuilder(),
    new FakePostprocessPromptComposer(),
    stagedGateway,
    new FakeNormalizer(),
    new FakePostprocessor(),
    new CapabilitySet(new[] { "llm.generate", "action.parse" }));
InteractionResult full = fullPipeline.GenerateAsync(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    provider,
    CancellationToken.None).GetAwaiter().GetResult();
AssertTrue(full.Status == InteractionStatus.Succeeded, "three-stage pipeline did not succeed");
AssertTrue(full.RawReply == "main-raw" && full.RawPostprocessReply == "post-raw", "three-stage raw outputs were not retained");
AssertTrue(stagedGateway.Stages.Count == 2 && stagedGateway.Stages[0] == InteractionStage.MainReply && stagedGateway.Stages[1] == InteractionStage.Postprocess, "three-stage order was not preserved");

var failingPostprocessGateway = new StagedGateway(failPostprocess: true);
var failingPostprocessPipeline = new FullInteractionPipeline(
    new FakeRuleSelector(new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>())),
    new FakePromptComposer(),
    new FakePostprocessContextBuilder(),
    new FakePostprocessPromptComposer(),
    failingPostprocessGateway,
    new FakeNormalizer(),
    new FakePostprocessor(),
    new CapabilitySet(new[] { "llm.generate", "action.parse" }));
InteractionResult degradedPostprocess = failingPostprocessPipeline.GenerateAsync(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    provider,
    CancellationToken.None).GetAwaiter().GetResult();
AssertTrue(degradedPostprocess.Status == InteractionStatus.Succeeded && degradedPostprocess.VisibleReply == "visible-reply", "postprocess failure erased the visible main reply");
AssertTrue(degradedPostprocess.ActionPlan.Actions.Count == 0 && degradedPostprocess.ErrorCode.StartsWith("postprocess_", StringComparison.Ordinal), "postprocess failure was not isolated");

var memory = new RecordingMemory();
var executor = new RecordingExecutor(InteractionStatus.Executed);
var committer = new InteractionResultCommitter();
InteractionResult committable = new InteractionResult(
    InteractionStatus.Succeeded,
    "visible",
    new ActionPlan(new[] { new ActionRequest("action.test", "hero-2", new Dictionary<string, string>()) }, "post-1"),
    new[] { new FactRecord("action", "hero-2", "confirmed") },
    "",
    "raw",
    "post-raw");
InteractionCommitResult committed = committer.Commit(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    committable,
    executor,
    memory);
AssertTrue(committed.Status == InteractionStatus.Executed && committed.ActionsExecuted && memory.Roles.Count == 2, "successful result was not committed in user/assistant order");
AssertTrue(memory.Facts.Count == 1 && executor.CallCount == 1, "confirmed facts were not gated by action execution");

var rejectedMemory = new RecordingMemory();
var rejectedCommit = committer.Commit(
    new InteractionEnvelope(new GameInteractionSnapshot(
        new InteractionIdentity("rejected-session-legacy", InteractionChannel.SceneShout, "hero-2"),
        new TraceContext("rejected-trace-legacy", 4, 9, "single-player", "1.4"),
        "hello", "town-1", 12, 8, Array.Empty<InteractionCandidate>(), Array.Empty<string>(), new Dictionary<string, string>()),
        Array.Empty<PromptMessage>()),
    committable,
    new RecordingExecutor(InteractionStatus.RejectedByValidation),
    rejectedMemory);
AssertTrue(rejectedCommit.Status == InteractionStatus.RejectedByValidation && rejectedMemory.Facts.Count == 0, "rejected action wrote confirmed facts");
var staleCommit = committer.Commit(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    stale,
    executor,
    new RecordingMemory());
AssertTrue(staleCommit.Status == InteractionStatus.CancelledAsStale && executor.CallCount == 1, "stale result reached the commit boundary");

var batchMemory = new RecordingBatchMemory();
var batchCommitter = new InteractionResultCommitter();
MemoryCommitReceiptCache.ClearForTests();
var batchExecutor = new RecordingExecutor(InteractionStatus.Executed);
InteractionCommitResult firstBatchCommit = batchCommitter.Commit(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    committable,
    batchExecutor,
    batchMemory);
InteractionCommitResult duplicateBatchCommit = batchCommitter.Commit(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    committable,
    batchExecutor,
    batchMemory);
AssertTrue(
    firstBatchCommit.Status == InteractionStatus.Executed
        && duplicateBatchCommit.Status == InteractionStatus.Executed
        && batchMemory.Commits.Count == 1
        && batchExecutor.CallCount == 1
        && batchMemory.Commits[0].UserText == "hello"
        && batchMemory.Commits[0].AssistantText == "visible"
        && batchMemory.Commits[0].ConfirmedFacts.Count == 1,
    "batch memory commit did not preserve user/assistant/facts or suppress a duplicate");

var inboundSeedBatchMemory = new RecordingBatchMemory();
InteractionCommitResult inboundSeedCommit = batchCommitter.Commit(
    new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "inbound reply", new ActionPlan(Array.Empty<ActionRequest>(), ""), Array.Empty<FactRecord>(), ""),
    null,
    inboundSeedBatchMemory,
    appendPlayerInput: false);
AssertTrue(
    inboundSeedCommit.Status == InteractionStatus.Succeeded
        && inboundSeedBatchMemory.Commits.Count == 1
        && string.IsNullOrEmpty(inboundSeedBatchMemory.Commits[0].UserText)
        && inboundSeedBatchMemory.Commits[0].AssistantText == "inbound reply",
    "inbound NPC seed was incorrectly committed as user history");

var rejectedBatchMemory = new RecordingBatchMemory();
InteractionCommitResult rejectedBatch = batchCommitter.Commit(
    new InteractionEnvelope(new GameInteractionSnapshot(
        new InteractionIdentity("rejected-session", InteractionChannel.SceneShout, "hero-rejected"),
        new TraceContext("rejected-trace", 4, 9, "single-player", "1.4"),
        "reject me", "town-1", 12, 8, Array.Empty<InteractionCandidate>(), Array.Empty<string>(), new Dictionary<string, string>()),
    Array.Empty<PromptMessage>()),
    committable,
    new RecordingExecutor(InteractionStatus.RejectedByValidation),
    rejectedBatchMemory);
AssertTrue(
    rejectedBatch.Status == InteractionStatus.RejectedByValidation
        && rejectedBatchMemory.Commits.Count == 1
        && rejectedBatchMemory.Commits[0].ConfirmedFacts.Count == 0,
    "rejected action wrote confirmed facts through the batch boundary");

long currentGeneration = 4;
using (var coordinator = new InteractionRequestCoordinator(pipeline, () => currentGeneration))
{
    InteractionResult coordinated = coordinator.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(coordinated.Status == InteractionStatus.Succeeded, "coordinator did not run an enabled provider");

    currentGeneration = 5;
    InteractionResult staleBeforeStart = coordinator.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(staleBeforeStart.Status == InteractionStatus.CancelledAsStale, "generation change was not rejected before start");
}

using (var cancelledCoordinator = new InteractionRequestCoordinator(pipeline, () => 4L))
using (var cancellation = new CancellationTokenSource())
{
    cancellation.Cancel();
    InteractionResult cancelled = cancelledCoordinator.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        cancellation.Token).GetAwaiter().GetResult();
    AssertTrue(cancelled.Status == InteractionStatus.CancelledAsStale, "external cancellation was not isolated as stale");
}

var compositionGateway = new FakeGateway(new LlmGenerateResult(LlmResultStatus.Succeeded, "composed", 0, 0, ""));
var ports = new LegacyInteractionPipelinePorts(
    snapshotInput => new RuleSelection(new[] { "rule.dialogue" }, Array.Empty<string>()),
    (envelope, selection, capabilities) => new PromptPackage(new[] { new PromptMessage("user", envelope.Snapshot.PlayerText) }, 128, "model-a"),
    (snapshotInput, selection, capabilities) => new PostprocessContext(selection.RuleIds, new[] { "action" }, capabilities),
    (rawText, context) => new ActionPlan(Array.Empty<ActionRequest>(), ""),
    (rawText, tagFamilies) => rawText,
    new CapabilitySet(new[] { "llm.generate", "action.parse" }));
using (var composedCoordinator = LegacyInteractionPipelineComposition.Create(ports, compositionGateway, () => 4L))
{
    InteractionResult composed = composedCoordinator.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(composed.Status == InteractionStatus.Succeeded && compositionGateway.CallCount == 1, "composition root did not connect the shared ports");
}

var nativePorts = new LegacyInteractionPipelinePorts(
    ports.SelectRules,
    ports.ComposePrompt,
    ports.BuildPostprocessContext,
    ports.ParseActions,
    ports.NormalizeVisibleReply,
    ports.Capabilities,
    (envelope, selection, visibleReply, rawReply, context) => new PromptPackage(new[] { new PromptMessage("user", "post:" + visibleReply) }, 64, "model-a"));
long nativeGeneration = 4;
using (var nativeFacade = new LegacyNativeConversationFacade(nativePorts, new StagedGateway(), () => nativeGeneration, text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
{
    InteractionResult nativeResult = nativeFacade.GenerateAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(nativeResult.Status == InteractionStatus.Succeeded && nativeResult.RawPostprocessReply == "post-raw", "native facade did not use the three-stage sidecar");
    nativeGeneration = 5;
    var nativeExecutor = new RecordingExecutor(InteractionStatus.Executed);
    InteractionCommitResult staleNativeCommit = nativeFacade.Commit(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        committable,
        nativeExecutor,
        new RecordingMemory());
    AssertTrue(staleNativeCommit.Status == InteractionStatus.CancelledAsStale && nativeExecutor.CallCount == 0, "native facade allowed a stale result to execute");
}

using (var channelFacade = new LegacyChannelInteractionFacade(nativePorts, new StagedGateway(), () => 4L, text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
{
    InteractionResult channelResult = channelFacade.GenerateAsync(
        channelFacade.Capture("channel facade"),
        config,
        "conversation",
        "main",
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(channelResult.Status == InteractionStatus.Succeeded, "channel-neutral facade did not share the coordinator lifecycle");
}

var channelReplayCases = new[]
{
    (Channel: InteractionChannel.SceneShout, Module: "scene-shout", Subject: "scene-hero"),
    (Channel: InteractionChannel.NativeConversation, Module: "conversation", Subject: "native-hero"),
    (Channel: InteractionChannel.Courier, Module: "courier", Subject: "courier-hero")
};
foreach (var channelCase in channelReplayCases)
{
    GameInteractionSnapshot channelSnapshot = new GameInteractionSnapshot(
        new InteractionIdentity("replay-" + channelCase.Module, channelCase.Channel, channelCase.Subject),
        new TraceContext("trace-" + channelCase.Module, 4, 9, "single-player", "1.4"),
        "channel replay input",
        "town-1",
        12,
        8,
        new[] { new InteractionCandidate(channelCase.Subject, "NPC", 3, true) },
        new[] { channelCase.Subject },
        new Dictionary<string, string>());
    RuntimeConfigSnapshot channelConfig = new RuntimeConfigSnapshot(
        "single-player",
        1,
        new Dictionary<string, bool> { [channelCase.Module] = true },
        providers);
    using (var replayFacade = new LegacyChannelInteractionFacade(
        nativePorts,
        new StagedGateway(),
        () => 4L,
        text => new InteractionEnvelope(channelSnapshot, Array.Empty<PromptMessage>())))
    {
        InteractionEnvelope replayEnvelope = replayFacade.Capture("channel replay input");
        InteractionResult replayResult = replayFacade.GenerateAsync(
            replayEnvelope,
            channelConfig,
            channelCase.Module,
            "main",
            CancellationToken.None).GetAwaiter().GetResult();
        var replayMemory = new RecordingMemory();
        var replayExecutor = new RecordingExecutor(InteractionStatus.Executed);
        InteractionCommitResult replayCommit = replayFacade.Commit(
            replayEnvelope,
            replayResult,
            replayExecutor,
            replayMemory);
        AssertTrue(
            replayEnvelope.Snapshot.Identity.Channel == channelCase.Channel
                && replayResult.Status == InteractionStatus.Succeeded
                && replayResult.RawPostprocessReply == "post-raw"
                && replayCommit.Status == InteractionStatus.Succeeded
                && replayExecutor.CallCount == 0
                && replayMemory.Roles.SequenceEqual(new[] { "user", "assistant" }),
            "channel opt-in replay did not preserve identity, three stages, commit, or history for " + channelCase.Channel
                + " channel=" + replayEnvelope.Snapshot.Identity.Channel
                + " result=" + replayResult.Status
                + " rawPost=" + replayResult.RawPostprocessReply
                + " commit=" + replayCommit.Status
                + " executorCalls=" + replayExecutor.CallCount
                + " roles=" + string.Join(",", replayMemory.Roles));
    }
}

using (var detachedHostFacade = new LegacyChannelInteractionFacade(nativePorts, new StagedGateway(), () => 4L, text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
{
    var detachedHost = new DetachedInteractionHost(
        detachedHostFacade.Capture,
        detachedHostFacade.GenerateAsync,
        detachedHostFacade.Commit);
    var detachedHostMemory = new RecordingMemory();
    int detachedHostDispatchCount = 0;
    int detachedHostAfterCommitCount = 0;
    bool detachedHostAfterCommitRanInsideDispatch = false;
    DetachedInteractionHostResult detachedHostResult = detachedHost.ExecuteAsync(
        "host lifecycle",
        config,
        "conversation",
        "main",
        envelope => new RecordingExecutor(InteractionStatus.Executed),
        envelope => detachedHostMemory,
        (envelope, commit) =>
        {
            detachedHostDispatchCount++;
            bool dispatching = true;
            try
            {
                InteractionCommitResult committedResult = commit();
                detachedHostAfterCommitRanInsideDispatch = dispatching && detachedHostAfterCommitCount == 1;
                return Task.FromResult(committedResult);
            }
            finally
            {
                dispatching = false;
            }
        },
        () => Task.FromResult("host legacy fallback"),
        CancellationToken.None,
        (envelope, result, commit) => detachedHostAfterCommitCount++).GetAwaiter().GetResult();
    AssertTrue(
        !detachedHostResult.UsedLegacyFallback
            && detachedHostResult.Status == InteractionStatus.Succeeded
            && detachedHostResult.Commit != null
            && detachedHostMemory.Roles.Count == 2
            && detachedHostDispatchCount == 1
            && detachedHostAfterCommitCount == 1
            && detachedHostAfterCommitRanInsideDispatch,
        "detached interaction host did not unify capture, generate, commit and memory");

    var inboundSeedMemory = new RecordingMemory();
    int inboundAfterCommitCount = 0;
    RuntimeConfigSnapshot courierConfig = new RuntimeConfigSnapshot(
        "single-player",
        1,
        new Dictionary<string, bool> { ["courier"] = true },
        providers);
    DetachedInteractionHostResult inboundSeedResult = detachedHost.ExecuteAsync(
        "npc seed",
        courierConfig,
        "courier",
        "main",
        envelope => null,
        envelope => inboundSeedMemory,
        (envelope, commit) => Task.FromResult(commit()),
        () => Task.FromResult("inbound legacy fallback"),
        CancellationToken.None,
        (envelope, result, commit) => inboundAfterCommitCount++,
        appendPlayerInput: false).GetAwaiter().GetResult();
    AssertTrue(
        !inboundSeedResult.UsedLegacyFallback
            && inboundSeedResult.Status == InteractionStatus.Succeeded
            && inboundSeedMemory.Roles.Count == 1
            && inboundSeedMemory.Roles[0] == "assistant"
            && inboundAfterCommitCount == 1,
        "detached inbound host wrote the NPC seed as player history or missed commit callback");

    int staleDispatchCount = 0;
    int staleAfterCommitCount = 0;
    using (var staleHostFacade = new LegacyChannelInteractionFacade(nativePorts, new StagedGateway(), () => 5L, text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
    {
        var staleHost = new DetachedInteractionHost(
            staleHostFacade.Capture,
            staleHostFacade.GenerateAsync,
            staleHostFacade.Commit);
        DetachedInteractionHostResult staleHostResult = staleHost.ExecuteAsync(
            "stale host",
            config,
            "conversation",
            "main",
            envelope => new RecordingExecutor(InteractionStatus.Executed),
            envelope => new RecordingMemory(),
            (envelope, commit) =>
            {
                staleDispatchCount++;
                return Task.FromResult(commit());
            },
            () => Task.FromResult("stale fallback must not run"),
            CancellationToken.None,
            (envelope, result, commit) => staleAfterCommitCount++).GetAwaiter().GetResult();
        AssertTrue(
            staleHostResult.Status == InteractionStatus.CancelledAsStale
                && !staleHostResult.UsedLegacyFallback
                && staleDispatchCount == 0
                && staleAfterCommitCount == 0,
            "detached host dispatched or called afterCommit for a stale result");
    }

    int rejectedAfterCommitCount = 0;
    using (var rejectedHostFacade = new LegacyChannelInteractionFacade(nativePorts, new StagedGateway(), () => 4L, text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
    {
        var rejectedHost = new DetachedInteractionHost(
            rejectedHostFacade.Capture,
            rejectedHostFacade.GenerateAsync,
            (envelope, result, actionExecutor, memory, appendPlayerInput) => new InteractionCommitResult(
                InteractionStatus.RejectedByValidation,
                false,
                false,
                "contract_rejected"));
        DetachedInteractionHostResult rejectedHostResult = rejectedHost.ExecuteAsync(
            "rejected host",
            config,
            "conversation",
            "main",
            envelope => null,
            envelope => new RecordingMemory(),
            (envelope, commit) => Task.FromResult(commit()),
            () => Task.FromResult("rejected fallback must not run"),
            CancellationToken.None,
            (envelope, result, commit) => rejectedAfterCommitCount++).GetAwaiter().GetResult();
        AssertTrue(
            rejectedHostResult.Status == InteractionStatus.RejectedByValidation
                && !rejectedHostResult.UsedLegacyFallback
                && rejectedAfterCommitCount == 0,
            "detached host called afterCommit for a rejected commit");
    }
}

nativeGeneration = 4;
using (var nativeOptInFacade = new LegacyNativeConversationFacade(nativePorts, new StagedGateway(), () => nativeGeneration, text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
{
    var nativeRunner = new LegacyNativeConversationOptInRunner(nativeOptInFacade);
    var nativeRunnerMemory = new RecordingMemory();
    var nativeRunnerExecutor = new RecordingExecutor(InteractionStatus.Executed);
    int nativeRunnerCommitCallbackCount = 0;
    LegacyNativeConversationOptInResult nativeOptInResult = nativeRunner.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        result =>
        {
            nativeRunnerCommitCallbackCount++;
            return nativeOptInFacade.Commit(
                new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
                result,
                nativeRunnerExecutor,
                nativeRunnerMemory);
        },
        () => Task.FromResult("legacy-fallback"),
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(!nativeOptInResult.UsedLegacyFallback
        && nativeOptInResult.Status == InteractionStatus.Succeeded
        && nativeOptInResult.VisibleReply == "main-raw"
        && nativeRunnerCommitCallbackCount == 1
        && nativeRunnerMemory.Roles.Count == 2,
        "Native opt-in runner did not complete detached generate and main-thread commit");
}

using (var failingNativeFacade = new LegacyNativeConversationFacade(
    nativePorts,
    new FakeGateway(new LlmGenerateResult(LlmResultStatus.RetryableFailure, "", 0, 0, "temporary")),
    () => 4L,
    text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
{
    var failingRunner = new LegacyNativeConversationOptInRunner(failingNativeFacade);
    LegacyNativeConversationOptInResult fallbackResult = failingRunner.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        result => null,
        () => Task.FromResult("legacy-fallback"),
        CancellationToken.None).GetAwaiter().GetResult();
    AssertTrue(fallbackResult.UsedLegacyFallback && fallbackResult.VisibleReply == "legacy-fallback", "Native opt-in runner did not fall back to legacy Native on detached failure");
}

using (var cancelledNativeFacade = new LegacyNativeConversationFacade(
    nativePorts,
    new StagedGateway(),
    () => 4L,
    text => new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>())))
using (var cancelledNativeToken = new CancellationTokenSource())
{
    cancelledNativeToken.Cancel();
    var cancelledRunner = new LegacyNativeConversationOptInRunner(cancelledNativeFacade);
    LegacyNativeConversationOptInResult cancelledResult = cancelledRunner.ExecuteAsync(
        new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>()),
        config,
        "conversation",
        "main",
        result => throw new InvalidOperationException("cancelled result reached commit"),
        () => Task.FromResult("legacy-fallback-must-not-run"),
        cancelledNativeToken.Token).GetAwaiter().GetResult();
    AssertTrue(cancelledResult.Status == InteractionStatus.CancelledAsStale
        && !cancelledResult.UsedLegacyFallback,
        "Native opt-in runner retried a stale/cancelled request through the legacy path");
}

Console.WriteLine("PASS interactionPipeline cases=40 immutableSnapshot=true configReloadIsolation=true runtimeConfigAtomicReload=true runtimeConfigFailureIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true channelFacade=true channelOptInReplay=true detachedHost=true detachedHostAfterCommit=true inboundSeedNoUserHistory=true detachedHostStaleIsolation=true detachedHostRejectedIsolation=true nativeOptInRunner=true nativeFallback=true nativeCancelIsolation=true nativeActionExecutor=true nativeActionRawIntegrity=true detachedRuleSelector=true promptAdapter=true actionTagParser=true detachedPromptComposer=true detachedPostprocessComposer=true atomicDetachedSections=true nativeMainParity=true nativePostprocessParity=true nativeAtomicBundle=true");

static InteractionPipeline BuildPipeline(RuleSelection selection, FakeGateway gateway)
{
    return new InteractionPipeline(
        new FakeRuleSelector(selection),
        new FakePromptComposer(),
        new FakePostprocessContextBuilder(),
        gateway,
        new FakeNormalizer(),
        new FakePostprocessor(),
        new CapabilitySet(new[] { "llm.generate", "action.parse" }));
}

sealed class FakeRuleSelector : IRuleSelector
{
    private readonly RuleSelection _selection;
    public FakeRuleSelector(RuleSelection selection) => _selection = selection;
    public RuleSelection Select(GameInteractionSnapshot snapshot) => _selection;
}

sealed class FakePromptComposer : IPromptPackageComposer
{
    public PromptPackage Compose(InteractionEnvelope envelope, RuleSelection selection, CapabilitySet capabilities)
    {
        return new PromptPackage(new[] { new PromptMessage("user", envelope.Snapshot.PlayerText) }, 128, "model-a");
    }
}

sealed class FakePostprocessContextBuilder : IPostprocessContextBuilder
{
    public PostprocessContext Build(GameInteractionSnapshot snapshot, RuleSelection selection, CapabilitySet capabilities)
    {
        return new PostprocessContext(selection.RuleIds, new[] { "action" }, capabilities);
    }
}

sealed class FakeGateway : ILlmGateway
{
    private readonly LlmGenerateResult _result;
    public FakeGateway(LlmGenerateResult result) => _result = result;
    public int CallCount { get; private set; }
    public Task<LlmGenerateResult> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}

sealed class StagedGateway : ILlmGateway
{
    private readonly bool _failPostprocess;
    public StagedGateway(bool failPostprocess = false) => _failPostprocess = failPostprocess;
    public List<InteractionStage> Stages { get; } = new List<InteractionStage>();
    public Task<LlmGenerateResult> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        Stages.Add(request.Stage);
        if (request.Stage == InteractionStage.Postprocess && _failPostprocess)
        {
            return Task.FromResult(new LlmGenerateResult(LlmResultStatus.RetryableFailure, "", 0, 0, "temporary"));
        }
        string raw = request.Stage == InteractionStage.Postprocess ? "post-raw" : "main-raw";
        return Task.FromResult(new LlmGenerateResult(LlmResultStatus.Succeeded, raw, 0, 0, ""));
    }
}

sealed class FakeNormalizer : IVisibleReplyNormalizer
{
    public string Normalize(string rawText, IEnumerable<string> internalTagFamilies) => "visible-reply";
}

sealed class FakePostprocessor : IActionPostprocessor
{
    public ActionPlan Parse(string rawText, PostprocessContext context)
    {
        return new ActionPlan(
            new[] { new ActionRequest("action.test", "hero-2", new Dictionary<string, string>()) },
            "postprocess-1");
    }
}

sealed class FakePostprocessPromptComposer : IPostprocessPromptComposer
{
    public PromptPackage Compose(InteractionEnvelope envelope, RuleSelection selection, string visibleReply, string rawReply, PostprocessContext context)
    {
        return new PromptPackage(new[] { new PromptMessage("user", "postprocess:" + visibleReply) }, 64, "model-a");
    }
}

sealed class RecordingMemory : IInteractionMemory
{
    public List<string> Roles { get; } = new List<string>();
    public List<FactRecord> Facts { get; } = new List<FactRecord>();
    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => new List<PromptMessage>().AsReadOnly();
    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
    {
        Roles.Add(message.Role);
        Facts.AddRange(confirmedFacts ?? Array.Empty<FactRecord>());
    }
}

sealed class RecordingBatchMemory : IInteractionMemory, IInteractionMemoryBatchCommitter
{
    public List<InteractionMemoryCommit> Commits { get; } = new List<InteractionMemoryCommit>();
    private readonly HashSet<string> _commitIds = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => new List<PromptMessage>().AsReadOnly();

    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
    {
        throw new InvalidOperationException("batch memory should not use the legacy append path");
    }

    public MemoryCommitResult Commit(InteractionMemoryCommit commit)
    {
        if (!_commitIds.Add(commit.CommitId))
        {
            return new MemoryCommitResult(MemoryCommitStatus.Duplicate);
        }
        Commits.Add(commit);
        return new MemoryCommitResult(MemoryCommitStatus.Applied);
    }
}

sealed class RecordingExecutor : IActionPlanExecutor
{
    private readonly InteractionStatus _status;
    public RecordingExecutor(InteractionStatus status) => _status = status;
    public int CallCount { get; private set; }
    public InteractionStatus ValidateAndExecute(ActionPlan actionPlan, GameInteractionSnapshot currentSnapshot)
    {
        CallCount++;
        return _status;
    }
}
