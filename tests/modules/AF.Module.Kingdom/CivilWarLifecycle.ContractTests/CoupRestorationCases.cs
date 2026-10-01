using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

internal static class CoupRestorationCases
{
    private static void Restoration(Fixture f)
    {
        f.Faction.CoupId = "coup";
        f.Faction.DemandId = CivilWarCatalog.UsurpDemandId;
        f.Faction.WarGoal = (int)CivilWarWarGoal.Usurp;
        f.Faction.RestorationClanId = f.Follower.StringId;
        f.Faction.RestorationHeroId = f.Follower.Leader.StringId;
        f.Faction.RestorationKingdomName = "原王国";
        f.Faction.RestorationKingdomShortName = "原国";
        f.Home.DisplayName = "篡位后的名字";
    }

    internal static void Run(Action<bool, string> check)
    {
        var f = new Fixture(); Restoration(f);
        f.Faction.ResolutionOutcomeId = "rebels_usurp";
        var former = f.Follower.Leader;
        f.Tick(101);
        check(f.Home.RulingClan == f.Follower && f.Home.Leader == former, "restoration victory crowns old king instead of uprising organizer");
        check(f.Home.Name == "原王国" && f.Home.InformalName == "原国", "restoration victory restores full and short original names");
        check(f.Follower.Kingdom == f.Home && f.Leader.Kingdom == f.Home && f.Rebel.IsEliminated, "restoration returns participants and retires temporary rebel kingdom");
        check(f.State.Factions.Count == 0, "restoration victory settles faction");

        f = new Fixture(); Restoration(f); f.Faction.ResolutionOutcomeId = "rebels_usurp";
        f.Follower.Leader.IsAlive = false;
        f.Follower.Leader = new Hero { Clan = f.Follower, Id = "royal_heir" };
        f.Tick(101);
        check(f.Home.Leader == f.Follower.Leader && f.Home.Name == "原王国", "dead old king is succeeded by current royal clan leader");

        foreach (bool preselectedVictory in new[] { false, true })
        {
            f = new Fixture(); Restoration(f);
            var originalRuler = f.Home.RulingClan;
            f.Faction.ResolutionOutcomeId = preselectedVictory ? "rebels_usurp" : "";
            f.Faction.EndedByPeace = true;
            MakePeaceAction.Apply(f.Home, f.Rebel);
            f.Tick(101);
            check(f.Home.RulingClan == originalRuler && f.Home.Name == "篡位后的名字", "external peace never restores throne or name, preselected=" + preselectedVictory);
            check(f.State.Factions.Count == 0, "peace follows concession cleanup without restoration");
        }

        f = new Fixture(); Restoration(f);
        f.Faction.ResolutionOutcomeId = CivilWarCatalog.NegotiatedOutcomeId;
        var crown = f.Home.RulingClan; f.Tick(101);
        check(f.Home.RulingClan == crown && f.Home.Name == "篡位后的名字", "negotiated outcome while still at war cannot restore dynasty");

        f = new Fixture(); Restoration(f); f.Faction.ResolutionOutcomeId = "rebels_usurp";
        ChangeKingdomAction.FailClan = f.Leader.StringId;
        f.Tick(101);
        check(f.Faction.RestorationVictoryConfirmed && f.Follower.Kingdom == f.Home && f.Leader.Kingdom == f.Rebel, "victory locks before its own peace and partial return");
        check(!f.Home.IsAtWarWith(f.Rebel), "victory cleanup can legitimately make peace before restoration finishes");
        f.Reload(); f.Faction.EndedByPeace = true; ChangeKingdomAction.FailClan = null;
        f.Tick(102);
        check(f.Home.RulingClan == f.Follower && f.Home.Name == "原王国", "saved authorized victory resumes after own peace without becoming negotiation");

        f = new Fixture(); Restoration(f); f.Faction.ResolutionOutcomeId = "rebels_usurp";
        f.Follower.Leader.IsPrisoner = true; crown = f.Home.RulingClan;
        f.Tick(101);
        check(f.Home.RulingClan == crown && f.State.Factions.Count == 1 && ChangeKingdomAction.Moves == 0, "captured claimant is never freed or crowned by restoration effect");

        f = new Fixture(); // No restoration metadata represents a captured-king ordinary war.
        f.Faction.DemandId = CivilWarCatalog.UsurpDemandId; f.Faction.ResolutionOutcomeId = "rebels_usurp";
        f.Tick(101);
        check(f.Home.RulingClan == f.Leader && f.Home.Name == "home", "ordinary rebel victory remains organizer usurpation without royal restoration");

        f = new Fixture(); f.Owner.Replace(new KingdomCivilWarStorage());
        var request = new CoupCivilWarRegistration { CoupId = "register", KingdomId = f.Home.StringId, RebelKingdomId = f.Rebel.StringId,
            LeaderClanId = f.Leader.StringId, FormerClanId = f.Follower.StringId, FormerKingId = f.Follower.Leader.StringId,
            RestoreDynasty = true, OriginalName = "原王国", OriginalShortName = "原国" };
        check(f.Owner.TryRegisterCoupWar(request, out _), "real owner adopts already-created restoration war");
        check(f.Faction.Stage == KingdomCivilWarStage.OpenWar && f.Faction.WarClanIds.Contains(f.Follower.StringId), "registration includes royal family among real combatants");
        check(f.Owner.IsCivilWarPair(f.Home, f.Rebel), "registered coup uses existing civil-war pair tracking");
        check(f.Owner.TryRegisterCoupWar(request, out _) && f.State.Factions.Count == 1, "registration retry is idempotent");
        f.Reload();
        check(f.Faction.RestorationClanId == f.Follower.StringId && f.Owner.TryRegisterCoupWar(request, out _), "registration and restoration identities survive save reload");
        f.Faction.ResolutionOutcomeId = "rebels_usurp"; f.Tick(101);
        check(f.State.Factions.Count == 0 && f.Owner.TryRegisterCoupWar(request, out _) && f.State.Factions.Count == 0, "resolved registration receipt prevents re-creating a finished civil war");

        f = new Fixture(); Restoration(f); f.Home.RulingClan = Clan.PlayerClan; DuelSettings.PlayerFactionsAllowed = false;
        check(KingdomCivilWarOwner.CanTrackCoupWar(f.Home), "automatic player factions disabled does not prohibit explicit coup war registration");
        f.Faction.ResolutionOutcomeId = "rebels_usurp"; f.Tick(101);
        check(f.Home.RulingClan == f.Follower && f.Home.Name == "原王国", "registered coup victory resolves with default player faction setting off");
        f = new Fixture(); f.Home.RulingClan = Clan.PlayerClan; DuelSettings.PlayerFactionsAllowed = false;
        f.Tick(101);
        check(f.State.Factions.Count == 1 && f.Home.RulingClan == Clan.PlayerClan, "disabled ordinary player factions retain existing behavior");

        f = new Fixture(); f.Owner.Replace(new KingdomCivilWarStorage()); DuelSettings.Enabled = false;
        check(!f.Owner.TryRegisterCoupWar(request, out _), "disabled civil war rejects new registration");
    }
}
