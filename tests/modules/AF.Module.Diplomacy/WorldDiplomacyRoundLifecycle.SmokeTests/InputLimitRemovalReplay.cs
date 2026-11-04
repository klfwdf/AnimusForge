using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    internal static void InputLimitRemoval()
    {
        foreach (string language in new[] { "简体中文", "English" })
        {
            PromptLanguage.Install(() => language, id => id == "简体中文");
            foreach (string kind in new[] { "generate", "privacy_material", "privacy_audit", "event_concept",
                "event_concept_audit", "event_progress", "event_progress_audit", "event_boundary", "event_boundary_audit" })
            {
                var job = new WorldDiplomacyJob { Kind = kind, AwaitingHistoryCompression = true };
                var messages = new JArray(new JObject { ["role"] = "user", ["content"] = new string('x', 600000) });
                string original = messages.ToString();
                Test.True(WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(job, messages, 32000, 10000,
                    _ => throw new Exception("Unexpected token scan"), _ => "", _ => false,
                    (_, _) => throw new Exception("Unexpected rejection"), () => throw new Exception("Unexpected compression"), _ => {})
                    && !job.AwaitingHistoryCompression && messages.ToString() == original,
                    "large diplomatic request remains unchanged: " + kind);
            }
            foreach (string reason in new[] { "input budget exceeded before send: 38254/32000",
                "event_required_evidence_budget_exceeded:retain_prior_cases_and_selected_sources",
                "event_required_input_budget_exceeded: 40000/32000", "declaration input budget exceeded before send: 40000/32000" })
            {
                var (_, o, r, j) = EvidenceOpening();
                SubmitEvidenceDraft(o, j, "We request recognition and offer an independent hearing.");
                string draft = j.PrivacyDraft;
                j.SuspendedReason = reason; j.TechnicalRecoveryAttempts = 3;
                j.RetryAfterUtcTicks = j.NextRecoveryProbeUtcTicks = long.MaxValue;
                r.TechnicalHoldReason = r.TechnicalRequestHoldReason = reason;
                Reload(o); j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
                Call(o, "TryResumeTechnicalRequest", j, false);
                Test.True(j.SuspendedReason == "" && j.Kind == "privacy_audit" && j.PrivacyDraft == draft
                    && j.TechnicalRecoveryAttempts == 3, "saved rejection resumes with draft and other retry allowances retained");
                j.SuspendedReason = "declaration_audit_invalid:controlled"; j.PrivacyAuditRetries = 3;
                j.RecoveryConfiguration = (string)Call(o, "SpeechConfiguration");
                j.RecoveryRevision = o.ResolveRound(j.RoundId).ConversationRevision;
                j.RecoveryKnowledgeRevision = j.CourtKnowledgeRevision;
                Call(o, "TryResumeTechnicalRequest", j, false);
                Test.True(j.SuspendedReason.Length > 0, "semantic rejection remains held");
            }
        }
        Console.WriteLine("PASS input-limit removal; controlled host, no provider/game acceptance.");
    }
}

internal static partial class MaterialSupplyReplay
{
    internal static void InputLimitBudgetRegressions()
    {
        PlanningAuditBudget();
        RealmVoiceAndProgressBudget();
    }
}
