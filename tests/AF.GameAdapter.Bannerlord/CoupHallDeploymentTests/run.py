"""Exercise the CURRENT production hall placement, allied-batch retry and defender-wave consumers.
Navmesh, SpawnAgent and UI doubles do not replace a live Bannerlord hall acceptance run.
"""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[3]
HOST=ROOT/'src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs'
OUT=ROOT/'artifacts/coup-hall-fix-20261002/harness'
text=HOST.read_text(encoding='utf-8-sig')
def method(signature):
 start=text.index(signature);opening=text.index('{',start);depth=1;end=opening+1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
methods=['private void TrySpawnSelectedAllies(','private void TrySpawnTimedDefenderReserveWave(',
 'private int CountActiveDefenderReserveWaves(','private void SpawnDefenderReserveWave(',
 'private void RemoveDefenderReserveEntries(','internal int CountArmedCoupRole(',
 'private List<Vec3> GetCoupHallSpawnCandidates(','private static bool TryProjectCoupHallSpawnPosition(',
 'private bool TryGetCoupHallSpawnPosition(','private void ObserveCoupHallDeployment(','internal void StopCoupStreetReinforcements(']
# Engine spawn itself is a double; verify its real loop obeys the tested prefix/final-position contract.
spawn=method('private int SpawnAgentsNearPlayer(')
assert 'asEnemy && !IsCoupHall && TryGetEnemyReserveSpawnFrames' in spawn
assert 'TryGetCoupHallSpawnPosition(mission, main, asEnemy, out position)' in spawn
assert 'if (!asEnemy) break;' in spawn
assert 'if (IsCoupHall && !asEnemy) break;' in spawn
assert 'wallSlot < 0 && !IsCoupHall' in spawn
assert 'if (_armedCoup && asEnemy && !IsCoupHall' in spawn
assert spawn.index('mission.SpawnAgent(buildData, false)') < spawn.index('_coupHallOccupiedSpawns.Add(position);')
assert 'LocationId = nextLocation?.StringId' in text and '_entryLocationId = entry?.LocationId' in text
mission=(ROOT/'extensions/AnimusForge.Coup/src/CoupSystem/CoupMissionBehavior.cs').read_text(encoding='utf-8-sig')
assert mission.index('_doorReady = _session.IsGateCleared') < mission.index('StopStreetReinforcements(Mission)')
assert 'if (_doorReady && !_streetReinforcementsStopped)' in mission
bridge=(ROOT/'extensions/AnimusForge.Coup/src/Integration/SettlementEntryTroopSelectionBehavior.cs').read_text(encoding='utf-8-sig')
assert 'Bind<Action<Mission>>(host, "StopArmedCoupStreetReinforcements")' in bridge
fixture=(Path(__file__).parent/'Fixture.cs.in').read_text(encoding='utf-8-sig')
fixture=fixture.replace('__METHODS__','\n'.join(method(s) for s in methods))
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'Program.cs').write_text(fixture,encoding='utf-8')
(OUT/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /></ItemGroup></Project>')
subprocess.run(['dotnet','run','--project',str(OUT/'Tests.csproj')],cwd=ROOT,check=True)
