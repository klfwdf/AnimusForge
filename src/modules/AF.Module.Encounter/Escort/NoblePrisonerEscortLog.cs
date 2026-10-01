namespace AnimusForge;

internal static class NoblePrisonerEscortLog
{
	private static readonly FeatureDiagnosticLogFile LogFile = new FeatureDiagnosticLogFile(
		"NoblePrisonerEscort.log",
		"NoblePrisonerEscort",
		"NoblePrisonerEscort",
		DuelSettings.IsSetsDiagnosticLogEnabled,
		DuelSettings.IsSetsVerboseDiagnosticLogEnabled,
		DuelSettings.GetSetsDiagnosticLogMaxSizeMegabytes);

	internal static void Log(string message)
	{
		Write(message, verbose: false);
	}

	internal static void LogVerbose(string message)
	{
		Write(message, verbose: true);
	}

	internal static string GetDiagnosticLogPath()
	{
		return LogFile.GetLogPath();
	}

	internal static string ExportLogToDesktop()
	{
		return LogFile.ExportToDesktop();
	}

	internal static string GetDiagnosticLogDirectory()
	{
		return LogFile.GetLogDirectory();
	}

	internal static void ClearLog()
	{
		LogFile.Clear();
	}

	private static void Write(string message, bool verbose)
	{
		try
		{
			LogFile.Write("NoblePrisonerEscort", message, verbose);
		}
		catch
		{
		}
	}
}
