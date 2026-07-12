using System;
using System.Globalization;
using System.Text;

namespace AnimusForge;

internal static class PolicySystemLog
{
	private const string FileName = "PolicySystem.txt";
	private const int MaxFieldChars = 200000;

	internal static void Write(string category, string stage, string message, string detail = null)
	{
		try
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
			builder.Append(" [").Append(Clean(category, "Policy")).Append("]");
			builder.Append(" [").Append(Clean(stage, "log")).Append("] ");
			builder.AppendLine(message ?? "");
			if (!string.IsNullOrWhiteSpace(detail))
			{
				builder.AppendLine("--- detail begin ---");
				builder.AppendLine(Clip(detail));
				builder.AppendLine("--- detail end ---");
			}
			Logger.LogToFile(FileName, builder.ToString());
		}
		catch
		{
		}
	}

	internal static string Clip(string text)
	{
		text ??= "";
		return text.Length <= MaxFieldChars
			? text
			: text.Substring(0, MaxFieldChars) + "\n...[truncated " + (text.Length - MaxFieldChars).ToString(CultureInfo.InvariantCulture) + " chars]";
	}

	private static string Clean(string value, string fallback)
	{
		string clean = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return string.IsNullOrWhiteSpace(clean) ? fallback : clean;
	}
}
