using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions;

public class MissionAgentAlarmTargetVM : ViewModel
{
	private enum AlarmStateEnum
	{
		Invalid = -1,
		None,
		Default,
		Cautious,
		PatrollingCautious,
		Alarmed
	}

	public readonly Agent TargetAgent;

	private readonly Action<MissionAgentAlarmTargetVM> _onRemove;

	private float _latestX;

	private float _latestY;

	private float _latestW;

	private float _wPosAfterPositionCalculation;

	private AlarmedBehaviorGroup _alarmedBehaviorGroupCache;

	private bool _isStealthModeEnabled;

	private bool _isMainAgentInVisibilityRange;

	private bool _isInVision;

	private bool _isSuspected;

	private string _alarmState;

	private int _wSign;

	private int _alarmProgress;

	private Vec2 _screenPosition;

	public bool HasCautiousness
	{
		get
		{
			if (!TargetAgent.AIStateFlags.HasAnyFlag(Agent.AIStateFlag.Alarmed))
			{
				return AlarmedBehaviorGroup.AlarmFactor > 0f;
			}
			return true;
		}
	}

	public AlarmedBehaviorGroup AlarmedBehaviorGroup
	{
		get
		{
			if (_alarmedBehaviorGroupCache == null)
			{
				_alarmedBehaviorGroupCache = TargetAgent.GetComponent<CampaignAgentComponent>().AgentNavigator?.GetBehaviorGroup<AlarmedBehaviorGroup>();
			}
			return _alarmedBehaviorGroupCache;
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
	public bool IsMainAgentInVisibilityRange
	{
		get
		{
			return _isMainAgentInVisibilityRange;
		}
		set
		{
			if (value != _isMainAgentInVisibilityRange)
			{
				_isMainAgentInVisibilityRange = value;
				OnPropertyChangedWithValue(value, "IsMainAgentInVisibilityRange");
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
	public bool IsSuspected
	{
		get
		{
			return _isSuspected;
		}
		set
		{
			if (value != _isSuspected)
			{
				_isSuspected = value;
				OnPropertyChangedWithValue(value, "IsSuspected");
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
	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (value != _wSign)
			{
				_wSign = value;
				OnPropertyChangedWithValue(value, "WSign");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 ScreenPosition
	{
		get
		{
			return _screenPosition;
		}
		set
		{
			if (value.x != _screenPosition.x || value.y != _screenPosition.y)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
			}
		}
	}

	public MissionAgentAlarmTargetVM(Agent agent, Action<MissionAgentAlarmTargetVM> onRemove)
	{
		TargetAgent = agent;
		_onRemove = onRemove;
	}

	public void UpdateValues()
	{
		string agentAlarmState = GetAgentAlarmState(TargetAgent.AIStateFlags);
		float num = AlarmedBehaviorGroup?.AlarmFactor ?? 0f;
		if (num > 1f)
		{
			num = TaleWorlds.Library.MathF.Min(num, 2f);
			num -= 1f;
			num = TaleWorlds.Library.MathF.Lerp(0.3f, 1f, num);
		}
		if (!IsInVision || !IsStealthModeEnabled || (!((float)AlarmProgress > 0f) && !IsMainAgentInVisibilityRange))
		{
			AlarmProgress = 0;
			AlarmState = AlarmStateEnum.Invalid.ToString();
		}
		else
		{
			AlarmState = agentAlarmState;
			AlarmProgress = (int)(num * 100f);
		}
	}

	private static string GetAgentAlarmState(Agent.AIStateFlag stateFlag)
	{
		if ((stateFlag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Alarmed)
		{
			return AlarmStateEnum.Alarmed.ToString();
		}
		if ((stateFlag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Cautious)
		{
			return AlarmStateEnum.Cautious.ToString();
		}
		if ((stateFlag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.PatrollingCautious)
		{
			return AlarmStateEnum.PatrollingCautious.ToString();
		}
		return AlarmStateEnum.None.ToString();
	}

	public void UpdateScreenPosition(Camera missionCamera)
	{
		Vec3 position = TargetAgent.Position;
		position.z += TargetAgent.GetEyeGlobalHeight() + 0.35f;
		_latestX = 0f;
		_latestY = 0f;
		_latestW = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(missionCamera, position, ref _latestX, ref _latestY, ref _latestW);
		_wPosAfterPositionCalculation = ((_latestW < 0f) ? (-1f) : 1.1f);
		WSign = (int)_wPosAfterPositionCalculation;
		ScreenPosition = new Vec2(_latestX, _latestY);
		_ = WSign;
		_ = 0;
	}

	public void ExecuteRemove()
	{
		_onRemove?.Invoke(this);
	}
}
