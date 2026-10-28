using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyRejoinVM : ViewModel
{
	private readonly Action<MPLobbyVM.LobbyPage> _onChangePageRequest;

	public Action OnRejoinRequested;

	private bool _isEnabled;

	private bool _isRejoining;

	private string _titleText;

	private string _descriptionText;

	private string _rejoinText;

	private string _fleeText;

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
	public bool IsRejoining
	{
		get
		{
			return _isRejoining;
		}
		set
		{
			if (value != _isRejoining)
			{
				_isRejoining = value;
				OnPropertyChangedWithValue(value, "IsRejoining");
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
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string RejoinText
	{
		get
		{
			return _rejoinText;
		}
		set
		{
			if (value != _rejoinText)
			{
				_rejoinText = value;
				OnPropertyChangedWithValue(value, "RejoinText");
			}
		}
	}

	[DataSourceProperty]
	public string FleeText
	{
		get
		{
			return _fleeText;
		}
		set
		{
			if (value != _fleeText)
			{
				_fleeText = value;
				OnPropertyChangedWithValue(value, "FleeText");
			}
		}
	}

	public MPLobbyRejoinVM(Action<MPLobbyVM.LobbyPage> onChangePageRequest)
	{
		_onChangePageRequest = onChangePageRequest;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=6zYeU0VO}Disconnected from a match").ToString();
		DescriptionText = new TextObject("{=1A1t1naG}You have left a ranked game in progress. Please reconnect to the game.").ToString();
		RejoinText = new TextObject("{=5gGyaTPL}Reconnect").ToString();
		FleeText = new TextObject("{=3sRdGQou}Leave").ToString();
	}

	private void ExecuteRejoin()
	{
		NetworkMain.GameClient.RejoinBattle();
		TitleText = new TextObject("{=N0DXasar}Reconnecting").ToString();
		DescriptionText = new TextObject("{=BZcFB1My}Please wait while you are reconnecting to the game").ToString();
		IsRejoining = true;
		OnRejoinRequested?.Invoke();
	}

	private void ExecuteFlee()
	{
		NetworkMain.GameClient.FleeBattle();
		_onChangePageRequest?.Invoke(MPLobbyVM.LobbyPage.Home);
	}
}
