using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MBDebugManager : IDebugManager
{
	void IDebugManager.SetCrashReportCustomString(string customString)
	{
		Utilities.SetCrashReportCustomString(customString);
	}

	void IDebugManager.SetCrashReportCustomStack(string customStack)
	{
		Utilities.SetCrashReportCustomStack(customStack);
	}

	void IDebugManager.ShowWarning(string message)
	{
		MBDebug.ShowWarning(message);
	}

	void IDebugManager.ShowError(string message)
	{
		MBDebug.ShowError(message);
	}

	void IDebugManager.ShowMessageBox(string lpText, string lpCaption, uint uType)
	{
		MBDebug.ShowMessageBox(lpText, lpCaption, uType);
	}

	void IDebugManager.Assert(bool condition, string message, string callerFile, string callerMethod, int callerLine)
	{
	}

	void IDebugManager.SilentAssert(bool condition, string message, bool getDump, string callerFile, string callerMethod, int callerLine)
	{
		MBDebug.SilentAssert(condition, message, getDump, callerFile, callerMethod, callerLine);
	}

	void IDebugManager.Print(string message, int logLevel, Debug.DebugColor color, ulong debugFilter)
	{
		MBDebug.Print(message, logLevel, color, debugFilter);
	}

	void IDebugManager.PrintError(string error, string stackTrace, ulong debugFilter)
	{
		MBDebug.Print(error, 0, Debug.DebugColor.White, debugFilter);
	}

	void IDebugManager.PrintWarning(string warning, ulong debugFilter)
	{
		MBDebug.Print(warning, 0, Debug.DebugColor.White, debugFilter);
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
		return MBDebug.DebugVector;
	}

	void IDebugManager.SetDebugVector(Vec3 value)
	{
		MBDebug.DebugVector = value;
	}

	void IDebugManager.SetTestModeEnabled(bool testModeEnabled)
	{
		MBDebug.TestModeEnabled = testModeEnabled;
	}

	void IDebugManager.AbortGame()
	{
		MBDebug.AbortGame();
	}

	void IDebugManager.DoDelayedexit(int returnCode)
	{
		Utilities.DoDelayedexit(returnCode);
	}

	void IDebugManager.ReportMemoryBookmark(string message)
	{
	}
}
