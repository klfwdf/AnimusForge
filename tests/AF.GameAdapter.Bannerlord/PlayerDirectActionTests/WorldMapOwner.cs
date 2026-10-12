using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

// Source-linked admission/protocol/manual preflight and extracted complete queue methods.
// Original roster/merge/governor qualification and delayed request queues are linked too.
// TaleWorlds state, AI navigation, native party screens and materialization remain boundaries.
internal static class WorldMapOwner
{
    internal static void Run(Action<bool, string> check)
    {
        WorldMapPartyCommandBehavior.Reset();
        Campaign.Current = new();
        var hero = new Hero { StringId = "lord", Clan = new() };
        var party = new MobileParty { StringId = "lord_party", LeaderHero = hero };
        hero.PartyBelongedTo = party;
        Hero.AllAliveHeroes.Add(hero); MobileParty.All.Add(party);
        var town = new Settlement { StringId = "town", IsTown = true };
        Settlement.All.Add(town);
        var actor = WorldMapPartyCommandBehavior.CapturePlayerDirectOrderActor(hero, new() { HeroObject = hero }, -1);
        const string go = "[ACTION:WORLDMAP_ORDER:GO_TO_SETTLEMENT:settlement:town:2]";
        const string replace = "[ACTION:WORLDMAP_ORDER:STOP] " + go;
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor, go) == null, "source-linked owner preflight accepts real location");
        string raw = go;
        var result = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero, ref raw);
        check(result.Handled && result.AddedCommandCount == 1 && raw == "", "production admission and queue algorithm accept and strip tag");
        var old = WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId];
        foreach (var condition in new[] { "battle", "pending-stop", "follow-siege", "protected" })
        {
            party.MapEvent = condition == "battle" ? new object() : null;
            old.PendingStop = condition == "pending-stop"; old.FollowSiege = condition == "follow-siege";
            old.SourceId = condition == "protected" ? "external_owner" : "";
            check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor, replace) != null
                && ReferenceEquals(old, WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId]) && old.Commands.Count == 1,
                "rejected replacement leaves original owner queue untouched: " + condition);
        }
        party.MapEvent = null; old.PendingStop = old.FollowSiege = false; old.SourceId = "";
        raw = go; WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero, ref raw);
        check(old.Commands.Count == 2, "real queue method appends without clearing");
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor, replace) == null, "safe replacement preflight");
        raw = replace; result = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero, ref raw);
        check(result.Handled && result.StopApplied && result.AddedCommandCount == 1
            && WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId].Commands.Count == 1, "real STOP plus command replaces once");
        var candidates = WorldMapPartyCommandBehavior.CapturePlayerDirectOrderTargets(actor, "GO_TO_SETTLEMENT");
        var target = candidates.Single(x => x.Id == "town");
        Settlement.All.Remove(town);
        check(!target.IsCurrent() && WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor, replace) != null,
            "removed destination is rejected before STOP"); Settlement.All.Add(town);
        hero.PartyBelongedTo = new() { LeaderHero = hero };
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor, go) != null, "party-source replacement invalidates captured actor");
        hero.PartyBelongedTo = party;
        town.Hostile = true; raw = go; result = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero, ref raw);
        check(WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId].Commands.Last().Kind == "AttackHero", "original hostile-location conversion retained"); town.Hostile = false;
        hero.InMainParty = true; hero.PartyBelongedTo = MobileParty.MainParty;
        actor = WorldMapPartyCommandBehavior.CapturePlayerDirectOrderActor(hero, new() { HeroObject = hero }, -1);
        raw = go; result = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero, ref raw);
        check(result.CompanionPartyCreationQueued && result.NeedsChannelExit, "real companion admission returns exit request");
        check(WorldMapPartyCommandBehavior.Instance.HasPendingCompanion(hero.StringId)
            && WorldMapPartyCommandBehavior.Instance.PendingCompanionCommands(hero.StringId)==1,"original companion owner queued actual task payload");
        raw=go;WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero,ref raw);
        check(WorldMapPartyCommandBehavior.Instance.PendingCompanionCommands(hero.StringId)==2,"companion append retains original pending task payloads");
        check(WorldMapPartyCommandBehavior.CapturePlayerDirectOrderTargets(actor,"FOLLOW",true).Single().Id==Hero.MainHero.StringId
            && !WorldMapPartyCommandBehavior.CapturePlayerDirectOrderTargets(actor,"FOLLOW").Any(x=>x.Type=="hero"&&x.Id==hero.StringId),"implicit source follows player but cannot select itself");
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,"[ACTION:WORLDMAP_ORDER:STOP] [ACTION:WORLDMAP_ORDER:FOLLOW:hero:lord:1]")!=null,"self-target denied before STOP even for implicit party creation");
        hero.InMainParty = false; WorldMapPartyCommandBehavior.SetGovernor(hero);
        actor = WorldMapPartyCommandBehavior.CapturePlayerDirectOrderActor(hero, new() { HeroObject = hero }, -1);
        raw = go; result = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero, ref raw);
        check(result.GovernorExpeditionCreationQueued && result.NeedsChannelExit, "real governor admission returns exit request");
        check(WorldMapPartyCommandBehavior.Instance.HasPendingGovernor(hero.StringId)
            && WorldMapPartyCommandBehavior.Instance.PendingGovernorCommands(hero.StringId)==1,"original governor owner stored ticketed expedition task payload");
        raw=go;WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero,ref raw);
        check(WorldMapPartyCommandBehavior.Instance.PendingGovernorCommands(hero.StringId)==2,"governor append preserves pending tasks under the same ticket");
        var garrison=hero.GovernorOf.GarrisonParty;
        check(garrison.MemberRoster.Elements.Single().Number==100,"governor qualification and queued request do not consume troops prematurely");
        var prior=WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId];
        foreach(var condition in new[]{"cannot-lead","siege","troop-reserve","wounded","away"})
        {
            var origin=hero.GovernorOf.Settlement;
            hero.CanLead=condition!="cannot-lead";origin.IsUnderSiege=condition=="siege";hero.CurrentSettlement=condition=="away"?town:origin;
            garrison.MemberRoster.Elements[0]=new(){Character=new(){StringId="regular"},Number=condition=="troop-reserve"?45:100,WoundedNumber=condition=="wounded"?95:0};
            check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,replace)!=null
                && ReferenceEquals(prior,WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId])
                && WorldMapPartyCommandBehavior.Instance.PendingGovernorCommands(hero.StringId)==2,"real governor eligibility rejects replacement without clearing tasks: "+condition);
        }
        hero.CanLead=true;hero.CurrentSettlement=hero.GovernorOf.Settlement;hero.GovernorOf.Settlement.IsUnderSiege=false;
        var nonHero = new CharacterObject(); var bandit = new MobileParty { StringId="bandit" }; WorldMapPartyCommandBehavior.NonHeroParty = bandit;
        actor = WorldMapPartyCommandBehavior.CapturePlayerDirectOrderActor(null, nonHero, 9);
        raw = go; result = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(null, nonHero, 9, ref raw);
        check(result.Handled && WorldMapPartyCommandBehavior.Instance.Queues.ContainsKey("party:" + bandit.StringId), "real non-hero admission and queue method");
        check(WorldMapPartyCommandBehavior.GetPlayerDirectOrderUnavailableReason(actor, "MERGE_TO_PLAYER") != null, "non-hero merge remains prohibited");
        bandit.LeaderHero=hero;
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,replace)!=null,"non-hero cannot take over a hero-led party under original qualification");bandit.LeaderHero=null;
        var hideout = new Settlement { StringId = "hideout", IsHideout = true, Ready = true }; Settlement.All.Add(hideout);
        check(WorldMapPartyCommandBehavior.CapturePlayerDirectOrderTargets(actor, "CLEAR_HIDEOUT").Count == 1, "eligible hideout offered");
        hideout.Ready = false;
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor, "[ACTION:WORLDMAP_ORDER:STOP] [ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout:5]") != null,
            "hideout eligibility rechecked before destructive prefix");
        hideout.Ready=true;
        foreach(var restriction in new[]{"unknown","quest","battle","time","interface"})
        {
            hideout.Hideout.IsSpotted=restriction!="unknown";hideout.QuestBusy=restriction=="quest";
            hideout.Party.MapEvent=restriction=="battle"?new object():null;hideout.Hideout.NextPossibleAttackTime.IsPast=restriction!="time";
            WorldMapNpcHideoutCompletionPatch.IsReady=restriction!="interface";
            check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,"[ACTION:WORLDMAP_ORDER:STOP] [ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout:5]")!=null,"source-linked hideout target qualification preserves old queue: "+restriction);
        }
        WorldMapNpcHideoutCompletionPatch.IsReady=true;hideout.Hideout.IsSpotted=true;hideout.QuestBusy=false;hideout.Party.MapEvent=null;hideout.Hideout.NextPossibleAttackTime.IsPast=true;
        var banditState=WorldMapPartyCommandBehavior.Instance.Queues["party:"+bandit.StringId];
        banditState.Commands[0]=new(){Kind="ClearHideout",TargetType="settlement",TargetId="hideout"};banditState.EngageCommitted=true;bandit.MapEvent=new FinalizedMapEvent();
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,replace)!=null
            &&ReferenceEquals(banditState,WorldMapPartyCommandBehavior.Instance.Queues["party:"+bandit.StringId]),"committed hideout ownership cannot be replaced before native finalization even when map event is finalized");
        bandit.MapEvent=null;
        hero.GovernorOf=null;hero.Clan=Clan.PlayerClan;hero.PartyBelongedTo=party;
        actor=WorldMapPartyCommandBehavior.CapturePlayerDirectOrderActor(hero,new(){HeroObject=hero},-1);
        const string merge="[ACTION:WORLDMAP_ORDER:STOP] [ACTION:WORLDMAP_ORDER:MERGE_TO_PLAYER:1]";
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,merge)!=null,"missing hero in independent roster cannot clear queue before rejected merge");
        party.MemberRoster.Elements.Add(new(){Character=hero.CharacterObject,Number=1});
        check(WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(actor,merge)==null,"original merge qualifies player family/companion with authoritative roster membership");
        raw=merge;result=WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(hero,ref raw);
        check(result.Handled&&WorldMapPartyCommandBehavior.Instance.Queues[hero.StringId].Commands.Single().Kind=="MergeToPlayer","original queue accepts eligible merge after STOP");
    }
}
