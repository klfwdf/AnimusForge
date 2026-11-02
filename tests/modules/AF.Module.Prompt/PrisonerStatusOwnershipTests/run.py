"""Exercise current prisoner capture methods and Extras composition; no game or API requests."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, subprocess, sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet

p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--api', choices=['1.3', '1.4'], default='1.3')
p.add_argument('--mutate', action='store_true', help='Restore the duplicate producer to verify regression detection.')
a = p.parse_args()
out = new_run_root(ROOT, 'prisoner-status-ownership', a.run_root)
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
inputs = {}
def body(path, signature):
    f = ROOT / path
    inputs[path] = hashlib.sha256(f.read_bytes()).hexdigest()
    return extract.declaration(f.read_text(encoding='utf-8-sig'), signature)

scene = body('src/AF.GameAdapter.Bannerlord/Prompt/SceneAgentIdentityPromptCaptureAdapter.cs', 'internal static string BuildPrisonerContextLineForPrompt(')
if a.mutate:
    scene = scene.replace('return "";', 'return SharedPromptCaptureBannerlordAdapter.BuildHeroPrisonerStatusPromptLineForExternal(hero);', 1)
shared = body('src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs', 'internal static string BuildHeroPrisonerStatusPromptLineForExternal(')
extras = 'src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs'
dto = body(extras, 'internal sealed class PromptExtrasSections')
compose = body(extras, 'internal static string Compose(')
append = body(extras, 'private static void Append(')
(out / 'Actual.cs').write_text('using System; using System.Linq; using System.Text; namespace AnimusForge; internal static partial class SceneAgentIdentityPromptCaptureAdapter {' + scene + '} internal static class SharedPromptCaptureBannerlordAdapter {' + shared + '}' + dto + ' internal static class PromptExtrasComposer {' + compose + append + '}', encoding='utf-8')
(out / 'Program.cs').write_bytes((HERE / 'Harness.cs.txt').read_bytes())
resource = ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData'
for language in ['zh-CN', 'en']:
    source = resource / ('AdapterPromptCapture.' + language + '.json')
    inputs[str(source.relative_to(ROOT))] = hashlib.sha256(source.read_bytes()).hexdigest()
    (out / (language + '.json')).write_bytes(source.read_bytes())

# Guard the real sole injection and its three channel consumers, rather than a synthetic ownership flag.
capture = (ROOT / 'src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
assert capture.count('extrasSections.ActivePrisonerStatusLine = ports.BuildPrisonerStatus(targetHero);') == 1
for channel in ['Native/ShoutBehavior.NativeTurnPrompt.cs', 'Scene/ShoutBehavior.SceneConversationChains.cs', 'Courier/CourierDeliveryBehavior.PromptPreparation.cs']:
    source = ROOT / 'src/modules/AF.Module.Conversation/Channels' / channel
    text = source.read_text(encoding='utf-8-sig')
    assert ('BuildSceneUserRuntimeContextForSingle' in text if channel.startswith('Native/') else 'BuildShoutPromptContextForExternal' in text), channel
    inputs[str(source.relative_to(ROOT))] = hashlib.sha256(source.read_bytes()).hexdigest()
capture_path = 'src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs'
inputs[capture_path] = hashlib.sha256((ROOT / capture_path).read_bytes()).hexdigest()
(out / 'Checks.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit><DefineConstants>' + ('BANNERLORD_1_4_OR_GREATER' if a.api == '1.4' else '') + '</DefineConstants></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), 'build', str(out / 'Checks.csproj'), '-c', 'Release', '--nologo'], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
run = subprocess.run([str(dotnet), str(out / 'bin/Release/net8.0/Checks.dll')], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=60) if build.returncode == 0 else None
log = build.stdout + build.stderr + (run.stdout + run.stderr if run else '')
(out / 'run.log').write_text(log, encoding='utf-8')
code = build.returncode or (run.returncode if run else 0)
assert all(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest for path, digest in inputs.items()), 'Production inputs changed during replay'
(out / 'receipt.json').write_text(json.dumps(dict(api=a.api, mutation=a.mutate, exitCode=code, sources=inputs, coverage='Exact production prisoner intro/status methods, Extras DTO and Compose/Append; real CN/EN resource text. Controlled game-object and special-scene leaves. No live-game acceptance.'), indent=2), encoding='utf-8')
print(log)
raise SystemExit(code)
