using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000191 RID: 401
	public abstract class PartySpeedModel : MBGameModel<PartySpeedModel>
	{
		// Token: 0x17000704 RID: 1796
		// (get) Token: 0x06001C0B RID: 7179
		public abstract float BaseSpeed { get; }

		// Token: 0x17000705 RID: 1797
		// (get) Token: 0x06001C0C RID: 7180
		public abstract float MinimumSpeed { get; }

		// Token: 0x06001C0D RID: 7181
		public abstract ExplainedNumber CalculateBaseSpeed(MobileParty party, bool includeDescriptions = false, int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0);

		// Token: 0x06001C0E RID: 7182
		public abstract ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed);
	}
}
