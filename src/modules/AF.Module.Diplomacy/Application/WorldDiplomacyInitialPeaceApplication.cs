using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyInitialPeacePort
{
    bool CampaignAvailable { get; }
    bool Enabled { get; }
    IEnumerable<string> ActiveKingdomIds();
    bool IsAtWar(string first, string second);
    void MakePeace(string first, string second);
    void SanitizeNativeQueue();
    void ClearWarSituationCache();
    int CurrentDay();
    void Log(string message);
}

internal static class WorldDiplomacyInitialPeaceApplication
{
    internal static void Apply<TPort>(WorldDiplomacyStorage storage, ref bool attempted, ref bool nativeQueueSanitized,
        ref TPort port) where TPort : IWorldDiplomacyInitialPeacePort
    {
        if (attempted || !storage.InitialPeacePending || !port.CampaignAvailable || !port.Enabled) return;
        List<string> kingdoms = port.ActiveKingdomIds().OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        if (kingdoms.Count < 2) return;
        attempted = true;
        int day = port.CurrentDay();
        int endedWars = 0;
        for (int firstIndex = 0; firstIndex < kingdoms.Count; firstIndex++)
            for (int secondIndex = firstIndex + 1; secondIndex < kingdoms.Count; secondIndex++)
            {
                string first = kingdoms[firstIndex], second = kingdoms[secondIndex];
                if (!port.IsAtWar(first, second)) continue;
                try
                {
                    port.MakePeace(first, second);
                    storage.LastPeaceDayByPair[WorldDiplomacyRoundLifecycleRules.PairKey(first, second)] = day;
                    WorldDiplomacyWarPressureRules.ClearWarPressure(storage.WarPressure, first, second, port.CurrentDay());
                    WorldDiplomacyWarPressureRules.ClearWarPressure(storage.WarPressure, second, first, port.CurrentDay());
                    endedWars++;
                }
                catch (Exception ex) { port.Log("initial peace failed pair=" + first + "|" + second + " error=" + ex.Message); }
            }
        storage.InitialPeacePending = false;
        storage.InitialPeaceApplied = true;
        storage.ActiveWarLedgers.Clear();
        storage.NativeSignals.Clear();
        port.SanitizeNativeQueue();
        storage.NativeSignals.Clear();
        storage.WarPressure.Clear();
        nativeQueueSanitized = true;
        port.ClearWarSituationCache();
        port.Log("new-game initial peace applied endedWars=" + endedWars.ToString(CultureInfo.InvariantCulture));
    }
}
