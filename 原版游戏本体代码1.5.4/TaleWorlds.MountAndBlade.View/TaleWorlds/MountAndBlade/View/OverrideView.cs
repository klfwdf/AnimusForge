using System;

namespace TaleWorlds.MountAndBlade.View;

public class OverrideView : Attribute
{
	public Type BaseType { get; private set; }

	public OverrideView(Type baseType)
	{
		BaseType = baseType;
	}
}
