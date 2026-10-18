using System;

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
    internal static GptSoVitsLanguage Parse(string value)
    {
        if ((value?.Length ?? 0) > 64) return Invalid;
        switch ((value ?? "").Trim().ToLowerInvariant().Replace('_', '-'))
        {
            case "": case "auto": case "自动": case "自動": case "跟随正文": return Auto;
            case "中文": case "汉语": case "漢語": case "普通话": case "普通話": case "国语": case "國語":
            case "chinese": case "mandarin": case "zh": case "zh-cn": case "zh-tw": case "zh-hans": case "zh-hant": case "all-zh": return Chinese;
            case "粤语": case "粵語": case "广东话": case "廣東話": case "cantonese": case "yue": case "zh-hk": case "all-yue": return Cantonese;
            case "英语": case "英語": case "英文": case "english": case "en": case "en-us": case "en-gb": return English;
            case "日语": case "日語": case "日文": case "日本語": case "japanese": case "ja": case "ja-jp": case "all-ja": return Japanese;
            default: return Invalid; // Never silently fall back to auto for an explicit unsupported choice.
        }
    }
}
