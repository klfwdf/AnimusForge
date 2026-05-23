using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class GeneratedBindCommandInfo
{
	public string Command { get; private set; }

	public string Path { get; private set; }

	public MethodInfo Method { get; internal set; }

	public int ParameterCount { get; internal set; }

	public List<GeneratedBindCommandParameterInfo> MethodParameters { get; private set; }

	public bool GotParameter { get; internal set; }

	public string Parameter { get; internal set; }

	internal GeneratedBindCommandInfo(string command, string path)
	{
		Command = command;
		Path = path;
		MethodParameters = new List<GeneratedBindCommandParameterInfo>();
	}
}
