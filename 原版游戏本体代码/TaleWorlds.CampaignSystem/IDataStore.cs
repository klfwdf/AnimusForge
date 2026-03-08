using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000033 RID: 51
	public interface IDataStore
	{
		// Token: 0x06000364 RID: 868
		bool SyncData<T>(string key, ref T data);

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000365 RID: 869
		bool IsSaving { get; }

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000366 RID: 870
		bool IsLoading { get; }
	}
}
