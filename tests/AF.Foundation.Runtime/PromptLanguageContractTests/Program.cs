using System;
using System.IO;
using System.Text;
using AnimusForge;
using AnimusForge.SceneActions.Core;

// Contract: fixed prompt text follows the game's text language (zh-HANS/zh-HANT -> CN, else EN),
// both editions ship in one module, each edition loads once, and a language switch is observed
// by the next read without reloading the other edition.
internal static class Program
{
    internal static string ModuleRoot;
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS " + name);
    }

    private static int Main()
    {
        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        while (!File.Exists(Path.Combine(repo, "AnimusForge.csproj"))) repo = Path.GetDirectoryName(repo);
        ModuleRoot = Path.Combine(repo, "artifacts", "prompt-language-tests", Guid.NewGuid().ToString("N"));
        string moduleData = Path.Combine(ModuleRoot, "ModuleData");
        Directory.CreateDirectory(moduleData);
        try
        {
            string promptDir = Path.Combine(repo, "content/modules/AF.Module.Prompt/ModuleData");
            File.Copy(Path.Combine(promptDir, "PromptComposerTexts.zh-CN.json"), Path.Combine(moduleData, "PromptComposerTexts.json"));
            File.Copy(Path.Combine(promptDir, "PromptComposerTexts.en.json"), Path.Combine(moduleData, "PromptComposerTexts.en.json"));
            string sceneDir = Path.Combine(repo, "content/modules/AnimusForge.XihaiAction/ModuleData");
            File.Copy(Path.Combine(sceneDir, "SceneActionPromptTexts.xml"), Path.Combine(moduleData, "SceneActionPromptTexts.xml"));
            File.Copy(Path.Combine(sceneDir, "SceneActionPromptTexts.en.xml"), Path.Combine(moduleData, "SceneActionPromptTexts.en.xml"));

            Check(PromptLanguage.IsChinese, "without a host the Chinese corpus is the default");

            string language = "English";
            int sourceReads = 0;
            PromptLanguage.Install(() => { sourceReads++; return language; },
                id => id == "简体中文" || id == "繁體中文");
            Check(!PromptLanguage.IsChinese && PromptLanguage.Code == "en", "English game language selects English");
            string english = PromptComposerTexts.Get("extras_player");
            Check(!HasHan(english), "English edition text has no Chinese: " + english);

            language = "简体中文";
            Check(PromptLanguage.IsChinese, "Simplified Chinese selects Chinese");
            string chinese = PromptComposerTexts.Get("extras_player");
            Check(HasHan(chinese) && chinese != english, "Chinese edition is read after the switch: " + chinese);

            language = "繁體中文";
            Check(PromptLanguage.IsChinese, "Traditional Chinese selects Chinese");
            language = "Deutsch";
            Check(!PromptLanguage.IsChinese, "any other language selects English");
            Check(PromptComposerTexts.Get("extras_player") == english, "English edition is cached and reused");

            language = "Français";
            Check(PromptLanguage.Refresh() == false, "Refresh reports no flip between two English-class languages");
            language = "简体中文";
            Check(PromptLanguage.Refresh(), "Refresh reports a flip to Chinese");

            PromptLanguage.Install(() => throw new InvalidOperationException("engine not ready"), id => true);
            Check(PromptLanguage.IsChinese, "a failing language source keeps the last selection");
            PromptLanguage.Install(() => "Klingon", id => throw new InvalidOperationException("unknown"));
            Check(!PromptLanguage.IsChinese, "an unclassifiable language is treated as English");

            string missing = Path.Combine(moduleData, "OnlyChinese.json");
            File.WriteAllText(missing, "{}", Encoding.UTF8);
            Check(PromptLanguage.ResolveModuleDataPath("OnlyChinese.json", true) == missing,
                "English falls back to the base file when no .en sibling exists");
            Check(PromptLanguage.ResolveModuleDataPath("PromptComposerTexts.json", true).EndsWith("PromptComposerTexts.en.json", StringComparison.Ordinal),
                "English resolves the .en sibling");

            bool preferChinese = false;
            SceneActionPromptPackage.SetLanguageSelector(() => preferChinese);
            SceneActionPromptPackage.Load(Path.Combine(moduleData, "SceneActionPromptTexts.xml"));
            Check(!SceneActionPromptPackage.UseChinese, "scene package follows the English selector");
            string sceneEnglish = SceneActionPromptPackage.NativeConversationActionInstruction;
            preferChinese = true;
            Check(SceneActionPromptPackage.UseChinese, "scene package follows the Chinese selector");
            Check(SceneActionPromptPackage.NativeConversationActionInstruction != sceneEnglish
                && HasHan(SceneActionPromptPackage.NativeConversationActionInstruction), "scene package returns the Chinese corpus");
            Check(!HasHan(sceneEnglish), "scene package English corpus has no Chinese");

            // BilingualTextTable: Get follows the language, "=" keeps the source, matching accepts both.
            PromptLanguage.Install(() => language, id => id == "简体中文");
            language = "English";
            Check(KnowledgeSourceTexts.Get("KN_Raw_007") == "the player", "table Get returns English under an English game");
            Check(KnowledgeSourceTexts.Get("KN_Raw_000") == "的", "an English '=' entry keeps the Chinese source identity");
            string[] playerWords = KnowledgeSourceTexts.Variants("KN_Raw_007");
            Check(playerWords.Length == 2 && playerWords[0] == "玩家" && playerWords[1] == "the player", "Variants lists Chinese then English");
            Check(KnowledgeSourceTexts.Variants("KN_Raw_000").Length == 1, "Variants collapses identical editions");
            language = "简体中文";
            Check(KnowledgeSourceTexts.Get("KN_Raw_007") == "玩家", "table Get returns Chinese under a Chinese game");
            var table = BilingualTextTable.FromLoaders(
                () => new System.Collections.Generic.Dictionary<string, string> { ["A"] = "甲", ["B"] = "乙" },
                () => new System.Collections.Generic.Dictionary<string, string> { ["A"] = "Alpha" });
            language = "English";
            Check(table.Get("B") == "乙", "a missing English entry keeps the Chinese source");
            Check(table.Is("甲", "A") && table.Is("Alpha", "A") && !table.Is("Beta", "A"), "Is matches either language only");

            Console.WriteLine("PromptLanguage contract checks=" + _checks);
            return 0;
        }
        finally
        {
            try { Directory.Delete(ModuleRoot, true); } catch { }
        }
    }

    private static bool HasHan(string text)
    {
        foreach (char c in text ?? "") if (c >= '\u3400' && c <= '\u9fff') return true;
        return false;
    }
}

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    {
        internal static string GetModuleDataFilePath(string fileName) => Path.Combine(Program.ModuleRoot, "ModuleData", fileName);
    }
}
