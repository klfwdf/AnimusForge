using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Cross-action exchanges lack a reversible multi-effect transaction. Preserve them
// as unexecuted negotiations; independent actions and the existing atomic peace
// cession/tribute action still use the single authoritative executor.
internal static class DiplomacyConditionalExecutionRules
{
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
