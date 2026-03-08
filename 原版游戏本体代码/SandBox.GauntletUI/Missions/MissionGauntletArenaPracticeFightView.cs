using System;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x02000019 RID: 25
	[OverrideView(typeof(MissionArenaPracticeFightView))]
	public class MissionGauntletArenaPracticeFightView : MissionView
	{
		// Token: 0x06000173 RID: 371 RVA: 0x0000A2E0 File Offset: 0x000084E0
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			ArenaPracticeFightMissionController missionBehavior = base.Mission.GetMissionBehavior<ArenaPracticeFightMissionController>();
			this._dataSource = new MissionArenaPracticeFightVM(missionBehavior);
			this._gauntletLayer = new GauntletLayer("MissionArenaPracticeFight", this.ViewOrderPriority, false);
			this._movie = this._gauntletLayer.LoadMovie("ArenaPracticeFight", this._dataSource);
			base.MissionScreen.AddLayer(this._gauntletLayer);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000A34F File Offset: 0x0000854F
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			this._dataSource.Tick();
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000A363 File Offset: 0x00008563
		public override void OnMissionScreenFinalize()
		{
			this._dataSource.OnFinalize();
			this._gauntletLayer.ReleaseMovie(this._movie);
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			base.OnMissionScreenFinalize();
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0000A398 File Offset: 0x00008598
		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 0f;
			}
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000A3BD File Offset: 0x000085BD
		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 1f;
			}
		}

		// Token: 0x04000072 RID: 114
		private MissionArenaPracticeFightVM _dataSource;

		// Token: 0x04000073 RID: 115
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000074 RID: 116
		private GauntletMovieIdentifier _movie;
	}
}
