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
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000041 RID: 65
	[OverrideView(typeof(MapSettlementNameplateView))]
	public class GauntletMapSettlementNameplateView : MapView, IGauntletMapEventVisualHandler
	{
		// Token: 0x060002F7 RID: 759 RVA: 0x0001157C File Offset: 0x0000F77C
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new SettlementNameplatesVM(base.MapScreen.MapCameraView.Camera, new Action<CampaignVec2>(base.MapScreen.FastMoveCameraToPosition));
			GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
			base.Layer = mapView.GauntletNameplateLayer;
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._movie = this._layerAsGauntletLayer.LoadMovie("SettlementNameplate", this._dataSource);
			List<Tuple<Settlement, GameEntity>> list = new List<Tuple<Settlement, GameEntity>>();
			foreach (Settlement settlement in Settlement.All)
			{
				GameEntity strategicEntity = SettlementVisualManager.Current.GetSettlementVisual(settlement).StrategicEntity;
				Tuple<Settlement, GameEntity> item = new Tuple<Settlement, GameEntity>(settlement, strategicEntity);
				list.Add(item);
			}
			CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, new Action<PartyBase, PartyBase>(this.OnHideoutSpotted));
			this._dataSource.Initialize(list);
			GauntletMapEventVisualCreator gauntletMapEventVisualCreator;
			if ((gauntletMapEventVisualCreator = Campaign.Current.VisualCreator.MapEventVisualCreator as GauntletMapEventVisualCreator) != null)
			{
				gauntletMapEventVisualCreator.Handlers.Add(this);
				foreach (GauntletMapEventVisual gauntletMapEventVisual in gauntletMapEventVisualCreator.GetCurrentEvents())
				{
					SettlementNameplateVM nameplateOfMapEvent = this.GetNameplateOfMapEvent(gauntletMapEventVisual);
					if (nameplateOfMapEvent != null)
					{
						nameplateOfMapEvent.OnMapEventStartedOnSettlement(gauntletMapEventVisual.MapEvent);
					}
				}
			}
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x00011708 File Offset: 0x0000F908
		protected override void OnResume()
		{
			base.OnResume();
			this._dataSource.RefreshDynamicPropertiesOfNameplates(true);
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x0001171C File Offset: 0x0000F91C
		protected override void OnMapScreenUpdate(float dt)
		{
			base.OnMapScreenUpdate(dt);
			this._dataSource.Update();
			bool flag = base.MapScreen.SceneLayer.Input.IsGameKeyDown(5);
			for (int i = 0; i < this._dataSource.AllNameplates.Count; i++)
			{
				SettlementNameplateVM settlementNameplateVM = this._dataSource.AllNameplates[i];
				if (settlementNameplateVM.IsInside && settlementNameplateVM.IsVisibleOnMap)
				{
					TextObject textObject;
					settlementNameplateVM.CanParley = flag && Campaign.Current.Models.EncounterModel.CanMainHeroDoParleyWithParty(settlementNameplateVM.Settlement.Party, out textObject);
				}
			}
		}

		// Token: 0x060002FA RID: 762 RVA: 0x000117BC File Offset: 0x0000F9BC
		protected override void OnFinalize()
		{
			GauntletMapEventVisualCreator gauntletMapEventVisualCreator;
			if ((gauntletMapEventVisualCreator = Campaign.Current.VisualCreator.MapEventVisualCreator as GauntletMapEventVisualCreator) != null)
			{
				gauntletMapEventVisualCreator.Handlers.Remove(this);
			}
			CampaignEvents.OnHideoutSpottedEvent.ClearListeners(this);
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			this._dataSource.OnFinalize();
			this._layerAsGauntletLayer = null;
			base.Layer = null;
			this._movie = null;
			this._dataSource = null;
			base.OnFinalize();
		}

		// Token: 0x060002FB RID: 763 RVA: 0x00011837 File Offset: 0x0000FA37
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060002FC RID: 764 RVA: 0x00011853 File Offset: 0x0000FA53
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x060002FD RID: 765 RVA: 0x0001186F File Offset: 0x0000FA6F
		private void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
		{
			MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/ui/notification/hideout_found"), hideoutParty.Settlement.GetPosition());
		}

		// Token: 0x060002FE RID: 766 RVA: 0x0001188C File Offset: 0x0000FA8C
		private SettlementNameplateVM GetNameplateOfMapEvent(GauntletMapEventVisual mapEvent)
		{
			bool flag;
			if (mapEvent.MapEvent.EventType == MapEvent.BattleTypes.Raid)
			{
				Settlement mapEventSettlement = mapEvent.MapEvent.MapEventSettlement;
				flag = (mapEventSettlement != null && mapEventSettlement.IsUnderRaid) || (mapEvent != null && mapEvent.MapEvent.IsFinalized);
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			bool flag3;
			if (mapEvent.MapEvent.EventType == MapEvent.BattleTypes.Siege)
			{
				Settlement mapEventSettlement2 = mapEvent.MapEvent.MapEventSettlement;
				flag3 = (mapEventSettlement2 != null && mapEventSettlement2.IsUnderSiege) || (mapEvent != null && mapEvent.MapEvent.IsFinalized);
			}
			else
			{
				flag3 = false;
			}
			bool flag4 = flag3;
			bool flag5;
			if (mapEvent.MapEvent.EventType == MapEvent.BattleTypes.SallyOut || mapEvent.MapEvent.EventType == MapEvent.BattleTypes.BlockadeSallyOutBattle)
			{
				Settlement mapEventSettlement3 = mapEvent.MapEvent.MapEventSettlement;
				flag5 = (mapEventSettlement3 != null && mapEventSettlement3.IsUnderSiege) || (mapEvent != null && mapEvent.MapEvent.IsFinalized);
			}
			else
			{
				flag5 = false;
			}
			bool flag6 = flag5;
			if (mapEvent.MapEvent.MapEventSettlement != null && (flag4 || flag2 || flag6))
			{
				return this._dataSource.GetNameplateOfSettlement(mapEvent.MapEvent.MapEventSettlement);
			}
			return null;
		}

		// Token: 0x060002FF RID: 767 RVA: 0x00011991 File Offset: 0x0000FB91
		void IGauntletMapEventVisualHandler.OnNewEventStarted(GauntletMapEventVisual newEvent)
		{
			SettlementNameplateVM nameplateOfMapEvent = this.GetNameplateOfMapEvent(newEvent);
			if (nameplateOfMapEvent == null)
			{
				return;
			}
			nameplateOfMapEvent.OnMapEventStartedOnSettlement(newEvent.MapEvent);
		}

		// Token: 0x06000300 RID: 768 RVA: 0x000119AA File Offset: 0x0000FBAA
		void IGauntletMapEventVisualHandler.OnInitialized(GauntletMapEventVisual newEvent)
		{
			SettlementNameplateVM nameplateOfMapEvent = this.GetNameplateOfMapEvent(newEvent);
			if (nameplateOfMapEvent == null)
			{
				return;
			}
			nameplateOfMapEvent.OnMapEventStartedOnSettlement(newEvent.MapEvent);
		}

		// Token: 0x06000301 RID: 769 RVA: 0x000119C3 File Offset: 0x0000FBC3
		void IGauntletMapEventVisualHandler.OnEventEnded(GauntletMapEventVisual newEvent)
		{
			SettlementNameplateVM nameplateOfMapEvent = this.GetNameplateOfMapEvent(newEvent);
			if (nameplateOfMapEvent == null)
			{
				return;
			}
			nameplateOfMapEvent.OnMapEventEndedOnSettlement();
		}

		// Token: 0x06000302 RID: 770 RVA: 0x000119D6 File Offset: 0x0000FBD6
		void IGauntletMapEventVisualHandler.OnEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent)
		{
		}

		// Token: 0x04000125 RID: 293
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x04000126 RID: 294
		private GauntletMovieIdentifier _movie;

		// Token: 0x04000127 RID: 295
		private SettlementNameplatesVM _dataSource;
	}
}
