using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public static class DevTextEditorHelper
{
	public static void ShowLongTextEditor(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSave, Action onCancel, string saveText = null, string cancelText = null)
	{
		saveText = saveText ?? new TextObject("{=AF_UI_Common_Save}Save").ToString();
		cancelText = cancelText ?? new TextObject("{=AF_UI_Common_Back}Back").ToString();
		string safeInitialText = AnimusForgeTextInputSanitizer.SanitizeMultiline(initialText, AnimusForgeTextInputSanitizer.MaxLongEditorChars);
		Action<string> safeOnSave = delegate(string text)
		{
			onSave?.Invoke(AnimusForgeTextInputSanitizer.SanitizeMultiline(text, AnimusForgeTextInputSanitizer.MaxLongEditorChars));
		};
		if (DevHistoryEditPopup.Show(titleText, subtitleText, safeInitialText, safeInitialText, safeOnSave, onCancel, inputHintText, saveText, cancelText))
		{
			return;
		}
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, BuildFallbackDescription(subtitleText, inputHintText), isAffirmativeOptionShown: true, isNegativeOptionShown: true, saveText, cancelText, safeOnSave, onCancel, shouldInputBeObfuscated: false, null, "", safeInitialText));
	}

    // Owned nested editors expose cancellation to their host. The ordinary inquiry fallback remains unchanged above.
    public static IDisposable ShowOwnedLongTextEditor(string titleText, string subtitleText, string inputHintText,
        string initialText, Action<string> onSave, Action onCancel, ScreenLayer inputOwner, Func<bool> isInputOwnerAlive,
        string saveText = null, string cancelText = null)
    {
        saveText = saveText ?? new TextObject("{=AF_UI_Common_Save}Save").ToString();
        cancelText = cancelText ?? new TextObject("{=AF_UI_Common_Back}Back").ToString();
        string initial = AnimusForgeTextInputSanitizer.SanitizeMultiline(initialText, AnimusForgeTextInputSanitizer.MaxLongEditorChars);
        Action<string> save = text => onSave?.Invoke(AnimusForgeTextInputSanitizer.SanitizeMultiline(text, AnimusForgeTextInputSanitizer.MaxLongEditorChars));
        if (DevHistoryEditPopup.TryShowOwned(titleText, subtitleText, initial, initial, save, onCancel,
            inputHintText, saveText, cancelText, inputOwner, isInputOwnerAlive, onCancel, out var session)) return session;
        throw new InvalidOperationException(new TextObject("{=AF_UI_Editor_OpenFailed}The text editor could not be opened. Close it and try again.").ToString());
    }

	private static string BuildFallbackDescription(string subtitleText, string inputHintText)
	{
		string text = (subtitleText ?? "").Trim();
		string text2 = (inputHintText ?? "").Trim();
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
