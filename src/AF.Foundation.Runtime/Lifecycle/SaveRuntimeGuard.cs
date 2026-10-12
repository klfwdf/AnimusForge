using System;
using System.IO;
using System.Text;
using System.Threading;

namespace AnimusForge;

internal static class SaveRuntimeGuard
{
    private static readonly Lazy<string> StaleRequestCompatibilityText = new Lazy<string>(() =>
    {
        using (Stream stream = typeof(SaveRuntimeGuard).Assembly.GetManifestResourceStream(
            "AnimusForge.Foundation.StaleRequestError.txt"))
        using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            return reader.ReadToEnd();
    }, true);

	private static long _generation = 1;

	public static long CurrentGeneration => Interlocked.Read(ref _generation);

	public static long CaptureGeneration()
	{
		return CurrentGeneration;
	}

	public static bool IsCurrentGeneration(long generation)
	{
		return generation > 0 && generation == CurrentGeneration;
	}

	public static long AdvanceGeneration(string reason)
	{
		long generation = Interlocked.Increment(ref _generation);
		try
		{
			Logger.Log("SaveRuntimeGuard", "generation_advanced reason=" + NormalizeReason(reason) + " generation=" + generation);
		}
		catch
		{
		}
		return generation;
	}

	public static bool IsStale(long generation, string source = null)
	{
		if (IsCurrentGeneration(generation))
		{
			return false;
		}
		try
		{
			Logger.Log("SaveRuntimeGuard", "stale_runtime_result source=" + NormalizeReason(source) + " captured=" + generation + " current=" + CurrentGeneration);
		}
		catch
		{
		}
		return true;
	}

	public static string BuildStaleRequestErrorText()
	{
		return StaleRequestCompatibilityText.Value;
	}

	private static string NormalizeReason(string reason)
	{
		string text = (reason ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "unknown";
		}
		return text.Replace(" ", "_").Replace("\r", "_").Replace("\n", "_");
	}
}
