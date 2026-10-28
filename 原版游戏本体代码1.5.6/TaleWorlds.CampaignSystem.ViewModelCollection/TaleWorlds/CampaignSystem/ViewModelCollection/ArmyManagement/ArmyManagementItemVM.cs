using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;

public class ArmyManagementItemVM : ViewModel
{
	private readonly Action<ArmyManagementItemVM> _onAddToCart;

	private readonly Action<ArmyManagementItemVM> _onRemove;

	private readonly Action<ArmyManagementItemVM> _onFocus;

	public readonly MobileParty Party;

	private const float _minimumPartySizeScoreNeeded = 0.4f;

	public bool CanJoinBackWithoutCost;

	private TextObject _eligibilityReason;

	private InputKeyItemVM _removeInputKey;

	private BannerImageIdentifierVM _clanBanner;

	private CharacterImageIdentifierVM _lordFace;

	private string _nameText;

	private string _inArmyText;

	private string _leaderNameText;

	private int _relation = -102;

	private int _strength = -1;

	private int _shipCount = -1;

	private bool _hasShip;

	private string _distanceText;

	private int _cost = -1;

	private bool _isCostRelevant;

	private bool _isEligible;

	private bool _isMainHero;

	private bool _isInCart;

	private bool _isAlreadyWithPlayer;

	private bool _isTransferDisabled;

	private bool _isFocused;

	public float DistInTime { get; }

	public float _distance { get; }

	public Clan Clan { get; }

	[DataSourceProperty]
	public InputKeyItemVM RemoveInputKey
	{
		get
		{
			return _removeInputKey;
		}
		set
		{
			if (value != _removeInputKey)
			{
				_removeInputKey = value;
				OnPropertyChangedWithValue(value, "RemoveInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEligible
	{
		get
		{
			return _isEligible;
		}
		set
		{
			if (value != _isEligible)
			{
				_isEligible = value;
				OnPropertyChangedWithValue(value, "IsEligible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInCart
	{
		get
		{
			return _isInCart;
		}
		set
		{
			if (value != _isInCart)
			{
				_isInCart = value;
				OnPropertyChangedWithValue(value, "IsInCart");
				UpdateIsCostRelevant();
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
	public int Strength
	{
		get
		{
			return _strength;
		}
		set
		{
			if (value != _strength)
			{
				_strength = value;
				OnPropertyChangedWithValue(value, "Strength");
			}
		}
	}

	[DataSourceProperty]
	public int ShipCount
	{
		get
		{
			return _shipCount;
		}
		set
		{
			if (value != _shipCount)
			{
				_shipCount = value;
				OnPropertyChangedWithValue(value, "ShipCount");
				HasShip = _shipCount > 0;
			}
		}
	}

	[DataSourceProperty]
	public bool HasShip
	{
		get
		{
			return _hasShip;
		}
		set
		{
			if (value != _hasShip)
			{
				_hasShip = value;
				OnPropertyChangedWithValue(value, "HasShip");
			}
		}
	}

	[DataSourceProperty]
	public string DistanceText
	{
		get
		{
			return _distanceText;
		}
		set
		{
			if (value != _distanceText)
			{
				_distanceText = value;
				OnPropertyChangedWithValue(value, "DistanceText");
			}
		}
	}

	[DataSourceProperty]
	public string InArmyText
	{
		get
		{
			return _inArmyText;
		}
		set
		{
			if (value != _inArmyText)
			{
				_inArmyText = value;
				OnPropertyChangedWithValue(value, "InArmyText");
			}
		}
	}

	[DataSourceProperty]
	public int Cost
	{
		get
		{
			return _cost;
		}
		set
		{
			if (value != _cost)
			{
				_cost = value;
				OnPropertyChangedWithValue(value, "Cost");
				UpdateIsCostRelevant();
			}
		}
	}

	[DataSourceProperty]
	public bool IsCostRelevant
	{
		get
		{
			return _isCostRelevant;
		}
		set
		{
			if (value != _isCostRelevant)
			{
				_isCostRelevant = value;
				OnPropertyChangedWithValue(value, "IsCostRelevant");
			}
		}
	}

	[DataSourceProperty]
	public int Relation
	{
		get
		{
			return _relation;
		}
		set
		{
			if (value != _relation)
			{
				_relation = value;
				OnPropertyChangedWithValue(value, "Relation");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM LordFace
	{
		get
		{
			return _lordFace;
		}
		set
		{
			if (value != _lordFace)
			{
				_lordFace = value;
				OnPropertyChangedWithValue(value, "LordFace");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAlreadyWithPlayer
	{
		get
		{
			return _isAlreadyWithPlayer;
		}
		set
		{
			if (value != _isAlreadyWithPlayer)
			{
				_isAlreadyWithPlayer = value;
				OnPropertyChangedWithValue(value, "IsAlreadyWithPlayer");
				UpdateIsCostRelevant();
			}
		}
	}

	[DataSourceProperty]
	public bool IsTransferDisabled
	{
		get
		{
			return _isTransferDisabled;
		}
		set
		{
			if (value != _isTransferDisabled)
			{
				_isTransferDisabled = value;
				OnPropertyChangedWithValue(value, "IsTransferDisabled");
			}
		}
	}

	[DataSourceProperty]
	public string LeaderNameText
	{
		get
		{
			return _leaderNameText;
		}
		set
		{
			if (value != _leaderNameText)
			{
				_leaderNameText = value;
				OnPropertyChangedWithValue(value, "LeaderNameText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
			}
		}
	}

	public ArmyManagementItemVM(Action<ArmyManagementItemVM> onAddToCart, Action<ArmyManagementItemVM> onRemove, Action<ArmyManagementItemVM> onFocus, MobileParty mobileParty)
	{
		ArmyManagementCalculationModel armyManagementCalculationModel = Campaign.Current.Models.ArmyManagementCalculationModel;
		_onAddToCart = onAddToCart;
		_onRemove = onRemove;
		_onFocus = onFocus;
		Party = mobileParty;
		_eligibilityReason = TextObject.GetEmpty();
		ClanBanner = new BannerImageIdentifierVM(mobileParty.LeaderHero.ClanBanner, nineGrid: true);
		CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(mobileParty.LeaderHero.CharacterObject);
		LordFace = new CharacterImageIdentifierVM(characterCode);
		Relation = armyManagementCalculationModel.GetPartyRelation(mobileParty.LeaderHero);
		Strength = Party.Party.NumberOfHealthyMembers;
		ShipCount = Party.Ships.Count;
		_distance = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(Party, MobileParty.MainParty, Party.NavigationCapability);
		if (MobileParty.MainParty.IsCurrentlyAtSea && !Party.HasNavalNavigationCapability)
		{
			DistInTime = 2.1474836E+09f;
		}
		else
		{
			DistInTime = TaleWorlds.Library.MathF.Ceiling(_distance / Party.Speed);
			Cost = armyManagementCalculationModel.CalculatePartyInfluenceCost(MobileParty.MainParty, mobileParty);
		}
		Clan = mobileParty.LeaderHero.Clan;
		IsMainHero = mobileParty.IsMainParty;
		UpdateEligibility();
		IsTransferDisabled = IsMainHero || PlayerSiege.PlayerSiegeEvent != null;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		InArmyText = GameTexts.FindText("str_in_army").ToString();
		LeaderNameText = Party.LeaderHero.Name.ToString();
		NameText = Party.Name.ToString();
		if (!Party.IsMainParty)
		{
			DistanceText = (((int)_distance < 5) ? GameTexts.FindText("str_nearby").ToString() : CampaignUIHelper.GetPartyDistanceByTimeTextAbbreviated((int)_distance, Party.Speed));
		}
	}

	public void ExecuteAction()
	{
		if (IsInCart)
		{
			OnRemove();
		}
		else
		{
			OnAddToCart();
		}
	}

	private void OnRemove()
	{
		if (!IsMainHero)
		{
			_onRemove(this);
			UpdateEligibility();
		}
	}

	private void OnAddToCart()
	{
		UpdateEligibility();
		if (IsEligible)
		{
			_onAddToCart(this);
		}
		UpdateEligibility();
	}

	public void ExecuteSetFocused()
	{
		IsFocused = true;
		_onFocus?.Invoke(this);
	}

	public void ExecuteSetUnfocused()
	{
		IsFocused = false;
		_onFocus?.Invoke(null);
	}

	public void UpdateEligibility()
	{
		ArmyManagementCalculationModel armyManagementCalculationModel = Campaign.Current.Models?.ArmyManagementCalculationModel;
		bool flag = true;
		_eligibilityReason = TextObject.GetEmpty();
		if (!CanJoinBackWithoutCost)
		{
			if (IsInCart && !IsAlreadyWithPlayer)
			{
				flag = false;
				_eligibilityReason = new TextObject("{=idRXFzQ6}Already added to the army.");
			}
			else
			{
				flag = armyManagementCalculationModel.CheckPartyEligibility(Party, out _eligibilityReason);
				if (flag)
				{
					flag = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out _eligibilityReason);
				}
			}
		}
		IsEligible = flag;
	}

	private void UpdateIsCostRelevant()
	{
		if (Cost == 0 && IsAlreadyWithPlayer && IsInCart)
		{
			IsCostRelevant = false;
		}
		else
		{
			IsCostRelevant = true;
		}
	}

	public void ExecuteBeginHint()
	{
		if (!IsEligible)
		{
			MBInformationManager.ShowHint(_eligibilityReason.ToString());
			return;
		}
		InformationManager.ShowTooltip(typeof(MobileParty), Party, true, true);
	}

	public void ExecuteBeginClanHint()
	{
		InformationManager.ShowTooltip(typeof(Clan), Party?.ActualClan, true, true);
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteOpenEncyclopedia()
	{
		if (Party?.LeaderHero != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Party.LeaderHero.EncyclopediaLink);
		}
	}

	public void ExecuteOpenClanEncyclopedia()
	{
		if (Party?.ActualClan != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Party.ActualClan.EncyclopediaLink);
		}
	}
}
