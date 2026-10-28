using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class RemoveExtraWeaponOnStopUsageComponent : UsableMissionObjectComponent
{
	protected internal override void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
		if (!GameNetwork.IsClientOrReplay && !userAgent.Equipment[EquipmentIndex.ExtraWeaponSlot].IsEmpty && !Mission.Current.MissionIsEnding)
		{
			userAgent.Mission.AddTickActionMT(Mission.MissionTickAction.RemoveEquippedWeapon, userAgent, 4, 0);
		}
	}
}
