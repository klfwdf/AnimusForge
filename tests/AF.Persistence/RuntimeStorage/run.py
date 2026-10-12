"""Isolated real-source storage replay. No game/profile writes or production build."""
from pathlib import Path
from xml.sax.saxutils import escape
import argparse, hashlib, json, re, subprocess, sys

ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p=argparse.ArgumentParser()
p.add_argument('--run-root',type=Path)
p.add_argument('--core-only',action='store_true')
a=p.parse_args()
OUT=new_run_root(ROOT,'runtime-storage',a.run_root)
DOTNET=resolve_dotnet(ROOT)
receipts=[]

def execute(command, directory, env, name):
    result=subprocess.run([str(x) for x in command],cwd=directory,env=env,capture_output=True,
                          text=True,encoding='utf-8',errors='replace',timeout=180)
    (directory/(name+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    receipts.append({'command':[str(x) for x in command],'cwd':str(directory),'exit':result.returncode,
                     'log':str(directory/(name+'.log'))})
    if result.returncode:
        print((result.stdout+result.stderr)[-7000:])
        (OUT/'receipt.json').write_text(json.dumps(receipts,indent=2),encoding='utf-8')
        raise SystemExit(result.returncode)
    print((result.stdout+result.stderr).strip()[-1600:])

def project_copy(source, target):
    text=source.read_text('utf-8-sig')
    text=re.sub(r'(<Compile Include=")([^"]+)(")',
                lambda m:m[1]+escape(str((source.parent/m[2]).resolve()))+m[3],text)
    target.write_text(text,encoding='utf-8')

core=OUT/'core';core.mkdir()
project_copy(HERE/'RuntimeStorageTests.csproj',core/'Tests.csproj')
env=minimal_test_environment(DOTNET,core)
execute([DOTNET,'run','--project',core/'Tests.csproj','-c','Release','--',core/'fixtures'],core,env,'run')

old=OUT/'data-paths';old.mkdir()
project_copy(ROOT/'tests/AF.Persistence/DataPaths/DataPathsTests.csproj',old/'Tests.csproj')
env=minimal_test_environment(DOTNET,old)
env['AF_DATA_PATHS_TEST_ROOT']=str(old/'module-fixture')
execute([DOTNET,'run','--project',old/'Tests.csproj','-c','Release'],ROOT,env,'run')

# Build copied editor projects rather than writing the original shared bin/obj.
sdk10=Path('C:/Program Files/dotnet/dotnet.exe')
editor=OUT/'editor';editor.mkdir()
core_editor=editor/'core';core_editor.mkdir()
test_editor=editor/'tests';test_editor.mkdir()
core_path=ROOT/'tools/PlayerExportsEditor/src/PlayerExportsEditor.Core'
links=''.join('<Compile Include="'+escape(str(x))+'" />' for x in core_path.glob('*.cs'))
links+='<Compile Include="'+escape(str(ROOT/'src/AF.Persistence/AnimusForgeDataPaths.cs'))+'" />'
(core_editor/'Core.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+links+'</ItemGroup></Project>',encoding='utf-8')
smoke=ROOT/'tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/Program.cs'
(test_editor/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="'+escape(str(smoke))+'"/><ProjectReference Include="../core/Core.csproj"/></ItemGroup></Project>',encoding='utf-8')
env=minimal_test_environment(sdk10,editor)
execute([sdk10,'build',test_editor/'Tests.csproj','-c','Release'],editor,env,'build')
exe=test_editor/'bin/Release/net10.0/Tests.dll'
execute([sdk10,exe,'--path-contract'],editor,env,'path')
execute([sdk10,exe,'--path-contract-invalid'],editor,env,'path-invalid')
execute([sdk10,exe,'--installed-library-contract',editor/'installed-fixture'],editor,env,'installed-library')
execute([sdk10,exe,'--backup-contract','runtime-storage-'+OUT.name.replace('run-','')],ROOT,env,'backup')
isolated=editor/'full-fixture';isolated.mkdir()
env_full=minimal_test_environment(sdk10,isolated)
env_full['ANIMUSFORGE_DATA_ROOT']=str(isolated/'data/AnimusForge')
execute([sdk10,exe,'--isolated-full',isolated],editor,env_full,'full-smoke')

(OUT/'receipt.json').write_text(json.dumps(receipts,indent=2),encoding='utf-8')
(OUT/'source-manifest.json').write_text(json.dumps({str(x.relative_to(ROOT)):hashlib.sha256(x.read_bytes()).hexdigest()
 for x in [HERE/'Program.cs',ROOT/'src/AF.Persistence/AnimusForgeDataPaths.cs',smoke]+list(core_path.glob('*.cs'))},indent=2),encoding='utf-8')
print('PASS core/old-contract/editor source replay:',OUT)
