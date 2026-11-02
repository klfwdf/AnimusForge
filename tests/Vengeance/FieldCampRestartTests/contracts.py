"""Structural consumer checks; complements linked C# tests, does not run Bannerlord."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
r=Path(__file__).resolve().parents[3]
def read(p): return (r/p).read_text(encoding='utf-8-sig')
def block(source,signature):
    start=source.index('{',source.index(signature));depth=1;end=start+1
    while depth:
        if source[end]=='{':depth+=1
        elif source[end]=='}':depth-=1
        end+=1
    return source[start:end]
checks=0
def check(ok,name):
    global checks
    assert ok,name
    checks+=1;print('PASS',name)
prefix='src/modules/AF.Module.FieldCamp/'
main=read(prefix+'Host/FieldCampMissionLogic.cs')
launcher=read(prefix+'Host/FieldCampMissionLauncher.cs')
site=read(prefix+'Host/FieldCampSite.cs')
props=read(prefix+'Host/FieldCampProps.cs')
execution=read(prefix+'Host/FieldCampMissionLogic.Execution.cs')
grounds=read(prefix+'Host/FieldCampMissionLogic.Grounds.cs')
zones=read(prefix+'FieldCampLayout.Zones.cs')
behavior=read(prefix+'Host/FieldCampBehavior.cs')
town=read('Vengeance/Source/Scene/TownExecutionMissionBehavior.cs')
retire=read('Vengeance/Source/Scene/TownExecutionMissionBehavior.CampRetirement.cs')
handler=read(prefix+'Host/FieldCampAgentHandler.cs')
check('GetBattleSceneForMapPatch(patch, false)' in launcher, 'town lifecycle retains wild field map selection')
check('new MissionBasicTeamLogic()' in launcher and 'Mission.Teams.Add(' not in block(main,'private void CreateCampTeam('),
      'native town team owner initializes camp teams exactly once')
check('SandBox.SandBoxHelpers.MissionHelper.SpawnPlayer(spawn' in main, 'native town player entry owns actual spawn lifecycle events')
check('view is MissionBoundaryCrossingView' in launcher and
      'ViewCreator.CreateMissionBoundaryCrossingView().GetType()' in launcher and
      'boundaryViewType.IsInstanceOfType(view)' in launcher, 'native town views omit registered Gauntlet boundary override as well as default countdown')
check('CreateAgentNavigator(resident)' in handler and 'resident.AddBehaviors(agent)' in handler and
      'LocationCharacterAgentSpawnedMissionEvent(resident, agent' in handler, 'native residents publish lifecycle after navigator creation')
check('origin ?? new PartyAgentOrigin' in handler and 'civilian: true, origin: origin' in read(prefix+'Host/FieldCampMissionLogic.People.cs'),
      'prisoners use resident lifecycle without losing mortal origin ownership')
check('cache.Ready = false' not in block(site,'internal static void InitializeGroundCache(') and
      'TryBuildGroundedRoute(scene, entry, exit' in site, 'blocked gate uses detour route without invalidating all actor cells')
check('if (found && checkedSites >= Finalists) break;' in site and 'i < candidates.Count && i < Finalists' not in site,
      'no valid site is lost solely because all initial finalists are unusable')
check('EndMission(' not in block(main,'private void SetUpCamp('),'setup failures do not terminate camp')
check('EndMission(' not in retire and 'RetreatMission' not in retire,'cleanup exhaustion never exits parent camp')
check('BasicLeaveMissionLogic' not in launcher and 'MissionBoundaryCrossingHandler' not in launcher,
      'camp omits native automatic casualty and boundary retreat owners')
check('canPlayerLeave = true;' in main and 'return false;' in block(main,'override bool MissionEnded('),
      'manual leave stays available without automatic victory result')
check('SandBoxMissionViews.OpenTownCenterMission(mission)' in launcher,'native town factory supplies manual leave and interaction views')
check('TryPlaceExecutionPreset' not in grounds and '.Add(' not in block(zones,'private void PlaceExecutionGround('),
      'logical execution ground creates no apparatus fences or decoration')
check('bool ready = _patrolPlayerReady;' in behavior and 'float hours = ready ? FieldCampSettings.PatrolHours : 0f;' in behavior and
      'MarkPatrolPlayerReady' in main,'unsuccessful player entry cannot charge patrol time')
check('AddGrid(scene, list, hasEncounter, encounter);' in site and 'FieldCampSurfaceProfile' in site and 'grid: 9' in site,
      'whole camp searches scene grid and checks full footprint not a single safe point')
check('FieldCampPlacementRules.TryTentScale' in props and 'ApplyScaleLocal(fittedScale)' in props,
      'fitted dimensions control real entity and collision frame')
check('pass < 2' in props and 'acceptedTents' in props and 'DoorIsClear(scene, previous.Frame' in props,
      'final standalone props and later tents cannot silently block accepted doors')
check('CountedWaiting = !watcher' in execution and 'if (!move.Spectator && !move.CountedWaiting)' in execution,
      'optional audience arrival is not mandatory deployment readiness')
check('VerifyCeremonyClearanceNow()' in execution and 'IsPhysicallyClear' in execution,
      'ready requires fresh physical clearance with body margin')
complete=block(execution,'private void CompleteCeremonyPreparation(')
check('move.Arrived && !move.ClearanceHeld' in complete and 'move.ClearanceHeld = true' in complete,
      'only arrived audience is borrowed and unfinished outside routes hold safely')
check('TryBuildGroundedRoute' in execution and 'covered || !HasCeremonyWalkPath' in execution and 'routeBudget=2' in execution,
      'cached collision routes are budgeted and no native fallback bypasses known obstruction')
tick=block(town,'public override void OnMissionTick(')
check(tick.index('if (Request.IsFieldCamp)') < tick.index('Mission.EndMission();') and
      'RetireCancelledCampExecution();' in tick,'town keeps ordinary exit while camp cancellation retires only execution')
for language in ['af_ui_strings.xml','CNs/af_ui_strings-zh-CN.xml']:
    strings={n.attrib['id']:n.attrib['text'] for n in ET.fromstring(read('content/modules/AF.Module.UI/ModuleData/Languages/'+language)).iter('string')}
    for key in ['AF_FC_NoCampSite','AF_FC_IncompleteCamp','AF_FC_DeploymentClearing']:
        check(key in strings and '{='+key+'}' in read(prefix+'FieldCampText.cs'),language+' '+key+' wired')
    if language.startswith('CNs/'):
        check(set(re.findall(r'\{[A-Z]+\}',strings['AF_FC_IncompleteCamp']))=={'{PLACED}','{TOTAL}'},'CN/EN tent-count variables preserved')
print(f'PASS {checks} restart source/resource contracts; native game NOT RUN')
