using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000125 RID: 293
	public class DefaultMapDistanceModel : MapDistanceModel
	{
		// Token: 0x1700066D RID: 1645
		// (get) Token: 0x0600183E RID: 6206 RVA: 0x000742CD File Offset: 0x000724CD
		public override int RegionSwitchCostFromLandToSea
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x1700066E RID: 1646
		// (get) Token: 0x0600183F RID: 6207 RVA: 0x000742D0 File Offset: 0x000724D0
		public override int RegionSwitchCostFromSeaToLand
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x1700066F RID: 1647
		// (get) Token: 0x06001840 RID: 6208 RVA: 0x000742D3 File Offset: 0x000724D3
		public override float MaximumSpawnDistanceForCompanionsAfterDisband
		{
			get
			{
				return 150f;
			}
		}

		// Token: 0x06001842 RID: 6210 RVA: 0x000742E2 File Offset: 0x000724E2
		public override void RegisterDistanceCache(MobileParty.NavigationType navigationCapability, MapDistanceModel.INavigationCache cacheToRegister)
		{
			this._navigationCache = cacheToRegister;
			cacheToRegister.FinalizeInitialization();
		}

		// Token: 0x06001843 RID: 6211 RVA: 0x000742F1 File Offset: 0x000724F1
		public override float GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType navigationCapabilities)
		{
			MapDistanceModel.INavigationCache navigationCache = this._navigationCache;
			if (navigationCache == null)
			{
				return 0f;
			}
			return navigationCache.MaximumDistanceBetweenTwoConnectedSettlements;
		}

		// Token: 0x06001844 RID: 6212 RVA: 0x00074308 File Offset: 0x00072508
		public override float GetLandRatioOfPathBetweenSettlements(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort)
		{
			if (this._navigationCache != null)
			{
				float result;
				this._navigationCache.GetSettlementToSettlementDistanceWithLandRatio(fromSettlement, false, toSettlement, false, out result);
				return result;
			}
			return 1f;
		}

		// Token: 0x06001845 RID: 6213 RVA: 0x00074338 File Offset: 0x00072538
		public override float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort = false, bool isTargetingPort = false, MobileParty.NavigationType navigationCapability = MobileParty.NavigationType.Default)
		{
			float num;
			return this.GetDistance(fromSettlement, toSettlement, isFromPort, isTargetingPort, MobileParty.NavigationType.Default, out num);
		}

		// Token: 0x06001846 RID: 6214 RVA: 0x00074354 File Offset: 0x00072554
		public override float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort, MobileParty.NavigationType navigationCapability, out float landRatio)
		{
			float result = float.MaxValue;
			landRatio = 1f;
			if (fromSettlement != null && toSettlement != null)
			{
				if (fromSettlement != toSettlement)
				{
					return this._navigationCache.GetSettlementToSettlementDistanceWithLandRatio(fromSettlement, isFromPort, toSettlement, isTargetingPort, out landRatio);
				}
				result = 0f;
			}
			return result;
		}

		// Token: 0x06001847 RID: 6215 RVA: 0x00074398 File Offset: 0x00072598
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
					value = fromMobileParty.Position.Distance(toSettlement.GatePosition) - item.GatePosition.Distance(toSettlement.GatePosition) + Campaign.Current.Models.MapDistanceModel.GetDistance(item, toSettlement, false, false, MobileParty.NavigationType.Default);
				}
			}
			return MBMath.ClampFloat(value, 0f, float.MaxValue);
		}

		// Token: 0x06001848 RID: 6216 RVA: 0x000744AC File Offset: 0x000726AC
		public override float GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, out float landRatio)
		{
			float result;
			Campaign.Current.Models.MapDistanceModel.GetDistance(fromMobileParty, toMobileParty, customCapability, 100000000f, out result, out landRatio);
			return result;
		}

		// Token: 0x06001849 RID: 6217 RVA: 0x000744DC File Offset: 0x000726DC
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

		// Token: 0x0600184A RID: 6218 RVA: 0x000745B0 File Offset: 0x000727B0
		public override float GetDistance(MobileParty fromMobileParty, in CampaignVec2 toPoint, MobileParty.NavigationType customCapability, out float landRatio)
		{
			float value = float.MaxValue;
			landRatio = 1f;
			CampaignVec2 campaignVec = toPoint;
			PathFaceRecord face = campaignVec.Face;
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
				ValueTuple<Settlement, bool> closestEntranceToFace = mapDistanceModel.GetClosestEntranceToFace(fromMobileParty.CurrentNavigationFace, MobileParty.NavigationType.Default);
				ref ValueTuple<Settlement, bool> closestEntranceToFace2 = mapDistanceModel.GetClosestEntranceToFace(face, MobileParty.NavigationType.Default);
				Settlement item = closestEntranceToFace.Item1;
				Settlement item2 = closestEntranceToFace2.Item1;
				if (item != null && item2 != null)
				{
					value = fromMobileParty.Position.Distance(toPoint) - item.GatePosition.Distance(item2.GatePosition) + this.GetDistance(item, item2, false, false, MobileParty.NavigationType.Default);
				}
			}
			return MBMath.ClampFloat(value, 0f, float.MaxValue);
		}

		// Token: 0x0600184B RID: 6219 RVA: 0x000746C0 File Offset: 0x000728C0
		public override float GetDistance(Settlement fromSettlement, in CampaignVec2 toPoint, bool isFromPort, MobileParty.NavigationType customCapability)
		{
			float value = float.MaxValue;
			CampaignVec2 campaignVec = (isFromPort ? fromSettlement.PortPosition : fromSettlement.GatePosition);
			CampaignVec2 campaignVec2 = toPoint;
			PathFaceRecord face = campaignVec2.Face;
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
					value = fromSettlement.GatePosition.Distance(toPoint) - fromSettlement.GatePosition.Distance(item.GatePosition) + mapDistanceModel.GetDistance(fromSettlement, item, false, false, MobileParty.NavigationType.Default);
				}
			}
			return MBMath.ClampFloat(value, 0f, 100000000f);
		}

		// Token: 0x0600184C RID: 6220 RVA: 0x000747B2 File Offset: 0x000729B2
		public override float GetPortToGateDistanceForSettlement(Settlement settlement)
		{
			return 100000000f;
		}

		// Token: 0x0600184D RID: 6221 RVA: 0x000747B9 File Offset: 0x000729B9
		public override bool PathExistBetweenPoints(in CampaignVec2 fromPoint, in CampaignVec2 toPoint, MobileParty.NavigationType navigationType)
		{
			return fromPoint.IsOnLand && toPoint.IsOnLand;
		}

		// Token: 0x0600184E RID: 6222 RVA: 0x000747CC File Offset: 0x000729CC
		public override ValueTuple<Settlement, bool> GetClosestEntranceToFace(PathFaceRecord face, MobileParty.NavigationType navigationCapabilities)
		{
			bool item;
			return new ValueTuple<Settlement, bool>(this._navigationCache.GetClosestSettlementToFaceIndex(face.FaceIndex, out item), item);
		}

		// Token: 0x0600184F RID: 6223 RVA: 0x000747F2 File Offset: 0x000729F2
		public override MBReadOnlyList<Settlement> GetNeighborsOfFortification(Town town, MobileParty.NavigationType navigationCapabilities)
		{
			return this._navigationCache.GetNeighbors(town.Settlement);
		}

		// Token: 0x06001850 RID: 6224 RVA: 0x00074805 File Offset: 0x00072A05
		public override float GetTransitionCostAdjustment(Settlement settlement1, bool isFromPort, Settlement settlement2, bool isTargetingPort, bool fromIsCurrentlyAtSea, bool toIsCurrentlyAtSea)
		{
			return 0f;
		}

		// Token: 0x040007E2 RID: 2018
		private MapDistanceModel.INavigationCache _navigationCache;
	}
}
