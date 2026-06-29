using System;
using System.IO;
using AnimusForge.SiegeAftermathIntervention;

namespace AnimusForge;

internal static class GcczDiagnosticLog
{
	private static readonly object LogLock = new object();

	private static string _logPath;

	internal static void Log(string source, string message)
	{
		try
		{
			string path = GetLogPath();
			string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
				+ " [" + NormalizeSource(source) + "] "
				+ (message ?? string.Empty)
				+ Environment.NewLine;
			lock (LogLock)
			{
				File.AppendAllText(path, line);
			}
		}
		catch
		{
			try
			{
				Logger.Log("GCCZ", "[" + NormalizeSource(source) + "] " + (message ?? string.Empty));
			}
			catch
			{
			}
		}
	}

	private static string GetLogPath()
	{
		if (!string.IsNullOrWhiteSpace(_logPath))
		{
			return _logPath;
		}

		string path = AnimusForgeModulePaths.GetLogFilePath(SiegeNpcResponseLimitProfile.DiagnosticLogFileName);
		string directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}
		_logPath = path;
		return _logPath;
	}

	private static string NormalizeSource(string source)
	{
		return string.IsNullOrWhiteSpace(source) ? "GCCZ" : source.Trim();
	}
}
