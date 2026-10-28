using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Cross-action exchanges lack a reversible multi-effect transaction. Preserve them
// as unexecuted negotiations; independent actions and the existing atomic peace
// cession/tribute action still use the single authoritative executor.
internal static class DiplomacyConditionalExecutionRules
{
    internal static bool ValidateImmediateWar(JArray actions, out string reason)
    {
        reason = "";
        if (actions?.OfType<JObject>().Any(x => (string)x["intent"] == "declare_war"
            && x["dependency_action_ids"] is JArray dependencies && dependencies.Count > 0) != true) return true;
        reason = "conditional_war_cannot_be_published_as_executed;use_a_conditional_proposal_or_threat_for_real_prerequisites;use_response_binding_for_prior_ultimatum";
        return false;
    }

    // Admission boundary only: at most 64 actions. Never discard a dependency
    // to make an invalid draft executable; the writer must repair its meaning.
    internal static bool Validate(JArray actions, Func<string, WorldDiplomacyDocument> resolveDocument, out string reason)
    {
        reason = "";
        if (actions == null || actions.Count == 0 || actions.Count > 64)
        { reason = "dependency_actions_shape_invalid"; return false; }
        var edges = new List<int>[actions.Count];
        for (int i = 0; i < actions.Count; i++)
        {
            edges[i] = new();
            if (actions[i] is not JObject action)
            { reason = "dependency_action_object_required"; return false; }
            var token = action["dependency_action_ids"];
            if (token == null || token.Type == JTokenType.Null) continue;
            if (token is not JArray dependencies || dependencies.Count > 64)
            { reason = "dependency_list_invalid:action_" + (i + 1); return false; }
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in dependencies)
            {
                if (dependency.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)dependency)
                    || !seen.Add((string)dependency))
                { reason = "dependency_reference_invalid:action_" + (i + 1); return false; }
                string id = (string)dependency;
                int separator = id.IndexOf('#');
                if (separator >= 0)
                {
                    var source = resolveDocument?.Invoke(id.Substring(0, separator));
                    string sourceAction = id.Substring(separator + 1);
                    if (separator == 0 || source?.IsReadyForPublication != true
                        || source.Actions?.Count(x => x != null && string.Equals(x.ActionId, sourceAction, StringComparison.OrdinalIgnoreCase)) != 1)
                    { reason = "dependency_source_action_unavailable:" + id; return false; }
                    var original = source.Actions.Single(x => x != null && string.Equals(x.ActionId, sourceAction, StringComparison.OrdinalIgnoreCase));
                    if (original.Intent is "warning" or "ultimatum")
                    { reason = "dependency_threat_is_context_not_prerequisite:" + id
                        + ";use_responding_to_threat_document_id_and_action_id;preserve_real_conditions_in_a_conditional_proposal_not_an_immediate_action"; return false; }
                    continue;
                }
                int index = -1;
                for (int j = 0; j < actions.Count; j++)
                    if (string.Equals(id, "action_" + (j + 1), StringComparison.OrdinalIgnoreCase)) { index = j; break; }
                if (index < 0) { reason = "dependency_local_action_missing:" + id; return false; }
                if (index == i) { reason = "dependency_self_reference:" + id; return false; }
                edges[i].Add(index);
            }
        }
        var colors = new byte[actions.Count];
        bool Visit(int index)
        {
            if (colors[index] == 1) return false;
            if (colors[index] == 2) return true;
            colors[index] = 1;
            foreach (int next in edges[index]) if (!Visit(next)) return false;
            colors[index] = 2;
            return true;
        }
        for (int i = 0; i < actions.Count; i++)
            if (!Visit(i)) { reason = "dependency_cycle:action_" + (i + 1); return false; }
        return true;
    }

    internal static HashSet<string> DeferredActions(IReadOnlyList<WorldDiplomacyDocumentAction> actions,
        Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        var deferred = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (actions == null) return deferred;
        var local = actions.Where(x => x != null).ToDictionary(x => x.ActionId, StringComparer.OrdinalIgnoreCase);
        foreach (var action in local.Values)
        {
            var dependencies = action.DependencyActionIds ?? new();
            var source = string.IsNullOrEmpty(action.RespondingToOfferDocumentId) ? null : resolveDocument(action.RespondingToOfferDocumentId);
            var original = source?.Actions?.FirstOrDefault(x => x?.ActionId == action.RespondingToOfferActionId);
            bool sourceCoupled = original?.DependencyActionIds?.Count > 0 || source?.Actions?.Any(x =>
                x?.DependencyActionIds?.Contains(action.RespondingToOfferActionId, StringComparer.OrdinalIgnoreCase) == true) == true;
            if (dependencies.Count == 0 && !sourceCoupled) continue;
            deferred.Add(action.ActionId);
            foreach (string dependency in dependencies)
            {
                if (local.ContainsKey(dependency ?? "")) deferred.Add(dependency);
                else if (string.IsNullOrEmpty(dependency) || dependency.IndexOf('#') < 1)
                    deferred.UnionWith(local.Keys); // Unbound local graph cannot safely identify an independent effect.
            }
            if (sourceCoupled)
                foreach (var linked in local.Values.Where(x => x.RespondingToOfferDocumentId == action.RespondingToOfferDocumentId
                    && source.Actions.Any(y => y.ActionId == x.RespondingToOfferActionId && (y.DependencyActionIds?.Count > 0
                        || source.Actions.Any(z => z.DependencyActionIds?.Contains(y.ActionId, StringComparer.OrdinalIgnoreCase) == true)))))
                    deferred.Add(linked.ActionId);
        }
        return deferred;
    }
}
