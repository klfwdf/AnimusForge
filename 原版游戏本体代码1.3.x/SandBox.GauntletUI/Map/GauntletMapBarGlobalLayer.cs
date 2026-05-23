using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200002D RID: 45
	public class GauntletMapBarGlobalLayer : GlobalLayer
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600022B RID: 555 RVA: 0x0000D7EB File Offset: 0x0000B9EB
		public bool IsInArmyManagement
		{
			get
			{
				return this._armyManagementLayer != null && this._armyManagementVM != null;
			}
		}

		// Token: 0x0600022C RID: 556 RVA: 0x0000D800 File Offset: 0x0000BA00
		public GauntletMapBarGlobalLayer(MapScreen mapScreen, INavigationHandler navigationHandler, float contextAlphaModifider)
		{
			this._mapScreen = mapScreen;
			this._mapNavigationHandler = navigationHandler;
			this._contextAlphaModifider = contextAlphaModifider;
			this._mapScreen.NavigationHandler = navigationHandler;
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0000D834 File Offset: 0x0000BA34
		public void Initialize(MapBarVM dataSource)
		{
			this._dataSource = dataSource;
			this._dataSource.Initialize(this._mapNavigationHandler, this._mapScreen, new Func<MapBarShortcuts>(this.GetMapBarShortcuts), new Action(this.OpenArmyManagement));
			this._gauntletLayer = new GauntletLayer("MapBar", 202, false);
			base.Layer = this._gauntletLayer;
			this._mapBarCategory = UIResourceManager.LoadSpriteCategory("ui_mapbar");
			this._movie = this._gauntletLayer.LoadMovie("MapBar", this._dataSource);
			this._encyclopediaManager = this._mapScreen.EncyclopediaScreenManager;
		}

		// Token: 0x0600022E RID: 558 RVA: 0x0000D8D8 File Offset: 0x0000BAD8
		public void OnFinalize()
		{
			if (this._gauntletLayer != null)
			{
				if (this._gauntletArmyManagementMovie != null)
				{
					this._gauntletLayer.ReleaseMovie(this._gauntletArmyManagementMovie);
				}
				if (this._movie != null)
				{
					this._gauntletLayer.ReleaseMovie(this._movie);
				}
			}
			ArmyManagementVM armyManagementVM = this._armyManagementVM;
			if (armyManagementVM != null)
			{
				armyManagementVM.OnFinalize();
			}
			this._dataSource.OnFinalize();
			this._mapBarCategory.Unload();
			this._armyManagementVM = null;
			this._gauntletLayer = null;
			this._dataSource = null;
			this._encyclopediaManager = null;
			this._mapScreen = null;
		}

		// Token: 0x0600022F RID: 559 RVA: 0x0000D969 File Offset: 0x0000BB69
		public void OnMapConversationStarted()
		{
			if (this._gauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._gauntletLayer, true);
			}
		}

		// Token: 0x06000230 RID: 560 RVA: 0x0000D97F File Offset: 0x0000BB7F
		public void OnMapConversationOver()
		{
			if (this._gauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._gauntletLayer, false);
			}
		}

		// Token: 0x06000231 RID: 561 RVA: 0x0000D995 File Offset: 0x0000BB95
		public void Refresh()
		{
			MapBarVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.OnRefresh();
		}

		// Token: 0x06000232 RID: 562 RVA: 0x0000D9A8 File Offset: 0x0000BBA8
		private MapBarShortcuts GetMapBarShortcuts()
		{
			return new MapBarShortcuts
			{
				FastForwardHotkey = Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", 62).ToString(),
				PauseHotkey = Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", 60).ToString(),
				PlayHotkey = Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", 61).ToString()
			};
		}

		// Token: 0x06000233 RID: 563 RVA: 0x0000DA24 File Offset: 0x0000BC24
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			this._gauntletLayer.UIContext.ContextAlpha = MathF.Lerp(this._gauntletLayer.UIContext.ContextAlpha, this._contextAlphaTarget, dt * this._contextAlphaModifider, 1E-05f);
			Game game = Game.Current;
			GameState gameState = ((game != null) ? game.GameStateManager.ActiveState : null);
			ScreenBase topScreen = ScreenManager.TopScreen;
			INavigationHandler mapNavigationHandler = this._mapNavigationHandler;
			bool flag = mapNavigationHandler != null && mapNavigationHandler.IsAnyElementActive();
			if (topScreen is MapScreen || flag)
			{
				this._dataSource.IsEnabled = true;
				this._dataSource.CurrentScreen = topScreen.GetType().Name;
				bool flag2 = ScreenManager.TopScreen is MapScreen;
				this._dataSource.MapTimeControl.IsInMap = flag2;
				base.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
				if (!(gameState is MapState))
				{
					this._dataSource.MapTimeControl.IsCenterPanelEnabled = false;
					if (flag)
					{
						this.HandlePanelSwitching();
					}
					this._contextAlphaTarget = 1f;
				}
				else
				{
					MapState mapState = (MapState)gameState;
					if (flag2)
					{
						MapScreen mapScreen = ScreenManager.TopScreen as MapScreen;
						mapScreen.SetIsBarExtended(this._dataSource.MapInfo.IsInfoBarExtended);
						this._dataSource.MapTimeControl.IsInRecruitment = mapScreen.IsInRecruitment;
						this._dataSource.MapTimeControl.IsInBattleSimulation = mapScreen.IsInBattleSimulation;
						MapTimeControlVM mapTimeControl = this._dataSource.MapTimeControl;
						MapEncyclopediaView encyclopediaManager = this._encyclopediaManager;
						mapTimeControl.IsEncyclopediaOpen = encyclopediaManager != null && encyclopediaManager.IsEncyclopediaOpen;
						this._dataSource.MapTimeControl.IsInArmyManagement = mapScreen.IsInArmyManagement;
						this._dataSource.MapTimeControl.IsInTownManagement = mapScreen.IsInTownManagement;
						this._dataSource.MapTimeControl.IsInHideoutTroopManage = mapScreen.IsInHideoutTroopManage;
						this._dataSource.MapTimeControl.IsInCampaignOptions = mapScreen.IsInCampaignOptions;
						this._dataSource.MapTimeControl.IsEscapeMenuOpened = mapScreen.IsEscapeMenuOpened;
						this._dataSource.MapTimeControl.IsMarriageOfferPopupActive = mapScreen.IsMarriageOfferPopupActive;
						this._dataSource.MapTimeControl.IsHeirSelectionPopupActive = mapScreen.IsHeirSelectionPopupActive;
						this._dataSource.MapTimeControl.IsMapCheatsActive = mapScreen.IsMapCheatsActive;
						this._dataSource.MapTimeControl.IsMapIncidentActive = mapScreen.IsMapIncidentActive;
						this._dataSource.MapTimeControl.IsOverlayContextMenuEnabled = mapScreen.IsOverlayContextMenuEnabled;
						MapConversationView mapView = mapScreen.GetMapView<MapConversationView>();
						if (((mapView != null) ? mapView.ConversationMission : null) != null)
						{
							this._contextAlphaTarget = 0f;
						}
						else
						{
							this._contextAlphaTarget = 1f;
						}
						if (this._armyManagementVM != null)
						{
							this.HandleArmyManagementInput();
						}
					}
					else
					{
						this._dataSource.MapTimeControl.IsCenterPanelEnabled = false;
					}
				}
				this._dataSource.Tick(dt);
				return;
			}
			this._dataSource.IsEnabled = false;
			base.Layer.InputRestrictions.ResetInputRestrictions();
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0000DD14 File Offset: 0x0000BF14
		private void HandleArmyManagementInput()
		{
			if (this._armyManagementLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._armyManagementVM.ExecuteCancel();
				return;
			}
			if (this._armyManagementLayer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._armyManagementVM.ExecuteDone();
				return;
			}
			if (this._armyManagementLayer.Input.IsHotKeyReleased("Reset"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._armyManagementVM.ExecuteReset();
				return;
			}
			if (this._armyManagementLayer.Input.IsHotKeyReleased("RemoveParty") && this._armyManagementVM.FocusedItem != null)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._armyManagementVM.FocusedItem.ExecuteAction();
			}
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0000DDE8 File Offset: 0x0000BFE8
		private void HandlePanelSwitching()
		{
			GauntletLayer gauntletLayer = ScreenManager.TopScreen.FindLayer<GauntletLayer>();
			SceneLayer sceneLayer = ScreenManager.TopScreen.FindLayer<SceneLayer>();
			if (((gauntletLayer != null) ? gauntletLayer.Input : null) != null && !gauntletLayer.IsFocusedOnInput() && ScreenManager.FocusedLayer == gauntletLayer)
			{
				this.HandlePanelSwitchingInput(gauntletLayer.Input);
				return;
			}
			if (((sceneLayer != null) ? sceneLayer.Input : null) != null && ScreenManager.FocusedLayer == sceneLayer)
			{
				this.HandlePanelSwitchingInput(sceneLayer.Input);
			}
		}

		// Token: 0x06000236 RID: 566 RVA: 0x0000DE5C File Offset: 0x0000C05C
		protected virtual bool HandlePanelSwitchingInput(InputContext inputContext)
		{
			if (inputContext.IsGameKeyReleased(37) && !this._mapNavigationHandler.IsActive(MapNavigationItemType.CharacterDeveloper))
			{
				INavigationHandler mapNavigationHandler = this._mapNavigationHandler;
				if (mapNavigationHandler != null)
				{
					mapNavigationHandler.OpenCharacterDeveloper();
				}
				return true;
			}
			if (inputContext.IsGameKeyReleased(43) && !this._mapNavigationHandler.IsActive(MapNavigationItemType.Party))
			{
				INavigationHandler mapNavigationHandler2 = this._mapNavigationHandler;
				if (mapNavigationHandler2 != null)
				{
					mapNavigationHandler2.OpenParty();
				}
				return true;
			}
			if (inputContext.IsGameKeyReleased(42) && !this._mapNavigationHandler.IsActive(MapNavigationItemType.Quest))
			{
				INavigationHandler mapNavigationHandler3 = this._mapNavigationHandler;
				if (mapNavigationHandler3 != null)
				{
					mapNavigationHandler3.OpenQuests();
				}
				return true;
			}
			if (inputContext.IsGameKeyReleased(38) && !this._mapNavigationHandler.IsActive(MapNavigationItemType.Inventory))
			{
				INavigationHandler mapNavigationHandler4 = this._mapNavigationHandler;
				if (mapNavigationHandler4 != null)
				{
					mapNavigationHandler4.OpenInventory();
				}
				return true;
			}
			if (inputContext.IsGameKeyReleased(41) && !this._mapNavigationHandler.IsActive(MapNavigationItemType.Clan))
			{
				INavigationHandler mapNavigationHandler5 = this._mapNavigationHandler;
				if (mapNavigationHandler5 != null)
				{
					mapNavigationHandler5.OpenClan();
				}
				return true;
			}
			if (inputContext.IsGameKeyReleased(40) && !this._mapNavigationHandler.IsActive(MapNavigationItemType.Kingdom))
			{
				INavigationHandler mapNavigationHandler6 = this._mapNavigationHandler;
				if (mapNavigationHandler6 != null)
				{
					mapNavigationHandler6.OpenKingdom();
				}
				return true;
			}
			return false;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x0000DF6C File Offset: 0x0000C16C
		private void OpenArmyManagement()
		{
			if (this._gauntletLayer != null)
			{
				this._armyManagementLayer = new GauntletLayer("MapBar_ArmyManagement", 300, false);
				this._armyManagementCategory = UIResourceManager.LoadSpriteCategory("ui_armymanagement");
				this._armyManagementVM = new ArmyManagementVM(new Action(this.CloseArmyManagement));
				this._gauntletArmyManagementMovie = this._armyManagementLayer.LoadMovie("ArmyManagement", this._armyManagementVM);
				this._armyManagementLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory"));
				this._armyManagementLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this._armyManagementLayer);
				this._mapScreen.AddLayer(this._armyManagementLayer);
				this._armyManagementVM.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
				this._armyManagementVM.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
				this._armyManagementVM.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
				this._armyManagementVM.SetRemoveInputKey(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory").GetHotKey("RemoveParty"));
				this._timeControlModeBeforeArmyManagementOpened = Campaign.Current.TimeControlMode;
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				Campaign.Current.SetTimeControlModeLock(true);
				MapScreen mapScreen;
				if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
				{
					mapScreen.SetIsInArmyManagement(true);
				}
			}
		}

		// Token: 0x06000238 RID: 568 RVA: 0x0000E138 File Offset: 0x0000C338
		private void CloseArmyManagement()
		{
			this._armyManagementVM.OnFinalize();
			this._armyManagementLayer.ReleaseMovie(this._gauntletArmyManagementMovie);
			this._mapScreen.RemoveLayer(this._armyManagementLayer);
			this._armyManagementCategory.Unload();
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
			this._gauntletArmyManagementMovie = null;
			this._armyManagementVM = null;
			this._armyManagementLayer = null;
			Campaign.Current.SetTimeControlModeLock(false);
			Campaign.Current.TimeControlMode = this._timeControlModeBeforeArmyManagementOpened;
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInArmyManagement(false);
			}
		}

		// Token: 0x06000239 RID: 569 RVA: 0x0000E1D7 File Offset: 0x0000C3D7
		public bool IsEscaped()
		{
			if (this._armyManagementVM != null)
			{
				this._armyManagementVM.ExecuteCancel();
				return true;
			}
			return false;
		}

		// Token: 0x040000BF RID: 191
		protected MapBarVM _dataSource;

		// Token: 0x040000C0 RID: 192
		protected GauntletLayer _gauntletLayer;

		// Token: 0x040000C1 RID: 193
		protected GauntletMovieIdentifier _movie;

		// Token: 0x040000C2 RID: 194
		protected SpriteCategory _mapBarCategory;

		// Token: 0x040000C3 RID: 195
		protected MapScreen _mapScreen;

		// Token: 0x040000C4 RID: 196
		protected INavigationHandler _mapNavigationHandler;

		// Token: 0x040000C5 RID: 197
		protected MapEncyclopediaView _encyclopediaManager;

		// Token: 0x040000C6 RID: 198
		protected float _contextAlphaTarget = 1f;

		// Token: 0x040000C7 RID: 199
		protected float _contextAlphaModifider;

		// Token: 0x040000C8 RID: 200
		private GauntletLayer _armyManagementLayer;

		// Token: 0x040000C9 RID: 201
		private SpriteCategory _armyManagementCategory;

		// Token: 0x040000CA RID: 202
		private ArmyManagementVM _armyManagementVM;

		// Token: 0x040000CB RID: 203
		private GauntletMovieIdentifier _gauntletArmyManagementMovie;

		// Token: 0x040000CC RID: 204
		private CampaignTimeControlMode _timeControlModeBeforeArmyManagementOpened;
	}
}
