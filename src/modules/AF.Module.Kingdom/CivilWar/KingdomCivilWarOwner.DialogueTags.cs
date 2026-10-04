using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Dialogue side of the faction system: which [A:CIVIL_FACTION:*] tags can execute right now, and the runtime fact the NPC speaks from.
// Both run once per prompt/postprocess build (a single conversation turn), never per tick. They only read state and reuse Quote.
internal sealed partial class KingdomCivilWarOwner
{
	// Action keys are the tag suffixes: JOIN:CROWN, JOIN:OPPOSITION, RECRUIT, LEAVE:SELF, LEAVE:PLAYER, DETONATE, ANSWER:ACCEPT, ANSWER:REFUSE.
	internal HashSet<string> ApplicableDialogueActions(Hero target)
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Clan player = Clan.PlayerClan;
		Kingdom kingdom = player?.Kingdom;
		if (player == null || kingdom == null || !DuelSettings.IsCivilWarFactionsEnabled()) return result;
		var state = Find(kingdom);
		// Ultimatums are addressed to the player's own kingdom whoever the player is talking to.
		if (CivilWarWorld.IsPlayerRuled(kingdom) && state != null && state.Factions.Any(x => x.PlayerAnswerPending))
		{
			result.Add("ANSWER:ACCEPT");
			result.Add("ANSWER:REFUSE");
		}
		Clan targetClan = target?.Clan;
		if (target == null || target == Hero.MainHero || targetClan == null || targetClan.Kingdom != kingdom) return result;
		string kid = kingdom.StringId ?? "";
		KingdomCivilWarClanState playerRecord = null;
		state?.Clans.TryGetValue(player.StringId ?? "", out playerRecord);
		if (playerRecord == null || playerRecord.Side == KingdomCivilWarSide.Middle)
		{
			if (Quote(new CivilWarActionRequest { KingdomId = kid, Action = CivilWarAction.JoinCrown }, player).Allowed) result.Add("JOIN:CROWN");
			KingdomCivilWarFactionState joinable = ResolveJoinFaction(state, target);
			if (joinable != null && Quote(new CivilWarActionRequest { KingdomId = kid, FactionId = joinable.Id, Action = CivilWarAction.JoinOpposition }, player).Allowed) result.Add("JOIN:OPPOSITION");
		}
		if (RecruitBlockReason(Hero.MainHero, targetClan, kingdom, state).Length == 0) result.Add("RECRUIT");
		if (targetClan != player && targetClan.Leader == target && Quote(new CivilWarActionRequest { KingdomId = kid, Action = CivilWarAction.Leave }, targetClan).Allowed) result.Add("LEAVE:SELF");
		if (Quote(new CivilWarActionRequest { KingdomId = kid, Action = CivilWarAction.Leave }, player).Allowed) result.Add("LEAVE:PLAYER");
		KingdomCivilWarFactionState own = FactionOfClan(state, player);
		if (own != null && IsPreWar(own) && own.LeaderClanId == targetClan.StringId
			&& Quote(new CivilWarActionRequest { KingdomId = kid, FactionId = own.Id, Action = CivilWarAction.Detonate }, player).Allowed) result.Add("DETONATE");
		return result;
	}

	// The speaker's faction when it is still pre-war, else the kingdom's only pre-war faction. Never guesses between several.
	private KingdomCivilWarFactionState ResolveJoinFaction(KingdomCivilWarKingdomState state, Hero speaker)
	{
		KingdomCivilWarFactionState faction = FactionOfClan(state, speaker?.Clan);
		if (IsPreWar(faction)) return faction;
		var open = state?.Factions.Where(IsPreWar).Take(2).ToList();
		return open != null && open.Count == 1 ? open[0] : null;
	}

	// Side-effect free recruitment preconditions; TryRecruitClan and the tag filter share it so they cannot drift apart.
	private string RecruitBlockReason(Hero recruiter, Clan target, Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		if (recruiter?.Clan == null || !PoliticalClan(target, kingdom) || kingdom == null || recruiter.Clan.Kingdom != kingdom || target == Clan.PlayerClan || target == kingdom.RulingClan)
			return "当前对象不满足派系招募条件。";
		if (state != null && state.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar)) return "内战期间不能招募或换派。";
		if (state != null && state.Factions.Any(x => x.LeaderClanId == target.StringId)) return "派系领袖不会被游说。";
		KingdomCivilWarClanState targetRecord = null;
		state?.Clans.TryGetValue(target.StringId ?? "", out targetRecord);
		if (targetRecord != null && targetRecord.Side != KingdomCivilWarSide.Middle) return "该家族已有派系，须先退出。";
		if (_storage.ClanExitUntilDay.TryGetValue(target.StringId ?? "", out int exitUntil) && CivilWarWorld.CurrentDay() < exitUntil) return "该家族刚退出派系，冷却至第 " + exitUntil + " 天，暂不能被招募。";
		if (CivilWarWorld.IsPlayerRuled(kingdom) && recruiter.Clan == kingdom.RulingClan) return "";
		KingdomCivilWarClanState source = null;
		if (state == null || !state.Clans.TryGetValue(recruiter.Clan.StringId ?? "", out source) || source.Side == KingdomCivilWarSide.Middle) return "招募者没有明确派系。";
		if (source.Side == KingdomCivilWarSide.Opposition && !IsPreWar(FactionOfClan(state, recruiter.Clan))) return "招募者所在派系已无法招募。";
		return "";
	}

	// Runtime fact for the NPC's own rule text. The relation only shades willingness; it never becomes a code veto,
	// because the NPC has already spoken by the time a tag is parsed.
	internal string BuildDialogueFact(Hero target)
	{
		Clan clan = target?.Clan;
		Kingdom kingdom = clan?.Kingdom;
		if (!DuelSettings.IsCivilWarFactionsEnabled() || kingdom == null || Clan.PlayerClan?.Kingdom != kingdom || kingdom.Leader == null) return "";
		if (clan == Clan.PlayerClan || clan == kingdom.RulingClan || !PoliticalClan(clan, kingdom)) return "";
		var state = Find(kingdom);
		if (state != null && state.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar))
			return "【派系运行时事实】王国正处于内战期间，家族的派系站位不能变动，不得答应任何加入、退出或招募派系的请求。";
		KingdomCivilWarClanState record = null;
		state?.Clans.TryGetValue(clan.StringId ?? "", out record);
		KingdomCivilWarFactionState faction = FactionOfClan(state, clan);
		string stance = record == null || record.Side == KingdomCivilWarSide.Middle ? "中立"
			: record.Side == KingdomCivilWarSide.Crown ? "王室派"
			: faction == null ? "反对派" : "反对派（" + FactionName(faction, CivilWarCatalog.FindDemand(faction.DemandId)) + "）";
		int relation = CivilWarWorld.Relation(clan.Leader, kingdom.Leader);
		float factor = CivilWarFactionRules.JoinRelationFactor(relation);
		var text = new StringBuilder("【派系运行时事实】你的家族目前站位：").Append(stance)
			.Append("；族长与国王关系 ").Append(relation).Append("（").Append(factor >= 0.95f ? "一般或更差" : factor > 0.5f ? "较好" : "很好").Append("）。");
		if (factor < 1f) text.Append("关系越好越不愿倒向反对派，但并非绝对不会；被充分说服或利益足够时仍可能同意。");
		if (clan.Leader != target) text.Append("你不是族长，不能代表家族加入或退出派系。");
		else if (faction != null && faction.LeaderClanId == clan.StringId) text.Append("你是该派系的领袖，不会被他人招募；若你自己决定退出，领导权将移交继任者。");
		if (_storage.ClanExitUntilDay.TryGetValue(clan.StringId ?? "", out int until) && CivilWarWorld.CurrentDay() < until)
			text.Append("你的家族刚退出派系，冷却至第 ").Append(until).Append(" 天，期间不能再加入派系。");
		return text.ToString();
	}
}
