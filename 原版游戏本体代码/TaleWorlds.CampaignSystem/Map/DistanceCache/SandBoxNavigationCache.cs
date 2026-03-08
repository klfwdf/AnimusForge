using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache
{
	// Token: 0x02000225 RID: 549
	public class SandBoxNavigationCache : NavigationCache<Settlement>, MapDistanceModel.INavigationCache
	{
		// Token: 0x1700080E RID: 2062
		// (get) Token: 0x060020CA RID: 8394 RVA: 0x00090ADF File Offset: 0x0008ECDF
		private IMapScene MapSceneWrapper
		{
			get
			{
				return Campaign.Current.MapSceneWrapper;
			}
		}

		// Token: 0x060020CB RID: 8395 RVA: 0x00090AEC File Offset: 0x0008ECEC
		public SandBoxNavigationCache(MobileParty.NavigationType navigationType)
			: base(navigationType)
		{
			this._excludedFaceIds = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(base._navigationType);
			this._regionSwitchCostTo0 = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea;
			this._regionSwitchCostTo1 = Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand;
		}

		// Token: 0x060020CC RID: 8396 RVA: 0x00090B54 File Offset: 0x0008ED54
		protected override Settlement GetCacheElement(string settlementId)
		{
			return Settlement.Find(settlementId);
		}

		// Token: 0x060020CD RID: 8397 RVA: 0x00090B5C File Offset: 0x0008ED5C
		protected override NavigationCacheElement<Settlement> GetCacheElement(Settlement settlement, bool isPortUsed)
		{
			return new NavigationCacheElement<Settlement>(settlement, isPortUsed);
		}

		// Token: 0x060020CE RID: 8398 RVA: 0x00090B68 File Offset: 0x0008ED68
		float MapDistanceModel.INavigationCache.GetSettlementToSettlementDistanceWithLandRatio(Settlement settlement1, bool isAtSea1, Settlement settlement2, bool isAtSea2, out float landRatio)
		{
			NavigationCacheElement<Settlement> cacheElement = this.GetCacheElement(settlement1, isAtSea1);
			NavigationCacheElement<Settlement> cacheElement2 = this.GetCacheElement(settlement2, isAtSea2);
			return base.GetSettlementToSettlementDistanceWithLandRatio(cacheElement, cacheElement2, out landRatio);
		}

		// Token: 0x060020CF RID: 8399 RVA: 0x00090B92 File Offset: 0x0008ED92
		public override void GetSceneXmlCrcValues(out uint sceneXmlCrc, out uint sceneNavigationMeshCrc)
		{
			sceneXmlCrc = this.MapSceneWrapper.GetSceneXmlCrc();
			sceneNavigationMeshCrc = this.MapSceneWrapper.GetSceneNavigationMeshCrc();
		}

		// Token: 0x060020D0 RID: 8400 RVA: 0x00090BAE File Offset: 0x0008EDAE
		protected override int GetNavMeshFaceCount()
		{
			return this.MapSceneWrapper.GetNumberOfNavigationMeshFaces();
		}

		// Token: 0x060020D1 RID: 8401 RVA: 0x00090BBB File Offset: 0x0008EDBB
		protected override Vec2 GetNavMeshFaceCenterPosition(int faceIndex)
		{
			return this.MapSceneWrapper.GetNavigationMeshCenterPosition(faceIndex);
		}

		// Token: 0x060020D2 RID: 8402 RVA: 0x00090BC9 File Offset: 0x0008EDC9
		protected override PathFaceRecord GetFaceRecordAtIndex(int faceIndex)
		{
			return this.MapSceneWrapper.GetFaceAtIndex(faceIndex);
		}

		// Token: 0x060020D3 RID: 8403 RVA: 0x00090BD7 File Offset: 0x0008EDD7
		protected override int GetRegionSwitchCostTo0()
		{
			return this._regionSwitchCostTo0;
		}

		// Token: 0x060020D4 RID: 8404 RVA: 0x00090BDF File Offset: 0x0008EDDF
		protected override int GetRegionSwitchCostTo1()
		{
			return this._regionSwitchCostTo1;
		}

		// Token: 0x060020D5 RID: 8405 RVA: 0x00090BE7 File Offset: 0x0008EDE7
		protected override int[] GetExcludedFaceIds()
		{
			return this._excludedFaceIds;
		}

		// Token: 0x060020D6 RID: 8406 RVA: 0x00090BF0 File Offset: 0x0008EDF0
		protected override float GetRealDistanceAndLandRatioBetweenSettlements(NavigationCacheElement<Settlement> settlement1, NavigationCacheElement<Settlement> settlement2, out float landRatio)
		{
			landRatio = 1f;
			float distanceLimit = (float)Campaign.PathFindingMaxCostLimit;
			CampaignVec2 campaignVec = (settlement1.IsPortUsed ? settlement1.PortPosition : settlement1.GatePosition);
			CampaignVec2 campaignVec2 = (settlement2.IsPortUsed ? settlement2.PortPosition : settlement2.GatePosition);
			NavigationPath path = new NavigationPath();
			Campaign.Current.MapSceneWrapper.GetPathBetweenAIFaces(campaignVec.Face, campaignVec2.Face, campaignVec.ToVec2(), campaignVec2.ToVec2(), 0.3f, path, this.GetExcludedFaceIds(), 1f, this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1());
			float num;
			Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(campaignVec.Face, campaignVec2.Face, campaignVec.ToVec2(), campaignVec2.ToVec2(), 0.3f, distanceLimit, out num, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1());
			float num2;
			Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(campaignVec2.Face, campaignVec.Face, campaignVec2.ToVec2(), campaignVec.ToVec2(), 0.3f, distanceLimit, out num2, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1());
			float num3 = (num + num2) * 0.5f;
			if (num3 > 0f)
			{
				if (base._navigationType == MobileParty.NavigationType.Naval)
				{
					landRatio = 0f;
				}
				else if (base._navigationType == MobileParty.NavigationType.All)
				{
					landRatio = base.GetLandRatioOfPath(path, campaignVec.ToVec2());
				}
				bool flag;
				NavigationCacheElement<Settlement>.Sort(ref settlement1, ref settlement2, out flag);
				return num3;
			}
			return 0f;
		}

		// Token: 0x060020D7 RID: 8407 RVA: 0x00090D70 File Offset: 0x0008EF70
		protected override void GetFaceRecordForPoint(Vec2 position, out bool isOnRegion1)
		{
			isOnRegion1 = true;
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			CampaignVec2 campaignVec = new CampaignVec2(position, true);
			PathFaceRecord faceIndex = mapSceneWrapper.GetFaceIndex(campaignVec);
			if (!faceIndex.IsValid())
			{
				isOnRegion1 = false;
				IMapScene mapSceneWrapper2 = Campaign.Current.MapSceneWrapper;
				campaignVec = new CampaignVec2(position, false);
				faceIndex = mapSceneWrapper2.GetFaceIndex(campaignVec);
			}
			if (!faceIndex.IsValid())
			{
				Debug.Print(string.Format("{0} has no region data.", position), 0, Debug.DebugColor.Red, 17592186044416UL);
			}
		}

		// Token: 0x060020D8 RID: 8408 RVA: 0x00090DEC File Offset: 0x0008EFEC
		protected override bool CheckBeingNeighbor(List<Settlement> settlementsToConsider, Settlement settlement1, Settlement settlement2, bool useGate1, bool useGate2, out float distance)
		{
			CampaignVec2 campaignVec = (useGate1 ? settlement1.GatePosition : settlement1.PortPosition);
			CampaignVec2 campaignVec2 = (useGate2 ? settlement2.GatePosition : settlement2.PortPosition);
			PathFaceRecord faceIndex = this.MapSceneWrapper.GetFaceIndex(campaignVec);
			PathFaceRecord faceIndex2 = this.MapSceneWrapper.GetFaceIndex(campaignVec2);
			if (!faceIndex.IsValid() || !faceIndex2.IsValid())
			{
				Debug.FailedAssert("Settlement navFace index should not be -1, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Map\\DistanceCache\\SandboxNavigationCache.cs", "CheckBeingNeighbor", 193);
			}
			NavigationPath navigationPath = new NavigationPath();
			this.MapSceneWrapper.GetPathBetweenAIFaces(faceIndex, faceIndex2, campaignVec.ToVec2(), campaignVec2.ToVec2(), 0.3f, navigationPath, this.GetExcludedFaceIds(), 2f, this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1());
			bool flag = navigationPath.Size > 0 || faceIndex.FaceIndex == faceIndex2.FaceIndex;
			bool flag2 = useGate1;
			if (!this.MapSceneWrapper.GetPathDistanceBetweenAIFaces(faceIndex, faceIndex2, campaignVec.ToVec2(), campaignVec2.ToVec2(), 0.3f, Campaign.MapDiagonalSquared, out distance, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1()))
			{
				distance = Campaign.MapDiagonalSquared;
			}
			int num = 0;
			while (num < navigationPath.Size && flag)
			{
				Vec2 v = navigationPath[num] - ((num == 0) ? campaignVec.ToVec2() : navigationPath[num - 1]);
				float num2 = v.Length / 1f;
				v.Normalize();
				int num3 = 0;
				while ((float)num3 < num2)
				{
					Vec2 vec = ((num == 0) ? campaignVec.ToVec2() : navigationPath[num - 1]) + v * 1f * (float)num3;
					if (vec != campaignVec.ToVec2() && vec != campaignVec2.ToVec2())
					{
						CampaignVec2 campaignVec3 = new CampaignVec2(vec, flag2);
						PathFaceRecord faceIndex3 = this.MapSceneWrapper.GetFaceIndex(campaignVec3);
						if (faceIndex3.FaceIndex == -1)
						{
							flag2 = !flag2;
							campaignVec3 = new CampaignVec2(vec, flag2);
							faceIndex3 = this.MapSceneWrapper.GetFaceIndex(campaignVec3);
						}
						bool flag3;
						float realPathDistanceFromPositionToSettlement = this.GetRealPathDistanceFromPositionToSettlement(vec, faceIndex3, distance, settlement1, out flag3);
						float realPathDistanceFromPositionToSettlement2 = this.GetRealPathDistanceFromPositionToSettlement(vec, faceIndex3, distance, settlement2, out flag3);
						float num4 = ((realPathDistanceFromPositionToSettlement < realPathDistanceFromPositionToSettlement2) ? realPathDistanceFromPositionToSettlement : realPathDistanceFromPositionToSettlement2);
						if (faceIndex3.FaceIndex != -1)
						{
							Settlement closestSettlementToPosition = base.GetClosestSettlementToPosition(vec, faceIndex3, this.GetExcludedFaceIds(), settlementsToConsider, this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1(), num4 * 0.8f, out flag3);
							if (closestSettlementToPosition != null && closestSettlementToPosition != settlement1 && closestSettlementToPosition != settlement2)
							{
								flag = false;
								break;
							}
						}
					}
					num3++;
				}
				num++;
			}
			return flag;
		}

		// Token: 0x060020D9 RID: 8409 RVA: 0x0009109C File Offset: 0x0008F29C
		protected override float GetRealPathDistanceFromPositionToSettlement(Vec2 checkPosition, PathFaceRecord currentFaceRecord, float maxDistanceToLookForPathDetection, Settlement currentSettlementToLook, out bool isPort)
		{
			float result = float.MaxValue;
			isPort = false;
			switch (base._navigationType)
			{
			case MobileParty.NavigationType.Default:
			{
				CampaignVec2 campaignVec = currentSettlementToLook.GatePosition;
				PathFaceRecord faceIndex = this.MapSceneWrapper.GetFaceIndex(campaignVec);
				float num;
				if (this.MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.GatePosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out num, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1()))
				{
					result = num;
				}
				break;
			}
			case MobileParty.NavigationType.Naval:
			{
				CampaignVec2 campaignVec = currentSettlementToLook.PortPosition;
				PathFaceRecord faceIndex = this.MapSceneWrapper.GetFaceIndex(campaignVec);
				float num2;
				if (this.MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.PortPosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out num2, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1()))
				{
					result = num2;
					isPort = true;
				}
				break;
			}
			case MobileParty.NavigationType.All:
			{
				CampaignVec2 campaignVec = currentSettlementToLook.GatePosition;
				PathFaceRecord faceIndex = this.MapSceneWrapper.GetFaceIndex(campaignVec);
				float num3;
				if (this.MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.GatePosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out num3, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1()))
				{
					result = num3;
				}
				if (currentSettlementToLook.HasPort)
				{
					campaignVec = currentSettlementToLook.PortPosition;
					faceIndex = this.MapSceneWrapper.GetFaceIndex(campaignVec);
					float num4;
					if (this.MapSceneWrapper.GetPathDistanceBetweenAIFaces(currentFaceRecord, faceIndex, checkPosition, currentSettlementToLook.PortPosition.ToVec2(), 0.3f, maxDistanceToLookForPathDetection, out num4, this.GetExcludedFaceIds(), this.GetRegionSwitchCostTo0(), this.GetRegionSwitchCostTo1()) && num4 < num3)
					{
						result = num4;
						isPort = true;
					}
				}
				break;
			}
			}
			return result;
		}

		// Token: 0x060020DA RID: 8410 RVA: 0x00091248 File Offset: 0x0008F448
		protected override IEnumerable<Settlement> GetClosestSettlementsToPositionInCache(Vec2 checkPosition, List<Settlement> settlements)
		{
			if (base._navigationType == MobileParty.NavigationType.Naval)
			{
				return from x in settlements
					where x.HasPort
					orderby checkPosition.DistanceSquared(x.PortPosition.ToVec2())
					select x;
			}
			return from x in settlements
				orderby checkPosition.DistanceSquared(x.GatePosition.ToVec2())
				select x;
		}

		// Token: 0x060020DB RID: 8411 RVA: 0x000912B4 File Offset: 0x0008F4B4
		protected override List<Settlement> GetAllRegisteredSettlements()
		{
			return Settlement.All.ToList<Settlement>();
		}

		// Token: 0x060020DC RID: 8412 RVA: 0x000912C0 File Offset: 0x0008F4C0
		public void FinalizeInitialization()
		{
			base.FinalizeCacheInitialization();
		}

		// Token: 0x04000994 RID: 2452
		private readonly int[] _excludedFaceIds;

		// Token: 0x04000995 RID: 2453
		private readonly int _regionSwitchCostTo0;

		// Token: 0x04000996 RID: 2454
		private readonly int _regionSwitchCostTo1;
	}
}
