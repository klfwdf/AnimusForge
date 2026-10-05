using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyWarPressureRules
{
    public static float CalculatePeacePressure(float warDays, float ownProgress, float enemyProgress,
        float ownStrength, float enemyStrength, int suffered, int inflicted, int otherWars, int lostFiefs)
    {
        float duration = WorldDiplomacyRoundLifecycleRules.Clamp01((warDays - 7f) / 112f) * 70f;
        float setback = WorldDiplomacyRoundLifecycleRules.Clamp01((enemyProgress - ownProgress) / 500f) * 70f;
        float strength = WorldDiplomacyRoundLifecycleRules.Clamp01((enemyStrength / Math.Max(1f, ownStrength) - 1f) / 1.5f) * 40f;
        float casualtyBurden = WorldDiplomacyRoundLifecycleRules.Clamp01(suffered / Math.Max(500f, ownStrength * 1.5f)) * 40f;
        float casualtyImbalance = WorldDiplomacyRoundLifecycleRules.Clamp01((suffered - inflicted) / Math.Max(500f, ownStrength)) * 20f;
        float multiWar = WorldDiplomacyRoundLifecycleRules.Clamp01(otherWars / 2f) * 30f;
        float territory = WorldDiplomacyRoundLifecycleRules.Clamp01(lostFiefs / 2f) * 30f;
        return Math.Max(0f, Math.Min(300f, duration + setback + strength + casualtyBurden + casualtyImbalance + multiWar + territory));
    }

    // Battle-built war weariness (0..100) adds up to 60 pressure on top of the snapshot terms.
    public static float WithWeariness(float pressure, float weariness)
        => Math.Max(0f, Math.Min(300f, pressure + Math.Max(0f, Math.Min(100f, weariness)) * 0.6f));

	public static int CalculateDocumentPressureDelta(string intent, string tone, string lastIntent, int consecutiveSimilarCount)
    {
		int delta = intent switch
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
			_ => string.Equals(tone, "hostile", StringComparison.OrdinalIgnoreCase) ? 3 : 0
		};
        int repetition = string.Equals(lastIntent, intent, StringComparison.OrdinalIgnoreCase) ? consecutiveSimilarCount : 0;
        float repetitionFactor = delta > 0 ? 1f / (1f + repetition * 0.35f) : 1f;
        return (int)Math.Round(delta * repetitionFactor);
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
