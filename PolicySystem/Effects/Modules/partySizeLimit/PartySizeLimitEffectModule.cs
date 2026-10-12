using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.PolicyTargets;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.PartySizeLimitEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class PartySizeLimitPayload : PolicyEffectPayload<int>
{
}

internal sealed class PartySizeLimitRuntimePayload : PolicyEffectPayload<int>
{
}

internal sealed class PartySizeLimitEffectModule
	: PolicyEffectModuleBase<PartySizeLimitPayload>, IPolicyEffectCompositeModule
{
	internal const int MinimumValue = -100;
	internal const int MaximumValue = 100;
	internal const string ClanLeaderRuntimeModuleId = "partySizeLimitClanLeader";
	internal const string ClanLordsRuntimeModuleId = "partySizeLimitClanLords";

	private static readonly IReadOnlyCollection<string> RuntimeModules = new[]
	{
		ClanLeaderRuntimeModuleId,
		ClanLordsRuntimeModuleId
	};

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "partySizeLimit",
		order: 170,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { PolicyCompatibilityTexts.Get("Module_partySizeLimit_Original_Closeout_0"), PolicyCompatibilityTexts.Get("Module_partySizeLimit_Original_Closeout_1"), PolicyCompatibilityTexts.Get("Module_partySizeLimit_Original_Closeout_2"), PolicyCompatibilityTexts.Get("Module_partySizeLimit_Original_Closeout_3"), PolicyCompatibilityTexts.Get("Module_partySizeLimit_Original_Closeout_4"), "party size limit", "lord party capacity", "maximum troops" },
		retrievalText: PolicyGenerationPromptTexts.Get("Module_partySizeLimit_retrievalText", "Directly raises or lowers native party size limits for the ruler, the current owner clan of local-policy source settlements, specified clan lords or all lords of a target kingdom. Only official lord parties are affected, excluding garrisons, militia, caravans, villagers, messengers and other non-lord parties."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_partySizeLimit_catalogSummary", "Party limits for rulers, clan lords or all kingdom lords"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_partySizeLimit_mainInstruction", "Select only when the policy explicitly and directly changes party size limits for the ruler, a clan’s lords or all kingdom lords. Local-policy source or selected settlements map to their current owner clans and affect all their official lord parties. A ruler target affects only the official lord party personally led by the current ruler and follows succession dynamically. Clan targets affect all official lord parties of those clans; kingdom targets affect all current clan lords including mercenary clans. Do not use for army-formation tendencies, garrison capacity, militia, caravans, villagers, messengers, wages, recruitment counts or soldier experience."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_partySizeLimit_postprocessRule", "payload must be exactly {\"value\": integer}, with no unknown fields, fractions or non-finite values. value adds or removes a fixed headcount from each affected lord party’s native limit, from -100 to +100. Positive raises it; negative lowers it. Mild: 5 to 10; regular reforms: 10 to 25; major: 25 to 50; extreme: 50 to 80; 80 to 100 only for ruler-exclusive targets or exceptionally strong policy evidence. Nationwide policies affect many parties and usually use 5 to 20, generally no more than 30. Assess negatives cautiously on the same scale. Insufficient funding scales linearly by the actual funding ratio and rounds to an integer. Targets must explicitly be current owner clans of local-policy source or selected settlements, the current ruler, lords of one or more clans, or nationwide lords of a kingdom. Do not mix ruler-exclusive targets with broader clan, settlement or kingdom targets."),
		payloadPromptSchema: CreatePayloadSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.Composite,
		hook: PolicyEffectHook.PartyMemberSizeLimit,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PartySizePoints,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Custom,
		payloadSchemaVersion: 1,
		promptVisible: true,
		displayGroup: "partySizeLimit",
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_partySizeLimit_playerDisplayName"),
		editableUnderstandingPrompt: PolicyGenerationPromptTexts.Get("Module_partySizeLimit_editableUnderstandingPrompt", "Lords' party size limits represent direct changes to native size limits of each official lord party of the ruler, current owner clans of local-policy source settlements, specified clans or nationwide lords. Local-policy source or selected settlements map to their current owner clans and affect all their official lord parties. Ruler targets affect only the party personally led by the current ruler and transfer with succession. Clan targets affect all that clan's lord parties. Nationwide targets affect all current clans of the kingdom, including mercenary clans. Garrisons, militia, caravans, villagers, messengers and other non-official lord parties are unaffected. Do not interpret army formation willingness, army numbers, recruitment, garrison capacity, wages or soldier experience as party size limits."),
		editableEvaluationPrompt: PolicyGenerationPromptTexts.Get("Module_partySizeLimit_editableEvaluationPrompt", "value is a fixed headcount added to each affected lord party's native calculation and must be an integer from -100 to +100. +20 increases every party limit by 20; -20 reduces it by 20. Multiple policies add together; the final limit has a minimum of 1. Mild organizational changes usually use 5 to 10; regular expansion or downsizing 10 to 25; major standing-army reform 25 to 50; extreme royal-guard or total-mobilization reform 50 to 80. Reserve 80 to 100 for ruler-exclusive targets or exceptionally strong policy evidence. Nationwide effects change many lord parties and usually use 5 to 20, generally no more than 30. Assess negatives cautiously on the same scale so ordinary policies do not remove lords' basic fighting capability."),
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.Dynamic,
		allowIndependentClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public IReadOnlyCollection<string> RuntimeModuleIds => RuntimeModules;

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject obj
			|| !TryReadBoundedInteger(obj["value"], out _))
		{
			error = "partySizeLimit value must be an integer between -100 and +100";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		PartySizeLimitPayload payload,
		string scope,
		out PartySizeLimitPayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.Value < MinimumValue || payload.Value > MaximumValue)
		{
			error = "partySizeLimit value must be between -100 and +100";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryApplyTypedFunding(
		PartySizeLimitPayload payload,
		PolicyEffectFundingContext funding,
		out PartySizeLimitPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryNormalizeTypedPayload(payload, string.Empty, out PartySizeLimitPayload normalized, out error))
		{
			return false;
		}
		double scale = funding?.ResolveScale(ModuleDescriptor.FundingMode) ?? 1d;
		PartySizeLimitPayload candidate = new PartySizeLimitPayload
		{
			ModuleId = ModuleDescriptor.Id,
			SchemaVersion = ModuleDescriptor.PayloadSchemaVersion,
			Value = (int)Math.Round(normalized.Value * scale, MidpointRounding.AwayFromZero)
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
		if (context?.TargetSet == null || payload is not PartySizeLimitPayload typed)
		{
			error = "partySizeLimit requires valid compilation targets and payload";
			return false;
		}
		if (!TryNormalizeTypedPayload(typed, context.SourceScope, out PartySizeLimitPayload normalized, out error))
		{
			return false;
		}

		PolicyEffectCanonicalTargetSet targetSet = context.TargetSet;
		bool hasDirectRuler = HasSelectorHandle(targetSet.SelectorHandles, 'R');
		bool hasBroadSelector = HasSelectorHandle(targetSet.SelectorHandles, 'C')
			|| HasSelectorHandle(targetSet.SelectorHandles, 'K')
			|| HasSelectorHandle(targetSet.SelectorHandles, 'S')
			|| HasSelectorHandle(targetSet.SelectorHandles, 'L');
		bool hasRulerRole = false;
		foreach (string selectorId in targetSet.SelectorIds ?? new List<string>())
		{
			if (!PolicyHeroTargetSelectorResolver.TryDescribeSelector(
				selectorId,
				out string kind,
				out string value,
				out _)
				|| !string.Equals(kind, "role", StringComparison.Ordinal))
			{
				error = "partySizeLimit supports only the Hero role selectors ruler or lords";
				return false;
			}
			if (string.Equals(value, "ruler", StringComparison.Ordinal))
			{
				hasRulerRole = true;
			}
			else if (string.Equals(value, "lords", StringComparison.Ordinal))
			{
				hasBroadSelector = true;
			}
			else
			{
				error = "partySizeLimit supports only the Hero role selectors ruler or lords";
				return false;
			}
		}

		if (hasDirectRuler && !targetSet.FollowCurrentRulingClan)
		{
			error = "partySizeLimit ruler target must dynamically follow the current ruling clan";
			return false;
		}
		bool rulerOnly = targetSet.FollowCurrentRulingClan || hasDirectRuler || hasRulerRole;
		if (!rulerOnly && !hasBroadSelector
			&& ((targetSet.ClanIds?.Count ?? 0) > 0 || (targetSet.KingdomIds?.Count ?? 0) > 0))
		{
			hasBroadSelector = true;
		}
		if (rulerOnly == hasBroadSelector)
		{
			error = rulerOnly
				? "partySizeLimit may not mix ruler-exclusive targets with clan or kingdom lord targets"
				: "partySizeLimit requires identifiable ruler, clan or kingdom lord targets";
			return false;
		}
		if (normalized.Value == 0)
		{
			return true;
		}

		string runtimeModuleId = rulerOnly ? ClanLeaderRuntimeModuleId : ClanLordsRuntimeModuleId;
		children = new[]
		{
			new PolicyEffectCompositeChild
			{
				ModuleId = runtimeModuleId,
				Payload = new PartySizeLimitRuntimePayload
				{
					ModuleId = runtimeModuleId,
					SchemaVersion = 1,
					Value = normalized.Value
				}
			}
		};
		return true;
	}

	public override string DescribeTypedPayload(PartySizeLimitPayload payload)
	{
		return payload == null
			? string.Empty
			: (payload.Value > 0 ? "+" : string.Empty)
				+ payload.Value.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("RawPayloadPartyMembers");
	}

	internal static bool TryReadBoundedInteger(JToken token, out int value)
	{
		value = 0;
		if (token?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long parsed = token.Value<long>();
			if (parsed < MinimumValue || parsed > MaximumValue)
			{
				return false;
			}
			value = (int)parsed;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasSelectorHandle(IEnumerable<string> handles, char kind)
	{
		char expected = char.ToUpperInvariant(kind);
		return (handles ?? Array.Empty<string>()).Any(handle =>
		{
			string normalized = (handle ?? string.Empty).Trim();
			return normalized.Length > 0
				&& char.ToUpperInvariant(normalized[0]) == expected
				&& (normalized.Length == 1 || char.IsDigit(normalized[1]) || normalized[1] == ':');
		});
	}

	private static JObject CreatePayloadSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("value"),
			["properties"] = new JObject
			{
				["value"] = new JObject
				{
					["type"] = "integer",
					["minimum"] = MinimumValue,
					["maximum"] = MaximumValue
				}
			},
			["additionalProperties"] = false
		};
	}
}

internal abstract class PartySizeLimitRuntimeEffectModuleBase
	: PolicyEffectModuleBase<PartySizeLimitRuntimePayload>, IModelModifierPolicyEffectModule
{
	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject obj
			|| !PartySizeLimitEffectModule.TryReadBoundedInteger(obj["value"], out _))
		{
			error = Id + " value must be an integer between -100 and +100";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public override bool TryNormalizeTypedPayload(
		PartySizeLimitRuntimePayload payload,
		string scope,
		out PartySizeLimitRuntimePayload normalizedPayload,
		out string error)
	{
		_ = scope;
		normalizedPayload = payload;
		if (!TryValidateEnvelope(payload, out error))
		{
			return false;
		}
		if (payload.Value < PartySizeLimitEffectModule.MinimumValue
			|| payload.Value > PartySizeLimitEffectModule.MaximumValue)
		{
			error = Id + " value must be between -100 and +100";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
	{
		if (preparedInstance?.Instance?.Payload is not PartySizeLimitRuntimePayload payload
			|| preparedInstance.Instance.TargetSet?.ClanIds == null
			|| payload.Value == 0)
		{
			return Array.Empty<PolicyEffectModelContribution>();
		}
		return preparedInstance.Instance.TargetSet.ClanIds
			.Select(id => (id ?? string.Empty).Trim())
			.Where(id => id.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(id => id, StringComparer.Ordinal)
			.Select(id => new PolicyEffectModelContribution
			{
				InstanceId = preparedInstance.Instance.InstanceId,
				ModuleId = Id,
				Hook = Descriptor.Hook,
				TargetKind = PolicyEffectTargetKind.Clan,
				TargetId = id,
				Value = payload.Value,
				DisplayText = DescribeTypedPayload(payload)
			})
			.ToArray();
	}

	public override string DescribeTypedPayload(PartySizeLimitRuntimePayload payload)
	{
		return payload == null
			? string.Empty
			: (payload.Value > 0 ? "+" : string.Empty)
				+ payload.Value.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("RawPayloadPartyMembers");
	}
}
