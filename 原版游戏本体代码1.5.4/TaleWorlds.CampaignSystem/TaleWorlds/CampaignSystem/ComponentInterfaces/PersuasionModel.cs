using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PersuasionModel : MBGameModel<PersuasionModel>
{
	public abstract int GetSkillXpFromPersuasion(PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient);

	public abstract void GetChances(PersuasionOptionArgs optionArgs, out float successChance, out float critSuccessChance, out float critFailChance, out float failChance, float difficultyMultiplier);

	public abstract void GetEffectChances(PersuasionOptionArgs option, out float moveToNextStageChance, out float blockRandomOptionChance, float difficultyMultiplier);

	public abstract PersuasionArgumentStrength GetArgumentStrengthBasedOnTargetTraits(CharacterObject character, Tuple<TraitObject, int>[] traitCorrelation);

	public abstract float GetDifficulty(PersuasionDifficulty difficulty);

	public abstract float CalculateInitialPersuasionProgress(CharacterObject character, float goalValue, float successValue);

	public abstract float CalculatePersuasionGoalValue(CharacterObject oneToOneConversationCharacter, float successValue);
}
