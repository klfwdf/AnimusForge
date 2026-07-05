using System;
using System.IO;

namespace AnimusForge;

internal static class SettlementEntryTroopSelectionLog
{
	private const string FileName = "SETS.log";
	private static string _logPath;

	public static void Log(string message)
	{
		try
		{
			string path = GetLogPath();
			File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + (message ?? "") + Environment.NewLine);
		}
		catch
		{
		}
	}

	private static string GetLogPath()
	{
		if (!string.IsNullOrWhiteSpace(_logPath))
		{
			return _logPath;
		}
		string dir = AnimusForgeModulePaths.GetLogsDirectory();
		Directory.CreateDirectory(dir);
		_logPath = Path.Combine(dir, FileName);
		return _logPath;
	}
}
