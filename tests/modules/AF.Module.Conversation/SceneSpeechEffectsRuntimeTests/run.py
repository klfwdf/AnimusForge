"""Complete production Publish and completion controllers. Game/actions/motion/presentation ports are substitutes."""
import argparse,importlib.util,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['duplicate','mission','generation','epoch','completion-order','hall-branch']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
src=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene'
effect=(src/'SceneSpeechEffectController.cs').read_text(encoding='utf-8-sig');effect=effect[effect.index('internal sealed class SceneSpeechEffectController'):]
item=ex.declaration((src/'ShoutBehavior.SpeechExecution.cs').read_text(encoding='utf-8-sig'),'internal sealed class SceneSpeechQueueItem')
completion=(src/'SceneSpeechCompletionController.cs').read_text(encoding='utf-8-sig')
mutations={

'mission': ('!ReferenceEquals(Mission.Current, item.SourceMission)', 'false'),
'generation': ('!SaveRuntimeGuard.IsCurrentGeneration(item.RuntimeGeneration)', 'false'),
'epoch': ('!_ports.IsSceneConversationEpochCurrent(item.RequiredConversationEpoch)', 'false'),
'completion-order': ('_movement.FlushSceneSummonReturnAfterSpeech(agentIndex);', '_movement.FlushSceneGuideReturnAfterSpeech(agentIndex);'),
'hall-branch': ('if (_ports.TryFlushLordsHallEntry(agentIndex))', 'if (_ports.TryFlushLordsHallEntry(agentIndex) && false)')}
if a.mutation and a.mutation!='duplicate':
    old,new=mutations[a.mutation]
    if a.mutation.startswith('completion') or a.mutation=='hall-branch':
        assert completion.count(old)==1;completion=completion.replace(old,new,1)
    else:
        assert effect.count(old)==1;effect=effect.replace(old,new,1)
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@EFFECT@@',effect).replace('@@ITEM@@',item)
out=new_run_root(ROOT,'scene-speech-effects',a.run_root)
(out/'Program.cs').write_text(code,encoding='utf-8')
for name in ['SceneSpeechQueueOwner.cs','SceneSpeechExecutionRuntime.cs']:
    runtime=(src/name).read_text(encoding='utf-8-sig')
    if a.mutation=='duplicate' and name=='SceneSpeechExecutionRuntime.cs':
        old='if (Interlocked.Exchange(ref claimed, 1) != 0) return;';assert runtime.count(old)==1;runtime=runtime.replace(old,'if (false) return;',1)
        old='if (!_queue.TryClaimDispatch(generation, retire))';assert runtime.count(old)==1;runtime=runtime.replace(old,'if (false)',1)
    (out/name).write_text(runtime,encoding='utf-8')
(out/'Ports.cs').write_text((src/'SceneSpeechEffectPorts.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Completion.cs').write_text(completion,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0169;CS0414</NoWarn></PropertyGroup></Project>')
# Compile the actual current AF edge, not a renamed old owner stub.
bridge_path=ROOT/'src/bridges/Diplomacy/DiplomacyConversationBridge.cs'
bridge_source=bridge_path.read_text(encoding='utf-8-sig')
bridge_matches=__import__('re').findall(r'(?m)^\s*internal static void ProcessDiplomacyTagsDispatch\([^\r\n]+\)\s*=>[^\r\n]+;',bridge_source)
assert len(bridge_matches)==1, 'actual diplomacy dispatch edge must be unique'
bridge=bridge_matches[0].strip()
assert 'DiplomacyModuleServices.Conversation.ProcessDiplomacyTags(hero?.StringId, ref text)' in bridge
(out/'DiplomacyBridge.cs').write_text('using TaleWorlds.CampaignSystem;\nnamespace AnimusForge { internal static class DiplomacyConversationBridge { '+bridge+' } }',encoding='utf-8')
(out/'current-bridge-source.json').write_text(__import__('json').dumps({'path':bridge_path.relative_to(ROOT).as_posix(),'rawSha256':__import__('hashlib').sha256(bridge_path.read_bytes()).hexdigest(),'methodSha256':__import__('hashlib').sha256(bridge.encode()).hexdigest(),'scope':'actual AF edge; diplomacy module leaf synthetic, no diplomacy game effects claim'},indent=2),encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
