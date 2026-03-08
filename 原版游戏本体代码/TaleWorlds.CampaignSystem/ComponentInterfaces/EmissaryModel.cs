using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A7 RID: 423
	public abstract class EmissaryModel : MBGameModel<EmissaryModel>
	{
		// Token: 0x17000722 RID: 1826
		// (get) Token: 0x06001CD2 RID: 7378
		public abstract int EmissaryRelationBonusForMainClan { get; }

		// Token: 0x06001CD3 RID: 7379
		public abstract bool IsEmissary(Hero hero);
	}
}
