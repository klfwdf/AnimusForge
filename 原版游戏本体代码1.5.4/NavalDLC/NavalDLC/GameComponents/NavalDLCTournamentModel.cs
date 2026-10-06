using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCTournamentModel : TournamentModel
{
	public override MBList<ItemObject> GetEliteRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
	{
		MBList<ItemObject> eliteRewardItems = base.BaseModel.GetEliteRewardItems(town, regularRewardMinValue, regularRewardMaxValue);
		string[] array = new string[2] { "head_breaker_2haxe", "world_chopper__1haxe" };
		foreach (string objectName in array)
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(objectName);
			if (itemObject != null)
			{
				eliteRewardItems.Add(itemObject);
			}
		}
		return eliteRewardItems;
	}

	public override MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
	{
		return base.BaseModel.GetRegularRewardItems(town, regularRewardMinValue, regularRewardMaxValue);
	}

	public override TournamentGame CreateTournament(Town town)
	{
		return base.BaseModel.CreateTournament(town);
	}

	public override int GetInfluenceReward(Hero winner, Town town)
	{
		return base.BaseModel.GetInfluenceReward(winner, town);
	}

	public override int GetNumLeaderboardVictoriesAtGameStart()
	{
		return base.BaseModel.GetNumLeaderboardVictoriesAtGameStart();
	}

	public override Equipment GetParticipantArmor(CharacterObject participant)
	{
		return base.BaseModel.GetParticipantArmor(participant);
	}

	public override int GetRenownReward(Hero winner, Town town)
	{
		return base.BaseModel.GetRenownReward(winner, town);
	}

	public override (SkillObject skill, int xp) GetSkillXpGainFromTournament(Town town)
	{
		return base.BaseModel.GetSkillXpGainFromTournament(town);
	}

	public override float GetTournamentEndChance(TournamentGame tournament)
	{
		return base.BaseModel.GetTournamentEndChance(tournament);
	}

	public override float GetTournamentSimulationScore(CharacterObject character)
	{
		return base.BaseModel.GetTournamentSimulationScore(character);
	}

	public override float GetTournamentStartChance(Town town)
	{
		return base.BaseModel.GetTournamentStartChance(town);
	}
}
