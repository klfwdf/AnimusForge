using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

internal static class VoiceMapper
{
	private static Dictionary<string, List<string>> _voicePools;

	private static string _fallbackVoice = "";

	private static bool _loaded;

	private static readonly object _loadLock = new object();

	private static readonly Dictionary<int, string> _sceneCache = new Dictionary<int, string>();

	private static readonly object _cacheLock = new object();

	private static readonly Random _rng = new Random();

	public static readonly string[] AllGroupKeys = new string[6] { "male_young", "male_middle", "male_old", "female_young", "female_middle", "female_old" };

	public static string ResolveVoiceId(string heroStringId, bool isFemale, float age, int agentIndex)
	{
		EnsureLoaded();
		if (_voicePools == null || _voicePools.Count == 0)
		{
			return null;
		}
		string groupKey = GetGroupKey(isFemale, age);
		List<string> list = null;
		if (!string.IsNullOrEmpty(groupKey) && _voicePools.ContainsKey(groupKey))
		{
			list = _voicePools[groupKey];
		}
		if (list == null || list.Count == 0)
		{
			string value = (isFemale ? "female" : "male");
			foreach (KeyValuePair<string, List<string>> voicePool in _voicePools)
			{
				if (voicePool.Key.StartsWith(value, StringComparison.OrdinalIgnoreCase) && voicePool.Value != null && voicePool.Value.Count > 0)
				{
					list = voicePool.Value;
					break;
				}
			}
		}
		if (list == null || list.Count == 0)
		{
			return string.IsNullOrWhiteSpace(_fallbackVoice) ? null : _fallbackVoice;
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		if (!string.IsNullOrEmpty(heroStringId))
		{
			int num = Math.Abs(heroStringId.GetHashCode());
			return list[num % list.Count];
		}
		if (agentIndex >= 0)
		{
			lock (_cacheLock)
			{
				if (_sceneCache.TryGetValue(agentIndex, out var value2))
				{
					return value2;
				}
				string text = list[_rng.Next(list.Count)];
				_sceneCache[agentIndex] = text;
				return text;
			}
		}
		return list[_rng.Next(list.Count)];
	}

	public static string ResolveVoiceId(Hero hero)
	{
		if (hero == null)
		{
			return null;
		}
		bool isFemale = hero.IsFemale;
		float age = hero.Age;
		string stringId = ((MBObjectBase)hero).StringId;
		return ResolveVoiceId(stringId, isFemale, age, -1);
	}

	public static string ResolveVoiceIdForNonHero(bool isFemale, float age, int agentIndex)
	{
		return ResolveVoiceId(null, isFemale, age, agentIndex);
	}

	public static void ClearSceneCache()
	{
		lock (_cacheLock)
		{
			_sceneCache.Clear();
		}
		Logger.Log("VoiceMapper", "场景声音缓存已清空");
	}

	public static void ReloadConfig()
	{
		EnsureLoaded();
		ClearSceneCache();
		Logger.Log("VoiceMapper", "运行时 VoiceMapping 已刷新（来源=当前存档/本次导入）");
	}

	public static void SetPreferredExportFolder(string exportFolderPath)
	{
	}

	public static string GetPreferredExportFolder()
	{
		return "";
	}

	public static string GetGroupDisplayName(string key)
	{
		string text = (key ?? "").ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"male_young" => "青年男声 (<30岁)", 
			"male_middle" => "中年男声 (30-50岁)", 
			"male_old" => "老年男声 (>50岁)", 
			"female_young" => "青年女声 (<30岁)", 
			"female_middle" => "中年女声 (30-50岁)", 
			"female_old" => "老年女声 (>50岁)", 
			_ => key ?? "", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static List<string> GetVoicesForGroup(string groupKey)
	{
		EnsureLoaded();
		if (_voicePools == null)
		{
			return new List<string>();
		}
		if (_voicePools.TryGetValue(groupKey, out var value))
		{
			return new List<string>(value);
		}
		return new List<string>();
	}

	public static string GetFallbackVoice()
	{
		EnsureLoaded();
		return _fallbackVoice ?? "";
	}

	public static void AddVoiceToGroup(string groupKey, string voiceId)
	{
		EnsureLoaded();
		string text = (voiceId ?? "").Trim();
		if (!string.IsNullOrEmpty(text))
		{
			if (_voicePools == null)
			{
				_voicePools = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			}
			if (!_voicePools.ContainsKey(groupKey))
			{
				_voicePools[groupKey] = new List<string>();
			}
			if (!_voicePools[groupKey].Contains(text))
			{
				_voicePools[groupKey].Add(text);
			}
			ClearSceneCache();
			Logger.Log("VoiceMapper", "添加声音: group=" + groupKey + " voice=" + text);
		}
	}

	public static void RemoveVoiceFromGroup(string groupKey, string voiceId)
	{
		EnsureLoaded();
		string text = (voiceId ?? "").Trim();
		if (!string.IsNullOrEmpty(text) && _voicePools != null && _voicePools.ContainsKey(groupKey))
		{
			_voicePools[groupKey].Remove(text);
			if (_voicePools[groupKey].Count == 0)
			{
				_voicePools.Remove(groupKey);
			}
			ClearSceneCache();
			Logger.Log("VoiceMapper", "删除声音: group=" + groupKey + " voice=" + text);
		}
	}

	public static void SetFallbackVoice(string voiceId)
	{
		EnsureLoaded();
		_fallbackVoice = (voiceId ?? "").Trim();
		ClearSceneCache();
		Logger.Log("VoiceMapper", "设置 fallback: " + _fallbackVoice);
	}

	public static int GetTotalVoiceCount()
	{
		EnsureLoaded();
		if (_voicePools == null)
		{
			return 0;
		}
		int num = 0;
		foreach (KeyValuePair<string, List<string>> voicePool in _voicePools)
		{
			if (voicePool.Value != null)
			{
				num += voicePool.Value.Count;
			}
		}
		return num;
	}

	public static string ExportMappingJson(bool pretty = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		EnsureLoaded();
		JObject val = new JObject();
		string[] allGroupKeys = AllGroupKeys;
		string[] array = allGroupKeys;
		foreach (string text in array)
		{
			JArray val2 = new JArray();
			if (_voicePools != null && _voicePools.TryGetValue(text, out var value) && value != null)
			{
				for (int j = 0; j < value.Count; j++)
				{
					string text2 = (value[j] ?? "").Trim();
					if (!string.IsNullOrEmpty(text2))
					{
						val2.Add(JToken.op_Implicit(text2));
					}
				}
			}
			val[text] = (JToken)(object)val2;
		}
		val["fallback"] = JToken.op_Implicit(_fallbackVoice ?? "");
		return ((JToken)val).ToString((Formatting)(pretty ? 1 : 0), Array.Empty<JsonConverter>());
	}

	public static bool ImportMappingJson(string json, bool overwriteExisting = true, bool saveToFile = false)
	{
		EnsureLoaded();
		try
		{
			string fallback;
			Dictionary<string, List<string>> dictionary = ParseVoiceMappingJson(json, out fallback);
			if (_voicePools == null)
			{
				_voicePools = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			}
			if (overwriteExisting)
			{
				_voicePools.Clear();
				foreach (KeyValuePair<string, List<string>> item in dictionary)
				{
					if (item.Value != null && item.Value.Count > 0)
					{
						_voicePools[item.Key] = new List<string>(item.Value);
					}
				}
				_fallbackVoice = fallback ?? "";
			}
			else
			{
				string[] allGroupKeys = AllGroupKeys;
				string[] array = allGroupKeys;
				foreach (string key in array)
				{
					if (!dictionary.TryGetValue(key, out var value) || value == null || value.Count <= 0)
					{
						continue;
					}
					if (!_voicePools.TryGetValue(key, out var value2) || value2 == null)
					{
						value2 = new List<string>();
						_voicePools[key] = value2;
					}
					for (int j = 0; j < value.Count; j++)
					{
						string text = (value[j] ?? "").Trim();
						if (!string.IsNullOrEmpty(text) && !value2.Contains(text))
						{
							value2.Add(text);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(_fallbackVoice) && !string.IsNullOrWhiteSpace(fallback))
				{
					_fallbackVoice = fallback.Trim();
				}
			}
			ClearSceneCache();
			Logger.Log("VoiceMapper", string.Format("导入 VoiceMapping 成功: mode={0}, groups={1}, totalVoices={2}, fallback={3}", overwriteExisting ? "overwrite" : "merge", _voicePools.Count, GetTotalVoiceCount(), _fallbackVoice));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("VoiceMapper", "[ERROR] 导入 VoiceMapping JSON 失败: " + ex.Message);
			return false;
		}
	}

	public static bool ImportMappingFromFile(string filePath, bool overwriteExisting = true, bool saveToFile = false)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			{
				Logger.Log("VoiceMapper", "[WARN] 导入 VoiceMapping 失败：文件不存在");
				return false;
			}
			string json = File.ReadAllText(filePath, Encoding.UTF8);
			return ImportMappingJson(json, overwriteExisting, saveToFile);
		}
		catch (Exception ex)
		{
			Logger.Log("VoiceMapper", "[ERROR] 从文件导入 VoiceMapping 失败: " + ex.Message);
			return false;
		}
	}

	private static void SaveConfig()
	{
	}

	private static Dictionary<string, List<string>> ParseVoiceMappingJson(string json, out string fallback)
	{
		fallback = "";
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(json))
		{
			throw new Exception("JSON 为空");
		}
		JObject val = JObject.Parse(json);
		string[] allGroupKeys = AllGroupKeys;
		string[] array = allGroupKeys;
		foreach (string text in array)
		{
			List<string> list = new List<string>();
			JToken obj = val[text];
			JArray val2 = (JArray)(object)((obj is JArray) ? obj : null);
			if (val2 != null && ((JContainer)val2).Count > 0)
			{
				for (int j = 0; j < ((JContainer)val2).Count; j++)
				{
					string text2 = ((val2[j] != null) ? ((object)val2[j]).ToString() : "").Trim();
					if (!string.IsNullOrEmpty(text2) && !list.Contains(text2))
					{
						list.Add(text2);
					}
				}
			}
			if (list.Count > 0)
			{
				dictionary[text] = list;
			}
		}
		if (val["fallback"] != null)
		{
			fallback = (((object)val["fallback"]).ToString() ?? "").Trim();
		}
		return dictionary;
	}

	private static string GetGroupKey(bool isFemale, float age)
	{
		string text = (isFemale ? "female" : "male");
		string text2 = ((age < 30f) ? "young" : ((!(age < 50f)) ? "old" : "middle"));
		return text + "_" + text2;
	}

	private static void EnsureLoaded()
	{
		if (_loaded)
		{
			return;
		}
		lock (_loadLock)
		{
			if (!_loaded)
			{
				try
				{
					LoadConfig();
				}
				catch (Exception ex)
				{
					Logger.Log("VoiceMapper", "[ERROR] 加载 VoiceMapping.json 失败: " + ex.Message);
				}
				_loaded = true;
			}
		}
	}

	private static void LoadConfig()
	{
		_voicePools = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		_fallbackVoice = "";
		Logger.Log("VoiceMapper", "运行时 VoiceMapping 已初始化（外部 JSON 仅用于手动导入/导出）");
	}
}
