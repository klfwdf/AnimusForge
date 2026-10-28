using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace NavalDLC.ViewModelCollection.Order;

public class NavalMissionOrderVM : MissionOrderVM
{
	private List<ClassConfiguration> _classData;

	public NavalMissionOrderVM(OrderController orderController, bool isDeployment, bool isMultiplayer)
		: base(orderController, isDeployment, isMultiplayer)
	{
		RefreshValues();
	}

	protected override MissionOrderTroopControllerVM CreateTroopController(OrderController orderController)
	{
		return new NavalMissionOrderTroopControllerVM(this, base.IsDeployment, base.OnTransferFinished);
	}

	public void OnClassesSet(List<ClassConfiguration> classData)
	{
		_classData = classData;
		(base.TroopController as NavalMissionOrderTroopControllerVM).OnClassesSet(_classData);
	}

	public override void OnOrderLayoutTypeChanged()
	{
		base.OnOrderLayoutTypeChanged();
		(base.TroopController as NavalMissionOrderTroopControllerVM).OnClassesSet(_classData);
	}
}
