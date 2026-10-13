from pathlib import Path
import os, subprocess, sys, importlib.util, argparse
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests')); from output_isolation import minimal_test_environment, resolve_dotnet
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);parser.add_argument('--prepare-only',action='store_true');parser.add_argument('--language',choices=['CN','EN'],default='CN');args=parser.parse_args()
out=(args.run_root or ROOT/'artifacts/af2-host-terminal-closeout/line-b/memory-recall-tests').resolve();out.mkdir(parents=True,exist_ok=True)
s=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
text=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs').read_text(encoding='utf-8-sig')
start=text.index('internal static bool ShouldCompleteInitialInteractionMemoryNotoriety(')
guard=text[start:text.index(';',start)+1]
(out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@INITIAL_GUARD@@',guard),encoding='utf-8')
paths=['src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs','src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs','src/modules/AF.Module.Memory/Records/MemoryRecallCandidate.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/modules/AF.Module.Prompt/Composition/PreprocessFormatException.cs','src/modules/AF.Module.Prompt/Retrieval/IntentQueryOptimizer.cs']
paths += ['src/modules/AF.Module.Memory/Recovery/InteractionMemoryRecoveryLedger.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryAuxiliaryCompletionCoordinator.cs']
paths += ['src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs', 'src/modules/AF.Module.Memory/Summary/MemoryFailureUiText.cs', 'src/modules/AF.Module.Memory/Runtime/MemoryTextResources.cs', 'src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalTexts.cs']
resources = [('content/modules/AF.Module.Memory/ModuleData/MemorySourceTexts.json', 'AnimusForge.Memory.SourceTexts.json'), ('content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalCompatibility.json', 'AnimusForge.Prompt.RetrievalCompatibility.json')]
for module, name in [('Memory','MemoryPromptTexts'), ('Prompt','PromptRetrievalTexts')]:
 (out/(name+'.json')).write_bytes((ROOT/f'content/modules/AF.Module.{module}/ModuleData/{name}{".en" if args.language=="EN" else ""}.json').read_bytes())
for suffix, file in [('CN','CNs/afmemory_ui_strings-zh-CN.xml'),('EN','afmemory_ui_strings.xml')]:
 (out/('UI-'+suffix+'.xml')).write_bytes((ROOT/'content/modules/AF.Module.Memory/ModuleData/Languages'/file).read_bytes())
# Exercise the actual shared capture and scene task/accept boundary (game/dispatcher leaves controlled).
shared=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
names=['internal sealed class HistoryWorkCapturePorts','internal static Func<string> CaptureHistoryContextWorkForHero(', 'internal static Func<string> CaptureHistoryContextWorkById(', 'internal static CompressedMemoryBlock CopyHistoryRecallBlock(']
shared='\n'.join(m.declaration(shared,n) for n in names)
snapshot=m.declaration((ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs').read_text(encoding='utf-8-sig'),'internal sealed class HistoryPromptSnapshot')
scene=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
native=m.declaration(scene,'internal static Func<string> CaptureNativeConversationPersistedHistoryWork(')
scene=scene[scene.index('internal sealed class PersistedHistoryCapturePorts'):scene.index('internal bool PersistNpcSpeechToNamedHeroes')]
(out/'SnapshotChecks.cs').write_text((HERE/'SnapshotHarness.cs.txt').read_text(encoding='utf-8').replace('@@SHARED@@',shared).replace('@@SNAPSHOT@@',snapshot).replace('@@SCENE@@',scene).replace('@@NATIVE@@',native),encoding='utf-8')
# The actual channel consumers must not reread live uncompressed drafts after awaited selection.
for relative, token, count in [
 ('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs','nativeMemorySnapshot?.UncompressedMessages',1),
 ('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs','memorySnapshot?.UncompressedMessages',5),
 ('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs','input.History.Messages',2)]:
 consumer=(ROOT/relative).read_text(encoding='utf-8-sig')
 assert consumer.count(token)==count,(relative, 'snapshot consumer missing')
 assert 'BuildUncompressedMemoryRoleMessagesFor' not in consumer,(relative,'late live draft read')
(out/'TextObjectFixture.cs').write_text((ROOT/'tests/modules/AF.Module.Memory/MemoryFailureUiBoundaryTests/TextObjectFixture.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
dotnet=resolve_dotnet(ROOT)
sdk=max((dotnet.parent/'sdk').glob('*/Newtonsoft.Json.dll'),key=lambda p:tuple(int(x) for x in p.parent.name.split('-')[0].split('.')))
reference=sdk
framework='net'+sdk.parent.name.split('.')[0]+'.0'
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>'+framework+'</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(ROOT/p)+'" />' for p in paths)+''.join('<EmbeddedResource Include="'+str(ROOT/p)+'"><LogicalName>'+name+'</LogicalName></EmbeddedResource>' for p,name in resources)+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(reference)+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
if args.prepare_only:
 print('PASS prepared actual Memory recall sources; compilation/runtime NOT-RUN');raise SystemExit(0)
env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='');raise SystemExit(r.returncode)
