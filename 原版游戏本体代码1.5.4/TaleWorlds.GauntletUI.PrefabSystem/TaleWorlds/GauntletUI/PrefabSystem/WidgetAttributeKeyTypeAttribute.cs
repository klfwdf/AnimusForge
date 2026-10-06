namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetAttributeKeyTypeAttribute : WidgetAttributeKeyType
{
	public override bool CheckKeyType(string key)
	{
		return true;
	}

	public override string GetKeyName(string key)
	{
		return key;
	}

	public override string GetSerializedKey(string key)
	{
		return key;
	}
}
