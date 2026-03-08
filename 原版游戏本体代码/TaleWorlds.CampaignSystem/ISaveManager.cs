using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000099 RID: 153
	public interface ISaveManager
	{
		// Token: 0x060012BF RID: 4799
		int GetAutoSaveInterval();

		// Token: 0x060012C0 RID: 4800
		bool IsAutoSaveDisabled();

		// Token: 0x060012C1 RID: 4801
		void OnSaveOver(bool isSuccessful, string newSaveGameName);
	}
}
