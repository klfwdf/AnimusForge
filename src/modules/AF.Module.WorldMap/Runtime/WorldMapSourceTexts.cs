using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Queue identities and saved facts remain invariant across UI and content languages.
internal static class WorldMapSourceTexts
{
    private static readonly BilingualTextTable Table =
        BilingualTextTable.FromModuleData("WorldMapCompatibility.json", BilingualTextTable.ParseSections);

    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);
}
