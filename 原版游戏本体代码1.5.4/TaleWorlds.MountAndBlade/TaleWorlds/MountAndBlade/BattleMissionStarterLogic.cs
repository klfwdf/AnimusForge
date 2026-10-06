using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class BattleMissionStarterLogic : MissionLogic
{
	public BattleMissionStarterLogic()
	{
	}

	public BattleMissionStarterLogic(IMissionTroopSupplier defenderTroopSupplier = null, IMissionTroopSupplier attackerTroopSupplier = null)
	{
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
	}
}
