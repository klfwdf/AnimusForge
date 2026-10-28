using System;

namespace AnimusForge.Refactor.Persistence;

internal static class WorldDiplomacyNotificationStateMigration
{
    internal const int CurrentSchemaVersion = 2;

    public static void Migrate(WorldDiplomacyStorage storage)
    {
        if (storage == null) throw new ArgumentNullException(nameof(storage));
        if (storage.DiplomacyNotificationStateSchemaVersion >= CurrentSchemaVersion) return;

        foreach (WorldDiplomacyDocument document in storage.Documents)
        {
            if (document == null) continue;
            // On upgrade, old published declarations remain archived without
            // replaying notices or starting a new silence deadline.
            if (document.IsReadyForPublication || document.IsPlayerAuthored)
            {
                document.LegacyNoticeSuppressed = true;
                document.RumorNotified = true;
                document.FormalNoticeShown = true;
            }
        }

        storage.DiplomacyNotificationStateSchemaVersion = CurrentSchemaVersion;
    }
}
