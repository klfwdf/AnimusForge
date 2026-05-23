using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000199 RID: 409
	public abstract class PersuasionModel : MBGameModel<PersuasionModel>
	{
		// Token: 0x06001C34 RID: 7220
		public abstract int GetSkillXpFromPersuasion(PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient);

		// Token: 0x06001C35 RID: 7221
		public abstract void GetChances(PersuasionOptionArgs optionArgs, out float successChance, out float critSuccessChance, out float critFailChance, out float failChance, float difficultyMultiplier);

		// Token: 0x06001C36 RID: 7222
		public abstract void GetEffectChances(PersuasionOptionArgs option, out float moveToNextStageChance, out float blockRandomOptionChance, float difficultyMultiplier);

		// Token: 0x06001C37 RID: 7223
		public abstract PersuasionArgumentStrength GetArgumentStrengthBasedOnTargetTraits(CharacterObject character, Tuple<TraitObject, int>[] traitCorrelation);

		// Token: 0x06001C38 RID: 7224
		public abstract float GetDifficulty(PersuasionDifficulty difficulty);

		// Token: 0x06001C39 RID: 7225
		public abstract float CalculateInitialPersuasionProgress(CharacterObject character, float goalValue, float successValue);

		// Token: 0x06001C3A RID: 7226
		public abstract float CalculatePersuasionGoalValue(CharacterObject oneToOneConversationCharacter, float successValue);
	}
}
