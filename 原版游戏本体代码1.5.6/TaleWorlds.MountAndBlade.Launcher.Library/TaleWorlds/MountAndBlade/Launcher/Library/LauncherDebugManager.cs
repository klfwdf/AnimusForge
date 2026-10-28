using System;
using System.IO;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherDebugManager : IDebugManager
{
	private readonly PlatformFilePath _logFilePath;

	public LauncherDebugManager()
	{
		PlatformDirectoryPath platformDirectoryPath = new PlatformDirectoryPath(PlatformFileType.Application, "logs");
		TryDeletePreviousLogs(platformDirectoryPath);
		_logFilePath = new PlatformFilePath(platformDirectoryPath, "launcher_log_" + new Random().Next(10000, 99999) + ".txt");
	}

	private void TryDeletePreviousLogs(PlatformDirectoryPath directoryPath)
	{
		PlatformFilePath[] array = new PlatformFilePath[0];
		try
		{
			array = FileHelper.GetFiles(directoryPath, "launcher_log_*.txt", SearchOption.AllDirectories);
		}
		catch (Exception)
		{
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				FileHelper.DeleteFile(array[i]);
			}
			catch (Exception)
			{
			}
		}
	}

	public void OnFinalize()
	{
	}

	private void AppendLineToLog(string message, bool forceSave = true)
	{
		FileHelper.AppendLineToFileString(_logFilePath, message);
	}

	void IDebugManager.SetCrashReportCustomString(string customString)
	{
	}

	void IDebugManager.SetCrashReportCustomStack(string customStack)
	{
	}

	void IDebugManager.ShowError(string message)
	{
	}

	void IDebugManager.ShowWarning(string message)
	{
		AppendLineToLog(message);
	}

	void IDebugManager.ShowMessageBox(string lpText, string lpCaption, uint uType)
	{
		User32.MessageBox(IntPtr.Zero, lpText, lpCaption, 16u);
		AppendLineToLog(lpCaption);
		AppendLineToLog(lpText);
	}

	void IDebugManager.Assert(bool condition, string message, string callerFile, string callerMethod, int callerLine)
	{
		if (!condition)
		{
			AppendLineToLog("ASSERT!\n" + message);
		}
	}

	void IDebugManager.SilentAssert(bool condition, string message, bool getDump, string callerFile, string callerMethod, int callerLine)
	{
		if (!condition)
		{
			AppendLineToLog("ASSERT!\n" + message);
		}
	}

	void IDebugManager.Print(string message, int logLevel, Debug.DebugColor color, ulong debugFilter)
	{
		AppendLineToLog(message);
	}

	void IDebugManager.PrintError(string error, string stackTrace, ulong debugFilter)
	{
		AppendLineToLog("ERROR!\n" + error);
	}

	void IDebugManager.PrintWarning(string warning, ulong debugFilter)
	{
		AppendLineToLog("warning!\n" + warning);
	}

	void IDebugManager.DisplayDebugMessage(string message)
	{
		AppendLineToLog(message);
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
	}

	void IDebugManager.DoDelayedexit(int returnCode)
	{
	}

	void IDebugManager.ReportMemoryBookmark(string message)
	{
	}
}
