from pathlib import Path
import importlib.util, json, re, shutil, subprocess, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/localization/verification-20261011/gathering-gate-replay'
OUT.mkdir(parents=True,exist_ok=True)
base=ROOT/'content/modules/AF.Module.Diplomacy/ModuleData'
gather=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/Gathering'
raw=json.loads((base/'DiplomacyMiscCompatibility.json').read_text(encoding='utf-8-sig'))
cn=json.loads((base/'DiplomacyMiscPromptTexts.json').read_text(encoding='utf-8-sig'))
en=json.loads((base/'DiplomacyMiscPromptTexts.en.json').read_text(encoding='utf-8-sig'))
ui_cn={x.attrib['id']:x.attrib['text'] for x in ET.parse(base/'Languages/CNs/afdip_ui_strings-zh-CN.xml').iter('string')}
ui_en={x.attrib['id']:x.attrib['text'] for x in ET.parse(base/'Languages/afdip_ui_strings.xml').iter('string')}
before_manifest=ROOT/'artifacts/localization/verification-20261011/gathering-gate-before.json'
if before_manifest.exists():
    for row in json.loads(before_manifest.read_text(encoding='utf-8-sig')):
        path=ROOT/row['source'];snapshot=ROOT/row['backup']
        if path.suffix=='.json':
            old=json.loads(snapshot.read_text(encoding='utf-8-sig'));new=json.loads(path.read_text(encoding='utf-8-sig'))
            assert all(new.get(key)==value for key,value in old.items()),str(path)+': old key changed'
        elif path.suffix=='.xml':
            old={x.attrib['id']:x.attrib['text'] for x in ET.parse(snapshot).iter('string')}
            new={x.attrib['id']:x.attrib['text'] for x in ET.parse(path).iter('string')}
            changes={key for key,value in old.items() if new.get(key)!=value}
            assert changes <= ({'AF_DIP_UI_Gathering_SelectedGuestsMore'} if path.name=='afdip_ui_strings.xml' else set()),changes
for key in cn:
    if not key.startswith('Gathering_'): continue
    assert key in en,key
    assert sorted(re.findall(r'\{[A-Z_]+\}',cn[key]))==sorted(re.findall(r'\{[A-Z_]+\}',en[key])),key
    assert not re.search('[一-鿿]',re.sub(r'\[AFEF[^\]]+\]','',en[key])),key
for key in ui_en:
    if not key.startswith('AF_DIP_UI_Gathering_'):continue
    assert key in ui_cn,key
    assert sorted(re.findall(r'\{[A-Z_]+\}',ui_en[key]))==sorted(re.findall(r'\{[A-Z_]+\}',ui_cn[key])),key
for path in gather.glob('*.cs'):
    assert not re.search('[一-鿿]',path.read_text(encoding='utf-8')),str(path)

# Source-derived selector methods; production text readers/projection/UI use real DLLs.
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
source=(gather/'NobleGatheringBehavior.cs').read_text(encoding='utf-8-sig')
baseline=ROOT/'artifacts/localization/verification-20261011/gathering-gate-before/src__AF.GameAdapter.Bannerlord__Composition__Gathering__NobleGatheringBehavior.cs'
if baseline.exists():
    old_source=baseline.read_text(encoding='utf-8-sig')
    predicates=['private static bool IsHeroAdultAndSpawnedForGathering(', 'private bool IsHeroEligibleForGatheringInvitation(', 'private bool CanUseExistingPartyForGatheringTravel(', 'private bool CanUseDelayedSettlementTravelForGathering(', 'private bool CanPlayerHostAtSettlement(Hero host, Settlement settlement, NobleGatheringOptions options,', 'private bool TryResolveGovernorOwnedSettlement(']
    def without_reason_assignment(value):
        return re.sub(r'\s+','',re.sub(r'^\s*reason =[^\n]*;\s*$','',value,flags=re.M))
    for name in predicates:
        assert without_reason_assignment(extract.declaration(old_source,name))==without_reason_assignment(extract.declaration(source,name)),name+': qualification/control flow changed'
    assert extract.declaration(old_source,'public override void SyncData(')==extract.declaration(source,'public override void SyncData('),'save boundary changed'
    migrations=json.loads((ROOT/'artifacts/localization/verification-20261011/gathering-gate-additions.json').read_text(encoding='utf-8'))['literalMigrations']
    assert all(raw[key]==json.loads(literal) for literal,key in migrations),'old literal bytes/meaning changed'
names=['private static NobleGatheringInvitationSelector ParseInvitationSelectorTag(', 'private static List<string> SplitSelectorValues(', 'private static List<string> NormalizeStringList(', 'private static void ParseAgeRange(', 'private static string NormalizeGenderToken(']
methods='\n'.join(extract.declaration(source,name).replace('private static','internal static',1) for name in names)
selector=extract.declaration(source,'internal sealed class NobleGatheringInvitationSelector')
(OUT/'Selectors.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Text.RegularExpressions;namespace AnimusForge {'+selector+' internal static class SelectorHarness {'+methods+'}}',encoding='utf-8')
(OUT/'Program.cs').write_text(r'''using System;using System.IO;using AnimusForge;
namespace AnimusForge {internal static class AnimusForgeModulePaths {internal static string Package; internal static string GetModuleDataFilePath(string name)=>Path.Combine(Package,name);}}
class Program {
static int checks;static void C(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
static string Raw(string key)=>DiplomacyCompatibilityTexts.Get("Gathering_"+key);
static string R(string key,params(string Name,object Value)[] vars)=>NobleGatheringTextProjection.RawFormat("Gathering_"+key,vars);
static void Main(string[] args){AnimusForgeModulePaths.Package=args[0];bool en=args[1]=="en";
TaleWorlds.Localization.LocalizedTextManager.LoadLocalizationXmls(new[]{args[2]});
string hero="Lady {HERO} $1 中文",place="Town {SETTLEMENT} $1 中文";
string cancel=R("CancelPlayerStatus",("HERO",hero),("SETTLEMENT",place));
C(cancel==hero+"已经遵照你的命令取消了在"+place+"举办的宴会。","canonical status exact and literal dynamic values");
foreach(string language in new[]{"English","简体中文","English"}){
TaleWorlds.Localization.MBTextManager.ChangeLanguage(language);bool english=language=="English";
C(NobleGatheringTextProjection.ProjectForUi(Raw("InvalidHero"))==(english?"Invalid hero":"无效人物"),"actual UI language independent of package language="+language+" actual="+NobleGatheringTextProjection.ProjectForUi(Raw("InvalidHero")));
C(NobleGatheringTextProjection.ProjectForUi(cancel)==(english?hero+" followed your order and cancelled the gathering at "+place+".":cancel),"dynamic UI template literal names");
C(NobleGatheringTextProjection.ProjectForUi(R("NeedGold",("COST",1000)))==(english?"Cannot host the gathering: you need 1000 denars.":"宴会无法召开：你需要 1000 第纳尔。"),"gold quantity preserved");
C(NobleGatheringTextProjection.ProjectForUi(R("TravellingTo",("SETTLEMENT",place)))==(english?"Travelling to "+place:"正在旅行至"+place),"travel data literal");
C(NobleGatheringUiText.SelectedGuests(hero,14,true)==(english?"Selected: "+hero+" and others, 14 in total.":"已选："+hero+" 等 14 人。"),"selected count is total");
string fact=R("FactPlayerCancel",("HERO",hero),("SETTLEMENT",place));
C(NobleGatheringTextProjection.ProjectFactBodyForPrompt(fact)==(en?"The player ordered "+hero+" to cancel their gathering at "+place+"; the guests have dispersed.":fact),"fact package independent of interface");
C(fact=="玩家命令"+hero+"取消了自己在"+place+"举办的宴会，宾客已经散去。","fact canonical preserved");
}
C(NobleGatheringTextProjection.ProjectForUi("player quotation 中文 {HERO} $1")=="player quotation 中文 {HERO} $1","unknown freeform is unchanged");
C(Raw("FactPlayerPrefix")=="[AFEF玩家行为补充] "&&Raw("FactNpcPrefix")=="[AFEF NPC行为补充] ","AFEF envelopes canonical");
foreach(string tag in new[]{"[ACTION:NOBLE_GATHERING:START:地点=town:人物=hero1，hero2、无:家族=clan:王国=king:文化=culture:年龄=30至18:性别=女]","[ACTION:NOBLE_GATHERING:START:settlement=town:heroes=hero1,hero2,none:clans=clan:kingdoms=king:cultures=culture:age=30-18:gender=female]"}){
var s=SelectorHarness.ParseInvitationSelectorTag(tag);C(s!=null&&s.SettlementId=="town"&&s.SpecificHeroIds.Count==2&&s.ClanIds[0]=="clan"&&s.KingdomIds[0]=="king"&&s.CultureIds[0]=="culture"&&s.MinAge==18&&s.MaxAge==30&&s.Gender=="female","CN/EN selector equivalent");}
C(SelectorHarness.NormalizeGenderToken("男")=="male"&&SelectorHarness.NormalizeGenderToken("women")=="female","legacy and English gender aliases");
C(SelectorHarness.ParseInvitationSelectorTag("[ACTION:OTHER:START:hero=x]")==null,"unknown action rejected");
string guest=R("WeeklyNameRole",("HERO",hero),("ROLE",Raw("RoleClanLeader")));
string started=R("WeeklyStart",("HOST",hero),("ROLE",Raw("RoleKing")),("SETTLEMENT",place),("DAYS",3))+R("WeeklyGuests",("GUESTS",guest))+Raw("WeeklyStartDisclaimer");
string projected=NobleGatheringTextProjection.ProjectWeeklyMaterialForPrompt("noble_gathering","noble_gathering:1:started",started);
C(en?projected.Contains(hero+" (king)")&&projected.Contains(hero+" (clan leader)")&&projected.Contains("3 days")&&projected.Contains("not an unverified plan")&&projected.Contains("have not arrived"):projected==started,"weekly roles quantity and uncertain arrivals preserved");
C(started.Contains("计划持续3天")&&started.Contains("尚未抵达"),"weekly capture remains canonical");
foreach(string language in new[]{"English","简体中文"}){
TaleWorlds.Localization.MBTextManager.ChangeLanguage(language);string visible=NobleGatheringTextProjection.ProjectForUi(started);
C(language=="English"?visible.Contains(hero+" (king)")&&visible.Contains(hero+" (clan leader)")&&visible.Contains("have not arrived"):visible==started,"weekly UI frame independent of package");}
string cancelled=R("WeeklyCancel",("HOST",hero),("SETTLEMENT",place),("REASON",R("ReasonSuffix",("REASON",R("CrisisSiege",("SETTLEMENT",place))))));
projected=NobleGatheringTextProjection.ProjectWeeklyMaterialForPrompt("raw_text","noble_gathering:1:cancelled",cancelled);
C(en?projected.Contains("cancelled")&&projected.Contains("under siege")&&projected.Contains(place):projected==cancelled,"weekly cancellation and nested reason translated");
C(NobleGatheringTextProjection.ProjectWeeklyMaterialForPrompt("raw_text","unrelated:1",started)==started,"unrelated material unchanged");
string attendance=R("WeeklyAttendance",("HERO",hero),("ROLE",Raw("RoleClanLeader")),("SETTLEMENT",place),("HOST",hero));
projected=NobleGatheringTextProjection.ProjectWeeklyMaterialForPrompt("noble_gathering","noble_gathering:1:attendance:x",attendance);
C(en?projected.Contains("has actually arrived")&&projected.Contains("attendance has occurred"):projected==attendance,"attendance confirmed semantics");
foreach(var row in new[]{("FactPlayerStartExact",0),("FactPlayerStartMore",14)}){
string fact=R(row.Item1,("SETTLEMENT",place),("GUESTS",hero),("COUNT",row.Item2));projected=NobleGatheringTextProjection.ProjectFactBodyForPrompt(fact);
C(en?projected.Contains(place)&&projected.Contains(hero)&&(row.Item2==0||projected.Contains("14 guests in total")):projected==fact,"start fact invited count and literal data");}
Console.WriteLine("PASS Gathering source-derived selectors + production text readers/TextObject "+args[1]+" checks="+checks);
}}
''',encoding='utf-8')
catalog=OUT/'catalog';languages=catalog/'ModuleData/Languages';(languages/'CNs').mkdir(parents=True,exist_ok=True)
shutil.copy2(base/'Languages/afdip_ui_strings.xml',languages/'afdip_ui_strings.xml')
shutil.copy2(base/'Languages/CNs/afdip_ui_strings-zh-CN.xml',languages/'CNs/afdip_ui_strings-zh-CN.xml')
for relative,xml_path in [('language_data.xml','afdip_ui_strings.xml'),('CNs/language_data.xml','CNs/afdip_ui_strings-zh-CN.xml')]:
    registration=ET.parse(ROOT/'content/foundation/AF.Foundation.Localization/ModuleData/Languages'/relative)
    root=registration.getroot()
    for child in list(root):root.remove(child)
    ET.SubElement(root,'LanguageFile',{'xml_path':xml_path})
    registration.write(languages/relative,encoding='utf-8',xml_declaration=True)
links=''.join('<Compile Include="'+str(path)+'" />' for path in [gather/'NobleGatheringUiText.cs',gather/'NobleGatheringTextProjection.cs',ROOT/'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyCompatibilityTexts.cs',ROOT/'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyMiscPromptTexts.cs'])
refs=''.join('<Reference Include="'+name+'"><HintPath>'+str(path)+'</HintPath></Reference>' for name,path in [('Newtonsoft.Json',ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'),('TaleWorlds.Localization',ROOT/'_deps_auto/TaleWorlds.Localization.dll'),('TaleWorlds.Library',ROOT/'_deps_auto/TaleWorlds.Library.dll')])
(OUT/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup>'+links+refs+'</ItemGroup></Project>',encoding='utf-8')
(OUT/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
sys.path.insert(0,str(ROOT/'tests'));from output_isolation import minimal_test_environment
dotnet=ROOT/'local/dotnet/8.0.425/dotnet.exe';env=minimal_test_environment(dotnet,OUT)
for language,suffix in [('cn',''),('en','.en')]:
    package=OUT/language;package.mkdir(exist_ok=True)
    shutil.copy2(base/('DiplomacyMiscPromptTexts'+suffix+'.json'),package/'DiplomacyMiscPromptTexts.json')
    shutil.copy2(base/'DiplomacyMiscCompatibility.json',package/'DiplomacyMiscCompatibility.json')
    result=subprocess.run([str(dotnet),'run','--project',str(OUT/'Proof.csproj'),'-c','Release','-p:RestoreConfigFile='+str(OUT/'NuGet.Config'),'--',str(package),language,str(catalog)],env=env,capture_output=True,text=True,encoding='utf-8',timeout=120)
    (OUT/(language+'-run.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    print(result.stdout+result.stderr);result.check_returncode()
print('PASS Gathering resource keys/placeholders, EN prose, source literal gate and actual replay')
