using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E4 RID: 996
	public class DesertersCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E0C RID: 3596
		// (get) Token: 0x06003D3C RID: 15676 RVA: 0x00109E3E File Offset: 0x0010803E
		public static int MergePartiesMaxSize
		{
			get
			{
				return 120;
			}
		}

		// Token: 0x17000E0D RID: 3597
		// (get) Token: 0x06003D3D RID: 15677 RVA: 0x00109E42 File Offset: 0x00108042
		private float DesertersSpawnRadiusAroundVillages
		{
			get
			{
				return 0.2f * Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;
			}
		}

		// Token: 0x17000E0E RID: 3598
		// (get) Token: 0x06003D3E RID: 15678 RVA: 0x00109E5B File Offset: 0x0010805B
		private Clan DeserterClan
		{
			get
			{
				if (this._deserterClan == null)
				{
					this._deserterClan = Clan.FindFirst((Clan x) => x.StringId == "deserters");
				}
				return this._deserterClan;
			}
		}

		// Token: 0x06003D3F RID: 15679 RVA: 0x00109E95 File Offset: 0x00108095
		public override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.MapEventEnded));
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickParty));
		}

		// Token: 0x06003D40 RID: 15680 RVA: 0x00109EC8 File Offset: 0x001080C8
		private void HourlyTickParty(MobileParty party)
		{
			if (this.IsDeserterParty(party) && this.CanPartyMerge(party) && party.MemberRoster.TotalRegulars < DesertersCampaignBehavior.MergePartiesMaxSize)
			{
				LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(party.Position.ToVec2(), this.GetMergeDistance(party));
				for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData))
				{
					if (this.IsDeserterParty(mobileParty) && mobileParty != party && this.CanPartyMerge(mobileParty) && mobileParty.MemberRoster.TotalRegulars + party.MemberRoster.TotalRegulars <= DesertersCampaignBehavior.MergePartiesMaxSize && MBRandom.RandomFloat < 0.05f)
					{
						this.MergeParties(party, mobileParty);
						return;
					}
				}
			}
		}

		// Token: 0x06003D41 RID: 15681 RVA: 0x00109F78 File Offset: 0x00108178
		private bool CanPartyMerge(MobileParty mobileParty)
		{
			return mobileParty.IsActive && mobileParty.MapEvent == null && !mobileParty.IsCurrentlyUsedByAQuest && !mobileParty.IsCurrentlyEngagingParty && !mobileParty.IsFleeing();
		}

		// Token: 0x06003D42 RID: 15682 RVA: 0x00109FA8 File Offset: 0x001081A8
		private void MergeParties(MobileParty party, MobileParty nearbyParty)
		{
			Debug.Print(string.Format("Deserter parties {0} of {1} and {2} of {3} merged.", new object[]
			{
				party.StringId,
				party.MemberRoster.TotalManCount,
				nearbyParty.StringId,
				nearbyParty.MemberRoster.TotalManCount
			}), 0, Debug.DebugColor.White, 17592186044416UL);
			party.MemberRoster.Add(nearbyParty.MemberRoster);
			foreach (TroopRosterElement troopRosterElement in nearbyParty.PrisonRoster.GetTroopRoster())
			{
				if (troopRosterElement.Character.HeroObject != null)
				{
					TransferPrisonerAction.Apply(troopRosterElement.Character, nearbyParty.Party, party.Party);
				}
			}
			if (party.PrisonRoster.Count > 0)
			{
				party.PrisonRoster.Add(nearbyParty.PrisonRoster);
			}
			party.PartyTradeGold += nearbyParty.PartyTradeGold;
			party.ItemRoster.Add(nearbyParty.ItemRoster);
			DestroyPartyAction.Apply(null, nearbyParty);
			PartyBaseHelper.SortRoster(party);
		}

		// Token: 0x06003D43 RID: 15683 RVA: 0x0010A0D8 File Offset: 0x001082D8
		private void MapEventEnded(MapEvent mapEvent)
		{
			if (!mapEvent.IsNavalMapEvent && (mapEvent.IsFieldBattle || mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut) && mapEvent.HasWinner && this.DeserterClan != null && this.DeserterClan.WarPartyComponents.Count < Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(this.DeserterClan))
			{
				MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
				TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
				foreach (MapEventParty mapEventParty in mapEventSide.Parties)
				{
					if (this.CanPartyGenerateDeserters(mapEventParty))
					{
						troopRoster.Add(mapEventParty.RoutedInBattle);
						troopRoster.Add(mapEventParty.DiedInBattle);
					}
				}
				if (MBRandom.RandomFloat < 0.9f)
				{
					troopRoster.RemoveIf((TroopRosterElement x) => x.Character.IsHero);
					if (troopRoster.TotalManCount >= 15)
					{
						this.TrySpawnDeserters(mapEvent, troopRoster);
					}
				}
			}
		}

		// Token: 0x06003D44 RID: 15684 RVA: 0x0010A210 File Offset: 0x00108410
		private bool CanPartyGenerateDeserters(MapEventParty mapEventParty)
		{
			return mapEventParty.Party.IsMobile && mapEventParty.Party.MobileParty.IsLordParty && mapEventParty.Party.MobileParty.ActualClan != null && !mapEventParty.Party.MobileParty.ActualClan.IsMinorFaction;
		}

		// Token: 0x06003D45 RID: 15685 RVA: 0x0010A268 File Offset: 0x00108468
		private void TrySpawnDeserters(MapEvent mapEvent, TroopRoster routedTroops)
		{
			int maxDeserterPartyCountForMapEvent = this.GetMaxDeserterPartyCountForMapEvent(mapEvent);
			List<TroopRoster> rostersSuitableForDeserters = this.GetRostersSuitableForDeserters(routedTroops, maxDeserterPartyCountForMapEvent);
			List<Settlement> list = this.SelectRandomSettlementsForDeserters(mapEvent, rostersSuitableForDeserters.Count);
			for (int i = 0; i < rostersSuitableForDeserters.Count; i++)
			{
				this.SpawnDesertersParty(mapEvent, rostersSuitableForDeserters[i], list[i]);
			}
		}

		// Token: 0x06003D46 RID: 15686 RVA: 0x0010A2BC File Offset: 0x001084BC
		private int GetMaxDeserterPartyCountForMapEvent(MapEvent mapEvent)
		{
			bool flag = mapEvent.AttackerSide.Parties.Any((MapEventParty x) => this.CanPartyGenerateDeserters(x) && x.Party.MobileParty.Army != null && (x.Party.MobileParty.AttachedTo != null || x.Party.MobileParty.Army.LeaderParty == x.Party.MobileParty));
			bool flag2 = mapEvent.DefenderSide.Parties.Any((MapEventParty x) => this.CanPartyGenerateDeserters(x) && x.Party.MobileParty.Army != null && (x.Party.MobileParty.AttachedTo != null || x.Party.MobileParty.Army.LeaderParty == x.Party.MobileParty));
			if (flag && flag2)
			{
				return 5;
			}
			return 3;
		}

		// Token: 0x06003D47 RID: 15687 RVA: 0x0010A30C File Offset: 0x0010850C
		private List<TroopRoster> GetRostersSuitableForDeserters(TroopRoster routedTroops, int maxPartyCount)
		{
			int totalManCount = routedTroops.TotalManCount;
			int maxSupportedNumberOfLootersForClan = Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(this.DeserterClan);
			int val = Math.Min(maxPartyCount, maxSupportedNumberOfLootersForClan - this.DeserterClan.WarPartyComponents.Count);
			int val2 = totalManCount / 15;
			int num = Math.Min(val, val2);
			List<TroopRoster> list = new List<TroopRoster>();
			for (int i = 0; i < num; i++)
			{
				list.Add(routedTroops.RemoveNumberOfNonHeroTroopsRandomly(Math.Min(routedTroops.TotalManCount / (num - i), 40)));
			}
			return list;
		}

		// Token: 0x06003D48 RID: 15688 RVA: 0x0010A397 File Offset: 0x00108597
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003D49 RID: 15689 RVA: 0x0010A39C File Offset: 0x0010859C
		private void SpawnDesertersParty(MapEvent mapEvent, TroopRoster troops, Settlement settlement)
		{
			CampaignVec2 deserterSpawnPosition = this.GetDeserterSpawnPosition(settlement);
			MobileParty mobileParty = BanditPartyComponent.CreateLooterParty(this.DeserterClan.StringId + "_1", this.DeserterClan, settlement, false, null, deserterSpawnPosition);
			mobileParty.MemberRoster.Add(troops);
			this.InitializeDeserterParty(mobileParty);
			mobileParty.SetMovePatrolAroundPoint(mobileParty.Position, MobileParty.NavigationType.Default);
			PartyBaseHelper.SortRoster(mobileParty);
			Debug.Print(mobileParty.StringId + " deserter party was created around: " + settlement.Name.ToString(), 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06003D4A RID: 15690 RVA: 0x0010A428 File Offset: 0x00108628
		private List<Settlement> SelectRandomSettlementsForDeserters(MapEvent mapEvent, int count)
		{
			CampaignVec2 position = mapEvent.Position;
			List<Settlement> list = DesertersCampaignBehavior.FindSettlementsAroundPoint(position, (Settlement x) => x.IsVillage, MobileParty.NavigationType.Default, this.GetMaxVillageDistance());
			if (list.Count > count)
			{
				list.Shuffle<Settlement>();
				return list.Take(count).ToList<Settlement>();
			}
			if (list.Count == 0)
			{
				List<Settlement> list2 = list;
				position = mapEvent.Position;
				list2.Add(SettlementHelper.FindNearestSettlementToPoint(position, (Settlement x) => x.IsVillage));
			}
			int count2 = list.Count;
			for (int i = 0; i < count - count2; i++)
			{
				list.Add(list[MBRandom.RandomInt(0, count2 - 1)]);
			}
			return list;
		}

		// Token: 0x06003D4B RID: 15691 RVA: 0x0010A4EC File Offset: 0x001086EC
		private static List<Settlement> FindSettlementsAroundPoint(in CampaignVec2 point, Func<Settlement, bool> condition, MobileParty.NavigationType navCapabilities, float maxDistance)
		{
			List<Settlement> list = new List<Settlement>();
			foreach (Settlement settlement in Settlement.All)
			{
				if ((condition == null || condition(settlement)) && settlement.Position.Distance(point) < maxDistance)
				{
					list.Add(settlement);
				}
			}
			return list;
		}

		// Token: 0x06003D4C RID: 15692 RVA: 0x0010A568 File Offset: 0x00108768
		private float GetMaxVillageDistance()
		{
			return Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay / 2f;
		}

		// Token: 0x06003D4D RID: 15693 RVA: 0x0010A584 File Offset: 0x00108784
		private CampaignVec2 GetDeserterSpawnPosition(Settlement settlement)
		{
			CampaignVec2 campaignVec = NavigationHelper.FindPointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, this.DesertersSpawnRadiusAroundVillages, 0f, true, false);
			float num = MobileParty.MainParty.SeeingRange * MobileParty.MainParty.SeeingRange;
			if (campaignVec.DistanceSquared(MobileParty.MainParty.Position) < num)
			{
				for (int i = 0; i < 15; i++)
				{
					CampaignVec2 campaignVec2 = NavigationHelper.FindReachablePointAroundPosition(campaignVec, MobileParty.NavigationType.Default, this.DesertersSpawnRadiusAroundVillages, 0f, false);
					if (NavigationHelper.IsPositionValidForNavigationType(campaignVec2, MobileParty.NavigationType.Default))
					{
						float num3;
						float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(MobileParty.MainParty, campaignVec2, MobileParty.NavigationType.Default, out num3);
						if (num2 * num2 > num)
						{
							campaignVec = campaignVec2;
							break;
						}
					}
				}
			}
			return campaignVec;
		}

		// Token: 0x06003D4E RID: 15694 RVA: 0x0010A617 File Offset: 0x00108817
		private void InitializeDeserterParty(MobileParty banditParty)
		{
			banditParty.Party.SetVisualAsDirty();
			banditParty.ActualClan = this.DeserterClan;
			banditParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
			DesertersCampaignBehavior.CreatePartyTrade(banditParty);
			this.GiveFoodToBanditParty(banditParty);
		}

		// Token: 0x06003D4F RID: 15695 RVA: 0x0010A654 File Offset: 0x00108854
		private static void CreatePartyTrade(MobileParty banditParty)
		{
			int initialGold = (int)(10f * (float)banditParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
			banditParty.InitializePartyTrade(initialGold);
		}

		// Token: 0x06003D50 RID: 15696 RVA: 0x0010A694 File Offset: 0x00108894
		private void GiveFoodToBanditParty(MobileParty banditParty)
		{
			foreach (ItemObject itemObject in Items.All)
			{
				if (itemObject.IsFood)
				{
					int num = MBRandom.RoundRandomized((float)banditParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * 8f * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
					if (num > 0)
					{
						banditParty.ItemRoster.AddToCounts(itemObject, num);
					}
				}
			}
		}

		// Token: 0x06003D51 RID: 15697 RVA: 0x0010A738 File Offset: 0x00108938
		private float GetMergeDistance(MobileParty mobileParty)
		{
			return mobileParty._lastCalculatedSpeed * 2f;
		}

		// Token: 0x06003D52 RID: 15698 RVA: 0x0010A746 File Offset: 0x00108946
		private bool IsDeserterParty(MobileParty mobileParty)
		{
			return mobileParty.ActualClan != null && mobileParty.ActualClan == this.DeserterClan;
		}

		// Token: 0x04001283 RID: 4739
		public const int MinimumDeserterPartyCount = 15;

		// Token: 0x04001284 RID: 4740
		public const int MaximumDeserterPartyCount = 40;

		// Token: 0x04001285 RID: 4741
		private const int MaxDeserterPartyCountAfterBattle = 3;

		// Token: 0x04001286 RID: 4742
		private const int MaxDeserterPartyCountAfterArmyBattle = 5;

		// Token: 0x04001287 RID: 4743
		private Clan _deserterClan;
	}
}
