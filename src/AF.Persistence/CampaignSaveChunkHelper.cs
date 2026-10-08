using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Collections;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class CampaignSaveChunkHelper
{
    // Opt-in protocol. The existing permissive readers/writers below stay unchanged.
    internal enum StrictReadStatus { Absent, Complete, InvalidManifest, MissingChunk, InvalidChunk, ReadFailed }
    internal sealed class StrictStringRead
    {
        internal StrictReadStatus Status;
        internal string Text, Error;
        internal bool EvidenceComplete;
        internal readonly Dictionary<string, object> Records = new Dictionary<string, object>(StringComparer.Ordinal);
    }
    private static readonly Dictionary<Type, FieldInfo> StrictRecordFields = new Dictionary<Type, FieldInfo>();

    // IDataStore has no key enumeration. Native BehaviorSaveData owns this exact
    // dictionary on both supported lines. Resolve its field once per store type,
    // only at SyncData; never retain a datastore, Campaign, or raw values statically.
    internal static StrictStringRead LoadChunkedStringStrict(IDataStore store, string key)
    {
        var result = new StrictStringRead { Status = StrictReadStatus.ReadFailed };
        try
        {
            if (store == null || !store.IsLoading) throw new InvalidOperationException("load_store_unavailable");
            FieldInfo field;
            Type type = store.GetType();
            lock (StrictRecordFields)
            {
                if (!StrictRecordFields.TryGetValue(type, out field))
                {
                    field = type.GetField("_records", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    StrictRecordFields[type] = field;
                }
            }
            if (!(field?.GetValue(store) is IDictionary records))
                throw new InvalidOperationException("record_namespace_unavailable");
            var keys = new List<string>();
            foreach (object rawKey in records.Keys)
                if (rawKey is string name && (name == key || name.StartsWith(key + StringChunkKeyPrefix, StringComparison.Ordinal))) keys.Add(name);
            foreach (string name in keys)
            {
                object value = null;
                if (!store.SyncData(name, ref value)) throw new InvalidOperationException("record_disappeared");
                if (value != null && !(value is string) && !(value is int))
                    throw new InvalidOperationException("unsupported_record_type");
                result.Records.Add(name, value);
            }
            result.EvidenceComplete = true;
            bool hasCount = result.Records.TryGetValue(key + StringChunkCountSuffix, out object rawCount);
            bool hasInline = result.Records.TryGetValue(key, out object rawInline);
            if (!hasCount && result.Records.Count == 0) { result.Status = StrictReadStatus.Absent; return result; }
            if (hasCount && (!(rawCount is int) || (int)rawCount < 0 || (int)rawCount > MaxChunkCount))
                return StrictFailure(result, StrictReadStatus.InvalidManifest, "invalid_chunk_count");
            int count = hasCount ? (int)rawCount : 0;
            if (count == 0)
            {
                int expected = (hasCount ? 1 : 0) + (hasInline ? 1 : 0);
                if (result.Records.Count != expected)
                    return StrictFailure(result, StrictReadStatus.InvalidManifest, "orphan_chunk_keys");
                if (!hasInline || !(rawInline is string inline) || string.IsNullOrWhiteSpace(inline))
                    return StrictFailure(result, StrictReadStatus.InvalidChunk, "declared_empty_payload");
                result.Text = inline; result.Status = StrictReadStatus.Complete; return result;
            }
            var text = new StringBuilder();
            foreach (string name in result.Records.Keys)
            {
                if (name == key || name == key + StringChunkCountSuffix) continue;
                string suffix = name.Substring((key + StringChunkKeyPrefix).Length);
                if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int index) || index < 0 || index >= count)
                    return StrictFailure(result, StrictReadStatus.InvalidManifest, "unexpected_chunk_key");
            }
            for (int i = 0; i < count; i++)
            {
                if (!result.Records.TryGetValue(key + StringChunkKeyPrefix + i, out object value))
                    return StrictFailure(result, StrictReadStatus.MissingChunk, "missing_chunk_" + i);
                if (!(value is string chunk) || chunk.Length == 0)
                    return StrictFailure(result, StrictReadStatus.InvalidChunk, "invalid_chunk_" + i);
                text.Append(chunk);
            }
            result.Text = text.ToString(); result.Status = StrictReadStatus.Complete; return result;
        }
        catch (Exception ex)
        {
            result.EvidenceComplete = false;
            result.Error = "strict_read_failed:" + ex.GetType().Name;
            return result;
        }
    }
    private static StrictStringRead StrictFailure(StrictStringRead result, StrictReadStatus status, string error)
    { result.Status = status; result.Error = error; result.Text = null; return result; }

    internal static void SaveChunkedStringStrict(IDataStore store, string key, string value)
    {
        if (store == null || !store.IsSaving) throw new InvalidOperationException("save_store_unavailable");
        string text = value ?? "";
        var chunks = SplitUtf8Chunks(text, StorageChunkMaxBytes);
        int count = chunks.Count; WriteRequired(store, key + StringChunkCountSuffix, count);
        for (int i = 0; i < count; i++) WriteRequired(store, key + StringChunkKeyPrefix + i, chunks[i]);
        WriteRequired(store, key, GetUtf8ByteCount(text) <= LegacyInlineStorageMaxBytes ? text : "");
    }
    internal static void ReplaySafeRawRecords(IDataStore store, IDictionary<string, object> records, string legacyKey)
    {
        foreach (var pair in records)
        {
            if (GetUtf8ByteCount(pair.Key) > StorageChunkMaxBytes) continue;
            // The quarantine envelope preserves ALL original bytes and key/type
            // identity. A long original scalar must not recreate a broken .sav.
            if (pair.Value is string value && GetUtf8ByteCount(value) >
                (pair.Key == legacyKey ? LegacyInlineStorageMaxBytes : StorageChunkMaxBytes)) continue;
            WriteRequired(store, pair.Key, pair.Value);
        }
    }
    private static void WriteRequired<T>(IDataStore store, string key, T value)
    {
        if (!store.SyncData(key, ref value)) throw new InvalidOperationException("save_write_rejected:" + key);
    }
	// TaleWorlds string save entries use a signed short data length; keep chunks well below 32767 bytes.
	private const int StorageChunkMaxBytes = 12000;

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
			return dataStore != null && dataStore.SyncData(key, ref data);
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
		List<string> list = SplitUtf8Chunks(text, StorageChunkMaxBytes);
		LogChunkedStringSaveStats(key, text, list.Count, loggerTag);
		int count = list.Count;
		SafeSyncData(dataStore, key + StringChunkCountSuffix, ref count, loggerTag);
		for (int i = 0; i < list.Count; i++)
		{
			string text2 = list[i] ?? "";
			SafeSyncData(dataStore, key + StringChunkKeyPrefix + i, ref text2, loggerTag);
		}
		string text3 = (GetUtf8ByteCount(text) <= LegacyInlineStorageMaxBytes) ? text : "";
		SafeSyncData(dataStore, key, ref text3, loggerTag);
	}

	public static string LoadChunkedString(IDataStore dataStore, string key, string loggerTag = "SaveChunk")
	{
		string text = TryLoadChunkedString(dataStore, key, loggerTag);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = "";
		return SafeSyncData(dataStore, key, ref text2, loggerTag) ? (text2 ?? "") : "";
	}

	public static Dictionary<string, string> FlattenStringDictionary(Dictionary<string, string> source)
	{
		return FlattenStringDictionary(source, null, "SaveChunk");
	}

	public static Dictionary<string, string> FlattenStringDictionary(Dictionary<string, string> source, string saveKey, string loggerTag = "SaveChunk")
	{
		Dictionary<string, string> dictionary = CreateCompatibleDictionary(source);
		if (source == null)
		{
			LogDictionarySaveStats(saveKey, source, dictionary, loggerTag);
			return dictionary;
		}
		foreach (KeyValuePair<string, string> item in source)
		{
			string text = item.Key ?? "";
			if (string.IsNullOrWhiteSpace(text) || item.Value == null)
			{
				continue;
			}
			if (GetUtf8ByteCount(item.Value) <= StorageChunkMaxBytes)
			{
				dictionary[text] = item.Value;
				continue;
			}
			List<string> list = SplitUtf8Chunks(item.Value, StorageChunkMaxBytes);
			dictionary[DictionaryChunkCountPrefix + text] = list.Count.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < list.Count; i++)
			{
				dictionary[BuildDictionaryChunkKey(text, i)] = list[i] ?? "";
			}
		}
		LogDictionarySaveStats(saveKey, source, dictionary, loggerTag);
		return dictionary;
	}

	public static int GetUtf8ByteCountForDiagnostics(string value)
	{
		return GetUtf8ByteCount(value);
	}

	public static void LogRawJsonSaveStats(string saveKey, string source, string json, string detail = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(saveKey) || !Logger.IsModLogicEnabled)
			{
				return;
			}
			string text = json ?? "";
			int bytes = GetUtf8ByteCount(text);
			Logger.Log("SaveSize", "save_size key=" + saveKey
				+ " kind=json"
				+ " source=" + NormalizeDiagnosticToken(source)
				+ " chars=" + text.Length
				+ " bytes=" + bytes
				+ (string.IsNullOrWhiteSpace(detail) ? "" : (" " + detail.Trim())));
		}
		catch
		{
		}
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
			if (!IsDictionaryChunkCountKey(item.Key))
			{
				continue;
			}
			string text = item.Key.Substring(DictionaryChunkCountPrefix.Length);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (int.TryParse(item.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result > 0)
			{
				hashSet.Add(text);
			}
		}
		foreach (string item2 in hashSet)
		{
			if (TryRestoreChunkedDictionaryValue(stored, item2, out var value))
			{
				dictionary[item2] = value;
			}
			else
			{
				try
				{
					Logger.Log(loggerTag ?? "SaveChunk", "[WARN] Restore chunked dictionary value failed for key " + item2);
				}
				catch
				{
				}
			}
		}
		foreach (KeyValuePair<string, string> item3 in stored)
		{
			if (string.IsNullOrWhiteSpace(item3.Key) || item3.Value == null)
			{
				continue;
			}
			if (IsDictionaryChunkCountKey(item3.Key) || IsDictionaryChunkValueKey(item3.Key))
			{
				continue;
			}
			if (hashSet.Contains(item3.Key))
			{
				continue;
			}
			dictionary[item3.Key] = item3.Value;
		}
		return dictionary;
	}

	private static void LogChunkedStringSaveStats(string saveKey, string value, int chunkCount, string loggerTag)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(saveKey) || !Logger.IsModLogicEnabled)
			{
				return;
			}
			string text = value ?? "";
			int bytes = GetUtf8ByteCount(text);
			int storedEntries = chunkCount + 1 + ((bytes <= LegacyInlineStorageMaxBytes) ? 1 : 0);
			int storedStringBytes = bytes + GetUtf8ByteCount(saveKey + StringChunkCountSuffix);
			if (bytes <= LegacyInlineStorageMaxBytes)
			{
				storedStringBytes += bytes + GetUtf8ByteCount(saveKey);
			}
			for (int i = 0; i < chunkCount; i++)
			{
				storedStringBytes += GetUtf8ByteCount(saveKey + StringChunkKeyPrefix + i.ToString(CultureInfo.InvariantCulture));
			}
			Logger.Log("SaveSize", "save_size key=" + saveKey
				+ " kind=chunked_string"
				+ " source=" + NormalizeDiagnosticToken(loggerTag)
				+ " chars=" + text.Length
				+ " bytes=" + bytes
				+ " chunks=" + chunkCount
				+ " storedEntries=" + storedEntries
				+ " approxStoredStringBytes=" + storedStringBytes);
		}
		catch
		{
		}
	}

	private static void LogDictionarySaveStats(string saveKey, Dictionary<string, string> raw, Dictionary<string, string> stored, string loggerTag)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(saveKey) || !Logger.IsModLogicEnabled)
			{
				return;
			}
			int rawEntries = raw?.Count ?? 0;
			int storedEntries = stored?.Count ?? 0;
			int rawKeyBytes = 0;
			int rawValueBytes = 0;
			int maxValueBytes = 0;
			string maxValueKey = "";
			int chunkedItems = 0;
			int chunkValueEntries = 0;
			if (raw != null)
			{
				foreach (KeyValuePair<string, string> item in raw)
				{
					string key = item.Key ?? "";
					string value = item.Value ?? "";
					int keyBytes = GetUtf8ByteCount(key);
					int valueBytes = GetUtf8ByteCount(value);
					rawKeyBytes += keyBytes;
					rawValueBytes += valueBytes;
					if (valueBytes > maxValueBytes)
					{
						maxValueBytes = valueBytes;
						maxValueKey = key;
					}
					if (valueBytes > StorageChunkMaxBytes)
					{
						chunkedItems++;
						chunkValueEntries += Math.Max(1, (valueBytes + StorageChunkMaxBytes - 1) / StorageChunkMaxBytes);
					}
				}
			}
			int storedStringBytes = 0;
			if (stored != null)
			{
				foreach (KeyValuePair<string, string> item2 in stored)
				{
					storedStringBytes += GetUtf8ByteCount(item2.Key ?? "");
					storedStringBytes += GetUtf8ByteCount(item2.Value ?? "");
				}
			}
			Logger.Log("SaveSize", "save_size key=" + saveKey
				+ " kind=dictionary"
				+ " source=" + NormalizeDiagnosticToken(loggerTag)
				+ " rawEntries=" + rawEntries
				+ " storedEntries=" + storedEntries
				+ " rawKeyBytes=" + rawKeyBytes
				+ " rawValueBytes=" + rawValueBytes
				+ " chunkedItems=" + chunkedItems
				+ " chunkValueEntries=" + chunkValueEntries
				+ " maxValueBytes=" + maxValueBytes
				+ " maxValueKey=" + QuoteDiagnosticValue(maxValueKey, 96)
				+ " approxStoredStringBytes=" + storedStringBytes);
		}
		catch
		{
		}
	}

	private static string NormalizeDiagnosticToken(string value)
	{
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "unknown";
		}
		return text.Replace(" ", "_").Replace("\r", "_").Replace("\n", "_");
	}

	private static string QuoteDiagnosticValue(string value, int maxChars)
	{
		string text = (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ").Trim();
		if (maxChars > 0 && text.Length > maxChars)
		{
			text = text.Substring(0, maxChars).Trim();
		}
		return "\"" + text + "\"";
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
		if (!SafeSyncData(dataStore, key + StringChunkCountSuffix, ref data, loggerTag) || data <= 0 || data > MaxChunkCount)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < data; i++)
		{
			string text = "";
			if (!SafeSyncData(dataStore, key + StringChunkKeyPrefix + i, ref text, loggerTag))
			{
				return "";
			}
			stringBuilder.Append(text ?? "");
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
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		while (num4 < text.Length)
		{
			int utf8ScalarByteCount = GetUtf8ScalarByteCount(text, num4, out int charCount);
			if (num3 > 0 && num3 + utf8ScalarByteCount > num)
			{
				list.Add(text.Substring(num2, num4 - num2));
				num2 = num4;
				num3 = 0;
			}
			num3 += utf8ScalarByteCount;
			num4 += charCount;
		}
		if (num4 > num2)
		{
			list.Add(text.Substring(num2, num4 - num2));
		}
		return list;
	}

	private static int GetUtf8ScalarByteCount(string value, int index, out int charCount)
	{
		char c = value[index];
		if (char.IsHighSurrogate(c) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
		{
			charCount = 2;
			return 4;
		}
		charCount = 1;
		if (c <= '\u007f')
		{
			return 1;
		}
		return (c <= '\u07ff') ? 2 : 3;
	}

	private static bool TryRestoreChunkedDictionaryValue(Dictionary<string, string> stored, string key, out string value)
	{
		value = "";
		if (stored == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (!stored.TryGetValue(DictionaryChunkCountPrefix + key, out var value2) || !int.TryParse(value2, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0 || result > MaxChunkCount)
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
		return !string.IsNullOrEmpty(key) && key.StartsWith(DictionaryChunkCountPrefix, StringComparison.Ordinal);
	}

	private static bool IsDictionaryChunkValueKey(string key)
	{
		return !string.IsNullOrEmpty(key) && key.StartsWith(DictionaryChunkValuePrefix, StringComparison.Ordinal);
	}

	private static string BuildDictionaryChunkKey(string key, int index)
	{
		return DictionaryChunkValuePrefix + key + ":" + index.ToString(CultureInfo.InvariantCulture);
	}
}
