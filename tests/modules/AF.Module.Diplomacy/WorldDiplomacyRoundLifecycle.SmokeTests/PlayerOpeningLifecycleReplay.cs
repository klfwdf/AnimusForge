using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    internal static void PlayerOpeningLifecycle(string incidentPath = null, bool continuation = false)
    {
        foreach (string intent in new[] { "statement", "declare_war", "condemn" })
        {
            var (h,o) = Owner(); h.OralEffectPortsEnabled = true;
            var dossier = JArray.Parse(h.CaptureEventSources(new[] { "a", "b", "c", "p" }, Array.Empty<string>()));
            dossier.Add(new JObject { ["key"] = "lore:imperial", ["known_by"] = "a", ["source_kind"] = "world_book_account",
                ["text"] = "The imperial courts dispute succession and recognition within Calradia." });
            h.MaterialOverride = dossier.ToString();
            string body = intent == "statement" ? "Our imperial legitimacy stands. We shall reunite the empire."
                : intent == "declare_war" ? "I declare war on a." : "I condemn a's interference.";
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o), body, o);
            var root = o.CurrentStorage.Documents.Single();
            var analyze = o.CurrentStorage.Jobs.Single(x => x.Kind == "analyze");
            Test.True(analyze.RoundId==root.RoundId && analyze.SchedulingOwner=="player"
                && o.CanDispatchDiplomacyJob(analyze),"analysis is bound to the actual player case at enqueue, not repaired by a later reload");
            JObject analysis = new() { ["status"]="success", ["title_summary"]="Imperial declaration",
                ["addressed_kingdom_ids"]=new JArray("a","b","c"), ["response_requests"]=new JArray(),
                ["event_participation"]="participate",["tone"]="firm",["confidence"]=1,
                ["actions"]=new JArray(new JObject { ["intent"]=intent,["target_kingdom_id"]=intent=="statement"?"":"a",
                    ["commitment"]=intent=="declare_war"?"binding":"non_binding",["requires_response"]=false }) };
            if (incidentPath != null)
            {
                analysis=JArray.Parse(File.ReadAllText(incidentPath)).OfType<JObject>().Select(x=>JObject.Parse((string)x["raw"]!))
                    .Single(x=>(string)x["actions"]![0]!["intent"]! == intent);
                foreach(var value in analysis.Descendants().OfType<JValue>().Where(x=>x.Type==JTokenType.String))
                    if((string)value=="empire_w")value.Value="p";else if((string)value=="empire_s")value.Value="a";
            }
            o.CommitAnalysis(analyze,analysis.ToString());o.RemoveJob(analyze.JobId);
            var round=o.ResolveRound(root.RoundId);
            Test.True(root.EventExecutionPending && h.Effects==0,"mechanics wait for the grounded opening");
            Test.True(!o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_boundary"),"no boundary shortcut before planning");
            var plan=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
            CountryAdmissionReplay.FreezeFixture(o,plan,new[]{"p","a"});
            o.PrepareSharedRequest(plan);
            var input=JObject.Parse(plan.UserPrompt);
            Test.True(input["sources"] is JArray sources && sources.Any(x=>(string)x["key"]! == root.DocumentId)
                && sources.Any(x=>(string)x["key"]! == "lore:succession"),"real entry captures original player words and related dossiers");
            Test.True(plan.SystemPrompt.Contains(DiplomacyPromptTexts.Get("PlayerOpeningPlan","")),"player opening contract is consumed: "+plan.SuspendedReason);
            var p=Position("p");p["address_to"]=new JArray();p["source_keys"]=new JArray(root.DocumentId);
            var a=Position("a");a["address_to"]=new JArray("p");a["source_keys"]=new JArray(root.DocumentId,"world:current");
            var concept=new JObject { ["title"]="Imperial legitimacy dispute",["background"]="The player published a claim; a seeks recognition of its independent authority.",
                ["initiator"]="p",["participants"]=new JArray(p,a),["material_keys"]=new JArray(root.DocumentId,"world:current","lore:imperial"),
                ["party_review"]=new JArray(plan.CandidateKingdomIds.Select(id=>new JObject { ["kingdom"]=id,["included"]=id is "a" or "p",
                    ["reply_only"]=false,["reason"]=id is "a" or "p" ? "The actual claim directly concerns this sovereign's authority." : "No direct claim or obligation affects this country." })),
                ["narrative"]=new JArray(new JObject { ["kind"]="public_fact",["subject"]="p",["source"]=root.DocumentId,
                    ["text"]=body,["hero_ids"]=new JArray(h.PartyRulerId("p")) }) };
            foreach(string defect in new[]{"human_position","human_fiction","wrong_initiator","missing_trigger","no_npc"})
            {
                var invalid=(JObject)concept.DeepClone();
                if(defect=="human_position")invalid["participants"]![0]!["agenda"]="A fabricated human demand";
                if(defect=="human_fiction") {invalid["narrative"]![0]!["kind"]="adopted_npc_story";invalid["narrative"]![0]!["source"]="adopted:false";((JArray)invalid["material_keys"]!).Add("adopted:false");}
                if(defect=="wrong_initiator")invalid["initiator"]="a";
                if(defect=="missing_trigger")invalid["material_keys"]=new JArray("world:current");
                if(defect=="no_npc")invalid["participants"]=new JArray(p.DeepClone());
                Test.True(!(bool)Call(o,"CheckEventConcept",round,plan.CandidateKingdomIds,plan.EventMaterialSnapshot,invalid,null,false)!,
                    "player planning rejects "+defect+" before adoption");
            }
            for (int i = 0; i < 3; i++)
            {
                string oldId = "prior-player-case-" + i;
                o.CurrentStorage.EventAnnouncements.Add(new() { EventId = oldId, Stage = "started", State = "public",
                    Title = "Prior question", Body = (string)concept["background"]!, Day = i });
                o.CurrentStorage.CompletedRounds.Add(new() { RoundId = oldId, EventBackground = (string)concept["background"]!,
                    Participants = new() { new() { KingdomId = "p" }, new() { KingdomId = "a" } }, LastActivityDay = i });
            }
            concept["narrative_safety"]=new JObject{["digest"]="copied-from-writer",["publication_order_valid"]=true};
            concept["approved"]=true;
            plan.MaterialRepairFeedback="public_narrative_internal_identity:old_replaced_draft";
            Complete(o,plan,concept);
            Test.True(plan.MaterialRepairFeedback=="","a corrected concept does not contaminate its independent audit with the prior rejected draft's finding");
            Test.True(JObject.Parse(plan.PrivacyDraft)["narrative_safety"]==null
                && JObject.Parse(plan.PrivacyDraft)["approved"]==null,"concept writer self-approval is never sent to the independent reviewer");
            Test.True(plan.Kind=="event_concept_audit","source-bound player concept reaches review: "+plan.SuspendedReason);
            Reload(o);round=o.ResolveRound(round.RoundId);plan=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept_audit");
            o.PrepareSharedRequest(plan);
            string boundDraft = plan.PrivacyDraft;
            Complete(o, plan, new JObject { ["approved"] = false, ["rejection_category"] = "duplicate",
                ["reason"] = "The player already asked the same question in an earlier event." });
            Test.True(plan.Kind == "event_concept_audit" && plan.PrivacyDraft == boundDraft
                && plan.MaterialRepairFeedback.Contains("bound_matter_not_novelty_candidate"),
                "an outdated novelty refusal corrects the same independent review without inventing a different human request");
            o.PrepareSharedRequest(plan);
            var unsupportedAudit = Approved(plan);
            unsupportedAudit["narrative_safety"]!["world_book_basis"]![0]!["quote"] = "A fabricated quotation absent from every supplied world book.";
            Complete(o, plan, unsupportedAudit);
            Test.True(plan.Kind == "event_concept_audit" && round.EventPartyReview == "",
                "an approving reviewer cannot pass fabricated world-book quotations");
            o.PrepareSharedRequest(plan);
            Test.True(plan.UserPrompt.Contains("quote_not_contiguous_source_text") && plan.UserPrompt.Contains("fabricated quotation")
                && plan.SystemPrompt.Contains("narrative_safety.world_book_basis"),
                "the real CN/EN independent request receives the specific rejected quotation and nested citation contract");
            Complete(o,plan,Approved(plan));
            Test.True(round.Participants.Single(x=>x.KingdomId=="a").State=="active" && round.PlayerResponses.Any(x=>x.KingdomId=="a"),
                "approved interests admit a real NPC and establish the first reply even for a broadcast");
            Test.True(round.Participants.Single(x=>x.KingdomId=="p").Agenda=="","no invented human position");
            var opening=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");o.PrepareSharedRequest(opening);
            Test.True((string)JObject.Parse(opening.UserPrompt)["setting_and_known_history"]! == "", "opening uses adopted case evidence instead of duplicating an unbounded generic world prompt");
            JObject draft=new() { ["title"]=round.RoundTopic,["body"]=round.EventBackground,["scene"]="Negotiation",
                ["effects"]=new JArray(),["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=concept["narrative"]!.DeepClone() };
            Test.True((bool)Call(o,"ValidateBoundaryEffectRequirement",round,"started",draft,null)!,"grounded opening permits no new numeric effects without bypassing its independent audit");
            draft["effects"]=new JArray(new JObject { ["kind"]="ruler_relation",["kingdom"]="p",["other"]="a",
                ["delta"]=-1,["days"]=0,["reason"]="The public claim challenges the other court's legitimacy." });
            var unsupported=(JObject)draft.DeepClone();
            ((JArray)unsupported["narrative"]!).Add(new JObject{["kind"]="allegation",["subject"]="a",
                ["source"]="lore:succession",["text"]="The southern ruler has publicly accused the player of usurpation.",
                ["hero_ids"]=new JArray(h.PartyRulerId("a"))});
            Complete(o,opening,unsupported);
            Test.True(opening.Kind=="event_boundary" && opening.PrivacySourceMaterial.Contains("npc_allegation_requires_published_source_document")
                && h.Writes==0,"opening cannot turn a dossier or internal stance into an already published NPC accusation");
            draft["narrative_safety"]=new JObject{["digest"]="copied-from-writer"};draft["actor_review"]=new JArray();
            o.PrepareSharedRequest(opening);Complete(o,opening,draft);o.PrepareSharedRequest(opening);
            Test.True(JObject.Parse(opening.PrivacyDraft)["narrative_safety"]==null
                && JObject.Parse(opening.PrivacyDraft)["actor_review"]==null,"boundary reviewer receives no writer-authored approval or old digest");
            if (intent == "statement")
            {
            var wrongQuote = Approved(opening);
            wrongQuote["narrative_safety"]!["world_book_basis"]![0]!["quote"] = "A stitched summary that is not the original source passage.";
            string unchangedDraft = opening.PrivacyDraft, unchangedSources = opening.EventMaterialSnapshot;
            Complete(o, opening, wrongQuote); o.PrepareSharedRequest(opening);
            Test.True(opening.Kind == "event_boundary_audit" && opening.PrivacyDraft == unchangedDraft
                && opening.EventMaterialSnapshot == unchangedSources && opening.UserPrompt.Contains("quote_not_contiguous_source_text")
                && opening.UserPrompt.Contains("stitched summary") && h.Writes == 0,
                "boundary correction reaches the actual request with the failed quote while preserving draft, sources and unexecuted effects");
            }
            else
            {
            var extraActor=Approved(opening);extraActor["actor_review"]=new JArray(new JObject{["index"]=0,["digest"]="wrong-global-digest",["complete"]=true,["hero_ids"]=new JArray("ruler_p")});
            Complete(o,opening,extraActor);
            Test.True(opening.Kind=="event_boundary_audit" && opening.PrivacySourceMaterial.Contains("match_exactly=[]"),
                "unsolicited actor rows receive the exact empty review contract instead of an unactionable generic retry");
            }
            var missingOrder=Approved(opening);((JObject)missingOrder["narrative_safety"]!).Remove("publication_order_valid");
            Complete(o,opening,missingOrder);
            Test.True(opening.Kind=="event_boundary_audit" && !o.CurrentStorage.EventAnnouncements.Any(x => x.EventId == round.RoundId),"missing publication-order audit cannot publish an opening");
            var premature=Approved(opening);premature["narrative_safety"]!["publication_order_valid"]=false;
            premature["narrative_safety"]!["reason"]="Controlled semantic finding: the body claims an NPC has publicly replied before its first declaration.";
            Complete(o,opening,premature);
            Test.True(opening.Kind=="event_boundary" && h.Writes==0,"premature reply claims return to writing before any effect");
            o.PrepareSharedRequest(opening);Complete(o,opening,draft);o.PrepareSharedRequest(opening);Complete(o,opening,Approved(opening));
            Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==round.RoundId && x.Stage=="started" && x.State=="public"),
                "opening publishes with actual player identity and adopted participants: "+opening.PrivacySourceMaterial);
            Test.True(h.Writes==1 && h.Effects==(intent=="declare_war"?1:0),"opening effect and authorized action execute exactly once");
            Test.True(!o.CurrentStorage.Jobs.Any(x=>x.Kind=="generate"),"no NPC response before delivery and no AI-authored player speech");
            Reload(o);round=o.ResolveRound(round.RoundId);root=o.ResolveDocument(root.DocumentId);
            o.ReconcileActiveDiplomacyAfterLoad();Call(o,"CommitPendingEventDocuments",round,false);
            Test.True(h.Writes==1 && h.Effects==(intent=="declare_war"?1:0),"reload retains effect and action receipts");
            h.CurrentDayValue=o.CurrentStorage.PropagationArrivals.Where(x=>x.Scope=="court" && x.DocumentId==root.DocumentId).Max(x=>x.DueDay);
            o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            if (!o.CurrentStorage.Jobs.Any(x=>x.Kind=="generate" && x.AuthorKingdomId=="a"))
                throw new Exception("Response not scheduled: "+JsonConvert.SerializeObject(new {round,o.CurrentStorage.Jobs,o.CurrentStorage.PropagationArrivals,o.CurrentStorage.KingdomKnowledge}));
            var response=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate" && x.AuthorKingdomId=="a");
            o.PrepareSharedRequest(response);
            if(response.Kind=="privacy_material")Complete(o,response,new(){["safe"]=true,["public_context"]="The player's declaration has reached our court. We dispute its claim; no agreement has been made."});
            o.PrepareSharedRequest(response);
            CompleteWriter(o,response,new(){["title"]="Response to the imperial claim",["body"]="We reject your claim and demand recognition of our independent authority.",
                ["actions"]=new JArray(new JObject{["intent"]="statement",["target_kingdom_id"]="p",["commitment"]="non_binding"}),
                ["requires_response"]=false,["round_participation"]="continue",["round_status"]="continue",["response_requests"]=new JArray(),
                ["answered_player_document_ids"]=new JArray(root.DocumentId)});
            o.PrepareSharedRequest(response);var evidence=Evidence(o,response);
            Test.True((bool)evidence["required_agenda"]!,"first reply must express the admitted NPC position, not merely acknowledge mail");
            Test.True(((JArray)evidence["allowed_answer_sources"]!).Any(x=>(string)x["source"]! == root.DocumentId
                && (bool)x["required_answer"]!),"actual player words are an answerable required source without inventing a human agenda");
            Complete(o,response,SpanReview(evidence,false,root.DocumentId));
            Test.True(response.Kind=="privacy_audit" && response.PrivacyRepairFeedback.Contains("required_core_agenda_span_missing"),
                "completion enforces the same required NPC agenda as its request");
            Complete(o,response,SpanReview(evidence,true,"unpublished-other-case-document"));
            Test.True(response.Kind=="privacy_audit" && response.PrivacyRepairFeedback.Contains("agenda_answer_source_or_quote_invalid"),
                "player source support does not admit unknown or cross-case evidence");
            Complete(o,response,SpanReview(evidence,true,root.DocumentId));
            var reply=o.CurrentStorage.Documents.Single(x=>x.AuthorKingdomId=="a" && x.IsReadyForPublication);
            var npcAllegation=new JObject{["kind"]="allegation",["subject"]="a",["source"]=reply.DocumentId,
                ["hero_ids"]=new JArray("ruler_a","ruler_p"),["text"]=reply.Body};
            Test.True((bool)Call(o,"ValidatePlayerNarrativeSource",npcAllegation,null)!,
                "an actual NPC allegation mentioning the player is not misclassified as player-authored speech");
            var falseHuman=(JObject)npcAllegation.DeepClone();falseHuman["subject"]="p";falseHuman["hero_ids"]=new JArray();
            Test.True(!(bool)Call(o,"ValidatePlayerNarrativeSource",falseHuman,null)!,
                "human speech still needs a real player document even when hero IDs are omitted");
            Test.True(round.PlayerResponses.Where(x=>x.KingdomId=="a").All(x=>x.AnswerDocumentId==reply.DocumentId)
                && round.Participants.Single(x=>x.KingdomId=="a").AgendaExpressionDocumentId==reply.DocumentId,
                "published reply fulfils the original obligation and joins continuing negotiation");
            if (continuation && !round.EventConvergence.Ending) reply = ContinuePlayerOpening(h, o, round, reply);
            round = o.ResolveRound(round.RoundId);
            h.CurrentDayValue=o.CurrentStorage.PropagationArrivals.Where(x=>x.Scope=="court" && x.DocumentId==reply.DocumentId).Max(x=>x.DueDay);
            o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            Call(o,"AdvanceEventProgress",round);
            if (!round.EventConvergence.Ending)
            {
            var progress=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_progress");
            o.PrepareSharedRequest(progress);
            var closure=new JObject { ["outcome"]="deadlocked",["reason"]="The actual rejection leaves the claim unresolved; neither side has accepted new terms.",
                ["positions"]=new JArray(Position("a")),
                ["closure"]=new JArray(round.Participants.Where(x=>x.State=="active" || x.Agenda.Length>0 || x.KingdomId==root.AuthorKingdomId).Select(x=>new JObject{
                    ["kingdom"]=x.KingdomId,["status"]="unresolved",["reason"]="The public claim remains disputed.",
                    ["sources"]=new JArray(root.DocumentId,reply.DocumentId) })) };
            var malformed=(JObject)closure.DeepClone();
            var misplaced=(JObject)closure.DeepClone();misplaced["narrative"]=new JObject{["body"]="An unsolicited announcement"};
            Complete(o,progress,misplaced);
            Test.True(progress.Kind=="event_progress" && progress.PrivacySourceMaterial.Contains("misplaced_boundary_fields:narrative"),
                "progress cannot hide an unsupported announcement outside the effect-update protocol");
            malformed["positions"]![0]!["preferred_outcome"]=malformed["positions"]![0]!["gain"]!.DeepClone();
            ((JObject)malformed["positions"]![0]!).Remove("gain");
            Complete(o,progress,malformed);
            Test.True(progress.Kind=="event_progress" && progress.PrivacySourceMaterial.Contains("use_nonempty_agenda_gain_cost"),
                "real-model position alias defect is corrected before spending an audit request");
            closure["narrative_safety"]=new JObject{["digest"]="copied-from-writer"};closure["closure_consistent"]=true;
            o.PrepareSharedRequest(progress);Complete(o,progress,closure);o.PrepareSharedRequest(progress);
            Test.True(progress.PrivacySourceMaterial=="","a corrected progression does not carry obsolete writer errors into the new audit");
            Test.True(JObject.Parse(progress.PrivacyDraft)["narrative_safety"]==null
                && JObject.Parse(progress.PrivacyDraft)["closure_consistent"]==null,"progress reviewer cannot copy the writer's self-approval");
            var approval=Approved(progress);approval["closure_consistent"]=true;
            Complete(o,progress,approval);
            }
            else Test.True(!o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_progress")
                && (round.EventConvergence.CompletedRounds==2 || round.EventConvergence.ActualWar),
                "two source pairs or actual war use program ending instead of another discretionary progress request");
            Test.True(round.EventPhase=="closing" && round.State=="active","case stays active until ending is published");
            var ending=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary" && x.ForcedIntent=="ended");
            o.PrepareSharedRequest(ending);
            Test.True(JObject.Parse(ending.UserPrompt)["applied_effect_receipts"] is JArray receipts
                && receipts.Any(x=>(string)x["Kind"]! == "ruler_relation" && (float)x["AppliedTotal"]! == -1),
                "ending retains completed instant effects even after they leave the active-effect list");
            var end=new JObject{["title"]=round.RoundTopic,["body"]="The claim and rejection remain unresolved. No agreement was reached.",
                ["effects"]=new JArray(),["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray()};
            foreach(string leakedBody in new[]{"Garios (ruler_p) published diplomacy_document_2.",
                "南帝国发布了diplomacy_document_10进行回应，随后又发布diplomacy_document_13宣布退出。"})
            {
                var leaked=(JObject)end.DeepClone();leaked["body"]=leakedBody;
                object[] leakCheck={round,"ended",leaked,null!};
                Test.True(!(bool)Call(o,"TryPublishEventBoundary",leakCheck)!
                    && ((string)leakCheck[3]).Contains("public_narrative_internal_identity"),
                    "provider internal identities cannot leak into public prose, including adjacent Chinese text");
            }
            if (!continuation) foreach(string unsupportedChoice in new[]{"四方立场针锋相对、互不妥协。","All sides refused compromise.","双方互不承认。",
                "随着双方正统之争立场对立且无进一步妥协意愿，当前外交交锋告一段落。", "Both sides showed no willingness to compromise.",
                "双方关于帝国合法性的争端未达成一致且无进一步谈判意向。"})
            {
                var invalidEnd=(JObject)end.DeepClone();invalidEnd["body"]=unsupportedChoice;
                object[] check={round,"ended",invalidEnd,null!};
                Test.True(!(bool)Call(o,"TryPublishEventBoundary",check)!
                    && ((string)check[3]).Contains("unanswered_player_collective_decision"),
                    "actual provider collective-choice failure cannot publish even with a rubber-stamped audit");
            }
            Complete(o,ending,end);o.PrepareSharedRequest(ending);Complete(o,ending,Approved(ending));
            Test.True(round.State!="active" && o.CurrentStorage.EventAnnouncements.Count(x=>x.EventId==round.RoundId && x.State=="public")==2,
                "real lifecycle reaches one opening and one ending under the same event: "+JsonConvert.SerializeObject(new {ending.Kind,ending.SuspendedReason,ending.PrivacySourceMaterial,ending.PrivacyRepairFeedback,round.State,round.EventPhase,o.CurrentStorage.EventAnnouncements}));
            Reload(o);o.ReconcileActiveDiplomacyAfterLoad();
            Test.True(o.CurrentStorage.Documents.Count(x=>x.IsPlayerAuthored)==(continuation && intent != "declare_war" ? 2 : 1) && h.Writes==1 && h.Effects==(intent=="declare_war"?1:0),
                "closed reload neither republishes declarations nor repeats actions or boundary effects");
            Console.WriteLine("PLAYER FULL LIFECYCLE PASS "+intent+" -> plan -> audit -> effect/opening -> delivery -> reply -> progression -> ending -> reload");
        }
    }
}
