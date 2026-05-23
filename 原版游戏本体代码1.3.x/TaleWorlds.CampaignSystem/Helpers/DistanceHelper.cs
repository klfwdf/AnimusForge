using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x02000024 RID: 36
	public static class DistanceHelper
	{
		// Token: 0x0600014F RID: 335 RVA: 0x0000F6E8 File Offset: 0x0000D8E8
		public static float FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isFromPort, out bool isTargetingPort, out float landRatio)
		{
			bool flag = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval;
			bool flag2 = (navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default;
			bool flag3 = flag && fromSettlement.HasPort && fromSettlement != toSettlement;
			bool flag4 = flag && toSettlement.HasPort && fromSettlement != toSettlement;
			float num = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, false, false, navCapabilities, out landRatio);
			isFromPort = false;
			isTargetingPort = false;
			if (flag3 && flag2)
			{
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, true, false, navCapabilities, out landRatio);
				if (distance < num)
				{
					isFromPort = true;
					isTargetingPort = false;
					num = distance;
				}
			}
			if (flag4 && flag2)
			{
				float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, false, true, navCapabilities, out landRatio);
				if (distance2 < num)
				{
					isFromPort = false;
					isTargetingPort = true;
					num = distance2;
				}
			}
			if (flag3 && flag4)
			{
				float distance3 = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, true, true, navCapabilities, out landRatio);
				if (distance3 < num)
				{
					isFromPort = true;
					isTargetingPort = true;
					num = distance3;
				}
			}
			return num;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000F7E4 File Offset: 0x0000D9E4
		private static float FindClosestDistanceFromSettlementToSettlementForMobileParty(MobileParty mobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isFromPort, out bool isTargetingPort, out float landRatio)
		{
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			bool flag = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval;
			bool flag2 = (navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default;
			bool flag3 = flag && currentSettlement.HasPort && currentSettlement != toSettlement;
			bool flag4 = flag && toSettlement.HasPort && currentSettlement != toSettlement;
			float num = float.MaxValue;
			if (navCapabilities != MobileParty.NavigationType.Naval)
			{
				num = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, false, false, navCapabilities, out landRatio);
			}
			else
			{
				landRatio = 0f;
			}
			bool flag5 = mobileParty.Anchor.IsAtSettlement(currentSettlement);
			isFromPort = false;
			isTargetingPort = false;
			if (flag3 && flag2)
			{
				float num3;
				float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, true, false, navCapabilities, out num3);
				if (!flag5)
				{
					num2 += (float)Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
				}
				if (num2 < num)
				{
					isFromPort = true;
					isTargetingPort = false;
					num = num2;
					landRatio = num3;
				}
			}
			if (flag4 && flag2)
			{
				float num4;
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, false, true, navCapabilities, out num4);
				if (distance < num)
				{
					isFromPort = false;
					isTargetingPort = true;
					num = distance;
					landRatio = num4;
				}
			}
			if (flag3 && flag4)
			{
				float num6;
				float num5 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, true, true, navCapabilities, out num6);
				if (!flag5)
				{
					num5 += (float)Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
				}
				if (num5 < num)
				{
					isFromPort = true;
					isTargetingPort = true;
					num = num5;
					landRatio = num6;
				}
			}
			return num;
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000F960 File Offset: 0x0000DB60
		public static float FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities)
		{
			bool flag;
			bool flag2;
			float num;
			return DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out flag, out flag2, out num);
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000F97C File Offset: 0x0000DB7C
		public static float FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out float landRatio)
		{
			bool flag;
			bool flag2;
			return DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out flag, out flag2, out landRatio);
		}

		// Token: 0x06000153 RID: 339 RVA: 0x0000F998 File Offset: 0x0000DB98
		public static float FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isTargetingPort, out float landRatio)
		{
			float num = float.MaxValue;
			isTargetingPort = false;
			landRatio = -1f;
			if (fromMobileParty.CurrentSettlement != null)
			{
				bool flag;
				num = DistanceHelper.FindClosestDistanceFromSettlementToSettlementForMobileParty(fromMobileParty, toSettlement, navCapabilities, out flag, out isTargetingPort, out landRatio);
			}
			else
			{
				bool flag2 = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval;
				if ((navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default)
				{
					num = Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toSettlement, false, navCapabilities, out landRatio);
				}
				if (flag2 && toSettlement.HasPort)
				{
					float num2;
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toSettlement, true, navCapabilities, out num2);
					if (distance < num)
					{
						isTargetingPort = true;
						num = distance;
						landRatio = num2;
					}
				}
			}
			return num;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000FA30 File Offset: 0x0000DC30
		public static float FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities)
		{
			bool flag;
			float num;
			return DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out flag, out num);
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000FA4C File Offset: 0x0000DC4C
		public static float FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out float landRatio)
		{
			bool flag;
			return DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out flag, out landRatio);
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000FA64 File Offset: 0x0000DC64
		public static bool FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, float maxDistance, out float distance, out float landRatio)
		{
			distance = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out landRatio);
			return distance < maxDistance;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x0000FA79 File Offset: 0x0000DC79
		public static bool FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities, float maxDistance, out float distance, out float landRatio)
		{
			distance = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out landRatio);
			return distance < maxDistance;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x0000FA8E File Offset: 0x0000DC8E
		public static bool FindClosestDistanceFromMobilePartyToMobileParty(MobileParty from, MobileParty to, MobileParty.NavigationType navigationType, float maxDistance, out float distance, out float landRatio)
		{
			distance = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(from, to, navigationType, out landRatio);
			return distance < maxDistance;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x0000FAA4 File Offset: 0x0000DCA4
		public static float FindClosestDistanceFromMobilePartyToMobileParty(MobileParty from, MobileParty to, MobileParty.NavigationType navigationType)
		{
			float num;
			return DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(from, to, navigationType, out num);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000FABC File Offset: 0x0000DCBC
		public static float FindClosestDistanceFromMobilePartyToMobileParty(MobileParty from, MobileParty to, MobileParty.NavigationType navigationType, out float landRatio)
		{
			Settlement currentSettlement = from.CurrentSettlement;
			Settlement currentSettlement2 = to.CurrentSettlement;
			if (currentSettlement2 != null)
			{
				return DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(from, currentSettlement2, navigationType, out landRatio);
			}
			if (currentSettlement != null)
			{
				return DistanceHelper.FindClosestDistanceFromSettlementToPointForMobileParty(from, to.Position, navigationType, out landRatio);
			}
			if (from.Position.DistanceSquared(to.Position) < 2500f)
			{
				return Campaign.Current.Models.MapDistanceModel.GetDistance(from, to, navigationType, out landRatio);
			}
			return DistanceHelper.GetDistanceBetweenMobilePartyToMobileParty(from, to, navigationType, out landRatio);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0000FB34 File Offset: 0x0000DD34
		public static float FindClosestDistanceFromSettlementToPoint(Settlement fromSettlement, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out bool isFromPort)
		{
			bool flag = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval && fromSettlement.HasPort;
			isFromPort = false;
			float num = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, point, !point.IsOnLand, navCapabilities);
			if (flag)
			{
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, point, true, navCapabilities);
				if (distance < num)
				{
					isFromPort = true;
					num = distance;
				}
			}
			return num;
		}

		// Token: 0x0600015C RID: 348 RVA: 0x0000FBA0 File Offset: 0x0000DDA0
		public static float FindClosestDistanceFromMapPointToSettlement(IMapPoint mapPoint, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isTargetingPort, out float landRatio)
		{
			isTargetingPort = false;
			Settlement fromSettlement;
			if ((fromSettlement = mapPoint as Settlement) != null)
			{
				bool flag;
				return DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out flag, out isTargetingPort, out landRatio);
			}
			MobileParty fromMobileParty;
			if ((fromMobileParty = mapPoint as MobileParty) != null)
			{
				return DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out isTargetingPort, out landRatio);
			}
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			CampaignVec2 position = mapPoint.Position;
			float num = mapDistanceModel.GetDistance(toSettlement, position, false, navCapabilities);
			landRatio = 1f;
			if ((navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval && toSettlement.HasPort)
			{
				MapDistanceModel mapDistanceModel2 = Campaign.Current.Models.MapDistanceModel;
				position = mapPoint.Position;
				float distance = mapDistanceModel2.GetDistance(toSettlement, position, true, navCapabilities);
				if (distance < num)
				{
					isTargetingPort = true;
					num = distance;
					landRatio = 0f;
				}
			}
			return num;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000FC4D File Offset: 0x0000DE4D
		public static float FindClosestDistanceFromSettlementToPoint(Settlement fromSettlement, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out float landRatio)
		{
			return DistanceHelper.FindClosestDistanceFromSettlementToPoint(fromSettlement, point, navCapabilities, out landRatio);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000FC58 File Offset: 0x0000DE58
		private static float FindClosestDistanceFromSettlementToPointForMobileParty(MobileParty mobileParty, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out float landRatio)
		{
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			if ((!mobileParty.IsCurrentlyAtSea || (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval) && (mobileParty.IsCurrentlyAtSea || (navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default))
			{
				bool flag = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval && currentSettlement.HasPort;
				float num = float.MaxValue;
				if (navCapabilities != MobileParty.NavigationType.Naval)
				{
					num = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, point, false, navCapabilities);
				}
				bool flag2 = mobileParty.Anchor.IsAtSettlement(currentSettlement);
				if (flag)
				{
					float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, point, true, navCapabilities);
					if (!flag2)
					{
						num2 += (float)Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
					}
					if (num2 < num)
					{
						num = num2;
					}
				}
				landRatio = (((mobileParty.IsCurrentlyAtSea || flag) && !point.IsOnLand) ? 0f : (((mobileParty.IsCurrentlyAtSea || flag) == point.IsOnLand) ? 0.5f : ((!mobileParty.IsCurrentlyAtSea && !flag && point.IsOnLand) ? 1f : (-1f))));
				return num;
			}
			landRatio = -1f;
			return float.MaxValue;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000FD7C File Offset: 0x0000DF7C
		public static float FindClosestDistanceFromMobilePartyToPoint(MobileParty fromMobileParty, CampaignVec2 point, MobileParty.NavigationType navCapabilities)
		{
			float num;
			return DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(fromMobileParty, point, navCapabilities, out num);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x0000FD93 File Offset: 0x0000DF93
		public static float FindClosestDistanceFromMobilePartyToPoint(MobileParty fromMobileParty, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out float landRatio)
		{
			if (fromMobileParty.CurrentSettlement != null)
			{
				return DistanceHelper.FindClosestDistanceFromSettlementToPointForMobileParty(fromMobileParty, point, navCapabilities, out landRatio);
			}
			return Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, point, navCapabilities, out landRatio);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x0000FDC0 File Offset: 0x0000DFC0
		public static float FindClosestDistanceFromMapPointToSettlement(IMapPoint mapPoint, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out float landRatio)
		{
			bool flag;
			return DistanceHelper.FindClosestDistanceFromMapPointToSettlement(mapPoint, toSettlement, navCapabilities, out flag, out landRatio);
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000FDD8 File Offset: 0x0000DFD8
		public static float GetDistanceBetweenMobilePartyToMobileParty(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, out float landRatio)
		{
			ValueTuple<Settlement, bool> closestEntranceToFace = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(fromMobileParty.CurrentNavigationFace, customCapability);
			ValueTuple<Settlement, bool> closestEntranceToFace2 = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(toMobileParty.CurrentNavigationFace, customCapability);
			Settlement item = closestEntranceToFace.Item1;
			Settlement item2 = closestEntranceToFace2.Item1;
			float num = float.MaxValue;
			landRatio = -1f;
			if (item != null && item2 != null)
			{
				bool item3 = closestEntranceToFace.Item2;
				bool item4 = closestEntranceToFace2.Item2;
				CampaignVec2 campaignVec = (item3 ? item.PortPosition : item.GatePosition);
				CampaignVec2 v = (item4 ? item2.PortPosition : item2.GatePosition);
				num = fromMobileParty.Position.Distance(toMobileParty.Position) - campaignVec.Distance(v) + Campaign.Current.Models.MapDistanceModel.GetDistance(item, item2, item3, item4, customCapability, out landRatio);
				if (customCapability == MobileParty.NavigationType.All)
				{
					num += Campaign.Current.Models.MapDistanceModel.GetTransitionCostAdjustment(item, item3, item2, item4, fromMobileParty.IsCurrentlyAtSea, toMobileParty.IsCurrentlyAtSea);
					if (fromMobileParty.IsCurrentlyAtSea == toMobileParty.IsCurrentlyAtSea)
					{
						float distanceBetweenMobilePartyToMobileParty = DistanceHelper.GetDistanceBetweenMobilePartyToMobileParty(fromMobileParty, toMobileParty, fromMobileParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Naval : MobileParty.NavigationType.Default, out landRatio);
						num = MathF.Min(num, distanceBetweenMobilePartyToMobileParty);
					}
				}
			}
			return num;
		}

		// Token: 0x04000006 RID: 6
		public const int BirdFlyDistanceSquaredThresholdForMobilePartyToMobilePartyDistance = 2500;
	}
}
