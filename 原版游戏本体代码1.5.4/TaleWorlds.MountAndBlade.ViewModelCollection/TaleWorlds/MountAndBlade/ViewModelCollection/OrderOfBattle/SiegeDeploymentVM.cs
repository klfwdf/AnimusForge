using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class SiegeDeploymentVM : ViewModel
{
	public const uint EntityHighlightColor = 4289622555u;

	public const uint EntitySelectedColor = 4293481743u;

	private GameEntity _currentSelectedEntity;

	private GameEntity _currentHoveredEntity;

	private readonly SiegeDeploymentHandler _siegeDeploymentHandler;

	private readonly Camera _deploymentCamera;

	private MBBindingList<DeploymentSiegeMachineVM> _deploymentTargets;

	private MBBindingList<DeploymentSiegeMachineVM> _siegeDeploymentList;

	private DeploymentSiegeMachineVM _selectedDeploymentPoint;

	private bool _isSiegeDeploymentListActive;

	private bool _isSiegeDeploymentDisabled;

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

	[DataSourceProperty]
	public DeploymentSiegeMachineVM SelectedDeploymentPoint
	{
		get
		{
			return _selectedDeploymentPoint;
		}
		set
		{
			if (value != _selectedDeploymentPoint)
			{
				_selectedDeploymentPoint = value;
				OnPropertyChanged("SelectedDeploymentPoint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSiegeDeploymentDisabled
	{
		get
		{
			return _isSiegeDeploymentDisabled;
		}
		set
		{
			if (value != _isSiegeDeploymentDisabled)
			{
				_isSiegeDeploymentDisabled = value;
				OnPropertyChangedWithValue(value, "IsSiegeDeploymentDisabled");
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
				if (SelectedDeploymentPoint != null)
				{
					SelectedDeploymentPoint.IsSelected = value;
				}
			}
		}
	}

	public SiegeDeploymentVM(SiegeDeploymentHandler siegeDeploymentHandler, Camera deploymentCamera, List<DeploymentPoint> deploymentPoints)
	{
		_siegeDeploymentHandler = siegeDeploymentHandler;
		_deploymentCamera = deploymentCamera;
		DeploymentTargets = new MBBindingList<DeploymentSiegeMachineVM>();
		SiegeDeploymentList = new MBBindingList<DeploymentSiegeMachineVM>();
		foreach (DeploymentPoint deploymentPoint in deploymentPoints)
		{
			if (deploymentPoint.DeployableWeapons.Any((SynchedMissionObject x) => _siegeDeploymentHandler.GetMaxDeployableWeaponCountOfPlayer(((object)x).GetType()) > 0))
			{
				DeploymentSiegeMachineVM item = new DeploymentSiegeMachineVM(deploymentPoint, null, _deploymentCamera, OnRefreshSelectedDeploymentPoint, OnEntityHover);
				DeploymentTargets.Add(item);
			}
		}
		RefreshDeployedWeapons();
		_siegeDeploymentHandler.OnPlayerSideDeploymentReady += RefreshDeployedWeapons;
		_siegeDeploymentHandler.OnEnemySideDeploymentReady += RefreshDeployedWeapons;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_deploymentTargets?.ApplyActionOnAllItems(delegate(DeploymentSiegeMachineVM x)
		{
			x.RefreshValues();
		});
		_siegeDeploymentList?.ApplyActionOnAllItems(delegate(DeploymentSiegeMachineVM x)
		{
			x.RefreshValues();
		});
	}

	public void Update()
	{
		IsSiegeDeploymentDisabled = Mission.Current.IsOrderMenuOpen;
		for (int i = 0; i < DeploymentTargets.Count; i++)
		{
			DeploymentTargets[i].Update();
		}
	}

	public void AutoDeploySiegeMachines()
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

	public bool HasUndeployedSiegeMachines()
	{
		return _siegeDeploymentHandler.PlayerDeploymentPoints.Any((DeploymentPoint d) => !d.IsDeployed && d.DeployableWeaponTypes.Any((Type type) => _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(type) > 0));
	}

	public void OnDeploymentFinalized()
	{
		DeploymentTargets.Clear();
		SiegeDeploymentList.Clear();
		_currentSelectedEntity?.SetContourColor(null);
		_currentSelectedEntity = null;
		_currentHoveredEntity = null;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_siegeDeploymentHandler.OnPlayerSideDeploymentReady -= RefreshDeployedWeapons;
		_siegeDeploymentHandler.OnEnemySideDeploymentReady -= RefreshDeployedWeapons;
		SiegeDeploymentList.Clear();
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

	public void OnRefreshSelectedDeploymentPoint(DeploymentSiegeMachineVM item)
	{
		if (item.DeploymentPoint == SelectedDeploymentPoint?.DeploymentPoint)
		{
			ExecuteCancelSelectedDeploymentPoint();
		}
		else
		{
			RefreshSelectedDeploymentPoint(item.DeploymentPoint);
		}
	}

	public void RefreshSelectedDeploymentPoint(DeploymentPoint selectedDeploymentPoint)
	{
		IsSiegeDeploymentListActive = false;
		foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
		{
			if (deploymentTarget.DeploymentPoint == selectedDeploymentPoint)
			{
				SelectedDeploymentPoint = deploymentTarget;
			}
		}
		if (!SelectedDeploymentPoint.IsSelected)
		{
			SelectedDeploymentPoint.IsSelected = true;
		}
		SiegeDeploymentList.Clear();
		DeploymentSiegeMachineVM deploymentSiegeMachineVM;
		foreach (SynchedMissionObject deployableWeapon in selectedDeploymentPoint.DeployableWeapons)
		{
			Type type = ((object)deployableWeapon).GetType();
			if (_siegeDeploymentHandler.GetMaxDeployableWeaponCountOfPlayer(type) > 0)
			{
				deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(selectedDeploymentPoint, deployableWeapon as SiegeWeapon, _deploymentCamera, OnSelectDeploymentSiegeMachine, null);
				SiegeDeploymentList.Add(deploymentSiegeMachineVM);
				deploymentSiegeMachineVM.RemainingCount = _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(type);
			}
		}
		deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(selectedDeploymentPoint, null, _deploymentCamera, OnSelectDeploymentSiegeMachine, null);
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

	private void OnSelectDeploymentSiegeMachine(DeploymentSiegeMachineVM item)
	{
		IsSiegeDeploymentListActive = false;
		_currentSelectedEntity?.SetContourColor(null);
		_currentSelectedEntity = null;
		SelectedDeploymentPoint = null;
		SiegeDeploymentList.Clear();
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
		RefreshDeployedWeapons();
	}

	private void RefreshDeployedWeapons()
	{
		foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
		{
			deploymentTarget.RefreshWithDeployedWeapon();
		}
	}
}
