"""Current sequencer + current game-capture adapter + current postprocess call slice.
Game/provider ports are deterministic substitutes, not real Host or audio acceptance.
"""
import argparse,importlib.util,os,subprocess,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['skip-stage-stop','duplicate-commit','skip-capture-guard','capture-on-worker','normalize-on-worker','swallow-capture-failure']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(f):return (ROOT/f).read_text(encoding='utf-8-sig')
owner=read('ShoutBehavior.NativeTurn.cs');capture=ex.declaration(owner,'private async Task<bool> CaptureOnGameThreadAsync(')
commit=read('ShoutBehavior.NativeTurnCommit.cs')
start=commit.index('                SceneActionPostprocessWorkItem workItem = null;')
# Select the actual prepare/network/complete statements, not a reimplementation.
end=commit.index('\n\n            }',start)
slice=commit[start:end]
prepare_signature=ex.declaration(read('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs'),'private static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess(').split('\n',1)[0]
prepare_signature=prepare_signature.replace('private static','private')
for t in ['Hero','CharacterObject','List<RewardSystemBehavior.DuelStakeOption>','List<PostprocessRuleEntry>','List<SceneSummonPromptTarget>','List<SceneGuidePromptTarget>','List<string>','List<NpcDataPacket>','DetachedPromptSections']:
 prepare_signature=prepare_signature.replace(t+' ', 'object ')
fields=[]
for file in ['ShoutBehavior.NativeTurn.cs','ShoutBehavior.NativeTurnPrompt.cs','ShoutBehavior.NativeTurnPresentation.cs','ShoutBehavior.NativeTurnCommit.cs']:
 for typ,name in re.findall(r'^        private ([\w.]+(?:<[^;=\n]+>)?(?:\[\])?) (\w+);$',read(file),re.M):
  if name in ['playerText','nativeTargetLog','nativeTargetAgentIndex','nativePendingAfefKey','nativePendingPlayerHistoryEventSequence']:continue
  typ=typ if typ in ['string','bool','int','long','Stopwatch'] else 'object'
  fields.append('    private '+typ+' '+name+';')
coordinator=read('src/modules/AF.Module.Conversation/Channels/Native/NativeConversationTurnCoordinator.cs')
if a.mutate=='skip-stage-stop':coordinator=coordinator.replace('if (!step.CanContinue) return step.StopText;','if (false) return step.StopText;',1)
if a.mutate=='duplicate-commit':coordinator=coordinator.replace('step = await host.PostprocessAndCommitAsync().ConfigureAwait(false);','step = await host.PostprocessAndCommitAsync().ConfigureAwait(false);\n        step = await host.PostprocessAndCommitAsync().ConfigureAwait(false);',1)
if a.mutate=='skip-capture-guard':capture=capture.replace('if (!_owner.IsNativeConversationAdmissionCurrent(admission, out reason)) return false;',';',1)
if a.mutate=='capture-on-worker':capture=capture.replace('capture();','Task.Run(capture).GetAwaiter().GetResult();',1)
if a.mutate=='normalize-on-worker':slice=slice.replace('postprocessed = CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error)','postprocessed = Task.Run(() => CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error)).GetAwaiter().GetResult()',1)
if a.mutate=='swallow-capture-failure':capture=capture.replace('failure?.Throw();',';',1)
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@CAPTURE@@',capture).replace('@@SLICE@@',slice).replace('@@FIELDS@@','\n'.join(fields)).replace('@@PREPARE@@',prepare_signature)
assert '@@' not in code
out=HERE/'.generated'/(a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Coordinator.cs').write_text(coordinator,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0169;CS0649;CS0414</NoWarn></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=Path(os.environ.get('AF_DOTNET') or 'G:/AFMOD/.dotnet-sdk/dotnet.exe');env=os.environ.copy();env.update(DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'))
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
