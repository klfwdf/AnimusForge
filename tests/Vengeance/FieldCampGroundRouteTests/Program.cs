using AnimusForge;
static class Program
{
    static int checks;
    static void Check(bool ok,string why) {checks++;if(!ok)throw new Exception(why);}
    static FieldCampGroundRouter Grid(int w,int h,out bool[] cells,out float[] heights)
    {cells=Enumerable.Repeat(true,w*h).ToArray();heights=new float[w*h];return new(w,h,cells,heights);}
    static void Validate(FieldCampGroundRouter router,int w,int start,int goal,int[] corners)
    {
        Check(corners.Length>0 && corners[0]==start && corners[^1]==goal,"complete route preserves actual endpoints");
        Check(corners.Length<=FieldCampGroundRouter.MaxCorners,"bounded route corner count");
        for(int i=1;i<corners.Length;i++)
        {
            int a=corners[i-1],b=corners[i];int dx=b%w-a%w,dy=b/w-a/w;
            Check(dx==0 || dy==0,"only real straight grid corridor segments, never diagonal corner cuts");
            int step=dx==0?Math.Sign(dy)*w:Math.Sign(dx);
            for(int p=a;p!=b;p+=step) Check(router.IsTraversable(p),"route never traverses a runtime tent/fence/body cell");
        }
    }
    static void Main()
    {
        var router=Grid(15,15,out var cells,out var heights);
        int start=7*15+3,goal=7*15+11;
        // Native baked navmesh would permit a straight walk; the runtime fence has one real gate at y=2.
        for(int y=0;y<15;y++)if(y!=2)cells[y*15+7]=false;
        Check(router.TryRoute(start,goal,out var path),"runtime fence is routed through its actual opening");
        Check(path.Length>2,"a collision-aware route cannot collapse to the blocked direct line");Validate(router,15,start,goal,path);
        Check(path.Any(p=>p/15==2),"waypoints actually visit the gate row");
        cells[2*15+7]=false;
        Check(!router.TryRoute(start,goal,out path) && path.Length==0,"closed fence remains a blocked deployment, not a fake success/teleport");
        cells[2*15+7]=true;
        Check(!router.TryRoute(start,goal,out path, minY:3), "camp bounds reject a detour through an outside gate");
        Check(router.TryRoute(start,goal,out path, minX:1, minY:1, maxX:13, maxY:13), "inner camp gate remains reachable");
        Check(path.All(p=>p%15>=1 && p%15<=13 && p/15>=1 && p/15<=13), "bounded route corners stay inside camp");
        Check(!router.TryRoute(start,goal,out path, maxX:10), "goal outside bounds is rejected");
        Check(!router.TryRoute(start,goal,out path, minX:4), "start outside bounds is rejected");
        Check(!router.TryRoute(start,goal,out path, minX:8, maxX:6), "inverted bounds are rejected");
        Check(router.TryRoute(start,goal,out path),"shared scratch supports later routes without stale visited marks");
        Validate(router,15,start,goal,path);
        router=Grid(2,2,out cells,out heights);cells[1]=cells[2]=false;
        Check(!router.TryRoute(0,3,out path),"cannot squeeze diagonally through two touching obstacles");
        router=Grid(3,2,out cells,out heights);cells[1]=cells[4]=false;
        Check(!router.TryRoute(2,3,out path),"linear indices cannot wrap the end of a row as a fake path");
        router=Grid(4,1,out cells,out heights);heights[2]=2f;heights[3]=2f;
        Check(!router.TryRoute(0,3,out path),"cliff height discontinuity remains blocked even if navmesh says both endpoints exist");
        router=Grid(4,1,out cells,out heights);heights[0]=float.NaN;
        Check(!router.TryRoute(0,0,out path),"invalid start height cannot report immediate arrival");
        Check(!router.TryRoute(-1,3,out path)&&!router.TryRoute(0,9,out path),"invalid endpoint indices fail safely");
        router=Grid(6,6,out cells,out heights);
        Check(router.TryRoute(14,14,out path)&&path.Length==1&&path[0]==14,"actor already at goal needs no invented movement");
        for(int i=0;i<20;i++)Check(router.TryRoute(i,35-i,out path),"generation-stamped scratch can be reused across actors");
        router=Grid(256,256,out cells,out heights);
        Check(!router.TryRoute(0,65535,out path)&&router.LastSearchVisited==FieldCampGroundRouter.SearchBudget,"large open grid obeys explicit work budget instead of a full-scene sweep");
        bool rejected=false;try{new FieldCampGroundRouter(1000,1000,new bool[1],new float[1]);}catch(ArgumentException){rejected=true;}
        Check(rejected,"oversized/malformed scene grid cannot allocate unbounded scratch");
        Console.WriteLine($"PASS {checks} production camp ground-route assertions (pure collision-grid algorithm; native engine not started)");
    }
}
