namespace TaleWorlds.MountAndBlade.Source.Missions;

public class MissionOptionsComponent : MissionLogic
{
	public event OnMissionAddOptionsDelegate OnOptionsAdded;

	public void OnAddOptionsUIHandler()
	{
		if (this.OnOptionsAdded != null)
		{
			this.OnOptionsAdded();
		}
	}
}
