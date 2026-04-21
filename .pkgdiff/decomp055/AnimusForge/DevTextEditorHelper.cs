using System;
using TaleWorlds.Library;

namespace AnimusForge;

public static class DevTextEditorHelper
{
	public static void ShowLongTextEditor(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSave, Action onCancel, string saveText = "保存", string cancelText = "返回")
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		if (!DevHistoryEditPopup.Show(titleText, subtitleText, initialText, initialText, onSave, onCancel, inputHintText, saveText, cancelText))
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, BuildFallbackDescription(subtitleText, inputHintText), true, true, saveText, cancelText, onSave, onCancel, false, (Func<string, Tuple<bool, string>>)null, "", initialText ?? ""), false, false);
		}
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
