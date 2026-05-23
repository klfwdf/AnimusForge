using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMapDistanceModel : MapDistanceModel
{
	private INavigationCache _navigationCache;

	public override int RegionSwitchCostFromLandToSea => 0;

	public override int RegionSwitchCostFromSeaToLand => 0;

	public override float MaximumSpawnDistanceForCompanionsAfterDisband => 150f;

	public override void RegisterDistanceCache(MobileParty.NavigationType navigationCapability, INavigationCache cacheToRegister)
	{
		_navigationCache = cacheToRegister;
		cacheToRegister.FinalizeInitialization();
	}

	public override float GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType navigationCapabilities)
	{
		return _navigationCache?.MaximumDistanceBetweenTwoConnectedSettlements ?? 0f;
	}

	public override float GetLandRatioOfPathBetweenSettlements(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort)
	{
		if (_navigationCache != null)
		{
			_navigationCache.GetSettlementToSettlementDistanceWithLandRatio(fromSettlement, isAtSea1: false, toSettlement, isAtSea2: false, out var landRatio);
			return landRatio;
		}
		return 1f;
	}

	public override float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort = false, bool isTargetingPort = false, MobileParty.NavigationType navigationCapability = MobileParty.NavigationType.Default)
	{
		float landRatio;
		return GetDistance(fromSettlement, toSettlement, isFromPort, isTargetingPort, MobileParty.NavigationType.Default, out landRatio);
	}

	public override float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort, MobileParty.NavigationType navigationCapability, out float landRatio)
	{
		float result = float.MaxValue;
		landRatio = 1f;
		if (fromSettlement != null && toSettlement != null)
		{
			if (fromSettlement != toSettlement)
			{
				return _navigationCache.GetSettlementToSettlementDistanceWithLandRatio(fromSettlement, isFromPort, toSettlement, isTargetingPort, out landRatio);
			}
			result = 0f;
		}
		return result;
	}

	public override float GetDistance(MobileParty fromMobileParty, Settlement toSettlement, bool isTargetingPort, MobileParty.NavigationType customCapability, out float estimatedLandRatio)
	{
		float value = 100000000f;
		estimatedLandRatio = 1f;
		if (fromMobileParty.CurrentNavigationFace.FaceIndex == toSettlement.GatePosition.Face.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(fromMobileParty.Position.Face), MobileParty.NavigationType.Default))
			{
				value = fromMobileParty.Position.Distance(toSettlement.GatePosition);
			}
		}
		else if (fromMobileParty.IsCurrentlyAtSea)
		{
			value = 100000000f;
		}
		else
		{
			Settlement item = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(fromMobileParty.CurrentNavigationFace, MobileParty.NavigationType.Default).Item1;
			if (item != null)
			{
				value = fromMobileParty.Position.Distance(toSettlement.GatePosition) - item.GatePosition.Distance(toSettlement.GatePosition) + Campaign.Current.Models.MapDistanceModel.GetDistance(item, toSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
			}
		}
		return MBMath.ClampFloat(value, 0f, float.MaxValue);
	}

	public override float GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, out float landRatio)
	{
		Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toMobileParty, customCapability, 100000000f, out var distance, out landRatio);
		return distance;
	}

	public override bool GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, float maxDistance, out float distance, out float landRatio)
	{
		landRatio = 1f;
		distance = float.MaxValue;
		if (fromMobileParty.CurrentNavigationFace.FaceIndex == toMobileParty.CurrentNavigationFace.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(fromMobileParty.Position.Face), MobileParty.NavigationType.Default))
			{
				distance = fromMobileParty.Position.Distance(toMobileParty.Position);
			}
		}
		else if (fromMobileParty.IsCurrentlyAtSea || toMobileParty.IsCurrentlyAtSea)
		{
			distance = float.MaxValue;
		}
		else
		{
			distance = fromMobileParty.Position.Distance(toMobileParty.Position);
		}
		distance = MBMath.ClampFloat(distance, 0f, float.MaxValue);
		return distance <= maxDistance;
	}

	public override float GetDistance(MobileParty fromMobileParty, in CampaignVec2 toPoint, MobileParty.NavigationType customCapability, out float landRatio)
	{
		float value = float.MaxValue;
		landRatio = 1f;
		PathFaceRecord face = toPoint.Face;
		if (fromMobileParty.CurrentNavigationFace.FaceIndex == face.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(fromMobileParty.Position.Face), MobileParty.NavigationType.Default))
			{
				value = fromMobileParty.Position.Distance(toPoint);
			}
		}
		else
		{
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			(Settlement, bool) closestEntranceToFace = mapDistanceModel.GetClosestEntranceToFace(fromMobileParty.CurrentNavigationFace, MobileParty.NavigationType.Default);
			(Settlement, bool) closestEntranceToFace2 = mapDistanceModel.GetClosestEntranceToFace(face, MobileParty.NavigationType.Default);
			var (settlement, _) = closestEntranceToFace;
			var (settlement2, _) = closestEntranceToFace2;
			if (settlement != null && settlement2 != null)
			{
				value = fromMobileParty.Position.Distance(toPoint) - settlement.GatePosition.Distance(settlement2.GatePosition) + GetDistance(settlement, settlement2, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
			}
		}
		return MBMath.ClampFloat(value, 0f, float.MaxValue);
	}

	public override float GetDistance(Settlement fromSettlement, in CampaignVec2 toPoint, bool isFromPort, MobileParty.NavigationType customCapability)
	{
		float value = float.MaxValue;
		CampaignVec2 campaignVec = (isFromPort ? fromSettlement.PortPosition : fromSettlement.GatePosition);
		PathFaceRecord face = toPoint.Face;
		PathFaceRecord face2 = campaignVec.Face;
		if (face2.FaceIndex == face.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face2), MobileParty.NavigationType.Default))
			{
				value = campaignVec.Distance(toPoint);
			}
		}
		else
		{
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			Settlement item = mapDistanceModel.GetClosestEntranceToFace(face, MobileParty.NavigationType.Default).Item1;
			if (item != null)
			{
				value = fromSettlement.GatePosition.Distance(toPoint) - fromSettlement.GatePosition.Distance(item.GatePosition) + mapDistanceModel.GetDistance(fromSettlement, item, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
			}
		}
		return MBMath.ClampFloat(value, 0f, 100000000f);
	}

	public override float GetPortToGateDistanceForSettlement(Settlement settlement)
	{
		return 100000000f;
	}

	public override bool PathExistBetweenPoints(in CampaignVec2 fromPoint, in CampaignVec2 toPoint, MobileParty.NavigationType navigationType)
	{
		if (fromPoint.IsOnLand)
		{
			return toPoint.IsOnLand;
		}
		return false;
	}

	public override (Settlement, bool) GetClosestEntranceToFace(PathFaceRecord face, MobileParty.NavigationType navigationCapabilities)
	{
		bool isAtSea;
		return (_navigationCache.GetClosestSettlementToFaceIndex(face.FaceIndex, out isAtSea), isAtSea);
	}

	public override MBReadOnlyList<Settlement> GetNeighborsOfFortification(Town town, MobileParty.NavigationType navigationCapabilities)
	{
		return _navigationCache.GetNeighbors(town.Settlement);
	}

	public override float GetTransitionCostAdjustment(Settlement settlement1, bool isFromPort, Settlement settlement2, bool isTargetingPort, bool fromIsCurrentlyAtSea, bool toIsCurrentlyAtSea)
	{
		return 0f;
	}
}
