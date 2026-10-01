"""Run the actual two shared scheduler declarations with a physical main-thread queue fixture."""
import argparse, importlib.util, subprocess, os, json, hashlib, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]; HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--original',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(name):return subprocess.check_output(['git','show','613ac245:'+name],cwd=ROOT).decode('utf-8-sig') if a.original else (current_source_path(ROOT, name)).read_text(encoding='utf-8-sig')
s=read('ShoutBehavior.cs')
assert 'private const int NativeConversationMainThreadPreprocessTimeoutMs = 30000;' in s
run=ex.declaration(s,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')
wait=ex.declaration(s,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(')
# J07/J10/J17 moved unrelated host responsibilities. Scope this scheduler
# proof to the exact two production declarations and the original lifetime edits;
# NativeTurn/ChannelPersona own preparation and algorithm coverage separately.
if not a.original:
 lifetime_spec=importlib.util.spec_from_file_location('scheduler_lifetime',ROOT/'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/source_parity.py');life=importlib.util.module_from_spec(lifetime_spec);lifetime_spec.loader.exec_module(life)
 expected=life.expected('ShoutBehavior.cs')
 for signature in ['private Task<T> RunNativeConversationMainThreadFuncAsync<T>(', 'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(']:
  assert ex.declaration(s,signature)==ex.declaration(expected,signature), 'Unreviewed scheduler declaration'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@RUN@@',run).replace('@@WAIT@@',wait)
if not a.original:
 code=code.replace('public sealed class ShoutBehavior\n{','public sealed class ShoutBehavior\n{\n    private readonly AnimusForge.Refactor.Runtime.PendingOperationRegistry _pendingMainThreadFunctions = new();',1)
mutations={
 'drop-claim':('if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;', 'if (false) return;'),
 'expire-started':('if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;', 'if (Interlocked.Exchange(ref state, 2) == 2) return false;'),
 'keep-expired-live':('if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;', 'if (Volatile.Read(ref state) != 0) return false;'),
 'failed-publication-live':('if (Interlocked.CompareExchange(ref state, 2, 0) == 0) tcs.TrySetResult(fallback);', 'tcs.TrySetResult(fallback);'),
 'swallow-format':('catch (PreprocessFormatException ex)\n            {\n                Observe(phase + "_exception", error: ex);\n                throw;\n            }', 'catch (PreprocessFormatException) { return fallback; }'),
 'diagnostic-throws':('// Optional diagnostics never own execution or completion.\n                return;', '// Mutated diagnostic.\n                throw;'),
 'forget-queued-result':('tcs.TrySetResult(Execute("mainthread"));', 'Execute("mainthread"); tcs.TrySetResult(fallback);'),
}
if a.mutate:
 old,new=mutations[a.mutate]; assert old in code, 'Mutation missed';code=code.replace(old,new,1)
out=new_run_root(ROOT,'main-thread-function-boundary',a.run_root)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'PreprocessFormatException.cs').write_text(read('PreprocessFormatException.cs'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>',encoding='utf-8')
if not a.original:(out/'PendingOperationRegistry.cs').write_text((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
