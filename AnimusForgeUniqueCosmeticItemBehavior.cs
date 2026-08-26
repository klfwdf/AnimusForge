using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Seeds the Xihai cosmetic helmet once and then permanently closes the spawn
/// gate for that save.  The XML item is non-merchandise, so native town refresh
/// cannot create additional copies after the one-time seed.
/// </summary>
public sealed class AnimusForgeUniqueCosmeticItemBehavior : CampaignBehaviorBase
{
	public const string UniqueItemId = "af_xihai_daimao_head";

	private bool _spawnResolved;

	private string _spawnSettlementId = string.Empty;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_af_daimao_singleton_spawn_resolved_v1", ref _spawnResolved);
		dataStore.SyncData("_af_daimao_singleton_spawn_settlement_v1", ref _spawnSettlementId);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		TrySeedOnce();
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

			Settlement target = (Settlement.All ?? Enumerable.Empty<Settlement>())
				.Where(settlement => settlement != null && settlement.IsActive && settlement.Town != null)
				.OrderBy(settlement => settlement.StringId ?? string.Empty, StringComparer.Ordinal)
				.FirstOrDefault();
			if (target?.ItemRoster == null)
			{
				Logger.Log("UniqueItem", "[WARN] singleton spawn deferred: no active town was available.");
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
