using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private static JObject Evidence(WorldDiplomacyOrchestration owner, WorldDiplomacyJob job)
    {
        var evidence=(JObject)Call(owner,"BuildDeclarationAuditEvidence",job);
        evidence["continuity_fixture"]=ContinuityReview(owner,job);
        return evidence;
    }

    private static JObject SpanReview(JObject evidence, bool agenda, params string[] answers)
    {
        int last=((JArray)evidence["body_segments"]!).Count-1;
        return new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["draft_digest"]=evidence["draft_digest"]!.DeepClone(),
            ["response_continuity"]=evidence["continuity_fixture"]?.DeepClone(),
            ["agenda_span"]=agenda?new JArray(0,last):new JArray(),
            ["agenda_answers"]=new JArray(answers.Select(id=>new JObject{["source"]=id,["span"]=new JArray(0,last)}))};
    }

    private static JObject SubmitEvidenceDraft(WorldDiplomacyOrchestration o,WorldDiplomacyJob j,string body,bool requiresResponse = true)
    {
        o.PrepareSharedRequest(j);
        if(j.Kind=="privacy_material")Complete(o,j,new(){["safe"]=true,["public_context"]="The public dispute is known. No treaty or transfer has occurred."});
        o.PrepareSharedRequest(j);
        CompleteWriter(o,j,new(){["title"]="Position on the dispute",["body"]=body,
            ["actions"]=new JArray(new JObject{["target_kingdom_id"]=j.TargetKingdomId,["intent"]="statement",["commitment"]="none",["negotiation_move"]="state_position"}),
            ["requires_response"]=requiresResponse,["round_participation"]="continue",["round_status"]="continue",["response_requests"]=new JArray(),["answered_player_document_ids"]=new JArray()});
        Test.True(j.Kind=="privacy_audit","actual writer enters review with an unpublished draft");
        o.PrepareSharedRequest(j);
        var evidence=Evidence(o,j);
        Test.True(j.UserPrompt.Contains("\"public_draft\": {")&&j.UserPrompt.Contains("\"allowed_answer_sources\"")
            &&j.SystemPrompt.Contains("agenda_span")&&!j.SystemPrompt.Contains("agenda_quote"),"actual CN/EN reviewer input uses structured draft and one consistent span contract");
        Test.True(string.Concat(((JArray)evidence["body_segments"]!).Select(x=>(string)x["text"]!))==body,
            "numbered segments retain every original punctuation and whitespace character");
        return evidence;
    }

    private static (Host h,WorldDiplomacyOrchestration o,WorldDiplomacyRound r,WorldDiplomacyJob j) EvidenceOpening(Host supplied = null)
    {
        var(h,o)=Owner(supplied);o.CurrentStorage.MaterialCountrySchemaVersion=1;
        var r=o.EnsureActiveRound("a","b",false);Foundation(r);r.InitiatorKingdomId="a";r.EventPhase="opening";
        foreach(var p in r.Participants){p.State="active";p.AgendaAddressTo=new(){p.KingdomId=="a"?"b":"a"};}
        var opening=Draft("The ordinary lord requested a hearing.","ordinary_lord");
        var row=(JObject)opening["narrative"]![0]!;row["subject_kingdom_id"]=row["subject"]!.DeepClone();row.Remove("subject");
        Test.True(ReviewedBoundary(o,r,"started",opening,new[]{"ordinary_lord"}),"recorded subject_kingdom_id spelling publishes through independent actor review");
        return(h,o,r,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
    }

    internal static void PublicationEvidenceReplay(string incidentPath=null)
    {
        foreach(string body in new[]{"We demand recognition.\nWe offer a hearing; no confession is accepted! Will you answer?",
            "我们要求公开承认。\n愿意举行听证；但绝不接受认罪！你方如何答复？"})
        {
            var(h,o,r,j)=EvidenceOpening();var evidence=SubmitEvidenceDraft(o,j,body);
            Test.True(((JArray)evidence["allowed_answer_sources"]!).Count==0,"first declaration has no previous published agenda document to answer");
            Complete(o,j,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["agenda_quote"]="Recognition",["agenda_answers"]=new JArray()});
            Test.True(j.PrivacyAuditRetries==1&&o.CurrentStorage.Documents.Count==0,"wrong copied author_position is rejected without rewriting the draft");
            Complete(o,j,SpanReview(Evidence(o,j),true));
            var first=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication);
            Test.True(first.Body==body&&first.AuditedAgendaQuote==body&&j.PrivacyRewrites==0,"selected exact original is published, not a replacement draft or automatic approval");
            Test.True(!o.CurrentStorage.Jobs.Contains(j),"successful audit removes completed work");
            h.CurrentDayValue+=20;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            var reply=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"&&x.AuthorKingdomId=="b");
            var replyEvidence=SubmitEvidenceDraft(o,reply,"We reject your demand for recognition. We seek an independent hearing, without confession. Please address this alternative.");
            Test.True(((JArray)replyEvidence["allowed_answer_sources"]!).Count==1
                &&(string)replyEvidence["allowed_answer_sources"]![0]!["source"]! == first.DocumentId
                &&(string)replyEvidence["allowed_answer_sources"]![0]!["body"]! == body,
                "recipient review receives only the actual delivered agenda document and exact original");
            Complete(o,reply,SpanReview(replyEvidence,true,first.DocumentId));
            var second=o.CurrentStorage.Documents.Single(x=>x.AuthorKingdomId=="b"&&x.IsReadyForPublication);
            Test.True(r.Participants.Single(x=>x.KingdomId=="a").AgendaAnswerDocumentIds.Contains(second.DocumentId),"second actual publication answers first demand");
            // A fresh process must continue from the same published documents.
            Reload(o);r=o.ResolveRound(r.RoundId);h.CurrentDayValue+=20;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            var third=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"&&x.AuthorKingdomId=="a");
            var thirdEvidence=SubmitEvidenceDraft(o,third,"We reject the independent hearing unless both courts select the witnesses. We still make no confession.");
            Complete(o,third,SpanReview(thirdEvidence,false,second.DocumentId));
            Test.True(o.CurrentStorage.Documents.Count(x=>x.IsReadyForPublication)==3
                &&r.Participants.Single(x=>x.KingdomId=="b").AgendaAnswerDocumentIds.Count==1,"after reload and real court delivery a third substantive declaration publishes");
            o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            Test.True(o.CurrentStorage.Documents.Count(x=>x.IsReadyForPublication)==3,"duplicate arrival does not duplicate publication");
        }
        {
            var(_,o,_,j)=EvidenceOpening();
            var evidence=SubmitEvidenceDraft(o,j,"We reject confession. We seek recognition.");
            var review=SpanReview(evidence,true);review["agenda_span"]=new JArray(1);
            Complete(o,j,review);
            Test.True(o.CurrentStorage.Documents.Single().AuditedAgendaQuote==" We seek recognition.",
                "real provider singleton span copies exactly one original segment without model rewriting");
        }
        foreach(string failure in new[]{"digest","range","integer","conflict","lore","undelivered","safe","negotiation","empty","singleton-range","singleton-type","singleton-conflict","too-many"})
        {
            var(_,o,r,j)=EvidenceOpening();var evidence=SubmitEvidenceDraft(o,j,"We seek recognition. We reject confession.");
            var review=SpanReview(evidence,true);
            if(failure=="digest")review["draft_digest"]="stale-draft";
            if(failure=="range")review["agenda_span"]=new JArray(-1,999);
            if(failure=="integer")review["agenda_span"]=JArray.Parse("[0,999999999999999999999999999999999999]");
            if(failure=="conflict")review["agenda_quote"]="Recognition";
            if(failure=="singleton-range")review["agenda_span"]=new JArray(999);
            if(failure=="singleton-type")review["agenda_span"]=new JArray("1");
            if(failure=="singleton-conflict"){review["agenda_span"]=new JArray(1);review["agenda_quote"]="Recognition";}
            if(failure=="too-many")review["agenda_span"]=new JArray(0,1,2);
            if(failure=="lore")review["agenda_answers"]=new JArray(new JObject{["source"]="lore:7d582101959980cf",["span"]=new JArray(0,1)});
            if(failure=="undelivered")
            {
                o.AddDocument(new(){DocumentId="not-delivered",RoundId=r.RoundId,AuthorKingdomId="b",IsReadyForPublication=true,Body="A secret destination."});
                var b=r.Participants.Single(x=>x.KingdomId=="b");b.AgendaExpressionDocumentId="not-delivered";
                Test.True(((JArray)Evidence(o,j)["allowed_answer_sources"]!).Count==0,"undelivered originals never enter audit evidence");
                review["agenda_answers"]=new JArray(new JObject{["source"]="not-delivered",["span"]=new JArray(0,1)});
            }
            if(failure=="safe")review["safe"]=false;
            if(failure=="negotiation")review["negotiation_consistent"]=false;
            if(failure=="empty")review["agenda_span"]=new JArray();
            Complete(o,j,review);
            Test.True(!o.CurrentStorage.Documents.Any(x=>x.AuthorKingdomId=="a"&&x.IsReadyForPublication),"new evidence protocol cannot bypass rejection: "+failure);
        }
        {
            var(_,o,r,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,"We seek recognition without confession.");
            for(int n=0;n<3;n++)Complete(o,j,new(){["safe"]=true});
            j.DeclarationAuditEvidenceVersion=0;j.TechnicalRecoveryAttempts=3;j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Reload(o);j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="privacy_audit");
            Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.SuspendedReason==""&&j.DeclarationAuditEvidenceVersion==3&&j.TechnicalRecoveryAttempts==0,"old exhausted audit gets one versioned recovery opportunity");
            for(int n=0;n<3;n++)Complete(o,j,new(){["safe"]=true});
            j.TechnicalRecoveryAttempts=3;j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Reload(o);j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="privacy_audit");Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.SuspendedReason!=""&&j.DeclarationAuditEvidenceVersion==3,"new exhausted failure stays bounded across reload, never fabricates approval");
        }
        foreach(string variant in new[]{"conflict","missing","object","wrong-country"})
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            var d=Draft("The ordinary lord petitioned.","ordinary_lord");var row=(JObject)d["narrative"]![0]!;
            if(variant=="conflict")row["subject_kingdom_id"]="b";
            if(variant=="missing")row.Remove("subject");
            if(variant=="object")row["subject"]=new JObject();
            if(variant=="wrong-country"){row.Remove("subject");row["subject_kingdom_id"]="c";}
            // A prior valid row used to clear reason before the invalid next row.
            ((JArray)d["narrative"]!).Insert(0,new JObject{["kind"]="public_fact",["subject"]="a",["source"]="public",["text"]="A dispute exists.", ["hero_ids"]=new JArray()});
            object[] args={r,"started",d,""};
            Test.True(!(bool)Call(o,"TryPublishEventBoundary",args)&&!string.IsNullOrEmpty((string)args[3])&&o.CurrentStorage.EventAnnouncements.Count==0,
                "invalid subject remains rejected with actionable feedback after valid row: "+variant);
        }
        if(incidentPath!=null)
        {
            var records=JArray.Parse(File.ReadAllText(incidentPath));int count=0;
            foreach(var item in records.OfType<JObject>())
            {
                var(_,o,_,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,(string)item["body"]!);
                Complete(o,j,(JObject)item["audit"]!.DeepClone());
                Test.True(o.CurrentStorage.Documents.Count==0&&j.Kind=="privacy_audit","actual recorded invalid review is rejected: "+item["time"]);
                Complete(o,j,SpanReview(Evidence(o,j),true));
                Test.True(o.CurrentStorage.Documents.Single().Body==(string)item["body"]!,"controlled span review publishes unchanged incident prose: "+item["time"]);count++;
            }
            Console.WriteLine("Exact incident reviews replayed="+count+"; correction responses are controlled, real provider NOT_RUN.");
        }
        Console.WriteLine("Evidence protocol: opening -> published declaration -> court delivery -> reply -> reload -> third publication PASS; real provider/game NOT_RUN.");
    }
}
