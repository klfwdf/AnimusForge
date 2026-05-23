using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPersuasionModel : PersuasionModel
{
	public override int GetSkillXpFromPersuasion(PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient)
	{
		return (int)(difficulty - 0) / 1 * 5 * argumentDifficultyBonusCoefficient;
	}

	public override void GetChances(PersuasionOptionArgs optionArgs, out float successChance, out float critSuccessChance, out float critFailChance, out float failChance, float difficultyMultiplier)
	{
		float defaultSuccessChance = GetDefaultSuccessChance(optionArgs, difficultyMultiplier);
		switch (optionArgs.ArgumentStrength)
		{
		case PersuasionArgumentStrength.ExtremelyHard:
			successChance = defaultSuccessChance * 0.05f;
			critFailChance = defaultSuccessChance * 0.15f;
			break;
		case PersuasionArgumentStrength.VeryHard:
			successChance = defaultSuccessChance * 0.15f;
			critFailChance = defaultSuccessChance * 0.2f;
			break;
		case PersuasionArgumentStrength.Hard:
			successChance = defaultSuccessChance * 0.35f;
			critFailChance = defaultSuccessChance * 0.15f;
			break;
		case PersuasionArgumentStrength.Easy:
			successChance = defaultSuccessChance * 0.7f;
			critFailChance = 0f;
			break;
		case PersuasionArgumentStrength.VeryEasy:
			successChance = defaultSuccessChance * 0.8f;
			critFailChance = 0f;
			break;
		case PersuasionArgumentStrength.ExtremelyEasy:
			successChance = defaultSuccessChance * 0.9f;
			critFailChance = 0f;
			break;
		default:
			successChance = defaultSuccessChance * 0.55f;
			critFailChance = defaultSuccessChance * 0.15f;
			break;
		}
		float bonusSuccessChance = GetBonusSuccessChance(optionArgs);
		successChance = TaleWorlds.Library.MathF.Clamp(successChance * bonusSuccessChance, 0f, 1f);
		critFailChance = TaleWorlds.Library.MathF.Clamp(critFailChance * (2f - bonusSuccessChance), 0f, 1f);
		critSuccessChance = 0f;
		if (optionArgs.GivesCriticalSuccess)
		{
			critSuccessChance = successChance;
			successChance = 0f;
		}
		if (critSuccessChance > 0f && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.MeaningfulFavors))
		{
			critSuccessChance = TaleWorlds.Library.MathF.Clamp(critSuccessChance + critSuccessChance * DefaultPerks.Charm.MeaningfulFavors.PrimaryBonus, 0f, 1f);
		}
		failChance = 1f - critSuccessChance - successChance - critFailChance;
	}

	private float GetDefaultSuccessChance(PersuasionOptionArgs optionArgs, float difficultyMultiplier)
	{
		int skillValue = Hero.MainHero.GetSkillValue(optionArgs.SkillUsed);
		float num = ((optionArgs.TraitEffect == TraitEffect.Positive) ? Hero.MainHero.GetTraitLevel(optionArgs.TraitUsed) : (-Hero.MainHero.GetTraitLevel(optionArgs.TraitUsed)));
		num = MBMath.ClampFloat((optionArgs.TraitUsed != null) ? num : 0f, -1f, 2f);
		float num2 = 0f;
		num2 = ((num <= 0f) ? (0.2f + (num - -1f) * 0.4f) : ((!(num <= 1f)) ? (1.2f + (num - 1f) * 0.4f) : (0.6f + (num - 0f) * 0.6f)));
		return TaleWorlds.Library.MathF.Clamp((100f - difficultyMultiplier / (0.01f * (100f + (float)skillValue * num2))) / 100f, 0.1f, 1f);
	}

	private float GetBonusSuccessChance(PersuasionOptionArgs optionArgs)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		explainedNumber.AddFactor(TaleWorlds.Library.MathF.Clamp((float)(Hero.MainHero.GetSkillValue(optionArgs.SkillUsed) / Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus) * 0.2f, 0f, 0.2f), optionArgs.SkillUsed.Name);
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Athletics.ImposingStature))
		{
			explainedNumber.AddFactor(DefaultPerks.Athletics.ImposingStature.PrimaryBonus, DefaultPerks.Athletics.ImposingStature.Name);
		}
		float persuasionBonusChance = Campaign.Current.Models.DifficultyModel.GetPersuasionBonusChance();
		if (persuasionBonusChance > 0f)
		{
			explainedNumber.AddFactor(persuasionBonusChance, GameTexts.FindText("str_game_difficulty"));
		}
		return explainedNumber.ResultNumber;
	}

	public override void GetEffectChances(PersuasionOptionArgs option, out float moveToNextStageChance, out float blockRandomOptionChance, float difficultyMultiplier)
	{
		moveToNextStageChance = 0f;
		blockRandomOptionChance = 0f;
		if (option.CanMoveToTheNextReservation || option.CanBlockOtherOption)
		{
			moveToNextStageChance = (option.CanMoveToTheNextReservation ? 1f : 0f);
			blockRandomOptionChance = (option.CanBlockOtherOption ? 1f : 0f);
		}
	}

	public override PersuasionArgumentStrength GetArgumentStrengthBasedOnTargetTraits(CharacterObject character, Tuple<TraitObject, int>[] traitCorrelations)
	{
		float num = 0f;
		float num2 = 1f;
		for (int i = 0; i < traitCorrelations.Length; i++)
		{
			num2 += (float)(character.GetTraitLevel(traitCorrelations[i].Item1) * traitCorrelations[i].Item2);
			num += (float)TaleWorlds.Library.MathF.Abs(traitCorrelations[i].Item2);
			num2 += (float)(CharacterObject.PlayerCharacter.GetTraitLevel(traitCorrelations[i].Item1) * traitCorrelations[i].Item2);
			num += (float)TaleWorlds.Library.MathF.Abs(traitCorrelations[i].Item2);
		}
		if (num2 > num / 6f)
		{
			return PersuasionArgumentStrength.VeryEasy;
		}
		if (num2 > 0f)
		{
			return PersuasionArgumentStrength.ExtremelyEasy;
		}
		if (num2 < num / -6f)
		{
			return PersuasionArgumentStrength.VeryHard;
		}
		if (num2 < 0f)
		{
			return PersuasionArgumentStrength.Hard;
		}
		return PersuasionArgumentStrength.Normal;
	}

	public override float CalculateInitialPersuasionProgress(CharacterObject character, float goalValue, float successValue)
	{
		if (!character.IsHero)
		{
			return 0f;
		}
		return TaleWorlds.Library.MathF.Max(0f, character.HeroObject.GetRelationWithPlayer() * successValue) / (float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit;
	}

	public override float CalculatePersuasionGoalValue(CharacterObject oneToOneConversationCharacter, float successValue)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(successValue);
		if (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.IsHero)
		{
			if (CharacterObject.OneToOneConversationCharacter.HeroObject.Culture == Hero.MainHero.Culture && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.MoralLeader))
			{
				explainedNumber.Add(DefaultPerks.Charm.MoralLeader.PrimaryBonus, DefaultPerks.Charm.MoralLeader.Name);
			}
			else if (CharacterObject.OneToOneConversationCharacter.HeroObject.Culture != Hero.MainHero.Culture && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.NaturalLeader))
			{
				explainedNumber.Add(DefaultPerks.Charm.NaturalLeader.PrimaryBonus, DefaultPerks.Charm.NaturalLeader.Name);
			}
		}
		explainedNumber.LimitMin(1f);
		return explainedNumber.ResultNumber;
	}

	public override float GetDifficulty(PersuasionDifficulty difficulty)
	{
		return difficulty switch
		{
			PersuasionDifficulty.VeryEasy => 0.9f, 
			PersuasionDifficulty.Easy => 0.8f, 
			PersuasionDifficulty.EasyMedium => 0.7f, 
			PersuasionDifficulty.Medium => 0.6f, 
			PersuasionDifficulty.MediumHard => 0.5f, 
			PersuasionDifficulty.Hard => 0.4f, 
			PersuasionDifficulty.VeryHard => 0.3f, 
			PersuasionDifficulty.UltraHard => 0.2f, 
			PersuasionDifficulty.Impossible => 0.1f, 
			_ => 1f, 
		};
	}
}
