using AFWarStatsTerminal.Behaviors;
using AFWarStatsTerminal.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsDeathItemVM : ViewModel
{
    private readonly Hero _hero;

    private string _nameText = string.Empty;

    private string _causeText = string.Empty;

    private string _metaText = string.Empty;

    private bool _isEven;

    [DataSourceProperty]
    public string NameText
    {
        get => _nameText;
        set => SetField(ref _nameText, value, nameof(NameText));
    }

    [DataSourceProperty]
    public string CauseText
    {
        get => _causeText;
        set => SetField(ref _causeText, value, nameof(CauseText));
    }

    [DataSourceProperty]
    public string MetaText
    {
        get => _metaText;
        set => SetField(ref _metaText, value, nameof(MetaText));
    }

    [DataSourceProperty]
    public bool IsEven
    {
        get => _isEven;
        set => SetField(ref _isEven, value, nameof(IsEven));
    }

    public AfWarStatsDeathItemVM(AfWarStatsBehavior.HeroDeathEntry entry, bool isEven)
    {
        _hero = AfWarStatsEncyclopedia.ResolveHero(entry?.HeroId);
        NameText = string.IsNullOrWhiteSpace(entry?.HeroName) ? AfWarStatsTexts.UnknownHero : entry.HeroName;
        CauseText = entry == null
            ? AfWarStatsTexts.UnknownCause
            : AfWarStatsTexts.DeathCause(entry.Cause, entry.KillerName);
        MetaText = entry == null
            ? string.Empty
            : AfWarStatsTexts.DeathMeta(entry.DateText, entry.BattleName);
        IsEven = isEven;
    }

    public void ExecuteOpenEncyclopedia()
    {
        AfWarStatsEncyclopedia.OpenHero(_hero);
    }
}
