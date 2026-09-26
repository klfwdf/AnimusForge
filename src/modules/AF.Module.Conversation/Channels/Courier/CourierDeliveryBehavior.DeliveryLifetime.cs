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
	private void CompleteReturn(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null)
		{
			return;
		}
		Log("return arrived session=" + session.Id + " deliveryApplied=" + session.DeliveryApplied + " replyGenerated=" + session.ReplyGenerated + " postConsumed=" + session.PostprocessConsumed);
		if (session.DeliveryApplied && !session.PostprocessConsumed && !string.IsNullOrWhiteSpace(session.ReplyPostprocessedText) && recipient != null)
		{
			string text = session.ReplyPostprocessedText;
			try
			{
				TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure);
				if (!string.IsNullOrWhiteSpace(proposalFailure))
				{
					Log("kingdom agenda custom policy fallback not queued session=" + session.Id + " reason=" + proposalFailure);
				}
			}
			catch (Exception ex)
			{
				Log("apply kingdom agenda custom policy fallback tag failed session=" + session.Id + " error=" + ex.Message);
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
			PersistCourierReplyToHistories(session, recipient, text);
			Log("postprocess consumed session=" + session.Id + " remainingLen=" + (text ?? "").Length);
		}
		ReturnCourierContentsToPlayer(session, courier);
		if (session.DeliveryApplied && !session.ReplyPopupShown && !string.IsNullOrWhiteSpace(session.ReplyText))
		{
			session.ReplyPopupShown = true;
			string reply = CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(session.ReplyPostprocessedText ?? session.ReplyText));
			string senderName = recipient?.Name?.ToString() ?? session.RecipientName ?? "NPC";
			string senderHeroId = SafeHeroId(recipient);
			AddCourierLetterToPlayerInventory(session, recipient, senderName, reply, isReply: true);
			MainThreadActions.Enqueue(() =>
			{
				ShowCourierReplyNotice(senderHeroId, senderName, reply);
			});
		}
		else if (!session.DeliveryApplied)
		{
			InformationManager.DisplayMessage(new InformationMessage("信使队已返回，未交付的信件与物资已退还。", Colors.Yellow));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("信使队已返回并解散。", Colors.Green));
		}
		CompleteAndDestroyCourier(session, courier);
	}

	private static void ShowCourierReplyNotice(string senderHeroId, string senderName, string replyText)
	{
		string name = string.IsNullOrWhiteSpace(senderName) ? "NPC" : senderName.Trim();
		string cleanedReply = CourierVisibleLetterSanitizer.Clean(replyText);
		string body = string.IsNullOrWhiteSpace(cleanedReply) ? "（无回信正文）" : cleanedReply;
		if (TryPublishCourierReplyMapNotification(senderHeroId, name, body))
		{
			return;
		}
		Action replyAction = string.IsNullOrWhiteSpace(senderHeroId) ? null : (() => OpenCourierReplyFlowForExternal(senderHeroId, name));
		try
		{
			if (CourierLetterReplyPopup.ShowWithReply("信使带回了回信", name + "写道：", body, replyAction, "回信", null, "关闭"))
			{
				return;
			}
		}
		catch (Exception ex)
		{
			Log("show courier reply popup failed sender=" + name + " error=" + ex.Message);
		}
		InformationManager.DisplayMessage(new InformationMessage("信使带回了" + name + "的回信。", Colors.Green));
	}

	private static bool TryPublishCourierReplyMapNotification(string senderHeroId, string senderName, string replyText)
	{
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is not MapState)
			{
				return false;
			}
			MapNotificationView mapNotificationView = MapScreen.Instance?.MapNotificationView;
			if (mapNotificationView == null)
			{
				return false;
			}
			if (!ReferenceEquals(_courierReplyRegisteredMapNotificationView, mapNotificationView))
			{
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeCourierReplyMapNotification), typeof(AnimusForgeCourierReplyMapNotificationItemVM));
				_courierReplyRegisteredMapNotificationView = mapNotificationView;
			}
			MBInformationManager.AddNotice(new AnimusForgeCourierReplyMapNotification(senderHeroId, senderName, replyText));
			return true;
		}
		catch (Exception ex)
		{
			Log("publish courier reply map notification failed error=" + ex.Message);
			return false;
		}
	}

	private void ApplyDeliveryPayload(CourierSession session, MobileParty courier, Hero recipient)
	{
		CourierPayloadMode mode = ParsePayloadMode(session.PayloadMode);
		if (mode == CourierPayloadMode.Show)
		{
			string shownTargetKey = BuildCourierShownTargetKey(recipient);
			int shownGold = 0;
			Dictionary<string, int> shownItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int remainingShowableGold = MyBehavior.GetRemainingShowableGoldForExternal(
				recipient,
				shownTargetKey,
				Math.Max(0, Hero.MainHero?.Gold ?? 0));
			Dictionary<string, int> currentItemCounts = null;
			bool needsItemSnapshot = (session.Entries ?? new List<CourierCargoEntry>())
				.Any(entry => entry != null
					&& entry.Amount > 0
					&& string.Equals(entry.Kind, "show_item", StringComparison.OrdinalIgnoreCase));
			if (needsItemSnapshot)
			{
				currentItemCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				ItemRoster playerRoster = MobileParty.MainParty?.ItemRoster;
				if (playerRoster != null)
				{
					// Snapshot the player roster once for this arrival. Later entries consume only
					// this snapshot, so duplicate entries cannot overstate what was actually held.
					for (int i = 0; i < playerRoster.Count; i++)
					{
						ItemRosterElement element = playerRoster.GetElementCopyAtIndex(i);
						string itemId = (element.EquipmentElement.Item?.StringId ?? "").Trim();
						if (element.Amount <= 0 || string.IsNullOrWhiteSpace(itemId))
						{
							continue;
						}
						currentItemCounts[itemId] = (currentItemCounts.TryGetValue(itemId, out int existingCount)
							? existingCount
							: 0) + element.Amount;
					}
				}
			}
			Dictionary<string, int> remainingShowableItems =
				new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
			{
				if (entry == null || entry.Amount <= 0)
				{
					continue;
				}
				if (string.Equals(entry.Kind, "show_gold", StringComparison.OrdinalIgnoreCase))
				{
					int actual = Math.Min(entry.Amount, Math.Max(0, remainingShowableGold));
					entry.Amount = actual;
					entry.Delivered = actual > 0;
					shownGold += actual;
					remainingShowableGold -= actual;
				}
				else if (string.Equals(entry.Kind, "show_item", StringComparison.OrdinalIgnoreCase))
				{
					string itemId = (entry.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(itemId))
					{
						entry.Amount = 0;
						entry.Delivered = false;
						continue;
					}
					if (!remainingShowableItems.TryGetValue(itemId, out int remaining))
					{
						int currentCount = currentItemCounts != null
							&& currentItemCounts.TryGetValue(itemId, out int heldCount)
								? Math.Max(0, heldCount)
								: 0;
						remaining = MyBehavior.GetRemainingShowableItemCountForExternal(
							recipient,
							shownTargetKey,
							itemId,
							currentCount);
					}
					int actual = Math.Min(entry.Amount, Math.Max(0, remaining));
					entry.Amount = actual;
					entry.Delivered = actual > 0;
					remainingShowableItems[itemId] = Math.Max(0, remaining - actual);
					if (actual > 0)
					{
						shownItems[itemId] = (shownItems.TryGetValue(itemId, out int old) ? old : 0) + actual;
					}
				}
				else
				{
					entry.Amount = 0;
					entry.Delivered = false;
				}
			}
			MyBehavior.RecordShownResourcesForExternal(
				recipient,
				shownTargetKey,
				shownGold,
				shownItems);
			return;
		}
		PartyBase targetParty = ResolveRecipientPartyBase(recipient);
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Delivered || entry.Amount <= 0)
			{
				continue;
			}
			if (string.Equals(entry.Kind, "gold", StringComparison.OrdinalIgnoreCase))
			{
				int amount = Math.Min(Math.Max(0, session.EscrowGold), entry.Amount);
				if (amount > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(null, recipient, amount, true);
					session.EscrowGold -= amount;
					entry.Amount = amount;
					entry.Delivered = true;
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(recipient, amount, null, 0);
					}
					catch
					{
					}
				}
			}
			else if (string.Equals(entry.Kind, "item", StringComparison.OrdinalIgnoreCase))
			{
				ItemObject item;
				ItemRoster targetRoster = ResolveRecipientItemRoster(recipient);
				int moved = MyBehavior.TransferItemsFromRosterByStringId(courier.ItemRoster, targetRoster, entry.Id, entry.Amount, out item);
				entry.Amount = moved;
				entry.Delivered = moved > 0;
				if (item != null)
				{
					entry.Name = item.Name?.ToString() ?? entry.Name;
					if (entry.GuidePriceDenars <= 0)
					{
						entry.GuidePriceDenars = EstimateCourierItemUnitValue(item);
					}
				}
				try
				{
					if (moved > 0)
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(recipient, 0, entry.Id, moved);
					}
				}
				catch
				{
				}
			}
			else if (string.Equals(entry.Kind, "troop", StringComparison.OrdinalIgnoreCase))
			{
				if (entry.GuidePriceDenars <= 0)
				{
					entry.GuidePriceDenars = EstimateCourierPartyTransferUnitValue(recipient, entry, isPrisoner: false);
				}
				int moved = MoveCharacterBetweenMemberRosters(courier.Party, targetParty, entry.Id, entry.Amount, entry.IsHero);
				entry.Amount = moved;
				entry.Delivered = moved > 0;
			}
			else if (string.Equals(entry.Kind, "prisoner", StringComparison.OrdinalIgnoreCase))
			{
				if (entry.GuidePriceDenars <= 0)
				{
					entry.GuidePriceDenars = EstimateCourierPartyTransferUnitValue(recipient, entry, isPrisoner: true);
				}
				int moved = MoveCharacterBetweenPrisonRosters(courier.Party, targetParty, entry.Id, entry.Amount, entry.IsHero);
				entry.Amount = moved;
				entry.Delivered = moved > 0;
			}
			else if (string.Equals(entry.Kind, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				if (entry.GuidePriceDenars <= 0)
				{
					entry.GuidePriceDenars = EstimateCourierSettlementTransferValue(recipient, entry);
				}
				string status = null;
				bool ok = RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryApplyPlayerSettlementTransferForExternal(recipient, entry.Id, out status);
				entry.Delivered = ok;
				entry.Amount = ok ? 1 : 0;
				if (!ok && !string.IsNullOrWhiteSpace(status))
				{
					Log("fixed asset transfer failed session=" + session.Id + " asset=" + entry.Id + " status=" + status);
				}
			}
		}
	}

	private void ReturnCourierContentsToPlayer(CourierSession session, MobileParty courier)
	{
		PartyBase playerParty = MobileParty.MainParty?.Party ?? PartyBase.MainParty;
		if (playerParty == null || courier == null)
		{
			return;
		}
		MoveWholeMemberRoster(courier.Party, playerParty);
		MoveWholePrisonRoster(courier.Party, playerParty);
		MoveWholeItemRoster(courier.ItemRoster, MobileParty.MainParty?.ItemRoster);
		if (!session.DeliveryApplied && session.EscrowGold > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, session.EscrowGold, true);
			Log("refunded escrow gold session=" + session.Id + " amount=" + session.EscrowGold);
			session.EscrowGold = 0;
		}
	}

	private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
	{
		try
		{
			if (destroyedParty == null)
			{
				return;
			}
			CourierSession session = null;
			lock (_sessionLock)
			{
				session = _sessions.Values.FirstOrDefault(x => x != null && string.Equals((x.CourierPartyId ?? "").Trim(), destroyedParty.StringId ?? "", StringComparison.OrdinalIgnoreCase) && !IsTerminalStage(x));
			}
			if (session == null)
			{
				return;
			}
			Hero recipient = ResolveRecipient(session);
			string destroyerName = destroyerParty?.Name?.ToString() ?? "未知势力";
			Log("destroyed session=" + session.Id + " party=" + destroyedParty.StringId + " destroyer=" + destroyerName + " deliveryApplied=" + session.DeliveryApplied);
			if (IsInboundToPlayer(session))
			{
				HandleInboundCourierDestroyed(session, destroyedParty, destroyerName);
				return;
			}
			DisplayCourierDestroyedStatus(session, "歼灭者：" + destroyerName);
			TryMoveHeroLossesToDestroyer(session, destroyerParty);
			string fact = "[AFEF玩家行为补充] " + (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家") + "通过信使寄出的信使队在途中被" + destroyerName + "歼灭。";
			if (!session.DeliveryApplied)
			{
				fact += "这封信和随信寄出的物品、金钱、部队或俘虏未能送达；固定资产转移没有发生。";
			}
			else
			{
				fact += "该信使队已完成交付，但未能把回信或剩余人员安全带回玩家处。";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
			UntrackCourierMapVisual(destroyedParty, "destroyed");
			DestroyCourierTemporaryShips(session, destroyedParty, "destroyed");
			session.Stage = CourierStage.Destroyed.ToString();
			EndCourierReplyWaitPause(session, "destroyed");
			lock (_sessionLock)
			{
				_sessions.Remove(session.Id);
			}
			RemoveCourierRuntimeIndex(session);
		}
		catch (Exception ex)
		{
			Log("destroy handler failed: " + ex);
		}
	}

	private void TryMoveHeroLossesToDestroyer(CourierSession session, PartyBase destroyerParty)
	{
		if (session == null || destroyerParty == null)
		{
			return;
		}
		foreach (CourierCargoEntry entry in (session.CrewEntries ?? new List<CourierCargoEntry>()).Concat(session.Entries ?? new List<CourierCargoEntry>()))
		{
			if (entry == null || !entry.IsHero)
			{
				continue;
			}
			if (!string.Equals(entry.Kind, "crew", StringComparison.OrdinalIgnoreCase) && entry.Delivered)
			{
				continue;
			}
			CharacterObject character = ResolveCharacter(entry.Id);
			Hero hero = character?.HeroObject;
			if (hero == null || hero.IsDead || hero.IsHumanPlayerCharacter)
			{
				continue;
			}
			try
			{
				if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null)
				{
					TakePrisonerAction.Apply(destroyerParty, hero);
					Log("hero loss captured session=" + session.Id + " hero=" + hero.StringId + " destroyer=" + destroyerParty.Name);
				}
			}
			catch (Exception ex)
			{
				Log("hero loss capture failed session=" + session.Id + " hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
			}
		}
	}

	private void CompleteAndDestroyCourier(CourierSession session, MobileParty courier)
	{
		if (session == null)
		{
			return;
		}
		session.Stage = CourierStage.Completed.ToString();
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
		try
		{
			if (courier != null && courier.IsActive)
			{
				UntrackCourierMapVisual(courier, "completed");
				DestroyCourierTemporaryShips(session, courier, "completed");
				if (courier.IsCurrentlyUsedByAQuest)
				{
					courier.SetPartyUsedByQuest(false);
				}
				DestroyPartyAction.Apply(null, courier);
			}
		}
		catch (Exception ex)
		{
			Log("destroy completed courier failed session=" + session.Id + " error=" + ex.Message);
		}
		Log("session completed id=" + session.Id);
	}

	private void HandleCourierMissing(CourierSession session)
	{
		if (session == null)
		{
			return;
		}
		Log("courier missing session=" + session.Id + " party=" + session.CourierPartyId);
		if (IsInboundToPlayer(session))
		{
			HandleInboundCourierMissing(session);
			return;
		}
		DisplayCourierDestroyedStatus(session, "信使队伍已从大地图消失。");
		Hero recipient = ResolveRecipient(session);
		MyBehavior.AppendExternalDialogueHistory(recipient, null, null, "[AFEF玩家行为补充] 玩家派出的信使队失去踪迹，信件与随信物资未能确认送达。");
		session.Stage = CourierStage.Destroyed.ToString();
		session.TemporaryShipCreated = false;
		session.TemporaryShipHullId = "";
		EndCourierReplyWaitPause(session, "missing");
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
	}

	private void HandleInboundCourierDestroyed(CourierSession session, MobileParty destroyedParty, string destroyerName)
	{
		try
		{
			DisplayCourierDestroyedStatus(session, "歼灭者：" + (destroyerName ?? "未知势力"));
			Hero sender = ResolveSender(session);
			string senderName = string.IsNullOrWhiteSpace(session?.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
			MyBehavior.AppendExternalDialogueHistory(sender, null, null, "[AFEF NPC行为补充] " + senderName + "派往玩家处的信使队在途中被" + (destroyerName ?? "未知势力") + "歼灭，这封" + GetInboundLetterKindDisplayText(session) + "未能确认送达。");
			UntrackCourierMapVisual(destroyedParty, "destroyed_inbound");
			DestroyCourierTemporaryShips(session, destroyedParty, "destroyed_inbound");
			session.Stage = CourierStage.Destroyed.ToString();
			EndCourierReplyWaitPause(session, "destroyed_inbound");
			lock (_sessionLock)
			{
				_sessions.Remove(session.Id);
			}
			RemoveCourierRuntimeIndex(session);
		}
		catch (Exception ex)
		{
			Log("inbound destroy handler failed: " + ex);
		}
	}

	private void HandleInboundCourierMissing(CourierSession session)
	{
		if (session == null)
		{
			return;
		}
		DisplayCourierDestroyedStatus(session, "信使队伍已从大地图消失。");
		Hero sender = ResolveSender(session);
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		MyBehavior.AppendExternalDialogueHistory(sender, null, null, "[AFEF NPC行为补充] " + senderName + "派往玩家处的信使队失去踪迹，这封" + GetInboundLetterKindDisplayText(session) + "未能确认送达。");
		session.Stage = CourierStage.Destroyed.ToString();
		session.TemporaryShipCreated = false;
		session.TemporaryShipHullId = "";
		EndCourierReplyWaitPause(session, "missing_inbound");
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
	}

	private static void DisplayCourierDestroyedStatus(CourierSession session, string detail)
	{
		try
		{
			string recipientName = string.IsNullOrWhiteSpace(session?.RecipientName) ? "" : " 收件人：" + session.RecipientName + "。";
			string detailText = string.IsNullOrWhiteSpace(detail) ? "" : " " + detail.Trim();
			InformationManager.DisplayMessage(new InformationMessage("信使部队已被歼灭。" + recipientName + detailText, Colors.Red));
			Log("status courier_destroyed session=" + (session?.Id ?? "") + " recipient=" + (session?.RecipientHeroId ?? "") + " detail=" + (detail ?? ""));
		}
		catch
		{
		}
	}
}
