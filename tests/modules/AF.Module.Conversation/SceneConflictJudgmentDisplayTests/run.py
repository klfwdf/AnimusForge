import argparse, hashlib, importlib.util, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec); spec.loader.exec_module(ex)
p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--mutate', choices=['cross-vm-replay', 'unowned-cleanup', 'retain-judgment'])
args = p.parse_args()
helper_path = ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationHelper.cs'
helper = helper_path.read_text(encoding='utf-8-sig')
taunt = (ROOT / 'src/modules/AF.Module.Taunt/Host/SceneTauntBehavior.cs').read_text(encoding='utf-8-sig')
methods = ['internal static void HandOffDeferredLordSceneDiplomacyToCriminalJudgmentForExternal(',
    'private bool TryHandOffDeferredLordSceneDiplomacyToCriminalJudgment(',
    'private void ClearPendingDeferredLordSceneDiplomacy(', 'private void OnGameMenuOpened(',
    'private void OnGameLoadFinished(', 'private void TryCommitDeferredLordSceneDiplomacyWhenBackOnWorldMap(']
fixture = '\n'.join(ex.declaration(taunt, selector) for selector in methods)
if args.mutate == 'cross-vm-replay':
    helper = helper.replace('Clear(); // Never replay another window\'s stream/typewriter on this VM.', '; // mutation: retain old stream')
if args.mutate == 'unowned-cleanup':
    start = helper.index('internal static void ClearForOwner(')
    before, body = helper[:start], helper[start:]
    assert body.count('owner != null && ReferenceEquals(_displayOwner, owner)') == 1
    helper = before + body.replace('owner != null && ReferenceEquals(_displayOwner, owner)', 'owner != null', 1)
if args.mutate == 'retain-judgment':
    assert fixture.count('ClearPendingDeferredLordSceneDiplomacy("criminal_judgment_handoff");') == 1
    fixture = fixture.replace('ClearPendingDeferredLordSceneDiplomacy("criminal_judgment_handoff");', '; // mutation: retain diplomacy')
# Routing assertions complement the runtime tests; never change storage keys or shared hostility.
my = (ROOT / 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
ended = ex.declaration(my, 'private void OnMemoryConversationEnded(')
assert ended.index('InvalidateNativeConversationAdmissionOnConversationEnd();') < ended.index('ConversationHelper.Clear();')
overlay = (ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs').read_text(encoding='utf-8-sig')
assert overlay.count('ConversationHelper.BeginStreaming(this);') == 2
assert 'ConversationHelper.ClearForOwner(this);' in ex.declaration(overlay, 'private void Close(bool silent)')
presentation = (ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Presentation.cs').read_text(encoding='utf-8-sig')
assert 'ConversationHelper.ClearForOwner(this);' in ex.declaration(presentation, 'private void RetireStaleSubmissionPresentation(')
assert 'ConversationHelper.EndStreaming(this);' in ex.declaration(presentation, 'private bool CompleteNativeSubmissionPresentation(')
start = taunt.index('if (flag2 && currentSettlement != null && currentSettlement.IsTown)')
branch = taunt[start:taunt.index('Mission.Current.EndMission();', start)]
assert branch.index('HandOffDeferredLordSceneDiplomacyToCriminalJudgmentForExternal(currentSettlement);') < branch.index('SetNextMenu("town_inside_criminal")')
assert 'OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);' in taunt
expected_keys = ['_sceneTauntPendingDeferredLordSceneDiplomacy_v1', '_sceneTauntPendingDeferredLordSceneTargetHeroId_v1',
    '_sceneTauntPendingDeferredLordSceneTargetFactionId_v1', '_sceneTauntPendingDeferredLordSceneSettlementId_v1', '_sceneTauntPendingDeferredLordSceneReason_v1']
assert all(taunt.count('SyncData("' + key + '"') == 1 for key in expected_keys)
out = new_run_root(ROOT, 'SceneConflictJudgmentDisplayTests', args.run_root)
for name, text in [('Helper.cs', helper), ('Stubs.cs', (HERE / 'Stubs.cs.txt').read_text(encoding='utf-8')),
    ('Program.cs', (HERE / 'Program.cs').read_text(encoding='utf-8')),
    ('Host.cs', 'using System;\nnamespace AnimusForge;\ninternal sealed partial class SceneTauntBehavior {\n' + fixture + '\n}'),
    ('Rules.cs', (ROOT / 'src/modules/AF.Module.Taunt/SceneTauntJudgmentHandoffRules.cs').read_text(encoding='utf-8'))]:
    (out / name).write_text(text, encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
# Compile the actual current presentation partial and queue/identity methods, not historical projections.
(out / 'Presentation.cs').write_text(presentation, encoding='utf-8')
ui_methods = '\n'.join(ex.declaration(overlay, selector) for selector in [
    'private bool IsSubmitGenerationActive(', 'private bool IsSubmitGenerationCurrent(',
    'private void RunOnMainThread(', 'private void ProcessMainThreadActions('])
(out / 'UiMethods.cs').write_text('using System;\nusing System.Threading;\nnamespace AnimusForge;\npublic sealed partial class AnimusForgeNativeConversationOverlay {\n' + ui_methods + '\n}', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
r = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'],
    cwd=ROOT, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=150)
log = 'helperSha256=' + hashlib.sha256(helper.encode()).hexdigest() + ' hostSha256=' + hashlib.sha256(fixture.encode()).hexdigest() + ' mutation=' + str(args.mutate) + '\n' + r.stdout + r.stderr
(out / 'run.log').write_text(log, encoding='utf-8'); print(log)
raise SystemExit(r.returncode)
