from pathlib import Path
import argparse,subprocess,sys,importlib.util
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'));from output_isolation import minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--mutate',choices=['skip-trust','skip-duel','lose-promoted-reward']);p.add_argument('--patience-current',action='store_true');p.add_argument('--persona-read-current',action='store_true');p.add_argument('--api',choices=['1.3','1.4'],default='1.3');a=p.parse_args()
if a.patience_current:
    import json,hashlib,re
    from output_isolation import new_run_root,resolve_dotnet
    assert a.run_root and not a.mutate
    out=new_run_root(R,'patience-current',a.run_root)
    spec=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    receipts=[]
    def extract(path,sig):
        f=R/path;source=f.read_text(encoding='utf-8-sig');start=source.index(sig);arrow=source.find('=>',start);brace=source.find('{',start);body=source[start:source.index(';',arrow)+1] if arrow>=0 and (brace<0 or arrow<brace) else ex.declaration(source,sig)
        receipts.append(dict(path=path,signature=sig,rawSha256=hashlib.sha256(f.read_bytes()).hexdigest(),bodySha256=hashlib.sha256(body.encode('utf-8')).hexdigest()));return body
    path='src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs'
    sigs=['internal sealed class PatiencePromptCapturePorts','private static string CapturePatienceSceneInlineStateText(','internal static bool ShouldOmitClanRelationFromPrompt(','internal static string BuildDirectPatiencePrompt(','internal static bool TryGetHeroSceneStatus(','internal static bool TryGetHeroSceneInlineState(','internal static bool TryGetUnnamedSceneStatus(','internal static bool TryGetUnnamedSceneInlineState(','internal static string CapturePlayerPronoun(']
    text='using System;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters;internal static class PersonaIdentityPromptCaptureAdapter {internal static int NameReads;internal static string BuildPlayerPublicDisplayNameForPrompt(){NameReads++;return "Player";}'+''.join(extract(path,x) for x in sigs)+'}'
    (out/'CurrentCapture.cs').write_text(text,encoding='utf-8')
    dto=extract('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs','internal struct PatienceSnapshot')
    (out/'ActualDto.cs').write_text('namespace AnimusForge;internal class MyBehavior {'+dto+'}',encoding='utf-8')
    romance='src/modules/AF.Module.Social/Host/RomanceSystemBehavior.cs';romtext=(R/romance).read_text(encoding='utf-8-sig');table=re.search(r'private static readonly string\[\] LoveLevelTexts = [^;]+;',romtext).group()
    rrule='src/modules/AF.Module.Social/Romance/RomanceRelationshipOwner.cs'
    love='using System;namespace AnimusForge;internal static class RomanceSystemBehavior {'+table+extract(romance,'public static string GetPrivateLoveLevelText(')+extract(romance,'private static int ToLoveLevelIndex(')+'}internal static class RomanceRelationshipOwner {'+extract(rrule,'internal static int ToLoveLevelIndex(')+extract(rrule,'internal static int ClampLove(')+'}'
    (out/'CurrentLoveLevels.cs').write_text(love,encoding='utf-8')
    paths=['src/modules/AF.Module.Prompt/Composition/PatiencePromptProjectionComposer.cs','src/modules/AF.Module.Social/Patience/PatienceRules.cs']
    receipts += [dict(path=x,signature='FULL_CURRENT_SOURCE',rawSha256=hashlib.sha256((R/x).read_bytes()).hexdigest()) for x in paths]
    (out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('// @@PATIENCE_CURRENT@@')[1],encoding='utf-8')
    (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/x)+'" />' for x in paths)+'</ItemGroup></Project>',encoding='utf-8')
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    dotnet=resolve_dotnet(R);env=minimal_test_environment(dotnet,out);run=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release']+(['-p:DefineConstants=BANNERLORD_1_4_OR_GREATER'] if a.api=='1.4' else []),cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
    (out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8')
    after=[dict(path=x['path'],rawSha256=hashlib.sha256((R/x['path']).read_bytes()).hexdigest()) for x in receipts];assert all(x['path']==y['path'] and x['rawSha256']==y['rawSha256'] for x,y in zip(receipts,after))
    (out/'receipt.json').write_text(json.dumps(dict(api=a.api,exitCode=run.returncode,inputs=receipts,after=after,sourceClass='current exact complete Patience capture + whole sole projection/rules + exact original DTO and Love-level policy',controlledLeaves=['Hero/Clan handles and public player name scalar capture','D Hero/Unnamed snapshot read capability, not Social mutation or snapshot implementation','Reward level fallback unvisited throw, not a tested trust policy']),indent=2),encoding='utf-8')
    print(run.stdout+run.stderr,end='');sys.exit(run.returncode)
if a.persona_read_current:
    import json,hashlib
    from output_isolation import new_run_root,resolve_dotnet
    assert a.run_root and not a.mutate
    out=new_run_root(R,'persona-read-current',a.run_root)
    spec=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    app=R/'src/AF.GameAdapter.Bannerlord/Prompt/NpcPersonaGenerationApplicationAdapter.cs';profile=R/'src/modules/AF.Module.Persona/Profiles/PersonaProfileStateOwner.cs';reservation=R/'src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs';host=R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'
    sources=[app,profile,reservation,host];before={str(x.relative_to(R)):hashlib.sha256(x.read_bytes()).hexdigest() for x in sources}
    text=app.read_text(encoding='utf-8-sig')
    methods=[ex.declaration(text,sig) for sig in ['internal bool NeedsNpcPersonaGeneration(','internal bool IsNpcPersonaGenerationInFlight(','internal void GetNpcPersonaGenerationRuntimeState(','internal void GetNpcPersonaStrings(']]
    dto=ex.declaration(host.read_text(encoding='utf-8-sig'),'internal class NpcPersonaProfile')
    (out/'Actual.cs').write_text('using System;using TaleWorlds.CampaignSystem;using AnimusForge.Refactor.Runtime;namespace AnimusForge {internal partial class MyBehavior {'+dto+'}}namespace AnimusForge.Refactor.Adapters {internal sealed class NpcPersonaGenerationApplicationAdapter {private readonly AnimusForge.PersonaProfileStateOwner _profiles;private readonly NpcPersonaGenerationOwner _reservations;internal NpcPersonaGenerationApplicationAdapter(AnimusForge.PersonaProfileStateOwner p,NpcPersonaGenerationOwner r){_profiles=p;_reservations=r;}'+''.join(methods)+'}}',encoding='utf-8')
    (out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8').split('// @@PERSONA_READ_CURRENT@@')[1].split('// @@PATIENCE_CURRENT@@')[0],encoding='utf-8')
    files=[out/'Actual.cs',out/'Program.cs',profile,reservation]
    includes=''.join('<Compile Include="'+str(x)+'" />' for x in files)
    (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>disable</ImplicitUsings><DefineConstants>'+('BANNERLORD_1_4_OR_GREATER' if a.api=='1.4' else '')+'</DefineConstants></PropertyGroup><ItemGroup>'+includes+'</ItemGroup></Project>',encoding='utf-8')
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    dotnet=resolve_dotnet(R);run=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');after={str(x.relative_to(R)):hashlib.sha256(x.read_bytes()).hexdigest() for x in sources};assert before==after
    (out/'receipt.json').write_text(json.dumps({'api':a.api,'exitCode':run.returncode,'before':before,'after':after,'layer':'four current actual application read declarations + original DTO + whole profile/reservation owners; synthetic Hero and binding constructor only; not full generation application'},indent=2),encoding='utf-8')
    print(run.stdout+run.stderr,end='');raise SystemExit(run.returncode)
out=(a.run_root or R/'artifacts/af2-host-terminal-closeout/line-b/context-capture-adapter').resolve();out.mkdir(parents=True,exist_ok=True)
proof=subprocess.run([sys.executable,str(H/'source_move_proof.py')],cwd=R,capture_output=True,text=True)
print(proof.stdout+proof.stderr,end='')
if proof.returncode:raise SystemExit(proof.returncode)
base=R/'tests/modules/AF.Module.Prompt/Composition'
stubs=(base/'Stubs.cs').read_text(encoding='utf-8-sig').replace('public static class WorldEntityRetrievalService','public static partial class WorldEntityRetrievalService')
(out/'Stubs.cs').write_text(stubs,encoding='utf-8')
spec=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
weekly=ex.declaration((R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig'),'public sealed class WeeklyPromptSnapshot')
(out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8').split('// @@PERSONA_READ_CURRENT@@')[0].replace('@@WEEKLY@@',weekly),encoding='utf-8')
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
