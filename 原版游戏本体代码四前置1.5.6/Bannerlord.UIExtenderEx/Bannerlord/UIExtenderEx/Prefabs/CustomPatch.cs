using System.Xml;

namespace Bannerlord.UIExtenderEx.Prefabs;

public abstract class CustomPatch<T> : IPrefabPatch where T : XmlNode
{
	public abstract string Id { get; }

	public abstract void Apply(T obj);
}
