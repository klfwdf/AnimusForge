namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free role classification for individual agents in castle GCCZ prompts.
/// Runtime adapters provide live facts; this profile only decides wording and role boundaries.
/// </summary>
public static class SiegeCastleAgentRoleProfile
{
    public const string RoleBlockMarker = "【城堡当前对象身份】";

    public static SiegeCastleAgentRoleKind ResolveRole(bool alliedSoldier, bool hero, bool soldierOrGuard, bool playerPartyOrSelectedTroop)
    {
        return ResolveRole(alliedSoldier, hero, soldierOrGuard, playerPartyOrSelectedTroop, selectedPrisoner: false);
    }

    public static SiegeCastleAgentRoleKind ResolveRole(bool alliedSoldier, bool hero, bool soldierOrGuard, bool playerPartyOrSelectedTroop, bool selectedPrisoner)
    {
        if (alliedSoldier)
        {
            return playerPartyOrSelectedTroop ? SiegeCastleAgentRoleKind.PrisonerGuard : SiegeCastleAgentRoleKind.AlliedSoldierRepresentative;
        }

        if (selectedPrisoner)
        {
            return hero ? SiegeCastleAgentRoleKind.CaptiveLordOrCommander : SiegeCastleAgentRoleKind.CaptivePrisoner;
        }

        if (hero)
        {
            return SiegeCastleAgentRoleKind.CaptiveLordOrCommander;
        }

        if (soldierOrGuard)
        {
            return SiegeCastleAgentRoleKind.CaptivePrisoner;
        }

        return SiegeCastleAgentRoleKind.CastleStaffOrWitness;
    }

    public static string BuildRoleContext(SiegeCastleAgentRoleKind role, string agentName)
    {
        return BuildRoleContext(role, agentName, personaSeed: 0);
    }

    public static string BuildRoleContext(SiegeCastleAgentRoleKind role, string agentName, int personaSeed)
    {
        string safeName = string.IsNullOrWhiteSpace(agentName) ? "当前对象" : agentName.Trim();
        return RoleBlockMarker + safeName + "：" + GetRoleLabel(role) + "。" + GetInstruction(role, personaSeed);
    }

    public static string GetRoleLabel(SiegeCastleAgentRoleKind role)
    {
        switch (role)
        {
            case SiegeCastleAgentRoleKind.CaptiveLordOrCommander:
                return "被俘领主/守将";
            case SiegeCastleAgentRoleKind.CaptivePrisoner:
                return "战败士兵俘虏";
            case SiegeCastleAgentRoleKind.AlliedSoldierRepresentative:
                return "玩家士兵代表";
            case SiegeCastleAgentRoleKind.PrisonerGuard:
                return "看押士兵/胜利方随军";
            case SiegeCastleAgentRoleKind.CastleStaffOrWitness:
                return "城堡军务旁观者/后勤人员";
            default:
                return "城堡军务对象";
        }
    }

    public static string GetInstruction(SiegeCastleAgentRoleKind role)
    {
        return GetInstruction(role, personaSeed: 0);
    }

    public static string GetInstruction(SiegeCastleAgentRoleKind role, int personaSeed)
    {
        switch (role)
        {
            case SiegeCastleAgentRoleKind.CaptiveLordOrCommander:
                return "应承认玩家是胜利方首领。可谈赎金、处决、收编领主或对守军下令；收编领主不能轻易答应，必须在战败被俘场景中明确接受进入收编/引荐流程才成立；若自己是家族族长，需按玩家是否为统治者/是否有国家决定投效路径；若不是族长，默认是为玩家写信引见家族族长，除非正文明确背叛家族成为玩家同伴，否则不能直接收编成功，也不能代表普通守军自动同意全体收编。";
            case SiegeCastleAgentRoleKind.CaptivePrisoner:
                return BuildCaptivePrisonerInstruction(personaSeed);
            case SiegeCastleAgentRoleKind.AlliedSoldierRepresentative:
                return "应称玩家为统帅/大人/长官，可请求军令、看押、战利品、复仇、收编安排或军械接收。";
            case SiegeCastleAgentRoleKind.PrisonerGuard:
                return "应称玩家为统帅/大人/长官，职责是押送俘虏、维持队列、解除武装、防止战败士兵俘虏暴动。";
            case SiegeCastleAgentRoleKind.CastleStaffOrWitness:
                return "应以城堡战后军务现场的后勤或旁观身份回应，不能把玩家称为陌生人，也不要套用城镇平民血洗/信任逻辑。";
            default:
                return "必须服从城堡战后军务规则，不要套用城镇平民逻辑。";
        }
    }

    public static SiegeCastlePrisonerPersonaKind ResolvePrisonerPersona(int personaSeed)
    {
        return (personaSeed & 1) == 0
            ? SiegeCastlePrisonerPersonaKind.CapturedGarrison
            : SiegeCastlePrisonerPersonaKind.FieldCapturedSoldier;
    }

    private static string BuildCaptivePrisonerInstruction(int personaSeed)
    {
        SiegeCastlePrisonerPersonaKind persona = ResolvePrisonerPersona(personaSeed);
        string personaInstruction = persona == SiegeCastlePrisonerPersonaKind.CapturedGarrison
            ? "人格预设=被俘守军：你原本参与防守这座城堡，战败后已被解除武装并押入处置现场。"
            : "人格预设=野战被俘士兵：你是玩家此前部队里押送来的战俘，可能是在野战或其他战斗中被俘，现在被带入城堡一并处置。";
        return personaInstruction + "必须承认自己是战败士兵俘虏，已被胜利方看押、缴械或控制；武器已被收缴但盔甲仍保留；可能面临赎卖、劳役、收编、屠戮或处决；不得把自己说成胜利方士兵、自由战士或普通城镇平民。";
    }
}

public enum SiegeCastleAgentRoleKind
{
    Unknown = 0,
    CaptiveLordOrCommander,
    CaptivePrisoner,
    AlliedSoldierRepresentative,
    PrisonerGuard,
    CastleStaffOrWitness,
}

public enum SiegeCastlePrisonerPersonaKind
{
    CapturedGarrison = 0,
    FieldCapturedSoldier = 1,
}
