using System;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class CustomServerAction
{
	public Action Execute { get; private set; }

	public GameServerEntry GameServerEntry { get; private set; }

	public string Name { get; private set; }

	public bool IsEnabled { get; private set; }

	public TextObject DisabledReason { get; private set; }

	public CustomServerAction(Action execute, GameServerEntry gameServerEntry, string name)
	{
		Execute = execute;
		GameServerEntry = gameServerEntry;
		Name = name;
		IsEnabled = true;
	}

	public CustomServerAction(Action execute, GameServerEntry gameServerEntry, string name, TextObject disabledReason)
		: this(execute, gameServerEntry, name)
	{
		IsEnabled = false;
		DisabledReason = disabledReason;
	}
}
