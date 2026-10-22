"""Exact production-method replay with detached hero fixtures; no native game/network."""
import argparse, hashlib, importlib.util, json, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--nuget-cache', type=Path)
args = p.parse_args()
out = new_run_root(ROOT, 'marriage-tags', args.run_root)
source_path = ROOT / 'src/modules/AF.Module.Social/Host/RomanceSystemBehavior.cs'
source = source_path.read_text('utf-8-sig')
base = ROOT / 'content/modules/AF.Module.Social/ModuleData'
cn = json.loads((base/'SocialPromptTexts.json').read_text('utf-8-sig'))
en = json.loads((base/'SocialPromptTexts.en.json').read_text('utf-8-sig'))
assert set(cn) == set(en), 'Social prompt key mismatch'
import re
for key in cn:
    assert sorted(re.findall(r'\{\d+(?::[^{}]+)?\}', cn[key])) == sorted(re.findall(r'\{\d+(?::[^{}]+)?\}', en[key])), key
    assert not re.search('[\u4e00-\u9fff]', en[key]), key
for suffix in ['', '.en']:
    config = json.loads((ROOT/('content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts'+suffix+'.json')).read_text('utf-8-sig'))
    rule = next(r for r in config['RulePrompts'] if r['Id'] == 'marriage')
    assert {r['Tag'] for r in rule['PostprocessRules']} == {'[ACTION:MARRIAGE_FORMAL:playerClanHeroId:targetHeroId]', '[ACTION:MARRIAGE_CLAN:playerClanHeroId:targetHeroId]', '[ACTION:MARRIAGE_ELOPE:targetHeroId]', '[ACTION:DIVORCE:playerClanHeroId:targetHeroId]'}
    assert '[ACTION:' not in rule['Instruction'] and all('[ACTION:' not in v for v in rule['RuntimeInstructionTemplates'].values()), 'tags leaked into main prompt'
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
import re
members = []
for name in ['MarriagePostprocessContext', 'GetMarriageClanMembers', 'GetMarriageRelationLabel', 'ResolveMarriageHeroId',
             'CanExecuteMarriageTagPair', 'CanExecuteFormalMarriageTagPair', 'CanExecuteElopeMarriageTagPair',
             'NormalizeMarriagePostprocessTags', 'StripMarriageActionTags', 'ContainsMarriageActionTags',
             'TryExecuteFormalMarriage', 'TryExecuteElopeMarriage', 'ApplyMarriageTags']:
    match = re.search(r'(?:private|internal|public) (?:static )?(?:sealed class ' + name + r'\b|[^\n]+? ' + name + r'\()', source)
    assert match, 'missing production member: ' + name
    members.append(extract.declaration(source, match[0]))
regexes = '\n'.join(line for line in source.splitlines() if 'private static readonly Regex ' in line)
harness = (HERE / 'Harness.cs.txt').read_text('utf-8')
(out / 'Program.cs').write_text(harness.replace('/* PRODUCTION */', regexes + '\n' + '\n'.join(members)), 'utf-8')
(out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649</NoWarn></PropertyGroup></Project>', 'utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', 'utf-8')
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
if args.nuget_cache:
    cache = args.nuget_cache.resolve(strict=True)
    assert cache.is_relative_to(ROOT), 'cache must stay inside the workspace'
    env['NUGET_PACKAGES'] = str(cache)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Replay.csproj'), '-c', 'Release'], env=env, cwd=out, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
(out / 'run.log').write_text(result.stdout + result.stderr, 'utf-8')
print(result.stdout + result.stderr)
assert source_path.read_text('utf-8-sig') == source, 'production input changed during replay'
(out / 'inputs.json').write_text(json.dumps({'source': str(source_path.relative_to(ROOT)), 'sha256': hashlib.sha256(source_path.read_bytes()).hexdigest(), 'uncovered': ['native marriage/clan mutation', 'live model classification', 'save acceptance']}, indent=2), 'utf-8')
raise SystemExit(result.returncode)
