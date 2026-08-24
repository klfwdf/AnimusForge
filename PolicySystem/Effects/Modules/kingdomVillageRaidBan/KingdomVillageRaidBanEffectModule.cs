using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.KingdomVillageRaidBanEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class KingdomVillageRaidBanPayload : PolicyEffectPayload
{
}

internal sealed class KingdomVillageRaidBanEffectModule
	: PolicyEffectModuleBase<KingdomVillageRaidBanPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "kingdomVillageRaidBan",
		order: 160,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Kingdom },
		cueTerms: new[] { "禁止劫掠村庄", "禁止烧村", "禁掠村庄", "领主不烧村", "不得劫掠村庄" },
		retrievalText: "政策禁止发布者本国的领主及领主队伍主动劫掠任何村庄；目标自动固定为政策发布者所属王国，不保护本国村庄免受外国劫掠，也不影响其他王国。",
		catalogSummary: "禁止发布者本国领主主动劫掠村庄",
		mainInstruction: "只有政策明确禁止、停止或约束发布者本国领主烧毁、袭击或劫掠村庄时，才选择此效果。该效果没有可选目标王国：一旦选择就只作用于政策发布者所属王国。它不保护本国村庄免受外国领主劫掠，也不能约束任何其他王国。不要输出模块 ID。",
		postprocessRule: "payload 必须严格为 {}。模块出现即表示发布者本国禁止领主队伍主动劫掠任何 Village；没有数值、等级或叠加强度。targetHandles 只能复制执行目录为此模块提供的唯一发布者本国句柄。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateMarkerSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.KingdomVillageRaidBlock,
		aggregation: PolicyEffectAggregationKind.AnyBlock,
		valueUnit: PolicyEffectValueUnit.BooleanFlag,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		playerDisplayName: "王国禁掠村庄",
		editableUnderstandingPrompt: "王国禁掠村庄表示政策发布者以国家规则禁止本国领主及其领主队伍主动烧毁、袭击或劫掠任何村庄。目标自动固定为发布者所属王国，不能选择外国王国；它不是保护发布者村庄的全局护盾，也不会阻止外国领主烧村。",
		editableEvaluationPrompt: "仅判断政策是否明确形成了禁止发布者本国领主劫掠村庄的持续规则。命中时输出空对象 {}，不输出强度或概率；多个生效政策仍只有同一个禁止效果。泛称爱民、改善治安、保护农业、惩罚敌国或保卫某些村庄，但没有禁止本国领主烧村时，不选择此效果。",
		targetRefresh: PolicyEffectTargetRefreshKind.FrozenCanonicalIds,
		targetBinding: PolicyEffectTargetBindingKind.IssuerKingdom);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public override bool TryNormalizeTypedPayload(
		KingdomVillageRaidBanPayload payload,
		string scope,
		out KingdomVillageRaidBanPayload normalizedPayload,
		out string error)
	{
		normalizedPayload = payload;
		if (!string.IsNullOrWhiteSpace(scope)
			&& !string.Equals(scope, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase))
		{
			error = "kingdomVillageRaidBan 仅支持王国政策作用域";
			return false;
		}
		return base.TryNormalizeTypedPayload(payload, scope, out normalizedPayload, out error);
	}

	public override PolicyEffectPrepareResult PrepareTyped(
		PolicyEffectCompileContext context,
		KingdomVillageRaidBanPayload payload)
	{
		if (!string.Equals(context?.SourceScope, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase)
			|| !TryGetSingleKingdomId(context.TargetSet, out _))
		{
			return PolicyEffectPrepareResult.Rejected(
				"kingdomVillageRaidBan 必须且只能绑定一个发布者王国目标");
		}
		return base.PrepareTyped(context, payload);
	}

	public override string DescribeTypedPayload(KingdomVillageRaidBanPayload payload)
	{
		return payload == null ? string.Empty : "发布者本国领主禁止劫掠村庄";
	}

	public IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
	{
		if (preparedInstance?.Instance?.Payload is not KingdomVillageRaidBanPayload
			|| !TryGetSingleKingdomId(preparedInstance.Instance.TargetSet, out string kingdomId))
		{
			return Array.Empty<PolicyEffectModelContribution>();
		}

		return new[]
		{
			new PolicyEffectModelContribution
			{
				InstanceId = preparedInstance.Instance.InstanceId,
				ModuleId = Id,
				Hook = Descriptor.Hook,
				TargetKind = PolicyEffectTargetKind.Kingdom,
				TargetId = kingdomId,
				Value = 1f,
				DisplayText = DescribeTypedPayload((KingdomVillageRaidBanPayload)preparedInstance.Instance.Payload)
			}
		};
	}

	protected override bool TryValidateRawPayload(JToken rawPayload, out string error)
	{
		if (rawPayload is not JObject payloadObject)
		{
			error = "kingdomVillageRaidBan payload 必须是空对象 {}";
			return false;
		}
		foreach (JProperty property in payloadObject.Properties())
		{
			if (!string.Equals(property.Name, "moduleId", StringComparison.Ordinal)
				&& !string.Equals(property.Name, "schemaVersion", StringComparison.Ordinal))
			{
				error = "kingdomVillageRaidBan payload 不允许字段: " + property.Name;
				return false;
			}
		}
		error = string.Empty;
		return true;
	}

	private static bool TryGetSingleKingdomId(
		PolicyEffectCanonicalTargetSet targetSet,
		out string kingdomId)
	{
		kingdomId = string.Empty;
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string candidate in targetSet?.KingdomIds ?? new List<string>())
		{
			string normalized = (candidate ?? string.Empty).Trim();
			if (normalized.Length > 0)
			{
				ids.Add(normalized);
			}
		}
		if (ids.Count != 1)
		{
			return false;
		}
		foreach (string id in ids)
		{
			kingdomId = id;
		}
		return true;
	}
}
