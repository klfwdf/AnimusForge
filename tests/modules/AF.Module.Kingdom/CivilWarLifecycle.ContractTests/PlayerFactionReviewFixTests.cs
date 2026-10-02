using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

internal static class PlayerFactionReviewFixTests
{
    private static Fixture Prewar(bool playerLeader)
    {
        var f = new Fixture();
        ChangeKingdomAction.Move(f.Leader, f.Home); ChangeKingdomAction.Move(f.Follower, f.Home);
        f.Faction.Stage = KingdomCivilWarStage.FactionFormed; f.Faction.RebelKingdomId = ""; f.Faction.ResolutionOutcomeId = "";
        f.Faction.WarClanIds.Clear(); f.Faction.UltimatumDay = 900; f.Faction.UltimatumWeek = 128;
        f.State.CooldownUntilDay = f.State.CooldownUntilWeek = 0;
        if (playerLeader) f.Faction.LeaderClanId = Clan.PlayerClan.StringId;
        f.Reload(); return f;
    }

    private static void OnlyMember(Fixture f, Clan member)
    {
        foreach (var record in f.State.Clans.Values)
            if (record.ClanId != member.StringId) { record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; }
    }

    private static CivilWarActionRequest Request(Fixture f, CivilWarAction action) => new()
    { OperationId = Guid.NewGuid().ToString("N"), KingdomId = f.Home.StringId, FactionId = f.State.Factions.FirstOrDefault()?.Id ?? "", Action = action, DemandId = "redress" };

    private static void Drain(Fixture f) { for (int i = 0; i < 100; i++) f.Owner.ProcessPending(); }

    internal static int RunCooldown()
    {
        int checks = 0;
        void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Review cooldown fix: " + message); }
        var f = Prewar(true); OnlyMember(f, Clan.PlayerClan);
        var leave = Request(f, CivilWarAction.Leave);
        Check(f.Owner.Execute(leave, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "last player member may leave");
        Check(f.State.Factions.Count == 0, "no successor closes faction");
        Check(f.State.CooldownUntilDay == 0 && f.State.CooldownUntilWeek == 0, "last player exit does not invent war cooldown");
        Check(f.Owner.Storage.ClanExitUntilDay[Clan.PlayerClan.StringId] == 707, "existing seven-day affiliation cooldown remains");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.JoinCrown), Clan.PlayerClan).Allowed, "joining still respects exit cooldown");
        Check(f.Owner.Quote(Request(f, CivilWarAction.Found), Clan.PlayerClan).Allowed, "manual creation remains available without real war cooldown");
        int facts = MyBehavior.MemoryFacts.Count; f.Owner.Execute(leave, Clan.PlayerClan);
        Check(MyBehavior.MemoryFacts.Count == facts && f.State.CooldownUntilDay == 0, "duplicate exit neither repeats facts nor starts cooldown");
        f.Reload();
        Check(f.State.Factions.Count == 0 && f.State.CooldownUntilDay == 0, "no invented cooldown after saved exit reload");

        f = Prewar(true); OnlyMember(f, Clan.PlayerClan); f.State.CooldownUntilDay = 730; f.State.CooldownUntilWeek = 104;
        Check(f.Owner.Execute(Request(f, CivilWarAction.Leave), Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "last player exit allowed with prior cooldown");
        Check(f.State.CooldownUntilDay == 730 && f.State.CooldownUntilWeek == 104, "last exit neither clears nor extends existing cooldown");
        Check(!f.Owner.Quote(Request(f, CivilWarAction.Found), Clan.PlayerClan).Allowed, "existing post-war cooldown still blocks manual founding");
        f.Reload(); CampaignTime.Day = 730;
        Check(f.Owner.Quote(Request(f, CivilWarAction.Found), Clan.PlayerClan).Allowed, "existing cooldown expires at exact saved day");

        f = Prewar(false); OnlyMember(f, f.Leader);
        Check(f.Owner.Execute(Request(f, CivilWarAction.Leave), f.Leader).Status == CivilWarActionStatus.Applied, "NPC last-member exit still supported");
        Check(f.State.Factions.Count == 0 && f.State.CooldownUntilDay == 756, "NPC no-successor cooldown semantics unchanged");
        f = new Fixture(); f.State.CooldownUntilDay = f.State.CooldownUntilWeek = 0;
        MakePeaceAction.Apply(f.Home, f.Rebel); f.Owner.NotifyPoliticalChange(f.Home, "peace"); Drain(f);
        Check(f.State.Factions.Count == 0, "real war completion still resolves faction");
        Check(f.State.CooldownUntilDay == 756 && f.State.CooldownUntilWeek == 108, "real war completion still starts eight-week cooldown");
        return checks;
    }

    internal static int RunFacts()
    {
        int checks = 0;
        void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Review founding facts fix: " + message); }
        bool HasFact(CivilWarActionRequest r, Hero hero) => MyBehavior.FactKeys.Contains("civil_war:action:" + r.OperationId + ":" + hero.StringId);
        var f = Prewar(false); var request = Request(f, CivilWarAction.Found);
        Check(f.Owner.Execute(request, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "member may found after exiting old faction");
        Check(ChangeRelationAction.Changes.Count == 3, "founding preserves old leader/member and ruler penalties");
        Check(HasFact(request, f.Leader.Leader) && HasFact(request, f.Follower.Leader), "former leader and member receive affected-party fact");
        Check(HasFact(request, Clan.PlayerClan.Leader) && HasFact(request, f.Home.Leader), "player and ruler facts retained");
        Check(MyBehavior.MemoryFacts.Count == 4 && MyBehavior.FactKeys.Distinct().Count() == 4, "exactly four affected heroes, no duplicate facts");
        Check(MyBehavior.MemoryFacts.All(x => x.Contains("退出") && x.Contains("建立派系")), "shared fact describes both actual exit and founding");
        int facts = MyBehavior.MemoryFacts.Count, changes = ChangeRelationAction.Changes.Count;
        f.Owner.Execute(request, Clan.PlayerClan);
        Check(MyBehavior.MemoryFacts.Count == facts && ChangeRelationAction.Changes.Count == changes, "duplicate founding repeats neither effects nor old-member facts");
        f.Reload(); f.Owner.Execute(request, Clan.PlayerClan);
        Check(MyBehavior.MemoryFacts.Count == facts && ChangeRelationAction.Changes.Count == changes, "saved founding receipt remains idempotent");
        // The captured participants belong only to this action, not to future facts of the new faction.
        var own = f.State.Factions.Single(x => x.LeaderClanId == Clan.PlayerClan.StringId);
        var change = Request(f, CivilWarAction.ChangeDemand); change.FactionId = own.Id; change.DemandId = "autonomy";
        Check(f.Owner.Execute(change, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "new faction still supports demand changes");
        Check(!HasFact(change, f.Leader.Leader) && !HasFact(change, f.Follower.Leader), "former members are not leaked into future faction facts");

        f = Prewar(false); var player = f.State.Clans[Clan.PlayerClan.StringId]; player.Side = KingdomCivilWarSide.Crown; player.FactionId = "";
        var royalMember = f.State.Clans[f.Follower.StringId]; royalMember.Side = KingdomCivilWarSide.Crown; royalMember.FactionId = "";
        request = Request(f, CivilWarAction.Found);
        Check(f.Owner.Execute(request, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "crown supporter may found after crown exit");
        Check(HasFact(request, f.Follower.Leader), "former royal-side member also receives exit fact");
        Check(MyBehavior.FactKeys.Count(x => x == "civil_war:action:" + request.OperationId + ":" + f.Home.Leader.StringId) == 1, "ruler affected by two relation changes receives fact once");
        Check(!HasFact(request, f.Leader.Leader) && MyBehavior.MemoryFacts.Count == 3, "unaffected opposition clan is not included");

        f = Prewar(true); request = Request(f, CivilWarAction.Found);
        Check(f.Owner.Execute(request, Clan.PlayerClan).Status == CivilWarActionStatus.Applied, "player-led old faction can be dissolved and rebuilt");
        Check(HasFact(request, f.Leader.Leader) && HasFact(request, f.Follower.Leader), "dissolved old faction members receive rebuilding fact");
        Check(MyBehavior.MemoryFacts.All(x => x.Contains("解散") && x.Contains("建立派系")), "rebuilding fact describes actual dissolution and founding");
        return checks;
    }

    internal static int Run()
    {
        int checks = RunCooldown() + RunFacts();
        Console.WriteLine($"Player faction review fixes passed: {checks} checks (real owner, fake actions).");
        return checks;
    }
}
