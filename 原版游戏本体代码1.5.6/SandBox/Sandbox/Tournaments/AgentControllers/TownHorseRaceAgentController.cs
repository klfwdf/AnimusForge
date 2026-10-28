using System;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.AgentControllers;

public class TownHorseRaceAgentController : AgentController
{
	private TownHorseRaceMissionController _controller;

	private int _checkPointIndex;

	private int _tourCount;

	public override void OnInitialize()
	{
		_controller = base.Mission.GetMissionBehavior<TownHorseRaceMissionController>();
		_checkPointIndex = 0;
		_tourCount = 0;
	}

	public void DisableMovement()
	{
		if (base.Owner.IsAIControlled)
		{
			WorldPosition scriptedPosition = base.Owner.GetWorldPosition();
			base.Owner.SetScriptedPositionAndDirection(ref scriptedPosition, base.Owner.Frame.rotation.f.AsVec2.RotationInRadians, addHumanLikeDelay: false);
		}
	}

	public void Start()
	{
		if (_checkPointIndex < _controller.CheckPoints.Count)
		{
			TownHorseRaceMissionController.CheckPoint checkPoint = _controller.CheckPoints[_checkPointIndex];
			checkPoint.AddToCheckList(base.Owner);
			if (base.Owner.IsAIControlled)
			{
				WorldPosition position = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, checkPoint.GetBestTargetPosition(), hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
		}
	}

	public void OnEnterCheckPoint(VolumeBox checkPoint)
	{
		_controller.CheckPoints[_checkPointIndex].RemoveFromCheckList(base.Owner);
		_checkPointIndex++;
		if (_checkPointIndex < _controller.CheckPoints.Count)
		{
			if (base.Owner.IsAIControlled)
			{
				WorldPosition position = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _controller.CheckPoints[_checkPointIndex].GetBestTargetPosition(), hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
			_controller.CheckPoints[_checkPointIndex].AddToCheckList(base.Owner);
			return;
		}
		_tourCount++;
		if (_tourCount < 2)
		{
			_checkPointIndex = 0;
			if (base.Owner.IsAIControlled)
			{
				WorldPosition position2 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _controller.CheckPoints[_checkPointIndex].GetBestTargetPosition(), hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position2, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
			_controller.CheckPoints[_checkPointIndex].AddToCheckList(base.Owner);
		}
	}
}
