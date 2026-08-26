namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Frozen numerical compatibility anchors for the SETS hostile urban-capture flow.
/// The fused runtime must reference these constants instead of redeclaring literals.
/// Changing any value here is a gameplay change and requires explicit re-approval
/// plus standalone snapshot updates (see the SETS urban-capture refactor handoff).
/// </summary>
public static class SetsUrbanCaptureContractProfile
{
    /// <summary>Saved regular-troop follower cap for player-owned settlements.</summary>
    public const int OwnSettlementFollowerLimit = SetsOwnedSettlementMassacreProfile.MaxAlliedAttackers;

    /// <summary>Saved regular-troop follower cap for settlements the player does not own.</summary>
    public const int OtherSettlementFollowerLimit = 10;

    /// <summary>Selected followers spawn in batches of this size.</summary>
    public const int AlliedSpawnBatchSize = 10;

    /// <summary>Delay before the first follower batch spawns after mission start.</summary>
    public const float AlliedSpawnInitialDelaySeconds = 0.75f;

    /// <summary>Delay between consecutive follower spawn batches.</summary>
    public const float AlliedSpawnBatchIntervalSeconds = 0.15f;

    /// <summary>Defender reserve troops spawned per wave.</summary>
    public const int DefenderReserveWaveSize = 30;

    /// <summary>Number of defender reserve phases (garrison, militia, lord parties).</summary>
    public const int DefenderReservePhaseCount = 3;

    /// <summary>Maximum simultaneously active defender reserve waves.</summary>
    public const int MaxActiveDefenderReserveWaves = 4;

    /// <summary>Interval between defender reserve waves.</summary>
    public const float DefenderReserveWaveIntervalSeconds = 30f;

    /// <summary>Town reserve defenders spawned per workshop marker group.</summary>
    public const int DefenderReserveWorkshopSpawnGroupSize = 10;

    /// <summary>Forced mission-end fallback delay after victory if the normal exit stalls.</summary>
    public const float VictoryEndMissionFallbackDelaySeconds = 2f;

    /// <summary>Save key for the owned-settlement follower profile. Never rename without a versioned migration.</summary>
    public const string OwnSettlementProfileSaveKey = "_setsOwnSettlementEntryProfile_v1";

    /// <summary>Save key for the other-settlement follower profile. Never rename without a versioned migration.</summary>
    public const string OtherSettlementProfileSaveKey = "_setsOtherSettlementEntryProfile_v1";

    public const string GarrisonReservePhaseKind = "garrison";

    public const string MilitiaReservePhaseKind = "militia";

    public const string LordPartyReservePhaseKind = "lord_party";

    /// <summary>Owner-hero reserve entries settle inside the lord-party phase.</summary>
    public const string OwnerHeroReserveSourceKind = "owner_hero";

    /// <summary>Fixed defender reserve phase order: garrison, then militia, then lord parties.</summary>
    public static string GetDefenderReservePhaseKind(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0:
                return GarrisonReservePhaseKind;
            case 1:
                return MilitiaReservePhaseKind;
            case 2:
                return LordPartyReservePhaseKind;
            default:
                return null;
        }
    }
}
