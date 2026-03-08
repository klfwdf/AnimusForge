using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200010C RID: 268
	public class DefaultDelayedTeleportationModel : DelayedTeleportationModel
	{
		// Token: 0x17000644 RID: 1604
		// (get) Token: 0x06001738 RID: 5944 RVA: 0x0006C2DF File Offset: 0x0006A4DF
		private float MaximumDistanceForDelayAsDays
		{
			get
			{
				return 2f;
			}
		}

		// Token: 0x17000645 RID: 1605
		// (get) Token: 0x06001739 RID: 5945 RVA: 0x0006C2E6 File Offset: 0x0006A4E6
		public override float DefaultTeleportationSpeed
		{
			get
			{
				return 0.24f;
			}
		}

		// Token: 0x0600173A RID: 5946 RVA: 0x0006C2F0 File Offset: 0x0006A4F0
		public override ExplainedNumber GetTeleportationDelayAsHours(Hero teleportingHero, PartyBase target)
		{
			float num = this.MaximumDistanceForDelayAsDays * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
			float num2 = 0f;
			IMapPoint mapPoint = teleportingHero.GetMapPoint();
			if (mapPoint != null)
			{
				MobileParty.NavigationType navigationType = (teleportingHero.Clan.HasNavalNavigationCapability ? MobileParty.NavigationType.All : MobileParty.NavigationType.Default);
				if (target.IsSettlement)
				{
					if (teleportingHero.CurrentSettlement != null && teleportingHero.CurrentSettlement == target.Settlement)
					{
						num2 = 0f;
					}
					else
					{
						float num3;
						num2 = DistanceHelper.FindClosestDistanceFromMapPointToSettlement(mapPoint, target.Settlement, navigationType, out num3);
					}
				}
				else if (target.IsMobile)
				{
					Settlement toSettlement;
					MobileParty to;
					if ((toSettlement = mapPoint as Settlement) != null)
					{
						num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(target.MobileParty, toSettlement, navigationType);
					}
					else if ((to = mapPoint as MobileParty) != null)
					{
						float num4 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(target.MobileParty, to, navigationType);
						if (num4 < num)
						{
							num2 = num4;
						}
					}
				}
			}
			num2 = MathF.Clamp(num2, 0f, num);
			return new ExplainedNumber(num2 * this.DefaultTeleportationSpeed, false, null);
		}

		// Token: 0x0600173B RID: 5947 RVA: 0x0006C3D6 File Offset: 0x0006A5D6
		public override bool CanPerformImmediateTeleport(Hero hero, MobileParty targetMobileParty, Settlement targetSettlement)
		{
			return (targetSettlement != null && !targetSettlement.IsUnderSiege && !targetSettlement.IsUnderRaid) || (targetMobileParty != null && targetMobileParty.MapEvent == null && !targetMobileParty.IsCurrentlyEngagingParty && (!targetMobileParty.IsCurrentlyAtSea || targetMobileParty.CurrentSettlement != null));
		}
	}
}
