using System;

[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.ArmyFormationTendencyPctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ArmyFormationTendencyPctPayload : NumericPolicyEffectPayload
{
}

internal sealed class ArmyFormationTendencyPctEffectModule
	: NumericPolicyEffectModuleBase<ArmyFormationTendencyPctPayload>, IModelModifierPolicyEffectModule
{
	internal const float MinimumPercent = -100f;
	internal const float MaximumPercent = 200f;

	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "armyFormationTendencyPct",
		order: 150,
		legacyIds: Array.Empty<string>(),
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[]
		{
			PolicyEffectTargetKind.Settlement,
			PolicyEffectTargetKind.Clan,
			PolicyEffectTargetKind.Kingdom,
			PolicyEffectTargetKind.Hero
		},
		targetKinds: new[] { PolicyEffectTargetKind.Clan },
		cueTerms: new[] { "组建军团", "军团组建", "创建军团", "召集军团", "军团动员", "领主组军" },
		retrievalText: "政策提高或降低目标家族领主主动组建军团的倾向；只调整已经满足原版组军资格的 WillGatherArmy 候选分数，不改变影响力、和平、食物、队伍规模、雇佣兵、候选成员或已有军团等原版门槛。",
		catalogSummary: "目标家族合格领主的军团组建候选分数相对变化",
		mainInstruction: "只有政策明确鼓励、要求、抑制或限制目标家族领主主动组建军团时，才填写相对原版合格组军候选分数的变化。泛称增强军力、增加影响力、扩军、参军或提高军团战斗力但未改变领主组军决策倾向时，不属于此效果。它不能绕过原版资格条件，也不是绝对组军概率。不要输出模块 ID。",
		postprocessRule: "payload 必须严格为 {\"value\": number}。value 是目标家族领主已经满足原版资格后，其组建军团候选分数相对原版的百分比变化，范围 -100 到 +200，必须是有限数字。+50 表示候选分数乘 1.5，+100 表示乘 2，-100 表示归零；不是绝对概率，不会解散已有军团，也不绕过任何原版资格门槛。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Military,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.ArmyFormationScore,
		aggregation: PolicyEffectAggregationKind.Additive,
		valueUnit: PolicyEffectValueUnit.RelativePercent,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "军团组建倾向",
		editableUnderstandingPrompt: "军团组建倾向表示政策让目标家族领主在已经满足原版组军资格时，更愿意或更不愿意选择组建军团，而不是选择其他 AI 行为。它只缩放原版已经产生且标记为 WillGatherArmy 的候选分数，不给予影响力，不制造可召集成员，不绕过和平、食物、队伍规模、非雇佣兵、未加入现有军团等原版条件，也不解散已经存在的军团。泛称增强军力、提高影响力、扩充部队或改善军团战力，但没有改变领主主动组军决策倾向时，不属于这一效果。",
		editableEvaluationPrompt: "按相对原版合格组军候选分数的百分比变化判断，不按绝对概率判断。轻微鼓励或限制通常为 ±10% 到 ±25%；明确鼓励、职责要求或一般制度性推动建议约 +50%；强力军团动员约 +75% 到 +100%；接近强制但仍保留原版资格与 AI 竞争时可到 +150%，+200% 已是总强度上限。抑制性政策按同一尺度给负值，-100% 会让目标家族的新组军候选归零。多个生效政策先相加，运行时总倍率限制在 0 到 3 倍。+100% 很强，不应当作普通默认值。"
	);

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	protected override bool TryNormalizeNumericValue(
		float rawValue,
		string scope,
		out float normalizedValue,
		out string error)
	{
		_ = scope;
		normalizedValue = rawValue;
		if (rawValue < MinimumPercent || rawValue > MaximumPercent)
		{
			error = "armyFormationTendencyPct 的 value 必须在 -100 到 +200 之间";
			return false;
		}
		error = string.Empty;
		return true;
	}

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(
		PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
