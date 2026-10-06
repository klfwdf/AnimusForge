using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponDesignResultPropertyItemVM : ViewModel
{
	private readonly TextObject _description;

	private bool _isExceedingBeneficial;

	private bool _showTooltip;

	private string _propertyLbl;

	private float _propertyValue;

	private float _requiredValue;

	private string _requiredValueText;

	private float _changeAmount;

	private bool _showFloatingPoint;

	private bool _isOrderResult;

	private bool _hasBenefit;

	private HintViewModel _orderRequirementTooltip;

	private HintViewModel _craftedValueTooltip;

	private HintViewModel _bonusPenaltyTooltip;

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
	public float InitialValue
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
				OnPropertyChangedWithValue(value, "InitialValue");
			}
		}
	}

	[DataSourceProperty]
	public float TargetValue
	{
		get
		{
			return _requiredValue;
		}
		set
		{
			if (value != _requiredValue)
			{
				_requiredValue = value;
				OnPropertyChangedWithValue(value, "TargetValue");
			}
		}
	}

	[DataSourceProperty]
	public string RequiredValueText
	{
		get
		{
			return _requiredValueText;
		}
		set
		{
			if (value != _requiredValueText)
			{
				_requiredValueText = value;
				OnPropertyChangedWithValue(value, "RequiredValueText");
			}
		}
	}

	[DataSourceProperty]
	public float ChangeAmount
	{
		get
		{
			return _changeAmount;
		}
		set
		{
			if (_changeAmount != value)
			{
				_changeAmount = value;
				OnPropertyChangedWithValue(value, "ChangeAmount");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowFloatingPoint
	{
		get
		{
			return _showFloatingPoint;
		}
		set
		{
			if (_showFloatingPoint != value)
			{
				_showFloatingPoint = value;
				OnPropertyChangedWithValue(value, "ShowFloatingPoint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOrderResult
	{
		get
		{
			return _isOrderResult;
		}
		set
		{
			if (value != _isOrderResult)
			{
				_isOrderResult = value;
				OnPropertyChangedWithValue(value, "IsOrderResult");
			}
		}
	}

	[DataSourceProperty]
	public bool HasBenefit
	{
		get
		{
			return _hasBenefit;
		}
		set
		{
			if (value != _hasBenefit)
			{
				_hasBenefit = value;
				OnPropertyChangedWithValue(value, "HasBenefit");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel OrderRequirementTooltip
	{
		get
		{
			return _orderRequirementTooltip;
		}
		set
		{
			if (value != _orderRequirementTooltip)
			{
				_orderRequirementTooltip = value;
				OnPropertyChangedWithValue(value, "OrderRequirementTooltip");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CraftedValueTooltip
	{
		get
		{
			return _craftedValueTooltip;
		}
		set
		{
			if (value != _craftedValueTooltip)
			{
				_craftedValueTooltip = value;
				OnPropertyChangedWithValue(value, "CraftedValueTooltip");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BonusPenaltyTooltip
	{
		get
		{
			return _bonusPenaltyTooltip;
		}
		set
		{
			if (value != _bonusPenaltyTooltip)
			{
				_bonusPenaltyTooltip = value;
				OnPropertyChangedWithValue(value, "BonusPenaltyTooltip");
			}
		}
	}

	public WeaponDesignResultPropertyItemVM(TextObject description, float value, float changeAmount, bool showFloatingPoint)
	{
		_description = description;
		InitialValue = value;
		ChangeAmount = changeAmount;
		ShowFloatingPoint = showFloatingPoint;
		IsOrderResult = false;
		OrderRequirementTooltip = new HintViewModel();
		CraftedValueTooltip = new HintViewModel();
		BonusPenaltyTooltip = new HintViewModel();
		RefreshValues();
	}

	public WeaponDesignResultPropertyItemVM(TextObject description, float craftedValue, float requiredValue, float changeAmount, bool showFloatingPoint, bool isExceedingBeneficial, bool showTooltip = true)
	{
		_showTooltip = showTooltip;
		_description = description;
		TargetValue = requiredValue;
		InitialValue = craftedValue;
		ChangeAmount = changeAmount;
		_isExceedingBeneficial = isExceedingBeneficial;
		IsOrderResult = true;
		ShowFloatingPoint = showFloatingPoint;
		OrderRequirementTooltip = new HintViewModel();
		CraftedValueTooltip = new HintViewModel();
		BonusPenaltyTooltip = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PropertyLbl = _description?.ToString();
		TextObject textObject = GameTexts.FindText("str_STR_in_parentheses");
		textObject.SetTextVariable("STR", CampaignUIHelper.GetFormattedItemPropertyText(TargetValue, ShowFloatingPoint));
		RequiredValueText = ((TargetValue == 0f) ? string.Empty : textObject.ToString());
		HasBenefit = (_isExceedingBeneficial ? (InitialValue + ChangeAmount >= TargetValue) : (InitialValue + ChangeAmount <= TargetValue));
		OrderRequirementTooltip.HintText = (_showTooltip ? GameTexts.FindText("str_crafting_order_requirement_tooltip") : TextObject.GetEmpty());
		CraftedValueTooltip.HintText = (_showTooltip ? GameTexts.FindText("str_crafting_crafted_value_tooltip") : TextObject.GetEmpty());
		BonusPenaltyTooltip.HintText = (_showTooltip ? GameTexts.FindText("str_crafting_bonus_penalty_tooltip") : TextObject.GetEmpty());
	}
}
