"""Exact channel-persona/admission delta before legacy whole-owner proofs."""
from pathlib import Path
import hashlib,importlib.util,json,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent;BASELINE='4140bd04'
spec=importlib.util.spec_from_file_location('channel_persona_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
def old(path):return subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def expected(path):
 s=old(path)
 if path=='ShoutBehavior.cs':
  for sig in ['private static async Task<bool> EnsureNativeConversationPersonaReadyAsync(','private static string BuildNativeConversationPersonaBackgroundHint(','private static string BuildNativeConversationPersonaGenerationFailedText(','private static async Task WaitForNativeConversationPersonaGenerationAsync(','private async Task EnsurePersonaForCandidatesAsync(']:
   needle='\t'+e.declaration(s,sig)+'\n\n';assert needle in s;s=s.replace(needle,'',1)
  prior=e.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
  current=prior.replace('EnsureNativeConversationPersonaReadyAsync(targetHero, onStreamText)','EnsureNativeConversationPersonaReadyAsync(admission, onStreamText)',1).replace('targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "unknown"','npcName ?? "unknown"')
  return s.replace(prior,current,1)
 if path=='CourierDeliveryBehavior.cs':
  needle='\t'+e.declaration(s,'private static async Task EnsureCourierPersonaContextReadyAsync(')+'\n\n';assert needle in s;s=s.replace(needle,'',1)
  for inbound,subject in [(False,'recipient'),(True,'sender')]:
   sig='private async Task PrepareAndGenerate'+('InboundLetter' if inbound else 'CourierReply')+'OffMainThreadAsync('
   prior=e.declaration(s,sig);chain='inbound' if inbound else 'reply';start=prior.index('\t\t\tCourierSession session = GetSessionById(sessionId);');end=prior.index('\n\t\t\tif (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_'+chain+'_persona_ready"))',start)
   replacement=f'''\t\t\tCourierPreparationAdmission admission = await RunCourierOwnerPhaseAsync(runtimeGeneration,
\t\t\t\t"courier_{chain}_admission", () => CaptureCourierPreparationAdmission(sessionId, {str(inbound).lower()}, runtimeGeneration), CancellationToken.None).ConfigureAwait(false);
\t\t\tif (admission == null) return;
\t\t\tCourierSession session = admission.Session;
\t\t\tHero {subject} = admission.Participant;
'''+('\t\t\tfallbackLetter = admission.FallbackLetter;\n' if inbound else '')+f'''\t\t\tif (!await EnsureCourierPersonaContextReadyAsync({subject}, "{chain}", sessionId, session, runtimeGeneration).ConfigureAwait(false)) return;'''
   s=s.replace(prior,prior[:start]+replacement+prior[end:],1)
  return s
 return s

def restore(path,source):
 if path not in ('ShoutBehavior.cs','CourierDeliveryBehavior.cs'):return source
 life_spec=importlib.util.spec_from_file_location('lifetime_inverse',ROOT/'tools/GameLifetimeTests/source_parity.py');life=importlib.util.module_from_spec(life_spec);life_spec.loader.exec_module(life)
 source=life.restore(path,source)
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in review['dependencies'].items():
  assert hashlib.sha256((ROOT/p).read_text(encoding='utf-8-sig').encode()).hexdigest()==h,'Unreviewed channel persona dependency: '+p
 assert source==expected(path),'Unreviewed '+('Courier source change' if path.startswith('Courier') else 'Shout source change')+' beyond channel persona preparation'
 return old(path)
if __name__=='__main__':
 for p in ['ShoutBehavior.cs','CourierDeliveryBehavior.cs']:restore(p,(ROOT/p).read_text(encoding='utf-8-sig'));print('PASS exact whole source inverse: '+p)
