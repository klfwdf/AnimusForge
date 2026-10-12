using AnimusForge;
using System;

using System.Collections.Concurrent;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Linq;

using System.Reflection;

using System.Runtime.InteropServices;

using System.Text;

using System.Text.RegularExpressions;

using System.Threading;

using System.Threading.Tasks;

using AnimusForge.SceneActions.Core;

using AnimusForge.SiegeAftermathIntervention;

using AnimusForge.XihaiAction;

using AnimusForge.Refactor.Adapters;

using AnimusForge.Refactor.Contracts;

using AnimusForge.Refactor.Modules;

using AnimusForge.Refactor.Runtime;

using RichExecutions.Core;

using RichExecutions.Scene;

using SandBox;

using SandBox.Missions.AgentBehaviors;

using SandBox.Missions.MissionLogics;

using SandBox.Missions.MissionLogics.Towns;

using SandBox.Objects.AnimationPoints;

using SandBox.Objects.Usables;

using TaleWorlds.CampaignSystem;

using TaleWorlds.CampaignSystem.Actions;

using TaleWorlds.CampaignSystem.ComponentInterfaces;

using TaleWorlds.CampaignSystem.Conversation;

using TaleWorlds.CampaignSystem.Encounters;

using TaleWorlds.CampaignSystem.Party;

using TaleWorlds.CampaignSystem.Roster;

using TaleWorlds.CampaignSystem.Settlements;

using TaleWorlds.CampaignSystem.Settlements.Locations;

using TaleWorlds.CampaignSystem.Siege;

using TaleWorlds.Core;

using TaleWorlds.Engine;

using TaleWorlds.InputSystem;

using TaleWorlds.Library;

using TaleWorlds.Localization;

using TaleWorlds.MountAndBlade;

using TaleWorlds.MountAndBlade.Missions;



using static AnimusForge.SceneMovementController;



using static AnimusForge.ShoutBehavior;

namespace AnimusForge;



internal sealed class SceneTradeController

{

    private readonly SceneTradeControllerPorts _ports;

    internal string TradeRequestMode;

    internal bool TradeOwnsState, TradeStaged;

    internal string TradeSummary = "";

    private long _flowRevision;

    private long _uiStepRevision;

    private bool _flowOpen, _commitConsumed;

    internal long FlowRevision => _flowRevision;

    // Synchronous UI-only capture. The original guarded callbacks remain authoritative.

    internal Func<MultiSelectionInquiryData, bool> InlineInquiryCapture;

    private void PresentTradeInquiry(MultiSelectionInquiryData data)

    {

        if (InlineInquiryCapture?.Invoke(data) == true) return;

        MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);

    }

    private Mission _flowMission;

    private long _flowGeneration;



    internal SceneTradeController(SceneTradeControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }



	internal List<ShoutTradeResourceOption> _shoutTradeOptions = new List<ShoutTradeResourceOption>();



	internal List<ShoutPendingTradeItem> _shoutPendingTradeItems = new List<ShoutPendingTradeItem>();



	internal int _shoutPendingTradeItemIndex = 0;



	internal ShoutChatMode _shoutTradeMode = ShoutChatMode.Normal;



	internal NpcDataPacket _shoutTradeTargetNpc = null;



	internal Agent _shoutTradeTargetAgentSnapshot = null;

    internal ConversationManager _shoutTradeNativeManager;

    internal Mission _shoutTradeNativeMission;

    internal Agent _shoutTradeNativeAgent;



	internal bool _shoutTradeActionOnly = false;



	internal Hero _shoutTradeTargetHeroOverride = null;



	internal CharacterObject _shoutTradeTargetCharacterOverride = null;



	internal Action _shoutTradeActionOnlyFinished = null;



	internal int _shoutTradeActionFactSequence = 0;



	internal static bool OpenNativeConversationGiveShowForExternal(Action onFinished = null)

	{

		try

		{

			ShoutBehavior instance = CurrentInstance;

			if (instance == null || !TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))

			{

				return false;

			}

			targetHero ??= targetCharacter?.HeroObject;

			targetCharacter ??= targetHero?.CharacterObject;

			NpcDataPacket targetNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);

			targetNpc.AgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);

			if (string.IsNullOrWhiteSpace(targetNpc.Name))

			{

				targetNpc.Name = npcName;

			}

			instance._j17SceneTradeController.OpenNativeConversationGiveShowMenu(targetNpc, targetHero, targetCharacter, onFinished);

			return true;

		}

		catch (Exception ex)

		{

			Logger.Log("NativeConversationTrade", "[WARN] Failed to open give/show menu: " + ex.Message);

			return false;

		}

	}



	internal void OpenNativeConversationGiveShowMenu(NpcDataPacket targetNpc, Hero targetHero, CharacterObject targetCharacter, Action onFinished)

    {

        StartFlowIdentity();

        _shoutTradeNativeManager = Campaign.Current?.ConversationManager;

        _shoutTradeNativeMission = Mission.Current;

        _shoutTradeNativeAgent = _shoutTradeNativeManager?.OneToOneConversationAgent as Agent;

        long uiRevision = _flowRevision;

        long uiStepRevision = ++_uiStepRevision;

		_shoutTradeActionOnly = true;

		_shoutTradeTargetHeroOverride = targetHero;

		_shoutTradeTargetCharacterOverride = targetCharacter ?? targetHero?.CharacterObject;

		_shoutTradeActionOnlyFinished = onFinished;

		_shoutTradeTargetNpc = targetNpc;

		PauseGame();

		string targetName = (targetNpc?.Name ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? ConversationUiLocalization.Text("AF_CONV_UI_Trade_TargetFallback", "the other party")).Trim();

		List<InquiryElement> inquiryElements = new List<InquiryElement>

		{

			new InquiryElement("give", ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveItems", "Give items"), null, isEnabled: true, ""),

			new InquiryElement("show", ConversationUiLocalization.Text("AF_CONV_UI_Trade_ShowItems", "Show items"), null, isEnabled: true, ""),

			new InquiryElement("give_troops", ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveTroops", "Give troops"), null, isEnabled: true, ""),

			new InquiryElement("give_prisoners", ConversationUiLocalization.Text("AF_CONV_UI_Trade_GivePrisoners", "Give prisoners"), null, isEnabled: true, ""),

			new InquiryElement("give_settlements", ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveAssets", "Transfer fixed assets"), null, isEnabled: true, "")

		};

		MultiSelectionInquiryData data = new MultiSelectionInquiryData(

			ConversationUiLocalization.Text("AF_CONV_UI_Trade_MenuTitle", "Give/Show - {TARGET}", ("TARGET", targetName)),

			ConversationUiLocalization.Text("AF_CONV_UI_Trade_MenuDescription", "Current target: {TARGET}\nThis performs only a give or show action and does not request an AI reply. The action is written to AnimusForge conversation history for later AI conversations.", ("TARGET", targetName)), inquiryElements, isExitShown: true, 1, 1,

			ConversationUiLocalization.Text("AF_CONV_UI_Courier_Confirm", "Confirm"), ConversationUiLocalization.Text("AF_CONV_UI_Courier_Cancel", "Cancel"), delegate(List<InquiryElement> selected)

		{

            if (!IsUiCurrent(uiRevision, uiStepRevision)) return;

			if (selected == null || selected.Count == 0)

			{

				ResetShoutTradeState();

				ResumeGame();

				FinishShoutTradeActionOnlyIfNeeded();

				return;

			}

			string choice = (selected[0]?.Identifier ?? "").ToString();

			if (choice == "give")

			{

				BeginShoutTradeFlow(targetNpc, ShoutChatMode.Give);

			}

			else if (choice == "show")

			{

				BeginShoutTradeFlow(targetNpc, ShoutChatMode.Show);

			}

			else if (choice == "give_troops")

			{

				BeginShoutTradeFlow(targetNpc, ShoutChatMode.GiveTroops);

			}

			else if (choice == "give_prisoners")

			{

				BeginShoutTradeFlow(targetNpc, ShoutChatMode.GivePrisoners);

			}

			else if (choice == "give_settlements")

			{

				BeginShoutTradeFlow(targetNpc, ShoutChatMode.GiveSettlements);

			}

			else

			{

				ResetShoutTradeState();

				ResumeGame();

				FinishShoutTradeActionOnlyIfNeeded();

			}

		}, delegate

		{

            if (!IsUiCurrent(uiRevision, uiStepRevision)) return;

			ResetShoutTradeState();

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

		}, "", isSeachAvailable: true);

		PresentTradeInquiry(data);

	}



	internal static bool IsShoutTradeShowMode(ShoutChatMode mode)

	{

		return mode == ShoutChatMode.Show;

	}



	internal static bool IsShoutPartyTransferMode(ShoutChatMode mode)

	{

		return mode == ShoutChatMode.GiveTroops || mode == ShoutChatMode.GivePrisoners;

	}



	internal static bool IsShoutSettlementTransferMode(ShoutChatMode mode)

	{

		return mode == ShoutChatMode.GiveSettlements;

	}



	internal static bool IsShoutTroopTransferMode(ShoutChatMode mode)

	{

		return mode == ShoutChatMode.GiveTroops;

	}



	internal static bool IsShoutPrisonerTransferMode(ShoutChatMode mode)

	{

		return mode == ShoutChatMode.GivePrisoners;

	}



	internal static bool IsShoutTradeGiveMode(ShoutChatMode mode)

	{

		return mode == ShoutChatMode.Give || mode == ShoutChatMode.GiveTroops || mode == ShoutChatMode.GivePrisoners || mode == ShoutChatMode.GiveSettlements;

	}



	internal int GetShoutTradeTargetAgentIndex()

	{

		return _shoutTradeTargetNpc?.AgentIndex ?? (-1);

	}



	internal void BeginShoutTradeFlow(NpcDataPacket targetNpc, ShoutChatMode mode)

    {

        StartFlowIdentity();

        long uiRevision = _flowRevision;

        long uiStepRevision = ++_uiStepRevision;

		_shoutTradeTargetNpc = targetNpc;

		_shoutTradeMode = mode;

		try

		{

			// Capture once per UI flow so later validation does not mistake a non-Hero conversation proxy for another NPC.

			_shoutTradeTargetAgentSnapshot = Mission.Current?.Agents?.FirstOrDefault(

				agent => agent != null && agent.Index == (targetNpc?.AgentIndex ?? (-1)));

		}

		catch

		{

			_shoutTradeTargetAgentSnapshot = null;

		}

		// Shared with the scene-session give panel (ShoutBehavior.ScenePresentationTrade.cs).

		string ineligible = GetShoutTradeTargetIneligibility(mode);

		if (ineligible != null)

		{

			InformationManager.DisplayMessage(new InformationMessage(ineligible));

			ResetShoutTradeState();

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

			return;

		}

		_shoutTradeOptions = BuildShoutTradeOptions();

		_shoutPendingTradeItems.Clear();

		_shoutPendingTradeItemIndex = 0;

		if (_shoutTradeOptions == null || _shoutTradeOptions.Count == 0)

		{

			string information = GetNoShoutTradeOptionsMessage(mode);

			InformationManager.DisplayMessage(new InformationMessage(information));

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

			return;

		}

		List<InquiryElement> list = new List<InquiryElement>();

		for (int i = 0; i < _shoutTradeOptions.Count; i++)

		{

			ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[i];

			string transferDisplayName = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(shoutTradeResourceOption.Name);

			string text2 = $"{transferDisplayName} (×{shoutTradeResourceOption.AvailableAmount})";

			string text3 = ConversationUiLocalization.Text("AF_CONV_UI_Trade_AvailableAmount", "Available: {AMOUNT}", ("AMOUNT", shoutTradeResourceOption.AvailableAmount));

			if (shoutTradeResourceOption.PartyEntry != null)

			{

				if (IsShoutTroopTransferMode(_shoutTradeMode))

				{

					text3 = ConversationUiLocalization.Text("AF_CONV_UI_Trade_TroopHint", "Available: {AMOUNT} | Daily wage: {WAGE} denars/day | Hire price: {PRICE} denars/person", ("AMOUNT", shoutTradeResourceOption.AvailableAmount), ("WAGE", shoutTradeResourceOption.PartyEntry.WageDenarsPerDay), ("PRICE", shoutTradeResourceOption.PartyEntry.HirePriceDenarsPerUnit));

				}

				else if (IsShoutPrisonerTransferMode(_shoutTradeMode))

				{

					text3 = ConversationUiLocalization.Text("AF_CONV_UI_Trade_PrisonerHint", "Available: {AMOUNT} | Purchase price: {PRICE} denars/person", ("AMOUNT", shoutTradeResourceOption.AvailableAmount), ("PRICE", shoutTradeResourceOption.PartyEntry.BuyPriceDenarsPerUnit));

					string sourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(shoutTradeResourceOption.PartyEntry);

					if (!string.IsNullOrWhiteSpace(sourceLabel))

					{

						text2 += ConversationUiLocalization.Text("AF_CONV_UI_Trade_SourceSuffix", " (Source: {SOURCE})", ("SOURCE", sourceLabel));

						text3 += ConversationUiLocalization.Text("AF_CONV_UI_Trade_SourceHint", " | Source: {SOURCE}", ("SOURCE", sourceLabel));

					}

				}

			}

			else if (shoutTradeResourceOption.SettlementEntry != null)

			{

				string typeLabel = string.IsNullOrWhiteSpace(shoutTradeResourceOption.SettlementEntry.TypeLabel)

					? ((shoutTradeResourceOption.SettlementEntry.Settlement?.IsTown ?? false) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_Town", "Town") : ConversationUiLocalization.Text("AF_CONV_UI_Trade_Castle", "Castle"))

					: shoutTradeResourceOption.SettlementEntry.TypeLabel;

				text3 = ConversationUiLocalization.Text("AF_CONV_UI_Trade_AssetHint", "Daily income: {INCOME} denars | One-time settlement guide price: {PRICE} denars | Type: {TYPE}", ("INCOME", Math.Max(0, shoutTradeResourceOption.SettlementEntry.DailyIncomeDenars)), ("PRICE", Math.Max(0, shoutTradeResourceOption.SettlementEntry.GuidePriceDenars)), ("TYPE", typeLabel));

			}

			list.Add(new InquiryElement(i, text2, null, isEnabled: true, text3));

		}

		string text = targetNpc?.Name ?? ConversationUiLocalization.Text("AF_CONV_UI_Shout_TargetFallback", "nearby person");

		string suffix = _shoutTradeActionOnly ? "" : ConversationUiLocalization.Text("AF_CONV_UI_Trade_ActionOnlyNote", "\nThis action is recorded in conversation history only; it does not request an AI reply.");

		string modeLabel = mode == ShoutChatMode.Give ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveItems", "Give items") : mode == ShoutChatMode.Show ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_ShowItems", "Show items") : mode == ShoutChatMode.GiveTroops ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveTroops", "Give troops") : mode == ShoutChatMode.GivePrisoners ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_GivePrisoners", "Give prisoners") : ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveAssets", "Transfer fixed assets");

		string titleText = ConversationUiLocalization.Text("AF_CONV_UI_Trade_TargetTitle", "{MODE}{SUFFIX} - {TARGET}", ("MODE", modeLabel), ("SUFFIX", _shoutTradeActionOnly ? "" : " and talk"), ("TARGET", text));

		string selectText = IsShoutTroopTransferMode(mode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_SelectTroops", "Select troops to transfer to the other party (multiple selection):") : IsShoutPrisonerTransferMode(mode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_SelectPrisoners", "Select prisoners to give to the other party (multiple selection):") : IsShoutSettlementTransferMode(mode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_SelectAssets", "Select fixed assets to transfer to the other party (multiple selection):") : ConversationUiLocalization.Text("AF_CONV_UI_Trade_SelectItems", "Select items or denars to use (multiple selection):");

		string descriptionText = ConversationUiLocalization.Text("AF_CONV_UI_Trade_CurrentTarget", "Current target: {TARGET}", ("TARGET", text)) + "\n" + selectText + suffix;

		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 1, list.Count, ConversationUiLocalization.Text("AF_CONV_UI_Courier_Confirm", "Confirm"), ConversationUiLocalization.Text("AF_CONV_UI_Courier_Cancel", "Cancel"), selected => { if (IsUiCurrent(uiRevision, uiStepRevision)) OnShoutTradeResourcesSelected(selected); }, delegate

		{

            if (!IsUiCurrent(uiRevision, uiStepRevision)) return;

			ResetShoutTradeState();

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

		}, "", isSeachAvailable: true);

		PresentTradeInquiry(data);

	}



	internal void OnShoutTradeResourcesSelected(List<InquiryElement> selectedElements)

	{

        if (!_flowOpen || _commitConsumed) return;

		if (selectedElements == null || selectedElements.Count == 0 || _shoutTradeOptions == null || _shoutTradeOptions.Count == 0)

		{

			ResetShoutTradeState();

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

			return;

		}

		_shoutPendingTradeItems.Clear();

		foreach (InquiryElement selectedElement in selectedElements)

		{

			int num = (int)selectedElement.Identifier;

			if (num >= 0 && num < _shoutTradeOptions.Count)

			{

				ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[num];

				_shoutPendingTradeItems.Add(new ShoutPendingTradeItem

				{

					IsGold = shoutTradeResourceOption.IsGold,

					ItemId = shoutTradeResourceOption.ItemId,

					ItemName = shoutTradeResourceOption.Name,

					Item = shoutTradeResourceOption.Item,

					InventoryUnitValue = shoutTradeResourceOption.InventoryUnitValue,

					PartyEntry = shoutTradeResourceOption.PartyEntry,

					SettlementEntry = shoutTradeResourceOption.SettlementEntry,

					Amount = (shoutTradeResourceOption.SettlementEntry != null) ? 1 : 0

				});

			}

		}

		if (_shoutPendingTradeItems.Count == 0)

		{

			ResetShoutTradeState();

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

		}

		else

		{

			_shoutPendingTradeItemIndex = 0;

			if (IsShoutSettlementTransferMode(_shoutTradeMode))

			{

				if (_shoutTradeActionOnly)

				{

					CommitShoutTradeActionOnly();

				}

				else

				{

					ShowShoutTradeChatInput();

				}

			}

			else

			{

				ShowShoutTradeAmountInquiry();

			}

		}

	}



	internal void ShowShoutTradeAmountInquiry()

	{

        long uiRevision = _flowRevision;

        long uiStepRevision = ++_uiStepRevision;

        int itemIndex = _shoutPendingTradeItemIndex;

		if (_shoutPendingTradeItemIndex >= _shoutPendingTradeItems.Count)

		{

			if (_shoutTradeActionOnly)

			{

				CommitShoutTradeActionOnly();

			}

			else

			{

				ShowShoutTradeChatInput();

			}

			return;

		}

		ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[_shoutPendingTradeItemIndex];

		int availableAmount = 0;

		for (int i = 0; i < _shoutTradeOptions.Count; i++)

		{

			ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[i];

			if (shoutPendingTradeItem.PartyEntry != null)

			{

				if (shoutTradeResourceOption.PartyEntry != null && shoutTradeResourceOption.PartyEntry.PromptIndex == shoutPendingTradeItem.PartyEntry.PromptIndex)

				{

					availableAmount = shoutTradeResourceOption.AvailableAmount;

					break;

				}

			}

			else if (shoutTradeResourceOption.IsGold == shoutPendingTradeItem.IsGold && shoutTradeResourceOption.ItemId == shoutPendingTradeItem.ItemId)

			{

				availableAmount = shoutTradeResourceOption.AvailableAmount;

				break;

			}

		}

		if (availableAmount <= 0)

		{

			_shoutPendingTradeItemIndex++;

			ShowShoutTradeAmountInquiry();

			return;

		}

		string titleText = IsShoutTradeShowMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_AmountTitleShow", "Amount to show") : IsShoutPartyTransferMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_AmountTitleTransfer", "Amount to transfer") : ConversationUiLocalization.Text("AF_CONV_UI_Trade_AmountTitleGive", "Amount to give");

		string transferDisplayName = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(shoutPendingTradeItem.ItemName);

		string text = ConversationUiLocalization.Text("AF_CONV_UI_Trade_AmountDescription", "[{INDEX}/{COUNT}] {ITEM}: maximum {MAX}.\nEnter an integer from 1 to {MAX}:", ("INDEX", _shoutPendingTradeItemIndex + 1), ("COUNT", _shoutPendingTradeItems.Count), ("ITEM", transferDisplayName), ("MAX", availableAmount));

		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, ConversationUiLocalization.Text("AF_CONV_UI_Courier_Confirm", "Confirm"), ConversationUiLocalization.Text("AF_CONV_UI_Courier_Back", "Back"), delegate(string input)

		{

            if (!IsUiCurrent(uiRevision, uiStepRevision) || _shoutPendingTradeItemIndex != itemIndex) return;

			if (!int.TryParse(input, out var result) || result <= 0 || result > availableAmount)

			{

				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Trade_InvalidAmount", "Enter a valid amount.")));

				ShowShoutTradeAmountInquiry();

			}

			else

			{

				_shoutPendingTradeItems[_shoutPendingTradeItemIndex].Amount = result;

				_shoutPendingTradeItemIndex++;

				ShowShoutTradeAmountInquiry();

			}

		}, delegate

		{

            if (!IsUiCurrent(uiRevision, uiStepRevision) || _shoutPendingTradeItemIndex != itemIndex) return;

			BeginShoutTradeFlow(_shoutTradeTargetNpc, _shoutTradeMode);

		}), pauseGameActiveState: true);

	}



	internal void ShowShoutTradeChatInput()

	{

        long uiRevision = _flowRevision;

        long uiStepRevision = ++_uiStepRevision;

		string text = _shoutTradeTargetNpc?.Name ?? ConversationUiLocalization.Text("AF_CONV_UI_Shout_TargetFallback", "nearby person");

		StringBuilder stringBuilder = new StringBuilder();

		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)

		{

			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];

			if (shoutPendingTradeItem.Amount > 0)

			{

				if (shoutPendingTradeItem.PartyEntry != null)

				{

					if (IsShoutTroopTransferMode(_shoutTradeMode))

					{

						stringBuilder.AppendLine(ConversationUiLocalization.Text("AF_CONV_UI_Trade_TransferTroopLine", "  · Transfer {AMOUNT} {ITEM}", ("AMOUNT", shoutPendingTradeItem.Amount), ("ITEM", shoutPendingTradeItem.ItemName)));

					}

					else

					{

						stringBuilder.AppendLine(shoutPendingTradeItem.PartyEntry.IsHero ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_DeliverPrisonerHeroLine", "  · Deliver prisoner {ITEM}", ("ITEM", shoutPendingTradeItem.ItemName)) : ConversationUiLocalization.Text("AF_CONV_UI_Trade_DeliverPrisonerLine", "  · Deliver {AMOUNT} {ITEM} prisoners", ("AMOUNT", shoutPendingTradeItem.Amount), ("ITEM", shoutPendingTradeItem.ItemName)));

					}

				}

				else if (shoutPendingTradeItem.SettlementEntry != null)

				{

					stringBuilder.AppendLine(ConversationUiLocalization.Text("AF_CONV_UI_Trade_TransferAssetLine", "  · Transfer {TYPE} {ITEM}", ("TYPE", shoutPendingTradeItem.SettlementEntry.TypeLabel ?? ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveAssets", "fixed assets")), ("ITEM", shoutPendingTradeItem.ItemName)));

				}

				else if (shoutPendingTradeItem.IsGold)

				{

					stringBuilder.AppendLine(IsShoutTradeGiveMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveGoldLine", "  · Give {AMOUNT} denars", ("AMOUNT", shoutPendingTradeItem.Amount)) : ConversationUiLocalization.Text("AF_CONV_UI_Trade_ShowGoldLine", "  · Show {AMOUNT} denars", ("AMOUNT", shoutPendingTradeItem.Amount)));

				}

				else

				{

					string transferDisplayName = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(shoutPendingTradeItem.ItemName);

					stringBuilder.AppendLine(IsShoutTradeGiveMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveItemLine", "  · Give {AMOUNT} {ITEM}", ("AMOUNT", shoutPendingTradeItem.Amount), ("ITEM", transferDisplayName)) : ConversationUiLocalization.Text("AF_CONV_UI_Trade_ShowItemLine", "  · Show {AMOUNT} {ITEM}", ("AMOUNT", shoutPendingTradeItem.Amount), ("ITEM", transferDisplayName)));

				}

			}

		}

		string itemList = stringBuilder.ToString();

		string text2 = IsShoutTroopTransferMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_PrepareTroops", "You are preparing to transfer these troops to the other party:\n{LIST}", ("LIST", itemList)) : IsShoutPrisonerTransferMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_PreparePrisoners", "You are preparing to give these prisoners to the other party:\n{LIST}", ("LIST", itemList)) : IsShoutSettlementTransferMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_PrepareAssets", "You are preparing to transfer these fixed assets to the other party:\n{LIST}", ("LIST", itemList)) : IsShoutTradeGiveMode(_shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_PrepareGive", "You are preparing to give the other party these items:\n{LIST}", ("LIST", itemList)) : ConversationUiLocalization.Text("AF_CONV_UI_Trade_PrepareShow", "You are preparing to show the other party these items:\n{LIST}", ("LIST", itemList));

		string titleText = text;

		PauseGame();

		string inputHint = ConversationUiLocalization.Text("AF_CONV_UI_Shout_InputHint", "Tell them something:");

		if (!ShoutTextInputPopup.Show(titleText, text2, inputHint, "", value => { if (IsUiCurrent(uiRevision, uiStepRevision)) OnShoutTradeChatConfirmed(value); }, delegate

		{

            if (!IsUiCurrent(uiRevision, uiStepRevision)) return;

			ResetShoutTradeState();

			OnShoutCancelled();

		}, BuildShoutTargetEncyclopediaAction(_shoutTradeTargetNpc)))

		{

			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2 + "\n" + inputHint, isAffirmativeOptionShown: true, isNegativeOptionShown: true, ConversationUiLocalization.Text("AF_CONV_UI_Shout_Send", "Send"), ConversationUiLocalization.Text("AF_CONV_UI_Shout_Close", "Close"), value => { if (IsUiCurrent(uiRevision, uiStepRevision)) OnShoutTradeChatConfirmed(value); }, delegate

			{

                if (!IsUiCurrent(uiRevision, uiStepRevision)) return;

				ResetShoutTradeState();

				OnShoutCancelled();

			}), pauseGameActiveState: true);

		}

	}



	internal void OnShoutTradeChatConfirmed(string input)

    {

        if (!_flowOpen || _commitConsumed) return;

		if (string.IsNullOrWhiteSpace(input))

		{

			ResetShoutTradeState();

			ResumeGame();

			return;

		}

		if (!EnsureShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame: true))

		{

			ResetShoutTradeState();

			ResumeGame();

			return;

		}

        if (!TryBeginIrreversibleTradeCommit()) { ResumeGame(); return; }

        string text = "";

        try

        {

        if (IsShoutTradeGiveMode(_shoutTradeMode))

		{

			ApplyShoutGiveTransfer();

			text = BuildShoutTradeFactText(isGive: true);

		}

		else

		{

			RecordShoutShownResources();

			text = BuildShoutTradeFactText(isGive: false);

			ShowShoutPendingDisplayValueMessage(EstimateShoutPendingShowTotalValue());

		}

		if (!string.IsNullOrWhiteSpace(text))

		{

			text = AppendShoutTradeActionFactSequence(text);

		}

        }

        catch (Exception ex)

        {

            // Confirmation is irreversible even if a reporting or show-record leaf fails.

			if (string.IsNullOrWhiteSpace(text)) text = ConversationUiLocalization.Text("AF_CONV_UI_Trade_Interrupted", "The give/show flow was interrupted; the resource and target outcome could not be confirmed. This confirmation was consumed and must not be repeated.");

            Logger.Log("ShoutBehavior", "[Trade] consumed confirmation reporting failed: " + ex);

            ResumeGame();

        }

		int? forcedPrimaryAgentIndex = ((_shoutTradeTargetNpc != null) ? new int?(_shoutTradeTargetNpc.AgentIndex) : ((int?)null));

		ResetShoutTradeState();

		OnShoutConfirmedWithContext(input, text, forcedPrimaryAgentIndex);

	}



	internal void CommitShoutTradeActionOnly()

    {

        if (!_flowOpen || _commitConsumed) return;

		string fact = "";

		bool isGive = IsShoutTradeGiveMode(_shoutTradeMode);

		try

		{

			if (!EnsureShoutTradePrimaryTargetValidForCommit())

			{

				return;

			}

            if (!TryBeginIrreversibleTradeCommit()) return;

            if (isGive)

            {

				ApplyShoutGiveTransfer();

				fact = BuildShoutTradeFactText(isGive: true);

			}

			else

			{

				RecordShoutShownResources();

				fact = BuildShoutTradeFactText(isGive: false);

				ShowShoutPendingDisplayValueMessage(EstimateShoutPendingShowTotalValue());

			}

			RecordNativeConversationTradeActionFact(fact);

			if (!string.IsNullOrWhiteSpace(fact))

			{

				InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Trade_Recorded", "The give/show action was recorded. The NPC will know about it in later AI conversations."), new Color(0.4f, 1f, 0.4f)));

			}

		}

		catch (Exception ex)

		{

			Logger.Log("ShoutBehavior", "[NativeConversationTrade] action-only commit failed: " + ex);

            if (_commitConsumed && string.IsNullOrWhiteSpace(fact))

            {

                try { RecordNativeConversationTradeActionFact(AdapterSceneTexts.Get("SceneTrade_CommitInterruptedFact", "Give/show execution interrupted, source and target outcome unconfirmed; confirmation consumed, do not repeat delivery.")); }

                catch (Exception reportError) { Logger.Log("ShoutBehavior", "[Trade] unknown outcome fact recording failed: " + reportError); }

            }

            InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Trade_CommitInterrupted", "The give/show action was interrupted and its outcome could not be confirmed. Do not repeat the delivery."), new Color(1f, 0.35f, 0.25f)));

		}

		finally

		{

			ResetShoutTradeState();

			ResumeGame();

			FinishShoutTradeActionOnlyIfNeeded();

		}

	}



	internal string AppendShoutTradeActionFactSequence(string fact)

	{

		fact = (fact ?? "").Trim();

		if (string.IsNullOrWhiteSpace(fact))

		{

			return "";

		}

		int sequence = Interlocked.Increment(ref _shoutTradeActionFactSequence);

		return fact + AdapterSceneTexts.Format("SceneTrade_ActionRecordSequence", " (trade action record #{SEQUENCE})", ("SEQUENCE", sequence));

	}



	internal void FinishShoutTradeActionOnlyIfNeeded()

	{

		Action callback = _shoutTradeActionOnlyFinished;

		bool shouldCallback = _shoutTradeActionOnly || callback != null;

		_shoutTradeActionOnly = false;

        _shoutTradeNativeManager = null;

        _shoutTradeNativeMission = null;

        _shoutTradeNativeAgent = null;

		_shoutTradeTargetHeroOverride = null;

		_shoutTradeTargetCharacterOverride = null;

		_shoutTradeActionOnlyFinished = null;

		if (!shouldCallback)

		{

			return;

		}

		try

		{

			callback?.Invoke();

		}

		catch

		{

		}

	}



	internal void ResetShoutTradeState()

    {

        _flowRevision++; _flowOpen = false; TradeOwnsState = false; TradeStaged = false; TradeSummary = "";

		_shoutTradeOptions.Clear();

		_shoutPendingTradeItems.Clear();

		_shoutPendingTradeItemIndex = 0;

		_shoutTradeMode = ShoutChatMode.Normal;

		_shoutTradeTargetNpc = null;

		// Release the scene-bound identity when the UI flow ends; Agents must not outlive a completed trade flow.

		_shoutTradeTargetAgentSnapshot = null;

	}



	internal static bool TryParsePresentationTradeMode(string mode, out ShoutChatMode result)

	{

		switch (mode)

		{

			case "give": result = ShoutChatMode.Give; return true;

			case "show": result = ShoutChatMode.Show; return true;

			case "give_troops": result = ShoutChatMode.GiveTroops; return true;

			case "give_prisoners": result = ShoutChatMode.GivePrisoners; return true;

			case "give_settlements": result = ShoutChatMode.GiveSettlements; return true;

			default: result = ShoutChatMode.Normal; return false;

		}

	}



	internal bool TryOpenPresentationTradeFromWheel(string mode)

	{

		if (!EnsurePresentationSessionForWheelAction())

		{

			return false;

		}

		TradeRequestMode = mode;

		BumpPresentation();

		ResumeGame();

		return true;

	}



	internal void ReleasePresentationTrade() => ReleaseTrade(ResetShoutTradeState);



	internal static string ConsumeScenePresentationTradeRequestForExternal()

	{

		SceneTradeController owner = CurrentInstance?._j17SceneTradeController;

		string mode = owner?.TradeRequestMode;

		if (owner != null) owner.TradeRequestMode = null;

		return mode;

	}



	internal static bool HasScenePresentationStagedTradeForExternal

	{

		get

		{

			SceneTradeController owner = CurrentInstance?._j17SceneTradeController;

			return owner != null && owner.TradeStaged;

		}

	}



	internal static string GetScenePresentationStagedTradeSummaryForExternal()

	{

		return CurrentInstance?._j17SceneTradeController.TradeSummary ?? "";

	}



    internal static bool StageScenePresentationTradeForExternal(long flowRevision, IReadOnlyList<int> indices, IReadOnlyList<int> amounts, out string status)

    {

        SceneTradeController owner = CurrentInstance?._j17SceneTradeController;

        if (owner == null || !IsBannerlordMainThreadForNativeActions() || !owner.IsFlowCurrent(flowRevision))

        { status = ConversationUiLocalization.Text("AF_CONV_UI_Trade_ListExpired", "The give list is no longer valid. Open it again."); return false; }

        return StageScenePresentationTradeForExternal(indices, amounts, out status);

    }

    internal static void CancelScenePresentationTradeForExternal(long flowRevision)

    {

        SceneTradeController owner = CurrentInstance?._j17SceneTradeController;

        if (owner != null && IsBannerlordMainThreadForNativeActions() && owner.IsFlowCurrent(flowRevision)) CancelScenePresentationTradeForExternal();

    }

	internal static bool StageScenePresentationTradeForExternal(IReadOnlyList<int> indices, IReadOnlyList<int> amounts, out string status)

 {

  SceneTradeController owner=CurrentInstance?._j17SceneTradeController;

  if (owner==null) { status=ConversationUiLocalization.Text("AF_CONV_UI_Trade_ListExpired", "The give list is no longer valid. Open it again."); return false; }

  List<ScenePresentationTradeOption> options=owner._shoutTradeOptions?.Select(option => new ScenePresentationTradeOption {

   Name=CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(option.Name), ValidationName=option.Name, Available=option.AvailableAmount,

   IsSettlement=option.SettlementEntry!=null }).ToList();

  return owner.StageTrade(options, indices, amounts, IsShoutTradeShowMode(owner._shoutTradeMode) ? ConversationUiLocalization.Text("AF_CONV_UI_Trade_ShowVerb", "Show") : ConversationUiLocalization.Text("AF_CONV_UI_Trade_GiveVerb", "Give"),

   owner._shoutTradeTargetNpc?.Name, owner._shoutPendingTradeItems.Clear,

   owner.StagePresentationTransferItem, () => owner._shoutPendingTradeItemIndex=owner._shoutPendingTradeItems.Count, out status);

 }



 internal void StagePresentationTransferItem(int index,int amount)

 {

  ShoutTradeResourceOption option=_shoutTradeOptions[index];

  _shoutPendingTradeItems.Add(new ShoutPendingTradeItem { IsGold=option.IsGold, ItemId=option.ItemId, ItemName=option.Name,

   Item=option.Item, InventoryUnitValue=option.InventoryUnitValue, PartyEntry=option.PartyEntry, SettlementEntry=option.SettlementEntry, Amount=amount });

 }



	internal static void CancelScenePresentationTradeForExternal()

	{

		SceneTradeController owner = CurrentInstance?._j17SceneTradeController;

		if (owner == null)

		{

			return;

		}

		bool staged = owner.TradeStaged;

		owner.ReleasePresentationTrade();

		if (staged)

		{

			BumpPresentation();

		}

	}



	internal bool SubmitPresentationTrade(string content, out string status)

	{

		status = "";

		ShoutTargetingContext context = BuildPresentationTargetingContext();

  if (!ValidateStagedTrade(_shoutTradeTargetNpc != null && _shoutPendingTradeItems.Count > 0,

   _shoutTradeTargetNpc != null && context.CandidateAgentIndices.Contains(_shoutTradeTargetNpc.AgentIndex), ResetShoutTradeState, out status)) return false;

		int target = _shoutTradeTargetNpc.AgentIndex;

		ConsumeStagedTrade();

		_activeShoutTargetingContext = context;

		BeginShoutProcessing("scene_presentation_trade_submit");

		ActivateMultiSceneMovementSuppression(new int[1] { target });

		// Validates the target again, applies the transfer / records the showing, then sends the line.

		Presentation.BeginRound(GetApplicationTimeSafe());

		OnShoutTradeChatConfirmed(content);

		BumpPresentation();

		return true;

	}



    private ShoutTargetingContext _activeShoutTargetingContext { get => _ports.Get_activeShoutTargetingContext(); set => _ports.Set_activeShoutTargetingContext(value); }

    private void BeginShoutProcessing(string reason) => _ports.BeginShoutProcessing_L1570(reason);

    private Action BuildShoutTargetEncyclopediaAction(NpcDataPacket targetNpc) => _ports.BuildShoutTargetEncyclopediaAction_L13863(targetNpc);

    private bool EnsureShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false) => _ports.EnsureShoutTradePrimaryTargetValidForCommit_L14056(requireCurrentShoutFrame);

    internal List<ShoutTradeResourceOption> BuildShoutTradeOptions() => _ports.BuildShoutTradeOptions_L14160();

    private void RecordNativeConversationTradeActionFact(string fact) => _ports.RecordNativeConversationTradeActionFact_L14519(fact);

    private void ApplyShoutGiveTransfer() => _ports.ApplyShoutGiveTransfer_L14619();

    private string BuildShoutTradeFactText(bool isGive) => _ports.BuildShoutTradeFactText_L15241(isGive);

    private long EstimateShoutPendingShowTotalValue() => _ports.EstimateShoutPendingShowTotalValue_L15395();

    private void ShowShoutPendingDisplayValueMessage(long totalValue) => _ports.ShowShoutPendingDisplayValueMessage_L15421(totalValue);

    private void RecordShoutShownResources() => _ports.RecordShoutShownResources_L16847();

    private void OnShoutConfirmedWithContext(string shoutText, string extraFact, int? forcedPrimaryAgentIndex) => _ports.OnShoutConfirmedWithContext_L17001(shoutText, extraFact, forcedPrimaryAgentIndex);

    private void ActivateMultiSceneMovementSuppression(IEnumerable<int> participantAgentIndices) => _ports.ActivateMultiSceneMovementSuppression_L18651(participantAgentIndices);

    private void PauseGame() => _ports.PauseGame_L21301();

    private void ResumeGame() => _ports.ResumeGame_L21311();

    private void OnShoutCancelled() => _ports.OnShoutCancelled_L21322();

    private ShoutTargetingContext BuildPresentationTargetingContext() => _ports.BuildPresentationTargetingContext_L51();

    private bool EnsurePresentationSessionForWheelAction() => _ports.EnsurePresentationSessionForWheelAction_L119();

    internal ScenePresentationController Presentation { get => _ports.GetPresentation(); }

    internal string GetShoutTradeTargetIneligibility(ShoutChatMode mode) => _ports.GetShoutTradeTargetIneligibility_L33(mode);

 internal bool IsPresentationSessionLive() => Presentation.IsPresentationSessionLive();

 internal ScenePresentationController.Member FindPresentationMember(int index) => Presentation.FindPresentationMember(index);

 internal bool IsPresentationAudience(ScenePresentationController.Member member) => Presentation.IsPresentationAudience(member);

 internal void ReleaseTrade(Action resetTransfer)

 {

  if (TradeOwnsState) resetTransfer();

  TradeOwnsState = false; TradeStaged = false; TradeSummary = ""; TradeRequestMode = null;

 }

 // UI selection validation only. The game transfer port still owns inventory and the actual commit.

 internal bool StageTrade(IReadOnlyList<ScenePresentationTradeOption> options, IReadOnlyList<int> indices,

  IReadOnlyList<int> amounts, string verb, string targetName, Action clearItems, Action<int,int> stageItem, Action finishItems, out string status)

 {

  status = "";

  if (!IsFlowCurrent(_flowRevision) || !TradeOwnsState || options == null || options.Count == 0) { status = ConversationUiLocalization.Text("AF_CONV_UI_Trade_ListExpired", "The give list is no longer valid. Open it again."); return false; }

  if (indices == null || amounts == null || indices.Count == 0 || indices.Count != amounts.Count) { status = ConversationUiLocalization.Text("AF_CONV_UI_Trade_SelectResource", "Select at least one resource to give."); return false; }

  clearItems();

  HashSet<int> seen = new HashSet<int>(); List<string> labels = new List<string>();

  for (int i=0; i<indices.Count; i++)

  {

   int index=indices[i]; if (index < 0 || index >= options.Count || !seen.Add(index)) continue;

   ScenePresentationTradeOption option=options[index]; int amount=option.IsSettlement ? 1 : amounts[i];

   if (amount < 1 || amount > option.Available) { clearItems(); status=ConversationUiLocalization.Text("AF_CONV_UI_Trade_AmountOutOfRange", "The amount exceeds the available range: {RESOURCE}", ("RESOURCE", option.ValidationName ?? option.Name ?? "resource")); return false; }

   stageItem(index,amount); labels.Add(option.Name+" ×"+amount);

  }

  if (labels.Count==0) { status=ConversationUiLocalization.Text("AF_CONV_UI_Trade_SelectResource", "Select at least one resource to give."); return false; }

  finishItems(); TradeStaged=true; TradeSummary=verb+" "+(targetName ?? ConversationUiLocalization.Text("AF_CONV_UI_Trade_OtherParty", "the other party"))+"："+string.Join("、",labels);

  BumpPresentation(); return true;

 }

 internal bool ValidateStagedTrade(bool hasTargetAndItems, bool targetInAudience, Action resetTransfer, out string status)

 {

  status="";

  if (TradeOwnsState && hasTargetAndItems && targetInAudience) return true;

  ReleaseTrade(resetTransfer); BumpPresentation(); status=ConversationUiLocalization.Text("AF_CONV_UI_Trade_NoDelivery", "The target left or the give action expired; nothing was delivered and no line was sent."); return false;

 }

 internal void ConsumeStagedTrade() { TradeOwnsState=false; TradeStaged=false; TradeSummary=""; }



    internal void StartFlowIdentity()

    {

        _flowRevision++; _flowOpen = true; _commitConsumed = false;

        TradeOwnsState = false; TradeStaged = false; TradeSummary = ""; TradeRequestMode = null;

        _flowMission = Mission.Current; _flowGeneration = SaveRuntimeGuard.CaptureGeneration();

    }

    internal bool IsFlowCurrent(long revision) => _flowOpen && !_commitConsumed && revision == _flowRevision

        && ReferenceEquals(_flowMission, Mission.Current) && SaveRuntimeGuard.IsCurrentGeneration(_flowGeneration);

    private bool IsUiCurrent(long revision, long stepRevision) => IsFlowCurrent(revision) && stepRevision == _uiStepRevision;

    private bool TryBeginIrreversibleTradeCommit()

    {

        if (!IsFlowCurrent(_flowRevision) || _shoutPendingTradeItems.Count == 0 || _shoutPendingTradeItems.Any(item => item == null || item.Amount <= 0)) return false;

        string ineligible = GetShoutTradeTargetIneligibility(_shoutTradeMode);

        if (ineligible != null) { InformationManager.DisplayMessage(new InformationMessage(ineligible)); return false; }

        List<ShoutTradeResourceOption> fresh = BuildShoutTradeOptions();

        foreach (ShoutPendingTradeItem item in _shoutPendingTradeItems)

        {

            if (item.Amount <= 0) continue;

            ShoutTradeResourceOption option = fresh?.FirstOrDefault(value => MatchesPendingSource(value, item));

            if (option == null || item.Amount > option.AvailableAmount)

            { InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Trade_ResourcesChanged", "The resource or amount changed; nothing was delivered."))); return false; }

        }

        _commitConsumed = true; TradeStaged = false; TradeSummary = "";

        return true;

    }

    private static bool MatchesPendingSource(ShoutTradeResourceOption option, ShoutPendingTradeItem item)

    {

        if (option.IsGold != item.IsGold) return false;

        if (item.IsGold) return true;

        if (item.PartyEntry != null) return option.PartyEntry != null && option.PartyEntry.Section == item.PartyEntry.Section

            && ReferenceEquals(option.PartyEntry.Character, item.PartyEntry.Character)

            && ReferenceEquals(option.PartyEntry.OwnerParty, item.PartyEntry.OwnerParty)

            && ReferenceEquals(option.PartyEntry.SourceSettlement, item.PartyEntry.SourceSettlement)

            && ReferenceEquals(option.PartyEntry.VolunteerOwner, item.PartyEntry.VolunteerOwner);

        if (item.SettlementEntry != null) return option.SettlementEntry != null

            && option.SettlementEntry.AssetKind == item.SettlementEntry.AssetKind

            && ReferenceEquals(option.SettlementEntry.Settlement, item.SettlementEntry.Settlement)

            && ReferenceEquals(option.SettlementEntry.Workshop, item.SettlementEntry.Workshop)

            && ReferenceEquals(option.SettlementEntry.CaravanParty, item.SettlementEntry.CaravanParty);

        return string.Equals(option.ItemId, item.ItemId, StringComparison.Ordinal) && ReferenceEquals(option.Item, item.Item);

    }



}



internal sealed class SceneTradeControllerPorts

{

    internal Func<ShoutTargetingContext> Get_activeShoutTargetingContext;

    internal Action<ShoutTargetingContext> Set_activeShoutTargetingContext;

    internal delegate void BeginShoutProcessing_L1570Callback(string reason);

    internal BeginShoutProcessing_L1570Callback BeginShoutProcessing_L1570;

    internal delegate Action BuildShoutTargetEncyclopediaAction_L13863Callback(NpcDataPacket targetNpc);

    internal BuildShoutTargetEncyclopediaAction_L13863Callback BuildShoutTargetEncyclopediaAction_L13863;

    internal delegate bool EnsureShoutTradePrimaryTargetValidForCommit_L14056Callback(bool requireCurrentShoutFrame);

    internal EnsureShoutTradePrimaryTargetValidForCommit_L14056Callback EnsureShoutTradePrimaryTargetValidForCommit_L14056;

    internal delegate List<ShoutTradeResourceOption> BuildShoutTradeOptions_L14160Callback();

    internal BuildShoutTradeOptions_L14160Callback BuildShoutTradeOptions_L14160;

    internal delegate void RecordNativeConversationTradeActionFact_L14519Callback(string fact);

    internal RecordNativeConversationTradeActionFact_L14519Callback RecordNativeConversationTradeActionFact_L14519;

    internal delegate void ApplyShoutGiveTransfer_L14619Callback();

    internal ApplyShoutGiveTransfer_L14619Callback ApplyShoutGiveTransfer_L14619;

    internal delegate string BuildShoutTradeFactText_L15241Callback(bool isGive);

    internal BuildShoutTradeFactText_L15241Callback BuildShoutTradeFactText_L15241;

    internal delegate long EstimateShoutPendingShowTotalValue_L15395Callback();

    internal EstimateShoutPendingShowTotalValue_L15395Callback EstimateShoutPendingShowTotalValue_L15395;

    internal delegate void ShowShoutPendingDisplayValueMessage_L15421Callback(long totalValue);

    internal ShowShoutPendingDisplayValueMessage_L15421Callback ShowShoutPendingDisplayValueMessage_L15421;

    internal delegate void RecordShoutShownResources_L16847Callback();

    internal RecordShoutShownResources_L16847Callback RecordShoutShownResources_L16847;

    internal delegate void OnShoutConfirmedWithContext_L17001Callback(string shoutText, string extraFact, int? forcedPrimaryAgentIndex);

    internal OnShoutConfirmedWithContext_L17001Callback OnShoutConfirmedWithContext_L17001;

    internal delegate void ActivateMultiSceneMovementSuppression_L18651Callback(IEnumerable<int> participantAgentIndices);

    internal ActivateMultiSceneMovementSuppression_L18651Callback ActivateMultiSceneMovementSuppression_L18651;

    internal delegate void PauseGame_L21301Callback();

    internal PauseGame_L21301Callback PauseGame_L21301;

    internal delegate void ResumeGame_L21311Callback();

    internal ResumeGame_L21311Callback ResumeGame_L21311;

    internal delegate void OnShoutCancelled_L21322Callback();

    internal OnShoutCancelled_L21322Callback OnShoutCancelled_L21322;

    internal delegate ShoutTargetingContext BuildPresentationTargetingContext_L51Callback();

    internal BuildPresentationTargetingContext_L51Callback BuildPresentationTargetingContext_L51;

    internal delegate bool EnsurePresentationSessionForWheelAction_L119Callback();

    internal EnsurePresentationSessionForWheelAction_L119Callback EnsurePresentationSessionForWheelAction_L119;

    internal Func<ScenePresentationController> GetPresentation;

    internal delegate string GetShoutTradeTargetIneligibility_L33Callback(ShoutChatMode mode);

    internal GetShoutTradeTargetIneligibility_L33Callback GetShoutTradeTargetIneligibility_L33;

}

