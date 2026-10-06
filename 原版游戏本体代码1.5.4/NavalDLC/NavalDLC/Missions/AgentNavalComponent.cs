using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions;

public class AgentNavalComponent : AgentComponent
{
	private const float OffShipCheckInterval = 5f;

	private const float BreatheCheckInterval = 5f;

	private const float BurnCheckInterval = 1f;

	private const float BreatheHoldMaxDurationBase = 60f;

	private const float JumpOffTimerInSeconds = 2f;

	private AgentMovementMode _lastMovementMode;

	private float _lastBreatheTime;

	private float _lastBreatheCheckTime;

	private float _lastBurnCheckTime;

	private float _lastOffShipCheckTime;

	private float _breatheHoldMaxDurationFinal;

	private ulong _parentShipUniqueBitwiseID;

	private TeamAINavalComponent _teamAINavalComponent;

	private GameEntity _steppedEntityCache;

	private NavalDLC.Missions.NavalPhysics.NavalPhysics _steppedNavalPhysicsCached;

	private PlankBridgeSteppedAgentManager _steppedPlankBridgeSteppedAgentManagerCached;

	private readonly NavalShipsLogic _navalShipsLogic;

	private readonly NavalAgentsLogic _navalAgentsLogic;

	private float _lastJumpOffTime = -1f;

	public MissionShip SteppedShip { get; private set; }

	public MissionShip FormationShip { get; private set; }

	public bool BlockDrowning { get; private set; }

	public bool BlockBurning { get; private set; }

	public bool BlockCheckingOffShipConsideration { get; private set; }

	public bool BlockFormationCleanupOnShipAdabandonment { get; private set; }

	public bool IsJumpingOffOnCooldown => _lastJumpOffTime >= 0f;

	public AgentNavalComponent(Agent agent)
		: base(agent)
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
		_lastMovementMode = AgentMovementMode.None;
		_lastBreatheTime = agent.Mission.CurrentTime;
	}

	public override void Initialize()
	{
		_breatheHoldMaxDurationFinal = MissionGameModels.Current.AgentStatCalculateModel.GetBreatheHoldMaxDuration(Agent, 60f) * (MBRandom.RandomFloat + 0.5f);
		Mission.Current.DeploymentFinishedEvent += OnDeploymentFinished;
		_lastBreatheCheckTime = Agent.Mission.CurrentTime + MBRandom.RandomFloat * 5f;
		_lastOffShipCheckTime = Agent.Mission.CurrentTime + MBRandom.RandomFloat * 5f;
		_lastBurnCheckTime = Agent.Mission.CurrentTime + MBRandom.RandomFloat * 1f;
	}

	public override void OnComponentRemoved()
	{
		Mission.Current.DeploymentFinishedEvent -= OnDeploymentFinished;
	}

	public override void OnFormationSet()
	{
		base.OnFormationSet();
		if (Agent.Formation != null)
		{
			_teamAINavalComponent = Agent.Formation.Team?.TeamAI as TeamAINavalComponent;
			if (_navalShipsLogic.GetShip(Agent.Team.TeamSide, Agent.Formation.FormationIndex, out var ship))
			{
				FormationShip = ship;
				_parentShipUniqueBitwiseID = FormationShip.ShipUniqueBitwiseID;
			}
			else
			{
				FormationShip = null;
				_parentShipUniqueBitwiseID = 0uL;
			}
		}
		else
		{
			FormationShip = null;
			_parentShipUniqueBitwiseID = 0uL;
		}
	}

	public void OnShipCaptured()
	{
		if (_navalShipsLogic.GetShip(Agent.Team.TeamSide, Agent.Formation.FormationIndex, out var ship))
		{
			FormationShip = ship;
			_parentShipUniqueBitwiseID = FormationShip.ShipUniqueBitwiseID;
		}
		else
		{
			FormationShip = null;
			_parentShipUniqueBitwiseID = 0uL;
		}
	}

	public void SetCanDrown(bool canDrown)
	{
		BlockDrowning = !canDrown;
	}

	public void SetCanBurn(bool canBurn)
	{
		BlockBurning = !canBurn;
	}

	public void SetBlockOffShipConsideration(bool canCheckOffShipConsideration)
	{
		BlockCheckingOffShipConsideration = !canCheckOffShipConsideration;
	}

	public void SetBlockFormationCleanupOnShipAdabandonment(bool canCleanFormationOnShipAdabandonment)
	{
		BlockFormationCleanupOnShipAdabandonment = !canCleanFormationOnShipAdabandonment;
	}

	public float GetBreath()
	{
		float num = ((Agent.Controller != AgentControllerType.AI) ? (_breatheHoldMaxDurationFinal * 2f) : _breatheHoldMaxDurationFinal);
		return MBMath.ClampFloat((_lastBreatheTime + 5f + num - Agent.Mission.CurrentTime) / num, 0f, 1f);
	}

	public ulong GetSteppedCombinedShipIsland()
	{
		if (SteppedShip != null)
		{
			return SteppedShip.ShipIslandCombinedID;
		}
		if (_steppedPlankBridgeSteppedAgentManagerCached != null)
		{
			ShipAttachmentMachine.ShipAttachment shipAttachment = _steppedPlankBridgeSteppedAgentManagerCached?.NavmeshHolder?.CurrentAttachment;
			if (shipAttachment != null)
			{
				return shipAttachment.AttachmentSource.OwnerShip.ShipIslandCombinedID;
			}
		}
		return 0uL;
	}

	public override void OnTickParallel(float dt)
	{
		AgentMovementMode agentMovementMode = Agent.MovementMode & AgentMovementMode.WaterDiving;
		if (agentMovementMode == AgentMovementMode.WaterSurface || agentMovementMode == AgentMovementMode.WaterDiving)
		{
			if (_lastMovementMode != AgentMovementMode.WaterSurface && _lastMovementMode != AgentMovementMode.WaterDiving && Agent.IsHuman)
			{
				Agent.SaveEquipmentsOnHand();
				if (Agent.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
				{
					Agent.Mission.AddTickActionMT(Mission.MissionTickAction.TryToSheathWeaponInHand, Agent, 0, 1);
				}
				if (Agent.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
				{
					Agent.Mission.AddTickActionMT(Mission.MissionTickAction.TryToSheathWeaponInHand, Agent, 1, 1);
				}
				if (Mission.Current.MissionResult != null)
				{
					Agent.SetAgentFlags((AgentFlag)((uint)Agent.GetAgentFlags() & 0xFFFFFFEFu));
				}
				else
				{
					Agent.SetAgentFlags((AgentFlag)((uint)Agent.GetAgentFlags() & 0xFFFFFFE7u));
				}
			}
		}
		else if (_lastMovementMode != AgentMovementMode.Land && agentMovementMode == AgentMovementMode.Land && Agent.IsHuman)
		{
			if (Mission.Current.MissionResult != null)
			{
				Agent.SetAgentFlags(Agent.GetAgentFlags() | AgentFlag.CanDefend);
			}
			else
			{
				Agent.SetAgentFlags(Agent.GetAgentFlags() | AgentFlag.CanAttack | AgentFlag.CanDefend);
			}
		}
		if (!IsJumpingOffOnCooldown)
		{
			WeakGameEntity steppedEntity = Agent.GetSteppedEntity();
			if (steppedEntity != _steppedEntityCache)
			{
				_steppedEntityCache = GameEntity.CreateFromWeakEntity(steppedEntity);
				WeakGameEntity root = steppedEntity.Root;
				SteppedShip = ((_steppedEntityCache != null) ? (root.GetFirstScriptWithNameHash(MissionShip.MissionShipScriptNameHash) as MissionShip) : null);
				_navalAgentsLogic.OnAgentSteppedShipChanged(Agent, SteppedShip);
				_steppedNavalPhysicsCached = root.GetFirstScriptOfType<NavalDLC.Missions.NavalPhysics.NavalPhysics>();
				_steppedPlankBridgeSteppedAgentManagerCached = root.GetFirstScriptOfTypeRecursive<PlankBridgeSteppedAgentManager>();
			}
		}
		_lastMovementMode = agentMovementMode;
	}

	public override void OnTick(float dt)
	{
		if (dt > 0f)
		{
			if (_lastJumpOffTime >= 0f && Mission.Current.CurrentTime - _lastJumpOffTime >= 2f)
			{
				_lastJumpOffTime = ((Agent.IsOnLand() || Agent.IsInWater()) ? (-1f) : (Mission.Current.CurrentTime - 1.9f));
			}
			if (!BlockCheckingOffShipConsideration && Agent.IsAIControlled && _lastOffShipCheckTime + 5f <= Agent.Mission.CurrentTime && _navalAgentsLogic.IsDeploymentFinished)
			{
				_lastOffShipCheckTime += 5f;
				CheckAgentOffShip();
			}
			if (!BlockDrowning && !Agent.Mission.MissionEnded && _lastBreatheCheckTime + 5f <= Agent.Mission.CurrentTime && _navalAgentsLogic.IsDeploymentFinished)
			{
				_lastBreatheCheckTime += 5f;
				CheckAgentDrown();
			}
			if (!BlockBurning && !Agent.Mission.MissionEnded && _lastBurnCheckTime + 1f <= Agent.Mission.CurrentTime && _navalAgentsLogic.IsDeploymentFinished)
			{
				_lastBurnCheckTime += 1f;
				CheckAgentBurn();
			}
			if (SteppedShip != null)
			{
				_steppedNavalPhysicsCached?.AddAgentWeightAndPositionInformation(Agent);
				_steppedPlankBridgeSteppedAgentManagerCached?.AddAgentWeightAndPositionInformation(Agent);
			}
		}
	}

	private void CheckAgentOffShip()
	{
		if (_lastMovementMode == AgentMovementMode.WaterSurface || _lastMovementMode == AgentMovementMode.WaterDiving)
		{
			if (FormationShip != null && (FormationShip.Physics.NavalSinkingState != NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating || FormationShip.BeingAbandoned))
			{
				Formation formation = _teamAINavalComponent?.GetNearestAllyShipFormation(Agent);
				if (formation != null && _navalShipsLogic.GetShip(formation, out var ship) && ship != FormationShip)
				{
					_navalAgentsLogic.TransferAgentToShip(Agent, ship);
					FormationShip = ship;
					_parentShipUniqueBitwiseID = ship.ShipUniqueBitwiseID;
				}
				else
				{
					_navalAgentsLogic.RemoveAgentFromShip(Agent, FormationShip);
					FormationShip = null;
					_parentShipUniqueBitwiseID = 0uL;
				}
			}
			else
			{
				if (FormationShip != null)
				{
					return;
				}
				Formation formation2 = _teamAINavalComponent?.GetNearestAllyShipFormation(Agent);
				if (formation2 != null && _navalShipsLogic.GetShip(formation2, out var ship2))
				{
					if (Agent.IsUsingGameObject || Agent.AIMoveToGameObjectIsEnabled())
					{
						Agent.StopUsingGameObjectMT();
					}
					_navalAgentsLogic.AddAgentToShip(Agent, ship2);
					FormationShip = ship2;
					_parentShipUniqueBitwiseID = ship2.ShipUniqueBitwiseID;
				}
			}
		}
		else
		{
			if (SteppedShip == null || SteppedShip.BeingAbandoned || (_parentShipUniqueBitwiseID & SteppedShip.ShipIslandCombinedID) != 0L)
			{
				return;
			}
			Formation formation3 = _teamAINavalComponent?.GetConnectedAllyFormation(SteppedShip.ShipUniqueBitwiseID);
			if (formation3 != null && _navalShipsLogic.GetShip(formation3, out var ship3) && _parentShipUniqueBitwiseID != ship3.ShipUniqueBitwiseID)
			{
				if (FormationShip != null)
				{
					_navalAgentsLogic.TransferAgentToShip(Agent, ship3);
				}
				else
				{
					if (Agent.IsUsingGameObject || Agent.AIMoveToGameObjectIsEnabled())
					{
						Agent.StopUsingGameObject();
					}
					_navalAgentsLogic.AddAgentToShip(Agent, ship3);
				}
				FormationShip = ship3;
				_parentShipUniqueBitwiseID = ship3.ShipUniqueBitwiseID;
			}
			else if (FormationShip != null)
			{
				_navalAgentsLogic.RemoveAgentFromShip(Agent, FormationShip);
				FormationShip = null;
				_parentShipUniqueBitwiseID = 0uL;
			}
		}
	}

	private void CheckAgentDrown()
	{
		AgentMovementMode agentMovementMode = Agent.MovementMode & AgentMovementMode.WaterDiving;
		if (agentMovementMode != AgentMovementMode.WaterDiving && (Agent.Controller != AgentControllerType.AI || agentMovementMode != AgentMovementMode.WaterSurface))
		{
			_lastBreatheTime = Agent.Mission.CurrentTime;
		}
		if (GetBreath() <= 0f)
		{
			DrownAgent();
		}
	}

	private void CheckAgentBurn()
	{
		if (Agent.IsMainAgent && SteppedShip != null && SteppedShip.FireHitPoints <= 0f)
		{
			Agent.Mission.AddTickAction(Mission.MissionTickAction.RegisterBurnBlow, Agent, 0, 0);
		}
	}

	public void DrownAgent()
	{
		Agent.Mission.AddTickAction(Mission.MissionTickAction.RegisterDrownBlow, Agent, 0, 0);
	}

	public void SetupAgentToAbandonShip()
	{
		if (!BlockFormationCleanupOnShipAdabandonment && FormationShip != null)
		{
			_navalAgentsLogic.RemoveAgentFromShip(Agent, FormationShip);
		}
		SteppedShip = null;
		_lastJumpOffTime = Mission.Current.CurrentTime;
	}

	private void OnDeploymentFinished()
	{
		_lastBreatheCheckTime = Agent.Mission.CurrentTime + MBRandom.RandomFloat * 5f;
		_lastOffShipCheckTime = Agent.Mission.CurrentTime + MBRandom.RandomFloat * 5f;
		_lastBurnCheckTime = Agent.Mission.CurrentTime + MBRandom.RandomFloat * 1f;
	}
}
