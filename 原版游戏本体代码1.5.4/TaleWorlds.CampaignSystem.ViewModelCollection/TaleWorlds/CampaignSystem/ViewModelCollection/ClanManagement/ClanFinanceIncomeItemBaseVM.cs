using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanFinanceIncomeItemBaseVM : ViewModel
{
	protected Action _onRefresh;

	protected Action<ClanFinanceIncomeItemBaseVM> _onSelection;

	protected IncomeTypes _incomeTypeAsEnum;

	private int _incomeType;

	private string _name;

	private string _location;

	private string _incomeValueText;

	private string _imageName;

	private int _income;

	private bool _isSelected;

	private ImageIdentifierVM _visual;

	private MBBindingList<SelectableItemPropertyVM> _itemProperties = new MBBindingList<SelectableItemPropertyVM>();

	public IncomeTypes IncomeTypeAsEnum
	{
		get
		{
			return _incomeTypeAsEnum;
		}
		protected set
		{
			if (value != _incomeTypeAsEnum)
			{
				_incomeTypeAsEnum = value;
				IncomeType = (int)value;
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SelectableItemPropertyVM> ItemProperties
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
	public string Location
	{
		get
		{
			return _location;
		}
		set
		{
			if (value != _location)
			{
				_location = value;
				OnPropertyChangedWithValue(value, "Location");
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
	public string IncomeValueText
	{
		get
		{
			return _incomeValueText;
		}
		set
		{
			if (value != _incomeValueText)
			{
				_incomeValueText = value;
				OnPropertyChangedWithValue(value, "IncomeValueText");
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
	public int Income
	{
		get
		{
			return _income;
		}
		set
		{
			if (value != _income)
			{
				_income = value;
				OnPropertyChangedWithValue(value, "Income");
			}
		}
	}

	[DataSourceProperty]
	public ImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public int IncomeType
	{
		get
		{
			return _incomeType;
		}
		set
		{
			if (value != _incomeType)
			{
				_incomeType = value;
				OnPropertyChangedWithValue(value, "IncomeType");
			}
		}
	}

	protected ClanFinanceIncomeItemBaseVM(Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
	{
		_onSelection = onSelection;
		_onRefresh = onRefresh;
	}

	protected virtual void PopulateStatsList()
	{
	}

	protected virtual void PopulateActionList()
	{
	}

	public void OnIncomeSelection()
	{
		_onSelection(this);
	}

	protected string DetermineIncomeText(int incomeAmount)
	{
		if (incomeAmount == 0)
		{
			return GameTexts.FindText("str_clan_finance_value_zero").ToString();
		}
		GameTexts.SetVariable("IS_POSITIVE", (Income > 0) ? 1 : 0);
		GameTexts.SetVariable("NUMBER", TaleWorlds.Library.MathF.Abs(Income));
		return GameTexts.FindText("str_clan_finance_value").ToString();
	}
}
