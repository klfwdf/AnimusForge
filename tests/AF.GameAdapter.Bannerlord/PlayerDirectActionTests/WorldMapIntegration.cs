using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

internal static class WorldMapIntegration
{
    internal static void Run(Action<bool,string> check)
    {
        Hero actor=null; CharacterObject character=null;
        void Reset(bool scene=false)
        {
            WorldMapPartyCommandBehavior.Reset(); InformationManager.Messages.Clear();
            Campaign.Current=new(); Mission.Current=scene?new(){Mode=MissionMode.StartUp}:null; Settlement.CurrentSettlement=null;
            MeetingBattleRuntime.IsMeetingActive=MeetingBattleRuntime.IsCombatEscalated=MeetingBattleRuntime.Duel=false;
            LordEncounterBehavior.IsEncounterMeetingMissionActive=MilitaryExerciseBehavior.Active=TroopInspectionBehavior.Active=PlayerEncounterCompat.Battle=AIConfigHandler.Restricted=false;
            ShoutBehavior.CurrentInstance=new(); ShoutBehavior.MainThread=true; ShoutBehavior.Busy=false;
            actor=new(){StringId="lord",Clan=new()};character=new(){HeroObject=actor};
            actor.PartyBelongedTo=new(){StringId="lord_party",LeaderHero=actor};
            actor.PartyBelongedTo.MemberRoster.Elements.Add(new(){Character=actor.CharacterObject,Number=1});
            Hero.AllAliveHeroes.AddRange(new[]{actor,Hero.MainHero}); MobileParty.All.AddRange(new[]{actor.PartyBelongedTo,MobileParty.MainParty});
            ShoutBehavior.Target=actor;ShoutBehavior.TargetCharacter=character;
            if(scene) Mission.Current.Agents.Add(new(){Index=7,Character=character});
            Settlement.All.AddRange(new[]{new Settlement{StringId="town",Name="Same",IsTown=true},new Settlement{StringId="village",Name="Same",IsVillage=true},new Settlement{StringId="castle",IsCastle=true},new Settlement{StringId="hideout",IsHideout=true,Ready=true}});
        }
        void Open()=>check(ShoutBehavior.OpenNativePlayerDirectActions(()=>{}),"real direct host admits context");
        void Select(object id)
        {
            var p=(MultiSelectionInquiryData)InformationManager.Popup;
            var e=p.Elements.Single(x=>Equals(x.Identifier,id)||x.Identifier is PlayerDirectActionChoice choice&&choice.Token==id as string);
            check(e.IsEnabled,"selected action/target enabled: "+id); p.AffirmativeAction(new(){e});
        }
        void Input(string value="")=>((TextInquiryData)InformationManager.Popup).AffirmativeAction(value);
        void Default()=>((MultiSelectionInquiryData)InformationManager.Popup).AffirmativeAction(new());
        void Confirm()=>((InquiryData)InformationManager.Popup).AffirmativeAction();
        void Go(bool replace=false) {Open();Select(PlayerDirectActionKind.GoToSettlement);Select("town");Input("2");if(replace)Select("replace");else Default();}
        Reset();Open();Select(PlayerDirectActionKind.Follow);Input();Default();Confirm();
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ACTION:WORLDMAP_ORDER:FOLLOW:hero:player:1]"&&ShoutBehavior.CurrentInstance.QueueCalls==0,"no Mission uses same follow button with real map admission");
        foreach(var meeting in new[]{"lord","runtime","lock"})
        {
            Reset(true); Mission.Current.Mode=MissionMode.Battle;
            if(meeting=="lord")LordEncounterBehavior.IsEncounterMeetingMissionActive=true;
            if(meeting=="runtime")MeetingBattleRuntime.IsMeetingActive=true;
            if(meeting=="lock")Mission.Current.Behaviors.Add(new MeetingBattleLockMissionBehavior());
            Open();check(((MultiSelectionInquiryData)InformationManager.Popup).Elements.Single(x=>Equals(x.Identifier,PlayerDirectActionKind.Follow)).Hint.Contains("WORLDMAP_ORDER"),"meeting Agent does not choose scene template");
            Select(PlayerDirectActionKind.Follow);Input();Default();Confirm();
            check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single().Contains("WORLDMAP_ORDER")&&ShoutBehavior.CurrentInstance.QueueCalls==0,"all existing meeting predicates route map follow: "+meeting);
        }
        Reset(true);Go();Confirm();
        check(WorldMapPartyCommandBehavior.Instance.Queues[actor.StringId].Commands.Single().TargetId=="town"&&ShoutBehavior.CurrentInstance.CloseCalls==0,"eligible scene lord submits real map task without losing scene follow");
        Reset(true);Open();Select(PlayerDirectActionKind.Follow);
        check(ShoutBehavior.CurrentInstance.QueueCalls==1&&ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"ordinary scene keeps original Agent follow");
        foreach(var restriction in new[]{"battle","escalated","exercise","inspection","duel","deployment"})
        {
            Reset(true);MeetingBattleRuntime.IsMeetingActive=restriction!="battle";
            if(restriction=="battle")Mission.Current.Mode=MissionMode.Battle;
            if(restriction=="escalated")MeetingBattleRuntime.IsCombatEscalated=true;
            if(restriction=="exercise"){MilitaryExerciseBehavior.Active=true;Mission.Current.Behaviors.Add(new MilitaryExerciseMissionLogic());}
            if(restriction=="inspection")TroopInspectionBehavior.Active=true;
            if(restriction=="duel")MeetingBattleRuntime.Duel=true;
            if(restriction=="deployment")Mission.Current.Mode=MissionMode.Deployment;
            Open();var menu=(MultiSelectionInquiryData)InformationManager.Popup;
            check(menu.Elements.Where(x=>x.Identifier is PlayerDirectActionKind k&&(PlayerDirectActionProtocol.IsWorldMap(k,PlayerDirectActionContext.WorldMap)))
                .All(x=>!x.IsEnabled),"restricted mission cannot degrade into map order: "+restriction);
        }
        Reset();actor.PartyBelongedTo=null;Open();
        check(!((MultiSelectionInquiryData)InformationManager.Popup).Elements.Single(x=>Equals(x.Identifier,PlayerDirectActionKind.GoToSettlement)).IsEnabled,"missing source displays disabled reason");
        Reset(true);actor.IsNotable=true;Open();
        check(!((MultiSelectionInquiryData)InformationManager.Popup).Elements.Single(x=>Equals(x.Identifier,PlayerDirectActionKind.GoToSettlement)).IsEnabled,"ordinary notable does not acquire map controls");
        Reset();Open();Select(PlayerDirectActionKind.GoToSettlement);var targets=(MultiSelectionInquiryData)InformationManager.Popup;
        check(targets.SearchAvailable&&targets.Elements.Where(x=>x.Name.StartsWith("Same")).Select(x=>x.Name).Distinct().Count()==2
            &&targets.Elements.Any(x=>x.Name.Contains("Town [town]"))&&targets.Elements.Any(x=>x.Name.Contains("Village [village]")),"real same-name targets distinguished by type, ID and native search enabled");
        Select("town");Input();Default();Settlement.All.RemoveAll(x=>x.StringId=="town");Confirm();
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"removed real target cannot dispatch");
        foreach(var command in new[]{PlayerDirectActionKind.PatrolSettlement,PlayerDirectActionKind.FollowTarget,PlayerDirectActionKind.AttackTarget,PlayerDirectActionKind.ClearHideout,PlayerDirectActionKind.MergeToPlayer})
        {
            Reset();actor.Clan=Clan.PlayerClan;Open();Select(command);
            if(command!=PlayerDirectActionKind.MergeToPlayer)Select(command==PlayerDirectActionKind.FollowTarget?"main":command==PlayerDirectActionKind.ClearHideout?"hideout":"village");
            if(command==PlayerDirectActionKind.AttackTarget)Select("FORCE");Input();Default();Confirm();
            string raw=ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single();
            check(raw.Contains(command==PlayerDirectActionKind.AttackTarget?":village:5:FORCE":command==PlayerDirectActionKind.ClearHideout?":hideout:5":":1]"),"real candidate defaults and existing executor: "+command);
        }
        foreach(var (id,type,days) in new[]{("player","hero",1),("main","party",1),("town","settlement",15),("castle","settlement",15),("village","settlement",5)})
        {
            Reset();Open();Select(PlayerDirectActionKind.AttackTarget);Select(id);Default();Input();Default();Confirm();
            check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()==$"[ACTION:WORLDMAP_ORDER:ATTACK:{type}:{id}:{days}:AI]","actual attack candidate type/default/mode: "+id);
        }
        foreach(var step in new[]{"target","mode","days","queue","confirm"})
        {
            Reset();Open();Select(PlayerDirectActionKind.AttackTarget);
            if(step!="target")Select("town");
            if(step is "days" or "queue" or "confirm")Default();
            if(step is "queue" or "confirm")Input();
            if(step=="confirm")Default();
            if(InformationManager.Popup is MultiSelectionInquiryData pick)pick.NegativeAction(null);
            else if(InformationManager.Popup is TextInquiryData input)input.NegativeAction();
            else ((InquiryData)InformationManager.Popup).NegativeAction();
            check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0&&WorldMapPartyCommandBehavior.Instance.Queues.Count==0,"cancel at each map parameter stage leaves owner untouched: "+step);
        }
        Reset();Go();Confirm();Go(true);var final=(InquiryData)InformationManager.Popup;final.AffirmativeAction();final.AffirmativeAction();
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==2&&WorldMapPartyCommandBehavior.Instance.Queues[actor.StringId].Commands.Count==1,"host submits STOP+new in a single normal chain, once");
        Go(true);var old=WorldMapPartyCommandBehavior.Instance.Queues[actor.StringId];actor.PartyBelongedTo.MapEvent=new();Confirm();
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==2&&ReferenceEquals(old,WorldMapPartyCommandBehavior.Instance.Queues[actor.StringId]),"battle final preflight never sends STOP");
        Reset();Go();MeetingBattleRuntime.IsCombatEscalated=true;Confirm();check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"captured context cannot change while parameters open");
        Reset();Open();Select(PlayerDirectActionKind.StopFollow);Confirm();check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ACTION:WORLDMAP_ORDER:STOP]","map stop button uses normal owner and explicit wholequeue confirmation");
        foreach(var role in new[]{"companion","governor"})
        {
            Reset(true);actor.InMainParty=role=="companion";actor.PartyBelongedTo=actor.InMainParty?MobileParty.MainParty:null;if(role=="governor")actor.GovernorOf=new();
            if(role=="governor")WorldMapPartyCommandBehavior.SetGovernor(actor);
            Go();Confirm();check(ShoutBehavior.CurrentInstance.CloseCalls==1&&ShoutBehavior.CurrentInstance.ExitCalls==1,"actual owner needs-exit result closes current native dialogue and mission: "+role);
        }
        Reset(true);actor.InMainParty=true;actor.PartyBelongedTo=MobileParty.MainParty;MeetingBattleRuntime.IsMeetingActive=true;
        Open();Select(PlayerDirectActionKind.Follow);Input();Default();Confirm();
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==1&&ShoutBehavior.CurrentInstance.ExitCalls==1,"companion can follow main hero via implicit new task party");
        Reset(true);actor.InMainParty=true;actor.PartyBelongedTo=MobileParty.MainParty;Go();var owner=ShoutBehavior.CurrentInstance;
        owner._nativeGameEffects.AfterDispatch=()=>Campaign.Current.ConversationManager.ActiveToken++;Confirm();
        check(owner.CloseCalls==0&&owner.ExitCalls==0,"late owner exit receipt cannot close a newer dialogue token");
        Reset();Go(true);ShoutBehavior.CurrentInstance._nativeGameEffects.ResultOverride=new(){HadTag=true,StopApplied=true};Confirm();
        check(InformationManager.Messages.Last().StartsWith("STOP was processed")&&ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==1,"partial receipt is not reported as successful action and is never retried");
        Reset();
    }
}
