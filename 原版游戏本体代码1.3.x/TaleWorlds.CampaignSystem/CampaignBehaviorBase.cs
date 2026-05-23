using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000032 RID: 50
	public abstract class CampaignBehaviorBase : ICampaignBehavior
	{
		// Token: 0x0600035F RID: 863 RVA: 0x00017254 File Offset: 0x00015454
		public CampaignBehaviorBase(string stringId)
		{
			this.StringId = stringId;
		}

		// Token: 0x06000360 RID: 864 RVA: 0x00017263 File Offset: 0x00015463
		public CampaignBehaviorBase()
		{
			this.StringId = base.GetType().Name;
		}

		// Token: 0x06000361 RID: 865
		public abstract void RegisterEvents();

		// Token: 0x06000362 RID: 866 RVA: 0x0001727C File Offset: 0x0001547C
		public static T GetCampaignBehavior<T>()
		{
			return Campaign.Current.GetCampaignBehavior<T>();
		}

		// Token: 0x06000363 RID: 867
		public abstract void SyncData(IDataStore dataStore);

		// Token: 0x040000CC RID: 204
		public readonly string StringId;
	}
}
