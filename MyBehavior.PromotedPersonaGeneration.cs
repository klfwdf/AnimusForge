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
			string sys = "你是《骑马与砍杀2：霸主》NPC的人设生成器。你只输出严格 JSON，不要输出任何额外文字。JSON 仅包含两个字段：personality 和 background。没有额外要求时，personality 和 background 各约 300 个中文字符；如果玩家自定义生成要求指定了篇幅、详略或文风，则以玩家自定义生成要求为准。每个字段都必须以完整句子结束，不要在半句话处停止。内容必须符合提供的事实，不要杜撰与事实冲突的家族关系、身份或势力。";
			sys = AppendNpcPersonaGenerationRequirementsToSystemPrompt(sys);
			StringBuilder userSb = new StringBuilder();
			userSb.AppendLine("请基于信息生成该 NPC 升格为玩家家族成员后的【个性】与【历史背景】。");
			userSb.AppendLine("写作风格沿用首次见到 Hero NPC 的人设格式：具体、可用于后续对话，不要写成系统说明。");
			userSb.AppendLine("背景必须解释他/她为何愿意追随玩家，并吸收加入前对话中的关系、承诺、冲突、交易或共同经历；如果历史里没有相关内容，明确写成谨慎而合理的动机，不要凭空创造重大事件。");
			userSb.AppendLine("个人名: " + name);
			userSb.AppendLine("原完整称呼: " + fullName);
			userSb.AppendLine("原兵种/职业: " + troopName + (string.IsNullOrWhiteSpace(troopId) ? "" : (" (StringId=" + troopId + ")")));
			userSb.AppendLine("文化: " + culture);
			userSb.AppendLine("当前场景: " + scene);
			userSb.AppendLine("加入事件: " + joinFact);
			userSb.AppendLine("升格后人物事实（只使用原非 Hero NPC/士兵事实、加入事件和对话历史；不要把玩家家族身份写成原生出身）:");
			userSb.AppendLine(BuildPromotedNonHeroCompanionFactsForPersonaGeneration(hero, name, fullName, troopName, troopId, culture, scene, joinFact, equipment));
			userSb.AppendLine("加入前该 NPC 与玩家的全部可用对话历史:");
			userSb.AppendLine(history);
                work.Response = CallAuxiliaryGatewayDetailed(sys, userSb.ToString().Trim(), "PromotedCompanionPersona", 0, forceThinkingDisabled: false);
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
				prof.Personality = TrimToMaxChars(name + "曾是" + troopName + "，保留着原本职业养成的警觉、纪律和生存本能；面对玩家时，他会把加入前的承诺与利害放在心里，既愿意服从队伍安排，也会在危险或失信时变得谨慎。", 420);
			}
			if (string.IsNullOrWhiteSpace(prof.Background))
			{
				prof.Background = TrimToMaxChars(joinFact + "他原本以“" + fullName + "”的身份在" + scene + "活动，加入后只以个人名“" + name + "”示人。此前对话中可用的经历会成为他追随玩家的理由：" + TrimToMaxChars(history, 240), 520);
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
			string sys = "你是《骑马与砍杀2：霸主》的家族成员技能生成器。只输出严格 JSON，格式为 {\"skills\":{\"OneHanded\":数值,...}}。只使用给出的技能ID，数值为 0 到 330 的整数。";
			StringBuilder userSb = new StringBuilder();
			userSb.AppendLine("按原兵种、装备、文化与人设，为这个刚升格的玩家家族 Hero 生成合理技能。不要过强；主要强化实际武器、骑术/跑动和少量战术/领导。");
			userSb.AppendLine("可用技能ID: OneHanded, TwoHanded, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Crafting, Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering");
			userSb.AppendLine("个人名: " + personalName);
			userSb.AppendLine("原兵种: " + originalTroopName);
			userSb.AppendLine("文化: " + cultureName);
			userSb.AppendLine("装备: " + equipmentSummary);
			userSb.AppendLine("当前基础技能(来自原兵种模板，生成失败时保留这些值): " + skillSource);
			userSb.AppendLine("人设摘要: " + TrimToMaxChars((personality + " " + background).Trim(), 700));
                request = CallAuxiliaryGatewayDetailed(sys, userSb.ToString().Trim(), "PromotedCompanionSkills", 0, forceThinkingDisabled: false);
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
