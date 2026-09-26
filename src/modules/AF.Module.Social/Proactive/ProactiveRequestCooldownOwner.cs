using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

public sealed partial class ProactiveNpcRequestBehavior
{
	// Owns detached cooldown/scan state; the host alone translates game time, MCM and save DTOs.
	private sealed class ProactiveRequestCooldownOwner
	{
		internal Dictionary<string, float> HeroCooldownUntilDays { get; private set; } = NewDictionary();
		internal Dictionary<string, float> NeedTypeFatigueUntilDays { get; private set; } = NewDictionary();
		internal Dictionary<string, float> DiplomacyDiscussionKeysUntilDays { get; private set; } = NewDictionary();
		internal float GlobalCooldownUntilHours { get; private set; }
		internal float LastScanHour { get; private set; } = -99999f;

		internal void Import(Dictionary<string, float> hero, Dictionary<string, float> need, Dictionary<string, float> discussion, float globalUntilHours, float lastScanHour)
		{
			HeroCooldownUntilDays = NormalizeDictionary(hero);
			NeedTypeFatigueUntilDays = NormalizeDictionary(need);
			DiplomacyDiscussionKeysUntilDays = NormalizeDictionary(discussion);
			GlobalCooldownUntilHours = globalUntilHours;
			LastScanHour = lastScanHour;
		}

		// Preserve the legacy load-failure behavior: only the three dictionaries are reset.
		internal void ResetDictionaries()
		{
			HeroCooldownUntilDays = NewDictionary();
			NeedTypeFatigueUntilDays = NewDictionary();
			DiplomacyDiscussionKeysUntilDays = NewDictionary();
		}

		internal bool TryBeginScan(float nowHours, int intervalHours)
		{
			if (nowHours - LastScanHour < intervalHours)
			{
				return false;
			}
			LastScanHour = nowHours;
			return true;
		}

		internal bool IsGlobalCooldownActive(float nowHours) => nowHours < GlobalCooldownUntilHours;
		internal void StartGlobalCooldown(float nowHours, int durationHours) => GlobalCooldownUntilHours = nowHours + durationHours;
		internal bool IsHeroOnCooldown(string key, float nowDays) => IsOnCooldown(HeroCooldownUntilDays, key, nowDays);
		internal void RecordHeroCooldown(string key, float nowDays, int durationDays)
		{
			if (!string.IsNullOrWhiteSpace(key))
			{
				HeroCooldownUntilDays[key] = nowDays + durationDays;
			}
		}

		internal float GetNeedRemainingDays(string normalizedNeedType, float nowDays)
		{
			return !string.IsNullOrWhiteSpace(normalizedNeedType)
				&& NeedTypeFatigueUntilDays.TryGetValue(normalizedNeedType, out float untilDays)
					? Math.Max(0f, untilDays - nowDays) : 0f;
		}

		internal void RecordNeedFatigue(string normalizedNeedType, float nowDays, int durationDays)
		{
			if (string.IsNullOrWhiteSpace(normalizedNeedType))
			{
				return;
			}
			if (durationDays <= 0)
			{
				NeedTypeFatigueUntilDays.Remove(normalizedNeedType);
				return;
			}
			NeedTypeFatigueUntilDays[normalizedNeedType] = nowDays + durationDays;
		}

		internal bool IsDiscussionOnCooldown(string key, float nowDays) => IsOnCooldown(DiplomacyDiscussionKeysUntilDays, key, nowDays);
		internal void RecordDiscussion(string key, float nowDays, int retentionDays)
		{
			if (!string.IsNullOrWhiteSpace(key))
			{
				DiplomacyDiscussionKeysUntilDays[key] = nowDays + retentionDays;
			}
		}

		// Called only after the hourly scan gate, never from a per-frame candidate path.
		internal void PruneExpiredNeedTypeFatigue(float nowDays)
		{
			foreach (string key in NeedTypeFatigueUntilDays.Where(pair => pair.Value <= nowDays).Select(pair => pair.Key).ToList())
			{
				NeedTypeFatigueUntilDays.Remove(key);
			}
		}

		internal void PruneExpiredDiplomacyDiscussionKeys(float nowDays)
		{
			foreach (string key in DiplomacyDiscussionKeysUntilDays.Where(pair => pair.Value <= nowDays).Select(pair => pair.Key).ToList())
			{
				DiplomacyDiscussionKeysUntilDays.Remove(key);
			}
			if (DiplomacyDiscussionKeysUntilDays.Count > 256)
			{
				foreach (string key in DiplomacyDiscussionKeysUntilDays.OrderBy(pair => pair.Value).Take(DiplomacyDiscussionKeysUntilDays.Count - 256).Select(pair => pair.Key).ToList())
				{
					DiplomacyDiscussionKeysUntilDays.Remove(key);
				}
			}
		}

		private static bool IsOnCooldown(Dictionary<string, float> dict, string key, float nowDays)
			=> !string.IsNullOrWhiteSpace(key) && dict.TryGetValue(key, out float untilDays) && untilDays > nowDays;

		private static Dictionary<string, float> NewDictionary() => new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

		private static Dictionary<string, float> NormalizeDictionary(Dictionary<string, float> source)
		{
			Dictionary<string, float> result = NewDictionary();
			if (source != null)
			{
				foreach (KeyValuePair<string, float> pair in source)
				{
					string key = (pair.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(key))
					{
						result[key] = pair.Value;
					}
				}
			}
			return result;
		}
	}
}
