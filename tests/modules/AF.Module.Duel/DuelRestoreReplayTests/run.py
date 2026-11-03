"""Execute current production restoration methods with synthetic engine objects.
No Bannerlord runtime or AI simulation; startup/terminal call sites are separate wiring checks.
Usage: python run.py --dotnet <dotnet.exe> [--output <dir>]
"""
from pathlib import Path
import argparse, subprocess, hashlib, json
p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');p.add_argument('--output');a=p.parse_args()
root=Path(__file__).resolve().parents[4];out=Path(a.output) if a.output else root/'.tmp/duel-restore-replay';out.mkdir(parents=True,exist_ok=True)
source=root/'src/modules/AF.Module.Duel/Host/DuelBehavior.cs';s=source.read_text(encoding='utf-8-sig')
def method(sig):
 start=s.index(sig);i=s.index('{',start);depth=1;j=i+1
 while depth:
  if s[j]=='{':depth+=1
  elif s[j]=='}':depth-=1
  j+=1
 return s[start:j]
sigs=['private void SetDuelTeamRelation(', 'private void CaptureDuelTeamRelation(', 'private void RestoreDuelTeamRelations()', 'private void ClearInPlaceDuelRestoreState()', 'private void AbortInPlaceDuel(', 'private void RestoreState(', 'private void FinishDuel()']
methods='\n'.join(method(x) for x in sigs)
start=method('private void StartDuelInternal(\n\t\tAgent agent,');end=method('private void EndDuel(bool playerDefeated)')
assert start.count('SetDuelTeamRelation(')==6
assert 'AbortInPlaceDuel("team_setup_failed")' in start and 'AbortInPlaceDuel("in_place_start_exception")' in start
assert '_activeInPlaceDuelMission = current' in start and '_inPlaceDuelStateCaptured = true' in start
assert end.count('RestoreState(restoreHealth: false)')==2
assert 'ChangeCanCheckForEndCondition(false)' in method('private void TryDisableBattleEndLogic(')
pre=r'''using System;using System.Collections.Generic;
class Team{public Dictionary<Team,bool> Edges=new();public bool Symmetric,ThrowAfterMutation;public bool IsEnemyOf(Team t)=>Edges.TryGetValue(t,out var v)&&v;public void SetIsEnemyOf(Team t,bool v){Edges[t]=v;if(Symmetric)t.Edges[this]=v;if(ThrowAfterMutation){ThrowAfterMutation=false;throw new Exception("partial write");}}}
enum MissionMode{Conversation,Battle}
class Mission{public static Mission Current;public MissionMode Mode;public void SetMissionMode(MissionMode m,bool atStart){Mode=m;}}
class Agent{public enum MortalityState{Mortal,Invulnerable}public Team Team;public Agent MountAgent;public bool Active=true;public float Health=70,HealthLimit=100;public string Controller="None";public MortalityState Mortality=MortalityState.Invulnerable;public bool Paused=true;public bool IsActive()=>Active;public void SetTeam(Team t,bool sync){Team=t;}public void SetMortalityState(MortalityState s){Mortality=s;}public void SetIsAIPaused(bool isPaused){Paused=isPaused;}}
static class LordEncounterBehavior{public static bool IsEncounterMeetingMissionActive;}
static class MeetingBattleRuntime{public static bool IsMeetingActive;}
static class MeetingBattleLockMissionBehavior{public static int Calls;public static void RestoreFormalDuelIsolationForCurrentMeeting(string s){Calls++;}}
static class Logger{public static void Log(string a,string b){}}
static class AnimusForgeQuickInfo{public static int Calls;public static void Show(string s){Calls++;}}
class Host{
public bool _inPlaceDuelStateCaptured=true,_isDuelActive=true,_meetingPreFightActive=true;public float _meetingPreFightEndTime,_formalDuelSpectatorRefreshTimer;
public Agent _inPlaceRestoreTarget=new(),_inPlaceRestorePlayer=new();public Mission _activeInPlaceDuelMission;public object _activeDuelOutcomeStart=new();public int Unknown,SpectatorRestore;
public Team _preDuelPlayerTeam,_preDuelTargetTeam,_preDuelPlayerMountTeam,_preDuelTargetMountTeam,_duelPlayerTeam,_duelEnemyTeam;
public MissionMode _preDuelMode=MissionMode.Conversation;
readonly Dictionary<Team,Dictionary<Team,bool>> _preDuelTeamRelations=new();readonly HashSet<int> _formalDuelSpectatorAgentIndices=new();
void RestoreFormalDuelSpectatorControl(){SpectatorRestore++;}void TrySetAgentController(Agent a,string s){a.Controller=s;}void UnlockAgentMovement(Agent a,bool unpauseAi,bool clearTargetFrame){}void MarkDuelOutcomeUnknown(object start,string reason,string source){if(start!=null)Unknown++;}string DuelString(string k,string fallback)=>fallback;
public void Change(Team a,Team b,bool enemy)=>SetDuelTeamRelation(a,b,enemy);public void Restore(bool heal=true)=>RestoreState(heal);public void Abort()=>AbortInPlaceDuel("test");public void Finish()=>FinishDuel();public int Snapshots=>_preDuelTeamRelations.Count;
'''
post=r'''}
static class Program{
static int checks;static void Check(bool c,string name){if(!c)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}static Host Fresh(){Mission.Current=new(){Mode=MissionMode.Battle};return new(){_activeInPlaceDuelMission=Mission.Current};}
static void Main(){
var h=Fresh();var player=h._inPlaceRestorePlayer;var target=h._inPlaceRestoreTarget;var p=new Team();var e=new Team();var other=new Team();var untouched=new Team();p.Edges[e]=false;e.Edges[p]=true;p.Edges[other]=true;other.Edges[p]=false;e.Edges[other]=true;other.Edges[e]=true;p.Edges[untouched]=true;
h.Change(p,e,true);h.Change(e,p,true);h.Change(p,other,false);h.Change(other,p,false);h.Change(e,other,false);h.Change(other,e,false);h.Change(p,e,false);h.Change(p,e,true);h.Restore();
Check(!p.IsEnemyOf(e)&&e.IsEnemyOf(p),"restore directed original relations not blanket neutral");Check(p.IsEnemyOf(other)&&!other.IsEnemyOf(p)&&e.IsEnemyOf(other)&&other.IsEnemyOf(e),"restore all changed third-party edges");Check(p.IsEnemyOf(untouched),"untouched edge preserved");Check(player.Health==100&&target.Health==100,"successful restoration preserves existing full health policy");Check(Mission.Current.Mode==MissionMode.Conversation&&h.Snapshots==0,"restore prior mode and consume snapshot");player.Health=50;h.Restore();Check(player.Health==50,"restoration is idempotent");
h=Fresh();player=h._inPlaceRestorePlayer;target=h._inPlaceRestoreTarget;p=new(){Symmetric=true};e=new(){Symmetric=true};h.Change(p,e,true);h.Abort();Check(!p.IsEnemyOf(e)&&!e.IsEnemyOf(p),"symmetric native-like setter captured before both writes");Check(player.Health==70&&target.Health==70,"abort does not grant full health");Check(!h._isDuelActive&&h._activeDuelOutcomeStart==null&&h.Unknown==1&&h._activeInPlaceDuelMission==null,"abort consumes owner without result");Check(player.Mortality==Agent.MortalityState.Mortal&&target.Controller=="AI"&&!target.Paused,"abort restores preparation locks");Check(AnimusForgeQuickInfo.Calls==0,"abort emits no win/loss/finish success notice");
h=Fresh();p=new(){ThrowAfterMutation=true};e=new();try{h.Change(p,e,true);}catch{}h.Abort();Check(!p.IsEnemyOf(e),"partial relation setter failure compensated");
h=Fresh();player=h._inPlaceRestorePlayer;p=new();e=new();h.Change(p,e,true);var replacement=new Mission(){Mode=MissionMode.Battle};Mission.Current=replacement;h.Abort();Check(p.IsEnemyOf(e)&&player.Health==70&&h.SpectatorRestore==0&&replacement.Mode==MissionMode.Battle,"mission change never mutates old objects or replacement scene");Check(h.Snapshots==0&&h._inPlaceRestoreTarget==null,"mission change discards stale restore identities");
h=Fresh();player=h._inPlaceRestorePlayer;target=h._inPlaceRestoreTarget;target.Active=false;target.Health=0;h.Restore();Check(target.Health==0&&player.Health==100,"inactive target is not revived by cleanup");
h=Fresh();p=new();e=new();h.Change(p,e,true);h.Finish();Check(!h._isDuelActive&&h._activeInPlaceDuelMission==null&&h.Snapshots==0,"finish drops mission and snapshot ownership");
Console.WriteLine($"{checks} behavior assertions passed; startup/terminal/native-gate wiring passed.");}}
'''
(out/'Program.cs').write_text(pre+methods+post,encoding='utf-8');(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649</NoWarn></PropertyGroup></Project>');(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'source.json').write_text(json.dumps({'source':str(source),'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'methods':sigs,'limits':'Synthetic engine objects. No AI, native callbacks, map event or actual game execution.'},indent=2))
r=subprocess.run([a.dotnet,'run','--project',str(out/'Replay.csproj')],capture_output=True,text=True);(out/'result.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr);raise SystemExit(r.returncode)
