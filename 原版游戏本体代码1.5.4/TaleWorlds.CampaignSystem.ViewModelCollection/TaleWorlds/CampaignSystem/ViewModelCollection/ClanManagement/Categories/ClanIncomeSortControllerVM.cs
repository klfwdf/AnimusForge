using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanIncomeSortControllerVM : ViewModel
{
	public abstract class WorkshopItemComparerBase : IComparer<ClanFinanceWorkshopItemVM>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y);
	}

	public abstract class SupporterItemComparerBase : IComparer<ClanSupporterGroupVM>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(ClanSupporterGroupVM x, ClanSupporterGroupVM y);
	}

	public abstract class AlleyItemComparerBase : IComparer<ClanFinanceAlleyItemVM>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y);
	}

	public class WorkshopItemNameComparer : WorkshopItemComparerBase
	{
		public override int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class SupporterItemNameComparer : SupporterItemComparerBase
	{
		public override int Compare(ClanSupporterGroupVM x, ClanSupporterGroupVM y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class AlleyItemNameComparer : AlleyItemComparerBase
	{
		public override int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class WorkshopItemLocationComparer : WorkshopItemComparerBase
	{
		public override int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y)
		{
			int num = GetDistanceToMainParty(y).CompareTo(GetDistanceToMainParty(x));
			if (_isAcending)
			{
				return num * -1;
			}
			return num;
		}

		private float GetDistanceToMainParty(ClanFinanceWorkshopItemVM item)
		{
			return item.Workshop.Settlement.Position.Distance(Hero.MainHero.GetCampaignPosition());
		}
	}

	public class AlleyItemLocationComparer : AlleyItemComparerBase
	{
		public override int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y)
		{
			int num = GetDistanceToMainParty(y).CompareTo(GetDistanceToMainParty(x));
			if (_isAcending)
			{
				return num * -1;
			}
			return num;
		}

		private float GetDistanceToMainParty(ClanFinanceAlleyItemVM item)
		{
			return item.Alley.Settlement.Position.Distance(Hero.MainHero.GetCampaignPosition());
		}
	}

	public class WorkshopItemIncomeComparer : WorkshopItemComparerBase
	{
		public override int Compare(ClanFinanceWorkshopItemVM x, ClanFinanceWorkshopItemVM y)
		{
			if (_isAcending)
			{
				return y.Workshop.ProfitMade.CompareTo(x.Workshop.ProfitMade) * -1;
			}
			return y.Workshop.ProfitMade.CompareTo(x.Workshop.ProfitMade);
		}
	}

	public class SupporterItemIncomeComparer : SupporterItemComparerBase
	{
		public override int Compare(ClanSupporterGroupVM x, ClanSupporterGroupVM y)
		{
			if (_isAcending)
			{
				return y.TotalInfluenceBonus.CompareTo(x.TotalInfluenceBonus) * -1;
			}
			return y.TotalInfluenceBonus.CompareTo(x.TotalInfluenceBonus);
		}
	}

	public class AlleyItemIncomeComparer : AlleyItemComparerBase
	{
		public override int Compare(ClanFinanceAlleyItemVM x, ClanFinanceAlleyItemVM y)
		{
			if (_isAcending)
			{
				return y.Income.CompareTo(x.Income) * -1;
			}
			return y.Income.CompareTo(x.Income);
		}
	}

	private readonly MBBindingList<ClanFinanceWorkshopItemVM> _workshopList;

	private readonly MBBindingList<ClanSupporterGroupVM> _supporterList;

	private readonly MBBindingList<ClanFinanceAlleyItemVM> _alleyList;

	private readonly WorkshopItemNameComparer _workshopNameComparer;

	private readonly SupporterItemNameComparer _supporterNameComparer;

	private readonly AlleyItemNameComparer _alleyNameComparer;

	private readonly WorkshopItemLocationComparer _workshopLocationComparer;

	private readonly AlleyItemLocationComparer _alleyLocationComparer;

	private readonly WorkshopItemIncomeComparer _workshopIncomeComparer;

	private readonly SupporterItemIncomeComparer _supporterIncomeComparer;

	private readonly AlleyItemIncomeComparer _alleyIncomeComparer;

	private int _nameState;

	private int _locationState;

	private int _incomeState;

	private bool _isNameSelected;

	private bool _isLocationSelected;

	private bool _isIncomeSelected;

	private string _nameText;

	private string _locationText;

	private string _incomeText;

	[DataSourceProperty]
	public int NameState
	{
		get
		{
			return _nameState;
		}
		set
		{
			if (value != _nameState)
			{
				_nameState = value;
				OnPropertyChangedWithValue(value, "NameState");
			}
		}
	}

	[DataSourceProperty]
	public int LocationState
	{
		get
		{
			return _locationState;
		}
		set
		{
			if (value != _locationState)
			{
				_locationState = value;
				OnPropertyChangedWithValue(value, "LocationState");
			}
		}
	}

	[DataSourceProperty]
	public int IncomeState
	{
		get
		{
			return _incomeState;
		}
		set
		{
			if (value != _incomeState)
			{
				_incomeState = value;
				OnPropertyChangedWithValue(value, "IncomeState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNameSelected
	{
		get
		{
			return _isNameSelected;
		}
		set
		{
			if (value != _isNameSelected)
			{
				_isNameSelected = value;
				OnPropertyChangedWithValue(value, "IsNameSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLocationSelected
	{
		get
		{
			return _isLocationSelected;
		}
		set
		{
			if (value != _isLocationSelected)
			{
				_isLocationSelected = value;
				OnPropertyChangedWithValue(value, "IsLocationSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsIncomeSelected
	{
		get
		{
			return _isIncomeSelected;
		}
		set
		{
			if (value != _isIncomeSelected)
			{
				_isIncomeSelected = value;
				OnPropertyChangedWithValue(value, "IsIncomeSelected");
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

	public ClanIncomeSortControllerVM(MBBindingList<ClanFinanceWorkshopItemVM> workshopList, MBBindingList<ClanSupporterGroupVM> supporterList, MBBindingList<ClanFinanceAlleyItemVM> alleyList)
	{
		_workshopList = workshopList;
		_supporterList = supporterList;
		_alleyList = alleyList;
		_workshopNameComparer = new WorkshopItemNameComparer();
		_supporterNameComparer = new SupporterItemNameComparer();
		_alleyNameComparer = new AlleyItemNameComparer();
		_workshopLocationComparer = new WorkshopItemLocationComparer();
		_alleyLocationComparer = new AlleyItemLocationComparer();
		_workshopIncomeComparer = new WorkshopItemIncomeComparer();
		_supporterIncomeComparer = new SupporterItemIncomeComparer();
		_alleyIncomeComparer = new AlleyItemIncomeComparer();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		LocationText = GameTexts.FindText("str_tooltip_label_location").ToString();
		IncomeText = GameTexts.FindText("str_income").ToString();
	}

	public void ExecuteSortByName()
	{
		int nameState = NameState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		NameState = (nameState + 1) % 3;
		if (NameState == 0)
		{
			NameState++;
		}
		_workshopNameComparer.SetSortMode(NameState == 1);
		_supporterNameComparer.SetSortMode(NameState == 1);
		_alleyNameComparer.SetSortMode(NameState == 1);
		_workshopList.Sort(_workshopNameComparer);
		_supporterList.Sort(_supporterNameComparer);
		_alleyList.Sort(_alleyNameComparer);
		IsNameSelected = true;
	}

	public void ExecuteSortByLocation()
	{
		int locationState = LocationState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		LocationState = (locationState + 1) % 3;
		if (LocationState == 0)
		{
			LocationState++;
		}
		_workshopLocationComparer.SetSortMode(LocationState == 1);
		_alleyLocationComparer.SetSortMode(LocationState == 1);
		_workshopList.Sort(_workshopLocationComparer);
		_alleyList.Sort(_alleyLocationComparer);
		IsLocationSelected = true;
	}

	public void ExecuteSortByIncome()
	{
		int incomeState = IncomeState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		IncomeState = (incomeState + 1) % 3;
		if (IncomeState == 0)
		{
			IncomeState++;
		}
		_workshopIncomeComparer.SetSortMode(IncomeState == 1);
		_supporterIncomeComparer.SetSortMode(IncomeState == 1);
		_alleyIncomeComparer.SetSortMode(IncomeState == 1);
		_workshopList.Sort(_workshopIncomeComparer);
		_supporterList.Sort(_supporterIncomeComparer);
		_alleyList.Sort(_alleyIncomeComparer);
		IsIncomeSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		NameState = (int)state;
		LocationState = (int)state;
		IncomeState = (int)state;
		IsNameSelected = false;
		IsLocationSelected = false;
		IsIncomeSelected = false;
	}

	public void ResetAllStates()
	{
		SetAllStates(CampaignUIHelper.SortState.Default);
	}
}
