using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class AnimusForgeConversationHistoryLogItemVM : ViewModel
{
	private string _chatItemTime;

	private string _chatSpeaker;

	private string _chatText;

	private string _fontColor;

	private readonly Action<string> _onOpenEncyclopediaLink;

	[DataSourceProperty]
	public string ChatItemTime
	{
		get => _chatItemTime;
		set
		{
			if (value != _chatItemTime)
			{
				_chatItemTime = value;
				OnPropertyChangedWithValue(value, nameof(ChatItemTime));
			}
		}
	}

	[DataSourceProperty]
	public string ChatSpeaker
	{
		get => _chatSpeaker;
		set
		{
			if (value != _chatSpeaker)
			{
				_chatSpeaker = value;
				OnPropertyChangedWithValue(value, nameof(ChatSpeaker));
			}
		}
	}

	[DataSourceProperty]
	public string ChatText
	{
		get => _chatText;
		set
		{
			if (value != _chatText)
			{
				_chatText = value;
				OnPropertyChangedWithValue(value, nameof(ChatText));
			}
		}
	}

	[DataSourceProperty]
	public string FontColor
	{
		get => _fontColor;
		set
		{
			if (value != _fontColor)
			{
				_fontColor = value;
				OnPropertyChangedWithValue(value, nameof(FontColor));
			}
		}
	}

	// This constructor receives an internal UI snapshot and is intentionally not part of the public VM surface.
	internal AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string text, string kind, EncyclopediaEntityLinkFormatter.DisplaySession linkDisplaySession, Hero conversationTargetHero, CharacterObject conversationTargetCharacter, Action<string> onOpenEncyclopediaLink)
	{
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		ChatItemTime = time ?? "";
		ChatSpeaker = string.IsNullOrWhiteSpace(speaker) ? "\u8bb0\u5f55" : speaker.Trim();
		// Build only a disposable RichText copy; the persisted dialogue entry stays plain for memory and LLM reuse.
		string rawDisplayText = string.IsNullOrWhiteSpace(ChatSpeaker) ? (text ?? "") : "(" + ChatSpeaker + ")" + (text ?? "");
		ChatText = linkDisplaySession?.Format(rawDisplayText, conversationTargetHero, conversationTargetCharacter) ?? EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawDisplayText);
		FontColor = ResolveFontColor(kind);
	}

	// Paged history has already prepared trusted RichText, so revisiting a page must not repeat entity matching or string replacement.
	internal AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string formattedText, string fontColor, Action<string> onOpenEncyclopediaLink)
	{
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		ChatItemTime = time ?? "";
		ChatSpeaker = string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker.Trim();
		ChatText = formattedText ?? "";
		FontColor = string.IsNullOrWhiteSpace(fontColor) ? "#D6D6D6FF" : fontColor;
	}

	public void ExecuteOpenEncyclopediaLink(string link)
	{
		_onOpenEncyclopediaLink?.Invoke(link);
	}

	// Optional two-click delete, enabled only by a host that supplies a delete handler (DialogueUI panel).
	// First click arms the row ("确认删除"), second click deletes; arming another row disarms this one.
	private Action<AnimusForgeConversationHistoryLogItemVM> _onDeleteArmed;

	private Action _onDeleteConfirmed;

	private bool _isDeleteArmed;

	[DataSourceProperty]
	public bool CanDelete => _onDeleteConfirmed != null;

	[DataSourceProperty]
	public bool IsDeleteArmed => _isDeleteArmed;

	[DataSourceProperty]
	public string DeleteText => _isDeleteArmed ? "确认删除" : "删除";

	internal void EnableDelete(Action<AnimusForgeConversationHistoryLogItemVM> onArmed, Action onConfirmed)
	{
		_onDeleteArmed = onArmed;
		_onDeleteConfirmed = onConfirmed;
		OnPropertyChanged(nameof(CanDelete));
		OnPropertyChanged(nameof(IsDeleteVisible));
	}

	internal void DisarmDelete()
	{
		if (!_isDeleteArmed)
		{
			return;
		}
		_isDeleteArmed = false;
		OnPropertyChanged(nameof(IsDeleteArmed));
		OnPropertyChanged(nameof(DeleteText));
	}

	public void ExecuteDelete()
	{
		if (_onDeleteConfirmed == null)
		{
			return;
		}
		if (!_isDeleteArmed)
		{
			_isDeleteArmed = true;
			OnPropertyChanged(nameof(IsDeleteArmed));
			OnPropertyChanged(nameof(DeleteText));
			_onDeleteArmed?.Invoke(this);
			return;
		}
		DisarmDelete();
		_onDeleteConfirmed();
	}

	// Row actions are hidden until the host's panel toggles them on; edit and delete share one slot, so at most one shows.
	private bool _showEditAction;

	private bool _showDeleteAction;

	[DataSourceProperty]
	public bool IsDeleteVisible => CanDelete && _showDeleteAction;

	// Optional inline edit, enabled only by a host that supplies an edit handler (DialogueUI panel).
	// First click opens the editor ("保存"), second click or Enter saves; another row opening cancels this one.
	private Action<AnimusForgeConversationHistoryLogItemVM> _onEditStarted;

	private Func<string, bool> _onEditSaved;

	private Action _onStartTyping;

	private Action _onStopTyping;

	private string _editSourceText = "";

	private string _editText = "";

	private bool _isEditing;

	[DataSourceProperty]
	public bool CanEdit => _onEditSaved != null;

	[DataSourceProperty]
	public bool IsEditVisible => CanEdit && _showEditAction;

	[DataSourceProperty]
	public bool IsEditing => _isEditing;

	[DataSourceProperty]
	public bool IsNotEditing => !_isEditing;

	// Bumped on each open so the reused editor widget refocuses itself.
	[DataSourceProperty]
	public int EditFocusRequestId { get; private set; }

	[DataSourceProperty]
	public string EditButtonText => _isEditing ? "保存" : "编辑";

	[DataSourceProperty]
	public string EditText
	{
		get => _editText;
		set
		{
			value = value ?? "";
			if (value != _editText)
			{
				_editText = value;
				OnPropertyChangedWithValue(value, nameof(EditText));
			}
		}
	}

	internal void EnableEdit(string sourceText, Action<AnimusForgeConversationHistoryLogItemVM> onStarted, Func<string, bool> onSaved, Action onStartTyping, Action onStopTyping)
	{
		_editSourceText = sourceText ?? "";
		_onEditStarted = onStarted;
		_onEditSaved = onSaved;
		_onStartTyping = onStartTyping;
		_onStopTyping = onStopTyping;
		OnPropertyChanged(nameof(CanEdit));
		OnPropertyChanged(nameof(IsEditVisible));
	}

	internal void SetActionVisibility(bool showEdit, bool showDelete)
	{
		if (!showEdit)
		{
			CancelEdit();
		}
		if (!showDelete)
		{
			DisarmDelete();
		}
		if (showEdit == _showEditAction && showDelete == _showDeleteAction)
		{
			return;
		}
		_showEditAction = showEdit;
		_showDeleteAction = showDelete;
		OnPropertyChanged(nameof(IsEditVisible));
		OnPropertyChanged(nameof(IsDeleteVisible));
	}

	// Called by the owner after a successful save so the row shows the stored text without rebuilding the page.
	internal void ApplyEdit(string speaker, string formattedText, string fontColor, string sourceText)
	{
		ChatSpeaker = string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker.Trim();
		ChatText = formattedText ?? "";
		FontColor = string.IsNullOrWhiteSpace(fontColor) ? "#D6D6D6FF" : fontColor;
		_editSourceText = sourceText ?? "";
	}

	internal void CancelEdit()
	{
		if (!_isEditing)
		{
			return;
		}
		SetEditing(false);
		_onStopTyping?.Invoke();
	}

	public void ExecuteEdit()
	{
		if (!IsEditVisible)
		{
			return;
		}
		if (_isEditing)
		{
			ExecuteSaveEdit();
			return;
		}
		EditText = _editSourceText;
		SetEditing(true);
		EditFocusRequestId++;
		OnPropertyChanged(nameof(EditFocusRequestId));
		_onEditStarted?.Invoke(this);
	}

	public void ExecuteSaveEdit()
	{
		if (!_isEditing || _onEditSaved == null)
		{
			return;
		}
		// An unchanged text simply closes the editor instead of touching the store.
		if (string.Equals((_editText ?? "").Trim(), _editSourceText.Trim(), StringComparison.Ordinal) || _onEditSaved(_editText))
		{
			CancelEdit();
		}
	}

	public void ExecuteCancelEdit()
	{
		CancelEdit();
	}

	public void StartTyping()
	{
		_onStartTyping?.Invoke();
	}

	public void StopTyping()
	{
		_onStopTyping?.Invoke();
	}

	private void SetEditing(bool value)
	{
		_isEditing = value;
		OnPropertyChanged(nameof(IsEditing));
		OnPropertyChanged(nameof(IsNotEditing));
		OnPropertyChanged(nameof(EditButtonText));
	}

	// Shared by the page-level cache so row colors remain identical whether the record is first formatted or restored from cache.
	internal static string ResolveFontColor(string kind)
	{
		switch ((kind ?? "").Trim())
		{
			case "player":
				return "#E2AF54FF";
			case "afef_player":
			case "afef_npc":
				return "#7DDCFFFF";
			case "scene":
				return "#A5FF9AFF";
			case "npc":
				return "#FFFFFFFF";
			default:
				return "#D6D6D6FF";
		}
	}
}
