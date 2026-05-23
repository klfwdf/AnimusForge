using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F5 RID: 501
	public abstract class AlleyModel : MBGameModel<AlleyModel>
	{
		// Token: 0x170007BB RID: 1979
		// (get) Token: 0x06001F0E RID: 7950
		public abstract CampaignTime DestroyAlleyAfterDaysWhenLeaderIsDeath { get; }

		// Token: 0x170007BC RID: 1980
		// (get) Token: 0x06001F0F RID: 7951
		public abstract int MinimumTroopCountInPlayerOwnedAlley { get; }

		// Token: 0x170007BD RID: 1981
		// (get) Token: 0x06001F10 RID: 7952
		public abstract int MaximumTroopCountInPlayerOwnedAlley { get; }

		// Token: 0x170007BE RID: 1982
		// (get) Token: 0x06001F11 RID: 7953
		public abstract float GetDailyCrimeRatingOfAlley { get; }

		// Token: 0x06001F12 RID: 7954
		public abstract float GetDailyXpGainForAssignedClanMember(Hero assignedHero);

		// Token: 0x06001F13 RID: 7955
		public abstract float GetDailyXpGainForMainHero();

		// Token: 0x06001F14 RID: 7956
		public abstract float GetInitialXpGainForMainHero();

		// Token: 0x06001F15 RID: 7957
		public abstract float GetXpGainAfterSuccessfulAlleyDefenseForMainHero();

		// Token: 0x06001F16 RID: 7958
		public abstract TroopRoster GetTroopsOfAIOwnedAlley(Alley alley);

		// Token: 0x06001F17 RID: 7959
		public abstract TroopRoster GetTroopsOfAlleyForBattleMission(Alley alley);

		// Token: 0x06001F18 RID: 7960
		public abstract int GetDailyIncomeOfAlley(Alley alley);

		// Token: 0x06001F19 RID: 7961
		public abstract List<ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>> GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(Alley alley);

		// Token: 0x06001F1A RID: 7962
		public abstract TroopRoster GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(Alley alley, float random);

		// Token: 0x06001F1B RID: 7963
		public abstract TextObject GetDisabledReasonTextForHero(Hero hero, Alley alley, DefaultAlleyModel.AlleyMemberAvailabilityDetail detail);

		// Token: 0x06001F1C RID: 7964
		public abstract float GetAlleyAttackResponseTimeInDays(TroopRoster troopRoster);
	}
}
