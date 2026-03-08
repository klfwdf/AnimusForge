using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000047 RID: 71
	public interface ITelemetryManager
	{
		// Token: 0x06000244 RID: 580
		TelemetryLevelMask GetTelemetryLevelMask();

		// Token: 0x06000245 RID: 581
		void StartTelemetryConnection(bool showErrors);

		// Token: 0x06000246 RID: 582
		void StopTelemetryConnection();

		// Token: 0x06000247 RID: 583
		void BeginTelemetryScopeInternal(TelemetryLevelMask levelMask, string scopeName);

		// Token: 0x06000248 RID: 584
		void BeginTelemetryScopeBaseLevelInternal(TelemetryLevelMask levelMask, string scopeName);

		// Token: 0x06000249 RID: 585
		void EndTelemetryScopeInternal();

		// Token: 0x0600024A RID: 586
		void EndTelemetryScopeBaseLevelInternal();
	}
}
