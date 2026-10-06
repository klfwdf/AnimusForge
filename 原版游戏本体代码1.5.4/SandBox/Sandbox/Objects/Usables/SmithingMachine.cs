using System.Linq;
using SandBox.AI;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class SmithingMachine : UsableMachine
{
	private enum State
	{
		Stable,
		Preparation,
		Working,
		Paused,
		UseAnvilPoint
	}

	private const string MachineIdleAnimationName = "anim_merchant_smithing_machine_idle";

	private const string MachineIdleWithBlendInAnimationName = "anim_merchant_smithing_machine_idle_with_blend_in";

	private const string MachineUseAnimationName = "anim_merchant_smithing_machine_loop";

	private static readonly ActionIndexCache[] _actionsWithoutLeftHandItem = new ActionIndexCache[4]
	{
		ActionIndexCache.act_smithing_machine_anvil_start,
		ActionIndexCache.act_smithing_machine_anvil_part_2,
		ActionIndexCache.act_smithing_machine_anvil_part_4,
		ActionIndexCache.act_smithing_machine_anvil_part_5
	};

	private AnimationPoint _anvilUsePoint;

	private AnimationPoint _machineUsePoint;

	private State _state;

	private Timer _disableTimer;

	private float _remainingTimeToReset;

	private bool _leftItemIsVisible;

	protected override void OnInit()
	{
		base.OnInit();
		_machineUsePoint = (AnimationPoint)base.PilotStandingPoint;
		if (_machineUsePoint == null)
		{
			_ = "Entity(" + base.GameEntity.Name + ") with script(SmithingMachine) does not have a valid 'PilotStandingPoint'.";
		}
		_machineUsePoint.IsDeactivated = false;
		_machineUsePoint.IsDisabledForPlayers = true;
		_machineUsePoint.KeepOldVisibility = true;
		_anvilUsePoint = (AnimationPoint)base.StandingPoints.First((StandingPoint x) => x != _machineUsePoint);
		_anvilUsePoint.IsDeactivated = true;
		_anvilUsePoint.KeepOldVisibility = true;
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.IsDisabledForPlayers = true;
		}
		SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle", 0);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=OCRafO5h}Bellows");
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		switch (_state)
		{
		case State.Stable:
			if (_machineUsePoint.HasUser && _machineUsePoint.UserAgent.GetCurrentVelocity().LengthSquared < 0.0001f)
			{
				_machineUsePoint.UserAgent.SetActionChannel(0, in ActionIndexCache.act_use_smithing_machine_ready, ignorePriority: false, (AnimFlags)0uL);
				_state = State.Preparation;
			}
			if (_anvilUsePoint.HasUser)
			{
				_state = State.UseAnvilPoint;
			}
			break;
		case State.Preparation:
			if (!_machineUsePoint.HasUser)
			{
				SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0);
				_state = State.Stable;
			}
			else if (_machineUsePoint.UserAgent.GetCurrentAction(0) == ActionIndexCache.act_use_smithing_machine_ready && _machineUsePoint.UserAgent.GetCurrentActionProgress(0) > 0.99f)
			{
				SetAnimationAtChannelSynched("anim_merchant_smithing_machine_loop", 0);
				_machineUsePoint.UserAgent.SetActionChannel(0, in ActionIndexCache.act_use_smithing_machine_loop, ignorePriority: false, (AnimFlags)0uL);
				_state = State.Working;
			}
			break;
		case State.Working:
			if (!_machineUsePoint.HasUser)
			{
				SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0);
				_state = State.Stable;
				_disableTimer = null;
				_anvilUsePoint.IsDeactivated = false;
			}
			else if (_machineUsePoint.UserAgent.GetCurrentAction(0) != ActionIndexCache.act_use_smithing_machine_loop)
			{
				SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0);
				_state = State.Paused;
				_remainingTimeToReset = _disableTimer.Duration - _disableTimer.ElapsedTime();
			}
			else if (_disableTimer == null)
			{
				_disableTimer = new Timer(Mission.Current.CurrentTime, 9.8f);
			}
			else if (_disableTimer.Check(Mission.Current.CurrentTime))
			{
				SetAnimationAtChannelSynched("anim_merchant_smithing_machine_idle_with_blend_in", 0);
				_disableTimer = null;
				_machineUsePoint.IsDeactivated = true;
				_anvilUsePoint.IsDeactivated = false;
				_state = State.Stable;
			}
			break;
		case State.Paused:
			if (_machineUsePoint.IsRotationCorrectDuringUsage())
			{
				_machineUsePoint.UserAgent.SetActionChannel(0, in ActionIndexCache.act_use_smithing_machine_ready, ignorePriority: false, (AnimFlags)0uL);
			}
			if (_machineUsePoint.UserAgent.GetCurrentAction(0) == ActionIndexCache.act_use_smithing_machine_ready)
			{
				_state = State.Preparation;
				_disableTimer.Reset(Mission.Current.CurrentTime, _remainingTimeToReset);
				_remainingTimeToReset = 0f;
			}
			break;
		case State.UseAnvilPoint:
		{
			if (!_anvilUsePoint.HasUser)
			{
				_state = State.Stable;
				_disableTimer = null;
				_machineUsePoint.IsDeactivated = false;
				break;
			}
			if (_disableTimer == null)
			{
				_disableTimer = new Timer(Mission.Current.CurrentTime, 96f);
				_leftItemIsVisible = true;
				break;
			}
			if (_disableTimer.Check(Mission.Current.CurrentTime))
			{
				_disableTimer = null;
				_anvilUsePoint.IsDeactivated = true;
				_machineUsePoint.IsDeactivated = false;
				_state = State.Stable;
				break;
			}
			ActionIndexCache currentAction = _anvilUsePoint.UserAgent.GetCurrentAction(0);
			if (_leftItemIsVisible && _actionsWithoutLeftHandItem.Contains(currentAction))
			{
				_anvilUsePoint.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(_anvilUsePoint.UserAgent.Monster.OffHandItemBoneIndex, _anvilUsePoint.LeftHandItem, isVisible: false);
				_leftItemIsVisible = false;
			}
			else if (!_leftItemIsVisible && !_actionsWithoutLeftHandItem.Contains(currentAction))
			{
				_anvilUsePoint.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(_anvilUsePoint.UserAgent.Monster.OffHandItemBoneIndex, _anvilUsePoint.LeftHandItem, isVisible: true);
				_leftItemIsVisible = true;
			}
			break;
		}
		}
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new UsablePlaceAI(this);
	}
}
