using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000190 RID: 400
	public abstract class MapDistanceModel : MBGameModel<MapDistanceModel>
	{
		// Token: 0x17000701 RID: 1793
		// (get) Token: 0x06001BF8 RID: 7160
		public abstract int RegionSwitchCostFromLandToSea { get; }

		// Token: 0x17000702 RID: 1794
		// (get) Token: 0x06001BF9 RID: 7161
		public abstract int RegionSwitchCostFromSeaToLand { get; }

		// Token: 0x17000703 RID: 1795
		// (get) Token: 0x06001BFA RID: 7162
		public abstract float MaximumSpawnDistanceForCompanionsAfterDisband { get; }

		// Token: 0x06001BFB RID: 7163
		public abstract float GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType navigationType);

		// Token: 0x06001BFC RID: 7164
		public abstract float GetLandRatioOfPathBetweenSettlements(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort);

		// Token: 0x06001BFD RID: 7165
		public abstract float GetDistance(MobileParty fromMobileParty, Settlement toSettlement, bool isTargetingPort, MobileParty.NavigationType customCapability, out float estimatedLandRatio);

		// Token: 0x06001BFE RID: 7166
		public abstract float GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, out float landRatio);

		// Token: 0x06001BFF RID: 7167
		public abstract bool GetDistance(MobileParty fromMobileParty, MobileParty toMobileParty, MobileParty.NavigationType customCapability, float maxDistance, out float distance, out float landRatio);

		// Token: 0x06001C00 RID: 7168
		public abstract float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort, MobileParty.NavigationType navigationCapability);

		// Token: 0x06001C01 RID: 7169
		public abstract float GetDistance(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort, MobileParty.NavigationType navigationCapability, out float landRatio);

		// Token: 0x06001C02 RID: 7170
		public abstract float GetDistance(MobileParty fromMobileParty, in CampaignVec2 toPoint, MobileParty.NavigationType navigationType, out float landRatio);

		// Token: 0x06001C03 RID: 7171
		public abstract float GetDistance(Settlement fromSettlement, in CampaignVec2 toPoint, bool isFromPort, MobileParty.NavigationType navigationType);

		// Token: 0x06001C04 RID: 7172
		public abstract float GetPortToGateDistanceForSettlement(Settlement settlement);

		// Token: 0x06001C05 RID: 7173
		public abstract bool PathExistBetweenPoints(in CampaignVec2 fromPoint, in CampaignVec2 toPoint, MobileParty.NavigationType navigationType);

		// Token: 0x06001C06 RID: 7174
		public abstract void RegisterDistanceCache(MobileParty.NavigationType navigationCapability, MapDistanceModel.INavigationCache cacheToRegister);

		// Token: 0x06001C07 RID: 7175
		public abstract ValueTuple<Settlement, bool> GetClosestEntranceToFace(PathFaceRecord face, MobileParty.NavigationType navigationCapabilities);

		// Token: 0x06001C08 RID: 7176
		public abstract MBReadOnlyList<Settlement> GetNeighborsOfFortification(Town town, MobileParty.NavigationType navigationCapabilities);

		// Token: 0x06001C09 RID: 7177
		public abstract float GetTransitionCostAdjustment(Settlement settlement1, bool isFromPort, Settlement settlement2, bool isTargetingPort, bool fromIsCurrentlyAtSea, bool toIsCurrentlyAtSea);

		// Token: 0x04000923 RID: 2339
		public const float PossibleMaximumMapBoundary = 100000000f;

		// Token: 0x020005F1 RID: 1521
		public interface INavigationCache
		{
			// Token: 0x17000F16 RID: 3862
			// (get) Token: 0x06004F12 RID: 20242
			float MaximumDistanceBetweenTwoConnectedSettlements { get; }

			// Token: 0x06004F13 RID: 20243
			float GetSettlementToSettlementDistanceWithLandRatio(Settlement settlement1, bool isAtSea1, Settlement settlement2, bool isAtSea2, out float landRatio);

			// Token: 0x06004F14 RID: 20244
			MBReadOnlyList<Settlement> GetNeighbors(Settlement settlement);

			// Token: 0x06004F15 RID: 20245
			Settlement GetClosestSettlementToFaceIndex(int faceId, out bool isAtSea);

			// Token: 0x06004F16 RID: 20246
			void FinalizeInitialization();
		}
	}
}
