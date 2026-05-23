using System;

namespace TaleWorlds.Library;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class VirtualDirectoryAttribute : Attribute
{
	public string Name { get; private set; }

	public VirtualDirectoryAttribute(string name)
	{
		Name = name;
	}
}
