using System;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.VolunteerProductionGrowthPctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class VolunteerProductionGrowthPctPayload : NumericPolicyEffectPayload
{
}

internal sealed class VolunteerProductionGrowthPctEffectModule
	: NumericPolicyEffectModuleBase<VolunteerProductionGrowthPctPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "volunteerProductionGrowthPct",
		order: 145,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom
		},
		targetKinds: new[] { PolicyEffectTargetKind.Settlement },
		cueTerms: new[] { "志愿兵补充", "志愿兵成长", "可招募志愿兵", "兵源补充", "募兵名额", "地方兵源", "新兵成长" },
		retrievalText: "政策直接提高或降低目标领地尚未招募的志愿兵补充与成长速度；同一个相对百分比同时缩放空槽补充判定和已有志愿兵进入原版升级尝试的频率。它是抽象政策结果，不要求招募站、训练营等特定设施，也不直接招募、授予兵种经验或保证升级。",
		catalogSummary: "目标领地志愿兵补充判定与原版成长尝试频率的相对变化",
		mainInstruction: "只有政策会直接改变目标领地尚未招募的志愿兵兵源补充、可招募名额恢复或志愿兵成长速度时，才填写相对原版每日判定频率的变化。空槽和已有志愿兵必须使用同一个数值：空槽影响补充判定，非空槽只影响进入原版升级尝试的频率，仍保留原版后续升级判定。它不表示招募池刷新，不要求招募站或训练营，不直接增加部队人数，也不给领主队伍或驻军士兵经验。泛称扩军、练兵或提高现役部队精锐程度但未改变志愿兵时不要选择。不要输出模块 ID。",
		postprocessRule: "payload 必须严格为 {\"value\": number}，value 必须是有限数字，表示相对原版每日判定频率的百分比变化。+20 表示原版概率乘 120%，+100 表示乘 200%，-100 及更低的叠加结果会使概率归零；不是增加绝对概率百分点。多个生效政策先加总百分比，再按原版公式缩放并将最终概率限制在 0 到 1。空槽补充和已有志愿兵进入原版升级尝试使用同一个 value。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.VolunteerProductionProbability,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.RelativePercent,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "志愿兵补充与成长",
		editableUnderstandingPrompt: "志愿兵补充与成长表示政策直接改变目标领地尚未招募的要人志愿兵槽位。一个数值同时作用于两种状态：空槽改变补充志愿兵的原版判定，已有志愿兵改变其进入原版升级尝试的频率；后者仍必须通过原版后续升级判定，因此不会保证升级。它是抽象政策结果，不要求政策写出招募站、训练营或其他固定设施，也不要把它描述成招募池刷新。它不直接招募士兵，不给英雄技能，不给领主队伍或驻军普通士兵兵种经验，也不影响民兵。",
		editableEvaluationPrompt: "数值是相对原版每日判定频率的百分比变化，不是绝对概率百分点。+20 表示原版概率乘 1.2，+50 表示乘 1.5，+100 表示乘 2，-100 表示归零。轻微改善或阻碍通常为 ±10% 到 ±25%，明确制度性改变约 ±25% 到 ±60%，强力改革约 ±60% 到 ±100%；超过 +100% 应要求很强的政策依据，但运行逻辑沿用旧政策系统，不设置额外正向硬上限。多个政策先加总，倍率下限为 0，最终概率限制在 0 到 1。只有政策分别还让已经进入领主队伍或驻军的普通士兵获得经验时，才同时使用士兵精锐化模块。",
		targetProjection: PolicyEffectTargetProjectionKind.PrimaryFiefAndBoundSettlements,
		targetRefresh: PolicyEffectTargetRefreshKind.Dynamic,
		allowCrossKingdomTargets: true);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
