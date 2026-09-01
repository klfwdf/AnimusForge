using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class Program
{
    private const string DuelBehaviorType = "AnimusForge.DuelBehavior";
    private const string RuntimeNamespace = "AnimusForge.Refactor.Runtime.";
    private const string ReceiptType = RuntimeNamespace + "DuelOutcomeReceipt";
    private const string OwnerType = RuntimeNamespace + "DuelOutcomeOwner";
    private const string DispatchContextType = RuntimeNamespace + "DetachedDuelDispatchContext";
    private const string DispatchReceiptType = RuntimeNamespace + "DetachedDuelDispatchReceipt";

    private static int Main(string[] args)
    {
        ReplayOptions options;
        try
        {
            options = ReplayOptions.Parse(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL options :: " + ex.Message);
            return 2;
        }

        TestSuite suite = new();
        suite.Run("source.processLocalOwner", () => VerifySourceGuards(options.ProjectRoot));
        suite.Run("source.stakeArmGate", () => VerifyStakeArmSourceGuard(options.ProjectRoot));
        suite.Run("source.pendingArtifactBinding", () => VerifyPendingArtifactSourceGuard(options.ProjectRoot));
        suite.Run("source.exactDispatchProvenance", () => VerifyExactDispatchSourceGuard(options.ProjectRoot));

        List<VariantEvidence> evidence = new();
        foreach (string api in new[] { "1.3", "1.4" })
        {
            string implementationPath = Path.Combine(
                options.ProjectRoot,
                "bin",
                options.Configuration,
                "single_module_stage",
                "AnimusForge",
                "bin",
                "Win64_Shipping_Client",
                "versions",
                api,
                "AnimusForge.dll");
            string markerPath = Path.ChangeExtension(implementationPath, ".build.json");

            BuildMarker marker = null;
            suite.Run(api + ".stage.integrity", () =>
            {
                marker = VerifyBuildMarker(implementationPath, markerPath, api);
            });
            if (marker != null)
            {
                suite.Run(api + ".stage.freshness", () =>
                    VerifyStageFreshness(options.ProjectRoot, marker, api));
            }

            MetadataAssembly assembly = null;
            suite.Run(api + ".stage.metadataLoad", () =>
            {
                Require(File.Exists(implementationPath), "Production implementation is missing: " + implementationPath);
                assembly = new MetadataAssembly(implementationPath);
                Require(assembly.AssemblyName == "AnimusForge",
                    "Production assembly identity drifted: " + assembly.AssemblyName);
                Require(assembly.ModuleVersionId != Guid.Empty, "Production module MVID is empty.");
            });
            if (assembly == null)
            {
                continue;
            }

            using (assembly)
            {
                suite.Run(api + ".typedSeam", () => VerifyTypedSeam(assembly));
                suite.Run(api + ".legacyAbi", () => VerifyLegacyAbi(assembly));
                suite.Run(api + ".cooldownSaveAbi", () => VerifyCooldownAndSaveAbi(assembly));
                suite.Run(api + ".processLocalOwnerAbi", () => VerifyProcessLocalOwnerAbi(assembly));
                suite.Run(api + ".subjectReadback", () => VerifySubjectReadback(assembly));
                suite.Run(api + ".ownerHostRoutes", () => VerifyOwnerHostRoutes(assembly));
                suite.Run(api + ".noLoadTickReplay", () => VerifyNoLoadOrTickReplay(assembly));
                suite.Run(api + ".stakeArmGate", () => VerifyStakeArmGate(assembly));
                suite.Run(api + ".pendingArtifactBinding", () => VerifyPendingArtifactBinding(assembly));
                suite.Run(api + ".fourberieSeam", () => VerifyFourberieSeam(assembly));
                suite.Run(api + ".exactDispatchProvenance", () => VerifyExactDispatchProvenance(assembly));

                string fingerprint = null;
                suite.Run(api + ".surfaceFingerprint", () =>
                {
                    fingerprint = BuildSurfaceFingerprint(assembly);
                    Require(!string.IsNullOrEmpty(fingerprint), "Production surface fingerprint is empty.");
                });
                if (fingerprint != null)
                {
                    string hash = marker?.Sha256 ?? ComputeSha256(implementationPath);
                    evidence.Add(new VariantEvidence(api, hash, assembly.ModuleVersionId, fingerprint));
                }
            }
        }

        suite.Run("dualApi.surfaceParity", () =>
        {
            Require(evidence.Count == 2, "Both 1.3 and 1.4 production implementations must be inspected.");
            Require(evidence[0].Fingerprint == evidence[1].Fingerprint,
                "Duel production surface differs between 1.3 and 1.4.");
            Require(evidence[0].ModuleVersionId != evidence[1].ModuleVersionId,
                "1.3 and 1.4 unexpectedly share one module MVID; variant staging may be wrong.");
        });

        foreach (VariantEvidence item in evidence)
        {
            Console.WriteLine(
                "EVIDENCE api=" + item.Api
                + " sha256=" + item.Sha256
                + " mvid=" + item.ModuleVersionId.ToString("D"));
        }
        Console.WriteLine(
            (suite.Failed == 0 ? "PASS" : "FAIL")
            + " productionDuelOutcomeReplay passed=" + suite.Passed
            + " failed=" + suite.Failed
            + " variants=" + evidence.Count);
        return suite.Failed == 0 ? 0 : 1;
    }

    private static void VerifySourceGuards(string projectRoot)
    {
        string behaviorPath = Path.Combine(projectRoot, "DuelBehavior.cs");
        string hostPath = Path.Combine(projectRoot, "DuelBehavior.Outcomes.cs");
        string contractPath = Path.Combine(projectRoot, "Refactor", "Runtime", "DuelOutcomeReceipt.cs");
        string fourberiePath = Path.Combine(projectRoot, "FourberieDuelCompatibility.cs");
        foreach (string path in new[] { behaviorPath, hostPath, contractPath, fourberiePath })
        {
            Require(File.Exists(path), "Required production source is missing: " + path);
        }

        string behavior = File.ReadAllText(behaviorPath);
        string host = File.ReadAllText(hostPath);
        string contract = File.ReadAllText(contractPath);
        Require(behavior.Contains("partial class DuelBehavior", StringComparison.Ordinal),
            "DuelBehavior is not partial; the isolated outcome host cannot be compiled.");
        Require(host.Contains("partial class DuelBehavior", StringComparison.Ordinal),
            "DuelBehavior.Outcomes.cs does not declare the partial production host.");
        Require(host.Contains("_duelOutcomeOwner", StringComparison.Ordinal)
                && host.Contains("TryBeginDuelOutcome", StringComparison.Ordinal)
                && host.Contains("TryRecordDuelOutcome", StringComparison.Ordinal)
                && host.Contains("TryFinalizeDuelOutcome", StringComparison.Ordinal)
                && host.Contains("MarkDuelOutcomeUnknown", StringComparison.Ordinal)
                && host.Contains("TryReadDuelOutcome", StringComparison.Ordinal)
                && host.Contains("TryReadLatestDuelOutcome", StringComparison.Ordinal),
            "Duel outcome host helpers are incomplete.");
        Require(host.Contains("private const int DuelOutcomeSubjectIndexCapacity = 256;", StringComparison.Ordinal)
                && host.Contains("_latestDuelOutcomeIdsBySubject", StringComparison.Ordinal)
                && host.Contains("_duelOutcomeSubjectIndexOrder", StringComparison.Ordinal),
            "Bounded per-subject Duel outcome readback index drifted.");
        Require(!host.Contains("_latestDuelOutcomeId =", StringComparison.Ordinal),
            "The removed global single-slot Duel outcome readback was reintroduced.");
        string beginOutcome = ExtractMethod(host, "private static bool TryBeginDuelOutcome(");
        string recordOutcome = ExtractMethod(host, "private static bool TryRecordDuelOutcome(");
        string finalizeOutcome = ExtractMethod(host, "private static bool TryFinalizeDuelOutcome(");
        string unknownOutcome = ExtractMethod(host, "private static void MarkDuelOutcomeUnknown(");
        Require(CountOccurrences(beginOutcome, "IndexDuelOutcome(normalizedSubject, exactDuelId);") == 1,
            "Duel start does not index exactly one subject/duel identity.");
        Require(recordOutcome.Contains("_duelOutcomeOwner.RecordOutcome(", StringComparison.Ordinal),
            "TryRecordDuelOutcome no longer locks the typed result before effects.");
        Require(!finalizeOutcome.Contains(".RecordOutcome(", StringComparison.Ordinal)
                && CountOccurrences(finalizeOutcome,
                    "IndexDuelOutcome(receipt.RequestIdentity?.SubjectId, result.DuelId);") == 1
                && finalizeOutcome.IndexOf("_duelOutcomeOwner.Finalize(", StringComparison.Ordinal)
                < finalizeOutcome.IndexOf("IndexDuelOutcome(", StringComparison.Ordinal),
            "Duel finalize does not publish subject readback after the owner terminal transition.");
        Require(CountOccurrences(unknownOutcome,
                    "IndexDuelOutcome(receipt.RequestIdentity?.SubjectId, start.DuelId);") == 1
                && unknownOutcome.IndexOf("_duelOutcomeOwner.MarkUnknownAfterStart(", StringComparison.Ordinal)
                < unknownOutcome.IndexOf("IndexDuelOutcome(", StringComparison.Ordinal),
            "Unknown-after-start does not publish subject readback after the owner transition.");

        VerifyTerminalWriterSourceOrder(
            behavior,
            "private void EndDuelLocal(",
            "Instance._lastDuelResults",
            finishMarker: null);
        VerifyTerminalWriterSourceOrder(
            behavior,
            "private static void SettleWildernessDuelRuntime(",
            "Instance._lastDuelResults",
            finishMarker: null);
        VerifyTerminalWriterSourceOrder(
            behavior,
            "private void EndDuel(",
            "_lastDuelResults",
            "FinishDuel();");

        string queueOwner = ExtractMethod(host, "private static DuelOutcomeOperationStatus QueueDuelOutcomeRequest(");
        string rollover = ExtractBlockAfter(queueOwner, "if (owner.ActiveCount == 0)");
        Require(queueOwner.Contains("DuelOutcomeOperationStatus.CapacityExceeded", StringComparison.Ordinal)
                && CountOccurrences(queueOwner, "owner.Queue(") == 2
                && rollover.Contains("_duelOutcomeOwner = new DuelOutcomeOwner();", StringComparison.Ordinal)
                && rollover.Contains("ClearDuelOutcomeSubjectIndex();", StringComparison.Ordinal),
            "Duel owner retention rollover is not fail-closed on active receipts.");

        string indexOutcome = ExtractMethod(host, "private static void IndexDuelOutcome(");
        int duplicateLookup = indexOutcome.IndexOf("_latestDuelOutcomeIdsBySubject.TryGetValue", StringComparison.Ordinal);
        int duplicateReturn = indexOutcome.IndexOf("return;", duplicateLookup, StringComparison.Ordinal);
        int enqueue = indexOutcome.IndexOf("_duelOutcomeSubjectIndexOrder.Enqueue", StringComparison.Ordinal);
        Require(duplicateLookup >= 0
                && indexOutcome.IndexOf("string.Equals(existing, duelId, StringComparison.Ordinal)", duplicateLookup, StringComparison.Ordinal) > duplicateLookup
                && duplicateReturn > duplicateLookup
                && enqueue > duplicateReturn,
            "Subject readback index can enqueue the same current DuelId twice.");

        string registerEvents = ExtractMethod(behavior, "public override void RegisterEvents(");
        Require(!registerEvents.Contains("_duelOutcomeOwner", StringComparison.Ordinal)
                && !registerEvents.Contains("DuelOutcome", StringComparison.Ordinal),
            "RegisterEvents references the process-local Duel outcome owner.");

        string syncData = ExtractMethod(behavior, "public override void SyncData(");
        string loadBoundary = ExtractBlockAfter(
            syncData,
            "if (dataStore != null && dataStore.IsLoading)");
        Require(CountOccurrences(loadBoundary, "MarkDuelOutcomeUnknown(") == 2,
            "SyncData load boundary must mark both active Duel sessions unknown exactly once.");
        Require(loadBoundary.IndexOf("ClearDetachedDuelDispatchesForLoad();", StringComparison.Ordinal)
                < loadBoundary.IndexOf("MarkDuelOutcomeUnknown(", StringComparison.Ordinal)
                && loadBoundary.Contains("_activeDuelOutcomeStart = null;", StringComparison.Ordinal)
                && loadBoundary.Contains("_wildernessDuelRuntime.AbortRequested = true;", StringComparison.Ordinal)
                && loadBoundary.Contains("_wildernessDuelRuntime.DuelOutcomeStart = null;", StringComparison.Ordinal)
                && loadBoundary.Contains("_wildernessDuelRuntime = null;", StringComparison.Ordinal)
                && loadBoundary.Contains("\"save_generation_changed\"", StringComparison.Ordinal),
            "SyncData load boundary does not clear detached triggers before settling both active identities.");
        string clearDetached = ExtractMethod(host, "private void ClearDetachedDuelDispatchesForLoad(");
        foreach (string holder in new[]
        {
            "_meetingPendingDuelDispatchContext",
            "_queuedDuelDispatchContext",
            "_openingDuelDispatchContext",
            "_wildernessDuelRuntime.DuelDispatchContext"
        })
        {
            Require(clearDetached.Contains(holder, StringComparison.Ordinal),
                "Load cleanup omits detached Duel holder: " + holder);
        }
        foreach (string triggerReset in new[]
        {
            "_meetingPendingStart = false;",
            "_queuedDuelWaitingForConversationExit = false;",
            "_leaveSourceMissionRequested = false;",
            "_pendingDuelTarget = null;",
            "_arenaMissionActive = false;",
            "_arenaMissionOpeningGraceUntilUtcTicks = 0L;",
            "_wildernessDuelRuntime.AbortRequested = true;",
            "_pendingMainHeroDeath = false;",
            "_openTownMenuRequested = false;",
            "_isDuelActive = false;"
        })
        {
            Require(clearDetached.Contains(triggerReset, StringComparison.Ordinal),
                "Load cleanup leaves a Duel trigger armed: " + triggerReset);
        }
        foreach (string forbidden in new[]
        {
            "TryBeginDuelOutcome(",
            "TryFinalizeDuelOutcome(",
            "TryReadDuelOutcome(",
            "_duelOutcomeOwner",
            ".Queue(",
            ".Start(",
            ".RecordOutcome(",
            ".Finalize("
        })
        {
            Require(!syncData.Contains(forbidden, StringComparison.Ordinal),
                "SyncData load path can replay or finalize the typed Duel owner: " + forbidden);
        }

        foreach (string forbidden in new[]
        {
            "SyncData(",
            "IDataStore",
            "CampaignEvents.",
            "TickEvent",
            "OnGameLoaded",
            "OnSessionLaunched",
            "[Saveable"
        })
        {
            Require(!host.Contains(forbidden, StringComparison.Ordinal),
                "DuelBehavior.Outcomes.cs introduced load/tick/save replay wiring: " + forbidden);
            Require(!contract.Contains(forbidden, StringComparison.Ordinal),
                "Typed Duel outcome seam introduced load/tick/save replay wiring: " + forbidden);
        }
    }

    private static void VerifyStakeArmSourceGuard(string projectRoot)
    {
        string behaviorPath = Path.Combine(projectRoot, "DuelBehavior.cs");
        Require(File.Exists(behaviorPath), "Required production source is missing: " + behaviorPath);
        string method = ExtractMethod(
            File.ReadAllText(behaviorPath),
            "public static bool TryCacheDuelStakeFromText(");
        int actionGate = method.IndexOf("Regex.IsMatch(responseText ?? \"\", \"\\\\[ACTION:DUEL\\\\]\"", StringComparison.Ordinal);
        int stakeParser = method.IndexOf("new Regex(\"\\\\[ACTION:DUEL_STAKE", StringComparison.Ordinal);
        int cacheWrite = method.IndexOf("CachePendingDuelStake(", StringComparison.Ordinal);
        Require(actionGate >= 0, "TryCacheDuelStakeFromText is missing the exact [ACTION:DUEL] owner-reply gate.");
        Require(stakeParser > actionGate && cacheWrite > stakeParser,
            "Duel stake parsing/cache write can run before the [ACTION:DUEL] owner-reply gate.");
        string gateBlock = ExtractBlockAfter(method, "if (!Regex.IsMatch(");
        Require(gateBlock.Contains("return false;", StringComparison.Ordinal),
            "Missing [ACTION:DUEL] does not fail closed before arming a wager.");
    }

    private static void VerifyPendingArtifactSourceGuard(string projectRoot)
    {
        string behavior = File.ReadAllText(Path.Combine(projectRoot, "DuelBehavior.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        string host = File.ReadAllText(Path.Combine(projectRoot, "DuelBehavior.Outcomes.cs"));
        string myBehavior = File.ReadAllText(Path.Combine(projectRoot, "MyBehavior.cs"));
        string shoutBehavior = File.ReadAllText(Path.Combine(projectRoot, "ShoutBehavior.cs"));

        foreach (string classMarker in new[]
        {
            "private class DuelAfterLines",
            "private class PendingDuelStake",
            "private class PendingDuelDebtTag"
        })
        {
            string pendingType = ExtractBlockAfter(behavior, classMarker);
            Require(pendingType.Contains("public string DuelOutcomeId;", StringComparison.Ordinal),
                classMarker + " is not bound to one typed Duel outcome.");
        }

        string begin = ExtractMethod(host, "private static bool TryBeginDuelOutcome(");
        Require(begin.Contains("BuildPendingDuelArtifactFingerprint(normalizedSubject)", StringComparison.Ordinal)
                && begin.Contains("BindPendingDuelArtifacts(normalizedSubject, exactDuelId);", StringComparison.Ordinal),
            "TryBeginDuelOutcome does not fingerprint and bind pending Duel artifacts.");
        string bind = ExtractMethod(host, "private static void BindPendingDuelArtifacts(");
        Require(CountOccurrences(bind, ".DuelOutcomeId = duelId;") == 3
                && CountOccurrences(bind, "string.IsNullOrWhiteSpace(") >= 5,
            "BindPendingDuelArtifacts must bind all three unbound artifacts once without rebinding a prior Duel.");

        string discardUnbound = ExtractMethod(host, "private static void DiscardUnboundDuelArtifacts(string subjectId)");
        string discardBound = ExtractMethod(host, "private static void DiscardBoundDuelArtifacts(");
        Require(CountOccurrences(discardUnbound, "string.IsNullOrWhiteSpace(") >= 4
                && CountOccurrences(discardUnbound, ".Remove(subjectId);") == 3,
            "Unbound Duel artifact cleanup must remove all three artifact types and no bound receipt.");
        Require(CountOccurrences(discardBound, "string.Equals(") == 3
                && CountOccurrences(discardBound, ".Remove(subjectId);") == 3,
            "Bound Duel artifact cleanup must remove all three artifact types only for the exact DuelId.");
        string discardForRequest = ExtractMethod(host, "private static void DiscardDuelArtifactsForRequest(");
        Require(CountOccurrences(begin, "DiscardDuelArtifactsForRequest(dispatchContext, normalizedSubject);") == 5
                && discardForRequest.Contains("DiscardBoundDuelArtifacts(requestSubject, context.DuelId);", StringComparison.Ordinal)
                && CountOccurrences(discardForRequest, "DiscardUnboundDuelArtifacts(") == 2
                && discardForRequest.Contains("string.Equals(requestSubject, subjectId, StringComparison.Ordinal)", StringComparison.Ordinal),
            "TryBeginDuelOutcome does not clear exact-request and actual-subject artifacts on every failure path.");

        string finalize = ExtractMethod(host, "private static bool TryFinalizeDuelOutcome(");
        string unknown = ExtractMethod(host, "private static void MarkDuelOutcomeUnknown(");
        Require(finalize.IndexOf("DiscardBoundDuelArtifacts(", StringComparison.Ordinal)
                > finalize.IndexOf("_duelOutcomeOwner.Finalize(", StringComparison.Ordinal)
                && unknown.IndexOf("DiscardBoundDuelArtifacts(", StringComparison.Ordinal)
                > unknown.IndexOf("_duelOutcomeOwner.MarkUnknownAfterStart(", StringComparison.Ordinal),
            "Typed terminal paths do not clear matching bound Duel artifacts after owner settlement.");

        foreach (string signature in new[]
        {
            "private static void PrepareDuel(\n\t\tHero target,",
            "public static void GlobalDuelStarterTick(",
            "private void StartDuelInternal(\n\t\tAgent agent,"
        })
        {
            string failurePath = ExtractMethod(behavior, signature);
            Require(failurePath.Contains("DiscardUnboundDuelArtifacts(", StringComparison.Ordinal)
                    || failurePath.Contains("DiscardDuelArtifactsForRequest(", StringComparison.Ordinal),
                signature + " does not route rejected/aborted Duel artifacts through cleanup.");
        }

        string afterLines = ExtractMethod(behavior, "public static bool TryCacheDuelAfterLinesFromText(");
        int clearLines = afterLines.IndexOf("_lastDuelAfterLines?.Remove(stringId);", StringComparison.Ordinal);
        int parseLines = afterLines.IndexOf("new Regex(\"\\\\[ACTION:DUEL_LINE_WIN", StringComparison.Ordinal);
        Require(clearLines >= 0 && parseLines > clearLines,
            "A new Duel reply does not replace stale after-lines before parsing its own lines.");

        string stake = ExtractMethod(behavior, "public static bool TryCacheDuelStakeFromText(");
        int clearStake = stake.IndexOf("_pendingDuelStakes?.Remove(stringId);", StringComparison.Ordinal);
        int parseStake = stake.IndexOf("new Regex(\"\\\\[ACTION:DUEL_STAKE", StringComparison.Ordinal);
        Require(clearStake >= 0 && parseStake > clearStake
                && !stake.Contains("_lastDuelAfterLines?.Remove", StringComparison.Ordinal),
            "A new Duel reply does not replace only the stale wager before parsing its own wager.");

        AssertOutcomeBoundConsume(
            ExtractMethod(behavior, "private static bool TryConsumePendingDuelDebtTagForOutcome("),
            "_pendingDuelDebtTags.Remove(heroId);",
            "value.DuelOutcomeId");
        AssertOutcomeBoundConsume(
            ExtractMethod(behavior, "private static bool TryConsumePendingDuelStake("),
            "_pendingDuelStakes.Remove(heroId);",
            "stake.DuelOutcomeId");
        AssertOutcomeBoundConsume(
            ExtractMethod(behavior, "private bool TryConsumeDuelAfterLines("),
            "_lastDuelAfterLines.Remove(stringId);",
            "lines.DuelOutcomeId");

        AssertDebtNormalizerClearsBeforeCache(
            ExtractMethod(myBehavior, "private static string NormalizeDuelPostprocessTags("),
            "MyBehavior.NormalizeDuelPostprocessTags");
        AssertDebtNormalizerClearsBeforeCache(
            ExtractMethod(shoutBehavior, "private static string NormalizeDuelPostprocessTagsForScene("),
            "ShoutBehavior.NormalizeDuelPostprocessTagsForScene");
    }

    private static void AssertOutcomeBoundConsume(
        string method,
        string removeMarker,
        string identityMarker)
    {
        int remove = method.IndexOf(removeMarker, StringComparison.Ordinal);
        int identity = method.IndexOf(identityMarker, StringComparison.Ordinal);
        Require(remove >= 0 && identity > remove
                && method.IndexOf("duelOutcomeId", StringComparison.Ordinal) >= 0
                && method.IndexOf("StringComparison.Ordinal", identity, StringComparison.Ordinal) > identity,
            "Pending Duel artifact is not destructively consumed and matched to the expected DuelId.");
    }

    private static void AssertDebtNormalizerClearsBeforeCache(string method, string owner)
    {
        int duelGate = method.IndexOf("[ACTION:DUEL]", StringComparison.Ordinal);
        int clear = method.IndexOf("DuelBehavior.ClearPendingDuelDebtTag(", StringComparison.Ordinal);
        int cache = method.IndexOf("DuelBehavior.CachePendingDuelDebtTag(", StringComparison.Ordinal);
        Require(duelGate >= 0 && clear > duelGate && cache > clear,
            owner + " does not clear stale debt before caching this Duel reply's debt.");
    }

    private static void VerifyExactDispatchSourceGuard(string projectRoot)
    {
        string contracts = File.ReadAllText(Path.Combine(
            projectRoot, "Refactor", "Contracts", "InteractionContracts.cs"));
        string committer = File.ReadAllText(Path.Combine(
            projectRoot, "Refactor", "Runtime", "InteractionResultCommitter.cs"));
        string executor = File.ReadAllText(Path.Combine(
            projectRoot, "Refactor", "Adapters", "LegacyNativeActionPlanExecutor.cs"));
        string host = File.ReadAllText(Path.Combine(projectRoot, "DuelBehavior.Outcomes.cs"));
        string behavior = File.ReadAllText(Path.Combine(projectRoot, "DuelBehavior.cs"));
        string shout = File.ReadAllText(Path.Combine(projectRoot, "ShoutBehavior.cs"));
        string courier = File.ReadAllText(Path.Combine(projectRoot, "CourierDeliveryBehavior.cs"));
        string receipt = File.ReadAllText(Path.Combine(
            projectRoot, "Refactor", "Runtime", "DuelOutcomeReceipt.cs"));
        string snapshots = File.ReadAllText(Path.Combine(
            projectRoot, "Refactor", "Adapters", "LegacyInteractionSnapshotAdapters.cs"));
        string normalizedHost = host.Replace("\r\n", "\n", StringComparison.Ordinal);
        string normalizedBehavior = behavior.Replace("\r\n", "\n", StringComparison.Ordinal);

        Require(contracts.Contains("internal interface IRequestBoundActionPlanExecutor", StringComparison.Ordinal)
                && committer.Contains("BuildCanonicalRequestId(envelope)", StringComparison.Ordinal)
                && committer.Contains("BuildCanonicalActionPlanFingerprint(result.ActionPlan)", StringComparison.Ordinal)
                && committer.Contains("requestBound.ValidateAndExecute(", StringComparison.Ordinal),
            "Commit reservation does not hand its canonical request/action identity to the internal executor seam.");

        string executeCore = ExtractMethod(executor, "private InteractionStatus ValidateAndExecuteCore(");
        int courierPreflight = executeCore.IndexOf(
            "currentSnapshot.Identity?.Channel == InteractionChannel.Courier",
            StringComparison.Ordinal);
        int exactQueue = executeCore.IndexOf("_duelDispatchOwner.TryQueue(", StringComparison.Ordinal);
        int economyGate = executeCore.IndexOf("_economyExecutionGate(", StringComparison.Ordinal);
        int economyReplay = executeCore.IndexOf("_economyPort.Replay(", StringComparison.Ordinal);
        int requestBoundCallback = executeCore.IndexOf("_requestBoundExecute(delegatedPlan", StringComparison.Ordinal);
        Require(exactQueue >= 0
                && courierPreflight >= 0
                && courierPreflight < exactQueue
                && economyGate > exactQueue
                && economyReplay > economyGate
                && requestBoundCallback > economyReplay,
            "Courier preflight / exact Queue / Economy gate / Economy replay / gameplay callback order drifted.");
        Require(executeCore.Contains("duelActionCount != 1", StringComparison.Ordinal)
                && executeCore.Contains("CancelUnstartedDuelDispatch", StringComparison.Ordinal)
                && executeCore.Contains("TryMapDuelChannel", StringComparison.Ordinal)
                && executeCore.Contains("duel.dispatch_queued", StringComparison.Ordinal),
            "Exact executor does not fail closed on multiple Duel actions or expose queued/started receipts.");

        string resolveExact = ExtractMethod(executor, "private InteractionStatus ResolveExactDuelDispatchStatus(");
        Require(resolveExact.Contains("duel.dispatch_started", StringComparison.Ordinal)
                && executeCore.Contains(
                    "_duelCompanionEffectUncertain = delegatedPlan.Actions.Any(IsDuelCompanionAction);",
                    StringComparison.Ordinal)
                && ExtractMethod(executor, "private static bool IsDuelProtocolAction(")
                    .Contains("ACTION:MOOD", StringComparison.Ordinal)
                && resolveExact.Contains("context?.SideEffectBoundaryCrossed == true", StringComparison.Ordinal)
                && resolveExact.Contains("_duelCompanionEffectUncertain", StringComparison.Ordinal)
                && CountOccurrences(resolveExact, "ActionExecutionEffectState.UnknownAfterStart") >= 4,
            "Duel+Mood or a crossed host side-effect boundary is no longer conservatively terminal Unknown.");

        Require(CountOccurrences(shout, "CreateRequestBoundDuelExecutor(") == 2
                && CountOccurrences(shout, "PrepareDuelForDetachedRequest(") == 3
                && shout.Contains("duelDispatchContext: duelDispatchContext", StringComparison.Ordinal),
            "Native/SceneShout do not pass the explicit request context to their actual Duel branch.");
        Require(courier.Contains("CreateRequestBoundDuelExecutor(", StringComparison.Ordinal)
                && courier.Contains("RejectDetachedDuelDispatchForExternal(", StringComparison.Ordinal)
                && courier.Contains("\"unsupported_channel\"", StringComparison.Ordinal),
            "Courier outbound Duel is not an explicit unsupported-channel rejection.");

        string nativeFactory = ExtractMethod(
            shout,
            "public static LegacyNativeActionPlanExecutor CreateNativeConversationActionPlanExecutorForExternal(");
        string sceneFactory = ExtractMethod(
            shout,
            "public static LegacyNativeActionPlanExecutor CreateSceneShoutActionPlanExecutorForExternal(");
        foreach ((string Factory, string ContextName, string Channel) provenance in new[]
        {
            (nativeFactory, "isCurrentNativeContext", "InteractionChannel.NativeConversation"),
            (sceneFactory, "isCurrentSceneContext", "InteractionChannel.SceneShout")
        })
        {
            int subjectCompare = provenance.Factory.IndexOf(
                "snapshot.Identity.SubjectId",
                StringComparison.Ordinal);
            int gateWiring = provenance.Factory.IndexOf("economyExecutionGate:", StringComparison.Ordinal);
            Require(provenance.Factory.Contains(provenance.Channel, StringComparison.Ordinal)
                    && subjectCompare >= 0
                    && provenance.Factory.IndexOf("StringComparison.Ordinal", subjectCompare, StringComparison.Ordinal) > subjectCompare
                    && gateWiring > subjectCompare
                    && provenance.Factory.IndexOf(provenance.ContextName + "(snapshot)", gateWiring, StringComparison.Ordinal) > gateWiring,
                provenance.Channel + " does not independently validate subject/session provenance before Economy replay.");
        }

        string courierGate = ExtractMethod(
            courier,
            "private InteractionStatus GateCourierEconomyActionPlanForExternal(");
        Require(courierGate.IndexOf("IsCourierActionSessionEligible(", StringComparison.Ordinal)
                < courierGate.IndexOf("TryReserveCourierEconomyOnly(", StringComparison.Ordinal)
                && ExtractMethod(courier, "private static bool IsCourierActionSessionEligible(")
                    .Contains("snapshot.Identity.SubjectId", StringComparison.Ordinal),
            "Courier Economy preflight no longer validates session/recipient before reserving replay.");

        foreach (string signature in new[]
        {
            "internal static void PrepareDuelForDetachedRequest(\n\t\tHero",
            "internal static void PrepareDuelForDetachedRequest(\n\t\tAgent",
            "internal static void PrepareDuelForDetachedRequest(\n\t\tCharacterObject"
        })
        {
            string prepare = ExtractMethod(normalizedHost, signature);
            int validate = prepare.IndexOf("ValidateDetachedDuelTarget(", StringComparison.Ordinal);
            Require(validate >= 0
                    && prepare.IndexOf("BindPendingDuelArtifacts(", StringComparison.Ordinal) > validate
                    && prepare.LastIndexOf("PrepareDuel(", StringComparison.Ordinal) > validate,
                "Detached prepare does not compare the actual target subject before artifact binding/dispatch: " + signature);
        }
        string validateTarget = ExtractMethod(host, "private static bool ValidateDetachedDuelTarget(");
        Require(validateTarget.Contains("context.RequestIdentity.SubjectId", StringComparison.Ordinal)
                && validateTarget.Contains("NormalizeDuelOutcomeSubject(subjectId)", StringComparison.Ordinal)
                && validateTarget.Contains("StringComparison.Ordinal", StringComparison.Ordinal),
            "Actual Duel subject is not independently compared with the request subject.");

        foreach (string holder in new[]
        {
            "_meetingPendingDuelDispatchContext",
            "_queuedDuelDispatchContext",
            "_openingDuelDispatchContext",
            "WildernessDuelBattleRuntime",
            "ArenaDuelMissionBehavior"
        })
        {
            Require(behavior.Contains(holder, StringComparison.Ordinal),
                "Delayed Duel holder is missing exact context: " + holder);
        }
        Require(host.Contains("TryReadDuelOutcomeByRequestId", StringComparison.Ordinal)
                && host.Contains("IndexDuelOutcomeRequest", StringComparison.Ordinal)
                && host.Contains("ClearDetachedDuelDispatchesForLoad", StringComparison.Ordinal),
            "Exact request readback or load cleanup is missing.");
        string markDispatchUnknown = ExtractMethod(
            receipt,
            "internal DuelOutcomeOperationStatus MarkUnknownAfterDispatch(");
        Require(markDispatchUnknown.Contains("existing.State != DuelOutcomeState.Queued", StringComparison.Ordinal)
                && markDispatchUnknown.Contains("DuelOutcomeState.UnknownAfterStart", StringComparison.Ordinal)
                && markDispatchUnknown.Contains("AnimusForge.DuelOutcome.UnknownAfterDispatch.v1", StringComparison.Ordinal)
                && ExtractMethod(host, "public void MarkUnknownAfterStart(")
                    .Contains("_duelOutcomeOwner.MarkUnknownAfterDispatch(", StringComparison.Ordinal),
            "A crossed pre-Start dispatch cannot be terminalized as a provenance-preserving Unknown.");

        string arenaType = ExtractBlockAfter(normalizedBehavior, "private class ArenaDuelMissionBehavior");
        string arenaAfterStart = ExtractMethod(arenaType, "public override void AfterStart(");
        Require(arenaAfterStart.IndexOf("EnsureDuelOutcomeStarted(", StringComparison.Ordinal)
                < arenaAfterStart.IndexOf("base.Mission.SetMissionMode(", StringComparison.Ordinal)
                && arenaAfterStart.IndexOf("EnsureDuelOutcomeStarted(", StringComparison.Ordinal)
                < arenaAfterStart.IndexOf("SetupArenaDuel();", StringComparison.Ordinal),
            "Arena owner Start is not before Mission/gameplay setup.");
        string wildernessType = ExtractBlockAfter(normalizedBehavior, "private sealed class WildernessDuelBattleMissionLogic");
        string wildernessInitialize = ExtractMethod(wildernessType, "public override void OnBehaviorInitialize(");
        Require(wildernessInitialize.IndexOf("EnsureDuelOutcomeStarted(", StringComparison.Ordinal)
                < wildernessInitialize.IndexOf("EnsureMainHeroHealthForWildernessDuel(", StringComparison.Ordinal),
            "Wilderness owner Start is not before gameplay mutation.");
        string inPlaceStart = ExtractMethod(
            normalizedBehavior,
            "private void StartDuelInternal(\n\t\tAgent agent,");
        Require(inPlaceStart.IndexOf("TryBeginDuelOutcome(", StringComparison.Ordinal)
                < inPlaceStart.IndexOf("_preDuelMode = current.Mode;", StringComparison.Ordinal)
                && inPlaceStart.IndexOf("TryBeginDuelOutcome(", StringComparison.Ordinal)
                < inPlaceStart.IndexOf("current.SetMissionMode(", StringComparison.Ordinal),
            "In-place owner Start is not before gameplay mutation.");

        Require(arenaType.Contains("private readonly DetachedDuelDispatchContext _duelDispatchContext;", StringComparison.Ordinal)
                && arenaType.Contains("private readonly string _nonHeroMemoryId;", StringComparison.Ordinal)
                && ExtractBlockAfter(snapshots, "public sealed class MyBehaviorMemoryFacade")
                    .Contains("private readonly string _nonHeroMemoryId;", StringComparison.Ordinal),
            "Non-hero/exact delayed holders are no longer immutable at the action boundary.");

        string openingTick = ExtractMethod(behavior, "public static void GlobalArenaLeaveTick(");
        string arenaTick = ExtractMethod(arenaType, "public override void OnMissionTick(");
        Require(openingTick.Contains("opening_timeout_before_afterstart", StringComparison.Ordinal)
                && openingTick.Contains("MarkDetachedDuelDispatchUnknownAfterStart(", StringComparison.Ordinal)
                && arenaTick.Contains("_setupAttempts >= 3", StringComparison.Ordinal)
                && arenaTick.Contains("_setupDeadline", StringComparison.Ordinal)
                && arenaTick.Contains("arena_setup_timeout", StringComparison.Ordinal)
                && arenaTick.Contains("MarkDetachedDuelDispatchUnknownAfterStart(", StringComparison.Ordinal),
            "Opening/setup retry bounds no longer terminalize an exact dispatch as Unknown.");

        string delayedReady = ExtractMethod(host, "private static bool IsDetachedDuelDispatchReadyForDelayedHost(");
        Require(delayedReady.Contains("receipt?.State == DetachedDuelDispatchState.Queued", StringComparison.Ordinal)
                && delayedReady.Contains("receipt.HostAccepted", StringComparison.Ordinal),
            "Delayed Duel consumers no longer require both Queued and HostAccepted.");
        foreach ((string Signature, string SideEffect) consumer in new[]
        {
            ("public static void GlobalDuelStarterTick(", "TryOpenWildernessDuelMission("),
            ("public void OnEngineTick(", "StartDuelInternal("),
            ("public static void GlobalSourceMissionLeaveTick(", "current.EndMission();")
        })
        {
            string method = ExtractMethod(behavior, consumer.Signature);
            int ready = method.IndexOf("IsDetachedDuelDispatchReadyForDelayedHost(", StringComparison.Ordinal);
            Require(ready >= 0
                    && method.IndexOf(consumer.SideEffect, ready, StringComparison.Ordinal) > ready,
                consumer.Signature + " can consume a non-accepted/non-queued exact dispatch.");
        }

        string heroPrepare = ExtractMethod(
            normalizedBehavior,
            "private static void PrepareDuel(\n\t\tHero target,");
        string nonHeroPrepare = ExtractMethod(
            normalizedBehavior,
            "private static void PrepareDuel(\n\t\tCharacterObject targetCharacter,");
        Require(heroPrepare.Contains("arena_vlandia_a", StringComparison.Ordinal)
                && heroPrepare.Contains("Instance.StartDuelViaAI(target, duelDispatchContext);", StringComparison.Ordinal)
                && nonHeroPrepare.Contains("arena_vlandia_a", StringComparison.Ordinal)
                && nonHeroPrepare.Contains("arena_target_agent_missing", StringComparison.Ordinal)
                && nonHeroPrepare.Contains("Instance.StartDuelViaAI(arenaTarget, duelDispatchContext);", StringComparison.Ordinal),
            "A Hero/non-hero already in the arena is no longer routed directly to actual Start.");

        string rejectExact = ExtractMethod(host, "public void Reject(");
        Require(rejectExact.IndexOf("_exactDuelDispatchIdsSeen.Add(context.DuelId);", StringComparison.Ordinal)
                < rejectExact.IndexOf("RejectDuelOutcomeRequest(", StringComparison.Ordinal)
                && rejectExact.Contains("ExactDuelDispatchSeenCapacity", StringComparison.Ordinal),
            "Exact Reject no longer records a bounded tombstone before owner rejection.");

        string wildernessTick = ExtractMethod(wildernessType, "public override void OnMissionTick(");
        Require(wildernessTick.Contains("_participantDeadline = base.Mission.CurrentTime + 30f;", StringComparison.Ordinal)
                && wildernessTick.Contains("wilderness_participant_timeout", StringComparison.Ordinal)
                && wildernessTick.Contains("_runtime.AbortRequested = true;", StringComparison.Ordinal)
                && wildernessTick.Contains("base.Mission.EndMission();", StringComparison.Ordinal),
            "Wilderness participant acquisition is no longer bounded to a terminal 30-second abort.");

        string arenaSettlement = ExtractMethod(arenaType, "private void EndDuelLocal(");
        string wildernessSettlement = ExtractMethod(behavior, "private static void SettleWildernessDuelRuntime(");
        Require(arenaSettlement.Contains("if (_abortRequested || _duelOutcomeStart == null)", StringComparison.Ordinal)
                && wildernessSettlement.Contains(
                    "if (runtime == null || runtime.SettlementDone || runtime.AbortRequested)",
                    StringComparison.Ordinal),
            "Arena/Wilderness abort state can still reach settlement.");
        Require(!executor.Contains("TryReadLatestDuelOutcome", StringComparison.Ordinal)
                && !executor.Contains("_lastDuelResults", StringComparison.Ordinal)
                && !executor.Contains("_duelCooldowns", StringComparison.Ordinal),
            "Exact executor infers provenance from a subject aggregate, legacy result, or cooldown.");
    }

    private static void VerifyTerminalWriterSourceOrder(
        string behaviorSource,
        string signature,
        string firstLegacyEffectMarker,
        string finishMarker)
    {
        string writer = ExtractMethod(behaviorSource, signature);
        int record = writer.IndexOf("TryRecordDuelOutcome(", StringComparison.Ordinal);
        int legacyEffect = writer.IndexOf(firstLegacyEffectMarker, StringComparison.Ordinal);
        int effectReceipt = writer.IndexOf("TryCreateDuelOutcomeEffects(", StringComparison.Ordinal);
        int finalize = writer.IndexOf("TryFinalizeDuelOutcome(", StringComparison.Ordinal);
        string recordFailure = ExtractBlockAfter(writer, "if (!TryRecordDuelOutcome(");
        Require(recordFailure.Contains("return;", StringComparison.Ordinal)
                && !recordFailure.Contains(firstLegacyEffectMarker, StringComparison.Ordinal)
                && !recordFailure.Contains("TryFinalizeDuelOutcome(", StringComparison.Ordinal),
            signature + " can mutate settlement/finalize after the typed result transition fails.");
        Require(record >= 0 && legacyEffect > record,
            signature + " does not lock the typed result before legacy effects.");
        Require(effectReceipt > legacyEffect && finalize > effectReceipt,
            signature + " does not finalize after effect-state materialization.");
        Require(writer.Contains("\"effects_unavailable\"", StringComparison.Ordinal)
                && writer.Contains("\"settlement_exception\"", StringComparison.Ordinal)
                && CountOccurrences(writer, "MarkDuelOutcomeUnknown(") >= 2,
            signature + " can leave an OutcomeKnown receipt active when effects/final settlement fails.");
        if (!string.IsNullOrEmpty(finishMarker))
        {
            // Early record/start failure branches may tear down before any
            // OutcomeKnown receipt exists. Lock the settled success path by
            // requiring the final teardown after the typed finalization seam.
            int finish = writer.LastIndexOf(finishMarker, StringComparison.Ordinal);
            Require(finish > finalize,
                signature + " tears down the Duel before typed finalization.");
        }
    }

    private static BuildMarker VerifyBuildMarker(string implementationPath, string markerPath, string api)
    {
        Require(File.Exists(implementationPath), "Production implementation is missing: " + implementationPath);
        Require(File.Exists(markerPath), "Production build marker is missing: " + markerPath);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(markerPath));
        JsonElement root = document.RootElement;
        BuildMarker marker = new(
            root.GetProperty("SchemaVersion").GetInt32(),
            root.GetProperty("Role").GetString(),
            root.GetProperty("FileName").GetString(),
            root.GetProperty("AssemblyName").GetString(),
            root.GetProperty("BannerlordApi").GetString(),
            root.GetProperty("BuildFlavor").GetString(),
            root.GetProperty("ReferenceGameVersion").GetString(),
            root.GetProperty("Sha256").GetString()?.ToUpperInvariant(),
            root.GetProperty("CreatedUtc").GetDateTimeOffset());

        Require(marker.SchemaVersion == 2, "Build marker schema drifted.");
        Require(marker.Role == "Implementation", "Build marker role is not Implementation.");
        Require(marker.FileName == "AnimusForge.dll", "Build marker file name drifted.");
        Require(marker.AssemblyName == "AnimusForge", "Build marker assembly identity drifted.");
        Require(marker.BannerlordApi == api, "Build marker API mismatch: " + marker.BannerlordApi);
        Require(marker.BuildFlavor == "ANIMUSFORGE_BANNERLORD_API_" + api.Replace('.', '_'),
            "Build marker flavor mismatch: " + marker.BuildFlavor);
        Require(marker.ReferenceGameVersion.StartsWith("v" + api + ".", StringComparison.Ordinal),
            "Build marker reference line mismatch: " + marker.ReferenceGameVersion);
        Require(marker.Sha256 == ComputeSha256(implementationPath),
            "Build marker SHA-256 does not match the staged implementation.");
        return marker;
    }

    private static void VerifyStageFreshness(string projectRoot, BuildMarker marker, string api)
    {
        string[] relevantSources =
        {
            Path.Combine(projectRoot, "AnimusForge.csproj"),
            Path.Combine(projectRoot, "DuelBehavior.cs"),
            Path.Combine(projectRoot, "DuelBehavior.Outcomes.cs"),
            Path.Combine(projectRoot, "FourberieDuelCompatibility.cs"),
            Path.Combine(projectRoot, "MyBehavior.cs"),
            Path.Combine(projectRoot, "ShoutBehavior.cs"),
            Path.Combine(projectRoot, "CourierDeliveryBehavior.cs"),
            Path.Combine(projectRoot, "Refactor", "Contracts", "InteractionContracts.cs"),
            Path.Combine(projectRoot, "Refactor", "Runtime", "DuelOutcomeReceipt.cs"),
            Path.Combine(projectRoot, "Refactor", "Runtime", "InteractionResultCommitter.cs"),
            Path.Combine(projectRoot, "Refactor", "Adapters", "LegacyNativeActionPlanExecutor.cs")
        };
        DateTimeOffset markerTime = marker.CreatedUtc.ToUniversalTime();
        foreach (string source in relevantSources)
        {
            Require(File.Exists(source), "Freshness source is missing: " + source);
            DateTimeOffset sourceTime = File.GetLastWriteTimeUtc(source);
            Require(sourceTime <= markerTime.AddSeconds(2),
                "Staged " + api + " implementation is stale; source is newer than its build marker: " + source);
        }
    }

    private static void VerifyTypedSeam(MetadataAssembly assembly)
    {
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> enums =
            new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal)
            {
                [RuntimeNamespace + "DuelOutcomeState"] = Values(
                    ("Rejected", 0), ("Queued", 1), ("Started", 2), ("OutcomeKnown", 3),
                    ("Completed", 4), ("PartiallyCompleted", 5), ("UnknownAfterStart", 6), ("Cancelled", 7)),
                [RuntimeNamespace + "DuelOutcomeEffectState"] = Values(
                    ("NotApplicable", 0), ("Confirmed", 1), ("Partial", 2),
                    ("AttemptedUnconfirmed", 3), ("Unknown", 4)),
                [RuntimeNamespace + "DuelOutcomeChannel"] = Values(
                    ("SceneShout", 0), ("NativeConversation", 1), ("Courier", 2),
                    ("ProactiveNpc", 3), ("Domain", 4)),
                [RuntimeNamespace + "DuelSessionKind"] = Values(
                    ("Meeting", 0), ("Arena", 1), ("Wilderness", 2)),
                [RuntimeNamespace + "DuelResultKind"] = Values(
                    ("PlayerWon", 0), ("OpponentWon", 1), ("Draw", 2)),
                [RuntimeNamespace + "DuelOutcomeOperationStatus"] = Values(
                    ("Accepted", 0), ("Duplicate", 1), ("NotFound", 2),
                    ("InvalidTransition", 3), ("IdentityConflict", 4), ("CapacityExceeded", 5),
                    ("InvalidIdentity", 6)),
                [RuntimeNamespace + "DetachedDuelDispatchState"] = Values(
                    ("Rejected", 0), ("Queued", 1), ("Started", 2), ("UnknownAfterStart", 3))
            };

        foreach ((string typeName, IReadOnlyDictionary<string, int> expected) in enums)
        {
            MetadataAssembly.TypeView type = assembly.RequireType(typeName);
            Require(type.BaseType == "System.Enum", typeName + " is not an enum.");
            Require((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic,
                typeName + " must remain internal.");
            AssertDictionaryEqual(expected, assembly.GetEnumValues(typeName), typeName + " values drifted");
        }

        string requestType = RuntimeNamespace + "DuelOutcomeRequestIdentity";
        string startType = RuntimeNamespace + "DuelOutcomeStartIdentity";
        string resultType = RuntimeNamespace + "DuelOutcomeResultIdentity";
        string effectsType = RuntimeNamespace + "DuelOutcomeEffects";
        foreach (string typeName in new[]
        {
            requestType,
            startType,
            resultType,
            effectsType,
            ReceiptType,
            OwnerType,
            DispatchContextType,
            DispatchReceiptType,
            RuntimeNamespace + "IDetachedDuelDispatchOwner",
            RuntimeNamespace + "IDetachedDuelDispatchExecutionReceipt"
        })
        {
            MetadataAssembly.TypeView type = assembly.RequireType(typeName);
            Require((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic,
                typeName + " must remain internal.");
        }

        AssertStaticMethod(
            assembly.RequireMethod(requestType, "TryCreate",
                "System.String", "System.String", "System.String", RuntimeNamespace + "DuelOutcomeChannel",
                "System.String", "System.String", "System.Int64", "System.Int64", "System.String",
                requestType + "&", "System.String&"),
            "System.Boolean");
        AssertStaticMethod(
            assembly.RequireMethod(startType, "TryCreate",
                requestType, "System.String", RuntimeNamespace + "DuelSessionKind", startType + "&", "System.String&"),
            "System.Boolean");
        AssertStaticMethod(
            assembly.RequireMethod(resultType, "TryCreate",
                startType, "System.String", RuntimeNamespace + "DuelResultKind", resultType + "&", "System.String&"),
            "System.Boolean");
        AssertStaticMethod(
            assembly.RequireMethod(effectsType, "TryCreate",
                RuntimeNamespace + "DuelOutcomeEffectState", RuntimeNamespace + "DuelOutcomeEffectState",
                RuntimeNamespace + "DuelOutcomeEffectState", RuntimeNamespace + "DuelOutcomeEffectState",
                RuntimeNamespace + "DuelOutcomeEffectState", effectsType + "&", "System.String&"),
            "System.Boolean");
        AssertStaticMethod(
            assembly.RequireMethod(
                DispatchContextType,
                "TryCreate",
                "System.String",
                "System.String",
                RuntimeNamespace + "DuelOutcomeChannel",
                "System.String",
                "System.String",
                "System.Int64",
                "System.Int64",
                "System.String",
                DispatchContextType + "&",
                "System.String&"),
            "System.Boolean");
        MetadataAssembly.MethodView dispatchReceiptConstructor = assembly.RequireMethod(
            DispatchReceiptType,
            ".ctor",
            RuntimeNamespace + "DetachedDuelDispatchState",
            "System.String",
            "System.String",
            "System.String",
            "System.Boolean",
            "System.String");
        Require(!dispatchReceiptConstructor.IsStatic && dispatchReceiptConstructor.ReturnType == "System.Void",
            "Detached dispatch receipt constructor ABI drifted: " + dispatchReceiptConstructor.DisplaySignature);

        string dispatchOwnerType = RuntimeNamespace + "IDetachedDuelDispatchOwner";
        AssertInstanceMethod(
            assembly.RequireMethod(
                dispatchOwnerType,
                "TryQueue",
                DispatchContextType,
                "System.Boolean&",
                "System.String&"),
            "System.Boolean");
        foreach (string ownerMethod in new[] { "Reject", "Cancel", "MarkUnknownAfterStart" })
        {
            AssertInstanceMethod(
                assembly.RequireMethod(dispatchOwnerType, ownerMethod, DispatchContextType, "System.String"),
                "System.Void");
        }

        MetadataAssembly.MethodView constructor = assembly.RequireMethod(OwnerType, ".ctor", "System.Int32", "System.Int32");
        Require(!constructor.IsStatic, "DuelOutcomeOwner constructor became static.");
        MetadataAssembly.ParameterView activeCapacity = assembly.GetParameter(constructor, 1);
        MetadataAssembly.ParameterView totalCapacity = assembly.GetParameter(constructor, 2);
        Require(activeCapacity.HasDefaultValue && Convert.ToInt32(activeCapacity.DefaultValue) == 64,
            "DuelOutcomeOwner active-capacity default drifted.");
        Require(totalCapacity.HasDefaultValue && Convert.ToInt32(totalCapacity.DefaultValue) == 512,
            "DuelOutcomeOwner total-capacity default drifted.");

        string operationStatus = RuntimeNamespace + "DuelOutcomeOperationStatus";
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "Queue", requestType, ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "Reject", requestType, "System.String", ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "Start", startType, ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "RecordOutcome", resultType, ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "Finalize", resultType, effectsType, ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "Cancel", requestType, "System.String", ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "MarkUnknownAfterStart", startType, "System.String", ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "MarkUnknownAfterDispatch", requestType, "System.String", ReceiptType + "&", "System.String&"), operationStatus);
        AssertInstanceMethod(assembly.RequireMethod(OwnerType, "TryGet", "System.String", ReceiptType + "&"), "System.Boolean");
    }

    private static void VerifyLegacyAbi(MetadataAssembly assembly)
    {
        MetadataAssembly.MethodView[] prepare = assembly.FindMethods(DuelBehaviorType, "PrepareDuel")
            .Where(method => method.IsPublic)
            .ToArray();
        Require(prepare.Length == 3, "PrepareDuel public overload count drifted: " + prepare.Length);
        foreach (string targetType in new[]
        {
            "TaleWorlds.CampaignSystem.Hero",
            "TaleWorlds.MountAndBlade.Agent",
            "TaleWorlds.CampaignSystem.CharacterObject"
        })
        {
            MetadataAssembly.MethodView method = assembly.RequireMethod(
                DuelBehaviorType, "PrepareDuel", targetType, "System.Single");
            Require(method.IsPublic && method.IsStatic && method.ReturnType == "System.Void",
                "PrepareDuel ABI drifted: " + method.DisplaySignature);
        }

        MetadataAssembly.MethodView[] start = assembly.FindMethods(DuelBehaviorType, "StartDuelViaAI")
            .Where(method => method.IsPublic)
            .ToArray();
        Require(start.Length == 2, "StartDuelViaAI public overload count drifted: " + start.Length);
        foreach (string targetType in new[]
        {
            "TaleWorlds.CampaignSystem.Hero",
            "TaleWorlds.MountAndBlade.Agent"
        })
        {
            MetadataAssembly.MethodView method = assembly.RequireMethod(DuelBehaviorType, "StartDuelViaAI", targetType);
            Require(method.IsPublic && !method.IsStatic && method.ReturnType == "System.Void",
                "StartDuelViaAI ABI drifted: " + method.DisplaySignature);
        }

        MetadataAssembly.MethodView consume = assembly.RequireMethod(
            DuelBehaviorType,
            "TryConsumeLastDuelResult",
            "TaleWorlds.CampaignSystem.Hero",
            "System.Boolean&");
        Require(consume.IsPublic && consume.IsStatic && consume.ReturnType == "System.Boolean",
            "TryConsumeLastDuelResult ABI drifted: " + consume.DisplaySignature);
        Require(assembly.GetParameter(consume, 2).IsOut,
            "TryConsumeLastDuelResult playerWon parameter is no longer out.");
    }

    private static void VerifyCooldownAndSaveAbi(MetadataAssembly assembly)
    {
        MetadataAssembly.TypeView behavior = assembly.RequireType(DuelBehaviorType);
        MetadataAssembly.FieldView cooldowns = RequireField(behavior, "_duelCooldowns");
        Require(cooldowns.FieldType == "System.Collections.Generic.Dictionary`2<System.String,System.Single>",
            "_duelCooldowns key/value type drifted: " + cooldowns.FieldType);
        Require(cooldowns.IsPrivate && !cooldowns.IsStatic,
            "_duelCooldowns field ownership drifted.");

        MetadataAssembly.MethodView syncData = assembly.RequireMethod(
            DuelBehaviorType, "SyncData", "TaleWorlds.CampaignSystem.IDataStore");
        IReadOnlyList<string> strings = assembly.GetLoadedStrings(syncData);
        Require(strings.Count(value => value == "_duelCooldowns") == 1,
            "SyncData no longer uses the exact _duelCooldowns save key once.");
        Require(!strings.Any(value => value.Contains("DuelOutcome", StringComparison.OrdinalIgnoreCase)),
            "SyncData introduced a typed Duel owner save key.");
        IReadOnlyList<MetadataAssembly.FieldReferenceView> syncFields = assembly.GetReferencedFields(syncData);
        Require(syncFields.Any(field => field.DeclaringType == DuelBehaviorType && field.Name == "_duelCooldowns"),
            "SyncData no longer references _duelCooldowns.");
        Require(!syncFields.Any(field => field.Name == "_duelOutcomeOwner"
                                         || field.FieldType == OwnerType
                                         || field.FieldType == ReceiptType),
            "SyncData persists or directly references the process-local typed Duel owner/receipt.");

        string saveDefinerType = DuelBehaviorType + "+DuelBehaviorSaveableTypeDefiner";
        MetadataAssembly.TypeView saveDefiner = assembly.RequireType(saveDefinerType);
        Require(saveDefiner.BaseType == "TaleWorlds.SaveSystem.SaveableTypeDefiner",
            "Duel save definer base type drifted: " + saveDefiner.BaseType);
        MetadataAssembly.MethodView constructor = assembly.RequireMethod(saveDefinerType, ".ctor");
        IReadOnlyList<int> baseIds = assembly.GetLoadedInt32Constants(constructor);
        Require(baseIds.SequenceEqual(new[] { 711070 }),
            "Duel SaveableTypeDefiner base ID drifted: " + string.Join(",", baseIds));

        MetadataAssembly.MethodView defineClasses = assembly.RequireMethod(saveDefinerType, "DefineClassTypes");
        IReadOnlyList<int> classIds = assembly.GetLoadedInt32Constants(defineClasses);
        Require(classIds.SequenceEqual(new[] { 1 }),
            "Duel saveable class IDs drifted: " + string.Join(",", classIds));
        Require(assembly.GetDirectCalls(defineClasses).Any(call =>
                call.DeclaringType == "TaleWorlds.SaveSystem.SaveableTypeDefiner"
                && call.Name == "AddClassDefinition"),
            "Duel save definer no longer registers its class through AddClassDefinition.");
    }

    private static void VerifyProcessLocalOwnerAbi(MetadataAssembly assembly)
    {
        MetadataAssembly.TypeView behavior = assembly.RequireType(DuelBehaviorType);
        MetadataAssembly.FieldView owner = RequireField(behavior, "_duelOutcomeOwner");
        Require(owner.FieldType == OwnerType && owner.IsPrivate && owner.IsStatic && !owner.IsInitOnly,
            "_duelOutcomeOwner must remain private/static, process-local, and replaceable only by safe rollover.");
        MetadataAssembly.FieldView ownerSync = RequireField(behavior, "_duelOutcomeOwnerSync");
        Require(ownerSync.FieldType == "System.Object"
                && ownerSync.IsPrivate && ownerSync.IsStatic && ownerSync.IsInitOnly,
            "Duel owner rollover lock drifted.");

        MetadataAssembly.MethodView queue = assembly.RequireMethod(
            DuelBehaviorType,
            "QueueDuelOutcomeRequest",
            RuntimeNamespace + "DuelOutcomeRequestIdentity",
            ReceiptType + "&",
            "System.String&");
        Require(queue.IsStatic
                && queue.ReturnType == RuntimeNamespace + "DuelOutcomeOperationStatus",
            "QueueDuelOutcomeRequest ABI drifted: " + queue.DisplaySignature);
        IReadOnlyList<MetadataAssembly.MethodCallView> calls = assembly.GetDirectCalls(queue);
        Require(calls.Count(call => call.DeclaringType == OwnerType && call.Name == "Queue") == 2,
            "Safe Duel owner rollover must attempt Queue once before and once after the guarded rollover.");
        Require(calls.Any(call => call.DeclaringType == OwnerType && call.Name == "get_ActiveCount")
                && calls.Any(call => call.DeclaringType == OwnerType && call.Name == ".ctor")
                && calls.Any(call => call.DeclaringType == DuelBehaviorType
                                     && call.Name == "ClearDuelOutcomeSubjectIndex"),
            "Duel owner rollover is missing the zero-active/new-owner/index-clear boundary.");
        Require(assembly.GetReferencedFields(queue).Any(field => field.Name == "_duelOutcomeOwnerSync"),
            "Duel owner rollover no longer uses its dedicated lock.");
    }

    private static void VerifySubjectReadback(MetadataAssembly assembly)
    {
        MetadataAssembly.TypeView behavior = assembly.RequireType(DuelBehaviorType);
        MetadataAssembly.FieldView latest = RequireField(behavior, "_latestDuelOutcomeIdsBySubject");
        MetadataAssembly.FieldView order = RequireField(behavior, "_duelOutcomeSubjectIndexOrder");
        MetadataAssembly.FieldView sync = RequireField(behavior, "_duelOutcomeSubjectIndexSync");
        Require(latest.FieldType == "System.Collections.Generic.Dictionary`2<System.String,System.String>"
                && latest.IsPrivate && latest.IsStatic && latest.IsInitOnly,
            "Per-subject Duel outcome lookup field drifted: " + latest.FieldType);
        Require(order.FieldType
                == "System.Collections.Generic.Queue`1<System.Collections.Generic.KeyValuePair`2<System.String,System.String>>"
                && order.IsPrivate && order.IsStatic && order.IsInitOnly,
            "Per-subject Duel outcome order field drifted: " + order.FieldType);
        Require(sync.FieldType == "System.Object" && sync.IsPrivate && sync.IsStatic && sync.IsInitOnly,
            "Per-subject Duel outcome index lock drifted.");

        MetadataAssembly.MethodView readLatest = assembly.RequireMethod(
            DuelBehaviorType, "TryReadLatestDuelOutcome", "System.String", ReceiptType + "&");
        AssertStaticMethod(readLatest, "System.Boolean");
        RequireCall(assembly, readLatest, DuelBehaviorType, "TryReadDuelOutcome");
        Require(assembly.GetDirectCalls(readLatest).Any(call =>
                call.DeclaringType == "System.Collections.Generic.Dictionary`2<System.String,System.String>"
                && call.Name == "TryGetValue"),
            "Latest Duel outcome readback no longer resolves by subject.");

        MetadataAssembly.MethodView index = assembly.RequireMethod(
            DuelBehaviorType, "IndexDuelOutcome", "System.String", "System.String");
        IReadOnlyList<int> constants = assembly.GetLoadedInt32Constants(index);
        Require(constants.Contains(256) && constants.Contains(512),
            "Subject readback bounds drifted; expected subject=256 and order=512. IL constants="
            + string.Join(",", constants));
        IReadOnlyList<MetadataAssembly.MethodCallView> indexCalls = assembly.GetDirectCalls(index);
        int duplicateLookup = FindCallIndex(indexCalls, call =>
            call.DeclaringType == "System.Collections.Generic.Dictionary`2<System.String,System.String>"
            && call.Name == "TryGetValue");
        int duplicateCompare = FindCallIndex(indexCalls, call =>
            call.DeclaringType == "System.String" && call.Name == "Equals");
        int firstEnqueue = FindCallIndex(indexCalls, call =>
            call.DeclaringType.StartsWith("System.Collections.Generic.Queue`1<", StringComparison.Ordinal)
            && call.Name == "Enqueue");
        Require(duplicateLookup >= 0 && duplicateCompare > duplicateLookup && firstEnqueue > duplicateCompare,
            "Subject readback index does not reject the same current DuelId before enqueue.");
        foreach (string queueMethod in new[] { "Enqueue", "Dequeue" })
        {
            Require(indexCalls.Any(call =>
                    call.DeclaringType.StartsWith("System.Collections.Generic.Queue`1<", StringComparison.Ordinal)
                    && call.Name == queueMethod),
                "Subject readback index is missing bounded queue operation: " + queueMethod);
        }
        Require(indexCalls.Any(call =>
                call.DeclaringType == "System.Collections.Generic.Dictionary`2<System.String,System.String>"
                && call.Name == "Remove"),
            "Subject readback index no longer evicts the superseded bounded entry.");

        MetadataAssembly.MethodView syncData = assembly.RequireMethod(
            DuelBehaviorType, "SyncData", "TaleWorlds.CampaignSystem.IDataStore");
        HashSet<string> processLocalFields = new(StringComparer.Ordinal)
        {
            "_latestDuelOutcomeIdsBySubject",
            "_duelOutcomeSubjectIndexOrder",
            "_duelOutcomeSubjectIndexSync"
        };
        Require(!assembly.GetReferencedFields(syncData).Any(field => processLocalFields.Contains(field.Name)),
            "SyncData references the process-local subject readback index.");
    }

    private static void VerifyOwnerHostRoutes(MetadataAssembly assembly)
    {
        MetadataAssembly.MethodView begin = assembly.RequireUniqueMethod(DuelBehaviorType, "TryBeginDuelOutcome");
        MetadataAssembly.MethodView record = assembly.RequireUniqueMethod(DuelBehaviorType, "TryRecordDuelOutcome");
        MetadataAssembly.MethodView finalize = assembly.RequireUniqueMethod(DuelBehaviorType, "TryFinalizeDuelOutcome");
        MetadataAssembly.MethodView markUnknown = assembly.RequireUniqueMethod(DuelBehaviorType, "MarkDuelOutcomeUnknown");
        MetadataAssembly.MethodView read = assembly.RequireUniqueMethod(DuelBehaviorType, "TryReadDuelOutcome");

        RequireCall(assembly, begin, OwnerType, "Queue");
        RequireCall(assembly, begin, OwnerType, "Start");
        RequireCall(assembly, record, OwnerType, "RecordOutcome");
        RequireCall(assembly, finalize, OwnerType, "Finalize");
        Require(!assembly.CallsTransitively(
                finalize,
                call => call.DeclaringType == OwnerType && call.Name == "RecordOutcome",
                out _),
            "TryFinalizeDuelOutcome regressed to recording the result after effects.");
        RequireCall(assembly, markUnknown, OwnerType, "MarkUnknownAfterStart");
        RequireCall(assembly, read, OwnerType, "TryGet");
        RequireCall(assembly, begin, DuelBehaviorType, "IndexDuelOutcome");
        RequireCall(assembly, finalize, DuelBehaviorType, "IndexDuelOutcome");
        RequireCall(assembly, markUnknown, DuelBehaviorType, "IndexDuelOutcome");

        MetadataAssembly.MethodView[] writers =
        {
            assembly.RequireMethod(DuelBehaviorType + "+ArenaDuelMissionBehavior", "EndDuelLocal", "System.Boolean"),
            assembly.RequireMethod(
                DuelBehaviorType,
                "SettleWildernessDuelRuntime",
                DuelBehaviorType + "+WildernessDuelBattleRuntime",
                "System.Boolean",
                "System.String"),
            assembly.RequireMethod(DuelBehaviorType, "EndDuel", "System.Boolean")
        };

        foreach (MetadataAssembly.MethodView writer in writers)
        {
            Require(assembly.CallsTransitively(
                    writer,
                    call => call.DeclaringType == DuelBehaviorType && call.Name == "TryRecordDuelOutcome",
                    out IReadOnlyList<string> recordPath),
                "Terminal writer does not lock the typed result before effects: " + writer.DisplaySignature);
            Require(assembly.CallsTransitively(
                    writer,
                    call => call.DeclaringType == DuelBehaviorType && call.Name == "TryFinalizeDuelOutcome",
                    out IReadOnlyList<string> path),
                "Terminal writer does not route through TryFinalizeDuelOutcome: " + writer.DisplaySignature);
            Console.WriteLine("ROUTE " + string.Join(" -> ", recordPath));
            Console.WriteLine("ROUTE " + string.Join(" -> ", path));
        }
    }

    private static void VerifyNoLoadOrTickReplay(MetadataAssembly assembly)
    {
        MetadataAssembly.MethodView syncData = assembly.RequireMethod(
            DuelBehaviorType, "SyncData", "TaleWorlds.CampaignSystem.IDataStore");
        MetadataAssembly.MethodView registerEvents = assembly.RequireMethod(DuelBehaviorType, "RegisterEvents");
        Require(!assembly.GetDirectCalls(registerEvents).Any(IsOwnerOrReadbackCall),
            "RegisterEvents directly calls the typed Duel owner.");
        Require(!assembly.GetReferencedFields(registerEvents).Any(field => field.Name == "_duelOutcomeOwner"),
            "RegisterEvents references _duelOutcomeOwner.");

        IReadOnlyList<MetadataAssembly.MethodCallView> syncCalls = assembly.GetDirectCalls(syncData);
        Require(syncCalls.Count(call =>
                    call.DeclaringType == DuelBehaviorType && call.Name == "MarkDuelOutcomeUnknown") == 2,
            "SyncData load boundary must emit exactly two unknown transitions for active Duel sessions.");
        Require(syncCalls.Any(call =>
                call.DeclaringType == "TaleWorlds.CampaignSystem.IDataStore" && call.Name == "get_IsLoading"),
            "SyncData does not guard the unknown transition with IDataStore.IsLoading.");
        Require(!syncCalls.Any(call =>
                call.DeclaringType == OwnerType
                || (call.DeclaringType == DuelBehaviorType
                    && (call.Name == "TryBeginDuelOutcome"
                        || call.Name == "TryFinalizeDuelOutcome"
                        || call.Name == "TryReadDuelOutcome"))),
            "SyncData can begin, finalize, read, or directly replay the typed Duel owner.");
        Require(!assembly.GetReferencedFields(syncData).Any(field => field.Name == "_duelOutcomeOwner"),
            "SyncData directly references _duelOutcomeOwner.");
        IReadOnlyList<string> syncStrings = assembly.GetLoadedStrings(syncData);
        Require(syncStrings.Count(value => value == "save_generation_changed") == 2
                && syncStrings.Contains("syncdata_load", StringComparer.Ordinal)
                && syncStrings.Contains("syncdata_load_wilderness", StringComparer.Ordinal),
            "SyncData unknown-transition reason/source markers drifted.");

        MetadataAssembly.TypeView behavior = assembly.RequireType(DuelBehaviorType);
        IEnumerable<MetadataAssembly.TypeView> duelTypes = new[]
        {
            behavior,
            assembly.RequireType(DuelBehaviorType + "+ArenaDuelMissionBehavior"),
            assembly.RequireType(DuelBehaviorType + "+WildernessDuelBattleMissionLogic")
        };
        foreach (MetadataAssembly.MethodView method in duelTypes
                     .SelectMany(type => type.Methods)
                     .Where(method => method.Name.Contains("Tick", StringComparison.Ordinal)
                                      || method.Name.Contains("Load", StringComparison.Ordinal)))
        {
            Require(!assembly.GetDirectCalls(method).Any(call =>
                    call.DeclaringType == OwnerType
                    || (call.DeclaringType == DuelBehaviorType && call.Name == "TryReadDuelOutcome")),
                "Load/tick method directly replays or reads the typed Duel owner: " + method.DisplaySignature);
            Require(!assembly.GetReferencedFields(method).Any(field => field.Name == "_duelOutcomeOwner"),
                "Load/tick method directly references _duelOutcomeOwner: " + method.DisplaySignature);
        }
    }

    private static void VerifyStakeArmGate(MetadataAssembly assembly)
    {
        MetadataAssembly.MethodView method = assembly.RequireMethod(
            DuelBehaviorType,
            "TryCacheDuelStakeFromText",
            "TaleWorlds.CampaignSystem.Hero",
            "System.String&");
        Require(method.IsPublic && method.IsStatic && method.ReturnType == "System.Boolean",
            "TryCacheDuelStakeFromText ABI drifted: " + method.DisplaySignature);
        IReadOnlyList<string> strings = assembly.GetLoadedStrings(method);
        Require(strings.Count(value => value == "\\[ACTION:DUEL\\]") == 1,
            "Production stake parser does not contain exactly one exact [ACTION:DUEL] gate pattern.");
        Require(assembly.GetDirectCalls(method).Any(call =>
                call.DeclaringType == "System.Text.RegularExpressions.Regex"
                && call.Name == "IsMatch"
                && call.ParameterTypes.SequenceEqual(
                    new[] { "System.String", "System.String", "System.Text.RegularExpressions.RegexOptions" },
                    StringComparer.Ordinal)),
            "Production stake parser does not execute the exact action gate through Regex.IsMatch.");
        Require(assembly.GetDirectCalls(method).Any(call =>
                call.DeclaringType == DuelBehaviorType && call.Name == "CachePendingDuelStake"),
            "Production stake parser no longer reaches the bounded pending-stake cache.");
    }

    private static void VerifyPendingArtifactBinding(MetadataAssembly assembly)
    {
        foreach (string nestedType in new[]
        {
            DuelBehaviorType + "+DuelAfterLines",
            DuelBehaviorType + "+PendingDuelStake",
            DuelBehaviorType + "+PendingDuelDebtTag"
        })
        {
            MetadataAssembly.FieldView outcomeId = RequireField(assembly.RequireType(nestedType), "DuelOutcomeId");
            Require(outcomeId.FieldType == "System.String",
                nestedType + " DuelOutcomeId type drifted: " + outcomeId.FieldType);
        }

        MetadataAssembly.MethodView begin = assembly.RequireUniqueMethod(DuelBehaviorType, "TryBeginDuelOutcome");
        RequireCall(assembly, begin, DuelBehaviorType, "BuildPendingDuelArtifactFingerprint");
        RequireCall(assembly, begin, DuelBehaviorType, "BindPendingDuelArtifacts");

        MetadataAssembly.MethodView bind = assembly.RequireMethod(
            DuelBehaviorType, "BindPendingDuelArtifacts", "System.String", "System.String");
        HashSet<string> boundTypes = assembly.GetReferencedFields(bind)
            .Where(field => field.Name == "DuelOutcomeId")
            .Select(field => field.DeclaringType)
            .ToHashSet(StringComparer.Ordinal);
        Require(boundTypes.SetEquals(new[]
            {
                DuelBehaviorType + "+DuelAfterLines",
                DuelBehaviorType + "+PendingDuelStake",
                DuelBehaviorType + "+PendingDuelDebtTag"
            }),
            "BindPendingDuelArtifacts does not bind all three pending artifact types.");

        MetadataAssembly.MethodView discardUnbound = assembly.RequireMethod(
            DuelBehaviorType, "DiscardUnboundDuelArtifacts", "System.String");
        MetadataAssembly.MethodView discardBound = assembly.RequireMethod(
            DuelBehaviorType, "DiscardBoundDuelArtifacts", "System.String", "System.String");
        foreach (MetadataAssembly.MethodView discard in new[] { discardUnbound, discardBound })
        {
            HashSet<string> discardedTypes = assembly.GetReferencedFields(discard)
                .Where(field => field.Name == "DuelOutcomeId")
                .Select(field => field.DeclaringType)
                .ToHashSet(StringComparer.Ordinal);
            Require(discardedTypes.SetEquals(boundTypes),
                "Pending artifact cleanup does not inspect all three DuelOutcomeId bindings: "
                + discard.DisplaySignature);
        }
        RequireCall(assembly, begin, DuelBehaviorType, "DiscardUnboundDuelArtifacts");
        RequireCall(assembly, begin, DuelBehaviorType, "DiscardDuelArtifactsForRequest");
        MetadataAssembly.MethodView finalize = assembly.RequireUniqueMethod(DuelBehaviorType, "TryFinalizeDuelOutcome");
        MetadataAssembly.MethodView unknown = assembly.RequireUniqueMethod(DuelBehaviorType, "MarkDuelOutcomeUnknown");
        RequireCall(assembly, finalize, DuelBehaviorType, "DiscardBoundDuelArtifacts");
        RequireCall(assembly, unknown, DuelBehaviorType, "DiscardBoundDuelArtifacts");

        foreach (MetadataAssembly.MethodView failurePath in new[]
        {
            assembly.RequireMethod(
                DuelBehaviorType, "PrepareDuel", "TaleWorlds.CampaignSystem.Hero", "System.Single"),
            assembly.RequireMethod(DuelBehaviorType, "GlobalDuelStarterTick"),
            assembly.RequireMethod(DuelBehaviorType, "StartDuelInternal", "TaleWorlds.MountAndBlade.Agent")
        })
        {
            RequireCall(assembly, failurePath, DuelBehaviorType, "DiscardUnboundDuelArtifacts");
        }

        MetadataAssembly.MethodView consumeDebt = assembly.RequireMethod(
            DuelBehaviorType,
            "TryConsumePendingDuelDebtTagForOutcome",
            "TaleWorlds.CampaignSystem.Hero",
            "System.String",
            "System.Int32&",
            "System.Int32&",
            "System.String&");
        MetadataAssembly.MethodView consumeStake = assembly.RequireMethod(
            DuelBehaviorType,
            "TryConsumePendingDuelStake",
            "System.String",
            "System.String",
            DuelBehaviorType + "+PendingDuelStake&");
        MetadataAssembly.MethodView consumeLines = assembly.RequireMethod(
            DuelBehaviorType,
            "TryConsumeDuelAfterLines",
            "TaleWorlds.CampaignSystem.Hero",
            "System.String",
            DuelBehaviorType + "+DuelAfterLines&");
        foreach (MetadataAssembly.MethodView consume in new[] { consumeDebt, consumeStake, consumeLines })
        {
            Require(assembly.GetReferencedFields(consume).Any(field => field.Name == "DuelOutcomeId")
                    && assembly.GetDirectCalls(consume).Any(call =>
                        call.DeclaringType == "System.String" && call.Name == "Equals"),
                "Pending artifact consume is not bound to expected DuelId: " + consume.DisplaySignature);
        }

        MetadataAssembly.MethodView settleStake = assembly.RequireUniqueMethod(
            DuelBehaviorType, "ApplyDuelStakeSettlementAndBuildResultText");
        RequireCall(assembly, settleStake, DuelBehaviorType, "TryConsumePendingDuelDebtTagForOutcome");
        RequireCall(assembly, settleStake, DuelBehaviorType, "TryConsumePendingDuelStake");
        MetadataAssembly.MethodView shout = assembly.RequireUniqueMethod(DuelBehaviorType, "TryPostDuelAiShout");
        RequireCall(assembly, shout, DuelBehaviorType, "TryConsumeDuelAfterLines");

        foreach ((string typeName, string methodName) in new[]
        {
            ("AnimusForge.MyBehavior", "NormalizeDuelPostprocessTags"),
            ("AnimusForge.ShoutBehavior", "NormalizeDuelPostprocessTagsForScene")
        })
        {
            MetadataAssembly.MethodView normalizer = assembly.RequireUniqueMethod(typeName, methodName);
            IReadOnlyList<MetadataAssembly.MethodCallView> calls = assembly.GetDirectCalls(normalizer);
            int clear = FindCallIndex(calls, call =>
                call.DeclaringType == DuelBehaviorType && call.Name == "ClearPendingDuelDebtTag");
            int cache = FindCallIndex(calls, call =>
                call.DeclaringType == DuelBehaviorType && call.Name == "CachePendingDuelDebtTag");
            Require(clear >= 0 && cache > clear,
                typeName + "::" + methodName + " does not clear stale debt before caching this Duel reply.");
        }
    }

    private static void VerifyExactDispatchProvenance(MetadataAssembly assembly)
    {
        const string executorType = "AnimusForge.Refactor.Adapters.LegacyNativeActionPlanExecutor";
        const string committerType = RuntimeNamespace + "InteractionResultCommitter";
        MetadataAssembly.TypeView behavior = assembly.RequireType(DuelBehaviorType);
        foreach ((string Name, bool IsStatic) field in new[]
        {
            ("_queuedDuelDispatchContext", true),
            ("_openingDuelDispatchContext", true),
            ("_meetingPendingDuelDispatchContext", false)
        })
        {
            MetadataAssembly.FieldView actual = RequireField(behavior, field.Name);
            Require(actual.FieldType == DispatchContextType
                    && actual.IsPrivate
                    && actual.IsStatic == field.IsStatic,
                "Exact Duel holder drifted: " + field.Name + " type=" + actual.FieldType);
        }

        MetadataAssembly.TypeView arenaBehavior = assembly.RequireType(
            DuelBehaviorType + "+ArenaDuelMissionBehavior");
        foreach (string fieldName in new[] { "_duelDispatchContext", "_nonHeroMemoryId", "_targetCharacter" })
        {
            MetadataAssembly.FieldView field = RequireField(arenaBehavior, fieldName);
            Require(field.IsPrivate && !field.IsStatic && field.IsInitOnly,
                "Arena immutable delayed holder drifted: " + fieldName);
        }
        MetadataAssembly.FieldView wildernessContext = RequireField(
            assembly.RequireType(DuelBehaviorType + "+WildernessDuelBattleRuntime"),
            "DuelDispatchContext");
        Require(wildernessContext.FieldType == DispatchContextType && !wildernessContext.IsStatic,
            "Wilderness runtime no longer carries the exact dispatch context.");
        MetadataAssembly.FieldView nonHeroMemory = RequireField(
            assembly.RequireType("AnimusForge.Refactor.Adapters.MyBehaviorMemoryFacade"),
            "_nonHeroMemoryId");
        Require(nonHeroMemory.FieldType == "System.String" && nonHeroMemory.IsPrivate && nonHeroMemory.IsInitOnly,
            "Non-hero memory owner no longer retains an immutable subject identity.");

        foreach (string targetType in new[]
        {
            "TaleWorlds.CampaignSystem.Hero",
            "TaleWorlds.MountAndBlade.Agent",
            "TaleWorlds.CampaignSystem.CharacterObject"
        })
        {
            MetadataAssembly.MethodView prepare = assembly.RequireMethod(
                DuelBehaviorType,
                "PrepareDuelForDetachedRequest",
                targetType,
                "System.Single",
                DispatchContextType);
            AssertStaticMethod(prepare, "System.Void");
            RequireCall(assembly, prepare, DuelBehaviorType, "ValidateDetachedDuelTarget");
            RequireCall(assembly, prepare, DuelBehaviorType, "BindPendingDuelArtifacts");
        }
        MetadataAssembly.MethodView validateTarget = assembly.RequireMethod(
            DuelBehaviorType,
            "ValidateDetachedDuelTarget",
            DispatchContextType,
            "System.String",
            "System.String");
        Require(assembly.GetDirectCalls(validateTarget).Any(call =>
                call.DeclaringType == "System.String" && call.Name == "Equals"),
            "Actual Duel subject validation no longer performs an independent ordinal comparison.");

        MetadataAssembly.MethodView readByRequest = assembly.RequireMethod(
            DuelBehaviorType,
            "TryReadDuelOutcomeByRequestId",
            "System.String",
            ReceiptType + "&");
        AssertStaticMethod(readByRequest, "System.Boolean");
        RequireCall(assembly, readByRequest, DuelBehaviorType, "TryReadDuelOutcome");

        MetadataAssembly.MethodView exactFactory = assembly.FindMethods(
                executorType,
                "CreateRequestBoundDuelExecutor")
            .Single();
        Require(exactFactory.IsStatic
                && exactFactory.ReturnType == executorType,
            "Exact executor factory ABI drifted: " + exactFactory.DisplaySignature);
        foreach (string factoryName in new[]
        {
            "CreateNativeConversationActionPlanExecutorForExternal",
            "CreateSceneShoutActionPlanExecutorForExternal"
        })
        {
            MetadataAssembly.MethodView factory = assembly.FindMethods(
                    "AnimusForge.ShoutBehavior",
                    factoryName)
                .Single();
            RequireCall(assembly, factory, executorType, "CreateRequestBoundDuelExecutor");
        }
        MetadataAssembly.MethodView courierFactory = assembly.FindMethods(
                "AnimusForge.CourierDeliveryBehavior",
                "CreateCourierReplyActionPlanExecutorForExternal")
            .Single();
        RequireCall(assembly, courierFactory, executorType, "CreateRequestBoundDuelExecutor");

        MetadataAssembly.MethodView commit = assembly.FindMethods(committerType, "Commit").Single();
        RequireCall(assembly, commit, committerType, "BuildCanonicalRequestId");
        RequireCall(assembly, commit, committerType, "BuildCanonicalActionPlanFingerprint");

        string requestType = RuntimeNamespace + "DuelOutcomeRequestIdentity";
        MetadataAssembly.MethodView markUnknownAfterDispatch = assembly.RequireMethod(
            OwnerType,
            "MarkUnknownAfterDispatch",
            requestType,
            "System.String",
            ReceiptType + "&",
            "System.String&");
        AssertInstanceMethod(markUnknownAfterDispatch, RuntimeNamespace + "DuelOutcomeOperationStatus");
        Require(assembly.GetLoadedStrings(markUnknownAfterDispatch)
                .Contains("AnimusForge.DuelOutcome.UnknownAfterDispatch.v1", StringComparer.Ordinal),
            "UnknownAfterDispatch provenance fingerprint drifted.");

        MetadataAssembly.MethodView dispatchUnknown = assembly.RequireMethod(
            DuelBehaviorType + "+DuelBehaviorDetachedDispatchOwner",
            "MarkUnknownAfterStart",
            DispatchContextType,
            "System.String");
        RequireCall(assembly, dispatchUnknown, OwnerType, "MarkUnknownAfterDispatch");

        MetadataAssembly.TypeView dispatchContext = assembly.RequireType(DispatchContextType);
        Require(dispatchContext.Fields.All(field =>
                !field.FieldType.Contains("TaleWorlds", StringComparison.Ordinal)
                && !field.FieldType.Contains("System.Action", StringComparison.Ordinal)
                && !field.FieldType.Contains("System.Func", StringComparison.Ordinal)),
            "Exact Duel context retained a game object or callback.");

        MetadataAssembly.MethodView syncData = assembly.RequireMethod(
            DuelBehaviorType,
            "SyncData",
            "TaleWorlds.CampaignSystem.IDataStore");
        Require(!assembly.GetDirectCalls(syncData).Any(call =>
                call.DeclaringType == OwnerType
                && (call.Name == "Queue" || call.Name == "Start")),
            "SyncData creates or starts an exact Duel request.");
        RequireCall(assembly, syncData, DuelBehaviorType, "ClearDetachedDuelDispatchesForLoad");

        MetadataAssembly.MethodView clearLoad = assembly.RequireMethod(
            DuelBehaviorType,
            "ClearDetachedDuelDispatchesForLoad");
        HashSet<string> clearedRuntimeFields = assembly.GetReferencedFields(clearLoad)
            .Select(field => field.Name)
            .ToHashSet(StringComparer.Ordinal);
        foreach (string fieldName in new[]
        {
            "_meetingPendingDuelDispatchContext",
            "_queuedDuelDispatchContext",
            "_openingDuelDispatchContext",
            "DuelDispatchContext",
            "_meetingPendingStart",
            "_queuedDuelWaitingForConversationExit",
            "_leaveSourceMissionRequested",
            "_pendingDuelTarget",
            "_arenaMissionActive",
            "_pendingMainHeroDeath",
            "_isDuelActive"
        })
        {
            Require(clearedRuntimeFields.Contains(fieldName),
                "Load cleanup IL omits Duel trigger/holder: " + fieldName);
        }
        HashSet<string> syncRuntimeFields = assembly.GetReferencedFields(syncData)
            .Select(field => field.Name)
            .ToHashSet(StringComparer.Ordinal);
        Require(syncRuntimeFields.Contains("AbortRequested")
                && syncRuntimeFields.Contains("DuelOutcomeStart"),
            "SyncData load does not mark the wilderness runtime aborted and clear its typed start.");

        MetadataAssembly.MethodView delayedReady = assembly.RequireMethod(
            DuelBehaviorType,
            "IsDetachedDuelDispatchReadyForDelayedHost",
            DispatchContextType);
        AssertStaticMethod(delayedReady, "System.Boolean");
        IReadOnlyList<MetadataAssembly.MethodCallView> delayedReadyCalls = assembly.GetDirectCalls(delayedReady);
        Require(delayedReadyCalls.Any(call => call.DeclaringType == DispatchContextType && call.Name == "Snapshot")
                && delayedReadyCalls.Any(call => call.DeclaringType == DispatchReceiptType && call.Name == "get_State")
                && delayedReadyCalls.Any(call => call.DeclaringType == DispatchReceiptType && call.Name == "get_HostAccepted"),
            "Delayed Duel readiness ABI no longer checks Queued plus HostAccepted receipt state.");
        foreach (MetadataAssembly.MethodView consumer in new[]
        {
            assembly.RequireMethod(DuelBehaviorType, "GlobalDuelStarterTick"),
            assembly.RequireMethod(DuelBehaviorType, "OnEngineTick"),
            assembly.RequireMethod(DuelBehaviorType, "GlobalSourceMissionLeaveTick")
        })
        {
            RequireCall(assembly, consumer, DuelBehaviorType, "IsDetachedDuelDispatchReadyForDelayedHost");
        }

        foreach (string targetType in new[]
        {
            "TaleWorlds.CampaignSystem.Hero",
            "TaleWorlds.CampaignSystem.CharacterObject"
        })
        {
            MetadataAssembly.MethodView prepare = assembly.RequireMethod(
                DuelBehaviorType,
                "PrepareDuel",
                targetType,
                "System.Single",
                DispatchContextType);
            Require(assembly.GetLoadedStrings(prepare).Contains("arena_vlandia_a", StringComparer.OrdinalIgnoreCase),
                "Current-arena direct-start scene marker drifted: " + prepare.DisplaySignature);
            RequireCall(assembly, prepare, DuelBehaviorType, "StartDuelViaAI");
        }

        MetadataAssembly.MethodView rejectExact = assembly.RequireMethod(
            DuelBehaviorType + "+DuelBehaviorDetachedDispatchOwner",
            "Reject",
            DispatchContextType,
            "System.String");
        AssertCallOrder(
            assembly,
            rejectExact,
            call => call.DeclaringType == "System.Collections.Generic.HashSet`1<System.String>" && call.Name == "Add",
            call => call.DeclaringType == DuelBehaviorType && call.Name == "RejectDuelOutcomeRequest",
            "Exact Reject must persist its bounded tombstone before owner rejection.");

        MetadataAssembly.MethodView arenaAfterStart = assembly.RequireMethod(
            DuelBehaviorType + "+ArenaDuelMissionBehavior",
            "AfterStart");
        AssertCallOrder(
            assembly,
            arenaAfterStart,
            call => call.DeclaringType == DuelBehaviorType + "+ArenaDuelMissionBehavior" && call.Name == "EnsureDuelOutcomeStarted",
            call => call.DeclaringType == DuelBehaviorType + "+ArenaDuelMissionBehavior" && call.Name == "SetupArenaDuel",
            "Arena owner Start must precede arena gameplay setup.");
        MetadataAssembly.MethodView wildernessInitialize = assembly.RequireMethod(
            DuelBehaviorType + "+WildernessDuelBattleMissionLogic",
            "OnBehaviorInitialize");
        AssertCallOrder(
            assembly,
            wildernessInitialize,
            call => call.DeclaringType == DuelBehaviorType + "+WildernessDuelBattleMissionLogic" && call.Name == "EnsureDuelOutcomeStarted",
            call => call.DeclaringType == DuelBehaviorType && call.Name == "EnsureMainHeroHealthForWildernessDuel",
            "Wilderness owner Start must precede gameplay mutation.");
        MetadataAssembly.MethodView inPlaceStart = assembly.RequireMethod(
            DuelBehaviorType,
            "StartDuelInternal",
            "TaleWorlds.MountAndBlade.Agent",
            DispatchContextType,
            "System.String");
        AssertCallOrder(
            assembly,
            inPlaceStart,
            call => call.DeclaringType == DuelBehaviorType && call.Name == "TryBeginDuelOutcome",
            call => call.DeclaringType == "TaleWorlds.MountAndBlade.Mission" && call.Name == "SetMissionMode",
            "In-place owner Start must precede Mission mutation.");

        MetadataAssembly.MethodView openingTick = assembly.RequireMethod(DuelBehaviorType, "GlobalArenaLeaveTick");
        Require(assembly.GetLoadedStrings(openingTick).Contains("opening_timeout_before_afterstart", StringComparer.Ordinal)
                && assembly.GetDirectCalls(openingTick).Any(call =>
                    call.DeclaringType == DuelBehaviorType && call.Name == "MarkDetachedDuelDispatchUnknownAfterStart"),
            "Opening timeout no longer terminalizes the exact dispatch Unknown.");
        MetadataAssembly.MethodView setupTick = assembly.RequireMethod(
            DuelBehaviorType + "+ArenaDuelMissionBehavior",
            "OnMissionTick",
            "System.Single");
        Require(assembly.GetLoadedStrings(setupTick).Contains("arena_setup_timeout", StringComparer.Ordinal)
                && assembly.GetDirectCalls(setupTick).Any(call =>
                    call.DeclaringType == DuelBehaviorType && call.Name == "MarkDetachedDuelDispatchUnknownAfterStart")
                && assembly.GetLoadedInt32Constants(setupTick).Contains(3),
            "Arena setup retry/timeout bound no longer terminalizes the exact dispatch Unknown.");

        MetadataAssembly.MethodView wildernessTick = assembly.RequireMethod(
            DuelBehaviorType + "+WildernessDuelBattleMissionLogic",
            "OnMissionTick",
            "System.Single");
        MetadataAssembly.FieldView participantDeadline = RequireField(
            assembly.RequireType(DuelBehaviorType + "+WildernessDuelBattleMissionLogic"),
            "_participantDeadline");
        Require(participantDeadline.FieldType == "System.Single"
                && participantDeadline.IsPrivate
                && !participantDeadline.IsStatic
                && assembly.GetLoadedStrings(wildernessTick)
                    .Contains("wilderness_participant_timeout", StringComparer.Ordinal)
                && assembly.GetReferencedFields(wildernessTick).Any(field => field.Name == "AbortRequested")
                && assembly.GetDirectCalls(wildernessTick).Any(call =>
                    call.DeclaringType == DuelBehaviorType && call.Name == "MarkDetachedDuelDispatchUnknownAfterStart"),
            "Wilderness participant timeout/abort IL contract drifted.");

        MetadataAssembly.MethodView arenaSettlement = assembly.RequireMethod(
            DuelBehaviorType + "+ArenaDuelMissionBehavior",
            "EndDuelLocal",
            "System.Boolean");
        MetadataAssembly.MethodView wildernessSettlement = assembly.RequireMethod(
            DuelBehaviorType,
            "SettleWildernessDuelRuntime",
            DuelBehaviorType + "+WildernessDuelBattleRuntime",
            "System.Boolean",
            "System.String");
        Require(assembly.GetReferencedFields(arenaSettlement).Any(field => field.Name == "_abortRequested")
                && assembly.GetReferencedFields(wildernessSettlement).Any(field => field.Name == "AbortRequested"),
            "Arena/Wilderness settlement no longer observes its abort guard.");

        MetadataAssembly.MethodView executeCore = assembly.RequireUniqueMethod(executorType, "ValidateAndExecuteCore");
        IReadOnlyList<string> executeStrings = assembly.GetLoadedStrings(executeCore);
        Require(executeStrings.Contains("duel.unsupported_channel", StringComparer.Ordinal),
            "Exact executor Courier preflight marker drifted.");
        MetadataAssembly.MethodView resolveExact = assembly.RequireUniqueMethod(
            executorType,
            "ResolveExactDuelDispatchStatus");
        IReadOnlyList<string> resolutionStrings = assembly.GetLoadedStrings(resolveExact);
        foreach (string marker in new[]
        {
            "duel.dispatch_host_side_effect_pending",
            "duel.dispatch_queued_companion_effect_unknown",
            "duel.companion_effect_unknown"
        })
        {
            Require(resolutionStrings.Contains(marker, StringComparer.Ordinal),
                "Exact executor conservative/preflight marker drifted: " + marker);
        }
    }

    private static void VerifyFourberieSeam(MetadataAssembly assembly)
    {
        const string compatibilityType = "AnimusForge.FourberieDuelCompatibility";
        MetadataAssembly.TypeView compatibility = assembly.RequireType(compatibilityType);
        Require((compatibility.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic
                && (compatibility.Attributes & TypeAttributes.Abstract) != 0
                && (compatibility.Attributes & TypeAttributes.Sealed) != 0,
            "FourberieDuelCompatibility must remain an internal static type.");

        AssertStaticMethod(
            assembly.RequireMethod(compatibilityType, "EnsurePatched", "HarmonyLib.Harmony"),
            "System.Void");
        AssertStaticMethod(assembly.RequireMethod(compatibilityType, "BeginWildernessMissionOpening"), "System.Void");
        AssertStaticMethod(assembly.RequireMethod(compatibilityType, "CompleteWildernessMissionOpening"), "System.Void");
        AssertStaticMethod(assembly.RequireMethod(compatibilityType, "CancelWildernessMissionOpening"), "System.Void");
        MetadataAssembly.MethodView blockReason = assembly.RequireMethod(
            compatibilityType, "TryGetDuelStartBlockReason", "System.String&");
        AssertStaticMethod(blockReason, "System.Boolean");
        Require(assembly.GetParameter(blockReason, 1).IsOut,
            "Fourberie block reason parameter is no longer out.");

        MetadataAssembly.MethodView guard = assembly.RequireMethod(DuelBehaviorType, "TryBlockDuelForFourberieCombat");
        RequireCall(assembly, guard, compatibilityType, "TryGetDuelStartBlockReason");
        RequireCall(
            assembly,
            assembly.RequireMethod(DuelBehaviorType, "PrepareDuel", "TaleWorlds.CampaignSystem.Hero", "System.Single"),
            DuelBehaviorType,
            "TryBlockDuelForFourberieCombat");
        RequireCall(
            assembly,
            assembly.RequireMethod(DuelBehaviorType, "PrepareDuel", "TaleWorlds.CampaignSystem.CharacterObject", "System.Single"),
            DuelBehaviorType,
            "TryBlockDuelForFourberieCombat");
        RequireCall(
            assembly,
            assembly.RequireMethod(DuelBehaviorType, "StartDuelInternal", "TaleWorlds.MountAndBlade.Agent"),
            DuelBehaviorType,
            "TryBlockDuelForFourberieCombat");

        MetadataAssembly.MethodView wildernessOpen = assembly.RequireMethod(
            DuelBehaviorType, "TryOpenWildernessDuelMission", "TaleWorlds.CampaignSystem.CharacterObject");
        foreach (string methodName in new[]
        {
            "BeginWildernessMissionOpening",
            "CompleteWildernessMissionOpening",
            "CancelWildernessMissionOpening"
        })
        {
            RequireCall(assembly, wildernessOpen, compatibilityType, methodName);
        }

        MetadataAssembly.MethodView moduleLoad = assembly.RequireMethod(
            "AnimusForge.SubModule", "OnBeforeInitialModuleScreenSetAsRoot");
        RequireCall(assembly, moduleLoad, compatibilityType, "EnsurePatched");
    }

    private static string BuildSurfaceFingerprint(MetadataAssembly assembly)
    {
        StringBuilder builder = new();
        foreach (string typeName in new[]
        {
            RuntimeNamespace + "DuelOutcomeState",
            RuntimeNamespace + "DuelOutcomeEffectState",
            RuntimeNamespace + "DuelOutcomeChannel",
            RuntimeNamespace + "DuelSessionKind",
            RuntimeNamespace + "DuelResultKind",
            RuntimeNamespace + "DuelOutcomeOperationStatus",
            RuntimeNamespace + "DetachedDuelDispatchState",
            DispatchContextType,
            DispatchReceiptType,
            RuntimeNamespace + "IDetachedDuelDispatchOwner",
            RuntimeNamespace + "IDetachedDuelDispatchExecutionReceipt",
            RuntimeNamespace + "DuelOutcomeRequestIdentity",
            RuntimeNamespace + "DuelOutcomeStartIdentity",
            RuntimeNamespace + "DuelOutcomeResultIdentity",
            RuntimeNamespace + "DuelOutcomeEffects",
            ReceiptType,
            OwnerType
        })
        {
            MetadataAssembly.TypeView type = assembly.RequireType(typeName);
            builder.AppendLine(type.FullName + " : " + type.BaseType);
            foreach (MetadataAssembly.MethodView method in type.Methods.OrderBy(method => method.DisplaySignature, StringComparer.Ordinal))
            {
                builder.AppendLine(method.DisplaySignature);
            }
            if (type.BaseType == "System.Enum")
            {
                foreach ((string name, int value) in assembly.GetEnumValues(typeName).OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    builder.AppendLine(name + "=" + value);
                }
            }
        }

        foreach (string methodName in new[]
        {
            "PrepareDuel",
            "StartDuelViaAI",
            "TryConsumeLastDuelResult",
            "TryCacheDuelStakeFromText",
            "SyncData",
            "TryBeginDuelOutcome",
            "QueueDuelOutcomeRequest",
            "TryRecordDuelOutcome",
            "TryFinalizeDuelOutcome",
            "MarkDuelOutcomeUnknown",
            "TryReadDuelOutcome",
            "TryReadLatestDuelOutcome",
            "TryReadDuelOutcomeByRequestId",
            "PrepareDuelForDetachedRequest",
            "RejectDetachedDuelDispatchForExternal",
            "ClearDetachedDuelDispatchesForLoad",
            "IndexDuelOutcome",
            "BuildPendingDuelArtifactFingerprint",
            "BindPendingDuelArtifacts",
            "DiscardUnboundDuelArtifacts",
            "DiscardBoundDuelArtifacts",
            "DiscardDuelArtifactsForRequest",
            "TryConsumePendingDuelDebtTagForOutcome",
            "TryConsumePendingDuelStake",
            "TryConsumeDuelAfterLines"
        })
        {
            foreach (MetadataAssembly.MethodView method in assembly.FindMethods(DuelBehaviorType, methodName)
                         .OrderBy(method => method.DisplaySignature, StringComparer.Ordinal))
            {
                builder.AppendLine(method.DisplaySignature);
            }
        }
        MetadataAssembly.TypeView duel = assembly.RequireType(DuelBehaviorType);
        builder.AppendLine(RequireField(duel, "_duelCooldowns").FieldType);
        builder.AppendLine(RequireField(duel, "_duelOutcomeOwner").FieldType);
        builder.AppendLine(RequireField(duel, "_duelOutcomeOwnerSync").FieldType);
        builder.AppendLine(RequireField(duel, "_latestDuelOutcomeIdsBySubject").FieldType);
        builder.AppendLine(RequireField(duel, "_duelOutcomeSubjectIndexOrder").FieldType);
        builder.AppendLine(RequireField(duel, "_duelOutcomeIdsByRequest").FieldType);
        builder.AppendLine(RequireField(duel, "_queuedDuelDispatchContext").FieldType);
        builder.AppendLine(RequireField(duel, "_openingDuelDispatchContext").FieldType);
        builder.AppendLine(RequireField(duel, "_meetingPendingDuelDispatchContext").FieldType);
        foreach ((string Type, string Method) method in new[]
        {
            ("AnimusForge.Refactor.Adapters.LegacyNativeActionPlanExecutor", "CreateRequestBoundDuelExecutor"),
            (RuntimeNamespace + "InteractionResultCommitter", "BuildCanonicalRequestId"),
            (RuntimeNamespace + "InteractionResultCommitter", "BuildCanonicalActionPlanFingerprint")
        })
        {
            foreach (MetadataAssembly.MethodView view in assembly.FindMethods(method.Type, method.Method)
                         .OrderBy(item => item.DisplaySignature, StringComparer.Ordinal))
            {
                builder.AppendLine(view.DisplaySignature);
            }
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static bool IsOwnerOrReadbackCall(MetadataAssembly.MethodCallView call)
    {
        return call.DeclaringType == OwnerType
               || (call.DeclaringType == DuelBehaviorType
                   && (call.Name == "TryReadDuelOutcome"
                       || call.Name == "TryFinalizeDuelOutcome"
                       || call.Name == "MarkDuelOutcomeUnknown"));
    }

    private static void RequireCall(
        MetadataAssembly assembly,
        MetadataAssembly.MethodView source,
        string declaringType,
        string methodName)
    {
        Require(assembly.CallsTransitively(
                source,
                call => call.DeclaringType == declaringType && call.Name == methodName,
                out IReadOnlyList<string> path),
            source.DisplaySignature + " does not call " + declaringType + "::" + methodName
            + ". Direct calls: " + string.Join(" | ", assembly.GetDirectCalls(source)
                .Select(call => call.DisplaySignature)));
        Console.WriteLine("ROUTE " + string.Join(" -> ", path));
    }

    private static void AssertStaticMethod(MetadataAssembly.MethodView method, string returnType)
    {
        Require(method.IsStatic && method.ReturnType == returnType,
            "Static method ABI drifted: " + method.DisplaySignature);
    }

    private static void AssertInstanceMethod(MetadataAssembly.MethodView method, string returnType)
    {
        Require(!method.IsStatic && method.ReturnType == returnType,
            "Instance method ABI drifted: " + method.DisplaySignature);
    }

    private static MetadataAssembly.FieldView RequireField(MetadataAssembly.TypeView type, string fieldName)
    {
        MetadataAssembly.FieldView[] matches = type.Fields.Where(field => field.Name == fieldName).ToArray();
        Require(matches.Length == 1, "Expected one field " + type.FullName + "::" + fieldName + "; found " + matches.Length + ".");
        return matches[0];
    }

    private static int FindCallIndex(
        IReadOnlyList<MetadataAssembly.MethodCallView> calls,
        Func<MetadataAssembly.MethodCallView, bool> predicate)
    {
        for (int index = 0; index < calls.Count; index++)
        {
            if (predicate(calls[index]))
            {
                return index;
            }
        }
        return -1;
    }

    private static void AssertCallOrder(
        MetadataAssembly assembly,
        MetadataAssembly.MethodView method,
        Func<MetadataAssembly.MethodCallView, bool> first,
        Func<MetadataAssembly.MethodCallView, bool> second,
        string message)
    {
        IReadOnlyList<MetadataAssembly.MethodCallView> calls = assembly.GetDirectCalls(method);
        int firstIndex = FindCallIndex(calls, first);
        int secondIndex = FindCallIndex(calls, second);
        Require(firstIndex >= 0 && secondIndex > firstIndex,
            message + " Direct calls: " + string.Join(" | ", calls.Select(call => call.DisplaySignature)));
    }

    private static IReadOnlyDictionary<string, int> Values(params (string Name, int Value)[] values)
    {
        return values.ToDictionary(pair => pair.Name, pair => pair.Value, StringComparer.Ordinal);
    }

    private static void AssertDictionaryEqual(
        IReadOnlyDictionary<string, int> expected,
        IReadOnlyDictionary<string, int> actual,
        string message)
    {
        bool equal = expected.Count == actual.Count
                     && expected.All(pair => actual.TryGetValue(pair.Key, out int value) && value == pair.Value);
        Require(equal,
            message + ". expected=" + FormatDictionary(expected) + " actual=" + FormatDictionary(actual));
    }

    private static string FormatDictionary(IReadOnlyDictionary<string, int> values)
    {
        return string.Join(",", values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Key + "=" + pair.Value));
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Require(start >= 0, "Source method not found: " + signature);
        return ExtractBlockAt(source, start, signature);
    }

    private static string ExtractBlockAfter(string source, string marker)
    {
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Require(start >= 0, "Source block marker not found: " + marker);
        return ExtractBlockAt(source, start, marker);
    }

    private static string ExtractBlockAt(string source, int start, string description)
    {
        int brace = source.IndexOf('{', start);
        Require(brace >= 0, "Source block body not found: " + description);
        int depth = 0;
        for (int index = brace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}' && --depth == 0)
            {
                return source.Substring(start, index - start + 1);
            }
        }
        throw new InvalidOperationException("Unterminated source block: " + description);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int cursor = 0;
        while ((cursor = source.IndexOf(value, cursor, StringComparison.Ordinal)) >= 0)
        {
            count++;
            cursor += value.Length;
        }
        return count;
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record BuildMarker(
        int SchemaVersion,
        string Role,
        string FileName,
        string AssemblyName,
        string BannerlordApi,
        string BuildFlavor,
        string ReferenceGameVersion,
        string Sha256,
        DateTimeOffset CreatedUtc);

    private sealed record VariantEvidence(string Api, string Sha256, Guid ModuleVersionId, string Fingerprint);

    private sealed class ReplayOptions
    {
        private ReplayOptions(string projectRoot, string configuration)
        {
            ProjectRoot = projectRoot;
            Configuration = configuration;
        }

        internal string ProjectRoot { get; }
        internal string Configuration { get; }

        internal static ReplayOptions Parse(string[] args)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            string configuration = "Debug";
            for (int index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--project-root":
                        projectRoot = Path.GetFullPath(RequireOptionValue(args, ref index));
                        break;
                    case "--configuration":
                        configuration = RequireOptionValue(args, ref index);
                        break;
                    default:
                        throw new ArgumentException("Unknown option: " + args[index]);
                }
            }
            if (configuration != "Debug" && configuration != "Release")
            {
                throw new ArgumentException("Configuration must be Debug or Release.");
            }
            Require(File.Exists(Path.Combine(projectRoot, "AnimusForge.csproj")),
                "Project root does not contain AnimusForge.csproj: " + projectRoot);
            return new ReplayOptions(projectRoot, configuration);
        }

        private static string RequireOptionValue(string[] args, ref int index)
        {
            if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            {
                throw new ArgumentException("Missing value for " + args[index - 1]);
            }
            return args[index];
        }
    }

    private sealed class TestSuite
    {
        internal int Passed { get; private set; }
        internal int Failed { get; private set; }

        internal void Run(string name, Action test)
        {
            try
            {
                test();
                Passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                Failed++;
                Console.Error.WriteLine("FAIL " + name + " :: " + ex.Message);
            }
        }
    }
}
