using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private readonly NpcPersonaGenerationOwner _npcPersonaGeneration = new NpcPersonaGenerationOwner();

    // The request carries copied text and an asynchronous transport, never a Hero/profile object.
    private sealed class NpcPersonaGenerationWork
    {
        internal NpcPersonaGenerationOwner.Lease Reservation;
        internal string Id, OriginalPersonality, OriginalBackground, Failure;
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
			string sys = "你是《骑马与砍杀2：霸主》NPC的人设生成器。你只输出严格 JSON，不要输出任何额外文字。JSON 仅包含两个字段：personality 和 background。没有额外要求时，personality 和 background 各约 300 个中文字符；如果玩家自定义生成要求指定了篇幅、详略或文风，则以玩家自定义生成要求为准。每个字段都必须以完整句子结束，不要在半句话处停止。内容必须符合提供的事实，不要杜撰与事实冲突的家族关系或身份；若事实中提供了势力/效忠信息，必须保持一致，禁止声称效忠于其他统治者或属于其他势力。";
			sys = AppendNpcPersonaGenerationRequirementsToSystemPrompt(sys);
			string facts = BuildHeroFactsForPersonaGeneration(hero);
			string user = "请基于以下信息生成该 NPC 的【个性】与【历史背景】。必须综合“人物百科背景”“家族背景”“所在家族百科背景”“王国百科背景”“家族族长背景”；这些素材是事实来源，不要复制成百科原文。\n" + facts;
			if (overwriteExisting)
			{
				string oldPersonality = NormalizePersonaPromptSourceText(personality, 500);
				string oldBackground = NormalizePersonaPromptSourceText(background, 500);
				user += "\n这是重新生成人设请求：请生成一版不同但仍符合事实的人设，不要照搬旧文本。"
					+ "\n旧个性（仅用于避重）：" + (string.IsNullOrWhiteSpace(oldPersonality) ? "无" : oldPersonality)
					+ "\n旧背景（仅用于避重）：" + (string.IsNullOrWhiteSpace(oldBackground) ? "无" : oldBackground);
			}
            // This async method snapshots provider settings before its first await. It starts an
            // asynchronous HTTP operation, never a blocking wait or a Task.Run that reads live settings.
            return new NpcPersonaGenerationWork
            {
                Id = id, OriginalPersonality = personality, OriginalBackground = background, Reservation = lease,
                Response = CallAuxiliaryGatewayDetailed(sys, user, "NpcPersona", 0, forceThinkingDisabled: false)
            };
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
