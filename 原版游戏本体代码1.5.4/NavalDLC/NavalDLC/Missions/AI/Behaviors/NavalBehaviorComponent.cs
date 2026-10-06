using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Behaviors;

public abstract class NavalBehaviorComponent : BehaviorComponent
{
	public NavalBehaviorComponent(Formation formation)
		: base(formation)
	{
	}

	public abstract void RefreshShipReferences();
}
