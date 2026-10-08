using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed partial class OrchestrationHost : IWorldDiplomacySubjectReleaseHost
    {
        public Dictionary<string, string> CapturePlayerSubjectReleaseTokens(string suzerainId) =>
            Campaign.Current?.GetCampaignBehavior<VassalageBehavior>()?.CapturePlayerSubjectReleaseTokens(ResolveKingdom(suzerainId));
        public string PlayerSubjectReleaseToken(string suzerainId, string subjectId) =>
            Campaign.Current?.GetCampaignBehavior<VassalageBehavior>()?.GetPlayerSubjectReleaseToken(ResolveKingdom(suzerainId), ResolveKingdom(subjectId)) ?? "";
        public WorldDiplomacyImmediateActionReceipt ReleasePlayerSubject(string suzerainId, string subjectId, string token)
        {
            if (!TWParallel.IsMainThread()) return new(false, "释放未执行：必须在游戏主线程处理。");
            var owner = Campaign.Current?.GetCampaignBehavior<VassalageBehavior>();
            if (owner == null) return new(false, "释放未执行：臣属系统不可用。");
            bool applied = owner.TryReleasePlayerSubject(ResolveKingdom(suzerainId), ResolveKingdom(subjectId), token, out string message);
            return new(applied, message);
        }
    }

    internal static string BuildPlayerSubjectReleaseContext(Hero npc) => Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
        .BuildPlayerSubjectReleaseContext(npc?.StringId, npc?.Clan?.Kingdom?.StringId) ?? "";
    internal static string SubmitPlayerSubjectRelease(Hero npc, string payload) => Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
        .SubmitPlayerSubjectRelease(npc?.StringId, npc?.Clan?.Kingdom?.StringId, payload, DiplomacyDialogueSourceScope.Current)
        ?? "释放未执行：世界外交系统不可用。";
}
