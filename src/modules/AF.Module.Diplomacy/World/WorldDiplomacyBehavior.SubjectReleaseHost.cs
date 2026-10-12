using AnimusForge;
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
            if (!TWParallel.IsMainThread()) return new(false, DiplomacyMiscPromptTexts.Get("SubjectRelease_MustBeMainThread", "Release not executed: must be processed on game main thread."));
            var owner = Campaign.Current?.GetCampaignBehavior<VassalageBehavior>();
            if (owner == null) return new(false, DiplomacyMiscPromptTexts.Get("OralTag_VassalageUnavailable", "Release not executed: vassalage system is unavailable."));
            bool applied = owner.TryReleasePlayerSubject(ResolveKingdom(suzerainId), ResolveKingdom(subjectId), token, out string message);
            return new(applied, message);
        }
    }

    internal static string BuildPlayerSubjectReleaseContext(Hero npc) => Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
        .BuildPlayerSubjectReleaseContext(npc?.StringId, npc?.Clan?.Kingdom?.StringId) ?? "";
    internal static string SubmitPlayerSubjectRelease(Hero npc, string payload) => Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
        .SubmitPlayerSubjectRelease(npc?.StringId, npc?.Clan?.Kingdom?.StringId, payload, DiplomacyDialogueSourceScope.Current)
        ?? DiplomacyMiscPromptTexts.Get("SubjectRelease_WorldDiplomacyUnavailable", "Release not executed: world diplomacy system is unavailable.");
}
