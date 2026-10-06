using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class EventTriggeringUsableMachine : UsableMachine
{
	public string ActivatorAgentTags;

	public string ActionTextId;

	public string DescriptionTextId;

	private List<GenericMissionEventScript> _genericMissionEvents = new List<GenericMissionEventScript>();

	public TextObject ActionText => GameTexts.FindText(ActionTextId);

	public TextObject DescriptionText => GameTexts.FindText(DescriptionTextId);

	protected internal override void OnInit()
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

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			if (!base.StandingPoints[i].HasUser)
			{
				continue;
			}
			foreach (GenericMissionEventScript genericMissionEvent in _genericMissionEvents)
			{
				if (!genericMissionEvent.IsDisabled)
				{
					Game.Current.EventManager.TriggerEvent(new GenericMissionEvent(genericMissionEvent.EventId, genericMissionEvent.Parameter));
				}
			}
		}
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = GameTexts.FindText("str_key_action");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		textObject.SetTextVariable("ACTION", ActionText);
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return DescriptionText;
	}
}
