using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors
{
	// Token: 0x02000467 RID: 1127
	public class ItemBarterBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600479E RID: 18334 RVA: 0x00166C41 File Offset: 0x00164E41
		public override void RegisterEvents()
		{
			CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, new Action<BarterData>(this.CheckForBarters));
		}

		// Token: 0x0600479F RID: 18335 RVA: 0x00166C5A File Offset: 0x00164E5A
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047A0 RID: 18336 RVA: 0x00166C5C File Offset: 0x00164E5C
		public void CheckForBarters(BarterData args)
		{
			CampaignVec2 campaignVec;
			if (args.OffererHero != null)
			{
				campaignVec = args.OffererHero.GetCampaignPosition();
			}
			else if (args.OffererParty != null)
			{
				campaignVec = args.OffererParty.MobileParty.Position;
			}
			else
			{
				campaignVec = args.OtherHero.GetCampaignPosition();
			}
			if (campaignVec.IsValid())
			{
				List<Settlement> closestSettlements = this._distanceCache.GetClosestSettlements(campaignVec.ToVec2());
				if (args.OffererParty != null && args.OtherParty != null)
				{
					for (int i = 0; i < args.OffererParty.ItemRoster.Count; i++)
					{
						ItemRosterElement elementCopyAtIndex = args.OffererParty.ItemRoster.GetElementCopyAtIndex(i);
						if (elementCopyAtIndex.Amount > 0 && elementCopyAtIndex.EquipmentElement.GetBaseValue() > 100)
						{
							int averageValueOfItemInNearbySettlements = this.CalculateAverageItemValueInNearbySettlements(elementCopyAtIndex.EquipmentElement, args.OffererParty, closestSettlements);
							Barterable barterable = new ItemBarterable(args.OffererHero, args.OtherHero, args.OffererParty, args.OtherParty, elementCopyAtIndex, averageValueOfItemInNearbySettlements);
							args.AddBarterable<ItemBarterGroup>(barterable, false);
						}
					}
					for (int j = 0; j < args.OtherParty.ItemRoster.Count; j++)
					{
						ItemRosterElement elementCopyAtIndex2 = args.OtherParty.ItemRoster.GetElementCopyAtIndex(j);
						if (elementCopyAtIndex2.Amount > 0 && elementCopyAtIndex2.EquipmentElement.GetBaseValue() > 100)
						{
							int averageValueOfItemInNearbySettlements2 = this.CalculateAverageItemValueInNearbySettlements(elementCopyAtIndex2.EquipmentElement, args.OtherParty, closestSettlements);
							Barterable barterable2 = new ItemBarterable(args.OtherHero, args.OffererHero, args.OtherParty, args.OffererParty, elementCopyAtIndex2, averageValueOfItemInNearbySettlements2);
							args.AddBarterable<ItemBarterGroup>(barterable2, false);
						}
					}
				}
			}
		}

		// Token: 0x060047A1 RID: 18337 RVA: 0x00166E00 File Offset: 0x00165000
		private int CalculateAverageItemValueInNearbySettlements(EquipmentElement itemRosterElement, PartyBase involvedParty, List<Settlement> nearbySettlements)
		{
			int num = 0;
			if (!nearbySettlements.IsEmpty<Settlement>())
			{
				foreach (Settlement settlement in nearbySettlements)
				{
					num += settlement.Town.GetItemPrice(itemRosterElement, involvedParty.MobileParty, true);
				}
				num /= nearbySettlements.Count;
			}
			return num;
		}

		// Token: 0x040013C9 RID: 5065
		private const int ItemValueThreshold = 100;

		// Token: 0x040013CA RID: 5066
		private ItemBarterBehavior.SettlementDistanceCache _distanceCache = new ItemBarterBehavior.SettlementDistanceCache();

		// Token: 0x02000870 RID: 2160
		private class SettlementDistanceCache
		{
			// Token: 0x0600678D RID: 26509 RVA: 0x001C3BED File Offset: 0x001C1DED
			public SettlementDistanceCache()
			{
				this._latestHeroPosition = new Vec2(-1f, -1f);
				this._sortedSettlements = new List<ItemBarterBehavior.SettlementDistanceCache.SettlementDistancePair>(64);
				this._closestSettlements = new List<Settlement>(3);
			}

			// Token: 0x0600678E RID: 26510 RVA: 0x001C3C24 File Offset: 0x001C1E24
			public List<Settlement> GetClosestSettlements(Vec2 position)
			{
				if (!position.NearlyEquals(this._latestHeroPosition, 1E-05f))
				{
					this._latestHeroPosition = position;
					MBReadOnlyList<Town> allTowns = Campaign.Current.AllTowns;
					int count = allTowns.Count;
					for (int i = 0; i < count; i++)
					{
						Settlement settlement = allTowns[i].Settlement;
						this._sortedSettlements.Add(new ItemBarterBehavior.SettlementDistanceCache.SettlementDistancePair(position.DistanceSquared(settlement.Position.ToVec2()), settlement));
					}
					this._sortedSettlements.Sort();
					this._closestSettlements.Clear();
					this._closestSettlements.Add(this._sortedSettlements[0].Settlement);
					this._closestSettlements.Add(this._sortedSettlements[1].Settlement);
					this._closestSettlements.Add(this._sortedSettlements[2].Settlement);
					this._sortedSettlements.Clear();
				}
				return this._closestSettlements;
			}

			// Token: 0x040023D1 RID: 9169
			private Vec2 _latestHeroPosition;

			// Token: 0x040023D2 RID: 9170
			private List<ItemBarterBehavior.SettlementDistanceCache.SettlementDistancePair> _sortedSettlements;

			// Token: 0x040023D3 RID: 9171
			private List<Settlement> _closestSettlements;

			// Token: 0x020008F5 RID: 2293
			private struct SettlementDistancePair : IComparable<ItemBarterBehavior.SettlementDistanceCache.SettlementDistancePair>
			{
				// Token: 0x06006914 RID: 26900 RVA: 0x001C5B9F File Offset: 0x001C3D9F
				public SettlementDistancePair(float distance, Settlement settlement)
				{
					this._distance = distance;
					this.Settlement = settlement;
				}

				// Token: 0x06006915 RID: 26901 RVA: 0x001C5BAF File Offset: 0x001C3DAF
				public int CompareTo(ItemBarterBehavior.SettlementDistanceCache.SettlementDistancePair other)
				{
					if (this._distance == other._distance)
					{
						return 0;
					}
					if (this._distance > other._distance)
					{
						return 1;
					}
					return -1;
				}

				// Token: 0x0400254F RID: 9551
				private float _distance;

				// Token: 0x04002550 RID: 9552
				public Settlement Settlement;
			}
		}
	}
}
