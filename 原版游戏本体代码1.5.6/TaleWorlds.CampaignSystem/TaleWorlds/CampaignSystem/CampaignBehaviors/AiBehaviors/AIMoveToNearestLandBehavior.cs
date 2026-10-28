using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

internal class AIMoveToNearestLandBehavior : CampaignBehaviorBase
{
	private const int MoveToNearestLandMaximumScore = 2;

	private const float RatioThreshold = 0.75f;

	public override void RegisterEvents()
	{
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (!mobileParty.IsCurrentlyAtSea || mobileParty.CurrentSettlement != null)
		{
			return;
		}
		float estimatedSafeSailDuration = Campaign.Current.Models.CampaignShipDamageModel.GetEstimatedSafeSailDuration(mobileParty);
		float num = float.MaxValue;
		Settlement settlement = null;
		if (!mobileParty.HasLandNavigationCapability)
		{
			return;
		}
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.All);
		CampaignVec2 nearestFaceCenterForPositionWithPath = Campaign.Current.MapSceneWrapper.GetNearestFaceCenterForPositionWithPath(mobileParty.CurrentNavigationFace, targetIsLand: true, Campaign.MapDiagonal / 2f, invalidTerrainTypesForNavigationType);
		num = DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(mobileParty, nearestFaceCenterForPositionWithPath, MobileParty.NavigationType.All, out var _);
		if (!(num > 0f) || !(num < Campaign.MapDiagonal))
		{
			return;
		}
		float num2 = (mobileParty.IsLordParty ? Campaign.Current.EstimatedAverageLordPartyNavalSpeed : (mobileParty.IsCaravan ? Campaign.Current.EstimatedAverageCaravanPartyNavalSpeed : (mobileParty.IsBandit ? Campaign.Current.EstimatedAverageBanditPartyNavalSpeed : (mobileParty.IsVillager ? Campaign.Current.EstimatedAverageVillagerPartyNavalSpeed : (Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer * 0.5f)))));
		float num3 = num / num2 / estimatedSafeSailDuration;
		if (num3 > 0.75f)
		{
			float num4 = 2f * num3;
			if (settlement != null && mobileParty.DefaultBehavior == AiBehavior.MoveToNearestLandOrPort && mobileParty.TargetSettlement == settlement)
			{
				num4 *= 1.2f;
			}
			p.AddBehaviorScore((new AIBehaviorData(settlement, AiBehavior.MoveToNearestLandOrPort, MobileParty.NavigationType.All, willGatherArmy: false, isFromPort: false, isTargetingPort: false), num4));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
