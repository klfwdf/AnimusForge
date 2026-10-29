using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private sealed class TermsHost : Host, IWorldDiplomacyThreatMechanicsHost
    {
        private readonly LiveDiplomacyFixReplay.Prestige standing = new();
        public IWorldDiplomacyPrestigePort Prestige() => standing;
        public IWorldDiplomacyThreatBindingPort ThreatBinding() => new TermsBinding(this);
        public override IWorldDiplomacyThreatSettlementPort ThreatSettlement() => new ThreatSettlementReplay.Port(Owner.CurrentStorage);
    }
    private sealed class TermsBinding(TermsHost host) : IWorldDiplomacyThreatBindingPort
    {
        public WorldDiplomacyRound ResolveRound(string id) => host.Owner.ResolveRound(id);
        public bool IsPolicyActive(string p,string o,string a) => false;
        public string RepresentativeId(string id) => id;
        public int CurrentDay() => host.CurrentDayValue;
        public string NewId(string kind) => host.NewId(kind);
        public int EscalationPrestigeReward => 3;
        public int WarPrestigeReward => 5;
        public void Log(string text) { }
    }
    internal static void OptionalTermsReplay(string incidentPath = null)
    {
        foreach (string field in new[] { "peace_terms", "treaty_terms" })
        foreach (JToken value in new JToken[] { JValue.CreateNull(), new JObject(), new JArray(), new JValue(""), new JValue(0), new JValue(false) })
        {
            var (_,o,r,j)=EvidenceOpening();
            var draft=new JObject { ["title"]="Public reply",["body"]="We reject your allegations and request a public hearing.",
                ["actions"]=new JArray(new JObject { ["target_kingdom_id"]="b",["intent"]="condemn",["commitment"]="non_binding",[field]=value.DeepClone() }),
                ["mentioned_kingdom_ids"]=new JArray("b"),["response_requests"]=new JArray(),["answered_player_document_ids"]=new JArray() };
            WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(j,draft);
            bool invalid=o.TryGetGeneratedIntentLegalityViolation(j,draft,"a","b",out _,out string reason);
            bool expected=value.Type!=JTokenType.Null && value is not JObject;
            Test.True(invalid==expected,"optional "+field+" "+value.Type+" expected invalid="+expected+" actual reason="+reason);
            if(expected) Test.True(reason=="action[0]:diplomatic_action_terms_shape_invalid:"+field,"invalid terms identify exact action and field");
        }
        foreach(string field in new[]{"peace_terms","treaty_terms"})
        {
            var world=new PromptWorldFixture { EnglishPrompts=SmokeLocalizationEnvironment.EnglishPackage };
            var owner=new PromptOrch(world);
            var j=new WorldDiplomacyJob { JobId="terms-repair",Kind="generate",RoundId="r",AuthorKingdomId="a",TargetKingdomId="b",SystemPrompt="contract",UserPrompt="request" };
            var bad=new JObject { ["actions"]=new JArray(new JObject { ["target_kingdom_id"]="b",["intent"]="condemn",["commitment"]="non_binding",[field]=new JArray() }) };
            WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world,owner,j,bad.ToString(),"a","b","action[0]:diplomatic_action_terms_shape_invalid:"+field,bad);
            var repair=world.Enqueued.Single();
            string feedback=repair.LlmMessages.Last().Content;
            Test.True(feedback.Contains("diplomatic_action_terms_shape_invalid:"+field)&&feedback.Contains("\"action_index\": 0")
                &&feedback.Contains(DiplomacyPromptTexts.Get("DeclarationTermsShapeRepair","MISSING")),"real repair request contains precise field/index and localized guidance");
        }
        {
            var(_,o,_,j)=EvidenceOpening();
            Call(o,"HoldTechnicalRequest",j,"diplomatic_action_terms_shape_invalid");
            j.TechnicalRecoveryAttempts=3;j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Reload(o);j=o.CurrentStorage.Jobs.Single();Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.SuspendedReason==""&&j.TechnicalRecoveryAttempts==0,"exhausted legacy null-terms job resumes after reload");
            Call(o,"HoldTechnicalRequest",j,"action[0]:diplomatic_action_terms_shape_invalid:peace_terms");
            j.TechnicalRecoveryAttempts=3;j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Reload(o);j=o.CurrentStorage.Jobs.Single();Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.SuspendedReason!=""&&j.TechnicalRecoveryAttempts==3,"new malformed terms failure cannot reset its budget across reload");
        }
        if(incidentPath!=null)
        foreach(var record in JArray.Parse(File.ReadAllText(incidentPath)).OfType<JObject>())
        {
            var(h,o,r,j)=EvidenceOpening(new TermsHost { PublishEnabled=true,PlayerAwareExecution=true,OralEffectPortsEnabled=true,OralRange=(0,3000),CurrentDayValue=40 });
            for(int turn=1;turn<=6;turn++)
            {
                o.PrepareSharedRequest(j);
                if(j.Kind=="privacy_material") Complete(o,j,new(){["safe"]=true,["public_context"]="The dispute and delivered declarations are public."});
                o.PrepareSharedRequest(j);
                var draft=(JObject)record["draft"]!.DeepClone();
                // Only realm/document identities are mapped to deterministic host IDs.
                draft["actions"]![0]!["target_kingdom_id"]=j.TargetKingdomId;
                draft["mentioned_kingdom_ids"]=new JArray(j.TargetKingdomId);
                draft["answered_player_document_ids"]=new JArray();
                if(turn!=4)
                {
                    draft["body"]="We demand a public hearing and reject the allegations. Please answer our demand.";
                    draft["requires_response"]=true;
                }
                if(turn==3){draft["actions"]![0]!["intent"]="ultimatum";draft["actions"]![0]!["commitment"]="binding";}
                if(turn==5){draft["actions"]![0]!["intent"]="declare_war";draft["actions"]![0]!["commitment"]="binding";}
                CompleteWriter(o,j,draft);o.PrepareSharedRequest(j);
                var evidence=Evidence(o,j);
                var answers=((JArray)evidence["allowed_answer_sources"]!).Select(x=>(string)x["source"]!).ToArray();
                Complete(o,j,SpanReview(evidence,true,answers));
                Test.True(o.CurrentStorage.Documents.Count(x=>x.IsReadyForPublication)==turn,"incident chain publishes turn "+turn+": "+j.SuspendedReason);
                var published=o.CurrentStorage.Documents.Last(x=>x.IsReadyForPublication);
                if(turn==4)
                {
                    Test.True(published.Body==(string)record["draft"]!["body"]! && !published.RequiresResponse,"recorded fourth declaration publishes unchanged, including no-reply flag: "+record["time"]);
                    Test.True(!o.CurrentStorage.Jobs.Any(x=>x.Kind=="generate"),"issuer cannot respond to an undelivered refusal");
                    Test.True(o.CurrentStorage.DiplomaticThreats.Any(x=>x.TargetDecision=="noncomplied"&&x.TargetDecisionDocumentId==published.DocumentId),"published refusal binds the exact pending ultimatum");
                }
                if(turn==6)break;
                Reload(o);r=o.ResolveRound(r.RoundId);h.CurrentDayValue+=20;
                o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
                var next=o.CurrentStorage.Jobs.Where(x=>x.Kind=="generate").ToList();
                Test.True(next.Count==1,"next response after turn "+turn+": "+Newtonsoft.Json.JsonConvert.SerializeObject(o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SuspendedReason,x.AuthorKingdomId})));j=next.Single();
            }
            int count=o.CurrentStorage.Documents.Count;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            Test.True(o.CurrentStorage.Documents.Count==count,"duplicate processing does not republish the response");
        }
        Console.WriteLine("Optional terms shape, exact-offer guards, bounded legacy recovery and repair feedback passed; incident cases="+(incidentPath==null?0:JArray.Parse(File.ReadAllText(incidentPath)).Count)+"; each incident reaches six publications via delivery/reload. Semantic reviews and engine effects are controlled fixtures, real provider/game NOT_RUN.");
    }
}
