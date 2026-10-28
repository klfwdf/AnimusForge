namespace TaleWorlds.Library;

public interface ITelemetryManager
{
	TelemetryLevelMask GetTelemetryLevelMask();

	void StartTelemetryConnection(bool showErrors);

	void StopTelemetryConnection();

	void BeginTelemetryScopeInternal(TelemetryLevelMask levelMask, string scopeName);

	void BeginTelemetryScopeBaseLevelInternal(TelemetryLevelMask levelMask, string scopeName);

	void EndTelemetryScopeInternal();

	void EndTelemetryScopeBaseLevelInternal();
}
