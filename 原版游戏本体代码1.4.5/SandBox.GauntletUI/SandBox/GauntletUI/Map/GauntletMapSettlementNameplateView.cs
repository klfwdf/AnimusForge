using System;
using System.Collections.Generic;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapSettlementNameplateView))]
public class GauntletMapSettlementNameplateView : MapView, IGauntletMapEventVisualHandler
{
	private GauntletLayer _layerAsGauntletLayer;

	private GauntletMovieIdentifier _movie;

	private SettlementNameplatesVM _dataSource;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new SettlementNameplatesVM(base.MapScreen.MapCameraView.Camera, base.MapScreen.FastMoveCameraToPosition);
		GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
		base.Layer = mapView.GauntletNameplateLayer;
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_movie = _layerAsGauntletLayer.LoadMovie("SettlementNameplate", _dataSource);
		List<Tuple<Settlement, GameEntity>> list = new List<Tuple<Settlement, GameEntity>>();
		foreach (Settlement item2 in Settlement.All)
		{
			GameEntity strategicEntity = SettlementVisualManager.Current.GetSettlementVisual(item2).StrategicEntity;
			Tuple<Settlement, GameEntity> item = new Tuple<Settlement, GameEntity>(item2, strategicEntity);
			list.Add(item);
		}
		CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, OnHideoutSpotted);
		_dataSource.Initialize(list);
		if (!(Campaign.Current.VisualCreator.MapEventVisualCreator is GauntletMapEventVisualCreator gauntletMapEventVisualCreator))
		{
			return;
		}
		gauntletMapEventVisualCreator.Handlers.Add(this);
		foreach (GauntletMapEventVisual currentEvent in gauntletMapEventVisualCreator.GetCurrentEvents())
		{
			GetNameplateOfMapEvent(currentEvent)?.OnMapEventStartedOnSettlement(currentEvent.MapEvent);
		}
	}

	protected override void OnResume()
	{
		base.OnResume();
		_dataSource.RefreshDynamicPropertiesOfNameplates(forceUpdate: true);
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		_dataSource.Update();
		bool flag = base.MapScreen.SceneLayer.Input.IsGameKeyDown(5);
		for (int i = 0; i < _dataSource.AllNameplates.Count; i++)
		{
			SettlementNameplateVM settlementNameplateVM = _dataSource.AllNameplates[i];
			if (settlementNameplateVM.IsInside && settlementNameplateVM.IsVisibleOnMap)
			{
				settlementNameplateVM.CanParley = flag && Campaign.Current.Models.EncounterModel.CanMainHeroDoParleyWithParty(settlementNameplateVM.Settlement.Party, out var _);
			}
		}
	}

	protected override void OnFinalize()
	{
		if (Campaign.Current.VisualCreator.MapEventVisualCreator is GauntletMapEventVisualCreator gauntletMapEventVisualCreator)
		{
			gauntletMapEventVisualCreator.Handlers.Remove(this);
		}
		CampaignEvents.OnHideoutSpottedEvent.ClearListeners(this);
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

	private void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
	{
		MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/ui/notification/hideout_found"), hideoutParty.Settlement.GetPosition());
	}

	private SettlementNameplateVM GetNameplateOfMapEvent(GauntletMapEventVisual mapEvent)
	{
		int num;
		if (mapEvent.MapEvent.EventType == MapEvent.BattleTypes.Raid)
		{
			Settlement mapEventSettlement = mapEvent.MapEvent.MapEventSettlement;
			num = (((mapEventSettlement != null && mapEventSettlement.IsUnderRaid) || (mapEvent?.MapEvent.IsFinalized ?? false)) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		int num2;
		if (mapEvent.MapEvent.EventType == MapEvent.BattleTypes.Siege)
		{
			Settlement mapEventSettlement2 = mapEvent.MapEvent.MapEventSettlement;
			num2 = (((mapEventSettlement2 != null && mapEventSettlement2.IsUnderSiege) || (mapEvent?.MapEvent.IsFinalized ?? false)) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag2 = (byte)num2 != 0;
		int num3;
		if (mapEvent.MapEvent.EventType == MapEvent.BattleTypes.SallyOut || mapEvent.MapEvent.EventType == MapEvent.BattleTypes.BlockadeSallyOutBattle)
		{
			Settlement mapEventSettlement3 = mapEvent.MapEvent.MapEventSettlement;
			num3 = (((mapEventSettlement3 != null && mapEventSettlement3.IsUnderSiege) || (mapEvent?.MapEvent.IsFinalized ?? false)) ? 1 : 0);
		}
		else
		{
			num3 = 0;
		}
		bool flag3 = (byte)num3 != 0;
		if (mapEvent.MapEvent.MapEventSettlement != null && (flag2 || flag || flag3))
		{
			return _dataSource.GetNameplateOfSettlement(mapEvent.MapEvent.MapEventSettlement);
		}
		return null;
	}

	void IGauntletMapEventVisualHandler.OnNewEventStarted(GauntletMapEventVisual newEvent)
	{
		GetNameplateOfMapEvent(newEvent)?.OnMapEventStartedOnSettlement(newEvent.MapEvent);
	}

	void IGauntletMapEventVisualHandler.OnInitialized(GauntletMapEventVisual newEvent)
	{
		GetNameplateOfMapEvent(newEvent)?.OnMapEventStartedOnSettlement(newEvent.MapEvent);
	}

	void IGauntletMapEventVisualHandler.OnEventEnded(GauntletMapEventVisual newEvent)
	{
		GetNameplateOfMapEvent(newEvent)?.OnMapEventEndedOnSettlement();
	}

	void IGauntletMapEventVisualHandler.OnEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent)
	{
	}
}
