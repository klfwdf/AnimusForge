using AnimusForge;
using AnimusForge.Refactor.Modules;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;

internal static class EligibilityAndDecisionTests
{
    internal static int Run()
    {
        int count=0;
        void Check(bool pass,string message){ count++;if(!pass)throw new Exception(message); }
        Fixture Realm()
        {
            var f=new Fixture();
            ChangeKingdomAction.Move(f.Leader,f.Home);ChangeKingdomAction.Move(f.Follower,f.Home);
            f.Owner.Replace(new KingdomCivilWarStorage());TeamModuleServices.CivilWar.Load("");
            return f;
        }
        float Points(Fixture f,string source)
        {
            var storage=JsonConvert.DeserializeObject<KingdomCivilWarStorage>(TeamModuleServices.CivilWar.Save());
            return storage.Kingdoms.TryGetValue(f.Home.StringId,out var k)&&k.Clans.TryGetValue(f.Follower.StringId,out var c)&&c.Grievance.TryGetValue(source,out var n)?n:0;
        }
        var f=Realm();var player=Clan.PlayerClan;
        player.IsMinorFaction=true;
        Check(CivilWarWorld.IsPoliticalClan(player),"native minor player metadata must not reject sworn vassal");
        Check(CivilWarWorld.LandedClans(f.Home).Contains(player),"player contributes to political strength and panel roster");
        Check(!f.Owner.BuildPlayerKingdomPanel().Identity.Contains("非正式封臣"),"actual panel identifies minor-tagged player as vassal");
        var join=new CivilWarActionRequest{OperationId="minor-vassal-join",KingdomId=f.Home.StringId,Action=CivilWarAction.JoinCrown};
        Check(f.Owner.Quote(join,player).Allowed,"actual action quote permits minor-tagged sworn player");
        f.Owner.AddGrievance(f.Home,"fief_lost",new[]{player},20,100,"player fief lost");
        Check(f.Owner.Storage.Kingdoms[f.Home.StringId].Clans[player.StringId].Grievance["fief_lost"]==10,"player event consumer shares corrected eligibility");
        player.IsClanTypeMercenary=true;
        Check(CivilWarWorld.IsPoliticalClan(player),"player origin mercenary type does not replace current contract status");
        player.IsUnderMercenaryService=true;
        Check(!CivilWarWorld.IsPoliticalClan(player)&&!CivilWarWorld.LandedClans(f.Home).Contains(player),"active player mercenary contract remains excluded");
        Check(!f.Owner.Quote(join,player).Allowed,"mercenary cannot act as a vassal");
        player.IsUnderMercenaryService=false;player.IsBanditFaction=true;
        Check(!CivilWarWorld.IsPoliticalClan(player),"bandit player not promoted by exception");
        player.IsBanditFaction=false;player.IsEliminated=true;
        Check(!CivilWarWorld.IsPoliticalClan(player),"eliminated player remains excluded");
        player.IsEliminated=false;player.Leader.IsAlive=false;
        Check(!CivilWarWorld.LandedClans(f.Home).Contains(player)&&!f.Owner.Quote(join,player).Allowed,"dead leader cannot act as a political vassal");
        player.Leader.IsAlive=true;
        ChangeKingdomAction.Move(player,f.Rebel);
        Check(!f.Owner.Quote(join,player).Allowed,"player exception cannot authorize actions in a foreign kingdom");
        f=Realm();f.Follower.IsMinorFaction=true;
        Check(!CivilWarWorld.IsPoliticalClan(f.Follower),"NPC minor faction remains excluded");
        f.Follower.IsMinorFaction=false;f.Follower.IsClanTypeMercenary=true;
        Check(!CivilWarWorld.IsPoliticalClan(f.Follower),"NPC mercenary clan type remains excluded");
        f.Follower.IsClanTypeMercenary=false;f.Follower.IsUnderMercenaryService=true;
        Check(!CivilWarWorld.IsPoliticalClan(f.Follower),"NPC current mercenary contract remains excluded");

        var behavior=new CivilWarCampaignBehavior();
        foreach(var status in new[]{KingdomDecision.SupportStatus.Majority,KingdomDecision.SupportStatus.Equal,KingdomDecision.SupportStatus.Minority})
        {
            f=Realm();var policy=new PolicyObject{StringId="policy",Name="policy"};f.Home.ActivePolicies.Add(policy);
            var decision=new KingdomPolicyDecision{Kingdom=f.Home,Policy=policy,SupportStatusOfFinalDecision=status};
            behavior.OnKingdomDecisionConcluded(decision,new KingdomPolicyDecision.PolicyDecisionOutcome{ShouldDecisionBeEnforced=true},false);
            Check(Points(f,"policy_imposed")== (status==KingdomDecision.SupportStatus.Minority?4:0),"policy imposed only for minority final decision: "+status);

            f=Realm();var war=new DeclareWarDecision{Kingdom=f.Home,SupportStatusOfFinalDecision=status};
            var warResult=new DeclareWarDecision.DeclareWarDecisionOutcome{Kingdom=f.Home,ShouldWarBeDeclared=true,FactionToDeclareWarOn=f.Rebel};
            behavior.OnWarDeclared(f.Home,f.Rebel,DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision);
            Check(Points(f,"war_imposed")==0,"raw war callback cannot guess unresolved vote: "+status);
            behavior.OnKingdomDecisionConcluded(war,warResult,false);
            Check(Points(f,"war_imposed")== (status==KingdomDecision.SupportStatus.Minority?4:0),"minority war recorded once at conclusion: "+status);

            f=Realm();MakePeaceAction.Apply(f.Home,f.Rebel);
            var peace=new MakePeaceKingdomDecision{Kingdom=f.Home,SupportStatusOfFinalDecision=status};
            var peaceResult=new MakePeaceKingdomDecision.MakePeaceDecisionOutcome{Kingdom=f.Home,ShouldPeaceBeDeclared=true,FactionToMakePeaceWith=f.Rebel};
            behavior.OnMakePeace(f.Home,f.Rebel,MakePeaceAction.MakePeaceDetail.ByKingdomDecision);
            Check(Points(f,"peace_imposed")==0,"raw peace callback defers vote grievance: "+status);
            behavior.OnKingdomDecisionConcluded(peace,peaceResult,false);
            Check(Points(f,"peace_imposed")== (status==KingdomDecision.SupportStatus.Minority?3.5f:0),"minority peace recorded once at conclusion: "+status);
        }
        f=Realm();
        var warDecision=new DeclareWarDecision{Kingdom=f.Home,SupportStatusOfFinalDecision=KingdomDecision.SupportStatus.Minority};
        var warOutcome=new DeclareWarDecision.DeclareWarDecisionOutcome{Kingdom=f.Home,ShouldWarBeDeclared=false,FactionToDeclareWarOn=f.Rebel};
        behavior.OnKingdomDecisionConcluded(warDecision,warOutcome,false);
        Check(Points(f,"war_imposed")==0,"vote against war is not an imposed war");
        MakePeaceAction.Apply(f.Home,f.Rebel);warOutcome.ShouldWarBeDeclared=true;
        behavior.OnKingdomDecisionConcluded(warDecision,warOutcome,false);
        Check(Points(f,"war_imposed")==0,"failed/nonexecuted war cannot produce grievance");
        f=Realm();var peaceDecision=new MakePeaceKingdomDecision{Kingdom=f.Home,SupportStatusOfFinalDecision=KingdomDecision.SupportStatus.Minority};
        var peaceOutcome=new MakePeaceKingdomDecision.MakePeaceDecisionOutcome{Kingdom=f.Home,ShouldPeaceBeDeclared=true,FactionToMakePeaceWith=f.Rebel};
        behavior.OnKingdomDecisionConcluded(peaceDecision,peaceOutcome,false);
        Check(Points(f,"peace_imposed")==0,"nonexecuted peace cannot produce grievance while still at war");
        MakePeaceAction.Apply(f.Home,f.Rebel);peaceOutcome.ShouldPeaceBeDeclared=false;
        behavior.OnKingdomDecisionConcluded(peaceDecision,peaceOutcome,false);
        Check(Points(f,"peace_imposed")==0,"rejected peace vote is not an imposed peace");
        f=Realm();behavior.OnWarDeclared(f.Home,f.Rebel,DeclareWarAction.DeclareWarDetail.Default);
        Check(Points(f,"war_imposed")==4,"non-vote direct crown war retains old rule");
        f=Realm();MakePeaceAction.Apply(f.Home,f.Rebel);behavior.OnMakePeace(f.Home,f.Rebel,MakePeaceAction.MakePeaceDetail.Default);
        Check(Points(f,"peace_imposed")==3.5f,"non-vote direct peace retains old rule");
        Console.WriteLine("PASS player eligibility + forced-vote grievance: "+count+" assertions; production owners and exact source-span event methods with fake game context");
        return count;
    }
}
