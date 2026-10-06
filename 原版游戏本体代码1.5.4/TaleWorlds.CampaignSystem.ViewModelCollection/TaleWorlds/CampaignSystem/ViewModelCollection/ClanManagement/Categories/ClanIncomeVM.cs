using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanIncomeVM : ViewModel
{
	private readonly Action _onRefresh;

	private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

	private MBBindingList<ClanFinanceWorkshopItemVM> _incomes;

	private MBBindingList<ClanSupporterGroupVM> _supporterGroups;

	private MBBindingList<ClanFinanceAlleyItemVM> _alleys;

	private ClanFinanceAlleyItemVM _currentSelectedAlley;

	private ClanFinanceWorkshopItemVM _currentSelectedIncome;

	private ClanSupporterGroupVM _currentSelectedSupporterGroup;

	private bool _isSelected;

	private string _nameText;

	private string _incomeText;

	private string _locationText;

	private string _workshopsText;

	private string _supportersText;

	private string _alleysText;

	private string _noAdditionalIncomesText;

	private bool _isAnyValidAlleySelected;

	private bool _isAnyValidIncomeSelected;

	private bool _isAnyValidSupporterSelected;

	private ClanIncomeSortControllerVM _sortController;

	public int TotalIncome { get; private set; }

	[DataSourceProperty]
	public ClanFinanceAlleyItemVM CurrentSelectedAlley
	{
		get
		{
			return _currentSelectedAlley;
		}
		set
		{
			if (value != _currentSelectedAlley)
			{
				_currentSelectedAlley = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedAlley");
				IsAnyValidAlleySelected = value != null;
				IsAnyValidIncomeSelected = false;
				IsAnyValidSupporterSelected = false;
			}
		}
	}

	[DataSourceProperty]
	public ClanFinanceWorkshopItemVM CurrentSelectedIncome
	{
		get
		{
			return _currentSelectedIncome;
		}
		set
		{
			if (value != _currentSelectedIncome)
			{
				_currentSelectedIncome = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedIncome");
				IsAnyValidIncomeSelected = value != null;
				IsAnyValidSupporterSelected = false;
				IsAnyValidAlleySelected = false;
			}
		}
	}

	[DataSourceProperty]
	public ClanSupporterGroupVM CurrentSelectedSupporterGroup
	{
		get
		{
			return _currentSelectedSupporterGroup;
		}
		set
		{
			if (value != _currentSelectedSupporterGroup)
			{
				_currentSelectedSupporterGroup = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedSupporterGroup");
				IsAnyValidSupporterSelected = value != null;
				IsAnyValidIncomeSelected = false;
				IsAnyValidAlleySelected = false;
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyValidAlleySelected
	{
		get
		{
			return _isAnyValidAlleySelected;
		}
		set
		{
			if (value != _isAnyValidAlleySelected)
			{
				_isAnyValidAlleySelected = value;
				OnPropertyChangedWithValue(value, "IsAnyValidAlleySelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyValidIncomeSelected
	{
		get
		{
			return _isAnyValidIncomeSelected;
		}
		set
		{
			if (value != _isAnyValidIncomeSelected)
			{
				_isAnyValidIncomeSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyValidIncomeSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyValidSupporterSelected
	{
		get
		{
			return _isAnyValidSupporterSelected;
		}
		set
		{
			if (value != _isAnyValidSupporterSelected)
			{
				_isAnyValidSupporterSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyValidSupporterSelected");
			}
		}
	}

	[DataSourceProperty]
	public string IncomeText
	{
		get
		{
			return _incomeText;
		}
		set
		{
			if (value != _incomeText)
			{
				_incomeText = value;
				OnPropertyChangedWithValue(value, "IncomeText");
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
	public string LocationText
	{
		get
		{
			return _locationText;
		}
		set
		{
			if (value != _locationText)
			{
				_locationText = value;
				OnPropertyChangedWithValue(value, "LocationText");
			}
		}
	}

	[DataSourceProperty]
	public string WorkshopText
	{
		get
		{
			return _workshopsText;
		}
		set
		{
			if (value != _workshopsText)
			{
				_workshopsText = value;
				OnPropertyChangedWithValue(value, "WorkshopText");
			}
		}
	}

	[DataSourceProperty]
	public string SupportersText
	{
		get
		{
			return _supportersText;
		}
		set
		{
			if (value != _supportersText)
			{
				_supportersText = value;
				OnPropertyChangedWithValue(value, "SupportersText");
			}
		}
	}

	[DataSourceProperty]
	public string AlleysText
	{
		get
		{
			return _alleysText;
		}
		set
		{
			if (value != _alleysText)
			{
				_alleysText = value;
				OnPropertyChangedWithValue(value, "AlleysText");
			}
		}
	}

	[DataSourceProperty]
	public string NoAdditionalIncomesText
	{
		get
		{
			return _noAdditionalIncomesText;
		}
		set
		{
			if (_noAdditionalIncomesText != value)
			{
				_noAdditionalIncomesText = value;
				OnPropertyChangedWithValue(value, "NoAdditionalIncomesText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanFinanceWorkshopItemVM> Incomes
	{
		get
		{
			return _incomes;
		}
		set
		{
			if (value != _incomes)
			{
				_incomes = value;
				OnPropertyChangedWithValue(value, "Incomes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanSupporterGroupVM> SupporterGroups
	{
		get
		{
			return _supporterGroups;
		}
		set
		{
			if (value != _supporterGroups)
			{
				_supporterGroups = value;
				OnPropertyChangedWithValue(value, "SupporterGroups");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanFinanceAlleyItemVM> Alleys
	{
		get
		{
			return _alleys;
		}
		set
		{
			if (value != _alleys)
			{
				_alleys = value;
				OnPropertyChangedWithValue(value, "Alleys");
			}
		}
	}

	[DataSourceProperty]
	public ClanIncomeSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	public ClanIncomeVM(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
	{
		_onRefresh = onRefresh;
		_openCardSelectionPopup = openCardSelectionPopup;
		Incomes = new MBBindingList<ClanFinanceWorkshopItemVM>();
		SupporterGroups = new MBBindingList<ClanSupporterGroupVM>();
		Alleys = new MBBindingList<ClanFinanceAlleyItemVM>();
		SortController = new ClanIncomeSortControllerVM(_incomes, _supporterGroups, _alleys);
		RefreshList();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		IncomeText = GameTexts.FindText("str_income").ToString();
		LocationText = GameTexts.FindText("str_tooltip_label_location").ToString();
		NoAdditionalIncomesText = GameTexts.FindText("str_clan_no_additional_incomes").ToString();
		Incomes.ApplyActionOnAllItems(delegate(ClanFinanceWorkshopItemVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedIncome?.RefreshValues();
		SortController.RefreshValues();
	}

	public void RefreshList()
	{
		Incomes.Clear();
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsTown)
			{
				continue;
			}
			Workshop[] workshops = item.Town.Workshops;
			foreach (Workshop workshop in workshops)
			{
				if (workshop.Owner == Hero.MainHero)
				{
					Incomes.Add(new ClanFinanceWorkshopItemVM(workshop, OnIncomeSelection, OnRefresh, _openCardSelectionPopup));
				}
			}
		}
		RefreshSupporters();
		RefreshAlleys();
		SortController.ResetAllStates();
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_clan_workshops"));
		GameTexts.SetVariable("LEFT", Hero.MainHero.OwnedWorkshops.Count);
		GameTexts.SetVariable("RIGHT", Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(Clan.PlayerClan.Tier));
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis"));
		WorkshopText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		int num = 0;
		foreach (ClanSupporterGroupVM supporterGroup in SupporterGroups)
		{
			num += supporterGroup.Supporters.Count;
		}
		GameTexts.SetVariable("RANK", new TextObject("{=RzFyGnWJ}Supporters").ToString());
		GameTexts.SetVariable("NUMBER", num);
		SupportersText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		GameTexts.SetVariable("RANK", new TextObject("{=7tKjfMSb}Alleys").ToString());
		GameTexts.SetVariable("NUMBER", Alleys.Count);
		AlleysText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		RefreshTotalIncome();
		OnIncomeSelection(GetDefaultIncome());
		RefreshValues();
	}

	private void RefreshSupporters()
	{
		foreach (ClanSupporterGroupVM supporterGroup in SupporterGroups)
		{
			supporterGroup.Supporters.Clear();
		}
		SupporterGroups.Clear();
		Dictionary<float, List<Hero>> dictionary = new Dictionary<float, List<Hero>>();
		NotablePowerModel notablePowerModel = Campaign.Current.Models.NotablePowerModel;
		foreach (Hero item in Clan.PlayerClan.SupporterNotables.OrderBy((Hero x) => x.Power))
		{
			if (item.CurrentSettlement != null)
			{
				float influenceBonusToClan = notablePowerModel.GetInfluenceBonusToClan(item);
				if (dictionary.TryGetValue(influenceBonusToClan, out var value))
				{
					value.Add(item);
					continue;
				}
				dictionary.Add(influenceBonusToClan, new List<Hero> { item });
			}
		}
		foreach (KeyValuePair<float, List<Hero>> item2 in dictionary)
		{
			if (item2.Value.Count <= 0)
			{
				continue;
			}
			ClanSupporterGroupVM clanSupporterGroupVM = new ClanSupporterGroupVM(notablePowerModel.GetPowerRankName(item2.Value.FirstOrDefault()), item2.Key, OnSupporterSelection);
			foreach (Hero item3 in item2.Value)
			{
				clanSupporterGroupVM.AddSupporter(item3);
			}
			SupporterGroups.Add(clanSupporterGroupVM);
		}
		foreach (ClanSupporterGroupVM supporterGroup2 in SupporterGroups)
		{
			supporterGroup2.Refresh();
		}
	}

	private void RefreshAlleys()
	{
		Alleys.Clear();
		foreach (Alley ownedAlley in Hero.MainHero.OwnedAlleys)
		{
			Alleys.Add(new ClanFinanceAlleyItemVM(ownedAlley, _openCardSelectionPopup, OnAlleySelection, OnRefresh));
		}
	}

	private ClanFinanceWorkshopItemVM GetDefaultIncome()
	{
		return Incomes.FirstOrDefault();
	}

	public void SelectWorkshop(Workshop workshop)
	{
		foreach (ClanFinanceWorkshopItemVM income in Incomes)
		{
			if (income != null)
			{
				ClanFinanceWorkshopItemVM clanFinanceWorkshopItemVM = income;
				if (clanFinanceWorkshopItemVM.Workshop == workshop)
				{
					OnIncomeSelection(clanFinanceWorkshopItemVM);
					break;
				}
			}
		}
	}

	public void SelectAlley(Alley alley)
	{
		for (int i = 0; i < Alleys.Count; i++)
		{
			if (Alleys[i].Alley == alley)
			{
				OnAlleySelection(Alleys[i]);
				break;
			}
		}
	}

	private void OnAlleySelection(ClanFinanceAlleyItemVM alley)
	{
		if (alley == null)
		{
			if (CurrentSelectedAlley != null)
			{
				CurrentSelectedAlley.IsSelected = false;
			}
			CurrentSelectedAlley = null;
			return;
		}
		OnIncomeSelection(null);
		OnSupporterSelection(null);
		if (CurrentSelectedAlley != null)
		{
			CurrentSelectedAlley.IsSelected = false;
		}
		CurrentSelectedAlley = alley;
		if (alley != null)
		{
			alley.IsSelected = true;
		}
	}

	private void OnIncomeSelection(ClanFinanceWorkshopItemVM income)
	{
		if (income == null)
		{
			if (CurrentSelectedIncome != null)
			{
				CurrentSelectedIncome.IsSelected = false;
			}
			CurrentSelectedIncome = null;
			return;
		}
		OnSupporterSelection(null);
		OnAlleySelection(null);
		if (CurrentSelectedIncome != null)
		{
			CurrentSelectedIncome.IsSelected = false;
		}
		CurrentSelectedIncome = income;
		if (income != null)
		{
			income.IsSelected = true;
		}
	}

	private void OnSupporterSelection(ClanSupporterGroupVM supporter)
	{
		if (supporter == null)
		{
			if (CurrentSelectedSupporterGroup != null)
			{
				CurrentSelectedSupporterGroup.IsSelected = false;
			}
			CurrentSelectedSupporterGroup = null;
			return;
		}
		OnIncomeSelection(null);
		OnAlleySelection(null);
		if (CurrentSelectedSupporterGroup != null)
		{
			CurrentSelectedSupporterGroup.IsSelected = false;
		}
		CurrentSelectedSupporterGroup = supporter;
		if (CurrentSelectedSupporterGroup != null)
		{
			CurrentSelectedSupporterGroup.IsSelected = true;
		}
	}

	public void RefreshTotalIncome()
	{
		TotalIncome = Incomes.Sum((ClanFinanceWorkshopItemVM i) => i.Income);
	}

	public void OnRefresh()
	{
		_onRefresh?.Invoke();
	}
}
