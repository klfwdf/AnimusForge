namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetAttributeValueTypeDefault : WidgetAttributeValueType
{
	public override bool CheckValueType(string value)
	{
		return true;
	}

	public override string GetAttributeValue(string value)
	{
		return value;
	}

	public override string GetSerializedValue(string value)
	{
		return value;
	}
}
