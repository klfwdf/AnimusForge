using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class AlleyModel : MBGameModel<AlleyModel>
{
	public abstract CampaignTime DestroyAlleyAfterDaysWhenLeaderIsDeath { get; }

	public abstract int MinimumTroopCountInPlayerOwnedAlley { get; }

	public abstract int MaximumTroopCountInPlayerOwnedAlley { get; }

	public abstract float GetDailyCrimeRatingOfAlley { get; }

	public abstract float GetDailyXpGainForAssignedClanMember(Hero assignedHero);

	public abstract float GetDailyXpGainForMainHero();

	public abstract float GetInitialXpGainForMainHero();

	public abstract float GetXpGainAfterSuccessfulAlleyDefenseForMainHero();

	public abstract TroopRoster GetTroopsOfAIOwnedAlley(Alley alley);

	public abstract TroopRoster GetTroopsOfAlleyForBattleMission(Alley alley);

	public abstract int GetDailyIncomeOfAlley(Alley alley);

	public abstract List<(Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail)> GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(Alley alley);

	public abstract TroopRoster GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(Alley alley, float random);

	public abstract TextObject GetDisabledReasonTextForHero(Hero hero, Alley alley, DefaultAlleyModel.AlleyMemberAvailabilityDetail detail);

	public abstract float GetAlleyAttackResponseTimeInDays(TroopRoster troopRoster);
}
