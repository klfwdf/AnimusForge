using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using AnimusForge;
namespace AnimusForge
{
    public sealed partial class FieldCampMissionLogic
    {
        internal static void RunPoseChecks(Action<bool,string> check)
        {
            MBActionSet.Cyclic.Clear(); MBActionSet.Missing.Clear(); MBActionSet.MetadataReads=0; MBActionSet.MissingSetTwo=false;
            var camp=new FieldCampMissionLogic(); var e=new CampAgent {Activity=Activity.Fire};
            check(camp.TryPlayCampPose(e,SitActions),"native ground sitting is accepted");
            check(e.Agent.Current.Name=="act_dungeon_prisoner_sitting_ground" && e.PoseTransition==CampPoseTransition.Enter,"arrival starts sit-down, not a random point in sitting loop");
            check((e.Agent.LastFlags & AnimFlags.anf_cyclic)==0 && e.Agent.LastProgress==0,"sit-down is one-shot from beginning");
            int sets=e.Agent.Sets;camp._elapsed=0.05f;camp.AdvanceCampPoseTransition(e);
            check(e.Agent.Sets==sets,"pending transition is not restarted");
            camp._elapsed=1f;e.Agent.Progress=0.98f;camp.AdvanceCampPoseTransition(e);
            check(e.PoseTransition==CampPoseTransition.None && e.Agent.Current.Name==SitActions[0],"sit-down completes into ground hold");
            check((e.Agent.LastFlags & AnimFlags.anf_cyclic)!=0 && e.Agent.LastProgress==0,"only explicit ground cycle is made cyclic, with a continuous entry");
            sets=e.Agent.Sets;for(int i=0;i<8;i++){e.PoseVariant++;camp.TryPlayCampPose(e,SitActions);camp.RefreshCampGroundRest(e);}
            check(e.Agent.Sets==sets && camp.Moves==0 && e.RestPose==CampRestPose.Sit,"refresh never inserts standing eating/talking or restarts the same seat");
            check(e.NextWanderTime==76f,"ground rest has a long bounded dwell rather than rapid pose alternation");
            check(camp.TryBeginCampPoseExit(e) && camp.Walks==0,"a real departure starts stand-up before walking");
            check(e.Agent.Current.Name=="act_dungeon_prisoner_sitting_ground_standup" && (e.Agent.LastFlags & AnimFlags.anf_cyclic)==0,"stand-up is not looped");
            camp._elapsed=1.2f;e.Agent.Progress=0.5f;camp.AdvanceCampPoseTransition(e);check(camp.Walks==0,"route waits during stand-up");
            e.Agent.Progress=0.98f;camp.AdvanceCampPoseTransition(e);
            check(camp.Walks==1 && e.RestPose==CampRestPose.None && e.Agent.Events[e.Agent.Events.Count-2]=="release" && e.Agent.Events.Last()=="walk","release completed pose immediately before starting prepared route");
            camp.AdvanceCampPoseTransition(e);check(camp.Walks==1,"finished stand-up never issues route twice");
            e=new CampAgent();camp.TryPlayCampPose(e,TalkActions);e.Agent.Current=new("act_talk_to_3");sets=e.Agent.Sets;
            check(IsCampPosePlaying(e),"native talk alternative counts as the same requested pose");
            e.PoseVariant++;camp.TryPlayCampPose(e,TalkActions);check(e.Agent.Sets==sets,"variant selection does not force a native alternative back to base");
            e=new CampAgent();camp.TryPlayCampPose(e,ListenActions);e.Agent.Current=new("act_listen_4");check(IsCampPosePlaying(e),"listener alternatives also remain valid");
            e.Agent.Current=ActionIndexCache.act_none;check(!IsCampPosePlaying(e),"truly finished action remains a completion signal");
            foreach(string a in TalkActions) MBActionSet.Cyclic[a]=false;
            camp=new FieldCampMissionLogic();e=new CampAgent();check(camp.TryPlayCampPose(e,TalkActions) && e.Agent.Current.Name.StartsWith("act_stand_"),"one-shot ambient talk is replaced with a calm native cyclic idle");
            check(!e.Agent.Events.Any(a=>a.StartsWith("act_talk_to_")),"non-looping gesture is never force-looped");
            MBActionSet.Cyclic.Clear();e=new CampAgent();camp.TryPlayCampPose(e,LoiterActions);
            check(!e.Agent.Events.Any(a=>a=="act_gossip"||a=="act_gossip_2"),"old repeated gossip gesture removed from loiterers");
            check(TalkActions.Length==5 && ListenActions.Length==4 && LoiterActions.Length>=4,"real distinct native idle/listener candidates, not random progress masquerading as variants");
            int reads=MBActionSet.MetadataReads;camp.TryPlayCampPose(e,LoiterActions);check(MBActionSet.MetadataReads==reads,"same action-set metadata read once, not on every upkeep");
            MBActionSet.MissingSetTwo=true;e.Agent.ActionSet=new MBActionSet{Id=2};check(!camp.TryPlayCampPose(e,StandActions),"missing female/other action set is not authorized by cached male metadata");MBActionSet.MissingSetTwo=false;
            e=new CampAgent();camp.TryPlayCampPose(e,LieActions);camp._elapsed+=0.2f;e.Agent.Progress=0.99f;camp.AdvanceCampPoseTransition(e);
            check(e.RestPose==CampRestPose.Lie && e.Agent.Current.Name==LieActions[0],"sleep entry also reaches its own ground cycle");
            camp.TryBeginCampPoseExit(e);check(e.Agent.Current.Name=="act_dungeon_prisoner_sleep_wakeup","sleeping residents wake instead of snapping to stand");
            e=new CampAgent();camp.TryPlayCampPose(e,SitActions);camp._elapsed=e.PoseTransitionDeadline+1;camp.AdvanceCampPoseTransition(e);
            check(e.PoseTransition==CampPoseTransition.None,"stalled transition cannot hold the resident forever");
            e=new CampAgent {Activity=Activity.Wounded};camp.TryPlayCampPose(e,SitActions);camp._elapsed+=0.2f;e.Agent.Progress=0.98f;camp.AdvanceCampPoseTransition(e);MBRandom.Roll=0.1f;camp.StrollAvailable=true;
            int moves=camp.Moves;camp.RefreshCampGroundRest(e);check(camp.Moves==moves,"wounded residents do not leave rest as a random variation");
            e.Activity=Activity.Fire;camp.RefreshCampGroundRest(e);check(camp.Moves==moves+1 && e.Returning && e.PoseTransition==CampPoseTransition.Exit,"healthy seated residents may take a real stroll through stand-up transition");
            MBRandom.Roll=0.8f;
            e=new CampAgent();camp.TryPlayCampPose(e,SitActions);check(HoldCampRestForConversation(e) && e.PoseTransition==CampPoseTransition.None && e.Agent.Current.Name==SitActions[0],"conversation takes over a sitting-down resident without resetting to standing");
            sets=e.Agent.Sets;check(HoldCampRestForConversation(e) && e.Agent.Sets==sets,"conversation acquisition preserves an already seated cycle without restarting it");
            camp.TryBeginCampPoseExit(e);check(!HoldCampRestForConversation(e),"a resident already departing is not mistaken for stationary seated speaker");
            e=new CampAgent();e.Agent.RejectActions=true;check(!camp.TryPlayCampPose(e,SitActions) && e.RestPose==CampRestPose.None && e.PoseAction==ActionIndexCache.act_none && e.PoseTransition==CampPoseTransition.None,"native action rejection never claims ownership of a sitting pose");
            e=new CampAgent();check(!camp.TryBeginCampPoseExit(e),"standing residents do not play ground stand-up");
            e.Agent.Current=ActionIndexCache.act_none;check(!IsCampActionPlaying(e.Agent,new("act_stand_1")),"empty native channel is not treated as an alternative");
        }
    }
}
internal static class Program
{
    static int checks;
    static int Main(){try{FieldCampMissionLogic.RunPoseChecks((ok,name)=>{checks++;if(!ok)throw new Exception("FAIL "+name);});Console.WriteLine($"PASS {checks} linked production camp pose assertions; native engine/animation visuals are doubles, live game NOT RUN");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
