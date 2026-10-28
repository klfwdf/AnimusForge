using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyBannerlordIDChangePopup : ViewModel
{
	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isSelected;

	private bool _hasRequestSent;

	private string _bannerlordIDInputText;

	private string _changeBannerlordIDText;

	private string _typeYourNameText;

	private string _requestSentText;

	private string _errorText;

	private string _cancelText;

	private string _doneText;

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
				ErrorText = "";
				OnPropertyChanged("BannerlordIDInputText");
			}
		}
	}

	[DataSourceProperty]
	public string ChangeBannerlordIDText
	{
		get
		{
			return _changeBannerlordIDText;
		}
		set
		{
			if (value != _changeBannerlordIDText)
			{
				_changeBannerlordIDText = value;
				OnPropertyChanged("ChangeBannerlordIDText");
			}
		}
	}

	[DataSourceProperty]
	public string TypeYourNameText
	{
		get
		{
			return _typeYourNameText;
		}
		set
		{
			if (value != _typeYourNameText)
			{
				_typeYourNameText = value;
				OnPropertyChanged("TypeYourNameText");
			}
		}
	}

	[DataSourceProperty]
	public string RequestSentText
	{
		get
		{
			return _requestSentText;
		}
		set
		{
			if (value != _requestSentText)
			{
				_requestSentText = value;
				OnPropertyChanged("RequestSentText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasRequestSent
	{
		get
		{
			return _hasRequestSent;
		}
		set
		{
			if (value != _hasRequestSent)
			{
				_hasRequestSent = value;
				OnPropertyChanged("HasRequestSent");
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
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChanged("CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChanged("DoneText");
			}
		}
	}

	public MPLobbyBannerlordIDChangePopup()
	{
		BannerlordIDInputText = "";
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ChangeBannerlordIDText = new TextObject("{=ozREO8ev}Change Bannerlord ID").ToString();
		TypeYourNameText = new TextObject("{=clxT9H4T}Type Your Name").ToString();
		RequestSentText = new TextObject("{=V2lpn6dc}Your Bannerlord ID changing request has been successfully sent.").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		ErrorText = "";
	}

	public void ExecuteOpenPopup()
	{
		IsSelected = true;
		HasRequestSent = false;
	}

	public void ExecuteClosePopup()
	{
		IsSelected = false;
		BannerlordIDInputText = "";
		ErrorText = "";
	}

	private async Task<bool> IsInputValid()
	{
		if (BannerlordIDInputText.Length < Parameters.UsernameMinLength)
		{
			GameTexts.SetVariable("STR1", new TextObject("{=k7fJ7TF0}Has to be at least"));
			GameTexts.SetVariable("STR2", Parameters.UsernameMinLength);
			string content = GameTexts.FindText("str_STR1_space_STR2").ToString();
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", new TextObject("{=nWJGjCgy}characters"));
			ErrorText = GameTexts.FindText("str_STR1_space_STR2").ToString();
			return false;
		}
		if (!Common.IsAllLetters(BannerlordIDInputText))
		{
			ErrorText = new TextObject("{=Po8jNaXb}Can only contain letters").ToString();
			return false;
		}
		if (!(await PlatformServices.Instance.VerifyString(BannerlordIDInputText)))
		{
			ErrorText = new TextObject("{=bXAIlBHv}Can not contain offensive language").ToString();
			return false;
		}
		return true;
	}

	public async void ExecuteApply()
	{
		if (!HasRequestSent)
		{
			if (await IsInputValid() && await NetworkMain.GameClient.ChangeUsername(BannerlordIDInputText))
			{
				HasRequestSent = true;
				ErrorText = "";
			}
		}
		else
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
