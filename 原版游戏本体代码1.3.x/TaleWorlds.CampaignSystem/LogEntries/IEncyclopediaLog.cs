using System;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.LogEntries
{
	// Token: 0x0200034F RID: 847
	public interface IEncyclopediaLog
	{
		// Token: 0x060031A9 RID: 12713
		bool IsVisibleInEncyclopediaPageOf<T>(T obj) where T : MBObjectBase;

		// Token: 0x060031AA RID: 12714
		TextObject GetEncyclopediaText();

		// Token: 0x17000BF4 RID: 3060
		// (get) Token: 0x060031AB RID: 12715
		CampaignTime GameTime { get; }
	}
}
