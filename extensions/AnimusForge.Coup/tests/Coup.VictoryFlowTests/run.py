"""Run extracted production callbacks/lifecycle against deterministic engine/UI doubles.
Real Bannerlord scene assets, input routing and political actions are NOT simulated acceptance.
"""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'extensions/AnimusForge.Coup/src/CoupSystem'
OUT=ROOT/'artifacts/coup-victory-feedback-20261002/flow-harness'

def extract(path, signature):
    text=path.read_text(encoding='utf-8-sig')
    start=text.index(signature)
    opening=text.index('{',start)
    arrow=text.find('=>',start,opening)
    if arrow>=0 and text.find(';',arrow,opening)>=0:
        return text[start:text.index(';',arrow)+1]
    depth=1;end=opening+1
    while depth:
        depth+=(text[end]=='{')-(text[end]=='}');end+=1
    return text[start:end]

campaign=SRC/'CoupCampaignBehavior.cs';mission=SRC/'CoupMissionBehavior.cs'
text=campaign.read_text(encoding='utf-8-sig')
assert 'TryOpenAftermath' not in text and 'af_coup_aftermath' not in text
assert '_session.AftermathPending = false;' in text
assert text.count('ChangeRulingClanAction.Apply(')==1
select=extract(campaign,'private void SelectDisposition(')
assert 'CommitVictory(' not in select and 'CompleteVictoryAndLeave()' in select
assert 'EndScene();' not in extract(mission,'private void CheckObjectives(')
commit=extract(campaign,'private void CommitVictory(')
assert 'CoupKingDisposition.Undecided' in commit
assert commit.index('_session.Phase = CoupPhase.Completed;')>commit.index('_session.RebellionQueued = true;')
assert 'CoronationRequested = true;' in text
notify=(SRC/'CoupBecomeKingSceneNotification.cs').read_text(encoding='utf-8-sig')
assert 'KingSelectionKingdomDecision' not in notify and 'ChangeRulingClanAction' not in notify
campaign_methods=['internal static bool IsHallObjectiveComplete(','internal static bool IsMissionActive(',
 'internal static void NotifyVictory(','internal static void NotifyDefeat(','internal static void NotifyTechnicalFailure(',
 'private void OnMissionEnded(','internal void OnEngineTick(','internal static bool TryOpenHallDisposition(',
 'private void OpenDisposition(','private bool IsDispositionUiCurrent(','private void SelectDisposition(',
 'private void PresentVictoryFeedback(','private void ShowVictoryReport(','private bool IsCurrentUi(']
mission_methods=['public override InquiryData OnEndMissionRequest(','private bool TryEstablishHallVictory(',
 'private void RequestVictoryDisposition(','internal bool CompleteVictoryAndLeave(','private void SaveHealth(',
 'private void EndScene(','protected override void OnEndMission(']
fixture=(Path(__file__).parent/'Fixture.cs.in').read_text(encoding='utf-8-sig')
fixture=fixture.replace('__CAMPAIGN__','\n'.join(extract(campaign,m) for m in campaign_methods))
fixture=fixture.replace('__MISSION__','\n'.join(extract(mission,m) for m in mission_methods))
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'Program.cs').write_text(fixture,encoding='utf-8')
links=''.join(f'<Compile Include="{SRC / name}" />' for name in ['CoupSession.cs','CoupOutcomeReport.cs','CoupBecomeKingSceneNotification.cs'])
(OUT/'Flow.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><PackageReference Include="Newtonsoft.Json" Version="13.0.3" /><Compile Include="Program.cs" />'+links+'</ItemGroup></Project>',encoding='utf-8')
subprocess.run(['dotnet','run','--project',str(OUT/'Flow.csproj')],cwd=ROOT,check=True)
