using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private void CommitGeneratedReplyActionsAtRecipientCore(CourierSession session, Hero recipient, bool persistHistory = true)
	{
		if (session == null || session.PostprocessConsumed)
		{
			return;
		}
		if (!session.DeliveryApplied)
		{
			return;
		}
		string text = session.ReplyPostprocessedText ?? session.ReplyText ?? "";
		if (recipient == null || recipient.IsDead)
		{
			session.PostprocessConsumed = true;
			session.ReplyPostprocessedText = StripCourierActionTags(text);
			Log("postprocess skipped recipient invalid session=" + session.Id);
			return;
		}
		try
		{
			TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure);
			if (!string.IsNullOrWhiteSpace(proposalFailure))
			{
				Log("kingdom agenda custom policy not queued session=" + session.Id + " reason=" + proposalFailure);
			}
		}
		catch (Exception ex)
		{
			Log("apply kingdom agenda custom policy tag failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			VoteDealBehavior.ProcessAgendaTagsDispatch(recipient, ref text);
			DiplomacyBehavior.ProcessDiplomacyTagsDispatch(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply vote deal tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply world map tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			if (TeamModuleServices.Gathering.TryApplyNobleGatheringTagsForExternal(recipient, ref text, out var nobleFacts, out var nobleNotifications))
			{
				foreach (string fact in nobleFacts ?? new List<string>())
				{
					MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
				}
				foreach (string note in nobleNotifications ?? new List<string>())
				{
					if (!string.IsNullOrWhiteSpace(note))
					{
						InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log("apply noble gathering tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			if (MyBehavior.TryApplyPartyTransferTagsForExternal(recipient, recipient.CharacterObject, -1, ref text, out var facts, out var notifications))
			{
				foreach (string fact in facts ?? new List<string>())
				{
					MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
				}
				foreach (string note in notifications ?? new List<string>())
				{
					InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
				}
			}
		}
		catch (Exception ex)
		{
			Log("apply party transfer tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			bool rewardBeforeHasVassalage = ContainsVassalageActionTag(text);
			bool rewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTag(text);
			Log("ApplyRewardTags start chain=courier session=" + session.Id + " containsVASSALAGE=" + rewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex);
			RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(text)
				? CreateCourierRpItemIntroductionContext(session, recipient, text)
				: null;
			RewardSystemBehavior.Instance?.ApplyRewardTags(recipient, Hero.MainHero, ref text, rpItemIntroductionContext);
			Log("ApplyRewardTags done chain=courier session=" + session.Id + " beforeVASSALAGE=" + rewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTag(text) + " beforeKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTag(text));
		}
		catch (Exception ex)
		{
			Log("apply reward tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			VanillaIssueOfferBridge.ApplyIssueOfferTags(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply vanilla issue tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			RomanceSystemBehavior.Instance?.ApplyMarriageTags(recipient, Hero.MainHero, ref text, runPostprocessIfMissing: false);
		}
		catch (Exception ex)
		{
			Log("apply marriage tags failed session=" + session.Id + " error=" + ex.Message);
		}
		session.ReplyPostprocessedText = text;
		session.PostprocessConsumed = true;
		if (persistHistory)
		{
			PersistCourierReplyToHistories(session, recipient, text);
		}
		Log("postprocess committed at recipient session=" + session.Id + " remainingLen=" + (text ?? "").Length);
	}

	private InteractionStatus ExecuteCourierActionPlanForExternal(
		ActionPlan actionPlan,
		GameInteractionSnapshot snapshot,
		string sessionId,
		DetachedDuelDispatchContext duelDispatchContext = null)
	{
		try
		{
			if (!TWParallel.IsMainThread()
				|| actionPlan == null
				|| snapshot?.Identity == null
				|| snapshot.Identity.Channel != InteractionChannel.Courier
				|| !string.Equals(snapshot.Identity.SessionId, sessionId, StringComparison.Ordinal)
				|| string.IsNullOrWhiteSpace(actionPlan.RawPostprocessId))
			{
				return InteractionStatus.RejectedByValidation;
			}
			if (duelDispatchContext != null)
			{
				DuelBehavior.RejectDetachedDuelDispatchForExternal(
					duelDispatchContext,
					"unsupported_channel");
				Log("detached courier Duel rejected session=" + sessionId
					+ " reason=unsupported_channel");
				return InteractionStatus.RejectedByValidation;
			}
			CourierSession session = GetSessionById(sessionId);
			Hero recipient = session == null ? null : ResolveRecipient(session);
			if (recipient == null
				|| recipient.IsDead
				|| !IsCourierActionSessionEligible(session, snapshot, sessionId, SafeHeroId(recipient)))
			{
				Log("detached courier action rejected session=" + sessionId + " reason=target_or_delivery_invalid");
				return InteractionStatus.RejectedByValidation;
			}
			session.ReplyPostprocessedText = actionPlan.RawPostprocessId;
			CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory: false);
			return session.PostprocessConsumed
				? InteractionStatus.Executed
				: InteractionStatus.RejectedByValidation;
		}
		catch (Exception ex)
		{
			Log("detached courier action failed session=" + (sessionId ?? "") + " error=" + ex.Message);
			return InteractionStatus.RejectedByValidation;
		}
	}

	private InteractionStatus GateCourierEconomyActionPlanForExternal(
		ActionPlan actionPlan,
		GameInteractionSnapshot snapshot,
		string sessionId,
		bool isEconomyOnly)
	{
		try
		{
			if (!TWParallel.IsMainThread()
				|| actionPlan == null
				|| string.IsNullOrWhiteSpace(actionPlan.RawPostprocessId)
				|| actionPlan.Actions.Count == 0)
			{
				return InteractionStatus.RejectedByValidation;
			}
			bool allEconomy = actionPlan.Actions.All(LegacyEconomyRewardDebtAdapter.IsEconomyAction);
			if (!actionPlan.Actions.Any(LegacyEconomyRewardDebtAdapter.IsEconomyAction)
				|| allEconomy != isEconomyOnly)
			{
				return InteractionStatus.RejectedByValidation;
			}
			CourierSession session = GetSessionById(sessionId);
			Hero recipient = session == null ? null : ResolveRecipient(session);
			if (recipient == null
				|| recipient.IsDead
				|| !IsCourierActionSessionEligible(session, snapshot, sessionId, SafeHeroId(recipient)))
			{
				Log("detached courier economy gate rejected session=" + (sessionId ?? "") + " reason=target_or_delivery_invalid");
				return InteractionStatus.RejectedByValidation;
			}
			if (isEconomyOnly && !TryReserveCourierEconomyOnly(session))
			{
				return InteractionStatus.RejectedByValidation;
			}
			Log("detached courier economy gate accepted session=" + session.Id + " economyOnly=" + isEconomyOnly);
			return InteractionStatus.Executed;
		}
		catch (Exception ex)
		{
			Log("detached courier economy gate failed session=" + (sessionId ?? "") + " error=" + ex.Message);
			return InteractionStatus.RejectedByValidation;
		}
	}

	private static bool IsCourierActionSessionEligible(
		CourierSession session,
		GameInteractionSnapshot snapshot,
		string expectedSessionId,
		string recipientHeroId)
	{
		return session != null
			&& snapshot?.Identity != null
			&& snapshot.Identity.Channel == InteractionChannel.Courier
			&& !string.IsNullOrWhiteSpace(expectedSessionId)
			&& string.Equals(session.Id, expectedSessionId, StringComparison.Ordinal)
			&& string.Equals(snapshot.Identity.SessionId, expectedSessionId, StringComparison.Ordinal)
			&& !IsInboundToPlayer(session)
			&& !IsTerminalStage(session)
			&& session.DeliveryApplied
			&& !session.PostprocessConsumed
			&& !string.IsNullOrWhiteSpace(recipientHeroId)
			&& string.Equals(snapshot.Identity.SubjectId, recipientHeroId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryReserveCourierEconomyOnly(CourierSession session)
	{
		if (session == null || session.PostprocessConsumed)
		{
			return false;
		}
		session.PostprocessConsumed = true;
		return true;
	}

	private void PersistCourierReplyToHistories(CourierSession session, Hero recipient, string processedReplyText)
	{
		try
		{
			if (session == null || recipient == null)
			{
				return;
			}
			// Keep role-play action prose in the shared dialogue history for later
			// postprocessing. The player-facing letter is still sanitized at display.
			string reply = StripCourierActionTags(processedReplyText);
			if (string.IsNullOrWhiteSpace(reply))
			{
				reply = StripCourierActionTags(session.ReplyText);
			}
			reply = (reply ?? "").Trim();
			if (string.IsNullOrWhiteSpace(reply))
			{
				return;
			}
			string historyLine = "【回信】" + reply;
			string npcName = (recipient.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = "NPC";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, null, historyLine, "[AFEF NPC行为补充] " + npcName + "已通过信使写下回信，信使正在把回信带给玩家。");
			ShoutBehavior.RecordNativeConversationNpcLineForExternal(recipient, recipient.CharacterObject, npcName, historyLine);
			PlayerNotorietyBehavior.NoteCourierReplyForExternal(recipient);
			Log("reply history persisted session=" + session.Id + " recipient=" + SafeHeroId(recipient));
		}
		catch (Exception ex)
		{
			Log("persist reply history failed session=" + (session?.Id ?? "") + " error=" + ex.Message);
		}
	}
}
