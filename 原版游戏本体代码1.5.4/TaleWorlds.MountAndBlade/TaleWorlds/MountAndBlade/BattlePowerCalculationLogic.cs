using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class BattlePowerCalculationLogic : MissionLogic, IBattlePowerCalculationLogic, IMissionBehavior
{
	private Dictionary<Team, float>[] _sidePowerData;

	public bool IsTeamPowersCalculated { get; private set; }

	public BattlePowerCalculationLogic()
	{
		_sidePowerData = new Dictionary<Team, float>[2];
		for (int i = 0; i < 2; i++)
		{
			_sidePowerData[i] = new Dictionary<Team, float>();
		}
		IsTeamPowersCalculated = false;
	}

	public float GetTotalTeamPower(Team team)
	{
		if (!IsTeamPowersCalculated)
		{
			CalculateTeamPowers();
		}
		return _sidePowerData[(int)team.Side][team];
	}

	private void CalculateTeamPowers()
	{
		Mission.TeamCollection teams = base.Mission.Teams;
		foreach (Team item in teams)
		{
			_sidePowerData[(int)item.Side].Add(item, 0f);
		}
		IMissionAgentSpawnLogic missionBehavior = base.Mission.GetMissionBehavior<IMissionAgentSpawnLogic>();
		for (int i = 0; i < 2; i++)
		{
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			IEnumerable<IAgentOriginBase> allTroopsForSide = missionBehavior.GetAllTroopsForSide(battleSideEnum);
			Dictionary<Team, float> dictionary = _sidePowerData[i];
			bool isPlayerSide = base.Mission.PlayerTeam != null && base.Mission.PlayerTeam.Side == battleSideEnum;
			foreach (IAgentOriginBase item2 in allTroopsForSide)
			{
				Team agentTeam = Mission.GetAgentTeam(item2, isPlayerSide);
				BasicCharacterObject troop = item2.Troop;
				dictionary[agentTeam] += troop.GetPower();
			}
		}
		foreach (Team item3 in teams)
		{
			item3.QuerySystem.Expire();
		}
		IsTeamPowersCalculated = true;
	}
}
