using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace SandBox
{
	// Token: 0x0200001A RID: 26
	public class WoundAllEnemiesCheat : GameplayCheatItem
	{
		// Token: 0x0600004E RID: 78 RVA: 0x00003F6F File Offset: 0x0000216F
		public override void ExecuteCheat()
		{
			this.KillAllEnemies();
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003F78 File Offset: 0x00002178
		private void KillAllEnemies()
		{
			Mission mission = Mission.Current;
			AgentReadOnlyList agentReadOnlyList = ((mission != null) ? mission.Agents : null);
			Mission mission2 = Mission.Current;
			Agent agent = ((mission2 != null) ? mission2.MainAgent : null);
			Mission mission3 = Mission.Current;
			Team team = ((mission3 != null) ? mission3.PlayerTeam : null);
			if (agentReadOnlyList == null || team == null)
			{
				return;
			}
			for (int i = agentReadOnlyList.Count - 1; i >= 0; i--)
			{
				Agent agent2 = agentReadOnlyList[i];
				if (agent2 != agent && agent2.GetAgentFlags().HasAnyFlag(AgentFlag.CanAttack) && team != null && agent2.Team != null && agent2.Team.IsValid && team.IsEnemyOf(agent2.Team))
				{
					this.KillAgent(agent2);
				}
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00004024 File Offset: 0x00002224
		private void KillAgent(Agent agent)
		{
			Mission mission = Mission.Current;
			Agent agent2 = ((mission != null) ? mission.MainAgent : null) ?? agent;
			Blow blow = new Blow(agent2.Index);
			blow.DamageType = DamageTypes.Blunt;
			blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
			blow.GlobalPosition = agent.Position;
			blow.GlobalPosition.z = blow.GlobalPosition.z + agent.GetEyeGlobalHeight();
			blow.BaseMagnitude = 2000f;
			blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
			blow.InflictedDamage = 2000;
			blow.SwingDirection = agent.LookDirection;
			blow.Direction = blow.SwingDirection;
			blow.DamageCalculated = true;
			sbyte mainHandItemBoneIndex = agent2.Monster.MainHandItemBoneIndex;
			AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
			agent.RegisterBlow(blow, attackCollisionDataForDebugPurpose);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004160 File Offset: 0x00002360
		public override TextObject GetName()
		{
			return new TextObject("{=FJ93PXVa}Wound All Enemies", null);
		}
	}
}
