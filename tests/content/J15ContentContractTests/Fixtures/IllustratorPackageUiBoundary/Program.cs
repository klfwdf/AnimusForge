using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using AnimusForge.Illustrator;
using AnimusForge.Illustrator.UI;

internal static class Program
{
    internal static string ModuleData;
    internal static Dictionary<string, string> Ui;
    internal static int UiThread;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    static int Main(string[] args)
    {
        ModuleData = args[0];
        Ui = XDocument.Load(args[1]).Descendants("string").ToDictionary(n => (string)n.Attribute("id"), n => (string)n.Attribute("text"));
        UiThread = Environment.CurrentManagedThreadId;
        var package = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(ModuleData, "IllustratorPromptTexts.json")));
        // Invoke the production package reader and metadata formatter on a worker.
        var values = Task.Run(() => {
            Check(Environment.CurrentManagedThreadId != UiThread, "actual worker");
            var hero = IllustratorText.Get("Hero_319");
            var partner = IllustratorText.Get("Conversation_31");
            var player = IllustratorText.Get("Side_Player");
            var cachePartner = IllustratorText.Get("Card_DefaultPartnerCacheName");
            return new[] {hero, partner, player,
                IllustratorText.Format("Card_PortraitCacheTitle", hero),
                IllustratorText.Format("Card_ConversationCacheTitle", cachePartner),
                IllustratorText.Get("Card_ConversationReferenceReuse"),
                IllustratorText.Get("Scene_PanoramaNote")};
        }).GetAwaiter().GetResult();
        Check(values[0] == package["Hero_319"] && values[1] == package["Conversation_31"] && values[2] == package["Side_Player"], "worker names follow package, not UI");
        Check(values[5] == package["Card_ConversationReferenceReuse"] && values[6] == package["Scene_PanoramaNote"], "worker facts follow package");
        string heroTitle = IllustratorUiText.ProjectKnownDefaultTitle(values[3], "encyclopedia");
        string partnerTitle = IllustratorUiText.ProjectKnownDefaultTitle(values[4], "conversation");
        Check(heroTitle == Ui["AF_ILL_UI_PORTRAIT_TITLE"].Replace("{HERO}", Ui["AF_ILL_UI_DEFAULT_HERO_NAME"]), "hero metadata projects to independent UI language");
        Check(partnerTitle == Ui["AF_ILL_UI_CONVERSATION_TITLE"].Replace("{HERO}", Ui["AF_ILL_UI_DEFAULT_PARTNER_NAME"]), "partner metadata projects to independent UI language");
        const string name = "Lady {0} $1 {HERO}";
        string custom = IllustratorText.Format("Card_PortraitCacheTitle", name);
        Check(custom.Contains(name), "package formatting does not reinterpret inserted name");
        Check(IllustratorUiText.ProjectKnownDefaultTitle(custom, "encyclopedia").Contains(name), "UI projection preserves literal name placeholders");
        const string userTitle = "My custom title {HERO} $1";
        Check(IllustratorUiText.ProjectKnownDefaultTitle(userTitle, "encyclopedia") == userTitle, "custom title unchanged");
        Check(IllustratorUiText.ProjectKnownDefaultTitle("Real Hero 纪事肖像", "encyclopedia").Contains("Real Hero"), "old saved title projects without changing actor");
        Ui = XDocument.Load(args[2]).Descendants("string").ToDictionary(n => (string)n.Attribute("id"), n => (string)n.Attribute("text"));
        Check(Task.Run(() => IllustratorText.Get("Hero_319")).GetAwaiter().GetResult() == values[0], "UI switch cannot change package prompt");
        Check(IllustratorUiText.ProjectKnownDefaultTitle(values[3], "encyclopedia") == Ui["AF_ILL_UI_PORTRAIT_TITLE"].Replace("{HERO}", Ui["AF_ILL_UI_DEFAULT_HERO_NAME"]), "UI switch updates only display projection");
        Console.WriteLine("PASS production IllustratorText/IllustratorUiText package-worker and independent UI boundary");
        return 0;
    }
}
namespace AnimusForge { internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string file) => Path.Combine(Program.ModuleData, file); } }
namespace TaleWorlds.Localization
{
    // Only the game localization surface is faked. Production readers and UI
    // projection code above compile unchanged; real Gauntlet/game rendering is separate.
    public sealed class TextObject
    {
        readonly string _text;
        readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        public TextObject(string text)
        {
            if (Environment.CurrentManagedThreadId != Program.UiThread) throw new Exception("UI locale resolved on worker");
            int end = text.IndexOf('}'); string key = text.Substring(2, end - 2);
            _text = Program.Ui.TryGetValue(key, out string translated) ? translated : text.Substring(end + 1);
        }
        public void SetTextVariable(string name, string value) => _variables[name] = value;
        public override string ToString() => Regex.Replace(_text, @"\{([A-Z_]+)\}", match => _variables.TryGetValue(match.Groups[1].Value, out string value) ? value : match.Value);
    }
}
