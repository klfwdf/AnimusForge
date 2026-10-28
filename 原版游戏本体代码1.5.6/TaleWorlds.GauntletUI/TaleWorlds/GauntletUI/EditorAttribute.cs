using System;

namespace TaleWorlds.GauntletUI;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EditorAttribute : Attribute
{
	public readonly bool IncludeInnerProperties;

	public EditorAttribute(bool includeInnerProperties = false)
	{
		IncludeInnerProperties = includeInnerProperties;
	}
}
