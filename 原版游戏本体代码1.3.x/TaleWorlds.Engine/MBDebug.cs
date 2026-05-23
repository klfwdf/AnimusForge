using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000061 RID: 97
	public static class MBDebug
	{
		// Token: 0x06000962 RID: 2402 RVA: 0x00008C42 File Offset: 0x00006E42
		[CommandLineFunctionality.CommandLineArgumentFunction("toggle_ui", "ui")]
		public static string DisableUI(List<string> strings)
		{
			if (strings.Count != 0)
			{
				return "Invalid input.";
			}
			MBDebug.DisableAllUI = !MBDebug.DisableAllUI;
			if (MBDebug.DisableAllUI)
			{
				return "UI is now disabled.";
			}
			return "UI is now enabled.";
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x00008C7D File Offset: 0x00006E7D
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void AssertMemoryUsage(int memoryMB)
		{
			EngineApplicationInterface.IDebug.AssertMemoryUsage(memoryMB);
		}

		// Token: 0x06000965 RID: 2405 RVA: 0x00008C8A File Offset: 0x00006E8A
		public static void AbortGame(int ExitCode = 5)
		{
			EngineApplicationInterface.IDebug.AbortGame(ExitCode);
		}

		// Token: 0x06000966 RID: 2406 RVA: 0x00008C98 File Offset: 0x00006E98
		public static void ShowWarning(string message)
		{
			bool flag = EngineApplicationInterface.IDebug.Warning(message);
			if (Debugger.IsAttached && flag)
			{
				Debugger.Break();
			}
		}

		// Token: 0x06000967 RID: 2407 RVA: 0x00008CC0 File Offset: 0x00006EC0
		public static void ContentWarning(string message)
		{
			bool flag = EngineApplicationInterface.IDebug.ContentWarning(message);
			if (Debugger.IsAttached && flag)
			{
				Debugger.Break();
			}
		}

		// Token: 0x06000968 RID: 2408 RVA: 0x00008CE8 File Offset: 0x00006EE8
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

		// Token: 0x06000969 RID: 2409 RVA: 0x00008D14 File Offset: 0x00006F14
		public static void ShowError(string message)
		{
			bool flag = EngineApplicationInterface.IDebug.Error(message);
			if (Debugger.IsAttached && flag)
			{
				Debugger.Break();
			}
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x00008D3B File Offset: 0x00006F3B
		public static void ShowMessageBox(string lpText, string lpCaption, uint uType)
		{
			EngineApplicationInterface.IDebug.MessageBox(lpText, lpCaption, uType);
		}

		// Token: 0x0600096B RID: 2411 RVA: 0x00008D4C File Offset: 0x00006F4C
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

		// Token: 0x0600096C RID: 2412 RVA: 0x00008D7A File Offset: 0x00006F7A
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void FailedAssert(string message, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMethod = "", [CallerLineNumber] int callerLine = 0)
		{
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x00008D7C File Offset: 0x00006F7C
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

		// Token: 0x0600096E RID: 2414 RVA: 0x00008DAC File Offset: 0x00006FAC
		[Conditional("DEBUG_MORE")]
		public static void AssertConditionOrCallerClassName(bool condition, string name)
		{
			StackFrame frame = new StackTrace(2, true).GetFrame(0);
			if (!condition)
			{
				string name2 = frame.GetMethod().DeclaringType.Name;
			}
		}

		// Token: 0x0600096F RID: 2415 RVA: 0x00008DDC File Offset: 0x00006FDC
		[Conditional("DEBUG_MORE")]
		public static void AssertConditionOrCallerClassNameSearchAllCallstack(bool condition, string name)
		{
			StackTrace stackTrace = new StackTrace(true);
			if (!condition)
			{
				int num = 0;
				while (num < stackTrace.FrameCount && !(stackTrace.GetFrame(num).GetMethod().DeclaringType.Name == name))
				{
					num++;
				}
			}
		}

		// Token: 0x06000970 RID: 2416 RVA: 0x00008E24 File Offset: 0x00007024
		public static void Print(string message, int logLevel = 0, Debug.DebugColor color = Debug.DebugColor.White, ulong debugFilter = 17592186044416UL)
		{
			if (MBDebug.DisableLogging)
			{
				return;
			}
			debugFilter &= 18446744069414584320UL;
			if (debugFilter == 0UL)
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

		// Token: 0x06000971 RID: 2417 RVA: 0x00008E78 File Offset: 0x00007078
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void ConsolePrint(string message, Debug.DebugColor color = Debug.DebugColor.White, ulong debugFilter = 17592186044416UL)
		{
			try
			{
				EngineApplicationInterface.IDebug.WriteLine(0, message, (int)color, debugFilter);
			}
			catch
			{
			}
		}

		// Token: 0x06000972 RID: 2418 RVA: 0x00008EA8 File Offset: 0x000070A8
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void WriteDebugLineOnScreen(string str)
		{
			EngineApplicationInterface.IDebug.WriteDebugLineOnScreen(str);
		}

		// Token: 0x06000973 RID: 2419 RVA: 0x00008EB5 File Offset: 0x000070B5
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugText(float screenX, float screenY, string text, uint color = 4294967295U, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugText(screenX, screenY, text, color, time);
		}

		// Token: 0x06000974 RID: 2420 RVA: 0x00008EC7 File Offset: 0x000070C7
		public static void RenderText(float screenX, float screenY, string text, uint color = 4294967295U, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugText(screenX, screenY, text, color, time);
		}

		// Token: 0x06000975 RID: 2421 RVA: 0x00008ED9 File Offset: 0x000070D9
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugRect(float left, float bottom, float right, float top)
		{
			EngineApplicationInterface.IDebug.RenderDebugRect(left, bottom, right, top);
		}

		// Token: 0x06000976 RID: 2422 RVA: 0x00008EE9 File Offset: 0x000070E9
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color = 4294967295U)
		{
			EngineApplicationInterface.IDebug.RenderDebugRectWithColor(left, bottom, right, top, color);
		}

		// Token: 0x06000977 RID: 2423 RVA: 0x00008EFB File Offset: 0x000070FB
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugFrame(MatrixFrame frame, float lineLength, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugFrame(ref frame, lineLength, time);
		}

		// Token: 0x06000978 RID: 2424 RVA: 0x00008F0B File Offset: 0x0000710B
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugText3D(Vec3 worldPosition, string str, uint color = 4294967295U, int screenPosOffsetX = 0, int screenPosOffsetY = 0, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugText3d(worldPosition, str, color, screenPosOffsetX, screenPosOffsetY, time);
		}

		// Token: 0x06000979 RID: 2425 RVA: 0x00008F1F File Offset: 0x0000711F
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugDirectionArrow(Vec3 position, Vec3 direction, uint color = 4294967295U, bool depthCheck = false)
		{
			EngineApplicationInterface.IDebug.RenderDebugDirectionArrow(position, direction, color, depthCheck);
		}

		// Token: 0x0600097A RID: 2426 RVA: 0x00008F2F File Offset: 0x0000712F
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugLine(Vec3 position, Vec3 direction, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugLine(position, direction, color, depthCheck, time);
		}

		// Token: 0x0600097B RID: 2427 RVA: 0x00008F41 File Offset: 0x00007141
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugSphere(Vec3 position, float radius, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugSphere(position, radius, color, depthCheck, time);
		}

		// Token: 0x0600097C RID: 2428 RVA: 0x00008F53 File Offset: 0x00007153
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugCapsule(Vec3 p0, Vec3 p1, float radius, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugCapsule(p0, p1, radius, color, depthCheck, time);
		}

		// Token: 0x0600097D RID: 2429 RVA: 0x00008F68 File Offset: 0x00007168
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugBoundingBoxOfEntity(GameEntity entity, MatrixFrame frame, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			Vec3 boundingBoxMin = entity.GetBoundingBoxMin();
			Vec3 boundingBoxMax = entity.GetBoundingBoxMax();
			List<Vec3> list = new List<Vec3>();
			list.Add(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z, -1f));
			list.Add(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z, -1f));
			list.Add(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z, -1f));
			list.Add(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z, -1f));
			list.Add(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z, -1f));
			list.Add(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z, -1f));
			list.Add(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z, -1f));
			list.Add(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z, -1f));
			for (int i = 0; i < list.Count / 2; i++)
			{
				Vec3 vec = list[i];
				frame.TransformToParent(vec);
				vec = list[(i + 1) % (list.Count / 2)];
				frame.TransformToParent(vec);
				vec = list[i + list.Count / 2];
				frame.TransformToParent(vec);
				vec = list[(i + 1) % (list.Count / 2) + list.Count / 2];
				frame.TransformToParent(vec);
			}
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x00009124 File Offset: 0x00007324
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugBoundingBox(BoundingBox box, MatrixFrame frame, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			Vec3 min = box.min;
			Vec3 max = box.max;
			List<Vec3> list = new List<Vec3>();
			list.Add(new Vec3(min.x, min.y, min.z, -1f));
			list.Add(new Vec3(max.x, min.y, min.z, -1f));
			list.Add(new Vec3(max.x, max.y, min.z, -1f));
			list.Add(new Vec3(min.x, max.y, min.z, -1f));
			list.Add(new Vec3(min.x, min.y, max.z, -1f));
			list.Add(new Vec3(max.x, min.y, max.z, -1f));
			list.Add(new Vec3(max.x, max.y, max.z, -1f));
			list.Add(new Vec3(min.x, max.y, max.z, -1f));
			for (int i = 0; i < list.Count / 2; i++)
			{
				Vec3 vec = list[i];
				frame.TransformToParent(vec);
				vec = list[(i + 1) % (list.Count / 2)];
				frame.TransformToParent(vec);
				vec = list[i + list.Count / 2];
				frame.TransformToParent(vec);
				vec = list[(i + 1) % (list.Count / 2) + list.Count / 2];
				frame.TransformToParent(vec);
			}
		}

		// Token: 0x0600097F RID: 2431 RVA: 0x000092DF File Offset: 0x000074DF
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void ClearRenderObjects()
		{
			EngineApplicationInterface.IDebug.ClearAllDebugRenderObjects();
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000980 RID: 2432 RVA: 0x000092EB File Offset: 0x000074EB
		// (set) Token: 0x06000981 RID: 2433 RVA: 0x000092F7 File Offset: 0x000074F7
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

		// Token: 0x06000982 RID: 2434 RVA: 0x00009304 File Offset: 0x00007504
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugBoxObject(Vec3 min, Vec3 max, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugBoxObject(min, max, color, depthCheck, time);
		}

		// Token: 0x06000983 RID: 2435 RVA: 0x00009316 File Offset: 0x00007516
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void RenderDebugBoxObject(Vec3 min, Vec3 max, MatrixFrame frame, uint color = 4294967295U, bool depthCheck = false, float time = 0f)
		{
			EngineApplicationInterface.IDebug.RenderDebugBoxObjectWithFrame(min, max, ref frame, color, depthCheck, time);
		}

		// Token: 0x06000984 RID: 2436 RVA: 0x0000932B File Offset: 0x0000752B
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void PostWarningLine(string line)
		{
			EngineApplicationInterface.IDebug.PostWarningLine(line);
		}

		// Token: 0x06000985 RID: 2437 RVA: 0x00009338 File Offset: 0x00007538
		public static bool IsErrorReportModeActive()
		{
			return EngineApplicationInterface.IDebug.IsErrorReportModeActive();
		}

		// Token: 0x06000986 RID: 2438 RVA: 0x00009344 File Offset: 0x00007544
		public static bool IsErrorReportModePauseMission()
		{
			return EngineApplicationInterface.IDebug.IsErrorReportModePauseMission();
		}

		// Token: 0x06000987 RID: 2439 RVA: 0x00009350 File Offset: 0x00007550
		public static void SetErrorReportScene(Scene scene)
		{
			UIntPtr errorReportScene = ((scene == null) ? UIntPtr.Zero : scene.Pointer);
			EngineApplicationInterface.IDebug.SetErrorReportScene(errorReportScene);
		}

		// Token: 0x06000988 RID: 2440 RVA: 0x0000937F File Offset: 0x0000757F
		public static void SetDumpGenerationDisabled(bool value)
		{
			EngineApplicationInterface.IDebug.SetDumpGenerationDisabled(value);
		}

		// Token: 0x06000989 RID: 2441 RVA: 0x0000938C File Offset: 0x0000758C
		public static void EchoCommandWindow(string content)
		{
			EngineApplicationInterface.IDebug.EchoCommandWindow(content);
		}

		// Token: 0x0600098A RID: 2442 RVA: 0x00009399 File Offset: 0x00007599
		[CommandLineFunctionality.CommandLineArgumentFunction("clear", "console")]
		public static string ClearConsole(List<string> strings)
		{
			Console.Clear();
			return "Debug console cleared.";
		}

		// Token: 0x0600098B RID: 2443 RVA: 0x000093A5 File Offset: 0x000075A5
		[CommandLineFunctionality.CommandLineArgumentFunction("echo_command_window", "console")]
		public static string EchoCommandWindow(List<string> strings)
		{
			MBDebug.EchoCommandWindow(strings[0]);
			return "";
		}

		// Token: 0x0600098C RID: 2444 RVA: 0x000093B8 File Offset: 0x000075B8
		[CommandLineFunctionality.CommandLineArgumentFunction("echo_command_window_test", "console")]
		public static string EchoCommandWindowTest(List<string> strings)
		{
			MBDebug.EchoCommandWindowTestAux();
			return "";
		}

		// Token: 0x0600098D RID: 2445 RVA: 0x000093C4 File Offset: 0x000075C4
		private static async void EchoCommandWindowTestAux()
		{
			MBDebug.EchoCommandWindow("5...");
			await Task.Delay(1000);
			MBDebug.EchoCommandWindow("4...");
			await Task.Delay(1000);
			MBDebug.EchoCommandWindow("3...");
			await Task.Delay(1000);
			MBDebug.EchoCommandWindow("2...");
			await Task.Delay(1000);
			MBDebug.EchoCommandWindow("1...");
			await Task.Delay(1000);
			MBDebug.EchoCommandWindow("Tada!");
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x0600098E RID: 2446 RVA: 0x000093F5 File Offset: 0x000075F5
		// (set) Token: 0x0600098F RID: 2447 RVA: 0x00009401 File Offset: 0x00007601
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

		// Token: 0x06000990 RID: 2448 RVA: 0x0000940E File Offset: 0x0000760E
		public static bool IsTestMode()
		{
			return EngineApplicationInterface.IDebug.IsTestMode();
		}

		// Token: 0x04000109 RID: 265
		public static bool DisableAllUI;

		// Token: 0x0400010A RID: 266
		public static bool TestModeEnabled;

		// Token: 0x0400010B RID: 267
		public static bool ShouldAssertThrowException;

		// Token: 0x0400010C RID: 268
		public static bool IsDisplayingHighLevelAI;

		// Token: 0x0400010D RID: 269
		public static bool DisableLogging;

		// Token: 0x0400010E RID: 270
		private static readonly Dictionary<string, int> ProcessedFrameList = new Dictionary<string, int>();

		// Token: 0x020000C7 RID: 199
		[Flags]
		public enum MessageBoxTypeFlag
		{
			// Token: 0x04000419 RID: 1049
			Ok = 1,
			// Token: 0x0400041A RID: 1050
			Warning = 2,
			// Token: 0x0400041B RID: 1051
			Error = 4,
			// Token: 0x0400041C RID: 1052
			OkCancel = 8,
			// Token: 0x0400041D RID: 1053
			RetryCancel = 16,
			// Token: 0x0400041E RID: 1054
			YesNo = 32,
			// Token: 0x0400041F RID: 1055
			YesNoCancel = 64,
			// Token: 0x04000420 RID: 1056
			Information = 128,
			// Token: 0x04000421 RID: 1057
			Exclamation = 256,
			// Token: 0x04000422 RID: 1058
			Question = 512,
			// Token: 0x04000423 RID: 1059
			AssertFailed = 1024
		}
	}
}
