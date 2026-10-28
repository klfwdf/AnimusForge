namespace TaleWorlds.GauntletUI.PrefabSystem;

public abstract class WidgetAttributeValueType
{
	public abstract bool CheckValueType(string value);

	public abstract string GetAttributeValue(string value);

	public abstract string GetSerializedValue(string value);
}
