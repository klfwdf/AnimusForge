using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyWarPressureRules
{
	public static void ApplyDocumentPressure(
		WorldDiplomacyDocument document,
		Func<string, string, WarPressureEntry> findPressure,
		Func<IEnumerable<string>, string, List<string>> normalizeKingdomIds,
		Action<string, string, int, string, string> applyPressure)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.AuthorKingdomId)
			|| findPressure == null || normalizeKingdomIds == null || applyPressure == null)
		{
			return;
		}
		int delta = document.Intent switch
		{
			"condemn" => 6,
			"warning" => 10,
			"ultimatum" => 18,
			"reject" => 8,
			"reject_peace" => 8,
			"reject_alliance" => 6,
			"reject_trade" => 4,
			"declare_war" => 0,
			"apology" => -8,
			"concession" => -12,
			"accept_peace" => -20,
			_ => string.Equals(document.Tone, "hostile", StringComparison.OrdinalIgnoreCase) ? 3 : 0
		};
		foreach (string targetId in normalizeKingdomIds((document.AddressedKingdomIds ?? new List<string>()).Concat(new[] { document.TargetKingdomId }), document.AuthorKingdomId))
		{
			WarPressureEntry existing = findPressure(document.AuthorKingdomId, targetId);
			int repetition = existing != null && string.Equals(existing.LastIntent, document.Intent, StringComparison.OrdinalIgnoreCase) ? existing.ConsecutiveSimilarCount : 0;
			float repetitionFactor = delta > 0 ? 1f / (1f + repetition * 0.35f) : 1f;
			int scaledDelta = (int)Math.Round(delta * repetitionFactor);
			if (scaledDelta != 0) applyPressure(document.AuthorKingdomId, targetId, scaledDelta, "外交宣言：" + document.Title, document.Intent);
		}
	}
	public static int GetWarPressure(List<WarPressureEntry> warPressure, string sourceId, string targetId)
{
		return warPressure?.FirstOrDefault(x => x != null
			&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))?.Value ?? 0;
	}
	public static void AddWarPressure(List<WarPressureEntry> warPressure, string sourceId, string targetId, int delta, string reason, int currentDay, string intent = "")
{
		if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId) || string.Equals(sourceId, targetId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		WarPressureEntry entry = warPressure?.FirstOrDefault(x => x != null
			&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
		if (entry == null)
		{
			entry = new WarPressureEntry
			{
				SourceKingdomId = sourceId,
				TargetKingdomId = targetId
			};
			warPressure.Add(entry);
		}
		entry.Value = Math.Max(0, Math.Min(300, entry.Value + delta));
		entry.LastUpdatedDay = currentDay;
		entry.LastReason = WorldDiplomacyTextRules.Limit(reason, 300);
		if (!string.IsNullOrWhiteSpace(intent))
		{
			entry.ConsecutiveSimilarCount = string.Equals(entry.LastIntent, intent, StringComparison.OrdinalIgnoreCase) ? Math.Min(8, entry.ConsecutiveSimilarCount + 1) : 0;
			entry.LastIntent = intent;
		}
		if (delta > 0)
		{
			entry.NeedsFreshEscalation = false;
		}
		// 兼容旧存档字段；压力现在只作为LLM可读的定性历史，不再武装任何自动行动。
		entry.IsEscalationArmed = false;
		entry.ArmedDay = 0;
	}
	public static void ClearWarPressure(List<WarPressureEntry> warPressure, string sourceId, string targetId, int currentDay)
{
		WarPressureEntry entry = warPressure?.FirstOrDefault(x => x != null
			&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
		if (entry != null)
		{
			entry.Value = 0;
			entry.IsEscalationArmed = false;
			entry.LastUpdatedDay = currentDay;
			entry.LastReason = "外交行动完成，压力清空";
		}
	}
	public static void DecayWarPressure(List<WarPressureEntry> warPressure, int currentDay)
{
		int day = currentDay;
		foreach (WarPressureEntry entry in warPressure)
		{
			if (entry == null || entry.Value <= 0 || day - entry.LastUpdatedDay < 7)
			{
				continue;
			}
			entry.Value = Math.Max(0, entry.Value - 4);
			entry.IsEscalationArmed = false;
			entry.ArmedDay = 0;
		}
	}
	public static WorldDiplomacyWarLedger ResolveWarLedger(List<WorldDiplomacyWarLedger> warLedgers, string firstId, string secondId)
{
		string key = WorldDiplomacyRoundLifecycleRules.PairKey(firstId, secondId);
		return warLedgers?.FirstOrDefault(x => x != null
			&& string.Equals(x.PairKey, key, StringComparison.OrdinalIgnoreCase));
	}
	public static void RemoveWarLedger(List<WorldDiplomacyWarLedger> warLedgers, string firstId, string secondId)
{
		string key = WorldDiplomacyRoundLifecycleRules.PairKey(firstId, secondId);
		warLedgers?.RemoveAll(x => x != null
			&& string.Equals(x.PairKey, key, StringComparison.OrdinalIgnoreCase));
	}
	public static WorldDiplomacyWarLedger EnsureWarLedger(List<WorldDiplomacyWarLedger> warLedgers, string firstId, string secondId, int currentDay)
{
		if (warLedgers == null || string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId)
			|| string.Equals(firstId, secondId, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		WorldDiplomacyWarLedger existing = ResolveWarLedger(warLedgers, firstId, secondId);
		if (existing != null)
		{
			return existing;
		}
		string orderedFirstId = string.Compare(firstId, secondId, StringComparison.OrdinalIgnoreCase) <= 0 ? firstId : secondId;
		string orderedSecondId = string.Equals(orderedFirstId, firstId, StringComparison.OrdinalIgnoreCase) ? secondId : firstId;
		WorldDiplomacyWarLedger ledger = new WorldDiplomacyWarLedger
		{
			PairKey = WorldDiplomacyRoundLifecycleRules.PairKey(orderedFirstId, orderedSecondId),
			FirstKingdomId = orderedFirstId,
			SecondKingdomId = orderedSecondId,
			StartedDay = currentDay
		};
		warLedgers.Add(ledger);
		return ledger;
	}
}
