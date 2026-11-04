using AnimusForge;
using TaleWorlds.Engine;
using TaleWorlds.Library;
static class Program
{
    static int checks;
    static void Check(bool ok,string name) {checks++;if(!ok)throw new Exception("FAIL "+name);}
    static BoundingBox Box(float x,float y,float z=3.5f)=>new() {min=new Vec3(-x,-y,0f),max=new Vec3(x,y,z)};
    static Scene SceneWith(string name,BoundingBox box,Func<Vec2,float> terrain=null)
    {GameEntity.Prefabs.Clear();GameEntity.Prefabs[name]=box;AnimusForge.Logger.Lines.Clear();return new Scene {Terrain=terrain??(_=>0f)};}
    static FieldCampLayout Layout(int variant=0,float scale=1f,Vec2? position=null)
    {var l=new FieldCampLayout();l.Props.Add(new() {Kind=FieldCampLayout.PropKind.Tent,CultureId="empire",Position=position??Vec2.Zero,Variant=variant,Scale=scale});return l;}
    static List<GameEntity> LiveTents(Scene s)=>s.Entities.Where(e=>!e.Removed && (e.Name.StartsWith("tent_")||e.Name=="village_tent_c")).ToList();
    static void Place(Scene s,FieldCampLayout l)=>FieldCampProps.Place(s,l,MatrixFrame.Identity,1f);
    static void Run()
    {
        var s=SceneWith("tent_vlandia_a",Box(5f,6f));Place(s,Layout());
        Check(LiveTents(s).Count==1,"wide native-style rope bounds fit an ordinary tent parcel instead of deleting the tent");
        var tent=LiveTents(s).Single();
        Check(tent.PhysicsEnabled && tent.SetFrameCalls==0,"solid tent is instantiated at final physical frame, not moved after body creation");
        Check(tent.Frame.rotation.s.Length*10f<=6.01f && tent.Frame.rotation.f.Length*12f<=8.01f,"mesh and collision are uniformly scaled with the actual parcel");
        float ordinaryScale=tent.Frame.rotation.s.Length;
        s=SceneWith("tent_vlandia_a",Box(5f,6f));Place(s,Layout(99,1.6f));
        Check(LiveTents(s).Count==1 && LiveTents(s)[0].Frame.rotation.s.Length>ordinaryScale,"command tent remains physically larger than a street tent");
        s=SceneWith("village_tent_c",Box(2.55f,2.76f),p=>0.12f*p.x+0.06f*p.y);Place(s,Layout());
        Check(LiveTents(s).Count==1,"ordinary sloping ground does not delete a tent over the old 22cm veto");
        tent=LiveTents(s).Single();Check(Math.Abs(tent.Frame.rotation.u.x)>0.01f,"tent frame actually follows the terrain plane");
        Check(s.TerrainQueries<200,"placement work stays bounded and initialization-only");
        var shifted=new BoundingBox {min=new Vec3(1f,-4f,-1f),max=new Vec3(7f,4f,3f)};
        s=SceneWith("tent_vlandia_b",shifted);Place(s,Layout(position:new Vec2(12f,8f)));
        Check(LiveTents(s).Count==1,"off-centre artist pivot and nonzero mesh bottom remain placeable");
        tent=LiveTents(s).Single();var basePoint=tent.Frame.TransformToParent(new Vec3(4f,0f,-1f));
        Check(basePoint.AsVec2.DistanceSquared(new Vec2(12f,8f))<0.001f && Math.Abs(basePoint.z)<0.04f,"real mesh base centre sits on intended ground rather than shifted or buried");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));var l=Layout();l.Props.Add(new() {Kind=FieldCampLayout.PropKind.Tent,CultureId="empire",Position=new Vec2(9f,0f),Scale=1f});Place(s,l);
        Check(LiveTents(s).Count==2,"multiple tent slots survive");Check(s.MeasurementSpawns==1,"prefab bounds measured once per initialization, not once per slot/orientation");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f),p=>p.x>=0f?3f:0f);Place(s,Layout());Check(LiveTents(s).Count==0,"cliff step still rejects physically unsafe tent support");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f),p=>float.NaN);Place(s,Layout());Check(LiveTents(s).Count==0,"non-finite terrain cannot create a physical tent");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));s.Water=1f;Place(s,Layout());Check(LiveTents(s).Count==0,"submerged support stays rejected");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f),p=>0.65f*p.x);Place(s,Layout());Check(LiveTents(s).Count==0,"steep hillside not forced into a tent floor");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));s.BlockDoor=true;Place(s,Layout());Check(LiveTents(s).Count==0,"closed entrance is not silently made walk-through");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f),p=>0.08f*p.y);GameEntity.Prefabs["bed_ground_a"]=Box(0.3f,0.65f,0.1f);l=Layout();l.Props.Add(new() {Kind=FieldCampLayout.PropKind.Bedroll,CultureId="empire",Position=new Vec2(-0.55f,-0.15f),TentAnchor=1});Place(s,l);
        Check(LiveTents(s).Count==1 && s.Entities.Count(e=>!e.Removed && e.Name=="bed_ground_a")==1,"furniture shares the supported tent frame under its roof");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f),p=>0.10f*p.x-0.06f*p.y);s.AuthoredDoorNormal=new Vec2(0f,-1f);l=Layout();var turned=l.Props[0];turned.Yaw=1.1f;l.Props[0]=turned;Place(s,l);
        Check(LiveTents(s).Count==1,"nonzero camp yaw and authored reverse doorway still find an accessible orientation on a slope");
        Check(AnimusForge.Logger.Lines.Any(x=>x.Contains("doorwayTurn=2")),"orientation trials inspect actual doorway before accepting a prefab");
        Check(s.MeasurementSpawns==1,"failed doorway rotations reuse the same measured prefab bounds");
        s=SceneWith("gallows",Box(2f,2f));GameEntity.Prefabs["bd_log_and_axe"]=Box(1f,1f);l=new FieldCampLayout();l.Props.Add(new() {Kind=FieldCampLayout.PropKind.Gallows});l.Props.Add(new() {Kind=FieldCampLayout.PropKind.ExecutionBlock});Place(s,l);
        Check(s.Entities.All(e=>e.Removed),"even legacy camp-layout apparatus records cannot pre-spawn a gallows or block");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));l=Layout();l.Props.Add(new() {Kind=FieldCampLayout.PropKind.Chest,TentAnchor=1,Position=new Vec2(0.58f,0f)});Place(s,l);
        Check(LiveTents(s).Count==1 && FieldCampProps.PlacedTentCount==FieldCampProps.RequestedTentCount,"missing optional furniture is not misreported as failed core camp initialization");
        Check(AnimusForge.Logger.Lines.Any(x=>x.Contains("skippedFurniture=1")),"optional furnishing loss is counted and logged, not hidden");
        Check(FieldCampProps.Place(s,l,MatrixFrame.Identity,1f)==1,"actual prop count excludes missing furniture");
        s=SceneWith("village_tent_c",Box(2.5f,2.7f));l=Layout();var foreign=l.Props[0];foreign.CultureId="khuzait";l.Props[0]=foreign;Place(s,l);
        Check(LiveTents(s).Count==1,"missing cultural tent falls back to a usable neutral prefab");
        FieldCampProps.ResetPlacementCounts();Check(FieldCampProps.PlacedTentCount==0&&FieldCampProps.RequestedTentCount==0,"failed-site initialization cannot display previous mission tent counts");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));l=Layout();GameEntity.Prefabs["crate_a"]=Box(1f,1f,1f);
        l.Props.Add(new() {Kind=FieldCampLayout.PropKind.Clutter,Position=new Vec2(0f,3.2f)});
        s.ExtraCollision=(from,to)=>s.Entities.Any(e=>!e.Removed&&e.Name=="crate_a");Place(s,l);
        Check(LiveTents(s).Count==0&&s.Entities.Any(e=>!e.Removed&&e.Name=="crate_a"),"later layout clutter is visible to every tent doorway trial");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));l=Layout();l.Props.Add(new(){Kind=FieldCampLayout.PropKind.Tent,CultureId="empire",Position=new Vec2(0f,6f),Scale=1f});
        s.ExtraCollision=(from,to)=>from.y<2f&&LiveTents(s).Count>1;Place(s,l);
        Check(LiveTents(s).Count==1&&Math.Abs(LiveTents(s)[0].Frame.origin.y)<0.01f,"a later tent that obstructs an accepted entrance is rolled back, preserving the first tent");
        Check(FieldCampProps.PlacedTentCount==1&&FieldCampProps.RequestedTentCount==2,"rejected trial does not claim both tents were placed");
        s=SceneWith("tent_vlandia_a",Box(2.5f,2.7f));l=Layout();s.MissingRoof=true;l.Props.Add(new(){Kind=FieldCampLayout.PropKind.Chest,TentAnchor=1});GameEntity.Prefabs["bd_chest_a"]=Box(0.2f,0.2f,0.5f);Place(s,l);
        Check(LiveTents(s).Count==1&&!s.Entities.Any(e=>!e.Removed&&e.Name=="bd_chest_a"),"furnishing support failure does not discard an accessible grounded tent");
        s=SceneWith("archery_practice_target",Box(0.5f,0.5f,1.7f));l=new FieldCampLayout();l.Props.Add(new(){Kind=FieldCampLayout.PropKind.Target,Position=new Vec2(6f,7f)});Place(s,l);
        Check(FieldCampProps.ArcheryTargets.Count==1 && s.Entities.Contains(FieldCampProps.ArcheryTargets[0]),"training target registry contains actual accepted entity, not guessed coordinates");
        FieldCampProps.ResetPlacementCounts();Check(FieldCampProps.ArcheryTargets.Count==0,"training targets never leak to the next mission");
        Console.WriteLine($"PASS {checks} production tent placement assertions (real Library math; fake native scene/prefab queries)");
    }
    static int Main() {try {Run();return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
