"""Actual candidate request ordering/state replacement probe; no game/provider/save."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import minimal_test_environment
p = argparse.ArgumentParser()
p.add_argument('--api', choices=['1.3', '1.4'], required=True)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--run-root', type=Path, required=True)
p.add_argument('--edition', choices=['CN', 'EN'], required=True)
a = p.parse_args()
candidate = a.candidate.resolve(strict=True)
digest = hashlib.sha256(candidate.read_bytes()).hexdigest().upper()
marker = json.loads(candidate.with_suffix('.build.json').read_text(encoding='utf-8-sig'))
assert marker['Sha256'] == digest and marker['BannerlordApi'] == a.api
out = a.run_root.resolve()
assert out.is_relative_to(ROOT / 'artifacts') and not out.exists(), out
out.mkdir(parents=True)
module = out / 'AnimusForge'
module.mkdir()
(module / 'SubModule.xml').write_text('<Module><Id value="AnimusForge" /></Module>')
copy = module / 'bin/Win64_Shipping_Client/versions' / a.api / 'AnimusForge.dll'
copy.parent.mkdir(parents=True)
shutil.copyfile(candidate, copy)
dotnet = Path(shutil.which('dotnet')).resolve()
powershell = shutil.which('powershell')
fixture_env = minimal_test_environment(dotnet, out)
# Select the installed 5.1 utility module instead of the parent's bundled 7.x module.
fixture_env['PSModulePath'] = str(Path(os.environ['SystemRoot']) / 'System32/WindowsPowerShell/v1.0/Modules')
fixture = subprocess.run([powershell, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    str(ROOT / 'tests/modules/AF.Module.Prompt/Composition/actual_module_fixture.ps1'),
    '-ProjectRoot', str(ROOT), '-ModuleRoot', str(module), '-Language', 'en' if a.edition == 'EN' else 'zh-CN'],
    cwd=ROOT, env=fixture_env, capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'fixture.log').write_text(fixture.stdout + fixture.stderr, encoding='utf8')
assert fixture.returncode == 0, fixture.stdout + fixture.stderr
framework = Path.home() / '.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
deps = ROOT / '.tmp/build_check/1.4'
fixture_sources = ROOT / 'tests/AF.GameAdapter.Bannerlord/TerminalLiveSettingsTests'
sources = [Path(__file__).with_name('LatestStateProbe.cs')]
items = ''.join('<Compile Include="' + escape(str(f)) + '" />' for f in sources)
for f in [copy, deps / '0Harmony.dll', deps / 'MCMv5.dll', deps / 'Newtonsoft.Json.dll', framework / 'Facades/netstandard.dll']:
    items += '<Reference Include="' + f.stem + '"><HintPath>' + escape(str(f)) + '</HintPath>' + ('<Private>false</Private>' if f == copy else '') + '</Reference>'
(out / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><StartupObject>LatestStateProbe</StartupObject><LangVersion>latest</LangVersion><PlatformTarget>x64</PlatformTarget><EnableDefaultCompileItems>false</EnableDefaultCompileItems><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(framework)) + '</FrameworkPathOverride><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
build = subprocess.run([str(dotnet), 'build', str(out / 'Probe.csproj'), '--nologo', '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')], cwd=ROOT, env=fixture_env, capture_output=True, text=True, encoding='utf8', errors='replace')
(out / 'build.log').write_text(build.stdout + build.stderr, encoding='utf8')
assert build.returncode == 0, build.stdout + build.stderr
refs = ROOT / ('_deps_auto' if a.api == '1.3' else '.tmp/build_check/1.4')
env = dict(fixture_env, AF_TEST_MODULE_ROOT=str(module), ANIMUSFORGE_DATA_ROOT=str(module))
run = subprocess.run([str(out / 'bin/Debug/net472/Probe.exe'), str(copy), str(module), str(refs), str(deps)], cwd=out, env=env,
    capture_output=True, text=True, encoding='utf8', errors='replace')
(out / 'run.log').write_text(run.stdout + run.stderr, encoding='utf8')
print(run.stdout + run.stderr)
(out / 'receipt.json').write_text(json.dumps(dict(candidate=str(candidate), sha256=digest, api=a.api, edition=a.edition, exit=run.returncode), indent=2))
assert hashlib.sha256(candidate.read_bytes()).hexdigest().upper() == digest
raise SystemExit(run.returncode)
