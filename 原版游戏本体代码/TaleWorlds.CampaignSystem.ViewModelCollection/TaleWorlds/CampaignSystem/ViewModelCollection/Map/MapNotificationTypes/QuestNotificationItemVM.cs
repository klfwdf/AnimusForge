using System;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200004F RID: 79
	public class QuestNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x0600063F RID: 1599 RVA: 0x00020214 File Offset: 0x0001E414
		public QuestNotificationItemVM(QuestBase quest, InformationData data, Action<QuestBase> onQuestNotificationInspect, Action<MapNotificationItemBaseVM> onRemove)
			: base(data)
		{
			this._quest = quest;
			this._onQuestNotificationInspect = onQuestNotificationInspect;
			this._onInspect = (this._onInspectAction = delegate()
			{
				this._onQuestNotificationInspect(this._quest);
			});
			base.NotificationIdentifier = "quest";
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x0002025C File Offset: 0x0001E45C
		public QuestNotificationItemVM(IssueBase issue, InformationData data, Action<IssueBase> onIssueNotificationInspect, Action<MapNotificationItemBaseVM> onRemove)
			: base(data)
		{
			this._issue = issue;
			this._onIssueNotificationInspect = onIssueNotificationInspect;
			this._onInspect = (this._onInspectAction = delegate()
			{
				this._onIssueNotificationInspect(this._issue);
			});
			base.NotificationIdentifier = "quest";
		}

		// Token: 0x06000641 RID: 1601 RVA: 0x000202A4 File Offset: 0x0001E4A4
		public override void ManualRefreshRelevantStatus()
		{
			base.ManualRefreshRelevantStatus();
		}

		// Token: 0x040002A6 RID: 678
		private QuestBase _quest;

		// Token: 0x040002A7 RID: 679
		private IssueBase _issue;

		// Token: 0x040002A8 RID: 680
		private Action<QuestBase> _onQuestNotificationInspect;

		// Token: 0x040002A9 RID: 681
		private Action<IssueBase> _onIssueNotificationInspect;

		// Token: 0x040002AA RID: 682
		protected Action _onInspectAction;
	}
}
