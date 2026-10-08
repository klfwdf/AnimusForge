using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class ShoutTextInputPopupVM : ViewModel
{
	private readonly Action<string> _onSubmit;

	private readonly Action _onCancel;

    private readonly Action _onTitleLink;
    private readonly bool _allowIllustration;
    private readonly Action _onIllustration;
    private int _illustrationVersion = -1;
    [DataSourceProperty] public bool HasScrollArtwork { get; }
    [DataSourceProperty] public bool UsePlainScrollBackground => !HasScrollArtwork;
    [DataSourceProperty] public bool IsInputEmpty => string.IsNullOrWhiteSpace(InputText);
    [DataSourceProperty] public bool HasSubtitleText => !string.IsNullOrWhiteSpace(SubtitleText);
    [DataSourceProperty] public bool IsIllustrationVisible => _allowIllustration && ShoutBehavior.IsSceneIllustrationAvailableForExternal;
    [DataSourceProperty] public bool CanIllustrate => IsIllustrationVisible && !ShoutBehavior.IsSceneIllustrationBusyForExternal;
    [DataSourceProperty] public string IllustrationButtonText => ShoutBehavior.IsSceneIllustrationBusyForExternal ? "截图中…" : "生图";
    [DataSourceProperty] public string IllustrationStatusText => ShoutBehavior.SceneIllustrationStatusForExternal;
    public void ExecuteIllustrate() { if (CanIllustrate) _onIllustration?.Invoke(); }
    public void ExecuteOpenGallery() { if (IsIllustrationVisible) ShoutBehavior.OpenSceneIllustrationGalleryForUi(); }
    public void RefreshIllustration()
    {
        int version = ShoutBehavior.SceneIllustrationVersionForExternal;
        if (_illustrationVersion == version) return;
        _illustrationVersion = version;
        OnPropertyChanged(nameof(IsIllustrationVisible)); OnPropertyChanged(nameof(CanIllustrate));
        OnPropertyChanged(nameof(IllustrationButtonText)); OnPropertyChanged(nameof(IllustrationStatusText));
    }

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
                OnPropertyChanged(nameof(HasSubtitleText));
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
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxShoutInputChars);
			if (text != _inputText)
			{
				_inputText = text;
				OnPropertyChangedWithValue(text, "InputText");
                OnPropertyChanged(nameof(IsInputEmpty));
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

    public ShoutTextInputPopupVM(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel, Action onTitleLink = null, bool allowIllustration = false, Action onIllustration = null)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
        _onTitleLink = onTitleLink;
        _allowIllustration = allowIllustration;
        _onIllustration = onIllustration;
        HasScrollArtwork = DialogueUI.DialogueUiSprites.EnsureShoutScrollLoaded();
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
			_onSubmit?.Invoke(AnimusForgeTextInputSanitizer.SanitizeMultiline(InputText, AnimusForgeTextInputSanitizer.MaxShoutInputChars));
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
