from pathlib import Path
import argparse,subprocess,sys,importlib.util
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'));from output_isolation import minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--mutate',choices=['skip-trust','skip-duel','lose-promoted-reward']);a=p.parse_args()
out=(a.run_root or R/'artifacts/af2-host-terminal-closeout/line-b/context-capture-adapter').resolve();out.mkdir(parents=True,exist_ok=True)
proof=subprocess.run([sys.executable,str(H/'source_move_proof.py')],cwd=R,capture_output=True,text=True)
print(proof.stdout+proof.stderr,end='')
if proof.returncode:raise SystemExit(proof.returncode)
base=R/'tests/modules/AF.Module.Prompt/Composition'
stubs=(base/'Stubs.cs').read_text(encoding='utf-8-sig').replace('public static class WorldEntityRetrievalService','public static partial class WorldEntityRetrievalService')
(out/'Stubs.cs').write_text(stubs,encoding='utf-8')
spec=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
weekly=ex.declaration((R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig'),'public sealed class WeeklyPromptSnapshot')
(out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@WEEKLY@@',weekly),encoding='utf-8')
adapter=(R/'src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs').read_text(encoding='utf-8')
mutations={'skip-trust':('if (relationshipPlan.CaptureTrust)','if (false)'), 'skip-duel':('targetHero != null && DuelBehavior.TryConsumeLastDuelResult','false && DuelBehavior.TryConsumeLastDuelResult'), 'lose-promoted-reward':('ports.BuildTriggeredRules(contextFlags)','ports.BuildTriggeredRules(default(PromptContextFlags))')}
if a.mutate:
    old,new=mutations[a.mutate];assert adapter.count(old)==1;adapter=adapter.replace(old,new,1)
(out/'Adapter.cs').write_text(adapter,encoding='utf-8')
project=(base/'PromptCompositionTests.csproj').read_text(encoding='utf-8-sig').replace('../../../../',str(R).replace('\\','/')+'/').replace('<Compile Include="Stubs.cs" />','<Compile Include="Stubs.cs" /><Compile Include="Adapter.cs" />')
(out/'Tests.csproj').write_text(project,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=R/'local/dotnet/8.0.425/dotnet.exe'
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='');raise SystemExit(r.returncode)
