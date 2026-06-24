using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class CourierLetterReplyPopupVM : ViewModel
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
		get => _titleText;
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
		get => _subtitleText;
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
		get => _bodyText;
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
		get => _closeText;
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
		get => _bodyFontSize;
		set
		{
			if (value != _bodyFontSize)
			{
				_bodyFontSize = value;
				OnPropertyChangedWithValue(value, "BodyFontSize");
			}
		}
	}

	public CourierLetterReplyPopupVM(string titleText, string subtitleText, string bodyText, int bodyFontSize, Action onClose, string closeText)
	{
		_onClose = onClose;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "信使带回了回信" : titleText;
		SubtitleText = subtitleText ?? "";
		BodyText = string.IsNullOrWhiteSpace(bodyText) ? "（无回信正文）" : bodyText;
		BodyFontSize = Math.Max(14, Math.Min(34, bodyFontSize));
		CloseText = closeText ?? "";
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}
