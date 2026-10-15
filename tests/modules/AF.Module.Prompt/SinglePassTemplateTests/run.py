from pathlib import Path
import subprocess,sys,shutil,json,argparse
from xml.sax.saxutils import escape
R=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(R/'tests'))
from output_isolation import new_run_root
parser=argparse.ArgumentParser(); parser.add_argument('--run-root',type=Path); options=parser.parse_args()
O=new_run_root(R,'prompt-singlepass',options.run_root)
sys.path.insert(0,str(R/'tools/localization'))
from inventory_untranslated import LEX
source=(R/'src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs').read_text('utf-8-sig');mask=LEX.sub(lambda m:' '*len(m.group()),source)
def method(signature):
 start=source.index(signature); opening=mask.index('{',start);depth=0
 for end in range(opening,len(mask)):
  depth+=(mask[end]=='{')-(mask[end]=='}')
  if not depth:break
 return source[start:end+1]
methods=['public static string FormatDuelHealthTemplate(', 'private static string NormalizeActionPostprocessOptionalValue(', 'private static string ReplaceActionPostprocessOptionalSection(', 'public static string BuildActionPostprocessSystemPrompt(', 'public static string BuildActionPostprocessUserPrompt(']
stubs="""internal static string ActionPostprocessSystemPrompt; private static string ApplyPlayerDisplayNameToGuardrailText(string s)=>s; private static string NormalizeActionPostprocessNameReferences(string s,string n)=>s; private static string AppendPersistentActionPostprocessRuntimeContext(string s)=>s; private static string PrepareActionPostprocessHistoryText(string s,int n,string reply)=>s; } internal static class DuelSettings {internal static int GetActionPostprocessHistoryEntryLimitForExternal()=>20;} internal static class AnimusForgeModulePaths { internal static string Directory; internal static string GetModuleDataFilePath(string name)=>Path.Combine(Directory,name);}"""
bodies=[method(s) for s in methods]
source=(R/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs').read_text('utf-8-sig');mask=LEX.sub(lambda m:' '*len(m.group()),source)
for signature in ['internal static object CreateCourierChatMessage(', 'internal static bool TryConvertCourierMemoryMessageToChatMessage(', 'internal static string BuildCourierMemoryMetadataPrefix(', 'internal static string StripCourierPromptScopeLabel(', 'internal static string StripCourierSpeakerPrefix(']:bodies.append(method(signature))
(O/'ActualMethods.cs').write_text('using System;using System.IO;using System.Text.RegularExpressions;namespace AnimusForge {internal static class ActualMethods {'+'\n'.join(bodies)+stubs+'}',encoding='utf-8')
source=(R/'src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs').read_text('utf-8-sig');mask=LEX.sub(lambda m:' '*len(m.group()),source)
(O/'DailyMemoryLine.cs').write_text('using System;namespace AnimusForge {'+method('internal sealed class DailyMemoryLine')+'}',encoding='utf-8')
shutil.copyfile(Path(__file__).with_name('Program.cs.txt'),O/'Program.cs')
(O/'GatheringBoundary.cs').write_text('namespace AnimusForge { internal static class NobleGatheringTextProjection { internal static string ProjectFactBodyForPrompt(string raw) => raw; } }',encoding='utf-8')
links=''
for path in ['src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs','src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalTexts.cs','src/modules/AF.Module.Prompt/Composition/BuiltInRuleStickyCarry.cs','src/modules/AF.Module.Prompt/Composition/MemorySystemFactProjection.cs','src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs','src/modules/AF.Module.WorldMap/Runtime/WorldMapFactPromptProjection.cs','src/modules/AF.Module.WorldMap/Runtime/WorldMapSourceTexts.cs']:
 links+='<Compile Include="'+escape(str(R/path))+'" />'
for path,name in [('PromptCompatibility.json','AnimusForge.Prompt.Compatibility.json'),('PromptRetrievalCompatibility.json','AnimusForge.Prompt.RetrievalCompatibility.json')]:
 links+='<EmbeddedResource Include="'+escape(str(R/'content/modules/AF.Module.Prompt/ModuleData'/path))+'" LogicalName="'+name+'" />'
for path in ['src/modules/AF.Module.Diplomacy/Presentation/DiplomacyReplyTextRules.cs','src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs']:
 links+='<Compile Include="'+escape(str(R/path))+'" />'
for path,name in [('content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyReceiptCompatibility.json','AnimusForge.Diplomacy.ReceiptCompatibility.json'),('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json','AnimusForge.Conversation.RuntimeCompatibility.json')]:
 links+='<EmbeddedResource Include="'+escape(str(R/path))+'" LogicalName="'+name+'" />'
links+='<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(R/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'))+'</HintPath></Reference>'
(O/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup>'+links+'</ItemGroup></Project>',encoding='utf-8')
(O/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=R/'local/dotnet/8.0.425/dotnet.exe'
for api in ['1.3','1.4']:
 args=[str(dotnet),'build',str(O/'Test.csproj'),'--nologo','-v:q','-p:DefineConstants='+('BANNERLORD_1_4_OR_GREATER' if api=='1.4' else 'BANNERLORD_1_3')]
 b=subprocess.run(args,capture_output=True,text=True,encoding='utf-8',errors='replace');(O/('build-'+api+'.log')).write_text(b.stdout+b.stderr,encoding='utf-8');print('BUILD',api,b.returncode);assert b.returncode==0,b.stdout+b.stderr
 for lang in ['zh-CN','en']:
  pack=O/lang;pack.mkdir(exist_ok=True)
  for name in ['PromptComposerTexts','PromptRetrievalTexts']:
   suffix=lang if name=='PromptComposerTexts' else ('en' if lang=='en' else '')
   filename=name+('.'+suffix if suffix else '')+'.json'
   shutil.copyfile(R/'content/modules/AF.Module.Prompt/ModuleData'/filename,pack/(name+'.json'))
  shutil.copyfile(R/'content/modules/AF.Module.WorldMap/ModuleData/WorldMapCompatibility.json',pack/'WorldMapCompatibility.json')
  run=subprocess.run([str(dotnet),str(O/'bin/Debug/net8.0/Test.dll'),str(pack),lang],capture_output=True,text=True,encoding='utf-8',errors='replace');(O/(api+'-'+lang+'.log')).write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);assert run.returncode==0
