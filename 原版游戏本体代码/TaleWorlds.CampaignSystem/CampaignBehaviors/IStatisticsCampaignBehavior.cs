using System;
using System.Runtime.CompilerServices;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200040A RID: 1034
	public interface IStatisticsCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x06004085 RID: 16517
		void OnDefectionPersuasionSucess();

		// Token: 0x06004086 RID: 16518
		void OnPlayerAcceptedRansomOffer(int ransomPrice);

		// Token: 0x06004087 RID: 16519
		int GetHighestTournamentRank();

		// Token: 0x06004088 RID: 16520
		int GetNumberOfTournamentWins();

		// Token: 0x06004089 RID: 16521
		int GetNumberOfChildrenBorn();

		// Token: 0x0600408A RID: 16522
		int GetNumberOfPrisonersRecruited();

		// Token: 0x0600408B RID: 16523
		int GetNumberOfTroopsRecruited();

		// Token: 0x0600408C RID: 16524
		int GetNumberOfClansDefected();

		// Token: 0x0600408D RID: 16525
		int GetNumberOfIssuesSolved();

		// Token: 0x0600408E RID: 16526
		int GetTotalInfluenceEarned();

		// Token: 0x0600408F RID: 16527
		int GetTotalCrimeRatingGained();

		// Token: 0x06004090 RID: 16528
		int GetNumberOfBattlesWon();

		// Token: 0x06004091 RID: 16529
		int GetNumberOfBattlesLost();

		// Token: 0x06004092 RID: 16530
		int GetLargestBattleWonAsLeader();

		// Token: 0x06004093 RID: 16531
		int GetLargestArmyFormedByPlayer();

		// Token: 0x06004094 RID: 16532
		int GetNumberOfEnemyClansDestroyed();

		// Token: 0x06004095 RID: 16533
		int GetNumberOfHeroesKilledInBattle();

		// Token: 0x06004096 RID: 16534
		int GetNumberOfTroopsKnockedOrKilledAsParty();

		// Token: 0x06004097 RID: 16535
		int GetNumberOfTroopsKnockedOrKilledByPlayer();

		// Token: 0x06004098 RID: 16536
		int GetNumberOfHeroPrisonersTaken();

		// Token: 0x06004099 RID: 16537
		int GetNumberOfTroopPrisonersTaken();

		// Token: 0x0600409A RID: 16538
		int GetNumberOfTownsCaptured();

		// Token: 0x0600409B RID: 16539
		int GetNumberOfHideoutsCleared();

		// Token: 0x0600409C RID: 16540
		int GetNumberOfCastlesCaptured();

		// Token: 0x0600409D RID: 16541
		int GetNumberOfVillagesRaided();

		// Token: 0x0600409E RID: 16542
		int GetNumberOfCraftingPartsUnlocked();

		// Token: 0x0600409F RID: 16543
		int GetNumberOfWeaponsCrafted();

		// Token: 0x060040A0 RID: 16544
		int GetNumberOfCraftingOrdersCompleted();

		// Token: 0x060040A1 RID: 16545
		int GetNumberOfCompanionsHired();

		// Token: 0x060040A2 RID: 16546
		ulong GetTotalTimePlayedInSeconds();

		// Token: 0x060040A3 RID: 16547
		ulong GetTotalDenarsEarned();

		// Token: 0x060040A4 RID: 16548
		ulong GetDenarsEarnedFromCaravans();

		// Token: 0x060040A5 RID: 16549
		ulong GetDenarsEarnedFromWorkshops();

		// Token: 0x060040A6 RID: 16550
		ulong GetDenarsEarnedFromRansoms();

		// Token: 0x060040A7 RID: 16551
		ulong GetDenarsEarnedFromTaxes();

		// Token: 0x060040A8 RID: 16552
		ulong GetDenarsEarnedFromTributes();

		// Token: 0x060040A9 RID: 16553
		ulong GetDenarsPaidAsTributes();

		// Token: 0x060040AA RID: 16554
		CampaignTime GetTotalTimePlayed();

		// Token: 0x060040AB RID: 16555
		CampaignTime GetTimeSpentAsPrisoner();

		// Token: 0x060040AC RID: 16556
		ValueTuple<string, int> GetMostExpensiveItemCrafted();

		// Token: 0x060040AD RID: 16557
		[return: TupleElementNames(new string[] { "name", "value" })]
		ValueTuple<string, int> GetCompanionWithMostKills();

		// Token: 0x060040AE RID: 16558
		[return: TupleElementNames(new string[] { "name", "value" })]
		ValueTuple<string, int> GetCompanionWithMostIssuesSolved();
	}
}
