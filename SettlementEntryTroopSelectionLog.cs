namespace AnimusForge;

internal static class SettlementEntryTroopSelectionLog
{
	private static readonly FeatureDiagnosticLogFile LogFile = new FeatureDiagnosticLogFile(
		"SETS.log",
		"SETS",
		"SETS",
		DuelSettings.IsSetsDiagnosticLogEnabled,
		DuelSettings.IsSetsVerboseDiagnosticLogEnabled,
		DuelSettings.GetSetsDiagnosticLogMaxSizeMegabytes);

	public static void Log(string message)
	{
		Write(message, verbose: false);
	}

	public static void LogVerbose(string message)
	{
		Write(message, verbose: true);
	}

	internal static string GetDiagnosticLogPath()
	{
		return LogFile.GetLogPath();
	}

	internal static string GetDiagnosticLogDirectory()
	{
		return LogFile.GetLogDirectory();
	}

	internal static string ExportLogToDesktop()
	{
		return LogFile.ExportToDesktop();
	}

	internal static void ClearLog()
	{
		LogFile.Clear();
	}

	private static void Write(string message, bool verbose)
	{
		try
		{
			LogFile.Write("SETS", message, verbose);
		}
		catch
		{
		}
	}
}
