using System;
using System.Text.Json;
using AnimusForge.CoupSystem;

internal static class Program
{
    private static int _assertions;
    private static void Assert(bool result, string message)
    {
        if (!result) throw new Exception(message);
        _assertions++;
    }
    private static CoupSession NewSession() => new CoupSession
    {
        SettlementId = "town", KingdomId = "kingdom", KingId = "king", OriginalOwnerClanId = "owner",
        OriginalRulingClanId = "old_ruler", PlayerHealth = 73, KingHealth = 55, Started = true, Phase = CoupPhase.Street,
        Disposition = CoupKingDisposition.Undecided, CasualtiesCommitted = false, TownCommitted = false,
        CustodyCommitted = false, FactsCommitted = false, RebellionQueued = false, WithdrawalCommitted = false, FailureReason = ""
    };
    private static CoupTroopRecord Troop(string id, CoupTroopRole role = CoupTroopRole.Ally) => new CoupTroopRecord
    { Id = id, CharacterId = "soldier", SourcePartyId = "party", Role = role, Health = 44 };

    private static void Main()
    {
        var session = NewSession();
        session.Troops.Add(Troop("1"));
        Assert(session.IsValid(), "valid mission snapshot");
        Assert(!session.TryAdvance(CoupPhase.Street, CoupPhase.AwaitingResolution), "street cannot award victory");
        Assert(session.TryAdvance(CoupPhase.Street, CoupPhase.HallSelection), "street reaches hall selection");
        Assert(!session.TryAdvance(CoupPhase.Street, CoupPhase.HallSelection), "duplicate street completion rejected");
        session.Phase = CoupPhase.Hall;
        Assert(!session.TryAdvance(CoupPhase.Hall, CoupPhase.AwaitingResolution), "king must be subdued");
        session.KingSubdued = true;
        Assert(session.TryAdvance(CoupPhase.Hall, CoupPhase.AwaitingResolution), "verified hall victory");
        Assert(!session.TryAdvance(CoupPhase.Hall, CoupPhase.AwaitingResolution), "duplicate victory rejected");
        session.KingSubdued = false;
        Assert(!session.IsValid(), "forged saved victory rejected");
        session.KingSubdued = true;

        var wounded = session.Troops[0];
        Assert(wounded.TryRecordCasualty(false), "first knockout recorded");
        Assert(!wounded.TryRecordCasualty(true), "later callback cannot kill a knocked out soldier");
        Assert(wounded.Wounded && !wounded.Killed && wounded.Removed, "knockout remains wound");
        var killed = Troop("2");
        Assert(killed.TryRecordCasualty(true), "first death recorded");
        Assert(!killed.TryRecordCasualty(false) && killed.Killed && !killed.Wounded, "death never rewritten by cleanup");
        session.Troops.Add(killed);
        wounded.CasualtyCommitted = true;
        session.RulingClanCommitted = session.PoliticalCommitStarted = true;
        session.ResumePhase = CoupPhase.AwaitingResolution;
        session.Phase = CoupPhase.Suspended;
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<CoupSession>(JsonSerializer.Serialize(session, options), options);
        Assert(restored.IsValid() && restored.HasPoliticalCommit, "partial political commit survives save roundtrip");
        Assert(restored.Troops[0].CasualtyCommitted && !restored.Troops[1].CasualtyCommitted, "per-soldier commit receipt survives");
        Assert(!restored.Troops[0].TryRecordCasualty(true), "reload does not reopen casualty");
        Assert(restored.PlayerHealth == 73 && restored.KingHealth == 55, "hero health survives transition snapshot");

        session = NewSession();
        session.Troops.Add(Troop("same")); session.Troops.Add(Troop("same"));
        Assert(!session.IsValid(), "duplicate soldier identifiers rejected");
        session.Troops.Clear();
        for (int i = 0; i < 61; i++) session.Troops.Add(Troop(i.ToString()));
        Assert(!session.IsValid(), "street over-cap rejected");
        session.Troops.RemoveAt(60);
        Assert(session.IsValid(), "street cap accepted");
        for (int i = 0; i < 21; i++) session.Troops[i].HallSelected = true;
        Assert(!session.IsValid(), "hall over-cap rejected");
        session.Troops[20].HallSelected = false;
        Assert(session.IsValid(), "hall cap accepted");
        session.Troops[0].Health = float.NaN;
        Assert(!session.IsValid(), "corrupt health rejected");
        session = NewSession(); session.Phase = CoupPhase.Failed; session.DefectionCommitted = true;
        Assert(session.IsValid(), "defection failure is not a forged victory");
        session.Phase = CoupPhase.Suspended; session.ResumePhase = CoupPhase.Failed;
        Assert(session.IsValid(), "failed transaction can resume after suspension");
        session.ResumePhase = (CoupPhase)999;
        Assert(!session.IsValid(), "unknown resume phase rejected");
        session = NewSession(); session.Phase = CoupPhase.Hall; session.Started = false; session.KingSubdued = true;
        Assert(!session.TryAdvance(CoupPhase.Hall, CoupPhase.AwaitingResolution), "unstarted hall cannot award crown");
        session = NewSession(); session.Phase = CoupPhase.Completed; session.KingSubdued = true;
        Assert(!session.IsValid(), "completed save requires all effect receipts");
        session.CasualtiesCommitted = session.RulingClanCommitted = session.TownCommitted = session.CustodyCommitted = session.FactsCommitted = session.RebellionQueued = true;
        session.Disposition = CoupKingDisposition.Release;
        Assert(session.IsValid(), "complete released outcome is valid");
        session.Disposition = CoupKingDisposition.Capture;
        Assert(session.IsValid(), "complete captured outcome is valid");
        session = NewSession(); session.Troops.Add(Troop("guard", CoupTroopRole.GateGuard)); session.Troops[0].HallSelected = true;
        Assert(!session.IsValid(), "cannot select a defender into the hall assault team");
        session.Troops[0].HallSelected = false;
        session.Troops[0].Wounded = true;
        Assert(!session.IsValid(), "wounded combatant must be removed from active pool");
        // Defender roles have a single owner: the session record list the host spawns from.
        session = NewSession();
        session.Troops.Add(Troop("a"));
        session.Troops.Add(Troop("g", CoupTroopRole.GateGuard));
        session.Troops.Add(Troop("s", CoupTroopRole.StreetDefender));
        session.Troops.Add(Troop("h", CoupTroopRole.HallGuard));
        Assert(session.PendingDefenders(false).Count == 2 && session.PendingDefenders(true).Count == 1, "street and hall spawn disjoint defender sets");
        Assert(!session.IsGateCleared, "unspawned gate guard keeps the door shut");
        session.Troops[1].TryRecordCasualty(true);
        Assert(session.IsGateCleared && session.PendingDefenders(false).Count == 1, "fallen gate guard opens door and is not respawned");
        Assert(session.SceneLocationId == "center", "street arms town center");
        session.Phase = CoupPhase.Hall;
        Assert(session.SceneLocationId == "lordshall", "hall arms lord's hall only");

        // A suspended victory must resume, never settle neutrally.
        session = NewSession(); session.KingSubdued = true; session.CasualtiesCommitted = true;
        session.ResumePhase = CoupPhase.AwaitingResolution; session.Phase = CoupPhase.Suspended;
        Assert(session.IsResumable && !session.IsSettled, "suspended victory stays pending until retried");
        session.ResumePhase = CoupPhase.Street;
        Assert(!session.IsResumable && session.IsSettled, "mid-fight technical stop settles after casualties");
        session.CasualtiesCommitted = false;
        Assert(!session.IsSettled, "technical stop waits for casualty commit");
        Console.WriteLine("Coup contracts: " + _assertions + " PASS");
    }
}
