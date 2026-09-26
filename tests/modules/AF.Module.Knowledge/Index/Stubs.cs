using System.Collections.Generic;

namespace AnimusForge;

// Harness stub: only the rule record type the index depends on (production type is nested in the campaign behavior).
public class KnowledgeLibraryBehavior
{
    public class LoreRule
    {
        public string Id;
        public List<string> Keywords = new List<string>();
        public List<string> RagShortTexts = new List<string>();
        public List<LoreVariant> Variants = new List<LoreVariant>();
    }

    public class KnowledgeFile { public int Version; public List<LoreRule> Rules = new List<LoreRule>(); }
    public class LoreVariant { public LoreWhen When; }
    public class LoreWhen
    {
        public List<string> HeroIds, Cultures, KingdomIds, SettlementIds, Roles;
        public bool? IsFemale, IsClanLeader;
        public Dictionary<string, int> SkillMin;
    }
}
