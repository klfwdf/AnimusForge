using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C8 RID: 1224
	public static class StartMercenaryServiceAction
	{
		// Token: 0x06004A2C RID: 18988 RVA: 0x00175CD4 File Offset: 0x00173ED4
		private static void ApplyStart(Clan clan, Kingdom kingdom, int awardMultiplier, StartMercenaryServiceAction.StartMercenaryServiceActionDetails details)
		{
			if (clan.IsUnderMercenaryService)
			{
				EndMercenaryServiceAction.EndByLeavingKingdom(clan);
			}
			clan.MercenaryAwardMultiplier = awardMultiplier;
			clan.Kingdom = kingdom;
			clan.StartMercenaryService();
			if (clan == Clan.PlayerClan)
			{
				Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay = Campaign.CurrentTime + 30f * (float)CampaignTime.HoursInDay;
			}
			CampaignEventDispatcher.Instance.OnMercenaryServiceStarted(clan, details);
		}

		// Token: 0x06004A2D RID: 18989 RVA: 0x00175D38 File Offset: 0x00173F38
		public static void ApplyByDefault(Clan clan, Kingdom kingdom, int awardMultiplier)
		{
			StartMercenaryServiceAction.ApplyStart(clan, kingdom, awardMultiplier, StartMercenaryServiceAction.StartMercenaryServiceActionDetails.ApplyByDefault);
		}

		// Token: 0x02000898 RID: 2200
		public enum StartMercenaryServiceActionDetails
		{
			// Token: 0x0400246C RID: 9324
			ApplyByDefault
		}
	}
}
