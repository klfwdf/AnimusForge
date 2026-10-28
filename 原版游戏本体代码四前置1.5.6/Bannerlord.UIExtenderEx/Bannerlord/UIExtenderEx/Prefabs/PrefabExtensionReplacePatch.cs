using System.Xml;

namespace Bannerlord.UIExtenderEx.Prefabs;

public abstract class PrefabExtensionReplacePatch : IPrefabPatch
{
	public abstract string Id { get; }

	public abstract XmlDocument GetPrefabExtension();
}
