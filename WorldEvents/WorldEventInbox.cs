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
			if (!AnimusForgeWorldEventBehavior.MarkEventReadForExternal(selected.EventId))
			{
				WorldDiplomacyBehavior.MarkDocumentReadForExternal(selected.EventId);
			}
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
		IndexTitleText = string.IsNullOrWhiteSpace(data.IndexTitleText) ? TitleText : data.IndexTitleText;
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
	public string IndexTitleText { get; }
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
	public string IndexTitleText = "";
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
