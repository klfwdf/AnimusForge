using TaleWorlds.Localization;
using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class SocialUiLocalization
{
	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Format(string id, string englishFallback, params (string Key, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + englishFallback);
		if (variables != null)
		{
			foreach ((string key, object value) in variables)
			{
				text.SetTextVariable(key, value?.ToString() ?? string.Empty);
			}
		}
		return text.ToString();
	}

	/// <summary>
	/// Converts the recruitment result only at the player-facing notification boundary.
	/// The source status remains unchanged because it is also used by AFEF/history and
	/// by the legacy action-prefix guard.
	/// </summary>
	internal static string RecruitmentStatusForDisplay(string status)
	{
		if (string.IsNullOrWhiteSpace(status))
		{
			return status ?? string.Empty;
		}

		string prefixId = null;
		string prefixFallback = null;
		string detail = status;
		if (status.StartsWith("执行失败：", StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentFailed";
			prefixFallback = "[Recruitment failed] {DETAIL}";
			detail = status.Substring("执行失败：".Length);
		}
		else if (status.StartsWith("执行失败（", StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentFailed";
			prefixFallback = "[Recruitment failed] {DETAIL}";
			detail = status.Substring("执行失败".Length);
		}
		else if (status.StartsWith("执行跳过：", StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentSkipped";
			prefixFallback = "[Recruitment skipped] {DETAIL}";
			detail = status.Substring("执行跳过：".Length);
		}
		else if (status.StartsWith("执行拦截：", StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentIntercepted";
			prefixFallback = "[Recruitment intercepted] {DETAIL}";
			detail = status.Substring("执行拦截：".Length);
		}
		else if (status.StartsWith("执行成功：", StringComparison.Ordinal))
		{
			prefixId = "AF_SOCIAL_UI_RecruitmentSucceeded";
			prefixFallback = "[Recruitment succeeded] {DETAIL}";
			detail = status.Substring("执行成功：".Length);
		}

		if (prefixId == null)
		{
			return status;
		}

		string translatedDetail = TranslateRecruitmentDetail(detail);
		return Format(prefixId, prefixFallback, ("DETAIL", translatedDetail));
	}

	internal static string MarriageStatusForDisplay(string status)
	{
		if (string.IsNullOrWhiteSpace(status))
			return status ?? string.Empty;
		if (status.StartsWith("正规联姻失败：", StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageFormalFailed", "Formal marriage failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring("正规联姻失败：".Length))));
		if (status.StartsWith("私奔失败：", StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageElopeFailed", "Elope failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring("私奔失败：".Length))));
		if (status.StartsWith("离婚失败：", StringComparison.Ordinal))
			return Format("AF_SOCIAL_UI_MarriageDivorceFailed", "Divorce failed: {REASON}", ("REASON", TranslateMarriageReason(status.Substring("离婚失败：".Length))));

		Match match = Regex.Match(status, "^正规联姻成功：(?<left>.+) 与 (?<right>.+) 已成婚。等级差D=(?<tier>-?\\d+)，家族关系=(?<relation>-?\\d+)，族长综合信任=(?<trust>-?\\d+)。(?<extra>.*)$");
		if (match.Success)
		{
			string extra = TranslateMarriageExtra(match.Groups["extra"].Value);
			return Format("AF_SOCIAL_UI_MarriageFormalSucceeded", "Formal marriage succeeded: {LEFT} and {RIGHT} are now married. Tier gap D={TIER}, clan relation={RELATION}, leader trust={TRUST}.{EXTRA}",
				("LEFT", match.Groups["left"].Value), ("RIGHT", match.Groups["right"].Value), ("TIER", match.Groups["tier"].Value), ("RELATION", match.Groups["relation"].Value), ("TRUST", match.Groups["trust"].Value), ("EXTRA", extra));
		}
		match = Regex.Match(status, "^私奔成功：你与 (?<target>.+) 已成婚。当前 L=(?<love>-?\\d+)，对象私人信任=(?<trust>-?\\d+)，对象家族融洽度F=(?<harmony>-?\\d+)。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_MarriageElopeSucceeded", "Elope succeeded: you and {TARGET} are now married. Current L={LOVE}, personal trust={TRUST}, family harmony F={HARMONY}.", ("TARGET", match.Groups["target"].Value), ("LOVE", match.Groups["love"].Value), ("TRUST", match.Groups["trust"].Value), ("HARMONY", match.Groups["harmony"].Value));
		match = Regex.Match(status, "^离婚成功：(?<left>.+) 与 (?<right>.+) 已解除婚姻关系。(?<extra>.*)$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_MarriageDivorceSucceeded", "Divorce succeeded: {LEFT} and {RIGHT} are no longer married.{EXTRA}", ("LEFT", match.Groups["left"].Value), ("RIGHT", match.Groups["right"].Value), ("EXTRA", TranslateMarriageExtra(match.Groups["extra"].Value)));
		return status;
	}

	private static string TranslateMarriageReason(string reason)
	{
		if (reason == "未找到婚配双方。") return Text("AF_SOCIAL_UI_MarriageReasonMissingPair", "The marriage pair could not be found.");
		if (reason == "不能和自己结婚。" || reason == "不能让同一人与自己结婚。") return Text("AF_SOCIAL_UI_MarriageReasonSelf", "A character cannot marry themselves.");
		if (reason == "双方已经存在有效婚姻记录。") return Text("AF_SOCIAL_UI_MarriageReasonAlreadyMarried", "The pair already has an active marriage record.");
		if (reason == "正规联姻要求双方来自不同家族。") return Text("AF_SOCIAL_UI_MarriageReasonSameClan", "A formal marriage requires two different clans.");
		if (reason == "未找到对方婚配对象。" || reason == "未找到对方婚配对象（targetHeroId）。") return Text("AF_SOCIAL_UI_MarriageReasonMissingTarget", "The target marriage partner could not be found.");
		if (reason == "未找到玩家家族婚配人选（playerClanHeroId）。") return Text("AF_SOCIAL_UI_MarriageReasonMissingPlayerCandidate", "The player-clan marriage candidate could not be found.");
		if (reason == "目标没有家族，无法走家族联姻流程。") return Text("AF_SOCIAL_UI_MarriageReasonTargetNoClan", "The target has no clan and cannot use the clan marriage flow.");
		if (reason == "未找到有效婚姻记录。") return Text("AF_SOCIAL_UI_MarriageReasonNoRecord", "No active marriage record was found.");
		if (reason == "双方当前并非彼此配偶。") return Text("AF_SOCIAL_UI_MarriageReasonNotSpouses", "The pair are not currently spouses.");
		if (reason == "未找到玩家英雄。") return Text("AF_SOCIAL_UI_MarriageReasonMissingPlayer", "The player hero could not be found.");
		return reason;
	}

	private static string TranslateMarriageExtra(string extra)
	{
		if (string.IsNullOrWhiteSpace(extra)) return string.Empty;
		Match match = Regex.Match(extra, "^ 已核销你先前主动交付给族长的彩礼 (?<amount>[0-9,]+) 第纳尔。$");
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePriceConsumed", " The previously prepaid bride price of {AMOUNT} denars was consumed.", ("AMOUNT", match.Groups["amount"].Value));
		match = Regex.Match(extra, "^ 对方族长已向玩家支付彩礼 (?<amount>[0-9,]+) 第纳尔。$");
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePricePaid", " The other clan leader paid the player a bride price of {AMOUNT} denars.", ("AMOUNT", match.Groups["amount"].Value));
		match = Regex.Match(extra, "^ 已返还彩礼 (?<amount>[0-9,]+) 第纳尔。$");
		if (match.Success) return Format("AF_SOCIAL_UI_MarriageBridePriceRefunded", " A bride price of {AMOUNT} denars was refunded.", ("AMOUNT", match.Groups["amount"].Value));
		return extra;
	}

	private static string TranslateRecruitmentDetail(string detail)
	{
		if (detail == "缺少要加入玩家家族的目标英雄。")
			return Text("AF_SOCIAL_UI_RecruitmentMissingClanHero", "No target hero is available to join the player's clan.");
		if (detail == "目标本来就是玩家本人。")
			return Text("AF_SOCIAL_UI_RecruitmentAlreadyPlayer", "The target is already the player.");
		if (detail == "玩家家族或玩家队伍不可用。")
			return Text("AF_SOCIAL_UI_RecruitmentPlayerPartyUnavailable", "The player's clan or party is unavailable.");
		if (detail == "缺少要加入队伍的非英雄NPC。")
			return Text("AF_SOCIAL_UI_RecruitmentMissingNonHero", "No non-hero NPC is available to join the party.");
		if (detail == "目标是英雄，应走英雄入队链路。")
			return Text("AF_SOCIAL_UI_RecruitmentTargetIsHero", "The target is a hero and must use the hero recruitment path.");
		if (detail == "找不到当前说话 NPC 的 live Agent，无法复制外观与装备，已取消升格。")
			return Text("AF_SOCIAL_UI_RecruitmentMissingLiveAgent", "The speaking NPC's live agent could not be found; promotion was cancelled.");
		if (detail == "当前 Agent 不是可升格的非英雄兵种模板。")
			return Text("AF_SOCIAL_UI_RecruitmentInvalidAgentTemplate", "The current agent is not a promotable non-hero troop template.");
		if (detail == "无法确定升格 Hero 的个人名，已取消升格。")
			return Text("AF_SOCIAL_UI_RecruitmentMissingPromotedName", "The promoted hero's personal name could not be determined; promotion was cancelled.");
		if (detail == "酒馆雇佣兵池已被原版选项或其他流程处理，未重复执行入队。")
			return Text("AF_SOCIAL_UI_RecruitmentTavernAlreadyHandled", "The tavern mercenary pool was already handled by the native option or another flow; recruitment was not repeated.");
		if (detail == "目标英雄的俘虏状态或俘虏名册残留未能清除。")
			return Text("AF_SOCIAL_UI_RecruitmentCaptivityCleanupFailed", "The target hero's captivity state or prisoner roster residue could not be cleared.");
		if (detail == "目标英雄未能恢复为可加入队伍的活动状态。")
			return Text("AF_SOCIAL_UI_RecruitmentHeroActivationFailed", "The target hero could not be restored to an active state for party entry.");
		if (detail == "目标英雄未能完成玩家家族成员身份或玩家主队归属更新。")
			return Text("AF_SOCIAL_UI_RecruitmentLordJoinFailed", "The target hero could not be made a player-clan member or assigned to the main party.");
		if (detail == "目标英雄未能完成同伴身份或玩家主队归属更新。")
			return Text("AF_SOCIAL_UI_RecruitmentCompanionJoinFailed", "The target hero could not be made a companion or assigned to the main party.");
		if (detail == "复制当前 NPC 外观失败（" || detail == "复制当前 NPC 装备失败（")
			return detail;

		Match match = Regex.Match(detail, "^(?<name>.+) 已经是玩家同伴并在玩家队伍中。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyCompanion", "{NAME} is already a player companion in the main party.", ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, "^(?<name>.+) 已经是玩家家族成员并在玩家队伍中。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyClanMember", "{NAME} is already a player-clan member in the main party.", ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, "^(?<name>.+) 已经由当前场景 NPC 升格为玩家家族 Hero，不能重复招募生成新的 Hero。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentAlreadyPromoted", "{NAME} was already promoted from the current scene NPC; a second hero cannot be recruited.", ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, "^酒馆中 (?<count>\\d+) 名(?<name>.+) 已全部作为普通士兵加入玩家队伍。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentTavernSoldiers", "{COUNT} {NAME} mercenaries joined the player's party as regular troops.", ("COUNT", match.Groups["count"].Value), ("NAME", match.Groups["name"].Value));
		match = Regex.Match(detail, "^(?<name>.+) 已升格为玩家同伴“(?<hero>.+)”，并加入玩家队伍(?<follow>，当前场景中已开始跟随玩家)?。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentPromotedCompanion", "{NAME} was promoted to player companion \"{HERO}\" and joined the player's party{FOLLOW}.", ("NAME", match.Groups["name"].Value), ("HERO", match.Groups["hero"].Value), ("FOLLOW", match.Groups["follow"].Success ? " and is now following you in the scene" : ""));
		match = Regex.Match(detail, "^(?<name>.+) 已升格为玩家家族 Hero“(?<hero>.+)”，并加入玩家队伍(?<follow>，当前场景中已开始跟随玩家)?。$");
		if (match.Success)
			return Format("AF_SOCIAL_UI_RecruitmentPromotedLord", "{NAME} was promoted to player-clan hero \"{HERO}\" and joined the player's party{FOLLOW}.", ("NAME", match.Groups["name"].Value), ("HERO", match.Groups["hero"].Value), ("FOLLOW", match.Groups["follow"].Success ? " and is now following you in the scene" : ""));
		return detail;
	}
}
