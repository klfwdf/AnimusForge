using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class LeaveMissionLogic : MissionLogic
{
	private string _menuId;

	private Timer _isAgentDeadTimer;

	public LeaveMissionLogic(string leaveMenuId = "settlement_player_unconscious")
	{
		_menuId = leaveMenuId;
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (base.Mission.MainAgent != null)
		{
			return !base.Mission.MainAgent.IsActive();
		}
		return false;
	}

	public override void OnMissionTick(float dt)
	{
		if (Agent.Main == null || !Agent.Main.IsActive())
		{
			if (_isAgentDeadTimer == null)
			{
				_isAgentDeadTimer = new Timer(Mission.Current.CurrentTime, 5f);
			}
			if (_isAgentDeadTimer.Check(Mission.Current.CurrentTime))
			{
				Mission.Current.NextCheckTimeEndMission = 0f;
				Mission.Current.EndMission();
				Campaign.Current.GameMenuManager.SetNextMenu(_menuId);
			}
		}
		else if (_isAgentDeadTimer != null)
		{
			_isAgentDeadTimer = null;
		}
	}
}
