using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyMaterialWorld
{
    IReadOnlyList<string> OwnedSettlementIds(string kingdom);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    // Only discrete public facts are compared. Time, wording, money drift and
    // unrelated kingdoms' relations cannot revive a refused aid request.
    private string MaterialEvidence(IEnumerable<string> kingdoms)
    {
        var all = _host.AllKingdomIds().Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return JsonConvert.SerializeObject(kingdoms.Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x, StringComparer.Ordinal).Select(id => new {
            kingdom = id, ruler = _host.PartyRulerId(id), eliminated = _host.IsEliminatedParty(id),
            independent = _host.HasIndependentAuthority(id), representative = _host.ResolveRepresentativeId(id),
            wars = all.Where(x => x != id && _host.PartiesAtWar(id, x)).ToArray(),
            allies = all.Where(x => x != id && _host.PartiesAllied(id, x)).ToArray(),
            trade = all.Where(x => x != id && _host.TradeAgreementExists(id, x)).ToArray(),
            settlements = (_host as IDiplomacyMaterialWorld)?.OwnedSettlementIds(id)?.OrderBy(x => x, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>()
        }));
    }
    private static string MaterialHash(string evidence) => WorldDiplomacyPromptContractRules.StablePromptHash(evidence);
    private string AidMaterialFingerprint(string caller, string called, string enemy) => MaterialHash(MaterialEvidence(new[] { caller, called, enemy }));
    private string SpeechConfiguration() => (_host as IDiplomacyModelConfiguration)?.ConfigurationFingerprint() ?? "";
    private IEnumerable<string> SpeechWatchedKingdoms(DiplomacySpeechReview item) =>
        (item.WatchedKingdoms ?? new()).Concat(new[] { item.Actor, item.PlayerKingdom }).Where(x => !string.IsNullOrEmpty(x)).Distinct();
    private void HoldSpeechReview(DiplomacySpeechReview item, string reason)
    {
        item.Status = "conditional"; item.Reason = reason;
        item.WorldFingerprint = MaterialHash(MaterialEvidence(SpeechWatchedKingdoms(item)));
    }
    private void ResumeSpeechReview(DiplomacySpeechReview item, string reason)
    {
        item.Status = "pending"; item.Attempts = 0; item.RetryDay = _host.CurrentDay(); item.Reason = reason;
        item.ConfigurationFingerprint = SpeechConfiguration(); item.ServiceRecoveryEpoch = Storage.SpeechServiceRecoveryEpoch;
        item.RecoverOnService = false;
    }
    private void ObserveDiplomacyService(LlmJobResult result)
    {
        if (!result.Success && result.IsServiceFailure) Storage.SpeechServiceOutage = true;
        else if (result.Success && Storage.SpeechServiceOutage)
        { Storage.SpeechServiceOutage = false; Storage.SpeechServiceRecoveryEpoch++; }
    }
    private void RefreshHeldSpeechReview(DiplomacySpeechReview item)
    {
        if (item.Status == "conditional")
        {
            string current = MaterialHash(MaterialEvidence(SpeechWatchedKingdoms(item)));
            if (string.IsNullOrEmpty(item.WorldFingerprint)) item.WorldFingerprint = current;
            else if (item.WorldFingerprint != current)
            { item.WorldFingerprint = current; ResumeSpeechReview(item, "relevant_world_state_changed"); }
        }
        else if (item.Status == "technical_hold")
        {
            string configuration = SpeechConfiguration();
            bool changed = !string.IsNullOrEmpty(item.ConfigurationFingerprint) && !string.IsNullOrEmpty(configuration)
                && configuration != item.ConfigurationFingerprint;
            if (changed || (item.RecoverOnService && item.ServiceRecoveryEpoch < Storage.SpeechServiceRecoveryEpoch))
                ResumeSpeechReview(item, changed ? "model_configuration_changed" : "service_recovered");
            else if (string.IsNullOrEmpty(item.ConfigurationFingerprint)) item.ConfigurationFingerprint = configuration;
        }
    }
}
