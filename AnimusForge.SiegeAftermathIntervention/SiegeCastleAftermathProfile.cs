using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for the castle variant of GCCZ aftermath.
/// Castle aftermath is military administration: captive lords, defeated soldier prisoners,
/// prisoner recruitment, ransom, armory, castle loyalty/security, and bound-village output.
/// It deliberately does not use town civilian trust or civilian massacre policy as its core state.
/// </summary>
public static class SiegeCastleAftermathProfile
{
    public const string RuleId = "siege_castle_aftermath";

    public const string InjectedRuleBlockMarker = "【附加规则:siege_castle_aftermath】";

    public const string CaptiveLordRoleFact = "被俘领主/守将：战败且受胜利方看押；领主个人标签只适用于当前这一个领主，不能代表普通守军自动同意全体收编。";

    public const string GarrisonRepresentativeRoleFact = "战败士兵俘虏代表：普通士兵俘虏统一按战败俘虏理解；可随机表现为被俘守军或野战被俘士兵。";

    public const string CaptivePrisonerRoleFact = "战败士兵俘虏：知道自己已被胜利方押入城堡处置，武器已被收缴但盔甲仍保留，可能面临赎卖、劳役、收编、屠戮或处决；不得自称胜利方士兵、自由战士或普通城镇平民。";

    public const string AlliedSoldierRepresentativeRoleFact = "玩家士兵代表：代表胜利方士兵请求军令、战利品、看押、复仇、收编或军械接收。";

    public const string PrisonerGuardRoleFact = "看押士兵：负责解除武装、押送俘虏、维持队列和防止战败士兵俘虏暴动。";

    public const int MaxRecruitmentPercent = 100;

    public const int MaxLaborPrisonerPercent = 50;

    public const int HonorCaptiveTrustDelta = 20;

    public const int HonorSoldierMoraleDelta = -20;

    public const int ArmorySoldierMoraleDelta = 10;

    public const int ArmoryReceiptWeight = 50;

    public const int RecruitPrisonerLordRelationDelta = -10;

    public const int RecruitPrisonerCastleLoyaltyDelta = -20;

    public const int RecruitPrisonerCastleSecurityDelta = -20;

    public const int RecruitPrisonerMoraleDelta = -30;

    public const int LaborPrisonerLordRelationDelta = -15;

    public const int LaborPrisonerCastleLoyaltyDelta = -10;

    public const int LaborPrisonerCastleSecurityDelta = 5;

    public const int LaborPrisonerVillageProductionBonusPercent = 20;

    public const int LaborPrisonerCaptiveTrustDelta = -5;

    public const int LaborPrisonerFearDelta = 1;

    public const int SlaughterGarrisonLordRelationDelta = -30;

    public const int SlaughterGarrisonCastleLoyaltyDelta = -20;

    public const int SlaughterGarrisonCastleSecurityDelta = -20;

    public const int SlaughterGarrisonVillageProductionBonusPercent = -10;

    public const int SlaughterGarrisonCaptiveTrustDelta = -40;

    public const int SlaughterGarrisonFearDelta = 2;

    public const int SlaughterGarrisonMoraleDelta = 30;

    public const int SlaughterGarrisonTroopXpWeight = 100;

    public const int SellPrisonerLordRelationDelta = -5;

    public const int SellPrisonerCastleLoyaltyDelta = -5;

    public const int SellPrisonerCastleSecurityDelta = -5;

    public const int SellPrisonerCaptiveTrustDelta = -5;

    public const int SellPrisonerFearDelta = 1;

    public const int SellPrisonerMoraleDelta = 15;

    public const int DemandRansomLordRelationDelta = -10;

    public const int DemandRansomSecurityDelta = -1;

    public const int DemandRansomGoldWeight = 50;

    public const int ExecuteClanLeaderRelationDelta = -80;

    public const int ExecuteNonLeaderRelationDelta = -30;

    public const int ExecuteLordCastleLoyaltyDelta = -10;

    public const int ExecuteLordCastleSecurityDelta = -10;

    public const int ExecuteLordRansomGoldWeight = -50;

    public const float ArmoryLootMinRatio = 0.12f;

    public const float ArmoryLootMaxRatio = 0.28f;

    private static readonly SiegeCastleAftermathRuleDefinition[] Rules =
    {
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.HonorCaptives, "[ACTION:优待战俘]", "玩家明确优待战俘、战败士兵俘虏或被俘军官；提高俘虏对玩家信任，但会降低己方士气并引发士兵不满，需要安兵安抚。与接收军械互斥。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.DemandRansom, "[ACTION:索要赎金]", "玩家明确向当前被俘领主、守将、家族或军官索取赎金、交换俘虏或要求写信筹款；只适用于当前这一个战败领主。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.RecruitLord, "[ACTION:收编领主]", "玩家明确试图收编当前这一个战败领主，且NPC明确接受进入收编/引荐流程时才输出；不能因玩家随口劝降、试探、威胁或NPC含糊拖延而输出。若其是家族族长：玩家为统治者则请求加入玩家国家；玩家不是统治者则请求带他去见统治者；玩家无国家则表达拥立玩家为王。若其不是族长：默认由该NPC为玩家写信引见家族族长，信件1-2天后到达；除非正文明确背叛家族成为玩家同伴，否则不要写成直接收编成功。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.RecruitGarrison, "[ACTION:收编战俘]", "玩家明确要求战败士兵俘虏换旗、加入玩家军队或编入新驻军；该标签默认招募当前可处置的全部普通士兵俘虏，并要求后续回应承认此前劳役、贩卖或屠戮命令留下的顺序记忆。普通被俘领主同意不等于士兵俘虏全体同意。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.SeizeArmory, "[ACTION:接收军械]", "玩家明确命令接收军械。若目标是被俘领主/守将：表示接收当前领主随身武器和盔甲给玩家；若目标是士兵俘虏或看押士兵：表示按指定数量缴械并在离场结算同数量士兵战利品的50%。与优待战俘互斥。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.LaborPrisoners, "[ACTION:战俘劳役]", "玩家明确命令将战俘或战败士兵俘虏派去修路、修桥、运粮，或分配到附属村庄当农奴劳作；不是处决、屠戮或普通收编。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.SlaughterGarrison, "[ACTION:屠戮守军]", "玩家明确命令杀死、清洗、屠戮已经被俘的战败士兵俘虏；必须是士兵/战俘目标，不是城镇平民血洗。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.SellPrisoners, "[ACTION:贩卖俘虏]", "玩家明确命令按数量把普通士兵俘虏卖给赎买人、奴隶贩子、酒馆或军需商；必须使用数量后缀，例如 [ACTION:贩卖俘虏:40]；不适用于被俘领主。"),
        new SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind.ExecuteLord, "[ACTION:处决领主]", "玩家明确命令处决当前这一个被俘领主、守将或贵族军官；该行为不可逆，不能由普通士兵主动请示自动触发。"),
    };

    private static readonly SiegeCastleAftermathEffectProfile[] Effects =
    {
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.HonorCaptives, lordRelationDelta: 0, castleLoyaltyDelta: 0, castleSecurityDelta: 0, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: 0, armoryReceiptWeight: 0, captiveTrustDelta: HonorCaptiveTrustDelta, captiveFearDelta: 0, playerTroopMoraleDelta: HonorSoldierMoraleDelta, playerTroopXpWeight: 0, requiresSoldierAppeasement: true, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: true, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.DemandRansom, lordRelationDelta: DemandRansomLordRelationDelta, castleLoyaltyDelta: 0, castleSecurityDelta: DemandRansomSecurityDelta, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: DemandRansomGoldWeight, armoryReceiptWeight: 0, captiveTrustDelta: 0, captiveFearDelta: 0, playerTroopMoraleDelta: 0, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: false, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.RecruitLord, lordRelationDelta: 0, castleLoyaltyDelta: 0, castleSecurityDelta: 0, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: -20, armoryReceiptWeight: 0, captiveTrustDelta: 10, captiveFearDelta: 0, playerTroopMoraleDelta: 0, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: true, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: false, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.RecruitGarrison, lordRelationDelta: RecruitPrisonerLordRelationDelta, castleLoyaltyDelta: RecruitPrisonerCastleLoyaltyDelta, castleSecurityDelta: RecruitPrisonerCastleSecurityDelta, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: MaxRecruitmentPercent, laborPrisonerPercent: 0, ransomGoldWeight: 0, armoryReceiptWeight: 0, captiveTrustDelta: 0, captiveFearDelta: 0, playerTroopMoraleDelta: RecruitPrisonerMoraleDelta, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: false, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.SeizeArmory, lordRelationDelta: 0, castleLoyaltyDelta: 0, castleSecurityDelta: 0, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: 0, armoryReceiptWeight: ArmoryReceiptWeight, captiveTrustDelta: 0, captiveFearDelta: 0, playerTroopMoraleDelta: ArmorySoldierMoraleDelta, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: true, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: true, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.LaborPrisoners, lordRelationDelta: LaborPrisonerLordRelationDelta, castleLoyaltyDelta: LaborPrisonerCastleLoyaltyDelta, castleSecurityDelta: LaborPrisonerCastleSecurityDelta, boundVillageProductionBonusPercent: LaborPrisonerVillageProductionBonusPercent, recruitablePrisonerPercent: 0, laborPrisonerPercent: MaxLaborPrisonerPercent, ransomGoldWeight: 0, armoryReceiptWeight: 0, captiveTrustDelta: LaborPrisonerCaptiveTrustDelta, captiveFearDelta: LaborPrisonerFearDelta, playerTroopMoraleDelta: 0, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: false, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.SlaughterGarrison, lordRelationDelta: SlaughterGarrisonLordRelationDelta, castleLoyaltyDelta: SlaughterGarrisonCastleLoyaltyDelta, castleSecurityDelta: SlaughterGarrisonCastleSecurityDelta, boundVillageProductionBonusPercent: SlaughterGarrisonVillageProductionBonusPercent, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: 0, armoryReceiptWeight: 0, captiveTrustDelta: SlaughterGarrisonCaptiveTrustDelta, captiveFearDelta: SlaughterGarrisonFearDelta, playerTroopMoraleDelta: SlaughterGarrisonMoraleDelta, playerTroopXpWeight: SlaughterGarrisonTroopXpWeight, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: false, isDestructive: true, isIrreversible: true),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.SellPrisoners, lordRelationDelta: SellPrisonerLordRelationDelta, castleLoyaltyDelta: SellPrisonerCastleLoyaltyDelta, castleSecurityDelta: SellPrisonerCastleSecurityDelta, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: 0, armoryReceiptWeight: 0, captiveTrustDelta: SellPrisonerCaptiveTrustDelta, captiveFearDelta: SellPrisonerFearDelta, playerTroopMoraleDelta: SellPrisonerMoraleDelta, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: 0, nonLeaderRelationDelta: 0, isProcessOnly: false, isDestructive: false, isIrreversible: false),
        new SiegeCastleAftermathEffectProfile(SiegeCastleAftermathActionKind.ExecuteLord, lordRelationDelta: ExecuteNonLeaderRelationDelta, castleLoyaltyDelta: ExecuteLordCastleLoyaltyDelta, castleSecurityDelta: ExecuteLordCastleSecurityDelta, boundVillageProductionBonusPercent: 0, recruitablePrisonerPercent: 0, laborPrisonerPercent: 0, ransomGoldWeight: ExecuteLordRansomGoldWeight, armoryReceiptWeight: 0, captiveTrustDelta: -30, captiveFearDelta: 0, playerTroopMoraleDelta: 0, playerTroopXpWeight: 0, requiresSoldierAppeasement: false, grantsArmoryLoot: false, recruitsDefeatedLord: false, clanLeaderRelationDelta: ExecuteClanLeaderRelationDelta, nonLeaderRelationDelta: ExecuteNonLeaderRelationDelta, isProcessOnly: false, isDestructive: true, isIrreversible: true),
    };

    public static IReadOnlyList<SiegeCastleAftermathRuleDefinition> GetRules()
    {
        return Rules;
    }

    public static IReadOnlyList<SiegeCastleAftermathEffectProfile> GetEffects()
    {
        return Effects;
    }

    public static bool TryGetRule(SiegeCastleAftermathActionKind kind, out SiegeCastleAftermathRuleDefinition rule)
    {
        for (int i = 0; i < Rules.Length; i++)
        {
            if (Rules[i].Kind == kind)
            {
                rule = Rules[i];
                return true;
            }
        }

        rule = default;
        return false;
    }

    public static SiegeCastleAftermathEffectProfile GetEffect(SiegeCastleAftermathActionKind kind)
    {
        for (int i = 0; i < Effects.Length; i++)
        {
            if (Effects[i].Kind == kind)
            {
                return Effects[i];
            }
        }

        return default;
    }

    public static bool TryParseCanonicalTag(string tag, out SiegeCastleAftermathActionKind kind)
    {
        string normalized = (tag ?? string.Empty).Trim();
        for (int i = 0; i < Rules.Length; i++)
        {
            if (string.Equals(Rules[i].CanonicalTag, normalized, StringComparison.OrdinalIgnoreCase))
            {
                kind = Rules[i].Kind;
                return true;
            }
        }

        kind = default;
        return false;
    }

    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetPostprocessRules()
    {
        var result = new SiegePostprocessRuleDefinition[Rules.Length];
        for (int i = 0; i < Rules.Length; i++)
        {
            result[i] = new SiegePostprocessRuleDefinition(Rules[i].CanonicalTag, Rules[i].Description);
        }

        return result;
    }

    public static bool IsLordFocused(SiegeCastleAftermathActionKind kind)
    {
        return kind == SiegeCastleAftermathActionKind.DemandRansom
            || kind == SiegeCastleAftermathActionKind.RecruitLord
            || kind == SiegeCastleAftermathActionKind.ExecuteLord;
    }

    public static bool IsGarrisonFocused(SiegeCastleAftermathActionKind kind)
    {
        return kind == SiegeCastleAftermathActionKind.HonorCaptives
            || kind == SiegeCastleAftermathActionKind.RecruitGarrison
            || kind == SiegeCastleAftermathActionKind.LaborPrisoners
            || kind == SiegeCastleAftermathActionKind.SlaughterGarrison
            || kind == SiegeCastleAftermathActionKind.SellPrisoners;
    }

    public static bool IsMilitaryLogisticsFocused(SiegeCastleAftermathActionKind kind)
    {
        return kind == SiegeCastleAftermathActionKind.SeizeArmory
            || kind == SiegeCastleAftermathActionKind.RecruitGarrison
            || kind == SiegeCastleAftermathActionKind.LaborPrisoners
            || kind == SiegeCastleAftermathActionKind.SellPrisoners;
    }

    public static int ClampRecruitmentPercent(int percent)
    {
        if (percent < 0)
        {
            return 0;
        }

        return percent > MaxRecruitmentPercent ? MaxRecruitmentPercent : percent;
    }

    public static int ClampLaborPrisonerPercent(int percent)
    {
        if (percent < 0)
        {
            return 0;
        }

        return percent > MaxLaborPrisonerPercent ? MaxLaborPrisonerPercent : percent;
    }

    public static string BuildRuntimeFactBlock(string castleName, int captiveLordCount, int surrenderedGarrisonCount, int alliedSoldierCount, bool hasArmory, int carriedRegularPrisonerCount = 0)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return InjectedRuleBlockMarker + "\n"
            + "【城堡处置定位】当前是城堡战后军务处置，不是城镇平民处置；核心对象是被俘领主、战败士兵俘虏、玩家士兵、战俘看押、军械、城堡忠诚/治安和附属村庄产出。\n"
            + "【城堡】" + safeCastleName + "\n"
            + "【被俘领主/守将】" + Math.Max(0, captiveLordCount) + " 人\n"
            + "【战败士兵俘虏】" + (Math.Max(0, surrenderedGarrisonCount) + Math.Max(0, carriedRegularPrisonerCount)) + " 人（可随机表现为被俘守军或野战被俘士兵；玩家此前携带的大量士兵俘虏最多按 " + SiegeCastleFinalSideEffectProfile.MaxCarriedRegularPrisonerSourceCount + " 人参与劳役/收编来源；数量标签只消耗普通士兵俘虏，不消耗被俘领主）\n"
            + "【玩家入堡士兵】" + Math.Max(0, alliedSoldierCount) + " 人\n"
            + "【军械库】" + (hasArmory ? "可接收/登记/分配" : "未确认或不可用") + "\n"
            + "【角色边界】" + CaptiveLordRoleFact + "\n"
            + "【角色边界】" + GarrisonRepresentativeRoleFact + "\n"
            + "【角色边界】" + CaptivePrisonerRoleFact + "\n"
            + "【角色边界】" + AlliedSoldierRepresentativeRoleFact + "\n"
            + "【角色边界】" + PrisonerGuardRoleFact + "\n"
            + "【收编领主限制】" + SiegeCastleLordIntroductionProfile.RecruitLordStrictRuntimeRule + "\n"
            + "【数量标签】" + SiegeCastlePrisonerAllocationProfile.QuantityInstruction;
    }

    public static string BuildActionMemoryText(SiegeCastleAftermathActionKind kind, string actorName, string castleName)
    {
        SiegeCastleAftermathEffectProfile effect = GetEffect(kind);
        string actor = string.IsNullOrWhiteSpace(actorName) ? "玩家" : actorName.Trim();
        string castle = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return actor + "在" + castle + (effect.IsProcessOnly ? "推进城堡战后流程：" : "下达城堡战后军务结算：") + GetActionLabel(kind)
            + "。" + BuildEffectSummary(effect);
    }

    public static string BuildActionMessageText(SiegeCastleAftermathActionKind kind)
    {
        SiegeCastleAftermathEffectProfile effect = GetEffect(kind);
        return (effect.IsProcessOnly ? "【城堡流程】" : "【城堡处置】")
            + GetActionLabel(kind)
            + (effect.IsProcessOnly ? " 已记录，并会按流程影响俘虏、士兵或军械。" : " 已记录为城堡军务结算。");
    }

    public static string BuildEffectSummary(SiegeCastleAftermathEffectProfile effect)
    {
        return "领主好感 " + FormatSigned(effect.LordRelationDelta)
            + "，城堡忠诚 " + FormatSigned(effect.CastleLoyaltyDelta)
            + "，城堡治安 " + FormatSigned(effect.CastleSecurityDelta)
            + "，附属村庄产出 " + FormatSigned(effect.BoundVillageProductionBonusPercent) + "%"
            + "，可收编俘虏 " + effect.RecruitablePrisonerPercent + "%"
            + "，劳役战俘 " + effect.LaborPrisonerPercent + "%"
            + "，赎金权重 " + FormatSigned(effect.RansomGoldWeight)
            + "，军械接收 " + FormatSigned(effect.ArmoryReceiptWeight)
            + "，俘虏信任 " + FormatSigned(effect.CaptiveTrustDelta)
            + "，俘虏恐惧 " + FormatSigned(effect.CaptiveFearDelta)
            + "，己方士气 " + FormatSigned(effect.PlayerTroopMoraleDelta)
            + "，部队经验权重 " + FormatSigned(effect.PlayerTroopXpWeight)
            + (effect.RequiresSoldierAppeasement ? "，需要安兵" : string.Empty)
            + (effect.GrantsArmoryLoot ? "，离场军械战利品" : string.Empty)
            + (effect.RecruitsDefeatedLord ? "，触发战败领主收编流程" : string.Empty)
            + (effect.ClanLeaderRelationDelta != 0 ? "，族长关系 " + FormatSigned(effect.ClanLeaderRelationDelta) : string.Empty)
            + (effect.NonLeaderRelationDelta != 0 ? "，非族长关系 " + FormatSigned(effect.NonLeaderRelationDelta) : string.Empty)
            + "。";
    }

    public static string GetActionLabel(SiegeCastleAftermathActionKind kind)
    {
        switch (kind)
        {
            case SiegeCastleAftermathActionKind.HonorCaptives:
                return "优待战俘";
            case SiegeCastleAftermathActionKind.DemandRansom:
                return "索要赎金";
            case SiegeCastleAftermathActionKind.RecruitLord:
                return "收编领主";
            case SiegeCastleAftermathActionKind.RecruitGarrison:
                return "收编战俘";
            case SiegeCastleAftermathActionKind.SeizeArmory:
                return "接收军械";
            case SiegeCastleAftermathActionKind.LaborPrisoners:
                return "战俘劳役";
            case SiegeCastleAftermathActionKind.SlaughterGarrison:
                return "屠戮守军";
            case SiegeCastleAftermathActionKind.SellPrisoners:
                return "贩卖俘虏";
            case SiegeCastleAftermathActionKind.ExecuteLord:
                return "处决领主";
            default:
                return "未知军务";
        }
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? ("+" + value) : value.ToString();
    }
}

public enum SiegeCastleAftermathActionKind
{
    Unknown = 0,
    HonorCaptives,
    DemandRansom,
    RecruitLord,
    RecruitGarrison,
    SeizeArmory,
    LaborPrisoners,
    SlaughterGarrison,
    SellPrisoners,
    ExecuteLord,
}

public readonly struct SiegeCastleAftermathRuleDefinition
{
    public SiegeCastleAftermathRuleDefinition(SiegeCastleAftermathActionKind kind, string canonicalTag, string description)
    {
        Kind = kind;
        CanonicalTag = canonicalTag ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public SiegeCastleAftermathActionKind Kind { get; }

    public string CanonicalTag { get; }

    public string Description { get; }
}

public readonly struct SiegeCastleAftermathEffectProfile
{
    public SiegeCastleAftermathEffectProfile(
        SiegeCastleAftermathActionKind kind,
        int lordRelationDelta,
        int castleLoyaltyDelta,
        int castleSecurityDelta,
        int boundVillageProductionBonusPercent,
        int recruitablePrisonerPercent,
        int laborPrisonerPercent,
        int ransomGoldWeight,
        int armoryReceiptWeight,
        int captiveTrustDelta,
        int captiveFearDelta,
        int playerTroopMoraleDelta,
        int playerTroopXpWeight,
        bool requiresSoldierAppeasement,
        bool grantsArmoryLoot,
        bool recruitsDefeatedLord,
        int clanLeaderRelationDelta,
        int nonLeaderRelationDelta,
        bool isProcessOnly,
        bool isDestructive,
        bool isIrreversible)
    {
        Kind = kind;
        LordRelationDelta = lordRelationDelta;
        CastleLoyaltyDelta = castleLoyaltyDelta;
        CastleSecurityDelta = castleSecurityDelta;
        BoundVillageProductionBonusPercent = boundVillageProductionBonusPercent;
        RecruitablePrisonerPercent = SiegeCastleAftermathProfile.ClampRecruitmentPercent(recruitablePrisonerPercent);
        LaborPrisonerPercent = SiegeCastleAftermathProfile.ClampLaborPrisonerPercent(laborPrisonerPercent);
        RansomGoldWeight = ransomGoldWeight;
        ArmoryReceiptWeight = armoryReceiptWeight;
        CaptiveTrustDelta = captiveTrustDelta;
        CaptiveFearDelta = captiveFearDelta;
        PlayerTroopMoraleDelta = playerTroopMoraleDelta;
        PlayerTroopXpWeight = playerTroopXpWeight;
        RequiresSoldierAppeasement = requiresSoldierAppeasement;
        GrantsArmoryLoot = grantsArmoryLoot;
        RecruitsDefeatedLord = recruitsDefeatedLord;
        ClanLeaderRelationDelta = clanLeaderRelationDelta;
        NonLeaderRelationDelta = nonLeaderRelationDelta;
        IsProcessOnly = isProcessOnly;
        IsDestructive = isDestructive;
        IsIrreversible = isIrreversible;
    }

    public SiegeCastleAftermathActionKind Kind { get; }

    public int LordRelationDelta { get; }

    public int CastleLoyaltyDelta { get; }

    public int CastleSecurityDelta { get; }

    public int BoundVillageProductionBonusPercent { get; }

    public int RecruitablePrisonerPercent { get; }

    public int LaborPrisonerPercent { get; }

    public int RansomGoldWeight { get; }

    public int ArmoryReceiptWeight { get; }

    public int CaptiveTrustDelta { get; }

    public int CaptiveFearDelta { get; }

    public int PlayerTroopMoraleDelta { get; }

    public int PlayerTroopXpWeight { get; }

    public bool RequiresSoldierAppeasement { get; }

    public bool GrantsArmoryLoot { get; }

    public bool RecruitsDefeatedLord { get; }

    public int ClanLeaderRelationDelta { get; }

    public int NonLeaderRelationDelta { get; }

    public bool IsProcessOnly { get; }

    public bool IsDestructive { get; }

    public bool IsIrreversible { get; }
}
