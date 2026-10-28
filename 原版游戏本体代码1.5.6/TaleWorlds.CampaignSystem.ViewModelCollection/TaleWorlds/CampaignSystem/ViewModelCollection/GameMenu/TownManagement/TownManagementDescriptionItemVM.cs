using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class TownManagementDescriptionItemVM : ViewModel
{
	public enum DescriptionType
	{
		Gold,
		Production,
		Militia,
		Prosperity,
		Food,
		Loyalty,
		Security,
		Garrison
	}

	private readonly TextObject _titleObj;

	private int _type = -1;

	private string _title;

	private int _value;

	private int _valueChange;

	private BasicTooltipViewModel _hint;

	private bool _isWarning;

	[DataSourceProperty]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	[DataSourceProperty]
	public int ValueChange
	{
		get
		{
			return _valueChange;
		}
		set
		{
			if (value != _valueChange)
			{
				_valueChange = value;
				OnPropertyChangedWithValue(value, "ValueChange");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint && value != null)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWarning
	{
		get
		{
			return _isWarning;
		}
		set
		{
			if (value != _isWarning)
			{
				_isWarning = value;
				OnPropertyChangedWithValue(value, "IsWarning");
			}
		}
	}

	public TownManagementDescriptionItemVM(TextObject title, int value, int valueChange, DescriptionType type, BasicTooltipViewModel hint = null)
	{
		_titleObj = title;
		Value = value;
		ValueChange = valueChange;
		Type = (int)type;
		Hint = hint ?? new BasicTooltipViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _titleObj.ToString();
		RefreshIsWarning();
	}

	private void RefreshIsWarning()
	{
		switch (Type)
		{
		case 1:
			IsWarning = Value < 1;
			break;
		case 5:
			IsWarning = Value < Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
			break;
		case 7:
			IsWarning = Value < 1;
			break;
		default:
			IsWarning = false;
			break;
		}
	}
}
