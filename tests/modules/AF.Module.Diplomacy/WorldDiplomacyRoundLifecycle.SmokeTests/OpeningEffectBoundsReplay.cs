using AnimusForge;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private static void OpeningEffectBounds()
    {
        foreach (string mode in new[] { "empty", "all_clamped", "partial", "became_clamped" })
        {
            var (h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);r.RoundTopic="Succession dispute";
            h.Relation=mode=="became_clamped"?0:-100;
            var effects=new JArray();
            if(mode!="empty")effects.Add(new JObject{["kind"]="ruler_relation",["kingdom"]="a",["other"]="b",
                ["delta"]=-1,["days"]=0,["reason"]="The rival claim strains existing relations."});
            if(mode=="partial")effects.Add(new JObject{["kind"]="town_loyalty",["kingdom"]="a",["settlement"]="town_a",
                ["delta"]=-1,["days"]=2,["reason"]="The sourced local legitimacy dispute divides the town."});
            var j=new WorldDiplomacyJob{JobId="bounds",Kind="event_boundary",ForcedIntent="started",RoundId=r.RoundId,AuthorKingdomId="a"};
            o.CurrentStorage.Jobs.Add(j);o.PrepareSharedRequest(j);
            var draft=new JObject{["title"]=r.RoundTopic,["body"]="The succession dispute brings rival claims into negotiation.",
                ["effects"]=effects,["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray()};
            Complete(o,j,draft);
            Test.True(j.Kind=="event_boundary_audit" && h.Writes==0,"bound-aware opening reaches audit without execution: "+mode+" "+j.PrivacySourceMaterial);
            Test.True(((JArray)JObject.Parse(j.PrivacyDraft)["effects"]!).Count==(mode is "partial" or "became_clamped"?1:0),
                "only clamped effects are removed; unrelated valid effects remain: "+mode);
            o.PrepareSharedRequest(j);var approval=Approved(j);
            if(mode=="became_clamped")h.Relation=-100;
            Complete(o,j,approval);
            if(mode=="became_clamped")
            {
                Test.True(j.Kind=="event_boundary_audit" && !o.CurrentStorage.EventAnnouncements.Any() && h.Writes==0,
                    "world change during review requires a review of the reduced exact draft, not stale approval");
                o.PrepareSharedRequest(j);Complete(o,j,Approved(j));
            }
            Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==r.RoundId && x.Stage=="started" && x.State=="public"),
                "grounded case actually opens after audit at numeric limits: "+mode+" "+Newtonsoft.Json.JsonConvert.SerializeObject(new{j.Kind,j.PrivacySourceMaterial,j.SuspendedReason,r.RoundTopic,r.State,notices=o.CurrentStorage.EventAnnouncements}));
            Test.True(h.Writes==(mode=="partial"?1:0) && h.Relation==-100,
                "no fabricated relation changes, valid town effect executes once: "+mode);
            Reload(o);o.ReconcileActiveDiplomacyAfterLoad();
            Test.True(h.Writes==(mode=="partial"?1:0),"reload does not reapply opening effects: "+mode);
        }
        var (nh,no)=Owner(new ModelHost{PublishEnabled=true,PlayerAwareExecution=true,OralRange=(0,3000),CurrentDayValue=40});
        WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(no),"We assert our legitimate claim.",no);
        var round=no.ResolveRound(no.CurrentStorage.Documents.Single().RoundId);
        foreach(string kind in new[]{"public_fact","adopted_npc_story","allegation"})
        {
            var draft=new JObject{["body"]="The claims are disputed.",["narrative"]=new JArray(new JObject{
                ["kind"]=kind,["subject"]="a",["source"]="world:current",["text"]="Rhagaea has issued a formal reply rejecting the claim."})};
            Test.True(!(bool)Call(no,"ValidatePlayerOpeningNarrative",round,"started",draft,null)!,
                "identity/world data cannot prove an already issued reply under narrative kind "+kind);
        }
        foreach(string body in new[]{"Rhagaea has issued a formal reply rejecting the claim.","Rhagaea针锋相对地发出严厉回应。"})
        {
            var draft=new JObject{["body"]=body,["narrative"]=new JArray()};
            Test.True(!(bool)Call(no,"ValidatePlayerOpeningNarrative",round,"started",draft,null)!,
                "omitting the invented reply from narrative records does not evade public prose checking");
        }
    }
}
