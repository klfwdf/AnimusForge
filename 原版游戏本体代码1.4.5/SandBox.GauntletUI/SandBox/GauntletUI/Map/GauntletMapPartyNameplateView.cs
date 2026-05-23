using SandBox.View.Map;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapPartyNameplateView))]
public class GauntletMapPartyNameplateView : MapView
{
	private GauntletLayer _layerAsGauntletLayer;

	private PartyNameplatesVM _dataSource;

	private GauntletMovieIdentifier _movie;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new PartyNameplatesVM(base.MapScreen.MapCameraView.Camera, base.MapScreen.FastMoveCameraToMainParty);
		GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
		base.Layer = mapView.GauntletNameplateLayer;
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_movie = _layerAsGauntletLayer.LoadMovie("PartyNameplate", _dataSource);
		_dataSource.Initialize();
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		_dataSource.Update();
		bool shouldShowFullName = base.MapScreen.SceneLayer.Input.IsGameKeyDown(5);
		EncounterModel encounterModel = Campaign.Current.Models.EncounterModel;
		for (int i = 0; i < _dataSource.Nameplates.Count; i++)
		{
			PartyNameplateVM partyNameplateVM = _dataSource.Nameplates[i];
			partyNameplateVM.ShouldShowFullName = shouldShowFullName;
			partyNameplateVM.CanParley = partyNameplateVM.ShouldShowFullName && encounterModel.CanMainHeroDoParleyWithParty(partyNameplateVM.Party.Party, out var _);
		}
		if (_dataSource.PlayerNameplate != null)
		{
			_dataSource.PlayerNameplate.ShouldShowFullName = shouldShowFullName;
		}
	}

	protected override void OnResume()
	{
		base.OnResume();
		foreach (PartyNameplateVM nameplate in _dataSource.Nameplates)
		{
			nameplate.RefreshDynamicProperties(forceUpdate: true);
		}
	}

	protected override void OnFinalize()
	{
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		_dataSource.OnFinalize();
		_layerAsGauntletLayer = null;
		base.Layer = null;
		_movie = null;
		_dataSource = null;
		base.OnFinalize();
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}
}
