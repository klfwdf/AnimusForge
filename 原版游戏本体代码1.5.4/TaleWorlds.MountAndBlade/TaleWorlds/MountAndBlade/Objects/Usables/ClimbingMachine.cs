using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class ClimbingMachine : UsableMachine
{
	private const float ClimbingEndDisplacement = 0.3f;

	private const float ClimbingSpeed = 1.4f;

	private const float EndingAnimationTriggerDifference = 1.8f;

	private const string ClimbingLoopActionName = "act_climb_net";

	private const string ClimbingEndActionName = "act_climb_net_ending";

	private const string ClimbingEndContinueActionName = "act_climb_net_ending_continue";

	private ActionIndexCache _climbingLoop;

	private ActionIndexCache _climbingEnd;

	private ActionIndexCache _climbingEndContinue;

	private GameEntity _climbEndingPoint;

	private List<float> _standingPointUsageDurations = new List<float>();

	public override float SinkingReferenceOffset => base.GameEntity.GetGlobalScale().z * -1.5f;

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=mUIew6a6}Climbing Net");
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_climbingLoop = ActionIndexCache.Create("act_climb_net");
		_climbingEnd = ActionIndexCache.Create("act_climb_net_ending");
		_climbingEndContinue = ActionIndexCache.Create("act_climb_net_ending_continue");
		_climbEndingPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("climb_end"));
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.AutoEquipWeaponsOnUseStopped = true;
			_standingPointUsageDurations.Add(0f);
		}
		SetScriptComponentToTick(GetTickRequirement());
	}

	private void OnUseAction(Agent userAgent)
	{
		userAgent.SetForceAttachedEntity(base.GameEntity);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	public override void OnDeploymentFinished()
	{
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.AddComponent(new ResetGravityExclusionAndEntityAttachmentOnStopUsageComponent(OnUseAction));
			standingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
			standingPoint.SetAreUserPositionsUpdatedInTheMachineTick(value: true);
		}
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		bool isDeactivated = false;
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			StandingPoint standingPoint = base.StandingPoints[i];
			float num = _standingPointUsageDurations[i];
			if (standingPoint.HasUser)
			{
				Agent userAgent = standingPoint.UserAgent;
				ActionIndexCache currentAction = userAgent.GetCurrentAction(0);
				num += dt;
				if (currentAction == _climbingLoop || currentAction == _climbingEnd)
				{
					MatrixFrame globalFrame = standingPoint.GameEntity.GetGlobalFrame();
					Vec3 vec = globalFrame.origin + globalFrame.rotation.u.NormalizedCopy() * num * 1.4f;
					if (currentAction == _climbingEnd)
					{
						vec += new Vec3(globalFrame.rotation.f.AsVec2.Normalized() * (Math.Min(userAgent.GetCurrentActionProgress(0) * 1f, 1f) * 0.3f));
					}
					userAgent.SetTargetZ(vec.z);
					userAgent.SetTargetPositionAndDirection(vec.AsVec2, globalFrame.rotation.f.NormalizedCopy());
					Vec3 targetUp = globalFrame.rotation.u.NormalizedCopy();
					userAgent.SetTargetUp(in targetUp);
					float num2 = Vec3.DotProduct(targetUp, _climbEndingPoint.GlobalPosition - vec);
					if (currentAction == _climbingLoop && num2 < 1.8f && !userAgent.SetActionChannel(0, in _climbingEnd, ignorePriority: false, (AnimFlags)0uL))
					{
						userAgent.StopUsingGameObject();
						num = 0f;
					}
					else if (num2 > Vec3.DotProduct(targetUp, _climbEndingPoint.GlobalPosition - globalFrame.origin) - 1.5f)
					{
						isDeactivated = true;
					}
				}
				else if (currentAction == _climbingEndContinue)
				{
					userAgent.ClearTargetFrame();
					userAgent.SetTargetZ(_climbEndingPoint.GlobalPosition.z);
					userAgent.SetTargetUp(in Vec3.Zero);
					if (userAgent.GetCurrentActionProgress(0) > 0.95f)
					{
						userAgent.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: true);
						userAgent.StopUsingGameObject(isSuccessful: false);
						num = 0f;
					}
				}
				else if (userAgent.SetActionChannel(0, in _climbingLoop, ignorePriority: false, (AnimFlags)0uL))
				{
					userAgent.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
					isDeactivated = true;
				}
				else
				{
					userAgent.StopUsingGameObject();
					num = 0f;
				}
			}
			else
			{
				num = 0f;
			}
			_standingPointUsageDurations[i] = num;
		}
		foreach (StandingPoint standingPoint2 in base.StandingPoints)
		{
			if (!standingPoint2.HasUser)
			{
				standingPoint2.IsDeactivated = isDeactivated;
				isDeactivated = true;
			}
		}
	}

	public override void OnMissionEnded()
	{
	}
}
