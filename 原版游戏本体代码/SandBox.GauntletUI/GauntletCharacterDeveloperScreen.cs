using System;
using SandBox.View;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI
{
	// Token: 0x02000006 RID: 6
	[GameStateScreen(typeof(CharacterDeveloperState))]
	public class GauntletCharacterDeveloperScreen : ScreenBase, IGameStateListener, IChangeableScreen, ICharacterDeveloperStateHandler
	{
		// Token: 0x06000017 RID: 23 RVA: 0x00002635 File Offset: 0x00000835
		public GauntletCharacterDeveloperScreen(CharacterDeveloperState clanState)
		{
			this._characterDeveloperState = clanState;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002644 File Offset: 0x00000844
		protected override void OnInitialize()
		{
			base.OnInitialize();
			InformationManager.HideAllMessages();
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002654 File Offset: 0x00000854
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			LoadingWindow.DisableGlobalLoadingWindow();
			if (this._gauntletLayer.Input.IsHotKeyReleased("Exit") || this._gauntletLayer.Input.IsGameKeyPressed(37))
			{
				if (this._dataSource.CurrentCharacter.IsInspectingAnAttribute)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.CurrentCharacter.ExecuteStopInspectingCurrentAttribute();
					return;
				}
				if (this._dataSource.CurrentCharacter.PerkSelection.IsActive)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.CurrentCharacter.PerkSelection.ExecuteDeactivate();
					return;
				}
				this.CloseCharacterDeveloperScreen();
				return;
			}
			else
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					this.ExecuteConfirm();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyReleased("Reset"))
				{
					this.ExecuteReset();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyPressed("SwitchToPreviousTab"))
				{
					this.ExecuteSwitchToPreviousTab();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyPressed("SwitchToNextTab"))
				{
					this.ExecuteSwitchToNextTab();
				}
				return;
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x0000277C File Offset: 0x0000097C
		void IGameStateListener.OnActivate()
		{
			base.OnActivate();
			this._characterdeveloper = UIResourceManager.LoadSpriteCategory("ui_characterdeveloper");
			this._dataSource = new CharacterDeveloperVM(new Action(this.CloseCharacterDeveloperScreen));
			this._dataSource.SetGetKeyTextFromKeyIDFunc(new Func<string, TextObject>(Game.Current.GameTextManager.GetHotKeyGameTextFromKeyID));
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			this._dataSource.SetPreviousCharacterInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
			this._dataSource.SetNextCharacterInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
			if (this._characterDeveloperState.InitialSelectedHero != null)
			{
				this._dataSource.SelectHero(this._characterDeveloperState.InitialSelectedHero);
			}
			this._gauntletLayer = new GauntletLayer("CharacterDeveloper", 1, true);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this._gauntletLayer.LoadMovie("CharacterDeveloper", this._dataSource);
			base.AddLayer(this._gauntletLayer);
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.CharacterScreen));
			UISoundsHelper.PlayUISound("event:/ui/panels/panel_character_open");
			this._gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002957 File Offset: 0x00000B57
		void IGameStateListener.OnDeactivate()
		{
			base.OnDeactivate();
			base.RemoveLayer(this._gauntletLayer);
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.None));
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002997 File Offset: 0x00000B97
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002999 File Offset: 0x00000B99
		void IGameStateListener.OnFinalize()
		{
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._gauntletLayer = null;
			this._characterdeveloper.Unload();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000029BF File Offset: 0x00000BBF
		private void CloseCharacterDeveloperScreen()
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			Game.Current.GameStateManager.PopState(0);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000029DB File Offset: 0x00000BDB
		private void ExecuteConfirm()
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			this._dataSource.ExecuteDone();
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000029F2 File Offset: 0x00000BF2
		private void ExecuteReset()
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			this._dataSource.ExecuteReset();
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002A09 File Offset: 0x00000C09
		private void ExecuteSwitchToPreviousTab()
		{
			MBBindingList<SelectorItemVM> itemList = this._dataSource.CharacterList.ItemList;
			if (itemList != null && itemList.Count > 1)
			{
				UISoundsHelper.PlayUISound("event:/ui/checkbox");
			}
			this._dataSource.CharacterList.ExecuteSelectPreviousItem();
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002A46 File Offset: 0x00000C46
		private void ExecuteSwitchToNextTab()
		{
			MBBindingList<SelectorItemVM> itemList = this._dataSource.CharacterList.ItemList;
			if (itemList != null && itemList.Count > 1)
			{
				UISoundsHelper.PlayUISound("event:/ui/checkbox");
			}
			this._dataSource.CharacterList.ExecuteSelectNextItem();
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002A83 File Offset: 0x00000C83
		bool IChangeableScreen.AnyUnsavedChanges()
		{
			return this._dataSource.IsThereAnyChanges();
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002A90 File Offset: 0x00000C90
		bool IChangeableScreen.CanChangesBeApplied()
		{
			return true;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002A93 File Offset: 0x00000C93
		void IChangeableScreen.ApplyChanges()
		{
			this._dataSource.ApplyAllChanges();
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002AA0 File Offset: 0x00000CA0
		void IChangeableScreen.ResetChanges()
		{
			this._dataSource.ExecuteReset();
		}

		// Token: 0x0400000A RID: 10
		private CharacterDeveloperVM _dataSource;

		// Token: 0x0400000B RID: 11
		private GauntletLayer _gauntletLayer;

		// Token: 0x0400000C RID: 12
		private SpriteCategory _characterdeveloper;

		// Token: 0x0400000D RID: 13
		private readonly CharacterDeveloperState _characterDeveloperState;
	}
}
