from pathlib import Path
import sys, json, hashlib, subprocess, re, argparse
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);parser.add_argument('--mutate',choices=['ignore-bulletin-epoch']);parser.add_argument('--target-framework',choices=['net8.0','net10.0'],default='net8.0');args=parser.parse_args()
out=new_run_root(ROOT,'f4-weekly-generation',args.run_root)
paths=['src/modules/AF.Module.Weekly/Generation/WeeklyNoticeStateOwner.cs','src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs','src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs','src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs','src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs','src/modules/AF.Module.Weekly/Generation/WeeklyGenerationModels.cs','src/modules/AF.Module.Weekly/Generation/WeeklyGenerationAttemptOwner.cs','src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
import importlib.util
paths.append('src/modules/AF.Module.Weekly/Panel/WeeklyReportArchivePolicy.cs')
paths.append('src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Regional.cs')
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
api=ex.declaration(source,'internal sealed class ApiCallResult')+'\n'+ex.declaration(source,'internal sealed class EventRecordEntry')
panel=(ROOT/'src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs').read_text(encoding='utf-8-sig')
(out/'PanelData.cs').write_text('using System;using System.Collections.Generic;namespace AnimusForge {'+ex.declaration(panel,'internal sealed class WorldBulletinPanelData')+'}',encoding='utf-8')
retry=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs').read_text(encoding='utf-8-sig')
spans=[ex.declaration(retry,'public static string BuildFailureDetail('),ex.declaration(retry,'private static string NormalizeFullText(')]
(ROOT/'artifacts').exists()
(out/'Guards.cs').write_text('using System;using System.Text;namespace AnimusForge { public partial class MyBehavior {'+api+'} public static class LlmRetryPrompt {'+'\n'.join(spans)+'}}',encoding='utf-8')
# Controlled settings leaf only; generation and notice state remain the real owners.
with (out/'Guards.cs').open('a',encoding='utf-8') as settings:
 settings.write('namespace AnimusForge { internal sealed class DuelSettings { internal bool UseWorldBulletin=true,AutoGenerateWeeklyReports=true,AutoGenerateWorldBulletins=true; internal static DuelSettings Current=new(); internal static DuelSettings GetSettings()=>Current; }}')
linked_paths=[ROOT/path for path in paths]
if args.mutate:
 original=ROOT/'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs'
 text=original.read_text(encoding='utf-8-sig');marker='if (epoch != _collectionEpoch)'
 assert text.count(marker)==1
 modified=out/'WorldBulletinStateOwner.Mutated.cs';modified.write_text(text.replace(marker,'if (false && epoch != _collectionEpoch)'),encoding='utf-8');linked_paths[linked_paths.index(original)]=modified
links=''.join('<Compile Include="'+str(path)+'"/>' for path in linked_paths)+ '<Compile Include="'+str(HERE/'Program.cs')+'"/><Compile Include="Guards.cs"/><Compile Include="PanelData.cs"/>'
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>'+args.target_framework+'</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+links+'<Reference Include="Newtonsoft.Json"><HintPath>$(MSBuildSDKsPath)/../Newtonsoft.Json.dll</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out/'manifest.json').write_text(json.dumps({'sources':{str(p):hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths},'tests':{name:hashlib.sha256((HERE/name).read_bytes()).hexdigest() for name in ['run.py','Program.cs']},'revision':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'targetFramework':args.target_framework,'mutation':args.mutate,'provider':'STUBBED','game':'STUBBED','liveSav':'NOT_RUN'},indent=2),encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
for key in ('DOTNET_CLI_HOME','USERPROFILE','APPDATA','LOCALAPPDATA','TEMP','TMP'):
    if key in env: Path(env[key]).mkdir(parents=True,exist_ok=True)
env['PYTHONDONTWRITEBYTECODE']='1'
build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],env=env,cwd=out,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
if build.returncode:print(build.stdout+build.stderr);raise SystemExit(build.returncode)
run=subprocess.run([str(dotnet),str(out/'bin/Release'/args.target_framework/'Proof.dll')],env=env,cwd=out,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);print('OUTPUT',out);raise SystemExit(run.returncode)
