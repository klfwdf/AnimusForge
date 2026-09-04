using System;
using System.Collections.Generic;
using System.Linq;
using AFWarStatsTerminal.Behaviors;
using AFWarStatsTerminal.Localization;
using AFWarStatsTerminal.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsPopupVM : ViewModel
{
    private enum TabKind
    {
        CurrentWars,
        HistoricalWars
    }

    private const int CurrentRowsPerPage = 6;

    private const int HistoryRowsPerPage = 8;

    private const float HistoryWearinessBarTotalWidth = 180f;

    private readonly Action _onClose;

    private TabKind _activeTab = TabKind.CurrentWars;

    private int _currentPageIndex;

    private int _historyPageIndex;

    private int _historyTotalCount;

    private string _selectedHistoryIdentity = string.Empty;

    private readonly HashSet<string> _selectedHistoryIdentities = new(StringComparer.Ordinal);

    private SelectorVM<SelectorItemVM> _currentSortSelector;

    private SelectorVM<SelectorItemVM> _historySortSelector;

    private Kingdom _historyKingdomA;

    private Kingdom _historyKingdomB;

    private string _titleText = string.Empty;

    private string _subtitleText = string.Empty;

    private string _currentTabText = string.Empty;

    private string _historyTabText = string.Empty;

    private string _clearAllText = string.Empty;

    private string _currentSortLabelText = string.Empty;

    private string _historySortLabelText = string.Empty;

    private string _selectAllText = string.Empty;

    private string _deleteSelectedText = string.Empty;

    private string _historySelectionCountText = string.Empty;

    private string _prevText = string.Empty;

    private string _nextText = string.Empty;

    private string _closeText = string.Empty;

    private string _versusText = string.Empty;

    private string _attackerHeaderText = string.Empty;

    private string _warHeaderText = string.Empty;

    private string _defenderHeaderText = string.Empty;

    private string _historyDetailsHeaderText = string.Empty;

    private string _selectedWarText = string.Empty;

    private string _playerInvolvedText = string.Empty;

    private string _durationLabelText = string.Empty;

    private string _warStatusLabelText = string.Empty;

    private string _attackerRoleText = string.Empty;

    private string _defenderRoleText = string.Empty;

    private string _advantageText = string.Empty;

    private string _stalemateText = string.Empty;

    private string _wearinessLabelText = string.Empty;

    private string _attackerFallenLordsText = string.Empty;

    private string _defenderFallenLordsText = string.Empty;

    private string _noFallenLordsText = string.Empty;

    private string _deathTrackingNoticeText = string.Empty;

    private string _summary1Label = string.Empty;

    private string _summary1Value = string.Empty;

    private string _summary1Detail = string.Empty;

    private string _summary2Label = string.Empty;

    private string _summary2Value = string.Empty;

    private string _summary2Detail = string.Empty;

    private string _summary3Label = string.Empty;

    private string _summary3Value = string.Empty;

    private string _summary3Detail = string.Empty;

    private string _pageText = string.Empty;

    private string _emptyText = string.Empty;

    private bool _currentTabSelected;

    private bool _historyTabSelected;

    private bool _showCurrentPanel;

    private bool _showHistoryPanel;

    private bool _currentWarsScrollable;

    private bool _historyWarsScrollable;

    private bool _showPaginationControls;

    private bool _hasCurrentRows;

    private bool _hasHistoryRows;

    private bool _showEmptyState;

    private bool _showHistoryDetail;

    private bool _canGoPrev;

    private bool _canGoNext;

    private bool _canDeleteSelectedHistory;

    private bool _historyInvolvesPlayer;

    private bool _showHistoryAdvantageA;

    private bool _showHistoryAdvantageB;

    private bool _showHistoryStalemate;

    private bool _hasHistoryDeathsA;

    private bool _hasHistoryDeathsB;

    private bool _showNoHistoryDeathsA;

    private bool _showNoHistoryDeathsB;

    private MBBindingList<AfWarStatsRowVM> _currentRows = new();

    private MBBindingList<AfWarStatsHistoryItemVM> _historyRows = new();

    private MBBindingList<AfWarStatsDeathItemVM> _historyDeathsA = new();

    private MBBindingList<AfWarStatsDeathItemVM> _historyDeathsB = new();

    private string _historyNameA = string.Empty;

    private string _historyNameB = string.Empty;

    private string _historyKillsA = string.Empty;

    private string _historyKillsB = string.Empty;

    private string _historyCasualtiesA = string.Empty;

    private string _historyCasualtiesB = string.Empty;

    private string _historyRecordA = string.Empty;

    private string _historyRecordB = string.Empty;

    private string _historyWearinessA = string.Empty;

    private string _historyWearinessB = string.Empty;

    private string _historyDuration = string.Empty;

    private string _historyStatus = string.Empty;

    private string _historyDateRange = string.Empty;

    private string _historyDeathCountA = string.Empty;

    private string _historyDeathCountB = string.Empty;

    private string _historyTerritoryALabel = string.Empty;

    private string _historyTerritoryBLabel = string.Empty;

    private string _historyTerritoryA = string.Empty;

    private string _historyTerritoryB = string.Empty;

    private string _historyWearinessBarTextA = string.Empty;

    private string _historyWearinessBarTextB = string.Empty;

    private float _historyWearinessBarWidthA;

    private float _historyWearinessBarWidthB;

    private BannerImageIdentifierVM _historyBannerA;

    private BannerImageIdentifierVM _historyBannerB;

    [DataSourceProperty]
    public string TitleText
    {
        get => _titleText;
        set => SetField(ref _titleText, value, nameof(TitleText));
    }

    [DataSourceProperty]
    public string SubtitleText
    {
        get => _subtitleText;
        set => SetField(ref _subtitleText, value, nameof(SubtitleText));
    }

    [DataSourceProperty]
    public string CurrentTabText
    {
        get => _currentTabText;
        set => SetField(ref _currentTabText, value, nameof(CurrentTabText));
    }

    [DataSourceProperty]
    public string HistoryTabText
    {
        get => _historyTabText;
        set => SetField(ref _historyTabText, value, nameof(HistoryTabText));
    }

    [DataSourceProperty]
    public string ClearAllText
    {
        get => _clearAllText;
        set => SetField(ref _clearAllText, value, nameof(ClearAllText));
    }

    [DataSourceProperty]
    public string CurrentSortLabelText
    {
        get => _currentSortLabelText;
        set => SetField(ref _currentSortLabelText, value, nameof(CurrentSortLabelText));
    }

    [DataSourceProperty]
    public string HistorySortLabelText
    {
        get => _historySortLabelText;
        set => SetField(ref _historySortLabelText, value, nameof(HistorySortLabelText));
    }

    [DataSourceProperty]
    public string SelectAllText
    {
        get => _selectAllText;
        set => SetField(ref _selectAllText, value, nameof(SelectAllText));
    }

    [DataSourceProperty]
    public string DeleteSelectedText
    {
        get => _deleteSelectedText;
        set => SetField(ref _deleteSelectedText, value, nameof(DeleteSelectedText));
    }

    [DataSourceProperty]
    public string HistorySelectionCountText
    {
        get => _historySelectionCountText;
        set => SetField(ref _historySelectionCountText, value, nameof(HistorySelectionCountText));
    }

    [DataSourceProperty]
    public string PrevText
    {
        get => _prevText;
        set => SetField(ref _prevText, value, nameof(PrevText));
    }

    [DataSourceProperty]
    public string NextText
    {
        get => _nextText;
        set => SetField(ref _nextText, value, nameof(NextText));
    }

    [DataSourceProperty]
    public string CloseText
    {
        get => _closeText;
        set => SetField(ref _closeText, value, nameof(CloseText));
    }

    [DataSourceProperty]
    public string VersusText
    {
        get => _versusText;
        set => SetField(ref _versusText, value, nameof(VersusText));
    }

    [DataSourceProperty]
    public string AttackerHeaderText
    {
        get => _attackerHeaderText;
        set => SetField(ref _attackerHeaderText, value, nameof(AttackerHeaderText));
    }

    [DataSourceProperty]
    public string WarHeaderText
    {
        get => _warHeaderText;
        set => SetField(ref _warHeaderText, value, nameof(WarHeaderText));
    }

    [DataSourceProperty]
    public string DefenderHeaderText
    {
        get => _defenderHeaderText;
        set => SetField(ref _defenderHeaderText, value, nameof(DefenderHeaderText));
    }

    [DataSourceProperty]
    public string HistoryDetailsHeaderText
    {
        get => _historyDetailsHeaderText;
        set => SetField(ref _historyDetailsHeaderText, value, nameof(HistoryDetailsHeaderText));
    }

    [DataSourceProperty]
    public string SelectedWarText
    {
        get => _selectedWarText;
        set => SetField(ref _selectedWarText, value, nameof(SelectedWarText));
    }

    [DataSourceProperty]
    public string PlayerInvolvedText
    {
        get => _playerInvolvedText;
        set => SetField(ref _playerInvolvedText, value, nameof(PlayerInvolvedText));
    }

    [DataSourceProperty]
    public string DurationLabelText
    {
        get => _durationLabelText;
        set => SetField(ref _durationLabelText, value, nameof(DurationLabelText));
    }

    [DataSourceProperty]
    public string WarStatusLabelText
    {
        get => _warStatusLabelText;
        set => SetField(ref _warStatusLabelText, value, nameof(WarStatusLabelText));
    }

    [DataSourceProperty]
    public string AttackerRoleText
    {
        get => _attackerRoleText;
        set => SetField(ref _attackerRoleText, value, nameof(AttackerRoleText));
    }

    [DataSourceProperty]
    public string DefenderRoleText
    {
        get => _defenderRoleText;
        set => SetField(ref _defenderRoleText, value, nameof(DefenderRoleText));
    }

    [DataSourceProperty]
    public string AdvantageText
    {
        get => _advantageText;
        set => SetField(ref _advantageText, value, nameof(AdvantageText));
    }

    [DataSourceProperty]
    public string StalemateText
    {
        get => _stalemateText;
        set => SetField(ref _stalemateText, value, nameof(StalemateText));
    }

    [DataSourceProperty]
    public string WearinessLabelText
    {
        get => _wearinessLabelText;
        set => SetField(ref _wearinessLabelText, value, nameof(WearinessLabelText));
    }

    [DataSourceProperty]
    public string AttackerFallenLordsText
    {
        get => _attackerFallenLordsText;
        set => SetField(ref _attackerFallenLordsText, value, nameof(AttackerFallenLordsText));
    }

    [DataSourceProperty]
    public string DefenderFallenLordsText
    {
        get => _defenderFallenLordsText;
        set => SetField(ref _defenderFallenLordsText, value, nameof(DefenderFallenLordsText));
    }

    [DataSourceProperty]
    public string NoFallenLordsText
    {
        get => _noFallenLordsText;
        set => SetField(ref _noFallenLordsText, value, nameof(NoFallenLordsText));
    }

    [DataSourceProperty]
    public string DeathTrackingNoticeText
    {
        get => _deathTrackingNoticeText;
        set => SetField(ref _deathTrackingNoticeText, value, nameof(DeathTrackingNoticeText));
    }

    [DataSourceProperty]
    public string Summary1Label
    {
        get => _summary1Label;
        set => SetField(ref _summary1Label, value, nameof(Summary1Label));
    }

    [DataSourceProperty]
    public string Summary1Value
    {
        get => _summary1Value;
        set => SetField(ref _summary1Value, value, nameof(Summary1Value));
    }

    [DataSourceProperty]
    public string Summary1Detail
    {
        get => _summary1Detail;
        set => SetField(ref _summary1Detail, value, nameof(Summary1Detail));
    }

    [DataSourceProperty]
    public string Summary2Label
    {
        get => _summary2Label;
        set => SetField(ref _summary2Label, value, nameof(Summary2Label));
    }

    [DataSourceProperty]
    public string Summary2Value
    {
        get => _summary2Value;
        set => SetField(ref _summary2Value, value, nameof(Summary2Value));
    }

    [DataSourceProperty]
    public string Summary2Detail
    {
        get => _summary2Detail;
        set => SetField(ref _summary2Detail, value, nameof(Summary2Detail));
    }

    [DataSourceProperty]
    public string Summary3Label
    {
        get => _summary3Label;
        set => SetField(ref _summary3Label, value, nameof(Summary3Label));
    }

    [DataSourceProperty]
    public string Summary3Value
    {
        get => _summary3Value;
        set => SetField(ref _summary3Value, value, nameof(Summary3Value));
    }

    [DataSourceProperty]
    public string Summary3Detail
    {
        get => _summary3Detail;
        set => SetField(ref _summary3Detail, value, nameof(Summary3Detail));
    }

    [DataSourceProperty]
    public string PageText
    {
        get => _pageText;
        set => SetField(ref _pageText, value, nameof(PageText));
    }

    [DataSourceProperty]
    public string EmptyText
    {
        get => _emptyText;
        set => SetField(ref _emptyText, value, nameof(EmptyText));
    }

    [DataSourceProperty]
    public SelectorVM<SelectorItemVM> HistorySortSelector
    {
        get => _historySortSelector;
        set => SetField(ref _historySortSelector, value, nameof(HistorySortSelector));
    }

    [DataSourceProperty]
    public SelectorVM<SelectorItemVM> CurrentSortSelector
    {
        get => _currentSortSelector;
        set => SetField(ref _currentSortSelector, value, nameof(CurrentSortSelector));
    }

    [DataSourceProperty]
    public bool CurrentTabSelected
    {
        get => _currentTabSelected;
        set => SetField(ref _currentTabSelected, value, nameof(CurrentTabSelected));
    }

    [DataSourceProperty]
    public bool HistoryTabSelected
    {
        get => _historyTabSelected;
        set => SetField(ref _historyTabSelected, value, nameof(HistoryTabSelected));
    }

    [DataSourceProperty]
    public bool ShowCurrentPanel
    {
        get => _showCurrentPanel;
        set => SetField(ref _showCurrentPanel, value, nameof(ShowCurrentPanel));
    }

    [DataSourceProperty]
    public bool ShowHistoryPanel
    {
        get => _showHistoryPanel;
        set => SetField(ref _showHistoryPanel, value, nameof(ShowHistoryPanel));
    }

    [DataSourceProperty]
    public bool CurrentWarsScrollable
    {
        get => _currentWarsScrollable;
        set => SetField(ref _currentWarsScrollable, value, nameof(CurrentWarsScrollable));
    }

    [DataSourceProperty]
    public bool HistoryWarsScrollable
    {
        get => _historyWarsScrollable;
        set => SetField(ref _historyWarsScrollable, value, nameof(HistoryWarsScrollable));
    }

    [DataSourceProperty]
    public bool ShowPaginationControls
    {
        get => _showPaginationControls;
        set => SetField(ref _showPaginationControls, value, nameof(ShowPaginationControls));
    }

    [DataSourceProperty]
    public bool HasCurrentRows
    {
        get => _hasCurrentRows;
        set => SetField(ref _hasCurrentRows, value, nameof(HasCurrentRows));
    }

    [DataSourceProperty]
    public bool HasHistoryRows
    {
        get => _hasHistoryRows;
        set => SetField(ref _hasHistoryRows, value, nameof(HasHistoryRows));
    }

    [DataSourceProperty]
    public bool ShowEmptyState
    {
        get => _showEmptyState;
        set => SetField(ref _showEmptyState, value, nameof(ShowEmptyState));
    }

    [DataSourceProperty]
    public bool ShowHistoryDetail
    {
        get => _showHistoryDetail;
        set => SetField(ref _showHistoryDetail, value, nameof(ShowHistoryDetail));
    }

    [DataSourceProperty]
    public bool CanGoPrev
    {
        get => _canGoPrev;
        set => SetField(ref _canGoPrev, value, nameof(CanGoPrev));
    }

    [DataSourceProperty]
    public bool CanGoNext
    {
        get => _canGoNext;
        set => SetField(ref _canGoNext, value, nameof(CanGoNext));
    }

    [DataSourceProperty]
    public bool CanDeleteSelectedHistory
    {
        get => _canDeleteSelectedHistory;
        set => SetField(ref _canDeleteSelectedHistory, value, nameof(CanDeleteSelectedHistory));
    }

    [DataSourceProperty]
    public bool HistoryInvolvesPlayer
    {
        get => _historyInvolvesPlayer;
        set => SetField(ref _historyInvolvesPlayer, value, nameof(HistoryInvolvesPlayer));
    }

    [DataSourceProperty]
    public bool ShowHistoryAdvantageA
    {
        get => _showHistoryAdvantageA;
        set => SetField(ref _showHistoryAdvantageA, value, nameof(ShowHistoryAdvantageA));
    }

    [DataSourceProperty]
    public bool ShowHistoryAdvantageB
    {
        get => _showHistoryAdvantageB;
        set => SetField(ref _showHistoryAdvantageB, value, nameof(ShowHistoryAdvantageB));
    }

    [DataSourceProperty]
    public bool ShowHistoryStalemate
    {
        get => _showHistoryStalemate;
        set => SetField(ref _showHistoryStalemate, value, nameof(ShowHistoryStalemate));
    }

    [DataSourceProperty]
    public bool HasHistoryDeathsA
    {
        get => _hasHistoryDeathsA;
        set => SetField(ref _hasHistoryDeathsA, value, nameof(HasHistoryDeathsA));
    }

    [DataSourceProperty]
    public bool HasHistoryDeathsB
    {
        get => _hasHistoryDeathsB;
        set => SetField(ref _hasHistoryDeathsB, value, nameof(HasHistoryDeathsB));
    }

    [DataSourceProperty]
    public bool ShowNoHistoryDeathsA
    {
        get => _showNoHistoryDeathsA;
        set => SetField(ref _showNoHistoryDeathsA, value, nameof(ShowNoHistoryDeathsA));
    }

    [DataSourceProperty]
    public bool ShowNoHistoryDeathsB
    {
        get => _showNoHistoryDeathsB;
        set => SetField(ref _showNoHistoryDeathsB, value, nameof(ShowNoHistoryDeathsB));
    }

    [DataSourceProperty]
    public MBBindingList<AfWarStatsRowVM> CurrentRows
    {
        get => _currentRows;
        set => SetField(ref _currentRows, value, nameof(CurrentRows));
    }

    [DataSourceProperty]
    public MBBindingList<AfWarStatsHistoryItemVM> HistoryRows
    {
        get => _historyRows;
        set => SetField(ref _historyRows, value, nameof(HistoryRows));
    }

    [DataSourceProperty]
    public MBBindingList<AfWarStatsDeathItemVM> HistoryDeathsA
    {
        get => _historyDeathsA;
        set => SetField(ref _historyDeathsA, value, nameof(HistoryDeathsA));
    }

    [DataSourceProperty]
    public MBBindingList<AfWarStatsDeathItemVM> HistoryDeathsB
    {
        get => _historyDeathsB;
        set => SetField(ref _historyDeathsB, value, nameof(HistoryDeathsB));
    }

    [DataSourceProperty]
    public string HistoryNameA
    {
        get => _historyNameA;
        set => SetField(ref _historyNameA, value, nameof(HistoryNameA));
    }

    [DataSourceProperty]
    public string HistoryNameB
    {
        get => _historyNameB;
        set => SetField(ref _historyNameB, value, nameof(HistoryNameB));
    }

    [DataSourceProperty]
    public string HistoryKillsA
    {
        get => _historyKillsA;
        set => SetField(ref _historyKillsA, value, nameof(HistoryKillsA));
    }

    [DataSourceProperty]
    public string HistoryKillsB
    {
        get => _historyKillsB;
        set => SetField(ref _historyKillsB, value, nameof(HistoryKillsB));
    }

    [DataSourceProperty]
    public string HistoryCasualtiesA
    {
        get => _historyCasualtiesA;
        set => SetField(ref _historyCasualtiesA, value, nameof(HistoryCasualtiesA));
    }

    [DataSourceProperty]
    public string HistoryCasualtiesB
    {
        get => _historyCasualtiesB;
        set => SetField(ref _historyCasualtiesB, value, nameof(HistoryCasualtiesB));
    }

    [DataSourceProperty]
    public string HistoryRecordA
    {
        get => _historyRecordA;
        set => SetField(ref _historyRecordA, value, nameof(HistoryRecordA));
    }

    [DataSourceProperty]
    public string HistoryRecordB
    {
        get => _historyRecordB;
        set => SetField(ref _historyRecordB, value, nameof(HistoryRecordB));
    }

    [DataSourceProperty]
    public string HistoryWearinessA
    {
        get => _historyWearinessA;
        set => SetField(ref _historyWearinessA, value, nameof(HistoryWearinessA));
    }

    [DataSourceProperty]
    public string HistoryWearinessB
    {
        get => _historyWearinessB;
        set => SetField(ref _historyWearinessB, value, nameof(HistoryWearinessB));
    }

    [DataSourceProperty]
    public string HistoryDuration
    {
        get => _historyDuration;
        set => SetField(ref _historyDuration, value, nameof(HistoryDuration));
    }

    [DataSourceProperty]
    public string HistoryStatus
    {
        get => _historyStatus;
        set => SetField(ref _historyStatus, value, nameof(HistoryStatus));
    }

    [DataSourceProperty]
    public string HistoryDateRange
    {
        get => _historyDateRange;
        set => SetField(ref _historyDateRange, value, nameof(HistoryDateRange));
    }

    [DataSourceProperty]
    public string HistoryDeathCountA
    {
        get => _historyDeathCountA;
        set => SetField(ref _historyDeathCountA, value, nameof(HistoryDeathCountA));
    }

    [DataSourceProperty]
    public string HistoryDeathCountB
    {
        get => _historyDeathCountB;
        set => SetField(ref _historyDeathCountB, value, nameof(HistoryDeathCountB));
    }

    [DataSourceProperty]
    public string HistoryTerritoryALabel
    {
        get => _historyTerritoryALabel;
        set => SetField(ref _historyTerritoryALabel, value, nameof(HistoryTerritoryALabel));
    }

    [DataSourceProperty]
    public string HistoryTerritoryBLabel
    {
        get => _historyTerritoryBLabel;
        set => SetField(ref _historyTerritoryBLabel, value, nameof(HistoryTerritoryBLabel));
    }

    [DataSourceProperty]
    public string HistoryTerritoryA
    {
        get => _historyTerritoryA;
        set => SetField(ref _historyTerritoryA, value, nameof(HistoryTerritoryA));
    }

    [DataSourceProperty]
    public string HistoryTerritoryB
    {
        get => _historyTerritoryB;
        set => SetField(ref _historyTerritoryB, value, nameof(HistoryTerritoryB));
    }

    [DataSourceProperty]
    public string HistoryWearinessBarTextA
    {
        get => _historyWearinessBarTextA;
        set => SetField(ref _historyWearinessBarTextA, value, nameof(HistoryWearinessBarTextA));
    }

    [DataSourceProperty]
    public string HistoryWearinessBarTextB
    {
        get => _historyWearinessBarTextB;
        set => SetField(ref _historyWearinessBarTextB, value, nameof(HistoryWearinessBarTextB));
    }

    [DataSourceProperty]
    public float HistoryWearinessBarWidthA
    {
        get => _historyWearinessBarWidthA;
        set => SetField(ref _historyWearinessBarWidthA, value, nameof(HistoryWearinessBarWidthA));
    }

    [DataSourceProperty]
    public float HistoryWearinessBarWidthB
    {
        get => _historyWearinessBarWidthB;
        set => SetField(ref _historyWearinessBarWidthB, value, nameof(HistoryWearinessBarWidthB));
    }

    [DataSourceProperty]
    public BannerImageIdentifierVM HistoryBannerA
    {
        get => _historyBannerA;
        set => SetField(ref _historyBannerA, value, nameof(HistoryBannerA));
    }

    [DataSourceProperty]
    public BannerImageIdentifierVM HistoryBannerB
    {
        get => _historyBannerB;
        set => SetField(ref _historyBannerB, value, nameof(HistoryBannerB));
    }

    public AfWarStatsPopupVM(Action onClose)
    {
        _onClose = onClose;
        BindLocalizedStaticText();
        CurrentSortSelector = new SelectorVM<SelectorItemVM>(
            AfWarStatsTexts.CurrentSortOptions(),
            AfWarStatsSettings.GetCurrentSortMode(),
            OnCurrentSortChanged);
        HistorySortSelector = new SelectorVM<SelectorItemVM>(
            AfWarStatsTexts.HistorySortOptions(),
            AfWarStatsSettings.GetHistorySortMode(),
            OnHistorySortChanged);
        HistoryBannerA = new BannerImageIdentifierVM(null, true);
        HistoryBannerB = new BannerImageIdentifierVM(null, true);
        RefreshContent();
    }

    public void ExecuteSelectCurrent()
    {
        if (_activeTab == TabKind.CurrentWars)
        {
            return;
        }

        _activeTab = TabKind.CurrentWars;
        RefreshContent();
    }

    public void ExecuteSelectHistory()
    {
        if (_activeTab == TabKind.HistoricalWars)
        {
            return;
        }

        _activeTab = TabKind.HistoricalWars;
        RefreshContent();
    }

    public void ExecutePrevPage()
    {
        ref int pageIndex = ref GetActivePageIndex();
        if (pageIndex > 0)
        {
            pageIndex--;
            RefreshContent();
        }
    }

    public void ExecuteNextPage()
    {
        if (!CanGoNext)
        {
            return;
        }

        ref int pageIndex = ref GetActivePageIndex();
        pageIndex++;
        RefreshContent();
    }

    public void ExecuteSelectAllHistory()
    {
        if (_activeTab != TabKind.HistoricalWars)
        {
            return;
        }

        AfWarStatsBehavior behavior = AfWarStatsBehavior.Instance;
        if (behavior == null)
        {
            return;
        }

        List<AfWarStatsBehavior.HistoricalWarEntry> entries = behavior.BuildHistoricalWars();
        if (entries.Count == 0)
        {
            _selectedHistoryIdentities.Clear();
            BindHistorySelectionControls(0);
            return;
        }

        HashSet<string> allIdentities = new(
            entries.Select(MakeHistoryIdentity),
            StringComparer.Ordinal);
        bool allSelected = allIdentities.All(_selectedHistoryIdentities.Contains);
        if (allSelected)
        {
            _selectedHistoryIdentities.Clear();
        }
        else
        {
            _selectedHistoryIdentities.UnionWith(allIdentities);
        }

        foreach (AfWarStatsHistoryItemVM item in HistoryRows)
        {
            item.IsMarkedForDeletion = _selectedHistoryIdentities.Contains(MakeHistoryIdentity(item.Entry));
        }

        BindHistorySelectionControls(allIdentities.Count);
    }

    public void ExecuteDeleteSelectedHistory()
    {
        if (_activeTab != TabKind.HistoricalWars || _selectedHistoryIdentities.Count == 0)
        {
            return;
        }

        AfWarStatsBehavior behavior = AfWarStatsBehavior.Instance;
        if (behavior == null)
        {
            InformationManager.DisplayMessage(new InformationMessage(AfWarStatsTexts.NotInitializedMessage));
            return;
        }

        List<AfWarStatsBehavior.HistoricalWarEntry> selectedEntries = behavior
            .BuildHistoricalWars()
            .Where(entry => _selectedHistoryIdentities.Contains(MakeHistoryIdentity(entry)))
            .ToList();
        if (selectedEntries.Count == 0)
        {
            _selectedHistoryIdentities.Clear();
            BindHistorySelectionControls(0);
            return;
        }

        InquiryData inquiry = new(
            AfWarStatsTexts.DeleteSelectedTitle,
            AfWarStatsTexts.DeleteSelectedBody(selectedEntries.Count),
            true,
            true,
            AfWarStatsTexts.ConfirmDeleteSelected,
            AfWarStatsTexts.Cancel,
            () =>
            {
                int removed = behavior.DeleteHistoricalWars(selectedEntries);
                foreach (AfWarStatsBehavior.HistoricalWarEntry entry in selectedEntries)
                {
                    _selectedHistoryIdentities.Remove(MakeHistoryIdentity(entry));
                }

                if (selectedEntries.Any(entry => string.Equals(
                    MakeHistoryIdentity(entry),
                    _selectedHistoryIdentity,
                    StringComparison.Ordinal)))
                {
                    _selectedHistoryIdentity = string.Empty;
                }

                RefreshContent();
                InformationManager.DisplayMessage(new InformationMessage(AfWarStatsTexts.RecordsDeleted(removed)));
            },
            null);
        InformationManager.ShowInquiry(inquiry, true);
    }

    public void ExecuteClearAll()
    {
        AfWarStatsBehavior behavior = AfWarStatsBehavior.Instance;
        if (behavior == null)
        {
            InformationManager.DisplayMessage(new InformationMessage(AfWarStatsTexts.NotInitializedMessage));
            return;
        }

        InquiryData inquiry = new(
            AfWarStatsTexts.ClearTitle,
            AfWarStatsTexts.ClearBody,
            true,
            true,
            AfWarStatsTexts.ConfirmClear,
            AfWarStatsTexts.Cancel,
            () =>
            {
                behavior.ClearAllRecords();
                _currentPageIndex = 0;
                _historyPageIndex = 0;
                _selectedHistoryIdentity = string.Empty;
                _selectedHistoryIdentities.Clear();
                RefreshContent();
                InformationManager.DisplayMessage(new InformationMessage(AfWarStatsTexts.RecordsCleared));
            },
            null);
        InformationManager.ShowInquiry(inquiry, true);
    }

    public void ExecuteClose()
    {
        _onClose?.Invoke();
    }

    public void ExecuteOpenHistoryKingdomA()
    {
        AfWarStatsEncyclopedia.OpenKingdom(_historyKingdomA);
    }

    public void ExecuteOpenHistoryKingdomB()
    {
        AfWarStatsEncyclopedia.OpenKingdom(_historyKingdomB);
    }

    private void BindLocalizedStaticText()
    {
        TitleText = AfWarStatsTexts.Title;
        CurrentTabText = AfWarStatsTexts.CurrentWars;
        HistoryTabText = AfWarStatsTexts.HistoricalWars;
        ClearAllText = AfWarStatsTexts.ClearAll;
        CurrentSortLabelText = AfWarStatsTexts.CurrentSortLabel;
        HistorySortLabelText = AfWarStatsTexts.HistorySortLabel;
        SelectAllText = AfWarStatsTexts.SelectAll;
        DeleteSelectedText = AfWarStatsTexts.DeleteSelected;
        HistorySelectionCountText = AfWarStatsTexts.HistorySelectionCount(0);
        PrevText = AfWarStatsTexts.Previous;
        NextText = AfWarStatsTexts.Next;
        CloseText = AfWarStatsTexts.Close;
        VersusText = AfWarStatsTexts.Versus;
        AttackerHeaderText = AfWarStatsTexts.AttackerLeft;
        WarHeaderText = AfWarStatsTexts.WarColumn;
        DefenderHeaderText = AfWarStatsTexts.DefenderRight;
        HistoryDetailsHeaderText = AfWarStatsTexts.HistoryDetails;
        SelectedWarText = AfWarStatsTexts.SelectedWar;
        PlayerInvolvedText = AfWarStatsTexts.PlayerInvolvedBadge;
        DurationLabelText = AfWarStatsTexts.DurationLabel;
        WarStatusLabelText = AfWarStatsTexts.WarStatusLabel;
        AttackerRoleText = AfWarStatsTexts.AttackerRole;
        DefenderRoleText = AfWarStatsTexts.DefenderRole;
        AdvantageText = AfWarStatsTexts.Advantage;
        StalemateText = AfWarStatsTexts.Stalemate;
        WearinessLabelText = AfWarStatsTexts.WearinessLabel;
        AttackerFallenLordsText = AfWarStatsTexts.AttackerFallenLords;
        DefenderFallenLordsText = AfWarStatsTexts.DefenderFallenLords;
        NoFallenLordsText = AfWarStatsTexts.NoFallenLords;
        DeathTrackingNoticeText = AfWarStatsTexts.DeathTrackingNotice;
    }

    private void OnCurrentSortChanged(SelectorVM<SelectorItemVM> selector)
    {
        if (selector == null)
        {
            return;
        }

        AfWarStatsSettings.SetCurrentSortMode(selector.SelectedIndex);
        if (_activeTab == TabKind.CurrentWars)
        {
            _currentPageIndex = 0;
            RefreshCurrentWars();
        }
    }

    private void OnHistorySortChanged(SelectorVM<SelectorItemVM> selector)
    {
        if (selector == null)
        {
            return;
        }

        AfWarStatsSettings.SetHistorySortMode(selector.SelectedIndex);
        if (_activeTab == TabKind.HistoricalWars)
        {
            _historyPageIndex = 0;
            RefreshHistoricalWars();
        }
    }

    private void RefreshContent()
    {
        CurrentTabSelected = _activeTab == TabKind.CurrentWars;
        HistoryTabSelected = _activeTab == TabKind.HistoricalWars;
        ShowCurrentPanel = CurrentTabSelected;
        ShowHistoryPanel = HistoryTabSelected;

        if (CurrentTabSelected)
        {
            RefreshCurrentWars();
        }
        else
        {
            RefreshHistoricalWars();
        }
    }

    private void RefreshCurrentWars()
    {
        CurrentWarsScrollable = AfWarStatsSettings.GetCurrentWarsDisplayMode() == 1;
        ShowPaginationControls = !CurrentWarsScrollable;
        SubtitleText = AfWarStatsTexts.CurrentSubtitle(CurrentRowsPerPage, CurrentWarsScrollable);
        AfWarStatsBehavior behavior = AfWarStatsBehavior.Instance;
        if (behavior == null)
        {
            BindUnavailableState();
            return;
        }

        List<AfWarStatsBehavior.WarEntry> entries = SortCurrentWars(
            behavior.BuildCurrentWars(),
            AfWarStatsSettings.GetCurrentSortMode());
        Summary1Label = AfWarStatsTexts.CurrentWars;
        Summary1Value = entries.Count.ToString("N0");
        Summary1Detail = AfWarStatsTexts.PlayerInvolvedCount(entries.Count(static entry => entry.InvolvesPlayer));
        Summary2Label = AfWarStatsTexts.TotalKills;
        Summary2Value = entries.Sum(static entry => entry.KillsA + entry.KillsB).ToString("N0");
        Summary2Detail = AfWarStatsTexts.ConfirmedDeaths;
        Summary3Label = AfWarStatsTexts.TotalCasualties;
        Summary3Value = entries.Sum(static entry => entry.CasualtiesA + entry.CasualtiesB).ToString("N0");
        Summary3Detail = AfWarStatsTexts.DeadAndWounded;

        int totalPages = CurrentWarsScrollable
            ? 1
            : ClampPage(ref _currentPageIndex, entries.Count, CurrentRowsPerPage);
        ClearCurrentRows();
        int start = CurrentWarsScrollable ? 0 : _currentPageIndex * CurrentRowsPerPage;
        int end = CurrentWarsScrollable
            ? entries.Count
            : Math.Min(entries.Count, start + CurrentRowsPerPage);
        for (int i = start; i < end; i++)
        {
            CurrentRows.Add(new AfWarStatsRowVM(entries[i], (i - start) % 2 == 0));
        }

        HasCurrentRows = CurrentRows.Count > 0;
        HasHistoryRows = false;
        ShowHistoryDetail = false;
        ShowEmptyState = !HasCurrentRows;
        EmptyText = AfWarStatsTexts.NoActiveWars;
        if (CurrentWarsScrollable)
        {
            PageText = string.Empty;
            CanGoPrev = false;
            CanGoNext = false;
        }
        else
        {
            BindPagination(_currentPageIndex, totalPages, CurrentRowsPerPage);
        }
    }

    private void RefreshHistoricalWars()
    {
        CurrentWarsScrollable = false;
        HistoryWarsScrollable = AfWarStatsSettings.GetHistoryWarsDisplayMode() == 1;
        ShowPaginationControls = !HistoryWarsScrollable;
        SubtitleText = AfWarStatsTexts.HistorySubtitle;
        AfWarStatsBehavior behavior = AfWarStatsBehavior.Instance;
        if (behavior == null)
        {
            BindUnavailableState();
            return;
        }

        List<AfWarStatsBehavior.HistoricalWarEntry> entries = SortHistoricalWars(
            behavior.BuildHistoricalWars(),
            AfWarStatsSettings.GetHistorySortMode());
        _historyTotalCount = entries.Count;
        HashSet<string> availableIdentities = new(
            entries.Select(MakeHistoryIdentity),
            StringComparer.Ordinal);
        _selectedHistoryIdentities.RemoveWhere(identity => !availableIdentities.Contains(identity));
        Summary1Label = AfWarStatsTexts.HistoricalWars;
        Summary1Value = entries.Count.ToString("N0");
        Summary1Detail = AfWarStatsTexts.PlayerInvolvedCount(entries.Count(static entry => entry.InvolvesPlayer));
        Summary2Label = AfWarStatsTexts.TotalKills;
        Summary2Value = entries.Sum(static entry => entry.TotalKills).ToString("N0");
        Summary2Detail = AfWarStatsTexts.EndedWars;
        Summary3Label = AfWarStatsTexts.TotalCasualties;
        Summary3Value = entries.Sum(static entry => entry.TotalCasualties).ToString("N0");
        Summary3Detail = AfWarStatsTexts.DeadAndWounded;
        BindHistorySelectionControls(entries.Count);

        int totalPages = HistoryWarsScrollable
            ? 1
            : ClampPage(ref _historyPageIndex, entries.Count, HistoryRowsPerPage);
        ClearHistoryRows();
        int start = HistoryWarsScrollable ? 0 : _historyPageIndex * HistoryRowsPerPage;
        int end = HistoryWarsScrollable
            ? entries.Count
            : Math.Min(entries.Count, start + HistoryRowsPerPage);
        AfWarStatsHistoryItemVM selected = null;
        for (int i = start; i < end; i++)
        {
            AfWarStatsBehavior.HistoricalWarEntry entry = entries[i];
            AfWarStatsHistoryItemVM item = new(
                entry,
                SelectHistoryItem,
                ToggleHistoryDeletion,
                _selectedHistoryIdentities.Contains(MakeHistoryIdentity(entry)));
            HistoryRows.Add(item);
            if (string.Equals(MakeHistoryIdentity(entry), _selectedHistoryIdentity, StringComparison.Ordinal))
            {
                selected = item;
            }
        }

        HasHistoryRows = HistoryRows.Count > 0;
        HasCurrentRows = false;
        ShowEmptyState = !HasHistoryRows;
        EmptyText = AfWarStatsTexts.NoHistoricalWars;
        ShowHistoryDetail = HasHistoryRows;
        if (HasHistoryRows)
        {
            SelectHistoryItem(selected ?? HistoryRows[0]);
        }
        else
        {
            ClearHistoryDetail();
        }

        if (HistoryWarsScrollable)
        {
            PageText = string.Empty;
            CanGoPrev = false;
            CanGoNext = false;
        }
        else
        {
            BindPagination(_historyPageIndex, totalPages, HistoryRowsPerPage);
        }
    }

    private static List<AfWarStatsBehavior.WarEntry> SortCurrentWars(
        List<AfWarStatsBehavior.WarEntry> entries,
        int sortMode)
    {
        IEnumerable<AfWarStatsBehavior.WarEntry> source = entries ?? new List<AfWarStatsBehavior.WarEntry>();
        IOrderedEnumerable<AfWarStatsBehavior.WarEntry> ordered = sortMode switch
        {
            1 => source
                .OrderBy(entry => entry.StartDay),
            2 => source
                .OrderByDescending(entry => entry.DurationDays)
                .ThenByDescending(entry => entry.StartDay),
            3 => source
                .OrderByDescending(entry => entry.CasualtiesA + entry.CasualtiesB)
                .ThenByDescending(entry => entry.StartDay),
            4 => source
                .OrderByDescending(entry => Math.Max(entry.WearinessA, entry.WearinessB))
                .ThenByDescending(entry => entry.StartDay),
            5 => source
                .OrderByDescending(entry => entry.InvolvesPlayer)
                .ThenByDescending(entry => entry.StartDay),
            _ => source
                .OrderByDescending(entry => entry.StartDay)
        };

        return ordered
            .ThenByDescending(entry => entry.DurationDays)
            .ThenByDescending(entry => entry.CasualtiesA + entry.CasualtiesB)
            .ThenBy(entry => entry.PairKey, StringComparer.Ordinal)
            .ToList();
    }

    private static List<AfWarStatsBehavior.HistoricalWarEntry> SortHistoricalWars(
        List<AfWarStatsBehavior.HistoricalWarEntry> entries,
        int sortMode)
    {
        IEnumerable<AfWarStatsBehavior.HistoricalWarEntry> source = entries ?? new List<AfWarStatsBehavior.HistoricalWarEntry>();
        IOrderedEnumerable<AfWarStatsBehavior.HistoricalWarEntry> ordered = sortMode switch
        {
            1 => source
                .OrderBy(entry => entry.StartDay)
                .ThenBy(entry => entry.EndDay),
            2 => source
                .OrderByDescending(entry => entry.DurationDays)
                .ThenByDescending(entry => entry.EndDay),
            3 => source
                .OrderByDescending(entry => entry.TotalCasualties)
                .ThenByDescending(entry => entry.EndDay),
            4 => source
                .OrderByDescending(entry => Math.Max(entry.WearinessA, entry.WearinessB))
                .ThenByDescending(entry => entry.EndDay),
            5 => source
                .OrderByDescending(entry => entry.InvolvesPlayer)
                .ThenByDescending(entry => entry.EndDay)
                .ThenByDescending(entry => entry.StartDay),
            _ => source
                .OrderByDescending(entry => entry.EndDay)
                .ThenByDescending(entry => entry.StartDay)
        };

        return ordered
            .ThenByDescending(entry => entry.TotalCasualties)
            .ThenBy(entry => MakeHistoryIdentity(entry), StringComparer.Ordinal)
            .ToList();
    }

    private void ToggleHistoryDeletion(AfWarStatsHistoryItemVM item)
    {
        if (item == null)
        {
            return;
        }

        string identity = MakeHistoryIdentity(item.Entry);
        if (item.IsMarkedForDeletion)
        {
            _selectedHistoryIdentities.Add(identity);
        }
        else
        {
            _selectedHistoryIdentities.Remove(identity);
        }

        BindHistorySelectionControls(_historyTotalCount);
    }

    private void BindHistorySelectionControls(int totalHistoryCount)
    {
        int selectedCount = _selectedHistoryIdentities.Count;
        SelectAllText = totalHistoryCount > 0 && selectedCount >= totalHistoryCount
            ? AfWarStatsTexts.DeselectAll
            : AfWarStatsTexts.SelectAll;
        DeleteSelectedText = AfWarStatsTexts.DeleteSelected;
        HistorySelectionCountText = AfWarStatsTexts.HistorySelectionCount(selectedCount);
        CanDeleteSelectedHistory = selectedCount > 0;
    }

    private void SelectHistoryItem(AfWarStatsHistoryItemVM selected)
    {
        if (selected == null)
        {
            return;
        }

        foreach (AfWarStatsHistoryItemVM item in HistoryRows)
        {
            item.IsSelected = ReferenceEquals(item, selected);
        }

        _selectedHistoryIdentity = MakeHistoryIdentity(selected.Entry);
        BindHistoryDetail(selected.Entry);
    }

    private void BindHistoryDetail(AfWarStatsBehavior.HistoricalWarEntry entry)
    {
        _historyKingdomA = entry.KingdomA;
        _historyKingdomB = entry.KingdomB;
        int wearinessA = Math.Max(0, Math.Min(100, entry.WearinessA));
        int wearinessB = Math.Max(0, Math.Min(100, entry.WearinessB));
        HistoryNameA = entry.NameA;
        HistoryNameB = entry.NameB;
        HistoryKillsA = AfWarStatsTexts.Kills(entry.KillsA);
        HistoryKillsB = AfWarStatsTexts.Kills(entry.KillsB);
        HistoryCasualtiesA = AfWarStatsTexts.Casualties(entry.CasualtiesA);
        HistoryCasualtiesB = AfWarStatsTexts.Casualties(entry.CasualtiesB);
        HistoryRecordA = AfWarStatsTexts.Record(entry.WinsA, entry.LossesA);
        HistoryRecordB = AfWarStatsTexts.Record(entry.WinsB, entry.LossesB);
        HistoryWearinessA = AfWarStatsTexts.FinalWeariness(wearinessA);
        HistoryWearinessB = AfWarStatsTexts.FinalWeariness(wearinessB);
        HistoryDuration = entry.DurationDays <= 0 ? AfWarStatsTexts.LegacyRecord : AfWarStatsTexts.DurationDays(entry.DurationDays);
        HistoryStatus = AfWarStatsTexts.WarEnded(entry.EndDateText);
        HistoryDateRange = entry.DateRangeText;
        HistoryTerritoryALabel = AfWarStatsTexts.TerritoryOf(entry.NameA);
        HistoryTerritoryBLabel = AfWarStatsTexts.TerritoryOf(entry.NameB);
        HistoryTerritoryA = entry.TerritoryA.ToString("N0");
        HistoryTerritoryB = entry.TerritoryB.ToString("N0");
        HistoryInvolvesPlayer = entry.InvolvesPlayer;
        ShowHistoryAdvantageA = entry.Advantage == AfWarStatsBehavior.WarAdvantage.Attacker;
        ShowHistoryAdvantageB = entry.Advantage == AfWarStatsBehavior.WarAdvantage.Defender;
        ShowHistoryStalemate = entry.Advantage == AfWarStatsBehavior.WarAdvantage.Stalemate;

        ReplaceHistoryBanners(entry);
        HistoryWearinessBarTextA = AfWarStatsTexts.Percent(wearinessA);
        HistoryWearinessBarTextB = AfWarStatsTexts.Percent(wearinessB);
        HistoryWearinessBarWidthA = HistoryWearinessBarTotalWidth * wearinessA / 100f;
        HistoryWearinessBarWidthB = HistoryWearinessBarTotalWidth * wearinessB / 100f;
        BindHistoryDeaths(entry);
    }

    private void ReplaceHistoryBanners(AfWarStatsBehavior.HistoricalWarEntry entry)
    {
        BannerImageIdentifierVM previousA = HistoryBannerA;
        BannerImageIdentifierVM previousB = HistoryBannerB;
        HistoryBannerA = new BannerImageIdentifierVM(entry.BannerA, true);
        HistoryBannerB = new BannerImageIdentifierVM(entry.BannerB, true);
        previousA?.OnFinalize();
        previousB?.OnFinalize();
    }

    private void BindHistoryDeaths(AfWarStatsBehavior.HistoricalWarEntry entry)
    {
        ClearHistoryDeathRows();
        HistoryDeathsA = new MBBindingList<AfWarStatsDeathItemVM>();
        HistoryDeathsB = new MBBindingList<AfWarStatsDeathItemVM>();
        List<AfWarStatsBehavior.HeroDeathEntry> deathsA = entry?.DeathsA ?? new List<AfWarStatsBehavior.HeroDeathEntry>();
        List<AfWarStatsBehavior.HeroDeathEntry> deathsB = entry?.DeathsB ?? new List<AfWarStatsBehavior.HeroDeathEntry>();
        for (int i = 0; i < deathsA.Count; i++)
        {
            HistoryDeathsA.Add(new AfWarStatsDeathItemVM(deathsA[i], i % 2 == 0));
        }

        for (int i = 0; i < deathsB.Count; i++)
        {
            HistoryDeathsB.Add(new AfWarStatsDeathItemVM(deathsB[i], i % 2 == 0));
        }

        HasHistoryDeathsA = HistoryDeathsA.Count > 0;
        HasHistoryDeathsB = HistoryDeathsB.Count > 0;
        ShowNoHistoryDeathsA = !HasHistoryDeathsA;
        ShowNoHistoryDeathsB = !HasHistoryDeathsB;
        HistoryDeathCountA = AfWarStatsTexts.DeathCount(HistoryDeathsA.Count);
        HistoryDeathCountB = AfWarStatsTexts.DeathCount(HistoryDeathsB.Count);
    }

    private void ClearHistoryDetail()
    {
        HistoryNameA = string.Empty;
        HistoryNameB = string.Empty;
        HistoryKillsA = string.Empty;
        HistoryKillsB = string.Empty;
        HistoryCasualtiesA = string.Empty;
        HistoryCasualtiesB = string.Empty;
        HistoryRecordA = string.Empty;
        HistoryRecordB = string.Empty;
        HistoryWearinessA = string.Empty;
        HistoryWearinessB = string.Empty;
        HistoryDuration = string.Empty;
        HistoryStatus = string.Empty;
        HistoryDateRange = string.Empty;
        HistoryTerritoryALabel = string.Empty;
        HistoryTerritoryBLabel = string.Empty;
        HistoryTerritoryA = string.Empty;
        HistoryTerritoryB = string.Empty;
        HistoryWearinessBarTextA = string.Empty;
        HistoryWearinessBarTextB = string.Empty;
        HistoryWearinessBarWidthA = 0f;
        HistoryWearinessBarWidthB = 0f;
        _historyKingdomA = null;
        _historyKingdomB = null;
        HistoryInvolvesPlayer = false;
        ShowHistoryAdvantageA = false;
        ShowHistoryAdvantageB = false;
        ShowHistoryStalemate = false;
        ClearHistoryDeathRows();
        HasHistoryDeathsA = false;
        HasHistoryDeathsB = false;
        ShowNoHistoryDeathsA = true;
        ShowNoHistoryDeathsB = true;
        HistoryDeathCountA = AfWarStatsTexts.DeathCount(0);
        HistoryDeathCountB = AfWarStatsTexts.DeathCount(0);
        BannerImageIdentifierVM previousA = HistoryBannerA;
        BannerImageIdentifierVM previousB = HistoryBannerB;
        HistoryBannerA = new BannerImageIdentifierVM(null, true);
        HistoryBannerB = new BannerImageIdentifierVM(null, true);
        previousA?.OnFinalize();
        previousB?.OnFinalize();
    }

    private void BindUnavailableState()
    {
        CurrentWarsScrollable = false;
        HistoryWarsScrollable = false;
        ShowPaginationControls = true;
        Summary1Label = AfWarStatsTexts.Status;
        Summary1Value = AfWarStatsTexts.NotInitialized;
        Summary1Detail = AfWarStatsTexts.WaitingForCampaign;
        Summary2Label = AfWarStatsTexts.TotalKills;
        Summary2Value = "-";
        Summary2Detail = string.Empty;
        Summary3Label = AfWarStatsTexts.TotalCasualties;
        Summary3Value = "-";
        Summary3Detail = string.Empty;
        ClearCurrentRows();
        ClearHistoryRows();
        ClearHistoryDetail();
        HasCurrentRows = false;
        HasHistoryRows = false;
        ShowHistoryDetail = false;
        ShowEmptyState = true;
        EmptyText = AfWarStatsTexts.BehaviorNotInitialized;
        BindPagination(
            0,
            1,
            _activeTab == TabKind.HistoricalWars ? HistoryRowsPerPage : CurrentRowsPerPage);
    }

    private static int ClampPage(ref int pageIndex, int count, int rowsPerPage)
    {
        int totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)rowsPerPage));
        pageIndex = Math.Max(0, Math.Min(pageIndex, totalPages - 1));
        return totalPages;
    }

    private void BindPagination(int pageIndex, int totalPages, int rowsPerPage)
    {
        PageText = AfWarStatsTexts.Page(pageIndex + 1, totalPages, rowsPerPage);
        CanGoPrev = pageIndex > 0;
        CanGoNext = pageIndex + 1 < totalPages;
    }

    private ref int GetActivePageIndex()
    {
        if (_activeTab == TabKind.CurrentWars)
        {
            return ref _currentPageIndex;
        }

        return ref _historyPageIndex;
    }

    private void ClearCurrentRows()
    {
        foreach (AfWarStatsRowVM row in CurrentRows)
        {
            row.OnFinalize();
        }

        CurrentRows.Clear();
    }

    private void ClearHistoryRows()
    {
        foreach (AfWarStatsHistoryItemVM row in HistoryRows)
        {
            row.OnFinalize();
        }

        HistoryRows.Clear();
    }

    private void ClearHistoryDeathRows()
    {
        foreach (AfWarStatsDeathItemVM row in HistoryDeathsA)
        {
            row.OnFinalize();
        }

        foreach (AfWarStatsDeathItemVM row in HistoryDeathsB)
        {
            row.OnFinalize();
        }

        HistoryDeathsA.Clear();
        HistoryDeathsB.Clear();
    }

    private static string MakeHistoryIdentity(AfWarStatsBehavior.HistoricalWarEntry entry)
    {
        return entry == null
            ? string.Empty
            : AfWarStatsBehavior.MakeHistoricalIdentity(entry.PairKey, entry.StartDay, entry.EndDay);
    }

    public override void OnFinalize()
    {
        ClearCurrentRows();
        ClearHistoryRows();
        ClearHistoryDeathRows();
        HistoryBannerA?.OnFinalize();
        HistoryBannerB?.OnFinalize();
        base.OnFinalize();
    }
}
