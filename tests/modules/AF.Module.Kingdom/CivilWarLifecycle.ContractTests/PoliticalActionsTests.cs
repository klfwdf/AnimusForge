using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

internal static class PoliticalActionsTests
{
    internal static int Run()
    {
        int count = 0;
        void Check(bool condition, string description) { count++; if (!condition) throw new Exception(description); }
        Fixture Prewar()
        {
            var f = new Fixture();
            ChangeKingdomAction.Move(f.Leader, f.Home); ChangeKingdomAction.Move(f.Follower, f.Home);
            f.Faction.Stage = KingdomCivilWarStage.FactionFormed; f.Faction.RebelKingdomId = ""; f.Faction.ResolutionOutcomeId = "";
            f.Faction.UltimatumDay = 900; f.Faction.UltimatumWeek = 128; f.Faction.CreatedWeek = 100;
            f.State.CooldownUntilDay = 0; f.State.CooldownUntilWeek = 0;
            f.Reload(); return f;
        }
        CivilWarActionRequest Request(Fixture f, CivilWarAction a) => new() { OperationId = Guid.NewGuid().ToString("N"), KingdomId = f.Home.StringId, FactionId = f.State.Factions.FirstOrDefault()?.Id ?? "", Action = a };
        void Drain(KingdomCivilWarOwner owner) { bool bounded = true; for (int i = 0; i < 100; i++) { owner.ProcessPending(); bounded &= owner.LastBatchRecords <= 32; } Check(bounded, "worker batch record cap across all drain calls"); }

        var f = Prewar();
        var leave = Request(f, CivilWarAction.Leave);
        Check(f.Owner.Execute(leave, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "player can leave before war");
        Check(ChangeRelationAction.Calls == 2, "only leader and other member penalized once");
        Check(f.Owner.Storage.ClanExitUntilDay[Clan.PlayerClan.StringId] == 707, "seven game day exit lock");
        f.Owner.Execute(leave, Clan.PlayerClan);
        Check(ChangeRelationAction.Calls == 2, "duplicate receipt does not repeat relation penalty");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.JoinCrown), Clan.PlayerClan).Allowed, "exit lock blocks crown switching");
        ChangeKingdomAction.Move(Clan.PlayerClan, f.Rebel);
        var join = Request(f, CivilWarAction.JoinCrown); join.KingdomId = f.Rebel.StringId; join.FactionId = "";
        Check(!f.Owner.Quote(join, Clan.PlayerClan).Allowed, "changing kingdoms does not bypass lock");
        f.Reload(); CampaignTime.Day = 707;
        Check(f.Owner.Execute(join, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "crown join needs no opposition and unlocks on day seven");
        int joinedWeek = f.Owner.Storage.Kingdoms[f.Rebel.StringId].Clans[Clan.PlayerClan.StringId].SideSinceWeek;
        join.OperationId = Guid.NewGuid().ToString("N"); CampaignTime.Day = 708;
        f.Owner.Execute(join, Clan.PlayerClan);
        Check(f.Owner.Storage.Kingdoms[f.Rebel.StringId].Clans[Clan.PlayerClan.StringId].SideSinceWeek == joinedWeek, "joining current side does not reset time");

        f = Prewar(); f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Leader.CurrentTotalStrength = f.Follower.CurrentTotalStrength = 50;
        f.Owner.Execute(Request(f, CivilWarAction.Leave), Clan.PlayerClan);
        Check(f.Faction.LeaderClanId == "follower", "equal strength succession uses ordinal clan id");
        f = Prewar(); f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Leader.IsEliminated = f.Follower.IsEliminated = true;
        f.Owner.Execute(Request(f, CivilWarAction.Leave), Clan.PlayerClan);
        Check(f.State.Factions.Count == 0, "no successor dissolves faction");

        f = Prewar(); f.State.Factions.Clear(); f.State.Clans[Clan.PlayerClan.StringId].Side = KingdomCivilWarSide.Middle;
        var found = Request(f, CivilWarAction.Found); found.DemandId = "redress";
        Check(f.Owner.Execute(found, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "zero grievance player can found");
        Check(f.Faction.LeaderClanId == Clan.PlayerClan.StringId && f.Faction.PlayerFounded, "player owns founded faction");
        Check(f.Owner.Execute(found, Clan.PlayerClan).Status == CivilWarActionStatus.Applied && f.State.Factions.Count == 1, "found receipt is idempotent");
        Check(f.Owner.Execute(Request(f, CivilWarAction.Detonate), Clan.PlayerClan).Status == CivilWarActionStatus.AwaitingKingdom, "player leader detonation is direct");
        Check(!f.Faction.PlayerFollowPending, "player never asked to follow themselves");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.Leave), Clan.PlayerClan).Allowed, "naming stage locks membership");

        f = Prewar(); var propose = Request(f, CivilWarAction.Detonate);
        Check(f.Owner.Execute(propose, Clan.PlayerClan).Status == CivilWarActionStatus.Rejected, "NPC leader may reject member proposal");
        Check(f.Faction.ProposalUntilDay == 707 && !f.Owner.Quote(Request(f, CivilWarAction.Detonate), Clan.PlayerClan).Allowed, "rejected proposal cooldown");
        f = Prewar();
        Check(f.Owner.TryDetonate(f.Leader.Leader, f.Home, out _), "explicit dialogue agreement does not reroll");

        f = Prewar(); var crown = f.Home.RulingClan; var suppress = Request(f, CivilWarAction.Suppress);
        float influence = crown.Influence;
        Check(f.Owner.Execute(suppress, crown).Status == CivilWarActionStatus.Applied, "king suppression works");
        Check(crown.Influence == influence - 100 && f.Leader.Influence == 480, "suppression pays and reduces member influence");
        Check(f.State.Clans[f.Leader.StringId].Grievance["royal_suppression"] == 7.5f && f.State.Clans[f.Follower.StringId].Grievance["royal_suppression"] == 4, "suppression grievances differentiated");
        Check(f.Faction.UltimatumDay == 907, "first suppression extends deadline");
        f.Owner.Execute(suppress, crown); Check(crown.Influence == influence - 100, "duplicate suppression no second charge");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.Negotiate), crown).Allowed, "shared governance cooldown");
        Check(f.Owner.Quote(Request(f, CivilWarAction.Concede), crown).Allowed, "concession bypasses cooldown");
        CampaignTime.Day = 707;
        f.Owner.Execute(Request(f, CivilWarAction.Suppress), crown);
        Check(f.Faction.UltimatumDay == 907, "suppression cannot extend repeatedly");

        f = Prewar(); crown = f.Home.RulingClan; f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        var offer = Request(f, CivilWarAction.Negotiate); offer.OfferInfluence = true; offer.OfferTier = 2;
        Check(f.Owner.Execute(offer, crown).Status == CivilWarActionStatus.AwaitingPlayer, "AI offer waits for player leader");
        Check(crown.Influence == 500 && f.Faction.PendingResponse.DeadlineDay == 703, "pending offer no charge and three day deadline");
        int offered = f.Faction.PendingResponse.Influence;
        Check(offered > 0 && offered % 5 == 0, "floating influence offer is positive and rounded");
        f.Reload(); var answer = Request(f, CivilWarAction.Respond); answer.Accept = true;
        Check(f.Owner.Execute(answer, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "saved offer accepts");
        Check(crown.Influence == 500 - offered && Clan.PlayerClan.Influence == 500 + offered && f.State.Factions.Count == 0, "compensation is actual transfer without original redress");
        Check(GiveGoldAction.Calls == 0, "negotiation does not execute original demand");
        f.Owner.Execute(answer, Clan.PlayerClan); Check(crown.Influence == 500 - offered, "saved response receipt prevents duplicate transfer");

        f = Prewar(); crown = f.Home.RulingClan; f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        offer = Request(f, CivilWarAction.ForceDissolve);
        Check(f.Owner.Execute(offer, crown).Status == CivilWarActionStatus.AwaitingPlayer, "player decides force order response");
        Check(crown.Influence == 300, "force order has upfront cost");
        CampaignTime.Day = 703; f.Owner.NotifyPoliticalChange(f.Home, "daily"); Drain(f.Owner);
        Check(f.Faction.Stage != KingdomCivilWarStage.OpenWar && f.Faction.PendingResponse == null, "player leader timeout never automatically detonates");

        f = Prewar(); crown = f.Home.RulingClan;
        var stale = Request(f, CivilWarAction.Suppress); stale.Version = 100;
        Check(f.Owner.Execute(stale, crown).Status == CivilWarActionStatus.Rejected && crown.Influence == 500, "stale quote has no charge");
        f = Prewar(); crown = f.Home.RulingClan; f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        offer = Request(f, CivilWarAction.Negotiate); f.Owner.Execute(offer, crown);
        GiveGoldAction.ThrowAfterApply = true; answer = Request(f, CivilWarAction.Respond); answer.Accept = true;
        Check(f.Owner.Execute(answer, Clan.PlayerClan).Status == CivilWarActionStatus.PartialFailure, "unknown transfer failure retained");
        f.Reload(); GiveGoldAction.ThrowAfterApply = false;
        f.Owner.Execute(answer, Clan.PlayerClan);
        Check(GiveGoldAction.Calls == 1 && f.Faction.ResolutionNeedsReview, "unknown transfer never repeats after reload");

        f = new Fixture(); crown = f.Home.RulingClan;
        f.Faction.ResolutionOutcomeId = ""; f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Rebel.RulingClan = Clan.PlayerClan; ChangeKingdomAction.Move(Clan.PlayerClan, f.Rebel); f.Faction.WarClanIds.Add(Clan.PlayerClan.StringId);
        offer = Request(f, CivilWarAction.Negotiate); f.Owner.Execute(offer, crown);
        MakePeaceAction.Fail = true; answer = Request(f, CivilWarAction.Respond); answer.Accept = true;
        Check(f.Owner.Execute(answer, Clan.PlayerClan).Status == CivilWarActionStatus.PartialFailure && GiveGoldAction.Calls == 0, "wartime compensation waits for confirmed peace");
        Check(f.Faction.PendingResponse.Accepted && !f.Faction.ResolutionNeedsReview, "safe unfinished settlement remains retryable");
        f.Reload(); MakePeaceAction.Fail = false; f.Owner.NotifyPoliticalChange(f.Home, "peace"); Drain(f.Owner);
        Check(f.State.Factions.Count == 0 && GiveGoldAction.Calls == 1 && Clan.PlayerClan.Kingdom == f.Home && f.Rebel.IsEliminated, "saved accepted offer returns rebels before paying exactly once");

        f = Prewar(); f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        offer = Request(f, CivilWarAction.Negotiate); f.Owner.Execute(offer, f.Home.RulingClan);
        Check(MyBehavior.PoliticalResults.Count == 0 && MyBehavior.MemoryFacts.Count == 0, "pending proposal is not an executed fact");
        answer = Request(f, CivilWarAction.Respond); answer.Accept = true; f.Owner.Execute(answer, Clan.PlayerClan);
        Check(MyBehavior.PoliticalResults.Count == 1 && MyBehavior.FactKeys.Distinct().Count() == 4 && MyBehavior.MemoryFacts.Count == 4, "successful settlement reaches all four involved leaders and shared history");
        f.Owner.Execute(answer, Clan.PlayerClan);
        Check(MyBehavior.MemoryFacts.Count == 4 && MyBehavior.PoliticalResults.Count == 1, "duplicate click does not repeat fact or report");

        f = Prewar(); f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Owner.Execute(Request(f, CivilWarAction.Detonate), Clan.PlayerClan);
        f.Owner.NotifyRebellionFailed(f.Faction.Id, "naming failed");
        Check(f.State.Factions.Count == 1 && f.Faction.Stage == KingdomCivilWarStage.FactionFormed, "failed naming preserves faction and reopens prewar actions");
        Check(!f.Owner.IsInOpenCivilWar(f.Home.StringId), "failed naming clears war index");

        f = Prewar();
        f.State.CooldownUntilDay = -1; f.State.CooldownUntilWeek = 102;
        f.Faction.UltimatumDay = -1; f.Faction.UltimatumWeek = 103;
        f.Reload();
        Check(f.Owner.Storage.Version == 4 && f.State.CooldownUntilDay == 714 && f.Faction.UltimatumDay == 721, "v3 week deadlines migrate into v4 days");
        f = Prewar(); var legacy = f.Faction; f.State.Factions.Clear(); f.State.LegacyFaction = legacy; f.State.Stage = KingdomCivilWarStage.FactionFormed;
        f.Reload(); Check(f.State.Factions.Count == 1 && f.State.LegacyFaction == null && f.Faction.UltimatumDay == 900, "v2 faction migration precedes day migration");

        f = Prewar(); f.Faction.PlayerFounded = true;
        f.State.Clans[f.Leader.StringId].Grievance["lands_raided"] = 60;
        f.Reload(); float beforeMean = f.Faction.Grievance;
        f.Owner.AddGrievance(f.Home, "lands_raided", new[] { f.Leader }, 30, 100, "raid");
        Check(Math.Abs(f.Faction.Grievance - beforeMean - 5) < .001, "event updates faction mean immediately without scanning all members");
        f.Owner.Execute(Request(f, CivilWarAction.Leave), Clan.PlayerClan);
        Check(Math.Abs(f.Faction.Grievance - 37.5f) < .001, "membership rebuild keeps mean consistent");

        f = Prewar(); f.Faction.PlayerFounded = true; f.Faction.UltimatumDay = 2000;
        f.State.CooldownUntilDay = 2000;
        for (int i = 0; i < 100; i++)
        {
            var clan = new Clan { StringId = "stress-" + i, Name = "stress", Kingdom = f.Home };
            clan.Leader = new Hero { Clan = clan }; Clan.All.Add(clan); f.Home.Clans.Add(clan);
            f.State.Clans[clan.StringId] = new() { ClanId = clan.StringId, Side = KingdomCivilWarSide.Opposition, FactionId = i % 2 == 0 ? f.Faction.Id : "faction2" };
        }
        f.State.Clans[f.Follower.StringId].FactionId = "faction2";
        f.State.Factions.Add(new() { Id = "faction2", LeaderClanId = f.Follower.StringId, DemandId = "autonomy", PlayerFounded = true, UltimatumDay = 2000 });
        f.Reload();
        for (int i = 0; i < 100; i++) f.Owner.AddGrievance(f.Home, "lands_raided", new[] { f.Leader }, .1f, 100, "stress");
        var queue = (System.Collections.ICollection)typeof(KingdomCivilWarOwner).GetField("_politicalQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(f.Owner);
        Check(queue.Count == 1, "one hundred events coalesce into one kingdom job");
        var watch = System.Diagnostics.Stopwatch.StartNew(); int batches = 0;
        bool stressBounded = true;
        while (batches++ < 100) { f.Owner.ProcessPending(); stressBounded &= f.Owner.LastBatchRecords <= 32; }
        Check(stressBounded, "all stress batches bounded");
        watch.Stop();
        Check(f.State.Clans.Count >= 103 && queue.Count == 0, "bounded worker drains hundred clan workload");
        Check(f.State.Factions[0].DemandId == "redress", "player founded demand never auto changes");
        Console.WriteLine($"Political workload: 100 events, 100 added clans, 2 factions; {watch.Elapsed.TotalMilliseconds:F3} ms total across bounded calls (fake campaign).");

        f = Prewar(); f.State.Factions.Clear();
        foreach (var clan in f.State.Clans.Values) { clan.Side = KingdomCivilWarSide.Middle; clan.FactionId = ""; }
        f.Reload();
        f.Owner.AddGrievance(f.Home, "lands_raided", new[] { f.Leader }, 100, 100, "raid"); Drain(f.Owner);
        Check(f.State.FormationAttemptDay == 700 && f.State.FormationDueDay == 707, "failed formation schedules seven day re-evaluation");
        for (int i = 0; i < 10; i++) f.Owner.AddGrievance(f.Home, "lands_raided", new[] { f.Leader }, 1, 100, "more raid");
        Drain(f.Owner);
        Check(f.State.FormationAttemptDay == 700, "repeated events cannot reroll formation");
        CampaignTime.Day = 707; f.Owner.NotifyPoliticalChange(f.Home, "daily"); Drain(f.Owner);
        Check(f.State.FormationAttemptDay == 707, "due formation retries without another grievance event");

        f = Prewar(); f.State.Clans[Clan.PlayerClan.StringId].Side = KingdomCivilWarSide.Middle;
        Clan.PlayerClan.IsUnderMercenaryService = true;
        Check(!f.Owner.Quote(Request(f, CivilWarAction.JoinOpposition), Clan.PlayerClan).Allowed, "mercenary cannot join opposition");
        Clan.PlayerClan.IsUnderMercenaryService = false;
        var choices = f.Owner.GetFoundingOptions(f.Home.StringId, Clan.PlayerClan);
        Check(choices.Any(x => x.DemandId == "autonomy") && choices.Any(x => x.DemandId == "usurp"), "player fortification qualifies without negative king relation");
        Clan.PlayerClan.Settlements.Clear();
        Check(!f.Owner.GetFoundingOptions(f.Home.StringId, Clan.PlayerClan).Any(x => x.DemandId == "autonomy" || x.DemandId == "usurp"), "player still needs demanded fortifications");
        PlayerKingdomRebellionImmunity.Protected = true;
        Check(!f.Owner.Quote(Request(f, CivilWarAction.ForceDissolve), f.Home.RulingClan).Allowed, "force order disabled when immunity blocks defiance");
        PlayerKingdomRebellionImmunity.Protected = false;

        f = Prewar(); crown = f.Home.RulingClan;
        Check(f.Owner.Execute(Request(f, CivilWarAction.ForceDissolve), crown).Status == CivilWarActionStatus.Applied, "NPC may comply with dissolution");
        Check(f.State.Factions.Count == 0 && crown.Influence == 300 && f.State.Clans[f.Leader.StringId].Grievance["royal_suppression"] == 7.5f, "dissolution retains grievances and charges once");
        Check(Math.Abs(CivilWarPoliticalRules.NegotiationChance(new Dictionary<string, float>(), 2, new CivilWarTuning()) - .66f) < .0001f, "negotiation uses specified raw formula and shared shaping");

        f = Prewar(); f.Faction.DemandId = "usurp";
        Check(f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan).Status == CivilWarActionStatus.Applied && f.Home.RulingClan == f.Leader && f.State.Factions.Count == 0, "conceding usurpation really changes ruler");
        f = new Fixture(); f.Faction.DemandId = "autonomy"; f.Faction.ResolutionOutcomeId = "";
        Check(f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan).Status == CivilWarActionStatus.Applied && !f.Home.IsAtWarWith(f.Rebel) && f.Leader.Kingdom == f.Rebel && !f.Rebel.IsEliminated, "wartime autonomy concession really preserves independent kingdom");
        f = Prewar(); f.Faction.DemandId = "autonomy";
        var inheritedWar = new Kingdom { StringId = "inherited_war" };
        var priorWar = new Kingdom { StringId = "prior_clan_war" };
        Kingdom.All.Add(inheritedWar); Kingdom.All.Add(priorWar);
        f.Home.FactionsAtWarWith.Add(inheritedWar); f.Home.FactionsAtWarWith.Add(priorWar);
        f.Leader.FactionsAtWarWith.Add(priorWar);
        var independenceRequest = Request(f, CivilWarAction.Concede);
        Check(f.Owner.Execute(independenceRequest, f.Home.RulingClan).Status == CivilWarActionStatus.AwaitingKingdom && f.Leader.Kingdom == f.Home && f.State.Factions.Count == 1, "pre-war autonomy awaits LLM founding without prematurely leaving or dissolving");
        string independenceOperation = f.Faction.Fulfillment.OperationId;
        Check(f.Owner.IsIndependenceRequestCurrent(f.Faction.Id, independenceOperation, f.Home.StringId, f.Leader.StringId) && f.Owner.WorldOpenWarCount == 0, "authorized independence does not consume a civil war slot");
        Check(!f.Owner.IsIndependenceRequestCurrent(f.Faction.Id, "stale", f.Home.StringId, f.Leader.StringId), "stale naming operation cannot create a kingdom");
        Check(f.Owner.GetIndependenceChoice(f.Faction.Id, independenceOperation) == "" && f.Owner.AnswerIndependenceChoice(f.Faction.Id, independenceOperation, true), "player participant chooses LLM kingdom founding explicitly");
        f.Owner.NoteIndependenceNamingReady(f.Faction.Id, independenceOperation, "Independent Realm", "Realm", "Founded by agreement.");
        f.Reload();
        Check(f.Owner.GetIndependenceNaming(f.Faction.Id, independenceOperation, out string formal, out _, out _) && formal == "Independent Realm", "validated LLM naming survives save reload");
        ChangeKingdomAction.Move(f.Leader, f.Rebel);
        f.Rebel.FactionsAtWarWith.Add(inheritedWar); inheritedWar.FactionsAtWarWith.Add(f.Rebel);
        f.Rebel.FactionsAtWarWith.Add(priorWar); priorWar.FactionsAtWarWith.Add(f.Rebel);
        f.Owner.NoteIndependentKingdomCreated(f.Faction.Id, independenceOperation, f.Rebel);
        MakePeaceAction.Fail = true;
        Drain(f.Owner);
        Check(f.State.Factions.Count == 1 && f.Rebel.IsAtWarWith(inheritedWar), "failed inherited-war reconciliation cannot complete an otherwise created kingdom");
        f.Reload(); MakePeaceAction.Fail = false;
        f.Owner.NotifyPoliticalChange(f.Home, "retry_structural_peace"); Drain(f.Owner);
        Check(!f.Rebel.IsAtWarWith(inheritedWar) && f.Rebel.IsAtWarWith(priorWar), "saved structural-war cleanup resumes after creation event failure without ending preexisting clan war");
        Check(f.Leader.Kingdom == f.Rebel && f.Follower.Kingdom == f.Rebel && Clan.PlayerClan.Kingdom == f.Rebel && !f.Home.IsAtWarWith(f.Rebel) && f.State.Factions.Count == 0, "confirmed founding joins authorized clans into one peaceful independent kingdom");
        Check(MyBehavior.StabilityChanges == 4, "peaceful independence has concession bonus only, no outbreak penalty");

        f = Prewar(); f.Faction.DemandId = "autonomy";
        f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan);
        string leaveOperation = f.Faction.Fulfillment.OperationId;
        Check(f.Owner.AnswerIndependenceChoice(f.Faction.Id, leaveOperation, false), "player may choose departure without kingdom founding");
        Check(!f.Owner.AnswerIndependenceChoice(f.Faction.Id, leaveOperation, true), "departure choice cannot be overwritten by a late callback");
        f.Reload(); Drain(f.Owner);
        Check(f.State.Factions.Count == 0 && f.Leader.Kingdom == null && f.Follower.Kingdom == null && Clan.PlayerClan.Kingdom == null && f.Follower.Settlements.Contains(f.RebelTown), "saved departure choice keeps fiefs and never creates a kingdom");

        f = Prewar(); f.Faction.DemandId = "autonomy";
        f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan);
        string failedOperation = f.Faction.Fulfillment.OperationId;
        f.Owner.AnswerIndependenceChoice(f.Faction.Id, failedOperation, true);
        f.Owner.NoteIndependenceNamingReady(f.Faction.Id, failedOperation, "Retry Realm", "Retry", "Agreed founding.");
        f.Owner.NotifyRebellionFailed(f.Faction.Id, "physical_create_failed");
        Check(f.Faction.Fulfillment.Status == "failed" && !f.Owner.IsRebellionRequestActive(f.Faction.Id), "physical failure closes the active naming request without losing faction");
        CampaignTime.Day += CivilWarPoliticalRules.ActionDays;
        var retryFounding = Request(f, CivilWarAction.Concede);
        Check(f.Owner.Execute(retryFounding, f.Home.RulingClan).Status == CivilWarActionStatus.AwaitingKingdom && f.Faction.Fulfillment.OperationId == retryFounding.OperationId, "explicit founding retry receives a new operation identity");
        Check(f.Owner.GetIndependenceNaming(f.Faction.Id, retryFounding.OperationId, out string reusedName, out _, out _) && reusedName == "Retry Realm", "physical retry reuses validated saved naming");

        f = Prewar(); f.Faction.DemandId = "make_peace"; f.Faction.TargetId = f.Rebel.StringId;
        VoteDealBehavior.AgendaStatus = "pending"; VoteDealBehavior.Submitted.Clear();
        Check(f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan).Status == CivilWarActionStatus.AwaitingKingdom && f.Home.IsAtWarWith(f.Rebel), "anti-war concession submits an agenda without directly making peace");
        f.Reload(); Drain(f.Owner);
        Check(VoteDealBehavior.Submitted.Count == 1 && f.State.Factions.Count == 1, "save reload retains one pending peace agenda");
        VoteDealBehavior.AgendaStatus = "rejected"; f.Owner.NotifyPoliticalChange(f.Home, "vote"); Drain(f.Owner);
        Check(f.State.Factions.Count == 1 && f.Faction.Fulfillment.Status == "rejected" && f.Home.IsAtWarWith(f.Rebel), "rejected peace vote preserves faction and war");
        f.Owner.NotifyPoliticalChange(f.Home, "daily"); Drain(f.Owner);
        Check(VoteDealBehavior.Submitted.Count == 1, "rejected agenda is not automatically submitted again");
        CampaignTime.Day += CivilWarPoliticalRules.ActionDays;
        VoteDealBehavior.AgendaStatus = "pending";
        Check(f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan).Status == CivilWarActionStatus.AwaitingKingdom && VoteDealBehavior.Submitted.Count == 2, "ruler may explicitly retry a rejected agenda after cooldown");
        MakePeaceAction.Apply(f.Home, f.Rebel);
        VoteDealBehavior.AgendaStatus = "completed";
        f.Owner.NotifyPoliticalChange(f.Home, "vote_complete"); Drain(f.Owner);
        Check(f.State.Factions.Count == 0 && !f.Home.IsAtWarWith(f.Rebel), "peace demand completes only after approved agenda and actual peace");
        VoteDealBehavior.AgendaStatus = "pending";

        f = Prewar(); f.Faction.DemandId = "make_peace"; f.Faction.TargetId = f.Rebel.StringId;
        VoteDealBehavior.Submitted.Clear(); VoteDealBehavior.AgendaStatus = "pending";
        f.Owner.Execute(Request(f, CivilWarAction.Concede), f.Home.RulingClan);
        VoteDealBehavior.AgendaStatus = "rejected"; f.Owner.NotifyPoliticalChange(f.Home, "vote_rejected"); Drain(f.Owner);
        int retryDue = f.Faction.GovernanceUntilDay;
        Check(VoteDealBehavior.Submitted.Count == 1, "AI ruler does not retry rejected peace before cooldown");
        CampaignTime.Day = retryDue; VoteDealBehavior.AgendaStatus = "pending";
        f.Owner.NotifyPoliticalChange(f.Home, "daily"); Drain(f.Owner);
        Check(VoteDealBehavior.Submitted.Count == 2 && f.Faction.Fulfillment.Status == "pending", "AI ruler can choose a fresh concession operation after failed peace cooldown");
        f.Owner.NotifyPoliticalChange(f.Home, "daily"); Drain(f.Owner);
        Check(VoteDealBehavior.Submitted.Count == 2, "AI retry decision cannot flood repeated same-day agendas");

        f = new Fixture(); f.Faction.DemandId = "make_peace"; f.Faction.ResolutionOutcomeId = CivilWarCatalog.NegotiatedOutcomeId;
        var foreign = new Kingdom { StringId = "foreign_peace" }; Kingdom.All.Add(foreign);
        f.Home.FactionsAtWarWith.Add(foreign); foreign.FactionsAtWarWith.Add(f.Home); f.Faction.TargetId = foreign.StringId;
        VoteDealBehavior.Submitted.Clear();
        f.Tick(101);
        Check(f.State.Factions.Count == 1 && f.Home.IsAtWarWith(foreign) && f.Faction.Fulfillment?.Kind == "peace", "war compromise also waits for peace agenda instead of forcing foreign peace");
        Check(f.Leader.Kingdom == f.Home && f.Follower.Kingdom == f.Home, "compromise safely returns civil-war participants before foreign vote");

        f = Prewar(); crown = f.Home.RulingClan; f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Owner.Execute(Request(f, CivilWarAction.Negotiate), crown);
        f.Owner.Execute(Request(f, CivilWarAction.Leave), Clan.PlayerClan); Drain(f.Owner);
        Check(f.Faction.PendingResponse == null && crown.Leader.Gold == 10000, "succession cancels unanswered offer without charging old terms");
        f = Prewar(); f.Home.RulingClan = f.Follower;
        f.Owner.NotifyPoliticalChange(f.Home, "ruler_changed"); Drain(f.Owner);
        Check(f.State.Clans[f.Follower.StringId].Side == KingdomCivilWarSide.Crown && f.State.Clans[f.Follower.StringId].FactionId == "", "new ruler cannot remain an opposition member");
        return count;
    }
}
