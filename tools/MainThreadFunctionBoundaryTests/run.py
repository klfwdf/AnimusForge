"""Run the actual two shared scheduler declarations with a physical main-thread queue fixture."""
import argparse, importlib.util, subprocess, os, json, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(name):return subprocess.check_output(['git','show','613ac245:'+name],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/name).read_text(encoding='utf-8-sig')
s=read('ShoutBehavior.cs')
assert 'private const int NativeConversationMainThreadPreprocessTimeoutMs = 30000;' in s
run=ex.declaration(s,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')
wait=ex.declaration(s,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(')
# Keep every caller and all unrelated host behavior byte-equivalent to the reviewed baseline.
if not a.original:
 prior=subprocess.check_output(['git','show','613ac245:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 restored=s
 snapshot_spec=importlib.util.spec_from_file_location('snapshot_parity',ROOT/'tools/NativeHistorySnapshotTests/source_parity.py');snapshot_parity=importlib.util.module_from_spec(snapshot_spec);snapshot_spec.loader.exec_module(snapshot_parity)
 restored=snapshot_parity.restore_snapshot_source('ShoutBehavior.cs',restored)
 # Separately proven preparation capture: permit only the exact shared reviewed declaration SHA.
 signature='private async Task<string> SubmitNativeConversationTextInternalAsync('
 current=ex.declaration(restored,signature)
 review=json.loads((ROOT/'tools/TeamModulePortParityTests/reviewed-native-admission-deltas.json').read_text(encoding='utf-8'))
 expected=next(x['sha256'] for x in review['methods'] if x['path']=='ShoutBehavior.cs' and x['signature']==signature)
 live_submit=ex.declaration(s,signature)
 assert hashlib.sha256(live_submit.encode()).hexdigest()==expected and 'TeamModuleServices.' not in live_submit
 restored=restored.replace(current,ex.declaration(prior,signature),1)
 for signature in ['private Task<T> RunNativeConversationMainThreadFuncAsync<T>(', 'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(']:
  restored=restored.replace(ex.declaration(restored,signature),ex.declaration(prior,signature),1)
 assert restored==prior, 'Changes outside scheduler and separately reviewed preparation declarations'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@RUN@@',run).replace('@@WAIT@@',wait)
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
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'PreprocessFormatException.cs').write_text(read('PreprocessFormatException.cs'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
