"""Actual GCCZ catalog providers and core composer; game UI language is substituted."""
from pathlib import Path
import argparse, importlib.util, json, re, shutil, subprocess, sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path,required=True);args=parser.parse_args()
out=new_run_root(ROOT,'gccz-prompt-projection',args.out);dotnet=resolve_dotnet(ROOT)
catalogs=ROOT/'content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData'
host=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.cs').read_text('utf-8-sig')
assert 'settlementResult.ConstructionSpeedBonusPercent), isEnglish: !GcczLocalizedResourceLoader.UseChineseInterface)' in host,'castle completion UI must follow game interface language'
stems=['GcczTownPrompt','GcczTownManual','GcczTownHiddenResidents','GcczTownEntryPresentation','GcczTownActionPresentation']
def strings(x,path=''):
 if isinstance(x,dict):
  for k,v in x.items():yield from strings(v,path+'.'+k)
 elif isinstance(x,list):
  for i,v in enumerate(x):yield from strings(v,path+'.'+str(i))
 elif isinstance(x,str):yield path,x
for stem in stems:
 cn=dict(strings(json.loads((catalogs/(stem+'.zh-CN.json')).read_text('utf-8-sig'))))
 en=dict(strings(json.loads((catalogs/(stem+'.en.json')).read_text('utf-8-sig'))))
 assert cn.keys()==en.keys(),stem
 for k,v in en.items():
  assert not re.search('[\u3400-\u4dbf\u4e00-\u9fff]',v),(stem,k)
  assert sorted(re.findall(r'\{[a-zA-Z_][a-zA-Z_0-9]*\}',cn[k]))==sorted(re.findall(r'\{[a-zA-Z_][a-zA-Z_0-9]*\}',v)),(stem,k)
print('PASS five GCCZ paired catalog trees, English fixed text and named placeholders')
bridge=ROOT/'src/bridges/Siege/Resources'
paths=[bridge/(name+'.cs') for name in ['GcczLocalizedResourceLoader','GcczTownPromptResourceProvider','GcczTownManualResourceProvider','GcczTownHiddenResidentResourceProvider','GcczTownEntryPresentationResourceProvider','GcczTownActionPresentationResourceProvider']]
paths += [ROOT/'src/modules/AF.Module.Prompt/Composition/MemorySystemFactProjection.cs',ROOT/'src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs']
items=''.join('<Compile Include="'+escape(str(p))+'" />' for p in paths)
items+='<EmbeddedResource Include="'+escape(str(ROOT/'content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json'))+'" LogicalName="AnimusForge.Prompt.Compatibility.json" />'
for stem in stems:
 for lang in ['zh-CN','en']:
  items+='<EmbeddedResource Include="'+escape(str(catalogs/(stem+'.'+lang+'.json')))+'" LogicalName="AnimusForge.Defaults.'+stem+'.'+lang+'.json" WithCulture="false" />'
items+='<ProjectReference Include="'+escape(str(ROOT/'AnimusForge.SiegeAftermathIntervention/AnimusForge.SiegeAftermathIntervention.csproj'))+'" />'
items+='<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(dotnet.parent/'sdk/8.0.425/Newtonsoft.Json.dll'))+'</HintPath></Reference>'
(out/'SceneMemoryReplay.cs').write_text(Path(__file__).with_name('SceneMemoryReplay.cs.txt').read_text('utf-8'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
(out/'Host.cs').write_text('''using System.IO;
namespace TaleWorlds.Localization {internal static class MBTextManager {internal static string ActiveTextLanguage="en";}internal static class LocalizedTextManager{internal static string GetSubtitleExtensionOfLanguage(string s)=>s;}}
namespace AnimusForge {internal static class AnimusForgeModulePaths {internal static string Root;internal static string GetModuleDataFilePath(string name)=>Path.Combine(Root,name);} internal static class Logger{internal static void Log(string a,string b){}}}
namespace AnimusForge {
// Other domains are outside this replay; only the real GCCZ and AFEF consumers are exercised.
internal static class SocialFactPromptProjection {internal static string ProjectForPrompt(string s)=>s;}
internal static class EconomyActionFactProjection {internal static string ProjectForPrompt(string s)=>s;}
internal static class NobleGatheringTextProjection {internal static string ProjectFactBodyForPrompt(string s)=>s;}
internal static class WorldMapFactPromptProjection {internal static string ProjectBodyForPrompt(string s)=>s;}
}
''',encoding='utf-8')
(out/'Program.cs').write_text(r'''using System;using System.Linq;using System.Text.RegularExpressions;using AnimusForge;using AnimusForge.SiegeAftermathIntervention;using Newtonsoft.Json;
int checks=0;void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
bool Han(string text)=>Regex.IsMatch(text??"",@"[\u3400-\u4dbf\u4e00-\u9fff]");
AnimusForgeModulePaths.Root=args[0];TaleWorlds.Localization.MBTextManager.ActiveTextLanguage=args[2];bool en=args[1]=="en",uiChinese=args[2]=="zh-CN";
var catalog=GcczTownPromptResourceProvider.GetCatalog();
Check(catalog.Language==args[1],"prompt follows installed package, independent of UI");
Check(GcczTownPromptResourceProvider.GetPresentationCatalog().Language==(uiChinese?"zh-CN":"en"),"presentation follows interface language");
Check(GcczTownPromptResourceProvider.GetFactCatalog().Language=="zh-CN","canonical fact catalog retains identity");
foreach(var ui in new object[]{GcczTownManualResourceProvider.GetCatalog(),GcczTownHiddenResidentResourceProvider.GetCatalog(),GcczTownEntryPresentationResourceProvider.GetCatalog(),GcczTownActionPresentationResourceProvider.GetCatalog()})Check(Han(JsonConvert.SerializeObject(ui))==uiChinese,"actual disk/embedded UI catalog follows interface");
foreach(var role in Enum.GetValues<TownDialogueRole>())foreach(bool hostile in new[]{false,true}){
 var facts=new SiegeRuntimePromptFacts("Town",role,true,true,true,true,false,"","","none",hostile,hostile);
 string prompt=TownPromptComposer.BuildMainPrompt(facts,catalog);
 Check(en?!Han(prompt):Han(prompt),"main composer selected language and role/state frame");
}
string empty=TownPromptComposer.BuildMainPrompt(SiegeRuntimePromptFacts.Empty,catalog);
Check(en?!Han(empty):Han(empty),"empty settlement fallback must use selected package catalog");
string contract=TownPromptComposer.BuildPostprocessContract(new[]{"[ACTION:TEST]"},catalog);
Check(en?!Han(contract):Han(contract),"postprocess uses same selected catalog");
string name="Town {0} $1 原名";
var named=new SiegeRuntimePromptFacts(name,TownDialogueRoleClassifier.SafeFallbackRole,false,false,true,false,false,"Original 中文记忆","","none",false,false);
string dynamicPrompt=TownPromptComposer.BuildMainPrompt(named,catalog);
Check(dynamicPrompt.Contains(name)&&dynamicPrompt.Contains("Original 中文记忆"),"dynamic names and original memory are preserved literally");
SceneMemoryReplay.Run(catalog,Check);
Console.WriteLine("PASS GCCZ actual providers/composer package="+args[1]+" UI="+args[2]+" source="+args[3]+" checks="+checks+" game=NOT_RUN");
''',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=minimal_test_environment(dotnet,out)
build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo'],env=env,capture_output=True,text=True,encoding='utf-8',errors='replace');(out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8');assert build.returncode==0,build.stdout+build.stderr
for lang in ['zh-CN','en']:
 for source in ['disk','embedded']:
  pack=out/(lang+'-'+source);pack.mkdir()
  shutil.copyfile(catalogs/('GcczTownPrompt.'+lang+'.json'),pack/'GcczTownPrompt.json')
  shutil.copyfile(ROOT/('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.'+lang+'.json'),pack/'PromptComposerTexts.json')
  if source=='disk':
   for stem in stems:
    for locale in ['zh-CN','en']:shutil.copyfile(catalogs/(stem+'.'+locale+'.json'),pack/(stem+'.'+locale+'.json'))
  for ui in ['en','zh-CN']:
   r=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll'),str(pack),lang,ui,source],env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
   (out/(lang+'-'+source+'-'+ui+'.log')).write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr);r.check_returncode()
