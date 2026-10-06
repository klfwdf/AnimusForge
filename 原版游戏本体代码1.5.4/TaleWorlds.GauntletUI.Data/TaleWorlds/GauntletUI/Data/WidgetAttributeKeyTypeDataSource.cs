using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class WidgetAttributeKeyTypeDataSource : WidgetAttributeKeyType
{
	public override bool CheckKeyType(string key)
	{
		return key == "DataSource";
	}

	public override string GetKeyName(string key)
	{
		return "DataSource";
	}

	public override string GetSerializedKey(string key)
	{
		return "DataSource";
	}
}
