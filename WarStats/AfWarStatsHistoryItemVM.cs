using System;
using AFWarStatsTerminal.Behaviors;
using AFWarStatsTerminal.Localization;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsHistoryItemVM : ViewModel
{
    private readonly Action<AfWarStatsHistoryItemVM> _onSelect;

    private readonly Action<AfWarStatsHistoryItemVM> _onToggleMarked;

    private string _nameAText = string.Empty;

    private string _nameBText = string.Empty;

    private string _versusText = string.Empty;

    private string _statsText = string.Empty;

    private string _durationText = string.Empty;

    private bool _isSelected;

    private bool _isMarkedForDeletion;

    private BannerImageIdentifierVM _bannerA;

    private BannerImageIdentifierVM _bannerB;

    public AfWarStatsBehavior.HistoricalWarEntry Entry { get; }

    [DataSourceProperty]
    public string NameAText
    {
        get => _nameAText;
        set => SetField(ref _nameAText, value, nameof(NameAText));
    }

    [DataSourceProperty]
    public string NameBText
    {
        get => _nameBText;
        set => SetField(ref _nameBText, value, nameof(NameBText));
    }

    [DataSourceProperty]
    public string VersusText
    {
        get => _versusText;
        set => SetField(ref _versusText, value, nameof(VersusText));
    }

    [DataSourceProperty]
    public string StatsText
    {
        get => _statsText;
        set => SetField(ref _statsText, value, nameof(StatsText));
    }

    [DataSourceProperty]
    public string DurationText
    {
        get => _durationText;
        set => SetField(ref _durationText, value, nameof(DurationText));
    }

    [DataSourceProperty]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value, nameof(IsSelected));
    }

    [DataSourceProperty]
    public bool IsMarkedForDeletion
    {
        get => _isMarkedForDeletion;
        set => SetField(ref _isMarkedForDeletion, value, nameof(IsMarkedForDeletion));
    }

    [DataSourceProperty]
    public BannerImageIdentifierVM BannerA
    {
        get => _bannerA;
        set => SetField(ref _bannerA, value, nameof(BannerA));
    }

    [DataSourceProperty]
    public BannerImageIdentifierVM BannerB
    {
        get => _bannerB;
        set => SetField(ref _bannerB, value, nameof(BannerB));
    }

    public AfWarStatsHistoryItemVM(
        AfWarStatsBehavior.HistoricalWarEntry entry,
        Action<AfWarStatsHistoryItemVM> onSelect,
        Action<AfWarStatsHistoryItemVM> onToggleMarked,
        bool isMarkedForDeletion)
    {
        Entry = entry;
        _onSelect = onSelect;
        _onToggleMarked = onToggleMarked;
        NameAText = entry.NameA;
        NameBText = entry.NameB;
        VersusText = AfWarStatsTexts.Versus;
        StatsText = AfWarStatsTexts.CompactHistory(
            entry.WinsA,
            entry.LossesA,
            entry.WinsB,
            entry.LossesB,
            entry.TotalCasualties);
        DurationText = entry.DurationDays <= 0 ? AfWarStatsTexts.Legacy : AfWarStatsTexts.DurationDays(entry.DurationDays);
        IsMarkedForDeletion = isMarkedForDeletion;
        BannerA = new BannerImageIdentifierVM(entry.BannerA, true);
        BannerB = new BannerImageIdentifierVM(entry.BannerB, true);
    }

    public void ExecuteSelect()
    {
        _onSelect?.Invoke(this);
    }

    public void ExecuteToggleMarked()
    {
        IsMarkedForDeletion = !IsMarkedForDeletion;
        _onToggleMarked?.Invoke(this);
    }

    public override void OnFinalize()
    {
        BannerA?.OnFinalize();
        BannerB?.OnFinalize();
        base.OnFinalize();
    }
}
