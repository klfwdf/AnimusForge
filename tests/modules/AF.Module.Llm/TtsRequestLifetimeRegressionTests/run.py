"""Compile the current, complete TtsEngine with deterministic network/game stubs and forbidden native APIs."""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import new_run_root
import argparse,hashlib,os,re,subprocess,importlib.util
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent

def main():
 p=argparse.ArgumentParser();p.add_argument('--dotnet',default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")));p.add_argument('--output-name',default='current');p.add_argument('--mutation',choices=['accept-after-publication','accept-under-lock','revive-dequeued','network-token-none','duplicate-terminal','late-legacy-event','match-agent-only','ignore-scene-epoch']);p.add_argument('--consumer-source',type=Path,default=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs');p.add_argument('--run-root',type=Path);a=p.parse_args()
 if not re.fullmatch(r'[A-Za-z0-9_-]+',a.output_name):p.error('invalid output name')
 out=new_run_root(ROOT, "TtsRequestLifetimeRegressionTests-"+a.output_name, a.run_root);out.mkdir(parents=True,exist_ok=True)
 raw=(ROOT/'src/modules/AF.Module.Llm/Tts/TtsEngine.cs').read_text(encoding='utf-8-sig')
 # Keep every production lifecycle method. Every P/Invoke fails before loading a native library.
 source,n=re.subn(r'\[DllImport\([^\n]+\)\]\s*internal static extern ([^;]+);',r'internal static \1 { throw new InvalidOperationException("Native API forbidden in regression harness"); }',raw)
 assert n==11,n
 if a.mutation=='accept-after-publication':
  source=source.replace('onAccepted?.Invoke(job.Request);','',1).replace('return queued;','onAccepted?.Invoke(job.Request); return queued;',1)
 if a.mutation=='accept-under-lock':source=source.replace('onAccepted?.Invoke(job.Request);','lock (_requestLock) { onAccepted?.Invoke(job.Request); }',1)
 if a.mutation=='revive-dequeued':source=source.replace('!job.Cancelled && !job.Request.IsCancellationRequested &&','',1)
 if a.mutation=='network-token-none':source=source.replace('.SynthesizeAsync(request, token, cancellationToken)','.SynthesizeAsync(request, token, CancellationToken.None)',1)
 if a.mutation=='duplicate-terminal':source=source.replace(' || job.TerminalPublished','',1)
 if a.mutation=='late-legacy-event':source=source.replace('if (!job.Request.IsCancellationRequested) { legacyEvent?.Invoke(); }','legacyEvent?.Invoke();',1).replace('InvokePlaybackSubscribers(OnPlaybackFinished, handler => handler(job.AgentIndex), job.Request)','InvokePlaybackSubscribers(OnPlaybackFinished, handler => handler(job.AgentIndex), job.Request, allowCancelled: true)')
 (out/'TtsEngine.cs').write_text(source,encoding='utf-8')
 (out/'VolcTtsGateway.cs').write_text((ROOT/'src/modules/AF.Module.Llm/Tts/VolcTtsGateway.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
 (out/'TtsTestFeedback.cs').write_text((ROOT/'src/AF.GameAdapter.Bannerlord/UI/Errors/TtsTestFeedback.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
 tick=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs').read_text(encoding='utf-8-sig')
 assert tick.count('TtsTestFeedback.OnApplicationTick()')==2, 'Both application tick paths must drain test feedback'
 settings_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs').read_text(encoding='utf-8-sig')
 button_start=settings_source.index('TestTtsVolcDedicatedVoice = delegate')
 button_end=settings_source.index('\n\t\tFetchMainModelList = delegate',button_start)
 (out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@TTS_TEST_BUTTON@@',settings_source[button_start:button_end]),encoding='utf-8')
 spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 # Read current owners, not the historical whole-repository projection (which binds unrelated modules).
 owner_paths=[ROOT/'src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs',ROOT/'src/AF.GameAdapter.Bannerlord/Scene/NativeConversationPlaybackWaitAdapter.cs']
 default_consumer=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'
 consumer=('\n'.join(path.read_text(encoding='utf-8-sig') for path in owner_paths) if a.consumer_source.resolve()==default_consumer.resolve() else a.consumer_source.read_text(encoding='utf-8-sig'))
 if a.mutation=='match-agent-only':consumer=consumer.replace('ReferenceEquals(_nativeConversationTtsPlaybackRequest, request)','_nativeConversationTtsPlaybackWaitAgentIndex == request.AgentIndex')
 if a.mutation=='ignore-scene-epoch':consumer=consumer.replace('&& owner.ConversationEpoch == _ports.ConversationEpoch()','')
 signatures=['private sealed class TtsPlaybackOwner','private void TrackTtsPlaybackRequest(','private bool IsTtsPlaybackRequestCurrent(','private bool PrepareTtsPlaybackRequest(','private bool IsActiveTtsPlaybackRequest(','private void RetireTtsPlaybackRequest(','private static long RegisterNativeConversationTtsPlaybackWait(','private static int ResolveNativeConversationTtsPlaybackWaitTimeoutMs(','private static bool CompleteNativeConversationTtsPlaybackWait(','private static bool IsNativeConversationTtsPlaybackWaitRequest(','private static bool IsNativeConversationTtsPlaybackWaitToken(','private static void CompleteNativeConversationTtsPlaybackWaitByToken(']
 # Only access modifiers and the native fixture's static storage differ; method bodies are verbatim.
 extracted='\n'.join(ex.declaration(consumer,x.replace('private static ', 'internal ').replace('private ', 'internal ')).replace(x.replace('private static ', 'internal ').replace('private ', 'internal '),x,1) for x in signatures)
 extracted+='\n'+'\n'.join(re.findall(r'internal readonly Dictionary<(?:long, TtsPlaybackOwner|int, long)>[^;]+;',consumer)).replace('internal readonly', 'private readonly')
 (out/'Consumer.cs').write_text((HERE/'ConsumerHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@METHODS@@',extracted),encoding='utf-8')
 (out/'consumer-source.txt').write_text('\n'.join(str(path) for path in (owner_paths if a.consumer_source.resolve()==default_consumer.resolve() else [a.consumer_source.resolve()]))+'\nSHA256='+hashlib.sha256(consumer.encode()).hexdigest(),encoding='utf-8')
 (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=os.environ.copy();env['DOTNET_ROOT']=str(Path(a.dotnet).parent);env['DOTNET_CLI_HOME']=str(out/'cli');env['DOTNET_CLI_TELEMETRY_OPTOUT']='1';env['DOTNET_NOLOGO']='1';env['DOTNET_CLI_UI_LANGUAGE']='en'
 r=subprocess.run([a.dotnet,'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
 log='TtsEngine SHA256='+hashlib.sha256(raw.encode()).hexdigest()+'\n'+r.stdout+r.stderr
 (out/'run.log').write_text(log,encoding='utf-8');print(log);return r.returncode
if __name__=='__main__':raise SystemExit(main())
