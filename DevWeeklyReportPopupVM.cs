using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class DevWeeklyReportPopupVM : ViewModel
{
	private readonly Action _onClose;

	private string _titleText;

	private string _subtitleText;

	private string _bodyText;

	private string _closeText;

	private int _bodyFontSize;

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get
		{
			return _subtitleText;
		}
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, "SubtitleText");
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get
		{
			return _bodyText;
		}
		set
		{
			if (value != _bodyText)
			{
				_bodyText = value;
				OnPropertyChangedWithValue(value, "BodyText");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public int BodyFontSize
	{
		get
		{
			return _bodyFontSize;
		}
		set
		{
			if (value != _bodyFontSize)
			{
				_bodyFontSize = value;
				OnPropertyChangedWithValue(value, "BodyFontSize");
			}
		}
	}

	public DevWeeklyReportPopupVM(string titleText, string subtitleText, string bodyText, int bodyFontSize, Action onClose, string closeText)
	{
		_onClose = onClose;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "\u5468\u62a5\u9884\u89c8" : titleText;
		SubtitleText = subtitleText ?? "";
		BodyText = string.IsNullOrWhiteSpace(bodyText) ? "\u5f53\u524d\u5468\u62a5\u6b63\u6587\u4e3a\u7a7a\u3002" : bodyText;
		BodyFontSize = Math.Max(12, Math.Min(36, bodyFontSize));
		CloseText = string.IsNullOrWhiteSpace(closeText) ? "\u5173\u95ed" : closeText;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}
