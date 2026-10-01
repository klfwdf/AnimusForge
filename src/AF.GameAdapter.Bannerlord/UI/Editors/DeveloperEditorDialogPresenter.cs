using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.Core;using TaleWorlds.Library;
namespace AnimusForge;
internal static class DeveloperEditorDialogPresenter
{
	internal static void ShowDevLargeTextOrInquiry(Func<string,int,string> preview, string title, string subtitle, string body, Action onClose, string closeText = "返回")
	{
		string safeBody = string.IsNullOrWhiteSpace(body) ? "（无数据）" : body.Trim();
		if (DevLargeSelectionPopup.ShowText(title, subtitle, safeBody, onClose, closeText))
		{
			return;
		}
		InformationManager.ShowInquiry(new InquiryData(title ?? "AnimusForge", BuildDevLargeFallbackDescription(subtitle, safeBody), isAffirmativeOptionShown: true, isNegativeOptionShown: false, closeText ?? "返回", "", onClose, null), pauseGameActiveState: true);
	}

	internal static void ShowDevLargeSelectionOrInquiry(Func<string,int,string> preview, string title, string subtitle, string body, List<DevLargeSelectionPopup.Option> options, Action<string> onSelect, Action onCancel, string affirmativeText = "进入", string cancelText = "返回")
	{
		options = options ?? new List<DevLargeSelectionPopup.Option>();
		string safeBody = string.IsNullOrWhiteSpace(body) ? "请选择要执行的操作。" : body.Trim();
		if (DevLargeSelectionPopup.Show(title, subtitle, safeBody, options, onSelect, onCancel, cancelText))
		{
			return;
		}
		if (options.Count == 0)
		{
			ShowDevLargeTextOrInquiry(preview, title, subtitle, safeBody, onCancel, cancelText);
			return;
		}
		List<InquiryElement> list = options.Select((DevLargeSelectionPopup.Option x) => new InquiryElement(x.Id, BuildDevLargeFallbackOptionText(preview, x), null)).ToList();
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title ?? "AnimusForge", BuildDevLargeFallbackDescription(subtitle, safeBody), list, isExitShown: true, 0, 1, affirmativeText ?? "进入", cancelText ?? "返回", delegate(List<InquiryElement> selected)
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
			new DevLargeSelectionPopup.Option("__confirm__", confirmText ?? "确认", isDanger: true, isPrimary: true)
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
		}, onCancel, confirmText ?? "确认", cancelText ?? "取消");
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
