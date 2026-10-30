using System;
using System.Collections.Generic;

namespace AnimusForge;

// Optional event state is serialized inside the existing chunked profile JSON.
internal sealed class PersonaEvolutionFact
{
    public string Key, Facts, Connection;
    public double Hour;
}
internal sealed class PersonaEvolutionState
{
    public List<PersonaEvolutionFact> Pending = new List<PersonaEvolutionFact>();
    public List<string> Seen = new List<string>();
    public int Attempts;
}

internal sealed class NpcPersonaTrait
{
    public string Id;
    public int Strength;
}

internal sealed class NpcPersonaGeneratedData
{
    internal string Description = "";
    internal List<NpcPersonaTrait> Traits = new List<NpcPersonaTrait>();
    internal List<string> Motivations = new List<string>();
    internal List<string> Values = new List<string>();
    internal List<string> Fears = new List<string>();
    internal string SpeechStyle = "";
}

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
		value = new MyBehavior.NpcPersonaProfile { Description = "", SchemaVersion = 3 };
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
		bool retainExplicitClear = profile?.SchemaVersion >= 3;
		profile?.NormalizeDescription();
		string text = profile?.Description ?? "";
		string text3 = (profile?.VoiceId ?? "").Trim();
		string localVoice = (profile?.GptSoVitsVoiceId ?? "").Trim();
		string openAiVoice = (profile?.OpenAiVoiceId ?? "").Trim();
		if (!retainExplicitClear && string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text3) && string.IsNullOrEmpty(localVoice) && string.IsNullOrEmpty(openAiVoice)
			&& (profile?.Traits == null || profile.Traits.Count == 0)
			&& (profile?.Motivations == null || profile.Motivations.Count == 0)
			&& (profile?.Values == null || profile.Values.Count == 0)
			&& (profile?.Fears == null || profile.Fears.Count == 0)
			&& string.IsNullOrEmpty((profile?.SpeechStyle ?? "").Trim()))
		{
			Profiles.Remove(stringId);
			return;
		}
		if (profile == null)
		{
			profile = new MyBehavior.NpcPersonaProfile();
		}
		stamp(stringId, profile);
		profile.Description = text;
		profile.VoiceId = text3;
		profile.GptSoVitsVoiceId = localVoice;
		profile.OpenAiVoiceId = openAiVoice;
		profile.SpeechStyle = (profile.SpeechStyle ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		profile.RecentChangeReason = (profile.RecentChangeReason ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		Profiles[stringId] = profile;
	}

internal void ResetForCurrentSave() { Profiles = new Dictionary<string,MyBehavior.NpcPersonaProfile>(); }
internal void GetNpcPersonaStrings(string heroId, out string personality, out string background)
{
 personality = "";
 background = "";
 if (!string.IsNullOrEmpty(heroId) && Profiles != null && Profiles.TryGetValue(heroId, out var value) && value != null)
 {
  personality = value.Description ?? "";
 }
}
internal string GetDescription(string heroId)
{
 return !string.IsNullOrEmpty(heroId) && Profiles != null && Profiles.TryGetValue(heroId, out var value)
  ? value?.Description ?? "" : "";
}
internal string GetNpcVoiceId(string heroId)
{
 if (string.IsNullOrEmpty(heroId) || Profiles == null) return "";
 if (Profiles.TryGetValue(heroId, out var value) && value != null) return (value.VoiceId ?? "").Trim();
 return "";
}
}
