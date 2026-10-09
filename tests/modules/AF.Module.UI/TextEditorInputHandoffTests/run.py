from pathlib import Path
import subprocess,sys,xml.etree.ElementTree as ET,json,re
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent;out=Path(sys.argv[1]).resolve();out.mkdir(parents=True,exist_ok=True)
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
for k,v in [('OutputType','Exe'),('TargetFramework','net8.0'),('LangVersion','latest'),('EnableDefaultCompileItems','false')]:ET.SubElement(props,k).text=v
items=ET.SubElement(project,'ItemGroup')
for f in [H/'Stubs.cs',H/'Program.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Common/DevPopupInputLease.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Common/DevTextEditorHelper.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopup.cs',R/'src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopupVM.cs']:ET.SubElement(items,'Compile',Include=str(f))
ET.ElementTree(project).write(out/'Tests.csproj',encoding='utf-8')
dotnet=R/'local/dotnet/8.0.425/dotnet.exe'
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
