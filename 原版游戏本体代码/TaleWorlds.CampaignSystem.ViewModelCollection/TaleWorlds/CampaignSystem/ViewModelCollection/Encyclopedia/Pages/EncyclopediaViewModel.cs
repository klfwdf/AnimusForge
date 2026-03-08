using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D7 RID: 215
	public class EncyclopediaViewModel : Attribute
	{
		// Token: 0x170006C5 RID: 1733
		// (get) Token: 0x06001471 RID: 5233 RVA: 0x0005102E File Offset: 0x0004F22E
		// (set) Token: 0x06001472 RID: 5234 RVA: 0x00051036 File Offset: 0x0004F236
		public Type PageTargetType { get; private set; }

		// Token: 0x06001473 RID: 5235 RVA: 0x0005103F File Offset: 0x0004F23F
		public EncyclopediaViewModel(Type pageTargetType)
		{
			this.PageTargetType = pageTargetType;
		}
	}
}
