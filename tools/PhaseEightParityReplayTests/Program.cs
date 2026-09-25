using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); }
static object Get(object value, string name) => value.GetType().GetProperty(name, Members)?.GetValue(value)
    ?? value.GetType().GetField(name, Members)?.GetValue(value);
static void Set(object value, string name, object fieldValue)
{
    var property = value.GetType().GetProperty(name, Members);
    if (property != null) property.SetValue(value, fieldValue);
    else value.GetType().GetField(name, Members).SetValue(value, fieldValue);
}
static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Members).Invoke(value, args);

string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
if (args.Length != 2) throw new InvalidOperationException("Pass the current candidate DLL path and its expected SHA256 after --.");
string dll = Path.GetFullPath(args[0]);
string expectedDll = Path.GetFullPath(Path.Combine(repo, "bin/Debug/single_module_artifacts/versions/1.4/AnimusForge.dll"));
Check(string.Equals(dll, expectedDll, StringComparison.OrdinalIgnoreCase), "PhaseEight candidate must be the current project-local Debug 1.4 artifact");
string marker = Path.ChangeExtension(dll, ".build.json");
Check(File.Exists(dll) && File.Exists(marker), "current candidate DLL/marker missing");
using (JsonDocument build = JsonDocument.Parse(File.ReadAllText(marker)))
{
    JsonElement record = build.RootElement;
    string actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll)));
    Check(string.Equals(actualHash, args[1], StringComparison.OrdinalIgnoreCase)
        && string.Equals(actualHash, record.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase),
        "candidate SHA256 differs from requested build marker");
    Check(record.GetProperty("Role").GetString() == "Implementation"
        && record.GetProperty("BannerlordApi").GetString() == "1.4"
        && record.GetProperty("BuildFlavor").GetString() == "ANIMUSFORGE_BANNERLORD_API_1_4",
        "candidate build identity mismatch");
    DateTime created = record.GetProperty("CreatedUtc").GetDateTime().ToUniversalTime();
    foreach (string source in new[] {
        "WorldEvents/WorldEventInbox.cs", "src/modules/AF.Module.WorldEvents/WorldEventInboxOwner.cs",
        "WarStats/AfWarStatsBehavior.cs", "src/modules/AF.Module.WarStats/WarStatsLedgerOwner.cs",
        "DuelBehavior.cs", "DuelBehavior.Outcomes.cs", "src/modules/AF.Module.Duel/DuelBehavior.DispatchOwner.cs",
        "src/modules/AF.Module.Duel/DuelSettlementEffectOwner.cs",
        "SceneTauntBehavior.cs", "src/modules/AF.Module.Taunt/ScenePeaceConflictContextOwner.cs",
        "LordEncounterBehavior.cs", "EncounterConversationTargetResolver.cs",
        "SettlementEntryTroopSelectionBehavior.cs",
        "src/modules/AF.Module.Settlement/SettlementMissionEntryOwner.cs",
        "src/modules/AF.Module.Settlement/SettlementFollowerMissionOwner.cs",
        "TroopInspectionBehavior.cs", "src/modules/AF.Module.Settlement/TroopInspectionSessionOwner.cs",
        "src/modules/AF.Module.Encounter/EncounterTargetOwner.cs",
        "src/modules/AF.Module.Encounter/EncounterConversationTargetOwner.cs",
        "src/modules/AF.Module.Encounter/EncounterReleaseOwner.cs",
        "src/modules/AF.Module.Encounter/EncounterPendingReturnOwner.cs",
        "src/modules/AF.Module.Taunt/SceneTauntPenaltyLedgerOwner.cs",
        "src/modules/AF.Module.Taunt/SceneTauntConflictLifecycleOwner.cs",
        "RewardSystemBehavior.cs", "src/modules/AF.Module.Social/Recruitment/RecruitmentOwner.cs",
        "ProactiveNpcRequestBehavior.cs", "src/modules/AF.Module.Social/Proactive/ProactiveOpeningOwner.cs",
        "src/modules/AF.Module.Social/Proactive/ProactiveRequestCooldownOwner.cs",
        "src/modules/AF.Module.Social/Proactive/ProactiveCandidateScanOwner.cs",
        "src/modules/AF.Module.Social/Proactive/ProactiveRequestSessionOwner.cs",
        "src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs",
        "VanillaIssueOfferBridge.cs", "VanillaIssuePromptBehavior.cs", "src/modules/AF.Module.Issue/Dispatch/IssueAlternativeDispatchOwner.cs",
        "src/modules/AF.Module.Issue/Completion/IssueCompletionReceiptOwner.cs",
        "src/modules/AF.Module.Issue/Runtime/IssueRuntimeStateOwner.cs",
        "src/modules/AF.Module.Issue/Runtime/IssueRuntimePromptOwner.cs",
        "src/modules/AF.Module.Issue/Actions/IssueActionOwner.cs",
        "src/modules/AF.Module.Issue/Actions/IssueTurnInDecisionOwner.cs",
        "MyBehavior.PromotedPersonaGeneration.cs",
        "RomanceSystemBehavior.cs", "src/modules/AF.Module.Social/Romance/RomanceRelationshipOwner.cs",
        "PlayerNotorietyBehavior.cs", "src/modules/AF.Module.Social/Notoriety/NotorietyObservationOwner.cs",
        "MyBehavior.PersonaGeneration.cs", "MyBehavior.PersonaReadiness.cs",
        "src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs",
        "src/modules/AF.Module.Persona/Generation/NpcPersonaProfilePolicy.cs",
        "src/modules/AF.Module.Kingdom/Stability/KingdomStabilityOwner.cs",
        "src/modules/AF.Module.Kingdom/Stability/KingdomStabilityPolicy.cs",
        "src/modules/AF.Module.Kingdom/Scheduling/KingdomMaintenanceOwner.cs",
        "src/modules/AF.Module.Kingdom/Scheduling/AutomaticKingdomRebellionOwner.cs" })
        Check(created >= File.GetLastWriteTimeUtc(Path.Combine(repo, source)), "candidate predates " + source);
    Check(created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "MyBehavior.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Generation/WeeklyFullReportCompletionOwner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Scheduling/WeeklyAutoScheduleOwner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Materials/WeeklyMaterialAggregationOwner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Materials/WeeklyActionMaterialCursor.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Materials/WeeklyMaterialStageCursor.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitQueueOwner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Generation/WeeklyReportWaveCoordinator.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Generation/WeeklyReportBlockMaterialCursor.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitTargetOwner.cs"))
        && created >= File.GetLastWriteTimeUtc(Path.Combine(repo, "src/modules/AF.Module.Weekly/Generation/WeeklyReportMaterialRevisionOwner.cs")),
        "candidate predates J13a production source");
    Console.WriteLine("PhaseEight candidate SHA256=" + actualHash);
}
AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
{
    string candidate = Path.Combine(AppContext.BaseDirectory, new AssemblyName(args.Name).Name + ".dll");
    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
};
Assembly af = Assembly.LoadFrom(dll);
WeeklyActionOutcomeProductionReplay.Run(af);
KingdomOwnerReplay.Run(af);
NotorietyOwnerReplay.Run(af);
RomanceOwnerReplay.Run(af);
RecruitmentOwnerReplay.Run(af);
ProactiveOpeningOwnerReplay.Run(af);
ProactiveCooldownOwnerReplay.Run(af);
ProactiveCandidateScanOwnerReplay.Run(af);
ProactiveSessionOwnerReplay.Run(af);
ProactiveQualificationReplay.Run(af, repo);
IssueAlternativeDispatchOwnerReplay.Run(af);
IssueCompletionReceiptOwnerReplay.Run(af, repo);
IssueRuntimeStateOwnerReplay.Run(af, repo);
IssueActionOwnerReplay.Run(af, repo);
J13D2DomainOwnerContractReplay.Run(repo);
WorldEventInboxOwnerReplay.Run(af);
J13D3DomainOwnerContractReplay.Run(repo);
J13D4DomainOwnerContractReplay.Run(repo);
J13E1DomainOwnerContractReplay.Run(repo);
DuelDispatchOwnerReplay.Run(af);
ScenePeaceConflictOwnerReplay.Run(af, repo);
SceneTauntPenaltyLedgerOwnerReplay.Run(af);
SceneTauntConflictLifecycleOwnerReplay.Run(af);
J13E2DomainOwnerContractReplay.Run(repo);
EncounterTargetOwnerReplay.Run(af);
EncounterConversationTargetOwnerReplay.Run(af);
EncounterReleaseOwnerReplay.Run(af);
EncounterPendingReturnOwnerReplay.Run(af);
J13E3DomainOwnerContractReplay.Run(repo);
SettlementMissionEntryOwnerReplay.Run(af);
SettlementFollowerMissionOwnerReplay.Run(af);
TroopInspectionSessionOwnerReplay.Run(af);
J13E4DomainOwnerContractReplay.Run(repo);
NotorietyConversationOutcomeProductionReplay.Run(af);
Type nodeType = af.GetType("AnimusForge.AnimusForgeTerminalNode", true);
Type vmType = af.GetType("AnimusForge.AnimusForgeTerminalPopupVM", true);
IList roots = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(nodeType));
int selected = -1, closed = 0;
for (int i = 0; i < 123; i++)
{
    int id = i;
    object node = Activator.CreateInstance(nodeType);
    Set(node, "Id", id.ToString()); Set(node, "Title", "NPC-" + id.ToString("D3"));
    Set(node, "Hint", "fixture trust " + id); Set(node, "OnExecute", (Action)(() => selected = id));
    roots.Add(node);
}
object vm = Activator.CreateInstance(vmType, roots, (Func<string, bool>)(_ => false), (Action)(() => closed++));
Call(vm, "ShowBrowser", "query-fixture", roots);
Check(((IList)Get(vm, "Items")).Count == 50, "first page bounded");
Call(vm, "ExecuteNextMenuPage"); Call(vm, "ExecuteNextMenuPage");
Check(((IList)Get(vm, "Items")).Count == 23 && !(bool)Get(vm, "HasNextMenuPage"), "last page includes all remaining identities");
Call(((IList)Get(vm, "Items"))[22], "ExecuteOpen"); Check(selected == 122, "selection beyond legacy truncation reaches exact identity");
Set(vm, "SearchText", "NPC-122");
Check(((IList)Get(vm, "Items")).Count == 1 && !(bool)Get(vm, "HasPreviousMenuPage"), "search spans full snapshot and resets page");
Call(vm, "ShowDetails", "trust", "merchant and hero detail fixture");
Check((bool)Get(vm, "IsDetailsVisible"), "details routed");
Call(vm, "ExecuteBack");
Check((bool)Get(vm, "IsMenuListVisible") && (string)Get(vm, "SearchText") == "NPC-122", "details back preserves query");
Set(vm, "SearchText", "no-such-NPC"); Check(((IList)Get(vm, "Items")).Count == 0, "empty search safe");
Call(vm, "ShowBrowser", "Subjects", roots);
Set(vm, "SearchText", "NPC-090"); Call(((IList)Get(vm, "Items"))[0], "ExecuteOpen");
Check(selected == 90, "subject selector keeps callback identity");
object historyData = Activator.CreateInstance(af.GetType("AnimusForge.TerminalTributaryPaymentHistoryData", true));
Call(vm, "ShowVassalageTributeHistory", historyData);
Check((bool)Get(Get(vm, "VassalageVm"), "ShowEmptyState"), "empty tribute history retains explanatory state");
Call(vm, "ExecuteBack"); Check((string)Get(vm, "SearchText") == "NPC-090", "tribute back returns to selected browser");
Call(vm, "ExecuteBack"); Check(((IList)Get(vm, "Items")).Count == 50, "browser back returns root");
Call(vm, "SelectTab", "系统"); Check(!(bool)Get(vm, "HasPreviousMenuPage"), "tab change resets paging");
Type snapshotType = af.GetType("AnimusForge.AnimusForgeTagCatalogSnapshot", true);
Type entryType = af.GetType("AnimusForge.AnimusForgeTagCatalogEntry", true);
object snapshot = Activator.CreateInstance(snapshotType, true);
IList entries = (IList)Get(snapshot, "Entries");
for (int i = 0; i < 73; i++)
{
    object entry = Activator.CreateInstance(entryType, true);
    Set(entry, "Id", "tag-" + i); Set(entry, "Tag", "[ACTION:fixture-" + i + "]");
    Set(entry, "Category", "后处理/规则表");
    Set(entry, "Description", new string('x', 300) + " 参数尾部-" + i);
    IList sources = (IList)Get(entry, "Sources");
    for (int j = 0; j < 15; j++) sources.Add("source-" + i + "-" + j);
    entries.Add(entry);
}
IList sourceRoots = (IList)Get(snapshot, "SourceRoots");
for (int i = 0; i < 4; i++) sourceRoots.Add("fixture-root-" + i);
Set(snapshot, "ScannedFileCount", 81);
Check((int)Get(Get(vm, "_path"), "Count") == 0, "system tab starts at navigation root");
Call(vm, "ShowTagCatalog", snapshot);
Check((int)Get(Get(vm, "_path"), "Count") == 1, "opening tag browser pushes exactly one navigation frame");
Check((bool)Get(vm, "IsMenuListVisible") && ((IList)Get(vm, "Items")).Count == 50, "tag catalog reuses bounded menu");
Check(((string)Get(vm, "TagCatalogStatusText")).Contains("73") && ((string)Get(vm, "TagCatalogStatusText")).Contains("更新"), "refresh feedback shows snapshot size and update time");
Check((bool)Get(vm, "IsTagCatalogBrowser") && (float)Get(vm, "MenuContentTop") == 98f, "tag commands have dedicated nonoverlapping header");
Call(vm, "ExecuteNextMenuPage"); Check(((IList)Get(vm, "Items")).Count == 23, "tag pagination covers all 73 entries");
Set(vm, "SearchText", "参数尾部-72"); Check(((IList)Get(vm, "Items")).Count == 1, "search includes description beyond card truncation");
Set(vm, "SearchText", "source-72-14"); Check(((IList)Get(vm, "Items")).Count == 1, "search includes all source paths");
Call(((IList)Get(vm, "Items"))[0], "ExecuteOpen");
string details = (string)Get(vm, "DetailText");
Check(details.Contains("[ACTION:fixture-72]") && details.Contains("参数尾部-72") && details.Contains("source-72-14"), "tag detail includes full description and more than 12 sources");
Call(vm, "ExecuteBack"); Check((string)Get(vm, "SearchText") == "source-72-14" && (bool)Get(vm, "IsTagCatalogBrowser"), "tag details back preserves filter");
Call(vm, "ExecuteTagCatalogInfo");
Check(((string)Get(vm, "DetailText")).Contains("fixture-root-3") && ((string)Get(vm, "DetailText")).Contains("81"), "index summary retains scan count and all roots");
Call(vm, "ExecuteBack");
Check(ReferenceEquals(Get(vm, "_tagCatalogSnapshot"), snapshot), "export is bound to displayed snapshot rather than fresh scan");
Type catalogType = af.GetType("AnimusForge.AnimusForgeTagCatalog", true);
string exportText = (string)catalogType.GetMethod("BuildExportText", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { snapshot, "fixture-root" });
Check(exportText.Contains("[ACTION:fixture-0]") && exportText.Contains("[ACTION:fixture-72]") && exportText.Contains("source-72-14"), "export formatter retains unfiltered snapshot without file IO");
object emptySnapshot = Activator.CreateInstance(snapshotType, true);
Call(vm, "ShowTagCatalog", emptySnapshot);
Check((int)Get(Get(vm, "_path"), "Count") == 1, "refresh replaces contents without pushing another navigation frame");
// Deliberately duplicate a fixture frame: the navigation invariant must detect it independently of layout.
object navigation = Get(vm, "_path");
Call(navigation, "Push", Get(vm, "_tagBrowser"));
Check((int)Get(navigation, "Count") != 1, "mutation: duplicate browser frame is observable");
Call(navigation, "Pop");
Check((int)Get(navigation, "Count") == 1, "navigation fixture restored after mutation");
Check(((IList)Get(vm, "Items")).Count == 0 && (bool)Get(vm, "IsTagCatalogBrowser"), "refresh to empty index remains navigable");
Call(vm, "ExecuteExportTagCatalog"); // Empty snapshot fails before module-root resolution or any write.
Check((bool)Get(vm, "IsDetailsVisible") && ((string)Get(vm, "DetailText")).Contains("没有可导出"), "export failure is visible and actionable");
Call(vm, "ExecuteBack"); Check((bool)Get(vm, "IsTagCatalogBrowser"), "export failure returns to index");
Call(vm, "ExecuteBack");
Check(!(bool)Get(vm, "IsTagCatalogBrowser") && (int)Get(Get(vm, "_path"), "Count") == 0,
    "refresh navigation follows 0-1-1-0 and returns to the original tab root");
Check(!(bool)Get(vm, "IsSearchVisible") && (float)Get(vm, "MenuContentTop") == 6f,
    "system tab without search uses the compact nonoverlapping header");
Call(vm, "SelectTab", "全部");
Check((bool)Get(vm, "IsSearchVisible") && (float)Get(vm, "MenuContentTop") == 50f,
    "all tab with search retains its dedicated header space");
Call(vm, "SelectTab", "系统");
Call(vm, "ExecuteExportTagCatalog"); Check((bool)Get(vm, "IsMenuListVisible"), "stale hidden export command is ignored");
Type countryType = af.GetType("AnimusForge.MyBehavior+WeeklyReportBrowserCountryData", true);
Type reportType = af.GetType("AnimusForge.MyBehavior+WeeklyReportBrowserEntryData", true);
Type weeklyType = af.GetType("AnimusForge.TerminalWeeklyReportBrowserPopupVM", true);
IList countries = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(countryType));
object Country(string id, string name, bool world)
{
    object country = Activator.CreateInstance(countryType);
    Set(country, "CountryId", id); Set(country, "DisplayName", name); Set(country, "IsWorld", world);
    countries.Add(country);
    return country;
}
object Report(object country, string id, int week, int day, string body, string tag, bool full)
{
    object report = Activator.CreateInstance(reportType);
    Set(report, "EventId", id); Set(report, "Title", id); Set(report, "WeekIndex", week);
    Set(report, "CreatedDay", day); Set(report, "CreatedDate", "fixture-day-" + day);
    Set(report, "BodyText", body); Set(report, "TagText", tag); Set(report, "HasFullReport", full);
    ((IList)Get(country, "Reports")).Add(report);
    return report;
}
object world = Country("world", "世界周报", true);
Report(world, "world-only", 10, 80, "世界全文", "STAB_FLAT", true);
object kingdom = Country("kingdom:test-a", "测试王国", false);
Report(kingdom, "older", 2, 20, "旧正文", "STAB_UP_2", true);
Report(kingdom, "same-week-earlier", 9, 70, "摘要", "STAB_DOWN_1", false);
Report(kingdom, "newest", 9, 75, new string('文', 12000) + "全文最后一行。", "STAB_FLAT", true);
((IList)Get(kingdom, "Reports")).Add(null);
Country("kingdom:empty", "空记录王国", false);
Call(vm, "SelectTab", "全部"); Set(vm, "SearchText", "NPC-090");
Call(vm, "ShowWeeklyReports", countries);
object weekly = Get(vm, "WeeklyReportVm");
Check((bool)Get(vm, "IsWeeklyReportsVisible") && (string)Get(weekly, "SelectedCountryNameText") == "测试王国", "weekly default selects a reporting kingdom before world");
IList reports = (IList)Get(weekly, "ReportItems");
Check(reports.Count == 3 && (string)Get(reports[0], "EventId") == "newest" && (string)Get(reports[1], "EventId") == "same-week-earlier", "weekly full history ignores null entries and sorts week then date descending");
Check(((string)Get(reports[0], "BodyText")).Contains("全文最后一行。") && ((string)Get(reports[0], "BodyText")).Length > 12000, "weekly body is never truncated to summary");
Check(!(bool)Get(reports[0], "ShowViewFullReport") && (bool)Get(reports[1], "ShowViewFullReport"), "weekly complete and summary actions differ");
Check((bool)Get(reports[0], "ShowNeutralTag") && (bool)Get(reports[1], "ShowNegativeTag") && (bool)Get(reports[2], "ShowPositiveTag"), "weekly stability tags retain all three display kinds");
Check(((string)Get(weekly, "SelectedCountryMetaText")).Contains("3") && ((string)Get(reports[0], "DateText")).Contains("75"), "weekly count and date metadata preserved");
IList countryItems = (IList)Get(weekly, "CountryItems");
Call(countryItems[0], "ExecuteSelect");
Check((string)Get(weekly, "SelectedCountryNameText") == "世界周报" && (string)Get(((IList)Get(weekly, "ReportItems"))[0], "EventId") == "world-only", "weekly world selection keeps exact report identity");
Call(countryItems[2], "ExecuteSelect");
Check((bool)Get(weekly, "ShowEmptyState") && !(bool)Get(weekly, "HasReportItems") && !string.IsNullOrWhiteSpace((string)Get(weekly, "EmptyStateText")), "weekly country without reports has visible explanation");
Call(weekly, "ExecuteClose");
Check((bool)Get(vm, "IsMenuListVisible") && (string)Get(vm, "SearchText") == "NPC-090", "weekly back preserves original terminal tab and query");
Call(vm, "ShowWeeklyReports", countries);
Check((bool)Get(weekly, "_isFinalized"), "reopening weekly finalizes previous model");
weekly = Get(vm, "WeeklyReportVm");
object summary = ((IList)Get(weekly, "ReportItems"))[1];
var completion = new System.Threading.Tasks.TaskCompletionSource<bool>();
Set(weekly, "_pendingFullReport", completion.Task); Set(weekly, "_requestItem", summary);
Set(summary, "ShowViewFullReport", false);
string beforeTick = (string)Get(weekly, "SelectedCountryMetaText");
Call(weekly, "Tick"); Check((string)Get(weekly, "SelectedCountryMetaText") == beforeTick, "weekly pending generation does not block tick");
completion.SetResult(false); Call(weekly, "Tick");
Check(((string)Get(weekly, "SelectedCountryMetaText")).Contains("原有内容已保留") && (bool)Get(summary, "ShowViewFullReport"), "weekly failed generation keeps contents and restores retry on tick");
Set(weekly, "_pendingFullReport", System.Threading.Tasks.Task.FromResult(false));
Call(weekly, "OnFinalize"); Call(weekly, "Tick");
Check(Get(weekly, "_pendingFullReport") == null, "weekly closed model drops pending UI completion");
var emptyCountries = (IList)Activator.CreateInstance(countries.GetType());
object emptyWeekly = Activator.CreateInstance(weeklyType, emptyCountries, null, (Action)(() => {}));
Check((bool)Get(emptyWeekly, "ShowEmptyState") && ((IList)Get(emptyWeekly, "CountryItems")).Count == 0, "weekly no countries is safe and navigable");
Call(emptyWeekly, "OnFinalize");
object selectedWeekly = Activator.CreateInstance(weeklyType, countries, "world", (Action)(() => {}));
Check((string)Get(selectedWeekly, "SelectedCountryNameText") == "世界周报", "weekly explicit selected country preserved");
Call(selectedWeekly, "OnFinalize");
XElement weeklyView = XDocument.Load(Path.Combine(repo, "AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml"))
    .Descendants().Single(element => (string)element.Attribute("IsVisible") == "@IsWeeklyReportsVisible");
var weeklyBindings = weeklyView.DescendantsAndSelf().Attributes().Select(attribute => attribute.Value).ToList();
foreach (string binding in new[] { "@BodyText", "@BodyFontSize", "@WeekText", "@DateText", "@TagText", "@ShowViewFullReport", "ExecuteViewFullReport" })
    Check(weeklyBindings.Contains(binding), "weekly XML binds " + binding);
static bool HasWeeklyModelBinding(XElement view, string binding) => view.DescendantsAndSelf()
    .Where(element => element.Attributes().Any(attribute => attribute.Value == binding))
    .Any(element => (string)element.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor.Attribute("DataSource") != null)?.Attribute("DataSource") == "{WeeklyReportVm}");
foreach (string property in new[] { "EmptyStateText", "SelectedCountryMetaText" })
{
    Check(weeklyType.GetProperty(property, Members) != null && HasWeeklyModelBinding(weeklyView, "@" + property),
        "weekly XML binds a real property inside the WeeklyReportVm data-source scope: " + property);
}
var wrongWeeklyScope = new XElement(weeklyView);
wrongWeeklyScope.Descendants().Single(element => (string)element.Attribute("DataSource") == "{WeeklyReportVm}")
    .SetAttributeValue("DataSource", "{WarStatsVm}");
Check(!HasWeeklyModelBinding(wrongWeeklyScope, "@EmptyStateText"), "mutation: same field in wrong view-model scope must fail");
Check(!weeklyBindings.Contains("@SummaryText"), "weekly XML does not bind nonexistent summary field");
Check(af.GetType("AnimusForge.TerminalWeeklyReportBrowserPopup", false) == null && af.GetType("AFWarStatsTerminal.UI.AfWarStatsPopup", false) == null, "replaced independent popup implementations removed");
Call(vm, "ExecuteBack"); Call(vm, "ShowDiagnostics", "status", "detail", false);
Call(vm, "ExecuteRequestAiAnalysis"); Check(closed == 0, "no-error analysis action is inert");
Call(vm, "ExecuteBack"); Check((string)Get(vm, "SearchText") == "NPC-090", "diagnostics back preserves menu state");
// Merge regression: upstream settings remain reachable alongside local query browsers.
Type registryType = af.GetType("AnimusForge.TerminalSettingsRegistry", true);
IList definitions = (IList)registryType.GetProperty("AllDefinitions").GetValue(null);
Check(definitions.Count > 0, "upstream setting definitions retained");
Check(definitions.Cast<object>().Select(d => (string)Get(d, "Id")).Distinct().Count() == definitions.Count, "setting IDs remain unique");
Call(vm, "SelectTab", "AI核心");
var expectedSettingIds = definitions.Cast<object>().Where(d => (string)Get(d, "TabCategory") == "AI核心").Select(d => (string)Get(d, "Id")).ToHashSet();
var visibleSettingIds = new System.Collections.Generic.HashSet<string>();
while (true)
{
    foreach (object item in (IList)Get(vm, "Items"))
        if (Get(item, "SettingDef") is object def) visibleSettingIds.Add((string)Get(def, "Id"));
    if (!(bool)Get(vm, "HasNextMenuPage")) break;
    Call(vm, "ExecuteNextMenuPage");
}
Check(expectedSettingIds.Count > 0 && expectedSettingIds.SetEquals(visibleSettingIds), "every upstream AI setting is accessible across pages");
Check(!(bool)Get(vm, "HasUnsavedChanges"), "browsing settings must not mark unchanged values dirty");
Call(vm, "SelectTab", "全部");
object hotkeyDef = definitions.Cast<object>().First(d => Get(d, "SettingType").ToString() == "Hotkey");
string hotkeyId = (string)Get(hotkeyDef, "Id");
Set(vm, "SearchText", hotkeyId);
object hotkeyItem = ((IList)Get(vm, "Items")).Cast<object>().First(item => Get(item, "SettingDef") is object def && (string)Get(def, "Id") == hotkeyId);
Check(!(bool)Get(vm, "HasPreviousMenuPage"), "global setting search resets page and expands matched groups");
Call(vm, "StartListeningKey", hotkeyItem); Check((bool)Get(vm, "IsListeningForKey"), "hotkey capture started");
Call(vm, "CancelListeningKey"); Check(!(bool)Get(vm, "IsListeningForKey"), "hotkey capture cancels without editing settings");
Call(vm, "StartListeningKey", hotkeyItem); Call(vm, "SelectTab", "全部");
Check(!(bool)Get(vm, "IsListeningForKey"), "navigation cannot leave an invisible key listener");
Check(vmType.GetMethod("ExecuteSaveSettings") != null && af.GetType("AnimusForge.DuelSettings", true).GetMethod("SaveCurrentSettings") != null, "upstream save entrypoints retained without writing settings");
Check(af.GetType("AnimusForge.AnimusForgeApiOnboardingPopup", false) != null && af.GetType("AnimusForge.AnimusForgeApiOnboardingVM", false) != null, "upstream API wizard types retained");
XDocument.Load(Path.Combine(repo, "AnimusForge/GUI/Prefabs/AnimusForgeApiOnboardingPopup.xml"));
var mergeXml = XDocument.Load(Path.Combine(repo, "AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml"));
var mergeBindings = mergeXml.Descendants().Attributes().Select(a => a.Value).ToHashSet();
foreach (string binding in new[] { "@IsBool", "@IsNumeric", "@IsDropdown", "@IsHotkey", "@IsText", "@IsButton", "@BodyText", "@SearchText", "@ImageId" })
    Check(mergeBindings.Contains(binding), "merged XML retains " + binding);
Console.WriteLine("PASS mergedTerminalSettings pagination/search/groups/hotkey-cancel/save-entrypoints/api-wizard settingsWrite=NOT_RUN apiNetwork=NOT_RUN");
Call(vm, "ExecuteClose"); Check(closed == 1, "close callback exactly once per command"); Call(vm, "OnFinalize");

WeeklyReportOwnerReplay.Run(af);
WeeklyMaterialBatchPlannerReplay.Run(af);
WeeklyMaterialAggregationReplay.Run(af);
WeeklyActionMaterialCursorReplay.Run(af);
WeeklyPromptMaterialOwnerReplay.Run(af);
WeeklyMaterialStageCursorReplay.Run(af);
WeeklyMaterialPipelineParityReplay.Run(af);
WeeklyReportCommitQueueReplay.Run(af);
WeeklyReportBlockMaterialCursorReplay.Run(af);
WeeklyReportMaterialRevisionReplay.Run(af);

Type warType = af.GetType("AFWarStatsTerminal.Behaviors.AfWarStatsBehavior", true);
Type recordType = warType.GetNestedType("WarStatsRecord", BindingFlags.NonPublic);
object owner = Activator.CreateInstance(warType);
IDictionary active = (IDictionary)Get(owner, "_activeWars");
IList history = (IList)Get(owner, "_historicalWars");
object first = Activator.CreateInstance(recordType, true);
Set(first, "KillsA", 123); Set(first, "CasualtiesB", 45); Set(first, "WinsA", 3);
// Empty kingdom identifiers deliberately avoid any live Campaign lookup: this tests the real owner,
// not native event dispatch, map state, or game save serialization.
active.Add("|", first);
Call(owner, "EndActiveWar", "|");
Check(active.Count == 0 && history.Count == 1 && (int)Get(history[0], "KillsA") == 123, "peace owner archives and releases pair");
Call(owner, "EndActiveWar", "|"); Check(history.Count == 1, "duplicate peace/reconcile does not double archive");
object second = Activator.CreateInstance(recordType, true); Set(second, "KillsA", 7); active.Add("|", second);
Check((int)Get(first, "KillsA") == 123 && (int)Get(second, "CasualtiesB") == 0, "new pair record is independent");
Call(owner, "PrepareSaveData");
object loaded = Activator.CreateInstance(warType);
foreach (FieldInfo field in warType.GetFields(Members).Where(field => field.Name.StartsWith("_saved", StringComparison.Ordinal)))
    field.SetValue(loaded, field.GetValue(owner));
Set(loaded, "_dataVersion", 5); Call(loaded, "LoadSavedData");
Check(((IDictionary)Get(loaded, "_activeWars")).Count == 1 && ((IList)Get(loaded, "_historicalWars")).Count == 1, "active/history list roundtrip");
Check((int)Get(((IDictionary)Get(loaded, "_activeWars"))["|"], "KillsA") == 7, "new war survives owner load");
Check((int)Get(((IList)Get(loaded, "_historicalWars"))[0], "KillsA") == 123, "old war survives owner load");
Type ledgerType = warType.GetNestedType("WarStatsLedgerOwner", BindingFlags.NonPublic);
Check(ledgerType != null, "WarStats has a dedicated ledger owner");
object ledger = Get(owner, "_ledger");
Check(ledger != null && ReferenceEquals(Get(ledger, "ActiveWars"), active)
    && ReferenceEquals(Get(ledger, "HistoricalWars"), history), "host and terminal share the ledger-owned state");
Call(ledger, "ArchiveAndRemove", "|", first, 0);
Check(history.Count == 1 && active.Count == 1, "stale ended record cannot archive a reopened pair");
Set(first, "KillsA", 999);
Check((int)Get(history[0], "KillsA") == 123, "archived war is a snapshot, not the ended live record");
Type historyEntryType = warType.GetNestedType("HistoricalWarEntry", BindingFlags.Public);
object selectedHistory = Activator.CreateInstance(historyEntryType);
Set(selectedHistory, "PairKey", "|"); Set(selectedHistory, "StartDay", 0); Set(selectedHistory, "EndDay", 0);
Array selection = Array.CreateInstance(historyEntryType, 1); selection.SetValue(selectedHistory, 0);
Check((int)Call(owner, "DeleteHistoricalWars", selection) == 1 && history.Count == 0 && active.Count == 1,
    "terminal history deletion is identity-scoped and keeps reopened war");
Check((int)Call(owner, "DeleteHistoricalWars", selection) == 0, "duplicate terminal deletion is harmless");
Check(ledgerType.GetMethod("ApplyBattleStats", Members) != null, "ledger owns battle count decisions");
Call(ledger, "ApplyBattleStats", second, true, 5, 6, 8, 9, true, true);
Check((int)Get(second, "KillsA") == 12 && (int)Get(second, "CasualtiesA") == 6
    && (int)Get(second, "KillsB") == 8 && (int)Get(second, "CasualtiesB") == 9
    && (int)Get(second, "WinsA") == 1 && (int)Get(second, "LossesB") == 1,
    "direct-order battle counts and winner apply once");
Call(ledger, "ApplyBattleStats", second, false, -2, 3, 4, -5, true, false);
Check((int)Get(second, "KillsA") == 16 && (int)Get(second, "CasualtiesA") == 6
    && (int)Get(second, "KillsB") == 8 && (int)Get(second, "CasualtiesB") == 12
    && (int)Get(second, "WinsA") == 2 && (int)Get(second, "LossesB") == 2,
    "reverse-order battle counts clamp negatives and preserve winner polarity");
Check(ledgerType.GetMethod("UpsertHeroDeath", Members) != null
    && ledgerType.GetMethod("RecordRecentHeroBattle", Members) != null,
    "ledger owns hero death and recent battle decisions");
Call(ledger, "UpsertHeroDeath", second, "hero-a", "A", "Killer", 3, 10, "Battle", 0);
Call(ledger, "UpsertHeroDeath", second, "hero-a", "A renamed", null, 4, 11, "", 1);
IList deaths = (IList)Get(second, "HeroDeaths");
Check(deaths.Count == 1 && (string)Get(deaths[0], "HeroName") == "A renamed"
    && (string)Get(deaths[0], "KillerName") == "Killer"
    && (string)Get(deaths[0], "BattleName") == "Battle"
    && (int)Get(deaths[0], "Day") == 10 && (int)Get(deaths[0], "Side") == 1,
    "duplicate death updates details without losing first day or known killer/battle");
Check((bool)Call(ledger, "HasRecordedHeroDeath", "hero-a", "A renamed")
    && !(bool)Call(ledger, "HasRecordedHeroDeath", "hero-b", "B"),
    "death lookup distinguishes recorded and unknown hero");
Call(ledger, "RecordRecentHeroBattle", second, "hero-a", "kingdom-a", 12, 1);
Call(ledger, "RecordRecentHeroBattle", second, "hero-a", "kingdom-a", 13, 2);
IDictionary recent = (IDictionary)Get(second, "RecentHeroBattles");
Check(recent.Count == 1 && (int)Get(recent["hero-a"], "Day") == 13
    && (int)Get(recent["hero-a"], "Sequence") == 2, "recent battle replaces only same hero");
Call(ledger, "RecordRecentHeroBattle", second, "", "kingdom-a", 14, 3);
Check(recent.Count == 1, "missing hero identity cannot pollute recent battle state");
Call(ledger, "SetRecentBattleSequence", int.MaxValue - 1);
Check((int)Call(ledger, "NextBattleSequence") == int.MaxValue
    && (int)Call(ledger, "NextBattleSequence") == int.MaxValue, "recent battle sequence saturates without overflow");
Call(ledger, "SetRecentBattleSequence", -1);
Check((int)Get(ledger, "RecentBattleSequence") == 0, "loaded negative sequence is clamped");
Call(ledger, "ArchiveAndRemove", "|", second, 14);
IList archivedDeaths = (IList)Get(history[0], "HeroDeaths");
Check(active.Count == 0 && history.Count == 1 && archivedDeaths.Count == 1
    && !ReferenceEquals(archivedDeaths, deaths)
    && (bool)Call(ledger, "HasRecordedHeroDeath", "hero-a", "A renamed"),
    "ended war retains a cloned death history for later duplicate suppression");
Check(ledgerType.GetMethod("PrepareSaveData", Members) != null
    && ledgerType.GetMethod("RestoreSavedData", Members) != null,
    "ledger owns v5 save projection and restore decisions");
object malformed = Activator.CreateInstance(warType);
Set(malformed, "_dataVersion", 5);
Set(malformed, "_savedActiveKeysV2", new System.Collections.Generic.List<string> { "", "|" });
Set(malformed, "_savedActiveKillsAV2", new System.Collections.Generic.List<int> { 4, -7 });
Set(malformed, "_savedHistoryKeysV2", new System.Collections.Generic.List<string> { "", "|" });
Set(malformed, "_savedHistoryEndDayV2", new System.Collections.Generic.List<int> { 0, 5 });
Set(malformed, "_savedHistoryDurationV2", new System.Collections.Generic.List<int> { 0, 2 });
Call(malformed, "LoadSavedData");
IDictionary malformedActive = (IDictionary)Get(malformed, "_activeWars");
IList malformedHistory = (IList)Get(malformed, "_historicalWars");
Check(malformedActive.Count == 1 && (int)Get(malformedActive["|"], "KillsA") == 0
    && malformedHistory.Count == 1 && (int)Get(malformedHistory[0], "StartDay") == 3,
    "v5 restore skips blank keys, clamps bad counts, and recovers missing start day");
object legacy = Activator.CreateInstance(warType);
Set(legacy, "_dataVersion", 1);
Set(legacy, "_savedPairKeys", new System.Collections.Generic.List<string> { "|", "" });
Set(legacy, "_savedCasualtiesA", new System.Collections.Generic.List<int> { 8, 20 });
Set(legacy, "_savedCasualtiesB", new System.Collections.Generic.List<int> { 5, 30 });
Call(legacy, "LoadSavedData");
IDictionary legacyRecords = (IDictionary)Get(legacy, "_legacyRecords");
Check(legacyRecords.Count == 1 && (int)Get(legacyRecords["|"], "InflictedByA") == 8
    && (int)Get(legacyRecords["|"], "InflictedByB") == 5
    && (bool)Get(legacy, "_legacyMigrationPending"), "v1 restore queues only complete legacy pair rows");
object legacyLedger = Get(legacy, "_ledger");
object migratedCurrent = Activator.CreateInstance(recordType, true);
Set(migratedCurrent, "CasualtiesA", 5);
Call(legacyLedger, "ApplyMigratedLegacy", "|", migratedCurrent, true);
Check(((IDictionary)Get(legacy, "_activeWars")).Count == 1
    && ((IList)Get(legacy, "_historicalWars")).Count == 0,
    "legacy current pair migrates only into active ledger");
object migratedEnded = Activator.CreateInstance(recordType, true);
Set(migratedEnded, "CasualtiesB", 8);
Call(legacyLedger, "ApplyMigratedLegacy", "ended|pair", migratedEnded, false);
Check(((IList)Get(legacy, "_historicalWars")).Count == 1
    && (int)Get(((IList)Get(legacy, "_historicalWars"))[0], "CasualtiesB") == 8,
    "legacy ended pair migrates only into history");
Call(legacyLedger, "ClearLegacyRecords");
Check(legacyRecords.Count == 0, "legacy migration consumes queued rows");
Check(ledgerType.GetMethod("StartWar", Members) != null
    && ledgerType.GetMethod("ResetActiveCountsAndIncidents", Members) != null,
    "ledger owns declaration and terminal-clear state transitions");
object lifecycle = Activator.CreateInstance(warType);
object lifecycleLedger = Get(lifecycle, "_ledger");
object currentWar = Activator.CreateInstance(recordType, true);
Set(currentWar, "KillsA", 21); Set(currentWar, "CasualtiesB", 34);
((IDictionary)Get(lifecycle, "_activeWars")).Add("|", currentWar);
((IList)Get(lifecycle, "_historicalWars")).Add(history[0]);
Call(lifecycleLedger, "RecordRecentHeroBattle", currentWar, "hero-a", "kingdom-a", 20, 1);
Call(lifecycleLedger, "UpsertHeroDeath", currentWar, "hero-a", "A", null, 3, 20, "Battle", 0);
Call(lifecycleLedger, "StartWar", currentWar, 1, 21);
Check((int)Get(currentWar, "AttackerSide") == 1 && (int)Get(currentWar, "StartDay") == 21
    && ((IDictionary)Get(currentWar, "RecentHeroBattles")).Count == 0,
    "declaration resets only recent participation and records oriented start");
Call(lifecycle, "ClearAllRecords");
Check(((IDictionary)Get(lifecycle, "_activeWars")).Count == 1
    && ((IList)Get(lifecycle, "_historicalWars")).Count == 0
    && (int)Get(currentWar, "KillsA") == 0 && (int)Get(currentWar, "CasualtiesB") == 0
    && ((IList)Get(currentWar, "HeroDeaths")).Count == 0,
    "terminal clear preserves live pair while resetting counts, deaths and history");
Console.WriteLine("PASS PhaseEightParityReplay terminal=paging/search/identity/details/back/empty/close tags=full-search/details/snapshot-export/refresh-back/empty weekly=country/date/full-body/tags/empty/back/completion-lifecycle/xml war=ledger/archive/stale/reverse-count/death/recent/v1-v5/terminal live=NOT_RUN");
Console.WriteLine("implementationSha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))));
