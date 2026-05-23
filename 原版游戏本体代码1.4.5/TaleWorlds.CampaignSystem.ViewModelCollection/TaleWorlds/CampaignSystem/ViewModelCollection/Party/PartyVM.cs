using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PartyVM : ViewModel
{
	private class TroopVMComparer : IComparer<PartyCharacterVM>
	{
		private readonly PartyScreenLogic.TroopComparer _originalTroopComparer;

		public TroopVMComparer(PartyScreenLogic.TroopComparer originalTroopComparer)
		{
			_originalTroopComparer = originalTroopComparer;
		}

		public int Compare(PartyCharacterVM x, PartyCharacterVM y)
		{
			return _originalTroopComparer.Compare(x.Troop, y.Troop);
		}
	}

	private readonly PartyScreenHelper.PartyScreenMode _currentMode;

	private readonly IViewDataTracker _viewDataTracker;

	public bool IsFiveStackModifierActive;

	public bool IsEntireStackModifierActive;

	private PartyCharacterVM _currentCharacter;

	private List<string> _lockedTroopIds;

	private List<string> _lockedPrisonerIds;

	private Func<string, TextObject> _getKeyTextFromKeyId;

	private PartyScreenLogic.TroopSortType _initialSortType;

	private bool _initialSortAscending;

	private List<string> _initialLockedTroopIds;

	private List<string> _initialLockedPrisonerIds;

	public bool IsInConversation;

	private List<Tuple<string, TextObject>> _formationNames;

	private PartySortControllerVM _otherPartySortController;

	private PartySortControllerVM _mainPartySortController;

	private PartyCompositionVM _otherPartyComposition;

	private PartyCompositionVM _mainPartyComposition;

	private PartyCharacterVM _currentFocusedCharacter;

	private UpgradeTargetVM _currentFocusedUpgrade;

	private HeroViewModel _selectedCharacter;

	private MBBindingList<PartyCharacterVM> _otherPartyTroops;

	private MBBindingList<PartyCharacterVM> _otherPartyPrisoners;

	private MBBindingList<PartyCharacterVM> _mainPartyTroops;

	private MBBindingList<PartyCharacterVM> _mainPartyPrisoners;

	private PartyUpgradeTroopVM _upgradePopUp;

	private PartyRecruitTroopVM _recruitPopUp;

	private string _titleLbl;

	private string _mainPartyNameLbl;

	private string _otherPartyNameLbl;

	private string _headerLbl;

	private string _otherPartyAccompanyingLbl;

	private string _talkLbl;

	private string _infoLbl;

	private string _cancelLbl;

	private string _doneLbl;

	private string _troopsLbl;

	private string _prisonersLabel;

	private string _mainPartyTotalGoldLbl;

	private string _mainPartyTotalMoraleLbl;

	private string _mainPartyTotalSpeedLbl;

	private string _mainPartyTotalWeeklyCostLbl;

	private string _currentCharacterWageLbl;

	private string _currentCharacterLevelLbl;

	private BasicTooltipViewModel _transferAllMainTroopsHint;

	private BasicTooltipViewModel _transferAllMainPrisonersHint;

	private BasicTooltipViewModel _transferAllOtherTroopsHint;

	private BasicTooltipViewModel _transferAllOtherPrisonersHint;

	private HintViewModel _moraleHint;

	private HintViewModel _doneHint;

	private BasicTooltipViewModel _speedHint;

	private BasicTooltipViewModel _mainPartyTroopSizeLimitHint;

	private BasicTooltipViewModel _mainPartyPrisonerSizeLimitHint;

	private BasicTooltipViewModel _otherPartyTroopSizeLimitHint;

	private BasicTooltipViewModel _otherPartyPrisonerSizeLimitHint;

	private BasicTooltipViewModel _usedHorsesHint;

	private HintViewModel _denarHint;

	private HintViewModel _totalWageHint;

	private HintViewModel _levelHint;

	private HintViewModel _wageHint;

	private HintViewModel _formationHint;

	private HintViewModel _resetHint;

	private StringItemWithHintVM _currentCharacterTier;

	private bool _isCurrentCharacterFormationEnabled;

	private bool _isCurrentCharacterWageEnabled;

	private bool _arePrisonersRelevantOnCurrentMode;

	private bool _areMembersRelevantOnCurrentMode;

	private bool _canChooseRoles;

	private string _otherPartyTroopsLbl;

	private string _otherPartyPrisonersLbl;

	private string _mainPartyTroopsLbl;

	private string _mainPartyPrisonersLbl;

	private string _goldChangeText;

	private string _moraleChangeText;

	private string _horseChangeText;

	private string _influenceChangeText;

	private bool _isMainTroopsLimitWarningEnabled;

	private bool _isMainPrisonersLimitWarningEnabled;

	private bool _isOtherTroopsLimitWarningEnabled;

	private bool _isOtherPrisonersLimitWarningEnabled;

	private bool _isMainTroopsHaveTransferableTroops;

	private bool _isMainPrisonersHaveTransferableTroops;

	private bool _isOtherTroopsHaveTransferableTroops;

	private bool _isOtherPrisonersHaveTransferableTroops;

	private bool _showQuestProgress;

	private bool _isUpgradePopupButtonHighlightEnabled;

	private int _questProgressRequiredCount;

	private int _questProgressCurrentCount;

	private int _upgradableTroopCount;

	private int _recruitableTroopCount;

	private bool _isDoneDisabled;

	private bool _isCancelDisabled;

	private bool _isUpgradePopUpDisabled;

	private bool _isAnyPopUpOpen;

	private bool _isRecruitPopUpDisabled;

	private bool _scrollToCharacter;

	private bool _isScrollTargetPrisoner;

	private string _scrollCharacterId;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _takeAllTroopsInputKey;

	private InputKeyItemVM _dismissAllTroopsInputKey;

	private InputKeyItemVM _takeAllPrisonersInputKey;

	private InputKeyItemVM _dismissAllPrisonersInputKey;

	private InputKeyItemVM _openUpgradePanelInputKey;

	private InputKeyItemVM _openRecruitPanelInputKey;

	private readonly string _upgradePopupButtonID = "UpgradePopupButton";

	private readonly string _upgradeButtonID = "UpgradeButton";

	private readonly string _recruitButtonID = "RecruitButton";

	private readonly string _transferButtonOnlyOtherPrisonersID = "TransferButtonOnlyOtherPrisoners";

	private bool _isUpgradePopupButtonHighlightApplied;

	private bool _isUpgradeButtonHighlightApplied;

	private bool _isRecruitButtonHighlightApplied;

	private bool _isTransferButtonHighlightApplied;

	private string _latestTutorialElementID;

	public PartyScreenLogic PartyScreenLogic { get; private set; }

	public bool CanRightPartyTakeMoreTroops => PartyScreenLogic.CurrentData.RightMemberRoster.TotalManCount < PartyScreenLogic.RightPartyMembersSizeLimit;

	public bool CanRightPartyTakeMorePrisoners => PartyScreenLogic.CurrentData.RightPrisonerRoster.TotalManCount < PartyScreenLogic.RightPartyPrisonersSizeLimit;

	[DataSourceProperty]
	public PartyCharacterVM CurrentCharacter
	{
		get
		{
			return _currentCharacter;
		}
		set
		{
			if (value != null && _currentCharacter != value)
			{
				_currentCharacter = value;
				RefreshCurrentCharacterInformation();
				OnPropertyChangedWithValue(value, "CurrentCharacter");
				ExecuteRemoveZeroCounts();
			}
		}
	}

	private List<Tuple<string, TextObject>> FormationNames
	{
		get
		{
			if (_formationNames == null)
			{
				int num = 8;
				_formationNames = new List<Tuple<string, TextObject>>(num + 1);
				for (int i = 0; i < num; i++)
				{
					string item = "<img src=\"PartyScreen\\FormationIcons\\" + (i + 1) + "\"/>";
					TextObject item2 = GameTexts.FindText("str_troop_group_name", i.ToString());
					_formationNames.Add(new Tuple<string, TextObject>(item, item2));
				}
			}
			return _formationNames;
		}
	}

	[DataSourceProperty]
	public PartySortControllerVM OtherPartySortController
	{
		get
		{
			return _otherPartySortController;
		}
		set
		{
			if (value != _otherPartySortController)
			{
				_otherPartySortController = value;
				OnPropertyChangedWithValue(value, "OtherPartySortController");
			}
		}
	}

	[DataSourceProperty]
	public PartySortControllerVM MainPartySortController
	{
		get
		{
			return _mainPartySortController;
		}
		set
		{
			if (value != _mainPartySortController)
			{
				_mainPartySortController = value;
				OnPropertyChangedWithValue(value, "MainPartySortController");
			}
		}
	}

	[DataSourceProperty]
	public PartyCompositionVM OtherPartyComposition
	{
		get
		{
			return _otherPartyComposition;
		}
		set
		{
			if (value != _otherPartyComposition)
			{
				_otherPartyComposition = value;
				OnPropertyChangedWithValue(value, "OtherPartyComposition");
			}
		}
	}

	[DataSourceProperty]
	public PartyCompositionVM MainPartyComposition
	{
		get
		{
			return _mainPartyComposition;
		}
		set
		{
			if (value != _mainPartyComposition)
			{
				_mainPartyComposition = value;
				OnPropertyChangedWithValue(value, "MainPartyComposition");
			}
		}
	}

	[DataSourceProperty]
	public PartyCharacterVM CurrentFocusedCharacter
	{
		get
		{
			return _currentFocusedCharacter;
		}
		set
		{
			if (value != _currentFocusedCharacter)
			{
				_currentFocusedCharacter = value;
				OnPropertyChangedWithValue(value, "CurrentFocusedCharacter");
			}
		}
	}

	[DataSourceProperty]
	public UpgradeTargetVM CurrentFocusedUpgrade
	{
		get
		{
			return _currentFocusedUpgrade;
		}
		set
		{
			if (value != _currentFocusedUpgrade)
			{
				_currentFocusedUpgrade = value;
				OnPropertyChangedWithValue(value, "CurrentFocusedUpgrade");
			}
		}
	}

	[DataSourceProperty]
	public string HeaderLbl
	{
		get
		{
			return _headerLbl;
		}
		set
		{
			if (value != _headerLbl)
			{
				_headerLbl = value;
				OnPropertyChangedWithValue(value, "HeaderLbl");
			}
		}
	}

	[DataSourceProperty]
	public string OtherPartyNameLbl
	{
		get
		{
			return _otherPartyNameLbl;
		}
		set
		{
			if (value != _otherPartyNameLbl)
			{
				_otherPartyNameLbl = value;
				OnPropertyChangedWithValue(value, "OtherPartyNameLbl");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PartyCharacterVM> OtherPartyTroops
	{
		get
		{
			return _otherPartyTroops;
		}
		set
		{
			if (value != _otherPartyTroops)
			{
				_otherPartyTroops = value;
				OnPropertyChangedWithValue(value, "OtherPartyTroops");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PartyCharacterVM> OtherPartyPrisoners
	{
		get
		{
			return _otherPartyPrisoners;
		}
		set
		{
			if (value != _otherPartyPrisoners)
			{
				_otherPartyPrisoners = value;
				OnPropertyChangedWithValue(value, "OtherPartyPrisoners");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PartyCharacterVM> MainPartyTroops
	{
		get
		{
			return _mainPartyTroops;
		}
		set
		{
			if (value != _mainPartyTroops)
			{
				_mainPartyTroops = value;
				OnPropertyChangedWithValue(value, "MainPartyTroops");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PartyCharacterVM> MainPartyPrisoners
	{
		get
		{
			return _mainPartyPrisoners;
		}
		set
		{
			if (value != _mainPartyPrisoners)
			{
				_mainPartyPrisoners = value;
				OnPropertyChangedWithValue(value, "MainPartyPrisoners");
			}
		}
	}

	[DataSourceProperty]
	public PartyUpgradeTroopVM UpgradePopUp
	{
		get
		{
			return _upgradePopUp;
		}
		set
		{
			if (value != _upgradePopUp)
			{
				_upgradePopUp = value;
				OnPropertyChangedWithValue(value, "UpgradePopUp");
			}
		}
	}

	[DataSourceProperty]
	public PartyRecruitTroopVM RecruitPopUp
	{
		get
		{
			return _recruitPopUp;
		}
		set
		{
			if (value != _recruitPopUp)
			{
				_recruitPopUp = value;
				OnPropertyChangedWithValue(value, "RecruitPopUp");
			}
		}
	}

	[DataSourceProperty]
	public HeroViewModel SelectedCharacter
	{
		get
		{
			return _selectedCharacter;
		}
		set
		{
			if (value != _selectedCharacter)
			{
				_selectedCharacter = value;
				OnPropertyChangedWithValue(value, "SelectedCharacter");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCharacterLevelLbl
	{
		get
		{
			return _currentCharacterLevelLbl;
		}
		set
		{
			if (value != _currentCharacterLevelLbl)
			{
				_currentCharacterLevelLbl = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterLevelLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCharacterWageLbl
	{
		get
		{
			return _currentCharacterWageLbl;
		}
		set
		{
			if (value != _currentCharacterWageLbl)
			{
				_currentCharacterWageLbl = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterWageLbl");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TransferAllOtherTroopsHint
	{
		get
		{
			return _transferAllOtherTroopsHint;
		}
		set
		{
			if (value != _transferAllOtherTroopsHint)
			{
				_transferAllOtherTroopsHint = value;
				OnPropertyChangedWithValue(value, "TransferAllOtherTroopsHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TransferAllOtherPrisonersHint
	{
		get
		{
			return _transferAllOtherPrisonersHint;
		}
		set
		{
			if (value != _transferAllOtherPrisonersHint)
			{
				_transferAllOtherPrisonersHint = value;
				OnPropertyChangedWithValue(value, "TransferAllOtherPrisonersHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TransferAllMainTroopsHint
	{
		get
		{
			return _transferAllMainTroopsHint;
		}
		set
		{
			if (value != _transferAllMainTroopsHint)
			{
				_transferAllMainTroopsHint = value;
				OnPropertyChangedWithValue(value, "TransferAllMainTroopsHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TransferAllMainPrisonersHint
	{
		get
		{
			return _transferAllMainPrisonersHint;
		}
		set
		{
			if (value != _transferAllMainPrisonersHint)
			{
				_transferAllMainPrisonersHint = value;
				OnPropertyChangedWithValue(value, "TransferAllMainPrisonersHint");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM CurrentCharacterTier
	{
		get
		{
			return _currentCharacterTier;
		}
		set
		{
			if (value != _currentCharacterTier)
			{
				_currentCharacterTier = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterTier");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetHint
	{
		get
		{
			return _resetHint;
		}
		set
		{
			if (value != _resetHint)
			{
				_resetHint = value;
				OnPropertyChangedWithValue(value, "ResetHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DoneHint
	{
		get
		{
			return _doneHint;
		}
		set
		{
			if (value != _doneHint)
			{
				_doneHint = value;
				OnPropertyChangedWithValue(value, "DoneHint");
			}
		}
	}

	[DataSourceProperty]
	public string OtherPartyAccompanyingLbl
	{
		get
		{
			return _otherPartyAccompanyingLbl;
		}
		set
		{
			if (value != _otherPartyAccompanyingLbl)
			{
				_otherPartyAccompanyingLbl = value;
				OnPropertyChangedWithValue(value, "OtherPartyAccompanyingLbl");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MoraleHint
	{
		get
		{
			return _moraleHint;
		}
		set
		{
			if (value != _moraleHint)
			{
				_moraleHint = value;
				OnPropertyChangedWithValue(value, "MoraleHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TotalWageHint
	{
		get
		{
			return _totalWageHint;
		}
		set
		{
			if (value != _totalWageHint)
			{
				_totalWageHint = value;
				OnPropertyChanged("Upgrade2Hint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SpeedHint
	{
		get
		{
			return _speedHint;
		}
		set
		{
			if (value != _speedHint)
			{
				_speedHint = value;
				OnPropertyChangedWithValue(value, "SpeedHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel MainPartyTroopSizeLimitHint
	{
		get
		{
			return _mainPartyTroopSizeLimitHint;
		}
		set
		{
			if (value != _mainPartyTroopSizeLimitHint)
			{
				_mainPartyTroopSizeLimitHint = value;
				OnPropertyChangedWithValue(value, "MainPartyTroopSizeLimitHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel MainPartyPrisonerSizeLimitHint
	{
		get
		{
			return _mainPartyPrisonerSizeLimitHint;
		}
		set
		{
			if (value != _mainPartyPrisonerSizeLimitHint)
			{
				_mainPartyPrisonerSizeLimitHint = value;
				OnPropertyChangedWithValue(value, "MainPartyPrisonerSizeLimitHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel OtherPartyTroopSizeLimitHint
	{
		get
		{
			return _otherPartyTroopSizeLimitHint;
		}
		set
		{
			if (value != _otherPartyTroopSizeLimitHint)
			{
				_otherPartyTroopSizeLimitHint = value;
				OnPropertyChangedWithValue(value, "OtherPartyTroopSizeLimitHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel OtherPartyPrisonerSizeLimitHint
	{
		get
		{
			return _otherPartyPrisonerSizeLimitHint;
		}
		set
		{
			if (value != _otherPartyPrisonerSizeLimitHint)
			{
				_otherPartyPrisonerSizeLimitHint = value;
				OnPropertyChangedWithValue(value, "OtherPartyPrisonerSizeLimitHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel UsedHorsesHint
	{
		get
		{
			return _usedHorsesHint;
		}
		set
		{
			if (value != _usedHorsesHint)
			{
				_usedHorsesHint = value;
				OnPropertyChangedWithValue(value, "UsedHorsesHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DenarHint
	{
		get
		{
			return _denarHint;
		}
		set
		{
			if (value != _denarHint)
			{
				_denarHint = value;
				OnPropertyChangedWithValue(value, "DenarHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LevelHint
	{
		get
		{
			return _levelHint;
		}
		set
		{
			if (value != _levelHint)
			{
				_levelHint = value;
				OnPropertyChangedWithValue(value, "LevelHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel WageHint
	{
		get
		{
			return _wageHint;
		}
		set
		{
			if (value != _wageHint)
			{
				_wageHint = value;
				OnPropertyChangedWithValue(value, "WageHint");
			}
		}
	}

	[DataSourceProperty]
	public string TitleLbl
	{
		get
		{
			return _titleLbl;
		}
		set
		{
			if (value != _titleLbl)
			{
				_titleLbl = value;
				OnPropertyChangedWithValue(value, "TitleLbl");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyNameLbl
	{
		get
		{
			return _mainPartyNameLbl;
		}
		set
		{
			if (value != _mainPartyNameLbl)
			{
				_mainPartyNameLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyNameLbl");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FormationHint
	{
		get
		{
			return _formationHint;
		}
		set
		{
			if (value != _formationHint)
			{
				_formationHint = value;
				OnPropertyChangedWithValue(value, "FormationHint");
			}
		}
	}

	[DataSourceProperty]
	public string TalkLbl
	{
		get
		{
			return _talkLbl;
		}
		set
		{
			if (value != _talkLbl)
			{
				_talkLbl = value;
				OnPropertyChangedWithValue(value, "TalkLbl");
			}
		}
	}

	[DataSourceProperty]
	public string InfoLbl
	{
		get
		{
			return _infoLbl;
		}
		set
		{
			if (value != _infoLbl)
			{
				_infoLbl = value;
				OnPropertyChangedWithValue(value, "InfoLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CancelLbl
	{
		get
		{
			return _cancelLbl;
		}
		set
		{
			if (value != _cancelLbl)
			{
				_cancelLbl = value;
				OnPropertyChangedWithValue(value, "CancelLbl");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
			}
		}
	}

	[DataSourceProperty]
	public string TroopsLabel
	{
		get
		{
			return _troopsLbl;
		}
		set
		{
			if (value != _troopsLbl)
			{
				_troopsLbl = value;
				OnPropertyChangedWithValue(value, "TroopsLabel");
			}
		}
	}

	[DataSourceProperty]
	public string PrisonersLabel
	{
		get
		{
			return _prisonersLabel;
		}
		set
		{
			if (value != _prisonersLabel)
			{
				_prisonersLabel = value;
				OnPropertyChangedWithValue(value, "PrisonersLabel");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyTotalGoldLbl
	{
		get
		{
			return _mainPartyTotalGoldLbl;
		}
		set
		{
			if (value != _mainPartyTotalGoldLbl)
			{
				_mainPartyTotalGoldLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyTotalGoldLbl");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyTotalMoraleLbl
	{
		get
		{
			return _mainPartyTotalMoraleLbl;
		}
		set
		{
			if (value != _mainPartyTotalMoraleLbl)
			{
				_mainPartyTotalMoraleLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyTotalMoraleLbl");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyTotalSpeedLbl
	{
		get
		{
			return _mainPartyTotalSpeedLbl;
		}
		set
		{
			if (value != _mainPartyTotalSpeedLbl)
			{
				_mainPartyTotalSpeedLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyTotalSpeedLbl");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyTotalWeeklyCostLbl
	{
		get
		{
			return _mainPartyTotalWeeklyCostLbl;
		}
		set
		{
			if (value != _mainPartyTotalWeeklyCostLbl)
			{
				_mainPartyTotalWeeklyCostLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyTotalWeeklyCostLbl");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentCharacterFormationEnabled
	{
		get
		{
			return _isCurrentCharacterFormationEnabled;
		}
		set
		{
			if (value != _isCurrentCharacterFormationEnabled)
			{
				_isCurrentCharacterFormationEnabled = value;
				OnPropertyChangedWithValue(value, "IsCurrentCharacterFormationEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentCharacterWageEnabled
	{
		get
		{
			return _isCurrentCharacterWageEnabled;
		}
		set
		{
			if (value != _isCurrentCharacterWageEnabled)
			{
				_isCurrentCharacterWageEnabled = value;
				OnPropertyChangedWithValue(value, "IsCurrentCharacterWageEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool CanChooseRoles
	{
		get
		{
			return _canChooseRoles;
		}
		set
		{
			if (value != _canChooseRoles)
			{
				_canChooseRoles = value;
				OnPropertyChangedWithValue(value, "CanChooseRoles");
			}
		}
	}

	[DataSourceProperty]
	public string OtherPartyTroopsLbl
	{
		get
		{
			return _otherPartyTroopsLbl;
		}
		set
		{
			if (value != _otherPartyTroopsLbl)
			{
				_otherPartyTroopsLbl = value;
				OnPropertyChangedWithValue(value, "OtherPartyTroopsLbl");
			}
		}
	}

	[DataSourceProperty]
	public string OtherPartyPrisonersLbl
	{
		get
		{
			return _otherPartyPrisonersLbl;
		}
		set
		{
			if (value != _otherPartyPrisonersLbl)
			{
				_otherPartyPrisonersLbl = value;
				OnPropertyChangedWithValue(value, "OtherPartyPrisonersLbl");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyTroopsLbl
	{
		get
		{
			return _mainPartyTroopsLbl;
		}
		set
		{
			if (value != _mainPartyTroopsLbl)
			{
				_mainPartyTroopsLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyTroopsLbl");
			}
		}
	}

	[DataSourceProperty]
	public string MainPartyPrisonersLbl
	{
		get
		{
			return _mainPartyPrisonersLbl;
		}
		set
		{
			if (value != _mainPartyPrisonersLbl)
			{
				_mainPartyPrisonersLbl = value;
				OnPropertyChangedWithValue(value, "MainPartyPrisonersLbl");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowQuestProgress
	{
		get
		{
			return _showQuestProgress;
		}
		set
		{
			if (value != _showQuestProgress)
			{
				_showQuestProgress = value;
				OnPropertyChangedWithValue(value, "ShowQuestProgress");
			}
		}
	}

	[DataSourceProperty]
	public int QuestProgressRequiredCount
	{
		get
		{
			return _questProgressRequiredCount;
		}
		set
		{
			if (value != _questProgressRequiredCount)
			{
				_questProgressRequiredCount = value;
				OnPropertyChangedWithValue(value, "QuestProgressRequiredCount");
			}
		}
	}

	[DataSourceProperty]
	public int QuestProgressCurrentCount
	{
		get
		{
			return _questProgressCurrentCount;
		}
		set
		{
			if (value != _questProgressCurrentCount)
			{
				_questProgressCurrentCount = value;
				OnPropertyChangedWithValue(value, "QuestProgressCurrentCount");
			}
		}
	}

	[DataSourceProperty]
	public int UpgradableTroopCount
	{
		get
		{
			return _upgradableTroopCount;
		}
		set
		{
			if (value != _upgradableTroopCount)
			{
				_upgradableTroopCount = value;
				OnPropertyChangedWithValue(value, "UpgradableTroopCount");
			}
		}
	}

	[DataSourceProperty]
	public int RecruitableTroopCount
	{
		get
		{
			return _recruitableTroopCount;
		}
		set
		{
			if (value != _recruitableTroopCount)
			{
				_recruitableTroopCount = value;
				OnPropertyChangedWithValue(value, "RecruitableTroopCount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDoneDisabled
	{
		get
		{
			return _isDoneDisabled;
		}
		set
		{
			if (value != _isDoneDisabled)
			{
				_isDoneDisabled = value;
				OnPropertyChangedWithValue(value, "IsDoneDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpgradePopUpDisabled
	{
		get
		{
			return _isUpgradePopUpDisabled;
		}
		set
		{
			if (value != _isUpgradePopUpDisabled)
			{
				_isUpgradePopUpDisabled = value;
				OnPropertyChangedWithValue(value, "IsUpgradePopUpDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRecruitPopUpDisabled
	{
		get
		{
			return _isRecruitPopUpDisabled;
		}
		set
		{
			if (value != _isRecruitPopUpDisabled)
			{
				_isRecruitPopUpDisabled = value;
				OnPropertyChangedWithValue(value, "IsRecruitPopUpDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainPrisonersLimitWarningEnabled
	{
		get
		{
			return _isMainPrisonersLimitWarningEnabled;
		}
		set
		{
			if (value != _isMainPrisonersLimitWarningEnabled)
			{
				_isMainPrisonersLimitWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsMainPrisonersLimitWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainTroopsLimitWarningEnabled
	{
		get
		{
			return _isMainTroopsLimitWarningEnabled;
		}
		set
		{
			if (value != _isMainTroopsLimitWarningEnabled)
			{
				_isMainTroopsLimitWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsMainTroopsLimitWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherPrisonersLimitWarningEnabled
	{
		get
		{
			return _isOtherPrisonersLimitWarningEnabled;
		}
		set
		{
			if (value != _isOtherPrisonersLimitWarningEnabled)
			{
				_isOtherPrisonersLimitWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsOtherPrisonersLimitWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpgradePopupButtonHighlightEnabled
	{
		get
		{
			return _isUpgradePopupButtonHighlightEnabled;
		}
		set
		{
			if (value != _isUpgradePopupButtonHighlightEnabled)
			{
				_isUpgradePopupButtonHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsUpgradePopupButtonHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherTroopsLimitWarningEnabled
	{
		get
		{
			return _isOtherTroopsLimitWarningEnabled;
		}
		set
		{
			if (value != _isOtherTroopsLimitWarningEnabled)
			{
				_isOtherTroopsLimitWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsOtherTroopsLimitWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainTroopsHaveTransferableTroops
	{
		get
		{
			return _isMainTroopsHaveTransferableTroops;
		}
		set
		{
			if (value != _isMainTroopsHaveTransferableTroops)
			{
				_isMainTroopsHaveTransferableTroops = value;
				OnPropertyChangedWithValue(value, "IsMainTroopsHaveTransferableTroops");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainPrisonersHaveTransferableTroops
	{
		get
		{
			return _isMainPrisonersHaveTransferableTroops;
		}
		set
		{
			if (value != _isMainPrisonersHaveTransferableTroops)
			{
				_isMainPrisonersHaveTransferableTroops = value;
				OnPropertyChangedWithValue(value, "IsMainPrisonersHaveTransferableTroops");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherTroopsHaveTransferableTroops
	{
		get
		{
			return _isOtherTroopsHaveTransferableTroops;
		}
		set
		{
			if (value != _isOtherTroopsHaveTransferableTroops)
			{
				_isOtherTroopsHaveTransferableTroops = value;
				OnPropertyChangedWithValue(value, "IsOtherTroopsHaveTransferableTroops");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherPrisonersHaveTransferableTroops
	{
		get
		{
			return _isOtherPrisonersHaveTransferableTroops;
		}
		set
		{
			if (value != _isOtherPrisonersHaveTransferableTroops)
			{
				_isOtherPrisonersHaveTransferableTroops = value;
				OnPropertyChangedWithValue(value, "IsOtherPrisonersHaveTransferableTroops");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCancelDisabled
	{
		get
		{
			return _isCancelDisabled;
		}
		set
		{
			if (value != _isCancelDisabled)
			{
				_isCancelDisabled = value;
				OnPropertyChangedWithValue(value, "IsCancelDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool AreMembersRelevantOnCurrentMode
	{
		get
		{
			return _areMembersRelevantOnCurrentMode;
		}
		set
		{
			if (value != _areMembersRelevantOnCurrentMode)
			{
				_areMembersRelevantOnCurrentMode = value;
				OnPropertyChangedWithValue(value, "AreMembersRelevantOnCurrentMode");
			}
		}
	}

	[DataSourceProperty]
	public bool ArePrisonersRelevantOnCurrentMode
	{
		get
		{
			return _arePrisonersRelevantOnCurrentMode;
		}
		set
		{
			if (value != _arePrisonersRelevantOnCurrentMode)
			{
				_arePrisonersRelevantOnCurrentMode = value;
				OnPropertyChangedWithValue(value, "ArePrisonersRelevantOnCurrentMode");
			}
		}
	}

	[DataSourceProperty]
	public string GoldChangeText
	{
		get
		{
			return _goldChangeText;
		}
		set
		{
			if (value != _goldChangeText)
			{
				_goldChangeText = value;
				OnPropertyChangedWithValue(value, "GoldChangeText");
			}
		}
	}

	[DataSourceProperty]
	public string MoraleChangeText
	{
		get
		{
			return _moraleChangeText;
		}
		set
		{
			if (value != _moraleChangeText)
			{
				_moraleChangeText = value;
				OnPropertyChangedWithValue(value, "MoraleChangeText");
			}
		}
	}

	[DataSourceProperty]
	public string HorseChangeText
	{
		get
		{
			return _horseChangeText;
		}
		set
		{
			if (value != _horseChangeText)
			{
				_horseChangeText = value;
				OnPropertyChangedWithValue(value, "HorseChangeText");
			}
		}
	}

	[DataSourceProperty]
	public string InfluenceChangeText
	{
		get
		{
			return _influenceChangeText;
		}
		set
		{
			if (value != _influenceChangeText)
			{
				_influenceChangeText = value;
				OnPropertyChangedWithValue(value, "InfluenceChangeText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyPopUpOpen
	{
		get
		{
			return _isAnyPopUpOpen;
		}
		set
		{
			if (value != _isAnyPopUpOpen)
			{
				_isAnyPopUpOpen = value;
				OnPropertyChangedWithValue(value, "IsAnyPopUpOpen");
			}
		}
	}

	[DataSourceProperty]
	public bool ScrollToCharacter
	{
		get
		{
			return _scrollToCharacter;
		}
		set
		{
			if (value != _scrollToCharacter)
			{
				_scrollToCharacter = value;
				OnPropertyChangedWithValue(value, "ScrollToCharacter");
			}
		}
	}

	[DataSourceProperty]
	public bool IsScrollTargetPrisoner
	{
		get
		{
			return _isScrollTargetPrisoner;
		}
		set
		{
			if (value != _isScrollTargetPrisoner)
			{
				_isScrollTargetPrisoner = value;
				OnPropertyChangedWithValue(value, "IsScrollTargetPrisoner");
			}
		}
	}

	[DataSourceProperty]
	public string ScrollCharacterId
	{
		get
		{
			return _scrollCharacterId;
		}
		set
		{
			if (value != _scrollCharacterId)
			{
				_scrollCharacterId = value;
				OnPropertyChangedWithValue(value, "ScrollCharacterId");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
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
	public InputKeyItemVM TakeAllTroopsInputKey
	{
		get
		{
			return _takeAllTroopsInputKey;
		}
		set
		{
			if (value != _takeAllTroopsInputKey)
			{
				_takeAllTroopsInputKey = value;
				OnPropertyChangedWithValue(value, "TakeAllTroopsInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DismissAllTroopsInputKey
	{
		get
		{
			return _dismissAllTroopsInputKey;
		}
		set
		{
			if (value != _dismissAllTroopsInputKey)
			{
				_dismissAllTroopsInputKey = value;
				OnPropertyChangedWithValue(value, "DismissAllTroopsInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM TakeAllPrisonersInputKey
	{
		get
		{
			return _takeAllPrisonersInputKey;
		}
		set
		{
			if (value != _takeAllPrisonersInputKey)
			{
				_takeAllPrisonersInputKey = value;
				OnPropertyChangedWithValue(value, "TakeAllPrisonersInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DismissAllPrisonersInputKey
	{
		get
		{
			return _dismissAllPrisonersInputKey;
		}
		set
		{
			if (value != _dismissAllPrisonersInputKey)
			{
				_dismissAllPrisonersInputKey = value;
				OnPropertyChangedWithValue(value, "DismissAllPrisonersInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM OpenUpgradePanelInputKey
	{
		get
		{
			return _openUpgradePanelInputKey;
		}
		set
		{
			if (value != _openUpgradePanelInputKey)
			{
				_openUpgradePanelInputKey = value;
				OnPropertyChangedWithValue(value, "OpenUpgradePanelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM OpenRecruitPanelInputKey
	{
		get
		{
			return _openRecruitPanelInputKey;
		}
		set
		{
			if (value != _openRecruitPanelInputKey)
			{
				_openRecruitPanelInputKey = value;
				OnPropertyChangedWithValue(value, "OpenRecruitPanelInputKey");
			}
		}
	}

	public PartyVM(PartyScreenLogic partyScreenLogic)
	{
		PartyScreenLogic = partyScreenLogic;
		_currentMode = PartyScreenHelper.GetActivePartyState()?.PartyScreenMode ?? PartyScreenHelper.PartyScreenMode.Normal;
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		OtherPartyTroops = new MBBindingList<PartyCharacterVM>();
		OtherPartyPrisoners = new MBBindingList<PartyCharacterVM>();
		MainPartyTroops = new MBBindingList<PartyCharacterVM>();
		MainPartyPrisoners = new MBBindingList<PartyCharacterVM>();
		UpgradePopUp = new PartyUpgradeTroopVM(this);
		RecruitPopUp = new PartyRecruitTroopVM(this);
		SelectedCharacter = new HeroViewModel();
		DoneHint = new HintViewModel();
		DenarHint = new HintViewModel();
		MoraleHint = new HintViewModel();
		SpeedHint = new BasicTooltipViewModel();
		TotalWageHint = new HintViewModel();
		FormationHint = new HintViewModel();
		PartyCharacterVM.ProcessCharacterLock = ProcessCharacterLock;
		PartyCharacterVM.OnFocus = OnFocusCharacter;
		PartyCharacterVM.OnShift = null;
		PartyCharacterVM.OnTransfer = OnTransferTroop;
		PartyCharacterVM.SetSelected = ExecuteSelectCharacterTuple;
		OtherPartyComposition = new PartyCompositionVM();
		MainPartyComposition = new PartyCompositionVM();
		CanChooseRoles = _currentMode == PartyScreenHelper.PartyScreenMode.Normal;
		if (PartyScreenLogic != null)
		{
			PartyScreenLogic.PartyGoldChange += OnPartyGoldChanged;
			PartyScreenLogic.PartyHorseChange += OnPartyHorseChanged;
			PartyScreenLogic.PartyInfluenceChange += OnPartyInfluenceChanged;
			PartyScreenLogic.PartyMoraleChange += OnPartyMoraleChanged;
			PartyScreenLogic.UpdateDelegate = Update;
			PartyScreenLogic.AfterReset += AfterReset;
			ShowQuestProgress = PartyScreenLogic.ShowProgressBar;
			if (ShowQuestProgress)
			{
				QuestProgressRequiredCount = PartyScreenLogic.GetCurrentQuestRequiredCount();
			}
			IsDoneDisabled = !PartyScreenLogic.IsDoneActive();
			DoneHint.HintText = new TextObject("{=!}" + PartyScreenLogic.DoneReasonString);
			IsCancelDisabled = !PartyScreenLogic.IsCancelActive();
			_initialSortType = (PartyScreenLogic.TroopSortType)_viewDataTracker.GetPartySortType();
			_initialSortAscending = _viewDataTracker.GetIsPartySortAscending();
			_initialLockedTroopIds = _viewDataTracker.GetPartyTroopLocks().ToList();
			_initialLockedPrisonerIds = _viewDataTracker.GetPartyPrisonerLocks().ToList();
			InitializeStaticInformation();
			InitializeTroopLists();
			RefreshPartyInformation();
		}
		UpdateTroopManagerPopUpCounts();
		PartyTradeVM.RemoveZeroCounts += ExecuteRemoveZeroCounts;
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		_viewDataTracker.ClearPartyNotification();
		IsAnyPopUpOpen = false;
		OtherPartySortController = new PartySortControllerVM(PartyScreenLogic.PartyRosterSide.Left, OnSortTroops);
		MainPartySortController = new PartySortControllerVM(PartyScreenLogic.PartyRosterSide.Right, OnSortTroops);
		MainPartySortController.SortWith(_initialSortType, _initialSortAscending);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset"));
		LevelHint = new HintViewModel(GameTexts.FindText("str_level_tag"));
		TitleLbl = GameTexts.FindText("str_party").ToString();
		OtherPartyAccompanyingLbl = GameTexts.FindText("str_party_list_tag_attached_groups").ToString();
		MoraleHint.HintText = GameTexts.FindText("str_party_morale");
		TotalWageHint.HintText = GameTexts.FindText("str_weekly_wage");
		TalkLbl = GameTexts.FindText("str_talk_button").ToString();
		InfoLbl = GameTexts.FindText("str_info").ToString();
		CancelLbl = GameTexts.FindText("str_cancel").ToString();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		FormationHint.HintText = GameTexts.FindText("str_party_formation");
		TroopsLabel = GameTexts.FindText("str_troops_group").ToString();
		PrisonersLabel = GameTexts.FindText("str_party_category_prisoners_tooltip").ToString();
		TransferAllMainTroopsHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllMainTroopsKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		TransferAllMainPrisonersHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllMainPrisonersKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		TransferAllOtherTroopsHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllOtherTroopsKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		TransferAllOtherPrisonersHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllOtherPrisonersKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		WageHint = new HintViewModel(GameTexts.FindText("str_wage"));
		UpgradePopUp.RefreshValues();
		RecruitPopUp.RefreshValues();
		OtherPartyTroops?.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
		{
			x.RefreshValues();
		});
		OtherPartyPrisoners?.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
		{
			x.RefreshValues();
		});
		MainPartyTroops?.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
		{
			x.RefreshValues();
		});
		MainPartyPrisoners?.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
		{
			x.RefreshValues();
		});
		UpdateLabelHints();
		OnPartyGoldChanged();
		if (PartyScreenLogic != null)
		{
			InitializeStaticInformation();
		}
	}

	private void OnPartyGoldChanged()
	{
		MBTextManager.SetTextVariable("PAY_OR_GET", (PartyScreenLogic.CurrentData.PartyGoldChangeAmount > 0) ? 1 : 0);
		MBTextManager.SetTextVariable("TRADE_AMOUNT", TaleWorlds.Library.MathF.Abs(PartyScreenLogic.CurrentData.PartyGoldChangeAmount));
		GoldChangeText = ((PartyScreenLogic.CurrentData.PartyGoldChangeAmount == 0) ? "" : GameTexts.FindText("str_inventory_trade_label").ToString());
	}

	private void OnPartyMoraleChanged()
	{
		MBTextManager.SetTextVariable("PAY_OR_GET", (PartyScreenLogic.CurrentData.PartyMoraleChangeAmount > 0) ? 1 : 0);
		MBTextManager.SetTextVariable("MORALE_ICON", "{=!}<img src=\"General\\Icons\\Morale@2x\" extend=\"4\">");
		MBTextManager.SetTextVariable("TRADE_AMOUNT", TaleWorlds.Library.MathF.Abs(PartyScreenLogic.CurrentData.PartyMoraleChangeAmount));
		MoraleChangeText = ((PartyScreenLogic.CurrentData.PartyMoraleChangeAmount == 0) ? "" : GameTexts.FindText("str_party_morale_label").ToString());
	}

	private void OnPartyInfluenceChanged()
	{
		int num = PartyScreenLogic.CurrentData.PartyInfluenceChangeAmount.Item1 + PartyScreenLogic.CurrentData.PartyInfluenceChangeAmount.Item2 + PartyScreenLogic.CurrentData.PartyInfluenceChangeAmount.Item3;
		MBTextManager.SetTextVariable("PAY_OR_GET", (num > 0) ? 1 : 0);
		MBTextManager.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
		MBTextManager.SetTextVariable("TRADE_AMOUNT", TaleWorlds.Library.MathF.Abs(num));
		InfluenceChangeText = ((num == 0) ? "" : GameTexts.FindText("str_party_influence_label").ToString());
	}

	private void OnPartyHorseChanged()
	{
		MBTextManager.SetTextVariable("IS_PLURAL", (PartyScreenLogic.CurrentData.PartyHorseChangeAmount > 1) ? 1 : 0);
		MBTextManager.SetTextVariable("TRADE_AMOUNT", TaleWorlds.Library.MathF.Abs(PartyScreenLogic.CurrentData.PartyHorseChangeAmount));
		HorseChangeText = ((PartyScreenLogic.CurrentData.PartyHorseChangeAmount == 0) ? "" : GameTexts.FindText("str_party_horse_label").ToString());
	}

	private void InitializeTroopLists()
	{
		ArePrisonersRelevantOnCurrentMode = _currentMode != PartyScreenHelper.PartyScreenMode.TroopsManage && _currentMode != PartyScreenHelper.PartyScreenMode.QuestTroopManage;
		AreMembersRelevantOnCurrentMode = _currentMode != PartyScreenHelper.PartyScreenMode.PrisonerManage && _currentMode != PartyScreenHelper.PartyScreenMode.Ransom;
		_lockedTroopIds = _viewDataTracker.GetPartyTroopLocks().ToList();
		_lockedPrisonerIds = _viewDataTracker.GetPartyPrisonerLocks().ToList();
		InitializePartyList(MainPartyPrisoners, PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner);
		InitializePartyList(OtherPartyPrisoners, PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner);
		InitializePartyList(MainPartyTroops, PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member);
		InitializePartyList(OtherPartyTroops, PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member);
		if (MainPartyTroops.Count > 0)
		{
			CurrentCharacter = MainPartyTroops[0];
		}
		else if (OtherPartyTroops.Count > 0)
		{
			CurrentCharacter = OtherPartyTroops[0];
		}
		RefreshTopInformation();
		OtherPartyComposition.RefreshCounts(OtherPartyTroops);
		MainPartyComposition.RefreshCounts(MainPartyTroops);
	}

	private void RefreshTopInformation()
	{
		MainPartyTotalWeeklyCostLbl = MobileParty.MainParty.TotalWage.ToString();
		MainPartyTotalGoldLbl = Hero.MainHero.Gold.ToString();
		MainPartyTotalMoraleLbl = ((int)MobileParty.MainParty.Morale).ToString("##.0");
		MainPartyTotalSpeedLbl = CampaignUIHelper.FloatToString(MobileParty.MainParty.Speed);
		UpdateLabelHints();
	}

	private void UpdateLabelHints()
	{
		SpeedHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartySpeedTooltip(considerArmySpeed: false));
		if (PartyScreenLogic.RightOwnerParty != null)
		{
			MainPartyTroopSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyTroopSizeLimitTooltip(PartyScreenLogic.RightOwnerParty));
			MainPartyPrisonerSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyPrisonerSizeLimitTooltip(PartyScreenLogic.RightOwnerParty));
		}
		if (PartyScreenLogic.LeftOwnerParty != null)
		{
			OtherPartyTroopSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyTroopSizeLimitTooltip(PartyScreenLogic.LeftOwnerParty));
			OtherPartyPrisonerSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyPrisonerSizeLimitTooltip(PartyScreenLogic.LeftOwnerParty));
		}
		UsedHorsesHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetUsedHorsesTooltip(PartyScreenLogic.CurrentData.UsedUpgradeHorsesHistory));
		DenarHint.HintText = GameTexts.FindText("str_gold");
	}

	private void InitializeStaticInformation()
	{
		if (PartyScreenLogic.RightOwnerParty != null)
		{
			MainPartyNameLbl = PartyScreenLogic.RightOwnerParty.Name.ToString();
		}
		else
		{
			MainPartyNameLbl = ((!TextObject.IsNullOrEmpty(PartyScreenLogic.RightPartyName)) ? PartyScreenLogic.RightPartyName.ToString() : string.Empty);
		}
		MBTextManager.SetTextVariable("PARTY_NAME", MobileParty.MainParty.Name);
		if (PartyScreenLogic.LeftOwnerParty != null)
		{
			OtherPartyNameLbl = PartyScreenLogic.LeftOwnerParty.Name.ToString();
		}
		else
		{
			OtherPartyNameLbl = ((!TextObject.IsNullOrEmpty(PartyScreenLogic.LeftPartyName)) ? PartyScreenLogic.LeftPartyName.ToString() : GameTexts.FindText("str_dismiss").ToString());
		}
		if (PartyScreenLogic.Header == null || string.IsNullOrEmpty(PartyScreenLogic.Header.ToString()))
		{
			HeaderLbl = GameTexts.FindText("str_party").ToString();
		}
		else
		{
			HeaderLbl = PartyScreenLogic.Header.ToString();
		}
	}

	public void SetSelectedCharacter(PartyCharacterVM troop)
	{
		CurrentCharacter = troop;
		CurrentCharacter.UpdateRecruitable();
	}

	public void ExecuteSelectCharacterTuple(PartyCharacterVM troop)
	{
		if (troop == null || troop.IsSelected)
		{
			foreach (PartyCharacterVM allCharacter in GetAllCharacters(includePrisoners: true))
			{
				allCharacter.IsSelected = false;
			}
		}
		else
		{
			foreach (PartyCharacterVM allCharacter2 in GetAllCharacters(includePrisoners: true))
			{
				allCharacter2.IsSelected = allCharacter2.Character.Equals(troop.Character) && allCharacter2.IsPrisoner == troop.IsPrisoner;
				if (allCharacter2.IsSelected)
				{
					ScrollCharacterId = allCharacter2.Character.StringId;
					ScrollToCharacter = true;
					IsScrollTargetPrisoner = allCharacter2.IsPrisoner;
				}
			}
		}
		if (troop != null)
		{
			SetSelectedCharacter(troop);
		}
	}

	public void ExecuteClearSelectedCharacterTuple()
	{
		ExecuteSelectCharacterTuple(null);
	}

	private IEnumerable<PartyCharacterVM> GetAllCharacters(bool includePrisoners)
	{
		foreach (PartyCharacterVM otherPartyTroop in OtherPartyTroops)
		{
			yield return otherPartyTroop;
		}
		foreach (PartyCharacterVM mainPartyTroop in MainPartyTroops)
		{
			yield return mainPartyTroop;
		}
		if (!includePrisoners)
		{
			yield break;
		}
		foreach (PartyCharacterVM prisonerCharacter in GetPrisonerCharacters())
		{
			yield return prisonerCharacter;
		}
	}

	private IEnumerable<PartyCharacterVM> GetPrisonerCharacters()
	{
		foreach (PartyCharacterVM otherPartyPrisoner in OtherPartyPrisoners)
		{
			yield return otherPartyPrisoner;
		}
		foreach (PartyCharacterVM mainPartyPrisoner in MainPartyPrisoners)
		{
			yield return mainPartyPrisoner;
		}
	}

	private void ProcessCharacterLock(PartyCharacterVM troop, bool isLocked)
	{
		List<string> list = (troop.IsPrisoner ? _lockedPrisonerIds : _lockedTroopIds);
		if (isLocked && !list.Contains(troop.StringId))
		{
			list.Add(troop.StringId);
		}
		else if (!isLocked && list.Contains(troop.StringId))
		{
			list.Remove(troop.StringId);
		}
	}

	private PartyCompositionVM GetCompositionForList(MBBindingList<PartyCharacterVM> list)
	{
		if (list == MainPartyTroops)
		{
			return MainPartyComposition;
		}
		if (list == OtherPartyTroops)
		{
			return OtherPartyComposition;
		}
		return null;
	}

	private void SaveSortState()
	{
		_viewDataTracker.SetPartySortType((int)PartyScreenLogic.ActiveMainPartySortType);
		_viewDataTracker.SetIsPartySortAscending(PartyScreenLogic.IsMainPartySortAscending);
	}

	private void SaveCharacterLockStates()
	{
		_viewDataTracker.SetPartyTroopLocks(_lockedTroopIds);
		_viewDataTracker.SetPartyPrisonerLocks(_lockedPrisonerIds);
	}

	private bool IsTroopLocked(TroopRosterElement troop, bool isPrisoner)
	{
		if (!isPrisoner)
		{
			return _lockedTroopIds.Contains(troop.Character.StringId);
		}
		return _lockedPrisonerIds.Contains(troop.Character.StringId);
	}

	private void UpdateCurrentCharacterFormationClass(SelectorVM<SelectorItemVM> s)
	{
		Campaign.Current.SetPlayerFormationPreference(CurrentCharacter.Character, (FormationClass)s.SelectedIndex);
	}

	private void InitializePartyList(MBBindingList<PartyCharacterVM> partyList, PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type)
	{
		partyList.Clear();
		TroopRoster roster = PartyScreenLogic.GetRoster(side, type);
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character == null)
			{
				TaleWorlds.Library.Debug.FailedAssert("Invalid TroopRosterElement found in InitializePartyList!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "InitializePartyList", 508);
				continue;
			}
			PartyCharacterVM partyCharacterVM = new PartyCharacterVM(PartyScreenLogic, this, roster, roster.FindIndexOfTroop(elementCopyAtIndex.Character), type, side, PartyScreenLogic.IsTroopTransferable(type, elementCopyAtIndex.Character, (int)side));
			partyList.Add(partyCharacterVM);
			partyCharacterVM.ThrowOnPropertyChanged();
			partyCharacterVM.IsLocked = partyCharacterVM.Side == PartyScreenLogic.PartyRosterSide.Right && IsTroopLocked(partyCharacterVM.Troop, partyCharacterVM.IsPrisoner);
		}
	}

	public void ExecuteTransferWithParameters(PartyCharacterVM party, int index, string targetTag)
	{
		PartyScreenLogic.PartyRosterSide side = party.Side;
		PartyScreenLogic.PartyRosterSide partyRosterSide = (targetTag.StartsWith("MainParty") ? PartyScreenLogic.PartyRosterSide.Right : PartyScreenLogic.PartyRosterSide.Left);
		if (targetTag == "MainParty")
		{
			index = -1;
		}
		else if (targetTag.EndsWith("Prisoners") != party.IsPrisoner)
		{
			index = -1;
		}
		if (side != partyRosterSide && party.IsTroopTransferrable)
		{
			OnTransferTroop(party, index, party.Number, party.Side);
			ExecuteRemoveZeroCounts();
		}
		else if (side == partyRosterSide)
		{
			OnShiftTroop(party, index);
		}
	}

	private void OnTransferTroop(PartyCharacterVM troop, int newIndex, int transferAmount, PartyScreenLogic.PartyRosterSide fromSide)
	{
		if (troop.Side == PartyScreenLogic.PartyRosterSide.None || fromSide == PartyScreenLogic.PartyRosterSide.None)
		{
			return;
		}
		_ = troop.Side;
		SetSelectedCharacter(troop);
		PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
		if (newIndex == -1)
		{
			newIndex = PartyScreenLogic.GetIndexToInsertTroop(1 - troop.Side, troop.Type, troop.Troop);
		}
		else
		{
			switch (fromSide)
			{
			case PartyScreenLogic.PartyRosterSide.Left:
				MainPartySortController.SelectSortType(PartyScreenLogic.TroopSortType.Custom);
				break;
			case PartyScreenLogic.PartyRosterSide.Right:
				OtherPartySortController.SelectSortType(PartyScreenLogic.TroopSortType.Custom);
				break;
			}
		}
		if (transferAmount > 0)
		{
			int numberOfHealthyTroopNumberForSide = GetNumberOfHealthyTroopNumberForSide(troop.Troop.Character, fromSide, troop.Type);
			int numberOfWoundedTroopNumberForSide = GetNumberOfWoundedTroopNumberForSide(troop.Troop.Character, fromSide, troop.Type);
			if ((PartyScreenLogic.TransferHealthiesGetWoundedsFirst && fromSide == PartyScreenLogic.PartyRosterSide.Right) || (!PartyScreenLogic.TransferHealthiesGetWoundedsFirst && fromSide == PartyScreenLogic.PartyRosterSide.Left))
			{
				int num = ((transferAmount > numberOfHealthyTroopNumberForSide) ? (transferAmount - numberOfHealthyTroopNumberForSide) : 0);
				num = (int)TaleWorlds.Library.MathF.Clamp(num, 0f, numberOfWoundedTroopNumberForSide);
				partyCommand.FillForTransferTroop(fromSide, troop.Type, troop.Character, transferAmount, num, newIndex);
			}
			else
			{
				partyCommand.FillForTransferTroop(fromSide, troop.Type, troop.Character, transferAmount, (numberOfWoundedTroopNumberForSide >= transferAmount) ? transferAmount : numberOfWoundedTroopNumberForSide, newIndex);
			}
			PartyScreenLogic.AddCommand(partyCommand);
		}
	}

	private void OnFocusCharacter(PartyCharacterVM character)
	{
		CurrentFocusedCharacter = character;
	}

	private int GetNumberOfWoundedTroopNumberForSide(CharacterObject character, PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.TroopType troopType)
	{
		return FindCharacterVM(character, fromSide, troopType)?.WoundedCount ?? 0;
	}

	private int GetNumberOfHealthyTroopNumberForSide(CharacterObject character, PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.TroopType troopType)
	{
		PartyCharacterVM partyCharacterVM = FindCharacterVM(character, fromSide, troopType);
		return (partyCharacterVM?.Troop.Number - partyCharacterVM?.Troop.WoundedNumber) ?? 0;
	}

	private void OnSortTroops(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopSortType sortType, bool isAscending)
	{
		PartyScreenLogic.TroopSortType activeSortTypeForSide = PartyScreenLogic.GetActiveSortTypeForSide(side);
		bool isAscendingSortForSide = PartyScreenLogic.GetIsAscendingSortForSide(side);
		if (activeSortTypeForSide != sortType || isAscendingSortForSide != isAscending)
		{
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			partyCommand.FillForSortTroops(side, sortType, isAscending);
			PartyScreenLogic.AddCommand(partyCommand);
		}
	}

	private PartyCharacterVM FindCharacterVM(CharacterObject character, PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType troopType)
	{
		MBBindingList<PartyCharacterVM> mBBindingList = null;
		switch (side)
		{
		case PartyScreenLogic.PartyRosterSide.Left:
			mBBindingList = ((troopType == PartyScreenLogic.TroopType.Member) ? OtherPartyTroops : OtherPartyPrisoners);
			break;
		case PartyScreenLogic.PartyRosterSide.Right:
			mBBindingList = ((troopType == PartyScreenLogic.TroopType.Member) ? MainPartyTroops : MainPartyPrisoners);
			break;
		}
		return mBBindingList?.FirstOrDefault((PartyCharacterVM x) => x.Troop.Character == character);
	}

	private void UpdateAllTradeDatasOfCharacter(CharacterObject character)
	{
		OtherPartyPrisoners?.FirstOrDefault((PartyCharacterVM x) => x.Character == character)?.UpdateTradeData();
		OtherPartyTroops?.FirstOrDefault((PartyCharacterVM x) => x.Character == character)?.UpdateTradeData();
		MainPartyPrisoners?.FirstOrDefault((PartyCharacterVM x) => x.Character == character)?.UpdateTradeData();
		MainPartyTroops?.FirstOrDefault((PartyCharacterVM x) => x.Character == character)?.UpdateTradeData();
	}

	private void OnShiftTroop(PartyCharacterVM troop, int newIndex)
	{
		if (troop.Side != PartyScreenLogic.PartyRosterSide.None)
		{
			SetSelectedCharacter(troop);
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			partyCommand.FillForShiftTroop(troop.Side, troop.Type, troop.Character, newIndex);
			PartyScreenLogic.AddCommand(partyCommand);
		}
	}

	private void Update(PartyScreenLogic.PartyCommand command)
	{
		switch (command.Code)
		{
		case PartyScreenLogic.PartyCommandCode.TransferTroop:
		case PartyScreenLogic.PartyCommandCode.TransferPartyLeaderTroop:
		case PartyScreenLogic.PartyCommandCode.TransferTroopToLeaderSlot:
			TransferTroop(command);
			break;
		case PartyScreenLogic.PartyCommandCode.UpgradeTroop:
			UpgradeTroop(command);
			RefreshTroopsUpgradeable();
			UpgradePopUp.OnTroopUpgraded();
			break;
		case PartyScreenLogic.PartyCommandCode.ShiftTroop:
			ShiftTroop(command);
			break;
		case PartyScreenLogic.PartyCommandCode.RecruitTroop:
		{
			PartyCharacterVM currentCharacter = CurrentCharacter;
			RecruitTroop(command);
			RecruitPopUp.OnTroopRecruited(currentCharacter);
			break;
		}
		case PartyScreenLogic.PartyCommandCode.ExecuteTroop:
			ExecuteTroop(command);
			break;
		case PartyScreenLogic.PartyCommandCode.TransferAllTroops:
			TransferAllTroops(command);
			break;
		case PartyScreenLogic.PartyCommandCode.SortTroops:
			SortTroops(command);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		RefreshTopInformation();
		UpdateTroopManagerPopUpCounts();
		RefreshPrisonersRecruitable();
		IsDoneDisabled = !PartyScreenLogic.IsDoneActive();
		DoneHint.HintText = new TextObject("{=!}" + PartyScreenLogic.DoneReasonString);
		IsCancelDisabled = !PartyScreenLogic.IsCancelActive();
	}

	private MBBindingList<PartyCharacterVM> GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide rosterSide, PartyScreenLogic.TroopType type)
	{
		MBBindingList<PartyCharacterVM> result = null;
		switch (type)
		{
		case PartyScreenLogic.TroopType.Member:
			switch (rosterSide)
			{
			case PartyScreenLogic.PartyRosterSide.Left:
				result = OtherPartyTroops;
				break;
			case PartyScreenLogic.PartyRosterSide.Right:
				result = MainPartyTroops;
				break;
			}
			break;
		case PartyScreenLogic.TroopType.Prisoner:
			switch (rosterSide)
			{
			case PartyScreenLogic.PartyRosterSide.Left:
				result = OtherPartyPrisoners;
				break;
			case PartyScreenLogic.PartyRosterSide.Right:
				result = MainPartyPrisoners;
				break;
			}
			break;
		}
		return result;
	}

	private void AfterReset(PartyScreenLogic partyScreenLogic, bool fromCancel)
	{
		_lockedTroopIds.Clear();
		for (int i = 0; i < _initialLockedTroopIds.Count; i++)
		{
			_lockedTroopIds.Add(_initialLockedTroopIds[i]);
		}
		_lockedPrisonerIds.Clear();
		for (int j = 0; j < _initialLockedPrisonerIds.Count; j++)
		{
			_lockedPrisonerIds.Add(_initialLockedPrisonerIds[j]);
		}
		SaveCharacterLockStates();
		if (!fromCancel)
		{
			InitializeTroopLists();
			RefreshPartyInformation();
			OnPartyGoldChanged();
			OnPartyMoraleChanged();
			OnPartyHorseChanged();
			OnPartyInfluenceChanged();
			UpdateTroopManagerPopUpCounts();
			MainPartyComposition.RefreshCounts(MainPartyTroops);
			OtherPartyComposition.RefreshCounts(OtherPartyTroops);
			IsDoneDisabled = !partyScreenLogic.IsDoneActive();
			DoneHint.HintText = new TextObject("{=!}" + PartyScreenLogic.DoneReasonString);
			IsCancelDisabled = !partyScreenLogic.IsCancelActive();
		}
		MainPartySortController.SortWith(_initialSortType, _initialSortAscending);
	}

	private void TransferTroop(PartyScreenLogic.PartyCommand command)
	{
		PartyScreenLogic.PartyRosterSide partyRosterSide = ((command.RosterSide == PartyScreenLogic.PartyRosterSide.Left) ? PartyScreenLogic.PartyRosterSide.Right : PartyScreenLogic.PartyRosterSide.Left);
		MBBindingList<PartyCharacterVM> partyCharacterVMList = GetPartyCharacterVMList(command.RosterSide, command.Type);
		MBBindingList<PartyCharacterVM> partyCharacterVMList2 = GetPartyCharacterVMList(partyRosterSide, command.Type);
		TroopRoster troopRoster = null;
		TroopRoster troopRoster2 = null;
		int index = 0;
		int index2 = 0;
		switch (command.Type)
		{
		case PartyScreenLogic.TroopType.Member:
			troopRoster = PartyScreenLogic.MemberRosters[(uint)partyRosterSide];
			index = PartyScreenLogic.MemberRosters[(uint)partyRosterSide].FindIndexOfTroop(CurrentCharacter.Character);
			troopRoster2 = PartyScreenLogic.MemberRosters[(uint)command.RosterSide];
			index2 = PartyScreenLogic.MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(CurrentCharacter.Character);
			break;
		case PartyScreenLogic.TroopType.Prisoner:
			troopRoster = PartyScreenLogic.PrisonerRosters[(uint)partyRosterSide];
			index = PartyScreenLogic.PrisonerRosters[(uint)partyRosterSide].FindIndexOfTroop(CurrentCharacter.Character);
			troopRoster2 = PartyScreenLogic.PrisonerRosters[(uint)command.RosterSide];
			index2 = PartyScreenLogic.PrisonerRosters[(uint)command.RosterSide].FindIndexOfTroop(CurrentCharacter.Character);
			break;
		}
		PartyCharacterVM partyCharacterVM = partyCharacterVMList.FirstOrDefault((PartyCharacterVM q) => q.Character == CurrentCharacter.Character);
		if (troopRoster2.FindIndexOfTroop(CurrentCharacter.Character) != -1 && partyCharacterVM != null)
		{
			partyCharacterVM.Troop = troopRoster2.GetElementCopyAtIndex(index2);
			partyCharacterVM.ThrowOnPropertyChanged();
			partyCharacterVM.UpdateTradeData();
		}
		int num = -1;
		for (int num2 = 0; num2 < partyCharacterVMList2.Count; num2++)
		{
			if (partyCharacterVMList2[num2].Character == command.Character)
			{
				num = num2;
				break;
			}
		}
		if (num >= 0)
		{
			PartyCharacterVM partyCharacterVM2 = partyCharacterVMList2[num];
			partyCharacterVM2.Troop = troopRoster.GetElementCopyAtIndex(index);
			partyCharacterVM2.ThrowOnPropertyChanged();
			if (!partyCharacterVMList.Contains(CurrentCharacter))
			{
				SetSelectedCharacter(partyCharacterVM2);
			}
			partyCharacterVM2.UpdateTradeData();
			if (partyCharacterVM2.IsSelected)
			{
				ScrollCharacterId = partyCharacterVM2.Character.StringId;
				IsScrollTargetPrisoner = partyCharacterVM2.IsPrisoner;
				ScrollToCharacter = true;
			}
			int num3 = command.Index;
			if (num3 != -1)
			{
				if (num3 == partyCharacterVMList2.Count)
				{
					num3 = partyCharacterVMList2.Count - 1;
				}
				partyCharacterVMList2.RemoveAt(num);
				partyCharacterVMList2.Insert(num3, partyCharacterVM2);
			}
		}
		else
		{
			PartyCharacterVM partyCharacterVM3 = new PartyCharacterVM(PartyScreenLogic, this, troopRoster, index, command.Type, partyRosterSide, PartyScreenLogic.IsTroopTransferable(command.Type, troopRoster.GetCharacterAtIndex(index), (int)partyRosterSide));
			if (command.Index != -1)
			{
				partyCharacterVMList2.Insert(command.Index, partyCharacterVM3);
			}
			else
			{
				partyCharacterVMList2.Add(partyCharacterVM3);
			}
			if (!partyCharacterVMList.Contains(CurrentCharacter))
			{
				SetSelectedCharacter(partyCharacterVM3);
			}
			partyCharacterVM3.IsLocked = partyCharacterVM3.Side == PartyScreenLogic.PartyRosterSide.Right && IsTroopLocked(partyCharacterVM3.Troop, partyCharacterVM3.IsPrisoner);
			partyCharacterVM3.IsSelected = partyCharacterVM?.IsSelected ?? false;
			if (partyCharacterVM3.IsSelected)
			{
				ScrollCharacterId = partyCharacterVM3.Character.StringId;
				IsScrollTargetPrisoner = partyCharacterVM3.IsPrisoner;
				ScrollToCharacter = true;
			}
		}
		CurrentCharacter = FindCharacterVM(command.Character, partyRosterSide, command.Type);
		GetCompositionForList(partyCharacterVMList)?.OnTroopRemoved(command.Character.DefaultFormationClass, command.TotalNumber);
		GetCompositionForList(partyCharacterVMList2)?.OnTroopAdded(command.Character.DefaultFormationClass, command.TotalNumber);
		CurrentCharacter.UpdateTradeData();
		CurrentCharacter.OnTransferred();
		CurrentCharacter.ThrowOnPropertyChanged();
		RefreshTopInformation();
		RefreshPartyInformation();
		Game.Current.EventManager.TriggerEvent(new PlayerMoveTroopEvent(command.Character, command.RosterSide, (PartyScreenLogic.PartyRosterSide)((int)(command.RosterSide + 1) % 2), command.TotalNumber, command.Type == PartyScreenLogic.TroopType.Prisoner));
	}

	private void ShiftTroop(PartyScreenLogic.PartyCommand command)
	{
		MBBindingList<PartyCharacterVM> partyCharacterVMList = GetPartyCharacterVMList(command.RosterSide, command.Type);
		if (command.Index >= 0)
		{
			PartyCharacterVM currentCharacter = CurrentCharacter;
			int num = partyCharacterVMList.IndexOf(CurrentCharacter);
			int num2 = -1;
			partyCharacterVMList.Remove(CurrentCharacter);
			if (partyCharacterVMList.Count < command.Index)
			{
				partyCharacterVMList.Add(currentCharacter);
			}
			else
			{
				num2 = ((num < command.Index) ? (command.Index - 1) : command.Index);
				partyCharacterVMList.Insert(num2, currentCharacter);
			}
			SetSelectedCharacter(currentCharacter);
			if (num != num2)
			{
				bool isAscendingSortForSide = PartyScreenLogic.GetIsAscendingSortForSide(command.RosterSide);
				OnSortTroops(command.RosterSide, PartyScreenLogic.TroopSortType.Custom, isAscendingSortForSide);
			}
			CurrentCharacter.ThrowOnPropertyChanged();
			RefreshTopInformation();
			RefreshPartyInformation();
		}
	}

	public void OnUpgradePopUpClosed(bool isCancelled)
	{
		if (!isCancelled)
		{
			UpdateTroopManagerPopUpCounts();
		}
		Game.Current.EventManager.TriggerEvent(new PlayerToggledUpgradePopupEvent(isOpened: false));
		IsAnyPopUpOpen = RecruitPopUp?.IsOpen ?? false;
	}

	public void OnRecruitPopUpClosed(bool isCancelled)
	{
		if (!isCancelled)
		{
			UpdateTroopManagerPopUpCounts();
		}
		IsAnyPopUpOpen = UpgradePopUp?.IsOpen ?? false;
	}

	private void UpdateTroopManagerPopUpCounts()
	{
		if (!UpgradePopUp.IsOpen && !RecruitPopUp.IsOpen)
		{
			RecruitableTroopCount = 0;
			UpgradableTroopCount = 0;
			MainPartyPrisoners.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
			{
				RecruitableTroopCount += x.NumOfRecruitablePrisoners;
			});
			MainPartyTroops.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
			{
				UpgradableTroopCount += x.NumOfUpgradeableTroops;
			});
			IsRecruitPopUpDisabled = !ArePrisonersRelevantOnCurrentMode || RecruitableTroopCount == 0 || PartyScreenLogic.IsTroopUpgradesDisabled;
			IsUpgradePopUpDisabled = !AreMembersRelevantOnCurrentMode || UpgradableTroopCount == 0 || PartyScreenLogic.IsTroopUpgradesDisabled;
			RecruitPopUp.UpdateOpenButtonHint(IsRecruitPopUpDisabled, !ArePrisonersRelevantOnCurrentMode, PartyScreenLogic.IsTroopUpgradesDisabled);
			UpgradePopUp.UpdateOpenButtonHint(IsUpgradePopUpDisabled, !AreMembersRelevantOnCurrentMode, PartyScreenLogic.IsTroopUpgradesDisabled);
		}
	}

	private void UpgradeTroop(PartyScreenLogic.PartyCommand command)
	{
		int index = PartyScreenLogic.MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(command.Character.UpgradeTargets[command.UpgradeTarget]);
		PartyCharacterVM newCharacter = new PartyCharacterVM(PartyScreenLogic, this, PartyScreenLogic.MemberRosters[(uint)command.RosterSide], index, command.Type, command.RosterSide, PartyScreenLogic.IsTroopTransferable(command.Type, PartyScreenLogic.MemberRosters[(uint)command.RosterSide].GetCharacterAtIndex(index), (int)command.RosterSide));
		newCharacter.IsLocked = IsTroopLocked(newCharacter.Troop, isPrisoner: false);
		MBBindingList<PartyCharacterVM> partyCharacterVMList = GetPartyCharacterVMList(command.RosterSide, command.Type);
		if (partyCharacterVMList.Contains(newCharacter))
		{
			PartyCharacterVM partyCharacterVM = partyCharacterVMList.First((PartyCharacterVM character) => character.Equals(newCharacter));
			partyCharacterVM.Troop = newCharacter.Troop;
			partyCharacterVM.ThrowOnPropertyChanged();
		}
		else
		{
			if (command.Index != -1)
			{
				partyCharacterVMList.Insert(command.Index, newCharacter);
			}
			else
			{
				partyCharacterVMList.Add(newCharacter);
			}
			newCharacter.ThrowOnPropertyChanged();
		}
		int num = -1;
		if (command.Type == PartyScreenLogic.TroopType.Member)
		{
			num = PartyScreenLogic.MemberRosters[(uint)CurrentCharacter.Side].FindIndexOfTroop(CurrentCharacter.Character);
			if (num > 0)
			{
				_currentCharacter.Troop = PartyScreenLogic.MemberRosters[(uint)CurrentCharacter.Side].GetElementCopyAtIndex(num);
			}
		}
		else if (command.Type == PartyScreenLogic.TroopType.Prisoner)
		{
			num = PartyScreenLogic.MemberRosters[(uint)CurrentCharacter.Side].FindIndexOfTroop(CurrentCharacter.Character);
			if (num > 0)
			{
				_currentCharacter.Troop = PartyScreenLogic.PrisonerRosters[(uint)CurrentCharacter.Side].GetElementCopyAtIndex(num);
			}
		}
		PartyCharacterVM currentCharacter = CurrentCharacter;
		if (num < 0)
		{
			UpgradePopUp.OnRanOutTroop(CurrentCharacter);
			partyCharacterVMList.Remove(CurrentCharacter);
			CurrentCharacter = newCharacter;
			MBInformationManager.HideInformations();
		}
		else
		{
			CurrentCharacter.InitializeUpgrades();
			CurrentCharacter.ThrowOnPropertyChanged();
		}
		GetCompositionForList(partyCharacterVMList)?.OnTroopRemoved(command.Character.DefaultFormationClass, command.TotalNumber);
		GetCompositionForList(partyCharacterVMList)?.OnTroopAdded(newCharacter.Character.DefaultFormationClass, command.TotalNumber);
		UpdateAllTradeDatasOfCharacter(currentCharacter?.Character);
		UpdateAllTradeDatasOfCharacter(newCharacter?.Character);
		Game.Current.EventManager.TriggerEvent(new PlayerRequestUpgradeTroopEvent(command.Character, command.Character.UpgradeTargets[command.UpgradeTarget], command.TotalNumber));
		RefreshTopInformation();
	}

	private void RecruitTroop(PartyScreenLogic.PartyCommand command)
	{
		int index = PartyScreenLogic.MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(command.Character);
		PartyCharacterVM newCharacter = new PartyCharacterVM(PartyScreenLogic, this, PartyScreenLogic.MemberRosters[(uint)command.RosterSide], index, PartyScreenLogic.TroopType.Member, command.RosterSide, PartyScreenLogic.IsTroopTransferable(command.Type, PartyScreenLogic.MemberRosters[(uint)command.RosterSide].GetCharacterAtIndex(index), (int)command.RosterSide));
		newCharacter.IsLocked = IsTroopLocked(newCharacter.Troop, isPrisoner: false);
		MBBindingList<PartyCharacterVM> partyCharacterVMList = GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Member);
		MBBindingList<PartyCharacterVM> partyCharacterVMList2 = GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Prisoner);
		if (partyCharacterVMList.Contains(newCharacter))
		{
			PartyCharacterVM partyCharacterVM = partyCharacterVMList.First((PartyCharacterVM character) => character.Equals(newCharacter));
			partyCharacterVM.Troop = newCharacter.Troop;
			partyCharacterVM.ThrowOnPropertyChanged();
		}
		else
		{
			if (command.Index != -1)
			{
				partyCharacterVMList.Insert(command.Index, newCharacter);
			}
			else
			{
				partyCharacterVMList.Add(newCharacter);
			}
			newCharacter.ThrowOnPropertyChanged();
		}
		int num = -1;
		if (command.Type == PartyScreenLogic.TroopType.Prisoner)
		{
			num = PartyScreenLogic.PrisonerRosters[(uint)CurrentCharacter.Side].FindIndexOfTroop(CurrentCharacter.Character);
			if (num >= 0)
			{
				_currentCharacter.Troop = PartyScreenLogic.PrisonerRosters[(uint)CurrentCharacter.Side].GetElementCopyAtIndex(num);
			}
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Players can only recruit prisoners", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "RecruitTroop", 1132);
		}
		if (num < 0)
		{
			partyCharacterVMList2.Remove(CurrentCharacter);
			CurrentCharacter = newCharacter;
			MBInformationManager.HideInformations();
		}
		else
		{
			CurrentCharacter.InitializeUpgrades();
			CurrentCharacter.ThrowOnPropertyChanged();
		}
		GetCompositionForList(partyCharacterVMList)?.OnTroopAdded(command.Character.DefaultFormationClass, command.TotalNumber);
		CurrentCharacter?.UpdateTradeData();
		RefreshTopInformation();
		RefreshPartyInformation();
	}

	private void ExecuteTroop(PartyScreenLogic.PartyCommand command)
	{
		PartyScreenLogic.MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(command.Character);
		MBBindingList<PartyCharacterVM> partyCharacterVMList = GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Member);
		MBBindingList<PartyCharacterVM> partyCharacterVMList2 = GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Prisoner);
		int num = -1;
		if (command.Type == PartyScreenLogic.TroopType.Prisoner)
		{
			num = PartyScreenLogic.PrisonerRosters[(uint)CurrentCharacter.Side].FindIndexOfTroop(CurrentCharacter.Character);
			if (num >= 0)
			{
				_currentCharacter.Troop = PartyScreenLogic.PrisonerRosters[(uint)CurrentCharacter.Side].GetElementCopyAtIndex(num);
			}
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Players can only execute prisoners", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "ExecuteTroop", 1172);
		}
		if (num < 0)
		{
			partyCharacterVMList2.Remove(CurrentCharacter);
			CurrentCharacter = partyCharacterVMList2.FirstOrDefault() ?? partyCharacterVMList.FirstOrDefault();
			MBInformationManager.HideInformations();
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("The prisoner should have been removed from the prisoner roster after execution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "ExecuteTroop", 1183);
		}
		RefreshTopInformation();
		RefreshPartyInformation();
	}

	private void TransferAllTroops(PartyScreenLogic.PartyCommand command)
	{
		InitializeTroopLists();
		RefreshPartyInformation();
	}

	private void SortTroops(PartyScreenLogic.PartyCommand command)
	{
		if (command.SortType != PartyScreenLogic.TroopSortType.Custom)
		{
			PartyScreenLogic.TroopSortType activeSortTypeForSide = PartyScreenLogic.GetActiveSortTypeForSide(command.RosterSide);
			TroopVMComparer comparer = new TroopVMComparer(PartyScreenLogic.GetComparer(activeSortTypeForSide));
			if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Left)
			{
				OtherPartyTroops.Sort(comparer);
				OtherPartyPrisoners.Sort(comparer);
			}
			else if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
			{
				MainPartyTroops.Sort(comparer);
				MainPartyPrisoners.Sort(comparer);
			}
		}
		if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Left)
		{
			OtherPartySortController.IsAscending = command.IsSortAscending;
			OtherPartySortController.SelectSortType(command.SortType);
		}
		else if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
		{
			MainPartySortController.IsAscending = command.IsSortAscending;
			MainPartySortController.SelectSortType(command.SortType);
		}
	}

	public void ExecuteTransferAllMainTroops()
	{
		TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member);
		ExecuteRemoveZeroCounts();
	}

	public void ExecuteTransferAllOtherTroops()
	{
		TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member);
		ExecuteRemoveZeroCounts();
	}

	public void ExecuteTransferAllMainPrisoners()
	{
		TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner);
		ExecuteRemoveZeroCounts();
	}

	public void ExecuteTransferAllOtherPrisoners()
	{
		TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner);
		ExecuteRemoveZeroCounts();
	}

	public void ExecuteOpenUpgradePopUp()
	{
		IsAnyPopUpOpen = true;
		UpgradePopUp.OpenPopUp();
		Game.Current.EventManager.TriggerEvent(new PlayerToggledUpgradePopupEvent(isOpened: true));
	}

	public void ExecuteOpenRecruitPopUp()
	{
		IsAnyPopUpOpen = true;
		RecruitPopUp.OpenPopUp();
	}

	public void ExecuteUpgrade(PartyCharacterVM troop, int upgradeTargetType, int maxUpgradeCount)
	{
		CurrentCharacter = troop;
		if (CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Right && CurrentCharacter.Type == PartyScreenLogic.TroopType.Member)
		{
			int number = 1;
			if (IsEntireStackModifierActive)
			{
				number = maxUpgradeCount;
			}
			else if (IsFiveStackModifierActive)
			{
				number = TaleWorlds.Library.MathF.Min(maxUpgradeCount, 5);
			}
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			int indexToInsertTroop = PartyScreenLogic.GetIndexToInsertTroop(CurrentCharacter.Side, CurrentCharacter.Type, CurrentCharacter.Troop);
			partyCommand.FillForUpgradeTroop(CurrentCharacter.Side, CurrentCharacter.Type, CurrentCharacter.Character, number, upgradeTargetType, indexToInsertTroop);
			PartyScreenLogic.AddCommand(partyCommand);
		}
	}

	public void ExecuteRecruit(PartyCharacterVM character, bool recruitAll = false)
	{
		CurrentCharacter = character;
		if (PartyScreenLogic.IsPrisonerRecruitable(CurrentCharacter.Type, CurrentCharacter.Character, CurrentCharacter.Side))
		{
			int number = 1;
			if (IsEntireStackModifierActive || recruitAll)
			{
				number = CurrentCharacter.NumOfRecruitablePrisoners;
			}
			else if (IsFiveStackModifierActive)
			{
				number = TaleWorlds.Library.MathF.Min(CurrentCharacter.NumOfRecruitablePrisoners, 5);
			}
			int indexToInsertTroop = PartyScreenLogic.GetIndexToInsertTroop(character.Side, character.Type, character.Troop);
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			partyCommand.FillForRecruitTroop(CurrentCharacter.Side, CurrentCharacter.Type, CurrentCharacter.Character, number, indexToInsertTroop);
			PartyScreenLogic.AddCommand(partyCommand);
			CurrentCharacter.UpdateRecruitable();
			CurrentCharacter.UpdateTalkable();
		}
	}

	public void ExecuteExecution()
	{
		if (PartyScreenLogic.IsExecutable(CurrentCharacter.Type, CurrentCharacter.Character, CurrentCharacter.Side))
		{
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			partyCommand.FillForExecuteTroop(CurrentCharacter.Side, CurrentCharacter.Type, CurrentCharacter.Character);
			PartyScreenLogic.AddCommand(partyCommand);
		}
	}

	public void ExecuteRemoveZeroCounts()
	{
		PartyScreenLogic.RemoveZeroCounts();
		List<PartyCharacterVM> list = OtherPartyTroops.ToList();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num].Number == 0 && OtherPartyTroops.Count > num)
			{
				list[num].IsSelected = false;
				OtherPartyTroops.RemoveAt(num);
			}
		}
		List<PartyCharacterVM> list2 = OtherPartyPrisoners.ToList();
		for (int num2 = list2.Count - 1; num2 >= 0; num2--)
		{
			if (list2[num2].Number == 0 && OtherPartyPrisoners.Count > num2)
			{
				list2[num2].IsSelected = false;
				OtherPartyPrisoners.RemoveAt(num2);
			}
		}
		List<PartyCharacterVM> list3 = MainPartyTroops.ToList();
		for (int num3 = list3.Count - 1; num3 >= 0; num3--)
		{
			if (list3[num3].Number == 0 && MainPartyTroops.Count > num3)
			{
				list3[num3].IsSelected = false;
				MainPartyTroops.RemoveAt(num3);
			}
		}
		List<PartyCharacterVM> list4 = MainPartyPrisoners.ToList();
		for (int num4 = list4.Count - 1; num4 >= 0; num4--)
		{
			if (list4[num4].Number == 0 && MainPartyPrisoners.Count > num4)
			{
				list4[num4].IsSelected = false;
				MainPartyPrisoners.RemoveAt(num4);
			}
		}
	}

	private void TransferAllCharacters(PartyScreenLogic.PartyRosterSide rosterSide, PartyScreenLogic.TroopType type)
	{
		SaveCharacterLockStates();
		PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
		partyCommand.FillForTransferAllTroops(rosterSide, type);
		PartyScreenLogic.AddCommand(partyCommand);
	}

	private void RefreshCurrentCharacterInformation()
	{
		HeroViewModel heroViewModel = new HeroViewModel();
		bool flag = CurrentCharacter.Character == CharacterObject.PlayerCharacter;
		CurrentCharacterWageLbl = "";
		if (CurrentCharacter.Type == PartyScreenLogic.TroopType.Member && !flag)
		{
			CurrentCharacterWageLbl = CurrentCharacter.Character.TroopWage.ToString();
		}
		CurrentCharacterLevelLbl = "-";
		if (CurrentCharacter.Type == PartyScreenLogic.TroopType.Member || CurrentCharacter.Type == PartyScreenLogic.TroopType.Prisoner)
		{
			CurrentCharacterLevelLbl = CurrentCharacter.Character.Level.ToString();
		}
		CurrentCharacter.InitializeUpgrades();
		if (CurrentCharacter.Character != null)
		{
			if (CurrentCharacter.Character.IsHero)
			{
				heroViewModel.FillFrom(CurrentCharacter.Character.HeroObject);
			}
			else
			{
				string bannerCode = "";
				if (!CurrentCharacter.IsPrisoner)
				{
					bannerCode = ((CurrentCharacter.Side != PartyScreenLogic.PartyRosterSide.Left) ? ((PartyScreenLogic.RightOwnerParty != null && PartyScreenLogic.RightOwnerParty.Banner != null) ? PartyScreenLogic.RightOwnerParty.Banner.BannerCode : "") : ((PartyScreenLogic.LeftOwnerParty != null && PartyScreenLogic.LeftOwnerParty.Banner != null) ? PartyScreenLogic.LeftOwnerParty.Banner.BannerCode : ""));
				}
				heroViewModel.FillFrom(CurrentCharacter.Character, CurrentCharacter.Character.StringId.GetDeterministicHashCode(), bannerCode);
			}
		}
		heroViewModel.SetEquipment(CurrentCharacter.Character.Equipment);
		if (!CurrentCharacter.IsPrisoner)
		{
			if (CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Right && PartyScreenLogic.RightOwnerParty != null && PartyScreenLogic.RightOwnerParty.MapFaction != null)
			{
				heroViewModel.ArmorColor1 = PartyScreenLogic.RightOwnerParty?.MapFaction?.Color ?? 0;
				heroViewModel.ArmorColor2 = PartyScreenLogic.RightOwnerParty?.MapFaction?.Color2 ?? 0;
			}
			else if (CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Left && PartyScreenLogic.LeftOwnerParty != null && PartyScreenLogic.LeftOwnerParty.MapFaction != null)
			{
				heroViewModel.ArmorColor1 = PartyScreenLogic.LeftOwnerParty?.MapFaction?.Color ?? 0;
				heroViewModel.ArmorColor2 = PartyScreenLogic.LeftOwnerParty?.MapFaction?.Color2 ?? 0;
			}
		}
		IsCurrentCharacterFormationEnabled = !CurrentCharacter.IsMainHero && !CurrentCharacter.IsPrisoner && CurrentCharacter.Side != PartyScreenLogic.PartyRosterSide.Left;
		IsCurrentCharacterWageEnabled = !CurrentCharacter.IsMainHero && !CurrentCharacter.IsPrisoner;
		CurrentCharacterTier = CampaignUIHelper.GetCharacterTierData(CurrentCharacter.Character, isBig: true);
		SelectedCharacter = heroViewModel;
		CurrentCharacter.UpdateTalkable();
	}

	private void RefreshPartyInformation()
	{
		OtherPartyTroopsLbl = PopulatePartyListLabel(OtherPartyTroops, PartyScreenLogic.LeftPartyMembersSizeLimit);
		OtherPartyPrisonersLbl = PopulatePartyListLabel(OtherPartyPrisoners, PartyScreenLogic.LeftPartyPrisonersSizeLimit);
		MainPartyTroopsLbl = PopulatePartyListLabel(MainPartyTroops, PartyScreenLogic.RightPartyMembersSizeLimit);
		MainPartyPrisonersLbl = PopulatePartyListLabel(MainPartyPrisoners, PartyScreenLogic.RightPartyPrisonersSizeLimit);
		if (ShowQuestProgress)
		{
			QuestProgressCurrentCount = PartyScreenLogic.GetCurrentQuestCurrentCount(ArePrisonersRelevantOnCurrentMode, AreMembersRelevantOnCurrentMode);
		}
		IsMainTroopsLimitWarningEnabled = PartyScreenLogic.RightPartyMembersSizeLimit < PartyScreenLogic.MemberRosters[1].TotalManCount && AreMembersRelevantOnCurrentMode;
		IsOtherTroopsLimitWarningEnabled = (_currentMode == PartyScreenHelper.PartyScreenMode.TroopsManage || _currentMode == PartyScreenHelper.PartyScreenMode.QuestTroopManage) && PartyScreenLogic.LeftPartyMembersSizeLimit < PartyScreenLogic.MemberRosters[0].TotalManCount && ArePrisonersRelevantOnCurrentMode;
		IsMainPrisonersLimitWarningEnabled = PartyScreenLogic.RightPartyPrisonersSizeLimit < PartyScreenLogic.PrisonerRosters[1].TotalManCount && ArePrisonersRelevantOnCurrentMode;
		UpdateAnyTransferableTroops(MainPartyTroops, delegate(bool result)
		{
			IsMainTroopsHaveTransferableTroops = result;
		}, DismissAllTroopsInputKey);
		UpdateAnyTransferableTroops(MainPartyPrisoners, delegate(bool result)
		{
			IsMainPrisonersHaveTransferableTroops = result;
		}, DismissAllPrisonersInputKey);
		UpdateAnyTransferableTroops(OtherPartyTroops, delegate(bool result)
		{
			IsOtherTroopsHaveTransferableTroops = result;
		}, TakeAllTroopsInputKey);
		UpdateAnyTransferableTroops(OtherPartyPrisoners, delegate(bool result)
		{
			IsOtherPrisonersHaveTransferableTroops = result;
		}, TakeAllPrisonersInputKey);
	}

	private void RefreshPrisonersRecruitable()
	{
		foreach (PartyCharacterVM mainPartyPrisoner in MainPartyPrisoners)
		{
			mainPartyPrisoner.UpdateRecruitable();
		}
	}

	private void RefreshTroopsUpgradeable()
	{
		foreach (PartyCharacterVM mainPartyTroop in MainPartyTroops)
		{
			mainPartyTroop.InitializeUpgrades();
		}
	}

	private static void UpdateAnyTransferableTroops(MBBindingList<PartyCharacterVM> partyList, Action<bool> setTransferableBoolean, InputKeyItemVM keyItem)
	{
		bool flag = false;
		for (int i = 0; i < partyList.Count; i++)
		{
			PartyCharacterVM partyCharacterVM = partyList[i];
			if (partyCharacterVM.Troop.Number > 0 && partyCharacterVM.IsTroopTransferrable)
			{
				flag = true;
				break;
			}
		}
		setTransferableBoolean(flag);
		bool? forcedVisibility = null;
		if (!flag)
		{
			forcedVisibility = false;
		}
		keyItem?.SetForcedVisibility(forcedVisibility);
	}

	private static string PopulatePartyListLabel(MBBindingList<PartyCharacterVM> partyList, int limit = 0)
	{
		int num = partyList.Sum((PartyCharacterVM item) => TaleWorlds.Library.MathF.Max(0, item.Number - item.WoundedCount));
		int num2 = partyList.Sum((PartyCharacterVM item) => (item.Number >= item.WoundedCount) ? item.WoundedCount : 0);
		MBTextManager.SetTextVariable("COUNT", num);
		MBTextManager.SetTextVariable("WEAK_COUNT", num2);
		if (limit != 0)
		{
			MBTextManager.SetTextVariable("MAX_COUNT", limit);
			if (num2 > 0)
			{
				MBTextManager.SetTextVariable("PARTY_LIST_TAG", "");
				MBTextManager.SetTextVariable("WEAK_COUNT", num2);
				MBTextManager.SetTextVariable("TOTAL_COUNT", num + num2);
				return GameTexts.FindText("str_party_list_label_with_weak").ToString();
			}
			MBTextManager.SetTextVariable("PARTY_LIST_TAG", "");
			return GameTexts.FindText("str_party_list_label").ToString();
		}
		if (num2 > 0)
		{
			return GameTexts.FindText("str_party_list_label_with_weak_without_max").ToString();
		}
		return num.ToString();
	}

	public void ExecuteTalk()
	{
		if (PartyScreenLogic.IsThereAnyChanges())
		{
			if (PartyScreenLogic.IsDoneActive())
			{
				ExecuteRemoveZeroCounts();
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=pF0SqQxL}Apply Changes?").ToString(), new TextObject("{=6DuCoCc2}You need to confirm your changes in order to engage in a conversation.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
				{
					if (PartyScreenLogic.DoneLogic(isForced: false))
					{
						ExecuteOpenConversation();
					}
					else
					{
						InformationManager.ShowInquiry(new InquiryData(new TextObject("{=1l4kpBDK}Failed to Apply Changes").ToString(), new TextObject("{=sFseX1Ka}Could not apply changes.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), string.Empty, null, null));
					}
				}, null));
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=kMAQndom}Reset Changes?").ToString(), new TextObject("{=XgkFpSdq}Cannot apply changes. You need reset your changes in order to engage in a conversation.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
				{
					ExecuteReset();
					ExecuteOpenConversation();
				}, null));
			}
		}
		else
		{
			ExecuteOpenConversation();
		}
	}

	private void ExecuteOpenConversation()
	{
		if (CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Right && CurrentCharacter.Character != CharacterObject.PlayerCharacter)
		{
			Location location = LocationComplex.Current?.GetLocationOfCharacter(LocationComplex.Current?.GetFirstLocationCharacterOfCharacter(CurrentCharacter.Character));
			if (location == null)
			{
				CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(CurrentCharacter.Character, PartyBase.MainParty, noHorse: false, noWeapon: false, spawnAfterFight: false, CurrentCharacter.IsPrisoner));
			}
			else
			{
				PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, CurrentCharacter.Character);
			}
			IsInConversation = true;
		}
	}

	public void ExecuteDone()
	{
		if (!PartyScreenLogic.IsDoneActive())
		{
			return;
		}
		ExecuteRemoveZeroCounts();
		if (_currentMode == PartyScreenHelper.PartyScreenMode.Loot && ((IsOtherPrisonersHaveTransferableTroops && CanRightPartyTakeMorePrisoners) || (IsOtherTroopsHaveTransferableTroops && CanRightPartyTakeMoreTroops)))
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_troops_behind").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), CloseScreenInternal, null));
		}
		else if (PartyScreenLogic.IsThereAnyChanges() && (IsMainPrisonersLimitWarningEnabled || IsMainTroopsLimitWarningEnabled || IsOtherTroopsLimitWarningEnabled))
		{
			GameTexts.SetVariable("newline", "\n");
			string text = string.Empty;
			if (IsMainTroopsLimitWarningEnabled)
			{
				text = GameTexts.FindText("str_party_over_limit_troops").ToString();
			}
			else if (IsMainPrisonersLimitWarningEnabled)
			{
				text = GameTexts.FindText("str_party_over_limit_prisoners").ToString();
			}
			else if (IsOtherTroopsLimitWarningEnabled)
			{
				text = GameTexts.FindText("str_other_party_over_limit_troops").ToString();
			}
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=uJro3Bua}Over Limit").ToString(), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), CloseScreenInternal, null));
		}
		else
		{
			CloseScreenInternal();
		}
	}

	private void CloseScreenInternal()
	{
		SaveSortState();
		SaveCharacterLockStates();
		PartyScreenHelper.CloseScreen(isForced: false);
	}

	public void ExecuteReset()
	{
		PartyScreenLogic.Reset(fromCancel: false);
		CurrentFocusedCharacter = null;
		CurrentFocusedUpgrade = null;
	}

	private void ExecuteCancelInternal()
	{
		ExecuteReset();
		PartyScreenHelper.CloseScreen(isForced: false, fromCancel: true);
	}

	public void ExecuteCancelWithoutInquiry()
	{
		ExecuteCancel();
	}

	public void ExecuteCancel(bool showCancelInquiry = false)
	{
		if (!PartyScreenLogic.IsCancelActive())
		{
			return;
		}
		if (_currentMode == PartyScreenHelper.PartyScreenMode.Loot)
		{
			if (PartyScreenLogic.IsThereAnyChanges())
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteCancelInternal, null));
			}
			else if ((IsOtherPrisonersHaveTransferableTroops && CanRightPartyTakeMorePrisoners) || (IsOtherTroopsHaveTransferableTroops && CanRightPartyTakeMoreTroops))
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_troops_behind").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteCancelInternal, null));
			}
			else
			{
				ExecuteCancelInternal();
			}
		}
		else if (showCancelInquiry && PartyScreenLogic.IsThereAnyChanges())
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteCancelInternal, null));
		}
		else
		{
			ExecuteCancelInternal();
		}
	}

	[Conditional("DEBUG")]
	private void EnsureLogicRostersAreInSyncWithVMLists()
	{
		List<TroopRoster> list = new List<TroopRoster>
		{
			PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member),
			PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner),
			PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member),
			PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner)
		};
		List<MBBindingList<PartyCharacterVM>> list2 = new List<MBBindingList<PartyCharacterVM>>
		{
			GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member),
			GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner),
			GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member),
			GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner)
		};
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Count != list2[i].Count)
			{
				TaleWorlds.Library.Debug.FailedAssert("Logic and VM list counts do not match", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "EnsureLogicRostersAreInSyncWithVMLists", 1818);
				continue;
			}
			for (int j = 0; j < list[i].Count; j++)
			{
				if (list[i].GetCharacterAtIndex(j).StringId != list2[i][j].Character.StringId)
				{
					TaleWorlds.Library.Debug.FailedAssert("Logic and VM rosters do not match", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "EnsureLogicRostersAreInSyncWithVMLists", 1826);
					return;
				}
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		IsAnyPopUpOpen = false;
		_selectedCharacter.OnFinalize();
		_selectedCharacter = null;
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		CancelInputKey.OnFinalize();
		DoneInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
		TakeAllTroopsInputKey.OnFinalize();
		DismissAllTroopsInputKey.OnFinalize();
		TakeAllPrisonersInputKey.OnFinalize();
		DismissAllPrisonersInputKey.OnFinalize();
		OpenUpgradePanelInputKey?.OnFinalize();
		OpenRecruitPanelInputKey?.OnFinalize();
		PartyTradeVM.RemoveZeroCounts -= ExecuteRemoveZeroCounts;
		PartyCharacterVM.ProcessCharacterLock = null;
		PartyCharacterVM.SetSelected = null;
		PartyCharacterVM.OnShift = null;
		PartyCharacterVM.OnFocus = null;
		PartyCharacterVM.OnTransfer = null;
		UpgradePopUp.OnFinalize();
		RecruitPopUp.OnFinalize();
	}

	private TextObject GetTransferAllOtherTroopsKeyText()
	{
		if (TakeAllTroopsInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(TakeAllTroopsInputKey.KeyID);
	}

	private TextObject GetTransferAllMainTroopsKeyText()
	{
		if (DismissAllTroopsInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(DismissAllTroopsInputKey.KeyID);
	}

	private TextObject GetTransferAllOtherPrisonersKeyText()
	{
		if (TakeAllPrisonersInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(TakeAllPrisonersInputKey.KeyID);
	}

	private TextObject GetTransferAllMainPrisonersKeyText()
	{
		if (DismissAllPrisonersInputKey == null || _getKeyTextFromKeyId == null)
		{
			return TextObject.GetEmpty();
		}
		return _getKeyTextFromKeyId(DismissAllPrisonersInputKey.KeyID);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		UpgradePopUp.SetCancelInputKey(hotKey);
		RecruitPopUp.SetCancelInputKey(hotKey);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		UpgradePopUp.SetDoneInputKey(hotKey);
		RecruitPopUp.SetDoneInputKey(hotKey);
	}

	public void SetTakeAllTroopsInputKey(HotKey hotKey)
	{
		TakeAllTroopsInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		TransferAllOtherTroopsHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllOtherTroopsKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		UpdateAnyTransferableTroops(OtherPartyTroops, delegate(bool result)
		{
			IsOtherTroopsHaveTransferableTroops = result;
		}, TakeAllTroopsInputKey);
	}

	public void SetDismissAllTroopsInputKey(HotKey hotKey)
	{
		DismissAllTroopsInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		TransferAllMainTroopsHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllMainTroopsKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		UpdateAnyTransferableTroops(MainPartyTroops, delegate(bool result)
		{
			IsMainTroopsHaveTransferableTroops = result;
		}, DismissAllTroopsInputKey);
	}

	public void SetTakeAllPrisonersInputKey(HotKey hotKey)
	{
		TakeAllPrisonersInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		TransferAllOtherPrisonersHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllOtherPrisonersKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		UpdateAnyTransferableTroops(OtherPartyPrisoners, delegate(bool result)
		{
			IsOtherPrisonersHaveTransferableTroops = result;
		}, TakeAllPrisonersInputKey);
	}

	public void SetDismissAllPrisonersInputKey(HotKey hotKey)
	{
		DismissAllPrisonersInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		TransferAllMainPrisonersHint = new BasicTooltipViewModel(delegate
		{
			GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners"));
			GameTexts.SetVariable("HOTKEY", GetTransferAllMainPrisonersKeyText());
			return GameTexts.FindText("str_hotkey_with_hint").ToString();
		});
		UpdateAnyTransferableTroops(MainPartyPrisoners, delegate(bool result)
		{
			IsMainPrisonersHaveTransferableTroops = result;
		}, DismissAllPrisonersInputKey);
	}

	public void SetOpenUpgradePanelInputKey(HotKey hotKey)
	{
		OpenUpgradePanelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetOpenRecruitPanelInputKey(HotKey hotKey)
	{
		OpenRecruitPanelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
	{
		_getKeyTextFromKeyId = getKeyTextFromKeyId;
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (!(obj.NewNotificationElementID != _latestTutorialElementID))
		{
			return;
		}
		if (_latestTutorialElementID != null)
		{
			if (_isUpgradePopupButtonHighlightApplied)
			{
				IsUpgradePopupButtonHighlightEnabled = false;
				_isUpgradePopupButtonHighlightApplied = false;
			}
			if (_isUpgradeButtonHighlightApplied)
			{
				SetUpgradeButtonsHighlightState(state: false);
				_isUpgradeButtonHighlightApplied = false;
			}
			if (_isRecruitButtonHighlightApplied)
			{
				SetRecruitButtonsHighlightState(state: false);
				_isRecruitButtonHighlightApplied = false;
			}
			if (_isTransferButtonHighlightApplied)
			{
				SetTransferButtonHighlightState(state: false, null);
				_isTransferButtonHighlightApplied = false;
			}
		}
		_latestTutorialElementID = obj.NewNotificationElementID;
		if (_latestTutorialElementID == null)
		{
			return;
		}
		if (!_isUpgradePopupButtonHighlightApplied && _latestTutorialElementID == _upgradePopupButtonID)
		{
			IsUpgradePopupButtonHighlightEnabled = true;
			_isUpgradePopupButtonHighlightApplied = true;
		}
		if (_latestTutorialElementID == _upgradeButtonID)
		{
			SetUpgradeButtonsHighlightState(state: true);
			_isUpgradeButtonHighlightApplied = true;
		}
		if (!_isRecruitButtonHighlightApplied && _latestTutorialElementID == _recruitButtonID)
		{
			SetRecruitButtonsHighlightState(state: true);
			_isRecruitButtonHighlightApplied = true;
		}
		if (!_isTransferButtonHighlightApplied && _latestTutorialElementID == _transferButtonOnlyOtherPrisonersID)
		{
			SetTransferButtonHighlightState(state: true, (PartyCharacterVM x) => x.Side == PartyScreenLogic.PartyRosterSide.Left && x.IsPrisoner && x.IsTroopTransferrable);
			_isTransferButtonHighlightApplied = true;
		}
	}

	private void SetUpgradeButtonsHighlightState(bool state)
	{
		MainPartyTroops?.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
		{
			x.SetIsUpgradeButtonHighlighted(state);
		});
	}

	private void SetRecruitButtonsHighlightState(bool state)
	{
		foreach (PartyCharacterVM mainPartyTroop in MainPartyTroops)
		{
			mainPartyTroop.IsRecruitButtonsHiglighted = state;
		}
	}

	private void SetTransferButtonHighlightState(bool state, Func<PartyCharacterVM, bool> predicate)
	{
		foreach (PartyCharacterVM mainPartyTroop in MainPartyTroops)
		{
			if (predicate == null || predicate(mainPartyTroop))
			{
				mainPartyTroop.IsTransferButtonHiglighted = state;
			}
		}
		foreach (PartyCharacterVM mainPartyPrisoner in MainPartyPrisoners)
		{
			if (predicate == null || predicate(mainPartyPrisoner))
			{
				mainPartyPrisoner.IsTransferButtonHiglighted = state;
			}
		}
		foreach (PartyCharacterVM otherPartyTroop in OtherPartyTroops)
		{
			if (predicate == null || predicate(otherPartyTroop))
			{
				otherPartyTroop.IsTransferButtonHiglighted = state;
			}
		}
		foreach (PartyCharacterVM otherPartyPrisoner in OtherPartyPrisoners)
		{
			if (predicate == null || predicate(otherPartyPrisoner))
			{
				otherPartyPrisoner.IsTransferButtonHiglighted = state;
			}
		}
	}
}
