using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class DifficultyModel : MBGameModel<DifficultyModel>
{
	public abstract float GetPlayerTroopsReceivedDamageMultiplier();

	public abstract int GetPlayerRecruitSlotBonus();

	public abstract float GetPlayerMapMovementSpeedBonusMultiplier();

	public abstract float GetCombatAIDifficultyMultiplier();

	public abstract float GetPersuasionBonusChance();

	public abstract float GetClanMemberDeathChanceMultiplier();

	public abstract float GetStealthDifficultyMultiplier();

	public abstract float GetDisguiseDifficultyMultiplier();
}
