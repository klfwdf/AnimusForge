using System.Xml;

namespace Bannerlord.UIExtenderEx.Prefabs;

public abstract class InsertPatch : IPrefabPatch
{
	public const int PositionFirst = 0;

	public const int PositionLast = int.MaxValue;

	public abstract string Id { get; }

	public abstract int Position { get; }

	public abstract XmlDocument GetPrefabExtension();
}
