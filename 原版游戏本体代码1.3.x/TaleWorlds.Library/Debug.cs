using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TaleWorlds.Library
{
	// Token: 0x02000029 RID: 41
	public static class Debug
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000101 RID: 257 RVA: 0x000058B0 File Offset: 0x00003AB0
		// (remove) Token: 0x06000102 RID: 258 RVA: 0x000058E4 File Offset: 0x00003AE4
		public static event Action<string, ulong> OnPrint;

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000103 RID: 259 RVA: 0x00005917 File Offset: 0x00003B17
		// (set) Token: 0x06000104 RID: 260 RVA: 0x0000591E File Offset: 0x00003B1E
		public static IDebugManager DebugManager { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000105 RID: 261 RVA: 0x00005926 File Offset: 0x00003B26
		// (set) Token: 0x06000106 RID: 262 RVA: 0x0000592D File Offset: 0x00003B2D
		public static ITelemetryManager TelemetryManager { get; set; }

		// Token: 0x06000107 RID: 263 RVA: 0x00005935 File Offset: 0x00003B35
		public static TelemetryLevelMask GetTelemetryLevelMask()
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return TelemetryLevelMask.Mono_0;
			}
			return telemetryManager.GetTelemetryLevelMask();
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000594B File Offset: 0x00003B4B
		public static void SetCrashReportCustomString(string customString)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.SetCrashReportCustomString(customString);
			}
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000595F File Offset: 0x00003B5F
		public static void SetCrashReportCustomStack(string customStack)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.SetCrashReportCustomStack(customStack);
			}
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00005973 File Offset: 0x00003B73
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void Assert(bool condition, string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.Assert(condition, message, callerFile, callerMethod, callerLine);
			}
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000598C File Offset: 0x00003B8C
		public static void FailedAssert(string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.Assert(false, message, callerFile, callerMethod, callerLine);
			}
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000059A4 File Offset: 0x00003BA4
		public static void SilentAssert(bool condition, string message = "", bool getDump = false, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.SilentAssert(condition, message, getDump, callerFile, callerMethod, callerLine);
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x000059BF File Offset: 0x00003BBF
		public static void ShowError(string message)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.ShowError(message);
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x000059D3 File Offset: 0x00003BD3
		internal static void DoDelayedexit(int returnCode)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.DoDelayedexit(returnCode);
			}
		}

		// Token: 0x0600010F RID: 271 RVA: 0x000059E7 File Offset: 0x00003BE7
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void ShowWarning(string message)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.ShowWarning(message);
			}
		}

		// Token: 0x06000110 RID: 272 RVA: 0x000059FB File Offset: 0x00003BFB
		public static void ReportMemoryBookmark(string message)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.ReportMemoryBookmark(message);
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00005A0D File Offset: 0x00003C0D
		public static void Print(string message, int logLevel = 0, Debug.DebugColor color = Debug.DebugColor.White, ulong debugFilter = 17592186044416UL)
		{
			if (Debug.DebugManager != null)
			{
				debugFilter &= 18446744069414584320UL;
				if (debugFilter == 0UL)
				{
					return;
				}
				Debug.DebugManager.Print(message, logLevel, color, debugFilter);
				Action<string, ulong> onPrint = Debug.OnPrint;
				if (onPrint == null)
				{
					return;
				}
				onPrint(message, debugFilter);
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00005A46 File Offset: 0x00003C46
		public static void ShowMessageBox(string lpText, string lpCaption, uint uType)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.ShowMessageBox(lpText, lpCaption, uType);
			}
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00005A5C File Offset: 0x00003C5C
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void PrintWarning(string warning, ulong debugFilter = 17592186044416UL)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.PrintWarning(warning, debugFilter);
			}
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00005A71 File Offset: 0x00003C71
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void PrintError(string error, string stackTrace = null, ulong debugFilter = 17592186044416UL)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.PrintError(error, stackTrace, debugFilter);
			}
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00005A87 File Offset: 0x00003C87
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void DisplayDebugMessage(string message)
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.DisplayDebugMessage(message);
			}
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00005A9B File Offset: 0x00003C9B
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void WatchVariable(string name, object value)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.WatchVariable(name, value);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00005AAE File Offset: 0x00003CAE
		[Conditional("NOT_SHIPPING")]
		[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
		public static void StartTelemetryConnection(bool showErrors)
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return;
			}
			telemetryManager.StartTelemetryConnection(showErrors);
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00005AC0 File Offset: 0x00003CC0
		[Conditional("NOT_SHIPPING")]
		[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
		public static void StopTelemetryConnection()
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return;
			}
			telemetryManager.StopTelemetryConnection();
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00005AD1 File Offset: 0x00003CD1
		[Conditional("NOT_SHIPPING")]
		[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
		internal static void BeginTelemetryScopeInternal(TelemetryLevelMask levelMask, string scopeName)
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return;
			}
			telemetryManager.BeginTelemetryScopeInternal(levelMask, scopeName);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00005AE4 File Offset: 0x00003CE4
		[Conditional("NOT_SHIPPING")]
		[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
		internal static void BeginTelemetryScopeBaseLevelInternal(TelemetryLevelMask levelMask, string scopeName)
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return;
			}
			telemetryManager.BeginTelemetryScopeBaseLevelInternal(levelMask, scopeName);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00005AF7 File Offset: 0x00003CF7
		[Conditional("NOT_SHIPPING")]
		[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
		internal static void EndTelemetryScopeInternal()
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return;
			}
			telemetryManager.EndTelemetryScopeInternal();
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00005B08 File Offset: 0x00003D08
		[Conditional("NOT_SHIPPING")]
		[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
		internal static void EndTelemetryScopeBaseLevelInternal()
		{
			ITelemetryManager telemetryManager = Debug.TelemetryManager;
			if (telemetryManager == null)
			{
				return;
			}
			telemetryManager.EndTelemetryScopeBaseLevelInternal();
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00005B19 File Offset: 0x00003D19
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void WriteDebugLineOnScreen(string message)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.WriteDebugLineOnScreen(message);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00005B2B File Offset: 0x00003D2B
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugLine(Vec3 position, Vec3 direction, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.RenderDebugLine(position, direction, color, depthCheck, time);
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00005B44 File Offset: 0x00003D44
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugLineWithThickness(Vec3 position, Vec3 direction, uint color = 4294967295U, bool depthCheck = false, float time = 0f, int thickness = 0)
		{
			Vec3 v = direction.AsVec2.RightVec().ToVec3(0f);
			v.Normalize();
			v *= 0.005f;
			for (int i = 0; i < thickness; i++)
			{
				IDebugManager debugManager = Debug.DebugManager;
				if (debugManager != null)
				{
					debugManager.RenderDebugLine(position + v * (float)i, direction, color, depthCheck, time);
				}
				IDebugManager debugManager2 = Debug.DebugManager;
				if (debugManager2 != null)
				{
					debugManager2.RenderDebugLine(position + v * (float)(-(float)i), direction, color, depthCheck, time);
				}
			}
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00005BD6 File Offset: 0x00003DD6
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugSphere(Vec3 position, float radius, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.RenderDebugSphere(position, radius, color, depthCheck, time);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00005BED File Offset: 0x00003DED
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugFrame(MatrixFrame frame, float lineLength, float time = 0f)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.RenderDebugFrame(frame, lineLength, time);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00005C01 File Offset: 0x00003E01
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugText(float screenX, float screenY, string text, uint color = 4294967295U, float time = 0f)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.RenderDebugText(screenX, screenY, text, color, time);
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00005C18 File Offset: 0x00003E18
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color = 4294967295U)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.RenderDebugRectWithColor(left, bottom, right, top, color);
		}

		// Token: 0x06000124 RID: 292 RVA: 0x00005C2F File Offset: 0x00003E2F
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugText3D(Vec3 position, string text, uint color = 4294967295U, int screenPosOffsetX = 0, int screenPosOffsetY = 0, float time = 0f)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.RenderDebugText3D(position, text, color, screenPosOffsetX, screenPosOffsetY, time);
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00005C48 File Offset: 0x00003E48
		public static Vec3 GetDebugVector()
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return Vec3.Zero;
			}
			return debugManager.GetDebugVector();
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00005C5E File Offset: 0x00003E5E
		public static void SetDebugVector(Vec3 value)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.SetDebugVector(value);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00005C70 File Offset: 0x00003E70
		public static void SetTestModeEnabled(bool testModeEnabled)
		{
			IDebugManager debugManager = Debug.DebugManager;
			if (debugManager == null)
			{
				return;
			}
			debugManager.SetTestModeEnabled(testModeEnabled);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00005C82 File Offset: 0x00003E82
		public static void AbortGame()
		{
			if (Debug.DebugManager != null)
			{
				Debug.DebugManager.AbortGame();
			}
		}

		// Token: 0x020000CD RID: 205
		public enum DebugColor
		{
			// Token: 0x0400025C RID: 604
			DarkRed,
			// Token: 0x0400025D RID: 605
			DarkGreen,
			// Token: 0x0400025E RID: 606
			DarkBlue,
			// Token: 0x0400025F RID: 607
			Red,
			// Token: 0x04000260 RID: 608
			Green,
			// Token: 0x04000261 RID: 609
			Blue,
			// Token: 0x04000262 RID: 610
			DarkCyan,
			// Token: 0x04000263 RID: 611
			Cyan,
			// Token: 0x04000264 RID: 612
			DarkYellow,
			// Token: 0x04000265 RID: 613
			Yellow,
			// Token: 0x04000266 RID: 614
			Purple,
			// Token: 0x04000267 RID: 615
			Magenta,
			// Token: 0x04000268 RID: 616
			White,
			// Token: 0x04000269 RID: 617
			BrightWhite
		}

		// Token: 0x020000CE RID: 206
		public enum DebugUserFilter : ulong
		{
			// Token: 0x0400026B RID: 619
			None,
			// Token: 0x0400026C RID: 620
			Unused0,
			// Token: 0x0400026D RID: 621
			Unused1,
			// Token: 0x0400026E RID: 622
			Koray = 4UL,
			// Token: 0x0400026F RID: 623
			Armagan = 8UL,
			// Token: 0x04000270 RID: 624
			Intern = 16UL,
			// Token: 0x04000271 RID: 625
			Mustafa = 32UL,
			// Token: 0x04000272 RID: 626
			Oguzhan = 64UL,
			// Token: 0x04000273 RID: 627
			Omer = 128UL,
			// Token: 0x04000274 RID: 628
			Ates = 256UL,
			// Token: 0x04000275 RID: 629
			Unused3 = 512UL,
			// Token: 0x04000276 RID: 630
			Basak = 1024UL,
			// Token: 0x04000277 RID: 631
			Can = 2048UL,
			// Token: 0x04000278 RID: 632
			Unused4 = 4096UL,
			// Token: 0x04000279 RID: 633
			Cem = 8192UL,
			// Token: 0x0400027A RID: 634
			Unused5 = 16384UL,
			// Token: 0x0400027B RID: 635
			Unused6 = 32768UL,
			// Token: 0x0400027C RID: 636
			Emircan = 65536UL,
			// Token: 0x0400027D RID: 637
			Unused7 = 131072UL,
			// Token: 0x0400027E RID: 638
			All = 4294967295UL,
			// Token: 0x0400027F RID: 639
			Default = 0UL,
			// Token: 0x04000280 RID: 640
			DamageDebug = 72UL
		}

		// Token: 0x020000CF RID: 207
		public enum DebugSystemFilter : ulong
		{
			// Token: 0x04000282 RID: 642
			None,
			// Token: 0x04000283 RID: 643
			Graphics = 4294967296UL,
			// Token: 0x04000284 RID: 644
			ArtificialIntelligence = 8589934592UL,
			// Token: 0x04000285 RID: 645
			MultiPlayer = 17179869184UL,
			// Token: 0x04000286 RID: 646
			IO = 34359738368UL,
			// Token: 0x04000287 RID: 647
			Network = 68719476736UL,
			// Token: 0x04000288 RID: 648
			CampaignEvents = 137438953472UL,
			// Token: 0x04000289 RID: 649
			MemoryManager = 274877906944UL,
			// Token: 0x0400028A RID: 650
			TCP = 549755813888UL,
			// Token: 0x0400028B RID: 651
			FileManager = 1099511627776UL,
			// Token: 0x0400028C RID: 652
			NaturalInteractionDevice = 2199023255552UL,
			// Token: 0x0400028D RID: 653
			UDP = 4398046511104UL,
			// Token: 0x0400028E RID: 654
			ResourceManager = 8796093022208UL,
			// Token: 0x0400028F RID: 655
			Mono = 17592186044416UL,
			// Token: 0x04000290 RID: 656
			ONO = 35184372088832UL,
			// Token: 0x04000291 RID: 657
			Old = 70368744177664UL,
			// Token: 0x04000292 RID: 658
			Sound = 281474976710656UL,
			// Token: 0x04000293 RID: 659
			CombatLog = 562949953421312UL,
			// Token: 0x04000294 RID: 660
			Notifications = 1125899906842624UL,
			// Token: 0x04000295 RID: 661
			Quest = 2251799813685248UL,
			// Token: 0x04000296 RID: 662
			Dialog = 4503599627370496UL,
			// Token: 0x04000297 RID: 663
			Steam = 9007199254740992UL,
			// Token: 0x04000298 RID: 664
			All = 18446744069414584320UL,
			// Token: 0x04000299 RID: 665
			DefaultMask = 18446744069414584320UL
		}
	}
}
