using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace SandBox.Missions
{
	// Token: 0x0200005F RID: 95
	public class SabotageMissionController : MissionLogic
	{
		// Token: 0x060003AE RID: 942 RVA: 0x0001596D File Offset: 0x00013B6D
		public SabotageMissionController()
		{
			Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x060003AF RID: 943 RVA: 0x0001599B File Offset: 0x00013B9B
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x000159B8 File Offset: 0x00013BB8
		private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
		{
			if (missionEvent.EventId == "sabotage_objective_used_event")
			{
				string[] array = missionEvent.Parameter.Split(new char[] { ' ' });
				SandBoxHelpers.MissionHelper.DisableGenericMissionEventScript(array[0], missionEvent);
				EventTriggeringUsableMachine firstScriptOfType = Mission.Current.Scene.FindEntityWithTag(array[0]).GetFirstScriptOfType<EventTriggeringUsableMachine>();
				for (int i = 0; i < firstScriptOfType.StandingPoints.Count; i++)
				{
					if (firstScriptOfType.StandingPoints[i].HasUser)
					{
						firstScriptOfType.StandingPoints[i].UserAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
				}
				this.OnSabotageObjectiveUsed(firstScriptOfType, array[1]);
			}
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00015A60 File Offset: 0x00013C60
		public override void AfterStart()
		{
			Mission.Current.SetMissionMode(MissionMode.Stealth, true);
			SandBoxHelpers.MissionHelper.SpawnPlayer(false, true, false, false, "");
			Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters(null);
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sabotage_objective"))
			{
				EventTriggeringUsableMachine firstScriptOfType = gameEntity.GetFirstScriptOfType<EventTriggeringUsableMachine>();
				this._sabotageObjectives.Add(firstScriptOfType);
			}
			this._allSabotageObjectivesCount = this._sabotageObjectives.Count;
			this._missionExitBarrier = Mission.Current.Scene.FindEntityWithTag("sabotage_mission_exit_barrier");
			this._missionExitBarrier.SetVisibilityExcludeParents(false);
			this._missionExitArea = Mission.Current.Scene.FindEntityWithTag("sabotage_mission_exit_area").GetFirstScriptOfType<BasicAreaIndicator>();
			this._missionExitArea.SetIsActive(false);
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x00015B54 File Offset: 0x00013D54
		private void OnSabotageObjectiveUsed(EventTriggeringUsableMachine eventTriggeringUsableMachine, string eventDescriptionTextId)
		{
			MBInformationManager.AddQuickInformation(GameTexts.FindText(eventDescriptionTextId, null), 0, null, null, "");
			eventTriggeringUsableMachine.SetDisabled(true);
			this._usedSabotageObjectivesCount++;
			if (this._usedSabotageObjectivesCount >= this._allSabotageObjectivesCount)
			{
				this._missionExitBarrier.SetVisibilityExcludeParents(true);
				this._missionExitArea.SetIsActive(true);
			}
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x00015BB0 File Offset: 0x00013DB0
		public override void OnMissionTick(float dt)
		{
		}

		// Token: 0x040001F2 RID: 498
		private const string SabotageObjectiveTag = "sabotage_objective";

		// Token: 0x040001F3 RID: 499
		private const string SabotageMissionExitBarrierTag = "sabotage_mission_exit_barrier";

		// Token: 0x040001F4 RID: 500
		private const string SabotageMissionExitAreaTag = "sabotage_mission_exit_area";

		// Token: 0x040001F5 RID: 501
		private const string SabotageObjectiveUsedEventId = "sabotage_objective_used_event";

		// Token: 0x040001F6 RID: 502
		private readonly List<EventTriggeringUsableMachine> _sabotageObjectives = new List<EventTriggeringUsableMachine>();

		// Token: 0x040001F7 RID: 503
		private GameEntity _missionExitBarrier;

		// Token: 0x040001F8 RID: 504
		private BasicAreaIndicator _missionExitArea;

		// Token: 0x040001F9 RID: 505
		private int _allSabotageObjectivesCount;

		// Token: 0x040001FA RID: 506
		private int _usedSabotageObjectivesCount;
	}
}
