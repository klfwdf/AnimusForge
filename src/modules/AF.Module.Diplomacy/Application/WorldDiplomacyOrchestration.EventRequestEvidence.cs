using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Request boundary only: cap completed history, never pending obligations.
    // Source records remain intact for archive retrieval and execution checks.
    private JObject BuildEventRequestEvidence(WorldDiplomacyRound round, bool ending = false)
    {
        const int recentLimit = 16;
        var arrangements = Storage.DialogueArrangements.Where(x => x.RoundId == round.RoundId).ToList();
        var pending = arrangements.Where(DiplomacyCommitmentRules.IsPending).ToList();
        var required = new HashSet<string>(StringComparer.Ordinal) { round.RootDocumentId };
        foreach (var offer in round.PendingOffers.Where(x => x.Status == "open")) required.Add(offer.SourceDocumentId);
        foreach (var item in pending) { required.Add(item.SourceDocumentId); required.Add(item.DocumentId); }
        foreach (var item in round.PlayerResponses.Where(x => x.Status == "pending")) required.Add(item.SourceDocumentId);
        foreach (var item in round.PlayerCourtWork.Where(x => x.Status is "pending" or "queued")) required.Add(item.SourceDocumentId);
        foreach (var item in round.PlayerNegotiationWaits.Where(x => x.RequiresAnswer)) required.Add(item.SourceDocumentId);
        foreach (var item in Storage.Jobs.Where(x => x.RoundId == round.RoundId)) required.Add(item.SourceDocumentId);
        foreach (var item in Storage.AidRequests.Where(x => x.RoundId == round.RoundId && x.Status == "accepted")) required.Add(item.RequestDocumentId);
        var documents = Storage.Documents.Where(x => x.RoundId == round.RoundId && x.IsReadyForPublication).ToList();
        var selected = documents.Where(x => required.Contains(x.DocumentId)).ToList();
        int historyBudget = 32000;
        foreach (var document in documents.Where(x => !required.Contains(x.DocumentId)).OrderByDescending(x => x.Day)
            .ThenByDescending(x => x.CreatedUtcTicks).Take(recentLimit))
        {
            int cost = Newtonsoft.Json.JsonConvert.SerializeObject(document).Length;
            if (cost > historyBudget) continue;
            selected.Add(document); historyBudget -= cost;
        }
        if (ending) selected = documents; // Every participant/result is represented in the conclusion.
        int bodyLimit = ending ? Math.Max(160, Math.Min(1600, 12000 / Math.Max(1, selected.Count))) : int.MaxValue;
        var completed = new List<WorldDiplomacyDialogueArrangement>();
        int commitmentBudget = 32000;
        foreach (var arrangement in arrangements.Where(x => !DiplomacyCommitmentRules.IsPending(x))
            .OrderByDescending(x => x.CreatedDay).Take(recentLimit))
        {
            int cost = Newtonsoft.Json.JsonConvert.SerializeObject(arrangement).Length;
            if (cost > commitmentBudget) continue;
            completed.Add(arrangement); commitmentBudget -= cost;
        }
        var offers = round.PendingOffers.Where(x => x.Status == "open")
            .Concat(round.PendingOffers.Where(x => x.Status != "open").OrderByDescending(x => x.CreatedDay).Take(recentLimit)).ToList();
        return JObject.FromObject(new {
            documents = selected.Select(x => new {
                x.DocumentId, x.SourceDocumentId, x.AuthorKingdomId, x.TargetKingdomId, x.Title,
                Body = x.Body?.Length > bodyLimit ? x.Body.Substring(0, bodyLimit) : x.Body, x.IsPlayerAuthored,
                body_is_excerpt = x.Body?.Length > bodyLimit,
                x.Intent, x.Commitment, x.Actions, x.PeaceTerms, x.TreatyTerms, x.RequiresResponse,
                x.RoundParticipation, x.RoundStatus, x.ChangedDiplomaticState, x.MechanicalResult,
                known_by = round.Participants.Where(p => DialogueDocumentKnown(p.KingdomId, x.DocumentId)).Select(p => p.KingdomId)
            }),
            commitments = pending.Concat(completed).Select(x => new {
                x.ArrangementId, x.Version, x.ActorKingdomId, x.TargetKingdomId, x.Action, x.Move, x.Terms,
                x.Status, x.Reason, x.DocumentId, x.SourceDocumentId, x.SourceActionId,
                source_is_private = true, source_ruler_id = x.RulerId,
                x.SupersedesArrangementId, x.SupersededByArrangementId,
                x.SourcePlayerText,
                x.SourceNpcText,
                source_is_excerpt = false
            }),
            pending_offers = offers,
            history_selection = new { omitted_documents = documents.Count - selected.Count,
                omitted_completed_commitments = arrangements.Count - pending.Count - completed.Count,
                omitted_completed_offers = round.PendingOffers.Count - offers.Count }
        });
    }

}
