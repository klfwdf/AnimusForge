"""Real Native detached adapter, work-item completion and canonical parser; physical owner-thread fixture."""
from pathlib import Path
import argparse, importlib.util, os, subprocess, sys
from xml.sax.saxutils import escape
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('courier', ROOT / 'tests/modules/AF.Module.Conversation/CourierPostprocessOwnerRegressionTests/run.py')
courier = importlib.util.module_from_spec(spec)
spec.loader.exec_module(courier)
p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--dotnet')
a = p.parse_args()
out = new_run_root(ROOT, 'native-detached-postprocess', a.run_root)
dotnet = resolve_dotnet(ROOT, a.dotnet)
owner = courier.ex.source('src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs', None)
admission = courier.ex.source('src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs', None)
shout = courier.ex.source('ShoutBehavior.cs', None)
constant = next(line.strip() for line in shout.splitlines() if 'const int NativeConversationMainThreadPreprocessTimeoutMs =' in line)
code = (HERE / 'DetachedHarness.cs.txt').read_text(encoding='utf-8')
code = code.replace('@@WORK@@', '\n'.join(courier.ex.declaration(owner, signature) for signature in
    ('internal sealed class SceneActionPostprocessWorkItem', 'internal sealed class PostprocessNetworkRequest')))
code = code.replace('@@COMPLETE@@', courier.ex.declaration(owner, 'internal static string CompleteSceneUnifiedActionPostprocess('))
code = code.replace('@@DELEGATES@@', '\n'.join(line for line in admission.splitlines() if line.startswith('internal delegate bool NativeAdmissionTarget')))
code = code.replace('@@TIMEOUT@@', constant)
(out / 'Program.cs').write_text(code, encoding='utf-8')
paths = [out / 'Program.cs', current_source_path(ROOT, 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeDetachedPostprocess.cs')]
paths += [current_source_path(ROOT, path) for path in courier.LINKS]
paths += [current_source_path(ROOT, path) for path in (
    'src/modules/AF.Module.Conversation/Channels/Native/NativeDetachedPostprocessApplicationAdapter.cs',
    'src/modules/AF.Module.Conversation/Channels/Native/ConversationGameThreadDispatcher.cs',
    'src/modules/AF.Module.Prompt/Configuration/LegacyDetachedRuleSelector.cs')]
newtonsoft = Path(os.environ.get('AF_NEWTONSOFT') or str(dotnet.parent / 'sdk/8.0.425/Newtonsoft.Json.dll'))
if not newtonsoft.is_file(): raise SystemExit('Missing existing Newtonsoft.Json.dll: ' + str(newtonsoft))
items = ''.join('<Compile Include="' + escape(str(path)) + '" />' for path in paths)
items += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference>'
(out / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
r = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Tests.csproj'), '-c', 'Release'], cwd=out,
    env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
log = r.stdout + r.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
print('OUTPUT', out)
raise SystemExit(r.returncode)
