using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class AnimusForgeConversationHistoryLogVM : ViewModel
{
	// Keeping the Gauntlet tree at this bounded size prevents large linked histories from updating hundreds of RichText widgets every frame.
	private const int HistoryPageSize = 50;

	// Idle prewarming is deliberately short so opening and input handling always win over unseen-page formatting work.
	private const int CacheWarmEntriesPerTick = 3;

	private const long CacheWarmBudgetStopwatchTicksDivisor = 750;

	private readonly Action _onClose;

	private readonly Action<string> _onOpenEncyclopediaLink;

	private readonly List<AnimusForgeDialogueHistoryEntry> _historyEntries;

	private readonly Hero _conversationTargetHero;

	private readonly CharacterObject _conversationTargetCharacter;

	private readonly EncyclopediaEntityLinkFormatter.DisplaySession _linkDisplaySession;

	// Formatted strings are kept outside the UI list, allowing a revisited page to recreate only lightweight item VMs.
	private readonly Dictionary<int, CachedHistoryDisplayEntry> _formattedEntriesByIndex = new Dictionary<int, CachedHistoryDisplayEntry>();

	private MBBindingList<AnimusForgeConversationHistoryLogItemVM> _items;

	private string _titleText;

	private string _subtitleText;

	private string _pageStatusText;

	private bool _canLoadOlderPage;

	private bool _canLoadNewerPage;

	private int _autoScrollRequestVersion;

	private int _autoScrollTopRequestVersion;

	private int _currentPageIndex;

	private int _nextCacheWarmIndex;

	private bool _isFinalized;

	[DataSourceProperty]
	public MBBindingList<AnimusForgeConversationHistoryLogItemVM> Items
	{
		get => _items;
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, nameof(Items));
			}
		}
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
	public string PageStatusText
	{
		get => _pageStatusText;
		set
		{
			if (value != _pageStatusText)
			{
				_pageStatusText = value;
				OnPropertyChangedWithValue(value, nameof(PageStatusText));
			}
		}
	}

	[DataSourceProperty]
	public bool CanLoadOlderPage
	{
		get => _canLoadOlderPage;
		set
		{
			if (value != _canLoadOlderPage)
			{
				_canLoadOlderPage = value;
				OnPropertyChangedWithValue(value, nameof(CanLoadOlderPage));
			}
		}
	}

	[DataSourceProperty]
	public bool CanLoadNewerPage
	{
		get => _canLoadNewerPage;
		set
		{
			if (value != _canLoadNewerPage)
			{
				_canLoadNewerPage = value;
				OnPropertyChangedWithValue(value, nameof(CanLoadNewerPage));
			}
		}
	}

	[DataSourceProperty]
	public int AutoScrollRequestVersion
	{
		get => _autoScrollRequestVersion;
		set
		{
			if (value != _autoScrollRequestVersion)
			{
				_autoScrollRequestVersion = value;
				OnPropertyChangedWithValue(value, nameof(AutoScrollRequestVersion));
			}
		}
	}

	[DataSourceProperty]
	public int AutoScrollTopRequestVersion
	{
		get => _autoScrollTopRequestVersion;
		set
		{
			if (value != _autoScrollTopRequestVersion)
			{
				_autoScrollTopRequestVersion = value;
				OnPropertyChangedWithValue(value, nameof(AutoScrollTopRequestVersion));
			}
		}
	}

	public AnimusForgeConversationHistoryLogVM(string targetName, IReadOnlyList<AnimusForgeDialogueHistoryEntry> entries, Hero conversationTargetHero, CharacterObject conversationTargetCharacter, Action onClose, Action<string> onOpenEncyclopediaLink)
	{
		_onClose = onClose;
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		_conversationTargetHero = conversationTargetHero;
		_conversationTargetCharacter = conversationTargetCharacter;
		// Snapshot the small history list once so page changes never reread campaign-memory collections while the modal is visible.
		_historyEntries = CopyDisplayableEntries(entries);
		// A single catalog remains valid for this popup lifetime and prevents a repeated campaign-entity scan for every page.
		_linkDisplaySession = EncyclopediaEntityLinkFormatter.CreateDisplaySession();
		TitleText = "AnimusForge 对话历史";
		SubtitleText = string.IsNullOrWhiteSpace(targetName) ? "当前对话对象" : targetName.Trim();
		Items = new MBBindingList<AnimusForgeConversationHistoryLogItemVM>();
		// Open at the newest page, matching the former panel's automatic scroll-to-bottom behavior.
		_currentPageIndex = Math.Max(0, GetPageCount() - 1);
		// The newest page can be short, so prewarm from its actual first index instead of assuming a full final page.
		_nextCacheWarmIndex = Math.Max(-1, _currentPageIndex * HistoryPageSize - 1);
		RefreshCurrentPage(requestBottomScroll: false, requestTopScroll: false);
	}

	// Gauntlet command: reveal the immediately older slice without retaining previous-page widgets in the live UI tree.
	public void LoadOlderPage()
	{
		if (_isFinalized || _currentPageIndex <= 0)
		{
			return;
		}
		_currentPageIndex--;
		RefreshCurrentPage(requestBottomScroll: true, requestTopScroll: false);
	}

	// Gauntlet command: return toward the newest slice; formatted text is reused from the popup-local cache when available.
	public void LoadNewerPage()
	{
		if (_isFinalized || _currentPageIndex >= GetPageCount() - 1)
		{
			return;
		}
		_currentPageIndex++;
		// Moving forward should reveal the chronological boundary at the top of the newer page.
		RefreshCurrentPage(requestBottomScroll: false, requestTopScroll: true);
	}

	// The popup calls this at most once per UI tick; unseen pages are formatted gradually but never create Gauntlet widgets.
	internal void WarmDisplayCache()
	{
		if (_isFinalized || _nextCacheWarmIndex < 0 || _historyEntries.Count == 0)
		{
			return;
		}
		long startTimestamp = Stopwatch.GetTimestamp();
		long budgetTicks = Math.Max(1L, Stopwatch.Frequency / CacheWarmBudgetStopwatchTicksDivisor);
		int warmedEntryCount = 0;
		while (_nextCacheWarmIndex >= 0 && warmedEntryCount < CacheWarmEntriesPerTick)
		{
			int entryIndex = _nextCacheWarmIndex--;
			GetOrCreateFormattedEntry(entryIndex);
			warmedEntryCount++;
			if (Stopwatch.GetTimestamp() - startTimestamp >= budgetTicks)
			{
				// One unusually long reply may consume the full allowance, but it cannot delay more rows in this frame.
				break;
			}
		}
	}

	// Closing the modal must release cached strings and Hero references instead of keeping an old conversation alive through the static popup slot.
	internal void CancelDeferredFormatting()
	{
		_isFinalized = true;
		_formattedEntriesByIndex.Clear();
		_historyEntries.Clear();
		_nextCacheWarmIndex = -1;
	}

	public void CloseEx()
	{
		_onClose?.Invoke();
	}

	private void RefreshCurrentPage(bool requestBottomScroll, bool requestTopScroll)
	{
		Items.Clear();
		int pageCount = GetPageCount();
		if (_historyEntries.Count == 0)
		{
			// The empty state deliberately remains a plain, non-link record and has no paging controls.
			Items.Add(new AnimusForgeConversationHistoryLogItemVM("", "AnimusForge", "(AnimusForge)当前对象还没有可显示的 AnimusForge 对话历史。", "#D6D6D6FF", _onOpenEncyclopediaLink));
			PageStatusText = "暂无记录";
			CanLoadOlderPage = false;
			CanLoadNewerPage = false;
			return;
		}

		int firstEntryIndex = _currentPageIndex * HistoryPageSize;
		int lastEntryIndexExclusive = Math.Min(firstEntryIndex + HistoryPageSize, _historyEntries.Count);
		for (int entryIndex = firstEntryIndex; entryIndex < lastEntryIndexExclusive; entryIndex++)
		{
			CachedHistoryDisplayEntry displayEntry = GetOrCreateFormattedEntry(entryIndex);
			Items.Add(new AnimusForgeConversationHistoryLogItemVM(displayEntry.GameDate, displayEntry.Speaker, displayEntry.FormattedText, displayEntry.FontColor, _onOpenEncyclopediaLink));
		}

		PageStatusText = "第 " + (_currentPageIndex + 1) + " / " + pageCount + " 页（" + _historyEntries.Count + " 条）";
		CanLoadOlderPage = _currentPageIndex > 0;
		CanLoadNewerPage = _currentPageIndex + 1 < pageCount;
		if (requestBottomScroll)
		{
			// The current page stays chronological; moving to its bottom preserves continuity with the page the player came from.
			AutoScrollRequestVersion++;
		}
		else if (requestTopScroll)
		{
			// Returning toward newer records starts at their first row, immediately adjacent to the older page boundary.
			AutoScrollTopRequestVersion++;
		}
	}

	private CachedHistoryDisplayEntry GetOrCreateFormattedEntry(int entryIndex)
	{
		if (_formattedEntriesByIndex.TryGetValue(entryIndex, out CachedHistoryDisplayEntry cachedEntry))
		{
			return cachedEntry;
		}
		AnimusForgeDialogueHistoryEntry entry = _historyEntries[entryIndex];
		string speaker = string.IsNullOrWhiteSpace(entry.Speaker) ? "记录" : entry.Speaker.Trim();
		// Only the disposable UI copy contains native RichText links; persisted dialogue stays untouched for memory and LLM reuse.
		string rawDisplayText = "(" + speaker + ")" + (entry.Text ?? string.Empty);
		string formattedText;
		try
		{
			formattedText = _linkDisplaySession?.Format(rawDisplayText, _conversationTargetHero, _conversationTargetCharacter)
				?? EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawDisplayText);
		}
		catch
		{
			// A transient encyclopedia entity must leave this one row as safe plain text, never interrupt page navigation or idle prewarming.
			formattedText = EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawDisplayText);
		}
		cachedEntry = new CachedHistoryDisplayEntry(
			entry.GameDate ?? string.Empty,
			speaker,
			formattedText,
			AnimusForgeConversationHistoryLogItemVM.ResolveFontColor(entry.Kind));
		_formattedEntriesByIndex.Add(entryIndex, cachedEntry);
		return cachedEntry;
	}

	private int GetPageCount()
	{
		return _historyEntries.Count == 0 ? 0 : (_historyEntries.Count + HistoryPageSize - 1) / HistoryPageSize;
	}

	private static List<AnimusForgeDialogueHistoryEntry> CopyDisplayableEntries(IReadOnlyList<AnimusForgeDialogueHistoryEntry> entries)
	{
		List<AnimusForgeDialogueHistoryEntry> copiedEntries = new List<AnimusForgeDialogueHistoryEntry>();
		if (entries == null)
		{
			return copiedEntries;
		}
		for (int index = 0; index < entries.Count; index++)
		{
			AnimusForgeDialogueHistoryEntry entry = entries[index];
			if (entry != null)
			{
				copiedEntries.Add(entry);
			}
		}
		return copiedEntries;
	}

	// Immutable display data avoids retaining Gauntlet item VMs and their disconnected bindings for pages outside the viewport.
	private sealed class CachedHistoryDisplayEntry
	{
		public readonly string GameDate;

		public readonly string Speaker;

		public readonly string FormattedText;

		public readonly string FontColor;

		public CachedHistoryDisplayEntry(string gameDate, string speaker, string formattedText, string fontColor)
		{
			GameDate = gameDate;
			Speaker = speaker;
			FormattedText = formattedText;
			FontColor = fontColor;
		}
	}
}
