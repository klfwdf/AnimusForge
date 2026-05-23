using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F7 RID: 1015
	public interface IAlleyCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x06003F7C RID: 16252
		bool GetIsPlayerAlleyUnderAttack(Alley alley);

		// Token: 0x06003F7D RID: 16253
		int GetPlayerOwnedAlleyTroopCount(Alley alley);

		// Token: 0x06003F7E RID: 16254
		int GetResponseTimeLeftForAttackInDays(Alley alley);

		// Token: 0x06003F7F RID: 16255
		void AbandonAlleyFromClanMenu(Alley alley);

		// Token: 0x06003F80 RID: 16256
		Hero GetAssignedClanMemberOfAlley(Alley alley);

		// Token: 0x06003F81 RID: 16257
		bool IsHeroAlleyLeaderOfAnyPlayerAlley(Hero hero);

		// Token: 0x06003F82 RID: 16258
		List<Hero> GetAllAssignedClanMembersForOwnedAlleys();

		// Token: 0x06003F83 RID: 16259
		void ChangeAlleyMember(Alley alley, Hero newAlleyLead);

		// Token: 0x06003F84 RID: 16260
		void OnPlayerRetreatedFromMission();

		// Token: 0x06003F85 RID: 16261
		void OnPlayerDiedInMission();
	}
}
