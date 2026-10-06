using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

internal static class WorldWarCapTests
{
    private static object Call(Fixture f, string name, params object[] args)
        => typeof(KingdomCivilWarOwner).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Owner, args);

    private static Fixture Prewar(bool playerLeader = false)
    {
        var f = new Fixture();
        ChangeKingdomAction.Move(f.Leader, f.Home); ChangeKingdomAction.Move(f.Follower, f.Home);
        f.Faction.Stage = KingdomCivilWarStage.FactionFormed; f.Faction.RebelKingdomId = "";
        f.Faction.ResolutionOutcomeId = ""; f.Faction.WarClanIds.Clear();
        f.Faction.UltimatumDay = 700; f.State.CooldownUntilDay = f.State.CooldownUntilWeek = 0;
        if (playerLeader) f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Reload(); return f;
    }

    private static void Occupy(Fixture f, int count)
    {
        var state = new KingdomCivilWarKingdomState { KingdomId = "other_home" };
        for (int i = 0; i < count; i++) state.Factions.Add(new()
        { Id = "other_" + i, DemandId = "redress", Stage = KingdomCivilWarStage.OpenWar, LeaderClanId = f.Leader.StringId });
        f.Owner.Storage.Kingdoms[state.KingdomId] = state;
        f.Reload();
    }

    private static void Open(Fixture f, bool player = false)
        => Call(f, "OpenWar", f.Home, f.State, f.Faction, CivilWarWorld.FindClan(f.Faction.LeaderClanId), 100,
            DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)null, player);

    private static CivilWarActionRequest Request(Fixture f, CivilWarAction action) => new()
    { OperationId = Guid.NewGuid().ToString("N"), KingdomId = f.Home.StringId, FactionId = f.Faction.Id, Action = action };

    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string text) { checks++; if (!value) throw new Exception(text); }
        var tuning = new CivilWarTuning();
        Check(tuning.WorldMaxWars == 3 && tuning.PlayerAnswerWeeks == 4, "new defaults");
        Check(CivilWarFactionRules.HasWorldWarSlot(2, tuning) && !CivilWarFactionRules.HasWorldWarSlot(3, tuning), "cap boundary");
        Check(CivilWarFactionRules.HasWorldWarSlot(20, tuning, true), "player exempt even above cap");
        tuning.WorldMaxWars = 0; Check(!CivilWarFactionRules.HasWorldWarSlot(1, tuning), "invalid cap clamps to one");
        tuning.WorldMaxWars = 99; Check(!CivilWarFactionRules.HasWorldWarSlot(20, tuning), "cap clamps to twenty");

        var f = Prewar(); Occupy(f, 2); Open(f);
        Check(f.Owner.WorldOpenWarCount == 3 && f.Faction.Stage == KingdomCivilWarStage.OpenWar && f.Faction.RebelKingdomId == "", "pending creation reserves last slot immediately");
        var second = new KingdomCivilWarFactionState { Id = "same_round", DemandId = "redress", LeaderClanId = f.Follower.StringId };
        var concurrent = DuelSettings.BuildCivilWarTuning(); concurrent.AllowConcurrentWars = true;
        f.State.Factions.Add(second);
        Call(f, "OpenWar", f.Home, f.State, second, f.Follower, 100, concurrent, (Action<Kingdom, int>)null, false);
        Check(second.WaitingForOtherWar && second.Stage == KingdomCivilWarStage.FactionFormed && f.Owner.WorldOpenWarCount == 3, "same-round second opening cannot use reserved slot even with domestic concurrency enabled");
        f.State.Factions.Remove(second);
        Open(f); Check(f.Owner.WorldOpenWarCount == 3, "duplicate opening cannot double count");
        f.Reload(); Check(f.Owner.WorldOpenWarCount == 3, "reload reconstructs pending reservations");
        DuelSettings.WorldMaxWars = 1;
        Check(f.Faction.Stage == KingdomCivilWarStage.OpenWar && f.Owner.WorldOpenWarCount == 3, "lowering cap preserves existing wars");
        f.Owner.NotifyRebellionFailed(f.Faction.Id, "test");
        Check(f.Owner.WorldOpenWarCount == 2 && f.Faction.Stage == KingdomCivilWarStage.FactionFormed, "failed creation releases reservation");
        f.Owner.NotifyRebellionFailed(f.Faction.Id, "duplicate");
        Check(f.Owner.WorldOpenWarCount == 2, "duplicate failure cannot release another slot");

        f = Prewar(); Occupy(f, 3); int cooldown = f.State.CooldownUntilDay;
        Open(f);
        Check(f.Faction.WaitingForOtherWar && f.Faction.Stage == KingdomCivilWarStage.FactionFormed && f.Owner.WorldOpenWarCount == 3, "automatic escalation waits at cap");
        Check(f.State.CooldownUntilDay == cooldown && f.Faction.WarClanIds.Count == 0, "blocked opening has no war initialization effects");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.Detonate), f.Leader).Allowed, "NPC command quote also respects cap");
        f.Reload(); Check(f.Faction.WaitingForOtherWar, "waiting persists across load");
        Call(f, "AdvanceUltimatum", f.Home, f.State, f.Faction, 100, 50, DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)null);
        Check(f.Faction.WaitingForOtherWar && f.Owner.WorldOpenWarCount == 3, "daily advance retains wait while full");
        Call(f, "DropKingdom", "other_home");
        Check(f.Owner.WorldOpenWarCount == 0, "kingdom removal releases all its reservations");
        Call(f, "AdvanceUltimatum", f.Home, f.State, f.Faction, 100, 50, DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)null);
        Check(f.Faction.Stage == KingdomCivilWarStage.OpenWar && !f.Faction.WaitingForOtherWar && f.Owner.WorldOpenWarCount == 1, "waiting uprising resumes when slot released");

        f = Prewar(); Occupy(f, 3); f.Faction.Refusals = 1;
        Call(f, "ApplyRuling", f.Home, f.State, f.Faction, CivilWarCatalog.FindDemand("redress"), f.Leader, 100,
            DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)null, new CivilWarUltimatumResult { Ruling = CivilWarRuling.Refuse, Escalate = true });
        Check(f.Faction.Refusals == 2 && f.Faction.WaitingForOtherWar && f.Owner.WorldOpenWarCount == 3, "ultimatum refusal escalation reaches global gate");
        Check(f.State.Clans[f.Leader.StringId].Grievance["demand_refused"] == 10 && f.State.Clans[f.Follower.StringId].Grievance["demand_refused"] == 4, "refusal additions halved exactly once");

        f = Prewar(); Occupy(f, 3); f.Faction.Refusals = 2; f.Faction.EscalationPending = true;
        f.Faction.LastEscalationDay = -1; f.Faction.UltimatumDay = 2000; f.Faction.DemandLocked = true;
        TaleWorlds.Core.MBRandom.Value = 0;
        f.Tick(100);
        Check(f.Faction.EscalationPending && f.Faction.Stage != KingdomCivilWarStage.OpenWar, "event escalation retains pending trigger while full");
        Call(f, "DropKingdom", "other_home"); f.Tick(101);
        Check(f.Faction.Stage == KingdomCivilWarStage.OpenWar && f.Owner.WorldOpenWarCount == 1, "event escalation retries after capacity is released");

        f = Prewar(); Occupy(f, 3);
        Check(f.Owner.Quote(Request(f, CivilWarAction.Detonate), Clan.PlayerClan).Allowed, "player proposal ignores cap");
        Check(f.Owner.Execute(Request(f, CivilWarAction.Detonate), Clan.PlayerClan, true).Status == CivilWarActionStatus.AwaitingKingdom && f.Owner.WorldOpenWarCount == 4, "agreed player proposal exceeds cap and counts");
        f = Prewar(true); Occupy(f, 3);
        Check(f.Owner.Execute(Request(f, CivilWarAction.Detonate), Clan.PlayerClan).Status == CivilWarActionStatus.AwaitingKingdom && f.Owner.WorldOpenWarCount == 4, "player leader can open above cap");
        f = Prewar(true); Occupy(f, 3); PlayerKingdomRebellionImmunity.Protected = true;
        Check(!f.Owner.Quote(Request(f, CivilWarAction.Detonate), Clan.PlayerClan).Allowed, "player exemption does not bypass immunity");

        f = Prewar(true); Occupy(f, 3);
        f.Faction.PendingResponse = new CivilWarPendingResponse { OperationId = "dissolve", RulerClanId = f.Home.RulingClan.StringId,
            LeaderClanId = Clan.PlayerClan.StringId, Dissolve = true, DeadlineDay = 703 };
        var response = Request(f, CivilWarAction.Respond); response.Accept = false;
        Check(f.Owner.Execute(response, Clan.PlayerClan).Status == CivilWarActionStatus.AwaitingKingdom && f.Owner.WorldOpenWarCount == 4, "player defiance also exempt");

        f = new Fixture(); Check(f.Owner.WorldOpenWarCount == 1, "live war loaded");
        MakePeaceAction.Fail = true; f.Tick(101);
        Check(f.Owner.WorldOpenWarCount == 1, "failed settlement retains slot");
        MakePeaceAction.Fail = false; f.Tick(102);
        Check(f.Owner.WorldOpenWarCount == 0, "successful settlement releases slot");

        f = new Fixture(); f.State.Factions.Clear(); f.Reload();
        var registration = new CoupCivilWarRegistration { CoupId = "cap_test", KingdomId = f.Home.StringId,
            RebelKingdomId = f.Rebel.StringId, LeaderClanId = f.Leader.StringId };
        Occupy(f, 3);
        Check(f.Owner.TryRegisterCoupWar(registration, out _) && f.Owner.WorldOpenWarCount == 4, "already-created coup accepted above cap and counted");
        Check(f.Owner.TryRegisterCoupWar(registration, out _) && f.Owner.WorldOpenWarCount == 4, "duplicate coup registration does not count twice");

        f = Prewar(); f.Home.RulingClan = Clan.PlayerClan;
        Call(f, "AdvanceUltimatum", f.Home, f.State, f.Faction, 100, 50, DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)null);
        Check(f.Faction.PlayerAnswerPending && f.Faction.AnswerDeadlineDay == 728 && f.Faction.PlayerAnswerDeadlineWeek == 104, "new ultimatum has 28 day answer window");
        f.Faction.AnswerDeadlineDay = 714; f.Faction.PlayerAnswerDeadlineWeek = 102; f.Reload();
        Check(f.Faction.AnswerDeadlineDay == 714 && f.Faction.PlayerAnswerDeadlineWeek == 102, "existing ultimatum not migrated");
        f.Owner.Replace(null); Check(f.Owner.WorldOpenWarCount == 0, "campaign reset clears index");

        f = Prewar(); var record = f.State.Clans[f.Follower.StringId];
        foreach (var source in CivilWarCatalog.Sources)
        {
            f.Owner.AddGrievance(f.Home, source.Id, new[] { f.Follower }, 3, 100, source.Name);
            Check(record.Grievance[source.Id] == 1.5f, "fractional half grievance: " + source.Id);
        }
        f.Reload();
        Check(f.State.Clans[f.Follower.StringId].Grievance.Values.All(x => x == 1.5f), "load never halves stored grievance again");
        return checks;
    }
}
