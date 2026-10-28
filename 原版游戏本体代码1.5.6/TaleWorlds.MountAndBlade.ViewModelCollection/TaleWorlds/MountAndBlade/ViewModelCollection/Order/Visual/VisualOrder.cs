using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public abstract class VisualOrder
{
	protected OrderState _lastActiveState;

	public string StringId { get; }

	public string IconId => GetIconId();

	public VisualOrder(string stringId)
	{
		StringId = stringId;
	}

	protected virtual string GetIconId()
	{
		return StringId;
	}

	public abstract TextObject GetName(OrderController orderController);

	public abstract bool IsTargeted();

	public abstract void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters);

	public virtual void BeforeExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
	}

	public virtual void AfterExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
	}

	protected abstract bool? OnGetFormationHasOrder(Formation formation);

	public bool GetFormationHasOrder(Formation formation)
	{
		return OnGetFormationHasOrder(formation) == true;
	}

	public OrderState GetActiveState(OrderController orderController)
	{
		_lastActiveState = GetActiveStateAux(orderController);
		return _lastActiveState;
	}

	private OrderState GetActiveStateAux(OrderController orderController)
	{
		if (orderController.SelectedFormations == null || orderController.SelectedFormations.Count == 0)
		{
			return OrderState.Default;
		}
		int num = orderController.SelectedFormations.Count;
		int num2 = 0;
		MBReadOnlyList<Formation> selectedFormations = orderController.SelectedFormations;
		for (int i = 0; i < selectedFormations.Count; i++)
		{
			Formation formation = selectedFormations[i];
			bool? flag = OnGetFormationHasOrder(formation);
			if (!flag.HasValue)
			{
				num--;
			}
			else if (flag == true)
			{
				num2++;
			}
		}
		if (num2 == 0)
		{
			return OrderState.Default;
		}
		if (num2 < num)
		{
			return OrderState.PartiallyActive;
		}
		if (num2 == num)
		{
			return OrderState.Active;
		}
		return OrderState.Default;
	}
}
