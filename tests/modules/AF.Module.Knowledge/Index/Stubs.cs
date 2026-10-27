using System.Collections.Generic;

namespace AnimusForge;

internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string fileName) => System.IO.Path.Combine(
        System.Environment.CurrentDirectory, "content/modules/AF.Module.Knowledge/ModuleData", fileName);
}

// Harness stub: only the rule record type the index depends on (production type is nested in the campaign behavior).
public partial class KnowledgeLibraryBehavior
{
    public KnowledgeFile Data = new KnowledgeFile();
    public string ExportRulesJson() => Newtonsoft.Json.JsonConvert.SerializeObject(Data);
    public List<string> GetRuleIdsForDev(int max) => System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(Data.Rules, r => r.Id));
    public static bool IsPlayerPersonaRuleId(string id) => id == "rule_animus_player_persona";
    public class LoreRule
    {
        public string Id;
        public List<string> Keywords = new List<string>();
        public List<string> RagShortTexts = new List<string>();
        public List<string> SemanticPrototypes;
        public List<LoreVariant> Variants = new List<LoreVariant>();
        public List<LoreTextMapping> TextMappings;
    }

    public class KnowledgeFile { public int Version; public List<LoreRule> Rules = new List<LoreRule>(); }
    public class LoreVariant { public LoreWhen When; public string Content; public int Priority; }
    public class LoreTextMapping { public string SourceText, Kind, TargetId, EmptyValueText, TrueText, FalseText; public int? AgeMin, AgeMax; }
    public class LoreWhen
    {
        public List<string> HeroIds, Cultures, KingdomIds, SettlementIds, Roles, IdentityIds;
        public bool? IsFemale, IsClanLeader;
        public Dictionary<string, int> SkillMin;
    }
}

internal static class Logger { internal static void Log(string channel, string text) { } }
// UI diagnostics are declarations only; this harness tests the real reader and validation owner.
internal static class KnowledgeUiTexts
{
    internal const string ImportBehaviorUnavailable = "unavailable", ImportRuleEmpty = "empty", ImportRuleIdEmpty = "id empty", ImportNoRulesFound = "no rules", DatabaseReloadEmptyEntry = "empty";
    internal static string ImportDuplicateKeyword(string k) => "duplicate:" + k;
    internal static string ImportExistingKeyword(string k) => "existing:" + k;
    internal static string ImportKeywordConflict(string k, string a, string b) => "conflict:" + k;
    internal static string ImportFolderDuplicateKeyword(string k, string a, string b) => "folder:" + k;
    internal static string ImportValidationFailed(string error) => error;
    internal static string DatabaseReloadRuleIdEmpty(string source) => source;
    internal static string DatabaseReloadDuplicateId(string id) => id;
}
