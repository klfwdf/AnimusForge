using System;
using System.IO;
using System.Linq;
using System.Reflection;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

string stageDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_stage", "AnimusForge",
    "bin", "Win64_Shipping_Client"));
string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string referenceDirectory = Path.Combine(projectRoot, ".tmp", "build_check", "1.4");
string implementationOverride = Environment.GetEnvironmentVariable("AF_IMPLEMENTATION_PATH");
string implementationPath = string.IsNullOrWhiteSpace(implementationOverride)
    ? Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll")
    : Path.GetFullPath(implementationOverride);
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory, referenceDirectory })
    {
        if (!Directory.Exists(root))
        {
            continue;
        }
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try
            {
                return Assembly.LoadFrom(candidate);
            }
            catch
            {
            }
        }
    }
    return null;
};

Assembly implementation = Assembly.LoadFrom(implementationPath);
Type rewardType = implementation.GetType("AnimusForge.RewardSystemBehavior", true);
Type portType = implementation.GetType("AnimusForge.Refactor.Adapters.LegacyEconomyRewardDebtMainThreadPort", true);
MethodInfo factory = rewardType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateEconomyRewardDebtMainThreadPortForExternal"
        && method.GetParameters().Length == 0);
object port = factory.Invoke(null, null);
AssertTrue(port == null, "economy owner factory created a port without a live Campaign/RewardSystem owner");
AssertTrue(portType.IsAssignableFrom(factory.ReturnType), "economy owner factory return type drifted");

MethodInfo partyFactory = rewardType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreatePartyEconomyRewardDebtMainThreadPortForExternal");
object partyPort = partyFactory.Invoke(null, new object[] { null, null, "party-subject", null });
AssertTrue(partyPort == null, "party economy owner factory created a port without a live Campaign/party owner");
AssertTrue(portType.IsAssignableFrom(partyFactory.ReturnType), "party economy owner factory return type drifted");

MethodInfo merchantFactory = rewardType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateMerchantEconomyRewardDebtMainThreadPortForExternal");
object merchantPort = merchantFactory.Invoke(null, new object[] { null, null, "merchant-subject", null });
AssertTrue(merchantPort == null, "merchant economy owner factory created a port without a live Campaign/merchant owner");
AssertTrue(portType.IsAssignableFrom(merchantFactory.ReturnType), "merchant economy owner factory return type drifted");

string heroSource = File.ReadAllText(Path.Combine(projectRoot, "src/modules/AF.Module.Economy/Execution/Hero/RewardSystemBehavior.EconomyReplay.cs"));
string partySource = File.ReadAllText(Path.Combine(projectRoot, "src/modules/AF.Module.Economy/Execution/Party/RewardSystemBehavior.EconomyPartyReplay.cs"));
string merchantSource = File.ReadAllText(Path.Combine(projectRoot, "src/modules/AF.Module.Economy/Execution/Merchant/RewardSystemBehavior.EconomyMerchantReplay.cs"));
string coordinatorSource = File.ReadAllText(Path.Combine(projectRoot, "src/modules/AF.Module.Economy/Execution/EconomyReplayBatchCoordinator.cs"));
string ownerSource = File.ReadAllText(Path.Combine(projectRoot, "RewardSystemBehavior.cs"));
string authorizationSource = File.ReadAllText(Path.Combine(projectRoot, "src/modules/AF.Module.Economy/Authorization/RewardSystemBehavior.EconomyAssetAuthorization.cs"));

AssertOwnerReplayUncertaintyContract(
    heroSource,
    "private EconomyRewardDebtReplayResult ReplayEconomyRewardDebtPlanOnMainThread(",
    "private EconomyReplayStepOutcome ExecuteHeroEconomyStep(",
    "TryReplayAction(",
    "confirmedFacts.Add(new FactRecord(",
    "economy.unknown_after_start",
    "Hero");
AssertOwnerReplayUncertaintyContract(
    partySource,
    "private EconomyRewardDebtReplayResult ReplayPartyEconomyPlanOnMainThread(",
    "private EconomyReplayStepOutcome ExecutePartyEconomyStep(",
    "TryReplayPartyAction(",
    "facts.Add(new FactRecord(",
    "economy.party_unknown_after_start",
    "Party");
AssertOwnerReplayUncertaintyContract(
    merchantSource,
    "private EconomyRewardDebtReplayResult ReplayMerchantEconomyPlanOnMainThread(",
    "private EconomyReplayStepOutcome ExecuteMerchantEconomyStep(",
    "TryReplayMerchantAction(",
    "facts.Add(new FactRecord(",
    "economy.merchant_unknown_after_start",
    "Merchant");
AssertBatchCoordinatorContract(coordinatorSource);

string safeLog = ExtractMethod(heroSource, "private static void LogEconomyReplayFailureSafe(");
string safeLogCatch = ExtractCatchAfter(safeLog, "Logger.Log(");
AssertTrue(safeLog.Contains("try", StringComparison.Ordinal)
    && !safeLogCatch.Contains("throw", StringComparison.Ordinal)
    && !safeLogCatch.Contains("unknownAfterStart", StringComparison.Ordinal),
    "economy replay diagnostics must swallow logging failures without changing gameplay certainty");

foreach (string marker in new[]
{
    "economy.roster_add_exception",
    "economy.generated_item_exception",
    "economy.equipment_restore_queue_exception",
    "economy.equipment_restore_rollback_exception",
    "economy.equipment_restore_rollback_unverified",
    "economy.settlement_transfer_entry_exception",
    "economy.settlement_transfer_exception"
})
{
    AssertTrue(ownerSource.Contains("MarkUnknown(\"" + marker + "\")", StringComparison.Ordinal),
        "swallowed mutation exception is not surfaced: " + marker);
}
foreach (string replayAwareHelper in new[]
{
    "TransferItemByIdForEconomyReplay(",
    "TransferItemFromPartyForEconomyReplay(",
    "TransferItemFromSettlementForEconomyReplay("
})
{
    AssertTrue(ownerSource.Contains(replayAwareHelper, StringComparison.Ordinal),
        "replay-aware mutation helper is missing: " + replayAwareHelper);
}
string heroAssetReplay = ExtractMethod(heroSource, "private bool TryReplayGiveAsset(");
AssertTrue(heroAssetReplay.Contains("TryResolveAuthorizedHeroRewardItem(", StringComparison.Ordinal)
    && heroAssetReplay.Contains("ResolveAllRewardItemAmount(lookup, authorizedItems)", StringComparison.Ordinal)
    && authorizationSource.Contains("private bool TryResolveAuthorizedHeroRewardItem(", StringComparison.Ordinal)
    && heroAssetReplay.Contains("forceComplete: !quantity.IsAll", StringComparison.Ordinal)
    && heroAssetReplay.Contains("TransferItemByIdForEconomyReplay(", StringComparison.Ordinal)
    && heroAssetReplay.Contains("GenerateRpAssetToPlayer(", StringComparison.Ordinal)
    && heroAssetReplay.Contains("TransferItemFromSettlementForEconomyReplay(", StringComparison.Ordinal)
    && heroAssetReplay.Contains("TryParseNotableMarketPromptStringId(", StringComparison.Ordinal)
    && CountOccurrences(heroAssetReplay, "mutationObservation: mutationObservation") == 3
    && heroSource.Contains("if (mutationObservation.UnknownAfterStart)", StringComparison.Ordinal),
    "Hero scoped finite/ALL and RP paths must preserve authority and swallowed mutation observation");
string heroGoldReplay = ExtractMethod(heroSource, "private bool TryReplayGiveGold(");
AssertTrue(heroGoldReplay.Contains("ResolveNotableMarketSettlement(giver)", StringComparison.Ordinal)
    && heroGoldReplay.Contains("IsNotableMarketHero(giver, market)", StringComparison.Ordinal)
    && heroGoldReplay.Contains("TransferGoldFromSettlement(", StringComparison.Ordinal)
    && heroGoldReplay.Contains("TransferGold(giver, receiver", StringComparison.Ordinal),
    "Hero gold must preserve notable market versus personal ownership");
AssertTrue(partySource.Contains("TransferItemFromPartyForEconomyReplay(", StringComparison.Ordinal)
    && partySource.Contains("mutationObservation: mutationObservation", StringComparison.Ordinal),
    "Party item/RP path does not propagate mutation observation");
AssertTrue(merchantSource.Contains("TransferItemFromSettlementForEconomyReplay(", StringComparison.Ordinal)
    && merchantSource.Contains("mutationObservation: mutationObservation", StringComparison.Ordinal),
    "Merchant item/RP path does not propagate mutation observation");

string heroItemCore = ExtractMethod(ownerSource, "private int TransferItemByIdCore(");
string partyItemCore = ExtractMethod(ownerSource, "private int TransferItemFromPartyCore(");
string merchantItemCore = ExtractMethod(ownerSource, "private int TransferItemFromSettlementCore(");
string namedItemCore = ExtractMethod(ownerSource, "private static int GenerateNamedInventoryItemToRosterForExternal(");
string settlementCore = ExtractMethod(ownerSource,
    "private bool TryApplySettlementTransferAction(Hero giver, Hero receiver, string directionToken, string settlementToken, IDictionary<string, FixedAssetTokenResolution> fixedAssetResolutionCache, ISet<string> unresolvedFixedAssetTokens, out MyBehavior.SettlementTransferPromptEntry authorizedEntry, out string statusText, EconomyMutationObservation mutationObservation)");
AssertTrue(heroItemCore.Contains("mutationObservation", StringComparison.Ordinal)
    && partyItemCore.Contains("mutationObservation", StringComparison.Ordinal)
    && merchantItemCore.Contains("mutationObservation", StringComparison.Ordinal)
    && namedItemCore.Contains("mutationObservation?.MarkUnknown", StringComparison.Ordinal)
    && settlementCore.Contains("mutationObservation", StringComparison.Ordinal),
    "replay-aware helper core dropped the structured mutation observation");

Console.WriteLine("PASS productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 merchantFactoryFailClosed=1 batchOwner=1 ownerUnknownAfterStart=3 swallowedMutationUnknown=7 replayAwareHelpers=3 propagationChains=4 factFailureIsolated=3 logFailureIsolated=1 productionType=1 noCampaignMutation=1");

static void AssertOwnerReplayUncertaintyContract(
    string source,
    string replaySignature,
    string stepSignature,
    string actionCallMarker,
    string factAddMarker,
    string unknownErrorCode,
    string ownerName)
{
    string replay = ExtractMethod(source, replaySignature);
    string step = ExtractMethod(source, stepSignature);
    string actionCatch = ExtractCatchAfter(step, actionCallMarker);
    string factCatch = ExtractCatchAfter(replay, factAddMarker);
    string observedUnknown = ExtractBlockAfter(step, "if (mutationObservation.UnknownAfterStart)");

    AssertTrue(replay.Contains("EconomyReplayBatchCoordinator.Execute(", StringComparison.Ordinal)
        && replay.Contains("EconomyReplayBatchCoordinator.Complete(", StringComparison.Ordinal)
        && replay.Contains("foreach (EconomyReplayAppliedFact fact in outcome.Facts)", StringComparison.Ordinal)
        && replay.Contains(factAddMarker, StringComparison.Ordinal)
        && replay.Contains(unknownErrorCode, StringComparison.Ordinal),
        ownerName + " replay must use the shared batch owner and preserve domain terminal codes");
    AssertTrue(actionCatch.Contains("EconomyReplayStepOutcome.Unknown(", StringComparison.Ordinal)
        && actionCatch.Contains("LogEconomyReplayFailureSafe(", StringComparison.Ordinal)
        && !actionCatch.Contains("return false", StringComparison.Ordinal),
        ownerName + " action exceptions must become terminal unknown");
    AssertTrue(factCatch.Contains("LogEconomyReplayFailureSafe(", StringComparison.Ordinal)
        && !factCatch.Contains("EconomyReplayStepOutcome.Unknown", StringComparison.Ordinal)
        && !factCatch.Contains("throw", StringComparison.Ordinal),
        ownerName + " fact/diagnostic failures must not be reported as unknown gameplay effects");
    AssertTrue(observedUnknown.Contains("EconomyReplayStepOutcome.Unknown(", StringComparison.Ordinal)
        && observedUnknown.Contains("mutationObservation.ErrorCode", StringComparison.Ordinal),
        ownerName + " swallowed mutation uncertainty must stop later actions");
    AssertTrue(step.Contains("EconomyReplayStepOutcome.Applied(factText)", StringComparison.Ordinal)
        && step.Contains("EconomyReplayStepOutcome.Rejected()", StringComparison.Ordinal),
        ownerName + " step must distinguish applied and rejected outcomes");
}

static void AssertBatchCoordinatorContract(string source)
{
    string execute = ExtractMethod(source, "internal static EconomyReplayBatchOutcome Execute(");
    string complete = ExtractMethod(source, "internal static EconomyRewardDebtReplayResult Complete(");
    string unknown = ExtractBlockAfter(execute,
        "if (step == null || step.State == EconomyReplayStepState.UnknownAfterStart)");
    AssertTrue(execute.Contains("foreach (EconomyRewardDebtAction action", StringComparison.Ordinal)
        && execute.Contains("failedCount++;", StringComparison.Ordinal)
        && execute.Contains("appliedCount++;", StringComparison.Ordinal)
        && execute.Contains("facts.Add(new EconomyReplayAppliedFact(action, step.FactText))", StringComparison.Ordinal),
        "batch coordinator must own null/reject/apply counting and fact identity");
    AssertTrue(unknown.Contains("unknownAfterStart = true;", StringComparison.Ordinal)
        && unknown.Contains("break;", StringComparison.Ordinal),
        "batch coordinator must stop after an unknown owner outcome");
    AssertTrue(complete.Contains("EconomyRewardDebtReplayStatus.UnknownAfterStart", StringComparison.Ordinal)
        && complete.Contains("outcome.AppliedCount", StringComparison.Ordinal)
        && complete.Contains("EconomyRewardDebtReplayStatus.PartiallyApplied", StringComparison.Ordinal)
        && complete.Contains("EconomyRewardDebtReplayStatus.Applied", StringComparison.Ordinal),
        "batch coordinator must retain applied count and distinguish unknown/partial/applied");
}

static string ExtractMethod(string source, string signature)
{
    int start = source.IndexOf(signature, StringComparison.Ordinal);
    AssertTrue(start >= 0, "method not found: " + signature);
    return ExtractBlockAt(source, start, signature);
}

static string ExtractCatchAfter(string source, string precedingMarker)
{
    int marker = source.IndexOf(precedingMarker, StringComparison.Ordinal);
    AssertTrue(marker >= 0, "source marker not found: " + precedingMarker);
    int start = source.IndexOf("catch", marker + precedingMarker.Length, StringComparison.Ordinal);
    AssertTrue(start >= 0, "catch not found after: " + precedingMarker);
    return ExtractBlockAt(source, start, "catch after " + precedingMarker);
}

static string ExtractBlockAfter(string source, string marker)
{
    int start = source.IndexOf(marker, StringComparison.Ordinal);
    AssertTrue(start >= 0, "source block not found: " + marker);
    return ExtractBlockAt(source, start, marker);
}

static string ExtractBlockAt(string source, int start, string description)
{
    int brace = source.IndexOf('{', start);
    AssertTrue(brace >= 0, "source block body not found: " + description);
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
    throw new InvalidOperationException("unterminated source block: " + description);
}

static int CountOccurrences(string source, string value)
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
