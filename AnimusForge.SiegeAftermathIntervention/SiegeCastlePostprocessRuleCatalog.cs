using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dynamic castle-only postprocess rules. No town GCCZ tag or state is referenced here.
/// </summary>
public static class SiegeCastlePostprocessRuleCatalog
{
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
        "【提议，不结算】仅在己方士兵建议劳役安置，或普通战俘请求以劳役赎罪，而玩家尚未明确同意时输出。只登记待确认；不得提前施加地方效果。");

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
        "【普通战俘群体最终标签】释放本次带入且尚在主队俘虏名册的普通战俘；领主不包含。提高地方与俘虏信任，但围城退出仍且只走一次原版宽恕，繁荣仍受原版宽恕损失。与其他普通战俘最终标签互斥。");

    private static readonly SiegePostprocessRuleDefinition SellRule = Rule(
        SiegeCastleActionTagCatalog.SellPrisonersTag,
        "【普通战俘群体最终标签】按原版俘虏赎金价值贩卖本次带入的普通战俘，金币直接入账并移出俘虏名册；领主不包含。造成地方、村庄、要人和俘虏信任负面效果。与其他普通战俘最终标签互斥。");

    private static readonly SiegePostprocessRuleDefinition VoluntaryRecruitRule = Rule(
        SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag,
        "【普通战俘群体最终标签·自愿】仅当战俘明确心甘情愿归顺且当前信任达到门槛时输出。按主队空余编制转入成员名册，实际人数不减半；自愿年度增益按城镇同类效果最多一半。与其他最终标签互斥，并可能引发随军士兵不满等待安抚。");

    private static readonly SiegePostprocessRuleDefinition ForcedRecruitRule = Rule(
        SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag,
        "【普通战俘群体最终标签·强制】玩家明确命令收编，但战俘未自愿或信任不足时输出。按主队空余编制转入成员名册，实际人数不减半；长期正面增益仅为自愿的50%，负面后果按约1.5倍。与其他最终标签互斥，并可能引发随军士兵不满等待安抚。");

    private static readonly SiegePostprocessRuleDefinition VoluntaryLaborRule = Rule(
        SiegeCastleActionTagCatalog.LaborPrisonersVoluntaryTag,
        "【普通战俘群体最终标签·自愿】战俘明确同意接受农奴、修路或修缮等劳役处置且信任达到门槛时输出。标签处理本次带入的全部普通战俘；离开场景后直接施加持续游戏一年的地方效果，不创建服役单位或期限，也不转入玩家部队。与其他最终标签互斥。");

    private static readonly SiegePostprocessRuleDefinition ForcedLaborRule = Rule(
        SiegeCastleActionTagCatalog.LaborPrisonersForcedTag,
        "【普通战俘群体最终标签·强制】玩家强迫本次带入的全部普通战俘接受劳役处置时输出。离开场景后直接施加持续游戏一年的地方效果，不创建服役单位或期限；正面提升仅为自愿的50%，负面后果约为自愿的1.5倍，也不转入玩家部队。与其他最终标签互斥。");

    private static readonly SiegePostprocessRuleDefinition VoluntaryInstructorRule = Rule(
        SiegeCastleActionTagCatalog.InstructorPrisonersVoluntaryTag,
        "【普通战俘群体最终标签·自愿】有训练能力的战俘明确自愿接受教官处置且信任达到门槛时输出。标签处理本次带入的全部普通战俘；离开场景后直接提高附近志愿兵补充速度与新兵精锐度一年，不创建教官单位或期限，上限为城镇同类效果一半。与其他最终标签互斥。");

    private static readonly SiegePostprocessRuleDefinition ForcedInstructorRule = Rule(
        SiegeCastleActionTagCatalog.InstructorPrisonersForcedTag,
        "【普通战俘群体最终标签·强制】玩家强迫本次带入的全部普通战俘接受教官处置时输出。离开场景后直接施加一年效果，不创建教官单位或期限；补充速度与精锐度提升仅为自愿的50%，负面后果约为自愿的1.5倍。与其他最终标签互斥。");

    private static readonly SiegePostprocessRuleDefinition SlaughterRule = Rule(
        SiegeCastleActionTagCatalog.SlaughterPrisonersTag,
        "【普通战俘群体最终标签·高风险】只有玩家明确命令或明确同意本说话者此前提议时输出。命令会把普通战俘转为敌对目标，由编队1的己方士兵在场景内实际攻击并杀死；死亡后才从名册扣除，绝不直接刷没。自动包含一次接收军械。退出仍只调用一次原版宽恕，再补齐城堡繁荣与忠诚至原版毁坏强度；领主不包含。");

    private static readonly SiegePostprocessRuleDefinition AppeaseRule = Rule(
        SiegeCastleActionTagCatalog.AppeaseSoldiersTag,
        "【己方士兵唯一正式结算标签】安抚随军士兵。仅当当前处置已引发军心不满且玩家本轮确实解释、补偿或安排军纪/战利品时输出。成功则免除离场士气惩罚；士兵可以不满但不能抗命。不得改变战俘名册。");

    private static readonly SiegePostprocessRuleDefinition RecruitLordRule = Rule(
        SiegeCastleActionTagCatalog.RecruitLordTag,
        "【被俘领主单体最终标签】只针对当前直接回应的被俘领主。族长按玩家政治身份走投效国家、请求引见统治者或拥立玩家分支；非族长必须由对话明确选择写信引见族长，或背叛家族成为同伴。触发条件严格，不得由普通战俘、己方士兵或旁听者输出。");

    private static readonly SiegePostprocessRuleDefinition ExecuteLordRule = Rule(
        SiegeCastleActionTagCatalog.ExecuteLordTag,
        "【被俘领主单体高风险结算】只针对当前直接回应的被俘领主，且玩家必须明确下达处决该领主的命令。普通战俘屠戮不能代替。标签只打开原版处刑确认；玩家确认并退出动画后才结算，取消不得处死。该流程与普通战俘群体结算完全隔离。");

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(SiegeCastlePostprocessRuleFacts facts)
    {
        facts ??= SiegeCastlePostprocessRuleFacts.Empty;
        var rules = new List<SiegePostprocessRuleDefinition>();
        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return rules;
        }

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier)
        {
            SiegeCastleSoldierAppeasementAuthorizationDecision appeasement =
                SiegeCastleSoldierAppeasementAuthorizationPolicy.Evaluate(facts.PlayerText);
            if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied && appeasement.IsAuthorized)
            {
                rules.Add(AppeaseRule);
                return rules;
            }

            if (facts.RemainingRegularPrisoners <= 0
                || facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
            {
                return rules;
            }

            SiegeCastlePlayerAuthorizationDecision disposition = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
                facts.PlayerText,
                facts.PendingProposalForSpeaker);
            if (disposition.IsAuthorized
                && TryAddAlliedAuthorizedDispositionRule(rules, facts, disposition.Disposition))
            {
                return rules;
            }

            if (TryAddDirectRule(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule)
                || TryAddDirectRule(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule))
            {
                return rules;
            }

            if (disposition.ReasonCode == "player_rejected_or_cancelled")
            {
                return rules;
            }

            SiegeCastlePrisonerDispositionKind intent = SiegeCastlePlayerAuthorizationPolicy.DetectIntent(facts.PlayerText);
            if (intent != SiegeCastlePrisonerDispositionKind.None)
            {
                AddProposalRule(rules, intent, allowAlliedOnlyProposal: true);
            }
            else if (SiegeCastlePlayerAuthorizationPolicy.IsGeneralDispositionAdviceRequest(facts.PlayerText))
            {
                AddAlliedProposalRules(rules);
            }
            return rules;
        }

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.RegularPrisoner)
        {
            if (facts.RemainingRegularPrisoners <= 0
                || facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
            {
                return rules;
            }

            if (TryAddDirectRule(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule)
                || TryAddDirectRule(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule))
            {
                return rules;
            }
            if (TryAddRegularPrisonerDispositionRule(rules, facts))
            {
                return rules;
            }

            if (rules.Count == 0)
            {
                SiegeCastlePlayerAuthorizationDecision disposition = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
                    facts.PlayerText,
                    facts.PendingProposalForSpeaker);
                if (disposition.ReasonCode != "player_rejected_or_cancelled")
                {
                    SiegeCastlePrisonerDispositionKind intent = SiegeCastlePlayerAuthorizationPolicy.DetectIntent(facts.PlayerText);
                    if (intent != SiegeCastlePrisonerDispositionKind.None)
                    {
                        AddProposalRule(rules, intent, allowAlliedOnlyProposal: false);
                    }
                    else if (SiegeCastlePlayerAuthorizationPolicy.IsGeneralDispositionAdviceRequest(facts.PlayerText))
                    {
                        AddRegularPrisonerProposalRules(rules);
                    }
                }
            }
            return rules;
        }

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord
            && facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown)
        {
            if (TryAddDirectRule(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule)
                || TryAddDirectRule(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule))
            {
                return rules;
            }
            SiegeCastleLordRecruitmentBranch branch = SiegeCastleLordRecruitmentBranchProfile.Resolve(
                facts.SpeakerIsClanLeader,
                facts.PlayerHasKingdom,
                facts.PlayerRulesKingdom,
                facts.PlayerText);
            if (branch != SiegeCastleLordRecruitmentBranch.Unknown)
            {
                if (TryAddDirectRule(rules, facts, SiegeCastleActionKind.RecruitLord, RecruitLordRule))
                {
                    return rules;
                }
            }
            TryAddDirectRule(rules, facts, SiegeCastleActionKind.ExecuteLord, ExecuteLordRule);
        }
        return rules;
    }

    private static bool TryAddDirectRule(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts,
        SiegeCastleActionKind action,
        SiegePostprocessRuleDefinition rule)
    {
        if (facts.IsActionAlreadyApplied(action))
        {
            return false;
        }
        SiegeCastleDirectActionAuthorizationDecision authorization =
            SiegeCastleDirectActionAuthorizationPolicy.Evaluate(action, facts.PlayerText, facts.PendingProposalForSpeaker);
        if (authorization.IsAuthorized)
        {
            rules.Add(rule);
            return true;
        }
        return false;
    }

    private static bool TryAddAlliedAuthorizedDispositionRule(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts,
        SiegeCastlePrisonerDispositionKind disposition)
    {
        return disposition switch
        {
            SiegeCastlePrisonerDispositionKind.Recruit => TryAddDirectRule(rules, facts, SiegeCastleActionKind.RecruitPrisonersForced, ForcedRecruitRule),
            SiegeCastlePrisonerDispositionKind.Slaughter => TryAddDirectRule(rules, facts, SiegeCastleActionKind.SlaughterPrisoners, SlaughterRule),
            SiegeCastlePrisonerDispositionKind.Release => TryAddDirectRule(rules, facts, SiegeCastleActionKind.ReleasePrisoners, ReleaseRule),
            SiegeCastlePrisonerDispositionKind.Sell => TryAddDirectRule(rules, facts, SiegeCastleActionKind.SellPrisoners, SellRule),
            SiegeCastlePrisonerDispositionKind.Labor => TryAddDirectRule(rules, facts, SiegeCastleActionKind.LaborPrisonersForced, ForcedLaborRule),
            SiegeCastlePrisonerDispositionKind.Instructor => TryAddDirectRule(rules, facts, SiegeCastleActionKind.InstructorPrisonersForced, ForcedInstructorRule),
            _ => false
        };
    }

    private static bool TryAddRegularPrisonerDispositionRule(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts)
    {
        SiegeCastlePlayerAuthorizationDecision authorization = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
            facts.PlayerText,
            facts.PendingProposalForSpeaker);
        SiegeCastlePrisonerDispositionKind intent = authorization.IsAuthorized
            ? authorization.Disposition
            : SiegeCastlePlayerAuthorizationPolicy.DetectIntent(facts.PlayerText);
        bool voluntaryContext = authorization.UsedPendingProposal
            || SiegeCastlePlayerAuthorizationPolicy.IsDiscussionText(facts.PlayerText);

        switch (intent)
        {
            case SiegeCastlePrisonerDispositionKind.Release when authorization.IsAuthorized:
                return TryAddDirectRule(rules, facts, SiegeCastleActionKind.ReleasePrisoners, ReleaseRule);
            case SiegeCastlePrisonerDispositionKind.Sell when authorization.IsAuthorized:
                return TryAddDirectRule(rules, facts, SiegeCastleActionKind.SellPrisoners, SellRule);
            case SiegeCastlePrisonerDispositionKind.Slaughter when authorization.IsAuthorized:
                return TryAddDirectRule(rules, facts, SiegeCastleActionKind.SlaughterPrisoners, SlaughterRule);
            case SiegeCastlePrisonerDispositionKind.Recruit:
                return TryAddConsentSensitiveRule(
                    rules,
                    facts,
                    authorization,
                    voluntaryContext,
                    SiegeCastleActionKind.RecruitPrisonersVoluntary,
                    VoluntaryRecruitRule,
                    SiegeCastleActionKind.RecruitPrisonersForced,
                    ForcedRecruitRule);
            case SiegeCastlePrisonerDispositionKind.Labor:
                return TryAddConsentSensitiveRule(
                    rules,
                    facts,
                    authorization,
                    voluntaryContext,
                    SiegeCastleActionKind.LaborPrisonersVoluntary,
                    VoluntaryLaborRule,
                    SiegeCastleActionKind.LaborPrisonersForced,
                    ForcedLaborRule);
            case SiegeCastlePrisonerDispositionKind.Instructor:
                return TryAddConsentSensitiveRule(
                    rules,
                    facts,
                    authorization,
                    voluntaryContext,
                    SiegeCastleActionKind.InstructorPrisonersVoluntary,
                    VoluntaryInstructorRule,
                    SiegeCastleActionKind.InstructorPrisonersForced,
                    ForcedInstructorRule);
            default:
                return false;
        }
    }

    private static bool TryAddConsentSensitiveRule(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePostprocessRuleFacts facts,
        SiegeCastlePlayerAuthorizationDecision authorization,
        bool voluntaryContext,
        SiegeCastleActionKind voluntaryAction,
        SiegePostprocessRuleDefinition voluntaryRule,
        SiegeCastleActionKind forcedAction,
        SiegePostprocessRuleDefinition forcedRule)
    {
        if (voluntaryContext)
        {
            return SiegeCastlePrisonerTrustProfile.MeetsVoluntaryThreshold(voluntaryAction, facts.SpeakerTrust)
                && TryAddDirectRule(rules, facts, voluntaryAction, voluntaryRule);
        }
        return authorization.IsAuthorized
            && TryAddDirectRule(rules, facts, forcedAction, forcedRule);
    }

    private static void AddAlliedProposalRules(ICollection<SiegePostprocessRuleDefinition> rules)
    {
        rules.Add(ProposeRecruitRule);
        rules.Add(ProposeSlaughterRule);
        rules.Add(ProposeReleaseRule);
        rules.Add(ProposeSellRule);
        rules.Add(ProposeLaborRule);
        rules.Add(ProposeInstructorRule);
    }

    private static void AddRegularPrisonerProposalRules(ICollection<SiegePostprocessRuleDefinition> rules)
    {
        rules.Add(ProposeReleaseRule);
        rules.Add(ProposeRecruitRule);
        rules.Add(ProposeLaborRule);
        rules.Add(ProposeInstructorRule);
    }

    private static void AddProposalRule(
        ICollection<SiegePostprocessRuleDefinition> rules,
        SiegeCastlePrisonerDispositionKind disposition,
        bool allowAlliedOnlyProposal)
    {
        switch (disposition)
        {
            case SiegeCastlePrisonerDispositionKind.Recruit:
                rules.Add(ProposeRecruitRule);
                break;
            case SiegeCastlePrisonerDispositionKind.Slaughter when allowAlliedOnlyProposal:
                rules.Add(ProposeSlaughterRule);
                break;
            case SiegeCastlePrisonerDispositionKind.Release:
                rules.Add(ProposeReleaseRule);
                break;
            case SiegeCastlePrisonerDispositionKind.Sell when allowAlliedOnlyProposal:
                rules.Add(ProposeSellRule);
                break;
            case SiegeCastlePrisonerDispositionKind.Labor:
                rules.Add(ProposeLaborRule);
                break;
            case SiegeCastlePrisonerDispositionKind.Instructor:
                rules.Add(ProposeInstructorRule);
                break;
        }
    }

    private static SiegePostprocessRuleDefinition Rule(string tag, string description)
        => new SiegePostprocessRuleDefinition(tag, description);
}
