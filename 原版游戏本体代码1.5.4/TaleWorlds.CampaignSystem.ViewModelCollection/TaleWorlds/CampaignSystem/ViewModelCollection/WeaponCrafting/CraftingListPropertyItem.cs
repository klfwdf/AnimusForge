using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingListPropertyItem : ViewModel
{
	public readonly TextObject Description;

	public readonly CraftingTemplate.CraftingStatTypes Type;

	private bool _showStats;

	private bool _isExceedingBeneficial;

	private bool _hasValidTarget;

	private bool _hasValidValue;

	private float _targetValue;

	private string _targetValueText;

	private string _propertyLbl;

	private float _propertyValue;

	private float _propertyMaxValue = -1f;

	private string _propertyValueText;

	public bool _isAlternativeUsageProperty;

	private string _separatorText;

	[DataSourceProperty]
	public bool IsValidForUsage
	{
		get
		{
			return _showStats;
		}
		set
		{
			if (value != _showStats)
			{
				_showStats = value;
				OnPropertyChangedWithValue(value, "IsValidForUsage");
			}
		}
	}

	[DataSourceProperty]
	public bool IsExceedingBeneficial
	{
		get
		{
			return _isExceedingBeneficial;
		}
		set
		{
			if (value != _isExceedingBeneficial)
			{
				_isExceedingBeneficial = value;
				OnPropertyChangedWithValue(value, "IsExceedingBeneficial");
			}
		}
	}

	[DataSourceProperty]
	public bool HasValidTarget
	{
		get
		{
			return _hasValidTarget;
		}
		set
		{
			if (value != _hasValidTarget)
			{
				_hasValidTarget = value;
				OnPropertyChangedWithValue(value, "HasValidTarget");
			}
		}
	}

	[DataSourceProperty]
	public bool HasValidValue
	{
		get
		{
			return _hasValidValue;
		}
		set
		{
			if (value != _hasValidValue)
			{
				_hasValidValue = value;
				OnPropertyChangedWithValue(value, "HasValidValue");
			}
		}
	}

	[DataSourceProperty]
	public float TargetValue
	{
		get
		{
			return _targetValue;
		}
		set
		{
			if (value != _targetValue)
			{
				_targetValue = value;
				OnPropertyChangedWithValue(value, "TargetValue");
			}
		}
	}

	[DataSourceProperty]
	public string TargetValueText
	{
		get
		{
			return _targetValueText;
		}
		set
		{
			if (value != _targetValueText)
			{
				_targetValueText = value;
				OnPropertyChangedWithValue(value, "TargetValueText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAlternativeUsageProperty
	{
		get
		{
			return _isAlternativeUsageProperty;
		}
		set
		{
			if (_isAlternativeUsageProperty != value)
			{
				_isAlternativeUsageProperty = value;
				OnPropertyChangedWithValue(value, "IsAlternativeUsageProperty");
			}
		}
	}

	[DataSourceProperty]
	public string PropertyLbl
	{
		get
		{
			return _propertyLbl;
		}
		set
		{
			if (value != _propertyLbl)
			{
				_propertyLbl = value;
				OnPropertyChangedWithValue(value, "PropertyLbl");
			}
		}
	}

	[DataSourceProperty]
	public float PropertyValue
	{
		get
		{
			return _propertyValue;
		}
		set
		{
			if (value == 0f || value != _propertyValue)
			{
				_propertyValue = value;
				OnPropertyChangedWithValue(value, "PropertyValue");
			}
		}
	}

	[DataSourceProperty]
	public float PropertyMaxValue
	{
		get
		{
			return _propertyMaxValue;
		}
		set
		{
			if (value != _propertyMaxValue)
			{
				_propertyMaxValue = value;
				OnPropertyChangedWithValue(value, "PropertyMaxValue");
			}
		}
	}

	[DataSourceProperty]
	public string PropertyValueText
	{
		get
		{
			return _propertyValueText;
		}
		set
		{
			if (_propertyValueText != value)
			{
				_propertyValueText = value;
				OnPropertyChangedWithValue(value, "PropertyValueText");
			}
		}
	}

	[DataSourceProperty]
	public string SeparatorText
	{
		get
		{
			return _separatorText;
		}
		set
		{
			if (value != _separatorText)
			{
				_separatorText = value;
				OnPropertyChangedWithValue(value, "SeparatorText");
			}
		}
	}

	public CraftingListPropertyItem(TextObject description, float maxValue, float value, float targetValue, CraftingTemplate.CraftingStatTypes propertyType, bool isAlternativeUsageProperty = false)
	{
		Description = description;
		PropertyMaxValue = maxValue;
		PropertyValue = value;
		TargetValue = targetValue;
		IsAlternativeUsageProperty = isAlternativeUsageProperty;
		Type = propertyType;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		HasValidTarget = TargetValue > float.Epsilon;
		HasValidValue = PropertyValue > float.Epsilon;
		PropertyLbl = Description?.ToString();
		IsExceedingBeneficial = CheckIfExceedingIsBeneficial();
		SeparatorText = new TextObject("{=dB6cFDmz}/").ToString();
		PropertyValueText = CampaignUIHelper.GetFormattedItemPropertyText(PropertyValue, GetIsTypeRequireInteger(Type));
		if (HasValidTarget)
		{
			TargetValueText = CampaignUIHelper.GetFormattedItemPropertyText(TargetValue, GetIsTypeRequireInteger(Type));
		}
	}

	private bool CheckIfExceedingIsBeneficial()
	{
		return Type != CraftingTemplate.CraftingStatTypes.Weight;
	}

	private bool GetIsTypeRequireInteger(CraftingTemplate.CraftingStatTypes type)
	{
		return type == CraftingTemplate.CraftingStatTypes.StackAmount;
	}
}
