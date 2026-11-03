using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Runtime text in Chinese and English (KnowledgeSourceTexts.en.json). Get returns the current game language;
// Variants returns both for matching text written earlier or by a worldbook of the other language.
internal static class KnowledgeSourceTexts
{
    private static readonly BilingualTextTable Table =
        BilingualTextTable.FromEmbedded(typeof(KnowledgeSourceTexts).Assembly, "AnimusForge.Knowledge.SourceTexts.json");

    internal static string Get(string id) => Table.Get(id);
    internal static string[] Variants(string id) => Table.Variants(id);
}
