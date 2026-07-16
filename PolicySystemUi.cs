using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeWorldEventInboxEntry
{
	public int Version { get; set; } = 1;
	public string EventId { get; set; }
	public string EventKind { get; set; }
	public string EventType { get; set; }
	public string KindLabel { get; set; }
	public string HeaderRightText { get; set; }
	public string BodySectionTitleText { get; set; }
	public string ImpactSectionTitleText { get; set; }
	public string ImpactText { get; set; }
	public string Title { get; set; }
	public string Summary { get; set; }
	public string DetailText { get; set; }
	public string KingdomId { get; set; }
	public string KingdomName { get; set; }
	public string ActorHeroId { get; set; }
	public string ActorHeroName { get; set; }
	public int Day { get; set; }
	public string GameDate { get; set; }
	public long CreatedUtcTicks { get; set; }
	public string StableKey { get; set; }
	public bool IsRead { get; set; }
}

public sealed class AnimusForgeWorldEventBehavior : CampaignBehaviorBase
{
	private const string SaveKeyRecords = "_afWorldEventInboxRecords_v1";
	private const string SaveKeyUnread = "_afWorldEventInboxUnread_v1";
	private const int MaxRecords = 240;
	private readonly Dictionary<string, string> _records = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _unread = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private long _version;

	public static AnimusForgeWorldEventBehavior Instance { get; private set; }

	public AnimusForgeWorldEventBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null) return;
		if (dataStore.IsSaving)
		{
			Trim();
			Dictionary<string, string> records = CampaignSaveChunkHelper.FlattenStringDictionary(_records, SaveKeyRecords, "WorldEventInbox");
			dataStore.SyncData(SaveKeyRecords, ref records);
			List<string> unread = _unread.ToList();
			dataStore.SyncData(SaveKeyUnread, ref unread);
			return;
		}
		_records.Clear();
		_unread.Clear();
		Dictionary<string, string> stored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyRecords, ref stored);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(stored, "WorldEventInbox"))
		{
			AnimusForgeWorldEventInboxEntry entry = Deserialize(item.Value);
			if (entry != null) _records[entry.EventId] = JsonConvert.SerializeObject(entry);
		}
		List<string> unreadIds = new List<string>();
		dataStore.SyncData(SaveKeyUnread, ref unreadIds);
		foreach (string id in unreadIds ?? new List<string>()) if (_records.ContainsKey(id ?? "")) _unread.Add(id);
		Trim();
		_version++;
	}

	public static void UpsertWorldEventForExternal(AnimusForgeWorldEventInboxEntry entry, bool markUnread = true) => Instance?.Upsert(entry, markUnread);
	public static long GetInboxVersionForExternal() => Instance?._version ?? 0L;
	public static int GetUnreadCountForExternal() => Instance?._unread.Count ?? 0;
	public static List<AnimusForgeWorldEventInboxEntry> GetInboxSnapshotForExternal(int maxCount = 80) => Instance?.Snapshot(maxCount) ?? new List<AnimusForgeWorldEventInboxEntry>();
	public static bool MarkEventReadForExternal(string eventId) => Instance?.MarkRead(eventId) == true;
	public static void MarkAllReadForExternal() => Instance?.MarkAllRead();

	private void Upsert(AnimusForgeWorldEventInboxEntry entry, bool markUnread)
	{
		AnimusForgeWorldEventInboxEntry normalized = Normalize(entry);
		if (normalized == null) return;
		if (markUnread)
		{
			normalized.IsRead = false;
			_unread.Add(normalized.EventId);
		}
		_records[normalized.EventId] = JsonConvert.SerializeObject(normalized);
		Trim();
		_version++;
	}

	private List<AnimusForgeWorldEventInboxEntry> Snapshot(int maxCount)
	{
		return _records.Values.Select(Deserialize).Where(x => x != null).OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).Take(Math.Max(1, Math.Min(200, maxCount))).ToList();
	}

	private bool MarkRead(string eventId)
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

	private void MarkAllRead()
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
		foreach (AnimusForgeWorldEventInboxEntry extra in _records.Values.Select(Deserialize).Where(x => x != null).OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).Skip(MaxRecords).ToList())
		{
			_records.Remove(extra.EventId);
			_unread.Remove(extra.EventId);
		}
	}

	private static AnimusForgeWorldEventInboxEntry Deserialize(string raw)
	{
		try { return Normalize(JsonConvert.DeserializeObject<AnimusForgeWorldEventInboxEntry>(raw ?? "")); } catch { return null; }
	}

	private static AnimusForgeWorldEventInboxEntry Normalize(AnimusForgeWorldEventInboxEntry entry)
	{
		if (entry == null) return null;
		entry.EventId = First(entry.EventId, entry.StableKey, Guid.NewGuid().ToString("N"));
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

public sealed class AnimusForgeTopDownListPanel : ListPanel
{
	public AnimusForgeTopDownListPanel(UIContext context)
		: base(context)
	{
#if BANNERLORD_1_4_OR_GREATER
		StackLayout.LayoutMethod = LayoutMethod.VerticalTopToBottom;
#else
		StackLayout.LayoutMethod = LayoutMethod.VerticalBottomToTop;
#endif
	}
}

public sealed class AnimusForgeVersionedScrollableListPanel : ListPanel
{
#if !BANNERLORD_1_4_OR_GREATER
	private bool _childOrderNormalized;
#endif

	public AnimusForgeVersionedScrollableListPanel(UIContext context)
		: base(context)
	{
		StackLayout.LayoutMethod = LayoutMethod.VerticalTopToBottom;
	}

	protected override void OnLateUpdate(float dt)
	{
#if !BANNERLORD_1_4_OR_GREATER
		if (!_childOrderNormalized && ChildCount > 0)
		{
			List<Widget> originalOrder = new List<Widget>();
			for (int i = 0; i < ChildCount; i++)
			{
				originalOrder.Add(GetChild(i));
			}
			foreach (Widget child in originalOrder)
			{
				child.SetSiblingIndex(0);
			}
			SetMeasureAndLayoutDirty();
			_childOrderNormalized = true;
		}
#endif
		base.OnLateUpdate(dt);
	}
}

public static class PolicySystemUi
{
	private const int EventInboxDisplayLimit = 160;
	private const int DetailBodyCharacterLimit = 1200;
	private const int DetailBodyLineLimit = 40;

	public static void OnApplicationTick()
	{
		try
		{
			AnimusForgeWorldEventInboxPopup.OnApplicationTick();
			ScreenBase top = ScreenManager.TopScreen;
			if (Campaign.Current == null || !(top is MapScreen))
			{
				AnimusForgeWorldEventInboxPopup.CloseActive(silent: true);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "world-policy-tick-failed", ex.Message, ex.ToString());
		}
	}

	public static bool ShowWorldPolicies(Action onClose = null)
	{
		try
		{
			if (Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
			{
				return false;
			}
			return AnimusForgeWorldEventInboxPopup.Show(BuildInboxPopupData(), onClose);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "world-policy-open-failed", ex.Message, ex.ToString());
			return false;
		}
	}

	public static void CloseWorldPolicies()
	{
		AnimusForgeWorldEventInboxPopup.CloseActive(silent: true);
	}

	private static WorldEventInboxPopupData BuildInboxPopupData()
	{
		List<AnimusForgeWorldEventInboxEntry> events = AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(EventInboxDisplayLimit);
		List<WorldEventCountryGroup> countries = BuildCountryGroups(events);
		WorldEventInboxPopupData data = new WorldEventInboxPopupData
		{
			TitleText = "世界政策",
			SubtitleText = "只读查看各国已经发布的玩家与 NPC 统治者政策及政策衍生事件。",
			EmptyStateText = "暂无世界事件。统治者政策与政策衍生事件会出现在这里。",
			CloseText = "关闭"
		};

		foreach (WorldEventCountryGroup country in countries)
		{
			WorldEventCountryData countryData = new WorldEventCountryData
			{
				KingdomId = country.KingdomId ?? "",
				KingdomName = FirstNonEmpty(country.KingdomName, country.KingdomId, "未知国家"),
				UnreadCount = country.Events.Count(e => e != null && !e.IsRead)
			};
			foreach (AnimusForgeWorldEventInboxEntry entry in country.Events)
			{
				WorldEventRecordData record = BuildRecordData(entry);
				if (record != null)
				{
					countryData.Records.Add(record);
				}
			}
			data.Countries.Add(countryData);
		}

		int selected = data.Countries.FindIndex(x => x != null && x.Records.Count > 0);
		data.SelectedCountryIndex = selected >= 0 ? selected : 0;
		return data;
	}

	private static WorldEventRecordData BuildRecordData(AnimusForgeWorldEventInboxEntry entry)
	{
		if (entry == null)
		{
			return null;
		}
		string kind = FirstNonEmpty(entry.KindLabel, "世界事件");
		string date = FirstNonEmpty(entry.GameDate, entry.Day > 0 ? ("第" + entry.Day.ToString(CultureInfo.InvariantCulture) + "天") : "未知日期");
		string title = FirstNonEmpty(entry.Title, kind);
		string body = FirstNonEmpty(entry.DetailText, entry.Summary);
		body = (body ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
		string meta = BuildRecordMetaText(entry, kind, date);
		string impact = entry.ImpactText ?? "";
		return new WorldEventRecordData
		{
			EventId = entry.EventId ?? "",
			KindLabel = kind,
			HeaderRightText = entry.HeaderRightText ?? "",
			DateText = date,
			TitleText = title,
			MetaText = meta,
			PolicyNameText = "",
			BodyText = string.IsNullOrWhiteSpace(body) ? "（无详情）" : body,
			BodySectionTitleText = FirstNonEmpty(entry.BodySectionTitleText, "事件详情"),
			ImpactSectionTitleText = entry.ImpactSectionTitleText ?? "",
			ImpactText = impact,
			IndexMetaText = date + "  ·  " + kind,
			UnreadMarkerText = entry.IsRead ? "" : "新",
			IsUnread = !entry.IsRead,
			HasPolicyName = false,
			HasImpact = !string.IsNullOrWhiteSpace(impact)
		};
	}

	private static string ExtractDetailLineValue(string detail, string prefix)
	{
		foreach (string line in (detail ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
		{
			string clean = (line ?? "").Trim();
			if (clean.StartsWith(prefix ?? "", StringComparison.Ordinal))
			{
				return clean.Substring((prefix ?? "").Length).Trim();
			}
		}
		return "";
	}

	private static string BuildRecordMetaText(AnimusForgeWorldEventInboxEntry entry, string kind, string date)
	{
		List<string> parts = new List<string>();
		parts.Add(date);
		parts.Add(kind);
		string kingdom = FirstNonEmpty(entry?.KingdomName, entry?.KingdomId);
		if (!string.IsNullOrWhiteSpace(kingdom))
		{
			parts.Add(kingdom);
		}
		string actor = FirstNonEmpty(entry?.ActorHeroName, entry?.ActorHeroId);
		if (!string.IsNullOrWhiteSpace(actor))
		{
			parts.Add("相关人物：" + actor);
		}
		return string.Join("  ·  ", parts);
	}

	private static List<WorldEventCountryGroup> BuildCountryGroups(List<AnimusForgeWorldEventInboxEntry> events)
	{
		Dictionary<string, WorldEventCountryGroup> byId = new Dictionary<string, WorldEventCountryGroup>(StringComparer.OrdinalIgnoreCase);
		List<WorldEventCountryGroup> groups = new List<WorldEventCountryGroup>();
		try
		{
			IEnumerable<Kingdom> kingdoms = Kingdom.All ?? Enumerable.Empty<Kingdom>();
			foreach (Kingdom kingdom in kingdoms.Where(k => k != null && !k.IsEliminated).OrderBy(GetKingdomNameSafe, StringComparer.OrdinalIgnoreCase))
			{
				string id = (kingdom.StringId ?? "").Trim();
				string name = GetKingdomNameSafe(kingdom);
				if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
				{
					continue;
				}
				AddOrGetCountryGroup(byId, groups, id, name);
			}
		}
		catch
		{
		}

		foreach (AnimusForgeWorldEventInboxEntry entry in events ?? new List<AnimusForgeWorldEventInboxEntry>())
		{
			if (entry == null)
			{
				continue;
			}
			string id = (entry.KingdomId ?? "").Trim();
			string name = FirstNonEmpty(entry.KingdomName, id, "未知国家");
			WorldEventCountryGroup group = AddOrGetCountryGroup(byId, groups, id, name);
			group.Events.Add(entry);
		}

		foreach (WorldEventCountryGroup group in groups)
		{
			group.Events = group.Events
				.Where(e => e != null)
				.OrderByDescending(e => e.Day)
				.ThenByDescending(e => e.CreatedUtcTicks)
				.ThenBy(e => e.EventKind ?? "", StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		return groups
			.Where(g => g.Events.Count > 0)
			.OrderBy(g => g.KingdomName ?? g.KingdomId ?? "", StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static WorldEventCountryGroup AddOrGetCountryGroup(Dictionary<string, WorldEventCountryGroup> byId, List<WorldEventCountryGroup> groups, string kingdomId, string kingdomName)
	{
		string id = (kingdomId ?? "").Trim();
		string name = FirstNonEmpty(kingdomName, id, "未知国家");
		string key = string.IsNullOrWhiteSpace(id) ? ("name:" + name.Trim()) : id;
		if (!byId.TryGetValue(key, out WorldEventCountryGroup group))
		{
			group = new WorldEventCountryGroup
			{
				KingdomId = id,
				KingdomName = name
			};
			byId[key] = group;
			groups.Add(group);
		}
		else if (string.IsNullOrWhiteSpace(group.KingdomName) || string.Equals(group.KingdomName, group.KingdomId, StringComparison.OrdinalIgnoreCase))
		{
			group.KingdomName = name;
		}
		return group;
	}

	private static string GetKingdomNameSafe(Kingdom kingdom)
	{
		try
		{
			return (kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "").Trim();
		}
		catch
		{
			return kingdom?.StringId ?? "";
		}
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}

	private static string Limit(string text, int max)
	{
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return text.Length <= max ? text : text.Substring(0, Math.Max(1, max - 1)).TrimEnd() + "…";
	}

	private static string LimitMultiline(string text, int maxCharacters, int maxLines)
	{
		text = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		bool truncated = false;
		string[] lines = text.Split('\n');
		if (maxLines > 0 && lines.Length > maxLines)
		{
			text = string.Join("\n", lines.Take(maxLines));
			truncated = true;
		}
		if (maxCharacters > 0 && text.Length > maxCharacters)
		{
			text = text.Substring(0, Math.Max(1, maxCharacters - 1)).TrimEnd();
			truncated = true;
		}
		return truncated ? text.TrimEnd('…') + "…" : text;
	}

	private sealed class WorldEventCountryGroup
	{
		public string KingdomId = "";
		public string KingdomName = "";
		public List<AnimusForgeWorldEventInboxEntry> Events = new List<AnimusForgeWorldEventInboxEntry>();
	}
}

public sealed class AnimusForgeWorldEventInboxPopup
{
	private static AnimusForgeWorldEventInboxPopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly AnimusForgeWorldEventInboxPopupVM _dataSource;
	private readonly Action _onClose;
	private bool _isClosed;

	private AnimusForgeWorldEventInboxPopup(ScreenBase screen, WorldEventInboxPopupData data, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new AnimusForgeWorldEventInboxPopupVM(data, HandleCloseRequested);
		_layer = new GauntletLayer("AnimusForgeWorldEventInboxPopup", 4100, false);
	}

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	public static bool Show(WorldEventInboxPopupData data, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			AnimusForgeWorldEventInboxPopup popup = new AnimusForgeWorldEventInboxPopup(topScreen, data ?? new WorldEventInboxPopupData(), onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "world-policy-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void OnApplicationTick()
	{
		AnimusForgeWorldEventInboxPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCloseForEscapeKey())
		{
			popup.HandleCloseRequested();
		}
	}

	public static void CloseActive(bool silent)
	{
		_activePopup?.Close(silent);
	}

	private void Open()
	{
		_layer.LoadMovie("AnimusForgeWorldEventInboxPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private bool ShouldCloseForEscapeKey()
	{
		try
		{
			return _layer?.Input != null && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape));
		}
		catch
		{
		}
		try
		{
			return Input.IsKeyReleased(InputKey.Escape);
		}
		catch
		{
			return false;
		}
	}

	private void HandleCloseRequested()
	{
		Close(silent: true);
		_onClose?.Invoke();
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				PolicySystemLog.Failure("UI", "world-policy-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class AnimusForgeWorldEventInboxPopupVM : ViewModel
{
	private readonly Action _onClose;
	private string _titleText;
	private string _subtitleText;
	private string _emptyStateText;
	private string _closeText;
	private string _selectedCountryTitleText;
	private string _selectedRecordTitleText;
	private string _selectedRecordKindLabel;
	private string _selectedRecordMetaText;
	private string _selectedRecordPolicyNameText;
	private string _selectedRecordBodySectionTitleText;
	private string _selectedRecordBodyText;
	private string _selectedRecordImpactSectionTitleText;
	private string _selectedRecordImpactText;
	private string _selectedRecordUnreadMarkerText;
	private float _selectedRecordTitleHeight;
	private float _selectedRecordMetaTop;
	private float _selectedRecordDividerTop;
	private float _selectedRecordDetailTop;
	private float _selectedRecordScrollbarTop;
	private bool _hasEvents;
	private bool _showEmptyState;
	private bool _selectedCountryHasRecords;
	private bool _showSelectedCountryEmptyState;
	private bool _hasSelectedRecordPolicyName;
	private bool _hasSelectedRecordImpact;
	private MBBindingList<WorldEventCountryItemVM> _countryItems;
	private MBBindingList<WorldEventRecordItemVM> _recordItems;

	public AnimusForgeWorldEventInboxPopupVM(WorldEventInboxPopupData data, Action onClose)
	{
		_onClose = onClose;
		WorldEventInboxPopupData source = data ?? new WorldEventInboxPopupData();
		TitleText = string.IsNullOrWhiteSpace(source.TitleText) ? "世界事件" : source.TitleText.Trim();
		SubtitleText = string.IsNullOrWhiteSpace(source.SubtitleText) ? "只读查看 NPC 统治者政策、民众反馈和世界事件。" : source.SubtitleText.Trim();
		EmptyStateText = string.IsNullOrWhiteSpace(source.EmptyStateText) ? "暂无世界事件。" : source.EmptyStateText.Trim();
		CloseText = string.IsNullOrWhiteSpace(source.CloseText) ? "关闭" : source.CloseText.Trim();
		CountryItems = new MBBindingList<WorldEventCountryItemVM>();
		RecordItems = new MBBindingList<WorldEventRecordItemVM>();
		List<WorldEventCountryData> countries = source.Countries ?? new List<WorldEventCountryData>();
		for (int i = 0; i < countries.Count; i++)
		{
			WorldEventCountryData country = countries[i];
			if (country != null)
			{
				CountryItems.Add(new WorldEventCountryItemVM(country, i, SelectCountry));
			}
		}
		HasEvents = CountryItems.Any(x => x != null && x.TotalCount > 0);
		ShowEmptyState = !HasEvents;
		int selectedIndex = Math.Max(0, Math.Min(Math.Max(0, CountryItems.Count - 1), source.SelectedCountryIndex));
		if (CountryItems.Count > 0)
		{
			SelectCountry(selectedIndex);
		}
		else
		{
			SelectedCountryTitleText = "世界事件";
			SelectedCountryHasRecords = false;
			ShowSelectedCountryEmptyState = false;
			ClearSelectedRecord();
		}
	}

	[DataSourceProperty]
	public string TitleText { get => _titleText; set { if (value != _titleText) { _titleText = value; OnPropertyChangedWithValue(value, nameof(TitleText)); } } }
	[DataSourceProperty]
	public string SubtitleText { get => _subtitleText; set { if (value != _subtitleText) { _subtitleText = value; OnPropertyChangedWithValue(value, nameof(SubtitleText)); } } }
	[DataSourceProperty]
	public string EmptyStateText { get => _emptyStateText; set { if (value != _emptyStateText) { _emptyStateText = value; OnPropertyChangedWithValue(value, nameof(EmptyStateText)); } } }
	[DataSourceProperty]
	public string CloseText { get => _closeText; set { if (value != _closeText) { _closeText = value; OnPropertyChangedWithValue(value, nameof(CloseText)); } } }
	[DataSourceProperty]
	public string SelectedCountryTitleText { get => _selectedCountryTitleText; set { if (value != _selectedCountryTitleText) { _selectedCountryTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedCountryTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordTitleText { get => _selectedRecordTitleText; set { if (value != _selectedRecordTitleText) { _selectedRecordTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordKindLabel { get => _selectedRecordKindLabel; set { if (value != _selectedRecordKindLabel) { _selectedRecordKindLabel = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordKindLabel)); } } }
	[DataSourceProperty]
	public string SelectedRecordMetaText { get => _selectedRecordMetaText; set { if (value != _selectedRecordMetaText) { _selectedRecordMetaText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordMetaText)); } } }
	[DataSourceProperty]
	public string SelectedRecordPolicyNameText { get => _selectedRecordPolicyNameText; set { if (value != _selectedRecordPolicyNameText) { _selectedRecordPolicyNameText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordPolicyNameText)); } } }
	[DataSourceProperty]
	public string SelectedRecordBodySectionTitleText { get => _selectedRecordBodySectionTitleText; set { if (value != _selectedRecordBodySectionTitleText) { _selectedRecordBodySectionTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordBodySectionTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordBodyText { get => _selectedRecordBodyText; set { if (value != _selectedRecordBodyText) { _selectedRecordBodyText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordBodyText)); } } }
	[DataSourceProperty]
	public string SelectedRecordImpactSectionTitleText { get => _selectedRecordImpactSectionTitleText; set { if (value != _selectedRecordImpactSectionTitleText) { _selectedRecordImpactSectionTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordImpactSectionTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordImpactText { get => _selectedRecordImpactText; set { if (value != _selectedRecordImpactText) { _selectedRecordImpactText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordImpactText)); } } }
	[DataSourceProperty]
	public string SelectedRecordUnreadMarkerText { get => _selectedRecordUnreadMarkerText; set { if (value != _selectedRecordUnreadMarkerText) { _selectedRecordUnreadMarkerText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordUnreadMarkerText)); } } }
	[DataSourceProperty]
	public float SelectedRecordTitleHeight { get => _selectedRecordTitleHeight; set { if (Math.Abs(value - _selectedRecordTitleHeight) > 0.01f) { _selectedRecordTitleHeight = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordTitleHeight)); } } }
	[DataSourceProperty]
	public float SelectedRecordMetaTop { get => _selectedRecordMetaTop; set { if (Math.Abs(value - _selectedRecordMetaTop) > 0.01f) { _selectedRecordMetaTop = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordMetaTop)); } } }
	[DataSourceProperty]
	public float SelectedRecordDividerTop { get => _selectedRecordDividerTop; set { if (Math.Abs(value - _selectedRecordDividerTop) > 0.01f) { _selectedRecordDividerTop = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordDividerTop)); } } }
	[DataSourceProperty]
	public float SelectedRecordDetailTop { get => _selectedRecordDetailTop; set { if (Math.Abs(value - _selectedRecordDetailTop) > 0.01f) { _selectedRecordDetailTop = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordDetailTop)); } } }
	[DataSourceProperty]
	public float SelectedRecordScrollbarTop { get => _selectedRecordScrollbarTop; set { if (Math.Abs(value - _selectedRecordScrollbarTop) > 0.01f) { _selectedRecordScrollbarTop = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordScrollbarTop)); } } }
	[DataSourceProperty]
	public bool HasEvents { get => _hasEvents; set { if (value != _hasEvents) { _hasEvents = value; OnPropertyChangedWithValue(value, nameof(HasEvents)); } } }
	[DataSourceProperty]
	public bool ShowEmptyState { get => _showEmptyState; set { if (value != _showEmptyState) { _showEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowEmptyState)); } } }
	[DataSourceProperty]
	public bool SelectedCountryHasRecords { get => _selectedCountryHasRecords; set { if (value != _selectedCountryHasRecords) { _selectedCountryHasRecords = value; OnPropertyChangedWithValue(value, nameof(SelectedCountryHasRecords)); } } }
	[DataSourceProperty]
	public bool ShowSelectedCountryEmptyState { get => _showSelectedCountryEmptyState; set { if (value != _showSelectedCountryEmptyState) { _showSelectedCountryEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowSelectedCountryEmptyState)); } } }
	[DataSourceProperty]
	public bool HasSelectedRecordPolicyName { get => _hasSelectedRecordPolicyName; set { if (value != _hasSelectedRecordPolicyName) { _hasSelectedRecordPolicyName = value; OnPropertyChangedWithValue(value, nameof(HasSelectedRecordPolicyName)); } } }
	[DataSourceProperty]
	public bool HasSelectedRecordImpact { get => _hasSelectedRecordImpact; set { if (value != _hasSelectedRecordImpact) { _hasSelectedRecordImpact = value; OnPropertyChangedWithValue(value, nameof(HasSelectedRecordImpact)); } } }
	[DataSourceProperty]
	public MBBindingList<WorldEventCountryItemVM> CountryItems { get => _countryItems; set { if (value != _countryItems) { _countryItems = value; OnPropertyChangedWithValue(value, nameof(CountryItems)); } } }
	[DataSourceProperty]
	public MBBindingList<WorldEventRecordItemVM> RecordItems { get => _recordItems; set { if (value != _recordItems) { _recordItems = value; OnPropertyChangedWithValue(value, nameof(RecordItems)); } } }

	private void SelectCountry(int index)
	{
		if (CountryItems == null || CountryItems.Count == 0)
		{
			return;
		}
		index = Math.Max(0, Math.Min(CountryItems.Count - 1, index));
		for (int i = 0; i < CountryItems.Count; i++)
		{
			CountryItems[i].IsSelected = i == index;
		}
		WorldEventCountryItemVM selected = CountryItems[index];
		RecordItems.Clear();
		int recordIndex = 0;
		foreach (WorldEventRecordData record in selected.Source.Records ?? new List<WorldEventRecordData>())
		{
			if (record != null)
			{
				RecordItems.Add(new WorldEventRecordItemVM(record, recordIndex, SelectRecord));
				recordIndex++;
			}
		}
		SelectedCountryTitleText = BuildSelectedCountryTitle(selected);
		SelectedCountryHasRecords = RecordItems.Count > 0;
		ShowSelectedCountryEmptyState = HasEvents && !SelectedCountryHasRecords;
		if (RecordItems.Count > 0)
		{
			SelectRecord(0);
		}
		else
		{
			ClearSelectedRecord();
		}
	}

	private static string BuildSelectedCountryTitle(WorldEventCountryItemVM country)
	{
		if (country == null)
		{
			return "世界事件";
		}
		return country.KingdomName;
	}

	private void SelectRecord(int index)
	{
		if (RecordItems == null || RecordItems.Count == 0)
		{
			ClearSelectedRecord();
			return;
		}
		index = Math.Max(0, Math.Min(RecordItems.Count - 1, index));
		for (int i = 0; i < RecordItems.Count; i++)
		{
			RecordItems[i].IsSelected = i == index;
		}
		WorldEventRecordItemVM selected = RecordItems[index];
		if (selected.IsUnread)
		{
			AnimusForgeWorldEventBehavior.MarkEventReadForExternal(selected.EventId);
			selected.MarkRead();
			CountryItems?.FirstOrDefault(x => x != null && x.IsSelected)?.RefreshUnreadCountFromRecords();
		}
		SelectedRecordTitleText = selected.TitleText;
		UpdateSelectedRecordHeaderLayout(selected.TitleText);
		SelectedRecordKindLabel = selected.HeaderRightText;
		SelectedRecordMetaText = selected.MetaText;
		SelectedRecordPolicyNameText = selected.PolicyNameText;
		SelectedRecordBodySectionTitleText = selected.BodySectionTitleText;
		SelectedRecordBodyText = selected.BodyText;
		SelectedRecordImpactSectionTitleText = selected.ImpactSectionTitleText;
		SelectedRecordImpactText = selected.ImpactText;
		SelectedRecordUnreadMarkerText = selected.UnreadMarkerText;
		HasSelectedRecordPolicyName = selected.HasPolicyName;
		HasSelectedRecordImpact = selected.HasImpact;
	}

	private void ClearSelectedRecord()
	{
		SelectedRecordTitleText = "";
		SelectedRecordKindLabel = "";
		SelectedRecordMetaText = "";
		SelectedRecordPolicyNameText = "";
		SelectedRecordBodySectionTitleText = "";
		SelectedRecordBodyText = "";
		SelectedRecordImpactSectionTitleText = "";
		SelectedRecordImpactText = "";
		SelectedRecordUnreadMarkerText = "";
		UpdateSelectedRecordHeaderLayout("");
		HasSelectedRecordPolicyName = false;
		HasSelectedRecordImpact = false;
	}

	private void UpdateSelectedRecordHeaderLayout(string title)
	{
		bool usesTwoLines = !string.IsNullOrEmpty(title) &&
			(title.IndexOf('\n') >= 0 || title.IndexOf('\r') >= 0 || title.Length > 22);
		SelectedRecordTitleHeight = usesTwoLines ? 68f : 38f;
		SelectedRecordMetaTop = usesTwoLines ? 102f : 72f;
		SelectedRecordDividerTop = usesTwoLines ? 136f : 106f;
		SelectedRecordDetailTop = usesTwoLines ? 154f : 124f;
		SelectedRecordScrollbarTop = usesTwoLines ? 156f : 126f;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}

public sealed class WorldEventCountryItemVM : ViewModel
{
	private readonly Action<int> _select;
	private bool _isSelected;
	private string _selectionText;

	public WorldEventCountryItemVM(WorldEventCountryData source, int index, Action<int> select)
	{
		Source = source ?? new WorldEventCountryData();
		Index = index;
		_select = select;
		UpdateSelectionText();
	}

	public WorldEventCountryData Source { get; }
	public int Index { get; }
	[DataSourceProperty]
	public string KingdomName => string.IsNullOrWhiteSpace(Source.KingdomName) ? "未知国家" : Source.KingdomName.Trim();
	public int UnreadCount => Math.Max(0, Source.UnreadCount);
	public int TotalCount => Math.Max(0, Source.Records?.Count ?? 0);
	public bool HasUnread => UnreadCount > 0;

	[DataSourceProperty]
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, nameof(IsSelected));
				UpdateSelectionText();
			}
		}
	}

	[DataSourceProperty]
	public string SelectionText
	{
		get => _selectionText;
		set
		{
			if (value != _selectionText)
			{
				_selectionText = value;
				OnPropertyChangedWithValue(value, nameof(SelectionText));
			}
		}
	}

	[DataSourceProperty]
	public string UnreadText => HasUnread ? ("新 " + UnreadCount.ToString(CultureInfo.InvariantCulture)) : "";

	public void ExecuteSelect()
	{
		_select?.Invoke(Index);
	}

	public void RefreshUnreadCountFromRecords()
	{
		Source.UnreadCount = Source.Records?.Count(x => x != null && x.IsUnread) ?? 0;
		OnPropertyChangedWithValue(UnreadText, nameof(UnreadText));
	}

	private void UpdateSelectionText()
	{
		SelectionText = "";
	}
}

public sealed class WorldEventRecordItemVM : ViewModel
{
	private readonly Action<int> _select;
	private readonly WorldEventRecordData _source;
	private bool _isSelected;
	private string _unreadMarkerText;
	private bool _isUnread;

	public WorldEventRecordItemVM(WorldEventRecordData source, int index, Action<int> select)
	{
		WorldEventRecordData data = source ?? new WorldEventRecordData();
		_source = data;
		EventId = data.EventId ?? "";
		Index = index;
		_select = select;
		KindLabel = data.KindLabel ?? "世界事件";
		HeaderRightText = data.HeaderRightText ?? "";
		DateText = data.DateText ?? "";
		TitleText = data.TitleText ?? "世界事件";
		MetaText = data.MetaText ?? "";
		IndexMetaText = data.IndexMetaText ?? "";
		PolicyNameText = data.PolicyNameText ?? "";
		BodySectionTitleText = data.BodySectionTitleText ?? "详情";
		BodyText = data.BodyText ?? "";
		ImpactSectionTitleText = data.ImpactSectionTitleText ?? "政策影响效果";
		ImpactText = data.ImpactText ?? "";
		_unreadMarkerText = data.UnreadMarkerText ?? "";
		_isUnread = data.IsUnread;
		HasPolicyName = data.HasPolicyName;
		HasImpact = data.HasImpact;
	}

	public int Index { get; }
	public string EventId { get; }
	[DataSourceProperty]
	public string KindLabel { get; }
	[DataSourceProperty]
	public string HeaderRightText { get; }
	[DataSourceProperty]
	public string DateText { get; }
	[DataSourceProperty]
	public string TitleText { get; }
	[DataSourceProperty]
	public string MetaText { get; }
	[DataSourceProperty]
	public string IndexMetaText { get; }
	[DataSourceProperty]
	public string PolicyNameText { get; }
	[DataSourceProperty]
	public string BodySectionTitleText { get; }
	[DataSourceProperty]
	public string BodyText { get; }
	[DataSourceProperty]
	public string ImpactSectionTitleText { get; }
	[DataSourceProperty]
	public string ImpactText { get; }
	[DataSourceProperty]
	public string UnreadMarkerText
	{
		get => _unreadMarkerText;
		private set
		{
			if (value != _unreadMarkerText)
			{
				_unreadMarkerText = value;
				OnPropertyChangedWithValue(value, nameof(UnreadMarkerText));
			}
		}
	}
	[DataSourceProperty]
	public bool IsUnread
	{
		get => _isUnread;
		private set
		{
			if (value != _isUnread)
			{
				_isUnread = value;
				OnPropertyChangedWithValue(value, nameof(IsUnread));
			}
		}
	}
	[DataSourceProperty]
	public bool HasPolicyName { get; }
	[DataSourceProperty]
	public bool HasImpact { get; }
	[DataSourceProperty]
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, nameof(IsSelected));
			}
		}
	}

	public void ExecuteSelect()
	{
		_select?.Invoke(Index);
	}

	public void MarkRead()
	{
		_source.IsUnread = false;
		_source.UnreadMarkerText = "";
		IsUnread = false;
		UnreadMarkerText = "";
	}
}

public sealed class WorldEventInboxPopupData
{
	public string TitleText = "世界事件";
	public string SubtitleText = "只读查看 NPC 统治者政策、民众反馈和世界事件。";
	public string EmptyStateText = "暂无世界事件。";
	public string CloseText = "关闭";
	public List<WorldEventCountryData> Countries = new List<WorldEventCountryData>();
	public int SelectedCountryIndex;
}

public sealed class WorldEventCountryData
{
	public string KingdomId = "";
	public string KingdomName = "";
	public int UnreadCount;
	public List<WorldEventRecordData> Records = new List<WorldEventRecordData>();
}

public sealed class WorldEventRecordData
{
	public string EventId = "";
	public string KindLabel = "";
	public string HeaderRightText = "";
	public string DateText = "";
	public string TitleText = "";
	public string MetaText = "";
	public string IndexMetaText = "";
	public string PolicyNameText = "";
	public string BodySectionTitleText = "";
	public string BodyText = "";
	public string ImpactSectionTitleText = "";
	public string ImpactText = "";
	public string UnreadMarkerText = "";
	public bool IsUnread;
	public bool HasPolicyName;
	public bool HasImpact;
}

public sealed class CustomPolicyComposePopup
{
	private static CustomPolicyComposePopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly CustomPolicyComposePopupVM _dataSource;

	private readonly Action<string, string, string> _onPublish;

	private readonly Action _onCancel;

	private bool _isClosed;

	private PendingCloseAction _pendingCloseAction = PendingCloseAction.None;

	private string _pendingPolicyName;

	private string _pendingPolicyContent;

	private string _pendingDateText;

	private enum PendingCloseAction
	{
		None,
		Publish,
		Cancel
	}

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	public static void ProcessDeferredCloseAction()
	{
		try
		{
			_activePopup?.ProcessPendingCloseAction();
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "compose-popup-deferred-close-failed", ex.Message, ex.ToString());
		}
	}

	private CustomPolicyComposePopup(ScreenBase screen, string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, Action<string, string, string> onPublish, Action onCancel)
	{
		_screen = screen;
		_onPublish = onPublish;
		_onCancel = onCancel;
		_dataSource = new CustomPolicyComposePopupVM(titleText, nameLabelText, contentLabelText, dateText, canPublish, blockReason, HandlePublishRequested, HandleCancelRequested);
		_layer = new GauntletLayer("CustomPolicyComposePopup", 4000, false);
	}

	public static bool Show(string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, Action<string, string, string> onPublish, Action onCancel)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			CustomPolicyComposePopup popup = new CustomPolicyComposePopup(topScreen, titleText, nameLabelText, contentLabelText, dateText, canPublish, blockReason, onPublish, onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "compose-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("CustomPolicyComposePopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandlePublishRequested(string policyName, string policyContent, string dateText)
	{
		RequestDeferredClose(PendingCloseAction.Publish, policyName ?? "", policyContent ?? "", dateText ?? "");
	}

	private void HandleCancelRequested()
	{
		RequestDeferredClose(PendingCloseAction.Cancel, null, null, null);
	}

	private void RequestDeferredClose(PendingCloseAction action, string policyName, string policyContent, string dateText)
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = action;
		_pendingPolicyName = policyName;
		_pendingPolicyContent = policyContent;
		_pendingDateText = dateText;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || _pendingCloseAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction action = _pendingCloseAction;
		string policyName = _pendingPolicyName ?? "";
		string policyContent = _pendingPolicyContent ?? "";
		string dateText = _pendingDateText ?? "";
		_pendingCloseAction = PendingCloseAction.None;
		_pendingPolicyName = null;
		_pendingPolicyContent = null;
		_pendingDateText = null;
		Close(silent: true);
		if (action == PendingCloseAction.Publish)
		{
			_onPublish?.Invoke(policyName, policyContent, dateText);
		}
		else if (action == PendingCloseAction.Cancel)
		{
			_onCancel?.Invoke();
		}
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		try
		{
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				PolicySystemLog.Failure("UI", "compose-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class CustomPolicyComposePopupVM : ViewModel
{
	private readonly Action<string, string, string> _onPublish;

	private readonly Action _onCancel;

	private bool _externalCanPublish;

	private string _titleText;

	private string _nameLabelText;

	private string _contentLabelText;

	private string _dateText;

	private string _policyName;

	private string _policyContent;

	private string _publishText;

	private string _cancelText;

	private string _statusText;

	private string _readyStatusText;

	private bool _canPublish;

	public CustomPolicyComposePopupVM(string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, Action<string, string, string> onPublish, Action onCancel)
	{
		_onPublish = onPublish;
		_onCancel = onCancel;
		_externalCanPublish = canPublish;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "撰写政策" : titleText;
		NameLabelText = string.IsNullOrWhiteSpace(nameLabelText) ? "政策名" : nameLabelText;
		ContentLabelText = string.IsNullOrWhiteSpace(contentLabelText) ? "政策内容" : contentLabelText;
		DateText = string.IsNullOrWhiteSpace(dateText) ? "未知日期" : dateText;
		PolicyName = "";
		PolicyContent = "";
		PublishText = "发布政策";
		CancelText = "取消";
		_readyStatusText = string.IsNullOrWhiteSpace(blockReason) ? "填写政策名和政策内容后即可发布。" : blockReason;
		StatusText = canPublish ? _readyStatusText : (string.IsNullOrWhiteSpace(blockReason) ? "当前不能发布政策。" : blockReason);
		RefreshCanPublish();
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string NameLabelText
	{
		get => _nameLabelText;
		set
		{
			if (value != _nameLabelText)
			{
				_nameLabelText = value;
				OnPropertyChangedWithValue(value, nameof(NameLabelText));
			}
		}
	}

	[DataSourceProperty]
	public string ContentLabelText
	{
		get => _contentLabelText;
		set
		{
			if (value != _contentLabelText)
			{
				_contentLabelText = value;
				OnPropertyChangedWithValue(value, nameof(ContentLabelText));
			}
		}
	}

	[DataSourceProperty]
	public string DateText
	{
		get => _dateText;
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, nameof(DateText));
			}
		}
	}

	[DataSourceProperty]
	public string PolicyName
	{
		get => _policyName;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, AnimusForgeTextInputSanitizer.MaxPolicyNameChars);
			if (text != _policyName)
			{
				_policyName = text;
				OnPropertyChangedWithValue(_policyName, nameof(PolicyName));
				RefreshCanPublish();
			}
		}
	}

	[DataSourceProperty]
	public string PolicyContent
	{
		get => _policyContent;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxPolicyContentChars);
			if (text != _policyContent)
			{
				_policyContent = text;
				OnPropertyChangedWithValue(_policyContent, nameof(PolicyContent));
				RefreshCanPublish();
			}
		}
	}

	[DataSourceProperty]
	public string PublishText
	{
		get => _publishText;
		set
		{
			if (value != _publishText)
			{
				_publishText = value;
				OnPropertyChangedWithValue(value, nameof(PublishText));
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get => _cancelText;
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, nameof(CancelText));
			}
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText;
		set
		{
			if (value != _statusText)
			{
				_statusText = value;
				OnPropertyChangedWithValue(value, nameof(StatusText));
			}
		}
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		set
		{
			if (value != _canPublish)
			{
				_canPublish = value;
				OnPropertyChangedWithValue(value, nameof(CanPublish));
			}
		}
	}

	public void ExecutePublish()
	{
		RefreshCanPublish();
		if (!CanPublish)
		{
			if (string.IsNullOrWhiteSpace(StatusText))
			{
				StatusText = "当前不能发布政策。";
			}
			return;
		}
		_onPublish?.Invoke(PolicyName ?? "", PolicyContent ?? "", DateText ?? "");
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}

	private void RefreshCanPublish()
	{
		bool hasName = !string.IsNullOrWhiteSpace(PolicyName);
		bool hasContent = !string.IsNullOrWhiteSpace(PolicyContent);
		CanPublish = _externalCanPublish && hasName && hasContent;
		if (_externalCanPublish)
		{
			if (!hasName)
			{
				StatusText = "请先填写政策名。";
			}
			else if (!hasContent)
			{
				StatusText = "请先填写政策内容。";
			}
			else
			{
				StatusText = string.IsNullOrWhiteSpace(_readyStatusText) ? "点击发布后将等待 LLM 评议；成功落地时扣除已配置成本。" : _readyStatusText;
			}
		}
	}
}

public sealed class PolicyHistoryData
{
	public string TitleText { get; set; } = "政策记录";

	public string SubtitleText { get; set; } = "";

	public string EmptyStateText { get; set; } = "尚无成功落地的政策记录。";

	public string CloseText { get; set; } = "返回政策管理";

	public List<PolicyHistoryRecordData> Records { get; set; } = new List<PolicyHistoryRecordData>();
}

internal sealed class LocalPolicyFiefData
{
	public string FiefId { get; set; }

	public string NameText { get; set; }

	public string TypeText { get; set; }
}

internal sealed class LocalPolicyComposeData
{
	public string DateText { get; set; }

	public bool CanPublish { get; set; }

	public string BlockReason { get; set; }

	public List<LocalPolicyFiefData> Fiefs { get; set; } = new List<LocalPolicyFiefData>();
}

internal sealed class LocalPolicyComposePopup
{
	private static LocalPolicyComposePopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly LocalPolicyComposePopupVM _dataSource;
	private readonly Action<string, string, string, string, List<string>> _onPublish;
	private readonly Action _onCancel;
	private bool _isClosed;
	private bool _publishPending;
	private bool _cancelPending;
	private string _pendingName;
	private string _pendingContent;
	private string _pendingDuration;
	private string _pendingDate;
	private List<string> _pendingFiefIds;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private LocalPolicyComposePopup(ScreenBase screen, LocalPolicyComposeData data, Action<string, string, string, string, List<string>> onPublish, Action onCancel)
	{
		_screen = screen;
		_onPublish = onPublish;
		_onCancel = onCancel;
		_dataSource = new LocalPolicyComposePopupVM(data, HandlePublishRequested, HandleCancelRequested);
		_layer = new GauntletLayer("LocalPolicyComposePopup", 4005, false);
	}

	public static bool Show(LocalPolicyComposeData data, Action<string, string, string, string, List<string>> onPublish, Action onCancel)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			LocalPolicyComposePopup popup = new LocalPolicyComposePopup(topScreen, data ?? new LocalPolicyComposeData(), onPublish, onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "local-compose-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseAction()
	{
		_activePopup?.ProcessPendingCloseAction();
	}

	private void Open()
	{
		_layer.LoadMovie("LocalPolicyComposePopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try { _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory")); } catch { }
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandlePublishRequested(string name, string content, string duration, string date, List<string> fiefIds)
	{
		if (_isClosed || _publishPending || _cancelPending) return;
		_publishPending = true;
		_pendingName = name ?? "";
		_pendingContent = content ?? "";
		_pendingDuration = duration ?? "";
		_pendingDate = date ?? "";
		_pendingFiefIds = fiefIds?.ToList() ?? new List<string>();
	}

	private void HandleCancelRequested()
	{
		if (_isClosed || _publishPending || _cancelPending) return;
		_cancelPending = true;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || (!_publishPending && !_cancelPending)) return;
		bool publish = _publishPending;
		string name = _pendingName ?? "";
		string content = _pendingContent ?? "";
		string duration = _pendingDuration ?? "";
		string date = _pendingDate ?? "";
		List<string> fiefIds = _pendingFiefIds?.ToList() ?? new List<string>();
		_publishPending = false;
		_cancelPending = false;
		Close(silent: true);
		if (publish) _onPublish?.Invoke(name, content, duration, date, fiefIds);
		else _onCancel?.Invoke();
	}

	private void Close(bool silent)
	{
		if (_isClosed) return;
		_isClosed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch { }
		try { _screen.RemoveLayer(_layer); }
		catch (Exception ex) { if (!silent) PolicySystemLog.Failure("UI", "local-compose-close-failed", ex.Message, ex.ToString()); }
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this)) _activePopup = null;
	}
}

internal sealed class LocalPolicyComposePopupVM : ViewModel
{
	private readonly Action<string, string, string, string, List<string>> _onPublish;
	private readonly Action _onCancel;
	private readonly bool _externalCanPublish;
	private readonly string _externalBlockReason;
	private string _policyName;
	private string _policyContent;
	private string _durationText;
	private string _statusText;
	private bool _canPublish;
	private int _selectedCount;

	public LocalPolicyComposePopupVM(LocalPolicyComposeData data, Action<string, string, string, string, List<string>> onPublish, Action onCancel)
	{
		data ??= new LocalPolicyComposeData();
		_onPublish = onPublish;
		_onCancel = onCancel;
		_externalCanPublish = data.CanPublish;
		_externalBlockReason = data.BlockReason ?? "";
		TitleText = "发布地方政策";
		ScopeTitleText = "选择作用封地";
		SelectAllText = "全选";
		ClearText = "清空";
		NameLabelText = "政策名";
		ContentLabelText = "政策内容";
		DurationLabelText = "持续天数（留空由 AI 决定）";
		DateText = string.IsNullOrWhiteSpace(data.DateText) ? "未知日期" : data.DateText;
		PublishText = "发布地方政策";
		CancelText = "取消";
		FiefItems = new MBBindingList<LocalPolicyFiefItemVM>();
		foreach (LocalPolicyFiefData fief in data.Fiefs ?? new List<LocalPolicyFiefData>())
		{
			if (fief != null) FiefItems.Add(new LocalPolicyFiefItemVM(fief, RefreshCanPublish));
		}
		PolicyName = "";
		PolicyContent = "";
		DurationText = "";
		RefreshCanPublish();
	}

	[DataSourceProperty] public string TitleText { get; set; }
	[DataSourceProperty] public string ScopeTitleText { get; set; }
	[DataSourceProperty] public string SelectAllText { get; set; }
	[DataSourceProperty] public string ClearText { get; set; }
	[DataSourceProperty] public string NameLabelText { get; set; }
	[DataSourceProperty] public string ContentLabelText { get; set; }
	[DataSourceProperty] public string DurationLabelText { get; set; }
	[DataSourceProperty] public string DateText { get; set; }
	[DataSourceProperty] public string PublishText { get; set; }
	[DataSourceProperty] public string CancelText { get; set; }
	[DataSourceProperty] public MBBindingList<LocalPolicyFiefItemVM> FiefItems { get; set; }

	[DataSourceProperty]
	public string PolicyName
	{
		get => _policyName;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, AnimusForgeTextInputSanitizer.MaxPolicyNameChars);
			if (text == _policyName) return;
			_policyName = text;
			OnPropertyChangedWithValue(text, nameof(PolicyName));
			RefreshCanPublish();
		}
	}

	[DataSourceProperty]
	public string PolicyContent
	{
		get => _policyContent;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxPolicyContentChars);
			if (text == _policyContent) return;
			_policyContent = text;
			OnPropertyChangedWithValue(text, nameof(PolicyContent));
			RefreshCanPublish();
		}
	}

	[DataSourceProperty]
	public string DurationText
	{
		get => _durationText;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, 16);
			if (text == _durationText) return;
			_durationText = text;
			OnPropertyChangedWithValue(text, nameof(DurationText));
			RefreshCanPublish();
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText;
		set { if (value != _statusText) { _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); } }
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		set { if (value != _canPublish) { _canPublish = value; OnPropertyChangedWithValue(value, nameof(CanPublish)); } }
	}

	[DataSourceProperty]
	public int SelectedCount
	{
		get => _selectedCount;
		set { if (value != _selectedCount) { _selectedCount = value; OnPropertyChangedWithValue(value, nameof(SelectedCount)); } }
	}

	public void ExecuteSelectAll() { foreach (LocalPolicyFiefItemVM item in FiefItems) item.SetSelected(true); RefreshCanPublish(); }
	public void ExecuteClear() { foreach (LocalPolicyFiefItemVM item in FiefItems) item.SetSelected(false); RefreshCanPublish(); }
	public void ExecuteCancel() => _onCancel?.Invoke();
	public void StartTyping() { }
	public void StopTyping() { }

	public void ExecutePublish()
	{
		RefreshCanPublish();
		if (!CanPublish) return;
		_onPublish?.Invoke(PolicyName ?? "", PolicyContent ?? "", DurationText ?? "", DateText ?? "", FiefItems.Where(x => x.IsSelected).Select(x => x.FiefId).ToList());
	}

	private void RefreshCanPublish()
	{
		SelectedCount = FiefItems?.Count(x => x.IsSelected) ?? 0;
		bool durationValid = string.IsNullOrWhiteSpace(DurationText) || (int.TryParse(DurationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) && days > 0);
		CanPublish = _externalCanPublish && SelectedCount > 0 && !string.IsNullOrWhiteSpace(PolicyName) && !string.IsNullOrWhiteSpace(PolicyContent) && durationValid;
		if (!_externalCanPublish) StatusText = string.IsNullOrWhiteSpace(_externalBlockReason) ? "当前不能发布地方政策。" : _externalBlockReason;
		else if (SelectedCount <= 0) StatusText = "请至少选择一个玩家家族拥有的城镇或城堡。";
		else if (string.IsNullOrWhiteSpace(PolicyName)) StatusText = "请填写政策名。";
		else if (string.IsNullOrWhiteSpace(PolicyContent)) StatusText = "请填写政策内容。";
		else if (!durationValid) StatusText = "持续天数必须留空或填写正 Int32。";
		else StatusText = "已选择 " + SelectedCount.ToString(CultureInfo.InvariantCulture) + " 个封地；作用范围由所选封地自动确定。";
	}
}

internal sealed class LocalPolicyFiefItemVM : ViewModel
{
	private readonly Action _onChanged;
	private bool _isSelected;
	public LocalPolicyFiefItemVM(LocalPolicyFiefData data, Action onChanged)
	{
		_onChanged = onChanged;
		FiefId = data?.FiefId ?? "";
		NameText = data?.NameText ?? "未知封地";
		TypeText = data?.TypeText ?? "封地";
	}
	[DataSourceProperty] public string FiefId { get; set; }
	[DataSourceProperty] public string NameText { get; set; }
	[DataSourceProperty] public string TypeText { get; set; }
	[DataSourceProperty]
	public bool IsSelected
	{
		get => _isSelected;
		set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); _onChanged?.Invoke(); } }
	}
	public void ExecuteToggle() => IsSelected = !IsSelected;
	public void SetSelected(bool value) => IsSelected = value;
}

internal sealed class LocalPolicyHistoryData
{
	public List<LocalPolicyHistoryRecordData> Records { get; set; } = new List<LocalPolicyHistoryRecordData>();
}

internal sealed class LocalPolicyHistoryRecordData
{
	public string RecordId { get; set; }
	public string DateText { get; set; }
	public string PolicyNameText { get; set; }
	public string StatusText { get; set; }
	public string TargetText { get; set; }
	public string RemainingText { get; set; }
	public string ContentText { get; set; }
	public string FeedbackText { get; set; }
	public string EffectText { get; set; }
	public string CostText { get; set; }
	public string CycleText { get; set; }
	public string RenewalText { get; set; }
	public bool CanRenew { get; set; }
	public bool CanAbolish { get; set; }
}

internal sealed class LocalPolicyHistoryPopup
{
	private static LocalPolicyHistoryPopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly LocalPolicyHistoryPopupVM _dataSource;
	private readonly Action _onClose;
	private bool _isClosed;
	private LocalPolicyHistoryPopup(ScreenBase screen, LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new LocalPolicyHistoryPopupVM(data, id => { Close(true); onRenew?.Invoke(id); }, id => { Close(true); onAbolish?.Invoke(id); }, HandleClose);
		_layer = new GauntletLayer("LocalPolicyHistoryPopup", 4110, false);
	}
	public static bool Show(LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action onClose)
	{
		ScreenBase screen = ScreenManager.TopScreen;
		if (screen == null) return false;
		try
		{
			_activePopup?.Close(true);
			LocalPolicyHistoryPopup popup = new LocalPolicyHistoryPopup(screen, data ?? new LocalPolicyHistoryData(), onRenew, onAbolish, onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "local-history-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(true);
			_activePopup = null;
			return false;
		}
	}
	private void Open()
	{
		_layer.LoadMovie("LocalPolicyHistoryPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}
	private void HandleClose() { Close(true); _onClose?.Invoke(); }
	private void Close(bool silent)
	{
		if (_isClosed) return;
		_isClosed = true;
		try { _layer.InputRestrictions.ResetInputRestrictions(); _layer.IsFocusLayer = false; ScreenManager.TryLoseFocus(_layer); } catch { }
		try { _screen.RemoveLayer(_layer); } catch (Exception ex) { if (!silent) PolicySystemLog.Failure("UI", "local-history-close-failed", ex.Message, ex.ToString()); }
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this)) _activePopup = null;
	}
}

internal sealed class LocalPolicyHistoryPopupVM : ViewModel
{
	private readonly Action<string> _onRenew;
	private readonly Action<string> _onAbolish;
	private readonly Action _onClose;
	private LocalPolicyHistoryRecordItemVM _selected;
	private bool _hasRecords;
	private bool _showEmptyState;
	private bool _canRenew;
	private bool _canAbolish;
	private string _policyNameText = "";
	private string _statusText = "";
	private string _targetText = "";
	private string _remainingText = "";
	private string _contentText = "";
	private string _feedbackText = "";
	private string _effectText = "";
	private string _costText = "";
	private string _cycleText = "";
	private string _renewalText = "";
	public LocalPolicyHistoryPopupVM(LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action onClose)
	{
		_onRenew = onRenew; _onAbolish = onAbolish; _onClose = onClose;
		TitleText = "地方政策记录"; SubtitleText = "有效记录全部保留；已结束记录只保留最近 100 条。"; EmptyStateText = "尚无地方政策记录。";
		RenewText = "续约"; AbolishText = "废除"; CloseText = "返回地方政策";
		RecordItems = new MBBindingList<LocalPolicyHistoryRecordItemVM>();
		foreach (LocalPolicyHistoryRecordData record in data?.Records ?? new List<LocalPolicyHistoryRecordData>()) RecordItems.Add(new LocalPolicyHistoryRecordItemVM(record, Select));
		HasRecords = RecordItems.Count > 0; ShowEmptyState = !HasRecords;
		if (HasRecords) Select(RecordItems[0]);
	}
	[DataSourceProperty] public string TitleText { get; set; }
	[DataSourceProperty] public string SubtitleText { get; set; }
	[DataSourceProperty] public string EmptyStateText { get; set; }
	[DataSourceProperty] public string RenewText { get; set; }
	[DataSourceProperty] public string AbolishText { get; set; }
	[DataSourceProperty] public string CloseText { get; set; }
	[DataSourceProperty] public MBBindingList<LocalPolicyHistoryRecordItemVM> RecordItems { get; set; }
	[DataSourceProperty] public bool HasRecords { get => _hasRecords; set { if (value != _hasRecords) { _hasRecords = value; OnPropertyChangedWithValue(value, nameof(HasRecords)); } } }
	[DataSourceProperty] public bool ShowEmptyState { get => _showEmptyState; set { if (value != _showEmptyState) { _showEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowEmptyState)); } } }
	[DataSourceProperty] public bool CanRenew { get => _canRenew; set { if (value != _canRenew) { _canRenew = value; OnPropertyChangedWithValue(value, nameof(CanRenew)); } } }
	[DataSourceProperty] public bool CanAbolish { get => _canAbolish; set { if (value != _canAbolish) { _canAbolish = value; OnPropertyChangedWithValue(value, nameof(CanAbolish)); } } }
	[DataSourceProperty] public string PolicyNameText { get => _policyNameText; set { _policyNameText = value; OnPropertyChangedWithValue(value, nameof(PolicyNameText)); } }
	[DataSourceProperty] public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); } }
	[DataSourceProperty] public string TargetText { get => _targetText; set { _targetText = value; OnPropertyChangedWithValue(value, nameof(TargetText)); } }
	[DataSourceProperty] public string RemainingText { get => _remainingText; set { _remainingText = value; OnPropertyChangedWithValue(value, nameof(RemainingText)); } }
	[DataSourceProperty] public string ContentText { get => _contentText; set { _contentText = value; OnPropertyChangedWithValue(value, nameof(ContentText)); } }
	[DataSourceProperty] public string FeedbackText { get => _feedbackText; set { _feedbackText = value; OnPropertyChangedWithValue(value, nameof(FeedbackText)); } }
	[DataSourceProperty] public string EffectText { get => _effectText; set { _effectText = value; OnPropertyChangedWithValue(value, nameof(EffectText)); } }
	[DataSourceProperty] public string CostText { get => _costText; set { _costText = value; OnPropertyChangedWithValue(value, nameof(CostText)); } }
	[DataSourceProperty] public string CycleText { get => _cycleText; set { _cycleText = value; OnPropertyChangedWithValue(value, nameof(CycleText)); } }
	[DataSourceProperty] public string RenewalText { get => _renewalText; set { _renewalText = value; OnPropertyChangedWithValue(value, nameof(RenewalText)); } }
	private void Select(LocalPolicyHistoryRecordItemVM item)
	{
		if (_selected != null) _selected.IsSelected = false;
		_selected = item;
		if (item == null) return;
		item.IsSelected = true;
		PolicyNameText = item.PolicyNameText; StatusText = item.StatusText; TargetText = item.TargetText; RemainingText = item.RemainingText;
		ContentText = item.ContentText; FeedbackText = item.FeedbackText; EffectText = item.EffectText; CostText = item.CostText; CycleText = item.CycleText; RenewalText = item.RenewalText;
		CanRenew = item.CanRenew; CanAbolish = item.CanAbolish;
	}
	public void ExecuteRenew() { if (CanRenew && _selected != null) _onRenew?.Invoke(_selected.RecordId); }
	public void ExecuteAbolish() { if (CanAbolish && _selected != null) _onAbolish?.Invoke(_selected.RecordId); }
	public void ExecuteClose() => _onClose?.Invoke();
}

internal sealed class LocalPolicyHistoryRecordItemVM : ViewModel
{
	private readonly Action<LocalPolicyHistoryRecordItemVM> _onSelect;
	private bool _isSelected;
	public LocalPolicyHistoryRecordItemVM(LocalPolicyHistoryRecordData data, Action<LocalPolicyHistoryRecordItemVM> onSelect)
	{
		_onSelect = onSelect; RecordId = data?.RecordId ?? ""; DateText = data?.DateText ?? ""; PolicyNameText = data?.PolicyNameText ?? ""; StatusText = data?.StatusText ?? "";
		TargetText = data?.TargetText ?? ""; RemainingText = data?.RemainingText ?? ""; ContentText = data?.ContentText ?? ""; FeedbackText = data?.FeedbackText ?? "";
		EffectText = data?.EffectText ?? ""; CostText = data?.CostText ?? ""; CycleText = data?.CycleText ?? ""; RenewalText = data?.RenewalText ?? ""; CanRenew = data?.CanRenew == true; CanAbolish = data?.CanAbolish == true;
	}
	[DataSourceProperty] public string RecordId { get; set; }
	[DataSourceProperty] public string DateText { get; set; }
	[DataSourceProperty] public string PolicyNameText { get; set; }
	[DataSourceProperty] public string StatusText { get; set; }
	[DataSourceProperty] public string TargetText { get; set; }
	[DataSourceProperty] public string RemainingText { get; set; }
	[DataSourceProperty] public string ContentText { get; set; }
	[DataSourceProperty] public string FeedbackText { get; set; }
	[DataSourceProperty] public string EffectText { get; set; }
	[DataSourceProperty] public string CostText { get; set; }
	[DataSourceProperty] public string CycleText { get; set; }
	[DataSourceProperty] public string RenewalText { get; set; }
	[DataSourceProperty] public bool CanRenew { get; set; }
	[DataSourceProperty] public bool CanAbolish { get; set; }
	[DataSourceProperty] public bool IsSelected { get => _isSelected; set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); } } }
	public void ExecuteSelect() => _onSelect?.Invoke(this);
}

public sealed class PolicyHistoryRecordData
{
	public string DateText { get; set; }

	public string PolicyNameText { get; set; }

	public string CostText { get; set; }

	public string ContentSectionTitleText { get; set; }

	public string ContentSummaryText { get; set; }

	public string FeedbackSectionTitleText { get; set; }

	public string FeedbackSummaryText { get; set; }

	public string ImpactSectionTitleText { get; set; }

	public string ImpactSummaryText { get; set; }
}

public sealed class CustomPolicyResultPopup
{
	private static CustomPolicyResultPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly CustomPolicyResultPopupVM _dataSource;

	private readonly Action _onClose;

	private bool _isClosed;

	private CustomPolicyResultPopup(ScreenBase screen, string titleText, string bodyText, string closeText, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new CustomPolicyResultPopupVM(titleText, bodyText, closeText, HandleCloseRequested);
		_layer = new GauntletLayer("CustomPolicyResultPopup", 4150, false);
	}

	public static bool Show(string titleText, string bodyText, string closeText, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			if (_activePopup != null)
			{
			}
			_activePopup?.Close(silent: true);
			CustomPolicyResultPopup popup = new CustomPolicyResultPopup(topScreen, titleText, bodyText, closeText, onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("CustomPolicyResultPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-hotkey-register-failed", ex.Message, ex.ToString());
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandleCloseRequested()
	{
		Close(silent: true);
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-focus-reset-failed", ex.Message, ex.ToString());
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				PolicySystemLog.Failure("UI", "result-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
		try
		{
			_onClose?.Invoke();
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-after-close-failed", ex.Message, ex.ToString());
		}
	}
}

public sealed class CustomPolicyResultPopupVM : ViewModel
{
	private readonly Action _onClose;

	private string _titleText;

	private string _bodyText;

	private string _closeText;

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get => _bodyText;
		set
		{
			if (value != _bodyText)
			{
				_bodyText = value;
				OnPropertyChangedWithValue(value, nameof(BodyText));
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get => _closeText;
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, nameof(CloseText));
			}
		}
	}

	public CustomPolicyResultPopupVM(string titleText, string bodyText, string closeText, Action onClose)
	{
		_onClose = onClose;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "政策已经发布" : titleText.Trim();
		BodyText = (bodyText ?? "").Trim();
		CloseText = string.IsNullOrWhiteSpace(closeText) ? "知道了" : closeText.Trim();
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}

public sealed class CustomPolicyHistoryPopup
{
	private static CustomPolicyHistoryPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly CustomPolicyHistoryPopupVM _dataSource;

	private readonly Action _onClose;

	private bool _isClosed;

	private CustomPolicyHistoryPopup(ScreenBase screen, PolicyHistoryData data, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new CustomPolicyHistoryPopupVM(data, HandleCloseRequested);
		_layer = new GauntletLayer("CustomPolicyHistoryPopup", 4100, false);
	}

	public static bool Show(PolicyHistoryData data, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			CustomPolicyHistoryPopup popup = new CustomPolicyHistoryPopup(topScreen, data ?? new PolicyHistoryData(), onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "history-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("CustomPolicyHistoryPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandleCloseRequested()
	{
		Close(silent: true);
		_onClose?.Invoke();
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				PolicySystemLog.Failure("UI", "history-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class CustomPolicyHistoryPopupVM : ViewModel
{
	private readonly Action _onClose;

	private string _titleText;

	private string _subtitleText;

	private string _emptyStateText;

	private string _closeText;

	private bool _hasRecords;

	private bool _showEmptyState;

	private MBBindingList<CustomPolicyHistoryRecordItemVM> _recordItems;

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	[DataSourceProperty]
	public string EmptyStateText
	{
		get => _emptyStateText;
		set
		{
			if (value != _emptyStateText)
			{
				_emptyStateText = value;
				OnPropertyChangedWithValue(value, nameof(EmptyStateText));
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get => _closeText;
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, nameof(CloseText));
			}
		}
	}

	[DataSourceProperty]
	public bool HasRecords
	{
		get => _hasRecords;
		set
		{
			if (value != _hasRecords)
			{
				_hasRecords = value;
				OnPropertyChangedWithValue(value, nameof(HasRecords));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowEmptyState
	{
		get => _showEmptyState;
		set
		{
			if (value != _showEmptyState)
			{
				_showEmptyState = value;
				OnPropertyChangedWithValue(value, nameof(ShowEmptyState));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CustomPolicyHistoryRecordItemVM> RecordItems
	{
		get => _recordItems;
		set
		{
			if (value != _recordItems)
			{
				_recordItems = value;
				OnPropertyChangedWithValue(value, nameof(RecordItems));
			}
		}
	}

	public CustomPolicyHistoryPopupVM(PolicyHistoryData data, Action onClose)
	{
		_onClose = onClose;
		PolicyHistoryData source = data ?? new PolicyHistoryData();
		TitleText = string.IsNullOrWhiteSpace(source.TitleText) ? "政策记录" : source.TitleText.Trim();
		SubtitleText = (source.SubtitleText ?? "").Trim();
		EmptyStateText = string.IsNullOrWhiteSpace(source.EmptyStateText) ? "尚无成功落地的政策记录。" : source.EmptyStateText.Trim();
		CloseText = string.IsNullOrWhiteSpace(source.CloseText) ? "返回政策管理" : source.CloseText.Trim();
		RecordItems = new MBBindingList<CustomPolicyHistoryRecordItemVM>();
		if (source.Records != null)
		{
			foreach (PolicyHistoryRecordData record in source.Records)
			{
				if (record != null)
				{
					RecordItems.Add(new CustomPolicyHistoryRecordItemVM(record));
				}
			}
		}
		HasRecords = RecordItems.Count > 0;
		ShowEmptyState = !HasRecords;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}

public sealed class CustomPolicyHistoryRecordItemVM : ViewModel
{
	private string _dateText;

	private string _policyNameText;

	private string _costText;

	private string _contentSectionTitleText;

	private string _contentSummaryText;

	private string _feedbackSectionTitleText;

	private string _feedbackSummaryText;

	private string _impactSectionTitleText;

	private string _impactSummaryText;

	[DataSourceProperty]
	public string DateText
	{
		get => _dateText;
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, nameof(DateText));
			}
		}
	}

	[DataSourceProperty]
	public string PolicyNameText
	{
		get => _policyNameText;
		set
		{
			if (value != _policyNameText)
			{
				_policyNameText = value;
				OnPropertyChangedWithValue(value, nameof(PolicyNameText));
			}
		}
	}

	[DataSourceProperty]
	public string CostText
	{
		get => _costText;
		set
		{
			if (value != _costText)
			{
				_costText = value;
				OnPropertyChangedWithValue(value, nameof(CostText));
			}
		}
	}

	[DataSourceProperty]
	public string ContentSectionTitleText
	{
		get => _contentSectionTitleText;
		set
		{
			if (value != _contentSectionTitleText)
			{
				_contentSectionTitleText = value;
				OnPropertyChangedWithValue(value, nameof(ContentSectionTitleText));
			}
		}
	}

	[DataSourceProperty]
	public string ContentSummaryText
	{
		get => _contentSummaryText;
		set
		{
			if (value != _contentSummaryText)
			{
				_contentSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(ContentSummaryText));
			}
		}
	}

	[DataSourceProperty]
	public string FeedbackSectionTitleText
	{
		get => _feedbackSectionTitleText;
		set
		{
			if (value != _feedbackSectionTitleText)
			{
				_feedbackSectionTitleText = value;
				OnPropertyChangedWithValue(value, nameof(FeedbackSectionTitleText));
			}
		}
	}

	[DataSourceProperty]
	public string FeedbackSummaryText
	{
		get => _feedbackSummaryText;
		set
		{
			if (value != _feedbackSummaryText)
			{
				_feedbackSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(FeedbackSummaryText));
			}
		}
	}

	[DataSourceProperty]
	public string ImpactSectionTitleText
	{
		get => _impactSectionTitleText;
		set
		{
			if (value != _impactSectionTitleText)
			{
				_impactSectionTitleText = value;
				OnPropertyChangedWithValue(value, nameof(ImpactSectionTitleText));
			}
		}
	}

	[DataSourceProperty]
	public string ImpactSummaryText
	{
		get => _impactSummaryText;
		set
		{
			if (value != _impactSummaryText)
			{
				_impactSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(ImpactSummaryText));
			}
		}
	}

	public CustomPolicyHistoryRecordItemVM(PolicyHistoryRecordData record)
	{
		DateText = (record?.DateText ?? "未知日期").Trim();
		PolicyNameText = (record?.PolicyNameText ?? "未命名政策").Trim();
		CostText = (record?.CostText ?? "").Trim();
		ContentSectionTitleText = string.IsNullOrWhiteSpace(record?.ContentSectionTitleText) ? "【政策内容】" : record.ContentSectionTitleText.Trim();
		ContentSummaryText = (record?.ContentSummaryText ?? "").Trim();
		FeedbackSectionTitleText = string.IsNullOrWhiteSpace(record?.FeedbackSectionTitleText) ? "【民众反馈】" : record.FeedbackSectionTitleText.Trim();
		FeedbackSummaryText = (record?.FeedbackSummaryText ?? "").Trim();
		ImpactSectionTitleText = string.IsNullOrWhiteSpace(record?.ImpactSectionTitleText) ? "【每日影响】" : record.ImpactSectionTitleText.Trim();
		ImpactSummaryText = (record?.ImpactSummaryText ?? "").Trim();
	}
}
