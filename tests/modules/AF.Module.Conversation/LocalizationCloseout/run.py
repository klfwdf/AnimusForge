"""Actual cached prompt reader and group-address source replay; no game UI claim."""
from pathlib import Path
import xml.etree.ElementTree as ET
import json,re,shutil,subprocess,sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
BASE=ROOT/'artifacts/localization/verification-20261011/conversation-closeout-replay'
BASE.mkdir(parents=True,exist_ok=True)
content=ROOT/'content/modules/AF.Module.Conversation/ModuleData'
cn=json.loads((content/'ConversationRuntimePrompts.json').read_text(encoding='utf-8-sig'))
en=json.loads((content/'ConversationRuntimePrompts.en.json').read_text(encoding='utf-8-sig'))
assert cn.keys()==en.keys()
ui={}
for language,filename in [('EN','Languages/afconv_ui_strings.xml'),('CN','Languages/CNs/afconv_ui_strings-zh-CN.xml')]:
    entries=ET.parse(content/filename).findall('.//string')
    ui[language]={e.attrib['id']:e.attrib['text'] for e in entries}
    assert len(ui[language])==len(entries),language+' duplicate UI ID'
assert ui['EN'].keys()==ui['CN'].keys(),'UI ID parity'
for k in ui['EN']:
    assert set(re.findall(r'\{[A-Z_]+\}',ui['EN'][k]))==set(re.findall(r'\{[A-Z_]+\}',ui['CN'][k])),k
available_ui=set(ui['EN'])
shared=ROOT/'content/modules/AF.Module.UI/ModuleData/Languages'
shared_en={s.attrib['id'] for s in ET.parse(shared/'af_ui_strings.xml').findall('.//string')}
shared_cn={s.attrib['id'] for s in ET.parse(shared/'CNs/af_ui_strings-zh-CN.xml').findall('.//string')}
available_ui.update(shared_en & shared_cn)
all_sources=list((ROOT/'src/modules/AF.Module.Conversation').rglob('*.cs'))
source_queue=[]
missing=[]
for p in all_sources:
    text=p.read_text(encoding='utf-8-sig')
    for k in re.findall(r'ConversationRuntimeTexts\.(?:FormatPrompt|Prompt)\("([^"]+)"',text): assert k in cn,(p,k)
    for k in re.findall(r'ConversationUiLocalization\.(?:Text|Object)\("([^"]+)"',text):
        if k not in available_ui:missing.append(dict(file=str(p.relative_to(ROOT)),key=k))
    source_queue.append(dict(file=str(p.relative_to(ROOT)),bytes=p.stat().st_size,promptCalls=len(re.findall(r'ConversationRuntimeTexts\.(?:FormatPrompt|Prompt)\(',text)),rawCalls=len(re.findall(r'ConversationRuntimeTexts\.Raw\(',text)),uiCalls=len(re.findall(r'ConversationUiLocalization\.(?:Text|Object)\(',text))))
(BASE/'source-queue.json').write_text(json.dumps(source_queue,indent=2),encoding='utf-8')
(BASE/'missing-ui.json').write_text(json.dumps(missing,indent=2),encoding='utf-8')
assert not missing,missing
for k in cn:
    assert set(re.findall(r'\{[A-Z_]+\}',cn[k]))==set(re.findall(r'\{[A-Z_]+\}',en[k])),k
    assert not re.search('[\u3400-\u9fff]',en[k].replace('[RELAY:接力编号]','')),k
source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs').read_text(encoding='utf-8-sig')
start=source.index('private static bool IsExplicitSceneGroupAddress(')
stop=source.index('\n\t}',start)+4
method=source[start:stop].replace('private static','internal static',1)
post=(ROOT/'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs').read_text(encoding='utf-8-sig')
start=post.index('internal static bool IsNativeConversationNoSpeechPlaceholder(')
stop=post.index('\n\t}',start)+4
method+='\n'+post[start:stop]
program='''using System; using System.IO; using System.Text.RegularExpressions; using AnimusForge;
namespace AnimusForge { internal static class AnimusForgeModulePaths { internal static string Root; internal static int Reads; internal static string GetModuleDataFilePath(string name) { Reads++; return Path.Combine(Root,name); } }
internal static class GroupProbe { METHOD } }
internal static class Program {
static void Check(bool value,string name) { if(!value) throw new Exception(name); }
static void Main(string[] args) {
AnimusForgeModulePaths.Root=args[0]; bool en=args[1]=="EN";
Check(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0684")== (en?"[Player action] ":"[玩家动作] "),"pack action heading");
string name="Lord {PLAYER} $1";
string letter=ConversationRuntimeTexts.FormatPrompt("letter_diplomacy_peace",("SENDER",name),("PLAYER","Player {SENDER}"),("SENDER_KINGDOM","A"),("PLAYER_KINGDOM","B"));
Check(letter.Contains(name)&&letter.Contains("Player {SENDER}"),"inserted names must be literal");
Check(letter.Contains(en?"unconditional peace":"无条件和平"),"peace proposal condition");
foreach(string kind in new[]{"peace","alliance","trade","formal"}) Check(!string.IsNullOrWhiteSpace(ConversationRuntimeTexts.FormatPrompt("letter_diplomacy_"+kind,("SENDER",name),("PLAYER","P"),("SENDER_KINGDOM","A"),("PLAYER_KINGDOM","B"))),"letter "+kind);
Check(AnimusForgeModulePaths.Reads==1,"cached reader");
foreach(string input in new[]{"Everyone, listen!","ALL OF YOU","you all should speak","大家说说","诸位"}) Check(GroupProbe.IsExplicitSceneGroupAddress(input,2),"group "+input);
foreach(string input in new[]{"someone","sisterhood","you alone","everybodyguard"}) Check(!GroupProbe.IsExplicitSceneGroupAddress(input,2),"non-group "+input);
Check(!GroupProbe.IsExplicitSceneGroupAddress("everyone",1),"single target gate");
foreach(string input in new[]{"（没说话）","无回信","(no speech)","(said nothing)","NONE","no reply"}) Check(GroupProbe.IsNativeConversationNoSpeechPlaceholder(input),"no speech "+input);
foreach(string input in new[]{"I said nothing about surrender.","No reply is needed.","none of your business"}) Check(!GroupProbe.IsNativeConversationNoSpeechPlaceholder(input),"spoken "+input);
Check(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0542")=="[AFEF玩家行为补充] ","stable fact prefix");
Console.WriteLine("PASS "+args[1]+": actual prompt reader, 4 letter templates, literal braces, cached IO, bilingual group address and stable fact prefix");
}}
'''.replace('METHOD',method)
(BASE/'Program.cs').write_text(program,encoding='utf-8')
dotnet=resolve_dotnet(ROOT)
newtonsoft=ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
helper=ROOT/'src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs'
(BASE/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Compile Include="'+escape(str(helper))+'"/><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath></Reference><EmbeddedResource Include="'+escape(str(content/'ConversationRuntimeCompatibility.json'))+'"><LogicalName>AnimusForge.Conversation.RuntimeCompatibility.json</LogicalName><WithCulture>false</WithCulture></EmbeddedResource></ItemGroup></Project>',encoding='utf-8')
(BASE/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=minimal_test_environment(dotnet,BASE)
build=subprocess.run([str(dotnet),'build','Probe.csproj','--nologo'],cwd=BASE,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
(BASE/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
if build.returncode: print(build.stdout+build.stderr);sys.exit(build.returncode)
for language,filename in [('CN','ConversationRuntimePrompts.json'),('EN','ConversationRuntimePrompts.en.json')]:
    package=BASE/language;package.mkdir(exist_ok=True)
    shutil.copyfile(content/filename,package/'ConversationRuntimePrompts.json')
    result=subprocess.run([str(dotnet),str(BASE/'bin/Debug/net8.0/Probe.dll'),str(package),language],cwd=BASE,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
    (BASE/(language+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    print(result.stdout+result.stderr)
    if result.returncode:sys.exit(result.returncode)
print('PASS',len(cn),'prompt pairs;',len(ui['EN']),'UI key pairs;',len(source_queue),'source files and all catalog call keys; actual source replay evidence:',BASE)
