using TaleWorlds.MountAndBlade.Missions.Hints;

namespace TaleWorlds.MountAndBlade.Missions.MissionLogics;

public class MissionHintLogic : MissionLogic
{
	public delegate void MissionHintChangedDelegate(MissionHint previousHint, MissionHint newHint);

	public MissionHint ActiveHint { get; private set; }

	public event MissionHintChangedDelegate OnActiveHintChanged;

	public void SetActiveHint(MissionHint hint)
	{
		MissionHint activeHint = ActiveHint;
		ActiveHint = hint;
		this.OnActiveHintChanged?.Invoke(activeHint, ActiveHint);
	}

	public void Clear()
	{
		SetActiveHint(null);
	}
}
