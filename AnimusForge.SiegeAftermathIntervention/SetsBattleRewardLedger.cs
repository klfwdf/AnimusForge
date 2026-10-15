using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public enum SetsBattleRewardScreen
{
    Wait = 0,
    Prisoners = 1,
    Items = 2,
    Complete = 3,
}

/// <summary>Owns casualty deduplication and ordered SETS reward presentation, not game objects.</summary>
public sealed class SetsBattleRewardLedger
{
    private readonly HashSet<int> _casualties = new HashSet<int>();

    public bool PrisonersPresented { get; private set; }
    public bool ItemsPresented { get; private set; }

    public bool TryRecordCasualty(int agentIndex, bool hostileReserve, bool isHero, bool defeated)
    {
        return agentIndex >= 0 && hostileReserve && !isHero && defeated && _casualties.Add(agentIndex);
    }

    public SetsBattleRewardScreen ResolveNextScreen(bool mapReady, bool hasPrisoners, bool hasItems)
    {
        if (!mapReady)
        {
            return SetsBattleRewardScreen.Wait;
        }
        if (hasPrisoners && !PrisonersPresented)
        {
            return SetsBattleRewardScreen.Prisoners;
        }
        if (hasItems && !ItemsPresented)
        {
            return SetsBattleRewardScreen.Items;
        }
        return SetsBattleRewardScreen.Complete;
    }

    public void MarkPresented(SetsBattleRewardScreen screen)
    {
        if (screen == SetsBattleRewardScreen.Prisoners)
        {
            PrisonersPresented = true;
        }
        else if (screen == SetsBattleRewardScreen.Items)
        {
            ItemsPresented = true;
        }
    }

    public void RestorePresentation(bool prisonersPresented, bool itemsPresented)
    {
        PrisonersPresented = prisonersPresented;
        ItemsPresented = itemsPresented;
    }
}
