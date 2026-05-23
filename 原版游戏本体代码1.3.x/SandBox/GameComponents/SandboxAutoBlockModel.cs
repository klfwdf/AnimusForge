using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents
{
	// Token: 0x020000BF RID: 191
	public class SandboxAutoBlockModel : AutoBlockModel
	{
		// Token: 0x060007EF RID: 2031 RVA: 0x000389F4 File Offset: 0x00036BF4
		public override Agent.UsageDirection GetBlockDirection(Mission mission)
		{
			Agent mainAgent = mission.MainAgent;
			float num = float.MinValue;
			Agent.UsageDirection usageDirection = Agent.UsageDirection.AttackDown;
			foreach (Agent agent in mission.Agents)
			{
				if (agent.IsHuman)
				{
					Agent.ActionStage currentActionStage = agent.GetCurrentActionStage(1);
					if ((currentActionStage == Agent.ActionStage.AttackReady || currentActionStage == Agent.ActionStage.AttackQuickReady || currentActionStage == Agent.ActionStage.AttackRelease) && agent.IsEnemyOf(mainAgent))
					{
						Vec3 v = agent.Position - mainAgent.Position;
						float num2 = v.Normalize();
						float num3 = MBMath.ClampFloat(Vec3.DotProduct(v, mainAgent.LookDirection) + 0.8f, 0f, 1f);
						float num4 = MBMath.ClampFloat(1f / (num2 + 0.5f), 0f, 1f);
						float num5 = MBMath.ClampFloat(-Vec3.DotProduct(v, agent.LookDirection) + 0.5f, 0f, 1f);
						float num6 = num3 * num4 * num5;
						if (num6 > num)
						{
							num = num6;
							usageDirection = agent.GetCurrentActionDirection(1);
							if (usageDirection == Agent.UsageDirection.None)
							{
								usageDirection = Agent.UsageDirection.AttackDown;
							}
						}
					}
				}
			}
			return usageDirection;
		}
	}
}
