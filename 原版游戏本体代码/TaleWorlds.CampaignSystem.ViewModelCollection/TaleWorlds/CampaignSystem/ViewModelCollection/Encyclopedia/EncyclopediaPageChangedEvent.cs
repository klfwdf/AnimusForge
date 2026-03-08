using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000C4 RID: 196
	public class EncyclopediaPageChangedEvent : EventBase
	{
		// Token: 0x1700063A RID: 1594
		// (get) Token: 0x06001302 RID: 4866 RVA: 0x0004CB4D File Offset: 0x0004AD4D
		// (set) Token: 0x06001303 RID: 4867 RVA: 0x0004CB55 File Offset: 0x0004AD55
		public EncyclopediaPages NewPage { get; private set; }

		// Token: 0x1700063B RID: 1595
		// (get) Token: 0x06001304 RID: 4868 RVA: 0x0004CB5E File Offset: 0x0004AD5E
		// (set) Token: 0x06001305 RID: 4869 RVA: 0x0004CB66 File Offset: 0x0004AD66
		public bool NewPageHasHiddenInformation { get; private set; }

		// Token: 0x06001306 RID: 4870 RVA: 0x0004CB6F File Offset: 0x0004AD6F
		public EncyclopediaPageChangedEvent(EncyclopediaPages newPage, bool hasHiddenInformation = false)
		{
			this.NewPage = newPage;
			this.NewPageHasHiddenInformation = hasHiddenInformation;
		}
	}
}
