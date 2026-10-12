from pathlib import Path
import re,sys,subprocess,json,shutil,xml.etree.ElementTree as ET,hashlib
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent;sys.path.insert(0,str(R/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
sys.path.insert(0,str(R/'tests/modules/AF.Module.Llm/RetryProjectionTests'))
from fixture_support import retry_members,add_retry_resources
O=R/'artifacts/localization/verification-20261011/ui-helper-sweep-run';O.mkdir(parents=True,exist_ok=True)
before='--before' in sys.argv
if before: O=O/'before';O.mkdir(exist_ok=True)
def read_source(path):
 backup=R/'artifacts/localization/verification-20261011/ui-helper-sweep-before'/path
 return (backup if before and backup.exists() else R/path).read_text(encoding='utf-8-sig')
catalog=O/'catalog';entries=json.loads((R/'content/content-map.json').read_text(encoding='utf-8'))['entries']
for entry in entries:
 if entry['target'].startswith('ModuleData/Languages/') and entry['target'].endswith('.xml'):
  target=catalog/entry['target'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/entry['source'],target)
for language,directory,prefix in [('English','','EN'),('简体中文','CNs','中')]:
 folder=catalog/'ModuleData/Languages'/directory;index=ET.parse(folder/'language_data.xml');ET.SubElement(index.getroot(),'LanguageFile',{'xml_path':(directory+'/' if directory else '')+'probe.xml'});index.write(folder/'language_data.xml',encoding='utf-8',xml_declaration=True)
 base=ET.Element('base',{'type':'string'});tags=ET.SubElement(base,'tags');ET.SubElement(tags,'tag',{'language':language});strings=ET.SubElement(base,'strings')
 for key,value in [('AF_TEST_Literal',prefix+'<{NAME}>|{SECOND}|{NAME}'),('AF_TEST_Repeat',prefix+'<{NAME}>|{NAME}'),('AF_TEST_Embedded','INNER'),('AF_TEST_Detail',prefix+'<{DETAIL}>|{DETAIL}')]:ET.SubElement(strings,'string',{'id':key,'text':value})
 ET.ElementTree(base).write(folder/'probe.xml',encoding='utf-8',xml_declaration=True)
import importlib.util
spec=importlib.util.spec_from_file_location('decl',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)

manifest=json.loads((H/'sweep-methods.json').read_text())
generated='using System.Runtime.CompilerServices;using System.Net;using Newtonsoft.Json;using System.Collections.Generic;using System;using System.IO;using System.Text;using System.Threading;using System.Globalization;using System.Text.RegularExpressions;using TaleWorlds.Localization;namespace AnimusForge { '
calls=[]
extra_calls=[]
nested_calls=[]
for path in dict.fromkeys(m['file'] for m in manifest):
 source=read_source(path);cls=re.search(r'internal static class (\w+)',source).group(1)
 body='\n'.join(re.findall(r'private static readonly (?:System.Text.RegularExpressions.)?Regex \w+ = [^\n]+;',source))
 if 'private static string RenderLiteral(' in source:body+=ex.declaration(source,'private static string RenderLiteral(')
 if cls=='ExerciseUiLocalization':body+=ex.declaration(source,'internal static TextObject Object(string id, string englishFallback)')+ex.declaration(source,'internal static TextObject Object(string id, string englishFallback, params')
 if cls=='IllustratorUiText':body+=ex.declaration(source,'private static string DisplayValue(')
 for item in [m for m in manifest if m['file']==path]:
  signature=item['signature'];body+=ex.declaration(source,signature).replace('private static','internal static',1)
  name=re.search(r'string (\w+)\(',signature).group(1)
  if 'params string[]' in signature:arguments='"NAME", value, "SECOND", "TAIL"';count=2
  elif 'params' in signature:
   val='(object)value' if 'object Value' in signature else 'value';arguments='("NAME", '+val+'), ("SECOND", "TAIL")';count=2
  else:
   names=re.findall(r'string (\w+Name|name|variable|firstName|secondName|thirdName|fourthName)',signature)
   count=len(names);arguments=', '.join('"'+('NAME' if i==0 else 'SECOND' if i==1 else 'EXTRA'+str(i))+'", '+('value' if i==0 else '"TAIL"') for i in range(count))
  template='EN<{NAME}>|{SECOND}|{NAME}' if count>=2 else 'EN<{NAME}>|{NAME}'
  id='AF_TEST_Literal' if count>=2 else 'AF_TEST_Repeat'
  expected='prefix+"<"+(value??"")+">|TAIL|"+(value??"")' if count>=2 else 'prefix+"<"+(value??"")+">|"+(value??"")'
  calls.append('Check("'+cls+'.'+name+'", '+cls+'.'+name+'("'+id+'", "'+template+'", '+arguments+'), '+expected+');')
  if 'params' in signature and 'object Value' in signature:
   nested_calls.append('Check("nested '+cls+'.'+name+'", '+cls+'.'+name+'("AF_TEST_Repeat", "EN<{NAME}>|{NAME}", ("NAME", new TextObject("{=AF_TEST_Embedded}Wrong"))), prefix+"<"+(lang=="English"?"Wrong":"INNER")+">|"+(lang=="English"?"Wrong":"INNER"));')
  if count>=2:
   duplicate=arguments.replace('"SECOND", "TAIL"','"NAME", "LAST{NAME}$1"')
   extra_calls.append('Check("duplicate '+cls+'.'+name+'", '+cls+'.'+name+'("AF_TEST_Repeat", "EN<{NAME}>|{NAME}", '+duplicate.replace('value','"FIRST"')+'), prefix+"<LAST{NAME}$1>|LAST{NAME}$1");')
  number=arguments.replace('(object)value','(object)1.5m') if 'object Value' in signature else arguments.replace('value','1.5m' if ('object value' in signature or 'object firstValue' in signature) else '1.5m.ToString()')
  extra_calls.append('Check("culture '+cls+'.'+name+'", '+cls+'.'+name+'("'+id+'", "'+template+'", '+number+'), '+expected.replace('(value??"")','"1,5"')+');')

 generated+=ex.declaration(source,'internal static class IllustratorUiText') if cls=='IllustratorUiText' else 'internal static class '+cls+' { '+body+' } '

# Real special string-return branches, excluding unrelated owners from compilation.
source=read_source('src/modules/AF.Module.Memory/Summary/MemoryFailureUiText.cs')
body=ex.declaration(source,'internal sealed class MemoryFailureUiText')
body=body.replace(ex.declaration(source,'internal static MemoryFailureUiText ParseFailure('),'')
generated+=body
source=read_source('extensions/AnimusForge.XihaiAction/src/Runtime/SceneActionsText.cs')
generated+='internal static class SceneActionsText { '+ex.declaration(source,'private static string LocalizeSpeechReason(').replace('private static','internal static',1)+' } '
source=read_source('src/modules/AF.Module.Conversation/Channels/Courier/CourierFailureUiText.cs')
generated+=ex.declaration(source,'internal static class CourierFailureUiText').replace('LocalizedTextObject','TextObject').replace('private static string FailureDetail','internal static string FailureDetail')
source=(R/'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs').read_text(encoding='utf-8-sig')
generated+='internal static class LlmRetryPrompt { '+retry_members(source,ex.declaration,['internal static string ProjectKnownErrorForUi('])+' } internal static class Logger { internal static void Log(string category,string text){} } '
source=(R/'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs').read_text(encoding='utf-8-sig')
generated+=ex.declaration(source,'internal static class SaveRuntimeGuard').replace('Stream stream', 'System.IO.Stream stream').replace('StreamReader reader', 'System.IO.StreamReader reader').replace('new StreamReader(', 'new System.IO.StreamReader(').replace('new UTF8Encoding(', 'new System.Text.UTF8Encoding(')
# Use the actual display projection, not a text stub.
failure=(R/'extensions/AnimusForge.Illustrator/src/UI/IllustrationFailureUiText.cs').read_text(encoding='utf-8-sig')
old_failure=(R/'artifacts/localization/verification-20261011/illustrator-full-before/extensions/AnimusForge.Illustrator/src/UI/IllustrationFailureUiText.cs').read_text(encoding='utf-8-sig')
generated+=ex.declaration(old_failure,'internal static class IllustrationFailureUiText').replace('IllustrationFailureUiText','BeforeIllustrationFailureUiText')
compatibility=json.loads((R/'content/modules/AnimusForge.Illustrator/ModuleData/IllustratorCompatibilityTexts.json').read_text(encoding='utf-8'))
legacy_errors=[v for k,v in compatibility.items() if k.startswith('Residual_IllustrationFailureUiText_')]
legacy_checks='foreach(string raw in new string[]{'+','.join(json.dumps(v,ensure_ascii=False) for v in legacy_errors)+'}){Check("legacy failure exact",IllustrationFailureUiText.Project(raw),BeforeIllustrationFailureUiText.Project(raw));}'
status_path='extensions/AnimusForge.Illustrator/src/UI/IllustrationStatusUiText.cs'
for cls,source in [('StatusSlice',(R/status_path).read_text(encoding='utf-8-sig')),('BeforeStatusSlice',(R/'artifacts/localization/verification-20261011/illustrator-full-before'/status_path).read_text(encoding='utf-8-sig'))]:
 body='\n'.join(re.findall(r'private static readonly Regex \w+ = [^\n]+;',source))
 body+=ex.declaration(source,'private static string KnownLocalFallback(').replace('private static','internal static',1)
 body+=ex.declaration(source,'internal static string Scene(')
 generated+='internal static class '+cls+' { '+body+' } '

for sourcepath,cls in [('extensions/AnimusForge.Illustrator/src/Context/NarrativeFactRouter.cs','NarrativeFactRouter'),('extensions/AnimusForge.Illustrator/src/Core/ImagePromptBudget.cs','ImagePromptBudget')]:
 source=(R/sourcepath).read_text(encoding='utf-8-sig');old=(R/'artifacts/localization/verification-20261011/illustrator-full-before'/sourcepath).read_text(encoding='utf-8-sig')
 signature=('internal' if cls=='ImagePromptBudget' else 'internal')+' static class '+cls
 generated+=ex.declaration(source,signature)
 generated+=ex.declaration(old,signature).replace(cls,'Before'+cls)
status_values=[v for k,v in compatibility.items() if k.startswith('Residual_IllustrationStatusUiText_')]
legacy_checks+='foreach(string raw in new string[]{'+','.join(json.dumps(v,ensure_ascii=False) for v in status_values)+'}){Check("legacy scene status",StatusSlice.Scene(raw),BeforeStatusSlice.Scene(raw));Check("legacy fallback status",StatusSlice.KnownLocalFallback(raw),BeforeStatusSlice.KnownLocalFallback(raw));}'
legacy_checks+='Check("cache placeholder",IllustratorUiText.ProjectKnownCachedPrompt(IllustratorText.Raw("'+next(k for k,v in compatibility.items() if v=='（本地已留存历史画卷）')+'")),new TextObject("{=AF_ILL_UI_LOCAL_CACHE_PROMPT}(A visual chronicle is retained locally)").ToString());Check("player prompt unchanged",IllustratorUiText.ProjectKnownCachedPrompt("PLAYER{NAME}$1"),"PLAYER{NAME}$1");'


generated+=ex.declaration((R/'extensions/AnimusForge.Illustrator/src/Core/IllustratorText.cs').read_text(encoding='utf-8-sig'),'internal static class IllustratorText')
generated+='internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string file)=>System.IO.Path.Combine(@"'+str(R/'content/modules/AnimusForge.Illustrator/ModuleData')+'",file); } '
generated+=' } '+failure.replace('using System;', '').replace('namespace AnimusForge.Illustrator.UI','namespace AnimusForge')
program="""using System;using System.Globalization;using TaleWorlds.Localization;using AnimusForge;
class Program {static int checks,failed;static void Check(string name,string actual,string expected){checks++;if(actual!=expected){failed++;Console.WriteLine("FAIL "+name+" actual="+actual+" expected="+expected);}}
static int Main(string[] args){LocalizedTextManager.LoadLocalizationXmls(new[]{args[0]});foreach(var lang in new[]{"English","简体中文","English"}){MBTextManager.ChangeLanguage(lang);string prefix=lang=="English"?"EN":"中";foreach(string value in new[]{"{NAME}","{=AF_TEST_Embedded}literal","$1","AFSWEEPLITERAL1END",null,""}){
"""+ '\n'.join(calls)+"""

Check("memory nested", MemoryFailureUiText.T("AF_TEST_Repeat","EN<{NAME}>|{NAME}",("NAME", MemoryFailureUiText.Raw(value))).Resolve(),prefix+"<"+(value??"")+">|"+(value??""));
if(!string.IsNullOrEmpty(value)) Check("courier literal", CourierFailureUiText.FailureDetail("{=AF_TEST_Detail}EN<{DETAIL}>|{DETAIL}",value),prefix+"<"+(value??"")+">|"+(value??""));
foreach(var start in new[]{"NPC speech request could not start: ","Deferred AF reply replay failed: "}) {
string actual=SceneActionsText.LocalizeSpeechReason(start+value);checks++;if(!actual.EndsWith(value??"",StringComparison.Ordinal)||actual.Contains("AFSCENEREASONLITERALEND")){failed++;Console.WriteLine("FAIL scene reason "+actual);}
}
}
"""+'\n'.join(nested_calls)+"""
var culture=CultureInfo.CurrentCulture;CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
"""+'\n'.join(extra_calls)+"""
CultureInfo.CurrentCulture=culture;
}
var delayed=ExerciseUiLocalization.Object("AF_TEST_Repeat","EN<{NAME}>|{NAME}",("NAME","Late"));
MBTextManager.ChangeLanguage("简体中文");Check("exercise delayed CN",delayed.ToString(),"中<Late>|Late");
MBTextManager.ChangeLanguage("English");Check("exercise delayed EN",delayed.ToString(),"EN<Late>|Late");
foreach(var language in new[]{"English","简体中文"}) {
MBTextManager.ChangeLanguage(language);
foreach(var key in new[]{"Error_PanoramaRootsBudget","Error_PanoramaNodesBudget"}) {
string raw=IllustratorText.Raw(key);string id=key=="Error_PanoramaRootsBudget"?"AF_ILL_ERROR_PANORAMA_ROOTS_BUDGET":"AF_ILL_ERROR_PANORAMA_NODES_BUDGET";
string en=key=="Error_PanoramaRootsBudget"?"The current scene has more than 32768 root entities to traverse. The environment snapshot stopped; a truncated copy must not be marked as a complete panorama.":"The current environment hierarchy has more than 32768 nodes. The snapshot stopped; an incomplete copy must not be marked as a complete panorama.";
Check("panorama budget projection",IllustrationFailureUiText.Project(raw),new TextObject("{="+id+"}"+en).ToString()); }
string error="生图提示词超过32000字符预算，无法在保留事实、参考图约束和完整段落的前提下缩短；请求未发送。请缩短自定义画风/要求或减少本次素材。";
string result=IllustrationFailureUiText.Project(error);checks++;if(!result.Contains("32000")||result==error&&language=="English"){failed++;Console.WriteLine("FAIL image prompt budget "+result);}
Check("unknown exception literal",IllustrationFailureUiText.Project("Unknown{NAME}$1"),"Unknown{NAME}$1");
Check("unknown internal-token literal",IllustrationFailureUiText.Project("AFICASE0f18bd4735d56b6e"),"AFICASE0f18bd4735d56b6e");
"""+legacy_checks+"""
}
foreach(string sentence in new[]{"敌军围城失败。领主获释。", "The army failed to attack; the lord was released.", "他说：我们已经获胜", "黑发领主穿着铠甲。", "<a>Lord</a> said victory"}) {
Check("event routing preserved",NarrativeFactRouter.BuildEventEvidence(sentence,"",sentence),BeforeNarrativeFactRouter.BuildEventEvidence(sentence,"",sentence));
Check("visual routing preserved",NarrativeFactRouter.ExtractVisualEvidence(sentence),BeforeNarrativeFactRouter.ExtractVisualEvidence(sentence));
checks++;if(NarrativeFactRouter.HasNarrativeEcho(sentence,sentence,"")!=BeforeNarrativeFactRouter.HasNarrativeEcho(sentence,sentence,""))failed++;
}
foreach(string name in new[]{"Residual_ImagePromptBudget_9d74adf88df4","Residual_ImagePromptBudget_6b05b94fef37","Residual_ImagePromptBudget_c03fde28bf42","Residual_ImagePromptBudget_59419c80e872"}) {
string longText="【"+IllustratorText.Raw(name)+"】"+string.Concat(System.Linq.Enumerable.Repeat("A complete sentence. ",100))+"【不可改写的核心事实】FIXED";
Check("budget protocol preserved",ImagePromptBudget.FitMain(longText,0,1000),BeforeImagePromptBudget.FitMain(longText,0,1000));
}
Console.WriteLine("real TW helper sweep checks="+checks+" failures="+failed);return failed==0?0:1;}}
"""
d=resolve_dotnet(R);results=[]
for api,dep in [('1.3','_deps_auto'),('1.4','.tmp/build_check/1.4')]:
 out=O/api;out.mkdir(exist_ok=True);(out/'ActualMethods.cs').write_text(generated,encoding='utf-8');(out/'Program.cs').write_text(program,encoding='utf-8');deps=R/dep
 project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup>'+''.join('<Reference Include="'+name+'"><HintPath>'+str((R/'.tmp/build_check/1.4' if name=='Newtonsoft.Json' else deps)/(name+'.dll'))+'</HintPath></Reference>' for name in ['TaleWorlds.Localization','TaleWorlds.Library','Newtonsoft.Json'])+'</ItemGroup></Project>'
 project=project.replace('</Project>','<ItemGroup><EmbeddedResource Include="'+str(R/'content/foundation/AF.Foundation.Localization/ModuleData/RuntimeStaleRequestCompatibility.txt')+'" LogicalName="AnimusForge.Foundation.StaleRequestError.txt" /></ItemGroup></Project>')
 (out/'Probe.csproj').write_text(project);add_retry_resources(R,out/'Probe.csproj',deps);(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 run=subprocess.run([str(d),'run','--project',str(out/'Probe.csproj'),'-c','Release','--',str(catalog)],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90);(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(api,run.stdout+run.stderr);results.append({'api':api,'exit_code':run.returncode,'tw_sha256':hashlib.sha256((deps/'TaleWorlds.Localization.dll').read_bytes()).hexdigest()})
(O/'receipt.json').write_text(json.dumps({'results':results,'source_sha256':{path:hashlib.sha256((R/path).read_bytes()).hexdigest() for path in list(dict.fromkeys(m['file'] for m in manifest))+['src/modules/AF.Module.Memory/Summary/MemoryFailureUiText.cs','extensions/AnimusForge.XihaiAction/src/Runtime/SceneActionsText.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierFailureUiText.cs']},'actual_managed_TW':True,'native_game_UI':'NOT_RUN'},indent=2));raise SystemExit(0 if all(x['exit_code']==0 for x in results) else 1)

