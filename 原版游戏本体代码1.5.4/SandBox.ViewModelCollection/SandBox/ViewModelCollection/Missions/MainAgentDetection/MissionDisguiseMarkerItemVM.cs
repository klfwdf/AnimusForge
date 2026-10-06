using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection;

public class MissionDisguiseMarkerItemVM : ViewModel
{
	public enum AgentAlarmStateEnum
	{
		None = -1,
		Alarmed,
		Cautious,
		PatrollingCautious,
		Suspicious,
		Visible
	}

	public enum AgentStealthOffenseType
	{
		None = -1,
		Default,
		Visible,
		Suspicious
	}

	private Camera _missionCamera;

	private AgentAlarmStateEnum _activeAlarmState;

	private AgentStealthOffenseType _offenseType;

	private Vec2 _screenPosition;

	private int _alarmProgress;

	private string _alarmState;

	private string _offenseTypeIdentifier;

	private bool _isStealthModeEnabled;

	private bool _isSuspicious;

	private bool _isTarget;

	private bool _isInVision;

	private bool _isInVisibilityRange;

	public DisguiseMissionLogic.ShadowingAgentOffenseInfo OffenseInfo { get; }

	[DataSourceProperty]
	public Vec2 ScreenPosition
	{
		get
		{
			return _screenPosition;
		}
		set
		{
			if (value != _screenPosition)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
			}
		}
	}

	[DataSourceProperty]
	public int AlarmProgress
	{
		get
		{
			return _alarmProgress;
		}
		set
		{
			if (value != _alarmProgress)
			{
				_alarmProgress = value;
				OnPropertyChangedWithValue(value, "AlarmProgress");
			}
		}
	}

	[DataSourceProperty]
	public string AlarmState
	{
		get
		{
			return _alarmState;
		}
		set
		{
			if (value != _alarmState)
			{
				_alarmState = value;
				OnPropertyChangedWithValue(value, "AlarmState");
			}
		}
	}

	[DataSourceProperty]
	public string OffenseTypeIdentifier
	{
		get
		{
			return _offenseTypeIdentifier;
		}
		set
		{
			if (value != _offenseTypeIdentifier)
			{
				_offenseTypeIdentifier = value;
				OnPropertyChangedWithValue(value, "OffenseTypeIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public bool IsStealthModeEnabled
	{
		get
		{
			return _isStealthModeEnabled;
		}
		set
		{
			if (value != _isStealthModeEnabled)
			{
				_isStealthModeEnabled = value;
				OnPropertyChangedWithValue(value, "IsStealthModeEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSuspicious
	{
		get
		{
			return _isSuspicious;
		}
		set
		{
			if (value != _isSuspicious)
			{
				_isSuspicious = value;
				OnPropertyChangedWithValue(value, "IsSuspicious");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTarget
	{
		get
		{
			return _isTarget;
		}
		set
		{
			if (value != _isTarget)
			{
				_isTarget = value;
				OnPropertyChangedWithValue(value, "IsTarget");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInVision
	{
		get
		{
			return _isInVision;
		}
		set
		{
			if (value != _isInVision)
			{
				_isInVision = value;
				OnPropertyChangedWithValue(value, "IsInVision");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInVisibilityRange
	{
		get
		{
			return _isInVisibilityRange;
		}
		set
		{
			if (value != _isInVisibilityRange)
			{
				_isInVisibilityRange = value;
				OnPropertyChangedWithValue(value, "IsInVisibilityRange");
			}
		}
	}

	public MissionDisguiseMarkerItemVM(Camera missionCamera, DisguiseMissionLogic.ShadowingAgentOffenseInfo offenseInfo)
	{
		_missionCamera = missionCamera;
		OffenseInfo = offenseInfo;
	}

	public void RefreshVisuals()
	{
		OffenseTypeIdentifier = GetOffenseTypeIdentifier(OffenseInfo?.OffenseType ?? StealthOffenseTypes.None);
		UpdateAlarmState();
	}

	public void UpdatePosition()
	{
		float screenX = 0f;
		float screenY = 0f;
		float w = 0f;
		Vec3 position = OffenseInfo.Agent.Position;
		position.z += OffenseInfo.Agent.GetEyeGlobalHeight() + 0.35f;
		if (position.IsValid)
		{
			MBWindowManager.WorldToScreenInsideUsableArea(_missionCamera, position, ref screenX, ref screenY, ref w);
		}
		if (!position.IsValid || w < 0f || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY))
		{
			screenX = -10000f;
			screenY = -10000f;
			w = 0f;
		}
		ScreenPosition = new Vec2(screenX, screenY);
	}

	private void UpdateAlarmState()
	{
		Agent agent = OffenseInfo.Agent;
		AlarmedBehaviorGroup alarmedBehaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator?.GetBehaviorGroup<AlarmedBehaviorGroup>();
		Agent.AIStateFlag aIStateFlags = agent.AIStateFlags;
		if (aIStateFlags.HasAnyFlag(Agent.AIStateFlag.Alarmed))
		{
			_activeAlarmState = AgentAlarmStateEnum.Alarmed;
		}
		else if (aIStateFlags.HasAnyFlag(Agent.AIStateFlag.Cautious))
		{
			_activeAlarmState = AgentAlarmStateEnum.Cautious;
		}
		else if (aIStateFlags.HasAnyFlag(Agent.AIStateFlag.PatrollingCautious))
		{
			_activeAlarmState = AgentAlarmStateEnum.PatrollingCautious;
		}
		else
		{
			_activeAlarmState = AgentAlarmStateEnum.None;
		}
		float num = ((!aIStateFlags.HasAnyFlag(Agent.AIStateFlag.Alarmed)) ? MathF.Clamp(alarmedBehaviorGroup.AlarmFactor / 2f, 0f, 1f) : 1f);
		AlarmState = _activeAlarmState.ToString();
		AlarmProgress = (int)(num * 100f);
	}

	private string GetOffenseTypeIdentifier(StealthOffenseTypes offenseType)
	{
		if (IsStealthModeEnabled || !IsInVision || !IsInVisibilityRange)
		{
			_offenseType = AgentStealthOffenseType.None;
			return _offenseType.ToString();
		}
		switch (offenseType)
		{
		case StealthOffenseTypes.None:
			_offenseType = AgentStealthOffenseType.Default;
			break;
		case StealthOffenseTypes.IsVisible:
			_offenseType = ((!IsSuspicious) ? AgentStealthOffenseType.Visible : AgentStealthOffenseType.Suspicious);
			break;
		case StealthOffenseTypes.IsInPersonalZone:
			_offenseType = AgentStealthOffenseType.Suspicious;
			break;
		}
		return _offenseType.ToString();
	}
}
