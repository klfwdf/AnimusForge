using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyBannerlordIDAddFriendPopupVM : ViewModel
{
	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isSelected;

	private string _titleText;

	private string _addText;

	private string _errorText;

	private string _bannerlordIDInputText;

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
	public string AddText
	{
		get
		{
			return _addText;
		}
		set
		{
			if (value != _addText)
			{
				_addText = value;
				OnPropertyChanged("AddText");
			}
		}
	}

	[DataSourceProperty]
	public string ErrorText
	{
		get
		{
			return _errorText;
		}
		set
		{
			if (value != _errorText)
			{
				_errorText = value;
				OnPropertyChanged("ErrorText");
			}
		}
	}

	[DataSourceProperty]
	public string BannerlordIDInputText
	{
		get
		{
			return _bannerlordIDInputText;
		}
		set
		{
			if (value != _bannerlordIDInputText)
			{
				_bannerlordIDInputText = value;
				OnPropertyChanged("BannerlordIDInputText");
				ErrorText = "";
			}
		}
	}

	public MPLobbyBannerlordIDAddFriendPopupVM()
	{
		BannerlordIDInputText = "";
		ErrorText = "";
		RefreshValues();
	}

	public void ExecuteOpenPopup()
	{
		IsSelected = true;
	}

	public void ExecuteClosePopup()
	{
		IsSelected = false;
		BannerlordIDInputText = "";
		ErrorText = "";
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=L3DJHTdY}Enter Bannerlord ID").ToString();
		AddText = new TextObject("{=tC9C8TLi}Add Friend").ToString();
	}

	public async void ExecuteTryAddFriend()
	{
		string[] array = BannerlordIDInputText.Split(new char[1] { '#' });
		if (array.Length == 2 && !array[1].IsEmpty())
		{
			string username = array[0];
			int id = 0;
			bool flag = Common.IsAllLetters(array[0]) && array[0].Length >= Parameters.UsernameMinLength;
			if (int.TryParse(array[1], out id) && flag)
			{
				if (await NetworkMain.GameClient.DoesPlayerWithUsernameAndIdExist(username, id))
				{
					NetworkMain.GameClient.AddFriendByUsernameAndId(username, id, BannerlordConfig.EnableGenericNames);
					ExecuteClosePopup();
				}
				else
				{
					ErrorText = new TextObject("{=tTwQsP6j}Player does not exist").ToString();
				}
			}
			else
			{
				ErrorText = new TextObject("{=rWm5udCd}You must enter a valid Bannerlord ID").ToString();
			}
		}
		else
		{
			ErrorText = new TextObject("{=rWm5udCd}You must enter a valid Bannerlord ID").ToString();
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
