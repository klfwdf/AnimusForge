using System;
using System.IO;

internal static class J13E1DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13E1 owner contract: " + label);
        }

        string campaign = Read("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string ticks = Read("src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs");
        string patches = Read("src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs");
        string host = Read("DuelBehavior.cs");
        string outcomeHost = Read("DuelBehavior.Outcomes.cs");
        string dispatch = Read("src/modules/AF.Module.Duel/DuelBehavior.DispatchOwner.cs");
        string settlement = Read("src/modules/AF.Module.Duel/DuelSettlementEffectOwner.cs");
        string courier = Read("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs");

        Require(campaign.Contains("campaignGameStarter.AddBehavior(new DuelBehavior())", StringComparison.Ordinal)
            && ticks.Contains("DuelBehavior.Instance?.OnEngineTick()", StringComparison.Ordinal)
            && patches.Contains("DuelBehavior.RegisterHarmonyPatches(harmony)", StringComparison.Ordinal)
            && patches.Contains("FourberieDuelCompatibility.EnsurePatched(harmony)", StringComparison.Ordinal)
            && host.Contains("public override void SyncData(IDataStore dataStore)", StringComparison.Ordinal),
            "original Campaign, tick, Harmony and save adapters are not wired");
        Require(host.Contains("PatchHarmonyClass(harmony, typeof(WildernessDuelMapEventResultsPatch))", StringComparison.Ordinal)
            && host.Contains("PatchHarmonyClass(harmony, typeof(WildernessDuelBattleRewardsZeroPatch))", StringComparison.Ordinal)
            && host.Contains("[HarmonyPatch(typeof(MapEvent), \"CalculateAndCommitMapEventResults\")]", StringComparison.Ordinal)
            && host.Contains("[HarmonyPatch(typeof(PlayerEncounter), \"GetBattleRewards\")]", StringComparison.Ordinal)
            && host.Contains("return !DuelBehavior.TryHandleWildernessDuelMapEventResults(__instance", StringComparison.Ordinal)
            && host.Contains("return !DuelBehavior.ShouldZeroBattleRewardsForWildernessDuel(", StringComparison.Ordinal)
            && host.Contains("if (!IsWildernessDuelMapEvent(mapEvent))", StringComparison.Ordinal)
            && host.Contains("#if BANNERLORD_1_4_OR_GREATER", StringComparison.Ordinal),
            "wilderness-only patch targets, default continuation or dual reward signatures drifted");
        Require(dispatch.Contains("private sealed class DuelBehaviorDetachedDispatchOwner : IDetachedDuelDispatchOwner", StringComparison.Ordinal)
            && dispatch.Contains("_duelOutcomeOwner.Queue(", StringComparison.Ordinal)
            && dispatch.Contains("ExactDuelDispatchSeenCapacity", StringComparison.Ordinal)
            && dispatch.Contains("IsDetachedDuelDispatchReadyForDelayedHost(", StringComparison.Ordinal)
            && dispatch.Contains("receipt.HostAccepted", StringComparison.Ordinal)
            && dispatch.Contains("MarkUnknownAfterStart(", StringComparison.Ordinal)
            && dispatch.Contains("IsSceneDuelBridgeEnabled()", StringComparison.Ordinal)
            && !outcomeHost.Contains("class DuelBehaviorDetachedDispatchOwner", StringComparison.Ordinal),
            "Duel domain does not own exact admission and delayed/abort transitions");
        Require(outcomeHost.Contains("DuelSettlementEffectOwner.TryCreate(", StringComparison.Ordinal)
            && settlement.Contains("kind == DuelSessionKind.Wilderness && hasNonHeroMemory", StringComparison.Ordinal)
            && settlement.Contains("DuelOutcomeEffects.TryCreate(", StringComparison.Ordinal)
            && host.Contains("TryRecordDuelOutcome(", StringComparison.Ordinal)
            && host.Contains("TryFinalizeDuelOutcome(", StringComparison.Ordinal)
            && courier.Contains("RejectDetachedDuelDispatchForExternal(", StringComparison.Ordinal)
            && courier.Contains("\"unsupported_channel\"", StringComparison.Ordinal),
            "three terminal writers, typed effects or Courier exclusion are disconnected");

        Console.WriteLine("PASS J13E1DomainOwnerContractReplay campaign/tick/patch=retained dispatch=domain settlement=typed courier=rejected; source-wiring-only live=NOT_RUN");
    }
}
