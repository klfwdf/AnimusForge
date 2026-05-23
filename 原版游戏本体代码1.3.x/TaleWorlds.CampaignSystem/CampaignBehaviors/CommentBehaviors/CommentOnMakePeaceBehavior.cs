using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000461 RID: 1121
	public class CommentOnMakePeaceBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600477F RID: 18303 RVA: 0x00165F41 File Offset: 0x00164141
		public override void RegisterEvents()
		{
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnMakePeace));
		}

		// Token: 0x06004780 RID: 18304 RVA: 0x00165F5A File Offset: 0x0016415A
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004781 RID: 18305 RVA: 0x00165F5C File Offset: 0x0016415C
		private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			MakePeaceLogEntry makePeaceLogEntry = new MakePeaceLogEntry(faction1, faction2);
			LogEntry.AddLogEntry(makePeaceLogEntry);
			if (faction2 == Hero.MainHero.MapFaction || (faction1 == Hero.MainHero.MapFaction && detail != MakePeaceAction.MakePeaceDetail.ByKingdomDecision))
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new PeaceMapNotification(faction1, faction2, makePeaceLogEntry.GetEncyclopediaText()));
			}
		}
	}
}
