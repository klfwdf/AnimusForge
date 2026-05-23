using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000077 RID: 119
	internal class MapAudioManager : CampaignEntityVisualComponent
	{
		// Token: 0x170000AF RID: 175
		// (get) Token: 0x0600053A RID: 1338 RVA: 0x00027E8B File Offset: 0x0002608B
		public override int Priority
		{
			get
			{
				return 70;
			}
		}

		// Token: 0x0600053B RID: 1339 RVA: 0x00027E8F File Offset: 0x0002608F
		public MapAudioManager()
		{
			this._mapScene = Campaign.Current.MapSceneWrapper as MapScene;
		}

		// Token: 0x0600053C RID: 1340 RVA: 0x00027EAC File Offset: 0x000260AC
		public override void OnVisualTick(MapScreen screen, float realDt, float dt)
		{
			if (CampaignTime.Now.GetSeasonOfYear != this._lastCachedSeason)
			{
				SoundManager.SetGlobalParameter("Season", (float)CampaignTime.Now.GetSeasonOfYear);
				this._lastCachedSeason = CampaignTime.Now.GetSeasonOfYear;
			}
			if (Math.Abs(this._lastCameraZ - this._mapScene.Scene.LastFinalRenderCameraPosition.Z) > 0.1f)
			{
				SoundManager.SetGlobalParameter("CampaignCameraHeight", this._mapScene.Scene.LastFinalRenderCameraPosition.Z);
				this._lastCameraZ = this._mapScene.Scene.LastFinalRenderCameraPosition.Z;
			}
			if ((int)CampaignTime.Now.CurrentHourInDay == this._lastHourUpdate)
			{
				SoundManager.SetGlobalParameter("Daytime", CampaignTime.Now.CurrentHourInDay);
				this._lastHourUpdate = (int)CampaignTime.Now.CurrentHourInDay;
			}
		}

		// Token: 0x0400026E RID: 622
		private const string SeasonParameterId = "Season";

		// Token: 0x0400026F RID: 623
		private const string CameraHeightParameterId = "CampaignCameraHeight";

		// Token: 0x04000270 RID: 624
		private const string TimeOfDayParameterId = "Daytime";

		// Token: 0x04000271 RID: 625
		private const string WeatherEventIntensityParameterId = "Rainfall";

		// Token: 0x04000272 RID: 626
		private CampaignTime.Seasons _lastCachedSeason;

		// Token: 0x04000273 RID: 627
		private float _lastCameraZ;

		// Token: 0x04000274 RID: 628
		private int _lastHourUpdate;

		// Token: 0x04000275 RID: 629
		private MapScene _mapScene;
	}
}
