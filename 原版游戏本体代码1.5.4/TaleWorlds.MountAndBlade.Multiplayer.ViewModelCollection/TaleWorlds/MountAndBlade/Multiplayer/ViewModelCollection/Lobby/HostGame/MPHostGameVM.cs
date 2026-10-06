using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public class MPHostGameVM : ViewModel
{
	private LobbyState _lobbyState;

	private MPCustomGameVM.CustomGameMode _customGameMode;

	private MPHostGameOptionsVM _hostGameOptions;

	private string _createText;

	[DataSourceProperty]
	public MPHostGameOptionsVM HostGameOptions
	{
		get
		{
			return _hostGameOptions;
		}
		set
		{
			if (value != _hostGameOptions)
			{
				_hostGameOptions = value;
				OnPropertyChangedWithValue(value, "HostGameOptions");
			}
		}
	}

	[DataSourceProperty]
	public string CreateText
	{
		get
		{
			return _createText;
		}
		set
		{
			if (value != _createText)
			{
				_createText = value;
				OnPropertyChangedWithValue(value, "CreateText");
			}
		}
	}

	public MPHostGameVM(LobbyState lobbyState, MPCustomGameVM.CustomGameMode customGameMode)
	{
		_lobbyState = lobbyState;
		_customGameMode = customGameMode;
		HostGameOptions = new MPHostGameOptionsVM(isInMission: false, _customGameMode);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CreateText = new TextObject("{=aRzlp5XH}CREATE").ToString();
		HostGameOptions.RefreshValues();
	}

	public void ExecuteStart()
	{
		if (_customGameMode == MPCustomGameVM.CustomGameMode.CustomServer)
		{
			_lobbyState.HostGame();
		}
		else if (_customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			_lobbyState.CreatePremadeGame();
		}
	}
}
