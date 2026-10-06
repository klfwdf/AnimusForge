using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;

namespace SandBox.ViewModelCollection.GameOver;

public class GameOverStatsProvider
{
	private readonly IStatisticsCampaignBehavior _statSource;

	public GameOverStatsProvider()
	{
		_statSource = Campaign.Current.GetCampaignBehavior<IStatisticsCampaignBehavior>();
	}

	public IEnumerable<StatCategory> GetGameOverStats()
	{
		yield return new StatCategory("General", GetGeneralStats(_statSource));
		yield return new StatCategory("Battle", GetBattleStats(_statSource));
		yield return new StatCategory("Finance", GetFinanceStats(_statSource));
		yield return new StatCategory("Crafting", GetCraftingStats(_statSource));
		yield return new StatCategory("Companion", GetCompanionStats(_statSource));
	}

	private IEnumerable<StatItem> GetGeneralStats(IStatisticsCampaignBehavior source)
	{
		int num = (int)source.GetTotalTimePlayed().ToYears;
		int num2 = (int)source.GetTotalTimePlayed().ToSeasons % CampaignTime.SeasonsInYear;
		int num3 = (int)source.GetTotalTimePlayed().ToDays % CampaignTime.DaysInSeason;
		GameTexts.SetVariable("YEAR_IS_PLURAL", (num != 1) ? 1 : 0);
		GameTexts.SetVariable("YEAR", num);
		GameTexts.SetVariable("SEASON_IS_PLURAL", (num2 != 1) ? 1 : 0);
		GameTexts.SetVariable("SEASON", num2);
		GameTexts.SetVariable("DAY_IS_PLURAL", (num3 != 1) ? 1 : 0);
		GameTexts.SetVariable("DAY", num3);
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_YEAR_years"));
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_SEASON_seasons"));
		string content = GameTexts.FindText("str_STR1_space_STR2").ToString();
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_DAY_days"));
		content = GameTexts.FindText("str_STR1_space_STR2").ToString();
		yield return new StatItem("CampaignPlayTime", content);
		string content2 = $"{TimeSpan.FromSeconds(source.GetTotalTimePlayedInSeconds()).TotalHours:0.##}";
		GameTexts.SetVariable("PLURAL_HOURS", 1);
		GameTexts.SetVariable("HOUR", content2);
		yield return new StatItem("CampaignRealPlayTime", GameTexts.FindText("str_hours").ToString());
		yield return new StatItem("ChildrenBorn", source.GetNumberOfChildrenBorn().ToString());
		yield return new StatItem("InfluenceEarned", source.GetTotalInfluenceEarned().ToString(), StatItem.StatType.Influence);
		yield return new StatItem("IssuesSolved", source.GetNumberOfIssuesSolved().ToString(), StatItem.StatType.Issue);
		yield return new StatItem("TournamentsWon", source.GetNumberOfTournamentWins().ToString(), StatItem.StatType.Tournament);
		yield return new StatItem("HighestLeaderboardRank", source.GetHighestTournamentRank().ToString());
		yield return new StatItem("PrisonersRecruited", source.GetNumberOfPrisonersRecruited().ToString());
		yield return new StatItem("TroopsRecruited", source.GetNumberOfTroopsRecruited().ToString());
		yield return new StatItem("ClansDefected", source.GetNumberOfClansDefected().ToString());
		yield return new StatItem("TotalCrimeGained", source.GetTotalCrimeRatingGained().ToString(), StatItem.StatType.Crime);
	}

	private IEnumerable<StatItem> GetBattleStats(IStatisticsCampaignBehavior source)
	{
		int numberOfBattlesWon = source.GetNumberOfBattlesWon();
		int numberOfBattlesLost = source.GetNumberOfBattlesLost();
		int content = numberOfBattlesWon + numberOfBattlesLost;
		GameTexts.SetVariable("BATTLES_WON", numberOfBattlesWon);
		GameTexts.SetVariable("BATTLES_LOST", numberOfBattlesLost);
		GameTexts.SetVariable("ALL_BATTLES", content);
		yield return new StatItem("BattlesWonLostAll", GameTexts.FindText("str_battles_won_lost").ToString());
		yield return new StatItem("BiggestBattleWonAsLeader", source.GetLargestBattleWonAsLeader().ToString());
		yield return new StatItem("BiggestArmyByPlayer", source.GetLargestArmyFormedByPlayer().ToString());
		yield return new StatItem("EnemyClansDestroyed", source.GetNumberOfEnemyClansDestroyed().ToString());
		yield return new StatItem("HeroesKilledInBattle", source.GetNumberOfHeroesKilledInBattle().ToString(), StatItem.StatType.Kill);
		yield return new StatItem("TroopsEliminatedByPlayer", source.GetNumberOfTroopsKnockedOrKilledByPlayer().ToString(), StatItem.StatType.Kill);
		yield return new StatItem("TroopsEliminatedByParty", source.GetNumberOfTroopsKnockedOrKilledAsParty().ToString(), StatItem.StatType.Kill);
		yield return new StatItem("HeroPrisonersTaken", source.GetNumberOfHeroPrisonersTaken().ToString());
		yield return new StatItem("TroopPrisonersTaken", source.GetNumberOfTroopPrisonersTaken().ToString());
		yield return new StatItem("CapturedTowns", source.GetNumberOfTownsCaptured().ToString());
		yield return new StatItem("CapturedCastles", source.GetNumberOfCastlesCaptured().ToString());
		yield return new StatItem("ClearedHideouts", source.GetNumberOfHideoutsCleared().ToString());
		yield return new StatItem("RaidedVillages", source.GetNumberOfVillagesRaided().ToString());
		double toDays = source.GetTimeSpentAsPrisoner().ToDays;
		string content2 = $"{toDays:0.##}";
		GameTexts.SetVariable("DAY_IS_PLURAL", 1);
		GameTexts.SetVariable("DAY", content2);
		yield return new StatItem("DaysSpentAsPrisoner", GameTexts.FindText("str_DAY_days").ToString());
	}

	private IEnumerable<StatItem> GetFinanceStats(IStatisticsCampaignBehavior source)
	{
		yield return new StatItem("TotalDenarsEarned", source.GetTotalDenarsEarned().ToString("0.##"), StatItem.StatType.Gold);
		yield return new StatItem("DenarsFromCaravans", source.GetDenarsEarnedFromCaravans().ToString("0.##"), StatItem.StatType.Gold);
		yield return new StatItem("DenarsFromWorkshops", source.GetDenarsEarnedFromWorkshops().ToString("0.##"), StatItem.StatType.Gold);
		yield return new StatItem("DenarsFromRansoms", source.GetDenarsEarnedFromRansoms().ToString("0.##"), StatItem.StatType.Gold);
		yield return new StatItem("DenarsFromTaxes", source.GetDenarsEarnedFromTaxes().ToString("0.##"), StatItem.StatType.Gold);
		yield return new StatItem("TributeCollected", source.GetDenarsEarnedFromTributes().ToString("0.##"), StatItem.StatType.Gold);
	}

	private IEnumerable<StatItem> GetCraftingStats(IStatisticsCampaignBehavior source)
	{
		yield return new StatItem("WeaponsCrafted", source.GetNumberOfWeaponsCrafted().ToString());
		string content = source.GetMostExpensiveItemCrafted().Item1 ?? GameTexts.FindText("str_no_items_crafted").ToString();
		GameTexts.SetVariable("LEFT", content);
		GameTexts.SetVariable("RIGHT", source.GetMostExpensiveItemCrafted().Item2.ToString());
		yield return new StatItem("MostExpensiveCraft", GameTexts.FindText("str_LEFT_over_RIGHT").ToString(), StatItem.StatType.Gold);
		yield return new StatItem("NumberOfPiecesUnlocked", source.GetNumberOfCraftingPartsUnlocked().ToString());
		yield return new StatItem("CraftingOrdersCompleted", source.GetNumberOfCraftingOrdersCompleted().ToString());
	}

	private IEnumerable<StatItem> GetCompanionStats(IStatisticsCampaignBehavior source)
	{
		yield return new StatItem("NumberOfHiredCompanions", source.GetNumberOfCompanionsHired().ToString());
		string content = source.GetCompanionWithMostIssuesSolved().name ?? GameTexts.FindText("str_no_companions_with_issues_solved").ToString();
		GameTexts.SetVariable("LEFT", content);
		GameTexts.SetVariable("RIGHT", source.GetCompanionWithMostIssuesSolved().value);
		yield return new StatItem("MostIssueCompanion", GameTexts.FindText("str_LEFT_over_RIGHT").ToString(), StatItem.StatType.Issue);
		string content2 = source.GetCompanionWithMostKills().name ?? GameTexts.FindText("str_no_companions_with_kills").ToString();
		GameTexts.SetVariable("LEFT", content2);
		GameTexts.SetVariable("RIGHT", source.GetCompanionWithMostKills().value);
		yield return new StatItem("MostKillCompanion", GameTexts.FindText("str_LEFT_over_RIGHT").ToString(), StatItem.StatType.Kill);
	}
}
