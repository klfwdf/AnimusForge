using System;

namespace TaleWorlds.MountAndBlade;

public class FormationPocket
{
	public Func<Agent, int> PriorityFunction { get; private set; }

	public int MaxValue { get; private set; }

	public int TroopCount { get; private set; }

	public int Index { get; private set; }

	public int AddedTroopCount { get; private set; }

	public int ScoreToSeek { get; private set; }

	public int BestScoreSoFar { get; private set; }

	public FormationPocket(Func<Agent, int> priorityFunction, int maxValue, int troopCount, int index)
	{
		PriorityFunction = priorityFunction;
		MaxValue = maxValue;
		TroopCount = troopCount;
		Index = index;
		AddedTroopCount = 0;
		ScoreToSeek = maxValue;
		BestScoreSoFar = 0;
	}

	public void AddTroop()
	{
		AddedTroopCount++;
	}

	public bool IsFormationPocketFilled()
	{
		return AddedTroopCount >= TroopCount;
	}

	public void UpdateScoreToSeek()
	{
		ScoreToSeek = BestScoreSoFar;
		BestScoreSoFar = 0;
	}

	public void SetBestScoreSoFar(int bestScoreSoFar)
	{
		BestScoreSoFar = bestScoreSoFar;
	}
}
