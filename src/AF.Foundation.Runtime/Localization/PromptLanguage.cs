using System;
using System.IO;

namespace AnimusForge;

/// <summary>
/// Language of fixed prompt text, facts and history written from now on. It follows the game's text
/// language: Simplified and Traditional Chinese select Chinese, every other language selects English.
/// Only player-editable prompts (CustomPrompts defaults, MCM prompt defaults, worldbooks) stay with the
/// installed CN/EN package. The host installs the language source once; a read is a field comparison,
/// and classification is recomputed only when the game's language id changes.
/// </summary>
internal static class PromptLanguage
{
    private sealed class State
    {
        internal readonly string LanguageId;
        internal readonly bool Chinese;
        internal State(string languageId, bool chinese) { LanguageId = languageId; Chinese = chinese; }
    }

    private static Func<string> _languageId;
    private static Func<string, bool> _isChineseLanguage;
    private static volatile State _state = new State(null, true);

    internal static void Install(Func<string> languageId, Func<string, bool> isChineseLanguage)
    {
        _isChineseLanguage = isChineseLanguage;
        _languageId = languageId;
        Refresh();
    }

    /// <summary>True when fixed text should be Chinese. Without an installed host (tests, tools) the CN corpus is used.</summary>
    internal static bool IsChinese
    {
        get
        {
            Func<string> source = _languageId;
            if (source == null) return _state.Chinese;
            string id;
            try { id = source() ?? ""; }
            catch { return _state.Chinese; }
            State state = _state;
            return string.Equals(state.LanguageId, id, StringComparison.Ordinal) ? state.Chinese : Update(id);
        }
    }

    internal static string Code => IsChinese ? "zh" : "en";

    /// <summary>Re-reads the game language; returns true when the Chinese/English selection flipped.</summary>
    internal static bool Refresh()
    {
        Func<string> source = _languageId;
        if (source == null) return false;
        bool before = _state.Chinese;
        try { return Update(source() ?? "") != before; }
        catch { return false; }
    }

    private static bool Update(string id)
    {
        bool chinese;
        // A language the game cannot describe is not Chinese.
        try { chinese = _isChineseLanguage?.Invoke(id) ?? true; }
        catch { chinese = false; }
        _state = new State(id, chinese);
        return chinese;
    }

    /// <summary>ModuleData path of a fixed prompt file for the current language. English uses the
    /// <c>X.en.json</c> sibling and falls back to <c>X.json</c> only when that sibling is absent.</summary>
    internal static string ResolveModuleDataPath(string fileName, bool english)
        => english ? EnglishSiblingOrSelf(AnimusForgeModulePaths.GetModuleDataFilePath(fileName))
            : AnimusForgeModulePaths.GetModuleDataFilePath(fileName);

    internal static string EnglishSiblingOrSelf(string path)
    {
        string candidate = EnglishSibling(path);
        return candidate != null && File.Exists(candidate) ? candidate : path;
    }

    internal static string EnglishSibling(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string extension = Path.GetExtension(path);
        return path.Substring(0, path.Length - extension.Length) + ".en" + extension;
    }
}

/// <summary>A fixed prompt resource kept in both languages. Each edition loads once, on first use in that language.</summary>
internal sealed class PromptLanguageFile<T> where T : class
{
    private readonly Lazy<T> _chinese;
    private readonly Lazy<T> _english;

    internal PromptLanguageFile(string fileName, Func<string, T> load)
    {
        _chinese = new Lazy<T>(() => load(PromptLanguage.ResolveModuleDataPath(fileName, false)), true);
        _english = new Lazy<T>(() => load(PromptLanguage.ResolveModuleDataPath(fileName, true)), true);
    }

    internal T Value => PromptLanguage.IsChinese ? _chinese.Value : _english.Value;
}

/// <summary>A value derived from per-language resources; each language computes it once.</summary>
internal sealed class PromptLanguageValue<T>
{
    private readonly Lazy<T> _chinese;
    private readonly Lazy<T> _english;

    internal PromptLanguageValue(Func<T> build)
    {
        _chinese = new Lazy<T>(build, true);
        _english = new Lazy<T>(build, true);
    }

    internal T Value => PromptLanguage.IsChinese ? _chinese.Value : _english.Value;
}
