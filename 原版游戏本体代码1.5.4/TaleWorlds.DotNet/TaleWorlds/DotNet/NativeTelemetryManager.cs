using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

public class NativeTelemetryManager : ITelemetryManager
{
	public static TelemetryLevelMask TelemetryLevelMask { get; private set; }

	public TelemetryLevelMask GetTelemetryLevelMask()
	{
		return TelemetryLevelMask;
	}

	public NativeTelemetryManager()
	{
		TelemetryLevelMask = TelemetryLevelMask.Mono_0;
	}

	internal void Update()
	{
		TelemetryLevelMask = LibraryApplicationInterface.ITelemetry.GetTelemetryLevelMask();
	}

	public void StartTelemetryConnection(bool showErrors)
	{
		LibraryApplicationInterface.ITelemetry.StartTelemetryConnection(showErrors);
	}

	public void StopTelemetryConnection()
	{
		LibraryApplicationInterface.ITelemetry.StopTelemetryConnection();
	}

	public void BeginTelemetryScopeInternal(TelemetryLevelMask levelMask, string scopeName)
	{
		if (TelemetryLevelMask.HasAnyFlag(levelMask))
		{
			LibraryApplicationInterface.ITelemetry.BeginTelemetryScope(levelMask, scopeName);
		}
	}

	public void EndTelemetryScopeInternal()
	{
		LibraryApplicationInterface.ITelemetry.EndTelemetryScope();
	}

	public void BeginTelemetryScopeBaseLevelInternal(TelemetryLevelMask levelMask, string scopeName)
	{
		if (TelemetryLevelMask.HasAnyFlag(levelMask))
		{
			LibraryApplicationInterface.ITelemetry.BeginTelemetryScope(levelMask, scopeName);
		}
	}

	public void EndTelemetryScopeBaseLevelInternal()
	{
		LibraryApplicationInterface.ITelemetry.EndTelemetryScope();
	}
}
