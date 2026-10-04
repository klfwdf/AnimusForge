using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class TerminalWeeklyReportBrowserPopupVM
{
    private const int PageSize = 12;
    private int _page;
    private string _typeFilter = "all";
    private List<MyBehavior.WeeklyReportBrowserEntryData> _sourceReports = new List<MyBehavior.WeeklyReportBrowserEntryData>();
    private List<MyBehavior.WeeklyReportBrowserEntryData> _filteredReports = new List<MyBehavior.WeeklyReportBrowserEntryData>();
    private TerminalWeeklyReportEntryItemVM _readerItem;
    private EncyclopediaEntityLinkFormatter.DisplaySession _readerLinks;
    private float _listScrollPosition, _readerScrollPosition;
    private float _savedListScrollPosition;
    private readonly Action<string> _onOpenEncyclopediaLink;
    private bool CanInteract => !_isFinalized && SaveRuntimeGuard.IsCurrentGeneration(_saveGeneration);

    [DataSourceProperty] public bool IsReading => _readerItem != null;
    [DataSourceProperty] public bool ShowReportList => !IsReading;
    [DataSourceProperty] public string FilterText => _typeFilter == "all" ? "全部" : _typeFilter == "bulletin" ? "快报" : _typeFilter == "recent" ? "近况" : "周报";
    [DataSourceProperty] public bool IsAllSelected => _typeFilter == "all";
    [DataSourceProperty] public bool IsBulletinSelected => _typeFilter == "bulletin";
    [DataSourceProperty] public bool IsRecentSelected => _typeFilter == "recent";
    [DataSourceProperty] public bool IsWeeklySelected => _typeFilter == "weekly";
    [DataSourceProperty] public string PageText => (_filteredReports.Count == 0 ? 0 : _page + 1) + " / " + Math.Max(1,(_filteredReports.Count + PageSize - 1) / PageSize);
    [DataSourceProperty] public bool CanPreviousPage => _page > 0;
    [DataSourceProperty] public bool CanNextPage => (_page + 1) * PageSize < _filteredReports.Count;
    [DataSourceProperty] public float ListScrollPosition { get => _listScrollPosition; set { _listScrollPosition=value; OnPropertyChangedWithValue(value,nameof(ListScrollPosition)); } }
    [DataSourceProperty] public float ReaderScrollPosition { get => _readerScrollPosition; set { _readerScrollPosition=value; OnPropertyChangedWithValue(value,nameof(ReaderScrollPosition)); } }
    [DataSourceProperty] public string ReaderTitleText => _readerItem?.TitleText ?? "";
    [DataSourceProperty] public string ReaderMetaText => (_readerItem?.WeekText ?? "") + " · " + (_readerItem?.DateText ?? "");
    [DataSourceProperty] public string ReaderBodyText { get; private set; } = "";
    [DataSourceProperty] public int ReaderBodyFontSize => _readerItem?.BodyFontSize ?? 18;
    [DataSourceProperty] public bool ReaderCanGenerateFull => _readerItem?.ShowViewFullReport == true && _pendingFullReport == null;

    public void ExecuteFilterAll() => SelectType("all");
    public void ExecuteFilterBulletin() => SelectType("bulletin");
    public void ExecuteFilterRecent() => SelectType("recent");
    public void ExecuteFilterWeekly() => SelectType("weekly");
    private void SelectType(string kind)
    {
        if (!CanInteract) return;
        ExecuteReturnToList(); _typeFilter=kind; _page=0; ListScrollPosition=0; ApplyTypeFilter();
    }
    private void ApplyTypeFilter()
    {
        _filteredReports=_sourceReports.Where(e => _typeFilter == "all" || (e.ArchiveKind ?? WeeklyReportArchivePolicy.EntryKind(e.EventId)) == _typeFilter).ToList();
        _page=Math.Min(_page,Math.Max(0,(_filteredReports.Count-1)/PageSize));
        RefreshPage();
        OnPropertyChangedWithValue(IsAllSelected,nameof(IsAllSelected));
        OnPropertyChangedWithValue(IsBulletinSelected,nameof(IsBulletinSelected));
        OnPropertyChangedWithValue(IsRecentSelected,nameof(IsRecentSelected));
        OnPropertyChangedWithValue(IsWeeklySelected,nameof(IsWeeklySelected));
        OnPropertyChangedWithValue(FilterText,nameof(FilterText));
    }
    private void RefreshPage()
    {
        foreach (var row in ReportItems) row.OnFinalize();
        var rows=new MBBindingList<TerminalWeeklyReportEntryItemVM>();
        foreach (var entry in _filteredReports.Skip(_page*PageSize).Take(PageSize))
            rows.Add(new TerminalWeeklyReportEntryItemVM(entry,RequestViewFullReport,RequestOpenReport));
        ReportItems=rows; HasReportItems=rows.Count>0; ShowEmptyState=!HasReportItems;
        EmptyStateText="当前筛选下暂无档案。";
        SelectedCountryMetaText=_filteredReports.Count + " 篇档案 · 每页 " + PageSize + " 篇";
        OnPropertyChangedWithValue(PageText,nameof(PageText));
        OnPropertyChangedWithValue(CanPreviousPage,nameof(CanPreviousPage));
        OnPropertyChangedWithValue(CanNextPage,nameof(CanNextPage));
    }
    public void ExecutePreviousPage() { if (CanInteract && CanPreviousPage) { _page--; ListScrollPosition=0; RefreshPage(); } }
    public void ExecuteNextPage() { if (CanInteract && CanNextPage) { _page++; ListScrollPosition=0; RefreshPage(); } }
    private void OpenReader(TerminalWeeklyReportEntryItemVM item)
    {
        if (!CanInteract) return;
        if (!IsReading) _savedListScrollPosition=ListScrollPosition;
        _readerItem=item; _readerLinks=EncyclopediaEntityLinkFormatter.CreateDisplaySession();
        ReaderBodyText=_readerLinks.Format(item.PlainBodyText); ReaderScrollPosition=0; NotifyReader();
    }
    private void NotifyReader()
    {
        OnPropertyChangedWithValue(IsReading,nameof(IsReading)); OnPropertyChangedWithValue(ShowReportList,nameof(ShowReportList));
        OnPropertyChangedWithValue(ReaderTitleText,nameof(ReaderTitleText)); OnPropertyChangedWithValue(ReaderMetaText,nameof(ReaderMetaText));
        OnPropertyChangedWithValue(ReaderBodyText,nameof(ReaderBodyText)); OnPropertyChangedWithValue(ReaderBodyFontSize,nameof(ReaderBodyFontSize));
        OnPropertyChangedWithValue(ReaderCanGenerateFull,nameof(ReaderCanGenerateFull));
    }
    public void ExecuteReturnToList()
    {
        if (!CanInteract || !IsReading) return;
        _readerItem=null; _readerLinks=null; ReaderBodyText=""; NotifyReader();
        ListScrollPosition=_savedListScrollPosition;
    }
    public void ExecuteReaderGenerateFull() { if (CanInteract && ReaderCanGenerateFull) { RequestViewFullReport(_readerItem.EventId); NotifyReader(); } }
    public void ExecuteOpenEncyclopediaLink(string link) { if (CanInteract && IsReading) _onOpenEncyclopediaLink?.Invoke(link); }
}
