namespace TaleWorlds.MountAndBlade.Source.Missions;

public class MissionOptionsComponent : MissionLogic
{
	public event OnMissionAddOptionsDelegate OnOptionsAdded;

	public void OnAddOptionsUIHandler()
	{
		if (OnOptionsAdded != null)
		{
			OnOptionsAdded();
		}
	}
}
