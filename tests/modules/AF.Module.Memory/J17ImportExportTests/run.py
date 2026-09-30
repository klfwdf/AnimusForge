from pathlib import Path
import os
import subprocess

root = Path(__file__).resolve().parents[4]
here = Path(__file__).resolve().parent
out = root / 'artifacts/j17b/session-20260930/p3-import-export'
out.mkdir(parents=True, exist_ok=True)
project = out / 'J17ImportExportTests.csproj'
project.write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><RestoreSources></RestoreSources></PropertyGroup><ItemGroup><Compile Include="../../../../src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs"/><Compile Include="../../../../src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.cs"/><Compile Include="../../../../tests/modules/AF.Module.Memory/J17ImportExportTests/OwnerChecks.cs"/></ItemGroup></Project>''', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = os.environ.get('AF_DOTNET10', r'C:\Program Files\dotnet\dotnet.exe')
env = {'PATH': os.path.dirname(dotnet), 'SystemRoot': os.environ.get('SystemRoot', r'C:\Windows'),
       'DOTNET_CLI_TELEMETRY_OPTOUT': '1', 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE': '1',
       'DOTNET_CLI_HOME': str(out), 'TEMP': str(out), 'TMP': str(out)}
for key in ('APPDATA', 'LOCALAPPDATA', 'USERPROFILE', 'HOMEDRIVE', 'HOMEPATH',
            'PROGRAMDATA', 'ALLUSERSPROFILE', 'PROGRAMFILES'):
    if key in os.environ:
        env[key] = os.environ[key]
restore = subprocess.run([dotnet, 'restore', str(project), '--configfile', str(out / 'NuGet.Config')],
                         cwd=out, env=env, text=True, encoding='utf-8', errors='replace',
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
if restore.returncode:
    (out / 'run.log').write_text(restore.stdout, encoding='utf-8')
    print(restore.stdout, end='')
    raise SystemExit(restore.returncode)
result = subprocess.run([dotnet, 'run', '--project', str(project), '-c', 'Release', '--no-restore'],
                        cwd=out, env=env, text=True, encoding='utf-8', errors='replace',
                        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
(out / 'run.log').write_text(result.stdout, encoding='utf-8')
print(result.stdout, end='')
raise SystemExit(result.returncode)
