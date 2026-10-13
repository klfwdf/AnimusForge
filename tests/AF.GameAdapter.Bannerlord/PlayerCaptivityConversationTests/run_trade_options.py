"""Replay the verbatim production options reader; no game mutation or LLM simulation."""
import argparse, hashlib, importlib.util, json, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('extractor', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec); spec.loader.exec_module(extractor)
parser = argparse.ArgumentParser(); parser.add_argument('--source-ref'); args = parser.parse_args()
path = 'src/AF.GameAdapter.Bannerlord/Scene/SceneTradeBannerlordAdapter.cs'
source = extractor.source(path, args.source_ref)
method = extractor.declaration(source, 'internal List<ShoutTradeResourceOption> BuildShoutTradeOptions()')
out = new_run_root(ROOT, 'captive-trade-options', None)
(out / 'Fixture.cs').write_text((HERE / 'TradeOptionsFixture.cs.txt').read_text(encoding='utf-8').replace('/* PRODUCTION_OPTIONS */', method), encoding='utf-8')
(out / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>', encoding='utf-8')
(out / 'source.json').write_text(json.dumps({'path': path, 'revision': args.source_ref or 'working-tree', 'sha256': hashlib.sha256(source.encode()).hexdigest(), 'methodSha256': hashlib.sha256(method.encode()).hexdigest()}), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([dotnet, 'run', '--project', str(out / 'Test.csproj')], cwd=ROOT, env=minimal_test_environment(dotnet, out), capture_output=True)
(out / 'run.log').write_bytes(result.stdout + result.stderr)
print((result.stdout + result.stderr).decode('utf-8', errors='replace'))
print('Run output:', out)
raise SystemExit(result.returncode)
