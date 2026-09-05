using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
string dll = Path.Combine(repo, "bin/Debug/single_module_stage/AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll");
AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
{
    string candidate = Path.Combine(AppContext.BaseDirectory, new AssemblyName(args.Name).Name + ".dll");
    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
};
Assembly af = Assembly.LoadFrom(dll);
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
Call(vm, "ShowTagCatalog", snapshot);
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
Check(((IList)Get(vm, "Items")).Count == 0 && (bool)Get(vm, "IsTagCatalogBrowser"), "refresh to empty index remains navigable");
Call(vm, "ExecuteExportTagCatalog"); // Empty snapshot fails before module-root resolution or any write.
Check((bool)Get(vm, "IsDetailsVisible") && ((string)Get(vm, "DetailText")).Contains("没有可导出"), "export failure is visible and actionable");
Call(vm, "ExecuteBack"); Check((bool)Get(vm, "IsTagCatalogBrowser"), "export failure returns to index");
Call(vm, "ExecuteBack"); Check(!(bool)Get(vm, "IsTagCatalogBrowser") && (float)Get(vm, "MenuContentTop") == 50f, "refresh does not stack duplicate browsers");
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
foreach (string binding in new[] { "@BodyText", "@BodyFontSize", "@WeekText", "@DateText", "@TagText", "@ShowViewFullReport", "ExecuteViewFullReport", "@WeeklyReportVm.EmptyStateText", "@WeeklyReportVm.SelectedCountryMetaText" })
    Check(weeklyBindings.Contains(binding), "weekly XML binds " + binding);
Check(!weeklyBindings.Contains("@SummaryText"), "weekly XML does not bind nonexistent summary field");
Check(af.GetType("AnimusForge.TerminalWeeklyReportBrowserPopup", false) == null && af.GetType("AFWarStatsTerminal.UI.AfWarStatsPopup", false) == null, "replaced independent popup implementations removed");
Call(vm, "ExecuteBack"); Call(vm, "ShowDiagnostics", "status", "detail", false);
Call(vm, "ExecuteRequestAiAnalysis"); Check(closed == 0, "no-error analysis action is inert");
Call(vm, "ExecuteBack"); Check((string)Get(vm, "SearchText") == "NPC-090", "diagnostics back preserves menu state");
Call(vm, "ExecuteClose"); Check(closed == 1, "close callback exactly once per command"); Call(vm, "OnFinalize");

WeeklyReportOwnerReplay.Run(af);

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
Console.WriteLine("PASS PhaseEightParityReplay terminal=paging/search/identity/details/back/empty/close tags=full-search/details/snapshot-export/refresh-back/empty weekly=country/date/full-body/tags/empty/back/completion-lifecycle/xml war=archive/idempotence/list-roundtrip live=NOT_RUN");
Console.WriteLine("implementationSha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))));
