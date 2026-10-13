"""Replay the production compact-persona boundary with a real async yield and a game-thread pump."""
from pathlib import Path
import importlib.util, json, hashlib, subprocess, sys
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
source=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs'
text=source.read_text(encoding='utf-8-sig')
method=ex.declaration(text,'private async Task<bool> EnsureCompactReactionUnnamedPersonaAsync(')
consumer=ex.declaration(text,'internal async Task<SceneCompactReactionInput> CaptureCompactSceneReactionInputAsync(')
assert 'await EnsureCompactReactionUnnamedPersonaAsync(' in consumer
assert 'await ShoutUtils.EnsureUnnamedNpcPersonaGeneratedAsync(Mission.Current' not in consumer
assert 'epoch != Volatile.Read(ref _sceneConversationEpoch)' in consumer
harness=(base_path:=Path(__file__).parent/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@METHOD@@',method)
if '--mutate' in sys.argv:
 harness=harness.replace('if (!Current()) return false;', 'if (false) return false;')
out=new_run_root(ROOT,'scene-compact-persona',None);dotnet=resolve_dotnet(ROOT)
(out/'Program.cs').write_text(harness,encoding='utf-8')
waiter=ROOT/'src/modules/AF.Module.Conversation/Internal/PersonaGenerationWaiter.cs'
(out/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Compile Include="'+str(waiter)+'" Link="PersonaGenerationWaiter.cs" /></ItemGroup></Project>',encoding='utf-8')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Test.csproj')],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');(out/'receipt.json').write_text(json.dumps({'source_sha256':hashlib.sha256(method.encode()).hexdigest(),'exit':r.returncode,'game_objects':'fixtures; getters assert actual game-thread ownership','production':'unchanged helper and PersonaGenerationWaiter'},indent=2),encoding='utf-8')
print(r.stdout+r.stderr);print(out);sys.exit(r.returncode)
