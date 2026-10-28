using System;

namespace TaleWorlds.MountAndBlade;

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommandMethod : Attribute
{
	public string CommandName { get; private set; }

	public string Description { get; private set; }

	public ConsoleCommandMethod(string commandName, string description)
	{
		CommandName = commandName;
		Description = description;
	}
}
