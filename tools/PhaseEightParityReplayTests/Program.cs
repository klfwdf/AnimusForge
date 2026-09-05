using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

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
Call(vm, "ExecuteClose"); Check(closed == 1, "close callback exactly once per command"); Call(vm, "OnFinalize");

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
Console.WriteLine("PASS PhaseEightParityReplay terminal=paging/search/identity/details/back/empty/close war=archive/idempotence/list-roundtrip live=NOT_RUN");
Console.WriteLine("implementationSha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))));
