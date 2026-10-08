"""Compile the actual Native operation claim; no game SDK or provider required."""
import argparse, os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
import sys
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
OWNER=ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs'
MUTATIONS={
    'start-without-claim': ('internal bool TryStart() => Interlocked.CompareExchange(ref _state,\n        (int)NativeConversationDispatchState.Started,\n        (int)NativeConversationDispatchState.Queued) == (int)NativeConversationDispatchState.Queued;', 'internal bool TryStart() => _state == (int)NativeConversationDispatchState.Queued;'),
    'expire-started': ('Interlocked.CompareExchange(ref _state,\n        (int)NativeConversationDispatchState.ExpiredBeforeStart,\n        (int)NativeConversationDispatchState.Queued) == (int)NativeConversationDispatchState.Queued', 'Interlocked.Exchange(ref _state, (int)NativeConversationDispatchState.ExpiredBeforeStart) != (int)NativeConversationDispatchState.ExpiredBeforeStart'),
    'expire-without-claim': ('internal bool TryExpireBeforeStart() => Interlocked.CompareExchange(ref _state,\n        (int)NativeConversationDispatchState.ExpiredBeforeStart,\n        (int)NativeConversationDispatchState.Queued) == (int)NativeConversationDispatchState.Queued;', 'internal bool TryExpireBeforeStart() => _state == (int)NativeConversationDispatchState.Queued;'),
    'allow-expired-start': ('internal bool TryStart() => Interlocked.CompareExchange(ref _state,\n        (int)NativeConversationDispatchState.Started,\n        (int)NativeConversationDispatchState.Queued) == (int)NativeConversationDispatchState.Queued;', 'internal bool TryStart() => Interlocked.Exchange(ref _state, (int)NativeConversationDispatchState.Started) != (int)NativeConversationDispatchState.Started;'),
}

p=argparse.ArgumentParser();p.add_argument('--mutate',choices=MUTATIONS);p.add_argument('--run-root',type=Path);args=p.parse_args()
s=OWNER.read_text(encoding='utf-8-sig')
if args.mutate:
    before,after=MUTATIONS[args.mutate];assert s.count(before)==1; s=s.replace(before,after)
out=new_run_root(ROOT,'native-dispatch-claim',args.run_root)
(out/'Owner.cs').write_text(s,encoding='utf-8')
(out/'Program.cs').write_text((HERE/'Program.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT)
env=minimal_test_environment(dotnet,out)
import json,hashlib
(out/'generated-inputs-before-build.json').write_text(json.dumps({str(f.name):hashlib.sha256(f.read_bytes()).hexdigest() for f in out.iterdir() if f.is_file() and f.suffix in ('.cs','.csproj','.Config')},indent=2),encoding='utf-8')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
