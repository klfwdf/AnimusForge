using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	private List<RewardItemInfo> GetHeroVisibleEquipmentItemsForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return new List<RewardItemInfo>();
		}
		bool useCivilianEquipment = false;
		TryResolvePromptEquipmentContext(hero, out useCivilianEquipment);
		List<RewardItemInfo> heroEquipmentItems = GetHeroEquipmentItems(hero, useCivilianEquipment);
		if (heroEquipmentItems.Count > 0)
		{
			return heroEquipmentItems;
		}
		if (hero == Hero.MainHero && Agent.Main != null && Agent.Main.IsActive())
		{
			List<RewardItemInfo> agentEquipmentItems = GetAgentEquipmentItems(Agent.Main);
			if (agentEquipmentItems.Count > 0)
			{
				return agentEquipmentItems;
			}
		}
		return GetHeroBattleEquipmentItems(hero);
	}

	public List<RewardItemInfo> BuildSettlementMerchantPostprocessItems(CharacterObject character, Settlement settlement = null, int maxItems = 0)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return list;
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		if (settlement == null || !settlement.IsTown || settlement.ItemRoster == null)
		{
			return list;
		}
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		ItemRoster itemRoster = settlement.ItemRoster;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
			ItemObject item = equipmentElement.Item;
			if (item == null || elementCopyAtIndex.Amount <= 0 || !MatchesSettlementMerchantKind(item, kind))
			{
				continue;
			}
			string text = BuildSettlementMerchantInventoryKey(equipmentElement);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = (dictionary[text] = new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId ?? "",
					PromptStringId = text,
					ModifierStringId = equipmentElement.ItemModifier?.StringId ?? "",
					Name = BuildSettlementMerchantDisplayName(equipmentElement),
					Count = 0,
					GuidePrice = TryGetSettlementBuyPrice(settlement, equipmentElement, out var merchantPostprocessPrice) ? Math.Max(1, merchantPostprocessPrice) : Math.Max(1, item.Value),
					EquipmentElement = equipmentElement
				});
			}
			value.Count += elementCopyAtIndex.Amount;
		}
		IEnumerable<RewardItemInfo> enumerable = dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal);
		if (maxItems > 0)
		{
			enumerable = enumerable.Take(maxItems);
		}
		return enumerable.ToList();
	}

	public List<RewardItemInfo> BuildHeroRewardPostprocessItems(Hero hero, int maxItems = 0)
	{
		List<RewardItemInfo> marketItems = BuildNotableMarketItems(hero)
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.ToList();
		List<RewardItemInfo> list = GetHeroInventoryItems(hero)
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.OrderByDescending((RewardItemInfo x) => x.Count)
			.ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal)
			.ToList();
		List<RewardItemInfo> list2 = GetHeroBattleEquipmentItems(hero)
			.Where((RewardItemInfo x) => x != null && x.Item != null)
			.Select(delegate(RewardItemInfo x)
			{
				x.IsPrivateEquipment = true;
				return x;
			})
			.OrderBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal)
			.ToList();
		List<RewardItemInfo> list3 = new List<RewardItemInfo>(marketItems.Count + list.Count + list2.Count);
		list3.AddRange(marketItems);
		list3.AddRange(list);
		list3.AddRange(list2);
		if (maxItems > 0)
		{
			return list3.Take(maxItems).ToList();
		}
		return list3;
	}

	public string BuildVisibleEquipmentPostprocessListForAI(Hero hero, int maxItems = 64)
	{
		if (hero == null)
		{
			return "（无）";
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return "（无）";
		}
		List<RewardItemInfo> filteredItems;
		if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, out filteredItems))
		{
			filteredItems = heroVisibleEquipmentItemsForPrompt
				.Where((RewardItemInfo x) => x != null && x.Item != null)
				.OrderByDescending((RewardItemInfo x) => x.Count)
				.ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal)
				.Take(Math.Max(1, maxItems))
				.ToList();
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (RewardItemInfo item in filteredItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			stringBuilder.Append(item.Name ?? item.StringId ?? "未知物品")
				.Append(" | type=")
				.Append(GetItemPromptTypeLabel(item.Item))
				.Append(" | x")
				.Append(Math.Max(1, item.Count))
				.Append(" | inventoryUnitValue=")
				.Append(Math.Max(1, GetVisibleEquipmentActualUnitValue(item)))
				.AppendLine();
		}
		return stringBuilder.Length > 0 ? stringBuilder.ToString().TrimEnd() : "（无）";
	}

	public string BuildVisibleEquipmentPostprocessListForAI(Hero hero, MentionedWorldEntities mentions, int maxItems = 0)
	{
		if (hero == null)
		{
			return "（无）";
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return "（无）";
		}
		List<RewardItemInfo> orderedItems = heroVisibleEquipmentItemsForPrompt.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal).ToList();
		List<RewardItemInfo> filteredItems;
		if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, out filteredItems))
		{
			filteredItems = PromptListRetrievalService.FilterRewardItems(orderedItems, mentions, maxItems);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (RewardItemInfo item in filteredItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			stringBuilder.Append(item.Name ?? item.StringId ?? "未知物品")
				.Append(" | type=")
				.Append(GetItemPromptTypeLabel(item.Item))
				.Append(" | x")
				.Append(Math.Max(1, item.Count))
				.Append(" | inventoryUnitValue=")
				.Append(Math.Max(1, GetVisibleEquipmentActualUnitValue(item)))
				.AppendLine();
		}
		return stringBuilder.Length > 0 ? stringBuilder.ToString().TrimEnd() : "（无）";
	}

	public string BuildFilteredInventorySummaryForAI(Hero hero, MentionedWorldEntities mentions, int maxItems = 0, bool includeGuidePrice = true, bool includePrivateBattleEquipment = false)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			List<RewardItemInfo> allOptions = BuildHeroRewardPostprocessItems(hero)
				.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
				.ToList();
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, hero, hero?.CharacterObject, -1, allOptions);
			List<RewardItemInfo> displayCandidates = allOptions;
			if (!includePrivateBattleEquipment)
			{
				displayCandidates = allOptions.Where((RewardItemInfo x) => !x.IsPrivateEquipment).ToList();
			}
			List<RewardItemInfo> options = includePrivateBattleEquipment
				? PromptListRetrievalService.FilterNpcRewardItemsForAssetTransfer(displayCandidates, mentions, maxItems)
				: PromptListRetrievalService.FilterRewardItems(displayCandidates, mentions, maxItems);
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope, hero, hero?.CharacterObject, -1, options);
			int gold = IsNotableMarketHero(hero, ResolveNotableMarketSettlement(hero)) ? GetRewardPostprocessGoldForHero(hero) : GetHeroGold(hero);
			return BuildFilteredItemSummaryForAI(options, gold, includeGuidePrice, allOptions, "你");
		}
		catch
		{
			return "";
		}
	}

	public string BuildFilteredSettlementMerchantInventorySummaryForAI(CharacterObject character, MentionedWorldEntities mentions, int maxItems = 0, Settlement settlement = null, bool includeGuidePrice = true)
	{
		try
		{
			settlement = settlement ?? Settlement.CurrentSettlement;
			List<RewardItemInfo> allOptions = BuildSettlementMerchantPostprocessItems(character, settlement)
				.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
				.ToList();
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, character, -1, allOptions);
			List<RewardItemInfo> options = PromptListRetrievalService.FilterRewardItems(allOptions, mentions, maxItems);
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsSnapshotScope, null, character, -1, options);
			int gold = GetSettlementMarketTradeGold(settlement);
			return BuildFilteredItemSummaryForAI(options, gold, includeGuidePrice, allOptions, "你");
		}
		catch
		{
			return "";
		}
	}

	private static string BuildFilteredItemSummaryForAI(List<RewardItemInfo> options, int gold, bool includeGuidePrice, List<RewardItemInfo> allOptions = null, string ownerLabel = "你")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("第纳尔: ").Append(Math.Max(0, gold)).AppendLine();
		if (options == null || options.Count == 0)
		{
			stringBuilder.AppendLine("（本轮检索清单无可显示物品）");
			stringBuilder.Append("全部可转物品总值: ").Append(CalculateRewardItemsTotalValueForExternal(allOptions)).AppendLine(" 第纳尔（不含第纳尔）");
			return stringBuilder.ToString().TrimEnd();
		}
		List<RewardItemInfo> publicItems = options.Where((RewardItemInfo x) => x != null && !x.IsPrivateEquipment).ToList();
		List<RewardItemInfo> privateItems = options.Where((RewardItemInfo x) => x != null && x.IsPrivateEquipment).ToList();
		AppendFilteredItemSummarySection(stringBuilder, "库存物品：", publicItems, includeGuidePrice);
		AppendFilteredItemSummarySection(stringBuilder, "私人战斗装备：", privateItems, includeGuidePrice);
		string remainder = PromptListRetrievalService.BuildRemainingRewardItemsSummary(allOptions, options, ownerLabel);
		if (!string.IsNullOrWhiteSpace(remainder))
		{
			stringBuilder.AppendLine(remainder);
		}
		stringBuilder.Append("全部可转物品总值: ").Append(CalculateRewardItemsTotalValueForExternal(allOptions ?? options)).AppendLine(" 第纳尔（不含第纳尔）");
		return stringBuilder.ToString().TrimEnd();
	}
}
