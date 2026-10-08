using System;
using System.Collections.Generic;

namespace AnimusForge;

// Keeps the legacy DTO and live profile references; callers capture Hero identity on the campaign thread.
internal sealed class PersonaProfileStateOwner
{
 internal Dictionary<string, MyBehavior.NpcPersonaProfile> Profiles = new Dictionary<string, MyBehavior.NpcPersonaProfile>();
	internal MyBehavior.NpcPersonaProfile Get(string heroId, bool createIfMissing)
	{
		if (heroId == null)
		{
			return null;
		}
		string stringId = heroId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (Profiles == null)
		{
			Profiles = new Dictionary<string, MyBehavior.NpcPersonaProfile>();
		}
		if (Profiles.TryGetValue(stringId, out var value))
		{
			return value;
		}
		if (!createIfMissing)
		{
			return null;
		}
		value = new MyBehavior.NpcPersonaProfile();
		Profiles[stringId] = value;
		return value;
	}
	internal void Save(string heroId, MyBehavior.NpcPersonaProfile profile, Action<string, MyBehavior.NpcPersonaProfile> stamp)
	{
		if (heroId == null)
		{
			return;
		}
		string stringId = heroId;
		if (string.IsNullOrEmpty(stringId))
		{
			return;
		}
		if (Profiles == null)
		{
			Profiles = new Dictionary<string, MyBehavior.NpcPersonaProfile>();
		}
		string text = (profile?.Personality ?? "").Trim();
		string text2 = (profile?.Background ?? "").Trim();
		string text3 = (profile?.VoiceId ?? "").Trim();
		if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3))
		{
			Profiles.Remove(stringId);
			return;
		}
		if (profile == null)
		{
			profile = new MyBehavior.NpcPersonaProfile();
		}
		stamp(stringId, profile);
		profile.Personality = text;
		profile.Background = text2;
		profile.VoiceId = text3;
		Profiles[stringId] = profile;
	}

internal void ResetForCurrentSave() { Profiles = new Dictionary<string,MyBehavior.NpcPersonaProfile>(); }
internal void GetNpcPersonaStrings(string heroId, out string personality, out string background)
{
 personality = "";
 background = "";
 if (!string.IsNullOrEmpty(heroId) && Profiles != null && Profiles.TryGetValue(heroId, out var value) && value != null)
 {
  personality = value.Personality ?? "";
  background = value.Background ?? "";
 }
}
internal string GetNpcVoiceId(string heroId)
{
 if (string.IsNullOrEmpty(heroId) || Profiles == null) return "";
 if (Profiles.TryGetValue(heroId, out var value) && value != null) return (value.VoiceId ?? "").Trim();
 return "";
}
}
