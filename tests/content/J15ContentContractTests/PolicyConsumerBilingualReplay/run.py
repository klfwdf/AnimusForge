"""Run real compiled AF consumers with real TW dependencies and fixed package fixtures.

Fixtures are made with create-content-fixtures.ps1 and the production content selectors.
This runner never builds AF, stages a distribution, deploys, or replaces game files.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode

ap = argparse.ArgumentParser()
ap.add_argument('--output', required=True)
ap.add_argument('--fixtures', required=True)
ap.add_argument('--before-root', required=True)
ap.add_argument('--candidate-13', default='bin/Release/single_module_artifacts/versions/1.3/AnimusForge.dll')
ap.add_argument('--candidate-14', default='bin/Release/single_module_artifacts/versions/1.4/AnimusForge.dll')
args = ap.parse_args()
OUT = (ROOT / args.output).resolve(); OUT.mkdir(parents=True, exist_ok=True)
BEFORE = (ROOT / args.before_root).resolve(); FIXTURES = (ROOT / args.fixtures).resolve()

def load(path):
    return json.loads(path.read_text('utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

base = ROOT / 'content/modules/AF.Module.Prompt/ModuleData'
raw = load(base / 'PromptCompatibility.json')
zh = load(base / 'PromptComposerTexts.zh-CN.json')
en = load(base / 'PromptComposerTexts.en.json')
data = {
    'agendaRawParts': raw['policy_agenda_source_parts'].split('|'),
    'agendaCn': zh['memory_policy_agenda_submission'], 'agendaEn': en['memory_policy_agenda_submission'],
    'factPrefixes': [raw[k] for k in ('afef_npc_action', 'afef_player_action', 'afef_npc_action_bracket', 'afef_player_action_bracket')],
    'factScopes': [raw[k] for k in ('main_history_raw_019', 'history_section_scope_current', 'history_section_scope_historical')],
    'noNumericRaw': raw['policy_weekly_no_numeric_change'], 'noNumericEn': en['policy_weekly_no_numeric_change'],
    'moduleCues': [],
}
for kind, suffix in (('ruler', 'Ruler'), ('vassal', 'Vassal')):
    key = 'policy_weekly_' + kind + '_parts'
    data['weeklyRaw' + suffix] = raw[key].split('|')
    data['weeklyEn' + suffix] = en[key].split('|')
    key = 'policy_weekly_label_' + kind
    data['labelRaw' + suffix] = raw[key]; data['labelEn' + suffix] = en[key]

# Cue oracle is the delivered, pre-closeout source, not the migrated resource.
# Retain all original English aliases and their order as well as the Chinese terms.
migration = load(ROOT / 'artifacts/policy-bilingual-closeout-20261012/root-literal-migration.json')
old_compat = load(BEFORE / 'content/modules/PolicySystem/ModuleData/PolicyCompatibilityTexts.json')
for entry in migration['files']:
    path = entry['file']
    if '/Effects/Modules/' not in path: continue
    source = (BEFORE / path).read_text('utf-8-sig')
    cues = re.search(r'cueTerms:\s*new\[\]\s*\{(.*?)\}\s*,', source, re.S)[1]
    cues = re.sub(r'PolicyCompatibilityTexts.Get\("([^"\n]+)"\)', lambda m: json.dumps(old_compat[m[1]], ensure_ascii=False), cues)
    cues = [decode(m[0]) for m in LEX.finditer(cues) if m.lastgroup == 'string']
    module_type = re.search(r'typeof\(global::([^()]+)\)', source)[1]
    data['moduleCues'].append({'type': module_type, 'cueTerms': cues,
        'id': re.search(r'\bid:\s*"([^"]+)"', source)[1],
        'order': int(re.search(r'\border:\s*(\d+)', source)[1])})
assert len(data['moduleCues']) == 16
cases = OUT / 'cases.json'; cases.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

inputs = [ROOT / 'src/modules/AF.Module.Prompt/Composition/PolicyWeeklyFactPromptProjection.cs',
    ROOT / 'src/modules/AF.Module.Prompt/Composition/MemorySystemFactProjection.cs',
    ROOT / 'src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.cs',
    Path(__file__), Path(__file__).with_name('Program.cs'), cases]
inputs += [base / name for name in ('PromptCompatibility.json', 'PromptComposerTexts.zh-CN.json', 'PromptComposerTexts.en.json')]
inputs += [(ROOT / args.candidate_13).resolve(), (ROOT / args.candidate_14).resolve()]
inputs += [BEFORE / row['file'] for row in migration['files'] if '/Effects/Modules/' in row['file']]
inputs += [FIXTURES / 'selection.json']
before_hashes = {str(p): sha(p) for p in inputs}
framework = Path(os.environ.get('USERPROFILE', 'C:/Users/29310')) / '.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
assert (framework / 'mscorlib.dll').is_file()
rows = []
for api, candidate in (('1.3', args.candidate_13), ('1.4', args.candidate_14)):
    api_out = OUT / api; api_out.mkdir(exist_ok=True)
    refs = ROOT / ('_deps_auto' if api == '1.3' else '.tmp/build_check/1.4')
    game = Path('F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord')
    runtime_dirs = [refs, game / 'Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client',
        game / 'Modules/AnimusForge/bin/Win64_Shipping_Client',
        game / 'Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client',
        game / 'Modules/Bannerlord.UIExtenderEx/bin/Win64_Shipping_Client',
        game / 'Modules/Bannerlord.ButterLib/bin/Win64_Shipping_Client']
    assert (runtime_dirs[1] / '0Harmony.dll').is_file()
    references = ''.join('<Reference Include="' + name + '"><HintPath>' + escape(str(refs / (name + '.dll'))) + '</HintPath></Reference>'
        for name in ('Newtonsoft.Json', 'TaleWorlds.Library', 'TaleWorlds.Localization'))
    references += '<Reference Include="netstandard"><HintPath>' + escape(str(framework / 'Facades/netstandard.dll')) + '</HintPath></Reference>'
    project = api_out / 'Replay.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net472</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><PlatformTarget>x64</PlatformTarget><EnableDefaultCompileItems>false</EnableDefaultCompileItems><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>'
        + escape(str(framework)) + '</FrameworkPathOverride></PropertyGroup><ItemGroup><Compile Include="' + escape(str(Path(__file__).with_name('Program.cs'))) + '"/>' + references + '</ItemGroup></Project>', encoding='utf-8')
    build_cmd = ['dotnet', 'build', str(project), '-c', 'Release', '--nologo']
    build = subprocess.run(build_cmd, cwd=ROOT, capture_output=True, timeout=120)
    (api_out / 'build.log').write_bytes(build.stdout + build.stderr)
    if build.returncode:
        rows.append({'api': api, 'buildExitCode': build.returncode, 'command': build_cmd}); continue
    executable = api_out / 'bin/Release/net472/Replay.exe'
    for language in ('CN', 'EN'):
        module = api_out / language / 'AnimusForge'
        shutil.copytree(FIXTURES / language / 'AnimusForge', module, dirs_exist_ok=True)
        copied = module / 'bin/Win64_Shipping_Client/versions' / api / 'AnimusForge.dll'
        copied.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(ROOT / candidate, copied)
        result = api_out / (language.lower() + '-result.json')
        cmd = [str(executable), str(copied), '|'.join(str(p) for p in runtime_dirs), language, str(module), str(cases), str(result)]
        run = subprocess.run(cmd, cwd=ROOT, capture_output=True, timeout=120)
        (api_out / (language.lower() + '.log')).write_bytes(run.stdout + run.stderr)
        row = {'api': api, 'package': language, 'exitCode': run.returncode, 'command': cmd,
            'candidateSha256': sha(ROOT / candidate), 'copiedSha256': sha(copied)}
        if run.returncode == 0:
            receipt = load(result); row.update({'checks': receipt['checks'], 'counts': receipt['counts']})
        rows.append(row)
        print(json.dumps({k: row[k] for k in ('api', 'package', 'exitCode', 'checks') if k in row}, ensure_ascii=False), flush=True)
after_hashes = {str(p): sha(p) for p in inputs}
drift = [p for p in before_hashes if before_hashes[p] != after_hashes[p]]
summary = {'passed': len(rows) == 4 and all(r.get('exitCode') == 0 for r in rows) and not drift,
    'casesSha256': sha(cases), 'candidateRuns': rows, 'inputsBefore': before_hashes, 'inputsAfter': after_hashes,
    'changedDuringRun': drift, 'coverage': 'Actual compiled consumers, native language switching and pure DTO producers. No gameplay, real model, old save, official package or deployment.'}
(OUT / 'summary.json').write_text(json.dumps(summary, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
print('PASS' if summary['passed'] else 'FAIL', 'policy compiled consumer matrix; input drift', len(drift))
raise SystemExit(0 if summary['passed'] else 1)
