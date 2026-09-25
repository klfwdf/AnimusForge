"""Real Scene input / frozen replay / gate methods, extracted from source; game objects stubbed."""
from pathlib import Path
import importlib.util,hashlib,subprocess,os,re,argparse
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def generate(source_ref=None):
 s=ex.source('ShoutBehavior.cs',source_ref);a=ex.source('extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs',source_ref);u=ex.source('ShoutUtils.cs',source_ref)
 try:o=ex.source('src/modules/AF.Module.Conversation/Channels/Scene/ScenePlayerShoutRequestOwner.cs',source_ref)
 except (FileNotFoundError,subprocess.CalledProcessError):o=''
 gates=['private void RegisterScenePostprocessGateTask(','private Task GetScenePostprocessGateTask(','private void ForceClearScenePostprocessGate(','private async Task WaitForScenePostprocessGateAsync(']
 gm='\n'.join(ex.declaration(s,x) for x in gates)
 fields=[]
 names=set(re.findall(r'\b(_[A-Za-z]\w*)\b',gm))
 current='private sealed class ScenePlayerShoutRequest' in s or 'internal sealed class ScenePlayerShoutRequest' in o
 if o:fields.append('private readonly ScenePlayerShoutRequestOwner _scenePlayerShoutRequestOwner=new();')
 elif current:names.add('_scenePlayerInputSequence')
 for name in sorted(names):
  m=re.search(r'^\s*private (?:readonly |volatile )?[^\n;{}]+\b'+name+r'\b[^\n;{}]*;',s,re.M)
  if not m:raise ValueError(name)
  fields.append(m.group().strip())
 methods='\n'.join(ex.declaration(s,x) for x in ['private void BeginShoutProcessing(','private void EndShoutProcessing(','private void ResumeGame(','private List<Agent> GetAgentsForShoutTargetingContext(','private static Agent ResolvePrimaryAgentForShoutTargetingContext(','private async void OnShoutConfirmedWithContext(','private int BeginNewPlayerDrivenSceneConversationEpoch(','private void ResetSceneShoutRuntimeOnMissionEnd('])
 request='';scene_request_types='public class ShoutTargetingContext{public List<int> CandidateAgentIndices;public List<Agent> PreviewCandidateAgents;public int PrimaryAgentIndex;}'
 if current:
  signatures=['internal object CaptureScenePlayerShoutRequestForReplay(','internal bool IsCapturedScenePlayerShoutRequestCurrent(','private ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(','private bool IsScenePlayerShoutRequestCurrent(','internal bool TryReplayCapturedScenePlayerShout(','private async Task ProcessShoutConfirmedInternal(','private async Task ProcessCapturedScenePlayerShoutAsync(']
 if o:
   scene_request_types='\n'.join(ex.declaration(o,x) for x in ['internal sealed class ShoutTargetingContext','internal sealed class ScenePlayerShoutRequest','internal sealed class ScenePlayerShoutContext','internal sealed class ScenePlayerShoutRequestOwner'])
   request='\n'.join(ex.declaration(s,x) for x in signatures)
 else:request='\n'.join(ex.declaration(s,x) for x in ['private sealed class ScenePlayerShoutRequest']+signatures)
 module_context_source=ex.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneContext.cs',source_ref)
 module_context='\n'.join(ex.declaration(module_context_source,x) for x in ['internal static string IssueModuleSceneTicket(', 'internal static bool TryClaimModuleSceneTicket(', 'internal static void RevokeModuleSceneTickets(', 'internal ScenePlayerShoutContext CaptureModuleSceneContext(', 'internal bool TryClaimModuleSceneContext(', 'private bool IsModuleSceneTargetingSourceCurrent(', 'private static ShoutTargetingContext CloneModuleSceneTargetingContext('])
 module_submission_source=ex.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs',source_ref)
 module_scene_thread='\n'.join(ex.declaration(module_submission_source,x) for x in ['private sealed class SceneMainThreadSynchronizationContext','private Task RunSceneGroupOnMainThreadAsync('])
 awaited_group=current and 'private Task ProcessCurrentScenePlayerShout(' in s
 sig=('private Task ProcessCurrentScenePlayerShout(' if awaited_group else 'private void ProcessCurrentScenePlayerShout(') if current else 'private async Task ProcessShoutConfirmedInternal('
 full=ex.declaration(s,sig);marker='\t\tif (!TryBuildSceneShoutConversationScope('
 assert full.count(marker)==1
 assert ('return receipt == null ? Task.Run(RunGroupAsync) : RunSceneGroupOnMainThreadAsync(RunGroupAsync);' if awaited_group else '_ = Task.Run(async delegate') in full, 'Scene group kickoff seam changed; review completion fixture'
 kickoff=('return Program.PendingGroup==null?Task.CompletedTask:Task.Run(async ()=>await Program.PendingGroup.Task);' if awaited_group else 'if(Program.PendingGroup!=null)_ = Task.Run(async ()=>await Program.PendingGroup.Task);')
 prefix=full.split(marker)[0]+'\nCheckMain(); Accepted++;if(AfCompatV130.Suppressed)SuppressedAccepted++;LastPrimary=primaryTarget;LastFramed=framedAgents;LastText=shoutText;AfCompatV130.Record(this,shoutText,primaryTarget.Index,framedAgents);'+kickoff+'\n}\n'
 replay=ex.declaration(a,'internal static bool TryReplayOriginalPlayerShout(');resume=ex.declaration(a,'private static void ResumeAfShoutUi(')
 observer=ex.declaration(a,'private static bool ObserveAcceptedPlayerShout(');record=ex.declaration(a,'private static void ObserveRecordedPlayerMessage(')
 utils='\n'.join(ex.declaration(u,x) for x in ['public static List<Agent> GetNearbyNPCAgents()','private static List<Agent> GetNearbyNPCAgentsLegacy('])
 pre=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
 for k,v in [('FIELDS','\n'.join(fields)),('GATES',gm),('METHODS',methods),('REQUEST',request),('MODULE_SCENE_CONTEXT',module_context),('MODULE_SCENE_THREAD',module_scene_thread),('SCENE_REQUEST_TYPES',scene_request_types),('PREFIX',prefix),('REPLAY',replay),('RESUME',resume),('OBSERVER',observer),('RECORD',record),('UTILS',utils)]:pre=pre.replace('@@'+k+'@@',v)
 assert '@@' not in pre
 return pre

MUTATIONS = {
 'request-guard': ('if (!IsScenePlayerShoutRequestCurrent(request))', 'if (false)', 'stale-session-after-await'),
 'mutable-target': ('ShoutTargetingContext targetingContext = request.TargetingContext;', 'ShoutTargetingContext targetingContext = _activeShoutTargetingContext;', 'replay-frozen-range'),
 'borrowed-flag': ('&& _scenePostprocessWaitBorrowedProcessingFlag', '&& false', 'same-gate-waiters'),
 'busy-lifetime': ('restoreProcessingFlag && processingSequence == Interlocked.Read(ref _sceneShoutProcessingSequence)', 'restoreProcessingFlag', 'resume-ui-no-stale-busy'),
 'observer-scope': ('if (!observeForBattleSpeech)', 'if (false)', 'deferred-ordinary-observation'),
 'replay-consumption': ('return request != null && Interlocked.CompareExchange(ref request.Started, 1, 0) == 0;', 'return request != null;', 'replay-pending-one-shot'),
 'context-capture-advances': ('InputSequence = Interlocked.Read(ref _inputSequence),', 'InputSequence = Interlocked.Increment(ref _inputSequence),', 'scene-context-claim-without-capture-side-effect'),
 'context-source-bypass': ('|| !_isProcessingShout || !IsModuleSceneTargetingSourceCurrent(context))', '|| !_isProcessingShout || false)', 'scene-host-context-source-and-thread'),
 'old-host-fallthrough': ('if (capturedRequest != null && BattleSpeechRuntimeHost.TryPreRouteNaturalPlayerShout(', 'if (capturedRequest == null) return true; if (BattleSpeechRuntimeHost.TryPreRouteNaturalPlayerShout(', 'old-host-observer-fallthrough'),
}

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--source-ref');ap.add_argument('--mutation',choices=sorted(MUTATIONS));ap.add_argument('--core',action='store_true');ap.add_argument('--j14-completion','--j14-red',dest='j14_completion',action='store_true');ap.add_argument('--output-name',default='current');ap.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe');args=ap.parse_args()
 if not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name):ap.error('Invalid output name')
 out=HERE/'.generated'/args.output_name;out.mkdir(parents=True,exist_ok=True);pre=generate(args.source_ref)
 if args.mutation:
  old,new,_=MUTATIONS[args.mutation]
  if old not in pre:raise ValueError('Mutation anchor absent: '+args.mutation)
  pre=pre.replace(old,new)
 (out/'Program.cs').write_text(pre,encoding='utf-8');(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=os.environ.copy();env['DOTNET_ROOT']=str(Path(args.dotnet).parent);env['DOTNET_CLI_HOME']=str(out/'cli');env['DOTNET_CLI_TELEMETRY_OPTOUT']='1';env['DOTNET_NOLOGO']='1';env['DOTNET_CLI_UI_LANGUAGE']='en'
 r=subprocess.run([args.dotnet,'run','--project',str(out/'Tests.csproj'),'-c','Release','--']+(['core'] if args.core else [])+(['j14-completion'] if args.j14_completion else []),cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
 log='source='+(args.source_ref or 'working-tree')+' mutation='+(args.mutation or 'none')+'\nHarness SHA256='+hashlib.sha256(pre.encode()).hexdigest()+'\n'+r.stdout+r.stderr
 (out/'run.log').write_text(log,encoding='utf-8');print(log);return r.returncode
if __name__=='__main__':raise SystemExit(main())
