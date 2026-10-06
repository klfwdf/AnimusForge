using System;

namespace TaleWorlds.MountAndBlade;

public class ViewMethod : Attribute
{
	public string Name { get; private set; }

	public ViewMethod(string name)
	{
		Name = name;
	}
}
