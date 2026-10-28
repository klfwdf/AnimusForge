using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    internal static void DeclarationAuditStalls()
    {
        foreach (bool exhaustAgain in new[] { false, true })
        {
            var (h, o, r, opening) = EvidenceOpening();
            SubmitEvidenceDraft(o, opening, "We require a public hearing. We reject confession.");
            Complete(o, opening, SpanReview(Evidence(o, opening), true));
            var first = o.CurrentStorage.Documents.Single(x => x.IsReadyForPublication);
            h.CurrentDayValue += 20; o.ProcessPropagationArrivals(); Call(o, "ReconcileEventQueue");
            var reply = o.CurrentStorage.Jobs.Single(x => x.Kind == "generate" && x.AuthorKingdomId == "b");
            SubmitEvidenceDraft(o, reply, "We reject confession. We require an independent hearing. Answer this alternative.");
            string draft = reply.PrivacyDraft;
            using (var reader = new JsonTextReader(new StringReader(reply.UserPrompt)))
            {
                var request = JObject.Load(reader);
                var shape = request["required_object_reviews"]!["response_continuity"]!;
                Test.True((string)shape["consistent"]! == "boolean"
                    && (string)shape["source_id"]! == first.DocumentId
                    && (string)shape["digest"]! == (string)request["response_continuity_required"]!["digest"]!,
                    "recipient request explicitly binds the required object without supplying a semantic approval");
            }
            // The observed provider response has all booleans/spans but no continuity object.
            for (int cycle = 0; cycle < 3; cycle++)
            {
                for (int n = 0; n < 3; n++)
                {
                    var omission = SpanReview(Evidence(o, reply), true, first.DocumentId);
                    omission.Remove("response_continuity"); Complete(o, reply, omission);
                }
                if (cycle < 2) Call(o, "TryResumeTechnicalRequest", reply, false);
            }
            Test.True(reply.TechnicalRecoveryAttempts == 3 && reply.SuspendedReason.Contains("response_continuity_review_missing_or_invalid")
                && o.CurrentStorage.Documents.Count == 1 && reply.PrivacyDraft == draft,
                "reproduces the observed reply stall without publishing or rewriting an unaudited draft");
            reply.DeclarationAuditEvidenceVersion = 1; // Exact installed old audit contract.
            Reload(o); reply = o.CurrentStorage.Jobs.Single(x => x.JobId == reply.JobId);
            Call(o, "TryResumeTechnicalRequest", reply, false);
            Test.True(reply.DeclarationAuditEvidenceVersion == 3 && reply.TechnicalRecoveryAttempts == 0
                && reply.SuspendedReason == "" && reply.Kind == "privacy_audit" && reply.PrivacyDraft == draft,
                "saved exhausted v1 audit resumes once against the same draft with the corrected request contract");
            if (exhaustAgain)
            {
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    for (int n = 0; n < 3; n++)
                    {
                        var bad = SpanReview(Evidence(o, reply), true, first.DocumentId);
                        bad["response_continuity"]!["digest"] = "stale"; Complete(o, reply, bad);
                    }
                    Reload(o); reply = o.CurrentStorage.Jobs.Single(x => x.JobId == reply.JobId);
                    Call(o, "TryResumeTechnicalRequest", reply, false);
                }
                Test.True(reply.TechnicalRecoveryAttempts == 3 && reply.SuspendedReason != ""
                    && reply.DeclarationAuditEvidenceVersion == 3 && o.CurrentStorage.Documents.Count == 1,
                    "reload cannot renew the migration budget or approve repeatedly stale evidence");
                continue;
            }
            var corrected = SpanReview(Evidence(o, reply), true, first.DocumentId);
            corrected["agenda_span"] = new JArray(0, 1, 2);
            Complete(o, reply, corrected);
            Test.True(o.CurrentStorage.Documents.Count(x => x.IsReadyForPublication) == 2
                && o.CurrentStorage.Documents.Single(x => x.AuthorKingdomId == "b").Body == (string)JObject.Parse(draft)["body"]!,
                "corrected independent review publishes the original recipient reply after saved hold recovery");
            var second = o.CurrentStorage.Documents.Single(x => x.AuthorKingdomId == "b");
            Reload(o); h.CurrentDayValue += 20; o.ProcessPropagationArrivals(); Call(o, "ReconcileEventQueue");
            r=o.ResolveRound(r.RoundId);
            Test.True((bool)Call(o,"TryApplyEventProgress",r,new JObject { ["outcome"]="continue",
                ["reason"]="A must decide on the newly received alternative.",["next_actor"]="a",["source_document"]=second.DocumentId,
                ["positions"]=new JArray(r.Participants.Select(x=>new JObject { ["kingdom"]=x.KingdomId,
                    ["agenda"]=x.Agenda,["gain"]=x.PreferredOutcome,["cost"]=x.ExpectedCost,["red_line"]=x.RedLine,["alternative"]=x.Alternative })) },""),
                "recovered source remains eligible for a selected second country speech");
            var third = o.CurrentStorage.Jobs.Single(x => x.Kind == "generate" && x.AuthorKingdomId == "a");
            SubmitEvidenceDraft(o, third, "We acknowledge the alternative. We retain our position without requesting another answer.",false);
            Complete(o, third, SpanReview(Evidence(o, third), false, second.DocumentId));
            Test.True(o.CurrentStorage.Documents.Count(x => x.IsReadyForPublication) == 3,
                "the recovered exchange continues to a third actual publication across save/load and delivery");
        }
        var method = typeof(WorldDiplomacyOrchestration).GetMethod("TryReadDeclarationSpan", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var span in new[] { new JArray(0, 1, 2), new JArray(0, 2), new JArray(1), new JArray(0, 2, 3), new JArray(0, 1, 1), new JArray(2, 1, 0), new JArray(0, 1, long.MaxValue) })
        {
            object[] args = { span, "First. Second. Third. Fourth.", "" };
            bool valid = (bool)method.Invoke(null, args)!;
            Test.True(valid == (span.Count < 3 || JToken.DeepEquals(span, new JArray(0, 1, 2))),
                "only bounded contiguous evidence ranges are accepted: " + span.ToString(Formatting.None));
        }
        string output = DiplomacyPromptTexts.Get("DeclarationAuditOutput", "");
        using (var reader = new JsonTextReader(new StringReader(output.Substring(output.IndexOf('{')))))
        {
            var example = JObject.Load(reader);
            Test.True(example["response_continuity"] is JObject && example["recovery_consistent"]?.Type == JTokenType.Boolean
                && output.Contains("required_object_reviews"), "actual CN/EN complete output example includes continuity and ending recovery contracts");
        }
        Console.WriteLine("Declaration stalls: saved v1 recovery, bounded migration, continued actual publication and evidence span guards PASS; semantic reviews controlled; game/provider NOT_RUN.");
    }
}
