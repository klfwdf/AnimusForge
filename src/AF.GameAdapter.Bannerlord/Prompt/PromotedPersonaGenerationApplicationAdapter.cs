using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using ApiCallResult = AnimusForge.MyBehavior.ApiCallResult;
using NpcPersonaProfile = AnimusForge.MyBehavior.NpcPersonaProfile;
using NpcPersonaGenerationWork = AnimusForge.MyBehavior.NpcPersonaGenerationWork;
namespace AnimusForge.Refactor.Adapters;
internal sealed class PromotedPersonaGenerationApplicationAdapter
{
    private readonly NpcPersonaGenerationApplicationAdapter _persona;
    private readonly NpcPersonaGenerationOwner _reservations;
    private readonly Func<long,Func<bool>,Task<bool>> _dispatch;
    private readonly Func<string,Hero> _findHero;
    private readonly Func<Hero,string> _captureSkills;
    private readonly Func<Hero,string,bool> _applySkills;
    internal PromotedPersonaGenerationApplicationAdapter(NpcPersonaGenerationApplicationAdapter persona,NpcPersonaGenerationOwner reservations,
        Func<long,Func<bool>,Task<bool>> dispatch,Func<string,Hero> findHero,Func<Hero,string> captureSkills,Func<Hero,string,bool> applySkills)
    { _persona=persona; _reservations=reservations; _dispatch=dispatch; _findHero=findHero; _captureSkills=captureSkills; _applySkills=applySkills; }
	internal async Task GeneratePromotedNonHeroCompanionProfileAsync(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string dialogueHistory, string equipmentSummary)
    {
        long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
        NpcPersonaGenerationWork work = null;
        string heroId = null, name = null, fullName = null, troopName = null, troopId = null,
            culture = null, scene = null, joinFact = null, history = null, equipment = null;
        bool personaSaved = false;
        try
        {
            bool captured = await _dispatch(runtimeGeneration, () =>
            {
                if (hero == null) return false;
                heroId = (hero.StringId ?? "").Trim();
                if (string.IsNullOrWhiteSpace(heroId) || !ReferenceEquals(_findHero(heroId), hero)) return false;
                var lease = _reservations.TryBegin(heroId, true, out _);
                if (lease == null) return false;
                work = new NpcPersonaGenerationWork { Id = heroId, Reservation = lease };
                work.OriginalDescription = _persona.GetNpcPersonaDescription(hero);
                var original = _persona.GetNpcPersonaProfile(hero, false);
                work.OriginalTraits = NpcPersonaGenerationApplicationAdapter.CloneTraits(original?.Traits);
                work.OriginalMotivations = NpcPersonaGenerationApplicationAdapter.CloneStrings(original?.Motivations);
                work.OriginalValues = NpcPersonaGenerationApplicationAdapter.CloneStrings(original?.Values);
                work.OriginalFears = NpcPersonaGenerationApplicationAdapter.CloneStrings(original?.Fears);
                work.OriginalSpeechStyle = original?.SpeechStyle ?? "";
		name = string.IsNullOrWhiteSpace(personalName) ? (hero.Name?.ToString() ?? AdapterPromptCaptureTexts.PcapText("persona_generation_48")) : personalName.Trim();
		fullName = string.IsNullOrWhiteSpace(originalFullName) ? name : originalFullName.Trim();
		troopName = string.IsNullOrWhiteSpace(originalTroopName) ? AdapterPromptCaptureTexts.PcapText("persona_generation_49") : originalTroopName.Trim();
		troopId = (originalTroopId ?? "").Trim();
		culture = string.IsNullOrWhiteSpace(cultureName) ? AdapterPromptCaptureTexts.PcapText("persona_generation_50") : cultureName.Trim();
		scene = string.IsNullOrWhiteSpace(sceneLabel) ? AdapterPromptCaptureTexts.PcapText("persona_generation_51") : sceneLabel.Trim();
		joinFact = string.IsNullOrWhiteSpace(joinEventFact) ? (name + AdapterPromptCaptureTexts.PcapText("persona_generation_52")) : joinEventFact.Trim();
		history = string.IsNullOrWhiteSpace(dialogueHistory) ? AdapterPromptCaptureTexts.PcapText("promoted_no_prejoin_history") : dialogueHistory.Trim();
		equipment = string.IsNullOrWhiteSpace(equipmentSummary) ? AdapterPromptCaptureTexts.PcapText("persona_generation_53") : equipmentSummary.Trim();
			NpcPersonaPrompt prompt = NpcPersonaTextRules.BuildPromoted(name,fullName,troopName,troopId,culture,scene,joinFact,history,equipment,PersonaGenerationFactCaptureAdapter.BuildPromotedNonHeroCompanionFactsForPersonaGeneration(hero,name,fullName,troopName,troopId,culture,scene,joinFact,equipment),DuelSettings.GetSettings()?.NpcPersonaGenerationRequirements);
                work.Response = ConfiguredChatApplicationAdapter.CallAuxiliaryGatewayDetailed(prompt.System, prompt.User, "PromotedCompanionPersona", 0, forceThinkingDisabled: false);
                return true;
            }).ConfigureAwait(false);
            if (!captured || work?.Response == null) return;
            ApiCallResult apiCallResult = await AwaitPromotedCompanionResponseAsync(work.Response).ConfigureAwait(false);
            Task skillsTask = null;
            bool committed = await _dispatch(runtimeGeneration, () =>
            {
                if (!_reservations.IsCurrent(work.Reservation) || !ReferenceEquals(_findHero(work.Id), hero)) return false;
                if (!string.Equals(_persona.GetNpcPersonaDescription(hero), work.OriginalDescription, StringComparison.Ordinal)
                    || !NpcPersonaGenerationApplicationAdapter.ProfileExtrasStillCurrent(_persona.GetNpcPersonaProfile(hero, false), work))
                {
                    Logger.Log("NpcPersona", "Promoted companion persona skipped after edit hero=" + work.Id);
                    return false;
                }
                try
                {
			string resp = apiCallResult.Content ?? "";
				NpcPersonaGeneratedData generated = null;
				if (apiCallResult.Success && !string.IsNullOrWhiteSpace(resp) && NpcPersonaTextRules.TryParsePersonaJson(resp, out generated))
			{
                    NpcPersonaProfile prof = _persona.GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
                    prof.Description = NpcPersonaProfilePolicy.NormalizeGenerated(generated.Description);
					NpcPersonaGenerationApplicationAdapter.ApplyGeneratedExtras(prof, prof, generated, overwriteExisting: false);
					prof.SchemaVersion = Math.Max(prof.SchemaVersion, 3);
				_persona.SaveNpcPersonaProfile(hero, prof);
                personaSaved = !string.IsNullOrWhiteSpace(prof.Description);
				Logger.Log("NpcPersona", "Promoted companion persona generated hero=" + heroId + " name=" + name);
			}
			if (!personaSaved)
			{
				string failureDetail = apiCallResult.Success
					? LlmRetryPrompt.BuildFailureDetail("Promoted companion persona response parsing failed; a local fallback persona will be used.", resp, apiCallResult.ResponseBody)
					: (apiCallResult.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("Promoted companion persona generation failed; a local fallback persona will be used.", resp, apiCallResult.ResponseBody));
				Logger.Log("NpcPersona", "[WARN] Promoted companion persona fallback hero=" + heroId + ": " + failureDetail);
				LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.PromotedPersonaTitle, PersonaFailureUiText.ProjectFailureDetail(failureDetail));
			}
                }
                catch (Exception ex)
                {
                    Logger.Log("NpcPersona", "[WARN] Promoted companion persona generation error hero=" + heroId + ": " + ex.Message);
                    LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.PromotedPersonaTitle, LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
                }
		if (!personaSaved)
		{
			NpcPersonaProfile prof = _persona.GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
            if (string.IsNullOrWhiteSpace(prof.Description))
                prof.Description = NpcPersonaProfilePolicy.MergeLegacy(
                    NpcPersonaTextRules.BuildFallbackPersonality(name,troopName,joinFact,fullName,scene,history),
                    NpcPersonaTextRules.BuildFallbackBackground(name,troopName,joinFact,fullName,scene,history));
            prof.SchemaVersion = Math.Max(prof.SchemaVersion, 3);
			_persona.SaveNpcPersonaProfile(hero, prof);
		}
                skillsTask = GeneratePromotedNonHeroCompanionSkillsAsync(hero, name, troopName, culture, equipment, work.Reservation, runtimeGeneration);
                return true;
            }).ConfigureAwait(false);
            if (committed && skillsTask != null) await skillsTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _dispatch(runtimeGeneration, () =>
            {
                if (work != null && !_reservations.IsCurrent(work.Reservation)) return false;
                LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.PromotedPersonaTitle, LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
                return true;
            }).ConfigureAwait(false);
        }
        finally
        {
            _reservations.Complete(work?.Reservation, personaSaved, retryOnFailure: false);
        }
    }

    internal static async Task<ApiCallResult> AwaitPromotedCompanionResponseAsync(Task<ApiCallResult> request)
    {
        try { return await request.ConfigureAwait(false); }
        catch (Exception ex) { return new ApiCallResult { Success = false, Content = "", ErrorMessage = ex.Message }; }
    }

	internal async Task GeneratePromotedNonHeroCompanionSkillsAsync(Hero hero, string personalName, string originalTroopName, string cultureName, string equipmentSummary, NpcPersonaGenerationOwner.Lease lease, long runtimeGeneration)
    {
        Task<ApiCallResult> request = null;
        string skillSource = null, description = null;
        try
        {
            bool captured = await _dispatch(runtimeGeneration, () =>
            {
                if (!_reservations.IsCurrent(lease) || !ReferenceEquals(_findHero(lease.Id), hero)) return false;
                skillSource = _captureSkills(hero);
			description = _persona.GetNpcPersonaDescription(hero);
			NpcPersonaPrompt skillPrompt = NpcPersonaTextRules.BuildSkills(personalName,originalTroopName,cultureName,equipmentSummary,skillSource,description);
                request = ConfiguredChatApplicationAdapter.CallAuxiliaryGatewayDetailed(skillPrompt.System, skillPrompt.User, "PromotedCompanionSkills", 0, forceThinkingDisabled: false);
                return true;
            }).ConfigureAwait(false);
            if (!captured || request == null) return;
            ApiCallResult apiCallResult = await AwaitPromotedCompanionResponseAsync(request).ConfigureAwait(false);
            await _dispatch(runtimeGeneration, () =>
            {
                if (!_reservations.IsCurrent(lease) || !ReferenceEquals(_findHero(lease.Id), hero)) return false;
                string currentDescription = _persona.GetNpcPersonaDescription(hero);
                if (!string.Equals(skillSource, _captureSkills(hero), StringComparison.Ordinal)
                    || !string.Equals(description, currentDescription, StringComparison.Ordinal)) return false;
			string resp = apiCallResult.Content ?? "";
			if (!apiCallResult.Success || !_applySkills(hero, resp))
			{
				Logger.Log("NpcPersona", "Promoted companion skill generation fallback hero=" + (hero.StringId ?? "") + ": keeping template skills.");
				string failureDetail = apiCallResult.Success
					? LlmRetryPrompt.BuildFailureDetail("Promoted companion skill response parsing failed; troop template skills will be kept.", resp, apiCallResult.ResponseBody)
					: (apiCallResult.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("Promoted companion skill generation failed; troop template skills will be kept.", resp, apiCallResult.ResponseBody));
				LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.PromotedSkillsTitle, PersonaFailureUiText.ProjectFailureDetail(failureDetail));
			}
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _dispatch(runtimeGeneration, () =>
            {
                if (!_reservations.IsCurrent(lease) || !ReferenceEquals(_findHero(lease.Id), hero)) return false;
                Logger.Log("NpcPersona", "[WARN] Promoted companion skill generation error hero=" + lease.Id + ": " + ex.Message);
                LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.PromotedSkillsTitle, LlmRetryPrompt.BuildFailureDetail(ex.Message, ""));
                return true;
            }).ConfigureAwait(false);
        }
    }
}
