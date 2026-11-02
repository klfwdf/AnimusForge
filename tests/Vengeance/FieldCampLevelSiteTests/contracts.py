"""Whole-footprint and optional-audience wiring; compiled policy/lifecycle tests are separate."""
from pathlib import Path
import sys,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'.tmp/field-camp-level-site-readiness-20261102/before' if '--before' in sys.argv else ROOT
C='src/modules/AF.Module.FieldCamp/'
def read(p):
    path=BASE/p
    return path.read_text(encoding='utf-8-sig') if path.exists() else ''
site=read(C+'Host/FieldCampSite.cs');setup=read(C+'Host/FieldCampMissionLogic.Setup.cs');execution=read(C+'Host/FieldCampMissionLogic.Execution.cs')
town=read('Vengeance/Source/Scene/TownExecutionMissionBehavior.cs');retire=read('Vengeance/Source/Scene/TownExecutionMissionBehavior.CampCancellation.cs')
checks=[
('all site candidates use full footprint hard flatness policy','FieldCampSitePlanner.TryChoose' in site and 'layout.HalfX, layout.Back, layout.Front' in site),
('scene-wide grid participates even when nearby spawn paths exist','candidates.AddRange(grid);' in site and 'TryChoose' in site),
('safe-player-only hillside fallback retired','ResolveFallback' not in site+setup and 'frame.origin = player - offset;' not in site),
('rejected site cannot continue through props and camp actors','if (!levelSiteReady)' in setup and setup.index('FieldCampText.NoLevelSite()')<setup.index('FieldCampProps.Place(') and 'return;' in setup[setup.index('if (!levelSiteReady)'):setup.index('int cleared')]),
('late optional spectators hold outside real clearance without fake arrival','FieldCampPreparationReadiness.CanParkOutside' in execution and 'move.ParkedOutside = true;' in execution and 'if (move.ParkedOutside && !uncleared) continue;' in execution),
('late parked actors not published as ready borrowed audience','move.Arrived && !move.ParkedOutside && move.Agent.IsActive()' in execution),
('real clearance and mandatory waits still gate runtime readiness','_ceremonyWaiting == 0 && _ceremonyUnclearedMoves == 0' in execution and '_ceremonyClearanceVerified' in execution),
('cleared actor re-entry is a live blocker again','move.ParkedOutside = false;' in execution and 'if (!move.CountedWaiting) { _ceremonyWaiting++; move.CountedWaiting = true; }' in execution),
('cancel cleanup removes death owner before host invalidates its Mission','var mission = Mission;' in retire and 'mission.RemoveMissionBehavior(deathLogic)' in retire and retire.index('mission.RemoveMissionBehavior(deathLogic)')<retire.index('mission.RemoveMissionBehavior(this)')),
('bounded cancelled-preparation retries never end the parent camp','cleanupAttempts > 3' in retire.lower().replace('cleanupattempts','cleanupAttempts') and 'Mission.EndMission(' not in retire),
('unbuilt camp rejection retires even if admission failed before preparation started','if (Request.IsFieldCamp && !_initialized && !_fieldCampCeremonyStaged)' in town and 'RetireCancelledCampPreparation();' in town),
]
for rel in ['content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml','content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml']:
    nodes=ET.fromstring(read(rel)).findall('.//string');checks.append(('no-level-site UI registered '+rel,sum(n.get('id')=='AF_FC_NoLevelSite' for n in nodes)==1))
failed=0
for name,ok in checks:
    print(('PASS ' if ok else 'FAIL ')+name);failed+=not ok
print(f'{len(checks)-failed}/{len(checks)} level-site/readiness wiring contracts passed; native camp placement and deployment NOT RUN')
sys.exit(bool(failed))
