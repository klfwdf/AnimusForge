from pathlib import Path
import importlib.util,subprocess,sys,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[4]; here=Path(__file__).resolve().parent
out=Path(sys.argv[1]).resolve();out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('extract',root/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
s=(here/'Fixture.cs.in').read_text(encoding='utf-8')
for token,path,signature in [('__OPEN__','UI/Gallery/IllustratorGalleryPopup.cs','private void OpenCurrentImageEditor('),('__OPEN_PROMPT__','UI/Overlays/IllustrationCardPopup.cs','private void OpenRedrawPromptEditor('),('__DEFAULT__','UI/Gallery/IllustratorGalleryPopupVM.cs','private void ExecuteSetDefaultCore('),('__PUBLISH__','Core/IllustratorRuntime.cs','internal static void PublishDefaultImageChanged('),('__CARD_DEFAULT__','UI/Overlays/IllustrationCardPopup.cs','private void OnDefaultImageChanged('),('__WEEKLY_DEFAULT__','UI/Patches/WeeklyReportPopupIllustrationPatch.cs','private static void OnDefaultImageChanged(')]:
 s=s.replace(token,extract.declaration((root/'extensions/AnimusForge.Illustrator/src'/path).read_text(encoding='utf-8-sig'),signature))
(out/'Program.cs').write_text(s,encoding='utf-8')
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
for k,v in [('OutputType','Exe'),('TargetFramework','net472'),('LangVersion','latest')]:ET.SubElement(props,k).text=v
items=ET.SubElement(project,'ItemGroup')
for f in [str(root/'src/AF.GameAdapter.Bannerlord/UI/Common/DevPopupInputLease.cs'),'Engine/DiskImageCacheManager.cs','Engine/ImagePayload.cs','Core/CurrentImageRedraw.cs','Core/IllustrationReferenceImage.cs','UI/Overlays/IllustrationRedrawPromptEditor.cs']:ET.SubElement(items,'Compile',Include=str(root/'extensions/AnimusForge.Illustrator/src'/f))
for f in ['System.Drawing','System.Net.Http']:ET.SubElement(items,'Reference',Include=f)
ref=ET.SubElement(items,'Reference',Include='Newtonsoft.Json');ET.SubElement(ref,'HintPath').text=str(root/'_deps_auto/Newtonsoft.Json.dll')
ET.ElementTree(project).write(out/'Tests.csproj',encoding='utf-8')
subprocess.run(['dotnet','build',str(out/'Tests.csproj'),'-o',str(out/'bin')],check=True)
subprocess.run([str(out/'bin/Tests.exe'),str(out/'images')],check=True)
xml=ET.parse(root/'extensions/AnimusForge.Illustrator/GUI/Prefabs/IllustratorGalleryPopup.xml')
b=[v for v in xml.iter('ButtonWidget') if v.get('Command.Click')=='ExecuteRedrawBasedOnImage']
assert len(b)==1 and b[0].get('IsEnabled')=='@CanRedrawBasedOnImage'
print('PASS gallery XML redraw binding')
