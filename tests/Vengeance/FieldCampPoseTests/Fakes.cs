using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
namespace TaleWorlds.Core
{
    public static class MBRandom { public static float Roll = 0.8f; public static float RandomFloat => Roll; public static float RandomFloatRanged(float min,float max) => (min+max)*0.5f; }
}
namespace TaleWorlds.MountAndBlade
{
    [Flags] public enum AnimFlags : ulong { anf_lock_movement=1, anf_cyclic=2 }
    public readonly struct ActionIndexCache : IEquatable<ActionIndexCache>
    {
        public readonly string Name; public ActionIndexCache(string n) {Name=n;}
        public static readonly ActionIndexCache act_none=new(null);
        public bool Equals(ActionIndexCache a)=>Name==a.Name;
        public override bool Equals(object a)=>a is ActionIndexCache x && Equals(x);
        public override int GetHashCode()=>Name?.GetHashCode()??0;
        public static bool operator ==(ActionIndexCache a,ActionIndexCache b)=>a.Equals(b);
        public static bool operator !=(ActionIndexCache a,ActionIndexCache b)=>!a.Equals(b);
        public override string ToString()=>Name;
    }
    public struct MBActionSet
    {
        public int Id; public override int GetHashCode()=>Id;
        public static readonly Dictionary<string,bool> Cyclic=new();
        public static readonly HashSet<string> Missing=new();
        public static int MetadataReads; public static bool MissingSetTwo;
        public static bool CheckActionAnimationClipExists(MBActionSet s,in ActionIndexCache a) { MetadataReads++;return !Missing.Contains(a.Name) && !(s.Id==2 && MissingSetTwo); }
        public static AnimFlags GetActionAnimationFlags(MBActionSet s,in ActionIndexCache a)=>Cyclic.TryGetValue(a.Name,out bool c)&&!c ? 0 : AnimFlags.anf_cyclic;
        public static float GetActionAnimationDuration(MBActionSet s,in ActionIndexCache a)=>2f;
        private static string Group(string s)=>s.StartsWith("act_talk_to_")?"talk":s.StartsWith("act_listen_")?"listen":s.StartsWith("act_stand_")?"stand":s;
        public bool AreActionsAlternatives(in ActionIndexCache a,in ActionIndexCache b)=>Group(a.Name)==Group(b.Name);
    }
    public sealed class Agent
    {
        public int Index=4; public MBActionSet ActionSet; public float Progress; public ActionIndexCache Current=ActionIndexCache.act_none;
        public readonly List<string> Events=new(); public bool RejectActions;public int Sets;public AnimFlags LastFlags;public float LastProgress,LastBlend,LastSpeed;
        public ActionIndexCache GetCurrentAction(int channel)=>Current;
        public float GetCurrentActionProgress(int channel)=>Progress;
        public bool SetActionChannel(int channel,in ActionIndexCache a,bool ignorePriority=false,AnimFlags flags=0,float blendWithNextActionFactor=0,float actionSpeed=1,float blendInPeriod=-0.2f,float blendOutPeriodToNoAnim=0.4f,float startProgress=0)
        { if(RejectActions)return false;Sets++;Current=a;Progress=startProgress;LastFlags=flags;LastProgress=startProgress;LastBlend=blendInPeriod;LastSpeed=actionSpeed;Events.Add(a.Name??"none");return true; }
    }
}
namespace AnimusForge
{
    internal static class Logger { internal static void Log(string source,string message){} }
    public sealed partial class FieldCampMissionLogic
    {
        private const string LogSource="FieldCamp";
        private enum Activity { Fire,Work,Wounded,Wander }
        private sealed class CampAgent
        {
            internal Agent Agent=new();internal int PoseVariant;internal ActionIndexCache PoseAction=ActionIndexCache.act_none;
            internal CampRestPose RestPose;internal CampPoseTransition PoseTransition;
            internal ActionIndexCache TransitionAction=ActionIndexCache.act_none;
            internal float PoseTransitionStartedAt,PoseTransitionDeadline,NextWanderTime;
            internal bool Returning;internal bool Seated=true;internal Activity Activity;internal Vec2 HomeFacing;
            internal int RestVariant;internal bool ReseatAfterExit;
        }
        private static ActionIndexCache ResolveAction(string name)=>new(name);
        private static void HideHandItems(CampAgent e){}
        private static void ShowHandItems(CampAgent e,string name){}
        private static void ReleasePose(CampAgent e) {e.Agent.Events.Add("release");e.PoseAction=ActionIndexCache.act_none;e.RestPose=CampRestPose.None;e.PoseTransition=CampPoseTransition.None;}
        private void IssueCampWalk(CampAgent e) {Walks++;e.Agent.Events.Add("walk");}
        private bool TryNearbyStroll(CampAgent e,out Vec3 p) {p=new Vec3(3,0,0);return StrollAvailable;}
        private void MoveTo(CampAgent e,Vec3 p,Vec2 f,bool run){Moves++;TryBeginCampPoseExit(e);}
        private float _elapsed;private bool StrollAvailable;private int Moves,Walks;
        private static int walksBefore(FieldCampMissionLogic c)=>c.Walks;
        private void ApplyPose(CampAgent e){e.Agent.Events.Add("apply");}
    }
}
