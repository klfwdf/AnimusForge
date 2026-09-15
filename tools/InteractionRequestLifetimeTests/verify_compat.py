from pathlib import Path
import importlib.util,subprocess,shutil,hashlib
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('build',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
base=[r'G:\Python310\python.exe','-X','utf8','-B',str(HERE/'run.py')]
surfaces=[]
for flag in [['--main'],[]]:
 r=subprocess.run(base+flag+['--case','surface'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',timeout=90)
 assert r.returncode==0,r.stdout+r.stderr;surfaces.append(r.stdout)
assert surfaces[0]==surfaces[1],'Public constructor/method signatures differ from main'
print('PASS coordinator public signatures equal pinned main')
out=HERE/'.generated/compat';out.mkdir(parents=True,exist_ok=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
original=HERE/'.generated/main/bin/Release/net8.0/InteractionRequestLifetimeChecks.dll'
replacement=HERE/'.generated/current/bin/Release/net8.0/InteractionRequestLifetimeChecks.dll'
project=out/'LegacyClient.csproj'
project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="{escape(str(HERE/'LegacyClient.cs'))}"/><Reference Include="InteractionRequestLifetimeChecks"><HintPath>{escape(str(original))}</HintPath></Reference></ItemGroup></Project>''',encoding='utf-8')
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['build',str(project),'-c','Release'],out)
assert code==0,log
client=out/'bin/Release/net8.0/LegacyClient.dll';before=hashlib.sha256(client.read_bytes()).hexdigest()
# Only the generated reference copy changes; main/current source artifacts remain separate.
shutil.copy2(replacement,client.parent/original.name)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',[str(client)],out)
assert hashlib.sha256(client.read_bytes()).hexdigest()==before,'Client was recompiled after replacement'
print(log,end='');(out/'run.log').write_text('clientSha256='+before+'\n'+log,encoding='utf-8');assert code==0,log
