using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class ItemMenuTooltipPropertyVM : TooltipProperty
{
	private HintViewModel _propertyHint;

	private bool _hasModifierBonus;

	private bool _isModifierBeneficial;

	private string _modifierBonusText;

	[DataSourceProperty]
	public HintViewModel PropertyHint
	{
		get
		{
			return _propertyHint;
		}
		set
		{
			if (value != _propertyHint)
			{
				_propertyHint = value;
				OnPropertyChangedWithValue(value, "PropertyHint");
			}
		}
	}

	[DataSourceProperty]
	public bool HasModifierBonus
	{
		get
		{
			return _hasModifierBonus;
		}
		set
		{
			if (value != _hasModifierBonus)
			{
				_hasModifierBonus = value;
				OnPropertyChangedWithValue(value, "HasModifierBonus");
			}
		}
	}

	[DataSourceProperty]
	public bool IsModifierBeneficial
	{
		get
		{
			return _isModifierBeneficial;
		}
		set
		{
			if (value != _isModifierBeneficial)
			{
				_isModifierBeneficial = value;
				OnPropertyChangedWithValue(value, "IsModifierBeneficial");
			}
		}
	}

	[DataSourceProperty]
	public string ModifierBonusText
	{
		get
		{
			return _modifierBonusText;
		}
		set
		{
			if (value != _modifierBonusText)
			{
				_modifierBonusText = value;
				OnPropertyChangedWithValue(value, "ModifierBonusText");
			}
		}
	}

	public ItemMenuTooltipPropertyVM()
	{
	}

	public ItemMenuTooltipPropertyVM(string definition, string value, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null, string modifierBonusText = null, bool isModifierBeneficial = false)
		: base(definition, value, textHeight, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
		ModifierBonusText = modifierBonusText;
		HasModifierBonus = !string.IsNullOrEmpty(modifierBonusText);
		IsModifierBeneficial = isModifierBeneficial;
	}

	public ItemMenuTooltipPropertyVM(string definition, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
		: base(definition, _valueFunc, textHeight, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
	}

	public ItemMenuTooltipPropertyVM(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
		: base(_definitionFunc, _valueFunc, textHeight, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
	}

	public ItemMenuTooltipPropertyVM(Func<string> _definitionFunc, Func<string> _valueFunc, object[] valueArgs, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
		: base(_definitionFunc, _valueFunc, valueArgs, textHeight, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
	}

	public ItemMenuTooltipPropertyVM(string definition, string value, int textHeight, Color color, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null, TooltipPropertyFlags propertyFlags = TooltipPropertyFlags.None, string modifierBonusText = null, bool isModifierBeneficial = false)
		: base(definition, value, textHeight, color, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
		ModifierBonusText = modifierBonusText;
		HasModifierBonus = !string.IsNullOrEmpty(modifierBonusText);
		IsModifierBeneficial = isModifierBeneficial;
	}

	public ItemMenuTooltipPropertyVM(string definition, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
		: base(definition, _valueFunc, textHeight, color, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
	}

	public ItemMenuTooltipPropertyVM(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
		: base(_definitionFunc, _valueFunc, textHeight, color, onlyShowWhenExtended)
	{
		PropertyHint = propertyHint;
	}

	public ItemMenuTooltipPropertyVM(TooltipProperty property, HintViewModel propertyHint = null)
		: base(property)
	{
		PropertyHint = propertyHint;
	}
}
