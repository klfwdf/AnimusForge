using System.Collections.Generic;
using System.Linq;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("OrderTutorial2Tutorial")]
public class OrderHideoutTutorial : TutorialItemBase
{
	private bool _hasPlayerOrderedFollowme;

	private bool _registeredToOrderEvent;

	public OrderHideoutTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "";
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
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
		return _hasPlayerOrderedFollowme;
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		if (_registeredToOrderEvent && Mission.Current?.PlayerTeam?.PlayerOrderController != null)
		{
			Mission.Current.PlayerTeam.PlayerOrderController.OnOrderIssued -= OnPlayerOrdered;
		}
		_registeredToOrderEvent = false;
	}

	private void OnPlayerOrdered(OrderType orderType, IEnumerable<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		_hasPlayerOrderedFollowme = _hasPlayerOrderedFollowme || (orderType == OrderType.FollowMe && appliedFormations.Any());
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.CurrentContext == TutorialContexts.Mission && TutorialHelper.IsPlayerInAHideoutBattleMission)
		{
			return TutorialHelper.IsOrderingAvailable;
		}
		return false;
	}
}
