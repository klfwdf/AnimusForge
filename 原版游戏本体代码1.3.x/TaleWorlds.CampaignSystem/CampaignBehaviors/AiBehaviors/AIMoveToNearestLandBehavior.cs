using System;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors
{
	// Token: 0x0200046F RID: 1135
	internal class AIMoveToNearestLandBehavior : CampaignBehaviorBase
	{
		// Token: 0x060047D0 RID: 18384 RVA: 0x001694EB File Offset: 0x001676EB
		public override void RegisterEvents()
		{
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
		}

		// Token: 0x060047D1 RID: 18385 RVA: 0x00169504 File Offset: 0x00167704
		private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty.IsCurrentlyAtSea && mobileParty.CurrentSettlement == null)
			{
				float estimatedSafeSailDuration = Campaign.Current.Models.CampaignShipDamageModel.GetEstimatedSafeSailDuration(mobileParty);
				Settlement settlement = null;
				if (mobileParty.HasLandNavigationCapability)
				{
					int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.All);
					CampaignVec2 nearestFaceCenterForPositionWithPath = Campaign.Current.MapSceneWrapper.GetNearestFaceCenterForPositionWithPath(mobileParty.CurrentNavigationFace, true, Campaign.MapDiagonal / 2f, invalidTerrainTypesForNavigationType);
					float num2;
					float num = DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(mobileParty, nearestFaceCenterForPositionWithPath, MobileParty.NavigationType.All, out num2);
					if (num > 0f && num < Campaign.MapDiagonal)
					{
						float num3 = (mobileParty.IsLordParty ? Campaign.Current.EstimatedAverageLordPartyNavalSpeed : (mobileParty.IsCaravan ? Campaign.Current.EstimatedAverageCaravanPartyNavalSpeed : (mobileParty.IsBandit ? Campaign.Current.EstimatedAverageBanditPartyNavalSpeed : (mobileParty.IsVillager ? Campaign.Current.EstimatedAverageVillagerPartyNavalSpeed : (Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer * 0.5f)))));
						float num4 = num / num3 / estimatedSafeSailDuration;
						if (num4 > 0.75f)
						{
							float num5 = 2f * num4;
							if (settlement != null && mobileParty.DefaultBehavior == AiBehavior.MoveToNearestLandOrPort && mobileParty.TargetSettlement == settlement)
							{
								num5 *= 1.2f;
							}
							ValueTuple<AIBehaviorData, float> valueTuple = new ValueTuple<AIBehaviorData, float>(new AIBehaviorData(settlement, AiBehavior.MoveToNearestLandOrPort, MobileParty.NavigationType.All, false, false, false), num5);
							p.AddBehaviorScore(valueTuple);
						}
					}
				}
			}
		}

		// Token: 0x060047D2 RID: 18386 RVA: 0x00169666 File Offset: 0x00167866
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x040013D2 RID: 5074
		private const int MoveToNearestLandMaximumScore = 2;

		// Token: 0x040013D3 RID: 5075
		private const float RatioThreshold = 0.75f;
	}
}
