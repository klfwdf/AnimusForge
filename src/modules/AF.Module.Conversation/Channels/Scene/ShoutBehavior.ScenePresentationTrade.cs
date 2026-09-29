using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ScenePresentationTradeOption
{
	public int Index { get; internal set; }
	public string Name { get; internal set; } = "";
	public string Category { get; internal set; } = "";
	public int Available { get; internal set; }
	public int UnitValue { get; internal set; }
	// Opaque host option; presentation code only passes it back for thumbnails.
	public object HostOption { get; internal set; }
}

// Give/show inside the persistent scene session. Replaces the old popup chain (resource list, one
// amount popup per item, one-shot text popup) with one panel, but reuses the host's own option list,
// eligibility rules and commit (OnShoutTradeChatConfirmed): the gift is staged and delivered together
// with the player's next line, exactly like the one-shot "给予其物品并交流" flow.
public partial class ShoutBehavior
{
	private static volatile string _presentationTradeRequestMode;
	private bool _presentationTradeOwnsState;
	private bool _presentationTradeStaged;
	private string _presentationTradeSummary = "";

	// Used by BeginShoutTradeFlow and the scene give panel, so both apply the same target rules.
	private string GetShoutTradeTargetIneligibility(ShoutChatMode mode)
	{
		ResolveShoutTradeRuntimeTarget(out var hero, out var character, out var _);
		if (IsNativeConversationSelfTarget(hero, character))
		{
			return "不能把资源给予自己。";
		}
		if (IsShoutPartyTransferMode(mode))
		{
			if (!MyBehavior.IsPartyTransferLordEligibleForExternal(hero, character))
			{
				return "只有领主才能谈部队与俘虏转移。";
			}
			PartyBase counterparty = MyBehavior.ResolvePartyTransferCounterpartyForExternal(hero, character, GetShoutTradeTargetAgentIndex());
			if (counterparty == null)
			{
				Logger.Log("ShoutBehavior", "[ShoutTrade] party transfer blocked: no counterparty target=" + (hero?.StringId ?? character?.StringId ?? _shoutTradeTargetNpc?.Name ?? "null") + " mode=" + mode);
				return "当前目标没有可接收部队或俘虏的队伍。";
			}
		}
		if (IsShoutSettlementTransferMode(mode) && !MyBehavior.IsSettlementTransferLeaderEligibleForExternal(hero, character))
		{
			return "当前目标没有可转移的固定资产。";
		}
		return null;
	}

	private static string GetNoShoutTradeOptionsMessage(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GiveTroops ? "你当前没有可转移给对方的部队。"
			: mode == ShoutChatMode.GivePrisoners ? "你当前没有可转移给对方的俘虏。"
			: IsShoutSettlementTransferMode(mode) ? "你当前没有可转移给对方的固定资产。"
			: "你没有可用的物品或第纳尔。";
	}

	private static bool TryParsePresentationTradeMode(string mode, out ShoutChatMode result)
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

	// Wheel give/show: keep (or open) the session and ask the panel to show its give view.
	private bool TryOpenPresentationTradeFromWheel(string mode)
	{
		if (!EnsurePresentationSessionForWheelAction())
		{
			return false;
		}
		_presentationTradeRequestMode = mode;
		BumpPresentation();
		ResumeGame();
		return true;
	}

	private void ReleasePresentationTrade()
	{
		if (_presentationTradeOwnsState)
		{
			ResetShoutTradeState();
		}
		_presentationTradeOwnsState = false;
		_presentationTradeStaged = false;
		_presentationTradeSummary = "";
	}

	public static string ConsumeScenePresentationTradeRequestForExternal()
	{
		string mode = _presentationTradeRequestMode;
		_presentationTradeRequestMode = null;
		return mode;
	}

	public static bool HasScenePresentationStagedTradeForExternal
	{
		get
		{
			ShoutBehavior owner = CurrentInstance;
			return owner != null && owner._presentationTradeStaged;
		}
	}

	public static string GetScenePresentationStagedTradeSummaryForExternal()
	{
		return CurrentInstance?._presentationTradeSummary ?? "";
	}

	// Loads the host option list for the session addressee. Never shows a popup.
	public static List<ScenePresentationTradeOption> LoadScenePresentationTradeOptionsForExternal(string mode, out string status)
	{
		status = "";
		List<ScenePresentationTradeOption> result = new List<ScenePresentationTradeOption>();
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner.IsPresentationSessionLive() || !IsBannerlordMainThreadForNativeActions())
		{
			status = "场景会话已结束。";
			return result;
		}
		if (!TryParsePresentationTradeMode(mode, out ShoutChatMode chatMode))
		{
			return result;
		}
		// A native-conversation give flow owns these fields while it runs; never take them over.
		// (A leftover scene target from an abandoned popup flow is not a live owner and is replaced.)
		if (owner._shoutTradeActionOnly)
		{
			status = "另一个给予流程正在进行，请先完成它。";
			return result;
		}
		ScenePresentationMember addressee = owner.FindPresentationMember(owner._presentationAddresseeIndex);
		NpcDataPacket target = owner.IsPresentationAudience(addressee) ? ShoutUtils.ExtractNpcData(addressee.Agent) : null;
		if (target == null)
		{
			status = "当前没有可以给予的对话对象。";
			return result;
		}
		// Reopening the give panel replaces a staged gift; refresh the session banner when that happens.
		bool hadStaged = owner._presentationTradeStaged;
		owner.ReleasePresentationTrade();
		if (hadStaged)
		{
			BumpPresentation();
		}
		owner._presentationTradeOwnsState = true;
		owner._shoutTradeTargetHeroOverride = null;
		owner._shoutTradeTargetCharacterOverride = null;
		owner._shoutTradeTargetNpc = target;
		owner._shoutTradeMode = chatMode;
		owner._shoutTradeTargetAgentSnapshot = addressee.Agent;
		string ineligible = owner.GetShoutTradeTargetIneligibility(chatMode);
		if (ineligible != null)
		{
			status = ineligible;
			owner.ReleasePresentationTrade();
			return result;
		}
		owner._shoutTradeOptions = owner.BuildShoutTradeOptions() ?? new List<ShoutTradeResourceOption>();
		owner._shoutPendingTradeItems.Clear();
		owner._shoutPendingTradeItemIndex = 0;
		if (owner._shoutTradeOptions.Count == 0)
		{
			status = GetNoShoutTradeOptionsMessage(chatMode);
			return result;
		}
		for (int i = 0; i < owner._shoutTradeOptions.Count; i++)
		{
			ShoutTradeResourceOption option = owner._shoutTradeOptions[i];
			MyBehavior.PartyTransferPromptEntry party = option.PartyEntry;
			MyBehavior.SettlementTransferPromptEntry asset = option.SettlementEntry;
			result.Add(new ScenePresentationTradeOption
			{
				Index = i,
				Name = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(option.Name),
				Category = option.IsGold ? "货币" : asset != null ? (asset.TypeLabel ?? "固定资产") : party != null ? (chatMode == ShoutChatMode.GiveTroops ? "部队" : "俘虏") : "物品",
				Available = Math.Max(0, option.AvailableAmount),
				UnitValue = option.IsGold ? 1 : asset != null ? Math.Max(0, asset.GuidePriceDenars)
					: party != null ? Math.Max(0, chatMode == ShoutChatMode.GiveTroops ? party.HirePriceDenarsPerUnit : party.BuyPriceDenarsPerUnit)
					: Math.Max(1, option.InventoryUnitValue),
				HostOption = option
			});
		}
		return result;
	}

	// Stages the chosen items; nothing moves until the next line is sent.
	public static bool StageScenePresentationTradeForExternal(IReadOnlyList<int> indices, IReadOnlyList<int> amounts, out string status)
	{
		status = "";
		ShoutBehavior owner = CurrentInstance;
		if (owner == null || !owner._presentationTradeOwnsState || owner._shoutTradeOptions == null || owner._shoutTradeOptions.Count == 0)
		{
			status = "给予列表已失效，请重新打开。";
			return false;
		}
		if (indices == null || amounts == null || indices.Count == 0 || indices.Count != amounts.Count)
		{
			status = "请先选择要给予的资源。";
			return false;
		}
		owner._shoutPendingTradeItems.Clear();
		HashSet<int> seen = new HashSet<int>();
		for (int i = 0; i < indices.Count; i++)
		{
			int index = indices[i];
			if (index < 0 || index >= owner._shoutTradeOptions.Count || !seen.Add(index))
			{
				continue;
			}
			ShoutTradeResourceOption option = owner._shoutTradeOptions[index];
			int amount = option.SettlementEntry != null ? 1 : amounts[i];
			if (amount < 1 || amount > option.AvailableAmount)
			{
				owner._shoutPendingTradeItems.Clear();
				status = "数量超出可用范围：" + (option.Name ?? "资源");
				return false;
			}
			owner._shoutPendingTradeItems.Add(new ShoutPendingTradeItem
			{
				IsGold = option.IsGold,
				ItemId = option.ItemId,
				ItemName = option.Name,
				Item = option.Item,
				InventoryUnitValue = option.InventoryUnitValue,
				PartyEntry = option.PartyEntry,
				SettlementEntry = option.SettlementEntry,
				Amount = amount
			});
		}
		if (owner._shoutPendingTradeItems.Count == 0)
		{
			status = "请先选择要给予的资源。";
			return false;
		}
		owner._shoutPendingTradeItemIndex = owner._shoutPendingTradeItems.Count;
		owner._presentationTradeStaged = true;
		string verb = IsShoutTradeShowMode(owner._shoutTradeMode) ? "展示" : "给予";
		owner._presentationTradeSummary = verb + " " + (owner._shoutTradeTargetNpc?.Name ?? "对方") + "："
			+ string.Join("、", owner._shoutPendingTradeItems.Select(x => CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(x.ItemName) + " ×" + x.Amount));
		BumpPresentation();
		return true;
	}

	public static void CancelScenePresentationTradeForExternal()
	{
		ShoutBehavior owner = CurrentInstance;
		if (owner == null)
		{
			return;
		}
		bool staged = owner._presentationTradeStaged;
		owner.ReleasePresentationTrade();
		if (staged)
		{
			BumpPresentation();
		}
	}

	// Called from the session submit: same commit as the one-shot flow's chat step.
	private bool SubmitPresentationTrade(string content, out string status)
	{
		status = "";
		bool valid = _presentationTradeOwnsState && _shoutTradeTargetNpc != null && _shoutPendingTradeItems.Count > 0;
		ShoutTargetingContext context = BuildPresentationTargetingContext();
		if (valid && !context.CandidateAgentIndices.Contains(_shoutTradeTargetNpc.AgentIndex))
		{
			valid = false;
		}
		if (!valid)
		{
			ReleasePresentationTrade();
			BumpPresentation();
			status = "给予对象已离开或给予已失效，本次没有交付，话也没有发出。";
			return false;
		}
		int target = _shoutTradeTargetNpc.AgentIndex;
		_presentationTradeOwnsState = false;
		_presentationTradeStaged = false;
		_presentationTradeSummary = "";
		_activeShoutTargetingContext = context;
		BeginShoutProcessing("scene_presentation_trade_submit");
		ActivateMultiSceneMovementSuppression(new int[1] { target });
		// Validates the target again, applies the transfer / records the showing, then sends the line.
		ClearPresentationRound();
		_presentationRoundPendingSince = GetApplicationTimeSafe();
		OnShoutTradeChatConfirmed(content);
		BumpPresentation();
		return true;
	}
}
