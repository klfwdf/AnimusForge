using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class ModuleExtensions
{
	public static IEnumerable<UsableMachine> GetUsedMachines(this Formation formation)
	{
		return from d in formation.Detachments
			select d as UsableMachine into u
			where u != null
			select u;
	}

	public static void StartUsingMachine(this Formation formation, UsableMachine usable, bool isPlayerOrder = false)
	{
		if (isPlayerOrder || (formation.IsAIControlled && !Mission.Current.IsMissionEnding))
		{
			formation.JoinDetachment(usable);
		}
	}

	public static void StopUsingMachine(this Formation formation, UsableMachine usable, bool isPlayerOrder = false)
	{
		if (isPlayerOrder || formation.IsAIControlled)
		{
			formation.LeaveDetachment(usable);
		}
	}

	public static WorldPosition ToWorldPosition(this Vec3 rawPosition)
	{
		return new WorldPosition(Mission.Current.Scene, rawPosition);
	}
}
