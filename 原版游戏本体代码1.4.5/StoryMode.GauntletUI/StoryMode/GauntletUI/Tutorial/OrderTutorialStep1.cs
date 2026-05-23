using System.Collections.Generic;
using System.Linq;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("OrderTutorial1TutorialStep1")]
public class OrderTutorialStep1 : TutorialItemBase
{
	private bool _hasPlayerOrderedFollowMe;

	private bool _registeredToOrderEvent;

	public OrderTutorialStep1()
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
		return _hasPlayerOrderedFollowMe;
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
		_hasPlayerOrderedFollowMe = _hasPlayerOrderedFollowMe || (orderType == OrderType.FollowMe && appliedFormations.Any());
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.CurrentContext == TutorialContexts.Mission && TutorialHelper.IsPlayerInABattleMission && Mission.Current.Mode != MissionMode.Deployment)
		{
			return TutorialHelper.IsOrderingAvailable;
		}
		return false;
	}
}
