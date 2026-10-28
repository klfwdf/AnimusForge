using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultDifficultyModel : DifficultyModel
{
	public override float GetPlayerTroopsReceivedDamageMultiplier()
	{
		return CampaignOptions.PlayerTroopsReceivedDamage switch
		{
			CampaignOptions.Difficulty.VeryEasy => 0.5f, 
			CampaignOptions.Difficulty.Easy => 0.75f, 
			CampaignOptions.Difficulty.Realistic => 1f, 
			_ => 1f, 
		};
	}

	public override int GetPlayerRecruitSlotBonus()
	{
		return CampaignOptions.RecruitmentDifficulty switch
		{
			CampaignOptions.Difficulty.VeryEasy => 2, 
			CampaignOptions.Difficulty.Easy => 1, 
			CampaignOptions.Difficulty.Realistic => 0, 
			_ => 0, 
		};
	}

	public override float GetPlayerMapMovementSpeedBonusMultiplier()
	{
		return CampaignOptions.PlayerMapMovementSpeed switch
		{
			CampaignOptions.Difficulty.VeryEasy => 0.1f, 
			CampaignOptions.Difficulty.Easy => 0.05f, 
			CampaignOptions.Difficulty.Realistic => 0f, 
			_ => 0f, 
		};
	}

	public override float GetStealthDifficultyMultiplier()
	{
		return CampaignOptions.StealthAndDisguiseDifficulty switch
		{
			CampaignOptions.Difficulty.VeryEasy => 0.5f, 
			CampaignOptions.Difficulty.Easy => 0.75f, 
			CampaignOptions.Difficulty.Realistic => 1f, 
			_ => 0f, 
		};
	}

	public override float GetDisguiseDifficultyMultiplier()
	{
		return CampaignOptions.StealthAndDisguiseDifficulty switch
		{
			CampaignOptions.Difficulty.VeryEasy => 0.4f, 
			CampaignOptions.Difficulty.Easy => 1f, 
			CampaignOptions.Difficulty.Realistic => 1.2f, 
			_ => 0f, 
		};
	}

	public override float GetCombatAIDifficultyMultiplier()
	{
		return CampaignOptions.CombatAIDifficulty switch
		{
			CampaignOptions.Difficulty.VeryEasy => 0f, 
			CampaignOptions.Difficulty.Easy => 0.5f, 
			_ => 1f, 
		};
	}

	public override float GetPersuasionBonusChance()
	{
		return CampaignOptions.PersuasionSuccessChance switch
		{
			CampaignOptions.Difficulty.VeryEasy => 0.1f, 
			CampaignOptions.Difficulty.Easy => 0.05f, 
			CampaignOptions.Difficulty.Realistic => 0f, 
			_ => 0f, 
		};
	}

	public override float GetClanMemberDeathChanceMultiplier()
	{
		return CampaignOptions.ClanMemberDeathChance switch
		{
			CampaignOptions.Difficulty.VeryEasy => -1f, 
			CampaignOptions.Difficulty.Easy => -0.5f, 
			CampaignOptions.Difficulty.Realistic => 0f, 
			_ => 0f, 
		};
	}
}
