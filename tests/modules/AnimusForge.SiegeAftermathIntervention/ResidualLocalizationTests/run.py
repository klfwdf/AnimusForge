from pathlib import Path
import sys,importlib.util,re,subprocess
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent;sys.path.insert(0,str(R/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
spec=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
out=R/'artifacts/localization/verification-20261011/gccz-residual-run';out.mkdir(parents=True,exist_ok=True)
base=R/'src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath';backup=R/'artifacts/localization/gccz-residual-backup'
v=(base/'VillageAftermathBehavior.cs').read_text(encoding='utf-8-sig');n=(base/'NoblePrisonerExecutionOrderBehavior.cs').read_text(encoding='utf-8-sig');oldv=(backup/'VillageAftermathBehavior.cs').read_text(encoding='utf-8-sig');oldn=(backup/'NoblePrisonerExecutionOrderBehavior.cs').read_text(encoding='utf-8-sig')
def methods(v,n):return ex.declaration(v,'internal static List<PostprocessRuleEntry> BuildPostprocessRulesForExternal(')+ex.declaration(v,'internal static string BuildPostprocessContextForExternal(')+ex.declaration(n,'private static string BuildConsentRule(').replace('private static','internal static',1)

leaf='''internal static bool Active=true;internal static bool IsActive()=>Active;internal static Settlement ResolveActiveVillage()=>new Settlement();internal static string _activeAuthorityKind="DirectOwner";internal static PostprocessRuleEntry Rule(string t,string d)=>new PostprocessRuleEntry{Tag=t,Description=d};'''
program='''using System;using System.Collections.Generic;using System.Linq;using AnimusForge.SiegeAftermathIntervention;
internal static class GcczTownPromptResourceProvider{internal static bool UseEnglishPrompt;}
internal class Hero{internal string Name;}
internal class Settlement{internal string Name="Village{NAME}$1";internal Village Village=new Village();}
internal class Village{internal Bound Bound=new Bound();}internal class Bound{internal string Name="Castle{SECOND}";}
internal class PostprocessRuleEntry{internal string Tag,Description;}

internal static class Current{'''+leaf+methods(v,n)+'''}
internal static class Legacy{'''+leaf+methods(oldv,oldn)+'''}
internal static class Program{static int count;static void C(bool v,string label){count++;if(!v)throw new Exception(label);}static void Main(){
GcczTownPromptResourceProvider.UseEnglishPrompt=false;
C(Current.BuildPostprocessContextForExternal(true)==Legacy.BuildPostprocessContextForExternal(true),"CN context exact old value");
var cn=Current.BuildPostprocessRulesForExternal(true);var old=Legacy.BuildPostprocessRulesForExternal(true);C(cn.Count==14&&cn.Select(x=>x.Tag+"|"+x.Description).SequenceEqual(old.Select(x=>x.Tag+"|"+x.Description)),"CN all 14 rules original protocol/order/conditions");
C(Current.BuildPostprocessRulesForExternal(false).Count==0,"direct-player guard");Current.Active=false;C(Current.BuildPostprocessRulesForExternal(true).Count==0&&Current.BuildPostprocessContextForExternal(true)=="","active guard");Current.Active=true;
var actor=new Hero{Name="Ada{NAME}$1"};var prisoner=new Hero{Name="Target{SECOND}"};C(Current.BuildConsentRule(actor,prisoner,"effect")==Legacy.BuildConsentRule(actor,prisoner,"effect"),"CN consent exact old value");
var facts=new SiegeSharedReliefPoolFacts(17,3,8,999);C(SiegeSharedReliefPoolFormatter.DescribeForContext(facts)==SiegeSharedReliefPoolFormatter.DescribeForContext(facts,false),"default fact original Chinese");
GcczTownPromptResourceProvider.UseEnglishPrompt=true;var en=Current.BuildPostprocessRulesForExternal(true);C(en.Select(x=>x.Tag).SequenceEqual(cn.Select(x=>x.Tag)),"EN protocol/order unchanged");C(en.All(x=>x.Description.StartsWith("Emit only when the player explicitly")),"EN all consent boundaries");C(en[0].Description.Contains("formation 3")&&en[13].Description.Contains("only open the player confirmation UI"),"formation number and culture authority");
var text=Current.BuildPostprocessContextForExternal(false);C(text.Contains("Village{NAME}$1")&&text.Contains("Castle{SECOND}")&&text.Contains("False")&&text.Contains("must not replace or block"),"EN context dynamic values/false/ordinary AF boundary");
text=Current.BuildConsentRule(actor,prisoner,"effect");C(text.Contains(actor.Name)&&text.Contains(prisoner.Name)&&text.Contains("refusal, hesitation, questions, reported speech, hypotheticals, historical content or execution of someone else"),"EN consent subjects and all negative cases");
text=SiegeSharedReliefPoolFormatter.DescribeForContext(facts,true);C(text=="17 denars, 3 food units, 5 non-food items, value 999","EN pool exact quantities");C(SiegeSharedReliefPoolFormatter.DescribeForContext(null,true)=="No shared supplies delivered through AF yet","EN empty pool");
Console.WriteLine("PASS GCCZ residual actual CN/EN method checks="+count+" live=NOT_RUN");}}
'''
(out/'Program.cs').write_text(program,encoding='utf-8');paths=['AnimusForge.SiegeAftermathIntervention/VillageAftermathActionTagCatalog.cs','AnimusForge.SiegeAftermathIntervention/VillageAftermathActionKind.cs','AnimusForge.SiegeAftermathIntervention/CompatibilityFlatJson.cs','AnimusForge.SiegeAftermathIntervention/SiegeAftermathCompatibilityTexts.cs','AnimusForge.SiegeAftermathIntervention/SiegeSharedReliefPoolFacts.cs','AnimusForge.SiegeAftermathIntervention/SiegeSharedReliefPoolFormatter.cs']
(out/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/p)+'"/>' for p in paths)+'</ItemGroup></Project>',encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
d=resolve_dotnet(R);run=subprocess.run([str(d),'run','--project',str(out/'Probe.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace');(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);raise SystemExit(run.returncode)
