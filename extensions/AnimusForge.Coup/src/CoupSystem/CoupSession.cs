using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.CoupSystem;

internal enum CoupPhase { Preparing, Street, HallSelection, Hall, AwaitingResolution, Completed, Failed, Suspended }
internal enum CoupTroopRole { Ally, StreetDefender, GateGuard, HallGuard }
internal enum CoupKingDisposition { Undecided, Release, Capture }

// Missing in legacy sessions means the original one-soldier admission rule.
internal sealed class CoupEntryRequirements
{
    public int MinimumClanTier;
    public int MinimumInfluence;
    public int MinimumTroops = 1;

    internal static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(value, max));
    internal static CoupEntryRequirements Normalize(int tier, int influence, int troops) => new CoupEntryRequirements
    {
        MinimumClanTier = Clamp(tier, 0, 6), MinimumInfluence = Clamp(influence, 0, 5000), MinimumTroops = Clamp(troops, 1, 120)
    };
    internal bool IsValid() => MinimumClanTier >= 0 && MinimumClanTier <= 6
        && MinimumInfluence >= 0 && MinimumInfluence <= 5000 && MinimumTroops >= 1 && MinimumTroops <= 120;

    internal bool Check(int tier, float influence, int healthyRegulars, int streetLimit, out string reason)
        => Evaluate(MinimumClanTier, MinimumInfluence, MinimumTroops, tier, influence, healthyRegulars, streetLimit, out reason);

    // Menu refresh uses primitive values rather than allocating a settings snapshot.
    internal static bool Evaluate(int minimumTier, int minimumInfluence, int minimumTroops,
        int tier, float influence, int healthyRegulars, int streetLimit, out string reason)
    {
        reason = "";
        if (minimumTroops > streetLimit)
            reason = "篡位配置冲突：最低突击队人数" + minimumTroops + "超过街道上限" + streetLimit + "，请在 MCM 中调整。";
        else if (tier < minimumTier)
            reason = "家族等级不足：" + tier + "／" + minimumTier + "级。";
        else if (minimumInfluence > 0 && (float.IsNaN(influence) || float.IsInfinity(influence) || influence < minimumInfluence))
            reason = "影响力不足：" + influence.ToString("0.##") + "／" + minimumInfluence + "（仅检查，不扣除）。";
        else if (healthyRegulars < minimumTroops + 1)
            reason = "至少需要" + (minimumTroops + 1) + "名健康普通士兵，其中1人留守接应；当前" + healthyRegulars + "人。";
        return reason.Length == 0;
    }
}

// Detached values captured once at Begin; never consult MCM while a coup is in flight.
// No field initializers here: an explicitly stored but incomplete snapshot is invalid.
internal sealed class CoupBattleOptions
{
    public int StreetAllyLimit;
    public int HallAllyLimit;
    public int GateGuardLimit;
    public int HallGuardLimit;
    public int DefenderWaveSize;
    public int DefenderWaveIntervalSeconds;
    public int MaxActiveDefenderWaves;

    internal static CoupBattleOptions LegacyDefaults() => Normalize(60, 20, 10, 20, 30, 30, 4);

    internal static CoupBattleOptions Normalize(int street, int hall, int gate, int guards, int wave, int interval, int active)
        => new CoupBattleOptions
        {
            StreetAllyLimit = Clamp(street, 1, 120), HallAllyLimit = Clamp(hall, 1, 40),
            GateGuardLimit = Clamp(gate, 1, 30), HallGuardLimit = Clamp(guards, 1, 40),
            DefenderWaveSize = Clamp(wave, 1, 60), DefenderWaveIntervalSeconds = Clamp(interval, 5, 120),
            MaxActiveDefenderWaves = Clamp(active, 1, 4)
        };

    internal bool IsValid() => InRange(StreetAllyLimit, 1, 120) && InRange(HallAllyLimit, 1, 40)
        && InRange(GateGuardLimit, 1, 30) && InRange(HallGuardLimit, 1, 40)
        && InRange(DefenderWaveSize, 1, 60) && InRange(DefenderWaveIntervalSeconds, 5, 120)
        && InRange(MaxActiveDefenderWaves, 1, 4);

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(value, max));
    private static bool InRange(int value, int min, int max) => value >= min && value <= max;
}

// Only stable campaign identifiers and values are persisted. Agents belong to one mission.
internal sealed class CoupTroopRecord
{
    public string Id;
    public string CharacterId;
    public string SourcePartyId;
    public CoupTroopRole Role;
    public float Health = -1f;
    public bool Removed;
    public bool Killed;
    public bool Wounded;
    public bool CasualtyCommitted;
    public bool HallSelected;

    internal bool TryRecordCasualty(bool killed)
    {
        if (Removed) return false;
        Removed = true;
        Killed = killed;
        Wounded = !killed;
        Health = 0f;
        return true;
    }
}

internal sealed class CoupSession
{
    public const int StreetAllyLimit = 60;
    public const int StreetDefenderLimit = 60;
    public const int HallAllyLimit = 20;
    public const int HallGuardLimit = 20;
    public const int GateGuardLimit = 10;
    // Missing in old JSON: initialize to the historical rules, independent of current MCM.
    [Newtonsoft.Json.JsonProperty(ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace)]
    public CoupBattleOptions BattleOptions = CoupBattleOptions.LegacyDefaults();
    [Newtonsoft.Json.JsonProperty(ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace)]
    public CoupEntryRequirements EntryRequirements = new CoupEntryRequirements();
    public string Id = Guid.NewGuid().ToString("N");
    public string SettlementId;
    public string KingdomId;
    public string KingId;
    public string OriginalRulingClanId;
    public string OriginalOwnerClanId;
    public CoupPhase Phase;
    public CoupKingDisposition Disposition;
    public float PlayerHealth;
    public float KingHealth;
    public bool Started;
    public bool KingSubdued;
    public bool CasualtiesCommitted;
    public bool RulingClanCommitted;
    public bool TownCommitted;
    public bool CustodyCommitted;
    public bool FactsCommitted;
    public bool RebellionQueued;
    public bool DefectionCommitted;
    public bool WithdrawalCommitted;
    public bool PoliticalCommitStarted;
    public CoupPhase ResumePhase;
    public string FailureReason;
    public List<CoupTroopRecord> Troops = new List<CoupTroopRecord>();

    public bool SceneEntered;
    // Optional JSON fields: old sessions retain only the progress their existing facts prove.
    public bool GateBreached;
    public bool HallEntered;

    public bool IsTerminal => Phase == CoupPhase.Completed || Phase == CoupPhase.Failed || Phase == CoupPhase.Suspended;
    public bool HasPoliticalCommit => PoliticalCommitStarted || RulingClanCommitted || TownCommitted || CustodyCommitted || DefectionCommitted;
    public bool IsCombatPhase => Phase == CoupPhase.Street || Phase == CoupPhase.Hall;
    // A suspended victory or failure must be retried to completion; it never settles by itself.
    public bool IsResumable => Phase == CoupPhase.Suspended
        && (HasPoliticalCommit || ResumePhase == CoupPhase.AwaitingResolution || ResumePhase == CoupPhase.Failed);
    public bool IsSettled => Phase == CoupPhase.Completed
        || (Phase == CoupPhase.Failed && DefectionCommitted && FactsCommitted && WithdrawalCommitted)
        || (Phase == CoupPhase.Suspended && CasualtiesCommitted && !IsResumable);
    // Street holds gate guards before the door; the hall holds only its own guard cap.
    public bool IsGateCleared => !Troops.Any(t => t.Role == CoupTroopRole.GateGuard && !t.Removed);
    public string SceneLocationId => Phase == CoupPhase.Hall ? "lordshall" : "center";

    // The single authority on which defenders a scene spawns; the host SETS copies this list.
    internal List<CoupTroopRecord> PendingDefenders(bool hall) => Troops.Where(t => !t.Removed
        && (hall ? t.Role == CoupTroopRole.HallGuard : t.Role == CoupTroopRole.GateGuard || t.Role == CoupTroopRole.StreetDefender)).ToList();

    internal bool TryAdvance(CoupPhase expected, CoupPhase next)
    {
        if (Phase != expected) return false;
        bool legal = (expected == CoupPhase.Street && next == CoupPhase.HallSelection)
            || (expected == CoupPhase.Hall && next == CoupPhase.AwaitingResolution && KingSubdued && Started);
        if (!legal) return false;
        Phase = next;
        return true;
    }

    internal bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(SettlementId)
            || string.IsNullOrWhiteSpace(KingdomId) || string.IsNullOrWhiteSpace(KingId)
            || string.IsNullOrWhiteSpace(OriginalRulingClanId) || string.IsNullOrWhiteSpace(OriginalOwnerClanId)
            || !Enum.IsDefined(typeof(CoupPhase), Phase) || !Enum.IsDefined(typeof(CoupPhase), ResumePhase) || !Enum.IsDefined(typeof(CoupKingDisposition), Disposition)
            || BattleOptions == null || !BattleOptions.IsValid()
            || EntryRequirements == null || !EntryRequirements.IsValid()
            || Troops == null || float.IsNaN(PlayerHealth) || float.IsInfinity(PlayerHealth)
            || float.IsNaN(KingHealth) || float.IsInfinity(KingHealth)) return false;
        if ((Phase == CoupPhase.AwaitingResolution || Phase == CoupPhase.Completed || HasPoliticalCommit)
            && Phase != CoupPhase.Failed && ResumePhase != CoupPhase.Failed && (!Started || !KingSubdued)) return false;
        if (Phase == CoupPhase.Completed && (!CasualtiesCommitted || !RulingClanCommitted || !TownCommitted
            || !CustodyCommitted || !FactsCommitted || !RebellionQueued || Disposition == CoupKingDisposition.Undecided)) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        int allies = 0, gate = 0, guards = 0, hall = 0;
        foreach (var troop in Troops)
        {
            if (troop == null || string.IsNullOrEmpty(troop.Id) || !ids.Add(troop.Id)
                || string.IsNullOrEmpty(troop.CharacterId) || string.IsNullOrEmpty(troop.SourcePartyId)
                || !Enum.IsDefined(typeof(CoupTroopRole), troop.Role) || float.IsNaN(troop.Health) || float.IsInfinity(troop.Health)
                || (troop.Killed && troop.Wounded) || ((troop.Killed || troop.Wounded) && !troop.Removed)
                || (troop.HallSelected && troop.Role != CoupTroopRole.Ally)) return false;
            if (troop.Role == CoupTroopRole.Ally) allies++;
            if (troop.Role == CoupTroopRole.GateGuard) gate++;
            if (troop.Role == CoupTroopRole.HallGuard) guards++;
            if (troop.HallSelected) hall++;
        }
        return allies <= BattleOptions.StreetAllyLimit && gate <= BattleOptions.GateGuardLimit
            && guards <= BattleOptions.HallGuardLimit && hall <= BattleOptions.HallAllyLimit;
    }
}
