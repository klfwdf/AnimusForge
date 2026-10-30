using System;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Connects <see cref="PromptLanguage"/> to the game's text language. Simplified (zh-HANS) and Traditional
/// (zh-HANT) Chinese select Chinese fixed prompts; every other language selects English. Installed once at
/// module load; a language change in the options screen republishes the prompt configuration snapshot so
/// rule prompts read after the change use the new language.
/// </summary>
internal static class GamePromptLanguageSource
{
    private static bool _installed;

    internal static void Install()
    {
        if (_installed) return;
        _installed = true;
        PromptLanguage.Install(() => MBTextManager.ActiveTextLanguage, IsChineseLanguage);
        AnimusForge.SceneActions.Core.SceneActionPromptPackage.SetLanguageSelector(() => PromptLanguage.IsChinese);
        ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(
            ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
        Logger.Log("PromptLanguage", "Installed. GameLanguage=" + (MBTextManager.ActiveTextLanguage ?? "") + " Prompt=" + PromptLanguage.Code);
    }

    private static bool IsChineseLanguage(string languageId)
    {
        if (string.IsNullOrEmpty(languageId)) return false;
        string subtitle = LocalizedTextManager.GetSubtitleExtensionOfLanguage(languageId) ?? "";
        return subtitle.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
    }

    private static void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType type)
    {
        if (type != ManagedOptions.ManagedOptionsType.Language) return;
        try
        {
            if (!PromptLanguage.Refresh()) return;
            Logger.Log("PromptLanguage", "Game language changed to " + (MBTextManager.ActiveTextLanguage ?? "") + "; prompt language is now " + PromptLanguage.Code + ".");
            AIConfigHandler.ReloadConfig();
        }
        catch (Exception ex)
        {
            Logger.Log("PromptLanguage", "[Error] Language change handling failed: " + ex.Message);
        }
    }
}
