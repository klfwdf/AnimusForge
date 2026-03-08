using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001AA RID: 426
	public abstract class PartyImpairmentModel : MBGameModel<PartyImpairmentModel>
	{
		// Token: 0x06001CE2 RID: 7394
		public abstract ExplainedNumber GetDisorganizedStateDuration(MobileParty party);

		// Token: 0x06001CE3 RID: 7395
		public abstract float GetVulnerabilityStateDuration(PartyBase party);

		// Token: 0x06001CE4 RID: 7396
		public abstract float GetSiegeExpectedVulnerabilityTime();

		// Token: 0x06001CE5 RID: 7397
		public abstract bool CanGetDisorganized(PartyBase partyBase);
	}
}
