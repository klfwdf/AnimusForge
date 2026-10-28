using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanSettlementItemVM : ViewModel
{
	private readonly Action<ClanSettlementItemVM> _onSelection;

	private readonly Action _onShowSendMembers;

	private readonly ITeleportationCampaignBehavior _teleportationBehavior;

	private readonly IPatrolPartiesCampaignBehavior _patrolsBehavior;

	public readonly Settlement Settlement;

	private string _name;

	private HeroVM _governor;

	private string _fileName;

	private string _imageName;

	private string _villagesText;

	private string _notablesText;

	private string _membersText;

	private bool _isFortification;

	private bool _isSelected;

	private bool _hasGovernor;

	private bool _hasNotables;

	private bool _isSendMembersEnabled;

	private MBBindingList<SelectableFiefItemPropertyVM> _itemProperties;

	private MBBindingList<ProfitItemPropertyVM> _profitItemProperties;

	private ProfitItemPropertyVM _totalProfit;

	private MBBindingList<ClanSettlementItemVM> _villagesOwned;

	private MBBindingList<HeroVM> _notables;

	private MBBindingList<HeroVM> _members;

	private HintViewModel _sendMembersHint;

	[DataSourceProperty]
	public HeroVM Governor
	{
		get
		{
			return _governor;
		}
		set
		{
			if (value != _governor)
			{
				_governor = value;
				OnPropertyChangedWithValue(value, "Governor");
			}
		}
	}

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
	public MBBindingList<ProfitItemPropertyVM> ProfitItemProperties
	{
		get
		{
			return _profitItemProperties;
		}
		set
		{
			if (value != _profitItemProperties)
			{
				_profitItemProperties = value;
				OnPropertyChangedWithValue(value, "ProfitItemProperties");
			}
		}
	}

	[DataSourceProperty]
	public ProfitItemPropertyVM TotalProfit
	{
		get
		{
			return _totalProfit;
		}
		set
		{
			if (value != _totalProfit)
			{
				_totalProfit = value;
				OnPropertyChangedWithValue(value, "TotalProfit");
			}
		}
	}

	[DataSourceProperty]
	public string FileName
	{
		get
		{
			return _fileName;
		}
		set
		{
			if (value != _fileName)
			{
				_fileName = value;
				OnPropertyChangedWithValue(value, "FileName");
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
	public string VillagesText
	{
		get
		{
			return _villagesText;
		}
		set
		{
			if (value != _villagesText)
			{
				_villagesText = value;
				OnPropertyChangedWithValue(value, "VillagesText");
			}
		}
	}

	[DataSourceProperty]
	public string NotablesText
	{
		get
		{
			return _notablesText;
		}
		set
		{
			if (value != _notablesText)
			{
				_notablesText = value;
				OnPropertyChangedWithValue(value, "NotablesText");
			}
		}
	}

	[DataSourceProperty]
	public string MembersText
	{
		get
		{
			return _membersText;
		}
		set
		{
			if (value != _membersText)
			{
				_membersText = value;
				OnPropertyChangedWithValue(value, "MembersText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFortification
	{
		get
		{
			return _isFortification;
		}
		set
		{
			if (value != _isFortification)
			{
				_isFortification = value;
				OnPropertyChangedWithValue(value, "IsFortification");
			}
		}
	}

	[DataSourceProperty]
	public bool HasGovernor
	{
		get
		{
			return _hasGovernor;
		}
		set
		{
			if (value != _hasGovernor)
			{
				_hasGovernor = value;
				OnPropertyChangedWithValue(value, "HasGovernor");
			}
		}
	}

	[DataSourceProperty]
	public bool HasNotables
	{
		get
		{
			return _hasNotables;
		}
		set
		{
			if (value != _hasNotables)
			{
				_hasNotables = value;
				OnPropertyChangedWithValue(value, "HasNotables");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSendMembersEnabled
	{
		get
		{
			return _isSendMembersEnabled;
		}
		set
		{
			if (value != _isSendMembersEnabled)
			{
				_isSendMembersEnabled = value;
				OnPropertyChangedWithValue(value, "IsSendMembersEnabled");
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
	public MBBindingList<ClanSettlementItemVM> VillagesOwned
	{
		get
		{
			return _villagesOwned;
		}
		set
		{
			if (value != _villagesOwned)
			{
				_villagesOwned = value;
				OnPropertyChangedWithValue(value, "VillagesOwned");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Notables
	{
		get
		{
			return _notables;
		}
		set
		{
			if (value != _notables)
			{
				_notables = value;
				OnPropertyChangedWithValue(value, "Notables");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Members
	{
		get
		{
			return _members;
		}
		set
		{
			if (value != _members)
			{
				_members = value;
				OnPropertyChangedWithValue(value, "Members");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SendMembersHint
	{
		get
		{
			return _sendMembersHint;
		}
		set
		{
			if (value != _sendMembersHint)
			{
				_sendMembersHint = value;
				OnPropertyChangedWithValue(value, "SendMembersHint");
			}
		}
	}

	public ClanSettlementItemVM(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
	{
		Settlement = settlement;
		_onSelection = onSelection;
		_onShowSendMembers = onShowSendMembers;
		_teleportationBehavior = teleportationBehavior;
		IsFortification = settlement.IsFortification;
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		FileName = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
		ItemProperties = new MBBindingList<SelectableFiefItemPropertyVM>();
		ProfitItemProperties = new MBBindingList<ProfitItemPropertyVM>();
		TotalProfit = new ProfitItemPropertyVM(GameTexts.FindText("str_profit").ToString(), 0);
		ImageName = ((settlementComponent != null) ? settlementComponent.WaitMeshName : "");
		VillagesOwned = new MBBindingList<ClanSettlementItemVM>();
		Notables = new MBBindingList<HeroVM>();
		Members = new MBBindingList<HeroVM>();
		_patrolsBehavior = Campaign.Current.GetCampaignBehavior<IPatrolPartiesCampaignBehavior>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		VillagesText = GameTexts.FindText("str_villages").ToString();
		NotablesText = GameTexts.FindText("str_center_notables").ToString();
		MembersText = GameTexts.FindText("str_members").ToString();
		Name = Settlement.Name.ToString();
		UpdateProperties();
	}

	protected virtual ClanSettlementItemVM CreateSettlementItem(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
	{
		return new ClanSettlementItemVM(settlement, onSelection, onShowSendMembers, teleportationBehavior);
	}

	public void OnSettlementSelection()
	{
		_onSelection(this);
	}

	public void ExecuteLink()
	{
		MBInformationManager.HideInformations();
		Campaign.Current.EncyclopediaManager.GoToLink(Settlement.EncyclopediaLink);
	}

	public void ExecuteCloseTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteOpenTooltip()
	{
		InformationManager.ShowTooltip(typeof(Settlement), Settlement);
	}

	public void ExecuteSendMembers()
	{
		_onShowSendMembers?.Invoke();
	}

	private void OnGovernorChanged(Hero oldHero, Hero newHero)
	{
		ChangeGovernorAction.Apply(Settlement.Town, newHero);
	}

	private bool IsGovernorAssignable(Hero oldHero, Hero newHero)
	{
		if (newHero.IsActive)
		{
			return newHero.GovernorOf == null;
		}
		return false;
	}

	protected virtual void UpdateProperties()
	{
		ItemProperties.Clear();
		VillagesOwned.Clear();
		Notables.Clear();
		Members.Clear();
		foreach (Village boundVillage in Settlement.BoundVillages)
		{
			VillagesOwned.Add(CreateSettlementItem(boundVillage.Settlement, null, null, null));
		}
		HasNotables = !Settlement.Notables.IsEmpty();
		foreach (Hero notable in Settlement.Notables)
		{
			Notables.Add(new HeroVM(notable));
		}
		foreach (Hero item in Settlement.HeroesWithoutParty.Where((Hero h) => h.Clan == Clan.PlayerClan))
		{
			Members.Add(new HeroVM(item));
		}
		HasGovernor = false;
		if (!Settlement.IsVillage)
		{
			Hero hero = ((Settlement.Town?.Governor != null) ? Settlement.Town.Governor : CampaignUIHelper.GetTeleportingGovernor(Settlement, _teleportationBehavior));
			HasGovernor = hero != null;
			Governor = (HasGovernor ? new HeroVM(hero) : null);
		}
		IsFortification = Settlement.IsFortification;
		if (Settlement.Town != null)
		{
			BasicTooltipViewModel hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(Settlement.Town));
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_walls").ToString(), Settlement.Town.GetWallLevel().ToString(), 0, SelectableItemPropertyVM.PropertyType.Wall, hint));
		}
		if (Settlement.Town != null)
		{
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
		if (Settlement.IsFortification)
		{
			int changeAmount4 = ((Settlement.Town != null) ? ((int)Settlement.Town.ProsperityChange) : ((int)Settlement.Village.HearthChange));
			BasicTooltipViewModel hint4 = ((Settlement.Town == null) ? new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageProsperityTooltip(Settlement.Village)) : new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(Settlement.Town)));
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_prosperity").ToString(), $"{Settlement.Town.Prosperity:0.##}", changeAmount4, SelectableItemPropertyVM.PropertyType.Prosperity, hint4));
		}
		if (Settlement.Town != null)
		{
			BasicTooltipViewModel hint5 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(Settlement.Town));
			int changeAmount5 = (int)Settlement.Town.LoyaltyChange;
			bool isWarning = Settlement.IsTown && Settlement.Town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_loyalty").ToString(), $"{Settlement.Town.Loyalty:0.#}", changeAmount5, SelectableItemPropertyVM.PropertyType.Loyalty, hint5, isWarning));
		}
		if (Settlement.Town != null)
		{
			BasicTooltipViewModel hint6 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(Settlement.Town));
			int changeAmount6 = (int)Settlement.Town.SecurityChange;
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_security").ToString(), $"{Settlement.Town.Security:0.#}", changeAmount6, SelectableItemPropertyVM.PropertyType.Security, hint6));
		}
		if (Settlement.IsTown)
		{
			BasicTooltipViewModel hint7 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownPatrolTooltip(Settlement.Town));
			ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_patrol").ToString(), _patrolsBehavior.GetSettlementPatrolStatus(Settlement).ToString(), 0, SelectableItemPropertyVM.PropertyType.Patrol, hint7));
		}
		IsSendMembersEnabled = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason);
		TextObject textObject = new TextObject("{=PTGsYoPc}Assign your clan members to {SETTLEMENT_NAME}");
		textObject.SetTextVariable("SETTLEMENT_NAME", Settlement.Name.ToString());
		SendMembersHint = new HintViewModel(IsSendMembersEnabled ? textObject : disabledReason);
		UpdateProfitProperties();
	}

	protected virtual void UpdateProfitProperties()
	{
		ProfitItemProperties.Clear();
		if (Settlement.Town == null)
		{
			return;
		}
		Town town = Settlement.Town;
		ClanFinanceModel clanFinanceModel = Campaign.Current.Models.ClanFinanceModel;
		int num = 0;
		int num2 = (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town).ResultNumber;
		int num3 = (int)clanFinanceModel.CalculateTownIncomeFromTariffs(Clan.PlayerClan, town).ResultNumber;
		int num4 = clanFinanceModel.CalculateTownIncomeFromProjects(town);
		if (num2 != 0)
		{
			ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=qeclv74c}Taxes").ToString(), num2, ProfitItemPropertyVM.PropertyType.Tax));
			num += num2;
		}
		if (num3 != 0)
		{
			ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=eIgC6YGp}Tariffs").ToString(), num3, ProfitItemPropertyVM.PropertyType.Tariff));
			num += num3;
		}
		if (town.GarrisonParty != null && town.GarrisonParty.IsActive)
		{
			int totalWage = town.GarrisonParty.TotalWage;
			if (totalWage != 0)
			{
				ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=5dkPxmZG}Garrison Wages").ToString(), -totalWage, ProfitItemPropertyVM.PropertyType.Garrison));
				num -= totalWage;
			}
		}
		foreach (Village village in town.Villages)
		{
			int num5 = clanFinanceModel.CalculateVillageIncome(Clan.PlayerClan, village);
			if (num5 != 0)
			{
				ProfitItemProperties.Add(new ProfitItemPropertyVM(village.Name.ToString(), num5, ProfitItemPropertyVM.PropertyType.Village));
				num += num5;
			}
		}
		if (num4 != 0)
		{
			ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=J8ddrAOf}Governor Effects").ToString(), num4, ProfitItemPropertyVM.PropertyType.Governor, Governor?.ImageIdentifier));
			num += num4;
		}
		TotalProfit.Value = num;
	}

	private bool IsSettlementSlotAssignable(Hero oldHero, Hero newHero)
	{
		if ((oldHero == null || !oldHero.IsHumanPlayerCharacter) && !newHero.IsHumanPlayerCharacter && newHero.IsActive && (newHero.PartyBelongedTo == null || newHero.PartyBelongedTo.LeaderHero != newHero))
		{
			return newHero.PartyBelongedToAsPrisoner == null;
		}
		return false;
	}

	private void ExecuteOpenSettlementPage()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Settlement.EncyclopediaLink);
	}
}
