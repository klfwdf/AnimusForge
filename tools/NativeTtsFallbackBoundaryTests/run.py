from __future__ import annotations
import argparse, hashlib, importlib.util, os, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
ap=argparse.ArgumentParser()
ap.add_argument('--schedule-source-ref')
ap.add_argument('--mutate',choices=['drop-lifetime','drop-instance','drop-atomic-wait'])
ap.add_argument('--output-name',default='current')
args=ap.parse_args()
if not args.output_name.replace('-','').replace('_','').isalnum(): ap.error('invalid output name')
sys.stdout.reconfigure(encoding='utf-8')
current=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8')
schedule=ex.declaration(ex.source('ShoutBehavior.cs',args.schedule_source_ref),'private static void ScheduleNativeConversationTypewriterPlaybackFallback(')
if args.mutate:
    before,after={
        'drop-lifetime':('if (!owner.IsTtsPlaybackRequestCurrent(request)) { return; }','if (false) { return; }'),
        'drop-instance':('if (!ReferenceEquals(CurrentInstance, owner)) { return; }','if (false) { return; }'),
        'drop-atomic-wait':('lock (_nativeConversationTtsPlaybackWaitLock)','lock (new object())'),
    }[args.mutate]
    if before not in schedule: raise ValueError('mutation anchor absent')
    schedule=schedule.replace(before,after)
methods=[schedule]+[ex.declaration(current,marker) for marker in [
    'private sealed class TtsPlaybackOwner',
    'private void TrackTtsPlaybackRequest(',
    'private bool IsTtsPlaybackRequestCurrent(',
    'private static long RegisterNativeConversationTtsPlaybackWait(',
    'private static bool IsNativeConversationTtsPlaybackWaitToken(']]
request=ex.declaration((ROOT/'TtsEngine.cs').read_text(encoding='utf-8'),'internal sealed class PlaybackRequest')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@METHODS@@','\n\n'.join(methods)).replace('@@REQUEST@@',request)
out=HERE/'.generated'/args.output_name;out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=ROOT.parent/'.dotnet-sdk/dotnet.exe';env=os.environ.copy();env['DOTNET_ROOT']=str(dotnet.parent);env['DOTNET_CLI_HOME']=str(out/'cli');env['DOTNET_NOLOGO']='1';env['DOTNET_CLI_TELEMETRY_OPTOUT']='1'
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
log='scheduleRef='+str(args.schedule_source_ref or 'working-tree')+' mutation='+str(args.mutate or 'none')+' scheduleSha256='+hashlib.sha256(schedule.encode()).hexdigest()+'\n'+r.stdout+r.stderr
(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
