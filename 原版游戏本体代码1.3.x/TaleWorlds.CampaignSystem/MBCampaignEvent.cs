using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200003A RID: 58
	public class MBCampaignEvent
	{
		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x060003DC RID: 988 RVA: 0x0001E252 File Offset: 0x0001C452
		// (set) Token: 0x060003DD RID: 989 RVA: 0x0001E25A File Offset: 0x0001C45A
		public CampaignTime TriggerPeriod { get; private set; }

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x060003DE RID: 990 RVA: 0x0001E263 File Offset: 0x0001C463
		// (set) Token: 0x060003DF RID: 991 RVA: 0x0001E26B File Offset: 0x0001C46B
		public CampaignTime InitialWait { get; private set; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x060003E0 RID: 992 RVA: 0x0001E274 File Offset: 0x0001C474
		// (set) Token: 0x060003E1 RID: 993 RVA: 0x0001E27C File Offset: 0x0001C47C
		public bool isEventDeleted { get; set; }

		// Token: 0x060003E2 RID: 994 RVA: 0x0001E285 File Offset: 0x0001C485
		public MBCampaignEvent(string eventName)
		{
			this.description = eventName;
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0001E29F File Offset: 0x0001C49F
		public MBCampaignEvent(CampaignTime triggerPeriod, CampaignTime initialWait)
		{
			this.TriggerPeriod = triggerPeriod;
			this.InitialWait = initialWait;
			this.NextTriggerTime = CampaignTime.Now + this.InitialWait;
			this.isEventDeleted = false;
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0001E2DD File Offset: 0x0001C4DD
		public void AddHandler(MBCampaignEvent.CampaignEventDelegate gameEventDelegate)
		{
			this.handlers.Add(gameEventDelegate);
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x0001E2EC File Offset: 0x0001C4EC
		public void RunHandlers(params object[] delegateParams)
		{
			for (int i = 0; i < this.handlers.Count; i++)
			{
				this.handlers[i](this, delegateParams);
			}
		}

		// Token: 0x060003E6 RID: 998 RVA: 0x0001E324 File Offset: 0x0001C524
		public void Unregister(object instance)
		{
			for (int i = 0; i < this.handlers.Count; i++)
			{
				if (this.handlers[i].Target == instance)
				{
					this.handlers.RemoveAt(i);
					i--;
				}
			}
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x0001E36C File Offset: 0x0001C56C
		public void CheckUpdate()
		{
			while (this.NextTriggerTime.IsPast && !this.isEventDeleted)
			{
				this.RunHandlers(new object[] { CampaignTime.Now });
				this.NextTriggerTime += this.TriggerPeriod;
			}
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x0001E3C0 File Offset: 0x0001C5C0
		public void DeletePeriodicEvent()
		{
			this.isEventDeleted = true;
		}

		// Token: 0x04000181 RID: 385
		public string description;

		// Token: 0x04000182 RID: 386
		protected List<MBCampaignEvent.CampaignEventDelegate> handlers = new List<MBCampaignEvent.CampaignEventDelegate>();

		// Token: 0x04000183 RID: 387
		[CachedData]
		protected CampaignTime NextTriggerTime;

		// Token: 0x02000503 RID: 1283
		// (Invoke) Token: 0x06004B01 RID: 19201
		public delegate void CampaignEventDelegate(MBCampaignEvent campaignEvent, params object[] delegateParams);
	}
}
