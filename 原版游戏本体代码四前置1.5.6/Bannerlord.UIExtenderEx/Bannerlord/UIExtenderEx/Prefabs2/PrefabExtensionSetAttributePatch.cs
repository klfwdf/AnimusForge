using System.Collections.Generic;

namespace Bannerlord.UIExtenderEx.Prefabs2;

public abstract class PrefabExtensionSetAttributePatch
{
	public readonly struct Attribute(string name, string value)
	{
		public string Name { get; } = name;

		public string Value { get; } = value;
	}

	public abstract List<Attribute> Attributes { get; }
}
