using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.Core;using TaleWorlds.Library;
namespace AnimusForge;
internal static class DeveloperEditorDialogPresenter
{
	internal static void ShowDevLargeTextOrInquiry(Func<string,int,string> preview, string title, string subtitle, string body, Action onClose, string closeText = null)
	{
		string safeBody = string.IsNullOrWhiteSpace(body) ? DeveloperUiText.T("AF_UI_Developer_NoData", "(No data)") : body.Trim();
		string safeCloseText = string.IsNullOrWhiteSpace(closeText) ? DeveloperUiText.T("AF_UI_Common_Back", "Back") : closeText;
		if (DevLargeSelectionPopup.ShowText(title, subtitle, safeBody, onClose, safeCloseText))
		{
			return;
		}
		InformationManager.ShowInquiry(new InquiryData(title ?? "AnimusForge", BuildDevLargeFallbackDescription(subtitle, safeBody), isAffirmativeOptionShown: true, isNegativeOptionShown: false, safeCloseText, "", onClose, null), pauseGameActiveState: true);
	}

	internal static void ShowDevLargeSelectionOrInquiry(Func<string,int,string> preview, string title, string subtitle, string body, List<DevLargeSelectionPopup.Option> options, Action<string> onSelect, Action onCancel, string affirmativeText = null, string cancelText = null)
	{
		options = options ?? new List<DevLargeSelectionPopup.Option>();
		string safeBody = string.IsNullOrWhiteSpace(body) ? DeveloperUiText.T("AF_UI_Developer_ChooseOperation", "Choose an operation.") : body.Trim();
		string safeAffirmativeText = string.IsNullOrWhiteSpace(affirmativeText) ? DeveloperUiText.T("AF_UI_Developer_Enter", "Enter") : affirmativeText;
		string safeCancelText = string.IsNullOrWhiteSpace(cancelText) ? DeveloperUiText.T("AF_UI_Common_Back", "Back") : cancelText;
		if (DevLargeSelectionPopup.Show(title, subtitle, safeBody, options, onSelect, onCancel, safeCancelText))
		{
			return;
		}
		if (options.Count == 0)
		{
			ShowDevLargeTextOrInquiry(preview, title, subtitle, safeBody, onCancel, safeCancelText);
			return;
		}
		List<InquiryElement> list = options.Select((DevLargeSelectionPopup.Option x) => new InquiryElement(x.Id, BuildDevLargeFallbackOptionText(preview, x), null)).ToList();
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title ?? "AnimusForge", BuildDevLargeFallbackDescription(subtitle, safeBody), list, isExitShown: true, 0, 1, safeAffirmativeText, safeCancelText, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onCancel?.Invoke();
				return;
			}
			onSelect?.Invoke(selected[0].Identifier as string ?? "");
		}, delegate
		{
			onCancel?.Invoke();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal static void ShowDevLargeConfirmOrInquiry(Func<string,int,string> preview, string title, string subtitle, string body, string confirmText, string cancelText, Action onConfirm, Action onCancel)
	{
  bool answered = false;
  Action Guard(Action callback) => () => { if (answered) return; answered = true; callback?.Invoke(); };
  onConfirm = Guard(onConfirm); onCancel = Guard(onCancel);

		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("__confirm__", string.IsNullOrWhiteSpace(confirmText) ? DeveloperUiText.T("AF_UI_Developer_Confirm", "Confirm") : confirmText, isDanger: true, isPrimary: true)
		};
		ShowDevLargeSelectionOrInquiry(preview, title, subtitle, body, options, delegate(string id)
		{
			if (string.Equals(id, "__confirm__", StringComparison.Ordinal))
			{
				onConfirm?.Invoke();
			}
			else
			{
				onCancel?.Invoke();
			}
		}, onCancel, string.IsNullOrWhiteSpace(confirmText) ? DeveloperUiText.T("AF_UI_Developer_Confirm", "Confirm") : confirmText, string.IsNullOrWhiteSpace(cancelText) ? DeveloperUiText.T("AF_UI_Common_Cancel", "Cancel") : cancelText);
	}

	internal static string BuildDevLargeFallbackDescription(string subtitle, string body)
	{
		string text = (subtitle ?? "").Trim();
		string text2 = (body ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return text2;
		}
		if (string.IsNullOrEmpty(text2))
		{
			return text;
		}
		return text + "\n\n" + text2;
	}

	internal static string BuildDevLargeFallbackOptionText(Func<string,int,string> preview, DevLargeSelectionPopup.Option option)
	{
		if (option == null)
		{
			return "";
		}
		string title = (option.TitleText ?? "").Trim();
		string detail = preview(option.DetailText, 90);
		if (string.IsNullOrWhiteSpace(option.DetailText))
		{
			return title;
		}
		return title + " - " + detail;
	}

}
