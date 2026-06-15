using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free snapshot of the AF give-item/give-gold pool reserved for GCCZ civilian relief.
/// Bannerlord item objects stay in the AF adapter; the core only sees totals.
/// </summary>
public sealed class SiegeSharedReliefPoolFacts
{
    public SiegeSharedReliefPoolFacts(int gold, int foodUnits, int itemTotal, long itemValue)
    {
        Gold = Math.Max(0, gold);
        FoodUnits = Math.Max(0, foodUnits);
        ItemTotal = Math.Max(0, itemTotal);
        ItemValue = Math.Max(0L, itemValue);
    }

    public int Gold { get; }

    public int FoodUnits { get; }

    public int ItemTotal { get; }

    public long ItemValue { get; }
}
