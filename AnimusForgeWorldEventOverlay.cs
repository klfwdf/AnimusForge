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
		Dictionary<string, NpcRulerPolicyRecord> policiesById = NpcRulerPolicyBehavior.GetRecentPolicyRecordsForExternal(null, 200)
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.PolicyId))
			.GroupBy(x => x.PolicyId.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		List<WorldEventCountryGroup> countries = BuildCountryGroups(events);
		WorldEventInboxPopupData data = new WorldEventInboxPopupData
		{
			TitleText = "世界事件",
			SubtitleText = "只读查看玩家与 NPC 统治者政策、政策衍生事件和世界事件。",
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
				WorldEventRecordData record = BuildRecordData(entry, policiesById);
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

	private static WorldEventRecordData BuildRecordData(AnimusForgeWorldEventInboxEntry entry, IReadOnlyDictionary<string, NpcRulerPolicyRecord> policiesById)
	{
		if (entry == null)
		{
			return null;
		}
		string kind = GetEventKindLabel(entry);
		string date = FirstNonEmpty(entry.GameDate, entry.Day > 0 ? ("第" + entry.Day.ToString(CultureInfo.InvariantCulture) + "天") : "未知日期");
		NpcRulerPolicyRecord policy = FindPolicyRecord(entry.PolicyId, policiesById);
		bool isPolicy = IsPolicyEvent(entry);
		bool isPolicyEvent = IsFeedbackEvent(entry);
		string title = isPolicy
			? FirstNonEmpty(policy?.PolicyName, entry.PolicyName, entry.Title, kind)
			: FirstNonEmpty(entry.Title, entry.PolicyName, kind);
		string body = isPolicy
			? FirstNonEmpty(policy?.PolicyContent, ExtractDetailLineValue(entry.DetailText, "政策："), entry.DetailText, entry.Summary)
			: FirstNonEmpty(entry.DetailText, entry.Summary, policy?.PublicFeedback);
		body = (body ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
		string policyName = string.IsNullOrWhiteSpace(entry.PolicyName)
			? ""
			: "关联政策：《" + entry.PolicyName.Trim() + "》";
		string headerRight = isPolicy ? kind : policyName;
		string meta = BuildRecordMetaText(entry, kind, date);
		string impact = isPolicy
			? BuildPolicyImpactDisplay(policy)
			: "";
		return new WorldEventRecordData
		{
			EventId = entry.EventId ?? "",
			KindLabel = kind,
			HeaderRightText = headerRight,
			DateText = date,
			TitleText = title,
			MetaText = meta,
			PolicyNameText = "",
			BodyText = string.IsNullOrWhiteSpace(body) ? "（无详情）" : body,
			BodySectionTitleText = isPolicy ? "政策内容" : (isPolicyEvent ? "事件经过" : "事件详情"),
			ImpactSectionTitleText = isPolicy ? "政策影响效果" : "",
			ImpactText = impact,
			IndexMetaText = date + "  ·  " + kind,
			UnreadMarkerText = entry.IsRead ? "" : "新",
			IsUnread = !entry.IsRead,
			HasPolicyName = false,
			HasImpact = !string.IsNullOrWhiteSpace(impact)
		};
	}

	private static NpcRulerPolicyRecord FindPolicyRecord(string policyId, IReadOnlyDictionary<string, NpcRulerPolicyRecord> policiesById)
	{
		string id = (policyId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(id) && policiesById != null && policiesById.TryGetValue(id, out NpcRulerPolicyRecord policy)
			? policy
			: null;
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

	private static string BuildPolicyImpactDisplay(NpcRulerPolicyRecord policy)
	{
		List<string> sections = new List<string>();
		List<NpcRulerPolicyEffectDto> effects = policy?.Effects?.Where(x => x != null).ToList() ?? new List<NpcRulerPolicyEffectDto>();
		for (int i = 0; i < effects.Count; i++)
		{
			NpcRulerPolicyEffectDto effect = effects[i];
			List<string> deltas = new List<string>();
			AddEffectDelta(deltas, "繁荣", effect.ProsperityDailyDeltaPerTown, "/城镇");
			AddEffectDelta(deltas, "粮食", effect.FoodDailyDeltaPerTown, "/城镇");
			AddEffectDelta(deltas, "炉火", effect.HearthDailyDeltaPerVillage, "/村庄");
			AddEffectDelta(deltas, "忠诚", effect.LoyaltyDailyDeltaPerTown, "/城镇");
			AddEffectDelta(deltas, "治安", effect.SecurityDailyDeltaPerTown, "/城镇");
			AddEffectDelta(deltas, "民兵", effect.MilitiaDailyDeltaPerTown, "/城镇");
			AddEffectDelta(deltas, "王国稳定度", effect.KingdomStabilityDailyDelta, "");

			List<string> lines = new List<string>
			{
				"影响国家：" + FirstNonEmpty(effect.TargetKingdomName, effect.TargetKingdomId, policy?.KingdomName, "未知国家"),
				"影响效果：" + (deltas.Count > 0 ? string.Join("  ·  ", deltas) : "无可显示的数值变化"),
				"持续时间：" + effect.DurationDays.ToString(CultureInfo.InvariantCulture) + " 天"
			};
			sections.Add(string.Join("\n", lines));
		}
		return string.Join("\n\n", sections);
	}

	private static void AddEffectDelta(List<string> parts, string label, float value, string unit)
	{
		if (parts == null || Math.Abs(value) < 0.0001f)
		{
			return;
		}
		string number = value > 0f
			? "+" + value.ToString("0.##", CultureInfo.InvariantCulture)
			: value.ToString("0.##", CultureInfo.InvariantCulture);
		parts.Add(label + " " + number + (unit ?? ""));
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
			parts.Add((IsPolicyEvent(entry) ? "发布者：" : (IsFeedbackEvent(entry) ? "关联统治者：" : "相关人物：")) + actor);
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
				.ThenBy(e => IsPolicyEvent(e) ? 0 : (IsFeedbackEvent(e) ? 1 : 2))
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

	private static bool IsPolicyEvent(AnimusForgeWorldEventInboxEntry entry)
	{
		return string.Equals((entry?.EventKind ?? "").Trim(), "npc_ruler_policy", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsFeedbackEvent(AnimusForgeWorldEventInboxEntry entry)
	{
		string kind = (entry?.EventKind ?? "").Trim();
		return string.Equals(kind, "npc_public_feedback", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(kind, "npc_ruler_policy_event", StringComparison.OrdinalIgnoreCase);
	}

	private static string GetEventKindLabel(AnimusForgeWorldEventInboxEntry entry)
	{
		if (IsPolicyEvent(entry))
		{
			return "统治者政策";
		}
		if (IsFeedbackEvent(entry))
		{
			return "政策衍生事件";
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
			NpcPublicFeedbackEventBehavior.MarkEventReadForExternal(selected.EventId);
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
