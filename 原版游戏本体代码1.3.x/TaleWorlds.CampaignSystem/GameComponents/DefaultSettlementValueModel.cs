using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000151 RID: 337
	public class DefaultSettlementValueModel : SettlementValueModel
	{
		// Token: 0x06001A2D RID: 6701 RVA: 0x00083760 File Offset: 0x00081960
		private static float GetSettlementScoreForBeingHomeSettlementOfClan(Settlement settlement, Clan clan, float maxDistanceOfSettlementsToHomeSettlement)
		{
			float num = 0f;
			if (clan.IsRebelClan || clan.IsBanditFaction || clan.MapFaction.Settlements.Count == 0)
			{
				if (settlement == clan.InitialHomeSettlement)
				{
					num = float.MaxValue;
				}
				else
				{
					num = float.MinValue;
				}
			}
			else
			{
				if (settlement.SettlementComponent is Hideout || settlement.SettlementComponent is RetirementSettlementComponent)
				{
					return float.MinValue;
				}
				if (settlement.MapFaction.IsAtWarWith(clan.MapFaction))
				{
					num -= 10240f;
				}
				if (settlement.OwnerClan == clan)
				{
					num += 5120f;
				}
				if (settlement.MapFaction == clan.MapFaction)
				{
					num += 2560f;
				}
				if (settlement.IsVillage)
				{
					num += 320f;
				}
				else if (settlement.IsCastle)
				{
					num += 640f;
				}
				else if (settlement.IsTown)
				{
					num += 1280f;
				}
				if (settlement == clan.HomeSettlement)
				{
					num += 4.5f;
				}
				if (settlement == clan.InitialHomeSettlement)
				{
					num += 3.5f;
				}
				if (settlement.Culture == clan.Culture)
				{
					num += 17f;
				}
				Clan ownerClan = settlement.OwnerClan;
				if (((ownerClan != null) ? ownerClan.Culture : null) == clan.Culture)
				{
					num += 12f;
				}
				Settlement factionMidSettlement = clan.MapFaction.FactionMidSettlement;
				if (clan.MapFaction.Settlements.Count > 1 && settlement != factionMidSettlement)
				{
					float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, false, false, MobileParty.NavigationType.All);
					if (settlement.HasPort)
					{
						float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, true, false, MobileParty.NavigationType.All);
						if (distance < num2)
						{
							num2 = distance;
						}
					}
					if (factionMidSettlement.HasPort)
					{
						float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, false, true, MobileParty.NavigationType.All);
						if (distance2 < num2)
						{
							num2 = distance2;
						}
						if (settlement.HasPort)
						{
							distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, true, true, MobileParty.NavigationType.All);
							if (distance2 < num2)
							{
								num2 = distance2;
							}
						}
					}
					float num3 = 20f - MBMath.Map(num2, 0f, maxDistanceOfSettlementsToHomeSettlement, 0f, 20f);
					num += num3;
				}
				else
				{
					num += 20f;
				}
				int num4 = DefaultSettlementValueModel.CalculateTotalProsperity(settlement);
				float num5 = MBMath.Map(MathF.Sqrt(2500f + (float)num4) / 100f, 0.5f, 1f, 0f, 5f);
				num += num5;
				float num6 = MBMath.Map(SettlementHelper.GetNeighborScoreForConsideringClan(settlement, clan), -2f, 1f, -10f, 10f);
				num += num6;
				int num7 = 0;
				for (;;)
				{
					int num8 = num7;
					Kingdom kingdom = clan.Kingdom;
					int? num9 = ((kingdom != null) ? new int?(kingdom.Clans.Count) : null);
					if (!((num8 < num9.GetValueOrDefault()) & (num9 != null)))
					{
						break;
					}
					Clan clan2 = clan.Kingdom.Clans[num7];
					if (clan2 != clan && settlement == clan2.HomeSettlement)
					{
						num -= 10f;
					}
					num7++;
				}
			}
			return num;
		}

		// Token: 0x06001A2E RID: 6702 RVA: 0x00083A70 File Offset: 0x00081C70
		public override Settlement FindMostSuitableHomeSettlement(Clan clan)
		{
			Settlement result = null;
			if (Settlement.All != null && Settlement.All.Count != 0 && !clan.IsRebelClan && !clan.IsBanditFaction && clan.MapFaction.Settlements.Count != 0)
			{
				float minValue = float.MinValue;
				float maxDistance = DefaultSettlementValueModel.FindFarthestDistanceBetweenSettlementsInClan(clan);
				DefaultSettlementValueModel.TryToFindHomeSettlementForClan(clan, clan.Fiefs.SelectQ((Town x) => x.Settlement), maxDistance, out result, ref minValue);
				if (minValue < 5120f && clan.Kingdom != null)
				{
					DefaultSettlementValueModel.TryToFindHomeSettlementForClan(clan, clan.Kingdom.Fiefs.SelectQ((Town x) => x.Settlement), maxDistance, out result, ref minValue);
				}
				if (minValue < 2560f)
				{
					DefaultSettlementValueModel.TryToFindHomeSettlementForClan(clan, Settlement.All, maxDistance, out result, ref minValue);
				}
				return result;
			}
			if (clan == Clan.PlayerClan && clan.InitialHomeSettlement == null)
			{
				return Settlement.All[0];
			}
			return clan.InitialHomeSettlement;
		}

		// Token: 0x06001A2F RID: 6703 RVA: 0x00083B7C File Offset: 0x00081D7C
		private static void TryToFindHomeSettlementForClan(Clan clanToConsider, IEnumerable<Settlement> settlementsToConsider, float maxDistance, out Settlement homeSettlement, ref float maxScore)
		{
			homeSettlement = null;
			foreach (Settlement settlement in settlementsToConsider)
			{
				if (settlement.IsFortification || settlement.IsVillage || settlement.IsHideout)
				{
					float settlementScoreForBeingHomeSettlementOfClan = DefaultSettlementValueModel.GetSettlementScoreForBeingHomeSettlementOfClan(settlement, clanToConsider, maxDistance);
					if (settlementScoreForBeingHomeSettlementOfClan > maxScore)
					{
						homeSettlement = settlement;
						maxScore = settlementScoreForBeingHomeSettlementOfClan;
					}
				}
			}
		}

		// Token: 0x06001A30 RID: 6704 RVA: 0x00083BF0 File Offset: 0x00081DF0
		private static float FindFarthestDistanceBetweenSettlementsInClan(Clan clan)
		{
			float num = float.MinValue;
			foreach (Settlement settlement in clan.MapFaction.Settlements)
			{
				if (settlement != clan.MapFaction.FactionMidSettlement)
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, false, false, MobileParty.NavigationType.All);
					if (distance > num)
					{
						num = distance;
					}
					if (settlement.HasPort)
					{
						distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, false, true, MobileParty.NavigationType.All);
						if (distance > num)
						{
							num = distance;
						}
					}
					if (clan.MapFaction.FactionMidSettlement.HasPort)
					{
						distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, true, false, MobileParty.NavigationType.All);
						if (distance > num)
						{
							num = distance;
						}
						if (settlement.HasPort)
						{
							distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, true, true, MobileParty.NavigationType.All);
							if (distance > num)
							{
								num = distance;
							}
						}
					}
				}
			}
			return num;
		}

		// Token: 0x06001A31 RID: 6705 RVA: 0x00083D2C File Offset: 0x00081F2C
		private static int CalculateTotalProsperity(Settlement settlement)
		{
			int num = 0;
			if (settlement.IsFortification)
			{
				num = (int)settlement.Town.Prosperity;
				using (List<Village>.Enumerator enumerator = settlement.BoundVillages.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Village village = enumerator.Current;
						num += (int)village.Hearth;
					}
					return num;
				}
			}
			if (settlement.IsVillage)
			{
				num = (int)settlement.Village.Hearth;
			}
			return num;
		}

		// Token: 0x06001A32 RID: 6706 RVA: 0x00083DB0 File Offset: 0x00081FB0
		public override float CalculateSettlementBaseValue(Settlement settlement)
		{
			float num = (settlement.IsCastle ? 1.25f : 1f);
			float value = settlement.GetValue(null, true);
			float baseGeographicalAdvantage = DefaultSettlementValueModel.GetBaseGeographicalAdvantage(settlement.IsVillage ? settlement.Village.Bound : settlement);
			return num * value * baseGeographicalAdvantage * 0.33f;
		}

		// Token: 0x06001A33 RID: 6707 RVA: 0x00083E00 File Offset: 0x00082000
		public override float CalculateSettlementValueForFaction(Settlement settlement, IFaction faction)
		{
			float num = (settlement.IsCastle ? 1.25f : 1f);
			float num2 = ((settlement.MapFaction == faction.MapFaction) ? 1.1f : 1f);
			float num3 = ((settlement.Culture == ((faction != null) ? faction.Culture : null)) ? 1.1f : 1f);
			float value = settlement.GetValue(null, true);
			float num4 = DefaultSettlementValueModel.GeographicalAdvantageForFaction(settlement.IsVillage ? settlement.Village.Bound : settlement, faction);
			float num5 = 1f;
			if (settlement.HasPort)
			{
				num5 = 1.2f;
				if (!faction.Settlements.Any((Settlement x) => x.HasPort))
				{
					num5 *= 1.4f;
				}
			}
			return value * num * num2 * num3 * num4 * num5 * 0.33f;
		}

		// Token: 0x06001A34 RID: 6708 RVA: 0x00083EE0 File Offset: 0x000820E0
		public override float CalculateSettlementValueForEnemyHero(Settlement settlement, Hero hero)
		{
			float num = (settlement.IsCastle ? 1.25f : 1f);
			float num2 = ((settlement.OwnerClan == hero.Clan) ? 1.1f : 1f);
			float num3 = ((settlement.Culture == hero.Culture) ? 1.1f : 1f);
			float value = settlement.GetValue(null, true);
			float num4 = DefaultSettlementValueModel.GeographicalAdvantageForFaction(settlement.IsVillage ? settlement.Village.Bound : settlement, hero.MapFaction);
			float num5 = 1f;
			if (settlement.HasPort)
			{
				num5 = 1.2f;
				if (!hero.Clan.Settlements.Any((Settlement x) => x.HasPort))
				{
					num5 *= 1.4f;
				}
			}
			return value * num * num3 * num2 * num4 * num5 * 0.33f;
		}

		// Token: 0x06001A35 RID: 6709 RVA: 0x00083FC4 File Offset: 0x000821C4
		private static float GetBaseGeographicalAdvantage(Settlement settlement)
		{
			float num = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement.MapFaction.FactionMidSettlement, settlement, false, false, MobileParty.NavigationType.All) / Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
			return 1f / (1f + num);
		}

		// Token: 0x06001A36 RID: 6710 RVA: 0x00084010 File Offset: 0x00082210
		private static float GeographicalAdvantageForFaction(Settlement settlement, IFaction faction)
		{
			Settlement factionMidSettlement = faction.FactionMidSettlement;
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, false, false, MobileParty.NavigationType.All);
			if (faction.FactionMidSettlement.MapFaction != faction)
			{
				return MathF.Clamp(Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) / (distance + 0.1f), 0f, 4f);
			}
			float distanceToClosestNonAllyFortification = faction.DistanceToClosestNonAllyFortification;
			if (settlement.MapFaction == faction && distance < distanceToClosestNonAllyFortification)
			{
				return MathF.Clamp(Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) / (distanceToClosestNonAllyFortification - distance), 1f, 4f);
			}
			float num = (distance - distanceToClosestNonAllyFortification) / Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
			return 1f / (1f + num);
		}

		// Token: 0x040008B5 RID: 2229
		private const float BenefitRatioForFaction = 0.33f;

		// Token: 0x040008B6 RID: 2230
		private const float CastleMultiplier = 1.25f;

		// Token: 0x040008B7 RID: 2231
		private const float SameMapFactionMultiplier = 1.1f;

		// Token: 0x040008B8 RID: 2232
		private const float SameCultureMultiplier = 1.1f;

		// Token: 0x040008B9 RID: 2233
		private const float BeingOwnerMultiplier = 1.1f;

		// Token: 0x040008BA RID: 2234
		private const float HavingNoCoastalSettlementMultiplier = 1.4f;

		// Token: 0x040008BB RID: 2235
		private const float HavingPortMultiplier = 1.2f;

		// Token: 0x040008BC RID: 2236
		private const int SettlementAtWarWithClan = 10240;

		// Token: 0x040008BD RID: 2237
		private const int HomeSettlementToOtherClanScore = 10;

		// Token: 0x040008BE RID: 2238
		private const int AlreadyOwnerClanScoreForHomeSettlement = 5120;

		// Token: 0x040008BF RID: 2239
		private const int SameFactionWithClanScoreForHomeSettlement = 2560;

		// Token: 0x040008C0 RID: 2240
		private const int SettlementTypeScoreForHomeSettlementTown = 1280;

		// Token: 0x040008C1 RID: 2241
		private const int SettlementTypeScoreForHomeSettlementCastle = 640;

		// Token: 0x040008C2 RID: 2242
		private const int SettlementTypeScoreForHomeSettlementVillage = 320;

		// Token: 0x040008C3 RID: 2243
		private const int MidSettlementDistanceScoreForHomeSettlement = 20;

		// Token: 0x040008C4 RID: 2244
		private const int SameCultureWithClanCultureScoreForHomeSettlement = 17;

		// Token: 0x040008C5 RID: 2245
		private const float AlreadyHomeSettlementScoreForHomeSettlement = 4.5f;

		// Token: 0x040008C6 RID: 2246
		private const float InitialHomeSettlementScoreForHomeSettlement = 3.5f;

		// Token: 0x040008C7 RID: 2247
		private const int SettlementOwnerClanCultureSameForHomeSettlement = 12;

		// Token: 0x040008C8 RID: 2248
		private const int NeighborScoreForHomeSettlement = 10;

		// Token: 0x040008C9 RID: 2249
		private const int ProsperityScoreForHomeSettlement = 5;
	}
}
