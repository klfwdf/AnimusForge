"""Replay current product DLL message owners; no game, network, or saved data writes."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import sys
from datetime import datetime

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--run-root', type=Path)
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
inputs = [ROOT / 'ShoutBehavior.cs', ROOT / 'ConversationMessage.cs']
inputs += list((ROOT / 'src/modules/AF.Module.Prompt/Composition').glob('*.cs'))
inputs += list((ROOT / 'src/modules/AF.Module.Conversation/Channels/Courier').glob('*Prompt*.cs'))
assert all(created >= path.stat().st_mtime for path in inputs), 'candidate predates a message assembly source'
out = new_run_root(ROOT, 'actual-message-assembly', a.run_root)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
source = Path(__file__).with_name('ActualMessageAssemblyChecks.cs')
(out / 'Checks.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Compile Include="' + str(source) + '"/></ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
refs = ROOT / ('local/bannerlord-refs/1.4.7.117484' if marker['BannerlordApi'] == '1.4' else '_deps_auto')
assert refs.is_dir(), 'candidate API references missing'
# Stage is only a read-only managed dependency source, never an implementation fallback.
stage = os.environ.get('AF_REPLAY_STAGE_BIN', '')
directories = [refs, ROOT / '_deps_auto']
if stage:
    assert Path(stage).is_dir(), 'managed dependencies path missing'
    directories.append(Path(stage))
build = subprocess.run([str(dotnet), 'build', str(out / 'Checks.csproj'), '-c', 'Release', '--nologo', '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
run = None
if build.returncode == 0:
    run = subprocess.run([str(dotnet), str(out / 'bin/Release/net8.0/Checks.dll'), str(candidate), digest] + [str(path) for path in directories], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=60)
log = build.stdout + build.stderr + (run.stdout + run.stderr if run else '')
(out / 'run.log').write_text(log, encoding='utf-8')
(out / 'result.json').write_text(json.dumps({'candidate': str(candidate), 'sha256': digest, 'api': marker['BannerlordApi'], 'buildExit': build.returncode, 'runExit': run.returncode if run else None}, indent=2), encoding='utf-8')
print(log)
print('EVIDENCE ' + str(out))
sys.exit(build.returncode or (run.returncode if run else 1))
