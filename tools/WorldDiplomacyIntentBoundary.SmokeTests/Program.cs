using System.Text;
using System.Text.Json;
using AnimusForge;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private const string NaturalTradeProposal = "阿塞莱愿与南帝国就边地商路秩序展开协商，并派遣使节商谈具体条款。";

    private static int Main()
    {
        string sourcePath = FindRepositoryFile("WorldDiplomacyBehavior.cs");
        string source = File.ReadAllText(sourcePath, Encoding.UTF8);
        string generatedValidation = ExtractSection(
            source,
            "private bool TryGetGeneratedIntentLegalityViolation(",
            "private static bool ArePeaceTermsEquivalent(");
        string generatedEnvelope = ExtractSection(
            source,
            "private bool TryApplyGeneratedSemanticEnvelope(",
            "private void CommitAnalysis(");
        string analysisCommit = ExtractSection(
            source,
            "private void CommitAnalysis(",
            "private void ReconcilePlayerDeclarationWithOpenOffer(");
        string playerOfferReconciliation = ExtractSection(
            source,
            "private void ReconcilePlayerDeclarationWithOpenOffer(",
            "private void ProcessAnalyzedDocument(");
        string fallbackAnalysis = ExtractSection(
            source,
            "private string BuildFallbackAnalysisJson(",
            "private string BuildFallbackAnnualSummary(");
        string playerSubmission = ExtractSection(
            source,
            "private void SubmitPlayerDocument(",
            "private void SuspendActiveExchangeForPlayerInsertion(");
        string invalidSuppression = ExtractSection(
            source,
            "private void SuppressInvalidDocumentBeforePropagation(",
            "private bool TryApplyGeneratedSemanticEnvelope(");
        string playerReplySubmission = ExtractSection(
            source,
            "private void OpenPlayerReplyCompose(",
            "private WorldEventInboxPopupData BuildRoyalAnnouncementArchiveData(");

        Test.True(NaturalTradeProposal.Contains("商路", StringComparison.Ordinal), "fixture must describe the trade domain");
        Test.True(!new[] { "提议", "建议", "倡议", "邀请", "请求" }
            .Any(NaturalTradeProposal.Contains), "fixture must retain the natural wording that the old whitelist rejected");

        Test.True(!generatedValidation.Contains("TryGetPlayerVisibleIntentViolation", StringComparison.Ordinal),
            "AI-generated declarations must not use the player free-text intent recognizer");
		Test.True(!generatedValidation.Contains("visible_intent_mismatch", StringComparison.Ordinal),
			"AI-generated declarations must not be rejected by a prose keyword whitelist");
		Test.True(!generatedValidation.Contains("HasExplicitUltimatumCompliance(body, author, generatedTarget)", StringComparison.Ordinal),
			"structured AI comply_ultimatum must be decided by intent and the live threat source, not reclassified from prose");
        Test.True(!generatedValidation.Contains("HasVisibleIntentDirectedAtTarget", StringComparison.Ordinal),
            "AI-generated declarations must not re-infer the structured target from literary prose");
        Test.True(!generatedValidation.Contains("LooksLikeMisaddressedThirdPartyOfferResponse", StringComparison.Ordinal),
            "AI-generated declarations must use structured offer ownership instead of prose inference");
        Test.True(!generatedValidation.Contains("LooksLikeExplicitPeaceNegotiationWithTarget", StringComparison.Ordinal),
            "AI-generated declarations must not acquire a different intent from prose keywords");

        foreach (string requiredGuard in new[]
        {
            "IsSupportedDiplomacyIntent",
            "IsActionableDiplomacyIntent",
            "CommitmentMatchesIntent",
            "TryGetDiplomaticStateViolation",
            "TryGetDiplomaticThreatIntentViolation",
            "TryResolveOpenProposalFor",
            "offer_response_source_mismatch",
            "diplomatic_action_has_no_target",
            "TryGetPublicPeaceTermsDisclosureViolation"
        })
        {
            Test.True(generatedValidation.Contains(requiredGuard, StringComparison.Ordinal),
                "generated validation must retain hard guard: " + requiredGuard);
        }
        Test.True(!generatedValidation.Contains("WorldDiplomacyThreatSemantics", StringComparison.Ordinal),
            "generated warning/ultimatum prose must not be reclassified after the model selected a legal structured intent");
        Test.True(generatedValidation.Contains("non_actionable_diplomatic_intent", StringComparison.Ordinal),
            "generated declarations must reject retired non-action intents before publication");
        Test.True(generatedEnvelope.Contains("IsActionableDiplomacyIntent(intent)", StringComparison.Ordinal),
            "the generation envelope must reject a retired intent even if it bypasses the first validator");
        Test.True(generatedValidation.Contains(
                      "bool resultSettlementRelay = job.IsRelayTurn && owningRound?.ResultSettlementPending == true",
                      StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "new HashSet<string>(job.CandidateKingdomIds ?? new List<string>()",
                      StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "presentedSettlementTargets.Contains(generatedTarget.StringId)",
                      StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "CanUseResultSettlementTarget(owningRound, author, generatedTarget)",
                      StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "kingdom_not_in_result_settlement_scope\" : \"kingdom_not_in_relay_route",
                      StringComparison.Ordinal),
            "a slot-owned settlement relay may select a presented extensible target, while an ordinary relay remains route-bound");
        Test.True(generatedEnvelope.Contains(
                      "bool resultSettlementRelay = relayTurn && envelopeRound?.ResultSettlementPending == true",
                      StringComparison.Ordinal)
                  && generatedEnvelope.Contains(
                      "? CanUseResultSettlementTarget(envelopeRound, author, target)",
                      StringComparison.Ordinal)
                  && generatedEnvelope.Contains(
                      ": RoundRouteContainsKingdom(envelopeRound, target.StringId)",
                      StringComparison.Ordinal),
            "the secondary generation envelope must preserve the same settlement-only target expansion boundary");
        Test.True(playerSubmission.Contains("PublishPlayerAuthoredDocumentImmediately(document)", StringComparison.Ordinal)
                  && playerSubmission.Contains("外交宣言已经公开发布", StringComparison.Ordinal)
                  && playerSubmission.IndexOf("PublishPlayerAuthoredDocumentImmediately(document)", StringComparison.Ordinal)
                     < playerSubmission.IndexOf("EnqueueAnalysisJob(document", StringComparison.Ordinal),
            "a player declaration must become public before its semantic-analysis job is queued");
        Test.True(playerReplySubmission.Contains("PublishPlayerAuthoredDocumentImmediately(response)", StringComparison.Ordinal)
                  && playerReplySubmission.Contains("外交回应已经公开发布", StringComparison.Ordinal)
                  && playerReplySubmission.IndexOf("PublishPlayerAuthoredDocumentImmediately(response)", StringComparison.Ordinal)
                     < playerReplySubmission.IndexOf("EnqueueAnalysisJob(response", StringComparison.Ordinal),
            "a player response must become public before its semantic-analysis job is queued");
        Test.True(analysisCommit.Contains("intent = \"statement\"", StringComparison.Ordinal)
                  && fallbackAnalysis.Contains("[\"intent\"] = \"statement\"", StringComparison.Ordinal)
                  && fallbackAnalysis.Contains("[\"status\"] = \"fallback\"", StringComparison.Ordinal),
            "no-action, malformed, and failed player analysis must preserve the public declaration as a statement");
        Test.True(invalidSuppression.Contains("document.IsPlayerAuthored && document.IsReadyForPublication", StringComparison.Ordinal)
                  && invalidSuppression.Contains("PreservePublishedPlayerDocumentAfterRejectedMechanic", StringComparison.Ordinal),
            "a mechanic rejection must never delete a player declaration that was already published");
        string analyzedPublication = ExtractSection(
            source,
            "private void ProcessAnalyzedDocument(",
            "private bool TryGetPlayerWorldStateIntentViolation(");
        Test.True(analyzedPublication.Contains("allowedPlayerPublicIntent", StringComparison.Ordinal)
                  && analyzedPublication.Contains("FinalizePublishedDocumentAfterAnalysis", StringComparison.Ordinal)
                  && analyzedPublication.Contains("ApplyDiplomaticPressureEffect", StringComparison.Ordinal),
            "player statements, condemnations, apologies, and concessions must remain published and retain their semantic effects");
        Test.True(analysisCommit.Contains("player declaration analysis downgraded to public statement", StringComparison.Ordinal),
            "MODE=ANALYZE no-action or malformed status must downgrade player mechanics without suppressing publication");

        Test.True(!source.Contains("TryGetPlayerVisibleIntentViolation", StringComparison.Ordinal)
                  && !source.Contains("HasExplicitUltimatumCompliance", StringComparison.Ordinal)
                  && !source.Contains("InferIntentFromExplicitPhrases", StringComparison.Ordinal)
                  && !source.Contains("HasExplicitPlayerAcceptance", StringComparison.Ordinal)
                  && !source.Contains("HasExplicitPlayerRejection", StringComparison.Ordinal),
            "production must not infer or veto structured diplomatic actions from player or AI prose keywords");
        Test.True(source.Contains("\"propose_trade\" => \"贸易申请\"", StringComparison.Ordinal),
            "structured trade intent must remain visible in the document UI");
        Test.True(source.Contains("\"declare_war\" => \"宣战告知\"", StringComparison.Ordinal),
            "structured war intent must remain visible in the document UI");
        Test.True(source.Contains("\"propose_alliance\" => \"同盟申请\"", StringComparison.Ordinal),
            "structured alliance intent must remain visible in the document UI");
        Test.True(source.Contains("\"comply_ultimatum\" => \"通牒就范\"", StringComparison.Ordinal)
				  || source.Contains("\"comply_ultimatum\" => \"退让\"", StringComparison.Ordinal),
            "structured ultimatum compliance must remain visible in the document UI");

        foreach (string threatBoundary in new[]
        {
            "comply_ultimatum_after_war_started",
            "warning_escalation_requires_target_noncompliance",
            "LogDiplomaticThreatFallbackAnalysisPublished",
            "DeferUnresolvedRequiredThreatAction",
            "DomesticPenaltySkippedClanIds",
            "DomesticPenaltyHistoryRecorded",
            "UltimatumComplianceRoyalRelationPenalty",
            "responding_to_threat_document_id"
        })
        {
            Test.True(source.Contains(threatBoundary, StringComparison.Ordinal),
                "missing ultimatum lifecycle boundary: " + threatBoundary);
        }

        string fixedDeclarationContract = ExtractSection(
            source,
            "private static string BuildDiplomaticDeclarationModeContract()",
            "private static string BuildCanonicalHistoryCompressionModeContract()");
        string fixedAnalysisContract = ExtractSection(
            source,
            "private static string BuildAnalysisModeContract()",
            "private string BuildAnalysisPrompt(");
        string actionableIntentHelper = ExtractSection(
            source,
            "private static bool IsActionableDiplomacyIntent(string intent)",
            "private static bool IsSupportedCommitment(string commitment)");
        HashSet<string> expectedActionableIntents = new(StringComparer.Ordinal)
        {
            "warning", "ultimatum", "comply_ultimatum",
            "propose_peace", "accept_peace", "reject_peace",
            "propose_alliance", "accept_alliance", "reject_alliance", "break_alliance",
            "propose_trade", "accept_trade", "reject_trade", "cancel_trade", "declare_war"
        };
        string[] publicNonMechanicalIntents = { "statement", "condemn", "apology", "concession" };
        string externalResolvedPublication = ExtractSection(
            source,
            "private void NotifyExternalDiplomacyResolvedInternal(",
            "private static bool Patch_Kingdom_AddDecision_Prefix(");
        string externalResolvedWhitelist = ExtractSection(
            source,
            "private static bool IsExternallyResolvedDiplomaticIntent(string intent)",
            "private static bool IsSupportedCommitment(string commitment)");
        Test.True(externalResolvedPublication.Contains("IsExternallyResolvedDiplomaticIntent(normalizedAction)", StringComparison.Ordinal),
            "the public external-result bridge must not bypass the action-only publication boundary");
        foreach (string resolvedIntent in new[]
        {
            "declare_war", "accept_peace", "accept_alliance", "break_alliance", "accept_trade", "cancel_trade"
        })
        {
            Test.True(externalResolvedWhitelist.Contains("\"" + resolvedIntent + "\"", StringComparison.Ordinal),
                "external resolved-action whitelist is missing: " + resolvedIntent);
        }
        foreach (string unresolvedIntent in publicNonMechanicalIntents.Concat(new[]
        {
            "warning", "ultimatum", "comply_ultimatum", "propose_peace", "propose_alliance", "propose_trade",
            "reject_peace", "reject_alliance", "reject_trade"
        }))
        {
            Test.True(!externalResolvedWhitelist.Contains("\"" + unresolvedIntent + "\"", StringComparison.Ordinal),
                "the external result bridge must reject an action that it did not mechanically resolve: " + unresolvedIntent);
        }
        HashSet<string> declarationIntentTemplate = ExtractIntentEnum(fixedDeclarationContract);
        HashSet<string> analysisIntentEnum = ExtractIntentEnum(fixedAnalysisContract);
        Test.True(fixedDeclarationContract.Contains("\\\"actions\\\":[{", StringComparison.Ordinal)
                  && fixedDeclarationContract.Contains("\\\"target_kingdom_id\\\"", StringComparison.Ordinal)
                  && fixedDeclarationContract.Contains("\\\"intent\\\":\\\"当前可选动作\\\"", StringComparison.Ordinal)
                  && fixedDeclarationContract.Contains("\\\"peace_terms\\\"", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains("author_intent", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains("primary_target_kingdom_id", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains(
                      "【本篇唯一合法intent清单】",
                      StringComparison.Ordinal),
            "MODE=DECLARE must use the short multi-target actions schema without restoring singular fields or the retired legal-list block");
        Test.True(declarationIntentTemplate.SetEquals(new[]
            {
				"当前可选动作"
            }),
            "MODE=DECLARE JSON must use only the compact live-action placeholder instead of a global intent enum; actual="
            + string.Join(",", declarationIntentTemplate.OrderBy(x => x, StringComparer.Ordinal)));
        Test.True(!fixedDeclarationContract.Contains("author_intent", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains(
                      "\\\"commitment\\\":\\\"复制同组commitment\\\"",
                      StringComparison.Ordinal),
            "MODE=DECLARE must not depend on commitment text that no longer appears in compact live actions");
        string[] fixedContractSourceLines = fixedDeclarationContract
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] fixedThreatBoundaryLines = fixedContractSourceLines
            .Where(line => line.Contains("warning表示谴责", StringComparison.Ordinal))
            .ToArray();
		string[] fixedAcceptanceBoundaryLines = fixedContractSourceLines
			.Where(line => line.Contains("accept_*只表示", StringComparison.Ordinal))
            .ToArray();
		Test.True(fixedThreatBoundaryLines.Length == 1
				  && fixedThreatBoundaryLines[0].Contains("不是劝告、关心或善意提醒", StringComparison.Ordinal)
				  && fixedThreatBoundaryLines[0].Contains("要求停止具体敌对或军事行为", StringComparison.Ordinal)
				  && fixedThreatBoundaryLines[0].Contains("拒不停止将升级最后通牒或正式宣战", StringComparison.Ordinal)
				  && fixedThreatBoundaryLines[0].Contains("ultimatum表示战争最后通牒", StringComparison.Ordinal)
                  && CountOccurrences(fixedThreatBoundaryLines[0], "。") == 2,
            "the fixed contract must keep exactly one concise semantic-boundary sentence for warning/ultimatum");
		Test.True(fixedAcceptanceBoundaryLines.Length == 1
				  && fixedAcceptanceBoundaryLines[0].Contains("无条件", StringComparison.Ordinal)
				  && fixedAcceptanceBoundaryLines[0].Contains("全部原条件", StringComparison.Ordinal)
				  && fixedAcceptanceBoundaryLines[0].Contains("立即生效", StringComparison.Ordinal)
				  && CountOccurrences(fixedAcceptanceBoundaryLines[0], "。") == 1,
			"the fixed contract must keep exactly one concise semantic-boundary sentence for accept_*");
		string documentStyleContract = ExtractSection(
			source,
			"private static void AppendDiplomaticDeclarationWritingContract(",
			"private static string BuildDiplomaticDeclarationModeContract(");
		Test.True(documentStyleContract.Contains("以王国", StringComparison.Ordinal)
				  && documentStyleContract.Contains("一个国家对另一个国家的公开评价", StringComparison.Ordinal),
			"the concise style contract must prevent ruler-to-ruler private-chat phrasing before repair");
		string openOfferRequirement = ExtractSection(
			source,
			"private static void AppendOpenOfferAnswerRequirement(",
			"private int FindPriorityThreatRelayIndex(");
		Test.True(openOfferRequirement.Contains("必须按当前可选动作处理", StringComparison.Ordinal)
			&& openOfferRequirement.Contains("accept_*只表示无条件接受全部原条件并立即生效", StringComparison.Ordinal)
			&& !openOfferRequirement.Contains("反提", StringComparison.Ordinal),
			"an answerable live offer must carry the concise unconditional-acceptance rule without advertising unavailable actions");
        foreach (string forbiddenFixedBodyTemplate in new[]
        {
            "本国正式警告贵国",
            "若贵国拒绝或继续该军事行为，本国将向贵国发出最后通牒或正式宣战",
            "这是本国最后通牒",
            "若贵国逾期不履行，本国将正式向贵国宣战",
            "本国无条件接受贵国提议的全部原条件，相关协定立即生效"
        })
        {
            Test.True(!fixedDeclarationContract.Contains(forbiddenFixedBodyTemplate, StringComparison.Ordinal),
                "the fixed contract must state semantics without a literal body template: " + forbiddenFixedBodyTemplate);
        }
        HashSet<string> expectedAnalysisIntents = new(expectedActionableIntents, StringComparer.Ordinal);
        expectedAnalysisIntents.UnionWith(publicNonMechanicalIntents);
        Test.True(analysisIntentEnum.SetEquals(expectedAnalysisIntents),
            "MODE=ANALYZE must expose actual mechanics plus public non-mechanical meanings; actual="
            + string.Join(",", analysisIntentEnum.OrderBy(x => x, StringComparer.Ordinal)));
        Test.True(actionableIntentHelper.Contains("NormalizeIntent(intent)", StringComparison.Ordinal),
            "the actionable whitelist must normalize intent before matching");
        foreach (string intent in expectedActionableIntents)
        {
            Test.True(actionableIntentHelper.Contains("\"" + intent + "\"", StringComparison.Ordinal),
                "actionable intent helper is missing: " + intent);
        }
        foreach (string publicIntent in publicNonMechanicalIntents)
        {
            Test.True(!actionableIntentHelper.Contains("\"" + publicIntent + "\"", StringComparison.Ordinal),
                "mechanical-action helper must not execute public-only type: " + publicIntent);
            Test.True(!declarationIntentTemplate.Contains(publicIntent) && analysisIntentEnum.Contains(publicIntent),
                "AI DECLARE must stay action-scoped while player ANALYZE retains public-only type: " + publicIntent);
        }
        string potentialActions = ExtractSection(
            source,
            "private List<string> BuildPotentialDiplomaticActionIntents(",
            "private static string DescribePotentialDiplomaticActions(");
        string currentLegalOptions = ExtractSection(
            source,
            "private string BuildCurrentLegalDiplomaticOptions(",
            "private List<Kingdom> GetEligibleAiKingdoms(");
        Test.True(!potentialActions.Contains("allowNewWarThreats", StringComparison.Ordinal)
                  && !currentLegalOptions.Contains("TopicCategory", StringComparison.Ordinal),
            "war warnings and ultimatums must not disappear because of relay topic classification");
        Test.True(potentialActions.Contains("CanIssueWarThreat(first, second", StringComparison.Ordinal)
                  && potentialActions.Contains("actions.Add(\"warning\")", StringComparison.Ordinal)
                  && potentialActions.Contains("actions.Add(\"ultimatum\")", StringComparison.Ordinal),
            "a legally eligible peaceful pair must expose warning and ultimatum actions");
        Test.True(potentialActions.Contains("if (atWar)", StringComparison.Ordinal)
                  && potentialActions.Contains("actions.Add(\"propose_peace\")", StringComparison.Ordinal),
            "a pair already at war must retain a substantive peace action");
        string declareWarBoundary = ExtractSection(
            source,
            "private bool CanDeclareWar(",
            "private void CompleteActiveExchange(");
        Test.True(declareWarBoundary.Contains("pendingThreatDecision.TargetDecision", StringComparison.Ordinal)
                  && declareWarBoundary.Contains("等待对象国一次性决定", StringComparison.Ordinal),
            "an issuer must not declare war before the threatened kingdom publishes its one-time decision");
        string threatDocumentProcessing = ExtractSection(
            source,
            "private void ProcessDiplomaticThreatDocument(",
            "private bool DeferUnresolvedRequiredThreatAction(");
        Test.True(threatDocumentProcessing.Contains("string.Equals(x.TargetDecision, \"noncomplied\"", StringComparison.Ordinal),
            "a war may enforce an ultimatum only after explicit target noncompliance");
        string roundPlanParticipants = ExtractSection(
            source,
            "private List<Kingdom> GetRoundPlanActionableParticipants(",
            "private static string DescribePotentialDiplomaticActions(");
        Test.True(roundPlanParticipants.Contains("BuildLegalDiplomaticActionIntents(round, x, author)", StringComparison.Ordinal)
                  && roundPlanParticipants.Contains("ResponseIntentToProposalIntent(intent)", StringComparison.Ordinal),
            "round planning must retain a kingdom that can answer the root author's open proposal");
        Test.True(source.Contains("GetRoundPlanActionableParticipants(author, round)", StringComparison.Ordinal),
            "embedded and fallback round planning must use response-aware candidates");
        string canonicalEntryRenderer = ExtractSection(
            source,
            "private static string RenderCanonicalHistoryEntry(",
            "private static string ProtectedFactStableKey(");
        Test.True(canonicalEntryRenderer.Contains("entry.Intent", StringComparison.Ordinal)
                  && !canonicalEntryRenderer.Contains("IsActionableDiplomacyIntent", StringComparison.Ordinal),
            "statement-style archive entries must remain renderable without treating them as executable mechanics");
        string threatDynamicContext = ExtractSection(
            source,
            "private void AppendDiplomaticThreatDynamicContext(",
            "private void AppendDiplomaticThreatAnalysisContext(");
        Test.True(!fixedDeclarationContract.Contains("warning会建立信誉义务", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains("外交声誉大幅下降", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains("发出前必须权衡后果", StringComparison.Ordinal)
                  && !fixedDeclarationContract.Contains("关系下降20点", StringComparison.Ordinal),
            "the fixed declaration contract must not disclose pre-issuance threat consequences");
		const string warningCondemnationContract = "warning表示谴责，不是劝告、关心或善意提醒；正文必须要求停止具体敌对或军事行为，并说明拒不停止将升级最后通牒或正式宣战。ultimatum表示战争最后通牒。";
		Test.True(fixedDeclarationContract.Contains(warningCondemnationContract, StringComparison.Ordinal),
			"the short DECLARE contract must define warning as a war condemnation with a concrete stop demand and explicit ultimatum-or-war escalation");
		Test.True(!fixedDeclarationContract.Contains("warning与ultimatum只用于战争升级", StringComparison.Ordinal),
			"the former generic warning boundary must be replaced instead of duplicated beside the precise condemnation contract");
		RunWarningCondemnationContractTests(source, expectedActionableIntents, actionableIntentHelper);
        Test.True(threatDynamicContext.Contains("强制后果提示", StringComparison.Ordinal)
                  && threatDynamicContext.Contains("本篇就是本国谴责后的下一份宣言", StringComparison.Ordinal)
                  && threatDynamicContext.Contains("本篇就是本国通牒后的下一份宣言", StringComparison.Ordinal)
                  && threatDynamicContext.Contains("关系下降20点", StringComparison.Ordinal),
            "post-issuance threat consequences must remain in the targeted dynamic context");
        Test.True(!source.Contains("下一份已发布公文必须", StringComparison.Ordinal),
            "canonical history must not turn a past noncompliance result into a global forward-looking instruction");
        Test.True(source.Contains("HasStaleDiplomaticThreatPresentation(job)", StringComparison.Ordinal)
                  && source.Contains("discarded completed generation from stale diplomatic threat stage", StringComparison.Ordinal),
            "queued and in-flight generated declarations must be rebuilt when the threat stage changes");
        Test.True(source.Contains("GetPresentedThreatFollowThroughDocumentIds(document.AuthorKingdomId)", StringComparison.Ordinal),
            "a queued player declaration must still be judged against the issuer obligation current at publication");
        Test.True(source.Contains("WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration", StringComparison.Ordinal)
                  && source.Contains("WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough", StringComparison.Ordinal),
            "the tested pure state rules must be wired into the publication pipeline");
        Test.True(source.Contains("legacy_next_declaration_already_consumed_without_retroactive_penalty", StringComparison.Ordinal),
            "migration must not shift an already-consumed legacy next-declaration duty past the load boundary");
        string reputationPenalty = ExtractSection(
            source,
            "private void ApplyDiplomaticThreatReputationPenalty(",
            "private void RetryDiplomaticThreatDomesticPenalties(");
        Test.True(!reputationPenalty.Contains("TryAppendDiplomaticThreatHistoryResult", StringComparison.Ordinal),
            "the reputation result must not be recorded before its triggering declaration");
        Test.True(source.Contains("NonComplianceEvents", StringComparison.Ordinal),
            "each warning and ultimatum stage must retain an independent noncompliance history retry payload");
        Test.True(source.Contains("Starting the war directly does not erase that broken promise", StringComparison.Ordinal),
            "declaring war directly after a rejected warning must not bypass the required ultimatum");
		Test.True(!source.Contains("threat_semantics_", StringComparison.Ordinal)
				  && !source.Contains("WorldDiplomacyThreatSemantics", StringComparison.Ordinal),
            "structured warning/ultimatum intent must not retain a second prose-based threat classifier or repair reason");
        string threatMigration = ExtractSection(
            source,
            "private void MigrateDiplomaticThreatsToNextDeclarationRules()",
            "private void NormalizeDiplomaticThreats(bool allowWorldValidation)");
        Test.True(!threatMigration.Contains("TryGetViolation", StringComparison.Ordinal)
                  && threatMigration.Contains("followThroughIntent == \"ultimatum\"", StringComparison.Ordinal)
                  && threatMigration.Contains("targetsThreatTarget", StringComparison.Ordinal),
            "legacy threat migration must trust the persisted structured stage/intent while retaining target and sequence checks");
        string threatRegistration = ExtractSection(
            source,
            "private bool RegisterOrAdvanceDiplomaticThreat(",
            "private bool ResolveDiplomaticThreatCompliance(");
        Test.True(!threatRegistration.Contains("TryGetViolation", StringComparison.Ordinal),
            "threat registration must use the validated structured stage instead of re-reading public prose");
        string failedJob = ExtractSection(source, "private void CommitFailedJob(", "private static bool IsAutonomousOpeningJob(");
        Test.True(failedJob.Contains("CommitAnalysis(job, BuildFallbackAnalysisJson(job))", StringComparison.Ordinal),
            "a published analysis fallback must enter the ordinary analysis/publication pipeline");

		RunRoundResponseNoActionContractTests(source);
		RunWarResponseNoActionContractTests(source);
		RunMultiTargetDeclarationContractTests(source);
		RunPermanentAllianceContractTests(source);
        RunPolicyThreatComplianceConsequenceContractTests(source);
        RunThreatStateRuleTests();
        RunOfferCooldownRuleTests();
        RunOfferCooldownIntegrationContractTests(source);
        RunRelayPromptDecisionContextParityContractTests(source);
        RunRelayGlobalRelationshipScopeContractTests(source);
        RunLiveLegalActionPublicationContractTests(source);
        RunGeneratedDraftRepairContractTests(source);
        RunOutputTruncationRepairContractTests(source);
		RunRecoveredDiplomacyRegressionContractTests(source);
		RunNegotiationAndDiplomaticStandingContractTests(source);

        Console.WriteLine("World diplomacy intent-boundary smoke tests passed: " + Test.Assertions);
		return 0;
	}

	private static void RunNegotiationAndDiplomaticStandingContractTests(string source)
	{
		Test.True(source.Contains("[JsonProperty(\"diplomaticReputationByKingdom\")]", StringComparison.Ordinal)
			&& source.Contains("public Dictionary<string, int> NationalPrestigeByKingdom", StringComparison.Ordinal)
			&& source.Contains("[JsonProperty(\"internationalReputationByKingdom\")]", StringComparison.Ordinal)
			&& source.Contains("[JsonProperty(\"internationalReputationNaturalChangeLastDayByKingdom\")]", StringComparison.Ordinal),
			"old diplomatic-reputation saves must migrate in place to prestige while international reputation persists separately");
		Test.True(source.Contains("MaximumInternationalReputationChangePerDocument = 10", StringComparison.Ordinal)
			&& source.Contains("SettleInternationalReputationForDocument(document)", StringComparison.Ordinal)
			&& source.Contains("international_reputation_reason", StringComparison.Ordinal),
			"every new declaration must carry one bounded retrospective international-reputation evaluation into publication settlement");
		string generatedEnvelope = ExtractMethod(source, "private bool TryApplyGeneratedSemanticEnvelope(");
		Test.True(!generatedEnvelope.Contains("json[\"international_reputation_delta\"] == null", StringComparison.Ordinal)
			&& !generatedEnvelope.Contains("json[\"international_reputation_reason\"] == null", StringComparison.Ordinal),
			"missing reputation fields must not reject an otherwise valid declaration or spend a repair API call");
		string reputationEvaluation = ExtractMethod(source, "private static void ApplyInternationalReputationEvaluation(");
		Test.True(reputationEvaluation.Contains("TryReadInteger", StringComparison.Ordinal)
			&& reputationEvaluation.Contains("modelDelta != 0", StringComparison.Ordinal)
			&& reputationEvaluation.Contains("CalculateStructuredInternationalReputationFallback", StringComparison.Ordinal)
			&& reputationEvaluation.Contains("local_structured_fallback", StringComparison.Ordinal),
			"reputation evaluation must use a nonzero model value and a no-call structured fallback for zero or invalid output");
		string reputationFallback = ExtractMethod(source, "private static int CalculateStructuredInternationalReputationFallback(");
		Test.True(reputationFallback.Contains("if (score == 0) score = -1", StringComparison.Ordinal)
			&& reputationFallback.Contains("没有提供新的条件、解释、行动或谈判进展", StringComparison.Ordinal)
			&& reputationFallback.Contains("及时、明确地答复了正式提案", StringComparison.Ordinal),
			"the structured fallback must always return a nonzero result and distinguish clear replies from empty repetition");
		string reputationSettlement = ExtractMethod(source, "private void SettleInternationalReputationForDocument(");
		Test.True(reputationSettlement.Contains("if (delta == 0)", StringComparison.Ordinal)
			&& reputationSettlement.Contains("local_nonzero_settlement_guard", StringComparison.Ordinal),
			"the final settlement boundary must prevent every remaining zero evaluation, including legacy and fallback paths");
		string declarationContract = ExtractMethod(source, "private static string BuildDiplomaticDeclarationModeContract(");
		string analysisContract = ExtractMethod(source, "private static string BuildAnalysisModeContract(");
		Test.True(declarationContract.Contains("只能填写-10到-1或1到10，不得为0", StringComparison.Ordinal)
			&& analysisContract.Contains("只能填写-10到-1或1到10，不得为0", StringComparison.Ordinal)
			&& !declarationContract.Contains("也可以为0", StringComparison.Ordinal)
			&& !analysisContract.Contains("也可以为0", StringComparison.Ordinal),
			"both AI generation and player-declaration analysis contracts must require a retrospective nonzero evaluation");
		string reputationRecovery = ExtractMethod(source, "private void RecoverUnsettledAiInternationalReputation(");
		Test.True(reputationRecovery.Contains("!x.IsPlayerAuthored", StringComparison.Ordinal)
			&& reputationRecovery.Contains("!x.InternationalReputationSettled", StringComparison.Ordinal)
			&& reputationRecovery.Contains("OrderBy(x => x.Day)", StringComparison.Ordinal)
			&& reputationRecovery.Contains("SettleInternationalReputationForDocument(document)", StringComparison.Ordinal)
			&& CountOccurrences(source, "RecoverUnsettledAiInternationalReputation();") == 2,
			"load/session migration must replay each persisted missed AI evaluation once in chronological order");
		Test.True(source.Contains("WarningCompliancePrestigeChange = 5", StringComparison.Ordinal)
			&& source.Contains("UltimatumCompliancePrestigeChange = 10", StringComparison.Ordinal)
			&& source.Contains("WarningEscalationPrestigeReward = 3", StringComparison.Ordinal)
			&& source.Contains("UltimatumWarPrestigeReward = 5", StringComparison.Ordinal),
			"the approved warning, ultimatum, compliance, and war prestige rewards must remain explicit");
		string prestigeTarget = ExtractMethod(source, "private static int GetNationalPrestigeRelationTarget(");
		Test.True(prestigeTarget.Contains("if (prestige >= 80) return 0", StringComparison.Ordinal)
			&& prestigeTarget.Contains("if (prestige >= 60) return -2", StringComparison.Ordinal)
			&& prestigeTarget.Contains("if (prestige >= 40) return -5", StringComparison.Ordinal)
			&& prestigeTarget.Contains("if (prestige >= 20) return -10", StringComparison.Ordinal)
			&& prestigeTarget.Contains("if (prestige >= 1) return -15", StringComparison.Ordinal)
			&& prestigeTarget.Contains("return -20", StringComparison.Ordinal),
			"prestige must drive the approved reversible vassal-leader relation bands");

		string passAccounting = ExtractMethod(source, "private void CompleteRelayPassProgressAccounting(");
		Test.True(passAccounting.Contains("round.DiplomaticActionAttemptCount > round.ActionAttemptCountAtPassStart", StringComparison.Ordinal)
			&& passAccounting.Contains("round.ConsecutiveNoActionPasses + 1", StringComparison.Ordinal),
			"non-mechanical talk must not reset the hard diplomatic-action progress counter");
		string roundPrompt = ExtractMethod(source, "private static void AppendRoundSubstantiveProgressRequirement(");
		Test.True(roundPrompt.Contains("round.ConsecutiveNoActionPasses >= 2", StringComparison.Ordinal)
			&& roundPrompt.Contains("end_negotiation", StringComparison.Ordinal)
			&& roundPrompt.Contains("declare_deadlock", StringComparison.Ordinal),
			"the third no-action phase must force a concrete result, exit, or declared deadlock");
		string resultRules = File.ReadAllText(FindRepositoryFile("WorldDiplomacyResultSettlementRules.cs"), Encoding.UTF8);
		Test.True(resultRules.Contains("kind != WorldDiplomacyConfirmedResultKind.OfferRejected", StringComparison.Ordinal),
			"rejecting one proposal must not be a confirmed terminal result for the whole round");

		string impactBuilder = ExtractMethod(source, "public static string BuildDiplomaticStandingImpactTextForExternal(");
		string boundaryImpact = ExtractMethod(source, "private static string BuildInternationalReputationImpactDeltaText(");
		string timeline = File.ReadAllText(FindRepositoryFile("WorldMessageTimelineUi.cs"), Encoding.UTF8);
		Test.True(impactBuilder.Contains("【外交结果】", StringComparison.Ordinal)
			&& impactBuilder.Contains("【国际声誉】", StringComparison.Ordinal)
			&& impactBuilder.Contains("【国家威望】", StringComparison.Ordinal)
			&& impactBuilder.Contains("变化：", StringComparison.Ordinal)
			&& impactBuilder.Contains("原因：", StringComparison.Ordinal)
			&& timeline.Contains("BuildDiplomaticStandingImpactTextForExternal(document)", StringComparison.Ordinal),
			"the persisted declaration detail must render diplomatic results and standing changes as separate readable sections");
		Test.True(boundaryImpact.Contains("已达上限100", StringComparison.Ordinal)
			&& boundaryImpact.Contains("已达下限0", StringComparison.Ordinal)
			&& boundaryImpact.Contains("无实际变化（评价", StringComparison.Ordinal),
			"a nonzero evaluation absorbed at 0 or 100 must remain visible instead of being mislabeled as a zero evaluation");
		string dailyTick = ExtractMethod(source, "private void OnDailyTick(");
		string naturalChange = ExtractMethod(source, "private void ProcessInternationalReputationNaturalChange(");
		string naturalCalculation = ExtractMethod(source, "private static int CalculateInternationalReputationNaturalChange(");
		Test.True(source.Contains("InternationalReputationNaturalAnchor = 20", StringComparison.Ordinal)
			&& source.Contains("InternationalReputationFastDecayMinimum = 71", StringComparison.Ordinal)
			&& source.Contains("InternationalReputationNormalDecayMinimum = 51", StringComparison.Ordinal)
			&& source.Contains("InternationalReputationSlowDecayMinimum = 21", StringComparison.Ordinal)
			&& source.Contains("InternationalReputationDailyIntervalDays = 1", StringComparison.Ordinal)
			&& source.Contains("InternationalReputationFastDecayStep = 3", StringComparison.Ordinal)
			&& source.Contains("InternationalReputationNormalDecayStep = 2", StringComparison.Ordinal),
			"international reputation must naturally converge on 20 using the approved fast, normal, slow, stable, and recovery bands");
		Test.True(dailyTick.Contains("AnchorInternationalReputationNaturalChangeDays();", StringComparison.Ordinal)
			&& dailyTick.Contains("ProcessInternationalReputationNaturalChange();", StringComparison.Ordinal)
			&& naturalChange.Contains("never apply retroactive decay", StringComparison.Ordinal)
			&& naturalChange.Contains("updated == InternationalReputationNaturalAnchor", StringComparison.Ordinal),
			"daily processing must avoid retroactive old-save or disabled-period decay and reset accumulated time at the stable anchor");
		Test.True(naturalCalculation.Contains("availableTicks = remainingDays / intervalDays", StringComparison.Ordinal)
			&& naturalCalculation.Contains("Math.Min(availableTicks, maximumTicksInBand)", StringComparison.Ordinal)
			&& CountOccurrences(naturalCalculation, "intervalDays = InternationalReputationDailyIntervalDays;") == 4
			&& naturalCalculation.Contains("step = -InternationalReputationFastDecayStep", StringComparison.Ordinal)
			&& naturalCalculation.Contains("step = -InternationalReputationNormalDecayStep", StringComparison.Ordinal)
			&& naturalCalculation.Contains("step = 1", StringComparison.Ordinal),
			"all natural reputation bands must tick daily while long time skips remain batched instead of scanning every elapsed day");
		string popup = File.ReadAllText(FindRepositoryFile("CourierLetterReplyPopup.cs"), Encoding.UTF8);
		string popupVm = File.ReadAllText(FindRepositoryFile("CourierLetterReplyPopupVM.cs"), Encoding.UTF8);
		string repositoryRoot = Path.GetDirectoryName(FindRepositoryFile("WorldDiplomacyBehavior.cs"))!;
		string popupPrefab = File.ReadAllText(Path.Combine(repositoryRoot, "AnimusForge", "GUI", "Prefabs", "CourierLetterReplyPopup.xml"), Encoding.UTF8);
		Test.True(popup.Contains("string impactText = null", StringComparison.Ordinal)
			&& popupVm.Contains("public string ImpactText", StringComparison.Ordinal)
			&& popupVm.Contains("public bool HasImpact", StringComparison.Ordinal)
			&& popupPrefab.Contains("Text=\"@ImpactText\"", StringComparison.Ordinal),
			"the formal letter popup must bind the standing changes and reasons into its right-side impact area");
		string encyclopedia = File.ReadAllText(FindRepositoryFile("EncyclopediaKingdomStabilityPatch.cs"), Encoding.UTF8);
		Test.True(encyclopedia.Contains("BuildKingdomDiplomaticStandingEncyclopediaTextForExternal", StringComparison.Ordinal),
			"kingdom encyclopedia refresh must append prestige and international reputation beside stability");
		string encyclopediaStanding = ExtractMethod(source, "public static string BuildKingdomDiplomaticStandingEncyclopediaTextForExternal(");
		Test.True(encyclopediaStanding.Contains("该国的外交信用与威慑", StringComparison.Ordinal)
			&& encyclopediaStanding.Contains("他国对该国的评价", StringComparison.Ordinal),
			"kingdom encyclopedia standing values must include concise player-facing explanations");
	}

	private static void RunRecoveredDiplomacyRegressionContractTests(string source)
	{
		string authorGate = ExtractMethod(source, "private static bool CanAiAuthorDiplomaticDocument(");
		Test.True(authorGate.Contains("ruler.IsPrisoner", StringComparison.Ordinal)
			&& authorGate.Contains("player_controlled_realm_requires_player_authorization", StringComparison.Ordinal),
			"AI diplomatic authorship must reject captive rulers and player-ruled realms");
		Test.True(CountOccurrences(source, "CanAiAuthorDiplomaticDocument(") >= 8,
			"AI author authority must be checked at scheduling, request, commit, propagation, and execution boundaries");
		string mandatoryResponse = ExtractMethod(source, "private void TryScheduleMandatoryCourtResponse(");
		Test.True(mandatoryResponse.Contains("CanAiAuthorDiplomaticDocument(receiver", StringComparison.Ordinal)
			&& mandatoryResponse.Contains("ruler_is_prisoner", StringComparison.Ordinal)
			&& mandatoryResponse.Contains("王庭暂时无法正式回应你的宣言", StringComparison.Ordinal),
			"a player declaration delivered to a captive ruler must receive an explicit unable-to-respond notice instead of hanging");
		Test.True(mandatoryResponse.IndexOf("CanAiAuthorDiplomaticDocument(receiver", StringComparison.Ordinal)
			< mandatoryResponse.IndexOf("if (round.ResultSettlementPending)", StringComparison.Ordinal),
			"captive-ruler feedback must not be bypassed when the round has already entered result settlement");

		string courtArrival = ExtractMethod(source, "private void ProcessCourtArrival(");
		Test.True(courtArrival.Contains("IsPlayerAffiliatedKingdom(receiver)", StringComparison.Ordinal),
			"formal court arrival must work for player rulers and player vassals");
		string propagation = ExtractMethod(source, "private void StartDocumentPropagation(");
		Test.True(propagation.Contains("IsPlayerAffiliatedKingdom(author)", StringComparison.Ordinal)
			&& propagation.Contains("document.HasReachedPlayerCourt = true", StringComparison.Ordinal),
			"a declaration authored at the player-affiliated sovereign court must be formally available immediately");
		Test.True(propagation.Contains("playerCourtReceiptMissing", StringComparison.Ordinal)
			&& propagation.Contains("knownKingdomIds.Contains", StringComparison.Ordinal),
			"relay knowledge must not suppress a still-missing formal player-court delivery");
		string propagationArrivals = ExtractMethod(source, "private void ProcessPropagationArrivals(");
		Test.True(propagationArrivals.Contains("newlyKnown || (IsPlayerAffiliatedKingdom(receiver) && !document.HasReachedPlayerCourt)", StringComparison.Ordinal),
			"an already-known declaration must still complete its formal player-court receipt");
		string relayArrivals = ExtractMethod(source, "private void ProcessRelayArrivals(");
		Test.True(CountOccurrences(relayArrivals, "MarkPlayerCourtReachedByRelay") == 2,
			"both ordinary and result-settlement relays must mark formal delivery when they reach the player court");
		string receiptRecovery = ExtractMethod(source, "private void RecoverPlayerCourtReceiptsFromKnowledge(");
		Test.True(receiptRecovery.Contains("knownDocumentIds", StringComparison.Ordinal)
			&& source.Contains("RecoverPlayerCourtReceiptsFromKnowledge();", StringComparison.Ordinal),
			"old saves whose player court already knows a declaration must recover the missing formal receipt flag");
		string notifications = ExtractMethod(source, "private void TryPublishPendingNotifications(");
		Test.True(notifications.Contains("!x.RumorNotified", StringComparison.Ordinal)
			&& notifications.Contains("x.HasReachedPlayerCourt", StringComparison.Ordinal)
			&& notifications.Contains("!x.FormalNoticeShown", StringComparison.Ordinal),
			"rumor and formal court delivery must remain separate notification stages");
		Test.True(notifications.Contains("_nextNotificationPollUtc", StringComparison.Ordinal),
			"per-frame diplomacy notification work must remain throttled");

		string knownDocuments = ExtractMethod(source, "private HashSet<string> GetKnownDocumentIdsForHero(");
		Test.True(!knownDocuments.Contains("Settlement.CurrentSettlement", StringComparison.Ordinal),
			"a remote NPC must not inherit the player's current-settlement diplomatic knowledge");
		string sharedMemoryPatch = ExtractMethod(source, "private static void Patch_BuildSharedDiplomacyMemory_Postfix(");
		Test.True(sharedMemoryPatch.Contains("ShouldInjectDiplomacyMemoryForInput", StringComparison.Ordinal)
			&& sharedMemoryPatch.Contains("BuildDiplomacyMemoryContext(hero, kingdomIdOverride, input)", StringComparison.Ordinal),
			"explicit player questions about known diplomacy must inject query-aware shared memory");
		string detailedMemory = ExtractMethod(source, "private static string BuildDetailedDocumentMemoryLine(");
		Test.True(detailedMemory.Contains("document.Body", StringComparison.Ordinal)
			&& detailedMemory.Contains("具体诉求与条件", StringComparison.Ordinal),
			"known declarations must expose concrete demands to NPC conversation memory");

		string archive = ExtractMethod(source, "private WorldEventInboxPopupData BuildRoyalAnnouncementArchiveData(");
		Test.True(archive.Contains("x.IsPlayerAuthored || x.IsReadyForPublication", StringComparison.Ordinal)
			&& !archive.Contains("x.IsReadyForPublication && x.HasReachedPlayerCourt", StringComparison.Ordinal),
			"the U-key archive must expose every published declaration immediately, even before the player joins a kingdom");
		Test.True(notifications.Contains("x.HasReachedPlayerCourt", StringComparison.Ordinal),
			"the right-side formal notice must still wait for delivery to the player's affiliated court");
		Test.True(archive.Contains("IndexTitleText = BuildArchiveIndexDocumentTitle(document)", StringComparison.Ordinal)
			&& archive.Contains("IndexMetaText = \"外交宣言：\" + typeLabel", StringComparison.Ordinal),
			"the archive index must show a clean title and centered declaration type line");
		string displayedTitle = ExtractMethod(source, "private string BuildDisplayedDocumentTitle(");
		Test.True(!displayedTitle.Contains("外交事件开始", StringComparison.Ordinal)
			&& !displayedTitle.Contains("外交事件结束", StringComparison.Ordinal)
			&& !displayedTitle.Contains("外交事件始末", StringComparison.Ordinal),
			"formal declaration popups must show only the original declaration title");
	}

    private static void RunRelayPromptDecisionContextParityContractTests(string source)
    {
		Test.True(source.Contains(
				"private const int DiplomacyPromptContractVersion = 28;",
				StringComparison.Ordinal),
			"the exact own-reputation contract must advance the dynamic prompt contract to v28");
		Test.True(source.Contains(
				"private const string CanonicalHistoryCacheAffinityKey = \"diplomacy-history:v28\";",
				StringComparison.Ordinal),
			"the exact own-reputation prompt must advance canonical-history cache affinity to v28");

		string ownStandingContext = ExtractMethod(
			source,
			"private void AppendDiplomaticThreatDynamicContext(");
		Test.True(ownStandingContext.Contains("GetInternationalReputation(author.StringId)", StringComparison.Ordinal)
			&& ownStandingContext.Contains("DescribeInternationalReputation(reputation)", StringComparison.Ordinal)
			&& ownStandingContext.Contains("DescribeInternationalReputationNaturalTrend(reputation)", StringComparison.Ordinal)
			&& ownStandingContext.Contains("GetRecentOwnInternationalReputationReasons(author.StringId)", StringComparison.Ordinal)
			&& ownStandingContext.Contains("reputation.ToString(CultureInfo.InvariantCulture)", StringComparison.Ordinal)
			&& ownStandingContext.Contains("现实局势允许时可主动发表有实际内容的宣言维护声誉", StringComparison.Ordinal)
			&& ownStandingContext.Contains("string.Equals(x.Kind, \"national_prestige\"", StringComparison.Ordinal),
			"the declaring kingdom must receive its exact reputation, tier, trend, and recent reasons so substantive diplomacy can maintain it");
		string ownReputationReasons = ExtractMethod(
			source,
			"private List<string> GetRecentOwnInternationalReputationReasons(");
		Test.True(ownReputationReasons.Contains("MaxPromptRecentOwnReputationReasons", StringComparison.Ordinal)
			&& ownReputationReasons.Contains("document.IsReadyForPublication", StringComparison.Ordinal)
			&& ownReputationReasons.Contains("document.InternationalReputationSettled", StringComparison.Ordinal)
			&& ownReputationReasons.Contains("评价方向=", StringComparison.Ordinal)
			&& !ownReputationReasons.Contains("FormatSignedDelta", StringComparison.Ordinal),
			"own-state prompt reasons must be bounded, settled public facts with qualitative direction and no exact delta");

		string foreignStandingContext = ExtractMethod(
			source,
			"private void AppendDiplomaticTargetDecisionContext(");
		string compactForeignStandingContext = ExtractMethod(
			source,
			"private string BuildCompactDiplomaticRelationshipLine(");
		Test.True(foreignStandingContext.Contains("DescribeInternationalReputation(targetReputation)", StringComparison.Ordinal)
			&& !foreignStandingContext.Contains("GetInternationalReputation(targetId).ToString", StringComparison.Ordinal)
			&& compactForeignStandingContext.Contains("DescribeInternationalReputation(candidateReputation)", StringComparison.Ordinal)
			&& !compactForeignStandingContext.Contains("Append(GetInternationalReputation", StringComparison.Ordinal),
			"foreign kingdoms may receive a public reputation tier, but exact foreign reputation scores must stay out of declaration prompts");

		string reputationConflictOpportunity = ExtractMethod(
			source,
			"private string BuildLowReputationConflictOpportunityContext(");
		string reputationConflictFacts = ExtractMethod(
			source,
			"private List<string> GetRecentPublicNegativeReputationFacts(");
		string compactCandidateContext = ExtractMethod(
			source,
			"private string BuildCompactRoundPlanCandidateLine(");
		Test.True(source.Contains("LowInternationalReputationThreshold = 40", StringComparison.Ordinal)
			&& source.Contains("SevereInternationalReputationThreshold = 20", StringComparison.Ordinal)
			&& reputationConflictOpportunity.Contains("x is \"warning\" or \"ultimatum\"", StringComparison.Ordinal)
			&& reputationConflictOpportunity.Contains("【国际声誉冲突机会】", StringComparison.Ordinal)
			&& reputationConflictOpportunity.Contains("不是强制行动", StringComparison.Ordinal)
			&& reputationConflictOpportunity.Contains("不得编造", StringComparison.Ordinal),
			"only low-reputation foreign targets with a currently legal warning or ultimatum may expose the bounded conflict-opportunity guidance");
		Test.True(reputationConflictFacts.Contains("document.IsReadyForPublication", StringComparison.Ordinal)
			&& reputationConflictFacts.Contains("document.InternationalReputationSettled", StringComparison.Ordinal)
			&& reputationConflictFacts.Contains("document.InternationalReputationEvaluationDelta >= 0", StringComparison.Ordinal)
			&& reputationConflictFacts.Contains("RecentNegativeReputationFactRetentionDays", StringComparison.Ordinal)
			&& reputationConflictFacts.Contains("MaxPromptRecentNegativeReputationFacts", StringComparison.Ordinal),
			"low-reputation guidance must cite at most two recent, published, settled, actually negative reputation facts");
		Test.True(foreignStandingContext.Contains("BuildLowReputationConflictOpportunityContext(target, legalActions)", StringComparison.Ordinal)
			&& compactCandidateContext.Contains("BuildLowReputationConflictOpportunityContext(candidate, actions)", StringComparison.Ordinal),
			"both detailed targets and autonomous candidate cards must receive the same legal-action-gated low-reputation guidance");

		string declarationContract = ExtractMethod(
			source,
			"private static string BuildDiplomaticDeclarationModeContract(");
		Test.True(declarationContract.Contains("国家生存与现实利益、长期战略", StringComparison.Ordinal)
			&& declarationContract.Contains("国家性格决定本国多看重守约与可靠", StringComparison.Ordinal)
			&& declarationContract.Contains("不得单独触发或阻止宣战", StringComparison.Ordinal)
			&& declarationContract.Contains("高声誉不等于爱好和平", StringComparison.Ordinal)
			&& declarationContract.Contains("需要由持续行为维护的战略资本", StringComparison.Ordinal)
			&& declarationContract.Contains("国家性格决定愿意为信誉付出多少代价", StringComparison.Ordinal)
			&& declarationContract.Contains("长期战略决定希望维持何种档位", StringComparison.Ordinal)
			&& declarationContract.Contains("不能刷取声誉", StringComparison.Ordinal),
			"personality and long-term strategy must interpret foreign reputation without turning it into an automatic peace or war switch");

        string authorContext = ExtractMethod(
            source,
            "private void AppendDiplomaticAuthorDecisionContext(");
        foreach (string requiredAuthorFactBuilder in new[]
        {
            "KingdomName(author)",
            "RulerName(author)",
            "BuildWorldDiplomacyVassalageSnapshot",
            "AppendDiplomaticThreatDynamicContext",
            "BuildRulerVoiceContext",
            "BuildRealmInstitutionalVoiceContext",
            "BuildAuthorRulerFamilyContext",
            "WorldDiplomacyPolicyContext.BuildSnapshot"
        })
        {
            Test.True(authorContext.Contains(requiredAuthorFactBuilder, StringComparison.Ordinal),
                "the shared author decision context must retain: " + requiredAuthorFactBuilder);
        }

        string targetContext = ExtractMethod(
            source,
            "private void AppendDiplomaticTargetDecisionContext(");
        foreach (string requiredTargetFactBuilder in new[]
        {
            "BuildBilateralState",
            "BuildBilateralRulerFamilyContext",
            "BuildRecentBilateralBattleContext",
            "GetKingdomBorderRelation",
            "GetRealmRelationProfile",
            "GetRulerRelation",
            "CountCulturalClaims",
            "GetWarPressure",
            "BuildRecentNativeSignalContext",
            "WorldDiplomacyPolicyContext.BuildSnapshot",
            "BuildWarDecisionContext"
        })
        {
            Test.True(targetContext.Contains(requiredTargetFactBuilder, StringComparison.Ordinal),
                "the shared target decision context must retain: " + requiredTargetFactBuilder);
        }
        Test.True(targetContext.Contains("includePeaceNegotiationTerms", StringComparison.Ordinal),
            "the shared target context must preserve the necessary immediate-war-response peace-term difference");
        foreach (string singleTargetFactBuilder in new[]
        {
            "BuildRecentBilateralBattleContext",
            "GetKingdomBorderRelation",
            "GetRealmRelationProfile",
            "WorldDiplomacyPolicyContext.BuildSnapshot",
            "BuildWarDecisionContext"
        })
        {
            Test.True(CountOccurrences(targetContext, singleTargetFactBuilder) == 1,
                "each target must receive one bounded shared fact block, without duplicate token cost: "
                + singleTargetFactBuilder);
        }

        string warDecisionContext = ExtractMethod(
            source,
            "private string BuildWarDecisionContext(");
        foreach (string requiredWarFactBuilder in new[]
        {
            "DescribeWarDuration",
            "DescribeStrengthBalance",
            "DescribeWarProgress",
            "DescribeOtherWarBurden"
        })
        {
            Test.True(warDecisionContext.Contains(requiredWarFactBuilder, StringComparison.Ordinal),
                "war-state context must always retain: " + requiredWarFactBuilder);
        }
        foreach (string peaceOnlyFactBuilder in new[]
        {
            "DescribePeacePressure",
            "BuildCessionCandidates",
            "SuggestedTribute"
        })
        {
            Test.True(warDecisionContext.Contains(peaceOnlyFactBuilder, StringComparison.Ordinal),
                "ordinary war negotiation context must retain: " + peaceOnlyFactBuilder);
        }
        int positivePeaceTermsGate = warDecisionContext.IndexOf(
            "if (includePeaceNegotiationTerms)",
            StringComparison.Ordinal);
        int negativePeaceTermsGate = warDecisionContext.IndexOf(
            "if (!includePeaceNegotiationTerms)",
            StringComparison.Ordinal);
        int peacePressure = warDecisionContext.IndexOf("DescribePeacePressure", StringComparison.Ordinal);
        int cessionCandidates = warDecisionContext.IndexOf("BuildCessionCandidates", StringComparison.Ordinal);
        int negativeGateReturn = negativePeaceTermsGate < 0
            ? -1
            : warDecisionContext.IndexOf("return", negativePeaceTermsGate, StringComparison.Ordinal);
        bool peaceTermsAreGated = positivePeaceTermsGate >= 0
            ? peacePressure > positivePeaceTermsGate && cessionCandidates > positivePeaceTermsGate
            : negativePeaceTermsGate >= 0
              && negativeGateReturn > negativePeaceTermsGate
              && negativeGateReturn < peacePressure && negativeGateReturn < cessionCandidates;
        Test.True(peaceTermsAreGated,
            "peace pressure, tribute, and cession terms must be gated without hiding the underlying war state");

        string warNegotiationWrapper = ExtractMethod(
            source,
            "private string BuildWarNegotiationContext(");
        Test.True(warNegotiationWrapper.Contains("BuildWarDecisionContext", StringComparison.Ordinal)
                  && warNegotiationWrapper.Contains("true", StringComparison.Ordinal)
                  && !warNegotiationWrapper.Contains("DescribeWarDuration", StringComparison.Ordinal)
                  && !warNegotiationWrapper.Contains("BuildCessionCandidates", StringComparison.Ordinal),
            "BuildWarNegotiationContext must remain a thin full-terms wrapper over the shared war decision context");

        string autonomousPrompt = ExtractMethod(
            source,
            "private string BuildAutonomousOpeningPrompt(");
        string targetedPrompt = ExtractMethod(
            source,
            "private string BuildGenerationPrompt(");
        string relayPrompt = ExtractMethod(
            source,
            "private string BuildRelayConversationTurnPrompt(");
        foreach ((string Name, string Prompt) requestPath in new[]
        {
            ("autonomous opening", autonomousPrompt),
            ("targeted generation", targetedPrompt),
            ("relay response", relayPrompt)
        })
        {
            Test.True(CountOccurrences(
                    requestPath.Prompt,
                    "AppendDiplomaticAuthorDecisionContext(") == 1,
                requestPath.Name + " must append the shared author decision context exactly once");
        }

        Test.True(CountOccurrences(
                targetedPrompt,
                "AppendDiplomaticTargetDecisionContext(") == 1
                  && CountOccurrences(
                      relayPrompt,
                      "AppendDiplomaticTargetDecisionContext(") == 1,
            "targeted and relay request bodies must share one target-fact renderer without duplicate blocks");
        Test.True(targetedPrompt.Contains("bool canProposePeace = legalActions.Any", StringComparison.Ordinal)
                  && targetedPrompt.Contains("\"propose_peace\"", StringComparison.Ordinal)
                  && targetedPrompt.Contains("includePeaceNegotiationTerms: canProposePeace", StringComparison.Ordinal),
            "targeted war context must expose negotiable terms only when propose_peace is currently legal");
        Test.True(relayPrompt.Contains("targetActions.Any", StringComparison.Ordinal)
                  && relayPrompt.Contains("\"propose_peace\"", StringComparison.Ordinal)
                  && relayPrompt.Contains("includePeaceNegotiationTerms", StringComparison.Ordinal),
            "relay war context must keep hard war facts while deriving peace-term visibility from the live propose_peace action");

        foreach (string duplicatedAuthorFactBuilder in new[]
        {
            "BuildWorldDiplomacyVassalageSnapshot",
            "BuildRulerVoiceContext(author)",
            "BuildRealmInstitutionalVoiceContext(author)",
            "BuildAuthorRulerFamilyContext(author)"
        })
        {
            Test.True(!autonomousPrompt.Contains(duplicatedAuthorFactBuilder, StringComparison.Ordinal)
                      && !targetedPrompt.Contains(duplicatedAuthorFactBuilder, StringComparison.Ordinal)
                      && !relayPrompt.Contains(duplicatedAuthorFactBuilder, StringComparison.Ordinal),
                "request renderers must not restore direct duplicated author blocks outside the shared helper: "
                + duplicatedAuthorFactBuilder);
        }
        foreach (string duplicatedTargetFactBuilder in new[]
        {
            "BuildRecentBilateralBattleContext(author, target)",
            "BuildWarNegotiationContext(author, target)"
        })
        {
            Test.True(!targetedPrompt.Contains(duplicatedTargetFactBuilder, StringComparison.Ordinal)
                      && !relayPrompt.Contains(duplicatedTargetFactBuilder, StringComparison.Ordinal),
                "targeted and relay renderers must not restore direct duplicated target blocks outside the shared helper: "
                + duplicatedTargetFactBuilder);
        }

        Test.True(relayPrompt.Contains("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal)
                  && relayPrompt.Contains("AppendOpenOfferAnswerRequirement(", StringComparison.Ordinal)
                  && relayPrompt.Contains("待回应提议=", StringComparison.Ordinal),
            "relay-only legal actions and pending-answer duties must remain explicit necessary differences");
        Test.True(CountOccurrences(relayPrompt, "BuildCurrentLegalDiplomaticOptions(") == 1
                  && CountOccurrences(targetedPrompt, "BuildLegalDiplomaticDeclarationIntents(") == 1
                  && CountOccurrences(autonomousPrompt, "BuildCompactRoundPlanCandidateLine(") == 1,
            "each request path must render its slot-aware current action source exactly once");
    }

    private static void RunRelayGlobalRelationshipScopeContractTests(string source)
    {
        string relayPrompt = ExtractMethod(
            source,
            "private string BuildRelayConversationTurnPrompt(");
        string targetedPrompt = ExtractMethod(
            source,
            "private string BuildGenerationPrompt(");
        string otherKingdomContext = ExtractMethod(
            source,
            "private void AppendOtherKingdomRelationshipContext(");
        string compactRelationship = ExtractMethod(
            source,
            "private string BuildCompactDiplomaticRelationshipLine(");
        string compactCandidate = ExtractMethod(
            source,
            "private string BuildCompactRoundPlanCandidateLine(");

        Test.True(relayPrompt.Contains(
                      "AppendOtherKingdomRelationshipContext(sb, author, legalTargetIds)",
                      StringComparison.Ordinal),
            "relay responses must append relationship knowledge for kingdoms outside their live action targets");
        Test.True(relayPrompt.Contains(
                      ": (round?.RelayRouteKingdomIds ?? new List<string>())",
                      StringComparison.Ordinal)
                  && relayPrompt.Contains(
                      "BuildLegalDiplomaticDeclarationIntentMap(",
                      StringComparison.Ordinal)
                  && relayPrompt.Contains(
                      "legalTargetIds = legalActionsByTarget.Keys",
                      StringComparison.Ordinal),
            "relay action authorization must remain limited to the route or current settlement targets");
        Test.True(targetedPrompt.Contains(
                      "AppendOtherKingdomRelationshipContext(",
                      StringComparison.Ordinal)
                  && targetedPrompt.Contains(
                      "if (isResponse || isExternalResponseOnly || sourceDocument != null)",
                      StringComparison.Ordinal)
                  && targetedPrompt.Contains(
                      "AppendOtherKingdomRelationshipContext(sb, author, new[] { targetId })",
                      StringComparison.Ordinal),
            "targeted response requests must receive the same all-kingdom relationship knowledge as relay responses");

        Test.True(CountOccurrences(otherKingdomContext, "Kingdom.All") == 1,
            "relationship knowledge must enumerate Kingdom.All once per prompt build");
        foreach (string sovereignFilter in new[]
        {
            "!x.IsEliminated",
            "HasIndependentWorldDiplomacyAuthority(x)"
        })
        {
            Test.True(otherKingdomContext.Contains(sovereignFilter, StringComparison.Ordinal),
                "relationship knowledge must include the sovereign-world filter: " + sovereignFilter);
        }
        Test.True(otherKingdomContext.Contains(
                      ".OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)",
                      StringComparison.Ordinal),
            "other-kingdom relationship rows must render in deterministic kingdom-id order");
        Test.True(otherKingdomContext.Contains("detailedTargetIds", StringComparison.Ordinal)
                  && otherKingdomContext.Contains("author.StringId", StringComparison.Ordinal)
                  && otherKingdomContext.Contains("!excludedIds.Contains(x.StringId)", StringComparison.Ordinal),
            "fully detailed live action targets must be excluded from the compact relationship rows");
        Test.True(otherKingdomContext.Contains(
                      "BuildCompactDiplomaticRelationshipLine(author, other)",
                      StringComparison.Ordinal),
            "non-action kingdoms must use the shared compact relationship renderer");
        Test.True(otherKingdomContext.Contains("不授予额外动作", StringComparison.Ordinal),
            "the relationship-only block must state that it grants no extra target authorization");

        foreach (string relationshipFact in new[]
        {
            "BuildBilateralState",
            "GetRealmRelationProfile",
            "GetRulerRelation",
            "GetKingdomBorderRelation",
            "GetWarSituation",
            "DescribeStrengthBalance",
            "WorldDiplomacyPolicyContext.BuildSnapshot"
        })
        {
            Test.True(compactRelationship.Contains(relationshipFact, StringComparison.Ordinal),
                "compact relationship knowledge must retain: " + relationshipFact);
        }
        Test.True(!compactRelationship.Contains("BuildLegal", StringComparison.Ordinal)
                  && !compactRelationship.Contains("可选动作", StringComparison.Ordinal),
            "relationship-only rows must never calculate or imply legal diplomatic actions");
        Test.True(compactCandidate.Contains(
                      "BuildCompactDiplomaticRelationshipLine(initiator, candidate)",
                      StringComparison.Ordinal)
                  && compactCandidate.Contains(
                      "BuildLegalDiplomaticActionIntents(round, initiator, candidate)",
                      StringComparison.Ordinal),
            "opening candidate rows must reuse the same relationship facts and append their own live action grant");

        Test.True(otherKingdomContext.Contains(
                      "FactionManager.IsAtWarAgainstFaction(author, other)",
                      StringComparison.Ordinal)
                  && otherKingdomContext.Contains(
                      "BuildWarDecisionContext(author, other, false)",
                      StringComparison.Ordinal),
            "relationship-only wartime rows must include hard war posture without peace-negotiation terms");
        foreach (string forbiddenActionBuilder in new[]
        {
            "BuildLegalDiplomaticActionIntents",
            "BuildLegalDiplomaticDeclarationIntents",
            "BuildLegalDiplomaticDeclarationIntentMap",
            "BuildCurrentLegalDiplomaticOptions"
        })
        {
            Test.True(!otherKingdomContext.Contains(forbiddenActionBuilder, StringComparison.Ordinal),
                "relationship-only kingdoms must not enter action authorization: " + forbiddenActionBuilder);
        }
        Test.True(!otherKingdomContext.Contains("BuildWarNegotiationContext", StringComparison.Ordinal)
                  && !otherKingdomContext.Contains("BuildCessionCandidates", StringComparison.Ordinal)
                  && !otherKingdomContext.Contains("SuggestedTribute", StringComparison.Ordinal),
            "relationship-only wartime knowledge must not disclose peace, tribute, or cession terms");
    }

	private static void RunWarningCondemnationContractTests(
		string source,
		IReadOnlySet<string> expectedActionableIntents,
		string actionableIntentHelper)
	{
		Test.True(expectedActionableIntents.Contains("warning")
			&& !expectedActionableIntents.Contains("condemn")
			&& actionableIntentHelper.Contains("\"warning\"", StringComparison.Ordinal)
			&& !actionableIntentHelper.Contains("\"condemn\"", StringComparison.Ordinal),
			"warning must remain the internal action token and condemn must remain retired");

		string potentialActions = ExtractSection(
			source,
			"private List<string> BuildPotentialDiplomaticActionIntents(",
			"private static string DescribePotentialDiplomaticActions(");
		string threatRegistration = ExtractSection(
			source,
			"private bool RegisterOrAdvanceDiplomaticThreat(",
			"private bool ResolveDiplomaticThreatCompliance(");
		Test.True(potentialActions.Contains("actions.Add(\"warning\")", StringComparison.Ordinal)
			&& !potentialActions.Contains("actions.Add(\"condemn\")", StringComparison.Ordinal)
			&& threatRegistration.Contains("Stage = \"warning\"", StringComparison.Ordinal)
			&& threatRegistration.Contains("WarningDocumentId", StringComparison.Ordinal),
			"the wording change must not rename warning state, persistence, or action fields");

		string intentLabel = ExtractSection(
			source,
			"private static string IntentLabel(",
			"private static string BuildFallbackDocumentTitle(");
		string fallbackTitle = ExtractSection(
			source,
			"private static string BuildFallbackDocumentTitle(",
			"private static string DocumentTypeLabel(");
		string documentTypeLabel = ExtractSection(
			source,
			"private static string DocumentTypeLabel(",
			"private static string BuildNotificationDescription(");
		foreach ((string name, string section) in new[]
		{
			("IntentLabel", intentLabel),
			("fallback title", fallbackTitle),
			("DocumentTypeLabel", documentTypeLabel)
		})
		{
			Test.True(section.Contains("谴责", StringComparison.Ordinal)
				&& !section.Contains("\"warning\" => \"警告", StringComparison.Ordinal)
				&& !section.Contains("\"warning\" => \"外交警告", StringComparison.Ordinal),
				name + " must expose warning as 谴责 without restoring the old 警告 wording");
		}

		string complianceResult = ExtractSection(
			source,
			"private bool ResolveDiplomaticThreatCompliance(",
			"private void ResolveDiplomaticThreatsAfterWarStarted(");
		string restoredComplianceResult = ExtractSection(
			source,
			"private void UpdateDiplomaticThreatComplianceDocumentResult(",
			"private void FinalizeDiplomaticThreatHistoryAfterDocument(");
		string nonComplianceHistoryResult = ExtractSection(
			source,
			"private void TryAppendDiplomaticThreatNonComplianceHistoryResult(",
			"private void TryAppendDiplomaticThreatHistoryResult(");
		string breachHistoryResult = ExtractSection(
			source,
			"private void TryAppendDiplomaticThreatHistoryResult(",
			"private bool HasOpenDiplomaticThreatForRound(");
		foreach ((string name, string section) in new[]
		{
			("immediate compliance result", complianceResult),
			("restored compliance result", restoredComplianceResult),
			("noncompliance history result", nonComplianceHistoryResult),
			("breach history result", breachHistoryResult)
		})
		{
			Test.True(section.Contains("谴责", StringComparison.Ordinal)
				&& !section.Contains("警告", StringComparison.Ordinal),
				name + " must use the player-visible war-condemnation wording");
		}
	}

    private static void RunOfferCooldownRuleTests()
    {
        foreach (string failedStatus in new[]
        {
            "rejected", "expired", "countered", "superseded", "execution_failed",
            "open"
        })
        {
            List<WorldDiplomacyOfferCooldownDecision> failed =
                WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(new[]
                {
                    new WorldDiplomacyOfferRoundObservation(
                        "kingdom_a", "kingdom_b", "propose_trade", failedStatus)
                });
            AssertOfferCooldownDecision(
                failed,
                "kingdom_a",
                "kingdom_b",
                WorldDiplomacyOfferDomain.Trade,
                WorldDiplomacyOfferCooldownAction.StartCooldown,
                "a closed trade proposal with status " + failedStatus + " must start cooldown");
        }

        List<WorldDiplomacyOfferCooldownDecision> accepted =
            WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(new[]
            {
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_alliance", "accepted")
            });
        Test.True(accepted.Count == 2,
            "one successful bilateral domain must clear both old directed cooldown keys");
        AssertOfferCooldownDecision(
            accepted,
            "kingdom_a",
            "kingdom_b",
            WorldDiplomacyOfferDomain.Alliance,
            WorldDiplomacyOfferCooldownAction.ClearCooldown,
            "a successful alliance acceptance must clear rather than start cooldown");
        AssertOfferCooldownDecision(
            accepted,
            "kingdom_b",
            "kingdom_a",
            WorldDiplomacyOfferDomain.Alliance,
            WorldDiplomacyOfferCooldownAction.ClearCooldown,
            "a successful alliance acceptance must also clear the reverse historical direction");

        List<WorldDiplomacyOfferCooldownDecision> partiallyExecuted =
            WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(new[]
            {
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_b", "kingdom_a", "propose_trade", "partially_executed")
            });
        Test.True(partiallyExecuted.Count == 2,
            "partially_executed is a successful bilateral result and must clear both directions");
        AssertOfferCooldownDecision(
            partiallyExecuted,
            "kingdom_a",
            "kingdom_b",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.ClearCooldown,
            "a reverse partially-executed result must clear the earlier forward cooldown");
        AssertOfferCooldownDecision(
            partiallyExecuted,
            "kingdom_b",
            "kingdom_a",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.ClearCooldown,
            "a partially-executed result must clear its own direction too");

        List<WorldDiplomacyOfferCooldownDecision> grouped =
            WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(new[]
            {
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_trade", "rejected"),
                new WorldDiplomacyOfferRoundObservation(
                    "KINGDOM_A", "KINGDOM_B", "PROPOSE_TRADE", "ACCEPTED"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_alliance", "rejected"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_b", "kingdom_a", "propose_trade", "expired"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_c", "propose_trade", "open"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_peace", "rejected"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "unknown", "rejected"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_a", "propose_trade", "rejected")
            });
        Test.True(grouped.Count == 4,
            "closed-round aggregation must isolate directed pair/domain keys and ignore peace, unknown, and self-pair offers");
        AssertOfferCooldownDecision(
            grouped,
            "kingdom_a",
            "kingdom_b",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.ClearCooldown,
            "any accepted result in one directed pair/domain group must win over failed statuses");
        AssertOfferCooldownDecision(
            grouped,
            "kingdom_a",
            "kingdom_b",
            WorldDiplomacyOfferDomain.Alliance,
            WorldDiplomacyOfferCooldownAction.StartCooldown,
            "trade success must not clear the same directed pair's failed alliance proposal");
        AssertOfferCooldownDecision(
            grouped,
            "kingdom_b",
            "kingdom_a",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.ClearCooldown,
            "success in either direction must suppress failure and clear both directions for that bilateral domain");
        AssertOfferCooldownDecision(
            grouped,
            "kingdom_a",
            "kingdom_c",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.StartCooldown,
            "one directed pair must not absorb another target's trade result");

        List<WorldDiplomacyOfferCooldownDecision> unrecognizedStatus =
            WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(new[]
            {
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_trade", "corrupt_status"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_alliance", "invalidated")
            });
        Test.True(unrecognizedStatus.Count == 0,
            "unknown and invalidated statuses must not silently create a cooldown");

        List<WorldDiplomacyOfferCooldownDecision> failedBothDirections =
            WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(new[]
            {
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_a", "kingdom_b", "propose_trade", "rejected"),
                new WorldDiplomacyOfferRoundObservation(
                    "kingdom_b", "kingdom_a", "propose_trade", "countered")
            });
        Test.True(failedBothDirections.Count == 2,
            "without bilateral success, only the two directions that actually proposed must start cooldown");
        AssertOfferCooldownDecision(
            failedBothDirections,
            "kingdom_a",
            "kingdom_b",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.StartCooldown,
            "the failed initial proposer direction must start cooldown");
        AssertOfferCooldownDecision(
            failedBothDirections,
            "kingdom_b",
            "kingdom_a",
            WorldDiplomacyOfferDomain.Trade,
            WorldDiplomacyOfferCooldownAction.StartCooldown,
            "the failed reverse proposer direction must independently start cooldown");

        const int FailedRoundDay = 100;
        const int DefaultCooldownDays = 168;
        Test.True(WorldDiplomacyOfferCooldownRules.IsCoolingDown(
                FailedRoundDay, 267, DefaultCooldownDays),
            "the 168-day cooldown must remain active one day before its boundary");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsCoolingDown(
                FailedRoundDay, 268, DefaultCooldownDays),
            "the 168-day cooldown must expire exactly at failedDay + 168");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsCoolingDown(
                FailedRoundDay, FailedRoundDay, 0),
            "MCM value zero must disable cooldown even on the failed round day");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsCoolingDown(
                -1, 0, DefaultCooldownDays),
            "a missing persisted failure day must not create a cooldown");
        Test.True(WorldDiplomacyOfferCooldownRules.IsCoolingDown(
                FailedRoundDay, 99, DefaultCooldownDays),
            "a save whose current day precedes the recorded failure must conservatively retain cooldown");

        WorldDiplomacyOfferCooldownKey directedTrade = new(
            "kingdom_a", "kingdom_b", WorldDiplomacyOfferDomain.Trade);
        HashSet<WorldDiplomacyOfferCooldownKey> directedLedger = new() { directedTrade };
        Test.True(directedLedger.Contains(new WorldDiplomacyOfferCooldownKey(
                "KINGDOM_A", "KINGDOM_B", WorldDiplomacyOfferDomain.Trade)),
            "loaded cooldown keys must match kingdom IDs case-insensitively");
        Test.True(!directedLedger.Contains(new WorldDiplomacyOfferCooldownKey(
                "kingdom_b", "kingdom_a", WorldDiplomacyOfferDomain.Trade)),
            "a loaded directed cooldown must not block its reverse direction");
        Test.True(!directedLedger.Contains(new WorldDiplomacyOfferCooldownKey(
                "kingdom_a", "kingdom_b", WorldDiplomacyOfferDomain.Alliance)),
            "a loaded trade cooldown must not block the alliance domain");

        CooldownLoadFixture persisted = new()
        {
            ProposerKingdomId = "kingdom_a",
            TargetKingdomId = "kingdom_b",
            Domain = "trade",
            FailedRoundDay = FailedRoundDay,
            SourceRoundId = "diplomacy_round:load-test"
        };
        string persistedJson = JsonSerializer.Serialize(persisted);
        CooldownLoadFixture? loaded = JsonSerializer.Deserialize<CooldownLoadFixture>(persistedJson);
        Test.True(loaded != null
                  && loaded.ProposerKingdomId == persisted.ProposerKingdomId
                  && loaded.TargetKingdomId == persisted.TargetKingdomId
                  && loaded.Domain == persisted.Domain
                  && loaded.FailedRoundDay == persisted.FailedRoundDay
                  && loaded.SourceRoundId == persisted.SourceRoundId,
            "a directed cooldown's persistence fields must survive a save/load round trip");
        Test.True(loaded != null && WorldDiplomacyOfferCooldownRules.IsCoolingDown(
                loaded.FailedRoundDay, 267, DefaultCooldownDays),
            "a reloaded failure day must retain the same cooldown boundary");
    }

    private static void RunOfferCooldownIntegrationContractTests(string source)
    {
        string closeActiveRound = ExtractSection(
            source,
            "private void CloseActiveRound(",
            "private void CommitLocalRoundSummary(");
        int expireOpenOffers = closeActiveRound.IndexOf(
            "offer.Status = \"expired\"",
            StringComparison.Ordinal);
        int settleCooldowns = closeActiveRound.IndexOf(
            "SettleTradeAllianceOfferCooldownsForClosedRound(round);",
            StringComparison.Ordinal);
        int persistClosedRound = closeActiveRound.IndexOf(
            "_storage.CompletedRounds.Add(round);",
            StringComparison.Ordinal);
        Test.True(expireOpenOffers >= 0
                  && settleCooldowns > expireOpenOffers
                  && persistClosedRound > settleCooldowns,
            "CloseActiveRound must expire open offers, settle their cooldown decisions, then persist the closed round");

        string potentialActions = ExtractSection(
            source,
            "private List<string> BuildPotentialDiplomaticActionIntents(",
            "private static void AppendOpenOfferResponseIntents(");
        Test.True(CountOccurrences(potentialActions, "IsTradeAllianceProposalCoolingDown(") == 2,
            "potential-action construction must apply cooldown exactly to the two proposal domains");
        Test.True(potentialActions.Contains(
                "else if (!IsTradeAllianceProposalCoolingDown(first, second, \"propose_alliance\")) actions.Add(\"propose_alliance\");",
                StringComparison.Ordinal),
            "propose_alliance must be omitted while its directed proposal key is cooling down");
        Test.True(potentialActions.Contains(
                "else if (!IsTradeAllianceProposalCoolingDown(first, second, \"propose_trade\")) actions.Add(\"propose_trade\");",
                StringComparison.Ordinal),
            "propose_trade must be omitted while its directed proposal key is cooling down");
        Test.True(potentialActions.Contains(
                "if (allied) actions.Add(\"break_alliance\");",
                StringComparison.Ordinal)
                  && potentialActions.Contains(
                      "if (trading) actions.Add(\"cancel_trade\");",
                      StringComparison.Ordinal),
            "break_alliance and cancel_trade must remain available from current state without a cooldown gate");
        string alliancePotential = ExtractSection(
            potentialActions,
            "if (alliance != null)",
            "if (trade != null)");
        string tradePotential = ExtractSection(
            potentialActions,
            "if (trade != null)",
            "return actions.Distinct(");
        Test.True(alliancePotential.Contains(
                "if (allied) actions.Add(\"break_alliance\");",
                StringComparison.Ordinal)
                  && alliancePotential.Contains(
                      "else if (!IsTradeAllianceProposalCoolingDown(first, second, \"propose_alliance\")) actions.Add(\"propose_alliance\");",
                      StringComparison.Ordinal),
            "an existing alliance must expose break_alliance instead of propose_alliance");
        Test.True(tradePotential.Contains(
                "if (trading) actions.Add(\"cancel_trade\");",
                StringComparison.Ordinal)
                  && tradePotential.Contains(
                      "else if (!IsTradeAllianceProposalCoolingDown(first, second, \"propose_trade\")) actions.Add(\"propose_trade\");",
                      StringComparison.Ordinal),
            "an existing trade agreement must expose cancel_trade instead of propose_trade");

        string responseActions = ExtractSection(
            source,
            "private static void AppendOpenOfferResponseIntents(",
            "private List<string> BuildLegalDiplomaticActionIntents(");
        Test.True(responseActions.Contains(
                "ProposalIntentToResponseIntent(proposalIntent, accepted: true)",
                StringComparison.Ordinal)
                  && responseActions.Contains(
                      "ProposalIntentToResponseIntent(proposalIntent, accepted: false)",
                      StringComparison.Ordinal),
            "open offers must continue to expose both accept and reject response actions");
        Test.True(!responseActions.Contains("IsTradeAllianceProposalCoolingDown(", StringComparison.Ordinal),
            "accept and reject response actions must not be hidden by proposal cooldown");

        string stateValidation = ExtractSection(
            source,
            "private bool TryGetDiplomaticStateViolation(",
            "private bool IsEnforcingRejectedUltimatum(");
        string proposeAllianceState = ExtractSection(
            stateValidation,
            "case \"propose_alliance\":",
            "case \"accept_alliance\":");
        string proposeTradeState = ExtractSection(
            stateValidation,
            "case \"propose_trade\":",
            "case \"accept_trade\":");
        Test.True(CountOccurrences(stateValidation, "IsTradeAllianceProposalCoolingDown(") == 2
                  && proposeAllianceState.Contains(
                      "IsTradeAllianceProposalCoolingDown(author, target, normalized)",
                      StringComparison.Ordinal)
                  && proposeTradeState.Contains(
                      "IsTradeAllianceProposalCoolingDown(author, target, normalized)",
                      StringComparison.Ordinal),
            "state validation must enforce cooldown only in propose_alliance and propose_trade branches");
        Test.True(proposeAllianceState.Contains(
                "if (atWar || allied) { reason = \"alliance_intent_conflicts_with_current_state\"; return true; }",
                StringComparison.Ordinal),
            "propose_alliance must be rejected when the kingdoms are already allied");
        Test.True(proposeTradeState.Contains(
                "if (atWar || trading) { reason = \"trade_intent_conflicts_with_current_state\"; return true; }",
                StringComparison.Ordinal),
            "propose_trade must be rejected when a trade agreement already exists");
        foreach (string unaffectedIntent in new[]
        {
            "break_alliance", "cancel_trade", "accept_alliance", "accept_trade"
        })
        {
            Test.True(stateValidation.Contains("case \"" + unaffectedIntent + "\":", StringComparison.Ordinal),
                "state validation must retain the non-proposal branch: " + unaffectedIntent);
        }

        string generatedCommit = ExtractSection(
            source,
            "private void CommitGeneratedDocument(",
            "private bool TryGetGeneratedIntentLegalityViolation(");
        int generatedLegalityCheck = generatedCommit.IndexOf(
            "TryGetGeneratedIntentLegalityViolation(job, json, author, fallbackTarget",
            StringComparison.Ordinal);
        int createDocument = generatedCommit.IndexOf("CreateDocument(", StringComparison.Ordinal);
        int publishDocument = generatedCommit.IndexOf("AddDocument(document);", StringComparison.Ordinal);
        Test.True(generatedLegalityCheck >= 0
                  && createDocument > generatedLegalityCheck
                  && publishDocument > generatedLegalityCheck,
            "every generated result must pass live diplomatic-state validation before a document can be created or published");
        string legalityRejection = ExtractSection(
            generatedCommit,
            "if (TryGetGeneratedIntentLegalityViolation(",
            "Kingdom target = generatedTarget;");
        Test.True(legalityRejection.Contains("RejectGeneratedDraftBeforePublication(", StringComparison.Ordinal)
                  && legalityRejection.Contains("generatedTarget ?? fallbackTarget", StringComparison.Ordinal)
                  && legalityRejection.Contains("legalityReason", StringComparison.Ordinal),
            "an illegal generated draft must enter the unified rejection gate rather than publication");

        string generationRepair = ExtractSection(
            source,
            "private bool EnqueueGeneratedDeclarationRepair(",
            "private void AbandonRejectedGeneration(");
        Test.True(generationRepair.Contains("Kind = \"generate\"", StringComparison.Ordinal)
                  && generationRepair.Contains(
                      "SemanticRepairAttempts = source.SemanticRepairAttempts + 1",
                      StringComparison.Ordinal),
            "a semantic repair must remain a generated job and return through the same legality gate");
        Test.True(generationRepair.Contains(
                "alliance_intent_conflicts_with_current_state",
                StringComparison.Ordinal)
                  && generationRepair.Contains(
                      "trade_intent_conflicts_with_current_state",
                      StringComparison.Ordinal),
            "repair guidance must recognize duplicate alliance and trade proposals as live-state conflicts");

        Test.True(source.Contains(
                "private const int OfferCooldownStateSchemaVersion = 1;",
                StringComparison.Ordinal),
            "offer cooldown storage must have an independent schema version");
        Test.True(source.Contains(
                "private readonly Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> _offerCooldownByKey",
                StringComparison.Ordinal),
            "offer cooldown checks must use the directed-key runtime index");

        string cooldownDto = ExtractSection(
            source,
            "public sealed class WorldDiplomacyOfferCooldown",
            "public sealed class WorldDiplomacyThreatNonComplianceEvent");
        foreach (string jsonField in new[]
        {
            "proposerKingdomId", "targetKingdomId", "domain", "lastFailedRoundDay", "sourceRoundId"
        })
        {
            Test.True(cooldownDto.Contains("[JsonProperty(\"" + jsonField + "\")]", StringComparison.Ordinal),
                "offer cooldown DTO is missing persisted field: " + jsonField);
        }

        string storageRoot = ExtractSection(
            source,
            "public sealed class WorldDiplomacyStorage",
            "public sealed class WorldDiplomacyCanonicalHistoryState");
        Test.True(storageRoot.Contains("[JsonProperty(\"offerCooldownStateSchemaVersion\")]", StringComparison.Ordinal)
                  && storageRoot.Contains("public int OfferCooldownStateSchemaVersion", StringComparison.Ordinal),
            "root storage must persist the offer cooldown schema version");
        Test.True(storageRoot.Contains("[JsonProperty(\"offerCooldowns\")]", StringComparison.Ordinal)
                  && storageRoot.Contains("public List<WorldDiplomacyOfferCooldown> OfferCooldowns", StringComparison.Ordinal),
            "root storage must persist the cooldown records");

        string rebuildIndex = ExtractSection(
            source,
            "private void RebuildOfferCooldownIndex()",
            "private void NormalizeOfferCooldownStorage()");
        Test.True(rebuildIndex.Contains("_offerCooldownByKey.Clear();", StringComparison.Ordinal)
                  && rebuildIndex.Contains("_storage?.OfferCooldowns", StringComparison.Ordinal)
                  && rebuildIndex.Contains("_offerCooldownByKey.TryGetValue(key", StringComparison.Ordinal)
                  && rebuildIndex.Contains("_offerCooldownByKey[key] = cooldown;", StringComparison.Ordinal),
            "load-time index rebuild must deduplicate persisted directed keys by their latest failure day");

        string normalizeCooldowns = ExtractSection(
            source,
            "private void NormalizeOfferCooldownStorage()",
            "private bool IsTradeAllianceProposalCoolingDown(");
        Test.True(normalizeCooldowns.Contains(
                "_storage.OfferCooldowns ??= new List<WorldDiplomacyOfferCooldown>();",
                StringComparison.Ordinal)
                  && normalizeCooldowns.Contains(".Take(MaxStoredOfferCooldowns)", StringComparison.Ordinal)
                  && normalizeCooldowns.Contains(
                      "_storage.OfferCooldownStateSchemaVersion = OfferCooldownStateSchemaVersion;",
                      StringComparison.Ordinal)
                  && normalizeCooldowns.Contains("RebuildOfferCooldownIndex();", StringComparison.Ordinal),
            "cooldown normalization must initialize, bound, version, and rebuild the persisted index");
        string normalizeStorage = ExtractSection(
            source,
            "private void NormalizeStorage(",
            "private void TrimRecentBattleFacts(");
        Test.True(normalizeStorage.Contains(
                "_storage.OfferCooldowns ??= new List<WorldDiplomacyOfferCooldown>();",
                StringComparison.Ordinal)
                  && normalizeStorage.Contains("NormalizeOfferCooldownStorage();", StringComparison.Ordinal),
            "ordinary save normalization must include the cooldown DTO list and runtime index rebuild");

        string promptProducingSource = string.Join("\n", new[]
        {
            ExtractSection(
                source,
                "private string BuildRelayConversationTurnPrompt(",
                "private static void AppendRoundSubstantiveProgressRequirement("),
            ExtractSection(
                source,
                "private static string BuildDiplomaticDeclarationModeContract()",
                "private static string BuildCanonicalHistoryCompressionModeContract()"),
            ExtractSection(
                source,
                "private void AppendDiplomaticThreatDynamicContext(",
                "private void AppendDiplomaticThreatAnalysisContext("),
            ExtractSection(
                source,
                "private void AppendDiplomaticThreatAnalysisContext(",
                "private string BuildAutonomousOpeningPrompt("),
            ExtractSection(
                source,
                "private string BuildAutonomousOpeningPrompt(",
                "private string BuildCompactRoundPlanCandidateLine("),
            ExtractSection(
                source,
                "private static string BuildAnalysisModeContract()",
                "private static string BuildTokenCompressionPrompt(")
        });
        foreach (string forbiddenPromptDisclosure in new[]
        {
            "WorldDiplomacyTradeAllianceFailedProposalCooldownDays",
            "OfferCooldown",
            "失败冷却",
            "冷却剩余",
            "剩余冷却",
            "贸易/结盟失败冷却"
        })
        {
            Test.True(!promptProducingSource.Contains(forbiddenPromptDisclosure, StringComparison.OrdinalIgnoreCase),
                "LLM-facing contracts and dynamic prompts must not disclose cooldown internals: "
                + forbiddenPromptDisclosure);
        }

        string settings = File.ReadAllText(FindRepositoryFile("DuelSettings.cs"), Encoding.UTF8);
        string mcmCooldownSetting = ExtractSection(
            settings,
            "[SettingPropertyInteger(\"贸易/结盟失败冷却（天）\"",
            "[SettingPropertyInteger(\"外交长期记忆压缩触发值");
        Test.True(mcmCooldownSetting.Contains(", 0, 672, \"0\"", StringComparison.Ordinal),
            "the MCM cooldown range must remain 0..672 days");
        Test.True(mcmCooldownSetting.Contains(
                "WorldDiplomacyTradeAllianceFailedProposalCooldownDays { get; set; } = 168;",
                StringComparison.Ordinal),
            "the MCM cooldown default must remain 168 days");
    }

    private static void RunLiveLegalActionPublicationContractTests(string source)
    {
		Test.True(source.Contains(
				"private const int DiplomacyPromptContractVersion = 28;",
				StringComparison.Ordinal),
			"the all-kingdom response context must use prompt contract version 28");
		Test.True(source.Contains(
				"private const string CanonicalHistoryCacheAffinityKey = \"diplomacy-history:v28\";",
				StringComparison.Ordinal),
			"the exact own-reputation prompt must advance canonical-history cache affinity to v28");
		string settings = File.ReadAllText(FindRepositoryFile("DuelSettings.cs"), Encoding.UTF8);
		Test.True(settings.Contains("【AnimusForge 王国外交共同契约 v25】", StringComparison.Ordinal),
			"the negotiated-round contract must use common diplomacy contract v25");

        string compactCurrentOptions = ExtractSection(
            source,
            "private string BuildCurrentLegalDiplomaticOptions(",
            "private List<Kingdom> GetEligibleAiKingdoms(");
        Test.True(compactCurrentOptions.Contains(
				"BuildLegalDiplomaticDeclarationIntents(",
                StringComparison.Ordinal)
				  && compactCurrentOptions.Contains("isRelayTurn", StringComparison.Ordinal)
				  && compactCurrentOptions.Contains("resultSettlementSlotId", StringComparison.Ordinal)
                  && compactCurrentOptions.Contains("List<string> normalizedActions = actions", StringComparison.Ordinal)
                  && compactCurrentOptions.Contains("if (normalizedActions.Count == 0) continue;", StringComparison.Ordinal)
                  && compactCurrentOptions.Contains(
                      "lines.Add(id + \"=\" + string.Join(\"/\"",
                      StringComparison.Ordinal)
                  && compactCurrentOptions.Contains(
                      ".Distinct(StringComparer.OrdinalIgnoreCase)",
                      StringComparison.Ordinal),
            "relay and repair prompts must compact each target's live actions into one deterministic entry");
        Test.True(CountOccurrences(
                      compactCurrentOptions,
                      ".OrderBy(x => x, StringComparer.OrdinalIgnoreCase)") >= 2,
            "compact legal-action targets and intents must both render deterministically");
        Test.True(!compactCurrentOptions.Contains("【本篇唯一合法intent清单】", StringComparison.Ordinal)
                  && !compactCurrentOptions.Contains("\"target=\" + id + \";intent=\"", StringComparison.Ordinal)
                  && !compactCurrentOptions.Contains("lines.AddRange(options)", StringComparison.Ordinal),
            "compact legal actions must not restore the retired marker or one-line-per-target-intent expansion");
        foreach (string forbiddenDynamicLegalDetail in new[]
        {
            ";commitment=",
            "DefaultCommitmentForIntent",
            ";responding_to_offer_document_id=",
            ";responding_to_threat_document_id=",
            "hasWarning",
            "hasUltimatum",
            "hasAcceptance",
            "选择warning",
            "选择ultimatum",
            "选择accept_*",
            "先选择",
            "正文",
            "逐字复制target",
            "本国正式警告贵国",
            "若贵国拒绝或继续该军事行为，本国将向贵国发出最后通牒或正式宣战",
            "这是本国最后通牒",
            "若贵国逾期不履行，本国将正式向贵国宣战",
            "本国无条件接受贵国提议的全部原条件，相关协定立即生效",
            "所选行若含responding_to_*字段"
        })
        {
            Test.True(!compactCurrentOptions.Contains(forbiddenDynamicLegalDetail, StringComparison.Ordinal),
                "compact legal actions must not repeat commitment/source/body-template detail: " + forbiddenDynamicLegalDetail);
        }

        string relayPrompt = ExtractSection(
            source,
            "private string BuildRelayConversationTurnPrompt(",
            "private static void AppendRoundSubstantiveProgressRequirement(");
        Test.True(relayPrompt.Contains(
                      "List<string> legalTargetIds = round?.ResultSettlementPending == true",
                      StringComparison.Ordinal)
                  && relayPrompt.Contains(
                      "GetResultSettlementActionableTargets(round, author)",
                      StringComparison.Ordinal)
                  && relayPrompt.Contains(
                      ": (round?.RelayRouteKingdomIds ?? new List<string>())",
                      StringComparison.Ordinal)
                  && relayPrompt.Contains(
					  "BuildCurrentLegalDiplomaticOptions(",
					  StringComparison.Ordinal)
				  && relayPrompt.Contains("isRelayTurn: true", StringComparison.Ordinal)
				  && relayPrompt.Contains("resultSettlementSlotId: round?.ResultSettlementCurrentSlotId", StringComparison.Ordinal),
            "a settlement relay may render current actionable independent kingdoms, while an ordinary relay remains route-only");
        string autonomousPrompt = ExtractSection(
            source,
            "private string BuildAutonomousOpeningPrompt(",
            "private string BuildGenerationPrompt(");
        string compactCandidateRenderer = ExtractSection(
            source,
            "private string BuildCompactRoundPlanCandidateLine(",
            "private string BuildWarNegotiationContext(");
        Test.True(autonomousPrompt.Contains(
					  "BuildCompactRoundPlanCandidateLine(author, candidate, round)",
                      StringComparison.Ordinal)
                  && compactCandidateRenderer.Contains(
                      "BuildLegalDiplomaticActionIntents(round, initiator, candidate)",
                      StringComparison.Ordinal),
            "autonomous candidate rows must carry their own live legal actions instead of a duplicated final block");
        string targetedPrompt = ExtractSection(
            source,
            "private string BuildGenerationPrompt(",
            "private string BuildCompactRoundPlanCandidateLine(");
        Test.True(targetedPrompt.Contains(
                      "List<string> legalActions = BuildLegalDiplomaticDeclarationIntents(",
                      StringComparison.Ordinal)
                  && targetedPrompt.Contains(
                      "isExternalResponseOnly: isExternalResponseOnly",
                      StringComparison.Ordinal)
                  && targetedPrompt.Contains(
                      "responseSource: sourceDocument",
                      StringComparison.Ordinal)
                  && targetedPrompt.Contains(
                      "sb.AppendLine(\"本篇合法动作=\" + string.Join(\"、\", legalActions)",
                      StringComparison.Ordinal),
            "targeted generation must expose one compact 本篇合法动作 field computed from the shared live declaration gate");
        foreach (string promptRenderer in new[] { relayPrompt, autonomousPrompt, targetedPrompt, compactCandidateRenderer })
        {
            Test.True(!promptRenderer.Contains("【本篇唯一合法intent清单】", StringComparison.Ordinal)
                      && !promptRenderer.Contains("\"target=\" + id + \";intent=\"", StringComparison.Ordinal),
                "declaration prompt builders must not restore the retired legal-list marker or per-combination rows");
        }

        string declareModeTail = ExtractSection(
            source,
            "private static string BuildDeclareModePrompt(string dynamicPrompt)",
            "private List<string> GetPresentedThreatDocumentIds(");
        int dynamicPromptIndex = declareModeTail.IndexOf("sb.AppendLine(dynamicPrompt.Trim())", StringComparison.Ordinal);
        int declareModeIndex = declareModeTail.IndexOf("sb.AppendLine(\"【MODE=DECLARE】\")", StringComparison.Ordinal);
        Test.True(dynamicPromptIndex >= 0 && declareModeIndex > dynamicPromptIndex,
            "MODE=DECLARE must follow the dynamic prompt that already contains compact live actions");

        string generationRepair = ExtractSection(
            source,
            "private bool EnqueueGeneratedDeclarationRepair(",
            "private void AbandonRejectedGeneration(");
        Test.True(generationRepair.Contains("【未发布草稿的硬事实纠正】", StringComparison.Ordinal),
            "semantic repair must retain its unpublished-draft hard-fact correction boundary");
		Test.True(generationRepair.Contains("JObject rejectedJson", StringComparison.Ordinal)
				  && generationRepair.Contains("[\"actions\"] is JArray rejectedActions", StringComparison.Ordinal),
            "semantic repair must receive and inspect the rejected action array instead of guessing one singular intent");
        Test.True(generationRepair.Contains("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal),
            "semantic repair must receive freshly rendered compact legal actions");
        foreach (string forbiddenGenericRepairAdvice in new[]
        {
            "若本意是贸易或结盟",
            "改用对应的实际动作",
            "改用贸易",
            "改用结盟"
        })
        {
            Test.True(!generationRepair.Contains(forbiddenGenericRepairAdvice, StringComparison.Ordinal),
                "semantic repair must not offer generic trade/alliance advice outside the final live legal-intent list: "
                + forbiddenGenericRepairAdvice);
        }
		int repairLegalOptions = generationRepair.IndexOf("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal);
		int repairRewriteInstruction = generationRepair.IndexOf(
			"重新输出完整JSON并重写title和body",
			StringComparison.Ordinal);
		int repairModeWrapper = generationRepair.IndexOf("BuildDeclareModePrompt(correctionBuilder.ToString())", StringComparison.Ordinal);
		Test.True(repairRewriteInstruction >= 0 && repairLegalOptions > repairRewriteInstruction
			&& repairModeWrapper > repairLegalOptions,
			"semantic repair must append compact live actions before the MODE selector");
        Test.True(!generationRepair.Contains("threat_semantics_", StringComparison.Ordinal)
                  && !generationRepair.Contains("isThreatSemanticFailure", StringComparison.Ordinal)
                  && !generationRepair.Contains("lockThreatIntent", StringComparison.Ordinal),
            "generated repair must not recreate a prose-based warning/ultimatum classifier or retry branch");
        foreach (string forbiddenRepairBodyTemplate in new[]
        {
            "本国正式警告贵国",
            "若贵国拒绝或继续该军事行为，本国将向贵国发出最后通牒或正式宣战",
            "这是本国最后通牒",
            "若贵国逾期不履行，本国将正式向贵国宣战",
            "本国无条件接受贵国提议的全部原条件，相关协定立即生效"
        })
        {
            Test.True(!generationRepair.Contains(forbiddenRepairBodyTemplate, StringComparison.Ordinal),
                "repair guidance must state only the semantic boundary, not a literal body template: " + forbiddenRepairBodyTemplate);
        }
		int repairLiveActions = repairLegalOptions;
		int invalidCombinationGuard = generationRepair.IndexOf(
			"string.Equals(reason, \"intent_not_in_current_legal_action_list\"",
			StringComparison.Ordinal);
		Test.True(invalidCombinationGuard >= 0 && repairLiveActions > invalidCombinationGuard,
			"a live-state-invalid action batch must be repaired against the freshly rendered target/action map");

        VerifyPresentedLegalActionSignatureContracts(source, generationRepair);
        VerifyFinalLiveStateAndExternalAcceptContracts(source);
    }

    private static void VerifyPresentedLegalActionSignatureContracts(
        string source,
        string generationRepair)
    {
        string enqueueGeneration = ExtractSection(
            source,
            "private void EnqueueGenerationJob(",
            "private bool EnsureGenerationJobHasKingdomStrategicProfile(");
        int initialSignature = enqueueGeneration.IndexOf(
            "job.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(job);",
            StringComparison.Ordinal);
        int initialCapture = enqueueGeneration.IndexOf("CaptureCanonicalHistoryForJob(job", StringComparison.Ordinal);
        int initialEnqueue = enqueueGeneration.IndexOf("EnqueueJob(job);", StringComparison.Ordinal);
        Test.True(initialSignature >= 0
                  && initialCapture > initialSignature
                  && initialEnqueue > initialSignature,
            "new generation jobs must freeze their live legal-action signature before enqueueing");

        string signatureBuilder = ExtractSection(
            source,
            "private string BuildGenerationLegalActionSignature(",
            "private bool HasStaleDiplomaticActionPresentation(");
        int settlementSignatureTargets = signatureBuilder.IndexOf(
            "GetResultSettlementActionableTargets(round, author)",
            StringComparison.Ordinal);
        int ordinaryRelaySignatureTargets = signatureBuilder.IndexOf(
            "else if (job.IsRelayTurn && round?.RelayRouteKingdomIds != null)",
            StringComparison.Ordinal);
        int targetedSignatureTarget = signatureBuilder.IndexOf(
            "else if (!string.IsNullOrWhiteSpace(job.TargetKingdomId))",
            StringComparison.Ordinal);
        Test.True(signatureBuilder.Contains("PruneInvalidOffers(round);", StringComparison.Ordinal)
                  && signatureBuilder.Contains("round?.ResultSettlementPending == true", StringComparison.Ordinal)
                  && signatureBuilder.Contains("job.ResultSettlementSlotId", StringComparison.Ordinal)
                  && settlementSignatureTargets >= 0
                  && ordinaryRelaySignatureTargets > settlementSignatureTargets
                  && targetedSignatureTarget > ordinaryRelaySignatureTargets
                  && signatureBuilder.Contains(
					  "BuildLegalDiplomaticDeclarationIntents(",
                      StringComparison.Ordinal)
				  && signatureBuilder.Contains("job.IsRelayTurn", StringComparison.Ordinal)
				  && signatureBuilder.Contains("job.ResultSettlementSlotId", StringComparison.Ordinal)
                  && signatureBuilder.Contains(
                      "actions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
                      StringComparison.Ordinal)
                  && signatureBuilder.Contains("return StablePromptHash(state.ToString());", StringComparison.Ordinal),
            "the presented-action signature must hash settlement-wide actionable targets, but keep ordinary relays route-only, with deterministic current actions");

        string staleActionCheck = ExtractSection(
            source,
            "private bool HasStaleDiplomaticActionPresentation(",
            "private bool RefreshDiplomaticActionPresentationAndPrompt(");
        Test.True(staleActionCheck.Contains("job.PresentedLegalActionSignature ?? \"\"", StringComparison.Ordinal)
                  && staleActionCheck.Contains("BuildGenerationLegalActionSignature(job)", StringComparison.Ordinal)
                  && staleActionCheck.Contains("StringComparison.Ordinal", StringComparison.Ordinal),
            "a generation must be stale whenever its live legal-action signature changes");

        string refreshActionPrompt = ExtractSection(
            source,
            "private bool RefreshDiplomaticActionPresentationAndPrompt(",
            "private bool RefreshDiplomaticThreatPresentationAndPrompt(");
        Test.True(refreshActionPrompt.Contains("job.LlmMessages?.Clear();", StringComparison.Ordinal)
                  && refreshActionPrompt.Contains("job.SemanticRepairAttempts = 0;", StringComparison.Ordinal)
                  && refreshActionPrompt.Contains("job.HistoryPrefixHash = \"\";", StringComparison.Ordinal)
                  && refreshActionPrompt.Contains("job.IsRunning = false;", StringComparison.Ordinal)
                  && refreshActionPrompt.Contains("return TryRebuildPendingWorldDiplomacyJob(job);", StringComparison.Ordinal),
            "a stale action presentation must discard its request/repair chain and rebuild the prompt");

        string beforeSend = ExtractSection(
            source,
            "private void TryStartNextLlmJob()",
            "private static bool HasCurrentCanonicalPromptContract(");
        int beforeSendStaleCheck = beforeSend.IndexOf("HasStaleDiplomaticActionPresentation(job)", StringComparison.Ordinal);
        int beforeSendRefresh = beforeSend.IndexOf("RefreshDiplomaticActionPresentationAndPrompt(job)", StringComparison.Ordinal);
        int requestMaterialization = beforeSend.IndexOf("BuildLlmMessageArray(job)", StringComparison.Ordinal);
        Test.True(beforeSendStaleCheck >= 0
                  && beforeSendRefresh > beforeSendStaleCheck
                  && requestMaterialization > beforeSendRefresh,
            "queued generation must rebuild a stale legal-action presentation before request messages are materialized");

        string completedJobs = ExtractSection(
            source,
            "private void ProcessCompletedJobs()",
            "private void CommitFailedJob(");
        int completedStaleCheck = completedJobs.IndexOf("HasStaleDiplomaticActionPresentation(job)", StringComparison.Ordinal);
        int completedRefresh = completedJobs.IndexOf(
            "RefreshDiplomaticActionPresentationAndPrompt(job)",
            completedStaleCheck,
            StringComparison.Ordinal);
        int commitGenerated = completedJobs.IndexOf("CommitGeneratedDocument(job, result.Content);", StringComparison.Ordinal);
        Test.True(completedStaleCheck >= 0
                  && completedRefresh > completedStaleCheck
                  && commitGenerated > completedRefresh,
            "a successful completion must be discarded and rebuilt if legal actions changed while the request was running");
        string completedActionRebuildBlock = completedJobs.Substring(
            completedStaleCheck,
            commitGenerated - completedStaleCheck);
        Test.True(completedActionRebuildBlock.Contains("continue;", StringComparison.Ordinal)
                  && completedActionRebuildBlock.Contains(
                      "discarded completed generation from stale diplomatic action list",
                      StringComparison.Ordinal),
            "a stale completion must never fall through into generated-document commit");

        string rebuildPending = ExtractSection(
            source,
            "private bool TryRebuildPendingWorldDiplomacyJob(",
            "private List<WorldDiplomacyDocument> GetRecentDocuments(");
        int rebuiltPrompt = rebuildPending.IndexOf("job.UserPrompt = BuildDeclareModePrompt(dynamicPrompt);", StringComparison.Ordinal);
        int rebuiltSignature = rebuildPending.IndexOf(
            "job.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(job);",
            StringComparison.Ordinal);
        int rebuiltCapture = rebuildPending.IndexOf("CaptureCanonicalHistoryForJob(job", StringComparison.Ordinal);
        Test.True(rebuiltPrompt >= 0
                  && rebuiltSignature > rebuiltPrompt
                  && rebuiltCapture > rebuiltSignature,
            "every rebuilt generation prompt must store the signature of the legal list it presents");

        int repairLegalActions = generationRepair.IndexOf("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal);
        int repairSignature = generationRepair.IndexOf(
            "repair.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(repair);",
            StringComparison.Ordinal);
        int repairEnqueue = generationRepair.IndexOf("EnqueueJob(repair);", StringComparison.Ordinal);
        Test.True(repairLegalActions >= 0
                  && repairSignature > repairLegalActions
                  && repairEnqueue > repairSignature,
            "semantic repair must freeze its current legal-action signature before enqueueing");

        string normalizeStorage = ExtractSection(
            source,
            "private void NormalizeStorage(",
            "private void TrimRecentBattleFacts(");
        Test.True(normalizeStorage.Contains("job.PresentedLegalActionSignature ??= \"\";", StringComparison.Ordinal),
            "old saves must normalize a missing presented legal-action signature");
        string jobDto = ExtractSection(
            source,
            "public sealed class WorldDiplomacyJob",
            "public sealed class WorldDiplomacyCanonicalHistoryState");
        Test.True(jobDto.Contains("[JsonProperty(\"presentedLegalActionSignature\")]", StringComparison.Ordinal)
                  && jobDto.Contains("public string PresentedLegalActionSignature", StringComparison.Ordinal),
            "the legal-action presentation signature must survive save/load with its job");
    }

    private static void VerifyFinalLiveStateAndExternalAcceptContracts(string source)
    {
        string publication = ExtractSection(
            source,
            "private void ProcessAnalyzedDocument(",
            "private bool TryGetPlayerWorldStateIntentViolation(");
        int liveGuardStart = publication.IndexOf("string liveStateBlockReason = \"\";", StringComparison.Ordinal);
        int liveStateValidation = publication.IndexOf(
            "TryGetDiplomaticStateViolation(normalizedIntent, author, target, out liveStateBlockReason)",
            StringComparison.Ordinal);
        int liveGuardSuppression = publication.IndexOf(
            "SuppressInvalidDocumentBeforePropagation(document, \"final_live_state_guard:\" + liveStateBlockReason);",
            StringComparison.Ordinal);
        int playerSpecificGuard = publication.IndexOf(
            "if (document.IsPlayerAuthored",
            liveGuardStart,
            StringComparison.Ordinal);
        int publishReady = publication.IndexOf(
            "document.IsReadyForPublication = true;",
            liveGuardSuppression,
            StringComparison.Ordinal);
        Test.True(liveGuardStart >= 0
                  && liveStateValidation > liveGuardStart
                  && liveGuardSuppression > liveStateValidation
                  && playerSpecificGuard > liveGuardSuppression
                  && publishReady > liveGuardSuppression,
            "the executable-action path must apply the live-state guard before player-only mechanic checks; the player document itself may already be public");
        Test.True(publication.Contains(
                "bool invalidLiveTarget = target == null || target == author || target.IsEliminated",
                StringComparison.Ordinal)
                  && publication.Contains("diplomatic_action_has_no_live_target", StringComparison.Ordinal),
            "the final live-state guard must also reject a missing, self, eliminated, or controlled target");

        string generatedCommitPath = ExtractSection(
            source,
            "private void CommitGeneratedDocument(",
            "private bool TryGetGeneratedIntentLegalityViolation(");
        string analyzedCommitPath = ExtractSection(
            source,
            "private void CommitAnalysis(",
            "private void ReconcilePlayerDeclarationWithOpenOffer(");
        Test.True(generatedCommitPath.Contains("ProcessAnalyzedDocument(document", StringComparison.Ordinal)
                  && analyzedCommitPath.Contains("ProcessAnalyzedDocument(document", StringComparison.Ordinal),
            "both generated and analyzed declarations must converge on the final live-state publication guard");

        string liveResultReader = ExtractSection(
            source,
            "private static bool HasProposalTakenEffect(",
            "private static string ProposalSuccessResult(");
        Test.True(liveResultReader.Contains(
                "\"propose_alliance\" => Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>()?.IsAllyWithKingdom(proposer, target) == true",
                StringComparison.Ordinal)
                  && liveResultReader.Contains(
                      "BannerlordApiCompat.HasTradeAgreement(trade, proposer, target)",
                      StringComparison.Ordinal),
            "external accept readback must use the live alliance and trade campaign systems");

        string externalResolution = ExtractSection(
            source,
            "private void NotifyExternalDiplomacyResolvedInternal(",
            "private static bool Patch_Kingdom_AddDecision_Prefix(");
        string externalTradeAccept = ExtractSection(
            externalResolution,
            "if (string.Equals(normalizedAction, \"accept_trade\"",
            "else if (string.Equals(normalizedAction, \"accept_alliance\"");
        string externalAllianceAccept = ExtractSection(
            externalResolution,
            "else if (string.Equals(normalizedAction, \"accept_alliance\"",
            "WorldDiplomacyDocument fact = CreateDocument(");
        Test.True(externalTradeAccept.Contains(
                "if (!HasProposalTakenEffect(\"propose_trade\", initiator, target))",
                StringComparison.Ordinal)
                  && externalTradeAccept.Contains("return;", StringComparison.Ordinal)
                  && externalTradeAccept.IndexOf("MarkOpenBilateralOffersAccepted(", StringComparison.Ordinal)
                      > externalTradeAccept.IndexOf("HasProposalTakenEffect(", StringComparison.Ordinal)
                  && externalTradeAccept.IndexOf("ClearBilateralOfferCooldowns(", StringComparison.Ordinal)
                      > externalTradeAccept.IndexOf("HasProposalTakenEffect(", StringComparison.Ordinal),
            "external accept_trade must confirm the live agreement before accepting an offer or clearing cooldown");
        Test.True(externalAllianceAccept.Contains(
                "if (!HasProposalTakenEffect(\"propose_alliance\", initiator, target))",
                StringComparison.Ordinal)
                  && externalAllianceAccept.Contains("return;", StringComparison.Ordinal)
                  && externalAllianceAccept.IndexOf("MarkOpenBilateralOffersAccepted(", StringComparison.Ordinal)
                      > externalAllianceAccept.IndexOf("HasProposalTakenEffect(", StringComparison.Ordinal)
                  && externalAllianceAccept.IndexOf("ClearBilateralOfferCooldowns(", StringComparison.Ordinal)
                      > externalAllianceAccept.IndexOf("HasProposalTakenEffect(", StringComparison.Ordinal),
            "external accept_alliance must confirm the live alliance before accepting an offer or clearing cooldown");
    }

    private static void RunGeneratedDraftRepairContractTests(string source)
    {
        Test.True(source.Contains(
                "private const int MaxGeneratedDraftRepairAttempts = 1;",
                StringComparison.Ordinal),
            "generated drafts must receive exactly one bounded semantic repair attempt");
        string generatedCommit = ExtractSection(
            source,
            "private void CommitGeneratedDocument(",
            "private bool TryGetGeneratedIntentLegalityViolation(");
        Test.True(CountOccurrences(generatedCommit, "RejectGeneratedDraftBeforePublication(") == 4,
            "parse, legality, sanitized-body, and second-stage envelope failures must share one draft-rejection exit");
        Test.True(!generatedCommit.Contains("EnqueueGeneratedDeclarationRepair(", StringComparison.Ordinal)
                  && CountOccurrences(generatedCommit, "AbandonRejectedGeneration(") == 2
                  && generatedCommit.Contains(
                      "AbandonRejectedGeneration(job, null, fallbackTarget, \"generated_party_missing\");",
                      StringComparison.Ordinal)
				  && generatedCommit.Contains("CanAiAuthorDiplomaticDocument(author", StringComparison.Ordinal),
            "missing or newly unauthorized authors may bypass draft repair; model-output failures must use the shared retry-budget decision");
		int statementNormalization = generatedCommit.IndexOf("RemoveRedundantStatementActions(json)", StringComparison.Ordinal);
		int legalityValidation = generatedCommit.IndexOf("TryGetGeneratedIntentLegalityViolation(", StringComparison.Ordinal);
		Test.True(statementNormalization >= 0 && legalityValidation > statementNormalization,
			"a redundant statement action must be normalized before generated-action legality validation");
		Test.True(generatedCommit.Contains("jobRound.ConsecutiveTechnicalGenerationFailures = 0;", StringComparison.Ordinal),
			"a structurally valid generated declaration must reset the round's consecutive technical failure count");
		string statementNormalizer = ExtractMethod(
			source,
			"private static int RemoveRedundantStatementActions(");
		Test.True(statementNormalizer.Contains("hasSubstantiveAction", StringComparison.Ordinal)
			&& statementNormalizer.Contains("actions.RemoveAt(index);", StringComparison.Ordinal)
			&& statementNormalizer.Contains("return removed;", StringComparison.Ordinal),
			"mixed statement plus substantive actions must drop only redundant statement entries instead of rejecting the draft");

        int parseAttempt = generatedCommit.IndexOf("if (!TryParseJsonObject(raw, out JObject json))", StringComparison.Ordinal);
        int parseRejection = generatedCommit.IndexOf(
            "RejectGeneratedDraftBeforePublication(job, raw, author, fallbackTarget, \"json_parse_failed\", null);",
            StringComparison.Ordinal);
        Test.True(parseAttempt >= 0 && parseRejection > parseAttempt,
            "an unparseable JSON response must enter the shared first-repair path");

        string inlineBindingNormalizer = ExtractSection(
            source,
            "private static bool TryNormalizeInlineResponseBinding(",
            "private static bool IsJsonStringArray(");
        int inlineNormalization = generatedCommit.IndexOf("TryNormalizeInlineResponseBinding(json", StringComparison.Ordinal);
        int legalityAfterNormalization = generatedCommit.IndexOf("TryGetGeneratedIntentLegalityViolation(", StringComparison.Ordinal);
        Test.True(inlineNormalization > parseAttempt && legalityAfterNormalization > inlineNormalization,
            "the narrow inline-binding compatibility must run after JSON parsing and before legality validation");
        Test.True(inlineBindingNormalizer.Contains("const string OfferMarker = \":offer=\";", StringComparison.Ordinal)
                  && inlineBindingNormalizer.Contains("const string ThreatMarker = \":threat=\";", StringComparison.Ordinal)
                  && inlineBindingNormalizer.Contains("authorIntent[\"commitment\"] = normalizedCommitment;", StringComparison.Ordinal)
                  && inlineBindingNormalizer.Contains("json[fieldName] = sourceDocumentId;", StringComparison.Ordinal)
                  && inlineBindingNormalizer.Contains("diplomacy_document:", StringComparison.Ordinal),
            "the compatibility path must only split the observed inline suffix into commitment and an exact document field");
        Test.True(inlineBindingNormalizer.Contains("if ((offerIndex < 0) == (threatIndex < 0)) return false;", StringComparison.Ordinal)
                  && inlineBindingNormalizer.Contains("!string.Equals(existingSource, sourceDocumentId", StringComparison.Ordinal),
            "ambiguous markers or conflicting explicit source fields must remain rejected");

        int legalityCheck = generatedCommit.IndexOf("TryGetGeneratedIntentLegalityViolation(", StringComparison.Ordinal);
        int legalityRejection = generatedCommit.IndexOf(
            "generatedTarget ?? fallbackTarget",
            legalityCheck,
            StringComparison.Ordinal);
        Test.True(legalityCheck >= 0
                  && legalityRejection > legalityCheck
                  && generatedCommit.IndexOf("legalityReason", legalityRejection, StringComparison.Ordinal) > legalityRejection,
            "a missing-field or semantic legality failure must enter the shared first-repair path with its reason");

        int sanitizedBodyCheck = generatedCommit.IndexOf("if (string.IsNullOrWhiteSpace(body))", StringComparison.Ordinal);
        int sanitizedBodyRejection = generatedCommit.IndexOf(
            "RejectGeneratedDraftBeforePublication(job, raw, author, target, \"empty_public_document\", json);",
            StringComparison.Ordinal);
        Test.True(sanitizedBodyCheck >= 0 && sanitizedBodyRejection > sanitizedBodyCheck,
            "a body emptied by public-text sanitization must receive a repair instead of direct abandonment");

        int secondStageEnvelopeCheck = generatedCommit.IndexOf(
            "if (!TryApplyGeneratedSemanticEnvelope(",
            StringComparison.Ordinal);
        int secondStageEnvelopeRejection = generatedCommit.IndexOf(
            "RejectGeneratedDraftBeforePublication(job, raw, author, target, \"generated_semantic_envelope_incomplete\", json);",
            StringComparison.Ordinal);
        int publishDocument = generatedCommit.IndexOf("AddDocument(document);", StringComparison.Ordinal);
        Test.True(secondStageEnvelopeCheck >= 0
                  && secondStageEnvelopeRejection > secondStageEnvelopeCheck
                  && publishDocument > secondStageEnvelopeRejection,
            "a second-stage envelope application failure must repair before any document publication");

        string generatedValidation = ExtractSection(
            source,
            "private bool TryGetGeneratedIntentLegalityViolation(",
            "private static bool ArePeaceTermsEquivalent(");
        int requiredEnvelopeCheck = generatedValidation.IndexOf(
            "json[\"actions\"]",
            StringComparison.Ordinal);
        int incompleteReason = generatedValidation.IndexOf(
            "reason = \"semantic_envelope_incomplete\";",
            requiredEnvelopeCheck,
            StringComparison.Ordinal);
        Test.True(requiredEnvelopeCheck >= 0
                  && incompleteReason > requiredEnvelopeCheck
                  && generatedValidation.Contains("json[\"mentioned_kingdom_ids\"]", StringComparison.Ordinal)
                  && generatedValidation.Contains("json[\"tone\"]", StringComparison.Ordinal)
                  && generatedValidation.Contains("json[\"confidence\"]", StringComparison.Ordinal),
            "a parseable response missing any required declaration field must be classified for repair");
        Test.True(generatedValidation.Contains("JArray", StringComparison.Ordinal)
                  && generatedValidation.Contains("target_kingdom_id", StringComparison.Ordinal)
                  && generatedValidation.Contains("intent", StringComparison.Ordinal)
                  && generatedValidation.Contains("peace_terms", StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "!IsJsonStringArray(json[\"mentioned_kingdom_ids\"])",
                      StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "!(json[\"round_plan\"] is JObject roundPlanEnvelope)",
                      StringComparison.Ordinal)
                  && generatedValidation.Contains(
                      "!IsJsonStringArray(roundPlanEnvelope[\"selected_kingdom_ids\"])",
                      StringComparison.Ordinal),
            "null, oversized, or mixed-token action containers must fail the first-stage envelope and enter repair");

        string stringArrayGuard = ExtractSection(
            source,
            "private static bool IsJsonStringArray(",
            "private static List<string> ReadStringList(");
        Test.True(stringArrayGuard.Contains("if (token is not JArray array) return false;", StringComparison.Ordinal)
                  && stringArrayGuard.Contains(
                      "if (item == null || item.Type != JTokenType.String) return false;",
                      StringComparison.Ordinal)
                  && stringArrayGuard.Contains("return true;", StringComparison.Ordinal),
            "kingdom and round-plan arrays must contain strings only, not objects, numbers, booleans, or nulls");

        string secondStageEnvelope = ExtractSection(
            source,
            "private bool TryApplyGeneratedSemanticEnvelope(",
            "private void CommitAnalysis(");
        foreach (string secondStageTypeGuard in new[]
        {
            "!IsJsonStringArray(json[\"addressed_kingdom_ids\"])",
            "!IsJsonStringArray(json[\"mentioned_kingdom_ids\"])",
            "!(json[\"round_plan\"] is JObject roundPlanEnvelope)",
            "!IsJsonStringArray(roundPlanEnvelope[\"selected_kingdom_ids\"])",
            "!(json[\"peace_terms\"] is JObject)"
        })
        {
            Test.True(secondStageEnvelope.Contains(secondStageTypeGuard, StringComparison.Ordinal),
                "second-stage envelope is missing defensive type guard: " + secondStageTypeGuard);
        }

        string peaceTermsParser = ExtractSection(
            source,
            "private WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(",
            "private bool IsCessionCurrentlyAllowed(");
        int peaceObjectGuard = peaceTermsParser.IndexOf(
            "if (json.SelectToken(\"peace_terms\") is not JObject token) return null;",
            StringComparison.Ordinal);
        int peaceFieldRead = peaceTermsParser.IndexOf("token[\"tribute_payer_kingdom_id\"]", StringComparison.Ordinal);
        Test.True(peaceObjectGuard >= 0 && peaceFieldRead > peaceObjectGuard,
            "peace-term parsing must reject null or array tokens before indexing treaty fields");

        string publicPeaceDisclosure = ExtractSection(
            source,
            "private bool TryGetPublicPeaceTermsDisclosureViolation(",
            "private static bool ContainsWholeNumber(");
        int disclosureObjectGuard = publicPeaceDisclosure.IndexOf(
            "json?.SelectToken(\"peace_terms\") is not JObject terms",
            StringComparison.Ordinal);
        int disclosureFieldRead = publicPeaceDisclosure.IndexOf("terms[\"daily_tribute\"]", StringComparison.Ordinal);
        Test.True(disclosureObjectGuard >= 0 && disclosureFieldRead > disclosureObjectGuard,
            "public peace-term disclosure validation must ignore malformed containers before indexing fields");

        string jsonParser = ExtractSection(
            source,
            "private static bool TryParseJsonObject(",
            "private static string ReadString(");
        Test.True(jsonParser.Contains("parsed = JObject.Parse(text);", StringComparison.Ordinal)
                  && jsonParser.Contains(
                      "parsed = JObject.Parse(text.Substring(start, end - start + 1));",
                      StringComparison.Ordinal)
                  && jsonParser.Contains("parsed = new JObject();", StringComparison.Ordinal)
                  && jsonParser.Contains("return false;", StringComparison.Ordinal),
            "TryParseJsonObject must distinguish irrecoverable JSON from fenced or prose-wrapped recoverable JSON");

        string rejectionGate = ExtractSection(
            source,
            "private void RejectGeneratedDraftBeforePublication(",
            "private bool EnqueueGeneratedDeclarationRepair(");
        int retryBudget = rejectionGate.IndexOf(
            "if (job.SemanticRepairAttempts < MaxGeneratedDraftRepairAttempts",
            StringComparison.Ordinal);
        int enqueueRepair = rejectionGate.IndexOf("EnqueueGeneratedDeclarationRepair(", StringComparison.Ordinal);
        int stopFirstFailure = rejectionGate.IndexOf("return;", enqueueRepair, StringComparison.Ordinal);
        int abandonSecondFailure = rejectionGate.IndexOf(
            "AbandonRejectedGeneration(",
            stopFirstFailure,
            StringComparison.Ordinal);
        Test.True(retryBudget >= 0
                  && enqueueRepair > retryBudget
                  && stopFirstFailure > enqueueRepair
                  && abandonSecondFailure > stopFirstFailure,
            "a first invalid draft may return only after a viable repair was enqueued; failed or non-viable repair must abandon");
        Test.True(rejectionGate.Contains(
                "&& EnqueueGeneratedDeclarationRepair(job, rejectedRaw, author, target, normalizedReason, parsedJson)",
                StringComparison.Ordinal),
            "the retry gate must pass the rejected parsed envelope and must not suppress abandonment when no legal repair action remains");
        Test.True(CountOccurrences(rejectionGate, "EnqueueGeneratedDeclarationRepair(") == 1
                  && CountOccurrences(rejectionGate, "AbandonRejectedGeneration(") == 2
                  && rejectionGate.Contains(
                      "AbandonRejectedGeneration(job, null, target, string.IsNullOrWhiteSpace(reason)",
                      StringComparison.Ordinal),
            "the shared rejection gate must not enqueue a second semantic repair");

        string generationRepair = ExtractSection(
            source,
            "private bool EnqueueGeneratedDeclarationRepair(",
            "private List<string> GetAuthorizedGenerationTargetIds(");
        foreach (string repairableFormatReason in new[]
        {
            "json_parse_failed",
            "semantic_envelope_incomplete",
            "empty_public_document",
            "generated_semantic_envelope_incomplete"
        })
        {
            Test.True(generationRepair.Contains("\"" + repairableFormatReason + "\"", StringComparison.Ordinal),
                "format repair guidance is missing reason: " + repairableFormatReason);
        }
        Test.True(generationRepair.Contains(
                "SemanticRepairAttempts = source.SemanticRepairAttempts + 1",
                StringComparison.Ordinal),
            "the repair job must persist its consumed retry so another failure abandons instead of requeueing");
        int noLegalActions = generationRepair.IndexOf("if (authorizedTargetIds.Count == 0)", StringComparison.Ordinal);
        int skipImpossibleRepair = generationRepair.IndexOf(
            "generated declaration repair skipped because no legal action remains",
            StringComparison.Ordinal);
        int rejectImpossibleRepair = generationRepair.IndexOf("return false;", noLegalActions, StringComparison.Ordinal);
        int enqueueViableRepair = generationRepair.IndexOf("EnqueueJob(repair);", StringComparison.Ordinal);
        int acceptViableRepair = generationRepair.IndexOf("return true;", enqueueViableRepair, StringComparison.Ordinal);
        Test.True(noLegalActions >= 0
                  && skipImpossibleRepair > noLegalActions
                  && rejectImpossibleRepair > skipImpossibleRepair
                  && enqueueViableRepair > rejectImpossibleRepair
                  && acceptViableRepair > enqueueViableRepair,
            "repair enqueue must report false when no legal action remains and true only after a viable repair job is queued");

        string authorizedTargets = ExtractSection(
            source,
            "private List<string> GetAuthorizedGenerationTargetIds(",
            "private void AbandonRejectedGeneration(");
        Test.True(authorizedTargets.Contains("source.TargetKingdomId", StringComparison.Ordinal)
                  && authorizedTargets.Contains("source.AllowUntargeted", StringComparison.Ordinal)
                  && authorizedTargets.Contains("source.CandidateKingdomIds", StringComparison.Ordinal)
                  && authorizedTargets.Contains("source.IsRelayTurn", StringComparison.Ordinal)
                  && authorizedTargets.Contains("round?.ResultSettlementPending == true", StringComparison.Ordinal)
                  && authorizedTargets.Contains("source.ResultSettlementSlotId", StringComparison.Ordinal)
                  && authorizedTargets.Contains(
                      "GetResultSettlementActionableTargets(round, author)",
                      StringComparison.Ordinal)
                  && authorizedTargets.Contains("round?.RelayRouteKingdomIds", StringComparison.Ordinal),
            "repair target authorization must refresh settlement-wide candidates only for a slot-owned settlement job and keep ordinary relays route-only");
        Test.True(!authorizedTargets.Contains("generatedTarget", StringComparison.Ordinal)
                  && !authorizedTargets.Contains("Kingdom target", StringComparison.Ordinal),
            "the model-selected invalid target must never expand the authorized repair target set");
        Test.True(authorizedTargets.Contains("!candidate.IsEliminated", StringComparison.Ordinal)
                  && authorizedTargets.Contains("HasIndependentWorldDiplomacyAuthority(candidate)", StringComparison.Ordinal)
                  && authorizedTargets.Contains(
					  "BuildLegalDiplomaticDeclarationIntents(",
                      StringComparison.Ordinal)
				  && authorizedTargets.Contains("source.IsRelayTurn", StringComparison.Ordinal)
				  && authorizedTargets.Contains("source.ResultSettlementSlotId", StringComparison.Ordinal)
                  && authorizedTargets.Contains(
                      ".OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
                      StringComparison.Ordinal),
            "authorized repair targets must remain live, independent, actionable, and deterministic");

        Test.True(generationRepair.Contains(
                "List<string> authorizedTargetIds = GetAuthorizedGenerationTargetIds(source, repairRound, author);",
                StringComparison.Ordinal)
                  && generationRepair.Contains(
                      "target != null && authorizedTargetIds.Contains(target.StringId, StringComparer.OrdinalIgnoreCase)",
                      StringComparison.Ordinal)
                  && generationRepair.Contains(
                      "authorizedTargetIds.Count == 1 ? ResolveKingdom(authorizedTargetIds[0]) : null",
                      StringComparison.Ordinal),
            "an unauthorized model target must fall back to the original sole target or the full authorized target range");
		Test.True(generationRepair.Contains("BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal)
				  && generationRepair.Contains("authorizedTargetIds", StringComparison.Ordinal)
				  && generationRepair.Contains("TargetKingdomId = source.TargetKingdomId", StringComparison.Ordinal)
				  && generationRepair.Contains(
					  "CandidateKingdomIds = resultSettlementRepair",
					  StringComparison.Ordinal)
				  && generationRepair.Contains(
					  "? new List<string>(authorizedTargetIds)",
					  StringComparison.Ordinal)
				  && generationRepair.Contains(
					  ": new List<string>(source.CandidateKingdomIds",
					  StringComparison.Ordinal)
                  && generationRepair.Contains("BuildBilateralState(author, repairTarget)", StringComparison.Ordinal)
                  && !generationRepair.Contains("BuildBilateralState(author, target)", StringComparison.Ordinal),
            "repair prompting and the repair job must preserve original authorization rather than narrowing to an illegal model target");
    }

    private static void RunOutputTruncationRepairContractTests(string source)
    {
        string clientSource = File.ReadAllText(FindRepositoryFile("WorldDiplomacyLlmClient.cs"), Encoding.UTF8);
        string apiResultDto = ExtractSection(
            clientSource,
            "internal sealed class WorldDiplomacyApiCallResult",
            "internal static class WorldDiplomacyLlmClient");
        Test.True(apiResultDto.Contains("public string FinishReason = \"\";", StringComparison.Ordinal)
                  && apiResultDto.Contains("public bool IsOutputTruncated;", StringComparison.Ordinal),
            "the API result must carry both the raw finish reason and an explicit truncation signal");

        string completedApiResponse = ExtractSection(
            clientSource,
            "private static WorldDiplomacyApiCallResult CompleteResult(",
            "private static void ApplyUsageStats(");
        Test.True(completedApiResponse.Contains(
                "json.SelectToken(\"choices[0].finish_reason\")",
                StringComparison.Ordinal)
                  && completedApiResponse.Contains("json[\"finish_reason\"]", StringComparison.Ordinal),
            "finish_reason must be read from both chat-completions choices and compatible root responses");
        string lengthBranch = ExtractSection(
            completedApiResponse,
            "if (finishReason == \"length\")",
            "else if (finishReason == \"content_filter\")");
        Test.True(lengthBranch.Contains("result.IsOutputTruncated = true;", StringComparison.Ordinal)
                  && lengthBranch.Contains("finish_reason=length", StringComparison.Ordinal),
            "finish_reason=length must be represented as a non-successful truncated output");
        Test.True(CountOccurrences(completedApiResponse, "result.IsOutputTruncated = true;") == 1,
            "only finish_reason=length may set the output-truncated signal");

        string contentFilterBranch = ExtractSection(
            completedApiResponse,
            "else if (finishReason == \"content_filter\")",
            "else if (string.IsNullOrWhiteSpace(result.Content))");
        string emptyContentBranch = ExtractSection(
            completedApiResponse,
            "else if (string.IsNullOrWhiteSpace(result.Content))",
            "RecordTokenStats(messages, result.Content");
        Test.True(!contentFilterBranch.Contains("IsOutputTruncated", StringComparison.Ordinal)
                  && contentFilterBranch.Contains("finish_reason=content_filter", StringComparison.Ordinal),
            "content-filter refusal must remain an ordinary failed request, not a repairable truncation");
        Test.True(!emptyContentBranch.Contains("IsOutputTruncated", StringComparison.Ordinal)
                  && emptyContentBranch.Contains("LLM returned empty content", StringComparison.Ordinal),
            "empty content must remain an ordinary failed request, not a partial draft repair");

        string requestCompletionBridge = ExtractSection(
            source,
            "private void TryStartNextLlmJob()",
            "private static bool HasCurrentCanonicalPromptContract(");
        Test.True(requestCompletionBridge.Contains(
                "result.IsOutputTruncated = api?.IsOutputTruncated == true;",
                StringComparison.Ordinal),
            "the API truncation signal must survive transfer into the queued LLM job result");
        string llmJobResultDto = ExtractSection(
            source,
            "private sealed class LlmJobResult",
            "public sealed class WorldDiplomacyRound");
        Test.True(llmJobResultDto.Contains("public bool IsOutputTruncated;", StringComparison.Ordinal),
            "the completed-job queue item must retain output truncation until main-thread commit");

        string completedJobs = ExtractSection(
            source,
            "private void ProcessCompletedJobs()",
            "private void CommitFailedJob(");
        int failedResultStart = completedJobs.IndexOf("if (!result.Success)", StringComparison.Ordinal);
        int ordinaryFailureCommit = completedJobs.IndexOf(
            "CommitFailedJob(job, result.Error);",
            failedResultStart,
            StringComparison.Ordinal);
        int ordinaryFailureContinue = completedJobs.IndexOf(
            "continue;",
            ordinaryFailureCommit,
            StringComparison.Ordinal);
        Test.True(failedResultStart >= 0
                  && ordinaryFailureCommit > failedResultStart
                  && ordinaryFailureContinue > ordinaryFailureCommit,
            "completed-job processing must retain a bounded ordinary failure branch after truncation handling");
        string failedResultBranch = completedJobs.Substring(
            failedResultStart,
            ordinaryFailureContinue + "continue;".Length - failedResultStart);
        int generateGuard = failedResultBranch.IndexOf(
            "string.Equals(job.Kind, \"generate\", StringComparison.OrdinalIgnoreCase)",
            StringComparison.Ordinal);
        int truncationGuard = failedResultBranch.IndexOf("result.IsOutputTruncated", StringComparison.Ordinal);
        int partialContentGuard = failedResultBranch.IndexOf(
            "!string.IsNullOrWhiteSpace(result.Content)",
            StringComparison.Ordinal);
        int unifiedRejection = failedResultBranch.IndexOf(
            "RejectGeneratedDraftBeforePublication(",
            StringComparison.Ordinal);
        int truncationReason = failedResultBranch.IndexOf("\"output_truncated\"", StringComparison.Ordinal);
        int removeOldJob = failedResultBranch.IndexOf("RemoveJob(job.JobId);", StringComparison.Ordinal);
        int truncationFailureReset = failedResultBranch.IndexOf(
            "_storage.ConsecutiveServiceFailures = 0;",
            partialContentGuard,
            StringComparison.Ordinal);
        int truncationTry = failedResultBranch.IndexOf("try", truncationFailureReset, StringComparison.Ordinal);
        int truncationCatch = failedResultBranch.IndexOf("catch (Exception ex)", removeOldJob, StringComparison.Ordinal);
        int truncationHandlingFailure = failedResultBranch.IndexOf(
            "CommitFailedJob(job, \"truncated generated draft handling failed: \" + ex.Message);",
            truncationCatch,
            StringComparison.Ordinal);
        int stopFailureFallthrough = failedResultBranch.IndexOf("continue;", removeOldJob, StringComparison.Ordinal);
        Test.True(generateGuard >= 0
                  && truncationGuard > generateGuard
                  && partialContentGuard > truncationGuard
                  && truncationFailureReset > partialContentGuard
                  && truncationTry > truncationFailureReset
                  && unifiedRejection > partialContentGuard
                  && truncationReason > unifiedRejection
                  && removeOldJob > truncationReason
                  && truncationCatch > removeOldJob
                  && truncationHandlingFailure > truncationCatch
                  && stopFailureFallthrough > removeOldJob,
            "a partial truncated generation must enter unified draft rejection, remove its old job, and not fall through to API failure handling");
        Test.True(truncationHandlingFailure < stopFailureFallthrough,
            "an exception while handling a truncated draft must use CommitFailedJob so the old job cannot be resent");
        Test.True(CountOccurrences(failedResultBranch, "RejectGeneratedDraftBeforePublication(") == 1
                  && failedResultBranch.Contains("if (result.IsServiceFailure)", StringComparison.Ordinal)
                  && failedResultBranch.IndexOf("if (result.IsServiceFailure)", StringComparison.Ordinal) > stopFailureFallthrough
                  && failedResultBranch.Contains("CommitFailedJob(job, result.Error);", StringComparison.Ordinal),
            "content filtering, empty output, and API/service failures must continue through ordinary failure handling");

        string rejectionGate = ExtractSection(
            source,
            "private void RejectGeneratedDraftBeforePublication(",
            "private bool EnqueueGeneratedDeclarationRepair(");
        Test.True(rejectionGate.Contains(
                "job.SemanticRepairAttempts < MaxGeneratedDraftRepairAttempts",
                StringComparison.Ordinal)
                  && rejectionGate.Contains("AbandonRejectedGeneration(", StringComparison.Ordinal),
            "a truncated repair response at attempt one must use the same gate and abandon without a second repair");
        string generationRepair = ExtractSection(
            source,
            "private bool EnqueueGeneratedDeclarationRepair(",
            "private List<string> GetAuthorizedGenerationTargetIds(");
        Test.True(generationRepair.Contains("\"output_truncated\"", StringComparison.Ordinal)
                  && generationRepair.Contains(
                      "SemanticRepairAttempts = source.SemanticRepairAttempts + 1",
                      StringComparison.Ordinal),
            "the first truncated draft must receive format guidance, while its repair consumes the sole retry");

        string generatedCommit = ExtractSection(
            source,
            "private void CommitGeneratedDocument(",
            "private bool TryGetGeneratedIntentLegalityViolation(");
        string missingAuthorCommit = ExtractSection(
            generatedCommit,
            "if (author == null)",
            "PruneInvalidOffers(");
        Test.True(missingAuthorCommit.Contains(
                "AbandonRejectedGeneration(job, null, fallbackTarget, \"generated_party_missing\");",
                StringComparison.Ordinal)
                  && !missingAuthorCommit.Contains("CompleteExchange(", StringComparison.Ordinal),
            "an author disappearing before generated commit must use abandonment cleanup for relay and root rounds");
        Test.True(rejectionGate.Contains("if (author == null)", StringComparison.Ordinal)
                  && rejectionGate.Contains(
                      "AbandonRejectedGeneration(job, null, target",
                      StringComparison.Ordinal),
            "unified rejection must also abandon safely if the author disappears before repair handling");
        string abandonGeneration = ExtractSection(
            source,
            "private void AbandonRejectedGeneration(",
            "private bool TryApplyGeneratedSemanticEnvelope(");
		Test.True(abandonGeneration.Contains("round.RelayWaiting = false;", StringComparison.Ordinal)
				  && abandonGeneration.Contains("AdvanceRelay(round, scheduleImmediately: true);", StringComparison.Ordinal)
				  && abandonGeneration.Contains("MaxConsecutiveTechnicalGenerationFailuresPerRound", StringComparison.Ordinal)
				  && abandonGeneration.Contains("CloseActiveRound(\"technical_consecutive_generation_rejections\")", StringComparison.Ordinal)
                  && abandonGeneration.Contains("CompleteExchange(job.ExchangeId, \"technical_generation_rejected\")", StringComparison.Ordinal)
                  && abandonGeneration.Contains("string.IsNullOrWhiteSpace(round.RootDocumentId)", StringComparison.Ordinal)
                  && abandonGeneration.Contains("CloseActiveRound(\"technical_generation_rejected\")", StringComparison.Ordinal),
			"generated-draft abandonment must immediately advance a relay, break repeated technical failures, or close an unpublished root round");
		string relayScheduling = ExtractMethod(
			source,
			"private void ScheduleNextRelayHop(");
		Test.True(relayScheduling.Contains("bool scheduleImmediately = false", StringComparison.Ordinal)
			&& relayScheduling.Contains("if (scheduleImmediately) plannedDay = CurrentDay();", StringComparison.Ordinal),
			"a technical generation rejection must be able to schedule the next relay speaker on the current day");
		Test.True(source.Contains("[JsonProperty(\"consecutiveTechnicalGenerationFailures\")]", StringComparison.Ordinal),
			"the consecutive technical generation failure circuit breaker must survive save/load");
    }

    private static void AssertOfferCooldownDecision(
        IReadOnlyList<WorldDiplomacyOfferCooldownDecision> decisions,
        string proposerKingdomId,
        string targetKingdomId,
        WorldDiplomacyOfferDomain domain,
        WorldDiplomacyOfferCooldownAction expectedAction,
        string message)
    {
        WorldDiplomacyOfferCooldownKey expectedKey = new(proposerKingdomId, targetKingdomId, domain);
        List<WorldDiplomacyOfferCooldownDecision> matches = decisions
            .Where(decision => decision.Key == expectedKey)
            .ToList();
        Test.True(matches.Count == 1, message + "; expected exactly one directed key");
        Test.True(matches[0].Action == expectedAction,
            message + "; expected=" + expectedAction + ", actual=" + matches[0].Action);
    }

	private static void RunRoundResponseNoActionContractTests(string source)
	{
		string authorization = ExtractMethod(
			source,
			"private bool IsNonRootAiRelayNoActionAllowed(");
		foreach (string requiredBoundary in new[]
		{
			"isRelayTurn",
			"round.RootDocumentId",
			"round.State",
			"IsPlayerKingdom(author)",
			"resultSettlementSlotId",
			"round.ResultSettlementCurrentSlotId",
			"RoundRouteContainsKingdom",
			"author.StringId",
			"target.StringId"
		})
		{
			Test.True(authorization.Contains(requiredBoundary, StringComparison.Ordinal),
				"non-root AI statement authorization is missing live relay boundary: " + requiredBoundary);
		}
		Test.True(!authorization.Contains("AllowAutonomousNoAction", StringComparison.Ordinal)
			&& !authorization.Contains("IsAutonomousNoActionDeclaration", StringComparison.Ordinal),
			"legacy autonomous no-action save fields must never authorize a new statement");
		foreach (string externalBoundary in new[]
		{
			"isExternalResponseOnly",
			"responseSource?.IsReadyForPublication",
			"responseSource.IsPlayerAuthored",
			"responseSource.RoundId, round.RoundId",
			"responseSource.AuthorKingdomId, target.StringId",
			"requiredResponder?.MandatoryReplyPending",
			"requiredResponder.LastTriggeredDocumentId, responseSource.DocumentId",
			"if (round.ResultSettlementPending) return false",
			"RoundRouteContainsKingdom(round, author.StringId)",
			"RoundRouteContainsKingdom(round, target.StringId)"
		})
		{
			Test.True(authorization.Contains(externalBoundary, StringComparison.Ordinal),
				"external player-priority statement authorization is missing boundary: " + externalBoundary);
		}
		Test.True(authorization.Contains("bool hasRelatedKingdom", StringComparison.Ordinal)
			&& authorization.Contains("? slot.RelatedKingdomIds.Contains(target.StringId", StringComparison.Ordinal)
			&& authorization.Contains(": RoundRouteContainsKingdom(round, target.StringId)", StringComparison.Ordinal),
			"an obligation settlement slot must bind statement to a related kingdom, while a pure route slot may address the route");

		string enqueueGeneration = ExtractSection(
			source,
			"private void EnqueueGenerationJob(",
			"private bool EnsureGenerationJobHasKingdomStrategicProfile(");
		Test.True(!enqueueGeneration.Contains("HasOnlyDisruptiveAutonomousActions", StringComparison.Ordinal)
			&& !enqueueGeneration.Contains("AllowAutonomousNoAction = allowAutonomousNoAction", StringComparison.Ordinal)
			&& !enqueueGeneration.Contains("AllowAutonomousNoAction = true", StringComparison.Ordinal)
			&& !enqueueGeneration.Contains("allowAutonomousNoAction = includeEmbeddedRoundPlan", StringComparison.Ordinal),
			"a new root generation job must never receive the legacy statement escape hatch");
		Test.True(enqueueGeneration.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& enqueueGeneration.Contains("isExternalResponseOnly: true", StringComparison.Ordinal)
			&& enqueueGeneration.Contains("responseSource: sourceDocument", StringComparison.Ordinal),
			"player-priority preflight must use the same source-bound statement gate as validation and publication");

		string generatedValidation = ExtractSection(
			source,
			"private bool TryGetGeneratedIntentLegalityViolation(",
			"private static bool ArePeaceTermsEquivalent(");
		Test.True(generatedValidation.Contains("IsNonRootAiRelayNoActionAllowed(", StringComparison.Ordinal)
			&& generatedValidation.Contains("job.ResultSettlementSlotId", StringComparison.Ordinal)
			&& generatedValidation.Contains("generatedTarget", StringComparison.Ordinal)
			&& generatedValidation.Contains("job.IsRelayTurn", StringComparison.Ordinal)
			&& generatedValidation.Contains("string.Equals(intent, \"statement\"", StringComparison.Ordinal)
			&& generatedValidation.Contains("non_actionable_diplomatic_intent", StringComparison.Ordinal),
			"generated statement must pass the live non-root AI relay authorization");
		Test.True(generatedValidation.Contains("job.IsExternalResponseOnly", StringComparison.Ordinal)
			&& generatedValidation.Contains("responseSource", StringComparison.Ordinal)
			&& !generatedValidation.Contains("bool allowedRoundResponseNoAction = !job.IsExternalResponseOnly", StringComparison.Ordinal),
			"external response status must be evaluated inside the shared source-bound statement gate");
		Test.True(!generatedValidation.Contains("job.AllowAutonomousNoAction", StringComparison.Ordinal),
			"root legality must not be recoverable from a legacy job flag");

		string generatedCommit = ExtractSection(
			source,
			"private void CommitGeneratedDocument(",
			"private bool TryGetGeneratedIntentLegalityViolation(");
		Test.True(!generatedCommit.Contains("document.IsAutonomousNoActionDeclaration = job.AllowAutonomousNoAction", StringComparison.Ordinal)
			&& generatedCommit.Contains("TryApplyGeneratedSemanticEnvelope(", StringComparison.Ordinal),
			"new documents must obtain their no-action stamp only from the live semantic envelope");

		string semanticEnvelope = ExtractSection(
			source,
			"private bool TryApplyGeneratedSemanticEnvelope(",
			"private void CommitAnalysis(");
		Test.True(semanticEnvelope.Contains("IsNonRootAiRelayNoActionAllowed(", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("document.ResultSettlementSlotId", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("relayTurn", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("document.IsRoundResponseNoActionDeclaration", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("if (!IsActionableDiplomacyIntent(intent)", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("document.RequiresResponse", StringComparison.Ordinal),
			"the secondary envelope must reauthorize, stamp, and make a relay statement non-responsive");
		Test.True(semanticEnvelope.Contains("document.IsExternalResponseOnly", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("ResolveDocument(document.SourceDocumentId)", StringComparison.Ordinal)
			&& !semanticEnvelope.Contains("bool allowedRoundResponseNoAction = !document.IsExternalResponseOnly", StringComparison.Ordinal),
			"the semantic envelope must feed external source identity through the shared authorization gate");
		Test.True(!semanticEnvelope.Contains("allowAutonomousNoAction", StringComparison.Ordinal)
			&& !semanticEnvelope.Contains("allowedAutonomousNoAction", StringComparison.Ordinal),
			"the semantic envelope must not retain a root statement parameter");

		string dynamicOptions = ExtractMethod(
			source,
			"private string BuildCurrentLegalDiplomaticOptions(");
		string actionSignature = ExtractMethod(
			source,
			"private string BuildGenerationLegalActionSignature(");
		string declarationIntents = ExtractMethod(
			source,
			"private List<string> BuildLegalDiplomaticDeclarationIntents(");
		Test.True(declarationIntents.Contains("IsNonRootAiRelayNoActionAllowed", StringComparison.Ordinal)
			&& declarationIntents.Contains("intents.Add(\"statement\")", StringComparison.Ordinal),
			"statement must be layered onto the generation-only action list instead of polluting root action discovery");
		Test.True(dynamicOptions.Contains("BuildLegalDiplomaticDeclarationIntents", StringComparison.Ordinal),
			"the prompt may expose statement only from the live non-root relay gate");
		Test.True(actionSignature.Contains("BuildLegalDiplomaticDeclarationIntents", StringComparison.Ordinal),
			"queued and repaired relay jobs must become stale when statement authorization changes");
		Test.True(actionSignature.Contains("job.IsExternalResponseOnly", StringComparison.Ordinal)
			&& actionSignature.Contains("responseSource", StringComparison.Ordinal),
			"the legal-action signature must derive external statement availability from the same source-bound gate");
		string repair = ExtractSection(
			source,
			"private bool EnqueueGeneratedDeclarationRepair(",
			"private void AbandonRejectedGeneration(");
		Test.True(repair.Contains("source.IsExternalResponseOnly", StringComparison.Ordinal)
			&& repair.Contains("ResolveDocument(source.SourceDocumentId)", StringComparison.Ordinal)
			&& repair.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal),
			"repair options and authorized targets must retain the same external source-bound statement gate");
		string externalRelayPrompt = ExtractSection(
			source,
			"private string BuildRelayConversationTurnPrompt(",
			"private static void AppendRoundSubstantiveProgressRequirement(");
		Test.True(externalRelayPrompt.Contains("isExternalResponseOnly: priorityResponseOnly", StringComparison.Ordinal)
			&& externalRelayPrompt.Contains("responseSource: prioritySource", StringComparison.Ordinal),
			"relay prompt options must use the same external source-bound statement gate");
		string mandatoryResponse = ExtractSection(
			source,
			"private void TryScheduleMandatoryCourtResponse(",
			"private bool HasKingdomRespondedToDocument(");
		int bindRequiredSource = mandatoryResponse.IndexOf("participant.LastTriggeredDocumentId = trigger.DocumentId", StringComparison.Ordinal);
		int enqueueRequiredResponse = mandatoryResponse.IndexOf("EnqueueGenerationJob(receiver, target", StringComparison.Ordinal);
		Test.True(bindRequiredSource >= 0 && enqueueRequiredResponse > bindRequiredSource,
			"mandatory source identity must be bound before shared preflight evaluates external statement eligibility");
		string fixedDeclarationContract = ExtractSection(
			source,
			"private static string BuildDiplomaticDeclarationModeContract()",
			"private static string BuildCanonicalHistoryCompressionModeContract()");
		Test.True(fixedDeclarationContract.Contains("回合首篇不得使用statement", StringComparison.Ordinal),
			"the shared declaration contract must explicitly keep every root action-only");
		string relayPrompt = ExtractSection(
			source,
			"private string BuildRelayConversationTurnPrompt(",
			"private static void AppendRoundSubstantiveProgressRequirement(");
		Test.True(relayPrompt.Contains("statement", StringComparison.Ordinal)
			&& relayPrompt.Contains("结构化谈判动作", StringComparison.Ordinal)
			&& relayPrompt.Contains("negotiation_move", StringComparison.Ordinal)
			&& relayPrompt.Contains("不得用空泛立场冒充新进展", StringComparison.Ordinal),
			"the relay-only prompt must require a meaningful structured negotiation move");

		string analyzedPublication = ExtractSection(
			source,
			"private void ProcessAnalyzedDocument(",
			"private bool TryGetPlayerWorldStateIntentViolation(");
		Test.True(analyzedPublication.Contains("document.IsRoundResponseNoActionDeclaration", StringComparison.Ordinal)
			&& analyzedPublication.Contains("IsNonRootAiRelayNoActionAllowed(", StringComparison.Ordinal)
			&& analyzedPublication.Contains("document.ResultSettlementSlotId", StringComparison.Ordinal)
			&& analyzedPublication.Contains("document.IsRelayTurn", StringComparison.Ordinal)
			&& analyzedPublication.Contains("stale_round_response_no_action_declaration", StringComparison.Ordinal)
			&& !analyzedPublication.Contains("document.IsAutonomousNoActionDeclaration", StringComparison.Ordinal),
			"publication must revalidate only the new non-root AI relay statement stamp");
		Test.True(analyzedPublication.Contains("document.IsExternalResponseOnly", StringComparison.Ordinal)
			&& analyzedPublication.Contains("ResolveDocument(document.SourceDocumentId)", StringComparison.Ordinal),
			"final publication must revalidate the same external source-bound statement authorization");
		int mechanicsGuard = analyzedPublication.IndexOf("if (!allowedNoAction)", StringComparison.Ordinal);
		int historyPublication = analyzedPublication.IndexOf("AppendCanonicalDocumentEvents(document)", StringComparison.Ordinal);
		Test.True(mechanicsGuard >= 0 && historyPublication > mechanicsGuard,
			"a valid statement must bypass action mechanics while remaining publishable");

		string roundProgress = ExtractSection(
			source,
			"private void HandleRoundDocumentProcessed(",
			"private void RetryDeferredRoundProgress(");
		Test.True(!roundProgress.Contains("CloseActiveRound(\"autonomous_no_action_declaration\")", StringComparison.Ordinal)
			&& !roundProgress.Contains("if (document.IsAutonomousNoActionDeclaration)", StringComparison.Ordinal),
			"a relay statement must not use the retired root-close shortcut");
		Test.True(!roundProgress.Contains("document.RoundParticipation = \"withdraw\"", StringComparison.Ordinal)
			&& roundProgress.Contains("AdvanceRelay(round)", StringComparison.Ordinal),
			"an ordinary negotiation statement must keep its participant in the back-and-forth relay");
		int consumeSettlement = roundProgress.IndexOf("ConsumeResultSettlementSpeaker(round, document)", StringComparison.Ordinal);
		int expireSettlementOffers = roundProgress.IndexOf(
			"ExpireUnansweredSettlementOffersForNoActionDeclaration(round, document)",
			StringComparison.Ordinal);
		int refreshSettlement = roundProgress.IndexOf("RefreshResultSettlementActionSlots(round)", consumeSettlement, StringComparison.Ordinal);
		int scheduleSettlement = roundProgress.IndexOf("ScheduleNextResultSettlementTurn(round)", refreshSettlement, StringComparison.Ordinal);
		Test.True(expireSettlementOffers >= 0 && consumeSettlement > expireSettlementOffers
			&& refreshSettlement > consumeSettlement && scheduleSettlement > refreshSettlement,
			"a settlement statement must consume its current slot and continue other pending settlement speakers");
		string expireOffers = ExtractMethod(
			source,
			"private void ExpireUnansweredSettlementOffersForNoActionDeclaration(");
		Test.True(expireOffers.Contains("document.IsRoundResponseNoActionDeclaration", StringComparison.Ordinal)
			&& expireOffers.Contains("document.ResultSettlementSlotId", StringComparison.Ordinal)
			&& expireOffers.Contains("slot.SourceDocumentIds", StringComparison.Ordinal)
			&& expireOffers.Contains("offer.Status = \"expired\"", StringComparison.Ordinal)
			&& !expireOffers.Contains("offer.Status = \"rejected\"", StringComparison.Ordinal),
			"a statement must leave only this slot's unanswered offers unaccepted so refresh cannot schedule the same AI again");

		Test.True(source.Contains("[JsonProperty(\"isRoundResponseNoActionDeclaration\")]", StringComparison.Ordinal)
			&& source.Contains("public bool IsRoundResponseNoActionDeclaration", StringComparison.Ordinal),
			"the validated relay statement stamp must survive save/load");
		string documentDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyDocument",
			"public sealed class WorldDiplomacyJob");
		string jobDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyJob",
			"public sealed class WorldDiplomacyCanonicalHistoryState");
		Test.True(documentDto.Contains("IsAutonomousNoActionDeclaration", StringComparison.Ordinal)
			&& jobDto.Contains("AllowAutonomousNoAction", StringComparison.Ordinal),
			"legacy no-action fields may remain only as inert save compatibility data");
	}

	private static void RunWarResponseNoActionContractTests(string source)
	{
		string authorization = ExtractMethod(
			source,
			"private bool IsWarResponseNoActionAllowed(");
		foreach (string requiredBoundary in new[]
		{
			"round?.ResultSettlementPending",
			"round.ResultSettlementCurrentSlotId",
			"slotId",
			"x.KingdomId",
			"author.StringId",
			"SettlementSlotHasKind(slot, \"war_response\")",
			"slot.SourceDocumentIds",
			"ResolveDocument",
			"IsReadyForPublication",
			"war.Actions",
			"NormalizeIntent(x.Intent)",
			"\"declare_war\"",
			"x.ChangedDiplomaticState",
			"war.AuthorKingdomId",
			"target.StringId",
			"x.TargetKingdomId"
		})
		{
			Test.True(authorization.Contains(requiredBoundary, StringComparison.Ordinal),
				"war-response audit marker is missing the exact successful-war boundary: "
				+ requiredBoundary);
		}
		Test.True(authorization.Contains("war.Actions == null || war.Actions.Count == 0", StringComparison.Ordinal),
			"legacy single-action war mirrors may be consulted only when the persisted actions collection is absent or empty");
		string generatedCommit = ExtractSection(
			source,
			"private void CommitGeneratedDocument(",
			"private bool TryGetGeneratedIntentLegalityViolation(");
		Test.True(generatedCommit.Contains("TryApplyGeneratedSemanticEnvelope(", StringComparison.Ordinal)
			&& generatedCommit.Contains("document.ResultSettlementSlotId = job.ResultSettlementSlotId", StringComparison.Ordinal),
			"the generated document must carry its exact persisted slot into the secondary live authorization");

		string semanticEnvelope = ExtractSection(
			source,
			"private bool TryApplyGeneratedSemanticEnvelope(",
			"private void CommitAnalysis(");
		Test.True(semanticEnvelope.Contains("IsRoundResponseNoActionDeclaration", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("IsWarResponseNoActionAllowed(envelopeRound, document.ResultSettlementSlotId, author, target)", StringComparison.Ordinal)
			&& semanticEnvelope.Contains("document.IsWarResponseNoActionDeclaration", StringComparison.Ordinal),
			"the general relay statement stamp must retain the exact successful-war audit marker");
		Test.True(CountOccurrences(source, "document.IsWarResponseNoActionDeclaration =")
			== CountOccurrences(semanticEnvelope, "document.IsWarResponseNoActionDeclaration ="),
			"only generated semantic-envelope validation may stamp a successful-war response; player, analysis, and external-fact paths must not manufacture it");

		string analyzedPublication = ExtractSection(
			source,
			"private void ProcessAnalyzedDocument(",
			"private bool TryGetPlayerWorldStateIntentViolation(");
		Test.True(analyzedPublication.Contains("document.IsRoundResponseNoActionDeclaration", StringComparison.Ordinal)
			&& !analyzedPublication.Contains("document.IsAutonomousNoActionDeclaration", StringComparison.Ordinal),
			"publication authorization must use the general non-root relay stamp rather than the war audit flag");
		int noActionMechanicsGuard = analyzedPublication.IndexOf("if (!allowedNoAction)", StringComparison.Ordinal);
		int historyPublication = analyzedPublication.IndexOf("AppendCanonicalDocumentEvents(document)", StringComparison.Ordinal);
		Test.True(noActionMechanicsGuard >= 0 && historyPublication > noActionMechanicsGuard,
			"no-action declarations need one explicit mechanics guard before ordinary history publication");
		foreach (string forbiddenMechanism in new[]
		{
			"ApplyDocumentPressure(document)",
			"ExecuteImmediateIntent(",
			"ProcessDiplomaticThreatDocument(",
			"TrySettleRelayOffer(document)",
			"ApplyDiplomaticPressureEffect(document)"
		})
		{
			int mechanism = analyzedPublication.IndexOf(forbiddenMechanism, noActionMechanicsGuard, StringComparison.Ordinal);
			Test.True(mechanism > noActionMechanicsGuard && mechanism < historyPublication,
				"war-response statement must bypass this mechanism while remaining publishable: " + forbiddenMechanism);
		}
		int targetDecision = analyzedPublication.IndexOf(
			"RecordDiplomaticThreatTargetDecisions(document, author, target, normalizedIntent)",
			noActionMechanicsGuard,
			StringComparison.Ordinal);
		Test.True(targetDecision > noActionMechanicsGuard && targetDecision < historyPublication,
			"a statement remains the target kingdom's next published declaration, so absent comply_ultimatum must still record noncompliance");
		Test.True(analyzedPublication.Contains("DeferUnresolvedRequiredThreatAction(document, author, target, normalizedIntent)", StringComparison.Ordinal)
			&& analyzedPublication.Contains("SettleDiplomaticThreatFollowThroughAfterDeclaration(document, author)", StringComparison.Ordinal),
			"existing next-declaration threat consequences must not be bypassed by the war-response no-action exception");

		Test.True(source.Contains("[JsonProperty(\"isWarResponseNoActionDeclaration\")]", StringComparison.Ordinal)
			&& source.Contains("public bool IsWarResponseNoActionDeclaration", StringComparison.Ordinal),
			"the validated document stamp must survive save/load");
		string jobDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyJob",
			"public sealed class WorldDiplomacyCanonicalHistoryState");
		Test.True(!jobDto.Contains("AllowWarResponseNoAction", StringComparison.Ordinal),
			"job authorization must be re-derived from the persisted live slot and war document instead of trusting a stale boolean");
	}

	private static void RunMultiTargetDeclarationContractTests(string source)
	{
		Test.True(source.Contains("private const int MaxDiplomaticActionsPerDocument = 4;", StringComparison.Ordinal),
			"one declaration must have a hard four-action bound before any per-target work begins");

		string documentDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyDocument",
			"public sealed class WorldDiplomacyExchange");
		Test.True(documentDto.Contains("[JsonProperty(\"actions\"", StringComparison.Ordinal)
			&& documentDto.Contains("public List<WorldDiplomacyDocumentAction> Actions { get; set; }", StringComparison.Ordinal)
			&& !documentDto.Contains("Actions { get; set; } = new", StringComparison.Ordinal),
			"document actions must be persisted but default null so old saves remain distinguishable for one-time promotion");
		Test.True(source.Contains("public sealed class WorldDiplomacyDocumentAction", StringComparison.Ordinal),
			"multi-target declarations need a persisted per-action DTO");
		foreach (string requiredActionField in new[]
		{
			"[JsonProperty(\"actionId\")]",
			"[JsonProperty(\"targetKingdomId\")]",
			"[JsonProperty(\"intent\")]",
			"[JsonProperty(\"commitment\")]",
			"[JsonProperty(\"respondingToOfferDocumentId\")]",
			"[JsonProperty(\"respondingToOfferActionId\")]",
			"[JsonProperty(\"respondingToThreatDocumentId\")]",
			"[JsonProperty(\"respondingToThreatActionId\")]",
			"[JsonProperty(\"peaceTerms\")]",
			"[JsonProperty(\"requiresResponse\")]",
			"[JsonProperty(\"changedDiplomaticState\")]",
			"[JsonProperty(\"mechanicalResult\")]"
		})
		{
			Test.True(source.Contains(requiredActionField, StringComparison.Ordinal),
				"per-target action persistence is missing field: " + requiredActionField);
		}

		string offerDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyRoundOffer",
			"public sealed class WorldDiplomacyOfferCooldown");
		Test.True(offerDto.Contains("[JsonProperty(\"sourceActionId\")]", StringComparison.Ordinal)
			&& offerDto.Contains("public string SourceActionId", StringComparison.Ordinal),
			"two offers published in one document must retain their exact source action id");
		foreach (string threatActionField in new[]
		{
			"WarningActionId", "UltimatumActionId", "StageActionId",
			"TargetDecisionActionId", "ComplianceActionId", "ResolutionActionId"
		})
		{
			Test.True(source.Contains(threatActionField, StringComparison.Ordinal),
				"threat lifecycle persistence is missing action identity: " + threatActionField);
		}
		string nonComplianceEventDto = ExtractSection(
			source,
			"public sealed class WorldDiplomacyThreatNonComplianceEvent",
			"public sealed class WorldDiplomacyThreat");
		Test.True(nonComplianceEventDto.Contains("StageActionId", StringComparison.Ordinal)
			&& nonComplianceEventDto.Contains("DecisionActionId", StringComparison.Ordinal),
			"each persisted noncompliance event must bind both the threat-stage action and decision action");

		string fixedDeclarationContract = ExtractSection(
			source,
			"private static string BuildDiplomaticDeclarationModeContract()",
			"private static string BuildCanonicalHistoryCompressionModeContract()");
		Test.True(fixedDeclarationContract.Contains("\\\"actions\\\":[{", StringComparison.Ordinal)
			&& fixedDeclarationContract.Contains("\\\"target_kingdom_id\\\"", StringComparison.Ordinal)
			&& fixedDeclarationContract.Contains("\\\"intent\\\":\\\"当前可选动作\\\"", StringComparison.Ordinal)
			&& fixedDeclarationContract.Contains("\\\"peace_terms\\\"", StringComparison.Ordinal)
			&& !fixedDeclarationContract.Contains("author_intent", StringComparison.Ordinal)
			&& !fixedDeclarationContract.Contains("primary_target_kingdom_id", StringComparison.Ordinal),
			"fixed DECLARE output must stay a short actions array; source bindings and derived fields belong to code");
		Test.True(fixedDeclarationContract.Contains("回合首篇不得使用statement", StringComparison.Ordinal)
			&& fixedDeclarationContract.Contains("statement必须单独使用", StringComparison.Ordinal),
			"the concise fixed contract must state the two statement boundaries without restoring an intent word list");

		string generatedNormalization = ExtractSection(
			source,
			"private static void NormalizeGeneratedDiplomaticEnvelopeShape(",
			"private static bool TryParseJsonObject(");
		Test.True(generatedNormalization.Contains("json[\"actions\"]", StringComparison.Ordinal)
			&& generatedNormalization.Contains("author_intent", StringComparison.Ordinal)
			&& generatedNormalization.Contains("primary_target_kingdom_id", StringComparison.Ordinal)
			&& generatedNormalization.Contains("new JArray", StringComparison.Ordinal),
			"the input normalizer must promote a legacy single-action JSON envelope into the new actions array");

		string storageNormalization = ExtractMethod(source, "private void NormalizeStorage(bool allowWorldValidation = false)");
		Test.True(!storageNormalization.Contains("document.Actions ??=", StringComparison.Ordinal)
			&& !storageNormalization.Contains("document.Actions = new List<WorldDiplomacyDocumentAction>", StringComparison.Ordinal),
			"save normalization must not allocate actions for every legacy document; null remains the legacy single-action marker");

		string generatedValidation = ExtractSection(
			source,
			"private bool TryGetGeneratedIntentLegalityViolation(",
			"private static bool ArePeaceTermsEquivalent(");
		Test.True(generatedValidation.Contains("json[\"actions\"] is not JArray actions", StringComparison.Ordinal)
			&& generatedValidation.Contains("actions.Count < 1", StringComparison.Ordinal)
			&& generatedValidation.Contains("actions.Count > MaxDiplomaticActionsPerDocument", StringComparison.Ordinal),
			"the generated root must contain between one and four directed action entries");
		Test.True(generatedValidation.Contains("HashSet<string> targetIds", StringComparison.Ordinal)
			&& generatedValidation.Contains("!targetIds.Add(targetId)", StringComparison.Ordinal),
			"one document must reject a missing target or a second action aimed at the same target");
		Test.True(generatedValidation.Contains("statementCount", StringComparison.Ordinal)
			&& generatedValidation.Contains("actions.Count != 1", StringComparison.Ordinal)
			&& generatedValidation.Contains("IsNonRootAiRelayNoActionAllowed(", StringComparison.Ordinal)
			&& generatedValidation.Contains("!IsActionableDiplomacyIntent(intent) && !allowedRoundResponseNoAction", StringComparison.Ordinal),
			"statement must be exclusive and authorized only for a non-root AI relay turn");
		Test.True(generatedValidation.Contains("outgoingThreatCount", StringComparison.Ordinal)
			&& generatedValidation.Contains("intent == \"warning\" || intent == \"ultimatum\"", StringComparison.Ordinal)
			&& generatedValidation.Contains("outgoingThreatCount > 1", StringComparison.Ordinal),
			"one declaration may contain at most one outbound warning or ultimatum");
		Test.True(generatedValidation.Contains("IsAutonomousOpeningJob(job)", StringComparison.Ordinal)
			&& generatedValidation.Contains("round_plan.selected_kingdom_ids", StringComparison.Ordinal)
			&& generatedValidation.Contains("targetIds.Any", StringComparison.Ordinal),
			"every root action target must be included in the same persisted round plan");
		Test.True(generatedValidation.Contains("for (int index = 0; index < actions.Count; index++)", StringComparison.Ordinal)
			&& generatedValidation.Contains("BuildGeneratedSingleActionEnvelope(json, action)", StringComparison.Ordinal)
			&& generatedValidation.Contains("TryGetGeneratedSingleActionLegalityViolation(", StringComparison.Ordinal)
			&& generatedValidation.Contains("CopyDerivedGeneratedActionEnvelope(single, action)", StringComparison.Ordinal),
			"each action entry must be independently normalized and validated before the batch is accepted");
		Test.True(generatedValidation.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& generatedValidation.Contains("TryGetDiplomaticStateViolation(", StringComparison.Ordinal)
			&& generatedValidation.Contains("TryGetDiplomaticThreatIntentViolation(", StringComparison.Ordinal)
			&& generatedValidation.Contains("ParseAndValidatePeaceTerms(", StringComparison.Ordinal),
			"each directed action must independently cross target, live-state, source, threat, and peace-term gates");
		string derivedActionStructure = ExtractMethod(
			source,
			"private bool TryDeriveGeneratedDiplomaticStructure(");
		Test.True(derivedActionStructure.Contains("responding_to_offer_action_id", StringComparison.Ordinal)
			&& derivedActionStructure.Contains("json[\"responding_to_offer_action_id\"] = offerActionId", StringComparison.Ordinal)
			&& derivedActionStructure.Contains("responding_to_threat_action_id", StringComparison.Ordinal)
			&& derivedActionStructure.Contains("StageActionId", StringComparison.Ordinal),
			"code must derive each offer/threat action source id instead of trusting extra LLM fields");

		string legalOptions = ExtractMethod(source, "private string BuildCurrentLegalDiplomaticOptions(");
		Test.True(legalOptions.Contains("foreach (string id", StringComparison.Ordinal)
			&& legalOptions.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal)
			&& legalOptions.Contains("if (normalizedActions.Count == 0) continue;", StringComparison.Ordinal)
			&& legalOptions.Contains("lines.Add(id + \"=\" + string.Join(\"/\", normalizedActions))", StringComparison.Ordinal),
			"the dynamic prompt must expose a separate legal-action list per target and omit targets with no executable action");

		string generatedEnvelope = ExtractSection(
			source,
			"private bool TryApplyGeneratedSemanticEnvelope(",
			"private void CommitAnalysis(");
		Test.True(generatedEnvelope.Contains("document.Actions", StringComparison.Ordinal)
			&& generatedEnvelope.Contains("action.TargetKingdomId", StringComparison.Ordinal)
			&& generatedEnvelope.Contains("document.AddressedKingdomIds", StringComparison.Ordinal)
			&& generatedEnvelope.Contains("ActionId = \"action_\" + (index + 1)", StringComparison.Ordinal),
			"addressed kingdoms must be derived from validated action targets rather than trusted as model output");

		string analyzedPublication = ExtractSection(
			source,
			"private void ProcessAnalyzedDocument(",
			"private bool TryGetPlayerWorldStateIntentViolation(");
		Test.True(analyzedPublication.Contains("document?.Actions", StringComparison.Ordinal)
			&& analyzedPublication.Contains("action.ChangedDiplomaticState", StringComparison.Ordinal)
			&& analyzedPublication.Contains("action.MechanicalResult", StringComparison.Ordinal)
			&& analyzedPublication.Contains("catch (Exception", StringComparison.Ordinal),
			"mechanical execution must isolate each action result so one failure cannot discard the document or later actions");

		string relayOffers = ExtractMethod(source, "private void TrySettleRelayOffer(");
		Test.True(relayOffers.Contains("SourceActionId", StringComparison.Ordinal)
			&& relayOffers.Contains("ProcessingActionId", StringComparison.Ordinal)
			&& relayOffers.Contains("RespondingToOfferActionId", StringComparison.Ordinal)
			&& relayOffers.Contains("SourceDocumentId", StringComparison.Ordinal),
			"offer registration and response matching must use an exact document/action source pair");
		Test.True(relayOffers.Contains("RemoveAll", StringComparison.Ordinal)
			&& relayOffers.Contains("SourceActionId", StringComparison.Ordinal),
			"registering a second proposal from the same document must not delete its sibling action's offer");

		string multiActionProcessing = ExtractMethod(source, "private void ProcessAnalyzedMultiActionDocument(");
		Test.True(multiActionProcessing.Contains("actions.Count < 1", StringComparison.Ordinal)
			&& multiActionProcessing.Contains("actions.Count > MaxDiplomaticActionsPerDocument", StringComparison.Ordinal),
			"the publication boundary must re-enforce the one-to-four action cap after generation and save/load");
		int actionLoop = multiActionProcessing.IndexOf(
			"for (int index = 0; index < actions.Count; index++)",
			StringComparison.Ordinal);
		int setActionContext = multiActionProcessing.IndexOf(
			"document.ProcessingActionId = action.ActionId",
			actionLoop,
			StringComparison.Ordinal);
		int narrowAddressedTargets = multiActionProcessing.IndexOf(
			"document.AddressedKingdomIds = new List<string> { target.StringId }",
			setActionContext,
			StringComparison.Ordinal);
		int applyPressure = multiActionProcessing.IndexOf(
			"ApplyDocumentPressure(document)",
			narrowAddressedTargets,
			StringComparison.Ordinal);
		int executeThreat = multiActionProcessing.IndexOf(
			"ProcessDiplomaticThreatDocument(document, author, target",
			setActionContext,
			StringComparison.Ordinal);
		int executeOffer = multiActionProcessing.IndexOf(
			"TrySettleRelayOffer(document",
			setActionContext,
			StringComparison.Ordinal);
		int saveActionResult = multiActionProcessing.IndexOf(
			"action.ChangedDiplomaticState = document.ChangedDiplomaticState",
			executeOffer,
			StringComparison.Ordinal);
		Test.True(actionLoop >= 0 && setActionContext > actionLoop
			&& narrowAddressedTargets > setActionContext && applyPressure > narrowAddressedTargets
			&& executeThreat > setActionContext && executeOffer > setActionContext
			&& saveActionResult > executeOffer,
			"pressure, threats, offers, and mechanical results must execute under only the current target/action before advancing");
		Test.True(CountOccurrences(multiActionProcessing, "AppendCanonicalDocumentEvents(document)") == 1
			&& CountOccurrences(multiActionProcessing, "StartDocumentPropagation(document, author)") == 1
			&& CountOccurrences(multiActionProcessing, "HandleRoundDocumentProcessed(document)") == 1
			&& CountOccurrences(multiActionProcessing, "SettleInternationalReputationForDocument(document)") == 1,
			"one multi-action document must settle reputation, publish history, propagate, and consume its round turn exactly once");
		Test.True(multiActionProcessing.Contains("new List<Kingdom>(actions.Count)", StringComparison.Ordinal)
			&& multiActionProcessing.Contains("new HashSet<string>(StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal)
			&& !multiActionProcessing.Contains("Kingdom.All", StringComparison.Ordinal),
			"the bounded four-action publication path must preallocate small collections and avoid a full-world scan per document");

		string threatCreation = ExtractMethod(source, "private bool RegisterOrAdvanceDiplomaticThreat(");
		foreach (string actionBinding in new[]
		{
			"WarningActionId = document.ProcessingActionId",
			"UltimatumActionId = document.ProcessingActionId",
			"StageActionId = document.ProcessingActionId"
		})
		{
			Test.True(threatCreation.Contains(actionBinding, StringComparison.Ordinal),
				"threat creation/advancement is missing exact action binding: " + actionBinding);
		}
		string threatCompliance = ExtractMethod(source, "private bool ResolveDiplomaticThreatCompliance(");
		Test.True(threatCompliance.Contains("RespondingToThreatActionId", StringComparison.Ordinal)
			&& threatCompliance.Contains("StageActionId", StringComparison.Ordinal)
			&& threatCompliance.Contains("ComplianceActionId = document.ProcessingActionId", StringComparison.Ordinal)
			&& threatCompliance.Contains("ResolutionActionId = document.ProcessingActionId", StringComparison.Ordinal),
			"ultimatum compliance must match the source action and persist the deciding action id");
		string threatDecisions = ExtractMethod(source, "private void RecordDiplomaticThreatTargetDecisionsForActions(");
		Test.True(threatDecisions.Contains("threat.IssuerKingdomId", StringComparison.Ordinal)
			&& threatDecisions.Contains(".ActionId", StringComparison.Ordinal)
			&& threatDecisions.Contains("TargetDecisionActionId", StringComparison.Ordinal)
			&& !threatDecisions.Contains(
				"threat.TargetDecisionActionId = document.ProcessingActionId ?? \"\";\r\n\t\t\tthreat.TargetDecisionActionId = \"\";",
				StringComparison.Ordinal)
			&& !threatDecisions.Contains(
				"threat.TargetDecisionActionId = document.ProcessingActionId ?? \"\";\n\t\t\tthreat.TargetDecisionActionId = \"\";",
				StringComparison.Ordinal),
			"multi-action noncompliance must retain the action directed at the issuer when one exists and must not erase a just-written action id");
		string threatExecution = ExtractMethod(source, "private void ProcessDiplomaticThreatDocument(");
		Test.True(threatExecution.Contains("ResolutionActionId = document.ProcessingActionId", StringComparison.Ordinal),
			"war enforcement must settle the exact declare-war action rather than only its containing document");
		string threatFollowThrough = ExtractMethod(source, "private void SettleDiplomaticThreatFollowThroughAfterDeclaration(");
		Test.True(threatFollowThrough.Contains("document.Actions?.FirstOrDefault", StringComparison.Ordinal)
			&& threatFollowThrough.Contains("threat.TargetKingdomId", StringComparison.Ordinal)
			&& threatFollowThrough.Contains("matchingAction?.Intent", StringComparison.Ordinal)
			&& threatFollowThrough.Contains("matchingAction?.ChangedDiplomaticState", StringComparison.Ordinal),
			"a warning/ultimatum obligation must evaluate the sibling action aimed at its target, not blindly use the first action");
		string nonComplianceCapture = ExtractMethod(source, "private static void CaptureDiplomaticThreatNonComplianceEvent(");
		Test.True(nonComplianceCapture.Contains("StageActionId", StringComparison.Ordinal)
			&& nonComplianceCapture.Contains("DecisionActionId", StringComparison.Ordinal),
			"a threat noncompliance event must copy both stage and decision action ids");

		string canonicalEvents = ExtractMethod(source, "private void AppendCanonicalDocumentEvents(");
		Test.True(canonicalEvents.Contains("document.Actions", StringComparison.Ordinal)
			&& canonicalEvents.Contains("action.ActionId", StringComparison.Ordinal)
			&& canonicalEvents.Contains("action.TargetKingdomId", StringComparison.Ordinal)
			&& canonicalEvents.Contains("action.Intent", StringComparison.Ordinal)
			&& canonicalEvents.Contains(".ChangedDiplomaticState", StringComparison.Ordinal)
			&& canonicalEvents.Contains("action.MechanicalResult", StringComparison.Ordinal),
			"canonical history must preserve per-target declared actions and only their own confirmed results");

		MultiActionLoadFixture persisted = new()
		{
			DocumentId = "diplomacy_document:batch-load",
			Actions = new List<MultiActionLoadEntry>
			{
				new()
				{
					ActionId = "action:trade",
					TargetKingdomId = "south_empire",
					Intent = "propose_trade",
					RespondingToOfferActionId = "",
					ChangedDiplomaticState = false,
					MechanicalResult = "提议已登记"
				},
				new()
				{
					ActionId = "action:war",
					TargetKingdomId = "sturgia",
					Intent = "declare_war",
					RespondingToThreatActionId = "action:ultimatum-source",
					ChangedDiplomaticState = true,
					MechanicalResult = "已宣战"
				}
			}
		};
		MultiActionLoadFixture? loaded = JsonSerializer.Deserialize<MultiActionLoadFixture>(
			JsonSerializer.Serialize(persisted));
		Test.True(loaded?.Actions?.Count == 2,
			"a multi-target declaration must retain every action across a save/load round trip");
		Test.True(loaded?.Actions?[0].ActionId == "action:trade"
			&& loaded.Actions[0].TargetKingdomId == "south_empire"
			&& loaded.Actions[0].Intent == "propose_trade"
			&& !loaded.Actions[0].ChangedDiplomaticState,
			"the first action's identity, target, intent, and independent failure state must survive save/load");
		Test.True(loaded?.Actions?[1].RespondingToThreatActionId == "action:ultimatum-source"
			&& loaded.Actions[1].ChangedDiplomaticState
			&& loaded.Actions[1].MechanicalResult == "已宣战",
			"the second action's source binding and successful mechanical result must survive save/load");
	}

	private static void RunPermanentAllianceContractTests(string worldDiplomacySource)
	{
		string guard = File.ReadAllText(
			FindRepositoryFile("PermanentAllianceGuard.cs"),
			Encoding.UTF8);
		string diplomacy = File.ReadAllText(
			FindRepositoryFile("DiplomacyBehavior.cs"),
			Encoding.UTF8);
		string declareWarPatch = File.ReadAllText(
			FindRepositoryFile("Patch_Meeting_SuppressDeclareWarAction.cs"),
			Encoding.UTF8);
		string subModule = File.ReadAllText(
			FindRepositoryFile("SubModule.cs"),
			Encoding.UTF8);

		Test.True(guard.Contains("AllianceCampaignBehavior", StringComparison.Ordinal)
			&& guard.Contains("EndAlliance", StringComparison.Ordinal)
			&& guard.Contains("Prefix", StringComparison.Ordinal),
			"every vanilla EndAlliance call, including the 84-day daily expiry, must cross one default-deny prefix");
		Test.True(guard.Contains("RunAuthorizedBreak", StringComparison.Ordinal)
			&& (guard.Contains("using (BeginAuthorizedBreak", StringComparison.Ordinal)
				|| (guard.Contains("try", StringComparison.Ordinal) && guard.Contains("finally", StringComparison.Ordinal))),
			"an explicit alliance break must use a bounded exception-safe authorization scope");
		foreach (string explicitScope in new[]
		{
			"world_diplomacy_break_alliance",
			"diplomacy_break_alliance"
		})
		{
			Test.True(guard.Contains("\"" + explicitScope + "\"", StringComparison.Ordinal),
				"permanent-alliance allowlist is missing exact break scope: " + explicitScope);
		}
		Test.True(guard.Contains("StringComparison.Ordinal", StringComparison.Ordinal)
			|| guard.Contains("StringComparer.Ordinal", StringComparison.Ordinal),
			"authorization scopes must use exact ordinal matching rather than a broad prefix or substring");
		string explicitBreak = ExtractMethod(guard, "internal static void RunAuthorizedBreak(");
		Test.True(explicitBreak.Contains("!ExplicitBreakSources.Contains(source ?? \"\")", StringComparison.Ordinal)
			&& explicitBreak.Contains("using (BeginAuthorizedBreak", StringComparison.Ordinal),
			"unknown source strings must be rejected before an exception-safe break scope is entered");
		Test.True(!explicitBreak.Contains("KingdomVoteWarSource", StringComparison.Ordinal)
			&& !explicitBreak.Contains("kingdom_vote_declared_war", StringComparison.Ordinal),
			"the general AI break entry point must not be able to manufacture a kingdom-vote authorization");
		Test.True(guard.Contains("[HarmonyPatch(typeof(DeclareWarDecision), nameof(DeclareWarDecision.ApplyChosenOutcome))]", StringComparison.Ordinal)
			&& guard.Contains("chosenOutcome is not DeclareWarDecision.DeclareWarDecisionOutcome outcome", StringComparison.Ordinal)
			&& guard.Contains("!outcome.ShouldWarBeDeclared", StringComparison.Ordinal)
			&& guard.Contains("BeginAuthorizedBreak(KingdomVoteWarSource", StringComparison.Ordinal),
			"only a yes outcome being executed by DeclareWarDecision may open the kingdom-vote authorization scope");
		Test.True(guard.Contains("!ReferenceEquals(outcome.Kingdom, decisionKingdom)", StringComparison.Ordinal)
			&& guard.Contains("!ReferenceEquals(outcome.FactionToDeclareWarOn, decisionTarget)", StringComparison.Ordinal),
			"the approved vote outcome must match the decision's exact kingdom pair before authorization");
		Test.True(guard.Contains("private static Exception Finalizer(", StringComparison.Ordinal)
			&& guard.Contains("__state.Dispose();", StringComparison.Ordinal),
			"the approved-vote authorization must be cleared even if ApplyChosenOutcome throws");
		Test.True(declareWarPatch.Contains("[HarmonyPatch(typeof(DeclareWarAction), \"ApplyInternal\")]", StringComparison.Ordinal)
			&& declareWarPatch.Contains("PermanentAllianceGuard.ShouldAllowDeclareWar(", StringComparison.Ordinal),
			"the global private ApplyInternal prefix must enforce the kingdom-decision boundary before war state changes");
		string declareWarBoundary = ExtractMethod(guard, "internal static bool ShouldAllowDeclareWar(");
		Test.True(declareWarBoundary.Contains("CausedByKingdomDecision", StringComparison.Ordinal)
			&& declareWarBoundary.Contains("IsAuthorizedBySource(firstKingdom, secondKingdom, KingdomVoteWarSource)", StringComparison.Ordinal)
			&& !declareWarBoundary.Contains("BeginAuthorizedBreak", StringComparison.Ordinal),
			"ApplyInternal may consume only an already-active exact vote scope and must never create its own authorization");
		Test.True(guard.Contains("private static bool TryPatch(", StringComparison.Ordinal)
			&& guard.Contains("patchedMethods == null || patchedMethods.Count == 0", StringComparison.Ordinal)
			&& guard.Contains("FATAL: EndAlliance guard patch failed", StringComparison.Ordinal)
			&& guard.Contains("if (endAlliancePatched && dailyTickPatched && startAlliancePatched && voteOutcomePatched)", StringComparison.Ordinal),
			"patch registration must report real success and make a missing critical EndAlliance guard unmistakable");
		string endAlliancePrefix = ExtractMethod(
			guard,
			"private static bool Prefix(AllianceCampaignBehavior __instance, Kingdom kingdom1, Kingdom kingdom2)");
		int renewExpiredAlliance = endAlliancePrefix.IndexOf(
			"RefreshAllianceEndTime(__instance, kingdom1, kingdom2)",
			StringComparison.Ordinal);
		int denyUnscopedEnd = endAlliancePrefix.IndexOf("return false;", renewExpiredAlliance, StringComparison.Ordinal);
		Test.True(endAlliancePrefix.Contains("IsAuthorized(kingdom1, kingdom2", StringComparison.Ordinal)
			&& renewExpiredAlliance >= 0 && denyUnscopedEnd > renewExpiredAlliance,
			"an unscoped live alliance expiry must be renewed and denied by the EndAlliance prefix");

		string declareWar = ExtractMethod(diplomacy, "private string TryExecuteDeclareWar(");
		Test.True(!declareWar.Contains("EndAlliance", StringComparison.Ordinal),
			"a direct face-to-face declaration of war must not pre-break a permanent alliance");
		Test.True(declareWar.Contains("IsAllyWithKingdom(declarer, target)", StringComparison.Ordinal)
			&& declareWar.Contains("must explicitly break the alliance", StringComparison.Ordinal),
			"a face-to-face war declaration must be rejected while the kingdoms remain allied");
		string directBreak = ExtractMethod(diplomacy, "private string TryExecuteBreakAlliance(");
		Test.True(directBreak.Contains("PermanentAllianceGuard.RunAuthorizedBreak(", StringComparison.Ordinal)
			&& directBreak.Contains("\"diplomacy_break_alliance\"", StringComparison.Ordinal)
			&& directBreak.Contains("EndAlliance", StringComparison.Ordinal),
			"the direct diplomacy break action must wrap only its EndAlliance call in the exact authorized scope");

		string immediateIntent = ExtractMethod(
			worldDiplomacySource,
			"private void ExecuteImmediateIntent(");
		Test.True(immediateIntent.Contains("PermanentAllianceGuard.RunAuthorizedBreak(", StringComparison.Ordinal)
			&& immediateIntent.Contains("\"world_diplomacy_break_alliance\"", StringComparison.Ordinal)
			&& immediateIntent.Contains("alliance.EndAlliance(author, target)", StringComparison.Ordinal),
			"WorldDiplomacy break_alliance must use its exact explicit authorization scope");
		Test.True(CountOccurrences(worldDiplomacySource, "alliance.EndAlliance(") == 1,
			"WorldDiplomacy must have no unscoped secondary path that can end a permanent alliance");

		Test.True(subModule.Contains("PermanentAllianceGuard.RegisterHarmonyPatches(harmony)", StringComparison.Ordinal)
			&& subModule.Contains("typeof(Patch_Meeting_SuppressDeclareWarAction)", StringComparison.Ordinal),
			"the permanent-alliance guard must be registered during the existing Harmony bootstrap");
	}

    private static void RunPolicyThreatComplianceConsequenceContractTests(string source)
    {
        string customPolicy = File.ReadAllText(
            FindRepositoryFile("CustomPolicyBehavior.cs"),
            Encoding.UTF8);
        string policyContext = File.ReadAllText(
            FindRepositoryFile("WorldDiplomacyPolicyContext.cs"),
            Encoding.UTF8);
        string settings = File.ReadAllText(
            FindRepositoryFile("DuelSettings.cs"),
            Encoding.UTF8);

        string roundDto = ExtractSection(
            source,
            "public sealed class WorldDiplomacyRound",
            "public sealed class WorldDiplomacyLlmMessage");
        Test.True(roundDto.Contains("[JsonProperty(\"attachedPolicySignals\")]", StringComparison.Ordinal)
                  && roundDto.Contains(
                      "public List<WorldDiplomacyPolicySignal> AttachedPolicySignals",
                      StringComparison.Ordinal),
            "a round must persist the exact policy signals that were attached to its event");

        string policySignalDto = ExtractSection(
            source,
            "public sealed class WorldDiplomacyPolicySignal",
            "public sealed class WorldDiplomacyCompressionSummary");
        foreach (string persistedPolicyField in new[]
        {
            "[JsonProperty(\"signalKey\")]",
            "[JsonProperty(\"policyId\")]",
            "[JsonProperty(\"policyKind\")]",
            "[JsonProperty(\"issuerKingdomId\")]",
            "[JsonProperty(\"targetKingdomId\")]"
        })
        {
            Test.True(policySignalDto.Contains(persistedPolicyField, StringComparison.Ordinal),
                "an attached policy signal must persist its binding identity: " + persistedPolicyField);
        }

        string attachPolicySignal = ExtractMethod(source, "private void AttachPolicySignalToRound(");
        Test.True(attachPolicySignal.Contains("round.AttachedPolicySignals ??=", StringComparison.Ordinal)
                  && attachPolicySignal.Contains("string.Equals(item.SignalKey, signal.SignalKey", StringComparison.Ordinal)
                  && attachPolicySignal.Contains("ClonePolicySignal(signal)", StringComparison.Ordinal),
            "round attachment must snapshot each exact signal once by stable signal key");

        string clonePolicySignal = ExtractMethod(source, "private static WorldDiplomacyPolicySignal ClonePolicySignal(");
        Test.True(clonePolicySignal.Contains("PolicyId = signal.PolicyId", StringComparison.Ordinal)
                  && clonePolicySignal.Contains("PolicyKind = string.IsNullOrWhiteSpace(signal.PolicyKind) ? \"kingdom\"", StringComparison.Ordinal)
                  && clonePolicySignal.Contains("IssuerKingdomId = signal.IssuerKingdomId", StringComparison.Ordinal)
                  && clonePolicySignal.Contains("TargetKingdomId = signal.TargetKingdomId", StringComparison.Ordinal),
            "the round snapshot must retain policy kind, owner, and affected kingdom without parsing the signal key");

        string storageNormalization = ExtractSection(
            source,
            "private void NormalizeStorage(bool allowWorldValidation = false)",
            "private void TrimRecentBattleFacts()");
        Test.True(storageNormalization.Contains("round.AttachedPolicySignals ??=", StringComparison.Ordinal)
                  && storageNormalization.Contains("round.AttachedPolicySignals = round.AttachedPolicySignals", StringComparison.Ordinal)
                  && storageNormalization.Contains(".GroupBy(x => x.SignalKey.Trim(), StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal)
                  && storageNormalization.Contains(".Select(group => ClonePolicySignal(", StringComparison.Ordinal),
            "save normalization must restore, validate, deduplicate, and clone attached policy signals");

        string resolvePolicyCondition = ExtractMethod(source, "private bool TryResolvePolicyConditionForThreat(");
        Test.True(resolvePolicyCondition.Contains("round?.AttachedPolicySignals", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("string.IsNullOrWhiteSpace(signal.PolicyKind)", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("signal.PolicyKind.Trim(), \"kingdom\"", StringComparison.Ordinal),
            "only a blank legacy kind or an explicit kingdom policy may become a threat cancellation condition");
        Test.True(resolvePolicyCondition.Contains("WorldDiplomacyPolicyContext.IsForeignPolicySignalActive(", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("signal.PolicyId", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("signal.IssuerKingdomId", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("signal.TargetKingdomId", StringComparison.Ordinal),
            "a policy condition must still be active under its exact policy, owner, and affected kingdom IDs");
        string activePolicySignal = ExtractMethod(
            policyContext,
            "public static bool IsForeignPolicySignalActive(");
        Test.True(activePolicySignal.Contains("record.AgendaStatus.Trim(), \"active\"", StringComparison.Ordinal)
                  && activePolicySignal.Contains("record.AgendaStatus.Trim(), \"expiry_vote_pending\"", StringComparison.Ordinal)
                  && activePolicySignal.Contains("record.Effects", StringComparison.Ordinal),
            "a cancellable threat condition must require both a live policy agenda and its exact active foreign effect");
        Test.True(resolvePolicyCondition.Contains("policyOwnerRepresentative.StringId, threatTarget.StringId", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("affectedRepresentative.StringId, threatIssuer.StringId", StringComparison.Ordinal),
            "the threatened kingdom must own the policy and the threatening kingdom must be its affected party");
        Test.True(resolvePolicyCondition.Contains(".GroupBy(x => (x.PolicyId ?? \"\").Trim()", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains(".Take(2)", StringComparison.Ordinal)
                  && resolvePolicyCondition.Contains("if (matches.Count != 1) return false;", StringComparison.Ordinal),
            "zero or ambiguous active kingdom-policy matches must remain unbound; exactly one may bind");

        string initializePolicyCondition = ExtractMethod(
            source,
            "private static void InitializeDiplomaticThreatPolicyCondition(");
        Test.True(initializePolicyCondition.Contains("PolicyConditionPolicyId = (signal.PolicyId", StringComparison.Ordinal)
                  && initializePolicyCondition.Contains("PolicyConditionOwnerKingdomId = (signal.IssuerKingdomId", StringComparison.Ordinal)
                  && initializePolicyCondition.Contains("PolicyConditionAffectedKingdomId = (signal.TargetKingdomId", StringComparison.Ordinal)
                  && initializePolicyCondition.Contains("PolicyConditionCancellationStatus = \"pending\"", StringComparison.Ordinal),
            "a newly bound threat must persist the exact condition and start one pending cancellation");

        string threatRegistration = ExtractMethod(source, "private bool RegisterOrAdvanceDiplomaticThreat(");
        Test.True(CountOccurrences(threatRegistration, "InitializeDiplomaticThreatPolicyCondition(") == 2
                  && threatRegistration.Contains("InitializeDiplomaticThreatPolicyCondition(warning, policyCondition, day)", StringComparison.Ordinal)
                  && threatRegistration.Contains("InitializeDiplomaticThreatPolicyCondition(ultimatum, directPolicyCondition, day)", StringComparison.Ordinal),
            "only a new warning or direct ultimatum may initialize a policy condition");
        Test.True(threatRegistration.Contains("existing.Stage = \"ultimatum\";", StringComparison.Ordinal)
                  && !threatRegistration.Contains("existing.PolicyCondition", StringComparison.Ordinal),
            "warning-to-ultimatum escalation must preserve the original threat's policy condition instead of replacing it");

        string compliance = ExtractMethod(source, "private bool ResolveDiplomaticThreatCompliance(");
        Test.True(compliance.Contains("TryApplyDiplomaticThreatPolicyConditionCancellation(threat)", StringComparison.Ordinal)
                  && compliance.Contains("TryApplyDiplomaticThreatIssuerRelationReward(threat, issuer", StringComparison.Ordinal),
            "an exact comply_ultimatum settlement must execute both the bound-policy cancellation and issuer reward");

        string policyCancellation = ExtractMethod(
            source,
            "private bool TryApplyDiplomaticThreatPolicyConditionCancellation(");
        Test.True(policyCancellation.Contains("if (threat.PolicyConditionCancellationCompleted) return true;", StringComparison.Ordinal)
                  && policyCancellation.Contains("CustomPolicyBehavior.TryCancelActiveKingdomPolicyForExternal(", StringComparison.Ordinal)
                  && policyCancellation.Contains("threat.PolicyConditionPolicyId", StringComparison.Ordinal)
                  && policyCancellation.Contains("threat.PolicyConditionOwnerKingdomId", StringComparison.Ordinal)
                  && policyCancellation.Contains("RemoveSettledPolicySignalContextFromActiveRound(", StringComparison.Ordinal)
                  && policyCancellation.Contains("InvalidateOtherThreatsBoundToSettledPolicy(threat)", StringComparison.Ordinal),
            "policy cancellation must be idempotent and call the CustomPolicy bridge with the persisted exact IDs");

        string settledPolicyContextCleanup = ExtractMethod(
            source,
            "private void RemoveSettledPolicySignalContextFromActiveRound(");
        Test.True(settledPolicyContextCleanup.Contains("round.AttachedPolicySignals", StringComparison.Ordinal)
                  && settledPolicyContextCleanup.Contains("signal.PolicyId, policyId", StringComparison.Ordinal)
                  && settledPolicyContextCleanup.Contains("signal.IssuerKingdomId, ownerKingdomId", StringComparison.Ordinal)
                  && settledPolicyContextCleanup.Contains("BuildPolicySignalContext(signal)", StringComparison.Ordinal)
                  && settledPolicyContextCleanup.Contains("round.ExternalOpeningContext = openingContext.Trim()", StringComparison.Ordinal)
                  && settledPolicyContextCleanup.Contains("round.ExternalSignalKeys.RemoveAll", StringComparison.Ordinal),
            "successful cancellation must remove the exact policy's stale active-round prompt context so queued jobs rebuild from current facts");

        string sharedPolicyInvalidation = ExtractMethod(
            source,
            "private void InvalidateOtherThreatsBoundToSettledPolicy(");
        Test.True(sharedPolicyInvalidation.Contains("IsOpenDiplomaticThreat(x)", StringComparison.Ordinal)
                  && sharedPolicyInvalidation.Contains("x.PolicyConditionPolicyId", StringComparison.Ordinal)
                  && sharedPolicyInvalidation.Contains("x.PolicyConditionOwnerKingdomId", StringComparison.Ordinal)
                  && sharedPolicyInvalidation.Contains("other.Status = \"invalidated\";", StringComparison.Ordinal)
                  && sharedPolicyInvalidation.Contains("other.ObligationRoundId = \"\";", StringComparison.Ordinal)
                  && sharedPolicyInvalidation.Contains("other.ResolutionDocumentId = \"\";", StringComparison.Ordinal)
                  && sharedPolicyInvalidation.Contains("other.HistoryResultRecorded = true;", StringComparison.Ordinal)
                  && !sharedPolicyInvalidation.Contains("IssuerReward", StringComparison.Ordinal),
            "one successful policy cancellation must close other threats bound to the same policy without transferring the compliance reward or resolution document");

        string externalPolicyCancellation = ExtractMethod(
            customPolicy,
            "private bool TryCancelActiveKingdomPolicyInternal(");
        Test.True(externalPolicyCancellation.Contains("candidate.RecordId ?? \"\", normalizedPolicyId", StringComparison.Ordinal)
                  && externalPolicyCancellation.Contains("candidate.OwnerKingdomId ?? \"\", normalizedOwnerKingdomId", StringComparison.Ordinal)
                  && externalPolicyCancellation.Contains("CompleteDynamicPolicyAbolition(matched, policy, cancellationReason)", StringComparison.Ordinal),
            "the CustomPolicy bridge must match RecordId plus owner and reuse the complete abolition lifecycle");
        string completePolicyAbolition = ExtractMethod(
            customPolicy,
            "private void CompleteDynamicPolicyAbolition(");
        int lifecycleSettlement = completePolicyAbolition.IndexOf("TryHandleConditionalPolicyAbolition(", StringComparison.Ordinal);
        int externalStatusSettlement = completePolicyAbolition.IndexOf("UpdatePolicyAgendaStatusForExternal(", StringComparison.Ordinal);
        int artifactSettlement = completePolicyAbolition.IndexOf("RecordPolicySnapshotForRecordId(", StringComparison.Ordinal);
        int terminalRegistryCommit = completePolicyAbolition.IndexOf("StoreDynamicPolicy(data);", StringComparison.Ordinal);
        Test.True(lifecycleSettlement >= 0
                  && externalStatusSettlement > lifecycleSettlement
                  && artifactSettlement > externalStatusSettlement
                  && terminalRegistryCommit > artifactSettlement,
            "dynamic abolition must commit its terminal registry state only after lifecycle, external status, and artifact side effects succeed");

        string fixedDeclarationContract = ExtractMethod(
            source,
            "private static string BuildDiplomaticDeclarationModeContract()");
        string policyOpeningContext = ExtractMethod(source, "private static string BuildPolicySignalContext(");
        string threatDynamicContext = ExtractMethod(source, "private void AppendDiplomaticThreatDynamicContext(");
        Test.True(!fixedDeclarationContract.Contains("将由机制取消", StringComparison.Ordinal)
                  && !policyOpeningContext.Contains("将由机制取消", StringComparison.Ordinal)
                  && !policyOpeningContext.Contains("comply_ultimatum", StringComparison.Ordinal),
            "pre-issuance contracts and policy-event context must not disclose a future threat consequence");
        Test.True(threatDynamicContext.Contains("incoming.PolicyConditionPolicyId", StringComparison.Ordinal)
                  && threatDynamicContext.Contains("若本国选择comply_ultimatum", StringComparison.Ordinal)
                  && threatDynamicContext.Contains("将由机制取消", StringComparison.Ordinal),
            "only the post-issuance incoming-threat context may explain the already-bound policy consequence");
        foreach (string forbiddenRewardDisclosure in new[]
        {
            "IssuerReward",
            "WorldDiplomacyThreatComplianceIssuerRelationReward",
            "关系增加",
            "国内关系奖励"
        })
        {
            Test.True(!threatDynamicContext.Contains(forbiddenRewardDisclosure, StringComparison.OrdinalIgnoreCase),
                "post-issuance LLM context must not disclose the issuer's backend reward: " + forbiddenRewardDisclosure);
        }

        Test.True(settings.Contains(
                "public const int WorldDiplomacyThreatComplianceIssuerRelationRewardMin = 0;",
                StringComparison.Ordinal)
                  && settings.Contains(
                      "public const int DefaultWorldDiplomacyThreatComplianceIssuerRelationReward = 10;",
                      StringComparison.Ordinal),
            "issuer relation reward must support MCM zero-disable and default to 10");
        string rewardGetter = ExtractMethod(source, "private static int GetThreatComplianceIssuerRelationReward()");
        Test.True(rewardGetter.Contains("WorldDiplomacyThreatComplianceIssuerRelationRewardMin", StringComparison.Ordinal)
                  && rewardGetter.Contains("WorldDiplomacyThreatComplianceIssuerRelationRewardMax", StringComparison.Ordinal)
                  && rewardGetter.Contains("DefaultWorldDiplomacyThreatComplianceIssuerRelationReward", StringComparison.Ordinal),
            "the live reward getter must clamp the MCM value and retain the safe default");

        string issuerReward = ExtractMethod(source, "private bool TryApplyDiplomaticThreatIssuerRelationReward(");
        foreach (string snapshotBoundary in new[]
        {
            "if (threat.IssuerRewardCompleted)",
            "if (!threat.IssuerRewardSnapshotCaptured)",
            "clan == currentRulingClan",
            "clan.Kingdom != issuerKingdom",
            "clan.IsEliminated",
            "clan.IsUnderMercenaryService",
            "clan.IsClanTypeMercenary",
            "IssuerRewardEligibleClanIds",
            "IssuerRewardAppliedClanIds",
            "IssuerRewardSkippedClanIds"
        })
        {
            Test.True(issuerReward.Contains(snapshotBoundary, StringComparison.Ordinal),
                "issuer reward snapshot/idempotency boundary is missing: " + snapshotBoundary);
        }
        Test.True(issuerReward.Contains("if (rewardAmount <= 0)", StringComparison.Ordinal)
                  && issuerReward.Contains("threat.IssuerRewardCompleted = true;", StringComparison.Ordinal),
            "MCM zero must settle the reward as a no-op without retrying or changing relations");
        Test.True(issuerReward.Contains("if (appliedIds.Contains(eligibleClanId) || skippedIds.Contains(eligibleClanId))", StringComparison.Ordinal)
                  && issuerReward.Contains("ChangeRelationAction.ApplyRelationChangeBetweenHeroes(", StringComparison.Ordinal)
                  && issuerReward.Contains("appliedIds.Add(eligibleClanId)", StringComparison.Ordinal)
                  && issuerReward.Contains("eligibleIds.All(id => appliedIds.Contains(id) || skippedIds.Contains(id))", StringComparison.Ordinal),
            "each snapshotted formal vassal clan must receive the relation reward at most once");

        string threatDto = ExtractSection(
            source,
            "public sealed class WorldDiplomacyThreat",
            "public sealed class WorldDiplomacyRoundParticipant");
        foreach (string persistedSettlementField in new[]
        {
            "policyConditionPolicyId",
            "policyConditionOwnerKingdomId",
            "policyConditionCancellationCompleted",
            "issuerRewardRulingClanId",
            "issuerRewardEligibleClanIds",
            "issuerRewardAppliedClanIds",
            "issuerRewardSkippedClanIds",
            "issuerRewardSnapshotCaptured",
            "issuerRewardCompleted",
            "issuerRewardAmount"
        })
        {
            Test.True(threatDto.Contains("[JsonProperty(\"" + persistedSettlementField + "\")]", StringComparison.Ordinal),
                "threat settlement idempotency state must survive save/load: " + persistedSettlementField);
        }

        string dailyTick = ExtractMethod(source, "private void OnDailyTick()");
        string retryDomesticPenalty = ExtractMethod(source, "private void RetryDiplomaticThreatDomesticPenalties()");
        string retryComplianceConsequences = ExtractMethod(source, "private void RetryDiplomaticThreatComplianceConsequences()");
        Test.True(dailyTick.Contains("RetryDiplomaticThreatDomesticPenalties();", StringComparison.Ordinal)
                  && dailyTick.Contains("RetryDiplomaticThreatComplianceConsequences();", StringComparison.Ordinal),
            "incomplete compliance consequences must retry from the daily bounded maintenance path");
        Test.True(CountOccurrences(retryDomesticPenalty, ".Take(8)") == 1
                  && CountOccurrences(retryComplianceConsequences, ".Take(8)") == 2,
            "each daily target-penalty, policy-cancellation, and issuer-reward batch must remain capped at eight threats");
        Test.True(CountOccurrences(retryComplianceConsequences, "threat.UpdatedDay = CurrentDay();") == 2,
            "failed bounded retries must rotate by attempt day instead of letting poisoned records starve newer consequences");

        Test.True(source.Contains(
                "private const int DiplomaticThreatStateSchemaVersion = 3;",
                StringComparison.Ordinal),
            "policy and issuer-reward settlement fields require diplomatic-threat schema version 3");
        string v3Migration = ExtractMethod(
            source,
            "private void MigrateDiplomaticThreatComplianceConsequencesV3()");
        int openThreatExit = v3Migration.IndexOf(
            "string.Equals((threat.Status ?? \"\").Trim(), \"open\"",
            StringComparison.Ordinal);
        int terminalRewardSuppression = v3Migration.IndexOf(
            "threat.IssuerRewardAmount = 0;",
            StringComparison.Ordinal);
        Test.True(openThreatExit >= 0 && terminalRewardSuppression > openThreatExit
                  && v3Migration.Contains("threat.IssuerRewardSnapshotCaptured = true;", StringComparison.Ordinal)
                  && v3Migration.Contains("threat.IssuerRewardCompleted = true;", StringComparison.Ordinal)
                  && v3Migration.Contains("threat.IssuerRewardHistoryRecorded = true;", StringComparison.Ordinal),
            "schema v3 migration must leave open threats eligible for future settlement but suppress retroactive rewards on old terminal threats");
        string threatNormalization = ExtractMethod(source, "private static void NormalizeDiplomaticThreatRecord(");
        Test.True(threatNormalization.Contains("NormalizeDiplomaticThreatIdList(threat.IssuerRewardEligibleClanIds)", StringComparison.Ordinal)
                  && threatNormalization.Contains("NormalizeDiplomaticThreatIdList(threat.IssuerRewardAppliedClanIds)", StringComparison.Ordinal)
                  && threatNormalization.Contains("NormalizeDiplomaticThreatIdList(threat.IssuerRewardSkippedClanIds)", StringComparison.Ordinal)
                  && threatNormalization.Contains("PolicyConditionCancellationStatus = NormalizeToken", StringComparison.Ordinal),
            "schema v3 threat records must normalize policy state and all issuer-reward idempotency lists");
    }

	private static void RunThreatStateRuleTests()
    {
        const string StageDocumentId = "diplomacy_document:threat-stage-1";

        // `statement` is deliberately retained only as a legacy published-document
        // fixture here. New DECLARE/ANALYZE contracts and publication guards must not
        // create it, but an old save containing one must still settle threat state.

        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, true, "comply_ultimatum", StageDocumentId, true),
            WorldDiplomacyThreatStateRuleResult.MarkTargetComplied,
            "an exact first-declaration compliance must comply");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, true, "statement", "", false),
            WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied,
            "any other first published intent must be noncompliance");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, true, "comply_ultimatum", "diplomacy_document:other", true),
            WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied,
            "compliance with the wrong source must not comply");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, true, "comply_ultimatum", StageDocumentId, false),
            WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied,
            "compliance addressed to another kingdom must not comply");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "noncomplied", StageDocumentId, false, "comply_ultimatum", StageDocumentId, true),
            WorldDiplomacyThreatStateRuleResult.RejectLateCompliance,
            "compliance after a noncompliance decision must be rejected");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "complied", StageDocumentId, false, "comply_ultimatum", StageDocumentId, true),
            WorldDiplomacyThreatStateRuleResult.RejectLateCompliance,
            "a duplicate compliance after the decision must be rejected");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, false, "statement", "", false),
            WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "an unseen current target stage must rebuild before publication");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, false, "comply_ultimatum", StageDocumentId, true),
            WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "even an apparently exact compliance must rebuild when its stage snapshot is stale");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", "", true, "statement", "", false),
            WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "a missing current target stage id must rebuild before publication");

        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, true, "ultimatum", true, false),
            WorldDiplomacyThreatStateRuleResult.MarkFollowThroughSatisfied,
            "a rejected warning must be followed by an ultimatum to the same target");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, true, "declare_war", true, true),
            WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached,
            "declaring war directly must not replace the promised warning-to-ultimatum step");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, true, "ultimatum", false, false),
            WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached,
            "an ultimatum to another kingdom must breach the warning obligation");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, true, "declare_war", true, true),
            WorldDiplomacyThreatStateRuleResult.MarkFollowThroughSatisfied,
            "a rejected ultimatum requires a successful war declaration");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, true, "declare_war", true, false),
            WorldDiplomacyThreatStateRuleResult.DeferFollowThroughForTechnicalFailure,
            "the correct war intent with a failed mechanic must defer instead of breach");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, true, "statement", true, false),
            WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached,
            "a non-war declaration must breach an ultimatum obligation");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, false, "statement", true, false),
            WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "an unseen current follow-through stage must rebuild rather than breach or bypass");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, false, "declare_war", true, true),
            WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "even a successful-looking war declaration must rebuild when its stage snapshot is stale");
        AssertStateRule(
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "unknown", StageDocumentId, true, "statement", true, false),
            WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "an unknown stage must rebuild rather than publish around the obligation");

        WorldDiplomacyThreatStateRuleResult normalTargetStatement =
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, true, "statement", "", false);
        WorldDiplomacyThreatStateRuleResult fallbackTargetStatement =
            WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
                "pending", StageDocumentId, true, "statement", "", false);
        Test.True(normalTargetStatement == fallbackTargetStatement
            && fallbackTargetStatement == WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied,
            "a published fallback target statement must settle exactly like a normal statement");

        WorldDiplomacyThreatStateRuleResult normalIssuerStatement =
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, true, "statement", true, false);
        WorldDiplomacyThreatStateRuleResult fallbackIssuerStatement =
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, true, "statement", true, false);
        Test.True(normalIssuerStatement == fallbackIssuerStatement
            && fallbackIssuerStatement == WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached,
            "a published fallback issuer statement must settle exactly like a normal statement");

        WorldDiplomacyThreatStateRuleResult normalMechanicalFailure =
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, true, "declare_war", true, false);
        WorldDiplomacyThreatStateRuleResult fallbackMechanicalFailure =
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "ultimatum", StageDocumentId, true, "declare_war", true, false);
        Test.True(normalMechanicalFailure == fallbackMechanicalFailure
            && fallbackMechanicalFailure == WorldDiplomacyThreatStateRuleResult.DeferFollowThroughForTechnicalFailure,
            "a fallback war action with a mechanical failure must defer exactly like a normal declaration");

        WorldDiplomacyThreatStateRuleResult normalStaleFollowThrough =
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, false, "ultimatum", true, false);
        WorldDiplomacyThreatStateRuleResult fallbackStaleFollowThrough =
            WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
                "noncomplied", "warning", StageDocumentId, false, "ultimatum", true, false);
        Test.True(normalStaleFollowThrough == fallbackStaleFollowThrough
            && fallbackStaleFollowThrough == WorldDiplomacyThreatStateRuleResult.RebuildStaleStageSnapshot,
            "a stale fallback must rebuild exactly like a stale normal declaration");
    }

    private static void AssertStateRule(
        WorldDiplomacyThreatStateRuleResult actual,
        WorldDiplomacyThreatStateRuleResult expected,
        string message)
    {
        Test.True(actual == expected, $"{message}; expected={expected}, actual={actual}");
    }

    private static string FindRepositoryFile(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file.", fileName);
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Test.True(start >= 0, "missing start marker: " + startMarker);
        int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Test.True(end > start, "missing end marker: " + endMarker);
        return source.Substring(start, end - start);
    }

	private static string ExtractMethod(string source, string marker)
	{
		int start = source.IndexOf(marker, StringComparison.Ordinal);
		Test.True(start >= 0, "missing method marker: " + marker);
		int openBrace = source.IndexOf('{', start + marker.Length);
		Test.True(openBrace > start, "missing method body: " + marker);
		int depth = 0;
		for (int index = openBrace; index < source.Length; index++)
		{
			char value = source[index];
			if (value == '{') depth++;
			else if (value == '}' && --depth == 0) return source.Substring(start, index - start + 1);
		}
		throw new InvalidOperationException("unterminated method body: " + marker);
	}

    private static HashSet<string> ExtractIntentEnum(string contractSource)
    {
        const string marker = "\\\"intent\\\":\\\"";
        int start = contractSource.IndexOf(marker, StringComparison.Ordinal);
        Test.True(start >= 0, "fixed mode contract is missing its JSON intent enum");
        start += marker.Length;
        int end = contractSource.IndexOf("\\\"", start, StringComparison.Ordinal);
        Test.True(end > start, "fixed mode contract has an unterminated JSON intent enum");
        return contractSource.Substring(start, end - start)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static int CountOccurrences(string source, string value)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value)) return 0;
        int count = 0;
        int cursor = 0;
        while ((cursor = source.IndexOf(value, cursor, StringComparison.Ordinal)) >= 0)
        {
            count++;
            cursor += value.Length;
        }
        return count;
    }
}

internal sealed class CooldownLoadFixture
{
    public string ProposerKingdomId { get; set; } = "";
    public string TargetKingdomId { get; set; } = "";
    public string Domain { get; set; } = "";
    public int FailedRoundDay { get; set; }
    public string SourceRoundId { get; set; } = "";
}

internal sealed class MultiActionLoadFixture
{
    public string DocumentId { get; set; } = "";
    public List<MultiActionLoadEntry>? Actions { get; set; }
}

internal sealed class MultiActionLoadEntry
{
    public string ActionId { get; set; } = "";
    public string TargetKingdomId { get; set; } = "";
    public string Intent { get; set; } = "";
    public string RespondingToOfferActionId { get; set; } = "";
    public string RespondingToThreatActionId { get; set; } = "";
    public bool ChangedDiplomaticState { get; set; }
    public string MechanicalResult { get; set; } = "";
}
