from pathlib import Path
import sys, json, hashlib, subprocess, re
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
out=new_run_root(ROOT,'f3-memory-business',None)
paths=['src/modules/AF.Module.Memory/Summary/MemoryFailureNoticeOwner.cs','src/AF.Contracts/Internal/LlmContracts.cs','src/modules/AF.Module.Memory/Recovery/MemoryRecoverySeedRules.cs','src/AF.Contracts/Internal/InteractionContracts.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs','src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryRecoveryLedger.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs','src/modules/AF.Module.Memory/Records/NpcActionLedger.cs','src/modules/AF.Module.Memory/Summary/MemorySealingOwner.cs','src/modules/AF.Module.Memory/Summary/CooperativeMemoryQueueSort.cs','src/modules/AF.Module.Memory/Summary/MemoryMaintenanceWorkBudget.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryPlanningOwner.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryRunOwner.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Identity.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Memory/Records/ExecutionTranscriptStore.cs']
recovery=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs').read_text(encoding='utf-8-sig')
import importlib.util
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
day=ex.declaration(source,'private class DialogueDay').replace('private class','internal class') if 'private class DialogueDay' in source else ex.declaration(source,'internal class DialogueDay')
guards=[day]
for name in ['IsValidMemoryCommitMarker','IsMemoryRecoveryHexDigest']:
    m=re.search(r'internal static bool '+name+r'\([^;]+;',recovery);assert m and '=>' in m.group()
    guards.append(m.group())
(out/'Guards.cs').write_text('using System;using System.Linq;namespace AnimusForge { public partial class MyBehavior {'+'\n'.join(guards)+'}}',encoding='utf-8')
newtonsoft=ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
# Reuse the existing actual-owner closure; generated engine/config leaves stay controlled.
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests'))
from business_owner_fixture_support import include
files={Path(path).name:(ROOT/path).read_text(encoding='utf-8-sig') for path in paths}
files['Program.cs']=(HERE/'Program.cs').read_text(encoding='utf-8-sig')
files['Guards.cs']=(out/'Guards.cs').read_text(encoding='utf-8-sig')
files['ControlledUnusedQueueFacts.cs']='namespace AnimusForge {public partial class MyBehavior {private static bool IsMemoryEntityEligibleForCompressedMemory(string id)=>throw new System.InvalidOperationException("Live eligibility outside state-only replay");private static int GetMemoryOverviewStartBlockCountFromSettings()=>throw new System.InvalidOperationException("Live settings outside state-only replay");private static int GetCurrentGameDayIndexSafe()=>throw new System.InvalidOperationException("Live clock outside state-only replay");}}'
closure_manifest=[]
include(ROOT,files,closure_manifest,ex)
for name,value in files.items(): (out/name).write_bytes(value.encode('utf-8'))
(out/'closure-manifest.json').write_text(json.dumps(closure_manifest,indent=2),encoding='utf-8')
links=''.join('<Compile Include="'+name+'"/>' for name in files)
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+links+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(newtonsoft)+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out/'manifest.json').write_text(json.dumps({str(p):hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths},indent=2),encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],env=env,cwd=out,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
if build.returncode:print(build.stdout+build.stderr);raise SystemExit(build.returncode)
run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],env=env,cwd=out,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);print('OUTPUT',out);raise SystemExit(run.returncode)
