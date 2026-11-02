"""Wiring checks, separate from the compiled production route-algorithm tests."""
from pathlib import Path
import sys,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'.tmp/field-camp-deployment-route-20261102/before' if '--before' in sys.argv else ROOT
C='src/modules/AF.Module.FieldCamp/'
def read(p):
    path=BASE/p
    return path.read_text(encoding='utf-8-sig') if path.exists() else ''
scene=read(C+'Host/FieldCampSite.cs');execution=read(C+'Host/FieldCampMissionLogic.Execution.cs');props=read(C+'Host/FieldCampProps.cs');setup=read(C+'Host/FieldCampMissionLogic.Setup.cs')
checks=[
('runtime prop clearance feeds a scene-owned route grid','cache.Router = new FieldCampGroundRouter' in scene and 'TryResolveGroundedPosition(scene, sample, 0.4f' in scene),
('a blocked cached route cannot be overridden by baked navmesh','if (!routed && (covered || !HasCeremonyWalkPath' in execution),
('walkers advance small actual route corners rather than retry final point through fences','goal = move.Route[move.RouteIndex];' in execution and 'move.RouteIndex++' in execution and '<= 0.09f' in execution),
('arrived requires reaching final route segment as well as final position','move.RouteIndex + 1 == move.Route.Length' in execution and 'move.Agent.Position.AsVec2.DistanceSquared(move.Goal.AsVec2)' in execution),
('clearance retries bounded multiple directions, not one obstructed line','attempt < 8' in execution and 'MathF.Cos(angle)' in execution and 'int newRoutes = 2;' in execution),
('runtime construction still waits for full clearance and watcher arrival','_ceremonyWaiting == 0 && _ceremonyUnclearedMoves == 0' in execution and 'CompleteCeremonyPreparation();' in execution),
('no teleport or automatic mission exit introduced','TeleportToPosition' not in execution and 'Mission.EndMission(' not in execution+setup),
('release cannot reapply the forward ceremony route on a return-home request','IssueCeremonyWalk(move, move.Home, move.HomeFacing, followRoute: false)' in execution),
('visible wait notice and throttled diagnostics reach actual preparation owner','FieldCampText.CeremonyPreparing()' in execution and '_nextCeremonyProgressLog = _elapsed + 5f;' in execution and 'execution preparation pending' in execution),
('optional skipped furnishing is counted only for accepted tents','OptionalFurnitureSkips += skippedFurniture' in props and 'else if (FieldCampProps.OptionalFurnitureSkips > 0)' in setup),
]
for rel in ['content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml','content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml']:
    doc=ET.fromstring(read(rel));nodes=doc.findall('.//string')
    for key in ['AF_FC_CeremonyPreparing','AF_FC_OptionalFurnitureSkipped']:
        checks.append(('registered UI '+key+' '+rel,sum(n.get('id')==key for n in nodes)==1))
failed=0
for name,ok in checks:
    print(('PASS ' if ok else 'FAIL ')+name);failed+=not ok
print(f'{len(checks)-failed}/{len(checks)} deployment-route wiring contracts passed; native crowd movement NOT RUN')
sys.exit(bool(failed))
