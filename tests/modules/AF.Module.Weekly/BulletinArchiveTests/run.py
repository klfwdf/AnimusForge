"""Replay actual archive/save/UI source with game and renderer boundaries stubbed."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, re, subprocess, sys
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument("--out",type=Path,required=True);args=parser.parse_args()
out=new_run_root(ROOT,"bulletin-archive",args.out);dotnet=resolve_dotnet(ROOT)
spec=importlib.util.spec_from_file_location("extract",ROOT/"tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py");extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
manifest=[]
def read(path):
 text=(ROOT/path).read_text(encoding="utf-8-sig");manifest.append({"path":path,"sha256":hashlib.sha256((ROOT/path).read_bytes()).hexdigest()});return text
def spans(text,names):return "\n".join(extract.declaration(text,name) for name in names)
host=read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs")
records=read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs")
editor=read("src/AF.GameAdapter.Bannerlord/UI/Editors/WeeklyEditorProjection.cs")
owner=read("src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs")
assert "day, WeeklyReportArchivePolicy.CaptureKingdomIds(selection), notify: false)" in owner, "Publish must persist selected kingdoms before notifying"
presentation=read("src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs")
timeline=read("src/AF.GameAdapter.Bannerlord/UI/WorldTimeline/WorldMessageTimelineUi.cs")
# Exact declarations and exact contiguous production methods, no body substitutions.
generated="using System;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Text;using System.Threading.Tasks;using TaleWorlds.CampaignSystem;using static AnimusForge.MyBehavior;namespace AnimusForge {public partial class MyBehavior {"
generated+=spans(host,["internal sealed class EventRecordEntry","public sealed class WeeklyReportBrowserEntryData","public sealed class WeeklyReportBrowserCountryData"])+extract.declaration(records,"internal sealed class EventImportPayload")+"}"
generated+=extract.declaration(editor,"internal class WeeklyEditorDisplayPort")+"internal static class WeeklyEditorProjection {"+spans(editor,["internal static WeeklyReportBrowserCountryData BuildWeeklyReportBrowserCountryData","internal static List<WeeklyReportBrowserEntryData> BuildWeeklyReportBrowserEntries","internal static string BuildWeeklyReportBrowserDefaultTitle"])+"}"
generated+="internal sealed class WorldBulletinStateOwner { internal WorldBulletinSaveState State; internal WorldBulletinPort _port;"+spans(owner,["internal void UpsertWorldBulletinRecord"])+spans(presentation,["internal IReadOnlyDictionary<string, List<string>> SnapshotLegacyBulletinKingdomAssociations"])+"}"
generated+=spans(timeline,["public sealed class WorldMessageTimelineEntryData","public sealed class WorldMessageTimelineCountryReference"])
constants="\n".join(re.search(r"(?:public|private) const (?:string|int) "+name+r" = [^;]+;",timeline).group() for name in ["WeeklyCategoryId","WorldWeeklyCountryId","UnknownCountryId","MaxWeeklySourceEntries","DetailCharacterLimit","DetailLineLimit"])
generated+="internal static class WorldMessageTimelineUi { "+constants+"internal static List<WorldMessageTimelineEntryData> Replay(){var result=new List<WorldMessageTimelineEntryData>();AppendWeeklyEntries(result);return result;}"+spans(timeline,["private static void AppendWeeklyEntries","private static void AddCountry","private static string FormatDay","private static string LimitMultiline","private static string FirstNonEmpty"])+"}}"
navigation=read("src/AF.GameAdapter.Bannerlord/UI/Common/EncyclopediaEntityLinkNavigationCoordinator.cs")
# Deferred request/admission code is real; native encyclopedia and its observation lifecycle are fixture boundaries.
generated+='namespace AnimusForge { internal static class EncyclopediaEntityLinkNavigationCoordinator { private static string _pendingLink; private static Action _pendingSuspend,_pendingResume,_activeResume; private static Func<bool> _pendingIsCurrent; private static long _processSequence; private static void ProcessActiveNavigation(){} private static void PrepareActiveNavigation(Action resume){_activeResume=resume;} private static void ResumeActiveNavigation(){var r=_activeResume;_activeResume=null;r?.Invoke();}'+spans(navigation,["internal static void Request","internal static void ProcessPending"])+"}}"
(out/"Extracted.cs").write_text(generated,encoding="utf-8")
paths=["src/modules/AF.Module.Weekly/Panel/WeeklyReportArchivePolicy.cs","src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs","src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs","src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs","src/AF.GameAdapter.Bannerlord/Persistence/CampaignWeeklyRecordPersistenceAdapter.cs","src/AF.Persistence/CampaignSaveChunkHelper.cs","src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs","src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs"]
paths.append("src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs")
paths.append("src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.Archive.cs")
for path in paths:read(path)
ui=ET.fromstring(read("content/modules/AF.Module.UI/GUI/Prefabs/AnimusForgeTerminalPopup.xml"))
period=next(node for node in ui.iter() if node.attrib.get("Text")=="@WeekText")
assert period.attrib.get("WidthSizePolicy")=="StretchToParent", "Issue/category labels need available row width"
country_list=next(node for node in ui.iter() if node.attrib.get("DataSource")=="{CountryItems}")
assert any(node.attrib.get("Command.Click")=="ExecuteSelect" for node in country_list.iter()), "Country select binding missing"
assert any(node.attrib.get("Text")=="@ReaderBodyText" for node in ui.iter()), "Independent reader binding missing"
open_button=next(node for node in country_list.iter() if node.attrib.get("Command.Click")=="ExecuteSelect")
assert open_button.attrib.get("DoNotPassEventsToChildren")=="true"
assert any(node.attrib.get("Text")=="@PreviewText" for node in ui.iter()), "Card preview missing"
print("PASS existing XML period width, country select and report body bindings (not rendering)")
newtonsoft=ROOT/"local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll"
if not newtonsoft.is_file():raise RuntimeError("Missing local Newtonsoft.Json reference")
links="".join('<Compile Include="'+str(ROOT/path)+'"/>' for path in paths)
links+="".join('<Compile Include="'+str(HERE/name)+'"/>' for name in ["Stubs.cs","Program.cs"])
(out/"Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+links+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(newtonsoft)+'</HintPath></Reference></ItemGroup></Project>',encoding="utf-8")
(out/"NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding="utf-8")
for name in ["Program.cs","Stubs.cs","run.py"]:read(str((HERE/name).relative_to(ROOT)).replace("\\","/"))
(out/"source-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
result=subprocess.run([str(dotnet),"run","--project",str(out/"Proof.csproj")],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding="utf-8",errors="replace")
(out/"result.log").write_text(result.stdout+result.stderr,encoding="utf-8");print(result.stdout+result.stderr);print("OUTPUT",out)
raise SystemExit(result.returncode)
