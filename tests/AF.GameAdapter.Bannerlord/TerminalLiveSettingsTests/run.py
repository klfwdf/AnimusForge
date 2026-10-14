from pathlib import Path
import argparse, hashlib, json, shutil, subprocess, sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser()
p.add_argument('--api', choices=['1.3', '1.4'], required=True)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--baseline', action='store_true')
a = p.parse_args()
candidate = a.candidate.resolve(strict=True)
digest = hashlib.sha256(candidate.read_bytes()).hexdigest().upper()
refs = ROOT / ('_deps_auto' if a.api == '1.3' else '.tmp/build_check/1.4')
if not a.baseline:
    marker = json.loads(candidate.with_suffix('.build.json').read_text(encoding='utf-8-sig'))
    assert marker['Sha256'] == digest and marker['BannerlordApi'] == a.api
out = new_run_root(ROOT, 'terminal-live-settings', None)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
module = out / 'AnimusForge'
module.mkdir()
(module / 'SubModule.xml').write_text('<Module><Id value="AnimusForge" /></Module>', encoding='utf-8')
copy = module / 'bin/Win64_Shipping_Client/versions' / a.api / 'AnimusForge.dll'
copy.parent.mkdir(parents=True)
shutil.copyfile(candidate, copy)
fixture = subprocess.run(['powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(ROOT / 'tests/modules/AF.Module.Prompt/Composition/actual_module_fixture.ps1'), '-ProjectRoot', str(ROOT), '-ModuleRoot', str(module), '-Language', 'zh-CN'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
(out / 'fixture.log').write_text(fixture.stdout + fixture.stderr, encoding='utf-8')
assert fixture.returncode == 0, fixture.stdout + fixture.stderr
framework = Path('C:/Users/29310/.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2')
deps = ROOT / '.tmp/build_check/1.4'
sources = [Path(__file__).with_name('Program.cs'), ROOT / 'tests/modules/AF.Module.Prompt/Composition/TerminalSettingsChecks.cs']
if not a.baseline:
    sources.append(Path(__file__).with_name('SupplementalChecks.cs'))
items = ''.join('<Compile Include="' + escape(str(f)) + '" />' for f in sources)
items += ''.join('<Reference Include="' + f.stem + '"><HintPath>' + escape(str(f)) + '</HintPath>' + ('<Private>false</Private>' if f == copy else '') + '</Reference>' for f in [copy, deps / '0Harmony.dll', deps / 'MCMv5.dll', deps / 'Newtonsoft.Json.dll', framework / 'Facades/netstandard.dll'])
(out / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><PlatformTarget>x64</PlatformTarget><DefineConstants>' + ('AF_BASELINE' if a.baseline else '') + '</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(framework)) + '</FrameworkPathOverride><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
build = subprocess.run([str(dotnet), 'build', str(out / 'Probe.csproj'), '--nologo', '-p:RestoreConfigFile=' + str(out / 'NuGet.Config'), '-p:UseSharedCompilation=false'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
(out / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
code = build.returncode
if not code:
    ui = Path('F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client')
    command = [str(out / 'bin/Debug/net472/Probe.exe'), str(copy), str(module), str(refs), str(deps)]
    if a.api == '1.4' and not a.baseline:
        command.append(str(ui))
    run = subprocess.run(command, cwd=out, capture_output=True, text=True, encoding='utf-8', errors='replace', env=dict(env, AF_TEST_MODULE_ROOT=str(module), ANIMUSFORGE_DATA_ROOT=str(module)))
    code = run.returncode
    (out / 'run.log').write_text(run.stdout + run.stderr, encoding='utf-8')
    print(run.stdout + run.stderr)
else:
    print(build.stdout + build.stderr)
assert hashlib.sha256(candidate.read_bytes()).hexdigest().upper() == digest
(out / 'receipt.json').write_text(json.dumps({'candidate': str(candidate), 'sha256': digest, 'api': a.api, 'baseline': a.baseline, 'exit': code, 'scope': 'Workspace fixture, actual MCM JSON/product VMs; no live UI or native/HTTP'}, indent=2))
print(out)
sys.exit(code)
