"""Real packaged text dependencies for detached history-owner replay."""
from pathlib import Path
from xml.sax.saxutils import escape
import shutil

def add_history_resources(root,out,project,language='zh-CN',diplomacy_receipts=False):
 project=Path(project);out=Path(out);root=Path(root)
 text=project.read_text('utf-8')
 sources=['src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs','src/modules/AF.Module.Prompt/Composition/MemorySystemFactProjection.cs','src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs']
 if diplomacy_receipts: sources.append('src/modules/AF.Module.Diplomacy/Presentation/DiplomacyReplyTextRules.cs')
 items=''
 for source in sources:
  if source.replace('/','\\') not in text and source not in text:
   items+='<Compile Include="'+escape(str(root/source))+'" />'
 for folder,compat,name in [('AF.Module.Prompt','PromptCompatibility.json','AnimusForge.Prompt.Compatibility.json'),('AF.Module.Conversation','ConversationRuntimeCompatibility.json','AnimusForge.Conversation.RuntimeCompatibility.json')]:
  items+='<EmbeddedResource Include="'+escape(str(root/'content/modules'/folder/'ModuleData'/compat))+'"><LogicalName>'+name+'</LogicalName><WithCulture>false</WithCulture></EmbeddedResource>'
 if diplomacy_receipts:
  # These independent gathering/world-map projections are named passthrough leaves,
  # not part of this recall budget or role-assembly proof.
  leaf=out/'UnrelatedProjectionLeaves.cs'
  leaf.write_text('namespace AnimusForge { internal static class NobleGatheringTextProjection {internal static string ProjectFactBodyForPrompt(string x)=>x;} internal static class WorldMapFactPromptProjection {internal static string ProjectBodyForPrompt(string x)=>x;} }',encoding='utf-8')
  if '<EnableDefaultCompileItems>false</EnableDefaultCompileItems>' in text:
   items+='<Compile Include="'+escape(str(leaf))+'" />'
  for path,name in [('DiplomacyReceiptCompatibility.json','ReceiptCompatibility.json')]:
   items+='<EmbeddedResource Include="'+escape(str(root/'content/modules/AF.Module.Diplomacy/ModuleData'/path))+'"><LogicalName>AnimusForge.Diplomacy.'+name+'</LogicalName><WithCulture>false</WithCulture></EmbeddedResource>'
 if '<EnableDefaultCompileItems>false</EnableDefaultCompileItems>' in text:
  items+='<Compile Include="'+escape(str(out/'RuntimeResourcePath.cs'))+'" />'
 from output_isolation import resolve_dotnet
 sdk_root=resolve_dotnet(root).parent/'sdk'
 json_dll=max(sdk_root.glob('*/Newtonsoft.Json.dll'),key=lambda p: tuple(int(x) for x in p.parent.name.split('-')[0].split('.')))
 items+='<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(json_dll))+'</HintPath></Reference>'
 project.write_text(text.replace('</Project>','<ItemGroup>'+items+'</ItemGroup></Project>'),encoding='utf-8')
 (out/'RuntimeResourcePath.cs').write_text('namespace AnimusForge; internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name)=>System.IO.Path.Combine(System.AppContext.BaseDirectory,name); }',encoding='utf-8')
 target=out/'bin/Release/net8.0';target.mkdir(parents=True,exist_ok=True)
 for folder,source,name in [('AF.Module.Prompt','PromptComposerTexts.'+language+'.json','PromptComposerTexts.json'),('AF.Module.Conversation','ConversationRuntimePrompts'+('.en' if language=='en' else '')+'.json','ConversationRuntimePrompts.json')]:
  shutil.copyfile(root/'content/modules'/folder/'ModuleData'/source,target/name)
