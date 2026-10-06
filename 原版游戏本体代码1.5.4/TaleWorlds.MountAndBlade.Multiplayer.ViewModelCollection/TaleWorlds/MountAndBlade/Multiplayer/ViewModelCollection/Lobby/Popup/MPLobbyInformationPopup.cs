using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Popup;

public class MPLobbyInformationPopup : ViewModel
{
	private TextObject _titleTextObj;

	private TextObject _messageTextObj;

	private InputKeyItemVM _doneInputKey;

	private bool _isEnabled;

	private string _title;

	private string _message;

	private string _closeText;

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

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_titleTextObj != null)
		{
			Title = _titleTextObj.ToString();
		}
		if (_messageTextObj != null)
		{
			Message = _messageTextObj.ToString();
		}
		CloseText = new TextObject("{=yQtzabbe}Close").ToString();
	}

	public void ShowInformation(TextObject title, TextObject message)
	{
		IsEnabled = true;
		_titleTextObj = title;
		_messageTextObj = message;
		RefreshValues();
	}

	public void ShowInformation(string title, string message)
	{
		IsEnabled = true;
		_titleTextObj = null;
		_messageTextObj = null;
		Title = title;
		Message = message;
		RefreshValues();
	}

	public void ExecuteClose()
	{
		IsEnabled = false;
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
