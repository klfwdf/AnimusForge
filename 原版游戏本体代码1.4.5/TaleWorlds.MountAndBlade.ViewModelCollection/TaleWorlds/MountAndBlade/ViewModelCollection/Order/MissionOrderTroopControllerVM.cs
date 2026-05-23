using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class MissionOrderTroopControllerVM : ViewModel
{
	protected class TroopItemFormationIndexComparer : IComparer<OrderTroopItemVM>
	{
		public int Compare(OrderTroopItemVM x, OrderTroopItemVM y)
		{
			int index = x.Formation.Index;
			return index.CompareTo(y.Formation.Index);
		}
	}

	protected readonly MissionOrderVM MissionOrder;

	protected readonly Action OnTransferFinished;

	protected List<MissionOrderVM.FormationConfiguration> FilterData;

	protected bool IsDeployment;

	protected TroopItemFormationIndexComparer FormationIndexComparer;

	private readonly OrderTroopItemVM _emptyTroopItemVM;

	private bool _isTransferActive;

	private MBBindingList<OrderTroopItemVM> _transferTargetList;

	private int _transferValue;

	private int _transferMaxValue;

	private string _transferTitleText;

	private string _acceptText;

	private string _cancelText;

	private bool _isTransferValid;

	private OrderTroopItemVM _troopItem0;

	private OrderTroopItemVM _troopItem1;

	private OrderTroopItemVM _troopItem2;

	private OrderTroopItemVM _troopItem3;

	private OrderTroopItemVM _troopItem4;

	private OrderTroopItemVM _troopItem5;

	private OrderTroopItemVM _troopItem6;

	private OrderTroopItemVM _troopItem7;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _resetInputKey;

	public MBList<OrderTroopItemVM> TroopList { get; private set; }

	private Agent MainAgent => Mission.Current.MainAgent;

	protected Team Team => Mission.Current.PlayerTeam;

	protected OrderController OrderController => Mission.Current.PlayerTeam.PlayerOrderController;

	[DataSourceProperty]
	public bool IsTransferActive
	{
		get
		{
			return _isTransferActive;
		}
		set
		{
			if (value == _isTransferActive)
			{
				return;
			}
			_isTransferActive = value;
			OnPropertyChangedWithValue(value, "IsTransferActive");
			MissionOrder.IsTroopPlacingActive = !value;
			for (int i = 0; i < MissionOrder.OrderSets.Count; i++)
			{
				MissionOrder.OrderSets[i].ExecuteDeSelect();
			}
			if (_isTransferActive)
			{
				foreach (OrderTroopItemVM transferTarget in TransferTargetList)
				{
					transferTarget.SetFormationClassFromFormation(transferTarget.Formation);
					transferTarget.Morale = (int)MissionGameModels.Current.BattleMoraleModel.GetAverageMorale(transferTarget.Formation);
					transferTarget.IsAmmoAvailable = transferTarget.Formation.QuerySystem.RangedUnitRatio > 0f || transferTarget.Formation.QuerySystem.RangedCavalryUnitRatio > 0f;
				}
			}
			if (Mission.Current != null)
			{
				Mission.Current.IsTransferMenuOpen = value;
			}
		}
	}

	[DataSourceProperty]
	public bool IsTransferValid
	{
		get
		{
			return _isTransferValid;
		}
		set
		{
			if (value != _isTransferValid)
			{
				_isTransferValid = value;
				OnPropertyChangedWithValue(value, "IsTransferValid");
				if (!value)
				{
					TransferTitleText = "";
				}
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderTroopItemVM> TransferTargetList
	{
		get
		{
			return _transferTargetList;
		}
		set
		{
			if (value != _transferTargetList)
			{
				_transferTargetList = value;
				OnPropertyChangedWithValue(value, "TransferTargetList");
			}
		}
	}

	[DataSourceProperty]
	public int TransferMaxValue
	{
		get
		{
			return _transferMaxValue;
		}
		set
		{
			if (value != _transferMaxValue)
			{
				_transferMaxValue = value;
				OnPropertyChangedWithValue(value, "TransferMaxValue");
			}
		}
	}

	[DataSourceProperty]
	public int TransferValue
	{
		get
		{
			return _transferValue;
		}
		set
		{
			if (value != _transferValue)
			{
				_transferValue = value;
				OnPropertyChangedWithValue(value, "TransferValue");
			}
		}
	}

	[DataSourceProperty]
	public string TransferTitleText
	{
		get
		{
			return _transferTitleText;
		}
		set
		{
			if (value != _transferTitleText)
			{
				_transferTitleText = value;
				OnPropertyChangedWithValue(value, "TransferTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string AcceptText
	{
		get
		{
			return _acceptText;
		}
		set
		{
			if (value != _acceptText)
			{
				_acceptText = value;
				OnPropertyChangedWithValue(value, "AcceptText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem0
	{
		get
		{
			return _troopItem0;
		}
		set
		{
			if (value != _troopItem0)
			{
				_troopItem0 = value;
				OnPropertyChangedWithValue(value, "TroopItem0");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem1
	{
		get
		{
			return _troopItem1;
		}
		set
		{
			if (value != _troopItem1)
			{
				_troopItem1 = value;
				OnPropertyChangedWithValue(value, "TroopItem1");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem2
	{
		get
		{
			return _troopItem2;
		}
		set
		{
			if (value != _troopItem2)
			{
				_troopItem2 = value;
				OnPropertyChangedWithValue(value, "TroopItem2");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem3
	{
		get
		{
			return _troopItem3;
		}
		set
		{
			if (value != _troopItem3)
			{
				_troopItem3 = value;
				OnPropertyChangedWithValue(value, "TroopItem3");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem4
	{
		get
		{
			return _troopItem4;
		}
		set
		{
			if (value != _troopItem4)
			{
				_troopItem4 = value;
				OnPropertyChangedWithValue(value, "TroopItem4");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem5
	{
		get
		{
			return _troopItem5;
		}
		set
		{
			if (value != _troopItem5)
			{
				_troopItem5 = value;
				OnPropertyChangedWithValue(value, "TroopItem5");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem6
	{
		get
		{
			return _troopItem6;
		}
		set
		{
			if (value != _troopItem6)
			{
				_troopItem6 = value;
				OnPropertyChangedWithValue(value, "TroopItem6");
			}
		}
	}

	[DataSourceProperty]
	public OrderTroopItemVM TroopItem7
	{
		get
		{
			return _troopItem7;
		}
		set
		{
			if (value != _troopItem7)
			{
				_troopItem7 = value;
				OnPropertyChangedWithValue(value, "TroopItem7");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
			}
		}
	}

	public MissionOrderTroopControllerVM(MissionOrderVM missionOrder, bool isDeployment, Action onTransferFinised)
	{
		MissionOrder = missionOrder;
		OnTransferFinished = onTransferFinised;
		_emptyTroopItemVM = new OrderTroopItemVM();
		IsDeployment = isDeployment;
		TroopList = new MBList<OrderTroopItemVM>();
		TransferTargetList = new MBBindingList<OrderTroopItemVM>();
		for (int i = 0; i < 8; i++)
		{
			Formation formation = Team.GetFormation((FormationClass)i);
			OrderTroopItemVM orderTroopItemVM = CreateTroopItemVM(formation, ExecuteSelectTransferTroop, GetFormationMorale);
			TransferTargetList.Add(orderTroopItemVM);
			orderTroopItemVM.IsSelected = false;
		}
		FormationIndexComparer = new TroopItemFormationIndexComparer();
		SortFormations();
		OrderController.OnOrderIssued += OrderController_OnTroopOrderIssued;
		OrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TroopList.ForEach(delegate(OrderTroopItemVM x)
		{
			x.RefreshValues();
		});
		AcceptText = GameTexts.FindText("str_selection_widget_accept").ToString();
		CancelText = GameTexts.FindText("str_selection_widget_cancel").ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		OrderController.OnOrderIssued -= OrderController_OnTroopOrderIssued;
		OrderController.OnSelectedFormationsChanged -= OrderController_OnSelectedFormationsChanged;
		_emptyTroopItemVM.OnFinalize();
		TroopList.ForEach(delegate(OrderTroopItemVM x)
		{
			x.OnFinalize();
		});
		TroopList.Clear();
		TransferTargetList.Clear();
	}

	public void ExecuteSelectAll()
	{
		SelectAllFormations();
	}

	public void ExecuteSelectTransferTroop(OrderTroopItemVM targetTroop)
	{
		foreach (OrderTroopItemVM transferTarget in TransferTargetList)
		{
			transferTarget.IsSelected = false;
		}
		targetTroop.IsSelected = targetTroop.IsSelectable;
		IsTransferValid = targetTroop.IsSelectable;
		GameTexts.SetVariable("FORMATION_INDEX", targetTroop.FormationName);
		TransferTitleText = new TextObject("{=DvnRkWQg}Transfer Troops To {FORMATION_INDEX}").ToString();
	}

	public void ExecuteConfirmTransfer()
	{
		IsTransferActive = false;
		OrderTroopItemVM orderTroopItemVM = TransferTargetList.Single((OrderTroopItemVM t) => t.IsSelected);
		int transferValue = TransferValue;
		int b = TroopList.Where((OrderTroopItemVM t) => t.IsSelected).Sum((OrderTroopItemVM t) => t.CurrentMemberCount);
		transferValue = TaleWorlds.Library.MathF.Min(transferValue, b);
		OrderController.SetOrderWithFormationAndNumber(OrderType.Transfer, orderTroopItemVM.Formation, transferValue);
		for (int num = 0; num < TroopList.Count; num++)
		{
			OrderTroopItemVM orderTroopItemVM2 = TroopList[num];
			if (!orderTroopItemVM2.ContainsDeadTroop && orderTroopItemVM2.CurrentMemberCount == 0)
			{
				TroopList.RemoveAt(num);
				num--;
			}
		}
		OnTransferFinished?.DynamicInvokeWithLog();
		UpdateTroops();
	}

	public void ExecuteCancelTransfer()
	{
		IsTransferActive = false;
		OnTransferFinished?.DynamicInvokeWithLog();
	}

	public void ExecuteReset()
	{
		RefreshValues();
	}

	public void SetTroopActiveOrders(OrderTroopItemVM item)
	{
		item.ClearActiveOrders();
		foreach (OrderSetVM orderSet in MissionOrder.OrderSets)
		{
			foreach (OrderItemVM order in orderSet.Orders)
			{
				if (order.Order.GetFormationHasOrder(item.Formation))
				{
					item.AddActiveOrder(order);
				}
			}
		}
	}

	public virtual void SelectAllFormations(bool uiFeedback = true)
	{
		foreach (OrderSetVM orderSet in MissionOrder.OrderSets)
		{
			orderSet.ExecuteDeSelect();
		}
		if (TroopList.Any((OrderTroopItemVM t) => t.IsSelectable))
		{
			OrderController.ClearSelectedFormations();
			OrderController.SelectAllFormations(uiFeedback);
			if (uiFeedback && OrderController.SelectedFormations.Count > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=xTv4tCbZ}Everybody!! Listen to me").ToString()));
			}
		}
		MissionOrder.SetActiveOrders();
	}

	public virtual void AddSelectedFormation(OrderTroopItemVM item)
	{
		if (item.IsSelectable)
		{
			Formation formation = Team.GetFormation(item.InitialFormationClass);
			OrderController.SelectFormation(formation);
			MissionOrder.SetActiveOrders();
		}
	}

	public void SetSelectedFormation(OrderTroopItemVM item)
	{
		UpdateTroops();
		if (item.IsSelectable)
		{
			OrderController.ClearSelectedFormations();
			AddSelectedFormation(item);
		}
	}

	public void OnDeselectFormation(int index)
	{
		OrderTroopItemVM item = TroopList.FirstOrDefault((OrderTroopItemVM t) => t.Formation.Index == index);
		OnDeselectFormation(item);
	}

	public void OnDeselectFormation(OrderTroopItemVM item)
	{
		if (item == null)
		{
			return;
		}
		Formation formation = Team.GetFormation(item.InitialFormationClass);
		if (OrderController.SelectedFormations.Contains(formation))
		{
			OrderController.DeselectFormation(formation);
		}
		if (IsDeployment)
		{
			if (TroopList.Count((OrderTroopItemVM t) => t.IsSelected) != 0)
			{
				MissionOrder.SetActiveOrders();
				return;
			}
			MissionOrder.TryCloseToggleOrder();
			MissionOrder.IsTroopPlacingActive = false;
		}
		else
		{
			MissionOrder.SetActiveOrders();
		}
	}

	public void OnSelectFormation(OrderTroopItemVM item)
	{
		foreach (OrderSetVM orderSet in MissionOrder.OrderSets)
		{
			orderSet.ExecuteDeSelect();
		}
		UpdateTroops();
		MissionOrder.IsTroopPlacingActive = true;
		if (TaleWorlds.InputSystem.Input.IsKeyDown(InputKey.LeftControl))
		{
			if (item.IsSelected)
			{
				OnDeselectFormation(item);
			}
			else
			{
				AddSelectedFormation(item);
			}
		}
		else
		{
			SetSelectedFormation(item);
		}
		if (!IsTransferActive)
		{
			return;
		}
		foreach (OrderTroopItemVM transferTarget in TransferTargetList)
		{
			transferTarget.IsSelectable = !OrderController.IsFormationListening(transferTarget.Formation);
		}
		IsTransferValid = TransferTargetList.Any((OrderTroopItemVM t) => t.IsSelected && t.IsSelectable);
		TransferMaxValue = TroopList.Where((OrderTroopItemVM t) => t.IsSelected).Sum((OrderTroopItemVM t) => t.CurrentMemberCount);
		TransferValue = TransferMaxValue;
	}

	private void UpdateFormationSelectedStates()
	{
		for (int i = 0; i < TroopList.Count; i++)
		{
			OrderTroopItemVM orderTroopItemVM = TroopList[i];
			orderTroopItemVM.IsSelectable = OrderController.IsFormationSelectable(orderTroopItemVM.Formation);
			if (orderTroopItemVM.IsSelectable && OrderController.IsFormationListening(orderTroopItemVM.Formation))
			{
				orderTroopItemVM.IsSelected = true;
			}
			else if (!orderTroopItemVM.IsSelectable && orderTroopItemVM.IsSelected)
			{
				OnDeselectFormation(orderTroopItemVM);
			}
		}
	}

	protected virtual OrderTroopItemVM CreateTroopItemVM(Formation formation, Action<OrderTroopItemVM> onSelectFormation, Func<Formation, int> getFormationMorale)
	{
		return new OrderTroopItemVM(formation, onSelectFormation, getFormationMorale);
	}

	public void UpdateTroops()
	{
		List<Formation> list = ((MainAgent == null || MainAgent.Controller == AgentControllerType.Player) ? Team.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && (!f.IsPlayerTroopInFormation || f.CountOfUnits > 1)).ToList() : Team.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0).ToList());
		for (int num = 0; num < list.Count; num++)
		{
			Formation formation = list[num];
			if (formation != null && !TroopList.Any((OrderTroopItemVM item) => item.Formation == formation))
			{
				AddTroopItemIfNotExist(CreateTroopItemVM(formation, OnSelectFormation, GetFormationMorale));
			}
		}
		for (int num2 = 0; num2 < TroopList.Count; num2++)
		{
			TroopList[num2].UpdateVisuals();
		}
		SortFormations();
		UpdateFormationSelectedStates();
		RefreshTroopItemBindings();
	}

	public void AddTroops(Agent agent)
	{
		if (agent.Team == Team && agent.Formation != null && !agent.IsPlayerControlled)
		{
			Formation formation = agent.Formation;
			OrderTroopItemVM orderTroopItemVM = TroopList.FirstOrDefault((OrderTroopItemVM item) => item.Formation.FormationIndex == formation.FormationIndex);
			if (orderTroopItemVM == null)
			{
				AddTroopItemIfNotExist(CreateTroopItemVM(formation, OnSelectFormation, GetFormationMorale));
			}
			else
			{
				orderTroopItemVM.SetFormationClassFromFormation(formation);
			}
			SortFormations();
			UpdateFormationSelectedStates();
		}
	}

	public void RemoveTroops(Agent agent)
	{
		if (agent.Team == Team && agent.Formation != null)
		{
			Formation formation = agent.Formation;
			OrderTroopItemVM orderTroopItemVM = TroopList.FirstOrDefault((OrderTroopItemVM item) => item.Formation.FormationIndex == formation.FormationIndex);
			if (orderTroopItemVM != null)
			{
				orderTroopItemVM.OnFormationAgentRemoved(agent);
				orderTroopItemVM.SetFormationClassFromFormation(formation);
			}
			UpdateFormationSelectedStates();
		}
	}

	public void OnTroopOrderIssued(List<OrderTroopItemVM> selectedFormations, OrderItemVM orderItem)
	{
		foreach (OrderSetVM orderSet in MissionOrder.OrderSets)
		{
			orderSet.RefreshOrderStates();
		}
		MissionOrder.SelectedOrderSet?.ExecuteDeSelect();
	}

	private void OrderController_OnTroopOrderIssued(OrderType orderType, IEnumerable<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		if (orderType == OrderType.Transfer)
		{
			if (!(delegateParams[1] is object[]))
			{
				_ = (int)delegateParams[1];
			}
			Formation formation = delegateParams[0] as Formation;
			OrderTroopItemVM orderTroopItemVM = TroopList.FirstOrDefault((OrderTroopItemVM item) => item.Formation == formation);
			if (orderTroopItemVM == null)
			{
				int index = -1;
				for (int num = 0; num < TroopList.Count; num++)
				{
					if (TroopList[num].Formation.Index > formation.Index)
					{
						index = num;
						break;
					}
				}
				AddTroopItemIfNotExist(CreateTroopItemVM(formation, OnSelectFormation, GetFormationMorale), index);
			}
			else
			{
				orderTroopItemVM.SetFormationClassFromFormation(formation);
			}
			foreach (Formation sourceFormation in appliedFormations)
			{
				OrderTroopItemVM orderTroopItemVM2 = TroopList.FirstOrDefault((OrderTroopItemVM item) => item.Formation == sourceFormation);
				if (orderTroopItemVM2 == null)
				{
					int index2 = -1;
					for (int num2 = 0; num2 < TroopList.Count; num2++)
					{
						if (TroopList[num2].Formation.Index > sourceFormation.Index)
						{
							index2 = num2;
							break;
						}
					}
					AddTroopItemIfNotExist(CreateTroopItemVM(sourceFormation, OnSelectFormation, GetFormationMorale), index2);
				}
				else
				{
					orderTroopItemVM2.SetFormationClassFromFormation(sourceFormation);
				}
			}
			int num3 = 1;
			foreach (Formation sourceFormation2 in appliedFormations)
			{
				TroopList.FirstOrDefault((OrderTroopItemVM item) => item.Formation.Index == sourceFormation2.Index).SetFormationClassFromFormation(sourceFormation2);
				num3++;
			}
		}
		UpdateTroops();
		foreach (OrderTroopItemVM item in TroopList.Where((OrderTroopItemVM item) => item.IsSelected))
		{
			SetTroopActiveOrders(item);
		}
		MissionOrder.SetActiveOrders();
		UpdateFormationSelectedStates();
		switch (orderType)
		{
		case OrderType.Move:
		case OrderType.MoveToLineSegment:
		case OrderType.MoveToLineSegmentWithHorizontalLayout:
		{
			OrderItemVM orderItemVM2 = FindOrderWithId("order_movement_move");
			if (orderItemVM2 != null)
			{
				MissionOrder.OnOrderExecuted(orderItemVM2);
			}
			break;
		}
		case OrderType.Charge:
		case OrderType.Advance:
		{
			OrderItemVM orderItemVM = null;
			switch (orderType)
			{
			case OrderType.Charge:
				orderItemVM = FindOrderWithId("order_movement_charge") ?? FindOrderWithId("order_movement_advance");
				break;
			case OrderType.Advance:
				orderItemVM = FindOrderWithId("order_movement_advance") ?? FindOrderWithId("order_movement_charge");
				break;
			}
			if (orderItemVM != null)
			{
				MissionOrder.OnOrderExecuted(orderItemVM);
			}
			break;
		}
		}
		if (MissionOrder.IsToggleOrderShown && !MissionOrder.IsDeployment && !MissionOrder.IsHolding)
		{
			MissionOrder.TryCloseToggleOrder();
		}
	}

	private OrderItemVM FindOrderWithId(string orderId)
	{
		for (int i = 0; i < MissionOrder.OrderSets.Count; i++)
		{
			OrderSetVM orderSetVM = MissionOrder.OrderSets[i];
			for (int j = 0; j < orderSetVM.Orders.Count; j++)
			{
				OrderItemVM orderItemVM = orderSetVM.Orders[j];
				if (orderItemVM.OrderIconId == orderId)
				{
					return orderItemVM;
				}
			}
		}
		return null;
	}

	private void OrderController_OnSelectedFormationsChanged()
	{
		for (int i = 0; i < TroopList.Count; i++)
		{
			OrderTroopItemVM orderTroopItemVM = TroopList[i];
			orderTroopItemVM.IsSelected = OrderController.IsFormationListening(orderTroopItemVM.Formation);
		}
	}

	internal void Update()
	{
		for (int i = 0; i < TroopList.Count; i++)
		{
			TroopList[i].Update();
		}
	}

	public void IntervalUpdate()
	{
		UpdateTroops();
		for (int num = TroopList.Count - 1; num >= 0; num--)
		{
			OrderTroopItemVM orderTroopItemVM = TroopList[num];
			Formation formation = orderTroopItemVM.Formation;
			if (formation != null && formation.CountOfUnits > 0)
			{
				orderTroopItemVM.UnderAttackOfType = (int)formation.GetUnderAttackTypeOfUnits();
				orderTroopItemVM.BehaviorType = (int)formation.GetMovementTypeOfUnits();
				if (!IsDeployment)
				{
					orderTroopItemVM.Morale = (int)MissionGameModels.Current.BattleMoraleModel.GetAverageMorale(formation);
					if (orderTroopItemVM.SetFormationClassFromFormation(formation))
					{
						UpdateTroops();
					}
					if (formation.QuerySystem.RangedUnitRatio > 0f || formation.QuerySystem.RangedCavalryUnitRatio > 0f)
					{
						int totalCurrentAmmo = 0;
						int totalMaxAmmo = 0;
						orderTroopItemVM.Formation.ApplyActionOnEachUnit(delegate(Agent agent)
						{
							if (!agent.IsMainAgent)
							{
								GetMaxAndCurrentAmmoOfAgent(agent, out var currentAmmo, out var maxAmmo);
								totalCurrentAmmo += currentAmmo;
								totalMaxAmmo += maxAmmo;
							}
						});
						if (totalMaxAmmo > 0)
						{
							orderTroopItemVM.IsAmmoAvailable = true;
							orderTroopItemVM.AmmoPercentage = (float)totalCurrentAmmo / (float)totalMaxAmmo;
						}
						else
						{
							orderTroopItemVM.IsAmmoAvailable = false;
						}
					}
					else
					{
						orderTroopItemVM.IsAmmoAvailable = false;
					}
				}
			}
			else if (formation != null && formation.CountOfUnits == 0)
			{
				orderTroopItemVM.Morale = 0;
				orderTroopItemVM.SetFormationClassFromFormation(formation);
			}
		}
	}

	public void RefreshTroopFormationTargetVisuals()
	{
		for (int i = 0; i < TroopList.Count; i++)
		{
			TroopList[i].RefreshTargetedOrderVisual();
		}
	}

	public void OnSelectFormationWithIndex(int formationTroopIndex)
	{
		UpdateTroops();
		OrderTroopItemVM orderTroopItemVM = null;
		if (formationTroopIndex >= 0)
		{
			orderTroopItemVM = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.Formation.Index == formationTroopIndex && x.IsSelectable);
		}
		if (orderTroopItemVM != null)
		{
			OnSelectFormation(orderTroopItemVM);
		}
		else
		{
			SelectAllFormations();
		}
	}

	public void SetCurrentActiveOrders()
	{
		foreach (OrderTroopItemVM troop in TroopList)
		{
			if (troop.IsSelected)
			{
				SetTroopActiveOrders(troop);
			}
		}
	}

	private void GetMaxAndCurrentAmmoOfAgent(Agent agent, out int currentAmmo, out int maxAmmo)
	{
		currentAmmo = 0;
		maxAmmo = 0;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
		{
			if (!agent.Equipment[equipmentIndex].IsEmpty && agent.Equipment[equipmentIndex].CurrentUsageItem.IsRangedWeapon)
			{
				currentAmmo = agent.Equipment.GetAmmoAmount(equipmentIndex);
				maxAmmo = agent.Equipment.GetMaxAmmo(equipmentIndex);
				break;
			}
		}
	}

	public void OnFiltersSet(List<MissionOrderVM.FormationConfiguration> filterData)
	{
		if (filterData == null)
		{
			return;
		}
		FilterData = filterData;
		foreach (MissionOrderVM.FormationConfiguration filter in filterData)
		{
			TroopList.FirstOrDefault((OrderTroopItemVM f) => f.Formation.Index == filter.FormationIndex)?.UpdateFilterData(filter.Filters);
			TransferTargetList.FirstOrDefault((OrderTroopItemVM f) => f.Formation.Index == filter.FormationIndex)?.UpdateFilterData(filter.Filters);
		}
	}

	public void OnDeploymentFinished()
	{
		IsDeployment = false;
		for (int num = TroopList.Count - 1; num >= 0; num--)
		{
			if (TroopList[num].CurrentMemberCount <= 0)
			{
				TroopList.RemoveAt(num);
			}
		}
		SortFormations();
		OrderController.ClearSelectedFormations();
	}

	public void OnAfterDeploymentFinished()
	{
		UpdateTroops();
	}

	private void SortFormations()
	{
		TroopList.Sort(FormationIndexComparer.Compare);
		TransferTargetList.Sort(FormationIndexComparer);
	}

	private int GetFormationMorale(Formation formation)
	{
		if (!IsDeployment)
		{
			return (int)MissionGameModels.Current.BattleMoraleModel.GetAverageMorale(formation);
		}
		return 0;
	}

	private OrderTroopItemVM AddTroopItemIfNotExist(OrderTroopItemVM troopItem, int index = -1)
	{
		OrderTroopItemVM orderTroopItemVM = null;
		if (troopItem != null)
		{
			bool flag = true;
			orderTroopItemVM = TroopList.FirstOrDefault((OrderTroopItemVM t) => t.Formation.Index == troopItem.Formation.Index);
			if (orderTroopItemVM == null)
			{
				flag = false;
				orderTroopItemVM = troopItem;
			}
			if (flag)
			{
				TroopList.Remove(orderTroopItemVM);
			}
			if (index == -1)
			{
				TroopList.Add(troopItem);
			}
			else
			{
				TroopList.Insert(index, troopItem);
			}
		}
		else
		{
			Debug.FailedAssert("Added troop item is null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\MissionOrderTroopControllerVM.cs", "AddTroopItemIfNotExist", 736);
		}
		OnAfterNewTroopItemAdded();
		RefreshTroopItemBindings();
		return orderTroopItemVM;
	}

	protected virtual void OnAfterNewTroopItemAdded()
	{
		OnFiltersSet(FilterData);
	}

	private void RefreshTroopItemBindings()
	{
		TroopItem0 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 0) ?? _emptyTroopItemVM;
		TroopItem1 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 1) ?? _emptyTroopItemVM;
		TroopItem2 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 2) ?? _emptyTroopItemVM;
		TroopItem3 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 3) ?? _emptyTroopItemVM;
		TroopItem4 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 4) ?? _emptyTroopItemVM;
		TroopItem5 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 5) ?? _emptyTroopItemVM;
		TroopItem6 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 6) ?? _emptyTroopItemVM;
		TroopItem7 = TroopList.FirstOrDefault((OrderTroopItemVM x) => x.FormationIndex == 7) ?? _emptyTroopItemVM;
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotKey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
