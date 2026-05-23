using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E9 RID: 489
	public abstract class PrisonerRecruitmentCalculationModel : MBGameModel<PrisonerRecruitmentCalculationModel>
	{
		// Token: 0x06001EC2 RID: 7874
		public abstract int GetConformityNeededToRecruitPrisoner(CharacterObject character);

		// Token: 0x06001EC3 RID: 7875
		public abstract ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject character);

		// Token: 0x06001EC4 RID: 7876
		public abstract int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num);

		// Token: 0x06001EC5 RID: 7877
		public abstract bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded);

		// Token: 0x06001EC6 RID: 7878
		public abstract bool ShouldPartyRecruitPrisoners(PartyBase party);

		// Token: 0x06001EC7 RID: 7879
		public abstract int CalculateRecruitableNumber(PartyBase party, CharacterObject character);
	}
}
