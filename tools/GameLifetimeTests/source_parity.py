"""Exact game lifecycle/retirement changes before previous whole-file proofs."""
from pathlib import Path
import hashlib,importlib.util,json,re,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent;BASELINE='807bc5b9'
spec=importlib.util.spec_from_file_location('life_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
PATHS=('ShoutBehavior.cs','CourierDeliveryBehavior.cs','CourierDeliveryBehavior.DetachedPostprocess.cs','SubModule.cs','MyBehavior.MemorySummaryMainThread.cs')
def restore_commit(source):
 spec=importlib.util.spec_from_file_location('courier_outcome_inverse',ROOT/'tools/CourierCommitOutcomeTests/source_parity.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
 return module.restore(source)

def old(path):return subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
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
  assert restore_commit((ROOT/'CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig'))==packet['header']+'\n\n'.join(moved)+'\n}\n','Unreviewed Courier commit extraction'
 elif path=='CourierDeliveryBehavior.DetachedPostprocess.cs':
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

def check_dependencies():
 data=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in data['dependencies'].items():
  source=(ROOT/p).read_text(encoding='utf-8-sig')
  if p=='CourierDeliveryBehavior.CommitDispatch.cs':source=restore_commit(source)
  assert hashlib.sha256(source.encode()).hexdigest()==h,'Unreviewed game lifetime dependency: '+p

def restore(path,source):
 if path=='CourierDeliveryBehavior.cs':
  prompt_spec=importlib.util.spec_from_file_location('courier_prompt_inverse',ROOT/'tools/CourierPromptPreparationTests/source_review.py');prompt=importlib.util.module_from_spec(prompt_spec);prompt_spec.loader.exec_module(prompt)
  source=prompt.restore(source)
 if path not in PATHS:return source
 check_dependencies();assert source==expected(path),'Unreviewed game lifetime source change: '+path
 return old(path)

def restore_method(path,sig,method):
 actual=(ROOT/path).read_text(encoding='utf-8-sig');restore(path,actual)
 assert method==e.declaration(actual,sig),'Unreviewed game lifetime method'
 return e.declaration(old(path),sig)
if __name__=='__main__':
 for p in PATHS:restore(p,(ROOT/p).read_text(encoding='utf-8-sig'));print('PASS lifecycle exact inverse '+p)
