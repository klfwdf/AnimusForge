using System;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A4 RID: 164
	public class FightBehavior : AgentBehavior
	{
		// Token: 0x060006EE RID: 1774 RVA: 0x0002F5EB File Offset: 0x0002D7EB
		public FightBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			if (base.OwnerAgent.HumanAIComponent == null)
			{
				base.OwnerAgent.AddComponent(new HumanAIComponent(base.OwnerAgent));
			}
		}

		// Token: 0x060006EF RID: 1775 RVA: 0x0002F617 File Offset: 0x0002D817
		public override float GetAvailability(bool isSimulation)
		{
			if (!MissionFightHandler.IsAgentAggressive(base.OwnerAgent))
			{
				return 0.1f;
			}
			return 1f;
		}

		// Token: 0x060006F0 RID: 1776 RVA: 0x0002F634 File Offset: 0x0002D834
		protected override void OnActivate()
		{
			TextObject textObject = new TextObject("{=!}{p0} {p1} activate alarmed behavior group.", null);
			textObject.SetTextVariable("p0", base.OwnerAgent.Name.ToString());
			textObject.SetTextVariable("p1", base.OwnerAgent.Index.ToString());
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x0002F688 File Offset: 0x0002D888
		protected override void OnDeactivate()
		{
			TextObject textObject = new TextObject("{=!}{p0} {p1} deactivate fight behavior.", null);
			textObject.SetTextVariable("p0", base.OwnerAgent.Name.ToString());
			textObject.SetTextVariable("p1", base.OwnerAgent.Index.ToString());
		}

		// Token: 0x060006F2 RID: 1778 RVA: 0x0002F6DA File Offset: 0x0002D8DA
		public override string GetDebugInfo()
		{
			return "Fight";
		}
	}
}
