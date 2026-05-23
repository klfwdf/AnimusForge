using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class MBDebug
{
	[Flags]
	public enum MessageBoxTypeFlag
	{
		Ok = 1,
		Warning = 2,
		Error = 4,
		OkCancel = 8,
		RetryCancel = 0x10,
		YesNo = 0x20,
		YesNoCancel = 0x40,
		Information = 0x80,
		Exclamation = 0x100,
		Question = 0x200,
		AssertFailed = 0x400
	}

	public static bool DisableAllUI;

	public static bool TestModeEnabled;

	public static bool ShouldAssertThrowException;

	public static bool IsDisplayingHighLevelAI;

	public static bool DisableLogging;

	private static readonly Dictionary<string, int> ProcessedFrameList;

	public static Vec3 DebugVector
	{
		get
		{
			return EngineApplicationInterface.IDebug.GetDebugVector();
		}
		set
		{
			EngineApplicationInterface.IDebug.SetDebugVector(value);
		}
	}

	public static int ShowDebugInfoState
	{
		get
		{
			return EngineApplicationInterface.IDebug.GetShowDebugInfo();
		}
		set
		{
			EngineApplicationInterface.IDebug.SetShowDebugInfo(value);
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("toggle_ui", "ui")]
	public static string DisableUI(List<string> strings)
	{
		if (strings.Count == 0)
		{
			DisableAllUI = !DisableAllUI;
			if (DisableAllUI)
			{
				return "UI is now disabled.";
			}
			return "UI is now enabled.";
		}
		return "Invalid input.";
	}

	static MBDebug()
	{
		ProcessedFrameList = new Dictionary<string, int>();
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void AssertMemoryUsage(int memoryMB)
	{
		EngineApplicationInterface.IDebug.AssertMemoryUsage(memoryMB);
	}

	public static void AbortGame(int ExitCode = 5)
	{
		EngineApplicationInterface.IDebug.AbortGame(ExitCode);
	}

	public static void ShowWarning(string message)
	{
		bool flag = EngineApplicationInterface.IDebug.Warning(message);
		if (Debugger.IsAttached && flag)
		{
			Debugger.Break();
		}
	}

	public static void ContentWarning(string message)
	{
		bool flag = EngineApplicationInterface.IDebug.ContentWarning(message);
		if (Debugger.IsAttached && flag)
		{
			Debugger.Break();
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void ConditionalContentWarning(bool condition, string message)
	{
		if (!condition)
		{
			bool flag = EngineApplicationInterface.IDebug.ContentWarning(message);
			if (Debugger.IsAttached && flag)
			{
				Debugger.Break();
			}
		}
	}

	public static void ShowError(string message)
	{
		bool flag = EngineApplicationInterface.IDebug.Error(message);
		if (Debugger.IsAttached && flag)
		{
			Debugger.Break();
		}
	}

	public static void ShowMessageBox(string lpText, string lpCaption, uint uType)
	{
		EngineApplicationInterface.IDebug.MessageBox(lpText, lpCaption, uType);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void Assert(bool condition, string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
	{
		if (!condition)
		{
			bool flag = EngineApplicationInterface.IDebug.FailedAssert(message, callerFile, callerMethod, callerLine);
			if (Debugger.IsAttached && flag)
			{
				Debugger.Break();
			}
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void FailedAssert(string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
	{
	}

	public static void SilentAssert(bool condition, string message = "", bool getDump = false, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
	{
		if (!condition)
		{
			bool flag = EngineApplicationInterface.IDebug.SilentAssert(message, callerFile, callerMethod, callerLine, getDump);
			if (Debugger.IsAttached && flag)
			{
				Debugger.Break();
			}
		}
	}

	[Conditional("DEBUG_MORE")]
	public static void AssertConditionOrCallerClassName(bool condition, string name)
	{
		StackFrame frame = new StackTrace(2, fNeedFileInfo: true).GetFrame(0);
		if (!condition)
		{
			_ = frame.GetMethod().DeclaringType.Name;
		}
	}

	[Conditional("DEBUG_MORE")]
	public static void AssertConditionOrCallerClassNameSearchAllCallstack(bool condition, string name)
	{
		StackTrace stackTrace = new StackTrace(fNeedFileInfo: true);
		if (!condition)
		{
			for (int i = 0; i < stackTrace.FrameCount && !(stackTrace.GetFrame(i).GetMethod().DeclaringType.Name == name); i++)
			{
			}
		}
	}

	public static void Print(string message, int logLevel = 0, TaleWorlds.Library.Debug.DebugColor color = TaleWorlds.Library.Debug.DebugColor.White, ulong debugFilter = 17592186044416uL)
	{
		if (DisableLogging)
		{
			return;
		}
		debugFilter &= 0xFFFFFFFF00000000uL;
		if (debugFilter == 0L)
		{
			return;
		}
		try
		{
			if (EngineApplicationInterface.IDebug != null)
			{
				EngineApplicationInterface.IDebug.WriteLine(logLevel, message, (int)color, debugFilter);
			}
		}
		catch
		{
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void ConsolePrint(string message, TaleWorlds.Library.Debug.DebugColor color = TaleWorlds.Library.Debug.DebugColor.White, ulong debugFilter = 17592186044416uL)
	{
		try
		{
			EngineApplicationInterface.IDebug.WriteLine(0, message, (int)color, debugFilter);
		}
		catch
		{
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void WriteDebugLineOnScreen(string str)
	{
		EngineApplicationInterface.IDebug.WriteDebugLineOnScreen(str);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugText(float screenX, float screenY, string text, uint color = uint.MaxValue, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugText(screenX, screenY, text, color, time);
	}

	public static void RenderText(float screenX, float screenY, string text, uint color = uint.MaxValue, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugText(screenX, screenY, text, color, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugRect(float left, float bottom, float right, float top)
	{
		EngineApplicationInterface.IDebug.RenderDebugRect(left, bottom, right, top);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color = uint.MaxValue)
	{
		EngineApplicationInterface.IDebug.RenderDebugRectWithColor(left, bottom, right, top, color);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugFrame(MatrixFrame frame, float lineLength, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugFrame(ref frame, lineLength, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugText3D(Vec3 worldPosition, string str, uint color = uint.MaxValue, int screenPosOffsetX = 0, int screenPosOffsetY = 0, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugText3d(worldPosition, str, color, screenPosOffsetX, screenPosOffsetY, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugDirectionArrow(Vec3 position, Vec3 direction, uint color = uint.MaxValue, bool depthCheck = false)
	{
		EngineApplicationInterface.IDebug.RenderDebugDirectionArrow(position, direction, color, depthCheck);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugLine(Vec3 position, Vec3 direction, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugLine(position, direction, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugSphere(Vec3 position, float radius, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugSphere(position, radius, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugCapsule(Vec3 p0, Vec3 p1, float radius, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugCapsule(p0, p1, radius, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugBoundingBoxOfEntity(GameEntity entity, MatrixFrame frame, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		Vec3 boundingBoxMin = entity.GetBoundingBoxMin();
		Vec3 boundingBoxMax = entity.GetBoundingBoxMax();
		List<Vec3> list = new List<Vec3>();
		list.Add(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z));
		list.Add(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z));
		list.Add(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z));
		list.Add(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z));
		list.Add(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z));
		list.Add(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z));
		list.Add(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z));
		list.Add(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z));
		for (int i = 0; i < list.Count / 2; i++)
		{
			frame.TransformToParent(list[i]);
			frame.TransformToParent(list[(i + 1) % (list.Count / 2)]);
			frame.TransformToParent(list[i + list.Count / 2]);
			frame.TransformToParent(list[(i + 1) % (list.Count / 2) + list.Count / 2]);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugBoundingBox(BoundingBox box, MatrixFrame frame, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		Vec3 min = box.min;
		Vec3 max = box.max;
		List<Vec3> list = new List<Vec3>();
		list.Add(new Vec3(min.x, min.y, min.z));
		list.Add(new Vec3(max.x, min.y, min.z));
		list.Add(new Vec3(max.x, max.y, min.z));
		list.Add(new Vec3(min.x, max.y, min.z));
		list.Add(new Vec3(min.x, min.y, max.z));
		list.Add(new Vec3(max.x, min.y, max.z));
		list.Add(new Vec3(max.x, max.y, max.z));
		list.Add(new Vec3(min.x, max.y, max.z));
		for (int i = 0; i < list.Count / 2; i++)
		{
			frame.TransformToParent(list[i]);
			frame.TransformToParent(list[(i + 1) % (list.Count / 2)]);
			frame.TransformToParent(list[i + list.Count / 2]);
			frame.TransformToParent(list[(i + 1) % (list.Count / 2) + list.Count / 2]);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void ClearRenderObjects()
	{
		EngineApplicationInterface.IDebug.ClearAllDebugRenderObjects();
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugBoxObject(Vec3 min, Vec3 max, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugBoxObject(min, max, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void RenderDebugBoxObject(Vec3 min, Vec3 max, MatrixFrame frame, uint color = uint.MaxValue, bool depthCheck = false, float time = 0f)
	{
		EngineApplicationInterface.IDebug.RenderDebugBoxObjectWithFrame(min, max, ref frame, color, depthCheck, time);
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void PostWarningLine(string line)
	{
		EngineApplicationInterface.IDebug.PostWarningLine(line);
	}

	public static bool IsErrorReportModeActive()
	{
		return EngineApplicationInterface.IDebug.IsErrorReportModeActive();
	}

	public static bool IsErrorReportModePauseMission()
	{
		return EngineApplicationInterface.IDebug.IsErrorReportModePauseMission();
	}

	public static void SetErrorReportScene(Scene scene)
	{
		UIntPtr errorReportScene = ((scene == null) ? UIntPtr.Zero : scene.Pointer);
		EngineApplicationInterface.IDebug.SetErrorReportScene(errorReportScene);
	}

	public static void SetDumpGenerationDisabled(bool value)
	{
		EngineApplicationInterface.IDebug.SetDumpGenerationDisabled(value);
	}

	public static void EchoCommandWindow(string content)
	{
		EngineApplicationInterface.IDebug.EchoCommandWindow(content);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("clear", "console")]
	public static string ClearConsole(List<string> strings)
	{
		Console.Clear();
		return "Debug console cleared.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("echo_command_window", "console")]
	public static string EchoCommandWindow(List<string> strings)
	{
		EchoCommandWindow(strings[0]);
		return "";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("echo_command_window_test", "console")]
	public static string EchoCommandWindowTest(List<string> strings)
	{
		EchoCommandWindowTestAux();
		return "";
	}

	private static async void EchoCommandWindowTestAux()
	{
		EchoCommandWindow("5...");
		await Task.Delay(1000);
		EchoCommandWindow("4...");
		await Task.Delay(1000);
		EchoCommandWindow("3...");
		await Task.Delay(1000);
		EchoCommandWindow("2...");
		await Task.Delay(1000);
		EchoCommandWindow("1...");
		await Task.Delay(1000);
		EchoCommandWindow("Tada!");
	}

	public static bool IsTestMode()
	{
		return EngineApplicationInterface.IDebug.IsTestMode();
	}
}
