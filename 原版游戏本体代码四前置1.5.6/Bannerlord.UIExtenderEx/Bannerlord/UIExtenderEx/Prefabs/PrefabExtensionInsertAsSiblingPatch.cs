using System.Xml;

namespace Bannerlord.UIExtenderEx.Prefabs;

public abstract class PrefabExtensionInsertAsSiblingPatch : IPrefabPatch
{
	public enum InsertType
	{
		Prepend,
		Append
	}

	public virtual InsertType Type => InsertType.Append;

	public abstract string Id { get; }

	public abstract XmlDocument GetPrefabExtension();
}
