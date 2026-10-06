using System;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Popup;

public class MPLobbyQueryPopupVM : ViewModel
{
	private TextObject _titleObj;

	private TextObject _messageObj;

	private Action _onAccepted;

	private Action _onDeclined;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private bool _isEnabled;

	private bool _isInquiry;

	private string _title;

	private string _message;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInquiry
	{
		get
		{
			return _isInquiry;
		}
		set
		{
			if (value != _isInquiry)
			{
				_isInquiry = value;
				OnPropertyChangedWithValue(value, "IsInquiry");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			if (value != _message)
			{
				_message = value;
				OnPropertyChangedWithValue(value, "Message");
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _titleObj?.ToString() ?? "";
		Message = _messageObj?.ToString() ?? "";
	}

	public void ShowMessage(TextObject title, TextObject message)
	{
		IsEnabled = true;
		_titleObj = title;
		_messageObj = message;
		RefreshValues();
	}

	public void ShowInquiry(TextObject title, TextObject message, Action onAccepted, Action onDeclined)
	{
		IsEnabled = true;
		IsInquiry = true;
		_titleObj = title;
		_messageObj = message;
		_onAccepted = onAccepted;
		_onDeclined = onDeclined;
		RefreshValues();
	}

	public void ExecuteAccept()
	{
		IsEnabled = false;
		IsInquiry = false;
		_onAccepted?.Invoke();
	}

	public void ExecuteDecline()
	{
		IsEnabled = false;
		IsInquiry = false;
		_onDeclined?.Invoke();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
