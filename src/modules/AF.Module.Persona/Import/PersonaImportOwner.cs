using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// The legacy nested profile type stays in its host to preserve save/type identity.
// This operation mutates only the supplied authoritative dictionary; it retains no state.
internal static class PersonaImportOwner
{
 internal static void RestoreProfileSnapshot<TProfile>(ref Dictionary<string,TProfile> authority, Dictionary<string,TProfile> restored) where TProfile : class
 { authority = restored; }
 internal static void ApplySingleProfile<TProfile>(ref Dictionary<string, TProfile> authority, string heroId, TProfile imported) where TProfile : class
 {
  if (authority == null) authority = new Dictionary<string, TProfile>();
  // Single-NPC null imports intentionally delete; bulk imports still skip null entries.
  if (imported == null) authority.Remove(heroId);
  else authority[heroId] = imported;
 }
 internal static void ApplyProfiles<TProfile>(ref Dictionary<string, TProfile> authority, Dictionary<string, TProfile> imported, bool overwriteExisting) where TProfile : class
 {
  if (imported == null) return;
  if (authority == null) authority = new Dictionary<string, TProfile>();
  foreach (var item in imported)
  {
   if (string.IsNullOrEmpty(item.Key) || item.Value == null) continue;
   if (!overwriteExisting && authority.ContainsKey(item.Key)) continue;
   if (overwriteExisting) authority.Remove(item.Key);
   authority[item.Key] = item.Value;
  }
 }
	internal static int ReplaceVoiceAssignments(ref Dictionary<string, MyBehavior.NpcPersonaProfile> authority, Dictionary<string, string> sourceVoiceIds, string mainHeroId, Action<string, MyBehavior.NpcPersonaProfile> stamp, out int appliedVoiceIdCount)
	{
		appliedVoiceIdCount = 0;
		if (authority == null)
		{
			authority = new Dictionary<string, MyBehavior.NpcPersonaProfile>();
		}
		int num = 0;
		string text = (mainHeroId ?? "").Trim();
		foreach (KeyValuePair<string, MyBehavior.NpcPersonaProfile> item in authority.ToList())
		{
			string text2 = (item.Key ?? "").Trim();
			MyBehavior.NpcPersonaProfile value = item.Value;
			if (string.IsNullOrWhiteSpace(text2) || value == null || string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (!string.IsNullOrWhiteSpace(value.VoiceId))
			{
				num++;
			}
			value.VoiceId = "";
			if (string.IsNullOrWhiteSpace(value.Personality) && string.IsNullOrWhiteSpace(value.Background))
			{
				authority.Remove(text2);
				continue;
			}
		}
		if (sourceVoiceIds == null)
		{
			return num;
		}
		foreach (KeyValuePair<string, string> sourceVoiceId in sourceVoiceIds)
		{
			string text3 = (sourceVoiceId.Key ?? "").Trim();
			string text4 = (sourceVoiceId.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3) || string.IsNullOrWhiteSpace(text4) || string.Equals(text3, text, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			bool flag = !authority.TryGetValue(text3, out var value2) || value2 == null;
			if (flag)
			{
				value2 = new MyBehavior.NpcPersonaProfile();
			}
			value2.VoiceId = text4;
			if (flag)
			{
				// Only a newly created voice-only profile needs identity metadata stamped from the current Hero registry.
				stamp(text3, value2);
			}
			authority[text3] = value2;
			appliedVoiceIdCount++;
		}
		return num;
	}

}
