namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Pure post-mission completion plan for one SETS capture operation.
/// The runtime applies each true step through the existing effect adapters,
/// consulting the ledger so every step runs at most once.
/// </summary>
public sealed class SetsUrbanCaptureCompletionPlan
{
    public static readonly SetsUrbanCaptureCompletionPlan DoNothing = new SetsUrbanCaptureCompletionPlan(
        transferOwnership: false,
        openNativeMenu: false,
        openOwnedIncidentMenu: false,
        grantVillageReward: false,
        rejectionReason: "no_pending_capture");

    private SetsUrbanCaptureCompletionPlan(
        bool transferOwnership,
        bool openNativeMenu,
        bool openOwnedIncidentMenu,
        bool grantVillageReward,
        string rejectionReason)
    {
        TransferOwnership = transferOwnership;
        OpenNativeMenu = openNativeMenu;
        OpenOwnedIncidentMenu = openOwnedIncidentMenu;
        GrantVillageReward = grantVillageReward;
        RejectionReason = rejectionReason ?? "";
    }

    /// <summary>Transfer settlement ownership to the player clan (hostile town/castle only).</summary>
    public bool TransferOwnership { get; }

    /// <summary>Open the native settlement-taken menu (menu_settlement_taken).</summary>
    public bool OpenNativeMenu { get; }

    /// <summary>Open the SETS owned/attached incident menu; ownership untouched.</summary>
    public bool OpenOwnedIncidentMenu { get; }

    /// <summary>Grant the village militia-victory loot reward.</summary>
    public bool GrantVillageReward { get; }

    /// <summary>Why the plan is empty, for diagnostics. Empty string when the plan has work.</summary>
    public string RejectionReason { get; }

    public bool HasWork
    {
        get { return TransferOwnership || OpenNativeMenu || OpenOwnedIncidentMenu || GrantVillageReward; }
    }

    /// <summary>
    /// Derive the completion plan from context, state, and ledger.
    /// Fail closed: an invalid or mismatched operation produces DoNothing rather
    /// than guessing, and an owned/attached incident can never produce a transfer.
    /// </summary>
    public static SetsUrbanCaptureCompletionPlan Resolve(
        SetsUrbanCaptureContext context,
        SetsUrbanCaptureState state,
        SetsUrbanCaptureLedger ledger)
    {
        if (context == null || ledger == null || !context.IsValid)
        {
            return Rejected("invalid_context");
        }

        if (state != SetsUrbanCaptureState.AwaitingMap && state != SetsUrbanCaptureState.OwnershipCommitted)
        {
            return Rejected("not_awaiting_completion");
        }

        if (ledger.CompletionCommitted)
        {
            return Rejected("already_completed");
        }

        if (context.IsOwnedOrAttachedIncident)
        {
            if (ledger.MenuCommitted)
            {
                return Rejected("owned_incident_menu_already_opened");
            }

            return new SetsUrbanCaptureCompletionPlan(
                transferOwnership: false,
                openNativeMenu: false,
                openOwnedIncidentMenu: true,
                grantVillageReward: false,
                rejectionReason: "");
        }

        if (!context.IsHostileCapture)
        {
            return Rejected("unknown_ownership_classification");
        }

        if (!ledger.VictoryCommitted)
        {
            return Rejected("victory_not_committed");
        }

        if (SetsSettlementEntryProfile.UsesVillageLootResolution(context.SceneKind))
        {
            if (ledger.VillageRewardCommitted)
            {
                return Rejected("village_reward_already_granted");
            }

            return new SetsUrbanCaptureCompletionPlan(
                transferOwnership: false,
                openNativeMenu: false,
                openOwnedIncidentMenu: false,
                grantVillageReward: true,
                rejectionReason: "");
        }

        if (!SetsSettlementEntryProfile.UsesNativeSiegeVictoryMenu(context.SceneKind))
        {
            return Rejected("unsupported_scene_kind");
        }

        bool needOwnership = !ledger.OwnershipCommitted;
        bool needMenu = !ledger.MenuCommitted;
        if (!needOwnership && !needMenu)
        {
            return Rejected("ownership_and_menu_already_committed");
        }

        // A retry after a committed ownership transfer must only open the menu.
        return new SetsUrbanCaptureCompletionPlan(
            transferOwnership: needOwnership,
            openNativeMenu: needMenu,
            openOwnedIncidentMenu: false,
            grantVillageReward: false,
            rejectionReason: "");
    }

    private static SetsUrbanCaptureCompletionPlan Rejected(string reason)
    {
        return new SetsUrbanCaptureCompletionPlan(
            transferOwnership: false,
            openNativeMenu: false,
            openOwnedIncidentMenu: false,
            grantVillageReward: false,
            rejectionReason: reason);
    }
}
