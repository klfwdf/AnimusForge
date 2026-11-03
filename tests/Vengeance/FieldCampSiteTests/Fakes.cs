using TaleWorlds.Library;
namespace TaleWorlds.Engine
{
    public struct BoundingBox { } // Only a cache key/value type in this site-only fixture.
    [Flags] public enum BodyFlags { Disabled=1, DoNotCollideWithRaycast=2, CommonCollisionExcludeFlagsForAgent=2 }
    public struct WeakGameEntity { public static WeakGameEntity Invalid=>default;public UIntPtr Pointer; public bool IsValid=>Pointer!=UIntPtr.Zero; }
    public sealed class Path { public int NumberOfPoints=>0; public float GetTotalLength()=>0; public MatrixFrame GetFrameForDistance(float d)=>MatrixFrame.Identity; }
    public struct WorldPosition
    {
        Scene scene; Vec3 point;
        public WorldPosition(Scene s,UIntPtr p,Vec3 v,bool b){scene=s;point=v;}
        public float GetNavMeshZ()=>scene.Terrain(point.AsVec2);
    }
    public sealed class Scene
    {
        public Func<Vec2,float> Terrain=_=>0; public Func<Vec2,bool> Navigable=_=>true, Blocked=_=>false;
        public readonly List<GameEntity> Entities=new(); public float Water=-100; public int Queries,NavRejects;
        public float GetTerrainHeight(Vec2 p,bool b){Queries++;return Terrain(p);}
        public float GetWaterLevelAtPosition(Vec2 p,bool a,bool b)=>Water;
        public void GetNavMeshFaceIndex(ref PathFaceRecord f,Vec3 p,bool b){bool valid=Navigable(p.AsVec2);f.FaceIndex=valid?0:-1;if(!valid)NavRejects++;}
        public Path GetPathWithName(string n)=>null;
        public void GetSceneLimits(out Vec3 min,out Vec3 max){min=new Vec3(-300,-300,0);max=new Vec3(300,300,0);}
        public void GetBoundingBox(out Vec3 min,out Vec3 max)=>GetSceneLimits(out min,out max);
        public void GetEntities(ref List<GameEntity> entities)=>entities.AddRange(Entities);
        public bool RayCastForClosestEntityOrTerrain(Vec3 from,Vec3 to,out float distance,out Vec3 hit,out WeakGameEntity entity,float radius,BodyFlags flags)
        {distance=1;hit=from;entity=default;return Blocked(from.AsVec2);}
    }
    public sealed class GameEntity
    {
        public string Name;public UIntPtr Pointer;public Vec3 GlobalBoxMin,GlobalBoxMax,GlobalPosition;public bool Hidden,Disabled;
        public int ChildCount=>0;public GameEntity GetChild(int c)=>throw new IndexOutOfRangeException();
        public bool IsVisibleIncludeParents()=>!Hidden;public void SetVisibilityExcludeParents(bool b)=>Hidden=!b;
        public void AddBodyFlags(BodyFlags f,bool applyToChildren)=>Disabled=true;public void SetPhysicsState(bool isEnabled,bool setChildren){}
    }
}
namespace TaleWorlds.MountAndBlade
{
    public sealed class Mission
    {
        public static Mission Current;public TaleWorlds.Engine.Scene Scene;
        public AgentBuildData LastBuild; public Agent SpawnAgent(AgentBuildData data){LastBuild=data;NativeCalls.Events.Add("spawn");return new Agent();}
        public void SetReportStuckAgentsMode(bool mode){NativeCalls.Events.Add("stuck_report");}
        public bool GetPatchSceneEncounterPosition(out Vec3 p){p=Vec3.Invalid;return false;}
        public bool IsPositionInsideBoundaries(Vec2 p)=>p.x>=-300&&p.x<=300&&p.y>=-300&&p.y<=300;
    }
}
namespace AnimusForge
{
    internal static class Logger{internal static readonly List<string> Lines=new();internal static void Log(string s,string text)=>Lines.Add(text);}
    internal sealed class FieldCampLayout
    {
        internal enum PropKind{Tent}
        internal struct Prop{internal PropKind Kind;internal Vec2 Position;}
        internal struct Slot{internal Vec2 Position;}
        internal float HalfX=36.75f,Back=-34,Front=30;internal Vec2 PlayerStart=new Vec2(0,12);
        internal readonly List<Slot> HorseSlots=new();internal readonly List<Prop> Props=new();
    }
}

namespace AnimusForge
{
    internal static class FieldCampProps
    {
        // These site cases have no tent assets; do not pretend to validate native prefab support.
        internal static bool CanSupportTents(TaleWorlds.Engine.Scene scene, FieldCampLayout layout, MatrixFrame frame, Dictionary<string, TaleWorlds.Engine.BoundingBox> measured)
        { if (layout.Props.Count != 0) throw new NotSupportedException("Tent geometry needs the prop-placement harness/native assets."); return true; }
    }
}
