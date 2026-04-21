using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public static class ShoutUtils
{
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
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		char[] array = invalidFileNameChars;
		foreach (char oldChar in array)
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
		BasicCharacterObject character = agent.Character;
		CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		string text = ((val != null) ? ((MBObjectBase)val).StringId : null) ?? "";
		string text2 = "";
		string text3 = "";
		try
		{
			IAgentOriginBase origin = agent.Origin;
			IBattleCombatant obj = ((origin != null) ? origin.BattleCombatant : null);
			PartyBase val2 = (PartyBase)(object)((obj is PartyBase) ? obj : null);
			object obj2;
			if (val2 == null)
			{
				obj2 = null;
			}
			else
			{
				IFaction mapFaction = val2.MapFaction;
				obj2 = ((mapFaction != null) ? mapFaction.StringId : null);
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			text2 = (string)obj2;
			object obj3;
			if (val2 == null)
			{
				obj3 = null;
			}
			else
			{
				Hero leaderHero = val2.LeaderHero;
				obj3 = ((leaderHero != null) ? ((MBObjectBase)leaderHero).StringId : null);
			}
			if (obj3 == null)
			{
				obj3 = "";
			}
			text3 = (string)obj3;
		}
		catch
		{
			text2 = "";
			text3 = "";
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			try
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				object obj5;
				if (currentSettlement == null)
				{
					obj5 = null;
				}
				else
				{
					Clan ownerClan = currentSettlement.OwnerClan;
					if (ownerClan == null)
					{
						obj5 = null;
					}
					else
					{
						Kingdom kingdom = ownerClan.Kingdom;
						obj5 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
					}
				}
				if (obj5 == null)
				{
					if (currentSettlement == null)
					{
						obj5 = null;
					}
					else
					{
						IFaction mapFaction2 = currentSettlement.MapFaction;
						obj5 = ((mapFaction2 != null) ? mapFaction2.StringId : null);
					}
					if (obj5 == null)
					{
						obj5 = "";
					}
				}
				text2 = ((string)obj5).Trim().ToLower();
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
		object obj7;
		if (val == null)
		{
			obj7 = null;
		}
		else
		{
			CultureObject culture = val.Culture;
			obj7 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
		}
		if (obj7 == null)
		{
			obj7 = "neutral";
		}
		string text6 = ((string)obj7).ToLower();
		string text7 = ((val != null && ((BasicCharacterObject)val).IsSoldier) ? "soldier" : "commoner");
		string text8 = agent.Name?.ToString() ?? "路人";
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
				BasicCharacterObject obj = ((agent != null) ? agent.Character : null);
				CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
				object obj2;
				if (val == null)
				{
					obj2 = null;
				}
				else
				{
					CultureObject culture = val.Culture;
					obj2 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
				}
				if (obj2 == null)
				{
					obj2 = "neutral";
				}
				string text4 = ((string)obj2).ToLower();
				string text5 = ((val != null && ((BasicCharacterObject)val).IsSoldier) ? "soldier" : "commoner");
				string text6 = ((agent == null) ? null : agent.Name?.ToString()) ?? "路人";
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
			JObject val = JObject.Parse(raw);
			string text = ((object)(val["profile"] ?? val["Profile"] ?? val["desc"] ?? val["Desc"] ?? val["description"] ?? val["Description"]))?.ToString() ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				personality = text;
				background = "";
				return true;
			}
			personality = ((object)(val["personality"] ?? val["Personality"]))?.ToString() ?? "";
			background = ((object)(val["background"] ?? val["Background"]))?.ToString() ?? "";
			return true;
		}
		catch
		{
			return false;
		}
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
			string n = (name ?? "路人").Trim();
			if (string.IsNullOrEmpty(n))
			{
				n = "路人";
			}
			string t = (troopId ?? "").Trim().ToLower();
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
					object obj2;
					if (settlement == null)
					{
						obj2 = null;
					}
					else
					{
						Clan ownerClan = settlement.OwnerClan;
						if (ownerClan == null)
						{
							obj2 = null;
						}
						else
						{
							Kingdom kingdom2 = ownerClan.Kingdom;
							obj2 = ((kingdom2 != null) ? ((MBObjectBase)kingdom2).StringId : null);
						}
					}
					if (obj2 == null)
					{
						if (settlement == null)
						{
							obj2 = null;
						}
						else
						{
							IFaction mapFaction = settlement.MapFaction;
							obj2 = ((mapFaction != null) ? mapFaction.StringId : null);
						}
						if (obj2 == null)
						{
							obj2 = "";
						}
					}
					kingdom = ((string)obj2).Trim().ToLower();
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
				Kingdom kObj = ((IEnumerable<Kingdom>)Kingdom.All)?.FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim().ToLower(), kingdom, StringComparison.OrdinalIgnoreCase));
				if (kObj != null)
				{
					kingdomName = (((object)kObj.Name)?.ToString() ?? kingdomName).Trim();
					Hero leader = kObj.Leader;
					rulerName = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
				}
			}
			catch
			{
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
					Hero obj5 = Hero.Find(lordId2);
					lordName = (((obj5 == null) ? null : ((object)obj5.Name)?.ToString()) ?? "").Trim();
				}
			}
			catch
			{
				lordId2 = "";
				lordName = "";
			}
			string sys = "你是《骑马与砍杀2：霸主》的无名NPC描述生成器。你只输出严格 JSON，不要输出任何额外文字，不要 Markdown，不要代码块。JSON 仅包含 1 个字段：profile。profile 是一段中文描述，请生成100字左右的描述，不换行。描述必须与提供的势力/效忠事实一致，不得让该 NPC 自称属于其他国家或效忠其他统治者。";
			StringBuilder userSb = new StringBuilder();
			userSb.AppendLine("请生成100字左右的描述（他大概是什么人/做什么的/说话风格如何），不要分段。");
			userSb.AppendLine("名字: " + n);
			if (!string.IsNullOrWhiteSpace(t))
			{
				userSb.AppendLine("兵种ID: " + t);
			}
			if (!string.IsNullOrWhiteSpace(c))
			{
				userSb.AppendLine("文化: " + c);
			}
			if (!string.IsNullOrWhiteSpace(r))
			{
				userSb.AppendLine("身份: " + r);
			}
			userSb.AppendLine("势力: " + kingdomName + " (StringId=" + kingdom + ")");
			if (!string.IsNullOrWhiteSpace(rulerName))
			{
				userSb.AppendLine("统治者/势力领袖: " + rulerName);
			}
			if (!string.IsNullOrWhiteSpace(lordName))
			{
				userSb.AppendLine("隶属领主: " + lordName);
			}
			else if (!string.IsNullOrWhiteSpace(lordId2))
			{
				userSb.AppendLine("隶属领主Id: " + lordId2);
			}
			string user = userSb.ToString().Trim();
			List<object> messages = new List<object>
			{
				new
				{
					role = "system",
					content = sys
				},
				new
				{
					role = "user",
					content = user
				}
			};
			string rawResp = ((await ShoutNetwork.CallApiWithMessages(messages, 5000)) ?? "").Trim();
			if (string.IsNullOrWhiteSpace(rawResp))
			{
				return;
			}
			if (rawResp.Contains("错误：未配置 API Key") || rawResp.Contains("API请求失败") || rawResp.Contains("程序错误") || rawResp.Contains("API响应格式错误"))
			{
				InformationManager.DisplayMessage(new InformationMessage("无名NPC人设生成失败：" + rawResp));
				return;
			}
			if (!TryParsePersonaJson(rawResp, out var genP, out var genB))
			{
				Logger.Log("UnnamedPersona", "auto_gen_parse_fail key=" + k + " resp=" + rawResp);
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
		BasicCharacterObject character = agent.Character;
		CharacterObject co = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		if (co != null && !((BasicCharacterObject)co).IsHero)
		{
			string key = GetUnnamedKey(agent);
			if (!string.IsNullOrEmpty(key))
			{
				CultureObject culture = co.Culture;
				string cultureId = (((culture != null) ? ((MBObjectBase)culture).StringId : null) ?? "neutral").ToLower();
				string rank = (((BasicCharacterObject)co).IsSoldier ? "soldier" : "commoner");
				string name = agent.Name?.ToString() ?? "路人";
				string troopId = (((MBObjectBase)co).StringId ?? "").ToLower();
				await EnsureUnnamedNpcPersonaGeneratedByKeyAsync(key, cultureId, rank, name, troopId);
			}
		}
	}

	public static bool IsInValidScene()
	{
		if (Mission.Current == null)
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
		string text = Mission.Current.SceneName?.ToLower() ?? "";
		if (text.Contains("arena"))
		{
			return true;
		}
		return true;
	}

	public static string GetCurrentSceneDescription()
	{
		if (Mission.Current == null)
		{
			return "未知场景";
		}
		string text = "某个地方";
		string text2 = "";
		bool flag = false;
		bool flag2 = false;
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
					if (text5.StartsWith("你位于 ", StringComparison.Ordinal))
					{
						text2 = text5.Substring("你位于 ".Length).Trim();
						text2 = text2.TrimEnd('。', '.', ' ');
						flag = false;
					}
					else if (text5.StartsWith("你身处野外，靠近 ", StringComparison.Ordinal))
					{
						text2 = text5.Substring("你身处野外，靠近 ".Length).Trim();
						text2 = text2.TrimEnd('。', '.', ' ');
						flag = true;
						flag2 = true;
					}
					else if (text5 == "你身处野外。" || text5 == "你身处野外")
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
		if (string.IsNullOrEmpty(text2))
		{
			try
			{
				MobileParty mainParty = MobileParty.MainParty;
				if (((mainParty != null) ? mainParty.CurrentSettlement : null) != null)
				{
					text2 = ((object)MobileParty.MainParty.CurrentSettlement.Name).ToString();
				}
			}
			catch
			{
			}
		}
		if (flag2)
		{
			text = "野外";
		}
		else
		{
			string text6 = "";
			try
			{
				ICampaignMission current = CampaignMission.Current;
				object obj3;
				if (current == null)
				{
					obj3 = null;
				}
				else
				{
					Location location = current.Location;
					obj3 = ((location != null) ? location.StringId : null);
				}
				if (obj3 == null)
				{
					obj3 = "";
				}
				text6 = ((string)obj3).Trim().ToLowerInvariant();
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
					text = "领主大厅";
					break;
				case "tavern":
					text = "酒馆";
					break;
				case "arena":
					text = "竞技场";
					break;
				default:
					if (!(text6 == "dungeon"))
					{
						switch (text6)
						{
						case "alley":
							text = "小巷";
							break;
						case "port":
							text = "港口";
							break;
						default:
							if (!(text6 == "village_center"))
							{
								text = ((!string.IsNullOrEmpty(text2)) ? "街道" : "野外");
								break;
							}
							goto case "center";
						case "center":
							text = ((!string.IsNullOrEmpty(text2)) ? "街道" : "野外");
							break;
						}
						break;
					}
					goto case "prison";
				case "prison":
					text = "地牢";
					break;
				}
			}
			else
			{
				string text7 = Mission.Current.SceneName?.ToLower() ?? "";
				text = ((text7.Contains("lordshall") || text7.Contains("lord_hall") || text7.Contains("lordhall") || text7.Contains("lord") || text7.Contains("keep")) ? "领主大厅" : (text7.Contains("tavern") ? "酒馆" : (text7.Contains("arena") ? "竞技场" : ((text7.Contains("prison") || text7.Contains("dungeon")) ? "地牢" : (string.IsNullOrEmpty(text2) ? "野外" : "街道")))));
			}
		}
		string text8 = (string.IsNullOrEmpty(text2) ? text : (flag ? ("靠近 " + text2 + " 的 " + text) : ("位于 " + text2 + " 的 " + text)));
		if (!string.IsNullOrEmpty(text3))
		{
			return text8 + " | " + text3;
		}
		return text8;
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
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Invalid comparison between Unknown and I4
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Invalid comparison between Unknown and I4
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Invalid comparison between Unknown and I4
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Invalid comparison between Unknown and I4
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Invalid comparison between Unknown and I4
		try
		{
			if (settlement == null)
			{
				return "";
			}
			string value = (((object)settlement.Name)?.ToString() ?? "").Trim();
			Clan ownerClan = settlement.OwnerClan;
			Hero val = ((ownerClan != null) ? ownerClan.Leader : null);
			string value2 = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
			string value3 = (((ownerClan == null) ? null : ((object)ownerClan.Name)?.ToString()) ?? "").Trim();
			string text = "";
			string text2 = "";
			try
			{
				IFaction mapFaction = settlement.MapFaction;
				text = (((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString()) ?? "").Trim();
				string text3 = "";
				try
				{
					CultureObject obj = ((mapFaction != null) ? mapFaction.Culture : null);
					text3 = (((obj != null) ? ((MBObjectBase)obj).StringId : null) ?? "").Trim();
					if (val != null && val.IsFemale)
					{
						text3 += "_f";
					}
				}
				catch
				{
					text3 = "";
				}
				text2 = ((mapFaction != null && mapFaction.IsKingdomFaction && val != null && mapFaction.Leader == val) ? ((object)GameTexts.FindText("str_faction_ruler", text3))?.ToString() : ((object)GameTexts.FindText("str_faction_official", text3))?.ToString());
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
				SettlementComponent settlementComponent = settlement.SettlementComponent;
				string text5 = ((settlementComponent != null) ? ((object)settlementComponent.GetProsperityLevel()/*cast due to .constrained prefix*/).ToString() : null) ?? "0";
				if (settlement.IsTown)
				{
					text4 = ((object)GameTexts.FindText("str_town_long_prosperity_1", text5))?.ToString();
				}
				else if (settlement.IsVillage)
				{
					text4 = ((object)GameTexts.FindText("str_village_long_prosperity", text5))?.ToString();
				}
			}
			catch
			{
				text4 = "";
			}
			text4 = (text4 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			string text6 = "";
			try
			{
				if (settlement.IsTown)
				{
					Town town = settlement.Town;
					SettlementComponent settlementComponent2 = settlement.SettlementComponent;
					if (town != null && settlementComponent2 != null)
					{
						float loyalty = town.Loyalty;
						ProsperityLevel prosperityLevel = settlementComponent2.GetProsperityLevel();
						string text7 = ((!(loyalty < 25f)) ? ((!(loyalty < 65f)) ? (((int)prosperityLevel <= 0) ? "str_settlement_morale_high_adversity" : (((int)prosperityLevel > 1) ? "str_settlement_morale_high_prosperity" : "str_settlement_morale_high_average")) : (((int)prosperityLevel > 1) ? "str_settlement_morale_medium_prosperity" : "str_settlement_morale_medium_average")) : (((int)prosperityLevel <= 0) ? "str_settlement_morale_rebellious_adversity" : (((int)prosperityLevel > 1) ? "str_settlement_morale_rebellious_prosperity" : "str_settlement_morale_rebellious_average")));
						text6 = ((object)GameTexts.FindText(text7, (string)null))?.ToString();
					}
				}
			}
			catch
			{
				text6 = "";
			}
			text6 = (text6 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(value))
			{
				if (settlement.IsTown)
				{
					if (ownerClan == Clan.PlayerClan)
					{
						stringBuilder.Append(value);
						stringBuilder.Append("是你的封地。");
					}
					else
					{
						stringBuilder.Append(value);
						if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(value2))
						{
							stringBuilder.Append("被");
							stringBuilder.Append(text);
							stringBuilder.Append("的");
							stringBuilder.Append(text2);
							stringBuilder.Append("，");
							stringBuilder.Append(value2);
							stringBuilder.Append("统治着。");
						}
						else if (!string.IsNullOrWhiteSpace(value2))
						{
							stringBuilder.Append("由");
							stringBuilder.Append(value2);
							stringBuilder.Append("统治。");
						}
						else
						{
							stringBuilder.Append("的情况如下。");
						}
					}
				}
				else
				{
					stringBuilder.Append(value);
					if (!string.IsNullOrWhiteSpace(value2))
					{
						stringBuilder.Append("由");
						stringBuilder.Append(value2);
						stringBuilder.Append("控制。");
					}
					else
					{
						stringBuilder.Append("的情况如下。");
					}
				}
			}
			if (!string.IsNullOrWhiteSpace(value3))
			{
				stringBuilder.Append("所属家族：");
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
			if (!string.IsNullOrWhiteSpace(text6))
			{
				stringBuilder.Append(text6);
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
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Invalid comparison between Unknown and I4
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Invalid comparison between Unknown and I4
		if (hero == null)
		{
			return "英雄";
		}
		try
		{
			if (hero.IsLord || (int)hero.Occupation == 3)
			{
				return "领主";
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsNotable || fromNotables || (int)hero.Occupation == 20 || (int)hero.Occupation == 22 || (int)hero.Occupation == 21 || (int)hero.Occupation == 18 || (int)hero.Occupation == 17 || (int)hero.Occupation == 19)
			{
				return "头人";
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsWanderer || (int)hero.Occupation == 16)
			{
				return "流浪者";
			}
		}
		catch
		{
		}
		return "英雄";
	}

	private static int GetHeroTypePriorityForPrompt(string type)
	{
		return (type ?? "").Trim() switch
		{
			"领主" => 4, 
			"头人" => 3, 
			"流浪者" => 2, 
			_ => 1, 
		};
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
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			if (((partyBelongedTo != null) ? partyBelongedTo.CurrentSettlement : null) == settlement)
			{
				return true;
			}
			try
			{
				MBReadOnlyList<Hero> heroesWithoutParty = settlement.HeroesWithoutParty;
				if (heroesWithoutParty != null)
				{
					string text = (((MBObjectBase)hero).StringId ?? "").Trim();
					foreach (Hero item in (List<Hero>)(object)heroesWithoutParty)
					{
						if (item != null)
						{
							if (item == hero)
							{
								return true;
							}
							string b = (((MBObjectBase)item).StringId ?? "").Trim();
							if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
							{
								return true;
							}
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
				if (hero != null)
				{
					string text4 = (((object)hero.Name)?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						string text5 = (((MBObjectBase)hero).StringId ?? "").Trim();
						string text6 = (string.IsNullOrWhiteSpace(text5) ? ("name:" + text4) : ("id:" + text5.ToLowerInvariant()));
						string heroTypeForPrompt = GetHeroTypeForPrompt(hero, fromNotables);
						if (!dictionary.ContainsKey(text6))
						{
							dictionary[text6] = text4;
							dictionary2[text6] = heroTypeForPrompt;
							list.Add(text6);
						}
						else if (GetHeroTypePriorityForPrompt(heroTypeForPrompt) > GetHeroTypePriorityForPrompt(dictionary2[text6]))
						{
							dictionary2[text6] = heroTypeForPrompt;
						}
					}
				}
			};
			try
			{
				MBReadOnlyList<Hero> heroesWithoutParty = settlement.HeroesWithoutParty;
				if (heroesWithoutParty != null)
				{
					foreach (Hero item in (List<Hero>)(object)heroesWithoutParty)
					{
						action(item, item != null && item.IsNotable);
					}
				}
			}
			catch
			{
			}
			Clan ownerClan = settlement.OwnerClan;
			Hero val = ((ownerClan != null) ? ownerClan.Leader : null);
			if (IsHeroInSettlementForPrompt(val, settlement))
			{
				action(val, arg2: false);
			}
			Town town = settlement.Town;
			Hero val2 = ((town != null) ? town.Governor : null);
			if (IsHeroInSettlementForPrompt(val2, settlement))
			{
				action(val2, arg2: false);
			}
			try
			{
				MBReadOnlyList<Hero> notables = settlement.Notables;
				if (notables != null)
				{
					foreach (Hero item2 in (List<Hero>)(object)notables)
					{
						if (IsHeroInSettlementForPrompt(item2, settlement))
						{
							action(item2, arg2: true);
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
			for (int num = 0; num < list.Count && num < maxCount; num++)
			{
				string key = list[num];
				string text = dictionary[key];
				string text2 = dictionary2[key];
				list2.Add("[" + text2 + "]" + text);
			}
			string text3 = "当前定居点HeroNPC：" + string.Join("；", list2);
			if (count > maxCount)
			{
				text3 = text3 + "；等" + count + "人";
			}
			text3 = text3.Replace("\r", "").Replace("\n", " ").Trim();
			if (text3.Length > maxLen)
			{
				text3 = text3.Substring(0, maxLen) + "…";
			}
			return text3;
		}
		catch
		{
			return "";
		}
	}

	public static string BuildNearbySettlementsDetailForPrompt(CampaignVec2 origin, Hero perspectiveHero = null)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!((CampaignVec2)(ref origin)).IsValid())
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
			Vec2 val = ((CampaignVec2)(ref origin)).ToVec2();
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				string value = (((object)item.Name)?.ToString() ?? "").Trim();
				if (string.IsNullOrEmpty(value))
				{
					continue;
				}
				CampaignVec2 gatePosition = item.GatePosition;
				Vec2 val2 = ((CampaignVec2)(ref gatePosition)).ToVec2();
				float num5 = val2.x - val.x;
				float num6 = val2.y - val.y;
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
			AppendDirLine(list, "北", st, num, perspectiveHero);
			AppendDirLine(list, "东", st2, num2, perspectiveHero);
			AppendDirLine(list, "南", st3, num3, perspectiveHero);
			AppendDirLine(list, "西", st4, num4, perspectiveHero);
			if (list.Count == 0)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【周边定居点（地图）】");
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
			Settlement val = Settlement.CurrentSettlement ?? FindNearestSettlementForCurrentScene();
			if (val == null)
			{
				return "";
			}
			return BuildCurrentSceneSettlementInlineSuffixForPrompt(val, perspectiveHero);
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
			string value = (((object)settlement.Name)?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(value))
			{
				return "";
			}
			CultureObject culture = settlement.Culture;
			string text = (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
			Clan ownerClan = settlement.OwnerClan;
			Hero val = ((ownerClan != null) ? ownerClan.Leader : null);
			string text2 = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
			Clan ownerClan2 = settlement.OwnerClan;
			string text3 = (((ownerClan2 == null) ? null : ((object)ownerClan2.Name)?.ToString()) ?? "").Trim();
			IFaction mapFaction = settlement.MapFaction;
			string text4 = (((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString()) ?? "").Trim();
			string text5 = BuildFactionRelationSummary(settlement.MapFaction, perspectiveHero);
			List<string> list = new List<string>();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add("该定居点属" + text + "文化");
			}
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
			{
				list.Add("由" + text2 + "所属的" + text3 + "家族统治");
			}
			else if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add("领主是" + text2);
			}
			else if (!string.IsNullOrWhiteSpace(text3))
			{
				list.Add("由" + text3 + "家族统治");
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list.Add("隶属" + text4);
			}
			if (!string.IsNullOrWhiteSpace(text5))
			{
				list.Add(text5);
			}
			if ((settlement.IsTown || settlement.IsCastle) && val != null && !IsHeroInSettlementForPrompt(val, settlement))
			{
				list.Add("当前统治者不在此定居点");
			}
			return (list.Count <= 0) ? "" : ("；" + string.Join("，", list));
		}
		catch
		{
			return "";
		}
	}

	private static Settlement FindNearestSettlementForCurrentScene()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			CampaignVec2? val = ((mainParty != null) ? new CampaignVec2?(mainParty.Position) : ((CampaignVec2?)null));
			if (val.HasValue)
			{
				CampaignVec2 val2 = val.Value;
				if (((CampaignVec2)(ref val2)).IsValid())
				{
					val2 = val.Value;
					Vec2 val3 = ((CampaignVec2)(ref val2)).ToVec2();
					Settlement result = null;
					float num = float.MaxValue;
					foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
					{
						if (item != null && !item.IsHideout)
						{
							val2 = item.GatePosition;
							Vec2 val4 = ((CampaignVec2)(ref val2)).ToVec2();
							float num2 = val4.x - val3.x;
							float num3 = val4.y - val3.y;
							float num4 = num2 * num2 + num3 * num3;
							if (num4 < num)
							{
								num = num4;
								result = item;
							}
						}
					}
					return result;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static string GetSettlementTypeText(Settlement settlement)
	{
		return settlement.IsTown ? "城镇" : (settlement.IsCastle ? "城堡" : (settlement.IsVillage ? "村庄" : ((!settlement.IsFortification) ? "定居点" : "要塞")));
	}

	private static string BuildFactionRelationSummary(IFaction faction, Hero perspectiveHero)
	{
		try
		{
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			Hero mainHero = Hero.MainHero;
			object obj;
			if (mainHero == null)
			{
				obj = null;
			}
			else
			{
				Clan clan = mainHero.Clan;
				obj = ((clan != null) ? clan.Kingdom : null);
			}
			IFaction val = (IFaction)obj;
			object obj2 = val;
			if (obj2 == null)
			{
				Hero mainHero2 = Hero.MainHero;
				obj2 = ((mainHero2 != null) ? mainHero2.MapFaction : null);
				if (obj2 == null)
				{
					Clan playerClan = Clan.PlayerClan;
					val = (IFaction)(object)((playerClan != null) ? playerClan.Kingdom : null);
					obj2 = val;
					if (obj2 == null)
					{
						Clan playerClan2 = Clan.PlayerClan;
						obj2 = ((playerClan2 != null) ? playerClan2.MapFaction : null);
					}
				}
			}
			string factionRelationLabel = GetFactionRelationLabel(faction, (IFaction)obj2);
			object obj3;
			if (perspectiveHero == null)
			{
				obj3 = null;
			}
			else
			{
				Clan clan2 = perspectiveHero.Clan;
				obj3 = ((clan2 != null) ? clan2.Kingdom : null);
			}
			val = (IFaction)obj3;
			string factionRelationLabel2 = GetFactionRelationLabel(faction, val ?? ((perspectiveHero != null) ? perspectiveHero.MapFaction : null));
			if (string.IsNullOrWhiteSpace(factionRelationLabel2))
			{
				return BuildSingleRelationPhrase(text, factionRelationLabel);
			}
			if (string.Equals(factionRelationLabel, factionRelationLabel2, StringComparison.Ordinal))
			{
				return BuildSharedRelationPhrase(factionRelationLabel, text);
			}
			return BuildSplitRelationPhrase(factionRelationLabel2, factionRelationLabel, text);
		}
		catch
		{
			string text2 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "玩家";
			}
			return "对你和" + text2 + "都保持中立";
		}
	}

	private static string BuildSingleRelationPhrase(string targetName, string relation)
	{
		string text = (relation ?? "").Trim();
		string text2 = text;
		if (!(text2 == "敌对"))
		{
			if (text2 == "友方")
			{
				return "是" + targetName + "的友方势力";
			}
			return "与" + targetName + "保持中立";
		}
		return "是" + targetName + "的敌人";
	}

	private static string BuildSharedRelationPhrase(string relation, string playerName)
	{
		string text = (string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName);
		string text2 = (relation ?? "").Trim();
		string text3 = text2;
		if (!(text3 == "敌对"))
		{
			if (text3 == "友方")
			{
				return "是你和" + text + "的友方势力";
			}
			return "对你和" + text + "都保持中立";
		}
		return "是你和" + text + "的敌人";
	}

	private static string BuildSplitRelationPhrase(string npcRelation, string playerRelation, string playerName)
	{
		string targetName = (string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName);
		return BuildSingleRelationPhrase("你", npcRelation) + "，但" + BuildSingleRelationPhrase(targetName, playerRelation);
	}

	private static string GetFactionRelationLabel(IFaction faction, IFaction referenceFaction)
	{
		try
		{
			if (faction == null || referenceFaction == null)
			{
				return "中立";
			}
			string text = (faction.StringId ?? "").Trim();
			string b = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
			{
				return "友方";
			}
			if (faction == referenceFaction)
			{
				return "友方";
			}
			if (referenceFaction.IsAtWarWith(faction) || faction.IsAtWarWith(referenceFaction))
			{
				return "敌对";
			}
			return "中立";
		}
		catch
		{
			return "中立";
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
					text2 = $"；距离：{num:0.0} 公里";
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
			string text = (((object)settlement.Name)?.ToString() ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			string settlementTypeText = GetSettlementTypeText(settlement);
			CultureObject culture = settlement.Culture;
			string text2 = (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
			IFaction mapFaction = settlement.MapFaction;
			string text3 = (((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString()) ?? "").Trim();
			Clan ownerClan = settlement.OwnerClan;
			object obj;
			if (ownerClan == null)
			{
				obj = null;
			}
			else
			{
				Hero leader = ownerClan.Leader;
				obj = ((leader == null) ? null : ((object)leader.Name)?.ToString());
			}
			if (obj == null)
			{
				obj = "";
			}
			string text4 = ((string)obj).Trim();
			Clan ownerClan2 = settlement.OwnerClan;
			string text5 = (((ownerClan2 == null) ? null : ((object)ownerClan2.Name)?.ToString()) ?? "").Trim();
			string text6 = BuildFactionRelationSummary(settlement.MapFaction, perspectiveHero);
			List<string> list = new List<string>();
			list.Add(settlementTypeText);
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add("文化：" + text2);
			}
			if (!string.IsNullOrEmpty(text4))
			{
				list.Add("领主：" + text4);
			}
			if (!string.IsNullOrEmpty(text5))
			{
				list.Add("家族：" + text5);
			}
			if (!string.IsNullOrEmpty(text3))
			{
				list.Add("隶属：" + text3);
			}
			if (!string.IsNullOrEmpty(text6))
			{
				list.Add(text6);
			}
			string text7 = text + "（" + string.Join("，", list) + "）";
			string nativeSettlementInfoForPrompt = GetNativeSettlementInfoForPrompt(settlement);
			if (string.IsNullOrWhiteSpace(nativeSettlementInfoForPrompt))
			{
				return text7;
			}
			return text7 + "；" + nativeSettlementInfoForPrompt;
		}
		catch
		{
			return "";
		}
	}

	public static List<Agent> GetNearbyNPCAgents()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		List<Agent> list = new List<Agent>();
		if (Mission.Current == null || Agent.Main == null)
		{
			return list;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
		{
			if (item == Agent.Main || !item.IsActive() || !item.IsHuman)
			{
				continue;
			}
			Vec3 position2 = item.Position;
			float num = ((Vec3)(ref position2)).Distance(position);
			if (num <= 4f)
			{
				Vec3 val = item.Position - position;
				((Vec3)(ref val)).Normalize();
				if (Vec3.DotProduct(lookDirection, val) > 0.70710677f)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static Agent GetClosestFacingAgent(float maxDistance)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (Mission.Current == null || Agent.Main == null)
		{
			return null;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		Agent result = null;
		float num = maxDistance;
		foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
		{
			if (item == Agent.Main || !item.IsActive() || !item.IsHuman)
			{
				continue;
			}
			Vec3 position2 = item.Position;
			float num2 = ((Vec3)(ref position2)).Distance(position);
			if (num2 > maxDistance)
			{
				continue;
			}
			Vec3 val = position - item.Position;
			((Vec3)(ref val)).Normalize();
			Vec3 lookDirection2 = item.LookDirection;
			if (!(Vec3.DotProduct(lookDirection2, val) < 0.5f))
			{
				Vec3 val2 = item.Position - position;
				((Vec3)(ref val2)).Normalize();
				if (Vec3.DotProduct(lookDirection, val2) >= 0.9f && num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	public static Agent GetFacingAgent(List<Agent> agents)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (Agent.Main == null || agents.Count == 0)
		{
			return null;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		Agent val = null;
		float num = -1f;
		foreach (Agent agent in agents)
		{
			Vec3 val2 = agent.Position - position;
			float length = ((Vec3)(ref val2)).Length;
			((Vec3)(ref val2)).Normalize();
			float num2 = Vec3.DotProduct(lookDirection, val2);
			if (num2 > 0.70710677f)
			{
				float num3 = num2 / (length * 0.1f + 0.1f);
				if (num3 > num)
				{
					num = num3;
					val = agent;
				}
			}
		}
		return val ?? agents[0];
	}

	public static NpcDataPacket ExtractNpcData(Agent agent)
	{
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Invalid comparison between Unknown and I4
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Invalid comparison between Unknown and I4
		if (agent == null)
		{
			return null;
		}
		NpcDataPacket npcDataPacket = new NpcDataPacket();
		npcDataPacket.Name = agent.Name ?? "路人";
		npcDataPacket.AgentIndex = agent.Index;
		npcDataPacket.RoleDesc = "平民";
		npcDataPacket.PersonalityDesc = "";
		npcDataPacket.BackgroundDesc = "";
		npcDataPacket.IsHero = false;
		npcDataPacket.CultureId = "neutral";
		npcDataPacket.UnnamedKey = "";
		npcDataPacket.TroopId = "";
		npcDataPacket.UnnamedRank = "";
		BasicCharacterObject character = agent.Character;
		CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		if (val != null)
		{
			npcDataPacket.IsFemale = ((BasicCharacterObject)val).IsFemale;
			try
			{
				if (((BasicCharacterObject)val).IsHero && val.HeroObject != null)
				{
					npcDataPacket.Age = val.HeroObject.Age;
				}
				else
				{
					npcDataPacket.Age = ResolveSceneNonHeroAge(agent, val);
				}
			}
			catch
			{
				npcDataPacket.Age = 30f;
			}
			if (val.Culture != null)
			{
				npcDataPacket.CultureId = ((MBObjectBase)val.Culture).StringId.ToLower();
			}
			npcDataPacket.CultureId = ResolveSceneCultureIdWithSettlementFallback(npcDataPacket.CultureId, agent, val);
			if (((BasicCharacterObject)val).IsHero)
			{
				npcDataPacket.IsHero = true;
				Hero val2 = null;
				try
				{
					val2 = val.HeroObject;
				}
				catch
				{
				}
				if (val2 != null)
				{
					if (val2.IsLord)
					{
						npcDataPacket.RoleDesc = "领主";
					}
					else if (val2.IsWanderer)
					{
						npcDataPacket.RoleDesc = "流浪者";
					}
					else if (val2.IsNotable)
					{
						npcDataPacket.RoleDesc = "要人";
					}
					else
					{
						npcDataPacket.RoleDesc = "英雄";
					}
					MyBehavior.GetNpcPersonaForExternal(val2, out var personality, out var background);
					if (!string.IsNullOrWhiteSpace(personality))
					{
						npcDataPacket.PersonalityDesc = personality.Trim();
					}
					if (!string.IsNullOrWhiteSpace(background))
					{
						npcDataPacket.BackgroundDesc = background.Trim();
					}
				}
				else if ((int)val.Occupation == 3)
				{
					npcDataPacket.RoleDesc = "领主";
				}
				else if ((int)val.Occupation == 16)
				{
					npcDataPacket.RoleDesc = "流浪者";
				}
				else
				{
					npcDataPacket.RoleDesc = "英雄";
				}
			}
			else if (((BasicCharacterObject)val).IsSoldier)
			{
				npcDataPacket.RoleDesc = "士兵";
			}
			if (!npcDataPacket.IsHero)
			{
				npcDataPacket.UnnamedKey = GetUnnamedKey(agent);
				npcDataPacket.TroopId = (((MBObjectBase)val).StringId ?? "").ToLower();
				npcDataPacket.UnnamedRank = (((BasicCharacterObject)val).IsSoldier ? "soldier" : "commoner");
				if (TryGetUnnamedNpcPersona(agent, out var personality2, out var background2))
				{
					if (!string.IsNullOrWhiteSpace(personality2))
					{
						npcDataPacket.PersonalityDesc = personality2.Trim();
					}
					if (!string.IsNullOrWhiteSpace(background2))
					{
						npcDataPacket.BackgroundDesc = background2.Trim();
					}
				}
			}
		}
		npcDataPacket.CultureId = ResolveSceneCultureIdWithSettlementFallback(npcDataPacket.CultureId, agent, null);
		EnsurePromptNameFields(npcDataPacket);
		return npcDataPacket;
	}

	public static void EnsurePromptNameFields(NpcDataPacket npc)
	{
		if (npc != null)
		{
			if (npc.IsHero)
			{
				npc.PromptGivenName = "";
				npc.PromptDisplayName = (npc.Name ?? "").Trim();
			}
			else
			{
				npc.PromptDisplayName = BuildPromptDisplayName(givenName: npc.PromptGivenName = PickPromptGivenName(npc, null), identityName: npc.Name);
			}
		}
	}

	public static void EnsureScenePromptNames(List<NpcDataPacket> allNpcData)
	{
		if (allNpcData == null || allNpcData.Count == 0)
		{
			return;
		}
		foreach (NpcDataPacket allNpcDatum in allNpcData)
		{
			EnsurePromptNameFields(allNpcDatum);
		}
		HashSet<string> usedGivenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcDataPacket item in from npc in allNpcData
			where npc != null && !npc.IsHero
			orderby npc.AgentIndex
			select npc)
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
		return string.IsNullOrWhiteSpace(text) ? "未命名NPC" : text;
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
			return string.IsNullOrWhiteSpace(text2) ? "未命名NPC" : text2;
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
			return string.IsNullOrWhiteSpace(text) ? "路人" : text;
		}
		int num = ComputeStablePromptNameHash(BuildPromptNameSeed(npc));
		int num2 = num % promptNamePool.Count;
		string text2 = "";
		for (int i = 0; i < promptNamePool.Count; i++)
		{
			string text3 = (promptNamePool[(num2 + i) % promptNamePool.Count] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = text3;
				}
				if (usedGivenNames == null || usedGivenNames.Add(text3))
				{
					return text3;
				}
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
		string key = ((cultureId ?? "").Trim().ToLowerInvariant() + "|" + (isFemale ? "f" : "m")).Trim();
		lock (_promptNamePoolLock)
		{
			if (_promptNamePoolCache.TryGetValue(key, out var value))
			{
				return value;
			}
		}
		List<string> list = BuildPromptNamePool(cultureId, isFemale);
		lock (_promptNamePoolLock)
		{
			_promptNamePoolCache[key] = list;
		}
		return list;
	}

	private static List<string> BuildPromptNamePool(string cultureId, bool isFemale)
	{
		List<string> list = new List<string>();
		CultureObject val = ResolvePromptNameCulture(cultureId);
		IEnumerable<TextObject> enumerable = null;
		try
		{
			NameGenerator current = NameGenerator.Current;
			enumerable = ((current == null) ? null : ((IEnumerable<TextObject>)current.GetNameListForCulture(val, isFemale))?.ToList());
		}
		catch
		{
			enumerable = null;
		}
		if ((enumerable == null || !enumerable.Any()) && val != null)
		{
			try
			{
				enumerable = ((IEnumerable<TextObject>)(isFemale ? val.FemaleNameList : val.MaleNameList))?.ToList();
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
			string text = (((object)item)?.ToString() ?? "").Trim();
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
				MBObjectManager instance = MBObjectManager.Instance;
				CultureObject val = ((instance != null) ? instance.GetObject<CultureObject>(text) : null);
				if (val != null)
				{
					return val;
				}
			}
			catch
			{
			}
			try
			{
				MBObjectManager instance2 = MBObjectManager.Instance;
				CultureObject val2 = ((instance2 == null) ? null : ((IEnumerable<CultureObject>)instance2.GetObjectTypeList<CultureObject>())?.FirstOrDefault((CultureObject c) => c != null && string.Equals((((MBObjectBase)c).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)));
				if (val2 != null)
				{
					return val2;
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
			MBObjectManager instance3 = MBObjectManager.Instance;
			return (instance3 == null) ? null : ((IEnumerable<CultureObject>)instance3.GetObjectTypeList<CultureObject>())?.FirstOrDefault((CultureObject c) => c != null);
		}
		catch
		{
			return null;
		}
	}

	private static string BuildPromptNameSeed(NpcDataPacket npc)
	{
		bool flag = npc?.IsFemale ?? false;
		return (npc?.UnnamedKey ?? "").Trim().ToLowerInvariant() + "|" + (npc?.TroopId ?? "").Trim().ToLowerInvariant() + "|" + (npc?.CultureId ?? "").Trim().ToLowerInvariant() + "|" + (flag ? "f" : "m") + "|" + (npc?.AgentIndex ?? 0).ToString(CultureInfo.InvariantCulture);
	}

	private static int ComputeStablePromptNameHash(string text)
	{
		int num = 17;
		string text2 = text ?? "";
		for (int i = 0; i < text2.Length; i++)
		{
			num = num * 31 + text2[i];
		}
		return num & 0x7FFFFFFF;
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
			object obj;
			if (characterObject == null)
			{
				obj = null;
			}
			else
			{
				CultureObject culture = characterObject.Culture;
				obj = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
			}
			if (obj == null)
			{
				if (agent == null)
				{
					obj = null;
				}
				else
				{
					BasicCharacterObject character = agent.Character;
					if (character == null)
					{
						obj = null;
					}
					else
					{
						BasicCultureObject culture2 = character.Culture;
						obj = ((culture2 != null) ? ((MBObjectBase)culture2).StringId : null);
					}
				}
				if (obj == null)
				{
					Settlement currentSettlement = Settlement.CurrentSettlement;
					if (currentSettlement == null)
					{
						obj = null;
					}
					else
					{
						CultureObject culture3 = currentSettlement.Culture;
						obj = ((culture3 != null) ? ((MBObjectBase)culture3).StringId : null);
					}
					if (obj == null)
					{
						obj = "";
					}
				}
			}
			string text2 = ((string)obj).Trim().ToLowerInvariant();
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
			Settlement currentSettlement2 = Settlement.CurrentSettlement;
			object obj3;
			if (currentSettlement2 == null)
			{
				obj3 = null;
			}
			else
			{
				IFaction mapFaction = currentSettlement2.MapFaction;
				if (mapFaction == null)
				{
					obj3 = null;
				}
				else
				{
					CultureObject culture4 = mapFaction.Culture;
					obj3 = ((culture4 != null) ? ((MBObjectBase)culture4).StringId : null);
				}
			}
			if (obj3 == null)
			{
				Settlement currentSettlement3 = Settlement.CurrentSettlement;
				if (currentSettlement3 == null)
				{
					obj3 = null;
				}
				else
				{
					Clan ownerClan = currentSettlement3.OwnerClan;
					if (ownerClan == null)
					{
						obj3 = null;
					}
					else
					{
						CultureObject culture5 = ownerClan.Culture;
						obj3 = ((culture5 != null) ? ((MBObjectBase)culture5).StringId : null);
					}
				}
				if (obj3 == null)
				{
					obj3 = "";
				}
			}
			string text3 = ((string)obj3).Trim().ToLowerInvariant();
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
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected I4, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Invalid comparison between Unknown and I4
		float num = 0f;
		try
		{
			num = ((agent != null) ? agent.Age : 0f);
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
			num = ((characterObject != null) ? ((BasicCharacterObject)characterObject).Age : 0f);
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
				if (((BasicCharacterObject)characterObject).IsSoldier)
				{
					num2 = 30f;
				}
				else
				{
					Occupation occupation = characterObject.Occupation;
					Occupation val = occupation;
					switch (val - 4)
					{
					default:
						if ((int)val != 28)
						{
							break;
						}
						goto case 0;
					case 0:
					case 6:
					case 7:
					case 8:
					case 13:
					case 14:
						num2 = 38f;
						goto end_IL_0096;
					case 15:
					case 16:
					case 17:
					case 18:
						num2 = 46f;
						goto end_IL_0096;
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 9:
					case 10:
					case 11:
					case 12:
						break;
					}
					num2 = 30f;
				}
			}
			end_IL_0096:;
		}
		catch
		{
			num2 = 30f;
		}
		int num3 = -1;
		try
		{
			num3 = ((agent != null) ? agent.Index : (-1));
		}
		catch
		{
			num3 = -1;
		}
		if (num3 >= 0)
		{
			num2 += (float)(num3 % 5 - 2);
		}
		return Math.Max(18f, Math.Min(55f, num2));
	}

	public static bool TryTriggerDuelAction(NpcDataPacket npcData, ref string content)
	{
		if (content.Contains("[ACTION:DUEL]"))
		{
			content = content.Replace("[ACTION:DUEL]", "").Trim();
			if (npcData.IsHero)
			{
				return true;
			}
		}
		return false;
	}

	public static void ExecuteDuel(Agent agent)
	{
		if (agent != null)
		{
			BasicCharacterObject character = agent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			if (val != null && val.HeroObject != null)
			{
				DuelBehavior.PrepareDuel(val.HeroObject, 3f);
			}
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
							list2.Add("文化=" + text2);
						}
						if (!string.IsNullOrEmpty(text3))
						{
							list2.Add("身份=" + text3);
						}
						if (!string.IsNullOrEmpty(text4))
						{
							list2.Add("称呼=" + text4);
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
			string text = Path.Combine(exportRootDir, "unnamed_persona");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			if (overwriteExistingFiles)
			{
				try
				{
					string[] files = Directory.GetFiles(text, "*.json", SearchOption.TopDirectoryOnly);
					string[] array = files;
					foreach (string path in array)
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
					string path2 = Path.Combine(text, text4 + ".json");
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
					var anon = new
					{
						unnamedProfile.Key,
						unnamedProfile.Value.Personality,
						unnamedProfile.Value.Background,
						unnamedProfile.Value.CultureId,
						unnamedProfile.Value.Rank,
						unnamedProfile.Value.Name,
						unnamedProfile.Value.TroopId
					};
					string contents = JsonConvert.SerializeObject((object)anon, (Formatting)1);
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

	public static void ImportUnnamedPersonaFromDir(string importRootDir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(importRootDir))
			{
				return;
			}
			string text = importRootDir;
			if (!Directory.Exists(text))
			{
				return;
			}
			string text2 = Path.Combine(text, "unnamed_persona");
			if (Directory.Exists(text2))
			{
				text = text2;
			}
			string[] files = Directory.GetFiles(text, "*.json");
			Dictionary<string, UnnamedNpcPersonaProfile> dictionary = new Dictionary<string, UnnamedNpcPersonaProfile>();
			string[] array = files;
			string[] array2 = array;
			foreach (string path in array2)
			{
				try
				{
					string text3 = File.ReadAllText(path);
					if (string.IsNullOrWhiteSpace(text3))
					{
						continue;
					}
					JObject val = null;
					try
					{
						val = JObject.Parse(text3);
					}
					catch
					{
						val = null;
					}
					if (val == null)
					{
						continue;
					}
					string text4 = ((object)(val["Key"] ?? val["key"]))?.ToString();
					if (string.IsNullOrWhiteSpace(text4))
					{
						string text5 = Path.GetFileNameWithoutExtension(path);
						int num = text5.LastIndexOf("__", StringComparison.Ordinal);
						if (num > 0)
						{
							text5 = text5.Substring(0, num);
						}
						text4 = text5;
					}
					UnnamedNpcPersonaProfile unnamedNpcPersonaProfile = new UnnamedNpcPersonaProfile();
					unnamedNpcPersonaProfile.Personality = ((object)(val["Personality"] ?? val["personality"]))?.ToString();
					unnamedNpcPersonaProfile.Background = ((object)(val["Background"] ?? val["background"]))?.ToString();
					unnamedNpcPersonaProfile.CultureId = ((object)(val["CultureId"] ?? val["cultureId"] ?? val["culture_id"]))?.ToString();
					unnamedNpcPersonaProfile.Rank = ((object)(val["Rank"] ?? val["rank"]))?.ToString();
					unnamedNpcPersonaProfile.Name = ((object)(val["Name"] ?? val["name"]))?.ToString();
					unnamedNpcPersonaProfile.TroopId = ((object)(val["TroopId"] ?? val["troopId"] ?? val["troop_id"]))?.ToString();
					text4 = (text4 ?? "").Trim().ToLower();
					if (!string.IsNullOrEmpty(text4))
					{
						dictionary[text4] = unnamedNpcPersonaProfile;
					}
				}
				catch
				{
				}
			}
			LoadUnnamedProfilesIfNeeded();
			lock (_unnamedProfilesLock)
			{
				if (_unnamedProfiles == null)
				{
					_unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
				}
				foreach (KeyValuePair<string, UnnamedNpcPersonaProfile> item in dictionary)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						_unnamedProfiles.Remove(item.Key);
						_unnamedProfiles[item.Key] = item.Value;
					}
				}
				SaveUnnamedProfilesUnsafe();
			}
		}
		catch
		{
		}
	}

	public static void ImportUnnamedPersonaFromDir(string importRootDir, bool overwriteExisting)
	{
		try
		{
			if (overwriteExisting)
			{
				ImportUnnamedPersonaFromDir(importRootDir);
			}
			else
			{
				if (string.IsNullOrWhiteSpace(importRootDir))
				{
					return;
				}
				string text = importRootDir;
				if (!Directory.Exists(text))
				{
					return;
				}
				string text2 = Path.Combine(text, "unnamed_persona");
				if (Directory.Exists(text2))
				{
					text = text2;
				}
				string[] files = Directory.GetFiles(text, "*.json");
				Dictionary<string, UnnamedNpcPersonaProfile> dictionary = new Dictionary<string, UnnamedNpcPersonaProfile>();
				string[] array = files;
				string[] array2 = array;
				foreach (string path in array2)
				{
					try
					{
						string text3 = File.ReadAllText(path);
						if (string.IsNullOrWhiteSpace(text3))
						{
							continue;
						}
						JObject val = null;
						try
						{
							val = JObject.Parse(text3);
						}
						catch
						{
							val = null;
						}
						if (val == null)
						{
							continue;
						}
						string text4 = ((object)(val["Key"] ?? val["key"]))?.ToString();
						if (string.IsNullOrWhiteSpace(text4))
						{
							string text5 = Path.GetFileNameWithoutExtension(path);
							int num = text5.LastIndexOf("__", StringComparison.Ordinal);
							if (num > 0)
							{
								text5 = text5.Substring(0, num);
							}
							text4 = text5;
						}
						UnnamedNpcPersonaProfile unnamedNpcPersonaProfile = new UnnamedNpcPersonaProfile();
						unnamedNpcPersonaProfile.Personality = ((object)(val["Personality"] ?? val["personality"]))?.ToString();
						unnamedNpcPersonaProfile.Background = ((object)(val["Background"] ?? val["background"]))?.ToString();
						unnamedNpcPersonaProfile.CultureId = ((object)(val["CultureId"] ?? val["cultureId"] ?? val["culture_id"]))?.ToString();
						unnamedNpcPersonaProfile.Rank = ((object)(val["Rank"] ?? val["rank"]))?.ToString();
						unnamedNpcPersonaProfile.Name = ((object)(val["Name"] ?? val["name"]))?.ToString();
						unnamedNpcPersonaProfile.TroopId = ((object)(val["TroopId"] ?? val["troopId"] ?? val["troop_id"]))?.ToString();
						text4 = (text4 ?? "").Trim().ToLower();
						if (!string.IsNullOrEmpty(text4))
						{
							dictionary[text4] = unnamedNpcPersonaProfile;
						}
					}
					catch
					{
					}
				}
				LoadUnnamedProfilesIfNeeded();
				lock (_unnamedProfilesLock)
				{
					if (_unnamedProfiles == null)
					{
						_unnamedProfiles = new Dictionary<string, UnnamedNpcPersonaProfile>();
					}
					foreach (KeyValuePair<string, UnnamedNpcPersonaProfile> item in dictionary)
					{
						if (!string.IsNullOrEmpty(item.Key) && item.Value != null && !_unnamedProfiles.ContainsKey(item.Key))
						{
							_unnamedProfiles[item.Key] = item.Value;
						}
					}
					SaveUnnamedProfilesUnsafe();
					return;
				}
			}
		}
		catch
		{
		}
	}

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
				return JsonConvert.SerializeObject((object)unnamedNpcProfilesFile, (Formatting)(pretty ? 1 : 0));
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
			Dictionary<string, UnnamedNpcPersonaProfile> dictionary = (string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<UnnamedNpcProfilesFile>(json))?.Profiles ?? new Dictionary<string, UnnamedNpcPersonaProfile>();
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
