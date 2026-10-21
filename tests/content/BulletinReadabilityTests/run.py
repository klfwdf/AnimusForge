"""Actual bulletin VM + font/scroll prefab contracts; native rendering is separate."""
from pathlib import Path
import argparse, importlib.util, json, subprocess, sys, xml.etree.ElementTree as ET
from xml.sax.saxutils import escape

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path,required=True);args=parser.parse_args()
out=new_run_root(ROOT,'bulletin-readability',args.out)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
source=(ROOT/'src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs').read_text('utf-8-sig')
declarations='\n'.join(extract.declaration(source,name) for name in ['internal sealed class WorldBulletinPanelData','public sealed class WorldBulletinMinorItemVM','public sealed class WorldBulletinPanelVM'])
(out/'Production.cs').write_text('using System;using System.Collections.Generic;using TaleWorlds.Library;using TaleWorlds.Localization;namespace AnimusForge {'+declarations+'}',encoding='utf-8')
(out/'Host.cs').write_text('''using System;using System.Collections.Generic;
namespace TaleWorlds.Library {
 [AttributeUsage(AttributeTargets.Property)]public sealed class DataSourceProperty:Attribute{}
 public class ViewModel {public string LastNotification; protected void OnPropertyChangedWithValue<T>(T value,string name){LastNotification=name;}}
 public class MBBindingList<T>:List<T>{}
}
namespace TaleWorlds.Localization {public class TextObject {readonly string value;public TextObject(string value){this.value=value;}public override string ToString()=>value.StartsWith("{=")?value.Substring(value.IndexOf('}')+1):value;}}
namespace AnimusForge {
 internal sealed class WorldBulletinIllustrationPlan{}
 public sealed class WorldBulletinIllustrationVM{}
 internal static class EncyclopediaEntityLinkFormatter {
  internal sealed class DisplaySession {internal string Format(string text)=>text;}
  internal static DisplaySession CreateDisplaySession()=>new();
  internal static string SanitizeUntrustedRichText(string text)=>text;
 }
}
''',encoding='utf-8')
(out/'Program.cs').write_text('''using System;using System.Collections.Generic;using System.Linq;using AnimusForge;
int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
foreach(var row in new[]{(-1,12),(12,12),(18,18),(24,24),(36,36),(99,36)}){
 string clicked=null;bool closed=false;string body="Article "+new string('x',8000);
 var data=new WorldBulletinPanelData{BodyText=body,Minors=new(){new("A","First minor"),new("B","Second minor"),new("C","Third minor")}};
 var vm=new WorldBulletinPanelVM(data,row.Item1,()=>closed=true,x=>clicked=x);
 Check(vm.BodyFontSize==row.Item2,"existing full setting range survives in lead VM");
 Check(vm.LeftMinors.Concat(vm.RightMinors).All(x=>x.BodyFontSize==row.Item2),"same setting reaches every minor datasource");
 Check(vm.BodyText==body,"large font never truncates article content");
 vm.BodyScrollPosition=63;Check(vm.BodyScrollPosition==63&&vm.LastNotification=="BodyScrollPosition","native scrollbar value notifies the actual VM binding");
 vm.MinorScrollPosition=35;Check(vm.MinorScrollPosition==35,"minor scroll remains independent");
 vm.LeftMinors[0].ExecuteOpenEncyclopediaLink("hero:id");Check(clicked=="hero:id","minor link preserved");
 vm.ExecuteOpenEncyclopediaLink("town:id");Check(clicked=="town:id","body link preserved");
 vm.ExecuteClose();Check(closed,"archive close callback preserved");
}
Console.WriteLine("PASS actual bulletin VM font/scroll checks="+checks+" native-rendering=NOT_RUN");
''',encoding='utf-8')
dotnet=resolve_dotnet(ROOT)
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
run=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj')],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'result.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);run.check_returncode()
ui=ET.parse(ROOT/'content/modules/AF.Module.Weekly/GUI/Prefabs/WorldBulletinPanel.xml').getroot()
def node(id):return next(n for n in ui.iter() if n.get('Id')==id)
bar=node('BodyScrollbar');scroll=bar.find('./Children/ScrollbarWidget');handle=scroll.find('./Children/Widget')
assert float(bar.get('SuggestedWidth'))>=6 and bar.get('IsVisible')!='false'
assert all(n.get('DoNotAcceptEvents')=='false' and n.get('IsVisible')!='false' for n in [bar,scroll,handle])
assert scroll.get('ValueFloat')=='@BodyScrollPosition' and scroll.get('Handle')==handle.get('Id')
assert handle.get('Sprite') and float(handle.get('SuggestedHeight'))>1
body_panel=next(n for n in ui.iter('ScrollablePanel') if n.get('VerticalScrollbar')=='..\\BodyScrollbar\\Scrollbar')
assert body_panel.get('ClipRect')=='BodyClip' and body_panel.get('InnerPanel')=='BodyClip\\BodyInner'
for n in [node('Lead'),node('Tail')]+[n for n in ui.iter('RichTextWidget') if n.get('Text')=='@Text']:
 assert n.get('Brush.FontSize')=='@BodyFontSize'
 assert n.get('Command.LinkClick')=='ExecuteOpenEncyclopediaLink' and n.get('DoNotAcceptEvents')=='false'
assert 's => s.WeeklyReportPopupBodyFontSize' in (ROOT/'src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs').read_text('utf-8-sig')
assert 'Report popup body font size", 12, 36' in (ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs').read_text('utf-8-sig')
print('PASS visible body scrollbar, matching clip/inner/handle/value bindings, shared lead/minor font and original terminal/MCM owner; rendering NOT_RUN')
