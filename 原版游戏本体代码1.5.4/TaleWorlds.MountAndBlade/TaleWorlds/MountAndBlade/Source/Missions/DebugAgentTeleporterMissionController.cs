using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class DebugAgentTeleporterMissionController : MissionLogic
{
	public override void AfterStart()
	{
	}

	public override void OnMissionTick(float dt)
	{
		Agent agent = null;
		int debugAgent = base.Mission.GetDebugAgent();
		foreach (Agent agent3 in base.Mission.Agents)
		{
			if (debugAgent == agent3.Index)
			{
				agent = agent3;
				break;
			}
		}
		if (agent == null && base.Mission.Agents.Count > 0)
		{
			int num = MBRandom.RandomInt(base.Mission.Agents.Count);
			int count = base.Mission.Agents.Count;
			int num2 = num;
			do
			{
				Agent agent2 = base.Mission.Agents[num2];
				if (agent2 != Agent.Main && agent2.IsActive())
				{
					agent = agent2;
					base.Mission.SetDebugAgent(num2);
					break;
				}
				num2 = (num2 + 1) % count;
			}
			while (num2 != num);
		}
		if (agent == null)
		{
			return;
		}
		MatrixFrame lastFinalRenderCameraFrame = base.Mission.Scene.LastFinalRenderCameraFrame;
		if (Input.DebugInput.IsKeyDown(InputKey.MiddleMouseButton))
		{
			base.Mission.Scene.RayCastForClosestEntityOrTerrain(lastFinalRenderCameraFrame.origin, lastFinalRenderCameraFrame.origin + -lastFinalRenderCameraFrame.rotation.u * 100f, out var _, 0.01f, BodyFlags.CommonCollisionExcludeFlags);
		}
		if (Input.DebugInput.IsKeyReleased(InputKey.MiddleMouseButton) && base.Mission.Scene.RayCastForClosestEntityOrTerrain(lastFinalRenderCameraFrame.origin, lastFinalRenderCameraFrame.origin + -lastFinalRenderCameraFrame.rotation.u * 100f, out var collisionDistance2, 0.01f, BodyFlags.CommonCollisionExcludeFlags))
		{
			Vec3 position = lastFinalRenderCameraFrame.origin + -lastFinalRenderCameraFrame.rotation.u * collisionDistance2;
			if (Input.DebugInput.IsHotKeyReleased("DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent"))
			{
				agent.TeleportToPosition(position);
			}
			else
			{
				Vec2 vec = -lastFinalRenderCameraFrame.rotation.u.AsVec2;
				WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false);
				agent.SetScriptedPositionAndDirection(ref scriptedPosition, vec.RotationInRadians, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NoAttack);
			}
		}
		if (Input.DebugInput.IsHotKeyPressed("DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement"))
		{
			agent.DisableScriptedMovement();
		}
	}
}
