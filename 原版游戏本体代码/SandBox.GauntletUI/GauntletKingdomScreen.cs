using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI
{
	// Token: 0x0200000C RID: 12
	[GameStateScreen(typeof(KingdomState))]
	public class GauntletKingdomScreen : ScreenBase, IGameStateListener
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600008A RID: 138 RVA: 0x00005BD7 File Offset: 0x00003DD7
		// (set) Token: 0x0600008B RID: 139 RVA: 0x00005BDF File Offset: 0x00003DDF
		public KingdomManagementVM DataSource { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00005BE8 File Offset: 0x00003DE8
		public bool IsMakingDecision
		{
			get
			{
				return this.DataSource.Decision.IsActive;
			}
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00005BFA File Offset: 0x00003DFA
		public GauntletKingdomScreen(KingdomState kingdomState)
		{
			this._kingdomState = kingdomState;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00005C09 File Offset: 0x00003E09
		protected override void OnInitialize()
		{
			base.OnInitialize();
			InformationManager.HideAllMessages();
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00005C18 File Offset: 0x00003E18
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			LoadingWindow.DisableGlobalLoadingWindow();
			this.DataSource.CanSwitchTabs = !InformationManager.GetIsAnyTooltipActiveAndExtended();
			if (MapScreen.Instance != null)
			{
				MapScreen.Instance.NavigationHandler.IsNavigationLocked = this.DataSource.Decision.IsActive;
			}
			if (this.DataSource.Decision.IsActive)
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					DecisionItemBaseVM currentDecision = this.DataSource.Decision.CurrentDecision;
					if (currentDecision != null && currentDecision.CanEndDecision)
					{
						this.DataSource.Decision.CurrentDecision.ExecuteFinalSelection();
						UISoundsHelper.PlayUISound("event:/ui/reign/decision");
					}
				}
			}
			else if (this.DataSource.GiftFief.IsOpen)
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					if (this.DataSource.GiftFief.IsAnyClanSelected)
					{
						this.DataSource.GiftFief.ExecuteGiftSettlement();
						UISoundsHelper.PlayUISound("event:/ui/default");
					}
				}
				else if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
				{
					this.DataSource.GiftFief.ExecuteClose();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
			}
			else if (this._armyManagementDatasource != null)
			{
				if (this._armyManagementLayer.Input.IsHotKeyReleased("Exit"))
				{
					this._armyManagementDatasource.ExecuteCancel();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
				else if (this._armyManagementLayer.Input.IsHotKeyReleased("Confirm"))
				{
					this._armyManagementDatasource.ExecuteDone();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
				else if (this._armyManagementLayer.Input.IsHotKeyReleased("Reset"))
				{
					this._armyManagementDatasource.ExecuteReset();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
				else if (this._armyManagementLayer.Input.IsHotKeyReleased("RemoveParty") && this._armyManagementDatasource.FocusedItem != null)
				{
					this._armyManagementDatasource.FocusedItem.ExecuteAction();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
			}
			else if (this._gauntletLayer.Input.IsHotKeyReleased("Exit") || this._gauntletLayer.Input.IsGameKeyPressed(40) || this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				this.CloseKingdomScreen();
			}
			else if (this.DataSource.CanSwitchTabs)
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
				{
					this.DataSource.SelectPreviousCategory();
					UISoundsHelper.PlayUISound("event:/ui/tab");
				}
				else if (this._gauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
				{
					this.DataSource.SelectNextCategory();
					UISoundsHelper.PlayUISound("event:/ui/tab");
				}
			}
			KingdomManagementVM dataSource = this.DataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.OnFrameTick();
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00005F1C File Offset: 0x0000411C
		protected virtual KingdomManagementVM CreateDataSource()
		{
			return new KingdomManagementVM(new Action(this.CloseKingdomScreen), new Action(this.OpenArmyManagement), new Action<Army>(this.ShowArmyOnMap));
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00005F48 File Offset: 0x00004148
		void IGameStateListener.OnActivate()
		{
			base.OnActivate();
			this._kingdomCategory = UIResourceManager.LoadSpriteCategory("ui_kingdom");
			this._clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
			this._gauntletLayer = new GauntletLayer("KingdomScreen", 1, true);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
			base.AddLayer(this._gauntletLayer);
			this.DataSource = this.CreateDataSource();
			this.DataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this.DataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this.DataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
			this.DataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
			if (this._kingdomState.InitialSelectedDecision != null)
			{
				this.DataSource.Decision.HandleDecision(this._kingdomState.InitialSelectedDecision);
			}
			else if (this._kingdomState.InitialSelectedArmy != null)
			{
				this.DataSource.SelectArmy(this._kingdomState.InitialSelectedArmy);
			}
			else if (this._kingdomState.InitialSelectedSettlement != null)
			{
				this.DataSource.SelectSettlement(this._kingdomState.InitialSelectedSettlement);
			}
			else if (this._kingdomState.InitialSelectedClan != null)
			{
				this.DataSource.SelectClan(this._kingdomState.InitialSelectedClan);
			}
			else if (this._kingdomState.InitialSelectedPolicy != null)
			{
				this.DataSource.SelectPolicy(this._kingdomState.InitialSelectedPolicy);
			}
			else if (this._kingdomState.InitialSelectedKingdom != null)
			{
				this.DataSource.SelectKingdom(this._kingdomState.InitialSelectedKingdom);
			}
			this._gauntletLayer.LoadMovie("KingdomManagement", this.DataSource);
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.KingdomScreen));
			UISoundsHelper.PlayUISound("event:/ui/panels/panel_kingdom_open");
			this._gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000061AD File Offset: 0x000043AD
		void IGameStateListener.OnDeactivate()
		{
			base.OnDeactivate();
			base.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.None));
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000061ED File Offset: 0x000043ED
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000061F0 File Offset: 0x000043F0
		void IGameStateListener.OnFinalize()
		{
			if (MapScreen.Instance != null)
			{
				MapScreen.Instance.NavigationHandler.IsNavigationLocked = false;
			}
			this._kingdomCategory.Unload();
			this._clanCategory.Unload();
			this.DataSource.OnFinalize();
			this.DataSource = null;
			this._gauntletLayer = null;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00006243 File Offset: 0x00004443
		protected void ShowArmyOnMap(Army army)
		{
			this.CloseKingdomScreen();
			MapScreen.Instance.FastMoveCameraToPosition(army.LeaderParty.Position);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00006260 File Offset: 0x00004460
		protected void OpenArmyManagement()
		{
			if (this._gauntletLayer != null)
			{
				this._armyManagementDatasource = new ArmyManagementVM(new Action(this.CloseArmyManagement));
				this._armyManagementDatasource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
				this._armyManagementDatasource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
				this._armyManagementDatasource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
				this._armyManagementDatasource.SetRemoveInputKey(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory").GetHotKey("RemoveParty"));
				this._armyManagementCategory = UIResourceManager.LoadSpriteCategory("ui_armymanagement");
				this._armyManagementLayer = new GauntletLayer("Kingdom_ArmManagement", 2, false);
				this._armyManagementLayer.LoadMovie("ArmyManagement", this._armyManagementDatasource);
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
				this._armyManagementLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ArmyManagementHotkeyCategory"));
				this._armyManagementLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
				this._armyManagementLayer.IsFocusLayer = true;
				base.AddLayer(this._armyManagementLayer);
				ScreenManager.TrySetFocus(this._armyManagementLayer);
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x000063C8 File Offset: 0x000045C8
		protected void CloseArmyManagement()
		{
			if (this._armyManagementLayer != null)
			{
				this._armyManagementLayer.InputRestrictions.ResetInputRestrictions();
				this._armyManagementLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._armyManagementLayer);
				base.RemoveLayer(this._armyManagementLayer);
				this._armyManagementLayer = null;
			}
			if (this._armyManagementDatasource != null)
			{
				this._armyManagementDatasource.OnFinalize();
				this._armyManagementDatasource = null;
			}
			if (this._armyManagementCategory != null)
			{
				this._armyManagementCategory.Unload();
				this._armyManagementCategory = null;
			}
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.KingdomScreen));
			this.DataSource.OnRefresh();
		}

		// Token: 0x06000098 RID: 152 RVA: 0x0000646B File Offset: 0x0000466B
		protected void CloseKingdomScreen()
		{
			Game.Current.GameStateManager.PopState(0);
			UISoundsHelper.PlayUISound("event:/ui/default");
		}

		// Token: 0x04000041 RID: 65
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000042 RID: 66
		private readonly KingdomState _kingdomState;

		// Token: 0x04000043 RID: 67
		private GauntletLayer _armyManagementLayer;

		// Token: 0x04000044 RID: 68
		private ArmyManagementVM _armyManagementDatasource;

		// Token: 0x04000045 RID: 69
		private SpriteCategory _kingdomCategory;

		// Token: 0x04000046 RID: 70
		private SpriteCategory _armyManagementCategory;

		// Token: 0x04000047 RID: 71
		private SpriteCategory _clanCategory;
	}
}
