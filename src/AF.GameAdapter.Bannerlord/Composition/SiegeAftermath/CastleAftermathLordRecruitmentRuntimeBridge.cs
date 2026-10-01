using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal readonly struct CastleAftermathLordPoliticalFacts
{
	internal CastleAftermathLordPoliticalFacts(bool speakerIsClanLeader, bool playerHasKingdom, bool playerRulesKingdom)
	{
		SpeakerIsClanLeader = speakerIsClanLeader;
		PlayerHasKingdom = playerHasKingdom;
		PlayerRulesKingdom = playerRulesKingdom;
	}

	internal bool SpeakerIsClanLeader { get; }
	internal bool PlayerHasKingdom { get; }
	internal bool PlayerRulesKingdom { get; }
}

internal readonly struct CastleAftermathLordRecruitmentApplyResult
{
	internal CastleAftermathLordRecruitmentApplyResult(
		bool succeeded,
		SiegeCastleLordRecruitmentBranch branch,
		string statusText,
		bool targetReleased)
	{
		Succeeded = succeeded;
		Branch = branch;
		StatusText = statusText ?? string.Empty;
		TargetReleased = targetReleased;
	}

	internal bool Succeeded { get; }
	internal SiegeCastleLordRecruitmentBranch Branch { get; }
	internal string StatusText { get; }
	internal bool TargetReleased { get; }
}

/// <summary>
/// Bannerlord-only adapter for castle lord recruitment. It executes a branch already
/// selected by the standalone GCCZ profile and owns a courier-free 1-2 day letter queue.
/// </summary>
internal static class CastleAftermathLordRecruitmentRuntimeBridge
{
	private const uint SuccessColor = 0xFFB6F7A8u;
	private static Dictionary<string, int> _letterDueDayBySender = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, string> _letterRecipientBySender = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, string> _letterBodyBySender = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, string> _letterCastleBySender = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	internal static void SyncData(IDataStore dataStore)
	{
		dataStore?.SyncData("_gcczCastleLordIntroLetterDueDayBySender_v1", ref _letterDueDayBySender);
		dataStore?.SyncData("_gcczCastleLordIntroLetterRecipientBySender_v1", ref _letterRecipientBySender);
		dataStore?.SyncData("_gcczCastleLordIntroLetterBodyBySender_v1", ref _letterBodyBySender);
		dataStore?.SyncData("_gcczCastleLordIntroLetterCastleBySender_v1", ref _letterCastleBySender);
		_letterDueDayBySender ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_letterRecipientBySender ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_letterBodyBySender ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_letterCastleBySender ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	internal static void ClearForNewGame()
	{
		_letterDueDayBySender.Clear();
		_letterRecipientBySender.Clear();
		_letterBodyBySender.Clear();
		_letterCastleBySender.Clear();
	}

	internal static CastleAftermathLordPoliticalFacts GetPoliticalFacts(Hero speaker)
	{
		Clan playerClan = Clan.PlayerClan;
		Kingdom playerKingdom = playerClan?.Kingdom;
		bool rules = playerKingdom != null
			&& (playerKingdom.Leader == Hero.MainHero || playerKingdom.RulingClan == playerClan);
		return new CastleAftermathLordPoliticalFacts(
			speaker?.Clan?.Leader == speaker,
			playerKingdom != null,
			rules);
	}

	internal static CastleAftermathLordRecruitmentApplyResult Apply(
		Hero capturedLord,
		SiegeCastleLordRecruitmentBranch branch,
		Settlement castle,
		string aiResponseText)
	{
		if (capturedLord == null || !capturedLord.IsAlive || capturedLord == Hero.MainHero)
		{
			return Failed(branch, "目标领主无效或已经无法处置。");
		}
		if (!CastleAftermathRuntimeBridge.ContainsSelectedLord(capturedLord))
		{
			return Failed(branch, "该领主不在本次带入城堡处置的俘虏名单中。");
		}

		try
		{
			switch (branch)
			{
				case SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom:
					return JoinClanToPlayerKingdom(capturedLord, castle);
				case SiegeCastleLordRecruitmentBranch.ClanLeaderRequestRulerAudience:
					return RecordDeferredPoliticalIntent(
						capturedLord,
						branch,
						castle,
						"请求玩家将自己带去面见玩家所属王国的统治者；家族归属和俘虏状态暂未改变。");
				case SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim:
					return RecordDeferredPoliticalIntent(
						capturedLord,
						branch,
						castle,
						"表达了支持玩家称王的政治意向；不会在玩家尚无王国时凭空创建国家或转移家族。");
				case SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter:
					return QueueClanLeaderIntroduction(capturedLord, castle, aiResponseText);
				case SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion:
					return JoinPlayerAsCompanion(capturedLord, castle);
				default:
					return Failed(branch, "领主收编分支尚未明确。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Apply castle lord recruitment branch failed. Hero="
				+ (capturedLord.StringId ?? "N/A") + ", Branch=" + branch + ", Error=" + ex);
			return Failed(branch, "执行领主收编分支时发生异常：" + ex.Message);
		}
	}

	internal static void ProcessDueLetters()
	{
		if (_letterDueDayBySender.Count == 0)
		{
			return;
		}
		int day = CurrentDay();
		foreach (string senderId in new List<string>(_letterDueDayBySender.Keys))
		{
			if (!_letterDueDayBySender.TryGetValue(senderId, out int dueDay) || day < dueDay)
			{
				continue;
			}

			try
			{
				_letterRecipientBySender.TryGetValue(senderId, out string recipientId);
				_letterBodyBySender.TryGetValue(senderId, out string body);
				_letterCastleBySender.TryGetValue(senderId, out string castleName);
				Hero sender = FindHero(senderId);
				Hero recipient = FindHero(recipientId);
				if (sender != null && recipient != null && recipient.IsAlive)
				{
					string senderName = sender.Name?.ToString() ?? "一名家族成员";
					string recipientName = recipient.Name?.ToString() ?? "族长";
					string location = string.IsNullOrWhiteSpace(castleName) ? "一座刚陷落的城堡" : castleName;
					string stableKey = "gccz_castle_lord_intro_letter:" + senderId + ":" + dueDay;
					MyBehavior.RecordNpcActionForExternal(
						sender,
						"你履行了在 " + location + " 被俘后作出的承诺，写信向 " + recipientName + " 引见玩家。信件内容：" + body,
						stableKey + ":sender",
						"gccz_castle_lord_intro_letter",
						isMajor: true,
						isRecent: true,
						targetHero: Hero.MainHero,
						locationText: location,
						allowNonLordHero: false,
						won: true);
					MyBehavior.RecordNpcActionForExternal(
						recipient,
						"你收到了 " + senderName + " 的引见信。信中说明玩家在 " + location + " 的招揽，并附有如下内容：" + body,
						stableKey + ":recipient",
						"gccz_castle_lord_intro_letter_received",
						isMajor: true,
						isRecent: true,
						targetHero: Hero.MainHero,
						locationText: location,
						allowNonLordHero: false,
						won: true);
					MyBehavior.RecordPlayerActionForExternal(
						"你收到了消息：" + senderName + " 已按约向族长 " + recipientName + " 写信引见你。",
						stableKey + ":player",
						"gccz_castle_lord_intro_letter_delivered",
						isMajor: true,
						targetHero: recipient,
						locationText: location,
						won: true);
					InformationManager.DisplayMessage(new InformationMessage(
						"【城堡处置来信】" + senderName + " 的引见信已在 " + recipientName + " 处送达；没有生成信使部队。",
						Color.FromUint(SuccessColor)));
					Logger.Log("CastleAftermath", "Delivered castle lord introduction letter. Sender=" + senderId
						+ ", Recipient=" + recipientId + ", DueDay=" + dueDay);
				}
				else
				{
					Logger.Log("CastleAftermath", "Dropped undeliverable castle lord introduction letter. Sender="
						+ (senderId ?? "N/A") + ", Recipient=" + (recipientId ?? "N/A"));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("CastleAftermath", "Deliver castle lord introduction letter failed. Sender="
					+ (senderId ?? "N/A") + ", DueDay=" + dueDay + ", Error=" + ex);
			}
			finally
			{
				RemoveLetter(senderId);
			}
		}
	}

	private static CastleAftermathLordRecruitmentApplyResult JoinClanToPlayerKingdom(Hero capturedLord, Settlement castle)
	{
		string status = "AF奖励系统当前不可用。";
		if (RewardSystemBehavior.Instance == null
			|| !RewardSystemBehavior.Instance.TryApplyClanLeaderJoinPlayerKingdomForExternal(capturedLord, out status))
		{
			return Failed(SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom, status);
		}
		bool targetReleased = TryReleaseAndRemoveFromScene(capturedLord, "clan_join_player_kingdom");
		RecordPoliticalMemory(capturedLord, castle, SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom, status);
		if (!targetReleased)
		{
			status += " 政治转投已经完成，但俘虏或场景清理未完全完成，详情已写入日志。";
		}
		return Succeeded(SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom, status, targetReleased);
	}

	private static CastleAftermathLordRecruitmentApplyResult JoinPlayerAsCompanion(Hero capturedLord, Settlement castle)
	{
		PartyBase capturer = capturedLord.PartyBelongedToAsPrisoner;
		if (capturer != null)
		{
			EndCaptivityAction.ApplyByReleasedByChoice(capturedLord, Hero.MainHero);
		}
		string status = "AF奖励系统当前不可用。";
		if (RewardSystemBehavior.Instance == null
			|| !RewardSystemBehavior.Instance.TryApplyHeroJoinPlayerPartyForExternal(capturedLord, out status))
		{
			if (capturer != null && capturedLord.IsAlive && capturedLord.PartyBelongedToAsPrisoner == null)
			{
				try { TakePrisonerAction.Apply(capturer, capturedLord); } catch { }
			}
			return Failed(SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion, status);
		}
		bool targetReleased = TryReleaseAndRemoveFromScene(capturedLord, "join_player_as_companion");
		RecordPoliticalMemory(capturedLord, castle, SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion, status);
		if (!targetReleased)
		{
			status += " 同伴加入已经完成，但场景清理未完全完成，详情已写入日志。";
		}
		return Succeeded(SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion, status, targetReleased);
	}

	private static CastleAftermathLordRecruitmentApplyResult RecordDeferredPoliticalIntent(
		Hero capturedLord,
		SiegeCastleLordRecruitmentBranch branch,
		Settlement castle,
		string status)
	{
		RecordPoliticalMemory(capturedLord, castle, branch, status);
		return Succeeded(branch, status, targetReleased: false);
	}

	private static CastleAftermathLordRecruitmentApplyResult QueueClanLeaderIntroduction(
		Hero capturedLord,
		Settlement castle,
		string aiResponseText)
	{
		Hero clanLeader = capturedLord.Clan?.Leader;
		if (clanLeader == null || clanLeader == capturedLord || !clanLeader.IsAlive)
		{
			return Failed(SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter, "未找到可接收引见信的在世族长。");
		}
		string senderId = capturedLord.StringId ?? string.Empty;
		string recipientId = clanLeader.StringId ?? string.Empty;
		if (senderId.Length == 0 || recipientId.Length == 0)
		{
			return Failed(SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter, "领主或族长缺少稳定角色标识。");
		}
		int delayDays = MBRandom.RandomInt(2) + 1;
		int dueDay = CurrentDay() + delayDays;
		string body = BuildAiAuthoredLetterBody(capturedLord, clanLeader, castle, aiResponseText);
		_letterDueDayBySender[senderId] = dueDay;
		_letterRecipientBySender[senderId] = recipientId;
		_letterBodyBySender[senderId] = body;
		_letterCastleBySender[senderId] = castle?.Name?.ToString() ?? string.Empty;
		string status = "引见信已写好，将在 " + delayDays + " 天后直接送达族长 "
			+ (clanLeader.Name?.ToString() ?? "未知族长") + "；不会生成AF信使部队，当前领主仍保持俘虏身份。";
		RecordPoliticalMemory(capturedLord, castle, SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter, status);
		Logger.Log("CastleAftermath", "Queued castle lord introduction letter. Sender=" + senderId
			+ ", Recipient=" + recipientId + ", DueDay=" + dueDay);
		return Succeeded(SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter, status, targetReleased: false);
	}

	private static bool TryReleaseAndRemoveFromScene(Hero capturedLord, string source)
	{
		bool succeeded = true;
		try
		{
			if (capturedLord?.PartyBelongedToAsPrisoner != null)
			{
				EndCaptivityAction.ApplyByReleasedByChoice(capturedLord, Hero.MainHero);
			}
		}
		catch (Exception ex)
		{
			succeeded = false;
			Logger.Log("CastleAftermath", "Release recruited castle lord failed. Hero="
				+ (capturedLord?.StringId ?? "N/A") + ", Source=" + source + ", Error=" + ex);
		}
		try
		{
			CastleAftermathRuntimeBridge.ResolveLordPrisoner(capturedLord, source);
		}
		catch (Exception ex)
		{
			succeeded = false;
			Logger.Log("CastleAftermath", "Remove recruited castle lord from scene failed. Hero="
				+ (capturedLord?.StringId ?? "N/A") + ", Source=" + source + ", Error=" + ex);
		}
		return succeeded;
	}

	private static void RecordPoliticalMemory(
		Hero capturedLord,
		Settlement castle,
		SiegeCastleLordRecruitmentBranch branch,
		string status)
	{
		try
		{
			string lordName = capturedLord?.Name?.ToString() ?? "该被俘领主";
			string castleName = castle?.Name?.ToString() ?? "刚陷落的城堡";
			string stableKey = "gccz_castle_lord_recruit:" + (capturedLord?.StringId ?? "unknown")
				+ ":" + CurrentDay() + ":" + branch;
			string npcText = "你在 " + castleName + " 战败被俘后，与玩家达成了领主收编政治安排："
				+ SiegeCastleLordRecruitmentBranchProfile.Describe(branch) + "。" + (status ?? string.Empty);
			MyBehavior.RecordNpcActionForExternal(
				capturedLord,
				npcText,
				stableKey + ":npc",
				"gccz_castle_lord_recruit",
				isMajor: true,
				isRecent: true,
				targetHero: Hero.MainHero,
				settlement: castle,
				locationText: castleName,
				allowNonLordHero: false,
				won: false);
			MyBehavior.RecordPlayerActionForExternal(
				"你在 " + castleName + " 与 " + lordName + " 达成了领主收编政治安排："
					+ SiegeCastleLordRecruitmentBranchProfile.Describe(branch) + "。",
				stableKey + ":player",
				"gccz_castle_lord_recruit",
				isMajor: true,
				targetHero: capturedLord,
				settlement: castle,
				locationText: castleName,
				won: true);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Record castle lord political memory failed. Hero="
				+ (capturedLord?.StringId ?? "N/A") + ", Branch=" + branch + ", Error=" + ex);
		}
	}

	private static string BuildAiAuthoredLetterBody(Hero sender, Hero recipient, Settlement castle, string aiResponseText)
	{
		string aiText = (aiResponseText ?? string.Empty).Trim();
		if (aiText.Length > 1200)
		{
			aiText = aiText.Substring(0, 1200);
		}
		if (aiText.Length == 0)
		{
			aiText = "我愿为玩家引见，请族长审慎考虑与其会面。";
		}
		return "致 " + (recipient?.Name?.ToString() ?? "族长") + "：我在 "
			+ (castle?.Name?.ToString() ?? "刚陷落的城堡") + " 战败被俘后与玩家交谈。"
			+ aiText + " ——" + (sender?.Name?.ToString() ?? "家族成员");
	}

	private static Hero FindHero(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		return Hero.Find(id) ?? Hero.FindFirst(hero => hero != null
			&& string.Equals(hero.StringId, id, StringComparison.OrdinalIgnoreCase));
	}

	private static void RemoveLetter(string senderId)
	{
		_letterDueDayBySender.Remove(senderId);
		_letterRecipientBySender.Remove(senderId);
		_letterBodyBySender.Remove(senderId);
		_letterCastleBySender.Remove(senderId);
	}

	private static int CurrentDay()
	{
		try { return (int)Math.Floor(CampaignTime.Now.ToDays); }
		catch { return 0; }
	}

	private static CastleAftermathLordRecruitmentApplyResult Succeeded(
		SiegeCastleLordRecruitmentBranch branch,
		string status,
		bool targetReleased)
		=> new CastleAftermathLordRecruitmentApplyResult(true, branch, status, targetReleased);

	private static CastleAftermathLordRecruitmentApplyResult Failed(
		SiegeCastleLordRecruitmentBranch branch,
		string status)
		=> new CastleAftermathLordRecruitmentApplyResult(false, branch, status, targetReleased: false);
}
