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
	private static readonly Regex TagPattern = new Regex("^\\[A:CIVIL_FACTION:(JOIN:(CROWN|OPPOSITION)|RECRUIT|DETONATE|ANSWER:(ACCEPT|REFUSE))\\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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

	public int GetSettlementLoyaltyDelta(Settlement settlement) => DuelSettings.IsCivilWarFactionsEnabled() ? _owner.GetOppositionLoyaltyDelta(settlement) : 0;

	public bool BlocksNewOffensiveWar(Kingdom kingdom) => DuelSettings.IsCivilWarFactionsEnabled() && _owner.IsInOpenCivilWar(kingdom?.StringId);

	public void ApplyPrestigeDelta(string kingdomId, int delta, string reason)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || delta == 0) return;
		DiplomacyConversationBridge.ApplyExternalPrestigeDelta(kingdomId, delta, reason ?? "内战");
	}

	public CivilWarPanelKingdom GetPlayerKingdomPanel() => _owner.BuildPlayerKingdomPanel();

	public List<PostprocessRuleEntry> BuildPostprocessRules()
	{
		if (!DuelSettings.IsCivilWarFactionsEnabled()) return new List<PostprocessRuleEntry>();
		return new List<PostprocessRuleEntry>
		{
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:JOIN:CROWN]", Description = "玩家正式封臣明确决定家族加入本国王室阵营，且当前NPC确认时输出。无需先存在反对派；换派须先退出并等待七天，不能用对话绕过。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:JOIN:OPPOSITION]", Description = "玩家明确决定自己的家族加入当前王国已成形的反对派，且当前NPC明确确认时输出。询问、犹豫或派系未成形时禁止。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:RECRUIT]", Description = "玩家已有明确派系，并明确说服当前NPC的整个家族加入该派系，双方都无条件同意时输出。拒绝、身份不明或只是本人入队时禁止。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:DETONATE]", Description = "玩家明确要求立即引爆当前王国的内战，且当前NPC明确同意执行时输出。讨论、威胁或劝阻时禁止。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:ANSWER:ACCEPT]", Description = "玩家国王明确接受当前最后通牒时输出；仅在面板或对话明确显示待答复时使用。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:ANSWER:REFUSE]", Description = "玩家国王明确拒绝当前最后通牒时输出；仅在面板或对话明确显示待答复时使用。" }
		};
	}

	public bool TryApplyTag(Hero speaker, string tag, out string message)
	{
		message = "";
		if (!DuelSettings.IsCivilWarFactionsEnabled()) return false;
		Match match = TagPattern.Match((tag ?? "").Trim());
		if (!match.Success || speaker == null) return false;
		Kingdom kingdom = speaker.Clan?.Kingdom ?? Clan.PlayerClan?.Kingdom;
		string action = match.Groups[1].Value ?? "";
		if (action.StartsWith("JOIN:CROWN", StringComparison.OrdinalIgnoreCase)) return _owner.TryJoinPlayer(kingdom, speaker, KingdomCivilWarSide.Crown, out message);
		if (action.StartsWith("JOIN:OPPOSITION", StringComparison.OrdinalIgnoreCase)) return _owner.TryJoinPlayer(kingdom, speaker, KingdomCivilWarSide.Opposition, out message);
		if (action.Equals("RECRUIT", StringComparison.OrdinalIgnoreCase)) return _owner.TryRecruitClan(Hero.MainHero, speaker.Clan, kingdom, out message);
		if (action.Equals("DETONATE", StringComparison.OrdinalIgnoreCase)) return _owner.TryDetonate(speaker, kingdom, out message);
		// Ultimatums are always addressed to the player's own kingdom, whoever the player is talking to.
		if (action.Equals("ANSWER:ACCEPT", StringComparison.OrdinalIgnoreCase) || action.Equals("ANSWER:REFUSE", StringComparison.OrdinalIgnoreCase))
			return _owner.TryAnswerPlayerUltimatum(Clan.PlayerClan?.Kingdom, speaker, action.EndsWith("ACCEPT", StringComparison.OrdinalIgnoreCase), out message);
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
