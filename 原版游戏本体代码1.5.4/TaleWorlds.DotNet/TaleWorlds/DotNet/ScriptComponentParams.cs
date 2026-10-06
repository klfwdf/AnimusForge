using System;

namespace TaleWorlds.DotNet;

public class ScriptComponentParams : Attribute
{
	public string Tag { get; set; }

	public string NameOverride { get; set; }

	public ScriptComponentParams(string tag = "", string nameOverride = "")
	{
		Tag = tag;
		NameOverride = nameOverride;
	}
}
