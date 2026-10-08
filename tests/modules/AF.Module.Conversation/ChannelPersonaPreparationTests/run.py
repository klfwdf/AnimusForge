from pathlib import Path
import os
import argparse,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--dotnet', default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")));p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['native_skip_admission','native_accept_failure','courier_drop_session','courier_reject_fallback','scene_generate_partial','scene_skip_scope','scene_accept_replaced','invalid_target_cleanup','waiter_ignore_deadline','waiter_ignore_scope']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
def read(p):return (current_source_path(ROOT, p)).read_text(encoding='utf-8-sig')
def old(p):return subprocess.check_output(['git','show','4140bd04:'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
code=read('tests/modules/AF.Module.Conversation/ChannelPersonaPreparationTests/Harness.cs.txt')
code=code.replace('@@COURIER_SCOPE@@',ex.declaration(read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs'),'private bool IsCourierHistoryOwnerCurrent('))
if a.original:
 s=old('ShoutBehavior.cs');c=old('CourierDeliveryBehavior.cs')
 code=code.replace('@@SHOUT_METHODS@@',ex.declaration(s,'private static async Task<bool> EnsureNativeConversationPersonaReadyAsync(')+'\n'+ex.declaration(s,'private async Task EnsurePersonaForCandidatesAsync('))
 code=code.replace('@@COURIER_METHODS@@',ex.declaration(c,'private static async Task EnsureCourierPersonaContextReadyAsync(')+'\nprivate async Task<bool> OldWait(Hero h,bool inbound){await EnsureCourierPersonaContextReadyAsync(h,inbound?"inbound":"reply");return true;}')
 code=code.replace('@@NATIVE_CALL@@','EnsureNativeConversationPersonaReadyAsync(a.Hero,_=>Probe.Read())').replace('@@COURIER_CALL@@','OldWait(hero,inbound)').replace('@@ADMIT_CALL@@','Task.FromResult(false)')
else:
 code=code.replace('@@SHOUT_METHODS@@','').replace('@@COURIER_METHODS@@','').replace('@@NATIVE_CALL@@','EnsureNativeConversationPersonaReadyAsync(a,_=>Probe.Read())').replace('@@COURIER_CALL@@','EnsureCourierPersonaContextReadyAsync(hero,inbound?"inbound":"reply","session",Session,1)').replace('@@ADMIT_CALL@@','Probe.Run(()=>CaptureCourierPreparationAdmission("session",inbound,1)!=null)')
# Actual capture/readiness algorithms are source-extracted unchanged; generation,
# admission predicate, dispatcher, and persona fallback remain the original controlled seams.
if a.original:
 code=code.replace('@@CURRENT_ADAPTERS@@','')
 for seam in ['  private readonly AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter NpcPersonaGenerationApplication=new();',
  '  private readonly AnimusForge.Refactor.Runtime.ConversationGameThreadDispatcher _conversationGameThreadDispatcher=new();',
  '  private AnimusForge.Refactor.Adapters.NativeAdmissionApplicationAdapter NativeAdmissions=>new(this);']:
  assert code.count(seam)==1;code=code.replace(seam,'',1)
 code=code.replace('AnimusForge.Refactor.Runtime.SceneConversationHistoryOwner.SessionId=1;','').replace('AnimusForge.Refactor.Runtime.SceneConversationHistoryOwner.SessionId++;','')
else:
 owner=read('src/AF.GameAdapter.Bannerlord/Prompt/ScenePersonaPreparationAdapter.cs')
 signatures=['internal ScenePersonaPreparationAdapter(', 'internal bool IsScenePersonaScopeCurrent(',
  'internal async Task EnsurePersonaForCandidatesAsync(', 'internal async Task<bool> EnsureNativeConversationPersonaReadyAsync(',
  'internal static NpcPersonaReadinessSnapshot CaptureNpcPersonaReadiness(']
 declarations=[ex.declaration(owner,signature) for signature in signatures]
 mutations={
  'native_skip_admission':('if (!_nativeAdmission.IsNativeConversationAdmissionCurrent(admission, out _)) return null;','if (false) return null;'),
  'native_accept_failure':('if (state.CoolingDown && !state.Active) break;','if (state.CoolingDown && !state.Active) return true;'),
  'scene_generate_partial':('string.IsNullOrWhiteSpace(state.Personality) && string.IsNullOrWhiteSpace(state.Background)','string.IsNullOrWhiteSpace(state.Personality) || string.IsNullOrWhiteSpace(state.Background)'),
  'scene_skip_scope':(ex.declaration(owner,'internal bool IsScenePersonaScopeCurrent('),'internal bool IsScenePersonaScopeCurrent(ScenePersonaPreparationScope scope) { return scope != null; }'),
  'scene_accept_replaced':('!ReferenceEquals(current, prepared.Hero)','false')}
 body='\n'.join(declarations)
 if a.mutate in mutations:
  before,after=mutations[a.mutate];assert body.count(before)==1,'Mutation anchor drift: '+a.mutate
  body=body.replace(before,after,1)
 body=body.replace('Task.Delay(500)','Task.Delay(1)')
 adapters="""
namespace AnimusForge.Refactor.Runtime {
 internal static class SceneConversationHistoryOwner {internal static int SessionId=1;}
 internal sealed class ConversationGameThreadDispatcher {
  internal Task<T> RunAsync<T>(string name,string target,int index,Func<T> f,T fallback)=>Probe.Run(()=>{Probe.BeforeOperation?.Invoke(name);return f();});
 }
}
namespace AnimusForge.Refactor.Adapters {
 using AnimusForge.Refactor.Runtime;
 using NpcDataPacket=AnimusForge.ShoutBehavior.NpcDataPacket;
 using ScenePersonaPreparationScope=AnimusForge.ShoutBehavior.ScenePersonaPreparationScope;
 using ScenePersonaCandidate=AnimusForge.ShoutBehavior.ScenePersonaCandidate;
 using NativeConversationAdmission=AnimusForge.ShoutBehavior.NativeConversationAdmission;
 internal sealed class NativeAdmissionApplicationAdapter {
  readonly AnimusForge.ShoutBehavior owner;internal NativeAdmissionApplicationAdapter(AnimusForge.ShoutBehavior owner){this.owner=owner;}
  internal bool IsNativeConversationAdmissionCurrent(NativeConversationAdmission a,out string reason)=>owner.IsNativeConversationAdmissionCurrent(a,out reason);
 }
 internal sealed class NpcPersonaGenerationApplicationAdapter {
  internal void GetNpcPersonaStrings(Hero h,out string p,out string b){Probe.Read();p=h.P;b=h.B;}
  internal void GetNpcPersonaGenerationRuntimeState(Hero h,out bool a,out bool c){Probe.Read();a=h.Active;c=h.Cooling;}
 }
 internal sealed class ScenePersonaPreparationAdapter {
  readonly ConversationGameThreadDispatcher _dispatcher;readonly NativeAdmissionApplicationAdapter _nativeAdmission;
  readonly Func<bool> _isCurrentOwner;readonly Func<int> _sceneEpoch;readonly int _nativeWaitTimeoutMs;
  // Existing controlled factual-fallback leaf, not a replacement for readiness policy.
  static void BuildHeroPersonaFallback(Hero h,out string p,out string b){Probe.Read();p="fallbackP";b="fallbackB";}
  @BODY@
 }
}
""".replace('@BODY@',body)
 code=code.replace('@@CURRENT_ADAPTERS@@',adapters)
 import hashlib,json
 capture_inputs=[{'path':'src/AF.GameAdapter.Bannerlord/Prompt/ScenePersonaPreparationAdapter.cs','rawSha256':hashlib.sha256((ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/ScenePersonaPreparationAdapter.cs').read_bytes()).hexdigest(),'signatures':signatures,'declarationSha256':[hashlib.sha256(v.encode()).hexdigest() for v in declarations]}]

code=code.replace('@@EXTRAS@@','' if a.original else read('tests/modules/AF.Module.Conversation/ChannelPersonaPreparationTests/Extras.cs.txt'))
code=code.replace('Task.Delay(500)','Task.Delay(1)').replace('const int waitTimeoutMs = 180000','const int waitTimeoutMs = 40')
out=util.new_run_root(ROOT,'ChannelPersonaPreparationTests',a.run_root);(out/'Program.cs').write_text(code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
files=[out/'Program.cs',ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaReadinessSnapshot.cs',ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaProfilePolicy.cs']
if not a.original:
 for path in ['MyBehavior.PersonaReadiness.cs','ShoutBehavior.PersonaPreparation.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PreparationAdmission.cs','src/modules/AF.Module.Conversation/Internal/PersonaGenerationWaiter.cs']:
  text=read(path).replace('Task.Delay(500)','Task.Delay(1)').replace('const int waitTimeoutMs = 180000','const int waitTimeoutMs = 40')
  if path=='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PreparationAdmission.cs':
   if a.mutate=='courier_drop_session':text=text.replace('!IsCourierHistoryOwnerCurrent(sessionId, session, hero, inbound) || hero.IsDead','hero.IsDead',1)
   if a.mutate=='courier_reject_fallback':text=text.replace('                    return true;','                    return false;',1)
   if a.mutate=='invalid_target_cleanup':text=text.replace('|| !ReferenceEquals(inbound ? ResolveSender(current) : ResolveRecipient(current), participant)','|| false',1)
  if path=='src/modules/AF.Module.Conversation/Internal/PersonaGenerationWaiter.cs':
   if a.mutate=='waiter_ignore_deadline':text=text.replace('!hasTime() || !await isCurrent().ConfigureAwait(false)','!await isCurrent().ConfigureAwait(false)',1)
   if a.mutate=='waiter_ignore_scope':text=text.replace('!hasTime() || !await isCurrent().ConfigureAwait(false)','!hasTime()',1)
  dest=out/Path(path).name;dest.write_text(text,encoding='utf-8');files.append(dest)
if not a.original: (out/'current-capture-inputs.json').write_text(json.dumps(capture_inputs,indent=2),encoding='utf-8')
project=util.project(out,'ChannelPersona',files,executable=True)
code,log=util.run_dotnet(a.dotnet,['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
