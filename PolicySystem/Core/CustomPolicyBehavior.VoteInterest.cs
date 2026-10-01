using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

// Vote support for AF dynamic policies. Vanilla KingdomPolicyDecision.DetermineSupport only reads
// three political weights, so lords ignore what a custom policy actually does to their own fiefs.
// This postfix adds two terms on the same scale as vanilla's political score S:
//   U = self-interest derived from the policy's structured module effects (cached per policy/day)
//   R = relation between the voter's leader and the proposer's leader
// Enforce outcome: result += 60 * (U' + R); reject outcome: result -= 100 * (U' + R),
// where U' = -U for abolition agendas (abolishing removes the benefits). R follows the agenda
// proposer, except for the AF natural-expiry abolition agenda (forced ruler proposer), where it
// follows the policy's original proposer and favors keeping the policy.
// The proposer clan itself is never adjusted, so vanilla cancellation logic is unchanged.
public sealed partial class CustomPolicyBehavior
{
	private const float VoteInterestMaxAbs = 2f;
	private const float VoteInterestPartMaxAbs = 3f;
	private const float VoteRelationMaxAbs = 1f;
	private const float VoteInterestFiefExposureMax = 2f;
	private const float VoteInterestFiefScale = 0.5f;
	private const float VoteInterestKingdomScaleRuler = 0.6f;
	private const float VoteInterestKingdomScaleVassal = 0.3f;
	private const float VoteInterestClanScale = 0.8f;
	private const float VoteInterestShortDurationDays = 60f;

	private enum VoteInterestCategory
	{
		Fief,
		Village,
		Kingdom,
		Clan,
		Hero
	}

	// Baselines equal the "ordinary policy" strength in each module's evaluation prompt, so a
	// normalized strength of 1.0 means a typical, clearly felt effect.
	private sealed class VoteInterestModuleSpec
	{
		internal VoteInterestModuleSpec(
			VoteInterestCategory category,
			float valueBaseline,
			bool valueIsDaily,
			float onceBaseline = 0f,
			float dailyBaseline = 0f)
		{
			Category = category;
			ValueBaseline = valueBaseline;
			ValueIsDaily = valueIsDaily;
			OnceBaseline = onceBaseline;
			DailyBaseline = dailyBaseline;
		}

		internal VoteInterestCategory Category { get; }

		internal float ValueBaseline { get; }

		internal bool ValueIsDaily { get; }

		internal float OnceBaseline { get; }

		internal float DailyBaseline { get; }
	}

	private sealed class VoteInterestContext
	{
		internal string Source = string.Empty;

		internal string Scope = PolicyEffectScopes.Kingdom;

		internal string TargetKingdomId = string.Empty;

		internal string IssuerKingdomId = string.Empty;

		internal string ProposerClanId = string.Empty;

		internal List<string> SourceSettlementIds = new List<string>();
	}

	private sealed class VoteInterestTargets
	{
		internal readonly HashSet<string> PrimaryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal readonly HashSet<string> VillageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal readonly HashSet<string> ClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal readonly HashSet<string> KingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal readonly HashSet<string> HeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal bool FollowCurrentRulingClan;
	}

	private sealed class VoteInterestCacheEntry
	{
		internal int Day;

		internal string Raw = string.Empty;

		internal string KingdomId = string.Empty;

		// True for the AF natural-expiry abolition agenda: the ruler is a forced proposer, so the
		// relation term follows the policy's original proposer instead.
		internal bool IsSystemExpiryAgenda;

		internal string PolicyProposerClanId = string.Empty;

		internal readonly Dictionary<string, float> InterestByClanId = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

		internal readonly Dictionary<string, string> DetailByClanId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	private static readonly Dictionary<string, VoteInterestModuleSpec> VoteInterestModuleSpecs =
		new Dictionary<string, VoteInterestModuleSpec>(StringComparer.Ordinal)
		{
			["prosperityPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 5f, true),
			["constructionPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 150f, true),
			["loyaltyPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 1.2f, true),
			["securityPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 0.7f, true),
			["foodPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 8f, true),
			["militiaPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 4f, true),
			["taxIncomePct"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 15f, true),
			["volunteerProductionGrowthPct"] = new VoteInterestModuleSpec(VoteInterestCategory.Fief, 40f, true),
			["hearthPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Village, 1.5f, true),
			["villageProductionPct"] = new VoteInterestModuleSpec(VoteInterestCategory.Village, 30f, true),
			["kingdomStability"] = new VoteInterestModuleSpec(VoteInterestCategory.Kingdom, 5f, false),
			["kingdomStabilityOnce"] = new VoteInterestModuleSpec(VoteInterestCategory.Kingdom, 5f, false),
			["kingdomStabilityNextDayOnce"] = new VoteInterestModuleSpec(VoteInterestCategory.Kingdom, 5f, false),
			["clanInfluence"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 0f, false, 30f, 0.5f),
			["clanInfluencePerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 0.5f, true),
			["clanInfluenceNextDayOnce"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 30f, false),
			["clanLeaderRelationOnce"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 3f, false),
			["partySizeLimit"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 15f, true),
			// The two runtime halves of partySizeLimit are summed, so each carries half the weight.
			["partySizeLimitClanLeader"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 30f, true),
			["partySizeLimitClanLords"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 30f, true),
			["soldierTroopXp"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 0f, false, 300f, 10f),
			["soldierTroopXpPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 10f, true),
			["soldierTroopXpOnce"] = new VoteInterestModuleSpec(VoteInterestCategory.Clan, 300f, false),
			["heroGold"] = new VoteInterestModuleSpec(VoteInterestCategory.Hero, 0f, false, 5000f, 100f),
			["heroGoldPerDay"] = new VoteInterestModuleSpec(VoteInterestCategory.Hero, 100f, true),
			["heroGoldNextDayOnce"] = new VoteInterestModuleSpec(VoteInterestCategory.Hero, 5000f, false)
		};

	// A composite module may be stored alongside its expanded runtime children; count only the children.
	private static readonly Dictionary<string, string[]> VoteInterestCompositeChildren =
		new Dictionary<string, string[]>(StringComparer.Ordinal)
		{
			["clanInfluence"] = new[] { "clanInfluencePerDay", "clanInfluenceNextDayOnce" },
			["soldierTroopXp"] = new[] { "soldierTroopXpPerDay", "soldierTroopXpOnce" },
			["heroGold"] = new[] { "heroGoldPerDay", "heroGoldNextDayOnce" },
			["kingdomStability"] = new[] { "kingdomStabilityNextDayOnce" },
			["partySizeLimit"] = new[] { "partySizeLimitClanLeader", "partySizeLimitClanLords" }
		};

	private static readonly System.Reflection.FieldInfo VoteInterestInvertedField =
		AccessTools.Field(typeof(KingdomPolicyDecision), "_isInvertedDecision");

	private static readonly Dictionary<string, VoteInterestCacheEntry> _voteInterestCache =
		new Dictionary<string, VoteInterestCacheEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> _voteInterestLoggedKeys = new HashSet<string>(StringComparer.Ordinal);

	private static Campaign _voteInterestCacheCampaign;

	private static int _voteInterestLoggedDay = -1;

	private static int _voteInterestSettingsTick;

	private static bool _voteInterestSettingsLoaded;

	private static bool _voteInterestEnabled = true;

	private static float _voteInterestWeight = 1f;

	private static float _voteRelationWeight = 1f;

	private static bool _voteInterestFailureLogged;

	private static void Patch_KingdomPolicyDecision_DetermineSupport_Postfix(
		KingdomPolicyDecision __instance,
		Clan clan,
		DecisionOutcome possibleOutcome,
		ref float __result,
		bool __runOriginal)
	{
		// A skipped original means another patch (vote deals, bilateral diplomacy) already fixed
		// this score; adding interest on top would break promised votes.
		if (!__runOriginal
			|| clan?.Leader == null
			|| !(possibleOutcome is KingdomPolicyDecision.PolicyDecisionOutcome outcome))
		{
			return;
		}
		try
		{
			PolicyObject policy = __instance?.Policy;
			if (policy == null || !IsDynamicPolicyId(policy.StringId))
			{
				return;
			}
			// The proposer's own preference drives vanilla ShouldBeCancelled and the NPC-ruler
			// adoption guards; changing it could cancel the agenda, so it is left untouched.
			Clan proposerClan = __instance.ProposerClan;
			if (proposerClan == null || proposerClan == clan)
			{
				return;
			}
			RefreshVoteInterestSettings();
			if (!_voteInterestEnabled || (_voteInterestWeight <= 0f && _voteRelationWeight <= 0f))
			{
				return;
			}
			VoteInterestCacheEntry entry = TryGetVoteInterestEntry(policy.StringId);
			if (entry == null)
			{
				return;
			}
			string clanId = clan.StringId ?? string.Empty;
			float interest = 0f;
			string detail = string.Empty;
			if (_voteInterestWeight > 0f && entry.InterestByClanId.TryGetValue(clanId, out float rawInterest))
			{
				interest = rawInterest * _voteInterestWeight;
				entry.DetailByClanId.TryGetValue(clanId, out detail);
			}
			bool isInverted = IsVoteInterestInvertedDecision(__instance);
			float relation = ComputeVoteRelation(entry, proposerClan, clan, isInverted);
			float delta = (isInverted ? -interest : interest) + relation;
			float scale = outcome.ShouldDecisionBeEnforced ? 60f : -100f;
			float original = __result;
			if (outcome.ShouldDecisionBeEnforced)
			{
				LogVoteInterestOnce(policy, clan, isInverted, original / scale, interest, relation, detail);
			}
			if (Math.Abs(delta) < 0.0001f)
			{
				return;
			}
			__result = original + delta * scale;
		}
		catch (Exception ex)
		{
			if (!_voteInterestFailureLogged)
			{
				_voteInterestFailureLogged = true;
				PolicySystemLog.Failure("Vote", "vote-interest-failed", ex.Message, ex.ToString());
			}
		}
	}

	private static bool IsVoteInterestInvertedDecision(KingdomPolicyDecision decision)
	{
		try
		{
			return VoteInterestInvertedField?.GetValue(decision) is bool inverted && inverted;
		}
		catch
		{
			return false;
		}
	}

	private static void RefreshVoteInterestSettings()
	{
		int now = Environment.TickCount;
		if (_voteInterestSettingsLoaded && unchecked(now - _voteInterestSettingsTick) < 2000)
		{
			return;
		}
		_voteInterestSettingsLoaded = true;
		_voteInterestSettingsTick = now;
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return;
			}
			_voteInterestEnabled = settings.PolicyVoteInterestEnabled;
			_voteInterestWeight = ClampVoteInterest(settings.PolicyVoteInterestWeight, 0f, 3f);
			_voteRelationWeight = ClampVoteInterest(settings.PolicyVoteRelationWeight, 0f, 3f);
		}
		catch
		{
		}
	}

	private static float ClampVoteInterest(float value, float min, float max)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0f;
		}
		return value < min ? min : value > max ? max : value;
	}

	// Hot path: one registry lookup plus a dictionary read. The full effect scan runs at most once
	// per policy per campaign day, or when the policy record itself is rewritten.
	private static VoteInterestCacheEntry TryGetVoteInterestEntry(string policyObjectId)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		string id = (policyObjectId ?? string.Empty).Trim();
		if (behavior == null
			|| id.Length == 0
			|| behavior._quarantinedDynamicPolicyIds.Contains(id)
			|| !behavior._dynamicPolicyRegistry.TryGetValue(id, out string raw)
			|| string.IsNullOrEmpty(raw))
		{
			return null;
		}
		if (!ReferenceEquals(_voteInterestCacheCampaign, Campaign.Current))
		{
			_voteInterestCache.Clear();
			_voteInterestLoggedKeys.Clear();
			_voteInterestCacheCampaign = Campaign.Current;
		}
		int day = GetCurrentCampaignDay();
		if (_voteInterestCache.TryGetValue(id, out VoteInterestCacheEntry entry) && entry.Day == day)
		{
			if (ReferenceEquals(entry.Raw, raw))
			{
				return entry;
			}
			if (string.Equals(entry.Raw, raw, StringComparison.Ordinal))
			{
				// Same content re-stored as a new string; adopt the reference so later calls skip the compare.
				entry.Raw = raw;
				return entry;
			}
		}
		entry = behavior.BuildVoteInterestEntry(raw, day);
		_voteInterestCache[id] = entry;
		return entry;
	}

	// R > 0 pushes toward the enforce outcome. Normally it follows the agenda proposer. For the AF
	// natural-expiry abolition agenda the ruler is a forced proposer, so friends of the policy's
	// original proposer lean toward keeping the policy (rejecting the abolition) instead.
	private static float ComputeVoteRelation(VoteInterestCacheEntry entry, Clan agendaProposer, Clan clan, bool isInverted)
	{
		if (_voteRelationWeight <= 0f)
		{
			return 0f;
		}
		Clan reference = agendaProposer;
		float direction = 1f;
		if (entry.IsSystemExpiryAgenda && isInverted)
		{
			reference = ResolveClanById(entry.PolicyProposerClanId);
			direction = -1f;
		}
		Hero referenceLeader = reference?.Leader;
		if (reference == null || reference == clan || referenceLeader == null || referenceLeader == clan.Leader)
		{
			return 0f;
		}
		return direction * _voteRelationWeight
			* ClampVoteInterest(clan.Leader.GetRelation(referenceLeader) / 100f, -VoteRelationMaxAbs, VoteRelationMaxAbs);
	}

	private VoteInterestCacheEntry BuildVoteInterestEntry(string raw, int day)
	{
		VoteInterestCacheEntry entry = new VoteInterestCacheEntry { Day = day, Raw = raw };
		DynamicPolicySaveData data;
		try
		{
			data = JsonConvert.DeserializeObject<DynamicPolicySaveData>(raw);
		}
		catch
		{
			return entry;
		}
		if (data == null)
		{
			return entry;
		}
		VoteInterestContext context = new VoteInterestContext
		{
			Source = data.Source ?? string.Empty,
			TargetKingdomId = data.OwnerKingdomId ?? string.Empty,
			IssuerKingdomId = FirstNonEmpty(data.IssuerKingdomId, data.OwnerKingdomId),
			ProposerClanId = data.ProposerClanId ?? string.Empty
		};
		entry.KingdomId = context.TargetKingdomId;
		entry.PolicyProposerClanId = data.ProposerClanId ?? string.Empty;
		entry.IsSystemExpiryAgenda = string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase);
		List<PolicyEffectInstanceSaveData> instances = CollectVoteInterestInstances(data, context);
		if (instances.Count == 0)
		{
			return entry;
		}
		Dictionary<string, float> totals = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> parts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> presentModules = new HashSet<string>(
			instances.Select(instance => (instance.ModuleId ?? string.Empty).Trim()),
			StringComparer.Ordinal);
		PolicyTargetWorldSnapshot snapshot = null;
		foreach (PolicyEffectInstanceSaveData instance in instances)
		{
			string moduleId = (instance.ModuleId ?? string.Empty).Trim();
			if (!VoteInterestModuleSpecs.TryGetValue(moduleId, out VoteInterestModuleSpec spec)
				|| (VoteInterestCompositeChildren.TryGetValue(moduleId, out string[] children)
					&& children.Any(presentModules.Contains)))
			{
				continue;
			}
			float strength = ComputeVoteInterestStrength(instance, spec);
			if (Math.Abs(strength) < 0.001f)
			{
				continue;
			}
			VoteInterestTargets targets = ResolveVoteInterestTargets(instance, context, ref snapshot);
			Dictionary<string, float> share = DistributeVoteInterest(spec.Category, targets, context);
			foreach (KeyValuePair<string, float> pair in share)
			{
				float contribution = strength * pair.Value;
				if (Math.Abs(contribution) < 0.001f)
				{
					continue;
				}
				totals.TryGetValue(pair.Key, out float current);
				totals[pair.Key] = current + contribution;
				if (!parts.TryGetValue(pair.Key, out List<string> list))
				{
					list = new List<string>();
					parts[pair.Key] = list;
				}
				list.Add(moduleId + (contribution >= 0f ? "+" : "") + contribution.ToString("0.00", CultureInfo.InvariantCulture));
			}
		}
		foreach (KeyValuePair<string, float> pair in totals)
		{
			entry.InterestByClanId[pair.Key] = ClampVoteInterest(pair.Value, -VoteInterestMaxAbs, VoteInterestMaxAbs);
			entry.DetailByClanId[pair.Key] = parts.TryGetValue(pair.Key, out List<string> list)
				? string.Join(",", list.Take(8))
				: string.Empty;
		}
		return entry;
	}

	// Effect source priority: the live bundle (abolition/renewal votes), then the pending player
	// payload (first adoption vote), then the NPC ruler record.
	private List<PolicyEffectInstanceSaveData> CollectVoteInterestInstances(DynamicPolicySaveData data, VoteInterestContext context)
	{
		string activeEffectId = (data.ActiveEffectId ?? string.Empty).Trim();
		if (activeEffectId.Length > 0
			&& _activePolicyEffects.TryGetValue(activeEffectId, out string activeRaw)
			&& !string.IsNullOrWhiteSpace(activeRaw))
		{
			try
			{
				ActivePolicyEffectSaveData active = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(activeRaw);
				if (active?.ModuleEffects != null && active.ModuleEffects.Count > 0)
				{
					context.Scope = FirstNonEmpty(active.ScopeKind, context.Scope);
					context.TargetKingdomId = FirstNonEmpty(active.TargetKingdomId, context.TargetKingdomId);
					context.IssuerKingdomId = FirstNonEmpty(active.IssuerKingdomId, context.IssuerKingdomId);
					context.ProposerClanId = FirstNonEmpty(active.ProposerClanId, context.ProposerClanId);
					context.SourceSettlementIds = NormalizeIdList(active.TargetFiefIds);
					return active.ModuleEffects.Where(instance => instance != null).ToList();
				}
			}
			catch
			{
			}
		}
		if (!string.IsNullOrWhiteSpace(data.PlayerPayloadJson))
		{
			try
			{
				PendingPlayerPolicyAgendaSaveData pending =
					JsonConvert.DeserializeObject<PendingPlayerPolicyAgendaSaveData>(data.PlayerPayloadJson);
				if (pending?.ModuleEffects != null && pending.ModuleEffects.Count > 0)
				{
					PolicyDraftRequest request = pending.Request;
					context.Scope = FirstNonEmpty(request?.ScopeKind, context.Scope);
					context.TargetKingdomId = FirstNonEmpty(request?.PlayerKingdomId, context.TargetKingdomId);
					context.IssuerKingdomId = FirstNonEmpty(request?.IssuerKingdomId, context.IssuerKingdomId);
					context.ProposerClanId = FirstNonEmpty(request?.ProposerClanId, context.ProposerClanId);
					context.SourceSettlementIds = NormalizeIdList(request?.SelectedFiefIds);
					return pending.ModuleEffects.Where(instance => instance != null).ToList();
				}
			}
			catch
			{
			}
		}
		if (string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase)
			&& NpcRulerPolicyBehavior.TryGetPolicyModuleEffectsForExternal(data.RecordId, out List<PolicyEffectInstanceSaveData> npcEffects))
		{
			context.Scope = PolicyEffectScopes.Kingdom;
			return npcEffects;
		}
		return new List<PolicyEffectInstanceSaveData>();
	}

	private static float ComputeVoteInterestStrength(PolicyEffectInstanceSaveData instance, VoteInterestModuleSpec spec)
	{
		JObject payload = instance.Payload as JObject;
		if (payload == null)
		{
			return 0f;
		}
		float durationFactor = ComputeVoteInterestDurationFactor(instance);
		float strength = 0f;
		if (spec.ValueBaseline > 0f)
		{
			strength += ReadVoteInterestNumber(payload, "value") / spec.ValueBaseline
				* (spec.ValueIsDaily ? durationFactor : 1f);
		}
		if (spec.OnceBaseline > 0f)
		{
			strength += ReadVoteInterestNumber(payload, "onceDelta") / spec.OnceBaseline;
		}
		if (spec.DailyBaseline > 0f)
		{
			strength += ReadVoteInterestNumber(payload, "dailyDelta") / spec.DailyBaseline * durationFactor;
		}
		return ClampVoteInterest(strength, -VoteInterestPartMaxAbs, VoteInterestPartMaxAbs);
	}

	// Permanent effects (EndDay <= StartDay) count fully; short-lived daily effects are discounted.
	private static float ComputeVoteInterestDurationFactor(PolicyEffectInstanceSaveData instance)
	{
		float days = instance.EndDay - instance.StartDay;
		if (float.IsNaN(days) || float.IsInfinity(days) || days <= 0f || instance.EndDay <= 0f)
		{
			return 1f;
		}
		return ClampVoteInterest(days / VoteInterestShortDurationDays, 0.2f, 1f);
	}

	private static float ReadVoteInterestNumber(JObject payload, string key)
	{
		JToken token = payload[key];
		if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
		{
			return 0f;
		}
		float value = token.Value<float>();
		return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
	}

	private static VoteInterestTargets ResolveVoteInterestTargets(
		PolicyEffectInstanceSaveData instance,
		VoteInterestContext context,
		ref PolicyTargetWorldSnapshot snapshot)
	{
		VoteInterestTargets targets = new VoteInterestTargets();
		PolicyEffectCanonicalTargetSet set = instance.TargetSet;
		if (set == null)
		{
			return targets;
		}
		targets.FollowCurrentRulingClan = set.FollowCurrentRulingClan;
		foreach (string id in (set.TownIds ?? new List<string>())
			.Concat(set.SettlementIds ?? new List<string>())
			.Concat(set.ParentSettlementIds ?? new List<string>()))
		{
			AddVoteInterestSettlement(targets, id);
		}
		foreach (string id in set.VillageIds ?? new List<string>())
		{
			AddVoteInterestSettlement(targets, id);
		}
		AddVoteInterestIds(targets.ClanIds, set.ClanIds);
		AddVoteInterestIds(targets.KingdomIds, set.KingdomIds);
		AddVoteInterestIds(targets.HeroIds, set.HeroIds);
		// Pending agendas may carry only unresolved TargetPlans; resolve them against the live world
		// the same way registration does, once per cache build.
		List<PolicyTargetPlanSaveData> plans = PolicyTargetPlanResolver.NormalizePlans(set.TargetPlans);
		if (plans.Count == 0)
		{
			return targets;
		}
		snapshot ??= PolicyTargetSemanticRouter.CaptureWorldSnapshot();
		PolicyTargetPlanResolutionContext resolutionContext = new PolicyTargetPlanResolutionContext
		{
			Scope = context.Scope ?? string.Empty,
			TargetKingdomId = context.TargetKingdomId ?? string.Empty,
			IssuerKingdomId = context.IssuerKingdomId ?? string.Empty,
			PlayerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
			ProposerClanId = context.ProposerClanId ?? string.Empty,
			SourceSettlementIds = context.SourceSettlementIds ?? new List<string>(),
			AllowPersistedValidatedReferences = true,
			Snapshot = snapshot
		};
		foreach (PolicyTargetPlanSaveData plan in plans)
		{
			if (!PolicyTargetPlanResolver.TryResolve(plan, resolutionContext, out PolicyTargetPlanResolution resolution, out _)
				|| resolution == null)
			{
				continue;
			}
			AddVoteInterestIds(targets.ClanIds, resolution.ClanIds);
			AddVoteInterestIds(targets.KingdomIds, resolution.KingdomIds);
			foreach (string primaryId in PolicyTargetPlanResolver.ExpandPrimarySettlementIds(resolution, snapshot))
			{
				AddVoteInterestSettlement(targets, primaryId);
			}
		}
		return targets;
	}

	private static void AddVoteInterestIds(HashSet<string> set, IEnumerable<string> ids)
	{
		foreach (string id in ids ?? Enumerable.Empty<string>())
		{
			string normalized = (id ?? string.Empty).Trim();
			if (normalized.Length > 0)
			{
				set.Add(normalized);
			}
		}
	}

	private static void AddVoteInterestSettlement(VoteInterestTargets targets, string id)
	{
		string normalized = (id ?? string.Empty).Trim();
		if (normalized.Length == 0)
		{
			return;
		}
		Settlement settlement = Settlement.Find(normalized);
		if (settlement == null)
		{
			return;
		}
		if (settlement.IsTown || settlement.IsCastle)
		{
			targets.PrimaryIds.Add(normalized);
		}
		else if (settlement.IsVillage)
		{
			targets.VillageIds.Add(normalized);
		}
	}

	// Returns clanId -> share multiplier for one effect. Fief shares scale with how much of the
	// targeted land the clan owns (town 1.0, castle 0.6, village 0.3), capped so a clan's interest
	// cannot grow without bound through sheer fief count.
	private static Dictionary<string, float> DistributeVoteInterest(
		VoteInterestCategory category,
		VoteInterestTargets targets,
		VoteInterestContext context)
	{
		Dictionary<string, float> share = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		switch (category)
		{
			case VoteInterestCategory.Fief:
			case VoteInterestCategory.Village:
				DistributeVoteInterestFiefs(category, targets, context, share);
				break;
			case VoteInterestCategory.Kingdom:
				foreach (Kingdom kingdom in ResolveVoteInterestKingdoms(targets, context))
				{
					foreach (Clan clan in kingdom.Clans ?? Enumerable.Empty<Clan>())
					{
						if (clan == null || clan.IsEliminated || string.IsNullOrEmpty(clan.StringId))
						{
							continue;
						}
						AddVoteInterestShare(share, clan.StringId,
							kingdom.RulingClan == clan ? VoteInterestKingdomScaleRuler : VoteInterestKingdomScaleVassal);
					}
				}
				break;
			case VoteInterestCategory.Clan:
				foreach (string clanId in ResolveVoteInterestClanIds(targets, context))
				{
					share[clanId] = VoteInterestClanScale;
				}
				break;
			case VoteInterestCategory.Hero:
				foreach (string heroId in targets.HeroIds)
				{
					Hero hero = Hero.Find(heroId);
					Clan clan = hero?.Clan;
					if (clan == null || string.IsNullOrEmpty(clan.StringId))
					{
						continue;
					}
					AddVoteInterestShare(share, clan.StringId, clan.Leader == hero ? VoteInterestClanScale : VoteInterestClanScale * 0.5f);
				}
				foreach (string clanId in share.Keys.ToList())
				{
					share[clanId] = Math.Min(share[clanId], VoteInterestClanScale * 1.5f);
				}
				break;
		}
		return share;
	}

	private static void DistributeVoteInterestFiefs(
		VoteInterestCategory category,
		VoteInterestTargets targets,
		VoteInterestContext context,
		Dictionary<string, float> share)
	{
		HashSet<string> primaryIds = new HashSet<string>(targets.PrimaryIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> villageIds = new HashSet<string>(targets.VillageIds, StringComparer.OrdinalIgnoreCase);
		// Selectors that were never materialized to settlements: expand clan/kingdom targets to their fiefs.
		if (primaryIds.Count == 0 && villageIds.Count == 0)
		{
			foreach (string clanId in targets.ClanIds)
			{
				Clan clan = ResolveClanById(clanId);
				foreach (Settlement fief in clan?.Settlements ?? Enumerable.Empty<Settlement>())
				{
					AddVoteInterestFiefId(fief, primaryIds, villageIds);
				}
			}
			foreach (Kingdom kingdom in ResolveVoteInterestKingdoms(targets, context))
			{
				foreach (Settlement fief in kingdom.Settlements ?? Enumerable.Empty<Settlement>())
				{
					AddVoteInterestFiefId(fief, primaryIds, villageIds);
				}
			}
		}
		Dictionary<string, float> exposure = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		if (category == VoteInterestCategory.Village)
		{
			if (villageIds.Count == 0)
			{
				foreach (string primaryId in primaryIds)
				{
					foreach (Village village in Settlement.Find(primaryId)?.BoundVillages ?? Enumerable.Empty<Village>())
					{
						if (!string.IsNullOrEmpty(village?.Settlement?.StringId))
						{
							villageIds.Add(village.Settlement.StringId);
						}
					}
				}
			}
			foreach (string villageId in villageIds)
			{
				AddVoteInterestExposure(exposure, Settlement.Find(villageId)?.OwnerClan, 0.3f);
			}
		}
		else
		{
			foreach (string primaryId in primaryIds)
			{
				Settlement settlement = Settlement.Find(primaryId);
				if (settlement != null)
				{
					AddVoteInterestExposure(exposure, settlement.OwnerClan, settlement.IsTown ? 1f : 0.6f);
				}
			}
		}
		foreach (KeyValuePair<string, float> pair in exposure)
		{
			share[pair.Key] = Math.Min(pair.Value * VoteInterestFiefScale, VoteInterestFiefExposureMax);
		}
	}

	private static void AddVoteInterestFiefId(Settlement settlement, HashSet<string> primaryIds, HashSet<string> villageIds)
	{
		string id = settlement?.StringId;
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		if (settlement.IsTown || settlement.IsCastle)
		{
			primaryIds.Add(id);
		}
		else if (settlement.IsVillage)
		{
			villageIds.Add(id);
		}
	}

	private static void AddVoteInterestExposure(Dictionary<string, float> exposure, Clan owner, float weight)
	{
		if (owner == null || owner.IsEliminated || string.IsNullOrEmpty(owner.StringId))
		{
			return;
		}
		exposure.TryGetValue(owner.StringId, out float current);
		exposure[owner.StringId] = current + weight;
	}

	private static void AddVoteInterestShare(Dictionary<string, float> share, string clanId, float weight)
	{
		share.TryGetValue(clanId, out float current);
		share[clanId] = current + weight;
	}

	private static IEnumerable<Kingdom> ResolveVoteInterestKingdoms(VoteInterestTargets targets, VoteInterestContext context)
	{
		IEnumerable<string> ids = targets.KingdomIds.Count > 0
			? targets.KingdomIds
			: new[] { context.TargetKingdomId ?? string.Empty };
		return ids.Select(ResolveKingdomStatic).Where(kingdom => kingdom != null && !kingdom.IsEliminated).Distinct();
	}

	private static IEnumerable<string> ResolveVoteInterestClanIds(VoteInterestTargets targets, VoteInterestContext context)
	{
		HashSet<string> clanIds = new HashSet<string>(targets.ClanIds, StringComparer.OrdinalIgnoreCase);
		if (targets.FollowCurrentRulingClan)
		{
			string rulingId = ResolveKingdomStatic(context.TargetKingdomId)?.RulingClan?.StringId;
			if (!string.IsNullOrEmpty(rulingId))
			{
				clanIds.Add(rulingId);
			}
		}
		if (clanIds.Count == 0 && targets.KingdomIds.Count > 0)
		{
			foreach (Kingdom kingdom in ResolveVoteInterestKingdoms(targets, context))
			{
				foreach (Clan clan in kingdom.Clans ?? Enumerable.Empty<Clan>())
				{
					if (clan != null && !clan.IsEliminated && !string.IsNullOrEmpty(clan.StringId))
					{
						clanIds.Add(clan.StringId);
					}
				}
			}
		}
		return clanIds;
	}

	// One line per (policy, clan, inverted) per campaign day, so UI refreshes cannot flood the log.
	private static void LogVoteInterestOnce(
		PolicyObject policy,
		Clan clan,
		bool isInverted,
		float politicalScore,
		float interest,
		float relation,
		string detail)
	{
		int day = GetCurrentCampaignDay();
		if (day != _voteInterestLoggedDay)
		{
			_voteInterestLoggedDay = day;
			_voteInterestLoggedKeys.Clear();
		}
		string key = (policy.StringId ?? string.Empty) + "|" + (clan.StringId ?? string.Empty) + "|" + (isInverted ? "1" : "0");
		if (_voteInterestLoggedKeys.Count > 4000 || !_voteInterestLoggedKeys.Add(key))
		{
			return;
		}
		float effectiveInterest = isInverted ? -interest : interest;
		PolicySystemLog.Write("Vote", "support-adjusted",
			"policy=" + (policy.StringId ?? string.Empty)
			+ " clan=" + (clan.StringId ?? string.Empty)
			+ " inverted=" + (isInverted ? "true" : "false")
			+ " S=" + politicalScore.ToString("0.00", CultureInfo.InvariantCulture)
			+ " U=" + effectiveInterest.ToString("0.00", CultureInfo.InvariantCulture)
			+ " R=" + relation.ToString("0.00", CultureInfo.InvariantCulture)
			+ " total=" + (politicalScore + effectiveInterest + relation).ToString("0.00", CultureInfo.InvariantCulture)
			+ (string.IsNullOrEmpty(detail) ? string.Empty : " parts=" + detail));
	}
}
