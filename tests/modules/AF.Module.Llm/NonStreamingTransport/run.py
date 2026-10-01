"""Execute actual shared transport, both primary bodies and actual configured adapter.
All network is a deterministic HttpMessageHandler; no game/provider/log writes.
"""
from pathlib import Path
import argparse,importlib.util,os,subprocess,json,hashlib,sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['leak-response','skip-accept','drop-caller-token','thinking-still-enabled','lose-retry-after']);p.add_argument("--run-root",type=Path);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(f):return (current_source_path(ROOT, f)).read_text(encoding='utf-8-sig')
for consumer in ['PolicySystem/Npc/PolicyLlmClient.cs','WorldDiplomacyLlmClient.cs']:
 domain=read(consumer)
 assert domain.count('LlmNonStreamingTransport.SendAsync(')==1, consumer+' must use the one-attempt shared owner exactly once'
 for duplicate in ['new HttpRequestMessage(HttpMethod.Post','response.Content.ReadAsStringAsync(']:
  assert duplicate not in domain, consumer+' still owns duplicate chat transport: '+duplicate
s=read('ShoutNetwork.cs');review=json.loads((HERE/'primary-source-review.json').read_text(encoding='utf-8-sig'))
current=ex.declaration(s,'public static async Task<string> CallApiWithMessages(')
# J17 B6 additive owner cancellation scope; exact inverse preserves the reviewed
# J01 baseline instead of refreshing its hash or weakening the behavior comparison.
owner_scope='\n\t\tusing CancellationTokenSource ownerCancellation = LlmNonStreamingTransport.LinkOwnerCancellation(cancellationToken);\n\t\tif (ownerCancellation != null) cancellationToken = ownerCancellation.Token;\n\t\tcancellationToken.ThrowIfCancellationRequested();'
assert current.count(owner_scope)==1, 'owner cancellation policy scope changed; review required'
assert hashlib.sha256(current.replace(owner_scope,'',1).encode()).hexdigest()==review['currentMethodSha256']
old=subprocess.check_output(['git','show',review['baseline']+':ShoutNetwork.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n');baseline_method=ex.declaration(old,'public static async Task<string> CallApiWithMessages(')
helpers='\n'.join(ex.declaration(s,sig) for sig in ['private static Task<HttpResponseMessage> SendPrimaryNonStreamingRequestAsync(','private static bool TryApplyPrimaryThinkingControls(','private static bool TryResolvePrimaryModelByDropdownState(','private static int ResolvePrimaryMaxTokens(','private static JObject BuildPrimaryChatPayload(','private static List<object> ApplyPlayerDisplayNameToOutgoingMessages(','private static string ExtractPrimaryResponseText(','private static string ExtractPrimaryReasoningText(','private static string BuildTokenStatsOutputContent('])
classes=[]
for name,body in [('Before',baseline_method),('After',current)]:
 if a.mutate=='thinking-still-enabled' and name=='After':body=body.replace('DuelSettings.RemoveThinkingControls(payload2);',';',1)
 classes.append('static class '+name+' { private const int DefaultPrimaryMaxTokens=DuelSettings.DefaultGeneralApiMaxTokens;'+helpers+body+' private static string ApplyPlayerDynamicNameToMainText(string s)=>s.Replace("PLAYER_PLACEHOLDER","FixturePlayer"); private static void LogNormalizedMessageTail(string a,string b,IEnumerable<object> c) {} private static void LogPrimaryRawResponse(string phase,string body) {} }')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@PRIMARY@@','\n'.join(classes));out=new_run_root(ROOT,'llm-nonstreamingtransport',a.run_root);(out/'Program.cs').write_text(code,encoding='utf-8')
files=['src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs','src/modules/AF.Module.Llm/Streaming/LlmStreamingTransport.cs','src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs','src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs','src/modules/AF.Module.Llm/Transport/LegacyConfiguredChatGateway.cs','src/AF.Contracts/Internal/FeatureBridgeContracts.cs','src/AF.Contracts/Internal/InteractionContracts.cs','src/AF.Contracts/Internal/LlmContracts.cs','src/AF.Foundation.Runtime/ModuleDirectory/FeatureBridgeRuntime.cs']
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
dotnet=str(resolve_dotnet(ROOT));newton=str(Path(dotnet).parent/'sdk/8.0.425/Newtonsoft.Json.dll')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><DefineConstants>TRACE</DefineConstants></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+newton+'</HintPath></Reference></ItemGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=minimal_test_environment(Path(dotnet),out)
p=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100);log=p.stdout+p.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(p.returncode)
