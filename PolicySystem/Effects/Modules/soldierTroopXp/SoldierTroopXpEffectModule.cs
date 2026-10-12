using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.SoldierTroopXpEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class SoldierTroopXpPayload : PolicyEffectPayload
{
	[JsonProperty("onceDelta", Required = Required.Always)]
	public int OnceDelta { get; set; }

	[JsonProperty("dailyDelta", Required = Required.Always)]
	public int DailyDelta { get; set; }
}

internal sealed class SoldierTroopXpDailyPayload : PolicyEffectPayload
{
	[JsonProperty("value", Required = Required.Always)]
	public int Value { get; set; }
}

// Composite children are normalized again by their runtime module. Keep this
// compiler-only shape integral so the once module's strict raw contract does
// not receive a JSON floating-point token from NumericPolicyEffectPayload.
internal sealed class SoldierTroopXpOnceChildPayload : PolicyEffectPayload<int>
{
}

internal sealed class SoldierTroopXpEffectModule
	: PolicyEffectModuleBase<SoldierTroopXpPayload>, IPolicyEffectCompositeModule
{
	internal const int MaximumOnceXpPerTroop = 5000;
	internal const int MaximumDailyXpPerTroop = 100;

	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"soldierTroopXpOnce",
		"soldierTroopXpPerDay"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "soldierTroopXp",
		order: 140,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero,
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_5"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_6"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_7"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_8"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_9"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_10"), PolicyCompatibilityTexts.Get("Module_soldierTroopXp_Original_Closeout_11"), "troop experience", "soldier training", "military reform", "drills", "reorganization" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_soldierTroopXp_retrievalText", "Training, reorganization, incentives, discipline, welfare, military reform or other reasonable mechanisms grant native troop experience to ordinary soldiers in all current official lord parties and town/castle garrisons of target clans, helping them reach native upgrade conditions. Judge from complete policy semantics, investment and causality. Does not directly upgrade or replace troops or affect heroes, prisoners, militia, recruitment, reinforcements or replenishment and growth of unrecruited volunteers."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_soldierTroopXp_catalogSummary", "Once/daily troop XP: target-clan lord parties and garrisons"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_soldierTroopXp_mainInstruction", "Judge independently from complete semantics, targets, investment, execution mechanisms and causality whether ordinary soldiers in target clan lord parties and fief garrisons jointly gain native troop experience. Do not mechanically select or exclude using single words such as pay, training, morale or discipline. Explicit single intensive training, reorganization, brief drills or one-time reform results use onceDelta with dailyDelta 0; standing, daily, long-term, continuing or institutionalized benefits use dailyDelta with onceDelta 0. Fill both only for explicit initial concentrated results followed by continuing training or equivalent two-stage benefits. Vague improved elite quality without a lasting institution or two-stage evidence defaults to onceDelta; permanent instructor corps, training institutions or fixed camps use dailyDelta. Exclude Hero skills, prisoners, militia, direct troop replacement, recruitment and reinforcements; policies only strengthening militia, recruitment, unrecruited volunteers or prisoners do not qualify. Lord parties and garrisons of one clan share one training result, not separate effects. Both values are per soldier, not diluted by clan, party, fief or soldier counts; multiply each stack count only once in game. Never convert between onceDelta and dailyDelta using policy duration. Do not output internal module IDs or selector syntax."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_soldierTroopXp_postprocessRule", "Output exactly {\"onceDelta\": integer, \"dailyDelta\": integer}; no unknown fields, fractions or negatives. onceDelta ranges 0 to 5000; dailyDelta 0 to 100. Both are native troop experience per eligible ordinary soldier; omit the effect if both are 0. Single intensive training uses onceDelta only; standing or daily training uses dailyDelta only; both require explicit initial reorganization followed by continuing drills. Never multiply dailyDelta by duration into onceDelta or divide onceDelta by days into dailyDelta. Both nonzero values stack on the first execution day: calibrate both total first-day intensity and continuing daily intensity."),
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "soldierTroopXp",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_soldierTroopXp_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_soldierTroopXp_editableUnderstandingPrompt", "Soldier elite training means that training, reorganization, incentives, discipline, welfare, military reform or other reasonable mechanisms grant native troop experience to ordinary soldiers in all current official lord parties and town/castle garrisons of target clans. The player's main party also counts as an official lord party when the player's clan is targeted. It may describe concentrated training results completed at policy activation or continuing training gains during policy validity; use both only when the body explicitly specifies both arrangements. Generic improvement of elite quality without a standing institution usually means concentrated training results. Explicit standing instructor corps, permanent training institutions or fixed training camps mean continuing gains. This does not increase hero skills or affect prisoners, militia, caravans, villagers, bandits, messengers or temporary parties. It does not directly replace or upgrade troops, recruit, or increase soldier numbers. Unrecruited notable volunteer slots and volunteer replenishment/growth belong to another effect; use soldier elite training alongside that effect only if the policy separately grants experience to ordinary soldiers already in active lord parties or garrisons."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_soldierTroopXp_editableEvaluationPrompt", "Both values are experience per eligible ordinary soldier in lord parties and garrisons, not total Party or Clan XP, and are not diluted by target clan, party, fief or soldier counts. Native AI daily training baselines are approximately 10+tier*2 XP/soldier/day for ordinary lords and 15+tier*3 for clan leaders; adjacent-tier upgrade costs are approximately 100/300/550/900/1300/1700. One-time changes: 1–99 symbolic; 100–150 weak; 151–300 regular; 301–600 clearly elite; 601–1200 major reorganization; 1201–2500 very strong; 2501–5000 only for well-supported extreme policies. Daily changes: 1–5 mild; 6–15 moderate; 16–30 clearly institutionalized; 31–45 strong standing training; 46–60 extremely strong; 61–80 extraordinary wartime intensive training; 81–100 only for extreme policies. Daily 60 is already extremely strong: it accumulates 300 in 5 days and 600 in 10 days and stacks with native training. Judge values from wording, investment, organization and continuity. Never convert between the two timing semantics using policy days. When both are nonzero, they stack on the first execution day. Reaching XP thresholds only produces native upgradeable troop counts, not direct upgrades; healthy troop counts, wages, funds, items and special conditions still apply."),
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.FrozenCanonicalIds,
		allowIndependentClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject obj
			|| !HasBoundedInteger(obj["onceDelta"], 0, MaximumOnceXpPerTroop)
			|| !HasBoundedInteger(obj["dailyDelta"], 0, MaximumDailyXpPerTroop))
		{
			error = "soldierTroopXp must contain exactly two integers: onceDelta from 0 to 5000 and dailyDelta from 0 to 100";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		SoldierTroopXpPayload payload,
		string scope,
		out SoldierTroopXpPayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.OnceDelta < 0 || payload.OnceDelta > MaximumOnceXpPerTroop
			|| payload.DailyDelta < 0 || payload.DailyDelta > MaximumDailyXpPerTroop)
		{
			error = "soldierTroopXp values exceed the allowed range";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		SoldierTroopXpPayload payload,
		PolicyEffectFundingContext funding,
		out SoldierTroopXpPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryNormalizeTypedPayload(payload, string.Empty, out SoldierTroopXpPayload normalized, out error))
		{
			return false;
		}
		double scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1d;
		int onceDelta = (int)Math.Round(normalized.OnceDelta * scale, MidpointRounding.AwayFromZero);
		int dailyDelta = (int)Math.Round(normalized.DailyDelta * scale, MidpointRounding.AwayFromZero);
		SoldierTroopXpPayload candidate = new SoldierTroopXpPayload
		{
			ModuleId = ModuleDescriptor.Id,
			SchemaVersion = ModuleDescriptor.PayloadSchemaVersion,
			OnceDelta = onceDelta,
			DailyDelta = dailyDelta
		};
		return TryNormalizeTypedPayload(candidate, string.Empty, out fundedPayload, out error);
	}

	public bool TryExpand(
		PolicyEffectCompileContext context,
		PolicyEffectPayload payload,
		out IReadOnlyList<PolicyEffectCompositeChild> children,
		out string error)
	{
		children = Array.Empty<PolicyEffectCompositeChild>();
		error = string.Empty;
		if (context == null || payload is not SoldierTroopXpPayload typed)
		{
			error = "soldierTroopXp requires a valid compilation context and payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out SoldierTroopXpPayload normalized, out error))
		{
			return false;
		}
		List<PolicyEffectCompositeChild> expanded = new List<PolicyEffectCompositeChild>(2);
		if (normalized.OnceDelta > 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "soldierTroopXpOnce",
				Payload = new SoldierTroopXpOnceChildPayload
				{
					ModuleId = "soldierTroopXpOnce",
					SchemaVersion = 1,
					Value = normalized.OnceDelta
				}
			});
		}
		if (normalized.DailyDelta > 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "soldierTroopXpPerDay",
				Payload = new SoldierTroopXpDailyPayload
				{
					ModuleId = "soldierTroopXpPerDay",
					SchemaVersion = 1,
					Value = normalized.DailyDelta
				}
			});
		}
		children = expanded;
		error = string.Empty;
		return true;
	}

	public override string DescribeTypedPayload(SoldierTroopXpPayload payload)
	{
		return payload == null
			? string.Empty
			: PolicyCompatibilityTexts.Get("RawPayloadOnce") + payload.OnceDelta.ToString(CultureInfo.InvariantCulture)
				+ PolicyCompatibilityTexts.Get("RawPayloadDaily") + payload.DailyDelta.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("RawPayloadXpPerSoldier");
	}

	private static bool HasBoundedInteger(JToken token, int minimum, int maximum)
	{
		if (token?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long value = token.Value<long>();
			return value >= minimum && value <= maximum;
		}
		catch
		{
			return false;
		}
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("onceDelta", "dailyDelta"),
			["properties"] = new JObject
			{
				["onceDelta"] = new JObject
				{
					["type"] = "integer",
					["minimum"] = 0,
					["maximum"] = MaximumOnceXpPerTroop
				},
				["dailyDelta"] = new JObject
				{
					["type"] = "integer",
					["minimum"] = 0,
					["maximum"] = MaximumDailyXpPerTroop
				}
			},
			["additionalProperties"] = false
		};
	}
}
