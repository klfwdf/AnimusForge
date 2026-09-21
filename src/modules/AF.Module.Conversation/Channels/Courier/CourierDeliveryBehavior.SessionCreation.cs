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
			InformationManager.DisplayMessage(new InformationMessage("信使与邮递打开失败：" + ex.Message, Colors.Red));
		}
	}

	public static void OpenCourierReplyFlowForExternal(string targetHeroId, string targetName)
	{
		try
		{
			if (Instance == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("信使系统尚未初始化，暂时不能回信。", Colors.Yellow));
				return;
			}
			Hero target = ResolveHeroByIdForCourier(targetHeroId);
			if (target == null)
			{
				string name = string.IsNullOrWhiteSpace(targetName) ? "该角色" : targetName.Trim();
				InformationManager.DisplayMessage(new InformationMessage("找不到" + name + "，暂时不能回信。", Colors.Yellow));
				return;
			}
			Instance.OpenCourierFlow(target, allowLetterReply: true);
		}
		catch (Exception ex)
		{
			Log("open courier reply flow failed target=" + (targetHeroId ?? "") + " error=" + ex);
			InformationManager.DisplayMessage(new InformationMessage("回信打开失败：" + ex.Message, Colors.Red));
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
			InformationManager.DisplayMessage(new InformationMessage("找不到可通信的收件人。", Colors.Yellow));
			return;
		}
		if (IsHeroInPlayerPartyForCourier(recipient))
		{
			InformationManager.DisplayMessage(new InformationMessage("该角色正在你的队伍中，不能通过信使写信。", Colors.Yellow));
			return;
		}
		if (recipient.IsDead)
		{
			InformationManager.DisplayMessage(new InformationMessage("该角色已经死亡，不能通过信使写信。", Colors.Yellow));
			return;
		}
		if (!allowLetterReply && !ShouldShowCourierButtonForExternal(recipient, informationHidden: false))
		{
			InformationManager.DisplayMessage(new InformationMessage("你尚未掌握此人的信息，不能寄信。", Colors.Yellow));
			return;
		}
		if (HasActiveCourierForHero(recipient))
		{
			InformationManager.DisplayMessage(new InformationMessage("已经有一支信使队正在处理发往此人的信件。", Colors.Yellow));
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty?.MemberRoster == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前找不到玩家部队。", Colors.Red));
			return;
		}
		TroopRoster available = BuildSelectableCrewRoster(mainParty.MemberRoster);
		if (available.TotalManCount <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你当前没有可派出的信使成员。", Colors.Yellow));
			return;
		}
		_pendingFlow = new PendingCourierFlow
		{
			Recipient = recipient,
			CrewRoster = null,
			Mode = CourierPayloadMode.Normal
		};
		Log("open crew selection recipient=" + SafeHeroId(recipient) + " available=" + available.TotalManCount);
		PartyScreenHelper.OpenScreenWithDummyRoster(
			available,
			TroopRoster.CreateDummyTroopRoster(),
			TroopRoster.CreateDummyTroopRoster(),
			TroopRoster.CreateDummyTroopRoster(),
			new TextObject("可选信使成员"),
			new TextObject("信使部队"),
			Math.Max(available.TotalManCount, 0),
			Math.Max(1, available.TotalManCount),
			new PartyPresentationDoneButtonConditionDelegate(CrewSelectionDoneCondition),
			new PartyScreenClosedDelegate(OnCrewSelectionClosed),
			new IsTroopTransferableDelegate(CourierCrewTransferableDelegate));
	}

	private static Tuple<bool, TextObject> CrewSelectionDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		if (rightMemberRoster == null || rightMemberRoster.TotalManCount <= 0)
		{
			return new Tuple<bool, TextObject>(false, new TextObject("信使部队必须至少 1 人。"));
		}
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static bool CourierCrewTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character != null && !character.IsPlayerCharacter && type == PartyScreenLogic.TroopType.Member;
	}

	private void OnCrewSelectionClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		try
		{
			if (fromCancel || _pendingFlow == null)
			{
				ResetPendingFlow("crew_cancel");
				return;
			}
			TroopRoster selected = BuildSelectionRosterFromUi(rightMemberRoster);
			if (selected.TotalManCount <= 0)
			{
				ResetPendingFlow("crew_empty");
				InformationManager.DisplayMessage(new InformationMessage("信使部队必须至少 1 人。", Colors.Yellow));
				return;
			}
			_pendingFlow.CrewRoster = selected;
			_pendingFlow.CrewEntries = BuildCargoEntriesFromRoster(selected, "crew");
			Log("crew selected recipient=" + SafeHeroId(_pendingFlow.Recipient) + " roster=" + RosterSummary(selected));
			ShowCourierModeInquiry();
		}
		catch (Exception ex)
		{
			Log("crew close failed: " + ex);
			ResetPendingFlow("crew_exception");
			InformationManager.DisplayMessage(new InformationMessage("信使部队选择失败：" + ex.Message, Colors.Red));
		}
	}

	private void ShowCourierModeInquiry()
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("mode_no_flow");
			return;
		}
		string targetName = flow.Recipient.Name?.ToString() ?? "收件人";
		List<InquiryElement> items = new List<InquiryElement>
		{
			new InquiryElement("normal", "单纯写信", null, true, ""),
			new InquiryElement("give", "发送物品并写信", null, true, ""),
			new InquiryElement("show", "展示物品并写信", null, true, ""),
			new InquiryElement("give_troops", "转移部队并写信", null, true, ""),
			new InquiryElement("give_prisoners", "转移俘虏并写信", null, true, ""),
			new InquiryElement("give_settlements", "转移固定资产并写信", null, true, "")
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"信使与邮递 - " + targetName,
			"当前收件人：" + targetName + "\n请选择寄送方式：",
			items,
			true,
			1,
			1,
			"确定",
			"取消",
			selected =>
			{
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
			_ => ResetPendingFlow("mode_cancel"),
			"",
			true), true);
	}

	private void BeginPayloadSelection(CourierPayloadMode mode)
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("payload_no_flow");
			return;
		}
		flow.Mode = mode;
		if (mode == CourierPayloadMode.GiveTroops && !MyBehavior.IsPartyTransferLordEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage("只有领主才能谈部队转移。", Colors.Yellow));
			ResetPendingFlow("payload_troop_ineligible");
			return;
		}
		if (mode == CourierPayloadMode.GivePrisoners && !MyBehavior.IsPartyTransferLordEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage("只有领主才能谈俘虏转移。", Colors.Yellow));
			ResetPendingFlow("payload_prisoner_ineligible");
			return;
		}
		if (mode == CourierPayloadMode.GiveSettlements && !MyBehavior.IsSettlementTransferLeaderEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage("当前收件人没有可接收或可谈的固定资产。", Colors.Yellow));
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
			string hint = "可用数量: " + Math.Max(0, option.AvailableAmount);
			if (option.PartyEntry != null)
			{
				hint = option.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops
					? $"可用数量: {option.AvailableAmount} | 日薪: {option.PartyEntry.WageDenarsPerDay}第纳尔/天 | 雇佣价: {option.PartyEntry.HirePriceDenarsPerUnit}第纳尔/人"
					: $"可用数量: {option.AvailableAmount} | 购买价: {option.PartyEntry.BuyPriceDenarsPerUnit}第纳尔/人";
				string sourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(option.PartyEntry);
				if (!string.IsNullOrWhiteSpace(sourceLabel))
				{
					hint += " | 来源: " + sourceLabel;
				}
			}
			else if (option.SettlementEntry != null)
			{
				hint = $"类型: {(string.IsNullOrWhiteSpace(option.SettlementEntry.TypeLabel) ? "固定资产" : option.SettlementEntry.TypeLabel)} | 每日收益: {Math.Max(0, option.SettlementEntry.DailyIncomeDenars)} 第纳尔 | 一次结清指导价: {Math.Max(0, option.SettlementEntry.GuidePriceDenars)} 第纳尔";
			}
			string displayName = option.Name;
			displayName = GetCourierLetterTransferDisplayTitleForExternal(displayName);
			string prisonerSourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(option.PartyEntry);
			if (!string.IsNullOrWhiteSpace(prisonerSourceLabel))
			{
				displayName += "（来源：" + prisonerSourceLabel + "）";
			}
			list.Add(new InquiryElement(i, displayName + " (×" + Math.Max(1, option.AvailableAmount) + ")", null, true, hint));
		}
		string targetName = flow.Recipient.Name?.ToString() ?? "收件人";
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			BuildPayloadTitle(mode, targetName),
			BuildPayloadDescription(mode, targetName),
			list,
			true,
			1,
			list.Count,
			"确定",
			"取消",
			OnPayloadResourcesSelected,
			_ => ResetPendingFlow("payload_cancel"),
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
		string title = flow.Mode == CourierPayloadMode.Show ? "展示数量" : (flow.Mode == CourierPayloadMode.Give ? "发送数量" : "转移数量");
		string entryDisplayName = GetCourierLetterTransferDisplayTitleForExternal(entry.Name);
		string text = $"[{flow.PendingAmountIndex + 1}/{flow.SelectedEntries.Count}] {entryDisplayName} 最多可填 {max}。\n请输入 1 到 {max} 的整数：";
		InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, "确定", "返回", input =>
		{
			if (!int.TryParse(input, out var amount) || amount <= 0 || amount > max)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的数量。", Colors.Yellow));
				ShowPayloadAmountInquiry();
				return;
			}
			entry.Amount = amount;
			flow.PendingAmountIndex++;
			ShowPayloadAmountInquiry();
		}, () => BeginPayloadSelection(flow.Mode)), true);
	}

	private void ShowLetterInput()
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("letter_no_flow");
			return;
		}
		string targetName = flow.Recipient.Name?.ToString() ?? "收件人";
		_letterInputOpen = true;
		bool opened = CourierLetterInputPopup.Show("写给 " + targetName + " 的信", "", "", "", input =>
		{
			_letterInputOpen = false;
			OnLetterConfirmed(input);
		}, () =>
		{
			_letterInputOpen = false;
			ResetPendingFlow("letter_cancel");
		});
		if (!opened)
		{
			InformationManager.ShowTextInquiry(new TextInquiryData("写给 " + targetName + " 的信", "", true, true, "发送", "取消", input =>
			{
				_letterInputOpen = false;
				OnLetterConfirmed(input);
			}, () =>
			{
				_letterInputOpen = false;
				ResetPendingFlow("letter_cancel_fallback");
			}), true);
		}
	}

	private void OnLetterConfirmed(string input)
	{
		PendingCourierFlow flow = _pendingFlow;
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
			CourierSession session = CreateCourierSession(flow, input.Trim());
			if (session == null)
			{
				ResetPendingFlow("confirm_create_null");
				return;
			}
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session);
			Log("session created id=" + session.Id + " recipient=" + session.RecipientHeroId + " party=" + session.CourierPartyId + " mode=" + session.PayloadMode + " entries=" + session.Entries.Count);
			StartCourierReplyGeneration(session, "created_preflight");
			InformationManager.DisplayMessage(new InformationMessage("信使队已出发，正在前往 " + session.RecipientName + "。", Colors.Green));
			ResetPendingFlow("confirm_done");
			ProcessSession(session);
		}
		catch (Exception ex)
		{
			Log("confirm failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("信使出发失败：" + ex.Message, Colors.Red));
			ResetPendingFlow("confirm_exception");
		}
	}

	private CourierSession CreateCourierSession(PendingCourierFlow flow, string letter)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || flow?.Recipient == null || flow.CrewRoster == null || flow.CrewRoster.TotalManCount <= 0)
		{
			throw new InvalidOperationException("信使队数据不完整。");
		}
		string id = NewSessionId();
		TroopRoster emptyMembers = TroopRoster.CreateDummyTroopRoster();
		TroopRoster emptyPrisoners = TroopRoster.CreateDummyTroopRoster();
		float speed = Math.Max(4f, mainParty.Speed) * 4f;
		TextObject name = new TextObject("AnimusForge 信使队");
		MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(mainParty.Position, 0.05f, mainParty.CurrentSettlement, name, Clan.PlayerClan, emptyMembers, emptyPrisoners, Hero.MainHero, "", "", speed, true);
		if (courier == null)
		{
			throw new InvalidOperationException("创建信使队失败。");
		}
		courier.IsVisible = true;
		ApplyCourierMapBannerVisual(courier, "create");
		courier.Party.SetCustomName(new TextObject("信使队 - " + (flow.Recipient.Name?.ToString() ?? "收件人")));
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
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, new TextObject("AnimusForge NPC Courier"), ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_npc_initiated_inbound");
			courier.Party.SetCustomName(new TextObject("信使队 - " + (sender.Name?.ToString() ?? "NPC")));
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
			TextObject name = new TextObject("AnimusForge NPC Courier");
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, name, ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_inbound");
			courier.Party.SetCustomName(new TextObject("信使队 - " + (sender.Name?.ToString() ?? "NPC")));
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
				InboundIntentText = "围绕当前真实外交事项写信，不得虚构已达成的结果。",
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
			TextObject name = new TextObject("AnimusForge NPC Courier");
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, name, ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_inbound_generic");
			courier.Party.SetCustomName(new TextObject("信使队 - " + (sender.Name?.ToString() ?? "NPC")));
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
