using System;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class PremadeServerAction
{
	public Action Execute { get; private set; }

	public PremadeGameEntry GameServerEntry { get; private set; }

	public string Name { get; private set; }

	public PremadeServerAction(Action execute, PremadeGameEntry gameServerEntry, string name)
	{
		Execute = execute;
		GameServerEntry = gameServerEntry;
		Name = name;
	}
}
