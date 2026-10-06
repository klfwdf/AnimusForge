using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache;

public class SandBoxNavigationCache : NavigationCache<Settlement>, MapDistanceModel.INavigationCache
{
	private readonly int[] _excludedFaceIds;

	private readonly int _regionSwitchCostTo0;

	private readonly int _regionSwitchCostTo1;

	private IMapScene MapSceneWrapper => Campaign.Current.MapSceneWrapper;

	public SandBoxNavigationCache(MobileParty.NavigationType navigationType)
		: base(navigationType)
	{
		_excludedFaceIds = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(base._navigationType);
		_regionSwitchCostTo0 = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
		_regionSwitchCostTo1 = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand;
	}

	protected override Settlement GetCacheElement(string settlementId)
	{
		return Settlement.Find(settlementId);
	}

	protected override NavigationCacheElement<Settlement> GetCacheElement(Settlement settlement, bool isPortUsed)
	{
		return new NavigationCacheElement<Settlement>(settlement, isPortUsed);
	}

	float MapDistanceModel.INavigationCache.GetSettlementToSettlementDistanceWithLandRatio(Settlement settlement1, bool isAtSea1, Settlement settlement2, bool isAtSea2, out float landRatio)
	{
		NavigationCacheElement<Settlement> cacheElement = GetCacheElement(settlement1, isAtSea1);
		NavigationCacheElement<Settlement> cacheElement2 = GetCacheElement(settlement2, isAtSea2);
		return GetSettlementToSettlementDistanceWithLandRatio(cacheElement, cacheElement2, out landRatio);
	}

	public override void GetSceneXmlCrcValues(out uint sceneXmlCrc, out uint sceneNavigationMeshCrc)
	{
		sceneXmlCrc = MapSceneWrapper.GetSceneXmlCrc();
		sceneNavigationMeshCrc = MapSceneWrapper.GetSceneNavigationMeshCrc();
	}

	protected override int GetNavMeshFaceCount()
	{
		return MapSceneWrapper.GetNumberOfNavigationMeshFaces();
	}

	protected override Vec2 GetNavMeshFaceCenterPosition(int faceIndex)
	{
		return MapSceneWrapper.GetNavigationMeshCenterPosition(faceIndex);
	}

	protected override PathFaceRecord GetFaceRecordAtIndex(int faceIndex)
	{
		return MapSceneWrapper.GetFaceAtIndex(faceIndex);
	}

	protected override int GetRegionSwitchCostTo0()
	{
		return _regionSwitchCostTo0;
	}

	protected override int GetRegionSwitchCostTo1()
	{
		return _regionSwitchCostTo1;
	}

	protected override int[] GetExcludedFaceIds()
	{
		return _excludedFaceIds;
	}

	protected override float GetRealDistanceAndLandRatioBetweenSettlements(NavigationCacheElement<Settlement> settlement1, NavigationCacheElement<Settlement> settlement2, out float landRatio)
	{
		landRatio = 1f;
		float distanceLimit = Campaign.PathFindingMaxCostLimit;
		CampaignVec2 campaignVec = (settlement1.IsPortUsed ? settlement1.PortPosition : settlement1.GatePosition);
		CampaignVec2 campaignVec2 = (settlement2.IsPortUsed ? settlement2.PortPosition : settlement2.GatePosition);
		NavigationPath path = new NavigationPath();
		Campaign.Current.MapSceneWrapper.GetPathBetweenAIFaces(campaignVec.Face, campaignVec2.Face, campaignVec.ToVec2(), campaignVec2.ToVec2(), 0.3f, path, GetExcludedFaceIds(), 1f, GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1());
		Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(campaignVec.Face, campaignVec2.Face, campaignVec.ToVec2(), campaignVec2.ToVec2(), 0.3f, distanceLimit, out var distance, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1());
		Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(campaignVec2.Face, campaignVec.Face, campaignVec2.ToVec2(), campaignVec.ToVec2(), 0.3f, distanceLimit, out var distance2, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1());
		float num = (distance + distance2) * 0.5f;
		if (num > 0f)
		{
			if (base._navigationType == MobileParty.NavigationType.Naval)
			{
				landRatio = 0f;
			}
			else if (base._navigationType == MobileParty.NavigationType.All)
			{
				landRatio = GetLandRatioOfPath(path, campaignVec.ToVec2());
			}
			NavigationCacheElement<Settlement>.Sort(ref settlement1, ref settlement2, out var _);
			return num;
		}
		return 0f;
	}

	protected override void GetFaceRecordForPoint(Vec2 position, out bool isOnRegion1)
	{
		isOnRegion1 = true;
		PathFaceRecord faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(position, isOnLand: true));
		if (!faceIndex.IsValid())
		{
			isOnRegion1 = false;
			faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(position, isOnLand: false));
		}
		if (!faceIndex.IsValid())
		{
			Debug.Print($"{position} has no region data.", 0, Debug.DebugColor.Red);
		}
	}

	protected override bool CheckBeingNeighbor(List<Settlement> settlementsToConsider, Settlement settlement1, Settlement settlement2, bool useGate1, bool useGate2, out float distance)
	{
		CampaignVec2 vec = (useGate1 ? settlement1.GatePosition : settlement1.PortPosition);
		CampaignVec2 vec2 = (useGate2 ? settlement2.GatePosition : settlement2.PortPosition);
		PathFaceRecord faceIndex = MapSceneWrapper.GetFaceIndex(in vec);
		PathFaceRecord faceIndex2 = MapSceneWrapper.GetFaceIndex(in vec2);
		if (!faceIndex.IsValid() || !faceIndex2.IsValid())
		{
			Debug.FailedAssert("Settlement navFace index should not be -1, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\DistanceCache\\SandboxNavigationCache.cs", "CheckBeingNeighbor", 193);
		}
		NavigationPath navigationPath = new NavigationPath();
		MapSceneWrapper.GetPathBetweenAIFaces(faceIndex, faceIndex2, vec.ToVec2(), vec2.ToVec2(), 0.3f, navigationPath, GetExcludedFaceIds(), 2f, GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1());
		bool flag = navigationPath.Size > 0 || faceIndex.FaceIndex == faceIndex2.FaceIndex;
		bool flag2 = useGate1;
		if (!MapSceneWrapper.GetPathDistanceBetweenAIFaces(faceIndex, faceIndex2, vec.ToVec2(), vec2.ToVec2(), 0.3f, Campaign.MapDiagonalSquared, out distance, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1()))
		{
			distance = Campaign.MapDiagonalSquared;
		}
		for (int i = 0; i < navigationPath.Size && flag; i++)
		{
			Vec2 vec3 = navigationPath[i] - ((i == 0) ? vec.ToVec2() : navigationPath[i - 1]);
			float num = vec3.Length / 1f;
			vec3.Normalize();
			for (int j = 0; (float)j < num; j++)
			{
				Vec2 vec4 = ((i == 0) ? vec.ToVec2() : navigationPath[i - 1]) + vec3 * 1f * j;
				if (!(vec4 != vec.ToVec2()) || !(vec4 != vec2.ToVec2()))
				{
					continue;
				}
				CampaignVec2 vec5 = new CampaignVec2(vec4, flag2);
				PathFaceRecord faceIndex3 = MapSceneWrapper.GetFaceIndex(in vec5);
				if (faceIndex3.FaceIndex == -1)
				{
					flag2 = !flag2;
					vec5 = new CampaignVec2(vec4, flag2);
					faceIndex3 = MapSceneWrapper.GetFaceIndex(in vec5);
				}
				bool isPort;
				float realPathDistanceFromPositionToSettlement = GetRealPathDistanceFromPositionToSettlement(vec4, faceIndex3, distance, settlement1, out isPort);
				float realPathDistanceFromPositionToSettlement2 = GetRealPathDistanceFromPositionToSettlement(vec4, faceIndex3, distance, settlement2, out isPort);
				float num2 = ((realPathDistanceFromPositionToSettlement < realPathDistanceFromPositionToSettlement2) ? realPathDistanceFromPositionToSettlement : realPathDistanceFromPositionToSettlement2);
				if (faceIndex3.FaceIndex != -1)
				{
					Settlement closestSettlementToPosition = GetClosestSettlementToPosition(vec4, faceIndex3, GetExcludedFaceIds(), settlementsToConsider, GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1(), num2 * 0.8f, out isPort);
					if (closestSettlementToPosition != null && closestSettlementToPosition != settlement1 && closestSettlementToPosition != settlement2)
					{
						flag = false;
						break;
					}
				}
			}
		}
		return flag;
	}

	protected override float GetRealPathDistanceFromPositionToSettlement(Vec2 checkPosition, PathFaceRecord currentFaceRecord, float maxDistanceToLookForPathDetection, Settlement currentSettlementToLook, out bool isPort)
	{
		float result = float.MaxValue;
		isPort = false;
		switch (base._navigationType)
		{
		case MobileParty.NavigationType.Default:
		{
			CampaignVec2 vec = currentSettlementToLook.GatePosition;
			PathFaceRecord faceIndex = MapSceneWrapper.GetFaceIndex(in vec);
			if (MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.GatePosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out var distance4, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1()))
			{
				result = distance4;
			}
			break;
		}
		case MobileParty.NavigationType.Naval:
		{
			CampaignVec2 vec = currentSettlementToLook.PortPosition;
			PathFaceRecord faceIndex = MapSceneWrapper.GetFaceIndex(in vec);
			if (MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.PortPosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out var distance3, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1()))
			{
				result = distance3;
				isPort = true;
			}
			break;
		}
		case MobileParty.NavigationType.All:
		{
			CampaignVec2 vec = currentSettlementToLook.GatePosition;
			PathFaceRecord faceIndex = MapSceneWrapper.GetFaceIndex(in vec);
			if (MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.GatePosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out var distance, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1()))
			{
				result = distance;
			}
			if (currentSettlementToLook.HasPort)
			{
				vec = currentSettlementToLook.PortPosition;
				faceIndex = MapSceneWrapper.GetFaceIndex(in vec);
				if (MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.PortPosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out var distance2, GetExcludedFaceIds(), GetRegionSwitchCostTo0(), GetRegionSwitchCostTo1()) && distance2 < distance)
				{
					result = distance2;
					isPort = true;
				}
			}
			break;
		}
		}
		return result;
	}

	protected override IEnumerable<Settlement> GetClosestSettlementsToPositionInCache(Vec2 checkPosition, List<Settlement> settlements)
	{
		if (base._navigationType == MobileParty.NavigationType.Naval)
		{
			return from x in settlements
				where x.HasPort
				orderby checkPosition.DistanceSquared(x.PortPosition.ToVec2())
				select x;
		}
		return settlements.OrderBy((Settlement x) => checkPosition.DistanceSquared(x.GatePosition.ToVec2()));
	}

	protected override List<Settlement> GetAllRegisteredSettlements()
	{
		return Settlement.All.ToList();
	}

	public void FinalizeInitialization()
	{
		FinalizeCacheInitialization();
	}
}
