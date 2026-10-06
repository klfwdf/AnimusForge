using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace Helpers;

public static class AiHelper
{
	public static void GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(MobileParty mobileParty, Settlement settlement, bool isTargetingPort, out MobileParty.NavigationType bestNavigationType, out float bestNavigationDistance, out bool isFromPort)
	{
		bestNavigationType = MobileParty.NavigationType.None;
		bestNavigationDistance = float.MaxValue;
		isFromPort = false;
		float landRatio = -1f;
		if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement == settlement)
		{
			bestNavigationDistance = 0f;
			bestNavigationType = ((!isTargetingPort) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
			return;
		}
		float num = float.MaxValue;
		if (mobileParty.HasLandNavigationCapability && !isTargetingPort)
		{
			num = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, settlement, MobileParty.NavigationType.Default, out landRatio);
		}
		if (num < Campaign.MapDiagonal * 5f && !isTargetingPort)
		{
			bestNavigationType = MobileParty.NavigationType.Default;
			bestNavigationDistance = num;
		}
		if (!mobileParty.HasNavalNavigationCapability)
		{
			return;
		}
		float num2 = float.MaxValue;
		if (isTargetingPort)
		{
			num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, settlement, MobileParty.NavigationType.Naval, out landRatio);
		}
		if (num2 < Campaign.MapDiagonal * 5f)
		{
			num2 *= CalculateShipDistanceAmplifier(mobileParty, num2);
			if (num2 < num && isTargetingPort)
			{
				bestNavigationType = MobileParty.NavigationType.Naval;
				bestNavigationDistance = num2;
				isFromPort = mobileParty.CurrentSettlement != null;
			}
		}
		if (!mobileParty.HasLandNavigationCapability)
		{
			return;
		}
		float num3 = float.MaxValue;
		bool flag = false;
		if (mobileParty.CurrentSettlement != null)
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, settlement, isFromPort: false, isTargetingPort, MobileParty.NavigationType.All, out landRatio);
			if (distance < Campaign.MapDiagonal * 5f)
			{
				float num4 = distance * landRatio;
				float num5 = distance - num4;
				num5 *= CalculateShipDistanceAmplifier(mobileParty, num5);
				num3 = num5 + num4;
			}
			if (mobileParty.CurrentSettlement.HasPort)
			{
				float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, settlement, isFromPort: true, isTargetingPort, MobileParty.NavigationType.All, out landRatio);
				if (distance2 < Campaign.MapDiagonal * 5f)
				{
					float num6 = distance2 * landRatio;
					float num7 = distance2 - num6;
					num7 *= CalculateShipDistanceAmplifier(mobileParty, num7);
					float num8 = num7 + num6;
					if (num8 < num3)
					{
						num3 = num8;
						flag = true;
					}
				}
			}
			if (num3 < num2 && num3 < num)
			{
				bestNavigationType = MobileParty.NavigationType.All;
				bestNavigationDistance = num3;
				isFromPort = flag;
			}
			return;
		}
		float distance3 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, settlement, isTargetingPort, MobileParty.NavigationType.All, out var estimatedLandRatio);
		if (distance3 < Campaign.MapDiagonal * 5f)
		{
			float num9 = distance3 * estimatedLandRatio;
			float num10 = distance3 - num9;
			num10 *= CalculateShipDistanceAmplifier(mobileParty, num10);
			distance3 = num10 + num9;
			if (distance3 < num2 && distance3 < num)
			{
				bestNavigationType = MobileParty.NavigationType.All;
				bestNavigationDistance = distance3;
				isFromPort = false;
			}
		}
	}

	public static void GetBestNavigationTypeAndDistanceOfMobilePartyForMobileParty(MobileParty mobileParty, MobileParty toMobileParty, out MobileParty.NavigationType bestNavigationType, out float bestNavigationDistance)
	{
		bestNavigationType = MobileParty.NavigationType.None;
		bestNavigationDistance = float.MaxValue;
		float landRatio = -1f;
		float num = float.MaxValue;
		if (mobileParty.HasLandNavigationCapability)
		{
			num = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, toMobileParty, MobileParty.NavigationType.Default, out landRatio);
		}
		if (num < Campaign.MapDiagonal * 5f)
		{
			bestNavigationType = MobileParty.NavigationType.Default;
			bestNavigationDistance = num;
		}
		if (!mobileParty.HasNavalNavigationCapability)
		{
			return;
		}
		float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, toMobileParty, MobileParty.NavigationType.Naval, out landRatio);
		if (num2 < Campaign.MapDiagonal * 5f)
		{
			num2 *= CalculateShipDistanceAmplifier(mobileParty, num2);
			if (num2 < num)
			{
				bestNavigationType = MobileParty.NavigationType.Naval;
				bestNavigationDistance = num2;
			}
		}
		if (!mobileParty.HasLandNavigationCapability)
		{
			return;
		}
		float num3 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, toMobileParty, MobileParty.NavigationType.All, out landRatio);
		if (num3 < Campaign.MapDiagonal * 5f)
		{
			num3 *= CalculateShipDistanceAmplifier(mobileParty, num3);
			if (num3 < num2 && num3 < num)
			{
				bestNavigationType = MobileParty.NavigationType.All;
				bestNavigationDistance = num3;
			}
		}
	}

	private static float CalculateShipDistanceAmplifier(MobileParty mobileParty, float navalDistance)
	{
		if (mobileParty.HasLandNavigationCapability)
		{
			float num = (mobileParty.IsLordParty ? Campaign.Current.EstimatedAverageLordPartyNavalSpeed : (mobileParty.IsCaravan ? Campaign.Current.EstimatedAverageCaravanPartyNavalSpeed : (mobileParty.IsBandit ? Campaign.Current.EstimatedAverageBanditPartyNavalSpeed : (mobileParty.IsVillager ? Campaign.Current.EstimatedAverageVillagerPartyNavalSpeed : (Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer * 0.5f)))));
			float num2 = navalDistance / num;
			float estimatedSafeSailDuration = Campaign.Current.Models.CampaignShipDamageModel.GetEstimatedSafeSailDuration(mobileParty);
			float num3 = Campaign.MapDiagonal * 0.5f;
			if (estimatedSafeSailDuration > num2)
			{
				float num4 = estimatedSafeSailDuration / num2;
				if (num4 > 4f)
				{
					num3 = 0.35f;
				}
				else if (num4 > 3f)
				{
					num3 = MBMath.Map(num4, 3f, 4f, 0.35f, 0.6f);
				}
				else if (num4 > 2f)
				{
					num3 = MBMath.Map(num4, 2f, 3f, 0.6f, 1f);
				}
				else if (num4 > 1f)
				{
					num3 = MBMath.Map(num4, 1f, 2f, 1f, 1.25f);
				}
			}
			int num5 = 0;
			foreach (Ship ship in mobileParty.Ships)
			{
				if (ship.HitPoints / ship.MaxHitPoints > 0.2f)
				{
					num5 += ship.TotalCrewCapacity;
				}
			}
			int num6 = mobileParty.MemberRoster.TotalManCount;
			foreach (MobileParty attachedParty in mobileParty.AttachedParties)
			{
				num6 += attachedParty.MemberRoster.TotalManCount;
			}
			float num7 = (float)num5 / (float)num6;
			if (num7 < 1f)
			{
				num3 = ((num7 > 0.8f) ? (num3 * MBMath.Map(num7, 0.8f, 1f, 1.5f, 1f)) : ((!(num7 > 0.6f)) ? (num3 * 3.5f) : (num3 * MBMath.Map(num7, 0.6f, 0.8f, 2.2f, 1.5f))));
			}
			return num3;
		}
		return 1f;
	}
}
