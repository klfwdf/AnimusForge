using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
namespace TaleWorlds.Library
{
    public struct Vec2
    {
        public float x,y;
        public Vec2(float a,float b){x=a;y=b;}
        public float LengthSquared=>x*x+y*y; public float Length=>MathF.Sqrt(LengthSquared);
        public float DistanceSquared(Vec2 b)=>(this-b).LengthSquared;
        public static float DotProduct(Vec2 a,Vec2 b)=>a.x*b.x+a.y*b.y;
        public static Vec2 operator +(Vec2 a,Vec2 b)=>new(a.x+b.x,a.y+b.y);
        public static Vec2 operator -(Vec2 a,Vec2 b)=>new(a.x-b.x,a.y-b.y);
        public static Vec2 operator *(Vec2 a,float b)=>new(a.x*b,a.y*b);
    }
    public struct Vec3
    {
        public float x,y,z; public Vec3(float a,float b,float c=0){x=a;y=b;z=c;}
        public Vec2 AsVec2=>new(x,y);
        public static Vec3 operator -(Vec3 a,Vec3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
    }
}
namespace TaleWorlds.Engine
{
    public struct WeakGameEntity { public GameEntity Entity; }
    public sealed class GameEntity { public Vec3 GlobalPosition; public WeakGameEntity WeakEntity=>new(){Entity=this}; }
}
namespace TaleWorlds.Core
{
    public enum EquipmentIndex { Weapon2, Weapon3 }
    public sealed class ItemObject { public string Id; }
    public struct MissionWeapon { public ItemObject Item;public MissionWeapon(ItemObject item,object a,object b){Item=item;} }
}
namespace TaleWorlds.ObjectSystem
{
    public sealed class MBObjectManager
    {
        public static MBObjectManager Instance=new();public bool Available=true;
        public T GetObject<T>(string id) where T:class=>Available?new TaleWorlds.Core.ItemObject{Id=id} as T:null;
    }
}
namespace TaleWorlds.MountAndBlade
{
    public sealed class Agent
    {
        public static Agent Main; public int Index;public bool Active=true,Paused=true,Valid=true;
        public Vec3 Position;public float Speed;public int Shots;public TaleWorlds.Engine.GameEntity Target;
        public readonly List<string> Events=new();
        public readonly Dictionary<TaleWorlds.Core.EquipmentIndex,TaleWorlds.Core.MissionWeapon> Equipment=new()
        {
            [TaleWorlds.Core.EquipmentIndex.Weapon2]=new(new TaleWorlds.Core.ItemObject{Id="original_sword"},null,null),
            [TaleWorlds.Core.EquipmentIndex.Weapon3]=new(new TaleWorlds.Core.ItemObject{Id="original_shield"},null,null)
        };
        public enum WeaponWieldActionType {Instant} public enum AISpecialCombatModeFlags {None}
        public bool IsActive()=>Active;
        public void SetIsAIPaused(bool b){Paused=b;Events.Add("pause:"+b);}
        public void SetMaximumSpeedLimit(float s,bool isMultiplier){Speed=s;}
        public void EquipWeaponWithNewEntity(TaleWorlds.Core.EquipmentIndex slot,ref TaleWorlds.Core.MissionWeapon w){Equipment[slot]=w;Events.Add("equip:"+(w.Item?.Id??"empty"));}
        public void TryToWieldWeaponInSlot(TaleWorlds.Core.EquipmentIndex slot,WeaponWieldActionType a,bool b){Events.Add("wield");}
        public void SetScriptedTargetEntity(TaleWorlds.Engine.WeakGameEntity target,AISpecialCombatModeFlags f,bool b){Target=target.Entity;Shots++;Events.Add("target");}
    }
    public sealed class Mission
    {
        public static Mission Current;public bool IsMissionEnding;
        public readonly List<Agent> Nearby=new();public int SpatialQueries;
        public void GetNearbyAgents(Vec2 p,float r,List<Agent> results){SpatialQueries++;results.Clear();results.AddRange(Nearby);}
    }
}
namespace AnimusForge
{
    internal static class Logger{internal static void Log(string a,string b){} }
    internal static class FieldCampProps{internal static readonly List<TaleWorlds.Engine.GameEntity> ArcheryTargets=new();}
    public sealed partial class FieldCampMissionLogic
    {
        internal const string LogSource="FieldCamp";
        internal Mission Mission=Mission.Current;
        internal enum Activity{Work,Wounded}
        internal sealed class CampAgent
        {
            internal Agent Agent;internal Activity Activity;internal bool FollowingPlayer,Returning,Seated,MoveBlocked,RouteQueued,RoutePrepared,Training;
            internal bool PracticeEquipmentHeld; internal TaleWorlds.Core.MissionWeapon PreviousPracticeWeapon,PreviousPracticeAmmo;
            internal Vec3[] Route=Array.Empty<Vec3>();internal Vec3 Home;internal Vec2 HomeFacing;
            internal TaleWorlds.Engine.GameEntity TrainingTarget;internal float NextTrainingTime,NextWanderTime;
        }
        internal readonly Dictionary<Agent,CampAgent> _campAgentsByAgent=new();
        internal readonly List<Agent> _campNearbyAgents=new();
        internal float _elapsed;internal bool SlaughterBusy,_assembled,Duel,Ceremony,Envoy,Conversation,NavigationAvailable=true,StrollAvailable=true;
        internal int Restores,Releases,Moves,Stops,Poses;
        private static readonly string[] LookoutActions={"look"};
        private bool IsCampPrisoner(Agent a)=>false;private bool IsDuelist(Agent a)=>Duel;
        private bool IsCeremonyAgentHeld(Agent a)=>Ceremony;private bool IsEnvoy(CampAgent a)=>Envoy;
        private bool IsSceneConversationHeld(Agent a)=>Conversation;
        private void ReleasePose(CampAgent e){Releases++;e.Training=false;RestoreCampPracticeEquipment(e);e.Agent.Events.Add("release");}
        private void StopCampNavigation(Agent a){Stops++;}
        private bool TryPrepareCampNavigation(Agent a)=>NavigationAvailable;
        private void RestoreCampAttentionOwner(Agent a){Restores++;}
        private static bool WieldBow(Agent a)=>false;
        private void PlayLoop(CampAgent e,string[] pool){Poses++;}
        private bool TryNearbyStroll(CampAgent e,out Vec3 visit){visit=new Vec3(2,2);return StrollAvailable;}
        private void MoveTo(CampAgent e,Vec3 v,Vec2 f,bool run){Moves++;}
        internal CampAgent Add(Agent a){var e=new CampAgent{Agent=a,Home=a.Position,Seated=true};_campAgentsByAgent[a]=e;return e;}
        internal bool StartTraining(CampAgent e)=>TryStartCampArchery(e);
        internal void TickTraining(CampAgent e)=>UpkeepCampArchery(e);
    }
}
