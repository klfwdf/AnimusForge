"""Full original routing and diagnostics body inverse, with reviewed capture substitutions."""
import importlib.util,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
old=subprocess.check_output(['git','show','84f428cd:src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
new=(ROOT/'src/modules/AF.Module.Prompt/Composition/SharedPromptRoutingRuntime.cs').read_text(encoding='utf-8-sig')
new=new.replace('PromptBuildPhases phases = work.Phases;', '').replace('PromptRoutingInput routingInput = work.Input;', 'PromptRoutingInput routingInput = CreatePromptRoutingInput(request);').replace('work.Ports)', 'CreatePromptRoutingPorts())').replace('MyBehavior.LogShoutPromptContextStage(', 'LogShoutPromptContextStage(')
new=new.replace('new HashSet<string>(request.ExcludedRuleIds, request.ExcludedRuleIds.Comparer)','request.ExcludedRuleIds').replace('request.ExcludedRuleIds == null ? null : request.ExcludedRuleIds','request.ExcludedRuleIds').replace('new HashSet<string>(request.PreprocessExcludedRuleIds, request.PreprocessExcludedRuleIds.Comparer)','request.PreprocessExcludedRuleIds').replace('request.PreprocessExcludedRuleIds == null ? null : request.PreprocessExcludedRuleIds','request.PreprocessExcludedRuleIds').replace('request.ForcedPreprocessRuleIds?.ToArray()', 'request.ForcedPreprocessRuleIds')
for a,b in [('internal void RunSharedPromptRouting(', 'internal static void Run('),('private static PromptRoutingInput CreatePromptRoutingInput(', 'internal static PromptRoutingInput CaptureInput('),('private static void LogPromptRoutingDiagnostics(', 'internal static void LogPromptRoutingDiagnostics(')]:
 before=ex.declaration(old,a);after=ex.declaration(new,b)
 assert re.findall(r'\S+',before[before.index('{'):])==re.findall(r'\S+',after[after.index('{'):]), 'routing body drift '+a
print('PASS complete routing/input/diagnostics inverse; explicit detached collection capture substitutions')
