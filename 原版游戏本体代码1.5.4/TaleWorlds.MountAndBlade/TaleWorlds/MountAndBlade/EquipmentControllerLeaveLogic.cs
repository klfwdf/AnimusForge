using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class EquipmentControllerLeaveLogic : MissionLogic
{
	public bool IsEquipmentSelectionActive { get; private set; }

	public void SetIsEquipmentSelectionActive(bool isActive)
	{
		IsEquipmentSelectionActive = isActive;
		Debug.Print("IsEquipmentSelectionActive: " + isActive);
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		canLeave = !IsEquipmentSelectionActive;
		return null;
	}
}
