"""AF gate + checked-in native 1.5.6 end-condition methods, with fake spawner/team/time. Not a game test."""
from pathlib import Path
import json,hashlib,argparse,subprocess
ap=argparse.ArgumentParser();ap.add_argument('--dotnet',default='dotnet');ap.add_argument('--output');args=ap.parse_args()
root=Path(__file__).resolve().parents[4];out=Path(args.output) if args.output else root/'.tmp/duel-native-end-replay';out.mkdir(parents=True,exist_ok=True);p=root/'原版游戏本体代码1.5.6/TaleWorlds.MountAndBlade/TaleWorlds/MountAndBlade/BattleEndLogic.cs';s=p.read_text(encoding='utf-8-sig');af=root/'src/modules/AF.Module.Duel/Host/DuelBehavior.cs';a=af.read_text(encoding='utf-8-sig')
def method(s,sig):
 st=s.index(sig);i=s.index('{',st)+1;n=1
 while n:
  if s[i]=='{':n+=1
  elif s[i]=='}':n-=1
  i+=1
 return s[st:i]
check=method(s,'private void CheckIsEnemySideRetreatingOrOneSideDepleted()');ended=method(s,'public override bool MissionEnded(ref MissionResult missionResult)');change=method(s,'public void ChangeCanCheckForEndCondition(');helper=method(a,'private void TryDisableBattleEndLogic(string source)')
pre='''using System;using System.Collections.Generic;using System.Text.Json;
public enum BattleSideEnum{Attacker,Defender}public static class Ext{public static BattleSideEnum GetOppositeSide(this BattleSideEnum x)=>x==BattleSideEnum.Attacker?BattleSideEnum.Defender:BattleSideEnum.Attacker;}
public class Agent{public bool IsPlayerControlled=true,IsRunningAway;public bool IsActive()=>true;}
public class Team{public BattleSideEnum Side;public List<Agent> ActiveAgents=new();public bool IsFriendOf(Team x)=>Side==x.Side;public bool IsEnemyOf(Team x)=>Side!=x.Side;}
public struct MissionTime{public static MissionTime Now=>new();public float ElapsedSeconds=>0;}
public class BattleDeploymentHandler{}public class HideoutPhasedMissionController{}
public class Mission {public Team PlayerTeam=new(){Side=BattleSideEnum.Attacker};public Agent MainAgent=new();public List<Team> Teams=new();public bool HasDeployment;public T GetMissionBehavior<T>()where T:class=>typeof(T)==typeof(BattleDeploymentHandler)&&HasDeployment?new BattleDeploymentHandler()as T:null;}
public class MissionResult {public string Result;public static MissionResult CreateSuccessful(Mission m,bool r)=>new(){Result="win"};public static MissionResult CreateDefeated(Mission m)=>new(){Result="loss"};public static MissionResult CreateDefenderPushedBack()=>new(){Result="pushback"};}
public class MissionLogic {public Mission Mission=new();public virtual bool MissionEnded(ref MissionResult r)=>false;}
public class Spawner{public bool EnemyDepleted,PlayerDepleted;public int StopCalls;public bool IsSideDepleted(BattleSideEnum s)=>s==BattleSideEnum.Attacker?PlayerDepleted:EnemyDepleted;public float GetReinforcementInterval(BattleSideEnum s)=>1;public void StopSpawner(BattleSideEnum s)=>StopCalls++;}
public class NativeReplay:MissionLogic {public Spawner _missionAgentSpawnLogic=new();bool _canCheckForEndCondition=true,_canCheckForEndConditionSiege,_isEnemySideDepleted,_isPlayerSideDepleted,_isEnemySideRetreating,_isPlayerSideRetreating,_isEnemyDefenderPulledBack,_enemyDefenderPullbackEnabled;MissionTime _playerSideNotYetRetreatingTime,_enemySideNotYetRetreatingTime;public void Check()=>CheckIsEnemySideRetreatingOrOneSideDepleted();
'''
bridge='''}
public static class Logger{public static void Log(string a,string b){}}
public class AfHost {NativeReplay _battleEndLogic;public AfHost(NativeReplay n)=>_battleEndLogic=n;void CacheBattleEndLogic(){}public void Run()=>TryDisableBattleEndLogic("test");
'''
post='''}
public static class Program{public static void Main(){var rows=new List<object>();void Case(string name,bool deploy,bool enemy,bool player,bool actualDisable,string expected){var n=new NativeReplay();n.Mission.HasDeployment=deploy;n._missionAgentSpawnLogic.EnemyDepleted=enemy;n._missionAgentSpawnLogic.PlayerDepleted=player;new AfHost(n).Run();if(actualDisable)n.ChangeCanCheckForEndCondition(true);n.Check();n.Check();MissionResult r=null;bool ended=n.MissionEnded(ref r);string got=ended?r.Result:"none";if(got!=expected)throw new Exception(name+" got "+got);rows.Add(new{name,ended,result=got,stopSpawnerCalls=n._missionAgentSpawnLogic.StopCalls});}Case("deployment_handler_present_enemy_empty",true,true,false,false,"none");Case("after_deployment_enemy_empty_AF_TryDisable",false,true,false,false,"none");Case("after_deployment_both_populated",false,false,false,false,"none");Case("unowned_native_positive_control",false,true,false,true,"win");Case("after_deployment_player_empty_AF_TryDisable",false,false,true,false,"none");Console.WriteLine(JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));}}
'''
(out/'Program.cs').write_text(pre+check+'\n'+ended+'\n'+change+bridge+helper+post,encoding='utf-8');(out/'NativeReplay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649</NoWarn></PropertyGroup></Project>');(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');(out/'source.json').write_text(json.dumps({'afSHA256':hashlib.sha256(af.read_bytes()).hexdigest(),'afSource':str(af),'nativeSource':str(p),'nativeSHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'extracts':['native CheckIsEnemySideRetreatingOrOneSideDepleted','native MissionEnded','native ChangeCanCheckForEndCondition','AF TryDisableBattleEndLogic'],'limits':['Native decompiled1.5.6 copied source with synthetic spawner/team/time','No native mission loader','Demonstrates native MissionEnded result and spawner stop, not automatic map exit','Requires actual side depletion or retreat; does not prove streamer scene state']},indent=2),encoding='utf-8')

r=subprocess.run([args.dotnet,"run","--project",str(out/"NativeReplay.csproj")],capture_output=True,text=True);(out/"result.log").write_text(r.stdout+r.stderr,encoding="utf-8");print(r.stdout+r.stderr);raise SystemExit(r.returncode)
