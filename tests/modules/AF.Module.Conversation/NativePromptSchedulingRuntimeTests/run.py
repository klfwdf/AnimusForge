import argparse,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['routing-game','skip-capture-guard','late-generation']);a=p.parse_args()
out=new_run_root(ROOT,'shared-routing-runtime',a.run_root)
names=['SharedPromptRoutingRuntime','PromptTopicRoutingStage','PromptBuildRequest','PromptRetrievalCapture','PromptRuleIdPolicy','BuiltInRuleStickyCarry','PromptBuiltInTopicRouter','PromptRuntimeTargetBinding','PromptRuleEligibility','PreprocessFormatException']
for n in names:
 text=(ROOT/f'src/modules/AF.Module.Prompt/Composition/{n}.cs').read_text(encoding='utf-8-sig')
 (out/f'{n}.cs').write_text(text,encoding='utf-8')
(out/'GuardrailRuleHit.cs').write_bytes((ROOT/'src/modules/AF.Module.Prompt/Retrieval/GuardrailRuleHit.cs').read_bytes())
(out/'Stubs.cs').write_bytes((ROOT/'tests/modules/AF.Module.Prompt/Composition/Stubs.cs').read_bytes())
for n in ['ShoutBehavior.NativePromptBuild','NativePromptWorkScheduler','ConversationGameThreadDispatcher']:
 text=(ROOT/f'src/modules/AF.Module.Conversation/Channels/Native/{n}.cs').read_text(encoding='utf-8-sig')
 if n=='ShoutBehavior.NativePromptBuild' and a.mutation:
  mutation={'routing-game':('Task<MyBehavior.ShoutPromptContext> routingTask = RunNativeConversationBackgroundPreprocessAsync(target, targetAgentIndex, runtimeGeneration, () =>', 'Task<MyBehavior.ShoutPromptContext> routingTask = _ports.PromptDispatcher.RunAsync("bad_route", target, targetAgentIndex, () =>'), 'skip-capture-guard':('_ports.IsNativeConversationAdmissionCurrent(admission, out _)\n                ?','true\n                ?'), 'late-generation':('if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_prompt_build_routing"))', 'if (false)')}
  old,new=mutation[a.mutation];assert text.count(old)==1;text=text.replace(old,new,1)
  if a.mutation=='routing-game':text=text.replace('});\n\t\tMyBehavior.ShoutPromptContext routed', '}, (MyBehavior.ShoutPromptContext)null);\n\t\tMyBehavior.ShoutPromptContext routed',1)
 (out/f'{n}.cs').write_text(text,encoding='utf-8')
(out/'Registry.cs').write_bytes((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_bytes())
base=(ROOT/'tests/modules/AF.Module.Prompt/SharedRoutingRuntimeTests/Harness.cs.txt').read_text(encoding='utf-8');base=base[:base.index('internal static class Program {')].replace('internal static class MyBehavior','internal static partial class MyBehavior').replace('internal static class AIConfigHandler','internal static partial class AIConfigHandler');(out/'Program.cs').write_text(base+(HERE/'Harness.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><UseAppHost>false</UseAppHost></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
