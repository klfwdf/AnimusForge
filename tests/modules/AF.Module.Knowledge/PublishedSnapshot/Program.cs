using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

public partial class KnowledgeLibraryBehavior
{
    private bool _knowledgeLoadFailed;
    public class KnowledgeFile { public string ContentRevision = ""; public List<LoreRule> Rules = new List<LoreRule>(); }
    public class LoreRule
    {
        public string Id;
        public List<string> Keywords, RagShortTexts, SemanticPrototypes;
        public List<LoreVariant> Variants;
        public List<LoreTextMapping> TextMappings;
    }
    public class LoreVariant { public int Priority; public LoreWhen When; public string Content; }
    public class LoreWhen
    {
        public List<string> HeroIds, Cultures, KingdomIds, SettlementIds, Roles, IdentityIds;
        public bool? IsFemale, IsClanLeader;
        public Dictionary<string, int> SkillMin;
    }
    public class LoreTextMapping
    {
        public string SourceText, Kind, TargetId, EmptyValueText, TrueText, FalseText;
        public int? AgeMin, AgeMax;
    }
    private sealed class FakeIndex
    {
        internal long Version = 1;
        internal void Touch() { Version++; }
        internal void EnsureVectorIndex(IReadOnlyList<LoreRule> rules, long version) { }
        internal void EnsureOnnxIndex(IReadOnlyList<LoreRule> rules, long version)
        { WorkerEntered.Set(); if (!WorkerRelease.Wait(5000)) throw new Exception("worker test timeout"); }
    }
    internal static readonly System.Threading.ManualResetEventSlim WorkerEntered = new(false), WorkerRelease = new(false);
    internal sealed class LoreCandidateRules { internal string Term; }
    internal sealed class MentionedWorldEntities
    {
        internal string Term;
        public List<string> Entities => new() { Term };
        internal MentionedWorldEntities Clone() => new() { Term = Term };
    }
    internal static int Setting = 1;
    private static object CapturePromptLoreSettings() => new { Enabled=true, SemanticTopK=Setting };
    private static LoreCandidateRules CollectPromptLoreCandidates(MentionedWorldEntities mentions, long version, object settings)
        => version == Index.Version ? new() { Term = mentions.Term } : null;
    private static readonly FakeIndex Index = new FakeIndex();
    private static IReadOnlyList<LoreRule> _publishedRules;
    private static long _publishedRulesVersion = -1;
    private static readonly object _loreContextCacheLock = new object();
    private static readonly Dictionary<string, string> _loreContextCache = new Dictionary<string, string>();
    private KnowledgeFile _file;
    public static KnowledgeLibraryBehavior Instance { get; private set; }
    internal static void Load(KnowledgeFile file) { Instance = new KnowledgeLibraryBehavior { _file = file }; TouchRuleData(); }
    internal static IReadOnlyList<LoreRule> Publish() { PublishPromptRules(); return GetPublishedRulesForIndex(); }
    internal static void Change() => TouchRuleData();
    internal static void Reload(KnowledgeFile file) { Instance = new KnowledgeLibraryBehavior { _file = file }; TouchRuleData(false); }
    internal static void FailLoad() { Instance._knowledgeLoadFailed = true; }
    internal static bool LoadFailed => Instance._knowledgeLoadFailed;
    internal static long Version => Index.Version;
}

internal static class Program
{
    private static int _checks;
    private static void Check(bool ok, string reason) { _checks++; if (!ok) throw new Exception("FAIL " + reason); }
    private static int Main()
    {
        try { Run(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
    private static void Run()
    {
        KnowledgeLibraryBehavior.Load(new KnowledgeLibraryBehavior.KnowledgeFile());
        var mentions = new KnowledgeLibraryBehavior.MentionedWorldEntities { Term = "original" };
        var worker = KnowledgeLibraryBehavior.Instance.StartExternalLoreSelection(mentions, System.Threading.CancellationToken.None, out long captured);
        Check(KnowledgeLibraryBehavior.WorkerEntered.Wait(5000) && !worker.IsCompleted, "actual host method returns while cold retrieval is still blocked on worker");
        mentions.Term = "changed on main thread";
        KnowledgeLibraryBehavior.WorkerRelease.Set();
        Check(worker.GetAwaiter().GetResult().Term == "original" && captured == KnowledgeLibraryBehavior.Version, "worker receives cloned mention strings and captured rule generation");
        var originalMentions=new KnowledgeLibraryBehavior.MentionedWorldEntities{Term="original"};
        Check(ReferenceEquals(worker,KnowledgeLibraryBehavior.Instance.StartExternalLoreSelection(originalMentions,System.Threading.CancellationToken.None,out _)),
            "completed same-input retrieval is reused across material batches");
        var other=KnowledgeLibraryBehavior.Instance.StartExternalLoreSelection(mentions,System.Threading.CancellationToken.None,out _);
        Check(!ReferenceEquals(worker,other)&&other.GetAwaiter().GetResult().Term==mentions.Term,"different mentions have independent candidates");
        KnowledgeLibraryBehavior.Setting++;
        var setting=KnowledgeLibraryBehavior.Instance.StartExternalLoreSelection(originalMentions,System.Threading.CancellationToken.None,out _);
        Check(!ReferenceEquals(worker,setting)&&setting.GetAwaiter().GetResult()!=null,"changed settings invalidate selection");
        var cancel=new System.Threading.CancellationTokenSource();cancel.Cancel();bool cancelled=false;
        try{KnowledgeLibraryBehavior.Instance.StartExternalLoreSelection(originalMentions,cancel.Token,out _);}
        catch(OperationCanceledException){cancelled=true;}
        Check(cancelled,"completed cache cannot bypass cancellation");cancel.Dispose();
        KnowledgeLibraryBehavior.Change();
        var refreshed=KnowledgeLibraryBehavior.Instance.StartExternalLoreSelection(originalMentions,System.Threading.CancellationToken.None,out _);
        Check(!ReferenceEquals(setting,refreshed)&&refreshed.GetAwaiter().GetResult()!=null,"rule version invalidates completed retrieval");
        var rule = new KnowledgeLibraryBehavior.LoreRule
        {
            Id = "r", Keywords = new List<string> { "first" }, RagShortTexts = new List<string> { "short" },
            SemanticPrototypes = new List<string> { "prototype" },
            Variants = new List<KnowledgeLibraryBehavior.LoreVariant> { new KnowledgeLibraryBehavior.LoreVariant
            { Content = "old", When = new KnowledgeLibraryBehavior.LoreWhen
                { HeroIds = new List<string> { "hero" }, SkillMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Riding"] = 3 } } } },
            TextMappings = new List<KnowledgeLibraryBehavior.LoreTextMapping> { new KnowledgeLibraryBehavior.LoreTextMapping { SourceText = "a", TrueText = "true" } }
        };
        var file = new KnowledgeLibraryBehavior.KnowledgeFile { Rules = new List<KnowledgeLibraryBehavior.LoreRule> { rule } };
        KnowledgeLibraryBehavior.Load(file);
        string revision = file.ContentRevision;
        Check(!string.IsNullOrEmpty(revision), "successful import assigns a persisted content revision");
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(Newtonsoft.Json.JsonConvert.SerializeObject(file));
        KnowledgeLibraryBehavior.Reload(restored);
        Check(restored.ContentRevision == revision, "save/load invalidates runtime indexes without changing persisted content identity");
        KnowledgeLibraryBehavior.Reload(file);
        KnowledgeLibraryBehavior.FailLoad();
        KnowledgeLibraryBehavior.Change();
        Check(file.ContentRevision != revision && file.Rules.Count == 1 && !KnowledgeLibraryBehavior.LoadFailed,
            "same-count edits/imports change identity and clear prior load failure");
        long version = KnowledgeLibraryBehavior.Version;
        var first = KnowledgeLibraryBehavior.Publish();
        Check(first.Count == 1 && !ReferenceEquals(first[0], rule), "rules detached once");
        Check(ReferenceEquals(first, KnowledgeLibraryBehavior.Publish()) && KnowledgeLibraryBehavior.Version == version, "same version reuses snapshot");
        rule.Keywords[0] = "changed"; rule.RagShortTexts[0] = "changed"; rule.SemanticPrototypes[0] = "changed";
        rule.Variants[0].Content = "changed"; rule.Variants[0].When.HeroIds[0] = "changed";
        rule.Variants[0].When.SkillMin["Riding"] = 9; rule.TextMappings[0].TrueText = "changed";
        Check(first[0].Keywords[0] == "first" && first[0].RagShortTexts[0] == "short" && first[0].SemanticPrototypes[0] == "prototype", "topic/evidence lists detached");
        Check(first[0].Variants[0].Content == "old" && first[0].Variants[0].When.HeroIds[0] == "hero" && first[0].Variants[0].When.SkillMin["riding"] == 3, "variant and dictionary detached with comparer");
        Check(first[0].TextMappings[0].TrueText == "true", "text mapping detached");
        KnowledgeLibraryBehavior.Change();
        Check(KnowledgeLibraryBehavior.Version > version, "edit bumps data version");
        var second = KnowledgeLibraryBehavior.Publish();
        Check(!ReferenceEquals(first, second) && second[0].Keywords[0] == "changed" && first[0].Keywords[0] == "first", "new version published; old snapshot stable");
        Console.WriteLine("PASS knowledge-published-snapshot checks=" + _checks + " source=production-methods");
    }
}
