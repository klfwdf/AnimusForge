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
	private readonly HashSet<CourierSession> _activeCourierReturns = new HashSet<CourierSession>();
	private void CompleteReturn(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null || IsTerminalStage(session) || !_activeCourierReturns.Add(session))
		{
			return;
		}
		try
		{
			Log("return arrived session=" + session.Id + " deliveryApplied=" + session.DeliveryApplied + " replyGenerated=" + session.ReplyGenerated + " postConsumed=" + session.PostprocessConsumed);
			if (session.DeliveryApplied && !session.PostprocessConsumed && !string.IsNullOrWhiteSpace(session.ReplyPostprocessedText) && recipient != null)
			{
				CommitGeneratedReplyAtRecipient(session, recipient);
			}
			if (ReturnCourierContentsToPlayer(session, courier))
				RecordModuleCourierStep(session, CoreCourierAcceptedSteps.ContentsReturned);
			else FailModuleCourierSession(session, "courier.contents_return_unconfirmed");
			if (session.DeliveryApplied && !session.ReplyPopupShown && !string.IsNullOrWhiteSpace(session.ReplyText))
			{
				session.ReplyPopupShown = true;
				string reply = CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(session.ReplyPostprocessedText ?? session.ReplyText));
				string senderName = recipient?.Name?.ToString() ?? session.RecipientName ?? "NPC";
				string senderHeroId = SafeHeroId(recipient);
				if (AddCourierLetterToPlayerInventory(session, recipient, senderName, reply, isReply: true))
					RecordModuleCourierStep(session, CoreCourierAcceptedSteps.ReplyDelivered, reply);
				else FailModuleCourierSession(session, "courier.reply_delivery_unconfirmed");
				EnqueueMainThreadActionForGeneration(SaveRuntimeGuard.CaptureGeneration(), () =>
				{
					ShowCourierReplyNotice(senderHeroId, senderName, session.DiplomacyReplyBody == null ? reply :
                        CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(DiplomacyConversationBridge.ComposeReply(session.DiplomacyReplyBody, session.DiplomacyReplyReceipts, forUi: true))));
				}, "reply_notice");
			}
			else if (!session.DeliveryApplied)
			{
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReturnedUndelivered", "The courier party returned; undelivered letters and cargo were returned."), Colors.Yellow));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReturnedDisbanded", "The courier party returned and disbanded."), Colors.Green));
			}
			CompleteAndDestroyCourier(session, courier);
		}
		catch
		{
			FailModuleCourierSession(session, "courier.return_unconfirmed");
			throw;
		}
		finally { _activeCourierReturns.Remove(session); }
	}

	private static void ShowCourierReplyNotice(string senderHeroId, string senderName, string replyText)
	{
		string name = string.IsNullOrWhiteSpace(senderName) ? "NPC" : senderName.Trim();
		string cleanedReply = CourierVisibleLetterSanitizer.Clean(replyText);
		string body = string.IsNullOrWhiteSpace(cleanedReply) ? ConversationUiLocalization.Text("AF_CONV_UI_Courier_EmptyReply", "(No reply text)") : cleanedReply;
		if (TryPublishCourierReplyMapNotification(senderHeroId, name, body))
		{
			return;
		}
		Action replyAction = string.IsNullOrWhiteSpace(senderHeroId) ? null : (() => OpenCourierReplyFlowForExternal(senderHeroId, name));
		try
		{
			if (CourierLetterReplyPopup.ShowWithReply(ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReplyTitle", "The courier brought a reply"), ConversationUiLocalization.Text("AF_CONV_UI_Courier_Wrote", "{NAME} wrote:", "NAME", name), body, replyAction, ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReplyButton", "Reply"), null, ConversationUiLocalization.Text("AF_CONV_UI_Courier_Close", "Close")))
			{
				return;
			}
		}
		catch (Exception ex)
		{
			Log("show courier reply popup failed sender=" + name + " error=" + ex.Message);
		}
		InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReplyNotice", "The courier brought a reply from {NAME}.", "NAME", name), Colors.Green));
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

	private bool ReturnCourierContentsToPlayer(CourierSession session, MobileParty courier)
	{
		PartyBase playerParty = MobileParty.MainParty?.Party ?? PartyBase.MainParty;
		if (playerParty == null || courier == null)
		{
			return false;
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
		if (courier.MemberRoster.TotalManCount != 0 || courier.PrisonRoster.TotalManCount != 0) return false;
		for (int i = 0; i < courier.ItemRoster.Count; i++)
			if (courier.ItemRoster.GetElementCopyAtIndex(i).Amount > 0) return false;
		return session.DeliveryApplied || session.EscrowGold == 0;
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
			string destroyerName = destroyerParty?.Name?.ToString() ?? ConversationUiLocalization.Text("AF_CONV_UI_Courier_UnknownFaction", "unknown faction");
			Log("destroyed session=" + session.Id + " party=" + destroyedParty.StringId + " destroyer=" + destroyerName + " deliveryApplied=" + session.DeliveryApplied);
			if (IsInboundToPlayer(session))
			{
				HandleInboundCourierDestroyed(session, destroyedParty, destroyerName);
				return;
			}
			FailModuleCourierSession(session, "courier.transport_destroyed", CoreCourierTransportOutcome.Destroyed);
			DisplayCourierDestroyedStatus(session, ConversationUiLocalization.Text("AF_CONV_UI_Courier_DestroyerDetail", " Destroyer: {NAME}", "NAME", destroyerName));
			TryMoveHeroLossesToDestroyer(session, destroyerParty);
			string fact = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0447") + (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0448")) + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0449") + destroyerName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0450");
			if (!session.DeliveryApplied)
			{
				fact += ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0451");
			}
			else
			{
				fact += ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0452");
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
			UntrackCourierMapVisual(destroyedParty, "destroyed");
			DestroyCourierTemporaryShips(session, destroyedParty, "destroyed");
			session.Stage = CourierStage.Destroyed.ToString();
			EndCourierReplyWaitPause(session, "destroyed");
			RetireCourierRequestLifetime(session);
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
		bool cleanupConfirmed = false;
		session.Stage = CourierStage.Completed.ToString();
		RetireCourierRequestLifetime(session);
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
			cleanupConfirmed = courier != null && !courier.IsActive;
		}
		catch (Exception ex)
		{
			Log("destroy completed courier failed session=" + session.Id + " error=" + ex.Message);
		}
		CompleteModuleCourierTransport(session, cleanupConfirmed);
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
		FailModuleCourierSession(session, "courier.transport_missing", CoreCourierTransportOutcome.Missing);
		DisplayCourierDestroyedStatus(session, ConversationUiLocalization.Text("AF_CONV_UI_Courier_LostDetail", " The courier party disappeared from the campaign map."));
		Hero recipient = ResolveRecipient(session);
		MyBehavior.AppendExternalDialogueHistory(recipient, null, null, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0453"));
		session.Stage = CourierStage.Destroyed.ToString();
		session.TemporaryShipCreated = false;
		session.TemporaryShipHullId = "";
		EndCourierReplyWaitPause(session, "missing");
		RetireCourierRequestLifetime(session);
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
			DisplayCourierDestroyedStatus(session, ConversationUiLocalization.Text("AF_CONV_UI_Courier_DestroyerDetail", " Destroyer: {NAME}", "NAME", destroyerName ?? ConversationUiLocalization.Text("AF_CONV_UI_Courier_UnknownFaction", "unknown faction")));
			Hero sender = ResolveSender(session);
			string senderName = string.IsNullOrWhiteSpace(session?.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
			MyBehavior.AppendExternalDialogueHistory(sender, null, null, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0454") + senderName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0455") + (destroyerName ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0456")) + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0457") + GetInboundLetterKindDisplayText(session) + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0458"));
			UntrackCourierMapVisual(destroyedParty, "destroyed_inbound");
			DestroyCourierTemporaryShips(session, destroyedParty, "destroyed_inbound");
			session.Stage = CourierStage.Destroyed.ToString();
			EndCourierReplyWaitPause(session, "destroyed_inbound");
			RetireCourierRequestLifetime(session);
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
		DisplayCourierDestroyedStatus(session, ConversationUiLocalization.Text("AF_CONV_UI_Courier_LostDetail", " The courier party disappeared from the campaign map."));
		Hero sender = ResolveSender(session);
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		MyBehavior.AppendExternalDialogueHistory(sender, null, null, ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0459") + senderName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0460") + GetInboundLetterKindDisplayText(session) + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0461"));
		session.Stage = CourierStage.Destroyed.ToString();
		session.TemporaryShipCreated = false;
		session.TemporaryShipHullId = "";
		EndCourierReplyWaitPause(session, "missing_inbound");
		RetireCourierRequestLifetime(session);
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
			string recipientName = string.IsNullOrWhiteSpace(session?.RecipientName) ? "" : ConversationUiLocalization.Text("AF_CONV_UI_Courier_RecipientSuffix", " Recipient: {NAME}.", "NAME", session.RecipientName);
			string detailText = string.IsNullOrWhiteSpace(detail) ? "" : ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReturnDetail", " {DETAIL}", "DETAIL", detail.Trim());
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_Destroyed", "The courier party was destroyed.{RECIPIENT}{DETAIL}", ("RECIPIENT", recipientName), ("DETAIL", detailText)), Colors.Red));
			Log("status courier_destroyed session=" + (session?.Id ?? "") + " recipient=" + (session?.RecipientHeroId ?? "") + " detail=" + (detail ?? ""));
		}
		catch
		{
		}
	}
}
