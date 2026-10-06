using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

public class MapInfoVM : ViewModel
{
	private IViewDataTracker _viewDataTracker;

	private MapInfoItemVM _goldInfo;

	private MapInfoItemVM _influenceInfo;

	private MapInfoItemVM _hitPointsInfo;

	private MapInfoItemVM _troopsInfo;

	private MapInfoItemVM _foodInfo;

	private MapInfoItemVM _moraleInfo;

	private MapInfoItemVM _speedInfo;

	private MapInfoItemVM _viewDistanceInfo;

	private MapInfoItemVM _troopWageInfo;

	private bool _isMainHeroSick;

	private bool _isMainPartyAtSea;

	private bool _isInfoBarExtended;

	private bool _isInfoBarEnabled;

	private HintViewModel _extendHint;

	private MBBindingList<MapInfoItemVM> _primaryInfoItems;

	private MBBindingList<MapInfoItemVM> _secondaryInfoItems;

	[DataSourceProperty]
	public bool IsInfoBarExtended
	{
		get
		{
			return _isInfoBarExtended;
		}
		set
		{
			if (value != _isInfoBarExtended)
			{
				_isInfoBarExtended = value;
				_viewDataTracker.SetMapBarExtendedState(value);
				OnPropertyChangedWithValue(value, "IsInfoBarExtended");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInfoBarEnabled
	{
		get
		{
			return _isInfoBarEnabled;
		}
		set
		{
			if (value != _isInfoBarEnabled)
			{
				_isInfoBarEnabled = value;
				OnPropertyChangedWithValue(value, "IsInfoBarEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ExtendHint
	{
		get
		{
			return _extendHint;
		}
		set
		{
			if (value != _extendHint)
			{
				_extendHint = value;
				OnPropertyChangedWithValue(value, "ExtendHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MapInfoItemVM> PrimaryInfoItems
	{
		get
		{
			return _primaryInfoItems;
		}
		set
		{
			if (value != _primaryInfoItems)
			{
				_primaryInfoItems = value;
				OnPropertyChangedWithValue(value, "PrimaryInfoItems");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MapInfoItemVM> SecondaryInfoItems
	{
		get
		{
			return _secondaryInfoItems;
		}
		set
		{
			if (value != _secondaryInfoItems)
			{
				_secondaryInfoItems = value;
				OnPropertyChangedWithValue(value, "SecondaryInfoItems");
			}
		}
	}

	public MapInfoVM()
	{
		ExtendHint = new HintViewModel(GameTexts.FindText("str_map_extend_bar_hint"));
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		IsInfoBarExtended = _viewDataTracker.GetMapBarExtendedState();
		PrimaryInfoItems = new MBBindingList<MapInfoItemVM>();
		SecondaryInfoItems = new MBBindingList<MapInfoItemVM>();
		CreateItems();
		RefreshValues();
	}

	protected virtual void CreateItems()
	{
		PrimaryInfoItems.ApplyActionOnAllItems(delegate(MapInfoItemVM i)
		{
			i.OnFinalize();
		});
		PrimaryInfoItems.Clear();
		SecondaryInfoItems.ApplyActionOnAllItems(delegate(MapInfoItemVM i)
		{
			i.OnFinalize();
		});
		SecondaryInfoItems.Clear();
		_goldInfo = new MapInfoItemVM("gold", CampaignUIHelper.GetDenarTooltip());
		_influenceInfo = new MapInfoItemVM("influence", () => CampaignUIHelper.GetInfluenceTooltip(Clan.PlayerClan));
		_hitPointsInfo = new MapInfoItemVM("hit_points", () => CampaignUIHelper.GetPlayerHitpointsTooltip());
		_troopsInfo = new MapInfoItemVM("troops", () => CampaignUIHelper.GetMainPartyHealthTooltip());
		_foodInfo = new MapInfoItemVM("food", () => CampaignUIHelper.GetPartyFoodTooltip(MobileParty.MainParty));
		_moraleInfo = new MapInfoItemVM("morale", () => CampaignUIHelper.GetPartyMoraleTooltip(MobileParty.MainParty));
		_speedInfo = new MapInfoItemVM("speed", () => CampaignUIHelper.GetPartySpeedTooltip(considerArmySpeed: true));
		_viewDistanceInfo = new MapInfoItemVM("view_distance", () => CampaignUIHelper.GetViewDistanceTooltip());
		_troopWageInfo = new MapInfoItemVM("troop_wage", () => CampaignUIHelper.GetPartyWageTooltip(MobileParty.MainParty));
		PrimaryInfoItems.Add(_goldInfo);
		PrimaryInfoItems.Add(_speedInfo);
		PrimaryInfoItems.Add(_hitPointsInfo);
		PrimaryInfoItems.Add(_troopsInfo);
		PrimaryInfoItems.Add(_foodInfo);
		PrimaryInfoItems.Add(_moraleInfo);
		SecondaryInfoItems.Add(_influenceInfo);
		SecondaryInfoItems.Add(_viewDistanceInfo);
		SecondaryInfoItems.Add(_troopWageInfo);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		UpdatePlayerInfo(updateForced: true);
	}

	public void Tick()
	{
		bool flag = Hero.MainHero != null && Hero.IsMainHeroIll;
		if (_isMainHeroSick != flag)
		{
			_isMainHeroSick = flag;
			_hitPointsInfo.SetOverriddenVisualId(_isMainHeroSick ? "hit_points_sick" : null);
		}
		bool isCurrentlyAtSea = MobileParty.MainParty.IsCurrentlyAtSea;
		if (_isMainPartyAtSea != isCurrentlyAtSea)
		{
			_isMainPartyAtSea = isCurrentlyAtSea;
			_speedInfo.SetOverriddenVisualId(_isMainPartyAtSea ? "speed_at_sea" : null);
		}
		IsInfoBarEnabled = Hero.MainHero?.IsAlive ?? false;
	}

	public void Refresh()
	{
		UpdatePlayerInfo(updateForced: false);
	}

	protected virtual void UpdatePlayerInfo(bool updateForced)
	{
		ExplainedNumber explainedNumber = Campaign.Current.Models.ClanFinanceModel.CalculateClanGoldChange(Clan.PlayerClan, includeDescriptions: true, applyWithdrawals: false, includeDetails: true);
		_goldInfo.HasWarning = (float)Hero.MainHero.Gold + explainedNumber.ResultNumber < 0f;
		if (_goldInfo.IntValue != Hero.MainHero.Gold || updateForced)
		{
			_goldInfo.IntValue = Hero.MainHero.Gold;
			_goldInfo.Value = CampaignUIHelper.GetAbbreviatedValueTextFromValue(_goldInfo.IntValue);
		}
		_influenceInfo.HasWarning = Hero.MainHero.Clan.Influence < -100f;
		if (_influenceInfo.IntValue != (int)Hero.MainHero.Clan.Influence || updateForced)
		{
			_influenceInfo.IntValue = (int)Hero.MainHero.Clan.Influence;
			_influenceInfo.Value = CampaignUIHelper.GetAbbreviatedValueTextFromValue(_influenceInfo.IntValue);
		}
		float num = MathF.Round(MobileParty.MainParty.Morale, 1);
		_moraleInfo.HasWarning = MobileParty.MainParty.Morale < (float)Campaign.Current.Models.PartyDesertionModel.GetMoraleThresholdForTroopDesertion();
		if (_moraleInfo.FloatValue != num || updateForced)
		{
			_moraleInfo.Value = num.ToString();
			_moraleInfo.FloatValue = num;
			MBTextManager.SetTextVariable("BASE_EFFECT", num.ToString("0.0"));
		}
		int numDaysForFoodToLast = MobileParty.MainParty.GetNumDaysForFoodToLast();
		_foodInfo.HasWarning = numDaysForFoodToLast < 1;
		_foodInfo.IntValue = (int)((MobileParty.MainParty.Food > 0f) ? MobileParty.MainParty.Food : 0f);
		_foodInfo.Value = _foodInfo.IntValue.ToString();
		_troopsInfo.HasWarning = PartyBase.MainParty.PartySizeLimit < PartyBase.MainParty.NumberOfAllMembers || PartyBase.MainParty.PrisonerSizeLimit < PartyBase.MainParty.NumberOfPrisoners;
		_troopsInfo.IntValue = PartyBase.MainParty.MemberRoster.TotalManCount;
		_troopsInfo.Value = CampaignUIHelper.GetPartyNameplateText(PartyBase.MainParty);
		int num2 = (int)MathF.Clamp(Hero.MainHero.HitPoints * 100 / CharacterObject.PlayerCharacter.MaxHitPoints(), 1f, 100f);
		_hitPointsInfo.HasWarning = Hero.MainHero.IsWounded;
		if (_hitPointsInfo.IntValue != num2 || updateForced)
		{
			_hitPointsInfo.IntValue = num2;
			GameTexts.SetVariable("NUMBER", _hitPointsInfo.IntValue);
			_hitPointsInfo.Value = GameTexts.FindText("str_NUMBER_percent").ToString();
		}
		MobileParty mobileParty = MobileParty.MainParty.Army?.LeaderParty ?? MobileParty.MainParty;
		float num3 = ((mobileParty.IsActive && mobileParty.CurrentNavigationFace.IsValid()) ? mobileParty.Speed : 0f);
		if (_speedInfo.FloatValue != num3 || updateForced)
		{
			_speedInfo.FloatValue = num3;
			_speedInfo.Value = CampaignUIHelper.FloatToString(num3);
		}
		float seeingRange = MobileParty.MainParty.SeeingRange;
		if (_viewDistanceInfo.FloatValue != seeingRange || updateForced)
		{
			_viewDistanceInfo.FloatValue = seeingRange;
			_viewDistanceInfo.Value = CampaignUIHelper.FloatToString(seeingRange);
		}
		int totalWage = MobileParty.MainParty.TotalWage;
		if (_troopWageInfo.IntValue != totalWage || updateForced)
		{
			_troopWageInfo.IntValue = totalWage;
			_troopWageInfo.Value = totalWage.ToString();
		}
	}
}
