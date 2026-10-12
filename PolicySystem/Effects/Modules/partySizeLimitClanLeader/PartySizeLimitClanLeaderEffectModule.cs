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
		retrievalText: PolicyGenerationPromptTexts.Get("Module_partySizeLimitClanLeader_retrievalText", "Internal runtime module: adjust the size limit of the official lord party personally led by the target clan's leader."),
		catalogSummary: PolicyGenerationPromptTexts.Get("Module_partySizeLimitClanLeader_catalogSummary", "Internal: adjust clan leader party size limit"),
		mainInstruction: PolicyGenerationPromptTexts.Get("Module_partySizeLimitClanLeader_mainInstruction", "Internal module; not exposed to the model."),
		postprocessRule: PolicyGenerationPromptTexts.Get("Module_partySizeLimitClanLeader_postprocessRule", "Internal module; it may not be selected directly."),
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
		playerDisplayName: PolicyCompatibilityTexts.Get("Module_partySizeLimitClanLeader_Original_18"),
		targetProjection: PolicyEffectTargetProjectionKind.None,
		targetRefresh: PolicyEffectTargetRefreshKind.Dynamic,
		allowIndependentClanTargets: true,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;
}
