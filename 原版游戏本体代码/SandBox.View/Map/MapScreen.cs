using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.View.Menu;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.View.Scripts;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map
{
	// Token: 0x02000057 RID: 87
	[GameStateScreen(typeof(MapState))]
	public class MapScreen : ScreenBase, IMapStateHandler, IGameStateListener, IChatLogHandlerScreen
	{
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060002B7 RID: 695 RVA: 0x0001835E File Offset: 0x0001655E
		public IInputContext Input
		{
			get
			{
				return this.SceneLayer.Input;
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060002B8 RID: 696 RVA: 0x0001836B File Offset: 0x0001656B
		// (set) Token: 0x060002B9 RID: 697 RVA: 0x00018372 File Offset: 0x00016572
		public static MapScreen Instance { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060002BA RID: 698 RVA: 0x0001837A File Offset: 0x0001657A
		public bool IsReady
		{
			get
			{
				return this._isReadyForRender;
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060002BB RID: 699 RVA: 0x00018382 File Offset: 0x00016582
		// (set) Token: 0x060002BC RID: 700 RVA: 0x0001838A File Offset: 0x0001658A
		public INavigationHandler NavigationHandler
		{
			get
			{
				return this._navigationHandler;
			}
			set
			{
				if (this._navigationHandler != null && value != null && value != this._navigationHandler)
				{
					Debug.FailedAssert("Navigation handler should not be changed after map bar initialization", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "NavigationHandler", 125);
					return;
				}
				this._navigationHandler = value;
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060002BD RID: 701 RVA: 0x000183BE File Offset: 0x000165BE
		// (set) Token: 0x060002BE RID: 702 RVA: 0x000183C6 File Offset: 0x000165C6
		public MapEntityVisual CurrentVisualOfTooltip { get; private set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060002BF RID: 703 RVA: 0x000183CF File Offset: 0x000165CF
		// (set) Token: 0x060002C0 RID: 704 RVA: 0x000183D7 File Offset: 0x000165D7
		public CampaignMapSiegePrefabEntityCache PrefabEntityCache { get; private set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060002C1 RID: 705 RVA: 0x000183E0 File Offset: 0x000165E0
		// (set) Token: 0x060002C2 RID: 706 RVA: 0x000183E8 File Offset: 0x000165E8
		public MapEncyclopediaView EncyclopediaScreenManager { get; private set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060002C3 RID: 707 RVA: 0x000183F1 File Offset: 0x000165F1
		// (set) Token: 0x060002C4 RID: 708 RVA: 0x000183F9 File Offset: 0x000165F9
		public bool IsEscapeMenuOpened { get; private set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060002C5 RID: 709 RVA: 0x00018402 File Offset: 0x00016602
		// (set) Token: 0x060002C6 RID: 710 RVA: 0x0001840A File Offset: 0x0001660A
		public MapNotificationView MapNotificationView { get; private set; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060002C7 RID: 711 RVA: 0x00018414 File Offset: 0x00016614
		public Dictionary<Tuple<Material, Banner>, Material> BannerTexturedMaterialCache
		{
			get
			{
				Dictionary<Tuple<Material, Banner>, Material> result;
				if ((result = this._bannerTexturedMaterialCache) == null)
				{
					result = (this._bannerTexturedMaterialCache = new Dictionary<Tuple<Material, Banner>, Material>());
				}
				return result;
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060002C8 RID: 712 RVA: 0x00018439 File Offset: 0x00016639
		public bool IsInMenu
		{
			get
			{
				return this._menuViewContext != null;
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060002C9 RID: 713 RVA: 0x00018444 File Offset: 0x00016644
		// (set) Token: 0x060002CA RID: 714 RVA: 0x0001844C File Offset: 0x0001664C
		public SceneLayer SceneLayer { get; private set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060002CB RID: 715 RVA: 0x00018455 File Offset: 0x00016655
		// (set) Token: 0x060002CC RID: 716 RVA: 0x0001845D File Offset: 0x0001665D
		public MapCameraView MapCameraView { get; private set; }

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060002CD RID: 717 RVA: 0x00018466 File Offset: 0x00016666
		// (set) Token: 0x060002CE RID: 718 RVA: 0x0001846E File Offset: 0x0001666E
		public bool MapSceneCursorActive
		{
			get
			{
				return this._mapSceneCursorActive;
			}
			set
			{
				if (this._mapSceneCursorActive != value)
				{
					this._mapSceneCursorActive = value;
				}
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060002CF RID: 719 RVA: 0x00018480 File Offset: 0x00016680
		// (set) Token: 0x060002D0 RID: 720 RVA: 0x00018488 File Offset: 0x00016688
		public GameEntity ContourMaskEntity { get; private set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060002D1 RID: 721 RVA: 0x00018491 File Offset: 0x00016691
		// (set) Token: 0x060002D2 RID: 722 RVA: 0x00018499 File Offset: 0x00016699
		public MapCursor MapCursor { get; private set; } = new MapCursor();

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060002D3 RID: 723 RVA: 0x000184A2 File Offset: 0x000166A2
		// (set) Token: 0x060002D4 RID: 724 RVA: 0x000184AA File Offset: 0x000166AA
		public List<Mesh> InactiveLightMeshes { get; private set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060002D5 RID: 725 RVA: 0x000184B3 File Offset: 0x000166B3
		// (set) Token: 0x060002D6 RID: 726 RVA: 0x000184BB File Offset: 0x000166BB
		public List<Mesh> ActiveLightMeshes { get; private set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x060002D7 RID: 727 RVA: 0x000184C4 File Offset: 0x000166C4
		// (set) Token: 0x060002D8 RID: 728 RVA: 0x000184CC File Offset: 0x000166CC
		public Scene MapScene { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x060002D9 RID: 729 RVA: 0x000184D5 File Offset: 0x000166D5
		// (set) Token: 0x060002DA RID: 730 RVA: 0x000184DD File Offset: 0x000166DD
		public MapState MapState { get; private set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x060002DB RID: 731 RVA: 0x000184E6 File Offset: 0x000166E6
		// (set) Token: 0x060002DC RID: 732 RVA: 0x000184EE File Offset: 0x000166EE
		public bool IsInBattleSimulation { get; private set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x060002DD RID: 733 RVA: 0x000184F7 File Offset: 0x000166F7
		// (set) Token: 0x060002DE RID: 734 RVA: 0x000184FF File Offset: 0x000166FF
		public bool IsInTownManagement { get; private set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x060002DF RID: 735 RVA: 0x00018508 File Offset: 0x00016708
		// (set) Token: 0x060002E0 RID: 736 RVA: 0x00018510 File Offset: 0x00016710
		public bool IsInHideoutTroopManage { get; private set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060002E1 RID: 737 RVA: 0x00018519 File Offset: 0x00016719
		// (set) Token: 0x060002E2 RID: 738 RVA: 0x00018521 File Offset: 0x00016721
		public bool IsInArmyManagement { get; private set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060002E3 RID: 739 RVA: 0x0001852A File Offset: 0x0001672A
		// (set) Token: 0x060002E4 RID: 740 RVA: 0x00018532 File Offset: 0x00016732
		public bool IsInRecruitment { get; private set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060002E5 RID: 741 RVA: 0x0001853B File Offset: 0x0001673B
		// (set) Token: 0x060002E6 RID: 742 RVA: 0x00018543 File Offset: 0x00016743
		public bool IsBarExtended { get; private set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060002E7 RID: 743 RVA: 0x0001854C File Offset: 0x0001674C
		// (set) Token: 0x060002E8 RID: 744 RVA: 0x00018554 File Offset: 0x00016754
		public bool IsInCampaignOptions { get; private set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x060002E9 RID: 745 RVA: 0x0001855D File Offset: 0x0001675D
		// (set) Token: 0x060002EA RID: 746 RVA: 0x00018565 File Offset: 0x00016765
		public bool IsMarriageOfferPopupActive { get; private set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060002EB RID: 747 RVA: 0x0001856E File Offset: 0x0001676E
		// (set) Token: 0x060002EC RID: 748 RVA: 0x00018576 File Offset: 0x00016776
		public bool IsMapCheatsActive { get; private set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060002ED RID: 749 RVA: 0x0001857F File Offset: 0x0001677F
		// (set) Token: 0x060002EE RID: 750 RVA: 0x00018587 File Offset: 0x00016787
		public bool IsMapIncidentActive { get; private set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060002EF RID: 751 RVA: 0x00018590 File Offset: 0x00016790
		// (set) Token: 0x060002F0 RID: 752 RVA: 0x00018598 File Offset: 0x00016798
		public bool IsHeirSelectionPopupActive { get; private set; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060002F1 RID: 753 RVA: 0x000185A1 File Offset: 0x000167A1
		// (set) Token: 0x060002F2 RID: 754 RVA: 0x000185A9 File Offset: 0x000167A9
		public bool IsOverlayContextMenuEnabled { get; private set; }

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060002F3 RID: 755 RVA: 0x000185B2 File Offset: 0x000167B2
		// (set) Token: 0x060002F4 RID: 756 RVA: 0x000185BA File Offset: 0x000167BA
		public bool IsSoundOn { get; private set; } = true;

		// Token: 0x060002F5 RID: 757 RVA: 0x000185C4 File Offset: 0x000167C4
		public MapScreen(MapState mapState)
		{
			this.MapState = mapState;
			mapState.Handler = this;
			this._periodicCampaignUIEvents = new List<MBCampaignEvent>();
			this.InitializeVisuals();
			CampaignMusicHandler.Create();
			this._mapViewsContainer = new MapViewsContainer();
			this.MapCameraView = (MapCameraView)this.AddMapView<MapCameraView>(Array.Empty<object>());
			this.AddMapView<MapBarView>(Array.Empty<object>());
			this.AddMapView<MapConversationView>(Array.Empty<object>());
			this._conversationView = this.GetMapView<MapConversationView>();
			this.MapTracksCampaignBehavior = Campaign.Current.GetCampaignBehavior<IMapTracksCampaignBehavior>();
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x000186B4 File Offset: 0x000168B4
		public void OnHoverMapEntity(MapEntityVisual mapEntityVisual)
		{
			uint hashCode = (uint)mapEntityVisual.GetHashCode();
			if (this._tooltipTargetHash != hashCode)
			{
				this._tooltipTargetHash = hashCode;
				this._tooltipTargetObject = null;
				mapEntityVisual.OnHover();
			}
		}

		// Token: 0x060002F7 RID: 759 RVA: 0x000186E5 File Offset: 0x000168E5
		public void RemoveMapTooltip()
		{
			if (this._tooltipTargetObject != null || this._tooltipTargetHash != 0U)
			{
				this._tooltipTargetObject = null;
				this._tooltipTargetHash = 0U;
				MBInformationManager.HideInformations();
				MapEntityVisual currentVisualOfTooltip = this.CurrentVisualOfTooltip;
				if (currentVisualOfTooltip == null)
				{
					return;
				}
				currentVisualOfTooltip.OnHoverEnd();
			}
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x0001871C File Offset: 0x0001691C
		private static void PreloadTextures()
		{
			List<string> list = new List<string>();
			list.Add("gui_map_circle_enemy");
			list.Add("gui_map_circle_enemy_selected");
			list.Add("gui_map_circle_neutral");
			list.Add("gui_map_circle_neutral_selected");
			for (int i = 2; i <= 5; i++)
			{
				list.Add("gui_map_circle_enemy_selected_" + i);
				list.Add("gui_map_circle_neutral_selected_" + i);
			}
			for (int j = 0; j < list.Count; j++)
			{
				Texture.GetFromResource(list[j]).PreloadTexture(false);
			}
			list.Clear();
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x000187BC File Offset: 0x000169BC
		private void SetCameraOfSceneLayer()
		{
			this.SceneLayer.SetCamera(this.MapCameraView.Camera);
			Vec3 origin = this.MapCameraView.CameraFrame.origin;
			origin.z = 0f;
			this.SceneLayer.SetFocusedShadowmap(false, ref origin, 0f);
		}

		// Token: 0x060002FA RID: 762 RVA: 0x00018810 File Offset: 0x00016A10
		protected override void OnResume()
		{
			base.OnResume();
			MapScreen.PreloadTextures();
			this.IsSoundOn = true;
			this.RestartAmbientSounds();
			if (this._gpuMemoryCleared)
			{
				this._gpuMemoryCleared = false;
			}
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnResume();
			});
			MenuContext menuContext = this.MapState.MenuContext;
			if (this._menuViewContext != null)
			{
				if (menuContext != null && menuContext != this._menuViewContext.MenuContext)
				{
					this._menuViewContext.UpdateMenuContext(menuContext);
				}
				else if (menuContext == null)
				{
					this.ExitMenuContext();
				}
			}
			MenuViewContext menuViewContext = this._menuViewContext;
			if (menuViewContext != null)
			{
				menuViewContext.OnResume();
			}
			(Campaign.Current.MapSceneWrapper as MapScene).ValidateAgentVisualsReseted();
		}

		// Token: 0x060002FB RID: 763 RVA: 0x000188CF File Offset: 0x00016ACF
		protected override void OnPause()
		{
			base.OnPause();
			MBInformationManager.HideInformations();
			this.PauseAmbientSounds();
			this.IsSoundOn = false;
			this._activatedFrameNo = Utilities.EngineFrameNo;
			this.HandleIfSceneIsReady();
		}

		// Token: 0x060002FC RID: 764 RVA: 0x000188FA File Offset: 0x00016AFA
		void IMapStateHandler.OnGameLoadFinished()
		{
			SandBoxViewVisualManager.OnGameLoadFinished();
		}

		// Token: 0x060002FD RID: 765 RVA: 0x00018904 File Offset: 0x00016B04
		protected override void OnActivate()
		{
			base.OnActivate();
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnActivate();
			});
			this.MapCameraView.OnActivate(this._leftButtonDraggingMode, this._clickedPosition);
			this._activatedFrameNo = Utilities.EngineFrameNo;
			this.HandleIfSceneIsReady();
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
			this.SetCameraOfSceneLayer();
			this.RestartAmbientSounds();
			MenuContext menuContext = this.MapState.MenuContext;
			if (this._menuViewContext != null)
			{
				if (menuContext != null && menuContext != this._menuViewContext.MenuContext)
				{
					this._menuViewContext.UpdateMenuContext(menuContext);
				}
				else if (menuContext == null)
				{
					this.ExitMenuContext();
				}
			}
			MenuViewContext menuViewContext = this._menuViewContext;
			if (menuViewContext != null)
			{
				menuViewContext.OnResume();
			}
			PartyBase.MainParty.SetVisualAsDirty();
			for (int i = base.Layers.Count - 1; i >= 0; i--)
			{
				if (base.Layers[i].IsActive && base.Layers[i].IsFocusLayer)
				{
					ScreenManager.TrySetFocus(base.Layers[i]);
				}
			}
		}

		// Token: 0x060002FE RID: 766 RVA: 0x00018A30 File Offset: 0x00016C30
		public void ClearGPUMemory()
		{
			if (true)
			{
				this.SceneLayer.ClearRuntimeGPUMemory(true);
				this.SceneLayer.SceneView.GetScene().DeleteWaterWakeRenderer();
			}
			SandBoxViewVisualManager.ClearVisualMemory();
			ThumbnailCacheManager.Current.ForceClearAllCache(true);
			Texture.ReleaseGpuMemories();
			this._gpuMemoryCleared = true;
		}

		// Token: 0x060002FF RID: 767 RVA: 0x00018A80 File Offset: 0x00016C80
		protected override void OnDeactivate()
		{
			this._sceneReadyFrameCounter = 0;
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.None));
			}
			this.PauseAmbientSounds();
			MenuViewContext menuViewContext = this._menuViewContext;
			if (menuViewContext != null)
			{
				menuViewContext.OnDeactivate();
			}
			MBInformationManager.HideInformations();
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnDeactivate();
			});
			base.OnDeactivate();
		}

		// Token: 0x06000300 RID: 768 RVA: 0x00018AFC File Offset: 0x00016CFC
		public override void OnFocusChangeOnGameWindow(bool focusGained)
		{
			base.OnFocusChangeOnGameWindow(focusGained);
			if (!focusGained && BannerlordConfig.StopGameOnFocusLost && !InformationManager.IsAnyInquiryActive())
			{
				MapEncyclopediaView encyclopediaScreenManager = this.EncyclopediaScreenManager;
				if ((encyclopediaScreenManager == null || !encyclopediaScreenManager.IsEncyclopediaOpen) && this._mapViewsContainer.IsOpeningEscapeMenuOnFocusChangeAllowedForAll())
				{
					this.OnEscapeMenuToggled(true);
				}
			}
			this._focusLost = !focusGained;
		}

		// Token: 0x06000301 RID: 769 RVA: 0x00018B58 File Offset: 0x00016D58
		public MapView AddMapView<T>(params object[] parameters) where T : MapView, new()
		{
			T mapViewWithType = this._mapViewsContainer.GetMapViewWithType<T>();
			if (mapViewWithType != null)
			{
				Debug.FailedAssert("Map view already added to the list", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "AddMapView", 545);
				Debug.Print("Map view already added to the list: " + typeof(T).Name + ". Returning existing view instead of creating new one.", 0, Debug.DebugColor.White, 17592186044416UL);
				return mapViewWithType;
			}
			MapView mapView = SandBoxViewCreator.CreateMapView<T>(parameters);
			mapView.MapScreen = this;
			mapView.MapState = this.MapState;
			this._mapViewsContainer.Add(mapView);
			mapView.CreateLayout();
			return mapView;
		}

		// Token: 0x06000302 RID: 770 RVA: 0x00018BF5 File Offset: 0x00016DF5
		public T GetMapView<T>() where T : MapView
		{
			return this._mapViewsContainer.GetMapViewWithType<T>();
		}

		// Token: 0x06000303 RID: 771 RVA: 0x00018C02 File Offset: 0x00016E02
		public void RemoveMapView(MapView mapView)
		{
			mapView.OnDeactivate();
			mapView.OnFinalize();
			this._mapViewsContainer.Remove(mapView);
		}

		// Token: 0x06000304 RID: 772 RVA: 0x00018C1C File Offset: 0x00016E1C
		public void AddEncounterOverlay(GameMenu.MenuOverlayType type)
		{
			if (this._encounterOverlay == null)
			{
				this._encounterOverlay = this.AddMapView<MapOverlayView>(new object[] { type });
				this._mapViewsContainer.Foreach(delegate(MapView view)
				{
					view.OnOverlayCreated();
				});
			}
		}

		// Token: 0x06000305 RID: 773 RVA: 0x00018C78 File Offset: 0x00016E78
		public void AddArmyOverlay(MapScreen.MapOverlayType type)
		{
			if (this._armyOverlay == null)
			{
				this._armyOverlay = this.AddMapView<MapOverlayView>(new object[] { type });
				this._mapViewsContainer.ForeachReverse(delegate(MapView view)
				{
					view.OnOverlayCreated();
				});
			}
		}

		// Token: 0x06000306 RID: 774 RVA: 0x00018CD4 File Offset: 0x00016ED4
		public void RemoveEncounterOverlay()
		{
			if (this._encounterOverlay != null)
			{
				this.RemoveMapView(this._encounterOverlay);
				this._encounterOverlay = null;
				this._mapViewsContainer.ForeachReverse(delegate(MapView view)
				{
					view.OnOverlayClosed();
				});
			}
		}

		// Token: 0x06000307 RID: 775 RVA: 0x00018D28 File Offset: 0x00016F28
		public void RemoveArmyOverlay()
		{
			if (this._armyOverlay != null)
			{
				this.RemoveMapView(this._armyOverlay);
				this._armyOverlay = null;
				this._mapViewsContainer.ForeachReverse(delegate(MapView view)
				{
					view.OnOverlayClosed();
				});
			}
		}

		// Token: 0x06000308 RID: 776 RVA: 0x00018D7C File Offset: 0x00016F7C
		protected override void OnInitialize()
		{
			base.OnInitialize();
			if (MBDebug.TestModeEnabled)
			{
				this.CheckValidityOfItems();
			}
			MapScreen.Instance = this;
			ThumbnailCacheManager.Current.ForceClearAllCache(true);
			this.MapCameraView.Initialize();
			ViewSubModule.BannerTexturedMaterialCache = this.BannerTexturedMaterialCache;
			this.SceneLayer = new SceneLayer(true, false);
			this.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
			this.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MapHotKeyCategory"));
			base.AddLayer(this.SceneLayer);
			this.MapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			Utilities.SetAllocationAlwaysValidScene(null);
			this.SceneLayer.SetScene(this.MapScene);
			this.SceneLayer.SceneView.SetEnable(false);
			this.SceneLayer.SetSceneUsesShadows(true);
			this.SceneLayer.SetRenderWithPostfx(true);
			this.SceneLayer.SetSceneUsesContour(true);
			this.SceneLayer.SceneView.SetAcceptGlobalDebugRenderObjects(true);
			this.SceneLayer.SceneView.SetResolutionScaling(true);
			this.CollectTickableMapMeshes();
			this.MapNotificationView = this.AddMapView<MapNotificationView>(Array.Empty<object>()) as MapNotificationView;
			this.AddMapView<MapBasicView>(Array.Empty<object>());
			this.AddMapView<MapPartyNameplateView>(Array.Empty<object>());
			this.AddMapView<MapSettlementNameplateView>(Array.Empty<object>());
			this.AddMapView<MapEventVisualsView>(Array.Empty<object>());
			this.AddMapView<MapTrackersView>(Array.Empty<object>());
			this.AddMapView<MapSaveView>(Array.Empty<object>());
			this.AddMapView<MapGamepadEffectsView>(Array.Empty<object>());
			this.AddMapView<MapCameraFadeView>(Array.Empty<object>());
			this.EncyclopediaScreenManager = this.AddMapView<MapEncyclopediaView>(Array.Empty<object>()) as MapEncyclopediaView;
			this._mapReadyView = this.AddMapView<MapReadyView>(Array.Empty<object>()) as MapReadyView;
			this._mapReadyView.SetIsMapSceneReady(false);
			this._mouseRay = new Ray(Vec3.Zero, Vec3.Up, float.MaxValue);
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				if (this != null)
				{
					((IMapStateHandler)this).OnPlayerSiegeActivated();
				}
			}
			this.PrefabEntityCache = this.SceneLayer.SceneView.GetScene().GetFirstEntityWithScriptComponent<CampaignMapSiegePrefabEntityCache>().GetFirstScriptOfType<CampaignMapSiegePrefabEntityCache>();
			CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, new Action<bool, string>(this.OnSaveOver));
			CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnMarriageOfferedToPlayer));
			CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnMarriageOfferCanceled));
			CampaignEvents.OnHeirSelectionRequestedEvent.AddNonSerializedListener(this, new Action<Dictionary<Hero, int>>(this.OnHeirSelectionRequested));
			CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeirSelectionOver));
			Game.Current.EventManager.RegisterEvent<TutorialContextChangedEvent>(new Action<TutorialContextChangedEvent>(this.OnTutorialContextChanged));
			GameEntity firstEntityWithScriptComponent = this.MapScene.GetFirstEntityWithScriptComponent<MapColorGradeManager>();
			if (firstEntityWithScriptComponent != null)
			{
				this._colorGradeManager = firstEntityWithScriptComponent.GetFirstScriptOfType<MapColorGradeManager>();
			}
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0001907C File Offset: 0x0001727C
		private void OnSaveOver(bool isSuccessful, string newSaveGameName)
		{
			if (this._exitOnSaveOver)
			{
				if (isSuccessful)
				{
					this.OnExit();
				}
				this._exitOnSaveOver = false;
			}
		}

		// Token: 0x0600030A RID: 778 RVA: 0x00019096 File Offset: 0x00017296
		private void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
		{
			this._marriageOfferPopupView = this.AddMapView<MarriageOfferPopupView>(new object[] { suitor, maiden });
		}

		// Token: 0x0600030B RID: 779 RVA: 0x000190B2 File Offset: 0x000172B2
		public void CloseMarriageOfferPopup()
		{
			if (this._marriageOfferPopupView != null)
			{
				this.RemoveMapView(this._marriageOfferPopupView);
				this._marriageOfferPopupView = null;
			}
		}

		// Token: 0x0600030C RID: 780 RVA: 0x000190D0 File Offset: 0x000172D0
		protected override void OnFinalize()
		{
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnFinalize();
			});
			List<EntityVisualManagerBase> components = SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase>();
			for (int i = components.Count - 1; i >= 0; i--)
			{
				SandBoxViewSubModule.SandBoxViewVisualManager.Finalize<EntityVisualManagerBase>(components[i]);
			}
			base.OnFinalize();
			if (this.MapScene != null)
			{
				this.MapScene.ClearAll();
			}
			Common.MemoryCleanupGC(false);
			this.CharacterBannerMaterialCache.Clear();
			ViewSubModule.BannerTexturedMaterialCache = null;
			MBMusicManager.Current.DeactivateCampaignMode();
			MBMusicManager.Current.OnCampaignMusicHandlerFinalize();
			CampaignEvents.OnSaveOverEvent.ClearListeners(this);
			CampaignEvents.OnMarriageOfferedToPlayerEvent.ClearListeners(this);
			CampaignEvents.OnMarriageOfferCanceledEvent.ClearListeners(this);
			Game.Current.EventManager.UnregisterEvent<TutorialContextChangedEvent>(new Action<TutorialContextChangedEvent>(this.OnTutorialContextChanged));
			BannerPersistentTextureCache bannerPersistentTextureCache = BannerPersistentTextureCache.Current;
			if (bannerPersistentTextureCache != null)
			{
				bannerPersistentTextureCache.FlushCache();
			}
			this.MapScene = null;
			this.MapCameraView = null;
			MapScreen.Instance = null;
		}

		// Token: 0x0600030D RID: 781 RVA: 0x000191E0 File Offset: 0x000173E0
		public void OnHourlyTick()
		{
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnHourlyTick();
			});
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			object obj;
			if (kingdom == null)
			{
				obj = null;
			}
			else
			{
				obj = kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d.NotifyPlayer && d.IsEnforced && d.IsPlayerParticipant && !d.ShouldBeCancelled());
			}
			this._isKingdomDecisionsDirty = obj != null;
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0001925A File Offset: 0x0001745A
		private void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
		{
			this.CloseMarriageOfferPopup();
		}

		// Token: 0x0600030F RID: 783 RVA: 0x00019262 File Offset: 0x00017462
		private void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
		{
			this._heirSelectionPopupView = this.AddMapView<HeirSelectionPopupView>(new object[] { heirApparents });
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0001927A File Offset: 0x0001747A
		public void BeginParleyWith(PartyBase party)
		{
			if (this.GetMapView<MapParleyAnimationView>() == null)
			{
				this.AddMapView<MapParleyAnimationView>(new object[] { party });
			}
		}

		// Token: 0x06000311 RID: 785 RVA: 0x00019295 File Offset: 0x00017495
		private void OnHeirSelectionOver(Hero selectedHeir)
		{
			if (this._heirSelectionPopupView != null)
			{
				this.RemoveMapView(this._heirSelectionPopupView);
				this._heirSelectionPopupView = null;
			}
		}

		// Token: 0x06000312 RID: 786 RVA: 0x000192B4 File Offset: 0x000174B4
		private void ShowNextKingdomDecisionPopup()
		{
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			KingdomDecision kingdomDecision;
			if (kingdom == null)
			{
				kingdomDecision = null;
			}
			else
			{
				kingdomDecision = kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d.NotifyPlayer && d.IsEnforced && d.IsPlayerParticipant && !d.ShouldBeCancelled());
			}
			KingdomDecision kingdomDecision2 = kingdomDecision;
			if (kingdomDecision2 != null)
			{
				InquiryData data = new InquiryData(new TextObject("{=A7349NHy}Critical Kingdom Decision", null).ToString(), kingdomDecision2.GetChooseTitle().ToString(), true, false, new TextObject("{=bFzZwwjT}Examine", null).ToString(), "", delegate()
				{
					this.OpenKingdom();
				}, null, "", 0f, null, null, null);
				kingdomDecision2.NotifyPlayer = false;
				InformationManager.ShowInquiry(data, true, false);
				this._isKingdomDecisionsDirty = false;
				return;
			}
			Debug.FailedAssert("There is no dirty decision but still demanded one", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "ShowNextKingdomDecisionPopup", 824);
		}

		// Token: 0x06000313 RID: 787 RVA: 0x00019380 File Offset: 0x00017580
		void IMapStateHandler.OnMenuModeTick(float dt)
		{
			this.UpdateTutorialContext();
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnMenuModeTick(dt);
			});
		}

		// Token: 0x06000314 RID: 788 RVA: 0x000193B8 File Offset: 0x000175B8
		private void HandleIfBlockerStatesDisabled()
		{
			bool isReadyForRender = this._isReadyForRender;
			bool flag = this.SceneLayer.SceneView.ReadyToRender() && this.SceneLayer.SceneView.CheckSceneReadyToRender();
			bool flag2 = (this._isSceneViewEnabled && flag) || this._conversationView.IsConversationActive;
			if (flag2)
			{
				if (LoadingWindow.IsLoadingWindowActive)
				{
					if (this._sceneReadyFrameCounter == 3)
					{
						LoadingWindow.DisableGlobalLoadingWindow();
						this._sceneReadyFrameCounter = 0;
					}
					else
					{
						this._sceneReadyFrameCounter++;
					}
				}
			}
			else if (!flag && !LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.EnableGlobalLoadingWindow();
			}
			if (flag)
			{
				this._mapReadyView.SetIsMapSceneReady(flag2);
				this._isReadyForRender = flag2;
			}
		}

		// Token: 0x06000315 RID: 789 RVA: 0x00019464 File Offset: 0x00017664
		private void UpdateTutorialContext()
		{
			if (base.IsActive)
			{
				TutorialContexts tutorialContexts = TutorialContexts.MapWindow;
				if (this.IsInMenu)
				{
					for (int i = this._menuViewContext.MenuViews.Count - 1; i >= 0; i--)
					{
						TutorialContexts tutorialContext = this._menuViewContext.MenuViews[i].GetTutorialContext();
						if (tutorialContext != TutorialContexts.MapWindow)
						{
							tutorialContexts = tutorialContext;
							break;
						}
					}
				}
				if (tutorialContexts == TutorialContexts.MapWindow)
				{
					tutorialContexts = this._mapViewsContainer.GetContextToChangeTo();
				}
				if (this._currentTutorialContext != tutorialContexts)
				{
					Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(tutorialContexts));
				}
			}
		}

		// Token: 0x06000316 RID: 790 RVA: 0x000194F0 File Offset: 0x000176F0
		private void CheckCursorState()
		{
			Vec3 zero = Vec3.Zero;
			Vec3 zero2 = Vec3.Zero;
			this.SceneLayer.SceneView.TranslateMouse(ref zero, ref zero2, -1f);
			PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
			float num;
			Vec3 vec;
			bool isOnLand;
			this.GetCursorIntersectionPoint(ref zero, ref zero2, out num, out vec, ref nullFaceRecord, out isOnLand, BodyFlags.CommonFocusRayCastExcludeFlags);
			MobileParty.NavigationType navigationType;
			this.SceneLayer.ActiveCursor = (NavigationHelper.CanPlayerNavigateToPosition(new CampaignVec2(vec.AsVec2, isOnLand), out navigationType) ? CursorType.Default : CursorType.Disabled);
		}

		// Token: 0x06000317 RID: 791 RVA: 0x00019568 File Offset: 0x00017768
		private void HandleIfSceneIsReady()
		{
			int num = Utilities.EngineFrameNo - this._activatedFrameNo;
			bool flag = this._isSceneViewEnabled;
			if (num < 5)
			{
				flag = false;
				MapColorGradeManager colorGradeManager = this._colorGradeManager;
				if (colorGradeManager != null)
				{
					colorGradeManager.ApplyAtmosphere(true);
				}
			}
			else
			{
				int isConversationActive = (this._conversationView.IsConversationActive ? 1 : 0);
				bool flag2 = ScreenManager.TopScreen == this;
				flag = isConversationActive == 0 && flag2;
			}
			if (flag != this._isSceneViewEnabled)
			{
				this._isSceneViewEnabled = flag;
				this.SceneLayer.SceneView.SetEnable(this._isSceneViewEnabled);
				if (this._isSceneViewEnabled)
				{
					this.MapScene.CheckResources(false);
					if (this.MapScene.SceneHadWaterWakeRenderer())
					{
						this.MapScene.EnsureWaterWakeRenderer();
						this.MapScene.SetWaterWakeWorldSize(128f, 0.994f);
						this.MapScene.SetWaterWakeCameraOffset(8f);
					}
					this._sceneReadyFrameCounter = 0;
					if (this._focusLost && !this.IsEscapeMenuOpened)
					{
						this.OnFocusChangeOnGameWindow(false);
					}
				}
			}
			this.HandleIfBlockerStatesDisabled();
		}

		// Token: 0x06000318 RID: 792 RVA: 0x00019659 File Offset: 0x00017859
		void IMapStateHandler.StartCameraAnimation(CampaignVec2 targetPosition, float animationStopDuration)
		{
			this.MapCameraView.StartCameraAnimation(targetPosition, animationStopDuration);
		}

		// Token: 0x06000319 RID: 793 RVA: 0x00019668 File Offset: 0x00017868
		private void OnTutorialContextChanged(TutorialContextChangedEvent evnt)
		{
			this._currentTutorialContext = evnt.NewContext;
		}

		// Token: 0x0600031A RID: 794 RVA: 0x00019678 File Offset: 0x00017878
		void IMapStateHandler.BeforeTick(float dt)
		{
			this.UpdateTutorialContext();
			this.HandleIfSceneIsReady();
			bool flag = MobileParty.MainParty != null && PartyBase.MainParty.IsValid;
			if (flag && !this.MapCameraView.CameraAnimationInProgress)
			{
				if (!this.IsInMenu && this.SceneLayer.Input.IsHotKeyPressed("MapChangeCursorMode"))
				{
					this._mapSceneCursorWanted = !this._mapSceneCursorWanted;
				}
				if (this.SceneLayer.Input.IsHotKeyPressed("MapClick"))
				{
					this._secondLastPressTime = this._lastPressTime;
					this._lastPressTime = (double)Time.ApplicationTime;
				}
				this._leftButtonDoubleClickOnSceneWidget = false;
				if (this.SceneLayer.Input.IsHotKeyReleased("MapClick"))
				{
					Vec2 mousePositionPixel = this.SceneLayer.Input.GetMousePositionPixel();
					float applicationTime = Time.ApplicationTime;
					this._leftButtonDoubleClickOnSceneWidget = (double)applicationTime - this._lastReleaseTime < 0.30000001192092896 && (double)applicationTime - this._secondLastPressTime < 0.44999998807907104 && mousePositionPixel.Distance(this._oldMousePosition) < 10f;
					if (this._leftButtonDoubleClickOnSceneWidget)
					{
						this._waitForDoubleClickUntilTime = 0f;
					}
					this._oldMousePosition = this.SceneLayer.Input.GetMousePositionPixel();
					this._lastReleaseTime = (double)applicationTime;
				}
				if (this.IsReady)
				{
					this.HandleMouse(dt);
				}
			}
			this.MapSceneCursorActive = !this.SceneLayer.Input.GetIsMouseActive() && !this.IsInMenu && ScreenManager.FocusedLayer == this.SceneLayer && this._mapSceneCursorWanted;
			float deltaMouseScroll = this.SceneLayer.Input.GetDeltaMouseScroll();
			Vec3 zero = Vec3.Zero;
			Vec3 zero2 = Vec3.Zero;
			this.SceneLayer.SceneView.TranslateMouse(ref zero, ref zero2, -1f);
			float gameKeyAxis = this.SceneLayer.Input.GetGameKeyAxis("CameraAxisX");
			float num;
			Vec3 projectedPosition;
			bool rayCastForClosestEntityOrTerrainCondition = this.MapScene.RayCastForClosestEntityOrTerrain(zero, zero2, out num, out projectedPosition, 0.01f, BodyFlags.CameraCollisionRayCastExludeFlags);
			float rx = 0f;
			float ry = 0f;
			float num2 = 1f;
			bool flag2 = !TaleWorlds.InputSystem.Input.IsGamepadActive && !this.IsInMenu && ScreenManager.FocusedLayer == this.SceneLayer;
			bool flag3 = TaleWorlds.InputSystem.Input.IsGamepadActive && this.MapSceneCursorActive;
			if (flag2 || flag3)
			{
				if (this.SceneLayer.Input.IsGameKeyDown(55))
				{
					num2 = this.MapCameraView.CameraFastMoveMultiplier;
				}
				rx = this.SceneLayer.Input.GetGameKeyAxis("MapMovementAxisX") * num2;
				ry = this.SceneLayer.Input.GetGameKeyAxis("MapMovementAxisY") * num2;
			}
			this._ignoreLeftMouseRelease = false;
			MapScreen.MouseInputState mouseInputState = this.GetMouseInputState();
			if (mouseInputState.IsLeftMousePressed)
			{
				this._clickedPositionPixel = this.SceneLayer.Input.GetMousePositionPixel();
				this.MapScene.RayCastForClosestEntityOrTerrain(this._mouseRay.Origin, this._mouseRay.EndPoint, out num, out this._clickedPosition, 0.01f, BodyFlags.CameraCollisionRayCastExludeFlags);
				if (this.CurrentVisualOfTooltip != null)
				{
					this.RemoveMapTooltip();
				}
				this._leftButtonDraggingMode = false;
			}
			else if (mouseInputState.IsLeftMouseDown && !mouseInputState.IsLeftMouseReleased && (this.SceneLayer.Input.GetMousePositionPixel().DistanceSquared(this._clickedPositionPixel) > 300f || this._leftButtonDraggingMode) && !this.IsInMenu)
			{
				this._leftButtonDraggingMode = true;
			}
			else if (this._leftButtonDraggingMode)
			{
				this._leftButtonDraggingMode = false;
				this._ignoreLeftMouseRelease = true;
			}
			if (mouseInputState.IsMiddleMouseDown)
			{
				MBWindowManager.DontChangeCursorPos();
			}
			if (mouseInputState.IsLeftMouseReleased)
			{
				this._clickedPositionPixel = this.SceneLayer.Input.GetMousePositionPixel();
			}
			MapCameraView.InputInformation inputInformation;
			inputInformation.IsMainPartyValid = flag;
			inputInformation.IsMapReady = this.IsReady;
			inputInformation.IsControlDown = this.SceneLayer.Input.IsControlDown();
			inputInformation.IsMouseActive = this.SceneLayer.Input.GetIsMouseActive();
			inputInformation.CheatModeEnabled = Game.Current.CheatMode;
			inputInformation.DeltaMouseScroll = deltaMouseScroll;
			inputInformation.LeftMouseButtonPressed = mouseInputState.IsLeftMousePressed;
			inputInformation.LeftMouseButtonDown = mouseInputState.IsLeftMouseDown;
			inputInformation.LeftMouseButtonReleased = mouseInputState.IsLeftMouseReleased;
			inputInformation.MiddleMouseButtonDown = mouseInputState.IsMiddleMouseDown;
			inputInformation.RightMouseButtonDown = mouseInputState.IsRightMouseDown;
			inputInformation.RotateLeftKeyDown = this.SceneLayer.Input.IsGameKeyDown(58);
			inputInformation.RotateRightKeyDown = this.SceneLayer.Input.IsGameKeyDown(59);
			inputInformation.PartyMoveUpKey = this.SceneLayer.Input.IsGameKeyDown(50);
			inputInformation.PartyMoveDownKey = this.SceneLayer.Input.IsGameKeyDown(51);
			inputInformation.PartyMoveLeftKey = this.SceneLayer.Input.IsGameKeyDown(53);
			inputInformation.PartyMoveRightKey = this.SceneLayer.Input.IsGameKeyDown(52);
			inputInformation.MapZoomIn = this.SceneLayer.Input.GetGameKeyState(56);
			inputInformation.MapZoomOut = this.SceneLayer.Input.GetGameKeyState(57);
			inputInformation.CameraFollowModeKeyPressed = this.SceneLayer.Input.IsGameKeyPressed(64);
			inputInformation.MousePositionPixel = this.SceneLayer.Input.GetMousePositionPixel();
			inputInformation.ClickedPositionPixel = this._clickedPositionPixel;
			inputInformation.ClickedPosition = this._clickedPosition;
			inputInformation.LeftButtonDraggingMode = this._leftButtonDraggingMode;
			inputInformation.IsInMenu = this.IsInMenu;
			inputInformation.WorldMouseNear = zero;
			inputInformation.WorldMouseFar = zero2;
			inputInformation.MouseSensitivity = this.SceneLayer.Input.GetMouseSensitivity();
			inputInformation.MouseMoveX = this.SceneLayer.Input.GetMouseMoveX();
			inputInformation.MouseMoveY = this.SceneLayer.Input.GetMouseMoveY();
			inputInformation.HorizontalCameraInput = gameKeyAxis;
			inputInformation.RayCastForClosestEntityOrTerrainCondition = rayCastForClosestEntityOrTerrainCondition;
			inputInformation.ProjectedPosition = projectedPosition;
			inputInformation.RX = rx;
			inputInformation.RY = ry;
			inputInformation.RS = num2;
			inputInformation.Dt = dt;
			this.MapCameraView.OnBeforeTick(inputInformation);
			this.MapCursor.SetVisible(this.MapSceneCursorActive);
			if (flag && !Campaign.Current.TimeControlModeLock)
			{
				if (this.MapState.AtMenu)
				{
					if (Campaign.Current.CurrentMenuContext == null)
					{
						goto IL_90E;
					}
					GameMenu gameMenu = Campaign.Current.CurrentMenuContext.GameMenu;
					if (gameMenu == null || !gameMenu.IsWaitActive)
					{
						goto IL_90E;
					}
				}
				float applicationTime2 = Time.ApplicationTime;
				if (this.SceneLayer.Input.IsGameKeyPressed(63) && this._timeToggleTimer == 3.4028235E+38f)
				{
					this._timeToggleTimer = applicationTime2;
				}
				if (this.SceneLayer.Input.IsGameKeyPressed(63) && applicationTime2 - this._timeToggleTimer > 0.4f)
				{
					if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppablePlay)
					{
						Campaign.Current.SetTimeSpeed(2);
					}
					else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward)
					{
						Campaign.Current.SetTimeSpeed(1);
					}
					else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
					{
						Campaign.Current.SetTimeSpeed(1);
					}
					else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.FastForwardStop)
					{
						Campaign.Current.SetTimeSpeed(2);
					}
					this._timeToggleTimer = float.MaxValue;
					this._ignoreNextTimeToggle = true;
				}
				else if (this.SceneLayer.Input.IsGameKeyPressed(63))
				{
					if (this._ignoreNextTimeToggle)
					{
						this._ignoreNextTimeToggle = false;
					}
					else
					{
						this._waitForDoubleClickUntilTime = 0f;
						if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppablePlay || ((Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay) && !Campaign.Current.IsMainPartyWaiting))
						{
							Campaign.Current.SetTimeSpeed(0);
						}
						else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay)
						{
							Campaign.Current.SetTimeSpeed(1);
						}
						else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.FastForwardStop || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
						{
							Campaign.Current.SetTimeSpeed(2);
						}
					}
					this._timeToggleTimer = float.MaxValue;
				}
				else if (this.SceneLayer.Input.IsGameKeyPressed(60))
				{
					this._waitForDoubleClickUntilTime = 0f;
					Campaign.Current.SetTimeSpeed(0);
				}
				else if (this.SceneLayer.Input.IsGameKeyPressed(61))
				{
					this._waitForDoubleClickUntilTime = 0f;
					Campaign.Current.SetTimeSpeed(1);
				}
				else if (this.SceneLayer.Input.IsGameKeyPressed(62))
				{
					this._waitForDoubleClickUntilTime = 0f;
					Campaign.Current.SetTimeSpeed(2);
				}
				else if (this.SceneLayer.Input.IsGameKeyPressed(65))
				{
					if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
					{
						Campaign.Current.SetTimeSpeed(0);
					}
					else
					{
						Campaign.Current.SetTimeSpeed(2);
					}
				}
			}
			IL_90E:
			if (!flag && this.CurrentVisualOfTooltip != null)
			{
				this.RemoveMapTooltip();
				this.CurrentVisualOfTooltip = null;
			}
			this.SetCameraOfSceneLayer();
			if (!this.SceneLayer.Input.GetIsMouseActive() && Campaign.Current.GameStarted)
			{
				this.MapCursor.BeforeTick(dt);
			}
		}

		// Token: 0x0600031B RID: 795 RVA: 0x00019FDC File Offset: 0x000181DC
		void IMapStateHandler.Tick(float dt)
		{
			if (!this.IsInMenu)
			{
				if (this._isKingdomDecisionsDirty)
				{
					this.ShowNextKingdomDecisionPopup();
				}
				else
				{
					if (ViewModel.UIDebugMode && base.DebugInput.IsHotKeyDown("UIExtendedDebugKey") && base.DebugInput.IsHotKeyPressed("MapScreenHotkeyOpenEncyclopedia"))
					{
						this.OpenEncyclopedia();
					}
					bool cheatMode = Game.Current.CheatMode;
					if (cheatMode && base.DebugInput.IsHotKeyPressed("MapScreenHotkeySwitchCampaignTrueSight"))
					{
						Campaign.Current.TrueSight = !Campaign.Current.TrueSight;
					}
					if (cheatMode)
					{
						base.DebugInput.IsHotKeyPressed("MapScreenPrintMultiLineText");
					}
					this._mapViewsContainer.ForeachReverse(delegate(MapView view)
					{
						view.OnFrameTick(dt);
					});
				}
			}
			SandBoxViewVisualManager.OnTick(dt, Campaign.Current.CampaignDt);
		}

		// Token: 0x0600031C RID: 796 RVA: 0x0001A0BC File Offset: 0x000182BC
		void IMapStateHandler.OnIdleTick(float dt)
		{
			this.UpdateTutorialContext();
			this.HandleIfSceneIsReady();
			this.RemoveMapTooltip();
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnIdleTick(dt);
			});
		}

		// Token: 0x0600031D RID: 797 RVA: 0x0001A100 File Offset: 0x00018300
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			MBDebug.SetErrorReportScene(this.MapScene);
			this.UpdateMenuView();
			TextObject textObject;
			if (this.IsInMenu)
			{
				this._menuViewContext.OnFrameTick(dt);
				if (this.SceneLayer.Input.IsGameKeyPressed(4))
				{
					GameMenuOption leaveMenuOption = Campaign.Current.GameMenuManager.GetLeaveMenuOption(this._menuViewContext.MenuContext);
					if (leaveMenuOption != null)
					{
						UISoundsHelper.PlayUISound("event:/ui/default");
						if (this._menuViewContext.MenuContext.GameMenu.IsWaitMenu)
						{
							this._menuViewContext.MenuContext.GameMenu.EndWait();
						}
						leaveMenuOption.RunConsequence(this._menuViewContext.MenuContext);
					}
				}
			}
			else if (Campaign.Current != null && !this.IsInBattleSimulation && !this.IsInArmyManagement && !this.IsMarriageOfferPopupActive && !this.IsHeirSelectionPopupActive && !this.IsMapCheatsActive && !this.IsMapIncidentActive && !this.IsOverlayContextMenuEnabled && !this.EncyclopediaScreenManager.IsEncyclopediaOpen && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				Kingdom kingdom = Clan.PlayerClan.Kingdom;
				bool flag;
				if (kingdom == null)
				{
					flag = null != null;
				}
				else
				{
					MBReadOnlyList<KingdomDecision> unresolvedDecisions = kingdom.UnresolvedDecisions;
					if (unresolvedDecisions == null)
					{
						flag = null != null;
					}
					else
					{
						flag = unresolvedDecisions.FirstOrDefault((KingdomDecision d) => d.NeedsPlayerResolution && !d.ShouldBeCancelled()) != null;
					}
				}
				if (flag)
				{
					this.OpenKingdom();
				}
			}
			if (this._partyIconNeedsRefreshing)
			{
				this._partyIconNeedsRefreshing = false;
				PartyBase.MainParty.SetVisualAsDirty();
			}
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnMapScreenUpdate(dt);
			});
			SandBoxViewVisualManager.OnFrameTick(Campaign.Current.CampaignDt);
		}

		// Token: 0x0600031E RID: 798 RVA: 0x0001A2BC File Offset: 0x000184BC
		protected override void OnPostFrameTick(float dt)
		{
			base.OnPostFrameTick(dt);
			if (Campaign.Current.CurrentTickCount != this._mapScreenTickCount)
			{
				ITask campaignLateAITickTask = Campaign.Current.CampaignLateAITickTask;
				if (campaignLateAITickTask != null)
				{
					campaignLateAITickTask.Invoke();
				}
				this._mapScreenTickCount = Campaign.Current.CurrentTickCount;
			}
		}

		// Token: 0x0600031F RID: 799 RVA: 0x0001A2FC File Offset: 0x000184FC
		private void UpdateMenuView()
		{
			if (this._latestMenuContext == null && this.IsInMenu)
			{
				this.ExitMenuContext();
				return;
			}
			if ((!this.IsInMenu && this._latestMenuContext != null) || (this.IsInMenu && this._menuViewContext.MenuContext != this._latestMenuContext))
			{
				this.EnterMenuContext(this._latestMenuContext);
			}
		}

		// Token: 0x06000320 RID: 800 RVA: 0x0001A358 File Offset: 0x00018558
		private void EnterMenuContext(MenuContext menuContext)
		{
			if (!Hero.MainHero.IsPrisoner)
			{
				this.MapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
				Campaign.Current.CameraFollowParty = PartyBase.MainParty;
			}
			if (!this.IsInMenu)
			{
				this._menuViewContext = new MenuViewContext(this, menuContext);
			}
			else
			{
				this._menuViewContext.UpdateMenuContext(menuContext);
			}
			this._menuViewContext.OnInitialize();
			this._menuViewContext.OnActivate();
			if (this._conversationView.IsConversationActive)
			{
				this._menuViewContext.OnMapConversationActivated();
			}
		}

		// Token: 0x06000321 RID: 801 RVA: 0x0001A3DD File Offset: 0x000185DD
		private void ExitMenuContext()
		{
			this._menuViewContext.OnGameStateDeactivate();
			this._menuViewContext.OnDeactivate();
			this._menuViewContext.OnFinalize();
			this._menuViewContext = null;
		}

		// Token: 0x06000322 RID: 802 RVA: 0x0001A407 File Offset: 0x00018607
		private void OpenBannerEditorScreen()
		{
			if (Campaign.Current.IsBannerEditorEnabled)
			{
				this._partyIconNeedsRefreshing = true;
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>(), 0);
			}
		}

		// Token: 0x06000323 RID: 803 RVA: 0x0001A43C File Offset: 0x0001863C
		private void OpenFaceGeneratorScreen()
		{
			if (Campaign.Current.IsFaceGenEnabled)
			{
				IFaceGeneratorCustomFilter faceGeneratorFilter = CharacterHelper.GetFaceGeneratorFilter();
				BarberState gameState = Game.Current.GameStateManager.CreateState<BarberState>(new object[]
				{
					Hero.MainHero.CharacterObject,
					faceGeneratorFilter
				});
				GameStateManager.Current.PushState(gameState, 0);
			}
		}

		// Token: 0x06000324 RID: 804 RVA: 0x0001A48E File Offset: 0x0001868E
		public void OnExit()
		{
			this.MapCameraView.OnExit();
			MBGameManager.EndGame();
		}

		// Token: 0x06000325 RID: 805 RVA: 0x0001A4A0 File Offset: 0x000186A0
		private void OnEscapeMenuToggled(bool isOpened = false)
		{
			this.MapCameraView.OnEscapeMenuToggled(isOpened);
			if (this.IsEscapeMenuOpened == isOpened)
			{
				return;
			}
			this.IsEscapeMenuOpened = isOpened;
			if (isOpened)
			{
				List<EscapeMenuItemVM> escapeMenuItems = this.GetEscapeMenuItems();
				Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
				this._escapeMenuView = this.AddMapView<MapEscapeMenuView>(new object[] { escapeMenuItems });
				return;
			}
			this.RemoveMapView(this._escapeMenuView);
			this._escapeMenuView = null;
			Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		}

		// Token: 0x06000326 RID: 806 RVA: 0x0001A520 File Offset: 0x00018720
		private void CheckValidityOfItems()
		{
			foreach (ItemObject itemObject in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
			{
				if (itemObject.IsUsingTeamColor)
				{
					MetaMesh copy = MetaMesh.GetCopy(itemObject.MultiMeshName, false, false);
					for (int i = 0; i < copy.MeshCount; i++)
					{
						Material material = copy.GetMeshAtIndex(i).GetMaterial();
						if (material.Name != "vertex_color_lighting_skinned" && material.Name != "vertex_color_lighting" && material.GetTexture(Material.MBTextureType.DiffuseMap2) == null)
						{
							MBDebug.ShowWarning("Item object(" + itemObject.Name + ") has 'Using Team Color' flag but does not have a mask texture in diffuse2 slot. ");
							break;
						}
					}
				}
			}
		}

		// Token: 0x06000327 RID: 807 RVA: 0x0001A600 File Offset: 0x00018800
		public void GetCursorIntersectionPoint(ref Vec3 clippedMouseNear, ref Vec3 clippedMouseFar, out float closestDistanceSquared, out Vec3 intersectionPoint, ref PathFaceRecord currentFace, out bool isOnland, BodyFlags excludedBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			(clippedMouseFar - clippedMouseNear).Normalize();
			Vec3 vec = clippedMouseFar - clippedMouseNear;
			float maxDistance = vec.Normalize();
			this._mouseRay.Reset(clippedMouseNear, vec, maxDistance);
			intersectionPoint = Vec3.Zero;
			closestDistanceSquared = 1E+12f;
			float num;
			Vec3 vec2;
			if (this.SceneLayer.SceneView.RayCastForClosestEntityOrTerrain(clippedMouseNear, clippedMouseFar, out num, out vec2, 0.01f, excludedBodyFlags))
			{
				closestDistanceSquared = num * num;
				intersectionPoint = clippedMouseNear + vec * num;
			}
			currentFace = new CampaignVec2(intersectionPoint.AsVec2, true).Face;
			isOnland = true;
			if (!currentFace.IsValid())
			{
				currentFace = new CampaignVec2(intersectionPoint.AsVec2, false).Face;
				isOnland = false;
			}
		}

		// Token: 0x06000328 RID: 808 RVA: 0x0001A6F9 File Offset: 0x000188F9
		public void FastMoveCameraToPosition(CampaignVec2 target)
		{
			this.MapCameraView.FastMoveCameraToPosition(target, this.IsInMenu);
		}

		// Token: 0x06000329 RID: 809 RVA: 0x0001A710 File Offset: 0x00018910
		private void HandleMouse(float dt)
		{
			if (Campaign.Current.GameStarted)
			{
				Vec3 zero = Vec3.Zero;
				Vec3 zero2 = Vec3.Zero;
				this.SceneLayer.SceneView.TranslateMouse(ref zero, ref zero2, -1f);
				Vec3 vec = zero;
				Vec3 vec2 = zero2;
				PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
				float num;
				Vec3 vec3;
				bool isOnLand;
				this.GetCursorIntersectionPoint(ref vec, ref vec2, out num, out vec3, ref nullFaceRecord, out isOnLand, BodyFlags.CommonFocusRayCastExcludeFlags);
				Vec3 terrainIntersectionPoint;
				bool flag;
				this.GetCursorIntersectionPoint(ref vec, ref vec2, out num, out terrainIntersectionPoint, ref nullFaceRecord, out flag, BodyFlags.Disabled | BodyFlags.Moveable | BodyFlags.AILimiter | BodyFlags.Barrier | BodyFlags.Barrier3D | BodyFlags.Ragdoll | BodyFlags.RagdollLimiter | BodyFlags.DoNotCollideWithRaycast);
				int num2 = this.MapScene.SelectEntitiesCollidedWith(ref this._mouseRay, this._intersectionInfos, this._intersectedEntityIDs);
				MapEntityVisual mapEntityVisual = null;
				MapEntityVisual mapEntityVisual2 = null;
				MBList<CampaignEntityVisualComponent> components = SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents();
				int num3 = 0;
				while (num3 < components.Count && !components[num3].OnVisualIntersected(this._mouseRay, this._intersectedEntityIDs, this._intersectionInfos, num2, zero, zero2, terrainIntersectionPoint, ref mapEntityVisual, ref mapEntityVisual2))
				{
					num3++;
				}
				Array.Clear(this._intersectedEntityIDs, 0, num2);
				Array.Clear(this._intersectionInfos, 0, num2);
				if (mapEntityVisual != null && !mapEntityVisual.IsMobileEntity)
				{
					this.SceneLayer.ActiveCursor = CursorType.Default;
				}
				else
				{
					this.CheckCursorState();
				}
				float gameKeyAxis = this.SceneLayer.Input.GetGameKeyAxis("CameraAxisY");
				bool flag2 = this.SceneLayer.IsHitThisFrame && this.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton);
				this.MapCameraView.HandleMouse(flag2, gameKeyAxis, this.SceneLayer.Input.GetMouseMoveY(), dt);
				if (flag2)
				{
					MBWindowManager.DontChangeCursorPos();
				}
				if (ScreenManager.FirstHitLayer == this.SceneLayer && this.SceneLayer.Input.IsHotKeyReleased("MapClick") && !this._leftButtonDraggingMode && !this._ignoreLeftMouseRelease)
				{
					CampaignVec2 intersectionPoint = new CampaignVec2(terrainIntersectionPoint.AsVec2, isOnLand);
					this.HandleLeftMouseButtonClick(this._leftButtonDoubleClickOnSceneWidget ? this._preVisualOfSelectedEntity : mapEntityVisual2, intersectionPoint, nullFaceRecord, this._leftButtonDoubleClickOnSceneWidget);
					this._preVisualOfSelectedEntity = mapEntityVisual2;
				}
				if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward && this._waitForDoubleClickUntilTime > 0f && this._waitForDoubleClickUntilTime < Time.ApplicationTime)
				{
					Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
					this._waitForDoubleClickUntilTime = 0f;
				}
				if (ScreenManager.FirstHitLayer == this.SceneLayer)
				{
					if (mapEntityVisual != null)
					{
						if (this.CurrentVisualOfTooltip != mapEntityVisual)
						{
							this.RemoveMapTooltip();
						}
						if (this.SceneLayer.Input.IsGameKeyPressed(67))
						{
							mapEntityVisual.OnOpenEncyclopedia();
							this.MapCursor.SetVisible(false);
						}
						if (this.SceneLayer.Input.IsGameKeyPressed(66))
						{
							mapEntityVisual.OnTrackAction();
						}
						this.OnHoverMapEntity(mapEntityVisual);
						this.CurrentVisualOfTooltip = mapEntityVisual;
						return;
					}
					if (!this.TooltipHandlingDisabled)
					{
						this.RemoveMapTooltip();
						this.CurrentVisualOfTooltip = null;
						return;
					}
				}
				else
				{
					this.RemoveMapTooltip();
					this.CurrentVisualOfTooltip = null;
				}
			}
		}

		// Token: 0x0600032A RID: 810 RVA: 0x0001A9E4 File Offset: 0x00018BE4
		private MapScreen.MouseInputState GetMouseInputState()
		{
			if (!this.SceneLayer.IsHitThisFrame)
			{
				return default(MapScreen.MouseInputState);
			}
			return new MapScreen.MouseInputState
			{
				IsLeftMousePressed = this.SceneLayer.Input.IsKeyPressed(InputKey.LeftMouseButton),
				IsLeftMouseDown = this.SceneLayer.Input.IsKeyDown(InputKey.LeftMouseButton),
				IsLeftMouseReleased = this.SceneLayer.Input.IsKeyReleased(InputKey.LeftMouseButton),
				IsMiddleMousePressed = this.SceneLayer.Input.IsKeyPressed(InputKey.MiddleMouseButton),
				IsMiddleMouseDown = this.SceneLayer.Input.IsKeyDown(InputKey.MiddleMouseButton),
				IsMiddleMouseReleased = this.SceneLayer.Input.IsKeyReleased(InputKey.MiddleMouseButton),
				IsRightMousePressed = this.SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton),
				IsRightMouseDown = this.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton),
				IsRightMouseReleased = this.SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton)
			};
		}

		// Token: 0x0600032B RID: 811 RVA: 0x0001AB10 File Offset: 0x00018D10
		private void HandleLeftMouseButtonClick(MapEntityVisual visualOfSelectedEntity, CampaignVec2 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = this.Input.IsControlDown() && Game.Current.CheatMode;
			if (!this.MapState.AtMenu)
			{
				if (visualOfSelectedEntity != null)
				{
					if (visualOfSelectedEntity.IsMainEntity)
					{
						MobileParty.MainParty.SetMoveModeHold();
					}
					else
					{
						PathFaceRecord face = visualOfSelectedEntity.InteractionPositionForPlayer.Face;
						flag2 = this.MapScene.DoesPathExistBetweenFaces(face.FaceIndex, MobileParty.MainParty.CurrentNavigationFace.FaceIndex, false);
						if (flag2 && this.MapCameraView.ProcessCameraInput && PartyBase.MainParty.MapEvent == null)
						{
							flag = visualOfSelectedEntity.OnMapClick(this.SceneLayer.Input.IsHotKeyDown("MapFollowModifier"));
							if (flag)
							{
								if (!this._leftButtonDoubleClickOnSceneWidget && Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
								{
									this._waitForDoubleClickUntilTime = Time.ApplicationTime + 0.3f;
									Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
								}
								else
								{
									Campaign.Current.TimeControlMode = (this._leftButtonDoubleClickOnSceneWidget ? CampaignTimeControlMode.StoppableFastForward : CampaignTimeControlMode.StoppablePlay);
								}
								if (TaleWorlds.InputSystem.Input.IsGamepadActive)
								{
									if (visualOfSelectedEntity.IsMobileEntity)
									{
										if (visualOfSelectedEntity.IsAllyOf(PartyBase.MainParty.MapFaction))
										{
											UISoundsHelper.PlayUISound("event:/ui/campaign/click_party");
										}
										else
										{
											UISoundsHelper.PlayUISound("event:/ui/campaign/click_party_enemy");
										}
									}
									else if (visualOfSelectedEntity.IsAllyOf(PartyBase.MainParty.MapFaction))
									{
										UISoundsHelper.PlayUISound("event:/ui/campaign/click_settlement");
									}
									else
									{
										UISoundsHelper.PlayUISound("event:/ui/campaign/click_settlement_enemy");
									}
								}
							}
							MobileParty.MainParty.ForceAiNoPathMode = false;
						}
					}
				}
				else if (mouseOverFaceIndex.IsValid() || flag4)
				{
					if (!MobileParty.MainParty.IsInRaftState)
					{
						if (flag4)
						{
							MobileParty.MainParty.Position = intersectionPoint;
							MobileParty.MainParty.SetMoveModeHold();
							if (NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(intersectionPoint.ToVec2(), true), MobileParty.MainParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval) || NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(intersectionPoint.ToVec2(), false), MobileParty.MainParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval))
							{
								MobileParty.MainParty.ChangeIsCurrentlyAtSeaCheat();
							}
							if (MobileParty.MainParty.Army != null)
							{
								foreach (MobileParty mobileParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
								{
									mobileParty.Position = intersectionPoint;
								}
							}
							foreach (MobileParty mobileParty2 in MobileParty.All)
							{
								mobileParty2.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
							}
							foreach (Settlement settlement in Settlement.All)
							{
								settlement.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
							}
							MBDebug.Print(string.Concat(new object[] { "main party cheat move! - ", intersectionPoint.X, " ", intersectionPoint.Y }), 0, Debug.DebugColor.White, 17592186044416UL);
							flag2 = true;
							flag3 = true;
						}
						else
						{
							MobileParty.NavigationType navigationType;
							flag2 = NavigationHelper.CanPlayerNavigateToPosition(intersectionPoint, out navigationType);
						}
					}
					if (flag2 && this.MapCameraView.ProcessCameraInput && MobileParty.MainParty.MapEvent == null)
					{
						if (!flag3)
						{
							this.MapState.ProcessTravel(intersectionPoint);
						}
						if (!isDoubleClick && Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
						{
							this._waitForDoubleClickUntilTime = Time.ApplicationTime + 0.3f;
							Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
						}
						else
						{
							Campaign.Current.TimeControlMode = (isDoubleClick ? CampaignTimeControlMode.StoppableFastForward : CampaignTimeControlMode.StoppablePlay);
						}
					}
					this.OnTerrainClick();
				}
			}
			Vec3 intersectionPoint2 = intersectionPoint.AsVec3();
			if (!SandBoxViewVisualManager.OnMouseClick(visualOfSelectedEntity, intersectionPoint2, mouseOverFaceIndex, isDoubleClick) && !flag)
			{
				this.OnTerrainClick();
			}
			if (flag2)
			{
				this.MapCameraView.HandleLeftMouseButtonClick(this.SceneLayer.Input.GetIsMouseActive());
			}
		}

		// Token: 0x0600032C RID: 812 RVA: 0x0001AF3C File Offset: 0x0001913C
		private void OnTerrainClick()
		{
			this._mapViewsContainer.Foreach(delegate(MapView view)
			{
				view.OnMapTerrainClick();
			});
			this.MapCursor.OnMapTerrainClick();
		}

		// Token: 0x0600032D RID: 813 RVA: 0x0001AF74 File Offset: 0x00019174
		public void OnSiegeEngineFrameClick(MatrixFrame siegeFrame)
		{
			this._mapViewsContainer.Foreach(delegate(MapView view)
			{
				view.OnSiegeEngineClick(siegeFrame);
			});
		}

		// Token: 0x0600032E RID: 814 RVA: 0x0001AFA8 File Offset: 0x000191A8
		void IMapStateHandler.AfterTick(float dt)
		{
			if (ScreenManager.TopScreen == this)
			{
				this.TickVisuals(dt);
				SceneLayer sceneLayer = this.SceneLayer;
				if (sceneLayer != null && sceneLayer.Input.IsGameKeyPressed(54))
				{
					Campaign.Current.SaveHandler.QuickSaveCurrentGame();
				}
			}
			base.DebugInput.IsHotKeyPressed("MapScreenHotkeyShowPos");
		}

		// Token: 0x0600032F RID: 815 RVA: 0x0001B000 File Offset: 0x00019200
		protected virtual bool TickNavigationInput(float dt)
		{
			if (this.SceneLayer.Input.IsShiftDown() || this.SceneLayer.Input.IsControlDown())
			{
				return false;
			}
			bool flag = false;
			if (this.SceneLayer.Input.IsGameKeyPressed(38) && this._navigationHandler.GetPermission(MapNavigationItemType.Inventory).IsAuthorized)
			{
				this.OpenInventory();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(43) && this._navigationHandler.GetPermission(MapNavigationItemType.Party).IsAuthorized)
			{
				this.OpenParty();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(39) && !this.IsInArmyManagement && !this.IsMapCheatsActive && !this.IsMapIncidentActive && !this.IsOverlayContextMenuEnabled)
			{
				this.OpenEncyclopedia();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(36) && !this.IsInArmyManagement && !this.IsMarriageOfferPopupActive && !this.IsHeirSelectionPopupActive && !this.IsMapCheatsActive && !this.IsMapIncidentActive && !this.EncyclopediaScreenManager.IsEncyclopediaOpen && !this.IsOverlayContextMenuEnabled)
			{
				this.OpenBannerEditorScreen();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(40) && this._navigationHandler.GetPermission(MapNavigationItemType.Kingdom).IsAuthorized)
			{
				this.OpenKingdom();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(42) && this._navigationHandler.GetPermission(MapNavigationItemType.Quest).IsAuthorized)
			{
				this.OpenQuestsScreen();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(41) && this._navigationHandler.GetPermission(MapNavigationItemType.Clan).IsAuthorized)
			{
				this.OpenClanScreen();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(37) && this._navigationHandler.GetPermission(MapNavigationItemType.CharacterDeveloper).IsAuthorized)
			{
				this.OpenCharacterDevelopmentScreen();
				flag = true;
			}
			else if (this.SceneLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
			{
				if (!this._mapViewsContainer.IsThereAnyViewIsEscaped())
				{
					this.OpenEscapeMenu();
					flag = true;
				}
			}
			else if (this.SceneLayer.Input.IsGameKeyPressed(44))
			{
				this.OpenFaceGeneratorScreen();
				flag = true;
			}
			else if (TaleWorlds.InputSystem.Input.IsGamepadActive)
			{
				flag = this.HandleCheatMenuInput(dt);
			}
			if (flag)
			{
				this.MapCursor.SetVisible(false);
			}
			return flag;
		}

		// Token: 0x06000330 RID: 816 RVA: 0x0001B286 File Offset: 0x00019486
		void IMapStateHandler.AfterWaitTick(float dt)
		{
			this.TickNavigationInput(dt);
		}

		// Token: 0x06000331 RID: 817 RVA: 0x0001B290 File Offset: 0x00019490
		private bool HandleCheatMenuInput(float dt)
		{
			if (!this.IsMapCheatsActive && this.Input.IsKeyDown(InputKey.ControllerLBumper) && this.Input.IsKeyDown(InputKey.ControllerRTrigger) && this.Input.IsKeyDown(InputKey.ControllerLDown))
			{
				this._cheatPressTimer += dt;
				if (this._cheatPressTimer > 0.55f)
				{
					this.OpenGameplayCheats();
				}
				return true;
			}
			this._cheatPressTimer = 0f;
			return false;
		}

		// Token: 0x06000332 RID: 818 RVA: 0x0001B30C File Offset: 0x0001950C
		void IMapStateHandler.OnRefreshState()
		{
			if (Game.Current.GameStateManager.ActiveState is MapState)
			{
				if (MobileParty.MainParty.Army != null && this._armyOverlay == null)
				{
					this.AddArmyOverlay(MapScreen.MapOverlayType.Army);
					return;
				}
				if (MobileParty.MainParty.Army == null && this._armyOverlay != null)
				{
					this._mapViewsContainer.ForeachReverse(delegate(MapView view)
					{
						view.OnArmyLeft();
					});
					this._mapViewsContainer.ForeachReverse(delegate(MapView view)
					{
						view.OnDispersePlayerLeadedArmy();
					});
				}
			}
		}

		// Token: 0x06000333 RID: 819 RVA: 0x0001B3B6 File Offset: 0x000195B6
		void IMapStateHandler.OnExitingMenuMode()
		{
			this._latestMenuContext = null;
		}

		// Token: 0x06000334 RID: 820 RVA: 0x0001B3BF File Offset: 0x000195BF
		void IMapStateHandler.OnEnteringMenuMode(MenuContext menuContext)
		{
			this._latestMenuContext = menuContext;
		}

		// Token: 0x06000335 RID: 821 RVA: 0x0001B3C8 File Offset: 0x000195C8
		void IMapStateHandler.OnMainPartyEncounter()
		{
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnMainPartyEncounter();
			});
		}

		// Token: 0x06000336 RID: 822 RVA: 0x0001B3F4 File Offset: 0x000195F4
		void IMapStateHandler.OnIncidentStarted(Incident incident)
		{
			if (this.GetMapView<MapIncidentView>() == null)
			{
				this.AddMapView<MapIncidentView>(new object[] { incident });
			}
		}

		// Token: 0x06000337 RID: 823 RVA: 0x0001B40F File Offset: 0x0001960F
		void IMapStateHandler.OnSignalPeriodicEvents()
		{
			this.DeleteMarkedPeriodicEvents();
		}

		// Token: 0x06000338 RID: 824 RVA: 0x0001B417 File Offset: 0x00019617
		void IMapStateHandler.OnBattleSimulationStarted(BattleSimulation battleSimulation)
		{
			this.IsInBattleSimulation = true;
			this._battleSimulationView = this.AddMapView<BattleSimulationMapView>(new object[] { this.CreateSimulationScoreboardDatasource(battleSimulation) });
		}

		// Token: 0x06000339 RID: 825 RVA: 0x0001B43C File Offset: 0x0001963C
		protected virtual SPScoreboardVM CreateSimulationScoreboardDatasource(BattleSimulation battleSimulation)
		{
			return new SPScoreboardVM(battleSimulation);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x0001B444 File Offset: 0x00019644
		void IMapStateHandler.OnBattleSimulationEnded()
		{
			this.IsInBattleSimulation = false;
			this.RemoveMapView(this._battleSimulationView);
			this._battleSimulationView = null;
		}

		// Token: 0x0600033B RID: 827 RVA: 0x0001B460 File Offset: 0x00019660
		void IMapStateHandler.OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
		{
			this.MapCameraView.SiegeEngineClick(siegeEngineFrame);
		}

		// Token: 0x0600033C RID: 828 RVA: 0x0001B46E File Offset: 0x0001966E
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x0600033D RID: 829 RVA: 0x0001B470 File Offset: 0x00019670
		void IMapStateHandler.OnPlayerSiegeActivated()
		{
		}

		// Token: 0x0600033E RID: 830 RVA: 0x0001B472 File Offset: 0x00019672
		void IMapStateHandler.OnPlayerSiegeDeactivated()
		{
		}

		// Token: 0x0600033F RID: 831 RVA: 0x0001B474 File Offset: 0x00019674
		public void OnFadeInAndOut(float fadeOutTime, float blackTime, float fadeInTime)
		{
			this.GetMapView<MapCameraFadeView>().BeginFadeOutAndIn(fadeOutTime, blackTime, fadeInTime);
		}

		// Token: 0x06000340 RID: 832 RVA: 0x0001B484 File Offset: 0x00019684
		public void SetIsMapCheatsActive(bool isMapCheatsActive)
		{
			if (this.IsMapCheatsActive != isMapCheatsActive)
			{
				this.IsMapCheatsActive = isMapCheatsActive;
				this._cheatPressTimer = 0f;
			}
		}

		// Token: 0x06000341 RID: 833 RVA: 0x0001B4A1 File Offset: 0x000196A1
		void IMapStateHandler.OnGameplayCheatsEnabled()
		{
			this.OpenGameplayCheats();
		}

		// Token: 0x06000342 RID: 834 RVA: 0x0001B4A9 File Offset: 0x000196A9
		void IGameStateListener.OnActivate()
		{
		}

		// Token: 0x06000343 RID: 835 RVA: 0x0001B4AB File Offset: 0x000196AB
		void IGameStateListener.OnDeactivate()
		{
		}

		// Token: 0x06000344 RID: 836 RVA: 0x0001B4AD File Offset: 0x000196AD
		void IMapStateHandler.OnMapConversationStarts(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
			this.HandleMapConversationInit(playerCharacterData, conversationPartnerData);
		}

		// Token: 0x06000345 RID: 837 RVA: 0x0001B4B8 File Offset: 0x000196B8
		private void HandleMapConversationInit(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnMapConversationStart();
			});
			MenuViewContext menuViewContext = this._menuViewContext;
			if (menuViewContext != null)
			{
				menuViewContext.OnMapConversationActivated();
			}
			this._conversationView.InitializeConversation(playerCharacterData, conversationPartnerData);
			this.MapCursor.SetVisible(false);
			this.HandleIfSceneIsReady();
		}

		// Token: 0x06000346 RID: 838 RVA: 0x0001B520 File Offset: 0x00019720
		void IMapStateHandler.OnMapConversationOver()
		{
			this._mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnMapConversationOver();
			});
			MenuViewContext menuViewContext = this._menuViewContext;
			if (menuViewContext != null)
			{
				menuViewContext.OnMapConversationDeactivated();
			}
			this._conversationView.FinalizeConversation();
			this._activatedFrameNo = Utilities.EngineFrameNo;
			this.HandleIfSceneIsReady();
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0001B584 File Offset: 0x00019784
		private void InitializeVisuals()
		{
			this.InactiveLightMeshes = new List<Mesh>();
			this.ActiveLightMeshes = new List<Mesh>();
			MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
			this.MapCursor.Initialize(this);
			this._pointTargetWindDirectionDecal = MapScreen.DecalEntity.Create(mapScene.Scene, "decal_map_circle_wind", "MainPartyTargetLocationWindIndicatorDecal");
			this._pointTargetInnerDecal = MapScreen.DecalEntity.Create(mapScene.Scene, "map_circle_decal", "InnerPointTarget");
			this._pointTargetOuterDecal = MapScreen.DecalEntity.Create(mapScene.Scene, "map_circle_decal", "OuterPointTarget");
			this._partyHoverOutlineDecal = MapScreen.DecalEntity.Create(mapScene.Scene, "map_circle_decal", "MapOutlineDecal");
			this._settlementHoverOutlineDecal = MapScreen.DecalEntity.Create(mapScene.Scene, "decal_city_circle_a", "SettlementOutlineDecal");
			this._townCircleDecal = MapScreen.DecalEntity.Create(mapScene.Scene, "decal_city_circle_a", "TownCircle");
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MapTracksVisualManager>();
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MapWeatherVisualManager>();
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MapAudioManager>();
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MobilePartyVisualManager>();
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<SettlementVisualManager>();
			this.ContourMaskEntity = GameEntity.CreateEmpty(mapScene.Scene, true, true, true);
			this.ContourMaskEntity.Name = "aContourMask";
		}

		// Token: 0x06000348 RID: 840 RVA: 0x0001B6C0 File Offset: 0x000198C0
		public void SetIsInTownManagement(bool isInTownManagement)
		{
			if (this.IsInTownManagement != isInTownManagement)
			{
				this.IsInTownManagement = isInTownManagement;
			}
		}

		// Token: 0x06000349 RID: 841 RVA: 0x0001B6D2 File Offset: 0x000198D2
		public void SetIsInHideoutTroopManage(bool isInHideoutTroopManage)
		{
			if (this.IsInHideoutTroopManage != isInHideoutTroopManage)
			{
				this.IsInHideoutTroopManage = isInHideoutTroopManage;
			}
		}

		// Token: 0x0600034A RID: 842 RVA: 0x0001B6E4 File Offset: 0x000198E4
		public void SetIsInArmyManagement(bool isInArmyManagement)
		{
			if (this.IsInArmyManagement != isInArmyManagement)
			{
				this.IsInArmyManagement = isInArmyManagement;
				if (!this.IsInArmyManagement)
				{
					MenuViewContext menuViewContext = this._menuViewContext;
					if (menuViewContext == null)
					{
						return;
					}
					menuViewContext.OnResume();
				}
			}
		}

		// Token: 0x0600034B RID: 843 RVA: 0x0001B70E File Offset: 0x0001990E
		public void SetIsOverlayContextMenuActive(bool isOverlayContextMenuEnabled)
		{
			if (this.IsOverlayContextMenuEnabled != isOverlayContextMenuEnabled)
			{
				this.IsOverlayContextMenuEnabled = isOverlayContextMenuEnabled;
			}
		}

		// Token: 0x0600034C RID: 844 RVA: 0x0001B720 File Offset: 0x00019920
		public void SetIsInRecruitment(bool isInRecruitment)
		{
			if (this.IsInRecruitment != isInRecruitment)
			{
				this.IsInRecruitment = isInRecruitment;
			}
		}

		// Token: 0x0600034D RID: 845 RVA: 0x0001B732 File Offset: 0x00019932
		public void SetIsBarExtended(bool isBarExtended)
		{
			if (this.IsBarExtended != isBarExtended)
			{
				this.IsBarExtended = isBarExtended;
			}
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0001B744 File Offset: 0x00019944
		public void SetIsMarriageOfferPopupActive(bool isMarriageOfferPopupActive)
		{
			if (this.IsMarriageOfferPopupActive != isMarriageOfferPopupActive)
			{
				this.IsMarriageOfferPopupActive = isMarriageOfferPopupActive;
			}
		}

		// Token: 0x0600034F RID: 847 RVA: 0x0001B756 File Offset: 0x00019956
		public void SetIsInCampaignOptions(bool isInCampaignOptions)
		{
			if (this.IsInCampaignOptions != isInCampaignOptions)
			{
				this.IsInCampaignOptions = isInCampaignOptions;
			}
		}

		// Token: 0x06000350 RID: 848 RVA: 0x0001B768 File Offset: 0x00019968
		public void SetIsMapIncidentActive(bool isMapIncidentActive)
		{
			if (this.IsMapIncidentActive != isMapIncidentActive)
			{
				this.IsMapIncidentActive = isMapIncidentActive;
			}
		}

		// Token: 0x06000351 RID: 849 RVA: 0x0001B77C File Offset: 0x0001997C
		private void TickVisuals(float realDt)
		{
			if (MapScreen.DisableVisualTicks)
			{
				this.MapScene.ClearCurrentFrameTickEntities();
				return;
			}
			this.MapScene.TimeOfDay = CampaignTime.Now.CurrentHourInDay;
			float seasonTimeFactor;
			float num;
			Campaign.Current.Models.MapWeatherModel.GetSeasonTimeFactorOfCampaignTime(CampaignTime.Now, out seasonTimeFactor, out num, false);
			MBMapScene.SetSeasonTimeFactor(this.MapScene, seasonTimeFactor);
			MBMapScene.TickVisuals(this.MapScene, Campaign.CurrentTime % (float)CampaignTime.HoursInDay, this._tickedMapMeshes);
			if (this.IsReady)
			{
				SandBoxViewVisualManager.VisualTick(this, realDt, Campaign.Current.CampaignDt);
				this.TickStepSounds(realDt);
				this.TickCircles();
			}
			MBWindowManager.PreDisplay();
		}

		// Token: 0x06000352 RID: 850 RVA: 0x0001B826 File Offset: 0x00019A26
		public void SetMouseVisible(bool value)
		{
			this.SceneLayer.InputRestrictions.SetMouseVisibility(value);
		}

		// Token: 0x06000353 RID: 851 RVA: 0x0001B839 File Offset: 0x00019A39
		public void SetIsHeirSelectionPopupActive(bool isHeirSelectionPopupActive)
		{
			if (this.IsHeirSelectionPopupActive != isHeirSelectionPopupActive)
			{
				this.IsHeirSelectionPopupActive = isHeirSelectionPopupActive;
			}
		}

		// Token: 0x06000354 RID: 852 RVA: 0x0001B84B File Offset: 0x00019A4B
		public bool GetMouseVisible()
		{
			return MBMapScene.GetMouseVisible();
		}

		// Token: 0x06000355 RID: 853 RVA: 0x0001B852 File Offset: 0x00019A52
		public void RestartAmbientSounds()
		{
			if (this.MapScene != null)
			{
				this.MapScene.ResumeSceneSounds();
			}
		}

		// Token: 0x06000356 RID: 854 RVA: 0x0001B86D File Offset: 0x00019A6D
		void IGameStateListener.OnFinalize()
		{
		}

		// Token: 0x06000357 RID: 855 RVA: 0x0001B86F File Offset: 0x00019A6F
		public void PauseAmbientSounds()
		{
			if (this.MapScene != null)
			{
				this.MapScene.PauseSceneSounds();
			}
		}

		// Token: 0x06000358 RID: 856 RVA: 0x0001B88C File Offset: 0x00019A8C
		private void CollectTickableMapMeshes()
		{
			this._tickedMapEntities = this.MapScene.FindEntitiesWithTag("ticked_map_entity").ToArray<GameEntity>();
			this._tickedMapMeshes = new Mesh[this._tickedMapEntities.Length];
			for (int i = 0; i < this._tickedMapEntities.Length; i++)
			{
				this._tickedMapMeshes[i] = this._tickedMapEntities[i].GetFirstMesh();
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000359 RID: 857 RVA: 0x0001B8EF File Offset: 0x00019AEF
		public static Dictionary<UIntPtr, MapEntityVisual> VisualsOfEntities
		{
			get
			{
				return SandBoxViewSubModule.VisualsOfEntities;
			}
		}

		// Token: 0x0600035A RID: 858 RVA: 0x0001B8F8 File Offset: 0x00019AF8
		public MBCampaignEvent CreatePeriodicUIEvent(CampaignTime triggerPeriod, CampaignTime initialWait)
		{
			MBCampaignEvent mbcampaignEvent = new MBCampaignEvent(triggerPeriod, initialWait);
			this._periodicCampaignUIEvents.Add(mbcampaignEvent);
			return mbcampaignEvent;
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x0600035B RID: 859 RVA: 0x0001B91A File Offset: 0x00019B1A
		internal static Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>> FrameAndVisualOfEngines
		{
			get
			{
				return SandBoxViewSubModule.FrameAndVisualOfEngines;
			}
		}

		// Token: 0x0600035C RID: 860 RVA: 0x0001B924 File Offset: 0x00019B24
		private void DeleteMarkedPeriodicEvents()
		{
			for (int i = this._periodicCampaignUIEvents.Count - 1; i >= 0; i--)
			{
				if (this._periodicCampaignUIEvents[i].isEventDeleted)
				{
					this._periodicCampaignUIEvents.RemoveAt(i);
				}
			}
		}

		// Token: 0x0600035D RID: 861 RVA: 0x0001B968 File Offset: 0x00019B68
		public void DeletePeriodicUIEvent(MBCampaignEvent campaignEvent)
		{
			campaignEvent.isEventDeleted = true;
		}

		// Token: 0x0600035E RID: 862 RVA: 0x0001B971 File Offset: 0x00019B71
		private static float CalculateCameraElevation(float cameraDistance)
		{
			return cameraDistance * 0.5f * 0.015f + 0.35f;
		}

		// Token: 0x0600035F RID: 863 RVA: 0x0001B986 File Offset: 0x00019B86
		public void OpenOptions()
		{
			ScreenManager.PushScreen(ViewCreator.CreateOptionsScreen(false));
		}

		// Token: 0x06000360 RID: 864 RVA: 0x0001B993 File Offset: 0x00019B93
		public void OpenEncyclopedia()
		{
			Campaign.Current.EncyclopediaManager.GoToLink("LastPage", "");
		}

		// Token: 0x06000361 RID: 865 RVA: 0x0001B9AE File Offset: 0x00019BAE
		public void OpenSaveLoad(bool isSaving)
		{
			ScreenManager.PushScreen(SandBoxViewCreator.CreateSaveLoadScreen(isSaving));
		}

		// Token: 0x06000362 RID: 866 RVA: 0x0001B9BB File Offset: 0x00019BBB
		public void CloseEscapeMenu()
		{
			this.OnEscapeMenuToggled(false);
		}

		// Token: 0x06000363 RID: 867 RVA: 0x0001B9C4 File Offset: 0x00019BC4
		public void OpenEscapeMenu()
		{
			this.OnEscapeMenuToggled(true);
		}

		// Token: 0x06000364 RID: 868 RVA: 0x0001B9CD File Offset: 0x00019BCD
		private void OpenGameplayCheats()
		{
			this._mapCheatsView = this.AddMapView<MapCheatsView>(Array.Empty<object>());
			this.IsMapCheatsActive = true;
		}

		// Token: 0x06000365 RID: 869 RVA: 0x0001B9E7 File Offset: 0x00019BE7
		public void CloseGameplayCheats()
		{
			if (this._mapCheatsView != null)
			{
				this.RemoveMapView(this._mapCheatsView);
				return;
			}
			Debug.FailedAssert("Requested remove map cheats but cheats is not enabled", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "CloseGameplayCheats", 2536);
		}

		// Token: 0x06000366 RID: 870 RVA: 0x0001BA18 File Offset: 0x00019C18
		public void CloseCampaignOptions()
		{
			if (this._campaignOptionsView == null)
			{
				Debug.FailedAssert("Trying to close campaign options when it's not set", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "CloseCampaignOptions", 2544);
				this._campaignOptionsView = this.GetMapView<MapCampaignOptionsView>();
				if (this._campaignOptionsView == null)
				{
					Debug.FailedAssert("Trying to close campaign options when it's not open", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "CloseCampaignOptions", 2549);
					this.IsInCampaignOptions = false;
					this._campaignOptionsView = null;
					return;
				}
			}
			if (this._campaignOptionsView != null)
			{
				this.RemoveMapView(this._campaignOptionsView);
			}
			this._campaignOptionsView = null;
			this.IsInCampaignOptions = false;
		}

		// Token: 0x06000367 RID: 871 RVA: 0x0001BAA4 File Offset: 0x00019CA4
		private List<EscapeMenuItemVM> GetEscapeMenuItems()
		{
			bool isMapConversationActive = this._conversationView.IsConversationActive;
			bool cannotQuickSave = MBSaveLoad.IsMaxNumberOfSavesReached() && !MBSaveLoad.IsSaveGameFileExists(MBSaveLoad.ActiveSaveSlotName);
			if (cannotQuickSave && CampaignOptions.IsIronmanMode)
			{
				string activeSaveSlotName = MBSaveLoad.ActiveSaveSlotName;
				string[] saveFileNames = MBSaveLoad.GetSaveFileNames();
				for (int i = 0; i < saveFileNames.Length; i++)
				{
					if (saveFileNames[i] == activeSaveSlotName)
					{
						cannotQuickSave = false;
						break;
					}
				}
			}
			List<EscapeMenuItemVM> list = new List<EscapeMenuItemVM>();
			list.Add(new EscapeMenuItemVM(new TextObject("{=e139gKZc}Return to the Game", null), delegate(object o)
			{
				this.OnEscapeMenuToggled(false);
			}, null, () => new Tuple<bool, TextObject>(false, null), true));
			list.Add(new EscapeMenuItemVM(new TextObject("{=PXT6aA4J}Campaign Options", null), delegate(object o)
			{
				this._campaignOptionsView = this.AddMapView<MapCampaignOptionsView>(Array.Empty<object>());
				this.IsInCampaignOptions = true;
			}, null, () => new Tuple<bool, TextObject>(false, null), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options", null), delegate(object o)
			{
				this.OnEscapeMenuToggled(false);
				this.OpenOptions();
			}, null, () => new Tuple<bool, TextObject>(false, null), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=bV75iwKa}Save", null), delegate(object o)
			{
				this.OnEscapeMenuToggled(false);
				Campaign.Current.SaveHandler.QuickSaveCurrentGame();
			}, null, () => this.GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, false, cannotQuickSave), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=e0KdfaNe}Save As", null), delegate(object o)
			{
				this.OnEscapeMenuToggled(false);
				this.OpenSaveLoad(true);
			}, null, () => this.GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, CampaignOptions.IsIronmanMode, false), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=9NuttOBC}Load", null), delegate(object o)
			{
				this.OnEscapeMenuToggled(false);
				this.OpenSaveLoad(false);
			}, null, () => this.GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, CampaignOptions.IsIronmanMode, false), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=AbEh2y8o}Save And Exit", null), delegate(object o)
			{
				Campaign.Current.SaveHandler.QuickSaveCurrentGame();
				this.OnEscapeMenuToggled(false);
				InformationManager.HideInquiry();
				this._exitOnSaveOver = true;
			}, null, () => this.GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, false, cannotQuickSave), false));
			Action <>9__16;
			list.Add(new EscapeMenuItemVM(new TextObject("{=RamV6yLM}Exit to Main Menu", null), delegate(object o)
			{
				string titleText = GameTexts.FindText("str_exit", null).ToString();
				string text = GameTexts.FindText("str_mission_exit_query", null).ToString();
				bool isAffirmativeOptionShown = true;
				bool isNegativeOptionShown = true;
				string affirmativeText = GameTexts.FindText("str_yes", null).ToString();
				string negativeText = GameTexts.FindText("str_no", null).ToString();
				Action affirmativeAction = new Action(this.OnExitToMainMenu);
				Action negativeAction;
				if ((negativeAction = <>9__16) == null)
				{
					negativeAction = (<>9__16 = delegate()
					{
						this.OnEscapeMenuToggled(false);
					});
				}
				InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown, isNegativeOptionShown, affirmativeText, negativeText, affirmativeAction, negativeAction, "", 0f, null, null, null), false, false);
			}, null, () => this.GetIsEscapeMenuOptionDisabledReason(false, CampaignOptions.IsIronmanMode, false), false));
			return list;
		}

		// Token: 0x06000368 RID: 872 RVA: 0x0001BCE8 File Offset: 0x00019EE8
		private Tuple<bool, TextObject> GetIsEscapeMenuOptionDisabledReason(bool isMapConversationActive, bool isIronmanMode, bool cannotQuickSave)
		{
			if (isIronmanMode)
			{
				return new Tuple<bool, TextObject>(true, GameTexts.FindText("str_pause_menu_disabled_hint", "IronmanMode"));
			}
			if (isMapConversationActive)
			{
				return new Tuple<bool, TextObject>(true, GameTexts.FindText("str_pause_menu_disabled_hint", "OngoingConversation"));
			}
			if (cannotQuickSave)
			{
				return new Tuple<bool, TextObject>(true, GameTexts.FindText("str_pause_menu_disabled_hint", "SaveLimitReached"));
			}
			return new Tuple<bool, TextObject>(false, null);
		}

		// Token: 0x06000369 RID: 873 RVA: 0x0001BD47 File Offset: 0x00019F47
		private void OpenParty()
		{
			if (Hero.MainHero != null && !Hero.MainHero.IsPrisoner && !Hero.MainHero.IsDead)
			{
				PartyScreenHelper.OpenScreenAsNormal();
			}
		}

		// Token: 0x0600036A RID: 874 RVA: 0x0001BD6D File Offset: 0x00019F6D
		public void OpenInventory()
		{
			if (Hero.MainHero != null)
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero != null && !mainHero.IsDead)
				{
					InventoryScreenHelper.OpenScreenAsInventory(null);
				}
			}
		}

		// Token: 0x0600036B RID: 875 RVA: 0x0001BD94 File Offset: 0x00019F94
		private void OpenKingdom()
		{
			if (Hero.MainHero != null)
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero != null && !mainHero.IsDead && Hero.MainHero.MapFaction.IsKingdomFaction)
				{
					KingdomState gameState = Game.Current.GameStateManager.CreateState<KingdomState>();
					Game.Current.GameStateManager.PushState(gameState, 0);
				}
			}
		}

		// Token: 0x0600036C RID: 876 RVA: 0x0001BDF0 File Offset: 0x00019FF0
		private void OnExitToMainMenu()
		{
			this.OnEscapeMenuToggled(false);
			InformationManager.HideInquiry();
			this.OnExit();
		}

		// Token: 0x0600036D RID: 877 RVA: 0x0001BE04 File Offset: 0x0001A004
		private void OpenQuestsScreen()
		{
			if (Hero.MainHero != null)
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero != null && !mainHero.IsDead)
				{
					Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<QuestsState>(), 0);
				}
			}
		}

		// Token: 0x0600036E RID: 878 RVA: 0x0001BE42 File Offset: 0x0001A042
		private void OpenClanScreen()
		{
			if (Hero.MainHero != null)
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero != null && !mainHero.IsDead)
				{
					Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<ClanState>(), 0);
				}
			}
		}

		// Token: 0x0600036F RID: 879 RVA: 0x0001BE80 File Offset: 0x0001A080
		private void OpenCharacterDevelopmentScreen()
		{
			if (Hero.MainHero != null)
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero != null && !mainHero.IsDead)
				{
					Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<CharacterDeveloperState>(), 0);
				}
			}
		}

		// Token: 0x06000370 RID: 880 RVA: 0x0001BEBE File Offset: 0x0001A0BE
		public void OpenFacegenScreenAux()
		{
			this.OpenFaceGeneratorScreen();
		}

		// Token: 0x06000371 RID: 881 RVA: 0x0001BEC6 File Offset: 0x0001A0C6
		public bool IsCameraLockedToPlayerParty()
		{
			return this.MapCameraView.IsCameraLockedToPlayerParty();
		}

		// Token: 0x06000372 RID: 882 RVA: 0x0001BED3 File Offset: 0x0001A0D3
		public void FastMoveCameraToMainParty()
		{
			this.MapCameraView.FastMoveCameraToMainParty();
		}

		// Token: 0x06000373 RID: 883 RVA: 0x0001BEE0 File Offset: 0x0001A0E0
		public void ResetCamera(bool resetDistance, bool teleportToMainParty)
		{
			this.MapCameraView.ResetCamera(resetDistance, teleportToMainParty);
		}

		// Token: 0x06000374 RID: 884 RVA: 0x0001BEEF File Offset: 0x0001A0EF
		public void TeleportCameraToMainParty()
		{
			this.MapCameraView.TeleportCameraToMainParty();
		}

		// Token: 0x06000375 RID: 885 RVA: 0x0001BEFC File Offset: 0x0001A0FC
		void IChatLogHandlerScreen.TryUpdateChatLogLayerParameters(ref bool isTeamChatAvailable, ref bool inputEnabled, ref bool isToggleChatHintAvailable, ref bool isMouseVisible, ref InputContext inputContext)
		{
			if (this.SceneLayer != null)
			{
				inputEnabled = true;
				isToggleChatHintAvailable = true;
				inputContext = this.SceneLayer.Input;
				isMouseVisible = this.SceneLayer.InputRestrictions.MouseVisibility;
			}
		}

		// Token: 0x06000376 RID: 886 RVA: 0x0001BF30 File Offset: 0x0001A130
		private void TickCircles()
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			float num = 0.5f;
			float num2 = 0.5f;
			int num3 = 0;
			int num4 = 0;
			uint factor1Linear = 4293199122U;
			uint factor1Linear2 = 4293199122U;
			uint factor1Linear3 = 4293199122U;
			bool flag4 = false;
			bool flag5 = false;
			MatrixFrame matrixFrame = MatrixFrame.Identity;
			PartyBase partyBase = null;
			if (MobileParty.MainParty.PartyMoveMode == MoveModeType.Point && MobileParty.MainParty.DefaultBehavior != AiBehavior.GoToSettlement && MobileParty.MainParty.DefaultBehavior != AiBehavior.Hold && !MobileParty.MainParty.ForceAiNoPathMode && MobileParty.MainParty.Ai.AiBehaviorInteractable == null && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.TargetPosition.DistanceSquared(MobileParty.MainParty.Position) > 0.01f)
			{
				flag3 = true;
				flag = true;
				num = 0.238846f;
				num2 = 0.278584f;
				num3 = 4;
				num4 = 5;
				factor1Linear = 4293993473U;
				factor1Linear2 = 4293993473U;
				matrixFrame.origin = MobileParty.MainParty.TargetPosition.AsVec3();
				flag5 = true;
			}
			else
			{
				if (MobileParty.MainParty.PartyMoveMode == MoveModeType.Party && MobileParty.MainParty.MoveTargetParty != null && MobileParty.MainParty.MoveTargetParty.IsVisible)
				{
					if (MobileParty.MainParty.MoveTargetParty.CurrentSettlement == null || MobileParty.MainParty.MoveTargetParty.CurrentSettlement.IsHideout)
					{
						partyBase = MobileParty.MainParty.MoveTargetParty.Party;
					}
					else
					{
						partyBase = MobileParty.MainParty.MoveTargetParty.CurrentSettlement.Party;
					}
				}
				else if (MobileParty.MainParty.DefaultBehavior == AiBehavior.GoToSettlement && MobileParty.MainParty.TargetSettlement != null)
				{
					partyBase = MobileParty.MainParty.TargetSettlement.Party;
				}
				if (partyBase != null)
				{
					bool flag6 = FactionManager.IsAtWarAgainstFaction(partyBase.MapFaction, Hero.MainHero.MapFaction);
					bool flag7 = DiplomacyHelper.IsSameFactionAndNotEliminated(partyBase.MapFaction, Hero.MainHero.MapFaction);
					if (partyBase.IsMobile)
					{
						MapEntityVisual<PartyBase> partyVisual = this.GetPartyVisual(partyBase);
						if (partyVisual != null)
						{
							matrixFrame = partyVisual.CircleLocalFrame;
							flag3 = true;
							num3 = this.GetCircleIndex();
							float num5 = 1.2f;
							if (partyBase.MobileParty.IsCurrentlyAtSea)
							{
								num5 = 2.5f;
							}
							factor1Linear = (flag6 ? 4292093218U : (flag7 ? 4284183827U : 4291596077U));
							num = matrixFrame.rotation.GetScaleVector().x * num5;
						}
					}
					else
					{
						matrixFrame = SettlementVisualManager.Current.GetSettlementVisual(partyBase.Settlement).CircleLocalFrame;
						if (partyBase.IsSettlement && partyBase.Settlement.IsFortification)
						{
							flag4 = true;
							flag2 = true;
							factor1Linear3 = (flag6 ? 4292093218U : (flag7 ? 4284183827U : 4291596077U));
							num = matrixFrame.rotation.GetScaleVector().x * 1.3f;
						}
						else
						{
							flag3 = true;
							num3 = 5;
							factor1Linear = (flag6 ? 4292093218U : (flag7 ? 4284183827U : 4291596077U));
							num = matrixFrame.rotation.GetScaleVector().x * 1.2f;
						}
					}
					if (!flag4)
					{
						matrixFrame.origin = partyBase.Position.AsVec3();
						if (partyBase.IsMobile)
						{
							matrixFrame.origin += (partyBase.MobileParty.EventPositionAdder + partyBase.MobileParty.ArmyPositionAdder).ToVec3(0f);
						}
					}
				}
			}
			if (flag5)
			{
				float num6 = (MapScreen.Instance.MapCameraView.CameraDistance + 80f) * (MapScreen.Instance.MapCameraView.CameraDistance + 80f) / 5000f;
				num6 = MathF.Clamp(num6, 0.2f, 45f);
				num *= num6;
				num2 *= num6;
			}
			if (partyBase == null)
			{
				this._targetCircleRotationStartTime = 0f;
			}
			else if (this._targetCircleRotationStartTime == 0f)
			{
				this._targetCircleRotationStartTime = MBCommon.GetApplicationTime();
			}
			Vec3 normalAt = MapScreen.Instance.MapScene.GetNormalAt(matrixFrame.origin.AsVec2);
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = matrixFrame.origin;
			MobileParty mainParty = MobileParty.MainParty;
			bool flag8 = mainParty != null && !mainParty.TargetPosition.IsOnLand;
			bool flag9 = partyBase != null;
			MobileParty mainParty2 = MobileParty.MainParty;
			if (mainParty2 != null)
			{
				bool isCurrentlyAtSea = mainParty2.IsCurrentlyAtSea;
			}
			identity.rotation.u = normalAt;
			MatrixFrame matrixFrame2 = identity;
			Vec3 vec = new Vec3(num, num, num, -1f);
			identity.rotation.ApplyScaleLocal(vec);
			vec = new Vec3(num2, num2, num2, -1f);
			matrixFrame2.rotation.ApplyScaleLocal(vec);
			this._townCircleDecal.GameEntity.SetVisibilityExcludeParents(flag2);
			this._pointTargetInnerDecal.GameEntity.SetVisibilityExcludeParents(flag3 && (!flag8 || flag9));
			this._pointTargetOuterDecal.GameEntity.SetVisibilityExcludeParents(flag && (!flag8 || flag9));
			this._pointTargetWindDirectionDecal.GameEntity.SetVisibilityExcludeParents(flag3 && flag8 && !flag9);
			if (flag3)
			{
				if (flag8 && !flag9)
				{
					float num7 = num + 0.15f;
					MatrixFrame matrixFrame3 = identity;
					vec = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(MobileParty.MainParty.TargetPosition).ToVec3(0f);
					Vec3 vec2 = vec.NormalizedCopy();
					matrixFrame3.rotation = Mat3.CreateMat3WithForward(vec2);
					vec = new Vec3(num7, num7, num7, -1f);
					matrixFrame3.rotation.ApplyScaleLocal(vec);
					matrixFrame3.rotation.RotateAboutUp(1.5707964f);
					this._pointTargetWindDirectionDecal.Decal.SetFactor1Linear(factor1Linear);
					this._pointTargetWindDirectionDecal.Decal.SetVectorArgument(1f, 1f, 0f, 0f);
					this._pointTargetWindDirectionDecal.GameEntity.SetGlobalFrame(matrixFrame3, true);
				}
				else
				{
					this._pointTargetInnerDecal.Decal.SetVectorArgument(0.166f, 1f, 0.166f * (float)num3, 0f);
					this._pointTargetInnerDecal.Decal.SetFactor1Linear(factor1Linear);
					this._pointTargetInnerDecal.GameEntity.SetGlobalFrame(identity, true);
				}
			}
			if (flag)
			{
				this._pointTargetOuterDecal.Decal.SetVectorArgument(0.166f, 1f, 0.166f * (float)num4, 0f);
				this._pointTargetOuterDecal.Decal.SetFactor1Linear(factor1Linear2);
				this._pointTargetOuterDecal.GameEntity.SetGlobalFrame(matrixFrame2, true);
			}
			if (flag2)
			{
				this._townCircleDecal.Decal.SetVectorArgument(1f, 1f, 0f, 0f);
				this._townCircleDecal.Decal.SetFactor1Linear(factor1Linear3);
				this._townCircleDecal.GameEntity.SetGlobalFrame(matrixFrame, true);
			}
			MatrixFrame matrixFrame4 = MatrixFrame.Identity;
			MapEntityVisual<PartyBase> mapEntityVisual;
			if (MapScreen.Instance.CurrentVisualOfTooltip == null || (partyBase != null && MapScreen.Instance.CurrentVisualOfTooltip == this.GetPartyVisual(partyBase)) || (mapEntityVisual = MapScreen.Instance.CurrentVisualOfTooltip as MapEntityVisual<PartyBase>) == null)
			{
				this._settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(false);
				this._partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(false);
				return;
			}
			MapScreen.Instance.MapCursor.OnAnotherEntityHighlighted();
			if (mapEntityVisual == null)
			{
				this._settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(false);
				this._partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(false);
				return;
			}
			bool flag10 = mapEntityVisual.IsEnemyOf(Hero.MainHero.MapFaction);
			bool flag11 = mapEntityVisual.IsAllyOf(Hero.MainHero.MapFaction);
			flag4 = mapEntityVisual.MapEntity.IsSettlement && mapEntityVisual.MapEntity.Settlement.IsFortification;
			if (flag4)
			{
				Vec3 origin = this._settlementHoverOutlineDecal.GameEntity.GetGlobalFrame().origin;
				matrixFrame4 = mapEntityVisual.CircleLocalFrame;
				if (flag10)
				{
					this._settlementHoverOutlineDecal.Decal.SetFactor1Linear(4292093218U);
				}
				else if (flag11)
				{
					this._settlementHoverOutlineDecal.Decal.SetFactor1Linear(4284183827U);
				}
				else
				{
					this._settlementHoverOutlineDecal.Decal.SetFactor1Linear(4291596077U);
				}
			}
			else
			{
				Vec3 origin = this._settlementHoverOutlineDecal.GameEntity.GetGlobalFrame().origin;
				matrixFrame4.origin = mapEntityVisual.GetVisualPosition() + mapEntityVisual.CircleLocalFrame.origin;
				matrixFrame4.rotation = mapEntityVisual.CircleLocalFrame.rotation;
				this._partyHoverOutlineDecal.Decal.SetFactor1Linear(flag10 ? 4292093218U : (flag11 ? 4284183827U : 4291596077U));
				this._partyHoverOutlineDecal.Decal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
				float z;
				if (!(origin.AsVec2 != matrixFrame4.origin.AsVec2))
				{
					z = origin.z;
				}
				else
				{
					PartyBase mapEntity = mapEntityVisual.MapEntity;
					bool flag12;
					if (mapEntity == null)
					{
						flag12 = false;
					}
					else
					{
						MobileParty mobileParty = mapEntity.MobileParty;
						bool? flag13 = ((mobileParty != null) ? new bool?(mobileParty.IsCurrentlyAtSea) : null);
						bool flag14 = true;
						flag12 = (flag13.GetValueOrDefault() == flag14) & (flag13 != null);
					}
					z = (flag12 ? matrixFrame4.origin.z : MapScreen.Instance.MapScene.GetTerrainHeight(matrixFrame4.origin.AsVec2, true));
				}
				matrixFrame4.origin.z = z;
			}
			if (flag4)
			{
				this._settlementHoverOutlineDecal.GameEntity.SetGlobalFrame(matrixFrame4, true);
				this._settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(true);
				this._partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(false);
				return;
			}
			if (mapEntityVisual.MapEntity.IsMobile && mapEntityVisual.MapEntity.MobileParty.IsCurrentlyAtSea)
			{
				vec = Vec3.One * 2.5f;
				matrixFrame4.Scale(vec);
			}
			this._partyHoverOutlineDecal.GameEntity.SetGlobalFrame(matrixFrame4, true);
			this._settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(false);
			this._partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(true);
		}

		// Token: 0x06000377 RID: 887 RVA: 0x0001C960 File Offset: 0x0001AB60
		private int GetCircleIndex()
		{
			int num = (int)((MBCommon.GetApplicationTime() - this._targetCircleRotationStartTime) / 0.1f) % 10;
			if (num >= 5)
			{
				num = 10 - num - 1;
			}
			return num;
		}

		// Token: 0x06000378 RID: 888 RVA: 0x0001C990 File Offset: 0x0001AB90
		private MapEntityVisual<PartyBase> GetPartyVisual(PartyBase party)
		{
			MapEntityVisual<PartyBase> mapEntityVisual = null;
			foreach (EntityVisualManagerBase<PartyBase> entityVisualManagerBase in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>())
			{
				mapEntityVisual = entityVisualManagerBase.GetVisualOfEntity(party);
				if (mapEntityVisual != null)
				{
					break;
				}
			}
			return mapEntityVisual;
		}

		// Token: 0x06000379 RID: 889 RVA: 0x0001C9F0 File Offset: 0x0001ABF0
		private void TickStepSounds(float realDt)
		{
			if (!NativeConfig.DisableSound && ScreenManager.TopScreen is MapScreen)
			{
				this._soundCalculationTime += realDt;
				if (this.IsSoundOn && Campaign.Current.CampaignDt > 0f)
				{
					MobileParty mainParty = MobileParty.MainParty;
					float seeingRange = mainParty.SeeingRange;
					LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(mainParty.Position.ToVec2(), seeingRange + 25f);
					for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData))
					{
						if (!mobileParty.IsMilitia && !mobileParty.IsGarrison && !mobileParty.IsCurrentlyAtSea)
						{
							this.StepSounds(mobileParty);
						}
					}
				}
				if (this._soundCalculationTime > 0.2f)
				{
					this._soundCalculationTime -= 0.2f;
				}
			}
		}

		// Token: 0x0600037A RID: 890 RVA: 0x0001CAB8 File Offset: 0x0001ACB8
		private void StepSounds(MobileParty party)
		{
			if (party.IsVisible && party.MemberRoster.TotalManCount > 0)
			{
				MobilePartyVisual partyVisual = MobilePartyVisualManager.Current.GetPartyVisual(party.Party);
				if (partyVisual.HumanAgentVisuals != null)
				{
					TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
					AgentVisuals agentVisuals = null;
					TerrainTypeSoundSlot soundType = TerrainTypeSoundSlot.Dismounted;
					if (partyVisual.CaravanMountAgentVisuals != null)
					{
						soundType = TerrainTypeSoundSlot.Caravan;
						agentVisuals = partyVisual.CaravanMountAgentVisuals;
					}
					else if (partyVisual.HumanAgentVisuals != null)
					{
						if (partyVisual.MountAgentVisuals != null)
						{
							soundType = TerrainTypeSoundSlot.Mounted;
							if (party.Army != null && party.AttachedParties.Count > 0)
							{
								soundType = TerrainTypeSoundSlot.ArmyMounted;
							}
							agentVisuals = partyVisual.MountAgentVisuals;
						}
						else
						{
							soundType = TerrainTypeSoundSlot.Dismounted;
							if (party.Army != null && party.AttachedParties.Count > 0)
							{
								soundType = TerrainTypeSoundSlot.ArmyDismounted;
							}
							agentVisuals = partyVisual.HumanAgentVisuals;
						}
					}
					if (party.AttachedTo == null)
					{
						MBMapScene.TickStepSound(this.MapScene, agentVisuals.GetVisuals(), (int)faceTerrainType, soundType, party.AttachedParties.Count);
					}
				}
			}
		}

		// Token: 0x04000180 RID: 384
		private const float DoubleClickTimeLimit = 0.3f;

		// Token: 0x04000181 RID: 385
		private INavigationHandler _navigationHandler;

		// Token: 0x04000184 RID: 388
		private const int _frameDelayAmountForRenderActivation = 5;

		// Token: 0x0400018A RID: 394
		private MenuViewContext _menuViewContext;

		// Token: 0x0400018B RID: 395
		private MenuContext _latestMenuContext;

		// Token: 0x0400018C RID: 396
		public readonly Dictionary<Tuple<Material, Banner>, Material> CharacterBannerMaterialCache = new Dictionary<Tuple<Material, Banner>, Material>();

		// Token: 0x0400018D RID: 397
		private bool _partyIconNeedsRefreshing;

		// Token: 0x0400018E RID: 398
		private uint _tooltipTargetHash;

		// Token: 0x0400018F RID: 399
		private object _tooltipTargetObject;

		// Token: 0x04000190 RID: 400
		private MapViewsContainer _mapViewsContainer;

		// Token: 0x04000191 RID: 401
		private MapView _encounterOverlay;

		// Token: 0x04000192 RID: 402
		public static bool DisableVisualTicks;

		// Token: 0x04000195 RID: 405
		private MapReadyView _mapReadyView;

		// Token: 0x04000196 RID: 406
		private MapView _armyOverlay;

		// Token: 0x0400019B RID: 411
		public IMapTracksCampaignBehavior MapTracksCampaignBehavior;

		// Token: 0x0400019C RID: 412
		private double _lastReleaseTime;

		// Token: 0x0400019D RID: 413
		private double _lastPressTime;

		// Token: 0x0400019E RID: 414
		private MapView _marriageOfferPopupView;

		// Token: 0x0400019F RID: 415
		private Vec3 _clickedPosition;

		// Token: 0x040001A0 RID: 416
		private Vec2 _clickedPositionPixel;

		// Token: 0x040001A1 RID: 417
		private double _secondLastPressTime;

		// Token: 0x040001A2 RID: 418
		private bool _leftButtonDoubleClickOnSceneWidget;

		// Token: 0x040001A3 RID: 419
		private bool _ignoreNextTimeToggle;

		// Token: 0x040001A4 RID: 420
		private MapView _heirSelectionPopupView;

		// Token: 0x040001A5 RID: 421
		private Ray _mouseRay;

		// Token: 0x040001A6 RID: 422
		private float _timeToggleTimer = float.MaxValue;

		// Token: 0x040001A7 RID: 423
		private float _waitForDoubleClickUntilTime;

		// Token: 0x040001A8 RID: 424
		private MapView _campaignOptionsView;

		// Token: 0x040001A9 RID: 425
		private MapView _mapCheatsView;

		// Token: 0x040001AA RID: 426
		private MapView _battleSimulationView;

		// Token: 0x040001AB RID: 427
		private MapView _escapeMenuView;

		// Token: 0x040001AC RID: 428
		private bool _leftButtonDraggingMode;

		// Token: 0x040001AD RID: 429
		private MapConversationView _conversationView;

		// Token: 0x040001AE RID: 430
		private MapEntityVisual _preVisualOfSelectedEntity;

		// Token: 0x040001AF RID: 431
		private Vec2 _oldMousePosition;

		// Token: 0x040001B0 RID: 432
		private int _activatedFrameNo = Utilities.EngineFrameNo;

		// Token: 0x040001B1 RID: 433
		private bool _exitOnSaveOver;

		// Token: 0x040001B2 RID: 434
		private bool _isSceneViewEnabled;

		// Token: 0x040001B3 RID: 435
		private bool _isReadyForRender;

		// Token: 0x040001B4 RID: 436
		private bool _gpuMemoryCleared;

		// Token: 0x040001B5 RID: 437
		private bool _focusLost;

		// Token: 0x040001B6 RID: 438
		private bool _isKingdomDecisionsDirty;

		// Token: 0x040001B7 RID: 439
		private float _cheatPressTimer;

		// Token: 0x040001B8 RID: 440
		private MapScreen.DecalEntity _pointTargetWindDirectionDecal;

		// Token: 0x040001B9 RID: 441
		private MapScreen.DecalEntity _pointTargetInnerDecal;

		// Token: 0x040001BA RID: 442
		private MapScreen.DecalEntity _pointTargetOuterDecal;

		// Token: 0x040001BB RID: 443
		private MapScreen.DecalEntity _partyHoverOutlineDecal;

		// Token: 0x040001BC RID: 444
		private MapScreen.DecalEntity _townCircleDecal;

		// Token: 0x040001BD RID: 445
		private MapScreen.DecalEntity _settlementHoverOutlineDecal;

		// Token: 0x040001BE RID: 446
		private float _targetCircleRotationStartTime;

		// Token: 0x040001BF RID: 447
		private float _soundCalculationTime;

		// Token: 0x040001C0 RID: 448
		private const float SoundCalculationInterval = 0.2f;

		// Token: 0x040001CD RID: 461
		private Dictionary<Tuple<Material, Banner>, Material> _bannerTexturedMaterialCache;

		// Token: 0x040001CF RID: 463
		public const uint EnemyPartyDecalColor = 4292093218U;

		// Token: 0x040001D0 RID: 464
		public const uint AllyPartyDecalColor = 4284183827U;

		// Token: 0x040001D1 RID: 465
		public const uint NeutralPartyDecalColor = 4291596077U;

		// Token: 0x040001D2 RID: 466
		private bool _mapSceneCursorWanted = true;

		// Token: 0x040001D3 RID: 467
		private bool _mapSceneCursorActive;

		// Token: 0x040001D4 RID: 468
		private TutorialContexts _currentTutorialContext = TutorialContexts.MapWindow;

		// Token: 0x040001D5 RID: 469
		private MapColorGradeManager _colorGradeManager;

		// Token: 0x040001D6 RID: 470
		private int _mapScreenTickCount;

		// Token: 0x040001D7 RID: 471
		private int _sceneReadyFrameCounter;

		// Token: 0x040001D8 RID: 472
		public bool TooltipHandlingDisabled;

		// Token: 0x040001D9 RID: 473
		private readonly UIntPtr[] _intersectedEntityIDs = new UIntPtr[128];

		// Token: 0x040001DA RID: 474
		private readonly Intersection[] _intersectionInfos = new Intersection[128];

		// Token: 0x040001DB RID: 475
		private GameEntity[] _tickedMapEntities;

		// Token: 0x040001DC RID: 476
		private Mesh[] _tickedMapMeshes;

		// Token: 0x040001DD RID: 477
		private readonly List<MBCampaignEvent> _periodicCampaignUIEvents;

		// Token: 0x040001DE RID: 478
		private bool _ignoreLeftMouseRelease;

		// Token: 0x020000A5 RID: 165
		public enum MapOverlayType
		{
			// Token: 0x04000352 RID: 850
			None,
			// Token: 0x04000353 RID: 851
			Army
		}

		// Token: 0x020000A6 RID: 166
		public struct DecalEntity
		{
			// Token: 0x170000B9 RID: 185
			// (get) Token: 0x060005CA RID: 1482 RVA: 0x00029018 File Offset: 0x00027218
			// (set) Token: 0x060005CB RID: 1483 RVA: 0x00029020 File Offset: 0x00027220
			public GameEntity GameEntity { get; set; }

			// Token: 0x170000BA RID: 186
			// (get) Token: 0x060005CC RID: 1484 RVA: 0x00029029 File Offset: 0x00027229
			// (set) Token: 0x060005CD RID: 1485 RVA: 0x00029031 File Offset: 0x00027231
			public Decal Decal { get; set; }

			// Token: 0x060005CE RID: 1486 RVA: 0x0002903A File Offset: 0x0002723A
			public DecalEntity(GameEntity gameEntity, Decal decal)
			{
				this.GameEntity = gameEntity;
				this.Decal = decal;
			}

			// Token: 0x060005CF RID: 1487 RVA: 0x0002904C File Offset: 0x0002724C
			public static MapScreen.DecalEntity Create(Scene scene, string material, string entityName = null)
			{
				GameEntity gameEntity = GameEntity.CreateEmpty(scene, true, true, true);
				gameEntity.Name = entityName ?? "Entity";
				Decal decal = Decal.CreateDecal(null);
				Material fromResource = Material.GetFromResource(material);
				if (fromResource != null)
				{
					decal.SetMaterial(fromResource);
				}
				scene.AddDecalInstance(decal, "editor_set", false);
				gameEntity.AddComponent(decal);
				return new MapScreen.DecalEntity(gameEntity, decal);
			}
		}

		// Token: 0x020000A7 RID: 167
		private struct MouseInputState
		{
			// Token: 0x04000356 RID: 854
			public bool IsLeftMouseDown;

			// Token: 0x04000357 RID: 855
			public bool IsLeftMousePressed;

			// Token: 0x04000358 RID: 856
			public bool IsLeftMouseReleased;

			// Token: 0x04000359 RID: 857
			public bool IsMiddleMouseDown;

			// Token: 0x0400035A RID: 858
			public bool IsMiddleMousePressed;

			// Token: 0x0400035B RID: 859
			public bool IsMiddleMouseReleased;

			// Token: 0x0400035C RID: 860
			public bool IsRightMouseDown;

			// Token: 0x0400035D RID: 861
			public bool IsRightMousePressed;

			// Token: 0x0400035E RID: 862
			public bool IsRightMouseReleased;
		}

		// Token: 0x020000A8 RID: 168
		public class MainMapCameraMoveEvent : EventBase
		{
			// Token: 0x170000BB RID: 187
			// (get) Token: 0x060005D0 RID: 1488 RVA: 0x000290AA File Offset: 0x000272AA
			// (set) Token: 0x060005D1 RID: 1489 RVA: 0x000290B2 File Offset: 0x000272B2
			public bool RotationChanged { get; private set; }

			// Token: 0x170000BC RID: 188
			// (get) Token: 0x060005D2 RID: 1490 RVA: 0x000290BB File Offset: 0x000272BB
			// (set) Token: 0x060005D3 RID: 1491 RVA: 0x000290C3 File Offset: 0x000272C3
			public bool PositionChanged { get; private set; }

			// Token: 0x060005D4 RID: 1492 RVA: 0x000290CC File Offset: 0x000272CC
			public MainMapCameraMoveEvent(bool rotationChanged, bool positionChanged)
			{
				this.RotationChanged = rotationChanged;
				this.PositionChanged = positionChanged;
			}
		}
	}
}
