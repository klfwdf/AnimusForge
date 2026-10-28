using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IAlleyCampaignBehavior : ICampaignBehavior
{
	bool GetIsPlayerAlleyUnderAttack(Alley alley);

	int GetPlayerOwnedAlleyTroopCount(Alley alley);

	int GetResponseTimeLeftForAttackInDays(Alley alley);

	void AbandonAlleyFromClanMenu(Alley alley);

	Hero GetAssignedClanMemberOfAlley(Alley alley);

	bool IsHeroAlleyLeaderOfAnyPlayerAlley(Hero hero);

	List<Hero> GetAllAssignedClanMembersForOwnedAlleys();

	void ChangeAlleyMember(Alley alley, Hero newAlleyLead);

	void OnPlayerRetreatedFromMission();

	void OnPlayerDiedInMission();
}
