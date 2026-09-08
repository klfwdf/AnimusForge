"""Real Scene input / frozen replay / gate methods, extracted from source; game objects stubbed."""
from pathlib import Path
import importlib.util,hashlib,subprocess,os,re,argparse
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def generate(source_ref=None):
 s=ex.source('ShoutBehavior.cs',source_ref);a=ex.source('extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs',source_ref);u=ex.source('ShoutUtils.cs',source_ref)
 gates=['private void RegisterScenePostprocessGateTask(','private Task GetScenePostprocessGateTask(','private void ForceClearScenePostprocessGate(','private async Task WaitForScenePostprocessGateAsync(']
 gm='\n'.join(ex.declaration(s,x) for x in gates)
 fields=[]
 names=set(re.findall(r'\b(_[A-Za-z]\w*)\b',gm))
 current='private sealed class ScenePlayerShoutRequest' in s
 if current:names.add('_scenePlayerInputSequence')
 for name in sorted(names):
  m=re.search(r'^\s*private (?:readonly |volatile )?[^\n;{}]+\b'+name+r'\b[^\n;{}]*;',s,re.M)
  if not m:raise ValueError(name)
  fields.append(m.group().strip())
 methods='\n'.join(ex.declaration(s,x) for x in ['private void BeginShoutProcessing(','private void EndShoutProcessing(','private void ResumeGame(','private List<Agent> GetAgentsForShoutTargetingContext(','private static Agent ResolvePrimaryAgentForShoutTargetingContext(','private async void OnShoutConfirmedWithContext(','private int BeginNewPlayerDrivenSceneConversationEpoch(','private void ResetSceneShoutRuntimeOnMissionEnd('])
 request=''
 if current:
  request='\n'.join(ex.declaration(s,x) for x in ['private sealed class ScenePlayerShoutRequest','internal object CaptureScenePlayerShoutRequestForReplay(','private ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(','private bool IsScenePlayerShoutRequestCurrent(','internal bool TryReplayCapturedScenePlayerShout(','private async Task ProcessShoutConfirmedInternal(','private async Task ProcessCapturedScenePlayerShoutAsync('])
 sig='private void ProcessCurrentScenePlayerShout(' if current else 'private async Task ProcessShoutConfirmedInternal('
 full=ex.declaration(s,sig);marker='\t\tif (!TryBuildSceneShoutConversationScope('
 assert full.count(marker)==1
 prefix=full.split(marker)[0]+'\nCheckMain(); Accepted++;if(AfCompatV130.Suppressed)SuppressedAccepted++;LastPrimary=primaryTarget;LastFramed=framedAgents;LastText=shoutText;AfCompatV130.Record(this,shoutText,primaryTarget.Index,framedAgents);\n}\n'
 replay=ex.declaration(a,'internal static bool TryReplayOriginalPlayerShout(');resume=ex.declaration(a,'private static void ResumeAfShoutUi(')
 observer=ex.declaration(a,'private static bool ObserveAcceptedPlayerShout(');record=ex.declaration(a,'private static void ObserveRecordedPlayerMessage(')
 utils='\n'.join(ex.declaration(u,x) for x in ['public static List<Agent> GetNearbyNPCAgents()','private static List<Agent> GetNearbyNPCAgentsLegacy('])
 pre=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
 for k,v in [('FIELDS','\n'.join(fields)),('GATES',gm),('METHODS',methods),('REQUEST',request),('PREFIX',prefix),('REPLAY',replay),('RESUME',resume),('OBSERVER',observer),('RECORD',record),('UTILS',utils)]:pre=pre.replace('@@'+k+'@@',v)
 assert '@@' not in pre
 return pre

MUTATIONS = {
 'request-guard': ('if (!IsScenePlayerShoutRequestCurrent(request))', 'if (false)', 'stale-session-after-await'),
 'mutable-target': ('ShoutTargetingContext targetingContext = request.TargetingContext;', 'ShoutTargetingContext targetingContext = _activeShoutTargetingContext;', 'replay-frozen-range'),
 'borrowed-flag': ('&& _scenePostprocessWaitBorrowedProcessingFlag', '&& false', 'same-gate-waiters'),
 'busy-lifetime': ('restoreProcessingFlag && processingSequence == Interlocked.Read(ref _sceneShoutProcessingSequence)', 'restoreProcessingFlag', 'resume-ui-no-stale-busy'),
 'observer-scope': ('if (!observeForBattleSpeech)', 'if (false)', 'deferred-ordinary-observation'),
 'replay-consumption': ('Interlocked.CompareExchange(ref request.Started, 1, 0) != 0', 'false', 'replay-pending-one-shot'),
 'old-host-fallthrough': ('if (capturedRequest != null && BattleSpeechRuntimeHost.TryPreRouteNaturalPlayerShout(', 'if (capturedRequest == null) return true; if (BattleSpeechRuntimeHost.TryPreRouteNaturalPlayerShout(', 'old-host-observer-fallthrough'),
}

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--source-ref');ap.add_argument('--mutation',choices=sorted(MUTATIONS));ap.add_argument('--core',action='store_true');ap.add_argument('--output-name',default='current');ap.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe');args=ap.parse_args()
 if not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name):ap.error('Invalid output name')
 out=HERE/'.generated'/args.output_name;out.mkdir(parents=True,exist_ok=True);pre=generate(args.source_ref)
 if args.mutation:
  old,new,_=MUTATIONS[args.mutation]
  if old not in pre:raise ValueError('Mutation anchor absent: '+args.mutation)
  pre=pre.replace(old,new)
 (out/'Program.cs').write_text(pre,encoding='utf-8');(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=os.environ.copy();env['DOTNET_ROOT']=str(Path(args.dotnet).parent);env['DOTNET_CLI_HOME']=str(out/'cli');env['DOTNET_CLI_TELEMETRY_OPTOUT']='1';env['DOTNET_NOLOGO']='1';env['DOTNET_CLI_UI_LANGUAGE']='en'
 r=subprocess.run([args.dotnet,'run','--project',str(out/'Tests.csproj'),'-c','Release','--']+(['core'] if args.core else []),cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
 log='source='+(args.source_ref or 'working-tree')+' mutation='+(args.mutation or 'none')+'\nHarness SHA256='+hashlib.sha256(pre.encode()).hexdigest()+'\n'+r.stdout+r.stderr
 (out/'run.log').write_text(log,encoding='utf-8');print(log);return r.returncode
if __name__=='__main__':raise SystemExit(main())
