using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Handlers;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class MissionOrderDeploymentControllerVM : ViewModel
{
	public const uint _entityHiglightColor = 4289622555u;

	public const uint _entitySelectedColor = 4293481743u;

	private GameEntity _currentSelectedEntity;

	private GameEntity _currentHoveredEntity;

	private InquiryData _siegeDeployQueryData;

	private DeploymentHandler _deploymentHandler;

	private SiegeDeploymentHandler _siegeDeploymentHandler;

	internal DeploymentSiegeMachineVM _selectedDeploymentPointVM;

	private readonly MissionOrderVM _missionOrder;

	private Camera _deploymentCamera;

	private List<DeploymentPoint> _deploymentPoints;

	private MissionOrderCallbacks _callbacks;

	private MBBindingList<OrderSiegeMachineVM> _siegeMachineList;

	private MBBindingList<DeploymentSiegeMachineVM> _siegeDeploymentList;

	private MBBindingList<DeploymentSiegeMachineVM> _deploymentTargets;

	private bool _isSiegeDeploymentListActive;

	private Mission Mission => Mission.Current;

	private Team Team => Mission.Current.PlayerTeam;

	public OrderController OrderController => Team.PlayerOrderController;

	[DataSourceProperty]
	public MBBindingList<OrderSiegeMachineVM> SiegeMachineList
	{
		get
		{
			return _siegeMachineList;
		}
		set
		{
			if (value != _siegeMachineList)
			{
				_siegeMachineList = value;
				OnPropertyChanged("SiegeMachineList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<DeploymentSiegeMachineVM> DeploymentTargets
	{
		get
		{
			return _deploymentTargets;
		}
		set
		{
			if (value != _deploymentTargets)
			{
				_deploymentTargets = value;
				OnPropertyChanged("DeploymentTargets");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSiegeDeploymentListActive
	{
		get
		{
			return _isSiegeDeploymentListActive;
		}
		set
		{
			if (value != _isSiegeDeploymentListActive)
			{
				_isSiegeDeploymentListActive = value;
				OnPropertyChanged("IsSiegeDeploymentListActive");
				_callbacks.ToggleMissionInputs(value);
				_callbacks.RefreshVisuals();
				if (_selectedDeploymentPointVM != null)
				{
					_selectedDeploymentPointVM.IsSelected = value;
				}
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<DeploymentSiegeMachineVM> SiegeDeploymentList
	{
		get
		{
			return _siegeDeploymentList;
		}
		set
		{
			if (value != _siegeDeploymentList)
			{
				_siegeDeploymentList = value;
				OnPropertyChanged("SiegeDeploymentList");
			}
		}
	}

	public void SetMissionParameters(Camera deploymentCamera, List<DeploymentPoint> deploymentPoints)
	{
		_deploymentPoints = deploymentPoints;
		_deploymentCamera = deploymentCamera;
		SiegeDeploymentList.Clear();
		if (_siegeDeploymentHandler == null)
		{
			return;
		}
		int num = 1;
		foreach (DeploymentPoint deploymentPoint in _deploymentPoints)
		{
			OrderSiegeMachineVM item = new OrderSiegeMachineVM(deploymentPoint, OnSelectOrderSiegeMachine, num++);
			SiegeMachineList.Add(item);
			if (deploymentPoint.DeployableWeapons.Any((SynchedMissionObject x) => _siegeDeploymentHandler.GetMaxDeployableWeaponCountOfPlayer(((object)x).GetType()) > 0))
			{
				DeploymentSiegeMachineVM item2 = new DeploymentSiegeMachineVM(deploymentPoint, null, _deploymentCamera, OnRefreshSelectedDeploymentPoint, OnEntityHover, deploymentPoint.IsDeployed);
				DeploymentTargets.Add(item2);
			}
		}
	}

	public void SetCallbacks(MissionOrderCallbacks callbacks)
	{
		_callbacks = callbacks;
	}

	public MissionOrderDeploymentControllerVM(MissionOrderVM missionOrder)
	{
		_missionOrder = missionOrder;
		SiegeMachineList = new MBBindingList<OrderSiegeMachineVM>();
		SiegeDeploymentList = new MBBindingList<DeploymentSiegeMachineVM>();
		DeploymentTargets = new MBBindingList<DeploymentSiegeMachineVM>();
		MBTextManager.SetTextVariable("UNDEPLOYED_SIEGE_MACHINE_COUNT", SiegeMachineList.Count((OrderSiegeMachineVM s) => !s.SiegeWeapon.IsUsed).ToString());
		_siegeDeployQueryData = new InquiryData(new TextObject("{=TxphX8Uk}Deployment").ToString(), new TextObject("{=LlrlE199}You can still deploy siege engines.{newline}Begin anyway?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate
		{
			_siegeDeploymentHandler.FinishDeployment();
			_missionOrder.TryCloseToggleOrder();
		}, null);
		SiegeMachineList.Clear();
		OrderController.SiegeWeaponController.OnSelectedSiegeWeaponsChanged += OnSelectedSiegeWeaponsChanged;
		_deploymentHandler = Mission.GetMissionBehavior<DeploymentHandler>();
		if (_deploymentHandler != null)
		{
			_deploymentHandler.OnPlayerSideDeploymentReady += ExecuteDeployPlayerSide;
			if (_deploymentHandler is SiegeDeploymentHandler siegeDeploymentHandler)
			{
				_siegeDeploymentHandler = siegeDeploymentHandler;
				_siegeDeploymentHandler.OnEnemySideDeploymentReady += ExecuteDeployEnemySide;
			}
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_siegeMachineList.ApplyActionOnAllItems(delegate(OrderSiegeMachineVM x)
		{
			x.RefreshValues();
		});
		_siegeDeploymentList.ApplyActionOnAllItems(delegate(DeploymentSiegeMachineVM x)
		{
			x.RefreshValues();
		});
		_deploymentTargets.ApplyActionOnAllItems(delegate(DeploymentSiegeMachineVM x)
		{
			x.RefreshValues();
		});
	}

	internal void Update()
	{
		for (int i = 0; i < DeploymentTargets.Count; i++)
		{
			DeploymentTargets[i].Update();
		}
	}

	internal void DeployFormationsOfPlayer()
	{
		if (_siegeDeploymentHandler != null)
		{
			_siegeDeploymentHandler.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam, autoAssignDetachments: false);
		}
		else if (!Mission.IsNavalBattle && !Mission.IsNavalRaidBattle && _deploymentHandler != null)
		{
			_deploymentHandler.AutoDeployTeamUsingDeploymentPlan(Mission.PlayerTeam);
		}
		Mission.Current.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>()?.OnPlayerTeamDeployed();
		if (_siegeDeploymentHandler != null)
		{
			_siegeDeploymentHandler.AutoAssignDetachmentsForDeployment(Mission.PlayerTeam);
		}
	}

	internal void SetSiegeMachineActiveOrders(OrderSiegeMachineVM siegeItemVM)
	{
		siegeItemVM.ActiveOrders.Clear();
	}

	internal void ProcessSiegeMachines()
	{
		for (int i = 0; i < SiegeMachineList.Count; i++)
		{
			OrderSiegeMachineVM orderSiegeMachineVM = SiegeMachineList[i];
			orderSiegeMachineVM.RefreshSiegeWeapon();
			if (orderSiegeMachineVM.IsSelectable && OrderController.SiegeWeaponController.SelectedWeapons.Contains(orderSiegeMachineVM.SiegeWeapon))
			{
				orderSiegeMachineVM.IsSelected = true;
			}
			else if (!orderSiegeMachineVM.IsSelectable && orderSiegeMachineVM.IsSelected)
			{
				OnDeselectSiegeMachine(orderSiegeMachineVM);
			}
		}
	}

	internal void SelectAllSiegeMachines()
	{
		if (SiegeMachineList.Any((OrderSiegeMachineVM t) => t.IsSelectable))
		{
			OrderController.SiegeWeaponController.SelectAll();
		}
		_missionOrder.SetActiveOrders();
		_callbacks.RefreshVisuals();
	}

	internal void AddSelectedSiegeMachine(OrderSiegeMachineVM item)
	{
		if (item.IsSelectable)
		{
			OrderController.SiegeWeaponController.Select(item.SiegeWeapon);
			_missionOrder.SetActiveOrders();
			_callbacks.RefreshVisuals();
		}
	}

	internal void SetSelectedSiegeMachine(OrderSiegeMachineVM item)
	{
		ProcessSiegeMachines();
		if (item.IsSelectable)
		{
			SetSiegeMachineActiveOrders(item);
			OrderController.SiegeWeaponController.ClearSelectedWeapons();
			AddSelectedSiegeMachine(item);
			_callbacks.RefreshVisuals();
		}
	}

	internal void OnDeselectSiegeMachine(OrderSiegeMachineVM item)
	{
		if (item.IsSelected)
		{
			OrderController.SiegeWeaponController.Deselect(item.SiegeWeapon);
		}
		_missionOrder.SetActiveOrders();
		_callbacks.RefreshVisuals();
	}

	internal void OnSelectOrderSiegeMachine(OrderSiegeMachineVM item)
	{
		ProcessSiegeMachines();
		_missionOrder.IsTroopPlacingActive = false;
		if (!item.IsSelectable)
		{
			return;
		}
		if (TaleWorlds.InputSystem.Input.DebugInput.IsControlDown())
		{
			if (item.IsSelected)
			{
				OnDeselectSiegeMachine(item);
			}
			else
			{
				AddSelectedSiegeMachine(item);
			}
		}
		else
		{
			SetSelectedSiegeMachine(item);
		}
		_callbacks.RefreshVisuals();
	}

	internal void OnSelectDeploymentSiegeMachine(DeploymentSiegeMachineVM item)
	{
		IsSiegeDeploymentListActive = false;
		_currentSelectedEntity?.SetContourColor(null);
		_currentSelectedEntity = null;
		_selectedDeploymentPointVM = null;
		SiegeDeploymentList.Clear();
		bool flag = false;
		if (item != null && (!(item.MachineType != null) || _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(item.MachineType) != 0) && (item.DeploymentPoint.DeployedWeapon == null || !(((object)item.DeploymentPoint.DeployedWeapon).GetType() == item.MachineType)))
		{
			bool num = !item.DeploymentPoint.IsDeployed || item.DeploymentPoint.DeployedWeapon != item.SiegeWeapon;
			if (item.DeploymentPoint.IsDeployed)
			{
				if (item.SiegeWeapon == null)
				{
					SoundEvent.PlaySound2D("event:/ui/dropdown");
				}
				item.DeploymentPoint.Disband();
			}
			flag = !SiegeMachineList.Any((OrderSiegeMachineVM s) => s.DeploymentPoint.IsDeployed);
			if (num && item.SiegeWeapon != null)
			{
				SiegeEngineType machine = item.Machine;
				if (machine == DefaultSiegeEngineTypes.Catapult || machine == DefaultSiegeEngineTypes.FireCatapult || machine == DefaultSiegeEngineTypes.Onager || machine == DefaultSiegeEngineTypes.FireOnager)
				{
					SoundEvent.PlaySound2D("event:/ui/mission/catapult");
				}
				else if (machine == DefaultSiegeEngineTypes.Ram)
				{
					SoundEvent.PlaySound2D("event:/ui/mission/batteringram");
				}
				else if (machine == DefaultSiegeEngineTypes.SiegeTower)
				{
					SoundEvent.PlaySound2D("event:/ui/mission/siegetower");
				}
				else if (machine == DefaultSiegeEngineTypes.Trebuchet || machine == DefaultSiegeEngineTypes.Bricole)
				{
					SoundEvent.PlaySound2D("event:/ui/mission/catapult");
				}
				else if (machine == DefaultSiegeEngineTypes.Ballista || machine == DefaultSiegeEngineTypes.FireBallista)
				{
					SoundEvent.PlaySound2D("event:/ui/mission/ballista");
				}
				item.DeploymentPoint.Deploy(item.SiegeWeapon);
			}
		}
		ProcessSiegeMachines();
		if (flag && _missionOrder.IsToggleOrderShown)
		{
			_missionOrder.SetActiveOrders();
		}
		_callbacks.RefreshVisuals();
		foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
		{
			deploymentTarget.RefreshWithDeployedWeapon();
		}
	}

	internal void OnSelectedSiegeWeaponsChanged()
	{
		for (int i = 0; i < SiegeMachineList.Count; i++)
		{
			OrderSiegeMachineVM orderSiegeMachineVM = SiegeMachineList[i];
			orderSiegeMachineVM.IsSelected = OrderController.SiegeWeaponController.SelectedWeapons.Contains(orderSiegeMachineVM.SiegeWeapon);
		}
	}

	public void OnRefreshSelectedDeploymentPoint(DeploymentSiegeMachineVM item)
	{
		RefreshSelectedDeploymentPoint(item.DeploymentPoint);
	}

	public void OnEntityHover(WeakGameEntity hoveredEntity)
	{
		if (_currentHoveredEntity == hoveredEntity)
		{
			return;
		}
		DeploymentPoint deploymentPoint = null;
		if (hoveredEntity.IsValid)
		{
			if (hoveredEntity.HasScriptOfType<DeploymentPoint>())
			{
				deploymentPoint = hoveredEntity.GetFirstScriptOfType<DeploymentPoint>();
			}
			else if (_siegeDeploymentHandler != null)
			{
				deploymentPoint = _siegeDeploymentHandler.PlayerDeploymentPoints.SingleOrDefault((DeploymentPoint dp) => dp.IsDeployed && hoveredEntity.GetScriptComponents().Any((ScriptComponentBehavior sc) => dp.DeployedWeapon == sc));
			}
		}
		OnEntityHover(deploymentPoint);
	}

	public void OnEntityHover(DeploymentPoint deploymentPoint)
	{
		if (_currentSelectedEntity != _currentHoveredEntity)
		{
			_currentHoveredEntity?.SetContourColor(null);
		}
		if (deploymentPoint != null)
		{
			_currentHoveredEntity = GameEntity.CreateFromWeakEntity(deploymentPoint.IsDeployed ? deploymentPoint.DeployedWeapon.GameEntity : deploymentPoint.GameEntity);
		}
		else
		{
			_currentHoveredEntity = null;
		}
		if (_currentSelectedEntity != _currentHoveredEntity)
		{
			_currentHoveredEntity?.SetContourColor(4289622555u);
		}
	}

	public void OnEntitySelect(WeakGameEntity selectedEntity)
	{
		if (_currentSelectedEntity == selectedEntity)
		{
			return;
		}
		DeploymentPoint deploymentPoint = null;
		if (selectedEntity.IsValid && _siegeDeploymentHandler != null)
		{
			if (selectedEntity.HasScriptOfType<DeploymentPoint>())
			{
				deploymentPoint = selectedEntity.GetFirstScriptOfType<DeploymentPoint>();
			}
			else if (_siegeDeploymentHandler != null)
			{
				deploymentPoint = _siegeDeploymentHandler.PlayerDeploymentPoints.SingleOrDefault((DeploymentPoint dp) => dp.IsDeployed && selectedEntity.GetScriptComponents().Any((ScriptComponentBehavior sc) => dp.DeployedWeapon == sc));
			}
		}
		if (deploymentPoint != null)
		{
			_missionOrder.IsTroopPlacingActive = false;
			RefreshSelectedDeploymentPoint(deploymentPoint);
		}
		else
		{
			ExecuteCancelSelectedDeploymentPoint();
		}
	}

	public void RefreshSelectedDeploymentPoint(DeploymentPoint selectedDeploymentPoint)
	{
		IsSiegeDeploymentListActive = false;
		foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
		{
			if (deploymentTarget.DeploymentPoint == selectedDeploymentPoint)
			{
				_selectedDeploymentPointVM = deploymentTarget;
			}
		}
		if (!_selectedDeploymentPointVM.IsSelected)
		{
			_selectedDeploymentPointVM.IsSelected = true;
		}
		SiegeDeploymentList.Clear();
		DeploymentSiegeMachineVM deploymentSiegeMachineVM;
		foreach (SynchedMissionObject deployableWeapon in selectedDeploymentPoint.DeployableWeapons)
		{
			Type type = ((object)deployableWeapon).GetType();
			if (_siegeDeploymentHandler.GetMaxDeployableWeaponCountOfPlayer(type) > 0)
			{
				deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(selectedDeploymentPoint, deployableWeapon as SiegeWeapon, _deploymentCamera, OnSelectDeploymentSiegeMachine, null, selectedDeploymentPoint.IsDeployed && selectedDeploymentPoint.DeployedWeapon == deployableWeapon);
				SiegeDeploymentList.Add(deploymentSiegeMachineVM);
				deploymentSiegeMachineVM.RemainingCount = _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(type);
			}
		}
		deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(selectedDeploymentPoint, null, _deploymentCamera, OnSelectDeploymentSiegeMachine, null, !selectedDeploymentPoint.IsDeployed);
		SiegeDeploymentList.Add(deploymentSiegeMachineVM);
		selectedDeploymentPoint.GameEntity.SetContourColor(4293481743u);
		IsSiegeDeploymentListActive = true;
		_currentSelectedEntity?.SetContourColor(null);
		_currentSelectedEntity = GameEntity.CreateFromWeakEntity(selectedDeploymentPoint.GameEntity);
		_currentSelectedEntity?.SetContourColor(4293481743u);
	}

	public void ExecuteCancelSelectedDeploymentPoint()
	{
		OnSelectDeploymentSiegeMachine(null);
	}

	public void ExecuteBeginMission()
	{
		IsSiegeDeploymentListActive = false;
		if (_siegeDeploymentHandler != null && _siegeDeploymentHandler.PlayerDeploymentPoints.Any((DeploymentPoint d) => !d.IsDeployed && d.DeployableWeaponTypes.Any((Type type) => _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(type) > 0)))
		{
			InformationManager.ShowInquiry(_siegeDeployQueryData);
		}
		else if (_deploymentHandler != null)
		{
			_missionOrder.TryCloseToggleOrder();
			_deploymentHandler.FinishDeployment();
		}
	}

	public void ExecuteAutoDeploy()
	{
		Mission.GetDeploymentPlan<IMissionDeploymentPlan>(out var deploymentPlan);
		deploymentPlan.RemakeDeploymentPlan(Mission.PlayerTeam);
		if (_siegeDeploymentHandler != null)
		{
			AutoDeploySiegeMachines();
			_siegeDeploymentHandler.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam);
		}
		else if (_deploymentHandler != null)
		{
			_deploymentHandler.AutoDeployTeamUsingDeploymentPlan(Mission.PlayerTeam);
		}
	}

	private void AutoDeploySiegeMachines()
	{
		IsSiegeDeploymentListActive = false;
		foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
		{
			if (!(deploymentTarget.MachineType != null))
			{
				deploymentTarget.ExecuteAction();
				SiegeDeploymentList.FirstOrDefault((DeploymentSiegeMachineVM d) => d.Machine != null && d.RemainingCount > 0)?.ExecuteAction();
			}
		}
		IsSiegeDeploymentListActive = false;
	}

	public void ExecuteDeployPlayerSide()
	{
		if (_siegeDeploymentHandler != null)
		{
			Mission.ForceTickOccasionally = true;
			bool isTeleportingAgents = Mission.Current.IsTeleportingAgents;
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = true;
			}
			if (!Mission.IsSallyOutBattle || Mission.PlayerTeam.Side == BattleSideEnum.Attacker)
			{
				DeployFormationsOfPlayer();
				_siegeDeploymentHandler.ForceUpdateAllUnits();
			}
			_missionOrder.OnDeployAll();
			foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
			{
				siegeMachine.RefreshSiegeWeapon();
			}
			foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
			{
				deploymentTarget.RefreshWithDeployedWeapon();
			}
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = isTeleportingAgents;
			}
			Mission.ForceTickOccasionally = false;
			SelectAllSiegeMachines();
		}
		else if (_deploymentHandler != null)
		{
			DeployFormationsOfPlayer();
			_deploymentHandler.ForceUpdateAllUnits();
			_missionOrder.OnDeployAll();
		}
	}

	private void ExecuteDeployEnemySide()
	{
		if (_siegeDeploymentHandler != null)
		{
			Mission.ForceTickOccasionally = true;
			bool isTeleportingAgents = Mission.Current.IsTeleportingAgents;
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = true;
			}
			if (!Mission.IsSallyOutBattle || Mission.PlayerTeam.Side == BattleSideEnum.Defender)
			{
				_siegeDeploymentHandler.AutoDeployTeamUsingTeamAI(Mission.PlayerEnemyTeam);
				_siegeDeploymentHandler.ForceUpdateAllUnits();
			}
			_missionOrder.OnDeployAll();
			foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
			{
				siegeMachine.RefreshSiegeWeapon();
			}
			foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
			{
				deploymentTarget.RefreshWithDeployedWeapon();
			}
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = isTeleportingAgents;
			}
			Mission.ForceTickOccasionally = false;
			SelectAllSiegeMachines();
		}
		else if (_deploymentHandler != null)
		{
			_deploymentHandler.ForceUpdateAllUnits();
			_missionOrder.OnDeployAll();
		}
	}

	public void FinalizeDeployment()
	{
		_missionOrder.IsDeployment = false;
		foreach (OrderSiegeMachineVM item in SiegeMachineList.ToList())
		{
			if (item.DeploymentPoint.IsDeployed)
			{
				SetSiegeMachineActiveOrders(item);
			}
			else
			{
				SiegeMachineList.Remove(item);
			}
		}
		DeploymentTargets.Clear();
		SiegeDeploymentList.Clear();
	}

	internal void OnSelectFormationWithIndex(int formationTroopIndex)
	{
		OrderSiegeMachineVM orderSiegeMachineVM = SiegeMachineList.ElementAtOrDefault(formationTroopIndex);
		if (orderSiegeMachineVM != null)
		{
			OnSelectOrderSiegeMachine(orderSiegeMachineVM);
		}
		else
		{
			SelectAllSiegeMachines();
		}
	}

	internal void SetCurrentActiveOrders()
	{
		if (SiegeMachineList.Any((OrderSiegeMachineVM x) => x.IsSelectable) && !SiegeMachineList.Any((OrderSiegeMachineVM x) => x.IsSelected))
		{
			SelectAllSiegeMachines();
		}
		foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
		{
			if (siegeMachine.IsSelected)
			{
				SetSiegeMachineActiveOrders(siegeMachine);
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		OrderController.SiegeWeaponController.OnSelectedSiegeWeaponsChanged -= OnSelectedSiegeWeaponsChanged;
		SiegeDeploymentList.Clear();
		foreach (OrderSiegeMachineVM item in SiegeMachineList.ToList())
		{
			if (!item.DeploymentPoint.IsDeployed)
			{
				SiegeMachineList.Remove(item);
			}
		}
		_siegeDeploymentHandler = null;
		_siegeDeployQueryData = null;
	}
}
