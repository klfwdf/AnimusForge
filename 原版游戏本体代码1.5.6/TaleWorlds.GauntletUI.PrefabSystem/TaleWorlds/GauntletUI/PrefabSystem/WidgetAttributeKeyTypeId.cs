namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetAttributeKeyTypeId : WidgetAttributeKeyType
{
	public override bool CheckKeyType(string key)
	{
		return key == "Id";
	}

	public override string GetKeyName(string key)
	{
		return "Id";
	}

	public override string GetSerializedKey(string key)
	{
		return "Id";
	}
}
