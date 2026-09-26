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
	private void ProcessSession(CourierSession session)
	{
		if (session == null || IsTerminalStage(session))
		{
			return;
		}
		NormalizeSession(session);
		CourierStage stage = ParseStage(session.Stage);
		MobileParty courier = ResolveCourierParty(session);
		if (courier == null || !courier.IsActive)
		{
			HandleCourierMissing(session);
			return;
		}
		ApplyCourierAiOverrides(courier, "tick");
		Hero recipient = ResolveRecipient(session);
		LogCourierStatusVerbose("tick:" + session.Id, BuildCourierStatusSnapshot(session, courier, recipient, "tick"), 6.0);
		if (IsInboundToPlayer(session))
		{
			ProcessInboundToPlayerSession(session, courier);
			return;
		}
		if (!session.DeliveryApplied && recipient != null && !recipient.IsDead && !session.ReplyGenerated && !session.ReplyGenerationStarted)
		{
			StartCourierReplyGeneration(session, "outbound_preflight");
		}
		if (stage == CourierStage.GeneratingReply)
		{
			if (session.DeliveryApplied && (recipient == null || recipient.IsDead))
			{
				EndCourierReplyWaitPause(session, "recipient_invalid_after_delivery");
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				session.Stage = CourierStage.Returning.ToString();
				RouteToSender(session, courier);
				return;
			}
			if (session.ReplyGenerated)
			{
				CommitGeneratedReplyAtRecipient(session, recipient);
				EndCourierReplyWaitPause(session, "reply_generated");
				session.Stage = CourierStage.Returning.ToString();
				RouteToSender(session, courier);
				return;
			}
			if (!session.ReplyGenerationStarted)
			{
				StartCourierReplyGeneration(session, "resume_or_tick");
			}
			ShowCourierReplyWaitPopupAndPause(session, recipient);
			MaintainReplyWaitAtRecipient(session, courier, recipient);
			return;
		}
		if ((stage == CourierStage.Outbound || stage == CourierStage.WaitingRecipient) && recipient == null)
		{
			LogCourierStatusVerbose("target_unresolved:" + session.Id, "target_unresolved session=" + session.Id + " reason=recipient_null " + BuildCourierStatusSnapshot(session, courier, recipient, "target_unresolved"), 5.0);
			RouteToSafeSettlement(session, courier, "recipient_unresolved");
			return;
		}
		if ((stage == CourierStage.Outbound || stage == CourierStage.WaitingRecipient) && recipient != null && recipient.IsDead)
		{
			Log("recipient dead before delivery, returning and refunding session=" + session.Id + " recipient=" + SafeHeroId(recipient));
			session.Stage = CourierStage.Returning.ToString();
			session.DeliveryApplied = false;
			session.RecipientWaitReason = "";
			EndCourierReplyWaitPause(session, "recipient_dead_before_delivery");
			RouteToSender(session, courier);
			return;
		}
		if (stage == CourierStage.Outbound || stage == CourierStage.WaitingRecipient)
		{
			if (HandleRecipientUnavailableStatus(session, courier, recipient))
			{
				return;
			}
			if (TryGetRecipientTarget(recipient, out var targetParty, out var targetSettlement))
			{
				session.Stage = CourierStage.Outbound.ToString();
				ClearRecipientWaitReasonIfNeeded(session, "target_resolved");
				LogCourierStatusVerbose("target_resolved:" + session.Id, "target_resolved session=" + session.Id + " " + DescribeRecipientTarget(recipient, targetParty, targetSettlement), 3.0);
				if (IsAtRecipient(courier, targetParty, targetSettlement))
				{
					DeliverToRecipient(session, courier, recipient);
					return;
				}
				RouteToRecipient(session, courier, targetParty, targetSettlement);
				return;
			}
			session.Stage = CourierStage.WaitingRecipient.ToString();
			if (!IsBlockingRecipientWaitReason(session.RecipientWaitReason))
			{
				SetRecipientWaitReason(session, "unresolved", "target_unresolved");
			}
			LogCourierStatusVerbose("target_unresolved:" + session.Id, "target_unresolved session=" + session.Id + " reason=no_party_or_settlement " + DescribeHero(recipient) + " courier=" + DescribeMobileParty(courier), 5.0);
			RouteToSafeSettlement(session, courier, "recipient_wait_respawn");
			return;
		}
		if (stage == CourierStage.Returning || stage == CourierStage.WaitingSender)
		{
			MobileParty senderParty = MobileParty.MainParty;
			if (senderParty == null || !senderParty.IsActive)
			{
				session.Stage = CourierStage.WaitingSender.ToString();
				RouteToSafeSettlement(session, courier, "sender_wait_respawn");
				return;
			}
			session.Stage = CourierStage.Returning.ToString();
			if (IsAtSender(courier, senderParty))
			{
				CompleteReturn(session, courier, recipient);
				return;
			}
			RouteToSender(session, courier);
		}
	}

	private void ProcessInboundToPlayerSession(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || !mainParty.IsActive)
		{
			session.Stage = CourierStage.WaitingSender.ToString();
			RouteToSafeSettlement(session, courier, "player_wait_respawn");
			return;
		}
		if (!session.ReplyGenerated && !session.ReplyGenerationStarted)
		{
			StartInboundLetterGeneration(session, "inbound_tick");
		}
		if (IsAtSender(courier, mainParty))
		{
			if (IsPlayerBusyForInboundLetterDelivery(mainParty))
			{
				HoldInboundCourierAtPlayer(session, courier);
				return;
			}
			DeliverInboundLetterToPlayer(session, courier);
			return;
		}
		session.Stage = CourierStage.Outbound.ToString();
		RouteToSender(session, courier);
	}

	private void DeliverInboundLetterToPlayer(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		Hero sender = ResolveSender(session);
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		if (!IsCourierInboundCompletionReadyForDelivery(session))
		{
			session.ReplyGenerationStarted = true;
			HoldInboundCourierAtPlayer(session, courier);
			return;
		}
		if (!session.ReplyGenerated)
		{
			session.Stage = CourierStage.GeneratingReply.ToString();
			ShowCourierReplyWaitPopupAndPause(session, sender);
			StartInboundLetterGeneration(session, "delivered");
			HoldInboundCourierAtPlayer(session, courier);
			return;
		}
		if (session.ReplyWaitPopupShown || _courierReplyWaitTimeLocked)
		{
			EndCourierReplyWaitPause(session, "inbound_letter_generated");
		}
		string letter = (session.LetterText ?? "").Trim();
		string visibleLetter = CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(letter));
		if (!session.DeliveryApplied)
		{
			session.DeliveryApplied = true;
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: true, sender);
			string historyLine = "【" + GetInboundLetterKindDisplayText(session) + "】" + senderName + "通过信使写道：" + visibleLetter;
			MyBehavior.AppendExternalDialogueHistory(sender, null, historyLine, session.DeliveryFactText);
			ShoutBehavior.RecordNativeConversationNpcLineForExternal(sender, sender?.CharacterObject, senderName, historyLine);
			AddCourierLetterToPlayerInventory(session, sender, senderName, visibleLetter, isReply: false);
			if (session.IsNpcInitiated && !string.IsNullOrWhiteSpace(session.InboundNeedType))
			{
				ProactiveNpcRequestBehavior.RecordLetterNeedDeliveredForExternal(session.InboundNeedType);
			}
			if (session.IsNpcInitiated && !string.IsNullOrWhiteSpace(session.InboundEventKey))
			{
				_npcLetterLastDeliveredEventKeyBySender[SafeHeroId(sender)] = session.InboundEventKey.Trim();
			}
			Log("inbound delivered session=" + session.Id + " sender=" + SafeHeroId(sender) + " factLen=" + (session.DeliveryFactText ?? "").Length);
		}
		string senderHeroId = SafeHeroId(sender);
		string letterKind = GetInboundLetterKindDisplayText(session);
		MainThreadActions.Enqueue(() =>
		{
			ShowInboundCourierLetterNotice(senderHeroId, senderName, visibleLetter, letterKind);
		});
		CompleteAndDestroyCourier(session, courier);
	}

	private static bool IsPlayerBusyForInboundLetterDelivery(MobileParty mainParty)
	{
		try
		{
			if (mainParty == null || !mainParty.IsActive)
			{
				return true;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				return true;
			}
			if (mainParty.MapEvent != null || PlayerEncounterCompat.GetCurrentMapEventSafe() != null)
			{
				return true;
			}
			if (mainParty.BesiegerCamp != null || mainParty.SiegeEvent != null || mainParty.BesiegedSettlement != null)
			{
				return true;
			}
			return IsCourierForbiddenSettlementCombatBehavior(mainParty.DefaultBehavior)
				|| IsCourierForbiddenSettlementCombatBehavior(mainParty.ShortTermBehavior);
		}
		catch
		{
			return true;
		}
	}

	private static void ShowInboundCourierLetterNotice(string senderHeroId, string senderName, string letterText, string letterKind)
	{
		string name = string.IsNullOrWhiteSpace(senderName) ? "NPC" : senderName.Trim();
		string cleanedLetter = CourierVisibleLetterSanitizer.Clean(letterText);
		string body = string.IsNullOrWhiteSpace(cleanedLetter) ? "（无来信正文）" : cleanedLetter;
		string title = "信使送来" + (string.IsNullOrWhiteSpace(letterKind) ? "来信" : letterKind.Trim());
		Action replyAction = string.IsNullOrWhiteSpace(senderHeroId) ? null : (() => OpenCourierReplyFlowForExternal(senderHeroId, name));
		try
		{
			if (CourierLetterReplyPopup.ShowWithReply(title, name + "写道：", body, replyAction, "回信", null, "关闭"))
			{
				return;
			}
		}
		catch (Exception ex)
		{
			Log("show inbound courier letter popup failed sender=" + name + " error=" + ex.Message);
		}
		try
		{
			if (replyAction != null)
			{
				InformationManager.ShowInquiry(new InquiryData(title, name + "写道：\n\n" + body, true, true, "回信", "关闭", replyAction, null), true);
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(title, name + "写道：\n\n" + body, true, false, "知道了", "", null, null), true);
			}
		}
		catch
		{
			InformationManager.DisplayMessage(new InformationMessage(title + "：" + body, Colors.Green));
		}
	}
}
