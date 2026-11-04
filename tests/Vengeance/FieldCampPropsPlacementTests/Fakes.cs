using TaleWorlds.Library;
namespace TaleWorlds.Engine
{
    public struct BoundingBox { public Vec3 min, max; }
    [Flags] public enum BodyFlags { None=0, Disabled=1, Dynamic=2, Moveable=4, DoNotCollideWithRaycast=8, CommonCollisionExcludeFlagsForAgent=9 }
    public struct WeakGameEntity { public UIntPtr Pointer; public bool IsValid => Pointer != UIntPtr.Zero; }
    public sealed class Scene
    {
        public Func<Vec2,float> Terrain = _=>0f; public float Water = -100f; public bool BlockDoor, MissingRoof; public Vec2? AuthoredDoorNormal;
        public Func<Vec3,Vec3,bool> ExtraCollision;
        public readonly List<GameEntity> Entities = new(); public int TerrainQueries, MeasurementSpawns;
        public float GetTerrainHeight(Vec2 p,bool exclude) { TerrainQueries++;return Terrain(p); }
        public float GetWaterLevelAtPosition(Vec2 p,bool a,bool b) => Water;
        public bool RayCastForClosestEntityOrTerrain(Vec3 from,Vec3 to,out float d,out Vec3 hit,out WeakGameEntity entity,float radius,BodyFlags flags)
        {
            d=0f;hit=to;entity=default;
            bool horizontal=from.AsVec2.DistanceSquared(to.AsVec2)>0.01f;
            if (horizontal && ExtraCollision?.Invoke(from,to)==true) return true;
            if (horizontal && AuthoredDoorNormal.HasValue)
            {
                var root=Entities.LastOrDefault(e=>!e.Removed && (e.Name.StartsWith("tent_")||e.Name=="village_tent_c"));
                if(root!=null)
                {
                    Vec2 local=AuthoredDoorNormal.Value;Vec3 normal=root.Frame.rotation.TransformToParent(new Vec3(local.x,local.y,0f));
                    Vec3 direction=to-from;
                    float dot=normal.x*direction.x+normal.y*direction.y;
                    if(dot<0.9f*normal.AsVec2.Length*direction.AsVec2.Length)return true;
                }
            }
            if (horizontal && (BlockDoor || from.z < Terrain(from.AsVec2)+radius || to.z < Terrain(to.AsVec2)+radius)) return true;
            if (!horizontal && from.z>to.z && !MissingRoof)
            {
                var roof=Entities.LastOrDefault(e=>!e.Removed && e.Name.StartsWith("tent_"));
                if (roof==null) roof=Entities.LastOrDefault(e=>!e.Removed && e.Name=="village_tent_c");
                if (roof!=null) { entity=new WeakGameEntity {Pointer=roof.Pointer};return true; }
            }
            return false;
        }
    }
    public sealed class GameEntity
    {
        public enum Mobility { Stationary }
        public static readonly Dictionary<string,BoundingBox> Prefabs = new(); static uint next;
        public string Name; public UIntPtr Pointer; public bool Removed, PhysicsEnabled; public int SetFrameCalls; public BodyFlags BodyFlag;
        public MatrixFrame Frame, InitialFrame; BoundingBox box;
        public int ChildCount=>0; public GameEntity GetChild(int i)=>throw new IndexOutOfRangeException();
        public static bool PrefabExists(string n)=>Prefabs.ContainsKey(n);
        public static GameEntity Instantiate(Scene s,string n,MatrixFrame f,bool callScript)
        { if(!PrefabExists(n))return null;var e=new GameEntity {Name=n,box=Prefabs[n],Pointer=(UIntPtr)(++next),Frame=f,InitialFrame=f};s.Entities.Add(e);return e; }
        public static GameEntity Instantiate(Scene s,string n,bool physics,bool script)
        {s.MeasurementSpawns++;return Instantiate(s,n,MatrixFrame.Identity,script);}
        public BoundingBox GetLocalBoundingBox()=>box;
        public void SetGlobalFrame(in MatrixFrame f) {Frame=f;SetFrameCalls++;}
        public void Remove(int reason)=>Removed=true;
        public void SetMobility(Mobility m) { }
        public void RemoveBodyFlags(BodyFlags f,bool applyToChildren)=>BodyFlag &= ~f;
        public void SetPhysicsState(bool isEnabled,bool setChildren)=>PhysicsEnabled=isEnabled;
        public bool HasBody()=>true;
    }
}
namespace AnimusForge
{
    internal static class Logger { internal static List<string> Lines=new();internal static void Log(string source,string line)=>Lines.Add(line); }
    internal sealed class FieldCampLayout
    {
        internal enum PropKind { Tent, Fire, Flag, Supply, Rack, Bedroll, Spit, Cauldron, Bench, Woodpile, Clutter, Cart, Chest, Torch, Target, TrainingRack, HorseBarrier, Trough, HayCart, Standard, Wall, Table, Anvil, WeaponHeap, Bed, ExecutionBlock, Gallows, Bag, Hearth, Rug, Ash, Dishes, Food, Gear, Wreck }
        internal struct Prop { internal PropKind Kind;internal string CultureId;internal Vec2 Position,End;internal float Yaw,Scale;internal int Variant,TentAnchor,StackOn; }
        internal readonly List<Prop> Props=new();
    }
}
