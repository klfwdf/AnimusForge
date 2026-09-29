using System.Linq;
using TaleWorlds.Library;
using AnimusForge.Refactor.Modules;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsFactionCardVM : ViewModel
{
    private readonly CivilWarPanelFaction _faction;

    internal AfWarStatsFactionCardVM(CivilWarPanelFaction faction) => _faction = faction ?? new CivilWarPanelFaction();

    [DataSourceProperty]
    public string Name => _faction.Name;

    [DataSourceProperty]
    public string Demand => _faction.Satisfied ? "诉求已满足" : _faction.Demand;

    [DataSourceProperty]
    public string Members => string.IsNullOrWhiteSpace(_faction.Members) ? "暂无成员" : _faction.Members;

    [DataSourceProperty]
    public string Color => _faction.Color;

    [DataSourceProperty]
    public int BarWidth => _faction.GrievanceBar;
}

public sealed class AfWarStatsFactionKingdomVM : ViewModel
{
    private readonly CivilWarPanelKingdom _kingdom;
    private readonly MBBindingList<AfWarStatsFactionCardVM> _factions;

    internal AfWarStatsFactionKingdomVM(CivilWarPanelKingdom kingdom)
    {
        _kingdom = kingdom ?? new CivilWarPanelKingdom();
        _factions = new MBBindingList<AfWarStatsFactionCardVM>();
        foreach (CivilWarPanelFaction faction in _kingdom.Factions ?? Enumerable.Empty<CivilWarPanelFaction>()) _factions.Add(new AfWarStatsFactionCardVM(faction));
    }

    [DataSourceProperty]
    public string Name => _kingdom.Name;

    [DataSourceProperty]
    public string Stage => _kingdom.Stage;

    [DataSourceProperty]
    public string GrievanceText => _kingdom.GrievanceText;

    [DataSourceProperty]
    public int BarWidth => _kingdom.GrievanceBar;

    [DataSourceProperty]
    public MBBindingList<AfWarStatsFactionCardVM> Factions => _factions;
}
