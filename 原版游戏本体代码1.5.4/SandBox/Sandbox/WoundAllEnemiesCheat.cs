using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace SandBox;

public class WoundAllEnemiesCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		KillAllEnemies();
	}

	private void KillAllEnemies()
	{
		AgentReadOnlyList agentReadOnlyList = Mission.Current?.Agents;
		Agent agent = Mission.Current?.MainAgent;
		Team team = Mission.Current?.PlayerTeam;
		if (agentReadOnlyList == null || team == null)
		{
			return;
		}
		for (int num = agentReadOnlyList.Count - 1; num >= 0; num--)
		{
			Agent agent2 = agentReadOnlyList[num];
			if (agent2 != agent && agent2.GetAgentFlags().HasAnyFlag(AgentFlag.CanAttack) && team != null && agent2.Team != null && agent2.Team.IsValid && team.IsEnemyOf(agent2.Team))
			{
				KillAgent(agent2);
			}
		}
	}

	private void KillAgent(Agent agent)
	{
		Agent agent2 = Mission.Current?.MainAgent ?? agent;
		Blow blow = new Blow(agent2.Index);
		blow.DamageType = DamageTypes.Blunt;
		blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
		blow.GlobalPosition = agent.Position;
		blow.GlobalPosition.z += agent.GetEyeGlobalHeight();
		blow.BaseMagnitude = 2000f;
		blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
		blow.InflictedDamage = 2000;
		blow.SwingDirection = agent.LookDirection;
		blow.Direction = blow.SwingDirection;
		blow.DamageCalculated = true;
		sbyte mainHandItemBoneIndex = agent2.Monster.MainHandItemBoneIndex;
		AttackCollisionData collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(_attackBlockedWithShield: false, _correctSideShieldBlock: false, _isAlternativeAttack: false, _isColliderAgent: true, _collidedWithShieldOnBack: false, _isMissile: false, _isMissileBlockedWithWeapon: false, _missileHasPhysics: false, _entityExists: false, _thrustTipHit: false, _missileGoneUnderWater: false, _missileGoneOutOfBorder: false, CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
		agent.RegisterBlow(blow, in collisionData);
	}

	public override TextObject GetName()
	{
		return new TextObject("{=FJ93PXVa}Wound All Enemies");
	}
}
