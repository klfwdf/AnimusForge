using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Same-DLL event-owner seam. Does not route through document propagation or execute effects.
internal static class DiplomacyEventPresentationBridge
{
    internal static bool IsOpen => DiplomacyEventPopup.IsOpen;
    internal static bool TryShow(DiplomacyEventPresentation data, out string imageId,
        Action acknowledged = null, IReadOnlyList<string> recentImageIds = null)
        => DiplomacyEventPopup.TryShow(data, out imageId, acknowledged, recentImageIds);
}
