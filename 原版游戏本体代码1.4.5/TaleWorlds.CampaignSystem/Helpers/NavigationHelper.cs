using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers;

public static class NavigationHelper
{
	public class EmbarkDisembarkData
	{
		public static readonly EmbarkDisembarkData Invalid = new EmbarkDisembarkData(isValid: false, CampaignVec2.Invalid, CampaignVec2.Invalid, CampaignVec2.Invalid, isTargetingTheDeadZone: false, isTargetingOwnSideOfTheDeadZone: false);

		public bool IsValidTransition;

		public CampaignVec2 NavMeshEdgePosition;

		public CampaignVec2 TransitionStartPosition;

		public CampaignVec2 TransitionEndPosition;

		public bool IsTargetingTheDeadZone;

		public bool IsTargetingOwnSideOfTheDeadZone;

		public EmbarkDisembarkData(bool isValid, CampaignVec2 navMeshEdgePosition, CampaignVec2 transitionStartPosition, CampaignVec2 transitionEndPosition, bool isTargetingTheDeadZone, bool isTargetingOwnSideOfTheDeadZone)
		{
			IsValidTransition = isValid;
			NavMeshEdgePosition = navMeshEdgePosition;
			TransitionStartPosition = transitionStartPosition;
			TransitionEndPosition = transitionEndPosition;
			IsTargetingTheDeadZone = isTargetingTheDeadZone;
			IsTargetingOwnSideOfTheDeadZone = isTargetingOwnSideOfTheDeadZone;
		}
	}

	public static bool IsPositionValidForNavigationType(CampaignVec2 vec2, MobileParty.NavigationType navigationType)
	{
		if (vec2.IsValid())
		{
			return IsPositionValidForNavigationType(vec2.Face, navigationType);
		}
		return false;
	}

	public static bool IsPositionValidForNavigationType(PathFaceRecord face, MobileParty.NavigationType navigationType)
	{
		bool result = false;
		if (face.IsValid())
		{
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
			result = Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(faceTerrainType, navigationType);
		}
		return result;
	}

	public static bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType)
	{
		return Campaign.Current.Models.PartyNavigationModel.CanPlayerNavigateToPosition(vec2, out navigationType);
	}

	public static CampaignVec2 GetClosestNavMeshFaceCenterPositionForPosition(CampaignVec2 vec2, int[] excludedFaceIds)
	{
		return Campaign.Current.MapSceneWrapper.GetNearestFaceCenterForPosition(in vec2, excludedFaceIds);
	}

	public static EmbarkDisembarkData GetEmbarkDisembarkDataForTick(CampaignVec2 position, Vec2 direction)
	{
		CalculateTransitionStartAndEndPosition(position, direction, out var transitionStartPosition, out var transitionEndPosition, out var originalEdge);
		_ = Campaign.Current.MapSceneWrapper;
		if (transitionEndPosition.IsValid())
		{
			CampaignVec2 transitionStartPosition2 = (transitionStartPosition.IsValid() ? transitionStartPosition : position);
			return new EmbarkDisembarkData(isValid: true, new CampaignVec2(originalEdge, position.IsOnLand), transitionStartPosition2, transitionEndPosition, isTargetingTheDeadZone: false, isTargetingOwnSideOfTheDeadZone: false);
		}
		return EmbarkDisembarkData.Invalid;
	}

	public static EmbarkDisembarkData GetEmbarkAndDisembarkDataForPlayer(CampaignVec2 position, Vec2 direction, CampaignVec2 moveTargetPointOfTheParty, bool isMoveTargetOnLand)
	{
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		EmbarkDisembarkData embarkDisembarkDataForTick = GetEmbarkDisembarkDataForTick(position, direction);
		if (embarkDisembarkDataForTick.IsValidTransition)
		{
			PathFaceRecord face = position.Face;
			PathFaceRecord face2 = embarkDisembarkDataForTick.TransitionStartPosition.Face;
			bool flag = face2.IsValid() && Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(mapSceneWrapper.GetFaceTerrainType(face2), MobileParty.NavigationType.Default);
			PathFaceRecord face3 = embarkDisembarkDataForTick.TransitionEndPosition.Face;
			bool flag2 = face3.IsValid() && Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(mapSceneWrapper.GetFaceTerrainType(face3), MobileParty.NavigationType.Default);
			if (flag == flag2)
			{
				PathFaceRecord face4 = moveTargetPointOfTheParty.Face;
				Vec2 navigationMeshCenterPosition = mapSceneWrapper.GetNavigationMeshCenterPosition(face4);
				direction = moveTargetPointOfTheParty.ToVec2() + navigationMeshCenterPosition;
				direction.Normalize();
				CampaignVec2 position2 = new CampaignVec2(navigationMeshCenterPosition, moveTargetPointOfTheParty.IsOnLand);
				embarkDisembarkDataForTick = GetEmbarkDisembarkDataForTick(position2, direction);
				if (embarkDisembarkDataForTick.IsValidTransition)
				{
					face = position2.Face;
					face2 = embarkDisembarkDataForTick.TransitionStartPosition.Face;
					flag = face2.IsValid() && Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(mapSceneWrapper.GetFaceTerrainType(face2), MobileParty.NavigationType.Default);
					face3 = embarkDisembarkDataForTick.TransitionEndPosition.Face;
					flag2 = face3.IsValid() && Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(mapSceneWrapper.GetFaceTerrainType(face3), MobileParty.NavigationType.Default);
				}
			}
			if (embarkDisembarkDataForTick.IsValidTransition)
			{
				PathFaceRecord face5 = embarkDisembarkDataForTick.TransitionStartPosition.Face;
				Vec2 navigationMeshCenterPosition2 = mapSceneWrapper.GetNavigationMeshCenterPosition(face5);
				Vec2 lastPositionOnNavMeshFaceForPointAndDirection = mapSceneWrapper.GetLastPositionOnNavMeshFaceForPointAndDirection(face, navigationMeshCenterPosition2, moveTargetPointOfTheParty.ToVec2() + navigationMeshCenterPosition2 * 10f);
				float num = moveTargetPointOfTheParty.Distance(lastPositionOnNavMeshFaceForPointAndDirection);
				float num2 = embarkDisembarkDataForTick.TransitionStartPosition.Distance(embarkDisembarkDataForTick.NavMeshEdgePosition);
				if (num < num2)
				{
					embarkDisembarkDataForTick.IsTargetingTheDeadZone = flag != flag2;
					PathFaceRecord face6 = moveTargetPointOfTheParty.Face;
					embarkDisembarkDataForTick.IsTargetingOwnSideOfTheDeadZone = embarkDisembarkDataForTick.IsTargetingTheDeadZone && face6.FaceIndex == face5.FaceIndex;
				}
			}
		}
		return embarkDisembarkDataForTick;
	}

	private static void CalculateTransitionStartAndEndPosition(CampaignVec2 position, Vec2 direction, out CampaignVec2 transitionStartPosition, out CampaignVec2 transitionEndPosition, out Vec2 originalEdge)
	{
		Vec2 vec = direction;
		Vec2 vec2 = direction;
		vec.RotateCCW(0.05f);
		vec2.RotateCCW(-0.05f);
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(position.IsOnLand ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
		PathFaceRecord face = position.Face;
		originalEdge = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(face, position.ToVec2(), position.ToVec2() + direction, invalidTerrainTypesForNavigationType);
		Vec2 lastPointOnNavigationMeshFromPositionToDestination = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(face, position.ToVec2(), position.ToVec2() + vec, invalidTerrainTypesForNavigationType);
		Vec2 vec3 = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(face, position.ToVec2(), position.ToVec2() + vec2, invalidTerrainTypesForNavigationType) - lastPointOnNavigationMeshFromPositionToDestination;
		Vec2 vec4 = vec3.LeftVec();
		Vec2 vec5 = vec3.RightVec();
		vec4.Normalize();
		vec4 *= Campaign.Current.Models.PartyNavigationModel.GetEmbarkDisembarkThresholdDistance();
		vec5.Normalize();
		vec5 *= Campaign.Current.Models.PartyNavigationModel.GetEmbarkDisembarkThresholdDistance();
		transitionStartPosition = new CampaignVec2(vec5 + originalEdge, position.IsOnLand);
		transitionEndPosition = new CampaignVec2(vec4 + originalEdge, position.IsOnLand);
		if (transitionStartPosition.Face.IsValid() && transitionEndPosition.Face.IsValid())
		{
			transitionStartPosition = CampaignVec2.Invalid;
			transitionEndPosition = CampaignVec2.Invalid;
		}
		else
		{
			transitionStartPosition = new CampaignVec2(vec5 + originalEdge, position.IsOnLand);
			transitionEndPosition = new CampaignVec2(vec4 + originalEdge, !position.IsOnLand);
		}
	}

	public static CampaignVec2 FindPointAroundPosition(CampaignVec2 centerPosition, MobileParty.NavigationType navigationCapability, float maxDistance, float minDistance = 0f, bool requirePath = true, bool useUniformDistribution = false)
	{
		PathFaceRecord face = centerPosition.Face;
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationCapability);
		CampaignVec2 result = centerPosition;
		if (maxDistance > 0f)
		{
			Campaign.Current.MapSceneWrapper.GetMapBorders(out var minimumPosition, out var maximumPosition, out var maximumHeight);
			Vec2 vec = new Vec2(TaleWorlds.Library.MathF.Max(centerPosition.X - maxDistance, minimumPosition.x), TaleWorlds.Library.MathF.Max(centerPosition.Y - maxDistance, minimumPosition.y));
			Vec2 vec2 = new Vec2(TaleWorlds.Library.MathF.Min(centerPosition.X + maxDistance, maximumPosition.x), TaleWorlds.Library.MathF.Min(centerPosition.Y + maxDistance, maximumPosition.y));
			maxDistance = TaleWorlds.Library.MathF.Min(vec2.x - vec.x, vec2.y - vec.y) * 0.5f;
			for (int i = 0; i < 250; i++)
			{
				CampaignVec2 campaignVec = FindPointInCircle(centerPosition, minDistance, maxDistance, useUniformDistribution);
				if (!(campaignVec != centerPosition))
				{
					continue;
				}
				PathFaceRecord face2 = campaignVec.Face;
				if (face2.IsValid())
				{
					int regionSwitchCostFromLandToSea = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
					int regionSwitchCostFromSeaToLand = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand;
					if ((!requirePath || Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(face, face2, centerPosition.ToVec2(), campaignVec.ToVec2(), 0.3f, maxDistance, out maximumHeight, invalidTerrainTypesForNavigationType, regionSwitchCostFromLandToSea, regionSwitchCostFromSeaToLand)) && IsPositionValidForNavigationType(campaignVec, navigationCapability))
					{
						result = campaignVec;
						break;
					}
				}
			}
		}
		return result;
	}

	public static CampaignVec2 FindReachablePointAroundPosition(CampaignVec2 center, int[] excludedFaceIds, float maxDistance, float minDistance = 0f, bool useUniformDistribution = false)
	{
		CampaignVec2 result = center;
		if (maxDistance > 0f)
		{
			Campaign.Current.MapSceneWrapper.GetMapBorders(out var minimumPosition, out var maximumPosition, out var maximumHeight);
			Vec2 vec = new Vec2(TaleWorlds.Library.MathF.Max(center.X - maxDistance, minimumPosition.x), TaleWorlds.Library.MathF.Max(center.Y - maxDistance, minimumPosition.y));
			Vec2 vec2 = new Vec2(TaleWorlds.Library.MathF.Min(center.X + maxDistance, maximumPosition.x), TaleWorlds.Library.MathF.Min(center.Y + maxDistance, maximumPosition.y));
			maxDistance = TaleWorlds.Library.MathF.Min(vec2.x - vec.x, vec2.y - vec.y) * 0.5f;
			for (int i = 0; i < 250; i++)
			{
				CampaignVec2 campaignVec = FindPointInCircle(center, minDistance, maxDistance, useUniformDistribution);
				if (campaignVec != center && campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(center.Face, campaignVec.Face, center.ToVec2(), campaignVec.ToVec2(), 0.3f, maxDistance, out maximumHeight, excludedFaceIds, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand))
				{
					result = campaignVec;
					break;
				}
			}
		}
		return result;
	}

	public static CampaignVec2 FindReachablePointAroundPosition(CampaignVec2 center, MobileParty.NavigationType navigationCapability, float maxDistance, float minDistance = 0f, bool useUniformDistribution = false)
	{
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationCapability);
		return FindReachablePointAroundPosition(center, invalidTerrainTypesForNavigationType, maxDistance, minDistance, useUniformDistribution);
	}

	public static CampaignVec2 FindPointInsideArea(Vec2 minBorder, Vec2 maxBorder, MobileParty.NavigationType navigationCapability)
	{
		Campaign.Current.MapSceneWrapper.GetMapBorders(out var _, out var _, out var _);
		CampaignVec2 result = CampaignVec2.Invalid;
		bool flag = false;
		for (int i = 0; i < 250; i++)
		{
			CampaignVec2 campaignVec = new CampaignVec2(new Vec2(MBRandom.RandomFloatRanged(minBorder.x, maxBorder.x), MBRandom.RandomFloatRanged(minBorder.y, maxBorder.y)), isOnLand: true);
			if (IsPositionValidForNavigationType(campaignVec, navigationCapability))
			{
				flag = true;
			}
			if (flag)
			{
				result = campaignVec;
				break;
			}
		}
		return result;
	}

	public static bool IsPointInsideBorders(Vec2 point, Vec2 minBorders, Vec2 maxBorders)
	{
		if (point.x < maxBorders.x && point.y < maxBorders.y && point.x > minBorders.x)
		{
			return point.y > minBorders.y;
		}
		return false;
	}

	public static CampaignVec2 FindPointInsideArea(Vec2 minBorders, Vec2 maxBorders, CampaignVec2 center, MobileParty.NavigationType navigationCapability, float maxDistance, float minDistance = 0f, bool requirePathFromCenter = false)
	{
		Campaign.Current.MapSceneWrapper.GetMapBorders(out var _, out var _, out var maximumHeight);
		CampaignVec2 result = CampaignVec2.Invalid;
		float a = TaleWorlds.Library.MathF.Max(minBorders.x, maxBorders.x);
		float a2 = TaleWorlds.Library.MathF.Min(minBorders.x, maxBorders.x);
		float b = TaleWorlds.Library.MathF.Max(minBorders.y, maxBorders.y);
		float b2 = TaleWorlds.Library.MathF.Min(minBorders.y, maxBorders.y);
		Vec2 v = new Vec2(a2, b2);
		Vec2 v2 = new Vec2(a, b2);
		float b3 = TaleWorlds.Library.MathF.Max(b: center.Distance(new Vec2(a, b)), a: TaleWorlds.Library.MathF.Max(c: center.Distance(new Vec2(a2, b)), a: center.Distance(v), b: center.Distance(v2)));
		maxDistance = TaleWorlds.Library.MathF.Min(maxDistance, b3);
		bool flag = false;
		CampaignVec2 invalid = CampaignVec2.Invalid;
		for (int i = 0; i < 250; i++)
		{
			invalid = FindPointInCircle(center, minDistance, maxDistance, useUniformDistribution: false);
			if (IsPositionValidForNavigationType(invalid, navigationCapability) && IsPointInsideBorders(invalid.ToVec2(), minBorders, maxBorders))
			{
				flag = true;
			}
			if (flag && requirePathFromCenter)
			{
				int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationCapability);
				if (!Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(center.Face, invalid.Face, center.ToVec2(), invalid.ToVec2(), 0.3f, maxDistance, out maximumHeight, invalidTerrainTypesForNavigationType, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand))
				{
					flag = false;
				}
			}
			if (flag)
			{
				result = invalid;
				break;
			}
		}
		if (result.ToVec2() == Vec2.Invalid)
		{
			Debug.FailedAssert("Point should not be invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "FindPointInsideArea", 9855);
			return FindPointInsideArea(minBorders, maxBorders, navigationCapability);
		}
		return result;
	}

	private static CampaignVec2 FindPointInCircle(CampaignVec2 center, float min, float max, bool useUniformDistribution)
	{
		float angleInRadians = MBRandom.RandomFloatRanged(0f, System.MathF.PI * 2f);
		Vec2 vec = Vec2.One.Normalized();
		vec.RotateCCW(angleInRadians);
		if (useUniformDistribution)
		{
			vec *= TaleWorlds.Library.MathF.Sqrt(MBRandom.RandomFloat) * (max - min);
		}
		else
		{
			vec *= MBRandom.RandomFloatRanged(min, max);
		}
		return center + vec;
	}

	public static void GetInteractionDataForMainParty(Settlement settlement, out bool canNavigate, out MobileParty.NavigationType bestNavigationType, out bool isTargetingPort)
	{
		CampaignVec2 vec;
		if (MobileParty.MainParty.IsCurrentlyAtSea && settlement.HasPort)
		{
			isTargetingPort = true;
			vec = settlement.PortPosition;
		}
		else
		{
			isTargetingPort = false;
			vec = settlement.GatePosition;
		}
		canNavigate = CanPlayerNavigateToPosition(vec, out bestNavigationType);
	}
}
