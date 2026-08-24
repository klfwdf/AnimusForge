using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

/// <summary>
/// Chronological projection of the three persistent world-message sources.
/// It is built only when the player opens or explicitly refreshes the UI; no per-frame source scan is used.
/// A short weekly report may delegate full-text generation to the pre-existing weekly-report workflow.
/// </summary>
public static class WorldMessageTimelineUi
{
	public const string AllCategoryId = "all";
	public const string DiplomacyCategoryId = "diplomacy";
	public const string PolicyCategoryId = "policy";
	public const string WeeklyCategoryId = "weekly";
	public const string AllCountriesId = "all";
	public const string WorldWeeklyCountryId = "world_weekly";

	private const string UnknownCountryId = "unknown";
	private const int MaxPolicySourceEntries = 480;
	private const int MaxWeeklySourceEntries = 360;
	private const int MaxDiplomacySourceEntries = 360;
	private const int MaxTimelineEntries = 600;
	private const int DetailCharacterLimit = 12000;
	private const int DetailLineLimit = 320;

	public static bool Show(Action onClose = null)
	{
		try
		{
			if (Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
			{
				return false;
			}
			return WorldMessageTimelinePopup.Show(BuildData(), onClose);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "timeline-open-failed", ex.Message, ex.ToString());
			return false;
		}
	}

	public static void OnApplicationTick()
	{
		if (!WorldMessageTimelinePopup.IsOpen)
		{
			return;
		}
		try
		{
			WorldMessageTimelinePopup.OnApplicationTick();
			if (Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
			{
				WorldMessageTimelinePopup.CloseActive(silent: true);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "timeline-tick-failed", ex.Message, ex.ToString());
		}
	}

	internal static WorldMessageTimelinePopupData BuildData()
	{
		List<WorldMessageTimelineEntryData> entries = new List<WorldMessageTimelineEntryData>();
		AppendPolicyEntries(entries);
		AppendWeeklyEntries(entries);
		AppendDiplomacyEntries(entries);

		List<WorldMessageTimelineEntryData> ordered = entries
			.Where(x => x != null)
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.ThenByDescending(x => x.Sequence)
			.ThenBy(x => x.EntryId ?? "", StringComparer.Ordinal)
			.Take(MaxTimelineEntries)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ThenBy(x => x.Sequence)
			.ThenBy(x => x.EntryId ?? "", StringComparer.Ordinal)
			.ToList();

		WorldMessageTimelinePopupData data = new WorldMessageTimelinePopupData
		{
			TitleText = "传闻",
			SubtitleText = "外交、政策与周报按时间正序汇总，最新消息位于底部；可叠加类别和国家筛选。当前显示最近 " + MaxTimelineEntries.ToString(CultureInfo.InvariantCulture) + " 条可用记录。",
			EmptyStateText = "暂无可查看的外交消息、政策或周报。",
			CloseText = "关闭",
			Entries = ordered,
			Countries = BuildKnownCountries(ordered)
		};
		return data;
	}

	private static void AppendPolicyEntries(List<WorldMessageTimelineEntryData> target)
	{
		if (target == null)
		{
			return;
		}
		try
		{
			long currentSequence = WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryCurrentSequence();
			long afterSequence = Math.Max(0L, currentSequence - MaxPolicySourceEntries);
			IReadOnlyList<PublishedPolicyArtifactLedgerEntry> source = WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryArtifacts(afterSequence, MaxPolicySourceEntries);
			foreach (PublishedPolicyArtifactLedgerEntry artifact in source ?? Array.Empty<PublishedPolicyArtifactLedgerEntry>())
			{
				if (artifact == null)
				{
					continue;
				}
				bool isPublished = string.Equals(artifact.EventKind, "policy_published", StringComparison.OrdinalIgnoreCase);
				string policyName = FirstNonEmpty(artifact.PolicyName, "未命名政策");
				string kindLabel = isPublished ? "政策发布" : "政策动态";
				WorldMessageTimelineEntryData entry = new WorldMessageTimelineEntryData
				{
					EntryId = "policy:" + FirstNonEmpty(artifact.PolicyId, artifact.Sequence.ToString(CultureInfo.InvariantCulture)) + ":" + artifact.Sequence.ToString(CultureInfo.InvariantCulture),
					CategoryId = PolicyCategoryId,
					CategoryLabel = kindLabel,
					TitleText = isPublished ? "《" + policyName + "》" : "《" + policyName + "》·政策动态",
					DateText = FirstNonEmpty(artifact.GameDate, FormatDay(artifact.OccurredDay)),
					MetaText = BuildPolicyMetaText(artifact, kindLabel),
					BodySectionTitleText = isPublished ? "政策公告" : "政策动态",
					BodyText = LimitMultiline(artifact.PublishedText, DetailCharacterLimit, DetailLineLimit, "（无政策详情）"),
					ImpactSectionTitleText = "",
					ImpactText = "",
					Day = Math.Max(0, artifact.OccurredDay),
					CreatedUtcTicks = artifact.CreatedUtcTicks,
					Sequence = artifact.Sequence,
					IsUnread = false,
					CanMarkRead = false
				};
				AddCountry(entry, artifact.KingdomId, artifact.KingdomName);
				target.Add(entry);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "policy-source-failed", ex.Message, ex.ToString());
		}
	}

	private static void AppendWeeklyEntries(List<WorldMessageTimelineEntryData> target)
	{
		if (target == null)
		{
			return;
		}
		try
		{
			List<WorldMessageTimelineEntryData> weeklyEntries = new List<WorldMessageTimelineEntryData>();
			IEnumerable<MyBehavior.WeeklyReportBrowserCountryData> countries = MyBehavior.Instance?.GetTerminalWeeklyReportBrowserCountries()
				?? Enumerable.Empty<MyBehavior.WeeklyReportBrowserCountryData>();
			foreach (MyBehavior.WeeklyReportBrowserCountryData country in countries)
			{
				if (country == null)
				{
					continue;
				}
				bool isWorld = country.IsWorld;
				string countryId = isWorld ? WorldWeeklyCountryId : FirstNonEmpty(country.CountryId, UnknownCountryId);
				string countryName = isWorld ? "世界周报" : FirstNonEmpty(country.DisplayName, country.CountryId, "未知国家");
				foreach (MyBehavior.WeeklyReportBrowserEntryData report in country.Reports ?? new List<MyBehavior.WeeklyReportBrowserEntryData>())
				{
					if (report == null)
					{
						continue;
					}
					int day = Math.Max(0, report.CreatedDay);
					if (day == 0 && report.WeekIndex > 0)
					{
						day = report.WeekIndex * 7;
					}
					string reportId = FirstNonEmpty(report.EventId,
						countryId + ":week:" + Math.Max(0, report.WeekIndex).ToString(CultureInfo.InvariantCulture) + ":day:" + day.ToString(CultureInfo.InvariantCulture));
					string tagText = (report.TagText ?? "").Trim();
					WorldMessageTimelineEntryData entry = new WorldMessageTimelineEntryData
					{
						EntryId = "weekly:" + reportId,
						CategoryId = WeeklyCategoryId,
						CategoryLabel = isWorld ? "世界周报" : "王国周报",
						TitleText = FirstNonEmpty(report.Title, countryName + "周报"),
						DateText = FirstNonEmpty(report.CreatedDate, FormatDay(day)),
						MetaText = FirstNonEmpty(report.CreatedDate, FormatDay(day)) + "  ·  " + (isWorld ? "世界周报" : "王国周报") + "  ·  " + countryName,
						BodySectionTitleText = "周报正文",
						BodyText = LimitMultiline(report.BodyText, DetailCharacterLimit, DetailLineLimit, "（本期周报尚无正文）"),
						ImpactSectionTitleText = string.IsNullOrWhiteSpace(tagText) ? "" : "周报标签",
						ImpactText = tagText,
						Day = day,
						CreatedUtcTicks = 0L,
					Sequence = Math.Max(0, report.WeekIndex),
					IsUnread = false,
					CanMarkRead = false,
					CanGenerateFullWeeklyReport = !report.HasFullReport && !string.IsNullOrWhiteSpace(report.EventId),
					WeeklyReportEventId = (report.EventId ?? "").Trim()
				};
					AddCountry(entry, countryId, countryName, isWorld);
					weeklyEntries.Add(entry);
				}
			}
			target.AddRange(weeklyEntries
				.OrderByDescending(x => x.Day)
				.ThenByDescending(x => x.Sequence)
				.ThenBy(x => x.EntryId ?? "", StringComparer.Ordinal)
				.Take(MaxWeeklySourceEntries));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "weekly-source-failed", ex.Message, ex.ToString());
		}
	}

	private static void AppendDiplomacyEntries(List<WorldMessageTimelineEntryData> target)
	{
		if (target == null)
		{
			return;
		}
		try
		{
			IEnumerable<WorldDiplomacyDocument> documents = WorldDiplomacyBehavior.GetRecentDocumentsForExternal(MaxDiplomacySourceEntries)
				.Where(x => x != null && (x.IsPlayerAuthored || x.IsReadyForPublication));
			foreach (WorldDiplomacyDocument document in documents)
			{
				string authorName = FirstNonEmpty(document.AuthorKingdomName, document.AuthorKingdomId, "未知国家");
				string targetName = BuildDiplomacyTargetText(document);
				string label = BuildDiplomacyLabel(document);
				string diplomacyImpact = WorldDiplomacyBehavior.BuildDiplomaticStandingImpactTextForExternal(document);
				WorldMessageTimelineEntryData entry = new WorldMessageTimelineEntryData
				{
					EntryId = "diplomacy:" + FirstNonEmpty(document.DocumentId, document.CreatedUtcTicks.ToString(CultureInfo.InvariantCulture)),
					CategoryId = DiplomacyCategoryId,
					CategoryLabel = label,
					TitleText = FirstNonEmpty(document.Title, label),
					DateText = FirstNonEmpty(document.GameDate, FormatDay(document.Day)),
					MetaText = BuildDiplomacyMetaText(document, label, authorName, targetName),
					BodySectionTitleText = "外交公文",
					BodyText = LimitMultiline(document.Body, DetailCharacterLimit, DetailLineLimit, "（该外交消息正文已经整理入外交编年档案。）"),
					ImpactSectionTitleText = "外交结果与外交影响",
					ImpactText = diplomacyImpact,
					Day = Math.Max(0, document.Day),
					CreatedUtcTicks = document.CreatedUtcTicks,
					Sequence = 0L,
					IsUnread = !document.IsRead,
					CanMarkRead = true,
					ReadKind = DiplomacyCategoryId,
					ReadSourceId = document.DocumentId ?? ""
				};
				AddCountry(entry, document.AuthorKingdomId, authorName);
				AddCountry(entry, document.TargetKingdomId, document.TargetKingdomName);
				foreach (WorldDiplomacyDocumentAction action in document.Actions ?? new List<WorldDiplomacyDocumentAction>())
				{
					if (action != null)
					{
						AddCountry(entry, action.TargetKingdomId, action.TargetKingdomName);
					}
				}
				target.Add(entry);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "diplomacy-source-failed", ex.Message, ex.ToString());
		}
	}

	private static List<WorldMessageTimelineCountryData> BuildKnownCountries(IEnumerable<WorldMessageTimelineEntryData> entries)
	{
		Dictionary<string, WorldMessageTimelineCountryData> byId = new Dictionary<string, WorldMessageTimelineCountryData>(StringComparer.OrdinalIgnoreCase)
		{
			[WorldWeeklyCountryId] = new WorldMessageTimelineCountryData
			{
				CountryId = WorldWeeklyCountryId,
				CountryName = "世界周报",
				IsWorldWeekly = true
			}
		};
		foreach (WorldMessageTimelineEntryData entry in entries ?? Enumerable.Empty<WorldMessageTimelineEntryData>())
		{
			foreach (WorldMessageTimelineCountryReference country in entry?.Countries ?? new List<WorldMessageTimelineCountryReference>())
			{
				if (country == null)
				{
					continue;
				}
				string id = FirstNonEmpty(country.CountryId, UnknownCountryId);
				string name = FirstNonEmpty(country.CountryName, id == WorldWeeklyCountryId ? "世界周报" : "未知国家");
				if (!byId.TryGetValue(id, out WorldMessageTimelineCountryData known))
				{
					byId[id] = new WorldMessageTimelineCountryData
					{
						CountryId = id,
						CountryName = name,
						IsWorldWeekly = country.IsWorldWeekly || string.Equals(id, WorldWeeklyCountryId, StringComparison.OrdinalIgnoreCase)
					};
				}
				else if (string.IsNullOrWhiteSpace(known.CountryName) || string.Equals(known.CountryName, known.CountryId, StringComparison.OrdinalIgnoreCase))
				{
					known.CountryName = name;
				}
			}
		}
		return byId.Values
			.OrderBy(x => x.IsWorldWeekly ? 0 : 1)
			.ThenBy(x => x.CountryName ?? x.CountryId ?? "", StringComparer.CurrentCulture)
			.ToList();
	}

	private static void AddCountry(WorldMessageTimelineEntryData entry, string countryId, string countryName, bool isWorldWeekly = false)
	{
		if (entry == null)
		{
			return;
		}
		string id = isWorldWeekly ? WorldWeeklyCountryId : FirstNonEmpty(countryId, UnknownCountryId);
		string name = isWorldWeekly ? "世界周报" : FirstNonEmpty(countryName, countryId, "未知国家");
		if (entry.Countries.Any(x => x != null && string.Equals(x.CountryId, id, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		entry.Countries.Add(new WorldMessageTimelineCountryReference
		{
			CountryId = id,
			CountryName = name,
			IsWorldWeekly = isWorldWeekly
		});
	}

	private static string BuildPolicyMetaText(PublishedPolicyArtifactLedgerEntry artifact, string kindLabel)
	{
		List<string> parts = new List<string>
		{
			FirstNonEmpty(artifact?.GameDate, FormatDay(artifact?.OccurredDay ?? 0)),
			kindLabel,
			PolicyScopeLabel(artifact?.ScopeKind)
		};
		string kingdom = FirstNonEmpty(artifact?.KingdomName, artifact?.KingdomId);
		if (!string.IsNullOrWhiteSpace(kingdom))
		{
			parts.Add(kingdom);
		}
		return string.Join("  ·  ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
	}

	private static string PolicyScopeLabel(string scopeKind)
	{
		if (string.Equals(scopeKind, "kingdom", StringComparison.OrdinalIgnoreCase)) return "王国政策";
		if (string.Equals(scopeKind, "local", StringComparison.OrdinalIgnoreCase)) return "地方政策";
		if (string.Equals(scopeKind, "vassal", StringComparison.OrdinalIgnoreCase)) return "附庸政策";
		return "政策";
	}

	private static string BuildDiplomacyLabel(WorldDiplomacyDocument document)
	{
		if (document?.IsResponse == true) return "外交回应";
		if (document?.RequiresResponse == true) return "外交照会";
		if (document?.ChangedDiplomaticState == true) return "外交结果";
		return "外交消息";
	}

	private static string BuildDiplomacyTargetText(WorldDiplomacyDocument document)
	{
		List<string> names = new List<string>();
		string directTarget = FirstNonEmpty(document?.TargetKingdomName, document?.TargetKingdomId);
		if (!string.IsNullOrWhiteSpace(directTarget))
		{
			names.Add(directTarget);
		}
		foreach (WorldDiplomacyDocumentAction action in document?.Actions ?? new List<WorldDiplomacyDocumentAction>())
		{
			string actionTarget = FirstNonEmpty(action?.TargetKingdomName, action?.TargetKingdomId);
			if (!string.IsNullOrWhiteSpace(actionTarget) && !names.Contains(actionTarget, StringComparer.OrdinalIgnoreCase))
			{
				names.Add(actionTarget);
			}
		}
		return string.Join("、", names.Take(4));
	}

	private static string BuildDiplomacyMetaText(WorldDiplomacyDocument document, string label, string authorName, string targetName)
	{
		string date = FirstNonEmpty(document?.GameDate, FormatDay(document?.Day ?? 0));
		return date + "  ·  " + label + "  ·  " + FirstNonEmpty(authorName, "未知国家")
			+ (string.IsNullOrWhiteSpace(targetName) ? "" : " → " + targetName);
	}

	private static string FormatDay(int day)
	{
		return day > 0 ? "第" + day.ToString(CultureInfo.InvariantCulture) + "天" : "未知日期";
	}

	private static string LimitMultiline(string text, int maxCharacters, int maxLines, string fallback)
	{
		string normalized = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return fallback ?? "";
		}
		bool truncated = false;
		string[] lines = normalized.Split('\n');
		if (maxLines > 0 && lines.Length > maxLines)
		{
			normalized = string.Join("\n", lines.Take(maxLines));
			truncated = true;
		}
		if (maxCharacters > 0 && normalized.Length > maxCharacters)
		{
			normalized = normalized.Substring(0, Math.Max(1, maxCharacters - 1)).TrimEnd();
			truncated = true;
		}
		return truncated ? normalized.TrimEnd('…') + "…" : normalized;
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
}

public sealed class WorldMessageTimelinePopup
{
	private static WorldMessageTimelinePopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly WorldMessageTimelinePopupVM _dataSource;
	private readonly Action _onClose;
	private bool _isClosed;

	private WorldMessageTimelinePopup(ScreenBase screen, WorldMessageTimelinePopupData data, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new WorldMessageTimelinePopupVM(data, HandleCloseRequested, HandleOpenEncyclopediaLink);
		_layer = new GauntletLayer("AnimusForgeWorldMessageTimelinePopup", 4101, false);
	}

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	public static bool Show(WorldMessageTimelinePopupData data, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			WorldMessageTimelinePopup popup = new WorldMessageTimelinePopup(topScreen, data ?? new WorldMessageTimelinePopupData(), onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "timeline-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void OnApplicationTick()
	{
		WorldMessageTimelinePopup popup = _activePopup;
		if (popup != null && !popup._isClosed && popup.ShouldCloseForEscapeKey())
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
		_layer.LoadMovie("AnimusForgeWorldMessageTimelinePopup", _dataSource);
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

	private void HandleOpenEncyclopediaLink(string link)
	{
		if (!_isClosed)
		{
			EncyclopediaEntityLinkNavigationCoordinator.Request(link, CloseForEncyclopediaNavigation);
		}
	}

	private void CloseForEncyclopediaNavigation()
	{
		// The timeline is dismissed without firing its regular close callback before encyclopedia navigation.
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
				PolicySystemLog.Failure("WorldMessage", "timeline-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class WorldMessageTimelinePopupVM : ViewModel
{
	private readonly Action _onClose;
	private readonly Action<string> _onOpenEncyclopediaLink;
	private readonly EncyclopediaEntityLinkFormatter.DisplaySession _linkDisplaySession;
	private List<WorldMessageTimelineEntryData> _allEntries;
	private List<WorldMessageTimelineCountryData> _knownCountries;
	// Empty country selection represents the explicit "全部国家" state. Keeping the
	// selected ids in sets makes every filter update a bounded in-memory operation.
	private readonly HashSet<string> _selectedCategoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _selectedCountryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private string _titleText;
	private string _subtitleText;
	private string _emptyStateText;
	private string _closeText;
	private string _timelineTitleText;
	private string _selectedRecordTitleText;
	private string _selectedRecordCategoryText;
	private string _selectedRecordMetaText;
	private string _selectedRecordBodySectionTitleText;
	private string _selectedRecordBodyText;
	private string _selectedRecordImpactSectionTitleText;
	private string _selectedRecordImpactText;
	private bool _hasMessages;
	private bool _showEmptyState;
	private bool _hasTimelineRecords;
	private bool _showFilteredEmptyState;
	private bool _hasSelectedRecord;
	private bool _hasSelectedRecordImpact;
	private bool _showGenerateFullWeeklyReport;
	private bool _canGenerateFullWeeklyReport;
	private bool _isGeneratingFullWeeklyReport;
	private bool _isAllCategorySelected;
	private bool _isDiplomacyCategorySelected;
	private bool _isPolicyCategorySelected;
	private bool _isWeeklyCategorySelected;
	private MBBindingList<WorldMessageTimelineCountryItemVM> _countryItems;
	private MBBindingList<WorldMessageTimelineRecordItemVM> _recordItems;
	private string _generateFullWeeklyReportText;
	private string _selectedRecordEntryId;
	private string _selectedWeeklyReportEventId;

	public WorldMessageTimelinePopupVM(WorldMessageTimelinePopupData data, Action onClose, Action<string> onOpenEncyclopediaLink)
	{
		_onClose = onClose;
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		// This catalog is built once when the rumor timeline opens; records are linked lazily only after selection.
		_linkDisplaySession = EncyclopediaEntityLinkFormatter.CreateDisplaySession();
		WorldMessageTimelinePopupData source = data ?? new WorldMessageTimelinePopupData();
		TitleText = FirstNonEmpty(source.TitleText, "传闻");
		SubtitleText = FirstNonEmpty(source.SubtitleText, "外交、政策与周报按时间正序汇总，最新消息位于底部。");
		EmptyStateText = FirstNonEmpty(source.EmptyStateText, "暂无传闻。");
		CloseText = FirstNonEmpty(source.CloseText, "关闭");
		GenerateFullWeeklyReportText = "生成完整周报";
		ReplaceSourceData(source);
		CountryItems = new MBBindingList<WorldMessageTimelineCountryItemVM>();
		RecordItems = new MBBindingList<WorldMessageTimelineRecordItemVM>();
		SelectAllCategories();
		RefreshCategorySelectionState();
		RebuildCountriesAndTimeline();
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
	public string TimelineTitleText { get => _timelineTitleText; set { if (value != _timelineTitleText) { _timelineTitleText = value; OnPropertyChangedWithValue(value, nameof(TimelineTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordTitleText { get => _selectedRecordTitleText; set { if (value != _selectedRecordTitleText) { _selectedRecordTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordCategoryText { get => _selectedRecordCategoryText; set { if (value != _selectedRecordCategoryText) { _selectedRecordCategoryText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordCategoryText)); } } }
	[DataSourceProperty]
	public string SelectedRecordMetaText { get => _selectedRecordMetaText; set { if (value != _selectedRecordMetaText) { _selectedRecordMetaText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordMetaText)); } } }
	[DataSourceProperty]
	public string SelectedRecordBodySectionTitleText { get => _selectedRecordBodySectionTitleText; set { if (value != _selectedRecordBodySectionTitleText) { _selectedRecordBodySectionTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordBodySectionTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordBodyText { get => _selectedRecordBodyText; set { if (value != _selectedRecordBodyText) { _selectedRecordBodyText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordBodyText)); } } }
	[DataSourceProperty]
	public string SelectedRecordImpactSectionTitleText { get => _selectedRecordImpactSectionTitleText; set { if (value != _selectedRecordImpactSectionTitleText) { _selectedRecordImpactSectionTitleText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordImpactSectionTitleText)); } } }
	[DataSourceProperty]
	public string SelectedRecordImpactText { get => _selectedRecordImpactText; set { if (value != _selectedRecordImpactText) { _selectedRecordImpactText = value; OnPropertyChangedWithValue(value, nameof(SelectedRecordImpactText)); } } }
	[DataSourceProperty]
	public bool HasMessages { get => _hasMessages; set { if (value != _hasMessages) { _hasMessages = value; OnPropertyChangedWithValue(value, nameof(HasMessages)); } } }
	[DataSourceProperty]
	public bool ShowEmptyState { get => _showEmptyState; set { if (value != _showEmptyState) { _showEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowEmptyState)); } } }
	[DataSourceProperty]
	public bool HasTimelineRecords { get => _hasTimelineRecords; set { if (value != _hasTimelineRecords) { _hasTimelineRecords = value; OnPropertyChangedWithValue(value, nameof(HasTimelineRecords)); } } }
	[DataSourceProperty]
	public bool ShowFilteredEmptyState { get => _showFilteredEmptyState; set { if (value != _showFilteredEmptyState) { _showFilteredEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowFilteredEmptyState)); } } }
	[DataSourceProperty]
	public bool HasSelectedRecord { get => _hasSelectedRecord; set { if (value != _hasSelectedRecord) { _hasSelectedRecord = value; OnPropertyChangedWithValue(value, nameof(HasSelectedRecord)); } } }
	[DataSourceProperty]
	public bool HasSelectedRecordImpact { get => _hasSelectedRecordImpact; set { if (value != _hasSelectedRecordImpact) { _hasSelectedRecordImpact = value; OnPropertyChangedWithValue(value, nameof(HasSelectedRecordImpact)); } } }
	[DataSourceProperty]
	public bool ShowGenerateFullWeeklyReport { get => _showGenerateFullWeeklyReport; set { if (value != _showGenerateFullWeeklyReport) { _showGenerateFullWeeklyReport = value; OnPropertyChangedWithValue(value, nameof(ShowGenerateFullWeeklyReport)); } } }
	[DataSourceProperty]
	public bool CanGenerateFullWeeklyReport { get => _canGenerateFullWeeklyReport; set { if (value != _canGenerateFullWeeklyReport) { _canGenerateFullWeeklyReport = value; OnPropertyChangedWithValue(value, nameof(CanGenerateFullWeeklyReport)); } } }
	[DataSourceProperty]
	public string GenerateFullWeeklyReportText { get => _generateFullWeeklyReportText; set { if (value != _generateFullWeeklyReportText) { _generateFullWeeklyReportText = value; OnPropertyChangedWithValue(value, nameof(GenerateFullWeeklyReportText)); } } }
	[DataSourceProperty]
	public bool IsAllCategorySelected { get => _isAllCategorySelected; set { if (value != _isAllCategorySelected) { _isAllCategorySelected = value; OnPropertyChangedWithValue(value, nameof(IsAllCategorySelected)); } } }
	[DataSourceProperty]
	public bool IsDiplomacyCategorySelected { get => _isDiplomacyCategorySelected; set { if (value != _isDiplomacyCategorySelected) { _isDiplomacyCategorySelected = value; OnPropertyChangedWithValue(value, nameof(IsDiplomacyCategorySelected)); } } }
	[DataSourceProperty]
	public bool IsPolicyCategorySelected { get => _isPolicyCategorySelected; set { if (value != _isPolicyCategorySelected) { _isPolicyCategorySelected = value; OnPropertyChangedWithValue(value, nameof(IsPolicyCategorySelected)); } } }
	[DataSourceProperty]
	public bool IsWeeklyCategorySelected { get => _isWeeklyCategorySelected; set { if (value != _isWeeklyCategorySelected) { _isWeeklyCategorySelected = value; OnPropertyChangedWithValue(value, nameof(IsWeeklyCategorySelected)); } } }
	[DataSourceProperty]
	public string AllFilterText => BuildCategoryFilterText("全部", WorldMessageTimelineUi.AllCategoryId);
	[DataSourceProperty]
	public string DiplomacyFilterText => BuildCategoryFilterText("外交", WorldMessageTimelineUi.DiplomacyCategoryId);
	[DataSourceProperty]
	public string PolicyFilterText => BuildCategoryFilterText("政策", WorldMessageTimelineUi.PolicyCategoryId);
	[DataSourceProperty]
	public string WeeklyFilterText => BuildCategoryFilterText("周报", WorldMessageTimelineUi.WeeklyCategoryId);
	[DataSourceProperty]
	public MBBindingList<WorldMessageTimelineCountryItemVM> CountryItems { get => _countryItems; set { if (value != _countryItems) { _countryItems = value; OnPropertyChangedWithValue(value, nameof(CountryItems)); } } }
	[DataSourceProperty]
	public MBBindingList<WorldMessageTimelineRecordItemVM> RecordItems { get => _recordItems; set { if (value != _recordItems) { _recordItems = value; OnPropertyChangedWithValue(value, nameof(RecordItems)); } } }

	public void ExecuteSelectAll() => ToggleCategory(WorldMessageTimelineUi.AllCategoryId);
	public void ExecuteSelectDiplomacy() => ToggleCategory(WorldMessageTimelineUi.DiplomacyCategoryId);
	public void ExecuteSelectPolicy() => ToggleCategory(WorldMessageTimelineUi.PolicyCategoryId);
	public void ExecuteSelectWeekly() => ToggleCategory(WorldMessageTimelineUi.WeeklyCategoryId);
	public void ExecuteGenerateFullWeeklyReport() => GenerateFullWeeklyReportAsync(_selectedWeeklyReportEventId, _selectedRecordEntryId);
	public void ExecuteClose() => _onClose?.Invoke();
	public void ExecuteOpenEncyclopediaLink(string link) => _onOpenEncyclopediaLink?.Invoke(link);

	private async void GenerateFullWeeklyReportAsync(string eventId, string preferredRecordEntryId)
	{
		string sourceEventId = FirstNonEmpty(eventId);
		if (_isGeneratingFullWeeklyReport || !CanGenerateFullWeeklyReport || string.IsNullOrWhiteSpace(sourceEventId))
		{
			return;
		}
		MyBehavior behavior = MyBehavior.Instance;
		if (behavior == null)
		{
			return;
		}

		bool generated = false;
		_isGeneratingFullWeeklyReport = true;
		RefreshFullWeeklyReportActionState();
		try
		{
			// Reuse the existing on-demand generator, including its saved material,
			// progress inquiry, parsing and failure handling. No prompt is changed here.
			generated = await behavior.GenerateWeeklyReportFullByEventIdAsync(sourceEventId);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "weekly-full-generate-failed", ex.Message, ex.ToString());
		}
		finally
		{
			_isGeneratingFullWeeklyReport = false;
		}

		if (generated)
		{
			ReloadFromCurrentSources(preferredRecordEntryId);
		}
		else
		{
			RefreshFullWeeklyReportActionState();
		}
	}

	private void ReloadFromCurrentSources(string preferredRecordEntryId)
	{
		try
		{
			ReplaceSourceData(WorldMessageTimelineUi.BuildData());
			RebuildCountriesAndTimeline(preferredRecordEntryId);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("WorldMessage", "weekly-full-refresh-failed", ex.Message, ex.ToString());
			RefreshFullWeeklyReportActionState();
		}
	}

	private void ToggleCategory(string categoryId)
	{
		string selected = NormalizeCategoryId(categoryId);
		if (string.Equals(selected, WorldMessageTimelineUi.AllCategoryId, StringComparison.OrdinalIgnoreCase))
		{
			SelectAllCategories();
		}
		else if (HasAllCategoriesSelected())
		{
			_selectedCategoryIds.Clear();
			_selectedCategoryIds.Add(selected);
		}
		else if (!_selectedCategoryIds.Remove(selected))
		{
			_selectedCategoryIds.Add(selected);
		}
		else if (_selectedCategoryIds.Count == 0)
		{
			// A filter with no categories has no useful player-facing meaning.
			_selectedCategoryIds.Add(selected);
		}
		RefreshCategorySelectionState();
		RebuildCountriesAndTimeline();
	}

	private void RebuildCountriesAndTimeline(string preferredRecordEntryId = null)
	{
		List<WorldMessageTimelineEntryData> categoryEntries = GetCategoryEntries().ToList();
		CountryItems.Clear();
		CountryItems.Add(new WorldMessageTimelineCountryItemVM(
			WorldMessageTimelineUi.AllCountriesId,
			"全部国家",
			categoryEntries.Count,
			SelectCountry));

		foreach (WorldMessageTimelineCountryData country in _knownCountries
			.Where(IsCountryAvailableForCurrentCategory)
			.OrderBy(x => x.IsWorldWeekly ? 0 : 1)
			.ThenBy(x => x.CountryName ?? x.CountryId ?? "", StringComparer.CurrentCulture))
		{
			int count = categoryEntries.Count(entry => EntryMatchesCountry(entry, country.CountryId));
			CountryItems.Add(new WorldMessageTimelineCountryItemVM(
				country.CountryId,
				FirstNonEmpty(country.CountryName, "未知国家"),
				count,
				SelectCountry));
		}

		NormalizeSelectedCountries();
		RefreshCountrySelectionState();
		RebuildTimeline(preferredRecordEntryId);
	}

	private bool IsCountryAvailableForCurrentCategory(WorldMessageTimelineCountryData country)
	{
		if (country == null)
		{
			return false;
		}
		if (country.IsWorldWeekly || string.Equals(country.CountryId, WorldMessageTimelineUi.WorldWeeklyCountryId, StringComparison.OrdinalIgnoreCase))
		{
			return IsCategorySelected(WorldMessageTimelineUi.WeeklyCategoryId);
		}
		return GetCategoryEntries().Any(entry => EntryMatchesCountry(entry, country.CountryId));
	}

	private void SelectCountry(string countryId)
	{
		string selected = FirstNonEmpty(countryId, WorldMessageTimelineUi.AllCountriesId);
		if (string.Equals(selected, WorldMessageTimelineUi.AllCountriesId, StringComparison.OrdinalIgnoreCase))
		{
			_selectedCountryIds.Clear();
		}
		else if (CountryItems.Any(x => string.Equals(x.CountryId, selected, StringComparison.OrdinalIgnoreCase)))
		{
			if (IsAllCountriesSelected())
			{
				_selectedCountryIds.Add(selected);
			}
			else if (!_selectedCountryIds.Remove(selected))
			{
				_selectedCountryIds.Add(selected);
			}
			NormalizeSelectedCountries();
		}
		RefreshCountrySelectionState();
		RebuildTimeline();
	}

	private void RebuildTimeline(string preferredRecordEntryId = null)
	{
		// _allEntries is already in chronological order, and LINQ filtering preserves it.
		List<WorldMessageTimelineEntryData> records = GetCategoryEntries()
			.Where(EntryMatchesSelectedCountries)
			.ToList();
		RecordItems.Clear();
		for (int i = 0; i < records.Count; i++)
		{
			RecordItems.Add(new WorldMessageTimelineRecordItemVM(records[i], i, SelectRecord));
		}
		HasTimelineRecords = RecordItems.Count > 0;
		ShowFilteredEmptyState = HasMessages && !HasTimelineRecords;
		TimelineTitleText = BuildTimelineTitle(records.Count);
		if (RecordItems.Count > 0)
		{
			int selectedIndex = RecordItems.Count - 1;
			if (!string.IsNullOrWhiteSpace(preferredRecordEntryId))
			{
				for (int i = 0; i < RecordItems.Count; i++)
				{
					if (string.Equals(RecordItems[i].EntryId, preferredRecordEntryId, StringComparison.OrdinalIgnoreCase))
					{
						selectedIndex = i;
						break;
					}
				}
			}
			SelectRecord(selectedIndex);
		}
		else
		{
			ClearSelectedRecord();
		}
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
		WorldMessageTimelineRecordItemVM selected = RecordItems[index];
		if (selected.IsUnread && selected.CanMarkRead && string.Equals(selected.ReadKind, WorldMessageTimelineUi.DiplomacyCategoryId, StringComparison.OrdinalIgnoreCase))
		{
			if (WorldDiplomacyBehavior.MarkDocumentReadForExternal(selected.ReadSourceId))
			{
				selected.MarkRead();
			}
		}
		// Keep ledger entries plain and derive links only for the selected rumor detail pane.
		SelectedRecordTitleText = _linkDisplaySession.Format(selected.TitleText);
		SelectedRecordCategoryText = selected.CategoryLabel;
		SelectedRecordMetaText = _linkDisplaySession.Format(selected.MetaText);
		SelectedRecordBodySectionTitleText = selected.BodySectionTitleText;
		SelectedRecordBodyText = _linkDisplaySession.Format(selected.BodyText);
		SelectedRecordImpactSectionTitleText = selected.ImpactSectionTitleText;
		SelectedRecordImpactText = _linkDisplaySession.Format(selected.ImpactText);
		HasSelectedRecord = true;
		HasSelectedRecordImpact = selected.HasImpact;
		_selectedRecordEntryId = selected.EntryId ?? "";
		_selectedWeeklyReportEventId = selected.WeeklyReportEventId ?? "";
		RefreshFullWeeklyReportActionState(selected);
	}

	private void ClearSelectedRecord()
	{
		SelectedRecordTitleText = "";
		SelectedRecordCategoryText = "";
		SelectedRecordMetaText = "";
		SelectedRecordBodySectionTitleText = "";
		SelectedRecordBodyText = "";
		SelectedRecordImpactSectionTitleText = "";
		SelectedRecordImpactText = "";
		HasSelectedRecord = false;
		HasSelectedRecordImpact = false;
		_selectedRecordEntryId = "";
		_selectedWeeklyReportEventId = "";
		ShowGenerateFullWeeklyReport = false;
		CanGenerateFullWeeklyReport = false;
		GenerateFullWeeklyReportText = "生成完整周报";
	}

	private void ReplaceSourceData(WorldMessageTimelinePopupData source)
	{
		WorldMessageTimelinePopupData data = source ?? new WorldMessageTimelinePopupData();
		_allEntries = (data.Entries ?? new List<WorldMessageTimelineEntryData>())
			.Where(x => x != null)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ThenBy(x => x.Sequence)
			.ThenBy(x => x.EntryId ?? "", StringComparer.Ordinal)
			.ToList();
		_knownCountries = (data.Countries ?? new List<WorldMessageTimelineCountryData>())
			.Where(x => x != null)
			.ToList();
		HasMessages = _allEntries.Count > 0;
		ShowEmptyState = !HasMessages;
	}

	private void RefreshFullWeeklyReportActionState(WorldMessageTimelineRecordItemVM selected = null)
	{
		WorldMessageTimelineRecordItemVM current = selected
			?? RecordItems?.FirstOrDefault(x => x != null && x.IsSelected);
		bool canGenerate = current != null
			&& current.CanGenerateFullWeeklyReport
			&& !string.IsNullOrWhiteSpace(current.WeeklyReportEventId);
		if (current != null)
		{
			_selectedRecordEntryId = current.EntryId ?? "";
			_selectedWeeklyReportEventId = current.WeeklyReportEventId ?? "";
		}
		ShowGenerateFullWeeklyReport = canGenerate;
		CanGenerateFullWeeklyReport = canGenerate && !_isGeneratingFullWeeklyReport;
		GenerateFullWeeklyReportText = canGenerate && _isGeneratingFullWeeklyReport
			? "正在生成完整周报…"
			: "生成完整周报";
	}

	private IEnumerable<WorldMessageTimelineEntryData> GetCategoryEntries()
	{
		return HasAllCategoriesSelected()
			? _allEntries
			: _allEntries.Where(x => x != null && _selectedCategoryIds.Contains(x.CategoryId ?? ""));
	}

	private bool EntryMatchesSelectedCountries(WorldMessageTimelineEntryData entry)
	{
		if (IsAllCountriesSelected())
		{
			return true;
		}
		return entry?.Countries != null
			&& entry.Countries.Any(country => country != null && _selectedCountryIds.Contains(country.CountryId ?? ""));
	}

	private static bool EntryMatchesCountry(WorldMessageTimelineEntryData entry, string countryId)
	{
		if (string.Equals(countryId, WorldMessageTimelineUi.AllCountriesId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return entry?.Countries != null
			&& entry.Countries
			.Any(country => country != null && string.Equals(country.CountryId, countryId, StringComparison.OrdinalIgnoreCase));
	}

	private string BuildCategoryFilterText(string label, string categoryId)
	{
		int count = string.Equals(categoryId, WorldMessageTimelineUi.AllCategoryId, StringComparison.OrdinalIgnoreCase)
			? _allEntries.Count
			: _allEntries.Count(x => string.Equals(x.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));
		return label + "  " + count.ToString(CultureInfo.InvariantCulture);
	}

	private string BuildTimelineTitle(int recordCount)
	{
		return BuildCategorySelectionText() + " · " + BuildCountrySelectionText()
			+ "（" + recordCount.ToString(CultureInfo.InvariantCulture) + "）";
	}

	private string BuildCategorySelectionText()
	{
		if (HasAllCategoriesSelected())
		{
			return "全部消息";
		}
		List<string> labels = new List<string>(3);
		if (_selectedCategoryIds.Contains(WorldMessageTimelineUi.DiplomacyCategoryId)) labels.Add("外交");
		if (_selectedCategoryIds.Contains(WorldMessageTimelineUi.PolicyCategoryId)) labels.Add("政策");
		if (_selectedCategoryIds.Contains(WorldMessageTimelineUi.WeeklyCategoryId)) labels.Add("周报");
		return labels.Count == 0 ? "全部消息" : string.Join("、", labels);
	}

	private string BuildCountrySelectionText()
	{
		if (IsAllCountriesSelected())
		{
			return "全部国家";
		}
		List<string> names = CountryItems
			.Where(x => x != null
				&& !string.Equals(x.CountryId, WorldMessageTimelineUi.AllCountriesId, StringComparison.OrdinalIgnoreCase)
				&& _selectedCountryIds.Contains(x.CountryId ?? ""))
			.Select(x => x.CountryName)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();
		if (names.Count <= 2)
		{
			return string.Join("、", names);
		}
		return names[0] + "等" + names.Count.ToString(CultureInfo.InvariantCulture) + "项";
	}

	private static string NormalizeCategoryId(string categoryId)
	{
		if (string.Equals(categoryId, WorldMessageTimelineUi.DiplomacyCategoryId, StringComparison.OrdinalIgnoreCase)) return WorldMessageTimelineUi.DiplomacyCategoryId;
		if (string.Equals(categoryId, WorldMessageTimelineUi.PolicyCategoryId, StringComparison.OrdinalIgnoreCase)) return WorldMessageTimelineUi.PolicyCategoryId;
		if (string.Equals(categoryId, WorldMessageTimelineUi.WeeklyCategoryId, StringComparison.OrdinalIgnoreCase)) return WorldMessageTimelineUi.WeeklyCategoryId;
		return WorldMessageTimelineUi.AllCategoryId;
	}

	private void SelectAllCategories()
	{
		_selectedCategoryIds.Clear();
		_selectedCategoryIds.Add(WorldMessageTimelineUi.DiplomacyCategoryId);
		_selectedCategoryIds.Add(WorldMessageTimelineUi.PolicyCategoryId);
		_selectedCategoryIds.Add(WorldMessageTimelineUi.WeeklyCategoryId);
	}

	private bool HasAllCategoriesSelected()
	{
		return _selectedCategoryIds.Count == 3
			&& _selectedCategoryIds.Contains(WorldMessageTimelineUi.DiplomacyCategoryId)
			&& _selectedCategoryIds.Contains(WorldMessageTimelineUi.PolicyCategoryId)
			&& _selectedCategoryIds.Contains(WorldMessageTimelineUi.WeeklyCategoryId);
	}

	private bool IsCategorySelected(string categoryId)
	{
		return HasAllCategoriesSelected() || _selectedCategoryIds.Contains(categoryId ?? "");
	}

	private bool IsAllCountriesSelected()
	{
		return _selectedCountryIds.Count == 0;
	}

	private void NormalizeSelectedCountries()
	{
		HashSet<string> availableCountryIds = new HashSet<string>(
			CountryItems
				.Where(x => x != null && !string.Equals(x.CountryId, WorldMessageTimelineUi.AllCountriesId, StringComparison.OrdinalIgnoreCase))
				.Select(x => x.CountryId ?? "")
				.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		_selectedCountryIds.IntersectWith(availableCountryIds);
		if (availableCountryIds.Count > 0 && _selectedCountryIds.SetEquals(availableCountryIds))
		{
			_selectedCountryIds.Clear();
		}
	}

	private void RefreshCountrySelectionState()
	{
		bool allCountries = IsAllCountriesSelected();
		for (int i = 0; i < CountryItems.Count; i++)
		{
			WorldMessageTimelineCountryItemVM item = CountryItems[i];
			item.IsSelected = string.Equals(item.CountryId, WorldMessageTimelineUi.AllCountriesId, StringComparison.OrdinalIgnoreCase)
				? allCountries
				: !allCountries && _selectedCountryIds.Contains(item.CountryId ?? "");
		}
	}

	private void RefreshCategorySelectionState()
	{
		bool allCategories = HasAllCategoriesSelected();
		IsAllCategorySelected = allCategories;
		IsDiplomacyCategorySelected = !allCategories && _selectedCategoryIds.Contains(WorldMessageTimelineUi.DiplomacyCategoryId);
		IsPolicyCategorySelected = !allCategories && _selectedCategoryIds.Contains(WorldMessageTimelineUi.PolicyCategoryId);
		IsWeeklyCategorySelected = !allCategories && _selectedCategoryIds.Contains(WorldMessageTimelineUi.WeeklyCategoryId);
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
}

public sealed class WorldMessageTimelineCountryItemVM : ViewModel
{
	private readonly Action<string> _select;
	private bool _isSelected;

	public WorldMessageTimelineCountryItemVM(string countryId, string countryName, int recordCount, Action<string> select)
	{
		CountryId = countryId ?? "";
		CountryName = countryName ?? "未知国家";
		RecordCount = Math.Max(0, recordCount);
		_select = select;
	}

	public string CountryId { get; }
	[DataSourceProperty]
	public string CountryName { get; }
	[DataSourceProperty]
	public int RecordCount { get; }
	[DataSourceProperty]
	public string RecordCountText => RecordCount.ToString(CultureInfo.InvariantCulture);
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
		_select?.Invoke(CountryId);
	}
}

public sealed class WorldMessageTimelineRecordItemVM : ViewModel
{
	private readonly Action<int> _select;
	private bool _isSelected;
	private bool _isUnread;
	private string _unreadMarkerText;

	public WorldMessageTimelineRecordItemVM(WorldMessageTimelineEntryData source, int index, Action<int> select)
	{
		WorldMessageTimelineEntryData data = source ?? new WorldMessageTimelineEntryData();
		Index = index;
		_select = select;
		EntryId = data.EntryId ?? "";
		CategoryLabel = data.CategoryLabel ?? "传闻";
		TitleText = data.TitleText ?? "传闻";
		DateText = data.DateText ?? "";
		MetaText = data.MetaText ?? "";
		BodySectionTitleText = data.BodySectionTitleText ?? "详情";
		BodyText = data.BodyText ?? "";
		ImpactSectionTitleText = data.ImpactSectionTitleText ?? "";
		ImpactText = data.ImpactText ?? "";
		CanMarkRead = data.CanMarkRead;
		ReadKind = data.ReadKind ?? "";
		ReadSourceId = data.ReadSourceId ?? "";
		CanGenerateFullWeeklyReport = data.CanGenerateFullWeeklyReport && !string.IsNullOrWhiteSpace(data.WeeklyReportEventId);
		WeeklyReportEventId = data.WeeklyReportEventId ?? "";
		_isUnread = data.IsUnread;
		_unreadMarkerText = data.IsUnread ? "新" : "";
	}

	public int Index { get; }
	public string EntryId { get; }
	[DataSourceProperty]
	public string CategoryLabel { get; }
	[DataSourceProperty]
	public string TitleText { get; }
	[DataSourceProperty]
	public string DateText { get; }
	[DataSourceProperty]
	public string MetaText { get; }
	[DataSourceProperty]
	public string BodySectionTitleText { get; }
	[DataSourceProperty]
	public string BodyText { get; }
	[DataSourceProperty]
	public string ImpactSectionTitleText { get; }
	[DataSourceProperty]
	public string ImpactText { get; }
	public bool CanMarkRead { get; }
	public string ReadKind { get; }
	public string ReadSourceId { get; }
	public bool CanGenerateFullWeeklyReport { get; }
	public string WeeklyReportEventId { get; }
	[DataSourceProperty]
	public bool HasImpact => !string.IsNullOrWhiteSpace(ImpactText);
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
		IsUnread = false;
		UnreadMarkerText = "";
	}
}

public sealed class WorldMessageTimelinePopupData
{
	public string TitleText = "传闻";
	public string SubtitleText = "外交、政策与周报按时间正序汇总，最新消息位于底部。";
	public string EmptyStateText = "暂无传闻。";
	public string CloseText = "关闭";
	public List<WorldMessageTimelineEntryData> Entries = new List<WorldMessageTimelineEntryData>();
	public List<WorldMessageTimelineCountryData> Countries = new List<WorldMessageTimelineCountryData>();
}

public sealed class WorldMessageTimelineEntryData
{
	public string EntryId = "";
	public string CategoryId = "";
	public string CategoryLabel = "";
	public string TitleText = "";
	public string DateText = "";
	public string MetaText = "";
	public string BodySectionTitleText = "";
	public string BodyText = "";
	public string ImpactSectionTitleText = "";
	public string ImpactText = "";
	public int Day;
	public long CreatedUtcTicks;
	public long Sequence;
	public bool IsUnread;
	public bool CanMarkRead;
	public string ReadKind = "";
	public string ReadSourceId = "";
	public bool CanGenerateFullWeeklyReport;
	public string WeeklyReportEventId = "";
	public List<WorldMessageTimelineCountryReference> Countries = new List<WorldMessageTimelineCountryReference>();
}

public sealed class WorldMessageTimelineCountryReference
{
	public string CountryId = "";
	public string CountryName = "";
	public bool IsWorldWeekly;
}

public sealed class WorldMessageTimelineCountryData
{
	public string CountryId = "";
	public string CountryName = "";
	public bool IsWorldWeekly;
}
