namespace Bannerlord.UIExtenderEx.Prefabs;

public abstract class PrefabExtensionSetAttributePatch : IPrefabPatch
{
	public abstract string Id { get; }

	public abstract string Attribute { get; }

	public abstract string Value { get; }
}
