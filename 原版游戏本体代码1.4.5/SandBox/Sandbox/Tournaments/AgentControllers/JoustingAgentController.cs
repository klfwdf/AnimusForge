using System;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.AgentControllers;

public class JoustingAgentController : AgentController
{
	public enum JoustingAgentState
	{
		GoingToBackStart,
		GoToStartPosition,
		WaitInStartPosition,
		WaitingOpponent,
		Ready,
		StartRiding,
		Riding,
		RidingAtWrongSide,
		SwordDuel
	}

	private JoustingAgentState _state;

	public int CurrentCornerIndex;

	private const float MaxDistance = 3f;

	public int Score;

	private Agent _opponentAgent;

	public JoustingAgentState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (value != _state)
			{
				_state = value;
				JoustingMissionController.OnJoustingAgentStateChanged(base.Owner, value);
			}
		}
	}

	public TournamentJoustingMissionController JoustingMissionController { get; private set; }

	public Agent Opponent
	{
		get
		{
			if (_opponentAgent == null)
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent.IsHuman && agent != base.Owner)
					{
						_opponentAgent = agent;
					}
				}
			}
			return _opponentAgent;
		}
	}

	public bool PrepareEquipmentsAfterDismount { get; private set; }

	public override void OnInitialize()
	{
		JoustingMissionController = base.Mission.GetMissionBehavior<TournamentJoustingMissionController>();
		_state = JoustingAgentState.WaitingOpponent;
	}

	public void UpdateState()
	{
		if (base.Owner.Character.IsPlayerCharacter)
		{
			UpdateMainAgentState();
		}
		else
		{
			UpdateAIAgentState();
		}
	}

	private void UpdateMainAgentState()
	{
		JoustingAgentController controller = Opponent.GetController<JoustingAgentController>();
		bool flag = JoustingMissionController.CornerStartList[CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position) && !JoustingMissionController.RegionBoxList[CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position);
		switch (State)
		{
		case JoustingAgentState.GoToStartPosition:
			if (flag)
			{
				State = JoustingAgentState.WaitInStartPosition;
			}
			break;
		case JoustingAgentState.WaitInStartPosition:
			if (!flag)
			{
				State = JoustingAgentState.GoToStartPosition;
			}
			else if (base.Owner.GetCurrentVelocity().LengthSquared < 0.0025000002f)
			{
				State = JoustingAgentState.WaitingOpponent;
			}
			break;
		case JoustingAgentState.WaitingOpponent:
			if (!flag)
			{
				State = JoustingAgentState.GoToStartPosition;
			}
			else if (controller.State == JoustingAgentState.WaitingOpponent || controller.State == JoustingAgentState.Ready)
			{
				State = JoustingAgentState.Ready;
			}
			break;
		case JoustingAgentState.Ready:
			if (JoustingMissionController.IsAgentInTheTrack(base.Owner) && base.Owner.GetCurrentVelocity().LengthSquared > 0.0025000002f)
			{
				State = JoustingAgentState.Riding;
			}
			else if (controller.State == JoustingAgentState.GoToStartPosition)
			{
				State = JoustingAgentState.WaitingOpponent;
			}
			else if (!JoustingMissionController.CornerStartList[CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position))
			{
				State = JoustingAgentState.GoToStartPosition;
			}
			break;
		case JoustingAgentState.Riding:
			if (JoustingMissionController.IsAgentInTheTrack(base.Owner, inCurrentTrack: false))
			{
				State = JoustingAgentState.RidingAtWrongSide;
			}
			if (JoustingMissionController.RegionExitBoxList[CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position))
			{
				State = JoustingAgentState.GoToStartPosition;
				CurrentCornerIndex = 1 - CurrentCornerIndex;
			}
			break;
		case JoustingAgentState.RidingAtWrongSide:
			if (JoustingMissionController.IsAgentInTheTrack(base.Owner))
			{
				State = JoustingAgentState.Riding;
			}
			else if (JoustingMissionController.CornerStartList[1 - CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position))
			{
				State = JoustingAgentState.GoToStartPosition;
				CurrentCornerIndex = 1 - CurrentCornerIndex;
			}
			break;
		case JoustingAgentState.StartRiding:
			break;
		}
	}

	private void UpdateAIAgentState()
	{
		if (Opponent == null || !Opponent.IsActive())
		{
			return;
		}
		JoustingAgentController controller = Opponent.GetController<JoustingAgentController>();
		switch (State)
		{
		case JoustingAgentState.GoingToBackStart:
			if (base.Owner.Position.Distance(JoustingMissionController.CornerBackStartList[CurrentCornerIndex].origin) < 3f && base.Owner.GetCurrentVelocity().LengthSquared < 0.0025000002f)
			{
				CurrentCornerIndex = 1 - CurrentCornerIndex;
				MatrixFrame globalFrame = JoustingMissionController.CornerStartList[CurrentCornerIndex].GetGlobalFrame();
				WorldPosition scriptedPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, globalFrame.origin, hasValidZ: false);
				base.Owner.SetScriptedPositionAndDirection(ref scriptedPosition, globalFrame.rotation.f.AsVec2.RotationInRadians, addHumanLikeDelay: false);
				State = JoustingAgentState.GoToStartPosition;
			}
			break;
		case JoustingAgentState.GoToStartPosition:
			if (JoustingMissionController.CornerStartList[CurrentCornerIndex].CheckPointWithOrientedBoundingBox(base.Owner.Position) && base.Owner.GetCurrentVelocity().LengthSquared < 0.0025000002f)
			{
				State = JoustingAgentState.WaitingOpponent;
			}
			break;
		case JoustingAgentState.WaitingOpponent:
			if (controller.State == JoustingAgentState.WaitingOpponent || controller.State == JoustingAgentState.Ready)
			{
				State = JoustingAgentState.Ready;
			}
			break;
		case JoustingAgentState.Ready:
			if (controller.State == JoustingAgentState.Riding)
			{
				State = JoustingAgentState.StartRiding;
				WorldPosition position3 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, JoustingMissionController.CornerMiddleList[CurrentCornerIndex].origin, hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position3, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
			else if (controller.State == JoustingAgentState.Ready)
			{
				WorldPosition position4 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, JoustingMissionController.CornerStartList[CurrentCornerIndex].GetGlobalFrame().origin, hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position4, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
			else
			{
				State = JoustingAgentState.WaitingOpponent;
			}
			break;
		case JoustingAgentState.StartRiding:
			if (base.Owner.Position.Distance(JoustingMissionController.CornerMiddleList[CurrentCornerIndex].origin) < 3f)
			{
				WorldPosition position2 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, JoustingMissionController.CornerFinishList[CurrentCornerIndex].origin, hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position2, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
				State = JoustingAgentState.Riding;
			}
			break;
		case JoustingAgentState.Riding:
			if (base.Owner.Position.Distance(JoustingMissionController.CornerFinishList[CurrentCornerIndex].origin) < 3f)
			{
				WorldPosition position = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, JoustingMissionController.CornerBackStartList[CurrentCornerIndex].origin, hasValidZ: false);
				base.Owner.SetScriptedPosition(ref position, addHumanLikeDelay: false);
				State = JoustingAgentState.GoingToBackStart;
			}
			break;
		case JoustingAgentState.WaitInStartPosition:
			break;
		}
	}

	public void PrepareAgentToSwordDuel()
	{
		if (base.Owner.MountAgent != null)
		{
			base.Owner.Controller = AgentControllerType.AI;
			WorldPosition position = Opponent.GetWorldPosition();
			base.Owner.SetScriptedPosition(ref position, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.GoWithoutMount);
			PrepareEquipmentsAfterDismount = true;
		}
		else
		{
			PrepareEquipmentsForSwordDuel();
			base.Owner.DisableScriptedMovement();
		}
	}

	public void PrepareEquipmentsForSwordDuel()
	{
		AddEquipmentsForSwordDuel();
		base.Owner.WieldInitialWeapons();
		PrepareEquipmentsAfterDismount = false;
	}

	private void AddEquipmentsForSwordDuel()
	{
		base.Owner.DropItem(EquipmentIndex.WeaponItemBeginSlot);
		MissionWeapon weapon = new MissionWeapon(Game.Current.ObjectManager.GetObject<ItemObject>("wooden_sword_t1"), null, base.Owner.Origin?.Banner);
		base.Owner.EquipWeaponWithNewEntity(EquipmentIndex.Weapon2, ref weapon);
	}

	public bool IsRiding()
	{
		if (State != JoustingAgentState.StartRiding)
		{
			return State == JoustingAgentState.Riding;
		}
		return true;
	}
}
