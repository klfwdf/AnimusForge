using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.PolicyEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge.PolicyTargets;

internal static class PolicyTargetEntityKinds
{
	internal const string Kingdom = "kingdom";
	internal const string Clan = "clan";
	internal const string Ruler = "ruler";
	internal const string Settlement = "settlement";
}

internal sealed class PolicyTargetEntitySnapshot
{
	internal string DocumentId { get; set; }

	internal string Kind { get; set; }

	internal string EntityId { get; set; }

	internal string OwnerClanId { get; set; }

	internal string OwnerKingdomId { get; set; }

	internal string DisplayName { get; set; }

	internal string RetrievalText { get; set; }

	internal IReadOnlyList<string> MentionAliases { get; set; }

	internal bool IsCity { get; set; }

	internal bool IsCastle { get; set; }

	internal bool IsBorder { get; set; }

	internal bool HasPosition { get; set; }

	internal float PositionX { get; set; }

	internal float PositionY { get; set; }

	internal float Wealth { get; set; }

	internal float Influence { get; set; }

	internal float Strength { get; set; }

	internal float FiefCount { get; set; }

	internal float Food { get; set; }

	internal float Prosperity { get; set; }

	internal float Loyalty { get; set; }

	internal float Security { get; set; }

	internal float? Hearth { get; set; }

	internal float? Militia { get; set; }

	internal int CurrentSettlementCount { get; set; }
}

internal sealed class PolicyTargetKingdomSnapshot
{
	internal string KingdomId { get; set; }

	internal string DisplayName { get; set; }

	internal bool HasPosition { get; set; }

	internal float PositionX { get; set; }

	internal float PositionY { get; set; }
}

internal sealed class PolicyTargetWorldSnapshot
{
	internal IReadOnlyDictionary<string, PolicyEffectJurisdictionFief> JurisdictionFiefs { get; set; }

	internal long StableVersion { get; set; }

	internal long DynamicVersion { get; set; }

	internal IReadOnlyList<PolicyTargetEntitySnapshot> Entities { get; set; }

	internal IReadOnlyDictionary<string, PolicyTargetKingdomSnapshot> Kingdoms { get; set; }

	internal IReadOnlyCollection<string> WarPairs { get; set; }

	internal IReadOnlyCollection<string> AlliancePairs { get; set; }
}

internal sealed class PolicyTargetSemanticContext
{
	internal string QueryText { get; set; }

	internal string Scope { get; set; }

	internal string TargetKingdomId { get; set; }

	internal string IssuerKingdomId { get; set; }

	internal string PlayerClanId { get; set; }

	internal string ProposerClanId { get; set; }

	internal IReadOnlyCollection<string> SourceSettlementIds { get; set; }

	internal PolicyTargetWorldSnapshot Snapshot { get; set; }

	internal bool StrictEntityEvidence { get; set; }
}

internal static class PolicyTargetObjectiveEvidence
{
	private static readonly HashSet<string> GenericAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		PolicyCompatibilityTexts.Get("Targets_Compat_24c60e89e082"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097"), PolicyCompatibilityTexts.Get("Targets_Compat_0cc659a9f57c"), PolicyCompatibilityTexts.Get("Targets_Compat_f752e00f622d"), PolicyCompatibilityTexts.Get("Targets_Compat_2f293016a5ff"), PolicyCompatibilityTexts.Get("Targets_Compat_cec69ce03690"), PolicyCompatibilityTexts.Get("Targets_Compat_f1377277c151")
	};

	internal static IReadOnlyList<PolicyTargetEntitySnapshot> FindStrictMentionedEntities(
		IEnumerable<PolicyTargetEntitySnapshot> entities,
		string queryText,
		string kind)
	{
		string query = (queryText ?? string.Empty).Trim();
		if (query.Length == 0)
		{
			return Array.Empty<PolicyTargetEntitySnapshot>();
		}
		string compactQuery = CompactQualifiedName(query);
		List<PolicyTargetEntitySnapshot> candidates = (entities ?? Enumerable.Empty<PolicyTargetEntitySnapshot>())
			.Where(entity => entity != null
				&& string.Equals(entity.Kind, kind, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(entity.EntityId))
			.GroupBy(entity => entity.EntityId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		Dictionary<string, string[]> strictAliasesByEntityId = candidates.ToDictionary(
			entity => entity.EntityId,
			entity => EnumerateStrictAliases(entity).ToArray(),
			StringComparer.OrdinalIgnoreCase);
		Dictionary<string, int> aliasOwners = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (PolicyTargetEntitySnapshot entity in candidates)
		{
			foreach (string alias in strictAliasesByEntityId[entity.EntityId])
			{
				if (!GenericAliases.Contains(alias))
				{
					aliasOwners[alias] = aliasOwners.TryGetValue(alias, out int count) ? count + 1 : 1;
				}
			}
		}
		return candidates
			.Where(entity => MentionsIdentifier(query, entity.EntityId)
				|| strictAliasesByEntityId[entity.EntityId].Any(alias =>
					!GenericAliases.Contains(alias)
					&& aliasOwners.TryGetValue(alias, out int count)
					&& count == 1
					&& MentionsStrictAlias(query, compactQuery, alias)))
			.OrderBy(entity => entity.EntityId, StringComparer.Ordinal)
			.ToArray();
	}

	internal static bool MentionsIdentifier(string queryText, string identifier)
	{
		string query = queryText ?? string.Empty;
		string value = (identifier ?? string.Empty).Trim();
		if (query.Length == 0 || value.Length < 2)
		{
			return false;
		}
		for (int start = query.IndexOf(value, StringComparison.OrdinalIgnoreCase);
			start >= 0;
			start = query.IndexOf(value, start + value.Length, StringComparison.OrdinalIgnoreCase))
		{
			int end = start + value.Length;
			if ((start == 0 || !IsIdentifierCharacter(query[start - 1]))
				&& (end == query.Length || !IsIdentifierCharacter(query[end])))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool IsGenericAlias(string value)
	{
		return GenericAliases.Contains((value ?? string.Empty).Trim());
	}

	private static IEnumerable<string> EnumerateAliases(PolicyTargetEntitySnapshot entity)
	{
		return (entity?.MentionAliases ?? Array.Empty<string>())
			.Concat(new[] { entity?.DisplayName ?? string.Empty })
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length >= 2
				&& !string.Equals(value, entity?.EntityId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
	}

	private static IEnumerable<string> EnumerateStrictAliases(PolicyTargetEntitySnapshot entity)
	{
		string[] aliases = EnumerateAliases(entity)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		return aliases.Where(alias => !aliases.Any(candidate => IsGeneratedNameSuffix(candidate, alias)));
	}

	private static bool IsGeneratedNameSuffix(string qualifiedName, string alias)
	{
		string value = (qualifiedName ?? string.Empty).Trim();
		string candidate = (alias ?? string.Empty).Trim();
		int separator = Math.Max(value.LastIndexOf('·'), value.LastIndexOf('.'));
		return separator >= 0
			&& separator + 1 < value.Length
			&& string.Equals(value.Substring(separator + 1), candidate, StringComparison.OrdinalIgnoreCase);
	}

	private static bool MentionsStrictAlias(string query, string compactQuery, string alias)
	{
		if ((query ?? string.Empty).IndexOf(alias ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string value = alias ?? string.Empty;
		if (value.IndexOf('·') < 0 && value.IndexOf('.') < 0)
		{
			return false;
		}
		string compactAlias = CompactQualifiedName(value);
		return compactAlias.Length >= 2
			&& (compactQuery ?? string.Empty).IndexOf(compactAlias, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string CompactQualifiedName(string value)
	{
		return (value ?? string.Empty).Replace("·", string.Empty).Replace(".", string.Empty);
	}

	private static bool IsIdentifierCharacter(char value)
	{
		return value <= 127 && (char.IsLetterOrDigit(value) || value == '_');
	}
}

internal sealed class PolicyTargetSemanticRecallItem
{
	internal string Category { get; set; }

	internal string Id { get; set; }

	internal string Document { get; set; }

	internal float RecallScore { get; set; }

	internal PolicyTargetEntitySnapshot Entity { get; set; }

	internal PolicyTargetSemanticFacet Facet { get; set; }
}

internal sealed class PolicyTargetSemanticRecall
{
	internal PolicyTargetSemanticContext Context { get; set; }

	internal IReadOnlyList<PolicyTargetSemanticRecallItem> Items { get; set; }
}

internal sealed class PolicyTargetSemanticProposal
{
	internal string HandleKind { get; set; }

	internal string EntityId { get; set; }

	internal string OwnerKingdomId { get; set; }

	internal string DisplayName { get; set; }

	internal string Evidence { get; set; }

	internal float Score { get; set; }

	internal int CurrentSettlementCount { get; set; }
}

internal sealed class PolicyTargetSemanticRoutingResult
{
	internal bool ExpansionEnabled { get; set; }

	internal string EnablementReason { get; set; }

	internal IReadOnlyList<PolicyTargetSemanticProposal> ShadowProposals { get; set; }

	internal IReadOnlyList<PolicyTargetSemanticProposal> EnabledProposals { get; set; }

	internal string ShadowLogText { get; set; }
}

internal sealed class PolicyTargetSemanticFacet
{
	internal string Id { get; set; }

	internal string Group { get; set; }

	internal string RetrievalText { get; set; }

	internal bool SortDescending { get; set; }
}

internal static class PolicyTargetSemanticRouter
{
	internal const int EntityRecallLimit = 10;

	private const int ApproximateMentionPromotionLimit = 4;

	internal const int FacetRecallLimit = 8;

	internal const int LocalSemanticAdditionLimit = 4;

	internal const int LocalPerKindAdditionLimit = 3;

	internal const int KingdomSemanticAdditionLimit = 3;

	private const float ShadowFacetMargin = 0.1f;

	private const float DirectEntityMinimumRecallScoreGap = 0.03f;

	private const bool SemanticExpansionEnabled = true;

	private const string SemanticCalibrationReason = "embedding-calibrated-20260809";

	private static readonly IReadOnlyDictionary<string, float> CalibratedFacetThresholds = new Dictionary<string, float>(StringComparer.Ordinal)
	{
		["direction_east"] = 0.409850f,
		["direction_north"] = 0.431938f,
		["geography_border"] = 0.423639f,
		["metric_food_high"] = 0.464818f,
		["metric_strength_low"] = 0.431590f,
		["metric_wealth_high"] = 0.532520f,
		["relation_domestic"] = 0.467539f,
		["relation_enemy"] = 0.468700f,
		["type_city"] = 0.383388f,
		["type_clan"] = 0.517851f
	};

	private static readonly IReadOnlyDictionary<string, float> CalibratedEntityThresholds = new Dictionary<string, float>(StringComparer.Ordinal)
	{
		[PolicyTargetEntityKinds.Kingdom] = 0.569633f,
		[PolicyTargetEntityKinds.Clan] = 0.550144f,
		[PolicyTargetEntityKinds.Ruler] = 0.612518f,
		[PolicyTargetEntityKinds.Settlement] = 0.411177f
	};

	private static readonly object SnapshotSync = new object();

	private static readonly object IndexSync = new object();

	private static long _stableVersion = 1L;

	private static long _dynamicVersion = 1L;

	private static PolicyTargetWorldSnapshot _cachedSnapshot;

	private static PolicyTargetSemanticIndex _cachedIndex;

	private static readonly IReadOnlyList<PolicyTargetSemanticFacet> Facets = BuildFacets();

	private static readonly Lazy<IReadOnlyDictionary<string, float[]>> FacetVectors = new Lazy<IReadOnlyDictionary<string, float[]>>(BuildFacetVectors, true);

	static PolicyTargetSemanticRouter()
	{
		AssertInternalContracts();
	}

	internal static void MarkStructureDirty()
	{
		lock (SnapshotSync)
		{
			_stableVersion++;
			_dynamicVersion++;
		}
	}

	internal static void MarkDynamicDirty()
	{
		lock (SnapshotSync)
		{
			_dynamicVersion++;
		}
	}

	internal static PolicyTargetWorldSnapshot CaptureWorldSnapshot()
	{
		lock (SnapshotSync)
		{
			if (_cachedSnapshot != null
				&& _cachedSnapshot.StableVersion == _stableVersion
				&& _cachedSnapshot.DynamicVersion == _dynamicVersion)
			{
				return _cachedSnapshot;
			}
			_cachedSnapshot = BuildWorldSnapshot(_stableVersion, _dynamicVersion);
			return _cachedSnapshot;
		}
	}

	internal static PolicyTargetSemanticRecall Recall(float[] queryVector, PolicyTargetSemanticContext context)
	{
		if (queryVector == null || queryVector.Length <= 0)
		{
			throw new ArgumentException("Policy target query embedding is invalid.", nameof(queryVector));
		}
		if (context?.Snapshot?.Entities == null)
		{
			throw new InvalidOperationException("Policy target semantic snapshot is unavailable.");
		}
		PolicyTargetSemanticIndex index = GetOrBuildIndex(context.Snapshot);
		Dictionary<string, PolicyTargetEntitySnapshot> currentEntities = context.Snapshot.Entities
			.GroupBy(entity => entity.DocumentId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
		List<PolicyTargetSemanticRecallItem> allEntityItems = index.Items
			.Where(item => currentEntities.ContainsKey(item.DocumentId))
			.Select(item => new { Indexed = item, Entity = currentEntities[item.DocumentId] })
			.Where(item => IsEntityInScope(item.Entity, context))
			.Select(item => new PolicyTargetSemanticRecallItem
			{
				Category = "entity",
				Id = item.Indexed.DocumentId,
				Document = item.Entity.RetrievalText,
				RecallScore = Cosine(queryVector, item.Indexed.Vector),
				Entity = item.Entity
			})
			.ToList();
		string normalizedQuery = NormalizeMentionText(context.QueryText);
		List<PolicyTargetSemanticRecallItem> promotedEntityItems = allEntityItems
			.Where(item => PolicyTextApproximatelyMentionsEntityNormalized(normalizedQuery, item.Entity)
				&& EntityMatchesExplicitTypeCueNormalized(normalizedQuery, item.Entity))
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.Id, StringComparer.Ordinal)
			.Take(ApproximateMentionPromotionLimit)
			.ToList();
		HashSet<string> promotedIds = new HashSet<string>(promotedEntityItems.Select(item => item.Id), StringComparer.Ordinal);
		List<PolicyTargetSemanticRecallItem> entityItems = promotedEntityItems
			.Concat(allEntityItems
				.Where(item => !promotedIds.Contains(item.Id))
				.OrderByDescending(item => item.RecallScore)
				.ThenBy(item => item.Id, StringComparer.Ordinal))
			.Take(EntityRecallLimit)
			.ToList();
		IEnumerable<PolicyTargetSemanticRecallItem> orderedFacetItems = Facets
			.Select(facet => new PolicyTargetSemanticRecallItem
			{
				Category = "facet",
				Id = facet.Id,
				Document = facet.RetrievalText,
				RecallScore = Cosine(queryVector, FacetVectors.Value[facet.Id]),
				Facet = facet
			})
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.Id, StringComparer.Ordinal);
		List<PolicyTargetSemanticRecallItem> facetItems = TakeDiverseFacetRecall(orderedFacetItems);
		return new PolicyTargetSemanticRecall
		{
			Context = context,
			Items = entityItems.Concat(facetItems).ToArray()
		};
	}

	private static List<PolicyTargetSemanticRecallItem> TakeDiverseFacetRecall(IEnumerable<PolicyTargetSemanticRecallItem> orderedItems)
	{
		Dictionary<string, int> groupCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		List<PolicyTargetSemanticRecallItem> result = new List<PolicyTargetSemanticRecallItem>(FacetRecallLimit);
		foreach (PolicyTargetSemanticRecallItem item in orderedItems ?? Enumerable.Empty<PolicyTargetSemanticRecallItem>())
		{
			string group = item.Facet?.Group ?? "";
			groupCounts.TryGetValue(group, out int count);
			if (count >= 2)
			{
				continue;
			}
			result.Add(item);
			groupCounts[group] = count + 1;
			if (result.Count >= FacetRecallLimit)
			{
				break;
			}
		}
		return result;
	}

	internal static PolicyTargetSemanticRoutingResult Complete(
		PolicyTargetSemanticRecall recall)
	{
		if (recall?.Items == null)
		{
			throw new ArgumentException("Policy target semantic recall result is invalid.", nameof(recall));
		}
		List<ScoredRecallItem> scored = recall.Items
			.Select(item => new ScoredRecallItem { Item = item, Score = item.RecallScore })
			.ToList();
		List<PolicyTargetSemanticProposal> shadowProposals = BuildShadowProposals(recall.Context, scored);
		List<PolicyTargetSemanticProposal> enabledProposals = SemanticExpansionEnabled
			? BuildEnabledProposals(recall.Context, scored)
			: new List<PolicyTargetSemanticProposal>();
		return new PolicyTargetSemanticRoutingResult
		{
			ExpansionEnabled = SemanticExpansionEnabled,
			EnablementReason = SemanticCalibrationReason,
			ShadowProposals = shadowProposals,
			EnabledProposals = SemanticExpansionEnabled ? ApplyProposalCaps(enabledProposals, recall.Context.Scope) : Array.Empty<PolicyTargetSemanticProposal>(),
			ShadowLogText = BuildShadowLog(recall.Context, scored, enabledProposals)
		};
	}

	internal static bool IsSemanticTargetAllowed(
		PolicyTargetWorldSnapshot snapshot,
		string scope,
		string targetKingdomId,
		string issuerKingdomId,
		IReadOnlyCollection<string> sourceSettlementIds,
		string handleKind,
		string entityId,
		string ownerKingdomId)
	{
		if (snapshot?.Entities == null || string.IsNullOrWhiteSpace(entityId))
		{
			return false;
		}
		PolicyTargetSemanticContext context = new PolicyTargetSemanticContext
		{
			Scope = scope,
			TargetKingdomId = targetKingdomId,
			IssuerKingdomId = issuerKingdomId,
			SourceSettlementIds = sourceSettlementIds ?? Array.Empty<string>(),
			Snapshot = snapshot
		};
		if (string.Equals(handleKind, PolicyTargetEntityKinds.Kingdom, StringComparison.OrdinalIgnoreCase))
		{
			return snapshot.Kingdoms.ContainsKey(entityId)
				&& !string.Equals(entityId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(entityId, issuerKingdomId, StringComparison.OrdinalIgnoreCase);
		}
		return snapshot.Entities.Any(entity =>
			string.Equals(entity.EntityId, entityId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(entity.OwnerKingdomId, ownerKingdomId, StringComparison.OrdinalIgnoreCase)
			&& IsEntityInScope(entity, context)
			&& KindMatchesHandle(entity.Kind, handleKind));
	}

	private static PolicyTargetWorldSnapshot BuildWorldSnapshot(long stableVersion, long dynamicVersion)
	{
		List<Kingdom> kingdoms = (Kingdom.All ?? Enumerable.Empty<Kingdom>())
			.Where(kingdom => kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))
			.OrderBy(kingdom => kingdom.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		Dictionary<string, List<Settlement>> settlementsByKingdom = kingdoms.ToDictionary(
			kingdom => kingdom.StringId,
			kingdom => SafeGetKingdomSettlements(kingdom),
			StringComparer.OrdinalIgnoreCase);
		Dictionary<string, PolicyTargetKingdomSnapshot> kingdomSnapshots = new Dictionary<string, PolicyTargetKingdomSnapshot>(StringComparer.OrdinalIgnoreCase);
		List<PolicyTargetEntitySnapshot> entities = new List<PolicyTargetEntitySnapshot>();
		var fiefSnapshots = new Dictionary<string, PolicyEffectJurisdictionFief>(StringComparer.OrdinalIgnoreCase);
		foreach (Kingdom kingdom in kingdoms)
		{
			List<Settlement> settlements = settlementsByKingdom[kingdom.StringId];
			ReadCentroid(settlements, out bool hasKingdomPosition, out float kingdomX, out float kingdomY);
			string kingdomName = SafeText(kingdom.Name, kingdom.StringId);
			string cultureName = SafeText(kingdom.Culture?.Name, "");
			PolicyTargetKingdomSnapshot kingdomSnapshot = new PolicyTargetKingdomSnapshot
			{
				KingdomId = kingdom.StringId,
				DisplayName = kingdomName,
				HasPosition = hasKingdomPosition,
				PositionX = kingdomX,
				PositionY = kingdomY
			};
			kingdomSnapshots.Add(kingdom.StringId, kingdomSnapshot);
			List<Clan> clans = ((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>())
				.Where(clan => clan != null
					&& !clan.IsEliminated
					&& clan.Kingdom == kingdom
					&& !string.IsNullOrWhiteSpace(clan.StringId))
				.OrderBy(clan => clan.StringId, StringComparer.OrdinalIgnoreCase)
				.ToList();
			entities.Add(new PolicyTargetEntitySnapshot
			{
				DocumentId = "kingdom:" + kingdom.StringId,
				Kind = PolicyTargetEntityKinds.Kingdom,
				EntityId = kingdom.StringId,
				OwnerKingdomId = kingdom.StringId,
				DisplayName = kingdomName,
				RetrievalText = PolicyCompatibilityTexts.Get("Targets_Compat_2edeee0ab5d6") + kingdomName + " ID " + kingdom.StringId + PolicyCompatibilityTexts.Get("Targets_Compat_a1a73e16845f") + SafeText(kingdom.Leader?.Name, "") + PolicyCompatibilityTexts.Get("Targets_Compat_9d52c73ade76") + cultureName,
				MentionAliases = BuildMentionAliases(kingdomName, kingdom.StringId),
				HasPosition = hasKingdomPosition,
				PositionX = kingdomX,
				PositionY = kingdomY,
				Wealth = clans.Sum(clan => (float)clan.Gold),
				Influence = clans.Sum(clan => clan.Influence),
				Strength = clans.Sum(clan => clan.CurrentTotalStrength),
				FiefCount = settlements.Count,
				CurrentSettlementCount = settlements.Count
			});
			foreach (Clan clan in clans)
			{
				List<Settlement> clanSettlements = ((IEnumerable<Settlement>)clan.Settlements ?? Enumerable.Empty<Settlement>())
					.Where(IsPrimaryPolicyFief)
					.ToList();
				ReadCentroid(clanSettlements, out bool hasClanPosition, out float clanX, out float clanY);
				string clanName = SafeText(clan.Name, clan.StringId);
				string informalName = SafeText(clan.InformalName, "");
				string leaderName = SafeText(clan.Leader?.Name, "");
				string clanCulture = SafeText(clan.Culture?.Name, cultureName);
				entities.Add(new PolicyTargetEntitySnapshot
				{
					DocumentId = "clan:" + clan.StringId,
					Kind = PolicyTargetEntityKinds.Clan,
					EntityId = clan.StringId,
					OwnerClanId = clan.StringId,
					OwnerKingdomId = kingdom.StringId,
					DisplayName = clanName,
					RetrievalText = PolicyCompatibilityTexts.Get("Targets_Compat_6da1befe1a08") + clanName + PolicyCompatibilityTexts.Get("Targets_Compat_448cf7f4df07") + informalName + " ID " + clan.StringId + PolicyCompatibilityTexts.Get("Targets_Compat_692cc786cc5c") + leaderName + PolicyCompatibilityTexts.Get("Targets_Compat_edc62ff1f0cd") + kingdomName + PolicyCompatibilityTexts.Get("Targets_Compat_9d52c73ade76") + clanCulture,
					MentionAliases = BuildMentionAliases(clanName, informalName, clan.StringId),
					HasPosition = hasClanPosition,
					PositionX = clanX,
					PositionY = clanY,
					Wealth = clan.Gold,
					Influence = clan.Influence,
					Strength = clan.CurrentTotalStrength,
					FiefCount = clanSettlements.Count,
					CurrentSettlementCount = clanSettlements.Count
				});
				if (clan.Leader != null && !string.IsNullOrWhiteSpace(clan.Leader.StringId))
				{
					entities.Add(new PolicyTargetEntitySnapshot
					{
						DocumentId = "ruler:" + clan.Leader.StringId,
						Kind = PolicyTargetEntityKinds.Ruler,
						EntityId = clan.StringId,
						OwnerClanId = clan.StringId,
						OwnerKingdomId = kingdom.StringId,
						DisplayName = leaderName + PolicyGenerationPromptTexts.Get("Targets_Text_a38ce950cf79", " -> their clan's fiefs"),
						RetrievalText = PolicyCompatibilityTexts.Get("Targets_Compat_b7ab70e7378b") + leaderName + " ID " + clan.Leader.StringId + PolicyCompatibilityTexts.Get("Targets_Compat_115d55d24500") + clanName + PolicyCompatibilityTexts.Get("Targets_Compat_edc62ff1f0cd") + kingdomName,
						MentionAliases = BuildMentionAliases(leaderName, clan.Leader.StringId),
						HasPosition = hasClanPosition,
						PositionX = clanX,
						PositionY = clanY,
						Wealth = clan.Gold,
						Influence = clan.Influence,
						Strength = clan.CurrentTotalStrength,
						FiefCount = clanSettlements.Count,
						CurrentSettlementCount = clanSettlements.Count
					});
				}
			}
			foreach (Settlement settlement in settlements)
			{
				entities.Add(CapturePrimaryFief(settlement, kingdom, kingdomName, cultureName));
				fiefSnapshots[settlement.StringId] = PolicyEffectJurisdictionContext.CaptureFief(settlement);
			}
		}
		// Local publication also exists before the player joins or founds a kingdom.
		// Capture that clan's bounded holdings, not every independent world entity.
		Clan playerClan = Clan.PlayerClan;
		if (playerClan != null && !playerClan.IsEliminated && playerClan.Kingdom == null)
		{
			foreach (Settlement settlement in (playerClan.Settlements ?? Enumerable.Empty<Settlement>())
				.Where(IsPrimaryPolicyFief).Where(settlement => settlement.OwnerClan == playerClan))
			{
				if (fiefSnapshots.ContainsKey(settlement.StringId)) continue;
				entities.Add(CapturePrimaryFief(settlement, null, string.Empty, string.Empty));
				fiefSnapshots[settlement.StringId] = PolicyEffectJurisdictionContext.CaptureFief(settlement);
			}
		}
		HashSet<string> wars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> alliances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int left = 0; left < kingdoms.Count; left++)
		{
			for (int right = left + 1; right < kingdoms.Count; right++)
			{
				string pair = BuildPairKey(kingdoms[left].StringId, kingdoms[right].StringId);
				if (kingdoms[left].IsAtWarWith(kingdoms[right]))
				{
					wars.Add(pair);
				}
				if (kingdoms[left].IsAllyWith(kingdoms[right]))
				{
					alliances.Add(pair);
				}
			}
		}
		return new PolicyTargetWorldSnapshot
		{
			StableVersion = stableVersion,
			DynamicVersion = dynamicVersion,
			Entities = entities.ToArray(),
			JurisdictionFiefs = fiefSnapshots,
			Kingdoms = kingdomSnapshots,
			WarPairs = wars.ToArray(),
			AlliancePairs = alliances.ToArray()
		};
	}

	private static PolicyTargetEntitySnapshot CapturePrimaryFief(Settlement settlement, Kingdom kingdom,
		string kingdomName, string cultureName)
	{
		Vec2 position = settlement.GetPosition2D;
		string settlementName = SafeText(settlement.Name, settlement.StringId);
		string ownerClanId = settlement.OwnerClan?.StringId ?? "";
		string typeText = settlement.IsCastle ? PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049") : PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79");
		IReadOnlyList<string> mentionAliases = BuildPrimaryFiefMentionAliases(
			settlement,
			settlementName,
			out float? averageHearth);
		float militia = settlement.Militia;
		return new PolicyTargetEntitySnapshot
		{
			DocumentId = "settlement:" + settlement.StringId,
			Kind = PolicyTargetEntityKinds.Settlement,
			EntityId = settlement.StringId,
			OwnerClanId = ownerClanId,
			OwnerKingdomId = kingdom?.StringId ?? string.Empty,
			DisplayName = settlementName,
			RetrievalText = PolicyCompatibilityTexts.Get("Targets_Compat_7f13c8c5383c") + typeText + PolicyCompatibilityTexts.Get("Targets_Compat_de7fbc881bdc") + settlementName + " ID " + settlement.StringId + PolicyCompatibilityTexts.Get("Targets_Compat_115d55d24500") + SafeText(settlement.OwnerClan?.Name, "") + PolicyCompatibilityTexts.Get("Targets_Compat_edc62ff1f0cd") + kingdomName + PolicyCompatibilityTexts.Get("Targets_Compat_9d52c73ade76") + SafeText(settlement.Culture?.Name, cultureName),
			MentionAliases = mentionAliases,
			IsCity = settlement.IsTown,
			IsCastle = settlement.IsCastle,
			IsBorder = kingdom != null && IsBorderSettlement(settlement, kingdom),
			HasPosition = true,
			PositionX = position.X,
			PositionY = position.Y,
			Food = settlement.Town?.FoodStocks ?? 0f,
			Prosperity = settlement.Town?.Prosperity ?? 0f,
			Loyalty = settlement.Town?.Loyalty ?? 0f,
			Security = settlement.Town?.Security ?? 0f,
			Hearth = averageHearth,
			Militia = float.IsNaN(militia) || float.IsInfinity(militia) ? (float?)null : militia,
			CurrentSettlementCount = 1
		};
	}

	private static PolicyTargetSemanticIndex GetOrBuildIndex(PolicyTargetWorldSnapshot snapshot)
	{
		lock (IndexSync)
		{
			if (_cachedIndex != null && _cachedIndex.StableVersion == snapshot.StableVersion)
			{
				return _cachedIndex;
			}
			OnnxEmbeddingEngine embedding = OnnxEmbeddingEngine.Instance;
			if (embedding == null || !embedding.IsAvailable)
			{
				throw new InvalidOperationException("Policy target ONNX embedding is unavailable: " + (embedding?.LastError ?? "unknown"));
			}
			List<IndexedEntity> items = new List<IndexedEntity>(snapshot.Entities.Count);
			foreach (PolicyTargetEntitySnapshot entity in snapshot.Entities)
			{
				if (!embedding.TryGetEmbedding(entity.RetrievalText, out float[] vector) || vector == null || vector.Length <= 0)
				{
					throw new InvalidOperationException("Policy target entity vector build failed: " + entity.DocumentId + ": " + (embedding.LastError ?? "unknown"));
				}
				items.Add(new IndexedEntity { DocumentId = entity.DocumentId, Vector = vector });
			}
			_cachedIndex = new PolicyTargetSemanticIndex { StableVersion = snapshot.StableVersion, Items = items.ToArray() };
			PolicySystemLog.Write("Generation", "target-semantic-index-built", "stableVersion=" + snapshot.StableVersion.ToString(CultureInfo.InvariantCulture) + " entities=" + items.Count.ToString(CultureInfo.InvariantCulture));
			return _cachedIndex;
		}
	}

	private static IReadOnlyDictionary<string, float[]> BuildFacetVectors()
	{
		OnnxEmbeddingEngine embedding = OnnxEmbeddingEngine.Instance;
		if (embedding == null || !embedding.IsAvailable)
		{
			throw new InvalidOperationException("Policy target Facet ONNX embedding is unavailable: " + (embedding?.LastError ?? "unknown"));
		}
		Dictionary<string, float[]> result = new Dictionary<string, float[]>(StringComparer.Ordinal);
		foreach (PolicyTargetSemanticFacet facet in Facets)
		{
			if (!embedding.TryGetEmbedding(facet.RetrievalText, out float[] vector) || vector == null || vector.Length <= 0)
			{
				throw new InvalidOperationException("Policy target Facet vector build failed: " + facet.Id + ": " + (embedding.LastError ?? "unknown"));
			}
			result.Add(facet.Id, vector);
		}
		return result;
	}

	private static List<PolicyTargetSemanticProposal> BuildShadowProposals(PolicyTargetSemanticContext context, List<ScoredRecallItem> scored)
	{
		List<PolicyTargetSemanticProposal> result = scored
			.Where(item => item.Item.Entity != null)
			.OrderByDescending(item => item.Score)
			.ThenByDescending(item => item.Item.RecallScore)
			.Take(3)
			.Select(item => ProjectProposal(item.Item.Entity, context.Scope, item.Score, PolicyGenerationPromptTexts.Get("Targets_Text_054c02d5e9bd", "Entity semantics: ") + item.Item.Entity.DisplayName))
			.Where(item => item != null)
			.ToList();
		List<ScoredRecallItem> orderedFacets = scored
			.Where(item => item.Item.Facet != null)
			.OrderByDescending(item => item.Score)
			.ThenByDescending(item => item.Item.RecallScore)
			.ToList();
		if (orderedFacets.Count > 0)
		{
			float topScore = orderedFacets[0].Score;
			List<ScoredRecallItem> activeFacets = orderedFacets
				.Where(item => topScore - item.Score <= ShadowFacetMargin)
				.GroupBy(item => item.Item.Facet.Group, StringComparer.Ordinal)
				.Select(group => group.OrderByDescending(item => item.Score).First())
				.Take(4)
				.ToList();
			IEnumerable<PolicyTargetEntitySnapshot> candidates = context.Snapshot.Entities.Where(entity => IsEntityInScope(entity, context));
			foreach (ScoredRecallItem facetItem in activeFacets)
			{
				candidates = ApplyFacetFilter(candidates, facetItem.Item.Facet, context);
			}
			ScoredRecallItem metricFacet = activeFacets.FirstOrDefault(item => string.Equals(item.Item.Facet.Group, "metric", StringComparison.Ordinal));
			ScoredRecallItem distanceFacet = activeFacets.FirstOrDefault(item => string.Equals(item.Item.Facet.Group, "distance", StringComparison.Ordinal));
			if (metricFacet != null)
			{
				candidates = OrderByMetric(candidates, metricFacet.Item.Facet);
			}
			else if (distanceFacet != null)
			{
				candidates = OrderByDistance(candidates, distanceFacet.Item.Facet, context);
			}
			else
			{
				candidates = candidates.OrderBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(entity => entity.EntityId, StringComparer.OrdinalIgnoreCase);
			}
			string evidence = "Facet:" + string.Join("+", activeFacets.Select(item => item.Item.Facet.Id));
			float score = activeFacets.Count > 0 ? activeFacets.Max(item => item.Score) : 0f;
			result.AddRange(candidates.Take(3).Select(entity => ProjectProposal(entity, context.Scope, score, evidence)).Where(item => item != null));
		}
		return result
			.GroupBy(item => item.HandleKind + "\n" + item.EntityId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(item => item.Score).First())
			.OrderByDescending(item => item.Score)
			.ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static List<PolicyTargetSemanticProposal> BuildEnabledProposals(PolicyTargetSemanticContext context, List<ScoredRecallItem> scored)
	{
		List<PolicyTargetSemanticProposal> result = SelectCalibratedDirectEntityProposals(context, scored);

		List<ScoredRecallItem> activeFacets = scored
			.Where(item => item.Item.Facet != null
				&& IsFacetApplicableToScope(item.Item.Facet, context.Scope)
				&& FacetMatchesExplicitCue(context?.QueryText, item.Item.Facet)
				&& CalibratedFacetThresholds.TryGetValue(item.Item.Facet.Id, out float threshold)
				&& item.Score >= threshold)
			.GroupBy(item => item.Item.Facet.Group, StringComparer.Ordinal)
			.Select(group => group.OrderByDescending(item => item.Score).ThenByDescending(item => item.Item.RecallScore).First())
			.OrderByDescending(item => item.Score)
			.Take(4)
			.ToList();

		// 单一关系或指标容易把泛化政策物化为任意实体；至少两个独立语义组才允许 Facet 生成候选。
		if (activeFacets.Select(item => item.Item.Facet.Group).Distinct(StringComparer.Ordinal).Count() >= 2)
		{
			IEnumerable<PolicyTargetEntitySnapshot> candidates = context.Snapshot.Entities.Where(entity => IsEntityInScope(entity, context));
			if (!string.Equals(context.Scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
			{
				// 外国氏族/定居点的直接语义命中仍可折叠为 K；Facet 排名只比较王国，避免子实体动态值污染王国排序。
				candidates = candidates.Where(entity => entity.Kind == PolicyTargetEntityKinds.Kingdom);
			}
			foreach (ScoredRecallItem facetItem in activeFacets)
			{
				candidates = ApplyFacetFilter(candidates, facetItem.Item.Facet, context);
			}
			ScoredRecallItem metricFacet = activeFacets.FirstOrDefault(item => string.Equals(item.Item.Facet.Group, "metric", StringComparison.Ordinal));
			ScoredRecallItem distanceFacet = activeFacets.FirstOrDefault(item => string.Equals(item.Item.Facet.Group, "distance", StringComparison.Ordinal));
			if (metricFacet != null)
			{
				candidates = OrderByMetric(candidates, metricFacet.Item.Facet);
			}
			else if (distanceFacet != null)
			{
				candidates = OrderByDistance(candidates, distanceFacet.Item.Facet, context);
			}
			else
			{
				candidates = candidates.OrderBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(entity => entity.EntityId, StringComparer.OrdinalIgnoreCase);
			}
			string evidence = "Facet:" + string.Join("+", activeFacets.Select(item => item.Item.Facet.Id));
			float score = activeFacets.Max(item => item.Score);
			int materializedLimit = metricFacet != null || distanceFacet != null ? 1 : 3;
			result.AddRange(candidates.Take(materializedLimit).Select(entity => ProjectProposal(entity, context.Scope, score, evidence)).Where(item => item != null));
		}

		return result
			.GroupBy(item => item.HandleKind + "\n" + item.EntityId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(item => item.Score).First())
			.OrderByDescending(item => item.Score)
			.ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static List<PolicyTargetSemanticProposal> SelectCalibratedDirectEntityProposals(
		PolicyTargetSemanticContext context,
		IEnumerable<ScoredRecallItem> scored)
	{
		List<ScoredRecallItem> mechanicalTargets = (scored ?? Enumerable.Empty<ScoredRecallItem>())
			.Where(item => item.Item.Entity != null
				&& CalibratedEntityThresholds.TryGetValue(item.Item.Entity.Kind, out float threshold)
				&& item.Score >= threshold
				&& PolicyTextApproximatelyMentionsEntity(context?.QueryText, item.Item.Entity)
				&& EntityMatchesExplicitTypeCue(context?.QueryText, item.Item.Entity))
			.GroupBy(item => BuildDirectMechanicalTargetKey(item.Item.Entity), StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(item => item.Score).ThenByDescending(item => item.Item.RecallScore).First())
			.ToList();
		List<PolicyTargetSemanticProposal> result = new List<PolicyTargetSemanticProposal>();
		foreach (IGrouping<string, ScoredRecallItem> category in mechanicalTargets.GroupBy(item => BuildDirectCompetitionCategory(item.Item.Entity), StringComparer.Ordinal))
		{
			List<ScoredRecallItem> ordered = category
				.OrderByDescending(item => item.Score)
				.ThenByDescending(item => item.Item.RecallScore)
				.ToList();
			if (ordered.Count <= 0)
			{
				continue;
			}
			if (ordered.Count > 1 && ordered[0].Score - ordered[1].Score < DirectEntityMinimumRecallScoreGap)
			{
				continue;
			}
			PolicyTargetSemanticProposal proposal = ProjectProposal(
				ordered[0].Item.Entity,
				context.Scope,
				ordered[0].Score,
				PolicyGenerationPromptTexts.Get("Targets_Text_20a08576c8f0", "Approximate entity-name match: ") + ordered[0].Item.Entity.DisplayName);
			if (proposal != null)
			{
				result.Add(proposal);
			}
		}
		return result.OrderByDescending(item => item.Score).ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string BuildDirectMechanicalTargetKey(PolicyTargetEntitySnapshot entity)
	{
		if (entity.Kind == PolicyTargetEntityKinds.Clan || entity.Kind == PolicyTargetEntityKinds.Ruler)
		{
			return "family:" + (entity.OwnerClanId ?? entity.EntityId ?? "");
		}
		return (entity.Kind ?? "") + ":" + (entity.EntityId ?? "");
	}

	private static string BuildDirectCompetitionCategory(PolicyTargetEntitySnapshot entity)
	{
		return entity.Kind == PolicyTargetEntityKinds.Clan || entity.Kind == PolicyTargetEntityKinds.Ruler
			? "family"
			: entity.Kind ?? "";
	}

	private static bool PolicyTextApproximatelyMentionsEntity(string queryText, PolicyTargetEntitySnapshot entity)
	{
		return PolicyTextApproximatelyMentionsEntityNormalized(NormalizeMentionText(queryText), entity);
	}

	private static bool PolicyTextApproximatelyMentionsEntityNormalized(string normalizedQuery, PolicyTargetEntitySnapshot entity)
	{
		if (entity?.MentionAliases == null || string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return false;
		}
		foreach (string rawAlias in entity.MentionAliases)
		{
			string alias = NormalizeMentionText(rawAlias);
			if (alias.Length <= 0)
			{
				continue;
			}
			if (normalizedQuery.IndexOf(alias, StringComparison.Ordinal) >= 0)
			{
				return true;
			}
			if (alias.Length < 3)
			{
				continue;
			}
			for (int length = Math.Max(2, alias.Length - 1); length <= alias.Length + 1; length++)
			{
				for (int start = 0; start + length <= normalizedQuery.Length; start++)
				{
					if (BoundedEditDistance(alias, normalizedQuery, start, length, 1) <= 1)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool EntityMatchesExplicitTypeCue(string queryText, PolicyTargetEntitySnapshot entity)
	{
		return EntityMatchesExplicitTypeCueNormalized(NormalizeMentionText(queryText), entity);
	}

	private static bool FacetMatchesExplicitCue(string queryText, PolicyTargetSemanticFacet facet)
	{
		if (facet == null || !string.Equals(facet.Group, "type", StringComparison.Ordinal))
		{
			return facet != null;
		}
		string normalizedQuery = NormalizeMentionText(queryText);
		if (string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return false;
		}
		switch (facet.Id)
		{
			case "type_kingdom": return normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_0cc659a9f57c")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_f1377277c151")) || normalizedQuery.Contains("kingdom");
			case "type_clan": return normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_091")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_12af41cb2d29")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_f49dc9dc440f")) || normalizedQuery.Contains("clan");
			case "type_ruler": return normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Generation_RulerFallback")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_f9f0546e964e")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_90c42cbd6c54")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_37ab35120d1a")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_38e917f02192")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_8655bf651c8c")) || normalizedQuery.Contains("ruler");
			case "type_settlement": return normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_095")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_f9bab1c5399d")) || normalizedQuery.Contains("settlement");
			case "type_city": return normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_d7eef7209e79")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_a10c545fdb9c")) || normalizedQuery.Contains("city");
			case "type_castle": return normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_61a401cf32d4")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_5ec7f9ec424e")) || normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_7f3b34fa6d00")) || normalizedQuery.Contains("castle");
			default: return true;
		}
	}

	private static bool EntityMatchesExplicitTypeCueNormalized(string normalizedQuery, PolicyTargetEntitySnapshot entity)
	{
		if (entity == null || entity.Kind != PolicyTargetEntityKinds.Settlement || string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return true;
		}
		if (normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_32bd97013331")) || normalizedQuery.Contains("village"))
		{
			return false;
		}
		bool castleCue = normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"))
			|| normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_61a401cf32d4"))
			|| normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_5ec7f9ec424e"))
			|| normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_7f3b34fa6d00"))
			|| normalizedQuery.Contains("castle");
		if (castleCue)
		{
			return entity.IsCastle;
		}
		bool cityCue = normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79"))
			|| normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"))
			|| normalizedQuery.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_a10c545fdb9c"))
			|| normalizedQuery.Contains("city");
		if (cityCue)
		{
			return entity.IsCity;
		}
		return true;
	}

	private static string NormalizeMentionText(string text)
	{
		StringBuilder builder = new StringBuilder(text?.Length ?? 0);
		foreach (char value in text ?? "")
		{
			if (char.IsLetterOrDigit(value))
			{
				builder.Append(char.ToLowerInvariant(value));
			}
		}
		return builder.ToString();
	}

	private static int BoundedEditDistance(string expected, string source, int sourceStart, int sourceLength, int maximum)
	{
		if (Math.Abs(expected.Length - sourceLength) > maximum)
		{
			return maximum + 1;
		}
		int[] previous = new int[sourceLength + 1];
		int[] current = new int[sourceLength + 1];
		for (int column = 0; column <= sourceLength; column++) previous[column] = column;
		for (int row = 1; row <= expected.Length; row++)
		{
			current[0] = row;
			int rowMinimum = current[0];
			for (int column = 1; column <= sourceLength; column++)
			{
				int substitution = previous[column - 1] + (expected[row - 1] == source[sourceStart + column - 1] ? 0 : 1);
				current[column] = Math.Min(Math.Min(previous[column] + 1, current[column - 1] + 1), substitution);
				rowMinimum = Math.Min(rowMinimum, current[column]);
			}
			if (rowMinimum > maximum)
			{
				return maximum + 1;
			}
			int[] swap = previous;
			previous = current;
			current = swap;
		}
		return previous[sourceLength];
	}

	private static bool IsFacetApplicableToScope(PolicyTargetSemanticFacet facet, string scope)
	{
		if (facet == null || string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			return facet != null;
		}
		return !string.Equals(facet.Group, "type", StringComparison.Ordinal)
			&& !string.Equals(facet.Group, "geography", StringComparison.Ordinal)
			&& !facet.Id.StartsWith("metric_food_", StringComparison.Ordinal)
			&& !facet.Id.StartsWith("metric_prosperity_", StringComparison.Ordinal)
			&& !facet.Id.StartsWith("metric_loyalty_", StringComparison.Ordinal)
			&& !facet.Id.StartsWith("metric_security_", StringComparison.Ordinal);
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> ApplyFacetFilter(
		IEnumerable<PolicyTargetEntitySnapshot> source,
		PolicyTargetSemanticFacet facet,
		PolicyTargetSemanticContext context)
	{
		if (facet.Id.StartsWith("metric_wealth_", StringComparison.Ordinal)
			|| facet.Id.StartsWith("metric_influence_", StringComparison.Ordinal)
			|| facet.Id.StartsWith("metric_strength_", StringComparison.Ordinal)
			|| facet.Id.StartsWith("metric_fiefs_", StringComparison.Ordinal))
		{
			return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Kingdom
				|| entity.Kind == PolicyTargetEntityKinds.Clan
				|| entity.Kind == PolicyTargetEntityKinds.Ruler);
		}
		if (facet.Id.StartsWith("metric_food_", StringComparison.Ordinal)
			|| facet.Id.StartsWith("metric_prosperity_", StringComparison.Ordinal)
			|| facet.Id.StartsWith("metric_loyalty_", StringComparison.Ordinal)
			|| facet.Id.StartsWith("metric_security_", StringComparison.Ordinal))
		{
			return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Settlement);
		}
		switch (facet.Id)
		{
			case "relation_enemy": return source.Where(entity => HasPair(context.Snapshot.WarPairs, context.TargetKingdomId, entity.OwnerKingdomId));
			case "relation_ally": return source.Where(entity => HasPair(context.Snapshot.AlliancePairs, context.TargetKingdomId, entity.OwnerKingdomId));
			case "relation_foreign": return source.Where(entity => !string.Equals(entity.OwnerKingdomId, context.TargetKingdomId, StringComparison.OrdinalIgnoreCase));
			case "relation_domestic": return source.Where(entity => string.Equals(entity.OwnerKingdomId, context.TargetKingdomId, StringComparison.OrdinalIgnoreCase));
			case "type_kingdom": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Kingdom);
			case "type_clan": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Clan);
			case "type_ruler": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Ruler);
			case "type_settlement": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Settlement);
			case "type_city": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Settlement && entity.IsCity);
			case "type_castle": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Settlement && entity.IsCastle);
			case "geography_border": return source.Where(entity => entity.Kind == PolicyTargetEntityKinds.Settlement && entity.IsBorder);
			case "direction_north": return FilterByDirection(source, context, 0f, 1f);
			case "direction_south": return FilterByDirection(source, context, 0f, -1f);
			case "direction_east": return FilterByDirection(source, context, 1f, 0f);
			case "direction_west": return FilterByDirection(source, context, -1f, 0f);
			default: return source;
		}
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> OrderByMetric(IEnumerable<PolicyTargetEntitySnapshot> source, PolicyTargetSemanticFacet facet)
	{
		Func<PolicyTargetEntitySnapshot, float> selector;
		if (facet.Id.StartsWith("metric_wealth_", StringComparison.Ordinal)) selector = entity => entity.Wealth;
		else if (facet.Id.StartsWith("metric_influence_", StringComparison.Ordinal)) selector = entity => entity.Influence;
		else if (facet.Id.StartsWith("metric_strength_", StringComparison.Ordinal)) selector = entity => entity.Strength;
		else if (facet.Id.StartsWith("metric_fiefs_", StringComparison.Ordinal)) selector = entity => entity.FiefCount;
		else if (facet.Id.StartsWith("metric_food_", StringComparison.Ordinal)) selector = entity => entity.Food;
		else if (facet.Id.StartsWith("metric_prosperity_", StringComparison.Ordinal)) selector = entity => entity.Prosperity;
		else if (facet.Id.StartsWith("metric_loyalty_", StringComparison.Ordinal)) selector = entity => entity.Loyalty;
		else if (facet.Id.StartsWith("metric_security_", StringComparison.Ordinal)) selector = entity => entity.Security;
		else return source.OrderBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(entity => entity.EntityId, StringComparer.OrdinalIgnoreCase);
		return facet.SortDescending
			? source.OrderByDescending(selector).ThenBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(entity => entity.EntityId, StringComparer.OrdinalIgnoreCase)
			: source.OrderBy(selector).ThenBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(entity => entity.EntityId, StringComparer.OrdinalIgnoreCase);
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> OrderByDistance(IEnumerable<PolicyTargetEntitySnapshot> source, PolicyTargetSemanticFacet facet, PolicyTargetSemanticContext context)
	{
		ReadReferencePosition(context, out bool hasReference, out float referenceX, out float referenceY);
		if (!hasReference)
		{
			return Enumerable.Empty<PolicyTargetEntitySnapshot>();
		}
		Func<PolicyTargetEntitySnapshot, float> distance = entity => DistanceSquared(entity.PositionX, entity.PositionY, referenceX, referenceY);
		IEnumerable<PolicyTargetEntitySnapshot> positioned = source.Where(entity => entity.HasPosition);
		return facet.SortDescending
			? positioned.OrderByDescending(distance).ThenBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase)
			: positioned.OrderBy(distance).ThenBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase);
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> FilterByDirection(
		IEnumerable<PolicyTargetEntitySnapshot> source,
		PolicyTargetSemanticContext context,
		float directionX,
		float directionY)
	{
		ReadReferencePosition(context, out bool hasReference, out float referenceX, out float referenceY);
		if (!hasReference)
		{
			return Enumerable.Empty<PolicyTargetEntitySnapshot>();
		}
		return source.Where(entity => entity.HasPosition
			&& (entity.PositionX - referenceX) * directionX + (entity.PositionY - referenceY) * directionY > 0f);
	}

	private static void ReadReferencePosition(PolicyTargetSemanticContext context, out bool hasPosition, out float x, out float y)
	{
		if (string.Equals(context.Scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			HashSet<string> sourceIds = new HashSet<string>(context.SourceSettlementIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
			List<PolicyTargetEntitySnapshot> sourceSettlements = context.Snapshot.Entities
				.Where(entity => entity.Kind == PolicyTargetEntityKinds.Settlement && sourceIds.Contains(entity.EntityId) && entity.HasPosition)
				.ToList();
			if (sourceSettlements.Count > 0)
			{
				hasPosition = true;
				x = sourceSettlements.Average(entity => entity.PositionX);
				y = sourceSettlements.Average(entity => entity.PositionY);
				return;
			}
		}
		if (context.Snapshot.Kingdoms.TryGetValue(context.TargetKingdomId ?? "", out PolicyTargetKingdomSnapshot kingdom) && kingdom.HasPosition)
		{
			hasPosition = true;
			x = kingdom.PositionX;
			y = kingdom.PositionY;
			return;
		}
		hasPosition = false;
		x = 0f;
		y = 0f;
	}

	private static PolicyTargetSemanticProposal ProjectProposal(PolicyTargetEntitySnapshot entity, string scope, float score, string evidence)
	{
		if (entity == null)
		{
			return null;
		}
		if (!string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			return new PolicyTargetSemanticProposal
			{
				HandleKind = PolicyTargetEntityKinds.Kingdom,
				EntityId = entity.OwnerKingdomId,
				OwnerKingdomId = entity.OwnerKingdomId,
				DisplayName = entity.OwnerKingdomId,
				Evidence = evidence + PolicyGenerationPromptTexts.Get("Targets_Text_99a8beafaddd", " -> owning kingdom"),
				Score = score,
				CurrentSettlementCount = 0
			};
		}
		return new PolicyTargetSemanticProposal
		{
			HandleKind = entity.Kind,
			EntityId = entity.EntityId,
			OwnerKingdomId = entity.OwnerKingdomId,
			DisplayName = entity.DisplayName,
			Evidence = evidence,
			Score = score,
			CurrentSettlementCount = entity.CurrentSettlementCount
		};
	}

	private static IReadOnlyList<PolicyTargetSemanticProposal> ApplyProposalCaps(List<PolicyTargetSemanticProposal> proposals, string scope)
	{
		if (!string.Equals(scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			return proposals
				.GroupBy(item => item.EntityId, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.OrderByDescending(item => item.Score).First())
				.Take(KingdomSemanticAdditionLimit)
				.ToArray();
		}
		Dictionary<string, int> kindCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		List<PolicyTargetSemanticProposal> result = new List<PolicyTargetSemanticProposal>();
		foreach (PolicyTargetSemanticProposal proposal in proposals)
		{
			kindCounts.TryGetValue(proposal.HandleKind ?? "", out int count);
			if (count >= LocalPerKindAdditionLimit)
			{
				continue;
			}
			result.Add(proposal);
			kindCounts[proposal.HandleKind ?? ""] = count + 1;
			if (result.Count >= LocalSemanticAdditionLimit)
			{
				break;
			}
		}
		return result;
	}

	private static bool IsEntityInScope(PolicyTargetEntitySnapshot entity, PolicyTargetSemanticContext context)
	{
		if (entity == null || context == null || string.IsNullOrWhiteSpace(entity.OwnerKingdomId))
		{
			return false;
		}
		if (string.Equals(context.Scope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			if (entity.Kind == PolicyTargetEntityKinds.Kingdom)
			{
				return false;
			}
			return entity.Kind != PolicyTargetEntityKinds.Settlement
				|| !(context.SourceSettlementIds ?? Array.Empty<string>()).Contains(entity.EntityId, StringComparer.OrdinalIgnoreCase);
		}
		return !string.Equals(entity.OwnerKingdomId, context.TargetKingdomId, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(entity.OwnerKingdomId, context.IssuerKingdomId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool KindMatchesHandle(string entityKind, string handleKind)
	{
		return string.Equals(entityKind, handleKind, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildShadowLog(
		PolicyTargetSemanticContext context,
		List<ScoredRecallItem> scored,
		List<PolicyTargetSemanticProposal> proposals)
	{
		string entities = string.Join(",", scored.Where(item => item.Item.Entity != null).OrderByDescending(item => item.Score).Take(5)
			.Select(item => item.Item.Id + ":" + item.Score.ToString("0.000", CultureInfo.InvariantCulture)));
		string facets = string.Join(",", scored.Where(item => item.Item.Facet != null).OrderByDescending(item => item.Score).Take(8)
			.Select(item => item.Item.Id + ":" + item.Score.ToString("0.000", CultureInfo.InvariantCulture)));
		string targets = string.Join(",", proposals.Take(6).Select(item => item.HandleKind + ":" + item.EntityId + ":" + item.Score.ToString("0.000", CultureInfo.InvariantCulture)));
		return "enabled=" + (SemanticExpansionEnabled ? "true" : "false")
			+ " reason=" + SemanticCalibrationReason
			+ " stableVersion=" + context.Snapshot.StableVersion.ToString(CultureInfo.InvariantCulture)
			+ " dynamicVersion=" + context.Snapshot.DynamicVersion.ToString(CultureInfo.InvariantCulture)
			+ " targetPairs=" + scored.Count.ToString(CultureInfo.InvariantCulture)
			+ " entities=" + entities
			+ " facets=" + facets
			+ " proposals=" + targets;
	}

	private static IReadOnlyList<PolicyTargetSemanticFacet> BuildFacets()
	{
		return new[]
		{
			Facet("relation_enemy", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_1bc2520e8c07")),
			Facet("relation_ally", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_b5a43c1f6c81")),
			Facet("relation_foreign", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_e342c5a250b1")),
			Facet("relation_domestic", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_f3ce27edac60")),
			Facet("direction_north", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_7a1771e49662")),
			Facet("direction_south", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_c0654d3463e8")),
			Facet("direction_east", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_d464628fa125")),
			Facet("direction_west", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_c55f0d0a134e")),
			Facet("type_kingdom", "type", PolicyCompatibilityTexts.Get("Targets_Compat_1df93f7754e3")),
			Facet("type_clan", "type", PolicyCompatibilityTexts.Get("Targets_Compat_9aea7d8d75dc")),
			Facet("type_ruler", "type", PolicyCompatibilityTexts.Get("Targets_Compat_660407989dcb")),
			Facet("type_settlement", "type", PolicyCompatibilityTexts.Get("Targets_Compat_80ee5523b224")),
			Facet("type_city", "type", PolicyCompatibilityTexts.Get("Targets_Compat_c90ebc9d2704")),
			Facet("type_castle", "type", PolicyCompatibilityTexts.Get("Targets_Compat_82c99ff833b4")),
			Facet("geography_border", "geography", PolicyCompatibilityTexts.Get("Targets_Compat_48dc984d92f7")),
			Facet("metric_wealth_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_c58a53e58c38"), true),
			Facet("metric_wealth_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_e9721faa1464")),
			Facet("metric_influence_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_f1aaab848f61"), true),
			Facet("metric_influence_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_6e15fe6194ae")),
			Facet("metric_strength_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_e26bee2f3829"), true),
			Facet("metric_strength_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_7da87d04bfc9")),
			Facet("metric_fiefs_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_fbe392c081bc"), true),
			Facet("metric_fiefs_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_584c38dc9a4f")),
			Facet("metric_food_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_22bfd140d6cc"), true),
			Facet("metric_food_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_7344d4fe06fe")),
			Facet("metric_prosperity_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_31fe4cb9e70c"), true),
			Facet("metric_prosperity_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_a1ddcfcdafa6")),
			Facet("metric_loyalty_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_fdad03c4bee1"), true),
			Facet("metric_loyalty_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_3a577c875ddd")),
			Facet("metric_security_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_942b8fd6032b"), true),
			Facet("metric_security_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_bb5bf9a9bd85")),
			Facet("metric_hearth_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_ae7739822531"), true),
			Facet("metric_hearth_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_036e88ef2afb")),
			Facet("metric_militia_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_e61213bf1404"), true),
			Facet("metric_militia_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_9222b05f325a")),
			Facet("distance_nearest", "distance", PolicyCompatibilityTexts.Get("Targets_Compat_affe8b9f933b")),
			Facet("distance_farthest", "distance", PolicyCompatibilityTexts.Get("Targets_Compat_15185b07b5f6"), true)
		};
	}

	private static PolicyTargetSemanticFacet Facet(string id, string group, string text, bool descending = false)
	{
		return new PolicyTargetSemanticFacet { Id = id, Group = group, RetrievalText = text, SortDescending = descending };
	}

	private static List<Settlement> SafeGetKingdomSettlements(Kingdom kingdom)
	{
		try
		{
			return ((IEnumerable<Settlement>)kingdom.Settlements ?? Enumerable.Empty<Settlement>())
				.Where(IsPrimaryPolicyFief)
				.OrderBy(settlement => settlement.StringId, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private static bool IsBorderSettlement(Settlement settlement, Kingdom kingdom)
	{
		try
		{
			Settlement fortification = settlement;
			return fortification?.Town != null
				&& fortification.Town.GetNeighborFortifications(MobileParty.NavigationType.All)
					.Any(neighbor => neighbor != null && neighbor.MapFaction != kingdom);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPrimaryPolicyFief(Settlement settlement)
	{
		return settlement != null && (settlement.IsTown || settlement.IsCastle);
	}

	private static void ReadCentroid(IEnumerable<Settlement> settlements, out bool hasPosition, out float x, out float y)
	{
		List<Settlement> list = (settlements ?? Enumerable.Empty<Settlement>()).Where(settlement => settlement != null).ToList();
		if (list.Count <= 0)
		{
			hasPosition = false;
			x = 0f;
			y = 0f;
			return;
		}
		hasPosition = true;
		x = list.Average(settlement => settlement.GetPosition2D.X);
		y = list.Average(settlement => settlement.GetPosition2D.Y);
	}

	private static string SafeText(object value, string fallback)
	{
		string text = value?.ToString()?.Trim() ?? "";
		return text.Length > 0 ? text : fallback ?? "";
	}

	private static IReadOnlyList<string> BuildMentionAliases(params string[] values)
	{
		HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string raw in values ?? Array.Empty<string>())
		{
			string value = raw?.Trim() ?? "";
			if (value.Length <= 0)
			{
				continue;
			}
			aliases.Add(value);
			int separator = Math.Max(value.LastIndexOf('·'), value.LastIndexOf('.'));
			if (separator >= 0 && separator + 1 < value.Length)
			{
				aliases.Add(value.Substring(separator + 1));
			}
		}
		return aliases.OrderBy(alias => alias, StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private static IReadOnlyList<string> BuildPrimaryFiefMentionAliases(
		Settlement primary,
		string primaryName,
		out float? averageHearth)
	{
		List<string> aliases = new List<string> { primaryName, primary?.StringId ?? string.Empty };
		float hearthSum = 0f;
		int hearthCount = 0;
		try
		{
			foreach (Village village in primary?.BoundVillages ?? Enumerable.Empty<Village>())
			{
				if (village?.Settlement == null)
				{
					continue;
				}
				aliases.Add(SafeText(village.Settlement.Name, village.Settlement.StringId));
				aliases.Add(village.Settlement.StringId ?? string.Empty);
				float hearth = village.Hearth;
				if (!float.IsNaN(hearth) && !float.IsInfinity(hearth))
				{
					hearthSum += hearth;
					hearthCount++;
				}
			}
		}
		catch
		{
			// The parent remains selectable even if a modded settlement exposes a
			// broken bound-village collection.
		}
		averageHearth = hearthCount > 0 ? hearthSum / hearthCount : (float?)null;
		return BuildMentionAliases(aliases.ToArray());
	}

	private static string BuildPairKey(string left, string right)
	{
		return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0 ? left + "\n" + right : right + "\n" + left;
	}

	private static bool HasPair(IReadOnlyCollection<string> pairs, string left, string right)
	{
		return !string.IsNullOrWhiteSpace(left)
			&& !string.IsNullOrWhiteSpace(right)
			&& (pairs ?? Array.Empty<string>()).Contains(BuildPairKey(left, right), StringComparer.OrdinalIgnoreCase);
	}

	private static float DistanceSquared(float leftX, float leftY, float rightX, float rightY)
	{
		float x = leftX - rightX;
		float y = leftY - rightY;
		return x * x + y * y;
	}

	private static float Cosine(float[] left, float[] right)
	{
		if (left == null || right == null || left.Length <= 0 || left.Length != right.Length)
		{
			return float.NegativeInfinity;
		}
		double dot = 0d;
		double leftNorm = 0d;
		double rightNorm = 0d;
		for (int index = 0; index < left.Length; index++)
		{
			dot += (double)left[index] * right[index];
			leftNorm += (double)left[index] * left[index];
			rightNorm += (double)right[index] * right[index];
		}
		if (leftNorm <= 0d || rightNorm <= 0d)
		{
			return float.NegativeInfinity;
		}
		return (float)(dot / Math.Sqrt(leftNorm * rightNorm));
	}

	private static void AssertInternalContracts()
	{
		if (EntityRecallLimit != 10
			|| FacetRecallLimit != 8
			|| PolicyEffectModuleRouter.RecallLimit != PolicyEffectModuleRouter.CandidateHardMaximum
			|| LocalSemanticAdditionLimit > 4
			|| LocalPerKindAdditionLimit > 3
			|| KingdomSemanticAdditionLimit > 3
			|| Facets.Count != Facets.Select(facet => facet.Id).Distinct(StringComparer.Ordinal).Count())
		{
			throw new InvalidOperationException("Policy target semantic routing internal contract is invalid.");
		}
	}

	private sealed class IndexedEntity
	{
		internal string DocumentId { get; set; }

		internal float[] Vector { get; set; }
	}

	private sealed class PolicyTargetSemanticIndex
	{
		internal long StableVersion { get; set; }

		internal IReadOnlyList<IndexedEntity> Items { get; set; }
	}

	private sealed class ScoredRecallItem
	{
		internal PolicyTargetSemanticRecallItem Item { get; set; }

		internal float Score { get; set; }
	}
}
