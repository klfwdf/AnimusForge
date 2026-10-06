using System;
using System.Collections.Generic;

namespace TaleWorlds.Library;

public class DiamondDebugManager : IDebugManager
{
	public enum DiamondDebugCategory
	{
		General,
		Warning,
		Error
	}

	private static Dictionary<DiamondDebugCategory, ConsoleColor> _colors = new Dictionary<DiamondDebugCategory, ConsoleColor>
	{
		{
			DiamondDebugCategory.General,
			ConsoleColor.Green
		},
		{
			DiamondDebugCategory.Warning,
			ConsoleColor.Yellow
		},
		{
			DiamondDebugCategory.Error,
			ConsoleColor.Red
		}
	};

	private ParameterContainer _parameters;

	public DiamondDebugManager(ParameterContainer parameters)
	{
		_parameters = parameters;
	}

	public DiamondDebugManager()
	{
		_parameters = null;
	}

	void IDebugManager.SetCrashReportCustomString(string customString)
	{
	}

	void IDebugManager.SetCrashReportCustomStack(string customStack)
	{
	}

	void IDebugManager.ShowMessageBox(string lpText, string lpCaption, uint uType)
	{
	}

	void IDebugManager.ShowError(string message)
	{
		PrintMessage(message, DiamondDebugCategory.Error);
	}

	void IDebugManager.ShowWarning(string message)
	{
		PrintMessage(message, DiamondDebugCategory.Warning);
	}

	void IDebugManager.Assert(bool condition, string message, string callerFile, string callerMethod, int callerLine)
	{
		if (!condition)
		{
			throw new Exception($"Assertion failed: {message} in {callerFile}, line:{callerLine}");
		}
	}

	void IDebugManager.SilentAssert(bool condition, string message, bool getDump, string callerFile, string callerMethod, int callerLine)
	{
		if (!condition)
		{
			PrintMessage($"Assertion failed: {message} in {callerMethod}, line:{callerLine}", DiamondDebugCategory.Warning);
		}
	}

	void IDebugManager.Print(string message, int logLevel, Debug.DebugColor color, ulong debugFilter)
	{
		PrintMessage(message, DiamondDebugCategory.General);
	}

	void IDebugManager.PrintError(string error, string stackTrace, ulong debugFilter)
	{
		PrintMessage(error + stackTrace, DiamondDebugCategory.Error);
	}

	void IDebugManager.PrintWarning(string warning, ulong debugFilter)
	{
		PrintMessage(warning, DiamondDebugCategory.Warning);
	}

	void IDebugManager.DisplayDebugMessage(string message)
	{
	}

	void IDebugManager.WatchVariable(string name, object value)
	{
	}

	void IDebugManager.WriteDebugLineOnScreen(string message)
	{
	}

	void IDebugManager.RenderDebugLine(Vec3 position, Vec3 direction, uint color, bool depthCheck, float time)
	{
	}

	void IDebugManager.RenderDebugSphere(Vec3 position, float radius, uint color, bool depthCheck, float time)
	{
	}

	void IDebugManager.RenderDebugFrame(MatrixFrame frame, float lineLength, float time)
	{
	}

	void IDebugManager.RenderDebugText(float screenX, float screenY, string text, uint color, float time)
	{
	}

	void IDebugManager.RenderDebugText3D(Vec3 position, string text, uint color, int screenPosOffsetX, int screenPosOffsetY, float time)
	{
	}

	void IDebugManager.RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color)
	{
	}

	Vec3 IDebugManager.GetDebugVector()
	{
		return Vec3.Zero;
	}

	void IDebugManager.SetDebugVector(Vec3 value)
	{
	}

	void IDebugManager.SetTestModeEnabled(bool testModeEnabled)
	{
	}

	void IDebugManager.AbortGame()
	{
		Environment.Exit(-5);
	}

	void IDebugManager.DoDelayedexit(int returnCode)
	{
	}

	public int GetLogLevel()
	{
		if (_parameters != null && _parameters.TryGetParameterAsInt("LogLevel", out var outValue))
		{
			return outValue;
		}
		return 1;
	}

	protected void PrintMessage(string message, DiamondDebugCategory debugCategory)
	{
		if (GetLogLevel() <= (int)debugCategory)
		{
			Console.Out.Flush();
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = _colors[debugCategory];
			Console.Write(message);
			Console.ResetColor();
			Console.WriteLine();
			Console.Out.Flush();
		}
	}

	void IDebugManager.ReportMemoryBookmark(string message)
	{
	}
}
