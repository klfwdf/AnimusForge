using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Missions.Objectives;

public abstract class MissionObjectiveTarget
{
	public abstract bool IsActive();

	public abstract TextObject GetName();

	public abstract Vec3 GetGlobalPosition();
}
public abstract class MissionObjectiveTarget<T> : MissionObjectiveTarget
{
	public T Target { get; }

	public MissionObjectiveTarget(T target)
	{
		Target = target;
	}
}
