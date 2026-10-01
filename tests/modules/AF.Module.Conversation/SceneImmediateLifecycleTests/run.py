"""Compile actual runtime state and immediate lifecycle methods, not a replacement model."""
import argparse,importlib.util,subprocess,sys,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['completion','epoch','request-claim','reset-fallback','late-target','network-main','gate-borrowed','gate-busy','gate-old-completion','module-agent-reference','module-distance','module-capture-sequence']);a=p.parse_args()
out=new_run_root(ROOT,'scene-immediate-lifecycle',a.run_root)
base=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene'
runtime=(base/'SceneConversationSessionRuntime.cs').read_text(encoding='utf-8-sig')
chains=(base/'ShoutBehavior.SceneConversationChains.cs').read_text(encoding='utf-8-sig')
members=[ex.declaration(chains,'internal sealed class ImmediateSceneReactionRequest')]
for name in ['bool FinishImmediateSceneReactionGeneration','bool RegisterImmediateSceneReactionRequest','bool TryTakeImmediateSceneReactionRequest','void InvokeImmediateSceneReactionNoSpeechFallback','bool IsImmediateSceneReactionRuntimeCurrent','bool CanPublishImmediateSceneReactionRequest']:
    members.append(ex.declaration(chains,'internal '+name+'('))
for name in ['private async Task<bool> CompleteImmediateSceneReactionCoreAsync(', 'private async Task<bool> TryPublishImmediateSceneReactionAsync(']:
    members.append(ex.declaration(chains,name))
module=(base/'ShoutBehavior.ModuleSceneSubmission.cs').read_text(encoding='utf-8-sig')
members.append(ex.declaration(module,'private sealed class SceneMainThreadSynchronizationContext'))
unified=(ROOT/'src/modules/AF.Module.Conversation/Internal/Postprocess/ShoutBehavior.UnifiedActionPostprocess.cs').read_text(encoding='utf-8-sig')
header=ex.declaration(unified,'internal static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess(').split('{',1)[0]
members.append(header+'{ return new SceneActionPostprocessWorkItem("system", "detached prompt", replyText); }')
gate=(base/'ShoutBehavior.ScenePostprocessGate.cs').read_text(encoding='utf-8-sig')
members += [m.group(0) for m in re.finditer(r'^\s*private (?:readonly )?[^\n;{}]+_[a-zA-Z]\w*[^\n;{}]*;',gate,re.M)]
for sig in ['internal void RegisterScenePostprocessGateTask(','internal Task GetScenePostprocessGateTask(','internal void ForceClearScenePostprocessGate(','internal async Task WaitForScenePostprocessGateAsync(']:
    members.append(ex.declaration(gate,sig))
source=runtime+'\nnamespace AnimusForge { internal sealed partial class SceneConversationSessionRuntime {\n'+'\n'.join(members)+'\n}}'
# File-scoped namespace applies to the entire generated file; combine as another partial without another namespace.
source='using System.Linq;using System.Threading.Tasks;using System.Text.RegularExpressions;using static AnimusForge.ShoutBehavior;\n'+runtime+'\ninternal sealed partial class SceneConversationSessionRuntime {\n'+'\n'.join(members)+'\n}'
mutations={
'completion':('if (request == null || Interlocked.CompareExchange(ref request.CompletionClaimed, 1, 0) != 0) return;','if (request == null) return;'),
'epoch':('request.ConversationEpoch == ConversationEpoch &&','true &&'),
'request-claim':('return Interlocked.CompareExchange(ref request.CompletionStarted, 1, 0) == 0;','return true;'),
'reset-fallback':('foreach (ImmediateSceneReactionRequest request in pending)\n            SettleImmediateSceneReaction(request, false, allowNoSpeech: false);','foreach (ImmediateSceneReactionRequest request in pending)\n            SettleImmediateSceneReaction(request, false, allowNoSpeech: true);'),
'late-target':('|| !CanAgentParticipateInSceneSpeech(npcAgent)\n                    || !IsNativeConversationResponseTargetAvailableForActionDispatch(request.TargetAgentIndex, contextHero, npcCharacter, out _)','|| false'),
'network-main':('var network = await Task.Run(() =>','var network = await RunInlineAsync(() =>'),
'gate-borrowed':('&& _scenePostprocessWaitBorrowedProcessingFlag','&& false'),
'gate-busy':('restoreProcessingFlag && processingSequence == _ports.ProcessingSequence()','restoreProcessingFlag'),
'gate-old-completion':('if (!ReferenceEquals(_scenePostprocessIdleTcs, registeredGate))','if (false)')}
if a.mutation in mutations:
    old,new=mutations[a.mutation];count=2 if a.mutation=='gate-busy' else 1;assert source.count(old)==count;source=source.replace(old,new,count)
(out/'Runtime.cs').write_text(source,encoding='utf-8')
(out/'Program.cs').write_bytes((HERE/'Harness.cs.txt').read_bytes())
(out/'Registry.cs').write_bytes((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_bytes())
owner=(base/'ScenePlayerShoutRequestOwner.cs').read_text(encoding='utf-8-sig')
context=(base/'ShoutBehavior.ModuleSceneContext.cs').read_text(encoding='utf-8-sig')
if a.mutation=='module-capture-sequence':
    old='InputSequence = Interlocked.Read(ref _inputSequence),';assert owner.count(old)==1;owner=owner.replace(old,'InputSequence = Interlocked.Increment(ref _inputSequence),',1)
if a.mutation in ['module-agent-reference','module-distance']:
    old='|| !ReferenceEquals(currentFramed[i], expected)' if a.mutation=='module-agent-reference' else '|| current != distance.Value';assert context.count(old)==1;context=context.replace(old,'|| false',1)
(out/'RequestOwner.cs').write_text(owner,encoding='utf-8')
(out/'ModuleContext.cs').write_text(context,encoding='utf-8')
for name in ['ConversationRequestLifetime.cs','InteractionRequestLease.cs']:
    (out/name).write_bytes((ROOT/'src/modules/AF.Module.Conversation/Internal'/name).read_bytes())
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
