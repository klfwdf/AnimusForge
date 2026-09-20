"""Compile the actual process-local Native owner; no game SDK or provider required."""
import argparse, os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
OWNER=ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs'
MUTATIONS={
    'value-identity': ('ReferenceEquals(Current, ticket)', 'Equals(Current, ticket)'),
    'release-new-slot': ('Interlocked.CompareExchange(ref _current, null, ticket)', 'Interlocked.Exchange(ref _current, null)'),
    'end-without-epoch': ('Interlocked.Increment(ref _conversationEpoch);', ';'),
    'release-ends-conversation': ('internal void Release(TTicket ticket) => Interlocked.CompareExchange(ref _current, null, ticket);', 'internal void Release(TTicket ticket) { Interlocked.CompareExchange(ref _current, null, ticket); Interlocked.Increment(ref _conversationEpoch); }'),
    'ignore-presentation': ('revision == PresentationRevision', 'true'),
    'begin-ends-conversation': ('internal long BeginPresentation() => Interlocked.Increment(ref _presentationRevision);', 'internal long BeginPresentation() { Interlocked.Increment(ref _conversationEpoch); return Interlocked.Increment(ref _presentationRevision); }'),
    'reserve-advances-presentation': ('internal void ReserveCaptured(TTicket ticket) => Interlocked.Exchange(ref _current, ticket);', 'internal void ReserveCaptured(TTicket ticket) { Interlocked.Exchange(ref _current, ticket); BeginPresentation(); }'),
}
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=MUTATIONS);args=p.parse_args()
s=OWNER.read_text(encoding='utf-8-sig')
if args.mutate:
    before,after=MUTATIONS[args.mutate];assert s.count(before)==1; s=s.replace(before,after)
out=HERE/'.generated'/(args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Owner.cs').write_text(s,encoding='utf-8')
(out/'Program.cs').write_text((HERE/'Program.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=Path(os.environ.get('AF_DOTNET') or ROOT/'local/dotnet/8.0.425/dotnet.exe')
env=os.environ.copy();env.update(DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
