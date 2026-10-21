"""Replay current product DLL message owners; no game, network, or saved data writes."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape
from datetime import datetime

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--references', type=Path, help='The actual reference directory recorded by this candidate unified build')
p.add_argument('--dependency-dir', type=Path, action='append', default=[], help='Real mod dependency directory; does not override the candidate game references')
p.add_argument('--framework-references', type=Path, required=True, help='Installed .NET Framework 4.7.2 reference assemblies')
p.add_argument('--run-root', type=Path)
p.add_argument('--recorded-request', type=Path, help='Optional private keyless logged JSON payload for final-wire replay')
a = p.parse_args()
candidate = a.candidate.resolve(strict=True)
candidate.relative_to(ROOT)
assert 'single_module_stage' not in str(candidate).lower(), 'Stage product is not a candidate'
marker = json.loads(candidate.with_suffix('.build.json').read_text(encoding='utf-8-sig'))
digest = hashlib.sha256(candidate.read_bytes()).hexdigest().upper()
assert marker['Sha256'] == digest and marker['Role'] == 'Implementation'
assert marker['BannerlordApi'] in ('1.3', '1.4')
assert marker['BuildFlavor'] == 'ANIMUSFORGE_BANNERLORD_API_' + marker['BannerlordApi'].replace('.', '_')
created = datetime.fromisoformat(marker['CreatedUtc'].replace('Z', '+00:00')).timestamp()
inputs = [ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs', ROOT / 'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs']
inputs += list((ROOT / 'src/modules/AF.Module.Prompt/Composition').glob('*.cs'))
inputs += list((ROOT / 'src/modules/AF.Module.Conversation/Channels/Courier').glob('*Prompt*.cs'))
inputs += [ROOT / path for path in (
    'src/modules/AF.Module.Prompt/Retrieval/PromptRuleTextEvidence.cs',
    'src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalTexts.cs',
    'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryEdit.cs',
    'src/AF.GameAdapter.Bannerlord/Composition/EquipmentPromptCaptureAdapter.cs',
    'src/AF.GameAdapter.Bannerlord/UI/Editors/MemoryEditorUiText.cs',
    'extensions/AnimusForge.Coup/src/CoupSystem/CoupUiText.cs',
    'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs',
)]
assert all(created >= path.stat().st_mtime for path in inputs), 'candidate predates a message assembly source'
out = new_run_root(ROOT, 'actual-message-assembly', a.run_root)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
source = Path(__file__).with_name('ActualMessageAssemblyChecks.cs')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
refs = (a.references or ROOT / ('local/bannerlord-refs/1.4.7.117484' if marker['BannerlordApi'] == '1.4' else '_deps_auto')).resolve()
assert refs.is_dir(), 'candidate API references missing'
game_directories = [refs]
overlay = ROOT / '.tmp/build_check' / marker['BannerlordApi']
if overlay.is_dir() and overlay != refs:
    game_directories.append(overlay)
for directory in game_directories:
    versions = set(re.findall(r'v\d+\.\d+\.\d+\.\d+', (directory / 'TaleWorlds.Library.dll').read_bytes().decode('utf-16-le', errors='ignore')))
    assert versions == {marker['ReferenceGameVersion']}, (directory, versions, marker['ReferenceGameVersion'])
# Stage is only a read-only managed dependency source, never an implementation fallback.
stage = os.environ.get('AF_REPLAY_STAGE_BIN', '')
directories = [path.resolve(strict=True) for path in a.dependency_dir] + game_directories
if stage:
    assert Path(stage).is_dir(), 'managed dependencies path missing'
    directories.append(Path(stage))
framework = a.framework_references.resolve(strict=True)
assert (framework / 'mscorlib.dll').is_file(), 'framework references incomplete'
json_dll = next(path / 'Newtonsoft.Json.dll' for path in directories if (path / 'Newtonsoft.Json.dll').is_file())
(out / 'Checks.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><PlatformTarget>x64</PlatformTarget><EnableDefaultCompileItems>false</EnableDefaultCompileItems><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(framework)) + '</FrameworkPathOverride><NuGetAudit>false</NuGetAudit><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Compile Include="' + escape(str(source)) + '"/><Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(json_dll)) + '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
project = out / 'Checks.csproj'
project.write_text(project.read_text(encoding='utf-8').replace('</ItemGroup>', '<Compile Include="' + escape(str(source.with_name('RuntimeLanguageChecks.cs'))) + '"/></ItemGroup>'), encoding='utf-8')
project.write_text(project.read_text(encoding='utf-8').replace('</ItemGroup>', '<Compile Include="' + escape(str(source.with_name('TerminalSettingsChecks.cs'))) + '"/></ItemGroup>'), encoding='utf-8')
project.write_text(project.read_text(encoding='utf-8').replace('</ItemGroup>', '<Compile Include="' + escape(str(source.with_name('RequestPromptAuditChecks.cs'))) + '"/></ItemGroup>'), encoding='utf-8')
project.write_text(project.read_text(encoding='utf-8').replace('</ItemGroup>', '<Compile Include="' + escape(str(ROOT / 'tests/modules/AF.Module.Kingdom/StabilityRebellionRetirementChecks.cs')) + '"/></ItemGroup>'), encoding='utf-8')
project.write_text(project.read_text(encoding='utf-8').replace('</ItemGroup>', '<Reference Include="netstandard"><HintPath>' + escape(str(framework / 'Facades/netstandard.dll')) + '</HintPath></Reference></ItemGroup>'), encoding='utf-8')
build = subprocess.run([str(dotnet), 'build', str(project), '-c', 'Release', '--nologo', '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
run = None
if build.returncode == 0:
    runs = []
    for language in ('zh-CN', 'en'):
        # Exercise the unmodified module-path resolver against a private test
        # module, never the installed game or an old Stage directory.
        module = out / language / 'AnimusForge'
        data = module / 'ModuleData'
        data.mkdir(parents=True)
        (module / 'SubModule.xml').write_text('<Module><Id value="AnimusForge" /></Module>', encoding='utf-8')
        replay_candidate = module / 'bin/Win64_Shipping_Client/versions' / marker['BannerlordApi'] / 'AnimusForge.dll'
        replay_candidate.parent.mkdir(parents=True)
        shutil.copyfile(candidate, replay_candidate)
        assert hashlib.sha256(replay_candidate.read_bytes()).hexdigest().upper() == digest
        fixture = subprocess.run(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(Path(__file__).with_name("actual_module_fixture.ps1")), "-ProjectRoot", str(ROOT), "-ModuleRoot", str(module), "-Language", language], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60)
        (out / f"fixture-{language}.log").write_text(fixture.stdout + fixture.stderr, encoding="utf-8")
        assert fixture.returncode == 0, fixture.stdout + fixture.stderr
        run = subprocess.run([str(out / 'bin/Release/net472/Checks.exe'), str(replay_candidate), digest] + [str(path) for path in directories], cwd=out, env=dict(env, AF_TEST_MODULE_ROOT=str(module), AF_TEST_REFERENCES='|'.join(map(str, game_directories)), ANIMUSFORGE_DATA_ROOT=str(module), AF_TEST_RECORDED_REQUEST=str(a.recorded_request.resolve()) if a.recorded_request else ''), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=60)
        (out / f'run-{language}.log').write_text(run.stdout + run.stderr, encoding='utf-8')
        runs.append({'language': language, 'exitCode': run.returncode, 'module': str(module)})
        if run.returncode: break
log = build.stdout + build.stderr + ''.join((out / f'run-{item["language"]}.log').read_text(encoding='utf-8') for item in runs) if build.returncode == 0 else build.stdout + build.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
(out / 'result.json').write_text(json.dumps({'candidate': str(candidate), 'sha256': digest, 'api': marker['BannerlordApi'], 'references': str(refs), 'buildExit': build.returncode, 'runExit': run.returncode if run else None, 'packages': runs if build.returncode == 0 else []}, indent=2), encoding='utf-8')
print(log)
print('EVIDENCE ' + str(out))
sys.exit(build.returncode or (run.returncode if run else 1))
