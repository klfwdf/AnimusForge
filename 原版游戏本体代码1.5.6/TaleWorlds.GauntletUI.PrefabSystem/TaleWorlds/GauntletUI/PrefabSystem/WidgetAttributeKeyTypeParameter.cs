namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetAttributeKeyTypeParameter : WidgetAttributeKeyType
{
	public override bool CheckKeyType(string key)
	{
		return key.StartsWith("Parameter.");
	}

	public override string GetKeyName(string key)
	{
		return key.Substring("Parameter.".Length);
	}

	public override string GetSerializedKey(string key)
	{
		return "Parameter." + key;
	}
}
