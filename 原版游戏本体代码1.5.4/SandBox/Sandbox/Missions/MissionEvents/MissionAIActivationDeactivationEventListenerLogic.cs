using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Missions.MissionEvents;

public class MissionAIActivationDeactivationEventListenerLogic : MissionLogic
{
	public const string ActivationEventId = "activate_agent_ai";

	public const string DeactivationEventId = "deactivate_agent_ai";

	public MissionAIActivationDeactivationEventListenerLogic()
	{
		Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
	{
		if (missionEvent.EventId == "activate_agent_ai")
		{
			string[] array = missionEvent.Parameter.Split(new char[1] { ' ' });
			SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array[0], missionEvent);
			string[] activationTags = new string[array.Length - 1];
			Array.Copy(array, 1, activationTags, 0, activationTags.Length);
			{
				foreach (Agent agent in Mission.Current.Agents)
				{
					if (agent.AgentVisuals.IsValid() && agent.AgentVisuals.GetEntity().Tags.AnyQ((string x) => activationTags.ContainsQ(x)))
					{
						CheckRemoveScriptedBehaviorFromAgent(agent);
					}
				}
				return;
			}
		}
		if (!(missionEvent.EventId == "deactivate_agent_ai"))
		{
			return;
		}
		string[] array2 = missionEvent.Parameter.Split(new char[1] { ' ' });
		SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array2[0], missionEvent);
		string[] deactivationTags = new string[array2.Length - 1];
		Array.Copy(array2, 1, deactivationTags, 0, deactivationTags.Length);
		foreach (Agent agent2 in Mission.Current.Agents)
		{
			if (agent2.AgentVisuals.IsValid() && agent2.AgentVisuals.GetEntity().Tags.AnyQ((string x) => deactivationTags.ContainsQ(x)))
			{
				CheckAddScriptedBehaviorToAgent(agent2);
			}
		}
	}

	private void CheckRemoveScriptedBehaviorFromAgent(Agent agent)
	{
		DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		if (behaviorGroup.HasBehavior<IdleAgentBehavior>())
		{
			behaviorGroup.RemoveBehavior<IdleAgentBehavior>();
		}
	}

	private void CheckAddScriptedBehaviorToAgent(Agent agent)
	{
		DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		if (!behaviorGroup.HasBehavior<IdleAgentBehavior>())
		{
			behaviorGroup.AddBehavior<IdleAgentBehavior>();
		}
		behaviorGroup.SetScriptedBehavior<IdleAgentBehavior>();
	}
}
