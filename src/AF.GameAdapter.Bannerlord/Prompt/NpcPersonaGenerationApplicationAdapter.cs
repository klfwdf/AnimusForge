using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using System;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using ApiCallResult = AnimusForge.MyBehavior.ApiCallResult;
using NpcPersonaProfile = AnimusForge.MyBehavior.NpcPersonaProfile;
using NpcPersonaGenerationWork = AnimusForge.MyBehavior.NpcPersonaGenerationWork;
namespace AnimusForge.Refactor.Adapters;
internal sealed class NpcPersonaGenerationApplicationAdapter
{
    private readonly PersonaProfileStateOwner _profiles;
    private readonly NpcPersonaGenerationOwner _reservations;
    private readonly Func<long,Func<bool>,Task<bool>> _dispatch;
    private readonly Func<bool> _ownerCurrent;
    private readonly Func<string,Hero> _findHero;
    private readonly Action<string,NpcPersonaProfile> _stampProfile;
    internal NpcPersonaGenerationApplicationAdapter(PersonaProfileStateOwner profiles,NpcPersonaGenerationOwner reservations,
        Func<long,Func<bool>,Task<bool>> dispatch,Func<bool> ownerCurrent,Func<string,Hero> findHero,Action<string,NpcPersonaProfile> stampProfile)
    { _profiles=profiles; _reservations=reservations; _dispatch=dispatch; _ownerCurrent=ownerCurrent; _findHero=findHero; _stampProfile=stampProfile; }
internal bool NeedsNpcPersonaGeneration(Hero hero)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return false;
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		return string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background);
	}
internal bool IsNpcPersonaGenerationInFlight(Hero hero)
	{
		_reservations.GetState((hero?.StringId ?? "").Trim(), out bool active, out bool coolingDown);
		return active || coolingDown;
	}
    internal void GetNpcPersonaStrings(Hero hero,out string personality,out string background)
    {
        personality=""; background="";
        if (hero!=null && !string.IsNullOrEmpty(hero.StringId) && _profiles.Profiles!=null && _profiles.Profiles.TryGetValue(hero.StringId,out var profile) && profile!=null)
        { personality=profile.Personality ?? ""; background=profile.Background ?? ""; }
    }
    internal NpcPersonaProfile GetNpcPersonaProfile(Hero hero,bool createIfMissing) => hero==null ? null : _profiles.Get(hero.StringId,createIfMissing);
    internal void SaveNpcPersonaProfile(Hero hero,NpcPersonaProfile profile) => _profiles.Save(hero?.StringId,profile,_stampProfile);
    internal NpcPersonaGenerationWork CaptureNpcPersonaGeneration(Hero hero, bool ignoreRetryCooldown, bool overwriteExisting)
    {
        if (hero == null) return new NpcPersonaGenerationWork { Failure = overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_MissingNpc", "The NPC whose persona should be regenerated could not be found.") : "" };
        string id = hero.StringId;
        if (string.IsNullOrEmpty(id)) return new NpcPersonaGenerationWork { Failure = overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_MissingHeroId", "This NPC has no valid HeroId; their persona cannot be regenerated.") : "" };
        GetNpcPersonaStrings(hero, out string personality, out string background);
        if (!overwriteExisting && !string.IsNullOrWhiteSpace(personality) && !string.IsNullOrWhiteSpace(background))
            return new NpcPersonaGenerationWork();
        NpcPersonaGenerationOwner.Lease lease = _reservations.TryBegin(id, ignoreRetryCooldown, out bool coolingDown);
        if (lease == null) return new NpcPersonaGenerationWork { Failure = !overwriteExisting ? "" : coolingDown
            ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_RecentFailure", "This NPC's last generation request just failed. Please try again later.") : AdapterRecordTexts.T("AF_UI_PersonaGeneration_AlreadyGenerating", "This NPC's personality and background are being generated. Wait for the current request to finish before trying again.") };
        try
        {
            var work = new NpcPersonaGenerationWork
            {
                Id = id, OriginalPersonality = personality, OriginalBackground = background, Reservation = lease,
                Facts = PersonaGenerationFactCaptureAdapter.BuildHeroFactsForPersonaGeneration(hero), Requirements = DuelSettings.GetSettings()?.NpcPersonaGenerationRequirements
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
        catch { _reservations.Complete(lease, saved: false); throw; }
    }

    internal async Task<string> GenerateNpcPersonaAsync(Hero hero, bool ignoreRetryCooldown, bool overwriteExisting)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        NpcPersonaGenerationWork work = null;
        bool saved = false, retired = false;
        try
        {
            // Reuse this MyBehavior owner's existing budgeted queue and its Campaign/generation checks.
            // The queue retains its historical memory name; persona does not create a second scheduler.
            bool captured = await _dispatch(generation, () =>
            {
                work = CaptureNpcPersonaGeneration(hero, ignoreRetryCooldown, overwriteExisting);
                return true;
            }).ConfigureAwait(false);
            if (!captured || work == null) return overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_InvalidatedNotSaved", "The request was invalidated; the new persona was not saved.") : "";
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
            bool started = await _dispatch(generation, () =>
            {
                if (!_reservations.IsCurrent(work.Reservation) || !ReferenceEquals(_findHero(work.Id), hero))
                {
                    retired = true;
                    work.Failure = overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_InvalidatedNotGenerated", "The request was invalidated by data cleanup or a character change; no new persona was generated.") : "";
                    return true;
                }
                if (overwriteExisting)
                {
                    GetNpcPersonaStrings(hero, out string currentPersonality, out string currentBackground);
                    if (!string.Equals(currentPersonality, work.OriginalPersonality, StringComparison.Ordinal)
                        || !string.Equals(currentBackground, work.OriginalBackground, StringComparison.Ordinal))
                    {
                        retired = true;
                        work.Failure = AdapterRecordTexts.T("AF_UI_PersonaGeneration_ChangedDuringGeneration", "The persona changed during generation; the latest personality and historical background were not overwritten. Review the changes before regenerating.");
                        return true;
                    }
                }
                string loreSource = "";
                try { loreSource = KnowledgeLibraryBehavior.Instance?.BuildPersonaLoreSource(hero, loreRule, work.LoreRuleVersion) ?? ""; }
                catch (Exception error) { Logger.Log("NpcPersona", "[WARN] Lore source skipped: " + error.Message); }
                NpcPersonaPrompt prompt = NpcPersonaTextRules.BuildNative(work.Facts + (string.IsNullOrWhiteSpace(loreSource) ? "" : "\n" + loreSource),
                    work.OriginalPersonality, work.OriginalBackground, overwriteExisting, work.Requirements);
                // Capture provider settings on the game thread before the gateway's first await.
                work.Response = ConfiguredChatApplicationAdapter.CallAuxiliaryGatewayDetailed(prompt.System, prompt.User, "NpcPersona", 0, forceThinkingDisabled: false);
                return true;
            }).ConfigureAwait(false);
            if (!started) { retired = true; return overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_InvalidatedNotSaved", "The request was invalidated; the new persona was not saved.") : ""; }
            if (work.Response == null) return work.Failure ?? "";
            ApiCallResult response = await work.Response.ConfigureAwait(false);
            if (!_ownerCurrent() || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return "";
            string resp = response.Content ?? "";
            string genP = "", genB = "";
            bool parsed = await Task.Run(() => response.Success && !string.IsNullOrWhiteSpace(resp)
                && NpcPersonaTextRules.TryParsePersonaJson(resp, out genP, out genB)).ConfigureAwait(false);
            genP = NpcPersonaProfilePolicy.NormalizeGenerated(genP);
            genB = NpcPersonaProfilePolicy.NormalizeGenerated(genB);
            NpcPersonaProfilePolicy.CompleteGeneratedFields(ref genP, ref genB);
            string failure = null;
            bool accepted = await _dispatch(generation, () =>
            {
                if (!_reservations.IsCurrent(work.Reservation) || !ReferenceEquals(_findHero(work.Id), hero))
                { retired = true; failure = overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_CleanupInvalidatedNotSaved", "The request was invalidated by data cleanup or a character change; the new persona was not saved.") : ""; return true; }
                if (parsed)
                {
                    GetNpcPersonaStrings(hero, out string curP, out string curB);
                    if (!NpcPersonaProfilePolicy.TryMerge(overwriteExisting, work.OriginalPersonality, work.OriginalBackground,
                        curP, curB, genP, genB, out string nextPersonality, out string nextBackground))
                    {
                        retired = true;
                        failure = AdapterRecordTexts.T("AF_UI_PersonaGeneration_ChangedDuringGeneration", "The persona changed during generation; the latest personality and historical background were not overwritten. Review the changes before regenerating.");
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
                        ? LlmRetryPrompt.BuildFailureDetail("NPC personality and background response parsing failed; the persona was not saved.", resp, response.ResponseBody)
                        : (response.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("NPC personality and background generation failed.", resp, response.ResponseBody));
                    Logger.Log("NpcPersona", "[WARN] AutoGen did not save profile for " + work.Id + ": " + failure);
                    failure = PersonaFailureUiText.ProjectFailureDetail(failure);
                }
                return true;
            }).ConfigureAwait(false);
            return accepted ? failure ?? "" : overwriteExisting ? AdapterRecordTexts.T("AF_UI_PersonaGeneration_InvalidatedNotSaved", "The request was invalidated; the new persona was not saved.") : "";
        }
        catch (Exception error)
        {
            if (!_ownerCurrent() || !SaveRuntimeGuard.IsCurrentGeneration(generation)) return "";
            Logger.Log("NpcPersona", "[ERROR] AutoGen failed: " + error.Message);
            return LlmRetryPrompt.BuildFailureDetail(error.Message, "");
        }
        finally
        {
            _reservations.Complete(work?.Reservation, saved,
                !retired && _ownerCurrent() && SaveRuntimeGuard.IsCurrentGeneration(generation));
        }
    }

    internal async Task EnsureNpcPersonaGeneratedAsync(Hero hero, bool ignoreRetryCooldown = false)
    {
        long generation = SaveRuntimeGuard.CaptureGeneration();
        string failure = await GenerateNpcPersonaAsync(hero, ignoreRetryCooldown, overwriteExisting: false).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(failure))
            await _dispatch(generation, () =>
            {
                LlmRetryPrompt.ShowFailurePopup(PersonaFailureUiText.NpcTitle, failure);
                return true;
            }).ConfigureAwait(false);
    }
    internal void GetNpcPersonaGenerationRuntimeState(Hero hero, out bool active, out bool coolingDown)
    {
        _reservations.GetState((hero?.StringId ?? "").Trim(), out active, out coolingDown);
    }



}

internal static class RebelNamingPromptSummaryCaptureAdapter
{
internal static string BuildRebelSettlementSummaryForNamingPrompt(IEnumerable<Settlement> settlements)
	{
		if (settlements == null)
		{
			return AdapterPromptCaptureTexts.PcapText("rebel_no_summary");
		}
		List<string> list = new List<string>();
		foreach (Settlement settlement in settlements.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).Take(4))
		{
			string settlementDisplayName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string settlementBackgroundForRebelNamingPrompt = KingdomRebellionGameAdapter.BuildSettlementBackgroundForRebelNamingPrompt(settlement);
			list.Add(RebellionNamingRules.SettlementLine(settlementDisplayName, settlementBackgroundForRebelNamingPrompt));
		}
		return RebellionNamingRules.JoinSettlementSummary(list);
	}
internal static string BuildRebelFollowerSummaryForNamingPrompt(IEnumerable<Clan> followerClans)
	{
		if (followerClans == null)
		{
			return AdapterPromptCaptureTexts.PcapText("rebel_no_summary");
		}
		List<string> list = new List<string>();
		foreach (Clan followerClan in followerClans)
		{
			if (followerClan == null)
			{
				continue;
			}
			string clanDisplayName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(followerClan);
			string heroDisplayName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(followerClan.Leader);
			string text = KingdomStabilityGameAdapter.BuildClanFortificationSummaryForPrompt(followerClan);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = AdapterPromptCaptureTexts.PcapText("rebel_no_fief_summary");
			}
			list.Add(RebellionNamingRules.FollowerLine(clanDisplayName, heroDisplayName, text));
		}
		return RebellionNamingRules.JoinFollowerSummary(list);
	}
}
