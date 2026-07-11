using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeWorldEventOverlay
{
	private const int EventInboxDisplayLimit = 160;
	private const int DetailBodyCharacterLimit = 1200;
	private const int DetailBodyLineLimit = 40;
	private const float ToggleButtonLeft = 616f;
	private const float ToggleButtonBottom = 18f;
	private const float ToggleButtonWidth = 120f;
	private const float ToggleButtonHeight = 38f;

	private static AnimusForgeWorldEventOverlay _active;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly AnimusForgeWorldEventOverlayVM _dataSource;
	private GauntletMovieIdentifier _movie;
	private bool _closed;
	private long _lastInboxVersion = -1;

	private AnimusForgeWorldEventOverlay(ScreenBase screen)
	{
		_screen = screen;
		_dataSource = new AnimusForgeWorldEventOverlayVM(HandleOpenInboxPopup);
		_layer = new GauntletLayer("AnimusForgeWorldEventOverlay", 260, false);
	}

	public static void OnApplicationTick()
	{
		try
		{
			AnimusForgeWorldEventInboxPopup.OnApplicationTick();
			ScreenBase top = ScreenManager.TopScreen;
			if (Campaign.Current == null || !(top is MapScreen))
			{
				CloseActive();
				AnimusForgeWorldEventInboxPopup.CloseActive(silent: true);
				return;
			}
			if (_active == null || _active._closed)
			{
				_active = new AnimusForgeWorldEventOverlay(top);
				_active.Open();
			}
			if (!ReferenceEquals(_active._screen, top))
			{
				CloseActive();
				_active = new AnimusForgeWorldEventOverlay(top);
				_active.Open();
			}
			_active.Tick();
		}
		catch (Exception ex)
		{
			Logger.LogTrace("WorldEventOverlay", "OnApplicationTick failed: " + ex.Message);
		}
	}

	public static void CloseActive()
	{
		_active?.Close();
	}

	private void Open()
	{
		_movie = _layer.LoadMovie("AnimusForgeWorldEventOverlay", _dataSource);
		_layer.IsFocusLayer = false;
		_screen.AddLayer(_layer);
		Refresh(force: true);
		UpdateOverlayAreaInputRestrictions();
	}

	private void Tick()
	{
		if (_closed)
		{
			return;
		}
		long version = NpcPublicFeedbackEventBehavior.GetInboxVersionForExternal();
		if (version != _lastInboxVersion)
		{
			Refresh(force: true);
		}
		UpdateOverlayAreaInputRestrictions();
	}

	private void Refresh(bool force)
	{
		try
		{
			_lastInboxVersion = NpcPublicFeedbackEventBehavior.GetInboxVersionForExternal();
			int unread = NpcPublicFeedbackEventBehavior.GetUnreadCountForExternal();
			_dataSource.UnreadText = unread > 0 ? "事件(" + unread.ToString(CultureInfo.InvariantCulture) + ")" : "事件";
		}
		catch (Exception ex)
		{
			Logger.LogTrace("WorldEventOverlay", "Refresh failed: " + ex.Message);
		}
	}

	private void HandleOpenInboxPopup()
	{
		try
		{
			Refresh(force: true);
			WorldEventInboxPopupData data = BuildInboxPopupData();
			if (!AnimusForgeWorldEventInboxPopup.Show(data))
			{
				Logger.LogTrace("WorldEventOverlay", "failed to open world event inbox popup");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("WorldEventOverlay", "OpenInboxPopup failed: " + ex.Message);
		}
	}

	private void Close()
	{
		if (_closed)
		{
			return;
		}
		_closed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			_screen.RemoveLayer(_layer);
		}
		catch
		{
		}
		try
		{
			_movie = null;
			_dataSource.OnFinalize();
		}
		catch
		{
		}
		if (ReferenceEquals(_active, this))
		{
			_active = null;
		}
	}

	private void UpdateOverlayAreaInputRestrictions()
	{
		try
		{
			if (AnimusForgeWorldEventInboxPopup.IsOpen)
			{
				_layer.InputRestrictions.ResetInputRestrictions();
				_layer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(_layer);
				return;
			}
			if (IsMouseOverToggleButton())
			{
				_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
			}
			else
			{
				_layer.InputRestrictions.ResetInputRestrictions();
			}
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
	}

	private bool IsMouseOverToggleButton()
	{
		try
		{
			Vec2 mouse = Input.MousePositionPixel;
			float height = TaleWorlds.Engine.Screen.RealScreenResolutionHeight;
			if (height <= 0f)
			{
				return false;
			}
			return IsMouseInside(mouse, ToggleButtonLeft, height - ToggleButtonBottom - ToggleButtonHeight, ToggleButtonWidth, ToggleButtonHeight);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMouseInside(Vec2 mouse, float left, float top, float width, float height)
	{
		return mouse.x >= left && mouse.x <= left + width && mouse.y >= top && mouse.y <= top + height;
	}

	private static WorldEventInboxPopupData BuildInboxPopupData()
	{
		List<AnimusForgeWorldEventInboxEntry> events = NpcPublicFeedbackEventBehavior.GetInboxSnapshotForExternal(EventInboxDisplayLimit);
		List<WorldEventCountryGroup> countries = BuildCountryGroups(events);
		WorldEventInboxPopupData data = new WorldEventInboxPopupData
		{
			TitleText = "世界事件",
			SubtitleText = "只读查看 NPC 统治者政策、民众反馈和世界事件。",
			EmptyStateText = "暂无世界事件。NPC 统治者政策与民众反馈会出现在这里。",
			CloseText = "关闭"
		};

		foreach (WorldEventCountryGroup country in countries)
		{
			WorldEventCountryData countryData = new WorldEventCountryData
			{
				KingdomId = country.KingdomId ?? "",
				KingdomName = FirstNonEmpty(country.KingdomName, country.KingdomId, "未知国家"),
				PolicyCount = country.Events.Count(IsPolicyEvent),
				FeedbackCount = country.Events.Count(IsFeedbackEvent),
				WorldEventCount = country.Events.Count(e => e != null && !IsPolicyEvent(e) && !IsFeedbackEvent(e)),
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
		string kind = GetEventKindLabel(entry);
		string date = FirstNonEmpty(entry.GameDate, entry.Day > 0 ? ("第" + entry.Day.ToString(CultureInfo.InvariantCulture) + "天") : "未知日期");
		string title = FirstNonEmpty(entry.Title, entry.PolicyName, kind);
		string body = LimitMultiline(FirstNonEmpty(entry.DetailText, entry.Summary), DetailBodyCharacterLimit, DetailBodyLineLimit);
		string policyName = string.IsNullOrWhiteSpace(entry.PolicyName) ? "" : "关联政策：《" + entry.PolicyName.Trim() + "》";
		string meta = BuildRecordMetaText(entry, kind, date);
		string footer = BuildRecordFooterText(entry);
		return new WorldEventRecordData
		{
			EventId = entry.EventId ?? "",
			KindLabel = kind,
			DateText = date,
			TitleText = title,
			MetaText = meta,
			PolicyNameText = policyName,
			BodyText = string.IsNullOrWhiteSpace(body) ? "（无详情）" : body,
			FooterText = footer,
			UnreadMarkerText = entry.IsRead ? "" : "新",
			IsUnread = !entry.IsRead,
			HasPolicyName = !string.IsNullOrWhiteSpace(policyName),
			HasFooter = !string.IsNullOrWhiteSpace(footer)
		};
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
			parts.Add("发布者：" + actor);
		}
		return string.Join("  ·  ", parts);
	}

	private static string BuildRecordFooterText(AnimusForgeWorldEventInboxEntry entry)
	{
		return "";
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
				.ToList();
		}

		return groups
			.OrderByDescending(g => g.Events.Count > 0)
			.ThenBy(g => g.KingdomName ?? g.KingdomId ?? "", StringComparer.OrdinalIgnoreCase)
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

	private static bool IsPolicyEvent(AnimusForgeWorldEventInboxEntry entry)
	{
		return string.Equals((entry?.EventKind ?? "").Trim(), "npc_ruler_policy", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsFeedbackEvent(AnimusForgeWorldEventInboxEntry entry)
	{
		return string.Equals((entry?.EventKind ?? "").Trim(), "npc_public_feedback", StringComparison.OrdinalIgnoreCase);
	}

	private static string GetEventKindLabel(AnimusForgeWorldEventInboxEntry entry)
	{
		if (IsPolicyEvent(entry))
		{
			return "统治者政策";
		}
		if (IsFeedbackEvent(entry))
		{
			return "民众反馈";
		}
		return "世界事件";
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

public sealed class AnimusForgeWorldEventOverlayVM : ViewModel
{
	private readonly Action _openInbox;
	private string _unreadText = "事件";

	public AnimusForgeWorldEventOverlayVM(Action openInbox)
	{
		_openInbox = openInbox;
	}

	[DataSourceProperty]
	public string UnreadText
	{
		get => _unreadText;
		set
		{
			if (value != _unreadText)
			{
				_unreadText = value;
				OnPropertyChangedWithValue(value, nameof(UnreadText));
			}
		}
	}

	public void TogglePanel() => _openInbox?.Invoke();
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
			Logger.LogTrace("WorldEventOverlay", "Failed to open world event inbox popup: " + ex.Message);
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
				Logger.LogTrace("WorldEventOverlay", "Failed to remove world event inbox popup layer: " + ex.Message);
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
	private bool _hasEvents;
	private bool _showEmptyState;
	private bool _selectedCountryHasRecords;
	private bool _showSelectedCountryEmptyState;
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
	public bool HasEvents { get => _hasEvents; set { if (value != _hasEvents) { _hasEvents = value; OnPropertyChangedWithValue(value, nameof(HasEvents)); } } }
	[DataSourceProperty]
	public bool ShowEmptyState { get => _showEmptyState; set { if (value != _showEmptyState) { _showEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowEmptyState)); } } }
	[DataSourceProperty]
	public bool SelectedCountryHasRecords { get => _selectedCountryHasRecords; set { if (value != _selectedCountryHasRecords) { _selectedCountryHasRecords = value; OnPropertyChangedWithValue(value, nameof(SelectedCountryHasRecords)); } } }
	[DataSourceProperty]
	public bool ShowSelectedCountryEmptyState { get => _showSelectedCountryEmptyState; set { if (value != _showSelectedCountryEmptyState) { _showSelectedCountryEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowSelectedCountryEmptyState)); } } }
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
		foreach (WorldEventRecordData record in selected.Source.Records ?? new List<WorldEventRecordData>())
		{
			if (record != null)
			{
				RecordItems.Add(new WorldEventRecordItemVM(record));
			}
		}
		SelectedCountryTitleText = BuildSelectedCountryTitle(selected);
		SelectedCountryHasRecords = RecordItems.Count > 0;
		ShowSelectedCountryEmptyState = HasEvents && !SelectedCountryHasRecords;
	}

	private static string BuildSelectedCountryTitle(WorldEventCountryItemVM country)
	{
		if (country == null)
		{
			return "世界事件";
		}
		return country.KingdomName + "：共 " + country.TotalCount.ToString(CultureInfo.InvariantCulture)
			+ " 条，政策 " + country.PolicyCount.ToString(CultureInfo.InvariantCulture)
			+ " / 反馈 " + country.FeedbackCount.ToString(CultureInfo.InvariantCulture)
			+ " / 其他 " + country.WorldEventCount.ToString(CultureInfo.InvariantCulture);
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
	public int PolicyCount => Math.Max(0, Source.PolicyCount);
	public int FeedbackCount => Math.Max(0, Source.FeedbackCount);
	public int WorldEventCount => Math.Max(0, Source.WorldEventCount);
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
	public string CountText
	{
		get
		{
			return "政策 " + PolicyCount.ToString(CultureInfo.InvariantCulture)
				+ " / 反馈 " + FeedbackCount.ToString(CultureInfo.InvariantCulture)
				+ " / 其他 " + WorldEventCount.ToString(CultureInfo.InvariantCulture);
		}
	}

	[DataSourceProperty]
	public string UnreadText => HasUnread ? ("新 " + UnreadCount.ToString(CultureInfo.InvariantCulture)) : "";

	public void ExecuteSelect()
	{
		_select?.Invoke(Index);
	}

	private void UpdateSelectionText()
	{
		SelectionText = "";
	}
}

public sealed class WorldEventRecordItemVM : ViewModel
{
	public WorldEventRecordItemVM(WorldEventRecordData source)
	{
		WorldEventRecordData data = source ?? new WorldEventRecordData();
		KindLabel = data.KindLabel ?? "世界事件";
		DateText = data.DateText ?? "";
		TitleText = data.TitleText ?? "世界事件";
		MetaText = data.MetaText ?? "";
		PolicyNameText = data.PolicyNameText ?? "";
		BodyText = data.BodyText ?? "";
		FooterText = data.FooterText ?? "";
		UnreadMarkerText = data.UnreadMarkerText ?? "";
		IsUnread = data.IsUnread;
		HasPolicyName = data.HasPolicyName;
		HasFooter = data.HasFooter;
	}

	[DataSourceProperty]
	public string KindLabel { get; }
	[DataSourceProperty]
	public string DateText { get; }
	[DataSourceProperty]
	public string TitleText { get; }
	[DataSourceProperty]
	public string MetaText { get; }
	[DataSourceProperty]
	public string PolicyNameText { get; }
	[DataSourceProperty]
	public string BodyText { get; }
	[DataSourceProperty]
	public string FooterText { get; }
	[DataSourceProperty]
	public string UnreadMarkerText { get; }
	[DataSourceProperty]
	public bool IsUnread { get; }
	[DataSourceProperty]
	public bool HasPolicyName { get; }
	[DataSourceProperty]
	public bool HasFooter { get; }
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
	public int PolicyCount;
	public int FeedbackCount;
	public int WorldEventCount;
	public int UnreadCount;
	public List<WorldEventRecordData> Records = new List<WorldEventRecordData>();
}

public sealed class WorldEventRecordData
{
	public string EventId = "";
	public string KindLabel = "";
	public string DateText = "";
	public string TitleText = "";
	public string MetaText = "";
	public string PolicyNameText = "";
	public string BodyText = "";
	public string FooterText = "";
	public string UnreadMarkerText = "";
	public bool IsUnread;
	public bool HasPolicyName;
	public bool HasFooter;
}
