using System;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge;

public partial class MyBehavior
{
    internal static void RetireConversationRequestsForDeveloperClear()
    {
        const string reason = "dev_clear_all_data";
        // Close the old generation before any data reset so queued/late results cannot refill it.
        SaveRuntimeGuard.AdvanceGeneration(reason);
        ShoutBehavior.ResetTransientRuntimeForLoadedSaveExternal(reason);
        CourierDeliveryBehavior.ResetTransientRuntimeForLoadedSaveExternal(reason);
    }

    internal void RetireCampaignRuntime(string reason)
    {
        // Close admission before draining: a concurrent submit may capture the new generation
        // while the old singleton still exists during the remaining cleanup steps.
        Volatile.Write(ref _campaignRuntimeRetired, 1);
        try
        {
            // These pending tasks must settle even if a later legacy reset step fails.
            ResetMemorySummaryMainThreadActions();
            _npcPersonaGeneration.Reset();
            ResetLocalTransientRuntimeForLoadedSave(reason);
        }
        finally
        {
            try { MBInformationManager.OnRemoveMapNotice -= OnMapNoticeRemoved; }
            finally { if (ReferenceEquals(Instance, this)) Instance = null; }
        }
    }
}
