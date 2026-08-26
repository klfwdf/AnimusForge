using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class PolicyLogContext
{
	internal string Version;
	internal string RequestId;
	internal string GenerationId;
	internal string BatchId;
	internal string JobId;
	internal string TransactionId;
	internal string PolicyId;
	internal string RecordId;
	internal string EffectId;
	internal string MechanismId;
	internal string ModuleId;
	internal string InstanceId;
	internal string ReceiptId;
	internal int? Attempt;
	internal string IdempotencyHash;
	internal string StateBefore;
	internal string StateAfter;
	internal string TargetHash;
	internal int? TargetCount;
	internal string IssuerKingdomId;
	internal string IssuerKingdomName;
	internal string TargetKingdomId;
	internal string TargetKingdomName;
	internal string TargetKind;
	internal string TargetId;
	internal string TargetName;
	internal string TargetKeys;
	internal string TargetSummary;
	internal string PlanSignature;
	internal long? Gold;
	internal double? Influence;
	internal int? CampaignDay;
	internal string Route;
	internal string Model;
	internal int? InputTokens;
	internal int? OutputTokens;
	internal int? HttpStatus;
	internal long? DurationMs;
	internal bool? TimedOut;
	internal bool? Retried;
	internal bool? ThinkingFallback;
	internal string ErrorKind;
	internal string ExceptionType;
	internal int? HResult;
	internal string StackHash;
	internal int? MessageChars;
	internal string MessageHash;
	internal int? DetailChars;
	internal string DetailHash;
	internal string CostReceiptHash;
	internal string ExecutionReceiptHash;
	internal Dictionary<string, int> Counts;
}

internal static class PolicySystemLog
{
	private const string FileName = "PolicySystem.txt";
	private const int SchemaVersion = 1;
	private const int MaxHashInputChars = 32768;
	private static readonly object WriteLock = new object();
	private static readonly string SessionId = Guid.NewGuid().ToString("N");
	private static long _sequence = DateTime.UtcNow.Ticks;
	private static bool _sessionStarted;

	// WriteRuntime remains filtered because several runtime adapters call it from recurring
	// maintenance paths. Explicit Write/Lifecycle calls are event boundaries and always emit.
	private static readonly HashSet<string> LifecycleStages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"session-start",
		"generation-start",
		"generation-stage",
		"generation-complete",
		"generation-failed",
		"generation-stale",
		"generation-stale-discarded",
		"generation-stale-pre-agenda-removed",
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
		"target-plan-route",
		"target-plan-fallback",
		"target-plan-blocked",
		"target-plan-candidate-rejected",
		"target-plan-resolved",
		"policy-effect-overlap-coalesced",
		"runtime-index-duplicate-contribution",
		"llm-stage-usage",
		"auto-draft-start",
		"auto-draft-complete",
		"policy-effect-duration-normalized",
		"policy-effect-legacy-normalized",
		"policy-complete-stale",
		"pending-created",
		"submit",
		"submitted",
		"published",
		"agenda-submitted",
		"agenda-approved",
		"agenda-rejected",
		"complete-agenda-submitted",
		"adopted",
		"adoption-rejected",
		"agenda-submit-rejected",
		"pre-cleanup-policy-restore-complete",
		"commit-start",
		"commit-step",
		"commit-complete",
		"commit-failed",
		"abolished",
		"abolition-start",
		"abolition-complete",
		"expiry-detected",
		"expiry-abolition-submitted",
		"expiry-abolition-rejected",
		"expiry-complete",
		"renewal-start",
		"renewal-committed",
		"renewal-rejected",
		"active-created",
		"active-effects-created",
		"active-bundle-created",
		"instance-created",
		"instance-ended",
		"effect-ended",
		"deduct-cost",
		"cost-committed",
		"cost-refunded",
		"cost-compensated",
		"receipt-created",
		"one-shot-start",
		"one-shot-complete",
		"scheduled-start",
		"scheduled-complete",
		"daily-start",
		"daily-complete",
		"daily-modules-complete",
		"compensation-start",
		"compensation-complete",
		"compensation-failed",
		"rollback-start",
		"rollback-complete",
		"rollback-failed",
		"save-write",
		"save-read",
		"save-summary",
		"load-summary",
		"load-normalized",
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
		"lifecycle-dispatched",
		"runtime-target-structure-refresh"
	};

	internal static void Write(string category, string stage, string message, string detail = null)
	{
		WriteLegacy(category, stage, message, detail, filterRuntimeStage: false);
	}

	internal static void WriteRuntime(string category, string message)
	{
		string normalizedMessage = Clean(message, string.Empty);
		WriteLegacy(category, FirstToken(normalizedMessage), normalizedMessage, null, filterRuntimeStage: true);
	}

	internal static void Lifecycle(string category, string stage, string result, PolicyLogContext context = null)
	{
		string normalizedResult = Clean(result, "event");
		bool failed = ContainsFailureMarker(normalizedResult) || ContainsFailureMarker(stage);
		Emit(category, stage, normalizedResult, "lifecycle", failed ? "error" : "info", context);
	}

	internal static string HashSensitive(string value)
	{
		return StableHash(value);
	}

	// Low-frequency structural checkpoint only. Never call this from model/getter hot paths.
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
		PolicyLogContext context = new PolicyLogContext
		{
			ModuleId = moduleId,
			Counts = new Dictionary<string, int>(StringComparer.Ordinal)
		};
		AddStateCount(context.Counts, "routing", routing);
		AddStateCount(context.Counts, "compile", compile);
		AddStateCount(context.Counts, "migration", migration);
		AddStateCount(context.Counts, "index", index);
		AddStateCount(context.Counts, "execution", execution);
		AddStateCount(context.Counts, "publication", publication);
		Lifecycle(category, "module-lifecycle", "checkpoint", context);
	}

	internal static void Failure(string category, string stage, string message, string detail = null)
	{
		Emit(category, Clean(stage, "failure"), "failed", "failure", "error", BuildLegacyContext(message, detail));
	}

	internal static void Failure(string category, string stage, Exception exception, PolicyLogContext context = null)
	{
		context ??= new PolicyLogContext();
		ApplyException(context, exception);
		Emit(category, Clean(stage, "failure"), "failed", "failure", "error", context);
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
		PolicyLogContext context = new PolicyLogContext
		{
			TransactionId = transactionId,
			RecordId = recordId,
			EffectId = effectId,
			MechanismId = mechanismId,
			ErrorKind = errorKind,
			TargetHash = targetHash,
			TargetCount = Math.Max(0, targetCount),
			StateBefore = stateBefore,
			StateAfter = stateAfter,
			CostReceiptHash = StableHash(costReceipt),
			ExecutionReceiptHash = StableHash(executionReceipt)
		};
		ApplyLegacyFields(context, costReceipt);
		Emit("Transaction", stage, result, "transaction", IsFailureResult(result, errorKind) ? "error" : "info", context);
	}

	internal static void DiagnosticFailure(string category, string stage, string message, string detail)
	{
		Emit(category, Clean(stage, "diagnostic-failure"), "failed", "diagnostic", "error", BuildLegacyContext(message, detail));
	}

	private static void WriteLegacy(string category, string stage, string message, string detail, bool filterRuntimeStage)
	{
		string normalizedStage = Clean(stage, "log");
		bool failure = ContainsFailureMarker(normalizedStage) || ContainsFailureMarker(message);
		if (filterRuntimeStage && !LifecycleStages.Contains(normalizedStage) && !failure)
		{
			return;
		}
		Emit(category, normalizedStage, failure ? "failed" : "event", failure ? "failure" : "lifecycle",
			failure ? "error" : "info", BuildLegacyContext(message, failure ? detail : null));
	}

	private static void Emit(string category, string stage, string result, string kind, string severity, PolicyLogContext context)
	{
		try
		{
			lock (WriteLock)
			{
				EnsureSessionStartedLocked();
				long sequence = NextSequence();
				string line = BuildEventJson(category, stage, result, kind, severity, context, sequence, DateTime.UtcNow);
				Logger.LogToFile(FileName, line + Environment.NewLine);
			}
		}
		catch
		{
		}
	}

	private static void EnsureSessionStartedLocked()
	{
		if (_sessionStarted)
		{
			return;
		}
		_sessionStarted = true;
		PolicyLogContext context = new PolicyLogContext();
		Version version = typeof(PolicySystemLog).Assembly.GetName().Version;
		if (version != null)
		{
			context.Version = version.ToString();
		}
		string line = BuildEventJson("Policy", "session-start", "started", "session", "info", context,
			NextSequence(), DateTime.UtcNow);
		Logger.LogToFile(FileName, line + Environment.NewLine);
	}

	private static long NextSequence()
	{
		lock (WriteLock)
		{
			return ++_sequence;
		}
	}

	private static string BuildEventJson(
		string category,
		string stage,
		string result,
		string kind,
		string severity,
		PolicyLogContext context,
		long sequence,
		DateTime utc)
	{
		context ??= new PolicyLogContext();
		JObject value = new JObject
		{
			["schema"] = SchemaVersion,
			["utc"] = utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
			["sessionId"] = SessionId,
			["sequence"] = sequence,
			["severity"] = SafeToken(severity, "info", 16),
			["kind"] = SafeToken(kind, "lifecycle", 32),
			["category"] = SafeToken(category, "Policy", 48),
			["stage"] = SafeToken(stage, "log", 96),
			["result"] = SafeToken(result, "event", 48)
		};
		AddString(value, "version", SafeToken(context.Version, null, 48), 48);
		AddString(value, "requestId", context.RequestId, 160);
		AddString(value, "generationId", context.GenerationId, 160);
		AddString(value, "batchId", context.BatchId, 160);
		AddString(value, "jobId", context.JobId, 160);
		AddString(value, "transactionId", context.TransactionId, 192);
		AddString(value, "policyId", context.PolicyId, 160);
		AddString(value, "recordId", context.RecordId, 160);
		AddString(value, "effectId", context.EffectId, 160);
		AddString(value, "mechanismId", context.MechanismId, 96);
		AddString(value, "moduleId", context.ModuleId, 96);
		AddString(value, "instanceId", context.InstanceId, 160);
		AddString(value, "receiptId", context.ReceiptId, 160);
		AddNumber(value, "attempt", context.Attempt);
		AddString(value, "idempotencyHash", NormalizeHash(context.IdempotencyHash), 80);
		AddString(value, "stateBefore", context.StateBefore, 96);
		AddString(value, "stateAfter", context.StateAfter, 96);
		AddString(value, "targetHash", NormalizeHash(context.TargetHash), 80);
		AddNumber(value, "targetCount", context.TargetCount);
		AddString(value, "issuerKingdomId", SafeToken(context.IssuerKingdomId, null, 96), 96);
		AddString(value, "issuerKingdomName", context.IssuerKingdomName, 160);
		AddString(value, "targetKingdomId", SafeToken(context.TargetKingdomId, null, 96), 96);
		AddString(value, "targetKingdomName", context.TargetKingdomName, 160);
		AddString(value, "targetKind", SafeToken(context.TargetKind, null, 64), 64);
		AddString(value, "targetId", SafeToken(context.TargetId, null, 160), 160);
		AddString(value, "targetName", context.TargetName, 160);
		AddString(value, "targetKeys", context.TargetKeys, 512);
		AddString(value, "targetSummary", context.TargetSummary, 1024);
		AddString(value, "planSignature", context.PlanSignature, 512);
		AddNumber(value, "gold", context.Gold);
		AddNumber(value, "influence", context.Influence);
		AddNumber(value, "campaignDay", context.CampaignDay);
		AddString(value, "route", SafeRoute(context.Route), 64);
		AddString(value, "model", SafeToken(context.Model, null, 96), 96);
		AddNumber(value, "inputTokens", context.InputTokens);
		AddNumber(value, "outputTokens", context.OutputTokens);
		AddNumber(value, "httpStatus", context.HttpStatus);
		AddNumber(value, "durationMs", context.DurationMs);
		AddBoolean(value, "timedOut", context.TimedOut);
		AddBoolean(value, "retried", context.Retried);
		AddBoolean(value, "thinkingFallback", context.ThinkingFallback);
		AddString(value, "errorKind", SafeToken(context.ErrorKind, null, 96), 96);
		AddString(value, "exceptionType", SafeToken(context.ExceptionType, null, 160), 160);
		AddNumber(value, "hResult", context.HResult);
		AddString(value, "stackHash", NormalizeHash(context.StackHash), 80);
		AddNumber(value, "messageChars", context.MessageChars);
		AddString(value, "messageHash", NormalizeHash(context.MessageHash), 80);
		AddNumber(value, "detailChars", context.DetailChars);
		AddString(value, "detailHash", NormalizeHash(context.DetailHash), 80);
		AddString(value, "costReceiptHash", NormalizeHash(context.CostReceiptHash), 80);
		AddString(value, "executionReceiptHash", NormalizeHash(context.ExecutionReceiptHash), 80);
		if (context.Counts != null && context.Counts.Count > 0)
		{
			JObject counts = new JObject();
			foreach (KeyValuePair<string, int> pair in context.Counts)
			{
				string key = SafeToken(pair.Key, null, 48);
				if (!string.IsNullOrWhiteSpace(key))
				{
					counts[key] = pair.Value;
				}
			}
			if (counts.Count > 0)
			{
				value["counts"] = counts;
			}
		}
		return value.ToString(Formatting.None);
	}

	private static PolicyLogContext BuildLegacyContext(string message, string detail)
	{
		PolicyLogContext context = new PolicyLogContext();
		if (!string.IsNullOrEmpty(message))
		{
			context.MessageChars = message.Length;
			context.MessageHash = StableHash(message);
			ApplyLegacyFields(context, message);
		}
		if (!string.IsNullOrEmpty(detail))
		{
			context.DetailChars = detail.Length;
			context.DetailHash = StableHash(detail);
		}
		return context;
	}

	private static void ApplyLegacyFields(PolicyLogContext context, string message)
	{
		if (context == null || string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		string[] tokens = Clean(message, string.Empty).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		for (int index = 0; index < tokens.Length; index++)
		{
			string token = tokens[index];
			int separator = token.IndexOf('=');
			if (separator <= 0 || separator >= token.Length - 1)
			{
				continue;
			}
			string key = token.Substring(0, separator).Trim();
			string value = token.Substring(separator + 1).Trim();
			switch (key.ToLowerInvariant())
			{
				case "requestid": context.RequestId ??= value; break;
				case "generationid": context.GenerationId ??= value; break;
				case "batch":
				case "batchid": context.BatchId ??= value; break;
				case "job":
				case "jobid": context.JobId ??= value; break;
				case "transactionid": context.TransactionId ??= value; break;
				case "policy":
				case "policyid": context.PolicyId ??= value; break;
				case "recordid": context.RecordId ??= value; break;
				case "effectid": context.EffectId ??= value; break;
				case "mechanismid": context.MechanismId ??= value; break;
				case "moduleid": context.ModuleId ??= value; break;
				case "instanceid": context.InstanceId ??= value; break;
				case "receiptid": context.ReceiptId ??= value; break;
				case "attempt":
				case "attempts": context.Attempt ??= ParseInt(value); break;
				case "targethash": context.TargetHash ??= value; break;
				case "targetcount": context.TargetCount ??= ParseInt(value); break;
				case "issuer":
				case "issuerkingdomid": context.IssuerKingdomId ??= value; break;
				case "issuerkingdomname": context.IssuerKingdomName ??= value; break;
				case "kingdom":
				case "target":
				case "targetkingdomid": context.TargetKingdomId ??= value; break;
				case "targetkingdomname": context.TargetKingdomName ??= value; break;
				case "targetkind": context.TargetKind ??= value; break;
				case "targetid": context.TargetId ??= value; break;
				case "targetname": context.TargetName ??= value; break;
				case "targetkeys": context.TargetKeys ??= value; break;
				case "targetsummary": context.TargetSummary ??= value; break;
				case "signature":
				case "plansignature": context.PlanSignature ??= value; break;
				case "gold": context.Gold ??= ParseLong(value); break;
				case "influence": context.Influence ??= ParseDouble(value); break;
				case "day":
				case "campaignday": context.CampaignDay ??= ParseInt(value); break;
				case "route": context.Route ??= value; break;
				case "model": context.Model ??= value; break;
				case "inputtokens": context.InputTokens ??= ParseInt(value); break;
				case "outputtokens": context.OutputTokens ??= ParseInt(value); break;
				case "httpstatus":
				case "statuscode": context.HttpStatus ??= ParseInt(value); break;
			}
		}
		if (string.IsNullOrWhiteSpace(context.GenerationId) && !string.IsNullOrWhiteSpace(context.RequestId))
		{
			context.GenerationId = context.RequestId;
		}
	}

	private static void ApplyException(PolicyLogContext context, Exception exception)
	{
		if (context == null || exception == null)
		{
			return;
		}
		context.ExceptionType = exception.GetType().FullName;
		context.HResult = exception.HResult;
		context.MessageChars = exception.Message?.Length ?? 0;
		context.MessageHash = StableHash(exception.Message);
		context.StackHash = StableHash(exception.StackTrace);
	}

	private static bool IsFailureResult(string result, string errorKind)
	{
		return ContainsFailureMarker(result)
			|| ContainsFailureMarker(errorKind);
	}

	private static string SafeRoute(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return value.IndexOf("://", StringComparison.Ordinal) >= 0
			? StableHash(value)
			: SafeToken(value, null, 64);
	}

	private static bool ContainsFailureMarker(string value)
	{
		string text = value ?? string.Empty;
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

	private static void AddStateCount(Dictionary<string, int> counts, string name, string value)
	{
		if (counts == null || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		counts[name + "." + SafeToken(value, "unknown", 32)] = 1;
	}

	private static void AddString(JObject value, string name, string fieldValue, int maxChars)
	{
		string normalized = ClipOneLine(fieldValue, maxChars);
		if (!string.IsNullOrWhiteSpace(normalized))
		{
			value[name] = normalized;
		}
	}

	private static void AddNumber(JObject value, string name, int? fieldValue)
	{
		if (fieldValue.HasValue)
		{
			value[name] = fieldValue.Value;
		}
	}

	private static void AddNumber(JObject value, string name, long? fieldValue)
	{
		if (fieldValue.HasValue)
		{
			value[name] = fieldValue.Value;
		}
	}

	private static void AddNumber(JObject value, string name, double? fieldValue)
	{
		if (fieldValue.HasValue && !double.IsNaN(fieldValue.Value) && !double.IsInfinity(fieldValue.Value))
		{
			value[name] = fieldValue.Value;
		}
	}

	private static void AddBoolean(JObject value, string name, bool? fieldValue)
	{
		if (fieldValue.HasValue)
		{
			value[name] = fieldValue.Value;
		}
	}

	private static int? ParseInt(string value)
	{
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;
	}

	private static long? ParseLong(string value)
	{
		return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) ? parsed : null;
	}

	private static double? ParseDouble(string value)
	{
		return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
			&& !double.IsNaN(parsed) && !double.IsInfinity(parsed) ? parsed : null;
	}

	private static string NormalizeHash(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}
		return value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? value : StableHash(value);
	}

	private static string StableHash(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		string bounded = value.Length <= MaxHashInputChars ? value : value.Substring(0, MaxHashInputChars);
		using SHA256 sha = SHA256.Create();
		byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(bounded));
		StringBuilder builder = new StringBuilder(39);
		builder.Append("sha256:");
		for (int index = 0; index < 16; index++)
		{
			builder.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));
		}
		return builder.ToString();
	}

	private static string SafeToken(string value, string fallback, int maxChars)
	{
		string normalized = ClipOneLine(value, maxChars);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return fallback;
		}
		for (int index = 0; index < normalized.Length; index++)
		{
			char character = normalized[index];
			if (!(char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.' || character == ':' || character == '/'))
			{
				return StableHash(normalized);
			}
		}
		return normalized;
	}

	private static string ClipOneLine(string text, int maxChars)
	{
		string clean = Clean(text, string.Empty);
		return clean.Length <= maxChars ? clean : clean.Substring(0, maxChars);
	}

	private static string Clean(string value, string fallback)
	{
		string clean = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
		return string.IsNullOrWhiteSpace(clean) ? fallback : clean;
	}
}
