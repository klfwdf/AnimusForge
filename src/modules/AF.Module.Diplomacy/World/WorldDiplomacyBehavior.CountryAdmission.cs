using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed partial class OrchestrationHost : IDiplomacyCountryAdmissionHost, IDiplomacyAdmissionIdentityHost
    {
        public int AdmissionCountryCountPreference => DuelSettings.Instance?.WorldDiplomacyActivityDropdown?.SelectedIndex ?? 1;
        public DiplomacyAdmissionIdentity ReadAdmissionIdentity(string kind, string id, string expectedCountry)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id)) return default;
                if (kind == "hero")
                {
                    // Exact lookup includes dead registered heroes. A lookup miss
                    // stays unknown; no fallback scan of AllAliveHeroes.
                    var hero = DiplomacyIdentityResolver.Hero(id);
                    if (hero == null) return DiplomacyAdmissionIdentity.Hero(false, false, false, null, expectedCountry);
                    if (hero.IsDead) return DiplomacyAdmissionIdentity.Hero(true, true, false, null, expectedCountry);
                    return DiplomacyAdmissionIdentity.Hero(true, false, hero.IsAlive, hero.Clan?.Kingdom?.StringId, expectedCountry);
                }
                if (kind == "kingdom")
                {
                    var kingdom = DiplomacyIdentityResolver.Kingdom(id);
                    return DiplomacyAdmissionIdentity.Kingdom(kingdom != null, kingdom?.IsEliminated == true, id);
                }
            }
            catch (Exception ex) { return new(DiplomacyAdmissionIdentityState.Unknown, reason: "identity_read_failed:" + ex.GetType().Name); }
            return default;
        }

        public string AdmissionSubjectCountry(string kind, string id)
        {
            if (kind == "kingdom") return ResolveKingdom(id)?.StringId;
            if (kind == "hero")
            {
                var identity = ReadAdmissionIdentity(kind, id, null);
                return identity.State == DiplomacyAdmissionIdentityState.Valid ? identity.Country : null;
            }
            if (kind == "settlement") return Settlement.Find(id)?.OwnerClan?.Kingdom?.StringId;
            return null;
        }

        public DiplomacyAdmissionInput CaptureCountryAdmission(IReadOnlyCollection<string> direct,
            IReadOnlyCollection<string> heroes, IReadOnlyCollection<string> candidates)
        {
            var input = new DiplomacyAdmissionInput { Direct = direct.Distinct(StringComparer.Ordinal).ToList(),
                Candidates = candidates.Distinct(StringComparer.Ordinal).ToList() };
            var origins = input.Direct.Select(id => ResolveKingdom(id) ?? throw new InvalidOperationException("admission_origin_missing:" + id)).ToList();
            var core = heroes.Distinct(StringComparer.Ordinal).Select(id => DiplomacyIdentityResolver.Hero(id)
                ?? throw new InvalidOperationException("admission_hero_missing:" + id)).ToList();
            foreach (var hero in core)
            {
                string country = hero.IsAlive ? hero.Clan?.Kingdom?.StringId : null;
                if (country == null || !input.Direct.Contains(country)) throw new InvalidOperationException("admission_hero_owner_changed:" + hero.StringId);
                input.Heroes.Add(hero.StringId, country);
            }
            // This host is called by main-thread completion once per seed.
            // Border work reuses the existing daily cache. Relations are read
            // once per actual unordered hero pair, never via clan averages.
            var relations = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string id in candidates.Distinct(StringComparer.Ordinal).Where(x => !input.Direct.Contains(x)))
            {
                var country = ResolveKingdom(id) ?? throw new InvalidOperationException("admission_candidate_missing:" + id);
                foreach (var origin in origins)
                {
                    var pair = new DiplomacyAdmissionPair { Country = id, Origin = origin.StringId,
                        Ruler = country.Leader?.StringId, OriginRuler = origin.Leader?.StringId,
                        Border = _owner.GetKingdomBorderRelation(country, origin).SharesBorder,
                        War = FactionManager.IsAtWarAgainstFaction(country, origin) };
                    if (country.Leader != null)
                        foreach (var other in core.Where(h => h.Clan?.Kingdom == origin).Concat(new[] { origin.Leader })
                            .Where(h => h != null && h != country.Leader).Distinct())
                        {
                            string key = string.CompareOrdinal(country.Leader.StringId, other.StringId) < 0
                                ? country.Leader.StringId + ":" + other.StringId : other.StringId + ":" + country.Leader.StringId;
                            if (!relations.TryGetValue(key, out int value))
                            {
                                // Both supported Hero.GetRelation APIs return integer points,
                                // with native -100..100 limits (not normalized -1..1 floats).
                                value = country.Leader.GetRelation(other);
                                relations.Add(key, value);
                            }
                            pair.Relations.Add(other.StringId, value);
                        }
                    input.Pairs.Add(pair);
                }
            }
            return input;
        }
    }
}
