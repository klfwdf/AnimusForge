using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class CourierLetterReplyPopupVM : ViewModel
{
	private readonly Action _onClose;
	private readonly Action _onReply;
	private string _titleText;
	private string _subtitleText;
	private string _bodyText;
	private string _closeText;
	private string _replyText;
	private string _impactText;
	private int _bodyFontSize;
	private bool _canReply;
	private bool _hasImpact;

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
	public string ReplyText
	{
		get => _replyText;
		set
		{
			if (value != _replyText)
			{
				_replyText = value;
				OnPropertyChangedWithValue(value, "ReplyText");
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

	[DataSourceProperty]
	public bool CanReply
	{
		get => _canReply;
		set
		{
			if (value != _canReply)
			{
				_canReply = value;
				OnPropertyChangedWithValue(value, "CanReply");
			}
		}
	}

	[DataSourceProperty]
	public string ImpactText
	{
		get => _impactText;
		set
		{
			if (value != _impactText)
			{
				_impactText = value;
				OnPropertyChangedWithValue(value, "ImpactText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasImpact
	{
		get => _hasImpact;
		set
		{
			if (value != _hasImpact)
			{
				_hasImpact = value;
				OnPropertyChangedWithValue(value, "HasImpact");
			}
		}
	}

	public CourierLetterReplyPopupVM(string titleText, string subtitleText, string bodyText, int bodyFontSize, Action onClose, string closeText, Action onReply, string replyText, string impactText = null)
	{
		_onClose = onClose;
		_onReply = onReply;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "信使带回了回信" : titleText;
		SubtitleText = subtitleText ?? "";
		BodyText = string.IsNullOrWhiteSpace(bodyText) ? "（无回信正文）" : bodyText;
		BodyFontSize = Math.Max(14, Math.Min(34, bodyFontSize));
		CloseText = string.IsNullOrWhiteSpace(closeText) ? "关闭" : closeText;
		ReplyText = string.IsNullOrWhiteSpace(replyText) ? "回信" : replyText;
		CanReply = onReply != null;
		ImpactText = (impactText ?? "").Trim();
		HasImpact = !string.IsNullOrWhiteSpace(ImpactText);
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	public void ExecuteReply()
	{
		if (CanReply)
		{
			_onReply?.Invoke();
		}
	}
}
