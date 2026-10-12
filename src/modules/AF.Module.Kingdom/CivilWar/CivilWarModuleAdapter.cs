using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

internal sealed class CivilWarModuleAdapter : ICivilWarModulePort
{
	private static readonly Regex TagPattern = new Regex("^\\[A:CIVIL_FACTION:(JOIN:(CROWN|OPPOSITION)|RECRUIT|LEAVE:(SELF|PLAYER)|DETONATE|ANSWER:(ACCEPT|REFUSE))\\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private readonly KingdomCivilWarOwner _owner = new KingdomCivilWarOwner();
	public bool CanTrackCoupWar(Kingdom kingdom) => KingdomCivilWarOwner.CanTrackCoupWar(kingdom);
	public bool TryRegisterCoupWar(CoupCivilWarRegistration registration, out string message) => _owner.TryRegisterCoupWar(registration, out message);
	public long Revision => _owner.Revision;
	public event Action StateChanged { add { _owner.StateChanged += value; } remove { _owner.StateChanged -= value; } }
	public List<CivilWarFoundingOption> GetFoundingOptions(string kingdomId) => _owner.GetFoundingOptions(kingdomId, Clan.PlayerClan);
	public CivilWarActionQuote Quote(CivilWarActionRequest request) => _owner.Quote(request, Clan.PlayerClan);
	public CivilWarActionResult Execute(CivilWarActionRequest request) => _owner.Execute(request, Clan.PlayerClan);
	public void NotifyPoliticalChange(Kingdom kingdom, string sourceId) => _owner.NotifyPoliticalChange(kingdom, sourceId);
	public void ProcessPending() => _owner.ProcessPending();
	public bool TryTakePoliticalResponse(out CivilWarActionRequest request, out string text) => _owner.TryTakePoliticalResponse(out request, out text);

	public void Load(string json)
	{
		try
		{
			_owner.Replace(string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<KingdomCivilWarStorage>(json));
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[WARN] Civil war save rejected: " + ex.Message);
			_owner.Replace(null);
		}
	}

	public string Save()
	{
		try { return JsonConvert.SerializeObject(_owner.Storage) ?? ""; }
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[ERROR] Serialize civil war failed: " + ex.Message);
			return "";
		}
	}

	public void AdvanceDay(int dayIndex) => _owner.AdvanceDay(dayIndex);

	public void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> recentEvents)
	{
		_owner.AdvanceWeek(kingdom, weekIndex, stability, (target, delta) => adjustStability?.Invoke(target, delta), recentEvents);
	}

	public void RecordGrievance(Kingdom kingdom, string sourceId, IEnumerable<Clan> clans, float points, int week, string text)
	{
		_owner.AddGrievance(kingdom, sourceId, clans, points, week, text);
	}

	public void RecordPolicyImposed(Kingdom kingdom, string policyId, int week, string text)
	{
		_owner.RecordPolicyImposed(kingdom, policyId, week, text);
	}

	public bool HasTrackedKingdom(Kingdom kingdom) => DuelSettings.IsCivilWarFactionsEnabled() && _owner.HasTrackedKingdom(kingdom);

	public void NoteKingdomRebellion(Kingdom kingdom)
	{
		if (DuelSettings.IsCivilWarFactionsEnabled()) _owner.NoteKingdomRebellion(kingdom);
	}

	public int GetSettlementLoyaltyDelta(Settlement settlement) => DuelSettings.IsCivilWarFactionsEnabled() ? _owner.GetOppositionLoyaltyDelta(settlement) : 0;

	public bool BlocksNewOffensiveWar(Kingdom kingdom) => DuelSettings.IsCivilWarFactionsEnabled() && _owner.IsInOpenCivilWar(kingdom?.StringId);

	public void ApplyPrestigeDelta(string kingdomId, int delta, string reason)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || delta == 0) return;
		DiplomacyConversationBridge.ApplyExternalPrestigeDelta(kingdomId, delta, reason ?? KingdomCivilWarTexts.Raw("KW_Raw_073"));
	}

	public CivilWarPanelKingdom GetPlayerKingdomPanel() => _owner.BuildPlayerKingdomPanel();

	public List<PostprocessRuleEntry> BuildPostprocessRules()
	{
		if (!DuelSettings.IsCivilWarFactionsEnabled()) return new List<PostprocessRuleEntry>();
		return new List<PostprocessRuleEntry>
		{
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:JOIN:CROWN]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_041") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:JOIN:OPPOSITION]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_042") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:RECRUIT]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_043") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:LEAVE:SELF]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_044") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:LEAVE:PLAYER]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_045") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:DETONATE]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_046") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:ANSWER:ACCEPT]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_047") },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:ANSWER:REFUSE]", Description = KingdomCivilWarTexts.Prompt("KW_Prompt_048") }
		};
	}

	// Built once per prompt/postprocess build (one conversation turn). Reads state only, never scans per tick.
	public List<PostprocessRuleEntry> BuildPostprocessRules(Hero target)
	{
		List<PostprocessRuleEntry> all = BuildPostprocessRules();
		if (all.Count == 0) return all;
		HashSet<string> applicable = _owner.ApplicableDialogueActions(target);
		const string prefix = "[A:CIVIL_FACTION:";
		return all.FindAll(rule =>
		{
			string tag = (rule?.Tag ?? "").Trim();
			return tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && tag.EndsWith("]", StringComparison.Ordinal)
				&& applicable.Contains(tag.Substring(prefix.Length, tag.Length - prefix.Length - 1));
		});
	}

	public string BuildDialogueFact(Hero target) => _owner.BuildDialogueFact(target);

	public bool IsClanProtected(Clan clan) => _owner.IsClanProtected(clan);

	public bool TryApplyTag(Hero speaker, string tag, out string message)
		=> TryApplyTag(speaker, tag, out message, out _);

	public bool TryApplyTag(Hero speaker, string tag, out string message, out string displayMessage)
	{
		message = "";
		displayMessage = "";
		if (!DuelSettings.IsCivilWarFactionsEnabled()) return false;
		Match match = TagPattern.Match((tag ?? "").Trim());
		if (!match.Success || speaker == null) return false;
		Kingdom kingdom = speaker.Clan?.Kingdom ?? Clan.PlayerClan?.Kingdom;
		string action = match.Groups[1].Value ?? "";
		if (action.StartsWith("JOIN:CROWN", StringComparison.OrdinalIgnoreCase)) return _owner.TryJoinPlayer(kingdom, speaker, KingdomCivilWarSide.Crown, out message, out displayMessage);
		if (action.StartsWith("JOIN:OPPOSITION", StringComparison.OrdinalIgnoreCase)) return _owner.TryJoinPlayer(kingdom, speaker, KingdomCivilWarSide.Opposition, out message, out displayMessage);
		if (action.Equals("RECRUIT", StringComparison.OrdinalIgnoreCase)) return _owner.TryRecruitClan(Hero.MainHero, speaker.Clan, kingdom, out message, out displayMessage);
		if (action.Equals("LEAVE:SELF", StringComparison.OrdinalIgnoreCase)) return speaker != Hero.MainHero && speaker.Clan?.Leader == speaker && _owner.TryLeaveClan(kingdom, speaker.Clan, out message, out displayMessage);
		if (action.Equals("LEAVE:PLAYER", StringComparison.OrdinalIgnoreCase)) return _owner.TryLeaveClan(Clan.PlayerClan?.Kingdom, Clan.PlayerClan, out message, out displayMessage);
		if (action.Equals("DETONATE", StringComparison.OrdinalIgnoreCase)) return _owner.TryDetonate(speaker, kingdom, out message, out displayMessage);
		// Ultimatums are always addressed to the player's own kingdom, whoever the player is talking to.
		if (action.Equals("ANSWER:ACCEPT", StringComparison.OrdinalIgnoreCase) || action.Equals("ANSWER:REFUSE", StringComparison.OrdinalIgnoreCase))
		{
			bool applied = _owner.TryAnswerPlayerUltimatum(Clan.PlayerClan?.Kingdom, speaker, action.EndsWith("ACCEPT", StringComparison.OrdinalIgnoreCase), out message);
			// This pre-existing entry already returns its localized outcome. Preserve its fact semantics.
			displayMessage = message;
			return applied;
		}
		return false;
	}

	public void NotifyRebelKingdomCreated(string factionId, Kingdom rebelKingdom, int week)
	{
		_owner.OnRebelKingdomCreated(factionId, rebelKingdom, week);
	}

	public bool IsRebellionRequestActive(string factionId) => _owner.IsRebellionRequestActive(factionId);

	public void NotifyRebellionFailed(string factionId, string reason) => _owner.NotifyRebellionFailed(factionId, reason);

	public void RecordPeace(Kingdom kingdom, IFaction other, int week) => _owner.RecordPeace(kingdom, other, week);

	public bool IsCivilWarPair(Kingdom a, Kingdom b) => _owner.IsCivilWarPair(a, b);

	public bool HasPendingPlayerUltimatumPrompt => _owner.HasUnpromptedPlayerUltimatum && DuelSettings.IsCivilWarFactionsEnabled();

	public bool HasPendingFollowPrompt => _owner.HasPendingFollowPrompt && DuelSettings.IsCivilWarFactionsEnabled();

	public bool TryTakeFollowPrompt(out string factionId, out string text)
	{
		KingdomCivilWarFactionState faction = _owner.TakePendingFollowPrompt();
		factionId = faction?.Id ?? "";
		text = KingdomCivilWarOwner.DescribeFollowPrompt(faction);
		return faction != null;
	}

	public bool AnswerFollow(string factionId, bool follow, out string message) => _owner.AnswerFollow(factionId, follow, out message);

	public bool TryTakePlayerUltimatumPrompt(out string factionId, out string text)
	{
		KingdomCivilWarFactionState faction = _owner.TakeUnpromptedPlayerUltimatum(out _);
		factionId = faction?.Id ?? "";
		text = KingdomCivilWarOwner.DescribeUltimatum(faction);
		return faction != null;
	}

	public bool AnswerPlayerUltimatum(string factionId, bool accept, out string message)
	{
		return _owner.TryAnswerPlayerUltimatum(factionId, accept, out message);
	}
}
