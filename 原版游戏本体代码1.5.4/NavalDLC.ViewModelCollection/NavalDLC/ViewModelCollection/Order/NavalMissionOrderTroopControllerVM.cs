using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace NavalDLC.ViewModelCollection.Order;

public class NavalMissionOrderTroopControllerVM : MissionOrderTroopControllerVM
{
	private List<MissionOrderVM.ClassConfiguration> _classData;

	public NavalMissionOrderTroopControllerVM(MissionOrderVM missionOrder, bool isDeployment, Action onTransferFinised)
		: base(missionOrder, isDeployment, onTransferFinised)
	{
	}

	protected override OrderTroopItemVM CreateTroopItemVM(Formation formation, Action<OrderTroopItemVM> onSelectFormation, Func<Formation, int> getFormationMorale)
	{
		return new NavalOrderTroopItemVM(formation, onSelectFormation, getFormationMorale);
	}

	public void OnClassesSet(List<MissionOrderVM.ClassConfiguration> classData)
	{
		if (classData == null)
		{
			return;
		}
		_classData = classData;
		foreach (MissionOrderVM.ClassConfiguration classItem in classData)
		{
			if (base.TroopList.FirstOrDefault((OrderTroopItemVM f) => f.Formation.Index == classItem.FormationIndex) is NavalOrderTroopItemVM navalOrderTroopItemVM)
			{
				navalOrderTroopItemVM.UpdateClassData(classItem.FormationClass);
			}
			if (base.TransferTargetList.FirstOrDefault((OrderTroopItemVM f) => f.Formation.Index == classItem.FormationIndex) is NavalOrderTroopItemVM navalOrderTroopItemVM2)
			{
				navalOrderTroopItemVM2.UpdateClassData(classItem.FormationClass);
			}
		}
	}

	protected override void OnAfterNewTroopItemAdded()
	{
		base.OnAfterNewTroopItemAdded();
		OnClassesSet(_classData);
	}

	public override void SelectAllFormations(bool uiFeedback = true)
	{
		foreach (OrderSetVM orderSet in MissionOrder.OrderSets)
		{
			orderSet.ExecuteDeSelect();
		}
		if (base.TroopList.Count((OrderTroopItemVM x) => x.IsSelectable) == 1)
		{
			OnSelectFormation(base.TroopList.FirstOrDefault((OrderTroopItemVM x) => x.IsSelectable));
			return;
		}
		if (base.TroopList.Any((OrderTroopItemVM t) => t.IsSelectable))
		{
			base.OrderController.ClearSelectedFormations();
			if (Mission.Current.IsNavalBattle)
			{
				for (int num = 0; num < base.TroopList.Count; num++)
				{
					OrderTroopItemVM orderTroopItemVM = base.TroopList[num];
					if (!NavalDLCHelpers.IsPlayerCaptainOfFormationShip(orderTroopItemVM.Formation))
					{
						AddSelectedFormation(orderTroopItemVM);
					}
				}
			}
			else
			{
				base.OrderController.SelectAllFormations(uiFeedback);
			}
			if (uiFeedback && base.OrderController.SelectedFormations.Count > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=xTv4tCbZ}Everybody!! Listen to me").ToString()));
			}
		}
		MissionOrder.SetActiveOrders();
	}

	public override void AddSelectedFormation(OrderTroopItemVM item)
	{
		if (!item.IsSelectable)
		{
			return;
		}
		if (Mission.Current.IsNavalBattle)
		{
			if (IsOnlyPlayerFormationSelected() && !NavalDLCHelpers.IsPlayerCaptainOfFormationShip(item.Formation))
			{
				SetSelectedFormation(item);
				return;
			}
			if (NavalDLCHelpers.IsPlayerCaptainOfFormationShip(item.Formation))
			{
				base.OrderController.ClearSelectedFormations();
			}
		}
		Formation formation = base.Team.GetFormation(item.InitialFormationClass);
		base.OrderController.SelectFormation(formation);
		MissionOrder.SetActiveOrders();
	}

	private bool IsOnlyPlayerFormationSelected()
	{
		int num = 0;
		for (int i = 0; i < base.TroopList.Count; i++)
		{
			if (base.TroopList[i].IsSelected)
			{
				num++;
				if (!NavalDLCHelpers.IsPlayerCaptainOfFormationShip(base.TroopList[i].Formation))
				{
					return false;
				}
			}
			if (num > 1)
			{
				return false;
			}
		}
		return num == 1;
	}
}
