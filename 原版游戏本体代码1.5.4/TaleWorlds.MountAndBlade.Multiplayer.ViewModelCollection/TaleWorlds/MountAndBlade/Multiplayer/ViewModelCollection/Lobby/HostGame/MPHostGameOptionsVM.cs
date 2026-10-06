using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public class MPHostGameOptionsVM : ViewModel
{
	private class OptionPreferredIndexComparer : IComparer<GenericHostGameOptionDataVM>
	{
		public int Compare(GenericHostGameOptionDataVM x, GenericHostGameOptionDataVM y)
		{
			return x.PreferredIndex.CompareTo(y.PreferredIndex);
		}
	}

	private List<GenericHostGameOptionDataVM> _hostGameItemsForNextTick = new List<GenericHostGameOptionDataVM>();

	private OptionPreferredIndexComparer _optionComparer;

	private MPCustomGameVM.CustomGameMode _customGameMode;

	private bool _isRefreshed;

	private bool _isInMission;

	private MBBindingList<GenericHostGameOptionDataVM> _generalOptions;

	[DataSourceProperty]
	public MBBindingList<GenericHostGameOptionDataVM> GeneralOptions
	{
		get
		{
			return _generalOptions;
		}
		set
		{
			if (value != _generalOptions)
			{
				_generalOptions = value;
				OnPropertyChangedWithValue(value, "GeneralOptions");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRefreshed
	{
		get
		{
			return _isRefreshed;
		}
		set
		{
			if (value != _isRefreshed)
			{
				_isRefreshed = value;
				OnPropertyChangedWithValue(value, "IsRefreshed");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInMission
	{
		get
		{
			return _isInMission;
		}
		set
		{
			if (value != _isInMission)
			{
				_isInMission = value;
				OnPropertyChangedWithValue(value, "IsInMission");
			}
		}
	}

	public MPHostGameOptionsVM(bool isInMission, MPCustomGameVM.CustomGameMode customGameMode = MPCustomGameVM.CustomGameMode.CustomServer)
	{
		IsInMission = isInMission;
		GeneralOptions = new MBBindingList<GenericHostGameOptionDataVM>();
		_optionComparer = new OptionPreferredIndexComparer();
		_customGameMode = customGameMode;
		InitializeDefaultOptionList();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GeneralOptions.ApplyActionOnAllItems(delegate(GenericHostGameOptionDataVM x)
		{
			x.RefreshValues();
		});
	}

	private void InitializeDefaultOptionList()
	{
		IsRefreshed = false;
		string text = "";
		if (_customGameMode == MPCustomGameVM.CustomGameMode.CustomServer)
		{
			text = MultiplayerOptions.OptionType.GameType.GetStrValue();
		}
		else
		{
			text = "Skirmish";
			MultiplayerOptions.Instance.SetValueForOptionWithMultipleSelectionFromText(MultiplayerOptions.OptionType.PremadeMatchGameMode, text);
		}
		OnGameModeChanged(text);
		foreach (GenericHostGameOptionDataVM item in GeneralOptions.ToList())
		{
			if ((item.OptionType == MultiplayerOptions.OptionType.GameType || item.OptionType == MultiplayerOptions.OptionType.PremadeMatchGameMode) && item is MultipleSelectionHostGameOptionDataVM)
			{
				(item as MultipleSelectionHostGameOptionDataVM).OnChangedSelection = OnChangeSelected;
			}
		}
		IsRefreshed = true;
	}

	private void OnChangeSelected(MultipleSelectionHostGameOptionDataVM option)
	{
		IsRefreshed = false;
		if (option.OptionType == MultiplayerOptions.OptionType.GameType || option.OptionType == MultiplayerOptions.OptionType.PremadeMatchGameMode)
		{
			OnGameModeChanged(MultiplayerOptions.Instance.GetMultiplayerOptionsList(MultiplayerOptions.OptionType.GameType)[option.Selector.SelectedIndex]);
		}
		IsRefreshed = true;
	}

	private void OnGameModeChanged(string gameModeName)
	{
		_hostGameItemsForNextTick.Clear();
		if (_customGameMode == MPCustomGameVM.CustomGameMode.CustomServer)
		{
			FillOptionsForCustomServer(gameModeName);
		}
		else if (_customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			FillOptionsForPremadeGame();
		}
		(GeneralOptions.First((GenericHostGameOptionDataVM o) => o.OptionType == MultiplayerOptions.OptionType.Map) as MultipleSelectionHostGameOptionDataVM)?.RefreshList();
		GeneralOptions.Sort(_optionComparer);
	}

	private void FillOptionsForCustomServer(string gameModeName)
	{
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			if (optionType == MultiplayerOptions.OptionType.PremadeMatchGameMode || optionType == MultiplayerOptions.OptionType.PremadeGameType || optionProperty == null)
			{
				continue;
			}
			int preferredIndex = (int)optionType;
			bool flag = optionProperty.ValidGameModes == null;
			if (optionProperty.ValidGameModes != null && optionProperty.ValidGameModes.Contains(gameModeName))
			{
				flag = true;
			}
			GenericHostGameOptionDataVM genericHostGameOptionDataVM = GeneralOptions.FirstOrDefault((GenericHostGameOptionDataVM o) => o.PreferredIndex == preferredIndex);
			if (flag)
			{
				if (genericHostGameOptionDataVM == null)
				{
					GenericHostGameOptionDataVM item = CreateOption(optionType, preferredIndex);
					GeneralOptions.Add(item);
				}
				else
				{
					genericHostGameOptionDataVM.RefreshData();
				}
			}
			else if (genericHostGameOptionDataVM != null)
			{
				GeneralOptions.Remove(genericHostGameOptionDataVM);
			}
		}
	}

	private void FillOptionsForPremadeGame()
	{
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			bool flag = false;
			if (optionType == MultiplayerOptions.OptionType.ServerName || optionType == MultiplayerOptions.OptionType.GamePassword || optionType == MultiplayerOptions.OptionType.CultureTeam1 || optionType == MultiplayerOptions.OptionType.CultureTeam2 || optionType == MultiplayerOptions.OptionType.Map || optionType == MultiplayerOptions.OptionType.PremadeMatchGameMode || optionType == MultiplayerOptions.OptionType.PremadeGameType)
			{
				flag = true;
			}
			if (flag && optionProperty != null)
			{
				int preferredIndex = (int)optionType;
				GenericHostGameOptionDataVM genericHostGameOptionDataVM = GeneralOptions.FirstOrDefault((GenericHostGameOptionDataVM o) => o.PreferredIndex == preferredIndex);
				if (genericHostGameOptionDataVM == null)
				{
					GenericHostGameOptionDataVM item = CreateOption(optionType, preferredIndex);
					GeneralOptions.Add(item);
				}
				else
				{
					genericHostGameOptionDataVM.RefreshData();
				}
			}
		}
	}

	private GenericHostGameOptionDataVM CreateOption(MultiplayerOptions.OptionType type, int preferredIndex)
	{
		GenericHostGameOptionDataVM genericHostGameOptionDataVM = null;
		switch (GetSpecificHostGameOptionTypeOf(type))
		{
		case OptionsVM.OptionsDataType.BooleanOption:
			genericHostGameOptionDataVM = new BooleanHostGameOptionDataVM(type, preferredIndex);
			break;
		case OptionsVM.OptionsDataType.NumericOption:
			genericHostGameOptionDataVM = new NumericHostGameOptionDataVM(type, preferredIndex);
			break;
		case OptionsVM.OptionsDataType.InputOption:
			genericHostGameOptionDataVM = new InputHostGameOptionDataVM(type, preferredIndex);
			break;
		case OptionsVM.OptionsDataType.MultipleSelectionOption:
			genericHostGameOptionDataVM = new MultipleSelectionHostGameOptionDataVM(type, preferredIndex);
			break;
		}
		if (genericHostGameOptionDataVM == null)
		{
			Debug.FailedAssert("Item was not added to host game options because it has an invalid type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\HostGame\\MPHostGameOptionsVM.cs", "CreateOption", 218);
			return null;
		}
		return genericHostGameOptionDataVM;
	}

	private OptionsVM.OptionsDataType GetSpecificHostGameOptionTypeOf(MultiplayerOptions.OptionType type)
	{
		MultiplayerOptionsProperty optionProperty = type.GetOptionProperty();
		switch (optionProperty.OptionValueType)
		{
		case MultiplayerOptions.OptionValueType.Bool:
			return OptionsVM.OptionsDataType.BooleanOption;
		case MultiplayerOptions.OptionValueType.Integer:
			return OptionsVM.OptionsDataType.NumericOption;
		case MultiplayerOptions.OptionValueType.Enum:
			return OptionsVM.OptionsDataType.MultipleSelectionOption;
		case MultiplayerOptions.OptionValueType.String:
			if (!optionProperty.HasMultipleSelections)
			{
				return OptionsVM.OptionsDataType.InputOption;
			}
			return OptionsVM.OptionsDataType.MultipleSelectionOption;
		default:
			return OptionsVM.OptionsDataType.None;
		}
	}
}
