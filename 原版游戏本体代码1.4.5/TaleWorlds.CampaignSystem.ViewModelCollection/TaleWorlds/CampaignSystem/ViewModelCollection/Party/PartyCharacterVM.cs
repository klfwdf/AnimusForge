using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PartyCharacterVM : ViewModel
{
	public static bool IsShiftingDisabled;

	public static Action<PartyCharacterVM, bool> ProcessCharacterLock;

	public static Action<PartyCharacterVM> SetSelected;

	public static Action<PartyCharacterVM, int, int, PartyScreenLogic.PartyRosterSide> OnTransfer;

	public static Action<PartyCharacterVM> OnShift;

	public static Action<PartyCharacterVM> OnFocus;

	public readonly PartyScreenLogic.PartyRosterSide Side;

	public readonly PartyScreenLogic.TroopType Type;

	protected readonly PartyVM _partyVm;

	protected readonly PartyScreenLogic _partyScreenLogic;

	protected readonly bool _initIsTroopTransferable;

	private Tuple<bool, TextObject> _partyCharacterTalkPermission;

	private TroopRosterElement _troop;

	private CharacterObject _character;

	private string _name;

	private string _strNumOfUpgradableTroop;

	private string _strNumOfRecruitableTroop;

	private string _troopID;

	private string _upgradeCostText;

	private string _recruitMoraleCostText;

	private MBBindingList<UpgradeTargetVM> _upgrades;

	private CharacterImageIdentifierVM _code;

	private BasicTooltipViewModel _transferHint;

	private BasicTooltipViewModel _recruitPrisonerHint;

	private BasicTooltipViewModel _executePrisonerHint;

	private BasicTooltipViewModel _heroHealthHint;

	private HintViewModel _talkHint;

	private int _transferAmount = 1;

	private int _index = -2;

	private int _numOfReadyToUpgradeTroops;

	private int _numOfUpgradeableTroops;

	private int _numOfRecruitablePrisoners;

	private int _maxXP;

	private int _currentXP;

	private int _maxConformity;

	private int _currentConformity;

	private BasicTooltipViewModel _troopXPTooltip;

	private BasicTooltipViewModel _troopConformityTooltip;

	private bool _isHero;

	private bool _isMainHero;

	private bool _isPrisoner;

	private bool _isPrisonerOfPlayer;

	private bool _isRecruitablePrisoner;

	private bool _isUpgradableTroop;

	private bool _isTroopTransferrable;

	private bool _isHeroPrisonerOfPlayer;

	private bool _isTroopUpgradable;

	private StringItemWithHintVM _tierIconData;

	private bool _hasEnoughGold;

	private bool _anyUpgradeHasRequirement;

	private StringItemWithHintVM _typeIconData;

	private bool _isRecruitButtonsHiglighted;

	private bool _isTransferButtonHiglighted;

	private bool _isFormationEnabled;

	private PartyTradeVM _tradeData;

	private bool _isTroopRecruitable;

	private bool _isExecutable;

	private bool _isLocked;

	private HintViewModel _lockHint;

	private bool _isTalkableCharacter;

	private bool _canTalk;

	private bool _isSelected;

	public TroopRoster Troops { get; private set; }

	public string StringId { get; private set; }

	public TroopRosterElement Troop
	{
		get
		{
			return _troop;
		}
		set
		{
			_troop = value;
			Character = value.Character;
			TroopID = Character.StringId;
			CheckTransferAmountDefaultValue();
			TroopXPTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetTroopXPTooltip(value));
			TroopConformityTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetTroopConformityTooltip(value));
		}
	}

	public CharacterObject Character
	{
		get
		{
			return _character;
		}
		set
		{
			if (_character != value)
			{
				_character = value;
				CharacterCode characterCode = GetCharacterCode(value, Type, Side);
				Code = new CharacterImageIdentifierVM(characterCode);
				CharacterObject[] upgradeTargets = _character.UpgradeTargets;
				if (upgradeTargets != null && upgradeTargets.Length != 0)
				{
					Upgrades = new MBBindingList<UpgradeTargetVM>();
					for (int i = 0; i < _character.UpgradeTargets.Length; i++)
					{
						CharacterCode characterCode2 = GetCharacterCode(_character.UpgradeTargets[i], Type, Side);
						Upgrades.Add(new UpgradeTargetVM(i, value, characterCode2, Upgrade, FocusUpgrade));
					}
				}
			}
			CheckTransferAmountDefaultValue();
		}
	}

	[DataSourceProperty]
	public bool IsFormationEnabled
	{
		get
		{
			return _isFormationEnabled;
		}
		set
		{
			if (_isFormationEnabled != value)
			{
				_isFormationEnabled = value;
				OnPropertyChangedWithValue(value, "IsFormationEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string TransferString => TransferAmount + "/" + Number;

	[DataSourceProperty]
	public bool IsTroopUpgradable
	{
		get
		{
			return _isTroopUpgradable;
		}
		set
		{
			if (value != _isTroopUpgradable)
			{
				_isTroopUpgradable = value;
				OnPropertyChangedWithValue(value, "IsTroopUpgradable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopRecruitable
	{
		get
		{
			return _isTroopRecruitable;
		}
		set
		{
			if (value != _isTroopRecruitable)
			{
				_isTroopRecruitable = value;
				OnPropertyChangedWithValue(value, "IsTroopRecruitable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRecruitablePrisoner
	{
		get
		{
			return _isRecruitablePrisoner;
		}
		set
		{
			if (value != _isRecruitablePrisoner)
			{
				_isRecruitablePrisoner = value;
				OnPropertyChangedWithValue(value, "IsRecruitablePrisoner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpgradableTroop
	{
		get
		{
			return _isUpgradableTroop;
		}
		set
		{
			if (value != _isUpgradableTroop)
			{
				_isUpgradableTroop = value;
				OnPropertyChangedWithValue(value, "IsUpgradableTroop");
			}
		}
	}

	[DataSourceProperty]
	public bool IsExecutable
	{
		get
		{
			return _isExecutable;
		}
		set
		{
			if (value != _isExecutable)
			{
				_isExecutable = value;
				OnPropertyChangedWithValue(value, "IsExecutable");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfReadyToUpgradeTroops
	{
		get
		{
			return _numOfReadyToUpgradeTroops;
		}
		set
		{
			if (value != _numOfReadyToUpgradeTroops)
			{
				_numOfReadyToUpgradeTroops = value;
				OnPropertyChangedWithValue(value, "NumOfReadyToUpgradeTroops");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfUpgradeableTroops
	{
		get
		{
			return _numOfUpgradeableTroops;
		}
		set
		{
			if (value != _numOfUpgradeableTroops)
			{
				_numOfUpgradeableTroops = value;
				OnPropertyChangedWithValue(value, "NumOfUpgradeableTroops");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfRecruitablePrisoners
	{
		get
		{
			return _numOfRecruitablePrisoners;
		}
		set
		{
			if (value != _numOfRecruitablePrisoners)
			{
				_numOfRecruitablePrisoners = value;
				OnPropertyChangedWithValue(value, "NumOfRecruitablePrisoners");
			}
		}
	}

	[DataSourceProperty]
	public int MaxXP
	{
		get
		{
			return _maxXP;
		}
		set
		{
			if (value != _maxXP)
			{
				_maxXP = value;
				OnPropertyChangedWithValue(value, "MaxXP");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentXP
	{
		get
		{
			return _currentXP;
		}
		set
		{
			if (value != _currentXP)
			{
				_currentXP = value;
				OnPropertyChangedWithValue(value, "CurrentXP");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentConformity
	{
		get
		{
			return _currentConformity;
		}
		set
		{
			if (value != _currentConformity)
			{
				_currentConformity = value;
				OnPropertyChangedWithValue(value, "CurrentConformity");
			}
		}
	}

	[DataSourceProperty]
	public int MaxConformity
	{
		get
		{
			return _maxConformity;
		}
		set
		{
			if (value != _maxConformity)
			{
				_maxConformity = value;
				OnPropertyChangedWithValue(value, "MaxConformity");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TroopXPTooltip
	{
		get
		{
			return _troopXPTooltip;
		}
		set
		{
			if (value != _troopXPTooltip)
			{
				_troopXPTooltip = value;
				OnPropertyChangedWithValue(value, "TroopXPTooltip");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TroopConformityTooltip
	{
		get
		{
			return _troopConformityTooltip;
		}
		set
		{
			if (value != _troopConformityTooltip)
			{
				_troopConformityTooltip = value;
				OnPropertyChangedWithValue(value, "TroopConformityTooltip");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TransferHint
	{
		get
		{
			return _transferHint;
		}
		set
		{
			if (value != _transferHint)
			{
				_transferHint = value;
				OnPropertyChangedWithValue(value, "TransferHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRecruitButtonsHiglighted
	{
		get
		{
			return _isRecruitButtonsHiglighted;
		}
		set
		{
			if (value != _isRecruitButtonsHiglighted)
			{
				_isRecruitButtonsHiglighted = value;
				OnPropertyChangedWithValue(value, "IsRecruitButtonsHiglighted");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTransferButtonHiglighted
	{
		get
		{
			return _isTransferButtonHiglighted;
		}
		set
		{
			if (value != _isTransferButtonHiglighted)
			{
				_isTransferButtonHiglighted = value;
				OnPropertyChangedWithValue(value, "IsTransferButtonHiglighted");
			}
		}
	}

	[DataSourceProperty]
	public string StrNumOfUpgradableTroop
	{
		get
		{
			return _strNumOfUpgradableTroop;
		}
		set
		{
			if (value != _strNumOfUpgradableTroop)
			{
				_strNumOfUpgradableTroop = value;
				OnPropertyChangedWithValue(value, "StrNumOfUpgradableTroop");
			}
		}
	}

	[DataSourceProperty]
	public string StrNumOfRecruitableTroop
	{
		get
		{
			return _strNumOfRecruitableTroop;
		}
		set
		{
			if (value != _strNumOfRecruitableTroop)
			{
				_strNumOfRecruitableTroop = value;
				OnPropertyChangedWithValue(value, "StrNumOfRecruitableTroop");
			}
		}
	}

	[DataSourceProperty]
	public string TroopID
	{
		get
		{
			return _troopID;
		}
		set
		{
			if (value != _troopID)
			{
				_troopID = value;
				OnPropertyChangedWithValue(value, "TroopID");
			}
		}
	}

	[DataSourceProperty]
	public string UpgradeCostText
	{
		get
		{
			return _upgradeCostText;
		}
		set
		{
			if (value != _upgradeCostText)
			{
				_upgradeCostText = value;
				OnPropertyChangedWithValue(value, "UpgradeCostText");
			}
		}
	}

	[DataSourceProperty]
	public string RecruitMoraleCostText
	{
		get
		{
			return _recruitMoraleCostText;
		}
		set
		{
			if (value != _recruitMoraleCostText)
			{
				_recruitMoraleCostText = value;
				OnPropertyChangedWithValue(value, "RecruitMoraleCostText");
			}
		}
	}

	[DataSourceProperty]
	public int Index
	{
		get
		{
			return _index;
		}
		set
		{
			if (_index != value)
			{
				_index = value;
				OnPropertyChangedWithValue(value, "Index");
			}
		}
	}

	[DataSourceProperty]
	public int TransferAmount
	{
		get
		{
			return _transferAmount;
		}
		set
		{
			if (value <= 0)
			{
				value = 1;
			}
			if (_transferAmount != value)
			{
				_transferAmount = value;
				OnPropertyChangedWithValue(value, "TransferAmount");
				OnPropertyChanged("TransferString");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopTransferrable
	{
		get
		{
			return _isTroopTransferrable;
		}
		set
		{
			if (Character != CharacterObject.PlayerCharacter)
			{
				_isTroopTransferrable = value;
				OnPropertyChangedWithValue(value, "IsTroopTransferrable");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string TroopNum
	{
		get
		{
			if (Character != null && Character.IsHero)
			{
				return "1";
			}
			if (Troop.Character != null)
			{
				int num = Troop.Number - Troop.WoundedNumber;
				string text = GameTexts.FindText("str_party_nameplate_wounded_abbr").ToString();
				if (num != Troop.Number && Type != PartyScreenLogic.TroopType.Prisoner)
				{
					return num + "+" + Troop.WoundedNumber + text;
				}
				return Troop.Number.ToString();
			}
			return "-1";
		}
	}

	[DataSourceProperty]
	public bool IsHeroWounded
	{
		get
		{
			CharacterObject character = Character;
			if (character != null && character.IsHero)
			{
				return Character.HeroObject.IsWounded;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public int HeroHealth
	{
		get
		{
			CharacterObject character = Character;
			if (character != null && character.IsHero)
			{
				return TaleWorlds.Library.MathF.Ceiling((float)Character.HeroObject.HitPoints * 100f / (float)Character.MaxHitPoints());
			}
			return 0;
		}
	}

	[DataSourceProperty]
	public int Number
	{
		get
		{
			IsTroopTransferrable = _initIsTroopTransferable && Troop.Number > 0;
			return Troop.Number;
		}
	}

	[DataSourceProperty]
	public int WoundedCount
	{
		get
		{
			if (Troop.Character == null)
			{
				return 0;
			}
			return Troop.WoundedNumber;
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel RecruitPrisonerHint
	{
		get
		{
			return _recruitPrisonerHint;
		}
		set
		{
			if (value != _recruitPrisonerHint)
			{
				_recruitPrisonerHint = value;
				OnPropertyChangedWithValue(value, "RecruitPrisonerHint");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM Code
	{
		get
		{
			return _code;
		}
		set
		{
			if (value != _code)
			{
				_code = value;
				OnPropertyChangedWithValue(value, "Code");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ExecutePrisonerHint
	{
		get
		{
			return _executePrisonerHint;
		}
		set
		{
			if (value != _executePrisonerHint)
			{
				_executePrisonerHint = value;
				OnPropertyChangedWithValue(value, "ExecutePrisonerHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<UpgradeTargetVM> Upgrades
	{
		get
		{
			return _upgrades;
		}
		set
		{
			if (value != _upgrades)
			{
				_upgrades = value;
				OnPropertyChangedWithValue(value, "Upgrades");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel HeroHealthHint
	{
		get
		{
			return _heroHealthHint;
		}
		set
		{
			if (value != _heroHealthHint)
			{
				_heroHealthHint = value;
				OnPropertyChangedWithValue(value, "HeroHealthHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHero
	{
		get
		{
			return _isHero;
		}
		set
		{
			if (value != _isHero)
			{
				_isHero = value;
				OnPropertyChangedWithValue(value, "IsHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChangedWithValue(value, "IsMainHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPrisoner
	{
		get
		{
			return _isPrisoner;
		}
		set
		{
			if (value != _isPrisoner)
			{
				_isPrisoner = value;
				OnPropertyChangedWithValue(value, "IsPrisoner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPrisonerOfPlayer
	{
		get
		{
			return _isPrisonerOfPlayer;
		}
		set
		{
			if (value != _isPrisonerOfPlayer)
			{
				_isPrisonerOfPlayer = value;
				OnPropertyChangedWithValue(value, "IsPrisonerOfPlayer");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHeroPrisonerOfPlayer
	{
		get
		{
			return _isHeroPrisonerOfPlayer;
		}
		set
		{
			if (value != _isHeroPrisonerOfPlayer)
			{
				_isHeroPrisonerOfPlayer = value;
				OnPropertyChangedWithValue(value, "IsHeroPrisonerOfPlayer");
			}
		}
	}

	[DataSourceProperty]
	public bool AnyUpgradeHasRequirement
	{
		get
		{
			return _anyUpgradeHasRequirement;
		}
		set
		{
			if (value != _anyUpgradeHasRequirement)
			{
				_anyUpgradeHasRequirement = value;
				OnPropertyChangedWithValue(value, "AnyUpgradeHasRequirement");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TierIconData
	{
		get
		{
			return _tierIconData;
		}
		set
		{
			if (value != _tierIconData)
			{
				_tierIconData = value;
				OnPropertyChangedWithValue(value, "TierIconData");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TypeIconData
	{
		get
		{
			return _typeIconData;
		}
		set
		{
			if (value != _typeIconData)
			{
				_typeIconData = value;
				OnPropertyChangedWithValue(value, "TypeIconData");
			}
		}
	}

	[DataSourceProperty]
	public bool HasEnoughGold
	{
		get
		{
			return _hasEnoughGold;
		}
		set
		{
			if (value != _hasEnoughGold)
			{
				_hasEnoughGold = value;
				OnPropertyChangedWithValue(value, "HasEnoughGold");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTalkableCharacter
	{
		get
		{
			return _isTalkableCharacter;
		}
		set
		{
			if (value != _isTalkableCharacter)
			{
				_isTalkableCharacter = value;
				OnPropertyChangedWithValue(value, "IsTalkableCharacter");
			}
		}
	}

	[DataSourceProperty]
	public bool CanTalk
	{
		get
		{
			return _canTalk;
		}
		set
		{
			if (value != _canTalk)
			{
				_canTalk = value;
				OnPropertyChangedWithValue(value, "CanTalk");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TalkHint
	{
		get
		{
			return _talkHint;
		}
		set
		{
			if (value != _talkHint)
			{
				_talkHint = value;
				OnPropertyChangedWithValue(value, "TalkHint");
			}
		}
	}

	[DataSourceProperty]
	public PartyTradeVM TradeData
	{
		get
		{
			return _tradeData;
		}
		set
		{
			if (value != _tradeData)
			{
				_tradeData = value;
				OnPropertyChangedWithValue(value, "TradeData");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (value != _isLocked)
			{
				_isLocked = value;
				OnPropertyChangedWithValue(value, "IsLocked");
				ProcessCharacterLock?.Invoke(this, value);
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LockHint
	{
		get
		{
			return _lockHint;
		}
		set
		{
			if (value != _lockHint)
			{
				_lockHint = value;
				OnPropertyChangedWithValue(value, "LockHint");
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
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	public PartyCharacterVM(PartyScreenLogic partyScreenLogic, PartyVM partyVm, TroopRoster troops, int index, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, bool isTroopTransferrable)
	{
		Upgrades = new MBBindingList<UpgradeTargetVM>();
		_partyScreenLogic = partyScreenLogic;
		_partyVm = partyVm;
		Troops = troops;
		Side = side;
		Type = type;
		Troop = troops.GetElementCopyAtIndex(index);
		Index = index;
		IsHero = Troop.Character.IsHero;
		IsMainHero = Hero.MainHero.CharacterObject == Troop.Character;
		IsPrisoner = Type == PartyScreenLogic.TroopType.Prisoner;
		TierIconData = CampaignUIHelper.GetCharacterTierData(Troop.Character, isBig: true);
		TypeIconData = CampaignUIHelper.GetCharacterTypeData(Troop.Character);
		StringId = CampaignUIHelper.GetTroopLockStringID(Troop);
		_initIsTroopTransferable = isTroopTransferrable;
		IsTroopTransferrable = _initIsTroopTransferable;
		TradeData = new PartyTradeVM(partyScreenLogic, Troop, Side, IsTroopTransferrable, IsPrisoner, OnTradeApplyTransaction);
		IsPrisonerOfPlayer = IsPrisoner && Side == PartyScreenLogic.PartyRosterSide.Right;
		IsHeroPrisonerOfPlayer = IsPrisonerOfPlayer && Character.IsHero;
		IsExecutable = _partyScreenLogic.IsExecutable(Type, Character, Side);
		IsUpgradableTroop = Side == PartyScreenLogic.PartyRosterSide.Right && !IsHero && !IsPrisoner && Character.UpgradeTargets.Length != 0;
		InitializeUpgrades();
		ThrowOnPropertyChanged();
		CheckTransferAmountDefaultValue();
		UpdateRecruitable();
		RefreshValues();
		SetMoraleCost();
		UpdateTalkable();
		TransferHint = new BasicTooltipViewModel(() => GetTransferHint());
		RecruitPrisonerHint = new BasicTooltipViewModel(() => GetRecruitHint());
		ExecutePrisonerHint = new BasicTooltipViewModel(() => _partyScreenLogic.GetExecutableReasonString(Troop.Character, IsExecutable));
		HeroHealthHint = (Troop.Character.IsHero ? new BasicTooltipViewModel(() => CampaignUIHelper.GetHeroHealthTooltip(Troop.Character.HeroObject)) : null);
	}

	public void UpdateTalkable()
	{
		bool flag = Side == PartyScreenLogic.PartyRosterSide.Right;
		bool flag2 = Troop.Character != CharacterObject.PlayerCharacter;
		bool isHero = Troop.Character.IsHero;
		IsTalkableCharacter = flag2 && flag && isHero;
		if (TalkHint == null)
		{
			TalkHint = new HintViewModel();
		}
		if (IsTalkableCharacter)
		{
			_partyCharacterTalkPermission = null;
			Game.Current.EventManager.TriggerEvent(new PartyScreenCharacterTalkPermissionEvent(Character.HeroObject, OnPartyCharacterTalkPermissionResult));
			if (_partyCharacterTalkPermission != null && !_partyCharacterTalkPermission.Item1)
			{
				CanTalk = false;
				TalkHint.HintText = _partyCharacterTalkPermission.Item2;
				if (TalkHint.HintText.IsEmpty())
				{
					TalkHint.HintText = new TextObject("{=epQYhd1A}Cannot talk to hero right now");
				}
				return;
			}
			CanTalkToHeroDelegate canTalkToHeroDelegate = _partyVm.PartyScreenLogic.CanTalkToHeroDelegate;
			CanTalk = (canTalkToHeroDelegate == null || canTalkToHeroDelegate(Character.HeroObject, Type, Side, _partyScreenLogic.LeftOwnerParty, out TalkHint.HintText)) && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out TalkHint.HintText);
			if (CanTalk)
			{
				TalkHint.HintText = GameTexts.FindText("str_talk_button");
			}
			else if (TalkHint.HintText.IsEmpty())
			{
				TalkHint.HintText = new TextObject("{=epQYhd1A}Cannot talk to hero right now");
			}
		}
		else
		{
			TalkHint.HintText = TextObject.GetEmpty();
			CanTalk = false;
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Troop.Character.Name.ToString();
		LockHint = new HintViewModel(GameTexts.FindText("str_lock_in_party").SetTextVariable("TRANSFERABLE", IsPrisoner ? GameTexts.FindText("str_prisoners").ToString() : GameTexts.FindText("str_troops").ToString()));
		Upgrades?.ApplyActionOnAllItems(delegate(UpgradeTargetVM x)
		{
			x.RefreshValues();
		});
		TradeData?.RefreshValues();
	}

	private void OnPartyCharacterTalkPermissionResult(bool isAvailable, TextObject reasonStr)
	{
		_partyCharacterTalkPermission = new Tuple<bool, TextObject>(isAvailable, reasonStr);
	}

	private string GetTransferHint()
	{
		string text = GameTexts.FindText("str_transfer").ToString();
		string stackModifierString = CampaignUIHelper.GetStackModifierString(GameTexts.FindText("str_entire_stack_shortcut_transfer"), GameTexts.FindText("str_five_stack_shortcut_transfer"), Troop.Number >= 5);
		if (string.IsNullOrEmpty(stackModifierString))
		{
			return text;
		}
		return GameTexts.FindText("str_string_newline_string").SetTextVariable("STR1", text).SetTextVariable("STR2", stackModifierString)
			.ToString();
	}

	private string GetRecruitHint()
	{
		bool showStackModifierText;
		string recruitableReasonString = _partyScreenLogic.GetRecruitableReasonString(Troop.Character, IsTroopRecruitable, Troop.Number, out showStackModifierText);
		string stackModifierString = CampaignUIHelper.GetStackModifierString(GameTexts.FindText("str_entire_stack_shortcut_recruit_units"), GameTexts.FindText("str_five_stack_shortcut_recruit_units"), Troop.Number >= 5);
		if (string.IsNullOrEmpty(stackModifierString) || !showStackModifierText)
		{
			return recruitableReasonString;
		}
		return GameTexts.FindText("str_string_newline_string").SetTextVariable("STR1", recruitableReasonString).SetTextVariable("STR2", stackModifierString)
			.ToString();
	}

	private void CheckTransferAmountDefaultValue()
	{
		if (TransferAmount == 0 && Troop.Character != null && Troop.Number > 0)
		{
			TransferAmount = 1;
		}
	}

	public void ExecuteSetSelected()
	{
		if (Character != null)
		{
			SetSelected(this);
		}
	}

	public void ExecuteTalk()
	{
		_partyVm?.ExecuteTalk();
	}

	public void UpdateTradeData()
	{
		TradeData?.UpdateTroopData(Troop, Side);
	}

	public void UpdateRecruitable()
	{
		MaxConformity = Troop.Character.ConformityNeededToRecruitPrisoner;
		int elementXp = PartyBase.MainParty.PrisonRoster.GetElementXp(Troop.Character);
		CurrentConformity = ((elementXp >= Troop.Number * MaxConformity) ? MaxConformity : (elementXp % MaxConformity));
		IsRecruitablePrisoner = !_character.IsHero && Type == PartyScreenLogic.TroopType.Prisoner;
		IsTroopRecruitable = _partyScreenLogic.IsPrisonerRecruitable(Type, Character, Side) && !_partyScreenLogic.IsTroopUpgradesDisabled;
		NumOfRecruitablePrisoners = _partyScreenLogic.GetTroopRecruitableAmount(Character);
		GameTexts.SetVariable("LEFT", NumOfRecruitablePrisoners);
		GameTexts.SetVariable("RIGHT", Troop.Number);
		StrNumOfRecruitableTroop = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
	}

	private void OnTradeApplyTransaction(int amount, bool isIncreasing)
	{
		TransferAmount = amount;
		PartyScreenLogic.PartyRosterSide side = ((!isIncreasing) ? PartyScreenLogic.PartyRosterSide.Right : PartyScreenLogic.PartyRosterSide.Left);
		ApplyTransfer(TransferAmount, side);
		IsExecutable = _partyScreenLogic.IsExecutable(Type, Character, Side) && Troop.Number > 0;
	}

	public void InitializeUpgrades()
	{
		if (IsUpgradableTroop)
		{
			for (int i = 0; i < Character.UpgradeTargets.Length; i++)
			{
				CharacterObject characterObject = Character.UpgradeTargets[i];
				bool flag = false;
				bool flag2 = false;
				int num = 0;
				int level = characterObject.Level;
				int upgradeGoldCost = Character.GetUpgradeGoldCost(PartyBase.MainParty, i);
				if (!Character.Culture.IsBandit)
				{
					_ = Character.Level;
				}
				else
				{
					_ = Character.Level;
				}
				PerkObject requiredPerk;
				bool flag3 = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, Character, characterObject, out requiredPerk);
				int b = (flag3 ? Troop.Number : 0);
				bool flag4 = true;
				int numOfCategoryItemPartyHas = GetNumOfCategoryItemPartyHas(_partyScreenLogic.RightOwnerParty.ItemRoster, characterObject.UpgradeRequiresItemFromCategory);
				if (characterObject.UpgradeRequiresItemFromCategory != null)
				{
					flag4 = numOfCategoryItemPartyHas > 0;
				}
				bool flag5 = Hero.MainHero.Gold + _partyScreenLogic.CurrentData.PartyGoldChangeAmount >= upgradeGoldCost;
				flag = level >= Character.Level && Troop.Xp >= Character.GetUpgradeXpCost(PartyBase.MainParty, i);
				flag2 = !(flag4 && flag5);
				int a = Troop.Number;
				if (upgradeGoldCost > 0)
				{
					a = (int)TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Floor((float)(Hero.MainHero.Gold + _partyScreenLogic.CurrentData.PartyGoldChangeAmount) / (float)upgradeGoldCost), 0f, Troop.Number);
				}
				int b2 = ((characterObject.UpgradeRequiresItemFromCategory != null) ? numOfCategoryItemPartyHas : Troop.Number);
				int num2 = (flag ? ((int)TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Floor((float)Troop.Xp / (float)Character.GetUpgradeXpCost(PartyBase.MainParty, i)), 0f, Troop.Number)) : 0);
				num = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Min(a, b2), TaleWorlds.Library.MathF.Min(num2, b));
				if (Character.Culture.IsBandit)
				{
					flag2 = flag2 || !Campaign.Current.Models.PartyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(PartyBase.MainParty, Character, characterObject);
					num = (flag ? num : 0);
				}
				flag = flag && !_partyVm.PartyScreenLogic.IsTroopUpgradesDisabled;
				string upgradeHint = CampaignUIHelper.GetUpgradeHint(i, numOfCategoryItemPartyHas, num, upgradeGoldCost, flag3, requiredPerk, Character, Troop, _partyScreenLogic.CurrentData.PartyGoldChangeAmount, _partyVm.PartyScreenLogic.IsTroopUpgradesDisabled);
				Upgrades[i].Refresh(num, flag, flag2, flag4, flag3, upgradeHint, !Character.IsHero && Character.IsMariner);
				if (i == 0)
				{
					UpgradeCostText = upgradeGoldCost.ToString();
					HasEnoughGold = flag5;
					NumOfReadyToUpgradeTroops = num2;
					MaxXP = Character.GetUpgradeXpCost(PartyBase.MainParty, i);
					CurrentXP = ((Troop.Xp >= Troop.Number * MaxXP) ? MaxXP : (Troop.Xp % MaxXP));
				}
			}
			AnyUpgradeHasRequirement = Upgrades.Any((UpgradeTargetVM x) => x.Requirements.HasItemRequirement || x.Requirements.HasPerkRequirement);
		}
		int num3 = 0;
		foreach (UpgradeTargetVM upgrade in Upgrades)
		{
			if (upgrade.AvailableUpgrades > num3)
			{
				num3 = upgrade.AvailableUpgrades;
			}
		}
		NumOfUpgradeableTroops = num3;
		IsTroopUpgradable = NumOfUpgradeableTroops > 0 && !_partyVm.PartyScreenLogic.IsTroopUpgradesDisabled;
		GameTexts.SetVariable("LEFT", NumOfReadyToUpgradeTroops);
		GameTexts.SetVariable("RIGHT", Troop.Number);
		StrNumOfUpgradableTroop = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		OnPropertyChanged("AmountOfUpgrades");
	}

	public void OnTransferred()
	{
		if (Side == PartyScreenLogic.PartyRosterSide.Left && !IsPrisoner)
		{
			_partyVm.MainPartyTroops.FirstOrDefault((PartyCharacterVM x) => x.Character == Character)?.InitializeUpgrades();
		}
		else
		{
			InitializeUpgrades();
		}
	}

	public void ThrowOnPropertyChanged()
	{
		OnPropertyChanged("Name");
		OnPropertyChanged("Number");
		OnPropertyChanged("WoundedCount");
		OnPropertyChanged("IsTroopTransferrable");
		OnPropertyChanged("MaxCount");
		OnPropertyChanged("AmountOfUpgrades");
		OnPropertyChanged("Level");
		OnPropertyChanged("PartyIndex");
		OnPropertyChanged("Index");
		OnPropertyChanged("TroopNum");
		OnPropertyChanged("TransferString");
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (!(obj is PartyCharacterVM partyCharacterVM))
		{
			return false;
		}
		if (partyCharacterVM.Character == null && Code == null)
		{
			return true;
		}
		return partyCharacterVM.Character == Character;
	}

	private void ApplyTransfer(int transferAmount, PartyScreenLogic.PartyRosterSide side)
	{
		OnTransfer(this, -1, transferAmount, side);
		ThrowOnPropertyChanged();
		UpdateTalkable();
	}

	private void ExecuteTransfer()
	{
		ApplyTransfer(TransferAmount, Side);
	}

	private void ExecuteTransferAll()
	{
		ApplyTransfer(Troop.Number, Side);
	}

	public void ExecuteSetFocused()
	{
		OnFocus?.Invoke(this);
	}

	public void ExecuteSetUnfocused()
	{
		OnFocus?.Invoke(null);
	}

	public void ExecuteTransferSingle()
	{
		int transferAmount = 1;
		if (_partyVm.IsEntireStackModifierActive)
		{
			transferAmount = Troop.Number;
		}
		else if (_partyVm.IsFiveStackModifierActive)
		{
			transferAmount = TaleWorlds.Library.MathF.Min(5, Troop.Number);
		}
		ApplyTransfer(transferAmount, Side);
		_partyVm.ExecuteRemoveZeroCounts();
	}

	public void ExecuteResetTrade()
	{
		TradeData.ExecuteReset();
	}

	public void Upgrade(int upgradeIndex, int maxUpgradeCount)
	{
		_partyVm?.ExecuteUpgrade(this, upgradeIndex, maxUpgradeCount);
	}

	public void FocusUpgrade(UpgradeTargetVM upgrade)
	{
		_partyVm.CurrentFocusedUpgrade = upgrade;
	}

	public void RecruitAll()
	{
		if (IsTroopRecruitable)
		{
			_partyVm.ExecuteRecruit(this, recruitAll: true);
		}
	}

	public void ExecuteRecruitTroop()
	{
		if (IsTroopRecruitable)
		{
			_partyVm.ExecuteRecruit(this);
		}
	}

	public void ExecuteExecuteTroop()
	{
		if (IsExecutable && FaceGen.GetMaturityTypeWithAge(Character.HeroObject.BodyProperties.Age) > BodyMeshMaturityType.Tween)
		{
			MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForPlayerExecutingHero(Character.HeroObject, delegate
			{
				_partyVm.ExecuteExecution();
			}));
		}
	}

	public void ExecuteOpenTroopEncyclopedia()
	{
		if (!Troop.Character.IsHero)
		{
			if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject)).IsValidEncyclopediaItem(Troop.Character))
			{
				Campaign.Current.EncyclopediaManager.GoToLink(Troop.Character.EncyclopediaLink);
			}
		}
		else if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero)).IsValidEncyclopediaItem(Troop.Character.HeroObject))
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Troop.Character.HeroObject.EncyclopediaLink);
		}
	}

	private CharacterCode GetCharacterCode(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side)
	{
		IFaction faction = null;
		if (type != PartyScreenLogic.TroopType.Prisoner)
		{
			if (side == PartyScreenLogic.PartyRosterSide.Left && _partyScreenLogic.LeftOwnerParty != null)
			{
				faction = _partyScreenLogic.LeftOwnerParty.MapFaction;
			}
			else if (Side == PartyScreenLogic.PartyRosterSide.Right && _partyScreenLogic.RightOwnerParty != null)
			{
				faction = _partyScreenLogic.RightOwnerParty.MapFaction;
			}
		}
		uint color = Color.White.ToUnsignedInteger();
		uint color2 = Color.White.ToUnsignedInteger();
		if (faction != null)
		{
			color = faction.Color;
			color2 = faction.Color2;
		}
		else if (character.Culture != null)
		{
			color = character.Culture.Color;
			color2 = character.Culture.Color2;
		}
		string equipmentCode = character.Equipment?.CalculateEquipmentCode();
		BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment);
		return CharacterCode.CreateFrom(equipmentCode, bodyProperties, character.IsFemale, character.IsHero, color, color2, character.DefaultFormationClass, character.Race);
	}

	private void SetMoraleCost()
	{
		if (IsTroopRecruitable)
		{
			RecruitMoraleCostText = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetPrisonerRecruitmentMoraleEffect(_partyScreenLogic.RightOwnerParty, Character, 1).ToString();
		}
	}

	public void SetIsUpgradeButtonHighlighted(bool isHighlighted)
	{
		Upgrades?.ApplyActionOnAllItems(delegate(UpgradeTargetVM x)
		{
			x.IsHighlighted = isHighlighted;
		});
	}

	public int GetNumOfCategoryItemPartyHas(ItemRoster items, ItemCategory itemCategory)
	{
		int num = 0;
		foreach (ItemRosterElement item in items)
		{
			if (item.EquipmentElement.Item.ItemCategory == itemCategory)
			{
				num += item.Amount;
			}
		}
		return num;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
