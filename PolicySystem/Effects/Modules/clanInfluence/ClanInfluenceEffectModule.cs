using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ClanInfluenceEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ClanInfluencePayload : PolicyEffectPayload
{
	[JsonProperty("onceDelta", Required = Required.Always)]
	public float OnceDelta { get; set; }

	[JsonProperty("dailyDelta", Required = Required.Always)]
	public float DailyDelta { get; set; }
}

internal sealed class ClanInfluenceEffectModule : PolicyEffectModuleBase<ClanInfluencePayload>, IPolicyEffectCompositeModule
{
	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"clanInfluenceNextDayOnce",
		"clanInfluencePerDay"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "clanInfluence",
		order: 100,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom, PolicyEffectTargetKind.Hero },
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_7"), PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_8"), PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_9"), PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_10"), PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_11"), PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_12"), "influence", "noble power", "centralization", "royal authority" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_clanInfluence_retrievalText", "Clan influence, aristocratic power, lord authority, centralization, weakening vassals, and strengthening royal power; a one-time change on the next game day and subsequent daily changes may both be specified."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_clanInfluence_catalogSummary", "Clan influence: one-time and daily changes"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_clanInfluence_mainInstruction", "If the policy changes influence for the issuer's clan or clans in the target kingdom, specify onceDelta and dailyDelta separately; both may be 0. The one-time portion settles on the game day following policy adoption, and the daily portion starts on the same day. Centralization usually gives negative values to other clans and positive values to the issuer's clan. Do not output internal module IDs."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_clanInfluence_postprocessRule", "The output structure must be {\"onceDelta\": number, \"dailyDelta\": number}. Values must be finite and may be positive, negative, or 0. onceDelta executes only once on the next game day; dailyDelta executes each day while the policy is active, without reclaiming historical accumulation. If both are 0, omit the effect."),
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Governance,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PointsPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "clanInfluence",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_13"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_clanInfluence_editableUnderstandingPrompt", "Clan influence represents political capital, aristocratic power, the degree of centralization, and vassal standing for the issuer's clan or clans in the target kingdom. When a policy directly changes these power relationships, clan influence is an actual consequence. Centralization usually weakens other clans and strengthens the issuer's clan, but must still follow the original policy and its execution path."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_clanInfluence_editableEvaluationPrompt", "The immediate change settles once on the game day following policy adoption; the ongoing change settles daily starting on that same day while the policy remains active. Ordinary native policies commonly change daily influence by about ±0.2 to ±1, and exceptional monarch-level policies may reach about +3. A custom policy that explicitly changes power structures should not remain at symbolic changes of 0.1 to 0.5 without reason. One-time changes: mild prestige or interest adjustments usually warrant ±10 to ±25 per clan; explicit political rewards, punishments, or transfers of power warrant ±25 to ±60; major centralization, weakening vassals, granting privileges, or widespread noble conflict warrants ±60 to ±120; restructuring governance or handling an existential state crisis may warrant ±120 to ±250. Daily changes: mild continuing effects warrant ±0.5 to ±1; explicit institutional changes warrant ±1 to ±3; major sustained redistribution of power warrants ±3 to ±6; extreme centralization, systematic deprivation, or extraordinary political mobilization warrants ±6 to ±10. Use both components only when the policy causes both an immediate change in political capital on adoption and a subsequent ongoing institution. Do not mechanically double-count the same effect or divide per-clan values because there are many target clans or the policy is long-term or permanent. Low fiscal cost does not mean low influence: judge the depth of power changes, execution authority, noble compliance or resistance, and sustained execution capability."),
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	public override bool TryNormalizeTypedPayload(
		ClanInfluencePayload payload,
		string scope,
		out ClanInfluencePayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (!IsFinite(payload.OnceDelta) || !IsFinite(payload.DailyDelta))
		{
			error = "clanInfluence onceDelta and dailyDelta must be finite numbers";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		ClanInfluencePayload payload,
		PolicyEffectFundingContext funding,
		out ClanInfluencePayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		float scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1f;
		ClanInfluencePayload candidate = new ClanInfluencePayload
		{
			ModuleId = ModuleDescriptor.Id,
			SchemaVersion = ModuleDescriptor.PayloadSchemaVersion,
			OnceDelta = payload.OnceDelta * scale,
			DailyDelta = payload.DailyDelta * scale
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
		if (context == null || !(payload is ClanInfluencePayload typed))
		{
			error = "clanInfluence requires a valid compilation context and payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out ClanInfluencePayload normalized, out error))
		{
			return false;
		}

		List<PolicyEffectCompositeChild> expanded = new List<PolicyEffectCompositeChild>(2);
		if (normalized.OnceDelta != 0f)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "clanInfluenceNextDayOnce",
				Payload = new ClanInfluenceNextDayOncePayload
				{
					ModuleId = "clanInfluenceNextDayOnce",
					SchemaVersion = 1,
					Value = normalized.OnceDelta
				}
			});
		}
		if (normalized.DailyDelta != 0f)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "clanInfluencePerDay",
				Payload = new ClanInfluencePerDayPayload
				{
					ModuleId = "clanInfluencePerDay",
					SchemaVersion = 1,
					Value = normalized.DailyDelta
				}
			});
		}

		children = expanded;
		error = string.Empty;
		return true;
	}

	public override string DescribeTypedPayload(ClanInfluencePayload payload)
	{
		return payload == null
			? string.Empty
			: PolicyCompatibilityTexts.Get("RawPayloadOnce") + payload.OnceDelta.ToString("0.###", CultureInfo.InvariantCulture)
				+ PolicyCompatibilityTexts.Get("RawPayloadDaily") + payload.DailyDelta.ToString("0.###", CultureInfo.InvariantCulture);
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("onceDelta", "dailyDelta"),
			["properties"] = new JObject
			{
				["onceDelta"] = new JObject { ["type"] = "number" },
				["dailyDelta"] = new JObject { ["type"] = "number" }
			},
			["additionalProperties"] = false
		};
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}
}
