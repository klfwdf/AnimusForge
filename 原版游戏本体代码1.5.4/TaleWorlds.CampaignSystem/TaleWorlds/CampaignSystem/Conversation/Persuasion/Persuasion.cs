using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation.Persuasion;

public class Persuasion
{
	public readonly float SuccessValue;

	public readonly float FailValue;

	public readonly float CriticalSuccessValue;

	public readonly float CriticalFailValue;

	private readonly float _difficultyMultiplier;

	private readonly PersuasionDifficulty _difficulty;

	private readonly List<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> _chosenOptions;

	public readonly float GoalValue;

	public float DifficultyMultiplier => _difficultyMultiplier;

	public float Progress { get; private set; }

	public Persuasion(float goalValue, float successValue, float failValue, float criticalSuccessValue, float criticalFailValue, float initialProgress, PersuasionDifficulty difficulty)
	{
		_chosenOptions = new List<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
		GoalValue = Campaign.Current.Models.PersuasionModel.CalculatePersuasionGoalValue(CharacterObject.OneToOneConversationCharacter, goalValue);
		SuccessValue = successValue;
		FailValue = failValue;
		CriticalSuccessValue = criticalSuccessValue;
		CriticalFailValue = criticalFailValue;
		_difficulty = difficulty;
		if (initialProgress < 0f)
		{
			Progress = Campaign.Current.Models.PersuasionModel.CalculateInitialPersuasionProgress(CharacterObject.OneToOneConversationCharacter, GoalValue, SuccessValue);
		}
		else
		{
			Progress = initialProgress;
		}
		_difficultyMultiplier = Campaign.Current.Models.PersuasionModel.GetDifficulty(difficulty);
	}

	public void CommitProgress(PersuasionOptionArgs persuasionOptionArgs)
	{
		PersuasionOptionResult result = GetResult(persuasionOptionArgs);
		result = CheckPerkEffectOnResult(result);
		Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = new Tuple<PersuasionOptionArgs, PersuasionOptionResult>(persuasionOptionArgs, result);
		persuasionOptionArgs.BlockTheOption(isBlocked: true);
		_chosenOptions.Add(tuple);
		Progress = TaleWorlds.Library.MathF.Clamp(Progress + GetPersuasionOptionResultValue(result), 0f, GoalValue);
		CampaignEventDispatcher.Instance.OnPersuasionProgressCommitted(tuple);
	}

	private PersuasionOptionResult CheckPerkEffectOnResult(PersuasionOptionResult result)
	{
		PersuasionOptionResult result2 = result;
		if (result == PersuasionOptionResult.CriticalFailure && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.ForgivableGrievances) && MBRandom.RandomFloat <= DefaultPerks.Charm.ForgivableGrievances.PrimaryBonus)
		{
			TextObject textObject = new TextObject("{=5IQriov5}You avoided critical failure because of {PERK_NAME}.");
			textObject.SetTextVariable("PERK_NAME", DefaultPerks.Charm.ForgivableGrievances.Name);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.White));
			result2 = PersuasionOptionResult.Failure;
		}
		return result2;
	}

	private float GetPersuasionOptionResultValue(PersuasionOptionResult result)
	{
		return result switch
		{
			PersuasionOptionResult.Success => SuccessValue, 
			PersuasionOptionResult.CriticalSuccess => CriticalSuccessValue, 
			PersuasionOptionResult.CriticalFailure => 0f - CriticalFailValue, 
			PersuasionOptionResult.Failure => 0f, 
			PersuasionOptionResult.Miss => 0f, 
			_ => 0f, 
		};
	}

	private PersuasionOptionResult GetResult(PersuasionOptionArgs optionArgs)
	{
		Campaign.Current.Models.PersuasionModel.GetChances(optionArgs, out var successChance, out var critSuccessChance, out var critFailChance, out var failChance, _difficultyMultiplier);
		float randomFloat = MBRandom.RandomFloat;
		if (randomFloat < critSuccessChance)
		{
			return PersuasionOptionResult.CriticalSuccess;
		}
		randomFloat -= critSuccessChance;
		if (randomFloat < successChance)
		{
			return PersuasionOptionResult.Success;
		}
		randomFloat -= successChance;
		if (randomFloat < failChance)
		{
			return PersuasionOptionResult.Failure;
		}
		randomFloat -= failChance;
		if (randomFloat < critFailChance)
		{
			return PersuasionOptionResult.CriticalFailure;
		}
		return PersuasionOptionResult.Miss;
	}

	public IEnumerable<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> GetChosenOptions()
	{
		return _chosenOptions.AsReadOnly();
	}
}
