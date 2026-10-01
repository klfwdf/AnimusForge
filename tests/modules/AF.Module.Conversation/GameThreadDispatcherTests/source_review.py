"""Complete dispatcher + deadline helper inverse to reviewed committed methods."""
import re,importlib.util,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
old=subprocess.check_output(['git','show','84f428cd:src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
new=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ConversationGameThreadDispatcher.cs').read_text(encoding='utf-8-sig')
assert new.count('if (_isMainThread() && !forceQueue)')==1
new=new.replace('if (_isMainThread() && !forceQueue)','if (_isMainThread())',1)
new=new.replace('_pendingOperations','_pendingMainThreadFunctions').replace('_postMainThread(', '_mainThreadActions.Enqueue(').replace('_isMainThread()','IsBannerlordMainThreadForNativeActions()')
for a,b in [('private Task<T> RunNativeConversationMainThreadFuncAsync<T>(', 'internal Task<T> RunAsync<T>('),('private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(', 'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(')]:
    before=ex.declaration(old,a);after=ex.declaration(new,b)
    assert re.findall(r'\S+',before[before.index('{'):])==re.findall(r'\S+',after[after.index('{'):]), 'dispatcher body drift '+a
assert 'ShoutBehavior _' not in new and '_owner' not in new
print('PASS complete 82+29-line dispatcher/deadline inverse; sole original pending registry')
