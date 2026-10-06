using NavalDLC.Storyline.MissionControllers;
using SandBox.AI;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.Objects;

public class BurnShipObject : UsableMachine
{
	public float UseTime = 5f;

	private DynamicObjectAnimationPoint _machineUsePoint;

	private BlockedEstuaryMissionController _controller;

	private bool _hasUserCached;

	private bool _stateSet;

	private bool _used;

	private float _timer;

	public bool HasUser => _machineUsePoint.HasUser;

	public override bool IsDeactivated => _used;

	protected override void OnInit()
	{
		base.OnInit();
		_controller = Mission.Current.GetMissionBehavior<BlockedEstuaryMissionController>();
		_machineUsePoint = (DynamicObjectAnimationPoint)base.PilotStandingPoint;
		_machineUsePoint.IsDeactivated = false;
		_machineUsePoint.IsDisabledForPlayers = true;
		_machineUsePoint.LockUserFrames = false;
		_machineUsePoint.LockUserPositions = false;
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_hasUserCached != HasUser)
		{
			_timer = 0f;
			_hasUserCached = HasUser;
		}
		if (!_used)
		{
			if (_machineUsePoint.HasUser && !_stateSet)
			{
				ActionIndexCache actionIndexCache = ActionIndexCache.Create(_machineUsePoint.LoopStartAction);
				_machineUsePoint.UserAgent.SetActionChannel(0, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL);
				_stateSet = true;
			}
			if (_machineUsePoint.HasUser)
			{
				_timer += dt;
			}
			if (_stateSet && _machineUsePoint.HasUser && _timer > UseTime)
			{
				OnUse();
			}
		}
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=eAnAZNib}Barrel of oil");
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = GameTexts.FindText("str_key_action");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new UsablePlaceAI(this);
	}

	private void OnUse()
	{
		_machineUsePoint.UserAgent.StopUsingGameObject();
		SetDisabled(isParentObject: true);
		_used = true;
		_controller.OnBurningMachineUsed(this);
	}
}
