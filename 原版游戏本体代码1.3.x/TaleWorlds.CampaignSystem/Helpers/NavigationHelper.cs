using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x02000025 RID: 37
	public static class NavigationHelper
	{
		// Token: 0x06000163 RID: 355 RVA: 0x0000FF1B File Offset: 0x0000E11B
		public static bool IsPositionValidForNavigationType(CampaignVec2 vec2, MobileParty.NavigationType navigationType)
		{
			return vec2.IsValid() && NavigationHelper.IsPositionValidForNavigationType(vec2.Face, navigationType);
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000FF38 File Offset: 0x0000E138
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

		// Token: 0x06000165 RID: 357 RVA: 0x0000FF79 File Offset: 0x0000E179
		public static bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType)
		{
			return Campaign.Current.Models.PartyNavigationModel.CanPlayerNavigateToPosition(vec2, out navigationType);
		}

		// Token: 0x06000166 RID: 358 RVA: 0x0000FF91 File Offset: 0x0000E191
		public static CampaignVec2 GetClosestNavMeshFaceCenterPositionForPosition(CampaignVec2 vec2, int[] excludedFaceIds)
		{
			return Campaign.Current.MapSceneWrapper.GetNearestFaceCenterForPosition(vec2, excludedFaceIds);
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000FFA8 File Offset: 0x0000E1A8
		public static NavigationHelper.EmbarkDisembarkData GetEmbarkDisembarkDataForTick(CampaignVec2 position, Vec2 direction)
		{
			CampaignVec2 campaignVec;
			CampaignVec2 transitionEndPosition;
			Vec2 pos;
			NavigationHelper.CalculateTransitionStartAndEndPosition(position, direction, out campaignVec, out transitionEndPosition, out pos);
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			if (transitionEndPosition.IsValid())
			{
				CampaignVec2 transitionStartPosition = (campaignVec.IsValid() ? campaignVec : position);
				return new NavigationHelper.EmbarkDisembarkData(true, new CampaignVec2(pos, position.IsOnLand), transitionStartPosition, transitionEndPosition, false, false);
			}
			return NavigationHelper.EmbarkDisembarkData.Invalid;
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00010000 File Offset: 0x0000E200
		public static NavigationHelper.EmbarkDisembarkData GetEmbarkAndDisembarkDataForPlayer(CampaignVec2 position, Vec2 direction, CampaignVec2 moveTargetPointOfTheParty, bool isMoveTargetOnLand)
		{
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			NavigationHelper.EmbarkDisembarkData embarkDisembarkDataForTick = NavigationHelper.GetEmbarkDisembarkDataForTick(position, direction);
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
					embarkDisembarkDataForTick = NavigationHelper.GetEmbarkDisembarkDataForTick(position2, direction);
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

		// Token: 0x06000169 RID: 361 RVA: 0x0001020C File Offset: 0x0000E40C
		private static void CalculateTransitionStartAndEndPosition(CampaignVec2 position, Vec2 direction, out CampaignVec2 transitionStartPosition, out CampaignVec2 transitionEndPosition, out Vec2 originalEdge)
		{
			Vec2 v = direction;
			Vec2 v2 = direction;
			v.RotateCCW(0.05f);
			v2.RotateCCW(-0.05f);
			int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(position.IsOnLand ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
			PathFaceRecord face = position.Face;
			originalEdge = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(face, position.ToVec2(), position.ToVec2() + direction, invalidTerrainTypesForNavigationType);
			Vec2 lastPointOnNavigationMeshFromPositionToDestination = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(face, position.ToVec2(), position.ToVec2() + v, invalidTerrainTypesForNavigationType);
			Vec2 vec = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(face, position.ToVec2(), position.ToVec2() + v2, invalidTerrainTypesForNavigationType) - lastPointOnNavigationMeshFromPositionToDestination;
			Vec2 vec2 = vec.LeftVec();
			Vec2 vec3 = vec.RightVec();
			vec2.Normalize();
			vec2 *= Campaign.Current.Models.PartyNavigationModel.GetEmbarkDisembarkThresholdDistance();
			vec3.Normalize();
			vec3 *= Campaign.Current.Models.PartyNavigationModel.GetEmbarkDisembarkThresholdDistance();
			transitionStartPosition = new CampaignVec2(vec3 + originalEdge, position.IsOnLand);
			transitionEndPosition = new CampaignVec2(vec2 + originalEdge, position.IsOnLand);
			if (transitionStartPosition.Face.IsValid() && transitionEndPosition.Face.IsValid())
			{
				transitionStartPosition = CampaignVec2.Invalid;
				transitionEndPosition = CampaignVec2.Invalid;
				return;
			}
			transitionStartPosition = new CampaignVec2(vec3 + originalEdge, position.IsOnLand);
			transitionEndPosition = new CampaignVec2(vec2 + originalEdge, !position.IsOnLand);
		}

		// Token: 0x0600016A RID: 362 RVA: 0x000103FC File Offset: 0x0000E5FC
		public static CampaignVec2 FindPointAroundPosition(CampaignVec2 centerPosition, MobileParty.NavigationType navigationCapability, float maxDistance, float minDistance = 0f, bool requirePath = true, bool useUniformDistribution = false)
		{
			PathFaceRecord face = centerPosition.Face;
			int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationCapability);
			CampaignVec2 result = centerPosition;
			if (maxDistance > 0f)
			{
				Vec2 vec;
				Vec2 vec2;
				float num;
				Campaign.Current.MapSceneWrapper.GetMapBorders(out vec, out vec2, out num);
				Vec2 vec3 = new Vec2(MathF.Max(centerPosition.X - maxDistance, vec.x), MathF.Max(centerPosition.Y - maxDistance, vec.y));
				Vec2 vec4 = new Vec2(MathF.Min(centerPosition.X + maxDistance, vec2.x), MathF.Min(centerPosition.Y + maxDistance, vec2.y));
				maxDistance = MathF.Min(vec4.x - vec3.x, vec4.y - vec3.y) * 0.5f;
				for (int i = 0; i < 250; i++)
				{
					CampaignVec2 campaignVec = NavigationHelper.FindPointInCircle(centerPosition, minDistance, maxDistance, useUniformDistribution);
					if (campaignVec != centerPosition)
					{
						PathFaceRecord face2 = campaignVec.Face;
						if (face2.IsValid())
						{
							int regionSwitchCostFromLandToSea = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
							int regionSwitchCostFromSeaToLand = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand;
							if ((!requirePath || Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(face, face2, centerPosition.ToVec2(), campaignVec.ToVec2(), 0.3f, maxDistance, out num, invalidTerrainTypesForNavigationType, regionSwitchCostFromLandToSea, regionSwitchCostFromSeaToLand)) && NavigationHelper.IsPositionValidForNavigationType(campaignVec, navigationCapability))
							{
								result = campaignVec;
								break;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0001058C File Offset: 0x0000E78C
		public static CampaignVec2 FindReachablePointAroundPosition(CampaignVec2 center, int[] excludedFaceIds, float maxDistance, float minDistance = 0f, bool useUniformDistribution = false)
		{
			CampaignVec2 result = center;
			if (maxDistance > 0f)
			{
				Vec2 vec;
				Vec2 vec2;
				float num;
				Campaign.Current.MapSceneWrapper.GetMapBorders(out vec, out vec2, out num);
				Vec2 vec3 = new Vec2(MathF.Max(center.X - maxDistance, vec.x), MathF.Max(center.Y - maxDistance, vec.y));
				Vec2 vec4 = new Vec2(MathF.Min(center.X + maxDistance, vec2.x), MathF.Min(center.Y + maxDistance, vec2.y));
				maxDistance = MathF.Min(vec4.x - vec3.x, vec4.y - vec3.y) * 0.5f;
				for (int i = 0; i < 250; i++)
				{
					CampaignVec2 campaignVec = NavigationHelper.FindPointInCircle(center, minDistance, maxDistance, useUniformDistribution);
					if (campaignVec != center && campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(center.Face, campaignVec.Face, center.ToVec2(), campaignVec.ToVec2(), 0.3f, maxDistance, out num, excludedFaceIds, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand))
					{
						result = campaignVec;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x000106E8 File Offset: 0x0000E8E8
		public static CampaignVec2 FindReachablePointAroundPosition(CampaignVec2 center, MobileParty.NavigationType navigationCapability, float maxDistance, float minDistance = 0f, bool useUniformDistribution = false)
		{
			int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationCapability);
			return NavigationHelper.FindReachablePointAroundPosition(center, invalidTerrainTypesForNavigationType, maxDistance, minDistance, useUniformDistribution);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00010718 File Offset: 0x0000E918
		public static CampaignVec2 FindPointInsideArea(Vec2 minBorder, Vec2 maxBorder, MobileParty.NavigationType navigationCapability)
		{
			Vec2 vec;
			Vec2 vec2;
			float num;
			Campaign.Current.MapSceneWrapper.GetMapBorders(out vec, out vec2, out num);
			CampaignVec2 result = CampaignVec2.Invalid;
			bool flag = false;
			for (int i = 0; i < 250; i++)
			{
				CampaignVec2 campaignVec = new CampaignVec2(new Vec2(MBRandom.RandomFloatRanged(minBorder.x, maxBorder.x), MBRandom.RandomFloatRanged(minBorder.y, maxBorder.y)), true);
				if (NavigationHelper.IsPositionValidForNavigationType(campaignVec, navigationCapability))
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

		// Token: 0x0600016E RID: 366 RVA: 0x0001079A File Offset: 0x0000E99A
		public static bool IsPointInsideBorders(Vec2 point, Vec2 minBorders, Vec2 maxBorders)
		{
			return point.x < maxBorders.x && point.y < maxBorders.y && point.x > minBorders.x && point.y > minBorders.y;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x000107D8 File Offset: 0x0000E9D8
		public static CampaignVec2 FindPointInsideArea(Vec2 minBorders, Vec2 maxBorders, CampaignVec2 center, MobileParty.NavigationType navigationCapability, float maxDistance, float minDistance = 0f, bool requirePathFromCenter = false)
		{
			Vec2 vec;
			Vec2 vec2;
			float num;
			Campaign.Current.MapSceneWrapper.GetMapBorders(out vec, out vec2, out num);
			CampaignVec2 result = CampaignVec2.Invalid;
			float a = MathF.Max(minBorders.x, maxBorders.x);
			float a2 = MathF.Min(minBorders.x, maxBorders.x);
			float b = MathF.Max(minBorders.y, maxBorders.y);
			float b2 = MathF.Min(minBorders.y, maxBorders.y);
			Vec2 v = new Vec2(a2, b2);
			Vec2 v2 = new Vec2(a, b2);
			Vec2 v3 = new Vec2(a, b);
			Vec2 v4 = new Vec2(a2, b);
			float b3 = MathF.Max(MathF.Max(center.Distance(v), center.Distance(v2), center.Distance(v4)), center.Distance(v3));
			maxDistance = MathF.Min(maxDistance, b3);
			bool flag = false;
			CampaignVec2 campaignVec = CampaignVec2.Invalid;
			for (int i = 0; i < 250; i++)
			{
				campaignVec = NavigationHelper.FindPointInCircle(center, minDistance, maxDistance, false);
				if (NavigationHelper.IsPositionValidForNavigationType(campaignVec, navigationCapability) && NavigationHelper.IsPointInsideBorders(campaignVec.ToVec2(), minBorders, maxBorders))
				{
					flag = true;
				}
				if (flag && requirePathFromCenter)
				{
					int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationCapability);
					if (!Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(center.Face, campaignVec.Face, center.ToVec2(), campaignVec.ToVec2(), 0.3f, maxDistance, out num, invalidTerrainTypesForNavigationType, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand))
					{
						flag = false;
					}
				}
				if (flag)
				{
					result = campaignVec;
					break;
				}
			}
			if (result.ToVec2() == Vec2.Invalid)
			{
				Debug.FailedAssert("Point should not be invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "FindPointInsideArea", 9751);
				return NavigationHelper.FindPointInsideArea(minBorders, maxBorders, navigationCapability);
			}
			return result;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x000109C0 File Offset: 0x0000EBC0
		private static CampaignVec2 FindPointInCircle(CampaignVec2 center, float min, float max, bool useUniformDistribution)
		{
			float angleInRadians = MBRandom.RandomFloatRanged(0f, 6.2831855f);
			Vec2 vec = Vec2.One.Normalized();
			vec.RotateCCW(angleInRadians);
			if (useUniformDistribution)
			{
				vec *= MathF.Sqrt(MBRandom.RandomFloat) * (max - min);
			}
			else
			{
				vec *= MBRandom.RandomFloatRanged(min, max);
			}
			return center + vec;
		}

		// Token: 0x020004EE RID: 1262
		public class EmbarkDisembarkData
		{
			// Token: 0x06004AC0 RID: 19136 RVA: 0x00176E0E File Offset: 0x0017500E
			public EmbarkDisembarkData(bool isValid, CampaignVec2 navMeshEdgePosition, CampaignVec2 transitionStartPosition, CampaignVec2 transitionEndPosition, bool isTargetingTheDeadZone, bool isTargetingOwnSideOfTheDeadZone)
			{
				this.IsValidTransition = isValid;
				this.NavMeshEdgePosition = navMeshEdgePosition;
				this.TransitionStartPosition = transitionStartPosition;
				this.TransitionEndPosition = transitionEndPosition;
				this.IsTargetingTheDeadZone = isTargetingTheDeadZone;
				this.IsTargetingOwnSideOfTheDeadZone = isTargetingOwnSideOfTheDeadZone;
			}

			// Token: 0x040014F3 RID: 5363
			public static readonly NavigationHelper.EmbarkDisembarkData Invalid = new NavigationHelper.EmbarkDisembarkData(false, CampaignVec2.Invalid, CampaignVec2.Invalid, CampaignVec2.Invalid, false, false);

			// Token: 0x040014F4 RID: 5364
			public bool IsValidTransition;

			// Token: 0x040014F5 RID: 5365
			public CampaignVec2 NavMeshEdgePosition;

			// Token: 0x040014F6 RID: 5366
			public CampaignVec2 TransitionStartPosition;

			// Token: 0x040014F7 RID: 5367
			public CampaignVec2 TransitionEndPosition;

			// Token: 0x040014F8 RID: 5368
			public bool IsTargetingTheDeadZone;

			// Token: 0x040014F9 RID: 5369
			public bool IsTargetingOwnSideOfTheDeadZone;
		}
	}
}
