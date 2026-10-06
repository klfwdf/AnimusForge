using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;

public class KingdomSettlementItemVM : KingdomItemVM
{
	private readonly Action<KingdomSettlementItemVM> _onSelect;

	public readonly Settlement Settlement;

	private string _iconPath;

	private string _name;

	private string _imageName;

	private string _settlementImagePath;

	private string _governorName;

	private BannerImageIdentifierVM _ownerClanBanner;

	private BannerImageIdentifierVM _ownerClanBanner_9;

	private HeroVM _owner;

	private MBBindingList<SelectableFiefItemPropertyVM> _itemProperties;

	private MBBindingList<KingdomSettlementVillageItemVM> _villages;

	private int _wallLevel;

	private int _prosperity;

	private int _defenders;

	public int Garrison { get; private set; }

	public int Militia { get; private set; }

	[DataSourceProperty]
	public MBBindingList<SelectableFiefItemPropertyVM> ItemProperties
	{
		get
		{
			return _itemProperties;
		}
		set
		{
			if (value != _itemProperties)
			{
				_itemProperties = value;
				OnPropertyChangedWithValue(value, "ItemProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomSettlementVillageItemVM> Villages
	{
		get
		{
			return _villages;
		}
		set
		{
			if (value != _villages)
			{
				_villages = value;
				OnPropertyChangedWithValue(value, "Villages");
			}
		}
	}

	[DataSourceProperty]
	public string IconPath
	{
		get
		{
			return _iconPath;
		}
		set
		{
			if (value != _iconPath)
			{
				_iconPath = value;
				OnPropertyChangedWithValue(value, "IconPath");
			}
		}
	}

	[DataSourceProperty]
	public int Defenders
	{
		get
		{
			return _defenders;
		}
		set
		{
			if (value != _defenders)
			{
				_defenders = value;
				OnPropertyChangedWithValue(value, "Defenders");
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
	public string ImageName
	{
		get
		{
			return _imageName;
		}
		set
		{
			if (value != _imageName)
			{
				_imageName = value;
				OnPropertyChangedWithValue(value, "ImageName");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementImagePath
	{
		get
		{
			return _settlementImagePath;
		}
		set
		{
			if (value != _settlementImagePath)
			{
				_settlementImagePath = value;
				OnPropertyChangedWithValue(value, "SettlementImagePath");
			}
		}
	}

	[DataSourceProperty]
	public string GovernorName
	{
		get
		{
			return _governorName;
		}
		set
		{
			if (value != _governorName)
			{
				_governorName = value;
				OnPropertyChangedWithValue(value, "GovernorName");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM OwnerClanBanner
	{
		get
		{
			return _ownerClanBanner;
		}
		set
		{
			if (value != _ownerClanBanner)
			{
				_ownerClanBanner = value;
				OnPropertyChangedWithValue(value, "OwnerClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM OwnerClanBanner_9
	{
		get
		{
			return _ownerClanBanner_9;
		}
		set
		{
			if (value != _ownerClanBanner_9)
			{
				_ownerClanBanner_9 = value;
				OnPropertyChangedWithValue(value, "OwnerClanBanner_9");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Owner
	{
		get
		{
			return _owner;
		}
		set
		{
			if (value != _owner)
			{
				_owner = value;
				OnPropertyChangedWithValue(value, "Owner");
			}
		}
	}

	[DataSourceProperty]
	public int WallLevel
	{
		get
		{
			return _wallLevel;
		}
		set
		{
			if (value != _wallLevel)
			{
				_wallLevel = value;
				OnPropertyChangedWithValue(value, "WallLevel");
			}
		}
	}

	[DataSourceProperty]
	public int Prosperity
	{
		get
		{
			return _prosperity;
		}
		set
		{
			if (value != _prosperity)
			{
				_prosperity = value;
				OnPropertyChangedWithValue(value, "Prosperity");
			}
		}
	}

	public KingdomSettlementItemVM(Settlement settlement, Action<KingdomSettlementItemVM> onSelect)
	{
		Settlement = settlement;
		_onSelect = onSelect;
		Name = settlement.Name.ToString();
		Villages = new MBBindingList<KingdomSettlementVillageItemVM>();
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		SettlementImagePath = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
		ItemProperties = new MBBindingList<SelectableFiefItemPropertyVM>();
		ImageName = ((settlementComponent != null) ? settlementComponent.WaitMeshName : "");
		Owner = new HeroVM(settlement.OwnerClan.Leader);
		OwnerClanBanner = new BannerImageIdentifierVM(Settlement.OwnerClan.Banner);
		OwnerClanBanner_9 = new BannerImageIdentifierVM(Settlement.OwnerClan.Banner, nineGrid: true);
		Town town = settlement.Town;
		WallLevel = town?.GetWallLevel() ?? (-1);
		if (town != null)
		{
			Prosperity = TaleWorlds.Library.MathF.Round(town.Prosperity);
			IconPath = town.BackgroundMeshName;
		}
		else if (settlement.IsCastle)
		{
			Prosperity = TaleWorlds.Library.MathF.Round(settlement.Town.Prosperity);
			IconPath = "";
		}
		foreach (Village boundVillage in Settlement.BoundVillages)
		{
			Villages.Add(new KingdomSettlementVillageItemVM(boundVillage));
		}
		Defenders = ((!Settlement.IsFortification) ? ((int)Settlement.Militia) : (Settlement.Town.GarrisonParty?.Party.NumberOfAllMembers ?? 0));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Villages.ApplyActionOnAllItems(delegate(KingdomSettlementVillageItemVM x)
		{
			x.RefreshValues();
		});
		UpdateProperties();
	}

	protected virtual void UpdateProperties()
	{
		ItemProperties.Clear();
		if (Settlement.Town != null)
		{
			BasicTooltipViewModel hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(Settlement.Town));
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_walls").ToString(), Settlement.Town.GetWallLevel().ToString(), 0, SelectableItemPropertyVM.PropertyType.Wall, hint));
			BasicTooltipViewModel hint2 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(Settlement.Town));
			int changeAmount = (int)SettlementHelper.GetGarrisonChangeExplainedNumber(Settlement.Town).ResultNumber;
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_garrison").ToString(), Settlement.Town.GarrisonParty?.Party.NumberOfAllMembers.ToString() ?? "0", changeAmount, SelectableItemPropertyVM.PropertyType.Garrison, hint2));
		}
		int num = (int)Settlement.Militia;
		List<TooltipProperty> militiaHint = (Settlement.IsVillage ? CampaignUIHelper.GetVillageMilitiaTooltip(Settlement.Village) : CampaignUIHelper.GetTownMilitiaTooltip(Settlement.Town));
		int changeAmount2 = ((Settlement.Town != null) ? ((int)Settlement.Town.MilitiaChange) : ((int)Settlement.Village.MilitiaChange));
		ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_militia").ToString(), num.ToString(), changeAmount2, SelectableItemPropertyVM.PropertyType.Militia, new BasicTooltipViewModel(() => militiaHint)));
		if (Settlement.Town != null)
		{
			BasicTooltipViewModel hint3 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(Settlement.Town));
			int changeAmount3 = (int)Settlement.Town.FoodChange;
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_food_stocks").ToString(), ((int)Settlement.Town.FoodStocks).ToString(), changeAmount3, SelectableItemPropertyVM.PropertyType.Food, hint3));
		}
		int changeAmount4 = ((Settlement.Town != null) ? ((int)Settlement.Town.ProsperityChange) : ((int)Settlement.Village.HearthChange));
		if (Settlement.IsFortification)
		{
			BasicTooltipViewModel hint4 = ((Settlement.Town == null) ? new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageProsperityTooltip(Settlement.Village)) : new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(Settlement.Town)));
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_prosperity").ToString(), $"{Settlement.Town.Prosperity:0.#}", changeAmount4, SelectableItemPropertyVM.PropertyType.Prosperity, hint4));
		}
		if (Settlement.Town != null)
		{
			BasicTooltipViewModel hint5 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(Settlement.Town));
			int changeAmount5 = (int)Settlement.Town.LoyaltyChange;
			bool isWarning = Settlement.IsTown && Settlement.Town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_loyalty").ToString(), $"{Settlement.Town.Loyalty:0.#}", changeAmount5, SelectableItemPropertyVM.PropertyType.Loyalty, hint5, isWarning));
			BasicTooltipViewModel hint6 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(Settlement.Town));
			int changeAmount6 = (int)Settlement.Town.SecurityChange;
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_security").ToString(), $"{Settlement.Town.Security:0.#}", changeAmount6, SelectableItemPropertyVM.PropertyType.Security, hint6));
		}
		if (Settlement.IsTown)
		{
			BasicTooltipViewModel hint7 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownPatrolTooltip(Settlement.Town));
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_patrol").ToString(), Campaign.Current.GetCampaignBehavior<IPatrolPartiesCampaignBehavior>().GetSettlementPatrolStatus(Settlement).ToString(), 0, SelectableItemPropertyVM.PropertyType.Patrol, hint7));
		}
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		_onSelect(this);
	}

	private void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Settlement), Settlement, true);
	}

	private void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteLink()
	{
		if (Settlement != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Settlement.EncyclopediaLink);
		}
	}
}
