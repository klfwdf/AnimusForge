using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class AnimusForgeNativeConversationOverlayVM : ViewModel
{
    private readonly Func<bool> _illustrationAvailable;
    private readonly Action _onIllustration;
    [DataSourceProperty] public bool IsIllustrationAvailable => _illustrationAvailable?.Invoke() == true;
    [DataSourceProperty] public string IllustrationText => ConversationUiLocalization.Text("AF_CONV_UI_Native_Illustration", "Scene illustration");
    public void ShowIllustration() { if (IsIllustrationAvailable) _onIllustration?.Invoke(); }
	private readonly Action<string> _onSubmit;

	private readonly Action _onSwitchTalk;

	private readonly Action _onShowHistory;

	private readonly Action _onGiveShow;

	private readonly Action _onEditPersona;

	private readonly Action _onTagTest;
	private readonly Action _onDirectActions;
	[DataSourceProperty] public bool CanOpenDirectActions { get => _canOpenDirectActions; set { if (_canOpenDirectActions != value) { _canOpenDirectActions = value; OnPropertyChangedWithValue(value, nameof(CanOpenDirectActions)); } } }
	private bool _canOpenDirectActions = true;
    [DataSourceProperty] public string DirectActionsButtonText => ConversationUiLocalization.Text("AF_CONV_UI_Direct_Button", "Trigger tags manually");
	public void OpenDirectActions() { if (CanOpenDirectActions) _onDirectActions?.Invoke(); }

	private string _inputText;

	private string _switchTitle;

	private string _aiChatHistoryButtonText;

	private string _giveShowButtonText;

	private string _personaEditButtonText;

	private string _tagTestButtonText;

	private bool _isCustomAnswerVisible;

	private bool _isInputEnabled;

	private bool _canSwitchTalk;

	private bool _isPersonaEditVisible;

	private bool _isTagTestVisible;

	private float _aiChatboxOffset;

	private int _inputFocusVersion;

	[DataSourceProperty]
	public string InputText
	{
		get => _inputText;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
			if (text != _inputText)
			{
				_inputText = text;
				OnPropertyChangedWithValue(text, nameof(InputText));
			}
		}
	}

	[DataSourceProperty]
	public string SwitchTitle
	{
		get => _switchTitle;
		private set
		{
			if (value != _switchTitle)
			{
				_switchTitle = value;
				OnPropertyChangedWithValue(value, nameof(SwitchTitle));
			}
		}
	}

	[DataSourceProperty]
	public string AIChatHistoryButtonText
	{
		get => _aiChatHistoryButtonText;
		private set
		{
			if (value != _aiChatHistoryButtonText)
			{
				_aiChatHistoryButtonText = value;
				OnPropertyChangedWithValue(value, nameof(AIChatHistoryButtonText));
			}
		}
	}

	[DataSourceProperty]
	public string GiveShowButtonText
	{
		get => _giveShowButtonText;
		private set
		{
			if (value != _giveShowButtonText)
			{
				_giveShowButtonText = value;
				OnPropertyChangedWithValue(value, nameof(GiveShowButtonText));
			}
		}
	}

	[DataSourceProperty]
	public string PersonaEditButtonText
	{
		get => _personaEditButtonText;
		private set
		{
			if (value != _personaEditButtonText)
			{
				_personaEditButtonText = value;
				OnPropertyChangedWithValue(value, nameof(PersonaEditButtonText));
			}
		}
	}

	[DataSourceProperty]
	public string TagTestButtonText
	{
		get => _tagTestButtonText;
		private set
		{
			if (value != _tagTestButtonText)
			{
				_tagTestButtonText = value;
				OnPropertyChangedWithValue(value, nameof(TagTestButtonText));
			}
		}
	}

	[DataSourceProperty]
	public bool IsCustomAnswerVisible
	{
		get => _isCustomAnswerVisible;
		set
		{
			if (value != _isCustomAnswerVisible)
			{
				_isCustomAnswerVisible = value;
				OnPropertyChangedWithValue(value, nameof(IsCustomAnswerVisible));
				RefreshSwitchTitle();
			}
		}
	}

	[DataSourceProperty]
	public bool IsInputEnabled
	{
		get => _isInputEnabled;
		set
		{
			if (value != _isInputEnabled)
			{
				_isInputEnabled = value;
				OnPropertyChangedWithValue(value, nameof(IsInputEnabled));
			}
		}
	}

	[DataSourceProperty]
	public bool CanSwitchTalk
	{
		get => _canSwitchTalk;
		set
		{
			if (value != _canSwitchTalk)
			{
				_canSwitchTalk = value;
				OnPropertyChangedWithValue(value, nameof(CanSwitchTalk));
			}
		}
	}

	[DataSourceProperty]
	public bool IsPersonaEditVisible
	{
		get => _isPersonaEditVisible;
		private set
		{
			if (value != _isPersonaEditVisible)
			{
				_isPersonaEditVisible = value;
				OnPropertyChangedWithValue(value, nameof(IsPersonaEditVisible));
			}
		}
	}

	[DataSourceProperty]
	public bool IsTagTestVisible
	{
		get => _isTagTestVisible;
		private set
		{
			if (value != _isTagTestVisible)
			{
				_isTagTestVisible = value;
				OnPropertyChangedWithValue(value, nameof(IsTagTestVisible));
			}
		}
	}

	[DataSourceProperty]
	public float AIChatboxOffset
	{
		get => _aiChatboxOffset;
		set
		{
			if (Math.Abs(value - _aiChatboxOffset) > 0.001f)
			{
				_aiChatboxOffset = value;
				OnPropertyChangedWithValue(value, nameof(AIChatboxOffset));
			}
		}
	}

	[DataSourceProperty]
	public int InputFocusVersion
	{
		get => _inputFocusVersion;
		private set
		{
			if (value != _inputFocusVersion)
			{
				_inputFocusVersion = value;
				OnPropertyChangedWithValue(value, nameof(InputFocusVersion));
			}
		}
	}

	public AnimusForgeNativeConversationOverlayVM(Action<string> onSubmit, Action onSwitchTalk, Action onShowHistory, Action onGiveShow, Action onEditPersona, Action onTagTest,
        Func<bool> illustrationAvailable = null, Action onIllustration = null, Action onDirectActions = null)
	{
        _onDirectActions = onDirectActions;
        _illustrationAvailable = illustrationAvailable;
        _onIllustration = onIllustration;
		_onSubmit = onSubmit;
		_onSwitchTalk = onSwitchTalk;
		_onShowHistory = onShowHistory;
		_onGiveShow = onGiveShow;
		_onEditPersona = onEditPersona;
		_onTagTest = onTagTest;
		PersonaEditButtonText = new TextObject("{=AF_UI_Conversation_EditNpc}Edit NPC").ToString();
		TagTestButtonText = new TextObject("{=AF_UI_Conversation_TagInput}Tag input").ToString();
		IsPersonaEditVisible = false;
		IsTagTestVisible = false;
		InputText = "";
		AIChatHistoryButtonText = new TextObject("{=AF_UI_Conversation_History}Conversation history").ToString();
		GiveShowButtonText = new TextObject("{=AF_UI_Conversation_GiveShow}Give / show").ToString();
		IsInputEnabled = true;
		CanSwitchTalk = true;
		AIChatboxOffset = 0f;
		InputFocusVersion = 0;
		RefreshSwitchTitle();
	}

	public void SetInputVisible(bool isVisible)
	{
		IsCustomAnswerVisible = isVisible;
		if (!isVisible)
		{
			InputText = "";
		}
	}

	public void SetBusy(bool isBusy)
	{
		IsInputEnabled = !isBusy;
		CanSwitchTalk = !isBusy;
	}

	public void SetPersonaEditVisible(bool isVisible)
	{
		IsPersonaEditVisible = isVisible;
	}

	public void SetTagTestVisible(bool isVisible)
	{
		IsTagTestVisible = isVisible;
	}

	public void RequestInputFocus()
	{
		InputFocusVersion++;
	}

	public void ExecuteSubmit()
	{
		if (!IsInputEnabled)
		{
			return;
		}
		_onSubmit?.Invoke(AnimusForgeTextInputSanitizer.SanitizeSingleLine(InputText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars));
	}

	public void SwitchTalk()
	{
		if (!CanSwitchTalk) return;
		_onSwitchTalk?.Invoke();
	}

	public void ShowLogView()
	{
		_onShowHistory?.Invoke();
	}

	public void ShowGiveShowMenu()
	{
		_onGiveShow?.Invoke();
	}

	public void EditPersona()
	{
		_onEditPersona?.Invoke();
	}

	public void OpenTagTest()
	{
		_onTagTest?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}

	private void RefreshSwitchTitle()
	{
		SwitchTitle = IsCustomAnswerVisible ? new TextObject("{=AF_UI_Conversation_NormalMode}Normal mode").ToString() : new TextObject("{=AF_UI_Conversation_AiMode}AI mode").ToString();
	}
}
