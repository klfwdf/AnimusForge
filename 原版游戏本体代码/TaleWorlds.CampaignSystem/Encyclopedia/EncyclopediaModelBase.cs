using System;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000179 RID: 377
	public abstract class EncyclopediaModelBase : Attribute
	{
		// Token: 0x170006EF RID: 1775
		// (get) Token: 0x06001B45 RID: 6981 RVA: 0x0008B891 File Offset: 0x00089A91
		// (set) Token: 0x06001B46 RID: 6982 RVA: 0x0008B899 File Offset: 0x00089A99
		public Type[] PageTargetTypes { get; private set; }

		// Token: 0x06001B47 RID: 6983 RVA: 0x0008B8A2 File Offset: 0x00089AA2
		public EncyclopediaModelBase(Type[] pageTargetTypes)
		{
			this.PageTargetTypes = pageTargetTypes;
		}
	}
}
