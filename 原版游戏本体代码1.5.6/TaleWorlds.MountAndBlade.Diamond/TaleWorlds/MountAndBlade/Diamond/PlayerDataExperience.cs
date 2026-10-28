using System;

namespace TaleWorlds.MountAndBlade.Diamond;

public struct PlayerDataExperience
{
	private static int[] _levelToXP;

	private static readonly int _maxLevelForXPRequirementCalculation;

	public int Experience { get; private set; }

	public int Level => CalculateLevelFromExperience(Experience);

	public int ExperienceToNextLevel => CalculateExperienceFromLevel(Level + 1) - Experience;

	public int ExperienceInCurrentLevel => Experience - CalculateExperienceFromLevel(Level);

	static PlayerDataExperience()
	{
		_maxLevelForXPRequirementCalculation = 30;
		InitializeXPRequirements();
	}

	public PlayerDataExperience(int experience)
	{
		Experience = experience;
	}

	public static int CalculateLevelFromExperience(int experience)
	{
		int num = 1;
		int num2 = 0;
		while (num2 <= experience)
		{
			num2 += ExperienceRequiredForLevel(num + 1);
			if (num2 <= experience)
			{
				num++;
			}
		}
		return num;
	}

	public static int CalculateExperienceFromLevel(int level)
	{
		if (level == 1)
		{
			return 0;
		}
		if (level < _maxLevelForXPRequirementCalculation)
		{
			return _levelToXP[level];
		}
		return ExperienceRequiredForLevel(level) + CalculateExperienceFromLevel(level - 1);
	}

	public static int ExperienceRequiredForLevel(int level)
	{
		return Convert.ToInt32(Math.Floor(100.0 * Math.Pow(level - 1, 1.03)));
	}

	private static void InitializeXPRequirements()
	{
		_levelToXP = new int[_maxLevelForXPRequirementCalculation];
		int num = 0;
		for (int i = 2; i < _maxLevelForXPRequirementCalculation; i++)
		{
			num += ExperienceRequiredForLevel(i);
			_levelToXP[i] = num;
		}
	}
}
