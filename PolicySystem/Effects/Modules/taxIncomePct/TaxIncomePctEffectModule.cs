[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(typeof(global::AnimusForge.PolicyEffects.Modules.TaxIncomePctEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class TaxIncomePctPayload : NumericPolicyEffectPayload
{
}

internal sealed class TaxIncomePctEffectModule : NumericPolicyEffectModuleBase<TaxIncomePctPayload>, IModelModifierPolicyEffectModule
{
	private static readonly PolicyEffectModuleDescriptor ModuleDescriptor = new PolicyEffectModuleDescriptor(
		id: "taxIncomePct",
		order: 70,
		legacyIds: new[] { "townTaxPercent" },
		allowedScopes: new[] { PolicyEffectScopes.Kingdom, PolicyEffectScopes.Local, PolicyEffectScopes.Vassal },
		allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement, PolicyEffectTargetKind.Clan, PolicyEffectTargetKind.Kingdom },
		targetKinds: new[] { PolicyEffectTargetKind.Town },
		cueTerms: new[] { "税收", "税率", "赋税", "减税", "免税", "征税", "贡税" },
		retrievalText: "领地、城镇或城堡的税收、税率、赋税、减税、免税、财政税收、领地税收收入、征税与税款转移；目标城镇或城堡所属氏族主税收收入百分比点变化。泛称人物或领主收入而未提领地或税制时不属于此效果。",
		catalogSummary: "城镇或城堡所属氏族税收百分比变化",
		mainInstruction: "只有政策明确改变领地、城镇、城堡或税制，并直接影响目标城镇或城堡所属氏族主的税收收入时，才在 numericIntent 中说明付出方、受益方、方向、强弱与理由。泛称增加领主、统治者或其他职业人物收入但未提领地、城镇、城堡或税制时，不属于此效果。不要输出模块 ID 或数值。",
		postprocessRule: "payload 只含一个有限数值。该值是目标城镇或城堡所属氏族最终收到的原版主税收百分比点变化，正数增收、负数减收；不是当地百姓税率，也不含村庄独立收入或关税。",
		payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
		family: PolicyEffectFamily.Fiscal,
		executionKind: PolicyEffectExecutionKind.ModelModifier,
		hook: PolicyEffectHook.TownTaxIncome,
		aggregation: PolicyEffectAggregationKind.PercentPoints,
		valueUnit: PolicyEffectValueUnit.PercentPoints,
		fundingMode: PolicyEffectFundingMode.InheritPolicy,
		fundingStrategy: PolicyEffectFundingStrategy.Linear,
		payloadSchemaVersion: 1,
		playerDisplayName: "城镇与城堡税收",
		editableUnderstandingPrompt: "城镇与城堡主税收表示目标领地所属氏族最终收到的原版主税收收入变化，不是当地百姓被抽取的税额。只有正文明确涉及领地、城镇、城堡或税制时才使用：提高所有者留存税率、征收效率或让其获得其他地区上缴会增加收入；减税、免税，或把税款截留、转交、上缴给另一方会减少收入。泛称提高领主、统治者或其他职业人物收入但未提领地或税制时，不属于城镇与城堡主税收。税款从一方转给另一方时，应分别体现付出方与受益方；村庄独立收入、关税和单纯繁荣变化不属于这一效果。",
		editableEvaluationPrompt: "按相对原版最终主税收收入的百分比点判断，增收为正、减收为负。普通税制调整为 ±5% 到 ±15%；明显调整为 ±15% 到 ±35%；全国重大税制为 ±20% 到 ±60%。只调整特定人群、行业、地区、时段、税种或部分税额时，按实际覆盖范围给出相称比例；只有明确取消作用范围内全部城镇与城堡主税收时，才接近 -100%。");

	public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

	public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution> BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
		=> PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
