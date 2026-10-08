"""Exact game lifecycle/retirement changes before previous whole-file proofs."""
from pathlib import Path
import hashlib,importlib.util,json,re,subprocess
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent;BASELINE='807bc5b9'
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
from remote_feature_delta import restore_remote_feature_delta
spec=importlib.util.spec_from_file_location('life_decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
PATHS=('ShoutBehavior.cs','CourierDeliveryBehavior.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs','SubModule.cs','MyBehavior.MemorySummaryMainThread.cs')
MOVED_DEPENDENCIES={
 'Refactor/Runtime/PendingOperationRegistry.cs':'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',
 'Refactor/Runtime/GameLifetimeCoordinator.cs':'src/AF.Foundation.Runtime/Lifecycle/GameLifetimeCoordinator.cs',
 'AfCampaignRuntimeLifecycle.cs':'src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs',
}
RUNNER_PATH_EDITS={
 'CourierDeliveryBehavior.DetachedPostprocess.cs':'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs',
 'CourierDeliveryBehavior.CampaignLifetime.cs':'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs',
 'CourierDeliveryBehavior.CommitDispatch.cs':'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs',
 'tools/ModuleFrameworkApiTests/run.py':'tests/AF.Contracts/ModuleFrameworkApiTests/run.py',
 'tools/ChannelCutoverBoundaryTests/run.py':'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py',
 'Refactor/Contracts/InteractionContracts.cs':'src/AF.Contracts/Internal/InteractionContracts.cs',
 'Refactor/Runtime/MemorySummaryDispatcher.cs':'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',
 'Refactor/Contracts/IMemorySummaryDispatchHost.cs':'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs',
 'Refactor/Runtime/InteractionResultCommitter.cs':'src/modules/AF.Module.Actions/Receipts/InteractionResultCommitter.cs',
 'Refactor/Runtime/PendingOperationRegistry.cs':'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',
 'Refactor/Runtime/GameLifetimeCoordinator.cs':'src/AF.Foundation.Runtime/Lifecycle/GameLifetimeCoordinator.cs',
 'AfCampaignRuntimeLifecycle.cs':'src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs',
}

def _restore_round2_current_paths(path, source):
    """Undo only reviewed current-file locator/import edits; retain original review hashes."""
    reviewed = {'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run.py': {'before_sha256': '056687f47d0812d022c7bc4fffaf9c3cf7537f7c91e7b112f3e8c02395b21ab1', 'edits': [('', 'import sys as _relocation_sys\n_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "tests"))\nfrom output_isolation import current_source_path\n'), ('    spec = importlib.util.spec_from_file_location(name, ROOT / path)\n', '    spec = importlib.util.spec_from_file_location(name, current_source_path(ROOT, path))\n'), ("def read(path): return (ROOT/path).read_text(encoding='utf-8-sig')\n", "def read(path): return (current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')\n")]}, 'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run_memory.py': {'before_sha256': 'bee94077ca75a97e112dcce95e7ce242086d4751ae29933d8bdd2605061c6abc', 'edits': [('', 'import sys as _relocation_sys\n_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "tests"))\nfrom output_isolation import current_source_path\n'), ("source=(ROOT/'MyBehavior.MemorySummaryMainThread.cs').read_text(encoding='utf-8-sig')\n", "source=(current_source_path(ROOT, 'MyBehavior.MemorySummaryMainThread.cs')).read_text(encoding='utf-8-sig')\n"), ("project=util.project(out,'MemoryRetirement',[out/'Boundary.cs',HERE/'MemoryRetirement.cs.txt',ROOT/'MyBehavior.CampaignLifetime.cs',ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',ROOT/'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs'],executable=True)\n", "project=util.project(out,'MemoryRetirement',[out/'Boundary.cs',HERE/'MemoryRetirement.cs.txt',current_source_path(ROOT, 'MyBehavior.CampaignLifetime.cs'),ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',ROOT/'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs'],executable=True)\n")]}}
    packet = reviewed.get(str(path).replace(chr(92), "/"))
    if packet is None:
        return source
    # Approved A4 changes affect only fresh output allocation and the SDK environment.
    # Restore those exact two runners before their original locator/whole-file guards.
    fresh_output = {'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run.py': {'current_sha256': 'f08023fe71337d9ac6d151d74f8b754a8482ab5c6dc898cb1880db589bb65851', 'reviewed_sha256': '63a2c7f695f1ae58f8f241271f188f4b8252ed53462ef62fe3880a0050dc826a', 'edits': [('from output_isolation import current_source_path\n', 'from output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment\n'), ("p.add_argument('--dotnet', default=os.environ.get('DOTNET_EXE', r'G:\\AFMOD\\.dotnet-sdk\\dotnet.exe'))\n", "p.add_argument('--dotnet', default=os.environ.get('DOTNET_EXE'))\np.add_argument('--run-root', type=Path)\n"), ('', "run_root = new_run_root(ROOT, 'game-lifetime', args.run_root)\ndotnet = resolve_dotnet(ROOT, args.dotnet)\n"), ("    out = HERE / '.generated' / name; out.mkdir(parents=True, exist_ok=True)\n", "    out = new_run_root(ROOT, 'game-lifetime', run_root / name)\n"), ("    status, log = util.run_dotnet(args.dotnet, ['run','--project',str(project),'-c','Release'], out)\n", "    result = subprocess.run([str(dotnet),'run','--project',str(project),'-c','Release'], cwd=out,\n        env=minimal_test_environment(dotnet,out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)\n    status, log = result.returncode, result.stdout + result.stderr\n")]}, 'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run_memory.py': {'current_sha256': '1644ccb9f416847e4794bf6745d0abcefce4de6a707652420be33b0bc3880fed', 'reviewed_sha256': 'b107577c746dbd6714605daa5d15d0fa520e3ff84ab50027379d61348882ed71', 'edits': [('from output_isolation import current_source_path\nimport argparse,importlib.util,os\n', 'from output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment\nimport argparse,importlib.util,os,subprocess\n'), ("p=argparse.ArgumentParser();p.add_argument('--mutate',action='store_true');a=p.parse_args()\nout=HERE/'.generated'/('memory-missing-retirement' if a.mutate else 'memory-current');out.mkdir(parents=True,exist_ok=True)\n", "p=argparse.ArgumentParser();p.add_argument('--mutate',action='store_true');p.add_argument('--run-root',type=Path);a=p.parse_args()\nrun_root=new_run_root(ROOT,'game-lifetime-memory',a.run_root)\nout=new_run_root(ROOT,'game-lifetime-memory',run_root/('memory-missing-retirement' if a.mutate else 'memory-current'))\n"), ("status,log=util.run_dotnet(os.environ.get('DOTNET_EXE',r'G:\\AFMOD\\.dotnet-sdk\\dotnet.exe'),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)\n", "dotnet=resolve_dotnet(ROOT)\nresult=subprocess.run([str(dotnet),'run','--project',str(project),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)\nstatus,log=result.returncode,result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)\n")]}}
    fresh = fresh_output.get(str(path).replace(chr(92), "/"))
    if fresh is not None and hashlib.sha256(source.encode()).hexdigest() != packet["before_sha256"]:
        if hashlib.sha256(source.encode()).hexdigest() != fresh["reviewed_sha256"]:
            assert hashlib.sha256(source.encode()).hexdigest() == fresh["current_sha256"], "Unreviewed lifetime fresh-output source: " + str(path)
            for before, after in reversed(fresh["edits"]):
                assert after and source.count(after) == 1, "Unreviewed lifetime fresh-output context: " + str(path)
                source = source.replace(after, before, 1)
            assert hashlib.sha256(source.encode()).hexdigest() == fresh["reviewed_sha256"], "Incomplete lifetime fresh-output inverse: " + str(path)
    if hashlib.sha256(source.encode()).hexdigest() == packet["before_sha256"]:
        return source
    for before, after in reversed(packet["edits"]):
        assert source.count(after) == 1, "Unreviewed round2 source locator: " + str(path)
        source = source.replace(after, before, 1)
    assert hashlib.sha256(source.encode()).hexdigest() == packet["before_sha256"], "Unreviewed round2 surrounding runner: " + str(path)
    return source

def restore_commit(source):
 # J09 adds authoritative action wrappers to this partial, not scheduler logic.
 # Compare every current scheduler/outcome declaration to the ORIGINAL reviewed
 # edits; do not refresh digests or compile unrelated game adapters as stubs.
 review=json.loads((ROOT/'tests/modules/AF.Module.Conversation/CourierCommitOutcomeTests/source-review.json').read_text(encoding='utf-8'))
 baseline=subprocess.check_output(['git','show',review['baseline']+':'+review['baselinePath']],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 assert hashlib.sha256(baseline.encode()).hexdigest()==review['originalNormalizedSha256']
 expected=baseline
 for before,after in review['edits']:
  assert expected.count(before)==1
  expected=expected.replace(before,after,1)
 packet=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['courierCommitExtraction']
 for signature in ['private static InteractionCommitResult CreateUnconfirmedCourierCommit(']+packet['signatures']:
  assert e.declaration(source,signature)==e.declaration(expected,signature), 'Unreviewed Courier scheduler/outcome declaration: '+signature
 return baseline

def restore_lifetime_dependency(path, source):
 if path == 'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/Bindings.cs.txt':
  # Reuse the approved B1_READ declaration inverse for this original lifetime
  # consumer; ec74 -> 1dec below remains the original, unchanged whole guard.
  from af2_terminal_migration_review import j17_packet
  chain = j17_packet()['independentLayers']['F3']['originalGuardChain']
  rows = [row for row in chain['stages'] if row['stage'] == 'B1_READ' and row['legacyPath'] == path]
  assert len(rows) == 1, 'Unreviewed lifetime dependency stage'
  row = rows[0]
  physical = [item for item in chain['physicalBindings'] if item['path'] == path]
  owners = chain['actualOwnerBindings']
  assert len(physical) == 1 and len(owners) == 1 and owners[0]['path'] == 'src/modules/AF.Module.Conversation/Channels/Native/ConversationMainThreadActionDrain.cs', 'Unreviewed lifetime physical binding set'
  for item in physical + owners:
   assert hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest() == item['rawSha256'], 'Unreviewed lifetime physical dependency: '+item['path']
  source = source.replace('\r\n', '\n')
  if hashlib.sha256(source.encode()).hexdigest() != row['targetSha256']:
   assert hashlib.sha256(source.encode()).hexdigest() == row['sourceSha256'], 'Unreviewed game lifetime binding fixture input'
   assert row['editOrder'] == 'FORWARD_LIST', 'Unreviewed lifetime context order'
   for edit in row['edits']:
    assert edit['symbols'] and edit['after'] and edit['after'] != source and source.count(edit['after']) == 1, 'Unreviewed lifetime dependency unique context'
    source = source.replace(edit['after'], edit['before'], 1)
   assert hashlib.sha256(source.encode()).hexdigest() == row['targetSha256'], 'Incomplete lifetime dependency inverse'
 source = restore_remote_feature_delta(path, source)
 if path=='tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/Bindings.cs.txt':
  reviewed=subprocess.check_output(['git','show','ec74d44d:'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
  assert source==reviewed, 'Unreviewed game lifetime binding fixture'
  # J17's fixture executes the actual ConversationRequestLifetime/lease and
  # ceremony-clear callback (not a replacement lifetime implementation).
  return subprocess.check_output(['git','show','1dec16b6:'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 # Reverse only the exact J17 cancellation/subscription additions. Current
 # cancellation behavior is executed by run_bindings; legacy hashes stay intact.
 if path=='tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/Harness.cs.txt':
  block=' // Integrated feature seams: registration runs inside the lifecycle try before CaptureOwners;\n // shutdown is after Stop. These no-op substitutes keep the real callbacks compilable.\n internal static class VengeanceRuntimeBridge {internal static void RegisterCampaign(IGameStarter starter){} internal static void Shutdown(){} }\n internal static class IntegratedModuleHost {internal static void RegisterCampaign(IGameStarter starter){} internal static void Shutdown(){} }\n'
  assert source.count(block)==1, 'Unreviewed integrated lifetime harness'
  source=source.replace(block,'',1)
 if path=='ShoutBehavior.CampaignLifetime.cs':
  block='    private AnimusForge.Refactor.Runtime.ConversationRequestLifetime _sceneRequestLifetime = new AnimusForge.Refactor.Runtime.ConversationRequestLifetime();\n\n    private void RetireChannelRequestLifetimes()\n    {\n        _nativeAdmissionOwner.Current?.Lifetime?.Retire();\n        _sceneRequestLifetime.Retire();\n    }\n\n'
  assert source.count(block)==1, 'Unreviewed J17 scene lifetime owner'
  source=source.replace(block,'',1)
  line='        RetireChannelRequestLifetimes();\n'
  assert source.count(line)==3, 'Unreviewed J17 retirement call count'
  source=source.replace(line,'')
  for line in ['        _sceneRequestLifetime = new AnimusForge.Refactor.Runtime.ConversationRequestLifetime();\n','        NativeConversationTurnHost.ClearCeremonyExecutionOrder(this);\n']:
   assert source.count(line)==1, 'Unreviewed J17 lifetime addition'
   source=source.replace(line,'',1)
 if path=='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs':
  start=source.index('    private readonly Dictionary<CourierSession, ConversationRequestLifetime>')
  end=source.index('    private readonly PendingOperationRegistry',start)
  original=subprocess.check_output(['git','show','cb045840:'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
  assert source[start:end]==original[start:original.index('    private readonly PendingOperationRegistry',start)], 'Unreviewed J17 courier lifetime owner'
  source=source[:start]+source[end:]
  line='        RetireCourierRequestLifetimes();\n'
  assert source.count(line)==2, 'Unreviewed J17 courier retirement call count'
  source=source.replace(line,'').replace('using System.Collections.Generic;\n','',1)
 return source

def restore_dequeue(source):
 spec=importlib.util.spec_from_file_location('dequeue_inverse',ROOT/'tests/modules/AF.Module.Conversation/CourierOwnerPhaseTests/source_parity.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
 return module.restore_j17_dequeue(source)

def old(path):return subprocess.check_output(['git','show',BASELINE+':'+('CourierDeliveryBehavior.DetachedPostprocess.cs' if path.endswith('CourierDeliveryBehavior.DetachedPostprocess.cs') else path)],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def expected(path):
 s=old(path)
 if path=='ShoutBehavior.cs':
  sig='private Task<T> RunNativeConversationMainThreadFuncAsync<T>(';before=e.declaration(s,sig);after=before.replace('if (func == null) return Task.FromResult(fallback);','long retirementVersion = _pendingMainThreadFunctions.Version;\n        if (func == null || !_pendingMainThreadFunctions.Accepting) return Task.FromResult(fallback);',1)
  after=after.replace('''        int state = 0;
        try''','''        int state = 0;
        bool Retire()
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;
            tcs.TrySetResult(fallback);
            return true;
        }
        IDisposable registration = _pendingMainThreadFunctions.Register(retirementVersion, () => Retire());
        if (registration == null) return tcs.Task;
        try''',1)
  after=after.replace('''        return AwaitNativeConversationMainThreadFuncAsync(tcs.Task, op, target, targetAgentIndex, () =>
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;
            tcs.TrySetResult(fallback);
            return true;
        });''','''        return AnimusForge.Refactor.Runtime.PendingOperationRegistry.AwaitRelease(
            AwaitNativeConversationMainThreadFuncAsync(tcs.Task, op, target, targetAgentIndex, Retire), registration);''',1)
  s=s.replace(before,after,1)
  for before,after in json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['nativeActionExactEdits']:
   assert s.count(before)==1;s=s.replace(before,after,1)
  pattern=r'(?m)^(\t+)(?:Action result;\n\1)?while \(_mainThreadActions.TryDequeue\(out (?:var _|result)\)\)\n\1\{\n\1\}'
  assert len(list(re.finditer(pattern,s)))==4;s=re.sub(pattern,lambda m:m.group(1)+'ResetPendingMainThreadFunctions();',s)
 elif path=='CourierDeliveryBehavior.cs':
  before='''\t\t\twhile (MainThreadActions.TryDequeue(out var _))
\t\t\t{
\t\t\t}''';assert s.count(before)==1;s=s.replace(before,'''\t\t\tif (Instance != null) Instance.ResetPendingOwnerPhases();
\t\t\telse while (MainThreadActions.TryDequeue(out var _)) { }''',1)
  packet=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['courierCommitExtraction']
  moved=[]
  for sig,(before,after) in zip(packet['signatures'],packet['edits']):
   assert e.declaration(s,sig)==before;s=s.replace('\t'+before+'\n\n','',1);moved.append('\t'+after)
  assert restore_commit((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig'))==packet['header']+'\n\n'.join(moved)+'\n}\n','Unreviewed Courier commit extraction'
 elif path=='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs':
  sig='private async Task<T> RunCourierOwnerPhaseAsync<T>(';before=e.declaration(s,sig);after=before.replace('cancellationToken.ThrowIfCancellationRequested();','long retirementVersion = _pendingOwnerPhases.Version;\n        cancellationToken.ThrowIfCancellationRequested();\n        if (!_pendingOwnerPhases.Accepting) throw new OperationCanceledException("Courier owner retired.");',1)
  after=after.replace('        bool mainThread = false;','''        using (IDisposable registration = _pendingOwnerPhases.Register(retirementVersion, () =>
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) == 0) completion.TrySetCanceled();
        }))
        {
        bool mainThread = false;''',1);after=after[:-1]+'    }\n    }';s=s.replace(before,after,1)
 elif path=='SubModule.cs':
  s=s.replace('\t\tRemoveMapButtonLayer();\n\t\tbase.OnGameEnd(game);','\t\tRemoveMapButtonLayer();\n\t\tAfCampaignRuntimeLifecycle.End(game);\n\t\tbase.OnGameEnd(game);',1).replace('\t\tModuleFrameworkRuntime.Shutdown();','\t\tAfCampaignRuntimeLifecycle.Stop();\n\t\tModuleFrameworkRuntime.Shutdown();',1)
  sig='protected override void InitializeGameStarter(';before=e.declaration(s,sig)
  after='''protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
\t{
\t\tCampaignGameStarter campaignStarter = starterObject as CampaignGameStarter;
\t\tif (campaignStarter != null) AfCampaignRuntimeLifecycle.Begin(game);
\t\ttry
\t\t{
\t\t\tModuleFrameworkRuntime.RegisterCampaign(starterObject);
\t\t\tif (campaignStarter != null) AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter);
\t\t}
\t\tcatch
\t\t{
\t\t\tif (campaignStarter != null)
\t\t\t{
\t\t\t\ttry { AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter); }
\t\t\t\tcatch (Exception) { } // Preserve the original registration failure.
\t\t\t\ttry { AfCampaignRuntimeLifecycle.End(game); }
\t\t\t\tcatch (Exception) { }
\t\t\t}
\t\t\tthrow;
\t\t}
\t}''';s=s.replace(before,after,1)
 elif path=='MyBehavior.MemorySummaryMainThread.cs':
  s=s.replace('    private MemorySummaryDispatcher _memorySummaryDispatcher;', '    private MemorySummaryDispatcher _memorySummaryDispatcher;\n    private int _campaignRuntimeRetired;',1).replace('            ReferenceEquals(Instance, _owner) && SaveRuntimeGuard.IsCurrentGeneration(generation);','            Volatile.Read(ref _owner._campaignRuntimeRetired) == 0\n            && ReferenceEquals(Instance, _owner) && SaveRuntimeGuard.IsCurrentGeneration(generation);',1)
 return s


def restore_relocation_runner(path, source):
 # Exact, reviewed fixture-only dependency/output wiring; legacy hashes remain unchanged.
 packet=json.loads((HERE/'f5-fixture-wiring.json').read_text(encoding='utf-8'))
 for edit in reversed(packet.get(path, [])):
  assert source.count(edit['after'])==1, 'Unreviewed game lifetime fixture wiring: '+path
  source=source.replace(edit['after'],edit['before'],1)
 source = _restore_round2_current_paths(path, source)
 if path != 'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run_bindings.py': return source
 edits=[("ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent\nspec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)\n", 'ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent\nimport sys\nsys.path.insert(0, str(ROOT / "tests"))\nfrom output_isolation import current_source_path\nspec=importlib.util.spec_from_file_location(\'util\',ROOT/\'tests/AF.Contracts/ModuleFrameworkApiTests/run.py\');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)\n'), ("out=HERE/'.generated/bindings';out.mkdir(parents=True,exist_ok=True)\nshout=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');courier=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig')\ncode=(HERE/'Bindings.cs.txt').read_text(encoding='utf-8-sig').replace('@@NATIVE@@',ex.declaration(shout,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')).replace('@@WAIT@@',ex.declaration(shout,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(')).replace('@@COURIER@@',ex.declaration(courier,'private async Task<T> RunCourierOwnerPhaseAsync<T>('))\n", "out=HERE/'.generated/bindings';out.mkdir(parents=True,exist_ok=True)\nshout=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig');courier=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig')\ncode=(HERE/'Bindings.cs.txt').read_text(encoding='utf-8-sig').replace('@@NATIVE@@',ex.declaration(shout,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')).replace('@@WAIT@@',ex.declaration(shout,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(')).replace('@@COURIER@@',ex.declaration(courier,'private async Task<T> RunCourierOwnerPhaseAsync<T>('))\n"), ('(out/\'Program.cs\').write_text(\'using System.Diagnostics;\\n\'+code,encoding=\'utf-8\');(out/\'NuGet.Config\').write_text(\'<configuration><packageSources><clear/></packageSources></configuration>\')\nfiles=[out/\'Program.cs\']+[ROOT/p for p in [\'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs\',\'PreprocessFormatException.cs\',\'MyBehavior.CampaignLifetime.cs\',\'ShoutBehavior.CampaignLifetime.cs\',\'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs\',\'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs\',\'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs\',\'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs\']]\nproject=util.project(out,\'Bindings\',files,executable=True);code,log=util.run_dotnet((os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[3] / "local/dotnet/8.0.425/dotnet.exe")),[\'run\',\'--project\',str(project),\'-c\',\'Release\'],out);(out/\'run.log\').write_text(log,encoding=\'utf-8\');print(log,end=\'\');raise SystemExit(code)\n', '(out/\'Program.cs\').write_text(\'using System.Diagnostics;\\n\'+code,encoding=\'utf-8\');(out/\'NuGet.Config\').write_text(\'<configuration><packageSources><clear/></packageSources></configuration>\')\nfiles=[out/\'Program.cs\']+[current_source_path(ROOT, p) for p in [\'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs\',\'PreprocessFormatException.cs\',\'MyBehavior.CampaignLifetime.cs\',\'ShoutBehavior.CampaignLifetime.cs\',\'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs\',\'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs\',\'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs\',\'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs\']]\nproject=util.project(out,\'Bindings\',files,executable=True);code,log=util.run_dotnet((os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[3] / "local/dotnet/8.0.425/dotnet.exe")),[\'run\',\'--project\',str(project),\'-c\',\'Release\'],out);(out/\'run.log\').write_text(log,encoding=\'utf-8\');print(log,end=\'\');raise SystemExit(code)\n')]
 for before, after in reversed(edits):
  assert source.count(after)==1, "Unreviewed source relocation runner delta: "+path
  source=source.replace(after,before,1)
 return source

def check_dependencies():
 data=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in data['dependencies'].items():
  source=restore_lifetime_dependency(p,restore_relocation_runner(p,(current_source_path(ROOT, MOVED_DEPENDENCIES.get(p,p))).read_text(encoding='utf-8-sig')))
  if p=='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs':source=restore_commit(source)
  if p in data.get('reviewedRunnerEdits',{}):
   for before,after in reversed(data['reviewedRunnerEdits'][p]):
    assert source.count(after)==1, 'Unreviewed lifetime runner adaptation: '+p
    source=source.replace(after,before,1)
  if p.startswith('tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run'):
   source=source.replace('parents[3]','parents[2]').replace('tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/','tools/GameLifetimeTests/')
   for historical,current in RUNNER_PATH_EDITS.items():
    source=source.replace(current,historical)
  assert hashlib.sha256(source.encode()).hexdigest()==h,'Unreviewed game lifetime dependency: '+p

spec_owner=importlib.util.spec_from_file_location('j07b_admission_inverse',ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/owner_extraction.py');owner_inverse=importlib.util.module_from_spec(spec_owner);spec_owner.loader.exec_module(owner_inverse)

from af2_terminal_migration_review import terminal_review

@terminal_review
def restore(path,source):
 # Shout's admission inverse already performs fixed F5 inverse -> remote inverse ->
 # original complete-turn proof. Do not apply either transform twice.
 if path == "ShoutBehavior.cs":
  try: source=owner_inverse.restore(path,source)
  except AssertionError as error: raise AssertionError("Unreviewed J07b source drift: "+path) from error
 else:
  # Courier Prompt owns its current-input binding and historical whole-host projection.
  if path != "CourierDeliveryBehavior.cs": source = restore_remote_feature_delta(path, source)
  source=owner_inverse.restore(path,source)
 if path=='ShoutBehavior.cs':
  # J03 exact inverse: d11eb572 added five request scopes; 2aa4edb7 moved
  # mission warmup seed capture to this call site. No other source drift is allowed.
  warmup_new='''\t\tPromptSemanticWarmupSeedBatch semanticWarmupSeeds = AIConfigHandler.CaptureGuardrailSemanticWarmupSeeds();
\t\tRagWarmupCoordinator.TryStartBackgroundWarmup("mission_start", semanticWarmupSeeds);
\t\tAIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start", semanticWarmupSeeds);'''
  warmup_old='''\t\tRagWarmupCoordinator.TryStartBackgroundWarmup("mission_start");
\t\tAIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start");'''
  assert source.count(warmup_new)==1,'Unreviewed J03 warmup source';source=source.replace(warmup_new,warmup_old,1)
  source,count=re.subn(r'(?m)^\t+using IDisposable guardrailScopeJ03 = AIConfigHandler\.BeginGuardrailRuntimeScope\(\);\n','',source)
  assert count==5,'Unreviewed J03 Shout scope count'
  # J04 exact inverse: 2a191526 collapsed one six-setter publish and four six-setter clears
  # into PromptRuntimeTargetBinding apply/clear. Only these exact blocks may differ.
  j04_apply_new='\t\t\tAIConfigHandler.ApplyGuardrailRuntimeTarget(MyBehavior.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex));\n'
  # bd2582aa introduced this exact capture/publication pair. Qualification behavior
  # is covered by CaptureEligibility; this inverse permits no other call-site drift.
  j06_apply_new=('\t\t\tPromptRuntimeTargetBinding runtimeTargetBinding = MyBehavior.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);\n'
   '\t\t\tAIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));\n')
  assert source.count(j06_apply_new)==1,'Unreviewed J06 Shout eligibility capture'
  source=source.replace(j06_apply_new,j04_apply_new,1)
  j04_apply_old=('\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetKingdom(targetKingdomId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetHero(targetHeroId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetCharacter(targetCharacterId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetTroop(targetCharacterId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank((targetHero == null && targetCharacter != null) ? (targetCharacter.IsSoldier ? "soldier" : "commoner") : "");\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(targetAgentIndex);\n')
  assert source.count(j04_apply_new)==1,'Unreviewed J04 Shout target apply';source=source.replace(j04_apply_new,j04_apply_old,1)
  def j04_clear(m):
   i=m.group(1);return ''.join(i+'AIConfigHandler.SetGuardrailRuntimeTarget'+x+'\n' for x in ('Kingdom("");','Hero("");','Character("");','Troop("");','UnnamedRank("");','AgentIndex(-1);'))
  source,count=re.subn(r'(?m)^(\t+)AIConfigHandler\.ClearGuardrailRuntimeTarget\(\);\n',j04_clear,source)
  assert count==4,'Unreviewed J04 Shout target clear count'
  j04_const_new='	public const string PersistentAdpDebtPostprocessRuleId = PromptPreprocessRuleIdAssembler.PersistentAdpDebtRuleId;'
  j04_const_old='	public const string PersistentAdpDebtPostprocessRuleId = "persistent_adp_debt";'
  assert source.count(j04_const_new)==1,'Unreviewed J04 Shout debt rule id';source=source.replace(j04_const_new,j04_const_old,1)
  # J04f exact inverse: the Native call site schedules the shared prompt build as three steps.
  j04f_new='\t\tMyBehavior.ShoutPromptContext ctx = await BuildNativePromptContextScheduledAsync(admission, nativeTargetLog, nativeTargetAgentIndex, runtimeGeneration, targetHero, targetCharacter, routingInput, extraFact, cultureId, npc.IsHero, preprocessExcludedRuleIds, weeklyPromptSnapshot).ConfigureAwait(false);\n'
  j04f_old=('\t\tTask<MyBehavior.ShoutPromptContext> nativePreprocessTask = RunNativeConversationBackgroundPreprocessAsync(\n'
   '\t\t\tnativeTargetLog,\n'
   '\t\t\tnativeTargetAgentIndex,\n'
   '\t\t\truntimeGeneration,\n'
   '\t\t\t() => MyBehavior.BuildShoutPromptContextForExternal(targetHero, routingInput, extraFact, cultureId, hasAnyHero: npc.IsHero, targetCharacter: targetCharacter, kingdomIdOverride: null, targetAgentIndex: nativeTargetAgentIndex, preprocessExcludedRuleIds: preprocessExcludedRuleIds, weeklyPromptSnapshot: weeklyPromptSnapshot));\n'
   '\t\tMyBehavior.ShoutPromptContext ctx = await AwaitNativeConversationBackgroundPreprocessAsync(nativePreprocessTask, nativeTargetLog, nativeTargetAgentIndex, runtimeGeneration).ConfigureAwait(false);\n')
  assert source.count(j04f_new)==1,'Unreviewed J04f Native prompt build call';source=source.replace(j04f_new,j04f_old,1)
 if path=='CourierDeliveryBehavior.cs':
  prompt_spec=importlib.util.spec_from_file_location('courier_prompt_inverse',ROOT/'tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/source_review.py');prompt=importlib.util.module_from_spec(prompt_spec);prompt_spec.loader.exec_module(prompt)
  source=prompt.restore(source)
 if path=='SubModule.cs':
  spec=importlib.util.spec_from_file_location('j02_host_inverse',ROOT/'tests/AF.GameAdapter.Bannerlord/HostCompositionTests/source_inverse.py');host=importlib.util.module_from_spec(spec);spec.loader.exec_module(host)
  source=host.restore_submodule(source)
 if path not in PATHS:return source
 if path.endswith('CourierDeliveryBehavior.DetachedPostprocess.cs'):
  signature='private async Task<T> RunCourierOwnerPhaseAsync<T>('
  assert restore_dequeue(e.declaration(source,signature))==e.declaration(expected(path),signature), 'Unreviewed game lifetime method'
  check_dependencies()
  return old(path)
 check_dependencies();assert source==expected(path),'Unreviewed game lifetime source change: '+path
 return old(path)

def restore_method(path,sig,method):
 actual=(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')
 if path.endswith('CourierDeliveryBehavior.DetachedPostprocess.cs'):
  assert restore_dequeue(e.declaration(actual,sig))==e.declaration(expected(path),sig), 'Unreviewed game lifetime method'
 else:restore(path,actual)
 assert method==e.declaration(actual,sig),'Unreviewed game lifetime method'
 return e.declaration(old(path),sig)
if __name__=='__main__':
 for p in PATHS:restore(p,(current_source_path(ROOT, p)).read_text(encoding='utf-8-sig'));print('PASS lifecycle exact inverse '+p)
