using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using AnimusForge;

int checks = 0;
void Check(bool value, string message)
{
    checks++;
    if (!value) throw new InvalidOperationException(message);
}

string Policy(int days, int duration = 100, string effect = "+2", string status = "active", string body = "正文") =>
    "政策《示例》；发布国=甲国；范围=kingdom\n政策状态=" + status + "\n" + body
    + "\n影响：目标王国：甲国｜粮食：每日 " + effect + "｜期限：持续 " + duration
    + " 天｜状态：剩余 " + days + "/" + duration + " 天。\n机械效果：粮食 " + effect;

var clocks = Enumerable.Range(0, 60).Select(day => new Entry(day + 1, "policy_published", "p", "date", Policy(100 - day))).ToList();
var clean = WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(clocks, e => e.Source, e => e.Text, out int removed);
Check(removed == 59 && clean.Count == 1 && clean[0].Sequence == 1, "60 daily observations must retain one original event");
Check(!WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90), Policy(95)), "renewed remaining days must survive");
Check(!WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90), Policy(89, 200)), "duration changes must survive");
Check(!WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90), Policy(89, effect: "+3")), "effect changes must survive");
Check(!WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90), Policy(0, status: "abolished")), "abolition must survive");
Check(!WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90), Policy(89).Replace("剩余 89/100 天", "已结束")), "expiry must survive");
Check(!WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90, body: "期限：剩余 90/100 天"), Policy(89, body: "期限：剩余 89/100 天")), "player prose must never be normalized");
Check(WorldDiplomacyPolicyHistoryRules.IsCountdownOnlyAdvance(Policy(90) + "\r\n", Policy(89) + "\n\n"), "trailing formatting is immaterial");
var renewal = new[] { 100, 90, 95, 94 }.Select((d, i) => new Entry(i, "policy_published", "p", "date", Policy(d)));
Check(WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(renewal, e => e.Source, e => e.Text, out _).Count == 2,
    "100 -> 90 -> 95 must preserve renewal rather than comparing only to the first retained 100");
var changes = new[] { Policy(100), Policy(99, effect: "+3"), Policy(98), Policy(97) }
    .Select((text, i) => new Entry(i, "policy_published", "p", "date", text));
Check(WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(changes, e => e.Source, e => e.Text, out _).Count == 3,
    "A -> B -> A must preserve the reverted policy change");
var mixed = clocks.Take(2).Concat(new[] { new Entry(3, "declaration", "p", "date", Policy(98)), new Entry(4, "policy_published", "other", "date", Policy(98)) });
Check(WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(mixed, e => e.Kind.StartsWith("policy_") ? e.Source : null, e => e.Text, out _).Count == 3,
    "other policies and declarations must remain isolated");
Check(WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(clean, e => e.Source, e => e.Text, out _).Count == 1, "migration must be idempotent");
var kindChanges = new[] { new Entry(1, "policy_published", "p", "date", Policy(100)),
    new Entry(2, "policy_snapshot", "p", "date", Policy(99)), new Entry(3, "policy_published", "p", "date", Policy(98)) };
Check(WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(kindChanges, e => e.Source, e => e.Text, out _,
    (a, b) => a.Kind == b.Kind && a.Date == b.Date).Count == 3, "event-kind transitions must form a barrier even when content returns to its old value");

var fingerprints = new Dictionary<string, string>();
var revisions = new Dictionary<string, long>();
long Observe(string id, string fingerprint)
{
    long revision = WorldDiplomacyPolicyHistoryRules.NextEventRevision(fingerprints, revisions, id, fingerprint);
    if (revision > 0) { fingerprints[id] = fingerprint; revisions[id] = revision; }
    return revision;
}
Check(Observe("p", "published:A") == 1, "first publication must receive event 1");
for (int day = 0; day < 60; day++) Check(Observe("p", "published:A") == 0, "unchanged policy must not append every sync");
Check(Observe("p", "modified:B") == 2 && Observe("p", "published:A") == 3, "reverting content must create a new event identity");
fingerprints = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(fingerprints));
revisions = JsonSerializer.Deserialize<Dictionary<string, long>>(JsonSerializer.Serialize(revisions));
Check(Observe("p", "published:A") == 0, "save/load must preserve the observation cursor");
Check(Observe("p", "renewal:1") == 4 && Observe("p", "expired") == 5 && Observe("p", "abolished") == 6,
    "renewal, expiry and abolition must be distinct revisions");
Check(Observe("other", "published:A") == 1, "another policy must have its own revision sequence");

var prefix = new[] { (seq: 11L, tokens: 200L), (seq: 12L, tokens: 250L), (seq: 13L, tokens: 1L) };
Check(WorldDiplomacyPolicyHistoryRules.SelectCompressionPrefix(10, 100, prefix, 400, e => e.seq, e => e.tokens) == 11,
    "batch compression must freeze a contiguous prefix, never skip an oversized middle entry");
Check(WorldDiplomacyPolicyHistoryRules.SelectCompressionPrefix(10, 100, prefix, 551, e => e.seq, e => e.tokens) == 13,
    "all entries may fit at the exact budget boundary");
Check(WorldDiplomacyPolicyHistoryRules.SelectCompressionPrefix(10, 600, prefix, 400, e => e.seq, e => e.tokens) == 10,
    "oversized snapshots must not pull more entries into the request");
Check(!WorldDiplomacyPolicyHistoryRules.CanAdvanceCompression(10, 10, 100, 200), "an oversized single entry must not cause repeated paid compaction of an already-small snapshot");
Check(WorldDiplomacyPolicyHistoryRules.CanAdvanceCompression(10, 10, 300, 200), "a reducible snapshot can make progress even without a new delta");
Check(WorldDiplomacyPolicyHistoryRules.CanAdvanceCompression(11, 10, 100, 200), "consuming the next delta makes progress");

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
string behavior = File.ReadAllText(Path.Combine(root, "WorldDiplomacyBehavior.cs"));
string context = File.ReadAllText(Path.Combine(root, "PolicySystem/Context/WorldDiplomacyPolicyContext.cs"));
string export = File.ReadAllText(Path.Combine(root, "PolicySystem/Core/CustomPolicyBehavior.Generation.cs"));
Check(export.Contains("BuildPolicyRecordEffectSummary(history, includeRemainingDays: false)"), "export must build stable impact BEFORE display truncation");
Check(context.Contains("entry?.DiplomacyImpactSummary ?? entry?.ImpactSummary"), "archive must consume stable impact");
Check(context.Contains("_publishedHistoryLedgerId = UnifiedPolicyHistoryLedgerId;"), "ledger identity must be independent of content revision");
Check(context.Contains("AppendHash(ref contentSignature, entry.DiplomacyRevisionKey)"), "renewal/application identity must participate in semantic revision");
Check(behavior.Contains("fingerprint = policy.ContentHash;"), "consumer must honor the complete semantic revision");
Check(behavior.Contains("WorldDiplomacyPolicyHistoryRules.NextEventRevision("), "consumer must use tested durable revision logic");
int budgetGuard = behavior.IndexOf("if (!EnsureRequestFitsInputBudget(job, requestMessages)) return;", StringComparison.Ordinal);
Check(budgetGuard >= 0 && budgetGuard < behavior.IndexOf("job.IsRunning = true;", budgetGuard, StringComparison.Ordinal), "input budget must be checked before network dispatch");
Check(behavior.Contains("CaptureCanonicalHistoryForJob(job, syncSources: false, throughSequence: throughSequence)"), "compression request must match its frozen commit cutoff");
Check(behavior.Contains("pending.AwaitingHistoryCompression = false;"), "successful compression must release waiting generation instead of scheduling an endless compaction loop");
Check(behavior.Contains("TryConsumeDiplomacyLlmRequestBudget(consume: false)"), "exhausted daily budgets must exit before assembling large prompts");

if (args.Length > 0)
{
    using var request = JsonDocument.Parse(File.ReadAllText(args[0]));
    string history = request.RootElement.GetProperty("messages")[1].GetProperty("content").GetString();
    var matches = Regex.Matches(history, @"(?ms)^\[seq=(?<seq>\d+)\|kind=(?<kind>[^|]+)\|date=(?<date>[^|]+)\|source=(?<source>[^|]+)[^\r\n]*\r?\n(?<body>.*?)(?=^\[seq=|\z)");
    var entries = matches.Select(m => new Entry(long.Parse(m.Groups["seq"].Value), m.Groups["kind"].Value,
        m.Groups["source"].Value, m.Groups["date"].Value, m.Groups["body"].Value)).ToList();
    var migrated = WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(entries,
        e => e.Kind.StartsWith("policy_") ? e.Source : null, e => e.Text, out int sampleRemoved,
        (a, b) => a.Kind == b.Kind && a.Date == b.Date);
    Check(sampleRemoved > 400, "reported request must lose its daily policy copies");
    Check(entries.Count(e => !e.Kind.StartsWith("policy_")) == migrated.Count(e => !e.Kind.StartsWith("policy_")), "sample's actual diplomacy facts must remain intact");
    Console.WriteLine($"Capture replay: entries {entries.Count} -> {migrated.Count}; countdown copies removed {sampleRemoved}; entry text chars {entries.Sum(e => e.Text.Length)} -> {migrated.Sum(e => e.Text.Length)}. Existing compressed summary retained.");
}
if (args.Length > 1)
{
    // Exercise the actual compiled renderers without starting a campaign or invoking an API.
    string implementation = Path.GetFullPath(args[1]);
    string[] dependencyRoots = { Path.GetDirectoryName(implementation), Path.Combine(root, ".tmp/build_check/1.4"), Path.Combine(root, ".tmp/phase7-build/1.4") };
    AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
    {
        string name = new AssemblyName(request.Name).Name + ".dll";
        foreach (string directory in dependencyRoots)
        {
            string candidate = Path.Combine(directory, name);
            if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
        }
        return null;
    };
    var assembly = Assembly.LoadFrom(implementation);
    var entryType = assembly.GetType("AnimusForge.NpcPolicyHistoryEntry", true);
    var adapter = assembly.GetType("AnimusForge.WorldDiplomacyPolicyContext", true)
        .GetMethod("BuildPublishedPolicyArtifactText", BindingFlags.Static | BindingFlags.NonPublic);
    object entry = Activator.CreateInstance(entryType, true);
    void Set(string property, object value) => entryType.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(entry, value);
    Set("PolicyContent", "国王发布政策");
    Set("RawPolicyStatus", "active");
    Set("EffectStatus", "active");
    Set("DiplomacyImpactSummary", "目标王国｜粮食：每日 +2｜期限：持续 100 天｜状态：生效中。");
    string first = null;
    for (int day = 0; day < 60; day++)
    {
        Set("ImpactSummary", "目标王国｜粮食：每日 +2｜期限：持续 100 天｜状态：剩余 " + (100 - day) + "/100 天。");
        string rendered = (string)adapter.Invoke(null, new[] { entry });
        first ??= rendered;
        Check(first == rendered && !rendered.Contains("剩余"), "compiled adapter must ignore all 60 countdown-only updates");
    }
    Set("EffectStatus", "expired");
    Check(first != (string)adapter.Invoke(null, new[] { entry }), "compiled adapter must preserve effect expiry");
    Set("RawPolicyStatus", "abolished");
    Check(((string)adapter.Invoke(null, new[] { entry })).Contains("政策状态=abolished"), "compiled adapter must preserve abolition");
    Console.WriteLine("Compiled adapter replay passed: 60 daily changes, expiry and abolition.");
}
Console.WriteLine($"Policy history regression checks passed: {checks}");

record Entry(long Sequence, string Kind, string Source, string Date, string Text);
