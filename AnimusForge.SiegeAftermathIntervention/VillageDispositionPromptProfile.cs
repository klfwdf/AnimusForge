using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Village-only wording. Main speech prompts and postprocess protocols stay separate.</summary>
public static class VillageDispositionPromptProfile
{
    private static readonly Regex InternalTag = new Regex(@"\[[A-Z][A-Z_]*:[^\]\r\n]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

    public static string BuildScenePrompt(string villageName, bool mercyApplied, bool english = false)
        => BuildScenePrompt(villageName, mercyApplied, true, false, TownPromptCatalogResources.Get(english));

    public static string BuildScenePrompt(string villageName, bool mercyApplied, bool hostile, bool canGovern, TownPromptTextCatalog text)
    {
        text ??= TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.GetRuntimeText("VillageScene_Common"), ("village", CleanValue(villageName, 80)))
            + "\n" + text.GetRuntimeText(hostile ? "VillageScene_Foreign" : canGovern ? "VillageScene_FriendlyGovernor" : "VillageScene_FriendlyVisitor")
            + "\n" + text.GetRuntimeText("VillageScene_Reaction") + "\n" + text.GetRuntimeText("VillageScene_Facts")
            + "\n" + text.GetRuntimeText("VillageScene_Actions")
            + "\n" + text.GetRuntimeText(mercyApplied ? "VillageScene_MercyApplied" : "VillageScene_MercyPending");
    }

    public static string BuildRoleContext(VillageDispositionSpeakerRole role, bool english = false)
        => BuildRoleContext(role, TownPromptCatalogResources.Get(english));

    public static string BuildRoleContext(VillageDispositionSpeakerRole role, TownPromptTextCatalog text)
    {
        if (!System.Enum.IsDefined(typeof(VillageDispositionSpeakerRole), role)) role = VillageDispositionSpeakerRole.Unknown;
        string context = text.GetRuntimeText("VillageScene_Role" + role);
        // Reuse the town's command/voice policy; only village authority is specialized.
        if (role == VillageDispositionSpeakerRole.PlayerTroop
            && text.RoleInstructions?.TryGetValue(TownDialogueRole.OrdinarySoldier.ToString(), out string soldier) == true)
            context += "\n" + soldier;
        return context;
    }

    internal static string BuildSpeakerPrompt(VillageDispositionSpeakerProfile profile, VillageDispositionSpeakerRole role, TownPromptTextCatalog text)
        => BuildRoleContext(role, text) + "\n" + TownPromptTextCatalog.FormatTemplate(text.GetRuntimeText("VillageScene_Persona"),
            ("name", profile.Name), ("personality", profile.Personality), ("background", profile.Background),
            ("rhythm", profile.Rhythm), ("concern", profile.Concern));

    public static string BuildPostprocessContext(string villageName, VillageDispositionSpeakerRole role, bool direct, bool mercyApplied, bool english = false)
        => BuildPostprocessContext(villageName, role, direct, mercyApplied, true, false, TownPromptCatalogResources.Get(english));

    public static string BuildPostprocessContext(string villageName, VillageDispositionSpeakerRole role, bool direct, bool mercyApplied,
        bool hostile, bool canGovern, TownPromptTextCatalog text)
        => TownPromptTextCatalog.FormatTemplate(text.GetRuntimeText("VillageScene_Postprocess"),
            ("village", CleanValue(villageName, 80)), ("mode", hostile ? "ForeignDisposition" : canGovern ? "FriendlyGovernor" : "FriendlyVisitor"),
            ("role", role.ToString()), ("direct", direct.ToString()), ("mercy", mercyApplied.ToString()), ("govern", (!hostile && canGovern).ToString()))
            + "\n" + text.GetRuntimeText("VillageScene_Role" + (System.Enum.IsDefined(typeof(VillageDispositionSpeakerRole), role) ? role : VillageDispositionSpeakerRole.Unknown));

    public const string MercyPostprocessDescription = "玩家明确宽恕且尚未执行；不重复奖励，不回滚损失。";

    // Shared authorization/settlement conditions live once in the postprocess context.
    public static string BuildActionDescription(VillageDispositionActionSpec spec, bool english = false)
    {
        if (spec.Kind == VillageDispositionActionKind.Mercy)
            return english ? "Explicit mercy, not already applied; no reward or reversal of losses." : MercyPostprocessDescription;
        if (spec.Kind == VillageDispositionActionKind.VillagerApproval)
            return english ? "Villager/notable genuinely persuaded this turn. Not threats, bargaining, politeness or same-turn aid refund."
                : "本轮真诚游说使村民／要人明确认可；非威胁、讨价还价、客套或同轮援助返还。";
        if (spec.OutcomeLevel > 0)
            return english ? "Personally volunteer: " + spec.Kind + "; spends one existing cooperation."
                : "本人自愿" + spec.Name + "；消耗已有一份合作意愿。";
        if (spec.Kind == VillageDispositionActionKind.GatherVillagers)
            return english ? "Call adult villagers closer; not recruitment or detention." : "召集成年村民走近；不是征兵或扣押。";
        if (spec.Kind == VillageDispositionActionKind.StopPlunder)
            return english ? "Stop ongoing plunder; no extra penalty or refund." : "叫停掠夺；不另扣关系、不退还已有收获。";
        return english ? "Accept the player's order: " + spec.Kind + "." : "接受玩家本轮命令：" + spec.Name + "。";
    }

    internal static string CleanValue(string value, int maxLength)
    {
        string clean = Whitespace.Replace(InternalTag.Replace(value ?? string.Empty, string.Empty), " ").Trim();
        return clean.Length > maxLength ? clean.Substring(0, maxLength) : clean;
    }
}
