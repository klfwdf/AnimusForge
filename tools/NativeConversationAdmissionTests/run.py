import argparse,importlib.util,os,subprocess,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['drop-busy','release-new-slot','skip-timeout-cas','skip-queued-action-guard','skip-generation','old-overlay-finalizer','skip-queued-epoch']);args=p.parse_args()
s=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');partial=(ROOT/'ShoutBehavior.NativeAdmission.cs').read_text(encoding='utf-8-sig')
selectors={'ENTRY':'public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)','OPENING_ENTRY':'public static Task<string> SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)','ACTION_RESULT':'private sealed class NativeConversationGameActionResult','ACTION_QUEUE':'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync('}
values={k:ex.declaration(s,v) for k,v in selectors.items()};body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
values['PREFIX']=body.split('\t\tLogger.Log("Logic", "[NativePerf] submit_start')[0];assert 'admission.ConversationToken' in values['PREFIX']
# Independent wiring checks: unchanged UI existence condition, actual conversation-end invalidation,
# no late target recapture in the full owner, and exact six pre-action guard sites.
baseline=subprocess.check_output(['git','show','14dec2d7:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
assert ex.declaration(s,'public static bool CanSubmitNativeConversationForExternal()')==ex.declaration(baseline,'public static bool CanSubmitNativeConversationForExternal()')
pre=body.split('\t\tStopwatch nativeActionSw =')[0]
assert pre.count('IsNativeConversationAdmissionCurrent(admission, out ')==7
assert 'TryResolveNativeConversationTarget(' not in body and 'NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening' not in body
assert 'npcOpeningConsumed = true;' in body and 'admission.OpeningExtraFact' in body
my=(ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig');ended=ex.declaration(my,'private void OnMemoryConversationEnded(')
assert ended.split('{',1)[1].lstrip().startswith('ShoutBehavior.InvalidateNativeConversationAdmissionOnConversationEnd();')
overlay=(ROOT/'AnimusForgeNativeConversationOverlay.cs').read_text(encoding='utf-8-sig')
assert overlay.count('catch (ShoutBehavior.NativeConversationAdmissionException ex)')==2
assert 'ShoutBehavior.IsNativeConversationBackendBusy()' in ex.declaration(overlay,'private void HandleSubmitRequested(')
opening=ex.declaration(overlay,'private void TryStartPendingNpcOpening(');assert opening.index('IsNativeConversationBackendBusy')<opening.index('_npcOpeningAutoStarted = true')
if args.mutate=='drop-busy':partial=partial.replace('if (IsNativeConversationAdmissionCurrent(Volatile.Read(ref _nativeConversationAdmission), out _))','if (false)',1)
if args.mutate=='release-new-slot':partial=partial.replace('Interlocked.CompareExchange(ref _nativeConversationAdmission, null, admission);','Interlocked.Exchange(ref _nativeConversationAdmission, null);',1)
if args.mutate=='skip-timeout-cas':partial=partial.replace('if (Interlocked.CompareExchange(ref dispatchState, 1, 0) != 0)','if (false)',1)
if args.mutate=='skip-generation':partial=partial.replace('|| !SaveRuntimeGuard.IsCurrentGeneration(admission.Generation)','|| false',1)
if args.mutate=='skip-queued-epoch':partial=partial.replace('|| conversationEpoch != Interlocked.Read(ref _nativeConversationAdmissionEpoch)', '|| false', 1)
if args.mutate=='skip-queued-action-guard':values['ACTION_QUEUE']=values['ACTION_QUEUE'].replace('if (!IsNativeConversationAdmissionCurrent(admission, out _))','if (false)',2)
overlay_source = subprocess.check_output(['git','show','14dec2d7:AnimusForgeNativeConversationOverlay.cs'],cwd=ROOT).decode('utf-8-sig') if args.mutate=='old-overlay-finalizer' else overlay
finalizers=[]
for signature,name in [('private async Task SubmitAsync(string text)', 'CompletePlayer'),('private async Task SubmitNpcInitiatedOpeningAsync(', 'CompleteOpening')]:
    method=ex.declaration(overlay_source,signature)
    final=method[method.rindex('\t\tfinally'):]
    if 'CompleteNativeSubmissionPresentation(generation)' in final:
        prefix=final.split('if (_dataSource.IsCustomAnswerVisible)')[0]
    else:
        prefix=final.split('_dataSource.SetBusy(false);')[0]
        assert 'ConversationHelper.EndStreaming();' in prefix and '_isSubmitting = false;' in prefix
    finalizers.append('private void '+name+'(int generation) { try { } '+prefix+'\n} }); } }')
values['OVERLAY_COMPLETION_HELPER']=ex.declaration((ROOT/'AnimusForgeNativeConversationOverlay.Presentation.cs').read_text(encoding='utf-8-sig'),'private bool CompleteNativeSubmissionPresentation(')
values['OVERLAY_FINALIZERS']='\n'.join(finalizers)+'\ninternal void Complete(int generation, bool opening) { if (opening) CompleteOpening(generation); else CompletePlayer(generation); }'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
for key,value in values.items():code=code.replace('@@'+key+'@@',value)
assert '@@' not in code
out=HERE/'.generated'/(args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Admission.cs').write_text(partial,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log='sourceSha256='+hashlib.sha256(s.encode()).hexdigest()+' admissionSha256='+hashlib.sha256(partial.encode()).hexdigest()+' mutation='+str(args.mutate)+'\n'+r.stdout+r.stderr
(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
