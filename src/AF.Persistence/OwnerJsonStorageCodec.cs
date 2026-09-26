using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Shared codec for the "owner id → JSON list" storage pattern used by MyBehavior's SyncData
/// blocks. It does not own save keys, chunking (CampaignSaveChunkHelper) or the persisted
/// element types; it only removes the repeated per-owner serialize/deserialize loops and gives
/// one place for the failure policy (a failing owner is skipped and reported, never aborts the
/// whole save/load).
/// </summary>
internal static class OwnerJsonStorageCodec
{
	/// <summary>
	/// Serialize each owner list into <paramref name="storage"/> (cleared first). <paramref name="transform"/> reproduces the
	/// legacy per-site pre-save sanitize; when it returns an empty list the owner is skipped. Returns owner count written.
	/// </summary>
	internal static int Serialize<T>(Dictionary<string, List<T>> source, Dictionary<string, string> storage, bool skipWhitespaceKeys, bool skipEmptyLists, Func<List<T>, List<T>> transform, Action<string, Exception> onError)
	{
		storage.Clear();
		int written = 0;
		if (source == null)
		{
			return 0;
		}
		foreach (KeyValuePair<string, List<T>> item in source)
		{
			bool blankKey = skipWhitespaceKeys ? string.IsNullOrWhiteSpace(item.Key) : string.IsNullOrEmpty(item.Key);
			if (blankKey || item.Value == null || (skipEmptyLists && item.Value.Count == 0))
			{
				continue;
			}
			try
			{
				List<T> list = transform != null ? transform(item.Value) : item.Value;
				if (transform != null && (list == null || list.Count == 0))
				{
					continue;
				}
				storage[item.Key] = JsonConvert.SerializeObject(list);
				written++;
			}
			catch (Exception ex)
			{
				onError?.Invoke(item.Key, ex);
			}
		}
		return written;
	}

	/// <summary>
	/// Deserialize each owner entry into <paramref name="target"/> (cleared first). <paramref name="normalizeKey"/> and
	/// <paramref name="sanitize"/> reproduce the legacy per-site behavior; a sanitized empty list is not stored.
	/// </summary>
	internal static int Deserialize<T>(Dictionary<string, string> storage, Dictionary<string, List<T>> target, Func<string, string> normalizeKey, Func<List<T>, List<T>> sanitize, bool skipWhitespaceKeys, Action<string, Exception> onError)
	{
		target.Clear();
		int restored = 0;
		if (storage == null)
		{
			return 0;
		}
		foreach (KeyValuePair<string, string> entry in storage)
		{
			bool blankKey = skipWhitespaceKeys ? string.IsNullOrWhiteSpace(entry.Key) : string.IsNullOrEmpty(entry.Key);
			bool blankValue = skipWhitespaceKeys ? string.IsNullOrWhiteSpace(entry.Value) : string.IsNullOrEmpty(entry.Value);
			if (blankKey || blankValue)
			{
				continue;
			}
			try
			{
				List<T> list = JsonConvert.DeserializeObject<List<T>>(entry.Value);
				if (sanitize != null)
				{
					list = sanitize(list ?? new List<T>());
					if (list == null || list.Count == 0)
					{
						continue;
					}
				}
				else if (list == null)
				{
					continue;
				}
				target[normalizeKey != null ? normalizeKey(entry.Key) : entry.Key] = list;
				restored++;
			}
			catch (Exception ex)
			{
				onError?.Invoke(entry.Key, ex);
			}
		}
		return restored;
	}
}
