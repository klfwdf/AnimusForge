using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AnimusForge;

// Owns the bounded, save-compatible inbox. CampaignBehavior and UI remain adapters.
internal sealed class WorldEventInboxOwner
{
	private const int MaxRecords = 240;
	private readonly Dictionary<string, string> _records = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _eventIdByStableKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _unread = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private long _version;

	internal long Version => _version;
	internal int UnreadCount => _unread.Count;

	internal Dictionary<string, string> ExportRecords()
	{
		Trim();
		return new Dictionary<string, string>(_records, StringComparer.OrdinalIgnoreCase);
	}

	internal List<string> ExportUnread() => _unread.ToList();

	internal void Import(Dictionary<string, string> stored, List<string> unreadIds)
	{
		_records.Clear();
		_eventIdByStableKey.Clear();
		_unread.Clear();
		var unreadFromSave = new HashSet<string>(unreadIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		var candidates = new List<(AnimusForgeWorldEventInboxEntry Entry, bool Unread)>();
		foreach (KeyValuePair<string, string> item in stored ?? new Dictionary<string, string>())
		{
			AnimusForgeWorldEventInboxEntry entry = Deserialize(item.Value, item.Key);
			if (entry != null)
			{
				candidates.Add((entry, unreadFromSave.Contains(item.Key) || unreadFromSave.Contains(entry.EventId) || !entry.IsRead));
			}
		}
		foreach (var candidate in candidates.OrderByDescending(x => x.Entry.Day)
			.ThenByDescending(x => x.Entry.CreatedUtcTicks)
			.ThenBy(x => x.Entry.EventId, StringComparer.Ordinal))
		{
			string id = candidate.Entry.EventId;
			if (_eventIdByStableKey.TryGetValue(candidate.Entry.StableKey, out string retainedId)
				|| _records.ContainsKey(id))
			{
				if (candidate.Unread)
				{
					retainedId ??= id;
					if (_records.TryGetValue(retainedId, out string raw))
					{
						AnimusForgeWorldEventInboxEntry retained = Deserialize(raw);
						retained.IsRead = false;
						_records[retainedId] = JsonConvert.SerializeObject(retained);
						_unread.Add(retainedId);
					}
				}
				continue;
			}
			if (_records.Count >= MaxRecords) continue;
			candidate.Entry.IsRead = !candidate.Unread;
			_records.Add(id, JsonConvert.SerializeObject(candidate.Entry));
			_eventIdByStableKey.Add(candidate.Entry.StableKey, id);
			if (candidate.Unread) _unread.Add(id);
		}
		_version++;
	}

	internal void Upsert(AnimusForgeWorldEventInboxEntry entry, bool markUnread)
	{
		AnimusForgeWorldEventInboxEntry normalized = Normalize(entry, null, generateId: true);
		if (normalized == null) return;
		string id = normalized.EventId;
		if (_eventIdByStableKey.TryGetValue(normalized.StableKey, out string existingId))
		{
			id = existingId;
		}
		else if (_records.TryGetValue(id, out string previousRaw))
		{
			AnimusForgeWorldEventInboxEntry previous = Deserialize(previousRaw);
			if (previous != null) _eventIdByStableKey.Remove(previous.StableKey);
		}
		bool exists = _records.ContainsKey(id);
		bool unread = exists ? _unread.Contains(id) : markUnread;
		normalized.EventId = id;
		normalized.IsRead = !unread;
		_records[id] = JsonConvert.SerializeObject(normalized);
		_eventIdByStableKey[normalized.StableKey] = id;
		if (unread) _unread.Add(id);
		else _unread.Remove(id);
		Trim();
		// Existing policy publishers use the version change as a successful-upsert acknowledgement.
		_version++;
	}

	internal List<AnimusForgeWorldEventInboxEntry> Snapshot(int maxCount)
	{
		return _records.Values.Select(raw => Deserialize(raw)).Where(x => x != null)
			.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks)
			.Take(Math.Max(1, Math.Min(200, maxCount))).ToList();
	}

	internal bool MarkRead(string eventId)
	{
		string id = (eventId ?? "").Trim();
		if (!_records.TryGetValue(id, out string raw)) return false;
		AnimusForgeWorldEventInboxEntry entry = Deserialize(raw);
		if (entry == null) return false;
		if (entry.IsRead)
		{
			bool removed = _unread.Remove(id);
			if (removed) _version++;
			return removed;
		}
		entry.IsRead = true;
		_records[id] = JsonConvert.SerializeObject(entry);
		_unread.Remove(id);
		_version++;
		return true;
	}

	internal void MarkAllRead()
	{
		foreach (string id in _records.Keys.ToList())
		{
			AnimusForgeWorldEventInboxEntry entry = Deserialize(_records[id]);
			if (entry == null) continue;
			entry.IsRead = true;
			_records[id] = JsonConvert.SerializeObject(entry);
		}
		_unread.Clear();
		_version++;
	}

	private void Trim()
	{
		foreach (AnimusForgeWorldEventInboxEntry extra in _records.Values.Select(raw => Deserialize(raw)).Where(x => x != null)
			.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).Skip(MaxRecords).ToList())
		{
			_records.Remove(extra.EventId);
			_unread.Remove(extra.EventId);
			if (_eventIdByStableKey.TryGetValue(extra.StableKey, out string indexedId)
				&& string.Equals(indexedId, extra.EventId, StringComparison.OrdinalIgnoreCase))
			{
				_eventIdByStableKey.Remove(extra.StableKey);
			}
		}
	}

	private static AnimusForgeWorldEventInboxEntry Deserialize(string raw, string fallbackId = null)
	{
		try { return Normalize(JsonConvert.DeserializeObject<AnimusForgeWorldEventInboxEntry>(raw ?? ""), fallbackId, generateId: false); }
		catch { return null; }
	}

	private static AnimusForgeWorldEventInboxEntry Normalize(AnimusForgeWorldEventInboxEntry entry, string fallbackId, bool generateId)
	{
		if (entry == null) return null;
		entry.EventId = First(entry.EventId, entry.StableKey, fallbackId);
		if (string.IsNullOrWhiteSpace(entry.EventId))
		{
			if (!generateId) return null;
			entry.EventId = Guid.NewGuid().ToString("N");
		}
		entry.EventKind = First(entry.EventKind, "world_event");
		entry.KindLabel = First(entry.KindLabel, "世界事件");
		entry.Title = First(entry.Title, "AnimusForge 事件");
		entry.Summary = First(entry.Summary, entry.DetailText);
		entry.DetailText = First(entry.DetailText, entry.Summary);
		entry.BodySectionTitleText = First(entry.BodySectionTitleText, "事件详情");
		entry.Day = Math.Max(0, entry.Day);
		entry.CreatedUtcTicks = entry.CreatedUtcTicks > 0 ? entry.CreatedUtcTicks : DateTime.UtcNow.Ticks;
		entry.StableKey = First(entry.StableKey, entry.EventId);
		return entry;
	}

	private static string First(params string[] values) => (values ?? Array.Empty<string>()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
}
