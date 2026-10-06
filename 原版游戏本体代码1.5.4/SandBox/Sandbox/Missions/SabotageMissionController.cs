using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace SandBox.Missions;

public class SabotageMissionController : MissionLogic
{
	private const string SabotageObjectiveTag = "sabotage_objective";

	private const string SabotageMissionExitBarrierTag = "sabotage_mission_exit_barrier";

	private const string SabotageMissionExitAreaTag = "sabotage_mission_exit_area";

	private const string SabotageObjectiveUsedEventId = "sabotage_objective_used_event";

	private readonly List<EventTriggeringUsableMachine> _sabotageObjectives = new List<EventTriggeringUsableMachine>();

	private GameEntity _missionExitBarrier;

	private BasicAreaIndicator _missionExitArea;

	private int _allSabotageObjectivesCount;

	private int _usedSabotageObjectivesCount;

	public SabotageMissionController()
	{
		Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
	{
		if (!(missionEvent.EventId == "sabotage_objective_used_event"))
		{
			return;
		}
		string[] array = missionEvent.Parameter.Split(new char[1] { ' ' });
		SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array[0], missionEvent);
		EventTriggeringUsableMachine firstScriptOfType = Mission.Current.Scene.FindEntityWithTag(array[0]).GetFirstScriptOfType<EventTriggeringUsableMachine>();
		for (int i = 0; i < firstScriptOfType.StandingPoints.Count; i++)
		{
			if (firstScriptOfType.StandingPoints[i].HasUser)
			{
				firstScriptOfType.StandingPoints[i].UserAgent.StopUsingGameObject();
			}
		}
		OnSabotageObjectiveUsed(firstScriptOfType, array[1]);
	}

	public override void AfterStart()
	{
		Mission.Current.SetMissionMode(MissionMode.Stealth, atStart: true);
		SandBoxHelpers.MissionHelper.SpawnPlayer(civilianEquipment: false, noHorses: true);
		Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters();
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sabotage_objective"))
		{
			EventTriggeringUsableMachine firstScriptOfType = item.GetFirstScriptOfType<EventTriggeringUsableMachine>();
			_sabotageObjectives.Add(firstScriptOfType);
		}
		_allSabotageObjectivesCount = _sabotageObjectives.Count;
		_missionExitBarrier = Mission.Current.Scene.FindEntityWithTag("sabotage_mission_exit_barrier");
		_missionExitBarrier.SetVisibilityExcludeParents(visible: false);
		_missionExitArea = Mission.Current.Scene.FindEntityWithTag("sabotage_mission_exit_area").GetFirstScriptOfType<BasicAreaIndicator>();
		_missionExitArea.SetIsActive(isActive: false);
	}

	private void OnSabotageObjectiveUsed(EventTriggeringUsableMachine eventTriggeringUsableMachine, string eventDescriptionTextId)
	{
		MBInformationManager.AddQuickInformation(GameTexts.FindText(eventDescriptionTextId));
		eventTriggeringUsableMachine.SetDisabled(isParentObject: true);
		_usedSabotageObjectivesCount++;
		if (_usedSabotageObjectivesCount >= _allSabotageObjectivesCount)
		{
			_missionExitBarrier.SetVisibilityExcludeParents(visible: true);
			_missionExitArea.SetIsActive(isActive: true);
		}
	}

	public override void OnMissionTick(float dt)
	{
	}
}
