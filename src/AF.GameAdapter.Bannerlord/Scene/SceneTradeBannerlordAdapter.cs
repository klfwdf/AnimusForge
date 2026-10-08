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

internal sealed class SceneTradeBannerlordAdapter
{
    private readonly SceneTradeBannerlordAdapterPorts _ports;
    internal SceneTradeBannerlordAdapter(SceneTradeBannerlordAdapterPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal void ResolveShoutTradeRuntimeTarget(out Hero hero, out CharacterObject characterObject, out Agent agent)
	{
		hero = null;
		characterObject = null;
		agent = null;
		try
		{
			agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == (_shoutTradeTargetNpc?.AgentIndex ?? (-1)));
			characterObject = agent?.Character as CharacterObject;
		}
		catch
		{
			agent = null;
			characterObject = null;
		}
		if (_shoutTradeTargetHeroOverride != null)
		{
			hero = _shoutTradeTargetHeroOverride;
		}
		else if (_shoutTradeTargetNpc != null && _shoutTradeTargetNpc.IsHero)
		{
			hero = ResolveHeroFromAgentIndex(_shoutTradeTargetNpc.AgentIndex) ?? characterObject?.HeroObject;
		}
		// Prefer the current scene Agent character; native non-Hero conversations can expose a proxy CharacterObject instead.
		if (_shoutTradeTargetCharacterOverride != null && characterObject == null)
		{
			characterObject = _shoutTradeTargetCharacterOverride;
		}
		if (hero == null)
		{
			hero = characterObject?.HeroObject;
		}
		if (characterObject == null)
		{
			characterObject = hero?.CharacterObject;
		}
	}

	private bool IsNativeTradeTargetValidForCommit()
	{
		try
		{
			ConversationManager manager = Campaign.Current?.ConversationManager;
			if (_shoutTradeNativeManager == null || !ReferenceEquals(manager, _shoutTradeNativeManager)
				|| !manager.IsConversationInProgress || !ReferenceEquals(Mission.Current, _shoutTradeNativeMission))
				return false;
			// Use the actual conversation participant, never the encountered army leader as fallback.
			CharacterObject character = manager.OneToOneConversationCharacter;
			Hero hero = character?.HeroObject;
			if (_shoutTradeTargetHeroOverride != null)
			{
				if (!ReferenceEquals(hero, _shoutTradeTargetHeroOverride) || !hero.IsAlive) return false;
			}
			else if (character == null || !ReferenceEquals(character, _shoutTradeTargetCharacterOverride))
				return false;

			Agent agent = manager.OneToOneConversationAgent as Agent;
			if (_shoutTradeNativeAgent != null)
			{
				// Native conversation proxies need not satisfy scene-shout speech/health rules.
				return ReferenceEquals(agent, _shoutTradeNativeAgent) && agent.IsActive() && !agent.IsMainAgent
					&& (_shoutTradeTargetHeroOverride == null
						|| (agent.Character as CharacterObject)?.HeroObject == _shoutTradeTargetHeroOverride);
			}
			// A genuinely agentless map conversation is valid; a missing scene participant is not.
			return agent == null && _shoutTradeNativeMission == null && ConversationActionBoundaryBannerlordAdapter.IsNativeConversationWorldMapContext();
		}
		catch { return false; }
	}

	internal bool IsShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false)
	{
		if (_shoutTradeActionOnly && !requireCurrentShoutFrame)
			return IsNativeTradeTargetValidForCommit();
		NpcDataPacket expectedTarget = _shoutTradeTargetNpc;
		if (expectedTarget == null || expectedTarget.AgentIndex < 0)
		{
			return false;
		}
		try
		{
			Agent liveAgent = Mission.Current?.Agents?.FirstOrDefault(
				agent => agent != null && agent.Index == expectedTarget.AgentIndex);
			if (!CanAgentParticipateInSceneSpeech(liveAgent)
				|| liveAgent.Character is not CharacterObject liveCharacter)
			{
				return false;
			}
			// Agent indexes are scene-local; reject a recycled index when this UI flow already captured the original Agent.
			bool hasCapturedLiveAgent = _shoutTradeTargetAgentSnapshot != null;
			if (hasCapturedLiveAgent && !ReferenceEquals(liveAgent, _shoutTradeTargetAgentSnapshot))
			{
				return false;
			}
			if (requireCurrentShoutFrame
				&& !GetAgentsForShoutTargetingContext(_activeShoutTargetingContext)
					.Any(agent => agent != null && agent.Index == expectedTarget.AgentIndex))
			{
				return false;
			}
			if (_shoutTradeTargetHeroOverride != null
				&& liveCharacter.HeroObject != _shoutTradeTargetHeroOverride)
			{
				return false;
			}
			// A captured Agent is authoritative for non-Hero native dialogue; its proxy CharacterObject may legitimately differ.
			if (!hasCapturedLiveAgent
				&& _shoutTradeTargetCharacterOverride != null
				&& !ReferenceEquals(liveCharacter, _shoutTradeTargetCharacterOverride)
				&& !string.Equals(
					liveCharacter.StringId ?? "",
					_shoutTradeTargetCharacterOverride.StringId ?? "",
					StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (expectedTarget.IsHero)
			{
				return liveCharacter.HeroObject != null;
			}
			if (liveCharacter.HeroObject != null)
			{
				return false;
			}
			// ExtractNpcData appends the location SpecialTargetTag ("troop_id tag"); only the first token is the StringId.
			if (!hasCapturedLiveAgent
				&& !string.IsNullOrWhiteSpace(expectedTarget.TroopId)
				&& !string.Equals(
					expectedTarget.TroopId.Trim().Split(' ')[0],
					liveCharacter.StringId ?? "",
					StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (!hasCapturedLiveAgent && !string.IsNullOrWhiteSpace(expectedTarget.UnnamedKey))
			{
				NpcDataPacket liveTarget = ShoutUtils.ExtractNpcData(liveAgent);
				if (!string.Equals(
					expectedTarget.UnnamedKey,
					liveTarget?.UnnamedKey ?? "",
					StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal bool EnsureShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false)
	{
		if (IsShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame))
		{
			return true;
		}
		try
		{
			Logger.Log("ShoutBehavior", "[ShoutTrade] commit cancelled because primary target is no longer valid agent="
				+ (_shoutTradeTargetNpc?.AgentIndex ?? (-1))
				+ " native=" + _shoutTradeActionOnly
				+ " sameManager=" + ReferenceEquals(_shoutTradeNativeManager, Campaign.Current?.ConversationManager)
				+ " sameMission=" + ReferenceEquals(_shoutTradeNativeMission, Mission.Current)
				+ " inConversation=" + (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
				+ " sameAgent=" + ReferenceEquals(_shoutTradeNativeAgent, Campaign.Current?.ConversationManager?.OneToOneConversationAgent)
				+ " expected=" + (_shoutTradeTargetHeroOverride?.StringId ?? _shoutTradeTargetCharacterOverride?.StringId ?? "")
				+ " current=" + (Campaign.Current?.ConversationManager?.OneToOneConversationCharacter?.StringId ?? ""));
			InformationManager.DisplayMessage(new InformationMessage(
				"交易目标已经离场或失效，本次给予/展示没有执行。",
				new Color(1f, 0.45f, 0.25f)));
		}
		catch
		{
		}
		return false;
	}

	internal List<ShoutTradeResourceOption> BuildShoutTradeOptions()
	{
		List<ShoutTradeResourceOption> list = new List<ShoutTradeResourceOption>();
		string text = "";
		ResolveShoutTradeRuntimeTarget(out var hero, out var characterObject, out var _);
		if (IsShoutPartyTransferMode(_shoutTradeMode))
		{
			List<MyBehavior.PartyTransferPromptEntry> list2 = MyBehavior.BuildPartyTransferPromptEntriesForExternal(hero, characterObject, GetShoutTradeTargetAgentIndex());
			IEnumerable<MyBehavior.PartyTransferPromptEntry> enumerable = list2.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == (IsShoutTroopTransferMode(_shoutTradeMode) ? MyBehavior.PartyTransferEntrySection.PlayerTroops : MyBehavior.PartyTransferEntrySection.PlayerPrisoners));
			foreach (MyBehavior.PartyTransferPromptEntry item in enumerable)
			{
				list.Add(new ShoutTradeResourceOption
				{
					Name = item.DisplayName,
					AvailableAmount = item.Count,
					PartyEntry = item
				});
			}
			return list;
		}
		if (IsShoutSettlementTransferMode(_shoutTradeMode))
		{
			List<MyBehavior.SettlementTransferPromptEntry> list3 = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(hero, characterObject).Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.PlayerFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)).ToList();
			foreach (MyBehavior.SettlementTransferPromptEntry item in list3)
			{
				list.Add(new ShoutTradeResourceOption
				{
					Name = MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(item),
					AvailableAmount = 1,
					SettlementEntry = item
				});
			}
			return list;
		}
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return list;
		}
		if (IsShoutTradeShowMode(_shoutTradeMode))
		{
			text = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		}
		int num = Hero.MainHero?.Gold ?? 0;
		if (IsShoutTradeShowMode(_shoutTradeMode))
		{
			num = MyBehavior.GetRemainingShowableGoldForExternal(hero, text, num);
		}
		if (num > 0)
		{
			list.Add(new ShoutTradeResourceOption
			{
				IsGold = true,
				ItemId = null,
				Name = "第纳尔",
				AvailableAmount = num,
				Item = null
			});
		}
		ItemRoster itemRoster = mobileParty.ItemRoster;
		if (itemRoster != null)
		{
			Dictionary<string, ShoutTradeResourceOption> dictionary = new Dictionary<string, ShoutTradeResourceOption>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				int amount = elementCopyAtIndex.Amount;
				if (item == null || amount <= 0)
				{
					continue;
				}
				string text2 = (item.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (dictionary.TryGetValue(text2, out var value))
				{
					value.AvailableAmount += amount;
					value.InventoryTotalValue += (long)amount * (RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(elementCopyAtIndex.EquipmentElement) ?? 1);
				}
				else
				{
					dictionary[text2] = new ShoutTradeResourceOption
					{
						IsGold = false,
						ItemId = text2,
						Name = item.Name.ToString(),
						AvailableAmount = amount,
						Item = item,
						InventoryTotalValue = (long)amount * (RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(elementCopyAtIndex.EquipmentElement) ?? 1)
					};
				}
			}
			foreach (ShoutTradeResourceOption value2 in dictionary.Values)
			{
				int availableAmount = value2.AvailableAmount;
				value2.InventoryUnitValue = (availableAmount > 0) ? Math.Max(1, (int)Math.Round((double)value2.InventoryTotalValue / (double)availableAmount, MidpointRounding.AwayFromZero)) : 1;
				if (IsShoutTradeShowMode(_shoutTradeMode))
				{
					availableAmount = MyBehavior.GetRemainingShowableItemCountForExternal(hero, text, value2.ItemId, availableAmount);
				}
				if (availableAmount > 0)
				{
					value2.AvailableAmount = availableAmount;
					list.Add(value2);
				}
			}
		}
		return list;
	}

	internal void RecordNativeConversationTradeActionFact(string fact)
	{
		fact = (fact ?? "").Trim();
		if (string.IsNullOrWhiteSpace(fact))
		{
			return;
		}
		fact = AppendShoutTradeActionFactSequence(fact);
		fact = NormalizeNativeConversationFactLineForPrompt(fact, "玩家动作");
		Hero targetHero = _shoutTradeTargetHeroOverride;
		CharacterObject targetCharacter = _shoutTradeTargetCharacterOverride;
		string npcName = (_shoutTradeTargetNpc?.Name ?? "").Trim();
		try
		{
			if ((targetHero == null && targetCharacter == null) && TryResolveNativeConversationTarget(out var hero, out var character, out var resolvedName))
			{
				targetHero = hero ?? character?.HeroObject;
				targetCharacter = character ?? targetHero?.CharacterObject;
				if (string.IsNullOrWhiteSpace(npcName))
				{
					npcName = resolvedName;
				}
			}
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			}
			int targetAgentIndex = (_shoutTradeTargetNpc != null) ? _shoutTradeTargetNpc.AgentIndex : TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			if (targetAgentIndex >= 0)
			{
				string sharedFact = ContainsPlayerCraftedAfefInspectionSuffix(fact)
					? StripPlayerCraftedAfefInspectionSuffix(fact)
					: fact;
				AppendActionAfefFactToSceneHistoryInOrder(targetAgentIndex, sharedFact, mirrorToNativeSharedHistory: false);
				if (!string.Equals(sharedFact, fact, StringComparison.Ordinal))
				{
					PromotePersonalizedExtraFactInScenePrivateHistory(fact, targetAgentIndex);
				}
				QueuePendingCurrentAfefFactForAgent(targetAgentIndex, fact);
			}
			else
			{
				QueuePendingCurrentNativeAfefFactForKey(BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, _shoutTradeTargetNpc), fact);
			}
			if (targetHero != null)
			{
				MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
			}
			AppendNativeConversationSessionHistory(
				targetHero,
				targetCharacter,
				npcName,
				"玩家动作",
				fact,
				"fact",
				targetAgentIndex: targetAgentIndex,
				npc: _shoutTradeTargetNpc,
				bridgeToSceneHistory: false);
			Logger.Log("NativeConversationTrade", "recorded action-only fact target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? npcName) + " fact=" + fact.Replace("\r", "\\r").Replace("\n", "\\n"));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTrade", "[WARN] Failed to record action fact: " + ex.Message);
		}
	}

    internal void ApplyShoutGiveTransfer()
    {
        _lastTransferOutcomeFact = "";
        try { ApplyShoutGiveTransferCore(); }
        catch (Exception ex)
        {
            // Capture/setup or a secondary reporting leaf can fail outside the per-item mutation boundary.
            // The confirmation has already been consumed: never rebuild a menu or infer delivery.
            _lastTransferOutcomeFact += "给予执行中断；无法确认剩余资源与目标的变化，不能据此重复交付。";
            if (_shoutPendingTradeItems != null)
                foreach (var item in _shoutPendingTradeItems) if (item != null) item.Amount = 0;
            Logger.Log("ShoutBehavior", "[Trade] transfer boundary interrupted; flow consumed: " + ex);
        }
    }
    private void ApplyShoutGiveTransferCore()
    {
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return;
		}
		NpcDataPacket shoutTradeTargetNpc = _shoutTradeTargetNpc;
		ResolveShoutTradeRuntimeTarget(out var hero, out var characterObject, out var _);
		RewardSystemBehavior.SettlementMerchantKind settlementMerchantKind = RewardSystemBehavior.SettlementMerchantKind.None;
		bool flag = hero == null && characterObject != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out settlementMerchantKind);
		Settlement currentSettlement = Settlement.CurrentSettlement;
		int shoutTradeTargetAgentIndex = GetShoutTradeTargetAgentIndex();
		bool hasWildernessNonHeroParty = TryResolveWildernessNonHeroRewardParty(hero, characterObject, shoutTradeTargetAgentIndex, out var wildernessNonHeroParty);
		bool captureGcczSharedRelief = AfGcczShoutBridge.ShouldCaptureSharedReliefTransfer(shoutTradeTargetAgentIndex);
		string text = MyBehavior.BuildRuleTargetKeyForExternal(hero, characterObject, shoutTradeTargetAgentIndex);
		string playerCraftObserverKey = ResolveShownTradeTargetKey(shoutTradeTargetNpc, out var _);
		if (string.IsNullOrWhiteSpace(playerCraftObserverKey))
		{
			playerCraftObserverKey = text;
		}
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
            int sourceRemoved = 0;
            int sourceGoldBefore = Hero.MainHero?.Gold ?? 0;
            bool authoritativeReceipt = false;
            try
            {
			if (shoutPendingTradeItem.SettlementEntry != null)
			{
				string statusText = "";
				bool flag2 = RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryApplyPlayerSettlementTransferForExternal(hero, shoutPendingTradeItem.SettlementEntry, out statusText);
                shoutPendingTradeItem.Amount = flag2 ? 1 : 0;
                authoritativeReceipt = true;
				if (flag2)
				{
					InformationManager.DisplayMessage(new InformationMessage("已将 " + shoutPendingTradeItem.ItemName + " 转交给 " + GetShoutTradeTargetDisplayName() + "。", new Color(0.4f, 1f, 0.4f)));
				}
				else if (!string.IsNullOrWhiteSpace(statusText))
				{
					InformationManager.DisplayMessage(new InformationMessage(statusText, new Color(1f, 0.5f, 0.5f)));
				}
				shoutPendingTradeItem.Amount = flag2 ? 1 : 0;
			}
			else if (shoutPendingTradeItem.PartyEntry != null)
			{
                var partyEffect = MyBehavior.TransferPlayerPartyEntryWithObservedEffects(hero, characterObject, GetShoutTradeTargetAgentIndex(), shoutPendingTradeItem.PartyEntry, shoutPendingTradeItem.Amount);
                authoritativeReceipt = true;
                int num = partyEffect.Delivered;
                shoutPendingTradeItem.Amount = num;
                shoutPendingTradeItem.PartyTransferPartialFact = PartyTransferExecutionOwner.BuildPartialEffectFact(shoutPendingTradeItem.PartyEntry, partyEffect);
				if (num > 0)
				{
					string information = (shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops) ? ("已将 " + num + " 名" + shoutPendingTradeItem.ItemName + "转入" + GetShoutTradeTargetDisplayName() + "的麾下") : (shoutPendingTradeItem.PartyEntry.IsHero ? ("已将俘虏" + shoutPendingTradeItem.ItemName + "交给" + GetShoutTradeTargetDisplayName()) : ("已将 " + num + " 名" + shoutPendingTradeItem.ItemName + "俘虏交给" + GetShoutTradeTargetDisplayName()));
					InformationManager.DisplayMessage(new InformationMessage(information, new Color(0.4f, 1f, 0.4f)));
				}
			}
			else if (shoutPendingTradeItem.IsGold)
			{
				int num2 = Math.Min(shoutPendingTradeItem.Amount, Hero.MainHero.Gold);
				shoutPendingTradeItem.Amount = num2;
				if (num2 > 0)
				{
					if (captureGcczSharedRelief && AfGcczShoutBridge.CaptureSharedReliefGoldTransfer(shoutTradeTargetAgentIndex, num2))
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
                        authoritativeReceipt = true;
					}
					else if (hero != null)
					{
						int targetGoldBefore = hero.Gold;
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, num2);
                        shoutPendingTradeItem.Amount = Math.Min(Math.Max(0, sourceGoldBefore - Hero.MainHero.Gold), Math.Max(0, hero.Gold - targetGoldBefore));
                        authoritativeReceipt = shoutPendingTradeItem.Amount > 0;
                        RecordObservedTransferSourceRemainder(shoutPendingTradeItem, Math.Max(0, sourceGoldBefore - Hero.MainHero.Gold));
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, num2, null, 0);
						}
						catch
						{
						}
					}
					else if (flag && currentSettlement != null)
					{
						if (RewardSystemBehavior.Instance != null)
						{
							RewardSystemBehavior.Instance.TransferGoldToSettlement(currentSettlement, Hero.MainHero, num2);
						}
						else
						{
							GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
						}
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, num2, null, 0);
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"玩家来源已扣除 {Math.Max(0, sourceGoldBefore - Hero.MainHero.Gold)} 第纳尔；目标到账尚未确认。", characterObject?.Name?.ToString());
					}
					else
					{
						if (hasWildernessNonHeroParty && RewardSystemBehavior.Instance != null)
						{
							num2 = RewardSystemBehavior.Instance.TransferGoldToParty(wildernessNonHeroParty, Hero.MainHero, num2);
							shoutPendingTradeItem.Amount = num2;
                            authoritativeReceipt = true;
							if (num2 > 0)
							{
								InformationManager.DisplayMessage(new InformationMessage("已将 " + num2 + " 第纳尔交给 " + GetShoutTradeTargetDisplayName() + " 所在部队。", new Color(0.4f, 1f, 0.4f)));
								RecordScenePrepaidTransfer(text, num2);
							}
						}
						else
						{
							GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
							RecordScenePrepaidTransfer(text, num2);
						}
					}
				}
			}
			else
			{
				if (mobileParty == null)
				{
                    shoutPendingTradeItem.Amount = 0;
					continue;
				}
				ItemRoster itemRoster = mobileParty.ItemRoster;
				if (itemRoster == null)
				{
                    shoutPendingTradeItem.Amount = 0;
					continue;
				}
				string text2 = (shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
                    shoutPendingTradeItem.Amount = 0;
					continue;
				}
				if (!captureGcczSharedRelief && hasWildernessNonHeroParty && RewardSystemBehavior.Instance != null)
				{
					string itemName;
					int num3 = RewardSystemBehavior.Instance.TransferItemToParty(wildernessNonHeroParty, Hero.MainHero, text2, shoutPendingTradeItem.Amount, out itemName);
					shoutPendingTradeItem.Amount = num3;
                    authoritativeReceipt = true;
					if (num3 > 0 && !string.IsNullOrWhiteSpace(itemName))
					{
						shoutPendingTradeItem.ItemName = itemName;
					}
					continue;
				}
				ItemObject itemObject;
				int num4 = MyBehavior.RemoveItemsFromRosterByStringId(itemRoster, text2, shoutPendingTradeItem.Amount, out itemObject);
				shoutPendingTradeItem.Amount = num4;
                sourceRemoved = num4;
				if (num4 > 0)
				{
					if (captureGcczSharedRelief && AfGcczShoutBridge.CaptureSharedReliefItemTransfer(shoutTradeTargetAgentIndex, text2, num4, itemObject ?? shoutPendingTradeItem.Item, shoutPendingTradeItem.InventoryUnitValue))
					{
						continue;
					}
					if (hero?.PartyBelongedTo != null && itemObject != null)
					{
						int targetCountBefore = hero.PartyBelongedTo.ItemRoster.GetItemNumber(itemObject);
                        hero.PartyBelongedTo.ItemRoster.AddToCounts(itemObject, num4);
                        shoutPendingTradeItem.Amount = Math.Min(num4, Math.Max(0, hero.PartyBelongedTo.ItemRoster.GetItemNumber(itemObject) - targetCountBefore));
                        authoritativeReceipt = shoutPendingTradeItem.Amount > 0;
                        RecordObservedTransferSourceRemainder(shoutPendingTradeItem, num4);
					}
					else if (flag && currentSettlement?.ItemRoster != null && itemObject != null)
					{
						int targetCountBefore = currentSettlement.ItemRoster.GetItemNumber(itemObject);
                        currentSettlement.ItemRoster.AddToCounts(itemObject, num4);
                        shoutPendingTradeItem.Amount = Math.Min(num4, Math.Max(0, currentSettlement.ItemRoster.GetItemNumber(itemObject) - targetCountBefore));
                        authoritativeReceipt = shoutPendingTradeItem.Amount > 0;
                        RecordObservedTransferSourceRemainder(shoutPendingTradeItem, num4);
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, 0, text2, num4);
						string text3 = RewardSystemBehavior.Instance?.BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, num4) ?? "";
						string merchantFactItemName = CourierDeliveryBehavior.GetCourierLetterTransferFactDescriptionForExternal(
							text2,
							itemObject.Id.InternalValue,
							itemObject.Name?.ToString() ?? text2);
						merchantFactItemName = RewardSystemBehavior.DecoratePlayerCraftedAfefItemNameForExternal(
							text2,
							itemObject.Id.InternalValue,
							merchantFactItemName,
							null,
							characterObject,
							playerCraftObserverKey,
							"give",
							commit: true);
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {shoutPendingTradeItem.Amount} 个 {merchantFactItemName}{text3}。", characterObject?.Name?.ToString());
					}
					if (hero != null)
					{
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, 0, text2, num4);
						}
						catch
						{
						}
					}
				}
			}
                if (!authoritativeReceipt && shoutPendingTradeItem.Amount > 0)
                {
                    int removed = shoutPendingTradeItem.IsGold ? Math.Max(0, sourceGoldBefore - (Hero.MainHero?.Gold ?? sourceGoldBefore)) : sourceRemoved;
                    string observed = removed > 0 ? "已观察到来源减少 " + removed + "；目标是否收到未知。" : "来源与目标是否发生变化未知。";
                    _lastTransferOutcomeFact += "向" + GetShoutTradeTargetDisplayName() + "给予" + shoutPendingTradeItem.ItemName + "：" + observed + "\n";
                    shoutPendingTradeItem.Amount = 0;
                }
            }
            catch (Exception ex)
            {
                // A game leaf may throw after changing a source. Never turn an unobserved delivery into success or retry it.
                if (!authoritativeReceipt)
                {
                    int removed = shoutPendingTradeItem.IsGold ? Math.Max(0, sourceGoldBefore - (Hero.MainHero?.Gold ?? sourceGoldBefore)) : sourceRemoved;
                    string source = removed > 0 ? "已观察到来源减少 " + removed + "，目标是否收到未知。" : "来源和目标是否发生变化未知。";
                    _lastTransferOutcomeFact += "向" + GetShoutTradeTargetDisplayName() + "交付" + shoutPendingTradeItem.ItemName + "时执行中断；" + source;
                    shoutPendingTradeItem.Amount = 0;
                }
                for (int remaining = i + 1; remaining < _shoutPendingTradeItems.Count; remaining++)
                    _shoutPendingTradeItems[remaining].Amount = 0;
                Logger.Log("ShoutBehavior", "[Trade] effect interrupted; flow consumed: " + ex);
                break;
            }

		}
	}

    private void RecordObservedTransferSourceRemainder(ShoutPendingTradeItem item, int removed)
    {
        int unconfirmed = Math.Max(0, removed - item.Amount);
        if (unconfirmed > 0)
            _lastTransferOutcomeFact += "给予" + item.ItemName + "：来源另减少 " + unconfirmed + "，这部分目标是否收到未知。\n";
    }
    private string _lastTransferOutcomeFact = "";
    internal string BuildShoutTradeFactText(bool isGive)
    {
        string completed = BuildShoutTradeCompletedFactText(isGive);
        if (!isGive || string.IsNullOrWhiteSpace(_lastTransferOutcomeFact)) return completed;
        return string.IsNullOrWhiteSpace(completed) ? _lastTransferOutcomeFact : completed + "\n" + _lastTransferOutcomeFact;
    }
	private string BuildShoutTradeCompletedFactText(bool isGive)
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return "";
		}
		if (isGive && IsShoutSettlementTransferMode(_shoutTradeMode))
		{
			string playerName = GetPlayerDisplayNameForShout();
			string targetName = GetShoutTradeTargetDisplayName();
			List<string> facts = new List<string>();
			for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
			{
				ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
				if (shoutPendingTradeItem?.SettlementEntry != null && shoutPendingTradeItem.Amount > 0)
				{
					facts.Add(playerName + "已经将" + shoutPendingTradeItem.ItemName + "转交给" + targetName + "的家族。");
				}
			}
			return string.Join("\n", facts.Where((string x) => !string.IsNullOrWhiteSpace(x)));
		}
		if (isGive && IsShoutPartyTransferMode(_shoutTradeMode))
		{
			return BuildShoutPartyTransferFactText();
		}
		string text = GetPlayerDisplayNameForShout();
		string text2 = GetShoutTradeTargetDisplayName();
		ResolveShoutTradeRuntimeTarget(out var observerHero, out var observerCharacter, out var _);
		int observerAgentIndex = GetShoutTradeTargetAgentIndex();
		string observerKey = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out var _);
		if (string.IsNullOrWhiteSpace(observerKey))
		{
			observerKey = MyBehavior.BuildRuleTargetKeyForExternal(observerHero, observerCharacter, observerAgentIndex);
		}
		List<string> list = new List<string>();
		long num = 0L;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount > 0)
			{
				if (shoutPendingTradeItem.IsGold)
				{
					list.Add($"{shoutPendingTradeItem.Amount} 第纳尔");
					num += shoutPendingTradeItem.Amount;
				}
				else
				{
					string itemFactName = CourierDeliveryBehavior.GetCourierLetterTransferFactDescriptionForExternal(
						shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId,
						shoutPendingTradeItem.Item?.Id.InternalValue ?? 0u,
						shoutPendingTradeItem.ItemName);
					string playerCraftedItemId = (shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(playerCraftedItemId))
					{
						itemFactName = RewardSystemBehavior.DecoratePlayerCraftedAfefItemNameForExternal(
							playerCraftedItemId,
							shoutPendingTradeItem.Item?.Id.InternalValue ?? 0u,
							itemFactName,
							observerHero,
							observerCharacter ?? observerHero?.CharacterObject,
							observerKey,
							isGive ? "give" : "show",
							commit: true);
					}
					string text3;
					if (isGive)
					{
						text3 = shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "";
						string text4 = "";
						try
						{
							ResolveShoutTradeRuntimeTarget(out var hero, out var characterObject, out var _);
							if (characterObject != null && hero == null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var _))
							{
								text4 = RewardSystemBehavior.Instance.BuildSettlementItemValueFactSuffixForExternal(Settlement.CurrentSettlement, text3, shoutPendingTradeItem.Amount);
								num += RewardSystemBehavior.Instance.EstimateSettlementItemValueForExternal(Settlement.CurrentSettlement, text3, shoutPendingTradeItem.Amount);
							}
							else
							{
								text4 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(hero ?? Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? "";
								num += RewardSystemBehavior.Instance?.EstimateItemValueForExternal(hero ?? Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? 0L;
							}
						}
						catch
						{
							text4 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? "";
							num += RewardSystemBehavior.Instance?.EstimateItemValueForExternal(Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? 0L;
						}
						list.Add($"{shoutPendingTradeItem.Amount} 个 {itemFactName}{text4}");
					}
					else
					{
						string text5 = RewardSystemBehavior.Instance?.BuildInventoryActualItemValueFactSuffixForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? "";
						num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0L;
						list.Add($"{shoutPendingTradeItem.Amount} 个 {itemFactName}{text5}");
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		if (isGive)
		{
			string text6 = (num > 0L) ? ("（合计总值约 " + num + " 第纳尔）") : "";
			return text + "已经将 " + string.Join("、", list) + text6 + " 交给 " + text2 + "。如果你正在和"+text+"交易，并核对好了数量和价值，那么你现在可以将商量好的物品，第纳尔或人交给"+text+"";
		}
		return text + "给 " + text2 + " 看了看 总值为 " + num + " 第纳尔的各类财物：" + string.Join("、", list) + "，但暂未交付给你，不过证明了他有这些东西，如果你正在和他交易，请问他索要，尽量不要先把东西交给他，如果不是交易，就无需索要";
	}

	internal string BuildShoutPartyTransferFactText()
	{
		string playerName = GetPlayerDisplayNameForShout();
		string targetName = GetShoutTradeTargetDisplayName();
		List<string> facts = new List<string>();
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
            if (!string.IsNullOrWhiteSpace(shoutPendingTradeItem?.PartyTransferPartialFact))
                facts.Add(shoutPendingTradeItem.PartyTransferPartialFact);
			if (shoutPendingTradeItem?.PartyEntry == null || shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			string itemName = (shoutPendingTradeItem.ItemName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(itemName))
			{
				itemName = (shoutPendingTradeItem.PartyEntry.DisplayName ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(itemName))
			{
				itemName = "目标";
			}
			if (shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops)
			{
				facts.Add(playerName + "已经将 " + shoutPendingTradeItem.Amount + " 名" + itemName + "转入" + targetName + "的麾下。");
			}
			else if (shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerPrisoners)
			{
				if (shoutPendingTradeItem.PartyEntry.IsHero)
				{
					facts.Add(playerName + "已经将俘虏" + itemName + "交给" + targetName + "。");
				}
				else
				{
					facts.Add(playerName + "已经将 " + shoutPendingTradeItem.Amount + " 名" + itemName + "俘虏交给" + targetName + "。");
				}
			}
		}
		return string.Join("\n", facts.Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

	internal long EstimateShoutPendingShowTotalValue()
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return 0L;
		}
		long num = 0L;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem == null || shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.IsGold)
			{
				num += shoutPendingTradeItem.Amount;
			}
			else
			{
				num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0L;
			}
		}
		return num;
	}

	internal void ShowShoutPendingDisplayValueMessage(long totalValue)
	{
		if (totalValue <= 0)
		{
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage("【展示估值】你向 " + GetShoutTradeTargetDisplayName() + " 展示了总值为 " + totalValue + " 第纳尔的财物。", new Color(0.95f, 0.85f, 0.25f)));
	}

	internal static string GetPlayerDisplayNameForShout()
	{
		return MyBehavior.BuildPlayerPublicDisplayNameForExternal();
	}

	internal string GetShoutTradeTargetDisplayName()
	{
		string text = (_shoutTradeTargetNpc?.Name ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "对方" : text;
	}

	internal void RecordShoutShownResources()
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return;
		}
		Hero hero = null;
		string targetKey = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		if (hero == null && string.IsNullOrWhiteSpace(targetKey))
		{
			return;
		}
		int num = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.IsGold)
			{
				num += shoutPendingTradeItem.Amount;
				continue;
			}
			string text = (shoutPendingTradeItem.ItemId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!dictionary.ContainsKey(text))
				{
					dictionary[text] = 0;
				}
				dictionary[text] += shoutPendingTradeItem.Amount;
			}
		}
		MyBehavior.RecordShownResourcesForExternal(hero, targetKey, num, dictionary);
	}

	internal string ResolveShownTradeTargetKey(NpcDataPacket targetNpc, out Hero hero)
	{
		hero = null;
		if (targetNpc == null)
		{
			return "";
		}
		if (_shoutTradeTargetHeroOverride != null)
		{
			hero = _shoutTradeTargetHeroOverride;
			if (!string.IsNullOrWhiteSpace(hero.StringId))
			{
				return hero.StringId;
			}
		}
		if (targetNpc.IsHero)
		{
			try
			{
				hero = ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
			}
			catch
			{
				hero = null;
			}
			if (!string.IsNullOrWhiteSpace(hero?.StringId))
			{
				return hero.StringId;
			}
		}
		string text = (targetNpc.UnnamedKey ?? "").Trim().ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return AppendSceneAgentIdentityToShownTargetKey(text, targetNpc.AgentIndex);
		}
		string text2 = (targetNpc.TroopId ?? "").Trim().ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return AppendSceneAgentIdentityToShownTargetKey("troop:" + text2, targetNpc.AgentIndex);
		}
		if (!string.IsNullOrWhiteSpace(targetNpc.Name))
		{
			return AppendSceneAgentIdentityToShownTargetKey(
				("name:" + targetNpc.Name).Trim().ToLowerInvariant(),
				targetNpc.AgentIndex);
		}
		return "";
	}

	internal static string AppendSceneAgentIdentityToShownTargetKey(string targetKey, int agentIndex)
	{
		string key = (targetKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(key)
			|| key.IndexOf("|agent:", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return key;
		}
		string scope = ResolveShownTradeNonHeroScope(agentIndex);
		if (!string.IsNullOrWhiteSpace(scope)
			&& key.IndexOf("|scope:", StringComparison.OrdinalIgnoreCase) < 0)
		{
			key += "|scope:" + scope;
		}
		if (agentIndex < 0)
		{
			return key;
		}
		return key + "|agent:" + agentIndex;
	}

	internal static string ResolveShownTradeNonHeroScope(int agentIndex)
	{
		try
		{
			string settlementId = NormalizeWildernessNonHeroMemoryKeyPart(
				Settlement.CurrentSettlement?.StringId);
			if (!string.IsNullOrWhiteSpace(settlementId))
			{
				return "settlement:" + settlementId;
			}
			string partyId = NormalizeWildernessNonHeroMemoryKeyPart(
				TryResolveWildernessNonHeroMobileParty(agentIndex)?.StringId);
			if (!string.IsNullOrWhiteSpace(partyId))
			{
				return "party:" + partyId;
			}
			string sceneName = NormalizeWildernessNonHeroMemoryKeyPart(
				Mission.Current?.SceneName);
			if (!string.IsNullOrWhiteSpace(sceneName))
			{
				return "scene:" + sceneName;
			}
		}
		catch
		{
		}
		return "";
	}

	internal string GetShoutTradeTargetIneligibility(ShoutChatMode mode)
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

	internal static string GetNoShoutTradeOptionsMessage(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GiveTroops ? "你当前没有可转移给对方的部队。"
			: mode == ShoutChatMode.GivePrisoners ? "你当前没有可转移给对方的俘虏。"
			: IsShoutSettlementTransferMode(mode) ? "你当前没有可转移给对方的固定资产。"
			: "你没有可用的物品或第纳尔。";
	}

	internal static List<ScenePresentationTradeOption> LoadScenePresentationTradeOptionsForExternal(string mode, out string status)
	{
		status = "";
		List<ScenePresentationTradeOption> result = new List<ScenePresentationTradeOption>();
		SceneTradeController owner = CurrentInstance?._j17SceneTradeController;
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
		ScenePresentationController.Member addressee = owner.FindPresentationMember(owner.Presentation._presentationAddresseeIndex);
		NpcDataPacket target = owner.IsPresentationAudience(addressee) ? ShoutUtils.ExtractNpcData(addressee.Agent) : null;
		if (target == null)
		{
			status = "当前没有可以给予的对话对象。";
			return result;
		}
		// Reopening the give panel replaces a staged gift; refresh the session banner when that happens.
		bool hadStaged = owner.TradeStaged;
		owner.ReleasePresentationTrade();
		if (hadStaged)
		{
			BumpPresentation();
		}
		owner.StartFlowIdentity();
        owner.TradeOwnsState = true;
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
                FlowRevision = owner.FlowRevision,
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

    private ShoutTargetingContext _activeShoutTargetingContext { get => _ports.Get_activeShoutTargetingContext(); set => _ports.Set_activeShoutTargetingContext(value); }
    private List<Agent> GetAgentsForShoutTargetingContext(ShoutTargetingContext targetingContext) => _ports.GetAgentsForShoutTargetingContext_L1285(targetingContext);
    private List<ShoutTradeResourceOption> _shoutTradeOptions { get => _ports.Get_shoutTradeOptions(); set => _ports.Set_shoutTradeOptions(value); }
    private List<ShoutPendingTradeItem> _shoutPendingTradeItems { get => _ports.Get_shoutPendingTradeItems(); set => _ports.Set_shoutPendingTradeItems(value); }
    private int _shoutPendingTradeItemIndex { get => _ports.Get_shoutPendingTradeItemIndex(); set => _ports.Set_shoutPendingTradeItemIndex(value); }
    private ShoutChatMode _shoutTradeMode { get => _ports.Get_shoutTradeMode(); set => _ports.Set_shoutTradeMode(value); }
    private NpcDataPacket _shoutTradeTargetNpc { get => _ports.Get_shoutTradeTargetNpc(); set => _ports.Set_shoutTradeTargetNpc(value); }
    private ConversationManager _shoutTradeNativeManager => _ports.Get_shoutTradeNativeManager();
    private Mission _shoutTradeNativeMission => _ports.Get_shoutTradeNativeMission();
    private Agent _shoutTradeNativeAgent => _ports.Get_shoutTradeNativeAgent();
    private Agent _shoutTradeTargetAgentSnapshot { get => _ports.Get_shoutTradeTargetAgentSnapshot(); set => _ports.Set_shoutTradeTargetAgentSnapshot(value); }
    private bool _shoutTradeActionOnly { get => _ports.Get_shoutTradeActionOnly(); set => _ports.Set_shoutTradeActionOnly(value); }
    private Hero _shoutTradeTargetHeroOverride { get => _ports.Get_shoutTradeTargetHeroOverride(); set => _ports.Set_shoutTradeTargetHeroOverride(value); }
    private CharacterObject _shoutTradeTargetCharacterOverride { get => _ports.Get_shoutTradeTargetCharacterOverride(); set => _ports.Set_shoutTradeTargetCharacterOverride(value); }
    private int GetShoutTradeTargetAgentIndex() => _ports.GetShoutTradeTargetAgentIndex_L14076();
    private string AppendShoutTradeActionFactSequence(string fact) => _ports.AppendShoutTradeActionFactSequence_L14587(fact);
    private void RecordScenePrepaidTransfer(string targetKey, int goldAmount) => _ports.RecordScenePrepaidTransfer_L14987(targetKey, goldAmount);
    private void QueuePendingCurrentAfefFactForAgent(int targetAgentIndex, string fact) => _ports.QueuePendingCurrentAfefFactForAgent_L17336(targetAgentIndex, fact);
    private void QueuePendingCurrentNativeAfefFactForKey(string key, string fact) => _ports.QueuePendingCurrentNativeAfefFactForKey_L17363(key, fact);
    private void AppendActionAfefFactToSceneHistoryInOrder(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory = true) => _ports.AppendActionAfefFactToSceneHistoryInOrder_L17395(targetAgentIndex, fact, mirrorToNativeSharedHistory);
    private void PromotePersonalizedExtraFactInScenePrivateHistory(
		string personalizedExtraFact,
		int personalizedAgentIndex) => _ports.PromotePersonalizedExtraFactInScenePrivateHistory_L17524(personalizedExtraFact, personalizedAgentIndex);
    private Hero ResolveHeroFromAgentIndex(int agentIndex) => _ports.ResolveHeroFromAgentIndex_L17635(agentIndex);
    private ScenePresentationController Presentation { get => _ports.GetPresentation(); }
}

internal sealed class SceneTradeBannerlordAdapterPorts
{
    internal Func<Agent> Get_shoutTradeNativeAgent;
    internal Func<Mission> Get_shoutTradeNativeMission;
    internal Func<ConversationManager> Get_shoutTradeNativeManager;
    internal Func<ShoutTargetingContext> Get_activeShoutTargetingContext;
    internal Action<ShoutTargetingContext> Set_activeShoutTargetingContext;
    internal delegate List<Agent> GetAgentsForShoutTargetingContext_L1285Callback(ShoutTargetingContext targetingContext);
    internal GetAgentsForShoutTargetingContext_L1285Callback GetAgentsForShoutTargetingContext_L1285;
    internal Func<List<ShoutTradeResourceOption>> Get_shoutTradeOptions;
    internal Action<List<ShoutTradeResourceOption>> Set_shoutTradeOptions;
    internal Func<List<ShoutPendingTradeItem>> Get_shoutPendingTradeItems;
    internal Action<List<ShoutPendingTradeItem>> Set_shoutPendingTradeItems;
    internal Func<int> Get_shoutPendingTradeItemIndex;
    internal Action<int> Set_shoutPendingTradeItemIndex;
    internal Func<ShoutChatMode> Get_shoutTradeMode;
    internal Action<ShoutChatMode> Set_shoutTradeMode;
    internal Func<NpcDataPacket> Get_shoutTradeTargetNpc;
    internal Action<NpcDataPacket> Set_shoutTradeTargetNpc;
    internal Func<Agent> Get_shoutTradeTargetAgentSnapshot;
    internal Action<Agent> Set_shoutTradeTargetAgentSnapshot;
    internal Func<bool> Get_shoutTradeActionOnly;
    internal Action<bool> Set_shoutTradeActionOnly;
    internal Func<Hero> Get_shoutTradeTargetHeroOverride;
    internal Action<Hero> Set_shoutTradeTargetHeroOverride;
    internal Func<CharacterObject> Get_shoutTradeTargetCharacterOverride;
    internal Action<CharacterObject> Set_shoutTradeTargetCharacterOverride;
    internal delegate int GetShoutTradeTargetAgentIndex_L14076Callback();
    internal GetShoutTradeTargetAgentIndex_L14076Callback GetShoutTradeTargetAgentIndex_L14076;
    internal delegate string AppendShoutTradeActionFactSequence_L14587Callback(string fact);
    internal AppendShoutTradeActionFactSequence_L14587Callback AppendShoutTradeActionFactSequence_L14587;
    internal delegate void RecordScenePrepaidTransfer_L14987Callback(string targetKey, int goldAmount);
    internal RecordScenePrepaidTransfer_L14987Callback RecordScenePrepaidTransfer_L14987;
    internal delegate void QueuePendingCurrentAfefFactForAgent_L17336Callback(int targetAgentIndex, string fact);
    internal QueuePendingCurrentAfefFactForAgent_L17336Callback QueuePendingCurrentAfefFactForAgent_L17336;
    internal delegate void QueuePendingCurrentNativeAfefFactForKey_L17363Callback(string key, string fact);
    internal QueuePendingCurrentNativeAfefFactForKey_L17363Callback QueuePendingCurrentNativeAfefFactForKey_L17363;
    internal delegate void AppendActionAfefFactToSceneHistoryInOrder_L17395Callback(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory);
    internal AppendActionAfefFactToSceneHistoryInOrder_L17395Callback AppendActionAfefFactToSceneHistoryInOrder_L17395;
    internal delegate void PromotePersonalizedExtraFactInScenePrivateHistory_L17524Callback(string personalizedExtraFact, int personalizedAgentIndex);
    internal PromotePersonalizedExtraFactInScenePrivateHistory_L17524Callback PromotePersonalizedExtraFactInScenePrivateHistory_L17524;
    internal delegate Hero ResolveHeroFromAgentIndex_L17635Callback(int agentIndex);
    internal ResolveHeroFromAgentIndex_L17635Callback ResolveHeroFromAgentIndex_L17635;
    internal Func<ScenePresentationController> GetPresentation;
}
