from pathlib import Path
import subprocess,sys,xml.etree.ElementTree as ET,json,re
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent;out=Path(sys.argv[1]).resolve();out.mkdir(parents=True,exist_ok=True)
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
for k,v in [('OutputType','Exe'),('TargetFramework','net8.0'),('LangVersion','latest'),('EnableDefaultCompileItems','false')]:ET.SubElement(props,k).text=v
# Execute the actual paste/length methods with a minimal text/selection host.
# Native input, undo, font/rendering and event dispatch remain outside this fixture.
widget=(R/'src/AF.GameAdapter.Bannerlord/UI/Common/DevMultilineEditableTextWidget.cs').read_text(encoding='utf-8-sig')
def method(name):
 match=re.search(r'(?m)^\s*private [^\n]+ '+re.escape(name)+r'\(',widget)
 assert match, name
 start=match.start();opening=widget.index('{',match.end());depth=1;end=opening+1
 while depth:
  depth += (widget[end]=='{')-(widget[end]=='}');end+=1
 return widget[start:end]
probe="""using System;
using AnimusForge;
sealed class WidgetInputProbe {
 private string _realText; public int MaxLength {get;set;}=60000;
 private int _start, _end; public Action Truncated; public int TruncationEvents;
 public string Text=>_realText;
 public WidgetInputProbe(string text,int start,int end){_realText=text;_start=start;_end=end;}
 public void Paste(string value)=>ReplaceSelection(SanitizeClipboardText(value));
 private int GetSelectionStart()=>_start;
 private int GetSelectionEnd()=>_end;
 private void ApplyTextChange(string text,int cursor,int anchor,bool recordUndo){_realText=text;_start=_end=cursor;}
 private void EventFired(string name){if(name=="InputTruncated"){TruncationEvents++;Truncated?.Invoke();}}
"""
probe+='\n'.join(method(n) for n in ['ReplaceSelection','NormalizeText','SanitizeClipboardText','GetAllowedExtraLength'])+'\n}'
(out/'WidgetInputProbe.cs').write_text(probe,encoding='utf-8')

items=ET.SubElement(project,'ItemGroup')
for f in [H/'Stubs.cs',H/'Program.cs',H/'LongTextSafetyReplay.cs',H/'LocalizationFixture.cs',out/'WidgetInputProbe.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeTextInputSanitizer.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Common/DevPopupInputLease.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Common/DevTextEditorHelper.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopup.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopupVM.cs']:ET.SubElement(items,'Compile',Include=str(f))
ET.ElementTree(project).write(out/'Tests.csproj',encoding='utf-8')
dotnet=Path(sys.argv[2]).resolve() if len(sys.argv)>2 else R/'local/dotnet/8.0.425/dotnet.exe'
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
if r.returncode:raise SystemExit(r.returncode)
xml=ET.parse(R/'content/modules/AF.Module.UI/GUI/Prefabs/DevHistoryEditPopup.xml')
editor=list(xml.iter('DevMultilineEditableTextWidget'))
assert len(editor)==1 and editor[0].get('AutoFocus')=='true' and editor[0].get('DoNotAcceptEvents')=='false'
buttons=list(xml.iter('ButtonWidget'));assert len(buttons)==2
for b in buttons:
 assert b.get('DoNotAcceptEvents')=='false' and b.get('DoNotPassEventsToChildren')=='true'
 assert all(t.get('DoNotAcceptEvents')=='true' for t in b.iter('TextWidget'))
print('PASS editor prefab focus/mouse/button contracts')

assert editor[0].get('Command.InputTruncated')=='NotifyInputTruncated'
assert len([w for w in xml.iter('TextWidget') if w.get('Text')=='@LengthStatusText'])==1
# Keep the empty editor inside the resized viewport (430 - 20 - 16 = 394).
assert int(editor[0].get('MinHeight'))+24 <= 430-20-16
keys=['AF_UI_Editor_LengthStatus','AF_UI_Editor_LengthLimit','AF_UI_Editor_InputTruncated','AF_UI_Editor_SaveFailed']
base=R/'content/modules/AF.Module.UI/ModuleData/Languages'
assets=[]
for path in [base/'af_ui_strings.xml',base/'CNs/af_ui_strings-zh-CN.xml']:
 entries=list(ET.parse(path).iter('string')); data={e.get('id'):e.get('text') for e in entries}
 for key in keys: assert sum(e.get('id')==key for e in entries)==1 and data[key].strip(),(path,key)
 assets.append(data)
for key in keys:
 assert set(re.findall(r'\{([A-Z_]+)\}',assets[0][key]))==set(re.findall(r'\{([A-Z_]+)\}',assets[1][key])),key
print('PASS truncation event/status binding, viewport and paired CN/EN editor resources')

layout=json.loads((R/'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
for source,target in [
 ('content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml','ModuleData/Languages/af_ui_strings.xml'),
 ('content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml','ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml'),
 ('content/modules/AF.Module.UI/GUI/Prefabs/DevHistoryEditPopup.xml','GUI/Prefabs/DevHistoryEditPopup.xml')]:
 assert sum(e.get('source')==source and e.get('target')==target for e in layout)==1,(source,target)
registrations=R/'content/foundation/AF.Foundation.Localization/ModuleData/Languages'
for path,filename in [(registrations/'language_data.xml','af_ui_strings.xml'),(registrations/'CNs/language_data.xml','af_ui_strings-zh-CN.xml')]:
 assert filename in path.read_text(encoding='utf-8-sig'),(path,filename)
print('PASS existing content-map and language registration reach both UI resources and editor prefab')
