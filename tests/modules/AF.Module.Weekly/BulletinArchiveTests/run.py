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
sys.path.insert(0,str(ROOT/"tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests"))
from business_owner_fixture_support import enable_expression_declarations
enable_expression_declarations(extract)
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
generated="using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Text;using System.Threading.Tasks;using Newtonsoft.Json;using TaleWorlds.CampaignSystem;using TaleWorlds.Core;using TaleWorlds.Library;using AnimusForge.Refactor.Contracts;using static AnimusForge.MyBehavior;namespace AnimusForge {public partial class MyBehavior {"
generated+=spans(host,["internal sealed class EventRecordEntry","internal sealed class ApiCallResult","public sealed class WeeklyPromptSnapshot","public sealed class WorldWeeklyReportHistoryEntry","public sealed class WeeklyReportBrowserEntryData","public sealed class WeeklyReportBrowserCountryData"])+extract.declaration(records,"internal sealed class EventImportPayload")+"}"
# Persistence adapter binds this production data-only authority; no extra record dictionary.
weekly_state=read("src/modules/AF.Module.Weekly/Records/WeeklyEventRecordStateOwner.cs")
state_fields=weekly_state[weekly_state.index(" internal Dictionary<string, string> KingdomOpenings"):weekly_state.index(" internal long PublishedHistoryRevision")]
generated+="internal sealed class WeeklyEventRecordStateOwner {"+state_fields+extract.declaration(weekly_state,"internal IReadOnlyList<WorldWeeklyReportHistoryEntry> GetPublishedWorldWeeklyReportHistoryInternal")+"}"
# Real diplomacy adapter/knowledge capture and shared permission rules; engine identity/storage resolution is stubbed.
dip_host=read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs")
dip_adapter=read("src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs")
dip_memory=read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyMemoryApplication.cs")
dip_rules=read("src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs")
dip_notifications=read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyNotificationApplication.cs")
dip_presentation=read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.Presentation.cs")
generated+=extract.declaration(dip_notifications,"internal interface IWorldDiplomacyNotificationSink")
generated+="public sealed partial class WorldDiplomacyBehavior {"+spans(dip_host,["internal static bool TryCaptureMemory","private HashSet<string> GetKnownDocumentIdsForHero"])+extract.declaration(dip_presentation,"private sealed class NotificationWorld")+"}"
generated+=spans(dip_memory,["internal readonly struct WorldDiplomacyMemorySnapshot","internal interface IWorldDiplomacyMemorySource"])
generated+="internal sealed partial class WorldDiplomacyModuleAdapter {"+spans(dip_adapter,["private sealed class MemorySource","public System.Collections.Generic.ISet<string> CaptureKnownDocumentIds"])+"private static readonly IWorldDiplomacyMemorySource Memory = new MemorySource();}"
generated+="internal static class WorldDiplomacyRoundLifecycleRules {"+spans(dip_rules,["public static HashSet<string> CollectKnownDocumentIds","public static string FirstNonEmpty"])+"}"

generated+=extract.declaration(editor,"internal class WeeklyEditorDisplayPort")+"internal static class WeeklyEditorProjection {"+spans(editor,["internal static WeeklyReportBrowserCountryData BuildWeeklyReportBrowserCountryData","internal static List<WeeklyReportBrowserEntryData> BuildWeeklyReportBrowserEntries","internal static string BuildWeeklyReportBrowserDefaultTitle"])+"}"
generated+="internal sealed partial class WorldBulletinStateOwner { internal WorldBulletinSaveState State; internal WorldBulletinPort _port;"+spans(owner,["internal bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, params", "internal bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, WorldBulletinParticipant[]", "internal void CaptureCivilNewsMaterial", "internal void UpsertWorldBulletinRecord","internal WorldBulletinSaveState EnsureWorldBulletinState","internal void PublishWorldBulletin","internal async Task RunWorldBulletinRequestAsync","internal void CompleteWorldBulletin","internal void ProcessWorldBulletinMainThreadActions","internal void QueueNoticeAfterIllustration","private void ReleasePendingWorldBulletinNotice"])+spans(presentation,["internal IReadOnlyDictionary<string, List<string>> SnapshotLegacyBulletinKingdomAssociations"])+"}"
generated+=extract.declaration(owner,"internal sealed class WorldBulletinPort")+extract.declaration(owner,"internal sealed class WorldBulletinPromptFacts")
settings=read("src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs")
bulletin_settings=read("src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.BulletinPrompt.cs")
generated+='public partial class DuelSettings { private const int WorldDiplomacyPromptJsonVersion=3; private const long CustomPromptJsonMaxBytes=262144L; private static readonly Encoding CustomPromptStrictUtf8Encoding=new UTF8Encoding(false,true);'
for name in ["WorldBulletinWritingRequirementsJsonFileName","DefaultWorldBulletinWritingRequirements"]:
 generated+=re.search(r"private const string "+name+r" = [^;]+;",bulletin_settings).group()
generated+=spans(bulletin_settings,["private static string MigrateLegacyWorldBulletinWritingRequirements","private static string NormalizeWorldBulletinWritingRequirementsText"])
generated+=spans(settings,["private sealed class CustomPromptTextJson","private static bool TryReadCustomPromptTextJsonFile","private static bool TryReadLayeredCustomPromptTextJsonFile","private static bool IsCustomPromptTextFileTooLarge"])+"}"
npc_source=read("src/AF.GameAdapter.Bannerlord/Prompt/WorldBulletinNpcPromptCaptureAdapter.cs")
assert "latestVisible.Anecdote" in npc_source and "ProjectBulletinForNpc" in npc_source, "NPC must project the optional digest through source visibility"
packaged=read("content/modules/AF.Module.Weekly/CustomPrompts/WorldBulletinWritingRequirements.json")
(out/"packaged-default.json").write_text(packaged,encoding="utf-8")
generated+=spans(timeline,["public sealed class WorldMessageTimelineEntryData","public sealed class WorldMessageTimelineCountryReference"])
constants="\n".join(re.search(r"(?:public|private) const (?:string|int) "+name+r" = [^;]+;",timeline).group() for name in ["WeeklyCategoryId","WorldWeeklyCountryId","UnknownCountryId","MaxWeeklySourceEntries","DetailCharacterLimit","DetailLineLimit"])
generated+="internal static class WorldMessageTimelineUi { "+constants+"internal static List<WorldMessageTimelineEntryData> Replay(){var result=new List<WorldMessageTimelineEntryData>();AppendWeeklyEntries(result);return result;}"+spans(timeline,["private static void AppendWeeklyEntries","private static void AddCountry","private static string FormatDay","private static string LimitMultiline","private static string FirstNonEmpty"])+"}}"
navigation=read("src/AF.GameAdapter.Bannerlord/UI/Common/EncyclopediaEntityLinkNavigationCoordinator.cs")
# Deferred request/admission code is real; native encyclopedia and its observation lifecycle are fixture boundaries.
generated+='namespace AnimusForge { internal static class EncyclopediaEntityLinkNavigationCoordinator { private static string _pendingLink; private static Action _pendingSuspend,_pendingResume,_activeResume; private static Func<bool> _pendingIsCurrent; private static long _processSequence; private static void ProcessActiveNavigation(){} private static void PrepareActiveNavigation(Action resume){_activeResume=resume;} private static void ResumeActiveNavigation(){var r=_activeResume;_activeResume=null;r?.Invoke();}'+spans(navigation,["internal static void Request","internal static void ProcessPending"])+"}}"
capture_ports=read("src/AF.GameAdapter.Bannerlord/Prompt/WeeklyPromptCaptureAdapter.cs")
generated+='namespace AnimusForge.Refactor.Adapters { internal static class WeeklyPromptCaptureAdapter {'+extract.declaration(capture_ports,"internal sealed class CapturePorts")+'} internal static class MemoryEntityIdentityBannerlordAdapter {internal static int GetCurrentGameDayIndexSafe()=>5;internal static string GetKingdomId(Kingdom kingdom)=>kingdom?.StringId;} }'
(out/"Extracted.cs").write_text(generated,encoding="utf-8")
paths=["src/modules/AF.Module.Weekly/Panel/WeeklyReportArchivePolicy.cs","src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs","src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs","src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs","src/AF.GameAdapter.Bannerlord/Persistence/CampaignWeeklyRecordPersistenceAdapter.cs","src/AF.Persistence/CampaignSaveChunkHelper.cs","src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs","src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs"]
paths.append("src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs")
paths.append("src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.Archive.cs")
paths.append("src/modules/AF.Module.Diplomacy/Persistence/WorldDiplomacyPropagationRecords.cs")
paths.append("src/AF.GameAdapter.Bannerlord/Prompt/WorldBulletinNpcPromptCaptureAdapter.cs")
paths.append("src/AF.Contracts/Internal/WorldDiplomacyPresentationContracts.cs")
for path in paths:read(path)
ui=ET.fromstring(read("content/modules/AF.Module.UI/GUI/Prefabs/AnimusForgeTerminalPopup.xml"))
period=next(node for node in ui.iter() if node.attrib.get("Text")=="@WeekText")
assert period.attrib.get("WidthSizePolicy") in ("StretchToParent", "CoverChildren"), "Issue/category label must stretch or fit content without a fixed width cap"
country_list=next(node for node in ui.iter() if node.attrib.get("DataSource")=="{CountryItems}")
assert any(node.attrib.get("Command.Click")=="ExecuteSelect" for node in country_list.iter()), "Country select binding missing"
assert any(node.attrib.get("Text")=="@ReaderBodyText" for node in ui.iter()), "Independent reader binding missing"
open_button=next(node for node in country_list.iter() if node.attrib.get("Command.Click")=="ExecuteSelect")
assert open_button.attrib.get("DoNotPassEventsToChildren")=="true"
assert any(node.attrib.get("Text")=="@PreviewText" for node in ui.iter()), "Card preview missing"
print("PASS existing XML period width, country select and report body bindings (not rendering)")
newtonsoft=dotnet.parent/"sdk/8.0.425/Containers/tasks/net8.0/Newtonsoft.Json.dll"
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
