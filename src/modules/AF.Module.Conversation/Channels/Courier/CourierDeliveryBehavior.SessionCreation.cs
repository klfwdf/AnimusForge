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
	// Main-thread draft identity is shared by UI callbacks and the module submission boundary.
	// Advancing a step revokes every callback issued by the preceding window, even on the same draft.
	private bool IsPendingCourierFlowCurrent(PendingCourierFlow flow, long revision, bool allowClaimed = false)
	{
		return TWParallel.IsMainThread() && ReferenceEquals(Instance, this) && _pendingOwnerPhases.Accepting
			&& flow != null && ReferenceEquals(_pendingFlow, flow) && flow.Revision == revision
			&& (allowClaimed || !flow.Claimed) && SaveRuntimeGuard.IsCurrentGeneration(flow.RuntimeGeneration);
	}

	private long BeginCourierDraftStep(PendingCourierFlow flow, bool readyToSend = false)
	{
		if (flow == null || !IsPendingCourierFlowCurrent(flow, flow.Revision)) return -1;
		InvalidateCourierDraftTickets();
		flow.ReadyToSend = readyToSend;
		return ++flow.Revision;
	}

	private void ResetPendingCourierFlow(PendingCourierFlow flow, long revision, string reason, bool allowClaimed = false)
	{
		if (IsPendingCourierFlowCurrent(flow, revision, allowClaimed)) ResetPendingFlow(reason);
	}

	public static bool ShouldShowCourierButtonForExternal(Hero hero, bool informationHidden)
	{
		try
		{
			if (hero == null || hero == Hero.MainHero || hero.CharacterObject?.IsHero != true || informationHidden)
			{
				return false;
			}
			if (hero.IsDead || !hero.IsKnownToPlayer)
			{
				return false;
			}
			if (IsHeroInPlayerPartyForCourier(hero))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHeroInPlayerPartyForCourier(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return false;
			}
			if (hero == Hero.MainHero || hero.IsHumanPlayerCharacter)
			{
				return true;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return false;
			}
			if (hero.PartyBelongedTo == mainParty || hero.PartyBelongedToAsPrisoner == mainParty.Party)
			{
				return true;
			}
			CharacterObject character = hero.CharacterObject;
			return CountRosterCharacter(mainParty.MemberRoster, character) > 0 || CountRosterCharacter(mainParty.PrisonRoster, character) > 0;
		}
		catch
		{
			return false;
		}
	}

	public static void OpenCourierFlowForExternal(Hero recipient)
	{
		try
		{
			Instance?.OpenCourierFlow(recipient);
		}
		catch (Exception ex)
		{
			Log("open courier flow failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_OpenFailed", "Could not open courier mail: {ERROR}", ("ERROR", ex.Message)), Colors.Red));
		}
	}

	public static void OpenCourierReplyFlowForExternal(string targetHeroId, string targetName)
	{
		try
		{
			if (Instance == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_NotReady", "The courier system is not initialized; replies are unavailable."), Colors.Yellow));
				return;
			}
			Hero target = ResolveHeroByIdForCourier(targetHeroId);
			if (target == null)
			{
				string name = string.IsNullOrWhiteSpace(targetName) ? ConversationUiLocalization.Text("AF_CONV_UI_Courier_Character", "this character") : targetName.Trim();
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_ReplyTargetMissing", "Could not find {NAME}; replies are unavailable.", ("NAME", name)), Colors.Yellow));
				return;
			}
			Instance.OpenCourierFlow(target, allowLetterReply: true);
		}
		catch (Exception ex)
		{
			Log("open courier reply flow failed target=" + (targetHeroId ?? "") + " error=" + ex);
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_OpenReplyFailed", "Could not open the reply: {ERROR}", ("ERROR", ex.Message)), Colors.Red));
		}
	}

	public static bool TrySendNpcDiplomacyLetterToPlayerForExternal(Hero sender, string letterText, out string status)
	{
		status = "";
		try
		{
			if (Instance == null)
			{
				status = "courier_behavior_missing";
				return false;
			}
			return Instance.TryCreateNpcDiplomacyLetterSession(sender, letterText, "external", out status);
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("external npc diplomacy letter failed sender=" + SafeHeroId(sender) + " error=" + ex);
			return false;
		}
	}

	public static bool TrySendNpcLetterToPlayerForExternal(Hero sender, string letterText, string reason, out string status)
	{
		status = "";
		try
		{
			if (Instance == null)
			{
				status = "behavior_not_initialized";
				return false;
			}
			return Instance.TryCreateNpcLetterToPlayerSession(sender, letterText, reason, out status);
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("external npc letter failed sender=" + SafeHeroId(sender) + " reason=" + (reason ?? "") + " error=" + ex);
			return false;
		}
	}

	private void OpenCourierFlow(Hero recipient, bool allowLetterReply = false)
	{
		if (!ModOnboardingBehavior.EnsureSetupReady())
		{
			return;
		}
		if (recipient == null || recipient == Hero.MainHero || recipient.CharacterObject?.IsHero != true)
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_NoRecipient", "No available recipient could be found."), Colors.Yellow));
			return;
		}
		if (IsHeroInPlayerPartyForCourier(recipient))
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_InPlayerParty", "This character is in your party and cannot receive a courier letter."), Colors.Yellow));
			return;
		}
		if (recipient.IsDead)
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_RecipientDead", "This character has died and cannot receive a courier letter."), Colors.Yellow));
			return;
		}
		if (!allowLetterReply && !ShouldShowCourierButtonForExternal(recipient, informationHidden: false))
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_RecipientUnknown", "You do not yet have information about this person and cannot send a letter."), Colors.Yellow));
			return;
		}
		if (HasActiveCourierForHero(recipient))
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_AlreadyActive", "A courier party is already handling a letter to this person."), Colors.Yellow));
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty?.MemberRoster == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_NoPlayerParty", "The player party could not be found."), Colors.Red));
			return;
		}
		TroopRoster available = BuildSelectableCrewRoster(mainParty.MemberRoster);
		if (available.TotalManCount <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_NoCrew", "You currently have no members available to dispatch as couriers."), Colors.Yellow));
			return;
		}
		_pendingFlow = new PendingCourierFlow
		{
			RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
			AllowLetterReply = allowLetterReply,
			Recipient = recipient,
			CrewRoster = null,
			Mode = CourierPayloadMode.Normal
		};
		_letterInputOpen = false;
		PendingCourierFlow flow = _pendingFlow;
		long revision = BeginCourierDraftStep(flow);
		Log("open crew selection recipient=" + SafeHeroId(recipient) + " available=" + available.TotalManCount);
		PartyScreenHelper.OpenScreenWithDummyRoster(
			available,
			TroopRoster.CreateDummyTroopRoster(),
			TroopRoster.CreateDummyTroopRoster(),
			TroopRoster.CreateDummyTroopRoster(),
			ConversationUiLocalization.Object("AF_CONV_UI_Courier_SelectableCrew", "Available courier members"),
			ConversationUiLocalization.Object("AF_CONV_UI_Courier_CrewParty", "Courier party"),
			Math.Max(available.TotalManCount, 0),
			Math.Max(1, available.TotalManCount),
			new PartyPresentationDoneButtonConditionDelegate(CrewSelectionDoneCondition),
			new PartyScreenClosedDelegate((leftOwner, leftMembers, leftPrisoners, rightOwner, rightMembers, rightPrisoners, cancelled) =>
			{
				if (!IsPendingCourierFlowCurrent(flow, revision)) return;
				OnCrewSelectionClosed(leftOwner, leftMembers, leftPrisoners, rightOwner, rightMembers, rightPrisoners, cancelled);
			}),
			new IsTroopTransferableDelegate(CourierCrewTransferableDelegate));
	}

	private static Tuple<bool, TextObject> CrewSelectionDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		if (rightMemberRoster == null || rightMemberRoster.TotalManCount <= 0)
		{
			return new Tuple<bool, TextObject>(false, ConversationUiLocalization.Object("AF_CONV_UI_Courier_MinCrew", "The courier party must contain at least 1 member."));
		}
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static bool CourierCrewTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character != null && !character.IsPlayerCharacter && type == PartyScreenLogic.TroopType.Member;
	}

	private void OnCrewSelectionClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		PendingCourierFlow flow = _pendingFlow;
		long revision = flow?.Revision ?? -1;
		if (!IsPendingCourierFlowCurrent(flow, revision)) return;
		try
		{
			if (fromCancel)
			{
				ResetPendingCourierFlow(flow, revision, "crew_cancel");
				return;
			}
			TroopRoster selected = BuildSelectionRosterFromUi(rightMemberRoster);
			if (selected.TotalManCount <= 0)
			{
				ResetPendingCourierFlow(flow, revision, "crew_empty");
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_MinCrew", "The courier party must contain at least 1 member."), Colors.Yellow));
				return;
			}
			flow.CrewRoster = selected;
			flow.CrewEntries = BuildCargoEntriesFromRoster(selected, "crew");
			Log("crew selected recipient=" + SafeHeroId(flow.Recipient) + " roster=" + RosterSummary(selected));
			ShowCourierModeInquiry();
		}
		catch (Exception ex)
		{
			Log("crew close failed: " + ex);
			ResetPendingCourierFlow(flow, revision, "crew_exception");
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_CrewSelectionFailed", "Courier party selection failed: {ERROR}", ("ERROR", ex.Message)), Colors.Red));
		}
	}

	private void ShowCourierModeInquiry()
	{
		PendingCourierFlow flow = _pendingFlow;
		long revision = BeginCourierDraftStep(flow);
		if (revision < 0) return;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("mode_no_flow");
			return;
		}
		string targetName = flow.Recipient.Name?.ToString() ?? ConversationUiLocalization.Text("AF_CONV_UI_Courier_Recipient", "recipient");
		List<InquiryElement> items = new List<InquiryElement>
		{
			new InquiryElement("normal", ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeNormal", "Write a letter"), null, true, ""),
			new InquiryElement("give", ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeGive", "Send items and write a letter"), null, true, ""),
			new InquiryElement("show", ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeShow", "Show items and write a letter"), null, true, ""),
			new InquiryElement("give_troops", ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeTroops", "Transfer troops and write a letter"), null, true, ""),
			new InquiryElement("give_prisoners", ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModePrisoners", "Transfer prisoners and write a letter"), null, true, ""),
			new InquiryElement("give_settlements", ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeAssets", "Transfer fixed assets and write a letter"), null, true, "")
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeTitle", "Courier mail - {TARGET}", ("TARGET", targetName)),
			ConversationUiLocalization.Text("AF_CONV_UI_Courier_ModeDescription", "Current recipient: {TARGET}\nChoose a delivery method:", ("TARGET", targetName)),
			items,
			true,
			1,
			1,
			ConversationUiLocalization.Text("AF_CONV_UI_Courier_Confirm", "Confirm"),
			ConversationUiLocalization.Text("AF_CONV_UI_Courier_Cancel", "Cancel"),
			selected =>
			{
				if (!IsPendingCourierFlowCurrent(flow, revision)) return;
				if (selected == null || selected.Count == 0)
				{
					ResetPendingFlow("mode_empty");
					return;
				}
				string id = (selected[0]?.Identifier ?? "").ToString();
				if (id == "give")
				{
					BeginPayloadSelection(CourierPayloadMode.Give);
				}
				else if (id == "show")
				{
					BeginPayloadSelection(CourierPayloadMode.Show);
				}
				else if (id == "give_troops")
				{
					BeginPayloadSelection(CourierPayloadMode.GiveTroops);
				}
				else if (id == "give_prisoners")
				{
					BeginPayloadSelection(CourierPayloadMode.GivePrisoners);
				}
				else if (id == "give_settlements")
				{
					BeginPayloadSelection(CourierPayloadMode.GiveSettlements);
				}
				else
				{
					flow.Mode = CourierPayloadMode.Normal;
					flow.SelectedEntries.Clear();
					ShowLetterInput();
				}
			},
			_ => ResetPendingCourierFlow(flow, revision, "mode_cancel"),
			"",
			true), true);
	}

	private void BeginPayloadSelection(CourierPayloadMode mode)
	{
		PendingCourierFlow flow = _pendingFlow;
		long revision = BeginCourierDraftStep(flow);
		if (revision < 0) return;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("payload_no_flow");
			return;
		}
		flow.Mode = mode;
		if (mode == CourierPayloadMode.GiveTroops && !MyBehavior.IsPartyTransferLordEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_TroopRecipientIneligible", "Only lords can discuss troop transfers."), Colors.Yellow));
			ResetPendingFlow("payload_troop_ineligible");
			return;
		}
		if (mode == CourierPayloadMode.GivePrisoners && !MyBehavior.IsPartyTransferLordEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_PrisonerRecipientIneligible", "Only lords can discuss prisoner transfers."), Colors.Yellow));
			ResetPendingFlow("payload_prisoner_ineligible");
			return;
		}
		if (mode == CourierPayloadMode.GiveSettlements && !MyBehavior.IsSettlementTransferLeaderEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_AssetRecipientIneligible", "This recipient has no fixed assets available to receive or discuss."), Colors.Yellow));
			ResetPendingFlow("payload_settlement_ineligible");
			return;
		}
		flow.TradeOptions = BuildCourierTradeOptions(flow, mode);
		flow.SelectedEntries.Clear();
		flow.PendingAmountIndex = 0;
		if (flow.TradeOptions.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(BuildEmptyPayloadMessage(mode), Colors.Yellow));
			ResetPendingFlow("payload_empty");
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		for (int i = 0; i < flow.TradeOptions.Count; i++)
		{
			CourierTradeOption option = flow.TradeOptions[i];
			string hint = ConversationUiLocalization.Text("AF_CONV_UI_Courier_AvailableAmount", "Available: {AMOUNT}", ("AMOUNT", Math.Max(0, option.AvailableAmount)));
			if (option.PartyEntry != null)
			{
				hint = option.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops
					? ConversationUiLocalization.Text("AF_CONV_UI_Courier_TroopHint", "Available: {AMOUNT} | Wage: {WAGE} denars/day | Hire price: {PRICE} denars/person", ("AMOUNT", option.AvailableAmount), ("WAGE", option.PartyEntry.WageDenarsPerDay), ("PRICE", option.PartyEntry.HirePriceDenarsPerUnit))
					: ConversationUiLocalization.Text("AF_CONV_UI_Courier_PrisonerHint", "Available: {AMOUNT} | Purchase price: {PRICE} denars/person", ("AMOUNT", option.AvailableAmount), ("PRICE", option.PartyEntry.BuyPriceDenarsPerUnit));
				string sourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(option.PartyEntry);
				if (!string.IsNullOrWhiteSpace(sourceLabel))
				{
					hint += ConversationUiLocalization.Text("AF_CONV_UI_Courier_SourceHint", " | Source: {SOURCE}", ("SOURCE", sourceLabel));
				}
			}
			else if (option.SettlementEntry != null)
			{
				hint = ConversationUiLocalization.Text("AF_CONV_UI_Courier_AssetHint", "Type: {TYPE} | Daily income: {INCOME} denars | Suggested full payment: {PRICE} denars", ("TYPE", string.IsNullOrWhiteSpace(option.SettlementEntry.TypeLabel) ? ConversationUiLocalization.Text("AF_CONV_UI_Courier_FixedAssets", "Fixed assets") : option.SettlementEntry.TypeLabel), ("INCOME", Math.Max(0, option.SettlementEntry.DailyIncomeDenars)), ("PRICE", Math.Max(0, option.SettlementEntry.GuidePriceDenars)));
			}
			string displayName = option.Name;
			displayName = GetCourierLetterTransferDisplayTitleForExternal(displayName);
			string prisonerSourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(option.PartyEntry);
			if (!string.IsNullOrWhiteSpace(prisonerSourceLabel))
			{
				displayName += ConversationUiLocalization.Text("AF_CONV_UI_Courier_SourceSuffix", " (Source: {SOURCE})", ("SOURCE", prisonerSourceLabel));
			}
			list.Add(new InquiryElement(i, displayName + " (×" + Math.Max(1, option.AvailableAmount) + ")", null, true, hint));
		}
		string targetName = flow.Recipient.Name?.ToString() ?? ConversationUiLocalization.Text("AF_CONV_UI_Courier_Recipient", "recipient");
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			BuildPayloadTitle(mode, targetName),
			BuildPayloadDescription(mode, targetName),
			list,
			true,
			1,
			list.Count,
			ConversationUiLocalization.Text("AF_CONV_UI_Courier_Confirm", "Confirm"),
			ConversationUiLocalization.Text("AF_CONV_UI_Courier_Cancel", "Cancel"),
			selected =>
			{
				if (!IsPendingCourierFlowCurrent(flow, revision)) return;
				OnPayloadResourcesSelected(selected);
			},
			_ => ResetPendingCourierFlow(flow, revision, "payload_cancel"),
			"",
			true), true);
	}

	private void OnPayloadResourcesSelected(List<InquiryElement> selected)
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow == null || selected == null || selected.Count == 0)
		{
			ResetPendingFlow("payload_selected_empty");
			return;
		}
		flow.SelectedEntries.Clear();
		foreach (InquiryElement element in selected)
		{
			int index = -1;
			try
			{
				index = (int)element.Identifier;
			}
			catch
			{
				index = -1;
			}
			if (index < 0 || index >= flow.TradeOptions.Count)
			{
				continue;
			}
			CourierTradeOption option = flow.TradeOptions[index];
			flow.SelectedEntries.Add(new CourierCargoEntry
			{
				Kind = option.Kind,
				Id = option.Id,
				Name = option.Name,
				Amount = option.SettlementEntry != null ? 1 : 0,
				GuidePriceDenars = Math.Max(0, option.GuidePriceDenars),
				IsHero = option.PartyEntry?.IsHero ?? false,
				SourceSettlementId = (option.PartyEntry?.SourceSettlement?.StringId ?? "").Trim()
			});
		}
		if (flow.SelectedEntries.Count == 0)
		{
			ResetPendingFlow("payload_selected_no_entries");
			return;
		}
		if (flow.Mode == CourierPayloadMode.GiveSettlements)
		{
			ShowLetterInput();
			return;
		}
		ShowPayloadAmountInquiry();
	}

	private void ShowPayloadAmountInquiry()
	{
		PendingCourierFlow flow = _pendingFlow;
		long revision = BeginCourierDraftStep(flow);
		if (revision < 0) return;
		if (flow == null)
		{
			ResetPendingFlow("amount_no_flow");
			return;
		}
		if (flow.PendingAmountIndex >= flow.SelectedEntries.Count)
		{
			ShowLetterInput();
			return;
		}
		CourierCargoEntry entry = flow.SelectedEntries[flow.PendingAmountIndex];
		CourierTradeOption option = flow.TradeOptions.FirstOrDefault(x => string.Equals(x.Kind, entry.Kind, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Id ?? "", entry.Id ?? "", StringComparison.OrdinalIgnoreCase));
		int max = Math.Max(0, option?.AvailableAmount ?? 0);
		if (max <= 0)
		{
			flow.PendingAmountIndex++;
			ShowPayloadAmountInquiry();
			return;
		}
		string title = flow.Mode == CourierPayloadMode.Show ? ConversationUiLocalization.Text("AF_CONV_UI_Courier_ShowAmount", "Amount to show") : (flow.Mode == CourierPayloadMode.Give ? ConversationUiLocalization.Text("AF_CONV_UI_Courier_SendAmount", "Amount to send") : ConversationUiLocalization.Text("AF_CONV_UI_Courier_TransferAmount", "Amount to transfer"));
		string entryDisplayName = GetCourierLetterTransferDisplayTitleForExternal(entry.Name);
		string text = ConversationUiLocalization.Text("AF_CONV_UI_Courier_AmountDescription", "[{INDEX}/{COUNT}] {ITEM}: maximum {MAX}.\nEnter an integer from 1 to {MAX}:", ("INDEX", flow.PendingAmountIndex + 1), ("COUNT", flow.SelectedEntries.Count), ("ITEM", entryDisplayName), ("MAX", max));
		InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, ConversationUiLocalization.Text("AF_CONV_UI_Courier_Confirm", "Confirm"), ConversationUiLocalization.Text("AF_CONV_UI_Courier_Back", "Back"), input =>
		{
			if (!IsPendingCourierFlowCurrent(flow, revision)) return;
			if (!int.TryParse(input, out var amount) || amount <= 0 || amount > max)
			{
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_InvalidAmount", "Enter a valid amount."), Colors.Yellow));
				ShowPayloadAmountInquiry();
				return;
			}
			entry.Amount = amount;
			flow.PendingAmountIndex++;
			ShowPayloadAmountInquiry();
		}, () =>
		{
			if (IsPendingCourierFlowCurrent(flow, revision)) BeginPayloadSelection(flow.Mode);
		}), true);
	}

	private void ShowLetterInput()
	{
		PendingCourierFlow flow = _pendingFlow;
		long revision = BeginCourierDraftStep(flow, readyToSend: true);
		if (revision < 0) return;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("letter_no_flow");
			return;
		}
		string targetName = flow.Recipient.Name?.ToString() ?? ConversationUiLocalization.Text("AF_CONV_UI_Courier_Recipient", "recipient");
		_letterInputOpen = true;
		string letterTitle = ConversationUiLocalization.Text("AF_CONV_UI_Courier_WriteTitle", "Letter to {TARGET}", "TARGET", targetName);
		bool opened = CourierLetterInputPopup.Show(letterTitle, "", "", "", input =>
		{
			if (!IsPendingCourierFlowCurrent(flow, revision)) return;
			_letterInputOpen = false;
			OnLetterConfirmed(input);
		}, () =>
		{
			ResetPendingCourierFlow(flow, revision, "letter_cancel");
		});
		if (!opened)
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(letterTitle, "", true, true, ConversationUiLocalization.Text("AF_CONV_UI_Courier_Send", "Send"), ConversationUiLocalization.Text("AF_CONV_UI_Courier_Cancel", "Cancel"), input =>
			{
				if (!IsPendingCourierFlowCurrent(flow, revision)) return;
				_letterInputOpen = false;
				OnLetterConfirmed(input);
			}, () =>
			{
				ResetPendingCourierFlow(flow, revision, "letter_cancel_fallback");
			}), true);
		}
	}

	private void OnLetterConfirmed(string input)
	{
		PendingCourierFlow flow = _pendingFlow;
		long revision = flow?.Revision ?? -1;
		if (!IsPendingCourierFlowCurrent(flow, revision) || !flow.ReadyToSend) return;
		if (flow == null || flow.Recipient == null)
		{
			ResetPendingFlow("confirm_no_flow");
			return;
		}
		if (string.IsNullOrWhiteSpace(input))
		{
			ResetPendingFlow("confirm_empty");
			return;
		}
		try
		{
			if (DispatchCourierDraft(flow, revision, input, out string reason) == null)
			{
				ResetPendingCourierFlow(flow, revision, "confirm_rejected");
				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_DraftExpired", "The courier draft has expired. Select the members and attachments again."), Colors.Yellow));
				Log("courier draft rejected reason=" + reason);
			}
		}
		catch (Exception ex)
		{
			Log("confirm failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_DispatchFailed", "Courier dispatch failed: {ERROR}", ("ERROR", ex.Message)), Colors.Red));
			ResetPendingCourierFlow(flow, revision, "confirm_exception", allowClaimed: true);
		}
	}

	// Shared UI/module dispatch. No callback may replay a claimed draft, even after partial failure.
	private CourierSession DispatchCourierDraft(PendingCourierFlow flow, long revision, string input, out string reason, CoreDialogueOperation operation = null)
	{
		reason = ValidateCourierDraftForDispatch(flow, revision, input);
		if (reason != null) return null;
		if (!IsPendingCourierFlowCurrent(flow, revision) || !flow.ReadyToSend)
		{
			reason = "courier.context_unavailable";
			return null;
		}
		if (operation != null && !operation.TryBegin())
		{
			reason = "dialogue.cancelled_before_start";
			return null;
		}
		flow.Claimed = true;
		flow.ReadyToSend = false;
		InvalidateCourierDraftTickets();
		operation?.MarkOwnerAdmitted();
		try
		{
			CourierSession session = CreateCourierSession(flow, input.Trim());
			if (session == null)
			{
				reason = "courier.dispatch_unconfirmed";
				return null;
			}
			if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(flow.RuntimeGeneration))
				throw new OperationCanceledException("Courier dispatch source expired.");
			lock (_sessionLock) _sessions[session.Id] = session;
			BindModuleCourierSession(operation, session, flow);
			AddCourierRuntimeIndex(session);
			Log("session created id=" + session.Id + " recipient=" + session.RecipientHeroId + " party=" + session.CourierPartyId + " mode=" + session.PayloadMode + " entries=" + session.Entries.Count);
			StartCourierReplyGeneration(session, "created_preflight");
			InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Courier_Dispatched", "The courier party has departed for {TARGET}.", ("TARGET", session.RecipientName)), Colors.Green));
			ResetPendingCourierFlow(flow, revision, "confirm_done", allowClaimed: true);
			ProcessSession(session);
			return session;
		}
		finally { ResetPendingCourierFlow(flow, revision, "dispatch_finished", allowClaimed: true); }
	}

	private CourierSession CreateCourierSession(PendingCourierFlow flow, string letter)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || flow?.Recipient == null || flow.CrewRoster == null || flow.CrewRoster.TotalManCount <= 0)
		{
			throw new InvalidOperationException(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0615"));
		}
		string id = NewSessionId();
		TroopRoster emptyMembers = TroopRoster.CreateDummyTroopRoster();
		TroopRoster emptyPrisoners = TroopRoster.CreateDummyTroopRoster();
		float speed = Math.Max(4f, mainParty.Speed) * 4f;
		TextObject name = ConversationUiLocalization.Object("AF_CONV_UI_Courier_PartyBaseName", "AnimusForge Courier Party");
		MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(mainParty.Position, 0.05f, mainParty.CurrentSettlement, name, Clan.PlayerClan, emptyMembers, emptyPrisoners, Hero.MainHero, "", "", speed, true);
		if (courier == null)
		{
			throw new InvalidOperationException(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0616"));
		}
		courier.IsVisible = true;
		ApplyCourierMapBannerVisual(courier, "create");
		courier.Party.SetCustomName(ConversationUiLocalization.Object("AF_CONV_UI_Courier_PartyName", "Courier party - {NAME}", ("NAME", flow.Recipient.Name ?? ConversationUiLocalization.Object("AF_CONV_UI_Courier_Recipient", "recipient"))));
		courier.SetMoveModeHold();
		ApplyCourierAiOverrides(courier, "create");

		CourierSession session = new CourierSession
		{
			Id = id,
			RecipientHeroId = SafeHeroId(flow.Recipient),
			RecipientName = flow.Recipient.Name?.ToString() ?? "",
			CourierPartyId = courier.StringId,
			Stage = CourierStage.Outbound.ToString(),
			PayloadMode = flow.Mode.ToString(),
			LetterText = letter,
			Entries = CloneEntries(flow.SelectedEntries),
			CrewEntries = BuildCargoEntriesFromRoster(flow.CrewRoster, "crew")
		};
		MoveRosterFromMainParty(flow.CrewRoster, courier, "crew");
		AssignCourierLeader(courier);
		EnsureCourierCampaignIdentity(courier, session, "create_session");
		PrepareOutgoingPayload(session, courier);
		session.DeliveryFactText = BuildDeliveryFactText(session, delivered: false, flow.Recipient);
		PlayerNotorietyBehavior.NoteCourierSentForExternal(flow.Recipient);
		return session;
	}

	private void PrepareOutgoingPayload(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		if (ParsePayloadMode(session.PayloadMode) == CourierPayloadMode.Show)
		{
			Log("payload show mode staged session=" + session.Id + " entries=" + session.Entries.Count);
			return;
		}
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			if (string.Equals(entry.Kind, "gold", StringComparison.OrdinalIgnoreCase))
			{
				int amount = Math.Min(Math.Max(0, Hero.MainHero?.Gold ?? 0), entry.Amount);
				if (amount > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount, true);
				}
				entry.Amount = amount;
				session.EscrowGold += amount;
				Log("escrow gold session=" + session.Id + " amount=" + amount);
			}
			else if (string.Equals(entry.Kind, "item", StringComparison.OrdinalIgnoreCase))
			{
				ItemObject item;
				int moved = MyBehavior.TransferItemsFromRosterByStringId(MobileParty.MainParty?.ItemRoster, courier.ItemRoster, entry.Id, entry.Amount, out item);
				entry.Amount = moved;
				if (item != null)
				{
					entry.Name = item.Name?.ToString() ?? entry.Name;
				}
				Log("escrow item session=" + session.Id + " item=" + entry.Id + " moved=" + moved);
			}
			else if (string.Equals(entry.Kind, "troop", StringComparison.OrdinalIgnoreCase))
			{
				MoveCharacterFromMainMembersToParty(entry.Id, entry.Amount, courier, entry.IsHero);
				Log("escrow troop session=" + session.Id + " troop=" + entry.Id + " amount=" + entry.Amount);
			}
			else if (string.Equals(entry.Kind, "prisoner", StringComparison.OrdinalIgnoreCase))
			{
				int moved = MoveCharacterFromPlayerPrisonerSourceToParty(entry.Id, entry.Amount, courier, entry.IsHero, entry.SourceSettlementId);
				entry.Amount = moved;
				Log("escrow prisoner session=" + session.Id + " troop=" + entry.Id + " moved=" + moved + " sourceSettlement=" + (entry.SourceSettlementId ?? ""));
			}
		}
	}

	private bool TryCreateNpcInitiatedLetterSession(NpcInitiatedLetterCandidate candidate, NpcInitiatedLetterMotive motive, out string status)
	{
		status = "";
		Hero sender = candidate?.Sender;
		try
		{
			if (sender == null || motive == null || sender == Hero.MainHero || sender.IsDead || sender.IsPrisoner || sender.IsFugitive || sender.PartyBelongedToAsPrisoner != null)
			{
				status = "sender_unavailable";
				return false;
			}
			if (HasAnyActiveNpcInitiatedInboundCourier() || HasActiveInboundCourierFromSender(sender))
			{
				status = "active_inbound_exists";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out CampaignVec2 startPosition, out Settlement startSettlement))
			{
				status = "sender_location_missing";
				return false;
			}
			TroopRoster members = TroopRoster.CreateDummyTroopRoster();
			TroopRoster prisoners = TroopRoster.CreateDummyTroopRoster();
			CharacterObject messenger = ResolveNpcCourierMessengerTroop(sender);
			if (messenger != null)
			{
				members.AddToCounts(messenger, 1, false, 0, 0, true, -1);
			}
			float baseSpeed = Math.Max(4f, sender.PartyBelongedTo?.Speed ?? MobileParty.MainParty?.Speed ?? 4f);
			Clan ownerClan = sender.Clan ?? sender.Clan?.Kingdom?.RulingClan ?? Clan.PlayerClan;
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, ConversationUiLocalization.Object("AF_CONV_UI_Courier_NpcPartyBaseName", "AnimusForge NPC Courier"), ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_npc_initiated_inbound");
			courier.Party.SetCustomName(ConversationUiLocalization.Object("AF_CONV_UI_Courier_PartyName", "Courier party - {NAME}", ("NAME", sender.Name ?? ConversationUiLocalization.Object("AF_CONV_UI_Courier_Npc", "NPC"))));
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "create_npc_initiated_inbound");
			CourierSession session = new CourierSession
			{
				Id = NewSessionId(),
				Direction = CourierDirectionInboundToPlayer,
				SenderHeroId = SafeHeroId(sender),
				SenderName = sender.Name?.ToString() ?? "",
				RecipientHeroId = SafeHeroId(Hero.MainHero),
				RecipientName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? "",
				CourierPartyId = courier.StringId,
				Stage = CourierStage.Outbound.ToString(),
				PayloadMode = CourierPayloadMode.Normal.ToString(),
				LetterText = motive.FallbackLetter ?? "",
				InboundFallbackLetter = motive.FallbackLetter ?? "",
				InboundLetterKind = motive.LetterKind ?? InboundLetterKindPersonal,
				InboundMotiveType = motive.MotiveType ?? "",
				InboundNeedType = motive.NeedType ?? "",
				InboundIntentFact = motive.FactText ?? "",
				InboundIntentText = motive.IntentText ?? "",
				InboundEventKey = motive.EventKey ?? "",
				IsNpcInitiated = true,
				NpcInitiatedBondScore = candidate.BondScore,
				CrewEntries = BuildCargoEntriesFromRoster(members, "crew")
			};
			EnsureCourierCampaignIdentity(courier, session, "create_npc_initiated_session");
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: false, sender);
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session, courier);
			StartInboundLetterGeneration(session, "npc_initiated_preflight");
			ProcessSession(session);
			status = "created";
			return true;
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("create npc initiated letter failed sender=" + SafeHeroId(sender) + " error=" + ex);
			return false;
		}
	}

	private bool TryCreateNpcDiplomacyLetterSession(Hero sender, string letterText, string reason, out string status)
	{
		status = "";
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (!CanNpcKingSendDiplomacyLetter(sender, playerKingdom, out status))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(letterText))
			{
				status = "empty_letter";
				return false;
			}
			if (HasAnyActiveNpcInitiatedInboundCourier() || HasActiveInboundCourierFromSender(sender))
			{
				status = "active_inbound_exists";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out CampaignVec2 startPosition, out Settlement startSettlement))
			{
				status = "sender_location_missing";
				return false;
			}
			string id = NewSessionId();
			TroopRoster members = TroopRoster.CreateDummyTroopRoster();
			TroopRoster prisoners = TroopRoster.CreateDummyTroopRoster();
			CharacterObject messenger = ResolveNpcCourierMessengerTroop(sender);
			if (messenger != null)
			{
				members.AddToCounts(messenger, 1, false, 0, 0, true, -1);
			}
			float baseSpeed = Math.Max(4f, sender.PartyBelongedTo?.Speed ?? MobileParty.MainParty?.Speed ?? 4f);
			Clan ownerClan = sender.Clan ?? sender.Clan?.Kingdom?.RulingClan ?? Clan.PlayerClan;
			TextObject name = ConversationUiLocalization.Object("AF_CONV_UI_Courier_NpcPartyBaseName", "AnimusForge NPC Courier");
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, name, ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_inbound");
			courier.Party.SetCustomName(ConversationUiLocalization.Object("AF_CONV_UI_Courier_PartyName", "Courier party - {NAME}", ("NAME", sender.Name ?? ConversationUiLocalization.Object("AF_CONV_UI_Courier_Npc", "NPC"))));
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "create_inbound");
			CourierSession session = new CourierSession
			{
				Id = id,
				Direction = CourierDirectionInboundToPlayer,
				SenderHeroId = SafeHeroId(sender),
				SenderName = sender.Name?.ToString() ?? "",
				RecipientHeroId = SafeHeroId(Hero.MainHero),
				RecipientName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? "",
				CourierPartyId = courier.StringId,
				Stage = CourierStage.Outbound.ToString(),
				PayloadMode = CourierPayloadMode.Normal.ToString(),
				LetterText = letterText.Trim(),
				InboundFallbackLetter = letterText.Trim(),
				InboundLetterKind = InboundLetterKindDiplomacy,
				InboundMotiveType = LetterMotiveDiplomacy,
				InboundIntentText = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0617"),
				IsNpcInitiated = true,
				CrewEntries = BuildCargoEntriesFromRoster(members, "crew")
			};
			EnsureCourierCampaignIdentity(courier, session, "create_inbound_session");
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: false, sender);
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session, courier);
			StartInboundLetterGeneration(session, "created_preflight");
			float nowDays = NowDays();
			string senderId = SafeHeroId(sender);
			if (!string.IsNullOrWhiteSpace(senderId))
			{
				_npcDiplomacyLetterSenderCooldownUntilDays[senderId] = nowDays + NpcDiplomacyLetterSenderCooldownDays;
			}
			_npcDiplomacyLetterGlobalCooldownUntilDays = nowDays + NpcDiplomacyLetterGlobalCooldownDays;
			Log("npc diplomacy letter session created id=" + session.Id + " sender=" + session.SenderHeroId + " party=" + session.CourierPartyId + " reason=" + (reason ?? ""));
			ProcessSession(session);
			status = "created";
			return true;
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("create npc diplomacy letter failed sender=" + SafeHeroId(sender) + " error=" + ex);
			return false;
		}
	}

	private bool TryCreateNpcLetterToPlayerSession(Hero sender, string letterText, string reason, out string status)
	{
		status = "";
		try
		{
			if (sender == null || sender == Hero.MainHero || sender.IsDead || sender.IsPrisoner || sender.IsFugitive || sender.PartyBelongedToAsPrisoner != null)
			{
				status = "sender_unavailable";
				return false;
			}
			if (string.IsNullOrWhiteSpace(letterText))
			{
				status = "empty_letter";
				return false;
			}
			if (HasAnyActiveNpcInitiatedInboundCourier() || HasActiveInboundCourierFromSender(sender))
			{
				status = "active_inbound_exists";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out CampaignVec2 startPosition, out Settlement startSettlement))
			{
				status = "sender_location_missing";
				return false;
			}
			string id = NewSessionId();
			TroopRoster members = TroopRoster.CreateDummyTroopRoster();
			TroopRoster prisoners = TroopRoster.CreateDummyTroopRoster();
			CharacterObject messenger = ResolveNpcCourierMessengerTroop(sender);
			if (messenger != null)
			{
				members.AddToCounts(messenger, 1, false, 0, 0, true, -1);
			}
			float baseSpeed = Math.Max(4f, sender.PartyBelongedTo?.Speed ?? MobileParty.MainParty?.Speed ?? 4f);
			Clan ownerClan = sender.Clan ?? sender.Clan?.Kingdom?.RulingClan ?? Clan.PlayerClan;
			TextObject name = ConversationUiLocalization.Object("AF_CONV_UI_Courier_NpcPartyBaseName", "AnimusForge NPC Courier");
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, name, ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_inbound_generic");
			courier.Party.SetCustomName(ConversationUiLocalization.Object("AF_CONV_UI_Courier_PartyName", "Courier party - {NAME}", ("NAME", sender.Name ?? ConversationUiLocalization.Object("AF_CONV_UI_Courier_Npc", "NPC"))));
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "create_inbound_generic");
			CourierSession session = new CourierSession
			{
				Id = id,
				Direction = CourierDirectionInboundToPlayer,
				SenderHeroId = SafeHeroId(sender),
				SenderName = sender.Name?.ToString() ?? "",
				RecipientHeroId = SafeHeroId(Hero.MainHero),
				RecipientName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? "",
				CourierPartyId = courier.StringId,
				Stage = CourierStage.Outbound.ToString(),
				PayloadMode = CourierPayloadMode.Normal.ToString(),
				LetterText = letterText.Trim(),
				InboundFallbackLetter = letterText.Trim(),
				InboundLetterKind = InboundLetterKindExternal,
				InboundMotiveType = InboundLetterKindExternal,
				IsNpcInitiated = true,
				CrewEntries = BuildCargoEntriesFromRoster(members, "crew"),
				ReplyGenerated = true
			};
			EnsureCourierCampaignIdentity(courier, session, "create_inbound_generic_session");
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: false, sender);
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session, courier);
			Log("npc generic letter session created id=" + session.Id + " sender=" + session.SenderHeroId + " party=" + session.CourierPartyId + " reason=" + (reason ?? ""));
			ProcessSession(session);
			status = "created";
			return true;
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("create npc generic letter failed sender=" + SafeHeroId(sender) + " reason=" + (reason ?? "") + " error=" + ex);
			return false;
		}
	}

	private bool HasActiveInboundCourierFromSender(Hero sender)
	{
		string senderId = SafeHeroId(sender);
		if (string.IsNullOrWhiteSpace(senderId))
		{
			return false;
		}
		lock (_sessionLock)
		{
			return _sessions.Values.Any(x => x != null
				&& IsInboundToPlayer(x)
				&& !IsTerminalStage(x)
				&& string.Equals((x.SenderHeroId ?? "").Trim(), senderId, StringComparison.OrdinalIgnoreCase));
		}
	}

	private HashSet<string> BuildActiveInboundCourierSenderIdSnapshot()
	{
		HashSet<string> senderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		lock (_sessionLock)
		{
			foreach (CourierSession session in _sessions.Values)
			{
				if (session == null || !IsInboundToPlayer(session) || IsTerminalStage(session))
				{
					continue;
				}
				string senderId = (session.SenderHeroId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(senderId))
				{
					senderIds.Add(senderId);
				}
			}
		}
		return senderIds;
	}

	private static bool TryGetNpcCourierStart(Hero sender, out CampaignVec2 position, out Settlement settlement)
	{
		position = CampaignVec2.Invalid;
		settlement = null;
		try
		{
			MobileParty party = sender?.PartyBelongedTo;
			if (party != null && party.IsActive)
			{
				position = party.Position;
				settlement = party.CurrentSettlement;
				return IsValidCampaignPosition(position);
			}
			settlement = sender?.CurrentSettlement ?? sender?.StayingInSettlement;
			if (settlement != null)
			{
				position = settlement.GatePosition;
				return IsValidCampaignPosition(position);
			}
		}
		catch
		{
		}
		return false;
	}

	private static CharacterObject ResolveNpcCourierMessengerTroop(Hero sender)
	{
		try
		{
			CharacterObject troop = sender?.Culture?.BasicTroop;
			if (troop != null && !troop.IsHero)
			{
				return troop;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject troop = sender?.Clan?.Culture?.BasicTroop;
			if (troop != null && !troop.IsHero)
			{
				return troop;
			}
		}
		catch
		{
		}
		try
		{
			return Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>()?.FirstOrDefault(x => x != null && x.IsBasicTroop && x.IsSoldier && !x.IsHero);
		}
		catch
		{
			return null;
		}
	}

	private static TroopRoster BuildSelectableCrewRoster(TroopRoster source)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in SnapshotRoster(source))
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			roster.AddToCounts(character, item.Number, false, item.WoundedNumber, item.Xp, true, -1);
		}
		return roster;
	}
}
