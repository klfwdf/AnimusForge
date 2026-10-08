from pathlib import Path
import sys,re,json,argparse,importlib.util,subprocess,hashlib
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment,new_run_root
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);p.add_argument('--mutate',choices=['ignore_generation','equipment_sort','known_player_identity']);p.add_argument('--api',choices=['1.3','1.4'],default='1.3');p.add_argument('--roster-current',action='store_true');p.add_argument('--observer-current',action='store_true');p.add_argument('--identity-current',action='store_true');p.add_argument('--scene-list-current',action='store_true');p.add_argument('--packet-current',action='store_true');a=p.parse_args();out=new_run_root(R,'PersonaIntro',a.run_root)
if a.packet_current: assert a.observer_current and a.scene_list_current
if a.observer_current:
    assert not a.mutate and not a.roster_current
    sources=[R/'src/AF.GameAdapter.Bannerlord/Prompt/SceneAgentIdentityPromptCaptureAdapter.cs',R/'src/AF.GameAdapter.Bannerlord/Prompt/PromptRuleCaptureBannerlordAdapter.cs',R/'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs']
    before={str(x.relative_to(R)):hashlib.sha256(x.read_bytes()).hexdigest() for x in sources}
    text=sources[0].read_text(encoding='utf-8-sig')
    names=['BuildPlayerSceneIdentitySentenceForPrompt','ShouldForceDetailedPlayerIntroForObserver','DoesSceneObserverKnowPlayerIdentityForPrompt','GetSceneNpcPatienceNameForPrompt','GetSceneNpcHistoryNameForPrompt','BuildSceneObserverInlineStateForPrompt','ResolveNpcPerspectiveFactionForPlayerCrimePrompt','BuildPlayerFactionWarLineForPrompt','BuildPlayerVassalageRelationLineForPrompt','ResolveNpcPerspectiveKingdomForPrompt','BuildFactionNameForPrompt','BuildPlayerCompanionPartyRoleLabelForPrompt']
    bodies=[]
    for name in names:
        match=re.search(r'internal static (?:string|bool|IFaction|Kingdom) '+name+r'\(',text)
        assert match,name
        bodies.append(ex.declaration(text,match.group()))
    rule=ex.declaration(sources[1].read_text(encoding='utf-8-sig'),'internal static string ResolveRuleTargetKey(')
    identity=ex.declaration(sources[2].read_text(encoding='utf-8-sig'),'internal static string GetSceneNpcIdentityNameForPrompt(')
    prefix='using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.MountAndBlade;'
    (out/'ActualObserver.cs').write_text(prefix+'namespace AnimusForge.Refactor.Adapters {internal static partial class SceneAgentIdentityPromptCaptureAdapter {'+'\n'.join(bodies)+'}internal static class PromptRuleCaptureBannerlordAdapter {'+rule+'}}namespace AnimusForge {internal static class ConversationActionPostprocessOwner {'+identity+'}}',encoding='utf-8')
    family_path=R/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs'
    sources.append(family_path);before[str(family_path.relative_to(R))]=hashlib.sha256(family_path.read_bytes()).hexdigest()
    family=family_path.read_text(encoding='utf-8-sig')
    family_names=['GetClanMembersForPrompt','GetMarriageCandidateMaxAgeSettingForPrompt','GetMarriageCandidateMaxAgeGapSettingForPrompt','IsMarriageGenderCompatibleForPrompt','IsMarriageAgeCompatibleForPrompt','IsMarriageCandidateForPrompt','IsMarriagePoolCandidateForPrompt','GetMarriageCandidateGenderLabelForPrompt','GetNativeSpouseLabelForMarriagePrompt','BuildClanUnmarriedCandidatesForPrompt','BuildPlayerClanUnmarriedCandidatesForPrompt','TryResolveEquipmentContextForPrompt','TryGetAgentEquipmentItemForPrompt','TryGetHeroEquipmentItemForPrompt']
    family_bodies=[ex.declaration(family,re.search(r'internal static [^\n{};]+ '+name+r'\(',family).group()) for name in family_names]
    constants='\n'.join(re.findall(r'internal const int MarriageCandidate[^;]+;',family))
    (out/'ActualFamily.cs').write_text('using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Linq;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.Core;using TaleWorlds.MountAndBlade;namespace AnimusForge.Refactor.Adapters;internal static partial class PersonaIdentityPromptCaptureAdapter {'+constants+'\n'+'\n'.join(family_bodies)+'}',encoding='utf-8')
    naming_path=R/'src/AF.GameAdapter.Bannerlord/Prompt/NpcPersonaGenerationApplicationAdapter.cs'
    rules_path=R/'src/modules/AF.Module.Kingdom/Rebellion/RebellionNamingRules.cs'
    for path in [naming_path,rules_path]: sources.append(path);before[str(path.relative_to(R))]=hashlib.sha256(path.read_bytes()).hexdigest()
    naming=naming_path.read_text(encoding='utf-8-sig')
    naming_bodies=[ex.declaration(naming,'internal static string '+name+'(') for name in ['BuildRebelSettlementSummaryForNamingPrompt','BuildRebelFollowerSummaryForNamingPrompt']]
    rules=rules_path.read_text(encoding='utf-8-sig');rule_bodies=[]
    for name in ['SettlementLine','JoinSettlementSummary','FollowerLine','JoinFollowerSummary']:
        signature='internal static string '+name+'(';start=rules.index(signature);arrow=rules.find('=>',start);brace=rules.find('{',start)
        rule_bodies.append(rules[start:rules.index(';',arrow)+1] if arrow>=0 and arrow<brace else ex.declaration(rules,signature))
    (out/'ActualNamingSummary.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;namespace AnimusForge.Refactor.Adapters {internal static class RebelNamingPromptSummaryCaptureAdapter {'+'\n'.join(naming_bodies)+'}}namespace AnimusForge {internal static class RebellionNamingRules {'+'\n'.join(rule_bodies)+'}}',encoding='utf-8')
    if a.identity_current:
        composer_path=R/'src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs'
        pure_path=R/'src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs'
        for path in [composer_path,pure_path]: sources.append(path);before[str(path.relative_to(R))]=hashlib.sha256(path.read_bytes()).hexdigest()
        capture_names=['BuildPlayerIdentityInfoForPrompt','BuildNpcIdentityInfoForPrompt','BuildNpcInventorySummaryHeader','BuildFactionLineForPrompt','BuildSiegeStartNarrative','BuildNobleEtiquettePromptForHero']
        capture_bodies=[ex.declaration(family,'internal static string '+name+'(') for name in capture_names]
        snapshot=ex.declaration(family,'internal sealed class HeroIdentityInfoSnapshot')
        composer=composer_path.read_text(encoding='utf-8-sig');pure=pure_path.read_text(encoding='utf-8-sig')
        composers=[ex.declaration(composer,'internal static string '+name+'(') for name in ['ComposePlayerIdentityInfo','ComposeNpcIdentityInfo']]
        pure_bodies=[ex.declaration(pure,'internal static string '+name+'(') for name in ['BuildAgeBracketLabel','GetClanTierReputationLabel','BuildNpcInventorySummaryHeader']]
        (out/'ActualIdentity.cs').write_text('using System;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.CampaignSystem.Siege;using TaleWorlds.Core;using AnimusForge;using AnimusForge.Refactor.Adapters;namespace AnimusForge.Refactor.Adapters {internal static partial class PersonaIdentityPromptCaptureAdapter {'+snapshot+'\n'+'\n'.join(capture_bodies)+'}}namespace AnimusForge.Refactor.Modules {internal static class ScenePromptMessageProjectionComposer {'+'\n'.join(composers)+'}}namespace AnimusForge {internal static class PersonaIntroTextRules {'+'\n'.join(pure_bodies)+'}}',encoding='utf-8')
    (out/'GameLeafStubs.cs').write_text((H/'GameLeafStubs.cs.txt').read_text(encoding='utf-8-sig').split('// @@OBSERVER_CURRENT_LEAVES@@',1)[1].split('// @@IDENTITY_CURRENT_LEAVES@@',1)[0],encoding='utf-8')
    (out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('// @@OBSERVER_CURRENT_CASES@@',1)[1].split('// @@IDENTITY_CURRENT_CASES@@',1)[0],encoding='utf-8')
    if a.identity_current:
        with (out/'GameLeafStubs.cs').open('a',encoding='utf-8') as f:f.write((H/'GameLeafStubs.cs.txt').read_text(encoding='utf-8-sig').split('// @@IDENTITY_CURRENT_LEAVES@@',1)[1].split('// @@SCENE_LIST_CURRENT@@')[0])
        with (out/'Program.cs').open('a',encoding='utf-8') as f:f.write((H/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('// @@IDENTITY_CURRENT_CASES@@',1)[1].split('// @@SCENE_LIST_CURRENT@@')[0])
        program=(out/'Program.cs').read_text(encoding='utf-8').replace('Console.WriteLine("PASS current observer/player capture checks="','IdentityCases.Run();Console.WriteLine("PASS current observer/player capture checks="')
        (out/'Program.cs').write_text(program,encoding='utf-8')
    if a.scene_list_current:
        assert not a.identity_current
        def declare(path,signature):
            if path not in sources:sources.append(path);before[str(path.relative_to(R))]=hashlib.sha256(path.read_bytes()).hexdigest()
            return ex.declaration(path.read_text(encoding='utf-8-sig'),signature)
        agent_names=[('string','BuildNativeConversationNpcListBlockForPrompt'),('string','BuildSceneNpcListLineForPrompt'),('string','BuildPlayerRelationIdentitySuffixForNpcListLine'),('string','BuildPlayerDisplayNameForSceneNpcListIdentity'),('string','BuildDistanceToCurrentNpcForNpcListLine'),('List<NpcDataPacket>','FilterScenePresentNpcsForPrompt'),('string','BuildSceneNpcListLineWithRuntimeFactsForPrompt'),('string','BuildScenePresentNpcListBlockForPrompt'),('string','BuildSceneNonHeroNamingNoteForPrompt'),('bool','IsSameSceneNpcForPrompt'),('string','NormalizeSceneNpcMatchValue'),('bool','IsInspectionPrisonerNpcForPrompt'),('bool','IsInspectionPrisonerAgentForPrompt'),('Hero','ResolveHeroFromAgentIndex')]
        bodies=[declare(sources[0],'internal static '+kind+' '+name+'(') for kind,name in agent_names]
        hist=R/'src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs';comp=R/'src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs';post=sources[2]
        projection=[declare(comp,'internal static string '+name+'(') for name in ['BuildSceneNpcListLineForPrompt','BuildDistanceToCurrentNpcForNpcListLine','BuildSceneNonHeroNamingNoteForPrompt']]
        naming=[declare(post,'internal static string '+name+'(') for name in ['GetSceneNpcGivenNameForPrompt','GetSceneNpcListIdentityForPrompt']]
        (out/'ActualSceneList.cs').write_text('using System;using System.Linq;using System.Text;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.MountAndBlade;using AnimusForge;using AnimusForge.Refactor.Modules;namespace AnimusForge.Refactor.Adapters {internal static partial class SceneAgentIdentityPromptCaptureAdapter {'+''.join(bodies)+'}internal static class SceneHistoryPromptCaptureAdapter {'+declare(hist,'internal static string BuildPlayerMarriageFactForNpcListLine(')+'}}namespace AnimusForge.Refactor.Modules {internal static class ScenePromptMessageProjectionComposer {'+''.join(projection)+'}}',encoding='utf-8')
        actual=(out/'ActualObserver.cs').read_text(encoding='utf-8');actual=actual.replace('internal static class ConversationActionPostprocessOwner {','internal static partial class ConversationActionPostprocessOwner {');(out/'ActualObserver.cs').write_text(actual,encoding='utf-8')
        (out/'ActualSceneNames.cs').write_text('using System;namespace AnimusForge {internal static partial class ConversationActionPostprocessOwner {'+''.join(naming)+'}}',encoding='utf-8')
        leaves=(out/'GameLeafStubs.cs').read_text(encoding='utf-8').replace('public sealed class Hero {','public sealed class Hero {public bool IsPlayerCompanion;public Clan CompanionOf;public TaleWorlds.CampaignSystem.Party.MobileParty PartyBelongedToAsPrisoner;public static Dictionary<string,Hero> Lookup=new();public static Hero Find(string id)=>Lookup.GetValueOrDefault(id);')
        leaves=leaves.replace('public sealed class Kingdom:IFaction {','public sealed class Kingdom:IFaction {public static List<Kingdom> All=new();').replace('public sealed class CharacterObject {','public class BasicCharacterObject {public bool IsHero;}public sealed class CharacterObject:BasicCharacterObject {').replace('public sealed class Origin {','public class Origin {').replace('public object Character;','public TaleWorlds.CampaignSystem.BasicCharacterObject Character;')
        leaves=leaves.replace('internal sealed class NpcDataPacket {','internal sealed class NpcDataPacket {public bool IsHero,IsFemale,HasScenePosition;public float ScenePositionX,ScenePositionY,ScenePositionZ;public string RoleDesc,PromptGivenName,TroopId;').replace('public sealed class Settlement {','public sealed class Settlement {public TaleWorlds.CampaignSystem.Clan OwnerClan;').replace('public sealed class MobileParty {','public sealed class MobileParty {public TaleWorlds.CampaignSystem.Hero LeaderHero;')
        leaves=leaves.replace('public static string GetPromptIdentityName(NpcDataPacket npc)=>npc?.Name;','public static string GetPromptIdentityName(NpcDataPacket npc)=>npc?.Name;public static string GetPromptListName(NpcDataPacket npc)=>npc?.PromptGivenName;')
        leaves+='namespace AnimusForge {internal static class TroopInspectionMissionLogic {internal static bool TryResolveInspectionPrisoner(TaleWorlds.MountAndBlade.Agent a,out bool lord){lord=false;return false;}}internal sealed class PrisonerAgentOrigin:TaleWorlds.MountAndBlade.Origin {}internal static class SceneTradeBannerlordAdapter {internal static string GetPlayerDisplayNameForShout()=>"Player";}}namespace AnimusForge.Refactor.Adapters {internal static partial class PersonaIdentityPromptCaptureAdapter {internal static string BuildPlayerPublicDisplayNameForPrompt()=>PublicName;internal static string BuildPlayerPublicDisplayNameForPrompt(TaleWorlds.CampaignSystem.Hero hero)=>PublicName;}}'
        (out/'GameLeafStubs.cs').write_text(leaves,encoding='utf-8')
        if a.packet_current:
            roster=R/'src/AF.GameAdapter.Bannerlord/Prompt/SceneRosterPromptCaptureAdapter.cs'
            badge=declare(sources[0],'internal static string BuildPatienceBadgeForNpc(')
            packet=declare(roster,'internal static NpcDataPacket BuildSceneNpcDataFromLocationCharacter(')
            (out/'ActualPacket.cs').write_text('using System;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;using TaleWorlds.CampaignSystem.Settlements.Locations;namespace AnimusForge.Refactor.Adapters {internal static partial class SceneAgentIdentityPromptCaptureAdapter {'+badge+'}internal static class SceneRosterPromptCaptureAdapter {'+packet+'}}',encoding='utf-8')
            leaves=leaves.replace('public float Age=30;','public bool IsWanderer,IsNotable;public float Age=30;')
            leaves=leaves.replace('public sealed class CharacterObject:BasicCharacterObject {','public enum Occupation {None,Villager,Guard,Mercenary}public sealed class CharacterObject:BasicCharacterObject {public string Name;public bool IsFemale;public Occupation Occupation;')
            leaves=leaves.replace('public string RoleDesc,PromptGivenName,TroopId;','public string RoleDesc,PromptGivenName,TroopId,UnnamedRank,PersonalityDesc,BackgroundDesc;public float Age;')
            leaves=leaves.replace('internal static class MyBehavior {','internal static class MyBehavior {internal static TaleWorlds.CampaignSystem.Hero BadgeHero;internal static string BadgeKey;internal static int BadgeReads,PersonaReads;internal static string BuildScenePatienceBadgeForHeroExternal(TaleWorlds.CampaignSystem.Hero h){BadgeReads++;BadgeHero=h;return "hero-badge";}internal static string BuildScenePatienceBadgeForUnnamedExternal(string key,string name){BadgeReads++;BadgeKey=key+":"+name;return "unnamed-badge";}internal static void GetNpcPersonaForExternal(TaleWorlds.CampaignSystem.Hero h,out string personality,out string background){PersonaReads++;personality=" persona ";background=" background ";}')
            leaves=leaves.replace('internal static class ShoutUtils {','internal static class ShoutUtils {internal static NpcDataPacket Extracted;internal static int ExtractReads,NameReads;internal static NpcDataPacket ExtractNpcData(TaleWorlds.MountAndBlade.Agent a){ExtractReads++;return Extracted;}internal static void EnsurePromptNameFields(NpcDataPacket p){NameReads++;p.PromptGivenName=p.Name;}')
            leaves+='namespace TaleWorlds.CampaignSystem.Settlements.Locations {public sealed class LocationCharacter {public TaleWorlds.CampaignSystem.CharacterObject Character;}}namespace AnimusForge {internal static class SceneMovementController {internal static int Reads;internal static TaleWorlds.MountAndBlade.Agent Resolved;internal static TaleWorlds.MountAndBlade.Agent ResolveAgentForLocationCharacter(TaleWorlds.CampaignSystem.Settlements.Locations.LocationCharacter c){Reads++;return Resolved;}}}'
            (out/'GameLeafStubs.cs').write_text(leaves,encoding='utf-8')
        program=(out/'Program.cs').read_text(encoding='utf-8').replace('Console.WriteLine("PASS current observer/player capture checks="','SceneListCases.Run();Console.WriteLine("PASS current observer/player capture checks="')
        program+=(H/'Harness.cs.txt').read_text(encoding='utf-8').split('// @@SCENE_LIST_CURRENT@@')[1].split('// @@PACKET_CURRENT@@')[0]
        if a.packet_current:
            program=program.replace('SceneListCases.Run();','SceneListCases.Run();PacketCases.Run();')
            program+=(H/'Harness.cs.txt').read_text(encoding='utf-8').split('// @@PACKET_CURRENT@@')[1]
        (out/'Program.cs').write_text(program,encoding='utf-8')
    (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>',encoding='utf-8')
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    dotnet=R/'local/dotnet/8.0.425/dotnet.exe'
    result=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release']+(['-p:DefineConstants=BANNERLORD_1_4_OR_GREATER'] if a.api=='1.4' else []),cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'run.log').write_text(result.stdout+result.stderr,encoding='utf-8');print(result.stdout+result.stderr,end='')
    after={str(x.relative_to(R)):hashlib.sha256(x.read_bytes()).hexdigest() for x in sources}
    (out/'receipt.json').write_text(json.dumps({'api':a.api,'exitCode':result.returncode,'sourceClass':'current-exact-observer/identity-capture-and-readonly-snapshot/pure-composer' if a.identity_current else ('current-exact-scene-list-capture-nine/narrow-projection-three-and-observer' if a.scene_list_current else 'current-exact-observer-methods-and-actual-target-key/naming-policy'),'rawBefore':before,'rawAfter':after,'sameProduction':before==after,'syntheticLeaves':['Hero/Clan/Kingdom/Agent/Settlement scalar handles','existing PlayerNotoriety game query','ShoutUtils independent naming leaves','public identity display capture leaf','inline snapshot/projection external narrow capability','vassalage external narrow capability','existing faction relation capture leaf','PartyRole role-holder game API','Clan Lords/Heroes iterable handles','marriage setting scalar source','Mission/Settlement and Agent/Hero equipment scalar handles','A naming identity/settlement background/fortification capture capabilities; not A domain algorithms','identity faction/title/culture/equipment/role/kinship narrow captures controlled when identity-current; not whole factory','Reward inventory/value capture, mentioned entities and logger controlled','A siege faction display and tracked Hero list typed capture leaves; not full tracked Hero algorithm' ],'notRun':['whole PersonaEquipment factory','whole Courier','live game']},indent=2),encoding='utf-8');sys.exit(result.returncode)
if a.roster_current:
    assert not a.mutate, 'mutants belong to original persona mode'
    owner=R/'src/AF.GameAdapter.Bannerlord/Prompt/SceneRosterPromptCaptureAdapter.cs'
    (out/'GameLeafStubs.cs').write_text((H/'GameLeafStubs.cs.txt').read_text(encoding='utf-8-sig').split('// @@ROSTER_CURRENT_LEAVES@@',1)[1].split('// @@OBSERVER_CURRENT_LEAVES@@',1)[0],encoding='utf-8')
    (out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('// @@ROSTER_CURRENT_CASES@@',1)[1].split('// @@OBSERVER_CURRENT_CASES@@',1)[0],encoding='utf-8')
    (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Compile Include="'+str(owner)+'" /></ItemGroup></Project>',encoding='utf-8')
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    d=R/'local/dotnet/8.0.425/dotnet.exe'; before=hashlib.sha256(owner.read_bytes()).hexdigest()
    r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release']+(['-p:DefineConstants=BANNERLORD_1_4_OR_GREATER'] if a.api=='1.4' else []),cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
    (out/'receipt.json').write_text(json.dumps({'api':a.api,'exit_code':r.returncode,'sourceClass':'current-full-named-roster-capture-owner','source':str(owner.relative_to(R)),'rawBefore':before,'rawAfter':hashlib.sha256(owner.read_bytes()).hexdigest(),'sameProduction':before==hashlib.sha256(owner.read_bytes()).hexdigest(),'syntheticLeaves':['engine roster/hero/party','D troop-type leaf','Shout name-disambiguation leaf'],'notRun':['whole scene/native/courier consumer','live game','old saves']},indent=2),encoding='utf-8');sys.exit(r.returncode)
my=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig');scene=(R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
ports=(R/'src/AF.GameAdapter.Bannerlord/Composition/PersonaIntroLivePorts.cs').read_text(encoding='utf-8-sig')
expected=json.loads((H/'TerminalConsumers.json').read_text(encoding='utf-8'))
forward={'my':[],'scene':[]};moved_names={'my':set(),'scene':set()}
for item in expected:
    host=my if item['host']=='my' else scene
    current_exact=item['exact']
    if item['symbol']=='BuildHeroIdentityTitleForPrompt' and item['host']=='my':
        # Keep TerminalConsumers.json as the historical exact binding; this is the J17
        # current, explicitly named title forwarding, not a relaxed count assertion.
        current_exact='private static string BuildHeroIdentityTitleForPrompt(Hero hero) => PersonaIdentityPromptCaptureAdapter.BuildHeroIdentityTitleForPrompt(hero);'
    assert host.count(current_exact)==1,item['symbol']
    forward[item['host']].append(current_exact);moved_names[item['host']].add(re.search(r'\b(\w+)\(',current_exact).group(1))
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
program=(H/'GameLeafStubs.cs.txt').read_text(encoding='utf-8').split('// @@ROSTER_CURRENT_LEAVES@@',1)[0].replace('@@MY_LEAVES@@',leaves(myports,'my')).replace('@@SCENE_LEAVES@@',leaves(sceneports,'scene')).replace('@@MY_FORWARDINGS@@','\n'.join(forward['my'])).replace('@@SCENE_FORWARDINGS@@','\n'.join(forward['scene'])).replace('@@QUERY_WRAPPERS@@',wrappers).replace('@@LEGACY_INTROS@@',(H/'LegacyIntroBodies.cs.txt').read_text(encoding='utf-8')).replace('@@LEGACY_EQUIPMENT@@',(H/'LegacyEquipmentBodies.cs.txt').read_text(encoding='utf-8')).replace('@@LEGACY_ROLES@@',(H/'LegacyRoleLeaves.cs.txt').read_text(encoding='utf-8'))
# Hero overload is the unchanged three-line live identity capture leaf; no text rules duplicated.
hero_header=ex.declaration(scene,'private static string BuildNpcInventorySummaryHeader(Hero hero)')
start=program.index('public partial class ShoutBehavior {');program=program[:start]+program[start:].replace('public partial class ShoutBehavior {','public partial class ShoutBehavior {\n'+hero_header,1)
identity_source=(R/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
identity_title=re.search(r'^\s*internal static string BuildHeroIdentityTitleForPrompt\(Hero hero\) => [^\n]+;',identity_source,re.M).group(0)
identity_factory=re.search(r'^\s*internal static HeroIdentityPromptLivePort CreateHeroIdentityPromptLivePort\(\) => [^\n]+;',identity_source,re.M).group(0)
# Identity title/text authority remains real; only the existing ruled-kingdom
# engine query is controlled, as it was in GameLeafStubs before this forwarding moved.
equipment_capture='\n'.join(ex.declaration(identity_source, signature) for signature in ['internal static bool TryResolveEquipmentContextForPrompt(', 'internal static ItemObject TryGetAgentEquipmentItemForPrompt(', 'internal static ItemObject TryGetHeroEquipmentItemForPrompt('])
program+='\nnamespace AnimusForge.Refactor.Adapters { internal static class PersonaIdentityPromptCaptureAdapter {\n'+identity_title+'\n'+identity_factory+'\n'+equipment_capture+'\nprivate static bool TryResolveActiveKingdomRuledByHeroForPrompt(Hero hero,out Kingdom kingdom){Leaf.Query("ruled-kingdom");kingdom=hero.RuledKingdom;return kingdom!=null;}\n} }\n'
(out/'GameLeafStubs.cs').write_text(program,encoding='utf-8');(out/'Program.cs').write_text((H/'Harness.cs.txt').read_text(encoding='utf-8').split('// @@ROSTER_CURRENT_CASES@@',1)[0],encoding='utf-8')
paths=['src/AF.GameAdapter.Bannerlord/Composition/PersonaIntroLivePorts.cs','src/AF.GameAdapter.Bannerlord/Composition/PersonaEquipmentPromptCaptureAdapter.cs','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaEquipmentPromptCapture.cs','src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.PersonaEquipmentPromptCapture.cs','src/AF.GameAdapter.Bannerlord/Composition/EquipmentPromptCaptureAdapter.cs','src/modules/AF.Module.Prompt/Composition/PersonaIntroMessageComposer.cs','src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
compile_paths=[R/x for x in paths]
if a.mutate:
    index,before,after,count={'ignore_generation':(1,'snapshot != null && SaveRuntimeGuard.IsCurrentGeneration(snapshot.Generation)','snapshot != null',3),'equipment_sort':(6,'orderby x.Count descending','orderby x.Count ascending',1),'known_player_identity':(5,'s.knowsPlayerIdentity','true',1)}[a.mutate]
    source=compile_paths[index].read_text(encoding='utf-8-sig');assert source.count(before)==count
    (out/'MutatedOwner.cs').write_text(source.replace(before,after),encoding='utf-8');compile_paths.pop(index)
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(x)+'" />' for x in compile_paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release']+(['-p:DefineConstants=BANNERLORD_1_4_OR_GREATER'] if a.api=='1.4' else []),cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
(out/'receipt.json').write_text(json.dumps({'api':a.api,'exit_code':r.returncode,'sources':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in paths},'test_sha256':{x.name:hashlib.sha256(x.read_bytes()).hexdigest() for x in H.iterdir() if x.is_file()},'title_current_authority_sha256':hashlib.sha256((R/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').read_bytes()).hexdigest(),'title_current_binding':'explicit exact host forwarding and source-extracted current named title/factory; ruled-kingdom engine query controlled','terminal_consumers':len(expected),'capture':'whole real adapters and typed host factories; only engine/domain-query leaves synthetic','live':'NOT_RUN'},indent=2),encoding='utf-8');sys.exit(r.returncode)
