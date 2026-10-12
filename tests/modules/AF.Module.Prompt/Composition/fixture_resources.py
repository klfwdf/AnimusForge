"""Real packaged text dependencies for detached history-owner replay."""
from pathlib import Path
from xml.sax.saxutils import escape
import shutil

def add_history_resources(root,out,project,language='zh-CN'):
 project=Path(project);out=Path(out);root=Path(root)
 text=project.read_text('utf-8')
 sources=['src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs','src/modules/AF.Module.Prompt/Composition/MemorySystemFactProjection.cs','src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs']
 items=''
 for source in sources:
  if source.replace('/','\\') not in text and source not in text:
   items+='<Compile Include="'+escape(str(root/source))+'" />'
 for folder,compat,name in [('AF.Module.Prompt','PromptCompatibility.json','AnimusForge.Prompt.Compatibility.json'),('AF.Module.Conversation','ConversationRuntimeCompatibility.json','AnimusForge.Conversation.RuntimeCompatibility.json')]:
  items+='<EmbeddedResource Include="'+escape(str(root/'content/modules'/folder/'ModuleData'/compat))+'"><LogicalName>'+name+'</LogicalName><WithCulture>false</WithCulture></EmbeddedResource>'
 items+='<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(root/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'))+'</HintPath></Reference>'
 project.write_text(text.replace('</Project>','<ItemGroup>'+items+'</ItemGroup></Project>'),encoding='utf-8')
 (out/'RuntimeResourcePath.cs').write_text('namespace AnimusForge; internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name)=>System.IO.Path.Combine(System.AppContext.BaseDirectory,name); }',encoding='utf-8')
 target=out/'bin/Release/net8.0';target.mkdir(parents=True,exist_ok=True)
 for folder,source,name in [('AF.Module.Prompt','PromptComposerTexts.'+language+'.json','PromptComposerTexts.json'),('AF.Module.Conversation','ConversationRuntimePrompts'+('.en' if language=='en' else '')+'.json','ConversationRuntimePrompts.json')]:
  shutil.copyfile(root/'content/modules'/folder/'ModuleData'/source,target/name)
