using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class WidgetAttributeValueTypeBinding : WidgetAttributeValueType
{
	public override bool CheckValueType(string value)
	{
		return value.StartsWith("@");
	}

	public override string GetAttributeValue(string value)
	{
		return value.Substring("@".Length);
	}

	public override string GetSerializedValue(string value)
	{
		return "@" + value;
	}
}
