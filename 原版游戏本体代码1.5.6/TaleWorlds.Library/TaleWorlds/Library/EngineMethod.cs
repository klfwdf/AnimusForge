using System;

namespace TaleWorlds.Library;

public class EngineMethod : Attribute
{
	public string EngineMethodName { get; private set; }

	public bool ActivateTelemetryProfiling { get; private set; }

	public string[] Conditionals { get; private set; }

	public bool IsMonoInline { get; private set; }

	public EngineMethod(string engineMethodName, bool activateTelemetryProfiling = false, string[] conditionals = null, bool isMonoInline = false)
	{
		EngineMethodName = engineMethodName;
		ActivateTelemetryProfiling = activateTelemetryProfiling;
		Conditionals = conditionals;
		IsMonoInline = isMonoInline;
	}
}
