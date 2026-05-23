using System.Collections.Generic;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public class DetachmentData
{
	public List<Formation> joinedFormations = new List<Formation>();

	public List<(Agent, List<float>)> agentScores = new List<(Agent, List<float>)>();

	public int MovingAgentCount;

	public int DefendingAgentCount;

	public float firstTime;

	public int AgentCount => joinedFormations.SumQ((Formation f) => f.CountOfDetachableNonPlayerUnits) + MovingAgentCount + DefendingAgentCount;

	public bool IsPrecalculated()
	{
		int count = agentScores.Count;
		if (count > 0)
		{
			return count >= AgentCount;
		}
		return false;
	}

	public DetachmentData()
	{
		firstTime = MBCommon.GetTotalMissionTime();
	}

	public void RemoveScoreOfAgent(Agent agent)
	{
		for (int num = agentScores.Count - 1; num >= 0; num--)
		{
			if (agentScores[num].Item1 == agent)
			{
				agentScores.RemoveAt(num);
				break;
			}
		}
	}
}
