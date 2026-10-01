"""Exact channel-persona/admission delta before legacy whole-owner proofs."""
from pathlib import Path
import hashlib,importlib.util,json,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent;BASELINE='4140bd04'
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
spec=importlib.util.spec_from_file_location('channel_persona_decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
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


def restore_relocation_runner(path, source):
 if path != 'tests/modules/AF.Module.Conversation/ChannelPersonaPreparationTests/run.py': return source
 edits=[('ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent\np=argparse.ArgumentParser();p.add_argument(\'--run-root\',type=Path);p.add_argument(\'--dotnet\', default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")));p.add_argument(\'--original\',action=\'store_true\');p.add_argument(\'--mutate\',choices=[\'native_skip_admission\',\'native_accept_failure\',\'courier_drop_session\',\'courier_reject_fallback\',\'scene_generate_partial\',\'scene_skip_scope\',\'scene_accept_replaced\',\'invalid_target_cleanup\',\'waiter_ignore_deadline\',\'waiter_ignore_scope\']);a=p.parse_args()\n', 'ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent\nimport sys\nsys.path.insert(0, str(ROOT / "tests"))\nfrom output_isolation import current_source_path\np=argparse.ArgumentParser();p.add_argument(\'--run-root\',type=Path);p.add_argument(\'--dotnet\', default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")));p.add_argument(\'--original\',action=\'store_true\');p.add_argument(\'--mutate\',choices=[\'native_skip_admission\',\'native_accept_failure\',\'courier_drop_session\',\'courier_reject_fallback\',\'scene_generate_partial\',\'scene_skip_scope\',\'scene_accept_replaced\',\'invalid_target_cleanup\',\'waiter_ignore_deadline\',\'waiter_ignore_scope\']);a=p.parse_args()\n'), ("spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)\ndef read(p):return (ROOT/p).read_text(encoding='utf-8-sig')\ndef old(p):return subprocess.check_output(['git','show','4140bd04:'+p],cwd=ROOT).decode('utf-8-sig').replace('\\r\\n','\\n')\n", "spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)\ndef read(p):return (current_source_path(ROOT, p)).read_text(encoding='utf-8-sig')\ndef old(p):return subprocess.check_output(['git','show','4140bd04:'+p],cwd=ROOT).decode('utf-8-sig').replace('\\r\\n','\\n')\n")]
 for before, after in reversed(edits):
  assert source.count(after)==1, "Unreviewed source relocation runner delta: "+path
  source=source.replace(after,before,1)
 return source

def restore(path,source):
 if path not in ('ShoutBehavior.cs','CourierDeliveryBehavior.cs'):return source
 live=(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')
 assert source==live,'Unreviewed '+('Courier' if path.startswith('Courier') else 'Shout')+' input change'
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in review['dependencies'].items():
  text=(current_source_path(ROOT, p)).read_text(encoding='utf-8-sig')
  if p.endswith('/run.py'):
   expected_runner=subprocess.check_output(['git','show','f6e2ead7:'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
   text=restore_relocation_runner(p,text)
   if p.endswith('/run.py'):
    for delta in review['runnerSafetyChanges']:
     assert expected_runner.count(delta['before'])==1
     expected_runner=expected_runner.replace(delta['before'],delta['after'],1)
   assert text==expected_runner,'Unreviewed channel persona dependency: '+p
  else:assert hashlib.sha256(text.encode()).hexdigest()==h,'Unreviewed channel persona dependency: '+p
 # Native now has a thin admission delegate; Courier preparation lives in GenerationLifecycle.
 scopes={'ShoutBehavior.NativeTurn.cs':['public async Task<NativeConversationTurnStep> PrepareAsync('],
         'ShoutBehavior.cs':['private Task<string> SubmitNativeConversationTextInternalAsync('],
         'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs':[
          'private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(',
          'private async Task PrepareAndGenerateInboundLetterOffMainThreadAsync(']}
 for actual,signatures in scopes.items():
  text=(current_source_path(ROOT, actual)).read_text(encoding='utf-8-sig')
  accepted=subprocess.check_output(['git','show','f6e2ead7:'+actual],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
  for signature in signatures:
   assert e.declaration(text,signature)==e.declaration(accepted,signature),'Unreviewed channel persona consumer: '+signature
 return old(path)
if __name__=='__main__':
 for p in ['ShoutBehavior.cs','CourierDeliveryBehavior.cs']:restore(p,(current_source_path(ROOT, p)).read_text(encoding='utf-8-sig'));print('PASS scoped current persona consumer, retained historical snapshot: '+p)
