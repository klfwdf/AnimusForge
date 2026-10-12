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
			return Failed(branch, CastleAftermathCompatibilityTexts.Raw("status_invalid"));
		}
		if (!CastleAftermathRuntimeBridge.ContainsSelectedLord(capturedLord))
		{
			return Failed(branch, CastleAftermathCompatibilityTexts.Raw("status_not_selected"));
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
						CastleAftermathCompatibilityTexts.Raw("status_audience"));
				case SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim:
					return RecordDeferredPoliticalIntent(
						capturedLord,
						branch,
						castle,
						CastleAftermathCompatibilityTexts.Raw("status_claim"));
				case SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter:
					return QueueClanLeaderIntroduction(capturedLord, castle, aiResponseText);
				case SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion:
					return JoinPlayerAsCompanion(capturedLord, castle);
				default:
					return Failed(branch, CastleAftermathCompatibilityTexts.Raw("status_unknown"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Apply castle lord recruitment branch failed. Hero="
				+ (capturedLord.StringId ?? "N/A") + ", Branch=" + branch + ", Error=" + ex);
			return Failed(branch, CastleAftermathCompatibilityTexts.Raw("exception_prefix") + ex.Message);
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
					string senderName = sender.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("family_member");
					string recipientName = recipient.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("clan_leader");
					string location = string.IsNullOrWhiteSpace(castleName) ? CastleAftermathCompatibilityTexts.Raw("fallen_castle") : castleName;
					string stableKey = "gccz_castle_lord_intro_letter:" + senderId + ":" + dueDay;
					MyBehavior.RecordNpcActionForExternal(
						sender,
						CastleAftermathCompatibilityTexts.Raw("sender_fact_prefix") + location + CastleAftermathCompatibilityTexts.Raw("sender_fact_middle") + recipientName + CastleAftermathCompatibilityTexts.Raw("sender_fact_suffix") + body,
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
						CastleAftermathCompatibilityTexts.Raw("recipient_fact_prefix") + senderName + CastleAftermathCompatibilityTexts.Raw("recipient_fact_middle") + location + CastleAftermathCompatibilityTexts.Raw("recipient_fact_suffix") + body,
						stableKey + ":recipient",
						"gccz_castle_lord_intro_letter_received",
						isMajor: true,
						isRecent: true,
						targetHero: Hero.MainHero,
						locationText: location,
						allowNonLordHero: false,
						won: true);
					MyBehavior.RecordPlayerActionForExternal(
						CastleAftermathCompatibilityTexts.Raw("player_fact_prefix") + senderName + CastleAftermathCompatibilityTexts.Raw("player_fact_middle") + recipientName + CastleAftermathCompatibilityTexts.Raw("player_fact_suffix"),
						stableKey + ":player",
						"gccz_castle_lord_intro_letter_delivered",
						isMajor: true,
						targetHero: recipient,
						locationText: location,
						won: true);
					InformationManager.DisplayMessage(new InformationMessage(
						CastleAftermathUiText.LetterDelivered(sender.Name?.ToString(), recipient.Name?.ToString()),
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
		string status = CastleAftermathCompatibilityTexts.Raw("reward_unavailable");
		if (RewardSystemBehavior.Instance == null
			|| !RewardSystemBehavior.Instance.TryApplyClanLeaderJoinPlayerKingdomForExternal(capturedLord, out status))
		{
			return Failed(SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom, status);
		}
		bool targetReleased = TryReleaseAndRemoveFromScene(capturedLord, "clan_join_player_kingdom");
		RecordPoliticalMemory(capturedLord, castle, SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom, status);
		if (!targetReleased)
		{
			status += CastleAftermathCompatibilityTexts.Raw("kingdom_cleanup_suffix");
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
		string status = CastleAftermathCompatibilityTexts.Raw("reward_unavailable");
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
			status += CastleAftermathCompatibilityTexts.Raw("companion_cleanup_suffix");
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
			return Failed(SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter, CastleAftermathCompatibilityTexts.Raw("no_leader"));
		}
		string senderId = capturedLord.StringId ?? string.Empty;
		string recipientId = clanLeader.StringId ?? string.Empty;
		if (senderId.Length == 0 || recipientId.Length == 0)
		{
			return Failed(SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter, CastleAftermathCompatibilityTexts.Raw("no_id"));
		}
		int delayDays = MBRandom.RandomInt(2) + 1;
		int dueDay = CurrentDay() + delayDays;
		string body = BuildAiAuthoredLetterBody(capturedLord, clanLeader, castle, aiResponseText);
		_letterDueDayBySender[senderId] = dueDay;
		_letterRecipientBySender[senderId] = recipientId;
		_letterBodyBySender[senderId] = body;
		_letterCastleBySender[senderId] = castle?.Name?.ToString() ?? string.Empty;
		string status = CastleAftermathCompatibilityTexts.Raw("letter_ready_prefix") + delayDays + CastleAftermathCompatibilityTexts.Raw("letter_ready_middle")
			+ (clanLeader.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("unknown_clan_leader")) + CastleAftermathCompatibilityTexts.Raw("letter_ready_suffix");
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
			string lordName = capturedLord?.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("captured_lord");
			string castleName = castle?.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("fallen_castle_short");
			string stableKey = "gccz_castle_lord_recruit:" + (capturedLord?.StringId ?? "unknown")
				+ ":" + CurrentDay() + ":" + branch;
			string npcText = CastleAftermathCompatibilityTexts.Raw("political_fact_prefix") + castleName + CastleAftermathCompatibilityTexts.Raw("political_fact_middle")
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
				CastleAftermathCompatibilityTexts.Raw("political_fact_prefix") + castleName + CastleAftermathCompatibilityTexts.Raw("political_fact_between") + lordName + CastleAftermathCompatibilityTexts.Raw("political_fact_suffix")
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
			aiText = CastleAftermathCompatibilityTexts.Raw("letter_ai_default");
		}
		return CastleAftermathCompatibilityTexts.Raw("letter_prefix") + (recipient?.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("clan_leader")) + CastleAftermathCompatibilityTexts.Raw("letter_colon")
			+ (castle?.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("fallen_castle_short")) + CastleAftermathCompatibilityTexts.Raw("letter_captive_middle")
			+ aiText + " ——" + (sender?.Name?.ToString() ?? CastleAftermathCompatibilityTexts.Raw("family_member_short"));
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
