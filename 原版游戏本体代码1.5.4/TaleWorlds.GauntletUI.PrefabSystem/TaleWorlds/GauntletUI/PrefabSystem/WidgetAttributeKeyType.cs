namespace TaleWorlds.GauntletUI.PrefabSystem;

public abstract class WidgetAttributeKeyType
{
	public abstract bool CheckKeyType(string key);

	public abstract string GetKeyName(string key);

	public abstract string GetSerializedKey(string key);
}
