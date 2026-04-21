using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class CampaignSaveChunkHelper
{
	private const int StorageChunkMaxBytes = 240;

	private const int LegacyInlineStorageMaxBytes = 240;

	private const int MaxChunkCount = 262144;

	private const string StringChunkCountSuffix = "__af_chunk_count";

	private const string StringChunkKeyPrefix = "__af_chunk_";

	private const string DictionaryChunkCountPrefix = "__af_chunkcount__:";

	private const string DictionaryChunkValuePrefix = "__af_chunk__:";

	public static bool SafeSyncData<T>(IDataStore dataStore, string key, ref T data, string loggerTag = "SaveChunk")
	{
		try
		{
			return dataStore != null && dataStore.SyncData<T>(key, ref data);
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log(loggerTag ?? "SaveChunk", "[WARN] SyncData failed for key " + key + ": " + ex.Message);
			}
			catch
			{
			}
			return false;
		}
	}

	public static void SaveChunkedString(IDataStore dataStore, string key, string value, string loggerTag = "SaveChunk")
	{
		string text = value ?? "";
		List<string> list = SplitUtf8Chunks(text, 240);
		int data = list.Count;
		SafeSyncData(dataStore, key + "__af_chunk_count", ref data, loggerTag);
		for (int i = 0; i < list.Count; i++)
		{
			string data2 = list[i] ?? "";
			SafeSyncData(dataStore, key + "__af_chunk_" + i, ref data2, loggerTag);
		}
		string data3 = ((GetUtf8ByteCount(text) <= 240) ? text : "");
		SafeSyncData(dataStore, key, ref data3, loggerTag);
	}

	public static string LoadChunkedString(IDataStore dataStore, string key, string loggerTag = "SaveChunk")
	{
		string text = TryLoadChunkedString(dataStore, key, loggerTag);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string data = "";
		return SafeSyncData(dataStore, key, ref data, loggerTag) ? (data ?? "") : "";
	}

	public static Dictionary<string, string> FlattenStringDictionary(Dictionary<string, string> source)
	{
		Dictionary<string, string> dictionary = CreateCompatibleDictionary(source);
		if (source == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, string> item in source)
		{
			string text = item.Key ?? "";
			if (string.IsNullOrWhiteSpace(text) || item.Value == null)
			{
				continue;
			}
			if (GetUtf8ByteCount(item.Value) <= 240)
			{
				dictionary[text] = item.Value;
				continue;
			}
			List<string> list = SplitUtf8Chunks(item.Value, 240);
			dictionary["__af_chunkcount__:" + text] = list.Count.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < list.Count; i++)
			{
				dictionary[BuildDictionaryChunkKey(text, i)] = list[i] ?? "";
			}
		}
		return dictionary;
	}

	public static Dictionary<string, string> RestoreStringDictionary(Dictionary<string, string> stored, string loggerTag = "SaveChunk")
	{
		Dictionary<string, string> dictionary = CreateCompatibleDictionary(stored);
		if (stored == null || stored.Count == 0)
		{
			return dictionary;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> item in stored)
		{
			if (IsDictionaryChunkCountKey(item.Key))
			{
				string text = item.Key.Substring("__af_chunkcount__:".Length);
				if (!string.IsNullOrWhiteSpace(text) && int.TryParse(item.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result > 0)
				{
					hashSet.Add(text);
				}
			}
		}
		foreach (string item2 in hashSet)
		{
			if (TryRestoreChunkedDictionaryValue(stored, item2, out var value))
			{
				dictionary[item2] = value;
				continue;
			}
			try
			{
				Logger.Log(loggerTag ?? "SaveChunk", "[WARN] Restore chunked dictionary value failed for key " + item2);
			}
			catch
			{
			}
		}
		foreach (KeyValuePair<string, string> item3 in stored)
		{
			if (!string.IsNullOrWhiteSpace(item3.Key) && item3.Value != null && !IsDictionaryChunkCountKey(item3.Key) && !IsDictionaryChunkValueKey(item3.Key) && !hashSet.Contains(item3.Key))
			{
				dictionary[item3.Key] = item3.Value;
			}
		}
		return dictionary;
	}

	private static Dictionary<string, string> CreateCompatibleDictionary(Dictionary<string, string> source)
	{
		try
		{
			if (source != null)
			{
				return new Dictionary<string, string>(source.Comparer);
			}
		}
		catch
		{
		}
		return new Dictionary<string, string>();
	}

	private static string TryLoadChunkedString(IDataStore dataStore, string key, string loggerTag)
	{
		int data = 0;
		if (!SafeSyncData(dataStore, key + "__af_chunk_count", ref data, loggerTag) || data <= 0 || data > 262144)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < data; i++)
		{
			string data2 = "";
			if (!SafeSyncData(dataStore, key + "__af_chunk_" + i, ref data2, loggerTag))
			{
				return "";
			}
			stringBuilder.Append(data2 ?? "");
		}
		return stringBuilder.ToString();
	}

	private static int GetUtf8ByteCount(string value)
	{
		try
		{
			return Encoding.UTF8.GetByteCount(value ?? "");
		}
		catch
		{
			return 0;
		}
	}

	private static List<string> SplitUtf8Chunks(string value, int maxBytesPerChunk)
	{
		List<string> list = new List<string>();
		string text = value ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		int num = Math.Max(32, maxBytesPerChunk);
		StringBuilder stringBuilder = new StringBuilder();
		int num2 = 0;
		for (int i = 0; i < text.Length; i++)
		{
			int num3 = 1;
			if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
			{
				num3 = 2;
			}
			string value2 = text.Substring(i, num3);
			int utf8ByteCount = GetUtf8ByteCount(value2);
			if (stringBuilder.Length > 0 && num2 + utf8ByteCount > num)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				num2 = 0;
			}
			stringBuilder.Append(value2);
			num2 += utf8ByteCount;
			if (num3 == 2)
			{
				i++;
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	private static bool TryRestoreChunkedDictionaryValue(Dictionary<string, string> stored, string key, out string value)
	{
		value = "";
		if (stored == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (!stored.TryGetValue("__af_chunkcount__:" + key, out var value2) || !int.TryParse(value2, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0 || result > 262144)
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < result; i++)
		{
			if (!stored.TryGetValue(BuildDictionaryChunkKey(key, i), out var value3))
			{
				return false;
			}
			stringBuilder.Append(value3 ?? "");
		}
		value = stringBuilder.ToString();
		return true;
	}

	private static bool IsDictionaryChunkCountKey(string key)
	{
		return !string.IsNullOrEmpty(key) && key.StartsWith("__af_chunkcount__:", StringComparison.Ordinal);
	}

	private static bool IsDictionaryChunkValueKey(string key)
	{
		return !string.IsNullOrEmpty(key) && key.StartsWith("__af_chunk__:", StringComparison.Ordinal);
	}

	private static string BuildDictionaryChunkKey(string key, int index)
	{
		return "__af_chunk__:" + key + ":" + index.ToString(CultureInfo.InvariantCulture);
	}
}
