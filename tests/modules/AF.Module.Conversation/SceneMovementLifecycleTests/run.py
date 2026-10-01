"""Current five movement admission/cancellation methods; game navigation primitives are substitutes."""
import argparse, importlib.util, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
parser.add_argument('--mutation', choices=['cancelled-arrival', 'generation', 'proxy-mission', 'arrival-once'])
args = parser.parse_args()
spec = importlib.util.spec_from_file_location('declarations', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec); spec.loader.exec_module(ex)
source = (ROOT / 'src/AF.GameAdapter.Bannerlord/SceneActions/SceneMovementController.cs').read_text(encoding='utf-8-sig')
if args.mutation:
    replacements = {
        'cancelled-arrival': ('if (item != null) item.Cancelled = true;', ''),
        'generation': ('Interlocked.Increment(ref _generation); // Retire callbacks', '// Retire callbacks'),
        'proxy-mission': ('if (ReferenceEquals(_mission, Mission.Current))', 'if (true)'),
        'arrival-once': ('request == null || request.Cancelled || request.GuideAgentIndex < 0 || request.ArrivalTriggered)', 'request == null || request.Cancelled || request.GuideAgentIndex < 0)'),
    }
    old, new = replacements[args.mutation]
    assert source.count(old) == 1, 'mutation anchor must select exactly one real statement'
    source = source.replace(old, new)
types = ['PendingSceneFollowCommand','SceneFollowReturnState','SceneSummonBatchState','ActiveSceneSummonRequest','ActiveSceneGuideRequest','SceneSummonConversationSession','SceneSummonConversationParticipant','SceneReturnJob','PendingSceneSummonReturnAfterSpeech','PendingSceneGuideReturnAfterSpeech','SceneGuideArrivalHold','SceneGhostWalkState']
declarations = [ex.declaration(source, 'internal sealed class ' + name) for name in types]
declarations += [ex.declaration(source, 'internal enum ' + name) for name in ['SceneSummonStage','SceneGuideStage']]
methods = ['internal bool StartSceneSummonBatchAction(', 'internal bool StartSceneGuideAction(', 'internal void CancelSceneSummonBatch(', 'internal void QueueSceneReturnJob(', 'internal void CancelSceneReturnJob(', 'internal static List<Location> FindSceneLocationPath(', 'internal bool TickSceneReturnJob(']
code = (HERE / 'Harness.cs.txt').read_text(encoding='utf-8').replace('@@TYPES@@', '\n'.join(declarations)).replace('@@METHODS@@','\n'.join(ex.declaration(source, signature) for signature in methods))
state = source[source.index('private readonly List<ActiveSceneSummonRequest>'):source.index('internal static string GetSceneSummonTargetDisplayName')]
lifecycle = ['private void EnsureCurrentMission(', 'internal void Reset(', 'internal void CancelSceneGuideActionForAgent(', 'internal EscortAgentBehavior.OnTargetReachedDelegate BuildSceneGuideArrivalCallback(', 'internal bool CompleteSceneGuideArrival(']
code = code.replace('@@STATE@@', state).replace('@@LIFECYCLE@@', '\n'.join(ex.declaration(source, signature) for signature in lifecycle))
assert '@@' not in code
out = new_run_root(ROOT, 'scene-movement-lifecycle', args.run_root)
(out/'Program.cs').write_text(code,encoding='utf-8')
(out/'CompactRuntime.cs').write_text((ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/SceneCompactReactionRuntime.cs').read_text(encoding='utf-8-sig'), encoding='utf-8')
(out/'CompactHarness.cs').write_text((HERE/'CompactHarness.cs.txt').read_text(encoding='utf-8').replace('@@OPTIONS@@', ex.declaration((ROOT/'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs').read_text(encoding='utf-8-sig'), 'internal readonly struct ConversationSpeechTextOptions')), encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0169</NoWarn></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT)
result=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(result.returncode)
