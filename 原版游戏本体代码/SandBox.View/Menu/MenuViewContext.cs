using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Menu
{
	// Token: 0x0200003A RID: 58
	public class MenuViewContext : IMenuContextHandler
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x060001BA RID: 442 RVA: 0x000130CD File Offset: 0x000112CD
		internal GameMenu CurGameMenu
		{
			get
			{
				return this._menuContext.GameMenu;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060001BB RID: 443 RVA: 0x000130DA File Offset: 0x000112DA
		public MenuContext MenuContext
		{
			get
			{
				return this._menuContext;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x060001BC RID: 444 RVA: 0x000130E2 File Offset: 0x000112E2
		// (set) Token: 0x060001BD RID: 445 RVA: 0x000130EA File Offset: 0x000112EA
		public List<MenuView> MenuViews { get; private set; }

		// Token: 0x060001BE RID: 446 RVA: 0x000130F4 File Offset: 0x000112F4
		public MenuViewContext(ScreenBase screen, MenuContext menuContext)
		{
			this._screen = screen;
			this._menuContext = menuContext;
			this.MenuViews = new List<MenuView>();
			this._menuContext.Handler = this;
			if (Campaign.Current.GameMode != CampaignGameMode.Tutorial && this.CurGameMenu.StringId != "siege_test_menu")
			{
				((IMenuContextHandler)this).OnMenuCreate();
				((IMenuContextHandler)this).OnMenuActivate();
			}
		}

		// Token: 0x060001BF RID: 447 RVA: 0x0001315C File Offset: 0x0001135C
		public void UpdateMenuContext(MenuContext menuContext)
		{
			this._menuContext = menuContext;
			this._menuContext.Handler = this;
			this.MenuViews.ForEach(delegate(MenuView m)
			{
				m.MenuContext = menuContext;
			});
			this.MenuViews.ForEach(delegate(MenuView m)
			{
				m.OnMenuContextUpdated(menuContext);
			});
			this.CheckAndInitializeOverlay();
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x000131C2 File Offset: 0x000113C2
		public void AddLayer(ScreenLayer layer)
		{
			this._screen.AddLayer(layer);
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x000131D0 File Offset: 0x000113D0
		public void RemoveLayer(ScreenLayer layer)
		{
			this._screen.RemoveLayer(layer);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x000131DE File Offset: 0x000113DE
		public T FindLayer<T>() where T : ScreenLayer
		{
			return this._screen.FindLayer<T>();
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x000131EB File Offset: 0x000113EB
		public T FindLayer<T>(string name) where T : ScreenLayer
		{
			return this._screen.FindLayer<T>(name);
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x000131FC File Offset: 0x000113FC
		public void OnFrameTick(float dt)
		{
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				MenuView menuView = this.MenuViews[i];
				menuView.OnFrameTick(dt);
				if (menuView.Removed)
				{
					i--;
				}
			}
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x00013240 File Offset: 0x00011440
		public void OnResume()
		{
			this._isActive = true;
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				this.MenuViews[i].OnResume();
			}
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0001327C File Offset: 0x0001147C
		public void OnHourlyTick()
		{
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				this.MenuViews[i].OnHourlyTick();
			}
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x000132B0 File Offset: 0x000114B0
		public void OnActivate()
		{
			this._isActive = true;
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				this.MenuViews[i].OnActivate();
			}
			MenuContext menuContext = this.MenuContext;
			if (!string.IsNullOrEmpty((menuContext != null) ? menuContext.CurrentAmbientSoundID : null))
			{
				this.PlayAmbientSound(this.MenuContext.CurrentAmbientSoundID);
			}
			MenuContext menuContext2 = this.MenuContext;
			if (!string.IsNullOrEmpty((menuContext2 != null) ? menuContext2.CurrentPanelSoundID : null))
			{
				this.PlayPanelSound(this.MenuContext.CurrentPanelSoundID);
			}
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00013340 File Offset: 0x00011540
		public void OnDeactivate()
		{
			this._isActive = false;
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				this.MenuViews[i].OnDeactivate();
			}
			this.StopAllSounds();
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00013381 File Offset: 0x00011581
		public void OnInitialize()
		{
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00013383 File Offset: 0x00011583
		public void OnFinalize()
		{
			this.ClearMenuViews();
			MBInformationManager.HideInformations();
			this._menuContext = null;
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00013398 File Offset: 0x00011598
		private void ClearMenuViews()
		{
			foreach (MenuView menuView in this.MenuViews.ToArray())
			{
				this.RemoveMenuView(menuView);
			}
			this._menuCharacterDeveloper = null;
			this._menuOverlayBase = null;
			this._menuRecruitVolunteers = null;
			this._menuTownManagement = null;
			this._menuTroopSelection = null;
		}

		// Token: 0x060001CC RID: 460 RVA: 0x000133ED File Offset: 0x000115ED
		public void StopAllSounds()
		{
			SoundEvent ambientSound = this._ambientSound;
			if (ambientSound != null)
			{
				ambientSound.Release();
			}
			SoundEvent panelSound = this._panelSound;
			if (panelSound == null)
			{
				return;
			}
			panelSound.Release();
		}

		// Token: 0x060001CD RID: 461 RVA: 0x00013410 File Offset: 0x00011610
		private void PlayAmbientSound(string ambientSoundID)
		{
			if (this._isActive)
			{
				SoundEvent ambientSound = this._ambientSound;
				if (ambientSound != null)
				{
					ambientSound.Release();
				}
				this._ambientSound = SoundEvent.CreateEventFromString(ambientSoundID, null);
				this._ambientSound.Play();
			}
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00013444 File Offset: 0x00011644
		private void PlayPanelSound(string panelSoundID)
		{
			if (this._isActive)
			{
				SoundEvent panelSound = this._panelSound;
				if (panelSound != null)
				{
					panelSound.Release();
				}
				this._panelSound = SoundEvent.CreateEventFromString(panelSoundID, null);
				this._panelSound.Play();
			}
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00013478 File Offset: 0x00011678
		void IMenuContextHandler.OnAmbientSoundIDSet(string ambientSoundID)
		{
			this.PlayAmbientSound(ambientSoundID);
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00013481 File Offset: 0x00011681
		void IMenuContextHandler.OnPanelSoundIDSet(string panelSoundID)
		{
			this.PlayPanelSound(panelSoundID);
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x0001348C File Offset: 0x0001168C
		void IMenuContextHandler.OnMenuCreate()
		{
			bool flag = Campaign.Current.GameMode == CampaignGameMode.Tutorial || this.CurGameMenu.StringId == "siege_test_menu";
			if (flag && this._currentMenuBackground == null)
			{
				this._currentMenuBackground = this.AddMenuView<MenuBackgroundView>(Array.Empty<object>());
			}
			if (this._currentMenuBase == null)
			{
				this._currentMenuBase = this.AddMenuView<MenuBaseView>(Array.Empty<object>());
			}
			if (!flag)
			{
				this.CheckAndInitializeOverlay();
			}
			this.StopAllSounds();
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x00013504 File Offset: 0x00011704
		void IMenuContextHandler.OnMenuActivate()
		{
			foreach (MenuView menuView in this.MenuViews)
			{
				menuView.OnActivate();
			}
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x00013554 File Offset: 0x00011754
		public void OnMapConversationActivated()
		{
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				MenuView menuView = this.MenuViews[i];
				menuView.OnMapConversationActivated();
				if (menuView.Removed)
				{
					i--;
				}
			}
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x00013594 File Offset: 0x00011794
		public void OnMapConversationDeactivated()
		{
			for (int i = 0; i < this.MenuViews.Count; i++)
			{
				MenuView menuView = this.MenuViews[i];
				menuView.OnMapConversationDeactivated();
				if (menuView.Removed)
				{
					i--;
				}
			}
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x000135D4 File Offset: 0x000117D4
		public void OnGameStateDeactivate()
		{
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x000135D6 File Offset: 0x000117D6
		public void OnGameStateInitialize()
		{
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x000135D8 File Offset: 0x000117D8
		public void OnGameStateFinalize()
		{
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x000135DC File Offset: 0x000117DC
		private void CheckAndInitializeOverlay()
		{
			GameMenu.MenuOverlayType menuOverlayType = Campaign.Current.GameMenuManager.GetMenuOverlayType(this._menuContext);
			if (menuOverlayType != GameMenu.MenuOverlayType.None)
			{
				if (menuOverlayType != this._currentOverlayType)
				{
					if (this._menuOverlayBase != null && ((this._currentOverlayType != GameMenu.MenuOverlayType.Encounter && menuOverlayType == GameMenu.MenuOverlayType.Encounter) || (this._currentOverlayType == GameMenu.MenuOverlayType.Encounter && (menuOverlayType == GameMenu.MenuOverlayType.SettlementWithBoth || menuOverlayType == GameMenu.MenuOverlayType.SettlementWithCharacters || menuOverlayType == GameMenu.MenuOverlayType.SettlementWithParties))))
					{
						this.RemoveMenuView(this._menuOverlayBase);
						this._menuOverlayBase = null;
					}
					if (this._menuOverlayBase == null)
					{
						this._menuOverlayBase = this.AddMenuView<MenuOverlayBaseView>(Array.Empty<object>());
					}
					else
					{
						this._menuOverlayBase.OnOverlayTypeChange(menuOverlayType);
					}
				}
				else
				{
					MenuView menuOverlayBase = this._menuOverlayBase;
					if (menuOverlayBase != null)
					{
						menuOverlayBase.OnOverlayTypeChange(menuOverlayType);
					}
				}
			}
			else
			{
				if (this._menuOverlayBase != null)
				{
					this.RemoveMenuView(this._menuOverlayBase);
					this._menuOverlayBase = null;
				}
				if (this._currentMenuBackground != null)
				{
					this.RemoveMenuView(this._currentMenuBackground);
					this._currentMenuBackground = null;
				}
			}
			this._currentOverlayType = menuOverlayType;
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x000136C8 File Offset: 0x000118C8
		public void CloseCharacterDeveloper()
		{
			this.RemoveMenuView(this._menuCharacterDeveloper);
			this._menuCharacterDeveloper = null;
			foreach (MenuView menuView in this.MenuViews)
			{
				menuView.OnCharacterDeveloperClosed();
			}
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0001372C File Offset: 0x0001192C
		public MenuView AddMenuView<T>(params object[] parameters) where T : MenuView, new()
		{
			MenuView menuView = SandBoxViewCreator.CreateMenuView<T>(parameters);
			menuView.MenuViewContext = this;
			menuView.MenuContext = this._menuContext;
			this.MenuViews.Add(menuView);
			menuView.OnInitialize();
			return menuView;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x00013768 File Offset: 0x00011968
		public T GetMenuView<T>() where T : MenuView
		{
			foreach (MenuView menuView in this.MenuViews)
			{
				T t = menuView as T;
				if (t != null)
				{
					return t;
				}
			}
			return default(T);
		}

		// Token: 0x060001DC RID: 476 RVA: 0x000137D8 File Offset: 0x000119D8
		public void RemoveMenuView(MenuView menuView)
		{
			menuView.OnFinalize();
			menuView.Removed = true;
			this.MenuViews.Remove(menuView);
			if (menuView.ShouldUpdateMenuAfterRemoved)
			{
				this.MenuViews.ForEach(delegate(MenuView m)
				{
					m.OnMenuContextUpdated(this._menuContext);
				});
			}
		}

		// Token: 0x060001DD RID: 477 RVA: 0x00013814 File Offset: 0x00011A14
		void IMenuContextHandler.OnBackgroundMeshNameSet(string name)
		{
			foreach (MenuView menuView in this.MenuViews)
			{
				menuView.OnBackgroundMeshNameSet(name);
			}
		}

		// Token: 0x060001DE RID: 478 RVA: 0x00013868 File Offset: 0x00011A68
		void IMenuContextHandler.OnOpenTownManagement()
		{
			if (this._menuTownManagement == null)
			{
				this._menuTownManagement = this.AddMenuView<MenuTownManagementView>(Array.Empty<object>());
			}
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00013883 File Offset: 0x00011A83
		public void CloseTownManagement()
		{
			this.RemoveMenuView(this._menuTownManagement);
			this._menuTownManagement = null;
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x00013898 File Offset: 0x00011A98
		void IMenuContextHandler.OnOpenRecruitVolunteers()
		{
			if (this._menuRecruitVolunteers == null)
			{
				this._menuRecruitVolunteers = this.AddMenuView<MenuRecruitVolunteersView>(Array.Empty<object>());
			}
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x000138B3 File Offset: 0x00011AB3
		public void CloseRecruitVolunteers()
		{
			this.RemoveMenuView(this._menuRecruitVolunteers);
			this._menuRecruitVolunteers = null;
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x000138C8 File Offset: 0x00011AC8
		void IMenuContextHandler.OnOpenTournamentLeaderboard()
		{
			if (this._menuTournamentLeaderboard == null)
			{
				this._menuTournamentLeaderboard = this.AddMenuView<MenuTournamentLeaderboardView>(Array.Empty<object>());
			}
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x000138E3 File Offset: 0x00011AE3
		public void CloseTournamentLeaderboard()
		{
			this.RemoveMenuView(this._menuTournamentLeaderboard);
			this._menuTournamentLeaderboard = null;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x000138F8 File Offset: 0x00011AF8
		void IMenuContextHandler.OnOpenTroopSelection(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount)
		{
			if (this._menuTroopSelection == null)
			{
				this._menuTroopSelection = this.AddMenuView<MenuTroopSelectionView>(new object[] { fullRoster, initialSelections, canChangeStatusOfTroop, onDone, maxSelectableTroopCount, minSelectableTroopCount });
			}
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x00013944 File Offset: 0x00011B44
		public void CloseTroopSelection()
		{
			this.RemoveMenuView(this._menuTroopSelection);
			this._menuTroopSelection = null;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0001395C File Offset: 0x00011B5C
		void IMenuContextHandler.OnMenuRefresh()
		{
			foreach (MenuView menuView in this.MenuViews)
			{
				menuView.OnMenuContextRefreshed();
			}
		}

		// Token: 0x04000100 RID: 256
		private MenuContext _menuContext;

		// Token: 0x04000101 RID: 257
		private MenuView _currentMenuBase;

		// Token: 0x04000102 RID: 258
		private MenuView _currentMenuBackground;

		// Token: 0x04000103 RID: 259
		private MenuView _menuCharacterDeveloper;

		// Token: 0x04000104 RID: 260
		private MenuView _menuOverlayBase;

		// Token: 0x04000105 RID: 261
		private MenuView _menuRecruitVolunteers;

		// Token: 0x04000106 RID: 262
		private MenuView _menuTournamentLeaderboard;

		// Token: 0x04000107 RID: 263
		private MenuView _menuTroopSelection;

		// Token: 0x04000108 RID: 264
		private MenuView _menuTownManagement;

		// Token: 0x04000109 RID: 265
		private SoundEvent _panelSound;

		// Token: 0x0400010A RID: 266
		private SoundEvent _ambientSound;

		// Token: 0x0400010B RID: 267
		private GameMenu.MenuOverlayType _currentOverlayType;

		// Token: 0x0400010D RID: 269
		private ScreenBase _screen;

		// Token: 0x0400010E RID: 270
		private bool _isActive;
	}
}
