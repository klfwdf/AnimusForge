using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for GCCZ scene-entry UI wording.
/// AF adapters still resolve Bannerlord settlements, locations, menu args, and display side effects.
/// </summary>
public static class SiegeInterventionEntryProfile
{
    public const string TownCenterLocationId = "center";

    public const string CastleLordHallLocationId = "lordshall";

    public const int DefaultAutoSummonCount = 50;

    public const int MaxSummonPerAction = 50;

    public const uint MissingSceneMessageColor = 0xFFFF7777u;

    public const uint EntryInstructionMessageColor = 0xFFB6F7A8u;

    public const uint SelectionConfirmedMessageColor = 0xFFB6F7A8u;

    public const uint SelectionFallbackMessageColor = 0xFFFFD27Fu;

    public const uint BattleEquipmentAppliedMessageColor = 0xFFB6F7A8u;

    public const uint NoHealthyTroopsMessageColor = 0xFFFFD27Fu;

    public const uint SummonedTroopsMessageColor = 0xFFB6F7A8u;

    public const string EntryMenuOptionText = "亲自进城决定";

    public const string SelectionUnavailableMissionSource = "selection_unavailable";

    public const string TroopSelectionDoneMissionSource = "game_menu_troop_selection_done";

    public const string SceneEntryCleanupSource = "siege_intervention_enter_isolated_scene";

    public const string AutoEnterSummonSource = "auto_enter";

    public const string EnsureAlliedTroopsSummonSource = "ensure";

    public static readonly string EnabledTooltip = "{=!}暂不立即处置战后事务；你将披甲带约" + DefaultAutoSummonCount + "名健康士兵进城，普通民众仍散在城内街区，再由现场对话或行动决定安抚、宽恕、搜掠或血洗。";

    public static readonly string CastleEnabledTooltip = "{=!}暂不立即处置城堡战后军务；你将披甲带约" + DefaultAutoSummonCount + "名健康士兵入堡，围绕被俘领主、战败士兵俘虏、战俘看押、军械库、城堡忠诚/治安和附属村庄产出做出处置。";

    public const string MissingSceneTooltip = "{=!}当前没有可进入的攻城胜利定居点场景。";

    public const string EntryMemoryTitle = "入城处置";

    public const string MissingSceneMessage = "【攻城处置】当前没有可进入的被攻陷定居点场景。";

    public const string DecisionPolicyMessage = "【攻城处置】处置方式由你现场决定：直接离场按搜掠结算；明确宽恕、安抚或宣抚会按对应处置结算；搜掠仍可因后续宽恕/宣抚回退；血洗不能回退为搜掠或正向处置，但仍可继续升级为屠民迁殖；屠民迁殖也可一开始直接触发。";

    public const string CastleDecisionPolicyMessage = "【城堡处置】优待战俘与接收军械互斥：优待战俘提高俘虏信任但降低己方士气并需要安兵；接收军械提高己方士气且离场给军械战利品。领主个人标签只适用于当前一个战败领主：索要赎金、收编领主、处决领主。士兵战俘标签：收编战俘、战俘劳役、屠戮守军、贩卖俘虏；后续回应必须承认这些标签的触发顺序。城堡不使用城镇平民信任作为核心结算。";

    public const string EntryFailedMessage = "【攻城处置】暂时无法进入被攻陷的定居点场景。";

    public const string SelectionFallbackMessage = "【攻城处置】未选择随行队员，将自动带入健康普通士兵。";

    public const string BattleEquipmentAppliedMessage = "【攻城处置】你已披甲执兵入城。";

    public const string NoHealthyTroopsMessage = "【攻城处置】主部队没有可入城的健康士兵或同伴。";

    public static string BuildEntryMemoryText(string settlementName)
    {
        string safeSettlementName = string.IsNullOrWhiteSpace(settlementName)
            ? "这座刚被攻下的定居点"
            : settlementName.Trim();
        return "玩家已经攻陷" + safeSettlementName + "并亲自进入城内处置战后秩序；旧守军已失败，旧领主已被打败，平民、商人、工匠、头人和要人都应知道自己处在胜利方处置现场，同时保留本文化、本定居点和个人旧记忆作为反应细节。";
    }

    public static string BuildEntryMemoryText(string settlementName, bool isCastle)
    {
        if (!isCastle)
        {
            return BuildEntryMemoryText(settlementName);
        }

        string safeSettlementName = string.IsNullOrWhiteSpace(settlementName)
            ? "这座刚被攻下的城堡"
            : settlementName.Trim();
        return "玩家已经攻陷" + safeSettlementName + "并亲自进入城堡处置战后军务；核心对象是被俘领主、战败士兵俘虏、玩家士兵、战俘看押、军械库、城堡忠诚/治安和附属村庄产出，不应按城镇平民信任或平民血洗逻辑理解。";
    }

    public static bool IsSupportedSettlementKind(bool isTown, bool isCastle)
    {
        return isTown || isCastle;
    }

    public static string BuildEnabledTooltip(bool isCastle)
    {
        return isCastle ? CastleEnabledTooltip : EnabledTooltip;
    }

    public static string BuildDecisionPolicyMessage(bool isCastle)
    {
        return isCastle ? CastleDecisionPolicyMessage : DecisionPolicyMessage;
    }

    public static IReadOnlyList<string> GetPreferredLocationIds(bool isTown, bool isCastle)
    {
        if (isCastle)
        {
            return new[] { CastleLordHallLocationId, TownCenterLocationId };
        }

        if (isTown)
        {
            return new[] { TownCenterLocationId };
        }

        return System.Array.Empty<string>();
    }

    public static string BuildTroopSelectionInstructionMessage(int maxCount)
    {
        int safeMaxCount = maxCount < 0 ? 0 : maxCount;
        return "【攻城处置】先选择最多 " + safeMaxCount + " 名入城随行士兵或同伴；未选择则自动带入健康普通士兵。";
    }

    public static string BuildSelectionConfirmedMessage(int selectedCount)
    {
        int safeSelectedCount = selectedCount < 0 ? 0 : selectedCount;
        return "【攻城处置】已选择 " + safeSelectedCount + " 名随行队员入城。";
    }

    public static string BuildSummonedTroopsMessage(int spawnedCount)
    {
        int safeSpawnedCount = spawnedCount < 0 ? 0 : spawnedCount;
        return "【攻城处置】已带入 " + safeSpawnedCount + " 名随行士兵/同伴，默认编入一队并保持列队跟随。";
    }
}
