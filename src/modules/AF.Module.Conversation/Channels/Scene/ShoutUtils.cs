using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

public static class ShoutUtils
{
	private const float ShoutLineOfSightFallbackEyeHeight = 1.55f;

	private const float ShoutLineOfSightLowerBodyHeight = 1.05f;

	private class UnnamedNpcPersonaProfile
	{
		public string Description;

		public string Personality;

		public string Background;

		public string CultureId;

		public string Rank;

		public string Name;

		public string TroopId;
	}

	private class UnnamedNpcProfilesFile
	{
		public int Version = 1;

		public Dictionary<string, UnnamedNpcPersonaProfile> Profiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
	}

	public class UnnamedPersonaIndexItem
	{
		public string Key;

		public string Label;
	}

	private static readonly object _unnamedProfilesLock = new object();

	private static Dictionary<string, UnnamedNpcPersonaProfile> _unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();

	private static HashSet<string> _unnamedProfilesInFlight = new HashSet<string>();

	private static readonly object _promptNamePoolLock = new object();

	private static readonly Dictionary<string, List<string>> _promptNamePoolCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

	private static string SanitizeFileName(string s)
	{
		s = (s ?? "").Trim();
		if (string.IsNullOrEmpty(s))
		{
			return "unnamed";
		}
		char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			s = s.Replace(oldChar, '_');
		}
		s = s.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
		while (s.Contains("__"))
		{
			s = s.Replace("__", "_");
		}
		if (s.Length > 120)
		{
			s = s.Substring(0, 120);
		}
		return s;
	}

	private static string StableHash8(string s)
	{
		uint num = 2166136261u;
		string text = s ?? "";
		for (int i = 0; i < text.Length; i++)
		{
			num ^= text[i];
			num *= 16777619;
		}
		return num.ToString("x8");
	}

	private static void LoadUnnamedProfilesIfNeeded()
	{
		lock (_unnamedProfilesLock)
		{
			if (_unnamedProfiles == null)
			{
				_unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
			}
		}
	}

	private static void SaveUnnamedProfilesUnsafe()
	{
	}

	private static string GetUnnamedKey(Agent agent)
	{
		if (agent == null)
		{
			return "";
		}
		CharacterObject characterObject = agent.Character as CharacterObject;
		string text = characterObject?.StringId ?? "";
		string text2 = "";
		string text3 = "";
		try
		{
			if (!(agent.Origin is PrisonerAgentOrigin))
			{
				PartyBase partyBase = agent.Origin?.BattleCombatant as PartyBase;
				text2 = partyBase?.MapFaction?.StringId ?? "";
				text3 = partyBase?.LeaderHero?.StringId ?? "";
			}
		}
		catch
		{
			text2 = "";
			text3 = "";
		}
		if (string.IsNullOrWhiteSpace(text2) && !(agent.Origin is PrisonerAgentOrigin))
		{
			try
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				text2 = (currentSettlement?.OwnerClan?.Kingdom?.StringId ?? currentSettlement?.MapFaction?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text2 = "";
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			string text4 = (text2 ?? "").Trim().ToLower();
			string text5 = (text3 ?? "").Trim().ToLower();
			if (!string.IsNullOrEmpty(text4))
			{
				if (!string.IsNullOrEmpty(text5))
				{
					return ("troop:" + text + ":kingdom:" + text4 + ":lord:" + text5).ToLower();
				}
				return ("troop:" + text + ":kingdom:" + text4).ToLower();
			}
			return ("troop:" + text).ToLower();
		}
		string text6 = (characterObject?.Culture?.StringId ?? "neutral").ToLower();
		string text7 = ((characterObject != null && characterObject.IsSoldier) ? "soldier" : "commoner");
		string text8 = agent.Name?.ToString() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0731");
		string text9 = ("mix:" + text6 + ":" + text7 + ":" + text8).ToLower();
		string text10 = (text2 ?? "").Trim().ToLower();
		string text11 = (text3 ?? "").Trim().ToLower();
		if (!string.IsNullOrWhiteSpace(text10))
		{
			text9 = text9 + ":kingdom:" + text10;
		}
		if (!string.IsNullOrWhiteSpace(text11))
		{
			text9 = text9 + ":lord:" + text11;
		}
		return text9;
	}

	private static string GetUnnamedProfileDescription(UnnamedNpcPersonaProfile prof)
	{
		if (prof == null)
		{
			return "";
		}
		string text = (prof.Description ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (prof.Personality ?? "").Trim();
		string text3 = (prof.Background ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
		{
			return text2 + "；" + text3;
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		if (!string.IsNullOrWhiteSpace(text3))
		{
			return text3;
		}
		return "";
	}

	private static string MergePersonaFieldsToDescription(string personality, string background)
	{
		string text = (personality ?? "").Trim();
		string text2 = (background ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + "；" + text2;
	}

	public static bool TryGetUnnamedNpcPersona(Agent agent, out string personality, out string background)
	{
		personality = "";
		background = "";
		string unnamedKey = GetUnnamedKey(agent);
		if (string.IsNullOrEmpty(unnamedKey))
		{
			return false;
		}
		string text = "";
		string text2 = "";
		string text3 = "";
		string value = "";
		try
		{
			if (unnamedKey.StartsWith("troop:", StringComparison.OrdinalIgnoreCase))
			{
				int num = unnamedKey.IndexOf(":kingdom:", StringComparison.OrdinalIgnoreCase);
				if (num > 0)
				{
					text2 = unnamedKey.Substring(0, num);
				}
				else
				{
					value = (unnamedKey + ":kingdom:").ToLower();
				}
				int num2 = unnamedKey.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
				if (num2 > 0)
				{
					text3 = unnamedKey.Substring(0, num2).ToLower();
				}
				CharacterObject characterObject = agent?.Character as CharacterObject;
				string text4 = (characterObject?.Culture?.StringId ?? "neutral").ToLower();
				string text5 = ((characterObject != null && characterObject.IsSoldier) ? "soldier" : "commoner");
				string text6 = agent?.Name?.ToString() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0732");
				text = ("mix:" + text4 + ":" + text5 + ":" + text6).ToLower();
			}
			else if (unnamedKey.StartsWith("mix:", StringComparison.OrdinalIgnoreCase))
			{
				int num3 = unnamedKey.IndexOf(":kingdom:", StringComparison.OrdinalIgnoreCase);
				int num4 = unnamedKey.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
				int num5 = -1;
				if (num3 > 0 && num4 > 0)
				{
					num5 = Math.Min(num3, num4);
				}
				else if (num3 > 0)
				{
					num5 = num3;
				}
				else if (num4 > 0)
				{
					num5 = num4;
				}
				text = ((num5 <= 0) ? unnamedKey.ToLower() : unnamedKey.Substring(0, num5).ToLower());
				if (num4 > 0)
				{
					text3 = unnamedKey.Substring(0, num4).ToLower();
				}
			}
		}
		catch
		{
			text = "";
			text2 = "";
			text3 = "";
			value = "";
		}
		LoadUnnamedProfilesIfNeeded();
		lock (_unnamedProfilesLock)
		{
			if (_unnamedProfiles != null && _unnamedProfiles.TryGetValue(unnamedKey, out var value2) && value2 != null)
			{
				string value3 = (personality = GetUnnamedProfileDescription(value2));
				background = "";
				return !string.IsNullOrWhiteSpace(value3);
			}
			if (!string.IsNullOrEmpty(text3) && _unnamedProfiles != null && _unnamedProfiles.TryGetValue(text3, out var value4) && value4 != null)
			{
				_unnamedProfiles[unnamedKey] = value4;
				try
				{
					SaveUnnamedProfilesUnsafe();
				}
				catch
				{
				}
				string value5 = (personality = GetUnnamedProfileDescription(value4));
				background = "";
				return !string.IsNullOrWhiteSpace(value5);
			}
			if (!string.IsNullOrEmpty(text) && text != unnamedKey && _unnamedProfiles != null && _unnamedProfiles.TryGetValue(text, out var value6) && value6 != null)
			{
				_unnamedProfiles[unnamedKey] = value6;
				_unnamedProfiles.Remove(text);
				try
				{
					SaveUnnamedProfilesUnsafe();
				}
				catch
				{
				}
				string value7 = (personality = GetUnnamedProfileDescription(value6));
				background = "";
				return !string.IsNullOrWhiteSpace(value7);
			}
			if (!string.IsNullOrEmpty(value) && _unnamedProfiles != null)
			{
				UnnamedNpcPersonaProfile unnamedNpcPersonaProfile = null;
				foreach (KeyValuePair<string, UnnamedNpcPersonaProfile> unnamedProfile in _unnamedProfiles)
				{
					if (!string.IsNullOrEmpty(unnamedProfile.Key) && unnamedProfile.Value != null && unnamedProfile.Key.StartsWith(value, StringComparison.OrdinalIgnoreCase))
					{
						if (unnamedNpcPersonaProfile == null)
						{
							unnamedNpcPersonaProfile = unnamedProfile.Value;
						}
						if (!string.IsNullOrWhiteSpace(GetUnnamedProfileDescription(unnamedProfile.Value)))
						{
							unnamedNpcPersonaProfile = unnamedProfile.Value;
							break;
						}
					}
				}
				if (unnamedNpcPersonaProfile != null)
				{
					_unnamedProfiles[unnamedKey] = unnamedNpcPersonaProfile;
					try
					{
						SaveUnnamedProfilesUnsafe();
					}
					catch
					{
					}
					string value8 = (personality = GetUnnamedProfileDescription(unnamedNpcPersonaProfile));
					background = "";
					return !string.IsNullOrWhiteSpace(value8);
				}
			}
			if (!string.IsNullOrEmpty(text2) && _unnamedProfiles != null && _unnamedProfiles.TryGetValue(text2, out var value9) && value9 != null)
			{
				_unnamedProfiles[unnamedKey] = value9;
				_unnamedProfiles.Remove(text2);
				try
				{
					SaveUnnamedProfilesUnsafe();
				}
				catch
				{
				}
				string value10 = (personality = GetUnnamedProfileDescription(value9));
				background = "";
				return !string.IsNullOrWhiteSpace(value10);
			}
			if (!string.IsNullOrEmpty(unnamedKey) && !string.IsNullOrEmpty(text) && unnamedKey != text && _unnamedProfiles != null && _unnamedProfiles.TryGetValue(text, out var value11) && value11 != null)
			{
				_unnamedProfiles[unnamedKey] = value11;
				_unnamedProfiles.Remove(text);
				try
				{
					SaveUnnamedProfilesUnsafe();
				}
				catch
				{
				}
				string value12 = (personality = GetUnnamedProfileDescription(value11));
				background = "";
				return !string.IsNullOrWhiteSpace(value12);
			}
		}
		return false;
	}

	private static string NormalizePersonaResponse(string text)
	{
		string text2 = (text ?? "").Trim();
		if (text2.StartsWith("```"))
		{
			int num = text2.IndexOf('\n');
			if (num >= 0)
			{
				text2 = text2.Substring(num + 1);
			}
			text2 = text2.Trim();
			int num2 = text2.LastIndexOf("```");
			if (num2 >= 0)
			{
				text2 = text2.Substring(0, num2).Trim();
			}
		}
		return text2;
	}

	private static bool TryParsePersonaJsonStrict(string raw, out string personality, out string background)
	{
		personality = "";
		background = "";
		try
		{
			JObject jObject = JObject.Parse(raw);
			string text = (jObject["profile"] ?? jObject["Profile"] ?? jObject["desc"] ?? jObject["Desc"] ?? jObject["description"] ?? jObject["Description"])?.ToString() ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				personality = text;
				background = "";
				return true;
			}
			personality = (jObject["personality"] ?? jObject["Personality"])?.ToString() ?? "";
			background = (jObject["background"] ?? jObject["Background"])?.ToString() ?? "";
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TrySplitNamePrefixedLineSafely(string text, out string prefix, out string rest, int maxPrefixLength = 30)
	{
		prefix = "";
		rest = "";
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		Match match = Regex.Match(text2, "^(?<prefix>[^\\[\\]\\r\\n:\\uFF1A]{1," + maxPrefixLength + "})[:\\uFF1A](?<rest>.+)$");
		if (!match.Success)
		{
			return false;
		}
		string text3 = (match.Groups["prefix"].Value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text3) || text3.IndexOfAny(new char[8] { '\u3002', '\uFF0C', '\u3001', '\uFF1B', '.', ',', '!', '?' }) >= 0)
		{
			return false;
		}
		string text4 = (match.Groups["rest"].Value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text4))
		{
			return false;
		}
		prefix = text3;
		rest = text4;
		return true;
	}
	public static string StripNamePrefixedLineSafely(string text, int maxPrefixLength = 30)
	{
		return TrySplitNamePrefixedLineSafely(text, out var _, out var rest, maxPrefixLength) ? rest : ((text ?? "").Trim());
	}

	public static string StripConversationMetadataPrefix(string text)
	{
		string result = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(result))
		{
			return result;
		}
		string pattern = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0733");
		for (int i = 0; i < 3; i++)
		{
			string stripped = Regex.Replace(result, pattern, "", RegexOptions.CultureInvariant);
			if (string.Equals(stripped, result, StringComparison.Ordinal))
			{
				break;
			}
			result = stripped.Trim();
			if (string.IsNullOrWhiteSpace(result))
			{
				return "";
			}
		}
		return result;
	}

	public static string StripKnownSpeakerMetadataPrefix(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		string[] array = new string[3]
		{
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0734"),
			"Name:",
			ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0735")
		};
		foreach (string text3 in array)
		{
			if (!text2.StartsWith(text3, StringComparison.Ordinal))
			{
				continue;
			}
			int num = text2.IndexOf(':');
			if (num >= 0)
			{
				return text2.Substring(num + 1).Trim();
			}
			break;
		}
		return text2;
	}

	private static string UnescapeJsonStringLoose(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c != '\\')
				{
					stringBuilder.Append(c);
					continue;
				}
				if (i + 1 >= s.Length)
				{
					break;
				}
				char c2 = s[++i];
				switch (c2)
				{
				case 'n':
					stringBuilder.Append('\n');
					continue;
				case 'r':
					stringBuilder.Append('\r');
					continue;
				case 't':
					stringBuilder.Append('\t');
					continue;
				case '\\':
					stringBuilder.Append('\\');
					continue;
				case '"':
					stringBuilder.Append('"');
					continue;
				case 'u':
					if (i + 4 < s.Length)
					{
						string s2 = s.Substring(i + 1, 4);
						if (int.TryParse(s2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
						{
							stringBuilder.Append((char)result);
							i += 4;
						}
						continue;
					}
					break;
				}
				stringBuilder.Append(c2);
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return s;
		}
	}

	private static string ExtractJsonValueLoose(string text, string key)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key))
		{
			return "";
		}
		int num = text.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			num = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
		}
		if (num < 0)
		{
			return "";
		}
		int num2 = text.IndexOf(':', num);
		if (num2 < 0)
		{
			return "";
		}
		int i;
		for (i = num2 + 1; i < text.Length && char.IsWhiteSpace(text[i]); i++)
		{
		}
		if (i >= text.Length)
		{
			return "";
		}
		if (text[i] == '"')
		{
			i++;
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			while (i < text.Length)
			{
				char c = text[i++];
				if (!flag)
				{
					switch (c)
					{
					case '\\':
						flag = true;
						stringBuilder.Append('\\');
						continue;
					default:
						stringBuilder.Append(c);
						continue;
					case '"':
						break;
					}
					break;
				}
				flag = false;
				stringBuilder.Append(c);
			}
			return UnescapeJsonStringLoose(stringBuilder.ToString()).Trim();
		}
		int j;
		for (j = i; j < text.Length; j++)
		{
			char c2 = text[j];
			if (c2 == '\n' || c2 == '\r' || c2 == ',' || c2 == '}')
			{
				break;
			}
		}
		return (text.Substring(i, Math.Max(0, j - i)) ?? "").Trim().Trim('"');
	}

	private static bool TryParsePersonaJson(string text, out string personality, out string background)
	{
		personality = "";
		background = "";
		string text2 = NormalizePersonaResponse(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		string text3 = text2;
		int num = text3.IndexOf('{');
		if (num >= 0)
		{
			text3 = text3.Substring(num);
		}
		int num2 = text3.LastIndexOf('}');
		if (num2 > 0)
		{
			string raw = text3.Substring(0, num2 + 1);
			if (TryParsePersonaJsonStrict(raw, out personality, out background))
			{
				return true;
			}
		}
		if (TryParsePersonaJsonStrict(text3, out personality, out background))
		{
			return true;
		}
		personality = ExtractJsonValueLoose(text2, "profile");
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "Profile");
		}
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "desc");
		}
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "Desc");
		}
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "description");
		}
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "Description");
		}
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "personality");
		}
		if (string.IsNullOrWhiteSpace(personality))
		{
			personality = ExtractJsonValueLoose(text2, "Personality");
		}
		background = ExtractJsonValueLoose(text2, "background");
		if (string.IsNullOrWhiteSpace(background))
		{
			background = ExtractJsonValueLoose(text2, "Background");
		}
		return !string.IsNullOrWhiteSpace(personality) || !string.IsNullOrWhiteSpace(background);
	}

	public static async Task EnsureUnnamedNpcPersonaGeneratedByKeyAsync(string key, string cultureId, string rank, string name, string troopId)
	{
		string k = (key ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(k))
		{
			return;
		}
		LoadUnnamedProfilesIfNeeded();
		lock (_unnamedProfilesLock)
		{
			if (_unnamedProfiles != null && _unnamedProfiles.TryGetValue(k, out var exist) && exist != null)
			{
				string desc0 = GetUnnamedProfileDescription(exist);
				if (!string.IsNullOrWhiteSpace(desc0))
				{
					return;
				}
			}
			if (_unnamedProfilesInFlight.Contains(k))
			{
				return;
			}
			_unnamedProfilesInFlight.Add(k);
		}
		try
		{
			string c = (cultureId ?? "neutral").Trim().ToLower();
			if (string.IsNullOrEmpty(c))
			{
				c = "neutral";
			}
			string r = (rank ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(r))
			{
				r = "commoner";
			}
			string n = (name ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0736")).Trim();
			if (string.IsNullOrEmpty(n))
			{
				n = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0737");
			}
			string t = (troopId ?? "").Trim().ToLower();
			string cultureName = c;
			try
			{
				CultureObject cultureObject = ResolvePromptNameCulture(c);
				string text = (cultureObject?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					cultureName = text;
				}
			}
			catch
			{
			}
			string roleName = r;
			if (string.Equals(r, "soldier", StringComparison.OrdinalIgnoreCase))
			{
				roleName = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0738");
			}
			else if (string.Equals(r, "commoner", StringComparison.OrdinalIgnoreCase))
			{
				roleName = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0739");
			}
			string troopName = "";
			try
			{
				if (!string.IsNullOrWhiteSpace(t))
				{
					CharacterObject characterObject = MBObjectManager.Instance?.GetObjectTypeList<CharacterObject>()?.FirstOrDefault((CharacterObject x) => x != null && string.Equals((x.StringId ?? "").Trim(), t, StringComparison.OrdinalIgnoreCase));
					troopName = (characterObject?.Name?.ToString() ?? "").Trim();
				}
			}
			catch
			{
				troopName = "";
			}
			t = troopName;
			c = cultureName;
			r = roleName;
			string kingdom = "";
			try
			{
				int kIdx = k.IndexOf(":kingdom:", StringComparison.OrdinalIgnoreCase);
				if (kIdx >= 0)
				{
					kingdom = k.Substring(kIdx + ":kingdom:".Length).Trim().ToLower();
					int cut = kingdom.IndexOf(':');
					if (cut >= 0)
					{
						kingdom = kingdom.Substring(0, cut);
					}
				}
			}
			catch
			{
				kingdom = "";
			}
			if (string.IsNullOrWhiteSpace(kingdom))
			{
				try
				{
					Settlement settlement = Settlement.CurrentSettlement;
					kingdom = (settlement?.OwnerClan?.Kingdom?.StringId ?? settlement?.MapFaction?.StringId ?? "").Trim().ToLower();
				}
				catch
				{
					kingdom = "";
				}
			}
			if (string.IsNullOrWhiteSpace(kingdom))
			{
				kingdom = c;
			}
			string kingdomName = kingdom;
			string rulerName = "";
			try
			{
				Kingdom kObj = Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim().ToLower(), kingdom, StringComparison.OrdinalIgnoreCase));
				if (kObj != null)
				{
					kingdomName = (kObj.Name?.ToString() ?? kingdomName).Trim();
					rulerName = (kObj.Leader?.Name?.ToString() ?? "").Trim();
				}
			}
			catch
			{
			}
			if ((string.IsNullOrWhiteSpace(kingdomName) || string.Equals(kingdomName, kingdom, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(cultureName))
			{
				kingdomName = cultureName;
			}
			string lordId2 = "";
			string lordName = "";
			try
			{
				int lIdx = k.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
				if (lIdx >= 0)
				{
					lordId2 = k.Substring(lIdx + ":lord:".Length).Trim().ToLower();
					int cut2 = lordId2.IndexOf(':');
					if (cut2 >= 0)
					{
						lordId2 = lordId2.Substring(0, cut2);
					}
				}
				if (!string.IsNullOrWhiteSpace(lordId2))
				{
					lordName = (Hero.Find(lordId2)?.Name?.ToString() ?? "").Trim();
				}
			}
			catch
			{
				lordId2 = "";
				lordName = "";
			}
			if (string.IsNullOrWhiteSpace(lordName))
			{
				lordId2 = "";
			}
			string sys = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0740");
			StringBuilder userSb = new StringBuilder();
			userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0741"));
			userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0742") + n);
			if (!string.IsNullOrWhiteSpace(t))
			{
				userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0743") + t);
			}
			if (!string.IsNullOrWhiteSpace(c))
			{
				userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0744") + c);
			}
			if (!string.IsNullOrWhiteSpace(r))
			{
				userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0745") + r);
			}
			userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0746") + kingdomName + " (StringId=" + kingdom + ")");
			if (!string.IsNullOrWhiteSpace(rulerName))
			{
				userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0747") + rulerName);
			}
			if (!string.IsNullOrWhiteSpace(lordName))
			{
				userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0748") + lordName);
			}
			else if (!string.IsNullOrWhiteSpace(lordId2))
			{
				userSb.AppendLine(ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0749") + lordId2);
			}
			string user = userSb.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(kingdom))
			{
				user = user.Replace(" (StringId=" + kingdom + ")", "");
			}
			else
			{
				user = user.Replace(" (StringId=)", "");
			}
			List<object> messages = new List<object>
			{
				new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
				{
					["role"] = "system",
					["content"] = sys
				},
				new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
				{
					["role"] = "user",
					["content"] = user
				}
			};
			PromptPackage prompt = LegacyPromptPackageAdapter.FromLegacyMessages(messages, 5000, "legacy-main");
			TraceContext trace = new TraceContext(
				"unnamed-persona-" + (k ?? "request"),
				0,
				0,
				"single-player",
				"shared");
			LlmGenerateResult generated = await new LegacyShoutNetworkGateway().GenerateAsync(
				new LlmGenerateRequest(
					trace,
					new LlmProviderSnapshot("legacy-main", "legacy://shout-network", "legacy-main", 0, 5000),
					prompt,
					InteractionStage.MainReply),
				default).ConfigureAwait(false);
			if (generated.Status != LlmResultStatus.Succeeded)
			{
				LlmRetryPrompt.ShowLocalizedFailurePopup("{=AF_UI_Retry_UnnamedPersonaFailed}Unnamed NPC persona generation failed", "Unified LLM Gateway request failed: " + (generated.ErrorCode ?? "unknown"),
					reasonTemplate: "{=AF_UI_Retry_GatewayFailed}LLM gateway request failed: {CODE}", reasonCode: generated.ErrorCode ?? "unknown");
				return;
			}
			string rawResp = (generated.RawText ?? "").Trim();
			if (string.IsNullOrWhiteSpace(rawResp))
			{
				LlmRetryPrompt.ShowLocalizedFailurePopup("{=AF_UI_Retry_UnnamedPersonaFailed}Unnamed NPC persona generation failed", LlmRetryPrompt.BuildFailureDetail("The model response is empty; a persona cannot be generated.", ""),
					reasonTemplate: "{=AF_UI_Retry_PersonaReplyEmpty}The model reply is empty; a persona cannot be generated.", modelReply: "");
				return;
			}
			if (rawResp.Contains(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0752")) || rawResp.Contains(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0753")) || rawResp.Contains(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0754")) || rawResp.Contains(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0755")))
			{
				LlmRetryPrompt.ShowLocalizedFailurePopup("{=AF_UI_Retry_UnnamedPersonaFailed}Unnamed NPC persona generation failed", rawResp);
				return;
			}
			if (!TryParsePersonaJson(rawResp, out var genP, out var genB))
			{
				Logger.Log("UnnamedPersona", "auto_gen_parse_fail key=" + k + " resp=" + rawResp);
				LlmRetryPrompt.ShowLocalizedFailurePopup("{=AF_UI_Retry_UnnamedPersonaParseFailed}Unnamed NPC persona parsing failed", LlmRetryPrompt.BuildFailureDetail("The model response could not be parsed as persona JSON.", rawResp),
					reasonTemplate: "{=AF_UI_Retry_PersonaReplyInvalid}The model reply could not be parsed as persona JSON.", modelReply: rawResp);
				return;
			}
			string profileText = "";
			if (!string.IsNullOrWhiteSpace(genP))
			{
				profileText = genP;
			}
			else if (!string.IsNullOrWhiteSpace(genB))
			{
				profileText = genB;
			}
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null)
				{
					_unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
				}
				if (!_unnamedProfiles.TryGetValue(k, out var prof) || prof == null)
				{
					prof = new UnnamedNpcPersonaProfile();
				}
				prof.CultureId = c;
				prof.Rank = r;
				prof.Name = n;
				prof.TroopId = t;
				if (string.IsNullOrWhiteSpace(prof.Description))
				{
					prof.Description = profileText;
				}
				if (string.IsNullOrWhiteSpace(prof.Personality))
				{
					prof.Personality = profileText;
				}
				_unnamedProfiles[k] = prof;
				SaveUnnamedProfilesUnsafe();
			}
			try
			{
				string pPrev = (profileText ?? "").Replace("\r", "").Replace("\n", " ");
				if (pPrev.Length > 120)
				{
					pPrev = pPrev.Substring(0, 120);
				}
				Logger.Log("UnnamedPersona", "auto_gen key=" + k + " troop=" + t + " culture=" + c + " rank=" + r + " profile=" + pPrev);
			}
			catch
			{
			}
		}
		catch
		{
		}
		finally
		{
			lock (_unnamedProfilesLock)
			{
				_unnamedProfilesInFlight.Remove(k);
			}
		}
	}

	public static async Task EnsureUnnamedNpcPersonaGeneratedAsync(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		CharacterObject co = agent.Character as CharacterObject;
		if (!(co?.IsHero ?? true))
		{
			string key = GetUnnamedKey(agent);
			if (!string.IsNullOrEmpty(key))
			{
				string cultureId = (co.Culture?.StringId ?? "neutral").ToLower();
				string rank = (co.IsSoldier ? "soldier" : "commoner");
				string name = agent.Name?.ToString() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0757");
				string troopId = (co.StringId ?? "").ToLower();
				await EnsureUnnamedNpcPersonaGeneratedByKeyAsync(key, cultureId, rank, name, troopId);
			}
		}
	}

	public static bool IsInValidScene()
	{
		Mission mission = Mission.Current;
		if (mission == null)
		{
			return false;
		}
		try
		{
			if (DuelBehavior.IsArenaMissionActive)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive && DuelBehavior.IsDuelEnded)
			{
				return true;
			}
		}
		catch
		{
		}
		string text = mission.SceneName?.ToLower() ?? "";
		if (text.Contains("arena"))
		{
			return true;
		}
		return true;
	}

	public static string GetCurrentSceneDescription()
	{
		Mission mission = Mission.Current;
		if (mission == null)
		{
			if (TryBuildMapSeaSceneDescription(out var mapSeaDescriptionWithoutMission))
			{
				return mapSeaDescriptionWithoutMission;
			}
			if (TryBuildMapLandSceneDescription(out var mapLandDescriptionWithoutMission))
			{
				return mapLandDescriptionWithoutMission;
			}
			return ConversationRuntimeTexts.Prompt("AF_CONV_Location_0758");
		}
		string text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0759");
		string text2 = "";
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		string text3 = "";
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
			{
				string text4 = (LordEncounterBehavior.EncounterMeetingLocationInfoOverride ?? "").Replace("\r", "").Trim();
				if (!string.IsNullOrEmpty(text4))
				{
					string text5 = text4;
					int num = text5.IndexOf('。');
					if (num >= 0)
					{
						text5 = text5.Substring(0, num + 1);
						text3 = text4.Substring(num + 1).Replace("\n", " ").Trim();
						text3 = text3.Trim('。', '.', ' ');
					}
					if (text5.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0760"), StringComparison.Ordinal))
					{
						text2 = text5.Substring(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0761").Length).Trim();
						text2 = text2.TrimEnd('。', '.', ' ');
						flag = false;
					}
					else if (text5.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0762"), StringComparison.Ordinal))
					{
						string text6 = text5.Substring(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0763").Length).Trim();
						text6 = text6.TrimEnd('。', '.', ' ');
						string seaSuffix = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0764");
						if (text6.EndsWith(seaSuffix, StringComparison.Ordinal))
						{
							text2 = text6.Substring(0, text6.Length - seaSuffix.Length).Trim();
							flag = true;
							flag2 = true;
							flag3 = true;
						}
						else if (string.Equals(text6, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0765"), StringComparison.Ordinal))
						{
							text2 = "";
							flag = false;
							flag2 = true;
							flag3 = true;
						}
					}
					else if (text5.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0766"), StringComparison.Ordinal))
					{
						text2 = text5.Substring(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0767").Length).Trim();
						text2 = text2.TrimEnd('。', '.', ' ');
						flag = true;
						flag2 = true;
					}
					else if (text5 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0768") || text5 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0769"))
					{
						text2 = "";
						flag = false;
						flag2 = true;
					}
				}
			}
		}
		catch
		{
		}
		if (!flag3 && TryBuildMapSeaSceneDescription(out var mapSeaDescription))
		{
			return mapSeaDescription;
		}
		if (!flag2 && string.IsNullOrEmpty(text2) && TryBuildMapLandSceneDescription(out var mapLandDescription))
		{
			return mapLandDescription;
		}
		if (flag3)
		{
			string text6 = string.IsNullOrEmpty(text2) ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0770") : (ConversationRuntimeTexts.Prompt("AF_CONV_Location_0771") + text2 + ConversationRuntimeTexts.Prompt("AF_CONV_Location_0772"));
			if (!string.IsNullOrEmpty(text3))
			{
				return text6 + " | " + text3;
			}
			return text6;
		}
		if (string.IsNullOrEmpty(text2))
		{
			try
			{
				if (MobileParty.MainParty?.CurrentSettlement != null)
				{
					text2 = MobileParty.MainParty.CurrentSettlement.Name.ToString();
				}
			}
			catch
			{
			}
		}
		if (flag2)
		{
			text = BuildCurrentMapLandTerrainSceneSpotLabel();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0773");
			}
		}
		else
		{
			string text6 = "";
			try
			{
				text6 = (CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text6 = "";
			}
			if (!string.IsNullOrEmpty(text6))
			{
				switch (text6)
				{
				case "lordshall":
					text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0774");
					break;
				case "tavern":
					text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0775");
					break;
				case "arena":
					text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0776");
					break;
				default:
					if (!(text6 == "dungeon"))
					{
						switch (text6)
						{
						case "alley":
							text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0777");
							break;
						case "port":
							text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0778");
							break;
						default:
							if (!(text6 == "village_center"))
							{
								text = ((!string.IsNullOrEmpty(text2)) ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0779") : ConversationRuntimeTexts.Prompt("AF_CONV_Location_0780"));
								break;
							}
							goto case "center";
						case "center":
							text = ((!string.IsNullOrEmpty(text2)) ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0781") : ConversationRuntimeTexts.Prompt("AF_CONV_Location_0782"));
							break;
						}
						break;
					}
					goto case "prison";
				case "prison":
					text = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0783");
					break;
				}
			}
			else
			{
				string text7 = mission.SceneName?.ToLower() ?? "";
				text = ((!text7.Contains("lordshall") && !text7.Contains("lord_hall") && !text7.Contains("lordhall") && !text7.Contains("lord") && !text7.Contains("keep")) ? (text7.Contains("tavern") ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0784") : (text7.Contains("arena") ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0785") : ((text7.Contains("prison") || text7.Contains("dungeon")) ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0786") : (string.IsNullOrEmpty(text2) ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0787") : ConversationRuntimeTexts.Prompt("AF_CONV_Location_0788"))))) : ConversationRuntimeTexts.Prompt("AF_CONV_Location_0789"));
			}
		}
		string text8 = string.IsNullOrEmpty(text2) ? text : ConversationRuntimeTexts.FormatPrompt(flag ? "AF_CONV_Location_Near" : "AF_CONV_Location_In", ("SETTLEMENT", text2), ("SPOT", text));
		if (!string.IsNullOrEmpty(text3))
		{
			return text8 + " | " + text3;
		}
		return text8;
	}

	private static bool TryBuildMapLandSceneDescription(out string description)
	{
		description = "";
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return false;
			}
			MobileParty party = MobileParty.MainParty;
			if (party == null || party.CurrentSettlement != null || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				return false;
			}
			string terrainLabel = BuildCurrentMapLandTerrainSceneSpotLabel();
			if (string.IsNullOrWhiteSpace(terrainLabel))
			{
				terrainLabel = ConversationRuntimeTexts.Prompt("AF_CONV_Location_0794");
			}
			Settlement settlement = MapSeaContextGuard.FindNearestSettlementForPrompt(party);
			string settlementName = MapSeaContextGuard.FormatSettlementNameWithTypeForPrompt(settlement);
			description = string.IsNullOrWhiteSpace(settlementName) ? terrainLabel : ConversationRuntimeTexts.FormatPrompt("AF_CONV_Location_Near", ("SETTLEMENT", settlementName), ("SPOT", terrainLabel));
			return !string.IsNullOrWhiteSpace(description);
		}
		catch
		{
			description = "";
			return false;
		}
	}

	private static string BuildCurrentMapLandTerrainSceneSpotLabel()
	{
		try
		{
			return MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(MobileParty.MainParty);
		}
		catch
		{
			return "";
		}
	}

	private static bool TryBuildMapSeaSceneDescription(out string description)
	{
		description = "";
		try
		{
			MobileParty party = MobileParty.MainParty;
			if (!MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				return false;
			}
			Settlement settlement = MapSeaContextGuard.FindNearestSettlementForPrompt(party) ?? FindNearestSettlementForCurrentScene();
			string settlementName = MapSeaContextGuard.FormatSettlementNameWithTypeForPrompt(settlement);
			description = string.IsNullOrWhiteSpace(settlementName) ? ConversationRuntimeTexts.Prompt("AF_CONV_Location_0797") : (ConversationRuntimeTexts.Prompt("AF_CONV_Location_0798") + settlementName + ConversationRuntimeTexts.Prompt("AF_CONV_Location_0799"));
			return true;
		}
		catch
		{
			description = "";
			return false;
		}
	}

	public static string GetNativeSettlementInfoForPrompt()
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			return GetNativeSettlementInfoForPrompt(currentSettlement);
		}
		catch
		{
			return "";
		}
	}

	public static string GetNativeSettlementInfoForPrompt(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return "";
			}
			string value = (settlement.Name?.ToString() ?? "").Trim();
			Clan ownerClan = settlement.OwnerClan;
			Hero hero = ownerClan?.Leader;
			string value2 = (hero?.Name?.ToString() ?? "").Trim();
			string value3 = (ownerClan?.Name?.ToString() ?? "").Trim();
			string text = "";
			string text2 = "";
			try
			{
				IFaction mapFaction = settlement.MapFaction;
				text = (mapFaction?.Name?.ToString() ?? "").Trim();
				string text3 = "";
				try
				{
					text3 = ((mapFaction?.Culture)?.StringId ?? "").Trim();
					if (hero != null && hero.IsFemale)
					{
						text3 += "_f";
					}
				}
				catch
				{
					text3 = "";
				}
				text2 = ((mapFaction == null || !mapFaction.IsKingdomFaction || hero == null || mapFaction.Leader != hero) ? GameTexts.FindText("str_faction_official", text3)?.ToString() : GameTexts.FindText("str_faction_ruler", text3)?.ToString());
				text2 = (text2 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			}
			catch
			{
				text = (text ?? "").Trim();
				text2 = (text2 ?? "").Trim();
			}
			string text4 = "";
			try
			{
				string variation = settlement.SettlementComponent?.GetProsperityLevel().ToString() ?? "0";
				if (settlement.IsTown)
				{
					text4 = GameTexts.FindText("str_town_long_prosperity_1", variation)?.ToString();
				}
				else if (settlement.IsVillage)
				{
					text4 = GameTexts.FindText("str_village_long_prosperity", variation)?.ToString();
				}
			}
			catch
			{
				text4 = "";
			}
			text4 = (text4 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			string text5 = "";
			try
			{
				if (settlement.IsTown)
				{
					Town town = settlement.Town;
					SettlementComponent settlementComponent = settlement.SettlementComponent;
					if (town != null && settlementComponent != null)
					{
						float loyalty = town.Loyalty;
						SettlementComponent.ProsperityLevel prosperityLevel = settlementComponent.GetProsperityLevel();
						string id = ((loyalty < 25f) ? ((prosperityLevel <= SettlementComponent.ProsperityLevel.Low) ? "str_settlement_morale_rebellious_adversity" : ((prosperityLevel > SettlementComponent.ProsperityLevel.Mid) ? "str_settlement_morale_rebellious_prosperity" : "str_settlement_morale_rebellious_average")) : ((loyalty < 65f) ? ((prosperityLevel > SettlementComponent.ProsperityLevel.Mid) ? "str_settlement_morale_medium_prosperity" : "str_settlement_morale_medium_average") : ((prosperityLevel <= SettlementComponent.ProsperityLevel.Low) ? "str_settlement_morale_high_adversity" : ((prosperityLevel > SettlementComponent.ProsperityLevel.Mid) ? "str_settlement_morale_high_prosperity" : "str_settlement_morale_high_average"))));
						text5 = GameTexts.FindText(id)?.ToString();
					}
				}
			}
			catch
			{
				text5 = "";
			}
			text5 = (text5 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(value))
			{
				if (settlement.IsTown)
				{
					if (ownerClan == Clan.PlayerClan)
					{
						stringBuilder.Append(value);
						stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0800"));
					}
					else
					{
						stringBuilder.Append(value);
						if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(value2))
						{
							stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0801"));
							stringBuilder.Append(text);
							stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0802"));
							stringBuilder.Append(text2);
							stringBuilder.Append("，");
							stringBuilder.Append(value2);
							stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0803"));
						}
						else if (!string.IsNullOrWhiteSpace(value2))
						{
							stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0804"));
							stringBuilder.Append(value2);
							stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0805"));
						}
						else
						{
							stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0806"));
						}
					}
				}
				else
				{
					stringBuilder.Append(value);
					if (!string.IsNullOrWhiteSpace(value2))
					{
						stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0807"));
						stringBuilder.Append(value2);
						stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0808"));
					}
					else
					{
						stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0809"));
					}
				}
			}
			if (!string.IsNullOrWhiteSpace(value3))
			{
				stringBuilder.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0810"));
				stringBuilder.Append(value3);
				stringBuilder.Append("。 ");
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				stringBuilder.Append(text4);
				if (!text4.EndsWith("。", StringComparison.Ordinal))
				{
					stringBuilder.Append("。");
				}
				stringBuilder.Append(" ");
			}
			if (!string.IsNullOrWhiteSpace(text5))
			{
				stringBuilder.Append(text5);
			}
			return stringBuilder.ToString().Replace("\r", "").Replace("\n", " ")
				.Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetHeroTypeForPrompt(Hero hero, bool fromNotables)
	{
		if (hero == null)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0811");
		}
		try
		{
			if (hero.IsLord || hero.Occupation == Occupation.Lord)
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0812");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsNotable || fromNotables || hero.Occupation == Occupation.Headman || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.Preacher)
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0813");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsWanderer || hero.Occupation == Occupation.Wanderer)
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0814");
			}
		}
		catch
		{
		}
		return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0815");
	}

	private static int GetHeroTypePriorityForPrompt(string type)
	{
		switch ((type ?? "").Trim())
		{
		case var conversationMatch816 when conversationMatch816 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0816"):
			return 4;
		case var conversationMatch817 when conversationMatch817 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0817"):
			return 3;
		case var conversationMatch818 when conversationMatch818 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0818"):
			return 2;
		default:
			return 1;
		}
	}

	private static bool IsHeroInSettlementForPrompt(Hero hero, Settlement settlement)
	{
		try
		{
			if (hero == null || settlement == null)
			{
				return false;
			}
			if (hero.CurrentSettlement == settlement)
			{
				return true;
			}
			if (hero.PartyBelongedTo?.CurrentSettlement == settlement)
			{
				return true;
			}
			try
			{
				MBReadOnlyList<Hero> heroesWithoutParty = settlement.HeroesWithoutParty;
				if (heroesWithoutParty != null)
				{
					string text = (hero.StringId ?? "").Trim();
					foreach (Hero item in heroesWithoutParty)
					{
						if (item == null)
						{
							continue;
						}
						if (item == hero)
						{
							return true;
						}
						string text2 = (item.StringId ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
						{
							return true;
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static string ProjectSettlementHeroTypeForPrompt(string type)
	{
		if (type == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0812")) return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleLord");
		if (type == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0813")) return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleNotable");
		if (type == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0814")) return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleWanderer");
		return ConversationRuntimeTexts.Prompt("SettlementHeroNpcRoleHero");
	}

	public static string BuildCurrentSettlementHeroNpcLineForPrompt(Settlement settlement = null, int maxCount = 12, int maxLen = 320)
	{
		try
		{
			settlement = settlement ?? Settlement.CurrentSettlement;
			if (settlement == null)
			{
				return "";
			}
			if (maxCount <= 0)
			{
				maxCount = 12;
			}
			if (maxCount > 30)
			{
				maxCount = 30;
			}
			if (maxLen <= 0)
			{
				maxLen = 320;
			}
			List<string> list = new List<string>();
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Action<Hero, bool> action = delegate(Hero hero, bool fromNotables)
			{
				if (hero == null)
				{
					return;
				}
				string text = (hero.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					return;
				}
				string text2 = (hero.StringId ?? "").Trim();
				string text3 = (string.IsNullOrWhiteSpace(text2) ? ("name:" + text) : ("id:" + text2.ToLowerInvariant()));
				string heroTypeForPrompt = GetHeroTypeForPrompt(hero, fromNotables);
				if (!dictionary.ContainsKey(text3))
				{
					dictionary[text3] = text;
					dictionary2[text3] = heroTypeForPrompt;
					list.Add(text3);
				}
				else if (GetHeroTypePriorityForPrompt(heroTypeForPrompt) > GetHeroTypePriorityForPrompt(dictionary2[text3]))
				{
					dictionary2[text3] = heroTypeForPrompt;
				}
			};
			try
			{
				MBReadOnlyList<Hero> heroesWithoutParty = settlement.HeroesWithoutParty;
				if (heroesWithoutParty != null)
				{
					foreach (Hero item in heroesWithoutParty)
					{
						action(item, item?.IsNotable ?? false);
					}
				}
			}
			catch
			{
			}
			Hero leader = settlement.OwnerClan?.Leader;
			if (IsHeroInSettlementForPrompt(leader, settlement))
			{
				action(leader, arg2: false);
			}
			Hero governor = settlement.Town?.Governor;
			if (IsHeroInSettlementForPrompt(governor, settlement))
			{
				action(governor, arg2: false);
			}
			try
			{
				MBReadOnlyList<Hero> notables = settlement.Notables;
				if (notables != null)
				{
					foreach (Hero item in notables)
					{
						if (IsHeroInSettlementForPrompt(item, settlement))
						{
							action(item, arg2: true);
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count <= 0)
			{
				return "";
			}
			int count = list.Count;
			List<string> list2 = new List<string>();
			for (int i = 0; i < list.Count && i < maxCount; i++)
			{
				string text4 = list[i];
				string value = dictionary[text4];
				string value2 = dictionary2[text4];
				list2.Add("[" + ProjectSettlementHeroTypeForPrompt(value2) + "]" + value);
			}
			string text5 = ConversationRuntimeTexts.Prompt("SettlementHeroNpcPrefix") + string.Join("；", list2);
			if (count > maxCount)
			{
				text5 = text5 + ConversationRuntimeTexts.Prompt("SettlementHeroNpcTotalPrefix") + count + ConversationRuntimeTexts.Prompt("SettlementHeroNpcTotalSuffix");
			}
			text5 = text5.Replace("\r", "").Replace("\n", " ").Trim();
			if (text5.Length > maxLen)
			{
				text5 = text5.Substring(0, maxLen) + "…";
			}
			return text5;
		}
		catch
		{
			return "";
		}
	}

	public static string BuildNearbySettlementsDetailForPrompt(CampaignVec2 origin, Hero perspectiveHero = null)
	{
		try
		{
			if (!origin.IsValid())
			{
				return "";
			}
			Settlement st = null;
			Settlement st2 = null;
			Settlement st3 = null;
			Settlement st4 = null;
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			float num3 = float.MaxValue;
			float num4 = float.MaxValue;
			Vec2 vec = origin.ToVec2();
			foreach (Settlement item in Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				string value = (item.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrEmpty(value))
				{
					continue;
				}
				Vec2 vec2 = item.GatePosition.ToVec2();
				float num5 = vec2.x - vec.x;
				float num6 = vec2.y - vec.y;
				float num7 = num5 * num5 + num6 * num6;
				if (num7 < 0.0001f)
				{
					continue;
				}
				if (Math.Abs(num5) >= Math.Abs(num6))
				{
					if (num5 > 0f)
					{
						if (num7 < num2)
						{
							num2 = num7;
							st2 = item;
						}
					}
					else if (num7 < num4)
					{
						num4 = num7;
						st4 = item;
					}
				}
				else if (num6 > 0f)
				{
					if (num7 < num)
					{
						num = num7;
						st = item;
					}
				}
				else if (num7 < num3)
				{
					num3 = num7;
					st3 = item;
				}
			}
			List<string> list = new List<string>();
			AppendDirLine(list, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0822"), st, num, perspectiveHero);
			AppendDirLine(list, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0823"), st2, num2, perspectiveHero);
			AppendDirLine(list, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0824"), st3, num3, perspectiveHero);
			AppendDirLine(list, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0825"), st4, num4, perspectiveHero);
			if (list.Count == 0)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0826"));
			foreach (string item2 in list)
			{
				stringBuilder.AppendLine(item2);
			}
			return stringBuilder.ToString().Replace("\r", "").Trim();
		}
		catch
		{
			return "";
		}
	}

	public static string BuildCurrentSceneSettlementInlineSuffixForPrompt(Hero perspectiveHero = null)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? FindNearestSettlementForCurrentScene();
			if (settlement == null)
			{
				return "";
			}
			return BuildCurrentSceneSettlementInlineSuffixForPrompt(settlement, perspectiveHero);
		}
		catch
		{
			return "";
		}
	}

	public static string BuildCurrentSceneSettlementInlineSuffixForPrompt(Settlement settlement, Hero perspectiveHero = null)
	{
		try
		{
			if (settlement == null)
			{
				return "";
			}
			string text = (settlement.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			string text5 = (settlement.Culture?.Name?.ToString() ?? "").Trim();
			Hero hero = settlement.OwnerClan?.Leader;
			string text2 = (hero?.Name?.ToString() ?? "").Trim();
			string text3 = (settlement.OwnerClan?.Name?.ToString() ?? "").Trim();
			string text4 = (settlement.MapFaction?.Name?.ToString() ?? "").Trim();
			string factionRelationSummary = BuildFactionRelationSummary(settlement.MapFaction, perspectiveHero);
			List<string> list = new List<string>();
			if (!string.IsNullOrWhiteSpace(text5))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0827") + text5 + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0828"));
			}
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0829") + text2 + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0830") + text3 + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0831"));
			}
			else if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0832") + text2);
			}
			else if (!string.IsNullOrWhiteSpace(text3))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0833") + text3 + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0834"));
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0835") + text4);
			}
			if (!string.IsNullOrWhiteSpace(factionRelationSummary))
			{
				list.Add(factionRelationSummary);
			}
			if ((settlement.IsTown || settlement.IsCastle) && hero != null && !IsHeroInSettlementForPrompt(hero, settlement))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0836"));
			}
			return list.Count <= 0 ? "" : ("；" + string.Join("，", list));
		}
		catch
		{
			return "";
		}
	}

	private static Settlement FindNearestSettlementForCurrentScene()
	{
		try
		{
			CampaignVec2? campaignVec = MobileParty.MainParty?.Position;
			if (!campaignVec.HasValue || !campaignVec.Value.IsValid())
			{
				return null;
			}
			Vec2 vec = campaignVec.Value.ToVec2();
			Settlement settlement = null;
			float num = float.MaxValue;
			foreach (Settlement item in Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				Vec2 vec2 = item.GatePosition.ToVec2();
				float num2 = vec2.x - vec.x;
				float num3 = vec2.y - vec.y;
				float num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					settlement = item;
				}
			}
			return settlement;
		}
		catch
		{
			return null;
		}
	}

	private static string GetSettlementTypeText(Settlement settlement)
	{
		return settlement.IsTown ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0837") : (settlement.IsCastle ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0838") : (settlement.IsVillage ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0839") : ((!settlement.IsFortification) ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0840") : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0841"))));
	}

	private static string BuildFactionRelationSummary(IFaction faction, Hero perspectiveHero)
	{
		try
		{
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0842");
			}
			string playerRelation = GetFactionRelationLabel(faction, Hero.MainHero?.Clan?.Kingdom ?? Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.Kingdom ?? Clan.PlayerClan?.MapFaction);
			string npcRelation = GetFactionRelationLabel(faction, perspectiveHero?.Clan?.Kingdom ?? perspectiveHero?.MapFaction);
			if (string.IsNullOrWhiteSpace(npcRelation))
			{
				return BuildSingleRelationPhrase(text, playerRelation);
			}
			if (string.Equals(playerRelation, npcRelation, StringComparison.Ordinal))
			{
				return BuildSharedRelationPhrase(playerRelation, text);
			}
			return BuildSplitRelationPhrase(npcRelation, playerRelation, text);
		}
		catch
		{
			string text2 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0843");
			}
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0844") + text2 + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0845");
		}
	}

	private static string BuildSingleRelationPhrase(string targetName, string relation)
	{
		switch ((relation ?? "").Trim())
		{
		case var conversationMatch846 when conversationMatch846 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0846"):
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0847") + targetName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0848");
		case var conversationMatch849 when conversationMatch849 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0849"):
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0850") + targetName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0851");
		default:
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0852") + targetName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0853");
		}
	}

	private static string BuildSharedRelationPhrase(string relation, string playerName)
	{
		string text = string.IsNullOrWhiteSpace(playerName) ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0854") : playerName;
		switch ((relation ?? "").Trim())
		{
		case var conversationMatch855 when conversationMatch855 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0855"):
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0856") + text + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0857");
		case var conversationMatch858 when conversationMatch858 == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0858"):
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0859") + text + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0860");
		default:
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0861") + text + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0862");
		}
	}

	private static string BuildSplitRelationPhrase(string npcRelation, string playerRelation, string playerName)
	{
		string text = string.IsNullOrWhiteSpace(playerName) ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0863") : playerName;
		return BuildSingleRelationPhrase(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0864"), npcRelation) + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0865") + BuildSingleRelationPhrase(text, playerRelation);
	}

	private static string GetFactionRelationLabel(IFaction faction, IFaction referenceFaction)
	{
		try
		{
			if (faction == null || referenceFaction == null)
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0866");
			}
			string text = (faction.StringId ?? "").Trim();
			string text2 = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0867");
			}
			if (ReferenceEquals(faction, referenceFaction))
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0868");
			}
			if (referenceFaction.IsAtWarWith(faction) || faction.IsAtWarWith(referenceFaction))
			{
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0869");
			}
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0870");
		}
		catch
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0871");
		}
	}

	private static void AppendDirLine(List<string> lines, string dir, Settlement st, float distanceSquared, Hero perspectiveHero = null)
	{
		if (st == null)
		{
			return;
		}
		string text = BuildSettlementStatusLineForPrompt(st, perspectiveHero);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = "";
		try
		{
			if (distanceSquared > 0f && distanceSquared < float.MaxValue)
			{
				float num = MathF.Sqrt(distanceSquared);
				if (num > 0.001f)
				{
					text2 = string.Format(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0872"), num);
				}
			}
		}
		catch
		{
			text2 = "";
		}
		lines.Add(dir + "：" + text + text2);
	}

	private static string BuildSettlementStatusLineForPrompt(Settlement settlement, Hero perspectiveHero = null)
	{
		try
		{
			if (settlement == null)
			{
				return "";
			}
			string text = (settlement.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			string item = GetSettlementTypeText(settlement);
			string cultureName = (settlement.Culture?.Name?.ToString() ?? "").Trim();
			string text2 = (settlement.MapFaction?.Name?.ToString() ?? "").Trim();
			string text3 = (settlement.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			string text4 = (settlement.OwnerClan?.Name?.ToString() ?? "").Trim();
			string factionRelationSummary = BuildFactionRelationSummary(settlement.MapFaction, perspectiveHero);
			List<string> list = new List<string>();
			list.Add(item);
			if (!string.IsNullOrEmpty(cultureName))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0873") + cultureName);
			}
			if (!string.IsNullOrEmpty(text3))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0874") + text3);
			}
			if (!string.IsNullOrEmpty(text4))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0875") + text4);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0876") + text2);
			}
			if (!string.IsNullOrEmpty(factionRelationSummary))
			{
				list.Add(factionRelationSummary);
			}
			string text5 = text + "（" + string.Join("，", list) + "）";
			string nativeSettlementInfoForPrompt = GetNativeSettlementInfoForPrompt(settlement);
			if (string.IsNullOrWhiteSpace(nativeSettlementInfoForPrompt))
			{
				return text5;
			}
			return text5 + "；" + nativeSettlementInfoForPrompt;
		}
		catch
		{
			return "";
		}
	}

	public static List<Agent> GetNearbyNPCAgents()
	{
		return GetNearbyNPCAgentsLegacy(4f, 0.7853982f);
	}

	private static Vec3 GetShoutLineOfSightPoint(Agent agent, bool lowerBodyPoint = false)
	{
		if (agent == null)
		{
			return Vec3.Invalid;
		}
		try
		{
			if (!lowerBodyPoint && agent.AgentVisuals != null)
			{
				Vec3 eyePoint = agent.AgentVisuals.GetGlobalStableEyePoint(true);
				if (IsValidShoutLineOfSightPoint(eyePoint))
				{
					return eyePoint;
				}
			}
		}
		catch
		{
		}
		Vec3 position = agent.Position;
		position.z += lowerBodyPoint ? ShoutLineOfSightLowerBodyHeight : ShoutLineOfSightFallbackEyeHeight;
		return position;
	}

	private static bool IsValidShoutLineOfSightPoint(Vec3 point)
	{
		return point.IsValid && !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z) && !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z);
	}

	private static bool CanScenePointSeePoint(Scene scene, Vec3 source, Vec3 target, bool failClosed = false)
	{
		if (scene == null || !IsValidShoutLineOfSightPoint(source) || !IsValidShoutLineOfSightPoint(target))
		{
			return false;
		}
		try
		{
			float distance = source.Distance(target);
			if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0.05f)
			{
				return true;
			}
			return scene.CheckPointCanSeePoint(source, target, distance);
		}
		catch
		{
			return !failClosed;
		}
	}

	public static bool HasShoutLineOfSightToMainAgent(Agent targetAgent)
	{
		return HasShoutLineOfSightBetweenAgents(Agent.Main, targetAgent);
	}

	public static bool HasShoutLineOfSightBetweenAgents(Agent sourceAgent, Agent targetAgent, bool failClosed = false)
	{
		if (sourceAgent == null || targetAgent == null || !sourceAgent.IsActive() || !targetAgent.IsActive())
		{
			return false;
		}
		Scene scene = Mission.Current?.Scene;
		if (scene == null)
		{
			return !failClosed;
		}
		Vec3 sourceEye = GetShoutLineOfSightPoint(sourceAgent);
		Vec3 targetEye = GetShoutLineOfSightPoint(targetAgent);
		if (CanScenePointSeePoint(scene, sourceEye, targetEye, failClosed))
		{
			return true;
		}
		Vec3 sourceLower = GetShoutLineOfSightPoint(sourceAgent, lowerBodyPoint: true);
		Vec3 targetLower = GetShoutLineOfSightPoint(targetAgent, lowerBodyPoint: true);
		return CanScenePointSeePoint(scene, sourceLower, targetLower, failClosed);
	}

	public static bool HasShoutLineOfSightFromFixedAnchor(Vec3 sourcePosition, Agent targetAgent)
	{
		if (!IsValidShoutLineOfSightPoint(sourcePosition) || targetAgent == null || !targetAgent.IsActive())
		{
			return false;
		}
		Scene scene = Mission.Current?.Scene;
		if (scene == null)
		{
			return false;
		}
		Vec3 sourceEye = sourcePosition;
		sourceEye.z += ShoutLineOfSightFallbackEyeHeight;
		Vec3 targetEye = GetShoutLineOfSightPoint(targetAgent);
		if (CanScenePointSeePoint(scene, sourceEye, targetEye, failClosed: true))
		{
			return true;
		}
		Vec3 sourceLower = sourcePosition;
		sourceLower.z += ShoutLineOfSightLowerBodyHeight;
		Vec3 targetLower = GetShoutLineOfSightPoint(targetAgent, lowerBodyPoint: true);
		return CanScenePointSeePoint(scene, sourceLower, targetLower, failClosed: true);
	}

	private static List<Agent> GetNearbyNPCAgentsLegacy(float maxDistance, float halfAngleRadians)
	{
		List<Agent> list = new List<Agent>();
		Mission mission = Mission.Current;
		Agent mainAgent = Agent.Main;
		var agents = mission?.Agents;
		if (agents == null || mainAgent == null)
		{
			return list;
		}
		float num = Math.Max(0.1f, maxDistance);
		float num2 = Math.Max(0f, Math.Min((float)Math.PI, halfAngleRadians));
		float num3 = (float)Math.Cos(num2);
		Vec3 position = mainAgent.Position;
		Vec3 lookDirection = mainAgent.LookDirection;
		foreach (Agent agent in agents)
		{
			if (agent == mainAgent || !agent.IsActive() || !agent.IsHuman || RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent))
			{
				continue;
			}
			float num4 = agent.Position.Distance(position);
			if (num4 <= num)
			{
				Vec3 v = agent.Position - position;
				v.Normalize();
				if (Vec3.DotProduct(lookDirection, v) > num3 && HasShoutLineOfSightToMainAgent(agent))
				{
					list.Add(agent);
				}
			}
		}
		return list;
	}

	public static List<Agent> GetNearbyNPCAgents(float maxDistance, float halfAngleRadians)
	{
		List<Agent> list = new List<Agent>();
		Mission mission = Mission.Current;
		Agent mainAgent = Agent.Main;
		var agents = mission?.Agents;
		if (agents == null || mainAgent == null)
		{
			return list;
		}
		float num = Math.Max(0.1f, maxDistance);
		float num2 = Math.Max(0f, Math.Min((float)Math.PI, halfAngleRadians));
		float num3 = (float)Math.Cos(num2);
		float num4 = num * num;
		Vec2 position = mainAgent.Position.AsVec2;
		Vec2 lookDirection = mainAgent.LookDirection.AsVec2;
		if (lookDirection.LengthSquared <= 1E-05f)
		{
			return list;
		}
		lookDirection.Normalize();
		foreach (Agent agent in agents)
		{
			if (agent == mainAgent || !agent.IsActive() || !agent.IsHuman || RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent))
			{
				continue;
			}
			Vec2 v = agent.Position.AsVec2 - position;
			float distanceSquared = v.LengthSquared;
			if (distanceSquared <= num4 && distanceSquared > 1E-05f)
			{
				v.Normalize();
				if (Vec2.DotProduct(lookDirection, v) >= num3 && HasShoutLineOfSightToMainAgent(agent))
				{
					list.Add(agent);
				}
			}
		}
		return list;
	}

	public static Agent GetClosestFacingAgent(float maxDistance)
	{
		Mission mission = Mission.Current;
		Agent mainAgent = Agent.Main;
		var agents = mission?.Agents;
		if (agents == null || mainAgent == null)
		{
			return null;
		}
		Vec3 position = mainAgent.Position;
		Vec3 lookDirection = mainAgent.LookDirection;
		Agent result = null;
		float num = maxDistance;
		const float strictCrosshairDotThreshold = 0.9f;
		const float npcFront120DotThreshold = 0.5f;
		foreach (Agent agent in agents)
		{
			if (agent == mainAgent || !agent.IsActive() || !agent.IsHuman)
			{
				continue;
			}
			float num2 = agent.Position.Distance(position);
			if (num2 > maxDistance)
			{
				continue;
			}
			Vec3 toPlayer = position - agent.Position;
			toPlayer.Normalize();
			Vec3 npcLookDirection = agent.LookDirection;
			if (Vec3.DotProduct(npcLookDirection, toPlayer) < npcFront120DotThreshold)
			{
				continue;
			}
			Vec3 v = agent.Position - position;
			v.Normalize();
			if (Vec3.DotProduct(lookDirection, v) >= strictCrosshairDotThreshold && num2 < num)
			{
				num = num2;
				result = agent;
			}
		}
		return result;
	}

	public static Agent GetFacingAgent(List<Agent> agents)
	{
		if (Agent.Main == null || agents.Count == 0)
		{
			return null;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		Agent agent = null;
		float num = -1f;
		foreach (Agent agent2 in agents)
		{
			Vec3 v = agent2.Position - position;
			float length = v.Length;
			v.Normalize();
			float num2 = Vec3.DotProduct(lookDirection, v);
			if (num2 > 0.70710677f)
			{
				float num3 = num2 / (length * 0.1f + 0.1f);
				if (num3 > num)
				{
					num = num3;
					agent = agent2;
				}
			}
		}
		return agent ?? agents[0];
	}

	public static Agent GetMostCenteredAgent(List<Agent> agents)
	{
		if (Agent.Main == null || agents == null || agents.Count == 0)
		{
			return null;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		lookDirection.z = 0f;
		if (lookDirection.LengthSquared <= 1E-05f)
		{
			return null;
		}
		lookDirection.Normalize();
		Agent result = null;
		float bestDot = float.MinValue;
		float bestDistance = float.MaxValue;
		foreach (Agent agent in agents)
		{
			if (agent == null || agent == Agent.Main || !agent.IsActive() || !agent.IsHuman)
			{
				continue;
			}
			Vec3 toAgent = agent.Position - position;
			toAgent.z = 0f;
			float distance = toAgent.Length;
			if (distance <= 1E-05f)
			{
				continue;
			}
			toAgent.Normalize();
			float dot = Vec3.DotProduct(lookDirection, toAgent);
			if (dot > bestDot + 1E-05f || (Math.Abs(dot - bestDot) <= 1E-05f && distance < bestDistance))
			{
				bestDot = dot;
				bestDistance = distance;
				result = agent;
			}
		}
		return result;
	}

	public static NpcDataPacket ExtractNpcData(Agent agent)
	{
		if (agent == null)
		{
			return null;
		}
		NpcDataPacket npcDataPacket = new NpcDataPacket();
		npcDataPacket.Name = agent.Name ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0877");
		npcDataPacket.AgentIndex = agent.Index;
		npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0878");
		npcDataPacket.PersonalityDesc = "";
		npcDataPacket.BackgroundDesc = "";
		npcDataPacket.IsHero = false;
		npcDataPacket.CultureId = "neutral";
		npcDataPacket.UnnamedKey = "";
		npcDataPacket.TroopId = "";
		npcDataPacket.UnnamedRank = "";
		try
		{
			Vec3 scenePosition = agent.Position;
			npcDataPacket.HasScenePosition = scenePosition.IsValid;
			npcDataPacket.ScenePositionX = scenePosition.x;
			npcDataPacket.ScenePositionY = scenePosition.y;
			npcDataPacket.ScenePositionZ = scenePosition.z;
		}
		catch
		{
			npcDataPacket.HasScenePosition = false;
		}
		if (agent.Character is CharacterObject characterObject)
		{
			npcDataPacket.IsFemale = characterObject.IsFemale;
			try
			{
				if (characterObject.IsHero && characterObject.HeroObject != null)
				{
					npcDataPacket.Age = characterObject.HeroObject.Age;
				}
				else
				{
					npcDataPacket.Age = ResolveSceneNonHeroAge(agent, characterObject);
				}
			}
			catch
			{
				npcDataPacket.Age = 30f;
			}
			if (characterObject.Culture != null)
			{
				npcDataPacket.CultureId = characterObject.Culture.StringId.ToLower();
			}
			npcDataPacket.CultureId = ResolveSceneCultureIdWithSettlementFallback(npcDataPacket.CultureId, agent, characterObject);
			if (characterObject.IsHero)
			{
				npcDataPacket.IsHero = true;
				Hero hero = null;
				try
				{
					hero = characterObject.HeroObject;
				}
				catch
				{
				}
				if (hero != null)
				{
					if (hero.IsLord)
					{
						npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0879");
					}
					else if (hero.IsWanderer)
					{
						npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0880");
					}
					else if (hero.IsNotable)
					{
						npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0881");
					}
					else
					{
						npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0882");
					}
                    npcDataPacket.SetPersonaDescription(MyBehavior.GetNpcPersonaDescriptionForExternal(hero));
					npcDataPacket.PersonaContext = MyBehavior.GetNpcPersonaContextForExternal(hero);
				}
				else if (characterObject.Occupation == Occupation.Lord)
				{
					npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0883");
				}
				else if (characterObject.Occupation == Occupation.Wanderer)
				{
					npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0884");
				}
				else
				{
					npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0885");
				}
			}
			else if (characterObject.IsSoldier)
			{
				npcDataPacket.RoleDesc = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0886");
			}
			else
			{
				// Scene civilians all use UnnamedRank=commoner.  Preserve the
				// actual occupation so ambient dialogue can distinguish a horse
				// trader, merchant and blacksmith instead of treating them all as
				// interchangeable villagers.
				npcDataPacket.RoleDesc = ResolveUnnamedRoleDescription(characterObject);
			}
			if (!npcDataPacket.IsHero)
			{
				npcDataPacket.UnnamedKey = GetUnnamedKey(agent);
				npcDataPacket.TroopId = (characterObject.StringId ?? "").ToLower();
				try
				{
					var locationCharacter = CampaignMission.Current?.Location?.GetLocationCharacter(agent.Origin);
					string specialTargetTag = locationCharacter?.SpecialTargetTag ?? "";
					if (!string.IsNullOrWhiteSpace(specialTargetTag))
					{
						npcDataPacket.TroopId = (npcDataPacket.TroopId + " " + specialTargetTag).Trim().ToLowerInvariant();
					}
				}
				catch
				{
				}
				npcDataPacket.UnnamedRank = (characterObject.IsSoldier ? "soldier" : "commoner");
				if (TryGetUnnamedNpcPersona(agent, out var personality2, out var background2))
				{
                    npcDataPacket.SetPersonaDescription(AnimusForge.Refactor.Runtime.NpcPersonaProfilePolicy.MergeLegacy(personality2, background2));
				}
			}
		}
		npcDataPacket.CultureId = ResolveSceneCultureIdWithSettlementFallback(npcDataPacket.CultureId, agent, null);
		EnsurePromptNameFields(npcDataPacket);
		return npcDataPacket;
	}

	private static string ResolveUnnamedRoleDescription(CharacterObject characterObject)
	{
		if (characterObject == null)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0887");
		}
		try
		{
			switch (characterObject.Occupation)
			{
			case Occupation.Weaponsmith:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0888");
			case Occupation.Blacksmith:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0889");
			case Occupation.Armorer:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0890");
			case Occupation.HorseTrader:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0891");
			case Occupation.GoodsTrader:
			case Occupation.Merchant:
			case Occupation.Artisan:
			case Occupation.ShopWorker:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0892");
			case Occupation.Tavernkeeper:
			case Occupation.TavernWench:
			case Occupation.TavernGameHost:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0893");
			case Occupation.Musician:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0894");
			case Occupation.RansomBroker:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0895");
			case Occupation.Headman:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0896");
			case Occupation.Preacher:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0897");
			case Occupation.GangLeader:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0898");
			case Occupation.RuralNotable:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0899");
			case Occupation.ShipWright:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0900");
			default:
				return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0901");
			}
		}
		catch
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0902");
		}
	}

	public static void EnsurePromptNameFields(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return;
		}
		if (npc.IsHero)
		{
			npc.PromptGivenName = "";
			npc.PromptDisplayName = (npc.Name ?? "").Trim();
			return;
		}
		string promptGivenName = PickPromptGivenName(npc, null);
		npc.PromptGivenName = promptGivenName;
		npc.PromptDisplayName = BuildPromptDisplayName(npc.Name, promptGivenName);
	}

	public static void EnsureScenePromptNames(List<NpcDataPacket> allNpcData)
	{
		if (allNpcData == null || allNpcData.Count == 0)
		{
			return;
		}
		foreach (NpcDataPacket npc in allNpcData)
		{
			EnsurePromptNameFields(npc);
		}
		HashSet<string> usedGivenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcDataPacket item in allNpcData.Where((NpcDataPacket npc) => npc != null && !npc.IsHero).OrderBy((NpcDataPacket npc) => npc.AgentIndex))
		{
			string text = PickPromptGivenName(item, usedGivenNames);
			if (!string.IsNullOrWhiteSpace(text))
			{
				item.PromptGivenName = text;
			}
			item.PromptDisplayName = BuildPromptDisplayName(item.Name, item.PromptGivenName);
		}
	}

	public static string GetPromptIdentityName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "";
		}
		string text = (npc.Name ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (npc.RoleDesc ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0903") : text;
	}

	public static string GetPromptListName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "";
		}
		EnsurePromptNameFields(npc);
		if (!npc.IsHero)
		{
			string text = (npc.PromptGivenName ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return GetPromptIdentityName(npc);
	}

	public static string GetPromptHistoryName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "";
		}
		EnsurePromptNameFields(npc);
		if (!npc.IsHero)
		{
			string text = (npc.PromptDisplayName ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return GetPromptIdentityName(npc);
	}

	public static string GetPromptPatienceName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "";
		}
		EnsurePromptNameFields(npc);
		if (!npc.IsHero)
		{
			string text = (npc.PromptGivenName ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return GetPromptHistoryName(npc);
	}

	private static string BuildPromptDisplayName(string identityName, string givenName)
	{
		string text = (identityName ?? "").Trim();
		string text2 = (givenName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.IsNullOrWhiteSpace(text2) ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0904") : text2;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		if (text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		return text + text2;
	}

	private static string PickPromptGivenName(NpcDataPacket npc, HashSet<string> usedGivenNames)
	{
		if (npc == null || npc.IsHero)
		{
			return "";
		}
		List<string> promptNamePool = GetPromptNamePool(npc.CultureId, npc.IsFemale);
		if (promptNamePool.Count == 0)
		{
			string text = (npc.PromptGivenName ?? "").Trim();
			return string.IsNullOrWhiteSpace(text) ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0905") : text;
		}
		int num = ComputeStablePromptNameHash(BuildPromptNameSeed(npc));
		int num2 = num % promptNamePool.Count;
		string text2 = "";
		for (int i = 0; i < promptNamePool.Count; i++)
		{
			string text3 = (promptNamePool[(num2 + i) % promptNamePool.Count] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = text3;
			}
			if (usedGivenNames == null || usedGivenNames.Add(text3))
			{
				return text3;
			}
		}
		if (usedGivenNames != null && !string.IsNullOrWhiteSpace(text2))
		{
			usedGivenNames.Add(text2);
		}
		return text2;
	}

	private static List<string> GetPromptNamePool(string cultureId, bool isFemale)
	{
		string text = ((cultureId ?? "").Trim().ToLowerInvariant() + "|" + (isFemale ? "f" : "m")).Trim();
		lock (_promptNamePoolLock)
		{
			if (_promptNamePoolCache.TryGetValue(text, out var value))
			{
				return value;
			}
		}
		List<string> list = BuildPromptNamePool(cultureId, isFemale);
		lock (_promptNamePoolLock)
		{
			_promptNamePoolCache[text] = list;
		}
		return list;
	}

	private static List<string> BuildPromptNamePool(string cultureId, bool isFemale)
	{
		List<string> list = new List<string>();
		CultureObject cultureObject = ResolvePromptNameCulture(cultureId);
		IEnumerable<TextObject> enumerable = null;
		try
		{
			enumerable = NameGenerator.Current?.GetNameListForCulture(cultureObject, isFemale)?.ToList();
		}
		catch
		{
			enumerable = null;
		}
		if ((enumerable == null || !enumerable.Any()) && cultureObject != null)
		{
			try
			{
				enumerable = (isFemale ? cultureObject.FemaleNameList : cultureObject.MaleNameList)?.ToList();
			}
			catch
			{
				enumerable = null;
			}
		}
		if (enumerable == null)
		{
			return list;
		}
		foreach (TextObject item in enumerable)
		{
			string text = (item?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !list.Contains(text, StringComparer.OrdinalIgnoreCase))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static CultureObject ResolvePromptNameCulture(string cultureId)
	{
		string text = (cultureId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			try
			{
				CultureObject @object = MBObjectManager.Instance?.GetObject<CultureObject>(text);
				if (@object != null)
				{
					return @object;
				}
			}
			catch
			{
			}
			try
			{
				CultureObject cultureObject = MBObjectManager.Instance?.GetObjectTypeList<CultureObject>()?.FirstOrDefault((CultureObject c) => c != null && string.Equals((c.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (cultureObject != null)
				{
					return cultureObject;
				}
			}
			catch
			{
			}
		}
		try
		{
			if (Settlement.CurrentSettlement?.Culture != null)
			{
				return Settlement.CurrentSettlement.Culture;
			}
		}
		catch
		{
		}
		try
		{
			if (Hero.MainHero?.Culture != null)
			{
				return Hero.MainHero.Culture;
			}
		}
		catch
		{
		}
		try
		{
			return MBObjectManager.Instance?.GetObjectTypeList<CultureObject>()?.FirstOrDefault((CultureObject c) => c != null);
		}
		catch
		{
			return null;
		}
	}

	private static string BuildPromptNameSeed(NpcDataPacket npc)
	{
		bool flag = npc != null && npc.IsFemale;
		return ((npc?.UnnamedKey ?? "").Trim().ToLowerInvariant()) + "|" + ((npc?.TroopId ?? "").Trim().ToLowerInvariant()) + "|" + ((npc?.CultureId ?? "").Trim().ToLowerInvariant()) + "|" + (flag ? "f" : "m") + "|" + ((npc?.AgentIndex ?? 0).ToString(CultureInfo.InvariantCulture));
	}

	private static int ComputeStablePromptNameHash(string text)
	{
		unchecked
		{
			int num = 17;
			string text2 = text ?? "";
			for (int i = 0; i < text2.Length; i++)
			{
				num = num * 31 + text2[i];
			}
			return num & int.MaxValue;
		}
	}

	private static string ResolveSceneCultureIdWithSettlementFallback(string cultureId, Agent agent, CharacterObject characterObject)
	{
		string text = (cultureId ?? "").Trim().ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "neutral", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "neutral_culture", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		try
		{
			string text2 = (characterObject?.Culture?.StringId ?? agent?.Character?.Culture?.StringId ?? Settlement.CurrentSettlement?.Culture?.StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, "neutral", StringComparison.OrdinalIgnoreCase) && !string.Equals(text2, "neutral_culture", StringComparison.OrdinalIgnoreCase))
			{
				return text2;
			}
		}
		catch
		{
		}
		try
		{
			string text3 = (Settlement.CurrentSettlement?.MapFaction?.Culture?.StringId ?? Settlement.CurrentSettlement?.OwnerClan?.Culture?.StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text3) && !string.Equals(text3, "neutral", StringComparison.OrdinalIgnoreCase) && !string.Equals(text3, "neutral_culture", StringComparison.OrdinalIgnoreCase))
			{
				return text3;
			}
		}
		catch
		{
		}
		return string.IsNullOrWhiteSpace(text) ? "neutral" : text;
	}

	private static float ResolveSceneNonHeroAge(Agent agent, CharacterObject characterObject)
	{
		float num = 0f;
		try
		{
			num = agent?.Age ?? 0f;
		}
		catch
		{
			num = 0f;
		}
		if (num >= 18f && num <= 80f)
		{
			return num;
		}
		try
		{
			num = characterObject?.Age ?? 0f;
		}
		catch
		{
			num = 0f;
		}
		if (num >= 18f && num <= 55f)
		{
			return num;
		}
		float num2 = 30f;
		try
		{
			if (characterObject != null)
			{
				if (characterObject.IsSoldier)
				{
					num2 = 30f;
				}
				else
				{
					switch (characterObject.Occupation)
					{
					case Occupation.Weaponsmith:
					case Occupation.Blacksmith:
					case Occupation.Armorer:
					case Occupation.GoodsTrader:
					case Occupation.HorseTrader:
					case Occupation.Artisan:
					case Occupation.Merchant:
						num2 = 38f;
						break;
					case Occupation.Headman:
					case Occupation.Preacher:
					case Occupation.GangLeader:
					case Occupation.RuralNotable:
						num2 = 46f;
						break;
					default:
						num2 = 30f;
						break;
					}
				}
			}
		}
		catch
		{
			num2 = 30f;
		}
		int num3 = -1;
		try
		{
			num3 = agent?.Index ?? (-1);
		}
		catch
		{
			num3 = -1;
		}
		if (num3 >= 0)
		{
			num2 += num3 % 5 - 2;
		}
		return Math.Max(18f, Math.Min(55f, num2));
	}

	public static bool TryTriggerDuelAction(NpcDataPacket npcData, string playerText, ref string content)
	{
		if (content.Contains("[ACTION:DUEL]"))
		{
			content = content.Replace("[ACTION:DUEL]", "").Trim();
			if (npcData == null)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public static void ExecuteDuel(Agent agent)
	{
		if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
		{
			DuelBehavior.PrepareDuel(characterObject.HeroObject, 3f);
			return;
		}
		if (agent != null)
		{
			DuelBehavior.PrepareDuel(agent, 3f);
		}
	}

	public static List<UnnamedPersonaIndexItem> GetUnnamedPersonaIndexItemsForDev(int maxCount)
	{
		List<UnnamedPersonaIndexItem> list = new List<UnnamedPersonaIndexItem>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null || _unnamedProfiles.Count == 0)
				{
					return list;
				}
				foreach (KeyValuePair<string, UnnamedNpcPersonaProfile> item in _unnamedProfiles.OrderBy((KeyValuePair<string, UnnamedNpcPersonaProfile> k) => k.Key))
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						UnnamedNpcPersonaProfile value = item.Value;
						string text = (value.TroopId ?? "").Trim();
						string text2 = (value.CultureId ?? "").Trim();
						string text3 = (value.Rank ?? "").Trim();
						string text4 = (value.Name ?? "").Trim();
						List<string> list2 = new List<string>();
						if (!string.IsNullOrEmpty(text))
						{
							list2.Add("Troop=" + text);
						}
						if (!string.IsNullOrEmpty(text2))
						{
							list2.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0906") + text2);
						}
						if (!string.IsNullOrEmpty(text3))
						{
							list2.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0907") + text3);
						}
						if (!string.IsNullOrEmpty(text4))
						{
							list2.Add(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0908") + text4);
						}
						string label = ((list2.Count > 0) ? string.Join(" | ", list2) : item.Key);
						list.Add(new UnnamedPersonaIndexItem
						{
							Key = item.Key,
							Label = label
						});
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public static bool TryGetUnnamedPersonaByKey(string key, out string personality, out string background)
	{
		personality = "";
		background = "";
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null)
				{
					return false;
				}
				if (_unnamedProfiles.TryGetValue(text, out var value) && value != null)
				{
					string value2 = (personality = GetUnnamedProfileDescription(value));
					background = "";
					return !string.IsNullOrWhiteSpace(value2);
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static void SaveUnnamedPersonaByKey(string key, string personality, string background)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			string personality2 = (personality ?? "").Trim();
			string background2 = (background ?? "").Trim();
			string text2 = MergePersonaFieldsToDescription(personality2, background2);
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null)
				{
					_unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
				}
				if (string.IsNullOrEmpty(text2))
				{
					_unnamedProfiles.Remove(text);
				}
				else
				{
					if (!_unnamedProfiles.TryGetValue(text, out var value) || value == null)
					{
						value = new UnnamedNpcPersonaProfile();
					}
					value.Description = text2;
					value.Personality = "";
					value.Background = "";
					_unnamedProfiles[text] = value;
				}
				SaveUnnamedProfilesUnsafe();
			}
			try
			{
				string text3 = (text2 ?? "").Replace("\r", "").Replace("\n", " ");
				if (text3.Length > 160)
				{
					text3 = text3.Substring(0, 160);
				}
				Logger.Log("UnnamedPersona", $"manual_save key={text} hasDesc={!string.IsNullOrEmpty(text2)} D={text3}");
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	public static void ExportUnnamedPersonaToDir(string exportRootDir, bool overwriteExistingFiles = true)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(exportRootDir))
			{
				return;
			}
			if (!Directory.Exists(exportRootDir))
			{
				Directory.CreateDirectory(exportRootDir);
			}
			string text = System.IO.Path.Combine(exportRootDir, "unnamed_persona");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			if (overwriteExistingFiles)
			{
				try
				{
					string[] files = Directory.GetFiles(text, "*.json", SearchOption.TopDirectoryOnly);
					foreach (string path in files)
					{
						try
						{
							File.Delete(path);
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
			}
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null || _unnamedProfiles.Count == 0)
				{
					return;
				}
				foreach (KeyValuePair<string, UnnamedNpcPersonaProfile> unnamedProfile in _unnamedProfiles)
				{
					if (string.IsNullOrEmpty(unnamedProfile.Key) || unnamedProfile.Value == null)
					{
						continue;
					}
					string text2 = (unnamedProfile.Value.Name ?? "").Trim();
					string text3 = (string.IsNullOrEmpty(text2) ? SanitizeFileName(unnamedProfile.Key) : SanitizeFileName(text2));
					string text4 = text3 + "__" + StableHash8(unnamedProfile.Key);
					string path2 = System.IO.Path.Combine(text, text4 + ".json");
					if (!overwriteExistingFiles && File.Exists(path2))
					{
						continue;
					}
					try
					{
						if (overwriteExistingFiles && File.Exists(path2))
						{
							File.Delete(path2);
						}
					}
					catch
					{
					}
					var value = new
					{
						unnamedProfile.Key,
						unnamedProfile.Value.Personality,
						unnamedProfile.Value.Background,
						unnamedProfile.Value.CultureId,
						unnamedProfile.Value.Rank,
						unnamedProfile.Value.Name,
						unnamedProfile.Value.TroopId
					};
					string contents = JsonConvert.SerializeObject(value, Formatting.Indented);
					File.WriteAllText(path2, contents, Encoding.UTF8);
				}
			}
		}
		catch
		{
		}
	}

	public static bool HasUnnamedPersonaKey(string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				return _unnamedProfiles != null && _unnamedProfiles.ContainsKey(text);
			}
		}
		catch
		{
			return false;
		}
	}

	public static void ImportUnnamedPersonaFromDir(string importRootDir) => TryImportUnnamedPersonaFromDir(importRootDir);

    internal static bool TryImportUnnamedPersonaFromDir(string importRootDir, bool overwriteExisting = true)
    {
        try
        {
            var records = UnnamedPersonaImportValidationOwner.ReadProfiles(importRootDir);
            var prepared = new Dictionary<string, UnnamedNpcPersonaProfile>();
            foreach (var entry in records)
            {
                JObject profile = entry.Value;
                prepared.Add(entry.Key, new UnnamedNpcPersonaProfile
                {
                    Description = (profile["Description"] ?? profile["description"])?.ToString(),
                    Personality = (profile["Personality"] ?? profile["personality"])?.ToString(),
                    Background = (profile["Background"] ?? profile["background"])?.ToString(),
                    CultureId = (profile["CultureId"] ?? profile["cultureId"] ?? profile["culture_id"])?.ToString(),
                    Rank = (profile["Rank"] ?? profile["rank"])?.ToString(),
                    Name = (profile["Name"] ?? profile["name"])?.ToString(),
                    TroopId = (profile["TroopId"] ?? profile["troopId"] ?? profile["troop_id"])?.ToString()
                });
            }
            if (prepared.Count == 0) return true;
            LoadUnnamedProfilesIfNeeded();
            lock (_unnamedProfilesLock)
            {
                if (_unnamedProfiles == null) _unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
                bool changed = false;
                foreach (var entry in prepared)
                {
                    if (!overwriteExisting && _unnamedProfiles.ContainsKey(entry.Key)) continue;
                    _unnamedProfiles[entry.Key] = entry.Value;
                    changed = true;
                }
                if (changed) SaveUnnamedProfilesUnsafe();
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("NpcPersona", "[WARN] Unnamed persona import failed: " + ex.Message);
            return false;
        }
    }

    public static void ImportUnnamedPersonaFromDir(string importRootDir, bool overwriteExisting)
        => TryImportUnnamedPersonaFromDir(importRootDir, overwriteExisting);

	public static string ExportUnnamedPersonaStateJson(bool pretty = false)
	{
		try
		{
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				UnnamedNpcProfilesFile unnamedNpcProfilesFile = new UnnamedNpcProfilesFile
				{
					Profiles = new Dictionary<string, UnnamedNpcPersonaProfile>(_unnamedProfiles ?? new Dictionary<string, UnnamedNpcPersonaProfile>())
				};
				return JsonConvert.SerializeObject(unnamedNpcProfilesFile, pretty ? Formatting.Indented : Formatting.None);
			}
		}
		catch
		{
			return "";
		}
	}

	public static bool ImportUnnamedPersonaStateJson(string json, bool overwriteExisting = true)
	{
		try
		{
			LoadUnnamedProfilesIfNeeded();
			UnnamedNpcProfilesFile unnamedNpcProfilesFile = string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<UnnamedNpcProfilesFile>(json);
			Dictionary<string, UnnamedNpcPersonaProfile> dictionary = unnamedNpcProfilesFile?.Profiles ?? new Dictionary<string, UnnamedNpcPersonaProfile>();
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null || overwriteExisting)
				{
					_unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
				}
				foreach (KeyValuePair<string, UnnamedNpcPersonaProfile> item in dictionary)
				{
					string text = (item.Key ?? "").Trim().ToLower();
					if (!string.IsNullOrEmpty(text) && item.Value != null && (overwriteExisting || !_unnamedProfiles.ContainsKey(text)))
					{
						_unnamedProfiles[text] = item.Value;
					}
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
