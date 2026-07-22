using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

public static class PromptListRetrievalService
{
	private const int DefaultCandidateMaxCount = 10;
	private const int CandidateMaxHardCap = 30;
	private const int CandidateSnapshotMaxCount = 80;
	private const int CandidateSnapshotMaxAgeMinutes = 10;
	private const float MatchThreshold = 0.66f;
	public const string PlayerVisibleEquipmentSnapshotScope = "player_visible_equipment";
	public const string NpcRewardItemsSnapshotScope = "npc_reward_items";
	internal const string NpcRewardItemsAllSnapshotScope = "npc_reward_items_all";
	internal const string PartyRewardItemsAllSnapshotScope = "party_reward_items_all";
	public const string SettlementMerchantItemsSnapshotScope = "settlement_merchant_items";
	internal const string SettlementMerchantItemsAllSnapshotScope = "settlement_merchant_items_all";
	public const string PartyTransferTroopsSnapshotScope = "party_transfer_troops";
	internal const string PartyTransferAllTroopsSnapshotScope = "party_transfer_all_troops";
	public const string PartyTransferPrisonersSnapshotScope = "party_transfer_prisoners";
	internal const string PartyTransferAllPrisonersSnapshotScope = "party_transfer_all_prisoners";
	public const string SettlementTransferNpcAssetsSnapshotScope = "settlement_transfer_npc_assets";
	internal const string SettlementTransferAllNpcAssetsSnapshotScope = "settlement_transfer_all_npc_assets";
	[Obsolete("LLM postprocess player-to-NPC asset snapshots are no longer supported.")]
	public const string SettlementTransferPlayerAssetsSnapshotScope = "settlement_transfer_player_assets";

	private static readonly object CandidateSnapshotLock = new object();
	private static readonly Dictionary<string, CandidateSnapshot> CandidateSnapshots = new Dictionary<string, CandidateSnapshot>(StringComparer.OrdinalIgnoreCase);

	private sealed class CandidateMatch<T>
	{
		public T Value;

		public int Index;

		public int MentionPriority;

		public float Score;
	}

	private sealed class CandidateSnapshot
	{
		public DateTime CreatedUtc;

		public List<RewardSystemBehavior.RewardItemInfo> RewardItems;

		public List<MyBehavior.PartyTransferPromptEntry> PartyTransferEntries;

		public List<MyBehavior.SettlementTransferPromptEntry> SettlementTransferEntries;
	}

	public static int GetMaxCandidateCount()
	{
		try
		{
			return ClampCandidateLimit(DuelSettings.GetSettings()?.PromptListCandidateMaxCount ?? DefaultCandidateMaxCount);
		}
		catch
		{
			return DefaultCandidateMaxCount;
		}
	}

	public static int ClampCandidateLimit(int value)
	{
		if (value <= 0)
		{
			value = DefaultCandidateMaxCount;
		}
		return Math.Max(1, Math.Min(CandidateMaxHardCap, value));
	}

	public static string BuildCandidateSnapshotKey(string scope, Hero targetHero = null, CharacterObject targetCharacter = null, int targetAgentIndex = -1, string discriminator = null)
	{
		string text = (scope ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "default";
		}
		string heroId = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
		string characterId = (targetCharacter?.StringId ?? "").Trim();
		string characterKey = string.IsNullOrWhiteSpace(heroId) ? characterId : "";
		string settlementId = "";
		try
		{
			settlementId = (Settlement.CurrentSettlement?.StringId ?? "").Trim();
		}
		catch
		{
			settlementId = "";
		}
		string entity = !string.IsNullOrWhiteSpace(heroId) ? ("hero:" + heroId) : (!string.IsNullOrWhiteSpace(characterId) ? ("character:" + characterId) : "entity:none");
		string agent = targetAgentIndex >= 0 ? targetAgentIndex.ToString(CultureInfo.InvariantCulture) : "-1";
		string extra = (discriminator ?? "").Trim();
		return text + "|entity=" + entity + "|character=" + characterKey + "|agent=" + agent + "|settlement=" + settlementId + "|extra=" + extra;
	}

	public static void PublishRewardItemSnapshot(string scope, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, IEnumerable<RewardSystemBehavior.RewardItemInfo> items, string discriminator = null)
	{
		List<RewardSystemBehavior.RewardItemInfo> list = (items ?? Enumerable.Empty<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null).ToList();
		PublishSnapshot(BuildCandidateSnapshotKey(scope, targetHero, targetCharacter, targetAgentIndex, discriminator), new CandidateSnapshot
		{
			RewardItems = list
		});
	}

	public static bool TryGetRewardItemSnapshot(string scope, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out List<RewardSystemBehavior.RewardItemInfo> items, string discriminator = null)
	{
		items = null;
		if (!TryGetSnapshot(BuildCandidateSnapshotKey(scope, targetHero, targetCharacter, targetAgentIndex, discriminator), out var snapshot) || snapshot?.RewardItems == null)
		{
			return false;
		}
		items = snapshot.RewardItems.Where((RewardSystemBehavior.RewardItemInfo x) => x != null).ToList();
		return items.Count > 0;
	}

	public static void PublishPartyTransferSnapshot(string scope, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, IEnumerable<MyBehavior.PartyTransferPromptEntry> entries, string discriminator = null)
	{
		List<MyBehavior.PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>()).Where((MyBehavior.PartyTransferPromptEntry x) => x != null).ToList();
		PublishSnapshot(BuildCandidateSnapshotKey(scope, targetHero, targetCharacter, targetAgentIndex, discriminator), new CandidateSnapshot
		{
			PartyTransferEntries = list
		});
	}

	public static bool TryGetPartyTransferSnapshot(string scope, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out List<MyBehavior.PartyTransferPromptEntry> entries, string discriminator = null)
	{
		entries = null;
		if (!TryGetSnapshot(BuildCandidateSnapshotKey(scope, targetHero, targetCharacter, targetAgentIndex, discriminator), out var snapshot) || snapshot?.PartyTransferEntries == null)
		{
			return false;
		}
		entries = snapshot.PartyTransferEntries.Where((MyBehavior.PartyTransferPromptEntry x) => x != null).ToList();
		return entries.Count > 0;
	}

	public static void PublishSettlementTransferSnapshot(string scope, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, IEnumerable<MyBehavior.SettlementTransferPromptEntry> entries, string discriminator = null)
	{
		List<MyBehavior.SettlementTransferPromptEntry> list = (entries ?? Enumerable.Empty<MyBehavior.SettlementTransferPromptEntry>()).Where(MyBehavior.IsSettlementTransferEntryValidForExternal).ToList();
		PublishSnapshot(BuildCandidateSnapshotKey(scope, targetHero, targetCharacter, targetAgentIndex, discriminator), new CandidateSnapshot
		{
			SettlementTransferEntries = list
		});
	}

	public static bool TryGetSettlementTransferSnapshot(string scope, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out List<MyBehavior.SettlementTransferPromptEntry> entries, string discriminator = null)
	{
		entries = null;
		if (!TryGetSnapshot(BuildCandidateSnapshotKey(scope, targetHero, targetCharacter, targetAgentIndex, discriminator), out var snapshot) || snapshot?.SettlementTransferEntries == null)
		{
			return false;
		}
		entries = snapshot.SettlementTransferEntries.Where(MyBehavior.IsSettlementTransferEntryValidForExternal).ToList();
		return entries.Count > 0;
	}

	private static void PublishSnapshot(string key, CandidateSnapshot snapshot)
	{
		if (string.IsNullOrWhiteSpace(key) || snapshot == null)
		{
			return;
		}
		lock (CandidateSnapshotLock)
		{
			snapshot.CreatedUtc = DateTime.UtcNow;
			CandidateSnapshots[key] = snapshot;
			TrimSnapshotsLocked();
		}
	}

	private static bool TryGetSnapshot(string key, out CandidateSnapshot snapshot)
	{
		snapshot = null;
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		lock (CandidateSnapshotLock)
		{
			if (!CandidateSnapshots.TryGetValue(key, out snapshot) || snapshot == null)
			{
				return false;
			}
			if (DateTime.UtcNow - snapshot.CreatedUtc > TimeSpan.FromMinutes(CandidateSnapshotMaxAgeMinutes))
			{
				CandidateSnapshots.Remove(key);
				snapshot = null;
				return false;
			}
			return true;
		}
	}

	private static void TrimSnapshotsLocked()
	{
		DateTime cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(CandidateSnapshotMaxAgeMinutes);
		foreach (string key in CandidateSnapshots.Where((KeyValuePair<string, CandidateSnapshot> x) => x.Value == null || x.Value.CreatedUtc < cutoff).Select((KeyValuePair<string, CandidateSnapshot> x) => x.Key).ToList())
		{
			CandidateSnapshots.Remove(key);
		}
		if (CandidateSnapshots.Count <= CandidateSnapshotMaxCount)
		{
			return;
		}
		foreach (string key in CandidateSnapshots.OrderBy((KeyValuePair<string, CandidateSnapshot> x) => x.Value?.CreatedUtc ?? DateTime.MinValue).Take(Math.Max(0, CandidateSnapshots.Count - CandidateSnapshotMaxCount)).Select((KeyValuePair<string, CandidateSnapshot> x) => x.Key).ToList())
		{
			CandidateSnapshots.Remove(key);
		}
	}

	public static List<string> BuildMentionTerms(MentionedWorldEntities mentions)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddTerms(result, seen, mentions?.Entities);
		return result;
	}

	public static List<T> SelectCandidates<T>(IEnumerable<T> candidates, MentionedWorldEntities mentions, Func<T, IEnumerable<string>> aliasesFactory, int maxCount = 0, bool fillWithFallback = true) where T : class
	{
		List<T> list = (candidates ?? Enumerable.Empty<T>()).Where((T x) => x != null).ToList();
		if (list.Count == 0)
		{
			return new List<T>();
		}
		int limit = ClampCandidateLimit(maxCount);
		List<string> terms = BuildMentionTerms(mentions);
		if (terms.Count == 0 || aliasesFactory == null)
		{
			return fillWithFallback ? list.Take(limit).ToList() : new List<T>();
		}
		Dictionary<string, int> mentionPriority = BuildMentionPriority(terms);
		List<CandidateMatch<T>> matches = new List<CandidateMatch<T>>();
		for (int i = 0; i < list.Count; i++)
		{
			T candidate = list[i];
			List<string> aliases = BuildDistinctAliases(aliasesFactory(candidate));
			if (aliases.Count == 0)
			{
				continue;
			}
			float bestScore = 0f;
			int bestPriority = int.MaxValue;
			int positiveAliasMatches = 0;
			foreach (string term in terms)
			{
				if (string.IsNullOrWhiteSpace(term))
				{
					continue;
				}
				float termBest = 0f;
				foreach (string alias in aliases)
				{
					float score = CalculateFuzzyScore(term, alias);
					if (score > termBest)
					{
						termBest = score;
					}
					if (score >= MatchThreshold)
					{
						positiveAliasMatches++;
					}
				}
				if (termBest > bestScore)
				{
					bestScore = termBest;
					bestPriority = mentionPriority.TryGetValue(term, out var priority) ? priority : int.MaxValue;
				}
			}
			if (bestScore >= MatchThreshold)
			{
				matches.Add(new CandidateMatch<T>
				{
					Value = candidate,
					Index = i,
					MentionPriority = bestPriority,
					Score = bestScore + Math.Min(0.18f, positiveAliasMatches * 0.02f)
				});
			}
		}
		if (matches.Count == 0)
		{
			return fillWithFallback ? list.Take(limit).ToList() : new List<T>();
		}
		List<T> selected = matches
			.OrderByDescending((CandidateMatch<T> x) => x.Score)
			.ThenBy((CandidateMatch<T> x) => x.MentionPriority)
			.ThenBy((CandidateMatch<T> x) => x.Index)
			.Take(limit)
			.Select((CandidateMatch<T> x) => x.Value)
			.ToList();
		if (fillWithFallback && selected.Count < limit)
		{
			HashSet<T> selectedSet = new HashSet<T>(selected);
			foreach (T candidate in list)
			{
				if (selected.Count >= limit)
				{
					break;
				}
				if (selectedSet.Add(candidate))
				{
					selected.Add(candidate);
				}
			}
		}
		return selected;
	}

	public static List<RewardSystemBehavior.RewardItemInfo> FilterRewardItems(IEnumerable<RewardSystemBehavior.RewardItemInfo> candidates, MentionedWorldEntities mentions, int maxCount = 0)
	{
		return FilterRewardItemsCore(candidates, mentions, maxCount, keepPrivateEquipmentOutsideLimit: false);
	}

	public static List<RewardSystemBehavior.RewardItemInfo> FilterNpcRewardItemsForAssetTransfer(IEnumerable<RewardSystemBehavior.RewardItemInfo> candidates, MentionedWorldEntities mentions, int maxCount = 0)
	{
		return FilterRewardItemsCore(candidates, mentions, maxCount, keepPrivateEquipmentOutsideLimit: true);
	}

	private static List<RewardSystemBehavior.RewardItemInfo> FilterRewardItemsCore(IEnumerable<RewardSystemBehavior.RewardItemInfo> candidates, MentionedWorldEntities mentions, int maxCount, bool keepPrivateEquipmentOutsideLimit)
	{
		List<RewardSystemBehavior.RewardItemInfo> list = (candidates ?? Enumerable.Empty<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null).ToList();
		if (list.Count == 0)
		{
			return new List<RewardSystemBehavior.RewardItemInfo>();
		}
		List<RewardSystemBehavior.RewardItemInfo> privateItems = list.Where((RewardSystemBehavior.RewardItemInfo x) => x.IsPrivateEquipment).ToList();
		if (privateItems.Count == 0)
		{
			return SelectCandidates(list, mentions, GetRewardItemAliases, maxCount);
		}
		int limit = ClampCandidateLimit(maxCount);
		List<RewardSystemBehavior.RewardItemInfo> publicItems = list.Where((RewardSystemBehavior.RewardItemInfo x) => !x.IsPrivateEquipment).ToList();
		if (keepPrivateEquipmentOutsideLimit)
		{
			List<RewardSystemBehavior.RewardItemInfo> selectedPublicItems = SelectCandidates(publicItems, mentions, GetRewardItemAliases, limit);
			List<RewardSystemBehavior.RewardItemInfo> persistentResult = new List<RewardSystemBehavior.RewardItemInfo>(selectedPublicItems.Count + privateItems.Count);
			persistentResult.AddRange(selectedPublicItems);
			persistentResult.AddRange(privateItems);
			return persistentResult;
		}
		List<RewardSystemBehavior.RewardItemInfo> result = SelectCandidates(privateItems, mentions, GetRewardItemAliases, limit);
		if (result.Count < limit)
		{
			result.AddRange(SelectCandidates(publicItems, mentions, GetRewardItemAliases, limit - result.Count));
		}
		return result.Take(limit).ToList();
	}

	public static List<MyBehavior.PartyTransferPromptEntry> FilterPartyTransferEntries(IEnumerable<MyBehavior.PartyTransferPromptEntry> candidates, MentionedWorldEntities mentions, int maxCount = 0, bool isPrisoner = false)
	{
		return SelectCandidates(candidates, mentions, (MyBehavior.PartyTransferPromptEntry x) => GetPartyTransferAliases(x, isPrisoner), maxCount);
	}

	public static List<MyBehavior.SettlementTransferPromptEntry> FilterSettlementTransferEntries(IEnumerable<MyBehavior.SettlementTransferPromptEntry> candidates, MentionedWorldEntities mentions, int maxCount = 0)
	{
		return SelectCandidates(candidates, mentions, GetSettlementTransferAliases, maxCount);
	}

	public static string BuildRemainingRewardItemsSummary(IEnumerable<RewardSystemBehavior.RewardItemInfo> allCandidates, IEnumerable<RewardSystemBehavior.RewardItemInfo> shownCandidates, string ownerLabel = "你")
	{
		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		HashSet<RewardSystemBehavior.RewardItemInfo> shown = new HashSet<RewardSystemBehavior.RewardItemInfo>((shownCandidates ?? Enumerable.Empty<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null));
		foreach (RewardSystemBehavior.RewardItemInfo item in allCandidates ?? Enumerable.Empty<RewardSystemBehavior.RewardItemInfo>())
		{
			if (item == null || shown.Contains(item) || item.Count <= 0)
			{
				continue;
			}
			AddRemainderCount(counts, GetRewardItemRemainderTypeLabel(item), Math.Max(1, item.Count));
		}
		return BuildRemainingSummaryText(counts, ownerLabel);
	}

	public static string BuildRemainingPartyTransferSummary(IEnumerable<MyBehavior.PartyTransferPromptEntry> allCandidates, IEnumerable<MyBehavior.PartyTransferPromptEntry> shownCandidates, bool isPrisoner, string ownerLabel = "你")
	{
		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		HashSet<MyBehavior.PartyTransferPromptEntry> shown = new HashSet<MyBehavior.PartyTransferPromptEntry>((shownCandidates ?? Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>()).Where((MyBehavior.PartyTransferPromptEntry x) => x != null));
		foreach (MyBehavior.PartyTransferPromptEntry entry in allCandidates ?? Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>())
		{
			if (entry == null || shown.Contains(entry) || entry.Count <= 0)
			{
				continue;
			}
			AddRemainderCount(counts, GetPartyTransferRemainderTypeLabel(entry, isPrisoner), Math.Max(1, entry.Count));
		}
		return BuildRemainingSummaryText(counts, ownerLabel);
	}

	public static string BuildRemainingSettlementTransferSummary(IEnumerable<MyBehavior.SettlementTransferPromptEntry> allCandidates, IEnumerable<MyBehavior.SettlementTransferPromptEntry> shownCandidates, string ownerLabel = "你")
	{
		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		HashSet<MyBehavior.SettlementTransferPromptEntry> shown = new HashSet<MyBehavior.SettlementTransferPromptEntry>((shownCandidates ?? Enumerable.Empty<MyBehavior.SettlementTransferPromptEntry>()).Where((MyBehavior.SettlementTransferPromptEntry x) => x != null));
		foreach (MyBehavior.SettlementTransferPromptEntry entry in allCandidates ?? Enumerable.Empty<MyBehavior.SettlementTransferPromptEntry>())
		{
			if (!MyBehavior.IsSettlementTransferEntryValidForExternal(entry) || shown.Contains(entry))
			{
				continue;
			}
			AddRemainderCount(counts, GetSettlementTransferRemainderTypeLabel(entry), 1);
		}
		return BuildRemainingSummaryText(counts, ownerLabel);
	}

	public static IEnumerable<string> GetRewardItemAliases(RewardSystemBehavior.RewardItemInfo item)
	{
		List<string> aliases = new List<string>();
		if (item == null)
		{
			return aliases;
		}
		AddAlias(aliases, item.Name);
		AddAlias(aliases, item.StringId);
		AddAlias(aliases, item.PromptStringId);
		AddAlias(aliases, item.ModifierStringId);
		ItemObject itemObject = item.Item;
		if (itemObject != null)
		{
			AddAlias(aliases, itemObject.Name?.ToString());
			AddAlias(aliases, itemObject.StringId);
			AddAlias(aliases, itemObject.Type.ToString());
			AddAlias(aliases, RewardSystemBehavior.GetItemPromptTypeLabelForExternal(itemObject));
			try
			{
				AddAlias(aliases, itemObject.ItemCategory?.StringId);
				AddAlias(aliases, itemObject.ItemCategory?.GetName()?.ToString());
			}
			catch
			{
			}
			AddItemTypeAliases(aliases, itemObject);
		}
		if (item.IsPrivateEquipment)
		{
			AddAlias(aliases, "私人装备");
			AddAlias(aliases, "装备");
			AddAlias(aliases, "身上装备");
		}
		return aliases;
	}

	public static IEnumerable<string> GetPartyTransferAliases(MyBehavior.PartyTransferPromptEntry entry, bool isPrisoner)
	{
		List<string> aliases = new List<string>();
		if (entry == null)
		{
			return aliases;
		}
		AddAlias(aliases, entry.DisplayName);
		CharacterObject character = entry.Character;
		if (character != null)
		{
			AddAlias(aliases, character.Name?.ToString());
			AddAlias(aliases, character.StringId);
			AddAlias(aliases, MyBehavior.GetPartyTransferTroopTypeLabelForExternal(character));
			try
			{
				AddAlias(aliases, character.Culture?.Name?.ToString());
				AddAlias(aliases, character.Culture?.StringId);
			}
			catch
			{
			}
			AddTroopTypeAliases(aliases, character);
		}
		int tier = Math.Max(0, character?.Tier ?? 0);
		AddAlias(aliases, "T" + tier.ToString(CultureInfo.InvariantCulture));
		AddAlias(aliases, tier.ToString(CultureInfo.InvariantCulture) + "阶");
		AddAlias(aliases, tier.ToString(CultureInfo.InvariantCulture) + "级");
		if (tier >= 4)
		{
			AddAlias(aliases, "高阶");
			AddAlias(aliases, "高级");
			AddAlias(aliases, "精锐");
		}
		else if (tier > 0 && tier <= 2)
		{
			AddAlias(aliases, "低阶");
			AddAlias(aliases, "低级");
			AddAlias(aliases, "新兵");
		}
		if (isPrisoner || entry.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners || entry.Section == MyBehavior.PartyTransferEntrySection.PlayerPrisoners)
		{
			AddAlias(aliases, "俘虏");
			AddAlias(aliases, "囚犯");
			AddAlias(aliases, "战俘");
			if (entry.IsHero || character?.IsHero == true)
			{
				AddAlias(aliases, "英雄俘虏");
				AddAlias(aliases, "贵族俘虏");
				AddAlias(aliases, "领主俘虏");
			}
		}
		else
		{
			AddAlias(aliases, "部队");
			AddAlias(aliases, "士兵");
			AddAlias(aliases, "兵种");
			if (entry.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers)
			{
				AddAlias(aliases, "要人募兵");
				AddAlias(aliases, "志愿兵");
				AddAlias(aliases, "可招募");
			}
		}
		return aliases;
	}

	public static IEnumerable<string> GetSettlementTransferAliases(MyBehavior.SettlementTransferPromptEntry entry)
	{
		List<string> aliases = new List<string>();
		if (entry == null)
		{
			return aliases;
		}
		AddAlias(aliases, entry.DisplayName);
		AddAlias(aliases, entry.SettlementId);
		AddAlias(aliases, entry.AssetId);
		AddAlias(aliases, entry.TypeLabel);
		AddAlias(aliases, MyBehavior.GetSettlementTransferAssetIdForExternal(entry));
		AddAlias(aliases, MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(entry));
		AddAlias(aliases, entry.OwnerClan?.Name?.ToString());
		AddAlias(aliases, entry.OwnerClan?.StringId);
		Settlement settlement = entry.Settlement;
		if (settlement != null)
		{
			AddAlias(aliases, settlement.Name?.ToString());
			AddAlias(aliases, settlement.StringId);
			AddAlias(aliases, settlement.OwnerClan?.Name?.ToString());
			AddAlias(aliases, settlement.OwnerClan?.StringId);
			if (settlement.IsTown)
			{
				AddAlias(aliases, "城市");
				AddAlias(aliases, "城镇");
			}
			if (settlement.IsCastle)
			{
				AddAlias(aliases, "城堡");
				AddAlias(aliases, "堡垒");
			}
		}
		try
		{
			AddAlias(aliases, entry.Workshop?.Name?.ToString());
		}
		catch
		{
		}
		try
		{
			AddAlias(aliases, entry.CaravanParty?.Name?.ToString());
			AddAlias(aliases, entry.CaravanParty?.StringId);
		}
		catch
		{
		}
		switch (entry.AssetKind)
		{
		case MyBehavior.SettlementTransferAssetKind.Settlement:
			AddAlias(aliases, "固定资产");
			AddAlias(aliases, "封地");
			AddAlias(aliases, "领地");
			AddAlias(aliases, "城市");
			AddAlias(aliases, "城堡");
			break;
		case MyBehavior.SettlementTransferAssetKind.Workshop:
			AddAlias(aliases, "固定资产");
			AddAlias(aliases, "工坊");
			AddAlias(aliases, "作坊");
			AddAlias(aliases, "店铺");
			break;
		case MyBehavior.SettlementTransferAssetKind.Caravan:
			AddAlias(aliases, "固定资产");
			AddAlias(aliases, "商队");
			AddAlias(aliases, "商船队");
			break;
		}
		return aliases;
	}

	private static string GetRewardItemRemainderTypeLabel(RewardSystemBehavior.RewardItemInfo itemInfo)
	{
		ItemObject item = itemInfo?.Item;
		if (item == null)
		{
			return itemInfo?.IsPrivateEquipment == true ? "装备" : "物品";
		}
		try
		{
			if (item.IsFood)
			{
				return "食物";
			}
			if (item.HasHorseComponent || item.Type == ItemObject.ItemTypeEnum.Horse)
			{
				return "马匹";
			}
			if (item.IsAnimal || item.Type == ItemObject.ItemTypeEnum.Animal)
			{
				return "牲畜";
			}
		}
		catch
		{
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			return "单手武器";
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			return "双手武器";
		case ItemObject.ItemTypeEnum.Polearm:
			return "长柄武器";
		case ItemObject.ItemTypeEnum.Bow:
			return "弓";
		case ItemObject.ItemTypeEnum.Crossbow:
			return "弩";
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Bullets:
			return "弹药";
		case ItemObject.ItemTypeEnum.Shield:
			return "盾牌";
		case ItemObject.ItemTypeEnum.Thrown:
			return "投掷武器";
		case ItemObject.ItemTypeEnum.HeadArmor:
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
		case ItemObject.ItemTypeEnum.LegArmor:
		case ItemObject.ItemTypeEnum.HandArmor:
		case ItemObject.ItemTypeEnum.Cape:
		case ItemObject.ItemTypeEnum.HorseHarness:
			return "护甲";
		case ItemObject.ItemTypeEnum.Goods:
			return "贸易品";
		}
		string label = RewardSystemBehavior.GetItemPromptTypeLabelForExternal(item);
		return string.IsNullOrWhiteSpace(label) ? (itemInfo?.IsPrivateEquipment == true ? "装备" : "物品") : label.Trim();
	}

	private static string GetPartyTransferRemainderTypeLabel(MyBehavior.PartyTransferPromptEntry entry, bool isPrisoner)
	{
		if (entry?.IsHero == true || entry?.Character?.IsHero == true)
		{
			return isPrisoner ? "英雄俘虏" : "英雄";
		}
		string type = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(entry?.Character);
		if (string.IsNullOrWhiteSpace(type))
		{
			type = "步兵";
		}
		int tier = Math.Max(0, entry?.Character?.Tier ?? 0);
		string prefix = "";
		if (tier >= 4)
		{
			prefix = "高阶";
		}
		else if (tier > 0 && tier <= 2)
		{
			prefix = "低阶";
		}
		string label = prefix + type.Trim();
		return isPrisoner ? (label + "俘虏") : label;
	}

	private static string GetSettlementTransferRemainderTypeLabel(MyBehavior.SettlementTransferPromptEntry entry)
	{
		if (entry == null)
		{
			return "固定资产";
		}
		Settlement settlement = entry.Settlement;
		if (settlement?.IsTown == true)
		{
			return "城市";
		}
		if (settlement?.IsCastle == true)
		{
			return "城堡";
		}
		if (!string.IsNullOrWhiteSpace(entry.TypeLabel))
		{
			return entry.TypeLabel.Trim();
		}
		switch (entry.AssetKind)
		{
		case MyBehavior.SettlementTransferAssetKind.Workshop:
			return "工坊";
		case MyBehavior.SettlementTransferAssetKind.Caravan:
			return "商队";
		case MyBehavior.SettlementTransferAssetKind.Settlement:
			return "领地";
		default:
			return "固定资产";
		}
	}

	private static void AddRemainderCount(Dictionary<string, int> counts, string label, int amount)
	{
		if (counts == null || amount <= 0)
		{
			return;
		}
		string key = (label ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			key = "其他";
		}
		counts.TryGetValue(key, out var current);
		counts[key] = current + amount;
	}

	private static string BuildRemainingSummaryText(Dictionary<string, int> counts, string ownerLabel)
	{
		List<KeyValuePair<string, int>> parts = (counts ?? new Dictionary<string, int>())
			.Where((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0)
			.OrderByDescending((KeyValuePair<string, int> x) => x.Value)
			.ThenBy((KeyValuePair<string, int> x) => x.Key, StringComparer.Ordinal)
			.ToList();
		if (parts.Count == 0)
		{
			return "";
		}
		int hiddenTypeCount = Math.Max(0, parts.Count - 8);
		string body = string.Join("、", parts.Take(8).Select((KeyValuePair<string, int> x) => x.Key + " x" + Math.Max(1, x.Value).ToString(CultureInfo.InvariantCulture)));
		if (hiddenTypeCount > 0)
		{
			body += " 等" + hiddenTypeCount.ToString(CultureInfo.InvariantCulture) + "类";
		}
		string owner = string.IsNullOrWhiteSpace(ownerLabel) ? "你" : ownerLabel.Trim();
		return "除此之外，" + owner + "还有未逐条展示的候选类型：" + body + "。";
	}

	private static void AddItemTypeAliases(List<string> aliases, ItemObject item)
	{
		if (item == null)
		{
			return;
		}
		try
		{
			if (item.IsFood)
			{
				AddAlias(aliases, "食物");
				AddAlias(aliases, "粮食");
				AddAlias(aliases, "粮草");
				AddAlias(aliases, "口粮");
			}
			if (item.IsAnimal)
			{
				AddAlias(aliases, "牲畜");
				AddAlias(aliases, "动物");
			}
			if (item.HasHorseComponent)
			{
				AddAlias(aliases, "马");
				AddAlias(aliases, "马匹");
				AddAlias(aliases, "坐骑");
			}
		}
		catch
		{
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Horse:
			AddAlias(aliases, "马");
			AddAlias(aliases, "马匹");
			AddAlias(aliases, "坐骑");
			break;
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			AddAlias(aliases, "单手武器");
			AddAlias(aliases, "剑");
			AddAlias(aliases, "刀");
			break;
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			AddAlias(aliases, "双手武器");
			AddAlias(aliases, "大剑");
			AddAlias(aliases, "长柄武器");
			break;
		case ItemObject.ItemTypeEnum.Polearm:
			AddAlias(aliases, "长柄武器");
			AddAlias(aliases, "长枪");
			AddAlias(aliases, "枪矛");
			break;
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
			AddAlias(aliases, "箭");
			AddAlias(aliases, "箭矢");
			AddAlias(aliases, "弩矢");
			AddAlias(aliases, "弹药");
			break;
		case ItemObject.ItemTypeEnum.Shield:
			AddAlias(aliases, "盾");
			AddAlias(aliases, "盾牌");
			break;
		case ItemObject.ItemTypeEnum.Bow:
		case ItemObject.ItemTypeEnum.Crossbow:
			AddAlias(aliases, "弓");
			AddAlias(aliases, "弓箭");
			AddAlias(aliases, "弩");
			AddAlias(aliases, "远程武器");
			break;
		case ItemObject.ItemTypeEnum.Thrown:
			AddAlias(aliases, "投掷武器");
			AddAlias(aliases, "标枪");
			AddAlias(aliases, "飞刀");
			break;
		case ItemObject.ItemTypeEnum.HeadArmor:
			AddAlias(aliases, "头盔");
			AddAlias(aliases, "盔");
			AddAlias(aliases, "甲");
			break;
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
			AddAlias(aliases, "甲");
			AddAlias(aliases, "盔甲");
			AddAlias(aliases, "护甲");
			AddAlias(aliases, "身甲");
			AddAlias(aliases, "铠甲");
			break;
		case ItemObject.ItemTypeEnum.LegArmor:
			AddAlias(aliases, "腿甲");
			AddAlias(aliases, "甲");
			break;
		case ItemObject.ItemTypeEnum.HandArmor:
			AddAlias(aliases, "手甲");
			AddAlias(aliases, "甲");
			break;
		case ItemObject.ItemTypeEnum.HorseHarness:
			AddAlias(aliases, "马具");
			AddAlias(aliases, "马甲");
			AddAlias(aliases, "甲");
			break;
		case ItemObject.ItemTypeEnum.Goods:
			AddAlias(aliases, "货物");
			AddAlias(aliases, "商品");
			break;
		case ItemObject.ItemTypeEnum.Animal:
			AddAlias(aliases, "牲畜");
			AddAlias(aliases, "动物");
			AddAlias(aliases, "马");
			break;
		}
	}

	private static void AddTroopTypeAliases(List<string> aliases, CharacterObject character)
	{
		if (character == null)
		{
			return;
		}
		string type = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(character);
		AddAlias(aliases, type);
		if (string.Equals(type, "弓手", StringComparison.Ordinal))
		{
			AddAlias(aliases, "弓箭手");
			AddAlias(aliases, "射手");
			AddAlias(aliases, "远程");
		}
		if (string.Equals(type, "骑兵", StringComparison.Ordinal))
		{
			AddAlias(aliases, "马兵");
			AddAlias(aliases, "骑手");
			AddAlias(aliases, "骑士");
		}
		if (string.Equals(type, "骑射手", StringComparison.Ordinal))
		{
			AddAlias(aliases, "骑射");
			AddAlias(aliases, "马弓手");
			AddAlias(aliases, "骑弓手");
		}
		if (string.Equals(type, "步兵", StringComparison.Ordinal))
		{
			AddAlias(aliases, "步卒");
			AddAlias(aliases, "近战");
		}
	}

	private static void AddTerms(List<string> result, HashSet<string> seen, IEnumerable<string> values)
	{
		if (result == null || seen == null || values == null)
		{
			return;
		}
		foreach (string value in values)
		{
			string text = (value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
			{
				result.Add(text);
			}
		}
	}

	private static void AddAlias(List<string> aliases, string value)
	{
		if (aliases == null)
		{
			return;
		}
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!aliases.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			aliases.Add(text);
		}
	}

	private static List<string> BuildDistinctAliases(IEnumerable<string> aliases)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string alias in aliases ?? Enumerable.Empty<string>())
		{
			string text = (alias ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
			{
				result.Add(text);
			}
		}
		return result;
	}

	private static Dictionary<string, int> BuildMentionPriority(List<string> terms)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < (terms?.Count ?? 0); i++)
		{
			string text = (terms[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !result.ContainsKey(text))
			{
				result[text] = i;
			}
		}
		return result;
	}

	private static float CalculateFuzzyScore(string mention, string alias)
	{
		string left = NormalizeFuzzyText(mention);
		string right = NormalizeFuzzyText(alias);
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return 0f;
		}
		if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
		{
			return 1f;
		}
		if (left.Contains(right) || right.Contains(left))
		{
			int min = Math.Min(left.Length, right.Length);
			int max = Math.Max(left.Length, right.Length);
			float ratio = max <= 0 ? 0f : (float)min / max;
			return Math.Max(0.78f, Math.Min(0.96f, 0.82f + ratio * 0.14f));
		}
		float tokenScore = CalculateTokenOverlapScore(left, right);
		if (tokenScore > 0f)
		{
			return tokenScore;
		}
		if (HasCjk(left) || HasCjk(right))
		{
			return CalculateCjkOverlapScore(left, right);
		}
		int maxLen = Math.Max(left.Length, right.Length);
		if (maxLen <= 0)
		{
			return 0f;
		}
		int distance = LevenshteinDistance(left, right, 64);
		if (distance < 0)
		{
			return 0f;
		}
		return Math.Max(0f, 1f - (float)distance / maxLen);
	}

	private static string NormalizeFuzzyText(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char ch in text)
		{
			if (char.IsLetterOrDigit(ch) || IsCjk(ch))
			{
				stringBuilder.Append(ch);
			}
			else if (char.IsWhiteSpace(ch) || ch == '_' || ch == '-' || ch == '/' || ch == '\\')
			{
				stringBuilder.Append(' ');
			}
		}
		return Regex.Replace(stringBuilder.ToString(), "\\s+", " ").Trim();
	}

	private static float CalculateTokenOverlapScore(string left, string right)
	{
		string[] leftTokens = left.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string[] rightTokens = right.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (leftTokens.Length == 0 || rightTokens.Length == 0)
		{
			return 0f;
		}
		HashSet<string> leftSet = new HashSet<string>(leftTokens, StringComparer.OrdinalIgnoreCase);
		HashSet<string> rightSet = new HashSet<string>(rightTokens, StringComparer.OrdinalIgnoreCase);
		int overlap = leftSet.Count((string x) => rightSet.Contains(x));
		if (overlap <= 0)
		{
			return 0f;
		}
		float precision = (float)overlap / Math.Max(1, rightSet.Count);
		float recall = (float)overlap / Math.Max(1, leftSet.Count);
		return Math.Max(precision, recall) >= 0.5f ? Math.Max(precision, recall) : 0f;
	}

	private static float CalculateCjkOverlapScore(string left, string right)
	{
		HashSet<char> leftChars = new HashSet<char>(left.Where(IsCjk));
		HashSet<char> rightChars = new HashSet<char>(right.Where(IsCjk));
		if (leftChars.Count == 0 || rightChars.Count == 0)
		{
			return 0f;
		}
		int overlap = leftChars.Count((char x) => rightChars.Contains(x));
		if (overlap <= 0)
		{
			return 0f;
		}
		float score = (float)overlap / Math.Min(leftChars.Count, rightChars.Count);
		return score >= 0.66f ? score : 0f;
	}

	private static bool HasCjk(string value)
	{
		return !string.IsNullOrWhiteSpace(value) && value.Any(IsCjk);
	}

	private static bool IsCjk(char ch)
	{
		return (ch >= '\u3400' && ch <= '\u9fff') || (ch >= '\uf900' && ch <= '\ufaff');
	}

	private static int LevenshteinDistance(string left, string right, int maxLength)
	{
		if (left == null || right == null)
		{
			return -1;
		}
		if (left.Length > maxLength || right.Length > maxLength)
		{
			return -1;
		}
		int[,] d = new int[left.Length + 1, right.Length + 1];
		for (int i = 0; i <= left.Length; i++)
		{
			d[i, 0] = i;
		}
		for (int j = 0; j <= right.Length; j++)
		{
			d[0, j] = j;
		}
		for (int i = 1; i <= left.Length; i++)
		{
			for (int j = 1; j <= right.Length; j++)
			{
				int cost = left[i - 1] == right[j - 1] ? 0 : 1;
				d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
			}
		}
		return d[left.Length, right.Length];
	}
}
