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

	internal static string GetDiagnosticLogPath()
	{
		return GetLogPath();
	}

	internal static string GetDiagnosticLogDirectory()
	{
		string path = GetLogPath();
		string directory = Path.GetDirectoryName(path);
		return string.IsNullOrWhiteSpace(directory) ? AnimusForgeModulePaths.GetLogsDirectory() : directory;
	}

	internal static string ExportLogToDesktop()
	{
		string sourcePath = GetLogPath();
		EnsureLogFileExists(sourcePath);

		string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		if (string.IsNullOrWhiteSpace(desktopDirectory))
		{
			desktopDirectory = GetDiagnosticLogDirectory();
		}
		Directory.CreateDirectory(desktopDirectory);

		string exportPath = Path.Combine(desktopDirectory, "GCCZ_Debug_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
		lock (LogLock)
		{
			File.Copy(sourcePath, exportPath, overwrite: true);
		}
		Log("Export", "exported source=" + sourcePath + " target=" + exportPath);
		return exportPath;
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

	private static void EnsureLogFileExists(string path)
	{
		if (File.Exists(path))
		{
			return;
		}
		string directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}
		File.WriteAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [GCCZ] GCCZ_Debug.log created; no GCCZ diagnostic events have been written yet." + Environment.NewLine);
	}

	private static string NormalizeSource(string source)
	{
		return string.IsNullOrWhiteSpace(source) ? "GCCZ" : source.Trim();
	}
}
