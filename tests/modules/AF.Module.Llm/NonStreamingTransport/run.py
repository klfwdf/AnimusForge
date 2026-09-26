"""Execute actual shared transport, both primary bodies and actual configured adapter.
All network is a deterministic HttpMessageHandler; no game/provider/log writes.
"""
from pathlib import Path
import argparse,importlib.util,os,subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['leak-response','skip-accept','drop-caller-token','thinking-still-enabled','lose-retry-after']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(f):return (ROOT/f).read_text(encoding='utf-8-sig')
for consumer in ['PolicySystem/Npc/PolicyLlmClient.cs','WorldDiplomacyLlmClient.cs']:
 domain=read(consumer)
 assert domain.count('LlmNonStreamingTransport.SendAsync(')==1, consumer+' must use the one-attempt shared owner exactly once'
 for duplicate in ['new HttpRequestMessage(HttpMethod.Post','response.Content.ReadAsStringAsync(']:
  assert duplicate not in domain, consumer+' still owns duplicate chat transport: '+duplicate
s=read('ShoutNetwork.cs');review=json.loads((HERE/'primary-source-review.json').read_text(encoding='utf-8-sig'))
current=ex.declaration(s,'public static async Task<string> CallApiWithMessages(');assert hashlib.sha256(current.encode()).hexdigest()==review['currentMethodSha256']
old=subprocess.check_output(['git','show',review['baseline']+':ShoutNetwork.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n');baseline_method=ex.declaration(old,'public static async Task<string> CallApiWithMessages(')
helpers='\n'.join(ex.declaration(s,sig) for sig in ['private static Task<HttpResponseMessage> SendPrimaryNonStreamingRequestAsync(','private static bool TryApplyPrimaryThinkingControls(','private static bool TryResolvePrimaryModelByDropdownState(','private static int ResolvePrimaryMaxTokens(','private static JObject BuildPrimaryChatPayload(','private static List<object> ApplyPlayerDisplayNameToOutgoingMessages(','private static string ExtractPrimaryResponseText(','private static string ExtractPrimaryReasoningText(','private static string BuildTokenStatsOutputContent('])
classes=[]
for name,body in [('Before',baseline_method),('After',current)]:
 if a.mutate=='thinking-still-enabled' and name=='After':body=body.replace('DuelSettings.RemoveThinkingControls(payload2);',';',1)
 classes.append('static class '+name+' { private const int DefaultPrimaryMaxTokens=DuelSettings.DefaultGeneralApiMaxTokens;'+helpers+body+' private static string ApplyPlayerDynamicNameToMainText(string s)=>s.Replace("PLAYER_PLACEHOLDER","FixturePlayer"); private static void LogNormalizedMessageTail(string a,string b,IEnumerable<object> c) {} private static void LogPrimaryRawResponse(string phase,string body) {} }')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@PRIMARY@@','\n'.join(classes));out=HERE/'.generated'/(a.mutate or 'current');out.mkdir(parents=True,exist_ok=True);(out/'Program.cs').write_text(code,encoding='utf-8')
files=['src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs','src/modules/AF.Module.Llm/Streaming/LlmStreamingTransport.cs','src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs','src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs','Refactor/Adapters/LegacyConfiguredChatGateway.cs','Refactor/Contracts/FeatureBridgeContracts.cs','Refactor/Contracts/InteractionContracts.cs','Refactor/Contracts/LlmContracts.cs','Refactor/Runtime/FeatureBridgeRuntime.cs']
for f in files:
 text=read(f)
 if f.endswith('LlmNonStreamingTransport.cs'):
  if a.mutate=='leak-response':text=text.replace('using (HttpResponseMessage response = await sender(request, cancellationToken).ConfigureAwait(false))','HttpResponseMessage response = await sender(request, cancellationToken).ConfigureAwait(false);',1)
  if a.mutate=='skip-accept':text=text.replace('acceptResponse != null && !acceptResponse(response.StatusCode)','acceptResponse != null && !acceptResponse(response.StatusCode) && false',1)
  if a.mutate=='drop-caller-token':text=text.replace('CreateLinkedTokenSource(callerToken)','CreateLinkedTokenSource(CancellationToken.None)',1)
  if a.mutate=='lose-retry-after':text=text.replace('return Math.Max(0, (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds));','return 0;',1)
 (out/Path(f).name).write_text(text,encoding='utf-8')
baseline_gateway=subprocess.check_output(['git','show',review['baseline']+':Refactor/Adapters/LegacyConfiguredChatGateway.cs'],cwd=ROOT).decode('utf-8-sig')
old_gateway=ex.declaration(baseline_gateway,'public sealed class LegacyConfiguredChatGateway :')
old_gateway=old_gateway.replace('LegacyConfiguredChatGateway','OriginalConfiguredChatGateway')
header=baseline_gateway.split('namespace AnimusForge.Refactor.Adapters;',1)[0]
(out/'OriginalConfiguredChatGateway.cs').write_text(header+'namespace AnimusForge.Refactor.Adapters;\n'+old_gateway,encoding='utf-8')
newton=os.environ.get('AF_NEWTONSOFT','G:/AFMOD/.dotnet-sdk/sdk/8.0.422/Newtonsoft.Json.dll')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><DefineConstants>TRACE</DefineConstants></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+newton+'</HintPath></Reference></ItemGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=os.environ.get('AF_DOTNET','G:/AFMOD/.dotnet-sdk/dotnet.exe');env=os.environ.copy();env.update(DOTNET_ROOT=str(Path(dotnet).parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'))
p=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100);log=p.stdout+p.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(p.returncode)
