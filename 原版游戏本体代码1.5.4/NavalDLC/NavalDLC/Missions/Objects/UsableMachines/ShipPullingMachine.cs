using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipPullingMachine : UsableMachine
{
	private const string ShipPullPointTag = "ShipPullPoint";

	private const float pullForceMult = 25f;

	private float currentDirection;

	private GameEntity pointToPull;

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	private void RotateMachine(float dt)
	{
		float num = 0f;
		Vec2 vec = new Vec2(0f - Input.GetMouseMoveX(), 0f - Input.GetMouseMoveY());
		if (vec.IsNonZero())
		{
			float num2 = Math.Min(vec.Normalize(), 5f) * 0.2f;
			num = vec.x * num2;
		}
		if (num != 0f)
		{
			currentDirection += 1f * dt * num;
			currentDirection = MBMath.WrapAngle(currentDirection);
		}
		MatrixFrame frame = base.GameEntity.GetFrame();
		frame.rotation = Mat3.Identity;
		frame.rotation.RotateAboutUp(currentDirection);
		base.GameEntity.SetFrame(ref frame);
	}

	protected override void OnFixedTick(float fixedDt)
	{
	}

	protected override void OnTick(float dt)
	{
		if (base.UserCountNotInStruckAction <= 0 || base.PilotAgent == null)
		{
			return;
		}
		RotateMachine(dt);
		if (!base.PilotAgent.IsInBeingStruckAction && base.PilotAgent.Mission.InputManager.IsGameKeyDown(9))
		{
			if (pointToPull != null)
			{
				PullOtherShip(pointToPull);
			}
		}
		else
		{
			FindPointToPull();
		}
	}

	private void FindPointToPull()
	{
		WeakGameEntity pullPointHolderEntity = WeakGameEntity.Invalid;
		foreach (WeakGameEntity child in base.GameEntity.Root.GetChildren())
		{
			if (child.Name == "pull_point_holder")
			{
				pullPointHolderEntity = child;
				break;
			}
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 v = globalFrame.rotation.f.NormalizedCopy();
		IEnumerable<GameEntity> enumerable = from x in base.Scene.FindEntitiesWithTag("ShipPullPoint")
			where x.Parent != pullPointHolderEntity
			select x;
		GameEntity gameEntity = null;
		float num = -1.1f;
		Vec3 lookDirection = base.StandingPoints[0].UserAgent.LookDirection;
		Vec3 position = base.StandingPoints[0].UserAgent.Position;
		lookDirection.Normalize();
		foreach (GameEntity item in enumerable)
		{
			MatrixFrame globalFrame2 = item.GetGlobalFrame();
			if (Vec3.DotProduct(globalFrame2.origin - globalFrame.origin, v) > 0f && Vec3.DotProduct(globalFrame2.rotation.f.NormalizedCopy(), v) < 0f)
			{
				float num2 = Vec3.DotProduct((globalFrame2.origin - position).NormalizedCopy(), lookDirection);
				if (num2 > num)
				{
					num = num2;
					gameEntity = item;
				}
			}
		}
		if (gameEntity != null)
		{
			pointToPull = gameEntity;
		}
	}

	private void PullOtherShip(GameEntity otherAttachmentPoint)
	{
		MissionShip firstScriptOfType = base.GameEntity.Root.GetFirstScriptOfType<MissionShip>();
		MissionShip firstScriptOfType2 = otherAttachmentPoint.Root.GetFirstScriptOfType<MissionShip>();
		Vec3 vec = otherAttachmentPoint.GlobalPosition - base.GameEntity.GlobalPosition;
		vec.Normalize();
		float num = 25f;
		NavalDLC.Missions.NavalPhysics.NavalPhysics physics = firstScriptOfType.Physics;
		MatrixFrame frame = base.GameEntity.GetFrame();
		physics.ApplyGlobalForceAtLocalPos(in frame.origin, vec * num);
		NavalDLC.Missions.NavalPhysics.NavalPhysics physics2 = firstScriptOfType2.Physics;
		frame = otherAttachmentPoint.GetFrame();
		physics2.ApplyGlobalForceAtLocalPos(in frame.origin, -vec * num);
	}

	protected override void OnMissionReset()
	{
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=5Pf5coO6}Ship Pulling machine");
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipPullingMachineAI(this);
	}
}
