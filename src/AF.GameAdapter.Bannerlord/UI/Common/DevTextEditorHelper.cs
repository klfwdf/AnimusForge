using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public static class DevTextEditorHelper
{
	public static void ShowLongTextEditor(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSave, Action onCancel, string saveText = null, string cancelText = null, Func<string> resetText = null)
	{
		saveText = saveText ?? new TextObject("{=AF_UI_Common_Save}Save").ToString();
		cancelText = cancelText ?? new TextObject("{=AF_UI_Common_Back}Back").ToString();
		string safeInitialText = AnimusForgeTextInputSanitizer.SanitizeMultiline(initialText, AnimusForgeTextInputSanitizer.MaxLongEditorChars, out bool initialWasTruncated);
		Action<string> safeOnSave = delegate(string text)
		{
			InvokeSaveSafely(onSave, text);
		};
		if (DevHistoryEditPopup.Show(titleText, subtitleText, safeInitialText, initialText, safeOnSave, onCancel, inputHintText, saveText, cancelText, resetText))
		{
			return;
		}
		if (resetText != null) throw new InvalidOperationException(new TextObject("{=AF_UI_Editor_OpenFailed}The text editor could not be opened. Close it and try again.").ToString());
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, BuildFallbackDescription(subtitleText, inputHintText), isAffirmativeOptionShown: true, isNegativeOptionShown: true, saveText, cancelText, safeOnSave, onCancel, shouldInputBeObfuscated: false, null, "", safeInitialText));
        if (initialWasTruncated) NotifyInputTruncated();
	}

    // Owned nested editors expose cancellation to their host; ordinary editors retain the native inquiry fallback above.
    public static IDisposable ShowOwnedLongTextEditor(string titleText, string subtitleText, string inputHintText,
        string initialText, Action<string> onSave, Action onCancel, ScreenLayer inputOwner, Func<bool> isInputOwnerAlive,
        string saveText = null, string cancelText = null)
    {
        saveText = saveText ?? new TextObject("{=AF_UI_Common_Save}Save").ToString();
        cancelText = cancelText ?? new TextObject("{=AF_UI_Common_Back}Back").ToString();
        string initial = AnimusForgeTextInputSanitizer.SanitizeMultiline(initialText, AnimusForgeTextInputSanitizer.MaxLongEditorChars);
        Action<string> save = text => InvokeSaveSafely(onSave, text);
        if (DevHistoryEditPopup.TryShowOwned(titleText, subtitleText, initial, initialText, save, onCancel,
            inputHintText, saveText, cancelText, inputOwner, isInputOwnerAlive, onCancel, out var session)) return session;
        throw new InvalidOperationException(new TextObject("{=AF_UI_Editor_OpenFailed}The text editor could not be opened. Close it and try again.").ToString());
    }

    // Shared by the custom editor and native inquiry fallback. Do not retry callbacks:
    // some callers have already changed state or opened a successor menu when they fail.
    internal static void InvokeSaveSafely(Action<string> onSave, string input)
    {
        try
        {
            string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(input,
                AnimusForgeTextInputSanitizer.MaxLongEditorChars, out bool wasTruncated);
            if (wasTruncated) NotifyInputTruncated();
            onSave?.Invoke(text);
        }
        catch (Exception ex)
        {
            // Do not log the draft or exception message, which may contain player text.
            try { Logger.Log("DevHistoryPopup", "[ERROR] Save callback failed: " + ex.GetType().FullName + "\n" + ex.StackTrace); } catch { }
            try
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject(
                    "{=AF_UI_Editor_SaveFailed}Could not complete saving. Check the log and verify the saved text before trying again.").ToString()));
            }
            catch { } // A notification failure must not escape back into the UI command.
        }
    }

    internal static string GetInputTruncatedText() => new TextObject(
        "{=AF_UI_Editor_InputTruncated}Some input was truncated at the limit; check the end of your text.").ToString();

    private static void NotifyInputTruncated()
    {
        try { InformationManager.DisplayMessage(new InformationMessage(GetInputTruncatedText())); } catch { }
    }

	private static string BuildFallbackDescription(string subtitleText, string inputHintText)
	{
		string text = (subtitleText ?? "").Trim();
		string text2 = ((inputHintText ?? "").Trim() + "\n" + new TextObject(
            "{=AF_UI_Editor_LengthLimit}Maximum: {MAX} characters.")
            .SetTextVariable("MAX", AnimusForgeTextInputSanitizer.MaxLongEditorChars).ToString()).Trim();
		if (string.IsNullOrEmpty(text))
		{
			return text2;
		}
		if (string.IsNullOrEmpty(text2))
		{
			return text;
		}
		return text + "\n" + text2;
	}
}
