"""Compile and execute the actual J09 action protocol owners."""
from pathlib import Path
import argparse, os, subprocess

ROOT = Path(__file__).resolve().parents[4]
import sys
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument('--mutate', choices=['skip-overflow','skip-disallowed','ignore-order','ignore-parameters','allow-action-star'])
parser.add_argument('--run-root', type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, 'action-protocol', args.run_root)

sources = {
    'InteractionContracts.cs': ROOT / 'src/AF.Contracts/Internal/InteractionContracts.cs',
    'LlmContracts.cs': ROOT / 'src/AF.Contracts/Internal/LlmContracts.cs',
    'LegacyActionTagCatalog.cs': ROOT / 'src/modules/AF.Module.Actions/Tags/LegacyActionTagCatalog.cs',
    'LegacyActionTagParser.cs': ROOT / 'src/modules/AF.Module.Actions/Tags/LegacyActionTagParser.cs',
    'ActionPlanIntegrityPolicy.cs': ROOT / 'src/modules/AF.Module.Actions/Plan/ActionPlanIntegrityPolicy.cs',
}
for name, path in sources.items():
    text = path.read_text(encoding='utf-8-sig')
    if args.mutate == 'skip-overflow' and name == 'ActionPlanIntegrityPolicy.cs':
        text = text.replace('            || _parser.ExceedsActionLimit(authorizedPlan.RawPostprocessId)\n', '', 1)
    elif args.mutate == 'skip-disallowed' and name == 'ActionPlanIntegrityPolicy.cs':
        text = text.replace('_parser.HasDisallowedProtocolTag(authorizedPlan.RawPostprocessId, _rawContext)', 'false', 1)
    elif args.mutate == 'ignore-order' and name == 'ActionPlanIntegrityPolicy.cs':
        text = text.replace('actual.Actions[i])', 'actual.Actions[expected.Actions.Count - 1 - i])', 1)
    elif args.mutate == 'ignore-parameters' and name == 'ActionPlanIntegrityPolicy.cs':
        text = text.replace('|| !string.Equals(pair.Value, actualValue, StringComparison.Ordinal)', '|| (!string.Equals(pair.Value, actualValue, StringComparison.Ordinal) && false)', 1)
    elif args.mutate == 'allow-action-star' and name == 'LegacyActionTagCatalog.cs':
        text = text.replace('        "ACTION:AGENDA",', '        "ACTION:*",\n        "ACTION:AGENDA",', 1)
    (out / name).write_text(text, encoding='utf-8')

(out / 'Program.cs').write_text((HERE / 'Harness.cs.txt').read_text(encoding='utf-8-sig'), encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = str(resolve_dotnet(ROOT))
env = minimal_test_environment(Path(dotnet), out)
import json,hashlib
(out/'generated-inputs-before-build.json').write_text(json.dumps({str(f.name):hashlib.sha256(f.read_bytes()).hexdigest() for f in out.iterdir() if f.is_file() and f.suffix in ('.cs','.csproj','.Config')},indent=2),encoding='utf-8')
result = subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release'], cwd=ROOT, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
