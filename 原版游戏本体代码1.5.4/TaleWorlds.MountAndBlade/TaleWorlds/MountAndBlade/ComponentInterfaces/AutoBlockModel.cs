using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class AutoBlockModel : MBGameModel<AutoBlockModel>
{
	public abstract Agent.UsageDirection GetBlockDirection(Mission mission);
}
