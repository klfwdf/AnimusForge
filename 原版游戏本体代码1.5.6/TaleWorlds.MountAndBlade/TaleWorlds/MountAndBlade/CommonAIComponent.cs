using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CommonAIComponent : AgentComponent
{
	public const float MoraleThresholdForPanicking = 0.01f;

	private const float MaxRecoverableMoraleMultiplier = 0.5f;

	private const float MoraleRecoveryPerSecond = 0.4f;

	private float _recoveryMorale;

	private float _initialMorale;

	private float _morale = 50f;

	private bool _panicTriggered;

	private readonly Timer _fadeOutTimer;

	private float _retreatDistanceSquared;

	public bool IsPanicked { get; private set; }

	public bool IsRetreating { get; private set; }

	public int ReservedRiderAgentIndex { get; private set; }

	public float InitialMorale => _initialMorale;

	public float RecoveryMorale => _recoveryMorale;

	public float Morale
	{
		get
		{
			return _morale;
		}
		set
		{
			_morale = MBMath.ClampFloat(value, 0f, 100f);
		}
	}

	public CommonAIComponent(Agent agent)
		: base(agent)
	{
		_fadeOutTimer = new Timer(Mission.Current.CurrentTime, 0.5f + MBRandom.RandomFloat * 0.1f);
		float num = agent.Monster.BodyCapsuleRadius * 2f * 7.5f;
		_retreatDistanceSquared = num * num;
		ReservedRiderAgentIndex = -1;
	}

	public override void Initialize()
	{
		base.Initialize();
		InitializeMorale();
	}

	private void InitializeMorale()
	{
		int num = MBRandom.RandomInt(30);
		float num2 = Agent.Components.Sum((AgentComponent c) => c.GetMoraleAddition());
		float baseMorale = 35f + (float)num + num2;
		baseMorale = MissionGameModels.Current.BattleMoraleModel.GetEffectiveInitialMorale(Agent, baseMorale);
		baseMorale = MBMath.ClampFloat(baseMorale, 15f, 100f);
		_initialMorale = baseMorale;
		_recoveryMorale = _initialMorale * 0.5f;
		Morale = _initialMorale;
	}

	public override void OnTickParallel(float dt)
	{
		if (!Agent.Mission.AllowAiTicking || !Agent.IsAIControlled)
		{
			return;
		}
		if (!IsRetreating && _morale < 0.01f)
		{
			if (CanPanic())
			{
				_panicTriggered = true;
			}
			else
			{
				Morale = 0.01f;
			}
		}
		if (!IsPanicked && !_panicTriggered && _morale < _recoveryMorale)
		{
			Morale = Math.Min(_morale + 0.4f * dt, _recoveryMorale);
		}
		if (_fadeOutTimer.Check(Mission.Current.CurrentTime) && Mission.Current.CanAgentRout(Agent) && !Agent.IsFadingOut())
		{
			Vec3 position = Agent.Position;
			WorldPosition retreatPos = Agent.GetRetreatPos();
			_retreatDistanceSquared = _fadeOutTimer.Duration * Agent.Velocity.AsVec2.LengthSquared + 2f * Agent.Monster.BodyCapsuleRadius;
			if ((retreatPos.AsVec2.IsValid && retreatPos.AsVec2.DistanceSquared(position.AsVec2) < _retreatDistanceSquared && retreatPos.GetGroundVec3MT().DistanceSquared(position) < _retreatDistanceSquared) || !Agent.Mission.IsPositionInsideBoundaries(position.AsVec2) || position.DistanceSquared(Agent.Mission.GetClosestBoundaryPosition(position.AsVec2).ToVec3()) < _retreatDistanceSquared)
			{
				Agent.StartFadingOut();
			}
		}
		if (IsPanicked && Agent.Mission.MissionEnded)
		{
			MissionResult missionResult = Agent.Mission.MissionResult;
			if (Agent.Team != null && missionResult != null && ((missionResult.PlayerVictory && (Agent.Team.IsPlayerTeam || Agent.Team.IsPlayerAlly)) || (missionResult.PlayerDefeated && !Agent.Team.IsPlayerTeam && !Agent.Team.IsPlayerAlly)) && Agent != Agent.Main && Agent.IsActive())
			{
				StopRetreating();
			}
		}
	}

	public override void OnTick(float dt)
	{
		if (_panicTriggered)
		{
			_panicTriggered = false;
			Panic();
		}
	}

	public void Panic()
	{
		Agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		if (!IsPanicked)
		{
			IsPanicked = true;
			Agent.Mission.OnAgentPanicked(Agent);
		}
	}

	public void Retreat(bool useCachingSystem = false)
	{
		if (IsRetreating)
		{
			return;
		}
		IsRetreating = true;
		Agent.EnforceShieldUsage(Agent.UsageDirection.None);
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (useCachingSystem)
		{
			worldPosition = Agent.Formation.RetreatPositionCache.GetRetreatPositionFromCache(Agent.Position.AsVec2);
		}
		if (!worldPosition.IsValid)
		{
			worldPosition = Agent.Mission.GetClosestFleePositionForAgent(Agent);
			if (useCachingSystem)
			{
				Agent.Formation.RetreatPositionCache.AddNewPositionToCache(Agent.Position.AsVec2, worldPosition);
			}
		}
		Agent.Retreat(worldPosition);
	}

	public void StopRetreating()
	{
		if (IsRetreating)
		{
			IsRetreating = false;
			IsPanicked = false;
			float morale = TaleWorlds.Library.MathF.Max(0.02f, Morale);
			Agent.SetMorale(morale);
			Agent.StopRetreating();
		}
	}

	public bool CanPanic()
	{
		if (!MissionGameModels.Current.BattleMoraleModel.CanPanicDueToMorale(Agent))
		{
			return false;
		}
		if (Mission.Current.IsSiegeBattle && Agent.Team.Side == BattleSideEnum.Attacker && Agent.Team.TeamAI is TeamAISiegeComponent teamAISiegeComponent)
		{
			int currentNavigationFaceId = Agent.GetCurrentNavigationFaceId();
			if (currentNavigationFaceId % 10 == 1)
			{
				return false;
			}
			if (teamAISiegeComponent.IsPrimarySiegeWeaponNavmeshFaceId(currentNavigationFaceId))
			{
				return false;
			}
		}
		return true;
	}

	public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon, in Blow b, in AttackCollisionData collisionData)
	{
		base.OnHit(affectorAgent, damage, in affectorWeapon, in b, in collisionData);
		if (damage >= 1 && Agent.IsMount && Agent.IsAIControlled && Agent.RiderAgent == null)
		{
			Panic();
		}
	}

	public override void OnAgentRemoved()
	{
		base.OnAgentRemoved();
		if (Agent.IsMount && Agent.RiderAgent == null)
		{
			FindReservingAgent()?.HumanAIComponent.UnreserveMount(Agent);
		}
	}

	public override void OnComponentRemoved()
	{
		base.OnComponentRemoved();
		if (Agent.IsMount && Agent.RiderAgent == null)
		{
			FindReservingAgent()?.HumanAIComponent.UnreserveMount(Agent);
		}
	}

	internal void OnMountReserved(int riderAgentIndex)
	{
		ReservedRiderAgentIndex = riderAgentIndex;
	}

	internal void OnMountUnreserved()
	{
		ReservedRiderAgentIndex = -1;
	}

	private Agent FindReservingAgent()
	{
		Agent result = null;
		if (ReservedRiderAgentIndex >= 0)
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent.Index == ReservedRiderAgentIndex)
				{
					result = agent;
					break;
				}
			}
		}
		return result;
	}
}
