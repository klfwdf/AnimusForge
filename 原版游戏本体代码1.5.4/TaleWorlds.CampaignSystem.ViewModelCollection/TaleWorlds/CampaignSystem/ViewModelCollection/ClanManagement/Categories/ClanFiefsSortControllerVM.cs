using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanFiefsSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<ClanSettlementItemVM>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y);
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class ItemGovernorComparer : ItemComparerBase
	{
		public override int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y)
		{
			if (_isAcending)
			{
				if (y.HasGovernor && x.HasGovernor)
				{
					return y.Governor.NameText.CompareTo(x.Governor.NameText) * -1;
				}
				if (y.HasGovernor)
				{
					return 1;
				}
				if (x.HasGovernor)
				{
					return -1;
				}
				return 0;
			}
			if (y.HasGovernor && x.HasGovernor)
			{
				return y.Governor.NameText.CompareTo(x.Governor.NameText);
			}
			if (y.HasGovernor)
			{
				return 1;
			}
			if (x.HasGovernor)
			{
				return -1;
			}
			return 0;
		}
	}

	public class ItemProfitComparer : ItemComparerBase
	{
		public override int Compare(ClanSettlementItemVM x, ClanSettlementItemVM y)
		{
			if (_isAcending)
			{
				return y.TotalProfit.Value.CompareTo(x.TotalProfit.Value) * -1;
			}
			return y.TotalProfit.Value.CompareTo(x.TotalProfit.Value);
		}
	}

	private readonly List<MBBindingList<ClanSettlementItemVM>> _listsToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemGovernorComparer _governorComparer;

	private readonly ItemProfitComparer _profitComparer;

	private int _nameState;

	private int _governorState;

	private int _profitState;

	private bool _isNameSelected;

	private bool _isGovernorSelected;

	private bool _isProfitSelected;

	private string _nameText;

	private string _governorText;

	private string _profitText;

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
	public int GovernorState
	{
		get
		{
			return _governorState;
		}
		set
		{
			if (value != _governorState)
			{
				_governorState = value;
				OnPropertyChangedWithValue(value, "GovernorState");
			}
		}
	}

	[DataSourceProperty]
	public int ProfitState
	{
		get
		{
			return _profitState;
		}
		set
		{
			if (value != _profitState)
			{
				_profitState = value;
				OnPropertyChangedWithValue(value, "ProfitState");
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
	public bool IsGovernorSelected
	{
		get
		{
			return _isGovernorSelected;
		}
		set
		{
			if (value != _isGovernorSelected)
			{
				_isGovernorSelected = value;
				OnPropertyChangedWithValue(value, "IsGovernorSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsProfitSelected
	{
		get
		{
			return _isProfitSelected;
		}
		set
		{
			if (value != _isProfitSelected)
			{
				_isProfitSelected = value;
				OnPropertyChangedWithValue(value, "IsProfitSelected");
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
	public string GovernorText
	{
		get
		{
			return _governorText;
		}
		set
		{
			if (value != _governorText)
			{
				_governorText = value;
				OnPropertyChangedWithValue(value, "GovernorText");
			}
		}
	}

	[DataSourceProperty]
	public string ProfitText
	{
		get
		{
			return _profitText;
		}
		set
		{
			if (value != _profitText)
			{
				_profitText = value;
				OnPropertyChangedWithValue(value, "ProfitText");
			}
		}
	}

	public ClanFiefsSortControllerVM(List<MBBindingList<ClanSettlementItemVM>> listsToControl)
	{
		_listsToControl = listsToControl;
		_nameComparer = new ItemNameComparer();
		_governorComparer = new ItemGovernorComparer();
		_profitComparer = new ItemProfitComparer();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		GovernorText = GameTexts.FindText("str_notable_governor").ToString();
		ProfitText = GameTexts.FindText("str_profit").ToString();
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
		_nameComparer.SetSortMode(NameState == 1);
		foreach (MBBindingList<ClanSettlementItemVM> item in _listsToControl)
		{
			item.Sort(_nameComparer);
		}
		IsNameSelected = true;
	}

	public void ExecuteSortByGovernor()
	{
		int governorState = GovernorState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		GovernorState = (governorState + 1) % 3;
		if (GovernorState == 0)
		{
			GovernorState++;
		}
		_governorComparer.SetSortMode(GovernorState == 1);
		foreach (MBBindingList<ClanSettlementItemVM> item in _listsToControl)
		{
			item.Sort(_governorComparer);
		}
		IsGovernorSelected = true;
	}

	public void ExecuteSortByProfit()
	{
		int profitState = ProfitState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		ProfitState = (profitState + 1) % 3;
		if (ProfitState == 0)
		{
			ProfitState++;
		}
		_profitComparer.SetSortMode(ProfitState == 1);
		foreach (MBBindingList<ClanSettlementItemVM> item in _listsToControl)
		{
			item.Sort(_profitComparer);
		}
		IsProfitSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		NameState = (int)state;
		GovernorState = (int)state;
		ProfitState = (int)state;
		IsNameSelected = false;
		IsGovernorSelected = false;
		IsProfitSelected = false;
	}

	public void ResetAllStates()
	{
		SetAllStates(CampaignUIHelper.SortState.Default);
	}
}
