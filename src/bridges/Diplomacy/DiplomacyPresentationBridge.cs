using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;
namespace AnimusForge;
internal static class DiplomacyPresentationBridge
{
    internal static bool OpenComposeFromTerminal(Action onClose = null) => DiplomacyModuleServices.World.OpenComposeFromTerminal(onClose);
    internal static bool ShowRoyalAnnouncementArchive(Action onClose = null) => DiplomacyModuleServices.World.ShowRoyalAnnouncementArchive(onClose);
    internal static bool IsOpen => DiplomacyModuleServices.World.ComposePopupOpen;
    internal static string Standing(string kingdomId) => DiplomacyModuleServices.World.Standing(kingdomId);
    internal static long GetRevisionOrZero() => DiplomacyModuleServices.World.QueryTimelineRevision().Revision;
    internal static IReadOnlyList<WorldDiplomacyTimelineDocument> GetRecentDocumentsOrEmpty(int maxCount) => DiplomacyModuleServices.World.QueryTimelineDocuments(maxCount).Documents;
    internal static bool MarkDocumentRead(string id) => DiplomacyModuleServices.World.MarkTimelineRead(id).IsApplied;
}
