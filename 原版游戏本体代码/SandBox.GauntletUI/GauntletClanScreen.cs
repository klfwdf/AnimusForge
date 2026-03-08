using System;
using Helpers;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
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
	// Token: 0x02000007 RID: 7
	[GameStateScreen(typeof(ClanState))]
	public class GauntletClanScreen : ScreenBase, IGameStateListener
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000027 RID: 39 RVA: 0x00002AAD File Offset: 0x00000CAD
		// (set) Token: 0x06000028 RID: 40 RVA: 0x00002AB5 File Offset: 0x00000CB5
		public ClanManagementVM _dataSource { get; private set; }

		// Token: 0x06000029 RID: 41 RVA: 0x00002ABE File Offset: 0x00000CBE
		public GauntletClanScreen(ClanState clanState)
		{
			this._clanState = clanState;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002ACD File Offset: 0x00000CCD
		protected virtual ClanManagementVM CreateDataSource()
		{
			return new ClanManagementVM(new Action(this.CloseClanScreen), new Action<Hero>(this.ShowHeroOnMap), new Action<Hero>(this.OpenPartyScreenForNewClanParty), new Action(this.OpenBannerEditorWithPlayerClan));
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002B04 File Offset: 0x00000D04
		protected override void OnInitialize()
		{
			base.OnInitialize();
			InformationManager.HideAllMessages();
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002B14 File Offset: 0x00000D14
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			LoadingWindow.DisableGlobalLoadingWindow();
			ClanManagementVM dataSource = this._dataSource;
			ClanCardSelectionPopupVM cardSelectionPopup = this._dataSource.CardSelectionPopup;
			dataSource.CanSwitchTabs = (cardSelectionPopup == null || !cardSelectionPopup.IsVisible) && (!Input.IsGamepadActive || (!InformationManager.GetIsAnyTooltipActiveAndExtended() && this._gauntletLayer.IsHitThisFrame));
			ClanManagementVM dataSource2 = this._dataSource;
			bool flag;
			if (dataSource2 == null)
			{
				flag = false;
			}
			else
			{
				ClanCardSelectionPopupVM cardSelectionPopup2 = dataSource2.CardSelectionPopup;
				bool? flag2 = ((cardSelectionPopup2 != null) ? new bool?(cardSelectionPopup2.IsVisible) : null);
				bool flag3 = true;
				flag = (flag2.GetValueOrDefault() == flag3) & (flag2 != null);
			}
			if (flag)
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					if (this._dataSource.CardSelectionPopup.IsDoneEnabled)
					{
						UISoundsHelper.PlayUISound("event:/ui/default");
						this._dataSource.CardSelectionPopup.ExecuteDone();
						return;
					}
				}
				else if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.CardSelectionPopup.ExecuteCancel();
					return;
				}
			}
			else if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				if (this.IsRoleSelectionPopupActive())
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.ClanParties.CurrentSelectedParty.IsRoleSelectionPopupVisible = false;
					return;
				}
				this.CloseClanScreen();
				return;
			}
			else
			{
				if (this._gauntletLayer.Input.IsGameKeyPressed(41) || this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					this.CloseClanScreen();
					return;
				}
				if (this._dataSource.CanSwitchTabs)
				{
					if (this._gauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
					{
						UISoundsHelper.PlayUISound("event:/ui/tab");
						this._dataSource.SelectPreviousCategory();
						return;
					}
					if (this._gauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
					{
						UISoundsHelper.PlayUISound("event:/ui/tab");
						this._dataSource.SelectNextCategory();
					}
				}
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002D14 File Offset: 0x00000F14
		protected bool IsRoleSelectionPopupActive()
		{
			ClanPartiesVM clanParties = this._dataSource.ClanParties;
			return clanParties.IsSelected && clanParties.IsAnyValidPartySelected && clanParties.CurrentSelectedParty.IsRoleSelectionPopupVisible;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002D4A File Offset: 0x00000F4A
		protected void OpenPartyScreenForNewClanParty(Hero hero)
		{
			this._isCreatingPartyWithMembers = true;
			PartyScreenHelper.OpenScreenAsCreateClanPartyForHero(hero, null, null);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002D5B File Offset: 0x00000F5B
		protected void OpenBannerEditorWithPlayerClan()
		{
			Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>(), 0);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002D7C File Offset: 0x00000F7C
		void IGameStateListener.OnActivate()
		{
			base.OnActivate();
			this._clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
			this._gauntletLayer = new GauntletLayer("ClanScreen", 1, true);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
			base.AddLayer(this._gauntletLayer);
			this._dataSource = this.CreateDataSource();
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
			this._dataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
			if (this._isCreatingPartyWithMembers)
			{
				this._dataSource.SelectParty(PartyBase.MainParty);
				this._isCreatingPartyWithMembers = false;
			}
			else if (this._clanState.InitialSelectedHero != null)
			{
				this._dataSource.SelectHero(this._clanState.InitialSelectedHero);
			}
			else if (this._clanState.InitialSelectedParty != null)
			{
				this._dataSource.SelectParty(this._clanState.InitialSelectedParty);
				if (this._clanState.InitialSelectedParty.LeaderHero == null)
				{
					ClanPartiesVM clanParties = this._dataSource.ClanParties;
					bool flag;
					if (clanParties == null)
					{
						flag = false;
					}
					else
					{
						ClanPartyItemVM currentSelectedParty = clanParties.CurrentSelectedParty;
						bool? flag2 = ((currentSelectedParty != null) ? new bool?(currentSelectedParty.IsChangeLeaderEnabled) : null);
						bool flag3 = true;
						flag = (flag2.GetValueOrDefault() == flag3) & (flag2 != null);
					}
					if (flag)
					{
						this._dataSource.ClanParties.OnShowChangeLeaderPopup();
					}
				}
			}
			else if (this._clanState.InitialSelectedSettlement != null)
			{
				this._dataSource.SelectSettlement(this._clanState.InitialSelectedSettlement);
			}
			else if (this._clanState.InitialSelectedWorkshop != null)
			{
				this._dataSource.SelectWorkshop(this._clanState.InitialSelectedWorkshop);
			}
			else if (this._clanState.InitialSelectedAlley != null)
			{
				this._dataSource.SelectAlley(this._clanState.InitialSelectedAlley);
			}
			this._gauntletLayer.LoadMovie("ClanScreen", this._dataSource);
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.ClanScreen));
			UISoundsHelper.PlayUISound("event:/ui/panels/panel_clan_open");
			this._gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x0000303A File Offset: 0x0000123A
		protected void ShowHeroOnMap(Hero hero)
		{
			this.CloseClanScreen();
			MapScreen.Instance.FastMoveCameraToPosition(hero.GetCampaignPosition());
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003052 File Offset: 0x00001252
		void IGameStateListener.OnDeactivate()
		{
			base.OnDeactivate();
			base.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.None));
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003092 File Offset: 0x00001292
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003094 File Offset: 0x00001294
		void IGameStateListener.OnFinalize()
		{
			this._clanCategory.Unload();
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._gauntletLayer = null;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000030BA File Offset: 0x000012BA
		protected override void OnActivate()
		{
			base.OnActivate();
			ClanManagementVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.RefreshCategoryValues();
			}
			ClanManagementVM dataSource2 = this._dataSource;
			if (dataSource2 == null)
			{
				return;
			}
			dataSource2.UpdateBannerVisuals();
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000030E3 File Offset: 0x000012E3
		protected void CloseClanScreen()
		{
			Game.Current.GameStateManager.PopState(0);
			UISoundsHelper.PlayUISound("event:/ui/default");
		}

		// Token: 0x0400000F RID: 15
		protected GauntletLayer _gauntletLayer;

		// Token: 0x04000010 RID: 16
		protected SpriteCategory _clanCategory;

		// Token: 0x04000011 RID: 17
		protected readonly ClanState _clanState;

		// Token: 0x04000012 RID: 18
		protected bool _isCreatingPartyWithMembers;
	}
}
