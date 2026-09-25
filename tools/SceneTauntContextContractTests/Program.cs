using AnimusForge.Refactor.Modules;

static void Check(bool expected, ScenePeaceConflictContext facts, string label)
{
    bool actual = ScenePeaceConflictContextOwner.CanInitialize(in facts);
    if (actual != expected) throw new InvalidOperationException(label + ": expected=" + expected + " actual=" + actual);
    Console.WriteLine("PASS " + label);
}

static ScenePeaceConflictContext Facts(
    bool mission = true, bool settlement = true, bool encounter = true,
    bool location = true, bool sameSettlement = true, bool battle = false,
    bool siegeHandler = false, bool battleTeam = false, bool battleMode = false,
    bool underSiege = false, string locationId = "center")
    => new ScenePeaceConflictContext(mission, settlement, encounter, location,
        sameSettlement, battle, siegeHandler, battleTeam, battleMode, underSiege, locationId);

Check(true, Facts(), "peace center allowed");
Check(true, Facts(locationId: "village_center"), "village center allowed");
Check(true, Facts(locationId: "lordshall"), "lord hall allowed");
Check(true, Facts(locationId: "tavern"), "tavern allowed");
Check(true, Facts(locationId: "alley"), "native alley allowed");
Check(true, Facts(locationId: "prison"), "dungeon allowed");
Check(true, Facts(locationId: "port"), "port allowed");
Check(false, Facts(mission: false), "missing Mission denied");
Check(false, Facts(settlement: false), "missing settlement denied");
Check(false, Facts(encounter: false), "missing LocationEncounter denied");
Check(false, Facts(location: false), "missing Campaign location denied");
Check(false, Facts(sameSettlement: false), "mismatched settlement denied");
Check(false, Facts(battle: true), "active map/encounter battle denied");
Check(false, Facts(siegeHandler: true), "siege handler denied");
Check(false, Facts(battleTeam: true), "siege/sally/field team type denied");
Check(false, Facts(battleMode: true), "deployment/stealth/duel mode denied");
Check(false, Facts(underSiege: true), "besieged settlement denied");
Check(false, Facts(locationId: "arena"), "arena denied");
Check(false, Facts(locationId: "TRAINING_FIELD"), "training field denied");
Check(false, Facts(locationId: " "), "unknown location denied");
Console.WriteLine("20/20 scene Taunt context cases passed; game Mission order NOT_RUN");

static void LedgerCheck(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException("penalty ledger: " + label);
    Console.WriteLine("PASS " + label);
}

var ledger = new SceneTauntPenaltyLedgerOwner();
ledger.RestoreDeferredCrime(new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
{
    [" Faction-A "] = 7f, [" "] = 9f, ["Faction-B"] = -1f
});
LedgerCheck(ledger.HasDeferredCrime && ledger.GetDeferredCrime("faction-a") == 7f,
    "deferred crime save restored by stable faction id");
LedgerCheck(ledger.CaptureDeferredCrime().Count == 1, "invalid saved crime filtered");
LedgerCheck(ledger.QueueDeferredCrime("FACTION-A", 6f) == 13f,
    "repeated faction crime accumulates case-insensitively");
LedgerCheck(ledger.QueueDeferredCrime("", 6f) == 0f && ledger.GetDeferredCrime("faction-a") == 13f,
    "missing faction does not mutate ledger");
LedgerCheck(ledger.PendingCrimeEntries().Length == 1, "pending iteration is bounded to recorded factions");
LedgerCheck(ledger.ReserveNativeCommit("faction-a", 100f, 100f) == 0f
    && ledger.GetDeferredCrime("faction-a") == 13f, "native cap preserves pending crime");
LedgerCheck(ledger.ReserveNativeCommit("faction-a", 95f, 100f) == 5f
    && ledger.GetDeferredCrime("faction-a") == 8f, "partial native commit retains remainder");
ledger.RestoreFailedNativeCommit("faction-a", 13f);
LedgerCheck(ledger.GetDeferredCrime("faction-a") == 13f, "failed native action restores pre-commit pool");
LedgerCheck(ledger.ReserveNativeCommit("faction-a", 0f, 100f) == 13f
    && !ledger.HasDeferredCrime, "full native commit consumes pool once");
ledger.QueueDeferredCrime("faction-a", 2f);
LedgerCheck(ledger.ClearDeferredCrime("FACTION-A") == 2f
    && ledger.ClearDeferredCrime("faction-a") == 0f, "execution clear is idempotent");

ledger.RestoreTrustTenths(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{ [" TOWN-A "] = 4, ["TOWN-B"] = -3 });
LedgerCheck(ledger.CaptureTrustTenths().Count == 1, "trust save normalizes legacy entries");
LedgerCheck(ledger.AwardCriminalKnockdownTrust("town-a", out int carry) == 1 && carry == 7,
    "knockdown grants whole trust and carries seven tenths");
LedgerCheck(ledger.AwardCriminalKnockdownTrust("TOWN-A", out carry) == 2 && carry == 0,
    "second knockdown consumes fractional carry exactly once");
LedgerCheck(ledger.CaptureTrustTenths().Count == 0, "zero trust carry is not persisted");
LedgerCheck(ledger.AwardCriminalKnockdownTrust("", out carry) == 0 && carry == 0,
    "missing settlement cannot earn trust");
ledger.QueueDeferredCrime("faction-a", 3f);
ledger.ClearForMainHeroDeath();
LedgerCheck(!ledger.HasDeferredCrime, "main hero death clears pending crime");
Console.WriteLine("16/16 scene Taunt penalty ledger cases passed; Bannerlord native crime/trust callbacks NOT_RUN");

static void LifecycleCheck(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException("conflict lifecycle: " + label);
    Console.WriteLine("PASS " + label);
}

var lifecycle = new SceneTauntConflictLifecycleOwner();
LifecycleCheck(!lifecycle.Active && !lifecycle.Armed && !lifecycle.ArmedOccurred,
    "Mission lifecycle starts idle");
LifecycleCheck(!lifecycle.TryEscalate(), "idle Mission cannot escalate");
LifecycleCheck(lifecycle.TryBeginUnarmed() && lifecycle.Active && !lifecycle.Armed,
    "valid peace conflict begins unarmed");
LifecycleCheck(!lifecycle.TryBeginUnarmed() && !lifecycle.TryBeginArmedCarryover(),
    "active conflict rejects duplicate initialization");
LifecycleCheck(lifecycle.TryEscalate() && lifecycle.Armed && lifecycle.ArmedOccurred,
    "unarmed conflict escalates once");
LifecycleCheck(!lifecycle.TryEscalate(), "armed conflict rejects repeat escalation");
lifecycle.End(preserveArmedDefeatState: true);
LifecycleCheck(!lifecycle.Active && !lifecycle.Armed && lifecycle.ArmedOccurred,
    "end preserves armed defeat decision when requested");
LifecycleCheck(lifecycle.TryBeginUnarmed() && !lifecycle.ArmedOccurred,
    "new conflict resets stale armed outcome");
lifecycle.End(preserveArmedDefeatState: false);
LifecycleCheck(!lifecycle.Active && !lifecycle.ArmedOccurred,
    "ordinary end clears all conflict flags");
LifecycleCheck(lifecycle.TryBeginArmedCarryover() && lifecycle.Active && lifecycle.Armed,
    "valid carryover starts armed");
LifecycleCheck(!lifecycle.TryBeginArmedCarryover(), "carryover cannot start twice");
lifecycle.End(preserveArmedDefeatState: false);
LifecycleCheck(!lifecycle.Active && !lifecycle.Armed && !lifecycle.ArmedOccurred,
    "carryover cleanup restores idle state");
lifecycle.MarkExternalArmedConflict();
LifecycleCheck(!lifecycle.Active && lifecycle.ArmedOccurred,
    "external SETS defeat marker does not initialize Taunt conflict");
lifecycle.End(preserveArmedDefeatState: false);
LifecycleCheck(!lifecycle.ArmedOccurred && !lifecycle.TryEscalate(),
    "external marker clears without admitting stale escalation");
Console.WriteLine("14/14 scene Taunt lifecycle cases passed; Mission callback order NOT_RUN");
