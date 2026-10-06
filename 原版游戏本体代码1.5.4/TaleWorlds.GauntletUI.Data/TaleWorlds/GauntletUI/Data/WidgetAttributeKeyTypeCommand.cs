using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class WidgetAttributeKeyTypeCommand : WidgetAttributeKeyType
{
	public override bool CheckKeyType(string key)
	{
		return key.StartsWith("Command.");
	}

	public override string GetKeyName(string key)
	{
		return key.Substring("Command.".Length);
	}

	public override string GetSerializedKey(string key)
	{
		return "Command." + key;
	}
}
