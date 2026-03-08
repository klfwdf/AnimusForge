using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x0200000F RID: 15
	public static class AiHelper
	{
		// Token: 0x0600007F RID: 127 RVA: 0x00007740 File Offset: 0x00005940
		public static void GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(MobileParty mobileParty, Settlement settlement, bool isTargetingPort, out MobileParty.NavigationType bestNavigationType, out float bestNavigationDistance, out bool isFromPort)
		{
			bestNavigationType = MobileParty.NavigationType.None;
			bestNavigationDistance = float.MaxValue;
			isFromPort = false;
			float num = -1f;
			if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement == settlement)
			{
				bestNavigationDistance = 0f;
				bestNavigationType = (isTargetingPort ? MobileParty.NavigationType.Naval : MobileParty.NavigationType.Default);
				return;
			}
			float num2 = float.MaxValue;
			if (mobileParty.HasLandNavigationCapability && !isTargetingPort)
			{
				num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, settlement, MobileParty.NavigationType.Default, out num);
			}
			if (num2 < Campaign.MapDiagonal * 5f && !isTargetingPort)
			{
				bestNavigationType = MobileParty.NavigationType.Default;
				bestNavigationDistance = num2;
			}
			if (mobileParty.HasNavalNavigationCapability)
			{
				float num3 = float.MaxValue;
				if (isTargetingPort)
				{
					num3 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(mobileParty, settlement, MobileParty.NavigationType.Naval, out num);
				}
				if (num3 < Campaign.MapDiagonal * 5f)
				{
					num3 *= AiHelper.CalculateShipDistanceAmplifier(mobileParty, num3);
					if (num3 < num2 && isTargetingPort)
					{
						bestNavigationType = MobileParty.NavigationType.Naval;
						bestNavigationDistance = num3;
						isFromPort = mobileParty.CurrentSettlement != null;
					}
				}
				if (mobileParty.HasLandNavigationCapability)
				{
					float num4 = float.MaxValue;
					bool flag = false;
					if (mobileParty.CurrentSettlement != null)
					{
						float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, settlement, false, isTargetingPort, MobileParty.NavigationType.All, out num);
						if (distance < Campaign.MapDiagonal * 5f)
						{
							float num5 = distance * num;
							float num6 = distance - num5;
							num6 *= AiHelper.CalculateShipDistanceAmplifier(mobileParty, num6);
							num4 = num6 + num5;
						}
						if (mobileParty.CurrentSettlement.HasPort)
						{
							float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, settlement, true, isTargetingPort, MobileParty.NavigationType.All, out num);
							if (distance2 < Campaign.MapDiagonal * 5f)
							{
								float num7 = distance2 * num;
								float num8 = distance2 - num7;
								num8 *= AiHelper.CalculateShipDistanceAmplifier(mobileParty, num8);
								float num9 = num8 + num7;
								if (num9 < num4)
								{
									num4 = num9;
									flag = true;
								}
							}
						}
						if (num4 < num3 && num4 < num2)
						{
							bestNavigationType = MobileParty.NavigationType.All;
							bestNavigationDistance = num4;
							isFromPort = flag;
							return;
						}
					}
					else
					{
						float num11;
						float num10 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, settlement, isTargetingPort, MobileParty.NavigationType.All, out num11);
						if (num10 < Campaign.MapDiagonal * 5f)
						{
							float num12 = num10 * num11;
							float num13 = num10 - num12;
							num13 *= AiHelper.CalculateShipDistanceAmplifier(mobileParty, num13);
							num10 = num13 + num12;
							if (num10 < num3 && num10 < num2)
							{
								bestNavigationType = MobileParty.NavigationType.All;
								bestNavigationDistance = num10;
								isFromPort = false;
							}
						}
					}
				}
			}
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00007960 File Offset: 0x00005B60
		public static void GetBestNavigationTypeAndDistanceOfMobilePartyForMobileParty(MobileParty mobileParty, MobileParty toMobileParty, out MobileParty.NavigationType bestNavigationType, out float bestNavigationDistance)
		{
			bestNavigationType = MobileParty.NavigationType.None;
			bestNavigationDistance = float.MaxValue;
			float num = -1f;
			float num2 = float.MaxValue;
			if (mobileParty.HasLandNavigationCapability)
			{
				num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, toMobileParty, MobileParty.NavigationType.Default, out num);
			}
			if (num2 < Campaign.MapDiagonal * 5f)
			{
				bestNavigationType = MobileParty.NavigationType.Default;
				bestNavigationDistance = num2;
			}
			if (mobileParty.HasNavalNavigationCapability)
			{
				float num3 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, toMobileParty, MobileParty.NavigationType.Naval, out num);
				if (num3 < Campaign.MapDiagonal * 5f)
				{
					num3 *= AiHelper.CalculateShipDistanceAmplifier(mobileParty, num3);
					if (num3 < num2)
					{
						bestNavigationType = MobileParty.NavigationType.Naval;
						bestNavigationDistance = num3;
					}
				}
				if (mobileParty.HasLandNavigationCapability)
				{
					float num4 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(mobileParty, toMobileParty, MobileParty.NavigationType.All, out num);
					if (num4 < Campaign.MapDiagonal * 5f)
					{
						num4 *= AiHelper.CalculateShipDistanceAmplifier(mobileParty, num4);
						if (num4 < num3 && num4 < num2)
						{
							bestNavigationType = MobileParty.NavigationType.All;
							bestNavigationDistance = num4;
						}
					}
				}
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00007A18 File Offset: 0x00005C18
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
				foreach (MobileParty mobileParty2 in mobileParty.AttachedParties)
				{
					num6 += mobileParty2.MemberRoster.TotalManCount;
				}
				float num7 = (float)num5 / (float)num6;
				if (num7 < 1f)
				{
					if (num7 > 0.8f)
					{
						num3 *= MBMath.Map(num7, 0.8f, 1f, 1.5f, 1f);
					}
					else if (num7 > 0.6f)
					{
						num3 *= MBMath.Map(num7, 0.6f, 0.8f, 2.2f, 1.5f);
					}
					else
					{
						num3 *= 3.5f;
					}
				}
				return num3;
			}
			return 1f;
		}
	}
}
