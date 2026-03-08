using System;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Map.Tracker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000043 RID: 67
	[OverrideView(typeof(MapTrackersView))]
	public class GauntletMapTrackersView : MapTrackersView
	{
		// Token: 0x0600030E RID: 782 RVA: 0x00011C14 File Offset: 0x0000FE14
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new MapTrackerCollectionVM();
			MapTrackerItemVM.OnFastMoveCameraToPosition = new Action<CampaignVec2>(this.FastMoveCameraToPosition);
			GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
			base.Layer = mapView.GauntletNameplateLayer;
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._movie = this._layerAsGauntletLayer.LoadMovie("MapTrackers", this._dataSource);
		}

		// Token: 0x0600030F RID: 783 RVA: 0x00011C88 File Offset: 0x0000FE88
		protected override void OnResume()
		{
			base.OnResume();
			this._dataSource.UpdateProperties();
		}

		// Token: 0x06000310 RID: 784 RVA: 0x00011C9C File Offset: 0x0000FE9C
		private void UpdateTrackerPropertiesAux(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				MapTrackerItemVM mapTrackerItemVM = this._dataSource.Trackers[i];
				mapTrackerItemVM.UpdateProperties();
				float screenX;
				float screenY;
				float screenW;
				this.GetScreenPosition(mapTrackerItemVM.TrackedObject, out screenX, out screenY, out screenW);
				mapTrackerItemVM.UpdatePosition(screenX, screenY, screenW);
			}
		}

		// Token: 0x06000311 RID: 785 RVA: 0x00011CE9 File Offset: 0x0000FEE9
		protected override void OnMapScreenUpdate(float dt)
		{
			base.OnMapScreenUpdate(dt);
			TWParallel.For(0, this._dataSource.Trackers.Count, new TWParallel.ParallelForAuxPredicate(this.UpdateTrackerPropertiesAux), 32);
			this._dataSource.Tick(dt);
		}

		// Token: 0x06000312 RID: 786 RVA: 0x00011D24 File Offset: 0x0000FF24
		protected override void OnFinalize()
		{
			MapTrackerItemVM.OnFastMoveCameraToPosition = null;
			this._dataSource.OnFinalize();
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			this._layerAsGauntletLayer = null;
			base.Layer = null;
			this._movie = null;
			this._dataSource = null;
			base.OnFinalize();
		}

		// Token: 0x06000313 RID: 787 RVA: 0x00011D75 File Offset: 0x0000FF75
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x06000314 RID: 788 RVA: 0x00011D91 File Offset: 0x0000FF91
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x06000315 RID: 789 RVA: 0x00011DB0 File Offset: 0x0000FFB0
		private void GetScreenPosition(ITrackableCampaignObject trackable, out float screenX, out float screenY, out float screenW)
		{
			float a = 0f;
			Vec3 position = trackable.GetPosition();
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			CampaignVec2 campaignVec = new CampaignVec2(position.AsVec2, true);
			mapSceneWrapper.GetHeightAtPoint(campaignVec, ref a);
			position.z = MathF.Max(a, 0f);
			screenX = -5000f;
			screenY = -5000f;
			screenW = -1f;
			MBWindowManager.WorldToScreenInsideUsableArea(base.MapScreen.MapCameraView.Camera, position, ref screenX, ref screenY, ref screenW);
		}

		// Token: 0x06000316 RID: 790 RVA: 0x00011E2F File Offset: 0x0001002F
		private void FastMoveCameraToPosition(CampaignVec2 target)
		{
			base.MapScreen.FastMoveCameraToPosition(target);
		}

		// Token: 0x0400012B RID: 299
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x0400012C RID: 300
		private GauntletMovieIdentifier _movie;

		// Token: 0x0400012D RID: 301
		private MapTrackerCollectionVM _dataSource;
	}
}
