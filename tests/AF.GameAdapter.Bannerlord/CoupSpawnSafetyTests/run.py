"""Execute the production final-position guard with deterministic scene/vector doubles.
This is an offline algorithm test, not a Bannerlord scene/navmesh acceptance test.
"""
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs'
OUT = ROOT / 'artifacts/coup-spawn-safety-20261002/guard-harness'
text = SOURCE.read_text(encoding='utf-8-sig')

def method(name):
    start = text.index('\t\tprivate static bool ' + name + '(')
    opening = text.index('{', start)
    depth = 1
    end = opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

# Ensure the tested guard protects the real consumer after final grounding and before spawn.
consumer = text[text.index('\t\tprivate int SpawnAgentsNearPlayer('):]
ground = consumer.index('position.z = mission.Scene.GetGroundHeightAtPosition(position);')
guard = consumer.index('&& (!main.IsActive() || !IsArmedCoupSpawnPositionSafe(')
spawn = consumer.index('mission.SpawnAgent(buildData, false)')
assert ground < guard < spawn
assert 'if (_armedCoup && asEnemy' in consumer[ground:guard]
assert 'continue;' in consumer[guard:spawn]
assert consumer.index('spawnedDefenderEntries?.Add(defenderEntry)') > spawn
assert 'private const float ArmedCoupSpawnMinDistance = 25f;' in text
assert 'private const float ArmedCoupHallSpawnMinDistance = 12f;' in text
OUT.mkdir(parents=True, exist_ok=True)
(OUT / 'Guard.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /></ItemGroup></Project>')
program = r'''
using System;
struct Vec3 {
 public float x,y,z;
 public Vec3(float x,float y,float z=0) {this.x=x;this.y=y;this.z=z;}
 public float LengthSquared => x*x+y*y+z*z;
 public static Vec3 operator -(Vec3 a,Vec3 b) => new Vec3(a.x-b.x,a.y-b.y,a.z-b.z);
 public float Distance(Vec3 b) => (float)Math.Sqrt((this-b).LengthSquared);
}
class Scene {
 public bool Visible, Throw;
 public int Calls;
 public bool CheckPointCanSeePoint(Vec3 a,Vec3 b,float d) {Calls++;if(Throw)throw new Exception();return Visible;}
}
class Program {
 static int checks;
 static void Check(bool v,string name) {if(!v)throw new Exception(name);checks++;}
 static void Main() {
  var s=new Scene();var p=new Vec3(0,0);var eye=new Vec3(0,0,1.6f);
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(24,0),25),"grid offset crosses street exclusion");
  Check(s.Calls==0,"near positions skip raycast");
  Check(IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(25,0),25),"street boundary allowed when hidden");
  Check(IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(40,0),25),"far hidden allowed");
  s.Visible=true;
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(40,0),25),"projection exposes troop at corner");
  s.Visible=false;s.Throw=true;
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(40,0),25),"raycast exception fails closed");
  s.Throw=false;
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(2,0,40),25),"vertical separation does not bypass exclusion");
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(11.9f,0),12),"hall too near");
  Check(IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(12,0),12),"hall boundary allowed hidden");
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(float.NaN,0),25),"invalid candidate rejected");
  Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(float.PositiveInfinity,0),25),"infinite candidate rejected");
  Check(!IsArmedCoupSpawnPositionSafe(null,eye,p,new Vec3(40,0),25),"missing scene fails closed");
  p=new Vec3(20,0);Check(!IsArmedCoupSpawnPositionSafe(s,eye,p,new Vec3(40,0),25),"player movement rechecked");
  Console.WriteLine("PASS: "+checks+" production guard checks + consumer wiring assertions");
 }
__METHODS__
}
'''
program = program.replace('__METHODS__', method('IsArmedCoupSpawnPositionSafe') + '\n' + method('IsArmedCoupAnchorVisible'))
(OUT / 'Program.cs').write_text(program)
subprocess.run(['dotnet','run','--project',str(OUT/'Guard.csproj')],cwd=ROOT,check=True)
