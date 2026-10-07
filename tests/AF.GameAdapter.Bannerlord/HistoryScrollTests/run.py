"""Real VM navigation methods and auto-scroll widget; row layout/game UI are doubles."""
from pathlib import Path
import argparse, importlib.util, json, hashlib, subprocess, sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import minimal_test_environment, resolve_dotnet

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
args = parser.parse_args()
out = args.run_root.resolve(); out.mkdir(parents=True, exist_ok=False)
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
vm = ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryLogVM.cs'
widget = ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryAutoScrollPanel.cs'
aux = ROOT / 'extensions/AnimusForge.DialogueUI/src/Native/DialogueAuxiliaryVM.cs'
source = vm.read_text(encoding='utf-8-sig')
fixture = (HERE / 'Fixture.cs.in').read_text(encoding='utf-8-sig')
for marker, name in [('__JUMP__', 'JumpToLatestPage'), ('__OLDER__', 'LoadOlderPage'), ('__NEWER__', 'LoadNewerPage')]:
    fixture = fixture.replace(marker, extract.declaration(source, 'public void ' + name + '()'))
assert 'History?.JumpToLatestPage()' in extract.declaration(aux.read_text(encoding='utf-8-sig'), 'public void LatestHistory()')
(out / 'Program.cs').write_text(fixture, encoding='utf-8')
(out / 'Widget.cs').write_text(widget.read_text(encoding='utf-8-sig'), encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
paths = [vm, widget, aux, HERE / 'Fixture.cs.in']
(out / 'source-manifest.json').write_text(json.dumps([{'path': str(p.relative_to(ROOT)), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in paths], indent=2))
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj')], cwd=out,
    env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'result.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr); raise SystemExit(result.returncode)
