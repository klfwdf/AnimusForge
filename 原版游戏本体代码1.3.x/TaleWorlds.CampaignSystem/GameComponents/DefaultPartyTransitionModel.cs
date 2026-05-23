using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200013A RID: 314
	public class DefaultPartyTransitionModel : PartyTransitionModel
	{
		// Token: 0x06001946 RID: 6470 RVA: 0x0007D698 File Offset: 0x0007B898
		public override CampaignTime GetFleetTravelTimeToPoint(MobileParty mobileParty, CampaignVec2 target)
		{
			CampaignVec2 campaignVec = (mobileParty.Anchor.IsMovingToPoint ? mobileParty.TargetPosition : mobileParty.Anchor.GetInteractionPosition(mobileParty));
			if (!campaignVec.IsValid())
			{
				return CampaignTime.Hours(6f);
			}
			float num = campaignVec.Distance(target);
			if (num < 20f)
			{
				return CampaignTime.Zero;
			}
			float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Naval);
			float num2 = (mobileParty.Anchor.IsMovingToPoint ? ((float)(mobileParty.Anchor.ArrivalTime - CampaignTime.Now).ToHours) : 0f);
			return CampaignTime.Hours(MBMath.ClampFloat(num / averageDistanceBetweenClosestTwoTownsWithNavigationType + num2, 1f, 6f));
		}

		// Token: 0x06001947 RID: 6471 RVA: 0x0007D74B File Offset: 0x0007B94B
		public override CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty)
		{
			if (mobileParty.IsInRaftState)
			{
				return CampaignTime.Zero;
			}
			return CampaignTime.Hours(2f);
		}

		// Token: 0x06001948 RID: 6472 RVA: 0x0007D768 File Offset: 0x0007B968
		public override CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty)
		{
			if (!mobileParty.Anchor.IsValid)
			{
				return CampaignTime.Hours(6f);
			}
			float distance;
			if (mobileParty.CurrentSettlement == null)
			{
				MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
				CampaignVec2 interactionPosition = mobileParty.Anchor.GetInteractionPosition(mobileParty);
				float num;
				distance = mapDistanceModel.GetDistance(mobileParty, interactionPosition, MobileParty.NavigationType.Default, out num);
			}
			else
			{
				MapDistanceModel mapDistanceModel2 = Campaign.Current.Models.MapDistanceModel;
				Settlement currentSettlement = mobileParty.CurrentSettlement;
				CampaignVec2 position = mobileParty.Anchor.Position;
				distance = mapDistanceModel2.GetDistance(currentSettlement, position, true, MobileParty.NavigationType.Naval);
			}
			float num2 = distance;
			if (num2 < 20f)
			{
				return CampaignTime.Zero;
			}
			float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Naval);
			return CampaignTime.Hours(MBMath.ClampFloat(num2 / averageDistanceBetweenClosestTwoTownsWithNavigationType, 1f, 6f));
		}

		// Token: 0x04000861 RID: 2145
		private const float MinHoursToMoveAnchor = 1f;

		// Token: 0x04000862 RID: 2146
		private const float MaxHoursToMoveAnchor = 6f;

		// Token: 0x04000863 RID: 2147
		private const float InstantEmbarkDistanceThreshold = 20f;

		// Token: 0x04000864 RID: 2148
		private const float DisembarkHours = 2f;
	}
}
