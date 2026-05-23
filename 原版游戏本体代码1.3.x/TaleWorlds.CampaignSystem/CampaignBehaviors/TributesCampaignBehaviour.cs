using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000449 RID: 1097
	public class TributesCampaignBehaviour : CampaignBehaviorBase
	{
		// Token: 0x060045E3 RID: 17891 RVA: 0x0015BD0A File Offset: 0x00159F0A
		public override void RegisterEvents()
		{
			CampaignEvents.OnClanEarnedGoldFromTributeEvent.AddNonSerializedListener(this, new Action<Clan, IFaction>(TributesCampaignBehaviour.OnClanEarnedGoldFromTribute));
		}

		// Token: 0x060045E4 RID: 17892 RVA: 0x0015BD24 File Offset: 0x00159F24
		private static void OnClanEarnedGoldFromTribute(Clan clan, IFaction payerFaction)
		{
			StanceLink stanceWith = clan.MapFaction.GetStanceWith(payerFaction);
			if ((clan == Clan.PlayerClan || payerFaction == Clan.PlayerClan.MapFaction) && stanceWith.GetRemainingTributePaymentCount() == 0)
			{
				bool flag = payerFaction == Clan.PlayerClan.MapFaction;
				TextObject textObject = (flag ? new TextObject("{=LJFXfmpn}The tribute your kingdom owed to {ENEMY_FACTION} is now complete.", null) : new TextObject("{=aod7KVc8}The tribute {ENEMY_FACTION} owed to your kingdom is now complete.", null));
				IFaction faction = (flag ? clan.MapFaction : payerFaction);
				textObject.SetTextVariable("ENEMY_FACTION", faction.Name);
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new TributeFinishedMapNotification(textObject, faction));
			}
		}

		// Token: 0x060045E5 RID: 17893 RVA: 0x0015BDBD File Offset: 0x00159FBD
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
