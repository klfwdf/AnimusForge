using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Seeds the Xihai cosmetic helmet once and then permanently closes the spawn
/// gate for that save.  The XML item is non-merchandise, so native town refresh
/// cannot create additional copies after the one-time seed.
/// </summary>
public sealed class AnimusForgeUniqueCosmeticItemBehavior : CampaignBehaviorBase
{
	public const string UniqueItemId = "af_xihai_daimao_head";

	private static readonly HashSet<string> LegacyXihaiItemIds =
		new HashSet<string>(StringComparer.Ordinal)
		{
			"af_xihai_naci_hat",
			"af_xihai_naci_clothes",
			"af_xihai_naci_shoes"
		};

	private bool _spawnResolved;

	private string _spawnSettlementId = string.Empty;

	private bool _legacyCleanupCompleted;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_af_daimao_singleton_spawn_resolved_v1", ref _spawnResolved);
		dataStore.SyncData("_af_daimao_singleton_spawn_settlement_v1", ref _spawnSettlementId);
		dataStore.SyncData("_af_xihai_legacy_equipment_cleanup_v1", ref _legacyCleanupCompleted);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		TryRemoveLegacyXihaiEquipment();
		TrySeedOnce();
	}

	private void TryRemoveLegacyXihaiEquipment()
	{
		if (_legacyCleanupCompleted)
		{
			return;
		}

		try
		{
			int removedRosterItems = 0;
			int removedRosterStacks = 0;
			int clearedHeroSlots = 0;

			foreach (MobileParty party in (MobileParty.All ?? Enumerable.Empty<MobileParty>()))
			{
				RemoveLegacyItemsFromRoster(
					party?.ItemRoster,
					ref removedRosterItems,
					ref removedRosterStacks);
			}

			foreach (Settlement settlement in (Settlement.All ?? Enumerable.Empty<Settlement>()))
			{
				RemoveLegacyItemsFromRoster(
					settlement?.ItemRoster,
					ref removedRosterItems,
					ref removedRosterStacks);
			}

			foreach (Hero hero in (Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>()))
			{
				if (hero == null)
				{
					continue;
				}
				clearedHeroSlots += ClearLegacyEquipmentSlots(hero.BattleEquipment);
				clearedHeroSlots += ClearLegacyEquipmentSlots(hero.CivilianEquipment);
				clearedHeroSlots += ClearLegacyEquipmentSlots(hero.StealthEquipment);
			}

			_legacyCleanupCompleted = true;
			Logger.Log(
				"UniqueItem",
				"[INFO] legacy Xihai equipment cleanup completed. " +
				"removedItems=" + removedRosterItems +
				" removedStacks=" + removedRosterStacks +
				" clearedHeroSlots=" + clearedHeroSlots);
		}
		catch (Exception ex)
		{
			Logger.Log(
				"UniqueItem",
				"[WARN] legacy Xihai equipment cleanup failed; will retry next session: " +
				ex.Message);
		}
	}

	private static void RemoveLegacyItemsFromRoster(
		ItemRoster roster,
		ref int removedItems,
		ref int removedStacks)
	{
		if (roster == null)
		{
			return;
		}

		for (int index = roster.Count - 1; index >= 0; index--)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(index);
			if (!IsLegacyXihaiItem(element.EquipmentElement.Item) || element.Amount <= 0)
			{
				continue;
			}

			roster.AddToCounts(element.EquipmentElement, -element.Amount);
			removedItems += element.Amount;
			removedStacks++;
		}
	}

	private static int ClearLegacyEquipmentSlots(Equipment equipment)
	{
		if (equipment == null)
		{
			return 0;
		}

		int cleared = 0;
		for (int index = 0; index < Equipment.EquipmentSlotLength; index++)
		{
			EquipmentElement element = equipment[index];
			if (!IsLegacyXihaiItem(element.Item) &&
				!IsLegacyXihaiItem(element.CosmeticItem))
			{
				continue;
			}

			equipment[index] = EquipmentElement.Invalid;
			cleared++;
		}
		return cleared;
	}

	private static bool IsLegacyXihaiItem(ItemObject item)
	{
		return item != null && LegacyXihaiItemIds.Contains(item.StringId);
	}

	private void TrySeedOnce()
	{
		if (_spawnResolved)
		{
			return;
		}

		try
		{
			ItemObject uniqueItem = Game.Current?.ObjectManager?.GetObject<ItemObject>(UniqueItemId);
			if (uniqueItem == null)
			{
				return;
			}

			// One migration-only lookup for saves created while the item was still
			// merchandise.  Existing copies are preserved; no cleanup is performed.
			if (FindExistingCopy(uniqueItem, out string existingOwner))
			{
				_spawnResolved = true;
				_spawnSettlementId = existingOwner ?? string.Empty;
				Logger.Log(
					"UniqueItem",
					"[INFO] singleton spawn gate adopted existing item=" + UniqueItemId +
					" owner=" + _spawnSettlementId);
				return;
			}

			List<Settlement> candidates = (Settlement.All ?? Enumerable.Empty<Settlement>())
				.Where(settlement => settlement != null && settlement.IsActive && settlement.IsTown)
				.OrderBy(settlement => settlement.StringId ?? string.Empty, StringComparer.Ordinal)
				.ToList();
			if (candidates.Count == 0)
			{
				Logger.Log("UniqueItem", "[WARN] singleton spawn deferred: no active town was available.");
				return;
			}

			Settlement target = candidates[MBRandom.RandomInt(candidates.Count)];
			if (target.ItemRoster == null)
			{
				Logger.Log(
					"UniqueItem",
					"[WARN] singleton spawn deferred: selected town had no item roster. settlement=" +
					(target.StringId ?? string.Empty));
				return;
			}

			target.ItemRoster.AddToCounts(uniqueItem, 1);
			_spawnResolved = true;
			_spawnSettlementId = target.StringId ?? string.Empty;
			Logger.Log(
				"UniqueItem",
				"[INFO] singleton item seeded item=" + UniqueItemId +
				" settlement=" + _spawnSettlementId);
		}
		catch (Exception ex)
		{
			Logger.Log("UniqueItem", "[WARN] singleton item seed failed: " + ex.Message);
		}
	}

	private static bool FindExistingCopy(ItemObject uniqueItem, out string owner)
	{
		owner = string.Empty;
		foreach (MobileParty party in (MobileParty.All ?? Enumerable.Empty<MobileParty>())
			.OrderBy(party => party?.StringId ?? string.Empty, StringComparer.Ordinal))
		{
			if (party?.ItemRoster == null || party.ItemRoster.GetItemNumber(uniqueItem) <= 0)
			{
				continue;
			}
			owner = "party:" + (party.StringId ?? string.Empty);
			return true;
		}

		foreach (Settlement settlement in (Settlement.All ?? Enumerable.Empty<Settlement>())
			.OrderBy(settlement => settlement?.StringId ?? string.Empty, StringComparer.Ordinal))
		{
			if (settlement?.ItemRoster == null || settlement.ItemRoster.GetItemNumber(uniqueItem) <= 0)
			{
				continue;
			}
			owner = "settlement:" + (settlement.StringId ?? string.Empty);
			return true;
		}

		return false;
	}
}
