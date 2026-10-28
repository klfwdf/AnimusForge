using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data;

internal struct ViewBindCommandInfo
{
	internal GauntletView Owner { get; private set; }

	internal string Command { get; private set; }

	internal BindingPath Path { get; private set; }

	internal string Parameter { get; private set; }

	internal ViewBindCommandInfo(GauntletView view, string command, BindingPath path, string parameter)
	{
		Owner = view;
		Command = command;
		Path = path;
		Parameter = parameter;
	}
}
