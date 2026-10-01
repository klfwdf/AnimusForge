from pathlib import Path
import importlib.util,sys,subprocess,argparse
R=Path(__file__).resolve().parents[4]; H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'));from output_isolation import minimal_test_environment
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=(args.run_root or R/'artifacts/af2-host-terminal-closeout/line-b/daily-developer-edit-tests').resolve();out.mkdir(parents=True,exist_ok=True)
state=(R/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs').read_text(encoding='utf-8-sig')
methods='\n'.join(ex.declaration(state,x) for x in ['internal List<DailyMemoryDraft> LoadDrafts(', 'internal void SaveDrafts('])
day=ex.declaration((R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig'),'internal class DialogueDay')
adapter=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryDeveloperDailyEdit.cs').read_text(encoding='utf-8-sig')
adapters='\n'.join(ex.declaration(adapter,x) for x in ['internal bool TryApplyDevDailyMemoryLineDataMutation(', 'internal bool TryApplyDevDailyMemoryDraftDataMutation(', 'internal bool ApplyImportedDialogueHistory(', 'internal bool DeleteDevDailyMemoryDraftData(', 'internal bool ClearDevCompressedMemoryData(', 'internal bool ClearDevDialogueHistoryData(', 'internal bool TryApplyDevDialogueHistoryLineDataMutation('])
(out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@STATE_METHODS@@',methods).replace('@@DAY@@',day).replace('@@ADAPTERS@@',adapters),encoding='utf-8')
paths=['src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.cs','src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.Daily.cs','src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs']
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/p)+'" />' for p in paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=R/'local/dotnet/8.0.425/dotnet.exe'
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='');raise SystemExit(r.returncode)
