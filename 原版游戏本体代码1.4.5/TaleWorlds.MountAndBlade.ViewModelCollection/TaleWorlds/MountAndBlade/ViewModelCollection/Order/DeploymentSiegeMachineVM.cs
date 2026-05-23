using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class DeploymentSiegeMachineVM : ViewModel
{
	public Type MachineType;

	public SiegeEngineType Machine;

	public SiegeWeapon SiegeWeapon;

	private readonly Camera _deploymentCamera;

	private Vec3 _worldPos;

	private float _latestX;

	private float _latestY;

	private readonly Action<DeploymentSiegeMachineVM> _onSelect;

	private readonly Action<DeploymentPoint> _onHover;

	private string _machineClass = "";

	private int _remainingCount = -1;

	private bool _isSelected;

	private bool _isPlayerGeneral;

	private int _type;

	private bool _isInside;

	private bool _isInFront;

	private string _breachedText;

	private Vec2 _position;

	public DeploymentPoint DeploymentPoint { get; }

	[DataSourceProperty]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerGeneral
	{
		get
		{
			return _isPlayerGeneral;
		}
		set
		{
			if (value != _isPlayerGeneral)
			{
				_isPlayerGeneral = value;
				OnPropertyChangedWithValue(value, "IsPlayerGeneral");
			}
		}
	}

	[DataSourceProperty]
	public string MachineClass
	{
		get
		{
			return _machineClass;
		}
		set
		{
			if (value != _machineClass)
			{
				_machineClass = value;
				OnPropertyChangedWithValue(value, "MachineClass");
			}
		}
	}

	[DataSourceProperty]
	public string BreachedText
	{
		get
		{
			return _breachedText;
		}
		set
		{
			if (value != _breachedText)
			{
				_breachedText = value;
				OnPropertyChangedWithValue(value, "BreachedText");
			}
		}
	}

	[DataSourceProperty]
	public int RemainingCount
	{
		get
		{
			return _remainingCount;
		}
		set
		{
			if (value != _remainingCount)
			{
				_remainingCount = value;
				OnPropertyChangedWithValue(value, "RemainingCount");
			}
		}
	}

	public bool IsInside
	{
		get
		{
			return _isInside;
		}
		set
		{
			if (value != _isInside)
			{
				_isInside = value;
				OnPropertyChangedWithValue(value, "IsInside");
			}
		}
	}

	public bool IsInFront
	{
		get
		{
			return _isInFront;
		}
		set
		{
			if (value != _isInFront)
			{
				_isInFront = value;
				OnPropertyChangedWithValue(value, "IsInFront");
			}
		}
	}

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChangedWithValue(value, "Position");
			}
		}
	}

	public DeploymentSiegeMachineVM(DeploymentPoint selectedDeploymentPoint, SiegeWeapon siegeMachine, Camera deploymentCamera, Action<DeploymentSiegeMachineVM> onSelectSiegeMachine, Action<DeploymentPoint> onHoverSiegeMachine, bool isSelected)
	{
		_deploymentCamera = deploymentCamera;
		DeploymentPoint = selectedDeploymentPoint;
		_onSelect = onSelectSiegeMachine;
		_onHover = onHoverSiegeMachine;
		SiegeWeapon = siegeMachine;
		IsSelected = isSelected;
		if (siegeMachine != null)
		{
			MachineType = ((object)siegeMachine).GetType();
			Machine = OrderSiegeMachineVM.GetSiegeType(MachineType, siegeMachine.Side);
			MachineClass = siegeMachine.GetSiegeEngineType().StringId;
		}
		else
		{
			MachineType = null;
			MachineClass = "Empty";
		}
		Type = (int)selectedDeploymentPoint.GetDeploymentPointType();
		_worldPos = selectedDeploymentPoint.GameEntity.GlobalPosition;
		IsPlayerGeneral = Mission.Current.PlayerTeam.IsPlayerGeneral;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		BreachedText = new TextObject("{=D0TbQm4r}BREACHED").ToString();
	}

	public void Update()
	{
		CalculatePosition();
		RefreshPosition();
	}

	public void CalculatePosition()
	{
		_latestX = 0f;
		_latestY = 0f;
		MatrixFrame viewProj = MatrixFrame.Identity;
		_deploymentCamera.GetViewProjMatrix(ref viewProj);
		Vec3 worldPos = _worldPos;
		worldPos.z += 8f;
		worldPos.w = 1f;
		Vec3 vec = worldPos * viewProj;
		IsInFront = vec.w > 0f;
		vec.x /= vec.w;
		vec.y /= vec.w;
		vec.z /= vec.w;
		vec.w /= vec.w;
		vec *= 0.5f;
		vec.x += 0.5f;
		vec.y += 0.5f;
		vec.y = 1f - vec.y;
		int num = (int)Screen.RealScreenResolutionWidth;
		int num2 = (int)Screen.RealScreenResolutionHeight;
		_latestX = vec.x * (float)num;
		_latestY = vec.y * (float)num2;
	}

	public void RefreshPosition()
	{
		IsInside = IsInsideWindow();
		Position = new Vec2(_latestX, _latestY);
	}

	private bool IsInsideWindow()
	{
		if (!(_latestX > Screen.RealScreenResolutionWidth) && !(_latestY > Screen.RealScreenResolutionHeight) && !(_latestX + 200f < 0f))
		{
			return !(_latestY + 100f < 0f);
		}
		return false;
	}

	public void ExecuteAction()
	{
		_onSelect?.Invoke(this);
	}

	public void ExecuteFocusBegin()
	{
		_onHover?.Invoke(DeploymentPoint);
	}

	public void ExecuteFocusEnd()
	{
		_onHover?.Invoke(null);
	}

	public void RefreshWithDeployedWeapon()
	{
		SiegeWeapon siegeWeapon = (SiegeWeapon = DeploymentPoint.DeployedWeapon as SiegeWeapon);
		if (siegeWeapon != null)
		{
			MachineType = ((object)siegeWeapon).GetType();
			MachineClass = siegeWeapon.GetSiegeEngineType().StringId;
		}
		else
		{
			MachineType = null;
			MachineClass = "none";
		}
	}
}
