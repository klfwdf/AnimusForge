using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class WidgetAttributeValueTypeBindingPath : WidgetAttributeValueType
{
	public override bool CheckValueType(string value)
	{
		if (value.Length > 2 && value[0] == '{')
		{
			return value[value.Length - 1] == '}';
		}
		return false;
	}

	public override string GetAttributeValue(string value)
	{
		return value.Substring(1, value.Length - 2);
	}

	public override string GetSerializedValue(string value)
	{
		return "{" + value + "}";
	}
}
