using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000030 RID: 48
	public class NativeTelemetryManager : ITelemetryManager
	{
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000134 RID: 308 RVA: 0x0000578E File Offset: 0x0000398E
		// (set) Token: 0x06000135 RID: 309 RVA: 0x00005795 File Offset: 0x00003995
		public static TelemetryLevelMask TelemetryLevelMask { get; private set; }

		// Token: 0x06000136 RID: 310 RVA: 0x0000579D File Offset: 0x0000399D
		public TelemetryLevelMask GetTelemetryLevelMask()
		{
			return NativeTelemetryManager.TelemetryLevelMask;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x000057A4 File Offset: 0x000039A4
		public NativeTelemetryManager()
		{
			NativeTelemetryManager.TelemetryLevelMask = TelemetryLevelMask.Mono_0;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x000057B6 File Offset: 0x000039B6
		internal void Update()
		{
			NativeTelemetryManager.TelemetryLevelMask = LibraryApplicationInterface.ITelemetry.GetTelemetryLevelMask();
		}

		// Token: 0x06000139 RID: 313 RVA: 0x000057C7 File Offset: 0x000039C7
		public void StartTelemetryConnection(bool showErrors)
		{
			LibraryApplicationInterface.ITelemetry.StartTelemetryConnection(showErrors);
		}

		// Token: 0x0600013A RID: 314 RVA: 0x000057D4 File Offset: 0x000039D4
		public void StopTelemetryConnection()
		{
			LibraryApplicationInterface.ITelemetry.StopTelemetryConnection();
		}

		// Token: 0x0600013B RID: 315 RVA: 0x000057E0 File Offset: 0x000039E0
		public void BeginTelemetryScopeInternal(TelemetryLevelMask levelMask, string scopeName)
		{
			if (NativeTelemetryManager.TelemetryLevelMask.HasAnyFlag(levelMask))
			{
				LibraryApplicationInterface.ITelemetry.BeginTelemetryScope(levelMask, scopeName);
			}
		}

		// Token: 0x0600013C RID: 316 RVA: 0x000057FB File Offset: 0x000039FB
		public void EndTelemetryScopeInternal()
		{
			LibraryApplicationInterface.ITelemetry.EndTelemetryScope();
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00005807 File Offset: 0x00003A07
		public void BeginTelemetryScopeBaseLevelInternal(TelemetryLevelMask levelMask, string scopeName)
		{
			if (NativeTelemetryManager.TelemetryLevelMask.HasAnyFlag(levelMask))
			{
				LibraryApplicationInterface.ITelemetry.BeginTelemetryScope(levelMask, scopeName);
			}
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00005822 File Offset: 0x00003A22
		public void EndTelemetryScopeBaseLevelInternal()
		{
			LibraryApplicationInterface.ITelemetry.EndTelemetryScope();
		}
	}
}
