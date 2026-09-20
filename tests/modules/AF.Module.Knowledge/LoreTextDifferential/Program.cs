using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using LoreRule = AnimusForge.KnowledgeLibraryBehavior.LoreRule;

namespace AnimusForge
{
    public sealed class FakeName { public string Value; public override string ToString() => Value; }
    public sealed class FakeCulture { public string StringId; }
    public sealed class FakeKingdom { public string StringId; }
    public sealed class FakeClan { public FakeKingdom Kingdom; public Hero Leader; public int Tier; }
    public sealed class Hero
    {
        public static Hero MainHero;
        public string StringId;
        public FakeName Name;
        public FakeCulture Culture;
        public FakeClan Clan;
        public FakeKingdom MapFaction;
        public Settlement CurrentSettlement;
        public bool IsLord, IsNotable, IsFemale;
        public Occupation Occupation;
        public CharacterObject CharacterObject;
    }
    public sealed class CharacterObject { public Hero HeroObject; public string StringId; }
    public sealed class Settlement { public static Settlement CurrentSettlement; public string StringId; }
    public enum Occupation { Soldier, Villager, Townsfolk, Wanderer, Lord }
    public sealed class MentionedWorldEntities
    {
        public List<string> Entities = new List<string>();
        public bool IsEmpty => Entities.Count == 0;
        public MentionedWorldEntities Clone() => new MentionedWorldEntities { Entities = new List<string>(Entities) };
    }
    public static class Logger
    {
        public static void Log(string category, string message) { }
        public static void RecordHitRate(string category, string key, bool hit, string detail, string input) { }
    }
    public partial class KnowledgeLibraryBehavior
    {
        public sealed class LoreRule
        {
            public string Id;
            public List<string> Keywords = new List<string>();
            public List<string> RagShortTexts = new List<string>();
            public List<LoreVariant> Variants = new List<LoreVariant>();
            public List<LoreTextMapping> TextMappings = new List<LoreTextMapping>();
        }
        public sealed class LoreVariant { public LoreWhen When; public string Content; }
        public sealed class LoreWhen
        {
            public List<string> HeroIds, Cultures, KingdomIds, SettlementIds, Roles, IdentityIds;
            public bool? IsFemale, IsClanLeader;
            public Dictionary<string, int> SkillMin;
        }
        public sealed class LoreTextMapping { public string SourceText; }
        public sealed class KnowledgeFile { public List<LoreRule> Rules = new List<LoreRule>(); }
        private const string PlayerPersonaRuleId = "player_persona";
        private KnowledgeFile _file;
        private static KnowledgeRuleIndex Index;
        private static LoreCandidateRetriever Retriever;
        private static int SelectionCalls;
        public static KnowledgeLibraryBehavior Instance { get; private set; }
        public KnowledgeLibraryBehavior(List<LoreRule> rules)
        {
            _file = new KnowledgeFile { Rules = rules };
            Index = new KnowledgeRuleIndex(new FakePorts(), () => _file.Rules);
            Index.EnsureVectorIndex();
            SelectionCalls = 0;
            Retriever = new LoreCandidateRetriever(Index, (c, m) => { if (m.StartsWith("candidate_pool")) SelectionCalls++; });
            Instance = this;
        }
        private static bool HasAnyTextMappings() => false;
        private static bool KnowledgeRetrievalEnabledSafe() => true;
        private static bool TryGetLoreContextCache(string key, long version, out string value) { value = null; return false; }
        private static void PutLoreContextCache(string key, long version, string value) { }
        private static string BuildExactKeywordSlotCacheSignature() => "player_slot=off";
        private static string GetPlayerKeywordSlotKeyword() => "";
        private static bool CanObserverReceivePlayerPersona(Hero hero, CharacterObject character) => false;
        private static string ProtectPlayerPersonaRawNameReferences(string text) => text;
        private static string BuildKnowledgeHitRateDetail(string detail, string secondary) => detail;
        private static void LogLoreMissOnce(string tag, string input, int count, string heroId, string cultureId, string kingdomId, string role) { }
        private static void LogLoreContextTrace(string source, string heroId, string charId, string cultureId, string kingdomId, string settlementId, string role, bool isFemale, bool isClanLeader, string kingdomOverride, string inputText, bool invalidContext = false) { }
        private string GetPlayerAppearanceForPrompt() => "";
        private static string BuildPermanentPlayerAppearanceContext(string npc, string appearance) => "";
        private static void AppendPermanentPlayerAppearanceContext(System.Text.StringBuilder b, string npc, string appearance) { }
        private static string GetCurrentCharacterId(Hero hero, CharacterObject character) => character?.StringId ?? hero?.CharacterObject?.StringId ?? "";
        private static bool TryParseRoleIdentityId(string raw, out string kind, out string id) { kind = ""; id = ""; return false; }
        private static bool TryGetSkillValueById(string id, Hero hero, CharacterObject character, out int value) { value = 0; return false; }
        private static void EnsureTextMappings(LoreRule rule) { }
        private static string ResolveTextMappingValueOrFallback(LoreTextMapping mapping, LoreRule rule, Hero hero, CharacterObject character) => "";
        public string Render(string input, Hero npc, MentionedWorldEntities mentions, bool stale)
        {
            SelectionCalls = 0;
#if CURRENT
            var candidates = Retriever.CollectCandidateRules(mentions.Entities, true);
            if (candidates.OrderedRules.Count == 0) throw new Exception("preselected Lore candidates missing");
            string text = AIConfigHandler.GetLoreContextWithCandidates(input, npc, "", mentions, candidates, Index.Version + (stale ? 1 : 0));
            if (SelectionCalls != (stale ? 2 : 1)) throw new Exception("Lore selection branch count=" + SelectionCalls);
            return text;
#else
            string text = AIConfigHandler.GetLoreContext(input, npc, "", mentions);
            if (SelectionCalls != 1) throw new Exception("legacy Lore selection count=" + SelectionCalls);
            return text;
#endif
        }
    }
    internal sealed class FakePorts : IKnowledgeIndexPorts
    {
        public bool EmbeddingAvailable => false;
        public bool RerankerAvailable => false;
        public int SemanticTopK => 4;
        public float SemanticMinScore => 0.21f;
        public bool TryGetEmbedding(string text, out float[] vector) { vector = null; return false; }
        public bool TryScoreBatch(string query, IReadOnlyList<string> docs, out List<float> scores) { scores = null; return false; }
        public void Log(string category, string message) { }
    }
}

internal static class Program
{
    public static void Main()
    {
        var rule = new LoreRule { Id = "lore_city", Keywords = new List<string> { "Praven" }, Variants = new List<KnowledgeLibraryBehavior.LoreVariant> { new KnowledgeLibraryBehavior.LoreVariant { Content = "Praven is a port city." } } };
        var behavior = new KnowledgeLibraryBehavior(new List<LoreRule> { rule });
        var hero = new Hero { StringId = "npc_1", Name = new FakeName { Value = "Alda" }, Culture = new FakeCulture { StringId = "vlandia" } };
        string input = Environment.GetEnvironmentVariable("AF_J06_COMMON_INPUT") ?? "Tell me about Praven, Alda the King; can we barter this item?";
        string mentionsJson = Environment.GetEnvironmentVariable("AF_J06_COMMON_MENTIONS");
        var mentions = new MentionedWorldEntities { Entities = mentionsJson == null ? new List<string> { "Praven" } : System.Text.Json.JsonSerializer.Deserialize<List<string>>(mentionsJson) };
        var result = behavior.Render(input, hero, mentions, stale: false);
        if (!result.Contains("Praven is a port city.")) throw new Exception("real Lore text missing: " + result);
        Console.WriteLine("RESULT=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result)));
#if CURRENT
        var fallback = behavior.Render(input, hero, mentions, stale: true);
        if (fallback != result) throw new Exception("stale candidate fallback changed Lore text");
        Console.WriteLine("FALLBACK=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fallback)));
#endif
    }
}
