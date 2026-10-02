using System;

namespace AnimusForge.Refactor.Persistence;

internal static class WorldDiplomacyNotificationStateMigration
{
    internal const int CurrentSchemaVersion = 1;

    public static void Migrate(WorldDiplomacyStorage storage)
    {
        if (storage == null) throw new ArgumentNullException(nameof(storage));
        if (storage.DiplomacyNotificationStateSchemaVersion >= CurrentSchemaVersion) return;

        foreach (WorldDiplomacyDocument document in storage.Documents)
        {
            if (document == null) continue;
            if (document.IsPlayerAuthored)
            {
                document.RumorNotified = true;
                document.FormalNoticeShown = true;
                continue;
            }
            if (document.IsReadyForPublication) document.RumorNotified = true;
            if (document.IsNotified || document.IsRead || document.IsCompressed)
            {
                document.FormalNoticeShown = true;
            }
        }

        storage.DiplomacyNotificationStateSchemaVersion = CurrentSchemaVersion;
    }
}
