using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public class MPHostGameOptionsVM : ViewModel
{
	private static readonly HashSet<MultiplayerOptions.OptionType> _premadeGameOptions = new HashSet<MultiplayerOptions.OptionType>
	{
		MultiplayerOptions.OptionType.ServerName,
		MultiplayerOptions.OptionType.GamePassword,
		MultiplayerOptions.OptionType.CultureTeam1,
		MultiplayerOptions.OptionType.CultureTeam2,
		MultiplayerOptions.OptionType.Map,
		MultiplayerOptions.OptionType.PremadeMatchGameMode,
		MultiplayerOptions.OptionType.PremadeGameType
	};

	private readonly MPCustomGameVM.CustomGameMode _customGameMode;

	private readonly Dictionary<MultiplayerOptions.OptionType, GenericHostGameOptionDataVM> _optionsByType;

	private readonly Dictionary<HostGameOptionSection, HostGameOptionSectionVM> _sectionsByType;

	private MBBindingList<HostGameOptionSectionVM> _sections;

	public Action OnAnyOptionChanged { get; set; }

	public Action OnGameModeChangedByUser { get; set; }

	public string CurrentGameModeName
	{
		get
		{
			if (_customGameMode != MPCustomGameVM.CustomGameMode.CustomServer)
			{
				return MultiplayerOptions.OptionType.PremadeMatchGameMode.GetStrValue();
			}
			return MultiplayerOptions.OptionType.GameType.GetStrValue();
		}
	}

	public IEnumerable<GenericHostGameOptionDataVM> AllOptions => _optionsByType.Values;

	[DataSourceProperty]
	public MBBindingList<HostGameOptionSectionVM> Sections
	{
		get
		{
			return _sections;
		}
		set
		{
			if (value != _sections)
			{
				_sections = value;
				OnPropertyChangedWithValue(value, "Sections");
			}
		}
	}

	public MPHostGameOptionsVM(MPCustomGameVM.CustomGameMode customGameMode)
	{
		_customGameMode = customGameMode;
		_optionsByType = new Dictionary<MultiplayerOptions.OptionType, GenericHostGameOptionDataVM>();
		_sectionsByType = new Dictionary<HostGameOptionSection, HostGameOptionSectionVM>();
		Sections = new MBBindingList<HostGameOptionSectionVM>();
		CreateSectionsAndOptions();
		InitializeGameMode();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Sections.ApplyActionOnAllItems(delegate(HostGameOptionSectionVM x)
		{
			x.RefreshValues();
		});
	}

	public bool HasOption(MultiplayerOptions.OptionType optionType)
	{
		return _optionsByType.ContainsKey(optionType);
	}

	public bool IsOptionShown(MultiplayerOptions.OptionType optionType)
	{
		if (_optionsByType.TryGetValue(optionType, out var value))
		{
			return value.IsVisible;
		}
		return false;
	}

	public void RefreshAllOptionData()
	{
		foreach (GenericHostGameOptionDataVM value in _optionsByType.Values)
		{
			value.RefreshData();
		}
		RefreshVisibility();
	}

	public void RefreshSelectionLists()
	{
		RefreshSelectionLists(includeGameModeOptions: true);
	}

	private void RefreshSelectionLists(bool includeGameModeOptions)
	{
		foreach (GenericHostGameOptionDataVM value in _optionsByType.Values)
		{
			if ((includeGameModeOptions || !IsGameModeOption(value.OptionType)) && value is MultipleSelectionHostGameOptionDataVM multipleSelectionHostGameOptionDataVM)
			{
				multipleSelectionHostGameOptionDataVM.RefreshList();
			}
		}
	}

	private void CreateSectionsAndOptions()
	{
		HostGameOptionSection[] array = (HostGameOptionSection[])Enum.GetValues(typeof(HostGameOptionSection));
		foreach (HostGameOptionSection hostGameOptionSection in array)
		{
			HostGameOptionSectionVM hostGameOptionSectionVM = new HostGameOptionSectionVM(hostGameOptionSection, hostGameOptionSection != HostGameOptionSection.Advanced);
			_sectionsByType.Add(hostGameOptionSection, hostGameOptionSectionVM);
			Sections.Add(hostGameOptionSectionVM);
		}
		List<HostGameOptionPresentation> list = new List<HostGameOptionPresentation>();
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			if (IsOptionUsedByThisMode(optionType))
			{
				HostGameOptionPresentation hostGameOptionPresentation = HostGameOptionPresentation.Get(optionType);
				if (!hostGameOptionPresentation.IsTeam2HalfOfPair)
				{
					list.Add(hostGameOptionPresentation);
				}
			}
		}
		list.Sort(delegate(HostGameOptionPresentation x, HostGameOptionPresentation y)
		{
			int order = x.Order;
			return order.CompareTo(y.Order);
		});
		foreach (HostGameOptionPresentation item in list)
		{
			GenericHostGameOptionDataVM genericHostGameOptionDataVM = CreateOption(item.OptionType, (int)item.OptionType);
			genericHostGameOptionDataVM.OnValueChanged = OnOptionValueChanged;
			_optionsByType.Add(item.OptionType, genericHostGameOptionDataVM);
			_sectionsByType[item.Section].Options.Add(genericHostGameOptionDataVM);
			if (genericHostGameOptionDataVM is MultipleSelectionHostGameOptionDataVM multipleSelectionHostGameOptionDataVM && IsGameModeOption(item.OptionType))
			{
				multipleSelectionHostGameOptionDataVM.OnChangedSelection = OnGameModeSelectionChanged;
			}
		}
	}

	private bool IsOptionUsedByThisMode(MultiplayerOptions.OptionType optionType)
	{
		if (_customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			return _premadeGameOptions.Contains(optionType);
		}
		if (optionType != MultiplayerOptions.OptionType.PremadeMatchGameMode)
		{
			return optionType != MultiplayerOptions.OptionType.PremadeGameType;
		}
		return false;
	}

	private static bool IsGameModeOption(MultiplayerOptions.OptionType optionType)
	{
		if (optionType != MultiplayerOptions.OptionType.GameType)
		{
			return optionType == MultiplayerOptions.OptionType.PremadeMatchGameMode;
		}
		return true;
	}

	private void InitializeGameMode()
	{
		if (_customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			MultiplayerOptions.Instance.SetValueForOptionWithMultipleSelectionFromText(MultiplayerOptions.OptionType.PremadeMatchGameMode, "Skirmish");
		}
		OnGameModeChanged();
	}

	private void OnGameModeSelectionChanged(MultipleSelectionHostGameOptionDataVM option)
	{
		OnGameModeChangedByUser?.Invoke();
		OnGameModeChanged();
	}

	private void OnGameModeChanged()
	{
		RefreshSelectionLists(includeGameModeOptions: false);
		RefreshAllOptionData();
	}

	private void OnOptionValueChanged(GenericHostGameOptionDataVM option)
	{
		RefreshVisibility();
		OnAnyOptionChanged?.Invoke();
	}

	private void RefreshVisibility()
	{
		string currentGameModeName = CurrentGameModeName;
		foreach (GenericHostGameOptionDataVM value in _optionsByType.Values)
		{
			value.IsVisible = IsValidForGameMode(value.OptionType, currentGameModeName) && IsRelevantNow(value.Presentation);
			if (value is NumericHostGameOptionDataVM { Team2: not null } numericHostGameOptionDataVM)
			{
				numericHostGameOptionDataVM.Team2.IsVisible = IsValidForGameMode(numericHostGameOptionDataVM.Team2.OptionType, currentGameModeName);
			}
		}
		foreach (HostGameOptionSectionVM section in Sections)
		{
			section.RefreshHasAnyVisibleOption();
		}
	}

	private bool IsValidForGameMode(MultiplayerOptions.OptionType optionType, string gameModeName)
	{
		if (_customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame)
		{
			return true;
		}
		return optionType.GetOptionProperty().ValidGameModes?.Contains(gameModeName) ?? true;
	}

	private static bool IsRelevantNow(HostGameOptionPresentation presentation)
	{
		if (presentation.IsRelevant != null)
		{
			return presentation.IsRelevant();
		}
		return true;
	}

	private static GenericHostGameOptionDataVM CreateOption(MultiplayerOptions.OptionType type, int preferredIndex)
	{
		switch (GetSpecificHostGameOptionTypeOf(type))
		{
		case OptionsVM.OptionsDataType.BooleanOption:
			return new BooleanHostGameOptionDataVM(type, preferredIndex);
		case OptionsVM.OptionsDataType.NumericOption:
			return new NumericHostGameOptionDataVM(type, preferredIndex);
		case OptionsVM.OptionsDataType.InputOption:
			return new InputHostGameOptionDataVM(type, preferredIndex);
		case OptionsVM.OptionsDataType.MultipleSelectionOption:
			return new MultipleSelectionHostGameOptionDataVM(type, preferredIndex);
		default:
			Debug.FailedAssert($"Host game option {type} has an option value type the host screen can't show.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\HostGame\\MPHostGameOptionsVM.cs", "CreateOption", 235);
			return null;
		}
	}

	private static OptionsVM.OptionsDataType GetSpecificHostGameOptionTypeOf(MultiplayerOptions.OptionType type)
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
