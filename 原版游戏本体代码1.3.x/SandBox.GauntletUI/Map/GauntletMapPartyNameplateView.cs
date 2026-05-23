using System;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200003E RID: 62
	[OverrideView(typeof(MapPartyNameplateView))]
	public class GauntletMapPartyNameplateView : MapView
	{
		// Token: 0x060002E4 RID: 740 RVA: 0x00011104 File Offset: 0x0000F304
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new PartyNameplatesVM(base.MapScreen.MapCameraView.Camera, new Action(base.MapScreen.FastMoveCameraToMainParty));
			GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
			base.Layer = mapView.GauntletNameplateLayer;
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._movie = this._layerAsGauntletLayer.LoadMovie("PartyNameplate", this._dataSource);
			this._dataSource.Initialize();
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x00011194 File Offset: 0x0000F394
		protected override void OnMapScreenUpdate(float dt)
		{
			base.OnMapScreenUpdate(dt);
			this._dataSource.Update();
			bool shouldShowFullName = base.MapScreen.SceneLayer.Input.IsGameKeyDown(5);
			EncounterModel encounterModel = Campaign.Current.Models.EncounterModel;
			for (int i = 0; i < this._dataSource.Nameplates.Count; i++)
			{
				PartyNameplateVM partyNameplateVM = this._dataSource.Nameplates[i];
				partyNameplateVM.ShouldShowFullName = shouldShowFullName;
				TextObject textObject;
				partyNameplateVM.CanParley = partyNameplateVM.ShouldShowFullName && encounterModel.CanMainHeroDoParleyWithParty(partyNameplateVM.Party.Party, out textObject);
			}
			if (this._dataSource.PlayerNameplate != null)
			{
				this._dataSource.PlayerNameplate.ShouldShowFullName = shouldShowFullName;
			}
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x00011250 File Offset: 0x0000F450
		protected override void OnResume()
		{
			base.OnResume();
			foreach (PartyNameplateVM partyNameplateVM in this._dataSource.Nameplates)
			{
				partyNameplateVM.RefreshDynamicProperties(true);
			}
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x000112A8 File Offset: 0x0000F4A8
		protected override void OnFinalize()
		{
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			this._dataSource.OnFinalize();
			this._layerAsGauntletLayer = null;
			base.Layer = null;
			this._movie = null;
			this._dataSource = null;
			base.OnFinalize();
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x000112E8 File Offset: 0x0000F4E8
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x00011304 File Offset: 0x0000F504
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x0400011E RID: 286
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x0400011F RID: 287
		private PartyNameplatesVM _dataSource;

		// Token: 0x04000120 RID: 288
		private GauntletMovieIdentifier _movie;
	}
}
