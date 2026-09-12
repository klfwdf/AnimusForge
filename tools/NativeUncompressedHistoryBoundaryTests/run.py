"""Exercise the actual Native uncompressed-history call site, bridge and main dispatcher."""
import argparse, hashlib, importlib.util, os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['skip-admission','always-include','skip-rollback']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
prior=subprocess.check_output(['git','show','53ddb7d4:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
s=prior if a.original else (ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig')
def fragment(source):
 turn=ex.declaration(source,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
 start=turn.index('\t\tbool useSharedDailyMemoryForNpcOpening = npcInitiatedOpening;')
 end=turn.index('\t\tstring taskSystemBlock =',start)
 return turn[start:end]
body=fragment(s)
if not a.original:
 assert s.replace(body,fragment(prior),1)==prior,'Unreviewed surrounding Native source change'
 print('PASS one-call-site inverse equals 53ddb7d4 full Shout owner')
if a.mutate=='skip-admission':body=body.replace('!IsNativeConversationAdmissionCurrent(admission, out _)','false')
if a.mutate=='always-include':body=body.replace('(useSharedDailyMemoryForNpcOpening || !hadNativeConversationSessionHistoryBeforeTurn)','true')
if a.mutate=='skip-rollback':
 old='await RollbackNativeConversationPendingPlayerHistoryAsync(admission, nativePendingAfefKey,\n\t\t\t\tnativePendingPlayerHistoryEventSequence, "uncompressed_history_unavailable").ConfigureAwait(false);'
 assert old in body;body=body.replace(old,'await Task.CompletedTask;')
body=body.replace('return "";','return null;')
selectors=['private Task<T> RunNativeConversationMainThreadFuncAsync<T>(', 'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(',
'private static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, CharacterObject targetCharacter,',
'private static List<ConversationMessage> RemoveNativeMessagesAlreadyInPersistentMemory(', 'private static string BuildConversationMemoryDedupKey(']
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@BODY@@',body).replace('@@METHODS@@','\n'.join(ex.declaration(s,sig) for sig in selectors))
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8')
for path in ['ConversationMessage.cs','SaveRuntimeGuard.cs','PreprocessFormatException.cs']:(out/path).write_text((ROOT/path).read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=os.environ.get('DOTNET_EXE',r'C:\Program Files\dotnet\dotnet.exe');env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_CLI_UI_LANGUAGE='en',APPDATA=str(ROOT/'.tmp/appdata'))
r=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
