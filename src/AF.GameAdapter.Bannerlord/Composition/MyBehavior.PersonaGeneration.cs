using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter _npcPersonaGenerationApplication;
    private AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter NpcPersonaGenerationApplication => _npcPersonaGenerationApplication ??= new AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter(
        _personaProfiles, _npcPersonaGeneration, RunMemorySummaryCompletionAsync,
        () => ReferenceEquals(Instance, this), MemoryEntityIdentityBannerlordAdapter.FindHeroById, StampNpcPersonaProfile,
        CaptureRecentPersonaExperiences);
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
        internal string Id, OriginalDescription, Failure;
        internal List<NpcPersonaTrait> OriginalTraits;
        internal List<string> OriginalMotivations, OriginalValues, OriginalFears;
        internal string OriginalSpeechStyle;
        internal string Facts, Requirements, RecentExperiences;
        internal MentionedWorldEntities Mentions;
        internal PromptLoreSettings LoreSettings;
        internal long LoreRuleVersion;
        internal Task<ApiCallResult> Response;
    }

    private string CaptureRecentPersonaExperiences(Hero hero)
    {
        // Called only for a manual request, inside the owner dispatcher. All lookups are target-keyed.
        string id = CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero);
        string actions = "";
        string actionId = CampaignCharacterRecordCaptureAdapter.GetNpcActionHeroKey(hero);
        if (_memoryBusinessState.RecentActions.TryGetValue(actionId, out var entries))
        {
            var recent = NpcActionLedger.SanitizeNpcActionEntries(entries, true, GetCurrentGameDayIndexSafe());
            var tail = recent.Skip(Math.Max(0, recent.Count - PersonaRegenerationExperienceContext.ActionLimit)).ToList();
            actions = CampaignCharacterRecordCaptureAdapter.RenderNpcActionEntriesForPrompt(hero, tail);
        }
        _memoryHistoryCommit.TryGetLatestCompressedMemoryInternal(hero, out _, out string date, out string summary, out _);
        var history = _memoryHistoryCommit.GetDialogueHistoryEntriesById(id, PersonaRegenerationExperienceContext.HistoryLimit);
        return PersonaRegenerationExperienceContext.Build(id, actions, date, summary, history);
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
                LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.NpcTitle, LlmRetryPrompt.BuildFailureDetail(error.Message, ""));
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
