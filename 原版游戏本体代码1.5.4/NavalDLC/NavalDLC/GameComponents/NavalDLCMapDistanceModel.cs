using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCMapDistanceModel : MapDistanceModel
{
	private Dictionary<MobileParty.NavigationType, INavigationCache> _navigationCaches = new Dictionary<MobileParty.NavigationType, INavigationCache>();

	public override int RegionSwitchCostFromLandToSea => 50;

	public override int RegionSwitchCostFromSeaToLand => 50;

	public override float MaximumSpawnDistanceForCompanionsAfterDisband => 150f;

	public override void RegisterDistanceCache(MobileParty.NavigationType navigationCapability, INavigationCache cacheToRegister)
	{
		_navigationCaches[navigationCapability] = cacheToRegister;
		cacheToRegister.FinalizeInitialization();
	}

	public override float GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType navigationCapabilities)
	{
		if (_navigationCaches.TryGetValue(navigationCapabilities, out var value))
		{
			return value.MaximumDistanceBetweenTwoConnectedSettlements;
		}
		return 0f;
	}

	public override float GetLandRatioOfPathBetweenSettlements(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort)
	{
		if (_navigationCaches.TryGetValue(MobileParty.NavigationType.All, out var value))
		{
			value.GetSettlementToSettlementDistanceWithLandRatio(fromSettlement, isFromPort, toSettlement, isTargetingPort, out var landRatio);
			return landRatio;
		}
		return 1f;
	}

	public override float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort = false, bool isTargetingPort = false, MobileParty.NavigationType navigationCapability = MobileParty.NavigationType.All)
	{
		float landRatio;
		return GetDistance(fromSettlement, toSettlement, isFromPort, isTargetingPort, navigationCapability, out landRatio);
	}

	public override float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort, MobileParty.NavigationType navigationCapability, out float landRatio)
	{
		float result = float.MaxValue;
		landRatio = -1f;
		if (fromSettlement != null && toSettlement != null)
		{
			if (fromSettlement != toSettlement)
			{
				return _navigationCaches[navigationCapability].GetSettlementToSettlementDistanceWithLandRatio(fromSettlement, isFromPort, toSettlement, isTargetingPort, out landRatio);
			}
			result = ((isFromPort == isTargetingPort) ? 0f : ((float)(isFromPort ? RegionSwitchCostFromSeaToLand : RegionSwitchCostFromLandToSea)));
			landRatio = navigationCapability switch
			{
				MobileParty.NavigationType.Naval => 0f, 
				MobileParty.NavigationType.Default => 1f, 
				MobileParty.NavigationType.All => 0.5f, 
				_ => -1f, 
			};
		}
		return result;
	}

	public override float GetPortToGateDistanceForSettlement(Settlement settlement)
	{
		float landRatio;
		return _navigationCaches[MobileParty.NavigationType.All].GetSettlementToSettlementDistanceWithLandRatio(settlement, isAtSea1: true, settlement, isAtSea2: false, out landRatio);
	}

	public override float GetDistance(MobileParty fromMobileParty, Settlement toSettlement, bool isTargetingPort, MobileParty.NavigationType customCapability, out float estimatedLandRatio)
	{
		float num = 100000000f;
		estimatedLandRatio = -1f;
		if (fromMobileParty.CurrentNavigationFace.FaceIndex == (isTargetingPort ? toSettlement.PortPosition.Face.FaceIndex : toSettlement.GatePosition.Face.FaceIndex))
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(fromMobileParty.Position.Face), customCapability))
			{
				num = fromMobileParty.Position.Distance(isTargetingPort ? toSettlement.PortPosition : toSettlement.GatePosition);
				estimatedLandRatio = customCapability switch
				{
					MobileParty.NavigationType.Naval => 0f, 
					MobileParty.NavigationType.Default => 1f, 
					_ => 0.5f, 
				};
			}
		}
		else if (customCapability == MobileParty.NavigationType.Default && (fromMobileParty.IsCurrentlyAtSea || isTargetingPort))
		{
			num = 100000000f;
		}
		else if (customCapability == MobileParty.NavigationType.Naval && (!fromMobileParty.IsCurrentlyAtSea || !isTargetingPort))
		{
			num = 100000000f;
		}
		else
		{
			(Settlement, bool) closestEntranceToFace = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(fromMobileParty.CurrentNavigationFace, customCapability);
			var (settlement, _) = closestEntranceToFace;
			if (settlement != null)
			{
				bool item = closestEntranceToFace.Item2;
				CampaignVec2 campaignVec = (item ? settlement.PortPosition : settlement.GatePosition);
				CampaignVec2 v = (isTargetingPort ? toSettlement.PortPosition : toSettlement.GatePosition);
				num = fromMobileParty.Position.Distance(v) - campaignVec.Distance(v) + Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, toSettlement, item, isTargetingPort, customCapability);
				if (settlement != toSettlement && customCapability == MobileParty.NavigationType.All)
				{
					bool isFromPort = settlement.HasPort && fromMobileParty.HasNavalNavigationCapability;
					bool isTargetingPort2 = toSettlement.HasPort && fromMobileParty.HasNavalNavigationCapability;
					estimatedLandRatio = GetLandRatioOfPathBetweenSettlements(settlement, toSettlement, isFromPort, isTargetingPort2);
				}
				else
				{
					estimatedLandRatio = customCapability switch
					{
						MobileParty.NavigationType.Naval => 0f, 
						MobileParty.NavigationType.Default => 1f, 
						MobileParty.NavigationType.All => 0.5f, 
						_ => -1f, 
					};
				}
				if (customCapability == MobileParty.NavigationType.All)
				{
					num += Campaign.Current.Models.MapDistanceModel.GetTransitionCostAdjustment(settlement, item, toSettlement, isTargetingPort, fromMobileParty.IsCurrentlyAtSea, isTargetingPort);
					if (fromMobileParty.IsCurrentlyAtSea == isTargetingPort)
					{
						float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toSettlement, isTargetingPort, (!fromMobileParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval, out estimatedLandRatio);
						num = MathF.Min(num, distance);
					}
				}
			}
		}
		return MBMath.ClampFloat(num, 0f, float.MaxValue);
	}

	public override float GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, out float landRatio)
	{
		Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toMobileParty, customCapability, 100000000f, out var distance, out landRatio);
		return distance;
	}

	public override bool GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, float maxDistance, out float distance, out float landRatio)
	{
		landRatio = customCapability switch
		{
			MobileParty.NavigationType.Naval => 0f, 
			MobileParty.NavigationType.Default => 1f, 
			_ => -0.5f, 
		};
		distance = float.MaxValue;
		if (fromMobileParty.CurrentNavigationFace.FaceIndex == toMobileParty.CurrentNavigationFace.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(fromMobileParty.Position.Face), customCapability))
			{
				distance = fromMobileParty.Position.Distance(toMobileParty.Position);
			}
		}
		else if (customCapability == MobileParty.NavigationType.Default && (fromMobileParty.IsCurrentlyAtSea || toMobileParty.IsCurrentlyAtSea))
		{
			distance = float.MaxValue;
		}
		else if (customCapability == MobileParty.NavigationType.Naval && (!fromMobileParty.IsCurrentlyAtSea || !toMobileParty.IsCurrentlyAtSea))
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

	public override float GetDistance(in CampaignVec2 fromPoint, in CampaignVec2 toPoint, MobileParty.NavigationType customCapability, out float landRatio)
	{
		float num = float.MaxValue;
		landRatio = -1f;
		PathFaceRecord face = toPoint.Face;
		if (fromPoint.Face.FaceIndex == face.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(fromPoint.Face), customCapability))
			{
				num = fromPoint.Distance(toPoint);
				landRatio = customCapability switch
				{
					MobileParty.NavigationType.Naval => 0f, 
					MobileParty.NavigationType.Default => 1f, 
					_ => -0.5f, 
				};
			}
		}
		else
		{
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			(Settlement, bool) closestEntranceToFace = mapDistanceModel.GetClosestEntranceToFace(fromPoint.Face, customCapability);
			(Settlement, bool) closestEntranceToFace2 = mapDistanceModel.GetClosestEntranceToFace(face, customCapability);
			var (settlement, _) = closestEntranceToFace;
			var (settlement2, _) = closestEntranceToFace2;
			if (settlement != null && settlement2 != null)
			{
				bool flag = NavigationHelper.IsPositionValidForNavigationType(toPoint, MobileParty.NavigationType.Naval);
				bool item = closestEntranceToFace.Item2;
				bool item2 = closestEntranceToFace2.Item2;
				CampaignVec2 campaignVec = (item ? settlement.PortPosition : settlement.GatePosition);
				CampaignVec2 v = (item2 ? settlement2.PortPosition : settlement2.GatePosition);
				num = fromPoint.Distance(toPoint) - campaignVec.Distance(v) + GetDistance(settlement, settlement2, item, item2, customCapability);
				if (customCapability == MobileParty.NavigationType.All)
				{
					num += mapDistanceModel.GetTransitionCostAdjustment(settlement, item, settlement2, item2, !fromPoint.IsOnLand, flag);
					if (fromPoint.IsOnLand != flag)
					{
						float distance = mapDistanceModel.GetDistance(in fromPoint, in toPoint, fromPoint.IsOnLand ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval, out landRatio);
						num = MathF.Min(num, distance);
					}
				}
			}
		}
		return MBMath.ClampFloat(num, 0f, float.MaxValue);
	}

	public override float GetDistance(MobileParty fromMobileParty, in CampaignVec2 toPoint, MobileParty.NavigationType navigationType, out float landRatio)
	{
		return base.BaseModel.GetDistance(fromMobileParty, in toPoint, navigationType, out landRatio);
	}

	public override float GetDistance(Settlement fromSettlement, in CampaignVec2 toPoint, bool isFromPort, MobileParty.NavigationType customCapability)
	{
		float num = float.MaxValue;
		CampaignVec2 campaignVec = (isFromPort ? fromSettlement.PortPosition : fromSettlement.GatePosition);
		PathFaceRecord face = toPoint.Face;
		PathFaceRecord face2 = campaignVec.Face;
		if (face2.FaceIndex == face.FaceIndex)
		{
			if (Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face2), customCapability))
			{
				num = campaignVec.Distance(toPoint);
			}
		}
		else
		{
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			(Settlement, bool) closestEntranceToFace = mapDistanceModel.GetClosestEntranceToFace(face, customCapability);
			var (settlement, _) = closestEntranceToFace;
			if (settlement != null)
			{
				bool flag = NavigationHelper.IsPositionValidForNavigationType(toPoint, MobileParty.NavigationType.Naval);
				bool item = closestEntranceToFace.Item2;
				CampaignVec2 campaignVec2 = (isFromPort ? fromSettlement.PortPosition : fromSettlement.GatePosition);
				CampaignVec2 v = (item ? settlement.PortPosition : settlement.GatePosition);
				num = campaignVec2.Distance(toPoint) - campaignVec2.Distance(v) + mapDistanceModel.GetDistance(fromSettlement, settlement, isFromPort, item, customCapability);
				if (customCapability == MobileParty.NavigationType.All)
				{
					num += mapDistanceModel.GetTransitionCostAdjustment(fromSettlement, isFromPort, settlement, item, isFromPort, flag);
					if (isFromPort == flag)
					{
						float distance = mapDistanceModel.GetDistance(fromSettlement, in toPoint, isFromPort, (!isFromPort) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
						num = MathF.Min(num, distance);
					}
				}
			}
		}
		return MBMath.ClampFloat(num, 0f, 100000000f);
	}

	public override bool PathExistBetweenPoints(in CampaignVec2 fromPoint, in CampaignVec2 toPoint, MobileParty.NavigationType navigationType)
	{
		(Settlement, bool) closestEntranceToFace = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(fromPoint.Face, navigationType);
		(Settlement, bool) closestEntranceToFace2 = Campaign.Current.Models.MapDistanceModel.GetClosestEntranceToFace(toPoint.Face, navigationType);
		if (closestEntranceToFace.Item1 != null && closestEntranceToFace2.Item1 != null)
		{
			return Campaign.Current.Models.MapDistanceModel.GetDistance(closestEntranceToFace.Item1, closestEntranceToFace2.Item1, closestEntranceToFace.Item2, closestEntranceToFace2.Item2, navigationType) < Campaign.MapDiagonal * 10f;
		}
		return false;
	}

	public override (Settlement, bool) GetClosestEntranceToFace(PathFaceRecord face, MobileParty.NavigationType navigationCapabilities)
	{
		bool isAtSea;
		return (_navigationCaches[navigationCapabilities].GetClosestSettlementToFaceIndex(face.FaceIndex, out isAtSea), isAtSea);
	}

	public override MBReadOnlyList<Settlement> GetNeighborsOfFortification(Town town, MobileParty.NavigationType navigationCapabilities)
	{
		if (!_navigationCaches.TryGetValue(navigationCapabilities, out var value))
		{
			Debug.FailedAssert("cache not found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCMapDistanceModel.cs", "GetNeighborsOfFortification", 382);
			return new MBReadOnlyList<Settlement>();
		}
		return value.GetNeighbors(town.Settlement);
	}

	public override float GetTransitionCostAdjustment(Settlement settlement1, bool isFromPort, Settlement settlement2, bool isTargetingPort, bool fromIsCurrentlyAtSea, bool toIsCurrentlyAtSea)
	{
		float num = 0f;
		if (isFromPort != fromIsCurrentlyAtSea)
		{
			num -= (float)(isFromPort ? RegionSwitchCostFromSeaToLand : RegionSwitchCostFromLandToSea);
		}
		if (isTargetingPort != toIsCurrentlyAtSea)
		{
			num -= (float)(isTargetingPort ? RegionSwitchCostFromLandToSea : RegionSwitchCostFromSeaToLand);
		}
		if (isFromPort == isTargetingPort && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement1, settlement2, isFromPort, isTargetingPort, (!isFromPort) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval) < Campaign.MapDiagonalSquared)
		{
			num *= -1f;
		}
		return num;
	}
}
