using System;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public struct MissionOrderCallbacks
{
	public delegate void OnRefreshVisualsDelegate();

	public delegate void OnToggleActivateOrderStateDelegate();

	public delegate void OnTransferTroopsFinishedDelegate();

	public delegate void OnBeforeOrderDelegate();

	public delegate void ToggleOrderPositionVisibilityDelegate(bool value);

	public delegate VisualOrderExecutionParameters GetOrderExecutionParametersDelegate();

	public OnRefreshVisualsDelegate RefreshVisuals;

	public OnToggleActivateOrderStateDelegate OnActivateToggleOrder;

	public OnToggleActivateOrderStateDelegate OnDeactivateToggleOrder;

	public OnTransferTroopsFinishedDelegate OnTransferTroopsFinished;

	public OnBeforeOrderDelegate OnBeforeOrder;

	public Action<bool> ToggleMissionInputs;

	public ToggleOrderPositionVisibilityDelegate SetSuspendTroopPlacer;

	public GetOrderExecutionParametersDelegate GetVisualOrderExecutionParameters;
}
