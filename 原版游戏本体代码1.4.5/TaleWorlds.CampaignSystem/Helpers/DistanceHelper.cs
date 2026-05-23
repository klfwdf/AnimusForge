using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace Helpers;

public static class DistanceHelper
{
	public const int BirdFlyDistanceSquaredThresholdForMobilePartyToMobilePartyDistance = 2500;

	public static float FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isFromPort, out bool isTargetingPort, out float landRatio)
	{
		bool num = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval;
		bool flag = (navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default;
		bool flag2 = num && fromSettlement.HasPort && fromSettlement != toSettlement;
		bool flag3 = num && toSettlement.HasPort && fromSettlement != toSettlement;
		float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, isFromPort: false, isTargetingPort: false, navCapabilities, out landRatio);
		isFromPort = false;
		isTargetingPort = false;
		if (flag2 && flag)
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, isFromPort: true, isTargetingPort: false, navCapabilities, out landRatio);
			if (distance < num2)
			{
				isFromPort = true;
				isTargetingPort = false;
				num2 = distance;
			}
		}
		if (flag3 && flag)
		{
			float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, isFromPort: false, isTargetingPort: true, navCapabilities, out landRatio);
			if (distance2 < num2)
			{
				isFromPort = false;
				isTargetingPort = true;
				num2 = distance2;
			}
		}
		if (flag2 && flag3)
		{
			float distance3 = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, isFromPort: true, isTargetingPort: true, navCapabilities, out landRatio);
			if (distance3 < num2)
			{
				isFromPort = true;
				isTargetingPort = true;
				num2 = distance3;
			}
		}
		return num2;
	}

	private static float FindClosestDistanceFromSettlementToSettlementForMobileParty(MobileParty mobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isFromPort, out bool isTargetingPort, out float landRatio)
	{
		Settlement currentSettlement = mobileParty.CurrentSettlement;
		bool num = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval;
		bool flag = (navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default;
		bool flag2 = num && currentSettlement.HasPort && currentSettlement != toSettlement;
		bool flag3 = num && toSettlement.HasPort && currentSettlement != toSettlement;
		float num2 = float.MaxValue;
		if (navCapabilities != MobileParty.NavigationType.Naval)
		{
			num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, isFromPort: false, isTargetingPort: false, navCapabilities, out landRatio);
		}
		else
		{
			landRatio = 0f;
		}
		bool flag4 = flag2 && mobileParty.Anchor.IsAtSettlement(currentSettlement);
		isFromPort = false;
		isTargetingPort = false;
		if (flag2 && flag)
		{
			float landRatio2;
			float num3 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, isFromPort: true, isTargetingPort: false, navCapabilities, out landRatio2);
			if (!flag4)
			{
				num3 += (float)Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
			}
			if (num3 < num2)
			{
				isFromPort = true;
				isTargetingPort = false;
				num2 = num3;
				landRatio = landRatio2;
			}
		}
		if (flag3 && flag)
		{
			float landRatio3;
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, isFromPort: false, isTargetingPort: true, navCapabilities, out landRatio3);
			if (distance < num2)
			{
				isFromPort = false;
				isTargetingPort = true;
				num2 = distance;
				landRatio = landRatio3;
			}
		}
		if (flag2 && flag3)
		{
			float landRatio4;
			float num4 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, toSettlement, isFromPort: true, isTargetingPort: true, navCapabilities, out landRatio4);
			if (!flag4)
			{
				num4 += (float)Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
			}
			if (num4 < num2)
			{
				isFromPort = true;
				isTargetingPort = true;
				num2 = num4;
				landRatio = landRatio4;
			}
		}
		return num2;
	}

	public static float FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities)
	{
		bool isFromPort;
		bool isTargetingPort;
		float landRatio;
		return FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out isFromPort, out isTargetingPort, out landRatio);
	}

	public static float FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out float landRatio)
	{
		bool isFromPort;
		bool isTargetingPort;
		return FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out isFromPort, out isTargetingPort, out landRatio);
	}

	public static float FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isTargetingPort, out float landRatio)
	{
		float num = float.MaxValue;
		isTargetingPort = false;
		landRatio = -1f;
		if (fromMobileParty.CurrentSettlement != null)
		{
			num = FindClosestDistanceFromSettlementToSettlementForMobileParty(fromMobileParty, toSettlement, navCapabilities, out var _, out isTargetingPort, out landRatio);
		}
		else
		{
			bool num2 = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval;
			if ((navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default)
			{
				num = Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toSettlement, isTargetingPort: false, navCapabilities, out landRatio);
			}
			if (num2 && toSettlement.HasPort)
			{
				float estimatedLandRatio;
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toSettlement, isTargetingPort: true, navCapabilities, out estimatedLandRatio);
				if (distance < num)
				{
					isTargetingPort = true;
					num = distance;
					landRatio = estimatedLandRatio;
				}
			}
		}
		return num;
	}

	public static float FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities)
	{
		bool isTargetingPort;
		float landRatio;
		return FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out isTargetingPort, out landRatio);
	}

	public static float FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out float landRatio)
	{
		bool isTargetingPort;
		return FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out isTargetingPort, out landRatio);
	}

	public static bool FindClosestDistanceFromMobilePartyToSettlement(MobileParty fromMobileParty, Settlement toSettlement, MobileParty.NavigationType navCapabilities, float maxDistance, out float distance, out float landRatio)
	{
		distance = FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out landRatio);
		return distance < maxDistance;
	}

	public static bool FindClosestDistanceFromSettlementToSettlement(Settlement fromSettlement, Settlement toSettlement, MobileParty.NavigationType navCapabilities, float maxDistance, out float distance, out float landRatio)
	{
		distance = FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out landRatio);
		return distance < maxDistance;
	}

	public static bool FindClosestDistanceFromMobilePartyToMobileParty(MobileParty from, MobileParty to, MobileParty.NavigationType navigationType, float maxDistance, out float distance, out float landRatio)
	{
		distance = FindClosestDistanceFromMobilePartyToMobileParty(from, to, navigationType, out landRatio);
		return distance < maxDistance;
	}

	public static float FindClosestDistanceFromMobilePartyToMobileParty(MobileParty from, MobileParty to, MobileParty.NavigationType navigationType)
	{
		float landRatio;
		return FindClosestDistanceFromMobilePartyToMobileParty(from, to, navigationType, out landRatio);
	}

	public static float FindClosestDistanceFromMobilePartyToMobileParty(MobileParty from, MobileParty to, MobileParty.NavigationType navigationType, out float landRatio)
	{
		Settlement currentSettlement = from.CurrentSettlement;
		Settlement currentSettlement2 = to.CurrentSettlement;
		if (currentSettlement2 != null)
		{
			return FindClosestDistanceFromMobilePartyToSettlement(from, currentSettlement2, navigationType, out landRatio);
		}
		if (currentSettlement != null)
		{
			return FindClosestDistanceFromSettlementToPointForMobileParty(from, to.Position, navigationType, out landRatio);
		}
		if (from.Position.DistanceSquared(to.Position) < 2500f)
		{
			return Campaign.Current.Models.MapDistanceModel.GetDistance(from, to, navigationType, out landRatio);
		}
		return GetDistanceBetweenMobilePartyToMobileParty(from, to, navigationType, out landRatio);
	}

	public static float FindClosestDistanceFromSettlementToPoint(Settlement fromSettlement, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out bool isFromPort)
	{
		bool num = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval && fromSettlement.HasPort;
		isFromPort = false;
		float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, in point, !point.IsOnLand, navCapabilities);
		if (num)
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, in point, isFromPort: true, navCapabilities);
			if (distance < num2)
			{
				isFromPort = true;
				num2 = distance;
			}
		}
		return num2;
	}

	public static float FindClosestDistanceFromMapPointToSettlement(IMapPoint mapPoint, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out bool isTargetingPort, out float landRatio)
	{
		isTargetingPort = false;
		if (mapPoint is Settlement fromSettlement)
		{
			bool isFromPort;
			return FindClosestDistanceFromSettlementToSettlement(fromSettlement, toSettlement, navCapabilities, out isFromPort, out isTargetingPort, out landRatio);
		}
		if (mapPoint is MobileParty fromMobileParty)
		{
			return FindClosestDistanceFromMobilePartyToSettlement(fromMobileParty, toSettlement, navCapabilities, out isTargetingPort, out landRatio);
		}
		float num = Campaign.Current.Models.MapDistanceModel.GetDistance(toSettlement, mapPoint.Position, isFromPort: false, navCapabilities);
		landRatio = 1f;
		if ((navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval && toSettlement.HasPort)
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(toSettlement, mapPoint.Position, isFromPort: true, navCapabilities);
			if (distance < num)
			{
				isTargetingPort = true;
				num = distance;
				landRatio = 0f;
			}
		}
		return num;
	}

	public static float FindClosestDistanceFromSettlementToPoint(Settlement fromSettlement, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out float landRatio)
	{
		return FindClosestDistanceFromSettlementToPoint(fromSettlement, point, navCapabilities, out landRatio);
	}

	private static float FindClosestDistanceFromSettlementToPointForMobileParty(MobileParty mobileParty, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out float landRatio)
	{
		Settlement currentSettlement = mobileParty.CurrentSettlement;
		if ((!mobileParty.IsCurrentlyAtSea || (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval) && (mobileParty.IsCurrentlyAtSea || (navCapabilities & MobileParty.NavigationType.Default) == MobileParty.NavigationType.Default))
		{
			bool flag = (navCapabilities & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval && currentSettlement.HasPort;
			float num = float.MaxValue;
			if (navCapabilities != MobileParty.NavigationType.Naval)
			{
				num = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, in point, isFromPort: false, navCapabilities);
			}
			bool flag2 = flag && mobileParty.Anchor.IsAtSettlement(currentSettlement);
			if (flag)
			{
				float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(currentSettlement, in point, isFromPort: true, navCapabilities);
				if (!flag2)
				{
					num2 += (float)Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
				}
				if (num2 < num)
				{
					num = num2;
				}
			}
			landRatio = (((mobileParty.IsCurrentlyAtSea || flag) && !point.IsOnLand) ? 0f : (((mobileParty.IsCurrentlyAtSea || flag) == point.IsOnLand) ? 0.5f : ((!(mobileParty.IsCurrentlyAtSea || flag) && point.IsOnLand) ? 1f : (-1f))));
			return num;
		}
		landRatio = -1f;
		return float.MaxValue;
	}

	public static float FindClosestDistanceFromMobilePartyToPoint(MobileParty fromMobileParty, CampaignVec2 point, MobileParty.NavigationType navCapabilities)
	{
		float landRatio;
		return FindClosestDistanceFromMobilePartyToPoint(fromMobileParty, point, navCapabilities, out landRatio);
	}

	public static float FindClosestDistanceFromMobilePartyToPoint(MobileParty fromMobileParty, CampaignVec2 point, MobileParty.NavigationType navCapabilities, out float landRatio)
	{
		if (fromMobileParty.CurrentSettlement != null)
		{
			return FindClosestDistanceFromSettlementToPointForMobileParty(fromMobileParty, point, navCapabilities, out landRatio);
		}
		return Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, in point, navCapabilities, out landRatio);
	}

	public static float FindClosestDistanceFromMapPointToSettlement(IMapPoint mapPoint, Settlement toSettlement, MobileParty.NavigationType navCapabilities, out float landRatio)
	{
		bool isTargetingPort;
		return FindClosestDistanceFromMapPointToSettlement(mapPoint, toSettlement, navCapabilities, out isTargetingPort, out landRatio);
	}

	public static float GetDistanceBetweenMobilePartyToMobileParty(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, out float landRatio)
	{
		(Settlement, bool) closestEntranceToFace = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(fromMobileParty.CurrentNavigationFace, customCapability);
		(Settlement, bool) closestEntranceToFace2 = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(toMobileParty.CurrentNavigationFace, customCapability);
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
					float distanceBetweenMobilePartyToMobileParty = GetDistanceBetweenMobilePartyToMobileParty(fromMobileParty, toMobileParty, (!fromMobileParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval, out landRatio);
					num = MathF.Min(num, distanceBetweenMobilePartyToMobileParty);
				}
			}
		}
		return num;
	}
}
