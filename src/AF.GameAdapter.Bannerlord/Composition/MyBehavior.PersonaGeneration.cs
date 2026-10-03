using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private readonly NpcPersonaGenerationOwner _npcPersonaGeneration = new NpcPersonaGenerationOwner();

    // The request carries detached retrieval inputs and copied text, never a Hero/profile object.
    private sealed class NpcPersonaGenerationWork
    {
        internal NpcPersonaGenerationOwner.Lease Reservation;
        internal string Id, OriginalPersonality, OriginalBackground, Failure;
        internal string Facts, Requirements;
        internal MentionedWorldEntities Mentions;
        internal PromptLoreSettings LoreSettings;
        internal long LoreRuleVersion;
        internal Task<ApiCallResult> Response;
    }

    private async Task EnsureNpcPersonaGeneratedAsync(Hero hero, bool ignoreRetryCooldown = false)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        string failure = await GenerateNpcPersonaAsync(hero, ignoreRetryCooldown, overwriteExisting: false).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(failure))
            await RunMemorySummaryCompletionAsync(generation, () =>
            {
                LlmRetryPrompt.ShowFailurePopup("NPC 个性与背景生成失败", failure);
                return true;
            }).ConfigureAwait(false);
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
        if (hero == null) return new NpcPersonaGenerationWork { Failure = overwriteExisting ? "找不到要重新生成人设的 NPC。" : "" };
        string id = hero.StringId;
        if (string.IsNullOrEmpty(id)) return new NpcPersonaGenerationWork { Failure = overwriteExisting ? "该 NPC 没有有效的 HeroId，无法重新生成人设。" : "" };
        GetNpcPersonaStrings(hero, out string personality, out string background);
        if (!overwriteExisting && !string.IsNullOrWhiteSpace(personality) && !string.IsNullOrWhiteSpace(background))
            return new NpcPersonaGenerationWork();
        NpcPersonaGenerationOwner.Lease lease = _npcPersonaGeneration.TryBegin(id, ignoreRetryCooldown, out bool coolingDown);
        if (lease == null) return new NpcPersonaGenerationWork { Failure = !overwriteExisting ? "" : coolingDown
            ? "该 NPC 的上次生成请求刚刚失败，请稍后再试。" : "该 NPC 的个性与背景正在生成，请等待当前请求完成后再试。" };
        try
        {
            var work = new NpcPersonaGenerationWork
            {
                Id = id, OriginalPersonality = personality, OriginalBackground = background, Reservation = lease,
                Facts = BuildHeroFactsForPersonaGeneration(hero), Requirements = DuelSettings.GetSettings()?.NpcPersonaGenerationRequirements
            };
            try
            {
                if (KnowledgeLibraryBehavior.Instance != null)
                {
                    work.LoreSettings = KnowledgeLibraryBehavior.CapturePromptLoreSettings();
                    string name = (hero.Name?.ToString() ?? "").Trim();
                    if (work.LoreSettings?.Enabled == true && !string.IsNullOrWhiteSpace(name))
                    {
                        work.Mentions = new MentionedWorldEntities();
                        work.Mentions.Entities.Add(name);
                        work.LoreRuleVersion = KnowledgeLibraryBehavior.PreparePromptLoreRetrieval(work.Mentions);
                    }
                }
            }
            catch (Exception error) { Logger.Log("NpcPersona", "[WARN] Lore capture skipped: " + error.Message); }
            return work;
        }
        catch { _npcPersonaGeneration.Complete(lease, saved: false); throw; }
    }

    private async Task<string> GenerateNpcPersonaAsync(Hero hero, bool ignoreRetryCooldown, bool overwriteExisting)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        NpcPersonaGenerationWork work = null;
        bool saved = false, retired = false;
        try
        {
            // Reuse this MyBehavior owner's existing budgeted queue and its Campaign/generation checks.
            // The queue retains its historical memory name; persona does not create a second scheduler.
            bool captured = await RunMemorySummaryCompletionAsync(generation, () =>
            {
                work = CaptureNpcPersonaGeneration(hero, ignoreRetryCooldown, overwriteExisting);
                return true;
            }).ConfigureAwait(false);
            if (!captured || work == null) return overwriteExisting ? "请求已失效，未保存新的人设。" : "";
            if (work.Reservation == null) return work.Failure ?? "";
            KnowledgeLibraryBehavior.LoreRule loreRule = null;
            if (work.Mentions != null && work.LoreRuleVersion > 0L)
            {
                try
                {
                    loreRule = await Task.Run(() => KnowledgeLibraryBehavior.CollectPersonaLoreRule(
                        work.Mentions, work.LoreRuleVersion, work.LoreSettings)).ConfigureAwait(false);
                }
                catch (Exception error) { Logger.Log("NpcPersona", "[WARN] Lore retrieval skipped: " + error.Message); }
            }
            bool started = await RunMemorySummaryCompletionAsync(generation, () =>
            {
                if (!_npcPersonaGeneration.IsCurrent(work.Reservation) || !ReferenceEquals(FindHeroById(work.Id), hero))
                {
                    retired = true;
                    work.Failure = overwriteExisting ? "请求已因数据清理或人物变更失效，未生成新的人设。" : "";
                    return true;
                }
                if (overwriteExisting)
                {
                    GetNpcPersonaStrings(hero, out string currentPersonality, out string currentBackground);
                    if (!string.Equals(currentPersonality, work.OriginalPersonality, StringComparison.Ordinal)
                        || !string.Equals(currentBackground, work.OriginalBackground, StringComparison.Ordinal))
                    {
                        retired = true;
                        work.Failure = "生成期间人设已被修改，未覆盖最新的个性与历史背景。请确认后重新生成。";
                        return true;
                    }
                }
                string loreSource = "";
                try { loreSource = KnowledgeLibraryBehavior.Instance?.BuildPersonaLoreSource(hero, loreRule, work.LoreRuleVersion) ?? ""; }
                catch (Exception error) { Logger.Log("NpcPersona", "[WARN] Lore source skipped: " + error.Message); }
                NpcPersonaPrompt prompt = NpcPersonaTextRules.BuildNative(work.Facts + (string.IsNullOrWhiteSpace(loreSource) ? "" : "\n" + loreSource),
                    work.OriginalPersonality, work.OriginalBackground, overwriteExisting, work.Requirements);
                // Capture provider settings on the game thread before the gateway's first await.
                work.Response = CallAuxiliaryGatewayDetailed(prompt.System, prompt.User, "NpcPersona", 0, forceThinkingDisabled: false);
                return true;
            }).ConfigureAwait(false);
            if (!started) { retired = true; return overwriteExisting ? "请求已失效，未保存新的人设。" : ""; }
            if (work.Response == null) return work.Failure ?? "";
            ApiCallResult response = await work.Response.ConfigureAwait(false);
            if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return "";
            string resp = response.Content ?? "";
            string genP = "", genB = "";
            bool parsed = await Task.Run(() => response.Success && !string.IsNullOrWhiteSpace(resp)
                && TryParsePersonaJson(resp, out genP, out genB)).ConfigureAwait(false);
            genP = NormalizeGeneratedPersonaText(genP);
            genB = NormalizeGeneratedPersonaText(genB);
            NpcPersonaProfilePolicy.CompleteGeneratedFields(ref genP, ref genB);
            string failure = null;
            bool accepted = await RunMemorySummaryCompletionAsync(generation, () =>
            {
                if (!_npcPersonaGeneration.IsCurrent(work.Reservation) || !ReferenceEquals(FindHeroById(work.Id), hero))
                { retired = true; failure = overwriteExisting ? "请求已因数据清理或人物变更失效，未保存新的人设。" : ""; return true; }
                if (parsed)
                {
                    GetNpcPersonaStrings(hero, out string curP, out string curB);
                    if (!NpcPersonaProfilePolicy.TryMerge(overwriteExisting, work.OriginalPersonality, work.OriginalBackground,
                        curP, curB, genP, genB, out string nextPersonality, out string nextBackground))
                    {
                        retired = true;
                        failure = "生成期间人设已被修改，未覆盖最新的个性与历史背景。请确认后重新生成。";
                        return true;
                    }
                    NpcPersonaProfile current = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
                    NpcPersonaProfile profile = overwriteExisting ? new NpcPersonaProfile { VoiceId = (current.VoiceId ?? "").Trim() } : current;
                    profile.Personality = nextPersonality;
                    profile.Background = nextBackground;
                    SaveNpcPersonaProfile(hero, profile);
                    saved = !string.IsNullOrWhiteSpace(profile.Personality) || !string.IsNullOrWhiteSpace(profile.Background);
                    if (saved && overwriteExisting) Logger.Log("NpcPersona", "[REROLL] Replaced personality and background for " + work.Id + "; voiceId preserved=" + !string.IsNullOrWhiteSpace(profile.VoiceId) + ".");
                }
                if (!saved)
                {
                    failure = response.Success
                        ? LlmRetryPrompt.BuildFailureDetail("NPC 个性与背景模型回复解析失败，未保存人设。", resp, response.ResponseBody)
                        : (response.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("NPC 个性与背景生成失败。", resp, response.ResponseBody));
                    Logger.Log("NpcPersona", "[WARN] AutoGen did not save profile for " + work.Id + ": " + failure);
                }
                return true;
            }).ConfigureAwait(false);
            return accepted ? failure ?? "" : overwriteExisting ? "请求已失效，未保存新的人设。" : "";
        }
        catch (Exception error)
        {
            if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return "";
            Logger.Log("NpcPersona", "[ERROR] AutoGen failed: " + error.Message);
            return LlmRetryPrompt.BuildFailureDetail(error.Message, "");
        }
        finally
        {
            _npcPersonaGeneration.Complete(work?.Reservation, saved,
                !retired && ReferenceEquals(Instance, this) && SaveRuntimeGuard.IsCurrentGeneration(generation));
        }
    }
}
