using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnimusForge.SiegeAftermathIntervention;

namespace AnimusForge;

/// <summary>
/// Schedules one guarded AF generation request for a missing town-tenure narrative.
/// </summary>
internal static class GcczTownRuleMemoryGenerationBridge
{
	internal delegate bool TryStoreNarrative(
		string settlementId,
		string rulerId,
		int ruleStartDay,
		string narrative);

	private const int GenerationMaxTokens = 220;
	private const float GenerationTemperature = 0.45f;
	private static readonly TimeSpan FailureCooldown = TimeSpan.FromMinutes(1);
	private static readonly object Gate = new object();
	private static readonly HashSet<string> InFlightKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, DateTime> RetryAfterUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

	internal static void Queue(
		SettlementRuleMemoryRecord record,
		int currentDay,
		bool force,
		TryStoreNarrative tryStore,
		Action<string> onStored)
	{
		if (record?.CurrentRule == null
			|| !string.IsNullOrWhiteSpace(record.CurrentRule.Narrative)
			|| tryStore == null)
		{
			return;
		}

		SettlementRuleMemoryGenerationPrompt prompt = TownPromptComposer.BuildSettlementRuleMemoryGenerationPrompt(
			record,
			currentDay,
			GcczTownPromptResourceProvider.GetCatalog());
		if (string.IsNullOrWhiteSpace(prompt.SystemPrompt) || string.IsNullOrWhiteSpace(prompt.UserPrompt))
		{
			return;
		}

		string generationKey = BuildGenerationKey(record);
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		lock (Gate)
		{
			if (InFlightKeys.Contains(generationKey))
			{
				return;
			}
			if (!force
				&& RetryAfterUtc.TryGetValue(generationKey, out DateTime retryAfter)
				&& retryAfter > DateTime.UtcNow)
			{
				return;
			}
			InFlightKeys.Add(generationKey);
		}

		Task.Run(delegate
		{
			bool stored = false;
			string error = string.Empty;
			try
			{
				var messages = new object[]
				{
					new { role = "system", content = prompt.SystemPrompt },
					new { role = "user", content = prompt.UserPrompt },
				};
				if (AIConfigHandler.TryCallAuxiliarySimpleDialogueOnceForExternal(
					messages,
					GenerationMaxTokens,
					GenerationTemperature,
					out string content,
					out error)
					&& !SaveRuntimeGuard.IsStale(runtimeGeneration, "gccz_town_memory")
					&& SettlementRuleMemoryNarrativePolicy.TryParseGeneratedResponse(content, out string narrative))
				{
					stored = tryStore(
						record.SettlementId,
						record.CurrentRule.RulerId,
						record.CurrentRule.RuleStartDay,
						narrative);
				}
			}
			catch (Exception ex)
			{
				error = ex.Message;
			}
			finally
			{
				CompleteGeneration(generationKey, stored);
				if (stored)
				{
					onStored?.Invoke(record.SettlementId);
					Logger.Log("GcczTownRuleMemory", "Generated town memory. Settlement=" + record.SettlementId + ", Ruler=" + record.CurrentRule.RulerId);
				}
				else if (!SaveRuntimeGuard.IsStale(runtimeGeneration))
				{
					Logger.Log("GcczTownRuleMemory", "Town memory generation was not stored. Settlement=" + record.SettlementId + ", Error=" + NormalizeLogValue(error));
				}
			}
		});
	}

	internal static void AllowImmediateRetry(SettlementRuleMemoryRecord record)
	{
		lock (Gate)
		{
			RetryAfterUtc.Remove(BuildGenerationKey(record));
		}
	}

	internal static void Reset()
	{
		lock (Gate)
		{
			InFlightKeys.Clear();
			RetryAfterUtc.Clear();
		}
	}

	private static void CompleteGeneration(string generationKey, bool stored)
	{
		lock (Gate)
		{
			InFlightKeys.Remove(generationKey);
			if (stored)
			{
				RetryAfterUtc.Remove(generationKey);
			}
			else
			{
				RetryAfterUtc[generationKey] = DateTime.UtcNow.Add(FailureCooldown);
			}
		}
	}

	private static string BuildGenerationKey(SettlementRuleMemoryRecord record)
	{
		SettlementRuleMemoryEntry rule = record?.CurrentRule;
		return (record?.SettlementId ?? string.Empty)
			+ "|"
			+ (rule?.RulerId ?? rule?.RulerName ?? string.Empty)
			+ "|"
			+ (rule?.RuleStartDay ?? 0);
	}

	private static string NormalizeLogValue(string value)
	{
		string normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
		return string.IsNullOrWhiteSpace(normalized) ? "none" : normalized;
	}
}
