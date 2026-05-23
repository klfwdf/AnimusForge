using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;

public class ItemBarterBehavior : CampaignBehaviorBase
{
	private class SettlementDistanceCache
	{
		private struct SettlementDistancePair : IComparable<SettlementDistancePair>
		{
			private float _distance;

			public Settlement Settlement;

			public SettlementDistancePair(float distance, Settlement settlement)
			{
				_distance = distance;
				Settlement = settlement;
			}

			public int CompareTo(SettlementDistancePair other)
			{
				if (_distance == other._distance)
				{
					return 0;
				}
				if (_distance > other._distance)
				{
					return 1;
				}
				return -1;
			}
		}

		private Vec2 _latestHeroPosition;

		private List<SettlementDistancePair> _sortedSettlements;

		private List<Settlement> _closestSettlements;

		public SettlementDistanceCache()
		{
			_latestHeroPosition = new Vec2(-1f, -1f);
			_sortedSettlements = new List<SettlementDistancePair>(64);
			_closestSettlements = new List<Settlement>(3);
		}

		public List<Settlement> GetClosestSettlements(Vec2 position)
		{
			if (!position.NearlyEquals(_latestHeroPosition))
			{
				_latestHeroPosition = position;
				MBReadOnlyList<Town> allTowns = Campaign.Current.AllTowns;
				int count = allTowns.Count;
				for (int i = 0; i < count; i++)
				{
					Settlement settlement = allTowns[i].Settlement;
					_sortedSettlements.Add(new SettlementDistancePair(position.DistanceSquared(settlement.Position.ToVec2()), settlement));
				}
				_sortedSettlements.Sort();
				_closestSettlements.Clear();
				_closestSettlements.Add(_sortedSettlements[0].Settlement);
				_closestSettlements.Add(_sortedSettlements[1].Settlement);
				_closestSettlements.Add(_sortedSettlements[2].Settlement);
				_sortedSettlements.Clear();
			}
			return _closestSettlements;
		}
	}

	private const int ItemValueThreshold = 100;

	private SettlementDistanceCache _distanceCache = new SettlementDistanceCache();

	public override void RegisterEvents()
	{
		CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, CheckForBarters);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void CheckForBarters(BarterData args)
	{
		CampaignVec2 campaignVec = ((args.OffererHero != null) ? args.OffererHero.GetCampaignPosition() : ((args.OffererParty == null) ? args.OtherHero.GetCampaignPosition() : args.OffererParty.MobileParty.Position));
		if (!campaignVec.IsValid())
		{
			return;
		}
		List<Settlement> closestSettlements = _distanceCache.GetClosestSettlements(campaignVec.ToVec2());
		if (args.OffererParty == null || args.OtherParty == null)
		{
			return;
		}
		for (int i = 0; i < args.OffererParty.ItemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = args.OffererParty.ItemRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Amount > 0 && elementCopyAtIndex.EquipmentElement.GetBaseValue() > 100)
			{
				int averageValueOfItemInNearbySettlements = CalculateAverageItemValueInNearbySettlements(elementCopyAtIndex.EquipmentElement, args.OffererParty, closestSettlements);
				Barterable barterable = new ItemBarterable(args.OffererHero, args.OtherHero, args.OffererParty, args.OtherParty, elementCopyAtIndex, averageValueOfItemInNearbySettlements);
				args.AddBarterable<ItemBarterGroup>(barterable);
			}
		}
		for (int j = 0; j < args.OtherParty.ItemRoster.Count; j++)
		{
			ItemRosterElement elementCopyAtIndex2 = args.OtherParty.ItemRoster.GetElementCopyAtIndex(j);
			if (elementCopyAtIndex2.Amount > 0 && elementCopyAtIndex2.EquipmentElement.GetBaseValue() > 100)
			{
				int averageValueOfItemInNearbySettlements2 = CalculateAverageItemValueInNearbySettlements(elementCopyAtIndex2.EquipmentElement, args.OtherParty, closestSettlements);
				Barterable barterable2 = new ItemBarterable(args.OtherHero, args.OffererHero, args.OtherParty, args.OffererParty, elementCopyAtIndex2, averageValueOfItemInNearbySettlements2);
				args.AddBarterable<ItemBarterGroup>(barterable2);
			}
		}
	}

	private int CalculateAverageItemValueInNearbySettlements(EquipmentElement itemRosterElement, PartyBase involvedParty, List<Settlement> nearbySettlements)
	{
		int num = 0;
		if (!nearbySettlements.IsEmpty())
		{
			foreach (Settlement nearbySettlement in nearbySettlements)
			{
				num += nearbySettlement.Town.GetItemPrice(itemRosterElement, involvedParty.MobileParty, isSelling: true);
			}
			num /= nearbySettlements.Count;
		}
		return num;
	}
}
