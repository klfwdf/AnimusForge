"""Compile actual deferred native scene-action callbacks against deterministic game substitutes."""
from pathlib import Path
import argparse, importlib.util, subprocess, sys
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);p.add_argument('--baseline',action='store_true');a=p.parse_args()
spec=importlib.util.spec_from_file_location('declarations',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
path='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'
s=(ROOT/path).read_text(encoding='utf-8-sig')
if a.baseline:
    s=subprocess.check_output(['git','show','4eaaeae37:'+path],cwd=ROOT).decode('utf-8-sig')
if a.baseline:
    signatures=['private sealed class PendingNativeSceneMechanismAction','private bool TryQueueNativeSceneMechanismActionAfterConversationExit(','private void ExecutePendingNativeSceneMechanismActionsAfterConversationExit(','private void ExecutePendingNativeSceneMechanismAction(','private void ClearPendingNativeSceneMechanismActions(','private void OnNativeConversationEnded(','private static void CaptureNativeIllustrationHistoryBoundary(']
    methods=[ex.declaration(s,x) for x in signatures]
else:
    controller_path='src/AF.GameAdapter.Bannerlord/Scene/SceneNativeMechanismController.cs'
    controller=(ROOT/controller_path).read_text(encoding='utf-8-sig')
    methods=[ex.declaration(s,'internal sealed class PendingNativeSceneMechanismAction')]
    signatures=['internal bool TryQueueNativeSceneMechanismActionAfterConversationExit(', 'internal void ExecutePendingNativeSceneMechanismActionsAfterConversationExit(', 'internal void TickPendingNativeSceneMechanismActions(', 'internal void ExecutePendingNativeSceneMechanismAction(', 'internal void ClearPendingNativeSceneMechanismActions(', 'internal void OnNativeConversationStarted(']
    methods += [ex.declaration(controller,x) for x in signatures]
    methods += [ex.declaration(s,x) for x in ['private void OnNativeConversationEnded(', 'private static void CaptureNativeIllustrationHistoryBoundary(']]
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@METHODS@@','\n'.join(methods))
if a.baseline:
    code=code.replace('@@BASELINE_TICK@@','private void TickPendingNativeSceneMechanismActions() { TryDrainNativeConversationQueuedActions("fixture_tick"); }')
else:
    code=code.replace('@@BASELINE_TICK@@','private ShoutBehavior _j17SceneNativeMechanismController => this;')
    code=code.replace('private static long _nativeSceneMechanismConversationGeneration;', 'private long _nativeSceneMechanismConversationGeneration;')
    code += '\ninternal static class SceneConversationHistoryOwner { internal static void CaptureNativeIllustrationHistoryBoundary() { } }\n'
# Verify actual consumers, not only isolated callbacks.
if not a.baseline:
    tick=ex.declaration(s,'public override void OnMissionTick(')
    assert '_parent.TickPendingNativeSceneMechanismActions();' in tick
    for sig in ['private void ResetInstanceTransientRuntimeForLoadedSave(', 'private void OnMissionStarted(', 'private void OnMissionEnded(']:
        assert 'ClearPendingNativeSceneMechanismActions(' in ex.declaration(s,sig),sig
    host_delete=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryDelete.cs').read_text(encoding='utf-8-sig')
    assert '_memoryHistoryCommit.RemoveMatchingDraftLine(' in ex.declaration(host_delete,'private bool RemoveMatchingDraftLine(')
    owner_delete=(ROOT/'src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
    assert '"dialogueui_delete", completeDaySnapshot: false' in ex.declaration(owner_delete,'internal bool RemoveMatchingDraftLine(')
    host_edit=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryEdit.cs').read_text(encoding='utf-8-sig')
    assert '"dialogueui_edit", completeDaySnapshot: false' in host_edit
    history_adapter=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/NativePendingHistoryApplicationAdapter.cs').read_text(encoding='utf-8-sig')
    history_sync=ex.declaration(history_adapter,'internal static void SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(')
    assert 'bool completeDaySnapshot = true' in history_sync and 'CaptureSceneSpeechTextOptions(), completeDaySnapshot)' in history_sync
    assert 'TickPendingNativeSceneMechanismActions() => _j17SceneNativeMechanismController.TickPendingNativeSceneMechanismActions()' in s
    assert 'OnNativeConversationStarted()' in ex.declaration(s,'private static void CaptureNativeIllustrationHistoryBoundary(')
    assert 'OnNativeConversationStarted()' not in ex.declaration(s,'private void OnNativeConversationEnded(')
    assert 'requiredConversationEpoch: 0' not in ex.declaration(controller,'internal void ExecutePendingNativeSceneMechanismAction(')
out=new_run_root(ROOT,'native-scene-exit-lifecycle',a.run_root)
(out/'Program.cs').write_text(code,encoding='utf-8')
import hashlib,json
(out/'source-fingerprints.json').write_text(json.dumps({str(i):hashlib.sha256(m.encode()).hexdigest() for i,m in enumerate(methods)},indent=2),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0169;CS0414</NoWarn></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
