using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private readonly WorldDiplomacyMaterialCaptureQueue _eventMaterialCaptures = new();
    private readonly WorldDiplomacyAsyncEmbeddingCache _historyEmbeddings = new();

    private sealed partial class OrchestrationHost : IDiplomacyEventHost, IDiplomacyEventCalendarHost, IDiplomacyEventMaterialHost, IDiplomacyWorldBookHost, IDiplomacyEventCultureHost, IDiplomacySilenceEffectHost, IDiplomacyPlayerContactHost, IDiplomacyMaterialVersionHost, IDiplomacyNarrativeActorHost, IWorldDiplomacyRealmVoicePreparation
    {
        public (string state, int count, string revision) ReadWorldBookAvailability() =>
            Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>()?.ReadWorldBookAvailability() ?? ("unavailable", 0, "");
        public bool PrepareRealmVoice(string kingdomId)
        {
            _owner.BuildRealmInstitutionalVoiceContext(ResolveKingdom(kingdomId), true, out bool ready);
            return ready;
        }
        public string FormatEventDate(int day) => DiplomacyUiLocalization.CampaignDate(day, CampaignTime.DaysInSeason, CampaignTime.DaysInYear);
        public IReadOnlyDictionary<string,string> CaptureNarrativeActors(IReadOnlyCollection<string> kingdoms) =>
            kingdoms.Select(ResolveKingdom).Where(k => k != null).SelectMany(k => k.Clans)
                .Where(c => !c.IsEliminated).SelectMany(c => c.Heroes).Where(h => h.IsAlive)
                .GroupBy(h => h.StringId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Name.ToString(), StringComparer.Ordinal);
        public string CaptureMaterialVersion(string kingdomId)
        {
            var kingdom = ResolveKingdom(kingdomId);
            // Cold request boundary: lore revision and live identities govern
            // retrieval validity. Daily town floats belong to the fresh world
            // snapshot, not the revision of immutable lore/persona evidence.
            return AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules.StablePromptHash(
                (KnowledgeLibraryBehavior.Instance?.GetRuleDataVersionForExternal() ?? 0) + ":" + kingdomId
                + ":" + kingdom?.IsEliminated + ":" + kingdom?.Leader?.StringId
                + ":" + string.Join("|", kingdom?.Clans.Where(c => !c.IsEliminated).SelectMany(c => c.Heroes)
                    .Where(h => h.IsAlive && h.IsLord).Select(h => h.StringId + ":" + h.Clan.StringId)
                    .OrderBy(x => x, StringComparer.Ordinal) ?? Enumerable.Empty<string>()));
        }
        public int PlayerContactIntervalDays() => Math.Max(1, Math.Min(30, DuelSettings.Instance?.WorldDiplomacyPlayerContactIntervalDays ?? 5));
        public string CaptureEventSources(IReadOnlyCollection<string> kingdoms, IReadOnlyCollection<string> excludedKeys, int searchPass = 0, string topic = "")
        {
            string key = JsonConvert.SerializeObject(new { kingdoms, excludedKeys, searchPass, topic });
            return _owner._eventMaterialCaptures.Advance(key, EventGeneration,
                token => CaptureEventSourcesSteps(kingdoms.ToArray(), excludedKeys.ToArray(), searchPass, topic, token).GetEnumerator(), Log);
        }
        private IEnumerable<string> CaptureEventSourcesSteps(IReadOnlyCollection<string> kingdoms, IReadOnlyCollection<string> excludedKeys, int searchPass, string topic, CancellationToken cancellation)
        {
            var result = new List<object>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var library = KnowledgeLibraryBehavior.Instance;
            long captureVersion = library?.GetRuleDataVersionForExternal() ?? 0;
            bool themed = WorldDiplomacyOrchestration.IsThematicResearchTopic(topic);
            var identityChecks = new List<Func<bool>>();
            foreach (var kingdom in kingdoms.Select(ResolveKingdom).Where(x => x?.Leader?.IsAlive == true))
            {
                var ruler = kingdom.Leader;
                string identity = kingdom.Name + " " + ruler.Name;
                // Cold request snapshot. Rotate bounded ordinary-lord samples; identities
                // are resolved from the campaign, never from world-book historical names.
                var lords = kingdom.Clans.Where(c => !c.IsEliminated).SelectMany(c => c.Heroes)
                    .Where(h => h.IsAlive && h.IsLord && h != ruler && h != Hero.MainHero)
                    .OrderBy(h => h.StringId, StringComparer.Ordinal).ToList();
                int start = lords.Count == 0 ? 0 : Math.Abs((CurrentDay() + Math.Min(1000000, Math.Max(0, searchPass)) * 3) % lords.Count);
                var selected = Enumerable.Range(0, Math.Min(searchPass > 0 ? 6 : 3, lords.Count)).Select(i => lords[(start + i) % lords.Count]).ToList();
                identityChecks.Add(() => !kingdom.IsEliminated && kingdom.Leader == ruler && ruler.IsAlive
                    && selected.All(h => h.IsAlive && h.Clan?.Kingdom == kingdom));
                var themes = DiplomacyPromptTexts.Get("EventMaterialQueries", "").Split('|');
                var queries = DiplomacyMaterialCaptureRules.Queries(topic, themed, themes, searchPass);
                for (int queryIndex = 0; queryIndex < queries.Count; queryIndex++)
                {
                    string cue = queries[queryIndex];
                    if (library == null) continue;
                    var entities = new MentionedWorldEntities();
                    // The retriever consumes these terms, not inputText. A theme is an
                    // actual semantic query, ahead of exact realm/ruler slots.
                    if (!string.IsNullOrWhiteSpace(cue)) entities.Entities.Add(kingdom.Name + " " + cue);
                    else
                    {
                        entities.Entities.Add(kingdom.Name.ToString()); entities.Entities.Add(ruler.Name.ToString());
                        foreach (var lord in selected) entities.Entities.Add(lord.Name.ToString());
                    }
                    cancellation.ThrowIfCancellationRequested();
                    // Only strings/settings/rule snapshots cross to the worker.
                    var work = library.StartExternalLoreSelection(entities, cancellation, out long version);
                    while (!work.IsCompleted) yield return null;
                    var candidates = work.GetAwaiter().GetResult(); // completed, never a blocking wait
                    if (candidates == null || !ReferenceEquals(library, KnowledgeLibraryBehavior.Instance)
                        || library.GetRuleDataVersionForExternal() != version
                        || !ruler.IsAlive || kingdom.Leader != ruler)
                        throw new InvalidOperationException("event_material_stale_capture");
                    if (queryIndex == 0 && themed)
                    {
                        // Resolve retrieved character entries by exact, unambiguous
                        // live names. Never turn historical prose mentions into IDs.
                        var liveNames = lords.GroupBy(h => h.Name.ToString(), StringComparer.Ordinal)
                            .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.Single(), StringComparer.Ordinal);
                        var related = (candidates.OrderedRules ?? new()).Take(Math.Max(1, candidates.InjectLimit))
                            .SelectMany(r => r.Keywords ?? new()).Where(liveNames.ContainsKey)
                            .Select(name => liveNames[name]).Distinct().Take(3).ToList();
                        var kin = related.SelectMany(h => new[] { h.Father, h.Mother, h.Spouse })
                            .Where(h => h != null && h.IsAlive && h.IsLord && h.Clan?.Kingdom == kingdom && h != Hero.MainHero && h != ruler)
                            .Distinct().Take(2);
                        selected = related.Concat(kin).Concat(selected).Distinct().Take(6).ToList();
                        if (related.Count > 0)
                            queries.Insert(1, string.Join(" ", related.Select(h => h.Name.ToString())) + " " + topic);
                    }
                    var loreRecords = library.BuildExternalLoreRecordsWithCandidates(identity + " " + cue, ruler, entities, candidates);
                    yield return null; // at most one query's live rendering in a scheduler step
                    foreach (string lore in loreRecords)
                    {
                        if (string.IsNullOrWhiteSpace(lore)) continue;
                        string key = "lore:" + AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules.StablePromptHash(lore);
                        if (!seen.Add(key)) continue;
                        result.Add(new { key, known_by = kingdom.StringId, source_kind = "world_book_account",
                            previously_used = excludedKeys.Contains(key), query = cue, topic_query = !string.IsNullOrWhiteSpace(topic) && cue == topic, text = lore,
                            hero_ids = selected.Select(h => h.StringId).Append(ruler.StringId).ToList() });
                    }
                }
                foreach (var person in selected.Prepend(ruler))
                    result.Add(new { key = "persona:" + person.StringId, known_by = kingdom.StringId,
                        source_kind = "character_setting", hero_ids = new[] { person.StringId },
                        text = JsonConvert.SerializeObject(new { hero_id = person.StringId, name = person.Name.ToString(),
                            traits = BuildRulerVoiceTraitSummary(person),
                            persona = MyBehavior.GetNpcPersonaDescriptionForExternal(person),
                            context = MyBehavior.GetNpcPersonaContextForExternal(person) }) });
                foreach (var lord in selected)
                    result.Add(new { key = "character:" + lord.StringId, known_by = kingdom.StringId,
                        source_kind = "current_character_relation", hero_ids = new[] { lord.StringId, ruler.StringId },
                        text = JsonConvert.SerializeObject(new { hero_id = lord.StringId, name = lord.Name.ToString(),
                            kingdom_id = kingdom.StringId, clan_id = lord.Clan?.StringId, ruler_id = ruler.StringId,
                            relation_to_ruler = lord.GetRelation(ruler), father_id = lord.Father?.StringId,
                            mother_id = lord.Mother?.StringId, spouse_id = lord.Spouse?.StringId,
                            related_people = selected.Where(other => other != lord).Select(other => new {
                                hero_id = other.StringId, relation = lord.GetRelation(other) }) }) });
            }
            cancellation.ThrowIfCancellationRequested();
            if (!ReferenceEquals(library, KnowledgeLibraryBehavior.Instance)
                || (library?.GetRuleDataVersionForExternal() ?? 0) != captureVersion || identityChecks.Any(check => !check()))
                throw new InvalidOperationException("event_material_stale_capture");
            yield return JsonConvert.SerializeObject(result);
        }
        public IReadOnlyList<string> KingdomTownIds(string kingdomId) => ResolveKingdom(kingdomId)?.Fiefs
            .Where(x => x.IsTown).Select(x => x.Settlement.StringId).ToList() ?? new List<string>();
        public string EventCulture(string kingdomId) => ResolveKingdom(kingdomId)?.Culture?.StringId ?? "";
        public long EventGeneration => SaveRuntimeGuard.CaptureGeneration();
        public string CaptureEventWorld(IReadOnlyCollection<string> participants)
        {
            _owner.EnsureKingdomBorderCache();
            return JsonConvert.SerializeObject(
            participants.Select(ResolveKingdom).Where(x => x != null && !x.IsEliminated).Select(k => new {
                kingdom_id = k.StringId, name = k.Name.ToString(), ruler_id = k.Leader?.StringId, ruler = k.Leader?.Name.ToString(),
                enemies = k.FactionsAtWarWith.OfType<Kingdom>().Select(x => x.StringId),
                ruler_relations = participants.Where(id => id != k.StringId).Select(ResolveKingdom)
                    .Where(other => other?.Leader?.IsAlive == true && k.Leader?.IsAlive == true)
                    .Select(other => new { kingdom = other.StringId, value = k.Leader.GetRelation(other.Leader), minimum = -100, maximum = 100 }),
                geography_basis = "current_campaign_gate_positions_proximity_not_terrain_or_route",
                nearby_foreign_radius = _owner._kingdomBorderDistanceThreshold,
                towns = k.Fiefs.Where(x => x.IsTown).OrderBy(x => x.StringId, StringComparer.Ordinal).Take(8).Select(t => new {
                    id = t.Settlement.StringId, name = t.Name.ToString(), loyalty = t.Loyalty, security = t.Security, prosperity = t.Prosperity, food = t.FoodStocks,
                    loyalty_minimum = 0, loyalty_maximum = 100, security_minimum = 0, security_maximum = 100,
                    prosperity_minimum = 0, food_minimum = 0, food_maximum = t.FoodStocksUpperLimit(),
                    map_x = t.Settlement.GatePosition.ToVec2().X, map_y = t.Settlement.GatePosition.ToVec2().Y,
                    nearby_foreign_forts = _owner._fortGeography.TryGetValue(t.Settlement.StringId, out var nearby) ? nearby : null })
            }));
        }

        private static Town EffectTown(DiplomacyEventEffectState effect)
        {
            var settlement = ResolveSettlementById(effect.SettlementId);
            return settlement?.IsTown == true && settlement.OwnerClan?.Kingdom?.StringId == effect.KingdomId ? settlement.Town : null;
        }
        private static bool EffectRulers(DiplomacyEventEffectState effect, out Hero first, out Hero second)
        {
            first = ResolveKingdom(effect.KingdomId)?.Leader; second = ResolveKingdom(effect.OtherKingdomId)?.Leader;
            return first?.IsAlive == true && second?.IsAlive == true && first != second
                && first.StringId == effect.RulerId && second.StringId == effect.OtherRulerId;
        }
        public bool TryReadEventEffect(DiplomacyEventEffectState effect, out float value, out float minimum, out float maximum)
        {
            value = minimum = 0; maximum = float.MaxValue;
            if (effect.Kind == "ruler_relation")
            {
                if (!EffectRulers(effect, out var first, out var second)) return false;
                value = first.GetRelation(second); minimum = -100; maximum = 100; return true;
            }
            var town = EffectTown(effect); if (town == null) return false;
            switch (effect.Kind)
            {
                case "town_loyalty": value = town.Loyalty; maximum = 100; return true;
                case "town_security": value = town.Security; maximum = 100; return true;
                case "town_prosperity": value = town.Prosperity; return true;
                case "town_food": value = town.FoodStocks; maximum = town.FoodStocksUpperLimit(); return true;
                default: return false;
            }
        }
        public void ApplyEventEffectDelta(DiplomacyEventEffectState effect, float delta)
        {
            if (effect.Kind == "ruler_relation")
            {
                if (!EffectRulers(effect, out var first, out var second)) throw new InvalidOperationException("event_rulers_changed");
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(first, second, (int)delta, false); return;
            }
            var town = EffectTown(effect); if (town == null) throw new InvalidOperationException("event_settlement_changed");
            switch (effect.Kind)
            {
                case "town_loyalty": town.Loyalty += delta; break;
                case "town_security": town.Security += delta; break;
                case "town_prosperity": town.Prosperity += delta; break;
                case "town_food": town.FoodStocks += delta; break;
                default: throw new InvalidOperationException("event_effect_unsupported");
            }
        }
        public bool ShowEvent(DiplomacyEventPresentation data, out string imageId, Action acknowledged, IReadOnlyList<string> recentImages)
            => DiplomacyEventPresentationBridge.TryShow(data, out imageId, acknowledged, recentImages);
    }
}
