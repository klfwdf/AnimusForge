using System;
using System.Text.Json;
using System.Collections.Generic;
using AnimusForge;
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
        CheckBulletinReport();
        Console.WriteLine("Coup contracts: " + _assertions + " PASS");
    }

    private static void CheckBulletinReport()
    {
        var session = NewSession();
        Assert(!CoupOutcomeReport.CanReport(session, true) && !CoupOutcomeReport.CanReport(session, false), "unsettled fighting cannot publish a final result");
        session.Troops.Add(Troop("a"));
        session.Troops.Add(Troop("b"));
        session.Troops[0].HallSelected = true;
        session.Troops[1].TryRecordCasualty(true);
        session.Troops.Add(Troop("g", CoupTroopRole.GateGuard));
        session.Troops[2].TryRecordCasualty(false);
        string Report(bool success, bool captured = false, bool war = true) =>
            CoupOutcomeReport.Build(session, "玩家领主", "德泰尔", "加伦", "瓦兰迪亚", success, captured, war);
        string unentered = Report(false);
        Assert(unentered.Contains("未记录到实际进场") && !unentered.Contains("进入城镇街道"), "pre-scene failure does not invent a battle");
        session.SceneEntered = true;
        string street = Report(false);
        Assert(street.Contains("瓦兰迪亚的加伦针对国王德泰尔") && street.Contains("突击队共2人"), "report names place, realm, ruler and assault size");
        Assert(street.Contains("进入城镇街道") && !street.Contains("突破大厅入口") && !street.Contains("攻入领主大厅"), "street-only result reports only observed progress");
        Assert(street.Contains("政变失败") && street.Contains("脱离原王国并与之开战"), "failed result includes confirmed consequences");
        Assert(!Report(false, false, false).Contains("与之开战"), "no war claimed when original kingdom no longer at war");
        session.GateBreached = true;
        Assert(Report(false).Contains("突破大厅入口") && !Report(false).Contains("攻入领主大厅"), "breaching gate alone does not invent hall combat");
        session.HallEntered = true;
        string hall = Report(false);
        Assert(hall.Contains("率1名突击队员攻入领主大厅") && !hall.Contains("制服国王"), "hall failure does not invent king subdued");
        session.KingSubdued = true;
        session.CasualtiesCommitted = session.RulingClanCommitted = session.TownCommitted = session.CustodyCommitted = true;
        Assert(CoupOutcomeReport.CanReport(session, true), "fully confirmed victory may publish");
        session.CustodyCommitted = false;
        Assert(!CoupOutcomeReport.CanReport(session, true), "incomplete custody blocks victory report");
        session.CustodyCommitted = true;
        string victory = Report(true, true);
        Assert(victory.Contains("制服国王") && victory.Contains("取得瓦兰迪亚王位并接管加伦") && victory.Contains("旧王被扣押"), "victory report includes exact outcome and custody");
        Assert(Report(true).Contains("选择不扣押旧王"), "non-capture disposition is described without inventing a release action");
        Assert(victory.Contains("突击队阵亡1、负伤0；守军阵亡0、负伤1"), "counts come from actual session casualties, not total defender reserve");
        session.DefectionCommitted = true;
        Assert(!CoupOutcomeReport.CanReport(session, false), "failure before withdrawal cannot publish final outcome");
        session.WithdrawalCommitted = true;
        Assert(CoupOutcomeReport.CanReport(session, false), "confirmed failure may publish");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var saved = JsonSerializer.Deserialize<CoupSession>(JsonSerializer.Serialize(session, options), options);
        Assert(saved.GateBreached && saved.HallEntered, "new progress survives session JSON roundtrip");
        var old = JsonSerializer.Deserialize<CoupSession>("{\"SceneEntered\":true}", options);
        Assert(!old.GateBreached && !old.HallEntered, "old JSON does not invent missing progress");

        var fact = new WorldBulletinEvent { Kind = "coup_success", Key = "coup:test:bulletin", Group = "coup:test", Score = 95,
            Day = 5, Hour = 120, InvolvesPlayer = true, Sentence = victory, KingdomIds = new List<string> { "vlandia" } };
        var focus = new WorldBulletinFocus { PlayerKingdomId = "vlandia" };
        Assert(WorldBulletinPolicy.IsTrigger(fact, focus), "coup opens normal bulletin collection window");
        var selection = WorldBulletinPolicy.Select(new List<WorldBulletinEvent> { fact }, new WorldBulletinScopeState { WindowEndHour = 144 }, focus, 144);
        Assert(selection?.Major == fact, "dedicated coup fact is selectable as headline");
        Assert(WorldBulletinPolicy.BuildTemplate(selection).Major.Contains(victory), "offline fallback preserves place, process and outcome");
        Assert(WorldBulletinPolicy.BuildUserPrompt("天下", "某日", selection, null).Contains(victory), "LLM receives full untruncated coup narrative");
        Assert(WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcKingdomHeader, "瓦兰迪亚", "vlandia", new[] { fact }, 5).Contains(victory), "NPC bulletin knowledge receives the same full facts");
        Assert(WorldBulletinPolicy.TitleForKind("coup_success") == "政变夺位" && WorldBulletinPolicy.TitleForKind("coup_failure") == "政变失败", "dedicated fallback titles distinguish final outcomes");
    }
}
