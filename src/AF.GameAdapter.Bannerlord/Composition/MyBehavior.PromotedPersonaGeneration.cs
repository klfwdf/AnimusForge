using System;
using System.Text;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    // Main-thread adapter for recruitment's existing two-stage generation.
    // Reuses Persona's sole reservation owner and the existing budgeted dispatcher.
	public static async Task GeneratePromotedNonHeroCompanionProfileForExternalAsync(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string dialogueHistory, string equipmentSummary)
    {
        MyBehavior owner = Instance;
        if (owner == null || hero == null) return;
        await owner.GeneratePromotedNonHeroCompanionProfileAsync(hero, personalName, originalFullName, originalTroopName,
            originalTroopId, cultureName, sceneLabel, joinEventFact, dialogueHistory, equipmentSummary).ConfigureAwait(false);
    }

	private async Task GeneratePromotedNonHeroCompanionProfileAsync(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string dialogueHistory, string equipmentSummary)
    {
        long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
        NpcPersonaGenerationWork work = null;
        string heroId = null, name = null, fullName = null, troopName = null, troopId = null,
            culture = null, scene = null, joinFact = null, history = null, equipment = null;
        bool personaSaved = false;
        try
        {
            bool captured = await RunMemorySummaryCompletionAsync(runtimeGeneration, () =>
            {
                if (hero == null) return false;
                heroId = (hero.StringId ?? "").Trim();
                if (string.IsNullOrWhiteSpace(heroId) || !ReferenceEquals(FindHeroById(heroId), hero)) return false;
                var lease = _npcPersonaGeneration.TryBegin(heroId, true, out _);
                if (lease == null) return false;
                work = new NpcPersonaGenerationWork { Id = heroId, Reservation = lease };
                GetNpcPersonaStrings(hero, out string originalPersonality, out string originalBackground);
                work.OriginalPersonality = originalPersonality;
                work.OriginalBackground = originalBackground;
		name = string.IsNullOrWhiteSpace(personalName) ? (hero.Name?.ToString() ?? "新家族成员") : personalName.Trim();
		fullName = string.IsNullOrWhiteSpace(originalFullName) ? name : originalFullName.Trim();
		troopName = string.IsNullOrWhiteSpace(originalTroopName) ? "非英雄NPC" : originalTroopName.Trim();
		troopId = (originalTroopId ?? "").Trim();
		culture = string.IsNullOrWhiteSpace(cultureName) ? "未知文化" : cultureName.Trim();
		scene = string.IsNullOrWhiteSpace(sceneLabel) ? "当前场景" : sceneLabel.Trim();
		joinFact = string.IsNullOrWhiteSpace(joinEventFact) ? (name + "同意追随玩家，成为玩家家族成员并加入玩家队伍。") : joinEventFact.Trim();
		history = string.IsNullOrWhiteSpace(dialogueHistory) ? "（无可用加入前对话历史）" : dialogueHistory.Trim();
		equipment = string.IsNullOrWhiteSpace(equipmentSummary) ? "（无装备）" : equipmentSummary.Trim();
			NpcPersonaPrompt prompt = NpcPersonaTextRules.BuildPromoted(name,fullName,troopName,troopId,culture,scene,joinFact,history,equipment,BuildPromotedNonHeroCompanionFactsForPersonaGeneration(hero,name,fullName,troopName,troopId,culture,scene,joinFact,equipment),DuelSettings.GetSettings()?.NpcPersonaGenerationRequirements);
                work.Response = CallAuxiliaryGatewayDetailed(prompt.System, prompt.User, "PromotedCompanionPersona", 0, forceThinkingDisabled: false);
                return true;
            }).ConfigureAwait(false);
            if (!captured || work?.Response == null) return;
            ApiCallResult apiCallResult = await AwaitPromotedCompanionResponseAsync(work.Response).ConfigureAwait(false);
            Task skillsTask = null;
            bool committed = await RunMemorySummaryCompletionAsync(runtimeGeneration, () =>
            {
                if (!_npcPersonaGeneration.IsCurrent(work.Reservation) || !ReferenceEquals(FindHeroById(work.Id), hero)) return false;
                GetNpcPersonaStrings(hero, out string currentPersonality, out string currentBackground);
                if (!string.Equals(currentPersonality, work.OriginalPersonality, StringComparison.Ordinal)
                    || !string.Equals(currentBackground, work.OriginalBackground, StringComparison.Ordinal))
                {
                    Logger.Log("NpcPersona", "Promoted companion persona skipped after edit hero=" + work.Id);
                    return false;
                }
                try
                {
			string resp = apiCallResult.Content ?? "";
			if (apiCallResult.Success && !string.IsNullOrWhiteSpace(resp) && TryParsePersonaJson(resp, out var genP, out var genB))
			{
				genP = NormalizeGeneratedPersonaText(genP);
				genB = NormalizeGeneratedPersonaText(genB);
				if (string.IsNullOrWhiteSpace(genP) && !string.IsNullOrWhiteSpace(genB))
				{
					genP = genB;
				}
				if (string.IsNullOrWhiteSpace(genB) && !string.IsNullOrWhiteSpace(genP))
				{
					genB = genP;
				}
				NpcPersonaProfile prof = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
				prof.Personality = genP.Trim();
				prof.Background = genB.Trim();
				SaveNpcPersonaProfile(hero, prof);
				personaSaved = !string.IsNullOrWhiteSpace(prof.Personality) || !string.IsNullOrWhiteSpace(prof.Background);
				Logger.Log("NpcPersona", "Promoted companion persona generated hero=" + heroId + " name=" + name);
			}
			if (!personaSaved)
			{
				string failureDetail = apiCallResult.Success
					? LlmRetryPrompt.BuildFailureDetail("升格同伴人设模型回复解析失败，将使用本地后备人设。", resp, apiCallResult.ResponseBody)
					: (apiCallResult.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("升格同伴人设生成失败，将使用本地后备人设。", resp, apiCallResult.ResponseBody));
				Logger.Log("NpcPersona", "[WARN] Promoted companion persona fallback hero=" + heroId + ": " + failureDetail);
				LlmRetryPrompt.ShowFailurePopup("升格同伴人设生成失败", failureDetail);
			}
                }
                catch (Exception ex)
                {
                    Logger.Log("NpcPersona", "[WARN] Promoted companion persona generation error hero=" + heroId + ": " + ex.Message);
                    LlmRetryPrompt.ShowFailurePopup("升格同伴人设生成失败", LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
                }
		if (!personaSaved)
		{
			NpcPersonaProfile prof = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
			if (string.IsNullOrWhiteSpace(prof.Personality))
			{
				prof.Personality = NpcPersonaTextRules.BuildFallbackPersonality(name,troopName,joinFact,fullName,scene,history);
			}
			if (string.IsNullOrWhiteSpace(prof.Background))
			{
				prof.Background = NpcPersonaTextRules.BuildFallbackBackground(name,troopName,joinFact,fullName,scene,history);
			}
			SaveNpcPersonaProfile(hero, prof);
		}
                skillsTask = GeneratePromotedNonHeroCompanionSkillsAsync(hero, name, troopName, culture, equipment, work.Reservation, runtimeGeneration);
                return true;
            }).ConfigureAwait(false);
            if (committed && skillsTask != null) await skillsTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await RunMemorySummaryCompletionAsync(runtimeGeneration, () =>
            {
                if (work != null && !_npcPersonaGeneration.IsCurrent(work.Reservation)) return false;
                LlmRetryPrompt.ShowFailurePopup("升格同伴人设生成失败", LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
                return true;
            }).ConfigureAwait(false);
        }
        finally
        {
            _npcPersonaGeneration.Complete(work?.Reservation, personaSaved, retryOnFailure: false);
        }
    }

    private static async Task<ApiCallResult> AwaitPromotedCompanionResponseAsync(Task<ApiCallResult> request)
    {
        try { return await request.ConfigureAwait(false); }
        catch (Exception ex) { return new ApiCallResult { Success = false, Content = "", ErrorMessage = ex.Message }; }
    }

	private async Task GeneratePromotedNonHeroCompanionSkillsAsync(Hero hero, string personalName, string originalTroopName, string cultureName, string equipmentSummary, NpcPersonaGenerationOwner.Lease lease, long runtimeGeneration)
    {
        Task<ApiCallResult> request = null;
        string skillSource = null, personality = null, background = null;
        try
        {
            bool captured = await RunMemorySummaryCompletionAsync(runtimeGeneration, () =>
            {
                if (!_npcPersonaGeneration.IsCurrent(lease) || !ReferenceEquals(FindHeroById(lease.Id), hero)) return false;
                skillSource = BuildPromotedHeroSkillSummary(hero);
			GetNpcPersonaStrings(hero, out personality, out background);
			NpcPersonaPrompt skillPrompt = NpcPersonaTextRules.BuildSkills(personalName,originalTroopName,cultureName,equipmentSummary,skillSource,personality,background);
                request = CallAuxiliaryGatewayDetailed(skillPrompt.System, skillPrompt.User, "PromotedCompanionSkills", 0, forceThinkingDisabled: false);
                return true;
            }).ConfigureAwait(false);
            if (!captured || request == null) return;
            ApiCallResult apiCallResult = await AwaitPromotedCompanionResponseAsync(request).ConfigureAwait(false);
            await RunMemorySummaryCompletionAsync(runtimeGeneration, () =>
            {
                if (!_npcPersonaGeneration.IsCurrent(lease) || !ReferenceEquals(FindHeroById(lease.Id), hero)) return false;
                GetNpcPersonaStrings(hero, out string currentPersonality, out string currentBackground);
                if (!string.Equals(skillSource, BuildPromotedHeroSkillSummary(hero), StringComparison.Ordinal)
                    || !string.Equals(personality, currentPersonality, StringComparison.Ordinal)
                    || !string.Equals(background, currentBackground, StringComparison.Ordinal)) return false;
			string resp = apiCallResult.Content ?? "";
			if (!apiCallResult.Success || !TryApplyPromotedHeroSkillJson(hero, resp))
			{
				Logger.Log("NpcPersona", "Promoted companion skill generation fallback hero=" + (hero.StringId ?? "") + ": keeping template skills.");
				string failureDetail = apiCallResult.Success
					? LlmRetryPrompt.BuildFailureDetail("升格同伴技能模型回复解析失败，将保留兵种模板技能。", resp, apiCallResult.ResponseBody)
					: (apiCallResult.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("升格同伴技能生成失败，将保留兵种模板技能。", resp, apiCallResult.ResponseBody));
				LlmRetryPrompt.ShowFailurePopup("升格同伴技能生成失败", failureDetail);
			}
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await RunMemorySummaryCompletionAsync(runtimeGeneration, () =>
            {
                if (!_npcPersonaGeneration.IsCurrent(lease) || !ReferenceEquals(FindHeroById(lease.Id), hero)) return false;
                Logger.Log("NpcPersona", "[WARN] Promoted companion skill generation error hero=" + lease.Id + ": " + ex.Message);
                LlmRetryPrompt.ShowFailurePopup("升格同伴技能生成失败", LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
                return true;
            }).ConfigureAwait(false);
        }
    }
}
