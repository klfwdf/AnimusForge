namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime prompt wording for the active GCCZ post-siege intervention scene.
/// AF adapters still resolve live Bannerlord agents, occupations, mission state, memory, and gather context.
/// </summary>
public static class SiegeRuntimePromptProfile
{
    public const string DefaultSettlementName = "这座刚被攻下的定居点";

    public static string Build(SiegeRuntimePromptFacts facts)
    {
        return Build(facts, TownPromptTextCatalog.CreateEnglishFallback());
    }

    public static string Build(
        SiegeRuntimePromptFacts facts,
        TownPromptTextCatalog textCatalog)
    {
        return TownPromptComposer.BuildMainPrompt(facts, textCatalog);
    }

    public static string BuildPlayerCommanderContext(string playerName, bool alliedSoldier, bool civilian)
    {
        string normalizedPlayerName = NormalizePlayerName(playerName);
        if (alliedSoldier)
        {
            return "【玩家统帅身份】当前玩家角色“" + normalizedPlayerName + "”就是率领你进入城镇的指挥官/统帅，也是你当前队伍的直接命令来源。你应把玩家当成我方统帅、长官或大人，不要把玩家当成本地平民、陌生路人、俘虏、敌方守军或无权处置者。";
        }
        if (civilian)
        {
            return "【玩家身份】当前玩家角色“" + normalizedPlayerName + "”是刚攻下本城的胜利方首领和当前处置者，城内民众应知道玩家掌握现场生杀、安抚、索取与搜掠处置权。";
        }
        return "【玩家身份】当前玩家角色“" + normalizedPlayerName + "”是本场攻城后处置的玩家本人、胜利方首领和现场处置者。";
    }

    public static string BuildImmediateReactionIdentityOverride(string playerName, bool alliedSoldier, bool civilian)
    {
        string normalizedPlayerName = NormalizePlayerName(playerName);
        string speakerIdentity = alliedSoldier
            ? "当前说话者按玩家己方入城士兵处理：玩家就是你的统帅、长官和直接命令来源；你随玩家进入城内执行战后处置，不得说玩家军队在城外、玩家独自进城或玩家无权指挥你。"
            : (civilian
                ? "当前说话者按战败城内平民/商人/工匠/头人/要人处理：玩家是刚攻下本城的胜利方首领和现场处置者，你只能恐惧、求生、谈判或服从，不得把玩家当和平城镇里的路人或本地人。"
                : "当前说话者必须承认玩家是刚攻下本城的胜利方首领和现场处置者；若你是玩家带入城的士兵则服从玩家，若你是城内民众则承认自己处在胜利方处置现场。");

        return "【GCCZ即时/环境短句最高优先级身份覆写】当前是攻城后入城处置现场，不是和平城镇日常、巡逻执法、深夜路人问话或单人潜入。玩家角色“" + normalizedPlayerName + "”不是“库赛特人”“陌生人”“路人”“本地人”或无权处置者，而是刚攻下本城的胜利方首领、现场处置者和入城部队的命令来源。"
            + "平民、镇民、商人、工匠、头人和要人称玩家为“大人”“领主”“攻城者”或“胜利方首领”；玩家己方士兵称玩家为“统帅”“大人”或“长官”。"
            + "禁止称玩家为“库赛特人”“陌生人”“路人”“本地人”“外乡人”，禁止说玩家的军队在城外、玩家独自进城、玩家没有处置权，除非后续运行时明确说明这已不是GCCZ处置现场。"
            + speakerIdentity;
    }

    private static string NormalizePlayerName(string playerName)
    {
        return string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
    }

}

public sealed class SiegeRuntimePromptFacts
{
    public SiegeRuntimePromptFacts(
        string settlementName,
        TownDialogueRole dialogueRole,
        bool isAlliedSoldier,
        bool isGuardOrSoldier,
        bool isCivilian,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        string gatherContext,
        string memoryContext,
        string sharedReliefPoolDescription,
        bool plunderStarted,
        bool massacreStarted)
    {
        SettlementName = settlementName ?? string.Empty;
        DialogueRole = TownDialogueRoleClassifier.NormalizeForRuntime(dialogueRole);
        IsAlliedSoldier = isAlliedSoldier;
        IsGuardOrSoldier = isGuardOrSoldier;
        IsCivilian = isCivilian;
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        GatherContext = gatherContext ?? string.Empty;
        MemoryContext = memoryContext ?? string.Empty;
        SharedReliefPoolDescription = sharedReliefPoolDescription ?? string.Empty;
        PlunderStarted = plunderStarted;
        MassacreStarted = massacreStarted;
    }

    public static SiegeRuntimePromptFacts Empty
    {
        get
        {
            return new SiegeRuntimePromptFacts(
                settlementName: string.Empty,
                dialogueRole: TownDialogueRoleClassifier.SafeFallbackRole,
                isAlliedSoldier: false,
                isGuardOrSoldier: false,
                isCivilian: true,
                soldierAppeasementRequired: false,
                soldierAppeasementApplied: false,
                gatherContext: string.Empty,
                memoryContext: string.Empty,
                sharedReliefPoolDescription: string.Empty,
                plunderStarted: false,
                massacreStarted: false);
        }
    }

    public string SettlementName { get; }

    public TownDialogueRole DialogueRole { get; }

    public bool IsAlliedSoldier { get; }

    public bool IsGuardOrSoldier { get; }

    public bool IsCivilian { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public string GatherContext { get; }

    public string MemoryContext { get; }

    public string SharedReliefPoolDescription { get; }

    public bool PlunderStarted { get; }

    public bool MassacreStarted { get; }
}
