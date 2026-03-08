using System;
using SandBox.Missions.MissionLogics;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x02000021 RID: 33
	[OverrideView(typeof(MissionQuestBarView))]
	public class MissionGauntletQuestBarView : MissionQuestBarView
	{
		// Token: 0x060001CA RID: 458 RVA: 0x0000BAC8 File Offset: 0x00009CC8
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._dataSource = new MissionQuestBarVM();
			this._gauntletLayer = new GauntletLayer("MissionQuestBar", 10, false);
			this._gauntletLayer.LoadMovie("MissionQuestBar", this._dataSource);
			base.MissionScreen.AddLayer(this._gauntletLayer);
			foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
			{
				if (missionBehavior is IMissionProgressTracker)
				{
					this._missionProgressTracker = missionBehavior as IMissionProgressTracker;
					break;
				}
			}
		}

		// Token: 0x060001CB RID: 459 RVA: 0x0000BB7C File Offset: 0x00009D7C
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			this._dataSource.OnFinalize();
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer = null;
			this._dataSource = null;
		}

		// Token: 0x060001CC RID: 460 RVA: 0x0000BBAE File Offset: 0x00009DAE
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (this._missionProgressTracker != null)
			{
				this._dataSource.UpdateQuestValues(0f, 1f, this._missionProgressTracker.CurrentProgress);
			}
		}

		// Token: 0x04000091 RID: 145
		private const float MinProgressValue = 0f;

		// Token: 0x04000092 RID: 146
		private const float MaxProgressValue = 1f;

		// Token: 0x04000093 RID: 147
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000094 RID: 148
		private MissionQuestBarVM _dataSource;

		// Token: 0x04000095 RID: 149
		private IMissionProgressTracker _missionProgressTracker;
	}
}
