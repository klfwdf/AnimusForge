"""Compile actual Summary owner sources in a fresh, repository-local output root."""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
SOURCES = [ROOT / 'src/modules/AF.Module.Memory/Summary/MemorySummaryRules.cs',
           ROOT / 'src/modules/AF.Module.Memory/Summary/MemorySummaryAttemptRunner.cs']

def main() -> int:
    out = ROOT / 'artifacts/j17b/session-20260930/p2-summary' / ('run-' + uuid4().hex)
    out.mkdir(parents=True, exist_ok=False)
    dotnet = ROOT / 'local/dotnet/8.0.425/dotnet.exe'
    newtonsoft = ROOT / 'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
    for path in [dotnet, newtonsoft, *SOURCES, HERE / 'Program.cs']:
        if not path.is_file(): raise FileNotFoundError(path)
    links = '\n'.join(f'<Compile Include="{p.as_posix()}" />' for p in [*SOURCES, HERE / 'Program.cs'])
    csproj = ('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
              '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
              '<LangVersion>latest</LangVersion></PropertyGroup><ItemGroup>' + links +
              f'<Reference Include="Newtonsoft.Json"><HintPath>{newtonsoft.as_posix()}</HintPath></Reference>'
              '</ItemGroup></Project>')
    (out / 'Proof.csproj').write_text(csproj, encoding='utf-8')
    (out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    env = {key: os.environ[key] for key in ('SYSTEMROOT', 'WINDIR', 'PATH', 'TEMP', 'TMP', 'USERPROFILE', 'APPDATA',
                                           'LOCALAPPDATA', 'PROGRAMDATA', 'ALLUSERSPROFILE', 'HOMEDRIVE', 'HOMEPATH',
                                           'PROGRAMFILES', 'PROGRAMFILES(X86)', 'COMMONPROGRAMFILES',
                                           'COMMONPROGRAMFILES(X86)', 'COMSPEC')
           if key in os.environ}
    env.update(DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(out / 'cli'), NUGET_PACKAGES=str(out / 'nuget'),
               DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1',
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
    manifest = {'sources': {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in SOURCES}}
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    build = subprocess.run([str(dotnet), 'build', str(out / 'Proof.csproj'), '-c', 'Release', '--nologo',
                            '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')], cwd=out, env=env,
                           capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
    (out / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
    if build.returncode:
        print('BUILD_FAILED', build.returncode, out)
        print(build.stdout + build.stderr)
        return 2
    run = subprocess.run([str(dotnet), str(out / 'bin/Release/net8.0/Proof.dll')], cwd=out, env=env,
                         capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=60)
    (out / 'run.log').write_text(run.stdout + run.stderr, encoding='utf-8')
    print(run.stdout + run.stderr, end='')
    print('OUTPUT', out)
    return run.returncode

if __name__ == '__main__':
    try: sys.exit(main())
    except Exception as exc:
        print('J17_SUMMARY_OWNER_TOOL_ERROR', type(exc).__name__, str(exc))
        sys.exit(2)
