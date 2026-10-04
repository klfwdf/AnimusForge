using AnimusForge.DiplomacyDialogue;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal static string SubmitLegacyOralCommitment(Hero npc, string action, string payload) =>
        Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration.SubmitLegacyOralCommitment(
            npc?.StringId, npc?.Clan?.Kingdom?.StringId, action, payload, DiplomacyDialogueSourceScope.Current) ?? "";
    public static string SubmitOralDiplomaticCommitment(Hero npc, string payload)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
        return owner?._orchestration.SubmitOralDiplomaticCommitment(npc?.StringId, npc?.Clan?.Kingdom?.StringId,
            payload, DiplomacyDialogueSourceScope.Current) ?? "外交约定暂未提交：世界外交系统未启用。";
    }
    public static string ControlOralDiplomaticCommitment(Hero npc, string payload)
    {
        return Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
            .ControlOralDiplomaticCommitment(npc?.StringId, npc?.Clan?.Kingdom?.StringId, payload) ?? "";
    }
    public static string BuildOralArrangementContext(Hero npc)
    {
        return Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
            .BuildOralArrangementContext(npc?.StringId, npc?.Clan?.Kingdom?.StringId) ?? "";
    }
}
