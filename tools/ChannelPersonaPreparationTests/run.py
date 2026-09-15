from pathlib import Path
import argparse,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['native_skip_admission','native_accept_failure','courier_drop_session','courier_reject_fallback','scene_generate_partial','scene_skip_scope','scene_accept_replaced','invalid_target_cleanup','waiter_ignore_deadline','waiter_ignore_scope']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
def read(p):return (ROOT/p).read_text(encoding='utf-8-sig')
def old(p):return subprocess.check_output(['git','show','4140bd04:'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
code=read('tools/ChannelPersonaPreparationTests/Harness.cs.txt')
code=code.replace('@@COURIER_SCOPE@@',ex.declaration(read('CourierDeliveryBehavior.HistoryPreparation.cs'),'private bool IsCourierHistoryOwnerCurrent('))
if a.original:
 s=old('ShoutBehavior.cs');c=old('CourierDeliveryBehavior.cs')
 code=code.replace('@@SHOUT_METHODS@@',ex.declaration(s,'private static async Task<bool> EnsureNativeConversationPersonaReadyAsync(')+'\n'+ex.declaration(s,'private async Task EnsurePersonaForCandidatesAsync('))
 code=code.replace('@@COURIER_METHODS@@',ex.declaration(c,'private static async Task EnsureCourierPersonaContextReadyAsync(')+'\nprivate async Task<bool> OldWait(Hero h,bool inbound){await EnsureCourierPersonaContextReadyAsync(h,inbound?"inbound":"reply");return true;}')
 code=code.replace('@@NATIVE_CALL@@','EnsureNativeConversationPersonaReadyAsync(a.Hero,_=>Probe.Read())').replace('@@COURIER_CALL@@','OldWait(hero,inbound)').replace('@@ADMIT_CALL@@','Task.FromResult(false)')
else:
 code=code.replace('@@SHOUT_METHODS@@','').replace('@@COURIER_METHODS@@','').replace('@@NATIVE_CALL@@','EnsureNativeConversationPersonaReadyAsync(a,_=>Probe.Read())').replace('@@COURIER_CALL@@','EnsureCourierPersonaContextReadyAsync(hero,inbound?"inbound":"reply","session",Session,1)').replace('@@ADMIT_CALL@@','Probe.Run(()=>CaptureCourierPreparationAdmission("session",inbound,1)!=null)')
code=code.replace('@@EXTRAS@@','' if a.original else read('tools/ChannelPersonaPreparationTests/Extras.cs.txt'))
code=code.replace('Task.Delay(500)','Task.Delay(1)').replace('const int waitTimeoutMs = 180000','const int waitTimeoutMs = 40')
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True);(out/'Program.cs').write_text(code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
files=[out/'Program.cs',ROOT/'Refactor/Contracts/NpcPersonaReadinessSnapshot.cs']
if not a.original:
 for path in ['MyBehavior.PersonaReadiness.cs','ShoutBehavior.PersonaPreparation.cs','CourierDeliveryBehavior.PreparationAdmission.cs','Refactor/Runtime/PersonaGenerationWaiter.cs']:
  text=read(path).replace('Task.Delay(500)','Task.Delay(1)').replace('const int waitTimeoutMs = 180000','const int waitTimeoutMs = 40')
  if path=='ShoutBehavior.PersonaPreparation.cs':
   if a.mutate=='native_skip_admission':text=text.replace('if (!IsNativeConversationAdmissionCurrent(admission, out _)) return null;','if (false) return null;',1)
   if a.mutate=='native_accept_failure':text=text.replace('if (state.CoolingDown && !state.Active) break;','if (state.CoolingDown && !state.Active) return true;',1)
   if a.mutate=='scene_generate_partial':text=text.replace('string.IsNullOrWhiteSpace(state.Personality) && string.IsNullOrWhiteSpace(state.Background)','string.IsNullOrWhiteSpace(state.Personality) || string.IsNullOrWhiteSpace(state.Background)',1)
   if a.mutate=='scene_skip_scope':
    old=ex.declaration(text,'private bool IsScenePersonaScopeCurrent(');text=text.replace(old,'private bool IsScenePersonaScopeCurrent(ScenePersonaPreparationScope scope) { return scope != null; }',1)
   if a.mutate=='scene_accept_replaced':text=text.replace('!ReferenceEquals(current, prepared.Hero)','false',1)
  if path=='CourierDeliveryBehavior.PreparationAdmission.cs':
   if a.mutate=='courier_drop_session':text=text.replace('!IsCourierHistoryOwnerCurrent(sessionId, session, hero, inbound) || hero.IsDead','hero.IsDead',1)
   if a.mutate=='courier_reject_fallback':text=text.replace('                    return true;','                    return false;',1)
   if a.mutate=='invalid_target_cleanup':text=text.replace('|| !ReferenceEquals(inbound ? ResolveSender(current) : ResolveRecipient(current), participant)','|| false',1)
  if path=='Refactor/Runtime/PersonaGenerationWaiter.cs':
   if a.mutate=='waiter_ignore_deadline':text=text.replace('!hasTime() || !await isCurrent().ConfigureAwait(false)','!await isCurrent().ConfigureAwait(false)',1)
   if a.mutate=='waiter_ignore_scope':text=text.replace('!hasTime() || !await isCurrent().ConfigureAwait(false)','!hasTime()',1)
  dest=out/Path(path).name;dest.write_text(text,encoding='utf-8');files.append(dest)
project=util.project(out,'ChannelPersona',files,executable=True)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
