using AnimusForge;
using System.Text.Json;

int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception("FAIL: " + name); }
Check(ExpeditionParadeRules.InWindow(100, 100), "declaration/peace day");
Check(ExpeditionParadeRules.InWindow(114, 100), "inclusive day 14");
Check(!ExpeditionParadeRules.InWindow(114.00001, 100), "expired immediately after day 14");
Check(!ExpeditionParadeRules.InWindow(99.99, 100), "future start rejected");
foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
    Check(!ExpeditionParadeRules.InWindow(invalid, 100), "invalid now");
    Check(!ExpeditionParadeRules.InWindow(100, invalid), "invalid start");
}
Check(ExpeditionParadeRules.Winner(100, true, 5, -5) == -1, "paying tribute loses despite fiefs");
Check(ExpeditionParadeRules.Winner(-100, false, 0, 0) == 1, "receiving tribute wins without snapshot");
Check(ExpeditionParadeRules.Winner(0, false, 5, -5) == 0, "unknown opening snapshot cannot invent victory");
Check(ExpeditionParadeRules.Winner(0, true, 2, -2) == 1, "territorial advantage first");
Check(ExpeditionParadeRules.Winner(0, true, -2, 2) == -1, "territorial advantage second");
Check(ExpeditionParadeRules.Winner(0, true, 1, 1) == 0, "draw is not victory");
Check(ExpeditionParadeRules.Winner(0, true, int.MaxValue, int.MinValue) == 1, "no subtraction overflow");
var rng = new Random(20261014);
for (int trial = 0; trial < 1000; trial++) {
    int[] inventory = Enumerable.Range(0, rng.Next(0, 24)).Select(_ => rng.Next(0, 180)).ToArray();
    int cap = rng.Next(-5, 150);
    int[] allocated = ExpeditionParadeRules.Allocate(inventory, cap);
    Check(allocated.Length == inventory.Length, "source count");
    Check(allocated.Sum() == Math.Min(Math.Max(0, cap), inventory.Sum()), "budget conservation");
    Check(allocated.Select((n, i) => n >= 0 && n <= inventory[i]).All(x => x), "never invent troops");
    Check(allocated.SequenceEqual(ExpeditionParadeRules.Allocate(inventory, cap)), "stable allocation");
    if (cap >= inventory.Count(n => n > 0))
        Check(allocated.Select((n, i) => inventory[i] == 0 || n > 0).All(x => x), "all healthy sources represented");
}
const string expedition = "[ACTION:PARADE:EXPEDITION]", victory = "[ACTION:PARADE:VICTORY]";
Check(ExpeditionParadeRules.NormalizeTags(expedition.ToLowerInvariant(), new[] { expedition }) == expedition, "case normalized");
Check(ExpeditionParadeRules.NormalizeTags(expedition + expedition, new[] { expedition }) == expedition, "duplicate deduplicated");
Check(ExpeditionParadeRules.NormalizeTags(expedition + victory, new[] { expedition }) == "", "conflict rejected BEFORE eligibility filtering");
Check(ExpeditionParadeRules.NormalizeTags(victory, new[] { expedition }) == "", "unavailable rejected");
Check(ExpeditionParadeRules.NormalizeTags("[ACTION:PARADE:UNKNOWN]", new[] { "[ACTION:PARADE:UNKNOWN]" }) == "", "unknown type rejected");
Check(ExpeditionParadeRules.NormalizeTags(expedition, null) == "", "missing allowlist rejected");
Check(ExpeditionParadeRules.NormalizeTags(null, new[] { expedition }) == "", "empty content");
string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
Check(ExpeditionParadeMenuPolicy.InsertionIndex(new[] { "town_center", "town_leave", "rex_stage_public_execution", "rex_stage_public_execution_custom" }, 1) == 2, "entry immediately before execution, not before Leave");
Check(ExpeditionParadeMenuPolicy.InsertionIndex(new[] { "castle_center", "castle_leave" }, 1) == 1, "castle without execution falls back before Leave");
Check(ExpeditionParadeMenuPolicy.InsertionIndex(new[] { "rex_stage_public_execution_custom", "town_leave" }, 1) == 0, "custom execution anchor");
Check(ExpeditionParadeMenuPolicy.InsertionIndex(Array.Empty<string>(), -1) == 0, "empty menu");
Check(ExpeditionParadeMenuPolicy.InsertionIndex(new[] { "town_center", "mod_option" }, -1) == 2, "no anchor appends without displacing existing options");
foreach (string resource in new[] { "af_ui_strings.xml", "CNs/af_ui_strings-zh-CN.xml" }) {
    var ui = System.Xml.Linq.XDocument.Load(Path.Combine(root, "content/modules/AF.Module.UI/ModuleData/Languages", resource));
    foreach (string suffix in new[] { "MenuEntry", "MenuDescription", "MenuExpedition", "MenuVictory", "MenuBack" })
        Check(ui.Descendants("string").Count(x => (string)x.Attribute("id") == "AF_UI_Parade_" + suffix && !string.IsNullOrWhiteSpace((string)x.Attribute("text"))) == 1, "unique nonempty bilingual menu key " + suffix);
}
foreach (string lang in new[] { "", ".en" }) {
    string path = Path.Combine(root, "content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts" + lang + ".json");
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var rules = doc.RootElement.GetProperty("RulePrompts").EnumerateArray().ToArray();
    var parade = rules.Single(r => r.GetProperty("Id").GetString() == "expedition_parade");
    Check(parade.GetProperty("IsEnabled").GetBoolean(), "localized rule enabled");
    Check(parade.GetProperty("PostprocessRules").EnumerateArray().Select(r => r.GetProperty("Tag").GetString()).Order().SequenceEqual(new[] { expedition, victory }.Order()), "only two action tags");
    Check(rules.Count(r => r.TryGetProperty("TopicNumber", out var t) && t.GetInt32() == parade.GetProperty("TopicNumber").GetInt32()) == 1, "unique topic number");
}
Console.WriteLine($"PASS: {checks} parade checks, including 1000 randomized roster allocations; linked production rules and both prompt resources.");
