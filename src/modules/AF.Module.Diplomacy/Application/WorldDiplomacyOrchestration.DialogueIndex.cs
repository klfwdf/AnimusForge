using System;
using System.Collections.Generic;
using System.Linq;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Once per analysis request; only live offers, never the full document archive.
    internal IEnumerable<WorldDiplomacyRoundOffer> PlayerAnalysisOffers(string actor)
        => GetLiveRounds().Where(IsLiveRound).SelectMany(x => x.PendingOffers ?? Enumerable.Empty<WorldDiplomacyRoundOffer>())
            .Where(x => x != null && x.Status == "open" && (x.TargetKingdomId == actor || x.ProposerKingdomId == actor)
                && DialogueDocumentKnown(actor, x.SourceDocumentId));
    private bool _dialogueIndexDirty = true;
    private readonly Dictionary<string, WorldDiplomacyRound> _dialogueRoundsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<KeyValuePair<WorldDiplomacyRound, WorldDiplomacyRoundOffer>>> _dialogueOffersByPair = new(StringComparer.OrdinalIgnoreCase);
    private void InvalidateDialogueIndex() => _dialogueIndexDirty = true;
    private static string DialogueOfferKey(string responder, string proposer, string intent) => responder + "|" + proposer + "|" + intent;
    private void EnsureDialogueIndex()
    {
        if (!_dialogueIndexDirty) return;
        _dialogueRoundsById.Clear(); _dialogueOffersByPair.Clear();
        foreach (var round in (Storage?.CompletedRounds ?? Enumerable.Empty<WorldDiplomacyRound>()).Concat(GetLiveRounds()))
        {
            if (round == null || string.IsNullOrEmpty(round.RoundId)) continue;
            _dialogueRoundsById[round.RoundId] = round;
            if (!IsLiveRound(round)) continue;
            foreach (var offer in round.PendingOffers ?? Enumerable.Empty<WorldDiplomacyRoundOffer>())
            {
                if (offer == null) continue;
                string key = DialogueOfferKey(offer.TargetKingdomId, offer.ProposerKingdomId, NormalizeIntent(offer.Intent));
                if (!_dialogueOffersByPair.TryGetValue(key, out var entries)) _dialogueOffersByPair[key] = entries = new();
                entries.Add(new(round, offer));
            }
        }
        _dialogueIndexDirty = false;
    }
    private WorldDiplomacyRound FindIndexedDialogueRound(string roundId)
    {
        if (string.IsNullOrWhiteSpace(roundId)) return null;
        EnsureDialogueIndex();
        return _dialogueRoundsById.TryGetValue(roundId, out var round) ? round : null;
    }
    private IEnumerable<KeyValuePair<WorldDiplomacyRound, WorldDiplomacyRoundOffer>> FindIndexedDialogueOffers(string responder, string proposer, string intent)
    {
        EnsureDialogueIndex();
        return _dialogueOffersByPair.TryGetValue(DialogueOfferKey(responder, proposer, intent), out var offers)
            ? offers : Enumerable.Empty<KeyValuePair<WorldDiplomacyRound, WorldDiplomacyRoundOffer>>();
    }
}
