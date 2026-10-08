using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public static class DevTextEditorHelper
{
	public static void ShowLongTextEditor(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSave, Action onCancel, string saveText = "保存", string cancelText = "返回")
	{
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
        string saveText = "保存", string cancelText = "返回")
    {
        string initial = AnimusForgeTextInputSanitizer.SanitizeMultiline(initialText, AnimusForgeTextInputSanitizer.MaxLongEditorChars);
        Action<string> save = text => onSave?.Invoke(AnimusForgeTextInputSanitizer.SanitizeMultiline(text, AnimusForgeTextInputSanitizer.MaxLongEditorChars));
        if (DevHistoryEditPopup.TryShowOwned(titleText, subtitleText, initial, initial, save, onCancel,
            inputHintText, saveText, cancelText, inputOwner, isInputOwnerAlive, onCancel, out var session)) return session;
        throw new InvalidOperationException("文本编辑器未能打开，请关闭后重试。");
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
