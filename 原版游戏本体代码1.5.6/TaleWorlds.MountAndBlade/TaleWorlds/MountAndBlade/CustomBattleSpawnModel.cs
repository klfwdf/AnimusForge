using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleSpawnModel : BattleSpawnModel
{
	public override void OnMissionStart()
	{
		MissionReinforcementsHelper.OnMissionStart();
	}

	public override void OnMissionEnd()
	{
		MissionReinforcementsHelper.OnMissionEnd();
	}

	public override List<(IAgentOriginBase origin, int formationIndex)> GetInitialSpawnAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
	{
		List<(IAgentOriginBase, int)> list = new List<(IAgentOriginBase, int)>();
		foreach (IAgentOriginBase troopOrigin in troopOrigins)
		{
			(IAgentOriginBase, int) item = (troopOrigin, (int)Mission.Current.GetAgentTroopClass(battleSide, troopOrigin.Troop));
			list.Add(item);
		}
		return list;
	}

	public override List<(IAgentOriginBase origin, int formationIndex)> GetReinforcementAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
	{
		return MissionReinforcementsHelper.GetReinforcementAssignments(battleSide, troopOrigins);
	}
}
