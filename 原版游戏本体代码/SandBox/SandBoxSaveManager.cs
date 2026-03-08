using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000027 RID: 39
	public class SandBoxSaveManager : ISaveManager
	{
		// Token: 0x06000126 RID: 294 RVA: 0x00007D0C File Offset: 0x00005F0C
		public int GetAutoSaveInterval()
		{
			return BannerlordConfig.AutoSaveInterval;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00007D13 File Offset: 0x00005F13
		public bool IsAutoSaveDisabled()
		{
			return BannerlordConfig.AutoSaveInterval == -1;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00007D1D File Offset: 0x00005F1D
		public void OnSaveOver(bool isSuccessful, string newSaveGameName)
		{
			if (isSuccessful)
			{
				BannerlordConfig.LatestSaveGameName = newSaveGameName;
				BannerlordConfig.Save();
			}
		}
	}
}
