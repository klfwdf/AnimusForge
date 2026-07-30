using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AnimusForge;

internal static class PolicySystemLog
{
	private const string FileName = "PolicySystem.txt";
	private const int MaxMessageChars = 800;
	private const int MaxFailureDetailChars = 4096;

	private static readonly HashSet<string> LifecycleStages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"generation-start",
		"generation-complete",
		"generation-commit-complete",
		"submit",
		"submitted",
		"agenda-submitted",
		"complete-agenda-submitted",
		"adopted",
		"adoption-rejected",
		"agenda-submit-rejected",
		"abolished",
		"expiry-abolition-submitted",
		"expiry-abolition-rejected",
		"active-created",
		"active-effects-created",
		"effect-ended",
		"deduct-cost",
		"player-steward-xp-awarded",
		"material-recorded"
	};

	internal static void Write(string category, string stage, string message, string detail = null)
	{
		string normalizedStage = Clean(stage, "log");
		bool lifecycle = LifecycleStages.Contains(normalizedStage);
		bool failure = !lifecycle && (ContainsFailureMarker(normalizedStage) || ContainsFailureMarker(message));
		if (!lifecycle && !failure)
		{
			return;
		}
		WriteCore(category, normalizedStage, message, failure ? detail : null);
	}

	internal static void WriteRuntime(string category, string message)
	{
		string normalizedMessage = Clean(message, "");
		string stage = FirstToken(normalizedMessage);
		Write(category, stage, normalizedMessage);
	}

	internal static void Failure(string category, string stage, string message, string detail = null)
	{
		WriteCore(category, Clean(stage, "failure"), message, detail);
	}

	private static void WriteCore(string category, string stage, string message, string detail)
	{
		try
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
			builder.Append(" [").Append(Clean(category, "Policy")).Append("]");
			builder.Append(" [").Append(Clean(stage, "log")).Append("] ");
			builder.AppendLine(ClipOneLine(message, MaxMessageChars));
			if (!string.IsNullOrWhiteSpace(detail))
			{
				builder.AppendLine("--- detail begin ---");
				builder.AppendLine(Clip(detail, MaxFailureDetailChars));
				builder.AppendLine("--- detail end ---");
			}
			Logger.LogToFile(FileName, builder.ToString());
		}
		catch
		{
		}
	}

	private static bool ContainsFailureMarker(string value)
	{
		string text = value ?? "";
		return text.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("blocked", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("失败", StringComparison.Ordinal) >= 0
			|| text.IndexOf("异常", StringComparison.Ordinal) >= 0
			|| text.IndexOf("错误", StringComparison.Ordinal) >= 0;
	}

	private static string FirstToken(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return "log";
		}
		int separator = message.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
		return separator > 0 ? message.Substring(0, separator) : message;
	}

	private static string ClipOneLine(string text, int maxChars)
	{
		return Clip(Clean(text, ""), maxChars);
	}

	private static string Clip(string text, int maxChars)
	{
		text ??= "";
		return text.Length <= maxChars ? text : text.Substring(0, maxChars);
	}

	private static string Clean(string value, string fallback)
	{
		string clean = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return string.IsNullOrWhiteSpace(clean) ? fallback : clean;
	}
}
