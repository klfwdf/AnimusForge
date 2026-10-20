using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

internal static class DiplomacyChainLocalizationReplay
{
    internal static void Run()
    {
        var round = new WorldDiplomacyRound { RoundId = "audit", RelayRouteKingdomIds = new() { "k1", "k2" } };
        string Resolve(string id) => id == "Vlandia" ? "k2" : id is "k1" or "k2" or "k3" ? id : null;
        (bool Failed, string Reason, JObject Json) Validate(bool relay, string target, string planned, bool duplicate = false)
        {
            var json = new JObject {
                ["primary_target_kingdom_id"] = target, ["addressed_kingdom_ids"] = new JArray(target),
                ["mentioned_kingdom_ids"] = new JArray("Vlandia"), ["round_plan"] = new JObject {
                    ["selected_kingdom_ids"] = new JArray(planned), ["topic"] = "Trade" },
                ["peace_terms"] = new JObject(), ["requires_response"] = true,
                ["tone"] = "neutral", ["confidence"] = 1.0, ["title"] = "Trade offer", ["body"] = "We propose trade.",
                ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = target, ["intent"] = "propose_trade" })
            };
            if (duplicate) ((JArray)json["actions"]).Add(new JObject { ["target_kingdom_id"] = "k2", ["intent"] = "propose_trade" });
            var job = new WorldDiplomacyJob { Kind = "generate", RoundId = round.RoundId, IsRelayTurn = relay,
                AllowUntargeted = !relay, CandidateKingdomIds = new() { "k2" } };
            (bool Violation, string TargetKingdomId, string Reason) Single(JObject single, bool _)
            {
                bool failed = WorldDiplomacyGenerationValidationRules.TryGetGeneratedSingleActionLegalityViolation(
                    job, single, "k1", "k2", 4, Resolve, _ => false, _ => true, (_, _) => false,
                    _ => round, _ => null, (_, _) => true, (_, _, _) => true,
                    (_, _, _) => new() { "propose_trade" }, (_, _, _) => (false, ""), (_, _) => (false, ""),
                    (_, _, _) => (false, ""), (_, _) => null, (_, _, _) => (false, ""), _ => (false, ""),
                    _ => { }, out string id, out string reason);
                return (failed, id, reason);
            }
            bool failed = WorldDiplomacyGenerationValidationRules.TryGetGeneratedIntentLegalityViolation(
                job, json, 3, 4, Single, _ => round, _ => null, _ => null, out _, out string reason);
            return (failed, reason, json);
        }
        foreach (bool relay in new[] { true, false })
        {
            foreach (string target in new[] { "k2", "Vlandia" })
            {
                var result = Validate(relay, target, target);
                Test.True(!result.Failed, "ID/display name accepted after canonicalization: " + result.Reason);
                Test.True((string)result.Json["actions"][0]["target_kingdom_id"] == "k2"
                    && (string)result.Json["addressed_kingdom_ids"][0] == "k2"
                    && (string)result.Json["mentioned_kingdom_ids"][0] == "k2", "canonical IDs survive action splitter and copy");
                if (!relay) Test.True((string)result.Json["round_plan"]["selected_kingdom_ids"][0] == "k2", "round plan uses resolved ID");
            }
            Test.True(Validate(relay, "unknown-name", "k2").Reason.Contains("target_kingdom_not_found"), "unknown names are not guessed");
            Test.True(Validate(relay, "k3", "k3").Failed, "outside-route/candidate targets remain rejected");
            Test.True(Validate(relay, "k1", "k1").Failed, "author cannot target itself");
            Test.True(Validate(relay, "Vlandia", "Vlandia", true).Reason == "diplomatic_action_target_missing_or_duplicate", "aliases cannot bypass duplicate-target restriction");
        }
        bool english = SmokeLocalizationEnvironment.EnglishPackage;
        string nativeReason = DiplomacyCompatibilityTexts.Get("WarScore_WarReasonSufficient") + "；"
            + DiplomacyCompatibilityTexts.Get("RulerScore_PrivateRelationPrefix") + WorldDiplomacyTextRules.DescribeRulerRelation(0)
            + "；" + DiplomacyCompatibilityTexts.Get("RulerScore_CultureClaimsPrefix") + "2";
        string context = WorldDiplomacyDocumentFactRules.BuildRecentNativeSignalContext(new[] {
            new NativeDiplomacySignal { SourceKingdomId = "k1", TargetKingdomId = "k2", Day = 12, Reason = nativeReason }
        }, "k1", "k2", DiplomacyPromptTexts.Get);
        Test.True(context.Contains(english ? "Day 12" : "第12天") && context.Contains(english ? "practical reasons" : "现实理由")
            && context.Contains(english ? "Cool relations" : WorldDiplomacyTextRules.DescribeRulerRelation(0)), "native day/reason fragments follow package");
        const string opaque = "玩家原文；{0} quotes and kingdom names";
        Test.True(DiplomacyLegacyPromptProjection.NativeReason(opaque, DiplomacyPromptTexts.Get) == opaque, "unknown reason text is opaque");
        Test.True(WorldDiplomacyRoundLifecycleRules.DescribeThreatStage("warning", DiplomacyPromptTexts.Get)
            == (english ? "denunciation" : "谴责"), "threat label follows package");
        string date = "1" + DiplomacyCompatibilityTexts.Get("KingdomProfile_YearSuffix") + DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonSpring")
            + DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonSuffix") + "2" + DiplomacyCompatibilityTexts.Get("KingdomProfile_DaySuffix");
        var fact = new WorldDiplomacyBattleFact { GameDate = date, Location = DiplomacyCompatibilityTexts.Get("Battle_Wilderness_Garbled"),
            BattleType = "Field battle", AttackerKingdomIds = new() { "k1" }, DefenderKingdomIds = new() { "k2" }, WinnerSide = "attacker" };
        string battle = WorldDiplomacyTextRules.FormatBattleFactForPrompt(fact, id => id, _ => "Day 1", DiplomacyPromptTexts.Get);
        Test.True(battle.Contains(english ? "Year 1, spring 2" : "1年春季2天") && battle.Contains(english ? "wilderness" : "野外"), "battle date and wilderness follow package");
        Test.True(fact.GameDate == date && fact.Location == DiplomacyCompatibilityTexts.Get("Battle_Wilderness_Garbled"), "projection does not mutate stored battle");
        Test.True(DiplomacyDateProjection.ForPrompt(opaque, DiplomacyPromptTexts.Get) == opaque, "unknown date is opaque");
        Test.True(DiplomacyLegacyPromptProjection.BattleLocation(opaque, DiplomacyPromptTexts.Get) == opaque, "dynamic location is opaque");
        foreach (string old in new[] { "宣言已公开；分析失败，外交动作未执行。可选择“重新解析”再次处理原文。",
            "Declaration published; analysis failed, diplomatic action not executed. Choose Reparse to process the original text again." })
            Test.True(WorldDiplomacyAnalysisApplication.ProjectMechanicalResultForDisplay(old).Contains(
                SmokeLocalizationEnvironment.EnglishUi ? "no diplomatic action" : "外交动作未执行"), "known CN/EN receipts follow UI across packs");
        Test.True(WorldDiplomacyAnalysisApplication.ProjectMechanicalResultForDisplay(opaque) == opaque, "unknown mechanical result unchanged");
        foreach (string old in DiplomacyReplyTextRules.SourcesFor("OralDiplomacy_Executed"))
            if (!string.IsNullOrEmpty(old)) Test.True(DiplomacyDialogueStatusUi.Project(old).Contains(
                SmokeLocalizationEnvironment.EnglishUi ? "executed" : "已生效"), "old oral receipts project across both package/UI languages");
        Test.True(DiplomacyDialogueStatusUi.Project(opaque) == opaque, "unknown dialogue status stays opaque");
        Test.True(DiplomacyUiLocalization.Text("AF_DIP_UI_Vassalage_SubjectWarSyncPrefix", "Suzerain war status synchronized:")
            == (SmokeLocalizationEnvironment.EnglishUi ? "Suzerain war status synchronized:" : "宗主国战争状态已同步："), "war sync prefix follows UI language");
        Test.True(DiplomacyUiLocalization.Text("AF_DIP_UI_Vassalage_SubjectWarConfirmedPrefix", "Suzerain war status confirmed:")
            == (SmokeLocalizationEnvironment.EnglishUi ? "Suzerain war status confirmed:" : "宗主国战争状态已确认："), "confirmed war prefix follows UI language");
        PlayerResponseRecoveryReplay.VerifyStatementResponse();
    }
}
