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
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
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

string heroSource = File.ReadAllText(Path.Combine(projectRoot, "RewardSystemBehavior.EconomyReplay.cs"));
string partySource = File.ReadAllText(Path.Combine(projectRoot, "RewardSystemBehavior.EconomyPartyReplay.cs"));
string merchantSource = File.ReadAllText(Path.Combine(projectRoot, "RewardSystemBehavior.EconomyMerchantReplay.cs"));
string ownerSource = File.ReadAllText(Path.Combine(projectRoot, "RewardSystemBehavior.cs"));

AssertOwnerReplayUncertaintyContract(
    heroSource,
    "private EconomyRewardDebtReplayResult ReplayEconomyRewardDebtPlanOnMainThread(",
    "TryReplayAction(",
    "confirmedFacts.Add(new FactRecord(",
    "confirmedFacts",
    "economy.unknown_after_start",
    "Hero");
AssertOwnerReplayUncertaintyContract(
    partySource,
    "private EconomyRewardDebtReplayResult ReplayPartyEconomyPlanOnMainThread(",
    "TryReplayPartyAction(",
    "facts.Add(new FactRecord(",
    "facts",
    "economy.party_unknown_after_start",
    "Party");
AssertOwnerReplayUncertaintyContract(
    merchantSource,
    "private EconomyRewardDebtReplayResult ReplayMerchantEconomyPlanOnMainThread(",
    "TryReplayMerchantAction(",
    "facts.Add(new FactRecord(",
    "facts",
    "economy.merchant_unknown_after_start",
    "Merchant");

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
AssertTrue(CountOccurrences(heroSource, "TransferItemByIdForEconomyReplay(") == 2
    && heroSource.Contains("if (mutationObservation.UnknownAfterStart)", StringComparison.Ordinal),
    "Hero finite/ALL item paths do not both propagate swallowed mutation uncertainty");
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

Console.WriteLine("PASS productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 merchantFactoryFailClosed=1 ownerUnknownAfterStart=3 swallowedMutationUnknown=7 replayAwareHelpers=3 propagationChains=4 factFailureIsolated=3 logFailureIsolated=1 productionType=1 noCampaignMutation=1");

static void AssertOwnerReplayUncertaintyContract(
    string source,
    string replaySignature,
    string actionCallMarker,
    string factAddMarker,
    string factsVariable,
    string unknownErrorCode,
    string ownerName)
{
    string replay = ExtractMethod(source, replaySignature);
    string actionCatch = ExtractCatchAfter(replay, actionCallMarker);
    string factCatch = ExtractCatchAfter(replay, factAddMarker);
    string unknownReturn = ExtractBlockAfter(replay, "if (unknownAfterStart)");
    string observedUnknown = ExtractBlockAfter(replay, "if (mutationObservation.UnknownAfterStart)");

    int actionCall = replay.IndexOf(actionCallMarker, StringComparison.Ordinal);
    int appliedIncrement = replay.IndexOf("appliedCount++;", actionCall, StringComparison.Ordinal);
    int factAdd = replay.IndexOf(factAddMarker, appliedIncrement, StringComparison.Ordinal);
    int unknownBranch = replay.IndexOf("if (unknownAfterStart)", factAdd, StringComparison.Ordinal);

    AssertTrue(actionCatch.Contains("unknownAfterStart = true;", StringComparison.Ordinal)
        && actionCatch.Contains("LogEconomyReplayFailureSafe(", StringComparison.Ordinal)
        && actionCatch.Contains("break;", StringComparison.Ordinal),
        ownerName + " action exceptions must become unknown-after-start and stop later actions");
    AssertTrue(appliedIncrement > actionCall && factAdd > appliedIncrement && unknownBranch > factAdd,
        ownerName + " must retain the known applied count before optional fact materialization");
    AssertTrue(unknownReturn.Contains("EconomyRewardDebtReplayStatus.UnknownAfterStart", StringComparison.Ordinal)
        && unknownReturn.Contains("appliedCount,", StringComparison.Ordinal)
        && unknownReturn.Contains(factsVariable + ",", StringComparison.Ordinal)
        && unknownReturn.Contains(unknownErrorCode, StringComparison.Ordinal),
        ownerName + " unknown result must preserve prior count and confirmed facts");
    AssertTrue(factCatch.Contains("LogEconomyReplayFailureSafe(", StringComparison.Ordinal)
        && !factCatch.Contains("unknownAfterStart = true;", StringComparison.Ordinal)
        && !factCatch.Contains("break;", StringComparison.Ordinal)
        && !factCatch.Contains("return ", StringComparison.Ordinal)
        && replay.Contains("EconomyMutationObservation mutationObservation", StringComparison.Ordinal)
        && CountOccurrences(replay, "unknownAfterStart = true;") == 2
        && !replay.Contains("Logger.Log(", StringComparison.Ordinal),
        ownerName + " fact/diagnostic failures must not be reported as unknown gameplay effects");
    AssertTrue(observedUnknown.Contains("unknownAfterStart = true;", StringComparison.Ordinal)
        && observedUnknown.Contains("break;", StringComparison.Ordinal),
        ownerName + " swallowed mutation uncertainty must stop later actions");
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
