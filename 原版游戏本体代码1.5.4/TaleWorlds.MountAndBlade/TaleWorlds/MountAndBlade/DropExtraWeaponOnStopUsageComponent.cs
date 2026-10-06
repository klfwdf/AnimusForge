using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

internal class DropExtraWeaponOnStopUsageComponent : UsableMissionObjectComponent
{
	protected internal override void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
		if (isSuccessful && !GameNetwork.IsClientOrReplay && !userAgent.Equipment[EquipmentIndex.ExtraWeaponSlot].IsEmpty && !Mission.Current.MissionIsEnding)
		{
			userAgent.Mission.AddTickAction(Mission.MissionTickAction.DropItem, userAgent, 4, 0);
		}
	}
}
