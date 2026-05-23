using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class MissionOrderVM : ViewModel
{
	public enum CursorStates
	{
		Move,
		Face,
		Form
	}

	public enum OrderTargets
	{
		Troops,
		SiegeMachines
	}

	public struct ClassConfiguration
	{
		public int FormationIndex;

		public DeploymentFormationClass FormationClass;

		public ClassConfiguration(int formationIndex, DeploymentFormationClass formationClass)
		{
			FormationIndex = formationIndex;
			FormationClass = formationClass;
		}
	}

	public struct FormationConfiguration
	{
		public int FormationIndex;

		public List<FormationFilterType> Filters;

		public FormationConfiguration(int formationIndex, List<FormationFilterType> filters)
		{
			FormationIndex = formationIndex;
			Filters = filters;
		}
	}

	public InputRestrictions InputRestrictions;

	private Timer _updateTroopsTimer;

	private MissionOrderCallbacks _callbacks;

	private bool _isTroopPlacingActive;

	private bool _isMultiplayer;

	private MBReadOnlyList<Formation> _focusedFormationsCache;

	private int _delayValueForAIFormationModifications;

	private readonly List<Formation> _modifiedAIFormations = new List<Formation>();

	private SoundEvent _slowMotionSoundEvent;

	private int _slowMotionSoundEventGlobalIndex;

	private List<FormationConfiguration> _filterData;

	private Dictionary<int, InputKeyItemVM> _orderKeys;

	private InputKeyItemVM _returnKey;

	private int _lastHighlightedFormationIndex;

	private InputKeyItemVM _cancelInputKey;

	private MBBindingList<OrderSetVM> _orderSets;

	private MissionOrderTroopControllerVM _troopController;

	private MissionOrderDeploymentControllerVM _deploymentController;

	private bool _isDeployment;

	private int _activeTargetState;

	private bool _hasAnyCascadingOrders;

	private bool _isToggleOrderShown;

	private bool _isTroopListShown;

	private bool _canUseShortcuts;

	private bool _isHolding;

	private bool _isAnyOrderSetActive;

	private string _returnText;

	private bool _useAlternativeFormationLayout;

	public CursorStates CursorState
	{
		get
		{
			if (SelectedOrderSet?.OrderIconId == "order_type_facing")
			{
				return CursorStates.Face;
			}
			return CursorStates.Move;
		}
	}

	public Team Team => Mission.Current.PlayerTeam;

	public OrderController OrderController => Mission.PlayerTeam.PlayerOrderController;

	public bool IsTroopPlacingActive
	{
		get
		{
			return _isTroopPlacingActive;
		}
		set
		{
			_isTroopPlacingActive = value;
			_callbacks.SetSuspendTroopPlacer(!value);
		}
	}

	public bool PlayerHasAnyTroopUnderThem => Team.FormationsIncludingEmpty.Any((Formation f) => f.PlayerOwner == Agent.Main && f.CountOfUnits > 0);

	private Mission Mission => Mission.Current;

	public OrderSetVM SelectedOrderSet { get; private set; }

	public bool DisplayedOrderMessageForLastOrder { get; private set; }

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
	public MBBindingList<OrderSetVM> OrderSets
	{
		get
		{
			return _orderSets;
		}
		set
		{
			if (value != _orderSets)
			{
				_orderSets = value;
				OnPropertyChangedWithValue(value, "OrderSets");
			}
		}
	}

	[DataSourceProperty]
	public MissionOrderTroopControllerVM TroopController
	{
		get
		{
			return _troopController;
		}
		set
		{
			if (value != _troopController)
			{
				_troopController = value;
				OnPropertyChangedWithValue(value, "TroopController");
			}
		}
	}

	[DataSourceProperty]
	public MissionOrderDeploymentControllerVM DeploymentController
	{
		get
		{
			return _deploymentController;
		}
		set
		{
			if (value != _deploymentController)
			{
				_deploymentController = value;
				OnPropertyChangedWithValue(value, "DeploymentController");
			}
		}
	}

	[DataSourceProperty]
	public int ActiveTargetState
	{
		get
		{
			return _activeTargetState;
		}
		set
		{
			if (value != _activeTargetState)
			{
				_activeTargetState = value;
				OnPropertyChangedWithValue(value, "ActiveTargetState");
				IsTroopPlacingActive = value == 0;
				_callbacks.RefreshVisuals();
			}
		}
	}

	[DataSourceProperty]
	public bool IsDeployment
	{
		get
		{
			return _isDeployment;
		}
		set
		{
			_isDeployment = value;
			OnPropertyChangedWithValue(value, "IsDeployment");
		}
	}

	[DataSourceProperty]
	public bool HasAnyCascadingOrders
	{
		get
		{
			return _hasAnyCascadingOrders;
		}
		set
		{
			if (value != _hasAnyCascadingOrders)
			{
				_hasAnyCascadingOrders = value;
				OnPropertyChangedWithValue(value, "HasAnyCascadingOrders");
			}
		}
	}

	[DataSourceProperty]
	public bool IsToggleOrderShown
	{
		get
		{
			return _isToggleOrderShown;
		}
		set
		{
			if (value != _isToggleOrderShown)
			{
				_isToggleOrderShown = value;
				OnPropertyChangedWithValue(value, "IsToggleOrderShown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopListShown
	{
		get
		{
			return _isTroopListShown;
		}
		set
		{
			if (value != _isTroopListShown)
			{
				_isTroopListShown = value;
				OnPropertyChangedWithValue(value, "IsTroopListShown");
			}
		}
	}

	[DataSourceProperty]
	public bool CanUseShortcuts
	{
		get
		{
			return _canUseShortcuts;
		}
		set
		{
			if (value != _canUseShortcuts)
			{
				_canUseShortcuts = value;
				OnPropertyChangedWithValue(value, "CanUseShortcuts");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHolding
	{
		get
		{
			return _isHolding;
		}
		set
		{
			if (value != _isHolding)
			{
				_isHolding = value;
				OnPropertyChangedWithValue(value, "IsHolding");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyOrderSetActive
	{
		get
		{
			return _isAnyOrderSetActive;
		}
		set
		{
			if (value != _isAnyOrderSetActive)
			{
				_isAnyOrderSetActive = value;
				OnPropertyChangedWithValue(value, "IsAnyOrderSetActive");
			}
		}
	}

	[DataSourceProperty]
	public string ReturnText
	{
		get
		{
			return _returnText;
		}
		set
		{
			if (value != _returnText)
			{
				_returnText = value;
				OnPropertyChangedWithValue(value, "ReturnText");
			}
		}
	}

	[DataSourceProperty]
	public bool UseAlternativeFormationLayout
	{
		get
		{
			return _useAlternativeFormationLayout;
		}
		set
		{
			if (value != _useAlternativeFormationLayout)
			{
				_useAlternativeFormationLayout = value;
				OnPropertyChangedWithValue(value, "UseAlternativeFormationLayout");
			}
		}
	}

	public MissionOrderVM(OrderController orderController, bool isDeployment, bool isMultiplayer)
	{
		_isMultiplayer = isMultiplayer;
		IsDeployment = isDeployment;
		_orderKeys = new Dictionary<int, InputKeyItemVM>();
		OrderSets = new MBBindingList<OrderSetVM>();
		DeploymentController = new MissionOrderDeploymentControllerVM(this);
		TroopController = CreateTroopController(OrderController);
		Team.OnFormationAIActiveBehaviorChanged += TeamOnFormationAIActiveBehaviorChanged;
		RefreshValues();
		Mission.OnMainAgentChanged += MissionOnMainAgentChanged;
		UpdateCanUseShortcuts(_isMultiplayer);
		_slowMotionSoundEventGlobalIndex = SoundManager.GetEventGlobalIndex("event:/ui/mission/slow_motion");
		RegisterEvents();
	}

	protected virtual MissionOrderTroopControllerVM CreateTroopController(OrderController orderController)
	{
		return new MissionOrderTroopControllerVM(this, IsDeployment, OnTransferFinished);
	}

	public void SetDeploymentParemeters(Camera deploymentCamera, List<DeploymentPoint> deploymentPoints)
	{
		DeploymentController.SetMissionParameters(deploymentCamera, deploymentPoints);
	}

	public void SetCallbacks(MissionOrderCallbacks callbacks)
	{
		_callbacks = callbacks;
		DeploymentController.SetCallbacks(callbacks);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ReturnText = new TextObject("{=EmVbbIUc}Return").ToString();
		OrderSets.ApplyActionOnAllItems(delegate(OrderSetVM o)
		{
			o.RefreshValues();
		});
		DeploymentController.RefreshValues();
		TroopController.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Mission.OnMainAgentChanged -= MissionOnMainAgentChanged;
		OrderSets.ApplyActionOnAllItems(delegate(OrderSetVM o)
		{
			o.OnFinalize();
		});
		DeploymentController.OnFinalize();
		TroopController.OnFinalize();
		for (int num = 0; num < _orderKeys.Count; num++)
		{
			_orderKeys[num].OnFinalize();
		}
		foreach (OrderSetVM orderSet in _orderSets)
		{
			orderSet.OnFinalize();
		}
		if (_slowMotionSoundEvent != null)
		{
			_slowMotionSoundEvent.Release();
			_slowMotionSoundEvent = null;
		}
		InputRestrictions = null;
		UnregisterEvents();
	}

	private void RegisterEvents()
	{
		OrderTroopItemVM.OnSelectionChange += OnTroopItemSelectionStateChanged;
		OrderSetVM.OnSelectionStateChanged += OnOrderSetSelectionStateChanged;
		OrderItemVM.OnExecuteOrder += OnOrderExecuted;
		TransferTroopsVisualOrder.OnTransferStarted += OnTransferStarted;
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveChanged));
	}

	private void UnregisterEvents()
	{
		OrderTroopItemVM.OnSelectionChange -= OnTroopItemSelectionStateChanged;
		OrderSetVM.OnSelectionStateChanged -= OnOrderSetSelectionStateChanged;
		OrderItemVM.OnExecuteOrder -= OnOrderExecuted;
		TransferTroopsVisualOrder.OnTransferStarted -= OnTransferStarted;
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveChanged));
	}

	private void OnGamepadActiveChanged()
	{
		if (IsToggleOrderShown)
		{
			TryCloseToggleOrder();
		}
	}

	private void OnOrderSetSelectionStateChanged(OrderSetVM orderSet, bool isSelected)
	{
		if (SelectedOrderSet == orderSet)
		{
			OrderSetVM selectedOrderSet = SelectedOrderSet;
			if (selectedOrderSet != null && selectedOrderSet.IsSelected == isSelected)
			{
				return;
			}
		}
		if (SelectedOrderSet != null)
		{
			SelectedOrderSet.IsSelected = false;
			SelectedOrderSet = null;
		}
		if (orderSet != null && isSelected)
		{
			SelectedOrderSet = orderSet;
			SelectedOrderSet.IsSelected = true;
		}
		IsAnyOrderSetActive = SelectedOrderSet != null;
		UpdateOrderShortcuts();
	}

	public void OnOrderExecuted(OrderItemVM orderItem)
	{
		if (IsToggleOrderShown)
		{
			OrderSets.ApplyActionOnAllItems(delegate(OrderSetVM o)
			{
				o.OnOrderExecuted(orderItem);
			});
		}
		List<TextObject> list = new List<TextObject>();
		if (ActiveTargetState == 1 && orderItem.Order.StringId != "order_toggle_facing")
		{
			for (int num = 0; num < DeploymentController.SiegeMachineList.Count; num++)
			{
				OrderSiegeMachineVM orderSiegeMachineVM = DeploymentController.SiegeMachineList[num];
				if (orderSiegeMachineVM.IsSelected)
				{
					list.Add(GameTexts.FindText("str_siege_engine", orderSiegeMachineVM.MachineClass));
				}
			}
		}
		else if (!(orderItem.Order is ReturnVisualOrder))
		{
			foreach (OrderTroopItemVM item in TroopController.TroopList.Where((OrderTroopItemVM item) => item.IsSelected))
			{
				list.Add(item.GetVisibleNameOfFormationForMessage());
			}
		}
		if (!list.IsEmpty() && !DisplayedOrderMessageForLastOrder)
		{
			orderItem.RefreshState();
			TextObject textObject = new TextObject("{=ApD0xQXT}{STR1}: {STR2}");
			textObject.SetTextVariable("STR1", GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, includeAnd: false));
			textObject.SetTextVariable("STR2", orderItem.Name);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
			DisplayedOrderMessageForLastOrder = true;
		}
	}

	private void PopulateOrderSets()
	{
		OrderSets.ApplyActionOnAllItems(delegate(OrderSetVM o)
		{
			o.OnFinalize();
		});
		OrderSets.Clear();
		MBReadOnlyList<VisualOrderSet> orders = VisualOrderFactory.GetOrders();
		HasAnyCascadingOrders = false;
		for (int num = 0; num < orders.Count; num++)
		{
			OrderSetVM orderSetVM = new OrderSetVM(OrderController, orders[num]);
			OrderSets.Add(orderSetVM);
			if (!orderSetVM.HasSingleOrder)
			{
				HasAnyCascadingOrders = true;
			}
		}
		UpdateOrderShortcuts();
		if (_isMultiplayer)
		{
			UpdateCanUseShortcuts(value: true);
		}
	}

	private void UpdateOrderShortcuts()
	{
		InputKeyItemVM inputKeyItemVM = InputKeyItemVM.CreateFromHotKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"), isConsoleOnly: false);
		inputKeyItemVM.SetForcedVisibility(false);
		if (SelectedOrderSet != null)
		{
			for (int i = 0; i < OrderSets.Count; i++)
			{
				OrderSetVM orderSetVM = OrderSets[i];
				if (orderSetVM == SelectedOrderSet)
				{
					for (int j = 0; j < orderSetVM.Orders.Count; j++)
					{
						OrderItemVM orderItemVM = orderSetVM.Orders[j];
						InputKeyItemVM value;
						if (orderItemVM.Order is ReturnVisualOrder)
						{
							orderItemVM.SetShortcutKey(_returnKey);
						}
						else if (_orderKeys.TryGetValue(j, out value))
						{
							orderItemVM.SetShortcutKey(value);
						}
					}
				}
				else
				{
					for (int k = 0; k < orderSetVM.Orders.Count; k++)
					{
						orderSetVM.Orders[k].SetShortcutKey(inputKeyItemVM);
					}
				}
				orderSetVM.SetShortcutKey(inputKeyItemVM);
			}
		}
		else
		{
			for (int l = 0; l < OrderSets.Count; l++)
			{
				OrderSetVM orderSetVM2 = OrderSets[l];
				InputKeyItemVM value2;
				if (orderSetVM2.HasSingleOrder && orderSetVM2.Orders[0].Order is ReturnVisualOrder)
				{
					orderSetVM2.SetShortcutKey(_returnKey);
				}
				else if (_orderKeys.TryGetValue(l, out value2))
				{
					orderSetVM2.SetShortcutKey(value2);
				}
			}
		}
		inputKeyItemVM.OnFinalize();
	}

	private void TeamOnFormationAIActiveBehaviorChanged(Formation formation)
	{
		if (formation.IsAIControlled)
		{
			if (_modifiedAIFormations.IndexOf(formation) < 0)
			{
				_modifiedAIFormations.Add(formation);
			}
			_delayValueForAIFormationModifications = 3;
		}
	}

	private void DisplayFormationAIFeedback()
	{
		_delayValueForAIFormationModifications = Math.Max(0, _delayValueForAIFormationModifications - 1);
		if (_delayValueForAIFormationModifications != 0 || _modifiedAIFormations.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < _modifiedAIFormations.Count; i++)
		{
			Formation formation = _modifiedAIFormations[i];
			if (formation?.AI.ActiveBehavior != null && formation.FormationIndex < FormationClass.NumberOfRegularFormations)
			{
				DisplayFormationAIFeedbackAux(_modifiedAIFormations);
			}
			else
			{
				_modifiedAIFormations[i] = null;
			}
		}
		_modifiedAIFormations.Clear();
	}

	private static void DisplayFormationAIFeedbackAux(List<Formation> formations)
	{
		Dictionary<FormationClass, TextObject> dictionary = new Dictionary<FormationClass, TextObject>();
		Type type = null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		for (int i = 0; i < formations.Count; i++)
		{
			Formation formation = formations[i];
			if (formation?.AI.ActiveBehavior != null && (type == null || type == formation.AI.ActiveBehavior.GetType()))
			{
				type = formation.AI.ActiveBehavior.GetType();
				switch (formation.AI.Side)
				{
				case FormationAI.BehaviorSide.Left:
					flag = true;
					break;
				case FormationAI.BehaviorSide.Right:
					flag2 = true;
					break;
				case FormationAI.BehaviorSide.Middle:
					flag3 = true;
					break;
				}
				if (!dictionary.ContainsKey(formation.PhysicalClass))
				{
					TextObject localizedName = formation.PhysicalClass.GetLocalizedName();
					TextObject textObject = GameTexts.FindText("str_troop_group_name_definite");
					textObject.SetTextVariable("FORMATION_CLASS", localizedName);
					dictionary.Add(formation.PhysicalClass, textObject);
				}
				formations[i] = null;
			}
		}
		if (dictionary.Count == 1)
		{
			MBTextManager.SetTextVariable("IS_PLURAL", 0);
			MBTextManager.SetTextVariable("TROOP_NAMES_BEGIN", TextObject.GetEmpty());
			MBTextManager.SetTextVariable("TROOP_NAMES_END", dictionary.First().Value);
		}
		else
		{
			MBTextManager.SetTextVariable("IS_PLURAL", 1);
			TextObject value = dictionary.Last().Value;
			TextObject textObject2;
			if (dictionary.Count == 2)
			{
				textObject2 = dictionary.First().Value;
			}
			else
			{
				textObject2 = GameTexts.FindText("str_LEFT_comma_RIGHT");
				textObject2.SetTextVariable("LEFT", dictionary.First().Value);
				textObject2.SetTextVariable("RIGHT", dictionary.Last().Value);
				for (int j = 2; j < dictionary.Count - 1; j++)
				{
					TextObject textObject3 = GameTexts.FindText("str_LEFT_comma_RIGHT");
					textObject3.SetTextVariable("LEFT", textObject2);
					textObject3.SetTextVariable("RIGHT", dictionary.Values.ElementAt(j));
					textObject2 = textObject3;
				}
			}
			MBTextManager.SetTextVariable("TROOP_NAMES_BEGIN", textObject2);
			MBTextManager.SetTextVariable("TROOP_NAMES_END", value);
		}
		bool flag4 = (flag ? 1 : 0) + (flag3 ? 1 : 0) + (flag2 ? 1 : 0) > 1;
		MBTextManager.SetTextVariable("IS_LEFT", flag4 ? 2 : (flag ? 1 : 0));
		MBTextManager.SetTextVariable("IS_MIDDLE", (!flag4 && flag3) ? 1 : 0);
		MBTextManager.SetTextVariable("IS_RIGHT", (!flag4 && flag2) ? 1 : 0);
		string name = type.Name;
		InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_formation_ai_behavior_text", name).ToString()));
	}

	private void OnTroopItemSelectionStateChanged(OrderTroopItemVM troopItem, bool isSelected)
	{
		for (int i = 0; i < TroopController.TroopList.Count; i++)
		{
			TroopController.TroopList[i].IsTargetRelevant = TroopController.TroopList[i].IsSelected;
		}
	}

	public virtual void OnOrderLayoutTypeChanged()
	{
		TroopController = CreateTroopController(OrderController);
		OrderSets.Clear();
		TroopController.UpdateTroops();
		TroopController.TroopList.ForEach(delegate(OrderTroopItemVM x)
		{
			TroopController.SetTroopActiveOrders(x);
		});
		TroopController.OnFiltersSet(_filterData);
	}

	public void OpenToggleOrder(bool fromHold, bool displayMessage = true)
	{
		if (IsToggleOrderShown)
		{
			return;
		}
		if (OrderController.SelectedFormations.Count == 0)
		{
			OrderController.SelectAllFormations();
		}
		PopulateOrderSets();
		if (!CheckCanBeOpened(displayMessage))
		{
			return;
		}
		Mission.Current.IsOrderMenuOpen = true;
		IsToggleOrderShown = true;
		TroopController.UpdateTroops();
		TroopController.IsTransferActive = false;
		DeploymentController.ProcessSiegeMachines();
		if (OrderController.SelectedFormations.IsEmpty())
		{
			TroopController.SelectAllFormations();
		}
		if (TaleWorlds.InputSystem.Input.IsGamepadActive && TroopController.TroopList.All((OrderTroopItemVM t) => !t.IsSelectionHighlightActive))
		{
			OrderTroopItemVM orderTroopItemVM = TroopController.TroopList.FirstOrDefault();
			if (orderTroopItemVM != null)
			{
				orderTroopItemVM.IsSelectionHighlightActive = true;
			}
		}
		SetActiveOrders();
		OnOrderShownToggle();
		DisplayedOrderMessageForLastOrder = false;
	}

	private bool CheckCanBeOpened(bool displayMessage = false)
	{
		if (Agent.Main == null)
		{
			if (displayMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=GMhOZGnb}Cannot issue order while dead.").ToString()));
			}
			return false;
		}
		if (Mission.Current.Mode != MissionMode.Deployment && !Agent.Main.IsPlayerControlled)
		{
			if (displayMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=b1DHZsaH}Cannot issue order right now.").ToString()));
			}
			return false;
		}
		if (!Team.HasBots || !PlayerHasAnyTroopUnderThem || (!Team.IsPlayerGeneral && !Team.IsPlayerSergeant))
		{
			if (displayMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=DQvGNQ0g}There isn't any unit under command.").ToString()));
			}
			return false;
		}
		if (Mission.Current.IsMissionEnding)
		{
			return Mission.Current.CheckIfBattleInRetreat();
		}
		return true;
	}

	public bool TryCloseToggleOrder(bool applySelectedOrders = false)
	{
		if (IsToggleOrderShown)
		{
			Mission.Current.IsOrderMenuOpen = false;
			if (applySelectedOrders && SelectedOrderSet != null)
			{
				OrderItemVM orderItemVM = SelectedOrderSet.Orders.FirstOrDefault((OrderItemVM o) => o.IsSelected);
				if (orderItemVM != null && _callbacks.GetVisualOrderExecutionParameters != null)
				{
					VisualOrderExecutionParameters executionParameters = _callbacks.GetVisualOrderExecutionParameters();
					orderItemVM.ExecuteAction(executionParameters);
				}
			}
			SelectedOrderSet?.ExecuteDeSelect();
			IsToggleOrderShown = false;
			OnOrderShownToggle();
			if (!IsDeployment)
			{
				InputRestrictions.ResetInputRestrictions();
				return true;
			}
		}
		return false;
	}

	public void SetActiveOrders()
	{
		if (ActiveTargetState == 1)
		{
			DeploymentController.SetCurrentActiveOrders();
		}
		else
		{
			TroopController.SetCurrentActiveOrders();
		}
		OrderSets.ApplyActionOnAllItems(delegate(OrderSetVM os)
		{
			os.RefreshOrderStates();
		});
	}

	public void SetFocusedFormations(MBReadOnlyList<Formation> focusedFormationsCache)
	{
		_focusedFormationsCache = focusedFormationsCache;
	}

	public void AfterInitialize()
	{
		TroopController.UpdateTroops();
		if (!IsDeployment)
		{
			TroopController.SelectAllFormations(uiFeedback: false);
		}
		DeploymentController.SetCurrentActiveOrders();
	}

	public void Update()
	{
		if (IsToggleOrderShown)
		{
			if (!CheckCanBeOpened())
			{
				TryCloseToggleOrder();
			}
			else if (_updateTroopsTimer.Check(MBCommon.GetApplicationTime()))
			{
				TroopController.IntervalUpdate();
			}
			TroopController.Update();
			TroopController.RefreshTroopFormationTargetVisuals();
			UseAlternativeFormationLayout = TaleWorlds.InputSystem.Input.IsGamepadActive;
		}
		if (IsToggleOrderShown)
		{
			if (BannerlordConfig.SlowDownOnOrder && !_isDeployment && !_isMultiplayer && _slowMotionSoundEvent == null)
			{
				_slowMotionSoundEvent = SoundEvent.CreateEvent(_slowMotionSoundEventGlobalIndex, Mission.Current.Scene);
				_slowMotionSoundEvent.Play();
			}
		}
		else if (_slowMotionSoundEvent != null)
		{
			_slowMotionSoundEvent.Release();
			_slowMotionSoundEvent = null;
		}
		DeploymentController.Update();
		DisplayFormationAIFeedback();
	}

	public void OnEscape()
	{
		if (IsToggleOrderShown)
		{
			if (SelectedOrderSet != null)
			{
				SelectedOrderSet.ExecuteDeSelect();
			}
			else
			{
				TryCloseToggleOrder();
			}
		}
	}

	public void ViewOrders()
	{
		if (!IsToggleOrderShown)
		{
			TroopController.UpdateTroops();
			OpenToggleOrder(fromHold: false);
		}
		else
		{
			TryCloseToggleOrder();
		}
	}

	public OrderSetVM GetOrderSetAtIndex(int orderSetIndex)
	{
		if (orderSetIndex < 0 || orderSetIndex >= OrderSets.Count)
		{
			return null;
		}
		return OrderSets[orderSetIndex];
	}

	public bool TrySelectOrderSet(OrderSetVM orderSet)
	{
		if (!CheckCanBeOpened(displayMessage: true))
		{
			return false;
		}
		VisualOrderExecutionParameters executionParameters = _callbacks.GetVisualOrderExecutionParameters();
		orderSet.ExecuteAction(executionParameters);
		if (!IsToggleOrderShown && !orderSet.OrderSet.IsSoloOrder)
		{
			OpenToggleOrder(fromHold: false);
		}
		else if (IsToggleOrderShown && orderSet.OrderSet.IsSoloOrder && !IsDeployment)
		{
			TryCloseToggleOrder();
		}
		return true;
	}

	public void OnTroopFormationSelected(int formationTroopIndex)
	{
		if (CheckCanBeOpened(displayMessage: true))
		{
			if (ActiveTargetState == 0)
			{
				TroopController.OnSelectFormationWithIndex(formationTroopIndex);
			}
			else if (ActiveTargetState == 1)
			{
				DeploymentController.OnSelectFormationWithIndex(formationTroopIndex);
			}
			TryCloseToggleOrder();
			OpenToggleOrder(fromHold: false);
		}
	}

	private void MissionOnMainAgentChanged(Agent oldAgent)
	{
		if (Mission.MainAgent == null)
		{
			TryCloseToggleOrder();
			Mission.IsOrderMenuOpen = false;
		}
	}

	internal void OnDeployAll()
	{
		TroopController.UpdateTroops();
		foreach (OrderTroopItemVM troop in TroopController.TroopList)
		{
			TroopController.SetTroopActiveOrders(troop);
		}
		if (!IsDeployment)
		{
			TroopController.SelectAllFormations(uiFeedback: false);
		}
	}

	private void OnOrderShownToggle()
	{
		IsTroopListShown = IsToggleOrderShown && !IsDeployment;
		if (!_isDeployment)
		{
			if (IsToggleOrderShown)
			{
				_callbacks.OnActivateToggleOrder();
			}
			else
			{
				_callbacks.OnDeactivateToggleOrder();
			}
		}
		_updateTroopsTimer = (IsToggleOrderShown ? new Timer(MBCommon.GetApplicationTime() - 2f, 2f) : null);
		IsTroopPlacingActive = IsToggleOrderShown && ActiveTargetState == 0;
		if (!IsDeployment && TroopController.TroopList.Count > 0 && TaleWorlds.InputSystem.Input.IsGamepadActive && TroopController.TroopList.FirstOrDefault((OrderTroopItemVM t) => t.FormationIndex == _lastHighlightedFormationIndex) == null)
		{
			TroopController.TroopList.ForEach(delegate(OrderTroopItemVM t)
			{
				t.IsSelectionHighlightActive = false;
			});
			TroopController.TroopList[0].IsSelectionHighlightActive = true;
		}
		_callbacks.RefreshVisuals();
	}

	public void OnTroopHighlightSelection(bool isDirectionLeft)
	{
		if (!CheckCanBeOpened(displayMessage: true) || TroopController.TroopList.Count <= 0)
		{
			return;
		}
		OrderTroopItemVM highlightedFormation = TroopController.TroopList.FirstOrDefault((OrderTroopItemVM t) => t.IsSelectionHighlightActive);
		if (highlightedFormation != null)
		{
			OrderTroopItemVM targetFormation = (isDirectionLeft ? TroopController.TroopList.LastOrDefault((OrderTroopItemVM t) => t.FormationIndex < highlightedFormation.FormationIndex) : TroopController.TroopList.FirstOrDefault((OrderTroopItemVM t) => t.FormationIndex > highlightedFormation.FormationIndex));
			if (targetFormation == null)
			{
				targetFormation = (isDirectionLeft ? TroopController.TroopList.LastOrDefault() : TroopController.TroopList.FirstOrDefault());
			}
			if (targetFormation != null)
			{
				TroopController.TroopList.ForEach(delegate(OrderTroopItemVM t)
				{
					t.IsSelectionHighlightActive = t == targetFormation;
				});
				_lastHighlightedFormationIndex = targetFormation.FormationIndex;
			}
		}
		else
		{
			TroopController.TroopList[0].IsSelectionHighlightActive = true;
		}
	}

	public void ExecuteSelectHighlightedFormation()
	{
		OrderTroopItemVM orderTroopItemVM = TroopController.TroopList.FirstOrDefault((OrderTroopItemVM t) => t.IsSelectable && t.IsSelectionHighlightActive);
		if (orderTroopItemVM != null)
		{
			if (orderTroopItemVM.IsSelected && TroopController.TroopList.Count((OrderTroopItemVM x) => x.IsSelected) == 1)
			{
				TroopController.SelectAllFormations();
			}
			else
			{
				OnTroopFormationSelected(orderTroopItemVM.FormationIndex);
			}
		}
	}

	public void ExecuteToggleHighlightedFormation()
	{
		OrderTroopItemVM orderTroopItemVM = TroopController.TroopList.FirstOrDefault((OrderTroopItemVM t) => t.IsSelectable && t.IsSelectionHighlightActive);
		if (orderTroopItemVM == null)
		{
			return;
		}
		if (orderTroopItemVM.IsSelected)
		{
			if (TroopController.TroopList.Count((OrderTroopItemVM t) => t.IsSelected) == 1)
			{
				TroopController.SelectAllFormations();
			}
			TroopController.OnDeselectFormation(orderTroopItemVM.FormationIndex);
		}
		else
		{
			TroopController.AddSelectedFormation(orderTroopItemVM);
		}
		TryCloseToggleOrder();
		OpenToggleOrder(fromHold: false);
	}

	private void OnTransferStarted()
	{
		if (IsDeployment)
		{
			return;
		}
		foreach (OrderTroopItemVM transferTarget in TroopController.TransferTargetList)
		{
			transferTarget.IsSelected = false;
			transferTarget.IsSelectable = !OrderController.IsFormationListening(transferTarget.Formation);
		}
		OrderTroopItemVM orderTroopItemVM = TroopController.TransferTargetList.FirstOrDefault((OrderTroopItemVM t) => t.IsSelectable);
		if (orderTroopItemVM != null)
		{
			TroopController.IsTransferActive = true;
			TroopController.ExecuteSelectTransferTroop(orderTroopItemVM);
			TroopController.TransferMaxValue = TroopController.TroopList.Where((OrderTroopItemVM t) => t.IsSelected).Sum((OrderTroopItemVM t) => t.CurrentMemberCount);
			TroopController.TransferValue = TroopController.TransferMaxValue;
			InputRestrictions.SetInputRestrictions();
		}
		else
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=SLY8z9fP}All formations are selected!"));
		}
	}

	protected void OnTransferFinished()
	{
		_callbacks.OnTransferTroopsFinished();
	}

	[Conditional("DEBUG")]
	private void DebugTick()
	{
		if (!IsToggleOrderShown)
		{
			return;
		}
		string text = "SelectedFormations (" + OrderController.SelectedFormations.Count + ") :";
		foreach (Formation selectedFormation in OrderController.SelectedFormations)
		{
			text = text + " " + selectedFormation.FormationIndex.GetName();
		}
	}

	public void OnDeploymentFinished()
	{
		TroopController.OnDeploymentFinished();
		DeploymentController.FinalizeDeployment();
		IsDeployment = false;
	}

	public void OnAfterDeploymentFinished()
	{
		TroopController.OnAfterDeploymentFinished();
	}

	public void OnFiltersSet(List<FormationConfiguration> filterData)
	{
		_filterData = filterData;
		TroopController.OnFiltersSet(filterData);
	}

	public void UpdateCanUseShortcuts(bool value)
	{
		CanUseShortcuts = value;
		for (int i = 0; i < OrderSets.Count; i++)
		{
			OrderSets[i].UpdateCanUseShortcuts(value);
		}
		if (!value)
		{
			TroopController.TroopList.ForEach(delegate(OrderTroopItemVM t)
			{
				t.ShowSelectionInputs = false;
			});
		}
	}

	public void SetOrderIndexKey(int orderIndex, GameKey gameKey)
	{
		if (_orderKeys.TryGetValue(orderIndex, out var value))
		{
			value?.OnFinalize();
		}
		InputKeyItemVM value2 = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
		_orderKeys[orderIndex] = value2;
	}

	public void SetReturnKey(GameKey gameKey)
	{
		_returnKey?.OnFinalize();
		_returnKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
