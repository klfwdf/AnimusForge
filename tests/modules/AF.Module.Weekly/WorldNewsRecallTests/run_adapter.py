from pathlib import Path
import sys,subprocess,json,hashlib,shutil,argparse,importlib.util
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--module-root',type=Path,required=True);args=parser.parse_args()
module=args.module_root.resolve();runtime=module/'bin/Win64_Shipping_Client'
out=new_run_root(ROOT,'world-news-onnx',None);dotnet=resolve_dotnet(ROOT)
sources=[Path(__file__).with_name('AdapterProgram.cs')]
sources += [ROOT/p for p in [
 'src/modules/AF.Module.Weekly/Recall/WorldNewsFactArchive.cs',
 'src/modules/AF.Module.Weekly/Recall/WorldNewsRecallPolicy.cs',
 'src/AF.GameAdapter.Bannerlord/Prompt/WorldNewsRecallPromptAdapter.cs',
 'src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs',
 'src/modules/AF.Module.Prompt/Composition/PromptRetrievalCapture.cs',
 'src/modules/AF.Module.Knowledge/Semantic/OnnxEmbeddingEngine.cs',
 'src/modules/AF.Module.Knowledge/Semantic/OnnxCrossEncoderReranker.cs',
 'src/modules/AF.Module.Knowledge/Semantic/RerankerWire.cs',
 'src/modules/AF.Module.Knowledge/Semantic/RerankerDeviceRuntime.cs',
 'src/modules/AF.Module.Knowledge/Semantic/RerankerCudaProcess.cs',
 'src/AF.Persistence/AnimusForgeModelStore.cs']]
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
shared=ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs'
source=shared.read_text(encoding='utf-8-sig')
signatures=['internal static void CaptureSharedKnowledgeSnapshot(', 'internal static PromptKnowledgeWorkInput CreateSharedKnowledgeWorkInput(',
 'internal static PromptKnowledgeWorkResult RunSharedKnowledgeRetrieval(', 'internal static void ApplySharedKnowledgeRetrieval(']
generated=out/'SharedProduction.cs'
generated.write_text('using System;using System.Linq;using System.Collections.Generic;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters { internal static class SharedPromptCaptureBannerlordAdapter {'+'\n'.join(extract.declaration(source,s) for s in signatures)+'}}',encoding='utf-8')
sources.append(generated)
assert 'extrasSections.WorldNewsRecall = WorldNewsRecallPromptAdapter.Render(phases.Retrieval.NewsResult);' in source
# Channel scheduler source checks complement the executable shared producer/worker/consumer replay.
channel_paths=[
 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs',
 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneSessionComposition.cs',
 'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptSchedule.cs']
for relative in channel_paths:
 path=ROOT/relative
 assert 'CaptureSharedKnowledgeSnapshot' in path.read_text(encoding='utf-8-sig'),relative
refs=[runtime/'Microsoft.ML.OnnxRuntime.dll',next((dotnet.parent/'sdk').glob('*/Newtonsoft.Json.dll'))]
items=''.join('<Compile Include="'+escape(str(p))+'"/>' for p in sources)
items+=''.join('<Reference Include="'+p.stem+'"><HintPath>'+escape(str(p))+'</HintPath></Reference>' for p in refs)
for name in ['onnxruntime.dll','onnxruntime_providers_shared.dll']:
 p=runtime/name
 if p.exists():items+='<Content Include="'+escape(str(p))+'" Link="'+name+'" CopyToOutputDirectory="PreserveNewest"/>'
(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>10</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out/'manifest.json').write_text(json.dumps({str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources+[shared]},indent=2),encoding='utf-8')
run=subprocess.run([str(dotnet),'run','--project',str(out/'Replay.csproj'),'--',str(module),str(ROOT/'content/modules/AF.Module.Weekly/ModuleData')],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'result.log').write_text(run.stdout+run.stderr,encoding='utf-8')
print(run.stdout+run.stderr);print(out);raise SystemExit(run.returncode)
