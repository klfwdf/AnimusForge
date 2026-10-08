"""Compile the production mission capture method against controlled native boundaries."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import re
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
p = argparse.ArgumentParser(); p.add_argument('--out', type=Path, required=True); a = p.parse_args()
out = a.out.resolve(); out.mkdir(parents=True, exist_ok=False)
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
capture = ROOT / 'extensions/AnimusForge.Illustrator/src/Engine/SceneReferenceCapture.cs'
snapshot = ROOT / 'extensions/AnimusForge.Illustrator/src/Engine/PanoramaSceneSnapshot.cs'
supplement = ROOT / 'extensions/AnimusForge.Illustrator/src/Engine/PanoramaResourceSupplement.cs'
budget = ROOT / 'extensions/AnimusForge.Illustrator/src/Engine/PanoramaGeometryBudget.cs'
source = capture.read_text(encoding='utf-8-sig')
methods = '\n'.join(extract.declaration(source, name) for name in [
    'private static async Task<ConversationSceneReferenceCapture> CaptureMissionSceneReferencesAsync(',
    'internal static IReadOnlyList<IllustrationReferenceImage> CreateCapturedSceneReferences(',
    'private static IReadOnlyList<IllustrationReferenceImage> PreserveBudgetFallbackScreenshot('])
# Both actual producers must use the tested guard, at the unchanged rendered-copy ceiling.
assert 'PanoramaSnapshotMaxCopies = 1024;' in snapshot.read_text(encoding='utf-8-sig')
assert 'PanoramaGeometryBudget.BeforeCopy(Snapshot.CopiedRoots, PanoramaSnapshotMaxCopies, "runtime");' in snapshot.read_text(encoding='utf-8-sig')
assert 'PanoramaGeometryBudget.BeforeCopy(_snapshot.CopiedRoots, ScreenCaptureHelper.PanoramaSnapshotMaxCopies, "runtime_and_resources");' in supplement.read_text(encoding='utf-8-sig')
snapshot_text = snapshot.read_text(encoding='utf-8-sig')
creation = extract.declaration(snapshot_text, 'internal static async Task<PanoramaSceneSnapshot> CreatePanoramaSnapshotAsync(')
assert 'builder.ReleaseRoots();' in creation and 'builder.Snapshot.DisposeUnrendered();' in creation
assert 'await retirement.ConfigureAwait(false);' in creation
assert 'finally { created.DisposeTemplates(); }' in creation
retirement = extract.declaration(snapshot_text, 'internal void DisposeUnrendered(')
assert 'ReferenceEquals(ScreenCaptureHelper._pendingPanoramaSnapshot, this)' in retirement
assert 'ReleaseSourceHandles?.Invoke();' in retirement and 'owned.ClearAll();' in retirement and 'owned.ManualInvalidate();' in retirement
popup = (ROOT / 'extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs').read_text(encoding='utf-8-sig')
start = popup.index('sceneCapture = await ScreenCaptureHelper.CaptureConversationSceneReferencesAsync(')
assert popup.index('var characterRefs = new List<IllustrationReferenceImage>();', start) > start
assert popup.index('promptPlan.HardFacts + sceneCapture.NearbyPropFacts', start) > start
(out / 'Program.cs').write_text((HERE / 'Fixture.cs.in').read_text(encoding='utf-8').replace('__METHODS__', methods), encoding='utf-8')
(out / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0414</NoWarn></PropertyGroup><ItemGroup><Compile Include="' + str(budget) + '"/></ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'source-manifest.json').write_text(json.dumps({str(path): hashlib.sha256(path.read_bytes()).hexdigest() for path in [capture, snapshot, supplement, budget, HERE / 'Fixture.cs.in']}, indent=2), encoding='utf-8')
result = subprocess.run(['dotnet', 'run', '--project', str(out / 'Test.csproj'), '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')], cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
text = result.stdout + result.stderr; (out / 'run.log').write_text(text, encoding='utf-8'); print(text)
raise SystemExit(result.returncode)
