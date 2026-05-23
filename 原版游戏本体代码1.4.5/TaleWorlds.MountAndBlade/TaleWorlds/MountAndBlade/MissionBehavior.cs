using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionBehavior : IMissionBehavior
{
	public Mission Mission { get; internal set; }

	public IInputContext DebugInput => Input.DebugInput;

	public abstract MissionBehaviorType BehaviorType { get; }

	public virtual void OnAfterMissionCreated()
	{
	}

	public virtual void OnBehaviorInitialize()
	{
	}

	public virtual void OnCreated()
	{
	}

	public virtual void EarlyStart()
	{
	}

	public virtual void AfterStart()
	{
	}

	public virtual void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
	}

	public virtual void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
	}

	public virtual void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex)
	{
	}

	public virtual void OnMissionScreenPreLoad()
	{
	}

	public virtual void OnAgentCreated(Agent agent)
	{
	}

	public virtual void OnAgentBuild(Agent agent, Banner banner)
	{
	}

	public virtual void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
	}

	public virtual void OnAgentControllerSetToPlayer(Agent agent)
	{
	}

	public virtual void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
	}

	public virtual void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
	}

	public virtual void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
	}

	public virtual void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
	}

	public virtual void OnAgentDeleted(Agent affectedAgent)
	{
	}

	public virtual void OnAgentFleeing(Agent affectedAgent)
	{
	}

	public virtual void OnAgentPanicked(Agent affectedAgent)
	{
	}

	public virtual void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
	{
	}

	public virtual void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
	}

	public virtual void OnAddTeam(Team team)
	{
	}

	public virtual void AfterAddTeam(Team team)
	{
	}

	public virtual void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
	}

	public virtual void OnClearScene()
	{
	}

	public virtual void OnEndMissionInternal()
	{
		OnEndMission();
	}

	protected virtual void OnEndMission()
	{
	}

	public virtual void OnRemoveBehavior()
	{
	}

	public virtual void OnFixedMissionTick(float fixedDt)
	{
	}

	public virtual void OnPreMissionTick(float dt)
	{
	}

	public virtual void OnPreDisplayMissionTick(float dt)
	{
	}

	public virtual void OnMissionTick(float dt)
	{
	}

	public virtual void OnAgentMount(Agent agent)
	{
	}

	public virtual void OnAgentDismount(Agent agent)
	{
	}

	public virtual bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		return false;
	}

	public virtual void OnEntityRemoved(GameEntity entity)
	{
	}

	public virtual void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
	}

	public virtual void OnObjectStoppedBeingUsed(Agent userAgent, UsableMissionObject usedObject)
	{
	}

	public virtual void OnRenderingStarted()
	{
	}

	public virtual void OnMissionStateActivated()
	{
	}

	public virtual void OnMissionStateFinalized()
	{
	}

	public virtual void OnMissionStateDeactivated()
	{
	}

	public virtual List<CompassItemUpdateParams> GetCompassTargets()
	{
		return null;
	}

	public virtual void OnAssignPlayerAsSergeantOfFormation(Agent agent)
	{
	}

	public virtual void OnDeploymentFinished()
	{
	}

	public virtual void OnAfterDeploymentFinished()
	{
	}

	public virtual void OnTeamDeployed(Team team)
	{
	}

	public virtual void OnBattleSideDeployed(BattleSideEnum side)
	{
	}

	protected internal virtual void OnGetAgentState(Agent agent, bool usedSurgery)
	{
	}

	public virtual void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
	{
	}

	protected internal virtual void OnObjectDisabled(DestructableComponent destructionComponent)
	{
	}

	public virtual void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
	{
	}

	protected internal virtual void OnAgentControllerChanged(Agent agent, AgentControllerType oldController)
	{
	}

	public virtual void OnRegisterBlow(Agent attacker, Agent victim, WeakGameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
	{
	}

	public virtual void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
	{
	}

	public virtual void OnMissileRemoved(int MissileIndex)
	{
	}

	public virtual void OnTutorialCompleted(string completedTutorialIdentifier)
	{
	}
}
