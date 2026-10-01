from pathlib import Path
import sys,re,json,argparse,importlib.util,subprocess,hashlib
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment,new_run_root
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);p.add_argument('--mutate',choices=['ignore_generation','equipment_sort','known_player_identity']);a=p.parse_args();out=new_run_root(R,'PersonaIntro',a.run_root)
my=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig');scene=(R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
ports=(R/'src/AF.GameAdapter.Bannerlord/Composition/PersonaIntroLivePorts.cs').read_text(encoding='utf-8-sig')
expected=json.loads((H/'TerminalConsumers.json').read_text(encoding='utf-8'))
forward={'my':[],'scene':[]};moved_names={'my':set(),'scene':set()}
for item in expected:
    host=my if item['host']=='my' else scene;assert host.count(item['exact'])==1,item['symbol']
    forward[item['host']].append(item['exact']);moved_names[item['host']].add(re.search(r'\b(\w+)\(',item['exact']).group(1))
def leaves(text,host):
    result=[]
    for m in re.finditer(r'internal delegate ([^\n]+?) (\w+)Query\(([^\n]*)\);',text):
        ret,field,params=m.groups();name=field
        if name.startswith('BuildNpcInventorySummaryHeader'):name='BuildNpcInventorySummaryHeader'
        if name in moved_names[host]:continue
        if ret=='string':body='return Leaf.Query("'+field+'");'
        elif ret=='bool':body='return Leaf.Flag("'+field+'");'
        elif ret=='PartyBase':body='Leaf.Query("'+field+'");return Leaf.True.Contains("party")?new PartyBase():null;'
        elif ret=='IFaction':body='Leaf.Query("'+field+'");return new Faction{Name="perspective"};'
        elif ret=='void':
            outs=re.findall(r'out string (\w+)',params);assert len(outs)==2,field
            body='Leaf.Query("'+field+'");'+outs[0]+'=Leaf.Query("'+outs[0]+'");'+outs[1]+'=Leaf.Query("'+outs[1]+'");'
        else:raise AssertionError(ret)
        result.append('private static '+ret+' '+name+'('+params+'){'+body+'}')
    return '\n'.join(result)
myports=ports[ports.index('internal sealed class MyPersonaIntroLivePort'):ports.index('internal sealed class ScenePersonaIntroLivePort')]
sceneports=ports[ports.index('internal sealed class ScenePersonaIntroLivePort'):ports.index('internal sealed class HeroIdentityPromptLivePort')]
adapter=(R/'src/AF.GameAdapter.Bannerlord/Composition/PersonaEquipmentPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
wrappers=adapter[adapter.index(' private string BuildAgeBracketLabel('):adapter.index('internal SystemNpcIntroSnapshot CaptureSystemNpcIntro(')]
wrappers=wrappers[:wrappers.rfind('\n')]
program=(H/'GameLeafStubs.cs.txt').read_text(encoding='utf-8').replace('@@MY_LEAVES@@',leaves(myports,'my')).replace('@@SCENE_LEAVES@@',leaves(sceneports,'scene')).replace('@@MY_FORWARDINGS@@','\n'.join(forward['my'])).replace('@@SCENE_FORWARDINGS@@','\n'.join(forward['scene'])).replace('@@QUERY_WRAPPERS@@',wrappers).replace('@@LEGACY_INTROS@@',(H/'LegacyIntroBodies.cs.txt').read_text(encoding='utf-8')).replace('@@LEGACY_EQUIPMENT@@',(H/'LegacyEquipmentBodies.cs.txt').read_text(encoding='utf-8')).replace('@@LEGACY_ROLES@@',(H/'LegacyRoleLeaves.cs.txt').read_text(encoding='utf-8'))
# Hero overload is the unchanged three-line live identity capture leaf; no text rules duplicated.
hero_header=ex.declaration(scene,'private static string BuildNpcInventorySummaryHeader(Hero hero)')
start=program.index('public partial class ShoutBehavior {');program=program[:start]+program[start:].replace('public partial class ShoutBehavior {','public partial class ShoutBehavior {\n'+hero_header,1)
(out/'GameLeafStubs.cs').write_text(program,encoding='utf-8');(out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
paths=['src/AF.GameAdapter.Bannerlord/Composition/PersonaIntroLivePorts.cs','src/AF.GameAdapter.Bannerlord/Composition/PersonaEquipmentPromptCaptureAdapter.cs','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaEquipmentPromptCapture.cs','src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.PersonaEquipmentPromptCapture.cs','src/AF.GameAdapter.Bannerlord/Composition/EquipmentPromptCaptureAdapter.cs','src/modules/AF.Module.Prompt/Composition/PersonaIntroMessageComposer.cs','src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
compile_paths=[R/x for x in paths]
if a.mutate:
    index,before,after,count={'ignore_generation':(1,'snapshot != null && SaveRuntimeGuard.IsCurrentGeneration(snapshot.Generation)','snapshot != null',3),'equipment_sort':(6,'orderby x.Count descending','orderby x.Count ascending',1),'known_player_identity':(5,'s.knowsPlayerIdentity','true',1)}[a.mutate]
    source=compile_paths[index].read_text(encoding='utf-8-sig');assert source.count(before)==count
    (out/'MutatedOwner.cs').write_text(source.replace(before,after),encoding='utf-8');compile_paths.pop(index)
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(x)+'" />' for x in compile_paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
(out/'receipt.json').write_text(json.dumps({'exit_code':r.returncode,'sources':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in paths},'test_sha256':{x.name:hashlib.sha256(x.read_bytes()).hexdigest() for x in H.iterdir() if x.is_file()},'terminal_consumers':len(expected),'capture':'whole real adapters and typed host factories; only engine/domain-query leaves synthetic','live':'NOT_RUN'},indent=2),encoding='utf-8');sys.exit(r.returncode)
