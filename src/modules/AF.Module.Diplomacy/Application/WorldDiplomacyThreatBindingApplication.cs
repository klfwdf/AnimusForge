using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyThreatBindingPort
{
    WorldDiplomacyRound ResolveRound(string id);
    bool IsPolicyActive(string policyId, string ownerId, string affectedId);
    string RepresentativeId(string kingdomId);
    int CurrentDay();
    string NewId(string kind);
    int EscalationPrestigeReward { get; }
    int WarPrestigeReward { get; }
    void Log(string message);
}

internal static class WorldDiplomacyThreatBindingApplication
{
    internal static bool TryResolvePolicyConditionForThreat(WorldDiplomacyDocument document, string issuerId, string targetId,
        IWorldDiplomacyThreatBindingPort port, out WorldDiplomacyPolicySignal selected)
    {
        selected = null;
        if (document == null || issuerId == null || targetId == null || issuerId == targetId) return false;
        WorldDiplomacyRound round = port.ResolveRound(document.RoundId);
        var matches = new List<WorldDiplomacyPolicySignal>();
        foreach (WorldDiplomacyPolicySignal signal in round?.AttachedPolicySignals ?? new List<WorldDiplomacyPolicySignal>())
        {
            if (!WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(signal)
                || !port.IsPolicyActive(signal.PolicyId, signal.IssuerKingdomId, signal.TargetKingdomId)) continue;
            string policyOwnerRepresentative = port.RepresentativeId(signal.IssuerKingdomId);
            string affectedRepresentative = port.RepresentativeId(signal.TargetKingdomId);
            if (!WorldDiplomacyRoundLifecycleRules.IsThreatPolicyPartyMatch(policyOwnerRepresentative, affectedRepresentative, targetId, issuerId)) continue;
            matches.Add(signal);
        }
        selected = WorldDiplomacyRoundLifecycleRules.SelectUniquePolicySignal(matches);
        return selected != null;
    }

    internal static bool Register(WorldDiplomacyStorage storage, WorldDiplomacyDocument document, string issuerId,
        string targetId, string stage, IWorldDiplomacyThreatBindingPort port,
        IWorldDiplomacyOrchestration orchestration)
    {
        if (document == null || issuerId == null || targetId == null || issuerId == targetId) return false;
        return RegisterOrAdvanceDiplomaticThreat(document, issuerId, targetId, stage,
            () => storage.DiplomaticThreats ??= new List<WorldDiplomacyThreat>(), port.CurrentDay(), port.EscalationPrestigeReward,
            port.NewId, () =>
            {
                TryResolvePolicyConditionForThreat(document, issuerId, targetId, port, out WorldDiplomacyPolicySignal selected);
                return selected;
            }, (id, delta, doc, r) => orchestration.ApplyNationalPrestigeDelta(id, delta, doc, r), port.Log);
    }

    internal static void Process(WorldDiplomacyStorage storage, WorldDiplomacyDocument document, string authorId, string targetId,
        bool recordTargetDecisions, IWorldDiplomacyThreatBindingPort port, IWorldDiplomacyOrchestration orchestration)
    {
        ProcessDiplomaticThreatDocument(document, authorId, targetId, recordTargetDecisions,
            storage?.DiplomaticThreats, port.CurrentDay(), port.WarPrestigeReward,
            (d, a, t, intent) => WorldDiplomacyThreatApplication.RecordTargetDecision(storage, d, a, t, intent, port.CurrentDay, port.Log),
            (d, a, t, intent) => Register(storage, d, a, t, intent, port, orchestration),
            (d, a, t) => orchestration.ResolveDiplomaticThreatCompliance(d, a, t), (id, delta, doc, r) => orchestration.ApplyNationalPrestigeDelta(id, delta, doc, r));
    }

    public static bool RegisterOrAdvanceDiplomaticThreat(
        WorldDiplomacyDocument document,
        string issuerKingdomId,
        string targetKingdomId,
        string stage,
        Func<List<WorldDiplomacyThreat>> resolveThreats,
        int currentDay,
        int escalationPrestigeReward,
        Func<string, string> newId,
        Func<WorldDiplomacyPolicySignal> resolvePolicyCondition,
        Action<string, int, WorldDiplomacyDocument, string> applyNationalPrestigeDelta,
        Action<string> log)
    {
        if (document == null || string.IsNullOrWhiteSpace(issuerKingdomId)
            || string.IsNullOrWhiteSpace(targetKingdomId)
            || string.Equals(issuerKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        string normalizedStage = WorldDiplomacyIntentVocabulary.NormalizeIntent(stage);
        if (!IsThreatRegistrationIdentityMatch(
            normalizedStage, WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent),
            document.AuthorKingdomId, document.TargetKingdomId,
            issuerKingdomId, targetKingdomId))
        {
            log?.Invoke("diplomatic threat registration blocked by structured identity mismatch document="
                + document.DocumentId + " stage=" + normalizedStage);
            return false;
        }
        List<WorldDiplomacyThreat> threats = resolveThreats?.Invoke();
        WorldDiplomacyThreat existing = SelectOpenThreatIssuedBy(threats, issuerKingdomId);
        int day = currentDay;
        WorldDiplomacyThreatRegistrationDecision registration = EvaluateThreatRegistration(
            existing, normalizedStage, document.DocumentId, targetKingdomId);
        if (registration == WorldDiplomacyThreatRegistrationDecision.AlreadyRegistered)
        {
            return true;
        }
        if (registration == WorldDiplomacyThreatRegistrationDecision.CreateWarning)
        {
            WorldDiplomacyPolicySignal policyCondition = resolvePolicyCondition?.Invoke();
            WorldDiplomacyThreat warning = new WorldDiplomacyThreat
            {
                ThreatId = newId?.Invoke("diplomacy_threat") ?? "",
                IssuerKingdomId = issuerKingdomId ?? "",
                TargetKingdomId = targetKingdomId ?? "",
                Stage = "warning",
                Status = "open",
                TargetDecision = "pending",
                WarningDocumentId = document.DocumentId ?? "",
                WarningActionId = document.ProcessingActionId ?? "",
                StageDocumentId = document.DocumentId ?? "",
                StageActionId = document.ProcessingActionId ?? "",
                StageRoundId = document.RoundId ?? "",
                CreatedDay = day,
                StageIssuedDay = day,
                UpdatedDay = day
            };
            InitializeThreatPolicyCondition(warning, policyCondition, day);
            threats?.Add(warning);
            log?.Invoke("diplomatic warning obligation opened issuer=" + issuerKingdomId
                + " target=" + targetKingdomId + " document=" + document.DocumentId
                + " policy=" + warning.PolicyConditionPolicyId);
            return true;
        }

        if (registration == WorldDiplomacyThreatRegistrationDecision.EscalateToUltimatum)
        {
            existing.Stage = "ultimatum";
            existing.UltimatumDocumentId = document.DocumentId ?? "";
            existing.UltimatumActionId = document.ProcessingActionId ?? "";
            existing.StageDocumentId = document.DocumentId ?? "";
            existing.StageActionId = document.ProcessingActionId ?? "";
            existing.StageRoundId = document.RoundId ?? "";
            existing.StageIssuedDay = day;
            existing.UpdatedDay = day;
            existing.TargetDecision = "pending";
            existing.TargetDecisionDocumentId = "";
            existing.TargetDecisionActionId = "";
            existing.TargetDecisionRoundId = "";
            existing.TargetDecisionDay = 0;
            existing.NonComplianceHistoryRecorded = false;
            existing.ObligationRoundId = "";
            existing.ObligationClaimedDay = 0;
            existing.ResolutionReason = "";
            applyNationalPrestigeDelta?.Invoke(issuerKingdomId, escalationPrestigeReward, document,
                "在谴责遭拒后按承诺升级为最后通牒");
            log?.Invoke("diplomatic warning escalated to ultimatum threat=" + existing.ThreatId
                + " issuer=" + issuerKingdomId + " target=" + targetKingdomId);
            return true;
        }

        if (registration != WorldDiplomacyThreatRegistrationDecision.CreateUltimatum) return false;
        WorldDiplomacyPolicySignal directPolicyCondition = resolvePolicyCondition?.Invoke();
        WorldDiplomacyThreat ultimatum = new WorldDiplomacyThreat
        {
            ThreatId = newId?.Invoke("diplomacy_threat") ?? "",
            IssuerKingdomId = issuerKingdomId ?? "",
            TargetKingdomId = targetKingdomId ?? "",
            Stage = "ultimatum",
            Status = "open",
            TargetDecision = "pending",
            UltimatumDocumentId = document.DocumentId ?? "",
            UltimatumActionId = document.ProcessingActionId ?? "",
            StageDocumentId = document.DocumentId ?? "",
            StageActionId = document.ProcessingActionId ?? "",
            StageRoundId = document.RoundId ?? "",
            CreatedDay = day,
            StageIssuedDay = day,
            UpdatedDay = day
        };
        InitializeThreatPolicyCondition(ultimatum, directPolicyCondition, day);
        threats?.Add(ultimatum);
        log?.Invoke("direct diplomatic ultimatum obligation opened issuer=" + issuerKingdomId
            + " target=" + targetKingdomId + " document=" + document.DocumentId
            + " policy=" + ultimatum.PolicyConditionPolicyId);
        return true;
    }

    public static void ProcessDiplomaticThreatDocument(
        WorldDiplomacyDocument document,
        string authorKingdomId,
        string targetKingdomId,
        bool recordTargetDecisions,
        List<WorldDiplomacyThreat> threats,
        int currentDay,
        int ultimatumWarPrestigeReward,
        Action<WorldDiplomacyDocument, string, string, string> recordThreatDecisions,
        Action<WorldDiplomacyDocument, string, string, string> registerOrAdvanceThreat,
        Action<WorldDiplomacyDocument, string, string> resolveThreatCompliance,
        Action<string, int, WorldDiplomacyDocument, string> applyNationalPrestigeDelta)
    {
        if (document == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
        if (recordTargetDecisions) recordThreatDecisions?.Invoke(document, authorKingdomId, targetKingdomId, intent);
        WorldDiplomacyThreatDocumentDispatch dispatch = EvaluateThreatDocumentDispatch(
            intent, !string.IsNullOrWhiteSpace(targetKingdomId), string.Equals(authorKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase), document.ChangedDiplomaticState);
        if (dispatch == WorldDiplomacyThreatDocumentDispatch.RegisterOrAdvance)
        {
            registerOrAdvanceThreat?.Invoke(document, authorKingdomId, targetKingdomId, intent);
            return;
        }
        if (dispatch == WorldDiplomacyThreatDocumentDispatch.ResolveCompliance)
        {
            resolveThreatCompliance?.Invoke(document, authorKingdomId, targetKingdomId);
            return;
        }
        if (dispatch != WorldDiplomacyThreatDocumentDispatch.ProcessWarEnforcement) return;

        WorldDiplomacyThreat enforced = SelectEnforceableUltimatumThreat(
            threats, authorKingdomId, targetKingdomId);
        if (enforced != null)
        {
            applyNationalPrestigeDelta?.Invoke(authorKingdomId, ultimatumWarPrestigeReward, document,
                "在对方拒绝最后通牒后兑现宣战承诺");
            enforced.Status = "enforced";
            enforced.ResolutionRoundId = document.RoundId ?? "";
            enforced.ResolutionDocumentId = document.DocumentId ?? "";
            enforced.ResolutionActionId = document.ProcessingActionId ?? "";
            enforced.ResolutionReason = "issuer_declared_war";
            enforced.UpdatedDay = currentDay;
            enforced.ObligationRoundId = "";
            enforced.ObligationClaimedDay = 0;
        }
        foreach (WorldDiplomacyThreat other in (threats ?? new List<WorldDiplomacyThreat>()).Where(x => IsOpenDiplomaticThreatStatus(x?.Status)
            && !ReferenceEquals(x, enforced)
            && IsThreatBetweenParties(x, authorKingdomId, targetKingdomId)))
        {
            if (ShouldKeepWarningOpenAfterWar(other, authorKingdomId, targetKingdomId))
            {
                // A rejected warning strictly requires an ultimatum in the issuer's next
                // declaration. Starting the war directly does not erase that broken promise;
                // leave this threat open for the declaration-level reputation settlement below.
                continue;
            }
            InvalidateThreatForNormalization(other, "war_started_by_other_direction", currentDay);
        }
    }
}
