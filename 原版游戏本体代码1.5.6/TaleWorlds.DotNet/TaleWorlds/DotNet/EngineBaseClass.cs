using System;

namespace TaleWorlds.DotNet;

public abstract class EngineBaseClass : Attribute
{
	public string EngineType { get; set; }

	protected EngineBaseClass(string engineType)
	{
		EngineType = engineType;
	}
}
