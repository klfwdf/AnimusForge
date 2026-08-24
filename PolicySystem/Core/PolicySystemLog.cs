using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class PolicySystemLog
{
	private const string FileName = "PolicySystem.txt";
	private const string TransactionFileName = "PolicySystemTransactions.jsonl";
	private const int MaxMessageChars = 800;
	private const int MaxFailureDetailChars = 4096;
	private const int MaxDiagnosticDetailChars = 1048576;

	private static readonly HashSet<string> LifecycleStages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"generation-start",
		"generation-complete",
		"generation-commit-complete",
		"historyRetrieved",
		"mainCompleted",
		"modulesRecalled",
		"detailsInjected",
		"targetDirectoryBuilt",
		"effectPlanParsed",
		"compiled",
		"effect-module-selected",
		"effect-module-outside-recall",
		"target-semantic-index-built",
		"target-semantic-shadow",
		"target-semantic-selected",
		"target-selector-recalled",
		"target-selector-selected",
		"target-selector-resolved",
		"policy-effect-overlap-coalesced",
		"runtime-index-duplicate-contribution",
		"llm-stage-usage",
		"auto-draft-start",
		"auto-draft-complete",
		"policy-effect-duration-normalized",
		"policy-effect-legacy-normalized",
		"submit",
		"submitted",
		"published",
		"agenda-submitted",
		"complete-agenda-submitted",
		"adopted",
		"adoption-rejected",
		"agenda-submit-rejected",
		"pre-cleanup-policy-restore-complete",
		"abolished",
		"expiry-abolition-submitted",
		"expiry-abolition-rejected",
		"active-created",
		"active-effects-created",
		"effect-ended",
		"deduct-cost",
		"player-steward-xp-awarded",
		"material-recorded",
		"module-lifecycle",
		"prepared",
		"bundlePersisted",
		"costCommitted",
		"effectsCommitted",
		"externalCommitted",
		"active",
		"suspended",
		"compensationPending",
		"compensationCompleted",
		"ended",
		"active-bundle-created",
		"daily-modules-complete",
		"runtime-target-structure-refresh"
	};

	// Seed from UTC ticks so an append-only JSONL file does not restart at sequence 1
	// after a campaign/game process reload. Interlocked preserves in-process ordering.
	private static long _transactionSequence = DateTime.UtcNow.Ticks;

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

	// Low-frequency structural checkpoint only. Never call this from model/getter hot paths,
	// and pass compact state tokens rather than prompts, credentials, or user-authored text.
	internal static void WriteModuleLifecycle(
		string category,
		string moduleId,
		string routing = null,
		string compile = null,
		string migration = null,
		string index = null,
		string execution = null,
		string publication = null)
	{
		StringBuilder builder = new StringBuilder(192);
		AppendModuleLifecycleField(builder, "moduleId", moduleId, "unknown");
		AppendModuleLifecycleField(builder, "routing", routing, "-");
		AppendModuleLifecycleField(builder, "compile", compile, "-");
		AppendModuleLifecycleField(builder, "migration", migration, "-");
		AppendModuleLifecycleField(builder, "index", index, "-");
		AppendModuleLifecycleField(builder, "execution", execution, "-");
		AppendModuleLifecycleField(builder, "publication", publication, "-");
		Write(category, "module-lifecycle", builder.ToString());
	}

	internal static void Failure(string category, string stage, string message, string detail = null)
	{
		WriteCore(category, Clean(stage, "failure"), message, detail);
	}

	internal static void Transaction(
		string transactionId,
		string recordId,
		string effectId,
		string mechanismId,
		string stage,
		string result,
		string errorKind = null,
		string targetHash = null,
		int targetCount = 0,
		string costReceipt = null,
		string executionReceipt = null,
		string stateBefore = null,
		string stateAfter = null)
	{
		try
		{
			JObject value = new JObject
			{
				["utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
				["sequence"] = Interlocked.Increment(ref _transactionSequence),
				["transactionId"] = ClipOneLine(transactionId, 160),
				["recordId"] = ClipOneLine(recordId, 160),
				["effectId"] = ClipOneLine(effectId, 160),
				["mechanismId"] = ClipOneLine(mechanismId, 96),
				["stage"] = ClipOneLine(stage, 64),
				["result"] = ClipOneLine(result, 64),
				["errorKind"] = ClipOneLine(errorKind, 96),
				["targetHash"] = ClipOneLine(targetHash, 96),
				["targetCount"] = Math.Max(0, targetCount),
				["costReceipt"] = ClipOneLine(costReceipt, 256),
				["executionReceipt"] = ClipOneLine(executionReceipt, 256),
				["stateBefore"] = ClipOneLine(stateBefore, 96),
				["stateAfter"] = ClipOneLine(stateAfter, 96)
			};
			Logger.LogToFile(TransactionFileName, value.ToString(Formatting.None) + Environment.NewLine);
		}
		catch
		{
		}
	}

	// Failure-only structured snapshots. These are intentionally allowed to be much
	// larger than ordinary exception details so module, payload, target-set and live
	// object identity data are not silently clipped during policy diagnosis.
	internal static void DiagnosticFailure(string category, string stage, string message, string detail)
	{
		WriteCore(category, Clean(stage, "diagnostic-failure"), message, detail, MaxDiagnosticDetailChars);
	}

	private static void WriteCore(
		string category,
		string stage,
		string message,
		string detail,
		int maxDetailChars = MaxFailureDetailChars)
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
				string clippedDetail = Clip(detail, maxDetailChars);
				builder.AppendLine(clippedDetail);
				if (clippedDetail.Length < detail.Length)
				{
					builder.AppendLine("--- detail truncated originalChars="
						+ detail.Length.ToString(CultureInfo.InvariantCulture)
						+ " retainedChars="
						+ clippedDetail.Length.ToString(CultureInfo.InvariantCulture)
						+ " ---");
				}
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

	private static void AppendModuleLifecycleField(StringBuilder builder, string name, string value, string fallback)
	{
		if (builder.Length > 0)
		{
			builder.Append(' ');
		}
		string normalizedValue = ClipOneLine(value, 96).Replace(' ', '_').Replace('\t', '_');
		builder.Append(name).Append('=').Append(string.IsNullOrWhiteSpace(normalizedValue) ? fallback : normalizedValue);
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
