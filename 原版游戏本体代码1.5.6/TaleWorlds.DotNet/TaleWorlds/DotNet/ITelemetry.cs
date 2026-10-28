using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

[LibraryInterfaceBase]
internal interface ITelemetry
{
	[EngineMethod("get_telemetry_level_mask", false, null, false)]
	TelemetryLevelMask GetTelemetryLevelMask();

	[EngineMethod("start_telemetry_connection", false, null, false)]
	void StartTelemetryConnection(bool showErrors);

	[EngineMethod("stop_telemetry_connection", false, null, false)]
	void StopTelemetryConnection();

	[EngineMethod("begin_telemetry_scope", false, null, false)]
	void BeginTelemetryScope(TelemetryLevelMask levelMask, string scopeName);

	[EngineMethod("end_telemetry_scope", false, null, false)]
	void EndTelemetryScope();

	[EngineMethod("has_telemetry_connection", false, null, false)]
	bool HasTelemetryConnection();
}
