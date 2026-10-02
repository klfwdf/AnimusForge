using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;

internal static class PlayerFactionControlTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Player control: " + message); }
        Fixture Prewar(bool playerLeader = true)
        {
            var fixture = new Fixture();
            ChangeKingdomAction.Move(fixture.Leader, fixture.Home); ChangeKingdomAction.Move(fixture.Follower, fixture.Home);
            fixture.Faction.Stage = KingdomCivilWarStage.FactionFormed; fixture.Faction.RebelKingdomId = ""; fixture.Faction.ResolutionOutcomeId = "";
            fixture.Faction.WarClanIds.Clear(); fixture.Faction.UltimatumDay = 900; fixture.Faction.UltimatumWeek = 128;
            fixture.State.CooldownUntilDay = fixture.State.CooldownUntilWeek = 0;
            if (playerLeader) fixture.Faction.LeaderClanId = Clan.PlayerClan.StringId;
            fixture.Reload(); return fixture;
        }
        CivilWarActionRequest Request(Fixture fixture, CivilWarAction action, string demand = "", string target = "") => new()
        { OperationId = Guid.NewGuid().ToString("N"), KingdomId = fixture.Home.StringId, FactionId = fixture.State.Factions.FirstOrDefault()?.Id ?? "", Action = action, DemandId = demand, TargetId = target };
        void Drain(Fixture fixture) { for (int i = 0; i < 100; i++) fixture.Owner.ProcessPending(); }

        var f = Prewar(); var faction = f.Faction; f.State.CooldownUntilDay = 730; f.State.CooldownUntilWeek = 104;
        var dissolve = Request(f, CivilWarAction.DissolveOwn);
        Check(f.Owner.Execute(dissolve, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "leader can dissolve while existing cooldown remains");
        Check(f.State.Factions.Count == 0 && f.State.CooldownUntilDay == 730 && f.State.CooldownUntilWeek == 104, "dissolution preserves but never extends cooldown");
        Check(ChangeRelationAction.Changes.Count == 2 && ChangeRelationAction.Changes.All(x => x.Delta == -10), "dissolution penalizes each other member once");
        Check(f.State.Clans.Values.All(x => x.Side == KingdomCivilWarSide.Middle), "dissolution releases all members");
        f.Owner.Execute(dissolve, Clan.PlayerClan);
        Check(ChangeRelationAction.Changes.Count == 2, "dissolution receipt prevents repeat penalties");
        var found = Request(f, CivilWarAction.Found, "redress");
        Check(!f.Owner.Quote(found, Clan.PlayerClan).Allowed, "player founding respects post-war cooldown");
        CampaignTime.Day = 730;
        Check(f.Owner.Execute(found, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "founding unlocks exactly on cooldown day");
        Check(ChangeRelationAction.Changes.Last().First == Clan.PlayerClan.Leader && ChangeRelationAction.Changes.Last().Second == f.Home.Leader && ChangeRelationAction.Changes.Last().Delta == -10, "player founder loses relation with ruler");
        int penalties = ChangeRelationAction.Changes.Count; f.Owner.Execute(found, Clan.PlayerClan);
        Check(ChangeRelationAction.Changes.Count == penalties, "founding receipt never double-charges relationship");

        f = Prewar(false); var leave = Request(f, CivilWarAction.Leave);
        f.Owner.Execute(leave, Clan.PlayerClan);
        Check(ChangeRelationAction.Changes.Any(x => x.Second == f.Leader.Leader && x.Delta == -20) && ChangeRelationAction.Changes.Any(x => x.Second == f.Follower.Leader && x.Delta == -10), "exit retains leader/member differentiated penalties");
        found = Request(f, CivilWarAction.Found, "redress");
        Check(f.Owner.Quote(found, Clan.PlayerClan).Allowed, "manual founding ignores exit cooldown and taken demand");
        Check(f.Owner.Execute(found, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "member can create their own faction without discontent threshold");
        Check(f.State.Factions.Count == 2, "manual creation does not overwrite unrelated faction");

        f = Prewar(); var detonate = Request(f, CivilWarAction.Detonate);
        Clan.PlayerClan.CurrentTotalStrength = 10; // 10 / (150 + 10) = 6.25%.
        Check(!f.Owner.Quote(detonate, Clan.PlayerClan).Allowed && f.Owner.Quote(detonate, Clan.PlayerClan).Reason.Contains("20%"), "low player strength blocks manual detonation");
        Check(f.Owner.Execute(detonate, Clan.PlayerClan).Status == CivilWarActionStatus.Rejected && f.Faction.Stage != KingdomCivilWarStage.OpenWar, "rejected detonation has no war effect");
        DuelSettings.PlayerDetonationStrengthPercent = 0;
        Check(f.Owner.Quote(detonate, Clan.PlayerClan).Allowed, "MCM zero disables strength threshold");
        DuelSettings.PlayerDetonationStrengthPercent = 20; Clan.PlayerClan.CurrentTotalStrength = 37.5f;
        Check(f.Owner.Quote(detonate, Clan.PlayerClan).Allowed, "exact 20% passes unrounded boundary");
        Clan.PlayerClan.CurrentTotalStrength = 37.49f;
        Check(!f.Owner.Quote(detonate, Clan.PlayerClan).Allowed, "rounding to 20% never bypasses actual threshold");
        Clan.PlayerClan.CurrentTotalStrength = 50;
        f.State.CooldownUntilDay = 701;
        Check(!f.Owner.Quote(detonate, Clan.PlayerClan).Allowed, "manual detonation also respects post-war cooldown");
        f.State.CooldownUntilDay = 0;
        Check(f.Owner.Execute(detonate, Clan.PlayerClan).Status == CivilWarActionStatus.AwaitingKingdom, "explicit qualified player command starts war");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.DissolveOwn), Clan.PlayerClan).Allowed, "naming/open war prevents administrative dissolution");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ChangeDemand, "autonomy"), Clan.PlayerClan).Allowed, "naming/open war locks demand changes");

        f = Prewar(); faction = f.Faction;
        var applyRuling = typeof(KingdomCivilWarOwner).GetMethod("ApplyRuling", BindingFlags.Instance | BindingFlags.NonPublic);
        var forcedEscalation = new CivilWarUltimatumResult { Ruling = CivilWarRuling.Refuse, Escalate = true, ConvertToUsurp = true, Log = "forced test escalation" };
        applyRuling.Invoke(f.Owner, new object[] { f.Home, f.State, faction, CivilWarCatalog.FindDemand(faction.DemandId), Clan.PlayerClan, 100, DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)((_, _) => { }), forcedEscalation });
        Check(faction.Stage != KingdomCivilWarStage.OpenWar && faction.DemandId == "redress", "forced automatic refusal cannot ignite or rewrite player-led demand");
        faction.WaitingForOtherWar = true; faction.UltimatumDay = 700; f.Owner.NotifyPoliticalChange(f.Home, "review_waiting"); Drain(f);
        Check(faction.Stage != KingdomCivilWarStage.OpenWar && !faction.WaitingForOtherWar, "legacy pending auto-war cannot ignite player-led faction");

        f = Prewar(); var order = Request(f, CivilWarAction.ForceDissolve); f.Owner.Execute(order, f.Home.RulingClan);
        Clan.PlayerClan.CurrentTotalStrength = 0; var refuse = Request(f, CivilWarAction.Respond); refuse.Accept = false;
        Check(!f.Owner.Quote(refuse, Clan.PlayerClan).Allowed, "defying royal dissolution obeys strength gate");
        CampaignTime.Day = 703; f.Owner.NotifyPoliticalChange(f.Home, "daily"); Drain(f);
        Check(f.Faction.Stage != KingdomCivilWarStage.OpenWar && f.Faction.PendingResponse == null, "timeout drains even below threshold without automatic war");
        f = Prewar(); f.Owner.Execute(Request(f, CivilWarAction.ForceDissolve), f.Home.RulingClan);
        refuse = Request(f, CivilWarAction.Respond); refuse.Accept = false;
        Check(f.Owner.Execute(refuse, Clan.PlayerClan).Status == CivilWarActionStatus.AwaitingKingdom, "explicit qualified defiance can ignite");

        f = Prewar(); faction = f.Faction; string stableId = faction.Id; int created = faction.CreatedWeek;
        f.State.Clans[f.Leader.StringId].Grievance["royal_execution"] = 100;
        f.State.Clans[f.Follower.StringId].Grievance["fief_denied"] = 100;
        faction.DemandId = "redress"; faction.Refusals = 3; faction.Defers = 2; faction.DemandLocked = true;
        MBRandom.Value = 0.5f;
        var change = Request(f, CivilWarAction.ChangeDemand, "autonomy");
        Check(f.Owner.Execute(change, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "player leader can change demand despite old ultimatum lock");
        Check(f.Faction.Id == stableId && f.Faction.CreatedWeek == created && f.Faction.DemandId == "autonomy", "changing demand preserves faction identity and founding date");
        Check(f.State.Clans[Clan.PlayerClan.StringId].Side == KingdomCivilWarSide.Opposition && f.Faction.LeaderClanId == Clan.PlayerClan.StringId, "player leader is never rerolled out");
        Check(f.State.Clans[f.Leader.StringId].Side == KingdomCivilWarSide.Middle, "member with mismatched new demand leaves");
        Check(f.State.Clans[f.Follower.StringId].Side == KingdomCivilWarSide.Opposition, "member aligned to new demand continues");
        Check(f.State.Clans[f.Leader.StringId].MembershipEvaluationDay == 700 && f.State.Clans[f.Follower.StringId].MembershipEvaluationDay == 700, "all other members reevaluate immediately, not after side lock");
        Check(f.Faction.Refusals == 0 && f.Faction.Defers == 0 && f.Faction.UltimatumDay == 714 && f.Faction.Stage == KingdomCivilWarStage.FactionFormed, "new demand resets old escalation/progress only");
        Check(ChangeRelationAction.Changes.Count == 0, "demand reconsideration is not forced player exit or new founding");
        Check(MyBehavior.FactKeys.Any(x => x.EndsWith(f.Leader.Leader.StringId)), "departing member receives authoritative demand-change fact");
        int factCount = MyBehavior.MemoryFacts.Count; f.Owner.Execute(change, Clan.PlayerClan);
        Check(MyBehavior.MemoryFacts.Count == factCount, "same change operation never rerolls or rewrites facts");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ChangeDemand, "autonomy"), Clan.PlayerClan).Allowed, "same demand/target is rejected without reroll");
        f.Reload();
        Check(f.Faction.DemandId == "autonomy" && f.State.Clans[f.Leader.StringId].Side == KingdomCivilWarSide.Middle && f.State.Clans[f.Follower.StringId].Side == KingdomCivilWarSide.Opposition, "changed demand and member decisions survive save reload");
        Check(f.Faction.CachedMemberCount == 2, "retained membership caches rebuilt");
        f.State.FormationAttemptDay = 700; f.State.FormationDueDay = 707;
        f.Owner.NotifyPoliticalChange(f.Home, "structure"); Drain(f);
        Check(f.Faction.DemandId == "autonomy" && f.State.Clans[f.Leader.StringId].Side == KingdomCivilWarSide.Middle, "queued events neither auto-rewrite demand nor reroll departed clan immediately");
        f = Prewar(false);
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ChangeDemand, "autonomy"), Clan.PlayerClan).Allowed, "ordinary member cannot change leader's demand");
        f = Prewar(); f.Owner.Execute(Request(f, CivilWarAction.Negotiate), f.Home.RulingClan);
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ChangeDemand, "autonomy"), Clan.PlayerClan).Allowed, "pending royal agreement locks demand rewrite");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ChangeDemand, "unknown"), Clan.PlayerClan).Allowed, "invalid demands cannot replace existing demand");

        f = Prewar(false); f.State.Factions.Clear();
        foreach (var record in f.State.Clans.Values) { record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; }
        f.State.Clans[f.Leader.StringId].Side = KingdomCivilWarSide.Crown; f.State.Clans[f.Leader.StringId].Grievance["lands_raided"] = 100;
        f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] = 10;
        Check(f.Owner.BuildPlayerKingdomPanel().TopGrievance == 10, "formation ledger matches actual neutral candidate pool");
        f = Prewar(false); f.State.Factions.Clear();
        foreach (var record in f.State.Clans.Values) { record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; }
        f.State.Clans[f.Leader.StringId].Grievance["royal_execution"] = 100; MBRandom.Value = 0f;
        f.Owner.NotifyPoliticalChange(f.Home, "royal_execution"); Drain(f);
        Check(f.State.Factions.Count == 1, "NPC automatic formation remains enabled");
        Check(ChangeRelationAction.Changes.Any(x => x.First == f.Leader.Leader && x.Second == f.Home.Leader && x.Delta == -10), "NPC faction founding also loses ruler relationship");
        f = Prewar(false); faction = f.Faction;
        applyRuling.Invoke(f.Owner, new object[] { f.Home, f.State, faction, CivilWarCatalog.FindDemand(faction.DemandId), f.Leader, 100, DuelSettings.BuildCivilWarTuning(), (Action<Kingdom, int>)((_, _) => { }), forcedEscalation });
        Check(f.Faction.Stage == KingdomCivilWarStage.OpenWar, "NPC-led automatic escalation remains unchanged");

        f = Prewar(false); f.State.Clans[Clan.PlayerClan.StringId].Side = KingdomCivilWarSide.Crown; f.State.Clans[Clan.PlayerClan.StringId].FactionId = "";
        found = Request(f, CivilWarAction.Found, "redress");
        Check(f.Owner.Execute(found, Clan.PlayerClan).Status == CivilWarActionStatus.Applied && f.State.Factions.Count == 2, "crown supporter may create a faction after paying old-side exit penalties");
        Check(ChangeRelationAction.Changes.Count(x => x.Second == f.Home.Leader && x.Delta == -20) == 1 && ChangeRelationAction.Changes.Count(x => x.Second == f.Home.Leader && x.Delta == -10) == 1, "old crown exit and new faction founding are distinct relationship consequences");
        f = Prewar(); detonate = Request(f, CivilWarAction.Detonate);
        Check(f.Owner.Quote(detonate, Clan.PlayerClan).Allowed, "initial quote meets strength gate");
        Clan.PlayerClan.CurrentTotalStrength = 1;
        Check(f.Owner.Execute(detonate, Clan.PlayerClan).Status == CivilWarActionStatus.Rejected, "confirmation revalidates strength instead of trusting UI quote");
        f = Prewar(false); DuelSettings.PlayerDetonationStrengthPercent = 100;
        Check(!f.Owner.TryDetonate(f.Leader.Leader, f.Home, out _), "shared dialogue detonation cannot bypass MCM strength gate");
        f = Prewar();
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ChangeDemand, "unknown"), Clan.PlayerClan).Allowed, "unknown demand rejected before state mutation");
        Check((int)CivilWarAction.Respond == 9 && (int)CivilWarAction.DissolveOwn == 10 && (int)CivilWarAction.ChangeDemand == 11, "new actions append without renumbering persisted operation values");
        Check(CivilWarPoliticalRules.DemandRetentionChance(5, .1f, 0, new()) < CivilWarPoliticalRules.DemandRetentionChance(.1f, 5, 0, new()), "member own grievance affinity affects reconsideration");
        Console.WriteLine($"Player faction controls passed: {checks} checks (real owner, fake game actions).");
        return checks;
    }
}
