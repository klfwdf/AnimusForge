using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects;

public class GenericMissionEventBox : VolumeBox
{
	public string ActivatorAgentTags;

	private List<GenericMissionEventScript> _genericMissionEvents = new List<GenericMissionEventScript>();

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(TickRequirement.Tick);
		foreach (ScriptComponentBehavior scriptComponent in base.GameEntity.GetScriptComponents())
		{
			if (scriptComponent is GenericMissionEventScript item)
			{
				_genericMissionEvents.Add(item);
			}
		}
	}

	protected override void OnTick(float dt)
	{
		bool flag = true;
		foreach (GenericMissionEventScript genericMissionEvent in _genericMissionEvents)
		{
			if (!genericMissionEvent.IsDisabled)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return;
		}
		bool flag2 = false;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.AgentVisuals.IsValid() && agent.AgentVisuals.GetEntity().Tags.Any((string x) => !string.IsNullOrEmpty(x) && ActivatorAgentTags.Contains(x)) && IsPointIn(agent.Position))
			{
				flag2 = true;
				break;
			}
		}
		if (!flag2)
		{
			return;
		}
		foreach (GenericMissionEventScript genericMissionEvent2 in _genericMissionEvents)
		{
			if (!genericMissionEvent2.IsDisabled)
			{
				Game.Current.EventManager.TriggerEvent(new GenericMissionEvent(genericMissionEvent2.EventId, genericMissionEvent2.Parameter));
			}
		}
	}
}
