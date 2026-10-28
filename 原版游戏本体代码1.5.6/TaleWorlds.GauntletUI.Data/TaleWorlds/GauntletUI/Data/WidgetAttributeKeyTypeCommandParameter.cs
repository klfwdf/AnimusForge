using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class WidgetAttributeKeyTypeCommandParameter : WidgetAttributeKeyType
{
	public override bool CheckKeyType(string key)
	{
		return key.StartsWith("CommandParameter.");
	}

	public override string GetKeyName(string key)
	{
		return key.Substring("CommandParameter.".Length);
	}

	public override string GetSerializedKey(string key)
	{
		return "CommandParameter." + key;
	}
}
