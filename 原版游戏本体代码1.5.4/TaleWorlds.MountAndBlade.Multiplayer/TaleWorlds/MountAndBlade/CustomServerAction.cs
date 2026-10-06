using System;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class CustomServerAction
{
	public Action Execute { get; private set; }

	public GameServerEntry GameServerEntry { get; private set; }

	public string Name { get; private set; }

	public CustomServerAction(Action execute, GameServerEntry gameServerEntry, string name)
	{
		Execute = execute;
		GameServerEntry = gameServerEntry;
		Name = name;
	}
}
