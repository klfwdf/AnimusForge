using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.HeroGoldEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class HeroGoldPayload : PolicyEffectPayload
{
	[JsonProperty("onceDelta", Required = Required.Always)]
	public int OnceDelta { get; set; }

	[JsonProperty("dailyDelta", Required = Required.Always)]
	public int DailyDelta { get; set; }
}

internal sealed class HeroGoldDeltaPayload : PolicyEffectPayload
{
	[JsonProperty("value", Required = Required.Always)]
	public int Value { get; set; }
}

internal sealed class HeroGoldEffectModule : PolicyEffectModuleBase<HeroGoldPayload>, IPolicyEffectCompositeModule
{
	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		"heroGoldNextDayOnce",
		"heroGoldPerDay"
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "heroGold",
		order: 130,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Hero, PolicyEffectTargetKind.Settlement },
		targetKinds: new[] { PolicyEffectTargetKind.Hero },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_4"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_5"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_6"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_7"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_8"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_9"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_10"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_11"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_12"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_13"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_14"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_15"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_16"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_17"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_18"), PolicyCompatibilityTexts.Get("Module_heroGold_Original_Closeout_19"), "denars", "personal income", "salary", "army pay", "allowance", "subsidy", "fine", "confiscation" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_heroGold_retrievalText", "Personal income and denar balances of the player, policy publisher, rulers, lords, clan leaders, notables or native professions through army pay, salaries, trade, industry, subsidies, rewards, punishments or confiscations. Local army pay, salaries, allowances or fiscal subsidies with no other personal recipient mean positive denars for current owner-clan leaders of affected settlements. May specify a one-time change on the next game day and subsequent daily changes. Startup, upkeep, construction investment and administrative budgets are excluded."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_heroGold_catalogSummary", "Personal denar changes for characters and native professions"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_heroGold_mainInstruction", "When policy measures reasonably and directly change personal economic gains through army pay, salaries, trade, industry, subsidies, rewards, punishments or confiscations, the model judges affected identities, direction and intensity without requiring professions or exact denar amounts verbatim. Local army pay, salaries, allowances or fiscal subsidies with no other personal recipient default to positive denars for current owner-clan leaders of affected settlements; retain settlement binding and select settlement handles, never nationwide lord or soldier character sets. Use character targets only when the body directly names individuals or professions. Continuing income, stipends, salaries, allowances and regular subsidies default to dailyDelta with onceDelta 0; explicit single rewards, bonuses, arrears, fines and confiscations default to onceDelta with dailyDelta 0. Fill both only when both are explicit. Settle per final character; values may be positive, negative or 0. A lord owning multiple affected settlements settles only once. Each change is independent, creating neither a payer nor character-to-character transfers. Interpret income as fief taxation only if it explicitly involves fiefs, towns, castles or taxation. Ordinary policy costs, startup, upkeep, construction and administrative budgets are not personal denar changes. Do not output internal module IDs or selector syntax."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_heroGold_postprocessRule", "Output exactly {\"onceDelta\": integer, \"dailyDelta\": integer}, with no unknown fields. The body need not give exact amounts; select integers by semantic intensity. onceDelta settles once on the game day following adoption; dailyDelta settles daily starting that same day. Local army-pay or subsidy gains must be positive; do not map policy costs again as negative personal denars. Omit the effect if both values are 0."),
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Fiscal,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.DailyScheduler,
		aggregation: PolicyEffectAggregationKind.IntegerDelta,
		valueUnit: PolicyEffectValueUnit.GoldPerDay,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "heroGold",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_heroGold_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_heroGold_editableUnderstandingPrompt", "Personal denars are funds held by individual heroes, not virtual treasuries of clans, parties or settlements. The player and policy publisher may also be direct character targets. When policies reasonably and directly change personal economic gains through army pay, salaries, trade, industry income, subsidies, rewards, punishments or confiscations, personal denars are the actual consequence. The model independently judges affected characters or native professions; policies need not name professions or exact amounts verbatim. Local army pay, salaries, allowances or fiscal subsidies without another specified personal recipient default to the current owner-clan leader of the affected settlement; retain settlement-bound targets. Use character targets only when the body directly names individuals or professions. Continuing income, stipends, salaries, allowances and regular subsidies are usually ongoing changes; rewards, bonuses, back pay, fines and confiscations are usually one-time changes. Interpret income as fief taxation only when the body explicitly involves fiefs, towns, castles or taxation. Publication and upkeep costs, construction investment and general administrative budgets are excluded."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_heroGold_editableEvaluationPrompt", "One-time changes settle on the game day after policy adoption; daily changes settle each day while the policy is valid. Values apply per final character, not to a group total. Positive values are independent income or subsidies; negatives are personal burdens, fines or confiscations. Do not create character-to-character transfers. When the body gives no amount, choose concrete integers from wording intensity, policy coverage and resource commitments, rather than mechanically using range midpoints. Mild or limited daily income changes are usually ±50 to ±150 per person; regular explicit income policies ±150 to ±500; major or generous long-term policies ±500 to ±1500; only extreme, well-supported policies reach ±1500 to ±5000. Small one-time rewards or fines are usually ±500 to ±2000 per person; significant dispositions ±2000 to ±10000; major rewards or confiscations ±10000 to ±50000. Generic increases to lords' income should produce positive daily changes without additional one-time changes. Do not divide a group total among individuals because many targets exist, or count policy costs again as personal balance changes."),
		targetProjection: PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (!HeroGoldPayloadValidation.HasIntegerProperty(rawPayload, "onceDelta")
			|| !HeroGoldPayloadValidation.HasIntegerProperty(rawPayload, "dailyDelta"))
		{
			error = "heroGold onceDelta and dailyDelta must be 32-bit integers";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		HeroGoldPayload payload,
		string scope,
		out HeroGoldPayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.OnceDelta == int.MinValue || payload.DailyDelta == int.MinValue)
		{
			error = "heroGold does not accept int.MinValue";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		HeroGoldPayload payload,
		PolicyEffectFundingContext funding,
		out HeroGoldPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryNormalizeTypedPayload(payload, string.Empty, out HeroGoldPayload normalized, out error))
		{
			return false;
		}
		double scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1d;
		if (!HeroGoldPayloadValidation.TryScale(normalized.OnceDelta, scale, out int onceDelta)
			|| !HeroGoldPayloadValidation.TryScale(normalized.DailyDelta, scale, out int dailyDelta))
		{
			error = "heroGold funding scale exceeds the 32-bit integer range";
			return false;
		}
		HeroGoldPayload candidate = new HeroGoldPayload
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
		if (context == null || payload is not HeroGoldPayload typed)
		{
			error = "heroGold requires a valid compilation context and payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out HeroGoldPayload normalized, out error))
		{
			return false;
		}

		List<PolicyEffectCompositeChild> expanded = new List<PolicyEffectCompositeChild>(2);
		if (normalized.OnceDelta != 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "heroGoldNextDayOnce",
				Payload = new HeroGoldDeltaPayload
				{
					ModuleId = "heroGoldNextDayOnce",
					SchemaVersion = 1,
					Value = normalized.OnceDelta
				}
			});
		}
		if (normalized.DailyDelta != 0)
		{
			expanded.Add(new PolicyEffectCompositeChild
			{
				ModuleId = "heroGoldPerDay",
				Payload = new HeroGoldDeltaPayload
				{
					ModuleId = "heroGoldPerDay",
					SchemaVersion = 1,
					Value = normalized.DailyDelta
				}
			});
		}
		children = expanded;
		error = string.Empty;
		return true;
	}

	public override string DescribeTypedPayload(HeroGoldPayload payload)
	{
		return payload == null
			? string.Empty
			: PolicyCompatibilityTexts.Get("RawPayloadOnce") + payload.OnceDelta.ToString(CultureInfo.InvariantCulture)
				+ PolicyCompatibilityTexts.Get("RawPayloadDaily") + payload.DailyDelta.ToString(CultureInfo.InvariantCulture);
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("onceDelta", "dailyDelta"),
			["properties"] = new JObject
			{
				["onceDelta"] = new JObject { ["type"] = "integer" },
				["dailyDelta"] = new JObject { ["type"] = "integer" }
			},
			["additionalProperties"] = false
		};
	}
}

internal static class HeroGoldPayloadValidation
{
	internal static bool HasIntegerProperty(JToken rawPayload, string propertyName)
	{
		JToken value = rawPayload?[propertyName];
		if (value?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long number = value.Value<long>();
			return number >= int.MinValue && number <= int.MaxValue;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryScale(int value, double scale, out int result)
	{
		result = 0;
		if (double.IsNaN(scale) || double.IsInfinity(scale))
		{
			return false;
		}
		double rounded = Math.Round(value * scale, MidpointRounding.AwayFromZero);
		if (rounded < int.MinValue || rounded > int.MaxValue || rounded == int.MinValue)
		{
			return false;
		}
		result = (int)rounded;
		return true;
	}
}
