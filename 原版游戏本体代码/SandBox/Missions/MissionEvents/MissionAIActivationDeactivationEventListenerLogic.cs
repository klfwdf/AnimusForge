using System;
using System.Collections.Generic;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Missions.MissionEvents
{
	// Token: 0x02000099 RID: 153
	public class MissionAIActivationDeactivationEventListenerLogic : MissionLogic
	{
		// Token: 0x06000667 RID: 1639 RVA: 0x0002BE21 File Offset: 0x0002A021
		public MissionAIActivationDeactivationEventListenerLogic()
		{
			Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x06000668 RID: 1640 RVA: 0x0002BE44 File Offset: 0x0002A044
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x06000669 RID: 1641 RVA: 0x0002BE64 File Offset: 0x0002A064
		private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
		{
			if (missionEvent.EventId == "activate_agent_ai")
			{
				string[] array = missionEvent.Parameter.Split(new char[] { ' ' });
				SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array[0], missionEvent);
				string[] activationTags = new string[array.Length - 1];
				Array.Copy(array, 1, activationTags, 0, activationTags.Length);
				using (List<Agent>.Enumerator enumerator = Mission.Current.Agents.GetEnumerator())
				{
					Func<string, bool> <>9__0;
					while (enumerator.MoveNext())
					{
						Agent agent = enumerator.Current;
						if (agent.AgentVisuals.IsValid())
						{
							string[] tags = agent.AgentVisuals.GetEntity().Tags;
							Func<string, bool> predicate;
							if ((predicate = <>9__0) == null)
							{
								predicate = (<>9__0 = (string x) => activationTags.ContainsQ(x));
							}
							if (tags.AnyQ(predicate))
							{
								this.CheckRemoveScriptedBehaviorFromAgent(agent);
							}
						}
					}
					return;
				}
			}
			if (missionEvent.EventId == "deactivate_agent_ai")
			{
				string[] array2 = missionEvent.Parameter.Split(new char[] { ' ' });
				SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array2[0], missionEvent);
				string[] deactivationTags = new string[array2.Length - 1];
				Array.Copy(array2, 1, deactivationTags, 0, deactivationTags.Length);
				Func<string, bool> <>9__1;
				foreach (Agent agent2 in Mission.Current.Agents)
				{
					if (agent2.AgentVisuals.IsValid())
					{
						string[] tags2 = agent2.AgentVisuals.GetEntity().Tags;
						Func<string, bool> predicate2;
						if ((predicate2 = <>9__1) == null)
						{
							predicate2 = (<>9__1 = (string x) => deactivationTags.ContainsQ(x));
						}
						if (tags2.AnyQ(predicate2))
						{
							this.CheckAddScriptedBehaviorToAgent(agent2);
						}
					}
				}
			}
		}

		// Token: 0x0600066A RID: 1642 RVA: 0x0002C060 File Offset: 0x0002A260
		private void CheckRemoveScriptedBehaviorFromAgent(Agent agent)
		{
			DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup.HasBehavior<IdleAgentBehavior>())
			{
				behaviorGroup.RemoveBehavior<IdleAgentBehavior>();
			}
		}

		// Token: 0x0600066B RID: 1643 RVA: 0x0002C08C File Offset: 0x0002A28C
		private void CheckAddScriptedBehaviorToAgent(Agent agent)
		{
			DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (!behaviorGroup.HasBehavior<IdleAgentBehavior>())
			{
				behaviorGroup.AddBehavior<IdleAgentBehavior>();
			}
			behaviorGroup.SetScriptedBehavior<IdleAgentBehavior>();
		}

		// Token: 0x04000377 RID: 887
		public const string ActivationEventId = "activate_agent_ai";

		// Token: 0x04000378 RID: 888
		public const string DeactivationEventId = "deactivate_agent_ai";
	}
}
