using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanSendPostPopupVM : ViewModel
{
	public enum PostPopupMode
	{
		Information,
		Announcement
	}

	private PostPopupMode _popupMode;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isSelected;

	private string _titleText;

	private string _postData;

	private string _sendText;

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
				OnPropertyChanged("CancelInputKey");
			}
		}
	}

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
				OnPropertyChanged("DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}
	}

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
				OnPropertyChanged("TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string PostData
	{
		get
		{
			return _postData;
		}
		set
		{
			if (value != _postData)
			{
				_postData = value;
				OnPropertyChanged("PostData");
			}
		}
	}

	[DataSourceProperty]
	public string SendText
	{
		get
		{
			return _sendText;
		}
		set
		{
			if (value != _sendText)
			{
				_sendText = value;
				OnPropertyChanged("SendText");
			}
		}
	}

	public MPLobbyClanSendPostPopupVM(PostPopupMode popupMode)
	{
		_popupMode = popupMode;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SendText = new TextObject("{=qTYsYJ9V}Send").ToString();
		if (_popupMode == PostPopupMode.Information)
		{
			TitleText = new TextObject("{=zravuI1b}Type Clan Information").ToString();
		}
		else if (_popupMode == PostPopupMode.Announcement)
		{
			TitleText = new TextObject("{=g5W32uf4}Type Your Announcement").ToString();
		}
	}

	public void ExecuteOpenPopup()
	{
		IsSelected = true;
		PostData = "";
	}

	public void ExecuteClosePopup()
	{
		IsSelected = false;
	}

	public async void ExecuteSend()
	{
		if ((_popupMode == PostPopupMode.Information) ? (await NetworkMain.GameClient.SetClanInformationText(PostData)) : (_popupMode != PostPopupMode.Announcement || await NetworkMain.GameClient.AddClanAnnouncement(PostData)))
		{
			ExecuteClosePopup();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
