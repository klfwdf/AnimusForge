using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class ShoutTextInputPopupVM : ViewModel
{
	private readonly Action<string> _onSubmit;

	private readonly Action _onCancel;

	private readonly Action _onTitleLink;

	private string _titleText;

	private string _titleLinkText;

	private string _titlePlainText;

	private string _subtitleText;

	private string _inputHintText;

	private string _inputText;

	private Color _inputBackgroundColor;

	private bool _isTitleLinkEnabled;

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
				RefreshTitleDisplayText();
			}
		}
	}

	[DataSourceProperty]
	public string TitleLinkText
	{
		get => _titleLinkText;
		private set
		{
			if (value != _titleLinkText)
			{
				_titleLinkText = value;
				OnPropertyChangedWithValue(value, "TitleLinkText");
			}
		}
	}

	[DataSourceProperty]
	public string TitlePlainText
	{
		get => _titlePlainText;
		private set
		{
			if (value != _titlePlainText)
			{
				_titlePlainText = value;
				OnPropertyChangedWithValue(value, "TitlePlainText");
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
	public string InputHintText
	{
		get => _inputHintText;
		set
		{
			if (value != _inputHintText)
			{
				_inputHintText = value;
				OnPropertyChangedWithValue(value, "InputHintText");
			}
		}
	}

	[DataSourceProperty]
	public string InputText
	{
		get => _inputText;
		set
		{
			if (value != _inputText)
			{
				_inputText = value;
				OnPropertyChangedWithValue(value, "InputText");
			}
		}
	}

	[DataSourceProperty]
	public Color InputBackgroundColor
	{
		get => _inputBackgroundColor;
		set
		{
			if (!_inputBackgroundColor.Equals(value))
			{
				_inputBackgroundColor = value;
				OnPropertyChangedWithValue(value, "InputBackgroundColor");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTitleLinkEnabled
	{
		get => _isTitleLinkEnabled;
		set
		{
			if (value != _isTitleLinkEnabled)
			{
				_isTitleLinkEnabled = value;
				OnPropertyChangedWithValue(value, "IsTitleLinkEnabled");
				OnPropertyChangedWithValue(IsTitlePlainTextVisible, "IsTitlePlainTextVisible");
				RefreshTitleDisplayText();
			}
		}
	}

	[DataSourceProperty]
	public bool IsTitlePlainTextVisible => !IsTitleLinkEnabled;

	public ShoutTextInputPopupVM(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel, Action onTitleLink = null)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		_onTitleLink = onTitleLink;
		TitleText = titleText ?? "";
		SubtitleText = subtitleText ?? "";
		InputHintText = inputHintText ?? "";
		InputText = initialText ?? "";
		InputBackgroundColor = ResolveInputBackgroundColor();
		IsTitleLinkEnabled = _onTitleLink != null;
		RefreshTitleDisplayText();
	}

	private void RefreshTitleDisplayText()
	{
		string titleText = TitleText ?? "";
		TitleLinkText = IsTitleLinkEnabled ? titleText : "";
		TitlePlainText = IsTitleLinkEnabled ? "" : titleText;
	}

	private static Color ResolveInputBackgroundColor()
	{
		string selected = DuelSettings.ShoutInputUiBackgroundBlack;
		try
		{
			selected = DuelSettings.NormalizeShoutInputUiBackground(DuelSettings.GetSettings()?.GetShoutInputUiBackgroundSelection());
		}
		catch
		{
			selected = DuelSettings.ShoutInputUiBackgroundBlack;
		}
		if (string.Equals(selected, DuelSettings.ShoutInputUiBackgroundWhite, StringComparison.OrdinalIgnoreCase))
		{
			return Color.FromUint(4294967295u);
		}
		if (string.Equals(selected, DuelSettings.ShoutInputUiBackgroundPink, StringComparison.OrdinalIgnoreCase))
		{
			return Color.FromUint(4294944954u);
		}
		return Color.FromUint(4278190080u);
	}

	public void ExecuteSubmit()
	{
		if (string.IsNullOrWhiteSpace(InputText))
		{
			_onCancel?.Invoke();
		}
		else
		{
			_onSubmit?.Invoke(InputText ?? "");
		}
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void ExecuteOpenTitleLink()
	{
		_onTitleLink?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}
}
