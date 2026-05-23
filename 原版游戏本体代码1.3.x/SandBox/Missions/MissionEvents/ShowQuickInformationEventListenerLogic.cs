using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Missions.MissionEvents
{
	// Token: 0x0200009B RID: 155
	public class ShowQuickInformationEventListenerLogic : MissionLogic
	{
		// Token: 0x06000674 RID: 1652 RVA: 0x0002C375 File Offset: 0x0002A575
		public ShowQuickInformationEventListenerLogic()
		{
			Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x06000675 RID: 1653 RVA: 0x0002C398 File Offset: 0x0002A598
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x0002C3B8 File Offset: 0x0002A5B8
		private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
		{
			if (missionEvent.EventId == "show_quick_information_event")
			{
				string[] array = missionEvent.Parameter.Split(new char[] { ' ' });
				SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array[0], missionEvent);
				MBInformationManager.AddQuickInformation(GameTexts.FindText(array[1], null), 0, null, null, "");
			}
		}

		// Token: 0x0400037B RID: 891
		private const string ShowQuickInformationEventId = "show_quick_information_event";
	}
}
