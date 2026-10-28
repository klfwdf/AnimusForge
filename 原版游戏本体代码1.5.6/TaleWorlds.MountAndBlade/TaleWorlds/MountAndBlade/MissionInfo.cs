using System;
using System.Reflection;

namespace TaleWorlds.MountAndBlade;

public class MissionInfo
{
	public string Name { get; set; }

	public MethodInfo Creator { get; set; }

	public Type Manager { get; set; }

	public bool UsableByEditor { get; set; }
}
