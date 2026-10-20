using System;
using System.Collections.Generic;

namespace AnimusForge;

// One bounded lookup per request; UI language and reference-recording language are independent.
internal sealed class GptSoVitsLanguage
{
    internal string Code { get; }
    internal string EngineCode { get; }
    internal bool IsForced => Code != "auto" && IsSupported;
    internal bool IsSupported => Code.Length != 0;
    private GptSoVitsLanguage(string code, string engineCode) { Code = code; EngineCode = engineCode; }
    internal static readonly GptSoVitsLanguage Auto = new GptSoVitsLanguage("auto", "auto");
    internal static readonly GptSoVitsLanguage Invalid = new GptSoVitsLanguage("", "");
    private static readonly GptSoVitsLanguage Chinese = new GptSoVitsLanguage("zh", "all_zh");
    private static readonly GptSoVitsLanguage Cantonese = new GptSoVitsLanguage("yue", "all_yue");
    private static readonly GptSoVitsLanguage English = new GptSoVitsLanguage("en", "en");
    private static readonly GptSoVitsLanguage Japanese = new GptSoVitsLanguage("ja", "all_ja");
    private static readonly Lazy<Dictionary<string, GptSoVitsLanguage>> LegacyAliases =
        new Lazy<Dictionary<string, GptSoVitsLanguage>>(() =>
        {
            var aliases = new Dictionary<string, GptSoVitsLanguage>(StringComparer.Ordinal);
            foreach (var entry in new[] {
                ("TtsLanguageAliases_Auto", Auto), ("TtsLanguageAliases_Chinese", Chinese),
                ("TtsLanguageAliases_Cantonese", Cantonese), ("TtsLanguageAliases_English", English),
                ("TtsLanguageAliases_Japanese", Japanese) })
                foreach (string alias in LlmSourceTexts.Get(entry.Item1).Split('|'))
                    aliases.Add(alias, entry.Item2);
            return aliases;
        });
    internal static GptSoVitsLanguage Parse(string value)
    {
        if ((value?.Length ?? 0) > 64) return Invalid;
        string normalized = (value ?? "").Trim().ToLowerInvariant().Replace('_', '-');
        switch (normalized)
        {
            case "": case "auto": return Auto;
            case "chinese": case "mandarin": case "zh": case "zh-cn": case "zh-tw": case "zh-hans": case "zh-hant": case "all-zh": return Chinese;
            case "cantonese": case "yue": case "zh-hk": case "all-yue": return Cantonese;
            case "english": case "en": case "en-us": case "en-gb": return English;
            case "japanese": case "ja": case "ja-jp": case "all-ja": return Japanese;
            default: return LegacyAliases.Value.TryGetValue(normalized, out var language) ? language : Invalid;
        }
    }
}
