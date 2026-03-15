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
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

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
			PartyBase partyBase = agent.Origin?.BattleCombatant as PartyBase;
			text2 = partyBase?.MapFaction?.StringId ?? "";
			text3 = partyBase?.LeaderHero?.StringId ?? "";
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
				CharacterObject characterObject = agent?.Character as CharacterObject;
				string text4 = (characterObject?.Culture?.StringId ?? "neutral").ToLower();
				string text5 = ((characterObject != null && characterObject.IsSoldier) ? "soldier" : "commoner");
				string text6 = agent?.Name?.ToString() ?? "路人";
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
			string rawResp = ((await ShoutNetwork.CallApiWithMessages(messages, 256)) ?? "").Trim();
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
		CharacterObject co = agent.Character as CharacterObject;
		if (!(co?.IsHero ?? true))
		{
			string key = GetUnnamedKey(agent);
			if (!string.IsNullOrEmpty(key))
			{
				string cultureId = (co.Culture?.StringId ?? "neutral").ToLower();
				string rank = (co.IsSoldier ? "soldier" : "commoner");
				string name = agent.Name?.ToString() ?? "路人";
				string troopId = (co.StringId ?? "").ToLower();
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
			text = "野外";
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
				text = ((!text7.Contains("lordshall") && !text7.Contains("lord_hall") && !text7.Contains("lordhall") && !text7.Contains("lord") && !text7.Contains("keep")) ? (text7.Contains("tavern") ? "酒馆" : (text7.Contains("arena") ? "竞技场" : ((text7.Contains("prison") || text7.Contains("dungeon")) ? "地牢" : (string.IsNullOrEmpty(text2) ? "野外" : "街道")))) : "领主大厅");
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
			return "英雄";
		}
		try
		{
			if (hero.IsLord || hero.Occupation == Occupation.Lord)
			{
				return "领主";
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsNotable || fromNotables || hero.Occupation == Occupation.Headman || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.Preacher)
			{
				return "头人";
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsWanderer || hero.Occupation == Occupation.Wanderer)
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
		switch ((type ?? "").Trim())
		{
		case "领主":
			return 4;
		case "头人":
			return 3;
		case "流浪者":
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
			action(settlement.OwnerClan?.Leader, arg2: false);
			action(settlement.Town?.Governor, arg2: false);
			try
			{
				MBReadOnlyList<Hero> notables = settlement.Notables;
				if (notables != null)
				{
					foreach (Hero item in notables)
					{
						action(item, arg2: true);
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
				list2.Add("[" + value2 + "]" + value);
			}
			string text5 = "当前定居点HeroNPC：" + string.Join("；", list2);
			if (count > maxCount)
			{
				text5 = text5 + "；等" + count + "人";
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
				list.Add("该定居点属" + text5 + "文化");
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
			if (!string.IsNullOrWhiteSpace(factionRelationSummary))
			{
				list.Add(factionRelationSummary);
			}
			if ((settlement.IsTown || settlement.IsCastle) && hero != null && !IsHeroInSettlementForPrompt(hero, settlement))
			{
				list.Add("当前统治者不在此定居点");
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
		return settlement.IsTown ? "城镇" : (settlement.IsCastle ? "城堡" : (settlement.IsVillage ? "村庄" : ((!settlement.IsFortification) ? "定居点" : "要塞")));
	}

	private static string BuildFactionRelationSummary(IFaction faction, Hero perspectiveHero)
	{
		try
		{
			string playerRelation = GetFactionRelationLabel(faction, Hero.MainHero?.Clan?.Kingdom ?? Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.Kingdom ?? Clan.PlayerClan?.MapFaction);
			string npcRelation = GetFactionRelationLabel(faction, perspectiveHero?.Clan?.Kingdom ?? perspectiveHero?.MapFaction);
			if (string.IsNullOrWhiteSpace(npcRelation))
			{
				return BuildSingleRelationPhrase("玩家", playerRelation);
			}
			if (string.Equals(playerRelation, npcRelation, StringComparison.Ordinal))
			{
				return BuildSharedRelationPhrase(playerRelation);
			}
			return BuildSplitRelationPhrase(npcRelation, playerRelation);
		}
		catch
		{
			return "对你和玩家都保持中立";
		}
	}

	private static string BuildSingleRelationPhrase(string targetName, string relation)
	{
		switch ((relation ?? "").Trim())
		{
		case "敌对":
			return "是" + targetName + "的敌人";
		case "友方":
			return "是" + targetName + "的友方势力";
		default:
			return "与" + targetName + "保持中立";
		}
	}

	private static string BuildSharedRelationPhrase(string relation)
	{
		switch ((relation ?? "").Trim())
		{
		case "敌对":
			return "是你和玩家的敌人";
		case "友方":
			return "是你和玩家的友方势力";
		default:
			return "对你和玩家都保持中立";
		}
	}

	private static string BuildSplitRelationPhrase(string npcRelation, string playerRelation)
	{
		return BuildSingleRelationPhrase("你", npcRelation) + "，但" + BuildSingleRelationPhrase("玩家", playerRelation);
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
			string text2 = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
			{
				return "友方";
			}
			if (ReferenceEquals(faction, referenceFaction))
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
				list.Add("文化：" + cultureName);
			}
			if (!string.IsNullOrEmpty(text3))
			{
				list.Add("领主：" + text3);
			}
			if (!string.IsNullOrEmpty(text4))
			{
				list.Add("家族：" + text4);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add("隶属：" + text2);
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
		List<Agent> list = new List<Agent>();
		if (Mission.Current == null || Agent.Main == null)
		{
			return list;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent == Agent.Main || !agent.IsActive() || !agent.IsHuman)
			{
				continue;
			}
			float num = agent.Position.Distance(position);
			if (num <= 4f)
			{
				Vec3 v = agent.Position - position;
				v.Normalize();
				if (Vec3.DotProduct(lookDirection, v) > 0.70710677f)
				{
					list.Add(agent);
				}
			}
		}
		return list;
	}

	public static Agent GetClosestFacingAgent(float maxDistance)
	{
		if (Mission.Current == null || Agent.Main == null)
		{
			return null;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		Agent result = null;
		float num = maxDistance;
		const float strictCrosshairDotThreshold = 0.9f;
		const float npcFront120DotThreshold = 0.5f;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent == Agent.Main || !agent.IsActive() || !agent.IsHuman)
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

	public static NpcDataPacket ExtractNpcData(Agent agent)
	{
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
					npcDataPacket.Age = characterObject.Age;
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
						npcDataPacket.RoleDesc = "领主";
					}
					else if (hero.IsWanderer)
					{
						npcDataPacket.RoleDesc = "流浪者";
					}
					else if (hero.IsNotable)
					{
						npcDataPacket.RoleDesc = "要人";
					}
					else
					{
						npcDataPacket.RoleDesc = "英雄";
					}
					MyBehavior.GetNpcPersonaForExternal(hero, out var personality, out var background);
					if (!string.IsNullOrWhiteSpace(personality))
					{
						npcDataPacket.PersonalityDesc = personality.Trim();
					}
					if (!string.IsNullOrWhiteSpace(background))
					{
						npcDataPacket.BackgroundDesc = background.Trim();
					}
				}
				else if (characterObject.Occupation == Occupation.Lord)
				{
					npcDataPacket.RoleDesc = "领主";
				}
				else if (characterObject.Occupation == Occupation.Wanderer)
				{
					npcDataPacket.RoleDesc = "流浪者";
				}
				else
				{
					npcDataPacket.RoleDesc = "英雄";
				}
			}
			else if (characterObject.IsSoldier)
			{
				npcDataPacket.RoleDesc = "士兵";
			}
			if (!npcDataPacket.IsHero)
			{
				npcDataPacket.UnnamedKey = GetUnnamedKey(agent);
				npcDataPacket.TroopId = (characterObject.StringId ?? "").ToLower();
				npcDataPacket.UnnamedRank = (characterObject.IsSoldier ? "soldier" : "commoner");
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
		return npcDataPacket;
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
		if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
		{
			DuelBehavior.PrepareDuel(characterObject.HeroObject, 3f);
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
			string text2 = System.IO.Path.Combine(text, "unnamed_persona");
			if (Directory.Exists(text2))
			{
				text = text2;
			}
			string[] files = Directory.GetFiles(text, "*.json");
			Dictionary<string, UnnamedNpcPersonaProfile> dictionary = new Dictionary<string, UnnamedNpcPersonaProfile>();
			string[] array = files;
			foreach (string path in array)
			{
				try
				{
					string text3 = File.ReadAllText(path);
					if (string.IsNullOrWhiteSpace(text3))
					{
						continue;
					}
					JObject jObject = null;
					try
					{
						jObject = JObject.Parse(text3);
					}
					catch
					{
						jObject = null;
					}
					if (jObject == null)
					{
						continue;
					}
					string text4 = (jObject["Key"] ?? jObject["key"])?.ToString();
					if (string.IsNullOrWhiteSpace(text4))
					{
						string text5 = System.IO.Path.GetFileNameWithoutExtension(path);
						int num = text5.LastIndexOf("__", StringComparison.Ordinal);
						if (num > 0)
						{
							text5 = text5.Substring(0, num);
						}
						text4 = text5;
					}
					UnnamedNpcPersonaProfile unnamedNpcPersonaProfile = new UnnamedNpcPersonaProfile();
					unnamedNpcPersonaProfile.Personality = (jObject["Personality"] ?? jObject["personality"])?.ToString();
					unnamedNpcPersonaProfile.Background = (jObject["Background"] ?? jObject["background"])?.ToString();
					unnamedNpcPersonaProfile.CultureId = (jObject["CultureId"] ?? jObject["cultureId"] ?? jObject["culture_id"])?.ToString();
					unnamedNpcPersonaProfile.Rank = (jObject["Rank"] ?? jObject["rank"])?.ToString();
					unnamedNpcPersonaProfile.Name = (jObject["Name"] ?? jObject["name"])?.ToString();
					unnamedNpcPersonaProfile.TroopId = (jObject["TroopId"] ?? jObject["troopId"] ?? jObject["troop_id"])?.ToString();
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
				string text2 = System.IO.Path.Combine(text, "unnamed_persona");
				if (Directory.Exists(text2))
				{
					text = text2;
				}
				string[] files = Directory.GetFiles(text, "*.json");
				Dictionary<string, UnnamedNpcPersonaProfile> dictionary = new Dictionary<string, UnnamedNpcPersonaProfile>();
				string[] array = files;
				foreach (string path in array)
				{
					try
					{
						string text3 = File.ReadAllText(path);
						if (string.IsNullOrWhiteSpace(text3))
						{
							continue;
						}
						JObject jObject = null;
						try
						{
							jObject = JObject.Parse(text3);
						}
						catch
						{
							jObject = null;
						}
						if (jObject == null)
						{
							continue;
						}
						string text4 = (jObject["Key"] ?? jObject["key"])?.ToString();
						if (string.IsNullOrWhiteSpace(text4))
						{
							string text5 = System.IO.Path.GetFileNameWithoutExtension(path);
							int num = text5.LastIndexOf("__", StringComparison.Ordinal);
							if (num > 0)
							{
								text5 = text5.Substring(0, num);
							}
							text4 = text5;
						}
						UnnamedNpcPersonaProfile unnamedNpcPersonaProfile = new UnnamedNpcPersonaProfile();
						unnamedNpcPersonaProfile.Personality = (jObject["Personality"] ?? jObject["personality"])?.ToString();
						unnamedNpcPersonaProfile.Background = (jObject["Background"] ?? jObject["background"])?.ToString();
						unnamedNpcPersonaProfile.CultureId = (jObject["CultureId"] ?? jObject["cultureId"] ?? jObject["culture_id"])?.ToString();
						unnamedNpcPersonaProfile.Rank = (jObject["Rank"] ?? jObject["rank"])?.ToString();
						unnamedNpcPersonaProfile.Name = (jObject["Name"] ?? jObject["name"])?.ToString();
						unnamedNpcPersonaProfile.TroopId = (jObject["TroopId"] ?? jObject["troopId"] ?? jObject["troop_id"])?.ToString();
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
