using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AnimusForge;
using TaleWorlds.Localization;

internal static class Program
{
    internal static string PackageRoot;
    private static int checks;
    private static void Check(bool value, string label)
    { checks++; if (!value) throw new InvalidOperationException(label); }

    private static void Register(string path, string language)
    {
        var data = new XmlDocument();
        data.LoadXml("<LanguageData id=\"" + language + "\" name=\"" + language
            + "\" supported_iso=\"en\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\"><LanguageFile xml_path=\""
            + Path.GetFileName(path) + "\" /></LanguageData>");
        var languageData = typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData", true);
        languageData.GetMethod("LoadFromXml", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { data, Path.GetDirectoryName(path) });
    }

    private static void History()
    {
        const string literal = "keep {REASON} {0} $1";
        string label = SceneHistoryMessageAssemblyOwner.FormatScenePlayerDirectSpeechLabel("Alice", -1);
        foreach (string prefix in new[] { "玩家:", "玩家：", "你:", "你：", "Player:", "You:" })
            Check(HistorySectionProjectionOwner.NormalizeScenePlayerHistoryLine(prefix + " " + literal, playerName: "Alice") == label + ": " + literal,
                "old and English player headers normalize without missing resource keys");
        Check(HistorySectionProjectionOwner.NormalizeScenePlayerHistoryLine(literal, playerName: "Alice") == label + ": " + literal, "ordinary user data remains literal");
        Check(HistorySectionProjectionOwner.NormalizeScenePlayerHistoryLine(label + ": " + literal, playerName: "Alice") == label + ": " + literal, "idempotent player label");
        foreach (string header in new[] { "【近期对话窗口】", "【最近对话历史】", "[Recent dialogue window]", "[Recent dialogue history]" })
        {
            Check(HistorySectionProjectionOwner.IsPrivateRecentWindowHeader(header), "both private history languages recognized");
            HistorySectionProjectionOwner.SplitPersistedHeroHistorySections("【其它记录】\npublic\n" + header + "\nprivate\n【其它事实】\nfact", out string recent, out string other);
            Check(recent.Replace("\r", "") == header + "\nprivate", "recent window bounded by next section");
            Check(other.Replace("\r", "") == "【其它记录】\npublic\n【其它事实】\nfact", "public facts retained outside private window");
        }
        Check(!HistorySectionProjectionOwner.IsPrivateRecentWindowHeader("ordinary text"), "non-header does not become recent window");
        Check(!HistorySectionProjectionOwner.IsPrivateRecentWindowHeader(""), "empty history");
    }

    private static void Status(string root)
    {
        string baseDir = Path.Combine(root, "content/modules/AF.Module.Diplomacy/ModuleData/Languages");
        Register(Path.Combine(baseDir, "afdip_misc_strings.xml"), "English");
        Register(Path.Combine(baseDir, "CNs/afdip_misc_strings-zh-CN.xml"), "简体中文");
        foreach (string language in new[] { "English", "简体中文", "English" })
        {
            Check(MBTextManager.ChangeLanguage(language), "actual UI language switch");
            foreach (string reason in new[] { "No authority", "literal {REASON} {0} $1", "0", "", "原始玩家文本" })
            {
                string raw = string.Format(DiplomacyMiscPromptTexts.Get("OralDiplomacy_SubmissionRejected", ""), reason);
                string expected = language == "English" ? "Diplomatic commitment not submitted: " + reason + "." : "外交约定暂未提交：" + reason + "。";
                Check(DiplomacyDialogueStatusUi.Project(raw) == expected, "rejected status retains exact reason through real TextObject");
                Check(raw == string.Format(DiplomacyMiscPromptTexts.Get("OralDiplomacy_SubmissionRejected", ""), reason), "raw package result is not modified");
            }
            const string unknown = "user {NAME} $1";
            Check(DiplomacyDialogueStatusUi.Project(unknown) == unknown, "unknown message stays literal");
        }
    }

    internal static void Main(string[] args)
    {
        try
        {
            PackageRoot = args[0];
            History();
            Status(args[1]);
            Console.WriteLine("PASS history/status " + args[2] + ": " + checks + " assertions; whole production history/status owners, actual package readers and real TaleWorlds TextObject; speech label source-derived and unrelated leaves controlled.");
        }
        catch (Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
                Console.Error.WriteLine(current.GetType().FullName + ": " + current.Message);
            Environment.Exit(1);
        }
    }
}

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    { internal static string GetModuleDataFilePath(string name) => Path.Combine(Program.PackageRoot, name); }

    // Unused presentation/filter leaves; tests directly exercise whole production
    // NormalizeScenePlayerHistoryLine, IsPrivateRecentWindowHeader and section splitter.
    internal readonly struct ConversationSpeechTextOptions { }
    internal static class ConversationSpeechTextRules
    {
        internal static bool IsLeakedPromptLineForShout(string text) => false;
        internal static string NormalizeNativeConversationVisibleTextKey(string text, ConversationSpeechTextOptions options) => text;
    }
    internal static class ShoutUtils
    { internal static string StripNamePrefixedLineSafely(string text, int max) => text; }
}
