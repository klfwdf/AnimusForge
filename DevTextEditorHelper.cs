using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public static class DevTextEditorHelper
{
	public static void ShowLongTextEditor(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSave, Action onCancel, string saveText = "保存", string cancelText = "返回")
	{
		if (DevHistoryEditPopup.Show(titleText, subtitleText, initialText, initialText, onSave, onCancel, inputHintText, saveText, cancelText))
		{
			return;
		}
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, BuildFallbackDescription(subtitleText, inputHintText), isAffirmativeOptionShown: true, isNegativeOptionShown: true, saveText, cancelText, onSave, onCancel, shouldInputBeObfuscated: false, null, "", initialText ?? ""));
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
