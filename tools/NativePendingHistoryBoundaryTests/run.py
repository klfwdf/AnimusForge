import argparse,importlib.util,subprocess,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(name):return subprocess.check_output(['git','show','5847a195:'+name],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/name).read_text(encoding='utf-8-sig')
s=read('ShoutBehavior.cs');ad=read('ShoutBehavior.NativeAdmission.cs');body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
start=body.index('\t\tstring playerName = GetPlayerDisplayNameForShout();') if a.original else body.index('\t\tNativeConversationPendingHistory nativePendingHistory = await')
end=body.index('\t\tbool useSharedDailyMemoryForNpcOpening',start)
values={'PREPARE':body[start:end],'ADMISSION':ex.declaration(ad,'internal sealed class NativeConversationAdmission'),'CHECKS':'\n'.join(ex.declaration(ad,x) for x in ['private bool IsNativeConversationAdmissionCurrent(','private bool IsNativeConversationContextStampCurrent(','private bool IsNativeConversationContextCurrent('])}
selectors=['private static void AppendNativeConversationSessionHistory(','private static void RollbackNativeConversationPendingPlayerHistory(','private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(','private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(','private static AnimusForgeDialogueHistoryEntry CloneNativeConversationHistoryEntry(','private void RemoveNativeConversationSessionHistoryEventFromSceneHistory(','private List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(','private static long NextConversationEventSequence(']
values['HISTORY_METHODS']='\n'.join(ex.declaration(s,x) for x in selectors)
branches=['nativeMainReplyTargetAvailableBeforeDispatch','nativeMainReplyTargetAvailable','nativePostprocessStartTargetAvailable','nativeDirectCommandTargetAvailable','nativePostprocessTargetAvailable']
values['REJECTIONS']='\n'.join('case '+str(i)+': { '+ex.declaration(body,'if (!'+name+')')+' break; }' for i,name in enumerate(branches))
# The fixture returns a capture object instead of visible text; preserve the actual no-capture gate.
if not a.original:
 assert 'if (nativePendingHistory == null) return "";' in values['PREPARE']
 values['PREPARE']=values['PREPARE'].replace('if (nativePendingHistory == null) return "";','if (nativePendingHistory == null) return null;',1)
if not a.original:
 prior=subprocess.check_output(['git','show','5847a195:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
 for signature in ['private static void AppendNativeConversationSessionHistory(','private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(','private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(']:
  current=ex.declaration(s,signature);old=ex.declaration(prior,signature)
  inverse=current.replace(current.splitlines()[0],old.splitlines()[0],1).replace('capturedHistoryKey ?? BuildNativeConversationHistoryKey','BuildNativeConversationHistoryKey').replace('maxLines, npc, capturedHistoryKey);','maxLines, npc);')
  assert inverse==old, 'Default history-helper behavior changed: '+signature
 if a.mutate in ['ignore-append-key','ignore-read-key']:
  signature='private static void AppendNativeConversationSessionHistory(' if a.mutate=='ignore-append-key' else 'private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot('
  old=ex.declaration(values['HISTORY_METHODS'],signature);values['HISTORY_METHODS']=values['HISTORY_METHODS'].replace(old,old.replace('capturedHistoryKey ?? BuildNativeConversationHistoryKey','BuildNativeConversationHistoryKey'),1)
 if a.mutate=='drop-rollback-context':values['HISTORY_METHODS']=values['HISTORY_METHODS'].replace('|| !owner.IsNativeConversationContextStampCurrent(admission)\n            || admission.PresentationRevision != Interlocked.Read(ref owner._nativeConversationPresentationRevision)','|| false',1)
 if a.mutate=='recompute-rollback-key':
  old=ex.declaration(values['HISTORY_METHODS'],'private static void RollbackNativeConversationPendingPlayerHistory(')
  new=old.replace('        lock (_nativeConversationSessionHistoryLock)','        historyKey = BuildNativeConversationHistoryKey(admission.Hero, admission.Character, admission.NpcName, admission.AgentIndex);\n        lock (_nativeConversationSessionHistoryLock)',1)
  values['HISTORY_METHODS']=values['HISTORY_METHODS'].replace(old,new,1)
 if a.mutate=='remove-player-filter':values['HISTORY_METHODS']=values['HISTORY_METHODS'].replace('&& string.Equals(entry.Kind, "player", StringComparison.OrdinalIgnoreCase)','',1)
 if a.mutate=='remove-user-filter':values['HISTORY_METHODS']=values['HISTORY_METHODS'].replace(' && string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)','')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
for k,v in values.items():code=code.replace('@@'+k+'@@',v)
assert '@@' not in code
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8')
for name in ['AnimusForgeDialogueHistoryEntry.cs','ConversationMessage.cs']:(out/name).write_text(read(name),encoding='utf-8')
if not a.original:
 pending=read('ShoutBehavior.NativePendingHistory.cs')
 if a.mutate=='drop-prepare-guard':pending=pending.replace('if (!IsNativeConversationAdmissionCurrent(admission, out _))','if (false)',1)
 if a.mutate=='drop-claim':pending=pending.replace('if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;','if (false) return;',1)
 if a.mutate=='keep-expired-live':pending=pending.replace('winner != completion.Task && Interlocked.CompareExchange(ref state, 2, 0) == 0','winner != completion.Task && Volatile.Read(ref state) == 0',1)
 if a.mutate=='expire-started':pending=pending.replace('winner != completion.Task && Interlocked.CompareExchange(ref state, 2, 0) == 0','winner != completion.Task && Interlocked.Exchange(ref state, 2) != 2',1)
 if a.mutate=='keep-failed-publication':pending=pending.replace('if (Interlocked.CompareExchange(ref state, 2, 0) == 0) completion.TrySetException(ex);','completion.TrySetException(ex);',1)
 if a.mutate=='allow-diagnostic-failure':pending=pending.replace('// Optional diagnostics cannot alter queue ownership or report a fake completion.\n            return;','// Mutated observer.\n            throw;',1)
 (out/'PendingHistory.cs').write_text(pending,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '')+'</PropertyGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
