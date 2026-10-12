"""Compile the current, complete TtsEngine with deterministic network/game stubs and forbidden native APIs."""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import new_run_root, minimal_test_environment
import argparse,hashlib,os,re,subprocess,importlib.util,json
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent

def main():
 p=argparse.ArgumentParser();p.add_argument('--dotnet',default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")));p.add_argument('--output-name',default='current');p.add_argument('--mutation',choices=['accept-after-publication','accept-under-lock','revive-dequeued','network-token-none','duplicate-terminal','late-legacy-event','match-agent-only','ignore-scene-epoch']);p.add_argument('--consumer-source',type=Path,default=ROOT/'src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs');p.add_argument('--native-consumer-source',type=Path,default=ROOT/'src/AF.GameAdapter.Bannerlord/Scene/NativeConversationPlaybackWaitAdapter.cs');p.add_argument('--run-root',type=Path);a=p.parse_args()
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
 
 spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 settings_path=ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs'
 settings=settings_path.read_text(encoding='utf-8-sig')
 button=ex.declaration(settings,'TestTtsVolcDedicatedVoice = delegate')+';'
 helpers='\n'.join(ex.declaration(settings,x) for x in ['private static string Ui(', 'private static string UiFormat('])
 program=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@TTS_TEST_BUTTON@@',button).replace('@@UI_HELPERS@@',helpers)
 assert '@@' not in program
 (out/'Program.cs').write_text(program,encoding='utf-8')

 frozen_inputs={path:hashlib.sha256(path.read_bytes()).hexdigest() for path in [ROOT/'src/modules/AF.Module.Llm/Tts/TtsEngine.cs',a.consumer_source,a.native_consumer_source,ROOT/'src/AF.Persistence/AnimusForgeDataPaths.cs',ROOT/'src/modules/AF.Module.Llm/Protocol/LlmSourceTexts.cs',ROOT/'content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json',ROOT/'_deps_auto/Newtonsoft.Json.dll',HERE/'Harness.cs.txt',HERE/'ConsumerHarness.cs.txt']}
 consumer=a.consumer_source.read_text(encoding='utf-8-sig')
 native=a.native_consumer_source.read_text(encoding='utf-8-sig')
 if a.mutation=='match-agent-only':
  native,native_count=native.replace('ReferenceEquals(_nativeConversationTtsPlaybackRequest, request)','(_nativeConversationTtsPlaybackWaitAgentIndex == request.AgentIndex)'),native.count('ReferenceEquals(_nativeConversationTtsPlaybackRequest, request)')
  assert native_count==3,native_count
 if a.mutation=='ignore-scene-epoch':
  needle='&& owner.ConversationEpoch == _ports.ConversationEpoch()'
  assert consumer.count(needle)==1
  consumer=consumer.replace(needle,'')
 signatures=['internal SceneAudioLipSyncController(','internal void TrackTtsPlaybackRequest(','internal bool IsTtsPlaybackRequestCurrent(','internal bool PrepareTtsPlaybackRequest(','internal bool IsActiveTtsPlaybackRequest(','internal void RetireTtsPlaybackRequest(','internal sealed class TtsPlaybackOwner']
 extracted='\n'.join(ex.declaration(consumer,x) for x in signatures)
 fields=re.findall(r'internal readonly Dictionary<(?:long, TtsPlaybackOwner|int, long)>[^;]+;',consumer)
 assert len(fields)==2,fields
 extracted+='\n'+'\n'.join(fields)
 extracted='internal sealed class SceneAudioLipSyncController { private readonly SceneAudioLipSyncPort _ports; private volatile bool _retired; internal readonly object _requestLock=new object();\n'+extracted+'\n}'
 ports='\n'.join(ex.declaration(consumer,x) for x in ['internal sealed class SceneAudioLipSyncPort','internal sealed class SceneAudioFailureOutput'])
 ports+='\n'+'\n'.join(re.findall(r'internal delegate bool SceneLipSync(?:Agent|Index)Check[^;]+;',consumer))
 assert 'SceneLipSyncAgentCheck' in ports and 'SceneLipSyncIndexCheck' in ports
 harness=(HERE/'ConsumerHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@SCENE_OWNER@@',extracted).replace('@@SCENE_PORTS@@',ports)
 assert '@@' not in harness
 (out/'Consumer.cs').write_text(harness,encoding='utf-8')
 (out/'NativeConversationPlaybackWaitAdapter.cs').write_text(native,encoding='utf-8')
 (out/'consumer-source.json').write_text(json.dumps({'responsibility':'exact current Scene request lifecycle methods and complete Native wait adapter/ports; game and network leaves are controlled','inputs':{str(path.relative_to(ROOT)):sha for path,sha in frozen_inputs.items()},'consumerAssertions':harness.count('check("consumer:'),'mutation':a.mutation},indent=2),encoding='utf-8')
 assets=json.loads((ROOT/'tools/Coup.RuntimeProbe/obj/project.assets.json').read_text('utf-8-sig'))
 refdir=next(Path(x)/'microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2' for x in assets['packageFolders'] if (Path(x)/'microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2/mscorlib.dll').exists())
 from xml.sax.saxutils import escape
 links=[refdir/'Facades/netstandard.dll',ROOT/'_deps_auto/Newtonsoft.Json.dll',ROOT/'.tmp/build_check/1.3/TaleWorlds.Localization.dll',ROOT/'.tmp/build_check/1.3/TaleWorlds.Library.dll']
 refs='<Reference Include="System.Net.Http" />'+''.join('<Reference Include="'+x.stem+'"><HintPath>'+escape(str(x))+'</HintPath></Reference>' for x in links)
 sources=[ROOT/x for x in ['src/AF.Persistence/AnimusForgeDataPaths.cs','src/modules/AF.Module.Llm/Protocol/LlmSourceTexts.cs','src/modules/AF.Module.Llm/Tts/VolcTtsGateway.cs','src/modules/AF.Module.Llm/Tts/LlmUiTexts.cs','src/AF.GameAdapter.Bannerlord/UI/Errors/TtsTestFeedback.cs','src/AF.GameAdapter.Bannerlord/UI/Errors/TtsErrorUiText.cs','src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettingsUiLocalization.cs']]
 includes=''.join('<Compile Include="'+escape(str(x))+'" />' for x in sources)
 frozen_inputs.update({x:hashlib.sha256(x.read_bytes()).hexdigest() for x in sources+links+[settings_path]})
 (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><PlatformTarget>x64</PlatformTarget><LangVersion>10</LangVersion><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>'+escape(str(refdir))+'</FrameworkPathOverride></PropertyGroup><ItemGroup>'+refs+includes+'<EmbeddedResource Include="'+escape(str(ROOT/'content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json'))+'" LogicalName="AnimusForge.Llm.RuntimeSources.json" WithCulture="false" /></ItemGroup></Project>',encoding='utf-8')
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=minimal_test_environment(Path(a.dotnet),out)
 module=out/'module-fixture'/'AnimusForge';(module/'ModuleData').mkdir(parents=True)
 (module/'SubModule.xml').write_text('<Module><Id value="AnimusForge" /></Module>',encoding='utf-8')
 env['ANIMUSFORGE_DATA_ROOT']=str(module)
 build=subprocess.run([a.dotnet,'build',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
 (out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
 if build.returncode: print(build.stdout+build.stderr);return build.returncode
 r=subprocess.run([str(out/'bin/Release/net472/Tests.exe')],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
 assert all(hashlib.sha256(path.read_bytes()).hexdigest()==sha for path,sha in frozen_inputs.items()), 'consumer/source drift during probe'
 log='TtsEngine SHA256='+hashlib.sha256(raw.encode()).hexdigest()+'\n'+r.stdout+r.stderr
 (out/'run.log').write_text(log,encoding='utf-8');print(log);return r.returncode
if __name__=='__main__':raise SystemExit(main())
