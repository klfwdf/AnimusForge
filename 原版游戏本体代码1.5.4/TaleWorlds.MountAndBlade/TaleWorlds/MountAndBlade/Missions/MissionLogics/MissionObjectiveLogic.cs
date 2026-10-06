using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TaleWorlds.MountAndBlade.Missions.MissionLogics;

public class MissionObjectiveLogic : MissionLogic
{
	private MissionObjective _currentObjective;

	public MissionObjective CurrentObjective => _currentObjective;

	public void StartObjective(MissionObjective objective)
	{
		if (objective == null || objective.IsStarted || objective.IsCompleted)
		{
			Debug.FailedAssert("Trying to start an invalid mission objective.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\MissionObjectiveLogic.cs", "StartObjective", 20);
			return;
		}
		CompleteCurrentObjective();
		_currentObjective = objective;
		if (_currentObjective != null && _currentObjective.GetIsActivationRequirementsMet())
		{
			StartObjectiveAux();
		}
	}

	private void StartObjectiveAux()
	{
		Debug.Print("Mission: Start objective: " + _currentObjective.UniqueId);
		_currentObjective.Start();
	}

	public void CompleteCurrentObjective()
	{
		if (_currentObjective != null)
		{
			Debug.Print("Mission: Complete objective: " + _currentObjective.UniqueId);
			_currentObjective.Complete();
			_currentObjective = null;
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_currentObjective != null && !_currentObjective.IsStarted && _currentObjective.GetIsActivationRequirementsMet())
		{
			StartObjectiveAux();
		}
		_currentObjective?.Tick(dt);
		if (_currentObjective != null && _currentObjective.IsStarted && _currentObjective.GetIsCompletionRequirementsMet())
		{
			CompleteCurrentObjective();
		}
	}
}
