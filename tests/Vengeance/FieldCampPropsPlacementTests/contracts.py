"""Manual-only apparatus wiring; native prefab tests are the companion C# executable."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'.tmp/field-camp-tents-manual-site-20261102/before' if '--before' in sys.argv else ROOT
C='src/modules/AF.Module.FieldCamp/'
def read(path,source=False):
    p=(ROOT if source else BASE)/path
    return p.read_text(encoding='utf-8-sig') if p.exists() else ''
def block(text,marker):
    a=text.index('{',text.index(marker));depth=0
    for i in range(a,len(text)):
        depth+=(text[i]=='{')-(text[i]=='}')
        if depth==0:return text[a:i+1]
    raise AssertionError(marker)
setup=read(C+'Host/FieldCampMissionLogic.Setup.cs');props=read(C+'Host/FieldCampProps.cs');grounds=read(C+'Host/FieldCampMissionLogic.Grounds.cs');zones=read(C+'FieldCampLayout.Zones.cs')
menu=read(C+'Host/FieldCampBehavior.Menu.cs',True)
behavior=read('Vengeance/Source/Campaign/RichExecutionCampaignBehavior.cs',True)
mission=read('Vengeance/Source/Scene/TownExecutionMissionBehavior.cs',True)
checks=[
('camp initialization cannot load or place an execution preset','TryPlaceExecutionPreset' not in setup and 'execution_preset' not in setup),
('automatic newest-preset selection/cleanup path retired','TryPlaceExecutionPreset' not in grounds and 'ExecutionSitePresetStore.LoadAll' not in grounds and '_executionPieces' not in grounds),
('execution rally position creates no apparatus fences lights or flags','Props.Add(' not in block(zones,'private void PlaceExecutionGround') and 'AddWall(' not in block(zones,'private void PlaceExecutionGround')),
('camp prop consumer ignores stable legacy apparatus kinds','if (prop.Kind == FieldCampLayout.PropKind.ExecutionBlock || prop.Kind == FieldCampLayout.PropKind.Gallows) continue;' in props),
('wide bounds fit a real uniformly scaled mesh/body, not a deletion threshold','float scale = Math.Min(requestedScale' in props and 'frame.rotation.ApplyScaleLocal(scale);' in props and 'width > 3f || depth > 4f' not in props),
('ordinary slopes are supported rather than a horizontal 22cm slab','TryFitTentGround' in props and '0.22f' not in props and 'Vec3.CrossProduct(side, forward)' in props),
('tent/furniture mesh base and artist pivot share the actual fitted frame','frame.origin = door.origin - frame.rotation.TransformToParent(baseCentre);' in props and 'Frame = door' in props),
('measuring entities are cached then removed','TryMeasurePrefab' in props and 'measured[prefab] = box;' in props and 'finally { probe?.Remove(80); }' in props),
('existing field-camp provisional request remains custom deployment','TryPrepareCampExecution()' in menu and 'ExecutionSceneMode.CustomPreset' in block(behavior,'internal bool TryBeginFieldCampDeployment')),
('builder waits for player input before any ceremony','if (Request.SceneMode == ExecutionSceneMode.CustomPreset && !_customSiteLayoutConfirmed)' in mission and 'EnsureCustomSiteBuilderCreated();' in mission and '_customSiteBuilder?.Tick(dt);' in mission),
]
failed=0
for name,ok in checks:
    print(('PASS ' if ok else 'FAIL ')+name);failed+=not ok
print(f'{len(checks)-failed}/{len(checks)} tent/manual-site source contracts passed; native builder/visual acceptance NOT RUN')
sys.exit(bool(failed))
