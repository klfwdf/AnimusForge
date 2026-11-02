"""Static wiring/geometry contracts only; C# setup execution lives in the companion project."""
from pathlib import Path
import sys, re, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
base=ROOT/'.tmp/field-camp-no-auto-exit-20261102/before' if '--before' in sys.argv else ROOT
C='src/modules/AF.Module.FieldCamp/'
def read(path):
    p=base/path
    return p.read_text(encoding='utf-8-sig') if p.exists() else ''
main=read(C+'Host/FieldCampMissionLogic.cs');setup=read(C+'Host/FieldCampMissionLogic.Setup.cs')
launcher=read(C+'Host/FieldCampMissionLauncher.cs');site=read(C+'Host/FieldCampSite.cs');props=read(C+'Host/FieldCampProps.cs');behavior=read(C+'Host/FieldCampBehavior.cs')
checks=[
('initialization cannot end the camp', 'Mission.EndMission(' not in main+setup and 'keeping mission open' in setup),
('native knockout and boundary timeout owners absent', 'new BasicLeaveMissionLogic(' not in launcher and 'new MissionBoundaryCrossingHandler(' not in launcher and 'CreateBoundaryCrossingHandler()' not in launcher),
('manual TAB/ESC stays native', 'CreateMissionLeaveView()' in launcher and 'CreateMissionSingleplayerEscapeMenu' in launcher and 'canPlayerLeave = true;' in main),
('mission completion stays explicit', bool(re.search(r'bool MissionEnded[^}]+return false;',main))),
('no polling retries of incomplete initialization', '_spawned = true;' in main and 'if (!_campInitialized) return;' in main),
('safe ground remains mandatory for fallback player', 'TryFindPlayerStart(Mission, desired, out pos)' in main and 'TryProbePlayerPosition' in site and '|| obstacle' in site and 'CellHasClearance(mission.Scene' in site),
('whole-camp flatness cannot degrade to a safe single player point', 'Scales = { 1f }' in site and 'FieldCampSitePlanner.TryChoose' in site and 'frame.origin = player - offset;' not in site),
('blocked gate does not close every otherwise valid cell', 'preferred player/gate corridor blocked; keeping valid ground cells' in site and 'cache.Ready = false;' not in site),
('tent failure is not fatal', 'No accessible, grounded tent candidate:' not in props and 'tent slot skipped;' in props),
('optional furniture failure does not reject a valid tent', 'if (!furnished ||' not in props and 'optional tent furniture skipped' in props),
('late blocked tent is removed with its furniture', 'interior.Root.Remove(80);' in props and 'interior.FurnitureEntities[f].Remove(80);' in props),
('cache is attempted exactly once after optional props', setup.count('FieldCampSite.InitializeGroundCache(')==1 and 'FieldCampSite.InitializeGroundCache(' not in props),
('unusable scene cannot charge patrol time', 'bool playerWasReady = _patrolPlayerReady;' in behavior and 'if (!playerWasReady || hours <= 0f)' in behavior and 'MarkPatrolPlayerReady' in setup),
]
for rel in ['content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml','content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml']:
    nodes=ET.fromstring(read(rel)).findall('.//string')
    checks.append(('degradation UI registered once '+rel, sum(n.get('id')=='AF_FC_SetupDegraded' for n in nodes)==1))
failed=[name for name,ok in checks if not ok]
for name,ok in checks:print(('PASS ' if ok else 'FAIL ')+name)
print(f'{len(checks)-len(failed)}/{len(checks)} no-auto-exit source/wiring contracts passed; native engine behavior NOT RUN')
sys.exit(bool(failed))
