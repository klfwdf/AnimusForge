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
    }
}
