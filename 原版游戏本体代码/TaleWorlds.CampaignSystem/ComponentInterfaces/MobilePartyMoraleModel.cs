using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001CE RID: 462
	public abstract class MobilePartyMoraleModel : MBGameModel<MobilePartyMoraleModel>
	{
		// Token: 0x06001E09 RID: 7689
		public abstract float CalculateMoraleChange(MobileParty party);

		// Token: 0x06001E0A RID: 7690
		public abstract TextObject GetMoraleTooltipText(MobileParty party);
	}
}
