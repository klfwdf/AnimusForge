using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class KnowledgeSourceTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
    {
        using (Stream stream = typeof(KnowledgeSourceTexts).Assembly.GetManifestResourceStream("AnimusForge.Knowledge.SourceTexts.json"))
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    });

    internal static string Get(string id) => Values.Value[id];
}
