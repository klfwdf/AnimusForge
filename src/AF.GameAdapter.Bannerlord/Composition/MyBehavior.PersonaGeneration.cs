using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter _npcPersonaGenerationApplication;
    private AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter NpcPersonaGenerationApplication => _npcPersonaGenerationApplication ??= new AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter(
        _personaProfiles, _npcPersonaGeneration, RunMemorySummaryCompletionAsync,
        () => ReferenceEquals(Instance, this), MemoryEntityIdentityBannerlordAdapter.FindHeroById, StampNpcPersonaProfile);
    private AnimusForge.Refactor.Adapters.PromotedPersonaGenerationApplicationAdapter _promotedPersonaGenerationApplication;
    private AnimusForge.Refactor.Adapters.PromotedPersonaGenerationApplicationAdapter PromotedPersonaGenerationApplication => _promotedPersonaGenerationApplication ??= new AnimusForge.Refactor.Adapters.PromotedPersonaGenerationApplicationAdapter(
        NpcPersonaGenerationApplication, _npcPersonaGeneration, RunMemorySummaryCompletionAsync,
        MemoryEntityIdentityBannerlordAdapter.FindHeroById,
        CampaignCharacterRecordCaptureAdapter.BuildPromotedHeroSkillSummary, CampaignCharacterRecordCaptureAdapter.TryApplyPromotedHeroSkillJson);
    private readonly NpcPersonaGenerationOwner _npcPersonaGeneration = new NpcPersonaGenerationOwner();

    // The request carries detached retrieval inputs and copied text, never a Hero/profile object.
    internal sealed class NpcPersonaGenerationWork
    {
        internal NpcPersonaGenerationOwner.Lease Reservation;
        internal string Id, OriginalPersonality, OriginalBackground, Failure;
        internal string Facts, Requirements;
        internal MentionedWorldEntities Mentions;
        internal PromptLoreSettings LoreSettings;
        internal long LoreRuleVersion;
        internal Task<ApiCallResult> Response;
    }

    private Task EnsureNpcPersonaGeneratedAsync(Hero hero, bool ignoreRetryCooldown = false)
    {
        return NpcPersonaGenerationApplication.EnsureNpcPersonaGeneratedAsync(hero, ignoreRetryCooldown);
    }

    public static async Task EnsureNpcPersonaGeneratedForExternalAsync(Hero hero, bool ignoreRetryCooldown = false)
    {
        // Instance is an identity read; Campaign/target validation occurs in the owner dispatcher.
        MyBehavior owner = Instance;
        if (owner == null || hero == null) return;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        try { await owner.EnsureNpcPersonaGeneratedAsync(hero, ignoreRetryCooldown).ConfigureAwait(false); }
        catch (Exception error)
        {
            Logger.Log("NpcPersona", "[ERROR] External persona generation failed: " + error.Message);
            await owner.RunMemorySummaryCompletionAsync(generation, () =>
            {
                LlmRetryPrompt.ShowFailurePopup("NPC 个性与背景生成失败", LlmRetryPrompt.BuildFailureDetail(error.Message, ""));
                return true;
            }).ConfigureAwait(false);
        }
    }

    private NpcPersonaGenerationWork CaptureNpcPersonaGeneration(Hero hero, bool ignoreRetryCooldown, bool overwriteExisting)
    {
        return NpcPersonaGenerationApplication.CaptureNpcPersonaGeneration(hero, ignoreRetryCooldown, overwriteExisting);
    }

    private Task<string> GenerateNpcPersonaAsync(Hero hero, bool ignoreRetryCooldown, bool overwriteExisting)
    {
        return NpcPersonaGenerationApplication.GenerateNpcPersonaAsync(hero, ignoreRetryCooldown, overwriteExisting);
    }
}
