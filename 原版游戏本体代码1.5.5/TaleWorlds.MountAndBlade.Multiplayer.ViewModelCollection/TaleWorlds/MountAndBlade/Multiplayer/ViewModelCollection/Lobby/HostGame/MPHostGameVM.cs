using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public class MPHostGameVM : ViewModel
{
	private const string SharedSettingsScope = "Shared";

	private readonly LobbyState _lobbyState;

	private readonly MPCustomGameVM.CustomGameMode _customGameMode;

	private MPHostGameOptionsVM _hostGameOptions;

	private string _createText;

	private string _resetText;

	private string _createDisabledReasonText;

	private bool _canCreate;

	private HintViewModel _createHint;

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

	[DataSourceProperty]
	public string ResetText
	{
		get
		{
			return _resetText;
		}
		set
		{
			if (value != _resetText)
			{
				_resetText = value;
				OnPropertyChangedWithValue(value, "ResetText");
			}
		}
	}

	[DataSourceProperty]
	public string CreateDisabledReasonText
	{
		get
		{
			return _createDisabledReasonText;
		}
		set
		{
			if (value != _createDisabledReasonText)
			{
				_createDisabledReasonText = value;
				OnPropertyChangedWithValue(value, "CreateDisabledReasonText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanCreate
	{
		get
		{
			return _canCreate;
		}
		set
		{
			if (value != _canCreate)
			{
				_canCreate = value;
				OnPropertyChangedWithValue(value, "CanCreate");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CreateHint
	{
		get
		{
			return _createHint;
		}
		set
		{
			if (value != _createHint)
			{
				_createHint = value;
				OnPropertyChangedWithValue(value, "CreateHint");
			}
		}
	}

	public MPHostGameVM(LobbyState lobbyState, MPCustomGameVM.CustomGameMode customGameMode)
	{
		_lobbyState = lobbyState;
		_customGameMode = customGameMode;
		HostGameOptions = new MPHostGameOptionsVM(_customGameMode);
		HostGameOptions.OnAnyOptionChanged = RefreshCanCreate;
		if (_customGameMode == MPCustomGameVM.CustomGameMode.CustomServer)
		{
			HostGameOptions.OnGameModeChangedByUser = LoadSavedGameModeSettings;
		}
		RefreshValues();
		RefreshCanCreate();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CreateText = new TextObject("{=aRzlp5XH}CREATE").ToString();
		ResetText = new TextObject("{=*}Reset to Defaults").ToString();
		HostGameOptions.RefreshValues();
		RefreshCanCreate();
	}

	public void OnPanelOpened()
	{
		if (_customGameMode == MPCustomGameVM.CustomGameMode.CustomServer)
		{
			LoadSavedSettings();
		}
		HostGameOptions.RefreshSelectionLists();
		HostGameOptions.RefreshAllOptionData();
		RefreshCanCreate();
	}

	public void ExecuteStart()
	{
		if (_customGameMode == MPCustomGameVM.CustomGameMode.CustomServer)
		{
			SaveSettings();
			_lobbyState.HostGame();
		}
		else if (_customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			_lobbyState.CreatePremadeGame();
		}
	}

	public void ExecuteResetToDefaults()
	{
		MultiplayerOptions.Instance.ResetCurrentToInitialValuesKeepingGameType();
		HostGameOptions.RefreshSelectionLists();
		HostGameOptions.RefreshAllOptionData();
		RefreshCanCreate();
	}

	private void RefreshCanCreate()
	{
		TextObject createDisabledReason = GetCreateDisabledReason();
		CanCreate = createDisabledReason == null;
		CreateDisabledReasonText = createDisabledReason?.ToString() ?? string.Empty;
		CreateHint = ((createDisabledReason != null) ? new HintViewModel(createDisabledReason) : new HintViewModel());
	}

	private TextObject GetCreateDisabledReason()
	{
		int intValue = MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue();
		if (HostGameOptions.IsOptionShown(MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart) && MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue() > intValue)
		{
			return new TextObject("{=*}Players needed to start can't be more than the player limit.");
		}
		if (string.IsNullOrEmpty(MultiplayerOptions.OptionType.Map.GetStrValue()))
		{
			return new TextObject("{=*}Pick a map for this game type.");
		}
		if (HostGameOptions.HasOption(MultiplayerOptions.OptionType.StreamerPassword) && MultiplayerOptions.IsStreamerPasswordSameAsGamePassword())
		{
			return new TextObject("{=*}Streamer password can't be the same as the game password.");
		}
		return null;
	}

	private void SaveSettings()
	{
		HostGameSettingsData hostGameSettingsData = new HostGameSettingsData
		{
			Scope = "Shared"
		};
		HostGameSettingsData hostGameSettingsData2 = new HostGameSettingsData
		{
			Scope = HostGameOptions.CurrentGameModeName
		};
		foreach (GenericHostGameOptionDataVM allOption in HostGameOptions.AllOptions)
		{
			AddSavedValue(allOption.OptionType, hostGameSettingsData, hostGameSettingsData2);
			if (allOption.Presentation.PairedTeam2Option.HasValue)
			{
				AddSavedValue(allOption.Presentation.PairedTeam2Option.Value, hostGameSettingsData, hostGameSettingsData2);
			}
		}
		MultiplayerLocalDataManager.Instance.HostGameSettings.SaveSettings(hostGameSettingsData);
		MultiplayerLocalDataManager.Instance.HostGameSettings.SaveSettings(hostGameSettingsData2);
	}

	private static void AddSavedValue(MultiplayerOptions.OptionType optionType, HostGameSettingsData sharedSettings, HostGameSettingsData gameModeSettings)
	{
		HostGameOptionPresentation hostGameOptionPresentation = HostGameOptionPresentation.Get(optionType);
		if (hostGameOptionPresentation.IsSavedBetweenSessions)
		{
			(hostGameOptionPresentation.IsSharedAcrossGameTypes ? sharedSettings : gameModeSettings).Values[optionType.ToString()] = optionType.GetValueText();
		}
	}

	private void LoadSavedSettings()
	{
		if (MultiplayerLocalDataManager.Instance.HostGameSettings.TryGetSettings("Shared", out var settingsData))
		{
			ApplySavedValues(settingsData.Values);
		}
		LoadSavedGameModeSettings();
	}

	private void LoadSavedGameModeSettings()
	{
		if (MultiplayerLocalDataManager.Instance.HostGameSettings.TryGetSettings(HostGameOptions.CurrentGameModeName, out var settingsData))
		{
			ApplySavedValues(settingsData.Values);
		}
	}

	private static void ApplySavedValues(Dictionary<string, string> savedValues)
	{
		foreach (KeyValuePair<string, string> savedValue in savedValues)
		{
			if (MultiplayerOptions.TryGetOptionTypeFromString(savedValue.Key, out var optionType, out var optionAttribute) && HostGameOptionPresentation.Get(optionType).IsSavedBetweenSessions)
			{
				ApplySavedValue(optionType, optionAttribute, savedValue.Value);
			}
		}
	}

	private static void ApplySavedValue(MultiplayerOptions.OptionType optionType, MultiplayerOptionsProperty attribute, string savedValue)
	{
		switch (attribute.OptionValueType)
		{
		case MultiplayerOptions.OptionValueType.Bool:
		{
			if (bool.TryParse(savedValue, out var result2))
			{
				optionType.SetValue(result2);
			}
			break;
		}
		case MultiplayerOptions.OptionValueType.Integer:
		case MultiplayerOptions.OptionValueType.Enum:
		{
			if (int.TryParse(savedValue, out var result) && result >= attribute.BoundsMin && result <= attribute.BoundsMax)
			{
				optionType.SetValue(result);
			}
			break;
		}
		case MultiplayerOptions.OptionValueType.String:
			if (attribute.HasMultipleSelections)
			{
				if (MultiplayerOptions.Instance.GetMultiplayerOptionsList(optionType).Contains(savedValue))
				{
					MultiplayerOptions.Instance.SetValueForOptionWithMultipleSelectionFromText(optionType, savedValue);
				}
			}
			else
			{
				optionType.SetValue(savedValue);
			}
			break;
		}
	}
}
