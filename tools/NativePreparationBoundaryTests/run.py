import argparse,importlib.util,subprocess,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(path):return subprocess.check_output(['git','show','50f84818:'+path],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/path).read_text(encoding='utf-8-sig')
s=read('ShoutBehavior.cs');body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
start=body.index('\t\tint nativeTargetAgentIndex = admission.AgentIndex;');end=body.index('\t\tbool includeCurrentSceneSessionInPersistedHistory',start)
prepare=body[start:end].replace('return "";','return null;')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@PREPARE@@',prepare).replace('@@RUN@@',ex.declaration(s,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')).replace('@@WAIT@@',ex.declaration(s,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>('))
prior=subprocess.check_output(['git','show','50f84818:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
prior_body=ex.declaration(prior,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
pa=prior_body.index('\t\tint nativeTargetAgentIndex = admission.AgentIndex;');pb=prior_body.index('\t\tbool includeCurrentSceneSessionInPersistedHistory',pa)
code=code.replace('@@BASELINE@@',prior_body[pa:pb].replace('return \"\";','return null;'))
if a.original:
 code=code.replace('@@SNAPSHOT@@','').replace('@@CAPTURE@@','')
else:
 snapshot=read('ShoutBehavior.NativePreparation.cs')
 # Exact preparation extraction, not permission to change any surrounding Native/Scene logic.
 assert body.replace(body[start:end],prior_body[pa:pb],1)==prior_body
 assert s.replace(body,prior_body,1)==prior
 old=prior_body[pa:pb]
 expected=old[:old.index('\t\tstring nativeInitialTargetUnavailableReason')]+old[old.index('\t\tList<NpcDataPacket> presentNpcs'):].replace('\t\t// Do not feed vanilla conversation UI text into AF prompt history.\n\t\tstring currentNativeDialogText = "";\n','').replace('\t\tstring extraFact = npcOpeningPersistentFactText;\n','')
 capture=ex.declaration(snapshot,'private NativeConversationPreparationSnapshot CaptureNativeConversationPreparation(')
 assert expected in capture, 'Preparation builder order/arguments differ from original'
 assert 'Task.Run' not in capture and 'await ' not in capture
 code=code.replace('@@SNAPSHOT@@',ex.declaration(snapshot,'private sealed class NativeConversationPreparationSnapshot')).replace('@@CAPTURE@@',ex.declaration(snapshot,'private NativeConversationPreparationSnapshot CaptureNativeConversationPreparation('))
 mutations={
  'drop-guard':('if (!IsNativeConversationAdmissionCurrent(admission, out reason)) return null;','reason = "";'),
  'move-capture-background':('() => CaptureNativeConversationPreparation(admission, targetHero, targetCharacter, npcName, routingInput, out nativeInitialTargetUnavailableReason)','() => Task.Run(() => CaptureNativeConversationPreparation(admission, targetHero, targetCharacter, npcName, routingInput, out nativeInitialTargetUnavailableReason)).GetAwaiter().GetResult()'),
  'lose-culture':('CultureId = cultureId,','CultureId = "wrong",'),
  'lose-rules':('ExcludedRuleIds = preprocessExcludedRuleIds','ExcludedRuleIds = new List<string>()'),
  'wrong-guide-offset':('x?.PromptId ?? 0) : 0) + 1','x?.PromptId ?? 0) : 0) + 2'),
 }
 if a.mutate:
  old,new=mutations[a.mutate];assert old in code;code=code.replace(old,new,1)
assert '@@' not in code
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True);(out/'Program.cs').write_text(code,encoding='utf-8');(out/'PreprocessFormatException.cs').write_text(read('PreprocessFormatException.cs'),encoding='utf-8');(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>',encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150);log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
