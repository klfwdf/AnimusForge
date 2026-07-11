using AnimusForge.SiegeAftermathIntervention;

namespace AnimusForge;

internal static class GcczDiagnosticLog
{
	private static readonly FeatureDiagnosticLogFile LogFile = new FeatureDiagnosticLogFile(
		SiegeNpcResponseLimitProfile.DiagnosticLogFileName,
		"GCCZ_Debug",
		"GCCZ",
		DuelSettings.IsGcczDiagnosticLogEnabled,
		DuelSettings.IsGcczVerboseDiagnosticLogEnabled,
		DuelSettings.GetGcczDiagnosticLogMaxSizeMegabytes);

	internal static void Log(string source, string message)
	{
		Write(source, message, verbose: false);
	}

	internal static void LogVerbose(string source, string message)
	{
		Write(source, message, verbose: true);
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

	private static void Write(string source, string message, bool verbose)
	{
		try
		{
			LogFile.Write(source, message, verbose);
		}
		catch
		{
		}
	}
}
