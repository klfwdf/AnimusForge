using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class WorldDiplomacyComposePopupVM : ViewModel
{
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private string _titleText;
	private string _subtitleText;
	private string _hintText;
	private string _bodyText;
	private string _cancelText;
	private string _publishText;
	private bool _canPublish;

	public WorldDiplomacyComposePopupVM(string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		TitleText = string.IsNullOrWhiteSpace(title) ? DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_Title", "Write diplomatic declaration") : title;
		SubtitleText = subtitle ?? "";
		HintText = string.IsNullOrWhiteSpace(hint) ? DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_Hint", "Write a title or outline first, then ask the scribe to draft it.") : hint;
		CancelText = DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_Cancel", "Cancel");
		PublishText = DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_Publish", "Publish diplomatic document");
		BodyText = "";
	}

	[DataSourceProperty]
	public string CancelText
	{
		get => _cancelText;
		private set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, nameof(CancelText));
			}
		}
	}

	[DataSourceProperty]
	public string PublishText
	{
		get => _publishText;
		private set
		{
			if (value != _publishText)
			{
				_publishText = value;
				OnPropertyChangedWithValue(value, nameof(PublishText));
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
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
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	[DataSourceProperty]
	public string HintText
	{
		get => _hintText;
		set
		{
			if (value != _hintText)
			{
				_hintText = value;
				OnPropertyChangedWithValue(value, nameof(HintText));
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get => _bodyText;
		set
		{
			string clean = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, 6000);
			if (clean != _bodyText)
			{
				_bodyText = clean;
				_bodyRevision++;
				OnPropertyChangedWithValue(clean, nameof(BodyText));
				RefreshDraftAvailability();
			}
		}
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		private set
		{
			if (value != _canPublish)
			{
				_canPublish = value;
				OnPropertyChangedWithValue(value, nameof(CanPublish));
			}
		}
	}

	public void ExecutePublish()
	{
		if (IsCurrentDraftWindow && CanPublish)
		{
			_onSubmit?.Invoke(BodyText);
		}
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}

}
