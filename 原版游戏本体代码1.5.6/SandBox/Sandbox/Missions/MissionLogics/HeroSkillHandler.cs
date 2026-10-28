using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class HeroSkillHandler : MissionLogic
{
	private MissionTime _nextCaptainSkillMoraleBoostTime;

	private bool _boostMorale;

	private int _nextMoraleTeam;

	public override void AfterStart()
	{
		_nextCaptainSkillMoraleBoostTime = MissionTime.SecondsFromNow(10f);
	}

	public override void OnMissionTick(float dt)
	{
		if (_nextCaptainSkillMoraleBoostTime.IsPast)
		{
			_boostMorale = true;
			_nextMoraleTeam = 0;
			_nextCaptainSkillMoraleBoostTime = MissionTime.SecondsFromNow(10f);
		}
		if (_boostMorale)
		{
			if (_nextMoraleTeam >= base.Mission.Teams.Count)
			{
				_boostMorale = false;
				return;
			}
			Team team = base.Mission.Teams[_nextMoraleTeam];
			BoostMoraleForTeam(team);
			_nextMoraleTeam++;
		}
	}

	private void BoostMoraleForTeam(Team team)
	{
	}
}
