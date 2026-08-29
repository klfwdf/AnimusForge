using System;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.PartySizeLimitClanLeaderEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class PartySizeLimitClanLeaderEffectModule : PartySizeLimitRuntimeEffectModuleBase
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: PartySizeLimitEffectModule.ClanLeaderRuntimeModuleId,
		order: 171,
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
		cueTerms: Array.Empty<string>(),
		retrievalText: "内部运行模块：调整目标家族族长亲自率领的正式领主部队上限。",
		catalogSummary: "内部：调整家族族长部队上限",
		mainInstruction: "内部模块，不向模型暴露。",
		postprocessRule: "内部模块，不允许直接选择。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateIntegerValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.PartyMemberSizeLimit,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.PartySizePoints,
		fundingMode: PolicyEffectFundingMode.Unscaled,
		fundingStrategy: PolicyEffectFundingStrategy.None,
		payloadSchemaVersion: 1,
		promptVisible: false,
		displayGroup: "partySizeLimit",
		playerDisplayName: "领主部队上限",
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.Dynamic,
		allowIndependentClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;
}
