using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000192 RID: 402
	public abstract class PartyTrainingModel : MBGameModel<PartyTrainingModel>
	{
		// Token: 0x06001C10 RID: 7184
		public abstract int GenerateSharedXp(CharacterObject troop, int xp, MobileParty mobileParty);

		// Token: 0x06001C11 RID: 7185
		public abstract ExplainedNumber CalculateXpGainFromBattles(FlattenedTroopRosterElement troopRosterElement, PartyBase party);

		// Token: 0x06001C12 RID: 7186
		public abstract int GetXpReward(CharacterObject character);

		// Token: 0x06001C13 RID: 7187
		public abstract ExplainedNumber GetEffectiveDailyExperience(MobileParty party, TroopRosterElement troop);
	}
}
