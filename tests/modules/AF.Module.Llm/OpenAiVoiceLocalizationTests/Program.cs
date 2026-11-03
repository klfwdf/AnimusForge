using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using AnimusForge;
using Newtonsoft.Json.Linq;
using TaleWorlds.Localization;

// Real catalog, embedded compatibility reader and native TextObject; disk paths/logging
// are isolated leaves. No game, network provider or live audio acceptance is claimed.
internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    private static void Register(string path, string language)
    {
        var metadata = new XmlDocument();
        metadata.LoadXml("<LanguageData id=\"" + language + "\" name=\"" + language
            + "\" supported_iso=\"" + (language == "English" ? "en" : "zh-CN")
            + "\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\"><LanguageFile xml_path=\""
            + Path.GetFileName(path) + "\" /></LanguageData>");
        typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData", true)
            .GetMethod("LoadFromXml", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { metadata, Path.GetDirectoryName(path) });
    }

    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        AnimusForgeDataPaths.Root = Path.GetFullPath(args[1]);
        string ui = Path.Combine(root, "content/modules/AF.Module.Llm/ModuleData/Languages");
        var paths = new Dictionary<string, string> {
            ["English"] = Path.Combine(ui, "afllm_ui_strings.xml"),
            ["简体中文"] = Path.Combine(ui, "CNs/afllm_ui_strings-zh-CN.xml") };
        foreach (var pair in paths) Register(pair.Value, pair.Key);
        const string url = "https://voice.example/v1/audio/speech";
        string endpoint = OpenAiTtsVoiceCatalog.VoicesEndpoint(url);
        var builtin = OpenAiTtsVoiceCatalog.For(url, "male-clear");
        Check(builtin.IsBuiltIn && builtin.Voices.Count == 12, "complete fallback catalog");
        Check(OpenAiTtsVoiceCatalog.For(url, "unrelated-provider-voice") == null, "no foreign-provider fallback");
        var models = OpenAiTtsVoiceCatalog.Parse(endpoint,
            "{\"data\":[{\"id\":\"male-clear\"},{\"id\":\"male-clear_happy\"},{\"id\":\"chat-model\"}]}", true);
        Check(models.Voices.Count == 1 && models.Get("male-clear").HasHappy, "model filtering and variants");
        foreach (string language in new[] { "English", "简体中文", "English", "简体中文" })
        {
            Check(MBTextManager.ChangeLanguage(language), "native language switch");
            var labels = XDocument.Load(paths[language]).Descendants("string")
                .ToDictionary(e => (string)e.Attribute("id"), e => (string)e.Attribute("text"));
            foreach (var voice in builtin.Voices)
            {
                Check(voice.Label == labels["AF_GPT_UI_OpenAiBuiltInVoice_" + voice.Id.Replace('-', '_')] + " (" + voice.Id + ")", "all builtin labels switch without catalog rebuild");
                Check(voice.Gender == (voice.Id.StartsWith("male-") ? 1 : voice.Id.StartsWith("female-") ? 2 : 0), "stable ids and gender");
            }
            Check(models.Get("male-clear").Label == builtin.Get("male-clear").Label, "model fallback label uses UI language");
            var custom = OpenAiTtsVoiceCatalog.Parse(endpoint,
                "{\"voices\":[{\"id\":\"male-clear\",\"name\":\"服务商原始名称\",\"gender\":\"男\"}]}");
            Check(custom.Get("male-clear").Label == "服务商原始名称 (male-clear)", "dynamic provider names stay literal");
        }
        foreach (var pair in new Dictionary<string,int> { ["女"]=2, ["女性"]=2, ["female"]=2, ["F"]=2, ["woman"]=2,
            ["男"]=1, ["男性"]=1, ["male"]=1, ["M"]=1, ["man"]=1, ["unknown"]=0, [""]=0 })
        {
            var body = new JObject { ["voices"] = new JArray(new JObject { ["id"]="custom", ["gender"]=pair.Key }) };
            Check(OpenAiTtsVoiceCatalog.Parse(endpoint, body.ToString()).Get("custom").Gender == pair.Value, "gender alias compatibility");
        }
        for (int i=0; i<30; i++)
        {
            Check(builtin.Get(builtin.PickRandom(false)).Gender == 1, "male pool unchanged");
            Check(builtin.Get(builtin.PickRandom(true)).Gender == 2, "female pool unchanged");
        }
        OpenAiTtsVoiceCatalog.Publish(models);
        Check(OpenAiTtsVoiceCatalog.ApplyTone(url, "male-clear", "male-clear", LocalTtsTone.Happy) == "male-clear_happy", "supported tone");
        Check(OpenAiTtsVoiceCatalog.ApplyTone(url, "male-clear", "male-clear", LocalTtsTone.Angry) == "male-clear", "unsupported tone fallback");
        typeof(OpenAiTtsVoiceCatalog).GetMethod("WriteCache", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[]{ models });
        typeof(OpenAiTtsVoiceCatalog).GetField("_current", BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
        typeof(OpenAiTtsVoiceCatalog).GetField("_cacheLoaded", BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,false);
        var restored = OpenAiTtsVoiceCatalog.For(url, "male-clear");
        Check(restored.Get("male-clear").UsesBuiltInLabel && restored.Get("male-clear").Label == builtin.Get("male-clear").Label, "localized model label survives cache reload");
        Check(restored.Get("male-clear").HasHappy, "variant survives cache reload");
        Console.WriteLine("PASS OpenAI voice localization checks=" + _checks);
        return 0;
    }
}

namespace AnimusForge
{
    // Enum-only leaf from LocalTtsToneTag; tone parsing is outside this catalog test.
    internal enum LocalTtsTone { Neutral = 0, Happy = 1, Angry = 2, Sad = 3 }
    internal static class Logger { internal static void Log(string area, string message) { } }
    internal static class AnimusForgeModulePaths
    {
        internal static string GetModuleDataFilePath(string name) => throw new InvalidOperationException("Embedded voice tables must not read module files");
    }
    internal static class AnimusForgeDataPaths
    {
        internal static string Root;
        internal static string GetCurrentRoot() => Root;
        internal static string GetCacheDirectory(string root) => Path.Combine(root, "cache");
    }
}
