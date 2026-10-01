from pathlib import Path
import os, subprocess, sys, importlib.util, argparse
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests')); from output_isolation import minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=(args.run_root or ROOT/'artifacts/af2-host-terminal-closeout/line-b/memory-recall-tests').resolve();out.mkdir(parents=True,exist_ok=True)
s=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
text=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs').read_text(encoding='utf-8-sig')
start=text.index('internal static bool ShouldCompleteInitialInteractionMemoryNotoriety(')
guard=text[start:text.index(';',start)+1]
(out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@INITIAL_GUARD@@',guard),encoding='utf-8')
paths=['src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs','src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs','src/modules/AF.Module.Memory/Records/MemoryRecallCandidate.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/modules/AF.Module.Prompt/Composition/PreprocessFormatException.cs','src/modules/AF.Module.Prompt/Retrieval/IntentQueryOptimizer.cs']
paths += ['src/modules/AF.Module.Memory/Recovery/InteractionMemoryRecoveryLedger.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryAuxiliaryCompletionCoordinator.cs']
reference=ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(ROOT/p)+'" />' for p in paths)+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(reference)+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=ROOT/'local/dotnet/8.0.425/dotnet.exe';env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='');raise SystemExit(r.returncode)
