using System;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000408 RID: 1032
	public interface IRetrainOutlawPartyMembersCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x0600403F RID: 16447
		int GetRetrainedNumber(CharacterObject character);

		// Token: 0x06004040 RID: 16448
		void SetRetrainedNumber(CharacterObject character, int number);
	}
}
