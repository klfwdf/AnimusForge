using NavalDLC.Storyline;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("ShipCommandingShipsTutorial")]
public class ShipCommandingShipsTutorial : TutorialItemBase
{
	private int _lastController;

	private bool _registeredToOrderEvent;

	private bool _hasOrderedCharge;

	public ShipCommandingShipsTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		PirateBattleMissionController pirateBattleMissionController = Mission.Current?.GetMissionBehavior<PirateBattleMissionController>();
		if (pirateBattleMissionController != null && pirateBattleMissionController.HasSelectedShip)
		{
			if (_lastController != pirateBattleMissionController.GetHashCode())
			{
				_hasOrderedCharge = false;
				_registeredToOrderEvent = false;
				_lastController = pirateBattleMissionController.GetHashCode();
			}
			if (pirateBattleMissionController.HasSelectedShip)
			{
				if (!_registeredToOrderEvent && Mission.Current?.PlayerTeam?.PlayerOrderController != null)
				{
					Mission current = Mission.Current;
					if (current != null && current.Mode == MissionMode.Battle)
					{
						Mission.Current.PlayerTeam.PlayerOrderController.OnOrderIssued += OnPlayerOrdered;
						_registeredToOrderEvent = true;
					}
				}
				return _hasOrderedCharge;
			}
		}
		return false;
	}

	private void OnPlayerOrdered(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, object[] delegateParams)
	{
		_hasOrderedCharge = _hasOrderedCharge || orderType == OrderType.Advance;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Mission.Current == null || !Mission.Current.IsNavalBattle)
		{
			return false;
		}
		PirateBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<PirateBattleMissionController>();
		if (missionBehavior != null && missionBehavior.IsFirstShipCleared)
		{
			return missionBehavior.HasSelectedShip;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}
}
