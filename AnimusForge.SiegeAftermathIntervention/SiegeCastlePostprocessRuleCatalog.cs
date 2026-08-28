using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dynamic castle-only postprocess rules. No town GCCZ tag or state is referenced here.
/// </summary>
public static class SiegeCastlePostprocessRuleCatalog
{
    private const string AiSemanticContract = "【AF原生AI语义候选】本标签只是当前角色与状态下可用的能力；必须由AI结合完整玩家输入、NPC本轮直接回复和上下文决定是否输出，不得按固定关键词命中。";

    private const string RevisableStageContract = "标签触发后只为本轮选中的数量与兵种登记暂定去向，尚存战俘不会立即从场景或俘虏名册消失；玩家离场时才逐组执行名册、金币与地方副作用。玩家未说明数量时由运行时随机选取，未说明兵种时随机选兵；说‘其余/剩下/全部’时使用当前未分配者。玩家同一句以明确数量和‘其余/剩下’划分多个去向时，可按分组顺序输出多个不同的普通战俘处置标签。明确说反悔、全部重来或推翻之前安排时才清空幸存者旧计划；单独说‘改判为某处置’只登记当前新分组，不得清空其他分组，NPC必须记住前后变化。";

    private static readonly SiegePostprocessRuleDefinition SoldierDiscontentRule = Rule(
        SiegeCastleActionTagCatalog.SoldierDiscontentTag,
        "【即时见证反应专用】仅当当前己方士兵正在自由评论刚发生的城堡战俘处置，而且其实际发言明确表达反感、忧虑、同情、军纪疑虑、文化冲突或对统帅做法的不满时输出。赞同、欢呼、中立复述、恐惧但服从、单纯询问或普通建议不得输出。该标签只创建一次待安抚军心事件，不得改变战俘处置，也不得再次触发见证反应。");

    private static readonly SiegePostprocessRuleDefinition ProposeRecruitRule = Rule(
        SiegeCastleActionTagCatalog.ProposeRecruitPrisonersTag,
        "【提议，不结算】仅在己方士兵建议收编，或普通战俘请求归顺，而玩家尚未明确同意时输出。只登记待确认；不得改变名册。士兵提出后必须由玩家后续明确同意。");

    private static readonly SiegePostprocessRuleDefinition ProposeSlaughterRule = Rule(
        SiegeCastleActionTagCatalog.ProposeSlaughterPrisonersTag,
        "【提议，不结算】仅在己方士兵建议屠戮，而玩家尚未明确同意时输出。只登记待确认；不得伤害任何人。必须由玩家后续明确同意。");

    private static readonly SiegePostprocessRuleDefinition ProposeReleaseRule = Rule(
        SiegeCastleActionTagCatalog.ProposeReleasePrisonersTag,
        "【提议，不结算】仅在己方士兵建议释放，或普通战俘请求释放，而玩家尚未明确同意时输出。只登记待确认；不得改变名册。必须由玩家后续明确同意。");

    private static readonly SiegePostprocessRuleDefinition ProposeSellRule = Rule(
        SiegeCastleActionTagCatalog.ProposeSellPrisonersTag,
        "【提议，不结算】仅在己方士兵建议贩卖普通战俘，而玩家尚未明确同意时输出。只登记待确认；不得改变名册或金币。必须由玩家后续明确同意。");

    private static readonly SiegePostprocessRuleDefinition ProposeLaborRule = Rule(
        SiegeCastleActionTagCatalog.ProposeLaborPrisonersTag,
        "【提议，不结算】仅在己方士兵建议把战俘派往村庄充作农奴、修路或耕种，或普通战俘请求接受这类外派劳役赎罪，而玩家尚未明确同意时输出。只登记待确认；不得提前施加地方效果，也不得与修缮城堡标签混用。");

    private static readonly SiegePostprocessRuleDefinition ProposeRepairCastleLaborRule = Rule(
        SiegeCastleActionTagCatalog.ProposeRepairCastleLaborTag,
        "【提议，不结算】仅在己方士兵建议让战俘留在本城堡修缮城墙、城防或城堡项目，或普通战俘请求以修缮本城堡赎罪，而玩家尚未明确同意时输出。只登记待确认；不得提前施加建设效果。");

    private static readonly SiegePostprocessRuleDefinition ProposeInstructorRule = Rule(
        SiegeCastleActionTagCatalog.ProposeInstructorPrisonersTag,
        "【提议，不结算】仅在己方士兵建议让战俘充当教官，或普通战俘主动提出训练新兵，而玩家尚未明确同意时输出。只登记待确认；不得提前施加训练效果。");

    private static readonly SiegePostprocessRuleDefinition TreatRule = Rule(
        SiegeCastleActionTagCatalog.TreatPrisonersTag,
        "【流程标签】善待俘虏。普通战俘回应时对本次带入的普通战俘群体生效；被俘领主回应时只对该领主生效。必须确有玩家给予食物、药品或物资并约束随军士兵不得虐待。按人数与兵种等级扣除物资，提高俘虏信任；同一目标本场只结算一次，不是最终处置。");

    private static readonly SiegePostprocessRuleDefinition ArmamentsRule = Rule(
        SiegeCastleActionTagCatalog.ReceiveArmamentsTag,
        "【流程标签】接收军械。普通战俘回应时收缴本次带入普通战俘群体的对应战利品并直接送入背包；领主回应时只收缴该领主当前武器和盔甲。降低信任，同一目标本场只结算一次，不弹战利品界面。屠戮普通战俘会自动包含一次群体接收军械，不得重复结算。");

    private static readonly SiegePostprocessRuleDefinition ReleaseRule = Rule(
        SiegeCastleActionTagCatalog.ReleasePrisonersTag,
        "【普通战俘群体暂定标签】暂定释放本次带入且尚存的普通战俘；领主不包含。最终执行后提高地方与俘虏信任，但围城退出仍且只走一次原版宽恕，繁荣仍受原版宽恕损失。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition SellRule = Rule(
        SiegeCastleActionTagCatalog.SellPrisonersTag,
        "【普通战俘群体暂定标签】暂定严格复用原版酒馆赎卖动作处理本次带入且尚存的普通战俘；离场最终执行时才按实时价格结算金币、技能经验和原版事件并移出名册；领主不包含。造成地方、村庄、要人和俘虏信任负面效果。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition VoluntaryRecruitRule = Rule(
        SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag,
        "【普通战俘群体暂定标签·自愿】仅当战俘明确心甘情愿归顺且当前信任达到门槛时输出。离场最终执行时按主队空余编制转入成员名册，实际人数不减半；自愿年度增益按城镇同类效果最多一半，并可能引发随军士兵不满等待安抚。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition ForcedRecruitRule = Rule(
        SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag,
        "【普通战俘群体暂定标签·强制】玩家明确命令收编，但战俘未自愿或信任不足时输出。离场最终执行时按主队空余编制转入成员名册，实际人数不减半；长期正面增益仅为自愿的50%，负面后果按约1.5倍，并可能引发随军士兵不满等待安抚。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition VoluntaryLaborRule = Rule(
        SiegeCastleActionTagCatalog.LaborPrisonersVoluntaryTag,
        "【普通战俘群体暂定标签·自愿】战俘明确同意被派往村庄充作农奴、修路或耕种且信任达到门槛时输出。离场最终执行时对尚存战俘直接施加持续游戏一年的地方效果，不创建服役单位或期限，也不转入玩家部队；修缮本城堡必须改用专用修缮城堡标签。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition ForcedLaborRule = Rule(
        SiegeCastleActionTagCatalog.LaborPrisonersForcedTag,
        "【普通战俘群体暂定标签·强制】玩家强迫本次带入且尚存的普通战俘派往村庄充作农奴、修路或耕种时输出。离场最终执行时直接施加持续游戏一年的地方效果，不创建服役单位或期限；正面提升仅为自愿的50%，负面后果约为自愿的1.5倍，也不转入玩家部队。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition VoluntaryRepairCastleLaborRule = Rule(
        SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag,
        "【普通战俘群体暂定标签·自愿修缮城堡】仅当战俘明确自愿留下修缮本城堡、城墙或城防且信任达到门槛时输出。离场最终执行时按200人为满额基准施加持续游戏一年的城堡项目建设速度加成：200人及以上+50%，不足200人按人数线性缩放；同时结算与村庄劳役相近的地方影响。不创建服役单位或期限。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition ForcedRepairCastleLaborRule = Rule(
        SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag,
        "【普通战俘群体暂定标签·强制修缮城堡】玩家强迫本次带入且尚存的普通战俘修缮本城堡、城墙或城防时输出。离场最终执行时按200人为满额基准施加持续游戏一年的城堡项目建设速度加成：200人及以上+25%，不足200人按人数线性缩放（例如50人+6.25%）；地方负面后果按强制劳役结算。不创建服役单位或期限。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition VoluntaryInstructorRule = Rule(
        SiegeCastleActionTagCatalog.InstructorPrisonersVoluntaryTag,
        "【普通战俘群体暂定标签·自愿】有训练能力的战俘明确自愿接受教官处置且信任达到门槛时输出。离场最终执行时对尚存战俘直接提高附近志愿兵补充速度与新兵精锐度一年，不创建教官单位或期限，上限为城镇同类效果一半。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition ForcedInstructorRule = Rule(
        SiegeCastleActionTagCatalog.InstructorPrisonersForcedTag,
        "【普通战俘群体暂定标签·强制】玩家强迫本次带入且尚存的普通战俘接受教官处置时输出。离场最终执行时直接施加一年效果，不创建教官单位或期限；补充速度与精锐度提升仅为自愿的50%，负面后果约为自愿的1.5倍。" + RevisableStageContract);

    private static readonly SiegePostprocessRuleDefinition SlaughterRule = Rule(
        SiegeCastleActionTagCatalog.SlaughterPrisonersTag,
        "【普通战俘群体现行命令·高风险】只有玩家明确命令或明确同意本说话者此前提议时输出。命令会覆盖先前暂定处置，把尚存普通战俘转为敌对目标，由编队1的己方士兵在场景内实际攻击并杀死；死亡后才从名册扣除，绝不直接刷没。若玩家明确反悔或推翻屠戮命令，停止攻击幸存者；已实际死亡者不可复活，新处置仅作用于幸存者。自动包含一次接收军械。退出仍只调用一次原版宽恕，再补齐城堡繁荣与忠诚至原版毁坏强度；领主不包含。");

    private static readonly SiegePostprocessRuleDefinition AppeaseRule = Rule(
        SiegeCastleActionTagCatalog.AppeaseSoldiersTag,
        "【己方士兵唯一正式结算标签】安抚随军士兵。仅当当前处置已引发军心不满且玩家本轮确实解释、补偿或安排军纪/战利品时输出。成功则免除离场士气惩罚；士兵可以不满但不能抗命。不得改变战俘名册。");

    private static readonly SiegePostprocessRuleDefinition RecruitLordRule = Rule(
        SiegeCastleActionTagCatalog.RecruitLordTag,
        "【被俘领主单体最终标签】只针对当前直接回应的被俘领主。族长按玩家政治身份走投效国家、请求引见统治者或拥立玩家分支；非族长必须由对话明确选择写信引见族长，或背叛家族成为同伴。触发条件严格，不得由普通战俘、己方士兵或旁听者输出。");

    private static readonly SiegePostprocessRuleDefinition SellLordRule = Rule(
        SiegeCastleActionTagCatalog.SellLordTag,
        "【被俘领主单体最终标签】只针对当前直接回应的被俘领主，且玩家必须明确命令将该领主交给赎金经纪人。严格复用原版酒馆赎卖的计价、金币、技能与事件链，按该领主在酒馆出售时的实时价格结算并解除俘虏、清理场景实体。与领主收编、处决互斥，不得由普通战俘、己方士兵或旁听者输出。");

    private static readonly SiegePostprocessRuleDefinition ExecuteLordRule = Rule(
        SiegeCastleActionTagCatalog.ExecuteLordTag,
        "【被俘领主单体高风险结算】只针对当前直接回应的被俘领主。玩家以任何自然说法明确命令杀死、斩杀或处决当前领主，且领主本轮回复已正面承接该命令时输出；‘我要斩了你’、‘我要杀了你’只是语义示例而不是词表。询问、假设、单纯威胁、谈论他人、反悔或拒绝处决不得输出。普通战俘屠戮不能代替。标签只打开原版处刑确认；玩家确认并退出动画后才结算，取消不得处死。该流程与普通战俘群体结算完全隔离。");

    private static readonly SiegePostprocessRuleDefinition DuelLordRule = Rule(
        SiegeCastleActionTagCatalog.DuelLordTag,
        "【被俘领主单体流程标签·由AI按回复语义确认】玩家本轮可以直接提出决斗，也可以紧接上一轮决斗谈判明确表示会守信、履行下马/收弓/公平交战等条件。只要当前被俘领主在本次直接回复中已经无条件同意现在应战，就必须输出本标签；接受不要求固定关键词，‘我不会放过这种机会’、‘我定当全力以赴’、‘来吧，为自由而战’等结合当前决斗语境明确表示立即应战的自然说法都算同意。玩家单方面宣告、领主拒绝、尚在考虑，或仍要求‘先下马/先放下弓/改用公平武器后才答应’时不得输出；条件满足后领主本轮明确应战即可输出，玩家无需重复说“决斗”二字。标签只启动当前场景内的不致死决斗，不自动释放或最终处置领主。同一领主本场只触发一次。");

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(SiegeCastlePostprocessRuleFacts facts)
    {
        facts ??= SiegeCastlePostprocessRuleFacts.Empty;
        var rules = new List<SiegePostprocessRuleDefinition>();
        if (facts.IsWitnessReaction)
        {
            if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
                && SiegeCastleSoldierReactionProfile.CanReactTo(facts.ReactionToAction))
            {
                rules.Add(SoldierDiscontentRule);
            }
            return rules;
        }
        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return rules;
        }

        switch (facts.SpeakerRole)
        {
            case SiegeCastleActionSpeakerRole.AlliedSoldier:
                AddAlliedSoldierRules(rules, facts);
                break;
            case SiegeCastleActionSpeakerRole.RegularPrisoner:
                AddRegularPrisonerRules(rules, facts);
                break;
            case SiegeCastleActionSpeakerRole.CapturedLord:
                AddCapturedLordRules(rules, facts);
                break;
        }
        return rules;
    }

    private static void AddAlliedSoldierRules(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts)
    {
        if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied)
        {
            rules.Add(AppeaseRule);
        }

        if (facts.RemainingRegularPrisoners <= 0)
        {
            return;
        }

        AddIfNotApplied(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule);
        AddIfNotApplied(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule);
        rules.Add(ReleaseRule);
        rules.Add(SellRule);
        rules.Add(ForcedRecruitRule);
        rules.Add(ForcedLaborRule);
        rules.Add(ForcedRepairCastleLaborRule);
        rules.Add(ForcedInstructorRule);
        rules.Add(SlaughterRule);
        AddAlliedProposalRules(rules);
    }

    private static void AddRegularPrisonerRules(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts)
    {
        if (facts.RemainingRegularPrisoners <= 0)
        {
            return;
        }

        AddIfNotApplied(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule);
        AddIfNotApplied(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule);
        rules.Add(ReleaseRule);
        rules.Add(SellRule);
        rules.Add(SlaughterRule);
        AddConsentPair(
            rules,
            facts,
            SiegeCastleActionKind.RecruitPrisonersVoluntary,
            VoluntaryRecruitRule,
            ForcedRecruitRule);
        AddConsentPair(
            rules,
            facts,
            SiegeCastleActionKind.LaborPrisonersVoluntary,
            VoluntaryLaborRule,
            ForcedLaborRule);
        AddConsentPair(
            rules,
            facts,
            SiegeCastleActionKind.RepairCastleLaborVoluntary,
            VoluntaryRepairCastleLaborRule,
            ForcedRepairCastleLaborRule);
        AddConsentPair(
            rules,
            facts,
            SiegeCastleActionKind.InstructorPrisonersVoluntary,
            VoluntaryInstructorRule,
            ForcedInstructorRule);
        AddRegularPrisonerProposalRules(rules);
    }

    private static void AddCapturedLordRules(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts)
    {
        if (facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
        {
            return;
        }

        AddIfNotApplied(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule);
        AddIfNotApplied(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule);
        rules.Add(RecruitLordRule);
        rules.Add(SellLordRule);
        rules.Add(ExecuteLordRule);
        AddIfNotApplied(rules, facts, SiegeCastleActionKind.DuelLord, DuelLordRule);
    }

    private static void AddIfNotApplied(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts,
        SiegeCastleActionKind action,
        SiegePostprocessRuleDefinition rule)
    {
        if (!facts.IsActionAlreadyApplied(action))
        {
            rules.Add(rule);
        }
    }

    private static void AddConsentPair(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts,
        SiegeCastleActionKind voluntaryAction,
        SiegePostprocessRuleDefinition voluntaryRule,
        SiegePostprocessRuleDefinition forcedRule)
    {
        if (SiegeCastlePrisonerTrustProfile.MeetsVoluntaryThreshold(voluntaryAction, facts.SpeakerTrust))
        {
            rules.Add(voluntaryRule);
        }
        rules.Add(forcedRule);
    }

    private static void AddAlliedProposalRules(ICollection<SiegePostprocessRuleDefinition> rules)
    {
        rules.Add(ProposeRecruitRule);
        rules.Add(ProposeSlaughterRule);
        rules.Add(ProposeReleaseRule);
        rules.Add(ProposeSellRule);
        rules.Add(ProposeLaborRule);
        rules.Add(ProposeRepairCastleLaborRule);
        rules.Add(ProposeInstructorRule);
    }

    private static void AddRegularPrisonerProposalRules(ICollection<SiegePostprocessRuleDefinition> rules)
    {
        rules.Add(ProposeReleaseRule);
        rules.Add(ProposeRecruitRule);
        rules.Add(ProposeLaborRule);
        rules.Add(ProposeRepairCastleLaborRule);
        rules.Add(ProposeInstructorRule);
    }

    private static SiegePostprocessRuleDefinition Rule(string tag, string description)
        => new SiegePostprocessRuleDefinition(tag, AiSemanticContract + description);
}
