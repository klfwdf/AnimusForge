import argparse,importlib.util,os,subprocess,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['drop-callback-guard','drop-revision','use-backend-slot','drop-stamp-retirement','allow-stale-finish','allow-stale-notice']);args=p.parse_args()
s=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');partial=(ROOT/'ShoutBehavior.NativeAdmission.cs').read_text(encoding='utf-8-sig');overlay=(ROOT/'AnimusForgeNativeConversationOverlay.cs').read_text(encoding='utf-8-sig');ui=(ROOT/'AnimusForgeNativeConversationOverlay.Presentation.cs').read_text(encoding='utf-8-sig')
selectors={'ENTRY':'public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)','OPENING_ENTRY':'public static Task<string> SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)','ACTION_RESULT':'private sealed class NativeConversationGameActionResult','ACTION_QUEUE':'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync('}
values={key:ex.declaration(s,sig) for key,sig in selectors.items()};body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(');values['PREFIX']=body.split('\t\tLogger.Log("Logic", "[NativePerf] submit_start')[0]
values['GENERATION_HELPERS']='\n'.join(ex.declaration(overlay,sig) for sig in ['private bool IsSubmitGenerationActive(', 'private bool IsSubmitGenerationCurrent('])
values['RUN_ON_MAIN']=ex.declaration(overlay,'private void RunOnMainThread(')
values['NOTICES']='\n'.join(ex.declaration(overlay,sig) for sig in ['private void QueuePostprocessNotice(', 'private void FlushPendingPostprocessNotice(', 'private void ClearPendingPostprocessNotice('])
values['DOTS']='\n'.join(ex.declaration(overlay,sig) for sig in ['private void UpdateWaitingDotsAnimation(', 'private static string GetWaitingDotsText('])
for key,sig in [('PLAYER_STREAM','private async Task SubmitAsync(string text)'),('OPENING_STREAM','private async Task SubmitNpcInitiatedOpeningAsync(')]:
 method=ex.declaration(overlay,sig);values[key]=ex.declaration(method,'RunNativePresentationCallback(generation, delegate')+');'
 # Every visual async callback is guarded. The sole unguarded scheduler is the owner-aware finally.
 before,finally_body=method.rsplit('\n\t\tfinally',1)
 assert 'RunOnMainThread(' not in before and before.count('RunNativePresentationCallback(generation, ')==7
 assert 'CompleteNativeSubmissionPresentation(generation)' in finally_body
 assert 'IsNativeConversationResponseTargetAvailableForExternal()' not in method
 assert 'CaptureNativeConversationPresentationScopeForOverlay()' in method and 'SubmitNativeConversationForOverlayAsync(presentationScope,' in method
assert 'ValidatePendingSubmissionPresentation();' in ex.declaration(overlay,'private void Tick()')
if args.mutate=='drop-callback-guard':ui=ui.replace('if (!IsSubmissionPresentationCurrent(generation))','if (false)',1)
if args.mutate=='drop-revision':partial=partial.replace('_snapshot.PresentationRevision == Interlocked.Read(ref _owner._nativeConversationPresentationRevision)','true',1)
if args.mutate=='use-backend-slot':partial=partial.replace('=> HasCurrentContext() && _owner.IsNativeConversationContextCurrent(_snapshot, out _);','=> _owner.IsNativeConversationAdmissionCurrent(_snapshot, out _);',1)
if args.mutate=='drop-stamp-retirement':ui=ui.replace('if (_isSubmitting && _submitPresentationScope != null && !_submitPresentationScope.HasCurrentContext())','if (false)',1)
if args.mutate=='allow-stale-finish':
 method=ex.declaration(ui,'private bool CompleteNativeSubmissionPresentation(');ui=ui.replace(method,method.replace('if (!IsSubmissionPresentationCurrent(generation))','if (false)',1),1)
if args.mutate=='allow-stale-notice':values['NOTICES']=values['NOTICES'].replace(' || !IsSubmissionPresentationCurrent(generation)','')
base=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('    static class ConversationHelper')[0]
code=base+(HERE/'PresentationHarness.cs.txt').read_text(encoding='utf-8-sig')
for key,value in values.items():code=code.replace('@@'+key+'@@',value)
assert '@@' not in code
# The test scope object must not depend on any game/link formatter implementation.
code=code.replace('internal static void Mark(string stage, string text, bool immediate = false) { }','internal static void Mark(string stage, string text, bool immediate = false) { } internal sealed class EmptyScope : IDisposable { public void Dispose() { } } internal static IDisposable Scope(string text) => new EmptyScope();')
out=HERE/'.generated'/('presentation-'+(args.mutate or 'current'));out.mkdir(parents=True,exist_ok=True)
for name,text in [('Program.cs',code),('Admission.cs',partial),('Presentation.cs',ui)]: (out/name).write_text(text,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0169;CS0414;CS0219</NoWarn></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log='admissionSha256='+hashlib.sha256(partial.encode()).hexdigest()+' uiSha256='+hashlib.sha256(ui.encode()).hexdigest()+' mutation='+str(args.mutate)+'\n'+r.stdout+r.stderr
(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
