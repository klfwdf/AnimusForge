using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B3 RID: 435
	public abstract class ArmyManagementCalculationModel : MBGameModel<ArmyManagementCalculationModel>
	{
		// Token: 0x17000742 RID: 1858
		// (get) Token: 0x06001D23 RID: 7459
		public abstract float AIMobilePartySizeRatioToCallToArmy { get; }

		// Token: 0x17000743 RID: 1859
		// (get) Token: 0x06001D24 RID: 7460
		public abstract float PlayerMobilePartySizeRatioToCallToArmy { get; }

		// Token: 0x17000744 RID: 1860
		// (get) Token: 0x06001D25 RID: 7461
		public abstract float MinimumNeededFoodInDaysToCallToArmy { get; }

		// Token: 0x17000745 RID: 1861
		// (get) Token: 0x06001D26 RID: 7462
		public abstract float MaximumDistanceToCallToArmy { get; }

		// Token: 0x17000746 RID: 1862
		// (get) Token: 0x06001D27 RID: 7463
		public abstract int InfluenceValuePerGold { get; }

		// Token: 0x17000747 RID: 1863
		// (get) Token: 0x06001D28 RID: 7464
		public abstract int AverageCallToArmyCost { get; }

		// Token: 0x17000748 RID: 1864
		// (get) Token: 0x06001D29 RID: 7465
		public abstract int CohesionThresholdForDispersion { get; }

		// Token: 0x17000749 RID: 1865
		// (get) Token: 0x06001D2A RID: 7466
		public abstract float MaximumWaitTime { get; }

		// Token: 0x06001D2B RID: 7467
		public abstract bool CanPlayerCreateArmy(out TextObject disabledReason);

		// Token: 0x06001D2C RID: 7468
		public abstract int CalculatePartyInfluenceCost(MobileParty armyLeaderParty, MobileParty party);

		// Token: 0x06001D2D RID: 7469
		public abstract float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty);

		// Token: 0x06001D2E RID: 7470
		public abstract List<MobileParty> GetMobilePartiesToCallToArmy(MobileParty leaderParty);

		// Token: 0x06001D2F RID: 7471
		public abstract int CalculateTotalInfluenceCost(Army army, float percentage);

		// Token: 0x06001D30 RID: 7472
		public abstract float GetPartySizeScore(MobileParty party);

		// Token: 0x06001D31 RID: 7473
		public abstract bool CheckPartyEligibility(MobileParty party, out TextObject explanation);

		// Token: 0x06001D32 RID: 7474
		public abstract int GetPartyRelation(Hero hero);

		// Token: 0x06001D33 RID: 7475
		public abstract ExplainedNumber CalculateDailyCohesionChange(Army army, bool includeDescriptions = false);

		// Token: 0x06001D34 RID: 7476
		public abstract int CalculateNewCohesion(Army army, PartyBase newParty, int calculatedCohesion, int sign);

		// Token: 0x06001D35 RID: 7477
		public abstract int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100);
	}
}
