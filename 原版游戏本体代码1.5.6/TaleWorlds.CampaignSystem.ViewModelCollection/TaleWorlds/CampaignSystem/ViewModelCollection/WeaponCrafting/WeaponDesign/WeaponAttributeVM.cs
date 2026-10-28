using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponAttributeVM : ViewModel
{
	private string _attributeFieldText;

	public DamageTypes DamageType { get; }

	public CraftingTemplate.CraftingStatTypes AttributeType { get; }

	public float AttributeValue { get; }

	[DataSourceProperty]
	public string AttributeFieldText
	{
		get
		{
			return _attributeFieldText;
		}
		set
		{
			if (value != _attributeFieldText)
			{
				_attributeFieldText = value;
				OnPropertyChangedWithValue(value, "AttributeFieldText");
			}
		}
	}

	public WeaponAttributeVM(CraftingTemplate.CraftingStatTypes type, DamageTypes damageType, string attributeName, float attributeValue)
	{
		AttributeType = type;
		DamageType = damageType;
		AttributeValue = attributeValue;
		string variable = "<span style=\"Value\">" + ((AttributeValue > 100f) ? attributeValue.ToString("F0") : attributeValue.ToString("F1")) + "</span>";
		TextObject textObject = new TextObject("{=!}{ATTR_NAME}{ATTR_VALUE_RTT}");
		textObject.SetTextVariable("ATTR_NAME", attributeName);
		textObject.SetTextVariable("ATTR_VALUE_RTT", variable);
		AttributeFieldText = textObject.ToString();
	}
}
