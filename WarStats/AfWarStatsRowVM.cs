using System;
using AFWarStatsTerminal.Behaviors;
using AFWarStatsTerminal.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsRowVM : ViewModel
{
    private const float WearinessBarTotalWidth = 290f;

    private readonly Kingdom _kingdomA;

    private readonly Kingdom _kingdomB;

    private string _nameAText = string.Empty;

    private string _nameBText = string.Empty;

    private string _versusText = string.Empty;

    private string _killsAText = string.Empty;

    private string _killsBText = string.Empty;

    private string _casualtiesAText = string.Empty;

    private string _casualtiesBText = string.Empty;

    private string _recordAText = string.Empty;

    private string _recordBText = string.Empty;

    private string _wearinessAText = string.Empty;

    private string _wearinessBText = string.Empty;

    private float _wearinessBarWidthA;

    private float _wearinessBarWidthB;

    private string _durationText = string.Empty;

    private string _territoryText = string.Empty;

    private bool _isHighlighted;

    private bool _isEven;

    private BannerImageIdentifierVM _bannerA;

    private BannerImageIdentifierVM _bannerB;

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
    public string KillsAText
    {
        get => _killsAText;
        set => SetField(ref _killsAText, value, nameof(KillsAText));
    }

    [DataSourceProperty]
    public string KillsBText
    {
        get => _killsBText;
        set => SetField(ref _killsBText, value, nameof(KillsBText));
    }

    [DataSourceProperty]
    public string CasualtiesAText
    {
        get => _casualtiesAText;
        set => SetField(ref _casualtiesAText, value, nameof(CasualtiesAText));
    }

    [DataSourceProperty]
    public string CasualtiesBText
    {
        get => _casualtiesBText;
        set => SetField(ref _casualtiesBText, value, nameof(CasualtiesBText));
    }

    [DataSourceProperty]
    public string RecordAText
    {
        get => _recordAText;
        set => SetField(ref _recordAText, value, nameof(RecordAText));
    }

    [DataSourceProperty]
    public string RecordBText
    {
        get => _recordBText;
        set => SetField(ref _recordBText, value, nameof(RecordBText));
    }

    [DataSourceProperty]
    public string WearinessAText
    {
        get => _wearinessAText;
        set => SetField(ref _wearinessAText, value, nameof(WearinessAText));
    }

    [DataSourceProperty]
    public string WearinessBText
    {
        get => _wearinessBText;
        set => SetField(ref _wearinessBText, value, nameof(WearinessBText));
    }

    [DataSourceProperty]
    public float WearinessBarWidthA
    {
        get => _wearinessBarWidthA;
        set => SetField(ref _wearinessBarWidthA, value, nameof(WearinessBarWidthA));
    }

    [DataSourceProperty]
    public float WearinessBarWidthB
    {
        get => _wearinessBarWidthB;
        set => SetField(ref _wearinessBarWidthB, value, nameof(WearinessBarWidthB));
    }

    [DataSourceProperty]
    public string DurationText
    {
        get => _durationText;
        set => SetField(ref _durationText, value, nameof(DurationText));
    }

    [DataSourceProperty]
    public string TerritoryText
    {
        get => _territoryText;
        set => SetField(ref _territoryText, value, nameof(TerritoryText));
    }

    [DataSourceProperty]
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value, nameof(IsHighlighted));
    }

    [DataSourceProperty]
    public bool IsEven
    {
        get => _isEven;
        set => SetField(ref _isEven, value, nameof(IsEven));
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

    public AfWarStatsRowVM(AfWarStatsBehavior.WarEntry entry, bool isEven)
    {
        _kingdomA = entry.KingdomA;
        _kingdomB = entry.KingdomB;
        int wearinessA = Math.Max(0, Math.Min(100, entry.WearinessA));
        int wearinessB = Math.Max(0, Math.Min(100, entry.WearinessB));
        NameAText = entry.NameA;
        NameBText = entry.NameB;
        VersusText = AfWarStatsTexts.Versus;
        KillsAText = AfWarStatsTexts.Kills(entry.KillsA);
        KillsBText = AfWarStatsTexts.Kills(entry.KillsB);
        CasualtiesAText = AfWarStatsTexts.Casualties(entry.CasualtiesA);
        CasualtiesBText = AfWarStatsTexts.Casualties(entry.CasualtiesB);
        RecordAText = AfWarStatsTexts.Record(entry.WinsA, entry.LossesA);
        RecordBText = AfWarStatsTexts.Record(entry.WinsB, entry.LossesB);
        WearinessAText = AfWarStatsTexts.Weariness(wearinessA);
        WearinessBText = AfWarStatsTexts.Weariness(wearinessB);
        WearinessBarWidthA = WearinessBarTotalWidth * wearinessA / 100f;
        WearinessBarWidthB = WearinessBarTotalWidth * wearinessB / 100f;
        DurationText = entry.DurationDays <= 0 ? AfWarStatsTexts.RecentlyDeclared : AfWarStatsTexts.DurationDays(entry.DurationDays);
        TerritoryText = AfWarStatsTexts.Fiefs(entry.TerritoryA, entry.TerritoryB);
        IsHighlighted = entry.InvolvesPlayer;
        IsEven = isEven;
        BannerA = new BannerImageIdentifierVM(entry.BannerA, true);
        BannerB = new BannerImageIdentifierVM(entry.BannerB, true);
    }

    public void ExecuteOpenKingdomA()
    {
        AfWarStatsEncyclopedia.OpenKingdom(_kingdomA);
    }

    public void ExecuteOpenKingdomB()
    {
        AfWarStatsEncyclopedia.OpenKingdom(_kingdomB);
    }

    public override void OnFinalize()
    {
        BannerA?.OnFinalize();
        BannerB?.OnFinalize();
        base.OnFinalize();
    }
}
