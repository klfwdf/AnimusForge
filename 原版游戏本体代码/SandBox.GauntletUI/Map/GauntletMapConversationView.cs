using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Barter;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000034 RID: 52
	[OverrideView(typeof(MapConversationView))]
	public class GauntletMapConversationView : MapConversationView, IConversationStateHandler
	{
		// Token: 0x0600026F RID: 623 RVA: 0x0000ED4A File Offset: 0x0000CF4A
		public GauntletMapConversationView()
		{
			this._barterManager = Campaign.Current.BarterManager;
			this._conversationCategory = UIResourceManager.GetSpriteCategory("ui_conversation");
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000ED72 File Offset: 0x0000CF72
		private void OnBarterActiveStateChanged(bool isBarterActive)
		{
			this._dataSource.IsBarterActive = isBarterActive;
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0000ED80 File Offset: 0x0000CF80
		protected override void InitializeConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
			base.InitializeConversation(playerCharacterData, conversationPartnerData);
			this._playerCharacterData = playerCharacterData;
			this._conversationPartnerData = conversationPartnerData;
			this.DestroyConversationTableau();
			base.DestroyConversationMission();
			base.CreateConversationMissionIfMissing();
			if (!base.IsConversationActive)
			{
				this.CreateConversationView();
				this.CreateConversationTableau();
			}
			else
			{
				this._minimumAvailableConversationInstallFrame = Utilities.EngineFrameNo + 2;
				this._isSwitchingConversations = true;
			}
			base.IsConversationActive = true;
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000EDE6 File Offset: 0x0000CFE6
		protected override void FinalizeConversation()
		{
			base.FinalizeConversation();
			this.DestroyConversationTableau();
			this.DestroyConversationView();
			base.DestroyConversationMission();
			this._minimumAvailableConversationInstallFrame = Utilities.EngineFrameNo + 2;
			base.IsConversationActive = false;
			if (!base.MapScreen.IsReady)
			{
				LoadingWindow.EnableGlobalLoadingWindow();
			}
		}

		// Token: 0x06000273 RID: 627 RVA: 0x0000EE28 File Offset: 0x0000D028
		protected override void OnActivate()
		{
			base.OnActivate();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
			if (base.IsConversationActive)
			{
				this._conversationMovie = this._layerAsGauntletLayer.LoadMovie("MapConversation", this._dataSource);
				if (this._barterView.IsCreated && !this._barterView.IsActive)
				{
					this._barterView.Activate();
				}
				this._conversationCategory.Load();
				this._dataSource.TableauData = this._tableauData;
			}
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0000EEB4 File Offset: 0x0000D0B4
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
			if (base.IsConversationActive)
			{
				this._dataSource.TableauData = null;
				this._layerAsGauntletLayer.ReleaseMovie(this._conversationMovie);
				if (this._barterView.IsCreated && this._barterView.IsActive)
				{
					this._barterView.Deactivate();
				}
				this._conversationCategory.Unload();
			}
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000EF30 File Offset: 0x0000D130
		private void Tick(float dt)
		{
			if (!base.IsConversationActive || this._layerAsGauntletLayer == null)
			{
				return;
			}
			if (this._isSwitchingConversations)
			{
				this._isSwitchingConversations = false;
			}
			if (base.IsConversationActive && ScreenManager.TopScreen == base.MapScreen && ScreenManager.FocusedLayer != base.Layer)
			{
				ScreenManager.TrySetFocus(base.Layer);
			}
			MapConversationVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.Tick(dt);
			}
			MapConversationVM dataSource2 = this._dataSource;
			bool flag;
			if (dataSource2 == null)
			{
				flag = false;
			}
			else
			{
				MissionConversationVM dialogController = dataSource2.DialogController;
				int? num = ((dialogController != null) ? new int?(dialogController.AnswerList.Count) : null);
				int num2 = 0;
				flag = (num.GetValueOrDefault() <= num2) & (num != null);
			}
			if (flag && !this._barterView.IsCreated && base.IsConversationActive && this._layerAsGauntletLayer.Input.IsHotKeyReleased("ContinueKey"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				((IConversationStateHandler)this).ExecuteConversationContinue();
			}
			if (base.IsConversationActive && this._layerAsGauntletLayer != null)
			{
				if (this._barterView.IsCreated)
				{
					this._barterView.TickInput();
				}
				else
				{
					if (base.IsConversationActive && this._tableauData == null && Utilities.EngineFrameNo > this._minimumAvailableConversationInstallFrame)
					{
						this.CreateConversationTableau();
					}
					if (this._layerAsGauntletLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
					{
						MapScreen mapScreen = base.MapScreen;
						if (mapScreen != null && mapScreen.IsEscapeMenuOpened)
						{
							base.MapScreen.CloseEscapeMenu();
						}
						else
						{
							MapScreen mapScreen2 = base.MapScreen;
							if (mapScreen2 != null)
							{
								mapScreen2.OpenEscapeMenu();
							}
						}
					}
				}
				BarterItemVM.IsFiveStackModifierActive = this._layerAsGauntletLayer.Input.IsHotKeyDown("FiveStackModifier");
				BarterItemVM.IsEntireStackModifierActive = this._layerAsGauntletLayer.Input.IsHotKeyDown("EntireStackModifier");
			}
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0000F0F2 File Offset: 0x0000D2F2
		protected override void OnFinalize()
		{
			base.OnFinalize();
			if (base.IsConversationActive)
			{
				this.FinalizeConversation();
			}
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0000F108 File Offset: 0x0000D308
		private void CreateConversationView()
		{
			base.Layer = new GauntletLayer("MapConversation", 205, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			this._barterView = new GauntletMapConversationBarterView(this._layerAsGauntletLayer, new GauntletMapConversationBarterView.OnBarterActiveStateChanged(this.OnBarterActiveStateChanged));
			BarterManager barterManager = this._barterManager;
			barterManager.BarterBegin = (BarterManager.BarterBeginEventDelegate)Delegate.Combine(barterManager.BarterBegin, new BarterManager.BarterBeginEventDelegate(this._barterView.CreateBarterView));
			BarterManager barterManager2 = this._barterManager;
			barterManager2.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Combine(barterManager2.Closed, new BarterManager.BarterCloseEventDelegate(this._barterView.DestroyBarterView));
			this._dataSource = new MapConversationVM(new Action(this.OnContinue), new Func<string>(GauntletMapConversationView.GetContinueKeyText));
			this._conversationMovie = this._layerAsGauntletLayer.LoadMovie("MapConversation", this._dataSource);
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ConversationHotKeyCategory"));
			base.MapScreen.AddLayer(base.Layer);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			this._conversationCategory.Load();
			Campaign.Current.ConversationManager.Handler = this;
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000F2C0 File Offset: 0x0000D4C0
		private void OnContinue()
		{
			if (base.IsConversationActive)
			{
				MapConversationVM dataSource = this._dataSource;
				bool flag;
				if (dataSource == null)
				{
					flag = false;
				}
				else
				{
					MissionConversationVM dialogController = dataSource.DialogController;
					int? num = ((dialogController != null) ? new int?(dialogController.AnswerList.Count) : null);
					int num2 = 0;
					flag = (num.GetValueOrDefault() <= num2) & (num != null);
				}
				if (flag && !this._barterView.IsCreated)
				{
					((IConversationStateHandler)this).ExecuteConversationContinue();
				}
			}
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0000F334 File Offset: 0x0000D534
		private void DestroyConversationView()
		{
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			if (this._barterView.IsCreated)
			{
				this._barterView.DestroyBarterView();
			}
			this._dataSource.OnFinalize();
			base.MapScreen.RemoveLayer(base.Layer);
			SpriteCategory conversationCategory = this._conversationCategory;
			if (conversationCategory != null && conversationCategory.IsLoaded)
			{
				this._conversationCategory.Unload();
			}
			BarterManager barterManager = this._barterManager;
			barterManager.BarterBegin = (BarterManager.BarterBeginEventDelegate)Delegate.Remove(barterManager.BarterBegin, new BarterManager.BarterBeginEventDelegate(this._barterView.CreateBarterView));
			BarterManager barterManager2 = this._barterManager;
			barterManager2.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Remove(barterManager2.Closed, new BarterManager.BarterCloseEventDelegate(this._barterView.DestroyBarterView));
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			this._dataSource = null;
			Campaign.Current.ConversationManager.Handler = null;
			Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		}

		// Token: 0x0600027A RID: 634 RVA: 0x0000F438 File Offset: 0x0000D638
		protected override bool IsEscaped()
		{
			return base.IsConversationActive;
		}

		// Token: 0x0600027B RID: 635 RVA: 0x0000F440 File Offset: 0x0000D640
		protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return true;
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000F443 File Offset: 0x0000D643
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.Tick(dt);
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0000F453 File Offset: 0x0000D653
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			this.Tick(dt);
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0000F463 File Offset: 0x0000D663
		protected override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			this.Tick(dt);
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0000F474 File Offset: 0x0000D674
		private void CreateConversationTableau()
		{
			float timeOfDay = CampaignTime.Now.CurrentHourInDay * (float)(24 / CampaignTime.HoursInDay);
			MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(MobileParty.MainParty.Position.ToVec2());
			bool isCurrentTerrainUnderSnow = weatherEventInPosition == MapWeatherModel.WeatherEvent.Snowy || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
			string locationId = null;
			if (this._conversationPartnerData.Character.HeroObject != null)
			{
				LocationComplex locationComplex = LocationComplex.Current;
				string text;
				if (locationComplex == null)
				{
					text = null;
				}
				else
				{
					Location locationOfCharacter = locationComplex.GetLocationOfCharacter(this._conversationPartnerData.Character.HeroObject);
					text = ((locationOfCharacter != null) ? locationOfCharacter.StringId : null);
				}
				locationId = text;
			}
			this._tableauData = MapConversationTableauData.CreateFrom(this._playerCharacterData, this._conversationPartnerData, Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace), timeOfDay, isCurrentTerrainUnderSnow, Hero.MainHero.CurrentSettlement, locationId, weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain, weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard);
			this._dataSource.TableauData = this._tableauData;
			this._layerAsGauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(1, null);
		}

		// Token: 0x06000280 RID: 640 RVA: 0x0000F578 File Offset: 0x0000D778
		private void DestroyConversationTableau()
		{
			if (this._dataSource != null)
			{
				this._dataSource.TableauData = null;
			}
			this._tableauData = null;
		}

		// Token: 0x06000281 RID: 641 RVA: 0x0000F595 File Offset: 0x0000D795
		void IConversationStateHandler.OnConversationUninstall()
		{
			if (!this._isSwitchingConversations)
			{
				MapState mapState = Game.Current.GameStateManager.LastOrDefault<MapState>();
				if (mapState == null)
				{
					return;
				}
				mapState.OnMapConversationOver();
			}
		}

		// Token: 0x06000282 RID: 642 RVA: 0x0000F5B8 File Offset: 0x0000D7B8
		private static string GetContinueKeyText()
		{
			if (Input.IsGamepadActive)
			{
				return GameTexts.FindText("str_click_to_continue_console", null).SetTextVariable("CONSOLE_KEY_NAME", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ConversationHotKeyCategory", "ContinueClick"), 1f)).ToString();
			}
			return GameTexts.FindText("str_click_to_continue", null).ToString();
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0000F610 File Offset: 0x0000D810
		void IConversationStateHandler.OnConversationInstall()
		{
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0000F612 File Offset: 0x0000D812
		void IConversationStateHandler.OnConversationActivate()
		{
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0000F614 File Offset: 0x0000D814
		void IConversationStateHandler.OnConversationDeactivate()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000F61B File Offset: 0x0000D81B
		void IConversationStateHandler.OnConversationContinue()
		{
			this._dataSource.DialogController.OnConversationContinue();
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000F62D File Offset: 0x0000D82D
		void IConversationStateHandler.ExecuteConversationContinue()
		{
			this._dataSource.DialogController.ExecuteContinue();
		}

		// Token: 0x040000DE RID: 222
		private GauntletMovieIdentifier _conversationMovie;

		// Token: 0x040000DF RID: 223
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000E0 RID: 224
		private MapConversationVM _dataSource;

		// Token: 0x040000E1 RID: 225
		private SpriteCategory _conversationCategory;

		// Token: 0x040000E2 RID: 226
		private MapConversationTableauData _tableauData;

		// Token: 0x040000E3 RID: 227
		private BarterManager _barterManager;

		// Token: 0x040000E4 RID: 228
		private GauntletMapConversationBarterView _barterView;

		// Token: 0x040000E5 RID: 229
		private ConversationCharacterData _playerCharacterData;

		// Token: 0x040000E6 RID: 230
		private ConversationCharacterData _conversationPartnerData;

		// Token: 0x040000E7 RID: 231
		private bool _isSwitchingConversations;

		// Token: 0x040000E8 RID: 232
		private int _minimumAvailableConversationInstallFrame;
	}
}
