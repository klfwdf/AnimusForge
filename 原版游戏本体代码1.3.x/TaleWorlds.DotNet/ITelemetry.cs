using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000018 RID: 24
	[LibraryInterfaceBase]
	internal interface ITelemetry
	{
		// Token: 0x06000073 RID: 115
		[EngineMethod("get_telemetry_level_mask", false, null, false)]
		TelemetryLevelMask GetTelemetryLevelMask();

		// Token: 0x06000074 RID: 116
		[EngineMethod("start_telemetry_connection", false, null, false)]
		void StartTelemetryConnection(bool showErrors);

		// Token: 0x06000075 RID: 117
		[EngineMethod("stop_telemetry_connection", false, null, false)]
		void StopTelemetryConnection();

		// Token: 0x06000076 RID: 118
		[EngineMethod("begin_telemetry_scope", false, null, false)]
		void BeginTelemetryScope(TelemetryLevelMask levelMask, string scopeName);

		// Token: 0x06000077 RID: 119
		[EngineMethod("end_telemetry_scope", false, null, false)]
		void EndTelemetryScope();

		// Token: 0x06000078 RID: 120
		[EngineMethod("has_telemetry_connection", false, null, false)]
		bool HasTelemetryConnection();
	}
}
