using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior
{
	// Rules are prepared once; only notification events project text, never queue ticks or saved facts.
	private sealed class WorldMapDisplayRule
	{
		internal readonly string Id;
		internal readonly string Fallback;
		internal readonly Regex Pattern;
		internal readonly string[] Variables;

		internal WorldMapDisplayRule(string suffix, string pattern, string fallback)
		{
			Id = "AF_WM_Review_" + suffix;
			Fallback = fallback;
			Pattern = new Regex(pattern, RegexOptions.CultureInvariant);
			Variables = Pattern.GetGroupNames();
		}
	}

	private static readonly WorldMapDisplayRule[] AdditionalWorldMapDisplayRules =
	{
		new WorldMapDisplayRule("RebellionNoFiefs", @"^玩家家族没有封地，不能执行带城叛乱。?$", "The player's clan has no fiefs and cannot rebel while retaining land."),
		new WorldMapDisplayRule("RebellionWarMissing", @"^玩家家族叛乱动作未能建立与旧王国的战争状态。?$", "The rebellion action failed to establish war with the former kingdom."),
		new WorldMapDisplayRule("RebellionException", @"^玩家家族叛乱动作执行失败：(?<DETAIL>.*)$", "The player's clan rebellion failed: {DETAIL}"),
		new WorldMapDisplayRule("RebellionStarted", @"^玩家家族已带领封地脱离(?<KINGDOM>.+)并发动叛乱，(?<HERO>.+)随后继续执行(?<ACTION>攻击|烧掠|围攻)(?<TARGET>.+)的命令。$", "The player's clan retained its fiefs and rebelled against {KINGDOM}; {HERO} then continued {ACTION} {TARGET}."),
		new WorldMapDisplayRule("TroopXpInputs", @"^兵种 XP 调整参数无效：(?<TROOP>.*)$", "Troop XP adjustment parameters are invalid: {TROOP}"),
		new WorldMapDisplayRule("TroopXpWrite", @"^兵种 XP 写入校验失败：(?<TROOP>.*) context=(?<CONTEXT>.*)$", "Troop XP write verification failed: {TROOP}; context={CONTEXT}"),
		new WorldMapDisplayRule("ArmyMaintained", @"^(?<ACTOR>.+)消耗(?<COST>\d+)影响力强行维持军团，继续(?<ACTION>烧掠|围攻)(?<SETTLEMENT>.+)（军团长影响力现为(?<INFLUENCE>[-\d.]+)）。$", "{ACTOR} spent {COST} influence to maintain the army and continue {ACTION} {SETTLEMENT} (leader influence: {INFLUENCE})."),
		new WorldMapDisplayRule("MergeCompleted", @"^(?<HERO>.+)已经与玩家部队会合，并转入(?<TROOPS>\d+)名成员、(?<PRISONERS>\d+)名俘虏及全部随队物资；临时远征记录已清除。$", "{HERO} met the player's party and transferred {TROOPS} members, {PRISONERS} prisoners, and all supplies; the temporary expedition record was cleared."),
		new WorldMapDisplayRule("GuestMergeCompleted", @"^(?<HERO>.+)已经与玩家部队会合，并转入(?<TROOPS>\d+)名成员、(?<PRISONERS>\d+)名俘虏及全部随队物资；原独立部队已解散。 (?<GUEST>.+)保留原家族、职业和同伴身份，以外族客军身份随玩家主队行动。$", "{HERO} met the player's party and transferred {TROOPS} members, {PRISONERS} prisoners, and all supplies; the original party was disbanded. {GUEST} accompanies the player as a foreign-clan guest, retaining clan, occupation, and companion status."),
		new WorldMapDisplayRule("GovernorReturned", @"^(?<HERO>.+)的远征队已返抵(?<SETTLEMENT>.+)，普通兵、俘虏与物资已经完成安置，临时部队已回收。(?<SHIPS>\d+艘随队船只已转交该定居点。)?(?<RESTORATION>该人物已恢复原总督职务。|未驱逐现任总督。)$", "{HERO}'s expedition returned to {SETTLEMENT}; troops, prisoners, and supplies were placed and the temporary party was removed.{SHIPS} {RESTORATION}"),
		new WorldMapDisplayRule("ForeignGuestWarDeparture", @"^(?<HERO>.+)所属势力已与玩家开战，因此结束外族客军随队状态并离开玩家主队，返回(?<DESTINATION>.+)；此前已并入玩家主队的普通兵员和物资不自动撤回。$", "{HERO}'s faction is at war with the player, so the foreign-clan guest left the player's party and returned to {DESTINATION}; troops and supplies previously merged into the player's party were not automatically withdrawn."),
		new WorldMapDisplayRule("ForeignGuestPartyReleased", @"^(?<HERO>.+)所属势力已与玩家开战，因此其外族客军分队已脱离玩家指挥并回归原家族的原版AI；该分队现有兵员和物资不自动转回玩家主队。$", "{HERO}'s faction is at war with the player, so the foreign-clan guest party left player control and returned to the original clan's native AI; its troops and supplies were not automatically transferred to the player's party."),
		new WorldMapDisplayRule("HoldUntilCompleted", @"^(?<ACTOR>.+)已经完成在(?<SETTLEMENT>.+)停留至指定期限的命令。$", "{ACTOR} completed the order to stay in {SETTLEMENT} until the specified date."),
		new WorldMapDisplayRule("HoldDaysCompleted", @"^(?<ACTOR>.+)已经完成在(?<SETTLEMENT>.+?)停留(?<DAYS>\d+)天的命令。$", "{ACTOR} completed the order to stay in {SETTLEMENT} for {DAYS} days."),
		new WorldMapDisplayRule("PatrolCompleted", @"^(?<ACTOR>.+)已经完成在(?<SETTLEMENT>.+?)附近巡逻(?<DAYS>\d+)天的命令。$", "{ACTOR} completed the order to patrol near {SETTLEMENT} for {DAYS} days."),
		new WorldMapDisplayRule("FollowCompleted", @"^(?<ACTOR>.+)已经完成跟随(?<TARGET>.+?)(?<DAYS>\d+)天的命令。$", "{ACTOR} completed the order to follow {TARGET} for {DAYS} days."),
		new WorldMapDisplayRule("PartyCreationCancelled", @"^(?<HERO>.+)的同伴部队创建已取消，后续大地图命令未执行。$", "Companion party creation for {HERO} was cancelled; follow-up world-map commands were not executed."),
		new WorldMapDisplayRule("PartyCreated", @"^(?<HERO>.+)已经创建同伴部队，并接收了(?<TROOPS>\d+)名士兵(?:、(?<PRISONERS>\d+)名俘虏)?。$", "{HERO} created a companion party and received {TROOPS} troops and {PRISONERS} prisoners."),
		new WorldMapDisplayRule("QueueSafeExit", @"^(?<ACTOR>.+)的大地图命令队列已经结束；当前原版战斗结算后将安全退出攻城并回归原版行动状态。$", "{ACTOR}'s world-map command queue ended; after the current battle resolves, the party will safely leave the siege and resume native behavior."),
		new WorldMapDisplayRule("TimeoutCommand", @"^(?<ACTOR>.+)的大地图命令时限已到，已跳过当前命令。$", "{ACTOR}'s world-map command timed out; the current command was skipped."),
		new WorldMapDisplayRule("TimeoutTravel", @"^(?<ACTOR>.+)未能在时限内抵达(?<SETTLEMENT>.+)，已跳过当前前往命令。$", "{ACTOR} could not reach {SETTLEMENT} before the time limit; the travel command was skipped."),
		new WorldMapDisplayRule("TimeoutPatrol", @"^(?<ACTOR>.+)未能在时限内抵达(?<SETTLEMENT>.+)附近，已跳过当前巡逻命令。$", "{ACTOR} could not reach the area near {SETTLEMENT} before the time limit; the patrol command was skipped."),
		new WorldMapDisplayRule("TimeoutFollow", @"^(?<ACTOR>.+)未能在时限内追上(?<TARGET>.+)，已跳过当前跟随命令。$", "{ACTOR} could not catch up with {TARGET} before the time limit; the follow command was skipped."),
		new WorldMapDisplayRule("TimeoutMerge", @"^(?<ACTOR>.+)未能在时限内与玩家部队会合，已跳过当前回队命令。$", "{ACTOR} could not meet the player's party before the time limit; the return command was skipped."),
		new WorldMapDisplayRule("RaidCompleted", @"^(?<HERO>.+)已经完成对(?<SETTLEMENT>.+)的烧掠。$", "{HERO} completed the raid on {SETTLEMENT}."),
		new WorldMapDisplayRule("PartyDefeated", @"^(?<PARTY>[^：]+)的部队被击败。?$", "{PARTY}'s party was defeated."),
		new WorldMapDisplayRule("PartyRepelled", @"^(?<PARTY>[^：]+)的部队被击退。?$", "{PARTY}'s party was repelled."),
		new WorldMapDisplayRule("SettlementCaptured", @"^(?<SETTLEMENT>[^：]+)已经被攻下。?$", "{SETTLEMENT} was captured."),
		new WorldMapDisplayRule("GovernorNoReturnTarget", @"^(?<HERO>.+)的总督远征队没有可安全返还的本家族城市或城堡，已保留当前部队并交还原版 AI 管理。$", "{HERO}'s governor expedition has no safe clan town or castle to return to; the party was preserved and returned to native AI."),
		new WorldMapDisplayRule("GovernorReturnUnsafe", @"^(?<HERO>.+)的总督远征队当前无法安全完成返城资产安置，已保留当前部队并交还原版 AI 管理。$", "{HERO}'s governor expedition could not safely transfer assets on return; the party was preserved and returned to native AI."),
		new WorldMapDisplayRule("HideoutMissingResult", @"^原版战斗已结束，但没有可确认的清剿结算；未重复发动攻击。?$", "The native battle ended without a confirmed hideout result; the attack was not repeated."),
		new WorldMapDisplayRule("HideoutQuestBusy", @"^目标被剧情或任务占用，未发动清剿。?$", "The target is reserved by a story or quest; the hideout attack was not started."),
		new WorldMapDisplayRule("HideoutUnavailable", @"^目标不可用或安全结算接口未就绪，未发动清剿。?$", "The target is unavailable or safe battle resolution is not ready; the hideout attack was not started."),
		new WorldMapDisplayRule("HideoutAlreadyEmpty", @"^目标已经没有驻扎土匪，未将他人清剿记为本次成功。?$", "The target no longer has bandits; another party's clearance was not counted as this command's success."),
		new WorldMapDisplayRule("HideoutActorIneligible", @"^执行部队不能与目标土匪交战，未宣战或强制删除目标。?$", "The executing party cannot fight the target bandits; no war was declared and the target was not forcibly removed."),
		new WorldMapDisplayRule("HideoutBattleNotCreated", @"^原版清剿战斗未能建立。?$", "The native hideout battle could not be created."),
		new WorldMapDisplayRule("HideoutCleared", @"^原版战斗获胜，驻扎土匪已被清除。?$", "The native battle was won and the resident bandits were cleared."),
		new WorldMapDisplayRule("HideoutDefeated", @"^原版战斗判定清剿部队战败。?$", "The native battle determined that the clearing party was defeated."),
		new WorldMapDisplayRule("HideoutIncomplete", @"^未取得明确清除结果，或目标仍有驻扎土匪。?$", "No definitive clearance was confirmed, or the target still has resident bandits."),
		new WorldMapDisplayRule("TimeoutNoResult", @"^命令时限已到，未能取得明确结果。?$", "The command timed out without a definitive result."),
		new WorldMapDisplayRule("PartyCreationInvalid", @"^新任务队伍未能正确建立。?$", "The new task party was not created correctly."),
		new WorldMapDisplayRule("GovernorResignFailed", @"^未能安全卸任现任总督。?$", "The current governor could not safely step down."),
		new WorldMapDisplayRule("GovernorPartyInvalid", @"^原版领主队未能以该总督和家族正确建立。?$", "The native lord party was not correctly created for this governor and clan."),
		new WorldMapDisplayRule("GovernorRosterMissing", @"^新领主队名册不可用。?$", "The new lord party roster is unavailable."),
		new WorldMapDisplayRule("GovernorUnexpectedHero", @"^新领主队出现了意外 Hero，已停止建队。?$", "An unexpected hero appeared in the new lord party; creation was stopped."),
		new WorldMapDisplayRule("GovernorTemplateRemaining", @"^原版模板兵、俘虏或模板船只未能完全清除。?$", "Native template troops, prisoners, or ships could not be fully removed."),
		new WorldMapDisplayRule("GovernorTemplateShips", @"^原版领主队模板船只清理失败。?$", "Native lord party template ships could not be removed."),
		new WorldMapDisplayRule("GovernorTransferInputs", @"^驻军、远征队或抽调计划不可用。?$", "The garrison, expedition party, or troop transfer plan is unavailable."),
		new WorldMapDisplayRule("GovernorTransferTroop", @"^驻军抽调计划包含无效兵种。?$", "The garrison transfer plan contains an invalid troop type."),
		new WorldMapDisplayRule("GovernorTroopChanged", @"^驻军兵种状态在转移前已经变化：(?<TROOP>.*)$", "Garrison troop state changed before transfer: {TROOP}"),
		new WorldMapDisplayRule("GovernorTroopDeduction", @"^驻军健康兵员扣除校验失败：(?<TROOP>.*)$", "Garrison troop deduction verification failed: {TROOP}"),
		new WorldMapDisplayRule("GovernorXpDeduction", @"^驻军 XP 扣除结果异常：(?<TROOP>.*)$", "Garrison XP deduction was inconsistent: {TROOP}"),
		new WorldMapDisplayRule("GovernorTroopWrite", @"^远征队兵员写入校验失败：(?<TROOP>.*)$", "Expedition troop insertion verification failed: {TROOP}"),
		new WorldMapDisplayRule("GovernorConservation", @"^驻军与远征队兵员或 XP 守恒校验失败：(?<TROOP>.*)$", "Garrison and expedition troop or XP conservation verification failed: {TROOP}"),
		new WorldMapDisplayRule("GovernorTransferFailure", @"^总督远征队兵员转移失败 troop=(?<TROOP>.*) step=(?<STEP>.*)$", "Governor expedition troop transfer failed: troop={TROOP} step={STEP}"),
	};

	private static bool ContainsChineseDisplayText(string text)
	{
		foreach (char ch in text ?? string.Empty)
		{
			if (ch >= '\u3400' && ch <= '\u9fff') return true;
		}
		return false;
	}

	private static bool TryProjectAdditionalWorldMapMessage(string message, out string display)
	{
		foreach (WorldMapDisplayRule rule in AdditionalWorldMapDisplayRules)
		{
			Match match = rule.Pattern.Match(message);
			if (!match.Success) continue;
			var values = new (string Name, object Value)[rule.Variables.Length - 1];
			for (int i = 1; i < rule.Variables.Length; i++)
			{
				string key = rule.Variables[i];
				string value = match.Groups[key].Value;
				if (key == "PRISONERS" && value.Length == 0) value = "0";
				if (key == "ACTION") value = value == "烧掠"
					? WorldMapUiText.Result("AF_WM_Review_Raiding", "raiding")
					: value == "围攻" ? WorldMapUiText.Result("AF_WM_Review_Besieging", "besieging")
					: WorldMapUiText.Result("AF_WM_Review_Attacking", "attacking");
				if (key == "DETAIL") value = CommandMessageForDisplay(value);
				if (key == "DESTINATION" && value == "原家族势力范围")
					value = WorldMapUiText.Result("AF_WM_Review_ClanTerritory", "the original clan's territory");
				if (key == "RESTORATION") value = value == "该人物已恢复原总督职务。"
					? WorldMapUiText.Result("AF_WM_Review_GovernorRestored", "The original governorship was restored.")
					: WorldMapUiText.Result("AF_WM_Review_GovernorRetained", "The current governor was not displaced.");
				if (key == "SHIPS" && value.Length > 0)
					value = WorldMapUiText.Result("AF_WM_Review_ShipsReturned", " {COUNT} accompanying ships were transferred to the settlement.",
						("COUNT", value.Substring(0, value.IndexOf('艘'))));
				values[i - 1] = (key, value);
			}
			display = WorldMapUiText.Result(rule.Id, rule.Fallback, values);
			return true;
		}
		display = message;
		return false;
	}
}
