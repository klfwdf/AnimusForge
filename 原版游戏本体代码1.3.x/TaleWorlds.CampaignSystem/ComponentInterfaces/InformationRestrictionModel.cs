using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200018B RID: 395
	public abstract class InformationRestrictionModel : MBGameModel<InformationRestrictionModel>
	{
		// Token: 0x06001BE4 RID: 7140
		public abstract bool DoesPlayerKnowDetailsOf(Settlement settlement);

		// Token: 0x06001BE5 RID: 7141
		public abstract bool DoesPlayerKnowDetailsOf(Hero hero);
	}
}
