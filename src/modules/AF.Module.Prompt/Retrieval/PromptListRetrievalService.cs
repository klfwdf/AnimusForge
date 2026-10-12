using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
	private static readonly PromptCandidateSnapshotIndex CandidateSnapshotIndex = new PromptCandidateSnapshotIndex(CandidateSnapshotMaxCount, TimeSpan.FromMinutes(CandidateSnapshotMaxAgeMinutes));

	private sealed class CandidateSnapshot
	{
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
			CandidateSnapshots[key] = snapshot;
			foreach (string expiredKey in CandidateSnapshotIndex.Publish(key, DateTime.UtcNow))
			{
				CandidateSnapshots.Remove(expiredKey);
			}
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
			if (!CandidateSnapshotIndex.IsFresh(key, DateTime.UtcNow))
			{
				CandidateSnapshots.Remove(key);
				return false;
			}
			if (!CandidateSnapshots.TryGetValue(key, out snapshot) || snapshot == null)
			{
				return false;
			}
			return true;
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
		List<PromptCandidateDescriptor> descriptions = new List<PromptCandidateDescriptor>(list.Count);
		Dictionary<T, int> equalityGroups = new Dictionary<T, int>();
		for (int i = 0; i < list.Count; i++)
		{
			T candidate = list[i];
			if (!equalityGroups.TryGetValue(candidate, out int group))
			{
				group = i;
				equalityGroups[candidate] = group;
			}
			descriptions.Add(new PromptCandidateDescriptor(i, (aliasesFactory(candidate) ?? Enumerable.Empty<string>()).ToList(), group));
		}
		return PromptCandidateSelection.SelectIndices(descriptions, terms, limit, fillWithFallback).Select(index => list[index]).ToList();
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

	public static string BuildRemainingRewardItemsSummary(IEnumerable<RewardSystemBehavior.RewardItemInfo> allCandidates, IEnumerable<RewardSystemBehavior.RewardItemInfo> shownCandidates, string ownerLabel = null)
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
		return BuildRemainingSummaryText(counts, ownerLabel ?? PromptRetrievalTexts.Prompt("DefaultOwner"));
	}

	public static string BuildRemainingPartyTransferSummary(IEnumerable<MyBehavior.PartyTransferPromptEntry> allCandidates, IEnumerable<MyBehavior.PartyTransferPromptEntry> shownCandidates, bool isPrisoner, string ownerLabel = null)
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
		return BuildRemainingSummaryText(counts, ownerLabel ?? PromptRetrievalTexts.Prompt("DefaultOwner"));
	}

	public static string BuildRemainingSettlementTransferSummary(IEnumerable<MyBehavior.SettlementTransferPromptEntry> allCandidates, IEnumerable<MyBehavior.SettlementTransferPromptEntry> shownCandidates, string ownerLabel = null)
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
		return BuildRemainingSummaryText(counts, ownerLabel ?? PromptRetrievalTexts.Prompt("DefaultOwner"));
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
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_7b165403022c"));
			AddAlias(aliases, "private equipment");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_144a327039fe"));
			AddAlias(aliases, "equipment");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_cecbde9ca1e4"));
			AddAlias(aliases, "worn equipment");
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
			AddAlias(aliases, EconomyPromptTexts.ProjectSource(MyBehavior.GetPartyTransferTroopTypeLabelForExternal(character)));
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
		AddAlias(aliases, tier.ToString(CultureInfo.InvariantCulture) + PromptRetrievalTexts.Raw("Raw_0e13242a813e"));
		AddAlias(aliases, tier.ToString(CultureInfo.InvariantCulture) + PromptRetrievalTexts.Raw("Raw_6e2d7f7c077c"));
        AddAlias(aliases, "tier " + tier.ToString(CultureInfo.InvariantCulture));
        AddAlias(aliases, "level " + tier.ToString(CultureInfo.InvariantCulture));
		if (tier >= 4)
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_a5399ff0bbbb"));
			AddAlias(aliases, "high tier");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d6e61888b078"));
			AddAlias(aliases, "high level");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_0425e9c31a48"));
			AddAlias(aliases, "elite");
		}
		else if (tier > 0 && tier <= 2)
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_463dd3e3d8a1"));
			AddAlias(aliases, "low tier");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_b65f8cbdeece"));
			AddAlias(aliases, "low level");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9a30fea1d2a7"));
			AddAlias(aliases, "recruits");
		}
		if (isPrisoner || entry.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners || entry.Section == MyBehavior.PartyTransferEntrySection.PlayerPrisoners)
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d4d7cb2d3fda"));
			AddAlias(aliases, "prisoners");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d16bf156e5f9"));
			AddAlias(aliases, "captives");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_995318f779e6"));
			AddAlias(aliases, "prisoners of war");
			if (entry.IsHero || character?.IsHero == true)
			{
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_5adcca8021b4"));
				AddAlias(aliases, "hero prisoners");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_1574b70d3435"));
				AddAlias(aliases, "noble prisoners");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_8559f30ae6fa"));
				AddAlias(aliases, "lord prisoners");
			}
		}
		else
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_3a4927628668"));
			AddAlias(aliases, "troops");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_eaa8dd04fd68"));
			AddAlias(aliases, "soldiers");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_ec172417f61d"));
			AddAlias(aliases, "troop type");
			if (entry.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers)
			{
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_fd0aaa452708"));
				AddAlias(aliases, "notable recruitment");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_177bbc8721a2"));
				AddAlias(aliases, "volunteers");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_4eaf93b3f318"));
				AddAlias(aliases, "recruitable");
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
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_ed84a1665f79"));
				AddAlias(aliases, "city");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f8e6a6ef09fb"));
				AddAlias(aliases, "town");
			}
			if (settlement.IsCastle)
			{
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f6f854a2bc96"));
				AddAlias(aliases, "castle");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_ec48dc10a769"));
				AddAlias(aliases, "fortress");
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
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_49be60b1a1b2"));
			AddAlias(aliases, "fixed assets");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_21ee56440eca"));
			AddAlias(aliases, "fief");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f9bab1c5399d"));
			AddAlias(aliases, "holdings");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_ed84a1665f79"));
			AddAlias(aliases, "city");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f6f854a2bc96"));
			AddAlias(aliases, "castle");
			break;
		case MyBehavior.SettlementTransferAssetKind.Workshop:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_49be60b1a1b2"));
			AddAlias(aliases, "fixed assets");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_c647c592b70e"));
			AddAlias(aliases, "workshop");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_fcd1cf260b43"));
			AddAlias(aliases, "workshop");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_7c517652a3a7"));
			AddAlias(aliases, "shop");
			break;
		case MyBehavior.SettlementTransferAssetKind.Caravan:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_49be60b1a1b2"));
			AddAlias(aliases, "fixed assets");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_2f05eb3e6e27"));
			AddAlias(aliases, "caravan");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_6162ae9bab6c"));
			AddAlias(aliases, "trade fleet");
			break;
		}
		return aliases;
	}

	private static string GetRewardItemRemainderTypeLabel(RewardSystemBehavior.RewardItemInfo itemInfo)
	{
		ItemObject item = itemInfo?.Item;
		if (item == null)
		{
			return itemInfo?.IsPrivateEquipment == true ? AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_1") : AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_2");
		}
		try
		{
			if (item.IsFood)
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_3");
			}
			if (item.HasHorseComponent || item.Type == ItemObject.ItemTypeEnum.Horse)
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_4");
			}
			if (item.IsAnimal || item.Type == ItemObject.ItemTypeEnum.Animal)
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_5");
			}
		}
		catch
		{
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_6");
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_7");
		case ItemObject.ItemTypeEnum.Polearm:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_8");
		case ItemObject.ItemTypeEnum.Bow:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_9");
		case ItemObject.ItemTypeEnum.Crossbow:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_10");
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Bullets:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_11");
		case ItemObject.ItemTypeEnum.Shield:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_12");
		case ItemObject.ItemTypeEnum.Thrown:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_13");
		case ItemObject.ItemTypeEnum.HeadArmor:
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
		case ItemObject.ItemTypeEnum.LegArmor:
		case ItemObject.ItemTypeEnum.HandArmor:
		case ItemObject.ItemTypeEnum.Cape:
		case ItemObject.ItemTypeEnum.HorseHarness:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_14");
		case ItemObject.ItemTypeEnum.Goods:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_15");
		}
		string label = RewardSystemBehavior.GetItemPromptTypeLabelForExternal(item);
		return string.IsNullOrWhiteSpace(label) ? (itemInfo?.IsPrivateEquipment == true ? AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_1") : AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_2")) : label.Trim();
	}

	private static string GetPartyTransferRemainderTypeLabel(MyBehavior.PartyTransferPromptEntry entry, bool isPrisoner)
	{
		if (entry?.IsHero == true || entry?.Character?.IsHero == true)
		{
			return isPrisoner ? AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_16") : AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_17");
		}
		string type = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(entry?.Character);
		if (string.IsNullOrWhiteSpace(type))
		{
			type = AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_18");
		}
		int tier = Math.Max(0, entry?.Character?.Tier ?? 0);
		string prefix = "";
		if (tier >= 4)
		{
			prefix = AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_19");
		}
		else if (tier > 0 && tier <= 2)
		{
			prefix = AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_20");
		}
		string label = prefix + type.Trim();
		return isPrisoner ? (label + AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_21")) : label;
	}

	private static string GetSettlementTransferRemainderTypeLabel(MyBehavior.SettlementTransferPromptEntry entry)
	{
		if (entry == null)
		{
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_22");
		}
		Settlement settlement = entry.Settlement;
		if (settlement?.IsTown == true)
		{
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_23");
		}
		if (settlement?.IsCastle == true)
		{
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_24");
		}
		if (!string.IsNullOrWhiteSpace(entry.TypeLabel))
		{
			return entry.TypeLabel.Trim();
		}
		switch (entry.AssetKind)
		{
		case MyBehavior.SettlementTransferAssetKind.Workshop:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_25");
		case MyBehavior.SettlementTransferAssetKind.Caravan:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_26");
		case MyBehavior.SettlementTransferAssetKind.Settlement:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_27");
		default:
			return AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_22");
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
			key = AdapterPromptCaptureTexts.PcapCompat("Project_Remainder_28");
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
		string separator = AdapterPromptCaptureTexts.PcapText("remainder_separator", ", ");
		string body = string.Join(separator, parts.Take(8).Select((KeyValuePair<string, int> x) => AdapterPromptCaptureTexts.PcapProjectKnown(x.Key) + " x" + Math.Max(1, x.Value).ToString(CultureInfo.InvariantCulture)));
		if (hiddenTypeCount > 0)
		{
			body += AdapterPromptCaptureTexts.PcapFormat("remainder_more_types", " and {COUNT} more types", ("COUNT", hiddenTypeCount.ToString(CultureInfo.InvariantCulture)));
		}
		string owner = string.IsNullOrWhiteSpace(ownerLabel) ? AdapterPromptCaptureTexts.PcapText("Project_Remainder_29") : AdapterPromptCaptureTexts.PcapProjectKnown(ownerLabel.Trim());
		return AdapterPromptCaptureTexts.PcapFormat("remainder_summary", "In addition, {OWNER} have candidate types not listed individually: {BODY}.", ("OWNER", owner), ("BODY", body));
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
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_ab2775f0d353"));
				AddAlias(aliases, "food");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_661b85c39112"));
				AddAlias(aliases, "grain");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_fc6b568be3c6"));
				AddAlias(aliases, "supplies");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_7db734f85dc9"));
				AddAlias(aliases, "rations");
			}
			if (item.IsAnimal)
			{
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9fa24900d772"));
				AddAlias(aliases, "livestock");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_7ebb07e850a4"));
				AddAlias(aliases, "animals");
			}
			if (item.HasHorseComponent)
			{
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_a25ee73c98d1"));
				AddAlias(aliases, "horse");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_39185c90ef7a"));
				AddAlias(aliases, "horses");
				AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f57b69e8b31c"));
				AddAlias(aliases, "mount");
			}
		}
		catch
		{
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Horse:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_a25ee73c98d1"));
			AddAlias(aliases, "horse");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_39185c90ef7a"));
			AddAlias(aliases, "horses");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f57b69e8b31c"));
			AddAlias(aliases, "mount");
			break;
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_21dd82d786f4"));
			AddAlias(aliases, "one-handed weapons");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_391aa9b897c3"));
			AddAlias(aliases, "sword");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_f7bff9ed8550"));
			AddAlias(aliases, "blade");
			break;
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_1dd52a9c6a4f"));
			AddAlias(aliases, "two-handed weapons");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_accf680837c8"));
			AddAlias(aliases, "greatsword");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_b4cc81a2dab8"));
			AddAlias(aliases, "polearms");
			break;
		case ItemObject.ItemTypeEnum.Polearm:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_b4cc81a2dab8"));
			AddAlias(aliases, "polearms");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_6263e32ea5d9"));
			AddAlias(aliases, "lance");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_6a446fa79174"));
			AddAlias(aliases, "spear");
			break;
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_674650b36c70"));
			AddAlias(aliases, "arrows");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_a392322b6893"));
			AddAlias(aliases, "arrows");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_69186efbd61f"));
			AddAlias(aliases, "bolts");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_5a8114a4b332"));
			AddAlias(aliases, "ammunition");
			break;
		case ItemObject.ItemTypeEnum.Shield:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_345a74c4fc37"));
			AddAlias(aliases, "shield");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_589c31669663"));
			AddAlias(aliases, "shields");
			break;
		case ItemObject.ItemTypeEnum.Bow:
		case ItemObject.ItemTypeEnum.Crossbow:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d97da89d3d05"));
			AddAlias(aliases, "bow");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_eab85d88711e"));
			AddAlias(aliases, "bows");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_e86ef966639c"));
			AddAlias(aliases, "crossbow");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_6cd042cf5f15"));
			AddAlias(aliases, "ranged weapons");
			break;
		case ItemObject.ItemTypeEnum.Thrown:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9d93b20d2e46"));
			AddAlias(aliases, "throwing weapons");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_36b9cf058f18"));
			AddAlias(aliases, "javelin");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_e45ae22bb0b1"));
			AddAlias(aliases, "throwing knife");
			break;
		case ItemObject.ItemTypeEnum.HeadArmor:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_730da70298b5"));
			AddAlias(aliases, "helmet");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_820b0aaf4da8"));
			AddAlias(aliases, "helm");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_33e3580c0931"));
			AddAlias(aliases, "armor");
			break;
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_33e3580c0931"));
			AddAlias(aliases, "armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9172f60d7869"));
			AddAlias(aliases, "armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_c18d8f09cb26"));
			AddAlias(aliases, "armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_c888afcc3ef5"));
			AddAlias(aliases, "body armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9a5d41c11c6b"));
			AddAlias(aliases, "armor");
			break;
		case ItemObject.ItemTypeEnum.LegArmor:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d7cc552a69ee"));
			AddAlias(aliases, "leg armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_33e3580c0931"));
			AddAlias(aliases, "armor");
			break;
		case ItemObject.ItemTypeEnum.HandArmor:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_958e2745d2cf"));
			AddAlias(aliases, "hand armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_33e3580c0931"));
			AddAlias(aliases, "armor");
			break;
		case ItemObject.ItemTypeEnum.HorseHarness:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9726cd4b1434"));
			AddAlias(aliases, "horse harness");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_bb2a92e89c3d"));
			AddAlias(aliases, "horse armor");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_33e3580c0931"));
			AddAlias(aliases, "armor");
			break;
		case ItemObject.ItemTypeEnum.Goods:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_0883a49c383f"));
			AddAlias(aliases, "goods");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_58fdb0941f36"));
			AddAlias(aliases, "trade goods");
			break;
		case ItemObject.ItemTypeEnum.Animal:
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9fa24900d772"));
			AddAlias(aliases, "livestock");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_7ebb07e850a4"));
			AddAlias(aliases, "animals");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_a25ee73c98d1"));
			AddAlias(aliases, "horse");
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
		AddAlias(aliases, EconomyPromptTexts.ProjectSource(type));
		if (string.Equals(type, PromptRetrievalTexts.Raw("Raw_ee0c3b2a23bf"), StringComparison.Ordinal))
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d94e4628ead1"));
			AddAlias(aliases, "archer");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9844debaf067"));
			AddAlias(aliases, "archers");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_7ba00927dde9"));
			AddAlias(aliases, "ranged");
		}
		if (string.Equals(type, PromptRetrievalTexts.Raw("Raw_fc088c205262"), StringComparison.Ordinal))
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_9a6f752a85a1"));
			AddAlias(aliases, "cavalry");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_4ae5af8543e0"));
			AddAlias(aliases, "rider");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_0551caec03ab"));
			AddAlias(aliases, "knight");
		}
		if (string.Equals(type, PromptRetrievalTexts.Raw("Raw_80e617d1ecec"), StringComparison.Ordinal))
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_5f34654a09e3"));
			AddAlias(aliases, "horse archers");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_5cd71bcf1a75"));
			AddAlias(aliases, "mounted archer");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_d4b0a71d03b5"));
			AddAlias(aliases, "horse archer");
		}
		if (string.Equals(type, PromptRetrievalTexts.Raw("Raw_8c69262092a7"), StringComparison.Ordinal))
		{
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_78a516f1005d"));
			AddAlias(aliases, "infantry");
			AddAlias(aliases, PromptRetrievalTexts.Raw("Raw_6a85ccf586ba"));
			AddAlias(aliases, "melee");
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


}
