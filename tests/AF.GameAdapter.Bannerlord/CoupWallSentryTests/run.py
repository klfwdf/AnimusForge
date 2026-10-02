"""Production wall-slot discovery and sentry lifecycle with deterministic game doubles.
Does not validate a live Bannerlord town's markers, weapon AI, or navigation mesh.
"""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[3]
SOURCE=ROOT/'src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs'
OUT=ROOT/'artifacts/coup-wall-sentries-20261002/harness'
text=SOURCE.read_text(encoding='utf-8-sig')
def method(signature):
    start=text.index(signature); opening=text.index('{',start); end=opening+1; depth=1
    while depth:
        depth+=(text[end]=='{')-(text[end]=='}');end+=1
    return text[start:end]
# Consumer and ownership checks in addition to executing production methods below.
consumer=text[text.index('private int SpawnAgentsNearPlayer('):]
assert 'mission.Scene != null && wallSlot < 0' in consumer
assert consumer.index('int wallSlot = -1;') < consumer.index('IsArmedCoupSpawnPositionSafe(mission.Scene, main.GetEyeGlobalPosition()') < consumer.index('mission.SpawnAgent(buildData, false)')
assert consumer.index('if (wallSlot >= 0) RegisterArmedCoupWallSentry') > consumer.index('spawnedDefenderEntries?.Add(defenderEntry)')
assert 'if (TryMaintainArmedCoupWallSentry(agent)) return;' in method('private void AssignEnemyAgentCombatTarget(')
assert 'if (TryMaintainArmedCoupWallSentry(agent)) return;' in method('private void MaintainEnemyAgentNativeCombat(')
assert '_armedCoupWallSentryHealth.Remove(affectedAgent.Index);' in text
assert '_armedCoupWallSentryHealth.Clear();' in text
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /></ItemGroup></Project>')
program=r'''
using System;using System.Collections.Generic;
struct Vec3 {
 public float x,y,z;public Vec3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
 public float DistanceSquared(Vec3 b){float dx=x-b.x,dy=y-b.y,dz=z-b.z;return dx*dx+dy*dy+dz*dz;}
}
struct MatrixFrame {public Vec3 origin;}
class GameEntity {public Vec3 Position;public GameEntity(float x,float y,float z){Position=new Vec3(x,y,z);}public MatrixFrame GetGlobalFrame()=>new MatrixFrame{origin=Position};}
class Scene {
 public Dictionary<string,List<GameEntity>> Markers=new Dictionary<string,List<GameEntity>>();
 public bool NavValid=true,Reachable=true;public float ProjectionZ;public int Reads,Paths;
 public IEnumerable<GameEntity> FindEntitiesWithTag(string tag){Reads++;return Markers.TryGetValue(tag,out var v)?v:new List<GameEntity>();}
 public bool GetPathDistanceBetweenPositions(ref WorldPosition a,ref WorldPosition b,float radius,out float distance){Paths++;distance=50;return Reachable;}
}
struct WorldPosition {
 Scene scene;Vec3 position;public WorldPosition(Scene s,Vec3 p){scene=s;position=p;}
 public UIntPtr GetNearestNavMesh()=>scene.NavValid?(UIntPtr)1:UIntPtr.Zero;
 public Vec3 GetNavMeshVec3()=>new Vec3(position.x,position.y,position.z+scene.ProjectionZ);
}
class Agent {
 public int Index=1;public float Health=100,Speed=-1;public Vec3 Position;public bool Active=true;
 public bool IsActive()=>Active;public void SetMaximumSpeedLimit(float value,bool multiplier){Speed=value;}
}
class Mission {public Scene Scene;public Agent MainAgent;}
class BaseLogic {public Mission Mission;}
enum SetsSettlementSceneKind {Town,Castle,Village}
static class SettlementEntryTroopSelectionLog {public static void Log(string message) {}}
class Program:BaseLogic {
 const int ArmedCoupSpawnMaxAnchors=48;
 bool IsCoupHall=false;bool _armedCoup=true;SetsSettlementSceneKind _sceneKind=SetsSettlementSceneKind.Town;
 List<Vec3> _armedCoupWallPositions;string _settlementId="test";
 Dictionary<int,float> _armedCoupWallSentryHealth=new Dictionary<int,float>();
 static int checks;static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
 static Scene SceneWith(params GameEntity[] markers){var scene=new Scene();scene.Markers["sp_guard"]=new List<GameEntity>(markers);return scene;}
 static void Main(){
  Check(IsArmedCoupWallTroopEligible(true,true,true,true,"StreetDefender"),"ranged street defender eligible");
  Check(!IsArmedCoupWallTroopEligible(false,true,true,true,"StreetDefender"),"normal entry excluded");
  Check(!IsArmedCoupWallTroopEligible(true,false,true,true,"StreetDefender"),"allies excluded");
  Check(!IsArmedCoupWallTroopEligible(true,true,false,true,"StreetDefender"),"hall/non-town excluded");
  Check(!IsArmedCoupWallTroopEligible(true,true,true,false,"StreetDefender"),"infantry excluded");
  Check(!IsArmedCoupWallTroopEligible(true,true,true,true,"GateGuard"),"gate objective role not reassigned");
  Check(!IsArmedCoupWallTroopEligible(true,true,true,true,null),"unknown role excluded");
  Check(ShouldReleaseArmedCoupWallSentry(100,99,400),"damage releases");
  Check(ShouldReleaseArmedCoupWallSentry(100,100,36),"six meter boundary releases");
  Check(!ShouldReleaseArmedCoupWallSentry(100,100,37),"unhurt distant sentry stays");
  var player=new Agent{Position=new Vec3(0,0,0)};
  var scene=SceneWith(new GameEntity(40,0,0),new GameEntity(40,0,8),new GameEntity(40.5f,0,8),new GameEntity(45,0,9));
  var mission=new Mission{Scene=scene,MainAgent=player};var owner=new Program{Mission=mission};
  var slots=owner.GetArmedCoupWallPositions(mission,player);
  Check(slots.Count==2,"ground guard excluded and duplicate slots merged");
  Check(slots[0].z==8,"wall height preserved");int reads=scene.Reads;
  Check(object.ReferenceEquals(slots,owner.GetArmedCoupWallPositions(mission,player))&&scene.Reads==reads,"discovery cached");
  scene=SceneWith(new GameEntity(40,0,8));scene.NavValid=false;
  owner=new Program();mission.Scene=scene;
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==0&&owner._armedCoupWallPositions==null,"navmesh not ready retries discovery");
  scene.NavValid=true;scene.ProjectionZ=-8;
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==0,"projection falling to street rejected");
  owner=new Program();scene.ProjectionZ=0;scene.Reachable=false;
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==0,"unreachable wall rejected");
  owner=new Program{_armedCoup=false};scene.Reachable=true;reads=scene.Reads;
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==0&&scene.Reads==reads,"non-coup avoids discovery");
  owner=new Program{_sceneKind=SetsSettlementSceneKind.Castle};
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==0,"castle/hall avoids deployment");
  scene=SceneWith();for(int i=0;i<80;i++)scene.Markers["sp_guard"].Add(new GameEntity(40+i*3,0,8));mission.Scene=scene;owner=new Program();
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==12&&scene.Paths==12,"twelve-slot work cap");
  scene=SceneWith();for(int i=0;i<80;i++)scene.Markers["sp_guard"].Add(new GameEntity(40+i*3,0,0));scene.Markers["sp_guard_with_spear"]=new List<GameEntity>{new GameEntity(40,0,8)};mission.Scene=scene;owner=new Program();
  Check(owner.GetArmedCoupWallPositions(mission,player).Count==0&&scene.Reads==1,"marker inspection bounded");
  owner=new Program{Mission=mission};var sentry=new Agent{Index=9,Position=new Vec3(40,0,8),Speed=0};owner._armedCoupWallSentryHealth[9]=100;
  Check(owner.TryMaintainArmedCoupWallSentry(sentry)&&sentry.Speed==0,"held archer remains combat-active stationary");
  sentry.Health=90;Check(!owner.TryMaintainArmedCoupWallSentry(sentry)&&sentry.Speed==-1&&!owner._armedCoupWallSentryHealth.ContainsKey(9),"hit permanently releases movement");
  Check(!owner.TryMaintainArmedCoupWallSentry(sentry),"released sentry not re-held");
  owner._armedCoupWallSentryHealth[9]=90;sentry.Position=new Vec3(0,0,5);sentry.Speed=0;
  Check(!owner.TryMaintainArmedCoupWallSentry(sentry)&&sentry.Speed==-1,"player arriving on wall releases sentry");
  Console.WriteLine("PASS: "+checks+" production wall discovery/eligibility/lifecycle checks + consumer wiring");
 }
__METHODS__
}
'''
program=program.replace('__METHODS__','\n'.join(method(s) for s in ['private static bool IsArmedCoupWallTroopEligible(','private static bool ShouldReleaseArmedCoupWallSentry(','private bool TryMaintainArmedCoupWallSentry(','private List<Vec3> GetArmedCoupWallPositions(']))
(OUT/'Program.cs').write_text(program)
subprocess.run(['dotnet','run','--project',str(OUT/'Tests.csproj')],cwd=ROOT,check=True)
