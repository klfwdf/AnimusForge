using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Missions.MissionEvents;

public class ShowQuickInformationEventListenerLogic : MissionLogic
{
	private const string ShowQuickInformationEventId = "show_quick_information_event";

	public ShowQuickInformationEventListenerLogic()
	{
		Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
	{
		if (missionEvent.EventId == "show_quick_information_event")
		{
			string[] array = missionEvent.Parameter.Split(new char[1] { ' ' });
			SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array[0], missionEvent);
			MBInformationManager.AddQuickInformation(GameTexts.FindText(array[1]));
		}
	}
}
