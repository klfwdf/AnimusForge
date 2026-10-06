using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TaleWorlds.Library;

public static class Debug
{
	public enum DebugColor
	{
		DarkRed,
		DarkGreen,
		DarkBlue,
		Red,
		Green,
		Blue,
		DarkCyan,
		Cyan,
		DarkYellow,
		Yellow,
		Purple,
		Magenta,
		White,
		BrightWhite
	}

	public enum DebugUserFilter : ulong
	{
		None = 0uL,
		Unused0 = 1uL,
		Unused1 = 2uL,
		Koray = 4uL,
		Armagan = 8uL,
		Intern = 16uL,
		Mustafa = 32uL,
		Oguzhan = 64uL,
		Omer = 128uL,
		Ates = 256uL,
		Unused3 = 512uL,
		Basak = 1024uL,
		Can = 2048uL,
		Unused4 = 4096uL,
		Cem = 8192uL,
		Unused5 = 16384uL,
		Unused6 = 32768uL,
		Emircan = 65536uL,
		Unused7 = 131072uL,
		All = 4294967295uL,
		Default = 0uL,
		DamageDebug = 72uL
	}

	public enum DebugSystemFilter : ulong
	{
		None = 0uL,
		Graphics = 4294967296uL,
		ArtificialIntelligence = 8589934592uL,
		MultiPlayer = 17179869184uL,
		IO = 34359738368uL,
		Network = 68719476736uL,
		CampaignEvents = 137438953472uL,
		MemoryManager = 274877906944uL,
		TCP = 549755813888uL,
		FileManager = 1099511627776uL,
		NaturalInteractionDevice = 2199023255552uL,
		UDP = 4398046511104uL,
		ResourceManager = 8796093022208uL,
		Mono = 17592186044416uL,
		ONO = 35184372088832uL,
		Old = 70368744177664uL,
		Sound = 281474976710656uL,
		CombatLog = 562949953421312uL,
		Notifications = 1125899906842624uL,
		Quest = 2251799813685248uL,
		Dialog = 4503599627370496uL,
		Steam = 9007199254740992uL,
		All = 18446744069414584320uL,
		DefaultMask = 18446744069414584320uL
	}

	public static IDebugManager DebugManager { get; set; }

	public static ITelemetryManager TelemetryManager { get; set; }

	public static event Action<string, ulong> OnPrint;

	public static TelemetryLevelMask GetTelemetryLevelMask()
	{
		return TelemetryManager?.GetTelemetryLevelMask() ?? TelemetryLevelMask.Mono_0;
	}

	public static void SetCrashReportCustomString(string customString)
	{
		if (DebugManager != null)
		{
			DebugManager.SetCrashReportCustomString(customString);
		}
	}

	public static void SetCrashReportCustomStack(string customStack)
	{
		if (DebugManager != null)
		{
			DebugManager.SetCrashReportCustomStack(customStack);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void Assert(bool condition, string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
	{
		if (DebugManager != null)
		{
			DebugManager.Assert(condition, message, callerFile, callerMethod, callerLine);
		}
	}

	public static void FailedAssert(string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
	{
		if (DebugManager != null)
		{
			DebugManager.Assert(condition: false, message, callerFile, callerMethod, callerLine);
		}
	}

	public static void SilentAssert(bool condition, string message = "", bool getDump = false, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
	{
		if (DebugManager != null)
		{
			DebugManager.SilentAssert(condition, message, getDump, callerFile, callerMethod, callerLine);
		}
	}

	public static void ShowError(string message)
	{
		if (DebugManager != null)
		{
			DebugManager.ShowError(message);
		}
	}

	internal static void DoDelayedexit(int returnCode)
	{
		if (DebugManager != null)
		{
			DebugManager.DoDelayedexit(returnCode);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void ShowWarning(string message)
	{
		if (DebugManager != null)
		{
			DebugManager.ShowWarning(message);
		}
	}

	public static void ReportMemoryBookmark(string message)
	{
		DebugManager?.ReportMemoryBookmark(message);
	}

	public static void Print(string message, int logLevel = 0, DebugColor color = DebugColor.White, ulong debugFilter = 17592186044416uL)
	{
		if (DebugManager != null)
		{
			debugFilter &= 0xFFFFFFFF00000000uL;
			if (debugFilter != 0L)
			{
				DebugManager.Print(message, logLevel, color, debugFilter);
				Debug.OnPrint?.Invoke(message, debugFilter);
			}
		}
	}

	public static void ShowMessageBox(string lpText, string lpCaption, uint uType)
	{
		if (DebugManager != null)
		{
			DebugManager.ShowMessageBox(lpText, lpCaption, uType);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void PrintWarning(string warning, ulong debugFilter = 17592186044416uL)
	{
		if (DebugManager != null)
		{
			DebugManager.PrintWarning(warning, debugFilter);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void PrintError(string error, string stackTrace = null, ulong debugFilter = 17592186044416uL)
	{
		if (DebugManager != null)
		{
			DebugManager.PrintError(error, stackTrace, debugFilter);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void DisplayDebugMessage(string message)
	{
		if (DebugManager != null)
		{
			DebugManager.DisplayDebugMessage(message);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void WatchVariable(string name, object value)
	{
		DebugManager?.WatchVariable(name, value);
	}

	[Conditional("NOT_SHIPPING")]
	[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
	public static void StartTelemetryConnection(bool showErrors)
	{
		TelemetryManager?.StartTelemetryConnection(showErrors);
	}

	[Conditional("NOT_SHIPPING")]
	[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
	public static void StopTelemetryConnection()
	{
		TelemetryManager?.StopTelemetryConnection();
	}

	[Conditional("NOT_SHIPPING")]
	[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
	internal static void BeginTelemetryScopeInternal(TelemetryLevelMask levelMask, string scopeName)
	{
		TelemetryManager?.BeginTelemetryScopeInternal(levelMask, scopeName);
	}

	[Conditional("NOT_SHIPPING")]
	[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
	internal static void BeginTelemetryScopeBaseLevelInternal(TelemetryLevelMask levelMask, string scopeName)
	{
		TelemetryManager?.BeginTelemetryScopeBaseLevelInternal(levelMask, scopeName);
	}

	[Conditional("NOT_SHIPPING")]
	[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
	internal static void EndTelemetryScopeInternal()
	{
		TelemetryManager?.EndTelemetryScopeInternal();
	}

	[Conditional("NOT_SHIPPING")]
	[Conditional("ENABLE_PROFILING_APIS_IN_SHIPPING")]
	internal static void EndTelemetryScopeBaseLevelInternal()
	{
		TelemetryManager?.EndTelemetryScopeBaseLevelInternal();
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void WriteDebugLineOnScreen(string message)
	{
		DebugManager?.WriteDebugLineOnScreen(message);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugLine(Vec3 position, Vec3 direction, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		DebugManager?.RenderDebugLine(position, direction, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugLineWithThickness(Vec3 position, Vec3 direction, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f, int thickness = 0)
	{
		Vec3 vec = direction.AsVec2.RightVec().ToVec3();
		vec.Normalize();
		vec *= 0.005f;
		for (int i = 0; i < thickness; i++)
		{
			DebugManager?.RenderDebugLine(position + vec * i, direction, color, depthCheck, time);
			DebugManager?.RenderDebugLine(position + vec * -i, direction, color, depthCheck, time);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugSphere(Vec3 position, float radius, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		DebugManager?.RenderDebugSphere(position, radius, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugFrame(MatrixFrame frame, float lineLength, float time = 0f)
	{
		DebugManager?.RenderDebugFrame(frame, lineLength, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugText(float screenX, float screenY, string text, uint color = uint.MaxValue, float time = 0f)
	{
		DebugManager?.RenderDebugText(screenX, screenY, text, color, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color = uint.MaxValue)
	{
		DebugManager?.RenderDebugRectWithColor(left, bottom, right, top, color);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugText3D(Vec3 position, string text, uint color = uint.MaxValue, int screenPosOffsetX = 0, int screenPosOffsetY = 0, float time = 0f)
	{
		DebugManager?.RenderDebugText3D(position, text, color, screenPosOffsetX, screenPosOffsetY, time);
	}

	public static Vec3 GetDebugVector()
	{
		return DebugManager?.GetDebugVector() ?? Vec3.Zero;
	}

	public static void SetDebugVector(Vec3 value)
	{
		DebugManager?.SetDebugVector(value);
	}

	public static void SetTestModeEnabled(bool testModeEnabled)
	{
		DebugManager?.SetTestModeEnabled(testModeEnabled);
	}

	public static void AbortGame()
	{
		if (DebugManager != null)
		{
			DebugManager.AbortGame();
		}
	}
}
