using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyScopedWorldHost
{
    string CaptureScopedEventWorld(IReadOnlyCollection<string> roots, IReadOnlyCollection<string> candidates, bool discover);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private static bool ClaimLegacyWorldUpgrade(WorldDiplomacyJob job)
    {
        try
        {
            var rows = JArray.Parse(job.EventMaterialSnapshot ?? "[]");
            var world = rows.OfType<JObject>().FirstOrDefault(x => (string)x["key"] == "world:current"
                && (string)x["source_kind"] == "current_world");
            if (world == null || (int?)world["query_version"] == 1 || (bool?)world["query_upgrade_attempted"] == true) return false;
            if (!JArray.Parse((string)world["text"]).OfType<JObject>().Any()) return false;
            world["query_upgrade_attempted"] = true;
            job.EventMaterialSnapshot = rows.ToString(Formatting.None);
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static bool HasScopedWorldSnapshot(WorldDiplomacyJob job)
    {
        try { return JArray.Parse(job.EventMaterialSnapshot ?? "[]").OfType<JObject>()
            .Any(x => (string)x["key"] == "world:current" && (int?)x["query_version"] == 1); }
        catch (JsonException) { return false; }
    }

    private bool TrimPeripheralWorld(JObject request)
    {
        var source = (request["sources"] as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["key"] == "world:current");
        if ((int?)source?["query_version"] != 1 || (bool?)source["query_detail"] == true
            || source["query_roots"] is not JArray roots) return false;
        JArray world;
        try { world = JArray.Parse((string)source["text"]); } catch (JsonException) { return false; }
        var keep = new HashSet<string>(roots.Values<string>(), StringComparer.Ordinal);
        int before = world.Count;
        foreach (var row in world.OfType<JObject>().ToList())
        {
            if (!keep.Contains((string)row["kingdom_id"])) { row.Remove(); continue; }
            if (row["ruler_relations"] is JArray relations)
                foreach (var relation in relations.OfType<JObject>().Where(x => !keep.Contains((string)x["kingdom"])).ToList()) relation.Remove();
        }
        if (world.Count == before) return false;
        source["text"] = world.ToString(Formatting.None);
        source["version"] = MaterialHash((string)source["text"]);
        source["peripheral_omitted"] = true;
        return true;
    }

    private JObject CapturePlanningWorld(IEnumerable<string> roots, List<string> candidates, bool detailed)
    {
        var selected = roots.Where(candidates.Contains).Distinct(StringComparer.Ordinal).ToList();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string raw = _host is IDiplomacyScopedWorldHost scoped
            ? scoped.CaptureScopedEventWorld(selected, candidates, !detailed)
            : EventHost.CaptureEventWorld(selected);
        string text = CompactPlanningWorld(raw, 6000);
        var result = new JObject { ["key"] = "world:current", ["known_by"] = "public",
            ["source_kind"] = "current_world", ["text"] = text, ["excerpt"] = false,
            ["query_version"] = 1, ["query_roots"] = new JArray(selected),
            ["query_detail"] = detailed, ["scope_incomplete"] = true,
            ["version"] = MaterialHash(text),
            ["hero_ids"] = new JArray(selected.Select(_host.PartyRulerId).Where(x => !string.IsNullOrEmpty(x))) };
        // Include discovered rulers in the same evidence/actor binding.
        int countryCount = 0, relationCount = 0;
        try
        {
            var worldRows = JArray.Parse(text).OfType<JObject>().ToList();
            countryCount = worldRows.Count;
            relationCount = worldRows.Sum(x => (x["ruler_relations"] as JArray)?.Count ?? 0);
            foreach (var row in worldRows)
                if (!string.IsNullOrEmpty((string)row["ruler_id"]) && !((JArray)result["hero_ids"]).Values<string>().Contains((string)row["ruler_id"]))
                    ((JArray)result["hero_ids"]).Add((string)row["ruler_id"]);
        }
        catch (JsonException) { /* Legacy hosts retain their original payload. */ }
        _host.Log("diplomacy.world_query roots=" + selected.Count + " detailed=" + detailed
            + " countries=" + countryCount + " relations=" + relationCount
            + " estimatedTokens=" + _host.EstimateTokens(text) + " elapsedMs=" + watch.ElapsedMilliseconds);
        return result;
    }

    private JArray RefreshPlanningWorld(WorldDiplomacyJob job, WorldDiplomacyRound round, List<string> candidates,
        JArray material, bool detailed)
    {
        var old = material.OfType<JObject>().FirstOrDefault(x => (string)x["key"] == "world:current");
        List<string> roots;
        if (detailed)
        {
            roots = ReadAdmission(job.CountryAdmission).Admitted.ToList();
            if (WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.EventSelectedCandidate, out var selected)
                && selected["kingdoms"] is JArray countries) roots.AddRange(countries.Values<string>());
        }
        else roots = old?["query_roots"] is JArray saved ? saved.Values<string>().ToList()
            : round.Participants.Select(x => x.KingdomId).Concat(job.EventInvestigationKingdoms)
                .Concat(material.OfType<JObject>().Select(x => (string)x["known_by"]).Where(candidates.Contains))
                .Distinct(StringComparer.Ordinal).Take(6).ToList();
        roots = roots.Where(candidates.Contains).Distinct(StringComparer.Ordinal).ToList();
        if (roots.Count == 0) roots.AddRange(candidates.Take(6));
        if ((int?)old?["query_version"] == 1 && (bool?)old["query_detail"] == detailed
            && old["query_roots"] is JArray prior && prior.Values<string>().SequenceEqual(roots)) return material;
        var replacement = CapturePlanningWorld(roots, candidates, detailed);
        if (old == null) material.Insert(0, replacement); else old.Replace(replacement);
        job.EventMaterialSnapshot = material.ToString(Formatting.None);
        return material;
    }
}
