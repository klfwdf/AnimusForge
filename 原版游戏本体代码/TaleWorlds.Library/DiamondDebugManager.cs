using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200002B RID: 43
	public class DiamondDebugManager : IDebugManager
	{
		// Token: 0x06000142 RID: 322 RVA: 0x00005C95 File Offset: 0x00003E95
		public DiamondDebugManager(ParameterContainer parameters)
		{
			this._parameters = parameters;
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00005CA4 File Offset: 0x00003EA4
		public DiamondDebugManager()
		{
			this._parameters = null;
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00005CB3 File Offset: 0x00003EB3
		void IDebugManager.SetCrashReportCustomString(string customString)
		{
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00005CB5 File Offset: 0x00003EB5
		void IDebugManager.SetCrashReportCustomStack(string customStack)
		{
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00005CB7 File Offset: 0x00003EB7
		void IDebugManager.ShowMessageBox(string lpText, string lpCaption, uint uType)
		{
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00005CB9 File Offset: 0x00003EB9
		void IDebugManager.ShowError(string message)
		{
			this.PrintMessage(message, DiamondDebugManager.DiamondDebugCategory.Error);
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00005CC3 File Offset: 0x00003EC3
		void IDebugManager.ShowWarning(string message)
		{
			this.PrintMessage(message, DiamondDebugManager.DiamondDebugCategory.Warning);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00005CCD File Offset: 0x00003ECD
		void IDebugManager.Assert(bool condition, string message, string callerFile, string callerMethod, int callerLine)
		{
			if (!condition)
			{
				throw new Exception(string.Format("Assertion failed: {0} in {1}, line:{2}", message, callerFile, callerLine));
			}
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00005CEB File Offset: 0x00003EEB
		void IDebugManager.SilentAssert(bool condition, string message, bool getDump, string callerFile, string callerMethod, int callerLine)
		{
			if (!condition)
			{
				this.PrintMessage(string.Format("Assertion failed: {0} in {1}, line:{2}", message, callerMethod, callerLine), DiamondDebugManager.DiamondDebugCategory.Warning);
			}
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00005D0B File Offset: 0x00003F0B
		void IDebugManager.Print(string message, int logLevel, Debug.DebugColor color, ulong debugFilter)
		{
			this.PrintMessage(message, DiamondDebugManager.DiamondDebugCategory.General);
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00005D15 File Offset: 0x00003F15
		void IDebugManager.PrintError(string error, string stackTrace, ulong debugFilter)
		{
			this.PrintMessage(error + stackTrace, DiamondDebugManager.DiamondDebugCategory.Error);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00005D25 File Offset: 0x00003F25
		void IDebugManager.PrintWarning(string warning, ulong debugFilter)
		{
			this.PrintMessage(warning, DiamondDebugManager.DiamondDebugCategory.Warning);
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00005D2F File Offset: 0x00003F2F
		void IDebugManager.DisplayDebugMessage(string message)
		{
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00005D31 File Offset: 0x00003F31
		void IDebugManager.WatchVariable(string name, object value)
		{
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00005D33 File Offset: 0x00003F33
		void IDebugManager.WriteDebugLineOnScreen(string message)
		{
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00005D35 File Offset: 0x00003F35
		void IDebugManager.RenderDebugLine(Vec3 position, Vec3 direction, uint color, bool depthCheck, float time)
		{
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00005D37 File Offset: 0x00003F37
		void IDebugManager.RenderDebugSphere(Vec3 position, float radius, uint color, bool depthCheck, float time)
		{
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00005D39 File Offset: 0x00003F39
		void IDebugManager.RenderDebugFrame(MatrixFrame frame, float lineLength, float time)
		{
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00005D3B File Offset: 0x00003F3B
		void IDebugManager.RenderDebugText(float screenX, float screenY, string text, uint color, float time)
		{
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00005D3D File Offset: 0x00003F3D
		void IDebugManager.RenderDebugText3D(Vec3 position, string text, uint color, int screenPosOffsetX, int screenPosOffsetY, float time)
		{
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00005D3F File Offset: 0x00003F3F
		void IDebugManager.RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color)
		{
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00005D41 File Offset: 0x00003F41
		Vec3 IDebugManager.GetDebugVector()
		{
			return Vec3.Zero;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00005D48 File Offset: 0x00003F48
		void IDebugManager.SetDebugVector(Vec3 value)
		{
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00005D4A File Offset: 0x00003F4A
		void IDebugManager.SetTestModeEnabled(bool testModeEnabled)
		{
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00005D4C File Offset: 0x00003F4C
		void IDebugManager.AbortGame()
		{
			Environment.Exit(-5);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00005D55 File Offset: 0x00003F55
		void IDebugManager.DoDelayedexit(int returnCode)
		{
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00005D58 File Offset: 0x00003F58
		public int GetLogLevel()
		{
			int result;
			if (this._parameters != null && this._parameters.TryGetParameterAsInt("LogLevel", out result))
			{
				return result;
			}
			return 1;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x00005D84 File Offset: 0x00003F84
		protected void PrintMessage(string message, DiamondDebugManager.DiamondDebugCategory debugCategory)
		{
			if (this.GetLogLevel() <= (int)debugCategory)
			{
				Console.Out.Flush();
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = DiamondDebugManager._colors[debugCategory];
				Console.Write(message);
				Console.ResetColor();
				Console.WriteLine();
				Console.Out.Flush();
			}
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00005DD4 File Offset: 0x00003FD4
		void IDebugManager.ReportMemoryBookmark(string message)
		{
		}

		// Token: 0x040000A1 RID: 161
		private static Dictionary<DiamondDebugManager.DiamondDebugCategory, ConsoleColor> _colors = new Dictionary<DiamondDebugManager.DiamondDebugCategory, ConsoleColor>
		{
			{
				DiamondDebugManager.DiamondDebugCategory.General,
				ConsoleColor.Green
			},
			{
				DiamondDebugManager.DiamondDebugCategory.Warning,
				ConsoleColor.Yellow
			},
			{
				DiamondDebugManager.DiamondDebugCategory.Error,
				ConsoleColor.Red
			}
		};

		// Token: 0x040000A2 RID: 162
		private ParameterContainer _parameters;

		// Token: 0x020000D0 RID: 208
		public enum DiamondDebugCategory
		{
			// Token: 0x0400029B RID: 667
			General,
			// Token: 0x0400029C RID: 668
			Warning,
			// Token: 0x0400029D RID: 669
			Error
		}
	}
}
