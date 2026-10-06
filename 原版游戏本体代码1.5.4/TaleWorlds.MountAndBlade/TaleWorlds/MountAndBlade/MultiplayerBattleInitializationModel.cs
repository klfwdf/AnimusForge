using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerBattleInitializationModel : BattleInitializationModel
{
	public override List<FormationClass> GetAllAvailableTroopTypes()
	{
		return new List<FormationClass>();
	}

	protected override bool CanPlayerSideDeployWithOrderOfBattleAux()
	{
		return false;
	}
}
