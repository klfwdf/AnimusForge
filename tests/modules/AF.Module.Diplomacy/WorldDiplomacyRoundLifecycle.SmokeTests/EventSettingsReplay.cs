using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using System.Text;

internal static class EventSettingsReplay
{
    private struct WarPort : IWorldDiplomacyWarAdmissionPort
    {
        public bool ValidPair => true;
        public bool InvolvesPlayer => false;
        public bool HasIndependentAuthority => true;
        public bool AtWar => false;
        public bool Allied => false;
        public bool PendingThreatDecision => false;
        public bool BlocksNewOffensiveWar => false;
        public int CurrentDay => throw new Exception("retired clock was read");
        public int PeaceProtectionDays => throw new Exception("retired peace cooldown was read");
        public bool TryGetLastPeaceDay(out int day) => throw new Exception("retired peace receipt was read");
        public int OffensiveWarCooldownDays => throw new Exception("retired war cooldown was read");
        public bool TryGetLastOffensiveWarDay(out int day) => throw new Exception("retired war receipt was read");
        public int ActiveWars => throw new Exception("retired all-kingdom scan was read");
        public int MaxConcurrentOffensiveWars => throw new Exception("retired war cap was read");
    }
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage { LastOrdinaryRoundStartedDay = 10, NextNormalRoundDay = 11, LastAppliedRoundIntervalDays = 1 };
        WorldDiplomacyLiveRoundRules.RefreshOpeningInterval(storage, 3);
        Test.True(!WorldDiplomacyLiveRoundRules.CanStartOrdinary(storage,12,1) && WorldDiplomacyLiveRoundRules.CanStartOrdinary(storage,13,1), "interval controls ordinary admission with fixed capacity");
        WorldDiplomacyLiveRoundRules.RefreshOpeningInterval(storage, 8);
        Test.True(storage.NextNormalRoundDay==18, "increased interval anchors to last opening instead of current time");
        storage = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        WorldDiplomacyLiveRoundRules.RefreshOpeningInterval(storage, 2);
        Test.True(storage.NextNormalRoundDay==12 && WorldDiplomacyLiveRoundRules.CanStartOrdinary(storage,13,1), "reload and shrinking interval preserve elapsed time");
        storage.ActiveRound = new() { RoundId="oral", State="active", SchedulingOwner="oral", EventSystemVersion=1 };
        Test.True(WorldDiplomacyLiveRoundRules.CanStartOrdinary(storage,13,1), "oral birth lane does not consume ordinary capacity");
        storage.ActiveRound.SchedulingOwner="normal";
        Test.True(!WorldDiplomacyLiveRoundRules.CanStartOrdinary(storage,13,1), "one active ordinary event holds its fixed slot");
        var host = new FakeOrchestrationHost { EventIntervalDays=7 };
        var orchestration = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
        orchestration.ScheduleNextNormalRoundAfter(20);
        Test.True(orchestration.CurrentStorage.NextNormalRoundDay==27, "actual owner uses current interval scalar");
        orchestration.CurrentStorage.LastOrdinaryRoundStartedDay=10;
        orchestration.ScheduleNextNormalRoundAfter(20);
        Test.True(orchestration.CurrentStorage.NextNormalRoundDay==17, "closing an old event does not add another interval");
        var war = new WarPort();
        Test.True(WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref war,out _), "legal war does not read retired cooldowns or scan wars");
        Test.True(DiplomacyNativeAdmissionRules.AdjustPeaceScore(100,50)==100, "balanced native peace score is unchanged");
        Test.True(DiplomacyNativeAdmissionRules.AdjustPeaceScore(100,0)>100 && DiplomacyNativeAdmissionRules.AdjustPeaceScore(100,100)<100, "war preference moves native peace consideration in opposite directions");
        Test.True(DiplomacyNativeAdmissionRules.AdjustPeaceScore(float.MinValue,0)==float.MinValue, "native impossible-score sentinel remains impossible");
        var prompt=new StringBuilder();
        WorldDiplomacyPromptContractRules.AppendDiplomaticDeclarationWritingContract(prompt,999,300,DiplomacyPromptTexts.Get);
        Test.True(prompt.ToString().Contains("300") && !prompt.ToString().Contains("999"), "real declaration contract ignores old minimum while retaining requested upper bound");
    }
}
