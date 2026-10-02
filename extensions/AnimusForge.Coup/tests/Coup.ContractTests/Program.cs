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
        BattleOptionsRegression();
        EntryRequirementsRegression();
        LoyalistRegression();
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
        VictoryFeedbackRegression();
        Console.WriteLine("Coup contracts: " + _assertions + " PASS");
    }

    private static void LoyalistRegression()
    {
        Assert(CoupLoyalistPolicy.Opposes(false, 80, 40), "positive relation to new king does not block stronger old allegiance");
        Assert(!CoupLoyalistPolicy.Opposes(false, 40, 80), "new king preferred remains loyal");
        Assert(!CoupLoyalistPolicy.Opposes(false, 40, 40), "equal relations do not manufacture opposition");
        Assert(CoupLoyalistPolicy.Opposes(true, 0, 100), "dispossessed ruling family has its own claim");
        Assert(CoupLoyalistPolicy.Opposes(false, -10, -50), "relative support also works with negative relations");
        var candidates = new List<CoupLoyalistCandidate> {
            new CoupLoyalistCandidate { ClanId = "neutral", RelationToOldKing = 20, RelationToNewKing = 20 },
            new CoupLoyalistCandidate { ClanId = "b", RelationToOldKing = 60, RelationToNewKing = 30, Fortifications = 3, ClanTier = 5 },
            new CoupLoyalistCandidate { ClanId = "a", RelationToOldKing = 60, RelationToNewKing = 30, Fortifications = 3, ClanTier = 5 },
            new CoupLoyalistCandidate { ClanId = "royal", FormerRulingClan = true, RelationToNewKing = 80, Fortifications = 1 }
        };
        var ranked = CoupLoyalistPolicy.Rank(candidates);
        Assert(ranked.Count == 3 && ranked[0].ClanId == "royal", "old royal family leads before stronger supporters");
        Assert(ranked[1].ClanId == "a" && ranked[2].ClanId == "b", "stable id breaks otherwise identical ties");
        candidates.RemoveAt(3);
        Assert(CoupLoyalistPolicy.Rank(candidates)[0].ClanId == "a", "unavailable old royal family permits another supporter to lead");
        Assert(CoupLoyalistPolicy.Rank(new List<CoupLoyalistCandidate>()).Count == 0, "no physical candidates never invents a rebel");
        var session = NewSession();
        Assert(!session.AftermathPending && !session.AftermathOpened, "old session does not auto replay disposition");
        session.AftermathPending = true;
        Assert(!session.IsValid(), "street victory cannot request political aftermath");
        session.Phase = CoupPhase.Completed; session.KingSubdued = true; session.Disposition = CoupKingDisposition.Release;
        session.CasualtiesCommitted = session.RulingClanCommitted = session.TownCommitted = session.CustodyCommitted
            = session.FactsCommitted = session.RebellionQueued = true;
        Assert(session.IsValid(), "fully committed coup may wait for disposition");
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<CoupSession>(Newtonsoft.Json.JsonConvert.SerializeObject(session));
        Assert(restored.IsValid() && restored.AftermathPending, "pending disposition survives reload");
        restored.AftermathOpened = true;
        Assert(!restored.IsValid(), "contradictory pending and opened flags rejected");
        restored.AftermathPending = false;
        Assert(restored.IsValid(), "opened receipt terminates pending work");
    }

    private static void EntryRequirementsRegression()
    {
        var requirements = CoupEntryRequirements.Normalize(4, 300, 60);
        Assert(requirements.IsValid(), "default admission snapshot valid");
        Assert(!requirements.Check(3, 300, 61, 60, out var reason) && reason.Contains("3／4"), "clan tier 3 rejected with deficit");
        Assert(!requirements.Check(4, 299, 61, 60, out reason) && reason.Contains("299／300"), "influence 299 rejected with deficit");
        Assert(!requirements.Check(4, 300, 60, 60, out reason) && reason.Contains("61") && reason.Contains("当前60"), "60 regulars cannot supply 60 assault plus rear guard");
        Assert(requirements.Check(4, 300, 61, 60, out reason) && reason == "", "tier4 influence300 healthy61 accepted");
        Assert(!requirements.Check(4, 300, 121, 59, out reason) && reason.Contains("配置冲突"), "minimum above street cap reports configuration conflict");
        Assert(!requirements.Check(4, float.NaN, 61, 60, out reason), "invalid influence does not satisfy positive requirement");
        var disabled = CoupEntryRequirements.Normalize(0, 0, 1);
        Assert(disabled.Check(0, -10, 2, 1, out reason), "zero tier and influence disable political gates");
        Assert(!disabled.Check(0, 0, 1, 1, out reason), "legacy mode still retains rear guard");
        var min = CoupEntryRequirements.Normalize(int.MinValue, int.MinValue, int.MinValue);
        var max = CoupEntryRequirements.Normalize(int.MaxValue, int.MaxValue, int.MaxValue);
        Assert(min.MinimumClanTier == 0 && min.MinimumInfluence == 0 && min.MinimumTroops == 1, "low settings clamp");
        Assert(max.MinimumClanTier == 6 && max.MinimumInfluence == 5000 && max.MinimumTroops == 120, "high settings clamp");
        foreach (var field in typeof(CoupEntryRequirements).GetFields())
        {
            var invalid = new CoupEntryRequirements();
            field.SetValue(invalid, -1);
            Assert(!invalid.IsValid(), "negative saved requirement rejected: " + field.Name);
            field.SetValue(invalid, int.MaxValue);
            Assert(!invalid.IsValid(), "excess saved requirement rejected: " + field.Name);
        }
        var session = NewSession();
        session.EntryRequirements = requirements;
        session.Troops.Add(Troop("survivor"));
        session.Phase = CoupPhase.Hall;
        Assert(session.IsValid(), "already started hall with one survivor remains valid under minimum60");
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(session);
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<CoupSession>(json);
        Assert(restored.IsValid() && restored.EntryRequirements.MinimumClanTier == 4
            && restored.EntryRequirements.MinimumInfluence == 300 && restored.EntryRequirements.MinimumTroops == 60, "actual saved requirements roundtrip");
        var legacyJson = Newtonsoft.Json.Linq.JObject.Parse(json);
        legacyJson.Remove("EntryRequirements");
        restored = Newtonsoft.Json.JsonConvert.DeserializeObject<CoupSession>(legacyJson.ToString());
        Assert(restored.IsValid() && restored.EntryRequirements.MinimumClanTier == 0
            && restored.EntryRequirements.MinimumInfluence == 0 && restored.EntryRequirements.MinimumTroops == 1, "legacy active coup receives no new gates");
        legacyJson["EntryRequirements"] = null;
        Assert(!Newtonsoft.Json.JsonConvert.DeserializeObject<CoupSession>(legacyJson.ToString()).IsValid(), "explicit null admission snapshot rejected");
        var newer = CoupEntryRequirements.Normalize(0, 0, 1);
        Assert(session.EntryRequirements.MinimumTroops == 60 && newer.MinimumTroops == 1, "new settings cannot mutate captured admission");
    }

    private static void BattleOptionsRegression()
    {
        var defaults = CoupBattleOptions.LegacyDefaults();
        Assert(defaults.IsValid() && defaults.StreetAllyLimit == 60 && defaults.HallAllyLimit == 20
            && defaults.GateGuardLimit == 10 && defaults.HallGuardLimit == 20
            && defaults.DefenderWaveSize == 30 && defaults.DefenderWaveIntervalSeconds == 30
            && defaults.MaxActiveDefenderWaves == 4, "historical defaults preserved");
        var min = CoupBattleOptions.Normalize(int.MinValue, -1, 0, 0, 0, 0, 0);
        Assert(min.IsValid() && min.StreetAllyLimit == 1 && min.HallAllyLimit == 1 && min.GateGuardLimit == 1
            && min.HallGuardLimit == 1 && min.DefenderWaveSize == 1 && min.DefenderWaveIntervalSeconds == 5
            && min.MaxActiveDefenderWaves == 1, "MCM low outliers clamped");
        var max = CoupBattleOptions.Normalize(int.MaxValue, 999, 999, 999, 999, 999, 999);
        Assert(max.IsValid() && max.StreetAllyLimit == 120 && max.HallAllyLimit == 40 && max.GateGuardLimit == 30
            && max.HallGuardLimit == 40 && max.DefenderWaveSize == 60 && max.DefenderWaveIntervalSeconds == 120
            && max.MaxActiveDefenderWaves == 4, "MCM high outliers clamped");
        foreach (var field in typeof(CoupBattleOptions).GetFields())
        {
            var invalid = CoupBattleOptions.LegacyDefaults();
            field.SetValue(invalid, 0);
            Assert(!invalid.IsValid(), "saved low outlier rejected: " + field.Name);
            field.SetValue(invalid, int.MaxValue);
            Assert(!invalid.IsValid(), "saved high outlier rejected: " + field.Name);
        }
        var session = NewSession();
        session.BattleOptions = max;
        for (int i = 0; i < 120; i++) session.Troops.Add(Troop("ally" + i));
        for (int i = 0; i < 40; i++) session.Troops[i].HallSelected = true;
        for (int i = 0; i < 30; i++) session.Troops.Add(Troop("gate" + i, CoupTroopRole.GateGuard));
        for (int i = 0; i < 40; i++) session.Troops.Add(Troop("guard" + i, CoupTroopRole.HallGuard));
        Assert(session.IsValid(), "expanded roster boundaries accepted");
        session.Troops[40].HallSelected = true;
        Assert(!session.IsValid(), "expanded hall boundary enforced");
        session.Troops[40].HallSelected = false;
        foreach (var role in new[] { CoupTroopRole.Ally, CoupTroopRole.GateGuard, CoupTroopRole.HallGuard })
        {
            session.Troops.Add(Troop("overflow", role));
            Assert(!session.IsValid(), "expanded role boundary enforced: " + role);
            session.Troops.RemoveAt(session.Troops.Count - 1);
        }
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<CoupSession>(JsonSerializer.Serialize(session, jsonOptions), jsonOptions);
        Assert(restored.IsValid() && restored.BattleOptions.StreetAllyLimit == 120
            && restored.BattleOptions.DefenderWaveIntervalSeconds == 120, "expanded snapshot survives JSON roundtrip");
        session = NewSession();
        var json = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(session, jsonOptions));
        json.AsObject().Remove("BattleOptions");
        restored = JsonSerializer.Deserialize<CoupSession>(json.ToJsonString(), jsonOptions);
        Assert(restored.IsValid() && restored.BattleOptions.StreetAllyLimit == 60
            && restored.BattleOptions.DefenderWaveSize == 30, "old save uses historical defaults");
        json["BattleOptions"] = null;
        Assert(!JsonSerializer.Deserialize<CoupSession>(json.ToJsonString(), jsonOptions).IsValid(), "explicit null snapshot rejected");
        json["BattleOptions"] = new System.Text.Json.Nodes.JsonObject { ["StreetAllyLimit"] = 60 };
        Assert(!JsonSerializer.Deserialize<CoupSession>(json.ToJsonString(), jsonOptions).IsValid(), "partial snapshot rejected");
        session.BattleOptions = min;
        session.Troops.Add(Troop("one"));
        session.Troops[0].HallSelected = true;
        Assert(session.IsValid(), "one-soldier snapshot accepted");
        var next = CoupBattleOptions.LegacyDefaults();
        next.StreetAllyLimit = 99;
        Assert(session.BattleOptions.StreetAllyLimit == 1 && restored.BattleOptions.StreetAllyLimit == 60,
            "sessions do not share options with future coups");
    }

    private static void VictoryFeedbackRegression()
    {
        var session = NewSession();
        session.Phase = CoupPhase.AwaitingResolution;
        session.KingSubdued = true;
        Assert(session.NeedsHallDisposition, "verified hall victory waits for explicit disposition");
        session.Disposition = CoupKingDisposition.Release;
        Assert(!session.NeedsHallDisposition, "selected disposition cannot open twice");
        Assert(!session.HasConfirmedVictory, "hall choice is not political success");
        session.Phase = CoupPhase.Completed;
        session.CasualtiesCommitted = session.RulingClanCommitted = session.TownCommitted
            = session.CustodyCommitted = session.FactsCommitted = session.RebellionQueued = true;
        Assert(session.HasConfirmedVictory && !session.NeedsVictoryFeedback, "legacy completed session does not replay feedback");
        session.AftermathPending = true;
        Assert(session.IsValid() && !session.NeedsVictoryFeedback, "historical aftermath flag does not request new UI");
        session.AftermathPending = false;
        session.VictoryReportAcknowledged = session.CoronationRequested = false;
        Assert(session.NeedsVictoryFeedback && session.HasConfirmedVictory, "new victory has pending presentation");
        session.Troops.Add(new CoupTroopRecord { Id = "ally_k", CharacterId = "s", SourcePartyId = "p", Role = CoupTroopRole.Ally, Killed = true, Removed = true });
        session.Troops.Add(new CoupTroopRecord { Id = "guard_w", CharacterId = "s", SourcePartyId = "p", Role = CoupTroopRole.HallGuard, Wounded = true, Removed = true });
        string report = CoupOutcomeReport.BuildPlayerVictory(session, "王国", "新王", "城镇", "玩家家族", "旧王", false);
        Assert(report.Contains("当前统治者：新王") && report.Contains("城镇当前归属：玩家家族"), "report uses current authoritative names");
        Assert(report.Contains("本次选择不扣押") && report.Contains("突击队：阵亡 1，负伤 0") && report.Contains("守军：阵亡 0，负伤 1"), "release and recorded casualties shown");
        Assert(report.Contains("是否实际起兵以之后的战役结果为准") && !report.Contains("叛乱已经发生"), "queue receipt never claims actual rebellion");
        session.Disposition = CoupKingDisposition.Capture;
        Assert(CoupOutcomeReport.BuildPlayerVictory(session, "k", "r", "t", "o", "king", true).Contains("当前仍由你的部队保管"), "actual held king shown");
        Assert(CoupOutcomeReport.BuildPlayerVictory(session, "k", "r", "t", "o", "king", false).Contains("目前已不在你的部队保管"), "later escape does not invent continuing custody");
        session.FactsCommitted = false;
        bool refused = false;
        try { CoupOutcomeReport.BuildPlayerVictory(session, "k", "r", "t", "o", "king", true); }
        catch (InvalidOperationException) { refused = true; }
        Assert(refused && !session.HasConfirmedVictory, "partial settlement cannot show final victory");
        session.FactsCommitted = true;
        session.CoronationRequested = true;
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(session);
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<CoupSession>(json);
        Assert(restored.IsValid() && restored.NeedsVictoryFeedback && restored.CoronationRequested == true, "save after animation submission retains report without resubmitting animation");
        restored.VictoryReportAcknowledged = true;
        restored = Newtonsoft.Json.JsonConvert.DeserializeObject<CoupSession>(Newtonsoft.Json.JsonConvert.SerializeObject(restored));
        Assert(!restored.NeedsVictoryFeedback, "acknowledged report survives save-load");
        var legacy = Newtonsoft.Json.Linq.JObject.Parse(json);
        legacy.Remove("VictoryReportAcknowledged"); legacy.Remove("CoronationRequested");
        restored = legacy.ToObject<CoupSession>();
        Assert(restored.VictoryReportAcknowledged == null && restored.CoronationRequested == null && !restored.NeedsVictoryFeedback, "missing nullable markers preserve legacy semantics");
        Assert((int)CoupPhase.Hall == 3 && (int)CoupPhase.AwaitingResolution == 4 && (int)CoupPhase.Completed == 5, "phase enum identities unchanged");
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
