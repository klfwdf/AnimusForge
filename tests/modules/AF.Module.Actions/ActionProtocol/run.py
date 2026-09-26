"""Compile and execute the actual J09 action protocol owners."""
from pathlib import Path
import argparse, os, subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument('--mutate', choices=['skip-overflow','skip-disallowed','ignore-order','ignore-parameters','allow-action-star'])
args = parser.parse_args()
out = HERE / '.generated' / (args.mutate or 'current')
out.mkdir(parents=True, exist_ok=True)

sources = {
    'InteractionContracts.cs': ROOT / 'Refactor/Contracts/InteractionContracts.cs',
    'LlmContracts.cs': ROOT / 'Refactor/Contracts/LlmContracts.cs',
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
dotnet = os.environ.get('AF_DOTNET', r'G:/AFMOD/.dotnet-sdk/dotnet.exe')
env = os.environ.copy()
env.update(DOTNET_ROOT=str(Path(dotnet).parent), DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'), DOTNET_NOLOGO='1')
result = subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release'], cwd=ROOT, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
